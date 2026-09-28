using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using AnimusForge;

static void Require(bool ok, string reason)
{
    if (ok) return;
    Console.Error.WriteLine(reason);
    Environment.Exit(1);
}

var window = new PerformanceWindow();
Require(window.BuildCurrentSnapshotForFreeze().Contains("frames=0"), "empty window snapshot");
window.RecordFrameDt(0.05f);
window.RecordElapsed("scope", Stopwatch.GetTimestamp() - Stopwatch.Frequency / 100);
window.MarkEvent("event");
string snapshot = window.BuildCurrentSnapshotForFreeze();
Require(snapshot.Contains("frames=1") && snapshot.Contains("slowFrames=1") && snapshot.Contains("name=scope"), "snapshot counters");
long due = DateTime.UtcNow.AddSeconds(31).Ticks;
Require(window.IsFlushDue(due), "flush due");
int contextReads = 0;
var lines = window.BuildFlushLines(due, () => { contextReads++; return "state=map"; });
Require(lines.Count >= 3 && lines[0].Contains("state=map") && lines[1].Contains("name=scope") && lines[2].Contains("name=event"), "flush output");
Require(!window.IsFlushDue(due), "window reset");
Require(window.BuildFlushLines(due, () => { contextReads++; return "state=map"; }).Count == 0
    && contextReads == 1, "no duplicate flush or context read");
Console.WriteLine("PASS PerformanceWindow counters/snapshot/flush/reset");

var trace = new DiagnosticTraceContext();
var outer = trace.Push("scene", "hero", "npc", null);
var inner = trace.Push(null, null, null, null);
Require(inner.TraceId == outer.TraceId && inner.Channel == "scene" && trace.Current == inner, "nested trace inheritance");
trace.Restore(outer);
Require(trace.Current == outer, "nested restore");
Require(System.Threading.Tasks.Task.Run(() => {
    Require(trace.Current == outer, "async context flows to child");
    trace.Push("child", null, null, null);
    return trace.CurrentChannel;
}).GetAwaiter().GetResult() == "child" && trace.Current == outer, "async child cannot overwrite parent context");
trace.Restore(null);
Require(trace.CurrentTraceId == "", "outer restore");
Console.WriteLine("PASS DiagnosticTraceContext nested/restore");

var metrics = new MetricWindow();
metrics.Record("api", true, 5);
metrics.Record("api", false, 10);
metrics.Record("negative", true, -1);
Require(!metrics.TryDrain(DateTime.UtcNow, out _, out _), "metrics not yet due");
Require(metrics.TryDrain(DateTime.UtcNow.AddSeconds(181), out double seconds, out var drained), "metrics due");
Require(seconds >= 180 && drained.Count == 2 && drained.Find(p => p.Key == "api").Value.Count == 2
    && drained.Find(p => p.Key == "api").Value.Ok == 1 && drained.Find(p => p.Key == "api").Value.Err == 1
    && drained.Find(p => p.Key == "api").Value.MaxMs == 10
    && drained.Find(p => p.Key == "negative").Value.SumMs == 0,
    "metric bucket and drain");
Require(metrics.TryDrain(DateTime.UtcNow.AddSeconds(362), out _, out var emptyWindow)
    && emptyWindow.Count == 0, "metric window clears after drain");
Console.WriteLine("PASS MetricWindow counter/window");

int flushed = 0;
var queue = new BoundedLogWriteQueue(_ => true, batches => {
    foreach (KeyValuePair<string, StringBuilder> pair in batches) {
        if (pair.Value.ToString() == "one") Interlocked.Increment(ref flushed);
    }
}, _ => { });
Require(queue.Enqueue("synthetic", "one"), "queue enqueue");
Require(SpinWait.SpinUntil(() => Volatile.Read(ref flushed) == 1 && queue.WriterRunning == 0, 3000), "queue flush");
Require(queue.Count == 0, "queue drained");
int attempts = 0;
var recoverable = new BoundedLogWriteQueue(_ => true, batches => {
    if (batches.Count == 0) return;
    if (Interlocked.Increment(ref attempts) == 1) throw new InvalidOperationException("synthetic sink failure");
}, _ => { });
recoverable.Enqueue("synthetic", "first");
Require(SpinWait.SpinUntil(() => Volatile.Read(ref attempts) == 1 && recoverable.WriterRunning == 0, 3000), "first worker exited");
recoverable.Enqueue("synthetic", "second");
Require(SpinWait.SpinUntil(() => Volatile.Read(ref attempts) == 2 && recoverable.WriterRunning == 0, 3000), "worker restart after sink failure");
Console.WriteLine("PASS BoundedLogWriteQueue flush/restart");

