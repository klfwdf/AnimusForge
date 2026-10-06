using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class BehaviorPullBack : BehaviorComponent
{
	public BehaviorPullBack(Formation formation)
		: base(formation)
	{
		CalculateCurrentOrder();
		base.BehaviorCoherence = 0.2f;
	}

	protected override void CalculateCurrentOrder()
	{
		WorldPosition cachedMedianPosition = base.Formation.CachedMedianPosition;
		FormationQuerySystem cachedClosestEnemyFormation = base.Formation.CachedClosestEnemyFormation;
		if (cachedClosestEnemyFormation == null)
		{
			cachedMedianPosition.SetVec2(base.Formation.CachedAveragePosition);
		}
		else
		{
			Vec2 vec = (base.Formation.CachedAveragePosition - cachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2).Normalized();
			cachedMedianPosition.SetVec2(base.Formation.CachedAveragePosition + 50f * vec);
		}
		base.CurrentOrder = MovementOrder.MovementOrderMove(cachedMedianPosition);
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
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
		base.Formation.SetFacingOrder(FacingOrder.FacingOrderLookAtEnemy);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderWide);
	}

	protected override float GetAiWeight()
	{
		if (base.Formation.Team.TeamAI is TeamAISiegeComponent && !(base.Formation.Team.TeamAI is TeamAISallyOutAttacker) && !(base.Formation.Team.TeamAI is TeamAISallyOutDefender))
		{
			return GetSiegeAIWeight();
		}
		FormationQuerySystem querySystem = base.Formation.QuerySystem;
		FormationQuerySystem formationQuerySystem = querySystem.ClosestSignificantlyLargeEnemyFormation;
		if (formationQuerySystem == null || formationQuerySystem.Formation.CachedClosestEnemyFormation != querySystem || formationQuerySystem.MovementSpeedMaximum - querySystem.MovementSpeedMaximum > 2f)
		{
			formationQuerySystem = base.Formation.CachedClosestEnemyFormation;
			if (formationQuerySystem == null || formationQuerySystem.Formation.CachedClosestEnemyFormation != querySystem || formationQuerySystem.MovementSpeedMaximum - querySystem.MovementSpeedMaximum > 2f)
			{
				return 0f;
			}
		}
		float num = base.Formation.CachedAveragePosition.Distance(formationQuerySystem.Formation.CachedMedianPosition.AsVec2) / formationQuerySystem.MovementSpeedMaximum;
		float num2 = MBMath.ClampFloat(num, 4f, 10f);
		float num3 = MBMath.Lerp(0.1f, 1f, 1f - (num2 - 4f) / 6f);
		float num4 = 0f;
		foreach (Team team in Mission.Current.Teams)
		{
			if (!team.IsEnemyOf(base.Formation.Team))
			{
				continue;
			}
			foreach (Formation item in team.FormationsIncludingSpecialAndEmpty)
			{
				if (item.CountOfUnits <= 0 || item == formationQuerySystem.Formation)
				{
					continue;
				}
				float num5 = item.CachedMedianPosition.AsVec2.Distance(formationQuerySystem.Formation.CachedMedianPosition.AsVec2) / item.QuerySystem.MovementSpeedMaximum;
				if (!(num5 <= num + 4f) || (!(num > 8f) && item.CachedClosestEnemyFormation != base.Formation.QuerySystem))
				{
					continue;
				}
				bool flag = false;
				if (num <= 8f)
				{
					foreach (Team team2 in base.Formation.Team.Mission.Teams)
					{
						if (!team2.IsFriendOf(base.Formation.Team))
						{
							continue;
						}
						foreach (Formation item2 in team2.FormationsIncludingSpecialAndEmpty)
						{
							if (item2.CountOfUnits > 0 && item2 != base.Formation && item2.CachedClosestEnemyFormation == item.QuerySystem && item2.CachedMedianPosition.AsVec2.DistanceSquared(base.Formation.CachedAveragePosition) / item2.QuerySystem.MovementSpeedMaximum < num5 + 4f)
							{
								flag = true;
								break;
							}
						}
						if (flag)
						{
							break;
						}
					}
				}
				if (!flag)
				{
					num4 += item.QuerySystem.FormationMeleeFightingPower * item.QuerySystem.GetClassWeightedFactor(1f, 1f, 1f, 1f);
				}
			}
		}
		float num6 = 0f;
		foreach (Team team3 in Mission.Current.Teams)
		{
			if (!team3.IsFriendOf(base.Formation.Team))
			{
				continue;
			}
			foreach (Formation item3 in team3.FormationsIncludingSpecialAndEmpty)
			{
				if (item3.CountOfUnits > 0 && item3 != base.Formation && item3.CachedClosestEnemyFormation == formationQuerySystem && item3.CachedMedianPosition.AsVec2.Distance(item3.CachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2) / item3.QuerySystem.MovementSpeedMaximum < 4f)
				{
					num6 += item3.QuerySystem.FormationMeleeFightingPower * item3.QuerySystem.GetClassWeightedFactor(1f, 1f, 1f, 1f);
				}
			}
		}
		return MBMath.ClampFloat((1f + num4 + formationQuerySystem.Formation.QuerySystem.FormationMeleeFightingPower * formationQuerySystem.GetClassWeightedFactor(1f, 1f, 1f, 1f)) / (base.Formation.GetFormationMeleeFightingPower() * querySystem.GetClassWeightedFactor(1f, 1f, 1f, 1f) + num6 + 1f) * querySystem.Team.RemainingPowerRatio / 3f, 0.1f, 1.21f) * num3;
	}

	private float GetSiegeAIWeight()
	{
		return 0f;
	}
}
