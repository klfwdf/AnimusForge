using System.Diagnostics;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge;

public partial class MyBehavior
{
    // Only the synchronous Campaign maintenance cycle holds this shared window.
    // EngineTick summary completions retain their separately audited budget.
    private const int DailyMemorySealMetadataPerSlice = 128;
    private bool _campaignMemoryMaintenanceCycleActive;
    private MemoryMaintenanceWorkBudget _campaignMemoryMaintenanceBudget;
    private bool _campaignMemorySummaryStartPending;
    private long _campaignMemorySummaryStartGeneration;

    private void ResolveDailyMaintenanceBudget(out long startTimestamp, out double budgetMs)
    {
        var window = _campaignMemoryMaintenanceBudget;
        // Idle Campaign ticks never allocate a window or read budget settings.
        if (window == null && _campaignMemoryMaintenanceCycleActive)
        {
            window = new MemoryMaintenanceWorkBudget(Stopwatch.GetTimestamp(),
                GetDailyMaintenanceFrameBudgetMs(), DailyMemorySealMetadataPerSlice,
                DailyMaintenanceMaxJobsPerTick);
            _campaignMemoryMaintenanceBudget = window;
        }
        startTimestamp = window == null ? Stopwatch.GetTimestamp() : window.Start;
        budgetMs = window == null ? GetDailyMaintenanceFrameBudgetMs() : window.Milliseconds;
    }
}
