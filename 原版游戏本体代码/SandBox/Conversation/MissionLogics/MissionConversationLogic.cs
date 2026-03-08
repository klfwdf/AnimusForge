using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;

namespace SandBox.Conversation.MissionLogics
{
	// Token: 0x020000C7 RID: 199
	public class MissionConversationLogic : MissionLogic
	{
		// Token: 0x170000A3 RID: 163
		// (get) Token: 0x0600082E RID: 2094 RVA: 0x0003B8AA File Offset: 0x00039AAA
		public static MissionConversationLogic Current
		{
			get
			{
				return Mission.Current.GetMissionBehavior<MissionConversationLogic>();
			}
		}

		// Token: 0x170000A4 RID: 164
		// (get) Token: 0x0600082F RID: 2095 RVA: 0x0003B8B6 File Offset: 0x00039AB6
		// (set) Token: 0x06000830 RID: 2096 RVA: 0x0003B8BE File Offset: 0x00039ABE
		public MissionState State { get; private set; }

		// Token: 0x170000A5 RID: 165
		// (get) Token: 0x06000831 RID: 2097 RVA: 0x0003B8C7 File Offset: 0x00039AC7
		// (set) Token: 0x06000832 RID: 2098 RVA: 0x0003B8CF File Offset: 0x00039ACF
		public ConversationManager ConversationManager { get; private set; }

		// Token: 0x170000A6 RID: 166
		// (get) Token: 0x06000833 RID: 2099 RVA: 0x0003B8D8 File Offset: 0x00039AD8
		public bool IsReadyForConversation
		{
			get
			{
				return this.ConversationAgent != null && this.ConversationManager != null && Agent.Main != null && Agent.Main.IsActive();
			}
		}

		// Token: 0x170000A7 RID: 167
		// (get) Token: 0x06000834 RID: 2100 RVA: 0x0003B8FF File Offset: 0x00039AFF
		// (set) Token: 0x06000835 RID: 2101 RVA: 0x0003B907 File Offset: 0x00039B07
		public Agent ConversationAgent { get; private set; }

		// Token: 0x06000836 RID: 2102 RVA: 0x0003B910 File Offset: 0x00039B10
		public MissionConversationLogic(CharacterObject teleportNearChar)
		{
			this._teleportNearCharacter = teleportNearChar;
			this._conversationPoints = new Dictionary<string, MBList<GameEntity>>();
		}

		// Token: 0x06000837 RID: 2103 RVA: 0x0003B935 File Offset: 0x00039B35
		public MissionConversationLogic()
		{
		}

		// Token: 0x06000838 RID: 2104 RVA: 0x0003B948 File Offset: 0x00039B48
		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			CampaignEvents.LocationCharactersSimulatedEvent.AddNonSerializedListener(this, new Action(this.OnLocationCharactersSimulated));
		}

		// Token: 0x06000839 RID: 2105 RVA: 0x0003B967 File Offset: 0x00039B67
		public override void OnRemoveBehavior()
		{
			base.OnRemoveBehavior();
			CampaignEvents.LocationCharactersAreReadyToSpawnEvent.ClearListeners(this);
		}

		// Token: 0x0600083A RID: 2106 RVA: 0x0003B97C File Offset: 0x00039B7C
		public override void OnAgentBuild(Agent agent, Banner banner)
		{
			if (this._teleportNearCharacter != null && agent.Character == this._teleportNearCharacter)
			{
				this.ConversationAgent = agent;
				this._conversationAgentFound = true;
			}
			if (agent.IsHuman)
			{
				CharacterObject characterObject = agent.Character as CharacterObject;
				if (characterObject != null && characterObject.Culture.FemaleDancer == characterObject)
				{
					this._uninteractableAgents.Add(agent);
				}
			}
		}

		// Token: 0x0600083B RID: 2107 RVA: 0x0003B9DF File Offset: 0x00039BDF
		public void SetSpawnArea(Alley alley)
		{
			this._customSpawnTag = alley.Tag;
		}

		// Token: 0x0600083C RID: 2108 RVA: 0x0003B9ED File Offset: 0x00039BED
		public void SetSpawnArea(Workshop workshop)
		{
			this._customSpawnTag = workshop.Tag;
		}

