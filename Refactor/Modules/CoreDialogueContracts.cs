using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AnimusForge.Refactor.Modules;

// Internal service contracts never depend on Api.V1 or carry engine objects.
internal enum CoreDialogueChannel { Native, Scene, Courier }
internal enum CoreDialogueState { Queued, Running, Completed, Rejected, Cancelled, Failed }
internal enum CoreDialogueEffectState { NoConfirmedEffect, UnknownAfterStart, CompletedByOwner }
internal enum CoreDialogueCancelResult { CancelledBeforeStart, AlreadyTerminal, TooLate }

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
        IReadOnlyList<CoreSceneUtterance> sceneUtterances = null)
    {
        ClientId = clientId; RequestId = requestId; State = state;
        Effects = effects; ReasonCode = reason; Reply = reply ?? "";
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
}
