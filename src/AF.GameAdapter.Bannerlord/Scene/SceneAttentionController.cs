using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.SceneActions.Core;
using AnimusForge.SiegeAftermathIntervention;
using AnimusForge.XihaiAction;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;
using RichExecutions.Core;
using RichExecutions.Scene;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Towns;
using SandBox.Objects.AnimationPoints;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;

using static AnimusForge.SceneMovementController;

using static AnimusForge.ShoutBehavior;
namespace AnimusForge;

internal sealed class SceneAttentionController
{
    private readonly SceneAttentionControllerPorts _ports;
    internal SceneAttentionController(SceneAttentionControllerPorts ports) { _ports = ports ?? throw new ArgumentNullException(nameof(ports)); }

	internal List<Agent> _staringAgents = new List<Agent>();

	internal readonly Dictionary<int, Vec3> _staringAgentAnchors = new Dictionary<int, Vec3>();

	internal readonly HashSet<int> _staringUseConversationAgents = new HashSet<int>();

	internal readonly object _pendingSceneConversationAttentionReleaseLock = new object();

	internal readonly HashSet<int> _pendingSceneConversationAttentionReleaseAgentIndices = new HashSet<int>();

	internal float _stopStaringTime = 0f;

	internal const float PLAYER_DRIVEN_MULTI_SCENE_STARE_HOLD_SECONDS = 60f;

	internal const float MULTI_SCENE_MOVEMENT_SUPPRESSION_INTERVAL = 0.01f;

	internal const float MULTI_SCENE_MOVEMENT_SUPPRESSION_HOLD_SECONDS = 0.25f;

	internal readonly object _multiSceneMovementSuppressionLock = new object();

	internal readonly HashSet<int> _multiSceneMovementSuppressionAgentIndices = new HashSet<int>();

	internal bool _multiSceneMovementSuppressionActive = false;

	internal float _multiSceneMovementSuppressionTimer = 0f;

