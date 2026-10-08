using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.ExceptionServices;
using AnimusForge.SceneActions.Core;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;

using static AnimusForge.ShoutBehavior;

namespace AnimusForge;

    // One instance per admitted turn. Opaque game references stay here; only guarded
    // game-thread callbacks may dereference them. The module owns stage progression.
    internal sealed partial class NativeConversationTurnRuntime : INativeConversationTurnHost
    {
        private readonly NativeConversationTurnPorts _ports;
        private readonly NativeConversationAdmission admission;
        private readonly Action<string> onStreamText;
        private readonly Action<string> onPostprocessStarted;
        private readonly Action<string, Hero, CharacterObject> onMainReplyReady;
        private readonly bool npcInitiatedOpening;
        private string playerText;
        private Stopwatch nativeTurnSw;
        private long runtimeGeneration;
        private Hero targetHero;
        private CharacterObject targetCharacter;
        private string npcName;
        private ConversationManager nativeRequestConversationManager;
        private int nativeRequestConversationToken;
        private string npcOpeningUserText;
        private string npcOpeningPersistentFactText;
        private bool npcOpeningConsumed;
        private string promptPlayerText;
        private string routingInput;
        private bool shouldRecordPlayerInput;
        private int nativeTargetAgentIndex;
        private NpcDataPacket npc;
        private string nativeTargetLog;
        private List<NpcDataPacket> presentNpcs;
        private string cultureId;
        private string currentNativeDialogText;
        private bool hadNativeConversationSessionHistoryBeforeTurn;
        private string extraFact;
        private string nativeMeetingTauntRuleBlock;
        private List<SceneSummonPromptTarget> nativeSceneSummonTargets;
        private List<SceneGuidePromptTarget> nativeSceneGuideTargets;
        private List<string> preprocessExcludedRuleIds;

        internal NativeConversationTurnRuntime(NativeConversationTurnPorts ports, NativeConversationAdmission admission,
            string playerText, Action<string> onStreamText, Action<string> onPostprocessStarted,
            Action<string, Hero, CharacterObject> onMainReplyReady, bool npcInitiatedOpening)
        {
            _ports = ports ?? throw new ArgumentNullException(nameof(ports));
            this.admission = admission;
            this.playerText = playerText;
            this.onStreamText = onStreamText;
            this.onPostprocessStarted = onPostprocessStarted;
            this.onMainReplyReady = onMainReplyReady;
            this.npcInitiatedOpening = npcInitiatedOpening;
        }

        // Capture and its validity test are one queue claim. A rejected callback never
        // reads world state; rollback is scoped to this turn's captured pending receipt.
        private async Task<bool> CaptureOnGameThreadAsync(string phase, Action capture)
        {
            string reason = "";
            ExceptionDispatchInfo failure = null;
            using ExecutionContext context = ExecutionContext.Capture();
            bool accepted = await _ports.RunNativeConversationMainThreadFuncAsync(phase,
                nativeTargetLog, nativeTargetAgentIndex, () =>
                {
                    if (!_ports.IsNativeConversationAdmissionCurrent(admission, out reason)) return false;
                    try
                    {
                        // Queue callbacks do not otherwise flow AsyncLocal prompt/guardrail state.
                        // Restore the game thread's previous context after this request's capture.
                        void Invoke()
                        {
                            using IDisposable scope = AIConfigHandler.BeginGuardrailRuntimeScope();
                            capture();
                        }
                        if (context == null) Invoke();
                        else ExecutionContext.Run(context, _ => Invoke(), null);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        // The generic queue returns fallback on ordinary exceptions. This turn
                        // must preserve an unknown outcome, not silently retry a partial effect.
                        failure = ExceptionDispatchInfo.Capture(ex);
                        return false;
                    }
                }, false).ConfigureAwait(false);
            failure?.Throw();
            if (!accepted && nativePendingAfefKey != null)
                await _ports.RollbackNativeConversationPendingPlayerHistoryAsync(admission,
                    nativePendingAfefKey, nativePendingPlayerHistoryEventSequence,
                    string.IsNullOrWhiteSpace(reason) ? "main_thread_capture_failed" : reason).ConfigureAwait(false);
            return accepted;
        }

        public async Task<NativeConversationTurnStep> PrepareAsync()
        {
            nativeTurnSw = Stopwatch.StartNew();
            runtimeGeneration = admission.Generation;
            playerText = (playerText ?? "").Replace("\r", "").Trim();
            if (!await _ports.RunNativeConversationMainThreadFuncAsync("admitted_request_start", admission.NpcName,
                admission.AgentIndex, () => _ports.IsNativeConversationAdmissionCurrent(admission, out _), false).ConfigureAwait(false))
                return NativeConversationTurnStep.Stop("");
            targetHero = admission.Hero;
            targetCharacter = admission.Character;
            npcName = admission.NpcName;
            nativeRequestConversationManager = admission.ConversationManager;
            nativeRequestConversationToken = admission.ConversationToken;
            Logger.Log("Logic", "[NativePerf] submit_start target=" + (npcName ?? "unknown") + " npcInitiated=" + npcInitiatedOpening + " inputLen=" + playerText.Length);
            FreezeWatchdog.Mark("NativeConversation.submit_start", "target=" + (npcName ?? "unknown") + " npcInitiated=" + npcInitiatedOpening + " inputLen=" + playerText.Length, immediate: true);
            string npcOpeningExtraFact = "";
            string npcOpeningPromptText = "";
            npcOpeningUserText = "";
            npcOpeningPersistentFactText = "";
            string npcOpeningSource = "";
            npcOpeningConsumed = false;
            if (npcInitiatedOpening)
            {
                npcOpeningConsumed = true;
                npcOpeningExtraFact = admission.OpeningExtraFact;
                npcOpeningPromptText = admission.OpeningPrompt;
                npcOpeningSource = admission.OpeningSource;
                npcOpeningUserText = BuildNpcInitiatedOpeningUserText(npcOpeningExtraFact, npcOpeningPromptText);
                npcOpeningPersistentFactText = BuildNpcInitiatedOpeningPersistentFactText(npcOpeningExtraFact);
                Logger.Log("ShoutBehavior", "[NativeConversation] NPC initiated opening consumed source=" + npcOpeningSource + " target=" + (npcName ?? "unknown"));
            }
            promptPlayerText = npcInitiatedOpening ? "" : playerText;
            routingInput = npcInitiatedOpening ? npcOpeningUserText : promptPlayerText;
            shouldRecordPlayerInput = !npcInitiatedOpening;
            FreezeWatchdog.Mark("NativeConversation.persona_start", "target=" + (npcName ?? "unknown"), immediate: true);
            bool personaReady = await _ports.EnsureNativeConversationPersonaReadyAsync(admission, onStreamText).ConfigureAwait(false);
            FreezeWatchdog.Mark("NativeConversation.persona_done", "target=" + (npcName ?? "unknown") + " ready=" + personaReady, immediate: true);
            if (SaveRuntimeGuard.IsStale(runtimeGeneration, "native_conversation_persona_ready"))
            {
                return NativeConversationTurnStep.Stop(SaveRuntimeGuard.BuildStaleRequestErrorText());
            }
            if (!personaReady)
            {
                return NativeConversationTurnStep.Stop("");
            }
            nativeTargetAgentIndex = admission.AgentIndex;
            string nativeInitialTargetUnavailableReason = "";
            NativeConversationPreparationSnapshot nativePreparation = await _ports.RunNativeConversationMainThreadFuncAsync(
                "request_target_validation",
                npcName,
                nativeTargetAgentIndex,
                () => _ports.CaptureNativeConversationPreparation(admission, targetHero, targetCharacter, npcName, routingInput, out nativeInitialTargetUnavailableReason),
                (NativeConversationPreparationSnapshot)null).ConfigureAwait(false);
            if (nativePreparation == null)
            {
                Logger.Log("ShoutBehavior", "[NativeConversation] skipped request because preparation is unavailable target=" + npcName + " agentIndex=" + nativeTargetAgentIndex + " reason=" + (string.IsNullOrWhiteSpace(nativeInitialTargetUnavailableReason) ? "main_thread_preparation_failed" : nativeInitialTargetUnavailableReason));
                return NativeConversationTurnStep.Stop("");
            }
            npc = nativePreparation.Npc;
            nativeTargetLog = nativePreparation.TargetLog;
            presentNpcs = nativePreparation.PresentNpcs;
            cultureId = nativePreparation.CultureId;
            // Do not feed vanilla conversation UI text into AF prompt history.
            currentNativeDialogText = "";
            hadNativeConversationSessionHistoryBeforeTurn = nativePreparation.HadSessionHistory;
            extraFact = npcOpeningPersistentFactText;
            nativeMeetingTauntRuleBlock = nativePreparation.MeetingTauntRuleBlock;
            nativeSceneSummonTargets = nativePreparation.SummonTargets;
            nativeSceneGuideTargets = nativePreparation.GuideTargets;
            preprocessExcludedRuleIds = nativePreparation.ExcludedRuleIds;
            return NativeConversationTurnStep.Continue();
        }
    }

