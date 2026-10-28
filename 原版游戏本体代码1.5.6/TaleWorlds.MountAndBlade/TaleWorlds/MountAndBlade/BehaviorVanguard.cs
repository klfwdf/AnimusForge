using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public class BehaviorVanguard : BehaviorComponent
{
	private Formation _mainFormation;

	public BehaviorVanguard(Formation formation)
		: base(formation)
	{
		_behaviorSide = formation.AI.Side;
		_mainFormation = formation.Team.FormationsIncludingEmpty.FirstOrDefaultQ((Formation f) => f.CountOfUnits > 0 && f.AI.IsMainFormation);
		CalculateCurrentOrder();
	}

	protected override void CalculateCurrentOrder()
	{
		if (_mainFormation != null && _mainFormation.CountOfUnits == 0)
		{
			_mainFormation = base.Formation.Team.FormationsIncludingEmpty.FirstOrDefaultQ((Formation f) => f.CountOfUnits > 0 && f.AI.IsMainFormation);
		}
		Vec2 direction;
		WorldPosition cachedMedianPosition;
		if (_mainFormation != null)
		{
			direction = _mainFormation.Direction;
			Vec2 vec = (base.Formation.QuerySystem.Team.MedianTargetFormationPosition.AsVec2 - _mainFormation.CachedMedianPosition.AsVec2).Normalized();
			cachedMedianPosition = _mainFormation.CachedMedianPosition;
			cachedMedianPosition.SetVec2(_mainFormation.CurrentPosition + vec * ((_mainFormation.Depth + base.Formation.Depth) * 0.5f + 10f));
		}
		else
		{
			direction = base.Formation.Direction;
			cachedMedianPosition = base.Formation.CachedMedianPosition;
			cachedMedianPosition.SetVec2(base.Formation.CachedAveragePosition);
		}
		base.CurrentOrder = MovementOrder.MovementOrderMove(cachedMedianPosition);
		CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(direction);
	}

	public override void OnValidBehaviorSideChanged()
	{
		base.OnValidBehaviorSideChanged();
		_mainFormation = base.Formation.Team.FormationsIncludingEmpty.FirstOrDefaultQ((Formation f) => f.CountOfUnits > 0 && f.AI.IsMainFormation);
	}

	public override void TickOccasionally()
	{
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		if (base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null && base.Formation.CachedAveragePosition.DistanceSquared(base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition.AsVec2) > 1600f && base.Formation.QuerySystem.UnderRangedAttackRatio > 0.2f - ((base.Formation.ArrangementOrder.OrderEnum == ArrangementOrder.ArrangementOrderEnum.Loose) ? 0.1f : 0f))
		{
			base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
		}
		else
		{
			base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
		}
	}

	protected override void OnBehaviorActivatedAux()
	{
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderWide);
	}

	public override TextObject GetBehaviorString()
	{
		TextObject behaviorString = base.GetBehaviorString();
		TextObject variable = GameTexts.FindText("str_formation_ai_side_strings", base.Formation.AI.Side.ToString());
		behaviorString.SetTextVariable("SIDE_STRING", variable);
		if (_mainFormation != null)
		{
			behaviorString.SetTextVariable("AI_SIDE", GameTexts.FindText("str_formation_ai_side_strings", _mainFormation.AI.Side.ToString()));
			behaviorString.SetTextVariable("CLASS", GameTexts.FindText("str_formation_class_string", _mainFormation.PhysicalClass.GetName()));
		}
		return behaviorString;
	}

	protected override float GetAiWeight()
	{
		if (_mainFormation == null || !_mainFormation.AI.IsMainFormation)
		{
			_mainFormation = base.Formation.Team.FormationsIncludingEmpty.FirstOrDefaultQ((Formation f) => f.CountOfUnits > 0 && f.AI.IsMainFormation);
		}
		if (_mainFormation == null || base.Formation.AI.IsMainFormation)
		{
			return 0f;
		}
		return 1.2f;
	}
}
