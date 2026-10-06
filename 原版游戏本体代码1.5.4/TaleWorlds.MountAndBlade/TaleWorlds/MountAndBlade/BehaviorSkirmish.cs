using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class BehaviorSkirmish : BehaviorComponent
{
	private enum BehaviorState
	{
		Approaching,
		Shooting,
		PullingBack
	}

	private bool _cantShoot;

	private float _cantShootDistance = float.MaxValue;

	private bool _alternatePositionUsed;

	private WorldPosition _alternatePosition = WorldPosition.Invalid;

	private BehaviorState _behaviorState = BehaviorState.Shooting;

	private Timer _cantShootTimer;

	private Timer _pullBackTimer;

	public BehaviorSkirmish(Formation formation)
		: base(formation)
	{
		base.BehaviorCoherence = 0.5f;
		_cantShootTimer = new Timer(0f, 0f);
		_pullBackTimer = new Timer(0f, 0f);
		CalculateCurrentOrder();
	}

	protected override void CalculateCurrentOrder()
	{
		WorldPosition position = base.Formation.CachedMedianPosition;
		bool flag = false;
		Vec2 vec;
		if (base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation == null)
		{
			vec = base.Formation.Direction;
			position.SetVec2(base.Formation.CachedAveragePosition);
		}
		else
		{
			vec = base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition.AsVec2 - base.Formation.CachedAveragePosition;
			float num = vec.Normalize();
			float num2 = base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedCurrentVelocity.DotProduct(vec);
			float num3 = MBMath.Lerp(5f, 10f, (MBMath.ClampFloat(base.Formation.CountOfUnits, 10f, 60f) - 10f) * 0.02f) * num2;
			num += num3;
			float num4 = MBMath.Lerp(0.1f, 0.33f, 1f - MBMath.ClampFloat(base.Formation.CountOfUnits, 1f, 50f) * 0.02f) * base.Formation.QuerySystem.RangedUnitRatio;
			switch (_behaviorState)
			{
			case BehaviorState.Shooting:
				if (base.Formation.QuerySystem.MakingRangedAttackRatio <= num4)
				{
					if (num > base.Formation.QuerySystem.MaximumMissileRange)
					{
						_behaviorState = BehaviorState.Approaching;
						_cantShootDistance = MathF.Min(_cantShootDistance, base.Formation.QuerySystem.MaximumMissileRange * 0.9f);
					}
					else if (!_cantShoot)
					{
						_cantShoot = true;
						_cantShootTimer.Reset(Mission.Current.CurrentTime, MBMath.Lerp(5f, 10f, (MBMath.ClampFloat(base.Formation.CountOfUnits, 10f, 60f) - 10f) * 0.02f));
					}
					else if (_cantShootTimer.Check(Mission.Current.CurrentTime))
					{
						_behaviorState = BehaviorState.Approaching;
						_cantShootDistance = MathF.Min(_cantShootDistance, num);
					}
				}
				else
				{
					_cantShootDistance = MathF.Max(_cantShootDistance, num);
					_cantShoot = false;
					if (_pullBackTimer.Check(Mission.Current.CurrentTime) && base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.IsInfantryFormation && num < MathF.Min(base.Formation.QuerySystem.MissileRangeAdjusted * 0.4f, _cantShootDistance * 0.666f))
					{
						_behaviorState = BehaviorState.PullingBack;
						_pullBackTimer.Reset(Mission.Current.CurrentTime, 10f);
					}
				}
				break;
			case BehaviorState.Approaching:
				if (num < _cantShootDistance * 0.8f)
				{
					_behaviorState = BehaviorState.Shooting;
					_cantShoot = false;
					flag = true;
				}
				else if (base.Formation.QuerySystem.MakingRangedAttackRatio >= num4 * 1.2f)
				{
					_behaviorState = BehaviorState.Shooting;
					_cantShoot = false;
					flag = true;
				}
				break;
			case BehaviorState.PullingBack:
				if (num > MathF.Min(_cantShootDistance, base.Formation.QuerySystem.MissileRangeAdjusted) * 0.8f)
				{
					_behaviorState = BehaviorState.Shooting;
					_cantShoot = false;
					flag = true;
				}
				else if (_pullBackTimer.Check(Mission.Current.CurrentTime) && base.Formation.QuerySystem.MakingRangedAttackRatio <= num4 * 0.5f)
				{
					_behaviorState = BehaviorState.Shooting;
					_cantShoot = false;
					flag = true;
					_pullBackTimer.Reset(Mission.Current.CurrentTime, 5f);
				}
				break;
			}
			switch (_behaviorState)
			{
			case BehaviorState.Shooting:
				position.SetVec2(base.Formation.CachedAveragePosition + base.Formation.CachedCurrentVelocity.Normalized() * (base.Formation.Depth * 0.5f));
				break;
			case BehaviorState.Approaching:
			{
				bool flag2 = false;
				if (_alternatePositionUsed)
				{
					float num5 = base.Formation.CachedAveragePosition.DistanceSquared(base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedAveragePosition);
					Vec2 v = (base.Formation.CachedAveragePosition + base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedAveragePosition) * 0.5f;
					bool flag3 = (double)_alternatePosition.AsVec2.DistanceSquared(v) > (double)num5 * 0.0625;
					if (!flag3)
					{
						Mission.Current.Scene.GetNavigationMeshForPosition(_alternatePosition.GetNavMeshVec3(), out var faceGroupId, 1.5f, excludeDynamicNavigationMeshes: false);
						Agent medianAgent = base.Formation.GetMedianAgent(excludeDetachedUnits: true, excludePlayer: true, base.Formation.CachedAveragePosition);
						flag3 = (medianAgent != null && medianAgent.GetCurrentNavigationFaceId() % 10 == 1) != (faceGroupId % 10 == 1);
					}
					if (flag3)
					{
						Agent medianAgent2 = base.Formation.GetMedianAgent(excludeDetachedUnits: true, excludePlayer: true, base.Formation.CachedAveragePosition);
						bool num6 = medianAgent2 != null && medianAgent2.GetCurrentNavigationFaceId() % 10 == 1;
						Agent medianAgent3 = base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.GetMedianAgent(excludeDetachedUnits: true, excludePlayer: true, base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedAveragePosition);
						if (num6 == (medianAgent3 != null && medianAgent3.GetCurrentNavigationFaceId() % 10 == 1))
						{
							_alternatePositionUsed = false;
							_alternatePosition = WorldPosition.Invalid;
						}
						else
						{
							flag2 = true;
						}
					}
				}
				else if (Mission.Current.MissionTeamAIType == Mission.MissionTeamAITypeEnum.Siege || Mission.Current.MissionTeamAIType == Mission.MissionTeamAITypeEnum.SallyOut)
				{
					Agent medianAgent4 = base.Formation.GetMedianAgent(excludeDetachedUnits: true, excludePlayer: true, base.Formation.CachedAveragePosition);
					bool num7 = medianAgent4 != null && medianAgent4.GetCurrentNavigationFaceId() % 10 == 1;
					Agent medianAgent5 = base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.GetMedianAgent(excludeDetachedUnits: true, excludePlayer: true, base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedAveragePosition);
					if (num7 != (medianAgent5 != null && medianAgent5.GetCurrentNavigationFaceId() % 10 == 1))
					{
						_alternatePositionUsed = true;
						flag2 = true;
					}
				}
				if (_alternatePositionUsed)
				{
					if (flag2)
					{
						_alternatePosition = new WorldPosition(Mission.Current.Scene, new Vec3((base.Formation.CachedAveragePosition + base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedAveragePosition) * 0.5f, base.Formation.CachedMedianPosition.GetNavMeshZ()));
					}
					position = _alternatePosition;
				}
				else
				{
					position = base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition;
					position.SetVec2(base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedAveragePosition);
				}
				break;
			}
			case BehaviorState.PullingBack:
				position = base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition;
				position.SetVec2(position.AsVec2 - vec * (base.Formation.QuerySystem.MissileRangeAdjusted - base.Formation.Depth * 0.5f - 10f));
				break;
			}
		}
		if (!base.CurrentOrder.GetPosition(base.Formation).IsValid || _behaviorState != BehaviorState.Shooting || flag)
		{
			base.CurrentOrder = MovementOrder.MovementOrderMove(position);
		}
		if (!CurrentFacingOrder.GetDirection(base.Formation).IsValid || _behaviorState != BehaviorState.Shooting || flag)
		{
			CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec);
		}
	}

	public override void TickOccasionally()
	{
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
	}

	protected override void OnBehaviorActivatedAux()
	{
		_cantShoot = false;
		_cantShootDistance = float.MaxValue;
		_behaviorState = BehaviorState.Shooting;
		_cantShootTimer.Reset(Mission.Current.CurrentTime, MBMath.Lerp(5f, 10f, (MBMath.ClampFloat(base.Formation.CountOfUnits, 10f, 60f) - 10f) * 0.02f));
		_pullBackTimer.Reset(0f, 0f);
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderScatter);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderWide);
	}

	protected override float GetAiWeight()
	{
		FormationQuerySystem querySystem = base.Formation.QuerySystem;
		return MBMath.Lerp(0.1f, 1f, MBMath.ClampFloat(querySystem.RangedUnitRatio + querySystem.RangedCavalryUnitRatio, 0f, 0.5f) * 2f);
	}
}
