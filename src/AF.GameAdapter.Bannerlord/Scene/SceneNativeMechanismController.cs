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

internal sealed class SceneNativeMechanismController
{
    private readonly SceneNativeMechanismControllerPorts _ports;
    internal SceneNativeMechanismController(SceneNativeMechanismControllerPorts ports) { _ports = ports ?? throw new ArgumentNullException(nameof(ports)); }

	internal readonly object _pendingNativeSceneMechanismActionLock = new object();

	internal readonly Queue<PendingNativeSceneMechanismAction> _pendingNativeSceneMechanismActions = new Queue<PendingNativeSceneMechanismAction>();

	internal const float NativeSceneTauntFightDelaySeconds = 10f;

	internal readonly object _pendingNativeSceneTauntFightLock = new object();

	internal PendingNativeSceneTauntFight _pendingNativeSceneTauntFight;

	internal bool TryQueueNativeSceneMechanismActionAfterConversationExit(NpcDataPacket npc, List<NpcDataPacket> allNpcData, List<SceneSummonPromptTarget> sceneSummonTargets, List<SceneGuidePromptTarget> sceneGuideTargets, ref string content)
	{
		if (ContainsOpenLordsHallActionTag(content))
		{
			// OPEN_LORDS_HALL is an immediate scene transition, never an escort follow-up.
			return false;
		}
		string text = ExtractSceneMechanismActionTagsForScene(content);
		if (npc == null || string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		content = StripSceneMechanismActionTagsForScene(content);
		PendingNativeSceneMechanismAction pending = new PendingNativeSceneMechanismAction
		{
			Speaker = CloneNpcDataPacket(npc),
			Context = CloneNpcDataSnapshot(allNpcData),
			SummonTargets = SceneMovementController.CloneSceneSummonPromptTargets(sceneSummonTargets),
			GuideTargets = SceneMovementController.CloneSceneGuidePromptTargets(sceneGuideTargets),
			Tags = text,
			CreatedUtcTicks = DateTime.UtcNow.Ticks
		};
		int pendingCount;
		lock (_pendingNativeSceneMechanismActionLock)
		{
			_pendingNativeSceneMechanismActions.Enqueue(pending);
			pendingCount = _pendingNativeSceneMechanismActions.Count;
		}
		ShowNativeSceneMechanismPendingExitPrompt(pendingCount);
		Logger.Log("ShoutBehavior", "[NativeConversation] deferred scene mechanism action until manual conversation exit agent=" + pending.Speaker.AgentIndex + " pending=" + pendingCount + " tags=" + text.Replace("\r", "\\r").Replace("\n", "\\n"));
		return true;
	}

	internal static void ShowNativeSceneMechanismPendingExitPrompt(int pendingCount)
	{
		try
		{
			string suffix = pendingCount > 1 ? (" 当前待执行 " + pendingCount + " 个。") : "";
			InformationManager.DisplayMessage(new InformationMessage("SCENE_MOVE已准备。请手动退出对话框，退出后将执行NPC场景动作。" + suffix, new Color(1f, 0.95f, 0.25f)));
		}
		catch
		{
		}
	}

	internal void ExecutePendingNativeSceneMechanismActionsAfterConversationExit(string reason)
	{
		List<PendingNativeSceneMechanismAction> pendingActions = new List<PendingNativeSceneMechanismAction>();
		lock (_pendingNativeSceneMechanismActionLock)
		{
			while (_pendingNativeSceneMechanismActions.Count > 0)
			{
				pendingActions.Add(_pendingNativeSceneMechanismActions.Dequeue());
			}
		}
		if (pendingActions.Count == 0)
		{
			return;
		}
		foreach (PendingNativeSceneMechanismAction pending in pendingActions)
		{
			_mainThreadActions.Enqueue(delegate
			{
				ExecutePendingNativeSceneMechanismAction(pending, reason);
			});
		}
		Logger.Log("ShoutBehavior", "[NativeConversation] queued deferred scene mechanism actions after manual exit count=" + pendingActions.Count + " reason=" + (reason ?? ""));
		TryDrainNativeConversationQueuedActions("scene_mechanism_after_conversation_exit");
	}

	internal void ExecutePendingNativeSceneMechanismAction(PendingNativeSceneMechanismAction pending, string reason)
	{
		if (pending == null || pending.Speaker == null || string.IsNullOrWhiteSpace(pending.Tags))
		{
			return;
		}
		try
		{
			if (TryExecuteNativeSceneMechanismActionTagsDirectly(pending.Speaker, pending.SummonTargets, pending.GuideTargets, pending.Tags))
			{
				Logger.Log("ShoutBehavior", "[NativeConversation] executed deferred scene mechanism action agent=" + pending.Speaker.AgentIndex + " reason=" + (reason ?? "") + " tags=" + pending.Tags.Replace("\r", "\\r").Replace("\n", "\\n"));
				return;
			}
			EnqueueSpeechLineWithOptions(pending.Speaker, pending.Tags, pending.Context, commitHistory: false, suppressStare: true, allowPlayerDirectedActions: true, requiredConversationEpoch: 0, pending.SummonTargets, pending.GuideTargets, null);
			Logger.Log("ShoutBehavior", "[NativeConversation] queued deferred scene mechanism action fallback agent=" + pending.Speaker.AgentIndex + " reason=" + (reason ?? "") + " tags=" + pending.Tags.Replace("\r", "\\r").Replace("\n", "\\n"));
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] execute deferred scene mechanism action failed: " + ex.Message);
		}
	}

