using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party;

public class PartySortControllerVM : ViewModel
{
	private readonly PartyScreenLogic.PartyRosterSide _rosterSide;

	private readonly Action<PartyScreenLogic.PartyRosterSide, PartyScreenLogic.TroopSortType, bool> _onSort;

	private PartyScreenLogic.TroopSortType _sortType;

	private bool _isAscending;

	private bool _isCustomSort;

	private SelectorVM<TroopSortSelectorItemVM> _sortOptions;

	[DataSourceProperty]
	public bool IsAscending
	{
		get
		{
			return _isAscending;
		}
		set
		{
			if (value != _isAscending)
			{
				_isAscending = value;
				OnPropertyChangedWithValue(value, "IsAscending");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCustomSort
	{
		get
		{
			return _isCustomSort;
		}
		set
		{
			if (value != _isCustomSort)
			{
				_isCustomSort = value;
				OnPropertyChangedWithValue(value, "IsCustomSort");
			}
		}
	}

	[DataSourceProperty]
	public SelectorVM<TroopSortSelectorItemVM> SortOptions
	{
		get
		{
			return _sortOptions;
		}
		set
		{
			if (value != _sortOptions)
			{
				_sortOptions = value;
				OnPropertyChangedWithValue(value, "SortOptions");
			}
		}
	}

	public PartySortControllerVM(PartyScreenLogic.PartyRosterSide rosterSide, Action<PartyScreenLogic.PartyRosterSide, PartyScreenLogic.TroopSortType, bool> onSort)
	{
		_rosterSide = rosterSide;
		SortOptions = new SelectorVM<TroopSortSelectorItemVM>(-1, OnSortSelected);
		SortOptions.AddItem(new TroopSortSelectorItemVM(new TextObject("{=zMMqgxb1}Type"), PartyScreenLogic.TroopSortType.Type));
		SortOptions.AddItem(new TroopSortSelectorItemVM(new TextObject("{=PDdh1sBj}Name"), PartyScreenLogic.TroopSortType.Name));
		SortOptions.AddItem(new TroopSortSelectorItemVM(new TextObject("{=zFDoDbNj}Count"), PartyScreenLogic.TroopSortType.Count));
		SortOptions.AddItem(new TroopSortSelectorItemVM(new TextObject("{=cc1d7mkq}Tier"), PartyScreenLogic.TroopSortType.Tier));
		SortOptions.AddItem(new TroopSortSelectorItemVM(new TextObject("{=jvOYgHOe}Custom"), PartyScreenLogic.TroopSortType.Custom));
		SortOptions.SelectedIndex = SortOptions.ItemList.Count - 1;
		IsAscending = true;
		_onSort = onSort;
	}

	private void OnSortSelected(SelectorVM<TroopSortSelectorItemVM> selector)
	{
		_sortType = selector.SelectedItem.SortType;
		IsCustomSort = _sortType == PartyScreenLogic.TroopSortType.Custom;
		_onSort?.Invoke(_rosterSide, _sortType, IsAscending);
	}

	public void SelectSortType(PartyScreenLogic.TroopSortType sortType)
	{
		for (int i = 0; i < SortOptions.ItemList.Count; i++)
		{
			if (SortOptions.ItemList[i].SortType == sortType)
			{
				SortOptions.SelectedIndex = i;
			}
		}
	}

	public void SortWith(PartyScreenLogic.TroopSortType sortType, bool isAscending)
	{
		_onSort?.Invoke(_rosterSide, sortType, isAscending);
	}

	public void ExecuteToggleOrder()
	{
		IsAscending = !IsAscending;
		_onSort?.Invoke(_rosterSide, _sortType, IsAscending);
	}
}
