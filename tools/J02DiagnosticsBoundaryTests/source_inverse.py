"""Bounded J02 source boundary check against the intent checkpoint; no game data is read."""

from pathlib import Path
import re
import subprocess


ROOT = Path(__file__).resolve().parents[2]
BASE = "5c97bfb0"


def old(path):
    return subprocess.check_output(
        ["git", "show", f"{BASE}:{path}"], cwd=ROOT
    ).decode("utf-8-sig")


def new(path):
    return (ROOT / path).read_text(encoding="utf-8-sig")


def required(source, *fragments):
    for fragment in fragments:
        assert fragment in source, f"missing expected J02 source invariant: {fragment}"


def absent(source, *fragments):
    for fragment in fragments:
        assert fragment not in source, f"old J02 owner remains in facade: {fragment}"


def ordered(source, *fragments):
    cursor = 0
    for fragment in fragments:
        found = source.find(fragment, cursor)
        assert found >= 0, f"J02 operation ordering changed at: {fragment}"
        cursor = found + len(fragment)


def unchanged_region(before, after, start, end):
    def region(source):
        first = source.index(start)
        return source[first:source.index(end, first)].replace("\r\n", "\n")
    assert region(before) == region(after), f"protected J02 adapter region changed: {start}"


def method_body(source, name):
    match = re.search(r"\b" + re.escape(name) + r"\([^)]*\)\s*\{", source)
    assert match, f"missing method: {name}"
    start = match.end()
    depth = 1
    cursor = start
    while depth:
        depth += (source[cursor] == "{") - (source[cursor] == "}")
        cursor += 1
    return re.sub(r"\s+", "", source[start:cursor - 1])


legacy_log = old("Logger.cs")
log = new("Logger.cs")
queue = new("src/AF.Foundation.Runtime/Diagnostics/BoundedLogWriteQueue.cs")
trace = new("src/AF.Foundation.Runtime/Diagnostics/DiagnosticTraceContext.cs")
metric = new("src/AF.Foundation.Runtime/Diagnostics/MetricWindow.cs")
required(legacy_log, "MaxLogWriteQueueItems = 4096", "HardMaxLogWriteQueueItems = 8192",
         "_traceState.Value = _prev", "lock (_metricsLock)", "Task.Run(ProcessLogWriteQueue)")
required(queue, "MaxLogWriteQueueItems = 4096", "HardMaxLogWriteQueueItems = 8192",
         "LogBatchFlushItemCount = 256", "_queue.IsEmpty || Interlocked.CompareExchange(ref _running, 1, 0)",
         "if (!_queue.IsEmpty) TryStartWriter()", "_processQueue = ProcessQueue", "Task.Run(_processQueue)")
ordered(queue[queue.index("private void ProcessQueue()") :], "_queue.TryDequeue", "Interlocked.Decrement(ref _count)",
        "AppendToBatch", "_flushBatches(batches)", "Interlocked.Exchange(ref _running, 0)", "_queue.IsEmpty")
required(trace, "AsyncLocal<ScopeState>", "Parent = previous", "_current.Value = previous")
required(metric, "lock (_sync)", "if (utcNow < _nextFlushUtc) return false",
         "_metrics.Clear()", "_nextFlushUtc = utcNow.AddSeconds(180.0)")
required(log, "LogQueue.Enqueue(path, content, isVerbose, bypassBackpressure)",
         "TraceContext.Restore(_prev)", "Metrics.TryDrain(DateTime.UtcNow",
         "_tokenStatsWriteQueue", "RecordHitRate")
absent(log, "_logWriteQueue", "_logWriterRunning", "_traceState", "_metricsLock")
unchanged_region(legacy_log, log, "\tpublic static void RecordHitRate(", "\tprivate static void MaybeFlushMetrics(")

