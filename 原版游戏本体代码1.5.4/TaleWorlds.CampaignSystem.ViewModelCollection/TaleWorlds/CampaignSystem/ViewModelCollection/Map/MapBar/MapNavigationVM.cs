using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;

public class MapNavigationVM : ViewModel
{
	protected INavigationHandler _navigationHandler;

	protected Func<MapBarShortcuts> _getMapBarShortcuts;

	protected MapBarShortcuts _shortcuts;

	protected readonly IViewDataTracker _viewDataTracker;

	private MBBindingList<MapNavigationItemVM> _navigationItems;

	private HintViewModel _encyclopediaHint;

	private HintViewModel _financeHint;

	private HintViewModel _centerCameraHint;

	private HintViewModel _campHint;

	[DataSourceProperty]
	public MBBindingList<MapNavigationItemVM> NavigationItems
	{
		get
		{
			return _navigationItems;
		}
		set
		{
			if (value != _navigationItems)
			{
				_navigationItems = value;
				OnPropertyChangedWithValue(value, "NavigationItems");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel FinanceHint
	{
		get
		{
			return _financeHint;
		}
		set
		{
			if (value != _financeHint)
			{
				_financeHint = value;
				OnPropertyChangedWithValue(value, "FinanceHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel EncyclopediaHint
	{
		get
		{
			return _encyclopediaHint;
		}
		set
		{
			if (value != _encyclopediaHint)
			{
				_encyclopediaHint = value;
				OnPropertyChangedWithValue(value, "EncyclopediaHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel CenterCameraHint
	{
		get
		{
			return _centerCameraHint;
		}
		set
		{
			if (value != _centerCameraHint)
			{
				_centerCameraHint = value;
				OnPropertyChangedWithValue(value, "CenterCameraHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel CampHint
	{
		get
		{
			return _campHint;
		}
		set
		{
			if (value != _campHint)
			{
				_campHint = value;
				OnPropertyChangedWithValue(value, "CampHint");
			}
		}
	}

	public MapNavigationVM(INavigationHandler navigationHandler, Func<MapBarShortcuts> getMapBarShortcuts)
	{
		_navigationHandler = navigationHandler;
		_getMapBarShortcuts = getMapBarShortcuts;
		_viewDataTracker = Campaign.Current.GetCampaignBehavior<IViewDataTracker>();
		NavigationItems = new MBBindingList<MapNavigationItemVM>();
		INavigationElement[] elements = navigationHandler.GetElements();
		for (int i = 0; i < elements.Length; i++)
		{
			NavigationItems.Add(new MapNavigationItemVM(elements[i]));
		}
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		_shortcuts = _getMapBarShortcuts();
		EncyclopediaHint = new HintViewModel(GameTexts.FindText("str_encyclopedia"));
		CampHint = new HintViewModel(GameTexts.FindText("str_camp"));
		FinanceHint = new HintViewModel(GameTexts.FindText("str_finance"));
		CenterCameraHint = new HintViewModel(GameTexts.FindText("str_return_to_hero"));
		Refresh();
		NavigationItems.ApplyActionOnAllItems(delegate(MapNavigationItemVM n)
		{
			n.RefreshValues();
		});
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		_navigationHandler = null;
		_getMapBarShortcuts = null;
		NavigationItems.ApplyActionOnAllItems(delegate(MapNavigationItemVM n)
		{
			n.OnFinalize();
		});
	}

	public void Refresh()
	{
		RefreshStates();
		_viewDataTracker.UpdatePartyNotification();
	}

	public void Tick()
	{
		RefreshStates();
	}

	protected virtual void RefreshStates()
	{
		NavigationItems.ApplyActionOnAllItems(delegate(MapNavigationItemVM n)
		{
			n.RefreshStates();
		});
	}

	public void ExecuteOpenQuests()
	{
		_navigationHandler.OpenQuests();
	}

	public void ExecuteOpenInventory()
	{
		_navigationHandler.OpenInventory();
	}

	public void ExecuteOpenParty()
	{
		_navigationHandler.OpenParty();
	}

	public void ExecuteOpenCharacterDeveloper()
	{
		_navigationHandler.OpenCharacterDeveloper();
	}

	public void ExecuteOpenKingdom()
	{
		_navigationHandler.OpenKingdom();
	}

	public void ExecuteOpenClan()
	{
		_navigationHandler.OpenClan();
	}

	public void ExecuteOpenEscapeMenu()
	{
		_navigationHandler.OpenEscapeMenu();
	}

	public void ExecuteOpenMainHeroKingdomEncyclopedia()
	{
		if (Hero.MainHero.MapFaction != null)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(Hero.MainHero.MapFaction.EncyclopediaLink);
		}
	}
}
