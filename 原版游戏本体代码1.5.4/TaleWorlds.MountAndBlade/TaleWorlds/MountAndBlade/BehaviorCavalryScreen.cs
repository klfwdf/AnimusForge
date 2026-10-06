using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public class BehaviorCavalryScreen : BehaviorComponent
{
	private Formation _mainFormation;

	private Formation _flankingEnemyCavalryFormation;

	private float _threatFormationCacheTime;

	private const float _threatFormationCacheExpireTime = 5f;

	public BehaviorCavalryScreen(Formation formation)
		: base(formation)
	{
		_behaviorSide = formation.AI.Side;
		_mainFormation = formation.Team.FormationsIncludingEmpty.FirstOrDefaultQ((Formation f) => f.CountOfUnits > 0 && f.AI.IsMainFormation);
		CalculateCurrentOrder();
	}

	public override void OnValidBehaviorSideChanged()
	{
		base.OnValidBehaviorSideChanged();
		_mainFormation = base.Formation.Team.FormationsIncludingEmpty.FirstOrDefaultQ((Formation f) => f.CountOfUnits > 0 && f.AI.IsMainFormation);
	}

	protected override void CalculateCurrentOrder()
	{
		if (_mainFormation == null || base.Formation.AI.IsMainFormation || (base.Formation.AI.Side != FormationAI.BehaviorSide.Left && base.Formation.AI.Side != FormationAI.BehaviorSide.Right))
		{
			_flankingEnemyCavalryFormation = null;
			WorldPosition cachedMedianPosition = base.Formation.CachedMedianPosition;
			cachedMedianPosition.SetVec2(base.Formation.CachedAveragePosition);
			base.CurrentOrder = MovementOrder.MovementOrderMove(cachedMedianPosition);
			return;
		}
		float currentTime = Mission.Current.CurrentTime;
		if (_threatFormationCacheTime + 5f < currentTime)
		{
			_threatFormationCacheTime = currentTime;
			Vec2 vec = ((base.Formation.AI.Side == FormationAI.BehaviorSide.Left) ? _mainFormation.Direction.LeftVec() : _mainFormation.Direction.RightVec()).Normalized() - _mainFormation.Direction.Normalized();
			_flankingEnemyCavalryFormation = null;
			float num = float.MinValue;
			foreach (Team team in Mission.Current.Teams)
			{
				if (!team.IsEnemyOf(base.Formation.Team))
				{
					continue;
				}
				foreach (Formation item in team.FormationsIncludingSpecialAndEmpty)
				{
					if (item.CountOfUnits <= 0)
					{
						continue;
					}
					Vec2 vec2 = item.CachedMedianPosition.AsVec2 - _mainFormation.CachedMedianPosition.AsVec2;
					if (vec.Normalized().DotProduct(vec2.Normalized()) > 0.9238795f)
					{
						float formationPower = item.QuerySystem.FormationPower;
						if (formationPower > num)
						{
							num = formationPower;
							_flankingEnemyCavalryFormation = item;
						}
					}
				}
			}
		}
		WorldPosition cachedMedianPosition2;
		if (_flankingEnemyCavalryFormation == null)
		{
			cachedMedianPosition2 = base.Formation.CachedMedianPosition;
			cachedMedianPosition2.SetVec2(base.Formation.CachedAveragePosition);
		}
		else
		{
			Vec2 vec3 = _flankingEnemyCavalryFormation.CachedMedianPosition.AsVec2 - _mainFormation.CachedMedianPosition.AsVec2;
			float num2 = vec3.Normalize() * 0.5f;
			cachedMedianPosition2 = _mainFormation.CachedMedianPosition;
			cachedMedianPosition2.SetVec2(cachedMedianPosition2.AsVec2 + num2 * vec3);
		}
		base.CurrentOrder = MovementOrder.MovementOrderMove(cachedMedianPosition2);
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
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderSkein);
		base.Formation.SetFacingOrder(FacingOrder.FacingOrderLookAtEnemy);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderDeep);
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
		if (_flankingEnemyCavalryFormation == null)
		{
			return 0f;
		}
		return 1.2f;
	}
}
