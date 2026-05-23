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

namespace SandBox
{
	// Token: 0x0200001D RID: 29
	public sealed class AgentNavigator
	{
		// Token: 0x17000002 RID: 2
		// (get) Token: 0x0600008F RID: 143 RVA: 0x00005038 File Offset: 0x00003238
		// (set) Token: 0x06000090 RID: 144 RVA: 0x00005040 File Offset: 0x00003240
		public UsableMachine TargetUsableMachine { get; private set; }

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000091 RID: 145 RVA: 0x00005049 File Offset: 0x00003249
		// (set) Token: 0x06000092 RID: 146 RVA: 0x00005051 File Offset: 0x00003251
		public WorldPosition TargetPosition { get; private set; }

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000093 RID: 147 RVA: 0x0000505A File Offset: 0x0000325A
		// (set) Token: 0x06000094 RID: 148 RVA: 0x00005062 File Offset: 0x00003262
		public Vec2 TargetDirection { get; private set; }

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000095 RID: 149 RVA: 0x0000506B File Offset: 0x0000326B
		// (set) Token: 0x06000096 RID: 150 RVA: 0x00005073 File Offset: 0x00003273
		public GameEntity TargetEntity { get; private set; }

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000097 RID: 151 RVA: 0x0000507C File Offset: 0x0000327C
		// (set) Token: 0x06000098 RID: 152 RVA: 0x00005084 File Offset: 0x00003284
		public Alley MemberOfAlley { get; private set; }

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000099 RID: 153 RVA: 0x0000508D File Offset: 0x0000328D
		// (set) Token: 0x0600009A RID: 154 RVA: 0x00005098 File Offset: 0x00003298
		public string SpecialTargetTag
		{
			get
			{
				return this._specialTargetTag;
			}
			set
			{
				if (value != this._specialTargetTag)
				{
					this._specialTargetTag = value;
					AgentBehavior activeBehavior = this.GetActiveBehavior();
					if (activeBehavior != null)
					{
						activeBehavior.OnSpecialTargetChanged();
					}
				}
			}
		}

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x0600009B RID: 155 RVA: 0x000050CA File Offset: 0x000032CA
		// (set) Token: 0x0600009C RID: 156 RVA: 0x000050D2 File Offset: 0x000032D2
		private Dictionary<KeyValuePair<sbyte, string>, int> _bodyComponents { get; set; }

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x0600009D RID: 157 RVA: 0x000050DB File Offset: 0x000032DB
		// (set) Token: 0x0600009E RID: 158 RVA: 0x000050E3 File Offset: 0x000032E3
		public AgentNavigator.NavigationState _agentState { get; private set; }

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x0600009F RID: 159 RVA: 0x000050EC File Offset: 0x000032EC
		// (set) Token: 0x060000A0 RID: 160 RVA: 0x000050F4 File Offset: 0x000032F4
		public bool CharacterHasVisiblePrefabs { get; private set; }

