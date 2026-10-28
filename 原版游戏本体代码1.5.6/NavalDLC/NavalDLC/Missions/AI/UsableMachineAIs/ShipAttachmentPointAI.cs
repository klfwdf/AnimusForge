using NavalDLC.Missions.Objects.UsableMachines;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.AI.UsableMachineAIs;

public sealed class ShipAttachmentPointAI : UsableMachineAIBase
{
	public override bool HasActionCompleted => false;

	protected override MovementOrder NextOrder => MovementOrder.MovementOrderCharge;

	public ShipAttachmentPointAI(ShipAttachmentPointMachine shipAttachmentPointMachine)
		: base(shipAttachmentPointMachine)
	{
	}
}
