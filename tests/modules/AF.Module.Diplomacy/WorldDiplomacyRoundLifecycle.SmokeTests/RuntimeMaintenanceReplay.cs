using AnimusForge;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

internal static class RuntimeMaintenanceReplay
{
    internal static void Run()
    {
        CampaignApplicationReplay.Run(); HistoryCaptureReplay.Run(); PersistenceSyncReplay.DailyArchiveIsUntouched();
        PolicyFilter(); PolicyMaterial(); Index();
    }

    private static void PolicyFilter()
    {
        Test.True(!PolicyMaterialRecordFilter.Since("{\"Day\":1,\"Effects\": BROKEN OLD BODY", "Day", 20),
            "old policy is rejected before parsing its body");
        Test.True(PolicyMaterialRecordFilter.Since("{\"Day\":20,\"Effects\":[]}", "Day", 20), "window includes boundary day");
        Test.True(!PolicyMaterialRecordFilter.Since("{\"nested\":{\"Day\":99},\"Day\":2}", "Day", 20), "nested date cannot replace publication day");
        Test.True(PolicyMaterialRecordFilter.Since("legacy", "Day", 0), "non-diplomacy callers retain existing loading behavior");
        Test.True(PolicyMaterialRecordFilter.HasRecord("{\"RecordId\":\"\",\"PolicyObjectId\":\"p\"}", new HashSet<string> { "p" }), "old player record identity fallback remains supported");
        var s = new WorldDiplomacyStorage(); s.ProcessedPolicySignalKeys.Add("policy:p:b");
        WorldDiplomacyPolicySignalSnapshot Signal(string revision) => new(revision, "p", "kingdom", "name", "body", "a", "a", "b", "b", "effect", 20);
        Test.True(WorldDiplomacyPolicyRoundApplication.WasUsed(s, Signal("revision1")), "legacy adoption is preserved on first versioned query");
        Test.True(!WorldDiplomacyPolicyRoundApplication.WasUsed(s, Signal("revision2")), "a later substantive revision is not permanently excluded by its old policy ID");
    }

