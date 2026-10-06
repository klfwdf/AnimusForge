using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.LinQuick;

namespace TaleWorlds.MountAndBlade;

public class BehaviorGeneral : BehaviorComponent
{
	private Formation _mainFormation;

	public BehaviorGeneral(Formation formation)
		: base(formation)
	{
		_mainFormation = formation.Team.FormationsIncludingEmpty.FirstOrDefaultQ((Formation f) => f.CountOfUnits > 0 && f.AI.IsMainFormation);
		CalculateCurrentOrder();
	}

	protected override void CalculateCurrentOrder()
	{
		bool flag = false;
		bool flag2 = false;
		foreach (Formation item in base.Formation.Team.FormationsIncludingEmpty)
		{
			if (item.CountOfUnits > 0)
			{
				flag = true;
				if (item.GetReadonlyMovementOrderReference().OrderEnum != MovementOrder.MovementOrderEnum.Retreat)
				{
					flag2 = false;
					break;
				}
				flag2 = true;
			}
		}
		if (!flag)
		{
			base.CurrentOrder = MovementOrder.MovementOrderCharge;
			return;
		}
		if (flag2)
		{
			base.CurrentOrder = MovementOrder.MovementOrderRetreat;
			return;
		}
		bool flag3 = false;
		foreach (Team team in Mission.Current.Teams)
		{
			if (team.IsEnemyOf(base.Formation.Team) && team.HasAnyFormationsIncludingSpecialThatIsNotEmpty())
			{
				flag3 = true;
				break;
			}
		}
		WorldPosition position;
		if (flag3 && base.Formation.Team.HasAnyFormationsIncludingSpecialThatIsNotEmpty())
		{
			float num = ((base.Formation.PhysicalClass.IsMounted() && base.Formation.Team.QuerySystem.CavalryRatio + base.Formation.Team.QuerySystem.RangedCavalryRatio >= 33.3f) ? 40f : 3f);
			if (_mainFormation != null && _mainFormation.CountOfUnits > 0)
			{
				float num2 = _mainFormation.Depth + num;
				position = _mainFormation.CachedMedianPosition;
				position.SetVec2(position.AsVec2 - (base.Formation.QuerySystem.Team.MedianTargetFormationPosition.AsVec2 - _mainFormation.CachedMedianPosition.AsVec2).Normalized() * num2);
			}
			else
			{
				position = base.Formation.QuerySystem.Team.MedianPosition;
				position.SetVec2(base.Formation.QuerySystem.Team.AveragePosition - (base.Formation.QuerySystem.Team.MedianTargetFormationPosition.AsVec2 - base.Formation.QuerySystem.Team.AveragePosition).Normalized() * num);
			}
		}
		else
		{
			position = base.Formation.CachedMedianPosition;
			position.SetVec2(base.Formation.CachedAveragePosition);
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
		base.Formation.SetFormOrder(FormOrder.FormOrderDeep);
	}

	protected override float GetAiWeight()
	{
		if (_mainFormation == null || !_mainFormation.AI.IsMainFormation)
		{
			_mainFormation = base.Formation.Team.FormationsIncludingEmpty.FirstOrDefaultQ((Formation f) => f.CountOfUnits > 0 && f.AI.IsMainFormation);
		}
		return 1.2f;
	}
}
