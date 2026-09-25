using System;
using System.Collections.Generic;

namespace AnimusForge.Refactor.Modules;

/// <summary>
/// An opaque per-consumer namespace. IDs are retained until disposal, never evicted and replayed.
/// No global player-history cache, caller-name registry, polling or gameplay state is added.
/// </summary>
internal sealed class CoreDialogueClient : IDisposable
{
    internal const int MaximumRequests = 128;
    internal const int MaximumTextLength = 16000;
    private readonly object _gate = new object();
    private readonly Dictionary<string, CoreDialogueOperation> _requests =
        new Dictionary<string, CoreDialogueOperation>(StringComparer.Ordinal);
    private readonly Action<CoreDialogueOperation> _submitNative;
    private bool _disposed;

    internal CoreDialogueClient(Action<CoreDialogueOperation> submitNative)
    {
        _submitNative = submitNative ?? throw new ArgumentNullException(nameof(submitNative));
        ClientId = Guid.NewGuid().ToString("N");
    }
    internal string ClientId { get; }

    internal CoreDialogueOperation SubmitNative(string requestId, string playerText)
        => Submit(CoreDialogueChannel.Native, "", requestId, playerText, _submitNative);

    internal string CaptureSceneContextTicket()
    {
        lock (_gate) if (_disposed) return null;
        string ticket = ShoutBehavior.IssueModuleSceneTicket(ClientId);
        if (ticket == null) return null;
        lock (_gate) if (!_disposed) return ticket;
        ShoutBehavior.RevokeModuleSceneTickets(ClientId);
        return null;
    }

    // Future Scene/Courier callers must supply an AF-issued context identity. This fingerprint
    // does not validate a ticket: the real owner must claim its UI context before side effects.
    internal CoreDialogueOperation SubmitForContext(CoreDialogueChannel channel, string contextIdentity,
        string requestId, string playerText, Action<CoreDialogueOperation> dispatch)
        => channel == CoreDialogueChannel.Scene || channel == CoreDialogueChannel.Courier
            ? Submit(channel, contextIdentity, requestId, playerText, dispatch)
            : Rejected(requestId, "dialogue.invalid_request");

    private CoreDialogueOperation Submit(CoreDialogueChannel channel, string contextIdentity,
        string requestId, string playerText, Action<CoreDialogueOperation> dispatch)
    {
        CoreDialogueOperation operation;
        lock (_gate)
        {
            if (_disposed) return Rejected(requestId, "dialogue.client_disposed");
            if (!ValidId(requestId) || string.IsNullOrWhiteSpace(playerText) || playerText.Length > MaximumTextLength)
                return Rejected(requestId, "dialogue.invalid_request");
            if (channel != CoreDialogueChannel.Native && channel != CoreDialogueChannel.Scene
                && channel != CoreDialogueChannel.Courier)
                return Rejected(requestId, "dialogue.invalid_request");
            if ((channel == CoreDialogueChannel.Native && !string.Equals(contextIdentity, "", StringComparison.Ordinal))
                || (channel != CoreDialogueChannel.Native && (string.IsNullOrEmpty(contextIdentity)
                    || contextIdentity.Length > 128 || dispatch == null)))
                return Rejected(requestId, "dialogue.invalid_request");
            if (_requests.TryGetValue(requestId, out operation))
                return operation.Channel == channel
                    && string.Equals(operation.ContextIdentity, contextIdentity, StringComparison.Ordinal)
                    && string.Equals(operation.PlayerText, playerText, StringComparison.Ordinal)
                    ? operation : Rejected(requestId, "dialogue.request_id_conflict");
            // Do not auto-evict completed IDs: eviction would turn a retry into another action commit.
            if (_requests.Count >= MaximumRequests) return Rejected(requestId, "dialogue.client_capacity");
            operation = new CoreDialogueOperation(ClientId, requestId, playerText, channel, contextIdentity);
            _requests.Add(requestId, operation);
        }
        try { dispatch(operation); }
        catch (Exception) { operation.Finish("dialogue.dispatch_failed"); }
        return operation;
    }

    public void Dispose()
    {
        CoreDialogueOperation[] operations;
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            operations = new CoreDialogueOperation[_requests.Count];
            _requests.Values.CopyTo(operations, 0);
            _requests.Clear();
        }
        ShoutBehavior.RevokeModuleSceneTickets(ClientId);
        foreach (CoreDialogueOperation operation in operations) operation.Cancel();
    }

    private CoreDialogueOperation Rejected(string requestId, string reason)
    {
        var operation = new CoreDialogueOperation(ClientId,
            ValidId(requestId) ? requestId : "", "");
        operation.Finish(reason);
        return operation;
    }

    private static bool ValidId(string id)
    {
        if (string.IsNullOrEmpty(id) || id.Length > 128) return false;
        foreach (char c in id)
            if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')
                || c == '.' || c == '_' || c == '-' || c == ':')) return false;
        return true;
    }
}
