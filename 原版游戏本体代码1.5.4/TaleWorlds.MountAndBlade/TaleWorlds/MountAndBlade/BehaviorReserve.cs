using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace TaleWorlds.MountAndBlade;

public class BehaviorReserve : BehaviorComponent
{
	public BehaviorReserve(Formation formation)
		: base(formation)
	{
		_behaviorSide = formation.AI.Side;
		CalculateCurrentOrder();
	}

	protected override void CalculateCurrentOrder()
	{
		Formation formation = base.Formation.Team.FormationsIncludingEmpty.FirstOrDefaultQ((Formation f) => f.CountOfUnits > 0 && f != base.Formation && f.AI.IsMainFormation);
		WorldPosition position;
		if (formation != null)
		{
			position = formation.CachedMedianPosition;
			Vec2 vec = (base.Formation.QuerySystem.Team.AverageEnemyPosition - formation.CachedMedianPosition.AsVec2).Normalized();
			position.SetVec2(position.AsVec2 - vec * (40f + base.Formation.Depth));
		}
		else
		{
			Vec2 zero = Vec2.Zero;
			int num = 0;
			foreach (Formation item in base.Formation.Team.FormationsIncludingSpecialAndEmpty)
			{
				if (item.CountOfUnits > 0 && item != base.Formation)
				{
					zero += item.CachedMedianPosition.AsVec2;
					num++;
				}
			}
			if (num <= 0)
			{
				base.CurrentOrder = MovementOrder.MovementOrderStop;
				return;
			}
			WorldPosition worldPosition = WorldPosition.Invalid;
			float num2 = float.MaxValue;
			zero *= 1f / (float)num;
			foreach (Formation item2 in base.Formation.Team.FormationsIncludingSpecialAndEmpty)
			{
				if (item2.CountOfUnits > 0 && item2 != base.Formation)
				{
					float num3 = zero.DistanceSquared(item2.CachedMedianPosition.AsVec2);
					if (num3 < num2)
					{
						num2 = num3;
						worldPosition = item2.CachedMedianPosition;
					}
				}
			}
			Vec2 vec2 = (base.Formation.QuerySystem.Team.AverageEnemyPosition - worldPosition.AsVec2).Normalized();
			position = worldPosition;
			position.SetVec2(position.AsVec2 - vec2 * (20f + base.Formation.Depth));
		}
		base.CurrentOrder = MovementOrder.MovementOrderMove(position);
	}

	public override void TickOccasionally()
	{
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
	}

	protected override void OnBehaviorActivatedAux()
	{
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
		base.Formation.SetFacingOrder(FacingOrder.FacingOrderLookAtEnemy);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderWider);
	}

	protected override float GetAiWeight()
	{
		if (!base.Formation.AI.IsMainFormation)
		{
			foreach (Formation item in base.Formation.Team.FormationsIncludingSpecialAndEmpty)
			{
				if (base.Formation == item || item.CountOfUnits <= 0)
				{
					continue;
				}
				foreach (Team team in Mission.Current.Teams)
				{
					if (!team.IsEnemyOf(base.Formation.Team))
					{
						continue;
					}
					foreach (Formation item2 in team.FormationsIncludingSpecialAndEmpty)
					{
						if (item2.CountOfUnits > 0)
						{
							return 0.04f;
						}
					}
				}
				break;
			}
		}
		return 0f;
	}
}
