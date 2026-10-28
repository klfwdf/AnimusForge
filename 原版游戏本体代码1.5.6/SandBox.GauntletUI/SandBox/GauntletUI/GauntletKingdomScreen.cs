using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions.ItemTypes;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI;

[GameStateScreen(typeof(KingdomState))]
public class GauntletKingdomScreen : ScreenBase, IGameStateListener
{
	private GauntletLayer _gauntletLayer;

	private readonly KingdomState _kingdomState;

	private GauntletLayer _armyManagementLayer;

	private ArmyManagementVM _armyManagementDatasource;

	private SpriteCategory _kingdomCategory;

	private SpriteCategory _armyManagementCategory;

	private SpriteCategory _clanCategory;

	public KingdomManagementVM DataSource { get; private set; }

	public bool IsMakingDecision => DataSource.Decision.IsActive;

	public GauntletKingdomScreen(KingdomState kingdomState)
	{
		_kingdomState = kingdomState;
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		InformationManager.HideAllMessages();
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		LoadingWindow.DisableGlobalLoadingWindow();
		DataSource.CanSwitchTabs = !InformationManager.GetIsAnyTooltipActiveAndExtended();
		if (MapScreen.Instance != null)
		{
			MapScreen.Instance.NavigationHandler.IsNavigationLocked = DataSource.Decision.IsActive;
		}
		if (DataSource.Decision.IsActive)
		{
			if (_gauntletLayer.Input.IsHotKeyReleased("Confirm"))
			{
				DecisionItemBaseVM currentDecision = DataSource.Decision.CurrentDecision;
				if (currentDecision != null && currentDecision.CanEndDecision)
				{
					DataSource.Decision.CurrentDecision.ExecuteFinalSelection();
					UISoundsHelper.PlayUISound("event:/ui/reign/decision");
				}
			}
		}
		else if (DataSource.GiftFief.IsOpen)
		{
			if (_gauntletLayer.Input.IsHotKeyReleased("Confirm"))
			{
				if (DataSource.GiftFief.IsAnyClanSelected)
				{
					DataSource.GiftFief.ExecuteGiftSettlement();
					UISoundsHelper.PlayUISound("event:/ui/default");
				}
			}
			else if (_gauntletLayer.Input.IsHotKeyReleased("Exit"))
			{
				DataSource.GiftFief.ExecuteClose();
				UISoundsHelper.PlayUISound("event:/ui/default");
			}
		}
		else if (_armyManagementDatasource != null)
		{
			if (_armyManagementLayer.Input.IsHotKeyReleased("Exit"))
			{
				_armyManagementDatasource.ExecuteCancel();
				UISoundsHelper.PlayUISound("event:/ui/default");
			}
			else if (_armyManagementLayer.Input.IsHotKeyReleased("Confirm"))
			{
				_armyManagementDatasource.ExecuteDone();
				UISoundsHelper.PlayUISound("event:/ui/default");
			}
			else if (_armyManagementLayer.Input.IsHotKeyReleased("Reset"))
			{
				_armyManagementDatasource.ExecuteReset();
				UISoundsHelper.PlayUISound("event:/ui/default");
			}
			else if (_armyManagementLayer.Input.IsHotKeyReleased("RemoveParty") && _armyManagementDatasource.FocusedItem != null)
			{
				_armyManagementDatasource.FocusedItem.ExecuteAction();
				UISoundsHelper.PlayUISound("event:/ui/default");
			}
		}
		else if (_gauntletLayer.Input.IsHotKeyReleased("Exit") || _gauntletLayer.Input.IsGameKeyPressed(40) || _gauntletLayer.Input.IsHotKeyReleased("Confirm"))
		{
			CloseKingdomScreen();
		}
		else if (DataSource.CanSwitchTabs)
		{
			if (_gauntletLayer.Input.IsHotKeyReleased("SwitchToPreviousTab"))
			{
				DataSource.SelectPreviousCategory();
				UISoundsHelper.PlayUISound("event:/ui/tab");
			}
			else if (_gauntletLayer.Input.IsHotKeyReleased("SwitchToNextTab"))
			{
				DataSource.SelectNextCategory();
				UISoundsHelper.PlayUISound("event:/ui/tab");
			}
		}
		DataSource?.OnFrameTick();
	}

	protected virtual KingdomManagementVM CreateDataSource()
	{
		return new KingdomManagementVM(CloseKingdomScreen, OpenArmyManagement, ShowArmyOnMap);
	}

