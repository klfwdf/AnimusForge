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

internal sealed class SceneInteractionLifecycleController
{
    private readonly SceneInteractionLifecycleControllerPorts _ports;
    internal SceneInteractionLifecycleController(SceneInteractionLifecycleControllerPorts ports) { _ports = ports ?? throw new ArgumentNullException(nameof(ports)); }

	internal const float ACTIVE_INTERACTION_IDLE_TIMEOUT = 45f;

	internal const float ACTIVE_INTERACTION_GROUP_IDLE_TIMEOUT = 300f;

	internal const float ACTIVE_INTERACTION_DYNAMIC_SINGLE_TIMEOUT_CAP = 240f;

	internal const float ACTIVE_INTERACTION_DYNAMIC_GROUP_TIMEOUT_CAP = 600f;

	internal const float ACTIVE_INTERACTION_GROUP_SPEAKER_BONUS_SECONDS = 20f;

	internal const float ACTIVE_INTERACTION_IDLE_PLAYER_RANGE = 10f;

	internal const float ACTIVE_INTERACTION_DISTANT_RELEASE_MIN_EXTRA_RANGE = 15f;

	internal const float ACTIVE_INTERACTION_DISTANT_RELEASE_EXTRA_RATIO = 0.5f;

	internal readonly Dictionary<int, SceneInteractionSession> _activeInteractionSessions = new Dictionary<int, SceneInteractionSession>();

	internal readonly Dictionary<int, PendingInteractionTimeoutArm> _pendingInteractionTimeoutArms = new Dictionary<int, PendingInteractionTimeoutArm>();

	internal bool IsMultiNpcSceneConversationActive()
	{
		Mission mission = Mission.Current;
		if (mission == null)
		{
			return false;
		}
		HashSet<int> agentIndices = new HashSet<int>();
        _ports.CaptureMovementSuppressionAgents(agentIndices);
		foreach (KeyValuePair<int, SceneInteractionSession> activeInteractionSession in _activeInteractionSessions)
		{
			if (activeInteractionSession.Key >= 0 && activeInteractionSession.Value != null)
			{
				agentIndices.Add(activeInteractionSession.Key);
			}
		}
		if (agentIndices.Count < 2)
		{
			return false;
		}
		int liveParticipantCount = 0;
		foreach (int agentIndex in agentIndices)
		{
			Agent agent = mission.Agents?.FirstOrDefault(a => a != null && a.Index == agentIndex && a.IsActive());
			if (CanAgentParticipateInSceneSpeech(agent))
			{
				liveParticipantCount++;
				if (liveParticipantCount >= 2)
				{
					return true;
				}
			}
		}
		return false;
	}

	internal void PrepareAutoGroupParticipantsForIdleTimeout(List<NpcDataPacket> participants, string trailingSpeechText = null, string playerText = null, List<string> npcVisibleTexts = null, int distinctNpcSpeakerCount = 0)
	{
		if (participants == null || participants.Count == 0 || Mission.Current == null)
		{
			return;
		}
		List<NpcDataPacket> list = participants.Where((NpcDataPacket npc) => npc != null && npc.AgentIndex >= 0)
			.GroupBy((NpcDataPacket npc) => npc.AgentIndex)
			.Select((IGrouping<int, NpcDataPacket> group) => group.First())
			.ToList();
		if (list.Count == 0)
		{
			return;
		}
		float num = 0f;
		string text = SanitizeSceneSpeechText(trailingSpeechText ?? "");
		if (!string.IsNullOrWhiteSpace(text))
		{
			num = Math.Max(0.25f, EstimateBubbleTypingDurationSeconds(text));
		}
		int num2 = Math.Max(1, list.Count);
		float timeoutSeconds = ResolveDynamicSceneConversationTimeoutSeconds(playerText, npcVisibleTexts, distinctNpcSpeakerCount, num2);
		foreach (NpcDataPacket item in list)
		{
			RefreshActiveInteractionTimeout(item, num2, timeoutSeconds);
			if (!_activeInteractionSessions.TryGetValue(item.AgentIndex, out var value) || value == null)
			{
				continue;
			}
			if (num > 0f)
			{
				ScheduleInteractionTimeoutArm(item.AgentIndex, value.InteractionToken, num);
			}
			else
			{
				ArmActiveInteractionTimeoutNow(item.AgentIndex, value.InteractionToken);
			}
		}
	}

	internal void RefreshSceneConversationParticipantInteractions(List<NpcDataPacket> participants, float timeoutSeconds = -1f, ShoutTargetingContext shoutTargetingContext = null)
	{
		if (participants == null || participants.Count == 0 || Mission.Current == null)
		{
			return;
		}
		List<NpcDataPacket> list = participants.Where((NpcDataPacket npc) => npc != null && npc.AgentIndex >= 0)
			.GroupBy((NpcDataPacket npc) => npc.AgentIndex)
			.Select((IGrouping<int, NpcDataPacket> group) => group.First())
			.ToList();
		if (list.Count == 0)
		{
			return;
		}
		int num = Math.Max(1, list.Count);
		foreach (NpcDataPacket item in list)
		{
			TrackPlayerInteraction(item, num, timeoutSeconds, false, shoutTargetingContext);
		}
	}

	internal static float GetSceneConversationTimeoutSecondsPerVisibleCharacter()
	{
		float value = 1f;
		try
		{
			value = DuelSettings.GetSettings()?.SceneConversationTimeoutSecondsPerVisibleCharacter ?? 1f;
		}
		catch
		{
			value = 1f;
		}
		if (float.IsNaN(value) || float.IsInfinity(value))
		{
			value = 1f;
		}
		return Math.Max(0.5f, Math.Min(3f, value));
	}

