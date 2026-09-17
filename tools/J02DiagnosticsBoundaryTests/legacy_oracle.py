"""Execute methods extracted from the J02 intent checkpoint against new owners."""

from pathlib import Path
import json
import os
import subprocess
import uuid


ROOT = Path(__file__).resolve().parents[2]
BASE = "5c97bfb0"
OUT = ROOT / "artifacts/tests/j02-diagnostics-a/oracle" / uuid.uuid4().hex
OUT.mkdir(parents=True, exist_ok=False)
DOTNET = ROOT / "local/dotnet/8.0.425/dotnet.exe"


def old(path):
    return subprocess.check_output(["git", "show", f"{BASE}:{path}"], cwd=ROOT).decode("utf-8-sig")


def block(source, marker):
    start = source.index(marker)
    brace = source.index("{", start)
    depth = 1
    end = brace + 1
    while depth:
        depth += (source[end] == "{") - (source[end] == "}")
        end += 1
    return source[start:end]


log = old("Logger.cs")
freeze = old("FreezeWatchdog.cs")
trace_blocks = [block(log, marker) for marker in (
    "\tprivate sealed class TraceScopeState", "\tprivate sealed class TraceScope : IDisposable",
    "\tpublic static IDisposable BeginTrace(", "\tprivate static string NewTraceId(")]
metric_blocks = [block(log, marker) for marker in (
    "\tprivate sealed class MetricBucket", "\tpublic static void Metric(",
    "\tprivate static void MaybeFlushMetrics(")]
queue_blocks = [block(log, marker) for marker in (
    "\tprivate sealed class LogWriteWorkItem", "\tprivate static void EnqueueLogWrite(",
    "\tprivate static void TryEnqueueDroppedLogSummary(", "\tprivate static void TryStartLogWriter(",
    "\tprivate static void ProcessLogWriteQueue(", "\tprivate static void AppendLogWorkItemToBatch(",
    "\tprivate static void DrainQueuedLogWrites(")]
freeze_blocks = [block(freeze, marker) for marker in (
    "\tinternal sealed class ScopeState", "\tinternal readonly struct ScopeToken : IDisposable",
    "\tinternal static ScopeToken Scope(", "\tprivate static void CompleteScope(",
    "\tprivate static void TouchMainHeartbeat(", "\tprivate static bool IsKnownMainThread(",
    "\tprivate static void RecordEvent(", "\tprivate static List<string> BuildRecentEventsSnapshot(",
    "\tprivate static string BuildRecentEventsOneLine(", "\tprivate static string Sanitize(")]


def join(blocks):
    return "\n\n".join(blocks)