	internal void ClearPendingNativeSceneMechanismActions(string reason)
	{
		try
		{
			int count;
			lock (_pendingNativeSceneMechanismActionLock)
			{
				count = _pendingNativeSceneMechanismActions.Count;
				_pendingNativeSceneMechanismActions.Clear();
			}
			if (count > 0)
			{
				Logger.Log("ShoutBehavior", "[NativeConversation] cleared pending scene mechanism actions count=" + count + " reason=" + (reason ?? ""));
			}
		}
		catch
		{
		}
	}

	internal bool ScheduleNativeSceneTauntFightAfterDelay(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string targetKey, string reason)
	{
		try
		{
			Mission mission = Mission.Current;
			if (mission == null)
			{
				return false;
			}
			string targetName = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "NPC").Trim();
			if (string.IsNullOrWhiteSpace(targetName))
			{
				targetName = "NPC";
			}
			PendingNativeSceneTauntFight pending = new PendingNativeSceneTauntFight
			{
				TargetHero = targetHero,
				TargetCharacter = targetCharacter,
				TargetAgentIndex = targetAgentIndex,
				TargetKey = targetKey,
				TargetName = targetName,
				ExecuteAtMissionTime = mission.CurrentTime + NativeSceneTauntFightDelaySeconds,
				CreatedUtcTicks = DateTime.UtcNow.Ticks
			};
			lock (_pendingNativeSceneTauntFightLock)
			{
				_pendingNativeSceneTauntFight = pending;
			}
			try
			{
				InformationManager.DisplayMessage(new InformationMessage("SCENE_TAUNT_FIGHT已触发：10秒后将自动退出对话并爆发冲突。", new Color(1f, 0.55f, 0.25f)));
			}
			catch
			{
			}
			Logger.Log("ShoutBehavior", "[NativeConversation] delayed scene taunt fight scheduled target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? targetName) + " agentIndex=" + targetAgentIndex + " executeAt=" + pending.ExecuteAtMissionTime.ToString("F2") + " reason=" + (reason ?? ""));
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] schedule delayed scene taunt fight failed: " + ex.Message);
			return false;
		}
	}

	internal void UpdatePendingNativeSceneTauntFightDelay()
	{
		PendingNativeSceneTauntFight pending = null;
		try
		{
			Mission mission = Mission.Current;
			if (mission == null)
			{
				return;
			}
			lock (_pendingNativeSceneTauntFightLock)
			{
				if (_pendingNativeSceneTauntFight == null)
				{
					return;
				}
				double elapsedRealSeconds = new TimeSpan(Math.Max(0L, DateTime.UtcNow.Ticks - _pendingNativeSceneTauntFight.CreatedUtcTicks)).TotalSeconds;
				bool missionTimeElapsed = mission.CurrentTime >= _pendingNativeSceneTauntFight.ExecuteAtMissionTime;
				bool realTimeElapsed = elapsedRealSeconds >= NativeSceneTauntFightDelaySeconds;
				if (!missionTimeElapsed && !realTimeElapsed)
				{
					return;
				}
				pending = _pendingNativeSceneTauntFight;
				_pendingNativeSceneTauntFight = null;
			}
			ExecuteDelayedNativeSceneTauntFight(pending);
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] update delayed scene taunt fight failed: " + ex.Message);
			if (pending != null)
			{
				ClearPendingNativeSceneTauntFight("update_failed");
			}
		}
	}

	internal void ExecuteDelayedNativeSceneTauntFight(PendingNativeSceneTauntFight pending)
	{
		if (pending == null)
		{
			return;
		}
		try
		{
			CloseNativeConversationForSceneMechanism("native_scene_taunt_fight_delay_elapsed");
			bool started = SceneTauntBehavior.TryStartSceneTauntFightForExternal(pending.TargetHero, pending.TargetCharacter, pending.TargetAgentIndex, pending.TargetKey, "native_scene_taunt_fight_delay_elapsed");
			try
			{
				InformationManager.DisplayMessage(new InformationMessage(started ? "冲突爆发！" : "冲突未能爆发：当前场景目标不可用。", started ? new Color(1f, 0.35f, 0.2f) : new Color(1f, 0.75f, 0.25f)));
			}
			catch
			{
			}
			Logger.Log("ShoutBehavior", "[NativeConversation] delayed scene taunt fight executed started=" + started + " target=" + (pending.TargetHero?.StringId ?? pending.TargetCharacter?.StringId ?? pending.TargetName ?? "unknown") + " agentIndex=" + pending.TargetAgentIndex);
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] execute delayed scene taunt fight failed: " + ex.Message);
		}
	}

	internal void ClearPendingNativeSceneTauntFight(string reason)
	{
		try
		{
			bool hadPending;
			lock (_pendingNativeSceneTauntFightLock)
			{
				hadPending = _pendingNativeSceneTauntFight != null;
				_pendingNativeSceneTauntFight = null;
			}
			if (hadPending)
			{
				Logger.Log("ShoutBehavior", "[NativeConversation] cleared delayed scene taunt fight reason=" + (reason ?? ""));
			}
		}
		catch
		{
		}
	}

	internal bool TryExecuteNativeSceneMechanismActionTagsDirectly(NpcDataPacket npc, List<SceneSummonPromptTarget> sceneSummonTargets, List<SceneGuidePromptTarget> sceneGuideTargets, string tags)
	{
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (npc == null || string.IsNullOrWhiteSpace(tags) || agents == null)
		{
			return false;
		}
		Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == npc.AgentIndex);
		string content = tags;
		bool openLordsHall = TryTriggerOpenLordsHallAction(npc, agent, ref content);
		if (openLordsHall)
		{
			bool waitForConversationEnd = IsNativeConversationActiveForLordsHallEntry();
			ScheduleLordsHallMissionEntryAfterSpeech(npc.AgentIndex, null, "native_scene_direct_tag", waitForConversationEnd);
			if (waitForConversationEnd)
			{
				CloseNativeConversationForSceneMechanism("native_scene_open_lords_hall_tag");
			}
			Logger.Log("ShoutBehavior", "[NativeConversation] scene mechanism direct result agent=" + npc.AgentIndex + " openLordsHall=True");
			return true;
		}
		bool setsOwnedMassacre = TryProcessSetsOwnedSettlementMassacreActionTags(npc.AgentIndex, ref content);
		if (setsOwnedMassacre && string.IsNullOrWhiteSpace(content))
		{
			return true;
		}
		if (SceneMovementController.IsPrisonBreakRescueMissionActive() && SceneMovementController.HasSceneFollowCommandTag(tags))
		{
			Agent commandAgent = SceneMovementController.ResolvePrisonBreakSceneFollowCommandAgent(agent);
			if (commandAgent != null && commandAgent != agent)
			{
				Logger.Log("SceneFollow", "prison_break_native_follow_redirect requested=" + (agent?.Index ?? npc.AgentIndex) + " prisoner=" + commandAgent.Index);
				agent = commandAgent;
				npc = ShoutUtils.ExtractNpcData(commandAgent) ?? npc;
			}
		}
		if (!CanAgentParticipateInSceneSpeech(agent) || agent == Agent.Main)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] scene mechanism direct skip agent=" + (npc?.AgentIndex ?? -1) + " reason=agent_unavailable tags=" + ((tags ?? "").Replace("\r", "\\r").Replace("\n", "\\n")));
			return false;
		}
		SceneSummonConversationSession sceneSummonConversationSession = null;
		ActiveSceneSummonRequest activeSceneSummonRequest = null;
		ActiveSceneGuideRequest activeSceneGuideRequest = null;
		bool stopFollow = _sceneMovement.TryConsumeSceneFollowStopTag(npc, agent, ref content);
		bool startFollow = _sceneMovement.TryConsumeSceneFollowStartTag(npc, agent, ref content);
		bool endChat = _sceneMovement.TryConsumeSceneEndChatActionTag(npc, agent, ref content, out sceneSummonConversationSession);
		bool summon = !openLordsHall && !string.IsNullOrWhiteSpace(content) && _sceneMovement.TryTriggerSceneSummonAction(npc, agent, sceneSummonTargets, sceneGuideTargets, ref content, out activeSceneSummonRequest);
		bool guide = !openLordsHall && !string.IsNullOrWhiteSpace(content) && _sceneMovement.TryTriggerSceneGuideAction(npc, agent, sceneGuideTargets, sceneSummonTargets, ref content, out activeSceneGuideRequest);
		bool handled = setsOwnedMassacre || openLordsHall || stopFollow || startFollow || endChat || summon || guide;
		if (!handled)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] scene mechanism direct no-op agent=" + npc.AgentIndex + " tags=" + ((tags ?? "").Replace("\r", "\\r").Replace("\n", "\\n")));
			return false;
		}
		if (stopFollow)
		{
			_sceneMovement.StopSceneSummonFollowPlayer(agent, restoreDailyBehaviors: false);
			_sceneMovement.ReturnAgentAfterStoppingSceneFollow(agent);
		}
		else if (startFollow)
		{
			_sceneMovement.RememberSceneFollowReturnState(agent, overwriteExisting: true);
			_sceneMovement.RemoveAgentFromSceneSummonConversationForFollow(agent);
			_sceneMovement.StartSceneSummonFollowPlayer(agent);
		}
		if (endChat && sceneSummonConversationSession != null)
		{
			_sceneMovement.BeginSceneSummonConversationReturn(sceneSummonConversationSession);
		}
		if (summon && activeSceneSummonRequest != null)
		{
			_sceneMovement.SchedulePreparedSceneSummonLaunch(activeSceneSummonRequest, null, "");
		}
		if (guide && activeSceneGuideRequest != null)
		{
			_sceneMovement.SchedulePreparedSceneGuideLaunch(activeSceneGuideRequest, null, "");
		}
		Logger.Log("ShoutBehavior", "[NativeConversation] scene mechanism direct result agent=" + npc.AgentIndex + " openLordsHall=" + openLordsHall + " setsMassacre=" + setsOwnedMassacre + " stop=" + stopFollow + " start=" + startFollow + " end=" + endChat + " summon=" + summon + " guide=" + guide + " remaining=" + ((content ?? "").Replace("\r", "\\r").Replace("\n", "\\n")));
		return true;
	}

	internal static void CloseNativeConversationForSceneMechanism(string reason)
	{
		try
		{
			CloseNativeConversationInput(clearSessionHistory: false);
			ConversationManager conversationManager = Campaign.Current?.ConversationManager;
			if (conversationManager != null && (conversationManager.IsConversationInProgress || conversationManager.IsConversationFlowActive))
			{
				conversationManager.EndConversation();
				Logger.Log("ShoutBehavior", "[NativeConversation] closed native conversation for scene mechanism. reason=" + (reason ?? ""));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] close native conversation for scene mechanism failed: " + ex.Message);
		}
	}

	internal bool TryTriggerNativeConversationOpenLordsHallAction(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, ref string content)
	{
		if (!ContainsOpenLordsHallActionTag(content))
		{
			return false;
		}
		try
		{
			NpcDataPacket npc = BuildNativeConversationNpcData(targetHero, targetCharacter);
			npc.AgentIndex = targetAgentIndex;
			Agent agent = (targetAgentIndex >= 0) ? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex) : null;
			bool result = TryTriggerOpenLordsHallAction(npc, agent, ref content);
			if (result)
			{
				content = StripActionTagsForSceneSpeech(content);
				bool waitForConversationEnd = IsNativeConversationActiveForLordsHallEntry();
				ScheduleLordsHallMissionEntryAfterSpeech(targetAgentIndex, null, "native_conversation_tag", waitForConversationEnd);
				if (waitForConversationEnd)
				{
					CloseNativeConversationForSceneMechanism("native_conversation_open_lords_hall_tag");
				}
			}
			Logger.Log("ShoutBehavior", "[NativeConversation] open lords hall tag handled=" + result + " target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown") + " agentIndex=" + targetAgentIndex);
			return result;
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] open lords hall action failed: " + ex.Message);
			return false;
		}
	}

    private ConcurrentQueue<Action> _mainThreadActions { get => _ports.Get_mainThreadActions(); set => _ports.Set_mainThreadActions(value); }
    private bool TryProcessSetsOwnedSettlementMassacreActionTags(int targetAgentIndex, ref string content) => _ports.TryProcessSetsOwnedSettlementMassacreActionTags_L12771(targetAgentIndex, ref content);
    private void TryDrainNativeConversationQueuedActions(string reason) => _ports.TryDrainNativeConversationQueuedActions_L13050(reason);
    private bool TryTriggerOpenLordsHallAction(NpcDataPacket npc, Agent agent, ref string content) => _ports.TryTriggerOpenLordsHallAction_L15221(npc, agent, ref content);
    private void ScheduleLordsHallMissionEntryAfterSpeech(int agentIndex, SceneSpeechPlaybackInfo playbackInfo, string reason, bool waitForConversationEnd = false) => _ports.ScheduleLordsHallMissionEntryAfterSpeech_L19653(agentIndex, playbackInfo, reason, waitForConversationEnd);
    private SceneMovementController _sceneMovement { get => _ports.Get_sceneMovement(); }
    private void EnqueueSpeechLineWithOptions(NpcDataPacket npc, string content, List<NpcDataPacket> allNpcData, bool commitHistory, bool suppressStare, bool allowPlayerDirectedActions, int requiredConversationEpoch, List<SceneSummonPromptTarget> sceneSummonTargets = null, List<SceneGuidePromptTarget> sceneGuideTargets = null, string afterSpeechInfoMessage = null, TaskCompletionSource<bool> completionSource = null, float interactionTimeoutSeconds = -1f, int interactionParticipantCount = 1, Func<bool> canStillPublish = null, string playerDirectedActionText = null, string playerDirectedNpcReplyText = null) => _ports.EnqueueSpeechLineWithOptions_L60(npc, content, allNpcData, commitHistory, suppressStare, allowPlayerDirectedActions, requiredConversationEpoch, sceneSummonTargets, sceneGuideTargets, afterSpeechInfoMessage, completionSource, interactionTimeoutSeconds, interactionParticipantCount, canStillPublish, playerDirectedActionText, playerDirectedNpcReplyText);
}

