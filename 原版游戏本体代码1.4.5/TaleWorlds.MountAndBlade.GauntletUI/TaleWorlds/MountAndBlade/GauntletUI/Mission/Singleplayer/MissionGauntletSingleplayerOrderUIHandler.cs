using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;

[OverrideView(typeof(MissionOrderUIHandler))]
public class MissionGauntletSingleplayerOrderUIHandler : GauntletOrderUIHandler, ISiegeDeploymentView
{
	private const float _slowDownAmountWhileOrderIsOpen = 0.25f;

	private const int _missionTimeSpeedRequestID = 864;

	private List<DeploymentSiegeMachineVM> _deploymentPointDataSources;

	public override bool IsValidForTick
	{
		get
		{
			if (!base.MissionScreen.IsPhotoModeEnabled && !GameStateManager.Current.ActiveStateDisabledByUser)
			{
				if (base.MissionScreen.IsRadialMenuActive)
				{
					return _dataSource.IsToggleOrderShown;
				}
				return true;
			}
			return false;
		}
	}

	public override bool IsDeployment
	{
		get
		{
			TaleWorlds.MountAndBlade.Mission mission = base.Mission;
			if (mission == null)
			{
				return false;
			}
			return mission.Mode == MissionMode.Deployment;
		}
	}

	public override bool IsSiegeDeployment
	{
		get
		{
			if (IsDeployment)
			{
				return _siegeDeploymentHandler != null;
			}
			return false;
		}
	}

	protected virtual MissionOrderVM CreateDataSource(OrderController orderController)
	{
		MissionOrderVM missionOrderVM = new MissionOrderVM(orderController, IsDeployment, isMultiplayer: false);
		missionOrderVM.SetDeploymentParemeters(base.MissionScreen.CombatCamera, IsSiegeDeployment ? _siegeDeploymentHandler.PlayerDeploymentPoints.ToList() : new List<DeploymentPoint>());
		missionOrderVM.SetCallbacks(new MissionOrderCallbacks
		{
			ToggleMissionInputs = base.ToggleScreenRotation,
			RefreshVisuals = RefreshVisuals,
			GetVisualOrderExecutionParameters = base.GetVisualOrderExecutionParameters,
			SetSuspendTroopPlacer = SetSuspendTroopPlacer,
			OnActivateToggleOrder = base.OnActivateToggleOrder,
			OnDeactivateToggleOrder = base.OnDeactivateToggleOrder,
			OnTransferTroopsFinished = OnTransferFinished,
			OnBeforeOrder = base.OnBeforeOrder
		});
		return missionOrderVM;
	}

	public override void OnConversationBegin()
	{
		base.OnConversationBegin();
		_dataSource?.TryCloseToggleOrder();
	}

