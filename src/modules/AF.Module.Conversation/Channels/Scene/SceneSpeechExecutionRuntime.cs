using System;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge;

// Scene dispatch lifecycle only. No Mission/Agent references or duplicate history state.
// Each queued payload retains its existing completion source and action protocol.
internal sealed class SceneSpeechExecutionRuntime<T> where T : class
{
    private readonly SceneSpeechQueueOwner<T> _queue = new SceneSpeechQueueOwner<T>();
    private readonly Func<bool> _isProcessing;
    private readonly Action<Action> _post;
    private readonly Action<T> _publish;
    private readonly Action<T> _reject;
    private readonly Func<int, Task> _delay;

    internal SceneSpeechExecutionRuntime(Func<bool> isProcessing, Action<Action> post,
        Action<T> publish, Action<T> reject, Func<int, Task> delay = null)
    {
        _isProcessing = isProcessing ?? throw new ArgumentNullException(nameof(isProcessing));
        _post = post ?? throw new ArgumentNullException(nameof(post));
        _publish = publish ?? throw new ArgumentNullException(nameof(publish));
        _reject = reject ?? throw new ArgumentNullException(nameof(reject));
        _delay = delay ?? Task.Delay;
    }

    internal void Enqueue(T item)
    {
        if (_queue.EnqueueAndTryStartWorker(item, out long generation))
            _ = Task.Run(() => RunAsync(generation));
    }

    private async Task RunAsync(long generation)
    {
        T dequeued = null;
        try
        {
            while (_queue.IsCurrentGeneration(generation))
            {
                if (_isProcessing())
                {
                    // Preserve the existing admission cadence; no game object reads here.
                    await _delay(100).ConfigureAwait(false);
                    continue;
                }
                if (!_queue.TryDequeueOrStopWorker(generation, out dequeued)) return;
                T item = dequeued;
                int claimed = 0;
                Action retire = () =>
                {
                    if (Interlocked.Exchange(ref claimed, 1) == 0) _reject(item);
                };
                if (!_queue.TryRegisterDispatch(generation, retire))
                {
                    retire();
                    dequeued = null;
                    return;
                }
                dequeued = null;
                try
                {
                _post(() =>
                {
                    // Dispatchers must not replay a non-transactional game effect, even
                    // after a publication failure or after mission reset/new admission.
                    if (Interlocked.Exchange(ref claimed, 1) != 0) return;
                    if (!_queue.TryClaimDispatch(generation, retire))
                    {
                        _reject(item);
                        return;
                    }
                    try { _publish(item); }
                    catch { _reject(item); }
                });
                }
                catch
                {
                    _queue.TryClaimDispatch(generation, retire);
                    retire();
                    _queue.StopWorker(generation);
                    return;
                }
                await _delay(2000).ConfigureAwait(false);
            }
        }
        catch
        {
            if (dequeued != null) _reject(dequeued);
            _queue.StopWorker(generation);
        }
    }

    internal T[] ClearQueued() => _queue.ClearQueued();
    internal T[] Reset() => _queue.Reset();
    internal bool HasQueuedOrWorker() => _queue.HasQueuedOrWorker();
    internal bool TryGetSnapshot(out int count, out bool running) => _queue.TryGetSnapshot(out count, out running);
}