public partial class ShoutBehavior
{
    private NativeConversationTurnPorts CreateNativeConversationTurnPorts() => new NativeConversationTurnPorts
    {
        ApplyNativeConversationGameActionsOnMainThreadAsync = _nativeGameEffects.ApplyNativeConversationGameActionsOnMainThreadAsync,
        RetireOpeningForAcceptedReply = NativeAdmissions.RetireOpeningForAcceptedReply,
        PromptDispatcher = _conversationGameThreadDispatcher,
        IsPromptOwnerAvailable = static () => MyBehavior.Instance != null,
        CapturePromptRoutingWork = static request =>
        {
            MyBehavior owner = MyBehavior.Instance;
            if (owner == null) return null;
            PromptBuildPhases phases = owner.BeginSharedPromptBuild(request.Hero, request.Input, request.ExtraFact, request.CultureId, request.HasAnyHero, request.Character, null, request.AgentIndex,
                suppressDynamicRuleAndLore: false, usePrefetchedLoreContext: false, prefetchedLoreContext: null,
                excludedRuleIds: null, preprocessExcludedRuleIds: request.ExcludedRules, forcedPreprocessRuleIds: null, preprocessMentionedEntities: null);
            return owner.CaptureSharedPromptRoutingWork(phases);
        },
        CapturePromptKnowledge = static (phases, hero) =>
        {
            MyBehavior owner = MyBehavior.Instance;
            if (owner == null) return null;
            owner.CaptureSharedKnowledgeSnapshot(phases, hero);
            return MyBehavior.CreateSharedKnowledgeWorkInput(phases);
        },
        CompletePromptCapture = static (phases, hero, character, weekly) => MyBehavior.Instance?.CompleteSharedPromptBuild(phases, hero, character, weekly),
        BuildRuntimeSceneMechanismPostprocessRulesForScene = BuildRuntimeSceneMechanismPostprocessRulesForScene,
        BuildSceneFollowControlPromptInstruction = _sceneMovement.BuildSceneFollowControlPromptInstruction,
        BuildSceneSummonClosurePromptInstruction = _sceneMovement.BuildSceneSummonClosurePromptInstruction,
        BuildStrictSceneMessagesForNpc = BuildStrictSceneMessagesForNpc,
        CaptureNativeConversationPreparation = CaptureNativeConversationPreparation,
        EnsureNativeConversationPersonaReadyAsync = EnsureNativeConversationPersonaReadyAsync,
        IsNativeConversationAdmissionCurrent = IsNativeConversationAdmissionCurrent,
        LogTtsReport = LogTtsReport,
        PrepareNativeConversationPendingHistoryAsync = PrepareNativeConversationPendingHistoryAsync,
        RollbackNativeConversationPendingPlayerHistoryAsync = RollbackNativeConversationPendingPlayerHistoryAsync,
        TrySpeakNativeConversationReplyWithTts = TrySpeakNativeConversationReplyWithTts,
        DispatchValidation = RunNativeConversationMainThreadFuncAsync,
        DispatchPreparation = RunNativeConversationMainThreadFuncAsync,
        DispatchHistoryWork = RunNativeConversationMainThreadFuncAsync,
        DispatchWeeklySnapshot = RunNativeConversationMainThreadFuncAsync,
    };
}
