using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AnimusForge.Refactor.Modules;

// Internal service contracts never depend on Api.V1 or carry engine objects.
internal enum CoreDialogueChannel { Native, Scene, Courier }
internal enum CoreDialogueState { Queued, Running, Completed, Rejected, Cancelled, Failed }
internal enum CoreDialogueEffectState { NoConfirmedEffect, UnknownAfterStart, CompletedByOwner }
internal enum CoreDialogueCancelResult { CancelledBeforeStart, AlreadyTerminal, TooLate }

[Flags]
internal enum CoreCourierAcceptedSteps
{
    None = 0, Dispatched = 1, ReplyPrepared = 2, Arrived = 4, Payload = 8,
    DeliveryHistory = 16, Actions = 32, ReplyHistory = 64, ReplyDelivered = 128, ContentsReturned = 256
}
internal enum CoreCourierTransportOutcome { NotStarted, InTransit, Returned, Destroyed, Missing, Unconfirmed }

// Independent stages: a reply may be prepared before arrival, but is not public until delivery.
internal sealed class CoreCourierReceipt
{
    internal CoreCourierReceipt(CoreCourierAcceptedSteps accepted, CoreCourierTransportOutcome transport)
    { Accepted = accepted; Transport = transport; }
    internal CoreCourierAcceptedSteps Accepted { get; }
    internal CoreCourierTransportOutcome Transport { get; }
    internal bool Has(CoreCourierAcceptedSteps step) => (Accepted & step) == step;
    internal bool Complete => Transport == CoreCourierTransportOutcome.Returned
        && Has(CoreCourierAcceptedSteps.Dispatched | CoreCourierAcceptedSteps.ReplyPrepared
            | CoreCourierAcceptedSteps.Arrived | CoreCourierAcceptedSteps.Payload | CoreCourierAcceptedSteps.DeliveryHistory
            | CoreCourierAcceptedSteps.Actions | CoreCourierAcceptedSteps.ReplyHistory
            | CoreCourierAcceptedSteps.ReplyDelivered | CoreCourierAcceptedSteps.ContentsReturned);
}

internal sealed class CoreSceneUtterance
{
    internal CoreSceneUtterance(int speakerAgentIndex, string speakerName, string text)
    {
        SpeakerAgentIndex = speakerAgentIndex;
        SpeakerName = speakerName ?? string.Empty;
        Text = text ?? string.Empty;
    }
    internal int SpeakerAgentIndex { get; }
    internal string SpeakerName { get; }
    internal string Text { get; }
}

internal sealed class CoreDialogueResult
{
    internal CoreDialogueResult(string clientId, string requestId, CoreDialogueState state,
        CoreDialogueEffectState effects, string reason, string reply = "",
        IReadOnlyList<CoreSceneUtterance> sceneUtterances = null, CoreCourierReceipt courier = null)
    {
        ClientId = clientId; RequestId = requestId; State = state;
        Effects = effects; ReasonCode = reason; Reply = reply ?? "";
        Courier = courier;
        SceneUtterances = new ReadOnlyCollection<CoreSceneUtterance>(
            new List<CoreSceneUtterance>(sceneUtterances ?? Array.Empty<CoreSceneUtterance>()));
    }
    internal string ClientId { get; }
    internal string RequestId { get; }
    internal CoreDialogueState State { get; }
    internal CoreDialogueEffectState Effects { get; }
    internal string ReasonCode { get; }
    internal string Reply { get; }
    internal IReadOnlyList<CoreSceneUtterance> SceneUtterances { get; }
    internal CoreCourierReceipt Courier { get; }
}
