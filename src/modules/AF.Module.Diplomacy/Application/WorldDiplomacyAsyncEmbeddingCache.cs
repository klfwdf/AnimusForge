using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge;

internal class WorldDiplomacyPreparationException : Exception
{
    internal WorldDiplomacyPreparationException(string message) : base(message) { }
}
internal sealed class WorldDiplomacyPreparationPendingException : WorldDiplomacyPreparationException
{
    internal WorldDiplomacyPreparationPendingException() : base("diplomacy_embedding_pending") { }
}

// Main-thread owner; only immutable text and a captured inference engine enter
// workers. Three pending calls and 128 retained results bound CPU and memory.
internal sealed class WorldDiplomacyAsyncEmbeddingCache : IDisposable
{
    private static readonly SemaphoreSlim InferenceSlots = new(3, 3);
    private sealed class Entry : IDisposable
    {
        internal readonly CancellationTokenSource Cancellation = new();
        internal long Started = Stopwatch.GetTimestamp();
        internal long Used;
        internal Task<float[]> Work;
        public void Dispose() { Cancellation.Cancel(); Cancellation.Dispose(); }
    }
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private long _generation = -1, _sequence;
    private object _engine;
    internal int Count => _entries.Count;

    internal float[] Get(string text, long generation, object engine, Func<string, float[]> infer)
    {
        if (_generation != generation || !ReferenceEquals(_engine, engine))
        { Dispose(); _generation = generation; _engine = engine; }
        text ??= "";
        if (_entries.TryGetValue(text, out var entry))
        {
            entry.Used = ++_sequence;
            if (!entry.Work.IsCompleted)
            {
                if (Stopwatch.GetTimestamp() - entry.Started >= 180L * Stopwatch.Frequency)
                {
                    _entries.Remove(text); entry.Dispose();
                    throw new WorldDiplomacyPreparationException("diplomacy_embedding_timeout");
                }
                throw new WorldDiplomacyPreparationPendingException();
            }
            try
            {
                // Retain failed/null inference too: a partially prepared batch must
                // not restart the same unavailable vector on every retry.
                return entry.Work.GetAwaiter().GetResult(); // completed only
            }
            catch { if (_entries.Remove(text)) entry.Dispose(); throw; }
        }
        int pending = 0;
        string oldestKey = null;
        Entry oldestEntry = null;
        foreach (var pair in _entries)
        {
            if (!pair.Value.Work.IsCompleted) pending++;
            else if (oldestEntry == null || pair.Value.Used < oldestEntry.Used)
            { oldestKey = pair.Key; oldestEntry = pair.Value; }
        }
        if (pending >= 3)
            throw new WorldDiplomacyPreparationPendingException();
        if (_entries.Count >= 128)
        {
            _entries.Remove(oldestKey); oldestEntry.Dispose();
        }
        entry = new Entry { Used = ++_sequence };
        CancellationToken token = entry.Cancellation.Token;
        string input = text;
        entry.Work = Task.Run(async () =>
        {
            await InferenceSlots.WaitAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                float[] vector = infer(input);
                token.ThrowIfCancellationRequested();
                return vector;
            }
            finally { InferenceSlots.Release(); }
        }, token);
        // Retirement never waits for native inference or publishes its late result.
        entry.Work.ContinueWith(t => { var observed = t.Exception; }, CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        _entries.Add(text, entry);
        throw new WorldDiplomacyPreparationPendingException();
    }
    public void Dispose()
    {
        foreach (var entry in _entries.Values) entry.Dispose();
        _entries.Clear(); _engine = null; _sequence = 0;
    }
}
