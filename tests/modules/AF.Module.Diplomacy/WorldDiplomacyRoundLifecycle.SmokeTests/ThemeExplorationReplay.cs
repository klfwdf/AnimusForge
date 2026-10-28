using System.Reflection;
using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Controlled material/provider fixtures verify production wiring, not model creativity.
internal static partial class MaterialSupplyReplay
{
    private sealed class ThemeHost : EventStructureReplay.Host, IDiplomacyEventMaterialHost, IDiplomacyMaterialVersionHost
    {
        internal readonly List<string> Queries = new();
        internal bool DeferDeep = true;
        internal string Version = "v1";
        public string CaptureMaterialVersion(string country) => Version;
        public new string CaptureEventSources(IReadOnlyCollection<string> countries, IReadOnlyCollection<string> excluded, int searchPass = 0, string topic = "")
        {
            Queries.Add(topic);
            if (topic.Length > 0 && DeferDeep) return null!;
            return new JArray(countries.Select(country => new JObject { ["key"] = "persona:" + country, ["known_by"] = country,
                ["source_kind"] = "character_setting", ["text"] = "Controlled court setting " + country })
                .Concat(new[] { new JObject { ["key"] = topic.Length == 0 ? "lore:navigation" : "lore:targeted",
                ["known_by"] = "b", ["source_kind"] = "world_book_account", ["topic_query"] = topic.Length > 0,
                ["text"] = topic.Length == 0 ? "A family account mentions a disputed obligation."
                    : "The obligation applies ONLY if the exile returns alive. One witness alleges betrayal; the family denies it. No court has established guilt. " + topic } })).ToString();
        }
    }