	internal void HoldSceneConversationParticipants(List<NpcDataPacket> participants)
	{
		var agents = Mission.Current?.Agents;
		if (ShouldSuppressSceneConversationControlForMeeting() || participants == null || participants.Count <= 1 || agents == null)
		{
			return;
		}
		ExtendStaringHoldForPlayerDrivenSceneRound(participants.Count);
		for (int i = 0; i < participants.Count; i++)
		{
			NpcDataPacket npcDataPacket = participants[i];
			if (npcDataPacket == null)
			{
				continue;
			}
			Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == npcDataPacket.AgentIndex);
			if (agent != null && agent.IsActive() && agent.IsHuman && agent != Agent.Main)
			{
				AddAgentToStareList(agent, interruptCurrentUse: false);
			}
		}
	}

	internal void HoldSceneConversationAgents(List<Agent> participants)
	{
		if (ShouldSuppressSceneConversationControlForMeeting() || participants == null || participants.Count <= 1 || Mission.Current == null)
		{
			return;
		}
		ExtendStaringHoldForPlayerDrivenSceneRound(participants.Count);
		for (int i = 0; i < participants.Count; i++)
		{
			Agent agent = participants[i];
			if (agent != null && agent.IsActive() && agent.IsHuman && agent != Agent.Main)
			{
				AddAgentToStareList(agent, interruptCurrentUse: false);
			}
		}
	}

	internal static void ReleaseSceneConversationAgentsForCombatExternal(
		IEnumerable<int> agentIndices,
		string reason = "combat_start")
	{
		try
		{
			ShoutBehavior instance = CurrentInstance;
			if (instance == null)
			{
				return;
			}
			List<NpcDataPacket> participants = (agentIndices ?? Enumerable.Empty<int>())
				.Where(agentIndex => agentIndex >= 0)
				.Distinct()
				.Select(agentIndex => new NpcDataPacket
				{
					AgentIndex = agentIndex
				})
				.ToList();
			if (participants.Count == 0)
			{
				return;
			}
			instance._j17SceneAttentionController.ReleaseSceneConversationConstraints(
				participants,
				stopAutoGroupSession: true,
				clearQueuedSpeech: false,
				forceFullAutonomyRelease: true);
			Logger.Log(
				"ShoutBehavior",
				"[SceneCombat] released conversation constraints agents="
				+ string.Join(",", participants.Select(participant => participant.AgentIndex))
				+ " reason=" + (reason ?? ""));
		}
		catch (Exception ex)
		{
			Logger.Log(
				"ShoutBehavior",
				"[SceneCombat] release conversation constraints failed reason="
				+ (reason ?? "")
				+ " error=" + ex.Message);
		}
	}

	internal static bool IsAgentHostileToMainAgent(Agent agent)
	{
		try
		{
			Agent main = Agent.Main;
			return agent != null && !agent.IsMainAgent && agent.IsActive() && main != null && main.IsActive() && AreAgentsHostileForSceneConversation(agent, main);
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsUsableTeam(Team team)
	{
		try
		{
			return team != null && team != Team.Invalid && team.IsValid;
		}
		catch
		{
			return false;
		}
	}

	internal static bool AreTeamsHostileSafely(Team firstTeam, Team secondTeam)
	{
		try
		{
			return IsUsableTeam(firstTeam) && IsUsableTeam(secondTeam) && firstTeam != secondTeam && (firstTeam.IsEnemyOf(secondTeam) || secondTeam.IsEnemyOf(firstTeam));
		}
		catch
		{
			return false;
		}
	}

	internal static bool AreAgentsHostileForSceneConversation(Agent a, Agent b)
	{
		if (a == null || b == null || !a.IsActive() || !b.IsActive())
		{
			return false;
		}
		try
		{
			if (a.IsEnemyOf(b) || b.IsEnemyOf(a))
			{
				return true;
			}
		}
		catch
		{
		}
		return AreTeamsHostileSafely(a.Team, b.Team);
	}

	internal void ActivateMultiSceneMovementSuppression(IEnumerable<int> participantAgentIndices)
	{
		if (ShouldSuppressSceneConversationControlForMeeting())
		{
			DeactivateMultiSceneMovementSuppression();
			return;
		}
		if (participantAgentIndices == null)
		{
			return;
		}
		lock (_multiSceneMovementSuppressionLock)
		{
			foreach (int participantAgentIndex in participantAgentIndices)
			{
				if (participantAgentIndex >= 0)
				{
					_multiSceneMovementSuppressionAgentIndices.Add(participantAgentIndex);
				}
			}
			_multiSceneMovementSuppressionActive = _multiSceneMovementSuppressionAgentIndices.Count >= 1;
			if (_multiSceneMovementSuppressionActive)
			{
				_multiSceneMovementSuppressionTimer = MULTI_SCENE_MOVEMENT_SUPPRESSION_INTERVAL;
			}
		}
	}

	internal void RemoveSceneMovementSuppressionAgents(IEnumerable<int> participantAgentIndices)
	{
		if (participantAgentIndices == null)
		{
			return;
		}
		lock (_multiSceneMovementSuppressionLock)
		{
			foreach (int participantAgentIndex in participantAgentIndices)
			{
				if (participantAgentIndex >= 0)
				{
					_multiSceneMovementSuppressionAgentIndices.Remove(participantAgentIndex);
				}
			}
			_multiSceneMovementSuppressionActive = _multiSceneMovementSuppressionAgentIndices.Count >= 1;
			if (!_multiSceneMovementSuppressionActive)
			{
				_multiSceneMovementSuppressionTimer = 0f;
			}
		}
	}

	internal void DeactivateMultiSceneMovementSuppression()
	{
		lock (_multiSceneMovementSuppressionLock)
		{
			_multiSceneMovementSuppressionActive = false;
			_multiSceneMovementSuppressionAgentIndices.Clear();
			_multiSceneMovementSuppressionTimer = 0f;
		}
	}

	internal void RequestSceneConversationAttentionRelease(IEnumerable<int> participantAgentIndices)
	{
		if (participantAgentIndices == null)
		{
			return;
		}
		lock (_pendingSceneConversationAttentionReleaseLock)
		{
			foreach (int participantAgentIndex in participantAgentIndices)
			{
				if (participantAgentIndex >= 0)
				{
					_pendingSceneConversationAttentionReleaseAgentIndices.Add(participantAgentIndex);
				}
			}
		}
	}

	internal void ClearPendingSceneConversationAttentionRelease()
	{
		lock (_pendingSceneConversationAttentionReleaseLock)
		{
			_pendingSceneConversationAttentionReleaseAgentIndices.Clear();
		}
	}

	internal void FlushPendingSceneConversationAttentionRelease()
	{
		if (Mission.Current == null || IsSpeechPipelineBusy())
		{
			return;
		}
		List<int> list = null;
		lock (_pendingSceneConversationAttentionReleaseLock)
		{
			if (_pendingSceneConversationAttentionReleaseAgentIndices.Count == 0)
			{
				return;
			}
			list = _pendingSceneConversationAttentionReleaseAgentIndices.ToList();
			_pendingSceneConversationAttentionReleaseAgentIndices.Clear();
		}
		ReleaseSceneConversationAttention(list);
	}

	internal void UpdateMultiSceneMovementSuppression(float dt)
	{
		List<int> list;
		lock (_multiSceneMovementSuppressionLock)
		{
			if (!_multiSceneMovementSuppressionActive)
			{
				return;
			}
			_multiSceneMovementSuppressionTimer += Math.Max(0f, dt);
			if (_multiSceneMovementSuppressionTimer < MULTI_SCENE_MOVEMENT_SUPPRESSION_INTERVAL)
			{
				return;
			}
			_multiSceneMovementSuppressionTimer = 0f;
			list = _multiSceneMovementSuppressionAgentIndices.ToList();
		}
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		Agent main = Agent.Main;
		if (agents == null || main == null || !main.IsActive())
		{
			DeactivateMultiSceneMovementSuppression();
			return;
		}
		List<int> list2 = null;
		int num = 0;
		foreach (int item in list)
		{
			Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == item);
			if (!CanAgentParticipateInSceneSpeech(agent))
			{
				if (list2 == null)
				{
					list2 = new List<int>();
				}
				list2.Add(item);
				continue;
			}
			try
			{
				_ports.TryGetInteractionSession(item, out var interactionSession);
				float playerRange = ResolveActiveInteractionPlayerRangeMeters(interactionSession);
				float num2 = playerRange * playerRange;
				if (agent.Position.AsVec2.DistanceSquared(main.Position.AsVec2) > num2)
				{
					continue;
				}
			}
			catch
			{
				continue;
			}
			num++;
			ApplyMultiSceneMovementSuppression(agent);
		}
		if (list2 != null && list2.Count > 0)
		{
			lock (_multiSceneMovementSuppressionLock)
			{
				foreach (int item2 in list2)
				{
					_multiSceneMovementSuppressionAgentIndices.Remove(item2);
				}
				if (_multiSceneMovementSuppressionAgentIndices.Count < 1)
				{
					_multiSceneMovementSuppressionActive = false;
					_multiSceneMovementSuppressionTimer = 0f;
				}
			}
		}
		if (num >= 1)
		{
			_stopStaringTime = Math.Max(_stopStaringTime, mission.CurrentTime + MULTI_SCENE_MOVEMENT_SUPPRESSION_HOLD_SECONDS);
		}
	}

	internal void ApplyMultiSceneMovementSuppression(Agent agent)
	{
		if (ShouldSuppressSceneConversationControlForMeeting())
		{
			return;
		}
		if (BattleSpeechRuntimeHost.IsClaimedSpeechSpeaker(agent))
		{
			return;
		}
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			return;
		}
		try
		{
			agent.SetLookAgent(Agent.Main);
			agent.SetMaximumSpeedLimit(0f, isMultiplier: false);
			WorldPosition worldPosition = agent.GetWorldPosition();
			agent.SetScriptedPosition(ref worldPosition, addHumanLikeDelay: false);
		}
		catch
		{
		}
		if (!_staringAgents.Contains(agent))
		{
			_staringAgents.Add(agent);
		}
	}

	internal static bool ShouldSuppressSceneConversationControlForMeeting()
	{
		try
		{
			if (IsMeetingPseudoCombatContext())
			{
				return true;
			}
		}
		catch
		{
		}
		return IsSceneConversationCombatContext();
	}

	internal static bool IsSceneConversationCombatContext()
	{
		try
		{
			if (IsSceneConversationMissionEnding())
			{
				return false;
			}
			if (IsMeetingPseudoCombatContext())
			{
				return false;
			}
			if (IsActiveSceneConversationDuelCombat())
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			MissionFightHandler missionBehavior = Mission.Current?.GetMissionBehavior<MissionFightHandler>();
			if (missionBehavior != null && missionBehavior.IsThereActiveFight())
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			Mission current = Mission.Current;
			Agent main = Agent.Main;
			if (current != null && main != null && main.IsActive() && current.Agents != null)
			{
				MissionMode mode = current.Mode;
				if (mode == MissionMode.Battle || mode == MissionMode.Duel)
				{
					foreach (Agent agent in current.Agents)
					{
						if (agent != null && agent.IsActive() && agent != main && AreAgentsHostileForSceneConversation(agent, main))
						{
							return true;
						}
					}
				}
			}
		}
		catch
		{
		}
		try
		{
			Mission mission = Mission.Current;
			var agents = mission?.Agents;
			Agent main = Agent.Main;
			if (main != null && main.IsActive() && agents != null)
			{
				foreach (Agent agent in agents)
				{
					if (agent != null && agent.IsActive() && agent != main && AreAgentsHostileForSceneConversation(agent, main))
					{
						return true;
					}
				}
			}
		}
		catch
		{
			return false;
		}
		return false;
	}

	internal static bool ShouldReleaseSceneConversationControlForCombat()
	{
		return IsSceneConversationCombatContext();
	}

	internal void ClearMeetingSceneConversationControlState()
	{
		if (!ShouldSuppressSceneConversationControlForMeeting())
		{
			return;
		}
		if (ShouldReleaseSceneConversationControlForCombat())
		{
			ReleaseAllSceneConversationControlForCombat();
			return;
		}
		DeactivateMultiSceneMovementSuppression();
		ClearPendingSceneConversationAttentionRelease();
		_staringAgents.Clear();
		_staringAgentAnchors.Clear();
		_staringUseConversationAgents.Clear();
		_stareTimer = 0f;
		_stareTargetLostGraceTimer = 0f;
		_currentStareTarget = null;
		_stopStaringTime = 0f;
	}

	internal void ReleaseAllSceneConversationControlForCombat()
	{
		HashSet<int> hashSet = new HashSet<int>();
		lock (_multiSceneMovementSuppressionLock)
		{
			foreach (int multiSceneMovementSuppressionAgentIndex in _multiSceneMovementSuppressionAgentIndices)
			{
				if (multiSceneMovementSuppressionAgentIndex >= 0)
				{
					hashSet.Add(multiSceneMovementSuppressionAgentIndex);
				}
			}
		}
		lock (_pendingSceneConversationAttentionReleaseLock)
		{
			foreach (int pendingSceneConversationAttentionReleaseAgentIndex in _pendingSceneConversationAttentionReleaseAgentIndices)
			{
				if (pendingSceneConversationAttentionReleaseAgentIndex >= 0)
				{
					hashSet.Add(pendingSceneConversationAttentionReleaseAgentIndex);
				}
			}
		}
		for (int i = 0; i < _staringAgents.Count; i++)
		{
			Agent agent = _staringAgents[i];
			if (agent != null)
			{
				hashSet.Add(agent.Index);
			}
		}
		DeactivateMultiSceneMovementSuppression();
		ClearPendingSceneConversationAttentionRelease();
		if (hashSet.Count > 0)
		{
			ReleaseSceneConversationAttention(hashSet, fullyRestoreAutonomy: true);
		}
		_staringAgents.Clear();
		_staringAgentAnchors.Clear();
		_staringUseConversationAgents.Clear();
		_stareTimer = 0f;
		_stareTargetLostGraceTimer = 0f;
		_currentStareTarget = null;
		_stopStaringTime = 0f;
	}

	internal static bool IsMeetingSceneConversationReleaseSensitive()
	{
		try
		{
			return IsMeetingPseudoCombatContext();
		}
		catch
		{
			return false;
		}
	}

	internal static bool ShouldPreserveMeetingSceneAutonomy()
	{
		try
		{
			return IsMeetingPseudoCombatContext();
		}
		catch
		{
			return false;
		}
	}

	internal void ClearAgentSceneConversationFocus(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		try
		{
			if (_staringUseConversationAgents.Remove(agent.Index) && agent.CurrentlyUsedGameObject != null)
			{
				agent.CurrentlyUsedGameObject.OnUserConversationEnd();
			}
		}
		catch
		{
		}
		try
		{
			agent.SetLookAgent(null);
		}
		catch
		{
		}
		try
		{
			agent.SetMaximumSpeedLimit(-1f, isMultiplier: false);
		}
		catch
		{
		}
	}

	internal void ReleaseAgentFromSceneConversationLocks(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		ClearAgentSceneConversationFocus(agent);
		if (ShouldPreserveMeetingSceneAutonomy())
		{
			return;
		}
		try
		{
			agent.DisableScriptedMovement();
		}
		catch
		{
		}
		try
		{
			agent.SetIsAIPaused(isPaused: false);
		}
		catch
		{
		}
	}

	internal void ReleaseSceneConversationAttention(IEnumerable<int> participantAgentIndices, bool fullyRestoreAutonomy = true)
	{
		if (participantAgentIndices == null)
		{
			return;
		}
		HashSet<int> hashSet = new HashSet<int>(participantAgentIndices.Where((int agentIndex) => agentIndex >= 0));
		if (hashSet.Count == 0)
		{
			return;
		}
		List<Agent> list = null;
		for (int num = _staringAgents.Count - 1; num >= 0; num--)
		{
			Agent agent = _staringAgents[num];
			if (agent == null)
			{
				_staringAgents.RemoveAt(num);
				continue;
			}
			if (!hashSet.Contains(agent.Index))
			{
				continue;
			}
			if (list == null)
			{
				list = new List<Agent>();
			}
			list.Add(agent);
			_staringAgents.RemoveAt(num);
			_staringAgentAnchors.Remove(agent.Index);
		}
		if (list == null || list.Count == 0)
		{
			return;
		}
		foreach (Agent item in list)
		{
			if (fullyRestoreAutonomy)
			{
				RestoreAgentAutonomy(item);
			}
			else
			{
				ReleaseAgentFromSceneConversationLocks(item);
			}
		}
		if (_staringAgents.Count == 0)
		{
			_stopStaringTime = 0f;
		}
	}

	internal void ReleaseSceneConversationConstraints(List<NpcDataPacket> participants, int fallbackAgentIndex = -1, bool stopAutoGroupSession = true, bool clearQueuedSpeech = true, bool forceFullAutonomyRelease = false)
	{
		List<int> list = new List<int>();
		if (participants != null)
		{
			foreach (NpcDataPacket participant in participants)
			{
				if (participant != null && participant.AgentIndex >= 0 && !list.Contains(participant.AgentIndex))
				{
					list.Add(participant.AgentIndex);
				}
			}
		}
		if (fallbackAgentIndex >= 0 && !list.Contains(fallbackAgentIndex))
		{
			list.Add(fallbackAgentIndex);
		}
		if (list.Count == 0)
		{
			return;
		}
		if (clearQueuedSpeech)
		{
			ClearQueuedSceneSpeech();
		}
		ClearPendingSceneConversationAttentionRelease();
		RemoveSceneMovementSuppressionAgents(list);
		ReleaseSceneConversationAttention(list, forceFullAutonomyRelease || !IsMeetingSceneConversationReleaseSensitive());
		foreach (int item in list)
		{
			_ports.CancelInteractionTimeoutArm(item);
			_ports.RemoveInteractionSession(item);
		}
	}

	internal static void RefreshHostileCombatAgentAutonomy(Agent agent)
	{
		if (!IsAgentHostileToMainAgent(agent) || !agent.IsAIControlled)
		{
			return;
		}
		try
		{
			AgentFlag agentFlags = agent.GetAgentFlags();
			agent.SetAgentFlags(agentFlags | AgentFlag.CanGetAlarmed);
		}
		catch
		{
		}
		try
		{
			agent.SetLookAgent(null);
		}
		catch
		{
		}
		try
		{
			agent.SetMaximumSpeedLimit(-1f, isMultiplier: false);
		}
		catch
		{
		}
		try
		{
			agent.DisableScriptedMovement();
		}
		catch
		{
		}
		try
		{
			agent.ClearTargetFrame();
		}
		catch
		{
		}
		try
		{
			agent.SetIsAIPaused(isPaused: false);
		}
		catch
		{
		}
		try
		{
			agent.ResetEnemyCaches();
			agent.InvalidateTargetAgent();
			agent.InvalidateAIWeaponSelections();
		}
		catch
		{
		}
		try
		{
			agent.SetAlarmState(Agent.AIStateFlag.Alarmed);
		}
		catch
		{
		}
		try
		{
			agent.SetWatchState(Agent.WatchState.Alarmed);
		}
		catch
		{
		}
	}

	internal void RestoreAgentAutonomy(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		_sceneMovement.ClearSceneSummonScriptedBehavior(agent);
		bool flag = IsAgentHostileToMainAgent(agent);
		ClearAgentSceneConversationFocus(agent);
		if (ShouldPreserveMeetingSceneAutonomy())
		{
			return;
		}
		try
		{
			agent.DisableScriptedMovement();
		}
		catch
		{
		}
		try
		{
			agent.ClearTargetFrame();
		}
		catch
		{
		}
		try
		{
			agent.SetIsAIPaused(isPaused: false);
		}
		catch
		{
		}
		if (flag)
		{
			RefreshHostileCombatAgentAutonomy(agent);
			return;
		}
		try
		{
			agent.SetWatchState(Agent.WatchState.Patrolling);
		}
		catch
		{
		}
		try
		{
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			(component?.AgentNavigator ?? component?.CreateAgentNavigator())?.ClearTarget();
		}
		catch
		{
		}
	}

	internal static bool ShouldPreserveAmbientUseDuringStare(Agent agent)
	{
		object obj = agent?.CurrentlyUsedGameObject;
		if (obj == null)
		{
			return false;
		}
		if (obj is PlayMusicPoint)
		{
			return true;
		}
		try
		{
			string text = obj.GetType().Name ?? "";
			if (agent.CurrentlyUsedGameObject != null)
			{
				WeakGameEntity gameEntity = agent.CurrentlyUsedGameObject.GameEntity;
				if (gameEntity.IsValid)
				{
					text = text + " " + gameEntity.Name;
				}
			}
			text = text.ToLowerInvariant();
			return text.Contains("dance") || text.Contains("dancing") || text.Contains("music") || text.Contains("musician") || text.Contains("instrument");
		}
		catch
		{
			return false;
		}
	}

	internal void TryInterruptAgentSceneUseForStare(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		try
		{
			if (!agent.IsUsingGameObject && agent.CurrentlyUsedGameObject == null)
			{
				return;
			}
		}
		catch
		{
			return;
		}
		if (ShouldPreserveAmbientUseDuringStare(agent))
		{
			return;
		}
		try
		{
			if (agent.CurrentlyUsedGameObject != null)
			{
				agent.CurrentlyUsedGameObject.OnUserConversationStart();
				_staringUseConversationAgents.Add(agent.Index);
			}
		}
		catch
		{
		}
	}

	internal static string GetAgentDebugLabel(Agent agent)
	{
		if (agent == null)
		{
			return "null";
		}
		string text = (agent.Name?.ToString() ?? "NPC").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "NPC";
		}
		return text + "#" + agent.Index;
	}

	internal void TraceStareDebug(string message)
	{
		try
		{
			Logger.Log("ShoutBehavior", "[STARE] " + message);
		}
		catch
		{
		}
	}

	internal void FreezeAgentForStare(Agent agent, bool trace = false)
	{
		if (agent == null || !agent.IsActive())
		{
			if (trace)
			{
				TraceStareDebug("Freeze skipped: invalid agent");
			}
			return;
		}
		try
		{
			agent.SetLookAgent(Agent.Main);
		}
		catch
		{
			if (trace)
			{
				TraceStareDebug("SetLookAgent failed for " + GetAgentDebugLabel(agent));
			}
		}
		try
		{
			agent.SetIsAIPaused(isPaused: false);
		}
		catch
		{
			if (trace)
			{
				TraceStareDebug("SetIsAIPaused(false) failed for " + GetAgentDebugLabel(agent));
			}
		}
		float? rotation = null;
		try
		{
			if (Agent.Main != null && Agent.Main.IsActive())
			{
				Vec3 vec = Agent.Main.Position - agent.Position;
				if (vec.AsVec2.LengthSquared > 0.0001f)
				{
					rotation = vec.AsVec2.RotationInRadians;
				}
			}
		}
		catch
		{
			rotation = null;
		}
		try
		{
			var scene = Mission.Current?.Scene;
			if (scene == null)
			{
				return;
			}
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			AgentNavigator agentNavigator = component?.AgentNavigator ?? component?.CreateAgentNavigator();
			Vec3 anchorPosition = agent.Position;
			if (_staringAgentAnchors.TryGetValue(agent.Index, out var storedAnchor))
			{
				anchorPosition = storedAnchor;
			}
			if (agentNavigator != null && rotation.HasValue)
			{
				WorldPosition worldPosition = new WorldPosition(scene, anchorPosition);
				agentNavigator.SetTargetFrame(worldPosition, rotation.Value, 0.05f, 0.8f, Agent.AIScriptedFrameFlags.DoNotRun | Agent.AIScriptedFrameFlags.NoAttack, false);
			}
			else
			{
				WorldPosition worldPosition2 = new WorldPosition(scene, anchorPosition);
				agent.SetScriptedPosition(ref worldPosition2, addHumanLikeDelay: false);
			}
		}
		catch
		{
			if (trace)
			{
				TraceStareDebug("SetTargetFrame/SetScriptedPosition failed for " + GetAgentDebugLabel(agent));
			}
		}
		if (trace)
		{
			TraceStareDebug("Freeze applied to " + GetAgentDebugLabel(agent) + " vel2=" + agent.Velocity.AsVec2.LengthSquared + " rotation=" + (rotation.HasValue ? rotation.Value.ToString("F3") : "null"));
		}
	}

	internal void ForceAgentFacePlayer(Agent agent)
	{
		var scene = Mission.Current?.Scene;
		if (agent == null || !agent.IsActive() || scene == null || Agent.Main == null || !Agent.Main.IsActive())
		{
			return;
		}
		try
		{
			Vec3 position = agent.Position;
			Vec3 position2 = Agent.Main.Position;
			try
			{
				if (agent.AgentVisuals != null)
				{
					position.z = agent.AgentVisuals.GetGlobalStableEyePoint(true).z;
				}
			}
			catch
			{
			}
			try
			{
				if (Agent.Main.AgentVisuals != null)
				{
					position2.z = Agent.Main.AgentVisuals.GetGlobalStableEyePoint(true).z;
				}
			}
			catch
			{
			}
			Vec3 vec = position2 - position;
			if (vec.LengthSquared < 0.0001f)
			{
				return;
			}
			try
			{
				if (Agent.Main.AgentVisuals != null)
				{
					agent.SetLookToPointOfInterest(Agent.Main.AgentVisuals.GetGlobalStableEyePoint(true));
				}
			}
			catch
			{
			}
			try
			{
				agent.AgentVisuals?.GetSkeleton()?.ForceUpdateBoneFrames();
			}
			catch
			{
			}
			agent.LookDirection = vec.NormalizedCopy();
			if (IsAgentNearlyStationary(agent))
			{
				Vec3 vec2 = agent.Position;
				if (_staringAgentAnchors.TryGetValue(agent.Index, out var value))
				{
					vec2 = value;
				}
				WorldPosition scriptedPosition = new WorldPosition(scene, vec2);
				agent.SetScriptedPositionAndDirection(ref scriptedPosition, vec.AsVec2.RotationInRadians, addHumanLikeDelay: false, Agent.AIScriptedFrameFlags.NoAttack | Agent.AIScriptedFrameFlags.DoNotRun);
			}
		}
		catch
		{
		}
	}

	internal static bool IsAgentNearlyStationary(Agent agent)
	{
		try
		{
			return agent != null && agent.Velocity.AsVec2.LengthSquared <= 0.01f;
		}
		catch
		{
			return false;
		}
	}

	internal void ResetStaringForActiveInteraction(List<Agent> nearbyAgents, Agent primaryTarget)
	{
		List<Agent> list = nearbyAgents ?? new List<Agent>();
		List<Agent> passiveCooldownGroupAgents = ((primaryTarget != null) ? GetPassiveCooldownGroupAgents(primaryTarget) : list);
		List<NpcDataPacket> list2 = (from a in passiveCooldownGroupAgents
			select ShoutUtils.ExtractNpcData(a) into d
			where d != null
			select d).ToList();
		ApplyInteractionGraceAndGroupCooldown(ScenePassiveInteractionController.PASSIVE_INTERACTION_GRACE, ScenePassiveInteractionController.ACTIVE_CHAT_COOLDOWN, passiveCooldownGroupAgents, primaryTarget, list2);
		if (ShouldSuppressSceneConversationControlForMeeting())
		{
			ClearMeetingSceneConversationControlState();
			return;
		}
		ResetStaringBehavior();
		Mission mission = Mission.Current;
		if (mission == null)
		{
			TraceStareDebug("ResetStaringForActiveInteraction aborted: Mission.Current null");
			return;
		}
		TraceStareDebug("ResetStaringForActiveInteraction primary=" + GetAgentDebugLabel(primaryTarget) + " nearby=" + list.Count);
		_stopStaringTime = mission.CurrentTime + 20f;
		foreach (Agent item in list)
		{
			AddAgentToStareList(item, primaryTarget != null && item.Index == primaryTarget.Index);
		}
	}

	internal void ExtendStaringHoldForPlayerDrivenSceneRound(int participantCount)
	{
		Mission mission = Mission.Current;
		if (participantCount <= 1 || mission == null)
		{
			return;
		}
		_stopStaringTime = Math.Max(_stopStaringTime, mission.CurrentTime + PLAYER_DRIVEN_MULTI_SCENE_STARE_HOLD_SECONDS);
	}

	internal void UpdateStaringBehavior()
	{
		Mission mission = Mission.Current;
		if (_staringAgents.Count <= 0 || mission == null)
		{
			return;
		}
		for (int i = _staringAgents.Count - 1; i >= 0; i--)
		{
			Agent controlledSpeaker = _staringAgents[i];
			if (!BattleSpeechRuntimeHost.IsClaimedSpeechSpeaker(controlledSpeaker))
			{
				continue;
			}
			_staringAgents.RemoveAt(i);
			_staringAgentAnchors.Remove(controlledSpeaker.Index);
			_staringUseConversationAgents.Remove(controlledSpeaker.Index);
			try
			{
				controlledSpeaker.SetLookAgent(null);
				controlledSpeaker.SetMaximumSpeedLimit(-1f, isMultiplier: false);
			}
			catch
			{
			}
		}
		if (_staringAgents.Count == 0)
		{
			_stopStaringTime = 0f;
			return;
		}
		float currentTime = mission.CurrentTime;
		if (currentTime < _stopStaringTime)
		{
			foreach (Agent staringAgent in _staringAgents)
			{
				if (staringAgent != null && staringAgent.IsActive())
				{
					try
					{
						staringAgent.SetLookAgent(Agent.Main);
						staringAgent.SetMaximumSpeedLimit(0f, isMultiplier: false);
					}
					catch
					{
					}
				}
			}
			return;
		}
		ResetStaringBehavior();
	}

	internal void ResetStaringBehavior()
	{
		foreach (Agent staringAgent in _staringAgents)
		{
			if (staringAgent != null && staringAgent.IsActive())
			{
				try
				{
					staringAgent.SetLookAgent(null);
					staringAgent.SetMaximumSpeedLimit(-1f, isMultiplier: false);
					if (!ShouldPreserveMeetingSceneAutonomy())
					{
						staringAgent.DisableScriptedMovement();
					}
				}
				catch
				{
				}
			}
		}
		_staringAgents.Clear();
		_staringAgentAnchors.Clear();
		_staringUseConversationAgents.Clear();
	}

	internal void AddAgentToStareList(Agent agent, bool interruptCurrentUse = false)
	{
		if (ShouldSuppressSceneConversationControlForMeeting())
		{
			return;
		}
		if (BattleSpeechRuntimeHost.IsClaimedSpeechSpeaker(agent))
		{
			try
			{
				agent?.SetLookAgent(null);
			}
			catch
			{
			}
			return;
		}
		if (agent != null)
		{
			Mission mission = Mission.Current;
			if (mission != null)
			{
				_stopStaringTime = Math.Max(_stopStaringTime, mission.CurrentTime + 20f);
			}
			if (interruptCurrentUse)
			{
				TryInterruptAgentSceneUseForStare(agent);
			}
			try
			{
				agent.SetLookAgent(Agent.Main);
				agent.SetMaximumSpeedLimit(0f, isMultiplier: false);
				WorldPosition worldPosition = agent.GetWorldPosition();
				agent.SetScriptedPosition(ref worldPosition, addHumanLikeDelay: false);
			}
			catch
			{
			}
			if (!_staringAgents.Contains(agent))
			{
				_staringAgents.Add(agent);
			}
		}
	}

    private Agent _currentStareTarget { get => _ports.Get_currentStareTarget(); set => _ports.Set_currentStareTarget(value); }
    private float _stareTimer { get => _ports.Get_stareTimer(); set => _ports.Set_stareTimer(value); }
    private float _stareTargetLostGraceTimer { get => _ports.Get_stareTargetLostGraceTimer(); set => _ports.Set_stareTargetLostGraceTimer(value); }
    private void ApplyInteractionGraceAndGroupCooldown(float graceSeconds, float cooldownSeconds, IEnumerable<Agent> participants, Agent extraTarget = null, IEnumerable<NpcDataPacket> participantsData = null) => _ports.ApplyInteractionGraceAndGroupCooldown_L8009(graceSeconds, cooldownSeconds, participants, extraTarget, participantsData);
    private List<Agent> GetPassiveCooldownGroupAgents(Agent targetAgent) => _ports.GetPassiveCooldownGroupAgents_L8070(targetAgent);
    private void ClearQueuedSceneSpeech() => _ports.ClearQueuedSceneSpeech_L19216();
    private bool IsSpeechPipelineBusy() => _ports.IsSpeechPipelineBusy_L19229();
    private SceneMovementController _sceneMovement { get => _ports.Get_sceneMovement(); }
    internal void CaptureMovementSuppressionAgents(HashSet<int> indices)
    {
        lock (_multiSceneMovementSuppressionLock)
            if (_multiSceneMovementSuppressionActive)
                foreach (int index in _multiSceneMovementSuppressionAgentIndices)
                    if (index >= 0) indices.Add(index);
    }
    internal void DetachStareAgent(int index)
    {
        _staringAgents.RemoveAll(agent => agent == null || agent.Index == index);
        _staringAgentAnchors.Remove(index);
    }
    // Read-only capture leaf: preserve original null-safe Any(Index) semantics without exposing the store.
    internal bool IsStaringAgentIndex(int index)
    {
        for (int i = 0; i < _staringAgents.Count; i++)
        {
            Agent agent = _staringAgents[i];
            if (agent != null && agent.Index == index) return true;
        }
        return false;
    }
    internal void ClearEmptyStareDeadline() { if (_staringAgents.Count == 0) _stopStaringTime = 0f; }
    internal void ReleaseStareAgentForInteraction(int index) { DetachStareAgent(index); ClearEmptyStareDeadline(); }
    internal void ReleaseUseConversationSlot(int index) { _staringUseConversationAgents.Remove(index); }
    internal void RemoveAttentionRelease(int index) { lock (_pendingSceneConversationAttentionReleaseLock) _pendingSceneConversationAttentionReleaseAgentIndices.Remove(index); }
    internal void ResetAttentionTransientForLoadedSave()
    {
        _staringAgents.Clear(); _staringAgentAnchors.Clear(); _staringUseConversationAgents.Clear();
    }

}