		// Token: 0x0600083D RID: 2109 RVA: 0x0003B9FB File Offset: 0x00039BFB
		public void SetSpawnArea(string customTag)
		{
			this._customSpawnTag = customTag;
		}

		// Token: 0x0600083E RID: 2110 RVA: 0x0003BA04 File Offset: 0x00039C04
		private void OnLocationCharactersSimulated()
		{
			if (this._conversationAgentFound)
			{
				if (this.FillConversationPointList())
				{
					this.DetermineSpawnPoint();
					this._teleported = this.TryToTeleportBothToCertainPoints();
					return;
				}
				MissionAgentHandler missionBehavior = base.Mission.GetMissionBehavior<MissionAgentHandler>();
				if (missionBehavior == null)
				{
					return;
				}
				missionBehavior.TeleportTargetAgentNearReferenceAgent(this.ConversationAgent, Agent.Main, true, false);
			}
		}

		// Token: 0x0600083F RID: 2111 RVA: 0x0003BA58 File Offset: 0x00039C58
		public override void OnMissionTick(float dt)
		{
			base.OnMissionTick(dt);
			if (!this.IsReadyForConversation)
			{
				return;
			}
			if (!this._teleported)
			{
				base.Mission.GetMissionBehavior<MissionAgentHandler>().TeleportTargetAgentNearReferenceAgent(this.ConversationAgent, Agent.Main, true, false);
				this._teleported = true;
			}
			if (this._teleportNearCharacter != null && !this._conversationStarted)
			{
				this.StartConversation(this.ConversationAgent, true, true);
				if (this.ConversationManager.NeedsToActivateForMapConversation && !GameNetwork.IsReplay)
				{
					this.ConversationManager.BeginConversation();
				}
			}
		}

		// Token: 0x06000840 RID: 2112 RVA: 0x0003BAE0 File Offset: 0x00039CE0
		private bool TryToTeleportBothToCertainPoints()
		{
			bool missionBehavior = base.Mission.GetMissionBehavior<MissionAgentHandler>() != null;
			bool flag = Agent.Main.MountAgent != null;
			WorldFrame worldFrame = new WorldFrame(this._selectedConversationPoint.GetGlobalFrame().rotation, new WorldPosition(Agent.Main.Mission.Scene, this._selectedConversationPoint.GetGlobalFrame().origin));
			worldFrame.Origin.SetVec2(worldFrame.Origin.AsVec2 + worldFrame.Rotation.f.AsVec2 * (flag ? 1f : 0.5f));
			WorldFrame worldFrame2 = new WorldFrame(this._selectedConversationPoint.GetGlobalFrame().rotation, new WorldPosition(Agent.Main.Mission.Scene, this._selectedConversationPoint.GetGlobalFrame().origin));
			worldFrame2.Origin.SetVec2(worldFrame2.Origin.AsVec2 - worldFrame2.Rotation.f.AsVec2 * (flag ? 1f : 0.5f));
			Vec3 vec = new Vec3(worldFrame.Origin.AsVec2 - worldFrame2.Origin.AsVec2, 0f, -1f);
			Vec3 vec2 = new Vec3(worldFrame2.Origin.AsVec2 - worldFrame.Origin.AsVec2, 0f, -1f);
			worldFrame.Rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			this.ConversationAgent.LookDirection = vec2.NormalizedCopy();
			this.ConversationAgent.TeleportToPosition(worldFrame.Origin.GetGroundVec3());
			worldFrame2.Rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			if (Agent.Main.MountAgent != null)
			{
				Vec2 vec3 = vec2.AsVec2;
				vec3 = vec3.RightVec();
				Vec3 vec4 = vec3.ToVec3(0f);
				Agent.Main.MountAgent.LookDirection = vec4.NormalizedCopy();
			}
			base.Mission.MainAgent.LookDirection = vec.NormalizedCopy();
			base.Mission.MainAgent.TeleportToPosition(worldFrame2.Origin.GetGroundVec3());
			this.SetConversationAgentAnimations(this.ConversationAgent);
			WorldPosition origin = worldFrame2.Origin;
			origin.SetVec2(origin.AsVec2 - worldFrame2.Rotation.s.AsVec2 * 2f);
			if (missionBehavior)
			{
				foreach (Agent agent in base.Mission.Agents)
				{
					LocationCharacter locationCharacter = LocationComplex.Current.FindCharacter(agent);
					AccompanyingCharacter accompanyingCharacter = PlayerEncounter.LocationEncounter.GetAccompanyingCharacter(locationCharacter);
					if (accompanyingCharacter != null && accompanyingCharacter.IsFollowingPlayerAtMissionStart)
					{
						if (agent.MountAgent != null && Agent.Main.MountAgent != null)
						{
							agent.MountAgent.LookDirection = Agent.Main.MountAgent.LookDirection;
						}
						if (accompanyingCharacter.LocationCharacter.Character == this._teleportNearCharacter)
						{
							agent.LookDirection = vec2.NormalizedCopy();
							Agent agent2 = agent;
							Vec2 vec3 = worldFrame2.Rotation.f.AsVec2;
							agent2.SetMovementDirection(vec3);
							agent.TeleportToPosition(worldFrame.Origin.GetGroundVec3());
						}
						else
						{
							agent.LookDirection = vec.NormalizedCopy();
							Agent agent3 = agent;
							Vec2 vec3 = worldFrame.Rotation.f.AsVec2;
							agent3.SetMovementDirection(vec3);
							agent.TeleportToPosition(origin.GetGroundVec3());
						}
					}
				}
			}
			this._teleported = true;
			return true;
		}

