namespace TaleWorlds.MountAndBlade;

public sealed class SiegeLadderAI : UsableMachineAIBase
{
	public SiegeLadder Ladder => UsableMachine as SiegeLadder;

	public override bool HasActionCompleted => false;

	protected override MovementOrder NextOrder => MovementOrder.MovementOrderCharge;

	public SiegeLadderAI(SiegeLadder ladder)
		: base(ladder)
	{
	}
}
