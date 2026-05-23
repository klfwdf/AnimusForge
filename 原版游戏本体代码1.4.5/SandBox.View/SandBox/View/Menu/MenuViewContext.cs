using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.ScreenSystem;

namespace SandBox.View.Menu;

public class MenuViewContext : IMenuContextHandler
{
	private MenuContext _menuContext;

	private MenuView _currentMenuBase;

	private MenuView _currentMenuBackground;

	private MenuView _menuCharacterDeveloper;

	private MenuView _menuOverlayBase;

	private MenuView _menuRecruitVolunteers;

	private MenuView _menuTournamentLeaderboard;

	private MenuView _menuTroopSelection;

	private MenuView _menuTownManagement;

	private SoundEvent _panelSound;

	private SoundEvent _ambientSound;

	private GameMenu.MenuOverlayType _currentOverlayType;

	private ScreenBase _screen;

	private bool _isActive;

	internal GameMenu CurGameMenu => _menuContext.GameMenu;

	public MenuContext MenuContext => _menuContext;

	public List<MenuView> MenuViews { get; private set; }

	public MenuViewContext(ScreenBase screen, MenuContext menuContext)
	{
		_screen = screen;
		_menuContext = menuContext;
		MenuViews = new List<MenuView>();
		_menuContext.Handler = this;
		if (Campaign.Current.GameMode != CampaignGameMode.Tutorial && CurGameMenu.StringId != "siege_test_menu")
		{
			((IMenuContextHandler)this).OnMenuCreate();
			((IMenuContextHandler)this).OnMenuActivate();
		}
	}

	public void UpdateMenuContext(MenuContext menuContext)
	{
		_menuContext = menuContext;
		_menuContext.Handler = this;
		MenuViews.ForEach(delegate(MenuView m)
		{
			m.MenuContext = menuContext;
		});
		MenuViews.ForEach(delegate(MenuView m)
		{
			m.OnMenuContextUpdated(menuContext);
		});
		CheckAndInitializeOverlay();
	}

	public void AddLayer(ScreenLayer layer)
	{
		_screen.AddLayer(layer);
	}

	public void RemoveLayer(ScreenLayer layer)
	{
		_screen.RemoveLayer(layer);
	}

	public T FindLayer<T>() where T : ScreenLayer
	{
		return _screen.FindLayer<T>();
	}

	public T FindLayer<T>(string name) where T : ScreenLayer
	{
		return _screen.FindLayer<T>(name);
	}

	public void OnFrameTick(float dt)
	{
		for (int i = 0; i < MenuViews.Count; i++)
		{
			MenuView menuView = MenuViews[i];
			menuView.OnFrameTick(dt);
			if (menuView.Removed)
			{
				i--;
			}
		}
	}

	public void OnResume()
	{
		_isActive = true;
		for (int i = 0; i < MenuViews.Count; i++)
		{
			MenuViews[i].OnResume();
		}
	}

	public void OnHourlyTick()
	{
		for (int i = 0; i < MenuViews.Count; i++)
		{
			MenuViews[i].OnHourlyTick();
		}
	}

	public void OnActivate()
	{
		_isActive = true;
		for (int i = 0; i < MenuViews.Count; i++)
		{
			MenuViews[i].OnActivate();
		}
		if (!string.IsNullOrEmpty(MenuContext?.CurrentAmbientSoundID))
		{
			PlayAmbientSound(MenuContext.CurrentAmbientSoundID);
		}
		if (!string.IsNullOrEmpty(MenuContext?.CurrentPanelSoundID))
		{
			PlayPanelSound(MenuContext.CurrentPanelSoundID);
		}
	}

	public void OnDeactivate()
	{
		_isActive = false;
		for (int i = 0; i < MenuViews.Count; i++)
		{
			MenuViews[i].OnDeactivate();
		}
		StopAllSounds();
	}

	public void OnInitialize()
	{
	}

	public void OnFinalize()
	{
		ClearMenuViews();
		MBInformationManager.HideInformations();
		_menuContext = null;
	}

	private void ClearMenuViews()
	{
		MenuView[] array = MenuViews.ToArray();
		foreach (MenuView menuView in array)
		{
			RemoveMenuView(menuView);
		}
		_menuCharacterDeveloper = null;
		_menuOverlayBase = null;
		_menuRecruitVolunteers = null;
		_menuTownManagement = null;
		_menuTroopSelection = null;
	}