legacy = r'''using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace AnimusForge;

internal static class LegacyTrace {
    private static readonly AsyncLocal<TraceScopeState> _traceState = new AsyncLocal<TraceScopeState>();
    private static long _traceSeed;
    public static string CurrentTraceId => _traceState.Value?.TraceId ?? "";
    public static string CurrentChannel => _traceState.Value?.Channel ?? "";
    private static void Obs(string source, string stage, Dictionary<string, object> fields = null) { }
TRACE_BLOCKS
}
internal static class LegacyMetric {
    private static readonly object _metricsLock = new object();
    private static readonly Dictionary<string, MetricBucket> _metrics = new Dictionary<string, MetricBucket>(StringComparer.Ordinal);
    private static DateTime _metricsWindowStartUtc = DateTime.UtcNow;
    private static DateTime _nextMetricsFlushUtc = DateTime.UtcNow.AddSeconds(180.0);
    private static readonly List<string> Lines = new List<string>();
    private static string _modLogPath = "synthetic";
    internal static void Due() { _nextMetricsFlushUtc = DateTime.UtcNow.AddSeconds(-1); MaybeFlushMetrics(); }
    internal static int Count => _metrics.Count;
    internal static string Output => string.Join("|", Lines);
    private static void Obs(string source, string stage, Dictionary<string, object> fields = null) { }
    private static void WriteHumanLine(string path, string source, string message) => Lines.Add(message);
METRIC_BLOCKS
}
internal static class LegacyQueue {
    private const int MaxLogWriteQueueItems = 4096;
    private const int HardMaxLogWriteQueueItems = 8192;
    private const int LogBatchFlushItemCount = 256;
    private const int DroppedLogSummaryIntervalSeconds = 10;
    private static readonly ConcurrentQueue<LogWriteWorkItem> _logWriteQueue = new ConcurrentQueue<LogWriteWorkItem>();
    private static int _logWriteQueueCount, _logWriterRunning;
    private static long _droppedVerboseLogCount, _droppedNormalLogCount, _lastDroppedLogSummaryUtcTicks;
    private static string _modLogPath = "synthetic";
    private static bool IsModLogicEnabled => false;
    internal static Action<Dictionary<string, StringBuilder>> Sink;
    internal static Action<string, string> ImmediateSink;
    internal static int Count => Volatile.Read(ref _logWriteQueueCount);
    internal static int WriterRunning => Volatile.Read(ref _logWriterRunning);
    internal static long DroppedVerbose => Interlocked.Read(ref _droppedVerboseLogCount);
    internal static long DroppedNormal => Interlocked.Read(ref _droppedNormalLogCount);
    internal static void Enqueue(string path, string content, bool verbose = false) => EnqueueLogWrite(path, content, verbose);
    private static bool IsPathEnabled(string path) => true;
    private static void FlushLogBatches(Dictionary<string, StringBuilder> batches) { if (batches.Count > 0) Sink(batches); }
    private static void WriteLogWorkItem(LogWriteWorkItem item) => ImmediateSink?.Invoke(item.Path, item.Content);
QUEUE_BLOCKS
}
internal static class LegacyFreeze {
    private const int RecentEventLimit = 256;
    private const double MainThreadSlowScopeMs = 250.0, BackgroundSlowScopeMs = 1000.0;
    private static readonly object SyncRoot = new object();
    private static readonly string[] RecentEvents = new string[RecentEventLimit];
    private static int _recentEventNext, _recentEventCount, _mainThreadId;
    private static long _frameIndex, _lastMainHeartbeatTimestamp, _lastMainHeartbeatUtcTicks;
    private static string _mainThreadActiveScope = "", _lastCompletedMainScope = "";
    private static long _mainThreadActiveScopeStartTimestamp, _lastCompletedMainScopeUtcTicks;
    [ThreadStatic] private static ScopeState _currentScope;
    internal static void InitializeMainThread(int threadId) => _mainThreadId = threadId;
    internal static string ActiveScope => _mainThreadActiveScope;
    internal static string LastCompleted => _lastCompletedMainScope;
    internal static long LastHeartbeat => Interlocked.Read(ref _lastMainHeartbeatTimestamp);
    internal static void Event(string kind, string name, string detail, int threadId) => RecordEvent(kind, name, detail, threadId);
    internal static List<string> Events() => BuildRecentEventsSnapshot();
    internal static string EventsOneLine(int count) => BuildRecentEventsOneLine(count);
    internal static string Clean(string value, int max) => Sanitize(value, max);
    private static bool IsEnabled() => true;
    private static void EnsureMonitorStarted() { }
    private static void WriteImmediate(string message, bool writeSnapshot) { }
    private static string BuildStateSummary(bool includeRecent) => "";
    private static double TimestampDeltaMs(long start, long end) => Math.Max(0.0, (end - start) * 1000.0 / Stopwatch.Frequency);
FREEZE_BLOCKS
}
'''.replace("TRACE_BLOCKS", join(trace_blocks)).replace("METRIC_BLOCKS", join(metric_blocks)).replace(
    "QUEUE_BLOCKS", join(queue_blocks)).replace("FREEZE_BLOCKS", join(freeze_blocks))
(OUT / "Legacy.cs").write_text(legacy, encoding="utf-8")

