using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnimusForge;

// One Native phase boundary, not a second provider or public sub-MOD interface.
internal interface INativeConversationMainReplyHost
{
    Task<string> GenerateAsync(List<object> messages, Action<string> onStreamText);
    bool IsGenerationStale();
    string BuildStaleErrorText();
    Task<NativeConversationReplyTargetValidation> ValidateTargetAsync();
    Task RollbackPendingPlayerHistoryAsync(string reason);
    string PreparePostprocessReply(string output);
    void ReportProviderFailure(string output);
}

internal readonly struct NativeConversationReplyTargetValidation
{
    internal NativeConversationReplyTargetValidation(bool isCurrent, string reason)
    { IsCurrent = isCurrent; Reason = reason; }
    internal bool IsCurrent { get; }
    internal string Reason { get; }
}

internal enum NativeConversationMainReplyStatus
{
    NotStarted, // default result must not authorize downstream raw actions.
    Ready,
    StaleGeneration,
    TargetUnavailable,
    EmptyReply,
    ProviderFailure
}

internal readonly struct NativeConversationMainReplyResult
{
    internal NativeConversationMainReplyResult(NativeConversationMainReplyStatus status, string text)
    {
        Status = status;
        PostprocessReply = status == NativeConversationMainReplyStatus.Ready ? text : "";
        StopText = status == NativeConversationMainReplyStatus.Ready ? "" : text;
    }
    internal NativeConversationMainReplyStatus Status { get; }
    internal bool CanContinue => Status == NativeConversationMainReplyStatus.Ready;
    internal string PostprocessReply { get; }
    internal string StopText { get; }
}