using var entered = new ManualResetEventSlim();
using var release = new ManualResetEventSlim();
var bounded = new BoundedLogWriteQueue(_ => true, batches => {
    if (batches.Count == 0) return;
    entered.Set();
    release.Wait();
}, _ => { });
try {
    Require(bounded.Enqueue("synthetic", "first"), "start bounded worker");
    Require(entered.Wait(3000), "worker reached synthetic blocked sink");
    for (int i = 0; i < 8192; i++) Require(bounded.Enqueue("synthetic", "x"), "hard queue capacity");
    Require(!bounded.Enqueue("synthetic", "x") && !bounded.Enqueue("synthetic", "x", isVerbose: true), "normal and verbose backpressure");
    Require(bounded.Count == 8192 && bounded.TryTakeDroppedSummary(DateTime.UtcNow.Ticks, out long verboseDrops, out long normalDrops)
        && verboseDrops == 1 && normalDrops == 1, "bounded counters and drop summary");
} finally { release.Set(); }
Require(SpinWait.SpinUntil(() => bounded.Count == 0 && bounded.WriterRunning == 0, 5000), "bounded drain after release");
Console.WriteLine("PASS BoundedLogWriteQueue hard-cap/drop/recovery");

var freeze = new FreezeWatchState();
int mainThread = Thread.CurrentThread.ManagedThreadId;
Require(!freeze.IsKnownMainThread(mainThread), "main thread unknown before first frame");
long frameTimestamp = Stopwatch.GetTimestamp();
freeze.EnsureMainThread(mainThread);
long previousFrame = freeze.BeginFrameHeartbeat(frameTimestamp, DateTime.UtcNow.Ticks);
Require(previousFrame == 0 && freeze.FrameIndex == 0 && freeze.AdvanceFrame() == 1
    && freeze.IsKnownMainThread(mainThread), "first frame heartbeat precedes frame advance");
var outerScope = freeze.EnterScope("outer", mainThread, true, Stopwatch.GetTimestamp());
var innerScope = freeze.EnterScope("inner", mainThread, true, Stopwatch.GetTimestamp());
Require(freeze.Capture().ActiveScope == "inner", "nested active scope");
freeze.CompleteScope(innerScope, Stopwatch.GetTimestamp(), 1.0);
Require(freeze.Capture().ActiveScope == "outer", "inner scope restores parent");
freeze.CompleteScope(outerScope, Stopwatch.GetTimestamp(), 2.0);
Require(freeze.Capture().ActiveScope == "" && freeze.Capture().LastCompleted.Contains("outer elapsedMs=2.00"), "outer scope completes");
freeze.EndFrame(Stopwatch.GetTimestamp());
Require(freeze.Capture().LastCompleted == "SubModule.OnApplicationTick.frame_end", "frame end state");
for (int i = 0; i < FreezeWatchState.RecentEventLimit + 3; i++)
    freeze.RecordEvent("mark", "event-" + i, "", mainThread);
var recent = freeze.RecentEventsSnapshot();
Require(recent.Count == FreezeWatchState.RecentEventLimit && !recent[0].Contains("event-0")
    && recent[^1].Contains("event-258"), "event ring wrap and order");
Require(freeze.TryStartMonitor() && !freeze.TryStartMonitor(), "single monitor start gate");
Console.WriteLine("PASS FreezeWatchState heartbeat/scope/ring");
