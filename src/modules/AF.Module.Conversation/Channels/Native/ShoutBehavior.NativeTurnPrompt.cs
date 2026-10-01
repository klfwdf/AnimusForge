using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AnimusForge.SceneActions.Core;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Adapters;
using AnimusForge.XihaiAction;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;

namespace AnimusForge;

public partial class ShoutBehavior
{
    // Native game adapter; phase ordering belongs to NativeConversationTurnCoordinator.
    private sealed partial class NativeConversationTurnHost : INativeConversationTurnHost
    {
        // Request-local phase output, published only after its awaited capture completes.
        private bool includeCurrentSceneSessionInPersistedHistory;
        private MyBehavior.ShoutPromptContext ctx;
        private List<string> postprocessPreprocessHits;
        private string postprocessEntityContext;
        private string baseExtras;
        private string trustBlock;
        private string persistedHeroHistory;
        private string playerName;
        private long nativePendingPlayerHistoryEventSequence;
        private string nativePendingAfefKey;
        private List<ConversationMessage> pendingNativeCurrentAfefFacts;
        private List<ConversationMessage> nativeHistoryMessages;
        private string privateRecentWindowSection;
        private string persistedWithoutRecentWindow;
        private string nativeNpcListBlock;
        private string roleTopIntro;
        private string roleRuntimeContext;
        private string systemRuleBlock;
        private bool duelRuleInjected;
        private bool rewardRuleInjected;
        private bool loanRuleInjected;
        private bool kingdomServiceRuleInjected;
        private bool kingdomVassalageRuleInjected;
        private bool lordsHallRuleInjected;
        private bool meetingReleaseRuleInjected;
        private bool vanillaIssueRuleInjected;
        private bool genericSceneMechanismRuleInjected;
        private bool noblePrisonerExecutionRuleInjected;
        private bool partyTransferRuleInjected;
        private bool voteDealRuleInjected;
        private bool customPolicyAgendaRuleInjected;
        private bool diplomacyRuleInjected;
        private bool worldMapPartyCommandRuleInjected;
        private bool siegeInterventionRuleInjected;
        private string nativePostprocessChainName;
        private string nativeHistoryDisplayName;
        private Hero nativeDuelTargetHero;
        private List<object> messages;
        private DetachedPromptSections nativeDetachedMainPromptSections;
        private int minTokens;
        private int maxTokens;
        private string miscExtrasSection;
        private string ruleExtrasSection;
        private string knowledgeExtrasSection;

