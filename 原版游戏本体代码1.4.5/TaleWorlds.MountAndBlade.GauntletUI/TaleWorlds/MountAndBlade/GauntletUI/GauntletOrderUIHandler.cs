using System;
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
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI;

public abstract class GauntletOrderUIHandler : MissionView
{
	protected MBReadOnlyList<Formation> _focusedFormationsCache;

	protected string _radialOrderMovieName = "OrderRadial";

	protected string _barOrderMovieName = "OrderBar";

	protected float _holdTime;

	protected bool _holdHandled;

	protected OrderTroopPlacer _orderTroopPlacer;

	protected GauntletLayer _gauntletLayer;

	protected GauntletMovieIdentifier _movie;

	protected SpriteCategory _spriteCategory;

	protected MissionOrderVM _dataSource;

	protected SiegeDeploymentHandler _siegeDeploymentHandler;

	protected MissionFormationTargetSelectionHandler _formationTargetHandler;

	protected bool _isOrderRadialEnabled;

	protected bool _isReceivingInput;

	protected bool _isInitialized;

	protected bool _slowedDownMission;

	protected float _latestDt;

	protected bool _targetFormationOrderGivenWithActionButton;

	protected bool _isTransferEnabled;

	public abstract bool IsDeployment { get; }

	public abstract bool IsSiegeDeployment { get; }

	public abstract bool IsValidForTick { get; }

	public MissionOrderVM.CursorStates CursorState => _dataSource?.CursorState ?? MissionOrderVM.CursorStates.Move;

	protected float _minHoldTimeForActivation => 0f;

	public bool IsOrderMenuActive => _dataSource?.IsToggleOrderShown ?? false;

	public bool IsAnyOrderSetActive => _dataSource?.IsAnyOrderSetActive ?? false;

	public bool IsViewCreated
	{
		get
		{
			if (_gauntletLayer != null)
			{
				return _dataSource != null;
			}
			return false;
		}
	}

	public GauntletOrderUIHandler()
	{
		ViewOrderPriority = 14;
	}

	protected abstract void OnTransferFinished();

	protected abstract void SetLayerEnabled(bool isEnabled);

	protected virtual void SetSuspendTroopPlacer(bool value)
	{
		_orderTroopPlacer.SuspendTroopPlacer = value;
		base.MissionScreen.SetOrderFlagVisibility(!value);
	}

	public virtual void SelectFormationAtIndex(int index)
	{
		_dataSource?.OnTroopFormationSelected(index);
	}

	public virtual void DeselectFormationAtIndex(int index)
	{
		_dataSource?.TroopController.OnDeselectFormation(index);
	}

	protected virtual IOrderable GetFocusedOrderableObject()
	{
		return base.MissionScreen.OrderFlag?.FocusedOrderableObject;
	}

	protected VisualOrderExecutionParameters GetVisualOrderExecutionParameters()
	{
		Formation formation = null;
		MBReadOnlyList<Formation> focusedFormationsCache = _focusedFormationsCache;
		if (focusedFormationsCache != null && focusedFormationsCache.Count > 0)
		{
			formation = _focusedFormationsCache[0];
		}
		WorldPosition? worldPosition = null;
		if (base.MissionScreen.Mission.Scene != null)
		{
			Vec3 orderFlagPosition = base.MissionScreen.GetOrderFlagPosition();
			worldPosition = new WorldPosition(base.MissionScreen.Mission.Scene, orderFlagPosition);
		}
		return new VisualOrderExecutionParameters(Agent.Main, formation, worldPosition);
	}

	public override void OnMissionScreenActivate()
	{
		base.OnMissionScreenActivate();
		if (_dataSource != null)
		{
			_dataSource.AfterInitialize();
			_isInitialized = true;
		}
		TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged = (Action)Delegate.Combine(TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged, new Action(OnGamepadActiveStateChanged));
	}

	public override void OnMissionScreenDeactivate()
	{
		base.OnMissionScreenDeactivate();
		TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged = (Action)Delegate.Remove(TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged, new Action(OnGamepadActiveStateChanged));
	}