internal sealed class SceneAttentionControllerPorts
{
    internal Func<Agent> Get_currentStareTarget;
    internal Action<Agent> Set_currentStareTarget;
    internal Func<float> Get_stareTimer;
    internal Action<float> Set_stareTimer;
    internal Func<float> Get_stareTargetLostGraceTimer;
    internal Action<float> Set_stareTargetLostGraceTimer;
    internal delegate void ApplyInteractionGraceAndGroupCooldown_L8009Callback(float graceSeconds, float cooldownSeconds, IEnumerable<Agent> participants, Agent extraTarget, IEnumerable<NpcDataPacket> participantsData);
    internal ApplyInteractionGraceAndGroupCooldown_L8009Callback ApplyInteractionGraceAndGroupCooldown_L8009;
    internal delegate List<Agent> GetPassiveCooldownGroupAgents_L8070Callback(Agent targetAgent);
    internal GetPassiveCooldownGroupAgents_L8070Callback GetPassiveCooldownGroupAgents_L8070;
    internal delegate void ClearQueuedSceneSpeech_L19216Callback();
    internal ClearQueuedSceneSpeech_L19216Callback ClearQueuedSceneSpeech_L19216;
    internal delegate bool IsSpeechPipelineBusy_L19229Callback();
    internal IsSpeechPipelineBusy_L19229Callback IsSpeechPipelineBusy_L19229;
    internal Func<SceneMovementController> Get_sceneMovement;
    internal delegate bool TryGetInteractionSessionCallback(int index, out SceneInteractionSession session);
    internal TryGetInteractionSessionCallback TryGetInteractionSession;
    internal Action<int> RemoveInteractionSession, CancelInteractionTimeoutArm;

}
