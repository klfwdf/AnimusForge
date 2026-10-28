using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List;
using TaleWorlds.Library;

namespace SandBox.GauntletUI.Encyclopedia;

public class EncyclopediaListViewDataController
{
	private readonly struct EncyclopediaListViewData
	{
		public readonly Dictionary<EncyclopediaFilterItem, bool> Filters;

		public readonly int SelectedSortIndex;

		public readonly string LastSelectedItemId;

		public readonly bool IsAscending;

		public EncyclopediaListViewData(MBBindingList<EncyclopediaFilterGroupVM> filters, int selectedSortIndex, string lastSelectedItemId, bool isAscending)
		{
			Dictionary<EncyclopediaFilterItem, bool> dictionary = new Dictionary<EncyclopediaFilterItem, bool>();
			foreach (EncyclopediaFilterGroupVM filter in filters)
			{
				foreach (EncyclopediaListFilterVM filter2 in filter.Filters)
				{
					if (!dictionary.ContainsKey(filter2.Filter))
					{
						dictionary.Add(filter2.Filter, filter2.IsSelected);
					}
				}
			}
			Filters = dictionary;
			SelectedSortIndex = selectedSortIndex;
			LastSelectedItemId = lastSelectedItemId;
			IsAscending = isAscending;
		}
	}

	private Dictionary<EncyclopediaPage, EncyclopediaListViewData> _listData;

	public EncyclopediaListViewDataController()
	{
		_listData = new Dictionary<EncyclopediaPage, EncyclopediaListViewData>();
		foreach (EncyclopediaPage encyclopediaPage in Campaign.Current.EncyclopediaManager.GetEncyclopediaPages())
		{
			if (!_listData.ContainsKey(encyclopediaPage))
			{
				_listData.Add(encyclopediaPage, new EncyclopediaListViewData(new MBBindingList<EncyclopediaFilterGroupVM>(), 0, "", isAscending: false));
			}
		}
	}

	public void SaveListData(EncyclopediaListVM list, string id)
	{
		if (list != null && _listData.ContainsKey(list.Page))
		{
			int selectedSortIndex = list.SortController?.SortSelection?.SelectedIndex ?? 0;
			_listData[list.Page] = new EncyclopediaListViewData(list.FilterGroups, selectedSortIndex, id, list.SortController?.GetSortOrder() ?? false);
		}
	}

	public void LoadListData(EncyclopediaListVM list)
	{
		if (list != null && _listData.ContainsKey(list.Page))
		{
			EncyclopediaListViewData encyclopediaListViewData = _listData[list.Page];
			list.SortController?.SetSortSelection(encyclopediaListViewData.SelectedSortIndex);
			list.SortController?.SetSortOrder(encyclopediaListViewData.IsAscending);
			list.CopyFiltersFrom(encyclopediaListViewData.Filters);
			list.LastSelectedItemId = encyclopediaListViewData.LastSelectedItemId;
		}
	}
}