		// Token: 0x06000841 RID: 2113 RVA: 0x0003BEA4 File Offset: 0x0003A0A4
		private void SetConversationAgentAnimations(Agent conversationAgent)
		{
			CampaignAgentComponent component = conversationAgent.GetComponent<CampaignAgentComponent>();
			AgentNavigator agentNavigator = component.AgentNavigator;
			AgentBehavior agentBehavior = ((agentNavigator != null) ? agentNavigator.GetActiveBehavior() : null);
			if (agentBehavior != null)
			{
				agentBehavior.IsActive = false;
				component.AgentNavigator.ForceThink(0f);
				conversationAgent.SetActionChannel(0, ActionIndexCache.act_none, false, (AnimFlags)((long)conversationAgent.GetCurrentActionPriority(0)), 0f, 1f, 0f, 0.4f, 0f, false, -0.2f, 0, true);
				conversationAgent.SetActionChannel(0, ActionIndexCache.act_none, false, (AnimFlags)((long)Math.Min(conversationAgent.GetCurrentActionPriority(0), 73)), 0f, 1f, 0f, 0.4f, 0f, false, -0.2f, 0, true);
				conversationAgent.TickActionChannels(0.1f);
				conversationAgent.AgentVisuals.GetSkeleton().TickAnimationsAndForceUpdate(0.1f, conversationAgent.AgentVisuals.GetGlobalFrame(), true);
			}
		}

		// Token: 0x06000842 RID: 2114 RVA: 0x0003BF8C File Offset: 0x0003A18C
		private void OnConversationEnd()
		{
			foreach (IAgent agent in this.ConversationManager.ConversationAgents)
			{
				Agent agent2 = (Agent)agent;
				agent2.AgentVisuals.SetVisible(true);
				agent2.AgentVisuals.SetClothComponentKeepStateOfAllMeshes(false);
				Agent mountAgent = agent2.MountAgent;
				if (mountAgent != null)
				{
					mountAgent.AgentVisuals.SetVisible(true);
				}
			}
			if (base.Mission.Mode == MissionMode.Conversation && !base.Mission.IsMissionEnding)
			{
				base.Mission.SetMissionMode(this._oldMissionMode, false);
			}
			if (Agent.Main != null)
			{
				Agent.Main.AgentVisuals.SetVisible(true);
				Agent.Main.AgentVisuals.SetClothComponentKeepStateOfAllMeshes(false);
				if (Agent.Main.MountAgent != null)
				{
					Agent.Main.MountAgent.AgentVisuals.SetVisible(true);
				}
			}
			base.Mission.MainAgentServer.Controller = AgentControllerType.Player;
			this.ConversationManager.ConversationEnd -= this.OnConversationEnd;
		}

