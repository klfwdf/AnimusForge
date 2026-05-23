using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI.Map;

public class GauntletMapBarGlobalLayer : GlobalLayer
{
	protected MapBarVM _dataSource;

	protected GauntletLayer _gauntletLayer;

	protected GauntletMovieIdentifier _movie;

	protected SpriteCategory _mapBarCategory;

	protected MapScreen _mapScreen;

	protected INavigationHandler _mapNavigationHandler;

	protected MapEncyclopediaView _encyclopediaManager;

	protected float _contextAlphaTarget = 1f;

	protected float _contextAlphaModifider;

	private GauntletLayer _armyManagementLayer;

	private SpriteCategory _armyManagementCategory;

	private ArmyManagementVM _armyManagementVM;

	private GauntletMovieIdentifier _gauntletArmyManagementMovie;

	private CampaignTimeControlMode _timeControlModeBeforeArmyManagementOpened;

	public bool IsInArmyManagement
	{
		get
		{
			if (_armyManagementLayer != null)
			{
				return _armyManagementVM != null;
			}
			return false;
		}
	}

	public GauntletMapBarGlobalLayer(MapScreen mapScreen, INavigationHandler navigationHandler, float contextAlphaModifider)
	{
		_mapScreen = mapScreen;
		_mapNavigationHandler = navigationHandler;
		_contextAlphaModifider = contextAlphaModifider;
		_mapScreen.NavigationHandler = navigationHandler;
	}

	public void Initialize(MapBarVM dataSource)
	{
		_dataSource = dataSource;
		_dataSource.Initialize(_mapNavigationHandler, _mapScreen, GetMapBarShortcuts, OpenArmyManagement);
		_gauntletLayer = new GauntletLayer("MapBar", 202);
		base.Layer = _gauntletLayer;
		_mapBarCategory = UIResourceManager.LoadSpriteCategory("ui_mapbar");
		_movie = _gauntletLayer.LoadMovie("MapBar", _dataSource);
		_encyclopediaManager = _mapScreen.EncyclopediaScreenManager;
	}

	public void OnFinalize()
	{
		if (_gauntletLayer != null)
		{
			if (_gauntletArmyManagementMovie != null)
			{
				_gauntletLayer.ReleaseMovie(_gauntletArmyManagementMovie);
			}
			if (_movie != null)
			{
				_gauntletLayer.ReleaseMovie(_movie);
			}
		}
		_armyManagementVM?.OnFinalize();
		_dataSource.OnFinalize();
		_mapBarCategory.Unload();
		_armyManagementVM = null;
		_gauntletLayer = null;
		_dataSource = null;
		_encyclopediaManager = null;
		_mapScreen = null;
	}

