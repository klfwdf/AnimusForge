using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Conversation;
using SandBox.Conversation.MissionLogics;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox;

public sealed class AgentNavigator
{
	public enum NavigationState
	{
		NoTarget,
		GoToTarget,
		AtTargetPosition,
		UseMachine
	}

	private const float SeeingDistance = 30f;

	public readonly Agent OwnerAgent;

	private readonly Mission _mission;

	private readonly List<AgentBehaviorGroup> _behaviorGroups;

	private readonly ItemObject _specialItem;

	private UsableMachineAIBase _targetBehavior;

	private bool _targetReached;

	private float _rangeThreshold;

	private float _rotationScoreThreshold;

	private string _specialTargetTag;

	private bool _disableClearTargetWhenTargetIsReached;

	private readonly Dictionary<sbyte, string> _prefabNamesForBones;

	private readonly List<int> _prevPrefabs;

	private readonly MissionConversationLogic _conversationHandler;

	private readonly BasicMissionTimer _checkBehaviorGroupsTimer;

	public UsableMachine TargetUsableMachine { get; private set; }

	public WorldPosition TargetPosition { get; private set; }

	public Vec2 TargetDirection { get; private set; }

	public GameEntity TargetEntity { get; private set; }

	public Alley MemberOfAlley { get; private set; }

	public string SpecialTargetTag
	{
		get
		{
			return _specialTargetTag;
		}
		set
		{
			if (value != _specialTargetTag)
			{
				_specialTargetTag = value;
				GetActiveBehavior()?.OnSpecialTargetChanged();
			}
		}
	}

	private Dictionary<KeyValuePair<sbyte, string>, int> _bodyComponents { get; set; }

	public NavigationState _agentState { get; private set; }

	public bool CharacterHasVisiblePrefabs { get; private set; }

	public AgentNavigator(Agent agent, LocationCharacter locationCharacter)
		: this(agent)
	{
		SpecialTargetTag = locationCharacter.SpecialTargetTag;
		_prefabNamesForBones = locationCharacter.PrefabNamesForBones;
		_specialItem = locationCharacter.SpecialItem;
		MemberOfAlley = locationCharacter.MemberOfAlley;
		SetItemsVisibility(isVisible: true);
		SetSpecialItem();
	}

	public AgentNavigator(Agent agent)
	{
		_mission = agent.Mission;
		_conversationHandler = _mission.GetMissionBehavior<MissionConversationLogic>();
		OwnerAgent = agent;
		_prefabNamesForBones = new Dictionary<sbyte, string>();
		_behaviorGroups = new List<AgentBehaviorGroup>();
		_bodyComponents = new Dictionary<KeyValuePair<sbyte, string>, int>();
		SpecialTargetTag = string.Empty;
		MemberOfAlley = null;
		TargetUsableMachine = null;
		_checkBehaviorGroupsTimer = new BasicMissionTimer();
		_prevPrefabs = new List<int>();
		CharacterHasVisiblePrefabs = false;
	}

	public void OnStopUsingGameObject()
	{
		_targetBehavior = null;
		TargetUsableMachine = null;
		_agentState = NavigationState.NoTarget;
	}

	public void OnAgentRemoved(Agent agent)
	{
		foreach (AgentBehaviorGroup behaviorGroup in _behaviorGroups)
		{
			behaviorGroup.OnAgentRemoved(agent);
		}
	}

	public void SetTarget(UsableMachine usableMachine, bool isInitialTarget = false, Agent.AIScriptedFrameFlags customFlags = Agent.AIScriptedFrameFlags.None)
	{
		if (usableMachine == null)
		{
			((IDetachment)TargetUsableMachine)?.RemoveAgent(OwnerAgent);
			TargetUsableMachine = null;
			OwnerAgent.DisableScriptedMovement();
			OwnerAgent.ClearTargetFrame();
			TargetPosition = WorldPosition.Invalid;
			TargetEntity = null;
			_agentState = NavigationState.NoTarget;
		}
		else if (TargetUsableMachine != usableMachine || isInitialTarget)
		{
			TargetPosition = WorldPosition.Invalid;
			_agentState = NavigationState.NoTarget;
			((IDetachment)TargetUsableMachine)?.RemoveAgent(OwnerAgent);
			if (usableMachine.IsStandingPointAvailableForAgent(OwnerAgent))
			{
				TargetUsableMachine = usableMachine;
				TargetPosition = WorldPosition.Invalid;
				_agentState = NavigationState.UseMachine;
				_targetBehavior = TargetUsableMachine.CreateAIBehaviorObject();
				((IDetachment)TargetUsableMachine).AddAgent(OwnerAgent, -1, customFlags);
				_targetReached = false;
			}
		}
	}

