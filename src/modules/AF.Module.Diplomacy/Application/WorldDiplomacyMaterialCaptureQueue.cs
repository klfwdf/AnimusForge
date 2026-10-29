using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace AnimusForge;

// Main-thread cooperative capture. A step may start a worker or consume its
// completed value, but never waits for it. No partial snapshot can reach a writer.
internal sealed class WorldDiplomacyMaterialCaptureQueue : IDisposable
{
    private sealed class Entry : IDisposable
    {
        internal readonly CancellationTokenSource Cancellation = new();
        internal IEnumerator<string> Steps;
        internal long LastUse;
        internal readonly long Started = Stopwatch.GetTimestamp();
        internal int StepsRun;
        internal double ActiveMs;
        internal string Result;
        internal Exception Error;
        internal bool Complete;
        public void Dispose() { Cancellation.Cancel(); Steps?.Dispose(); Cancellation.Dispose(); }
    }
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private long _generation = -1;
    private long _nextPump;
    internal int Count => _entries.Count;
    internal string Advance(string key, long generation, Func<CancellationToken, IEnumerator<string>> create, Action<string> log)
    {
        if (_generation != generation) { Dispose(); _generation = generation; }
        long now = Stopwatch.GetTimestamp();
        foreach (var expired in _entries.Where(x => now - x.Value.LastUse > 60L * Stopwatch.Frequency).Select(x => x.Key).ToList())
        { _entries[expired].Dispose(); _entries.Remove(expired); }
        if (!_entries.TryGetValue(key, out var entry))
        {
            if (_entries.Count >= 3) return null;
            entry = new Entry { LastUse = now };
            entry.Steps = create(entry.Cancellation.Token);
            _entries.Add(key, entry);
        }
        entry.LastUse = now;
        try
        {
            Step(entry, log);
            if (!entry.Complete) return null;
            if (entry.Error != null) throw entry.Error;
            log?.Invoke("diplomacy.material_capture_completed elapsedMs="
                + ((Stopwatch.GetTimestamp() - entry.Started) * 1000.0 / Stopwatch.Frequency).ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                + " mainThreadMs=" + entry.ActiveMs.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " steps=" + entry.StepsRun);
            _entries.Remove(key); entry.Dispose();
            return entry.Result;
        }
        catch { if (_entries.Remove(key)) entry.Dispose(); throw; }
    }
    // At most three already queued captures, one nonblocking step each at 20 Hz.
    // Two milliseconds between steps is a cooperative budget, not a preemption
    // of a single game-object read. No world scan or disk IO occurs in this pump.
    internal void Pump(long generation, Action<string> log)
    {
        if (_generation != generation) { Dispose(); _generation = generation; }
        if (_entries.Count == 0) return;
        long now = Stopwatch.GetTimestamp();
        if (now < _nextPump) return;
        _nextPump = now + Stopwatch.Frequency / 20;
        foreach (var entry in _entries.Values)
        {
            Step(entry, log);
            if (Stopwatch.GetTimestamp() - now >= Stopwatch.Frequency / 500) break;
        }
    }
    private static void Step(Entry entry, Action<string> log)
    {
        if (entry.Complete) return;
        var watch = Stopwatch.StartNew();
        try
        {
            // LastUse is refreshed by every poll, so idle eviction cannot bound
            // a worker that never completes. Fail the whole capture, never its
            // partial rows. The material owner retains its existing retry cap.
            if (Stopwatch.GetTimestamp() - entry.Started >= 180L * Stopwatch.Frequency)
            {
                entry.Cancellation.Cancel();
                throw new TimeoutException("material_capture_timeout");
            }
            bool more = entry.Steps.MoveNext();
            entry.Result = more ? entry.Steps.Current : null;
            if (!more) throw new InvalidOperationException("material_capture_ended_without_snapshot");
            entry.Complete = entry.Result != null;
        }
        catch (Exception ex) { entry.Error = ex; entry.Complete = true; }
        double ms = watch.Elapsed.TotalMilliseconds;
        entry.StepsRun++; entry.ActiveMs += ms;
        if (ms > 25) log?.Invoke("diplomacy.material_capture_slow_step ms=" + ms.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
    }
    public void Dispose()
    {
        foreach (var entry in _entries.Values) entry.Dispose();
        _entries.Clear();
        _nextPump = 0;
    }
}
