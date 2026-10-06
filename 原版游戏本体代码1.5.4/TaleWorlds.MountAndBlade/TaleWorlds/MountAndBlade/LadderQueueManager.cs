using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class LadderQueueManager : MissionObject
{
	public int ManagedNavigationFaceId;

	public int ManagedNavigationFaceAlternateID1;

	public int ManagedNavigationFaceAlternateID2;

	public float CostAddition;

	private readonly List<Agent> _userAgents = new List<Agent>();

	private readonly List<Agent> _queuedAgents = new List<Agent>();

	private MatrixFrame _managedEntitialFrame;

	private Vec2 _managedEntitialDirection;

	private Vec3 _lastCachedGameEntityGlobalPosition;

	private MatrixFrame _managedGlobalFrame;

	private WorldPosition _managedGlobalWorldPosition;

	private Vec2 _managedGlobalDirection;

	private BattleSideEnum _managedSide;

	private bool _blockUsage;

	private int _maxUserCount;

	private int _queuedAgentCount;

	private float _arcAngle = System.MathF.PI * 3f / 4f;

	private float _queueBeginDistance = 1f;

	private float _queueRowSize = 0.8f;

	private float _agentSpacing = 1f;

	private float _timeSinceLastUpdate;

	private float _updatePeriod;

	private float _usingAgentResetTime;

	private float _costPerRow;

	private float _baseCost;

	private float _zDifferenceToStopUsing = 2f;

	private float _distanceToStopUsing2d = 5f;

	private bool _doesManageMultipleIDs;

	private int _maxClimberCount = 18;

	private int _maxRunnerCount = 6;

	private Timer _deactivateTimer;

	private bool _deactivationDelayTimerElapsed = true;

	private LadderQueueManager _neighborLadderQueueManager;

	private (float, bool)[] _lastUserCostPenaltyPerLadder;

	public bool IsDeactivated { get; private set; }

	protected internal override void OnInit()
	{
		base.OnInit();
		SetScriptComponentToTick(GetTickRequirement());
	}

	public void DeactivateImmediate()
	{
		IsDeactivated = true;
		_deactivationDelayTimerElapsed = true;
	}

	public void Deactivate()
	{
		IsDeactivated = true;
		_deactivateTimer.Reset(Mission.Current.CurrentTime, 10f);
		int num = Math.Min(2, _queuedAgentCount);
		int num2 = 0;
		for (int i = 0; i < _queuedAgents.Count; i++)
		{
			if (num <= 0)
			{
				break;
			}
			if (_queuedAgents[i] != null)
			{
				num2++;
				if (num2 == num)
				{
					RemoveAgentFromQueueAtIndex(i);
					num--;
					i = -1;
					num2 = 0;
				}
			}
		}
	}

	public void Activate()
	{
		IsDeactivated = false;
		_deactivationDelayTimerElapsed = false;
	}

	public void Initialize(int managedNavigationFaceId, MatrixFrame managedFrame, Vec3 managedDirection, BattleSideEnum managedSide, int maxUserCount, float arcAngle, float queueBeginDistance, float queueRowSize, float costPerRow, float baseCost, bool blockUsage, float agentSpacing, float zDifferenceToStopUsing, float distanceToStopUsing2d, bool doesManageMultipleIDs, int managedNavigationFaceAlternateID1, int managedNavigationFaceAlternateID2, int maxClimberCount, int maxRunnerCount)
	{
		ManagedNavigationFaceId = managedNavigationFaceId;
		_managedEntitialFrame = managedFrame;
		_managedEntitialDirection = managedDirection.AsVec2.Normalized();
		_managedGlobalFrame = base.GameEntity.GetGlobalFrame().TransformToParent(in managedFrame);
		_managedGlobalWorldPosition = new WorldPosition(base.GameEntity.GetScenePointer(), UIntPtr.Zero, _managedGlobalFrame.origin, hasValidZ: false);
		_managedGlobalWorldPosition.GetGroundVec3();
		_managedGlobalDirection = base.GameEntity.GetGlobalFrame().rotation.TransformToParent(in managedDirection).AsVec2.Normalized();
		_lastCachedGameEntityGlobalPosition = base.GameEntity.GetGlobalFrame().origin;
		_managedSide = managedSide;
		_maxUserCount = maxUserCount;
		_arcAngle = arcAngle;
		_queueBeginDistance = queueBeginDistance;
		_queueRowSize = queueRowSize;
		_costPerRow = costPerRow;
		_baseCost = baseCost;
		_blockUsage = blockUsage;
		_agentSpacing = agentSpacing;
		_zDifferenceToStopUsing = zDifferenceToStopUsing;
		_distanceToStopUsing2d = distanceToStopUsing2d;
		_doesManageMultipleIDs = doesManageMultipleIDs;
		ManagedNavigationFaceAlternateID1 = managedNavigationFaceAlternateID1;
		ManagedNavigationFaceAlternateID2 = managedNavigationFaceAlternateID2;
		_maxClimberCount = maxClimberCount;
		_maxRunnerCount = maxRunnerCount;
		_lastUserCostPenaltyPerLadder = new(float, bool)[3];
		_deactivateTimer = new Timer(0f, 0f);
	}

	public override TickRequirement GetTickRequirement()
	{
		if (!GameNetwork.IsClientOrReplay)
		{
			return base.GetTickRequirement() | TickRequirement.Tick | TickRequirement.TickParallel;
		}
		return base.GetTickRequirement();
	}

	private void UpdateGlobalFrameCache()
	{
		MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
		if (_lastCachedGameEntityGlobalPosition != globalFrame.origin)
		{
			_lastCachedGameEntityGlobalPosition = globalFrame.origin;
			_managedGlobalFrame = globalFrame.TransformToParent(in _managedEntitialFrame);
			_managedGlobalWorldPosition = new WorldPosition(base.GameEntity.GetScenePointer(), UIntPtr.Zero, _managedGlobalFrame.origin, hasValidZ: false);
			_managedGlobalWorldPosition.GetGroundVec3MT();
			_managedGlobalDirection = globalFrame.rotation.TransformToParent(new Vec3(_managedEntitialDirection)).AsVec2.Normalized();
		}
	}

	private void OnTickParallelAux(float dt)
	{
		if (IsDeactivated && !_deactivationDelayTimerElapsed && _deactivateTimer.Check(Mission.Current.CurrentTime))
		{
			_deactivationDelayTimerElapsed = true;
		}
		if (_deactivationDelayTimerElapsed)
		{
			_userAgents.Clear();
			for (int i = 0; i < _queuedAgents.Count; i++)
			{
				if (_queuedAgents[i] != null && _queuedAgents[i].IsActive())
				{
					RemoveAgentFromQueueAtIndex(i);
				}
			}
			_queuedAgents.Clear();
			return;
		}
		UpdateGlobalFrameCache();
		Vec3 groundVec = _managedGlobalWorldPosition.GetGroundVec3();
		_timeSinceLastUpdate += dt;
		if (_timeSinceLastUpdate < _updatePeriod)
		{
			return;
		}
		if (_neighborLadderQueueManager != null && (float)_neighborLadderQueueManager._queuedAgentCount < (float)_queuedAgentCount * 0.4f && _neighborLadderQueueManager.CostAddition < CostAddition * 0.6667f)
		{
			FlushQueueManager();
		}
		_usingAgentResetTime -= _timeSinceLastUpdate;
		_timeSinceLastUpdate = 0f;
		_updatePeriod = 0.2f + MBRandom.RandomFloat * 0.1f;
		StackArray.StackArray3Float stackArray3Float = default(StackArray.StackArray3Float);
		int num = 0;
		for (int num2 = _userAgents.Count - 1; num2 >= 0; num2--)
		{
			Agent agent = _userAgents[num2];
			bool flag = false;
			int currentNavigationFaceId = agent.GetCurrentNavigationFaceId();
			float num3 = ((_zDifferenceToStopUsing > 0.01f) ? ((agent.Position.z - groundVec.z) / _zDifferenceToStopUsing) : 1.01f);
			if (!agent.IsActive())
			{
				flag = true;
			}
			else if (_usingAgentResetTime < 0f && (num3 > 1f || (agent.Position.AsVec2 - groundVec.AsVec2).LengthSquared > _distanceToStopUsing2d * _distanceToStopUsing2d))
			{
				if (currentNavigationFaceId == ManagedNavigationFaceId || (_doesManageMultipleIDs && (currentNavigationFaceId == ManagedNavigationFaceAlternateID1 || currentNavigationFaceId == ManagedNavigationFaceAlternateID2)))
				{
					flag = true;
				}
				else if (!ShouldAgentUseTheLadder(agent))
				{
					flag = true;
				}
			}
			if (flag)
			{
				_userAgents[num2].HumanAIComponent?.AdjustSpeedLimit(_userAgents[num2], -1f, limitIsMultiplier: false);
				_userAgents[num2].SetIsLadderQueueUsing(isLadderQueueUsing: false);
				_userAgents.RemoveAt(num2);
			}
			else
			{
				bool flag2 = false;
				if (currentNavigationFaceId == ManagedNavigationFaceId)
				{
					stackArray3Float[0] += ((num3 < 1f) ? (1f - num3 * num3 * num3) : 0f);
					num++;
					flag2 = true;
				}
				else if (_doesManageMultipleIDs)
				{
					if (currentNavigationFaceId == ManagedNavigationFaceAlternateID1)
					{
						stackArray3Float[1] += ((num3 < 1f) ? (1f - num3 * num3 * num3) : 0f);
						num++;
						flag2 = true;
					}
					else if (currentNavigationFaceId == ManagedNavigationFaceAlternateID2)
					{
						stackArray3Float[2] += ((num3 < 1f) ? (1f - num3 * num3 * num3) : 0f);
						num++;
						flag2 = true;
					}
				}
				if (!flag2)
				{
					if (_userAgents[num2].HasPathThroughNavigationFaceIdFromDirectionMT(ManagedNavigationFaceId, _managedGlobalDirection))
					{
						stackArray3Float[0] += 0.3f;
					}
					else if (_userAgents[num2].HasPathThroughNavigationFaceIdFromDirectionMT(ManagedNavigationFaceAlternateID1, _managedGlobalDirection))
					{
						stackArray3Float[1] += 0.3f;
					}
					else if (_userAgents[num2].HasPathThroughNavigationFaceIdFromDirectionMT(ManagedNavigationFaceAlternateID2, _managedGlobalDirection))
					{
						stackArray3Float[2] += 0.3f;
					}
				}
			}
		}
		if (_neighborLadderQueueManager != null)
		{
			for (int num4 = _neighborLadderQueueManager._userAgents.Count - 1; num4 >= 0; num4--)
			{
				int currentNavigationFaceId2 = _neighborLadderQueueManager._userAgents[num4].GetCurrentNavigationFaceId();
				if (currentNavigationFaceId2 == ManagedNavigationFaceId)
				{
					stackArray3Float[0] += 0.3f;
					num++;
				}
				else if (_doesManageMultipleIDs)
				{
					if (currentNavigationFaceId2 == ManagedNavigationFaceAlternateID1)
					{
						stackArray3Float[1] += 0.3f;
						num++;
					}
					else if (currentNavigationFaceId2 == ManagedNavigationFaceAlternateID2)
					{
						stackArray3Float[2] += 0.3f;
						num++;
					}
				}
			}
		}
		for (int j = 0; j < 3; j++)
		{
			if (!_lastUserCostPenaltyPerLadder[j].Item1.ApproximatelyEqualsTo(stackArray3Float[j]))
			{
				_lastUserCostPenaltyPerLadder[j].Item1 = stackArray3Float[j];
				_lastUserCostPenaltyPerLadder[j].Item2 = true;
			}
		}
		for (int num5 = _queuedAgents.Count - 1; num5 >= 0; num5--)
		{
			if (_queuedAgents[num5] != null)
			{
				if (!ConditionsAreMet(_queuedAgents[num5], Agent.AIScriptedFrameFlags.GoToPosition))
				{
					RemoveAgentFromQueueAtIndex(num5);
				}
				else
				{
					float num6 = MBRandom.RandomFloat * (float)_maxUserCount;
					if (num6 > 0.7f)
					{
						GetParentIndicesForQueueIndex(num5, out var parentIndex, out var parentIndex2);
						if (parentIndex >= 0 && _queuedAgents[parentIndex] == null && num6 > ((parentIndex2 >= 0) ? 0.85f : 0.7f))
						{
							MoveAgentFromQueueIndexToQueueIndex(num5, parentIndex);
						}
						else if (parentIndex2 >= 0 && _queuedAgents[parentIndex2] == null)
						{
							MoveAgentFromQueueIndexToQueueIndex(num5, parentIndex2);
						}
					}
				}
			}
		}
		int num7 = _queuedAgents.Count - 1;
		while (num7 >= 0 && _queuedAgents[num7] == null)
		{
			_queuedAgents.RemoveAt(num7--);
		}
		AgentProximityMap.ProximityMapSearchStruct searchStruct = AgentProximityMap.BeginSearch(Mission.Current, groundVec.AsVec2, 30f);
		while (searchStruct.LastFoundAgent != null)
		{
			Agent lastFoundAgent = searchStruct.LastFoundAgent;
			if (ConditionsAreMet(lastFoundAgent, Agent.AIScriptedFrameFlags.None) && lastFoundAgent.Position.DistanceSquared(groundVec) < 900f && !_queuedAgents.Contains(lastFoundAgent) && lastFoundAgent.HasPathThroughNavigationFacesIDFromDirectionMT(ManagedNavigationFaceId, ManagedNavigationFaceAlternateID1, ManagedNavigationFaceAlternateID2, Vec2.Zero))
			{
				if (_neighborLadderQueueManager == null)
				{
					AddAgentToQueue(lastFoundAgent);
				}
				else if (!_neighborLadderQueueManager._userAgents.Contains(lastFoundAgent))
				{
					AddAgentToQueue(lastFoundAgent);
				}
				else
				{
					lastFoundAgent.SetIsLadderQueueUsing(isLadderQueueUsing: true);
					_userAgents.Add(lastFoundAgent);
				}
			}
			AgentProximityMap.FindNext(Mission.Current, ref searchStruct);
		}
		int num8 = _userAgents.Count - num;
		int num9 = Math.Min(_maxClimberCount - num, _maxRunnerCount - num8);
		if (_blockUsage || num9 <= 0)
		{
			return;
		}
		float num10 = float.MaxValue;
		int num11 = -1;
		for (int k = 0; k < _queuedAgents.Count; k++)
		{
			if (_queuedAgents[k] != null)
			{
				float lengthSquared = (_queuedAgents[k].Position - groundVec).LengthSquared;
				if (lengthSquared < num10)
				{
					num10 = lengthSquared;
					num11 = k;
				}
			}
		}
		if (num11 >= 0)
		{
			_queuedAgents[num11].SetIsLadderQueueUsing(isLadderQueueUsing: true);
			_userAgents.Add(_queuedAgents[num11]);
			_queuedAgents[num11].HumanAIComponent.AdjustSpeedLimit(_queuedAgents[num11], 0.2f, limitIsMultiplier: true);
			_usingAgentResetTime = 2f;
			RemoveAgentFromQueueAtIndex(num11);
		}
	}

	protected internal override void OnTickParallel(float dt)
	{
		if (_neighborLadderQueueManager == null)
		{
			OnTickParallelAux(dt);
			return;
		}
		lock ((base.Id.Id < _neighborLadderQueueManager.Id.Id) ? this : _neighborLadderQueueManager)
		{
			OnTickParallelAux(dt);
		}
	}

	protected internal override void OnTick(float dt)
	{
		if (!GameNetwork.IsClientOrReplay && !IsDeactivated && !_blockUsage)
		{
			if (ManagedNavigationFaceId > 1 && _lastUserCostPenaltyPerLadder[0].Item2)
			{
				_lastUserCostPenaltyPerLadder[0].Item2 = false;
				CostAddition = GetNavigationFaceCostPerClimber(_lastUserCostPenaltyPerLadder[0].Item1);
				Mission.Current.SetNavigationFaceCostWithIdAroundPosition(ManagedNavigationFaceId, _managedGlobalWorldPosition.GetGroundVec3(), CostAddition);
			}
			if (ManagedNavigationFaceAlternateID1 > 1 && _lastUserCostPenaltyPerLadder[1].Item2)
			{
				_lastUserCostPenaltyPerLadder[1].Item2 = false;
				Mission.Current.SetNavigationFaceCostWithIdAroundPosition(ManagedNavigationFaceAlternateID1, _managedGlobalWorldPosition.GetGroundVec3(), GetNavigationFaceCostPerClimber(_lastUserCostPenaltyPerLadder[1].Item1));
			}
			if (ManagedNavigationFaceAlternateID2 > 1 && _lastUserCostPenaltyPerLadder[2].Item2)
			{
				_lastUserCostPenaltyPerLadder[2].Item2 = false;
				Mission.Current.SetNavigationFaceCostWithIdAroundPosition(ManagedNavigationFaceAlternateID2, _managedGlobalWorldPosition.GetGroundVec3(), GetNavigationFaceCostPerClimber(_lastUserCostPenaltyPerLadder[2].Item1));
			}
		}
	}

	private bool ConditionsAreMet(Agent agent, Agent.AIScriptedFrameFlags flags)
	{
		if (agent.IsAIControlled && agent.IsActive() && agent.Team != null && agent.Team.Side == _managedSide && agent.MovementLockedState == AgentMovementLockedState.None && !agent.IsUsingGameObject && !agent.InteractingWithAnyGameObject() && !agent.IsDetachedFromFormation && agent.Position.z - _managedGlobalWorldPosition.GetGroundZ() < _zDifferenceToStopUsing && !_userAgents.Contains(agent) && agent.GetScriptedFlags() == flags && agent.GetCurrentNavigationFaceId() != ManagedNavigationFaceId)
		{
			if (_doesManageMultipleIDs)
			{
				if (agent.GetCurrentNavigationFaceId() != ManagedNavigationFaceAlternateID1)
				{
					return agent.GetCurrentNavigationFaceId() != ManagedNavigationFaceAlternateID2;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	protected internal override void OnMissionReset()
	{
		base.OnMissionReset();
		_userAgents.Clear();
		IsDeactivated = true;
		_deactivationDelayTimerElapsed = true;
		_queuedAgents.Clear();
		_queuedAgentCount = 0;
	}

	private void GetParentIndicesForQueueIndex(int queueIndex, out int parentIndex1, out int parentIndex2)
	{
		parentIndex1 = -1;
		parentIndex2 = -1;
		Vec2i coordinatesForQueueIndex = GetCoordinatesForQueueIndex(queueIndex);
		int num = coordinatesForQueueIndex.Y - 1;
		if (num < 0)
		{
			return;
		}
		int num2 = TaleWorlds.Library.MathF.Max(GetRowSize(num) - 1, 1);
		int num3 = TaleWorlds.Library.MathF.Max(GetRowSize(coordinatesForQueueIndex.Y) - 1, 1);
		float num4 = (float)coordinatesForQueueIndex.X * (float)num2 / (float)num3;
		parentIndex1 = (int)num4;
		float num5 = TaleWorlds.Library.MathF.Abs(num4 - (float)parentIndex1);
		if (num5 > 0.2f)
		{
			if (num5 > 0.8f)
			{
				parentIndex1++;
			}
			else
			{
				parentIndex2 = parentIndex1 + 1;
			}
		}
		parentIndex1 = GetQueueIndexForCoordinates(new Vec2i(parentIndex1, num));
		if (parentIndex2 >= 0)
		{
			parentIndex2 = GetQueueIndexForCoordinates(new Vec2i(parentIndex2, num));
		}
	}

	private float GetScoreForAddingAgentToQueueIndex(Vec3 agentPosition, int queueIndex, out int scoreOfQueueIndex)
	{
		scoreOfQueueIndex = queueIndex;
		float num = float.MinValue;
		if (_queuedAgents.Count <= queueIndex || _queuedAgents[queueIndex] == null)
		{
			GetParentIndicesForQueueIndex(queueIndex, out var parentIndex, out var parentIndex2);
			if (parentIndex < 0 || (_queuedAgents.Count > parentIndex && _queuedAgents[parentIndex] != null) || (parentIndex2 >= 0 && _queuedAgents.Count > parentIndex2 && _queuedAgents[parentIndex2] != null))
			{
				Vec2i coordinatesForQueueIndex = GetCoordinatesForQueueIndex(queueIndex);
				num = (float)coordinatesForQueueIndex.Y * _queueRowSize * -3f;
				num -= (agentPosition.AsVec2 - GetQueuePositionForCoordinates(coordinatesForQueueIndex, -1).AsVec2).Length;
			}
			if (parentIndex >= 0 && (_queuedAgents.Count <= parentIndex || _queuedAgents[parentIndex] == null))
			{
				int scoreOfQueueIndex2;
				float scoreForAddingAgentToQueueIndex = GetScoreForAddingAgentToQueueIndex(agentPosition, parentIndex, out scoreOfQueueIndex2);
				if (num < scoreForAddingAgentToQueueIndex)
				{
					scoreOfQueueIndex = scoreOfQueueIndex2;
					num = scoreForAddingAgentToQueueIndex;
				}
			}
			if (parentIndex2 >= 0 && (_queuedAgents.Count <= parentIndex2 || _queuedAgents[parentIndex2] == null))
			{
				int scoreOfQueueIndex3;
				float scoreForAddingAgentToQueueIndex2 = GetScoreForAddingAgentToQueueIndex(agentPosition, parentIndex2, out scoreOfQueueIndex3);
				if (num < scoreForAddingAgentToQueueIndex2)
				{
					scoreOfQueueIndex = scoreOfQueueIndex3;
					num = scoreForAddingAgentToQueueIndex2;
				}
			}
		}
		return num;
	}

	private void AddAgentToQueue(Agent agent)
	{
		int y = GetCoordinatesForQueueIndex(_queuedAgents.Count).Y;
		int rowSize = GetRowSize(y);
		Vec3 position = agent.Position;
		int num = -1;
		float num2 = float.MinValue;
		for (int i = 0; i < rowSize; i++)
		{
			int scoreOfQueueIndex;
			float scoreForAddingAgentToQueueIndex = GetScoreForAddingAgentToQueueIndex(position, GetQueueIndexForCoordinates(new Vec2i(i, y)), out scoreOfQueueIndex);
			if (scoreForAddingAgentToQueueIndex > num2)
			{
				num2 = scoreForAddingAgentToQueueIndex;
				num = scoreOfQueueIndex;
			}
		}
		while (_queuedAgents.Count <= num)
		{
			_queuedAgents.Add(null);
		}
		_queuedAgents[num] = agent;
		WorldPosition position2 = GetQueuePositionForIndex(num, agent.Index);
		agent.SetScriptedPosition(ref position2, addHumanLikeDelay: false);
		agent.SetIsInLadderQueue(isInLadderQueue: true);
		_queuedAgentCount++;
	}

	private void RemoveAgentFromQueueAtIndex(int queueIndex)
	{
		_queuedAgentCount--;
		if (!_queuedAgents[queueIndex].IsUsingGameObject && (!_queuedAgents[queueIndex].IsAIControlled || !_queuedAgents[queueIndex].AIMoveToGameObjectIsEnabled()))
		{
			_queuedAgents[queueIndex].DisableScriptedMovement();
			_queuedAgents[queueIndex].SetIsInLadderQueue(isInLadderQueue: false);
		}
		_queuedAgents[queueIndex] = null;
	}

	private float GetNavigationFaceCost(int rowIndex)
	{
		return _baseCost + (float)TaleWorlds.Library.MathF.Max(rowIndex - 1, 0) * _costPerRow;
	}

	private float GetNavigationFaceCostPerClimber(float costPenalty)
	{
		return _baseCost + costPenalty * _costPerRow;
	}

	private void MoveAgentFromQueueIndexToQueueIndex(int fromQueueIndex, int toQueueIndex)
	{
		_queuedAgents[toQueueIndex] = _queuedAgents[fromQueueIndex];
		_queuedAgents[fromQueueIndex] = null;
		WorldPosition position = GetQueuePositionForIndex(toQueueIndex, _queuedAgents[toQueueIndex].Index);
		_queuedAgents[toQueueIndex].SetScriptedPosition(ref position, addHumanLikeDelay: false);
	}

	private int GetRowSize(int rowIndex)
	{
		float num = _arcAngle * (_queueBeginDistance + _queueRowSize * (float)rowIndex);
		return 1 + (int)(num / _agentSpacing);
	}

	private int GetQueueIndexForCoordinates(Vec2i coordinates)
	{
		int num = coordinates.X;
		for (int i = 0; i < coordinates.Y; i++)
		{
			num += GetRowSize(i);
		}
		return num;
	}

	private Vec2i GetCoordinatesForQueueIndex(int queueIndex)
	{
		Vec2i result = default(Vec2i);
		while (true)
		{
			int rowSize = GetRowSize(result.Y);
			if (rowSize > queueIndex)
			{
				break;
			}
			queueIndex -= rowSize;
			result.Y++;
		}
		result.X = queueIndex;
		return result;
	}

	private WorldPosition GetQueuePositionForCoordinates(Vec2i coordinates, int randomSeed)
	{
		MatrixFrame managedGlobalFrame = _managedGlobalFrame;
		WorldPosition managedGlobalWorldPosition = _managedGlobalWorldPosition;
		float a = 0f;
		int rowSize = GetRowSize(coordinates.Y);
		if (rowSize > 1)
		{
			a = _arcAngle * ((float)coordinates.X / (float)(rowSize - 1) - 0.5f);
		}
		managedGlobalFrame.rotation.RotateAboutForward(a);
		managedGlobalFrame.origin += managedGlobalFrame.rotation.u * (_queueBeginDistance + _queueRowSize * (float)coordinates.Y);
		if (randomSeed >= 0)
		{
			Random random = new Random(coordinates.X * 100000 + coordinates.Y * 10000000 + randomSeed);
			managedGlobalFrame.rotation.RotateAboutForward(random.NextFloat() * System.MathF.PI * 2f);
			managedGlobalFrame.origin += managedGlobalFrame.rotation.u * random.NextFloat() * 0.3f;
		}
		managedGlobalWorldPosition.SetVec2(managedGlobalFrame.origin.AsVec2);
		return managedGlobalWorldPosition;
	}

	private WorldPosition GetQueuePositionForIndex(int queueIndex, int randomSeed)
	{
		return GetQueuePositionForCoordinates(GetCoordinatesForQueueIndex(queueIndex), randomSeed);
	}

	public void FlushQueueManager()
	{
		int num = _queuedAgentCount / 2;
		for (int num2 = _queuedAgents.Count - 1; num2 >= num; num2--)
		{
			if (_queuedAgents[num2] != null)
			{
				RemoveAgentFromQueueAtIndex(num2);
			}
		}
	}

	public void AssignNeighborQueueManager(LadderQueueManager neighborLadderQueueManager)
	{
		_neighborLadderQueueManager = neighborLadderQueueManager;
	}

	private bool IsFormationPositionOtherSideOfTheCastle(Agent agent)
	{
		UIntPtr navMesh = agent.Formation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.NavMeshVec3).GetNavMesh();
		UIntPtr navMesh2 = agent.GetWorldPosition().GetNavMesh();
		PathFaceRecord pathFaceRecordFromNavMeshFacePointer = base.Scene.GetPathFaceRecordFromNavMeshFacePointer(navMesh);
		PathFaceRecord pathFaceRecordFromNavMeshFacePointer2 = base.Scene.GetPathFaceRecordFromNavMeshFacePointer(navMesh2);
		return pathFaceRecordFromNavMeshFacePointer.FaceGroupIndex % 10 != pathFaceRecordFromNavMeshFacePointer2.FaceGroupIndex % 10;
	}

	private bool ShouldAgentUseTheLadder(Agent agent)
	{
		if (!agent.IsFormationFrameEnabled)
		{
			return agent.HasPathThroughNavigationFacesIDFromDirectionMT(ManagedNavigationFaceId, ManagedNavigationFaceAlternateID1, ManagedNavigationFaceAlternateID2, Vec2.Zero);
		}
		return IsFormationPositionOtherSideOfTheCastle(agent);
	}

	public void OnFormationFrameChanged(Agent agent, bool hasFrame, WorldPosition frame)
	{
		if (agent.IsInLadderQueue && _queuedAgents.Contains(agent) && (!ConditionsAreMet(agent, Agent.AIScriptedFrameFlags.GoToPosition) || !ShouldAgentUseTheLadder(agent)))
		{
			RemoveAgentFromQueueAtIndex(_queuedAgents.IndexOf(agent));
		}
	}
}
