using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Runtime;

/// <summary>
/// Owns memory capture/acceptance dispatch, never memory data or game objects.
/// Submit is worker-safe; Tick and elapsed-time inspection belong to the host thread.
/// Reset retires queued, unclaimed work only. It cannot undo an executing operation.
/// </summary>
internal sealed class MemorySummaryDispatcher
{
    private readonly IMemorySummaryDispatchHost _host;
    private readonly int _actionsPerTick;
    private readonly ConcurrentQueue<WorkItem> _pending = new ConcurrentQueue<WorkItem>();
    private int _actionsThisTick;
    private long _elapsedTicks;

    internal MemorySummaryDispatcher(IMemorySummaryDispatchHost host, int actionsPerTick)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        if (actionsPerTick <= 0) throw new ArgumentOutOfRangeException(nameof(actionsPerTick));
        _actionsPerTick = actionsPerTick;
    }

    // Diagnostic snapshots, not a transaction or proof that an operation has completed.
    internal int PendingCount => _pending.Count;
    internal bool HasPending => !_pending.IsEmpty;
    internal long ElapsedTicks => _elapsedTicks;

    private bool HasAllowance()
    {
        return _actionsThisTick < _actionsPerTick
            && _elapsedTicks * 1000.0 / Stopwatch.Frequency < _host.GetBudgetMilliseconds();
    }

    private sealed class WorkItem
    {
        internal readonly long Generation;
        internal readonly Func<bool> Operation;
        internal readonly TaskCompletionSource<bool> Completion =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _state;

        internal WorkItem(long generation, Func<bool> operation)
        { Generation = generation; Operation = operation; }

        internal bool TryClaim() => Interlocked.CompareExchange(ref _state, 1, 0) == 0;

        internal void Retire()
        {
            if (Interlocked.CompareExchange(ref _state, 2, 0) == 0)
                Completion.TrySetResult(false);
        }
    }

    internal Task<bool> Submit(long generation, Func<bool> operation)
    {
        if (operation == null) return Task.FromResult(false);
        if (_host.IsMainThread && _pending.IsEmpty && HasAllowance())
        {
            _actionsThisTick++;
            return Task.FromResult(TryExecute(generation, operation));
        }
        if (!_host.IsOwnerGenerationCurrent(generation)) return Task.FromResult(false);
        var work = new WorkItem(generation, operation);
        _pending.Enqueue(work);
        // A retired owner may never Tick again. Settle a submit/load race here.
        if (!_host.IsOwnerGenerationCurrent(generation)) work.Retire();
        return work.Completion.Task;
    }

    // Ordinary captures/writers retain false-on-error semantics. Completion callers
    // need the original exception after partial execution, not a false success or replay.
    internal async Task<bool> SubmitCompletion(long generation, Func<bool> operation)
    {
        Exception failure = null;
        bool accepted = await Submit(generation, delegate
        {
            try { return operation(); }
            catch (Exception ex) { failure = ex; return true; }
        });
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
        return accepted;
    }

    private bool TryExecute(long generation, Func<bool> operation)
    {
        if (!_host.IsMainThread || !_host.IsOwnerGenerationCurrent(generation)) return false;
        long started = Stopwatch.GetTimestamp();
        try
        {
            if (!_host.IsExecutionContextCurrent()) return false;
            return operation();
        }
        catch (Exception ex)
        {
            try { _host.ReportFailure(ex); }
            catch (Exception) { } // Diagnostics cannot strand a queued completion.
            return false;
        }
        finally { _elapsedTicks += Stopwatch.GetTimestamp() - started; }
    }

    internal void Tick()
    {
        if (!_host.IsMainThread) return;
        _actionsThisTick = 0;
        _elapsedTicks = 0;
        while (HasAllowance() && _pending.TryDequeue(out WorkItem work))
        {
            _actionsThisTick++;
            if (work == null || !work.TryClaim()) continue;
            bool applied = TryExecute(work.Generation, work.Operation);
            work.Completion.TrySetResult(applied);
        }
    }

    internal void Reset()
    {
        while (_pending.TryDequeue(out WorkItem work)) work?.Retire();
    }
}
