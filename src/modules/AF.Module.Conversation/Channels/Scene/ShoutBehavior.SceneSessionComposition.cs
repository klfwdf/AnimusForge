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

using static AnimusForge.SceneConversationSessionRuntime;
namespace AnimusForge;

public partial class ShoutBehavior
{
    private int _sceneConversationEpoch { get => _sceneConversation.ConversationEpoch; set => _sceneConversation.ConversationEpoch=value; }
    private bool _isWaitingForScenePostprocessGate { get => _sceneConversation.IsWaitingForPostprocess; set => _sceneConversation.IsWaitingForPostprocess=value; }
    private ScenePlayerShoutRequestOwner _scenePlayerShoutRequestOwner => _sceneConversation.PlayerRequests;
    private AnimusForge.Refactor.Runtime.ConversationRequestLifetime _sceneRequestLifetime { get => _sceneConversation.RequestLifetime; set => _sceneConversation.RequestLifetime=value; }
    internal static bool CanPublishImmediateSceneReaction(Func<bool> canStillPublish) => SceneConversationSessionRuntime.CanPublishImmediateSceneReaction(canStillPublish);
    private SceneConversationSessionRuntime _sceneConversationRuntime;
    private SceneConversationSessionRuntime _sceneConversation => _sceneConversationRuntime ?? (_sceneConversationRuntime = new SceneConversationSessionRuntime(CreateSceneConversationSessionPorts(), _conversationGameThreadDispatcher, _sceneMovement, _pendingMainThreadFunctions));
    private SceneConversationSessionPorts CreateSceneConversationSessionPorts() => new SceneConversationSessionPorts
    {
        AbsorbPresentationAudience = AbsorbPresentationAudience,
        ActivateMultiSceneMovementSuppression = ActivateMultiSceneMovementSuppression,
        AwaitPrecomputedPersistedHistoryContextAsync = AwaitPrecomputedPersistedHistoryContextAsync,
        BuildPersistedHeroHistoryContext = BuildPersistedHeroHistoryContext,
        BuildPreprocessExcludedRuleIdsForCurrentInteraction = BuildPreprocessExcludedRuleIdsForCurrentInteraction,
        BuildRuntimeSceneMechanismPostprocessRulesForScene = BuildRuntimeSceneMechanismPostprocessRulesForScene,
        BuildScenePresentNpcListBlockForPrompt = BuildScenePresentNpcListBlockForPrompt,
        BuildStrictSceneMessagesForNpc = BuildStrictSceneMessagesForNpc,
        ClearPendingSceneConversationAttentionRelease = ClearPendingSceneConversationAttentionRelease,
        ClearQueuedSceneSpeech = ClearQueuedSceneSpeech,
        DeactivateMultiSceneMovementSuppression = DeactivateMultiSceneMovementSuppression,
        EnqueueSpeechLine = EnqueueSpeechLine,
        EnqueueSpeechLineWithOptions = EnqueueSpeechLineWithOptions,
        EnsurePersonaForCandidatesAsync = EnsurePersonaForCandidatesAsync,
        ExtendStaringHoldForPlayerDrivenSceneRound = ExtendStaringHoldForPlayerDrivenSceneRound,
        GetAgentsForShoutTargetingContext = GetAgentsForShoutTargetingContext,
        GetLatestSceneNpcUtterance = GetLatestSceneNpcUtterance,
        GetPresentationExcludedAgentIndices = GetPresentationExcludedAgentIndices,
        HoldSceneConversationAgents = HoldSceneConversationAgents,
        NotePresentationRoundGroup = NotePresentationRoundGroup,
        PersistExtraFactToNamedHeroes = PersistExtraFactToNamedHeroes,
        PersistNpcSpeechToNamedHeroes = PersistNpcSpeechToNamedHeroes,
        PrepareAutoGroupParticipantsForIdleTimeout = PrepareAutoGroupParticipantsForIdleTimeout,
        PromotePersonalizedExtraFactInScenePrivateHistory = PromotePersonalizedExtraFactInScenePrivateHistory,
        QueueSceneInfoMessage = QueueSceneInfoMessage,
        RecordExtraFactToSceneHistory = RecordExtraFactToSceneHistory,
        RecordPlayerMessage = RecordPlayerMessage,
        RecordPlayerSpeechToMessageFeed = RecordPlayerSpeechToMessageFeed,
        RecordResponseForAllNearbySafe = RecordResponseForAllNearbySafe,
        ReleaseSceneConversationConstraints = ReleaseSceneConversationConstraints,
        RemoveSceneMovementSuppressionAgents = RemoveSceneMovementSuppressionAgents,
        ResetStaringBehavior = ResetStaringBehavior,
        ResetStaringForActiveInteraction = ResetStaringForActiveInteraction,
        ResolveHeroFromAgentIndex = ResolveHeroFromAgentIndex,
        ResumeGame = ResumeGame,
        SetPendingHeroHistoryExtraFactAfterSceneReply = SetPendingHeroHistoryExtraFactAfterSceneReply,
        StartPrecomputedPersistedHistoryContextTask = StartPrecomputedPersistedHistoryContextTask,
        StopAllLipSyncPlaybackAndCleanup = StopAllLipSyncPlaybackAndCleanup,
        TrackPlayerInteraction = TrackPlayerInteraction,
        TryApplyDeferredSceneMoodTag = TryApplyDeferredSceneMoodTag,
        TryApplyDeferredScenePostprocessActionTagsDirectly = TryApplyDeferredScenePostprocessActionTagsDirectly,
        CapturePromptRoutingWork = static request =>
        {
            MyBehavior owner = MyBehavior.Instance;
            if (owner == null) return null;
            PromptBuildPhases phases = owner.BeginSharedPromptBuild(request.Hero, request.Input, request.ExtraFact, request.CultureId,
                request.IsHero, request.Character, request.KingdomId, request.AgentIndex,
                suppressDynamicRuleAndLore: false, usePrefetchedLoreContext: request.HasLore,
                prefetchedLoreContext: request.Lore, excludedRuleIds: null,
                preprocessExcludedRuleIds: request.ExcludedRules, forcedPreprocessRuleIds: null,
                preprocessMentionedEntities: null);
            return phases == null ? null : owner.CaptureSharedPromptRoutingWork(phases);
        },
        CapturePromptKnowledge = static (phases, hero) =>
        {
            MyBehavior owner = MyBehavior.Instance;
            if (owner == null) return null;
            owner.CaptureSharedKnowledgeSnapshot(phases, hero);
            return MyBehavior.CreateSharedKnowledgeWorkInput(phases);
        },
        CompletePromptCapture = static (phases, hero, character) => MyBehavior.Instance?.CompleteSharedPromptBuild(phases, hero, character, null),
        IsOwnerCurrent = () => ReferenceEquals(CurrentInstance, this),
        GetProcessing = () => _isProcessingShout,
        SetProcessing = value => _isProcessingShout = value,
        SceneSessionId = () => Volatile.Read(ref _sceneHistorySessionId),
        ProcessingSequence = () => Interlocked.Read(ref _sceneShoutProcessingSequence),
        ActiveTargetingContext = () => _activeShoutTargetingContext,
        PostMainThread = action => _mainThreadActions.Enqueue(action),
        CaptureVisibleSceneHistoryLines = CaptureVisibleSceneHistoryLinesForPrompt,
        PreparePlayerRound = text => { _stareTimer = 0f; _currentStareTarget = null; try { _lastShoutDuelLiteralHit = ContainsLiteralKeywordHit(text, AIConfigHandler.DuelTriggerKeywords); } catch { _lastShoutDuelLiteralHit = false; } },
        ClearPendingHeroFacts = () => { _pendingHeroHistoryExtraFactAfterSceneReply = ""; _pendingHeroHistoryExtraFactTargetsAfterSceneReply = new List<NpcDataPacket>(); _pendingHeroHistoryExtraFactPersonalizedAgentIndexAfterSceneReply = -1; },
        ShowPlayerSpeech = text => { if (_floatingTextView != null && Agent.Main != null) _floatingTextView.AddOrUpdateText(Agent.Main, text); },
        ClearImmediateInteractionTimeouts = index => { _activeInteractionSessions.Remove(index); _pendingInteractionTimeoutArms.Remove(index); },
    };
private Task<string> GenerateGroupConversationTurnLineAsync(NpcDataPacket speakerNpc, List<NpcDataPacket> allNpcData, Dictionary<int, Hero> resolvedHeroes, Dictionary<int, PrecomputedShoutRagContext> precomputedContexts, string playerText, string extraFact, string commonCandidatesPrompt, List<SceneSummonPromptTarget> sceneSummonTargets, List<SceneGuidePromptTarget> sceneGuideTargets, string sceneMechanismPromptSectionBase, List<string> patienceStatusLines, bool multiNpcScene, int minTokens, int maxTokens, Func<bool> isCurrent) => _sceneConversation.GenerateGroupConversationTurnLineAsync(speakerNpc, allNpcData, resolvedHeroes, precomputedContexts, playerText, extraFact, commonCandidatesPrompt, sceneSummonTargets, sceneGuideTargets, sceneMechanismPromptSectionBase, patienceStatusLines, multiNpcScene, minTokens, maxTokens, isCurrent);
private Task<string> GetPassiveNpcResponse(NpcDataPacket data, string sceneDesc, string inputActionText, string precalculatedLore, List<NpcDataPacket> allNpcData, Dictionary<int, Hero> resolvedHeroes) => _sceneConversation.GetPassiveNpcResponse(data, sceneDesc, inputActionText, precalculatedLore, allNpcData, resolvedHeroes);
private Task HandleGroupResponse(string playerText, List<NpcDataPacket> allNpcData, string sceneDesc, NpcDataPacket primaryNpc, string extraFact, Dictionary<int, PrecomputedShoutRagContext> precomputedContexts, Dictionary<int, Hero> resolvedHeroes, int conversationEpoch, SceneShoutConversationScope conversationScope, List<NpcDataPacket> framedNpcData, SceneGroupReceipt receipt = null) => _sceneConversation.HandleGroupResponse(playerText, allNpcData, sceneDesc, primaryNpc, extraFact, precomputedContexts, resolvedHeroes, conversationEpoch, conversationScope, framedNpcData, receipt);
private Task HandleGroupResponsePerHeroIndependent(string playerText, List<NpcDataPacket> allNpcData, string sceneDesc, NpcDataPacket primaryNpc, string extraFact, Dictionary<int, PrecomputedShoutRagContext> precomputedContexts, Dictionary<int, Hero> resolvedHeroes, int conversationEpoch, SceneShoutConversationScope conversationScope, List<NpcDataPacket> framedNpcData, SceneGroupReceipt receipt = null) => _sceneConversation.HandleGroupResponsePerHeroIndependent(playerText, allNpcData, sceneDesc, primaryNpc, extraFact, precomputedContexts, resolvedHeroes, conversationEpoch, conversationScope, framedNpcData, receipt);
private bool TryBeginImmediateSceneReactionGeneration(int targetAgentIndex, out long requestId, out string suppressReason) => _sceneConversation.TryBeginImmediateSceneReactionGeneration(targetAgentIndex, out requestId, out suppressReason);
private bool FinishImmediateSceneReactionGeneration(int targetAgentIndex, long requestId) => _sceneConversation.FinishImmediateSceneReactionGeneration(targetAgentIndex, requestId);
private bool RegisterImmediateSceneReactionRequest(ImmediateSceneReactionRequest request) => _sceneConversation.RegisterImmediateSceneReactionRequest(request);
private bool TryTakeImmediateSceneReactionRequest(long requestId, out ImmediateSceneReactionRequest request) => _sceneConversation.TryTakeImmediateSceneReactionRequest(requestId, out request);
private void InvokeImmediateSceneReactionNoSpeechFallback(Action onNoSpeech) => _sceneConversation.InvokeImmediateSceneReactionNoSpeechFallback(onNoSpeech);
private void QueueImmediateSceneReactionNoSpeechFallback(Action onNoSpeech) => _sceneConversation.QueueImmediateSceneReactionNoSpeechFallback(onNoSpeech);
private void QueueImmediateSceneReactionCompletion(Action<bool> onCompleted, bool generated) => _sceneConversation.QueueImmediateSceneReactionCompletion(onCompleted, generated);
private bool TriggerImmediateSceneBehaviorReaction(string factText, int targetAgentIndex, bool persistHeroPrivateHistory, bool suppressStare, float postSpeechLeaveSeconds = -1f, bool skipSceneFactRecord = false, bool returnSceneSummonOnTimeout = false, Action onNoSpeech = null, bool runSiegeReactionPostprocess = false, Func<bool> canStillPublish = null, Action<bool> onCompleted = null) => _sceneConversation.TriggerImmediateSceneBehaviorReaction(factText, targetAgentIndex, persistHeroPrivateHistory, suppressStare, postSpeechLeaveSeconds, skipSceneFactRecord, returnSceneSummonOnTimeout, onNoSpeech, runSiegeReactionPostprocess, canStillPublish, onCompleted);
private bool TryPrepareImmediateSceneReactionRequest(Mission sourceMission, long requestId, NpcDataPacket targetNpc, List<NpcDataPacket> allNpcData, Dictionary<int, Hero> resolvedHeroes, bool suppressStare, string factText, bool runSiegeReactionPostprocess, Func<bool> canStillPublish, out ImmediateSceneReactionRequest request) => _sceneConversation.TryPrepareImmediateSceneReactionRequest(sourceMission, requestId, targetNpc, allNpcData, resolvedHeroes, suppressStare, factText, runSiegeReactionPostprocess, canStillPublish, out request);
private void StartImmediateSceneReactionBackgroundRequest(long requestId, int targetAgentIndex, List<object> messages, int maxTokens, float temperature) => _sceneConversation.StartImmediateSceneReactionBackgroundRequest(requestId, targetAgentIndex, messages, maxTokens, temperature);
private void CompleteImmediateSceneReactionOnMainThread(long requestId, bool requestSucceeded, string response, string error) => _sceneConversation.CompleteImmediateSceneReactionOnMainThread(requestId, requestSucceeded, response, error);
private bool IsImmediateSceneReactionRuntimeCurrent(ImmediateSceneReactionRequest request) => _sceneConversation.IsImmediateSceneReactionRuntimeCurrent(request);
private bool CanPublishImmediateSceneReactionRequest(ImmediateSceneReactionRequest request) => _sceneConversation.CanPublishImmediateSceneReactionRequest(request);
private void PopulateImmediateSceneReactionPersonaOnMainThread(NpcDataPacket npc, Hero hero) => _sceneConversation.PopulateImmediateSceneReactionPersonaOnMainThread(npc, hero);
private Task<SceneCompactReactionInput> CaptureCompactSceneReactionInputAsync(NpcDataPacket targetNpc, Hero contextHero, List<NpcDataPacket> allNpcData, string extraFactLine, string singleReplyUserContent, Func<bool> isCurrent) => _sceneConversation.CaptureCompactSceneReactionInputAsync(targetNpc, contextHero, allNpcData, extraFactLine, singleReplyUserContent, isCurrent);
private Task<ScenePostprocessOutcome> QueueDeferredScenePostprocessActions(NpcDataPacket currentSpeaker, List<NpcDataPacket> allNpcData, Hero speakingHero, CharacterObject npcCharacter, string privateRecentWindowSection, string scenePublicHistorySection, string playerText, string replyText, bool duelRuleInjected, bool rewardRuleInjected, bool loanRuleInjected, bool kingdomServiceRuleInjected, bool kingdomVassalageRuleInjected, bool kingdomAnnexationRuleInjected, bool lordsHallRuleInjected, bool meetingReleaseRuleInjected, bool vanillaIssueRuleInjected, bool heroJoinPartyRuleInjected, bool sceneMechanismRuleInjected, bool partyTransferRuleInjected, bool voteDealRuleInjected, bool diplomacyRuleInjected, bool worldMapPartyCommandRuleInjected, bool marriageRuleInjected, bool siegeInterventionRuleInjected, List<RewardSystemBehavior.DuelStakeOption> duelStakeOptions, List<PostprocessRuleEntry> kingdomServiceRules, List<PostprocessRuleEntry> sceneMechanismRules, int conversationEpoch, List<SceneSummonPromptTarget> sceneSummonTargets, List<SceneGuidePromptTarget> sceneGuideTargets, string entityPostprocessContext = null, bool replyIsDirectPlayerResponse = false, List<string> preprocessRuleHits = null, bool relayRuleInjected = false, List<NpcDataPacket> relayCandidates = null, int relayPrimaryTargetAgentIndex = -1, bool relaySingleFramedNpc = false, bool customPolicyAgendaRuleInjected = false, long expectedRuntimeGeneration = 0L, int expectedSceneSessionId = -1) => _sceneConversation.QueueDeferredScenePostprocessActions(currentSpeaker, allNpcData, speakingHero, npcCharacter, privateRecentWindowSection, scenePublicHistorySection, playerText, replyText, duelRuleInjected, rewardRuleInjected, loanRuleInjected, kingdomServiceRuleInjected, kingdomVassalageRuleInjected, kingdomAnnexationRuleInjected, lordsHallRuleInjected, meetingReleaseRuleInjected, vanillaIssueRuleInjected, heroJoinPartyRuleInjected, sceneMechanismRuleInjected, partyTransferRuleInjected, voteDealRuleInjected, diplomacyRuleInjected, worldMapPartyCommandRuleInjected, marriageRuleInjected, siegeInterventionRuleInjected, duelStakeOptions, kingdomServiceRules, sceneMechanismRules, conversationEpoch, sceneSummonTargets, sceneGuideTargets, entityPostprocessContext, replyIsDirectPlayerResponse, preprocessRuleHits, relayRuleInjected, relayCandidates, relayPrimaryTargetAgentIndex, relaySingleFramedNpc, customPolicyAgendaRuleInjected, expectedRuntimeGeneration, expectedSceneSessionId);
private bool CommitDeferredSceneActionPlan(
		string rawTags,
		NpcDataPacket speakerSnapshot,
		List<NpcDataPacket> contextSnapshot,
		Hero speakingHero,
		CharacterObject npcCharacter,
		int targetAgentIndex,
		string playerText,
		string replyText,
		string chainName,
		bool replyIsDirectPlayerResponse,
		bool siegeInterventionRuleInjected,
		int conversationEpoch,
		List<SceneSummonPromptTarget> summonTargets,
		List<SceneGuidePromptTarget> guideTargets,
		Func<bool> validateCurrentTarget,
		Func<bool> canStillPublish,
		out TaskCompletionSource<bool> speechCompletion) => _sceneConversation.CommitDeferredSceneActionPlan(rawTags, speakerSnapshot, contextSnapshot, speakingHero, npcCharacter, targetAgentIndex, playerText, replyText, chainName, replyIsDirectPlayerResponse, siegeInterventionRuleInjected, conversationEpoch, summonTargets, guideTargets, validateCurrentTarget, canStillPublish, out speechCompletion);
internal static string IssueModuleSceneTicket(string clientId) => CurrentInstance?._sceneConversation.IssueModuleSceneTicket(clientId);
internal static bool TryClaimModuleSceneTicket(string clientId, string ticketId, out ScenePlayerShoutRequest request) { request = null; return CurrentInstance != null && CurrentInstance._sceneConversation.TryClaimModuleSceneTicket(clientId, ticketId, out request); }
internal static void RevokeModuleSceneTickets(string clientId) => CurrentInstance?._sceneConversation.RevokeModuleSceneTickets(clientId);
internal ScenePlayerShoutContext CaptureModuleSceneContext() => _sceneConversation.CaptureModuleSceneContext();
internal bool TryClaimModuleSceneContext(ScenePlayerShoutContext context, out ScenePlayerShoutRequest request) => _sceneConversation.TryClaimModuleSceneContext(context, out request);
private bool IsModuleSceneTargetingSourceCurrent(ScenePlayerShoutContext context) => _sceneConversation.IsModuleSceneTargetingSourceCurrent(context);
private void RegisterModuleSceneGroup(SceneGroupReceipt receipt) => _sceneConversation.RegisterModuleSceneGroup(receipt);
private void RetireModuleSceneGroup(string reason) => _sceneConversation.RetireModuleSceneGroup(reason);
private void ReleaseModuleSceneGroup(SceneGroupReceipt receipt) => _sceneConversation.ReleaseModuleSceneGroup(receipt);
private bool IsModuleSceneGroupCurrent(SceneGroupReceipt receipt, long generation, int sceneSessionId, int conversationEpoch) => _sceneConversation.IsModuleSceneGroupCurrent(receipt, generation, sceneSessionId, conversationEpoch);
private Task RunSceneGroupOnMainThreadAsync(Func<Task> run) => _sceneConversation.RunSceneGroupOnMainThreadAsync(run);
private Task<MyBehavior.ShoutPromptContext> BuildModuleScenePromptContextAsync(
        Hero hero, CharacterObject character, string playerText, string extraFact,
        string cultureId, string kingdomId, int agentIndex, bool isHero,
        bool hasLore, string lore, List<string> excludedRules,
        long generation, int sessionId, int conversationEpoch) => _sceneConversation.BuildModuleScenePromptContextAsync(hero, character, playerText, extraFact, cultureId, kingdomId, agentIndex, isHero, hasLore, lore, excludedRules, generation, sessionId, conversationEpoch);
private Task RunModuleSceneDialogueAsync(CoreDialogueOperation operation) => _sceneConversation.RunModuleSceneDialogueAsync(operation);
private Task ProcessCapturedScenePlayerShoutAsync(string shoutText, string extraFact, int? forcedPrimaryAgentIndex,
		ScenePlayerShoutRequest request, Action<Action> runWithObservationScope, SceneGroupReceipt receipt = null) => _sceneConversation.ProcessCapturedScenePlayerShoutAsync(shoutText, extraFact, forcedPrimaryAgentIndex, request, runWithObservationScope, receipt);
private Task ProcessCurrentScenePlayerShout(string shoutText, string extraFact, int? forcedPrimaryAgentIndex,
		ScenePlayerShoutRequest request, SceneGroupReceipt receipt = null) => _sceneConversation.ProcessCurrentScenePlayerShout(shoutText, extraFact, forcedPrimaryAgentIndex, request, receipt);
private ScenePlayerShoutRequest CaptureScenePlayerShoutRequest(IEnumerable<Agent> framedTargets, int primaryAgentIndex) => _sceneConversation.CaptureScenePlayerShoutRequest(framedTargets, primaryAgentIndex);
private bool IsScenePlayerShoutRequestCurrent(ScenePlayerShoutRequest request) => _sceneConversation.IsScenePlayerShoutRequestCurrent(request);
private void RegisterScenePostprocessGateTask(Task task) => _sceneConversation.RegisterScenePostprocessGateTask(task);
private Task GetScenePostprocessGateTask() => _sceneConversation.GetScenePostprocessGateTask();
private void ForceClearScenePostprocessGate(string reason, Task expectedGate = null) => _sceneConversation.ForceClearScenePostprocessGate(reason, expectedGate);
private Task WaitForScenePostprocessGateAsync(string reason) => _sceneConversation.WaitForScenePostprocessGateAsync(reason);
private Task<DetachedInteractionHostResult> GenerateSceneShoutMainReplyAsync(
		LegacyChannelInteractionFacade facade,
		RuntimeConfigSnapshot configuration,
		string moduleId,
		string providerId,
		InteractionEnvelope envelope,
		int conversationEpoch,
		Func<Task<string>> fallbackToLegacy,
		CancellationToken cancellationToken) => _sceneConversation.GenerateSceneShoutMainReplyAsync(facade, configuration, moduleId, providerId, envelope, conversationEpoch, fallbackToLegacy, cancellationToken);
private Task<bool> RecordSceneReplyHistoryOnMainThreadAsync(
		NpcDataPacket speaker, List<NpcDataPacket> audience,
		Hero expectedHero, CharacterObject expectedCharacter, string historyText,
		long generation, int sceneSessionId, int conversationEpoch, bool requireMemoryReceipt = false) => _sceneConversation.RecordSceneReplyHistoryOnMainThreadAsync(speaker, audience, expectedHero, expectedCharacter, historyText, generation, sceneSessionId, conversationEpoch, requireMemoryReceipt);
private Task<bool> QueueSceneMainReplyOnMainThreadAsync(
		NpcDataPacket speaker, List<NpcDataPacket> audience,
		Hero expectedHero, CharacterObject expectedCharacter, string replyText,
		long generation, int sceneSessionId, int conversationEpoch,
		List<SceneSummonPromptTarget> sceneSummonTargets, List<SceneGuidePromptTarget> sceneGuideTargets,
		bool hasPostprocess, float interactionTimeoutSeconds, int participantCount,
		string playerText, bool replyIsDirectPlayerResponse,
		TaskCompletionSource<bool> speechCompletion = null) => _sceneConversation.QueueSceneMainReplyOnMainThreadAsync(speaker, audience, expectedHero, expectedCharacter, replyText, generation, sceneSessionId, conversationEpoch, sceneSummonTargets, sceneGuideTargets, hasPostprocess, interactionTimeoutSeconds, participantCount, playerText, replyIsDirectPlayerResponse, speechCompletion);
private int BeginNewPlayerDrivenSceneConversationEpoch() => _sceneConversation.BeginNewPlayerDrivenSceneConversationEpoch();
private bool IsSceneConversationEpochCurrent(int epoch) => _sceneConversation.IsSceneConversationEpochCurrent(epoch);
private bool TryPrepareCompactTownOrdinaryReactionRequest(
		Mission sourceMission,
		long requestId,
		NpcDataPacket targetNpc,
		List<NpcDataPacket> allNpcData,
		bool suppressStare,
		string factText,
		Func<bool> canStillPublish,
		Hero contextHero,
		CharacterObject npcCharacter,
		int minTokens,
		int maxTokens,
		out ImmediateSceneReactionRequest request) => _sceneConversation.TryPrepareCompactTownOrdinaryReactionRequest(sourceMission, requestId, targetNpc, allNpcData, suppressStare, factText, canStillPublish, contextHero, npcCharacter, minTokens, maxTokens, out request);
    internal static void SubmitModuleSceneDialogue(CoreDialogueOperation operation)
    { if (CurrentInstance == null) { operation.Finish("scene.owner_unavailable"); return; } _ = CurrentInstance._sceneConversation.RunModuleSceneDialogueAsync(operation); }
}
