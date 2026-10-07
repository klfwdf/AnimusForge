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
        public int CurrentHour() => Hour;
        public long WeeklyRevision() => Revision;
        public IEnumerable<WorldDiplomacyWeeklyArtifact> WeeklyArtifacts() => new[] { new WorldDiplomacyWeeklyArtifact("w", "title", "text", 1, "date") };
    }
    private sealed class Orch : FakeOrchestration
    {
        private readonly Port _p;
        internal bool FailPolicy;
        internal Orch(Port port) { _p = port; }
        public override void EnsureCanonicalHistoryInitialized() => _p.Events.Add("init");
        public override bool AppendCanonicalHistoryWeeklyArtifact(WorldDiplomacyWeeklyArtifact item) { _p.Events.Add("weekly:" + item.SourceId); return true; }
        public override void SyncPublishedPolicyArtifacts(int maxBatches) { _p.Events.Add("policy:" + maxBatches); if (FailPolicy) throw new InvalidOperationException("policy"); }
        public override void RetryDeferredCanonicalHistoryEntries(int maxAttempts) => _p.Events.Add("retry");
        public override void SyncCanonicalHistorySources(bool force) => _p.Events.Add("sync:" + force);
        public override string BuildCanonicalHistoryBlock(long throughSequence) { _p.Events.Add("render:" + throughSequence); return "history"; }
    }
    internal static void Run()
    {
        var newsHost = new FakeOrchestrationHost();
        var newsOwner = new WorldDiplomacyOrchestration(newsHost, new WorldDiplomacyRuntimeState());
        newsOwner.ReplaceStorage(new WorldDiplomacyStorage());
        newsHost.Calls.Clear();
        newsOwner.RecordDiplomacyWeeklyMaterial(new WorldDiplomacyDocument { DocumentId = "unpublished" });
        Test.True(newsHost.Calls.Count == 0, "unpublished diplomacy document cannot enter bulletin or weekly material");
        newsOwner.RecordDiplomacyWeeklyMaterial(new WorldDiplomacyDocument {
            DocumentId = "published", IsReadyForPublication = true, AuthorKingdomId = "a", TargetKingdomId = "b", Day = 12 });
        Test.True(newsHost.Calls.Count(x => x == "RecordDiplomacyBulletinMaterial") == 1
            && newsHost.Calls.Count(x => x == "RecordWorldDiplomacyWeeklyMaterialExternal") == 2,
            "one declaration capture precedes separate world and kingdom weekly projections");
        int hour = -1; long revision = -1;
        var port = new Port();
        var orch = new Orch(port);
        WorldDiplomacyHistoryCaptureApplication.SyncSources(port, orch, false, ref hour, ref revision, 4);
        Test.True(string.Join(",", port.Events) == "init,weekly:w,policy:1" && hour == 6 && revision == 2,
            "source sync preserves initialization, changed weekly revision, bounded policy and clock order");
        port.Events.Clear();
        WorldDiplomacyHistoryCaptureApplication.SyncSources(port, orch, false, ref hour, ref revision, 4);
        Test.True(port.Events.SequenceEqual(new[] { "init" }), "hour gate prevents repeated source scans");
        port.Events.Clear();
        WorldDiplomacyHistoryCaptureApplication.SyncSources(port, orch, true, ref hour, ref revision, 4);
        Test.True(string.Join(",", port.Events) == "init,policy:4", "forced sync expands policy batches but preserves weekly revision gate");
        port.Hour++; port.Revision++; orch.FailPolicy = true; port.Events.Clear();
        try { WorldDiplomacyHistoryCaptureApplication.SyncSources(port, orch, false, ref hour, ref revision, 4); }
        catch (InvalidOperationException) { }
        Test.True(hour == 6 && revision == 3, "failed policy sync retains successful weekly cursor but does not advance hour gate");
        orch.FailPolicy = false; port.Events.Clear();
        WorldDiplomacyHistoryCaptureApplication.SyncSources(port, orch, false, ref hour, ref revision, 4);
        Test.True(string.Join(",", port.Events) == "init,policy:1" && hour == 7, "retry avoids duplicate weekly publication");
        var storage = new WorldDiplomacyStorage();
        storage.CanonicalHistory.NextSequence = 12;
        storage.CanonicalHistory.Revision = 9;
        var job = new WorldDiplomacyJob { SystemPrompt = "system" };
        port.Events.Clear();
        WorldDiplomacyHistoryCaptureApplication.Capture(storage, job, true, long.MaxValue, port, orch);
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