	void IGameStateListener.OnActivate()
	{
		base.OnActivate();
		_kingdomCategory = UIResourceManager.LoadSpriteCategory("ui_kingdom");
		_clanCategory = UIResourceManager.LoadSpriteCategory("ui_clan");
		_gauntletLayer = new GauntletLayer("KingdomScreen", 1, shouldClear: true);
		_gauntletLayer.InputRestrictions.SetInputRestrictions();
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
		_gauntletLayer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_gauntletLayer);
		AddLayer(_gauntletLayer);
		DataSource = CreateDataSource();
		DataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		DataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		DataSource.SetPreviousTabInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToPreviousTab"));
		DataSource.SetNextTabInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToNextTab"));
		if (_kingdomState.InitialSelectedDecision != null)
		{
			DataSource.Decision.HandleDecision(_kingdomState.InitialSelectedDecision);
		}
		else if (_kingdomState.InitialSelectedArmy != null)
		{
			DataSource.SelectArmy(_kingdomState.InitialSelectedArmy);
		}
		else if (_kingdomState.InitialSelectedSettlement != null)
		{
			DataSource.SelectSettlement(_kingdomState.InitialSelectedSettlement);
		}
		else if (_kingdomState.InitialSelectedClan != null)
		{
			DataSource.SelectClan(_kingdomState.InitialSelectedClan);
		}
		else if (_kingdomState.InitialSelectedPolicy != null)
		{
			DataSource.SelectPolicy(_kingdomState.InitialSelectedPolicy);
		}
		else if (_kingdomState.InitialSelectedKingdom != null)
		{
			DataSource.SelectKingdom(_kingdomState.InitialSelectedKingdom);
		}
		_gauntletLayer.LoadMovie("KingdomManagement", DataSource);
		Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.KingdomScreen));
		UISoundsHelper.PlayUISound("event:/ui/panels/panel_kingdom_open");
		_gauntletLayer.GamepadNavigationContext.GainNavigationAfterFrames(2, null);
	}

	void IGameStateListener.OnDeactivate()
	{
		base.OnDeactivate();
		RemoveLayer(_gauntletLayer);
		_gauntletLayer.IsFocusLayer = false;
		ScreenManager.TryLoseFocus(_gauntletLayer);
		Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.None));
	}

	void IGameStateListener.OnInitialize()
	{
	}

	void IGameStateListener.OnFinalize()
	{
		if (MapScreen.Instance != null)
		{
			MapScreen.Instance.NavigationHandler.IsNavigationLocked = false;
		}
		_kingdomCategory.Unload();
		_clanCategory.Unload();
		DataSource.OnFinalize();
		DataSource = null;
		_gauntletLayer = null;
	}

	protected void ShowArmyOnMap(Army army)
	{
		CloseKingdomScreen();
		MapScreen.Instance.FastMoveCameraToPosition(army.LeaderParty.Position);
	}

	protected void OpenArmyManagement()
	{
		if (_gauntletLayer != null)
		{
			_armyManagementDatasource = new ArmyManagementVM(CloseArmyManagement);
			_armyManagementDatasource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
			_armyManagementDatasource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
			_armyManagementDatasource.SetResetInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Reset"));
			_armyManagementDatasource.SetRemoveInputKey(HotKeyManager.GetCategory("ArmyManagementHotkeyCategory").GetHotKey("RemoveParty"));
			_armyManagementCategory = UIResourceManager.LoadSpriteCategory("ui_armymanagement");
			_armyManagementLayer = new GauntletLayer("Kingdom_ArmManagement", 2);
			_armyManagementLayer.LoadMovie("ArmyManagement", _armyManagementDatasource);
			_armyManagementLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
			_armyManagementLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			_armyManagementLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("ArmyManagementHotkeyCategory"));
			_armyManagementLayer.InputRestrictions.SetInputRestrictions();
			_armyManagementLayer.IsFocusLayer = true;
			AddLayer(_armyManagementLayer);
			ScreenManager.TrySetFocus(_armyManagementLayer);
		}
	}

	protected void CloseArmyManagement()
	{
		if (_armyManagementLayer != null)
		{
			_armyManagementLayer.InputRestrictions.ResetInputRestrictions();
			_armyManagementLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_armyManagementLayer);
			RemoveLayer(_armyManagementLayer);
			_armyManagementLayer = null;
		}
		if (_armyManagementDatasource != null)
		{
			_armyManagementDatasource.OnFinalize();
			_armyManagementDatasource = null;
		}
		if (_armyManagementCategory != null)
		{
			_armyManagementCategory.Unload();
			_armyManagementCategory = null;
		}
		Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.KingdomScreen));
		DataSource.OnRefresh();
	}

	protected void CloseKingdomScreen()
	{
		Game.Current.GameStateManager.PopState();
		UISoundsHelper.PlayUISound("event:/ui/default");
	}
}