	public void StopAllSounds()
	{
		_ambientSound?.Release();
		_panelSound?.Release();
	}

	private void PlayAmbientSound(string ambientSoundID)
	{
		if (_isActive)
		{
			_ambientSound?.Release();
			_ambientSound = SoundEvent.CreateEventFromString(ambientSoundID, null);
			_ambientSound.Play();
		}
	}

	private void PlayPanelSound(string panelSoundID)
	{
		if (_isActive)
		{
			_panelSound?.Release();
			_panelSound = SoundEvent.CreateEventFromString(panelSoundID, null);
			_panelSound.Play();
		}
	}

	public void OnMapConversationActivated()
	{
		for (int i = 0; i < MenuViews.Count; i++)
		{
			MenuView menuView = MenuViews[i];
			menuView.OnMapConversationActivated();
			if (menuView.Removed)
			{
				i--;
			}
		}
	}

	public void OnMapConversationDeactivated()
	{
		for (int i = 0; i < MenuViews.Count; i++)
		{
			MenuView menuView = MenuViews[i];
			menuView.OnMapConversationDeactivated();
			if (menuView.Removed)
			{
				i--;
			}
		}
	}

	public void OnGameStateDeactivate()
	{
	}

	public void OnGameStateInitialize()
	{
	}

	public void OnGameStateFinalize()
	{
	}

	private void CheckAndInitializeOverlay()
	{
		GameMenu.MenuOverlayType menuOverlayType = Campaign.Current.GameMenuManager.GetMenuOverlayType(_menuContext);
		if (menuOverlayType != GameMenu.MenuOverlayType.None)
		{
			if (menuOverlayType != _currentOverlayType)
			{
				if (_menuOverlayBase != null && ((_currentOverlayType != GameMenu.MenuOverlayType.Encounter && menuOverlayType == GameMenu.MenuOverlayType.Encounter) || (_currentOverlayType == GameMenu.MenuOverlayType.Encounter && (menuOverlayType == GameMenu.MenuOverlayType.SettlementWithBoth || menuOverlayType == GameMenu.MenuOverlayType.SettlementWithCharacters || menuOverlayType == GameMenu.MenuOverlayType.SettlementWithParties))))
				{
					RemoveMenuView(_menuOverlayBase);
					_menuOverlayBase = null;
				}
				if (_menuOverlayBase == null)
				{
					_menuOverlayBase = AddMenuView<MenuOverlayBaseView>(Array.Empty<object>());
				}
				else
				{
					_menuOverlayBase.OnOverlayTypeChange(menuOverlayType);
				}
			}
			else
			{
				_menuOverlayBase?.OnOverlayTypeChange(menuOverlayType);
			}
		}
		else
		{
			if (_menuOverlayBase != null)
			{
				RemoveMenuView(_menuOverlayBase);
				_menuOverlayBase = null;
			}
			if (_currentMenuBackground != null)
			{
				RemoveMenuView(_currentMenuBackground);
				_currentMenuBackground = null;
			}
		}
		_currentOverlayType = menuOverlayType;
	}

	public void CloseCharacterDeveloper()
	{
		RemoveMenuView(_menuCharacterDeveloper);
		_menuCharacterDeveloper = null;
		foreach (MenuView menuView in MenuViews)
		{
			menuView.OnCharacterDeveloperClosed();
		}
	}

	public MenuView AddMenuView<T>(params object[] parameters) where T : MenuView, new()
	{
		MenuView menuView = SandBoxViewCreator.CreateMenuView<T>(parameters);
		menuView.MenuViewContext = this;
		menuView.MenuContext = _menuContext;
		MenuViews.Add(menuView);
		menuView.OnInitialize();
		return menuView;
	}

	public T GetMenuView<T>() where T : MenuView
	{
		foreach (MenuView menuView in MenuViews)
		{
			if (menuView is T result)
			{
				return result;
			}
		}
		return null;
	}

	public void RemoveMenuView(MenuView menuView)
	{
		menuView.OnFinalize();
		menuView.Removed = true;
		MenuViews.Remove(menuView);
		if (menuView.ShouldUpdateMenuAfterRemoved)
		{
			MenuViews.ForEach(delegate(MenuView m)
			{
				m.OnMenuContextUpdated(_menuContext);
			});
		}
	}

