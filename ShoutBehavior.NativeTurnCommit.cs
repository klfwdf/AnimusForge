using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AnimusForge.SceneActions.Core;
using AnimusForge.Refactor.Contracts;
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
        private string historyForPostprocess;
        private bool duelPostprocessSelected;
        private bool rewardPostprocessSelected;
        private bool loanPostprocessSelected;
        private bool kingdomServicePostprocessSelected;
        private bool kingdomVassalagePostprocessSelected;
        private bool kingdomAnnexationPostprocessSelected;
        private bool lordsHallPostprocessSelected;
        private bool meetingReleasePostprocessSelected;
        private bool vanillaIssuePostprocessSelected;
        private bool heroJoinPartyPostprocessSelected;
        private bool genericSceneMechanismPostprocessSelected;
        private bool sceneMechanismPostprocessSelected;
        private bool partyTransferPostprocessSelected;
        private bool voteDealPostprocessSelected;
        private bool customPolicyAgendaPostprocessSelected;
        private bool diplomacyPostprocessSelected;
        private bool worldMapPartyCommandPostprocessSelected;
        private bool marriagePostprocessSelected;
        private bool siegeInterventionPostprocessSelected;
        private bool directNoblePrisonerConversation;
        private List<RewardSystemBehavior.DuelStakeOption> nativeDuelStakeOptions;
        private List<PostprocessRuleEntry> nativeSceneMechanismPostprocessRules;
        private string runtimeTargetKingdomId;
        private string runtimeTargetHeroId;
        private string runtimeTargetCharacterId;
        private string runtimeTargetTroopId;
        private string runtimeTargetUnnamedRank;

        public async Task<NativeConversationTurnStep> PostprocessAndCommitAsync()
        {
            if (!await CaptureOnGameThreadAsync("postprocess_rules_capture", CapturePostprocessRules).ConfigureAwait(false)) return NativeConversationTurnStep.Stop("");
            if (shouldRecordPlayerInput && !directNoblePrisonerConversation)
            {
                string nativeDirectCommandTargetUnavailableReason = "";
                bool nativeDirectCommandTargetAvailable = await _owner.RunNativeConversationMainThreadFuncAsync(
                    "direct_scene_command_target_validation",
                    nativeTargetLog,
                    nativeTargetAgentIndex,
                    () => _owner.IsNativeConversationAdmissionCurrent(admission, out nativeDirectCommandTargetUnavailableReason),
                    false).ConfigureAwait(false);
                if (!nativeDirectCommandTargetAvailable)
                {
                    string reason = string.IsNullOrWhiteSpace(nativeDirectCommandTargetUnavailableReason) ? "main_thread_validation_failed" : nativeDirectCommandTargetUnavailableReason;
                    await _owner.RollbackNativeConversationPendingPlayerHistoryAsync(admission, nativePendingAfefKey, nativePendingPlayerHistoryEventSequence, reason).ConfigureAwait(false);
                    Logger.Log("ShoutBehavior", "[NativeConversation] skipped direct scene command because target is unavailable target=" + nativeTargetLog + " agentIndex=" + nativeTargetAgentIndex + " reason=" + reason);
                    return NativeConversationTurnStep.Stop("");
                }
                if (!await CaptureOnGameThreadAsync("direct_scene_command", CaptureDirectSceneCommand).ConfigureAwait(false)) return NativeConversationTurnStep.Stop("");
            }
            if (!await CaptureOnGameThreadAsync("postprocess_targets_capture", CapturePostprocessTargets).ConfigureAwait(false)) return NativeConversationTurnStep.Stop("");
            string postprocessed;
            Stopwatch nativePostprocessSw = Stopwatch.StartNew();
            Logger.Log("Logic", "[NativePerf] postprocess_start target=" + (npcName ?? "unknown") + " agent=" + nativeTargetAgentIndex + " hits=" + ((postprocessPreprocessHits == null || postprocessPreprocessHits.Count == 0) ? "(none)" : string.Join(",", postprocessPreprocessHits)));
            FreezeWatchdog.Mark("NativeConversation.postprocess_start", "target=" + (npcName ?? "unknown") + " agent=" + nativeTargetAgentIndex + " hits=" + ((postprocessPreprocessHits == null || postprocessPreprocessHits.Count == 0) ? "(none)" : string.Join(",", postprocessPreprocessHits)), immediate: true);
            using IDisposable guardrailScopeJ03 = AIConfigHandler.BeginGuardrailRuntimeScope();
            AIConfigHandler.SetGuardrailRuntimeTargetKingdom(runtimeTargetKingdomId);
            AIConfigHandler.SetGuardrailRuntimeTargetHero(runtimeTargetHeroId);
            AIConfigHandler.SetGuardrailRuntimeTargetCharacter(runtimeTargetCharacterId);
            AIConfigHandler.SetGuardrailRuntimeTargetTroop(runtimeTargetTroopId);
            AIConfigHandler.SetGuardrailRuntimeTargetUnnamedRank(runtimeTargetUnnamedRank);
            AIConfigHandler.SetGuardrailRuntimeTargetAgentIndex(nativeTargetAgentIndex);
            try
            {
                SceneActionPostprocessWorkItem workItem = null;
                if (!await CaptureOnGameThreadAsync("postprocess_prepare", () =>
                {
                    workItem = PrepareSceneUnifiedActionPostprocess(targetHero, targetCharacter, nativeTargetAgentIndex, GetSceneNpcHistoryNameForPrompt(npc), shouldRecordPlayerInput ? promptPlayerText : "", historyForPostprocess, postprocessReply, duelPostprocessSelected, rewardPostprocessSelected, loanPostprocessSelected, kingdomServicePostprocessSelected, kingdomVassalagePostprocessSelected, kingdomAnnexationPostprocessSelected, lordsHallPostprocessSelected, meetingReleasePostprocessSelected, vanillaIssuePostprocessSelected, heroJoinPartyPostprocessSelected, sceneMechanismPostprocessSelected, partyTransferPostprocessSelected, voteDealPostprocessSelected, diplomacyPostprocessSelected, worldMapPartyCommandPostprocessSelected, marriagePostprocessSelected, nativeDuelStakeOptions, null, nativeSceneMechanismPostprocessRules, nativeSceneSummonTargets, nativeSceneGuideTargets, postprocessEntityContext, siegeInterventionRuleInjected: siegeInterventionPostprocessSelected, replyIsDirectPlayerResponse: shouldRecordPlayerInput, preprocessRuleHits: postprocessPreprocessHits, chainName: nativePostprocessChainName, customPolicyAgendaRuleInjected: customPolicyAgendaPostprocessSelected, detachedMainPromptSections: nativeDetachedMainPromptSections);
                }).ConfigureAwait(false)) return NativeConversationTurnStep.Stop("");
                // Only detached prompt strings cross the network boundary. Do not put the
                // synchronous provider call on the game thread or normalize on the worker.
                bool succeeded = true;
                string content = null;
                string error = null;
                if (workItem.RequiresNetwork)
                    succeeded = TryRequestSceneUnifiedActionPostprocess(workItem.SystemPrompt,
                        workItem.UserPrompt, out content, out error);
                postprocessed = null;
                if (!await CaptureOnGameThreadAsync("postprocess_complete", () =>
                    postprocessed = CompleteSceneUnifiedActionPostprocess(workItem, succeeded, content, error)
                    ).ConfigureAwait(false)) return NativeConversationTurnStep.Stop("");

            }
            finally
            {
                AIConfigHandler.ClearGuardrailRuntimeTarget();
            }
            nativePostprocessSw.Stop();
            Logger.Log("Logic", "[NativePerf] postprocess_done target=" + (npcName ?? "unknown") + " agent=" + nativeTargetAgentIndex + " resultLen=" + ((postprocessed ?? "").Length) + " ms=" + Math.Round(nativePostprocessSw.Elapsed.TotalMilliseconds, 2) + " elapsedMs=" + Math.Round(nativeTurnSw.Elapsed.TotalMilliseconds, 2));
            FreezeWatchdog.Mark("NativeConversation.postprocess_done", "target=" + (npcName ?? "unknown") + " agent=" + nativeTargetAgentIndex + " resultLen=" + ((postprocessed ?? "").Length) + " ms=" + Math.Round(nativePostprocessSw.Elapsed.TotalMilliseconds, 2), immediate: true);
            if (!string.IsNullOrWhiteSpace(postprocessed))
            {
                cleaned = postprocessed.Trim();
            }
            if (SaveRuntimeGuard.IsStale(runtimeGeneration, "native_conversation_postprocess"))
            {
                return NativeConversationTurnStep.Stop(SaveRuntimeGuard.BuildStaleRequestErrorText());
            }
            string nativePostprocessTargetUnavailableReason = "";
            bool nativePostprocessTargetAvailable = await _owner.RunNativeConversationMainThreadFuncAsync(
                "postprocess_target_validation",
                nativeTargetLog,
                nativeTargetAgentIndex,
                () => _owner.IsNativeConversationAdmissionCurrent(admission, out nativePostprocessTargetUnavailableReason),
                false).ConfigureAwait(false);
            if (!nativePostprocessTargetAvailable)
            {
                string reason = string.IsNullOrWhiteSpace(nativePostprocessTargetUnavailableReason) ? "main_thread_validation_failed" : nativePostprocessTargetUnavailableReason;
                await _owner.RollbackNativeConversationPendingPlayerHistoryAsync(admission, nativePendingAfefKey, nativePendingPlayerHistoryEventSequence, reason).ConfigureAwait(false);
                Logger.Log("ShoutBehavior", "[NativeConversation] dropped completed response because target is unavailable target=" + nativeTargetLog + " agentIndex=" + nativeTargetAgentIndex + " reason=" + reason);
                return NativeConversationTurnStep.Stop("");
            }
            FreezeWatchdog.Mark("NativeConversation.action_tags_start", "target=" + (npcName ?? "unknown") + " agent=" + nativeTargetAgentIndex, immediate: true);
            NativeConversationGameActionResult nativeActionResult = await _owner.ApplyNativeConversationGameActionsOnMainThreadAsync(
                targetHero,
                targetCharacter,
                npc,
                presentNpcs,
                nativeSceneSummonTargets,
                nativeSceneGuideTargets,
                cleaned,
                npcName,
                nativeTargetAgentIndex,
                shouldRecordPlayerInput ? promptPlayerText : string.Empty,
                nativeRequestConversationManager,
                nativeRequestConversationToken, admission, new NativeConversationCompletionRequest
                {
                    PlayerText = shouldRecordPlayerInput ? promptPlayerText : null,
                    OpeningFact = npcOpeningConsumed ? npcOpeningPersistentFactText : null,
                    TtsAlreadyDispatched = nativeTtsDispatchedBeforePostprocess,
                    PendingPlayerHistorySequence = nativePendingPlayerHistoryEventSequence,
                    PendingPlayerHistoryKey = nativePendingAfefKey
                }).ConfigureAwait(false);
            if (nativeActionResult?.ResponseDiscarded == true)
            {
                Logger.Log("ShoutBehavior", "[NativeConversation] response discarded during main-thread action dispatch because the target became unavailable target=" + nativeTargetLog + " agentIndex=" + nativeTargetAgentIndex);
                return NativeConversationTurnStep.Stop("");
            }
            nativeTurnSw.Stop();
            ObserveNativeActionDispatch("completion_returned", nativeTargetLog, nativeTargetAgentIndex, nativeTurnSw);
            return NativeConversationTurnStep.Stop(nativeActionResult?.FinalVisible ?? "");
        }

        private void CapturePostprocessRules()
        {
            List<string> nativePostprocessHistoryLines = BuildNativeConversationUnifiedSceneHistoryLinesForPrompt(targetHero, targetCharacter, npcName, nativeTargetAgentIndex);
            string scenePublicHistorySection = BuildScenePublicHistorySection(nativePostprocessHistoryLines);
            string privateRecentWindowForPostprocess = TrimPrivateRecentWindowForActionPostprocess(privateRecentWindowSection, 5);
            privateRecentWindowForPostprocess = FilterHistorySectionAgainstScenePublicHistory(privateRecentWindowForPostprocess, scenePublicHistorySection);
            historyForPostprocess = BuildSceneCompositeUserBlock("", privateRecentWindowForPostprocess, scenePublicHistorySection);
            if (string.IsNullOrWhiteSpace(historyForPostprocess))
            {
                historyForPostprocess = npcOpeningConsumed ? npcOpeningUserText : promptPlayerText;
            }
            duelPostprocessSelected = duelRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "duel");
            rewardPostprocessSelected = rewardRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "reward");
            loanPostprocessSelected = loanRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "loan");
            kingdomServicePostprocessSelected = kingdomServiceRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "kingdom_service");
            kingdomVassalagePostprocessSelected = kingdomVassalageRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "kingdom_vassalage");
            kingdomAnnexationPostprocessSelected = false;
            lordsHallPostprocessSelected = lordsHallRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "lords_hall_access");
            meetingReleasePostprocessSelected = meetingReleaseRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "encounter_release_player");
            vanillaIssuePostprocessSelected = vanillaIssueRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "vanilla_issue");
            heroJoinPartyPostprocessSelected = kingdomServicePostprocessSelected;
            genericSceneMechanismPostprocessSelected = (genericSceneMechanismRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "scene_mechanism_actions"))
                && CanUseSceneMechanismPostprocessForSpeaker(nativeTargetAgentIndex)
                && (!AIConfigHandler.ShouldExcludeSceneMoveRuleForCurrentMission() || shouldRecordPlayerInput);
            bool noblePrisonerExecutionPostprocessSelected = shouldRecordPlayerInput && noblePrisonerExecutionRuleInjected;
            sceneMechanismPostprocessSelected = genericSceneMechanismPostprocessSelected || noblePrisonerExecutionPostprocessSelected;
            partyTransferPostprocessSelected = partyTransferRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "party_transfer");
            voteDealPostprocessSelected = voteDealRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "kingdom_agenda");
            customPolicyAgendaPostprocessSelected = shouldRecordPlayerInput && (customPolicyAgendaRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, CustomPolicyAgendaPostprocessRuleId));
            diplomacyPostprocessSelected = (shouldRecordPlayerInput && DiplomacyBehavior.CanUseIndependentClanPeaceForExternal(targetHero, targetCharacter))
                || ((diplomacyRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "diplomacy"))
                    && DiplomacyBehavior.CanUseDiplomacyActionPostprocessForExternal(targetHero, targetCharacter));
            worldMapPartyCommandPostprocessSelected = worldMapPartyCommandRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "worldmap_party_command");
            marriagePostprocessSelected = HasPreprocessRuleHit(postprocessPreprocessHits, "marriage");
            siegeInterventionPostprocessSelected = AfGcczShoutBridge.ShouldContinuePostprocess(siegeInterventionRuleInjected, postprocessPreprocessHits);
            directNoblePrisonerConversation = shouldRecordPlayerInput && noblePrisonerExecutionRuleInjected;
            if (directNoblePrisonerConversation)
            {
                siegeInterventionPostprocessSelected = false;
            }
        }

        private void CapturePostprocessTargets()
        {
            nativeDuelStakeOptions = null;
            if (duelPostprocessSelected && nativeDuelTargetHero != null && RewardSystemBehavior.Instance != null)
            {
                nativeDuelStakeOptions = RewardSystemBehavior.Instance.BuildDuelStakeOptionsForAI(nativeDuelTargetHero);
            }
            nativeSceneMechanismPostprocessRules = sceneMechanismPostprocessSelected
                ? _owner.BuildRuntimeSceneMechanismPostprocessRulesForScene(
                    npc,
                    nativeSceneSummonTargets,
                    nativeSceneGuideTargets,
                    includeGenericRules: genericSceneMechanismPostprocessSelected)
                : null;
            runtimeTargetKingdomId = ResolveCourierRuntimeTargetKingdomId(targetHero, targetCharacter);
            runtimeTargetHeroId = (targetHero?.StringId ?? targetCharacter?.HeroObject?.StringId ?? "").Trim();
            runtimeTargetCharacterId = (targetCharacter?.StringId ?? "").Trim();
            runtimeTargetTroopId = runtimeTargetCharacterId.ToLowerInvariant();
            runtimeTargetUnnamedRank = (targetHero == null && targetCharacter != null) ? (targetCharacter.IsSoldier ? "soldier" : "commoner") : "";
        }

        private void CaptureDirectSceneCommand()
        {
                bool directSceneCommandHandled;
                if (AfGcczShoutBridge.TryProcessDirectSceneCommand(nativeTargetAgentIndex, promptPlayerText, replyIsDirectPlayerResponse: true, out directSceneCommandHandled) && directSceneCommandHandled)
                {
                    siegeInterventionPostprocessSelected = false;
                    Logger.Log("ShoutBehavior", "[NativeConversation] GCCZ direct scene command handled target=" + nativeTargetLog);
                }
        }
    }
}
