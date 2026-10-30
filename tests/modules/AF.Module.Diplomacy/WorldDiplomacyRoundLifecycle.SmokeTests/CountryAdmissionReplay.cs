using System.Reflection;
using AnimusForge;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Contracts;
using AnimusForge.DiplomacyDialogue;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Model semantics and game reads below are controlled leaves. This suite drives
// the actual new preparation/completion/adoption path and does not claim live QA.
internal static class CountryAdmissionReplay
{
    internal sealed class Host : EventStructureReplay.Host, IDiplomacyEventMaterialHost, IWorldDiplomacyAnalysisPort, IDiplomacyAdmissionIdentityHost
    {
        internal string IdentityMode = "", ChangedHero = "lord_7_3";
        internal int IdentityReads;
        public DiplomacyAdmissionIdentity ReadAdmissionIdentity(string kind, string id, string expectedCountry)
        {
            IdentityReads++;
            if (IdentityMode == "exception") throw new InvalidOperationException("controlled_identity_read_failure");
            if (kind == "hero" && id == ChangedHero)
                return DiplomacyAdmissionIdentity.Hero(IdentityMode != "missing", IdentityMode == "dead", true,
                    IdentityMode == "empty_owner" ? null : IdentityMode == "moved" ? "b" : "a", expectedCountry);
            if (kind == "kingdom") return DiplomacyAdmissionIdentity.Kingdom(IdentityMode != "kingdom_missing", IdentityMode == "eliminated" && id == "a", id);
            return default;
        }
        public override IWorldDiplomacyAnalysisPort AnalysisPort()=>this;
        public WorldDiplomacyStorage Storage=>Owner.CurrentStorage;
        public IWorldDiplomacyDocumentExecutionPort Execution=>DocumentExecution();
        public int MaxAutomaticReplyDepth=>3;
        public string KingdomName(string id)=>id;
        internal int Captures;
        internal int CountPreference = 1;
        public override int AdmissionCountryCountPreference => CountPreference;
        public override string AdmissionSubjectCountry(string kind, string id) => kind == "hero" && id == "lord_7_3"
            ? "a" : kind == "hero" && id == "lord_1_1" ? "empire"
            : kind == "hero" && id == "lord_1_14" ? "empire_s"
            : kind == "settlement" && id == "fixture_town_s" ? "empire_s"
            : base.AdmissionSubjectCountry(kind, id);
        internal bool Extra = true, ThrowRead;
        internal bool FourthNpc;
        public override IReadOnlyList<string> AllKingdomIds()=>FourthNpc?new[]{"a","b","c","d","p"}:base.AllKingdomIds();
        public new string CaptureEventSources(IReadOnlyCollection<string> ids, IReadOnlyCollection<string> excluded, int pass = 0, string topic = "")
            => new JArray(ids.Select(x => new JObject { ["key"] = "lore:" + x, ["known_by"] = x,
                ["source_kind"] = "world_book_account", ["text"] = "Court " + x + " records a contested succession oath and recognition claim.",
                ["hero_ids"] = new JArray("ruler_" + x) })).ToString();
        public override DiplomacyAdmissionInput CaptureCountryAdmission(IReadOnlyCollection<string> direct, IReadOnlyCollection<string> heroes, IReadOnlyCollection<string> candidates)
        {
            Captures++;
            if (ThrowRead) throw new InvalidOperationException("controlled_read_failure");
            var input = base.CaptureCountryAdmission(direct, heroes, candidates);
            input.Candidates = candidates.ToList();
            if (Extra)
                foreach (string country in candidates.Where(x => !direct.Contains(x) && x != "p"))
                    input.Pairs.Add(new() { Country = country, Origin = direct.First(), Ruler = "ruler_" + country,
                        OriginRuler = "ruler_" + direct.First(), Border = true, War = true,
                        Relations = new() { ["ruler_" + direct.First()] = -65 } });
            return input;
        }
    }

