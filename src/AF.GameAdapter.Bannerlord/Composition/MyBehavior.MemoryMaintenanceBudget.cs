using System.Diagnostics;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge;

public partial class MyBehavior
{
    // Only the synchronous Campaign maintenance cycle holds this shared window.
    // EngineTick summary completions retain their separately audited budget.
    private const int DailyMemorySealMetadataPerSlice = 128;
    private bool _campaignMemoryMaintenanceCycleActive { get => _memoryBusinessState.MaintenanceCycleActive; set => _memoryBusinessState.MaintenanceCycleActive=value; }
    private MemoryMaintenanceWorkBudget _campaignMemoryMaintenanceBudget { get => _memoryBusinessState.MaintenanceBudget; set => _memoryBusinessState.MaintenanceBudget=value; }
    private bool _campaignMemorySummaryStartPending { get => _dailyMaintenanceController.SummaryStartPending; set => _dailyMaintenanceController.SummaryStartPending = value; }
    private long _campaignMemorySummaryStartGeneration { get => _dailyMaintenanceController.SummaryStartGeneration; set => _dailyMaintenanceController.SummaryStartGeneration = value; }

    private void ResolveDailyMaintenanceBudget(out long startTimestamp, out double budgetMs)
        => _memoryBusinessState.ResolveMaintenanceBudget(CampaignDailyMaintenanceController.GetDailyMaintenanceFrameBudgetMs,
            DailyMemorySealMetadataPerSlice, DailyMaintenanceMaxJobsPerTick, out startTimestamp, out budgetMs);
}
