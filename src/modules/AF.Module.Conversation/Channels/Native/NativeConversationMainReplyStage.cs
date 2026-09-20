using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AnimusForge;

/// <summary>
/// Owns main-reply acceptance: normalize the completed response, reject stale context,
/// revoke only this request's pending input when the target is unavailable, and stop
/// empty/provider-error replies before raw actions. No game objects or prompt assembly.
/// </summary>
internal static class NativeConversationMainReplyStage
{
    internal static async Task<NativeConversationMainReplyResult> RunAsync(
        INativeConversationMainReplyHost host, List<object> messages, Action<string> onStreamText,
        string npcName, string nativeTargetLog, int nativeTargetAgentIndex, Stopwatch nativeTurnSw)
    {
        Stopwatch nativeMainApiSw = Stopwatch.StartNew();
        FreezeWatchdog.Mark("NativeConversation.main_reply_start", "target=" + (npcName ?? "unknown") + " agent=" + nativeTargetAgentIndex + " messages=" + messages.Count, immediate: true);
        string output = await host.GenerateAsync(messages, onStreamText).ConfigureAwait(false);
        output = LlmVisibleReplyNormalizer.NormalizeComplete(output);
        nativeMainApiSw.Stop();
        Logger.Log("Logic", "[NativePerf] main_reply_done target=" + (npcName ?? "unknown") + " agent=" + nativeTargetAgentIndex + " outputLen=" + ((output ?? "").Length) + " apiMs=" + Math.Round(nativeMainApiSw.Elapsed.TotalMilliseconds, 2) + " elapsedMs=" + Math.Round(nativeTurnSw.Elapsed.TotalMilliseconds, 2));
        FreezeWatchdog.Mark("NativeConversation.main_reply_done", "target=" + (npcName ?? "unknown") + " agent=" + nativeTargetAgentIndex + " outputLen=" + ((output ?? "").Length) + " apiMs=" + Math.Round(nativeMainApiSw.Elapsed.TotalMilliseconds, 2), immediate: true);
        if (host.IsGenerationStale())
            return new NativeConversationMainReplyResult(NativeConversationMainReplyStatus.StaleGeneration, host.BuildStaleErrorText());

        // The main-thread check precedes even empty/error handling, exactly as in the old flow.
        NativeConversationReplyTargetValidation validation = await host.ValidateTargetAsync().ConfigureAwait(false);
        if (!validation.IsCurrent)
        {
            string reason = string.IsNullOrWhiteSpace(validation.Reason) ? "main_thread_validation_failed" : validation.Reason;
            await host.RollbackPendingPlayerHistoryAsync(reason).ConfigureAwait(false);
            Logger.Log("ShoutBehavior", "[NativeConversation] dropped main reply because target is unavailable target=" + nativeTargetLog + " agentIndex=" + nativeTargetAgentIndex + " reason=" + reason);
            return new NativeConversationMainReplyResult(NativeConversationMainReplyStatus.TargetUnavailable, "");
        }
        if (string.IsNullOrWhiteSpace(output))
            return new NativeConversationMainReplyResult(NativeConversationMainReplyStatus.EmptyReply, "");
        if (output.StartsWith("（错误") || output.StartsWith("（程序错误") || output.StartsWith("（API请求失败") || output.StartsWith("（API响应格式错误"))
        {
            host.ReportProviderFailure(output.Trim());
            return new NativeConversationMainReplyResult(NativeConversationMainReplyStatus.ProviderFailure, output.Trim());
        }
        return new NativeConversationMainReplyResult(NativeConversationMainReplyStatus.Ready,
            host.PreparePostprocessReply(output));
    }
}