	public void SetTargetFrame(WorldPosition position, float rotation, float rangeThreshold = 1f, float rotationThreshold = -10f, Agent.AIScriptedFrameFlags flags = Agent.AIScriptedFrameFlags.None, bool disableClearTargetWhenTargetIsReached = false)
	{
		if (_agentState != NavigationState.NoTarget)
		{
			ClearTarget();
		}
		TargetPosition = position;
		TargetDirection = Vec2.FromRotation(rotation);
		_rangeThreshold = rangeThreshold;
		_rotationScoreThreshold = rotationThreshold;
		_disableClearTargetWhenTargetIsReached = disableClearTargetWhenTargetIsReached;
		if (IsTargetReached())
		{
			TargetPosition = WorldPosition.Invalid;
			_agentState = NavigationState.NoTarget;
		}
		else
		{
			OwnerAgent.SetScriptedPositionAndDirection(ref position, rotation, addHumanLikeDelay: false, flags);
			_agentState = NavigationState.GoToTarget;
		}
	}

	public void ClearTarget()
	{
		SetTarget(null);
	}

	public void Tick(float dt, bool isSimulation = false)
	{
		HandleBehaviorGroups(isSimulation);
		if (ConversationMission.ConversationAgents.Contains(OwnerAgent))
		{
			foreach (AgentBehaviorGroup behaviorGroup in _behaviorGroups)
			{
				if (behaviorGroup.IsActive)
				{
					behaviorGroup.ConversationTick();
				}
			}
		}
		else
		{
			TickBehaviorGroups(dt, isSimulation);
		}
		if (TargetUsableMachine != null)
		{
			_targetBehavior.Tick(OwnerAgent, null, null, dt);
		}
		else
		{
			HandleMovement();
		}
		if (TargetUsableMachine != null && isSimulation)
		{
			_targetBehavior.TeleportUserAgentsToMachine(new List<Agent> { OwnerAgent });
		}
	}

	public float GetDistanceToTarget(UsableMachine target)
	{
		float result = 100000f;
		if (target != null && OwnerAgent.CurrentlyUsedGameObject != null)
		{
			result = OwnerAgent.CurrentlyUsedGameObject.GetUserFrameForAgent(OwnerAgent).Origin.GetGroundVec3().Distance(OwnerAgent.Position);
		}
		return result;
	}

	public bool IsTargetReached()
	{
		if (TargetDirection.IsValid && TargetPosition.IsValid)
		{
			float num = Vec2.DotProduct(TargetDirection, OwnerAgent.GetMovementDirection());
			_targetReached = (OwnerAgent.Position - TargetPosition.GetGroundVec3()).LengthSquared < _rangeThreshold * _rangeThreshold && num > _rotationScoreThreshold;
		}
		return _targetReached;
	}

	private void HandleMovement()
	{
		if (_agentState == NavigationState.GoToTarget && IsTargetReached())
		{
			_agentState = NavigationState.AtTargetPosition;
			if (!_disableClearTargetWhenTargetIsReached)
			{
				OwnerAgent.ClearTargetFrame();
			}
		}
	}

	public void HoldAndHideRecentlyUsedMeshes()
	{
		foreach (KeyValuePair<KeyValuePair<sbyte, string>, int> bodyComponent in _bodyComponents)
		{
			if (OwnerAgent.IsSynchedPrefabComponentVisible(bodyComponent.Value))
			{
				OwnerAgent.SetSynchedPrefabComponentVisibility(bodyComponent.Value, visibility: false);
				_prevPrefabs.Add(bodyComponent.Value);
			}
		}
	}

	public void RecoverRecentlyUsedMeshes()
	{
		foreach (int prevPrefab in _prevPrefabs)
		{
			OwnerAgent.SetSynchedPrefabComponentVisibility(prevPrefab, visibility: true);
		}
		_prevPrefabs.Clear();
	}

