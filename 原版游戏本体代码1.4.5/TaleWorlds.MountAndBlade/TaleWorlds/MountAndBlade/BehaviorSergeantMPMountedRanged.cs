using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Objects;

namespace TaleWorlds.MountAndBlade;

public class BehaviorSergeantMPMountedRanged : BehaviorComponent
{
	private List<FlagCapturePoint> _flagpositions;

	private MissionMultiplayerFlagDomination _flagDominationGameMode;

	public BehaviorSergeantMPMountedRanged(Formation formation)
		: base(formation)
	{
		_flagpositions = base.Formation.Team.Mission.ActiveMissionObjects.FindAllWithType<FlagCapturePoint>().ToList();
		_flagDominationGameMode = base.Formation.Team.Mission.GetMissionBehavior<MissionMultiplayerFlagDomination>();
		CalculateCurrentOrder();
	}

	private MovementOrder UncapturedFlagMoveOrder()
	{
		if (_flagpositions.Any((FlagCapturePoint fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) != base.Formation.Team))
		{
			FlagCapturePoint flagCapturePoint = TaleWorlds.Core.Extensions.MinBy(_flagpositions.Where((FlagCapturePoint fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) != base.Formation.Team), (FlagCapturePoint fp) => base.Formation.Team.QuerySystem.GetLocalEnemyPower(fp.Position.AsVec2));
			return MovementOrder.MovementOrderMove(new WorldPosition(base.Formation.Team.Mission.Scene, UIntPtr.Zero, flagCapturePoint.Position, hasValidZ: false));
		}
		if (_flagpositions.Any((FlagCapturePoint fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) == base.Formation.Team))
		{
			Vec3 position = TaleWorlds.Core.Extensions.MinBy(_flagpositions.Where((FlagCapturePoint fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) == base.Formation.Team), (FlagCapturePoint fp) => fp.Position.AsVec2.DistanceSquared(base.Formation.CachedAveragePosition)).Position;
			return MovementOrder.MovementOrderMove(new WorldPosition(base.Formation.Team.Mission.Scene, UIntPtr.Zero, position, hasValidZ: false));
		}
		return MovementOrder.MovementOrderStop;
	}

	protected override void CalculateCurrentOrder()
	{
		if (base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation == null || base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition.AsVec2.DistanceSquared(base.Formation.CachedAveragePosition) > 2500f)
		{
			base.CurrentOrder = UncapturedFlagMoveOrder();
			CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
			return;
		}
		FlagCapturePoint flagCapturePoint = null;
		if (_flagpositions.Any((FlagCapturePoint fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) != base.Formation.Team && !fp.IsContested))
		{
			flagCapturePoint = TaleWorlds.Core.Extensions.MinBy(_flagpositions.Where((FlagCapturePoint fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) != base.Formation.Team && !fp.IsContested), (FlagCapturePoint fp) => base.Formation.CachedAveragePosition.DistanceSquared(fp.Position.AsVec2));
		}
		if (!base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.IsInfantryFormation && flagCapturePoint != null)
		{
			WorldPosition position = new WorldPosition(base.Formation.Team.Mission.Scene, UIntPtr.Zero, flagCapturePoint.Position, hasValidZ: false);
			base.CurrentOrder = MovementOrder.MovementOrderMove(position);
			CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
			return;
		}
		if (base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.IsRangedFormation)
		{
			base.CurrentOrder = MovementOrder.MovementOrderChargeToTarget(base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation);
			CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
			return;
		}
		Vec2 vec = base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition.AsVec2 - base.Formation.CachedAveragePosition;
		float num = vec.Normalize();
		WorldPosition cachedMedianPosition = base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition;
		if (num > base.Formation.QuerySystem.MissileRangeAdjusted)
		{
			cachedMedianPosition.SetVec2(cachedMedianPosition.AsVec2 - vec * (base.Formation.QuerySystem.MissileRangeAdjusted - base.Formation.Depth * 0.5f));
		}
		else if (num < base.Formation.QuerySystem.MissileRangeAdjusted * 0.4f)
		{
			cachedMedianPosition.SetVec2(cachedMedianPosition.AsVec2 - vec * (base.Formation.QuerySystem.MissileRangeAdjusted * 0.7f));
		}
		else
		{
			vec = vec.RightVec();
			cachedMedianPosition.SetVec2(base.Formation.CachedAveragePosition + vec * 20f);
		}
		base.CurrentOrder = MovementOrder.MovementOrderMove(cachedMedianPosition);
		CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec);
	}

	public override void TickOccasionally()
	{
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		if (base.CurrentOrder.OrderEnum == MovementOrder.MovementOrderEnum.ChargeToTarget && base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null && base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.IsRangedFormation)
		{
			base.Formation.SetFiringOrder(FiringOrder.FiringOrderHoldYourFire);
		}
		else
		{
			base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		}
	}

	protected override void OnBehaviorActivatedAux()
	{
		_flagpositions.RemoveAll((FlagCapturePoint fp) => fp.IsDeactivated);
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderDeep);
	}

	protected override float GetAiWeight()
	{
		if (base.Formation.QuerySystem.IsRangedCavalryFormation)
		{
			return 1.2f;
		}
		return 0f;
	}
}
