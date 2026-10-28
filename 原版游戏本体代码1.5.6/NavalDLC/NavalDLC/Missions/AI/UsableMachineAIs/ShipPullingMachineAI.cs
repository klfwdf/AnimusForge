using NavalDLC.Missions.Objects.UsableMachines;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.AI.UsableMachineAIs;

public sealed class ShipPullingMachineAI : UsableMachineAIBase
{
	public override bool HasActionCompleted => false;

	protected override MovementOrder NextOrder => MovementOrder.MovementOrderCharge;

	private ShipPullingMachine ShipPullingMachine => UsableMachine as ShipPullingMachine;

	public ShipPullingMachineAI(ShipPullingMachine shipPullingMachine)
		: base(shipPullingMachine)
	{
	}
}
