using AnimusForge.Api.V1;
using AnimusForge.Refactor.Modules;

namespace AnimusForge.Api.Internal;

// Explicit mapping isolates the published V1 values from internal enum layout changes.
internal static class AfV1DialogueProjection
{
    internal static AfCourierReceipt Courier(CoreCourierReceipt source)
    {
        if (source == null) return null;
        return new AfCourierReceipt(
            transport: CourierTransport(source.Transport),
            dispatched: source.Has(CoreCourierAcceptedSteps.Dispatched),
            replyPrepared: source.Has(CoreCourierAcceptedSteps.ReplyPrepared),
            arrived: source.Has(CoreCourierAcceptedSteps.Arrived),
            payloadAccepted: source.Has(CoreCourierAcceptedSteps.Payload),
            deliveryHistoryAccepted: source.Has(CoreCourierAcceptedSteps.DeliveryHistory),
            actionsAccepted: source.Has(CoreCourierAcceptedSteps.Actions),
            replyHistoryAccepted: source.Has(CoreCourierAcceptedSteps.ReplyHistory),
            replyDelivered: source.Has(CoreCourierAcceptedSteps.ReplyDelivered),
            contentsReturned: source.Has(CoreCourierAcceptedSteps.ContentsReturned));
    }

    private static AfCourierTransportOutcome CourierTransport(CoreCourierTransportOutcome value)
    {
        switch (value)
        {
            case CoreCourierTransportOutcome.NotStarted: return AfCourierTransportOutcome.NotStarted;
            case CoreCourierTransportOutcome.InTransit: return AfCourierTransportOutcome.InTransit;
            case CoreCourierTransportOutcome.Returned: return AfCourierTransportOutcome.Returned;
            case CoreCourierTransportOutcome.Destroyed: return AfCourierTransportOutcome.Destroyed;
            case CoreCourierTransportOutcome.Missing: return AfCourierTransportOutcome.Missing;
            default: return AfCourierTransportOutcome.Unconfirmed;
        }
    }

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