		// Token: 0x06000843 RID: 2115 RVA: 0x0003C0A8 File Offset: 0x0003A2A8
		public override void EarlyStart()
		{
			this.State = Game.Current.GameStateManager.ActiveState as MissionState;
		}

		// Token: 0x06000844 RID: 2116 RVA: 0x0003C0C4 File Offset: 0x0003A2C4
		protected override void OnEndMission()
		{
			if (this.ConversationManager != null && this.ConversationManager.IsConversationInProgress)
			{
				this.ConversationManager.EndConversation();
			}
			this.State = null;
			CampaignEvents.LocationCharactersSimulatedEvent.ClearListeners(this);
		}

		// Token: 0x06000845 RID: 2117 RVA: 0x0003C0F8 File Offset: 0x0003A2F8
		public override void OnAgentInteraction(Agent userAgent, Agent agent, sbyte agentBoneIndex)
		{
			if (Campaign.Current.GameMode == CampaignGameMode.Campaign && this.IsThereAgentAction(userAgent, agent) && !this.ConversationManager.IsConversationInProgress)
			{
				this.StartConversation(agent, false, false);
			}
		}

		// Token: 0x06000846 RID: 2118 RVA: 0x0003C128 File Offset: 0x0003A328
		public void StartConversation(Agent agent, bool setActionsInstantly, bool isInitialization = false)
		{
			this._oldMissionMode = base.Mission.Mode;
			this.ConversationManager = Campaign.Current.ConversationManager;
			this.ConversationManager.SetupAndStartMissionConversation(agent, base.Mission.MainAgent, setActionsInstantly);
			this.ConversationManager.ConversationEnd += this.OnConversationEnd;
			this._conversationStarted = true;
			foreach (IAgent agent2 in this.ConversationManager.ConversationAgents)
			{
				Agent agent3 = (Agent)agent2;
				agent3.ForceAiBehaviorSelection();
				agent3.AgentVisuals.SetClothComponentKeepStateOfAllMeshes(true);
			}
			base.Mission.MainAgentServer.AgentVisuals.SetClothComponentKeepStateOfAllMeshes(true);
			base.Mission.SetMissionMode(MissionMode.Conversation, setActionsInstantly);
		}

		// Token: 0x06000847 RID: 2119 RVA: 0x0003C204 File Offset: 0x0003A404
		public override bool IsThereAgentAction(Agent userAgent, Agent otherAgent)
		{
			return base.Mission.Mode != MissionMode.Battle && base.Mission.Mode != MissionMode.Duel && base.Mission.Mode != MissionMode.Conversation && otherAgent.IsHuman && !this._disableStartConversation && otherAgent.IsActive() && !otherAgent.IsEnemyOf(userAgent) && otherAgent.GetDistanceTo(Agent.Main) > 0.2f && otherAgent.GetDistanceTo(Agent.Main) < 2f && !this._uninteractableAgents.Contains(otherAgent) && (!otherAgent.GetCurrentAnimationFlag(0).HasAnyFlag(AnimFlags.anf_enforce_lowerbody | AnimFlags.anf_enforce_all | AnimFlags.anf_enforce_root_rotation) || Math.Abs(userAgent.LookDirection.AsVec2.AngleBetween(otherAgent.LookDirection.AsVec2) * 57.295776f) > 45f);
		}

		// Token: 0x06000848 RID: 2120 RVA: 0x0003C2F4 File Offset: 0x0003A4F4
		public override void OnRenderingStarted()
		{
			this.ConversationManager = Campaign.Current.ConversationManager;
			if (this.ConversationManager == null)
			{
				throw new ArgumentNullException("conversationManager");
			}
		}

		// Token: 0x06000849 RID: 2121 RVA: 0x0003C319 File Offset: 0x0003A519
		public void DisableStartConversation(bool isDisabled)
		{
			this._disableStartConversation = isDisabled;
		}

