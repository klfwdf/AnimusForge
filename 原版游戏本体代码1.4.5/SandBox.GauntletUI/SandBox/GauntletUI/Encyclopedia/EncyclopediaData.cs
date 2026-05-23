using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.Encyclopedia.Pages;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Encyclopedia;

public class EncyclopediaData
{
	private Dictionary<string, EncyclopediaPage> _pages;

	private string _previousPageID;

	private EncyclopediaHomeVM _homeDatasource;

	private GauntletMovieIdentifier _homeGauntletMovie;

	private Dictionary<EncyclopediaPage, EncyclopediaListVM> _lists;

	private EncyclopediaPageVM _activeDatasource;

	private GauntletLayer _activeGauntletLayer;

	private GauntletMovieIdentifier _activeGauntletMovie;

	private EncyclopediaNavigatorVM _navigatorDatasource;

	private GauntletMovieIdentifier _navigatorActiveGauntletMovie;

	private readonly ScreenBase _screen;

	private TutorialContexts _prevContext;

	private readonly GauntletMapEncyclopediaView _manager;

	private object _initialState;

	public EncyclopediaData(GauntletMapEncyclopediaView manager, ScreenBase screen, EncyclopediaHomeVM homeDatasource, EncyclopediaNavigatorVM navigatorDatasource)
	{
		_manager = manager;
		_screen = screen;
		_pages = new Dictionary<string, EncyclopediaPage>();
		foreach (EncyclopediaPage encyclopediaPage in Campaign.Current.EncyclopediaManager.GetEncyclopediaPages())
		{
			string[] identifierNames = encyclopediaPage.GetIdentifierNames();
			foreach (string key in identifierNames)
			{
				if (!_pages.ContainsKey(key))
				{
					_pages.Add(key, encyclopediaPage);
				}
			}
		}
		_homeDatasource = homeDatasource;
		_lists = new Dictionary<EncyclopediaPage, EncyclopediaListVM>();
		foreach (EncyclopediaPage encyclopediaPage2 in Campaign.Current.EncyclopediaManager.GetEncyclopediaPages())
		{
			if (!_lists.ContainsKey(encyclopediaPage2))
			{
				EncyclopediaListVM encyclopediaListVM = new EncyclopediaListVM(new EncyclopediaPageArgs(encyclopediaPage2));
				_manager.ListViewDataController.LoadListData(encyclopediaListVM);
				_lists.Add(encyclopediaPage2, encyclopediaListVM);
			}
		}
		_navigatorDatasource = navigatorDatasource;
		_navigatorDatasource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_navigatorDatasource.SetPreviousPageInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToPreviousTab"));
		_navigatorDatasource.SetNextPageInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToNextTab"));
		Game.Current.EventManager.RegisterEvent<TutorialContextChangedEvent>(OnTutorialContextChanged);
	}

	private void OnTutorialContextChanged(TutorialContextChangedEvent obj)
	{
		if (obj.NewContext != TutorialContexts.EncyclopediaWindow)
		{
			_prevContext = obj.NewContext;
		}
	}

	internal void OnTick()
	{
		_navigatorDatasource.CanSwitchTabs = !Input.IsGamepadActive || !InformationManager.GetIsAnyTooltipActiveAndExtended();
		if (_activeGauntletLayer.Input.IsHotKeyReleased("Exit") || (_activeGauntletLayer.Input.IsGameKeyReleased(39) && !_activeGauntletLayer.IsFocusedOnInput()))
		{
			if (_navigatorDatasource.IsSearchResultsShown)
			{
				_navigatorDatasource.SearchText = string.Empty;
			}
			else
			{
				_manager.CloseEncyclopedia();
				UISoundsHelper.PlayUISound("event:/ui/default");
			}
		}
		else if (!_activeGauntletLayer.IsFocusedOnInput() && _navigatorDatasource.CanSwitchTabs)
		{
			if ((Input.IsKeyPressed(InputKey.BackSpace) && _navigatorDatasource.IsBackEnabled) || _activeGauntletLayer.Input.IsHotKeyReleased("SwitchToPreviousTab"))
			{
				_navigatorDatasource.ExecuteBack();
			}
			else if (_activeGauntletLayer.Input.IsHotKeyReleased("SwitchToNextTab"))
			{
				_navigatorDatasource.ExecuteForward();
			}
		}
		if (_activeGauntletLayer != null && _initialState != Game.Current?.GameStateManager?.ActiveState)
		{
			_manager.CloseEncyclopedia();
		}
		_activeDatasource?.OnTick();
	}

