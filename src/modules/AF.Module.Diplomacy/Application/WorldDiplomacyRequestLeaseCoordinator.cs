using System;
using System.Collections.Generic;
using System.Threading;

namespace AnimusForge;

// Application-owned transient transport identities. Removed jobs still retain
// their lease until their actual request completes. Never persisted.
internal sealed class WorldDiplomacyRequestLeaseCoordinator
{
    private readonly Dictionary<string, WorldDiplomacyRequestSnapshot> _leases = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CancellationTokenSource> _leaseCancellations = new(StringComparer.OrdinalIgnoreCase);
    private readonly int _capacity;
    private CancellationTokenSource _cancellation = new();
    internal WorldDiplomacyRequestLeaseCoordinator(int capacity = 1) => _capacity = Math.Max(1, capacity);
    internal bool IsRunning => _leases.Count > 0;
    internal bool IsFull => _leases.Count >= _capacity;
    internal int Count => _leases.Count;
    internal bool ContainsJob(string id) => _leases.ContainsKey(id ?? "");
    internal bool ContainsSpeech(string roundId)
    {
        foreach (var lease in _leases.Values)
            if (lease.IsSpeech && string.Equals(lease.RoundId, roundId, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
    internal bool TryClaim(string jobId, long generation, int tokens, int timeout, out WorldDiplomacyRequestSnapshot snapshot)
        => TryClaim(new WorldDiplomacyJob { JobId = jobId }, generation, tokens, timeout, false, out snapshot);
    internal bool TryClaim(WorldDiplomacyJob job, long generation, int tokens, int timeout, bool player,
        out WorldDiplomacyRequestSnapshot snapshot)
    {
        snapshot = null;
        string id = (job?.JobId ?? "").Trim();
        if (id.Length == 0 || generation <= 0 || IsFull || ContainsJob(id)) return false;
        string roundId = string.IsNullOrEmpty(job.RoundId) ? job.ExchangeId : job.RoundId;
        bool speech = !string.IsNullOrEmpty(roundId) && job.Kind != "compress";
        if (speech && ContainsSpeech(roundId)) return false;
        var leaseCancellation = CancellationTokenSource.CreateLinkedTokenSource(_cancellation.Token);
        snapshot = new WorldDiplomacyRequestSnapshot(id, generation, Math.Max(256, tokens), Math.Max(1, timeout),
            roundId, speech, ++job.RequestAttempt, job.RoundConversationRevision, player, leaseCancellation.Token,
            job.OwnershipEpoch, job.SchedulingOwner, job.PlayerAuthorityRulerId, job.RequestLane);
        _leases.Add(id, snapshot);
        _leaseCancellations.Add(id, leaseCancellation);
        return true;
    }
    internal bool TryRelease(string id, long generation) => TryRelease(id, generation, 0);
    internal bool TryRelease(string id, long generation, int attempt)
    {
        if (!_leases.TryGetValue((id ?? "").Trim(), out var lease) || lease.RuntimeGeneration != generation
            || (attempt > 0 && lease.Attempt != attempt)) return false;
        if (_leaseCancellations.TryGetValue(lease.JobId, out var cancellation)) { cancellation.Dispose(); _leaseCancellations.Remove(lease.JobId); }
        return _leases.Remove(lease.JobId);
    }
    internal void InvalidateRound(string roundId)
    {
        foreach (var lease in _leases.Values)
            if (string.Equals(lease.RoundId, roundId, StringComparison.OrdinalIgnoreCase)
                && _leaseCancellations.TryGetValue(lease.JobId, out var cancellation)) cancellation.Cancel();
    }
    internal void Reset()
    {
        _cancellation.Cancel();
        foreach (var cancellation in _leaseCancellations.Values) cancellation.Dispose();
        _leaseCancellations.Clear();
        _cancellation.Dispose();
        _cancellation = new CancellationTokenSource();
        _leases.Clear();
    }
}

internal sealed class WorldDiplomacyRequestSnapshot
{
    internal WorldDiplomacyRequestSnapshot(string jobId, long runtimeGeneration, int maxTokens, int timeoutMilliseconds,
        string roundId = "", bool isSpeech = false, int attempt = 0, long revision = 0,
        bool player = false, CancellationToken cancellation = default, int ownershipEpoch = 0,
        string schedulingOwner = "", string playerAuthorityRulerId = "", string requestLane = "")
    {
        JobId = jobId; RuntimeGeneration = runtimeGeneration; MaxTokens = maxTokens; TimeoutMilliseconds = timeoutMilliseconds;
        RoundId = roundId; IsSpeech = isSpeech; Attempt = attempt; ConversationRevision = revision;
        IsPlayerWork = player; Cancellation = cancellation;
        RequestLane = requestLane is "player" or "oral" ? requestLane : player ? "player" : "normal";
        OwnershipEpoch = ownershipEpoch; SchedulingOwner = schedulingOwner; PlayerAuthorityRulerId = playerAuthorityRulerId;
    }
    internal string JobId { get; }
    internal long RuntimeGeneration { get; }
    internal int MaxTokens { get; }
    internal int TimeoutMilliseconds { get; }
    internal string RoundId { get; }
    internal bool IsSpeech { get; }
    internal int Attempt { get; }
    internal long ConversationRevision { get; }
    internal bool IsPlayerWork { get; }
    internal string RequestLane { get; }
    internal CancellationToken Cancellation { get; }
    internal int OwnershipEpoch { get; }
    internal string SchedulingOwner { get; }
    internal string PlayerAuthorityRulerId { get; }
}