		// Token: 0x060000A1 RID: 161 RVA: 0x00005100 File Offset: 0x00003300
		public AgentNavigator(Agent agent, LocationCharacter locationCharacter)
			: this(agent)
		{
			this.SpecialTargetTag = locationCharacter.SpecialTargetTag;
			this._prefabNamesForBones = locationCharacter.PrefabNamesForBones;
			this._specialItem = locationCharacter.SpecialItem;
			this.MemberOfAlley = locationCharacter.MemberOfAlley;
			this.SetItemsVisibility(true);
			this.SetSpecialItem();
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x00005154 File Offset: 0x00003354
		public AgentNavigator(Agent agent)
		{
			this._mission = agent.Mission;
			this._conversationHandler = this._mission.GetMissionBehavior<MissionConversationLogic>();
			this.OwnerAgent = agent;
			this._prefabNamesForBones = new Dictionary<sbyte, string>();
			this._behaviorGroups = new List<AgentBehaviorGroup>();
			this._bodyComponents = new Dictionary<KeyValuePair<sbyte, string>, int>();
			this.SpecialTargetTag = string.Empty;
			this.MemberOfAlley = null;
			this.TargetUsableMachine = null;
			this._checkBehaviorGroupsTimer = new BasicMissionTimer();
			this._prevPrefabs = new List<int>();
			this.CharacterHasVisiblePrefabs = false;
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x000051E2 File Offset: 0x000033E2
		public void OnStopUsingGameObject()
		{
			this._targetBehavior = null;
			this.TargetUsableMachine = null;
			this._agentState = AgentNavigator.NavigationState.NoTarget;
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x000051FC File Offset: 0x000033FC
		public void OnAgentRemoved(Agent agent)
		{
			foreach (AgentBehaviorGroup agentBehaviorGroup in this._behaviorGroups)
			{
				agentBehaviorGroup.OnAgentRemoved(agent);
			}
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x00005250 File Offset: 0x00003450
		public void SetTarget(UsableMachine usableMachine, bool isInitialTarget = false, Agent.AIScriptedFrameFlags customFlags = Agent.AIScriptedFrameFlags.None)
		{
			if (usableMachine == null)
			{
				UsableMachine targetUsableMachine = this.TargetUsableMachine;
				if (targetUsableMachine != null)
				{
					((IDetachment)targetUsableMachine).RemoveAgent(this.OwnerAgent);
				}
				this.TargetUsableMachine = null;
				this.OwnerAgent.DisableScriptedMovement();
				this.OwnerAgent.ClearTargetFrame();
				this.TargetPosition = WorldPosition.Invalid;
				this.TargetEntity = null;
				this._agentState = AgentNavigator.NavigationState.NoTarget;
				return;
			}
			if (this.TargetUsableMachine != usableMachine || isInitialTarget)
			{
				this.TargetPosition = WorldPosition.Invalid;
				this._agentState = AgentNavigator.NavigationState.NoTarget;
				UsableMachine targetUsableMachine2 = this.TargetUsableMachine;
				if (targetUsableMachine2 != null)
				{
					((IDetachment)targetUsableMachine2).RemoveAgent(this.OwnerAgent);
				}
				if (usableMachine.IsStandingPointAvailableForAgent(this.OwnerAgent))
				{
					this.TargetUsableMachine = usableMachine;
					this.TargetPosition = WorldPosition.Invalid;
					this._agentState = AgentNavigator.NavigationState.UseMachine;
					this._targetBehavior = this.TargetUsableMachine.CreateAIBehaviorObject();
					((IDetachment)this.TargetUsableMachine).AddAgent(this.OwnerAgent, -1, customFlags);
					this._targetReached = false;
				}
			}
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x0000533C File Offset: 0x0000353C
		public void SetTargetFrame(WorldPosition position, float rotation, float rangeThreshold = 1f, float rotationThreshold = -10f, Agent.AIScriptedFrameFlags flags = Agent.AIScriptedFrameFlags.None, bool disableClearTargetWhenTargetIsReached = false)
		{
			if (this._agentState != AgentNavigator.NavigationState.NoTarget)
			{
				this.ClearTarget();
			}
			this.TargetPosition = position;
			this.TargetDirection = Vec2.FromRotation(rotation);
			this._rangeThreshold = rangeThreshold;
			this._rotationScoreThreshold = rotationThreshold;
			this._disableClearTargetWhenTargetIsReached = disableClearTargetWhenTargetIsReached;
			if (this.IsTargetReached())
			{
				this.TargetPosition = WorldPosition.Invalid;
				this._agentState = AgentNavigator.NavigationState.NoTarget;
				return;
			}
			this.OwnerAgent.SetScriptedPositionAndDirection(ref position, rotation, false, flags);
			this._agentState = AgentNavigator.NavigationState.GoToTarget;
		}

		// Token: 0x060000A7 RID: 167 RVA: 0x000053B4 File Offset: 0x000035B4
		public void ClearTarget()
		{
			this.SetTarget(null, false, Agent.AIScriptedFrameFlags.None);
		}

		// Token: 0x060000A8 RID: 168 RVA: 0x000053C0 File Offset: 0x000035C0
		public void Tick(float dt, bool isSimulation = false)
		{
			this.HandleBehaviorGroups(isSimulation);
			if (ConversationMission.ConversationAgents.Contains(this.OwnerAgent))
			{
				using (List<AgentBehaviorGroup>.Enumerator enumerator = this._behaviorGroups.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						AgentBehaviorGroup agentBehaviorGroup = enumerator.Current;
						if (agentBehaviorGroup.IsActive)
						{
							agentBehaviorGroup.ConversationTick();
						}
					}
					goto IL_5E;
				}
			}
			this.TickBehaviorGroups(dt, isSimulation);
			IL_5E:
			if (this.TargetUsableMachine != null)
			{
				this._targetBehavior.Tick(this.OwnerAgent, null, null, dt);
			}
			else
			{
				this.HandleMovement();
			}
			if (this.TargetUsableMachine != null && isSimulation)
			{
				this._targetBehavior.TeleportUserAgentsToMachine(new List<Agent> { this.OwnerAgent });
			}
		}

		// Token: 0x060000A9 RID: 169 RVA: 0x00005488 File Offset: 0x00003688
		public float GetDistanceToTarget(UsableMachine target)
		{
			float result = 100000f;
			if (target != null && this.OwnerAgent.CurrentlyUsedGameObject != null)
			{
				result = this.OwnerAgent.CurrentlyUsedGameObject.GetUserFrameForAgent(this.OwnerAgent).Origin.GetGroundVec3().Distance(this.OwnerAgent.Position);
			}
			return result;
		}

		// Token: 0x060000AA RID: 170 RVA: 0x000054E4 File Offset: 0x000036E4
		public bool IsTargetReached()
		{
			if (this.TargetDirection.IsValid && this.TargetPosition.IsValid)
			{
				float num = Vec2.DotProduct(this.TargetDirection, this.OwnerAgent.GetMovementDirection());
				this._targetReached = (this.OwnerAgent.Position - this.TargetPosition.GetGroundVec3()).LengthSquared < this._rangeThreshold * this._rangeThreshold && num > this._rotationScoreThreshold;
			}
			return this._targetReached;
		}

		// Token: 0x060000AB RID: 171 RVA: 0x00005575 File Offset: 0x00003775
		private void HandleMovement()
		{
			if (this._agentState == AgentNavigator.NavigationState.GoToTarget && this.IsTargetReached())
			{
				this._agentState = AgentNavigator.NavigationState.AtTargetPosition;
				if (!this._disableClearTargetWhenTargetIsReached)
				{
					this.OwnerAgent.ClearTargetFrame();
				}
			}
		}

		// Token: 0x060000AC RID: 172 RVA: 0x000055A4 File Offset: 0x000037A4
		public void HoldAndHideRecentlyUsedMeshes()
		{
			foreach (KeyValuePair<KeyValuePair<sbyte, string>, int> keyValuePair in this._bodyComponents)
			{
				if (this.OwnerAgent.IsSynchedPrefabComponentVisible(keyValuePair.Value))
				{
					this.OwnerAgent.SetSynchedPrefabComponentVisibility(keyValuePair.Value, false);
					this._prevPrefabs.Add(keyValuePair.Value);
				}
			}
		}

		// Token: 0x060000AD RID: 173 RVA: 0x0000562C File Offset: 0x0000382C
		public void RecoverRecentlyUsedMeshes()
		{
			foreach (int componentIndex in this._prevPrefabs)
			{
				this.OwnerAgent.SetSynchedPrefabComponentVisibility(componentIndex, true);
			}
			this._prevPrefabs.Clear();
		}

		// Token: 0x060000AE RID: 174 RVA: 0x00005690 File Offset: 0x00003890
		public bool CanSeeAgent(Agent otherAgent)
		{
			if ((this.OwnerAgent.Position - otherAgent.Position).Length < 30f)
			{
				Vec3 eyeGlobalPosition = otherAgent.GetEyeGlobalPosition();
				Vec3 eyeGlobalPosition2 = this.OwnerAgent.GetEyeGlobalPosition();
				if (MathF.Abs(Vec3.AngleBetweenTwoVectors(otherAgent.Position - this.OwnerAgent.Position, this.OwnerAgent.LookDirection)) < 1.5f)
				{
					float num;
					return !Mission.Current.Scene.RayCastForClosestEntityOrTerrain(eyeGlobalPosition2, eyeGlobalPosition, out num, 0.01f, BodyFlags.CommonFocusRayCastExcludeFlags);
				}
			}
			return false;
		}

		// Token: 0x060000AF RID: 175 RVA: 0x00005729 File Offset: 0x00003929
		public bool IsCarryingSomething()
		{
			return this.OwnerAgent.GetPrimaryWieldedItemIndex() >= EquipmentIndex.WeaponItemBeginSlot || this.OwnerAgent.GetOffhandWieldedItemIndex() >= EquipmentIndex.WeaponItemBeginSlot || this._bodyComponents.Any((KeyValuePair<KeyValuePair<sbyte, string>, int> component) => this.OwnerAgent.IsSynchedPrefabComponentVisible(component.Value));
		}

		// Token: 0x060000B0 RID: 176 RVA: 0x00005760 File Offset: 0x00003960
		public void SetPrefabVisibility(sbyte realBoneIndex, string prefabName, bool isVisible)
		{
			KeyValuePair<sbyte, string> key = new KeyValuePair<sbyte, string>(realBoneIndex, prefabName);
			int componentIndex2;
			if (isVisible)
			{
				int componentIndex;
				if (!this._bodyComponents.TryGetValue(key, out componentIndex))
				{
					this._bodyComponents.Add(key, this.OwnerAgent.AddSynchedPrefabComponentToBone(prefabName, realBoneIndex));
					return;
				}
				if (!this.OwnerAgent.IsSynchedPrefabComponentVisible(componentIndex))
				{
					this.OwnerAgent.SetSynchedPrefabComponentVisibility(componentIndex, true);
					return;
				}
			}
			else if (this._bodyComponents.TryGetValue(key, out componentIndex2) && this.OwnerAgent.IsSynchedPrefabComponentVisible(componentIndex2))
			{
				this.OwnerAgent.SetSynchedPrefabComponentVisibility(componentIndex2, false);
			}
		}

		// Token: 0x060000B1 RID: 177 RVA: 0x000057EC File Offset: 0x000039EC
		public bool GetPrefabVisibility(sbyte realBoneIndex, string prefabName)
		{
			KeyValuePair<sbyte, string> key = new KeyValuePair<sbyte, string>(realBoneIndex, prefabName);
			int componentIndex;
			return this._bodyComponents.TryGetValue(key, out componentIndex) && this.OwnerAgent.IsSynchedPrefabComponentVisible(componentIndex);
		}

		// Token: 0x060000B2 RID: 178 RVA: 0x00005824 File Offset: 0x00003A24
		public void SetSpecialItem()
		{
			if (this._specialItem != null)
			{
				bool flag = false;
				EquipmentIndex equipmentIndex = EquipmentIndex.None;
				for (EquipmentIndex equipmentIndex2 = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex2 <= EquipmentIndex.Weapon3; equipmentIndex2++)
				{
					if (this.OwnerAgent.Equipment[equipmentIndex2].IsEmpty)
					{
						equipmentIndex = equipmentIndex2;
					}
					else if (this.OwnerAgent.Equipment[equipmentIndex2].Item == this._specialItem)
					{
						equipmentIndex = equipmentIndex2;
						flag = true;
						break;
					}
				}
				if (equipmentIndex == EquipmentIndex.None)
				{
					this.OwnerAgent.DropItem(EquipmentIndex.Weapon3, WeaponClass.Undefined);
					equipmentIndex = EquipmentIndex.Weapon3;
				}
				if (!flag)
				{
					ItemObject specialItem = this._specialItem;
					ItemModifier itemModifier = null;
					IAgentOriginBase origin = this.OwnerAgent.Origin;
					MissionWeapon missionWeapon = new MissionWeapon(specialItem, itemModifier, (origin != null) ? origin.Banner : null);
					this.OwnerAgent.EquipWeaponWithNewEntity(equipmentIndex, ref missionWeapon);
				}
				this.OwnerAgent.TryToWieldWeaponInSlot(equipmentIndex, Agent.WeaponWieldActionType.Instant, false);
			}
		}

		// Token: 0x060000B3 RID: 179 RVA: 0x000058EC File Offset: 0x00003AEC
		public void SetItemsVisibility(bool isVisible)
		{
			foreach (KeyValuePair<sbyte, string> keyValuePair in this._prefabNamesForBones)
			{
				this.SetPrefabVisibility(keyValuePair.Key, keyValuePair.Value, isVisible);
			}
			this.CharacterHasVisiblePrefabs = this._prefabNamesForBones.Count > 0 && isVisible;
		}

		// Token: 0x060000B4 RID: 180 RVA: 0x00005964 File Offset: 0x00003B64
		public void SetCommonArea(Alley alley)
		{
			if (alley != this.MemberOfAlley)
			{
				this.MemberOfAlley = alley;
				this.SpecialTargetTag = ((alley == null) ? "" : alley.Tag);
			}
		}

		// Token: 0x060000B5 RID: 181 RVA: 0x0000598C File Offset: 0x00003B8C
		public void ForceThink(float inSeconds)
		{
			foreach (AgentBehaviorGroup agentBehaviorGroup in this._behaviorGroups)
			{
				agentBehaviorGroup.ForceThink(inSeconds);
			}
		}

		// Token: 0x060000B6 RID: 182 RVA: 0x000059E0 File Offset: 0x00003BE0
		public T AddBehaviorGroup<T>() where T : AgentBehaviorGroup
		{
			T t = this.GetBehaviorGroup<T>();
			if (t == null)
			{
				t = Activator.CreateInstance(typeof(T), new object[] { this, this._mission }) as T;
				if (t != null)
				{
					this._behaviorGroups.Add(t);
				}
			}
			return t;
		}

		// Token: 0x060000B7 RID: 183 RVA: 0x00005A44 File Offset: 0x00003C44
		public T GetBehaviorGroup<T>() where T : AgentBehaviorGroup
		{
			foreach (AgentBehaviorGroup agentBehaviorGroup in this._behaviorGroups)
			{
				if (agentBehaviorGroup is T)
				{
					return (T)((object)agentBehaviorGroup);
				}
			}
			return default(T);
		}

		// Token: 0x060000B8 RID: 184 RVA: 0x00005AAC File Offset: 0x00003CAC
		public AgentBehavior GetBehavior<T>() where T : AgentBehavior
		{
			foreach (AgentBehaviorGroup agentBehaviorGroup in this._behaviorGroups)
			{
				foreach (AgentBehavior agentBehavior in agentBehaviorGroup.Behaviors)
				{
					if (agentBehavior.GetType() == typeof(T))
					{
						return agentBehavior;
					}
				}
			}
			return null;
		}

		// Token: 0x060000B9 RID: 185 RVA: 0x00005B50 File Offset: 0x00003D50
		public bool HasBehaviorGroup<T>()
		{
			using (List<AgentBehaviorGroup>.Enumerator enumerator = this._behaviorGroups.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.GetType() is T)
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x060000BA RID: 186 RVA: 0x00005BB0 File Offset: 0x00003DB0
		public void RemoveBehaviorGroup<T>() where T : AgentBehaviorGroup
		{
			for (int i = 0; i < this._behaviorGroups.Count; i++)
			{
				if (this._behaviorGroups[i] is T)
				{
					this._behaviorGroups.RemoveAt(i);
				}
			}
		}

		// Token: 0x060000BB RID: 187 RVA: 0x00005BF4 File Offset: 0x00003DF4
		public void RefreshBehaviorGroups(bool isSimulation)
		{
			this._checkBehaviorGroupsTimer.Reset();
			float num = 0f;
			AgentBehaviorGroup agentBehaviorGroup = null;
			foreach (AgentBehaviorGroup agentBehaviorGroup2 in this._behaviorGroups)
			{
				float score = agentBehaviorGroup2.GetScore(isSimulation);
				if (score > num)
				{
					num = score;
					agentBehaviorGroup = agentBehaviorGroup2;
				}
			}
			if (num > 0f && agentBehaviorGroup != null && !agentBehaviorGroup.IsActive)
			{
				this.ActivateGroup(agentBehaviorGroup);
			}
		}

		// Token: 0x060000BC RID: 188 RVA: 0x00005C80 File Offset: 0x00003E80
		private void ActivateGroup(AgentBehaviorGroup behaviorGroup)
		{
			foreach (AgentBehaviorGroup agentBehaviorGroup in this._behaviorGroups)
			{
				agentBehaviorGroup.IsActive = false;
			}
			behaviorGroup.IsActive = true;
		}

		// Token: 0x060000BD RID: 189 RVA: 0x00005CD8 File Offset: 0x00003ED8
		private void HandleBehaviorGroups(bool isSimulation)
		{
			if (isSimulation || this._checkBehaviorGroupsTimer.ElapsedTime > 1f)
			{
				this.RefreshBehaviorGroups(isSimulation);
			}
		}

		// Token: 0x060000BE RID: 190 RVA: 0x00005CF8 File Offset: 0x00003EF8
		private void TickBehaviorGroups(float dt, bool isSimulation)
		{
			if (!this.OwnerAgent.IsActive())
			{
				return;
			}
			foreach (AgentBehaviorGroup agentBehaviorGroup in this._behaviorGroups)
			{
				agentBehaviorGroup.Tick(dt, isSimulation);
			}
		}

		// Token: 0x060000BF RID: 191 RVA: 0x00005D58 File Offset: 0x00003F58
		public AgentBehavior GetActiveBehavior()
		{
			foreach (AgentBehaviorGroup agentBehaviorGroup in this._behaviorGroups)
			{
				if (agentBehaviorGroup.IsActive)
				{
					return agentBehaviorGroup.GetActiveBehavior();
				}
			}
			return null;
		}

		// Token: 0x060000C0 RID: 192 RVA: 0x00005DB8 File Offset: 0x00003FB8
		public AgentBehaviorGroup GetActiveBehaviorGroup()
		{
			foreach (AgentBehaviorGroup agentBehaviorGroup in this._behaviorGroups)
			{
				if (agentBehaviorGroup.IsActive)
				{
					return agentBehaviorGroup;
				}
			}
			return null;
		}

		// Token: 0x0400003A RID: 58
		private const float SeeingDistance = 30f;

		// Token: 0x0400003B RID: 59
		public readonly Agent OwnerAgent;

		// Token: 0x04000041 RID: 65
		private readonly Mission _mission;

		// Token: 0x04000042 RID: 66
		private readonly List<AgentBehaviorGroup> _behaviorGroups;

		// Token: 0x04000043 RID: 67
		private readonly ItemObject _specialItem;

		// Token: 0x04000044 RID: 68
		private UsableMachineAIBase _targetBehavior;

		// Token: 0x04000045 RID: 69
		private bool _targetReached;

		// Token: 0x04000046 RID: 70
		private float _rangeThreshold;

		// Token: 0x04000047 RID: 71
		private float _rotationScoreThreshold;

		// Token: 0x04000048 RID: 72
		private string _specialTargetTag;

		// Token: 0x04000049 RID: 73
		private bool _disableClearTargetWhenTargetIsReached;

		// Token: 0x0400004B RID: 75
		private readonly Dictionary<sbyte, string> _prefabNamesForBones;

		// Token: 0x0400004C RID: 76
		private readonly List<int> _prevPrefabs;

		// Token: 0x0400004F RID: 79
		private readonly MissionConversationLogic _conversationHandler;

		// Token: 0x04000050 RID: 80
		private readonly BasicMissionTimer _checkBehaviorGroupsTimer;

		// Token: 0x02000112 RID: 274
		public enum NavigationState
		{
			// Token: 0x040005C5 RID: 1477
			NoTarget,
			// Token: 0x040005C6 RID: 1478
			GoToTarget,
			// Token: 0x040005C7 RID: 1479
			AtTargetPosition,
			// Token: 0x040005C8 RID: 1480
			UseMachine
		}
	}
}
