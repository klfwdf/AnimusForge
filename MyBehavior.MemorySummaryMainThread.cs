using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class MyBehavior
{
    private const int MemorySummaryMainThreadActionsPerTick = 2;
    private int _memorySummaryMainThreadActionsThisTick;
    private long _memorySummaryMainThreadElapsedTicks;

    // Charge actual executed work, not the idle time between producer calls. The
    // existing maintenance setting is a cooperative limit, not an atomic-work timeout.
    private bool HasMemorySummaryMainThreadAllowance()
    {
        return _memorySummaryMainThreadActionsThisTick < MemorySummaryMainThreadActionsPerTick
            && _memorySummaryMainThreadElapsedTicks * 1000.0 / Stopwatch.Frequency
                < GetDailyMaintenanceFrameBudgetMs();
    }

    private sealed class MemorySummaryMainThreadAction
    {
        internal readonly long Generation;
        internal readonly Func<bool> Operation;
        internal readonly TaskCompletionSource<bool> Completion =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _state;

        internal MemorySummaryMainThreadAction(long generation, Func<bool> operation)
        {
            Generation = generation;
            Operation = operation;
        }

        internal bool TryClaim() => Interlocked.CompareExchange(ref _state, 1, 0) == 0;

        internal void Retire()
        {
            if (Interlocked.CompareExchange(ref _state, 2, 0) == 0)
            {
                Completion.TrySetResult(false);
            }
        }
    }

    // A completion now represents one business result, not a foreach over all results.
    // Inline and queued calls share the same allowance, including synchronous providers.
    // A single atomic capture/apply may overrun; no second operation starts after it does.
    private readonly ConcurrentQueue<MemorySummaryMainThreadAction> _memorySummaryMainThreadActions =
        new ConcurrentQueue<MemorySummaryMainThreadAction>();

    private Task<bool> RunMemorySummaryMainThreadAsync(long generation, Func<bool> operation)
    {
        if (operation == null)
        {
            return Task.FromResult(false);
        }
        if (TWParallel.IsMainThread() && _memorySummaryMainThreadActions.IsEmpty
            && HasMemorySummaryMainThreadAllowance())
        {
            _memorySummaryMainThreadActionsThisTick++;
            return Task.FromResult(TryApplyMemorySummaryMainThreadAction(generation, operation));
        }
        if (!ReferenceEquals(Instance, this) || !SaveRuntimeGuard.IsCurrentGeneration(generation))
        {
            return Task.FromResult(false);
        }
        var work = new MemorySummaryMainThreadAction(generation, operation);
        _memorySummaryMainThreadActions.Enqueue(work);
        // A load/owner replacement can race the enqueue after its first check. Retire the
        // completion here so an old owner that will never tick cannot strand its worker.
        if (!ReferenceEquals(Instance, this) || !SaveRuntimeGuard.IsCurrentGeneration(generation))
        {
            work.Retire();
        }
        return work.Completion.Task;
    }

    // Queue completion must distinguish rejection from a partially executed operation.
    // Keep legacy writer/capture false-on-error semantics; only the queue coordinator
    // receives the original exception and reports it, without replaying side effects.
    private async Task<bool> RunMemorySummaryCompletionAsync(long generation, Func<bool> operation)
    {
        Exception failure = null;
        bool accepted = await RunMemorySummaryMainThreadAsync(generation, delegate
        {
            try { return operation(); }
            catch (Exception ex) { failure = ex; return true; }
        });
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
        return accepted;
    }

    private bool TryApplyMemorySummaryMainThreadAction(long generation, Func<bool> operation)
    {
        if (!TWParallel.IsMainThread() || !ReferenceEquals(Instance, this)
            || !SaveRuntimeGuard.IsCurrentGeneration(generation))
        {
            return false;
        }
        long started = Stopwatch.GetTimestamp();
        try
        {
            if (!ReferenceEquals(Campaign.Current?.GetCampaignBehavior<MyBehavior>(), this))
            {
                return false;
            }
            return operation();
        }
        catch (Exception ex)
        {
            try { Logger.Log("CompressedMemory", "[ERROR] main-thread completion failed: " + ex.Message); }
            catch (Exception) { }
            return false;
        }
        finally
        {
            _memorySummaryMainThreadElapsedTicks += Stopwatch.GetTimestamp() - started;
        }
    }

    private void ProcessMemorySummaryMainThreadActions()
    {
        if (!TWParallel.IsMainThread())
        {
            return;
        }
        _memorySummaryMainThreadActionsThisTick = 0;
        _memorySummaryMainThreadElapsedTicks = 0;
        while (HasMemorySummaryMainThreadAllowance()
            && _memorySummaryMainThreadActions.TryDequeue(out MemorySummaryMainThreadAction work))
        {
            _memorySummaryMainThreadActionsThisTick++;
            if (work == null || !work.TryClaim())
            {
                continue;
            }
            bool applied = TryApplyMemorySummaryMainThreadAction(work.Generation, work.Operation);
            work.Completion.TrySetResult(applied);
        }
    }

    private void ResetMemorySummaryMainThreadActions()
    {
        while (_memorySummaryMainThreadActions.TryDequeue(out MemorySummaryMainThreadAction work))
        {
            work?.Retire();
        }
    }
}
