using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List;

public class EncyclopediaListSortControllerVM : ViewModel
{
	private TextObject _sortedValueLabel = TextObject.GetEmpty();

	private MBBindingList<EncyclopediaListItemVM> _items;

	private EncyclopediaPage _page;

	private EncyclopediaListSelectorVM _sortSelection;

	private string _nameLabel;

	private string _sortedValueLabelText;

	private string _sortByLabel;

	private int _alternativeSortState;

	private bool _isAlternativeSortVisible;

	private bool _isHighlightEnabled;

	[DataSourceProperty]
	public EncyclopediaListSelectorVM SortSelection
	{
		get
		{
			return _sortSelection;
		}
		set
		{
			if (value != _sortSelection)
			{
				_sortSelection = value;
				OnPropertyChangedWithValue(value, "SortSelection");
			}
		}
	}

	[DataSourceProperty]
	public string NameLabel
	{
		get
		{
			return _nameLabel;
		}
		set
		{
			if (value != _nameLabel)
			{
				_nameLabel = value;
				OnPropertyChangedWithValue(value, "NameLabel");
			}
		}
	}

	[DataSourceProperty]
	public string SortedValueLabelText
	{
		get
		{
			return _sortedValueLabelText;
		}
		set
		{
			if (value != _sortedValueLabelText)
			{
				_sortedValueLabelText = value;
				OnPropertyChangedWithValue(value, "SortedValueLabelText");
			}
		}
	}

	[DataSourceProperty]
	public string SortByLabel
	{
		get
		{
			return _sortByLabel;
		}
		set
		{
			if (value != _sortByLabel)
			{
				_sortByLabel = value;
				OnPropertyChangedWithValue(value, "SortByLabel");
			}
		}
	}

	[DataSourceProperty]
	public int AlternativeSortState
	{
		get
		{
			return _alternativeSortState;
		}
		set
		{
			if (value != _alternativeSortState)
			{
				_alternativeSortState = value;
				OnPropertyChangedWithValue(value, "AlternativeSortState");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAlternativeSortVisible
	{
		get
		{
			return _isAlternativeSortVisible;
		}
		set
		{
			if (value != _isAlternativeSortVisible)
			{
				_isAlternativeSortVisible = value;
				OnPropertyChangedWithValue(value, "IsAlternativeSortVisible");
			}
		}
	}

	[DataSourceProperty]
	public bool IsHighlightEnabled
	{
		get
		{
			return _isHighlightEnabled;
		}
		set
		{
			if (value != _isHighlightEnabled)
			{
				_isHighlightEnabled = value;
				OnPropertyChangedWithValue(value, "IsHighlightEnabled");
			}
		}
	}

	public EncyclopediaListSortControllerVM(EncyclopediaPage page, MBBindingList<EncyclopediaListItemVM> items)
	{
		_page = page;
		_items = items;
		UpdateSortItemsFromPage(page);
		Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
	}

	private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent evnt)
	{
		IsHighlightEnabled = evnt.NewNotificationElementID == "EncyclopediaSortButton";
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		NameLabel = GameTexts.FindText("str_sort_by_name_label").ToString();
		SortByLabel = GameTexts.FindText("str_sort_by_label").ToString();
		SortedValueLabelText = _sortedValueLabel.ToString();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
	}

	public void SetSortSelection(int index)
	{
		SortSelection.SelectedIndex = index;
		OnSortSelectionChanged(SortSelection);
	}

	private void UpdateSortItemsFromPage(EncyclopediaPage page)
	{
		SortSelection = new EncyclopediaListSelectorVM(0, OnSortSelectionChanged, OnSortSelectionActivated);
		foreach (EncyclopediaSortController sortController in page.GetSortControllers())
		{
			EncyclopediaListItemComparer comparer = new EncyclopediaListItemComparer(sortController);
			SortSelection.AddItem(new EncyclopediaListSelectorItemVM(comparer));
		}
	}

	private void UpdateAlternativeSortState(EncyclopediaListItemComparerBase comparer)
	{
		CampaignUIHelper.SortState alternativeSortState = (comparer.IsAscending ? CampaignUIHelper.SortState.Ascending : CampaignUIHelper.SortState.Descending);
		AlternativeSortState = (int)alternativeSortState;
	}

	private void OnSortSelectionChanged(SelectorVM<EncyclopediaListSelectorItemVM> s)
	{
		EncyclopediaListItemComparer comparer = s.SelectedItem.Comparer;
		comparer.SortController.Comparer.SetDefaultSortOrder();
		_items.Sort(comparer);
		_items.ApplyActionOnAllItems(delegate(EncyclopediaListItemVM x)
		{
			x.SetComparedValue(comparer.SortController.Comparer);
		});
		_sortedValueLabel = comparer.SortController.Name;
		SortedValueLabelText = _sortedValueLabel.ToString();
		IsAlternativeSortVisible = SortSelection.SelectedIndex != 0;
		UpdateAlternativeSortState(comparer.SortController.Comparer);
	}

	public void ExecuteSwitchSortOrder()
	{
		EncyclopediaListItemComparer comparer = SortSelection.SelectedItem.Comparer;
		comparer.SortController.Comparer.SwitchSortOrder();
		_items.Sort(comparer);
		UpdateAlternativeSortState(comparer.SortController.Comparer);
	}

	public void SetSortOrder(bool isAscending)
	{
		EncyclopediaListItemComparer comparer = SortSelection.SelectedItem.Comparer;
		if (comparer.SortController.Comparer.IsAscending != isAscending)
		{
			comparer.SortController.Comparer.SetSortOrder(isAscending);
			_items.Sort(comparer);
			UpdateAlternativeSortState(comparer.SortController.Comparer);
		}
	}

	public bool GetSortOrder()
	{
		return SortSelection.SelectedItem.Comparer.SortController.Comparer.IsAscending;
	}

	private void OnSortSelectionActivated()
	{
		Game.Current.EventManager.TriggerEvent(new OnEncyclopediaListSortedEvent());
	}
}
