using System.Collections.Generic;

namespace AnimusForge;

// All calls occur on the existing Campaign/main-thread action pump.
internal sealed class AutomaticKingdomRebellionOwner<T> where T : class
{
    private readonly Queue<T> _pending = new Queue<T>();
    private T _ready;
    private bool _hasReady;
    private bool _inProgress;
    private long _requestVersion;
    internal bool FlowActive { get; private set; }
    internal int PendingCount => _pending.Count;
    internal bool CanStart => !_inProgress && !_hasReady;

    internal void Enqueue(T context) => _pending.Enqueue(context);
    internal bool ActivateIfQueued()
    {
        if (_pending.Count == 0) return false;
        FlowActive = true;
        return true;
    }
    internal bool TryDequeue(out T context)
    {
        context = null;
        if (!CanStart) return false;
        if (_pending.Count == 0 || _pending.Peek() == null)
        {
            FlowActive = false;
            return false;
        }
        context = _pending.Dequeue();
        return true;
    }
    internal long BeginNaming()
    {
        _inProgress = true;
        _hasReady = false;
        _ready = null;
        return ++_requestVersion;
    }
    internal bool CompleteNaming(long requestVersion, T context)
    {
        if (!_inProgress || _hasReady || requestVersion != _requestVersion) return false;
        _ready = context;
        _hasReady = true;
        return true;
    }
    internal bool TryTakeReady(out T context)
    {
        context = null;
        if (!_hasReady) return false;
        context = _ready;
        _hasReady = false;
        _ready = null;
        _inProgress = false;
        return true;
    }
    internal void FinishFlow() => FlowActive = false;
    internal void Cancel()
    {
        _pending.Clear();
        _hasReady = false;
        _ready = null;
        _inProgress = false;
        FlowActive = false;
        _requestVersion++;
    }
}