        public async Task<NativeConversationTurnStep> BuildPromptAsync()
        {
            includeCurrentSceneSessionInPersistedHistory = false;
            if (includeCurrentSceneSessionInPersistedHistory)
            {
                Logger.Log("ShoutBehavior", "[NativeConversation] including active scene-session memory because scene dialogue snapshot is missing. target=" + nativeTargetLog + " agentIndex=" + nativeTargetAgentIndex);
            }
            Logger.Log("Logic", "[MemoryPerf] parallel_history_start reason=native_conversation target=" + nativeTargetLog + " agent=" + nativeTargetAgentIndex + " mode=task");
            Func<string> nativeHistoryWork = await _owner.RunNativeConversationMainThreadFuncAsync(
                "persisted_history_capture", nativeTargetLog, nativeTargetAgentIndex,
                () => _owner.IsNativeConversationAdmissionCurrent(admission, out _)
                    ? CaptureNativeConversationPersistedHistoryWork(targetHero, targetCharacter, shouldRecordPlayerInput ? promptPlayerText : "", currentNativeDialogText, includeCurrentSceneSessionInPersistedHistory, admission.Generation)
                    : null, (Func<string>)null).ConfigureAwait(false);
            if (nativeHistoryWork == null) return NativeConversationTurnStep.Stop("");
            Task<string> persistedHeroHistoryTask = Task.Run(nativeHistoryWork);
            Stopwatch nativePreprocessSw = Stopwatch.StartNew();
            FreezeWatchdog.Mark("NativeConversation.preprocess_start", "target=" + (npcName ?? "unknown") + " agent=" + nativeTargetAgentIndex, immediate: true);
            MyBehavior.WeeklyPromptSnapshot weeklyPromptSnapshot = await _owner.RunNativeConversationMainThreadFuncAsync(
                "weekly_prompt_snapshot",
                nativeTargetLog,
                nativeTargetAgentIndex,
                () => _owner.IsNativeConversationAdmissionCurrent(admission, out _)
                ? MyBehavior.CaptureWeeklyPromptSnapshotForExternal(targetHero, targetCharacter)
                : MyBehavior.WeeklyPromptSnapshot.Empty,
                MyBehavior.WeeklyPromptSnapshot.Empty).ConfigureAwait(false) ?? MyBehavior.WeeklyPromptSnapshot.Empty;
            ctx = await _owner.BuildNativePromptContextScheduledAsync(admission, nativeTargetLog, nativeTargetAgentIndex, runtimeGeneration, targetHero, targetCharacter, routingInput, extraFact, cultureId, npc.IsHero, preprocessExcludedRuleIds, weeklyPromptSnapshot).ConfigureAwait(false);
            nativePreprocessSw.Stop();
            if (ctx == null)
            {
                FreezeWatchdog.Mark("NativeConversation.preprocess_aborted", "target=" + nativeTargetLog + " agent=" + nativeTargetAgentIndex + " ms=" + Math.Round(nativePreprocessSw.Elapsed.TotalMilliseconds, 2), immediate: true);
                Logger.Log("ShoutBehavior", "[NativeConversation] preprocess aborted before main reply target=" + nativeTargetLog + " agent=" + nativeTargetAgentIndex + " elapsedMs=" + Math.Round(nativePreprocessSw.Elapsed.TotalMilliseconds, 2));
                return NativeConversationTurnStep.Stop(BuildNativeConversationPreprocessUnavailableText());
            }
            if (SaveRuntimeGuard.IsStale(runtimeGeneration, "native_conversation_preprocess"))
            {
                return NativeConversationTurnStep.Stop(SaveRuntimeGuard.BuildStaleRequestErrorText());
            }
            postprocessPreprocessHits = ctx?.PreprocessRuleIds ?? new List<string>();
            postprocessEntityContext = ctx?.EntityPostprocessContext ?? "";
            Logger.Log("Logic", "[NativePerf] preprocess_done target=" + (npcName ?? "unknown") + " agent=" + nativeTargetAgentIndex + " ms=" + Math.Round(nativePreprocessSw.Elapsed.TotalMilliseconds, 2) + " hits=" + ((postprocessPreprocessHits == null || postprocessPreprocessHits.Count == 0) ? "(none)" : string.Join(",", postprocessPreprocessHits)) + " extrasLen=" + ((ctx?.Extras ?? "").Length));
            FreezeWatchdog.Mark("NativeConversation.preprocess_done", "target=" + (npcName ?? "unknown") + " agent=" + nativeTargetAgentIndex + " ms=" + Math.Round(nativePreprocessSw.Elapsed.TotalMilliseconds, 2) + " hits=" + ((postprocessPreprocessHits == null || postprocessPreprocessHits.Count == 0) ? "(none)" : string.Join(",", postprocessPreprocessHits)), immediate: true);
            GetSceneReplyLengthLimits(DuelSettings.GetSettings(), out minTokens, out maxTokens);
            baseExtras = StripScenePersonaBlocks((ctx?.Extras ?? "").Trim());
            trustBlock = ExtractTrustPromptBlock(baseExtras, out var baseExtrasWithoutTrust);
            SplitSceneExtraSections(baseExtrasWithoutTrust, out miscExtrasSection, out ruleExtrasSection, out knowledgeExtrasSection);
            Stopwatch nativeHistoryJoinSw = Stopwatch.StartNew();
            FreezeWatchdog.Mark("NativeConversation.history_join_start", "target=" + (npcName ?? "unknown") + " agent=" + nativeTargetAgentIndex, immediate: true);
            persistedHeroHistory = ((await persistedHeroHistoryTask) ?? "").Trim();
            if (!await _owner.RunNativeConversationMainThreadFuncAsync("persisted_history_accept", nativeTargetLog, nativeTargetAgentIndex,
                () => _owner.IsNativeConversationAdmissionCurrent(admission, out _), false).ConfigureAwait(false)) return NativeConversationTurnStep.Stop("");
            nativeHistoryJoinSw.Stop();
            Logger.Log("Logic", "[MemoryPerf] parallel_history_join reason=native_conversation target=" + nativeTargetLog + " agent=" + nativeTargetAgentIndex + " chars=" + persistedHeroHistory.Length + " hasValue=" + !string.IsNullOrWhiteSpace(persistedHeroHistory) + " waitMs=" + Math.Round(nativeHistoryJoinSw.Elapsed.TotalMilliseconds, 2));
            FreezeWatchdog.Mark("NativeConversation.history_join_done", "target=" + (npcName ?? "unknown") + " agent=" + nativeTargetAgentIndex + " chars=" + persistedHeroHistory.Length + " ms=" + Math.Round(nativeHistoryJoinSw.Elapsed.TotalMilliseconds, 2), immediate: true);
            if (!await CaptureOnGameThreadAsync("prompt_rules_capture", CapturePromptRules).ConfigureAwait(false)) return NativeConversationTurnStep.Stop("");
            NativeConversationPendingHistory nativePendingHistory = await _owner.PrepareNativeConversationPendingHistoryAsync(
                admission, npc, npcName, nativeTargetAgentIndex, promptPlayerText, shouldRecordPlayerInput).ConfigureAwait(false);
            if (nativePendingHistory == null) return NativeConversationTurnStep.Stop("");
            playerName = nativePendingHistory.PlayerName;
            nativePendingPlayerHistoryEventSequence = nativePendingHistory.EventSequence;
            nativePendingAfefKey = nativePendingHistory.HistoryKey;
            pendingNativeCurrentAfefFacts = nativePendingHistory.PendingFacts;
            nativeHistoryMessages = nativePendingHistory.Messages;
            if (!await CaptureOnGameThreadAsync("prompt_messages_capture", CapturePromptMessages).ConfigureAwait(false)) return NativeConversationTurnStep.Stop("");
            return NativeConversationTurnStep.Continue();
        }

