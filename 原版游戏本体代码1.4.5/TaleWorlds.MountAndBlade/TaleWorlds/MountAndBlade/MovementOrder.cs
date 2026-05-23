using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public struct MovementOrder
{
	public enum MovementOrderEnum
	{
		Invalid = 0,
		AttackEntity = 1,
		Charge = 2,
		ChargeToTarget = 3,
		Follow = 4,
		FollowEntity = 5,
		Move = 7,
		Retreat = 8,
		Stop = 9,
		Advance = 10,
		FallBack = 11
	}

	public enum MovementStateEnum
	{
		Charge,
		Hold,
		Retreat,
		StandGround
	}

	public enum Side
	{
		Front,
		Rear,
		Left,
		Right
	}

	private enum FollowState
	{
		Stop,
		Depart,
		Move,
		Arrive
	}

	public static readonly MovementOrder MovementOrderNull = new MovementOrder(MovementOrderEnum.Invalid);

	public static readonly MovementOrder MovementOrderCharge = new MovementOrder(MovementOrderEnum.Charge);

	public static readonly MovementOrder MovementOrderRetreat = new MovementOrder(MovementOrderEnum.Retreat);

	public static readonly MovementOrder MovementOrderStop = new MovementOrder(MovementOrderEnum.Stop);

	public static readonly MovementOrder MovementOrderAdvance = new MovementOrder(MovementOrderEnum.Advance);

	public static readonly MovementOrder MovementOrderFallBack = new MovementOrder(MovementOrderEnum.FallBack);

	private FollowState _followState;

	private float _departStartTime;

	public readonly MovementOrderEnum OrderEnum;

	private Func<Formation, WorldPosition> _positionLambda;

	private WorldPosition _position;

	private WorldPosition _getPositionResultCache;

	private WorldPosition _engageTargetPositionCache;

	private float _engageTargetPositionOffset;

	private bool _getPositionIsNavmeshlessCache;

	private WorldPosition _getPositionFirstSectionCache;

	public GameEntity TargetEntity;

	private readonly Timer _tickTimer;

	private WorldPosition _lastPosition;

	public readonly bool _isFacingDirection;

	public Formation TargetFormation { get; private set; }

	public Agent _targetAgent { get; }

	public OrderType OrderType
	{
		get
		{
			switch (OrderEnum)
			{
			case MovementOrderEnum.AttackEntity:
				return OrderType.AttackEntity;
			case MovementOrderEnum.Charge:
				return OrderType.Charge;
			case MovementOrderEnum.ChargeToTarget:
				return OrderType.ChargeWithTarget;
			case MovementOrderEnum.Follow:
				return OrderType.FollowMe;
			case MovementOrderEnum.FollowEntity:
				return OrderType.FollowEntity;
			case MovementOrderEnum.Move:
				return OrderType.Move;
			case MovementOrderEnum.Retreat:
				return OrderType.Retreat;
			case MovementOrderEnum.Stop:
				return OrderType.StandYourGround;
			case MovementOrderEnum.Advance:
				return OrderType.Advance;
			case MovementOrderEnum.FallBack:
				return OrderType.FallBack;
			default:
				Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\Orders\\MovementOrder.cs", "OrderType", 114);
				return OrderType.Move;
			}
		}
	}

	public MovementStateEnum MovementState
	{
		get
		{
			switch (OrderEnum)
			{
			case MovementOrderEnum.Charge:
			case MovementOrderEnum.ChargeToTarget:
				if (_position.IsValid)
				{
					return MovementStateEnum.Hold;
				}
				return MovementStateEnum.Charge;
			case MovementOrderEnum.Retreat:
				return MovementStateEnum.Retreat;
			case MovementOrderEnum.Stop:
				return MovementStateEnum.StandGround;
			default:
				return MovementStateEnum.Hold;
			}
		}
	}

	private MovementOrder(MovementOrderEnum orderEnum)
	{
		OrderEnum = orderEnum;
		switch (orderEnum)
		{
		case MovementOrderEnum.Charge:
			_positionLambda = null;
			break;
		case MovementOrderEnum.Retreat:
			_positionLambda = null;
			break;
		case MovementOrderEnum.Advance:
			_positionLambda = null;
			break;
		case MovementOrderEnum.FallBack:
			_positionLambda = null;
			break;
		default:
			_positionLambda = null;
			break;
		}
		TargetFormation = null;
		TargetEntity = null;
		_targetAgent = null;
		_tickTimer = new Timer(Mission.Current.CurrentTime, 0.5f);
		_lastPosition = WorldPosition.Invalid;
		_isFacingDirection = false;
		_position = WorldPosition.Invalid;
		_getPositionResultCache = WorldPosition.Invalid;
		_getPositionFirstSectionCache = WorldPosition.Invalid;
		_getPositionIsNavmeshlessCache = false;
		_engageTargetPositionCache = WorldPosition.Invalid;
		_engageTargetPositionOffset = 0f;
		_followState = FollowState.Stop;
		_departStartTime = -1f;
	}

	private MovementOrder(MovementOrderEnum orderEnum, Formation targetFormation)
	{
		OrderEnum = orderEnum;
		_positionLambda = null;
		TargetFormation = targetFormation;
		TargetEntity = null;
		_targetAgent = null;
		_tickTimer = new Timer(Mission.Current.CurrentTime, 0.5f);
		_lastPosition = WorldPosition.Invalid;
		_isFacingDirection = false;
		_position = WorldPosition.Invalid;
		_getPositionResultCache = WorldPosition.Invalid;
		_getPositionFirstSectionCache = WorldPosition.Invalid;
		_getPositionIsNavmeshlessCache = false;
		_engageTargetPositionCache = WorldPosition.Invalid;
		_engageTargetPositionOffset = 0f;
		_followState = FollowState.Stop;
		_departStartTime = -1f;
	}

	private WorldPosition ComputeAttackEntityWaitPosition(Formation formation, WeakGameEntity targetEntity)
	{
		Scene scene = formation.Team.Mission.Scene;
		WorldPosition worldPosition = new WorldPosition(scene, UIntPtr.Zero, targetEntity.GlobalPosition, hasValidZ: false);
		Vec2 vec = formation.CachedAveragePosition - worldPosition.AsVec2;
		Vec2 vec2 = targetEntity.GetGlobalFrame().rotation.f.AsVec2.Normalized();
		Vec2 vec3 = ((vec.DotProduct(vec2) >= 0f) ? vec2 : (-vec2));
		WorldPosition worldPosition2 = worldPosition;
		worldPosition2.SetVec2(worldPosition.AsVec2 + vec3 * 3f);
		if (scene.DoesPathExistBetweenPositions(worldPosition2, formation.CachedMedianPosition))
		{
			return worldPosition2;
		}
		WorldPosition worldPosition3 = worldPosition;
		worldPosition3.SetVec2(worldPosition.AsVec2 - vec3 * 3f);
		if (scene.DoesPathExistBetweenPositions(worldPosition3, formation.CachedMedianPosition))
		{
			return worldPosition3;
		}
		worldPosition3 = worldPosition;
		worldPosition3.SetVec2(worldPosition.AsVec2 + targetEntity.GetGlobalFrame().rotation.s.AsVec2.Normalized() * 3f);
		if (scene.DoesPathExistBetweenPositions(worldPosition3, formation.CachedMedianPosition))
		{
			return worldPosition3;
		}
		worldPosition3 = worldPosition;
		worldPosition3.SetVec2(worldPosition.AsVec2 - targetEntity.GetGlobalFrame().rotation.s.AsVec2.Normalized() * 3f);
		if (!scene.DoesPathExistBetweenPositions(worldPosition3, formation.CachedMedianPosition))
		{
			return worldPosition2;
		}
		return worldPosition3;
	}

	private MovementOrder(MovementOrderEnum orderEnum, GameEntity targetEntity, bool surroundEntity)
	{
		targetEntity.GetFirstScriptOfType<UsableMachine>();
		OrderEnum = orderEnum;
		_positionLambda = delegate(Formation f)
		{
			WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, targetEntity.GlobalPosition, hasValidZ: false);
			Vec2 vec = f.CachedAveragePosition - worldPosition.AsVec2;
			Vec2 vec2 = targetEntity.GetGlobalFrame().rotation.f.AsVec2.Normalized();
			Vec2 vec3 = ((vec.DotProduct(vec2) >= 0f) ? vec2 : (-vec2));
			WorldPosition worldPosition2 = worldPosition;
			worldPosition2.SetVec2MT(worldPosition.AsVec2 + vec3 * 3f);
			if (Mission.Current.Scene.DoesPathExistBetweenPositions(worldPosition2, f.CachedMedianPosition))
			{
				return worldPosition2;
			}
			WorldPosition worldPosition3 = worldPosition;
			worldPosition3.SetVec2MT(worldPosition.AsVec2 - vec3 * 3f);
			if (Mission.Current.Scene.DoesPathExistBetweenPositions(worldPosition3, f.CachedMedianPosition))
			{
				return worldPosition3;
			}
			worldPosition3 = worldPosition;
			worldPosition3.SetVec2MT(worldPosition.AsVec2 + targetEntity.GetGlobalFrame().rotation.s.AsVec2.Normalized() * 3f);
			if (Mission.Current.Scene.DoesPathExistBetweenPositions(worldPosition3, f.CachedMedianPosition))
			{
				return worldPosition3;
			}
			worldPosition3 = worldPosition;
			worldPosition3.SetVec2MT(worldPosition.AsVec2 - targetEntity.GetGlobalFrame().rotation.s.AsVec2.Normalized() * 3f);
			return Mission.Current.Scene.DoesPathExistBetweenPositions(worldPosition3, f.CachedMedianPosition) ? worldPosition3 : worldPosition2;
		};
		TargetEntity = targetEntity;
		_tickTimer = new Timer(Mission.Current.CurrentTime, 0.5f);
		TargetFormation = null;
		_targetAgent = null;
		_lastPosition = WorldPosition.Invalid;
		_isFacingDirection = false;
		_position = WorldPosition.Invalid;
		_getPositionResultCache = WorldPosition.Invalid;
		_getPositionFirstSectionCache = WorldPosition.Invalid;
		_getPositionIsNavmeshlessCache = false;
		_engageTargetPositionCache = WorldPosition.Invalid;
		_engageTargetPositionOffset = 0f;
		_followState = FollowState.Stop;
		_departStartTime = -1f;
	}

	private MovementOrder(MovementOrderEnum orderEnum, Agent targetAgent)
	{
		OrderEnum = orderEnum;
		WorldPosition targetAgentPos = targetAgent.GetWorldPosition();
		if (orderEnum == MovementOrderEnum.Follow)
		{
			_positionLambda = delegate(Formation f)
			{
				WorldPosition result = targetAgentPos;
				result.SetVec2(result.AsVec2 - f.GetMiddleFrontUnitPositionOffset());
				return result;
			};
		}
		else
		{
			_positionLambda = delegate(Formation f)
			{
				WorldPosition worldPosition = targetAgentPos;
				worldPosition.SetVec2(worldPosition.AsVec2 - 4f * (f.QuerySystem.Team.MedianTargetFormationPosition.AsVec2 - targetAgentPos.AsVec2).Normalized());
				return (worldPosition.AsVec2.DistanceSquared(f.GetReadonlyMovementOrderReference()._lastPosition.AsVec2) > 6.25f) ? worldPosition : f.GetReadonlyMovementOrderReference()._lastPosition;
			};
		}
		_targetAgent = targetAgent;
		TargetFormation = null;
		TargetEntity = null;
		_tickTimer = new Timer(targetAgent.Mission.CurrentTime, 0.5f);
		_lastPosition = targetAgentPos;
		_isFacingDirection = false;
		_position = WorldPosition.Invalid;
		_getPositionResultCache = WorldPosition.Invalid;
		_getPositionFirstSectionCache = WorldPosition.Invalid;
		_getPositionIsNavmeshlessCache = false;
		_engageTargetPositionCache = WorldPosition.Invalid;
		_engageTargetPositionOffset = 0f;
		_followState = FollowState.Stop;
		_departStartTime = -1f;
	}

	private MovementOrder(MovementOrderEnum orderEnum, GameEntity targetEntity)
	{
		OrderEnum = orderEnum;
		_positionLambda = delegate
		{
			WorldPosition result = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, targetEntity.GlobalPosition, hasValidZ: false);
			result.SetVec2(result.AsVec2);
			return result;
		};
		TargetEntity = targetEntity;
		TargetFormation = null;
		_targetAgent = null;
		_tickTimer = new Timer(Mission.Current.CurrentTime, 0.5f);
		_lastPosition = WorldPosition.Invalid;
		_isFacingDirection = false;
		_position = WorldPosition.Invalid;
		_getPositionResultCache = WorldPosition.Invalid;
		_getPositionFirstSectionCache = WorldPosition.Invalid;
		_getPositionIsNavmeshlessCache = false;
		_engageTargetPositionCache = WorldPosition.Invalid;
		_engageTargetPositionOffset = 0f;
		_followState = FollowState.Stop;
		_departStartTime = -1f;
	}

	private MovementOrder(MovementOrderEnum orderEnum, WorldPosition position)
	{
		OrderEnum = orderEnum;
		_positionLambda = null;
		_isFacingDirection = false;
		TargetFormation = null;
		TargetEntity = null;
		_targetAgent = null;
		_tickTimer = new Timer(Mission.Current.CurrentTime, 0.5f);
		_lastPosition = WorldPosition.Invalid;
		_position = position;
		_getPositionResultCache = WorldPosition.Invalid;
		_getPositionFirstSectionCache = WorldPosition.Invalid;
		_getPositionIsNavmeshlessCache = false;
		_engageTargetPositionCache = WorldPosition.Invalid;
		_engageTargetPositionOffset = 0f;
		_followState = FollowState.Stop;
		_departStartTime = -1f;
	}

	public override bool Equals(object obj)
	{
		if (obj is MovementOrder m)
		{
			return m == this;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (int)OrderEnum;
	}

	public static bool operator !=(in MovementOrder m, MovementOrder obj)
	{
		return m.OrderEnum != obj.OrderEnum;
	}

	public static bool operator ==(in MovementOrder m, MovementOrder obj)
	{
		return m.OrderEnum == obj.OrderEnum;
	}

	public static MovementOrder MovementOrderChargeToTarget(Formation targetFormation)
	{
		return new MovementOrder(MovementOrderEnum.ChargeToTarget, targetFormation);
	}

	public static MovementOrder MovementOrderFollow(Agent targetAgent)
	{
		return new MovementOrder(MovementOrderEnum.Follow, targetAgent);
	}

	public static MovementOrder MovementOrderFollowEntity(GameEntity targetEntity)
	{
		return new MovementOrder(MovementOrderEnum.FollowEntity, targetEntity);
	}

	public static MovementOrder MovementOrderMove(WorldPosition position)
	{
		return new MovementOrder(MovementOrderEnum.Move, position);
	}

	public static MovementOrder MovementOrderAttackEntity(GameEntity targetEntity, bool surroundEntity)
	{
		return new MovementOrder(MovementOrderEnum.AttackEntity, targetEntity, surroundEntity);
	}

	public static int GetMovementOrderDefensiveness(MovementOrderEnum orderEnum)
	{
		if (orderEnum == MovementOrderEnum.Charge || orderEnum == MovementOrderEnum.ChargeToTarget)
		{
			return 0;
		}
		return 1;
	}

	public static int GetMovementOrderDefensivenessChange(MovementOrderEnum previousOrderEnum, MovementOrderEnum nextOrderEnum)
	{
		if (previousOrderEnum == MovementOrderEnum.Charge || previousOrderEnum == MovementOrderEnum.ChargeToTarget)
		{
			if (nextOrderEnum != MovementOrderEnum.Charge && nextOrderEnum != MovementOrderEnum.ChargeToTarget)
			{
				return 1;
			}
			return 0;
		}
		if (nextOrderEnum == MovementOrderEnum.Charge || nextOrderEnum == MovementOrderEnum.ChargeToTarget)
		{
			return -1;
		}
		return 0;
	}

	private static void RetreatAux(Formation formation)
	{
		for (int num = formation.Detachments.Count - 1; num >= 0; num--)
		{
			formation.LeaveDetachment(formation.Detachments[num]);
		}
		formation.ApplyActionOnEachUnitViaBackupList(delegate(Agent agent)
		{
			if (agent.IsAIControlled)
			{
				agent.Retreat(useCachingSystem: true);
			}
		});
	}

	private static WorldPosition GetAlternatePositionForNavmeshlessOrOutOfBoundsPosition(Formation f, WorldPosition originalPosition)
	{
		float positionPenalty = 1f;
		WorldPosition alternatePositionForNavmeshlessOrOutOfBoundsPosition = Mission.Current.GetAlternatePositionForNavmeshlessOrOutOfBoundsPosition(originalPosition.AsVec2 - f.CachedAveragePosition, originalPosition, ref positionPenalty);
		if (f.AI?.ActiveBehavior != null)
		{
			f.AI.ActiveBehavior.NavmeshlessTargetPositionPenalty = positionPenalty;
		}
		return alternatePositionForNavmeshlessOrOutOfBoundsPosition;
	}

	private void GetPositionAuxFollow(Formation f)
	{
		Vec2 zero = Vec2.Zero;
		if (_followState != FollowState.Move && _targetAgent.MountAgent != null)
		{
			zero += f.Direction * -2f;
		}
		if (_followState == FollowState.Move && f.PhysicalClass.IsMounted())
		{
			zero += 2f * _targetAgent.Velocity.AsVec2;
		}
		else if (_followState == FollowState.Move)
		{
			f.PhysicalClass.IsMounted();
		}
		WorldPosition worldPosition = _targetAgent.GetWorldPosition();
		worldPosition.SetVec2(worldPosition.AsVec2 - f.GetMiddleFrontUnitPositionOffset() + zero);
		if (_followState == FollowState.Stop || _followState == FollowState.Depart)
		{
			float num = (f.PhysicalClass.IsMounted() ? 4f : 2.5f);
			if (Mission.Current.IsTeleportingAgents || worldPosition.AsVec2.DistanceSquared(_lastPosition.AsVec2) > num * num)
			{
				_lastPosition = worldPosition;
			}
		}
		else
		{
			_lastPosition = worldPosition;
		}
	}

	public Vec2 GetPosition(Formation f)
	{
		return CreateNewOrderWorldPositionMT(f, WorldPosition.WorldPositionEnforcedCache.None).AsVec2;
	}

	public Vec2 GetTargetVelocity()
	{
		switch (OrderEnum)
		{
		case MovementOrderEnum.AttackEntity:
		case MovementOrderEnum.Charge:
		case MovementOrderEnum.ChargeToTarget:
		case MovementOrderEnum.FollowEntity:
		case MovementOrderEnum.Move:
		case MovementOrderEnum.Retreat:
		case MovementOrderEnum.Stop:
		case MovementOrderEnum.Advance:
		case MovementOrderEnum.FallBack:
			return Vec2.Zero;
		case MovementOrderEnum.Follow:
			return _targetAgent.AverageVelocity.AsVec2;
		default:
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\Orders\\MovementOrder.cs", "GetTargetVelocity", 842);
			return Vec2.Zero;
		}
	}

	public WorldPosition CreateNewOrderWorldPositionMT(Formation f, WorldPosition.WorldPositionEnforcedCache worldPositionEnforcedCache)
	{
		lock (f.OrderPositionLock)
		{
			if (!IsApplicable(f))
			{
				return f.CreateNewOrderWorldPosition(worldPositionEnforcedCache);
			}
			WorldPosition orderPosition;
			switch (OrderEnum)
			{
			case MovementOrderEnum.Follow:
				GetPositionAuxFollow(f);
				orderPosition = _lastPosition;
				break;
			case MovementOrderEnum.Advance:
			case MovementOrderEnum.FallBack:
				orderPosition = GetPositionAux(f, worldPositionEnforcedCache);
				break;
			default:
				orderPosition = _positionLambda?.Invoke(f) ?? _position;
				break;
			}
			if (Mission.Current.Mode == MissionMode.Deployment)
			{
				if (!Mission.Current.IsOrderPositionAvailable(in orderPosition, f.Team))
				{
					orderPosition = f.CreateNewOrderWorldPosition(worldPositionEnforcedCache);
				}
				else
				{
					if (Mission.Current.DeploymentPlan.SupportsNavmesh(f.Team))
					{
						Mission.Current.DeploymentPlan.ProjectPositionToDeploymentBoundaries(f.Team, ref orderPosition);
					}
					if (!Mission.Current.IsOrderPositionAvailable(in orderPosition, f.Team))
					{
						orderPosition = f.CreateNewOrderWorldPosition(worldPositionEnforcedCache);
					}
				}
			}
			bool flag = false;
			if (_getPositionFirstSectionCache.AsVec2 != orderPosition.AsVec2)
			{
				_getPositionIsNavmeshlessCache = false;
				if (orderPosition.IsValid)
				{
					switch (worldPositionEnforcedCache)
					{
					case WorldPosition.WorldPositionEnforcedCache.NavMeshVec3:
						orderPosition.GetNavMeshVec3MT();
						break;
					case WorldPosition.WorldPositionEnforcedCache.GroundVec3:
						orderPosition.GetGroundVec3MT();
						break;
					}
					_getPositionFirstSectionCache = orderPosition;
					if (OrderEnum != MovementOrderEnum.Follow && (orderPosition.GetNavMeshMT() == UIntPtr.Zero || !Mission.Current.IsPositionInsideBoundaries(orderPosition.AsVec2)))
					{
						orderPosition = GetAlternatePositionForNavmeshlessOrOutOfBoundsPosition(f, orderPosition);
						switch (worldPositionEnforcedCache)
						{
						case WorldPosition.WorldPositionEnforcedCache.NavMeshVec3:
							orderPosition.GetNavMeshVec3MT();
							break;
						case WorldPosition.WorldPositionEnforcedCache.GroundVec3:
							orderPosition.GetGroundVec3MT();
							break;
						}
					}
					else
					{
						flag = true;
						_getPositionIsNavmeshlessCache = true;
					}
					_getPositionResultCache = orderPosition;
				}
			}
			else
			{
				if (_getPositionResultCache.IsValid)
				{
					switch (worldPositionEnforcedCache)
					{
					case WorldPosition.WorldPositionEnforcedCache.NavMeshVec3:
						_getPositionResultCache.GetNavMeshVec3MT();
						break;
					case WorldPosition.WorldPositionEnforcedCache.GroundVec3:
						_getPositionResultCache.GetGroundVec3MT();
						break;
					}
				}
				orderPosition = _getPositionResultCache;
			}
			if ((_getPositionIsNavmeshlessCache || flag) && f.AI?.ActiveBehavior != null)
			{
				f.AI.ActiveBehavior.NavmeshlessTargetPositionPenalty = 1f;
			}
			return orderPosition;
		}
	}

	public void ResetPositionCache()
	{
		_getPositionFirstSectionCache = WorldPosition.Invalid;
		_getPositionResultCache = WorldPosition.Invalid;
	}

	public bool AreOrdersPracticallySame(MovementOrder m1, MovementOrder m2, bool isAIControlled)
	{
		if (m1.OrderEnum != m2.OrderEnum)
		{
			return false;
		}
		switch (m1.OrderEnum)
		{
		case MovementOrderEnum.Advance:
			return true;
		case MovementOrderEnum.AttackEntity:
			return m1.TargetEntity == m2.TargetEntity;
		case MovementOrderEnum.Charge:
			return true;
		case MovementOrderEnum.ChargeToTarget:
			return m1.TargetFormation == m2.TargetFormation;
		case MovementOrderEnum.FallBack:
			return true;
		case MovementOrderEnum.Follow:
			return m1._targetAgent == m2._targetAgent;
		case MovementOrderEnum.FollowEntity:
			return m1.TargetEntity == m2.TargetEntity;
		case MovementOrderEnum.Move:
			if (!isAIControlled)
			{
				return false;
			}
			return m1._position.AsVec2.DistanceSquared(m2._position.AsVec2) < 1f;
		case MovementOrderEnum.Retreat:
			return true;
		case MovementOrderEnum.Stop:
			return true;
		default:
			return true;
		}
	}

	public void OnApply(Formation formation)
	{
		switch (OrderEnum)
		{
		case MovementOrderEnum.AttackEntity:
			formation.FormAttackEntityDetachment(TargetEntity);
			break;
		case MovementOrderEnum.ChargeToTarget:
			formation.SetTargetFormation(TargetFormation);
			break;
		case MovementOrderEnum.Follow:
			formation.Arrangement.ReserveMiddleFrontUnitPosition(_targetAgent);
			break;
		case MovementOrderEnum.Move:
			formation.SetPositioning(CreateNewOrderWorldPositionMT(formation, WorldPosition.WorldPositionEnforcedCache.None));
			break;
		case MovementOrderEnum.Retreat:
			RetreatAux(formation);
			break;
		}
		MovementOrderEnum orderEnum = OrderEnum;
		if ((orderEnum == MovementOrderEnum.Charge || orderEnum == MovementOrderEnum.ChargeToTarget) && GetPosition(formation).IsValid)
		{
			orderEnum = MovementOrderEnum.Move;
		}
		formation.ApplyActionOnEachUnit(delegate(Agent agent)
		{
			agent.RefreshBehaviorValues(orderEnum, formation.ArrangementOrder.OrderEnum);
		});
	}

	public void OnCancel(Formation formation)
	{
		switch (OrderEnum)
		{
		case MovementOrderEnum.Charge:
			CancelChargeOrder(formation);
			break;
		case MovementOrderEnum.AttackEntity:
			formation.DisbandAttackEntityDetachment();
			break;
		case MovementOrderEnum.ChargeToTarget:
			formation.SetTargetFormation(null);
			CancelChargeOrder(formation);
			break;
		case MovementOrderEnum.Follow:
			formation.Arrangement.ReleaseMiddleFrontUnitPosition();
			break;
		case MovementOrderEnum.Retreat:
			formation.ApplyActionOnEachUnitViaBackupList(delegate(Agent agent)
			{
				if (agent.IsAIControlled)
				{
					agent.StopRetreatingMoraleComponent();
				}
			});
			break;
		case MovementOrderEnum.FallBack:
			if (Mission.Current.IsPositionInsideBoundaries(GetPosition(formation)))
			{
				break;
			}
			formation.ApplyActionOnEachUnitViaBackupList(delegate(Agent agent)
			{
				if (agent.IsAIControlled)
				{
					agent.StopRetreatingMoraleComponent();
				}
			});
			break;
		case MovementOrderEnum.FollowEntity:
		case (MovementOrderEnum)6:
		case MovementOrderEnum.Move:
		case MovementOrderEnum.Stop:
		case MovementOrderEnum.Advance:
			break;
		}
	}

	public void OnUnitJoinOrLeave(Formation formation, Agent unit, bool isJoining)
	{
		if (!IsApplicable(formation))
		{
			return;
		}
		if (isJoining)
		{
			if (OrderEnum == MovementOrderEnum.Retreat)
			{
				if (unit.IsAIControlled)
				{
					unit.Retreat();
				}
			}
			else if ((OrderEnum == MovementOrderEnum.Charge || OrderEnum == MovementOrderEnum.ChargeToTarget) && GetPosition(formation).IsValid)
			{
				unit.RefreshBehaviorValues(MovementOrderEnum.Move, formation.ArrangementOrder.OrderEnum);
			}
			else
			{
				unit.RefreshBehaviorValues(OrderEnum, formation.ArrangementOrder.OrderEnum);
			}
		}
		else if (OrderEnum == MovementOrderEnum.Retreat && unit.IsAIControlled && unit.IsActive())
		{
			unit.StopRetreatingMoraleComponent();
		}
	}

	public bool IsApplicable(Formation formation)
	{
		switch (OrderEnum)
		{
		case MovementOrderEnum.AttackEntity:
		{
			UsableMachine firstScriptOfType2 = TargetEntity.GetFirstScriptOfType<UsableMachine>();
			if (firstScriptOfType2 != null)
			{
				return !firstScriptOfType2.IsDestroyed;
			}
			DestructableComponent firstScriptOfType3 = TargetEntity.GetFirstScriptOfType<DestructableComponent>();
			if (firstScriptOfType3 != null)
			{
				return !firstScriptOfType3.IsDestroyed;
			}
			return false;
		}
		case MovementOrderEnum.Charge:
		{
			for (int i = 0; i < Mission.Current.Teams.Count; i++)
			{
				Team team = Mission.Current.Teams[i];
				if (team.IsEnemyOf(formation.Team) && team.ActiveAgents.Count > 0)
				{
					return true;
				}
			}
			return false;
		}
		case MovementOrderEnum.ChargeToTarget:
			return TargetFormation.CountOfUnits > 0;
		case MovementOrderEnum.Follow:
			return _targetAgent.IsActive();
		case MovementOrderEnum.FollowEntity:
		{
			UsableMachine firstScriptOfType = TargetEntity.GetFirstScriptOfType<UsableMachine>();
			if (firstScriptOfType != null)
			{
				return !firstScriptOfType.IsDestroyed;
			}
			return true;
		}
		default:
			return true;
		}
	}

	private bool IsInstance()
	{
		if (OrderEnum != MovementOrderEnum.Invalid && OrderEnum != MovementOrderEnum.Charge && OrderEnum != MovementOrderEnum.Retreat && OrderEnum != MovementOrderEnum.Stop && OrderEnum != MovementOrderEnum.Advance)
		{
			return OrderEnum != MovementOrderEnum.FallBack;
		}
		return false;
	}

	public bool Tick(Formation formation)
	{
		bool num = !IsInstance() || _tickTimer.Check(Mission.Current.CurrentTime);
		TickAux();
		if (num)
		{
			TickOccasionally(formation, _tickTimer.PreviousDeltaTime);
		}
		return num;
	}

	private void TickOccasionally(Formation formation, float dt)
	{
		switch (OrderEnum)
		{
		case MovementOrderEnum.FallBack:
			if (!Mission.Current.IsPositionInsideBoundaries(GetPosition(formation)))
			{
				RetreatAux(formation);
			}
			break;
		case MovementOrderEnum.AttackEntity:
			formation.AttackEntityOrderSecondaryDetachment.TickOccasionally(formation);
			break;
		case MovementOrderEnum.Charge:
		case MovementOrderEnum.ChargeToTarget:
		{
			TeamAISiegeComponent teamAISiegeComponent = formation.Team?.TeamAI as TeamAISiegeComponent;
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			if (!Mission.Current.IsTeleportingAgents && teamAISiegeComponent != null)
			{
				flag4 = TeamAISiegeComponent.IsFormationInsideCastle(formation, includeOnlyPositionedUnits: false);
				bool flag5 = false;
				foreach (Team team in formation.Team.Mission.Teams)
				{
					if (!team.IsEnemyOf(formation.Team))
					{
						continue;
					}
					foreach (Formation item in team.FormationsIncludingEmpty)
					{
						if (item.CountOfUnits > 0 && flag4 == TeamAISiegeComponent.IsFormationInsideCastle(item, includeOnlyPositionedUnits: false))
						{
							flag5 = true;
							break;
						}
					}
					if (flag5)
					{
						break;
					}
				}
				if (!flag5)
				{
					if (flag4 && !teamAISiegeComponent.CalculateIsAnyLaneOpenToGoOutside())
					{
						CastleGate gateToGetThrough = ((!teamAISiegeComponent.InnerGate.IsGateOpen) ? teamAISiegeComponent.InnerGate : teamAISiegeComponent.OuterGate);
						if (gateToGetThrough != null)
						{
							if (!gateToGetThrough.IsUsedByFormation(formation))
							{
								formation.StartUsingMachine(gateToGetThrough, isPlayerOrder: true);
								SiegeLane siegeLane = TeamAISiegeComponent.SiegeLanes.FirstOrDefault((SiegeLane sl) => sl.LaneSide == gateToGetThrough.DefenseSide) ?? TeamAISiegeComponent.SiegeLanes.FirstOrDefault((SiegeLane sl) => sl.LaneSide == FormationAI.BehaviorSide.Middle);
								TacticalPosition tacticalPosition = siegeLane?.DefensePoints.FirstOrDefault((ICastleKeyPosition dp) => dp.AttackerSiegeWeapon is UsableMachine usableMachine && !usableMachine.IsDisabled)?.WaitPosition;
								if (tacticalPosition != null)
								{
									_position = tacticalPosition.Position;
								}
								else
								{
									WorldFrame worldFrame = siegeLane?.DefensePoints.FirstOrDefault((ICastleKeyPosition dp) => dp.AttackerSiegeWeapon is UsableMachine usableMachine && !usableMachine.IsDisabled)?.DefenseWaitFrame ?? siegeLane?.DefensePoints.FirstOrDefault()?.DefenseWaitFrame ?? WorldFrame.Invalid;
									_position = (worldFrame.Origin.IsValid ? worldFrame.Origin : formation.CachedMedianPosition);
								}
							}
							flag = true;
						}
					}
					else if (!teamAISiegeComponent.CalculateIsAnyLaneOpenToGetInside())
					{
						SiegeLadder siegeLadder = null;
						float num = float.MaxValue;
						foreach (SiegeLadder ladder in teamAISiegeComponent.Ladders)
						{
							if (!ladder.IsDeactivated && !ladder.IsDisabled)
							{
								float num2 = ladder.WaitFrame.origin.DistanceSquared(formation.CachedMedianPosition.GetNavMeshVec3());
								if (num2 < num)
								{
									num = num2;
									siegeLadder = ladder;
								}
							}
						}
						if (siegeLadder != null)
						{
							if (!siegeLadder.IsUsedByFormation(formation))
							{
								formation.StartUsingMachine(siegeLadder, isPlayerOrder: true);
								_position = siegeLadder.WaitFrame.origin.ToWorldPosition();
							}
							else if (!_position.IsValid)
							{
								_position = siegeLadder.WaitFrame.origin.ToWorldPosition();
							}
							flag2 = true;
						}
						else
						{
							CastleGate castleGate = ((!teamAISiegeComponent.OuterGate.IsGateOpen) ? teamAISiegeComponent.OuterGate : teamAISiegeComponent.InnerGate);
							if (castleGate != null)
							{
								flag3 = true;
								if (formation.AttackEntityOrderSecondaryDetachment == null)
								{
									GameEntity targetEntity = GameEntity.CreateFromWeakEntity(castleGate.GameEntity);
									formation.FormAttackEntityDetachment(targetEntity);
									TargetEntity = targetEntity;
									_position = ComputeAttackEntityWaitPosition(formation, castleGate.GameEntity);
								}
								else if (TargetEntity != castleGate.GameEntity)
								{
									GameEntity targetEntity2 = GameEntity.CreateFromWeakEntity(castleGate.GameEntity);
									formation.DisbandAttackEntityDetachment();
									formation.FormAttackEntityDetachment(targetEntity2);
									TargetEntity = targetEntity2;
									_position = ComputeAttackEntityWaitPosition(formation, castleGate.GameEntity);
								}
								formation.AttackEntityOrderSecondaryDetachment.TickOccasionally(formation);
							}
						}
					}
				}
			}
			if (teamAISiegeComponent != null && flag4 && _position.IsValid && !flag)
			{
				_position = WorldPosition.Invalid;
				formation.SetPositioning(_position);
				formation.ApplyActionOnEachUnit(delegate(Agent agent)
				{
					agent.RefreshBehaviorValues(MovementOrderEnum.Charge, formation.ArrangementOrder.OrderEnum);
				});
			}
			if (teamAISiegeComponent != null && !flag4 && _position.IsValid && !flag2 && !flag3)
			{
				_position = WorldPosition.Invalid;
				formation.SetPositioning(_position);
				formation.ApplyActionOnEachUnit(delegate(Agent agent)
				{
					agent.RefreshBehaviorValues(MovementOrderEnum.Charge, formation.ArrangementOrder.OrderEnum);
				});
			}
			if (teamAISiegeComponent != null && formation.AttackEntityOrderSecondaryDetachment != null && !flag3)
			{
				formation.DisbandAttackEntityDetachment();
				TargetEntity = null;
				_position = WorldPosition.Invalid;
				formation.SetPositioning(_position);
				formation.ApplyActionOnEachUnit(delegate(Agent agent)
				{
					agent.RefreshBehaviorValues(MovementOrderEnum.Charge, formation.ArrangementOrder.OrderEnum);
				});
			}
			if (_position.IsValid)
			{
				formation.SetPositioning(_position);
				formation.ApplyActionOnEachUnit(delegate(Agent agent)
				{
					agent.RefreshBehaviorValues(MovementOrderEnum.Move, formation.ArrangementOrder.OrderEnum);
				});
			}
			break;
		}
		}
	}

	private void TickAux()
	{
		MovementOrderEnum orderEnum = OrderEnum;
		if (orderEnum != MovementOrderEnum.Follow)
		{
			return;
		}
		float length = _targetAgent.GetCurrentVelocity().Length;
		if (length < 0.01f)
		{
			_followState = FollowState.Stop;
		}
		else if (length < _targetAgent.Monster.WalkingSpeedLimit * 0.7f)
		{
			if (_followState == FollowState.Stop)
			{
				_followState = FollowState.Depart;
				_departStartTime = Mission.Current.CurrentTime;
			}
			else if (_followState == FollowState.Move)
			{
				_followState = FollowState.Arrive;
			}
		}
		else if (_followState == FollowState.Depart)
		{
			if (Mission.Current.CurrentTime - _departStartTime > 1f)
			{
				_followState = FollowState.Move;
			}
		}
		else
		{
			_followState = FollowState.Move;
		}
	}

	public void OnArrangementChanged(Formation formation)
	{
		if (IsApplicable(formation))
		{
			MovementOrderEnum orderEnum = OrderEnum;
			if (orderEnum == MovementOrderEnum.Follow)
			{
				formation.Arrangement.ReserveMiddleFrontUnitPosition(_targetAgent);
			}
		}
	}

	public void Advance(Formation formation, float distance)
	{
		WorldPosition currentPosition = CreateNewOrderWorldPositionMT(formation, WorldPosition.WorldPositionEnforcedCache.None);
		Vec2 direction = formation.Direction;
		currentPosition.SetVec2(currentPosition.AsVec2 + direction * distance);
		_positionLambda = (Formation f) => currentPosition;
	}

	public void FallBack(Formation formation, float distance)
	{
		Advance(formation, 0f - distance);
	}

	private (Agent, float) GetBestAgent(List<Agent> candidateAgents)
	{
		if (candidateAgents.IsEmpty())
		{
			return (null, float.MaxValue);
		}
		GameEntity targetEntity = TargetEntity;
		Vec3 targetEntityPos = targetEntity.GlobalPosition;
		Agent agent = TaleWorlds.Core.Extensions.MinBy(candidateAgents, (Agent ca) => ca.Position.DistanceSquared(targetEntityPos));
		return (agent, agent.Position.DistanceSquared(targetEntityPos));
	}

	private (Agent, float) GetWorstAgent(List<Agent> currentAgents, int requiredAgentCount)
	{
		if (requiredAgentCount <= 0 || currentAgents.Count < requiredAgentCount)
		{
			return (null, float.MaxValue);
		}
		GameEntity targetEntity = TargetEntity;
		Vec3 targetEntityPos = targetEntity.GlobalPosition;
		Agent agent = TaleWorlds.Core.Extensions.MaxBy(currentAgents, (Agent ca) => ca.Position.DistanceSquared(targetEntityPos));
		return (agent, agent.Position.DistanceSquared(targetEntityPos));
	}

	public MovementOrder GetSubstituteOrder(Formation formation)
	{
		MovementOrderEnum orderEnum = OrderEnum;
		if (orderEnum == MovementOrderEnum.Charge)
		{
			return MovementOrderStop;
		}
		return MovementOrderCharge;
	}

	private Vec2 GetDirectionAux(Formation f)
	{
		MovementOrderEnum orderEnum = OrderEnum;
		if ((uint)(orderEnum - 10) <= 1u)
		{
			FormationQuerySystem formationQuerySystem = f.TargetFormation?.QuerySystem ?? f.CachedClosestEnemyFormation;
			if (formationQuerySystem != null)
			{
				return (formationQuerySystem.Formation.CachedMedianPosition.AsVec2 - f.CachedAveragePosition).Normalized();
			}
			return Vec2.One;
		}
		Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\Orders\\MovementOrder.cs", "GetDirectionAux", 1742);
		return Vec2.One;
	}

	private WorldPosition GetPositionAux(Formation f, WorldPosition.WorldPositionEnforcedCache worldPositionEnforcedCache)
	{
		switch (OrderEnum)
		{
		case MovementOrderEnum.Advance:
		{
			if (Mission.Current.Mode == MissionMode.Deployment)
			{
				return f.CreateNewOrderWorldPosition(worldPositionEnforcedCache);
			}
			Vec2 vec = f.Direction;
			FormationQuerySystem querySystem = f.QuerySystem;
			FormationQuerySystem formationQuerySystem = f.TargetFormation?.QuerySystem ?? f.CachedClosestEnemyFormation;
			WorldPosition engageTargetPositionCache;
			if (formationQuerySystem == null)
			{
				Agent closestEnemyAgent = querySystem.ClosestEnemyAgent;
				if (closestEnemyAgent == null)
				{
					return f.CreateNewOrderWorldPosition(worldPositionEnforcedCache);
				}
				engageTargetPositionCache = closestEnemyAgent.GetWorldPosition();
			}
			else
			{
				engageTargetPositionCache = formationQuerySystem.Formation.CachedMedianPosition;
			}
			if (querySystem.IsRangedFormation || querySystem.IsRangedCavalryFormation)
			{
				vec = GetDirectionAux(f);
				engageTargetPositionCache.SetVec2(engageTargetPositionCache.AsVec2 - vec * querySystem.MissileRangeAdjusted);
			}
			else if (formationQuerySystem != null)
			{
				vec = (formationQuerySystem.Formation.CachedAveragePosition - f.CachedAveragePosition).Normalized();
				float num = 2f;
				if (formationQuerySystem.FormationPower < f.QuerySystem.FormationPower * 0.2f)
				{
					num = 0.1f;
				}
				engageTargetPositionCache.SetVec2(engageTargetPositionCache.AsVec2 - vec * num);
			}
			if (!_engageTargetPositionCache.IsValid)
			{
				_engageTargetPositionCache = engageTargetPositionCache;
			}
			float num2 = f.QuerySystem.MovementSpeedMaximum * f.QuerySystem.MovementSpeedMaximum * 9f * f.Depth;
			if ((_engageTargetPositionCache.AsVec2 + vec * _engageTargetPositionOffset).DistanceSquared(engageTargetPositionCache.AsVec2) > f.CurrentPosition.DistanceSquared(_engageTargetPositionCache.AsVec2) * 0.1f || engageTargetPositionCache.AsVec2.DistanceSquared(f.CurrentPosition) <= num2)
			{
				_engageTargetPositionCache = engageTargetPositionCache;
				_engageTargetPositionOffset = 0f;
			}
			engageTargetPositionCache = _engageTargetPositionCache;
			if (engageTargetPositionCache.AsVec2.DistanceSquared(f.CurrentPosition) > num2 && vec.DotProduct(engageTargetPositionCache.AsVec2 - f.CurrentPosition) > 0f && f.Arrangement is LineFormation lineFormation && (double)lineFormation.GetUnavailableUnitPositions().Count() > (double)lineFormation.UnitCount * 0.03)
			{
				engageTargetPositionCache.SetVec2(engageTargetPositionCache.AsVec2 - vec * 10f);
				_engageTargetPositionOffset += 10f;
			}
			_engageTargetPositionCache = engageTargetPositionCache;
			return engageTargetPositionCache;
		}
		case MovementOrderEnum.FallBack:
		{
			if (Mission.Current.Mode == MissionMode.Deployment)
			{
				return f.CreateNewOrderWorldPosition(worldPositionEnforcedCache);
			}
			Vec2 directionAux = GetDirectionAux(f);
			WorldPosition cachedMedianPosition = f.CachedMedianPosition;
			cachedMedianPosition.SetVec2(f.CachedAveragePosition - directionAux * 7f);
			return cachedMedianPosition;
		}
		default:
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\Orders\\MovementOrder.cs", "GetPositionAux", 1845);
			return WorldPosition.Invalid;
		}
	}

	private void CancelChargeOrder(Formation formation)
	{
		if (!(formation.Team?.TeamAI is TeamAISiegeComponent teamAISiegeComponent))
		{
			return;
		}
		if (teamAISiegeComponent.InnerGate != null && teamAISiegeComponent.InnerGate.IsUsedByFormation(formation))
		{
			formation.StopUsingMachine(teamAISiegeComponent.InnerGate, isPlayerOrder: true);
		}
		if (teamAISiegeComponent.OuterGate != null && teamAISiegeComponent.OuterGate.IsUsedByFormation(formation))
		{
			formation.StopUsingMachine(teamAISiegeComponent.OuterGate, isPlayerOrder: true);
		}
		foreach (SiegeLadder ladder in teamAISiegeComponent.Ladders)
		{
			if (ladder.IsUsedByFormation(formation))
			{
				formation.StopUsingMachine(ladder, isPlayerOrder: true);
			}
		}
		if (formation.AttackEntityOrderSecondaryDetachment != null)
		{
			formation.DisbandAttackEntityDetachment();
			TargetEntity = null;
		}
		_position = WorldPosition.Invalid;
	}
}