	public bool CanSeeAgent(Agent otherAgent)
	{
		if ((OwnerAgent.Position - otherAgent.Position).Length < 30f)
		{
			Vec3 eyeGlobalPosition = otherAgent.GetEyeGlobalPosition();
			Vec3 eyeGlobalPosition2 = OwnerAgent.GetEyeGlobalPosition();
			float collisionDistance;
			if (TaleWorlds.Library.MathF.Abs(Vec3.AngleBetweenTwoVectors(otherAgent.Position - OwnerAgent.Position, OwnerAgent.LookDirection)) < 1.5f)
			{
				return !Mission.Current.Scene.RayCastForClosestEntityOrTerrain(eyeGlobalPosition2, eyeGlobalPosition, out collisionDistance);
			}
		}
		return false;
	}

	public bool IsCarryingSomething()
	{
		if (OwnerAgent.GetPrimaryWieldedItemIndex() >= EquipmentIndex.WeaponItemBeginSlot || OwnerAgent.GetOffhandWieldedItemIndex() >= EquipmentIndex.WeaponItemBeginSlot)
		{
			return true;
		}
		return _bodyComponents.Any((KeyValuePair<KeyValuePair<sbyte, string>, int> component) => OwnerAgent.IsSynchedPrefabComponentVisible(component.Value));
	}

	public void SetPrefabVisibility(sbyte realBoneIndex, string prefabName, bool isVisible)
	{
		KeyValuePair<sbyte, string> key = new KeyValuePair<sbyte, string>(realBoneIndex, prefabName);
		int value2;
		if (isVisible)
		{
			if (!_bodyComponents.TryGetValue(key, out var value))
			{
				_bodyComponents.Add(key, OwnerAgent.AddSynchedPrefabComponentToBone(prefabName, realBoneIndex));
			}
			else if (!OwnerAgent.IsSynchedPrefabComponentVisible(value))
			{
				OwnerAgent.SetSynchedPrefabComponentVisibility(value, visibility: true);
			}
		}
		else if (_bodyComponents.TryGetValue(key, out value2) && OwnerAgent.IsSynchedPrefabComponentVisible(value2))
		{
			OwnerAgent.SetSynchedPrefabComponentVisibility(value2, visibility: false);
		}
	}

	public bool GetPrefabVisibility(sbyte realBoneIndex, string prefabName)
	{
		KeyValuePair<sbyte, string> key = new KeyValuePair<sbyte, string>(realBoneIndex, prefabName);
		if (_bodyComponents.TryGetValue(key, out var value) && OwnerAgent.IsSynchedPrefabComponentVisible(value))
		{
			return true;
		}
		return false;
	}

	public void SetSpecialItem()
	{
		if (_specialItem == null)
		{
			return;
		}
		bool flag = false;
		EquipmentIndex equipmentIndex = EquipmentIndex.None;
		for (EquipmentIndex equipmentIndex2 = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex2 <= EquipmentIndex.Weapon3; equipmentIndex2++)
		{
			if (OwnerAgent.Equipment[equipmentIndex2].IsEmpty)
			{
				equipmentIndex = equipmentIndex2;
			}
			else if (OwnerAgent.Equipment[equipmentIndex2].Item == _specialItem)
			{
				equipmentIndex = equipmentIndex2;
				flag = true;
				break;
			}
		}
		if (equipmentIndex == EquipmentIndex.None)
		{
			OwnerAgent.DropItem(EquipmentIndex.Weapon3);
			equipmentIndex = EquipmentIndex.Weapon3;
		}
		if (!flag)
		{
			MissionWeapon weapon = new MissionWeapon(_specialItem, null, OwnerAgent.Origin?.Banner);
			OwnerAgent.EquipWeaponWithNewEntity(equipmentIndex, ref weapon);
		}
		OwnerAgent.TryToWieldWeaponInSlot(equipmentIndex, Agent.WeaponWieldActionType.Instant, isWieldedOnSpawn: false);
	}

	public void SetItemsVisibility(bool isVisible)
	{
		foreach (KeyValuePair<sbyte, string> prefabNamesForBone in _prefabNamesForBones)
		{
			SetPrefabVisibility(prefabNamesForBone.Key, prefabNamesForBone.Value, isVisible);
		}
		CharacterHasVisiblePrefabs = _prefabNamesForBones.Count > 0 && isVisible;
	}

