using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public sealed class BehaviorAdvance : BehaviorComponent
{
	private bool _isInShieldWallDistance;

	private bool _switchedToShieldWallRecently;

	private Timer _switchedToShieldWallTimer;

	private Vec2 _reformPosition = Vec2.Invalid;

	public BehaviorAdvance(Formation formation)
		: base(formation)
	{
		base.BehaviorCoherence = 0.8f;
		_switchedToShieldWallTimer = new Timer(0f, 0f);
		CalculateCurrentOrder();
	}

	protected override void CalculateCurrentOrder()
	{
		Formation.FormationIntegrityDataGroup cachedFormationIntegrityData = base.Formation.CachedFormationIntegrityData;
		if (_switchedToShieldWallRecently && !_switchedToShieldWallTimer.Check(Mission.Current.CurrentTime) && cachedFormationIntegrityData.DeviationOfPositionsExcludeFarAgents > cachedFormationIntegrityData.AverageMaxUnlimitedSpeedExcludeFarAgents * 0.5f)
		{
			WorldPosition cachedMedianPosition = base.Formation.CachedMedianPosition;
			if (_reformPosition.IsValid)
			{
				cachedMedianPosition.SetVec2(_reformPosition);
			}
			else
			{
				Vec2 vec = (base.Formation.QuerySystem.Team.MedianTargetFormationPosition.AsVec2 - base.Formation.CachedAveragePosition).Normalized();
				_reformPosition = base.Formation.CachedAveragePosition + vec * 5f;
				cachedMedianPosition.SetVec2(_reformPosition);
			}
			base.CurrentOrder = MovementOrder.MovementOrderMove(cachedMedianPosition);
			return;
		}
		_switchedToShieldWallRecently = false;
		bool flag = false;
		FormationQuerySystem cachedClosestEnemyFormation = base.Formation.CachedClosestEnemyFormation;
		if (cachedClosestEnemyFormation != null && cachedClosestEnemyFormation.IsCavalryFormation)
		{
			Vec2 vec2 = base.Formation.CachedAveragePosition - cachedClosestEnemyFormation.Formation.CachedAveragePosition;
			float num = vec2.Normalize();
			Vec2 cachedCurrentVelocity = cachedClosestEnemyFormation.Formation.CachedCurrentVelocity;
			float num2 = cachedCurrentVelocity.Normalize();
			if (num < 30f && num2 > 2f && vec2.DotProduct(cachedCurrentVelocity) > 0.5f)
			{
				flag = true;
				WorldPosition cachedMedianPosition2 = base.Formation.CachedMedianPosition;
				if (_reformPosition.IsValid)
				{
					cachedMedianPosition2.SetVec2(_reformPosition);
				}
				else
				{
					Vec2 vec3 = (base.Formation.QuerySystem.Team.MedianTargetFormationPosition.AsVec2 - base.Formation.CachedAveragePosition).Normalized();
					_reformPosition = base.Formation.CachedAveragePosition + vec3 * 5f;
					cachedMedianPosition2.SetVec2(_reformPosition);
				}
				base.CurrentOrder = MovementOrder.MovementOrderMove(cachedMedianPosition2);
			}
		}
		if (flag)
		{
			return;
		}
		_reformPosition = Vec2.Invalid;
		int num3 = 0;
		bool flag2 = false;
		foreach (Team team in Mission.Current.Teams)
		{
			if (!team.IsEnemyOf(base.Formation.Team))
			{
				continue;
			}
			foreach (Formation item in team.FormationsIncludingSpecialAndEmpty)
			{
				if (item.CountOfUnits > 0)
				{
					num3++;
					flag2 = num3 == 1;
					if (num3 > 1)
					{
						break;
					}
				}
			}
		}
		FormationQuerySystem formationQuerySystem = (flag2 ? base.Formation.CachedClosestEnemyFormation : base.Formation.QuerySystem.Team.MedianTargetFormation);
		if (formationQuerySystem != null)
		{
			WorldPosition cachedMedianPosition3 = formationQuerySystem.Formation.CachedMedianPosition;
			cachedMedianPosition3.SetVec2(cachedMedianPosition3.AsVec2 + formationQuerySystem.Formation.Direction * formationQuerySystem.Formation.Depth * 0.5f);
			Vec2 direction = -formationQuerySystem.Formation.Direction;
			base.CurrentOrder = MovementOrder.MovementOrderMove(cachedMedianPosition3);
			CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(direction);
			return;
		}
		FormationQuerySystem medianTargetFormation = base.Formation.QuerySystem.Team.MedianTargetFormation;
		WorldPosition position = (flag2 ? base.Formation.CachedClosestEnemyFormation.Formation.CachedMedianPosition : ((medianTargetFormation != null) ? base.Formation.QuerySystem.Team.MedianTargetFormationPosition : WorldPosition.Invalid));
		Vec2 direction2 = ((medianTargetFormation != null) ? (base.Formation.QuerySystem.Team.MedianTargetFormationPosition.AsVec2 - base.Formation.CachedAveragePosition).Normalized() : Vec2.Invalid);
		if (position.IsValid)
		{
			base.CurrentOrder = MovementOrder.MovementOrderMove(position);
		}
		if (direction2.IsValid)
		{
			CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(direction2);
		}
	}

	protected override void OnBehaviorActivatedAux()
	{
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		_isInShieldWallDistance = false;
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderWide);
	}

	public override void TickOccasionally()
	{
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		if (base.Formation.PhysicalClass.IsMeleeInfantry())
		{
			bool flag = false;
			if (base.Formation.CachedClosestEnemyFormation != null && base.Formation.QuerySystem.IsUnderRangedAttack)
			{
				float num = base.Formation.CachedAveragePosition.DistanceSquared(base.Formation.CachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2);
				if (num < 6400f + (_isInShieldWallDistance ? 3600f : 0f) && num > 100f - (_isInShieldWallDistance ? 75f : 0f))
				{
					flag = true;
				}
			}
			if (flag != _isInShieldWallDistance)
			{
				_isInShieldWallDistance = flag;
				if (_isInShieldWallDistance)
				{
					if (base.Formation.QuerySystem.HasShield)
					{
						base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderShieldWall);
					}
					else
					{
						base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
					}
					_switchedToShieldWallRecently = true;
					_switchedToShieldWallTimer.Reset(Mission.Current.CurrentTime, 5f);
				}
				else
				{
					base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
				}
			}
		}
		base.Formation.SetMovementOrder(base.CurrentOrder);
	}

	protected override float GetAiWeight()
	{
		return 1f;
	}
}
