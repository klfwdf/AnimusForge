using System.Reflection;
using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class ScopedWorldReplay
{
    private sealed class Host : EventStructureReplay.Host, IDiplomacyScopedWorldHost
    {
        internal int Reads, PairReads;
        internal readonly List<int> Scopes = new();
        public override IReadOnlyList<string> AllKingdomIds() => new[] { "a", "b", "c", "p" }
            .Concat(Enumerable.Range(0, 52).Select(x => "realm_" + x)).ToArray();
        public override int EstimateTokens(string text)
        {
            int total = 0, run = 0;
            foreach (char c in text ?? "")
                if (c <= 127 && char.IsLetterOrDigit(c)) run++;
                else { total += (run + 3) / 4; run = 0; if (!char.IsWhiteSpace(c)) total++; }
            return total + (run + 3) / 4;
        }
        public string CaptureScopedEventWorld(IReadOnlyCollection<string> roots, IReadOnlyCollection<string> all, bool discover)
        {
            Reads++;
            var scope = discover ? DiplomacyWorldQuery.Select(roots, all,
                (a, b) => b == "realm_51", (a, b) => { PairReads++; return b == "realm_50" ? 80 : 0; }) : roots.ToList();
            Scopes.Add(scope.Count);
            return CaptureEventWorld(scope);
        }
        public override string CaptureEventWorld(IReadOnlyCollection<string> ids) => JsonConvert.SerializeObject(ids.Select(id => new {
            kingdom_id = id, ruler_id = "ruler_" + id,
            ruler_relations = ids.Where(x => x != id).Select(x => new { kingdom = x, value = Relation, minimum = -100, maximum = 100 }),
            towns = Array.Empty<object>() }));
    }
    private static object Call(WorldDiplomacyOrchestration o, string name, params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(o, args)!;
    internal static void Run()
    {
        var h = new Host { PublishEnabled = true, CurrentDayValue = 40 };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        o.TryScheduleNormalRound(); var job = o.CurrentStorage.Jobs.Single(x => x.Kind == "event_concept");
        o.PrepareSharedRequest(job);
        var input = JObject.Parse(job.UserPrompt);
        var sources = (JArray)input["sources"]!;
        var world = sources.OfType<JObject>().Single(x => (string)x["key"]! == "world:current");
        var rows = JArray.Parse((string)world["text"]!);
        Test.True(rows.Count <= 12 && rows.Any(x => (string)x["kingdom_id"]! == "realm_51"), "query discovers third-party enemy without full matrix");
        Test.True(h.PairReads <= 6 * 56, "bounded seed-to-world discovery, not all-pairs scan");
        Test.True(sources.Any(x => (string)x["source_kind"]! == "world_book_account"), "relation snapshot leaves room for retrieved lore");
        int tokens = h.EstimateTokens(job.SystemPrompt) + h.EstimateTokens(job.UserPrompt);
        Test.True(tokens < 28000 && !job.RequestPreparationPending && string.IsNullOrEmpty(job.SuspendedReason), "56-realm actual preparation is sendable");
        int reads = h.Reads; string frozen = job.EventMaterialSnapshot;
        h.Relation = 42; o.PrepareSharedRequest(job);
        Test.True(reads == h.Reads && frozen == job.EventMaterialSnapshot, "same-stage retries reuse consistent evidence");
        // Persist/reload retains the query marker and never rebuilds the same stage.
        o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
        job = o.CurrentStorage.Jobs.Single(x => x.Kind == "event_concept"); o.PrepareSharedRequest(job);
        Test.True(h.Reads == reads, "reload preserves scoped snapshot");
        // Old unpublished snapshots are replaced through the real preparation owner.
        var legacy = JArray.Parse(job.EventMaterialSnapshot);
        var oldWorld = legacy.OfType<JObject>().Single(x => (string)x["key"]! == "world:current");
        oldWorld.Remove("query_version"); oldWorld.Remove("query_roots");
        oldWorld["text"] = h.CaptureEventWorld(h.AllKingdomIds());
        int oldTokens = h.EstimateTokens(legacy.ToString(Formatting.None));
        job.EventMaterialSnapshot = legacy.ToString(Formatting.None); o.PrepareSharedRequest(job);
        Test.True(h.Reads == reads + 1 && h.EstimateTokens(job.UserPrompt) < 28000, "legacy all-world snapshot upgrades before send");
        // Detailed phase binds the exact selected countries, including a previously unqueried third realm.
        var round = o.ResolveRound(job.RoundId);
        var admission = DiplomacyCountryAdmission.Calculate(new DiplomacyAdmissionInput { Direct = new List<string> { "a", "b", "realm_49" }, Candidates = new List<string> { "a", "b", "realm_49" } }, 1, "scoped-test");
        job.CountryAdmission = JsonConvert.SerializeObject(admission);
        job.EventSelectedCandidate = "{\"kingdoms\":[\"a\",\"b\",\"realm_49\"]}";
        var detail = (JArray)Call(o, "RefreshPlanningWorld", job, round, job.CandidateKingdomIds,
            JArray.Parse(job.EventMaterialSnapshot), true);
        var detailWorld = detail.OfType<JObject>().Single(x => (string)x["key"]! == "world:current");
        var detailRows = JArray.Parse((string)detailWorld["text"]!);
        Test.True(detailRows.Count == 3 && detailRows.Any(x => (string)x["kingdom_id"]! == "realm_49"), "selected third realm gets complete fresh details");
        Test.True(detailRows.SelectMany(x => (JArray)x["ruler_relations"]!).All(x => (float)x["value"]! == 42), "next stage reads latest game relation values");
        reads = h.Reads; Call(o, "RefreshPlanningWorld", job, round, job.CandidateKingdomIds, detail, true);
        Test.True(reads == h.Reads, "writer and auditor share detailed evidence");
        var peripheral = new JObject { ["sources"] = JArray.Parse(frozen) };
        Test.True((bool)Call(o, "TrimPeripheralWorld", peripheral), "oversized preparation can shed peripheral relations before lore");
        var trimmed = ((JArray)peripheral["sources"]!).OfType<JObject>().Single(x => (string)x["key"]! == "world:current");
        Test.True(JArray.Parse((string)trimmed["text"]!).Count == ((JArray)trimmed["query_roots"]!).Count
            && ((JArray)peripheral["sources"]!).Count == sources.Count, "trim keeps all core realms and all retrieved evidence");
        job.CountryAdmission = ""; job.EventSelectedCandidate = "";
        Call(o, "HoldTechnicalRequest", job, "event_required_evidence_budget_exceeded:retain_prior_cases_and_selected_sources");
        job.RetryAfterUtcTicks = 0; job.NextRecoveryProbeUtcTicks = 0;
        Call(o, "TryResumeTechnicalRequest", job, false);
        Test.True(job.MaterialSearchPass == 0 && job.EventMaterialSnapshot.Length > 0 && h.Reads == reads
            && job.SuspendedReason.StartsWith("event_required_evidence_budget_exceeded:", StringComparison.Ordinal),
            "identical oversized scoped input remains held with evidence intact and no paid retry");
        Console.WriteLine("Scoped world: 56 realms; legacy-source-estimate=" + oldTokens + "; scoped-total-estimate=" + tokens + "; max-scope=" + h.Scopes.Max());
    }
}
