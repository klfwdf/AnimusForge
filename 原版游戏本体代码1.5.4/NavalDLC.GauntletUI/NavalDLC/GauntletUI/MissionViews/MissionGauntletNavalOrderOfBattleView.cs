using NavalDLC.Missions.Objects;
using NavalDLC.View.MissionViews;
using NavalDLC.ViewModelCollection.OrderOfBattle;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace NavalDLC.GauntletUI.MissionViews;

[OverrideView(typeof(NavalOrderOfBattleView))]
public class MissionGauntletNavalOrderOfBattleView : MissionView
{
	private NavalOrderOfBattleVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private SpriteCategory _orderOfBattleSpriteCategory;

	private MissionGauntletNavalOrderUIHandler _orderUIHandler;

	private DeploymentMissionController _deploymentController;

	private bool _isActive;

	private bool _wereHotkeysEnabledLastFrame;

	private bool _isResetPressed;

	private bool _isReadyPressed;

	private float _cachedOrderTypeSetting;

	public MissionGauntletNavalOrderOfBattleView(Mission mission)
	{
		_dataSource = new NavalOrderOfBattleVM(mission, OnFormationSelected, ClearFormationSelection, OnAutoDeploy, OnBeginMission);
		_dataSource.SetDoneInputKey(HotKeyManager.GetCategory("OrderOfBattleHotKeyCategory").GetHotKey("Confirm"));
		_dataSource.SetResetInputKey(HotKeyManager.GetCategory("OrderOfBattleHotKeyCategory").GetHotKey("AutoDeploy"));
		ViewOrderPriority = 13;
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		InitializeView();
		_orderUIHandler = base.Mission.GetMissionBehavior<MissionGauntletNavalOrderUIHandler>();
		_deploymentController = base.Mission.GetMissionBehavior<DeploymentMissionController>();
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (!_isActive && _deploymentController.TeamSetupOver && !base.Mission.IsDeploymentFinished)
		{
			_cachedOrderTypeSetting = ManagedOptions.GetConfig(ManagedOptions.ManagedOptionsType.OrderType);
			ManagedOptions.SetConfig(ManagedOptions.ManagedOptionsType.OrderType, 1f);
			_dataSource.Initialize();
			_gauntletLayer.InputRestrictions.SetInputRestrictions();
			_isActive = true;
		}
		if (_isActive)
		{
			UpdateFormationPositions();
			_wereHotkeysEnabledLastFrame = _dataSource.AreHotkeysEnabled;
			HandleLayerFocus();
			_dataSource.AreHotkeysEnabled = !base.MissionScreen.IsRadialMenuActive && !base.Mission.IsOrderMenuOpen && TaleWorlds.InputSystem.Input.IsGamepadActive && !_gauntletLayer.IsFocusLayer;
			TickInput();
		}
	}

	public override void OnDeploymentFinished()
	{
		base.OnDeploymentFinished();
		DestroyView();
	}

	private void TickInput()
	{
		if (!_dataSource.IsAssignmentDirty)
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
			if (_gauntletLayer.Input.IsHotKeyReleased("Exit") && (_dataSource.HasSelectedHero || _dataSource.HasSelectedShip) && _dataSource.CanToggleHeroOrShipSelection)
			{
				UISoundsHelper.PlayUISound("event:/ui/oob/officer_pick");
				_dataSource.ExecuteClearHeroAndShipSelection();
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
	}

	private void HandleLayerFocus()
	{
		bool flag = _dataSource.HasSelectedHero || _dataSource.HasSelectedShip;
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

	private void UpdateFormationPositions()
	{
		if (!_dataSource.IsAssignmentDirty)
		{
			for (int i = 0; i < _dataSource.AllFormations.Count; i++)
			{
				UpdateFormationPosition(_dataSource.AllFormations[i]);
			}
		}
	}

	private void UpdateFormationPosition(NavalOrderOfBattleFormationItemVM formation)
	{
		if (formation.HasShip)
		{
			MissionShip missionShip = formation.Ship.MissionShip;
			if (missionShip != null)
			{
				Vec3 worldSpacePosition = missionShip.GlobalFrame.origin + Vec3.Up * 3f;
				float screenX = 0f;
				float screenY = 0f;
				float w = 0f;
				MBWindowManager.WorldToScreenInsideUsableArea(base.MissionScreen.CombatCamera, worldSpacePosition, ref screenX, ref screenY, ref w);
				formation.ScreenPosition = new Vec2(screenX, screenY - 50f);
				formation.WSign = MathF.Sign(w);
			}
		}
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

	public override void OnMissionScreenFinalize()
	{
		DestroyView();
		base.OnMissionScreenFinalize();
	}

	public override bool IsOpeningEscapeMenuOnFocusChangeAllowed()
	{
		return !_isActive;
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

	private void InitializeView()
	{
		_gauntletLayer = new GauntletLayer("NavalOrderOfBattle", ViewOrderPriority);
		_gauntletLayer.LoadMovie("NavalOrderOfBattle", _dataSource);
		_orderOfBattleSpriteCategory = UIResourceManager.LoadSpriteCategory("ui_order_of_battle");
		base.MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("OrderOfBattleHotKeyCategory"));
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("OrderOfBattleHotKeyCategory"));
		base.MissionScreen.AddLayer(_gauntletLayer);
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
			_orderOfBattleSpriteCategory.Unload();
		}
	}

	private void OnFormationSelected(NavalOrderOfBattleFormationItemVM selectedFormation)
	{
		SelectFormationAtIndex(selectedFormation.Formation.Index);
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
		_orderUIHandler.OnFiltersSet(_dataSource.CurrentFilterConfiguration);
		_orderUIHandler.OnClassesSet(_dataSource.CurrentClassConfiguration);
		_orderUIHandler.OnBeginMission();
	}
}
