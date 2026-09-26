using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge.Refactor.Runtime;

// Tracks retirement callbacks for existing host queues; this class is not another work queue.
internal sealed class PendingOperationRegistry
{
    private readonly object _sync = new object();
    private readonly HashSet<Registration> _pending = new HashSet<Registration>();
    private readonly Action<Exception> _report;
    private long _version;
    private bool _sealed;
    private int _pauseCount;

    internal PendingOperationRegistry(Action<Exception> report = null) { _report = report; }
    internal long Version { get { lock (_sync) return _version; } }
    internal bool Accepting { get { lock (_sync) return !_sealed && _pauseCount == 0; } }
    internal int Count { get { lock (_sync) return _pending.Count; } }

    private sealed class Registration : IDisposable
    {
        private PendingOperationRegistry _owner;
        internal readonly Action Retire;
        internal Registration(PendingOperationRegistry owner, Action retire) { _owner = owner; Retire = retire; }
        public void Dispose()
        {
            PendingOperationRegistry owner = Interlocked.Exchange(ref _owner, null);
            if (owner != null) lock (owner._sync) owner._pending.Remove(this);
        }
    }

    internal IDisposable Register(long version, Action retire)
    {
        if (retire == null) throw new ArgumentNullException(nameof(retire));
        lock (_sync)
        {
            if (!_sealed && _pauseCount == 0 && version == _version)
            {
                var registration = new Registration(this, retire);
                _pending.Add(registration);
                return registration;
            }
        }
        retire(); // A submit/reset race must settle even when this owner never ticks again.
        return null;
    }

    internal void Reset() => RetireAll(false);
    internal void Seal() => RetireAll(true);

    // Keep new registrations out of the host's queue-clear window. A reset followed by a
    // separate clear can otherwise discard a newly registered callback without settling it.
    internal void ResetAndClear(Action clearQueue)
    {
        if (clearQueue == null) throw new ArgumentNullException(nameof(clearQueue));
        lock (_sync) _pauseCount++;
        try { RetireAll(false); clearQueue(); }
        finally { lock (_sync) { _version++; _pauseCount--; } }
    }

    private void RetireAll(bool seal)
    {
        Registration[] pending;
        lock (_sync)
        {
            _version++;
            if (seal) _sealed = true;
            pending = new Registration[_pending.Count];
            _pending.CopyTo(pending);
            _pending.Clear();
        }
        foreach (Registration registration in pending)
        {
            try { registration.Retire(); }
            catch (Exception error) { try { _report?.Invoke(error); } catch { } }
            finally { registration.Dispose(); }
        }
    }

    internal static async Task<T> AwaitRelease<T>(Task<T> completion, IDisposable registration)
    {
        try { return await completion.ConfigureAwait(false); }
        finally { registration?.Dispose(); }
    }
}