    internal static void ThemeExploration()
    {
        var h = new ThemeHost { PublishEnabled = true, CurrentDayValue = 40 };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        o.TryScheduleNormalRound(); var j = o.CurrentStorage.Jobs.Single(x => x.Kind == "event_concept");
        o.PrepareSharedRequest(j);
        Test.True(j.RequestPreparationPending && h.Queries.Count == 2 && h.Queries[0] == "" && h.Queries[1].Length > 0,
            "actual preparation probes before themed retrieval and waits without a model request");
        string theme = j.EventExplorationTheme, query = h.Queries[1];
        Test.True(WorldDiplomacyOrchestration.EventThemeIds.Contains(theme) && j.EventProbeSnapshot.Contains("navigation"), "pending direction and probe are durable");
        o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
        j = o.CurrentStorage.Jobs.Single(x => x.JobId == j.JobId); h.DeferDeep = false;
        o.PrepareSharedRequest(j);
        Test.True(h.Queries.Count == 3 && h.Queries[2] == query && j.EventExplorationTheme == theme,
            "reload reuses probe and direction rather than redoing initial research or drawing again");
        var request = JObject.Parse(j.UserPrompt);
        Test.True((string)request["exploration_theme"]! == theme && ((JArray)request["theme_catalog"]!).Count == 10,
            "actual writer gets stable direction and all equal exploration meanings");
        Test.True(j.EventMaterialSnapshot.Contains("ONLY if the exile returns alive") && j.EventMaterialSnapshot.Contains("family denies")
            && j.EventMaterialSnapshot.Contains("lore:navigation"), "original conditions, conflicting claims and unrestricted probe survive final request");
        string sources = j.EventMaterialSnapshot; o.PrepareSharedRequest(j);
        Test.True(h.Queries.Count == 3 && j.EventMaterialSnapshot == sources, "writer retry uses frozen sources without retrieval");
        var expansionHost = new ThemeHost { PublishEnabled = true, CurrentDayValue = 40, DeferDeep = false };
        var expansion = new WorldDiplomacyOrchestration(expansionHost,new WorldDiplomacyRuntimeState()); expansionHost.Owner=expansion;
        expansion.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
        var work=expansion.CurrentStorage.Jobs.Single(x=>x.JobId==j.JobId);
        var seed=Candidate(); seed["material_keys"]=new JArray("lore:targeted"); seed["theme"]="kinship";
        seed["theme_reason"]="A conditional family obligation motivates the demand despite disputed accusations";
        Complete(expansion,work,new(){["candidates"]=new JArray(seed)}); Complete(expansion,work,Compare());
        CountryAdmissionReplay.FreezeFixture(expansion, work, new[] { "b", "a" });
        expansion.PrepareSharedRequest(work);
        Test.True(expansionHost.Queries.Last()==DiplomacyPromptTexts.Get("EventThemeQuery_kinship",""),
            "cross-theme winner drives actual supplemental retrieval instead of sampled exploration direction");
        var draft=new JObject { ["title"]="The conditional obligation", ["background"]="The family seeks clarification of an obligation conditional on a living return; rival accusations remain disputed.",
            ["initiator"]="b",["material_keys"]=new JArray("lore:targeted"),["participants"]=new JArray(Position("b","a"),Position("a","b")),
            ["party_review"]=new JArray(work.CandidateKingdomIds.Select(id=>new JObject{["kingdom"]=id,["included"]=id is "a" or "b",["reason"]="Controlled standing judgement"})),
            ["narrative"]=new JArray(new JObject{["kind"]="allegation",["subject"]="b",["source"]="lore:targeted",["text"]="One witness alleges betrayal; the family denies it."}) };
        foreach(var party in ((JArray)draft["participants"]!).OfType<JObject>())party["source_keys"]=new JArray("lore:targeted");
        Test.True(work.UserPrompt.Contains("ONLY if the exile returns alive") && work.UserPrompt.Contains("No court has established guilt"),
            "expansion receives full limiting conditions and uncertain status, not navigation alone");
        Complete(expansion,work,draft); expansion.PrepareSharedRequest(work);
        Test.True(work.Kind=="event_concept_audit" && work.UserPrompt.Contains("ONLY if the exile returns alive")
            && work.UserPrompt.Contains("family denies"), "independent audit receives the same raw contradictions and conditions");
        var rejected=MaterialCountryThirdReplay.Approved(work); rejected["theme_review"]!["valid"]=false;
        Complete(expansion,work,rejected);
        Test.True(expansion.CurrentStorage.EventAnnouncements.Count==0 && work.MaterialSearchPass==1 && work.SuspendedReason==""
            && expansion.ResolveRound(work.RoundId).EventSelectionEvidence.Contains("theme_motive_mismatch"),
            "semantic theme mismatch rejects the seed and searches within the finite budget instead of rewriting it");
        Complete(o,j,new() { ["candidates"] = new JArray(), ["reason"] = "No supported current negotiation after inspection" });
        Test.True(j.MaterialSearchPass == 1 && j.EventExplorationTheme == theme, "first empty inspected batch deepens same theme");
        Complete(o,j,new() { ["candidates"] = new JArray(), ["reason"] = "Follow-up also lacks an answerable issue" });
        Test.True(j.MaterialSearchPass == 2 && j.EventThemePass == 1 && j.EventExplorationTheme == "" && j.MaterialNextSearchDay == 0,
            "second inspected batch permits a new direction immediately within the three-batch budget");
        Test.True(o.CurrentStorage.EventAnnouncements.Count == 0, "empty exploration does not count adoption");

        // A source revision during pending deep retrieval discards obsolete probe
        // evidence while preserving the already selected exploration direction.
        h.CurrentDayValue++; h.DeferDeep = true; o.PrepareSharedRequest(j); theme = j.EventExplorationTheme;
        h.Version = "v2"; h.DeferDeep = false; o.PrepareSharedRequest(j);
        Test.True(j.EventProbeSnapshot == "" && !j.RequestPreparationPending && j.EventExplorationTheme == theme
            && !j.EventMaterialSnapshot.Contains("lore:navigation"),
            "source revision discards obsolete probe but retains independently validated complete deep capture without another retrieval cycle");
        int completedQueries = h.Queries.Count;
        o.PrepareSharedRequest(j);
        Test.True(j.EventProbeVersion == "" && j.EventProbeSnapshot == "" && j.EventMaterialSnapshot.Contains("lore:targeted")
            && !j.RequestPreparationPending && h.Queries.Count == completedQueries,
            "fresh deep evidence freezes immediately and a request retry does not recapture it");
        var continueSearch=typeof(WorldDiplomacyOrchestration).GetMethod("ContinueMaterialSearch",BindingFlags.Instance|BindingFlags.NonPublic)!;
        for(int n=0;n<3;n++)continueSearch.Invoke(o,new object[]{j,o.ResolveRound(j.RoundId),true,false});
        Test.True(j.EventExplorationTheme == theme && j.EventThemePass == 1 && j.EventThemeInvestigations == 0,
            "technical exhaustion broadens materials without counting an investigation or rerolling theme");

        var choose = typeof(WorldDiplomacyOrchestration).GetMethod("ChooseExplorationTheme", BindingFlags.Instance|BindingFlags.NonPublic)!;
        var adjacentPasses = Enumerable.Range(0, 64).Select(pass => (string)choose.Invoke(o, new object[] {
            new WorldDiplomacyJob { JobId = "event_concept:d61dae434d8443dca226ff7f4199b093", EventThemePass = pass } })!).ToList();
        Test.True(adjacentPasses.Distinct().Count() >= 8 && adjacentPasses.Take(6).Distinct().Count() > 1,
            "recorded incident job explores genuinely different directions across permitted passes");
        string Draw(int id) => (string)choose.Invoke(o,new object[] { new WorldDiplomacyJob { JobId = "distribution-check-" + id } })!;
        var baseline = Enumerable.Range(0,4000).Select(Draw).ToArray();
        Test.True(WorldDiplomacyOrchestration.EventThemeIds.All(t => baseline.Count(x => x == t) is > 280 and < 520),
            "equal base opportunity covers all ten themes across stable independent identities");
        o.CurrentStorage.EventAnnouncements.Add(new() { Id="pending", EventId="pending", Theme="honor", State="pending", Day=40 });
        Test.True(Enumerable.Range(0,4000).Select(Draw).SequenceEqual(baseline), "unpublished staged opening has zero influence on exploration");
        o.CurrentStorage.EventAnnouncements.Clear();
        for(int n=0;n<6;n++)o.CurrentStorage.EventAnnouncements.Add(new(){Id="public"+n,EventId="public"+n,Theme="honor",State="public",Day=40});
        var cooled = Enumerable.Range(0,4000).Select(Draw).ToArray();
        Test.True(cooled.Count(x => x == "honor") < baseline.Count(x => x == "honor")/4 && cooled.Contains("honor"),
            "recent official publications suppress repetition without a forbidden theme or hard quota");
        var duplicate = JsonConvert.DeserializeObject<DiplomacyEventAnnouncement>(JsonConvert.SerializeObject(o.CurrentStorage.EventAnnouncements.Last()))!;
        o.CurrentStorage.EventAnnouncements.Add(duplicate);
        Test.True(Enumerable.Range(0,4000).Select(Draw).SequenceEqual(cooled), "same publication receipt cannot count twice");
        var guard=typeof(WorldDiplomacyOrchestration).GetMethod("ValidateUnansweredPlayerNarration",BindingFlags.Instance|BindingFlags.NonPublic)!;
        foreach(string text in new[]{"已有依据：原文", "theme: honor", "A source is lore:0123456789abcdef0123456789abcdef."})
        {
            object[] args={o.ResolveRound(j.RoundId),new JObject{["body"]=text},""};
            Test.True(!(bool)guard.Invoke(o,args)!, "internal diagnostics rejected before public narrative: "+text);
        }
        object[] natural={o.ResolveRound(j.RoundId),new JObject{["body"]="One court alleges betrayal; the family denies it."},""};
        Test.True((bool)guard.Invoke(o,natural)!, "natural attribution remains valid public prose");
        var (_,missingOwner,missingJob)=Start();
        var missing=Candidate();missing.Remove("theme");
        Complete(missingOwner,missingJob,new(){["candidates"]=new JArray(missing)});
        Test.True(missingJob.SuspendedReason.Contains("theme_and_substantive"),"missing substantive classification cannot silently record sampled theme as adopted");
        SingleSeedToPublication();
        CaptureReuseAndLatency();
        Console.WriteLine("PASS theme exploration: real preparation, persistence, source versions, weights, publication and public-prose guards; generated semantic diversity NOT_RUN");
    }

