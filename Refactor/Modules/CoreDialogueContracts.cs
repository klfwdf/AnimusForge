using System;

namespace AnimusForge.Refactor.Modules;

// Internal service contracts never depend on Api.V1 or carry engine objects.
internal enum CoreDialogueState { Queued, Running, Completed, Rejected, Cancelled, Failed }
internal enum CoreDialogueEffectState { NoConfirmedEffect, UnknownAfterStart, CompletedByOwner }
internal enum CoreDialogueCancelResult { CancelledBeforeStart, AlreadyTerminal, TooLate }

internal sealed class CoreDialogueResult
{
    internal CoreDialogueResult(string clientId, string requestId, CoreDialogueState state,
        CoreDialogueEffectState effects, string reason, string reply = "")
    {
        ClientId = clientId; RequestId = requestId; State = state;
        Effects = effects; ReasonCode = reason; Reply = reply ?? "";
    }
    internal string ClientId { get; }
    internal string RequestId { get; }
    internal CoreDialogueState State { get; }
    internal CoreDialogueEffectState Effects { get; }
    internal string ReasonCode { get; }
    internal string Reply { get; }
}
