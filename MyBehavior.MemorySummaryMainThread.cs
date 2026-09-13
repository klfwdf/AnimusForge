using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class MyBehavior
{
    private const int MemorySummaryMainThreadActionsPerTick = 2;
    private int _memorySummaryMainThreadActionsThisTick;

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
    // Large atomic capture/apply internals still require separate record/time accounting.
    private readonly ConcurrentQueue<MemorySummaryMainThreadAction> _memorySummaryMainThreadActions =
        new ConcurrentQueue<MemorySummaryMainThreadAction>();

    private Task<bool> RunMemorySummaryMainThreadAsync(long generation, Func<bool> operation)
    {
        if (operation == null)
        {
            return Task.FromResult(false);
        }
        if (TWParallel.IsMainThread() && _memorySummaryMainThreadActions.IsEmpty
            && _memorySummaryMainThreadActionsThisTick < MemorySummaryMainThreadActionsPerTick)
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

    private bool TryApplyMemorySummaryMainThreadAction(long generation, Func<bool> operation)
    {
        if (!TWParallel.IsMainThread() || !ReferenceEquals(Instance, this)
            || !SaveRuntimeGuard.IsCurrentGeneration(generation))
        {
            return false;
        }
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
    }

    private void ProcessMemorySummaryMainThreadActions()
    {
        if (!TWParallel.IsMainThread())
        {
            return;
        }
        _memorySummaryMainThreadActionsThisTick = 0;
        while (_memorySummaryMainThreadActionsThisTick < MemorySummaryMainThreadActionsPerTick
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
