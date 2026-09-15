using System;
using System.Threading.Tasks;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge;

public partial class MyBehavior
{
    private readonly MemorySummaryRunOwner _memorySummaryRunOwner = new MemorySummaryRunOwner();

    // An explicit run token is carried only by the summary pipeline. Ordinary dialogue,
    // persona and editor calls keep using the same dispatcher without inheriting this scope.
    private Task<bool> RunMemorySummaryRunPhaseAsync(MemorySummaryRunOwner.Lease run, long generation, Func<bool> phase)
    {
        if (run != null && !run.IsCurrent) return Task.FromResult(false);
        return RunMemorySummaryCompletionAsync(generation, () => (run == null || run.IsCurrent) && phase());
    }

    private Task<bool> RunMemorySummaryRunCaptureAsync(MemorySummaryRunOwner.Lease run, long generation, Func<bool> phase)
    {
        if (run != null && !run.IsCurrent) return Task.FromResult(false);
        return RunMemorySummaryMainThreadAsync(generation, () => (run == null || run.IsCurrent) && phase());
    }
}
