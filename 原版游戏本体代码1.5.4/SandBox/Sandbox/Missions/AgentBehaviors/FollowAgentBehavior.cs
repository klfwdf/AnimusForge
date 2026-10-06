using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Objects;

namespace SandBox.Missions.AgentBehaviors;

public class FollowAgentBehavior : AgentBehavior
{
	private enum State
	{
		Idle,
		OnMove,
		Fight
	}

	private const float _moveReactionProximityThreshold = 4f;

	private const float _longitudinalClearanceOffset = 2f;

	private const float _onFootMoveProximityThreshold = 1.2f;

	private const float _mountedMoveProximityThreshold = 2.2f;

	private const float _onFootAgentLongitudinalOffset = 0.6f;

	private const float _onFootAgentLateralOffset = 1f;

	private const float _mountedAgentLongitudinalOffset = 1.25f;

	private const float _mountedAgentLateralOffset = 1.5f;

	private float _idleDistance;

	private Agent _selectedAgent;

	private State _state;

	private Agent _deactivatedAgent;

	private bool _myLastStateWasRunning;

	private bool _updatePositionThisFrame;

	public FollowAgentBehavior(AgentBehaviorGroup behaviorGroup)
		: base(behaviorGroup)
	{
		_selectedAgent = null;
		_deactivatedAgent = null;
		_myLastStateWasRunning = false;
	}

	public void SetTargetAgent(Agent agent)
	{
		_selectedAgent = agent;
		_state = State.Idle;
		GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("navigation_mesh_deactivator");
		if (gameEntity != null)
		{
			int disableFaceWithId = gameEntity.GetFirstScriptOfType<NavigationMeshDeactivator>().DisableFaceWithId;
			if (disableFaceWithId != -1)
			{
				base.OwnerAgent.SetAgentExcludeStateForFaceGroupId(disableFaceWithId, isExcluded: false);
			}
		}
		TryMoveStateTransition(forceMove: true);
	}

	public override void Tick(float dt, bool isSimulation)
	{
		if (_selectedAgent != null)
		{
			ControlMovement();
		}
	}

	private void ControlMovement()
	{
		if (base.Navigator.TargetPosition.IsValid && base.Navigator.IsTargetReached())
		{
			base.OwnerAgent.DisableScriptedMovement();
			base.OwnerAgent.SetMaximumSpeedLimit(-1f, isMultiplier: false);
			if (_state == State.OnMove)
			{
				_idleDistance = base.OwnerAgent.Position.AsVec2.Distance(_selectedAgent.Position.AsVec2);
			}
			_state = State.Idle;
		}
		int nearbyEnemyAgentCount = base.Mission.GetNearbyEnemyAgentCount(base.OwnerAgent.Team, base.OwnerAgent.Position.AsVec2, 5f);
		if (_state != State.Fight && nearbyEnemyAgentCount > 0)
		{
			base.OwnerAgent.SetWatchState(Agent.WatchState.Alarmed);
			base.OwnerAgent.ResetLookAgent();
			base.Navigator.ClearTarget();
			base.OwnerAgent.DisableScriptedMovement();
			_state = State.Fight;
			Debug.Print("[Follow agent behavior] Fight!");
		}
		switch (_state)
		{
		case State.Fight:
			if (nearbyEnemyAgentCount == 0)
			{
				base.OwnerAgent.SetWatchState(Agent.WatchState.Patrolling);
				base.OwnerAgent.SetLookAgent(_selectedAgent);
				_state = State.Idle;
				Debug.Print("[Follow agent behavior] Stop fighting!");
			}
			break;
		case State.Idle:
			TryMoveStateTransition(forceMove: false);
			break;
		case State.OnMove:
			MoveToFollowingAgent(forcedMove: false);
			break;
		}
	}

