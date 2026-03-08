using System;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages
{
	// Token: 0x020000D6 RID: 214
	public class EncyclopediaPageVM : ViewModel
	{
		// Token: 0x170006BE RID: 1726
		// (get) Token: 0x0600145D RID: 5213 RVA: 0x00050F28 File Offset: 0x0004F128
		public object Obj
		{
			get
			{
				return this._args.Obj;
			}
		}

		// Token: 0x0600145E RID: 5214 RVA: 0x00050F35 File Offset: 0x0004F135
		public virtual string GetName()
		{
			return "";
		}

		// Token: 0x0600145F RID: 5215 RVA: 0x00050F3C File Offset: 0x0004F13C
		public virtual string GetNavigationBarURL()
		{
			return "";
		}

		// Token: 0x06001460 RID: 5216 RVA: 0x00050F43 File Offset: 0x0004F143
		public virtual void Refresh()
		{
		}

		// Token: 0x06001461 RID: 5217 RVA: 0x00050F45 File Offset: 0x0004F145
		public EncyclopediaPageVM(EncyclopediaPageArgs args)
		{
			this._args = args;
			this.BookmarkHint = new HintViewModel();
		}

		// Token: 0x06001462 RID: 5218 RVA: 0x00050F5F File Offset: 0x0004F15F
		public virtual void OnTick()
		{
		}

		// Token: 0x06001463 RID: 5219 RVA: 0x00050F61 File Offset: 0x0004F161
		public virtual void ExecuteSwitchBookmarkedState()
		{
			this.IsBookmarked = !this.IsBookmarked;
			this.UpdateBookmarkHintText();
		}

		// Token: 0x06001464 RID: 5220 RVA: 0x00050F78 File Offset: 0x0004F178
		protected void UpdateBookmarkHintText()
		{
			if (this.IsBookmarked)
			{
				this.BookmarkHint.HintText = new TextObject("{=BV5exuPf}Remove From Bookmarks", null);
				return;
			}
			this.BookmarkHint.HintText = new TextObject("{=d8jrv3nA}Add To Bookmarks", null);
		}

		// Token: 0x170006BF RID: 1727
		// (get) Token: 0x06001465 RID: 5221 RVA: 0x00050FAF File Offset: 0x0004F1AF
		// (set) Token: 0x06001466 RID: 5222 RVA: 0x00050FB7 File Offset: 0x0004F1B7
		[DataSourceProperty]
		public bool IsLoadingOver
		{
			get
			{
				return this._isLoadingOver;
			}
			set
			{
				if (value != this._isLoadingOver)
				{
					this._isLoadingOver = value;
					base.OnPropertyChangedWithValue(value, "IsLoadingOver");
				}
			}
		}

		// Token: 0x170006C0 RID: 1728
		// (get) Token: 0x06001467 RID: 5223 RVA: 0x00050FD5 File Offset: 0x0004F1D5
		// (set) Token: 0x06001468 RID: 5224 RVA: 0x00050FDD File Offset: 0x0004F1DD
		[DataSourceProperty]
		public bool IsBookmarked
		{
			get
			{
				return this._isBookmarked;
			}
			set
			{
				if (value != this._isBookmarked)
				{
					this._isBookmarked = value;
					base.OnPropertyChanged("IsBookmarked");
				}
			}
		}

		// Token: 0x170006C1 RID: 1729
		// (get) Token: 0x06001469 RID: 5225 RVA: 0x00050FFA File Offset: 0x0004F1FA
		// (set) Token: 0x0600146A RID: 5226 RVA: 0x00051002 File Offset: 0x0004F202
		[DataSourceProperty]
		public HintViewModel BookmarkHint
		{
			get
			{
				return this._bookmarkHint;
			}
			set
			{
				if (value != this._bookmarkHint)
				{
					this._bookmarkHint = value;
					base.OnPropertyChanged("BookmarkHint");
				}
			}
		}

		// Token: 0x170006C2 RID: 1730
		// (get) Token: 0x0600146B RID: 5227 RVA: 0x0005101F File Offset: 0x0004F21F
		// (set) Token: 0x0600146C RID: 5228 RVA: 0x00051022 File Offset: 0x0004F222
		[DataSourceProperty]
		public virtual MBBindingList<EncyclopediaListItemVM> Items
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		// Token: 0x170006C3 RID: 1731
		// (get) Token: 0x0600146D RID: 5229 RVA: 0x00051024 File Offset: 0x0004F224
		// (set) Token: 0x0600146E RID: 5230 RVA: 0x00051027 File Offset: 0x0004F227
		[DataSourceProperty]
		public virtual MBBindingList<EncyclopediaFilterGroupVM> FilterGroups
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		// Token: 0x170006C4 RID: 1732
		// (get) Token: 0x0600146F RID: 5231 RVA: 0x00051029 File Offset: 0x0004F229
		// (set) Token: 0x06001470 RID: 5232 RVA: 0x0005102C File Offset: 0x0004F22C
		[DataSourceProperty]
		public virtual EncyclopediaListSortControllerVM SortController
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		// Token: 0x0400095A RID: 2394
		private EncyclopediaPageArgs _args;

		// Token: 0x0400095B RID: 2395
		private bool _isLoadingOver;

		// Token: 0x0400095C RID: 2396
		private bool _isBookmarked;

		// Token: 0x0400095D RID: 2397
		private HintViewModel _bookmarkHint;
	}
}
