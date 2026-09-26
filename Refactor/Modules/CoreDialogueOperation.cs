using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnimusForge.Refactor.Modules;

/// <summary>
/// One caller-owned operation. Cancellation only wins before the real main-thread owner claims it.
/// A late cancellation/disposal cannot replace an actual action/history receipt or promise rollback.
/// </summary>
internal sealed class CoreDialogueOperation
{
    private readonly object _gate = new object();
    private readonly TaskCompletionSource<CoreDialogueResult> _completion =
        new TaskCompletionSource<CoreDialogueResult>(TaskCreationOptions.RunContinuationsAsynchronously);
    private CoreDialogueResult _snapshot;
    private bool _ownerAdmitted;
    private string _confirmedReply;
    private bool _ownerCompleted;
    private IReadOnlyList<CoreSceneUtterance> _sceneUtterances = Array.Empty<CoreSceneUtterance>();

    internal CoreDialogueOperation(string clientId, string requestId, string playerText,
        CoreDialogueChannel channel = CoreDialogueChannel.Native, string contextIdentity = "")
    {
        ClientId = clientId; RequestId = requestId; PlayerText = playerText;
        Channel = channel; ContextIdentity = contextIdentity;
        _snapshot = Result(CoreDialogueState.Queued, CoreDialogueEffectState.NoConfirmedEffect, "dialogue.queued");
    }
    internal string ClientId { get; }
    internal string RequestId { get; }
    internal string PlayerText { get; }
    internal CoreDialogueChannel Channel { get; }
    internal string ContextIdentity { get; }
    internal Task<CoreDialogueResult> Completion => _completion.Task;
    internal CoreDialogueResult Snapshot { get { lock (_gate) return _snapshot; } }

    internal bool TryBegin()
    {
        lock (_gate)
        {
            if (_snapshot.State != CoreDialogueState.Queued) return false;
            _snapshot = Result(CoreDialogueState.Running, CoreDialogueEffectState.NoConfirmedEffect, "dialogue.starting");
            return true;
        }
    }

    // Called after the existing Native owner accepts its admission, before starting its worker.
    internal void MarkOwnerAdmitted()
    {
        lock (_gate)
        {
            if (_snapshot.State != CoreDialogueState.Running) return;
            _ownerAdmitted = true;
            _snapshot = Result(CoreDialogueState.Running, CoreDialogueEffectState.UnknownAfterStart, "dialogue.running");
        }
    }

    // Called only at the existing successful action + required history completion point.
    // This is a receipt, not a second writer. The worker still releases its real admission first.
    internal void RecordOwnerCompletion(string reply, IReadOnlyList<CoreSceneUtterance> sceneUtterances = null)
    {
        lock (_gate)
        {
            if (!_ownerAdmitted || _snapshot.State != CoreDialogueState.Running || _ownerCompleted) return;
            _ownerCompleted = true;
            _confirmedReply = reply ?? "";
            if (sceneUtterances != null) _sceneUtterances = new List<CoreSceneUtterance>(sceneUtterances).AsReadOnly();
        }
    }

    internal void RecordSceneProgress(IReadOnlyList<CoreSceneUtterance> sceneUtterances)
    {
        lock (_gate)
        {
            if (_snapshot.State != CoreDialogueState.Running) return;
            _sceneUtterances = new List<CoreSceneUtterance>(sceneUtterances ?? Array.Empty<CoreSceneUtterance>()).AsReadOnly();
            _snapshot = Result(CoreDialogueState.Running, _snapshot.Effects, _snapshot.ReasonCode);
        }
    }

    internal void Finish(string unconfirmedReason)
    {
        lock (_gate)
        {
            if (IsTerminal(_snapshot.State)) return;
            if (_ownerCompleted)
                SetTerminal(Result(CoreDialogueState.Completed, CoreDialogueEffectState.CompletedByOwner,
                    "dialogue.completed", _confirmedReply));
            else
                SetTerminal(Result(_ownerAdmitted ? CoreDialogueState.Failed : CoreDialogueState.Rejected,
                    _ownerAdmitted ? CoreDialogueEffectState.UnknownAfterStart : CoreDialogueEffectState.NoConfirmedEffect,
                    unconfirmedReason));
        }
    }

    internal CoreDialogueCancelResult Cancel()
    {
        lock (_gate)
        {
            if (IsTerminal(_snapshot.State)) return CoreDialogueCancelResult.AlreadyTerminal;
            if (_snapshot.State != CoreDialogueState.Queued) return CoreDialogueCancelResult.TooLate;
            SetTerminal(Result(CoreDialogueState.Cancelled, CoreDialogueEffectState.NoConfirmedEffect,
                "dialogue.cancelled_before_start"));
            return CoreDialogueCancelResult.CancelledBeforeStart;
        }
    }

    private static bool IsTerminal(CoreDialogueState state)
        => state != CoreDialogueState.Queued && state != CoreDialogueState.Running;
    private CoreDialogueResult Result(CoreDialogueState state, CoreDialogueEffectState effects, string reason, string reply = "")
        => new CoreDialogueResult(ClientId, RequestId, state, effects, reason, reply, _sceneUtterances);
    private void SetTerminal(CoreDialogueResult result)
    {
        _snapshot = result;
        _completion.TrySetResult(result);
    }
}