program = r'''using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using AnimusForge;
static void Check(bool ok, string name) { if (!ok) { Console.Error.WriteLine(name); Environment.Exit(1); } }
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

using (LegacyTrace.BeginTrace("scene", "hero", "npc", "same")) {
    var current = new DiagnosticTraceContext();
    var outer = current.Push("scene", "hero", "npc", "same");
    Check(LegacyTrace.CurrentTraceId == current.CurrentTraceId && LegacyTrace.CurrentChannel == current.CurrentChannel, "trace outer");
    using (LegacyTrace.BeginTrace(null, null, null, null)) {
        var inner = current.Push(null, null, null, null);
        Check(LegacyTrace.CurrentTraceId == current.CurrentTraceId && LegacyTrace.CurrentChannel == current.CurrentChannel, "trace nested");
        current.Restore(inner.Parent);
    }
    Check(LegacyTrace.CurrentTraceId == current.CurrentTraceId, "trace restore");
    current.Restore(null);
}
Check(LegacyTrace.CurrentTraceId == "", "trace outer restore");
Console.WriteLine("PASS old/new Logger trace oracle");

LegacyMetric.Metric("api", true, 5); LegacyMetric.Metric("api", false, 10);
var metric = new MetricWindow(); metric.Record("api", true, 5); metric.Record("api", false, 10);
LegacyMetric.Due();
Check(metric.TryDrain(DateTime.UtcNow.AddSeconds(181), out _, out var buckets), "metric new due");
var bucket = buckets.Find(x => x.Key == "api").Value;
Check(LegacyMetric.Count == 0 && bucket.Count == 2 && bucket.Ok == 1 && bucket.Err == 1 && bucket.MaxMs == 10
    && LegacyMetric.Output.Contains("api: count=2 ok=1 err=1 avgMs=7.5 maxMs=10"), "metric old/new flush");
Console.WriteLine("PASS old/new Logger metric oracle");

var oldWrites = new ConcurrentQueue<string>(); var newWrites = new ConcurrentQueue<string>();
LegacyQueue.Sink = batches => { foreach (var pair in batches) oldWrites.Enqueue(pair.Value.ToString()); };
LegacyQueue.ImmediateSink = (path, content) => oldWrites.Enqueue(content);
var queue = new BoundedLogWriteQueue(_ => true, batches => { foreach (var pair in batches) newWrites.Enqueue(pair.Value.ToString()); },
    item => newWrites.Enqueue(item.Content));
LegacyQueue.Enqueue("synthetic", "one"); queue.Enqueue("synthetic", "one");
Check(SpinWait.SpinUntil(() => LegacyQueue.WriterRunning == 0 && LegacyQueue.Count == 0 && oldWrites.Count > 0, 3000), "old queue flush");
Check(SpinWait.SpinUntil(() => queue.WriterRunning == 0 && queue.Count == 0 && newWrites.Count > 0, 3000), "new queue flush");
Check(string.Join("", oldWrites) == string.Join("", newWrites), "old/new queue output");
using var oldEntered = new ManualResetEventSlim(); using var oldRelease = new ManualResetEventSlim();
using var newEntered = new ManualResetEventSlim(); using var newRelease = new ManualResetEventSlim();
LegacyQueue.Sink = batches => { if (batches.Count == 0) return; oldEntered.Set(); oldRelease.Wait(); };
var bounded = new BoundedLogWriteQueue(_ => true, batches => { if (batches.Count == 0) return; newEntered.Set(); newRelease.Wait(); }, _ => { });
try {
    LegacyQueue.Enqueue("synthetic", "start"); bounded.Enqueue("synthetic", "start");
    Check(oldEntered.Wait(3000) && newEntered.Wait(3000), "old/new queue worker start");
    for (int i = 0; i < 8192; i++) { LegacyQueue.Enqueue("synthetic", "x"); Check(bounded.Enqueue("synthetic", "x"), "new queue fill"); }
    LegacyQueue.Enqueue("synthetic", "drop"); LegacyQueue.Enqueue("synthetic", "drop", true);
    Check(!bounded.Enqueue("synthetic", "drop") && !bounded.Enqueue("synthetic", "drop", true), "new queue drop");
    Check(LegacyQueue.Count == bounded.Count && LegacyQueue.Count == 8192
        && LegacyQueue.DroppedNormal == bounded.DroppedNormal && LegacyQueue.DroppedVerbose == bounded.DroppedVerbose,
        "old/new queue hard cap and drop counters");
} finally { oldRelease.Set(); newRelease.Set(); }
Check(SpinWait.SpinUntil(() => LegacyQueue.Count == 0 && LegacyQueue.WriterRunning == 0, 5000), "old queue drain");
Check(SpinWait.SpinUntil(() => bounded.Count == 0 && bounded.WriterRunning == 0, 5000), "new queue drain");
Console.WriteLine("PASS old/new Logger bounded queue oracle");

int tid = Thread.CurrentThread.ManagedThreadId;
LegacyFreeze.InitializeMainThread(tid);
var state = new FreezeWatchState(); state.EnsureMainThread(tid);
using (LegacyFreeze.Scope("outer")) {
    var outer = state.EnterScope("outer", tid, true, Stopwatch.GetTimestamp());
    using (LegacyFreeze.Scope("inner")) {
        var inner = state.EnterScope("inner", tid, true, Stopwatch.GetTimestamp());
        Check(LegacyFreeze.ActiveScope == state.Capture().ActiveScope, "old/new freeze inner");
        state.CompleteScope(inner, Stopwatch.GetTimestamp(), 1.0);
    }
    Check(LegacyFreeze.ActiveScope == state.Capture().ActiveScope, "old/new freeze parent restore");
    state.CompleteScope(outer, Stopwatch.GetTimestamp(), 2.0);
}
Check(LegacyFreeze.ActiveScope == state.Capture().ActiveScope && LegacyFreeze.LastCompleted.StartsWith("outer elapsedMs=")
    && state.Capture().LastCompleted.StartsWith("outer elapsedMs="), "old/new freeze completion");
for (int i = 0; i < 259; i++) { LegacyFreeze.Event("mark", "event-" + i, "", tid); state.RecordEvent("mark", "event-" + i, "", tid); }
var oldEvents = LegacyFreeze.Events(); var newEvents = state.RecentEventsSnapshot();
Check(oldEvents.Count == newEvents.Count && oldEvents.Count == 256 && oldEvents[0].Contains("event-3")
    && newEvents[0].Contains("event-3") && oldEvents[^1].Contains("event-258") && newEvents[^1].Contains("event-258"), "old/new freeze ring");
Check(LegacyFreeze.Clean(" a\r\nb\t", 180) == FreezeWatchState.Sanitize(" a\r\nb\t", 180), "old/new freeze sanitize");
Console.WriteLine("PASS old/new Freeze scope/ring/sanitize oracle");
'''
(OUT / "Program.cs").write_text(program, encoding="utf-8")