legacy_perf = old("PerfProbe.cs")
perf = new("PerfProbe.cs")
window = new("src/AF.Foundation.Runtime/Diagnostics/PerformanceWindow.cs")
required(legacy_perf, "FlushIntervalSeconds = 30.0", "EnabledStateRefreshIntervalTicks = TimeSpan.FromMilliseconds(250.0).Ticks")
required(window, "FlushIntervalSeconds = 30.0", "TopBucketCount = 8", "lock (SyncRoot)",
         "Buckets.Clear()", "Events.Clear()", "BuildCurrentSnapshotForFreeze()",
         "{runtimeContextProvider()}")
required(perf, "DuelSettings.GetSettings()?.EnablePerformanceMonitor", "Window.RecordFrameDt(dt)",
         "RuntimeContextProvider = BuildRuntimeContext", "Window.BuildFlushLines(nowTicks, RuntimeContextProvider)",
         'return IsEnabled() ? Window.BuildCurrentSnapshotForFreeze() : "disabled_by_mcm"')
ordered(window[window.index("internal List<string> BuildFlushLines") :], "lock (SyncRoot)",
        "Buckets.Clear()", "Events.Clear()", "{runtimeContextProvider()}")
absent(perf, "_frameCount", "_nextFlushUtcTicks", "Dictionary<string, Bucket>")
for method in ("ResetWindow", "RecordFrameDt", "CompareBucketsByMaxThenSum"):
    assert method_body(legacy_perf, method) == method_body(window, method), f"Perf algorithm drift: {method}"
assert method_body(legacy_perf, "RecordElapsed").removesuffix("FlushIfDue();") == method_body(window, "RecordElapsed")

legacy_freeze = old("FreezeWatchdog.cs")
freeze = new("FreezeWatchdog.cs")
state = new("src/AF.Foundation.Runtime/Diagnostics/FreezeWatchState.cs")
required(legacy_freeze, "_recentEventNext = (_recentEventNext + 1) % RecentEventLimit",
         "_currentScope = state.Parent", "new Thread(MonitorLoop)", "MiniDumpWriteDump")
required(state, "RecentEventLimit = 256", "_recentEventNext = (_recentEventNext + 1) % RecentEventLimit",
         "_currentScope = state.Parent", "lock (_sync)", "TryStartMonitor()", "Interlocked.Exchange(ref _lastMainHeartbeatTimestamp, timestamp)")
required(freeze, "State.EnsureMainThread(threadId)", "State.BeginFrameHeartbeat(now, DateTime.UtcNow.Ticks)",
         "Interlocked.Exchange(ref _hangDumpCapturedForCurrentStall, 0);", "State.AdvanceFrame()",
         "State.EnterScope(name, threadId, isMainThread, startTimestamp)",
         "State.EndFrame(now)", "State.RecordEvent(kind, name, detail, threadId)",
         "new Thread(MonitorLoop)", "MiniDumpWriteDump", "Campaign.Current", "State.SetRuntimeContext(")
ordered(freeze[freeze.index("internal static void BeginFrame") :], "State.EnsureMainThread(threadId)",
        "Stopwatch.GetTimestamp()", "State.BeginFrameHeartbeat(now, DateTime.UtcNow.Ticks)",
        "Interlocked.Exchange(ref _hangDumpCapturedForCurrentStall, 0)", "State.AdvanceFrame()")
ordered(freeze[freeze.index("private static void EnsureMonitorStarted") :], "State.TryStartMonitor()", "new Thread(MonitorLoop)")
absent(freeze, "_recentEventNext", "_currentScope", "_lastMainHeartbeatTimestamp", "_monitorStarted")
unchanged_region(legacy_freeze, freeze, "\tprivate static void WriteImmediate(", "\tprivate static string BuildStateSummary(")
unchanged_region(legacy_freeze, freeze, "\tprivate static string BuildProcessDiagnostics(", "\tprivate static string Sanitize(")

print("PASS source-linked J02 exact protected regions, Perf body lifts, owner/adapter gates and ordering")
