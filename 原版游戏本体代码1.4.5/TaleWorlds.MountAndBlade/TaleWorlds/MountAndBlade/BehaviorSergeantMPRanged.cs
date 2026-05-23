using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade.Objects;

namespace TaleWorlds.MountAndBlade;

public class BehaviorSergeantMPRanged : BehaviorComponent
{
	private List<FlagCapturePoint> _flagpositions;

	private Formation _attachedInfantry;

	private MissionMultiplayerFlagDomination _flagDominationGameMode;

	public BehaviorSergeantMPRanged(Formation formation)
		: base(formation)
	{
		_flagpositions = base.Formation.Team.Mission.ActiveMissionObjects.FindAllWithType<FlagCapturePoint>().ToList();
		_flagDominationGameMode = base.Formation.Team.Mission.GetMissionBehavior<MissionMultiplayerFlagDomination>();
		CalculateCurrentOrder();
	}

	protected override void CalculateCurrentOrder()
	{
		bool flag = false;
		Formation formation = null;
		float num = float.MaxValue;
		foreach (Team team in base.Formation.Team.Mission.Teams)
		{
			if (!team.IsEnemyOf(base.Formation.Team))
			{
				continue;
			}
			for (int i = 0; i < Math.Min(team.FormationsIncludingSpecialAndEmpty.Count, 8); i++)
			{
				Formation formation2 = team.FormationsIncludingSpecialAndEmpty[i];
				if (formation2.CountOfUnits <= 0)
				{
					continue;
				}
				flag = true;
				if (formation2.QuerySystem.IsCavalryFormation || formation2.QuerySystem.IsRangedCavalryFormation)
				{
					float num2 = formation2.CachedMedianPosition.AsVec2.DistanceSquared(base.Formation.CachedAveragePosition);
					if (num2 < num)
					{
						num = num2;
						formation = formation2;
					}
				}
			}
		}
		if (base.Formation.Team.FormationsIncludingEmpty.AnyQ((Formation f) => f.CountOfUnits > 0 && f != base.Formation && f.QuerySystem.IsInfantryFormation))
		{
			_attachedInfantry = TaleWorlds.Core.Extensions.MinBy(base.Formation.Team.FormationsIncludingEmpty.Where((Formation f) => f.CountOfUnits > 0 && f != base.Formation && f.QuerySystem.IsInfantryFormation), (Formation f) => f.CachedMedianPosition.AsVec2.DistanceSquared(base.Formation.CachedAveragePosition));
			Formation formation3 = null;
			if (flag)
			{
				if (base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition.AsVec2.DistanceSquared(base.Formation.CachedAveragePosition) <= 4900f)
				{
					formation3 = base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation;
				}
				else if (formation != null)
				{
					formation3 = formation;
				}
			}
			Vec2 vec = ((formation3 == null) ? _attachedInfantry.Direction : (formation3.CachedMedianPosition.AsVec2 - _attachedInfantry.CachedMedianPosition.AsVec2).Normalized());
			WorldPosition cachedMedianPosition = _attachedInfantry.CachedMedianPosition;
			cachedMedianPosition.SetVec2(cachedMedianPosition.AsVec2 - vec * ((_attachedInfantry.Depth + base.Formation.Depth) / 2f));
			base.CurrentOrder = MovementOrder.MovementOrderMove(cachedMedianPosition);
			CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec);
		}
		else if (base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null && base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition.AsVec2.DistanceSquared(base.Formation.CachedAveragePosition) <= 4900f)
		{
			Vec2 vec2 = (base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition.AsVec2 - base.Formation.CachedAveragePosition).Normalized();
			float num3 = base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition.AsVec2.Distance(base.Formation.CachedAveragePosition);
			WorldPosition cachedMedianPosition2 = base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition;
			if (num3 > base.Formation.QuerySystem.MissileRangeAdjusted)
			{
				cachedMedianPosition2.SetVec2(cachedMedianPosition2.AsVec2 - vec2 * (base.Formation.QuerySystem.MissileRangeAdjusted - base.Formation.Depth * 0.5f));
			}
			else if (num3 < base.Formation.QuerySystem.MissileRangeAdjusted * 0.4f)
			{
				cachedMedianPosition2.SetVec2(cachedMedianPosition2.AsVec2 - vec2 * (base.Formation.QuerySystem.MissileRangeAdjusted * 0.4f));
			}
			else
			{
				cachedMedianPosition2.SetVec2(base.Formation.CachedAveragePosition);
			}
			base.CurrentOrder = MovementOrder.MovementOrderMove(cachedMedianPosition2);
			CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec2);
		}
		else if (_flagpositions.Any((FlagCapturePoint fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) != base.Formation.Team))
		{
			Vec3 position = TaleWorlds.Core.Extensions.MinBy(_flagpositions.Where((FlagCapturePoint fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) != base.Formation.Team), (FlagCapturePoint fp) => fp.Position.AsVec2.DistanceSquared(base.Formation.CachedAveragePosition)).Position;
			if (base.CurrentOrder.OrderEnum == MovementOrder.MovementOrderEnum.Invalid || base.CurrentOrder.GetPosition(base.Formation) != position.AsVec2)
			{
				Vec2 direction = ((base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null) ? (base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition.AsVec2 - base.Formation.CachedAveragePosition).Normalized() : base.Formation.Direction);
				WorldPosition position2 = new WorldPosition(base.Formation.Team.Mission.Scene, UIntPtr.Zero, position, hasValidZ: false);
				base.CurrentOrder = MovementOrder.MovementOrderMove(position2);
				CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(direction);
			}
		}
		else if (_flagpositions.Any((FlagCapturePoint fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) == base.Formation.Team))
		{
			Vec3 position3 = TaleWorlds.Core.Extensions.MinBy(_flagpositions.Where((FlagCapturePoint fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) == base.Formation.Team), (FlagCapturePoint fp) => fp.Position.AsVec2.DistanceSquared(base.Formation.CachedAveragePosition)).Position;
			base.CurrentOrder = MovementOrder.MovementOrderMove(new WorldPosition(base.Formation.Team.Mission.Scene, UIntPtr.Zero, position3, hasValidZ: false));
			CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
		}
		else
		{
			WorldPosition cachedMedianPosition3 = base.Formation.CachedMedianPosition;
			cachedMedianPosition3.SetVec2(base.Formation.CachedAveragePosition);
			base.CurrentOrder = MovementOrder.MovementOrderMove(cachedMedianPosition3);
			CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
		}
	}

	public override void TickOccasionally()
	{
		_flagpositions.RemoveAll((FlagCapturePoint fp) => fp.IsDeactivated);
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
	}

	protected override void OnBehaviorActivatedAux()
	{
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderWide);
	}

	protected override float GetAiWeight()
	{
		if (base.Formation.QuerySystem.IsRangedFormation)
		{
			return 1.2f;
		}
		return 0f;
	}
}
