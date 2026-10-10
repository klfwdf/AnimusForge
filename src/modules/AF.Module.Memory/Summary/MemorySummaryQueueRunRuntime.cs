using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnimusForge.Refactor.Runtime;
namespace AnimusForge;

internal sealed class MemorySummaryWaveResult<TDaily, TMajor, TOverview>
    where TDaily : class where TMajor : class where TOverview : class
{
    internal TDaily Daily;
    internal TMajor Major;
    internal TOverview Overview;
}
internal sealed class MemorySummaryResultPort<T> where T : class
{
    internal Func<T, bool> HasJob, IsObsolete, IsSourceCurrent, Succeeded, ApplySuccess;
    internal Action<T> MarkFailure;
    internal Func<T, string> FailureText;
    internal Func<T, MemoryFailureUiText> FailureDisplayText;
    internal bool Accept(T result, List<string> failures, List<MemoryFailureUiText> displayFailures) => MemorySummaryAttemptRunner.Accept(
        HasJob(result), IsObsolete(result), () => IsSourceCurrent(result), Succeeded(result),
        () => ApplySuccess(result), () => { MarkFailure(result); failures.Add(FailureText(result)); displayFailures.Add(FailureDisplayText(result)); });
}
internal sealed class MemorySummaryQueueRunPort<TDaily, TMajor, TOverview>
    where TDaily : class where TMajor : class where TOverview : class
{
    internal Func<long, bool, HashSet<string>, bool, MemorySummaryRunOwner.Lease, Task<MemorySummaryPlan>> BuildPlanAsync;
    internal Func<MemorySummaryRunOwner.Lease, long, Func<bool>, Task<bool>> RunPhaseAsync;
    internal Func<object, MemorySummaryRunOwner.Lease, Task<MemorySummaryWaveResult<TDaily, TMajor, TOverview>>> ExecuteItemAsync;
    internal Func<bool, bool> ShouldScanOverview;
    internal Action QueueAllOverview, QueueDirtyOverview;
    internal Func<int> RequestsPerMinute;
    internal Func<bool> IsHostCurrent;
    internal Action<string> Notify;
    internal Action<string, string, long, MemoryFailureDisplay> BlockingPopup;
    internal Action<Exception> LogFailure;
    internal Func<int, Task> DelayAsync;
    internal MemorySummaryResultPort<TDaily> Daily;
    internal MemorySummaryResultPort<TMajor> Major;
    internal MemorySummaryResultPort<TOverview> Overview;
}
// Owns one run's orchestration, never the persisted queues or a game behavior.
internal sealed class MemorySummaryQueueRunRuntime<TDaily, TMajor, TOverview>
    where TDaily : class where TMajor : class where TOverview : class
{
    private readonly MemorySummaryRunOwner _runs;
    private readonly MemorySummaryQueueRunPort<TDaily, TMajor, TOverview> _ports;
    internal MemorySummaryQueueRunRuntime(MemorySummaryRunOwner runs, MemorySummaryQueueRunPort<TDaily, TMajor, TOverview> ports)
    { _runs = runs; _ports = ports; }
    internal async Task RunAsync(bool forceOverviewCandidateScan = false)
    {
        long generation = SaveRuntimeGuard.CaptureGeneration();
        var run = _runs.TryBegin(generation);
        if (run == null) return;
        try
        {
            var plan = await _ports.BuildPlanAsync(generation, false, null, false, run);
            if (plan == null) return;
            var items = plan.Items;
            int burst = 1;
            bool accepted = await _ports.RunPhaseAsync(run, generation, () =>
            {
                if (items.Count == 0)
                {
                    if (_ports.ShouldScanOverview(forceOverviewCandidateScan))
                    {
                        if (forceOverviewCandidateScan) _ports.QueueAllOverview();
                        else _ports.QueueDirtyOverview();
                    }
                }
                else _ports.QueueDirtyOverview();
                burst = _ports.RequestsPerMinute();
                if (items.Count > 0) _ports.Notify(MemoryFailureUiText.T("AF_MEMORY_UI_SummaryStarted", "AnimusForge started {TOTAL} end-of-day summary jobs: {DAILY} dialogue memories, {MAJOR} major histories, {OVERVIEW} memory overviews; limit {RPM} per minute.",
                    ("TOTAL", items.Count), ("DAILY", plan.DailyCount), ("MAJOR", plan.MajorCount), ("OVERVIEW", plan.OverviewCount), ("RPM", burst)).Resolve());
                return true;
            });
            if (!accepted || items.Count == 0) return;
            var daily = new List<TDaily>(); var major = new List<TMajor>(); var overview = new List<TOverview>();
            await RunQueueItemsAsync(items, burst, daily, major, overview, run);
            if (!run.IsCurrent || SaveRuntimeGuard.IsStale(generation, "memory_summary_queue_results")) return;
            var failures = new List<string>();
            var displayFailures = new List<MemoryFailureUiText>();
            int appliedDaily = 0, appliedMajor = 0, appliedOverview = 0;
            foreach (var result in daily)
            {
                accepted = await _ports.RunPhaseAsync(run, generation, () => { if (_ports.Daily.Accept(result, failures, displayFailures)) appliedDaily++; return true; });
                if (!accepted) return;
            }
            foreach (var result in major)
            {
                accepted = await _ports.RunPhaseAsync(run, generation, () => { if (_ports.Major.Accept(result, failures, displayFailures)) appliedMajor++; return true; });
                if (!accepted) return;
            }
            for (int wave = 0; wave < 2; wave++)
            {
                foreach (var result in overview)
                {
                    accepted = await _ports.RunPhaseAsync(run, generation, () => { if (_ports.Overview.Accept(result, failures, displayFailures)) appliedOverview++; return true; });
                    if (!accepted) return;
                }
                if (wave == 1) break;
                accepted = await _ports.RunPhaseAsync(run, generation, () => { _ports.QueueDirtyOverview(); return true; });
                if (!accepted) return;
                var extraPlan = await _ports.BuildPlanAsync(generation, true, plan.OverviewIds, false, run);
                if (extraPlan == null) return;
                var extra = extraPlan.Items;
                if (extra.Count == 0) break;
                await _ports.DelayAsync(60000);
                if (!run.IsCurrent || SaveRuntimeGuard.IsStale(generation, "memory_summary_queue_extra_delay")) return;
                overview = new List<TOverview>();
                await RunQueueItemsAsync(extra, burst, new List<TDaily>(), new List<TMajor>(), overview, run);
                if (!run.IsCurrent || SaveRuntimeGuard.IsStale(generation, "memory_summary_queue_extra_results")) return;
            }
            if (await _ports.BuildPlanAsync(generation, false, null, true, run) == null) return;
            string failureMessage = failures.Count == 0 ? null : await Task.Run(() => "以下日结压缩任务重试 3 次后仍失败：\n\n" + string.Join("\n", failures) + "\n\n请修复 API 或调低记忆总结 RPM 后重试。");
            await _ports.RunPhaseAsync(run, generation, () =>
            {
                if (failureMessage != null) {
                    string displayFailureLines = string.Join("\n", displayFailures.Select(x => x?.Resolve() ?? ""));
                    _ports.BlockingPopup("日结压缩总结失败", failureMessage, generation,
                    new MemoryFailureDisplay(MemoryFailureUiText.T("AF_MEMORY_UI_SummaryFailedTitle", "End-of-day summary failed"),
                        MemoryFailureUiText.T("AF_MEMORY_UI_SummaryFailedBody", "The following end-of-day summary jobs still failed after 3 attempts:\n\n{FAILURES}\n\nFix the API or lower the memory summary RPM, then retry.", ("FAILURES", displayFailureLines))));
                }
                else if (appliedDaily + appliedMajor + appliedOverview > 0)
                    _ports.Notify(MemoryFailureUiText.T("AF_MEMORY_UI_SummaryCompleted", "AnimusForge end-of-day summaries completed: {DAILY} dialogue memories, {MAJOR} major histories, {OVERVIEW} memory overviews.",
                        ("DAILY", appliedDaily), ("MAJOR", appliedMajor), ("OVERVIEW", appliedOverview)).Resolve());
                return true;
            });
        }
        catch (Exception error)
        {
            _ports.LogFailure(error);
            if (run.IsCurrent && SaveRuntimeGuard.IsCurrentGeneration(generation))
                await _ports.RunPhaseAsync(run, generation, () => { _ports.BlockingPopup("压缩记忆总结异常", "任务可能已有部分写入，本轮已停止；请查看日志后再重试。\n\n" + error.Message, generation,
                    new MemoryFailureDisplay(MemoryFailureUiText.T("AF_MEMORY_UI_SummaryExceptionTitle", "Memory summary exception"),
                        MemoryFailureUiText.T("AF_MEMORY_UI_SummaryExceptionBody", "Some results may already have been saved. This run has stopped; check the log before retrying.\n\n{ERROR}", ("ERROR", MemoryFailureUiText.Error(error.Message))))); return true; });
        }
        finally { run.Dispose(); }
    }
    internal async Task RunQueueItemsAsync(List<object> items, int burst, List<TDaily> daily, List<TMajor> major, List<TOverview> overview, MemorySummaryRunOwner.Lease run = null)
    {
        long generation = run?.Generation ?? SaveRuntimeGuard.CaptureGeneration();
        int clampedBurst = Math.Max(1, burst);
        for (int i = 0; i < (items?.Count ?? 0); i += clampedBurst)
        {
            if ((run != null && !run.IsCurrent) || !_ports.IsHostCurrent() || SaveRuntimeGuard.IsStale(generation, "memory_summary_queue_wave")) return;
            var wave = items.Skip(i).Take(clampedBurst).ToList();
            var tasks = wave.Select(item => _ports.ExecuteItemAsync(item, run)).ToList();
            var completed = await Task.WhenAll(tasks);
            foreach (var result in completed)
            {
                if (result == null) continue;
                if (result.Daily != null) daily?.Add(result.Daily);
                if (result.Major != null) major?.Add(result.Major);
                if (result.Overview != null) overview?.Add(result.Overview);
            }
            if (i + clampedBurst < (items?.Count ?? 0)) await _ports.DelayAsync(60000);
        }
    }
}
