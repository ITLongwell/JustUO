#region Header
//   Vorspire    _,-'/-'/  Rect2DList.cs
//   .      __,-; ,'( '/
//    \.    `-.__`-._`:_,-._       _ , . ``
//     `:-._,------' ` _,`--` -: `_ , ` ,' :
//        `---..__,,--'  (C) 2014  ` -'. -'
//        #  Vita-Nex [http://core.vita-nex.com]  #
//  {o)xxx|===============-   #   -===============|xxx(o}
//        #        The MIT License (MIT)          #
#endregion

#region References
using System;
using System.Collections.Generic;

using Server;
using Server.Gumps;
using Server.Mobiles;

using VitaNex.Crypto;
using VitaNex.Items;
#endregion

namespace VitaNex.SuperGumps.UI
{
	public abstract class Rect2DListGump : GenericListGump<Rectangle2D>
	{
		public Dictionary<Rectangle2D, List<BoundsPreviewTile>> PreviewCache { get; protected set; }
		public CryptoHashCode PreviewHash { get; private set; }

		public bool Preview { get; set; }
		public int PreviewHue { get; set; }
		public string PreviewTileName { get; set; }

		public Rectangle2D? InputRect { get; set; }
		public Map InputMap { get; set; }

		public Rect2DListGump(
			PlayerMobile user,
			Gump parent = null,
			int? x = null,
			int? y = null,
			IEnumerable<Rectangle2D> list = null,
			string emptyText = null,
			string title = null,
			IEnumerable<ListGumpEntry> opts = null,
			bool canAdd = true,
			bool canRemove = true,
			bool canClear = true,
			Action<Rectangle2D> addCallback = null,
			Action<Rectangle2D> removeCallback = null,
			Action<List<Rectangle2D>> applyCallback = null,
			Action clearCallback = null)
			: base(
				user,
				parent,
				x,
				y,
				list,
				emptyText,
				title,
				opts,
				canAdd,
				canRemove,
				canClear,
				addCallback,
				removeCallback,
				applyCallback,
				clearCallback)
		{
			InputMap = User.Map;
			PreviewCache = new Dictionary<Rectangle2D, List<BoundsPreviewTile>>();

			Preview = false;
			PreviewHue = TextHue;
			PreviewTileName = "Preview Tile";

			ForceRecompile = true;
		}

		protected override int GetLabelHue(int index, int pageIndex, Rectangle2D entry)
		{
			return PreviewHue;
		}

		protected override string GetLabelText(int index, int pageIndex, Rectangle2D entry)
		{
			return entry.Start + " -> " + entry.End;
		}

		public override string GetSearchKeyFor(Rectangle2D key)
		{
			return key.Start + " -> " + key.End;
		}

		protected override bool OnBeforeListAdd()
		{
			if (InputRect != null)
			{
				return true;
			}

			Minimize();

			BoundingBoxPicker.Begin(
				User,
				(from, map, start, end, state) =>
				{
					InputMap = map;
					InputRect = new Rectangle2D(start, end.Clone2D(1, 1));
					HandleAdd();
					InputRect = null;

					Maximize();
				},
				null);

			return false;
		}

		public override Rectangle2D GetListAddObject()
		{
			return InputRect != null ? InputRect.Value : default(Rectangle2D);
		}

		protected override void OnSend()
		{
			DisplayPreview();

			base.OnSend();
		}

		protected override void OnRefreshed()
		{
			base.OnRefreshed();

			DisplayPreview();
		}

		protected override void OnClosed(bool all)
		{
			base.OnClosed(all);

			ClearPreview();
		}

		protected override void CompileEntryOptions(MenuGumpOptions opts, Rectangle2D entry)
		{
			opts.AppendEntry(
				new ListGumpEntry("Go To", () => User.MoveToWorld(entry.Start.GetWorldTop(InputMap), InputMap), HighlightHue));

			base.CompileEntryOptions(opts, entry);
		}

		public virtual void ClearPreview()
		{
			PreviewCache.Values.ForEach(
				l =>
				{
					l.ForEach(
						t =>
						{
							if (t != null && !t.Deleted)
							{
								t.Delete();
							}
						});

					l.Clear();
				});

			PreviewCache.Clear();
			PreviewHash = null;
		}

		public virtual void DisplayPreview()
		{
			if (!Preview || InputMap == null || InputMap == Map.Internal)
			{
				ClearPreview();
				return;
			}

			if (PreviewHash != null && PreviewHash == GetInternalHashCode())
			{
				return;
			}

			ClearPreview();

			List.ForEach(
				r =>
				{
					PreviewCache.Add(r, new List<BoundsPreviewTile>(r.Width * r.Height));

					int spacing = Math.Max(1, Math.Min(10, (r.Width * r.Height) / 100));

					r.ForEach(
						p =>
						{
							if (p.X != r.Start.X && p.Y != r.Start.Y && p.X != r.End.X - 1 && p.Y != r.End.Y - 1 &&
								(p.X % spacing != 0 || p.Y % spacing != 0))
							{
								return;
							}

							var t = new BoundsPreviewTile(PreviewTileName, PreviewHue);
							t.MoveToWorld(p.GetWorldTop(InputMap), InputMap);
							PreviewCache[r].Add(t);
						});

					PreviewCache[r].TrimExcess();
				});

			PreviewHash = GetInternalHashCode();
		}

		private CryptoHashCode GetInternalHashCode()
		{
			string seed = String.Empty;
			PreviewCache.Keys.ForEach(r => { seed += (r.Start.ToString() + r.End.ToString()); });
			return (String.IsNullOrEmpty(seed) ? null : CryptoGenerator.GenHashCode(CryptoHashType.MD5, seed));
		}

		protected override void CompileMenuOptions(MenuGumpOptions list)
		{
			if (!Preview)
			{
				list.Replace(
					"Disable Preview",
					new ListGumpEntry(
						"Enable Preview",
						() =>
						{
							Preview = true;
							DisplayPreview();
							Refresh();
						},
						HighlightHue));
			}
			else
			{
				list.Replace(
					"Enable Preview",
					new ListGumpEntry(
						"Disable Preview",
						() =>
						{
							Preview = false;
							ClearPreview();
							Refresh();
						},
						ErrorHue));
			}

			base.CompileMenuOptions(list);
		}
	}
}