	public MissionGauntletSingleplayerOrderUIHandler()
	{
		ViewOrderPriority = 14;
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		GameKeyContext category = HotKeyManager.GetCategory("MissionOrderHotkeyCategory");
		GameKeyContext category2 = HotKeyManager.GetCategory("GenericPanelGameKeyCategory");
		base.MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(category);
		_orderTroopPlacer = base.Mission.GetMissionBehavior<OrderTroopPlacer>();
		if (_orderTroopPlacer?.OrderFlag == null)
		{
			Debug.FailedAssert("Order troop placer's order flag is null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI\\Mission\\Singleplayer\\MissionGauntletSingleplayerOrderUIHandler.cs", "OnMissionScreenInitialize", 74);
		}
		base.MissionScreen.OrderFlag = _orderTroopPlacer.OrderFlag;
		Debug.Print("MissionScreen.OrderFlag has been set (SP)");
		base.MissionScreen.SetOrderFlagVisibility(value: false);
		_siegeDeploymentHandler = base.Mission.GetMissionBehavior<SiegeDeploymentHandler>();
		_formationTargetHandler = base.Mission.GetMissionBehavior<MissionFormationTargetSelectionHandler>();
		if (_formationTargetHandler != null)
		{
			_formationTargetHandler.OnFormationFocused += OnFormationFocused;
		}
		_deploymentPointDataSources = new List<DeploymentSiegeMachineVM>();
		_dataSource = CreateDataSource(base.Mission.PlayerTeam.PlayerOrderController);
		_dataSource.SetCancelInputKey(category2.GetHotKey("ToggleEscapeMenu"));
		_dataSource.TroopController.SetDoneInputKey(category2.GetHotKey("Confirm"));
		_dataSource.TroopController.SetCancelInputKey(category2.GetHotKey("Exit"));
		_dataSource.TroopController.SetResetInputKey(category2.GetHotKey("Reset"));
		_dataSource.SetOrderIndexKey(0, category.GetGameKey(69));
		_dataSource.SetOrderIndexKey(1, category.GetGameKey(70));
		_dataSource.SetOrderIndexKey(2, category.GetGameKey(71));
		_dataSource.SetOrderIndexKey(3, category.GetGameKey(72));
		_dataSource.SetOrderIndexKey(4, category.GetGameKey(73));
		_dataSource.SetOrderIndexKey(5, category.GetGameKey(74));
		_dataSource.SetOrderIndexKey(6, category.GetGameKey(75));
		_dataSource.SetOrderIndexKey(7, category.GetGameKey(76));
		_dataSource.SetOrderIndexKey(8, category.GetGameKey(77));
		_dataSource.SetReturnKey(category.GetGameKey(77));
		if (IsSiegeDeployment)
		{
			foreach (DeploymentPoint playerDeploymentPoint in _siegeDeploymentHandler.PlayerDeploymentPoints)
			{
				DeploymentSiegeMachineVM deploymentSiegeMachineVM = new DeploymentSiegeMachineVM(playerDeploymentPoint, null, base.MissionScreen.CombatCamera, _dataSource.DeploymentController.OnRefreshSelectedDeploymentPoint, _dataSource.DeploymentController.OnEntityHover, isSelected: false);
				Vec3 origin = playerDeploymentPoint.GameEntity.GetFrame().origin;
				for (int i = 0; i < playerDeploymentPoint.GameEntity.ChildCount; i++)
				{
					if (playerDeploymentPoint.GameEntity.GetChild(i).HasTag("deployment_point_icon_target"))
					{
						origin += playerDeploymentPoint.GameEntity.GetChild(i).GetFrame().origin;
						break;
					}
				}
				_deploymentPointDataSources.Add(deploymentSiegeMachineVM);
				deploymentSiegeMachineVM.RemainingCount = 0;
			}
		}
		_gauntletLayer = new GauntletLayer("MissionOrder", ViewOrderPriority);
		_gauntletLayer.Input.RegisterHotKeyCategory(category2);
		string movieName = ((!IsDeployment) ? ((BannerlordConfig.OrderType == 0) ? _barOrderMovieName : _radialOrderMovieName) : _radialOrderMovieName);
		_spriteCategory = UIResourceManager.LoadSpriteCategory("ui_order");
		_movie = _gauntletLayer.LoadMovie(movieName, _dataSource);
		base.MissionScreen.AddLayer(_gauntletLayer);
		if (!IsDeployment && BannerlordConfig.HideBattleUI)
		{
			_gauntletLayer.UIContext.ContextAlpha = 0f;
		}
		_dataSource.InputRestrictions = _gauntletLayer.InputRestrictions;
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
	}

	private void OnManagedOptionChanged(ManagedOptions.ManagedOptionsType changedManagedOptionsType)
	{
		switch (changedManagedOptionsType)
		{
		case ManagedOptions.ManagedOptionsType.OrderType:
			if (!IsDeployment)
			{
				_gauntletLayer.ReleaseMovie(_movie);
				string movieName = ((BannerlordConfig.OrderType == 0) ? _barOrderMovieName : _radialOrderMovieName);
				_movie = _gauntletLayer.LoadMovie(movieName, _dataSource);
			}
			break;
		case ManagedOptions.ManagedOptionsType.OrderLayoutType:
			_dataSource?.OnOrderLayoutTypeChanged();
			break;
		case ManagedOptions.ManagedOptionsType.HideBattleUI:
			if (!IsDeployment)
			{
				_gauntletLayer.UIContext.ContextAlpha = (BannerlordConfig.HideBattleUI ? 0f : 1f);
			}
			break;
		case ManagedOptions.ManagedOptionsType.SlowDownOnOrder:
			if (!BannerlordConfig.SlowDownOnOrder && _slowedDownMission)
			{
				base.Mission.RemoveTimeSpeedRequest(864);
			}
			break;
		}
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
		if (_formationTargetHandler != null)
		{
			_formationTargetHandler.OnFormationFocused -= OnFormationFocused;
		}
		_deploymentPointDataSources = null;
		_orderTroopPlacer = null;
		_movie = null;
		_gauntletLayer = null;
		_dataSource.OnFinalize();
		_dataSource = null;
		_siegeDeploymentHandler = null;
		_spriteCategory.Unload();
		_formationTargetHandler = null;
	}