	private void TryMoveStateTransition(bool forceMove)
	{
		if (_selectedAgent != null)
		{
			if ((base.OwnerAgent.GetScriptedFlags() & Agent.AIScriptedFrameFlags.Crouch) != (_selectedAgent.GetScriptedFlags() & Agent.AIScriptedFrameFlags.Crouch))
			{
				base.OwnerAgent.SetCrouchMode(_selectedAgent.CrouchMode);
			}
			if (base.OwnerAgent.Position.AsVec2.Distance(_selectedAgent.Position.AsVec2) > 4f + _idleDistance)
			{
				_state = State.OnMove;
				MoveToFollowingAgent(forceMove);
			}
		}
	}

	private void MoveToFollowingAgent(bool forcedMove)
	{
		Vec2 asVec = _selectedAgent.Velocity.AsVec2;
		if (!(_updatePositionThisFrame || forcedMove) && !asVec.IsNonZero())
		{
			return;
		}
		_updatePositionThisFrame = false;
		WorldPosition destination = _selectedAgent.GetWorldPosition();
		Vec2 vec = (asVec.IsNonZero() ? asVec.Normalized() : _selectedAgent.GetMovementDirection());
		Vec2 vec2 = vec.LeftVec();
		Vec2 va = _selectedAgent.Position.AsVec2 - base.OwnerAgent.Position.AsVec2;
		float lengthSquared = va.LengthSquared;
		int num = ((Vec2.DotProduct(va, vec2) > 0f) ? 1 : (-1));
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		foreach (Agent agent in base.Mission.Agents)
		{
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			if (component?.AgentNavigator == null)
			{
				continue;
			}
			FollowAgentBehavior followAgentBehavior = component.AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>()?.GetBehavior<FollowAgentBehavior>();
			if (followAgentBehavior == null || followAgentBehavior._selectedAgent == null || followAgentBehavior._selectedAgent != _selectedAgent)
			{
				continue;
			}
			Vec2 va2 = _selectedAgent.Position.AsVec2 - agent.Position.AsVec2;
			int num6 = ((Vec2.DotProduct(va2, vec2) > 0f) ? 1 : (-1));
			if (!(va2.LengthSquared < lengthSquared))
			{
				continue;
			}
			if (num6 == num)
			{
				if (agent.HasMount)
				{
					num3++;
				}
				else
				{
					num2++;
				}
			}
			if (Vec2.DotProduct(va2, vec) > 0.3f)
			{
				if (agent.HasMount)
				{
					num5++;
				}
				else
				{
					num4++;
				}
			}
		}
		float num7 = (_selectedAgent.HasMount ? 1.25f : 0.6f);
		float num8 = (base.OwnerAgent.HasMount ? 1.25f : 0.6f);
		float num9 = (_selectedAgent.HasMount ? 1.5f : 1f);
		float num10 = (base.OwnerAgent.HasMount ? 1.5f : 1f);
		Vec2 vec3 = vec * (2f + 0.5f * (num8 + num7) + (float)num2 * 0.6f + (float)num3 * 1.25f);
		Vec2 vec4 = num * vec2 * (0.5f * (num10 + num9) + (float)num2 * 1f + (float)num3 * 1.5f);
		Vec2 vec5 = _selectedAgent.Position.AsVec2 - vec3 - vec4;
		bool flag = false;
		AgentProximityMap.ProximityMapSearchStruct searchStruct = AgentProximityMap.BeginSearch(Mission.Current, vec5, 0.5f);
		while (searchStruct.LastFoundAgent != null)
		{
			Agent lastFoundAgent = searchStruct.LastFoundAgent;
			if (lastFoundAgent.Index != base.OwnerAgent.Index && lastFoundAgent.Index != _selectedAgent.Index)
			{
				flag = true;
				break;
			}
			AgentProximityMap.FindNext(Mission.Current, ref searchStruct);
		}
		float num11 = (base.OwnerAgent.HasMount ? 2.2f : 1.2f);
		if (!flag)
		{
			WorldPosition worldPosition = destination;
			worldPosition = new WorldPosition(base.Mission.Scene, UIntPtr.Zero, worldPosition.GetGroundVec3(), hasValidZ: false);
			worldPosition.SetVec2(vec5);
			if (worldPosition.GetNavMesh() != UIntPtr.Zero && base.Mission.Scene.IsLineToPointClear(ref worldPosition, ref destination, base.OwnerAgent.Monster.BodyCapsuleRadius))
			{
				WorldPosition position = worldPosition;
				position.SetVec2(position.AsVec2 + vec * 1.5f);
				if (position.GetNavMesh() != UIntPtr.Zero && base.Mission.Scene.IsLineToPointClear(ref position, ref worldPosition, base.OwnerAgent.Monster.BodyCapsuleRadius))
				{
					SetMovePos(position, _selectedAgent.MovementDirectionAsAngle, num11, Agent.AIScriptedFrameFlags.NoAttack);
				}
				else
				{
					SetMovePos(worldPosition, _selectedAgent.MovementDirectionAsAngle, num11, Agent.AIScriptedFrameFlags.NoAttack);
				}
			}
			else
			{
				flag = true;
			}
		}
		if (flag)
		{
			float rangeThreshold = num11 + (float)num4 * 0.6f + (float)num5 * 1.25f;
			SetMovePos(destination, _selectedAgent.MovementDirectionAsAngle, rangeThreshold, Agent.AIScriptedFrameFlags.NoAttack);
		}
	}