		// Token: 0x0600084A RID: 2122 RVA: 0x0003C324 File Offset: 0x0003A524
		private bool FillConversationPointList()
		{
			List<GameEntity> list = base.Mission.Scene.FindEntitiesWithTag("sp_player_conversation").ToList<GameEntity>();
			bool result = false;
			if (!list.IsEmpty<GameEntity>())
			{
				List<AreaMarker> list2 = base.Mission.ActiveMissionObjects.FindAllWithType<AreaMarker>().ToList<AreaMarker>();
				foreach (GameEntity gameEntity in list)
				{
					bool flag = false;
					foreach (AreaMarker areaMarker in list2)
					{
						if (areaMarker.IsPositionInRange(gameEntity.GlobalPosition))
						{
							if (this._conversationPoints.ContainsKey(areaMarker.Tag))
							{
								this._conversationPoints[areaMarker.Tag].Add(gameEntity);
							}
							else
							{
								this._conversationPoints.Add(areaMarker.Tag, new MBList<GameEntity> { gameEntity });
							}
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						if (this._conversationPoints.ContainsKey("CenterConversationPoint"))
						{
							this._conversationPoints["CenterConversationPoint"].Add(gameEntity);
						}
						else
						{
							this._conversationPoints.Add("CenterConversationPoint", new MBList<GameEntity> { gameEntity });
						}
					}
				}
				result = true;
			}
			else
			{
				Debug.FailedAssert("Scene must have at least one 'sp_player_conversation' game entity. Scene Name: " + Mission.Current.Scene.GetName(), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Conversation\\Logics\\MissionConversationLogic.cs", "FillConversationPointList", 404);
			}
			return result;
		}

		// Token: 0x0600084B RID: 2123 RVA: 0x0003C4CC File Offset: 0x0003A6CC
		private void DetermineSpawnPoint()
		{
			MBList<GameEntity> e;
			if (this._customSpawnTag != null && this._conversationPoints.TryGetValue(this._customSpawnTag, out e))
			{
				this._selectedConversationPoint = e.GetRandomElement<GameEntity>();
				return;
			}
			string agentsTag = this.ConversationAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.SpecialTargetTag;
			if (agentsTag != null)
			{
				MBList<GameEntity> value = this._conversationPoints.FirstOrDefault((KeyValuePair<string, MBList<GameEntity>> x) => agentsTag.Contains(x.Key)).Value;
				this._selectedConversationPoint = ((value != null) ? value.GetRandomElement<GameEntity>() : null);
			}
			if (this._selectedConversationPoint == null)
			{
				if (this._conversationPoints.ContainsKey("CenterConversationPoint"))
				{
					this._selectedConversationPoint = this._conversationPoints["CenterConversationPoint"].GetRandomElement<GameEntity>();
					return;
				}
				this._selectedConversationPoint = this._conversationPoints.GetRandomElementInefficiently<KeyValuePair<string, MBList<GameEntity>>>().Value.GetRandomElement<GameEntity>();
			}
		}

		// Token: 0x0400042C RID: 1068
		private const string CenterConversationPointMappingTag = "CenterConversationPoint";

		// Token: 0x0400042D RID: 1069
		private const int StartingConversationFromBehindAngleThresholdInDegrees = 45;

		// Token: 0x0400042E RID: 1070
		private const float MinDistanceThresholdToOpenConversation = 0.2f;

		// Token: 0x0400042F RID: 1071
		private const float MaxDistanceThresholdToOpenConversation = 2f;

		// Token: 0x04000432 RID: 1074
		private MissionMode _oldMissionMode;

		// Token: 0x04000433 RID: 1075
		private readonly CharacterObject _teleportNearCharacter;

		// Token: 0x04000434 RID: 1076
		private GameEntity _selectedConversationPoint;

		// Token: 0x04000435 RID: 1077
		private bool _conversationStarted;

		// Token: 0x04000436 RID: 1078
		private bool _teleported;

		// Token: 0x04000437 RID: 1079
		private bool _conversationAgentFound;

		// Token: 0x04000438 RID: 1080
		private bool _disableStartConversation;

		// Token: 0x04000439 RID: 1081
		private readonly Dictionary<string, MBList<GameEntity>> _conversationPoints;

		// Token: 0x0400043A RID: 1082
		private string _customSpawnTag;

		// Token: 0x0400043B RID: 1083
		private HashSet<Agent> _uninteractableAgents = new HashSet<Agent>();
	}
}
