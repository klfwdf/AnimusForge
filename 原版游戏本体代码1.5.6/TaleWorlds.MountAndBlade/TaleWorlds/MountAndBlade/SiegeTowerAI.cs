namespace TaleWorlds.MountAndBlade;

public sealed class SiegeTowerAI : UsableMachineAIBase
{
	private SiegeTower SiegeTower => UsableMachine as SiegeTower;

	public override bool HasActionCompleted
	{
		get
		{
			if (SiegeTower.MovementComponent.HasArrivedAtTarget)
			{
				return SiegeTower.State == SiegeTower.GateState.Open;
			}
			return false;
		}
	}

	protected override MovementOrder NextOrder => MovementOrder.MovementOrderCharge;

	public SiegeTowerAI(SiegeTower siegeTower)
		: base(siegeTower)
	{
	}
}
