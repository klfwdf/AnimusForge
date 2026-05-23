using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.Objects;

namespace TaleWorlds.MountAndBlade;

public class BehaviorSergeantMPLastFlagLastStand : BehaviorComponent
{
	private List<FlagCapturePoint> _flagpositions;

	private bool _lastEffort;

	private MissionMultiplayerFlagDomination _flagDominationGameMode;

	public BehaviorSergeantMPLastFlagLastStand(Formation formation)
		: base(formation)
	{
		_flagpositions = Mission.Current.ActiveMissionObjects.FindAllWithType<FlagCapturePoint>().ToList();
		_flagDominationGameMode = Mission.Current.GetMissionBehavior<MissionMultiplayerFlagDomination>();
		CalculateCurrentOrder();
	}

	protected override void CalculateCurrentOrder()
	{
		base.CurrentOrder = ((_flagpositions.Count > 0) ? MovementOrder.MovementOrderMove(new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, _flagpositions[0].Position, hasValidZ: false)) : MovementOrder.MovementOrderStop);
	}

	public override void TickOccasionally()
	{
		_flagpositions.RemoveAll((FlagCapturePoint fp) => fp.IsDeactivated);
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderHoldYourFire);
	}

	protected override void OnBehaviorActivatedAux()
	{
		_flagpositions.RemoveAll((FlagCapturePoint fp) => fp.IsDeactivated);
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
		base.Formation.SetFacingOrder(FacingOrder.FacingOrderLookAtEnemy);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderHoldYourFire);
		base.Formation.SetFormOrder(FormOrder.FormOrderDeep);
	}

	protected override float GetAiWeight()
	{
		if (_lastEffort)
		{
			return 10f;
		}
		_flagpositions.RemoveAll((FlagCapturePoint fp) => fp.IsDeactivated);
		FlagCapturePoint flagCapturePoint = _flagpositions.FirstOrDefault();
		if (_flagpositions.Count != 1 || _flagDominationGameMode.GetFlagOwnerTeam(flagCapturePoint) == null || !_flagDominationGameMode.GetFlagOwnerTeam(flagCapturePoint).IsEnemyOf(base.Formation.Team))
		{
			return 0f;
		}
		float timeUntilBattleSideVictory = _flagDominationGameMode.GetTimeUntilBattleSideVictory(_flagDominationGameMode.GetFlagOwnerTeam(flagCapturePoint).Side);
		if (timeUntilBattleSideVictory <= 60f)
		{
			return 10f;
		}
		float num = base.Formation.CachedAveragePosition.Distance(flagCapturePoint.Position.AsVec2);
		float movementSpeedMaximum = base.Formation.QuerySystem.MovementSpeedMaximum;
		if (num / movementSpeedMaximum * 8f > timeUntilBattleSideVictory)
		{
			_lastEffort = true;
			return 10f;
		}
		return 0f;
	}
}
