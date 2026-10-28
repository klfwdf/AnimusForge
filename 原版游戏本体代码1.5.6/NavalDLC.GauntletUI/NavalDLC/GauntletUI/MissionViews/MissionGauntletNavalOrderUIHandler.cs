using System.Collections.Generic;
using NavalDLC.Missions;
using NavalDLC.Missions.Objects.UsableMachines;
using NavalDLC.View.MissionViews;
using NavalDLC.ViewModelCollection.Order;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace NavalDLC.GauntletUI.MissionViews;

[OverrideView(typeof(NavalMissionOrderUIHandler))]
public class MissionGauntletNavalOrderUIHandler : MissionGauntletSingleplayerOrderUIHandler
{
	protected NavalShipTargetSelectionHandler _shipTargetHandler;

	private OrderController _orderController;

	public MissionGauntletNavalOrderUIHandler()
	{
		_radialOrderMovieName = "NavalOrderRadial";
		_barOrderMovieName = "NavalOrderBar";
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_shipTargetHandler = base.Mission.GetMissionBehavior<NavalShipTargetSelectionHandler>();
		_orderController = base.Mission?.PlayerTeam?.PlayerOrderController;
		if (_orderController != null)
		{
			_orderController.OnSelectedFormationsChanged += OnSelectedFormationsChanged;
		}
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		if (_orderController != null)
		{
			_orderController.OnSelectedFormationsChanged -= OnSelectedFormationsChanged;
		}
	}

	protected override MissionOrderVM CreateDataSource(OrderController orderController)
	{
		NavalMissionOrderVM navalMissionOrderVM = new NavalMissionOrderVM(orderController, IsDeployment, isMultiplayer: false);
		navalMissionOrderVM.SetCallbacks(new MissionOrderCallbacks
		{
			ToggleMissionInputs = base.ToggleScreenRotation,
			GetVisualOrderExecutionParameters = base.GetVisualOrderExecutionParameters,
			SetSuspendTroopPlacer = SetSuspendTroopPlacer,
			OnActivateToggleOrder = base.OnActivateToggleOrder,
			OnDeactivateToggleOrder = base.OnDeactivateToggleOrder,
			OnTransferTroopsFinished = OnTransferFinished,
			OnBeforeOrder = base.OnBeforeOrder
		});
		return navalMissionOrderVM;
	}

	protected override OrderItemVM GetChargeOrder()
	{
		string text = (NavalDLCHelpers.IsShipOrdersAvailable() ? "order_movement_advance" : "order_movement_charge");
		for (int i = 0; i < _dataSource.OrderSets.Count; i++)
		{
			OrderSetVM orderSetVM = _dataSource.OrderSets[i];
			for (int j = 0; j < orderSetVM.Orders.Count; j++)
			{
				OrderItemVM orderItemVM = orderSetVM.Orders[j];
				if (orderItemVM.Order.StringId == text)
				{
					return orderItemVM;
				}
			}
		}
		return null;
	}

	public void OnClassesSet(List<MissionOrderVM.ClassConfiguration> classData)
	{
		(_dataSource as NavalMissionOrderVM).OnClassesSet(classData);
	}

	protected override void TickInput(float dt)
	{
		bool flag = true;
		if (Agent.Main != null)
		{
			ShipControllerMachine shipControllerMachine = Agent.Main.GetComponent<AgentNavalComponent>()?.SteppedShip?.ShipControllerMachine;
			if (shipControllerMachine != null && shipControllerMachine.PilotAgent == Agent.Main && shipControllerMachine.CaptureTimer > 0f)
			{
				flag = false;
			}
		}
		if (flag)
		{
			base.TickInput(dt);
			return;
		}
		_isReceivingInput = false;
		_dataSource?.UpdateCanUseShortcuts(value: false);
		_dataSource?.TryCloseToggleOrder();
	}

	private void OnSelectedFormationsChanged()
	{
		MBReadOnlyList<Formation> mBReadOnlyList = _orderController?.SelectedFormations;
		if (mBReadOnlyList != null)
		{
			bool isFormationTargetingDisabled = mBReadOnlyList.Count == 1 && NavalDLCHelpers.IsPlayerCaptainOfFormationShip(mBReadOnlyList[0]);
			_formationTargetHandler?.SetIsFormationTargetingDisabled(isFormationTargetingDisabled);
			_shipTargetHandler?.SetIsFormationTargetingDisabled(isFormationTargetingDisabled);
		}
	}
}
