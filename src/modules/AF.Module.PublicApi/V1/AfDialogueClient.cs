using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AnimusForge.Refactor.Modules;
using AnimusForge.Api.Internal;

namespace AnimusForge.Api.V1;

public enum AfDialogueState { Queued = 0, Running = 1, Completed = 2, Rejected = 3, Cancelled = 4, Failed = 5 }
public enum AfDialogueEffectState { NoConfirmedEffect = 0, UnknownAfterStart = 1, CompletedByOwner = 2 }
public enum AfDialogueCancelResult { CancelledBeforeStart = 0, AlreadyTerminal = 1, TooLate = 2 }

/// <summary>Scene-only detached utterance; the index is diagnostic, not an Agent handle.</summary>
public sealed class AfSceneUtterance
{
    public int SpeakerAgentIndex { get; }
    public string SpeakerName { get; }
    public string Text { get; }
    internal AfSceneUtterance(CoreSceneUtterance source)
    {
        SpeakerAgentIndex = source.SpeakerAgentIndex;
        SpeakerName = source.SpeakerName;
        Text = source.Text;
    }
}

/// <summary>Detached receipt; Completed means the existing owner's action/history phase returned, not TTS finished.</summary>
public sealed class AfDialogueResult
{
    public int ContractVersion => 1;
    public string ClientId { get; }
    public string RequestId { get; }
    public AfDialogueState State { get; }
    public AfDialogueEffectState EffectState { get; }
    public string ReasonCode { get; }
    public string Reply { get; }
    public IReadOnlyList<AfSceneUtterance> SceneUtterances { get; }
    // Even failed turns can have already executed raw actions. Retry only with the same client + ID.
    public bool CanRetryAutomatically => false;

    internal AfDialogueResult(CoreDialogueResult source)
    {
        ClientId = source.ClientId; RequestId = source.RequestId;
        State = AfV1DialogueProjection.State(source.State); EffectState = AfV1DialogueProjection.Effects(source.Effects);
        ReasonCode = source.ReasonCode; Reply = source.Reply;
        var utterances = new List<AfSceneUtterance>(source.SceneUtterances.Count);
        foreach (CoreSceneUtterance utterance in source.SceneUtterances) utterances.Add(new AfSceneUtterance(utterance));
        SceneUtterances = new ReadOnlyCollection<AfSceneUtterance>(utterances);
    }
}

/// <summary>Caller-owned receipt and cancellation handle. No callbacks run inside game/registry locks.</summary>
public sealed class AfDialogueOperation
{
    private readonly CoreDialogueOperation _operation;
    internal AfDialogueOperation(CoreDialogueOperation operation)
    {
        _operation = operation;
        Completion = ProjectCompletion(operation.Completion);
    }
    public Task<AfDialogueResult> Completion { get; }
    public AfDialogueResult GetSnapshot() => new AfDialogueResult(_operation.Snapshot);
    public AfDialogueCancelResult Cancel() => AfV1DialogueProjection.Cancellation(_operation.Cancel());
    private static async Task<AfDialogueResult> ProjectCompletion(Task<CoreDialogueResult> completion)
        => new AfDialogueResult(await completion.ConfigureAwait(false));
}

/// <summary>
/// One isolated consumer namespace. Native uses the active AF conversation; Scene claims an AF-issued UI context ticket.
/// Keep this object and reuse a request ID for retries. A new client is a new namespace, not a retry.
/// </summary>
public sealed class AfDialogueClient : IDisposable
{
    private readonly CoreDialogueClient _client;
    internal AfDialogueClient(CoreDialogueClient client) { _client = client; }
    public string ClientId => _client.ClientId;
    public int MaximumRequests => CoreDialogueClient.MaximumRequests;
    public int MaximumTextLength => CoreDialogueClient.MaximumTextLength;
    /// <summary>绑定提交时的会话/呈现代次；目标由主线程准入时解析，不接受指定 NPC，也不宣称后台锁定 Hero。</summary>
    public AfDialogueOperation SubmitNative(string requestId, string playerText)
        => new AfDialogueOperation(_client.SubmitNative(requestId, playerText));
    /// <summary>Main-thread preview of the active Scene shout selection; null means no eligible context.</summary>
    public string CaptureSceneContextTicket() => _client.CaptureSceneContextTicket();
    /// <summary>Claim one opaque Scene ticket. Cancellation only wins before the host's main-thread claim.</summary>
    public AfDialogueOperation SubmitScene(string contextTicket, string requestId, string playerText)
        => new AfDialogueOperation(_client.SubmitScene(contextTicket, requestId, playerText));
    public void Dispose() => _client.Dispose();
}
