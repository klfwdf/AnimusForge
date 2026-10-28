using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public sealed class BatteringRamAI : UsableMachineAIBase
{
	private BatteringRam BatteringRam => UsableMachine as BatteringRam;

	public override bool HasActionCompleted => BatteringRam.IsDeactivated;

	protected override MovementOrder NextOrder
	{
		get
		{
			if (Mission.Current.Teams[0].TeamAI is TeamAISiegeComponent { InnerGate: not null } teamAISiegeComponent && !teamAISiegeComponent.InnerGate.IsDestroyed)
			{
				return MovementOrder.MovementOrderAttackEntity(GameEntity.CreateFromWeakEntity(teamAISiegeComponent.InnerGate.GameEntity), surroundEntity: false);
			}
			return MovementOrder.MovementOrderCharge;
		}
	}

	public BatteringRamAI(BatteringRam batteringRam)
		: base(batteringRam)
	{
	}
}