	private void OnGamepadActiveStateChanged()
	{
		if (_dataSource != null)
		{
			_dataSource.TroopController.TroopList.ForEach(delegate(OrderTroopItemVM t)
			{
				t.UpdateSelectionKeyInfo();
			});
		}
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		_latestDt = dt;
		_isReceivingInput = false;
		if (IsValidForTick && _dataSource != null && _gauntletLayer.IsActive)
		{
			TickInput(dt);
			_dataSource.Update();
			if (_dataSource.IsToggleOrderShown)
			{
				if (_targetFormationOrderGivenWithActionButton)
				{
					SetSuspendTroopPlacer(value: false);
					_targetFormationOrderGivenWithActionButton = false;
				}
				_orderTroopPlacer.IsDrawingForced = _dataSource.SelectedOrderSet?.OrderSet.StringId == "order_type_movement";
				_orderTroopPlacer.IsDrawingFacing = _dataSource.SelectedOrderSet?.OrderSet.StringId == "order_type_facing";
				_orderTroopPlacer.IsDrawingForming = false;
				if (CursorState == MissionOrderVM.CursorStates.Face)
				{
					Vec2 orderLookAtDirection = OrderController.GetOrderLookAtDirection(base.Mission.MainAgent.Team.PlayerOrderController.SelectedFormations, base.MissionScreen.OrderFlag.Position.AsVec2);
					base.MissionScreen.OrderFlag.SetArrowVisibility(isVisible: true, orderLookAtDirection);
				}
				else
				{
					base.MissionScreen.OrderFlag.SetArrowVisibility(isVisible: false, Vec2.Invalid);
				}
				if (CursorState == MissionOrderVM.CursorStates.Form)
				{
					float orderFormCustomWidth = OrderController.GetOrderFormCustomWidth(base.Mission.MainAgent.Team.PlayerOrderController.SelectedFormations, base.MissionScreen.OrderFlag.Position);
					base.MissionScreen.OrderFlag.SetWidthVisibility(isVisible: true, orderFormCustomWidth);
				}
				else
				{
					base.MissionScreen.OrderFlag.SetWidthVisibility(isVisible: false, -1f);
				}
				if (TaleWorlds.InputSystem.Input.IsGamepadActive)
				{
					OrderSetVM selectedOrderSet = _dataSource.SelectedOrderSet;
					if (selectedOrderSet == null || selectedOrderSet.HasSingleOrder)
					{
						if (_orderTroopPlacer.SuspendTroopPlacer && _dataSource.ActiveTargetState == 0)
						{
							_orderTroopPlacer.SuspendTroopPlacer = false;
						}
					}
					else if (!_orderTroopPlacer.SuspendTroopPlacer)
					{
						_orderTroopPlacer.SuspendTroopPlacer = true;
					}
				}
			}
			else if (_dataSource.TroopController.IsTransferActive || IsDeployment)
			{
				_gauntletLayer.InputRestrictions.SetInputRestrictions();
			}
			else
			{
				if (!_dataSource.TroopController.IsTransferActive && !_orderTroopPlacer.SuspendTroopPlacer)
				{
					_orderTroopPlacer.SuspendTroopPlacer = true;
				}
				_gauntletLayer.InputRestrictions.ResetInputRestrictions();
			}
			if (IsDeployment)
			{
				if (!base.MissionScreen.IsRadialMenuActive && (base.MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.RightMouseButton) || base.MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.ControllerLTrigger)))
				{
					_gauntletLayer.InputRestrictions.SetMouseVisibility(isVisible: false);
				}
				else
				{
					_gauntletLayer.InputRestrictions.SetMouseVisibility(isVisible: true);
				}
			}
			base.MissionScreen.OrderFlag.IsTroop = _dataSource.ActiveTargetState == 0;
			TickOrderFlag(_latestDt, forceUpdate: false);
		}
		bool flag = IsOrderRadialActive();
		if (_isOrderRadialEnabled && !flag)
		{
			base.MissionScreen.UnregisterRadialMenuObject(this);
		}
		else if (!_isOrderRadialEnabled && flag)
		{
			base.MissionScreen.RegisterRadialMenuObject(this);
		}
		_isOrderRadialEnabled = flag;
		_targetFormationOrderGivenWithActionButton = false;
		_dataSource?.UpdateCanUseShortcuts(_isReceivingInput);
	}

	protected virtual void TickInput(float dt)
	{
		if (_dataSource == null)
		{
			return;
		}
		bool displayDialog = ((IMissionScreen)base.MissionScreen).GetDisplayDialog();
		bool flag = base.MissionScreen.SceneLayer.IsHitThisFrame || _gauntletLayer.IsHitThisFrame;
		if (displayDialog || (TaleWorlds.InputSystem.Input.IsGamepadActive && !flag))
		{
			_isReceivingInput = false;
			_dataSource.UpdateCanUseShortcuts(value: false);
			return;
		}
		if (TaleWorlds.InputSystem.Input.IsGamepadActive)
		{
			for (int i = 0; i < _dataSource.TroopController.TroopList.Count; i++)
			{
				OrderTroopItemVM orderTroopItemVM = _dataSource.TroopController.TroopList[i];
				orderTroopItemVM.ShowSelectionInputs = orderTroopItemVM.IsSelectionHighlightActive && orderTroopItemVM.IsSelectable;
			}
		}
		else
		{
			for (int j = 0; j < _dataSource.TroopController.TroopList.Count; j++)
			{
				OrderTroopItemVM orderTroopItemVM2 = _dataSource.TroopController.TroopList[j];
				orderTroopItemVM2.IsSelectionHighlightActive = false;
				orderTroopItemVM2.ShowSelectionInputs = orderTroopItemVM2.IsSelectable;
			}
		}
		_isReceivingInput = true;
		if (!IsDeployment)
		{
			if (!_holdHandled && base.Input.IsGameKeyDown(87) && !_dataSource.IsToggleOrderShown)
			{
				_holdTime += dt;
				if (_holdTime >= _minHoldTimeForActivation)
				{
					_dataSource.OpenToggleOrder(fromHold: true, !_dataSource.IsHolding);
					_dataSource.IsHolding = true;
					_holdHandled = true;
				}
			}
			else if (_holdHandled && !base.Input.IsGameKeyDown(87))
			{
				if (_dataSource.IsHolding && _dataSource.IsToggleOrderShown)
				{
					_dataSource.TryCloseToggleOrder(applySelectedOrders: true);
				}
				_dataSource.IsHolding = false;
				_holdTime = 0f;
				_holdHandled = false;
			}
		}
		if (_dataSource.IsToggleOrderShown)
		{
			if (_dataSource.ActiveTargetState == 0 && (base.Input.IsKeyReleased(InputKey.LeftMouseButton) || base.Input.IsKeyReleased(InputKey.ControllerRTrigger)))
			{
				if (_dataSource.SelectedOrderSet != null && TaleWorlds.InputSystem.Input.IsGamepadActive)
				{
					VisualOrderExecutionParameters visualOrderExecutionParameters = GetVisualOrderExecutionParameters();
					_dataSource.SelectedOrderSet.Orders.FirstOrDefault((OrderItemVM o) => o.IsSelected)?.ExecuteAction(visualOrderExecutionParameters);
				}
				else
				{
					switch (CursorState)
					{
					case MissionOrderVM.CursorStates.Move:
					{
						MBReadOnlyList<Formation> focusedFormationsCache = _focusedFormationsCache;
						if (focusedFormationsCache != null && focusedFormationsCache.Count > 0)
						{
							OrderItemVM chargeOrder = GetChargeOrder();
							VisualOrderExecutionParameters visualOrderExecutionParameters2 = GetVisualOrderExecutionParameters();
							chargeOrder.ExecuteAction(visualOrderExecutionParameters2);
							SetSuspendTroopPlacer(value: true);
							_targetFormationOrderGivenWithActionButton = true;
							if (!_dataSource.IsHolding)
							{
								_dataSource.TryCloseToggleOrder();
							}
							break;
						}
						IOrderable focusedOrderableObject = GetFocusedOrderableObject();
						if (focusedOrderableObject != null)
						{
							if (_dataSource.OrderController.SelectedFormations.Count > 0)
							{
								_dataSource.OrderController.SetOrderWithOrderableObject(focusedOrderableObject);
							}
							else
							{
								Debug.FailedAssert("No selected formations when issuing order", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI\\GauntletOrderUIBase.cs", "TickInput", 370);
							}
						}
						break;
					}
					case MissionOrderVM.CursorStates.Face:
						_dataSource.OrderController.SetOrderWithPosition(OrderType.LookAtDirection, new WorldPosition(TaleWorlds.MountAndBlade.Mission.Current.Scene, UIntPtr.Zero, base.MissionScreen.GetOrderFlagPosition(), hasValidZ: false));
						break;
					case MissionOrderVM.CursorStates.Form:
						_dataSource.OrderController.SetOrderWithPosition(OrderType.FormCustom, new WorldPosition(TaleWorlds.MountAndBlade.Mission.Current.Scene, UIntPtr.Zero, base.MissionScreen.GetOrderFlagPosition(), hasValidZ: false));
						break;
					default:
						Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI\\GauntletOrderUIBase.cs", "TickInput", 385);
						break;
					}
				}
			}
			if (base.Input.IsKeyReleased(InputKey.RightMouseButton) && !IsDeployment)
			{
				_dataSource.OnEscape();
			}
		}
		else if (_dataSource.TroopController.IsTransferActive != _isTransferEnabled)
		{
			_isTransferEnabled = _dataSource.TroopController.IsTransferActive;
			if (!_isTransferEnabled)
			{
				_gauntletLayer.UIContext.ContextAlpha = (BannerlordConfig.HideBattleUI ? 0f : 1f);
				_gauntletLayer.IsFocusLayer = false;
				ScreenManager.TryLoseFocus(_gauntletLayer);
			}
			else
			{
				_gauntletLayer.UIContext.ContextAlpha = 1f;
				_gauntletLayer.IsFocusLayer = true;
				ScreenManager.TrySetFocus(_gauntletLayer);
			}
		}
		else if (_dataSource.TroopController.IsTransferActive)
		{
			if (_gauntletLayer.Input.IsHotKeyReleased("Exit"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_dataSource.TroopController.ExecuteCancelTransfer();
			}
			else if (_gauntletLayer.Input.IsHotKeyReleased("Confirm"))
			{
				if (_dataSource.TroopController.IsTransferValid)
				{
					UISoundsHelper.PlayUISound("event:/ui/default");
					_dataSource.TroopController.ExecuteConfirmTransfer();
				}
			}
			else if (_gauntletLayer.Input.IsHotKeyReleased("Reset"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_dataSource.TroopController.ExecuteReset();
			}
		}
		int num = -1;
		if ((!TaleWorlds.InputSystem.Input.IsGamepadActive || _dataSource.IsToggleOrderShown) && !base.DebugInput.IsControlDown())
		{
			if (base.Input.IsGameKeyPressed(69))
			{
				num = 0;
			}
			else if (base.Input.IsGameKeyPressed(70))
			{
				num = 1;
			}
			else if (base.Input.IsGameKeyPressed(71))
			{
				num = 2;
			}
			else if (base.Input.IsGameKeyPressed(72))
			{
				num = 3;
			}
			else if (base.Input.IsGameKeyPressed(73))
			{
				num = 4;
			}
			else if (base.Input.IsGameKeyPressed(74))
			{
				num = 5;
			}
			else if (base.Input.IsGameKeyPressed(75))
			{
				num = 6;
			}
			else if (base.Input.IsGameKeyPressed(76))
			{
				num = 7;
			}
			else if (base.Input.IsGameKeyPressed(77) && !TaleWorlds.InputSystem.Input.IsGamepadActive)
			{
				num = 8;
			}
		}
		if (num > -1)
		{
			if (_dataSource.SelectedOrderSet != null)
			{
				int count = _dataSource.SelectedOrderSet.Orders.Count;
				if (count > 0 && num >= 0)
				{
					if (num == 8 && _dataSource.SelectedOrderSet.Orders.Any((OrderItemVM x) => x.Order is ReturnVisualOrder))
					{
						_dataSource.SelectedOrderSet.ExecuteDeSelect();
					}
					else if (num < count)
					{
						OrderItemVM orderItemVM = _dataSource.SelectedOrderSet.Orders[num];
						if (!(orderItemVM.Order is ReturnVisualOrder))
						{
							VisualOrderExecutionParameters visualOrderExecutionParameters3 = GetVisualOrderExecutionParameters();
							orderItemVM.ExecuteAction(visualOrderExecutionParameters3);
							if (IsDeployment || _dataSource.IsHolding)
							{
								_dataSource.SelectedOrderSet?.ExecuteDeSelect();
							}
							else
							{
								_dataSource.TryCloseToggleOrder();
							}
						}
					}
				}
			}
			else
			{
				_dataSource.OpenToggleOrder(fromHold: false);
				if (_dataSource.IsToggleOrderShown)
				{
					if (num == 8 && _dataSource.OrderSets.Any((OrderSetVM x) => x.HasSingleOrder && x.Orders[0].Order is ReturnVisualOrder))
					{
						_dataSource.TryCloseToggleOrder();
					}
					else
					{
						OrderSetVM orderSetAtIndex = _dataSource.GetOrderSetAtIndex(num);
						if (orderSetAtIndex != null && (!orderSetAtIndex.HasSingleOrder || !(orderSetAtIndex.Orders[0].Order is ReturnVisualOrder)))
						{
							_dataSource.TrySelectOrderSet(orderSetAtIndex);
						}
					}
				}
			}
		}
		int num2 = -1;
		if (base.Input.IsGameKeyPressed(78))
		{
			num2 = 100;
		}
		else if (base.Input.IsGameKeyPressed(79))
		{
			num2 = 0;
		}
		else if (base.Input.IsGameKeyPressed(80))
		{
			num2 = 1;
		}
		else if (base.Input.IsGameKeyPressed(81))
		{
			num2 = 2;
		}
		else if (base.Input.IsGameKeyPressed(82))
		{
			num2 = 3;
		}
		else if (base.Input.IsGameKeyPressed(83))
		{
			num2 = 4;
		}
		else if (base.Input.IsGameKeyPressed(84))
		{
			num2 = 5;
		}
		else if (base.Input.IsGameKeyPressed(85))
		{
			num2 = 6;
		}
		else if (base.Input.IsGameKeyPressed(86))
		{
			num2 = 7;
		}
		if (!IsDeployment && _dataSource.IsToggleOrderShown && TaleWorlds.InputSystem.Input.IsGamepadActive)
		{
			if (base.Input.IsGameKeyPressed(88))
			{
				_dataSource.OnTroopHighlightSelection(isDirectionLeft: true);
			}
			else if (base.Input.IsGameKeyPressed(89))
			{
				_dataSource.OnTroopHighlightSelection(isDirectionLeft: false);
			}
			else if (base.Input.IsGameKeyPressed(90))
			{
				_dataSource.ExecuteSelectHighlightedFormation();
			}
			else if (base.Input.IsGameKeyPressed(91))
			{
				_dataSource.ExecuteToggleHighlightedFormation();
			}
		}
		if (num2 != -1)
		{
			_dataSource.OnTroopFormationSelected(num2);
		}
		if (base.Input.IsGameKeyPressed(68))
		{
			_dataSource.ViewOrders();
		}
	}

	protected virtual OrderItemVM GetChargeOrder()
	{
		if (_dataSource == null)
		{
			return null;
		}
		for (int i = 0; i < _dataSource.OrderSets.Count; i++)
		{
			OrderSetVM orderSetVM = _dataSource.OrderSets[i];
			for (int j = 0; j < orderSetVM.Orders.Count; j++)
			{
				OrderItemVM orderItemVM = orderSetVM.Orders[j];
				if (orderItemVM.Order.StringId == "order_movement_charge")
				{
					return orderItemVM;
				}
			}
		}
		return null;
	}

	public override void OnAgentBuild(Agent agent, Banner banner)
	{
		if (_isInitialized && agent.IsHuman && _dataSource != null)
		{
			_dataSource.TroopController.AddTroops(agent);
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, killingBlow);
		if (affectedAgent.IsHuman && _dataSource != null)
		{
			_dataSource.TroopController.RemoveTroops(affectedAgent);
		}
	}

	public override bool OnEscape()
	{
		if (_dataSource != null)
		{
			bool isToggleOrderShown = _dataSource.IsToggleOrderShown;
			_dataSource.OnEscape();
			return isToggleOrderShown;
		}
		return false;
	}

	public override bool IsReady()
	{
		return _spriteCategory.IsCategoryFullyLoaded();
	}

	private bool IsOrderRadialActive()
	{
		if (_dataSource != null && _dataSource.IsToggleOrderShown && (TaleWorlds.InputSystem.Input.IsGamepadActive || base.Mission.Mode == MissionMode.Deployment))
		{
			return _dataSource.OrderSets.Any((OrderSetVM x) => x.IsSelected);
		}
		return false;
	}

	public void OnActivateToggleOrder()
	{
		SetLayerEnabled(isEnabled: true);
	}

	public void OnDeactivateToggleOrder()
	{
		if (_dataSource != null && !_dataSource.TroopController.IsTransferActive)
		{
			SetLayerEnabled(isEnabled: false);
		}
	}

	protected void OnBeforeOrder()
	{
		TickOrderFlag(_latestDt, forceUpdate: true);
	}

	protected void TickOrderFlag(float dt, bool forceUpdate)
	{
		if ((base.MissionScreen.OrderFlag.IsVisible || forceUpdate) && Utilities.EngineFrameNo != base.MissionScreen.OrderFlag.LatestUpdateFrameNo)
		{
			base.MissionScreen.OrderFlag.Tick(_latestDt);
		}
	}

	protected void ToggleScreenRotation(bool isLocked)
	{
		MissionScreen.SetFixedMissionCameraActive(isLocked);
	}

	protected override void OnSuspendView()
	{
		base.OnSuspendView();
		_dataSource.TryCloseToggleOrder();
		ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: true);
	}

	protected override void OnResumeView()
	{
		base.OnResumeView();
		ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: false);
	}
}
