using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List;
using TaleWorlds.Library;

namespace SandBox.GauntletUI.Encyclopedia
{
	// Token: 0x02000046 RID: 70
	public class EncyclopediaListViewDataController
	{
		// Token: 0x0600032E RID: 814 RVA: 0x00012CD0 File Offset: 0x00010ED0
		public EncyclopediaListViewDataController()
		{
			this._listData = new Dictionary<EncyclopediaPage, EncyclopediaListViewDataController.EncyclopediaListViewData>();
			foreach (EncyclopediaPage key in Campaign.Current.EncyclopediaManager.GetEncyclopediaPages())
			{
				if (!this._listData.ContainsKey(key))
				{
					this._listData.Add(key, new EncyclopediaListViewDataController.EncyclopediaListViewData(new MBBindingList<EncyclopediaFilterGroupVM>(), 0, "", false));
				}
			}
		}

		// Token: 0x0600032F RID: 815 RVA: 0x00012D5C File Offset: 0x00010F5C
		public void SaveListData(EncyclopediaListVM list, string id)
		{
			if (list != null && this._listData.ContainsKey(list.Page))
			{
				EncyclopediaListSortControllerVM sortController = list.SortController;
				int? num;
				if (sortController == null)
				{
					num = null;
				}
				else
				{
					EncyclopediaListSelectorVM sortSelection = sortController.SortSelection;
					num = ((sortSelection != null) ? new int?(sortSelection.SelectedIndex) : null);
				}
				int num2 = num ?? 0;
				Dictionary<EncyclopediaPage, EncyclopediaListViewDataController.EncyclopediaListViewData> listData = this._listData;
				EncyclopediaPage page = list.Page;
				MBBindingList<EncyclopediaFilterGroupVM> filterGroups = list.FilterGroups;
				int selectedSortIndex = num2;
				EncyclopediaListSortControllerVM sortController2 = list.SortController;
				listData[page] = new EncyclopediaListViewDataController.EncyclopediaListViewData(filterGroups, selectedSortIndex, id, sortController2 != null && sortController2.GetSortOrder());
			}
		}

		// Token: 0x06000330 RID: 816 RVA: 0x00012DFC File Offset: 0x00010FFC
		public void LoadListData(EncyclopediaListVM list)
		{
			if (list != null && this._listData.ContainsKey(list.Page))
			{
				EncyclopediaListViewDataController.EncyclopediaListViewData encyclopediaListViewData = this._listData[list.Page];
				EncyclopediaListSortControllerVM sortController = list.SortController;
				if (sortController != null)
				{
					sortController.SetSortSelection(encyclopediaListViewData.SelectedSortIndex);
				}
				EncyclopediaListSortControllerVM sortController2 = list.SortController;
				if (sortController2 != null)
				{
					sortController2.SetSortOrder(encyclopediaListViewData.IsAscending);
				}
				list.CopyFiltersFrom(encyclopediaListViewData.Filters);
				list.LastSelectedItemId = encyclopediaListViewData.LastSelectedItemId;
			}
		}

		// Token: 0x04000142 RID: 322
		private Dictionary<EncyclopediaPage, EncyclopediaListViewDataController.EncyclopediaListViewData> _listData;

		// Token: 0x02000086 RID: 134
		private readonly struct EncyclopediaListViewData
		{
			// Token: 0x06000462 RID: 1122 RVA: 0x0001825C File Offset: 0x0001645C
			public EncyclopediaListViewData(MBBindingList<EncyclopediaFilterGroupVM> filters, int selectedSortIndex, string lastSelectedItemId, bool isAscending)
			{
				Dictionary<EncyclopediaFilterItem, bool> dictionary = new Dictionary<EncyclopediaFilterItem, bool>();
				foreach (EncyclopediaFilterGroupVM encyclopediaFilterGroupVM in filters)
				{
					foreach (EncyclopediaListFilterVM encyclopediaListFilterVM in encyclopediaFilterGroupVM.Filters)
					{
						if (!dictionary.ContainsKey(encyclopediaListFilterVM.Filter))
						{
							dictionary.Add(encyclopediaListFilterVM.Filter, encyclopediaListFilterVM.IsSelected);
						}
					}
				}
				this.Filters = dictionary;
				this.SelectedSortIndex = selectedSortIndex;
				this.LastSelectedItemId = lastSelectedItemId;
				this.IsAscending = isAscending;
			}

			// Token: 0x04000209 RID: 521
			public readonly Dictionary<EncyclopediaFilterItem, bool> Filters;

			// Token: 0x0400020A RID: 522
			public readonly int SelectedSortIndex;

			// Token: 0x0400020B RID: 523
			public readonly string LastSelectedItemId;

			// Token: 0x0400020C RID: 524
			public readonly bool IsAscending;
		}
	}
}