	private void SetMovePos(WorldPosition pos, float rotationInRadians, float rangeThreshold, Agent.AIScriptedFrameFlags flags)
	{
		bool flag = base.Mission.Mode == MissionMode.Stealth;
		if (base.Navigator.CharacterHasVisiblePrefabs)
		{
			_myLastStateWasRunning = false;
		}
		else
		{
			if (flag && _selectedAgent.CrouchMode)
			{
				flags |= Agent.AIScriptedFrameFlags.Crouch;
			}
			if (flag && _selectedAgent.WalkMode)
			{
				base.OwnerAgent.SetMaximumSpeedLimit(_selectedAgent.CrouchMode ? _selectedAgent.Monster.CrouchWalkingSpeedLimit : _selectedAgent.Monster.WalkingSpeedLimit, isMultiplier: false);
				_myLastStateWasRunning = false;
			}
			else
			{
				float num = base.OwnerAgent.Position.AsVec2.Distance(pos.AsVec2);
				if (num - rangeThreshold <= 0.5f * (_myLastStateWasRunning ? 1f : 1.2f) && _selectedAgent.Velocity.AsVec2.Length <= base.OwnerAgent.Monster.WalkingSpeedLimit * (_myLastStateWasRunning ? 1f : 1.2f))
				{
					_myLastStateWasRunning = false;
				}
				else
				{
					base.OwnerAgent.SetMaximumSpeedLimit(num - rangeThreshold + _selectedAgent.Velocity.AsVec2.Length, isMultiplier: false);
					_myLastStateWasRunning = true;
				}
			}
		}
		if (!_myLastStateWasRunning)
		{
			flags |= Agent.AIScriptedFrameFlags.DoNotRun;
		}
		base.Navigator.SetTargetFrame(pos, rotationInRadians, rangeThreshold, -10f, flags, flag);
	}

	public override void OnAgentRemoved(Agent agent)
	{
		if (agent == _selectedAgent)
		{
			base.OwnerAgent.ResetLookAgent();
			_selectedAgent = null;
		}
	}

	protected override void OnActivate()
	{
		if (_deactivatedAgent != null)
		{
			SetTargetAgent(_deactivatedAgent);
			_deactivatedAgent = null;
		}
	}

	protected override void OnDeactivate()
	{
		_state = State.Idle;
		_deactivatedAgent = _selectedAgent;
		_selectedAgent = null;
		base.OwnerAgent.DisableScriptedMovement();
		base.OwnerAgent.ResetLookAgent();
		base.Navigator.ClearTarget();
	}

	public override string GetDebugInfo()
	{
		return "Follow " + _selectedAgent.Name + " (id:" + _selectedAgent.Index + ")";
	}

	public override float GetAvailability(bool isSimulation)
	{
		return (_selectedAgent != null) ? 100 : 0;
	}
}
