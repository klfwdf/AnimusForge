using System.Collections.Generic;
using System.Threading.Tasks;
using AnimusForge.Refactor.Runtime;
namespace AnimusForge;
public partial class MyBehavior
{
    private MemorySummaryPlanningOwner CreateMemorySummaryPlanningOwner() => new MemorySummaryPlanningOwner(_memoryBusinessState,
        new MemorySummaryPlanningPort
        {
            RunPhaseAsync = RunMemorySummaryRunPhaseAsync,
            GetBudgetMilliseconds = GetDailyMaintenanceFrameBudgetMs,
            ElapsedTicks = () => MemorySummaryDispatchElapsedTicks,
            MaxJobs = DailyMaintenanceMaxJobsPerTick,
            IsEntityEligible = IsMemoryEntityEligibleForCompressedMemory,
            IsDailyPending = HasMemorySummaryJobStillPending,
            IsMajorPending = HasMajorActionSummaryJobStillPending,
            IsOverviewPending = HasMemoryOverviewJobStillPending,
            CancelUnavailable = (id, reason) => CancelUnavailableHeroCompressionWorkById(id, reason)
        });
    private MemorySummaryPlanEntry DescribeMemorySummaryJob(object job, int ordinal) =>
        CreateMemorySummaryPlanningOwner().DescribeMemorySummaryJob(job, ordinal);
    private Task<MemorySummaryPlan> BuildMemorySummaryPlanAsync(long generation,
        bool overviewOnly = false, HashSet<string> excludedOverviewIds = null, bool cleanupOnly = false, MemorySummaryRunOwner.Lease run = null) =>
        CreateMemorySummaryPlanningOwner().BuildMemorySummaryPlanAsync(generation, overviewOnly, excludedOverviewIds, cleanupOnly, run);
}
