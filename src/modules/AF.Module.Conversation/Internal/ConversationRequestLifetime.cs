using System;
using System.Threading;

namespace AnimusForge.Refactor.Runtime;

// Owns cancellation resources only, not request identity or publication authority.
// A retired lifetime remains alive until all already-entered async workers settle.
internal sealed class ConversationRequestLifetime
{
    private readonly object _gate = new object();
    private readonly InteractionRequestLease _lease = new InteractionRequestLease(CancellationToken.None);
    private int _workers;
    private bool _retired;
    private bool _cancelling;

    internal CancellationToken Token => _lease.Token;

    internal IDisposable Enter()
    {
        lock (_gate)
        {
            if (_retired) throw new OperationCanceledException(Token);
            _workers++;
            return new Worker(this);
        }
    }

    internal void Retire()
    {
        lock (_gate)
        {
            if (_retired) return;
            _retired = true;
            _cancelling = true;
        }
        // Never invoke a transport/plugin callback while holding the worker gate.
        _lease.Cancel();
        lock (_gate)
        {
            _cancelling = false;
            if (_workers == 0) _lease.Complete();
        }
    }

    private void Leave()
    {
        lock (_gate)
        {
            _workers--;
            if (_retired && !_cancelling && _workers == 0) _lease.Complete();
        }
    }

    private sealed class Worker : IDisposable
    {
        private ConversationRequestLifetime _owner;
        internal Worker(ConversationRequestLifetime owner) { _owner = owner; }
        public void Dispose() => Interlocked.Exchange(ref _owner, null)?.Leave();
    }
}