    private static object Call(WorldDiplomacyOrchestration o, string name, params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(o, args)!;

    private static void Complete(WorldDiplomacyOrchestration o, WorldDiplomacyJob j, JObject value)
        => Test.True(o.TryCompleteDiplomacyPipeline(j, new() { Success = true, Content = value.ToString() }), "admission production completion");

    private static void Prepare(WorldDiplomacyOrchestration o, WorldDiplomacyJob j)
    {
        for (int i = 0; i < 20; i++)
        {
            j.RetryAfterUtcTicks = 0; o.PrepareSharedRequest(j);
            if (!j.RequestPreparationPending) return;
        }
        throw new Exception("admission preparation did not finish in bounded fixture");
    }

    internal static void Run()
    {
        Scores();
        WeightedCountrySelectionReplay.Run();
        LegacySelectionUpgrade();
        DirectTargetReplay();
        ProtectedRepairBudgets();
        ProtocolFailureReplay();
        SemanticRejectionBudget();
        ChangedSeedIdentity();
        ConfirmedIdentityLifecycle();
        RecoveredIdentityResumesExhaustedHold();
        SingleCountryRoute();
        foreach (bool extra in new[] { false, true }) Planning(extra);
        Planning(true,true);
        FailedCapture();
        SnapshotFailure();
        Entrances();
    }

    // Older content/audit tests start at the full-concept boundary. Explicitly
    // advance the newly required seed stages with controlled direct-subject
    // judgments; these are not evidence for model quality or score expansion.
    internal static void FreezeFixture(WorldDiplomacyOrchestration o, WorldDiplomacyJob j, IEnumerable<string> countries, bool prepareConcept = true)
    {
        if (!string.IsNullOrEmpty(j.CountryAdmission) || j.EventSelectionStage is "candidates" or "compare") return;
        Prepare(o, j);
        if (!string.IsNullOrEmpty(j.CountryAdmission)) return; // Structured oral parties freeze during preparation.
        var sources = JArray.Parse(j.EventMaterialSnapshot);
        string source = sources.OfType<JObject>().Any(x => (string)x["key"]! == "world:current") ? "world:current" : (string)sources[0]["key"]!;
        Complete(o, j, new() { ["direct_subjects"] = new JArray(countries.Distinct().Select(x => new JObject {
            ["kind"] = "kingdom", ["id"] = x, ["source"] = source, ["action"] = "Controlled fixture directly proposes or bears the issue",
            ["target"] = "The supplied fixture issue" })) });
        Prepare(o, j);
        Complete(o, j, new() { ["approved"] = true, ["seed_digest"] = JObject.Parse(j.UserPrompt)["seed_digest"]!.DeepClone(),
            ["reason"] = "Controlled legacy content fixture: direct subjects fixed before expansion." });
        Test.True(!string.IsNullOrEmpty(j.CountryAdmission),"fixture advances actual seed stages: "+j.SuspendedReason);
        if (!prepareConcept) return;
        Prepare(o, j);
        Test.True(!j.RequestPreparationPending,"fixture finishes targeted supplementation: "+j.SuspendedReason);
        Test.True(!string.IsNullOrEmpty(j.CountryAdmission),"fixture retains frozen admission during supplementation: "+j.SuspendedReason+" / "+o.ResolveRound(j.RoundId).EventSelectionEvidence);
    }

    internal static void BeforeLegacyConcept(WorldDiplomacyOrchestration o, WorldDiplomacyJob j, JObject result)
    {
        if (j.Kind == "event_concept" && result["participants"] is JArray parties && string.IsNullOrEmpty(j.CountryAdmission))
            FreezeFixture(o, j, parties.OfType<JObject>().Select(x => (string)x["kingdom"]!));
        else if(j.Kind == "event_concept" && (bool?)result["no_negotiation"] == true && string.IsNullOrEmpty(j.CountryAdmission))
            FreezeFixture(o, j, ((JArray?)JObject.Parse(j.EventSelectedCandidate)["kingdoms"])?.Values<string>()! ?? new[]{"b"}, prepareConcept: false);
    }

    private static void Scores()
    {
        int[] values = { 0, 19, 20, 39, 40, 59, 60, 79, 80, 100 };
        int[] ruler = { 0, 0, 5, 5, 10, 10, 15, 15, 20, 20 }, core = { 0, 0, 10, 10, 20, 20, 30, 30, 35, 35 };
        for (int i = 0; i < values.Length; i++)
            foreach (int sign in new[] { -1, 1 })
            {
                Test.True(DiplomacyCountryAdmission.RelationPoints(sign * values[i], false) == ruler[i], "ruler signed band " + sign * values[i]);
                Test.True(DiplomacyCountryAdmission.RelationPoints(sign * values[i], true) == core[i], "core signed band " + sign * values[i]);
            }
        var input = new DiplomacyAdmissionInput { Direct = new() { "a", "b" }, Candidates = new() { "a", "b", "c" }, Heroes = new() { ["ra"] = "a", ["ha"] = "a", ["hb"] = "a" },
            Pairs = new() { new() { Country = "c", Origin = "a", OriginRuler = "ra", Border = true,
                Relations = new() { ["ra"] = 90, ["ha"] = -60, ["hb"] = 20 } },
                new() { Country = "c", Origin = "b", OriginRuler = "rb", War = true, Relations = new() { ["rb"] = 20 } } } };
        var result = DiplomacyCountryAdmission.Calculate(input);
        Test.True(result.Scores[0].Total == 70 && result.Scores[0].CoreHero == "ha", "best distinct combination beats doubled same ruler and multiple cores");
        input.Heroes.Remove("ha"); input.Heroes.Remove("hb");
        result = DiplomacyCountryAdmission.Calculate(input);
        Test.True(result.Scores[0].Total == 55, "same ruler counted once; different origins not summed");
        Test.True(input.Pairs[0].Relations["ra"] == 90, "raw relationship evidence preserved");
        input.Pairs.Add(new() { Country = "d", Origin = "c" });
        bool rejected = false;
        try { DiplomacyCountryAdmission.Calculate(input); } catch (ArgumentException) { rejected = true; }
        Test.True(rejected, "new countries cannot recursively become origins");
    }

    private static void LegacySelectionUpgrade()
    {
        foreach (string mode in new[] { "prepare", "held", "published", "corrupt_comparison", "uncertain" })
        {
            var (h, o, j) = SeedOwner(false);
            var r = o.ResolveRound(j.RoundId);
            r.CountryAdmissionVersion = 1;
            string seed = Subject("world:current", "kingdom", "a").ToString(Formatting.None);
            var legacy = new DiplomacyAdmissionResult { Version = 1, Threshold = 60, Seed = seed,
                Input = new() { Direct = new() { "a" } }, Admitted = new() { "a" } };
            j.CountryAdmission = JsonConvert.SerializeObject(legacy); j.CountryAdmissionSeed = seed;
            j.PrivacyDraft = "old one-country concept"; j.Kind = "event_concept_audit";
            string previous = j.CountryAdmission;
            if (mode == "published") o.CurrentStorage.EventAnnouncements.Add(new() { EventId = r.RoundId, Stage = "started" });
            if (mode == "uncertain") r.TechnicalHoldReason = "event_document_execution_uncertain";
            if (mode == "corrupt_comparison") j.EventComparison = "broken";
            if (mode == "held")
            {
                j.SuspendedReason = r.TechnicalHoldReason = r.TechnicalRequestHoldReason = "admission_identity_existing_case_review:single_admitted_npc_has_no_external_counterparty";
                j.TechnicalRecoveryAttempts = 3; j.RetryAfterUtcTicks = 0; j.NextRecoveryProbeUtcTicks = 0;
                Call(o, "TryResumeTechnicalRequest", j, false);
            }
            else Call(o, "UpgradeLegacyCountrySelection", j, r);
            if (mode is "published" or "uncertain" or "corrupt_comparison")
            {
                Test.True(j.CountryAdmission == previous && r.CountryAdmissionVersion == 1 && j.PrivacyDraft == "old one-country concept",
                    "migration preserves published/uncertain/invalid-input evidence atomically: " + mode);
                continue;
            }
            var result = JsonConvert.DeserializeObject<DiplomacyAdmissionResult>(j.CountryAdmission)!;
            Test.True(result.Version == 2 && result.Admitted.Count >= 2 && j.Kind == "event_concept" && j.PrivacyDraft == ""
                && j.SuspendedReason == "" && r.TechnicalHoldReason == "", "unpublished legacy selection is replaced once, including exhausted single-country holds: " + mode);
            string frozen = j.CountryAdmission;
            h.CountPreference = 2;
            o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
            j = o.CurrentStorage.Jobs.Single(x => x.JobId == j.JobId); r = o.ResolveRound(j.RoundId);
            Prepare(o, j);
            Test.True(j.CountryAdmission == frozen && h.Captures == 1 && r.CountryAdmissionVersion == 2,
                "reload and settings changes cannot repeat migration or redraw the frozen cast");
        }
    }

    private static (Host h, WorldDiplomacyOrchestration o, WorldDiplomacyJob j) SeedOwner(bool extra,bool fourthNpc=false)
    {
        var h = new Host { Extra = extra, FourthNpc=fourthNpc, PublishEnabled = true, PlayerAwareExecution = true, OralRange = (0, 3000) };
        var o = new WorldDiplomacyOrchestration(h, new()); h.Owner = o;
        o.TryScheduleNormalRound(); var j = o.CurrentStorage.Jobs.Single(x => x.Kind == "event_concept");
        Prepare(o, j);
        Complete(o, j, new() { ["candidates"] = new JArray(new JObject {
            ["id"] = "seed", ["kingdoms"] = new JArray("a", "b"), ["material_keys"] = new JArray("world:current"),
            ["theme"] = "history", ["theme_reason"] = "Rival accounts of the oath", ["conflict"] = "The renewal of an oath is disputed",
            ["motive"] = "Recognition of precedence", ["new_development"] = "Renewal is due", ["npc_creation"] = "A request for a hearing",
            ["negotiation_space"] = "Review and recognition without confession" }) });
        Prepare(o, j);
        Complete(o, j, new() { ["assessments"] = new JArray(new JObject { ["id"] = "seed", ["qualified"] = true,
            ["strength"] = "Specific disputed oath", ["weakness"] = "Witnesses disagree", ["novelty_reason"] = "Renewal is due",
            ["urgency_reason"] = "No immediate deadline", ["duplicate_of"] = "" }), ["best_equivalent"] = new JArray("seed"),
            ["urgent"] = new JArray(), ["comparison_reason"] = "One qualifying seed" });
        return (h, o, j);
    }

    private static void Freeze(WorldDiplomacyOrchestration o, WorldDiplomacyJob j)
    {
        Prepare(o, j);
        Test.True((string)JObject.Parse(j.UserPrompt)["phase"]! == "identify", "short identity stage precedes full writing");
        Complete(o, j, new() { ["direct_subjects"] = new JArray(new[] { "a", "b" }.Select(x => new JObject {
            ["kind"] = "kingdom", ["id"] = x, ["source"] = "world:current", ["action"] = "Disputes the oath renewal",
            ["target"] = x == "a" ? "b" : "a" })) });
        Prepare(o, j);
        Complete(o, j, new() { ["approved"] = true, ["seed_digest"] = JObject.Parse(j.UserPrompt)["seed_digest"]!.DeepClone(),
            ["reason"] = "Controlled audit: both realms directly dispute the oath, no incidental states." });
    }

    private static void Planning(bool extra,bool fourthNpc=false)
    {
        var (h, o, j) = SeedOwner(extra,fourthNpc); Freeze(o, j);
        var result = JsonConvert.DeserializeObject<DiplomacyAdmissionResult>(j.CountryAdmission)!;
        Test.True(result.Admitted.Count == result.TargetCount && result.TargetCount == Math.Min(fourthNpc ? 4 : 3, result.SampledCount) && !result.Admitted.Contains("p"), "program fills sampled count even with zero relations, within the legal NPC pool");
        string frozen = j.CountryAdmission;
        o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
        j = o.CurrentStorage.Jobs.Single(x => x.JobId == j.JobId); h.Extra = !extra;
        Prepare(o, j);
        Test.True(j.CountryAdmission == frozen && h.Captures == 1, "save/retry uses frozen snapshot despite relation changes");
        Test.True(result.Admitted.All(j.AdmissionSupplementedCountries.Contains), "all admitted realms get targeted material");
        var request = JObject.Parse(j.UserPrompt);
        Test.True(new HashSet<string>(((JArray)request["frozen_participants"]!).Values<string>()!).SetEquals(result.Admitted), "writer receives frozen membership");
        var concept = new JObject { ["title"] = "Disputed oath renewal", ["background"] = "The renewal of an old oath brings rival recognition claims before the courts.",
            ["initiator"] = "a", ["material_keys"] = new JArray("world:current"),
            ["participants"] = new JArray(result.Admitted.Select(x => new JObject { ["kingdom"] = x, ["agenda"] = "Recognition through review",
                ["gain"] = "Recognized precedence", ["cost"] = "Mutual inspection", ["red_line"] = "No confession of forgery", ["alternative"] = "Leave claims unresolved",
                ["source_keys"] = new JArray("world:current"), ["address_to"] = new JArray(x == "a" ? "b" : "a") })),
            ["party_review"] = new JArray(new JObject { ["kingdom"] = "c", ["included"] = false, ["reason"] = "C need not join" }),
            ["narrative"] = new JArray(new JObject { ["kind"] = "allegation", ["subject"] = "a", ["source"] = "world:current",
                ["text"] = "The courts dispute the account of the oath.", ["hero_ids"] = new JArray() }) };
        Complete(o, j, concept);
        Test.True(j.Kind == "event_concept_audit", "obsolete party_review cannot veto program membership");
        Prepare(o, j); Complete(o, j, MaterialCountryThirdReplay.Approved(j));
        var round = o.ResolveRound(j.RoundId);
        Test.True(round.EventPhase == "opening" && round.CountryAdmission == frozen && round.Participants.Count == result.Admitted.Count,
            "full reviewed positions and frozen result adopted atomically: " + j.SuspendedReason);
        Test.True(round.Participants.All(x => x.AgendaAddressTo.Count > 0 && x.AgendaSourceKeys.Count > 0 && x.Alternative.Length > 0), "every admitted NPC has complete consumer data");
        Lifecycle(h,o,round);
    }

    private static JObject Boundary() => new() { ["title"]="Disputed oath",["body"]="The courts acknowledge their unresolved claims.",
        ["effects"]=new JArray(new JObject{["kind"]="ruler_relation",["kingdom"]="a",["other"]="b",["delta"]=-1,["days"]=0,["reason"]="The disputed oath strains relations."}),
        ["remove_effects"]=new JArray(),["confirmed_documents"]=new JArray(),["narrative"]=new JArray() };

    private static void Reviewed(WorldDiplomacyOrchestration o,WorldDiplomacyJob j,JObject value)
    {
        Prepare(o,j);Complete(o,j,value);Prepare(o,j);
        var audit=MaterialCountryThirdReplay.Approved(j);audit["closure_consistent"]=true;Complete(o,j,audit);
    }

    private static void Lifecycle(Host h,WorldDiplomacyOrchestration o,WorldDiplomacyRound round)
    {
        var opening=o.CurrentStorage.Jobs.Single(x=>x.Kind=="event_boundary");
        h.UnavailableParty="b";h.IdentityMode="kingdom_missing";Prepare(o,opening);Complete(o,opening,Boundary());
        Test.True(opening.SuspendedReason.StartsWith("admission_identity_publication_hold:")&&h.Writes==0,"identity loss stops publication without pruning frozen countries: "+opening.SuspendedReason);
        h.UnavailableParty=null!;h.IdentityMode="";Call(o,"TryResumeTechnicalRequest",opening,true);
        Reviewed(o,opening,Boundary());
        Test.True(round.EventBeginningId!=""&&h.Writes==1,"frozen cast reaches actual opening and effect owner");
        for(int step=0;step<16;step++)
        {
            var speech=o.CurrentStorage.Jobs.FirstOrDefault(x=>x.Kind=="generate");
            if(speech!=null)EventStructureReplay.PublishDeclaration(o,speech,speech.TargetKingdomId,"statement",false);
            h.CurrentDayValue+=8;o.ProcessPropagationArrivals();o.ProcessRoundLifecycle();
            foreach(var decision in o.CurrentStorage.Jobs.Where(x=>x.Kind=="player_decision").ToList())
                Complete(o,decision,new(){["participate"]=false,["reason"]="No optional interest."});
            if(round.EventPhase=="closing" || o.CurrentStorage.Jobs.Any(x=>x.Kind=="event_progress")&&!o.CurrentStorage.Jobs.Any(x=>x.Kind=="generate"))break;
        }
        Test.True(round.Participants.All(x=>!string.IsNullOrEmpty(x.AgendaExpressionDocumentId)),"every scored participant actually publishes its substantive position");
        Test.True(round.Participants.All(x=>o.ResolveDocument(x.AgendaExpressionDocumentId)?.IsReadyForPublication==true),"position receipts bind actual published declarations");
        var progress=o.CurrentStorage.Jobs.SingleOrDefault(x=>x.Kind=="event_progress");
        if(progress!=null)Reviewed(o,progress,new(){["outcome"]="deferred",["reason"]="All claims were expressed and answered; no agreement or useful pending proposal remains.",
            ["positions"]=new JArray(round.Participants.Select(x=>new JObject{["kingdom"]=x.KingdomId,["agenda"]=x.Agenda,["gain"]=x.PreferredOutcome,["cost"]=x.ExpectedCost,["red_line"]=x.RedLine,["alternative"]=x.Alternative})),
            ["closure"]=new JArray(round.Participants.Select(x=>new JObject{["kingdom"]=x.KingdomId,["status"]="unresolved",["reason"]="No agreement on recognition",["sources"]=new JArray(x.AgendaExpressionDocumentId)})),["effect_update"]=null});
        Test.True(round.EventPhase=="closing","complete multi-country exchange can enter reviewed closure");
        Reviewed(o,o.CurrentStorage.Jobs.Single(x=>x.Kind=="event_boundary"),Boundary());
        Test.True(round.State=="closed"&&round.EventEndingId!=""&&h.Writes==2,"full scored event closes through real ending owner");
    }

    private static JObject Subject(string source, string kind = "hero", string id = "lord_7_3") => new() {
        ["direct_subjects"] = new JArray(new JObject { ["kind"] = kind, ["id"] = id, ["source"] = source,
            ["action"] = "Disputes renewal of the recorded oath", ["target"] = "Recognition of the oath" }) };

    private static void Resume(WorldDiplomacyOrchestration o, WorldDiplomacyJob j)
    {
        j.NextRecoveryProbeUtcTicks = 0;
        Call(o, "TryResumeTechnicalRequest", j, false);
        Test.True(j.SuspendedReason == "", "bounded protocol recovery resumes without explicit budget reset");
        Prepare(o, j);
    }

    private static void ProtocolFailureReplay()
    {
        foreach (string invalidSource in new[] { "sources.persona:lord_7_3", "aserai", "sources.hero_ids", "world:current" })
        {
            var (_, o, j) = SeedOwner(false);
            var sources = JArray.Parse(j.EventMaterialSnapshot);
            sources.Add(new JObject { ["key"] = "persona:lord_7_3", ["known_by"] = "a", ["text"] = "The lord disputes the oath.",
                ["hero_ids"] = new JArray("lord_7_3") });
            j.EventMaterialSnapshot = sources.ToString(Formatting.None);
            Prepare(o, j);
            var request = JObject.Parse(j.UserPrompt);
            Test.True(((JArray)request["hero_source_keys"]!["lord_7_3"]!).Values<string>().Contains("persona:lord_7_3"), "real request lists exact available hero binding");
            string material = j.EventMaterialSnapshot, candidate = j.EventSelectedCandidate;
            for (int i = 0; i < 2; i++)
            {
                Complete(o, j, Subject(invalidSource));
                Test.True(j.MaterialRepairFeedback.Contains("persona:lord_7_3") && j.MaterialRepairFeedback.Contains("allowed_source_keys"), "invalid live source value receives exact allowed binding: " + invalidSource);
                Test.True(j.TechnicalRecoveryAttempts == i + 1 && j.EventMaterialSnapshot == material && j.MaterialSearchPass == 0, "format failure retains evidence and attempt budget");
                Resume(o, j);
            }
            Complete(o, j, Subject("persona:lord_7_3"));
            Test.True(j.TechnicalRecoveryAttempts == 0 && j.CountryAdmissionSeed != "", "two failed identifications do not consume audit budget");
            Prepare(o, j); request = JObject.Parse(j.UserPrompt);
            string seed = j.CountryAdmissionSeed;
            Test.True((string)request["phase"]! == "audit" && (string)request["response_contract"]!["seed_digest"]! == (string)request["seed_digest"]!, "audit has a distinct response contract with exact checksum");
            for (int i = 0; i < 3; i++)
            {
                // Actual production failure: approved=true but the whole seed is
                // copied into the checksum field. It must never be accepted.
                Complete(o, j, new() { ["approved"] = true, ["seed_digest"] = seed, ["reason"] = "Direct subjects checked" });
                if (i < 2)
                {
                    Test.True(j.TechnicalRecoveryAttempts == i + 1 && j.CountryAdmission == "" && j.CountryAdmissionSeed == seed
                        && j.EventSelectedCandidate == candidate && j.EventMaterialSnapshot == material && j.MaterialSearchPass == 0,
                        "bounded audit checksum repair keeps exact seed/material");
                    Test.True(j.MaterialRepairFeedback.Contains((string)request["seed_digest"]!), "audit feedback specifies actual required checksum");
                    Resume(o, j);
                }
                else Test.True(j.MaterialSearchPass == 1 && j.EventSelectionStage == "candidates" && j.CountryAdmissionSeed == "",
                    "exhausted ordinary draft advances bounded research rather than occupying a slot forever");
            }
            o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
            var restored = o.CurrentStorage.Jobs.Single(x => x.JobId == j.JobId);
            restored.NextRecoveryProbeUtcTicks = 0; restored.RetryAfterUtcTicks = 0;
            Call(o, "TryResumeTechnicalRequest", restored, false);
            Test.True(restored.MaterialSearchPass == 1 && restored.CountryAdmissionSeed == "" && restored.EventSelectionStage == "candidates",
                "reload retains bounded research progress instead of resurrecting an exhausted seed");
        }
    }

    private static void SemanticRejectionBudget()
    {
        var (_, o, j) = SeedOwner(false);
        for (int i = 0; i < 3; i++)
        {
            Prepare(o, j); Complete(o, j, Subject("world:current", "kingdom", "a")); Prepare(o, j);
            Complete(o, j, new() { ["approved"] = false, ["seed_digest"] = JObject.Parse(j.UserPrompt)["seed_digest"]!.DeepClone(),
                ["reason"] = "The candidate mentions this country only as background." });
            if (i < 2) Test.True(j.AdmissionSeedRewrites == i + 1 && j.MaterialSearchPass == 0, "semantic rejection is bounded separately from format correction");
            if (i < 2)
            {
                Test.True(j.TechnicalRecoveryAttempts == 0, "semantic rejection does not spend the new identify protocol budget");
                Resume(o, j);
            }
        }
        Test.True(j.MaterialSearchPass == 1 && j.CountryAdmissionSeed == "" && j.EventSelectionStage == "candidates", "third ordinary semantic rejection leaves exhausted seed and advances bounded research");
        var copy = JsonConvert.DeserializeObject<WorldDiplomacyJob>(JsonConvert.SerializeObject(j))!;
        Test.True(copy.MaterialSearchPass == 1 && copy.AdmissionSeedRewrites == 0, "persistence retains new-batch identity and its independent budget");
    }

    private static void ProtectedRepairBudgets()
    {
        foreach (string mode in new[] { "published", "oral", "pending_reply" })
        {
            var (_, o, j) = SeedOwner(false); var round = o.ResolveRound(j.RoundId);
            Prepare(o, j); Complete(o, j, Subject("world:current", "kingdom", "a")); Prepare(o, j);
            string seed = j.CountryAdmissionSeed;
            if (mode == "published") o.CurrentStorage.Documents.Add(new() { DocumentId = "protected-seed", RoundId = j.RoundId });
            if (mode == "oral") round.EventSourceType = "dialogue_commitment";
            if (mode == "pending_reply") round.PlayerResponses.Add(new() { KingdomId = "b", Status = "pending", SourceDocumentId = "recorded-player-source", OriginalRoundId = round.RoundId });
            for (int attempt = 0; attempt < 3; attempt++)
            {
                Complete(o, j, new() { ["approved"] = true, ["seed_digest"] = seed, ["reason"] = "Controlled invalid checksum" });
                if (attempt < 2) Resume(o, j);
            }
            Test.True(j.TechnicalRecoveryAttempts == 3 && j.CountryAdmissionSeed == seed && j.MaterialSearchPass == 0 && round.State == "active",
                "protected case retains bounded hold and original seed: " + mode);
            o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
            var restored = o.CurrentStorage.Jobs.Single(x => x.JobId == j.JobId);
            restored.NextRecoveryProbeUtcTicks = 0; restored.RetryAfterUtcTicks = 0;
            Call(o, "TryResumeTechnicalRequest", restored, false);
            Test.True(restored.TechnicalRecoveryAttempts == 3 && restored.SuspendedReason != "" && restored.CountryAdmissionSeed == seed,
                "protected exhausted budget remains exhausted after reload: " + mode);
        }
    }

    private static void ChangedSeedIdentity()
    {
        var (h, o, j) = SeedOwner(false);
        Prepare(o, j); Complete(o, j, Subject("world:current", "kingdom", "a")); Prepare(o, j);
        string seed = j.CountryAdmissionSeed, digest = (string)JObject.Parse(j.UserPrompt)["seed_digest"]!;
        h.UnavailableParty = "a";
        Complete(o, j, new() { ["approved"] = true, ["seed_digest"] = digest, ["reason"] = "Controlled stale review" });
        Test.True(j.CountryAdmissionSeed == "" && j.MaterialSearchPass == 1 && o.ResolveRound(j.RoundId).State == "active"
            && (string)JObject.Parse(o.ResolveRound(j.RoundId).EventSelectionEvidence)["seed"]! == seed && h.Captures == 0,
            "confirmed authority loss retires only the unpublished seed with its evidence, never scores an invalid party");
    }

    private static void ConfirmedIdentityLifecycle()
    {
        Test.True(DiplomacyAdmissionIdentity.Hero(false, false, false, null, "a").State == DiplomacyAdmissionIdentityState.Unknown,
            "missing native hero is unknown, never death");
        Test.True(DiplomacyAdmissionIdentity.Hero(true, false, false, "a", "a").State == DiplomacyAdmissionIdentityState.Unknown,
            "not-alive without explicit death is unknown");
        Test.True(DiplomacyAdmissionIdentity.Kingdom(false, false, "a").State == DiplomacyAdmissionIdentityState.Unknown,
            "missing native kingdom is unknown, never elimination");
        foreach (string phase in new[] { "forming", "opening" })
        foreach (string mode in new[] { "dead", "moved", "eliminated", "missing", "empty_owner", "exception", "valid", "corrupt", "missing_snapshot" })
        {
            var (h, o, j) = SeedOwner(false); Freeze(o, j);
            var round = o.ResolveRound(j.RoundId);
            var admission = JsonConvert.DeserializeObject<DiplomacyAdmissionResult>(j.CountryAdmission)!;
            admission.Input.Heroes["lord_7_3"] = "a";
            j.CountryAdmission = JsonConvert.SerializeObject(admission);
            string seed = j.CountryAdmissionSeed, frozen = j.CountryAdmission;
            h.IdentityMode = mode;
            if (mode == "corrupt") j.CountryAdmission = "{\"Input\":null}";
            if (mode == "missing_snapshot") { j.CountryAdmission = ""; j.CountryAdmissionSeed = ""; }
            round.EventPhase = phase;
            if (phase == "opening")
            {
                round.CountryAdmissionVersion = 1; round.CountryAdmission = j.CountryAdmission;
                j.Kind = "event_boundary"; j.ForcedIntent = "started";
                // The round is authoritative at publication, even if a stale job copy is valid.
                if (mode is "corrupt" or "missing_snapshot") j.CountryAdmission = frozen;
            }
            string preserved = j.CountryAdmission;
            if (phase == "opening") Call(o, "CheckOpeningAdmission", j, round);
            else Call(o, "HoldTechnicalRequest", j, "admission_core_identity_changed_requires_recovery");
            bool confirmed = mode is "dead" or "moved" or "eliminated";
            if (confirmed)
            {
                var evidence = JObject.Parse(round.EventSelectionEvidence);
                Test.True((string)evidence["outcome"]! == "confirmed_identity_invalidated" && (string)evidence["seed"]! == seed
                    && (string)evidence["admission"]! == frozen, "retirement preserves exact seed/snapshot: " + phase + "/" + mode);
                Test.True(phase == "forming" ? j.MaterialSearchPass == 1 && j.EventSelectionStage == "candidates" && j.CountryAdmission == ""
                    : round.State != "active", "confirmed invalidation exits disposable owner: " + phase + "/" + mode);
                Test.True(j.RequestPreparationPending && j.LlmMessages.Count == 0, "retirement cannot dispatch old prompt: " + phase + "/" + mode);
            }
            else Test.True(round.State == "active" && j.MaterialSearchPass == 0 && j.CountryAdmission == preserved
                && j.SuspendedReason != "", "unknown/corrupt identity preserves hold: " + phase + "/" + mode);
            if (mode is "corrupt" or "missing_snapshot") Test.True(h.IdentityReads == 0, "missing/corrupt snapshot cannot obtain retirement proof");
            Test.True(h.Captures == 1, "identity recovery never rescores relationships");
        }
        foreach (string protection in new[] { "oral", "published", "effect", "pending_reply" })
        {
            var (h, o, j) = SeedOwner(false); Freeze(o, j); var round = o.ResolveRound(j.RoundId);
            string frozen = j.CountryAdmission;
            if (protection == "oral") round.EventSourceType = "dialogue_commitment";
            if (protection == "published") o.CurrentStorage.Documents.Add(new() { DocumentId = "protected", RoundId = round.RoundId });
            if (protection == "effect") o.CurrentStorage.EventEffects.Add(new() { EventId = round.RoundId });
            if (protection == "pending_reply") round.PlayerResponses.Add(new() { KingdomId = "b", Status = "pending", SourceDocumentId = "player-source", OriginalRoundId = round.RoundId });
            h.IdentityMode = "eliminated";
            Call(o, "HoldTechnicalRequest", j, "admission_identity_unavailable_requires_recovery");
            Test.True(round.State == "active" && j.CountryAdmission == frozen && j.MaterialSearchPass == 0 && j.SuspendedReason != "",
                "confirmed loss cannot erase existing obligations: " + protection);
        }
        // Existing exhausted save resumes only on the cold probe, retaining its search budget.
        {
            var (h, o, j) = SeedOwner(false); Freeze(o, j);
            j.MaterialSearchPass = 2;
            // Restore the old exhausted shape directly; the new runtime closes
            // a disposable unknown-identity draft on its third failed attempt.
            j.SuspendedReason = "admission_identity_unavailable_requires_recovery";
            j.TechnicalRecoveryAttempts = 3;
            o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
            j = o.CurrentStorage.Jobs.Single(x => x.JobId == j.JobId); h.IdentityMode = "eliminated";
            j.RetryAfterUtcTicks = 0; j.NextRecoveryProbeUtcTicks = DateTime.UtcNow.AddMinutes(1).Ticks;
            int reads = h.IdentityReads; Call(o, "TryResumeTechnicalRequest", j, false);
            Test.True(h.IdentityReads == reads, "held identity is not read before cold probe deadline");
            j.NextRecoveryProbeUtcTicks = 0; Call(o, "TryResumeTechnicalRequest", j, false);
            Test.True(o.ResolveRound(j.RoundId).State != "active" && j.MaterialSearchPass == 2,
                "confirmed invalidation releases exhausted restored owner without resetting search budget");
        }
        // Real scheduled owners already have schema version 1 before any snapshot.
        foreach (string mode in new[] { "dead", "eliminated" })
        {
            var (h, o, j) = SeedOwner(false); Prepare(o, j);
            Test.True(o.ResolveRound(j.RoundId).CountryAdmissionVersion == 2, "actual producer marks forming owner schema before freeze");
            var sources = JArray.Parse(j.EventMaterialSnapshot);
            ((JObject)sources.Single(x => (string)x["key"]! == "world:current"))["hero_ids"] = new JArray("lord_7_3");
            j.EventMaterialSnapshot = sources.ToString(Formatting.None);
            Complete(o, j, Subject("world:current", mode == "dead" ? "hero" : "kingdom", mode == "dead" ? "lord_7_3" : "a")); Prepare(o, j);
            string digest = (string)JObject.Parse(j.UserPrompt)["seed_digest"]!, seed = j.CountryAdmissionSeed;
            h.IdentityMode = mode; h.UnavailableParty = "a";
            Complete(o, j, new() { ["approved"] = true, ["seed_digest"] = digest, ["reason"] = "Controlled stale audit" });
            Test.True(j.MaterialSearchPass == 1 && j.CountryAdmissionSeed == "" && h.Captures == 0
                && (string)JObject.Parse(o.ResolveRound(j.RoundId).EventSelectionEvidence)["seed"]! == seed,
                "confirmed invalid seed retires before score capture while preserving evidence: pass=" + j.MaterialSearchPass
                    + "; captures=" + h.Captures + "; reason=" + j.SuspendedReason + "; evidence=" + o.ResolveRound(j.RoundId).EventSelectionEvidence);
        }
    }

    private static void RecoveredIdentityResumesExhaustedHold()
    {
        foreach (string state in new[] { "protected", "disposable", "still_unknown", "uncertain", "snapshot_invalid", "null_owner", "foreign_owner", "policy_hold" })
        {
            var (h, o, j) = SeedOwner(false); Freeze(o, j);
            var round = o.ResolveRound(j.RoundId);
            if (state != "disposable")
                o.CurrentStorage.Documents.Add(new() { DocumentId = "retained-player-input", RoundId = round.RoundId });
            h.UnavailableParty = "b"; h.IdentityMode = "kingdom_missing";
            j.SuspendedReason = state == "policy_hold" ? "admission_identity_existing_case_review:single_admitted_npc_has_no_external_counterparty"
                : "admission_identity_unavailable_requires_recovery:kingdom_independent_authority_unavailable:b";
            j.TechnicalRecoveryAttempts = 3;
            j.RecoveryConfiguration = (string)Call(o, "SpeechConfiguration")!;
            j.RecoveryRevision = round.ConversationRevision;
            j.RecoveryKnowledgeRevision = DiplomacyEventRules.KnowledgeRevision(round, j.AuthorKingdomId);
            round.TechnicalHoldReason = round.TechnicalRequestHoldReason = j.SuspendedReason;
            if (state == "uncertain")
                o.CurrentStorage.EventEffects.Add(new() { EventId = round.RoundId, Status = "uncertain" });
            if (state == "snapshot_invalid") j.CountryAdmission = "{\"Input\":null}";
            if (state is "null_owner" or "foreign_owner")
            {
                var malformed = JObject.Parse(j.CountryAdmission);
                malformed["Input"]!["Heroes"] = state == "null_owner"
                    ? new JObject { ["missing_hero"] = null }
                    : new JObject { ["ruler_c"] = "c" };
                j.CountryAdmission = malformed.ToString(Formatting.None);
            }
            string snapshot = j.CountryAdmission;
            o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
            j = o.CurrentStorage.Jobs.Single(x => x.JobId == j.JobId); round = o.ResolveRound(j.RoundId);
            if (state != "still_unknown") { h.UnavailableParty = null!; h.IdentityMode = ""; }
            j.RetryAfterUtcTicks = 0; j.NextRecoveryProbeUtcTicks = DateTime.UtcNow.AddMinutes(1).Ticks;
            Call(o, "TryResumeTechnicalRequest", j, false);
            Test.True(j.SuspendedReason != "" && j.TechnicalRecoveryAttempts == 3, "recovered identity obeys cold probe deadline: " + state);
            j.NextRecoveryProbeUtcTicks = 0;
            Call(o, "TryResumeTechnicalRequest", j, false);
            bool resumes = state is "protected" or "disposable";
            Test.True(resumes ? j.SuspendedReason == "" && j.TechnicalRecoveryAttempts == 0
                : j.SuspendedReason != "" && j.TechnicalRecoveryAttempts == 3,
                "only fully recovered identity resumes exhausted work: " + state);
            Test.True(o.CurrentStorage.Jobs.Contains(j) && round.State == "active" && j.CountryAdmission == snapshot
                && h.Captures == 1 && h.Writes == 0, "recovery retains owner/evidence without rescoring or side effects: " + state);
            if (resumes) Test.True(round.TechnicalHoldReason == "", "recovered task releases matching round hold");
        }
    }

    private static void SingleCountryRoute()
    {
        foreach (string mode in new[] { "ordinary", "oral", "published", "effect", "player", "pending_reply", "identity_changed" })
        {
            var (h, o, j) = SeedOwner(false);
            string direct = mode == "player" ? "p" : "a";
            var round = o.ResolveRound(j.RoundId);
            // Frozen production scoring, with no invented qualifying second party.
            j.CountryAdmission = JsonConvert.SerializeObject(DiplomacyCountryAdmission.Calculate(new() { Direct = new() { direct } }));
            j.CountryAdmissionSeed = Subject("world:current", "kingdom", direct).ToString(Formatting.None);
            string frozen = j.CountryAdmission;
            if (mode == "oral") j.EventSelectionStage = "oral";
            if (mode == "identity_changed") h.UnavailableParty = "a";
            if (mode == "published") o.CurrentStorage.Documents.Add(new() { DocumentId = "protected", RoundId = round.RoundId });
            if (mode == "effect") o.CurrentStorage.EventEffects.Add(new() { EventId = round.RoundId });
            if (mode == "pending_reply") round.PlayerResponses.Add(new() { KingdomId = "b", Status = "pending" });
            bool handled = (bool)Call(o, "HandleSingleCountryAdmission", j, round);
            if (mode is "ordinary" or "identity_changed")
                Test.True(handled && j.EventSelectionStage == "candidates" && j.MaterialSearchPass == 1 && h.Captures == 0,
                    "single NPC or confirmed loss of independent authority exits before impossible writer attempts: " + mode);
            else if (mode is "player" or "pending_reply")
                Test.True(!handled && j.CountryAdmission == frozen, "valid sole-player or limited respondent path is not removed: " + mode);
            else
                Test.True(handled && j.CountryAdmission == frozen && j.MaterialSearchPass == 0 && round.State == "active"
                    && j.SuspendedReason.StartsWith("admission_identity_"), "existing case is preserved instead of disposable NPC reroll: " + mode);
        }
        var (_, owner, job) = SeedOwner(false);
        Prepare(owner, job); Complete(owner, job, Subject("world:current", "kingdom", "a")); Prepare(owner, job);
        Complete(owner, job, new() { ["approved"] = true, ["seed_digest"] = JObject.Parse(job.UserPrompt)["seed_digest"]!.DeepClone(), ["reason"] = "One direct realm, no qualifying third country" });
        Test.True(job.CountryAdmission != "", "single country scoring itself remains valid");
        Prepare(owner, job);
        var selected = JsonConvert.DeserializeObject<DiplomacyAdmissionResult>(job.CountryAdmission)!;
        Test.True(job.EventSelectionStage == "expand" && job.MaterialSearchPass == 0 && selected.Admitted.Count >= 2
            && selected.Admitted.All(job.AdmissionSupplementedCountries.Contains) && JObject.Parse(job.UserPrompt)["phase"] == null,
            "a sole direct country with zero relations now reaches full writing with additional countries instead of discarding the seed");
    }

    private static void DirectTargetReplay()
    {
        // Exact 2026-10-25 06:08:52 response shape: recipients occur only in
        // target, never as another direct_subjects row. Game/source leaves below
        // reproduce the relevant current IDs and source ownership, not a model.
        JObject Incident() => JObject.Parse(@"{""direct_subjects"":[{""kind"":""hero"",""id"":""lord_1_1"",""source"":""persona:lord_1_1"",""action"":""拒绝承认南帝国的正统性并提交文告草案"",""target"":""lord_1_14""},{""kind"":""kingdom"",""id"":""empire"",""source"":""lore:f9628117c9cd3746"",""action"":""争夺帝国正统地位并指责对方为窃国者"",""target"":""empire_s""}]}");
        (Host h, WorldDiplomacyOrchestration o, WorldDiplomacyJob j) Setup()
        {
            var (h, o, j) = SeedOwner(false);
            j.CandidateKingdomIds = new() { "empire", "empire_s", "c", "p" };
            j.EventMaterialSnapshot = new JArray(
                new JObject { ["key"] = "world:current", ["source_kind"] = "current_world", ["known_by"] = "public",
                    ["hero_ids"] = new JArray("lord_1_1", "lord_1_14", "ruler_p"),
                    ["text"] = new JArray(new JObject { ["kingdom_id"] = "empire" },
                        new JObject { ["kingdom_id"] = "empire_s", ["towns"] = new JArray(new JObject { ["id"] = "fixture_town_s" }) },
                        new JObject { ["kingdom_id"] = "p" }).ToString(Formatting.None) },
                new JObject { ["key"] = "persona:lord_1_1", ["known_by"] = "empire", ["hero_ids"] = new JArray("lord_1_1"), ["text"] = "The ruler disputes southern imperial legitimacy." },
                new JObject { ["key"] = "lore:f9628117c9cd3746", ["known_by"] = "empire", ["hero_ids"] = new JArray("lord_1_1"), ["text"] = "The rival courts dispute imperial legitimacy." }
            ).ToString(Formatting.None);
            Prepare(o, j); return (h, o, j);
        }
        foreach (bool structured in new[] { false, true })
        {
            var (h, o, j) = Setup(); var seed = Incident();
            if (structured)
            {
                seed["direct_subjects"]![0]!["target"] = new JObject { ["kind"] = "hero", ["id"] = "lord_1_14", ["source"] = "world:current" };
                seed["direct_subjects"]![1]!["target"] = new JObject { ["kind"] = "kingdom", ["id"] = "empire_s", ["source"] = "world:current" };
            }
            Complete(o, j, seed);
            Test.True(j.CountryAdmissionSeed == seed.ToString(Formatting.None) && j.AdmissionRequiredSourceKeys.Contains("world:current"), "identify preserves exact seed and pins target identity evidence");
            Prepare(o, j);
            var request = JObject.Parse(j.UserPrompt);
            Test.True((string)request["seed"]! == seed.ToString(Formatting.None), "audit sees original actor/recipient binding, with no seed rewrite");
            Complete(o, j, new() { ["approved"] = true, ["seed_digest"] = request["seed_digest"]!.DeepClone(), ["reason"] = "Both direct initiator and recipient are supported by the supplied material." });
            var result = JsonConvert.DeserializeObject<DiplomacyAdmissionResult>(j.CountryAdmission)!;
            Test.True(result.Input.Direct.ToHashSet().SetEquals(new[] { "empire", "empire_s" }) && result.Admitted.Contains("empire_s"), "actual target-only southern recipient is direct, never excluded for scoring only 30 points");
            Test.True(result.Input.Heroes.Count == 2 && result.Input.Heroes["lord_1_14"] == "empire_s" && h.Captures == 1, "both core heroes captured once with their actual owners");
            Test.True(result.Version == 2 && result.Threshold == 0 && !result.Admitted.Contains("p"), "direct-target resolution uses weighted selection, never auto-selects player");
            Prepare(o, j);
            Test.True(j.EventSelectionStage == "expand" && j.MaterialSearchPass == 0 && ((JArray)JObject.Parse(j.UserPrompt)["frozen_participants"]!).Count == result.TargetCount, "complete two-country seed reaches writer without the single-country discard path");
        }
        {
            var (h, o, j) = Setup(); var seed = Incident();
            Complete(o, j, seed); Prepare(o, j); h.UnavailableParty = "empire_s";
            Complete(o, j, new() { ["approved"] = true, ["seed_digest"] = JObject.Parse(j.UserPrompt)["seed_digest"]!.DeepClone(), ["reason"] = "Stale controlled approval" });
            Test.True(j.CountryAdmission == "" && j.CountryAdmissionSeed == "" && j.MaterialSearchPass == 1
                && (string)JObject.Parse(o.ResolveRound(j.RoundId).EventSelectionEvidence)["seed"]! == seed.ToString(Formatting.None)
                && h.Captures == 0, "confirmed recipient authority loss archives the original seed and changes material without scoring invalid recipients");
        }
        foreach (string mode in new[] { "missing_source", "wrong_source", "unknown", "player", "dead", "settlement", "description", "embedded_id", "unindexed_legacy" })
        {
            var (h, o, j) = Setup(); var seed = Incident();
            seed["direct_subjects"] = new JArray(seed["direct_subjects"]![0]!.DeepClone());
            if (mode == "missing_source")
            { var material = JArray.Parse(j.EventMaterialSnapshot); foreach (var hero in ((JArray)material[0]["hero_ids"]!).Where(x => (string)x! == "lord_1_14").ToList()) hero.Remove(); j.EventMaterialSnapshot = material.ToString(Formatting.None); }
            if (mode is "missing_source" or "wrong_source" or "unknown" or "player" or "settlement")
                seed["direct_subjects"]![0]!["target"] = new JObject { ["kind"] = mode == "player" ? "kingdom" : mode == "settlement" ? "settlement" : "hero",
                    ["id"] = mode == "unknown" ? "no_such_hero" : mode == "player" ? "p" : mode == "settlement" ? "fixture_town_s" : "lord_1_14",
                    ["source"] = mode == "wrong_source" ? "persona:lord_1_1" : "world:current" };
            if (mode == "dead") h.UnavailableParty = "empire_s";
            if (mode is "description" or "embedded_id") seed["direct_subjects"]![0]!["target"] = mode == "description" ? "Recognition of the old imperial oath" : "A story mentioning lord_1_14";
            if (mode == "unindexed_legacy") seed["direct_subjects"]![0]!["target"] = "unknown_old_target";
            Complete(o, j, seed);
            if (mode is "settlement" or "description" or "embedded_id" or "unindexed_legacy")
            {
                Test.True(j.CountryAdmissionSeed != "", "supported place or descriptive legacy target is accepted: " + mode);
                Prepare(o, j); Complete(o, j, new() { ["approved"] = true, ["seed_digest"] = JObject.Parse(j.UserPrompt)["seed_digest"]!.DeepClone(), ["reason"] = "Controlled target semantics." });
                var direct = JsonConvert.DeserializeObject<DiplomacyAdmissionResult>(j.CountryAdmission)!.Input.Direct;
                Test.True(direct.Contains("empire_s") == (mode == "settlement"), "only exact supported target identity can add an owner: " + mode);
            }
            else Test.True(j.CountryAdmissionSeed == "" && j.CountryAdmission == "" && j.SuspendedReason.StartsWith("admission_seed_protocol_invalid:"), "invalid recipient source/identity/player authority cannot gain direct admission: " + mode);
        }
    }

    private static void FailedCapture()
    {
        var (h, o, j) = SeedOwner(true); h.ThrowRead = true; Freeze(o, j);
        Test.True(j.CountryAdmission == "" && j.SuspendedReason.StartsWith("admission_capture_failed:"), "read failure is technical, not neutral points");
        Test.True(o.ResolveRound(j.RoundId).Participants.Count == 0, "failed capture cannot partially adopt membership");
        for(int attempt=0;attempt<3;attempt++)
        {
            Call(o,"TryResumeTechnicalRequest",j,true);Prepare(o,j);
            Complete(o,j,new(){["approved"]=true,["seed_digest"]=JObject.Parse(j.UserPrompt)["seed_digest"]!.DeepClone(),["reason"]="Same previously verified seed; controlled retry."});
        }
        Test.True(o.ResolveRound(j.RoundId).State=="active"&&j.CountryAdmission==""&&!string.IsNullOrEmpty(j.CountryAdmissionSeed),"repeated native read failure retains seed and never rerolls into a lower-scoring event");
    }

    private static void SnapshotFailure()
    {
        var(h,o,j)=SeedOwner(true);Freeze(o,j);string valid=j.CountryAdmission;
        j.CountryAdmission="{\"Input\":null}";
        for(int attempt=0;attempt<2;attempt++)Prepare(o,j);
        Test.True(j.SuspendedReason.StartsWith("admission_snapshot_invalid:")&&o.ResolveRound(j.RoundId).State=="active"&&h.Captures==1,
            "damaged persisted snapshot is explicit technical failure, not a new seed or empty successful list");
        j.CountryAdmission=valid;Call(o,"TryResumeTechnicalRequest",j,true);Prepare(o,j);
        Test.True(j.CountryAdmission==valid&&h.Captures==1,"restoring exact snapshot resumes without live rescoring: captures=" + h.Captures + ";same=" + (j.CountryAdmission == valid) + ";stage=" + j.EventSelectionStage + ";reason=" + j.SuspendedReason);
        var (_, exhausted, work) = SeedOwner(true); Freeze(exhausted, work);
        work.CountryAdmission = "{\"Input\":null}";
        for (int attempt = 0; attempt < 3; attempt++) Prepare(exhausted, work);
        Test.True(exhausted.ResolveRound(work.RoundId).State == "closed"
            && (string)JObject.Parse(exhausted.ResolveRound(work.RoundId).EventSelectionEvidence)["admission"]! == work.CountryAdmission,
            "unrecoverable unpublished snapshot releases its slot while retaining exact corrupt evidence, never fabricating a successful empty snapshot");
    }

    private sealed class PlayerWorld(WorldDiplomacyOrchestration owner) : IWorldDiplomacyPlayerWorld
    {
        public WorldDiplomacyPlayerContext Player => new(1,"p",true,true,"p");
        public bool KingdomExists(string id)=> !string.IsNullOrEmpty(id);
        public WorldDiplomacyDocument ResolveDocument(string id)=>owner.ResolveDocument(id);
        public WorldDiplomacyRound ResolveRound(string id)=>owner.ResolveRound(id);
        public int CurrentDay()=>40;
    }

    private static void Entrances()
    {
        foreach(string mode in new[]{"player","contact","oral","direct","direct_war","direct_peace"})
        {
            bool direct=mode.StartsWith("direct",StringComparison.Ordinal);
            int directApplications=0;
            var h=new Host {PublishEnabled=true,PlayerAwareExecution=true,OralEffectPortsEnabled=true,OralRange=(0,3000),Extra=true};
            var o=new WorldDiplomacyOrchestration(h,new());h.Owner=o;
            WorldDiplomacyRound r;
            WorldDiplomacyDocument root=null!;
            WorldDiplomacyDialogueArrangement oral=null!;
            if(mode=="player")
            {
                WorldDiplomacyPlayerApplication.SubmitPlayerDocument(new PlayerWorld(o),"I condemn a's interference. All courts may hear this declaration.",o);
                root=o.CurrentStorage.Documents.Single();var analysis=o.CurrentStorage.Jobs.Single(x=>x.Kind=="analyze");
                o.CommitAnalysis(analysis,new JObject{["status"]="success",["title_summary"]="Interference",["addressed_kingdom_ids"]=new JArray("a","b","c"),
                    ["response_requests"]=new JArray(),["event_participation"]="participate",["tone"]="firm",["confidence"]=1,
                    ["actions"]=new JArray(new JObject{["intent"]="condemn",["target_kingdom_id"]="a",["commitment"]="non_binding",["requires_response"]=false})}.ToString());
                o.RemoveJob(analysis.JobId);r=o.ResolveRound(root.RoundId);
            }
            else if(mode=="contact")
            {
                r=(WorldDiplomacyRound)Call(o,"CreateIndependentDialogueRound","a","p","npc_player_agenda");
                Call(o,"CompletePlayerDecision",new WorldDiplomacyJob{JobId="contact-decision",RoundId=r.RoundId,Kind="player_decision",ForcedIntent="opening",AuthorKingdomId="a",TargetKingdomId="p"},new JObject{["participate"]=true,["reason"]="An actual recognition dispute"});
            }
            else
            {
                if(direct)
                {
                    var action=mode=="direct_war"?DialogueDiplomaticAction.DeclareWar:mode=="direct_peace"?DialogueDiplomaticAction.Peace:DialogueDiplomaticAction.Trade;
                    o.ExecuteBilateral("ruler_a","a",action,new("native","admission-direct","session","We agree on all terms.","I agree."),()=>
                    {directApplications++;h.WorldWar=mode=="direct_war";return new(){State="applied",Actor=mode=="direct_war"?"p":"a",Target=mode=="direct_war"?"a":"p",Terms=new()};});
                    Test.True(directApplications==1&&h.TradeExecutions==0,"direct action applied before drafting or event opening");
                }
                else
                {
                    o.SubmitOralDiplomaticCommitment("ruler_a","a","action=Trade;move=NewMatter;target=p;days=14",new DialogueInteractionOrigin("native","admission-oral","session","We agree on fourteen days of trade.","I agree to fourteen days."),out bool registered);
                    Test.True(registered,"legacy persisted oral entry registers exact confirmed terms");
                }
                oral=o.CurrentStorage.DialogueArrangements.Single();
                var declaration=o.CurrentStorage.Jobs.Single(x=>!string.IsNullOrEmpty(x.DialogueArrangementId));
                Prepare(o,declaration);Complete(o,declaration,new(){["safe"]=true,["public_context"]="The ruler confirms fourteen days of trade.",["attitude_summary"]="firm"});
                Prepare(o,declaration);
                o.CommitGeneratedDocument(declaration,OralDeclarationReplay.Draft(oral,direct?"statement":"accept_trade"));
                Prepare(o,declaration);Complete(o,declaration,new(){["safe"]=true,["agreement_consistent"]=true,["negotiation_consistent"]=true});
                r=o.ResolveRound(oral.RoundId);
                Test.True(h.TradeExecutions==0&&directApplications==(direct?1:0),"only legacy oral execution waits for original public opening");
            }
            Test.True(o.CurrentStorage.Jobs.Any(x=>x.Kind=="event_concept"),mode+": entry queues admission planning: "+JsonConvert.SerializeObject(o.CurrentStorage.Jobs.Select(x=>new{x.Kind,x.SuspendedReason,x.PrivacyRepairFeedback})));
            var j=o.CurrentStorage.Jobs.Single(x=>x.Kind=="event_concept");
            if(mode=="contact")
            {
                // Candidate quality has its own production replay; this fixture
                // resumes its selected short seed to isolate the actual entry.
                j.EventSelectionStage="expand";j.EventSelectedCandidate="{\"theme\":\"history\"}";j.EventComparison="{}";
            }
            FreezeFixture(o,j,new[]{"a","p"});
            var score=JsonConvert.DeserializeObject<DiplomacyAdmissionResult>(j.CountryAdmission)!;
            Test.True(score.Input.Direct.Count==2&&!score.Input.Direct.Contains("b")&&!score.Input.Direct.Contains("c"),mode+": broadcasts and mentions cannot become mandatory parties");
            Test.True(score.Admitted.Count==score.TargetCount&&score.Admitted.Contains("a")&&score.Admitted.Contains("p")&&score.Scores.All(x=>x.Country is "b" or "c"),mode+": exact sampled cast retains both direct parties, player not auto-selected");
            string primary=mode=="player"?root.DocumentId:"world:current";
            var keys=new JArray(primary,"lore:a");
            var concept=new JObject{["title"]="Recognition and cooperation",["background"]="The recorded issue brings the courts' recognition and cooperation claims into dispute.",["initiator"]=mode=="player"?"p":"a",
                ["material_keys"]=keys,["participants"]=new JArray(score.Admitted.Select(id=>new JObject{["kingdom"]=id,
                    ["agenda"]=id=="p"?"":"Recognition",["gain"]=id=="p"?"":"Review",["cost"]=id=="p"?"":"Concession",["red_line"]=id=="p"?"":"No confession",["alternative"]=id=="p"?"":"Unresolved",
                    ["source_keys"]=new JArray(primary),["address_to"]=id=="p"?new JArray():new JArray(id=="a"?"p":"a")})),
                ["narrative"]=new JArray(new JObject{["kind"]=mode=="player"?"public_fact":"allegation",["subject"]=mode=="player"?"p":"a",["source"]=primary,
                    ["text"]=mode=="player"?root.Body:"The court disputes recognition of the undertaking.",["hero_ids"]=new JArray()})};
            if (mode == "oral")
                for (int i = 0; i < 3; i++)
                {
                    string oldId = "prior-oral-case-" + i;
                    o.CurrentStorage.EventAnnouncements.Add(new() { EventId = oldId, Stage = "started", State = "public",
                        Title = "Earlier undertaking", Body = (string)concept["background"]!, Day = i });
                    o.CurrentStorage.CompletedRounds.Add(new() { RoundId = oldId, EventBackground = (string)concept["background"]!,
                        Participants = score.Admitted.Select(id => new WorldDiplomacyRoundParticipant { KingdomId = id }).ToList(), LastActivityDay = i });
                }
            Complete(o,j,concept);Test.True(j.Kind=="event_concept_audit",mode+": writer accepts full scored cast: "+j.SuspendedReason);
            Prepare(o,j);
            if (mode is "oral" or "player") Test.True(JObject.Parse(j.UserPrompt)["story_review_required"] == null,
                mode + ": confirmed matter uses factual continuity, never mandatory comparisons against three prior cases");
            Complete(o,j,MaterialCountryThirdReplay.Approved(j));
            Test.True(r.EventPhase=="opening"&&r.Participants.Count==score.TargetCount,mode+": full cast adopted before publication: "+j.SuspendedReason+" / "+j.PrivacySourceMaterial);
            Test.True(r.Participants.Single(x=>x.KingdomId=="p").Agenda=="",mode+": no invented player position");
            if(mode!="player")Test.True(r.Participants.Single(x=>x.KingdomId=="p").State=="observer",mode+": no player consent inferred");
            if(mode=="oral" || direct)
            {
                Reviewed(o,o.CurrentStorage.Jobs.Single(x=>x.Kind=="event_boundary"),Boundary());
                Test.True(oral.Terms.DurationDays==(direct?0:14)&&oral.TargetKingdomId=="p"&&oral.ActorKingdomId=="a","admission preserves oral terms and signatories");
                Test.True(oral.Status=="executed"&&h.TradeExecutions==(direct?0:1),"scored third countries do not add a new veto or replay: "+oral.Status+":"+oral.Reason);
                Test.True(o.CurrentStorage.Documents.Count(x=>x.DocumentId==oral.DocumentId&&x.IsReadyForPublication)==1,"original oral declaration published once");
                Call(o,"MaintainDialogueArrangements");Call(o,"MaintainDialogueArrangements");
                Test.True(h.TradeExecutions==(direct?0:1)&&!o.CurrentStorage.Jobs.Any(x=>x.Kind=="generate"&&x.AuthorKingdomId=="a"&&!x.IsResponse),"oral recovery cannot duplicate execution or unconditional opening speech");
                if(direct)
                {
                    var resultDocument=o.ResolveDocument(oral.DocumentId);
                    Test.True(resultDocument.Intent=="statement"&&!resultDocument.RequiresResponse
                        && !r.PendingOffers.Any(x=>x.Status=="open"&&x.SourceDocumentId==oral.DocumentId)
                        && !r.PlayerResponses.Any(x=>x.Status=="pending"&&x.SourceDocumentId==oral.DocumentId),"direct opening creates no original-agreement acceptance obligation");
                    Test.True(j.EventTriggerMaterial.Contains("\"already_executed\":true")&&j.EventTriggerMaterial.Contains("DirectActorKingdomId"),"actual executed source enters event planning");
                    Test.True(r.EventConvergence.Result==(mode=="direct_war"?"war_executed":"action_executed")
                        && r.EventConvergence.ActualWar==(mode=="direct_war"),"convergence reads the executed result independently of statement intent "+mode);
                    var evidence=(JObject)Call(o,"DirectDocumentEvidence",resultDocument);
                    Test.True((string)evidence["actor"]==(mode=="direct_war"?"p":"a")&&(string)evidence["announcement_author"]=="a","result actor remains separate from NPC author "+mode);
                    for(int step=0;step<20&&r.EventPhase!="closing";step++)
                    {
                        foreach(var speech in o.CurrentStorage.Jobs.Where(x=>x.Kind=="generate").ToList())
                            EventStructureReplay.PublishDeclaration(o,speech,speech.TargetKingdomId,"statement",false);
                        h.CurrentDayValue+=8;o.ProcessPropagationArrivals();o.ProcessRoundLifecycle();
                        foreach(var decision in o.CurrentStorage.Jobs.Where(x=>x.Kind=="player_decision").ToList())
                            Complete(o,decision,new(){["participate"]=false,["reason"]="No new optional matter."});
                    }
                    Test.True(r.EventPhase=="closing","direct result with additional participant reactions reaches closing "+mode);
                    var ending=o.CurrentStorage.Jobs.Single(x=>x.Kind=="event_boundary");Prepare(o,ending);
                    Test.True(ending.UserPrompt.Contains("executed_oral_result")&&ending.UserPrompt.Contains(oral.ArrangementId),"actual result reaches ending evidence "+mode);
                    var conclusion=Boundary();conclusion["confirmed_documents"]=new JArray(oral.DocumentId);
                    Reviewed(o,ending,conclusion);
                    Test.True(r.State=="closed"&&r.EventEndingId!=""&&directApplications==1&&h.TradeExecutions==0,"direct result closes with confirmed receipt without replay "+mode);
                }
            }
        }
    }
}
