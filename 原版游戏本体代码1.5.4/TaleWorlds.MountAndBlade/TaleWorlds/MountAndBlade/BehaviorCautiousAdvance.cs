using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace TaleWorlds.MountAndBlade;

public sealed class BehaviorCautiousAdvance : BehaviorComponent
{
	private enum BehaviorState
	{
		Approaching,
		Shooting,
		PullingBack
	}

	private bool _isInShieldWallDistance;

	private bool _switchedToShieldWallRecently;

	private Timer _switchedToShieldWallTimer;

	private Vec2 _reformPosition = Vec2.Invalid;

	private Formation _archerFormation;

	private bool _cantShoot;

	private float _cantShootDistance = float.MaxValue;

	private BehaviorState _behaviorState = BehaviorState.Shooting;

	private Timer _cantShootTimer;

	private Vec2 _shootPosition = Vec2.Invalid;

	private FormationQuerySystem _targetFormation;

	public BehaviorCautiousAdvance()
	{
		base.BehaviorCoherence = 1f;
		_cantShootTimer = new Timer(0f, 0f);
		_switchedToShieldWallTimer = new Timer(0f, 0f);
	}

	public BehaviorCautiousAdvance(Formation formation)
		: base(formation)
	{
		base.BehaviorCoherence = 1f;
		_cantShootTimer = new Timer(0f, 0f);
		_switchedToShieldWallTimer = new Timer(0f, 0f);
		CalculateCurrentOrder();
	}

	protected override void CalculateCurrentOrder()
	{
		WorldPosition cachedMedianPosition = base.Formation.CachedMedianPosition;
		bool flag = false;
		Vec2 vec;
		if (_targetFormation == null || _archerFormation == null)
		{
			vec = base.Formation.Direction;
			cachedMedianPosition.SetVec2(base.Formation.CachedAveragePosition);
		}
		else
		{
			vec = _targetFormation.Formation.CachedMedianPosition.AsVec2 - base.Formation.CachedAveragePosition;
			float num = vec.Normalize();
			float num2 = _archerFormation.QuerySystem.RangedUnitRatio * 0.5f / (float)_archerFormation.Arrangement.RankCount;
			switch (_behaviorState)
			{
			case BehaviorState.Shooting:
				if (_archerFormation.QuerySystem.MakingRangedAttackRatio <= num2)
				{
					if (num > _archerFormation.QuerySystem.MaximumMissileRange)
					{
						_behaviorState = BehaviorState.Approaching;
						_cantShootDistance = MathF.Min(_cantShootDistance, _archerFormation.QuerySystem.MaximumMissileRange * 0.9f);
						_shootPosition = Vec2.Invalid;
					}
					else if (!_cantShoot)
					{
						_cantShoot = true;
						_cantShootTimer.Reset(Mission.Current.CurrentTime, (_archerFormation == null) ? 10f : MBMath.Lerp(10f, 15f, (MBMath.ClampFloat(_archerFormation.CountOfUnits, 10f, 60f) - 10f) * 0.02f));
					}
					else if (_cantShootTimer.Check(Mission.Current.CurrentTime))
					{
						_behaviorState = BehaviorState.Approaching;
						_cantShootDistance = MathF.Min(_cantShootDistance, num);
						_shootPosition = Vec2.Invalid;
					}
				}
				else
				{
					_cantShootDistance = MathF.Max(_cantShootDistance, num);
					_cantShoot = false;
					if (((!_targetFormation.IsRangedFormation && !_targetFormation.IsRangedCavalryFormation) || (num < _targetFormation.MissileRangeAdjusted && _targetFormation.MakingRangedAttackRatio < 0.1f)) && num < MathF.Min(_archerFormation.QuerySystem.MissileRangeAdjusted * 0.4f, _cantShootDistance * 0.667f))
					{
						_behaviorState = BehaviorState.PullingBack;
						_shootPosition = Vec2.Invalid;
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
				else if (_archerFormation.QuerySystem.MakingRangedAttackRatio >= num2 * 1.2f)
				{
					_behaviorState = BehaviorState.Shooting;
					_cantShoot = false;
					flag = true;
				}
				if (_behaviorState == BehaviorState.Shooting)
				{
					_shootPosition = base.Formation.CachedAveragePosition + vec * 5f;
				}
				break;
			case BehaviorState.PullingBack:
				if (num > MathF.Min(_cantShootDistance, _archerFormation.QuerySystem.MissileRangeAdjusted) * 0.8f)
				{
					_behaviorState = BehaviorState.Shooting;
					_cantShoot = false;
					_shootPosition = base.Formation.CachedAveragePosition + vec * 5f;
					flag = true;
				}
				break;
			}
			switch (_behaviorState)
			{
			case BehaviorState.Shooting:
				if (_shootPosition.IsValid)
				{
					cachedMedianPosition.SetVec2(_shootPosition);
				}
				else
				{
					cachedMedianPosition.SetVec2(base.Formation.CachedAveragePosition);
				}
				break;
			case BehaviorState.Approaching:
			{
				cachedMedianPosition = _targetFormation.Formation.CachedMedianPosition;
				Formation.FormationIntegrityDataGroup cachedFormationIntegrityData = base.Formation.CachedFormationIntegrityData;
				if (_switchedToShieldWallRecently && !_switchedToShieldWallTimer.Check(Mission.Current.CurrentTime) && cachedFormationIntegrityData.DeviationOfPositionsExcludeFarAgents > cachedFormationIntegrityData.AverageMaxUnlimitedSpeedExcludeFarAgents * 0.5f)
				{
					if (_reformPosition.IsValid)
					{
						cachedMedianPosition.SetVec2(_reformPosition);
						break;
					}
					vec = (base.Formation.QuerySystem.Team.MedianTargetFormationPosition.AsVec2 - base.Formation.CachedAveragePosition).Normalized();
					_reformPosition = base.Formation.CachedAveragePosition + vec * 5f;
					cachedMedianPosition.SetVec2(_reformPosition);
				}
				else
				{
					_switchedToShieldWallRecently = false;
					_reformPosition = Vec2.Invalid;
					cachedMedianPosition.SetVec2(_targetFormation.Formation.CachedAveragePosition);
				}
				break;
			}
			case BehaviorState.PullingBack:
				cachedMedianPosition = base.Formation.CachedMedianPosition;
				cachedMedianPosition.SetVec2(base.Formation.CachedAveragePosition);
				break;
			}
		}
		if (!base.CurrentOrder.CreateNewOrderWorldPositionMT(base.Formation, WorldPosition.WorldPositionEnforcedCache.None).IsValid || _behaviorState != BehaviorState.Shooting || flag || base.CurrentOrder.CreateNewOrderWorldPositionMT(base.Formation, WorldPosition.WorldPositionEnforcedCache.NavMeshVec3).GetNavMeshVec3().DistanceSquared(cachedMedianPosition.GetNavMeshVec3()) >= base.Formation.Depth * base.Formation.Depth)
		{
			base.CurrentOrder = MovementOrder.MovementOrderMove(cachedMedianPosition);
		}
		if (!CurrentFacingOrder.GetDirection(base.Formation).IsValid || _behaviorState != BehaviorState.Shooting || flag || CurrentFacingOrder.GetDirection(base.Formation).DotProduct(vec) <= MBMath.Lerp(0.5f, 1f, 1f - MBMath.ClampFloat(base.Formation.Width, 1f, 20f) * 0.05f))
		{
			CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec);
		}
	}

	protected override void OnBehaviorActivatedAux()
	{
		IEnumerable<Formation> source = base.Formation.Team.FormationsIncludingEmpty.WhereQ((Formation f) => f.CountOfUnits > 0 && f != base.Formation && f.QuerySystem.IsRangedFormation);
		if (source.AnyQ())
		{
			_archerFormation = source.MaxBy((Formation f) => f.QuerySystem.FormationPower);
		}
		_cantShoot = false;
		_cantShootDistance = float.MaxValue;
		_behaviorState = BehaviorState.Shooting;
		_cantShootTimer.Reset(Mission.Current.CurrentTime, (_archerFormation == null) ? 10f : MBMath.Lerp(10f, 15f, (MBMath.ClampFloat(_archerFormation.CountOfUnits, 10f, 60f) - 10f) * 0.02f));
		_targetFormation = base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation ?? base.Formation.CachedClosestEnemyFormation;
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		_isInShieldWallDistance = true;
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderWide);
	}

