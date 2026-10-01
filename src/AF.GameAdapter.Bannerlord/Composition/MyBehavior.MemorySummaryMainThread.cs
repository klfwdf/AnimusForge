using System;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class MyBehavior
{
    private const int MemorySummaryMainThreadActionsPerTick = 2;
    private MemorySummaryDispatcher _memorySummaryDispatcher;
    private int _campaignRuntimeRetired;

    // Construct once on first submission, without consulting live game state. Concurrent
    // submitters all use the published winner; unused Tick/Reset paths do not allocate.
    private MemorySummaryDispatcher MemorySummaryDispatch
    {
        get
        {
            var current = Volatile.Read(ref _memorySummaryDispatcher);
            if (current != null) return current;
            var created = new MemorySummaryDispatcher(new MemorySummaryDispatchHost(this),
                MemorySummaryMainThreadActionsPerTick);
            return Interlocked.CompareExchange(ref _memorySummaryDispatcher, created, null) ?? created;
        }
    }

    private long MemorySummaryDispatchElapsedTicks => Volatile.Read(ref _memorySummaryDispatcher)?.ElapsedTicks ?? 0L;

    private sealed class MemorySummaryDispatchHost : IMemorySummaryDispatchHost
    {
        private readonly MyBehavior _owner;
        internal MemorySummaryDispatchHost(MyBehavior owner) { _owner = owner; }
        public bool IsMainThread => TWParallel.IsMainThread();
        public bool IsOwnerGenerationCurrent(long generation) =>
            Volatile.Read(ref _owner._campaignRuntimeRetired) == 0
            && ReferenceEquals(Instance, _owner) && SaveRuntimeGuard.IsCurrentGeneration(generation);
        public bool IsExecutionContextCurrent() =>
            ReferenceEquals(Campaign.Current?.GetCampaignBehavior<MyBehavior>(), _owner);
        public double GetBudgetMilliseconds() => GetDailyMaintenanceFrameBudgetMs();
        public void ReportFailure(Exception error) =>
            Logger.Log("CompressedMemory", "[ERROR] main-thread completion failed: " + error.Message);
    }

    private Task<bool> RunMemorySummaryMainThreadAsync(long generation, Func<bool> operation) =>
        MemorySummaryDispatch.Submit(generation, operation);

    private Task<bool> RunMemorySummaryCompletionAsync(long generation, Func<bool> operation) =>
        MemorySummaryDispatch.SubmitCompletion(generation, operation);

    private void ProcessMemorySummaryMainThreadActions() =>
        Volatile.Read(ref _memorySummaryDispatcher)?.Tick();

    private void ResetMemorySummaryMainThreadActions() =>
        Volatile.Read(ref _memorySummaryDispatcher)?.Reset();
}