	internal static string NormalizeSceneTimeoutVisibleText(string text)
	{
		string text2 = StripNpcNamePrefixSafely(SanitizeSceneSpeechText(text ?? ""), 30);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		text2 = Regex.Replace(text2, "\\[[^\\]\\r\\n]*AFEF[^\\]\\r\\n]*\\][^\\r\\n]*", "", RegexOptions.IgnoreCase);
		string[] array = text2.Replace("\r", "").Split('\n');
		List<string> list = new List<string>();
		foreach (string item in array)
		{
			string text3 = (item ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text3))
			{
				continue;
			}
			if (text3.StartsWith("[AFEF", StringComparison.OrdinalIgnoreCase) || text3.StartsWith("【AFEF", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			list.Add(text3);
		}
		return string.Join("\n", list).Trim();
	}

	internal static int CountSceneTimeoutVisibleCharacters(string text)
	{
		string text2 = NormalizeSceneTimeoutVisibleText(text);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return 0;
		}
		int num = 0;
		foreach (char c in text2)
		{
			if (!char.IsWhiteSpace(c))
			{
				num++;
			}
		}
		return num;
	}

	internal static int CountSceneTimeoutVisibleCharacters(IEnumerable<string> texts)
	{
		if (texts == null)
		{
			return 0;
		}
		int num = 0;
		foreach (string text in texts)
		{
			num += CountSceneTimeoutVisibleCharacters(text);
		}
		return num;
	}

	internal static float ResolveDynamicSceneConversationTimeoutSeconds(string playerText, IEnumerable<string> npcVisibleTexts, int distinctNpcSpeakerCount, int participantCount)
	{
		int visibleCharCount = CountSceneTimeoutVisibleCharacters(playerText) + CountSceneTimeoutVisibleCharacters(npcVisibleTexts);
		float secondsPerChar = GetSceneConversationTimeoutSecondsPerVisibleCharacter();
		int speakerCount = Math.Max(0, distinctNpcSpeakerCount);
		bool groupLike = participantCount > 1 || speakerCount > 1;
		float cap = groupLike ? ACTIVE_INTERACTION_DYNAMIC_GROUP_TIMEOUT_CAP : ACTIVE_INTERACTION_DYNAMIC_SINGLE_TIMEOUT_CAP;
		float value = ACTIVE_INTERACTION_IDLE_TIMEOUT + visibleCharCount * secondsPerChar + Math.Max(0, speakerCount - 1) * ACTIVE_INTERACTION_GROUP_SPEAKER_BONUS_SECONDS;
		if (float.IsNaN(value) || float.IsInfinity(value))
		{
			value = ACTIVE_INTERACTION_IDLE_TIMEOUT;
		}
		return Math.Max(ACTIVE_INTERACTION_IDLE_TIMEOUT, Math.Min(cap, value));
	}

	internal static float ResolveActiveInteractionTimeoutSeconds(int participantCount, float timeoutSeconds)
	{
		if (timeoutSeconds > 0f)
		{
			return timeoutSeconds;
		}
		return (participantCount > 1) ? ACTIVE_INTERACTION_GROUP_IDLE_TIMEOUT : ACTIVE_INTERACTION_IDLE_TIMEOUT;
	}

	internal void RefreshActiveInteractionTimeout(NpcDataPacket primaryTarget, int participantCount, float timeoutSeconds)
	{
		if (primaryTarget == null || primaryTarget.AgentIndex < 0 || Mission.Current == null || timeoutSeconds <= 0f)
		{
			return;
		}
		if (_activeInteractionSessions.TryGetValue(primaryTarget.AgentIndex, out var value) && value != null)
		{
			value.TimeoutSeconds = ResolveActiveInteractionTimeoutSeconds(participantCount, timeoutSeconds);
			return;
		}
		TrackPlayerInteraction(primaryTarget, participantCount, timeoutSeconds);
	}

	internal void TrackPlayerInteraction(NpcDataPacket primaryTarget, int participantCount = 1, float timeoutSeconds = -1f, bool returnSceneSummonOnTimeout = false, ShoutTargetingContext shoutTargetingContext = null)
	{
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (primaryTarget == null || primaryTarget.AgentIndex < 0 || mission == null)
		{
			return;
		}
		Agent agent = agents?.FirstOrDefault((Agent a) => a != null && a.Index == primaryTarget.AgentIndex && a.IsActive());
		bool flag = _sceneMovement.IsAgentBusyWithSceneGuideErrand(primaryTarget.AgentIndex);
		bool flag2 = flag && _sceneMovement.HasGuideArrivalHold(primaryTarget.AgentIndex);
		if (_sceneMovement.IsAgentBusyWithSceneSummonErrand(primaryTarget.AgentIndex) || (flag && !flag2))
		{
			_pendingInteractionTimeoutArms.Remove(primaryTarget.AgentIndex);
			_activeInteractionSessions.Remove(primaryTarget.AgentIndex);
			return;
		}
		if (_sceneMovement.IsAgentFollowingPlayerBySceneCommand(agent))
		{
			_pendingInteractionTimeoutArms.Remove(primaryTarget.AgentIndex);
			_activeInteractionSessions.Remove(primaryTarget.AgentIndex);
			return;
		}
		string text = (primaryTarget.Name ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "NPC";
		}
		float initialPlayerDistanceMeters = 0f;
		float playerReleaseRangeMeters = ACTIVE_INTERACTION_IDLE_PLAYER_RANGE;
		if (TryGetSnapshotPlayerDistanceMeters(shoutTargetingContext, primaryTarget.AgentIndex, agent, out initialPlayerDistanceMeters))
		{
			playerReleaseRangeMeters = CalculateDistantActiveInteractionReleaseRange(initialPlayerDistanceMeters);
		}
		_activeInteractionSessions[primaryTarget.AgentIndex] = new SceneInteractionSession
		{
			TargetAgentIndex = primaryTarget.AgentIndex,
			TargetName = text,
			LastActivityTime = mission.CurrentTime,
			TimeoutArmed = false,
			TimeoutSeconds = ResolveActiveInteractionTimeoutSeconds(participantCount, timeoutSeconds),
			InteractionToken = DateTime.UtcNow.Ticks,
			ReturnSceneSummonOnTimeout = returnSceneSummonOnTimeout,
			InitialPlayerDistanceMeters = initialPlayerDistanceMeters,
			PlayerReleaseRangeMeters = playerReleaseRangeMeters
		};
		_pendingInteractionTimeoutArms.Remove(primaryTarget.AgentIndex);
	}

