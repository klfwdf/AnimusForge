using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List;

public class EncyclopediaListVM : EncyclopediaPageVM
{
	public readonly EncyclopediaPage Page;

	private MBBindingList<EncyclopediaFilterGroupVM> _filterGroups;

	private MBBindingList<EncyclopediaListItemVM> _items;

	private EncyclopediaListSortControllerVM _sortController;

	private bool _isInitializationOver;

	private bool _isFilterHighlightEnabled;

	private string _emptyListText;

	private string _lastSelectedItemId;

	[DataSourceProperty]
	public string EmptyListText
	{
		get
		{
			return _emptyListText;
		}
		set
		{
			if (value != _emptyListText)
			{
				_emptyListText = value;
				OnPropertyChangedWithValue(value, "EmptyListText");
			}
		}
	}

	[DataSourceProperty]
	public string LastSelectedItemId
	{
		get
		{
			return _lastSelectedItemId;
		}
		set
		{
			if (value != _lastSelectedItemId)
			{
				_lastSelectedItemId = value;
				OnPropertyChangedWithValue(value, "LastSelectedItemId");
			}
		}
	}

	[DataSourceProperty]
	public override MBBindingList<EncyclopediaListItemVM> Items
	{
		get
		{
			return _items;
		}
		set
		{
			if (value != _items)
			{
				_items = value;
				OnPropertyChangedWithValue(value, "Items");
			}
		}
	}

