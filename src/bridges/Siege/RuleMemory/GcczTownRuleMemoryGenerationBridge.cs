using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.SiegeAftermathIntervention;

namespace AnimusForge;

/// <summary>Demand-driven generation; workers receive snapshots and completions commit on the main tick.</summary>
internal static class GcczTownRuleMemoryGenerationBridge
{
    internal delegate bool TryStoreNarrative(SettlementRuleMemoryRecord expected, int day, string narrative);
    private const int MaximumInFlight = 2;
    private static readonly object Gate = new object();
    private static readonly HashSet<string> InFlight = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, DateTime> RetryAfter = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentQueue<Completion> Completed = new ConcurrentQueue<Completion>();
    private static long _epoch;
    private static int _activeWorkers;

    private sealed class Completion
    {
        internal long Epoch;
        internal long Generation;
        internal SettlementRuleMemoryRecord Record;
        internal int Day;
        internal string Narrative;
        internal string Error;
        internal TryStoreNarrative Store;
        internal Action<string> OnStored;
    }

    internal static void Queue(SettlementRuleMemoryRecord record, int currentDay, bool force,
        TryStoreNarrative tryStore, Action<string> onStored)
    {
        if (tryStore == null || !SettlementRuleMemoryEvolution.ShouldGenerate(record, currentDay, force)) return;
        // Even explicit regeneration observes network backpressure; never erase the last good text.
        lock (Gate)
        {
            if (_activeWorkers >= MaximumInFlight || InFlight.Count >= MaximumInFlight || InFlight.Contains(record.SettlementId)
                || (RetryAfter.TryGetValue(record.SettlementId, out var until) && until > DateTime.UtcNow)) return;
        }
        var prompt = TownPromptComposer.BuildSettlementRuleMemoryGenerationPrompt(record, currentDay, GcczTownPromptResourceProvider.GetCatalog());
        if (string.IsNullOrWhiteSpace(prompt.SystemPrompt) || string.IsNullOrWhiteSpace(prompt.UserPrompt)) return;
        var completion = new Completion { Epoch = Interlocked.Read(ref _epoch),
            Generation = SaveRuntimeGuard.CaptureGeneration(), Record = record, Day = currentDay,
            Store = tryStore, OnStored = onStored };
        lock (Gate)
        {
            if (_activeWorkers >= MaximumInFlight || InFlight.Count >= MaximumInFlight || !InFlight.Add(record.SettlementId)) return;
            _activeWorkers++;
            RetryAfter[record.SettlementId] = DateTime.UtcNow.AddMinutes(1);
        }
        Task.Run(() =>
        {
            try
            {
                var messages = new object[] {
                    new { role = "system", content = prompt.SystemPrompt },
                    new { role = "user", content = prompt.UserPrompt } };
                if (AIConfigHandler.TryCallBoundedAuxiliarySimpleDialogueOnceForExternal(messages, 384, 0.45f, out var content, out var error))
                {
                    if (SettlementRuleMemoryNarrativePolicy.TryParseGeneratedResponse(content, out var narrative)) completion.Narrative = narrative;
                    else completion.Error = "invalid_memory_response";
                }
                else completion.Error = error;
            }
            catch (Exception ex) { completion.Error = ex.Message; }
            lock (Gate)
            {
                _activeWorkers--;
                if (completion.Epoch == Interlocked.Read(ref _epoch)) Completed.Enqueue(completion);
            }
        });
    }

    internal static void OnApplicationTick()
    {
        for (int i = 0; i < MaximumInFlight && Completed.TryDequeue(out var result); i++)
        {
            if (result.Epoch != Interlocked.Read(ref _epoch)) continue;
            lock (Gate) InFlight.Remove(result.Record.SettlementId);
            if (SaveRuntimeGuard.IsStale(result.Generation)) continue;
            bool stored = false;
            try
            {
                stored = !string.IsNullOrWhiteSpace(result.Narrative)
                    && result.Store(result.Record, result.Day, result.Narrative);
                if (stored) result.OnStored?.Invoke(result.Record.SettlementId);
            }
            catch (Exception ex) { result.Error = ex.Message; }
            if (!stored)
            {
                lock (Gate) RetryAfter[result.Record.SettlementId] = DateTime.UtcNow.AddMinutes(1);
            }
            Logger.Log("GcczTownRuleMemory", (stored ? "Generated town memory. Settlement=" : "Town memory generation was not stored. Settlement=")
                + result.Record.SettlementId + (stored ? "" : ", Reason=" + (result.Error ?? "source_changed")));
        }
    }

    internal static void Reset()
    {
        lock (Gate)
        {
            Interlocked.Increment(ref _epoch);
            InFlight.Clear();
            RetryAfter.Clear();
            while (Completed.TryDequeue(out _)) { }
        }
    }
}