	public void SetCommonArea(Alley alley)
	{
		if (alley != MemberOfAlley)
		{
			MemberOfAlley = alley;
			SpecialTargetTag = ((alley == null) ? "" : alley.Tag);
		}
	}

	public void ForceThink(float inSeconds)
	{
		foreach (AgentBehaviorGroup behaviorGroup in _behaviorGroups)
		{
			behaviorGroup.ForceThink(inSeconds);
		}
	}

	public T AddBehaviorGroup<T>() where T : AgentBehaviorGroup
	{
		T val = GetBehaviorGroup<T>();
		if (val == null)
		{
			val = Activator.CreateInstance(typeof(T), this, _mission) as T;
			if (val != null)
			{
				_behaviorGroups.Add(val);
			}
		}
		return val;
	}

	public T GetBehaviorGroup<T>() where T : AgentBehaviorGroup
	{
		foreach (AgentBehaviorGroup behaviorGroup in _behaviorGroups)
		{
			if (behaviorGroup is T)
			{
				return (T)behaviorGroup;
			}
		}
		return null;
	}

	public AgentBehavior GetBehavior<T>() where T : AgentBehavior
	{
		foreach (AgentBehaviorGroup behaviorGroup in _behaviorGroups)
		{
			foreach (AgentBehavior behavior in behaviorGroup.Behaviors)
			{
				if (behavior.GetType() == typeof(T))
				{
					return behavior;
				}
			}
		}
		return null;
	}

	public bool HasBehaviorGroup<T>()
	{
		foreach (AgentBehaviorGroup behaviorGroup in _behaviorGroups)
		{
			if (behaviorGroup.GetType() is T)
			{
				return true;
			}
		}
		return false;
	}

	public void RemoveBehaviorGroup<T>() where T : AgentBehaviorGroup
	{
		for (int i = 0; i < _behaviorGroups.Count; i++)
		{
			if (_behaviorGroups[i] is T)
			{
				_behaviorGroups.RemoveAt(i);
			}
		}
	}

	public void RefreshBehaviorGroups(bool isSimulation)
	{
		_checkBehaviorGroupsTimer.Reset();
		float num = 0f;
		AgentBehaviorGroup agentBehaviorGroup = null;
		foreach (AgentBehaviorGroup behaviorGroup in _behaviorGroups)
		{
			float score = behaviorGroup.GetScore(isSimulation);
			if (score > num)
			{
				num = score;
				agentBehaviorGroup = behaviorGroup;
			}
		}
		if (num > 0f && agentBehaviorGroup != null && !agentBehaviorGroup.IsActive)
		{
			ActivateGroup(agentBehaviorGroup);
		}
	}

	private void ActivateGroup(AgentBehaviorGroup behaviorGroup)
	{
		foreach (AgentBehaviorGroup behaviorGroup2 in _behaviorGroups)
		{
			behaviorGroup2.IsActive = false;
		}
		behaviorGroup.IsActive = true;
	}

	private void HandleBehaviorGroups(bool isSimulation)
	{
		if (isSimulation || _checkBehaviorGroupsTimer.ElapsedTime > 1f)
		{
			RefreshBehaviorGroups(isSimulation);
		}
	}

	private void TickBehaviorGroups(float dt, bool isSimulation)
	{
		if (!OwnerAgent.IsActive())
		{
			return;
		}
		foreach (AgentBehaviorGroup behaviorGroup in _behaviorGroups)
		{
			behaviorGroup.Tick(dt, isSimulation);
		}
	}

	public AgentBehavior GetActiveBehavior()
	{
		foreach (AgentBehaviorGroup behaviorGroup in _behaviorGroups)
		{
			if (behaviorGroup.IsActive)
			{
				return behaviorGroup.GetActiveBehavior();
			}
		}
		return null;
	}

	public AgentBehaviorGroup GetActiveBehaviorGroup()
	{
		foreach (AgentBehaviorGroup behaviorGroup in _behaviorGroups)
		{
			if (behaviorGroup.IsActive)
			{
				return behaviorGroup;
			}
		}
		return null;
	}
}
