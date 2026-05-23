using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public class BehaviorDestroySiegeWeapons : BehaviorComponent
{
	private readonly List<SiegeWeapon> _allWeapons;

	private List<SiegeWeapon> _targetWeapons;

	public SiegeWeapon LastTargetWeapon;

	private bool _isTargetPrimaryWeapon;

	public override float NavmeshlessTargetPositionPenalty => 1f;

	private void DetermineTargetWeapons()
	{
		_targetWeapons = _allWeapons.Where((SiegeWeapon w) => w is IPrimarySiegeWeapon && (w as IPrimarySiegeWeapon).WeaponSide == _behaviorSide && w.IsDestructible && !w.IsDestroyed && !w.IsDisabled).ToList();
		if (_targetWeapons.IsEmpty())
		{
			_targetWeapons = _allWeapons.Where((SiegeWeapon w) => !(w is IPrimarySiegeWeapon) && w.IsDestructible && !w.IsDestroyed && !w.IsDisabled).ToList();
			_isTargetPrimaryWeapon = false;
		}
		else
		{
			_isTargetPrimaryWeapon = true;
		}
	}

	public BehaviorDestroySiegeWeapons(Formation formation)
		: base(formation)
	{
		base.BehaviorCoherence = 0.2f;
		_behaviorSide = formation.AI.Side;
		_allWeapons = (from sw in Mission.Current.ActiveMissionObjects.FindAllWithType<SiegeWeapon>()
			where sw.Side != formation.Team.Side
			select sw).ToList();
		DetermineTargetWeapons();
		base.CurrentOrder = MovementOrder.MovementOrderCharge;
	}

	public override TextObject GetBehaviorString()
	{
		TextObject behaviorString = base.GetBehaviorString();
		TextObject variable = GameTexts.FindText("str_formation_ai_side_strings", base.Formation.AI.Side.ToString());
		behaviorString.SetTextVariable("SIDE_STRING", variable);
		behaviorString.SetTextVariable("IS_GENERAL_SIDE", "0");
		return behaviorString;
	}

	public override void OnValidBehaviorSideChanged()
	{
		base.OnValidBehaviorSideChanged();
		DetermineTargetWeapons();
	}

	public override void TickOccasionally()
	{
		base.TickOccasionally();
		_targetWeapons.RemoveAll((SiegeWeapon tw) => tw.IsDestroyed);
		if (_targetWeapons.Count == 0)
		{
			DetermineTargetWeapons();
		}
		if (base.Formation.AI.ActiveBehavior != this)
		{
			return;
		}
		if (_targetWeapons.Count == 0)
		{
			if (base.CurrentOrder != MovementOrder.MovementOrderCharge)
			{
				base.CurrentOrder = MovementOrder.MovementOrderCharge;
			}
			_isTargetPrimaryWeapon = false;
		}
		else
		{
			SiegeWeapon siegeWeapon = Extensions.MinBy(_targetWeapons, (SiegeWeapon tw) => base.Formation.CachedAveragePosition.DistanceSquared(tw.GameEntity.GlobalPosition.AsVec2));
			if (base.CurrentOrder.OrderEnum != MovementOrder.MovementOrderEnum.AttackEntity || LastTargetWeapon != siegeWeapon)
			{
				LastTargetWeapon = siegeWeapon;
				base.CurrentOrder = MovementOrder.MovementOrderAttackEntity(GameEntity.CreateFromWeakEntity(LastTargetWeapon.GameEntity), surroundEntity: true);
			}
		}
		base.Formation.SetMovementOrder(base.CurrentOrder);
	}

	protected override void OnBehaviorActivatedAux()
	{
		DetermineTargetWeapons();
		base.Formation.SetArrangementOrder((base.Formation.QuerySystem.IsCavalryFormation || base.Formation.QuerySystem.IsRangedCavalryFormation) ? ArrangementOrder.ArrangementOrderSkein : ArrangementOrder.ArrangementOrderLine);
		base.Formation.SetFacingOrder(FacingOrder.FacingOrderLookAtEnemy);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderDeep);
	}

	protected override float GetAiWeight()
	{
		if (!_targetWeapons.IsEmpty())
		{
			if (!_isTargetPrimaryWeapon)
			{
				return 0.7f;
			}
			return 1f;
		}
		return 0f;
	}
}