names = ("DiagnosticTraceContext", "MetricWindow", "BoundedLogWriteQueue", "FreezeWatchState")
include = "\n".join(f'    <Compile Include="{ROOT / "src/AF.Foundation.Runtime/Diagnostics" / (name + ".cs")}" />' for name in names)
(OUT / "Oracle.csproj").write_text(
    '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>'
    '<EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup>'
    '<Compile Include="Legacy.cs" /><Compile Include="Program.cs" />' + include + '</ItemGroup></Project>', encoding="utf-8")
env = os.environ.copy()
env.update(DOTNET_CLI_HOME=str(ROOT / "artifacts/tests/j02-diagnostics-a/home"), DOTNET_CLI_TELEMETRY_OPTOUT="1",
           DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1")
build = subprocess.run([str(DOTNET), "build", str(OUT / "Oracle.csproj"), "--verbosity:quiet"],
                       cwd=ROOT, env=env, capture_output=True, text=True)
(OUT / "build.stdout.log").write_text(build.stdout, encoding="utf-8")
(OUT / "build.stderr.log").write_text(build.stderr, encoding="utf-8")
if build.returncode:
    print(build.stdout[-1500:] + build.stderr[-500:])
    raise SystemExit(build.returncode)
run = subprocess.run([str(DOTNET), str(OUT / "bin/Debug/net8.0/Oracle.dll")],
                     cwd=ROOT, env=env, capture_output=True, text=True, timeout=30)
(OUT / "run.stdout.log").write_text(run.stdout, encoding="utf-8")
(OUT / "run.stderr.log").write_text(run.stderr, encoding="utf-8")
receipt = {"source_revision": BASE, "extracted_methods": len(trace_blocks) + len(metric_blocks) + len(queue_blocks) + len(freeze_blocks),
           "build_exit": build.returncode, "run_exit": run.returncode}
(OUT / "results.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
print(run.stdout)
if run.returncode:
    print(run.stderr[-500:])
    raise SystemExit(run.returncode)
print("receipt=" + str(OUT / "results.json"))