	internal void ScheduleInteractionTimeoutArm(int agentIndex, long interactionToken, float speechDurationSeconds)
	{
		Mission mission = Mission.Current;
		if (agentIndex < 0 || interactionToken == 0L || mission == null)
		{
			return;
		}
		float num = mission.CurrentTime + Math.Max(0f, speechDurationSeconds);
		_pendingInteractionTimeoutArms[agentIndex] = new PendingInteractionTimeoutArm
		{
			AgentIndex = agentIndex,
			InteractionToken = interactionToken,
			ArmAtMissionTime = num
		};
	}

	internal void ArmActiveInteractionTimeoutNow(int agentIndex, long interactionToken)
	{
		Mission mission = Mission.Current;
		if (agentIndex < 0 || mission == null)
		{
			return;
		}
		if (!_activeInteractionSessions.TryGetValue(agentIndex, out var value) || value == null || value.InteractionToken != interactionToken)
		{
			return;
		}
		value.LastActivityTime = mission.CurrentTime;
		value.TimeoutArmed = true;
		_pendingInteractionTimeoutArms.Remove(agentIndex);
	}

	internal void ProcessPendingInteractionTimeoutArms()
	{
		Mission mission = Mission.Current;
		if (mission == null || _pendingInteractionTimeoutArms.Count == 0)
		{
			return;
		}
		float currentTime = mission.CurrentTime;
		List<int> list = null;
		foreach (KeyValuePair<int, PendingInteractionTimeoutArm> pendingInteractionTimeoutArm in _pendingInteractionTimeoutArms)
		{
			PendingInteractionTimeoutArm value = pendingInteractionTimeoutArm.Value;
			if (value == null || currentTime >= value.ArmAtMissionTime)
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(pendingInteractionTimeoutArm.Key);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (int item in list)
		{
			if (_pendingInteractionTimeoutArms.TryGetValue(item, out var value2))
			{
				if (value2 != null)
				{
					ArmActiveInteractionTimeoutNow(value2.AgentIndex, value2.InteractionToken);
				}
				else
				{
					_pendingInteractionTimeoutArms.Remove(item);
				}
			}
		}
	}

	internal static float ResolveActiveInteractionPlayerRangeMeters(SceneInteractionSession session)
	{
		float range = session?.PlayerReleaseRangeMeters ?? 0f;
		if (float.IsNaN(range) || float.IsInfinity(range) || range < ACTIVE_INTERACTION_IDLE_PLAYER_RANGE)
		{
			return ACTIVE_INTERACTION_IDLE_PLAYER_RANGE;
		}
		return range;
	}

	internal static float CalculateDistantActiveInteractionReleaseRange(float initialPlayerDistanceMeters)
	{
		if (float.IsNaN(initialPlayerDistanceMeters) || float.IsInfinity(initialPlayerDistanceMeters) || initialPlayerDistanceMeters <= ACTIVE_INTERACTION_IDLE_PLAYER_RANGE)
		{
			return ACTIVE_INTERACTION_IDLE_PLAYER_RANGE;
		}
		float extraRange = Math.Max(ACTIVE_INTERACTION_DISTANT_RELEASE_MIN_EXTRA_RANGE, initialPlayerDistanceMeters * ACTIVE_INTERACTION_DISTANT_RELEASE_EXTRA_RATIO);
		float releaseRange = initialPlayerDistanceMeters + extraRange;
		if (float.IsNaN(releaseRange) || float.IsInfinity(releaseRange) || releaseRange < ACTIVE_INTERACTION_IDLE_PLAYER_RANGE)
		{
			return ACTIVE_INTERACTION_IDLE_PLAYER_RANGE;
		}
		return releaseRange;
	}

	internal static bool TryGetSnapshotPlayerDistanceMeters(ShoutTargetingContext targetingContext, int agentIndex, Agent agent, out float distanceMeters)
	{
		distanceMeters = 0f;
		if (targetingContext == null || agentIndex < 0)
		{
			return false;
		}
		if (targetingContext.CandidateAgentIndices == null || !targetingContext.CandidateAgentIndices.Contains(agentIndex))
		{
			return false;
		}
		if (targetingContext.CandidatePlayerDistancesMeters != null && targetingContext.CandidatePlayerDistancesMeters.TryGetValue(agentIndex, out distanceMeters))
		{
			return !(float.IsNaN(distanceMeters) || float.IsInfinity(distanceMeters) || distanceMeters < 0f);
		}
		return TryGetPlayerPlanarDistanceMeters(agent, out distanceMeters);
	}

	internal static bool IsPlayerWithinActiveInteractionRange(Agent targetAgent, SceneInteractionSession session = null)
	{
		if (targetAgent == null || !targetAgent.IsActive() || Agent.Main == null || !Agent.Main.IsActive())
		{
			return false;
		}
		try
		{
			float playerRange = ResolveActiveInteractionPlayerRangeMeters(session);
			float num = playerRange * playerRange;
			return targetAgent.Position.AsVec2.DistanceSquared(Agent.Main.Position.AsVec2) <= num;
		}
		catch
		{
			return false;
		}
	}

	internal long TimeoutUpdateCalls;
    internal int LastTimeoutActiveCount, LastTimeoutAgentCount, LastTimeoutWorkItems, LastTimeoutAgentInspections, LastTimeoutGroupAgentInspections;
    internal int LastTimeoutTotalAgentInspections => LastTimeoutAgentInspections + LastTimeoutGroupAgentInspections;
    internal float LastTimeoutMissionTime;
    internal void UpdateActiveInteractionTimeouts()
	{
        TimeoutUpdateCalls++;
        LastTimeoutActiveCount = _activeInteractionSessions.Count;
        LastTimeoutWorkItems = LastTimeoutAgentInspections = LastTimeoutGroupAgentInspections = 0;
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
        LastTimeoutAgentCount = agents?.Count ?? 0;
        LastTimeoutMissionTime = mission?.CurrentTime ?? 0f;
		if (mission == null || agents == null || _activeInteractionSessions.Count == 0)
		{
			return;
		}
		float currentTime = mission.CurrentTime;
		List<int> list = null;
		HashSet<int> outOfRangeReleaseAgentIndices = null;
		foreach (KeyValuePair<int, SceneInteractionSession> activeInteractionSession in _activeInteractionSessions)
		{
			LastTimeoutWorkItems++;
			SceneInteractionSession value = activeInteractionSession.Value;
			float num = ((value != null && value.TimeoutSeconds > 0f) ? value.TimeoutSeconds : ACTIVE_INTERACTION_IDLE_TIMEOUT);
			if (value == null)
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(activeInteractionSession.Key);
				continue;
			}
			if (!value.TimeoutArmed)
			{
				continue;
			}
			if (_sceneMovement.HasPendingGuideReturn(activeInteractionSession.Key))
			{
				continue;
			}
			if (_sceneMovement.IsAgentBusyWithSceneSummonErrand(activeInteractionSession.Key) || _sceneMovement.IsAgentBusyWithSceneGuideErrand(activeInteractionSession.Key))
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(activeInteractionSession.Key);
				_pendingInteractionTimeoutArms.Remove(activeInteractionSession.Key);
				continue;
			}
			Agent agent = null;
            // Preserve first matching active Agent and check order without a per-session predicate allocation.
            foreach (Agent candidate in agents)
            {
                LastTimeoutAgentInspections++;
                if (candidate != null && candidate.Index == activeInteractionSession.Key && candidate.IsActive())
                { agent = candidate; break; }
            }
			if (_sceneMovement.IsAgentFollowingPlayerBySceneCommand(agent))
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(activeInteractionSession.Key);
				_pendingInteractionTimeoutArms.Remove(activeInteractionSession.Key);
				continue;
			}
			if (!IsPlayerWithinActiveInteractionRange(agent, value))
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(activeInteractionSession.Key);
				if (outOfRangeReleaseAgentIndices == null)
				{
					outOfRangeReleaseAgentIndices = new HashSet<int>();
				}
				outOfRangeReleaseAgentIndices.Add(activeInteractionSession.Key);
				continue;
			}
			if (currentTime - value.LastActivityTime >= num)
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(activeInteractionSession.Key);
			}
		}
		if (list == null || list.Count == 0)
		{
			return;
		}
		HashSet<int> hashSet = new HashSet<int>();
		List<int> groupedTimeoutCandidateIndices = list;
		if (outOfRangeReleaseAgentIndices != null && outOfRangeReleaseAgentIndices.Count > 0)
		{
			groupedTimeoutCandidateIndices = list.Where(agentIndex => !outOfRangeReleaseAgentIndices.Contains(agentIndex)).ToList();
		}
		foreach (int item in list)
		{
			if (!hashSet.Add(item))
			{
				continue;
			}
			if (_activeInteractionSessions.TryGetValue(item, out var value2))
			{
				if (outOfRangeReleaseAgentIndices != null && outOfRangeReleaseAgentIndices.Contains(item))
				{
					ExpireActiveInteractionSilently(value2, deferSceneSummonReturn: false);
				}
				else
				{
					List<SceneInteractionSession> groupedTimeoutSessions = BuildGroupedExpiredInteractionSessions(value2, groupedTimeoutCandidateIndices);
					if (groupedTimeoutSessions != null && groupedTimeoutSessions.Count > 1)
					{
						foreach (SceneInteractionSession groupedTimeoutSession in groupedTimeoutSessions)
						{
							if (groupedTimeoutSession != null && groupedTimeoutSession.TargetAgentIndex >= 0)
							{
								hashSet.Add(groupedTimeoutSession.TargetAgentIndex);
							}
						}
						ExpireGroupedActiveInteractions(groupedTimeoutSessions);
					}
					else
					{
						ExpireActiveInteraction(value2);
					}
				}
			}
			if (_activeInteractionSessions.TryGetValue(item, out var value3) && (value3 == null || ReferenceEquals(value3, value2) || value3.InteractionToken == value2?.InteractionToken))
			{
				_activeInteractionSessions.Remove(item);
			}
		}
	}

	internal List<SceneInteractionSession> BuildGroupedExpiredInteractionSessions(SceneInteractionSession seedSession, List<int> expiredAgentIndices)
	{
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (seedSession == null || seedSession.TargetAgentIndex < 0 || expiredAgentIndices == null || expiredAgentIndices.Count < 2 || seedSession.TimeoutSeconds <= 3.5f || agents == null)
		{
			return null;
		}
		Agent agent = FindTimeoutReleaseAgent(seedSession.TargetAgentIndex);
		if (agent == null)
		{
			return null;
		}
		List<SceneInteractionSession> list = new List<SceneInteractionSession> { seedSession };
		for (int i = 0; i < expiredAgentIndices.Count; i++)
		{
			int num = expiredAgentIndices[i];
			if (num < 0 || num == seedSession.TargetAgentIndex || !_activeInteractionSessions.TryGetValue(num, out var value) || value == null || value.TimeoutSeconds <= 3.5f)
			{
				continue;
			}
			if (Math.Abs(value.TimeoutSeconds - seedSession.TimeoutSeconds) > 0.5f || Math.Abs(value.LastActivityTime - seedSession.LastActivityTime) > 1.5f)
			{
				continue;
			}
			Agent agent2 = FindTimeoutReleaseAgent(num);
			if (agent2 == null || agent.Position.DistanceSquared(agent2.Position) > 36f)
			{
				continue;
			}
			list.Add(value);
		}
		return (list.Count > 1) ? list : null;
	}

	internal void ExpireGroupedActiveInteractions(List<SceneInteractionSession> sessions)
	{
		Mission mission = Mission.Current;
		if (sessions == null || sessions.Count == 0 || mission == null)
		{
			return;
		}
		List<SceneInteractionSession> list = sessions.Where(s => s != null && s.TargetAgentIndex >= 0).GroupBy(s => s.TargetAgentIndex).Select(group => group.First()).ToList();
		if (list.Count == 0)
		{
			return;
		}
		SceneInteractionSession representativeSession = ChooseGroupedTimeoutRepresentative(list);
		if (representativeSession == null)
		{
			foreach (SceneInteractionSession item in list)
			{
				ExpireActiveInteractionSilently(item, deferSceneSummonReturn: false);
			}
			return;
		}
		SceneSummonConversationSession representativeSummonSession = _sceneMovement.TryGetSceneSummonConversationSessionForAgentIndex(representativeSession.TargetAgentIndex);
		foreach (SceneInteractionSession item2 in list)
		{
			if (item2 == null || item2.TargetAgentIndex < 0 || item2.TargetAgentIndex == representativeSession.TargetAgentIndex)
			{
				continue;
			}
			bool deferSceneSummonReturn = representativeSummonSession != null && _sceneMovement.TryGetSceneSummonConversationSessionForAgentIndex(item2.TargetAgentIndex) == representativeSummonSession;
			ExpireActiveInteractionSilently(item2, deferSceneSummonReturn);
			_activeInteractionSessions.Remove(item2.TargetAgentIndex);
			_pendingInteractionTimeoutArms.Remove(item2.TargetAgentIndex);
		}
		if (!TriggerGroupedTimeoutDisperseSpeech(representativeSession, list, representativeSummonSession))
		{
			ExpireActiveInteractionSilently(representativeSession, deferSceneSummonReturn: false);
		}
		_activeInteractionSessions.Remove(representativeSession.TargetAgentIndex);
		_pendingInteractionTimeoutArms.Remove(representativeSession.TargetAgentIndex);
	}

	internal SceneInteractionSession ChooseGroupedTimeoutRepresentative(List<SceneInteractionSession> sessions)
	{
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (sessions == null || sessions.Count == 0 || agents == null)
		{
			return null;
		}
		Agent main = Agent.Main;
		SceneInteractionSession sceneInteractionSession = null;
		float num = float.MaxValue;
		for (int i = 0; i < sessions.Count; i++)
		{
			SceneInteractionSession sceneInteractionSession2 = sessions[i];
			if (sceneInteractionSession2 == null || sceneInteractionSession2.TargetAgentIndex < 0)
			{
				continue;
			}
			Agent agent = FindTimeoutReleaseAgent(sceneInteractionSession2.TargetAgentIndex);
			if (!CanAgentParticipateInSceneSpeech(agent) || IsAgentHostileToMainAgent(agent))
			{
				continue;
			}
			float num2 = ((main != null && main.IsActive()) ? main.Position.DistanceSquared(agent.Position) : 0f);
			if (sceneInteractionSession == null || num2 < num)
			{
				sceneInteractionSession = sceneInteractionSession2;
				num = num2;
			}
		}
		return sceneInteractionSession ?? sessions.FirstOrDefault();
	}

	internal bool TriggerGroupedTimeoutDisperseSpeech(SceneInteractionSession representativeSession, List<SceneInteractionSession> sessions, SceneSummonConversationSession representativeSummonSession)
	{
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (representativeSession == null || representativeSession.TargetAgentIndex < 0 || agents == null)
		{
			return false;
		}
		Agent agent = FindTimeoutReleaseAgent(representativeSession.TargetAgentIndex);
		if (!CanAgentParticipateInSceneSpeech(agent) || IsAgentHostileToMainAgent(agent))
		{
			return false;
		}
		NpcDataPacket npcDataPacket = ShoutUtils.ExtractNpcData(agent);
		if (npcDataPacket == null)
		{
			return false;
		}
		List<NpcDataPacket> list = new List<NpcDataPacket>();
		if (sessions != null)
		{
			foreach (SceneInteractionSession session in sessions)
			{
				if (session == null || session.TargetAgentIndex < 0)
				{
					continue;
				}
				Agent agent2 = FindTimeoutReleaseAgent(session.TargetAgentIndex);
				NpcDataPacket npcDataPacket2 = ShoutUtils.ExtractNpcData(agent2);
				if (npcDataPacket2 != null && !list.Any(x => x != null && x.AgentIndex == npcDataPacket2.AgentIndex))
				{
					list.Add(npcDataPacket2);
				}
			}
		}
		if (!list.Any(x => x != null && x.AgentIndex == npcDataPacket.AgentIndex))
		{
			list.Insert(0, npcDataPacket);
		}
		Action onNoSpeech = delegate
		{
			_sceneMovement.CancelPendingSummonReturn(npcDataPacket.AgentIndex);
			_ports.CancelAutonomyRestore(npcDataPacket.AgentIndex);
			ExpireActiveInteractionSilently(representativeSession, deferSceneSummonReturn: false);
		};
		if (representativeSummonSession != null)
		{
			_sceneMovement.ClearSceneSummonConversationInteractionTimers(representativeSummonSession);
			_sceneMovement.SetPendingSummonReturn(npcDataPacket.AgentIndex, representativeSummonSession, _sceneMovement.ShouldReturnOnlySceneSummonSpeaker(representativeSummonSession, agent));
		}
		else
		{
			_ports.PrepareAutonomyRestore(npcDataPacket.AgentIndex);
		}
		string factText = "[AFEF NPC行为补充] 玩家长时间未继续发言，会话准备自然结束。";
		TriggerImmediateSceneBehaviorReaction(factText, npcDataPacket.AgentIndex, persistHeroPrivateHistory: true, suppressStare: false, postSpeechLeaveSeconds: 3f, skipSceneFactRecord: false, returnSceneSummonOnTimeout: false, onNoSpeech);
		return true;
	}

	internal void ExpireActiveInteractionSilently(SceneInteractionSession session, bool deferSceneSummonReturn)
	{
		if (session == null || session.TargetAgentIndex < 0)
		{
			return;
		}
		Agent agent = FindTimeoutReleaseAgent(session.TargetAgentIndex);
		SceneSummonConversationSession sceneSummonConversationSession = _sceneMovement.TryGetSceneSummonConversationSessionForAgentIndex(session.TargetAgentIndex);
		if (_sceneMovement.IsAgentBusyWithSceneSummonErrand(session.TargetAgentIndex) || _sceneMovement.IsAgentBusyWithSceneGuideErrand(session.TargetAgentIndex) || _sceneMovement.IsAgentFollowingPlayerBySceneCommand(agent))
		{
			return;
		}
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			RemoveSceneMovementSuppressionAgents(new int[1] { session.TargetAgentIndex });
			if (!deferSceneSummonReturn && sceneSummonConversationSession != null)
			{
				_sceneMovement.BeginSceneSummonConversationReturn(sceneSummonConversationSession);
			}
			return;
		}
		if (ShouldPreserveMeetingSceneAutonomy())
		{
			_ports.ReleaseStareAgent(session.TargetAgentIndex);
			RemoveSceneMovementSuppressionAgents(new int[1] { session.TargetAgentIndex });
			ReleaseAgentFromSceneConversationLocks(agent);
			return;
		}
		_ports.ReleaseStareAgent(session.TargetAgentIndex);
		RemoveSceneMovementSuppressionAgents(new int[1] { session.TargetAgentIndex });
		RestoreAgentAutonomy(agent);
		if (!deferSceneSummonReturn && sceneSummonConversationSession != null)
		{
			_sceneMovement.BeginSceneSummonConversationReturn(sceneSummonConversationSession);
		}
	}

	internal void ExpireActiveInteraction(SceneInteractionSession session)
	{
		if (session == null || session.TargetAgentIndex < 0)
		{
			return;
		}
		Agent agent = FindTimeoutReleaseAgent(session.TargetAgentIndex);
		SceneSummonConversationSession sceneSummonConversationSession = _sceneMovement.TryGetSceneSummonConversationSessionForAgentIndex(session.TargetAgentIndex);
		if (_sceneMovement.IsAgentBusyWithSceneSummonErrand(session.TargetAgentIndex) || _sceneMovement.IsAgentBusyWithSceneGuideErrand(session.TargetAgentIndex))
		{
			_pendingInteractionTimeoutArms.Remove(session.TargetAgentIndex);
			_activeInteractionSessions.Remove(session.TargetAgentIndex);
			return;
		}
		if (_sceneMovement.IsAgentFollowingPlayerBySceneCommand(agent))
		{
			_pendingInteractionTimeoutArms.Remove(session.TargetAgentIndex);
			_activeInteractionSessions.Remove(session.TargetAgentIndex);
			return;
		}
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			RemoveSceneMovementSuppressionAgents(new int[1] { session.TargetAgentIndex });
			if (sceneSummonConversationSession != null)
			{
				_sceneMovement.BeginSceneSummonConversationReturn(sceneSummonConversationSession);
			}
			return;
		}
		if (IsAgentHostileToMainAgent(agent))
		{
			RemoveSceneMovementSuppressionAgents(new int[1] { session.TargetAgentIndex });
			RestoreAgentAutonomy(agent);
			if (sceneSummonConversationSession != null)
			{
				_sceneMovement.BeginSceneSummonConversationReturn(sceneSummonConversationSession);
			}
			return;
		}
		if (ShouldPreserveMeetingSceneAutonomy())
		{
			_ports.ReleaseStareAgent(session.TargetAgentIndex);
			RemoveSceneMovementSuppressionAgents(new int[1] { session.TargetAgentIndex });
			ReleaseAgentFromSceneConversationLocks(agent);
			return;
		}
		string text = (session.TargetName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = (agent.Name?.ToString() ?? "NPC").Trim();
		}
		string text2 = GetPlayerDisplayNameForShout();
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "玩家";
		}
		string factText =  text2 +"长时间没有发言，"+text+"决定去干自己的事了。";
		string prefixedFactText = "[AFEF NPC行为补充] " + factText;
		if (session.TimeoutSeconds > 3.5f)
		{
			Action onNoSpeech = delegate
			{
				_sceneMovement.CancelPendingSummonReturn(session.TargetAgentIndex);
				_ports.CancelAutonomyRestore(session.TargetAgentIndex);
				ExpireActiveInteractionSilently(session, deferSceneSummonReturn: false);
			};
			if (sceneSummonConversationSession != null)
			{
				_sceneMovement.ClearSceneSummonConversationInteractionTimers(sceneSummonConversationSession);
				_sceneMovement.SetPendingSummonReturn(session.TargetAgentIndex, sceneSummonConversationSession, _sceneMovement.ShouldReturnOnlySceneSummonSpeaker(sceneSummonConversationSession, agent));
			}
			TriggerImmediateSceneBehaviorReaction(prefixedFactText, session.TargetAgentIndex, persistHeroPrivateHistory: true, suppressStare: false, postSpeechLeaveSeconds: 3f, skipSceneFactRecord: false, returnSceneSummonOnTimeout: false, onNoSpeech);
			return;
		}
		AppendTargetedSceneNpcFact(prefixedFactText, session.TargetAgentIndex, persistHeroPrivateHistory: true);
		_ports.ReleaseStareAgent(session.TargetAgentIndex);
		RemoveSceneMovementSuppressionAgents(new int[1] { session.TargetAgentIndex });
		RestoreAgentAutonomy(agent);
		if (sceneSummonConversationSession != null)
		{
			_sceneMovement.BeginSceneSummonConversationReturn(sceneSummonConversationSession);
		}
	}

    private Agent FindTimeoutReleaseAgent(int index)
    {
        var agents = Mission.Current?.Agents;
        if (agents == null) return null;
        foreach (Agent candidate in agents)
        {
            LastTimeoutGroupAgentInspections++;
            if (candidate != null && candidate.Index == index && candidate.IsActive()) return candidate;
        }
        return null;
    }

 internal void RemoveHostileSpeechInteraction(int agentIndex)
 {
  _activeInteractionSessions.Remove(agentIndex);
  _pendingInteractionTimeoutArms.Remove(agentIndex);
  _ports.ClearPendingSpeechCompletionTokens(agentIndex);
 }

 internal long CaptureSpeechInteractionToken(int agentIndex)
  => _activeInteractionSessions.TryGetValue(agentIndex, out var session) && session != null ? session.InteractionToken : 0L;

	internal void PrepareInteractionCompletion(int agentIndex)
	{
		long interactionToken = 0L;
		bool hasInteractionToken = false;
		bool hostileFinishedSpeech = false;
		RunSceneTtsPlaybackFinishedStep("resolve_state", agentIndex, delegate
		{
			hasInteractionToken = TryDequeuePendingSpeechCompletionToken(agentIndex, out interactionToken);
			Agent finishedAgent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
			hostileFinishedSpeech = IsAgentHostileToMainAgent(finishedAgent);
			if (hostileFinishedSpeech)
			{
				hasInteractionToken = false;
				interactionToken = 0L;
				_pendingInteractionTimeoutArms.Remove(agentIndex);
				_activeInteractionSessions.Remove(agentIndex);
			}
		});
		RunSceneTtsPlaybackFinishedStep("clear_speaking", agentIndex, delegate
		{
			SceneAudio.MarkPlaybackFinished(agentIndex);
			_ports.UnmarkPlaybackStarted(agentIndex);
			Logger.Log("LipSync", $"[OnPlaybackFinished] agentIndex={agentIndex} finalized on main thread");
			LogTtsReport("PlaybackFinished", agentIndex, $"hasInteractionToken={hasInteractionToken};interactionToken={interactionToken};hostileFinishedSpeech={hostileFinishedSpeech};mainThread=True");
		});
		if (hasInteractionToken)
		{
			RunSceneTtsPlaybackFinishedStep("arm_interaction_timeout", agentIndex, delegate
			{
				ArmActiveInteractionTimeoutNow(agentIndex, interactionToken);
			});
		}
	}

    private bool TryDequeuePendingSpeechCompletionToken(int agentIndex, out long interactionToken) => _ports.TryDequeuePendingSpeechCompletionToken_L2832(agentIndex, out interactionToken);
    private void AppendTargetedSceneNpcFact(string factText, int targetAgentIndex, bool persistHeroPrivateHistory) => _ports.AppendTargetedSceneNpcFact_L18284(factText, targetAgentIndex, persistHeroPrivateHistory);
    private void RemoveSceneMovementSuppressionAgents(IEnumerable<int> participantAgentIndices) => _ports.RemoveSceneMovementSuppressionAgents_L18679(participantAgentIndices);
    private void ReleaseAgentFromSceneConversationLocks(Agent agent) => _ports.ReleaseAgentFromSceneConversationLocks_L19099(agent);
    private void RestoreAgentAutonomy(Agent agent) => _ports.RestoreAgentAutonomy_L20524(agent);
    private SceneAudioLipSyncController SceneAudio { get => _ports.GetSceneAudio(); }
    private void RunSceneTtsPlaybackFinishedStep(string step, int agentIndex, Action action) => _ports.RunSceneTtsPlaybackFinishedStep_L176(step, agentIndex, action);
    private void LogTtsReport(string stage, int agentIndex, string extra = null) => _ports.LogTtsReport_L208(stage, agentIndex, extra);
    private SceneMovementController _sceneMovement { get => _ports.Get_sceneMovement(); }
    private bool TriggerImmediateSceneBehaviorReaction(string factText, int targetAgentIndex, bool persistHeroPrivateHistory, bool suppressStare, float postSpeechLeaveSeconds = -1f, bool skipSceneFactRecord = false, bool returnSceneSummonOnTimeout = false, Action onNoSpeech = null, bool runSiegeReactionPostprocess = false, Func<bool> canStillPublish = null, Action<bool> onCompleted = null) => _ports.TriggerImmediateSceneBehaviorReaction_L147(factText, targetAgentIndex, persistHeroPrivateHistory, suppressStare, postSpeechLeaveSeconds, skipSceneFactRecord, returnSceneSummonOnTimeout, onNoSpeech, runSiegeReactionPostprocess, canStillPublish, onCompleted);
    internal void CancelInteractionTimeoutArm(int index) { _pendingInteractionTimeoutArms.Remove(index); }
    internal void RemoveInteractionSession(int index) { _activeInteractionSessions.Remove(index); }
    internal void ClearInteractionTimeoutArms() { _pendingInteractionTimeoutArms.Clear(); }
    internal void ResetInteractionsForLoadedSave() { _activeInteractionSessions.Clear(); _pendingInteractionTimeoutArms.Clear(); }
    internal bool TryGetInteractionSession(int index, out SceneInteractionSession session) => _activeInteractionSessions.TryGetValue(index, out session);
    internal NativeSpeechInteractionDiagnosticSnapshot CaptureInteractionDiagnostic(int index)
    {
        bool has = _activeInteractionSessions.TryGetValue(index, out var session) && session != null;
        bool arm = _pendingInteractionTimeoutArms.TryGetValue(index, out var pending) && pending != null;
        return new NativeSpeechInteractionDiagnosticSnapshot { HasInteraction = has, InteractionToken = has ? session.InteractionToken : 0L,
            TimeoutArmed = has && session.TimeoutArmed, HasPendingArm = arm, ArmAtMissionTime = arm ? pending.ArmAtMissionTime : 0f };
    }

}