	public override void OnBehaviorCanceled()
	{
	}

	public override void TickOccasionally()
	{
		_targetFormation = base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation ?? base.Formation.CachedClosestEnemyFormation;
		if (base.Formation.PhysicalClass.IsMeleeInfantry())
		{
			bool flag = _targetFormation != null && (base.Formation.QuerySystem.IsUnderRangedAttack || base.Formation.CachedAveragePosition.DistanceSquared(base.CurrentOrder.GetPosition(base.Formation)) < 25f + (_isInShieldWallDistance ? 75f : 0f)) && base.Formation.CachedAveragePosition.DistanceSquared(_targetFormation.Formation.CachedMedianPosition.AsVec2) > 100f - (_isInShieldWallDistance ? 75f : 0f);
			if (flag != _isInShieldWallDistance)
			{
				_isInShieldWallDistance = flag;
				if (_isInShieldWallDistance)
				{
					ArrangementOrder arrangementOrder = (base.Formation.QuerySystem.HasShield ? ArrangementOrder.ArrangementOrderShieldWall : ArrangementOrder.ArrangementOrderLoose);
					if (base.Formation.ArrangementOrder != arrangementOrder)
					{
						base.Formation.SetArrangementOrder(arrangementOrder);
						_switchedToShieldWallRecently = true;
						_switchedToShieldWallTimer.Reset(Mission.Current.CurrentTime, 5f);
					}
				}
				else if (!(base.Formation.ArrangementOrder == ArrangementOrder.ArrangementOrderLine))
				{
					base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
				}
			}
		}
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
	}

	protected override float GetAiWeight()
	{
		return 1f;
	}
}
