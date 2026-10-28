using System.Reflection;
using System.Threading.Tasks;
using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class PlayerMaterialOptimizationReplay
{
    private sealed class Host : EventStructureReplay.Host, IDiplomacyEventMaterialHost, IDiplomacyMaterialVersionHost
    {
        internal readonly List<(string[] Countries, string Topic)> Completed = new();
        internal readonly TaskCompletionSource<bool> Ready = new();
        internal string Version = "v1";
        internal bool NoLore;
        internal bool OnlyExtraLore;
        public string CaptureMaterialVersion(string id) => Version;
        public new string CaptureEventSources(IReadOnlyCollection<string> ids, IReadOnlyCollection<string> excluded, int pass = 0, string topic = "")
        {
            if (!Ready.Task.IsCompleted) return null!;
            Completed.Add((ids.ToArray(), topic));
            var rows = new JArray();
            foreach (string id in ids)
            {
                rows.Add(new JObject { ["key"] = "persona:" + id, ["known_by"] = id,
                    ["source_kind"] = "character_setting", ["text"] = "Current persona " + id + " day=" + CurrentDayValue,
                    ["hero_ids"] = new JArray("ruler_" + id) });
                if (!NoLore && (!OnlyExtraLore || id is "b" or "c"))
                    rows.Add(new JObject { ["key"] = "lore:" + id, ["known_by"] = id,
                        ["source_kind"] = "world_book_account", ["text"] = "The court disputes an old succession oath. " + id,
                        ["hero_ids"] = new JArray("ruler_" + id) });
            }
            return rows.ToString(Formatting.None);
        }
    }
    private static object Call(WorldDiplomacyOrchestration o, string name, params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(o, args)!;

    private static (Host, WorldDiplomacyOrchestration, WorldDiplomacyJob) Start(bool mentions = false)
    {
        var h = new Host { PublishEnabled = true, PlayerAwareExecution = true, CurrentDayValue = 40 };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        var root = new WorldDiplomacyDocument { DocumentId = "player-source", RoundId = "player-case",
            AuthorKingdomId = "p", TargetKingdomId = "a", IsPlayerAuthored = true, Body = "Our imperial claim requires your answer.",
            AddressedKingdomIds = new() { "a", "b", "c" }, MentionedKingdomIds = mentions ? new() { "b" } : new() };
        o.CurrentStorage.Documents.Add(root);
        o.CurrentStorage.ConcurrentRounds.Add(new WorldDiplomacyRound { RoundId = root.RoundId, RootDocumentId = root.DocumentId,
            State = "active", EventPhase = "forming", DialogueArrangementId = "player_manual_declaration",
            InitiatorKingdomId = "p", Participants = new() { new() { KingdomId = "p" }, new() { KingdomId = "a" } } });
        var j = new WorldDiplomacyJob { JobId = "player-plan", Kind = "event_concept", RoundId = root.RoundId,
            DocumentId = root.DocumentId, AuthorKingdomId = "p", CandidateKingdomIds = new() { "p", "a", "b", "c" },
            EventSelectionStage = "player", EventSelectedCandidate = "{}", EventComparison = "{}",
            EventTriggerMaterial = JsonConvert.SerializeObject(new { root.Body, root.Title, root.Actions }),
            EventInvestigationKingdoms = new() { "a", "b", "c" }, CreatedDay = 40 };
        o.CurrentStorage.Jobs.Add(j);
        return (h, o, j);
    }
    internal static void Run()
    {
        foreach (bool mentions in new[] { false, true })
        {
            var (h, o, j) = Start(mentions);
            for (int day = 40; day < 43; day++)
            {
                h.CurrentDayValue = day; o.PrepareSharedRequest(j);
                Test.True(j.RequestPreparationPending && h.Completed.Count == 0 && o.CurrentStorage.EventAnnouncements.Count == 0,
                    "pending retrieval across accelerated days neither publishes nor fabricates partial material");
            }
            h.Ready.SetResult(true); o.PrepareSharedRequest(j);
            Test.True(h.Completed.Single().Countries.OrderBy(x => x).SequenceEqual((mentions ? new[] { "a", "b", "p" } : new[] { "a", "p" })),
                "initial player retrieval includes direct and explicitly mentioned countries, not all broadcast recipients");
            var request = JObject.Parse(j.UserPrompt);
            Test.True((string)request["phase"]! == "identify" && ((JArray)request["candidates"]!).Count == 4,
                "all countries remain eligible for independent seed review and weighted selection");
            string original = o.ResolveDocument(j.DocumentId).Body;
            CountryAdmissionReplay.FreezeFixture(o, j, new[] { "p", "a", "c" });
            Test.True(h.Completed.All(x => x.Topic == original), "initial and supplement use the exact original declaration, not its JSON envelope");
            Test.True(h.Completed.Count(x => x.Countries.Contains("p")) == 1 && h.Completed.Count(x => x.Countries.Contains("a")) == 1,
                "same-day direct dossiers are reused instead of retrieved twice");
            Test.True(h.Completed.Any(x => x.Countries.Contains("c")) && j.AdmissionSupplementedCountries.Contains("c"),
                "extra selected country receives full sources without changing frozen membership");
            Test.True(j.SystemPrompt.Contains(DiplomacyPromptTexts.Get("PlayerOpeningPlan", "")) && o.CurrentStorage.EventAnnouncements.Count == 0,
                "real package writer contract remains connected and publication still waits for independent review");

            int before = h.Completed.Count; h.CurrentDayValue++;
            string Capture() => (string)Call(o, "CapturePlanningSources", j, new[] { "p" }, Array.Empty<string>(), original);
            Test.True(Capture().Contains("day=" + h.CurrentDayValue) && h.Completed.Count == before + 1,
                "day rollover rereads live facts; rendered prior-day persona is not reused");
            before = h.Completed.Count; Capture();
            Test.True(h.Completed.Count == before, "fresh same-day snapshot is reused");
            h.Version = "v2"; Capture();
            Test.True(h.Completed.Count == before + 1, "changed source/identity version forces retrieval");
            j = JsonConvert.DeserializeObject<WorldDiplomacyJob>(JsonConvert.SerializeObject(j))!;
            before = h.Completed.Count; Capture();
            Test.True(h.Completed.Count == before + 1, "a deserialized job cannot reuse the old runtime object's cache");
        }
        foreach (bool missingEverywhere in new[] { false, true })
        {
            var (h, o, j) = Start(); h.OnlyExtraLore = true; h.NoLore = missingEverywhere; h.Ready.SetResult(true);
            o.PrepareSharedRequest(j);
            Test.True(j.SuspendedReason == "" && (string)JObject.Parse(j.UserPrompt)["phase"]! == "identify",
                "empty initial lore does not block source-bound identification before selected countries are supplemented");
            CountryAdmissionReplay.FreezeFixture(o, j, new[] { "p", "a", "c" }, prepareConcept: false);
            for (int i = 0; i < 8 && j.AdmissionSupplementedCountries.Count < 3; i++) o.PrepareSharedRequest(j);
            Test.True(missingEverywhere ? j.SuspendedReason == "event_required_world_book_missing" : j.SuspendedReason == "",
                "all selected sources are checked; genuinely missing world-book evidence still blocks writing");
            Test.True(o.CurrentStorage.EventAnnouncements.Count == 0, "material optimization never bypasses opening audit");
        }
        Console.WriteLine("PASS player material optimization: scoped input, completed-only reuse, delayed cross-day reads, version/reload invalidation, extra-country lore and real CN/EN contracts; live timing NOT_RUN.");
    }
}