	public void CloseTownManagement()
	{
		RemoveMenuView(_menuTownManagement);
		_menuTownManagement = null;
	}

	public void CloseRecruitVolunteers()
	{
		RemoveMenuView(_menuRecruitVolunteers);
		_menuRecruitVolunteers = null;
	}

	public void CloseTournamentLeaderboard()
	{
		RemoveMenuView(_menuTournamentLeaderboard);
		_menuTournamentLeaderboard = null;
	}

	public void CloseTroopSelection()
	{
		RemoveMenuView(_menuTroopSelection);
		_menuTroopSelection = null;
	}

	protected virtual MenuView CreateTroopSelectionView(TroopRoster fullRoster, TroopRoster initialSelections, List<Ship> eligibleShips, Func<CharacterObject, bool> canChangeStatusOfTroop, Action<TroopRoster> onDone, int maxSelectableTroopCount, int minSelectableTroopCount, bool isNavalRaid)
	{
		return AddMenuView<MenuTroopSelectionView>(new object[6] { fullRoster, initialSelections, canChangeStatusOfTroop, onDone, maxSelectableTroopCount, minSelectableTroopCount });
	}

	void IMenuContextHandler.OnAmbientSoundIDSet(string ambientSoundID)
	{
		PlayAmbientSound(ambientSoundID);
	}

	void IMenuContextHandler.OnPanelSoundIDSet(string panelSoundID)
	{
		PlayPanelSound(panelSoundID);
	}

	void IMenuContextHandler.OnMenuCreate()
	{
		int num;
		if (Campaign.Current.GameMode != CampaignGameMode.Tutorial)
		{
			num = ((CurGameMenu.StringId == "siege_test_menu") ? 1 : 0);
			if (num == 0)
			{
				goto IL_0041;
			}
		}
		else
		{
			num = 1;
		}
		if (_currentMenuBackground == null)
		{
			_currentMenuBackground = AddMenuView<MenuBackgroundView>(Array.Empty<object>());
		}
		goto IL_0041;
		IL_0041:
		if (_currentMenuBase == null)
		{
			_currentMenuBase = AddMenuView<MenuBaseView>(Array.Empty<object>());
		}
		if (num == 0)
		{
			CheckAndInitializeOverlay();
		}
		StopAllSounds();
	}

	void IMenuContextHandler.OnMenuActivate()
	{
		foreach (MenuView menuView in MenuViews)
		{
			menuView.OnActivate();
		}
	}

	void IMenuContextHandler.OnBackgroundMeshNameSet(string name)
	{
		foreach (MenuView menuView in MenuViews)
		{
			menuView.OnBackgroundMeshNameSet(name);
		}
	}

	void IMenuContextHandler.OnOpenTownManagement()
	{
		if (_menuTownManagement == null)
		{
			_menuTownManagement = AddMenuView<MenuTownManagementView>(Array.Empty<object>());
		}
	}

	void IMenuContextHandler.OnOpenRecruitVolunteers()
	{
		if (_menuRecruitVolunteers == null)
		{
			_menuRecruitVolunteers = AddMenuView<MenuRecruitVolunteersView>(Array.Empty<object>());
		}
	}

	void IMenuContextHandler.OnOpenTournamentLeaderboard()
	{
		if (_menuTournamentLeaderboard == null)
		{
			_menuTournamentLeaderboard = AddMenuView<MenuTournamentLeaderboardView>(Array.Empty<object>());
		}
	}

	void IMenuContextHandler.OnOpenTroopSelection(TroopRoster fullRoster, TroopRoster initialSelections, List<Ship> eligibleShips, Func<CharacterObject, bool> canChangeStatusOfTroop, Action<TroopRoster> onDone, int maxSelectableTroopCount, int minSelectableTroopCount, bool isNavalRaid)
	{
		if (_menuTroopSelection == null)
		{
			_menuTroopSelection = CreateTroopSelectionView(fullRoster, initialSelections, eligibleShips, canChangeStatusOfTroop, onDone, maxSelectableTroopCount, minSelectableTroopCount, isNavalRaid);
		}
	}

	void IMenuContextHandler.OnMenuRefresh()
	{
		foreach (MenuView menuView in MenuViews)
		{
			menuView.OnMenuContextRefreshed();
		}
	}
}
