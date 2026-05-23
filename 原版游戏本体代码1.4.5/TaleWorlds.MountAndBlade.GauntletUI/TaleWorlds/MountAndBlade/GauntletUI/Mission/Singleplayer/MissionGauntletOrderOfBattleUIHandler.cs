using System;
using System.Collections.Generic;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;

[OverrideView(typeof(MissionOrderOfBattleUIHandler))]
public class MissionGauntletOrderOfBattleUIHandler : MissionView
{
	private OrderOfBattleVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private GauntletMovieIdentifier _movie;

	private SpriteCategory _orderOfBattleCategory;

	private MissionGauntletSingleplayerOrderUIHandler _orderUIHandler;

	private AssignPlayerRoleInTeamMissionController _playerRoleMissionController;

	private OrderTroopPlacer _orderTroopPlacer;

	private bool _isActive;

	private bool _wereHotkeysEnabledLastFrame;

	private bool _isResetPressed;

	private bool _isReadyPressed;

	private bool _isAnyHeroSelected;

	private bool _isClassSelectionEnabled;

	private float _cachedOrderTypeSetting;

	public MissionGauntletOrderOfBattleUIHandler(OrderOfBattleVM dataSource)
	{
		_dataSource = dataSource;
		_dataSource.SetDoneInputKey(HotKeyManager.GetCategory("OrderOfBattleHotKeyCategory").GetHotKey("Confirm"));
		_dataSource.SetResetInputKey(HotKeyManager.GetCategory("OrderOfBattleHotKeyCategory").GetHotKey("AutoDeploy"));
		ViewOrderPriority = 13;
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_playerRoleMissionController = base.Mission.GetMissionBehavior<AssignPlayerRoleInTeamMissionController>();
		_playerRoleMissionController.OnPlayerTurnToChooseFormationToLead += OnPlayerTurnToChooseFormationToLead;
		_playerRoleMissionController.OnAllFormationsAssignedSergeants += OnAllFormationsAssignedSergeants;
		_orderUIHandler = base.Mission.GetMissionBehavior<MissionGauntletSingleplayerOrderUIHandler>();
		_orderTroopPlacer = base.Mission.GetMissionBehavior<OrderTroopPlacer>();
		OrderTroopPlacer orderTroopPlacer = _orderTroopPlacer;
		orderTroopPlacer.OnUnitDeployed = (Action)Delegate.Combine(orderTroopPlacer.OnUnitDeployed, new Action(OnUnitDeployed));
		_gauntletLayer = new GauntletLayer("MissionOrderOfBattle", ViewOrderPriority);
		_movie = _gauntletLayer.LoadMovie("OrderOfBattle", _dataSource);
		_orderOfBattleCategory = UIResourceManager.LoadSpriteCategory("ui_order_of_battle");
		base.MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("OrderOfBattleHotKeyCategory"));
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("OrderOfBattleHotKeyCategory"));
		base.MissionScreen.AddLayer(_gauntletLayer);
		_gauntletLayer.InputRestrictions.SetInputRestrictions(isMouseVisible: false);
	}

	public override bool IsReady()
	{
		if (!base.Mission.IsDeploymentFinished)
		{
			return _orderOfBattleCategory.IsLoaded;
		}
		return true;
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (_isActive)
		{
			_dataSource.Tick();
		}
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (_isActive)
		{
			_wereHotkeysEnabledLastFrame = _dataSource.AreHotkeysEnabled;
			HandleLayerFocus(out _isAnyHeroSelected, out _isClassSelectionEnabled);
			_dataSource.AreHotkeysEnabled = !base.MissionScreen.IsRadialMenuActive && !base.Mission.IsOrderMenuOpen && TaleWorlds.InputSystem.Input.IsGamepadActive && !_gauntletLayer.IsFocusLayer;
			TickInput();
		}
	}

	private void DestroyView()
	{
		if (_gauntletLayer != null || _dataSource != null)
		{
			if (_isActive)
			{
				ManagedOptions.SetConfig(ManagedOptions.ManagedOptionsType.OrderType, _cachedOrderTypeSetting);
			}
			_isActive = false;
			base.MissionScreen.SetDisplayDialog(value: false);
			_dataSource.OnFinalize();
			_dataSource = null;
			base.MissionScreen.RemoveLayer(_gauntletLayer);
			_gauntletLayer = null;
			_orderOfBattleCategory.Unload();
			_playerRoleMissionController.OnPlayerTurnToChooseFormationToLead -= OnPlayerTurnToChooseFormationToLead;
			_playerRoleMissionController.OnAllFormationsAssignedSergeants -= OnAllFormationsAssignedSergeants;
			OrderTroopPlacer orderTroopPlacer = _orderTroopPlacer;
			orderTroopPlacer.OnUnitDeployed = (Action)Delegate.Remove(orderTroopPlacer.OnUnitDeployed, new Action(OnUnitDeployed));
		}
	}

	private void TickInput()
	{
		if (base.MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.RightMouseButton) || base.MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.ControllerLTrigger))
		{
			_gauntletLayer.InputRestrictions.SetMouseVisibility(isVisible: false);
			_dataSource.AreCameraControlsEnabled = true;
		}
		else
		{
			_gauntletLayer.InputRestrictions.SetMouseVisibility(isVisible: true);
			_dataSource.AreCameraControlsEnabled = false;
		}
		if (_gauntletLayer.Input.IsHotKeyReleased("Exit"))
		{
			if (_isClassSelectionEnabled)
			{
				UISoundsHelper.PlayUISound("event:/ui/oob/dropdown");
				_dataSource.ExecuteDisableAllClassSelections();
			}
			else if (_isAnyHeroSelected && _dataSource.CanToggleHeroSelection)
			{
				UISoundsHelper.PlayUISound("event:/ui/oob/officer_pick");
				_dataSource.ExecuteClearHeroSelection();
			}
		}
		if (base.MissionScreen.SceneLayer.Input.IsHotKeyPressed("AutoDeploy"))
		{
			_isResetPressed = _dataSource.AreHotkeysEnabled && _wereHotkeysEnabledLastFrame;
		}
		if (base.MissionScreen.SceneLayer.Input.IsHotKeyPressed("Confirm"))
		{
			_isReadyPressed = _dataSource.AreHotkeysEnabled && _wereHotkeysEnabledLastFrame;
		}
		if (!_dataSource.AreHotkeysEnabled)
		{
			_isResetPressed = false;
			_isReadyPressed = false;
		}
		if (base.MissionScreen.SceneLayer.Input.IsHotKeyReleased("AutoDeploy") && _dataSource.AreHotkeysEnabled && _isResetPressed)
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_dataSource.ExecuteAutoDeploy();
		}
		if (base.MissionScreen.SceneLayer.Input.IsHotKeyReleased("Confirm") && _dataSource.AreHotkeysEnabled && _dataSource.CanStartMission && _isReadyPressed)
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_dataSource.ExecuteBeginMission();
		}
	}

	private void HandleLayerFocus(out bool isAnyHeroSelected, out bool isClassSelectionEnabled)
	{
		isAnyHeroSelected = _dataSource.HasSelectedHeroes;
		isClassSelectionEnabled = _dataSource.IsAnyClassSelectionEnabled();
		bool flag = isAnyHeroSelected | isClassSelectionEnabled;
		if (_gauntletLayer.IsFocusLayer && !flag)
		{
			base.MissionScreen.SetDisplayDialog(value: false);
			_gauntletLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_gauntletLayer);
		}
		else if (!_gauntletLayer.IsFocusLayer && flag)
		{
			base.MissionScreen.SetDisplayDialog(value: true);
			_gauntletLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(_gauntletLayer);
		}
	}

	public override void OnMissionScreenFinalize()
	{
		DestroyView();
		base.OnMissionScreenFinalize();
	}

	public override bool OnEscape()
	{
		bool flag = false;
		if (_isActive)
		{
			bool flag2 = false;
			if (_orderUIHandler != null && _orderUIHandler.IsOrderMenuActive)
			{
				flag2 = _orderUIHandler.IsAnyOrderSetActive;
				flag = _orderUIHandler.OnEscape();
			}
			if (!flag2)
			{
				flag = _dataSource.OnEscape() || flag;
			}
		}
		return flag;
	}

	public override void OnPhotoModeActivated()
	{
		base.OnPhotoModeActivated();
		if (_gauntletLayer != null)
		{
			_gauntletLayer.UIContext.ContextAlpha = 0f;
		}
	}

	public override void OnPhotoModeDeactivated()
	{
		base.OnPhotoModeDeactivated();
		if (_gauntletLayer != null)
		{
			_gauntletLayer.UIContext.ContextAlpha = 1f;
		}
	}

	public override bool IsOpeningEscapeMenuOnFocusChangeAllowed()
	{
		return !_isActive;
	}

	private void OnPlayerTurnToChooseFormationToLead(Dictionary<int, Agent> lockedFormationIndicesAndSergeants, List<int> remainingFormationIndices)
	{
		if (base.Mission.PlayerTeam == null)
		{
			Debug.FailedAssert("Player team must be initialized before OOB", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI\\Mission\\Singleplayer\\MissionGauntletOrderOfBattleUIHandler.cs", "OnPlayerTurnToChooseFormationToLead", 285);
		}
		_cachedOrderTypeSetting = ManagedOptions.GetConfig(ManagedOptions.ManagedOptionsType.OrderType);
		ManagedOptions.SetConfig(ManagedOptions.ManagedOptionsType.OrderType, 1f);
		_dataSource.Initialize(base.Mission, base.MissionScreen.CombatCamera, SelectFormationAtIndex, DeselectFormationAtIndex, ClearFormationSelection, OnAutoDeploy, OnBeginMission, lockedFormationIndicesAndSergeants);
		_gauntletLayer.InputRestrictions.SetInputRestrictions();
		_isActive = true;
	}

	private void OnAllFormationsAssignedSergeants(Dictionary<int, Agent> formationsWithLooselyAssignedSergeants)
	{
		_dataSource.OnAllFormationsAssignedSergeants(formationsWithLooselyAssignedSergeants);
	}

	public override void OnDeploymentFinished()
	{
		base.OnDeploymentFinished();
		bool playerDeployed = MissionGameModels.Current.BattleInitializationModel.CanPlayerSideDeployWithOrderOfBattle();
		_dataSource.OnDeploymentFinalized(playerDeployed);
		DestroyView();
	}

	private void SelectFormationAtIndex(int index)
	{
		_orderUIHandler?.SelectFormationAtIndex(index);
	}

	private void DeselectFormationAtIndex(int index)
	{
		_orderUIHandler?.DeselectFormationAtIndex(index);
	}

	private void ClearFormationSelection()
	{
		_orderUIHandler?.ClearFormationSelection();
	}

	private void OnAutoDeploy()
	{
		_orderUIHandler.OnAutoDeploy();
	}

	private void OnBeginMission()
	{
		_orderUIHandler.OnFiltersSet(_dataSource.CurrentConfiguration);
		_orderUIHandler.OnBeginMission();
	}

	private void OnUnitDeployed()
	{
		_dataSource.OnUnitDeployed();
	}
}
