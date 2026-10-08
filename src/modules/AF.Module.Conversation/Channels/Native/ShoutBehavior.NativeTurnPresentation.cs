using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AnimusForge.SceneActions.Core;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;
using System.Linq;
using RichExecutions.Core;
using RichExecutions.Scene;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.MountAndBlade;

using static AnimusForge.ShoutBehavior;

namespace AnimusForge;

    // Native game adapter; phase ordering belongs to NativeConversationTurnCoordinator.
    internal sealed partial class NativeConversationTurnRuntime : INativeConversationTurnHost
    {
        // Request-local phase output, published only after its awaited capture completes.
        private string postprocessReply;
        private string cleaned;
        private bool nativeTtsDispatchedBeforePostprocess;
        public async Task<NativeConversationTurnStep> ReceiveAndPresentAsync()
        {
            NativeConversationMainReplyResult nativeMainReply = await NativeConversationMainReplyStage.RunAsync(
                new NativeConversationMainReplyRuntime(_ports, admission, nativeTargetLog,
                    nativePendingAfefKey, nativePendingPlayerHistoryEventSequence),
                messages, onStreamText, npcName, nativeTargetLog, nativeTargetAgentIndex, nativeTurnSw).ConfigureAwait(false);
            if (!nativeMainReply.CanContinue) return NativeConversationTurnStep.Stop(nativeMainReply.StopText);
            postprocessReply = nativeMainReply.PostprocessReply;
            cleaned = "";
            string nativeMainVisibleForTts = "";
            nativeTtsDispatchedBeforePostprocess = false;
            string nativeMainReplyTargetUnavailableReason = "";
            bool nativeMainReplyTargetAvailable = await _ports.RunNativeConversationMainThreadFuncAsync(
                "main_reply_action_validation",
                nativeTargetLog,
                nativeTargetAgentIndex,
                () =>
                {
                    if (!_ports.IsNativeConversationAdmissionCurrent(admission, out nativeMainReplyTargetUnavailableReason))
                    {
                        return false;
                    }
                    // The main-reply stage accepted a nonempty raw reply. Retire
                    // an opening retry before its raw tags can apply any effects,
                    // even when sanitizing those tags leaves no visible text.
                    _ports.RetireOpeningForAcceptedReply?.Invoke(admission);
                    _ports.ConfirmMeetingElapsedBoundary?.Invoke(admission);
                    TryProcessNativeConversationRawMeetingTauntTags(targetHero, targetCharacter, nativeTargetAgentIndex, ref postprocessReply, out var nativeRawMeetingTauntEscalated);
                    if (TryProcessNativeConversationSceneTauntTags(targetHero, targetCharacter, nativeTargetAgentIndex, ref postprocessReply, out var nativeRawSceneTauntEscalated) && string.IsNullOrWhiteSpace(postprocessReply))
                    {
                        postprocessReply = BuildFallbackSceneTauntSpeech(nativeRawSceneTauntEscalated);
                    }
                    // The observer reads Mission/Agents. Keep it with the validated raw actions on
                    // their owning thread, before this callback releases the worker continuation.
                    RecordSceneActionReplyCapture(nativeTargetAgentIndex);
                    SubmitNativeConversationSceneActionObservation(postprocessReply, nativeTargetAgentIndex);
                    // Only record the intent here; the order is armed after the reply is accepted.
                    cleaned = StripStageDirectionsForPassiveShout(postprocessReply);
                    nativeMainVisibleForTts = SanitizeSceneSpeechText(cleaned);
                    if (nativeTargetAgentIndex < 0 && !string.IsNullOrWhiteSpace(nativeMainVisibleForTts) && !IsNativeConversationNoSpeechPlaceholder(nativeMainVisibleForTts))
                    {
                        _ports.TrySpeakNativeConversationReplyWithTts(targetHero, targetCharacter, npc, nativeTargetAgentIndex, nativeMainVisibleForTts);
                        nativeTtsDispatchedBeforePostprocess = true;
                        _ports.LogTtsReport("NativeConversationTts.EarlyDispatchBeforePostprocess", nativeTargetAgentIndex, $"uiLen={nativeMainVisibleForTts.Length};target={(targetHero?.StringId ?? targetCharacter?.StringId ?? npc?.Name ?? "unknown")}");
                    }
                    return true;
                },
                false).ConfigureAwait(false);
            if (!nativeMainReplyTargetAvailable)
            {
                string reason = string.IsNullOrWhiteSpace(nativeMainReplyTargetUnavailableReason) ? "main_thread_validation_failed" : nativeMainReplyTargetUnavailableReason;
                await _ports.RollbackNativeConversationPendingPlayerHistoryAsync(admission, nativePendingAfefKey, nativePendingPlayerHistoryEventSequence, reason).ConfigureAwait(false);
                Logger.Log("ShoutBehavior", "[NativeConversation] dropped main reply before postprocess because target is unavailable target=" + nativeTargetLog + " agentIndex=" + nativeTargetAgentIndex + " reason=" + reason);
                return NativeConversationTurnStep.Stop("");
            }
            // Keep role-play action prose for the postprocessor; display/TTS retains the
            // sanitized display/TTS variants captured in the validated phase above.
            string nativePostprocessStartTargetUnavailableReason = "";
            bool nativePostprocessStartTargetAvailable = await _ports.RunNativeConversationMainThreadFuncAsync(
                "postprocess_start_target_validation",
                nativeTargetLog,
                nativeTargetAgentIndex,
                () => _ports.IsNativeConversationAdmissionCurrent(admission, out nativePostprocessStartTargetUnavailableReason),
                false).ConfigureAwait(false);
            if (!nativePostprocessStartTargetAvailable)
            {
                string reason = string.IsNullOrWhiteSpace(nativePostprocessStartTargetUnavailableReason) ? "main_thread_validation_failed" : nativePostprocessStartTargetUnavailableReason;
                await _ports.RollbackNativeConversationPendingPlayerHistoryAsync(admission, nativePendingAfefKey, nativePendingPlayerHistoryEventSequence, reason).ConfigureAwait(false);
                Logger.Log("ShoutBehavior", "[NativeConversation] skipped postprocess because target is unavailable target=" + nativeTargetLog + " agentIndex=" + nativeTargetAgentIndex + " reason=" + reason);
                return NativeConversationTurnStep.Stop("");
            }
            try
            {
                // Only the completed, normalized main reply is safe to linkify; streamed fragments remain plain text.
                onMainReplyReady?.Invoke(nativeMainVisibleForTts, targetHero, targetCharacter);
                Logger.Log("Logic", "[NativePerf] main_reply_display_ready target=" + (npcName ?? "unknown") + " agent=" + nativeTargetAgentIndex + " visibleLen=" + nativeMainVisibleForTts.Length + " elapsedMs=" + Math.Round(nativeTurnSw.Elapsed.TotalMilliseconds, 2));
            }
            catch (Exception ex)
            {
                Logger.Log("ShoutBehavior", "[NativeConversation] main-reply-ready callback failed: " + ex.Message);
            }
            try
            {
                // Reuse the name captured during prompt assembly; a cold non-Hero name
                // cache otherwise reads Culture/NameGenerator on this worker callback.
                string postprocessNpcName = nativeHistoryDisplayName;
                onPostprocessStarted?.Invoke(string.IsNullOrWhiteSpace(postprocessNpcName) ? npcName : postprocessNpcName);
            }
            catch (Exception ex)
            {
                Logger.Log("ShoutBehavior", "[NativeConversation] postprocess-start callback failed: " + ex.Message);
            }
            return NativeConversationTurnStep.Continue();
        }

        // The ceremony cannot move its actors while this conversation still owns them.
        // An explicit order is therefore kept until the conversation closes, then it
        // uses the same executioner-start path as the scripted "Proceed" line.
    }