	private void SetEncyclopediaPage(string pageId, object obj)
	{
		GauntletLayer activeGauntletLayer = _activeGauntletLayer;
		if (_activeGauntletLayer != null && _activeGauntletMovie != null)
		{
			_activeGauntletLayer.ReleaseMovie(_activeGauntletMovie);
		}
		if (_activeDatasource is EncyclopediaListVM encyclopediaListVM)
		{
			EncyclopediaListItemVM encyclopediaListItemVM = encyclopediaListVM.Items.FirstOrDefault((EncyclopediaListItemVM x) => x.Object == obj);
			_manager.ListViewDataController.SaveListData(encyclopediaListVM, (encyclopediaListItemVM != null) ? encyclopediaListItemVM.Id : encyclopediaListVM.LastSelectedItemId);
		}
		if (_activeGauntletLayer == null)
		{
			_activeGauntletLayer = new GauntletLayer("EncyclopediaBar", 310);
			_navigatorActiveGauntletMovie = _activeGauntletLayer.LoadMovie("EncyclopediaBar", _navigatorDatasource);
			_navigatorDatasource.PageName = _homeDatasource.GetName();
			_activeGauntletLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(_activeGauntletLayer);
			_activeGauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			_activeGauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
			Game.Current.GameStateManager.RegisterActiveStateDisableRequest(this);
			Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.EncyclopediaWindow));
			_initialState = Game.Current.GameStateManager.ActiveState;
		}
		if (pageId == "Home")
		{
			_activeGauntletMovie = _activeGauntletLayer.LoadMovie("EncyclopediaHome", _homeDatasource);
			_homeGauntletMovie = _activeGauntletMovie;
			_activeDatasource = _homeDatasource;
			_activeDatasource.Refresh();
			Game.Current.EventManager.TriggerEvent(new EncyclopediaPageChangedEvent(EncyclopediaPages.Home));
		}
		else if (pageId == "ListPage")
		{
			EncyclopediaPage encyclopediaPage = obj as EncyclopediaPage;
			_activeDatasource = _lists[encyclopediaPage];
			_activeGauntletMovie = _activeGauntletLayer.LoadMovie("EncyclopediaItemList", _activeDatasource);
			_activeDatasource.Refresh();
			_manager.ListViewDataController.LoadListData(_activeDatasource as EncyclopediaListVM);
			SetTutorialListPageContext(encyclopediaPage);
		}
		else
		{
			EncyclopediaPage encyclopediaPage2 = _pages[pageId];
			_activeDatasource = GetEncyclopediaPageInstance(encyclopediaPage2, obj);
			(_activeDatasource as EncyclopediaContentPageVM)?.InitializeQuickNavigation(_lists[encyclopediaPage2]);
			_activeGauntletMovie = _activeGauntletLayer.LoadMovie(_pages[pageId].GetViewFullyQualifiedName(), _activeDatasource);
			SetTutorialPageContext(_activeDatasource);
		}
		_navigatorDatasource.NavBarString = _activeDatasource.GetNavigationBarURL();
		if (activeGauntletLayer != null && activeGauntletLayer != _activeGauntletLayer)
		{
			_screen.RemoveLayer(activeGauntletLayer);
			_screen.AddLayer(_activeGauntletLayer);
		}
		else if (activeGauntletLayer == null && _activeGauntletLayer != null)
		{
			_screen.AddLayer(_activeGauntletLayer);
		}
		_activeGauntletLayer.InputRestrictions.SetInputRestrictions();
		_previousPageID = pageId;
	}

	internal EncyclopediaPageVM ExecuteLink(string pageId, object obj, bool needsRefresh)
	{
		SetEncyclopediaPage(pageId, obj);
		return _activeDatasource;
	}

	private EncyclopediaPageVM GetEncyclopediaPageInstance(EncyclopediaPage page, object o)
	{
		Type type = null;
		EncyclopediaPageArgs encyclopediaPageArgs = new EncyclopediaPageArgs(o);
		Assembly assembly = typeof(EncyclopediaHomeVM).Assembly;
		Type[] types = assembly.GetTypes();
		foreach (Type type2 in types)
		{
			if (IsEncyclopediaPageType(page, type2))
			{
				type = type2;
			}
		}
		Assembly[] activeReferencingGameAssembliesSafe = assembly.GetActiveReferencingGameAssembliesSafe();
		for (int j = 0; j < activeReferencingGameAssembliesSafe.Length; j++)
		{
			List<Type> typesSafe = activeReferencingGameAssembliesSafe[j].GetTypesSafe();
			for (int k = 0; k < typesSafe.Count; k++)
			{
				Type type3 = typesSafe[k];
				if (IsEncyclopediaPageType(page, type3))
				{
					type = type3;
				}
			}
		}
		if (type != null)
		{
			return Activator.CreateInstance(type, encyclopediaPageArgs) as EncyclopediaPageVM;
		}
		return null;
	}

	private static bool IsEncyclopediaPageType(EncyclopediaPage page, Type type)
	{
		if (typeof(EncyclopediaPageVM).IsAssignableFrom(type))
		{
			object[] customAttributesSafe = type.GetCustomAttributesSafe(typeof(EncyclopediaViewModel), inherit: false);
			for (int i = 0; i < customAttributesSafe.Length; i++)
			{
				if (customAttributesSafe[i] is EncyclopediaViewModel encyclopediaViewModel && page.HasIdentifierType(encyclopediaViewModel.PageTargetType))
				{
					return true;
				}
			}
		}
		return false;
	}

	public void OnFinalize()
	{
		Game.Current.GameStateManager.UnregisterActiveStateDisableRequest(this);
		_pages = null;
		_homeDatasource = null;
		foreach (KeyValuePair<EncyclopediaPage, EncyclopediaListVM> list in _lists)
		{
			list.Value?.OnFinalize();
		}
		_lists = null;
		_activeGauntletMovie = null;
		_activeDatasource = null;
		_activeGauntletLayer = null;
		_navigatorActiveGauntletMovie = null;
		_navigatorDatasource = null;
		_initialState = null;
		Game.Current.EventManager.UnregisterEvent<TutorialContextChangedEvent>(OnTutorialContextChanged);
	}

	public void CloseEncyclopedia()
	{
		if (_activeDatasource is EncyclopediaListVM encyclopediaListVM)
		{
			_manager.ListViewDataController.SaveListData(encyclopediaListVM, encyclopediaListVM.LastSelectedItemId);
		}
		ResetPageFilters();
		_activeGauntletLayer.ReleaseMovie(_activeGauntletMovie);
		_screen.RemoveLayer(_activeGauntletLayer);
		_activeGauntletLayer.InputRestrictions.ResetInputRestrictions();
		OnFinalize();
		Game.Current.EventManager.TriggerEvent(new EncyclopediaPageChangedEvent(EncyclopediaPages.None));
		Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(_prevContext));
	}

	private void ResetPageFilters()
	{
		foreach (EncyclopediaListVM value in _lists.Values)
		{
			foreach (EncyclopediaFilterGroupVM filterGroup in value.FilterGroups)
			{
				foreach (EncyclopediaListFilterVM filter in filterGroup.Filters)
				{
					filter.IsSelected = false;
				}
			}
		}
	}

	private void SetTutorialPageContext(EncyclopediaPageVM _page)
	{
		if (_page is EncyclopediaClanPageVM)
		{
			Game.Current.EventManager.TriggerEvent(new EncyclopediaPageChangedEvent(EncyclopediaPages.Clan));
		}
		else if (_page is EncyclopediaConceptPageVM)
		{
			Game.Current.EventManager.TriggerEvent(new EncyclopediaPageChangedEvent(EncyclopediaPages.Concept));
		}
		else if (_page is EncyclopediaFactionPageVM)
		{
			Game.Current.EventManager.TriggerEvent(new EncyclopediaPageChangedEvent(EncyclopediaPages.Kingdom));
		}
		else if (_page is EncyclopediaUnitPageVM)
		{
			Game.Current.EventManager.TriggerEvent(new EncyclopediaPageChangedEvent(EncyclopediaPages.Unit));
		}
		else if (_page is EncyclopediaHeroPageVM)
		{
			Game.Current.EventManager.TriggerEvent(new EncyclopediaPageChangedEvent(EncyclopediaPages.Hero));
		}
		else if (_page is EncyclopediaShipPageVM)
		{
			Game.Current.EventManager.TriggerEvent(new EncyclopediaPageChangedEvent(EncyclopediaPages.Ship));
		}
		else if (_page is EncyclopediaSettlementPageVM)
		{
			Game.Current.EventManager.TriggerEvent(new EncyclopediaPageChangedEvent(EncyclopediaPages.Settlement));
		}
	}

	private void SetTutorialListPageContext(EncyclopediaPage _page)
	{
		if (_page is DefaultEncyclopediaClanPage)
		{
			Game.Current.EventManager.TriggerEvent(new EncyclopediaPageChangedEvent(EncyclopediaPages.ListClans));
		}
		else if (_page is DefaultEncyclopediaConceptPage)
		{
			Game.Current.EventManager.TriggerEvent(new EncyclopediaPageChangedEvent(EncyclopediaPages.ListConcepts));
		}
		else if (_page is DefaultEncyclopediaFactionPage)
		{
			Game.Current.EventManager.TriggerEvent(new EncyclopediaPageChangedEvent(EncyclopediaPages.ListKingdoms));
		}
		else if (_page is DefaultEncyclopediaUnitPage)
		{
			Game.Current.EventManager.TriggerEvent(new EncyclopediaPageChangedEvent(EncyclopediaPages.ListUnits));
		}
		else if (_page is DefaultEncyclopediaHeroPage)
		{
			Game.Current.EventManager.TriggerEvent(new EncyclopediaPageChangedEvent(EncyclopediaPages.ListHeroes));
		}
		else if (_page is DefaultEncyclopediaShipPage)
		{
			Game.Current.EventManager.TriggerEvent(new EncyclopediaPageChangedEvent(EncyclopediaPages.ListShips));
		}
		else if (_page is DefaultEncyclopediaSettlementPage)
		{
			Game.Current.EventManager.TriggerEvent(new EncyclopediaPageChangedEvent(EncyclopediaPages.ListSettlements));
		}
	}
}