        private void CapturePromptRules()
        {
            privateRecentWindowSection = "";
            persistedWithoutRecentWindow = "";
            SplitPersistedHeroHistorySections(persistedHeroHistory, out privateRecentWindowSection, out persistedWithoutRecentWindow);
            nativeNpcListBlock = BuildNativeConversationNpcListBlockForPrompt(presentNpcs, npc);
            bool includeInventorySummary = ctx != null && (ctx.UseRewardContext || ctx.IsLoanContext);
            bool includeTradePricing = includeInventorySummary;
            bool partyTransferTopicSelected = HasPartyTransferRuleContext(baseExtras);
            roleTopIntro = BuildSceneSystemTopPromptIntroForSingle(npc, targetHero, presentNpcs, includeInventorySummary, includeTradePricing, partyTransferTopicSelected, ctx?.MentionedEntities);
            roleRuntimeContext = BuildSceneUserRuntimeContextForSingle(npc, targetHero, presentNpcs, includeInventorySummary, includeTradePricing, partyTransferTopicSelected, ctx?.MentionedEntities);
            string nativeSceneSummonClosureInstruction = _owner.BuildSceneSummonClosurePromptInstruction(presentNpcs);
            string nativeSceneFollowControlInstruction = _owner.BuildSceneFollowControlPromptInstruction(npc);
            string nativeSceneMechanismPromptSection = BuildSceneMechanismPromptSection(nativeSceneSummonTargets, nativeSceneGuideTargets, nativeSceneSummonClosureInstruction, nativeSceneFollowControlInstruction, npc);
            systemRuleBlock = BuildSceneSystemRuleBlock(ruleExtrasSection, nativeSceneMechanismPromptSection);
            string combinedRuleInspectionBlock = BuildSceneCompositeUserBlock("", knowledgeExtrasSection, systemRuleBlock);
            Hero nativePostprocessHero = targetHero ?? targetCharacter?.HeroObject;
            duelRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "duel");
            rewardRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "reward");
            loanRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "loan");
            kingdomServiceRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "kingdom_service");
            kingdomVassalageRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "kingdom_vassalage");
            lordsHallRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "lords_hall_access");
            meetingReleaseRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "encounter_release_player");
            vanillaIssueRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "vanilla_issue");
            bool heroJoinPartyRuleInjected = kingdomServiceRuleInjected;
            genericSceneMechanismRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "scene_mechanism_actions");
            noblePrisonerExecutionRuleInjected = shouldRecordPlayerInput
                && HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "noble_prisoner_execution");
            if (!CanUseSceneMechanismPostprocessForSpeaker(nativeTargetAgentIndex)
                || (AIConfigHandler.ShouldExcludeSceneMoveRuleForCurrentMission() && !shouldRecordPlayerInput))
            {
                genericSceneMechanismRuleInjected = false;
            }
            bool sceneMechanismRuleInjected = genericSceneMechanismRuleInjected || noblePrisonerExecutionRuleInjected;
            partyTransferRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "party_transfer");
            voteDealRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "kingdom_agenda");
            customPolicyAgendaRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, CustomPolicyAgendaPostprocessRuleId);
            diplomacyRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "diplomacy");
            worldMapPartyCommandRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "worldmap_party_command");
            if (worldMapPartyCommandRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "worldmap_party_command"))
            {
                roleRuntimeContext = AppendPostprocessContextBlockForScene(roleRuntimeContext, WorldMapPartyCommandBehavior.BuildCurrentNpcCommandTasksPromptForExternal(targetHero, targetCharacter, nativeTargetAgentIndex));
            }
            siegeInterventionRuleInjected = AfGcczShoutBridge.ShouldRunPostprocessFromPrompt(combinedRuleInspectionBlock, postprocessPreprocessHits);
            nativePostprocessChainName = ResolveNativeConversationPostprocessChainName();
            Logger.Log("ShoutBehavior", "[NativeConversation] postprocess setup chain=" + nativePostprocessChainName
                + " target=" + nativeTargetLog
                + " preprocessHits=" + ((postprocessPreprocessHits == null || postprocessPreprocessHits.Count == 0) ? "(none)" : string.Join(",", postprocessPreprocessHits))
                + " kingdom_vassalage_hit=" + HasPreprocessRuleHit(postprocessPreprocessHits, "kingdom_vassalage")
                + " kingdom_vassalage_block=" + kingdomVassalageRuleInjected
                + " diplomacy_block=" + diplomacyRuleInjected);
            nativeDuelTargetHero = nativePostprocessHero;
            if (duelRuleInjected && !CanInjectDuelPostprocessRule(ctx, nativeDuelTargetHero, nativeTargetAgentIndex, routingInput, out var duelPostprocessBlockedReason))
            {
                duelRuleInjected = false;
                Logger.Log("ShoutBehavior", "[NativeConversation] duel postprocess blocked: " + duelPostprocessBlockedReason);
            }
        }

        private void CapturePromptMessages()
        {
            bool useSharedDailyMemoryForNpcOpening = npcInitiatedOpening;
            List<ConversationMessage> persistentMemoryRoleMessages = (useSharedDailyMemoryForNpcOpening || !hadNativeConversationSessionHistoryBeforeTurn)
                ? BuildUncompressedMemoryRoleMessagesForPrompt(targetHero ?? targetCharacter?.HeroObject, targetCharacter, npc, nativeTargetAgentIndex)
                : new List<ConversationMessage>();
            if (useSharedDailyMemoryForNpcOpening && persistentMemoryRoleMessages.Count > 0 && nativeHistoryMessages.Count > 0)
            {
                nativeHistoryMessages = RemoveNativeMessagesAlreadyInPersistentMemory(nativeHistoryMessages, persistentMemoryRoleMessages);
            }
            nativeHistoryDisplayName = GetSceneNpcHistoryNameForPrompt(npc);
            string taskSystemBlock = BuildSceneSingleNpcTaskSystemBlock(nativeHistoryDisplayName, false, minTokens, maxTokens, playerName);
            string nativeSceneActionInstruction = SceneActionsRuntimeHost.BuildNativeConversationActionInstruction();
            string layeredPrompt = BuildSceneCompositeUserBlock("", roleTopIntro, taskSystemBlock, nativeSceneActionInstruction, ctx?.PreprocessExcludedRuleBlock);
            layeredPrompt = AppendPlayerCustomPromptRuleToSystemPrompt(layeredPrompt);
            string sceneDynamicUserBlock = BuildSceneCompositeUserBlock("", roleRuntimeContext, nativeNpcListBlock, trustBlock, miscExtrasSection);
        string[] nativePromptPrefixSections = new string[4] { privateRecentWindowSection, persistedWithoutRecentWindow, sceneDynamicUserBlock, BuildSceneCompositeUserBlock("", knowledgeExtrasSection, systemRuleBlock, nativeMeetingTauntRuleBlock) };
        string[] nativePromptSuffixSections = new string[1] { npcInitiatedOpening ? npcOpeningUserText : "" };
        messages = _owner.BuildStrictSceneMessagesForNpc(nativeTargetAgentIndex, layeredPrompt, nativePromptPrefixSections, nativePromptSuffixSections, currentInputAlreadyRecorded: true, currentPlayerInput: promptPlayerText, injectedHistoryMessages: nativeHistoryMessages, includeSceneHistory: false, persistentHistoryMessages: persistentMemoryRoleMessages, pendingCurrentAfefFactMessages: pendingNativeCurrentAfefFacts, useSceneDistanceSpeechLabels: false);
        nativeDetachedMainPromptSections = null;
        if (NativeConversationDetachedPromptParityLoggingEnabled)
        {
            try
            {
                LegacyNativePromptParity.LegacyNativePromptParityResult parity = LegacyNativePromptParity.CompareMainMessages(
                    messages,
                    nativePromptPrefixSections,
                    nativePromptSuffixSections,
                    promptPlayerText,
                    maxTokens: maxTokens,
                    model: "legacy-native");
                nativeDetachedMainPromptSections = parity.MainSections;
                Logger.Log("ShoutBehavior", "[NativeDetachedPromptParity] " + parity.ToDiagnosticString());
            }
            catch (Exception ex)
            {
                // Diagnostics must fail open to the unchanged Native request path.
                Logger.Log("ShoutBehavior", "[NativeDetachedPromptParity] main comparison failed open: " + ex.Message);
            }
        }
            Logger.Log("ShoutBehavior", "[NativeConversation] request target=" + nativeTargetLog + " agentIndex=" + nativeTargetAgentIndex + " messages=" + messages.Count + " includeSceneSessionMemory=" + includeCurrentSceneSessionInPersistedHistory + " sharedDailyMemory=" + useSharedDailyMemoryForNpcOpening + " persistentMemoryMessages=" + persistentMemoryRoleMessages.Count + " nativeHistoryMessages=" + nativeHistoryMessages.Count + " persistedChars=" + (persistedHeroHistory?.Length ?? 0) + " preprocessHits=" + ((postprocessPreprocessHits.Count == 0) ? "(none)" : string.Join(",", postprocessPreprocessHits)));
        }
    }
}