	protected override void OnTransferFinished()
	{
		if (!IsDeployment)
		{
			SetLayerEnabled(isEnabled: false);
		}
	}

	public void OnAutoDeploy()
	{
		_dataSource.DeploymentController.ExecuteAutoDeploy();
		ClearFormationSelection();
	}

	public void OnBeginMission()
	{
		_dataSource.DeploymentController.ExecuteBeginMission();
	}

	protected override void SetLayerEnabled(bool isEnabled)
	{
		if (isEnabled)
		{
			if (!base.MissionScreen.IsRadialMenuActive)
			{
				if (_dataSource == null || _dataSource.ActiveTargetState == 0)
				{
					_orderTroopPlacer.SuspendTroopPlacer = false;
				}
				if (!_slowedDownMission && BannerlordConfig.SlowDownOnOrder)
				{
					base.Mission.AddTimeSpeedRequest(new TaleWorlds.MountAndBlade.Mission.TimeSpeedRequest(0.25f, 864));
					_slowedDownMission = true;
				}
				base.MissionScreen.SetOrderFlagVisibility(value: true);
				Game.Current.EventManager.TriggerEvent(new MissionPlayerToggledOrderViewEvent(newIsEnabledState: true));
			}
		}
		else
		{
			SetSuspendTroopPlacer(value: true);
			if (_slowedDownMission)
			{
				base.Mission.RemoveTimeSpeedRequest(864);
				_slowedDownMission = false;
			}
			Game.Current.EventManager.TriggerEvent(new MissionPlayerToggledOrderViewEvent(newIsEnabledState: false));
		}
	}

	public override void OnDeploymentFinished()
	{
		base.OnDeploymentFinished();
		_dataSource.OnDeploymentFinished();
		_dataSource.TryCloseToggleOrder();
		_deploymentPointDataSources.Clear();
		SetSuspendTroopPlacer(value: true);
		_gauntletLayer.IsFocusLayer = false;
		ScreenManager.TryLoseFocus(_gauntletLayer);
		_gauntletLayer.UIContext.ContextAlpha = (BannerlordConfig.HideBattleUI ? 0f : 1f);
		string text = ((BannerlordConfig.OrderType == 0) ? _barOrderMovieName : _radialOrderMovieName);
		if (text != _radialOrderMovieName)
		{
			_gauntletLayer.ReleaseMovie(_movie);
			_movie = _gauntletLayer.LoadMovie(text, _dataSource);
		}
	}

	public override void OnAfterDeploymentFinished()
	{
		base.OnAfterDeploymentFinished();
		_dataSource.OnAfterDeploymentFinished();
	}

	protected void RefreshVisuals()
	{
		if (!IsSiegeDeployment)
		{
			return;
		}
		foreach (DeploymentSiegeMachineVM deploymentPointDataSource in _deploymentPointDataSources)
		{
			deploymentPointDataSource.RefreshWithDeployedWeapon();
		}
	}

	public void ClearFormationSelection()
	{
		_dataSource?.DeploymentController.ExecuteCancelSelectedDeploymentPoint();
		_dataSource?.OrderController.ClearSelectedFormations();
		_dataSource?.TryCloseToggleOrder();
	}

	public void OnFiltersSet(List<MissionOrderVM.FormationConfiguration> filterData)
	{
		_dataSource.OnFiltersSet(filterData);
	}

	private void OnFormationFocused(MBReadOnlyList<Formation> focusedFormations)
	{
		_focusedFormationsCache = focusedFormations;
		_dataSource.SetFocusedFormations(_focusedFormationsCache);
	}

	void ISiegeDeploymentView.OnEntityHover(WeakGameEntity hoveredEntity)
	{
		if (!_gauntletLayer.IsHitThisFrame)
		{
			_dataSource.DeploymentController.OnEntityHover(hoveredEntity);
		}
	}

	void ISiegeDeploymentView.OnEntitySelection(WeakGameEntity selectedEntity)
	{
		_dataSource.DeploymentController.OnEntitySelect(selectedEntity);
	}
}
