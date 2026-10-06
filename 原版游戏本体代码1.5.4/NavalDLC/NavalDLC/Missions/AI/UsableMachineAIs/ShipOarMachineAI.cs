using NavalDLC.Missions.Objects.UsableMachines;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.AI.UsableMachineAIs;

public sealed class ShipOarMachineAI : UsableMachineAIBase
{
	public override bool HasActionCompleted => false;

	protected override MovementOrder NextOrder => MovementOrder.MovementOrderCharge;

	private ShipOarMachine ShipOarMachine => UsableMachine as ShipOarMachine;

	public ShipOarMachineAI(ShipOarMachine shipOarMachine)
		: base(shipOarMachine)
	{
	}

	protected override void HandleAgentStopUsingStandingPoint(Agent agent, StandingPoint standingPoint)
	{
		if (agent == ShipOarMachine.PilotAgent)
		{
			ShipOarMachine.StartDelayedPilotRemoval(GetStopUsingStandingPointFlags(agent, standingPoint));
		}
		else
		{
			base.HandleAgentStopUsingStandingPoint(agent, standingPoint);
		}
	}
}
