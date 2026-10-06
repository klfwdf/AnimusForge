using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public class BehaviorUseSiegeMachines : BehaviorComponent
{
	private enum BehaviorState
	{
		Unset,
		Follow,
		ClimbSiegeTower,
		Stop
	}

	private List<SiegeWeapon> _primarySiegeWeapons;

	private TeamAISiegeComponent _teamAISiegeComponent;

	private MovementOrder _followEntityOrder;

	private GameEntity _followedEntity;

	private MovementOrder _stopOrder;

	private BehaviorState _behaviorState;

	public override float NavmeshlessTargetPositionPenalty => 1f;

	public BehaviorUseSiegeMachines(Formation formation)
		: base(formation)
	{
		_behaviorSide = formation.AI.Side;
		_primarySiegeWeapons = new List<SiegeWeapon>();
		foreach (MissionObject activeMissionObject in Mission.Current.ActiveMissionObjects)
		{
			if (activeMissionObject is IPrimarySiegeWeapon primarySiegeWeapon && primarySiegeWeapon.WeaponSide == _behaviorSide)
			{
				_primarySiegeWeapons.Add(activeMissionObject as SiegeWeapon);
			}
		}
		_teamAISiegeComponent = (TeamAISiegeComponent)formation.Team.TeamAI;
		base.BehaviorCoherence = 0f;
		_stopOrder = MovementOrder.MovementOrderStop;
		RecreateFollowEntityOrder();
		if (_followEntityOrder.OrderEnum != MovementOrder.MovementOrderEnum.Invalid)
		{
			_behaviorState = BehaviorState.Follow;
			base.CurrentOrder = _followEntityOrder;
		}
		else
		{
			_behaviorState = BehaviorState.Stop;
			base.CurrentOrder = _stopOrder;
		}
	}

	public override TextObject GetBehaviorString()
	{
		TextObject behaviorString = base.GetBehaviorString();
		TextObject variable = GameTexts.FindText("str_formation_ai_side_strings", base.Formation.AI.Side.ToString());
		behaviorString.SetTextVariable("SIDE_STRING", variable);
		behaviorString.SetTextVariable("IS_GENERAL_SIDE", "0");
		return behaviorString;
	}

	private void RecreateFollowEntityOrder()
	{
		_followEntityOrder = MovementOrder.MovementOrderStop;
		_followedEntity = _primarySiegeWeapons.FirstOrDefault((SiegeWeapon psw) => !psw.IsDeactivated && psw is IPrimarySiegeWeapon primarySiegeWeapon && !primarySiegeWeapon.HasCompletedAction())?.WaitEntity;
		if (_followedEntity != null)
		{
			_followEntityOrder = MovementOrder.MovementOrderFollowEntity(_followedEntity);
		}
	}

	public override void OnValidBehaviorSideChanged()
	{
		base.OnValidBehaviorSideChanged();
		_primarySiegeWeapons.Clear();
		foreach (MissionObject activeMissionObject in Mission.Current.ActiveMissionObjects)
		{
			if (activeMissionObject is IPrimarySiegeWeapon primarySiegeWeapon && primarySiegeWeapon.WeaponSide == _behaviorSide && !((SiegeWeapon)activeMissionObject).IsDeactivated)
			{
				_primarySiegeWeapons.Add(activeMissionObject as SiegeWeapon);
			}
		}
		RecreateFollowEntityOrder();
		_behaviorState = BehaviorState.Unset;
	}

	public override void TickOccasionally()
	{
		base.TickOccasionally();
		bool flag = false;
		for (int num = _primarySiegeWeapons.Count - 1; num >= 0; num--)
		{
			SiegeWeapon siegeWeapon = _primarySiegeWeapons[num];
			if (siegeWeapon.IsDestroyed || siegeWeapon.IsDeactivated)
			{
				_primarySiegeWeapons.RemoveAt(num);
				flag = true;
			}
		}
		if (flag)
		{
			RecreateFollowEntityOrder();
		}
		int num2 = 0;
		SiegeTower siegeTower = null;
		foreach (SiegeWeapon primarySiegeWeapon in _primarySiegeWeapons)
		{
			if (!((IPrimarySiegeWeapon)primarySiegeWeapon).HasCompletedAction())
			{
				num2++;
				if (primarySiegeWeapon is SiegeTower siegeTower2)
				{
					siegeTower = siegeTower2;
				}
			}
		}
		if (num2 == 0)
		{
			base.CurrentOrder = _stopOrder;
			return;
		}
		if (_behaviorState == BehaviorState.Follow)
		{
			if (_followEntityOrder.OrderEnum == MovementOrder.MovementOrderEnum.Stop)
			{
				RecreateFollowEntityOrder();
			}
			base.CurrentOrder = _followEntityOrder;
		}
		BehaviorState behaviorState = ((siegeTower != null && siegeTower.HasArrivedAtTarget) ? BehaviorState.ClimbSiegeTower : ((_followEntityOrder.OrderEnum != MovementOrder.MovementOrderEnum.Invalid) ? BehaviorState.Follow : BehaviorState.Stop));
		if (behaviorState != _behaviorState)
		{
			switch (behaviorState)
			{
			case BehaviorState.Follow:
				base.CurrentOrder = _followEntityOrder;
				break;
			case BehaviorState.ClimbSiegeTower:
				RecreateFollowEntityOrder();
				base.CurrentOrder = _followEntityOrder;
				break;
			default:
				base.CurrentOrder = _stopOrder;
				break;
			}
			_behaviorState = behaviorState;
			bool flag2 = _behaviorState == BehaviorState.ClimbSiegeTower;
			if (!flag2)
			{
				foreach (SiegeWeapon primarySiegeWeapon2 in _primarySiegeWeapons)
				{
					if (primarySiegeWeapon2 is SiegeLadder { IsDisabled: false })
					{
						flag2 = true;
						break;
					}
				}
			}
			if (flag2)
			{
				base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
			}
			else if (base.Formation.QuerySystem.IsRangedFormation)
			{
				base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderScatter);
			}
			else
			{
				base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderShieldWall);
			}
		}
		if (_followedEntity != null && (_behaviorState == BehaviorState.Follow || _behaviorState == BehaviorState.ClimbSiegeTower))
		{
			base.Formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(_followedEntity.GetGlobalFrame().rotation.f.AsVec2.Normalized()));
		}
		else
		{
			base.Formation.SetFacingOrder(FacingOrder.FacingOrderLookAtEnemy);
		}
		if (base.Formation.AI.ActiveBehavior == this)
		{
			foreach (SiegeWeapon primarySiegeWeapon3 in _primarySiegeWeapons)
			{
				if (((IPrimarySiegeWeapon)primarySiegeWeapon3).HasCompletedAction())
				{
					continue;
				}
				if (!primarySiegeWeapon3.IsUsedByFormation(base.Formation))
				{
					base.Formation.StartUsingMachine(primarySiegeWeapon3);
				}
				for (int num3 = primarySiegeWeapon3.UserFormations.Count - 1; num3 >= 0; num3--)
				{
					Formation formation = primarySiegeWeapon3.UserFormations[num3];
					if (formation != base.Formation && formation.IsAIControlled && (formation.AI.Side != _behaviorSide || !(formation.AI.ActiveBehavior is BehaviorUseSiegeMachines)) && formation.Team == base.Formation.Team)
					{
						formation.StopUsingMachine(primarySiegeWeapon3);
					}
				}
			}
		}
		base.Formation.SetMovementOrder(base.CurrentOrder);
	}

	protected override void OnBehaviorActivatedAux()
	{
		base.Formation.SetArrangementOrder(base.Formation.QuerySystem.IsRangedFormation ? ArrangementOrder.ArrangementOrderScatter : ArrangementOrder.ArrangementOrderShieldWall);
		base.Formation.SetFacingOrder(FacingOrder.FacingOrderLookAtEnemy);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderDeep);
	}

	protected override float GetAiWeight()
	{
		float result = 0f;
		if (_teamAISiegeComponent != null && _primarySiegeWeapons.Count > 0 && _primarySiegeWeapons.All((SiegeWeapon psw) => !(psw as IPrimarySiegeWeapon).HasCompletedAction()))
		{
			result = ((!_teamAISiegeComponent.IsCastleBreached()) ? 0.75f : 0.25f);
		}
		return result;
	}
}
