using NavalDLC.Missions.Objects.UsableMachines;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.AI.UsableMachineAIs;

public sealed class ShipControllerMachineAI : UsableMachineAIBase
{
	public override bool HasActionCompleted => false;

	protected override MovementOrder NextOrder => MovementOrder.MovementOrderCharge;

	private ShipControllerMachine ShipControllerMachine => UsableMachine as ShipControllerMachine;

	public ShipControllerMachineAI(ShipControllerMachine shipControllerMachine)
		: base(shipControllerMachine)
	{
	}
}