internal sealed class SceneInteractionLifecycleControllerPorts
{
    internal delegate bool TryDequeuePendingSpeechCompletionToken_L2832Callback(int agentIndex, out long interactionToken);
    internal TryDequeuePendingSpeechCompletionToken_L2832Callback TryDequeuePendingSpeechCompletionToken_L2832;
    internal delegate void AppendTargetedSceneNpcFact_L18284Callback(string factText, int targetAgentIndex, bool persistHeroPrivateHistory);
    internal AppendTargetedSceneNpcFact_L18284Callback AppendTargetedSceneNpcFact_L18284;
    internal delegate void RemoveSceneMovementSuppressionAgents_L18679Callback(IEnumerable<int> participantAgentIndices);
    internal RemoveSceneMovementSuppressionAgents_L18679Callback RemoveSceneMovementSuppressionAgents_L18679;
    internal delegate void ReleaseAgentFromSceneConversationLocks_L19099Callback(Agent agent);
    internal ReleaseAgentFromSceneConversationLocks_L19099Callback ReleaseAgentFromSceneConversationLocks_L19099;
    internal delegate void RestoreAgentAutonomy_L20524Callback(Agent agent);
    internal RestoreAgentAutonomy_L20524Callback RestoreAgentAutonomy_L20524;
    internal Func<SceneAudioLipSyncController> GetSceneAudio;
    internal delegate void RunSceneTtsPlaybackFinishedStep_L176Callback(string step, int agentIndex, Action action);
    internal RunSceneTtsPlaybackFinishedStep_L176Callback RunSceneTtsPlaybackFinishedStep_L176;
    internal delegate void LogTtsReport_L208Callback(string stage, int agentIndex, string extra);
    internal LogTtsReport_L208Callback LogTtsReport_L208;
    internal Func<SceneMovementController> Get_sceneMovement;
    internal delegate bool TriggerImmediateSceneBehaviorReaction_L147Callback(string factText, int targetAgentIndex, bool persistHeroPrivateHistory, bool suppressStare, float postSpeechLeaveSeconds, bool skipSceneFactRecord, bool returnSceneSummonOnTimeout, Action onNoSpeech, bool runSiegeReactionPostprocess, Func<bool> canStillPublish, Action<bool> onCompleted);
    internal TriggerImmediateSceneBehaviorReaction_L147Callback TriggerImmediateSceneBehaviorReaction_L147;
    internal Action<int> ReleaseStareAgent;
    internal Action<HashSet<int>> CaptureMovementSuppressionAgents;
    internal Action<int> CancelAutonomyRestore, PrepareAutonomyRestore, ClearPendingSpeechCompletionTokens, UnmarkPlaybackStarted;

}
