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

internal sealed class SceneSpeechFollowupController
{
    private readonly SceneSpeechFollowupControllerPorts _ports;
    internal SceneSpeechFollowupController(SceneSpeechFollowupControllerPorts ports) { _ports = ports ?? throw new ArgumentNullException(nameof(ports)); }

	internal const float LORDS_HALL_ENTRY_TTS_FALLBACK_SECONDS = 15f;

	internal readonly Dictionary<int, PendingMeetingReleaseAfterSpeech> _pendingMeetingReleasesAfterSpeech = new Dictionary<int, PendingMeetingReleaseAfterSpeech>();

	internal readonly Dictionary<int, PendingWorldMapMissionExitAfterSpeech> _pendingWorldMapMissionExitsAfterSpeech = new Dictionary<int, PendingWorldMapMissionExitAfterSpeech>();

	internal PendingLordsHallMissionEntryAfterSpeech _pendingLordsHallMissionEntryAfterSpeech;

	internal bool _pendingLordsHallMissionEntryConversationEndHookRegistered;

	internal readonly Dictionary<int, PendingSceneAutonomyRestoreAfterSpeech> _pendingSceneAutonomyRestoresAfterSpeech = new Dictionary<int, PendingSceneAutonomyRestoreAfterSpeech>();

	internal bool TryCaptureNegotiatedLordsHallBribe(NpcDataPacket npc, Agent agent, ref string content)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(content) || npc == null)
			{
				return false;
			}
			Match match = Regex.Match(content, "\\[ACTION:LORDS_HALL_BRIBE_PRICE:(\\d+)\\]", RegexOptions.IgnoreCase);
			if (!match.Success || !int.TryParse(match.Groups[1].Value, out var result) || result <= 0)
			{
				return false;
			}
			Agent agent2 = agent ?? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == npc.AgentIndex);
			CharacterObject characterObject = agent2?.Character as CharacterObject;
			string text = MyBehavior.BuildRuleTargetKeyForExternal(null, characterObject, npc.AgentIndex);
			if (!string.IsNullOrWhiteSpace(text))
			{
				RecordNegotiatedNonHeroBribe(text, result);
			}
			content = Regex.Replace(content, "\\[ACTION:LORDS_HALL_BRIBE_PRICE:(\\d+)\\]", "", RegexOptions.IgnoreCase).Trim();
			return true;
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsLordsHallGuardAgent(Agent agent)
	{
		try
		{
			if (agent == null || !(agent.Character is CharacterObject characterObject) || !characterObject.IsSoldier)
			{
				return false;
			}
			AgentNavigator agentNavigator = agent.GetComponent<CampaignAgentComponent>()?.AgentNavigator;
			bool flag = false;
			if (agentNavigator != null)
			{
				flag = agentNavigator.TargetUsableMachine != null && agent.IsUsingGameObject && agentNavigator.TargetUsableMachine.GameEntity.HasTag("sp_guard_castle");
				if (!flag && (agentNavigator.SpecialTargetTag == "sp_guard_castle" || agentNavigator.SpecialTargetTag == "sp_guard"))
				{
					Location lordsHallLocation = LocationComplex.Current?.GetLocationWithId("lordshall");
					MissionAgentHandler missionBehavior = Mission.Current?.GetMissionBehavior<MissionAgentHandler>();
					if (lordsHallLocation != null && missionBehavior?.TownPassageProps != null)
					{
						UsableMachine usableMachine = missionBehavior.TownPassageProps.FirstOrDefault((UsableMachine x) => x is Passage passage && passage.ToLocation == lordsHallLocation);
						if (usableMachine != null && usableMachine.GameEntity.GlobalPosition.DistanceSquared(agent.Position) < 100f)
						{
							flag = true;
						}
					}
				}
			}
			return flag;
		}
		catch
		{
			return false;
		}
	}

	internal bool TryUnlockLordsHallForNpc(NpcDataPacket npc, Agent agent)
	{
		try
		{
			Settlement settlement = Settlement.CurrentSettlement;
			Mission mission = Mission.Current;
			LocationComplex locationComplex = LocationComplex.Current;
			Location currentLocation = CampaignMission.Current?.Location;
			if (settlement == null || !settlement.IsTown || mission == null || locationComplex == null || currentLocation == null)
			{
				Logger.Log("ShoutBehavior", "[LordsHallAccess] ignored OPEN_LORDS_HALL outside an active town-center scene. speaker=" + (npc?.Name ?? agent?.Name?.ToString() ?? "unknown"));
				return false;
			}
			if (!string.Equals(currentLocation.StringId, "center", StringComparison.OrdinalIgnoreCase))
			{
				Logger.Log("ShoutBehavior", "[LordsHallAccess] ignored OPEN_LORDS_HALL outside town center. location=" + (currentLocation.StringId ?? "") + " speaker=" + (npc?.Name ?? agent?.Name?.ToString() ?? "unknown"));
				return false;
			}
			Location lordsHall = locationComplex.GetLocationWithId("lordshall");
			Location center = locationComplex.GetLocationWithId("center");
			if (lordsHall == null || center == null || Campaign.Current?.GameMenuManager == null)
			{
				Logger.Log("ShoutBehavior", "[LordsHallAccess] ignored OPEN_LORDS_HALL because the location transition is unavailable. settlement=" + (settlement.StringId ?? "") + " hasHall=" + (lordsHall != null) + " hasCenter=" + (center != null));
				return false;
			}
			Logger.Log("ShoutBehavior", "[LordsHallAccess] accepted OPEN_LORDS_HALL. settlement=" + (settlement.StringId ?? "") + " speaker=" + (npc?.Name ?? agent?.Name?.ToString() ?? "unknown"));
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[LordsHallAccess] OPEN_LORDS_HALL validation failed: " + ex.Message);
		}
		return false;
	}

	internal bool TryTriggerOpenLordsHallAction(NpcDataPacket npc, Agent agent, ref string content)
	{
		try
		{
			if (!ContainsOpenLordsHallActionTag(content))
			{
				return false;
			}
			content = StripLordsHallAccessActionTagsForScene(content);
			bool result = TryUnlockLordsHallForNpc(npc, agent);
			Logger.Log("ShoutBehavior", "[LordsHallAccess] OPEN_LORDS_HALL parsed handled=" + result + " speaker=" + (npc?.Name ?? agent?.Name?.ToString() ?? "unknown"));
			return result;
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[LordsHallAccess] OPEN_LORDS_HALL parse failed: " + ex.Message);
			return false;
		}
	}

	internal void ScheduleMeetingReleaseAfterSpeech(int agentIndex, Hero targetHero, SceneSpeechPlaybackInfo playbackInfo)
	{
		Mission mission = Mission.Current;
		if (agentIndex < 0 || mission == null)
		{
			return;
		}
		float num = Math.Max(0.25f, playbackInfo?.VisualDurationSeconds ?? 0f);
		_pendingMeetingReleasesAfterSpeech[agentIndex] = new PendingMeetingReleaseAfterSpeech
		{
			AgentIndex = agentIndex,
			TargetHero = targetHero,
			EncounterParty = LordEncounterBehavior.CaptureMeetingReleaseEncounterPartyForExternal(),
			WaitForPlaybackFinished = playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished,
			ExecuteAtMissionTime = ((playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished) ? (-1f) : (mission.CurrentTime + num))
		};
	}

	internal void FlushMeetingReleaseAfterSpeech(int agentIndex)
	{
		if (agentIndex < 0 || !_pendingMeetingReleasesAfterSpeech.TryGetValue(agentIndex, out var value) || value == null)
		{
			return;
		}
		_pendingMeetingReleasesAfterSpeech.Remove(agentIndex);
		LordEncounterBehavior.TryExecuteMeetingPlayerRelease(value.TargetHero, value.EncounterParty, "meeting_release_player_after_speech");
	}

	internal void ShowOpenLordsHallResponseAndScheduleEntry(NpcDataPacket npc, Agent agent, List<NpcDataPacket> allNpcData, string content, bool commitHistory, string afterSpeechInfoMessage)
	{
		int agentIndex = npc?.AgentIndex ?? agent?.Index ?? -1;
		string visibleContent = StripActionTagsForSceneSpeech(content);
		string historyText = SanitizeSceneSpeechText(visibleContent);
		string fullHistoryText = PrepareSceneHistorySpeechText(visibleContent);
		SceneSpeechPlaybackInfo playbackInfo = null;
		if (npc != null && CanAgentParticipateInSceneSpeech(agent) && !string.IsNullOrWhiteSpace(historyText))
		{
			playbackInfo = ShowNpcSpeechOutput(npc, agent, historyText, allowTts: true, attachTtsToSceneAgent: true, suppressInteractionTimeoutArm: true);
			if (commitHistory && !string.IsNullOrWhiteSpace(fullHistoryText))
			{
				RecordResponseForAllNearbySafe(allNpcData, npc.AgentIndex, npc.Name, fullHistoryText);
				PersistNpcSpeechToNamedHeroes(npc.AgentIndex, npc.Name, fullHistoryText, allNpcData);
			}
		}
		if (!string.IsNullOrWhiteSpace(afterSpeechInfoMessage))
		{
			try
			{
				InformationManager.DisplayMessage(new InformationMessage(afterSpeechInfoMessage, new Color(1f, 0.95f, 0.25f)));
			}
			catch
			{
			}
		}
		ScheduleLordsHallMissionEntryAfterSpeech(agentIndex, playbackInfo, "scene_tag_priority");
	}

	internal void ScheduleLordsHallMissionEntryAfterSpeech(int agentIndex, SceneSpeechPlaybackInfo playbackInfo, string reason, bool waitForConversationEnd = false)
	{
		Mission mission = Mission.Current;
		Settlement settlement = Settlement.CurrentSettlement;
		if (mission == null || settlement == null || !settlement.IsTown)
		{
			Logger.Log("ShoutBehavior", "[LordsHallAccess] entry schedule skipped because the town mission is no longer active. reason=" + (reason ?? ""));
			return;
		}
		float delay = Math.Max(0.25f, playbackInfo?.VisualDurationSeconds ?? 0f);
		bool waitForNativeConversation = waitForConversationEnd && IsNativeConversationActiveForLordsHallEntry();
		bool waitForPlayback = !waitForNativeConversation && agentIndex >= 0 && playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished;
		float fallbackDelay = waitForPlayback ? Math.Max(LORDS_HALL_ENTRY_TTS_FALLBACK_SECONDS, delay + 3f) : delay;
		UnregisterPendingLordsHallMissionEntryConversationEndHook();
		_pendingLordsHallMissionEntryAfterSpeech = new PendingLordsHallMissionEntryAfterSpeech
		{
			Mission = mission,
			AgentIndex = agentIndex,
			SettlementId = settlement.StringId ?? "",
			Reason = reason ?? "",
			WaitForConversationEnd = waitForNativeConversation,
			WaitForPlaybackFinished = waitForPlayback,
			ExecuteAtMissionTime = mission.CurrentTime + fallbackDelay
		};
		if (waitForNativeConversation)
		{
			RegisterPendingLordsHallMissionEntryConversationEndHook();
		}
		Logger.Log("ShoutBehavior", "[LordsHallAccess] entry scheduled after speech. agent=" + agentIndex + " settlement=" + (settlement.StringId ?? "") + " waitForConversationEnd=" + waitForNativeConversation + " waitForPlayback=" + waitForPlayback + " delay=" + delay.ToString("F2") + " fallback=" + fallbackDelay.ToString("F2") + " reason=" + (reason ?? ""));
	}

	internal static bool IsNativeConversationActiveForLordsHallEntry()
	{
		try
		{
			ConversationManager conversationManager = Campaign.Current?.ConversationManager;
			return conversationManager != null && (conversationManager.IsConversationInProgress || conversationManager.IsConversationFlowActive);
		}
		catch
		{
			return false;
		}
	}

	internal void RegisterPendingLordsHallMissionEntryConversationEndHook()
	{
		try
		{
			ConversationManager conversationManager = Campaign.Current?.ConversationManager;
			if (conversationManager == null)
			{
				return;
			}
			conversationManager.ConversationEndOneShot -= OnPendingLordsHallMissionEntryConversationEnded;
			conversationManager.ConversationEndOneShot += OnPendingLordsHallMissionEntryConversationEnded;
			_pendingLordsHallMissionEntryConversationEndHookRegistered = true;
			Logger.Log("ShoutBehavior", "[LordsHallAccess] registered native conversation-end transition hook.");
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[LordsHallAccess] failed to register native conversation-end transition hook: " + ex.Message);
		}
	}

	internal void UnregisterPendingLordsHallMissionEntryConversationEndHook()
	{
		if (!_pendingLordsHallMissionEntryConversationEndHookRegistered)
		{
			return;
		}
		try
		{
			ConversationManager conversationManager = Campaign.Current?.ConversationManager;
			if (conversationManager != null)
			{
				conversationManager.ConversationEndOneShot -= OnPendingLordsHallMissionEntryConversationEnded;
			}
		}
		catch
		{
		}
		_pendingLordsHallMissionEntryConversationEndHookRegistered = false;
	}

	internal void OnPendingLordsHallMissionEntryConversationEnded()
	{
		_pendingLordsHallMissionEntryConversationEndHookRegistered = false;
		PendingLordsHallMissionEntryAfterSpeech pending = _pendingLordsHallMissionEntryAfterSpeech;
		if (pending == null || !pending.WaitForConversationEnd)
		{
			return;
		}
		Logger.Log("ShoutBehavior", "[LordsHallAccess] native conversation ended; entering lordshall.");
		ExecutePendingLordsHallMissionEntry(pending);
	}

	internal bool FlushLordsHallMissionEntryAfterSpeech(int agentIndex)
	{
		PendingLordsHallMissionEntryAfterSpeech pending = _pendingLordsHallMissionEntryAfterSpeech;
		if (pending == null || pending.WaitForConversationEnd || !pending.WaitForPlaybackFinished || pending.AgentIndex != agentIndex)
		{
			return false;
		}
		ExecutePendingLordsHallMissionEntry(pending);
		return true;
	}

	internal void ExecutePendingLordsHallMissionEntry(PendingLordsHallMissionEntryAfterSpeech pending)
	{
		if (pending == null || !ReferenceEquals(_pendingLordsHallMissionEntryAfterSpeech, pending))
		{
			return;
		}
		_pendingLordsHallMissionEntryAfterSpeech = null;
		UnregisterPendingLordsHallMissionEntryConversationEndHook();
		try
		{
			Mission mission = Mission.Current;
			Settlement settlement = Settlement.CurrentSettlement;
			LocationComplex locationComplex = LocationComplex.Current;
			Location currentLocation = CampaignMission.Current?.Location;
			if (mission == null || !ReferenceEquals(mission, pending.Mission) || settlement == null || !settlement.IsTown || locationComplex == null || currentLocation == null)
			{
				Logger.Log("ShoutBehavior", "[LordsHallAccess] entry cancelled because the scene changed. reason=" + (pending.Reason ?? ""));
				return;
			}
			if (!string.Equals(settlement.StringId ?? "", pending.SettlementId ?? "", StringComparison.OrdinalIgnoreCase)
				|| !string.Equals(currentLocation.StringId, "center", StringComparison.OrdinalIgnoreCase))
			{
				Logger.Log("ShoutBehavior", "[LordsHallAccess] entry cancelled because the settlement or source location changed. expectedSettlement=" + (pending.SettlementId ?? "") + " actualSettlement=" + (settlement.StringId ?? "") + " location=" + (currentLocation.StringId ?? ""));
				return;
			}
			Location lordsHall = locationComplex.GetLocationWithId("lordshall");
			Location center = locationComplex.GetLocationWithId("center");
			if (lordsHall == null || center == null || Campaign.Current?.GameMenuManager == null)
			{
				Logger.Log("ShoutBehavior", "[LordsHallAccess] entry cancelled because the lordshall transition is unavailable. settlement=" + (settlement.StringId ?? ""));
				return;
			}
			Campaign.Current.GameMenuManager.NextLocation = lordsHall;
			Campaign.Current.GameMenuManager.PreviousLocation = center;
			mission.EndMission();
			Logger.Log("ShoutBehavior", "[LordsHallAccess] entered lordshall from OPEN_LORDS_HALL. settlement=" + (settlement.StringId ?? "") + " agent=" + pending.AgentIndex + " reason=" + (pending.Reason ?? ""));
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[LordsHallAccess] entry transition failed: " + ex.Message);
		}
	}

	internal void ScheduleWorldMapMissionExitAfterSpeech(int agentIndex, SceneSpeechPlaybackInfo playbackInfo)
	{
		Mission mission = Mission.Current;
		if (agentIndex < 0 || mission == null)
		{
			return;
		}
		float delay = Math.Max(0.25f, playbackInfo?.VisualDurationSeconds ?? 0f);
		bool waitForPlayback = playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished;
		_pendingWorldMapMissionExitsAfterSpeech[agentIndex] = new PendingWorldMapMissionExitAfterSpeech
		{
			AgentIndex = agentIndex,
			WaitForPlaybackFinished = waitForPlayback,
			ExecuteAtMissionTime = waitForPlayback ? -1f : mission.CurrentTime + delay
		};
		Logger.Log("ShoutBehavior", "[WorldMapCommand] scheduled mission exit after speech agent=" + agentIndex + " waitForPlayback=" + waitForPlayback + " delay=" + delay.ToString("F2"));
	}

	internal void FlushWorldMapMissionExitAfterSpeech(int agentIndex)
	{
		if (agentIndex < 0 || !_pendingWorldMapMissionExitsAfterSpeech.Remove(agentIndex))
		{
			return;
		}
		try
		{
			Mission mission = Mission.Current;
			if (mission != null)
			{
				mission.NextCheckTimeEndMission = 0f;
				mission.EndMission();
				Logger.Log("ShoutBehavior", "[WorldMapCommand] ended mission for implicit party creation agent=" + agentIndex);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[WorldMapCommand] mission exit failed agent=" + agentIndex + " error=" + ex.Message);
		}
	}

	internal void ScheduleSceneAutonomyRestoreAfterSpeech(int agentIndex, SceneSpeechPlaybackInfo playbackInfo)
	{
		Mission mission = Mission.Current;
		if (agentIndex < 0 || mission == null || !_pendingSceneAutonomyRestoresAfterSpeech.TryGetValue(agentIndex, out var value) || value == null)
		{
			return;
		}
		float num = Math.Max(0.25f, playbackInfo?.VisualDurationSeconds ?? 0f);
		value.WaitForPlaybackFinished = playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished;
		value.ExecuteAtMissionTime = value.WaitForPlaybackFinished ? (-1f) : (mission.CurrentTime + num);
	}

	internal void FlushSceneAutonomyRestoreAfterSpeech(int agentIndex)
	{
		if (agentIndex < 0 || !_pendingSceneAutonomyRestoresAfterSpeech.TryGetValue(agentIndex, out var value) || value == null)
		{
			return;
		}
		_pendingSceneAutonomyRestoresAfterSpeech.Remove(agentIndex);
		Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex && a.IsActive());
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			return;
		}
		_ports.ReleaseStareAgent(agentIndex);
		RemoveSceneMovementSuppressionAgents(new int[1] { agentIndex });
		RestoreAgentAutonomy(agent);
	}

	internal void UpdatePendingSceneAutonomyRestoresAfterSpeech()
	{
		Mission mission = Mission.Current;
		if (mission == null || _pendingSceneAutonomyRestoresAfterSpeech.Count == 0)
		{
			return;
		}
		float currentTime = mission.CurrentTime;
		List<int> list = null;
		foreach (KeyValuePair<int, PendingSceneAutonomyRestoreAfterSpeech> pendingSceneAutonomyRestoresAfterSpeech in _pendingSceneAutonomyRestoresAfterSpeech)
		{
			PendingSceneAutonomyRestoreAfterSpeech value = pendingSceneAutonomyRestoresAfterSpeech.Value;
			if (value != null && !value.WaitForPlaybackFinished && value.ExecuteAtMissionTime >= 0f && currentTime >= value.ExecuteAtMissionTime)
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(pendingSceneAutonomyRestoresAfterSpeech.Key);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (int item in list)
		{
			FlushSceneAutonomyRestoreAfterSpeech(item);
		}
	}

	internal void UpdatePendingMeetingReleasesAfterSpeech()
	{
		Mission mission = Mission.Current;
		if (mission == null || _pendingMeetingReleasesAfterSpeech.Count == 0)
		{
			return;
		}
		float currentTime = mission.CurrentTime;
		List<int> list = null;
		foreach (KeyValuePair<int, PendingMeetingReleaseAfterSpeech> pendingMeetingReleaseAfterSpeech in _pendingMeetingReleasesAfterSpeech)
		{
			PendingMeetingReleaseAfterSpeech value = pendingMeetingReleaseAfterSpeech.Value;
			if (value != null && !value.WaitForPlaybackFinished && value.ExecuteAtMissionTime >= 0f && currentTime >= value.ExecuteAtMissionTime)
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(pendingMeetingReleaseAfterSpeech.Key);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (int item in list)
		{
			FlushMeetingReleaseAfterSpeech(item);
		}
	}

	internal void UpdatePendingLordsHallMissionEntryAfterSpeech()
	{
		PendingLordsHallMissionEntryAfterSpeech pending = _pendingLordsHallMissionEntryAfterSpeech;
		if (pending == null)
		{
			return;
		}
		Mission mission = Mission.Current;
		if (mission == null || !ReferenceEquals(mission, pending.Mission))
		{
			_pendingLordsHallMissionEntryAfterSpeech = null;
			UnregisterPendingLordsHallMissionEntryConversationEndHook();
			Logger.Log("ShoutBehavior", "[LordsHallAccess] cleared stale pending entry because the mission changed.");
			return;
		}
		if (pending.WaitForConversationEnd)
		{
			if (IsNativeConversationActiveForLordsHallEntry())
			{
				return;
			}
			Logger.Log("ShoutBehavior", "[LordsHallAccess] native conversation-end hook fallback entering lordshall.");
			ExecutePendingLordsHallMissionEntry(pending);
			return;
		}
		if (pending.ExecuteAtMissionTime >= 0f && mission.CurrentTime >= pending.ExecuteAtMissionTime)
		{
			if (pending.WaitForPlaybackFinished)
			{
				Logger.Log("ShoutBehavior", "[LordsHallAccess] TTS completion fallback entering lordshall. agent=" + pending.AgentIndex);
			}
			ExecutePendingLordsHallMissionEntry(pending);
		}
	}

	internal void UpdatePendingWorldMapMissionExitsAfterSpeech()
	{
		Mission mission = Mission.Current;
		if (mission == null || _pendingWorldMapMissionExitsAfterSpeech.Count == 0)
		{
			return;
		}
		float currentTime = mission.CurrentTime;
		List<int> ready = _pendingWorldMapMissionExitsAfterSpeech
			.Where(pair => pair.Value != null && !pair.Value.WaitForPlaybackFinished && pair.Value.ExecuteAtMissionTime >= 0f && currentTime >= pair.Value.ExecuteAtMissionTime)
			.Select(pair => pair.Key)
			.ToList();
		foreach (int agentIndex in ready)
		{
			FlushWorldMapMissionExitAfterSpeech(agentIndex);
		}
	}

	internal void RunSceneTtsPlaybackFinishedStep(string step, int agentIndex, Action action)
	{
		if (action == null)
		{
			return;
		}
		string safeStep = string.IsNullOrWhiteSpace(step) ? "step" : step.Trim();
		string markName = "SceneTts.playback_finished." + safeStep;
		Stopwatch stopwatch = Stopwatch.StartNew();
		FreezeWatchdog.Mark(markName + ".begin", "agent=" + agentIndex, immediate: true);
		try
		{
			action();
		}
		catch (Exception ex)
		{
			FreezeWatchdog.Mark(markName + ".exception", ex.GetType().Name + ": " + ex.Message, immediate: true);
			Logger.Log("LipSync", "[ERROR] playback_finished step failed step=" + safeStep + " agent=" + agentIndex + " error=" + ex.Message);
			BannerlordExceptionSentinel.ReportObservedException("LipSync.PlaybackFinished." + safeStep, ex, "agentIndex=" + agentIndex);
		}
		finally
		{
			stopwatch.Stop();
			double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
			FreezeWatchdog.Mark(markName + ".end", "agent=" + agentIndex + " elapsedMs=" + Math.Round(elapsedMs, 2), immediate: true);
			if (elapsedMs >= ConversationMainThreadActionDrain.SceneMainThreadActionSlowMs)
			{
				Logger.Log("LipSync", "[WARN] playback_finished slow step step=" + safeStep + " agent=" + agentIndex + " elapsedMs=" + Math.Round(elapsedMs, 2));
			}
		}
	}

    private SceneSpeechPlaybackInfo ShowNpcSpeechOutput(NpcDataPacket npc, Agent liveAgent, string content, bool allowTts = true, bool attachTtsToSceneAgent = true, bool suppressInteractionTimeoutArm = false) => _ports.ShowNpcSpeechOutput_L2959(npc, liveAgent, content, allowTts, attachTtsToSceneAgent, suppressInteractionTimeoutArm);
    private void RecordNegotiatedNonHeroBribe(string targetKey, int goldAmount) => _ports.RecordNegotiatedNonHeroBribe_L15018(targetKey, goldAmount);
    private bool RecordResponseForAllNearbySafe(List<NpcDataPacket> nearbyData, int speakerAgentIndex, string speakerName, string response, bool requireMemoryReceipt = false) => _ports.RecordResponseForAllNearbySafe_L17560(nearbyData, speakerAgentIndex, speakerName, response, requireMemoryReceipt);
    private void RemoveSceneMovementSuppressionAgents(IEnumerable<int> participantAgentIndices) => _ports.RemoveSceneMovementSuppressionAgents_L18679(participantAgentIndices);
    private void RestoreAgentAutonomy(Agent agent) => _ports.RestoreAgentAutonomy_L20524(agent);
    private bool PersistNpcSpeechToNamedHeroes(int speakerAgentIndex, string speakerName, string response, List<NpcDataPacket> nearbyData, bool requireMemoryReceipt = false) => _ports.PersistNpcSpeechToNamedHeroes_L21118(speakerAgentIndex, speakerName, response, nearbyData, requireMemoryReceipt);
    internal void CancelAutonomyRestore(int index) { _pendingSceneAutonomyRestoresAfterSpeech.Remove(index); }
    internal void PrepareAutonomyRestore(int index)
    { _pendingSceneAutonomyRestoresAfterSpeech[index] = new PendingSceneAutonomyRestoreAfterSpeech { AgentIndex = index }; }
    internal void ClearWorldMapExitsForLoadedSave() { _pendingWorldMapMissionExitsAfterSpeech.Clear(); }

}

internal sealed class SceneSpeechFollowupControllerPorts
{
    internal delegate SceneSpeechPlaybackInfo ShowNpcSpeechOutput_L2959Callback(NpcDataPacket npc, Agent liveAgent, string content, bool allowTts, bool attachTtsToSceneAgent, bool suppressInteractionTimeoutArm);
    internal ShowNpcSpeechOutput_L2959Callback ShowNpcSpeechOutput_L2959;
    internal delegate void RecordNegotiatedNonHeroBribe_L15018Callback(string targetKey, int goldAmount);
    internal RecordNegotiatedNonHeroBribe_L15018Callback RecordNegotiatedNonHeroBribe_L15018;
    internal delegate bool RecordResponseForAllNearbySafe_L17560Callback(List<NpcDataPacket> nearbyData, int speakerAgentIndex, string speakerName, string response, bool requireMemoryReceipt);
    internal RecordResponseForAllNearbySafe_L17560Callback RecordResponseForAllNearbySafe_L17560;
    internal delegate void RemoveSceneMovementSuppressionAgents_L18679Callback(IEnumerable<int> participantAgentIndices);
    internal RemoveSceneMovementSuppressionAgents_L18679Callback RemoveSceneMovementSuppressionAgents_L18679;
    internal delegate void RestoreAgentAutonomy_L20524Callback(Agent agent);
    internal RestoreAgentAutonomy_L20524Callback RestoreAgentAutonomy_L20524;
    internal delegate bool PersistNpcSpeechToNamedHeroes_L21118Callback(int speakerAgentIndex, string speakerName, string response, List<NpcDataPacket> nearbyData, bool requireMemoryReceipt);
    internal PersistNpcSpeechToNamedHeroes_L21118Callback PersistNpcSpeechToNamedHeroes_L21118;
    internal Action<int> ReleaseStareAgent;
}