    private static void CaptureReuseAndLatency()
    {
        var themes = new[] { "history", "territory", "trade" };
        Test.True(DiplomacyMaterialCaptureRules.Queries("", false, themes, 0).SequenceEqual(new[] { "" }),
            "navigation costs one identity query per country rather than four full research queries");
        Test.True(DiplomacyMaterialCaptureRules.Queries("family", true, themes, 0).SequenceEqual(new[] { "family", "history", "" }),
            "deep exploration retains theme, unrestricted generic and identity lanes");
        Test.True(DiplomacyMaterialCaptureRules.Queries("player trigger", false, themes, 0).Count == 5,
            "specific player/oral trigger retains broad material coverage");
        var h = new ThemeHost { PublishEnabled = true, CurrentDayValue = 40, DeferDeep = false };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        o.TryScheduleNormalRound(); var job = o.CurrentStorage.Jobs.Single(x => x.Kind == "event_concept");
        var capture = typeof(WorldDiplomacyOrchestration).GetMethod("CapturePlanningSources", BindingFlags.Instance | BindingFlags.NonPublic)!;
        string Read(string[] countries, string topic, string[]? excluded = null) => (string)capture.Invoke(o,
            new object[] { job, countries, excluded ?? Array.Empty<string>(), topic })!;
        Read(new[] { "a", "b" }, "family"); int queries = h.Queries.Count;
        string subset = Read(new[] { "b" }, "family", new[] { "lore:targeted" });
        Test.True(h.Queries.Count == queries && subset.Contains("ONLY if the exile returns alive")
            && JArray.Parse(subset).OfType<JObject>().Single(x => (string)x["key"]! == "lore:targeted")["previously_used"]!.Value<bool>(),
            "same-day supplement reuses complete exact-topic country sources and updates dedup metadata");
        Read(new[] { "b" }, "honor");
        Test.True(h.Queries.Count == ++queries, "a different winning theme requires actual supplemental retrieval");
        h.Version = "v2"; Read(new[] { "b" }, "family");
        Test.True(h.Queries.Count == ++queries, "source/identity revision invalidates completed capture reuse");
        h.CurrentDayValue++; Read(new[] { "b" }, "family");
        Test.True(h.Queries.Count == ++queries, "next-day relationships/personas are recaptured rather than reused indefinitely");
        job.MaterialSearchPass++; Read(new[] { "b" }, "family");
        Test.True(h.Queries.Count == ++queries, "new investigation pass cannot reuse previous sampled lord batch");
        var restored = JsonConvert.DeserializeObject<WorldDiplomacyJob>(JsonConvert.SerializeObject(job))!;
        capture.Invoke(o, new object[] { restored, new[] { "b" }, Array.Empty<string>(), "family" });
        Test.True(h.Queries.Count == ++queries, "save/load never resurrects runtime capture cache");
    }
}
