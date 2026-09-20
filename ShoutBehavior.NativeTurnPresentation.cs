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
        private string postprocessReply;
        private string cleaned;
        private bool nativeTtsDispatchedBeforePostprocess;

        public async Task<NativeConversationTurnStep> ReceiveAndPresentAsync()
        {
            NativeConversationMainReplyResult nativeMainReply = await NativeConversationMainReplyStage.RunAsync(
                new NativeConversationMainReplyHost(_owner, admission, nativeTargetLog,
                    nativePendingAfefKey, nativePendingPlayerHistoryEventSequence),
                messages, onStreamText, npcName, nativeTargetLog, nativeTargetAgentIndex, nativeTurnSw).ConfigureAwait(false);
            if (!nativeMainReply.CanContinue) return NativeConversationTurnStep.Stop(nativeMainReply.StopText);
            postprocessReply = nativeMainReply.PostprocessReply;
            cleaned = "";
            string nativeMainVisibleForTts = "";
            nativeTtsDispatchedBeforePostprocess = false;
            string nativeMainReplyTargetUnavailableReason = "";
            bool nativeMainReplyTargetAvailable = await _owner.RunNativeConversationMainThreadFuncAsync(
                "main_reply_action_validation",
                nativeTargetLog,
                nativeTargetAgentIndex,
                () =>
                {
                    if (!_owner.IsNativeConversationAdmissionCurrent(admission, out nativeMainReplyTargetUnavailableReason))
                    {
                        return false;
                    }
                    TryProcessNativeConversationRawMeetingTauntTags(targetHero, targetCharacter, nativeTargetAgentIndex, ref postprocessReply, out var nativeRawMeetingTauntEscalated);
                    if (TryProcessNativeConversationSceneTauntTags(targetHero, targetCharacter, nativeTargetAgentIndex, ref postprocessReply, out var nativeRawSceneTauntEscalated) && string.IsNullOrWhiteSpace(postprocessReply))
                    {
                        postprocessReply = BuildFallbackSceneTauntSpeech(nativeRawSceneTauntEscalated);
                    }
                    // The observer reads Mission/Agents. Keep it with the validated raw actions on
                    // their owning thread, before this callback releases the worker continuation.
                    SubmitNativeConversationSceneActionObservation(postprocessReply, nativeTargetAgentIndex);
                    cleaned = StripStageDirectionsForPassiveShout(postprocessReply);
                    nativeMainVisibleForTts = SanitizeSceneSpeechText(cleaned);
                    if (nativeTargetAgentIndex < 0 && !string.IsNullOrWhiteSpace(nativeMainVisibleForTts) && !IsNativeConversationNoSpeechPlaceholder(nativeMainVisibleForTts))
                    {
                        _owner.TrySpeakNativeConversationReplyWithTts(targetHero, targetCharacter, npc, nativeTargetAgentIndex, nativeMainVisibleForTts);
                        nativeTtsDispatchedBeforePostprocess = true;
                        _owner.LogTtsReport("NativeConversationTts.EarlyDispatchBeforePostprocess", nativeTargetAgentIndex, $"uiLen={nativeMainVisibleForTts.Length};target={(targetHero?.StringId ?? targetCharacter?.StringId ?? npc?.Name ?? "unknown")}");
                    }
                    return true;
                },
                false).ConfigureAwait(false);
            if (!nativeMainReplyTargetAvailable)
            {
                string reason = string.IsNullOrWhiteSpace(nativeMainReplyTargetUnavailableReason) ? "main_thread_validation_failed" : nativeMainReplyTargetUnavailableReason;
                await _owner.RollbackNativeConversationPendingPlayerHistoryAsync(admission, nativePendingAfefKey, nativePendingPlayerHistoryEventSequence, reason).ConfigureAwait(false);
                Logger.Log("ShoutBehavior", "[NativeConversation] dropped main reply before postprocess because target is unavailable target=" + nativeTargetLog + " agentIndex=" + nativeTargetAgentIndex + " reason=" + reason);
                return NativeConversationTurnStep.Stop("");
            }
            // Keep role-play action prose for the postprocessor; display/TTS retains the
            // sanitized display/TTS variants captured in the validated phase above.
            string nativePostprocessStartTargetUnavailableReason = "";
            bool nativePostprocessStartTargetAvailable = await _owner.RunNativeConversationMainThreadFuncAsync(
                "postprocess_start_target_validation",
                nativeTargetLog,
                nativeTargetAgentIndex,
                () => _owner.IsNativeConversationAdmissionCurrent(admission, out nativePostprocessStartTargetUnavailableReason),
                false).ConfigureAwait(false);
            if (!nativePostprocessStartTargetAvailable)
            {
                string reason = string.IsNullOrWhiteSpace(nativePostprocessStartTargetUnavailableReason) ? "main_thread_validation_failed" : nativePostprocessStartTargetUnavailableReason;
                await _owner.RollbackNativeConversationPendingPlayerHistoryAsync(admission, nativePendingAfefKey, nativePendingPlayerHistoryEventSequence, reason).ConfigureAwait(false);
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
    }
}
