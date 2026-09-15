using System;
using System.Collections.Generic;

namespace AnimusForge.Refactor.Runtime;

/// <summary>
/// Owns transient Hero persona reservations and failure cooldowns, not profiles or game objects.
/// A lease identity survives asynchronous work; Reset must not let old completions release new work.
/// </summary>
internal sealed class NpcPersonaGenerationOwner
{
    internal sealed class Lease
    {
        internal Lease(string id) { Id = id; }
        internal string Id { get; }
    }

    private readonly object _sync = new object();
    // Preserve the original active-id and cooldown-id comparison semantics.
    private readonly Dictionary<string, Lease> _active = new Dictionary<string, Lease>();
    private readonly Dictionary<string, long> _retryAfter = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

    internal Lease TryBegin(string id, bool ignoreRetryCooldown, out bool coolingDown)
    {
        coolingDown = false;
        lock (_sync)
        {
            if (_active.ContainsKey(id)) return null;
            if (_retryAfter.TryGetValue(id, out long retryAfter))
            {
                if (!ignoreRetryCooldown && DateTime.UtcNow.Ticks < retryAfter)
                { coolingDown = true; return null; }
                _retryAfter.Remove(id);
            }
            var lease = new Lease(id);
            _active.Add(id, lease);
            return lease;
        }
    }

    internal bool IsCurrent(Lease lease)
    {
        lock (_sync) return lease != null && _active.TryGetValue(lease.Id, out Lease current)
            && ReferenceEquals(current, lease);
    }

    internal void Complete(Lease lease, bool saved, bool retryOnFailure = true)
    {
        lock (_sync)
        {
            if (lease == null || !_active.TryGetValue(lease.Id, out Lease current)
                || !ReferenceEquals(current, lease)) return;
            _active.Remove(lease.Id);
            if (saved || !retryOnFailure) _retryAfter.Remove(lease.Id);
            else _retryAfter[lease.Id] = DateTime.UtcNow.AddMinutes(5.0).Ticks;
        }
    }

    internal void GetState(string id, out bool active, out bool coolingDown)
    {
        active = coolingDown = false;
        if (string.IsNullOrWhiteSpace(id)) return;
        lock (_sync)
        {
            active = _active.ContainsKey(id);
            if (!_retryAfter.TryGetValue(id, out long retryAfter)) return;
            coolingDown = DateTime.UtcNow.Ticks < retryAfter;
            if (!coolingDown) _retryAfter.Remove(id);
        }
    }

    internal void Reset()
    {
        lock (_sync) { _active.Clear(); _retryAfter.Clear(); }
    }
}
