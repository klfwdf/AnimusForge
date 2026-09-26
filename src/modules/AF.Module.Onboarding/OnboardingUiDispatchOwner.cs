using System;
using System.Collections.Concurrent;
using System.Threading;

namespace AnimusForge.Refactor.Modules;

internal sealed class OnboardingUiDispatchOwner
{
    private readonly ConcurrentQueue<Action> _pending = new ConcurrentQueue<Action>();
    private int _testGeneration;
    private int _closed;

    internal int BeginTest() => Interlocked.Increment(ref _testGeneration);
    internal void CancelTest() => Interlocked.Increment(ref _testGeneration);

    internal bool Post(Action action)
    {
        if (action == null || Volatile.Read(ref _closed) != 0)
            return false;
        _pending.Enqueue(action);
        return true;
    }

    internal bool PostTest(int generation, Action action)
        => Post(() =>
        {
            if (Volatile.Read(ref _testGeneration) == generation)
                action();
        });

    internal int Pump(int limit)
    {
        int count = 0;
        while (count < limit && Volatile.Read(ref _closed) == 0 && _pending.TryDequeue(out Action action))
        {
            count++;
            action();
        }
        return count;
    }

    internal void Close()
    {
        Interlocked.Exchange(ref _closed, 1);
        CancelTest();
        while (_pending.TryDequeue(out _))
        {
        }
    }
}
