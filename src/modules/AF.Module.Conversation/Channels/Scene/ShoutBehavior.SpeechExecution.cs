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

namespace AnimusForge;


public partial class ShoutBehavior
{


	private void EnqueueSpeechLine(NpcDataPacket npc, string content, List<NpcDataPacket> allNpcData, bool skipHistory = false, bool suppressStare = false, List<SceneSummonPromptTarget> sceneSummonTargets = null, List<SceneGuidePromptTarget> sceneGuideTargets = null, Func<bool> canStillPublish = null)
	{
		EnqueueSpeechLineWithOptions(npc, content, allNpcData, !skipHistory, suppressStare, allowPlayerDirectedActions: true, requiredConversationEpoch: 0, sceneSummonTargets, sceneGuideTargets, null, canStillPublish: canStillPublish);
	}

private void EnqueueSpeechLineWithOptions(NpcDataPacket npc, string content, List<NpcDataPacket> allNpcData, bool commitHistory, bool suppressStare, bool allowPlayerDirectedActions, int requiredConversationEpoch, List<SceneSummonPromptTarget> sceneSummonTargets = null, List<SceneGuidePromptTarget> sceneGuideTargets = null, string afterSpeechInfoMessage = null, TaskCompletionSource<bool> completionSource = null, float interactionTimeoutSeconds = -1f, int interactionParticipantCount = 1, Func<bool> canStillPublish = null, string playerDirectedActionText = null, string playerDirectedNpcReplyText = null) => _j17SceneSpeechEnqueueAdapter.EnqueueSpeechLineWithOptions(npc, content, allNpcData, commitHistory, suppressStare, allowPlayerDirectedActions, requiredConversationEpoch, sceneSummonTargets, sceneGuideTargets, afterSpeechInfoMessage, completionSource, interactionTimeoutSeconds, interactionParticipantCount, canStillPublish, playerDirectedActionText, playerDirectedNpcReplyText);

	internal sealed class SceneSpeechQueueItem
	{
		public Mission SourceMission;
        public long RuntimeGeneration;

        public NpcDataPacket Npc;

		public string Content;
		public AnimusForge.DiplomacyDialogue.DialogueInteractionOrigin DiplomacyOrigin;

		public List<NpcDataPacket> ContextSnapshot;

		public List<SceneSummonPromptTarget> SceneSummonTargets;

		public List<SceneGuidePromptTarget> SceneGuideTargets;

		public bool CommitHistory;

		public bool SuppressStare;

		public bool AllowPlayerDirectedActions = true;

		public string PlayerDirectedActionText;

		public string PlayerDirectedNpcReplyText;

		public int RequiredConversationEpoch;

		public string AfterSpeechInfoMessage;

		public TaskCompletionSource<bool> CompletionSource;

		public float InteractionTimeoutSeconds = -1f;

		public int InteractionParticipantCount = 1;

		public Func<bool> CanStillPublish;
	}

	private SceneSpeechExecutionRuntime<SceneSpeechQueueItem> _sceneSpeechExecution;
	private SceneSpeechExecutionRuntime<SceneSpeechQueueItem> _sceneSpeechQueueOwner =>
		_sceneSpeechExecution ??= new SceneSpeechExecutionRuntime<SceneSpeechQueueItem>(
			() => _isProcessingShout, action => _mainThreadActions.Enqueue(action), _sceneSpeechEffects.Publish,
			item => item.CompletionSource?.TrySetResult(false));
    private SceneSystemNpcShoutRuntime _systemNpcShoutRuntime;
    private SceneSystemNpcShoutRuntime _systemNpcShout => _systemNpcShoutRuntime ??= new SceneSystemNpcShoutRuntime(
        _sceneSpeechEffects.Ports, _conversationGameThreadDispatcher, () => _sceneConversationEpoch,
        () => System.Threading.Volatile.Read(ref SceneConversationHistoryOwner.SessionId), () => ReferenceEquals(CurrentInstance, this));
    private SceneSpeechEffectController _sceneSpeechEffectController;
    private SceneSpeechEffectController _sceneSpeechEffects => _sceneSpeechEffectController ??= new SceneSpeechEffectController(new SceneSpeechEffectPorts
    {
        AddAgentToStareList = AddAgentToStareList,
        HoldSceneConversationParticipants = HoldSceneConversationParticipants,
        InterruptAgentSpeechForCombat = InterruptAgentSpeechForCombat,
        IsSceneConversationEpochCurrent = IsSceneConversationEpochCurrent,
        PersistNpcSpeechToNamedHeroes = PersistNpcSpeechToNamedHeroes,
        RecordResponseForAllNearbySafe = RecordResponseForAllNearbySafe,
        RecordSystemFactForNearbySafe = RecordSystemFactForNearbySafe,
        RefreshActiveInteractionTimeout = RefreshActiveInteractionTimeout,
        ReleaseSceneConversationConstraints = ReleaseSceneConversationConstraints,
        ResolveHeroFromAgentIndex = ResolveHeroFromAgentIndex,
        ScheduleLordsHallMissionEntryAfterSpeech = ScheduleLordsHallMissionEntryAfterSpeech,
        ScheduleMeetingReleaseAfterSpeech = ScheduleMeetingReleaseAfterSpeech,
        ScheduleSceneAutonomyRestoreAfterSpeech = ScheduleSceneAutonomyRestoreAfterSpeech,
        ScheduleWorldMapMissionExitAfterSpeech = ScheduleWorldMapMissionExitAfterSpeech,
        ShowNpcSpeechOutput = ShowNpcSpeechOutput,
        ShowOpenLordsHallResponseAndScheduleEntry = ShowOpenLordsHallResponseAndScheduleEntry,
        TryConsumeSceneNpcSurrenderTag = TryConsumeSceneNpcSurrenderTag,
        TryTriggerOpenLordsHallAction = TryTriggerOpenLordsHallAction,
        GetDuelLiteralHit = () => _lastShoutDuelLiteralHit,
    }, _sceneMovement);
    private SceneSpeechCompletionController _sceneSpeechCompletionController;
    private SceneSpeechCompletionController _sceneSpeechCompletion => _sceneSpeechCompletionController ??= new SceneSpeechCompletionController(new SceneSpeechCompletionPorts
    {
        PrepareInteractionCompletion = PrepareInteractionCompletion,
        FlushDialogueFeed = FlushPendingSceneDialogueFeedAfterSpeech,
        TryFlushLordsHallEntry = FlushLordsHallMissionEntryAfterSpeech,
        FlushMeetingRelease = FlushMeetingReleaseAfterSpeech,
        FlushWorldMapExit = FlushWorldMapMissionExitAfterSpeech,
        FlushAutonomyRestore = FlushSceneAutonomyRestoreAfterSpeech,
        CleanupLipSync = CleanupSceneLipSyncAfterPlaybackFinished,
        RunStep = RunSceneTtsPlaybackFinishedStep,
        MainThreadQueueCount = () => _mainThreadActions.Count,
    }, _sceneMovement);
}
