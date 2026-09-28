using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge;

internal static class HistoryCaptureReplay
{
    private sealed class Port : IWorldDiplomacyHistoryCapturePort
    {
        internal readonly List<string> Events = new();
        internal int Hour = 6;
        internal long Revision = 2;
        internal bool FailPolicy;
        public void EnsureInitialized() => Events.Add("init");
        public int CurrentHour() => Hour;
        public long WeeklyRevision() => Revision;
        public IEnumerable<WorldDiplomacyWeeklyArtifact> WeeklyArtifacts() => new[] { new WorldDiplomacyWeeklyArtifact("w", "title", "text", 1, "date") };
        public void AppendWeekly(WorldDiplomacyWeeklyArtifact item) => Events.Add("weekly:" + item.SourceId);
        public void SyncPolicyArtifacts(int count) { Events.Add("policy:" + count); if (FailPolicy) throw new InvalidOperationException("policy"); }
        public void RetryDeferredEntries() => Events.Add("retry");
        public void SyncSources(bool force) => Events.Add("sync:" + force);
        public string Render(long sequence) { Events.Add("render:" + sequence); return "history"; }
    }
    internal static void Run()
    {
        int hour = -1; long revision = -1;
        var port = new Port();
        WorldDiplomacyHistoryCaptureApplication.SyncSources(ref port, false, ref hour, ref revision, 4);
        Test.True(string.Join(",", port.Events) == "init,weekly:w,policy:1" && hour == 6 && revision == 2,
            "source sync preserves initialization, changed weekly revision, bounded policy and clock order");
        port.Events.Clear();
        WorldDiplomacyHistoryCaptureApplication.SyncSources(ref port, false, ref hour, ref revision, 4);
        Test.True(port.Events.SequenceEqual(new[] { "init" }), "hour gate prevents repeated source scans");
        port.Events.Clear();
        WorldDiplomacyHistoryCaptureApplication.SyncSources(ref port, true, ref hour, ref revision, 4);
        Test.True(string.Join(",", port.Events) == "init,policy:4", "forced sync expands policy batches but preserves weekly revision gate");
        port.Hour++; port.Revision++; port.FailPolicy = true; port.Events.Clear();
        try { WorldDiplomacyHistoryCaptureApplication.SyncSources(ref port, false, ref hour, ref revision, 4); }
        catch (InvalidOperationException) { }
        Test.True(hour == 6 && revision == 3, "failed policy sync retains successful weekly cursor but does not advance hour gate");
        port.FailPolicy = false; port.Events.Clear();
        WorldDiplomacyHistoryCaptureApplication.SyncSources(ref port, false, ref hour, ref revision, 4);
        Test.True(string.Join(",", port.Events) == "init,policy:1" && hour == 7, "retry avoids duplicate weekly publication");
        var storage = new WorldDiplomacyStorage();
        storage.CanonicalHistory.NextSequence = 12;
        storage.CanonicalHistory.Revision = 9;
        var job = new WorldDiplomacyJob { SystemPrompt = "system" };
        port.Events.Clear();
        WorldDiplomacyHistoryCaptureApplication.Capture(storage, job, true, long.MaxValue, ref port);
        Test.True(string.Join(",", port.Events) == "retry,sync:True,init,render:11"
            && job.HistoryThroughSequence == 11 && job.HistoryRevision == 9 && job.HistoryPrefixHash.Length > 0,
            "job capture retries sources before stamping the bounded canonical snapshot");
        var current = new WorldDiplomacyExchange { ExchangeId = "current", State = "waiting" };
        var suspended = new WorldDiplomacyExchange { ExchangeId = "paused", State = "suspended_by_player", StateBeforeSuspension = "waiting",
            SuspendedDay = 2, ResponseDueDay = 10, CloseDueDay = 12 };
        storage.ActiveExchange = current; storage.SuspendedExchanges.Add(suspended);
        int scheduled = 0;
        WorldDiplomacyRoundApplication.CompleteExchange(storage, "current", "done", () => 7, day =>
        { Test.True(storage.ActiveExchange == null && suspended.State == "suspended_by_player", "next-round scheduling precedes suspended exchange restore"); scheduled = day; });
        Test.True(current.State == "completed" && scheduled == 7 && storage.ActiveExchange == suspended
            && suspended.ResponseDueDay == 15 && storage.SuspendedExchanges.Count == 0,
            "completion commits, schedules and restores the paused exchange with shifted deadline");
        scheduled = 0;
        WorldDiplomacyRoundApplication.CompleteExchange(storage, "current", "duplicate", () => 8, day => scheduled = day);
        Test.True(scheduled == 0 && storage.ActiveExchange == suspended, "late duplicate exchange completion cannot close the replacement");
    }
}
