using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public class BehaviorSkirmishLine : BehaviorComponent
{
	private Formation _mainFormation;

	private FormationQuerySystem _targetFormation;

	public BehaviorSkirmishLine(Formation formation)
		: base(formation)
	{
		_behaviorSide = FormationAI.BehaviorSide.BehaviorSideNotSet;
		_mainFormation = formation.Team.FormationsIncludingEmpty.FirstOrDefaultQ((Formation f) => f.CountOfUnits > 0 && f.AI.IsMainFormation);
		CalculateCurrentOrder();
	}

	protected override void CalculateCurrentOrder()
	{
		_targetFormation = base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation ?? base.Formation.CachedClosestEnemyFormation;
		Vec2 vec;
		WorldPosition cachedMedianPosition;
		if (_targetFormation == null || _mainFormation == null)
		{
			vec = base.Formation.Direction;
			cachedMedianPosition = base.Formation.CachedMedianPosition;
			cachedMedianPosition.SetVec2(base.Formation.CachedAveragePosition);
		}
		else
		{
			vec = ((!(_mainFormation.AI.ActiveBehavior is BehaviorCautiousAdvance)) ? ((base.Formation.Direction.DotProduct((_targetFormation.Formation.CachedMedianPosition.AsVec2 - _mainFormation.CachedMedianPosition.AsVec2).Normalized()) < 0.5f) ? (_targetFormation.Formation.CachedMedianPosition.AsVec2 - _mainFormation.CachedMedianPosition.AsVec2) : base.Formation.Direction).Normalized() : _mainFormation.Direction);
			Vec2 vec2 = _mainFormation.OrderPosition - _mainFormation.CachedMedianPosition.AsVec2;
			float num = _mainFormation.CachedMovementSpeed * 7f;
			float length = vec2.Length;
			if (length > 0f)
			{
				float num2 = num / length;
				if (num2 < 1f)
				{
					vec2 *= num2;
				}
			}
			cachedMedianPosition = _mainFormation.CachedMedianPosition;
			cachedMedianPosition.SetVec2(cachedMedianPosition.AsVec2 + vec * 8f + vec2);
		}
		base.CurrentOrder = MovementOrder.MovementOrderMove(cachedMedianPosition);
		if (!CurrentFacingOrder.GetDirection(base.Formation).IsValid || CurrentFacingOrder.OrderEnum == FacingOrder.FacingOrderEnum.LookAtEnemy || (_targetFormation != null && (base.Formation.CachedAveragePosition.DistanceSquared(_targetFormation.Formation.CachedMedianPosition.GetNavMeshVec3().AsVec2) >= base.Formation.QuerySystem.MissileRangeAdjusted * base.Formation.QuerySystem.MissileRangeAdjusted || (!_targetFormation.IsRangedCavalryFormation && CurrentFacingOrder.GetDirection(base.Formation).DotProduct(vec) <= MBMath.Lerp(0.5f, 1f, 1f - MBMath.ClampFloat(base.Formation.Width, 1f, 20f) * 0.05f)))))
		{
			CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec);
		}
	}

	public override TextObject GetBehaviorString()
	{
		TextObject behaviorString = base.GetBehaviorString();
		if (_mainFormation != null)
		{
			behaviorString.SetTextVariable("AI_SIDE", GameTexts.FindText("str_formation_ai_side_strings", _mainFormation.AI.Side.ToString()));
			behaviorString.SetTextVariable("CLASS", GameTexts.FindText("str_formation_class_string", _mainFormation.PhysicalClass.GetName()));
		}
		return behaviorString;
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
		if (_mainFormation != null && base.Formation.Width > _mainFormation.Width * 1.5f)
		{
			base.Formation.SetFormOrder(FormOrder.FormOrderCustom(_mainFormation.Width * 1.2f));
		}
	}

	protected override void OnBehaviorActivatedAux()
	{
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderScatter);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderWider);
	}

	protected override float GetAiWeight()
	{
		if (_mainFormation == null || !_mainFormation.AI.IsMainFormation)
		{
			_mainFormation = base.Formation.Team.FormationsIncludingEmpty.FirstOrDefaultQ((Formation f) => f.CountOfUnits > 0 && f.AI.IsMainFormation);
		}
		if (_behaviorSide != base.Formation.AI.Side)
		{
			_behaviorSide = base.Formation.AI.Side;
		}
		FormationQuerySystem cachedClosestEnemyFormation = base.Formation.CachedClosestEnemyFormation;
		if (_mainFormation == null || base.Formation.AI.IsMainFormation || cachedClosestEnemyFormation == null)
		{
			return 0f;
		}
		FormationQuerySystem querySystem = base.Formation.QuerySystem;
		float num = MBMath.Lerp(0.1f, 1f, MBMath.ClampFloat(querySystem.RangedUnitRatio + querySystem.RangedCavalryUnitRatio, 0f, 0.5f) * 2f);
		float value = base.Formation.CachedAveragePosition.Distance((querySystem.ClosestSignificantlyLargeEnemyFormation ?? cachedClosestEnemyFormation).Formation.CachedMedianPosition.AsVec2) / (querySystem.ClosestSignificantlyLargeEnemyFormation ?? cachedClosestEnemyFormation).MovementSpeedMaximum;
		float num2 = MBMath.Lerp(0.5f, 1.2f, (MBMath.ClampFloat(value, 4f, 8f) - 4f) / 4f);
		return num * querySystem.MainFormationReliabilityFactor * num2;
	}
}