	public void OnMapConversationStarted()
	{
		if (_gauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: true);
		}
	}

	public void OnMapConversationOver()
	{
		if (_gauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: false);
		}
	}

	public void Refresh()
	{
		_dataSource?.OnRefresh();
	}

	private MapBarShortcuts GetMapBarShortcuts()
	{
		return new MapBarShortcuts
		{
			FastForwardHotkey = Game.Current.GameTextManager.GetHotKeyGameText("MapHotKeyCategory", 62).ToString(),
			PauseHotkey = Game.Current.GameTextManager.GetHotKeyGameText("MapHotKeyCategory", 60).ToString(),
			PlayHotkey = Game.Current.GameTextManager.GetHotKeyGameText("MapHotKeyCategory", 61).ToString()
		};
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		_gauntletLayer.UIContext.ContextAlpha = MathF.Lerp(_gauntletLayer.UIContext.ContextAlpha, _contextAlphaTarget, dt * _contextAlphaModifider);
		GameState gameState = Game.Current?.GameStateManager.ActiveState;
		ScreenBase topScreen = ScreenManager.TopScreen;
		bool flag = _mapNavigationHandler?.IsAnyElementActive() ?? false;
		if (topScreen is MapScreen || flag)
		{
			_dataSource.IsEnabled = true;
			_dataSource.CurrentScreen = topScreen.GetType().Name;
			bool flag2 = ScreenManager.TopScreen is MapScreen;
			_dataSource.MapTimeControl.IsInMap = flag2;
			base.Layer.InputRestrictions.SetInputRestrictions(isMouseVisible: false);
			if (!(gameState is MapState))
			{
				_dataSource.MapTimeControl.IsCenterPanelEnabled = false;
				if (flag)
				{
					HandlePanelSwitching();
				}
				_contextAlphaTarget = 1f;
			}
			else
			{
				_ = (MapState)gameState;
				if (flag2)
				{
					MapScreen mapScreen = ScreenManager.TopScreen as MapScreen;
					mapScreen.SetIsBarExtended(_dataSource.MapInfo.IsInfoBarExtended);
					_dataSource.MapTimeControl.IsInRecruitment = mapScreen.IsInRecruitment;
					_dataSource.MapTimeControl.IsInBattleSimulation = mapScreen.IsInBattleSimulation;
					_dataSource.MapTimeControl.IsEncyclopediaOpen = _encyclopediaManager?.IsEncyclopediaOpen ?? false;
					_dataSource.MapTimeControl.IsInArmyManagement = mapScreen.IsInArmyManagement;
					_dataSource.MapTimeControl.IsInTownManagement = mapScreen.IsInTownManagement;
					_dataSource.MapTimeControl.IsInHideoutTroopManage = mapScreen.IsInHideoutTroopManage;
					_dataSource.MapTimeControl.IsInCampaignOptions = mapScreen.IsInCampaignOptions;
					_dataSource.MapTimeControl.IsEscapeMenuOpened = mapScreen.IsEscapeMenuOpened;
					_dataSource.MapTimeControl.IsMarriageOfferPopupActive = mapScreen.IsMarriageOfferPopupActive;
					_dataSource.MapTimeControl.IsHeirSelectionPopupActive = mapScreen.IsHeirSelectionPopupActive;
					_dataSource.MapTimeControl.IsMapCheatsActive = mapScreen.IsMapCheatsActive;
					_dataSource.MapTimeControl.IsMapIncidentActive = mapScreen.IsMapIncidentActive;
					_dataSource.MapTimeControl.IsOverlayContextMenuEnabled = mapScreen.IsOverlayContextMenuEnabled;
					if (mapScreen.GetMapView<MapConversationView>()?.ConversationMission != null)
					{
						_contextAlphaTarget = 0f;
					}
					else
					{
						_contextAlphaTarget = 1f;
					}
					if (_armyManagementVM != null)
					{
						HandleArmyManagementInput();
					}
				}
				else
				{
					_dataSource.MapTimeControl.IsCenterPanelEnabled = false;
				}
			}
			_dataSource.Tick(dt);
		}
		else
		{
			_dataSource.IsEnabled = false;
			base.Layer.InputRestrictions.ResetInputRestrictions();
		}
	}

	private void HandleArmyManagementInput()
	{
		if (_armyManagementLayer.Input.IsHotKeyReleased("Exit"))
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_armyManagementVM.ExecuteCancel();
		}
		else if (_armyManagementLayer.Input.IsHotKeyReleased("Confirm"))
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_armyManagementVM.ExecuteDone();
		}
		else if (_armyManagementLayer.Input.IsHotKeyReleased("Reset"))
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_armyManagementVM.ExecuteReset();
		}
		else if (_armyManagementLayer.Input.IsHotKeyReleased("RemoveParty") && _armyManagementVM.FocusedItem != null)
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_armyManagementVM.FocusedItem.ExecuteAction();
		}
	}

	private void HandlePanelSwitching()
	{
		GauntletLayer gauntletLayer = ScreenManager.TopScreen.FindLayer<GauntletLayer>();
		SceneLayer sceneLayer = ScreenManager.TopScreen.FindLayer<SceneLayer>();
		if (gauntletLayer?.Input != null && !gauntletLayer.IsFocusedOnInput() && ScreenManager.FocusedLayer == gauntletLayer)
		{
			HandlePanelSwitchingInput(gauntletLayer.Input);
		}
		else if (sceneLayer?.Input != null && ScreenManager.FocusedLayer == sceneLayer)
		{
			HandlePanelSwitchingInput(sceneLayer.Input);
		}
	}

	protected virtual bool HandlePanelSwitchingInput(InputContext inputContext)
	{
		if (inputContext.IsGameKeyReleased(37) && !_mapNavigationHandler.IsActive(MapNavigationItemType.CharacterDeveloper))
		{
			_mapNavigationHandler?.OpenCharacterDeveloper();
			return true;
		}
		if (inputContext.IsGameKeyReleased(43) && !_mapNavigationHandler.IsActive(MapNavigationItemType.Party))
		{
			_mapNavigationHandler?.OpenParty();
			return true;
		}
		if (inputContext.IsGameKeyReleased(42) && !_mapNavigationHandler.IsActive(MapNavigationItemType.Quest))
		{
			_mapNavigationHandler?.OpenQuests();
			return true;
		}
		if (inputContext.IsGameKeyReleased(38) && !_mapNavigationHandler.IsActive(MapNavigationItemType.Inventory))
		{
			_mapNavigationHandler?.OpenInventory();
			return true;
		}
		if (inputContext.IsGameKeyReleased(41) && !_mapNavigationHandler.IsActive(MapNavigationItemType.Clan))
		{
			_mapNavigationHandler?.OpenClan();
			return true;
		}
		if (inputContext.IsGameKeyReleased(40) && !_mapNavigationHandler.IsActive(MapNavigationItemType.Kingdom))
		{
			_mapNavigationHandler?.OpenKingdom();
			return true;
		}
		return false;
	}

	private void OpenArmyManagement()
	{
		if (_gauntletLayer != null)
		{
			_armyManagementLayer = new GauntletLayer("MapBar_ArmyManagement", 300);
			_armyManagementCategory = UIResourceManager.LoadSpriteCategory("ui_armymanagement");
			_armyManagementVM = new ArmyManagementVM(CloseArmyManagement);
			_gauntletArmyManagementMovie = _armyManagementLayer.LoadMovie("ArmyManagement", _armyManagementVM);
			_armyManagementLayer.InputRestrictions.SetInputRestrictions();
			_armyManagementLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("Generic"));
			_armyManagementLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			_armyManagementLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
			_armyManagementLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("ArmyManagementHotkeyCategory"));
			_armyManagementLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(_armyManagementLayer);
			_mapScreen.AddLayer(_armyManagementLayer);
			_armyManagementVM.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
			_armyManagementVM.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
			_armyManagementVM.SetResetInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Reset"));
			_armyManagementVM.SetRemoveInputKey(HotKeyManager.GetCategory("ArmyManagementHotkeyCategory").GetHotKey("RemoveParty"));
			_timeControlModeBeforeArmyManagementOpened = Campaign.Current.TimeControlMode;
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			Campaign.Current.SetTimeControlModeLock(isLocked: true);
			if (ScreenManager.TopScreen is MapScreen mapScreen)
			{
				mapScreen.SetIsInArmyManagement(isInArmyManagement: true);
			}
		}
	}

	private void CloseArmyManagement()
	{
		_armyManagementVM.OnFinalize();
		_armyManagementLayer.ReleaseMovie(_gauntletArmyManagementMovie);
		_mapScreen.RemoveLayer(_armyManagementLayer);
		_armyManagementCategory.Unload();
		Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.MapWindow));
		_gauntletArmyManagementMovie = null;
		_armyManagementVM = null;
		_armyManagementLayer = null;
		Campaign.Current.SetTimeControlModeLock(isLocked: false);
		Campaign.Current.TimeControlMode = _timeControlModeBeforeArmyManagementOpened;
		if (ScreenManager.TopScreen is MapScreen mapScreen)
		{
			mapScreen.SetIsInArmyManagement(isInArmyManagement: false);
		}
	}

	public bool IsEscaped()
	{
		if (_armyManagementVM != null)
		{
			_armyManagementVM.ExecuteCancel();
			return true;
		}
		return false;
	}
}