	[DataSourceProperty]
	public override EncyclopediaListSortControllerVM SortController
	{
		get
		{
			return _sortController;
		}
		set
		{
			if (value != _sortController)
			{
				_sortController = value;
				OnPropertyChangedWithValue(value, "SortController");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInitializationOver
	{
		get
		{
			return _isInitializationOver;
		}
		set
		{
			if (value != _isInitializationOver)
			{
				_isInitializationOver = value;
				OnPropertyChangedWithValue(value, "IsInitializationOver");
			}
		}
	}

	[DataSourceProperty]
	public bool IsFilterHighlightEnabled
	{
		get
		{
			return _isFilterHighlightEnabled;
		}
		set
		{
			if (value != _isFilterHighlightEnabled)
			{
				_isFilterHighlightEnabled = value;
				OnPropertyChangedWithValue(value, "IsFilterHighlightEnabled");
			}
		}
	}

	[DataSourceProperty]
	public override MBBindingList<EncyclopediaFilterGroupVM> FilterGroups
	{
		get
		{
			return _filterGroups;
		}
		set
		{
			if (value != _filterGroups)
			{
				_filterGroups = value;
				OnPropertyChangedWithValue(value, "FilterGroups");
			}
		}
	}

	public EncyclopediaListVM(EncyclopediaPageArgs args)
		: base(args)
	{
		Page = base.Obj as EncyclopediaPage;
		Items = new MBBindingList<EncyclopediaListItemVM>();
		FilterGroups = new MBBindingList<EncyclopediaFilterGroupVM>();
		SortController = new EncyclopediaListSortControllerVM(Page, Items);
		IsInitializationOver = true;
		foreach (EncyclopediaFilterGroup filterItem in Page.GetFilterItems())
		{
			FilterGroups.Add(new EncyclopediaFilterGroupVM(filterItem, UpdateFilters));
		}
		IsInitializationOver = false;
		Items.Clear();
		foreach (EncyclopediaListItem listItem in Page.GetListItems())
		{
			EncyclopediaListItemVM encyclopediaListItemVM = new EncyclopediaListItemVM(listItem);
			encyclopediaListItemVM.IsFiltered = Page.IsFiltered(encyclopediaListItemVM.Object);
			Items.Add(encyclopediaListItemVM);
		}
		RefreshValues();
		IsInitializationOver = true;
		Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
	}

	private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent evnt)
	{
		IsFilterHighlightEnabled = evnt.NewNotificationElementID == "EncyclopediaFiltersContainer";
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		SortController.RefreshValues();
		EmptyListText = GameTexts.FindText("str_encyclopedia_empty_list_error").ToString();
		Items.ApplyActionOnAllItems(delegate(EncyclopediaListItemVM x)
		{
			x.RefreshValues();
		});
		FilterGroups.ApplyActionOnAllItems(delegate(EncyclopediaFilterGroupVM x)
		{
			x.RefreshValues();
		});
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
		SortController?.OnFinalize();
		SortController = null;
		FilterGroups.ApplyActionOnAllItems(delegate(EncyclopediaFilterGroupVM x)
		{
			x.OnFinalize();
		});
		FilterGroups.Clear();
		Items.ApplyActionOnAllItems(delegate(EncyclopediaListItemVM x)
		{
			x.OnFinalize();
		});
		Items.Clear();
	}

	public override string GetName()
	{
		return Page.GetName().ToString();
	}

	public override string GetNavigationBarURL()
	{
		string text = HyperlinkTexts.GetGenericHyperlinkText("Home", GameTexts.FindText("str_encyclopedia_home").ToString()) + " \\ ";
		if (Page.HasIdentifierType(typeof(Kingdom)))
		{
			text += GameTexts.FindText("str_encyclopedia_kingdoms").ToString();
		}
		else if (Page.HasIdentifierType(typeof(Clan)))
		{
			text += GameTexts.FindText("str_encyclopedia_clans").ToString();
		}
		else if (Page.HasIdentifierType(typeof(Hero)))
		{
			text += GameTexts.FindText("str_encyclopedia_heroes").ToString();
		}
		else if (Page.HasIdentifierType(typeof(Settlement)))
		{
			text += GameTexts.FindText("str_encyclopedia_settlements").ToString();
		}
		else if (Page.HasIdentifierType(typeof(CharacterObject)))
		{
			text += GameTexts.FindText("str_encyclopedia_troops").ToString();
		}
		else if (Page.HasIdentifierType(typeof(Concept)))
		{
			text += GameTexts.FindText("str_encyclopedia_concepts").ToString();
		}
		else if (Page.HasIdentifierType(typeof(ShipHull)))
		{
			text += GameTexts.FindText("str_encyclopedia_ships").ToString();
		}
		return text;
	}

	private void ExecuteResetFilters()
	{
		foreach (EncyclopediaFilterGroupVM filterGroup in FilterGroups)
		{
			foreach (EncyclopediaListFilterVM filter in filterGroup.Filters)
			{
				filter.IsSelected = false;
			}
		}
	}

	public void CopyFiltersFrom(Dictionary<EncyclopediaFilterItem, bool> filters)
	{
		FilterGroups.ApplyActionOnAllItems(delegate(EncyclopediaFilterGroupVM x)
		{
			x.CopyFiltersFrom(filters);
		});
	}

	public override void Refresh()
	{
		base.Refresh();
		foreach (EncyclopediaListItemVM item in Items)
		{
			if (item.Object is Hero hero)
			{
				item.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(hero);
			}
			else if (item.Object is Clan clan)
			{
				item.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(clan);
			}
			else if (item.Object is Concept concept)
			{
				item.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(concept);
			}
			else if (item.Object is Kingdom kingdom)
			{
				item.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(kingdom);
			}
			else if (item.Object is Settlement settlement)
			{
				item.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(settlement);
			}
			else if (item.Object is CharacterObject unit)
			{
				item.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(unit);
			}
			else if (item.Object is ShipHull shipHull)
			{
				item.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(shipHull);
			}
		}
		_isInitializationOver = false;
		IsInitializationOver = true;
	}

	private void UpdateFilters(EncyclopediaListFilterVM filterVM)
	{
		IsInitializationOver = false;
		foreach (EncyclopediaListItemVM item in Items)
		{
			item.IsFiltered = Page.IsFiltered(item.Object);
		}
		IsInitializationOver = true;
	}
}
