using AnimusForge.Api.V1;
using AnimusForge.Refactor.Modules;

namespace AnimusForge.Api.Internal;

// Explicit mapping isolates the published V1 values from internal enum layout changes.
internal static class AfV1DialogueProjection
{
    internal static AfDialogueState State(CoreDialogueState value)
    {
        switch (value)
        {
            case CoreDialogueState.Queued: return AfDialogueState.Queued;
            case CoreDialogueState.Running: return AfDialogueState.Running;
            case CoreDialogueState.Completed: return AfDialogueState.Completed;
            case CoreDialogueState.Rejected: return AfDialogueState.Rejected;
            case CoreDialogueState.Cancelled: return AfDialogueState.Cancelled;
            default: return AfDialogueState.Failed;
        }
    }
    internal static AfDialogueEffectState Effects(CoreDialogueEffectState value)
    {
        switch (value)
        {
            case CoreDialogueEffectState.NoConfirmedEffect: return AfDialogueEffectState.NoConfirmedEffect;
            case CoreDialogueEffectState.CompletedByOwner: return AfDialogueEffectState.CompletedByOwner;
            default: return AfDialogueEffectState.UnknownAfterStart;
        }
    }
    internal static AfDialogueCancelResult Cancellation(CoreDialogueCancelResult value)
    {
        switch (value)
        {
            case CoreDialogueCancelResult.CancelledBeforeStart: return AfDialogueCancelResult.CancelledBeforeStart;
            case CoreDialogueCancelResult.AlreadyTerminal: return AfDialogueCancelResult.AlreadyTerminal;
            default: return AfDialogueCancelResult.TooLate;
        }
    }
}