internal sealed class SceneNativeMechanismControllerPorts
{
    internal Func<ConcurrentQueue<Action>> Get_mainThreadActions;
    internal Action<ConcurrentQueue<Action>> Set_mainThreadActions;
    internal delegate bool TryProcessSetsOwnedSettlementMassacreActionTags_L12771Callback(int targetAgentIndex, ref string content);
    internal TryProcessSetsOwnedSettlementMassacreActionTags_L12771Callback TryProcessSetsOwnedSettlementMassacreActionTags_L12771;
    internal delegate void TryDrainNativeConversationQueuedActions_L13050Callback(string reason);
    internal TryDrainNativeConversationQueuedActions_L13050Callback TryDrainNativeConversationQueuedActions_L13050;
    internal delegate bool TryTriggerOpenLordsHallAction_L15221Callback(NpcDataPacket npc, Agent agent, ref string content);
    internal TryTriggerOpenLordsHallAction_L15221Callback TryTriggerOpenLordsHallAction_L15221;
    internal delegate void ScheduleLordsHallMissionEntryAfterSpeech_L19653Callback(int agentIndex, SceneSpeechPlaybackInfo playbackInfo, string reason, bool waitForConversationEnd);
    internal ScheduleLordsHallMissionEntryAfterSpeech_L19653Callback ScheduleLordsHallMissionEntryAfterSpeech_L19653;
    internal Func<SceneMovementController> Get_sceneMovement;
    internal delegate void EnqueueSpeechLineWithOptions_L60Callback(NpcDataPacket npc, string content, List<NpcDataPacket> allNpcData, bool commitHistory, bool suppressStare, bool allowPlayerDirectedActions, int requiredConversationEpoch, List<SceneSummonPromptTarget> sceneSummonTargets, List<SceneGuidePromptTarget> sceneGuideTargets, string afterSpeechInfoMessage, TaskCompletionSource<bool> completionSource, float interactionTimeoutSeconds, int interactionParticipantCount, Func<bool> canStillPublish, string playerDirectedActionText, string playerDirectedNpcReplyText);
    internal EnqueueSpeechLineWithOptions_L60Callback EnqueueSpeechLineWithOptions_L60;
}
