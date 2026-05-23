using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party
{
	// Token: 0x02000029 RID: 41
	public class PartySortControllerVM : ViewModel
	{
		// Token: 0x0600033D RID: 829 RVA: 0x000161B0 File Offset: 0x000143B0
		public PartySortControllerVM(PartyScreenLogic.PartyRosterSide rosterSide, Action<PartyScreenLogic.PartyRosterSide, PartyScreenLogic.TroopSortType, bool> onSort)
		{
			this._rosterSide = rosterSide;
			this.SortOptions = new SelectorVM<TroopSortSelectorItemVM>(-1, new Action<SelectorVM<TroopSortSelectorItemVM>>(this.OnSortSelected));
			this.SortOptions.AddItem(new TroopSortSelectorItemVM(new TextObject("{=zMMqgxb1}Type", null), PartyScreenLogic.TroopSortType.Type));
			this.SortOptions.AddItem(new TroopSortSelectorItemVM(new TextObject("{=PDdh1sBj}Name", null), PartyScreenLogic.TroopSortType.Name));
			this.SortOptions.AddItem(new TroopSortSelectorItemVM(new TextObject("{=zFDoDbNj}Count", null), PartyScreenLogic.TroopSortType.Count));
			this.SortOptions.AddItem(new TroopSortSelectorItemVM(new TextObject("{=cc1d7mkq}Tier", null), PartyScreenLogic.TroopSortType.Tier));
			this.SortOptions.AddItem(new TroopSortSelectorItemVM(new TextObject("{=jvOYgHOe}Custom", null), PartyScreenLogic.TroopSortType.Custom));
			this.SortOptions.SelectedIndex = this.SortOptions.ItemList.Count - 1;
			this.IsAscending = true;
			this._onSort = onSort;
		}

		// Token: 0x0600033E RID: 830 RVA: 0x0001629C File Offset: 0x0001449C
		private void OnSortSelected(SelectorVM<TroopSortSelectorItemVM> selector)
		{
			this._sortType = selector.SelectedItem.SortType;
			this.IsCustomSort = this._sortType == PartyScreenLogic.TroopSortType.Custom;
			Action<PartyScreenLogic.PartyRosterSide, PartyScreenLogic.TroopSortType, bool> onSort = this._onSort;
			if (onSort == null)
			{
				return;
			}
			onSort(this._rosterSide, this._sortType, this.IsAscending);
		}

		// Token: 0x0600033F RID: 831 RVA: 0x000162EC File Offset: 0x000144EC
		public void SelectSortType(PartyScreenLogic.TroopSortType sortType)
		{
			for (int i = 0; i < this.SortOptions.ItemList.Count; i++)
			{
				if (this.SortOptions.ItemList[i].SortType == sortType)
				{
					this.SortOptions.SelectedIndex = i;
				}
			}
		}

		// Token: 0x06000340 RID: 832 RVA: 0x00016339 File Offset: 0x00014539
		public void SortWith(PartyScreenLogic.TroopSortType sortType, bool isAscending)
		{
			Action<PartyScreenLogic.PartyRosterSide, PartyScreenLogic.TroopSortType, bool> onSort = this._onSort;
			if (onSort == null)
			{
				return;
			}
			onSort(this._rosterSide, sortType, isAscending);
		}

		// Token: 0x06000341 RID: 833 RVA: 0x00016353 File Offset: 0x00014553
		public void ExecuteToggleOrder()
		{
			this.IsAscending = !this.IsAscending;
			Action<PartyScreenLogic.PartyRosterSide, PartyScreenLogic.TroopSortType, bool> onSort = this._onSort;
			if (onSort == null)
			{
				return;
			}
			onSort(this._rosterSide, this._sortType, this.IsAscending);
		}

		// Token: 0x170000D7 RID: 215
		// (get) Token: 0x06000342 RID: 834 RVA: 0x00016386 File Offset: 0x00014586
		// (set) Token: 0x06000343 RID: 835 RVA: 0x0001638E File Offset: 0x0001458E
		[DataSourceProperty]
		public bool IsAscending
		{
			get
			{
				return this._isAscending;
			}
			set
			{
				if (value != this._isAscending)
				{
					this._isAscending = value;
					base.OnPropertyChangedWithValue(value, "IsAscending");
				}
			}
		}

		// Token: 0x170000D8 RID: 216
		// (get) Token: 0x06000344 RID: 836 RVA: 0x000163AC File Offset: 0x000145AC
		// (set) Token: 0x06000345 RID: 837 RVA: 0x000163B4 File Offset: 0x000145B4
		[DataSourceProperty]
		public bool IsCustomSort
		{
			get
			{
				return this._isCustomSort;
			}
			set
			{
				if (value != this._isCustomSort)
				{
					this._isCustomSort = value;
					base.OnPropertyChangedWithValue(value, "IsCustomSort");
				}
			}
		}

		// Token: 0x170000D9 RID: 217
		// (get) Token: 0x06000346 RID: 838 RVA: 0x000163D2 File Offset: 0x000145D2
		// (set) Token: 0x06000347 RID: 839 RVA: 0x000163DA File Offset: 0x000145DA
		[DataSourceProperty]
		public SelectorVM<TroopSortSelectorItemVM> SortOptions
		{
			get
			{
				return this._sortOptions;
			}
			set
			{
				if (value != this._sortOptions)
				{
					this._sortOptions = value;
					base.OnPropertyChangedWithValue<SelectorVM<TroopSortSelectorItemVM>>(value, "SortOptions");
				}
			}
		}

		// Token: 0x04000172 RID: 370
		private readonly PartyScreenLogic.PartyRosterSide _rosterSide;

		// Token: 0x04000173 RID: 371
		private readonly Action<PartyScreenLogic.PartyRosterSide, PartyScreenLogic.TroopSortType, bool> _onSort;

		// Token: 0x04000174 RID: 372
		private PartyScreenLogic.TroopSortType _sortType;

		// Token: 0x04000175 RID: 373
		private bool _isAscending;

		// Token: 0x04000176 RID: 374
		private bool _isCustomSort;

		// Token: 0x04000177 RID: 375
		private SelectorVM<TroopSortSelectorItemVM> _sortOptions;
	}
}