    private sealed class PolicyHost : EventStructureReplay.Host, IDiplomacyPolicyMaterialHost
    {
        internal int Reads, Since;
        internal bool Fail;
        internal IReadOnlyCollection<string> Countries;
        public override int MaxProcessedPolicySignalKeys() => 256;
        public IReadOnlyList<WorldDiplomacyPolicySignalSnapshot> QueryPolicyMaterials(int minimumDay, IReadOnlyCollection<string> kingdoms)
        {
            Reads++; Since = minimumDay; Countries = kingdoms;
            if (Fail) throw new InvalidOperationException("policy fixture unavailable");
            return Enumerable.Range(0, 8).Select(i => new WorldDiplomacyPolicySignalSnapshot("revision-" + i,
                "policy-" + i, "kingdom", "Policy " + i, "Original policy with condition: only if grain arrives.",
                "b", "B", "a", "A", "Conditional levy", CurrentDayValue - i)).ToList();
        }
    }
    private static void PolicyMaterial()
    {
        var failed = new PolicyHost { Fail = true, CurrentDayValue = 40 };
        var blocked = new WorldDiplomacyOrchestration(failed, new WorldDiplomacyRuntimeState()); failed.Owner = blocked;
        blocked.TryScheduleNormalRound(); var failedJob = blocked.CurrentStorage.Jobs.Single();
        blocked.PrepareSharedRequest(failedJob);
        Test.True(failedJob.SuspendedReason == "event_policy_material_unavailable" && string.IsNullOrEmpty(failedJob.EventMaterialSnapshot),
            "unavailable policy evidence suspends preparation without publishing a partial frozen set");
        var h = new PolicyHost { CurrentDayValue = 40, PublishEnabled = true, PlayerAwareExecution = true, OralRange = (0, 3000) };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        o.CurrentStorage.ProcessedPolicySignalKeys.Add("revision-0");
        o.TryScheduleNormalRound();
        Test.True(h.Reads == 0, "scheduling a possible event does not read policy history");
        var job = o.CurrentStorage.Jobs.Single(x => x.Kind == "event_concept");
        o.PrepareSharedRequest(job);
        Test.True(h.Reads == 1 && h.Since == 26 && h.Countries.Count > 0, "only exploration requests recent related policy material");
        var policies = JArray.Parse(job.EventMaterialSnapshot).OfType<JObject>().Where(x => (string)x["source_kind"] == "published_policy").ToList();
        Test.True(policies.Count <= 3 && policies.Count > 0 && policies.All(x => (string)x["key"] != "policy:revision-0"), "ordinary material budget caps policies and excludes already adopted revision");
        Test.True(policies.All(x => ((string)x["text"]).Contains("only if grain arrives")), "full relevant condition survives freezing");
        string snapshot = job.EventMaterialSnapshot;
        o.PrepareSharedRequest(job);
        Test.True(h.Reads == 1 && job.EventMaterialSnapshot == snapshot, "retry reuses evidence without policy reread");
        Test.True(o.CurrentStorage.ProcessedPolicySignalKeys.SequenceEqual(new[] { "revision-0" }), "selection alone never marks policy adopted");
        Test.True(!h.Calls.Contains("ForeignPolicySignals"), "old full policy signal reader remains unused");
        var round = o.CurrentStorage.ActiveRound;
        string adopted = (string)policies[0]["key"];
        round.EventMaterialKeys = new() { adopted };
        var notice = new DiplomacyEventAnnouncement { Id = "publication", EventId = round.RoundId, Stage = "started", Title = "Policy event", Body = "Original conditions retained", State = "pending" };
        var publish = typeof(WorldDiplomacyOrchestration).GetMethod("PublishStagedEventBoundary", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        object[] arguments = { round, notice, null };
        Test.True((bool)publish.Invoke(o, arguments), "policy material event can formally publish through the actual owner");
        Test.True(o.CurrentStorage.ProcessedPolicySignalKeys.Count == 2
            && o.CurrentStorage.ProcessedPolicySignalKeys.Contains(adopted.Substring("policy:".Length)), "publication marks only the actually adopted policy, leaving other candidates reusable");
    }

    private static DiplomacyEventAnnouncement Notice(int i) => new() { Id = "notice:" + i, EventId = "event:" + i,
        State = "public", Day = i, Title = "Archive", Body = "archive context " + i };
    private static void Index()
    {
        var s = new WorldDiplomacyStorage();
        for (int i = 0; i < 10000; i++) s.EventAnnouncements.Add(Notice(i));
        var view = new WorldDiplomacyMemorySnapshot(s, "a", new());
        string Read(string query) => WorldDiplomacyMemoryApplication.Build(view, query, d => d.ToString());
        var timer = Stopwatch.StartNew(); Read("archive"); timer.Stop();
        var cold = WorldDiplomacyMemoryApplication.WorkCounts(s);
        s.EventAnnouncements.Add(new() { Id = "fresh", EventId = "new", State = "public", Title = "UniqueSaltAccord", Body = "Preserve the exact tariff condition" });
        var update = Stopwatch.StartNew(); var found = Read("UniqueSaltAccord"); update.Stop();
        var after = WorldDiplomacyMemoryApplication.WorkCounts(s);
        Test.True(found.Contains("exact tariff condition") && after.Tokenized == cold.Tokenized + 1 && after.Boundaries == cold.Boundaries + 1,
            "one new announcement in ten thousand only indexes one record");
        for (int i = 0; i < 4; i++) { WorldDiplomacyMemoryApplication.InvalidateRecords(s); Read("UniqueSaltAccord"); }
        Test.True(WorldDiplomacyMemoryApplication.WorkCounts(s) == after, "ordinary maintenance never retokenizes unchanged archive");
        var changed = s.EventAnnouncements.Last(); changed.Body = "Updated precise clause";
        WorldDiplomacyMemoryApplication.InvalidateBoundary(s, changed);
        Test.True(Read("UniqueSaltAccord").Contains("Updated precise clause"), "same-count content update replaces the actual result");
        changed.State = "pending"; WorldDiplomacyMemoryApplication.InvalidateBoundary(s, changed);
        Test.True(!Read("UniqueSaltAccord").Contains("Updated precise clause"), "revoked publication is removed from search");
        changed.State = "public"; WorldDiplomacyMemoryApplication.InvalidateBoundary(s, changed); Read("UniqueSaltAccord");
        s.EventAnnouncements.Remove(changed); Read("UniqueSaltAccord");
        Test.True(!Read("UniqueSaltAccord").Contains("Updated precise clause"), "archive removal reconciles stale rows");
        s.Documents.Add(new() { DocumentId = "private", Title = "PrivateSaltRecord", Body = "Secret terms", IsReadyForPublication = true });
        Test.True(!Read("PrivateSaltRecord").Contains("Secret terms"), "incremental docs preserve knowledge boundary");
        var known = new WorldDiplomacyMemorySnapshot(s, "a", new() { "private" });
        string Known() => WorldDiplomacyMemoryApplication.Build(known, "PrivateSaltRecord", d => d.ToString());
        Test.True(Known().Contains("Secret terms"), "new knowledge exposes the same indexed source without rebuilding");
        s.Documents[0].IsCompressed = true; WorldDiplomacyMemoryApplication.InvalidateRecords(s);
        Test.True(!Known().Contains("Secret terms"), "compression removes retired document row");
        var fresh = new WorldDiplomacyStorage { EventAnnouncements = s.EventAnnouncements, Documents = s.Documents };
        Test.True(Read("archive") == WorldDiplomacyMemoryApplication.Build(new(fresh, "a", new()), "archive", d => d.ToString()),
            "incremental and rebuilt index have stable ranking and exact rendering");
        var backlog = new WorldDiplomacyStorage();
        for (int i = 0; i < 100; i++) backlog.EventAnnouncements.Add(Notice(i));
        bool pending = false;
        try { WorldDiplomacyMemoryApplication.Build(new(backlog, "a", new()), "archive", d => d.ToString(), deferInitialIndex: true); }
        catch (WorldDiplomacyPreparationPendingException) { pending = true; }
        Test.True(pending && WorldDiplomacyMemoryApplication.WorkCounts(backlog).Boundaries <= 32, "cold request yields a bounded initial batch instead of pretending history is empty");
        int attempts = 0; string complete = "";
        while (attempts++ < 110)
        {
            try { complete = WorldDiplomacyMemoryApplication.Build(new(backlog, "a", new()), "archive", d => d.ToString(), deferInitialIndex: true); break; }
            catch (WorldDiplomacyPreparationPendingException) { }
        }
        Test.True(complete.Contains("archive context") && WorldDiplomacyMemoryApplication.WorkCounts(backlog).Boundaries == 100,
            "resumable first build publishes complete usable history");
        Console.WriteLine($"Index 10000 records coldMs={timer.Elapsed.TotalMilliseconds:F2} appendMs={update.Elapsed.TotalMilliseconds:F2} appendTokenized=1 appendVisited=1");
    }
}
