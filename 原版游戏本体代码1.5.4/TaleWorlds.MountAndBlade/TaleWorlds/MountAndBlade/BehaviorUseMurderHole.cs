using System;
using System.Linq;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class BehaviorUseMurderHole : BehaviorComponent
{
	private readonly CastleGate _outerGate;

	private readonly CastleGate _innerGate;

	private readonly BatteringRam _batteringRam;

	public BehaviorUseMurderHole(Formation formation)
		: base(formation)
	{
		_behaviorSide = formation.AI.Side;
		WorldPosition position = new WorldPosition(base.Formation.Team.Mission.Scene, UIntPtr.Zero, (formation.Team.TeamAI as TeamAISiegeDefender).MurderHolePosition, hasValidZ: false);
		_outerGate = (formation.Team.TeamAI as TeamAISiegeDefender).OuterGate;
		_innerGate = (formation.Team.TeamAI as TeamAISiegeDefender).InnerGate;
		_batteringRam = base.Formation.Team.Mission.ActiveMissionObjects.FindAllWithType<BatteringRam>().FirstOrDefault();
		base.CurrentOrder = MovementOrder.MovementOrderMove(position);
		base.BehaviorCoherence = 0f;
	}

	public override void TickOccasionally()
	{
		base.Formation.SetMovementOrder(base.CurrentOrder);
	}

	public bool IsMurderHoleActive()
	{
		if (_batteringRam == null || !_batteringRam.HasArrivedAtTarget || _innerGate.IsDestroyed)
		{
			if (_outerGate.IsDestroyed)
			{
				return !_innerGate.IsDestroyed;
			}
			return false;
		}
		return true;
	}

	protected override float GetAiWeight()
	{
		return 10f * (IsMurderHoleActive() ? 1f : 0f);
	}
}
