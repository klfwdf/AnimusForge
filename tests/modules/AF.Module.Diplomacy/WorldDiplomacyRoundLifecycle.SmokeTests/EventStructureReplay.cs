using AnimusForge;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Actual orchestration entry, request composition, completions, document execution,
// propagation and hourly lifecycle. Only game/transport leaves are simulated.
internal static class EventStructureReplay
{
    internal class Host : ConcurrentOralMigrationReplay.Host, IDiplomacyEventHost, IDiplomacyEventMaterialHost, IDiplomacyNarrativeActorHost, IDiplomacyCountryAdmissionHost
    {
        public virtual int AdmissionCountryCountPreference => 1;
        public virtual string AdmissionSubjectCountry(string kind, string id) => kind == "kingdom" ? id
            : kind == "hero" && id.StartsWith("ruler_", StringComparison.Ordinal) ? id.Substring(6) : id == "ordinary_lord" ? "b" : null;
        public virtual DiplomacyAdmissionInput CaptureCountryAdmission(IReadOnlyCollection<string> direct, IReadOnlyCollection<string> heroes, IReadOnlyCollection<string> candidates)
            => new() { Direct = direct.ToList(), Heroes = heroes.ToDictionary(x => x, x => AdmissionSubjectCountry("hero", x)) };
        public IReadOnlyDictionary<string,string> CaptureNarrativeActors(IReadOnlyCollection<string> ids) => new Dictionary<string,string> { ["ordinary_lord"] = "ordinary lord" };
        internal int Writes;
        internal int MaterialReads;
        internal string MaterialOverride;
        internal string UnavailableParty;
        internal string AffiliatedParty;
        public override bool IsPlayerAffiliatedParty(string id) => id == AffiliatedParty || base.IsPlayerAffiliatedParty(id);
        internal float Relation, Loyalty = 50;
        public long EventGeneration => 1;
        public virtual string CaptureEventWorld(IReadOnlyCollection<string> ids) => JsonConvert.SerializeObject(ids);
        public string CaptureEventSources(IReadOnlyCollection<string> ids, IReadOnlyCollection<string> excluded, int searchPass = 0, string topic = "")
        {
            MaterialReads++;
            if (MaterialOverride != null) return MaterialOverride;
            var rows = JArray.Parse("[{\"key\":\"lore:succession\",\"source_kind\":\"world_book_account\",\"known_by\":\"b\",\"text\":\"Two courts dispute an old succession oath\"},"
                + "{\"key\":\"lore:envoy\",\"source_kind\":\"world_book_account\",\"known_by\":\"c\",\"text\":\"Rival testimony about an insult to an envoy\"}]");
            foreach (string id in ids.Where(id => !rows.OfType<JObject>().Any(x => (string)x["known_by"] == id)))
                rows.Add(new JObject { ["key"] = "persona:" + id, ["known_by"] = id, ["source_kind"] = "character_setting",
                    ["text"] = "Controlled court setting for " + id, ["hero_ids"] = new JArray("ruler_" + id) });
            return rows.ToString();
        }
        public bool TryReadEventEffect(DiplomacyEventEffectState e, out float v, out float min, out float max)
        { v = e.Kind == "ruler_relation" ? Relation : Loyalty; min = e.Kind == "ruler_relation" ? -100 : 0; max = 100; return true; }
        public void ApplyEventEffectDelta(DiplomacyEventEffectState e, float delta) { if (e.Kind == "ruler_relation") Relation += delta; else Loyalty += delta; Writes++; }
        public bool ShowEvent(DiplomacyEventPresentation data, out string image, Action ack, IReadOnlyList<string> recent)
        { image = ""; return false; }
        public override string ResolveKingdomIdOrNull(string id) => string.IsNullOrWhiteSpace(id) ? null : id;
        public override string ResolveKingdomNameOrEmpty(string id) => id;
        public override bool HasIndependentAuthority(string id) => id != UnavailableParty && base.HasIndependentAuthority(id);
        public override int MaxDiplomaticActionsPerDocument() => 8;

        public override IWorldDiplomacyJobPreparationPort JobPreparation() => new Preparation(this);
        public override IWorldDiplomacyPeaceAdmissionPort PeaceAdmission() => new PeaceAdmissionReplay.Port { War = WorldWar };
    }
    private sealed class Preparation : IWorldDiplomacyJobPreparationPort
    {
        private readonly Host h;
        internal Preparation(Host value) { h = value; }
        public WorldDiplomacyStorage Storage => h.Owner.CurrentStorage;
        public int GenerationMaxTokens => 3000;
        public int AnalysisMaxTokens => 3000;
        public (int minimum, int maximum) CharacterRange() => (0,3000);
        public bool KingdomExists(string id) => !string.IsNullOrEmpty(id);
        public WorldDiplomacyRound ResolveRound(string id) => h.Owner.ResolveRound(id);
        public WorldDiplomacyDocument ResolveDocument(string id) => h.Owner.ResolveDocument(id);
        public string CommonContract(WorldDiplomacyRound r) => "contract";
        public bool TryBuildProfile(string id, string marker, out string prompt) { prompt = marker; return true; }
        public void LogProfile(WorldDiplomacyJob j, string prompt) { }
    }
    private static JObject Position(string id) => new() { ["kingdom"] = id, ["agenda"] = id == "p" ? "" : "recognition",
        ["gain"] = id == "p" ? "" : "legitimate claim", ["cost"] = id == "p" ? "" : "concession of precedence",
        ["red_line"] = id == "p" ? "" : "no forged confession", ["alternative"] = id == "p" ? "" : "leave dispute unresolved" };
    private static JObject Concept(string first, string second, string key) => new() {
        ["title"] = key == "lore:succession" ? "The disputed oath" : "The envoy testimony",
        ["background"] = key == "lore:succession" ? "The renewal of a succession oath revives competing historical claims."
            : "New testimony contradicts the courts' accounts of an insult to an envoy.",
        ["initiator"] = first, ["material_keys"] = new JArray(key),
        ["participants"] = new JArray(Position(first), Position(second)),
        ["narrative"] = new JArray(new JObject { ["kind"] = "allegation", ["subject"] = first, ["source"] = key, ["text"] = "The accounts are disputed." }) };
    private static JObject Boundary(string first, string second, string title) => new() {
        ["title"] = title, ["body"] = "The courts acknowledge their unresolved disagreement.", ["scene"] = "Negotiation",
        ["effects"] = new JArray(new JObject { ["kind"] = "ruler_relation", ["kingdom"] = first,
            ["other"] = second, ["delta"] = -1, ["days"] = 0, ["reason"] = "The dispute strains their relations." }),
        ["remove_effects"] = new JArray(), ["confirmed_documents"] = new JArray(), ["narrative"] = new JArray() };
    private static void BackgroundCountryScope()
    {
        foreach (bool thirdCountry in new[] { false,true })
        {
            var h=new Host { PublishEnabled=true };var o=new WorldDiplomacyOrchestration(h,new WorldDiplomacyRuntimeState());h.Owner=o;
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var r=(WorldDiplomacyRound)typeof(WorldDiplomacyOrchestration).GetMethod("CreateIndependentDialogueRound",flags)!
                .Invoke(o,new object[]{"a","p","npc_player_agenda"})!;
            var decision=new WorldDiplomacyJob { JobId="opening-decision",RoundId=r.RoundId,Kind="player_decision",ForcedIntent="opening",AuthorKingdomId="a",TargetKingdomId="p" };
            typeof(WorldDiplomacyOrchestration).GetMethod("CompletePlayerDecision",flags)!.Invoke(o,new object[]{decision,new JObject{["participate"]=true,["reason"]="Current independent interests"}});
            var j=Job(o,"event_concept");
            Test.True(j.CandidateKingdomIds.Contains("b")&&j.CandidateKingdomIds.Contains("c"),"NPC-to-player opening researches additional current countries");
            var concept=Concept("a","p","lore:envoy");
            // This source is known by c, so two-party case uses a public world source.
            concept["material_keys"]=new JArray("world:current");concept["narrative"][0]["source"]="world:current";
            if(thirdCountry)((JArray)concept["participants"]).Add(Position("c"));
            SelectConcept(o,j,concept);
            foreach(var row in ((JArray)concept["participants"]).OfType<JObject>()) row["source_keys"]=new JArray("world:current");
            Complete(o,j,concept);o.PrepareSharedRequest(j);Complete(o,j,MaterialCountryThirdReplay.Approved(j));
            Test.True(r.EventPhase=="opening"&&r.Participants.Count==(thirdCountry?3:2),"audited background admits precisely justified two/three countries");
            Test.True(r.Participants.Single(x=>x.KingdomId=="p").State=="observer"&&o.CurrentStorage.NpcPlayerContactCount==0,
                "player background presence is neither consent nor published contact");
        }
    }
    private static void Complete(WorldDiplomacyOrchestration o, WorldDiplomacyJob j, JObject body)
    {
        o.PrepareSharedRequest(j);
        Test.True(o.TryCompleteDiplomacyPipeline(j, new LlmJobResult { JobId = j.JobId, Success = true, Content = body.ToString() }), "production pipeline consumes " + j.Kind);
    }
    private static void Audited(WorldDiplomacyOrchestration o, WorldDiplomacyJob j, JObject body)
    {
        if (j.Kind == "event_concept") SelectConcept(o, j, body);
        if (j.Kind == "event_progress" && (string)body["outcome"] is not "continue" and not "wait")
        {
            var round=o.ResolveRound(j.RoundId);
            body["closure"]=new JArray(round.Participants.Where(p=>p.State=="active"||!string.IsNullOrEmpty(p.Agenda))
                .Select(p=>new JObject{["kingdom"]=p.KingdomId,["status"]=(string)body["outcome"]=="resolved"?"satisfied":"unresolved",
                    ["reason"]=body["reason"]!.DeepClone(),["sources"]=new JArray(round.EventBeginningId)}));
        }
        Complete(o,j,body);
        if (j.Kind=="event_concept" && (!string.IsNullOrEmpty(j.SuspendedReason) || j.EventSelectionStage is "candidates" or "compare")) return;
        o.PrepareSharedRequest(j); var audit=MaterialCountryThirdReplay.Approved(j);audit["closure_consistent"]=true;Complete(o,j,audit);
    }
    private static void SelectConcept(WorldDiplomacyOrchestration o, WorldDiplomacyJob j, JObject concept)
    {
        // Simulated model payloads exercise production transitions, not model quality.
        o.PrepareSharedRequest(j);
        Complete(o, j, new JObject { ["candidates"] = new JArray(new JObject {
            ["id"] = "test-candidate", ["material_keys"] = concept["material_keys"].DeepClone(),
            ["kingdoms"] = new JArray(((JArray)concept["participants"]).Select(x => (string)x["kingdom"])),
            ["conflict"] = "Rival interpretations of a recorded oath", ["motive"] = "A public renewal makes recognition relevant",
            ["new_development"] = "The current renewal", ["theme"]="history", ["theme_reason"]="Conflicting historical accounts motivate the current demand for recognition", ["npc_creation"] = "No additional creation",
            ["negotiation_space"] = "Recognition and precedence can be negotiated" }) });
        Complete(o, j, new JObject { ["assessments"] = new JArray(new JObject { ["id"] = "test-candidate", ["qualified"] = true,
            ["duplicate_of"] = "",
            ["strength"] = "Concrete rival claims", ["weakness"] = "Witness disagreement", ["novelty_reason"] = "New renewal", ["urgency_reason"] = "No immediate deadline" }),
            ["best_equivalent"] = new JArray("test-candidate"), ["urgent"] = new JArray(), ["comparison_reason"] = "Only qualifying candidate" });
        CountryAdmissionReplay.FreezeFixture(o, j, ((JArray)concept["participants"]).Select(x => (string)x["kingdom"]));
        foreach (var p in ((JArray)concept["participants"]).OfType<JObject>())
        { p["source_keys"] = concept["material_keys"].DeepClone(); p["address_to"] = (string)p["kingdom"] == "p" ? new JArray()
            : new JArray(((JArray)concept["participants"]).Where(x => (string)x["kingdom"] != (string)p["kingdom"]).Select(x => (string)x["kingdom"])); }
        concept["party_review"] = new JArray(j.CandidateKingdomIds.Select(id => new JObject { ["kingdom"] = id,
            ["included"] = ((JArray)concept["participants"]).Any(x => (string)x["kingdom"] == id), ["reason"] = "Source-based inclusion or absence of a stake" }));
    }
    private static WorldDiplomacyJob Job(WorldDiplomacyOrchestration o, string kind) => o.CurrentStorage.Jobs.Single(x => x.Kind == kind);
    internal static void PublishDeclaration(WorldDiplomacyOrchestration o, WorldDiplomacyJob generation, string target, string intent, bool requiresResponse,
        string offerDocument = null, string offerAction = null, bool includeAlliance = false)
    {
        o.PrepareSharedRequest(generation);
        string context = generation.UserPrompt;
        Complete(o,generation,new JObject { ["safe"] = true, ["public_context"] = context, ["attitude_summary"] = "" });
        o.PrepareSharedRequest(generation);
        var declaration = new JObject { ["title"] = "Position", ["body"] = intent == "ultimatum"
                ? "We demand recognition of our claim. We await your position without promising a specific subsequent action."
                : intent == "declare_war" ? "We declare war upon the addressed kingdom with immediate effect."
                : intent == "propose_trade" ? "We propose a trade agreement between our kingdoms. Consider these terms without assuming agreement."
                : intent == "accept_trade" ? "We accept the exact original trade proposal without amendments. This agreement addresses the practical cooperation under discussion."
                : intent == "withdraw_offer" ? "We withdraw our original unaccepted trade proposal. No trade agreement was established."
                : "We maintain our account of the dispute. We neither accept the opposing claim nor promise another action.",
            ["actions"] = new JArray(new JObject { ["target_kingdom_id"] = target, ["intent"] = intent,
                ["commitment"] = WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent(intent), ["negotiation_move"] = "state_position" }),
            ["mentioned_kingdom_ids"] = new JArray(target), ["requires_response"] = requiresResponse,
            ["round_participation"] = "continue", ["round_status"] = "continue", ["tone"] = "firm",
            ["response_requests"] = new JArray() };
        if (includeAlliance)
        {
            ((JArray)declaration["actions"]).Add(new JObject { ["target_kingdom_id"] = target, ["intent"] = "propose_alliance", ["commitment"] = "proposal" });
            declaration["body"] = (string)declaration["body"] + " We also propose an alliance of non-aggression and cooperation. Participation in each particular war must be agreed separately; any explicitly promised military assistance will be fulfilled.";
        }
        if (offerDocument != null)
        {
            declaration["actions"][0]["responding_to_offer_document_id"] = offerDocument;
            declaration["actions"][0]["responding_to_offer_action_id"] = offerAction;
        }
        declaration["body"] = (string)declaration["body"] + " We seek recognition through review, without confessing to forgery; otherwise the claim remains unresolved.";
        declaration["answered_player_document_ids"] = new JArray(generation.PlayerResponseSourceIds);
        o.CommitGeneratedDocument(generation,declaration.ToString());
        var checkedEnvelope = (JObject)declaration.DeepClone();
        WorldDiplomacyEnvelopeJsonRules.NormalizeGeneratedDiplomaticEnvelopeShape(generation, checkedEnvelope);
        Test.True(!o.TryGetGeneratedIntentLegalityViolation(generation, checkedEnvelope, generation.AuthorKingdomId, target, out _, out var invalidReason),
            "actual generated declaration passes common validation: " + invalidReason);
        var answers = o.ResolveRound(generation.RoundId).Participants.Where(x => x.KingdomId != generation.AuthorKingdomId
            && !string.IsNullOrEmpty(x.AgendaExpressionDocumentId)
            && x.AgendaExpressionDocumentId == generation.SourceDocumentId && x.AgendaAddressTo.Contains(generation.AuthorKingdomId));
        Complete(o,generation,new JObject { ["safe"] = true, ["position_owner_valid"]=true,["negotiation_consistent"] = true,
            ["response_continuity"] = MaterialCountryThirdReplay.ContinuityReview(o,generation),
            ["agenda_quote"] = (string)declaration["body"], ["agenda_answers"] = new JArray(answers.Select(x => new JObject {
                ["source"] = x.AgendaExpressionDocumentId, ["quote"] = (string)declaration["body"] })) });
    }

    private static void DeclarationAdmission()
    {
        var h = new Host { PublishEnabled = true, OralRange = (0,3000) };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        var r = o.EnsureActiveRound("a", "b", false);
        r.ResultSettlementPending = true; r.HardEndDay = 0; r.RelayWaiting = true;
        r.RelayRouteKingdomIds = new() { "unrelated" };
        var source = new WorldDiplomacyDocument { DocumentId = "public-source", RoundId = r.RoundId,
            AuthorKingdomId = "a", TargetKingdomId = "b", IsReadyForPublication = true };
        o.AddDocument(source);
        o.EnqueueGeneration("b", "a", null, true, source, 80, roundId: r.RoundId);
        Test.True(o.CurrentStorage.Jobs.Count == 0, "undelivered declaration cannot become the source of an event response");
        WorldDiplomacyPropagationApplication.ReceiveCourt(o.CurrentStorage, source, "b", h.CurrentDayValue, () => false, () => { });
        o.EnqueueGeneration("b", "a", null, true, source, 80, roundId: r.RoundId);
        Test.True(o.CurrentStorage.Jobs.Count == 0, "knowledge alone cannot promote an observer into a speaking participant");
        r.Participants.Single(x => x.KingdomId == "b").State = "active";
        o.EnqueueGeneration("b", "missing", null, true, source, 80, roundId: r.RoundId);
        Test.True(o.CurrentStorage.Jobs.Count == 0, "an unavailable intended target cannot be silently replaced with a different country");
        o.EnqueueGeneration("b", "a", null, true, source, 80, roundId: r.RoundId);
        var job = Job(o,"generate");
        Test.True(job.TargetKingdomId == "a" && job.RequestLane == r.BirthLane && o.CanDispatchDiplomacyJob(job),
            "delivered active-participant response ignores legacy route, seat and hard-end fields");
        o.EnqueueGeneration("b", "a", null, true, source, 80, roundId: r.RoundId);
        Test.True(o.CurrentStorage.Jobs.Count == 1, "reconciliation coalesces the same event author and source");
        o.CommitFailedJob(job,"transport_unavailable");
        h.CurrentDayValue += 30; o.ProcessRoundLifecycle();
        Test.True(o.CurrentStorage.Jobs.Contains(job) && job.SuspendedReason == "transport_unavailable"
            && r.State == "active" && r.EventEndingId == "", "ordinary technical failure retains work and cannot become a political conclusion");

        var h2 = new Host { PublishEnabled = true, OralRange = (0,3000) };
        var o2 = new WorldDiplomacyOrchestration(h2, new WorldDiplomacyRuntimeState()); h2.Owner = o2;
        var followup = o2.EnsureActiveRound("a", "b", false);
        followup.Participants.Single(x => x.KingdomId == "b").State = "active";
        var old = new WorldDiplomacyDocument { DocumentId = "old-source", RoundId = "closed-original", AuthorKingdomId = "a",
            TargetKingdomId = "b", IsReadyForPublication = true, IsPlayerAuthored = true };
        o2.AddDocument(old);
        WorldDiplomacyPropagationApplication.ReceiveCourt(o2.CurrentStorage, old, "b", h2.CurrentDayValue, () => false, () => { });
        o2.EnqueueGeneration("b", "a", null, true, old, 80, roundId: followup.RoundId);
        Test.True(o2.CurrentStorage.Jobs.Count == 0, "another event's known declaration alone does not authorize a response in this event");
        followup.PlayerResponses.Add(new() { KingdomId = "b", SourceDocumentId = old.DocumentId, OriginalRoundId = old.RoundId });
        o2.EnqueueGeneration("b", "a", null, true, old, 80, roundId: followup.RoundId, externalResponseOnly: true);
        Test.True(Job(o2,"generate").SourceDocumentId == old.DocumentId, "exact carried response obligation retains its original source across legacy recovery");
    }

    private static void MaterialAdmission()
    {
        foreach (string invalid in new[] { "invented-key", "unknown-knower", "unbound-narrative" })
        {
            var h = new Host();
            var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
            o.TryScheduleNormalRound();
            var job = Job(o, "event_concept");
            o.PrepareSharedRequest(job);
            var concept = Concept("b", "a", "lore:succession");
            SelectConcept(o, job, concept);
            o.PrepareSharedRequest(job);
            int capturedReads = h.MaterialReads;
            string frozen = new JArray(JArray.Parse(job.EventMaterialSnapshot).OrderBy(x => (string)x["key"],StringComparer.Ordinal)).ToString();
            if (invalid == "invented-key") concept["material_keys"] = new JArray("lore:invented");
            if (invalid == "unknown-knower") concept["material_keys"] = new JArray("lore:envoy");
            if (invalid == "unbound-narrative") concept["narrative"][0]["source"] = "unlisted testimony";
            Complete(o, job, concept);
            h.MaterialOverride = "[]";
            o.PrepareSharedRequest(job);
            Test.True(h.MaterialReads == capturedReads
                && new JArray(JArray.Parse(job.EventMaterialSnapshot).OrderBy(x => (string)x["key"],StringComparer.Ordinal)).ToString() == frozen
                && job.UserPrompt.Contains("lore:succession"),
                "correction/audit reuses complete frozen records after bounded selection-time supplementation");
            Test.True(JsonConvert.DeserializeObject<WorldDiplomacyJob>(JsonConvert.SerializeObject(job))!.EventMaterialSnapshot == job.EventMaterialSnapshot,
                "evidence snapshot survives persisted job reconstruction");
            Test.True(job.Kind=="event_concept" && job.SuspendedReason.Length>0,"invalid evidence is rejected before semantic audit");
            job.Kind="event_concept_audit"; // A stale/legacy approved review must still face final validation.
            o.PrepareSharedRequest(job);Complete(o, job, MaterialCountryThirdReplay.Approved(job));
            Test.True(job.SuspendedReason.Length > 0 && o.CurrentStorage.ActiveRound.EventBackground == "" && h.Writes == 0,
                "semantic approval cannot authorize an unknown or unbound material source: " + invalid);
        }
        var boundedHost = new Host { MaterialOverride = new JArray(Enumerable.Range(0, 100).Select(i => new JObject {
            ["key"] = "lore:" + i, ["known_by"] = "b", ["text"] = new string('x', 3000) })).ToString() };
        var bounded = new WorldDiplomacyOrchestration(boundedHost, new WorldDiplomacyRuntimeState()); boundedHost.Owner = bounded;
        bounded.TryScheduleNormalRound();
        var boundedJob = Job(bounded, "event_concept"); bounded.PrepareSharedRequest(boundedJob);
        var evidence = JArray.Parse(boundedJob.EventMaterialSnapshot);
        Test.True(evidence.Count <= 32 && evidence.Sum(x => ((string)x["text"])!.Length) <= 48000
            && evidence.All(x => !(bool)x["excerpt"]!) && evidence.Where(x => ((string)x["key"]).StartsWith("lore:")).All(x => ((string)x["text"]).Length == 3000),
            "request-boundary evidence has a total budget and retains whole source records");
    }

    private static void LostParticipantAuthority(bool failBeforeLoss = false, bool uncertainEffect = false)
    {
        var h = new Host { PublishEnabled = true, PlayerAwareExecution = true, OralEffectPortsEnabled = true, OralRange = (0,3000) };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        o.TryScheduleNormalRound();
        Audited(o, Job(o,"event_concept"), Concept("b", "a", "lore:succession"));
        var round = o.CurrentStorage.ActiveRound;
        Audited(o, Job(o,"event_boundary"), Boundary("b", "a", round.RoundTopic));
        PublishDeclaration(o, Job(o,"generate"), "a", "statement", true);
        h.CurrentDayValue += 8; o.ProcessPropagationArrivals(); o.ProcessRoundLifecycle();
        Test.True(o.CurrentStorage.Jobs.Any(x => x.AuthorKingdomId == "a" && x.Kind == "generate"), "actual delivery queues the requested response before authority loss: " + JsonConvert.SerializeObject(new { round.PlayerResponses,round.PlayerCourtWork,jobs=o.CurrentStorage.Jobs.Select(x=>new{x.Kind,x.AuthorKingdomId,x.SuspendedReason}),docs=o.CurrentStorage.Documents.Select(x=>new{x.DocumentId,x.IsReadyForPublication,x.RequiresResponse,x.ResponseRequests}) }));
        if (failBeforeLoss)
        {
            var reply = o.CurrentStorage.Jobs.Single(x => x.AuthorKingdomId == "a" && x.Kind == "generate");
            o.CommitFailedJob(reply, "temporary_transport_failure");
            Test.True(round.TechnicalHoldReason == "temporary_transport_failure", "failed uncommitted reply owns a technical hold before authority loss");
        }
        if (uncertainEffect) o.CurrentStorage.EventEffects.Add(new() { Id = "unconfirmed-effect", EventId = round.RoundId, Status = "uncertain" });
        h.UnavailableParty = "a"; h.CurrentDayValue++; o.ProcessRoundLifecycle();
        Test.True(round.Participants.Single(x => x.KingdomId == "a").State == "unavailable"
            && round.PlayerCourtWork.All(x => x.KingdomId != "a" || x.Status == "unavailable")
            && !o.CurrentStorage.Jobs.Any(x => x.AuthorKingdomId == "a" && x.Kind == "generate"),
            "loss of independent authority retires only uncommitted reply work and records objective unavailability");
        foreach (var decision in o.CurrentStorage.Jobs.Where(x => x.Kind == "player_decision").ToList())
            Complete(o, decision, new JObject { ["participate"] = false, ["reason"] = "No stake." });
        h.CurrentDayValue++; o.ProcessRoundLifecycle();
        if (uncertainEffect)
            Test.True(round.TechnicalHoldReason == "temporary_transport_failure" && !o.CurrentStorage.Jobs.Any(x => x.Kind == "event_progress"),
                "retiring an unavailable reply cannot erase a remaining uncertain effect hold");
        else Test.True(o.CurrentStorage.Jobs.Any(x => x.Kind == "event_progress") && round.State == "active"
            && o.CurrentStorage.Documents.Count == 1, "unavailable reply no longer blocks natural progress and creates no fictional answer or closure");
    }

    private static void ExactGeneratedOfferBinding()
    {
        var h = new Host { PublishEnabled = true };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        var round = o.EnsureActiveRound("a", "b", false);
        round.Participants.Single(x => x.KingdomId == "b").State = "active";
        foreach (string id in new[] { "older", "chosen" })
        {
            var document = new WorldDiplomacyDocument { DocumentId = id, RoundId = round.RoundId,
                AuthorKingdomId = "b", TargetKingdomId = "a", Intent = "propose_trade", Commitment = "proposal", IsReadyForPublication = true,
                Actions = new() { new() { ActionId = "terms", TargetKingdomId = "a", Intent = "propose_trade", Commitment = "proposal" } } };
            o.AddDocument(document);
            round.PendingOffers.Add(new() { SourceDocumentId = id, SourceActionId = "terms", ProposerKingdomId = "b",
                TargetKingdomId = "a", Intent = "propose_trade", Status = "open" });
        }
        var job = new WorldDiplomacyJob { Kind = "generate", AuthorKingdomId = "a", TargetKingdomId = "b", RoundId = round.RoundId };
        JObject Envelope(string source, string action) => new() { ["title"] = "Trade agreement", ["body"] = "We accept your proposed trade terms.",
            ["actions"] = new JArray(new JObject { ["target_kingdom_id"] = "b", ["intent"] = "accept_trade", ["commitment"] = "acceptance",
                ["responding_to_offer_document_id"] = source, ["responding_to_offer_action_id"] = action }), ["mentioned_kingdom_ids"] = new JArray("b") };
        bool Invalid(JObject json, out string reason)
        {
            WorldDiplomacyEnvelopeJsonRules.NormalizeGeneratedDiplomaticEnvelopeShape(job, json);
            return o.TryGetGeneratedIntentLegalityViolation(job, json, "a", "b", out _, out reason);
        }
        Test.True(Invalid(Envelope("chosen", "terms"), out var reason) && reason.Contains("not_known"), "a valid but undelivered offer cannot be accepted by generated speech");
        WorldDiplomacyPropagationApplication.ReceiveCourt(o.CurrentStorage, o.ResolveDocument("chosen"), "a", h.CurrentDayValue, () => false, () => { });
        var exact = Envelope("chosen", "terms");
        Test.True(!Invalid(exact, out reason) && (string)exact["actions"][0]["responding_to_offer_document_id"] == "chosen",
            "exact delivered source remains valid among multiple original offers: " + reason);
        Test.True(Invalid(Envelope("missing", "terms"), out _) && Invalid(Envelope("chosen", "other-action"), out _), "invalid explicit source or action is never silently replaced");
        Test.True(Invalid(Envelope("", ""), out _), "ambiguous omitted source cannot choose an arbitrary offer");
        Test.True(round.PendingOffers.All(x => x.Status == "open") && h.TradeExecutions == 0, "validation neither consumes another offer nor executes acceptance");
        var mismatch = Envelope("chosen", "terms");
        mismatch["actions"][0]["commitment"] = "proposal";
        Test.True(Invalid(mismatch, out reason) && reason.Contains("commitment_mismatch"), "explicit proposal commitment cannot be rewritten to acceptance");
        h.WorldWar = true; h.OralEffectPortsEnabled = true;
        var peace = o.ResolveDocument("chosen");
        peace.Intent = "propose_peace"; peace.Actions[0].Intent = "propose_peace";
        var original = new WorldDiplomacyPeaceTerms { TributePayerKingdomId = "b", TributeReceiverKingdomId = "a", DailyTribute = 100, DurationDays = 30 };
        peace.PeaceTerms = original; peace.Actions[0].PeaceTerms = original;
        round.PendingOffers.Single(x => x.SourceDocumentId == "chosen").Intent = "propose_peace";
        JObject Peace(JObject terms)
        {
            var result = Envelope("chosen", "terms"); result["actions"][0]["intent"] = "accept_peace";
            result["body"] = "We accept the original peace proposal.";
            if (terms != null) result["actions"][0]["peace_terms"] = terms;
            return result;
        }
        var amended = WorldDiplomacyEnvelopeJsonRules.BuildPeaceTermsJson(original); amended["daily_tribute"] = 50;
        Test.True(Invalid(Peace(amended), out reason) && reason.Contains("changes_offer_terms"), "acceptance with lower tribute is rejected before original terms can overwrite it");
        Test.True(Invalid(Peace(WorldDiplomacyEnvelopeJsonRules.BuildPeaceTermsJson(null)), out reason) && reason.Contains("changes_offer_terms"), "explicit removal of all original obligations is an amendment");
        var implicitOriginal = Peace(null);
        Test.True(!Invalid(implicitOriginal, out reason) && (int)implicitOriginal["actions"][0]["peace_terms"]["daily_tribute"] == 100,
            "acceptance with no restated terms binds the exact source: " + reason);
        var nullOriginal = Peace(null); nullOriginal["actions"][0]["peace_terms"] = JValue.CreateNull();
        Test.True(!Invalid(nullOriginal, out reason) && (int)nullOriginal["actions"][0]["peace_terms"]["daily_tribute"] == 100,
            "JSON null inherits exact original obligations like omitted terms: " + reason);
        Test.True(!Invalid(Peace(WorldDiplomacyEnvelopeJsonRules.BuildPeaceTermsJson(original)), out reason), "identical explicit peace terms remain acceptable: " + reason);
        h.WorldWar = false;
        peace.Intent = "propose_vassal"; peace.Actions[0].Intent = "propose_vassal";
        peace.TreatyTerms = new() { ReceivingKingdomId = "b", JoiningKingdomId = "a" }; peace.Actions[0].TreatyTerms = peace.TreatyTerms;
        round.PendingOffers.Single(x => x.SourceDocumentId == "chosen").Intent = "propose_vassal";
        JObject Vassal(string receiving, string joining)
        {
            var result = Envelope("chosen", "terms"); result["actions"][0]["intent"] = "accept_vassal";
            result["actions"][0]["treaty_terms"] = new JObject { ["receiving_kingdom_id"] = receiving, ["joining_kingdom_id"] = joining };
            return result;
        }
        Test.True(Invalid(Vassal("a", "b"), out reason) && reason.Contains("changes_source_terms"), "generated acceptance cannot reverse original treaty roles");
        var identical = Vassal("b", "a");
        Test.True(!Invalid(identical, out reason) && (string)identical["actions"][0]["treaty_terms"]["joining_kingdom_id"] == "a", "formal treaty roles survive per-action validation: " + reason);
        var materialized = new WorldDiplomacyDocument { AuthorKingdomId = "a", RoundId = round.RoundId };
        Test.True(o.TryApplyGeneratedSemanticEnvelope(materialized, identical, "a", "b", false, false)
            && materialized.Actions.Single().TreatyTerms?.ReceivingKingdomId == "b"
            && materialized.Actions.Single().TreatyTerms?.JoiningKingdomId == "a", "validated treaty roles reach the actual per-action execution record");
        var trade = Envelope("older", "terms"); trade["actions"][0]["treaty_terms"] = new JObject { ["duration_days"] = 17 };
        WorldDiplomacyEnvelopeJsonRules.NormalizeGeneratedDiplomaticEnvelopeShape(job, trade);
        var tradeRecord = new WorldDiplomacyDocument { AuthorKingdomId = "a", RoundId = round.RoundId };
        Test.True(o.TryApplyGeneratedSemanticEnvelope(tradeRecord, trade, "a", "b", false, false)
            && tradeRecord.Actions.Single().TreatyTerms?.DurationDays == 17, "explicit trade duration survives to the exact-terms execution guard instead of becoming omitted");
        var extra = Vassal("b", "a"); extra["actions"][0]["treaty_terms"]["daily_tribute"] = 25;
        Test.True(Invalid(extra, out reason) && reason.Contains("changes_source_terms"), "unsupported extra treaty conditions cannot be discarded");

    }

    private static void ThirdCountryIntervention()
    {
        var h = new Host { PublishEnabled = true, PlayerAwareExecution = true, OralEffectPortsEnabled = true, OralRange = (0,3000) };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        o.TryScheduleNormalRound();
        Audited(o, Job(o,"event_concept"), Concept("b", "a", "lore:succession"));
        var round = o.CurrentStorage.ActiveRound;
        Audited(o, Job(o,"event_boundary"), Boundary("b", "a", round.RoundTopic));
        PublishDeclaration(o, Job(o,"generate"), "c", "statement", true);
        Test.True(round.Participants.Single(x => x.KingdomId == "c").State == "observer"
            && !o.CurrentStorage.Jobs.Any(x => x.AuthorKingdomId == "c"), "addressing a third country does not grant participation or pre-delivery knowledge");
        h.CurrentDayValue += 8; o.ProcessPropagationArrivals(); o.ProcessRoundLifecycle();
        var decision = o.CurrentStorage.Jobs.Single(x => x.Kind == "player_decision" && x.AuthorKingdomId == "c");
        Test.True(decision.ForcedIntent == "join_known_event" && round.Participants.Single(x => x.KingdomId == "c").State == "observer",
            "delivered invitation creates a private intervention decision, not an automatic formal declaration");
        Complete(o, decision, new JObject { ["participate"] = true, ["reason"] = "The disputed oath affects our claim.", ["position"] = Position("c") });
        var entrant = round.Participants.Single(x => x.KingdomId == "c");
        Test.True(entrant.State == "observer" && entrant.PendingParticipationSourceId != "" && entrant.ExpectedCost != "" && entrant.Alternative != "" && round.BirthLane == "normal",
            "reasoned intervention persists drafting permission without admitting an unpublished participant");
        PublishDeclaration(o, Job(o,"generate"), "b", "statement", false);
        Test.True(o.CurrentStorage.Documents.Count == 2 && entrant.State == "active" && o.CurrentStorage.Documents.Last().AuthorKingdomId == "c",
            "admitted third country publishes through the same real drafting and validation pipeline");
    }

    private static void SpecificOfferWait(bool withdrawBeforeReview)
    {
        var h = new Host { PublishEnabled = true, PlayerAwareExecution = true, OralEffectPortsEnabled = true, OralRange = (0,3000) };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        o.TryScheduleNormalRound();
        Audited(o, Job(o,"event_concept"), Concept("b", "a", "lore:succession"));
        var round = o.CurrentStorage.ActiveRound;
        Audited(o, Job(o,"event_boundary"), Boundary("b", "a", round.RoundTopic));
        PublishDeclaration(o, Job(o,"generate"), "a", "propose_trade", false);
        var proposal = o.CurrentStorage.Documents.Single();
        h.CurrentDayValue += 8; o.ProcessPropagationArrivals(); o.ProcessRoundLifecycle();
        foreach (var decision in o.CurrentStorage.Jobs.Where(x => x.Kind == "player_decision").ToList())
            Complete(o, decision, new JObject { ["participate"] = false, ["reason"] = "No stake." });
        h.CurrentDayValue++; o.ProcessRoundLifecycle();
        var offer = round.PendingOffers.Single(x => x.Status == "open");
        var positionReply = o.CurrentStorage.Jobs.FirstOrDefault(x => x.Kind == "generate" && x.AuthorKingdomId == "a");
        if (positionReply != null)
        {
            PublishDeclaration(o, positionReply, "b", "statement", false);
            h.CurrentDayValue += 8; o.ProcessPropagationArrivals(); o.ProcessRoundLifecycle();
            foreach (var decision in o.CurrentStorage.Jobs.Where(x => x.Kind == "player_decision").ToList())
                Complete(o, decision, new JObject { ["participate"] = false, ["reason"] = "No stake." });
            h.CurrentDayValue++; o.ProcessRoundLifecycle();
        }
        // Each core position now requires actual substantive handling, including
        // the second country's counter-position. Complete those real queued turns.
        for (int turn=0; turn<3; turn++)
        {
            var coreReply=o.CurrentStorage.Jobs.FirstOrDefault(x=>x.Kind=="generate");
            if(coreReply==null)break;
            PublishDeclaration(o,coreReply,coreReply.TargetKingdomId,"statement",false);
            h.CurrentDayValue+=8;o.ProcessPropagationArrivals();o.ProcessRoundLifecycle();
            foreach(var decision in o.CurrentStorage.Jobs.Where(x=>x.Kind=="player_decision").ToList())
                Complete(o,decision,new JObject{["participate"]=false,["reason"]="No stake."});
            h.CurrentDayValue++;o.ProcessRoundLifecycle();
        }
        Test.True(round.Participants.Where(x=>!string.IsNullOrEmpty(x.AgendaExpressionDocumentId)).All(x=>x.AgendaAnswerDocumentIds.Count>0),
            "both core positions received substantive published handling before offer review");
        Audited(o, Job(o,"event_progress"), new JObject { ["outcome"] = "wait", ["reason"] = "The offered trade terms remain open for consideration.",
            ["positions"] = new JArray(round.Participants.Select(x => Position(x.KingdomId))),
            ["wait"] = new JObject { ["kind"] = "offer", ["source_id"] = proposal.DocumentId, ["source_action_id"] = offer.SourceActionId,
                ["reason"] = "Reassess the unanswered trade proposal.", ["until_day"] = h.CurrentDayValue + (withdrawBeforeReview ? 20 : 2) } });
        h.CurrentDayValue++; o.ProcessRoundLifecycle();
        Test.True(round.EventWait?.SourceActionId == offer.SourceActionId && !o.CurrentStorage.Jobs.Any(x => x.Kind == "event_progress"),
            "specific still-open offer waits until its review time without a new model request every hour");
        if (withdrawBeforeReview)
        {
            o.EnqueueGeneration("b", "a", null, true, proposal, 80, roundId: round.RoundId);
            PublishDeclaration(o, Job(o,"generate"), "a", "withdraw_offer", false, offer.SourceDocumentId, offer.SourceActionId);
            h.CurrentDayValue += 8; o.ProcessPropagationArrivals(); o.ProcessRoundLifecycle();
            foreach (var decision in o.CurrentStorage.Jobs.Where(x => x.Kind == "player_decision").ToList())
                Complete(o, decision, new JObject { ["participate"] = false, ["reason"] = "No stake." });
            h.CurrentDayValue++; o.ProcessRoundLifecycle();
            Test.True(round.EventWait == null && round.EventConvergence.Ending && offer.Status == "withdrawn",
                "actual withdrawal wakes the specific waiting condition before its scheduled review, without inventing agreement");
        }
        else
        {
            h.CurrentDayValue++; o.ProcessRoundLifecycle();
            Test.True(round.EventWait == null && o.CurrentStorage.Jobs.Any(x => x.Kind == "event_progress") && offer.Status == "open",
                "review time wakes progress without fabricating expiry, acceptance or an event ending");
        }
        if (!round.EventConvergence.Ending) Audited(o, Job(o,"event_progress"), new JObject { ["outcome"] = "deferred", ["reason"] = "No agreement was reached on the outstanding offer.",
            ["positions"] = new JArray(round.Participants.Select(x => Position(x.KingdomId))) });
        if (offer.Status == "open")
        {
            Test.True(round.EventConvergence.Ending && !o.CurrentStorage.Jobs.Any(x=>x.Kind=="event_boundary"),
                "model closure cannot discard the unresolved original proposal");
            h.CurrentDayValue++;o.ProcessRoundLifecycle();
            PublishDeclaration(o,Job(o,"generate"),offer.ProposerKingdomId,"reject_trade",false,offer.SourceDocumentId,offer.SourceActionId);
            h.CurrentDayValue+=8;o.ProcessPropagationArrivals();o.ProcessRoundLifecycle();
        }
        Audited(o, Job(o,"event_boundary"), Boundary("b", "a", round.RoundTopic));
        Test.True(round.State == "closed" && round.EventOutcome == "deadlocked" && h.TradeExecutions == 0 && h.Writes == 2,
            "unaccepted proposal can reach an honest unresolved conclusion through actual ending effects");
    }

    private static void AcceptedProposalConclusion(bool partial)
    {
        var h = new Host { PublishEnabled = true, PlayerAwareExecution = true, OralEffectPortsEnabled = true, OralRange = (0,3000) };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        o.TryScheduleNormalRound();
        Audited(o, Job(o,"event_concept"), Concept("b", "a", "lore:succession"));
        var round = o.CurrentStorage.ActiveRound;
        var opening = Boundary("b", "a", round.RoundTopic);
        if (partial) { opening["effects"][0]["kind"] = "town_loyalty"; opening["effects"][0]["settlement"] = "town_b"; opening["effects"][0]["days"] = 21; }
        Audited(o, Job(o,"event_boundary"), opening);
        PublishDeclaration(o, Job(o,"generate"), "a", "propose_trade", true, includeAlliance: partial);
        var proposal = o.CurrentStorage.Documents.Single();
        h.CurrentDayValue += 8; o.ProcessPropagationArrivals(); o.ProcessRoundLifecycle();
        foreach (var decision in o.CurrentStorage.Jobs.Where(x => x.Kind == "player_decision").ToList())
            Complete(o, decision, new JObject { ["participate"] = false, ["reason"] = "No stake." });
        h.CurrentDayValue++; o.ProcessRoundLifecycle();
        var offer = round.PendingOffers.Single(x => x.Intent == "propose_trade");
        var answer = Job(o,"generate");
        Test.True(answer.AuthorKingdomId == "a" && answer.SourceDocumentId == proposal.DocumentId, "actual delivered proposal queues the participating addressee");
        PublishDeclaration(o, answer, "b", "accept_trade", false, proposal.DocumentId, offer.SourceActionId);
        Test.True(offer.Status == "accepted" && h.TradeExecutions == 1, "the exact published acceptance executes trade once");
        if (partial) Test.True(round.PendingOffers.Single(x => x.Intent == "propose_alliance").Status == "open" && h.Effects == 0,
            "accepting one action leaves the second proposal unaccepted and unexecuted");
        h.CurrentDayValue += 8; o.ProcessPropagationArrivals(); o.ProcessRoundLifecycle();
        foreach (var decision in o.CurrentStorage.Jobs.Where(x => x.Kind == "player_decision").ToList())
            Complete(o, decision, new JObject { ["participate"] = false, ["reason"] = "No stake." });
        h.CurrentDayValue++; o.ProcessRoundLifecycle();
        for (int pass = 0; pass < 4 && o.CurrentStorage.Jobs.Any(x => x.Kind == "player_decision"); pass++)
        {
            foreach (var decision in o.CurrentStorage.Jobs.Where(x => x.Kind == "player_decision").ToList())
                Complete(o, decision, new JObject { ["participate"] = false, ["reason"] = "No additional stake or requested response." });
            h.CurrentDayValue++; o.ProcessRoundLifecycle();
        }
        for(int duty=0;duty<3;duty++)
        {
            var coreAnswer=o.CurrentStorage.Jobs.FirstOrDefault(x=>x.Kind=="generate");
            if(coreAnswer==null)break;
            var remaining = round.PendingOffers.FirstOrDefault(x=>x.Status=="open" && x.TargetKingdomId==coreAnswer.AuthorKingdomId);
            PublishDeclaration(o,coreAnswer,coreAnswer.TargetKingdomId,remaining==null?"statement":"reject_alliance",false,
                remaining?.SourceDocumentId??"",remaining?.SourceActionId??"");
            h.CurrentDayValue+=8;o.ProcessPropagationArrivals();o.ProcessRoundLifecycle();
            foreach(var decision in o.CurrentStorage.Jobs.Where(x=>x.Kind=="player_decision").ToList())
                Complete(o,decision,new JObject{["participate"]=false,["reason"]="No additional stake."});
            h.CurrentDayValue++;o.ProcessRoundLifecycle();
        }
        int declarationsBeforeConclusion=o.CurrentStorage.Documents.Count;
        Test.True(round.EventConvergence.Ending && !o.CurrentStorage.Jobs.Any(x => x.Kind == "event_progress")
            && round.PendingOffers.All(x=>x.Status!="open"), "confirmed acceptance ends after all original proposals receive decisions");
        var conclusion = "resolved";
        string originalEffect = o.CurrentStorage.EventEffects.Single().Id;
        if (partial)
        {
            var update = Boundary("b", "a", round.RoundTopic);
            update["effects"][0]["kind"] = "town_loyalty"; update["effects"][0]["settlement"] = "town_b";
            update["effects"][0]["delta"] = 1; update["effects"][0]["days"] = 4;
            update["remove_effects"] = new JArray(originalEffect);
            update["confirmed_documents"] = new JArray(o.CurrentStorage.Documents.Single(x => x.Intent == "accept_trade").DocumentId);
            object[] apply = { round,"progress:"+round.ConversationRevision,update,"" };
            Test.True((bool)typeof(WorldDiplomacyOrchestration).GetMethod("TryPublishEventBoundary",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!
                .Invoke(o,apply)!,"confirmed effect update uses the original publication owner without reopening ordinary progress");
        }
        if (partial)
        {
            Test.True(o.CurrentStorage.EventEffects.Single(x => x.Id == originalEffect).Status == "removed" && h.Loyalty == 50
                && o.CurrentStorage.EventAnnouncements.Count(x => x.State == "public") == 1,
                "confirmed trade adjusts the lasting effect without an extra public process announcement: "+JsonConvert.SerializeObject(o.CurrentStorage.Jobs.Select(x=>new{x.Kind,x.PrivacySourceMaterial,x.SuspendedReason})));
            var saved = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage));
            o.ReplaceStorage(saved); round = o.ResolveRound(round.RoundId);
        }
        Test.True(round.State == "active" && round.EventEndingId == "", "a political conclusion still waits for actual ending effects and public announcement");
        var ending = Boundary("b", "a", round.RoundTopic);
        ending["body"] = partial ? "Trade has taken effect. No alliance was agreed; that question remains unresolved." : "The agreed trade has taken effect and the negotiations are complete.";
        ending["effects"][0]["delta"] = 1;
        ending["effects"][0]["reason"] = "Their completed trade agreement improves relations.";
        if (partial)
        {
            ending["effects"] = new JArray();
            ending["remove_effects"] = new JArray(o.CurrentStorage.EventEffects.Single(x => x.Status == "active").Id);
        }
        Test.True(o.CurrentStorage.Jobs.Any(x=>x.Kind=="event_boundary"),"confirmed-action boundary ready: "+JsonConvert.SerializeObject(new {
            partial,round.EventPhase,round.EventConvergence,round.TechnicalHoldReason,round.PlayerResponses,round.PlayerCourtWork,
            round.Participants,jobs=o.CurrentStorage.Jobs.Select(x=>new{x.Kind,x.SourceDocumentId,x.AuthorKingdomId,x.TargetKingdomId,x.SuspendedReason}),o.CurrentStorage.PropagationArrivals }));
        Audited(o, Job(o,"event_boundary"), ending);
        Test.True(round.State == "closed" && round.EventOutcome == conclusion && h.TradeExecutions == 1 && h.Writes == 2,
            "last acceptance or partial agreement ends naturally with actual outcome and exactly one ending effect");
        Test.True(o.CurrentStorage.Documents.Count == declarationsBeforeConclusion && o.CurrentStorage.EventAnnouncements.Count(x => x.EventId == round.RoundId && x.Stage == "ended") == 1,
            "natural closure creates no forced withdrawal or extra declaration");
        if (partial)
        {
            h.CurrentDayValue++; o.AdvanceEventRuntime();
            Test.True(h.Writes == 2 && o.CurrentStorage.EventEffects.All(x => x.Status == "removed"),
                "restored adjusted effect is removed by the ending and never pulses again or resets its duration");
        }
    }

    private static void ArchivedStorySelection()
    {
        var h = new Host { PublishEnabled = true, OralEffectPortsEnabled = true }; var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        o.TryScheduleNormalRound();
        Audited(o, Job(o,"event_concept"), Concept("b", "a", "lore:succession"));
        var original = o.CurrentStorage.ActiveRound;
        Audited(o, Job(o,"event_boundary"), Boundary("b", "a", original.RoundTopic));
        string originalId = original.RoundId;
        var saved = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!;
        var opening = saved.EventAnnouncements.Single();
        Test.True(opening.MaterialKeys.Contains("lore:succession") && opening.ConceptFingerprint.Length > 0,
            "actual opening receipt persists source identities and story fingerprint through JSON reconstruction");
        // Archive-retention fixture: completed detail may leave the display cache;
        // public boundary receipts remain the durable evidence of the prior case.
        saved.ActiveRound = null; saved.ConcurrentRounds.Clear(); saved.CompletedRounds.Clear(); saved.Jobs.Clear();
        for (int i = 0; i < 40; i++) saved.EventAnnouncements.Add(new() { Id = "later-" + i, EventId = "later-" + i,
            State = "public", Stage = "started", Day = 20 + i, Title = "Unrelated later matter", Body = "Unrelated public event",
            MaterialKeys = new() { "other:" + i } });
        string archive = JsonConvert.SerializeObject(saved);
        foreach (bool newMotive in new[] { false, true })
        {
            var nextHost = new Host { CurrentDayValue = 100, Counter = 1000, PublishEnabled = true, OralEffectPortsEnabled = true };
            var next = new WorldDiplomacyOrchestration(nextHost, new WorldDiplomacyRuntimeState()); nextHost.Owner = next;
            next.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(archive)!);
            next.TryScheduleNormalRound(); var job = Job(next,"event_concept"); next.PrepareSharedRequest(job);
            var context = JObject.Parse(job.UserPrompt);
            Test.True(context["prior_events"]!.Count() <= 12 && context["prior_events"]!.Any(x => (string)x["RoundId"]! == originalId),
                "relevant old case outranks forty newer unrelated cases without injecting the full archive");
            var concept = Concept("b", "a", "lore:succession");
            if (newMotive) concept["background"] = "A newly submitted witness statement challenges the earlier unresolved succession negotiations; the courts now seek a joint hearing.";
            Audited(next, job, concept);
            if (newMotive)
                Test.True(next.CurrentStorage.ActiveRound.EventBackground.Contains("newly submitted") && next.CurrentStorage.Jobs.Any(x => x.Kind == "event_boundary"),
                    "old material remains usable when a new motive explicitly continues the prior dispute");
            else Test.True(job.SuspendedReason == "" && job.MaterialSearchPass == 1 && nextHost.Writes == 0
                && next.CurrentStorage.ActiveRound.EventBackground == ""
                && next.CurrentStorage.ActiveRound.EventSelectionEvidence.Contains("event_story_already_used_without_new_motive"),
                "exact repeated story is retired before effects and consumes one batch even when its detailed round left retention");
        }
    }

    private static void BoundedEventEvidence()
    {
        var h = new Host { PublishEnabled = true };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        var r = o.EnsureActiveRound("a", "b", false); r.RootDocumentId = "history-0";
        string longBody = new string('x', 10000) + " ORIGINAL-END";
        for (int i = 0; i < 300; i++)
        {
            o.AddDocument(new WorldDiplomacyDocument { DocumentId = "history-" + i, RoundId = r.RoundId,
                AuthorKingdomId = "a", TargetKingdomId = "b", Day = i, IsReadyForPublication = true,
                Title = "Historical declaration", Body = longBody,
                Actions = new() { new() { ActionId = "terms", Intent = "propose_trade", TargetKingdomId = "b",
                    TreatyTerms = new() { DurationDays = 77 } } } });
            r.PendingOffers.Add(new() { SourceDocumentId = "history-" + i, SourceActionId = "terms",
                ProposerKingdomId = "a", TargetKingdomId = "b", Intent = "propose_trade", CreatedDay = i,
                Status = i == 1 ? "open" : "accepted" });
            o.CurrentStorage.DialogueArrangements.Add(new() { ArrangementId = "arrangement-" + i, RoundId = r.RoundId,
                ActorKingdomId = "a", TargetKingdomId = "b", SourceDocumentId = "history-" + i,
                CreatedDay = i, SourcePlayerText = longBody, SourceNpcText = longBody,
                Status = i == 2 ? "accepted" : "fulfilled", Terms = new() { DailyTribute = 700, DurationDays = 77 } });
        }
        // Publication readiness does not substitute for arrival at b's court.
        WorldDiplomacyPropagationApplication.ReceiveCourt(o.CurrentStorage, o.ResolveDocument("history-1"), "b", h.CurrentDayValue, () => false, () => { });
        foreach (string kind in new[] { "event_progress", "event_boundary" })
        {
            var job = new WorldDiplomacyJob { Kind = kind, RoundId = r.RoundId, AuthorKingdomId = "a", ForcedIntent = "ended" };
            o.PrepareSharedRequest(job);
            var request = JObject.Parse(job.UserPrompt);
            var docs = (JArray)request[kind == "event_progress" ? "documents" : "public_documents"]!;
            var commitments = (JArray)request[kind == "event_progress" ? "commitments" : "exact_commitments"]!;
            bool ending = kind == "event_boundary";
            Test.True((ending ? docs.Count == 300 && docs.Sum(x=>((string)x["Body"]!).Length) <= 48000
                    : docs.Count >= 3 && docs.Count < 19 && job.UserPrompt.Length < 180000)
                && commitments.Count >= 1 && commitments.Count < 17,
                kind + " bounds completed history without reinjecting three million characters");
            var exact = docs.Single(x => (string)x["DocumentId"]! == "history-1");
            Test.True((ending ? (bool)exact["body_is_excerpt"]! && longBody.StartsWith((string)exact["Body"]!) : (string)exact["Body"]! == longBody) && (int)(exact["Actions"]![0]!["treatyTerms"] ?? exact["Actions"]![0]!["TreatyTerms"])!["DurationDays"]! == 77,
                kind + " preserves exact structured terms; only conclusion prose may be excerpted");
            Test.True(exact["known_by"]!.Values<string>().Contains("b")
                && !docs.Single(x => (string)x["DocumentId"]! == "history-2")["known_by"]!.Values<string>().Contains("b"),
                kind + " distinguishes known original from an undelivered declaration");
            var obligation = commitments.Single(x => (string)x["ArrangementId"]! == "arrangement-2");
            Test.True((string)obligation["SourceNpcText"]! == longBody && (bool)obligation["source_is_private"]!
                && (int)obligation["Terms"]!["DailyTribute"]! == 700,
                kind + " preserves the complete pending promise with private-source protection");
            Test.True((int)request["history_selection"]!["omitted_documents"]! == 300 - docs.Count
                && docs.All(x => ending ? (bool)x["body_is_excerpt"]! && longBody.StartsWith((string)x["Body"]!)
                    : !(bool)x["body_is_excerpt"]! && (string)x["Body"]! == longBody),
                kind + " reports selected records/excerpts explicitly without truncating structured conditions");
        }
        Test.True(o.CurrentStorage.Documents.All(x => x.Body == longBody)
            && o.CurrentStorage.DialogueArrangements.All(x => x.SourceNpcText == longBody),
            "request projection never truncates persisted originals or commitments");
    }

    internal static void Run()
    {
        BackgroundCountryScope();
        BoundedEventEvidence();
        DeclarationAdmission();
        MaterialAdmission();
        LostParticipantAuthority();
        LostParticipantAuthority(true);
        LostParticipantAuthority(true, true);
        ArchivedStorySelection();
        ExactGeneratedOfferBinding();
        ThirdCountryIntervention();
        SpecificOfferWait(false);
        SpecificOfferWait(true);
        AcceptedProposalConclusion(false);
        AcceptedProposalConclusion(true);
        foreach (var scenario in new[] { ("b","a","lore:succession","statement"), ("c","b","lore:envoy","statement"), ("b","c","lore:envoy","ultimatum") })
        {
            var h = new Host { PublishEnabled = true, PlayerAwareExecution = true, OralEffectPortsEnabled = true, OralRange = (0,3000) };
            var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
            o.TryScheduleNormalRound();
            var r = o.CurrentStorage.ActiveRound;
            Test.True(r != null && r.InitiatorKingdomId == "" && o.CurrentStorage.Documents.Count == 0,
                "real ordinary entry reserves a story slot without preselecting a country/action/document");
            var concept = Job(o,"event_concept"); o.PrepareSharedRequest(concept);
            Test.True(concept.UserPrompt.Contains("lore:succession") && concept.UserPrompt.Contains("lore:envoy"), "actual concept request receives attributed historical material before first declaration");
            Audited(o,concept,Concept(scenario.Item1,scenario.Item2,scenario.Item3));
            Test.True(r.EventBackground != "" && r.Participants.All(x => x.Agenda != "" && x.Alternative != "")
                && !o.CurrentStorage.Jobs.Any(x => x.Kind == "generate"), "background and persistent positions precede opening and generation");
            Audited(o,Job(o,"event_boundary"),Boundary(scenario.Item1,scenario.Item2,r.RoundTopic));
            Test.True(h.Writes == 1 && r.EventBeginningId != "", "opening actual effect commits before declaration");
            var generation = Job(o,"generate"); o.PrepareSharedRequest(generation);
            Test.True(generation.Kind == "privacy_material" && generation.UserPrompt.Contains(r.EventBackground)
                && generation.UserPrompt.Contains("no forged confession"), "first real draft material reads the established event and its own persistent position");
            bool demandsReply = scenario.Item4 == "ultimatum";
            PublishDeclaration(o,generation,scenario.Item2,scenario.Item4,demandsReply);
            var doc = o.CurrentStorage.Documents.Single();
            Test.True(doc.IsReadyForPublication && doc.RequiresResponse == demandsReply, "actual completion preserves the declaration's requested response");
            // Deliver through the actual distance-based propagation processor, no manual court-knowledge writes.
            h.CurrentDayValue += 8; o.ProcessPropagationArrivals(); o.ProcessRoundLifecycle();
            // Optional observers can privately decline; this is a model response, not a forced participant exit.
            foreach (var decision in o.CurrentStorage.Jobs.Where(x => x.Kind == "player_decision").ToList())
                Complete(o,decision,new JObject { ["participate"] = false, ["reason"] = "No stake in this dispute." });
            h.CurrentDayValue++; o.ProcessRoundLifecycle();
            if (demandsReply || o.CurrentStorage.Jobs.Any(x => x.Kind == "generate"))
            {
                h.CurrentDayValue++; o.ProcessRoundLifecycle();
                Test.True(o.CurrentStorage.Jobs.Any(x => x.Kind == "generate"), "response queued: " + JsonConvert.SerializeObject(new { jobs=o.CurrentStorage.Jobs.Select(x=>new{x.Kind,x.SuspendedReason}),r.PlayerCourtWork,r.Participants,r.TechnicalHoldReason, arrivals=o.CurrentStorage.PropagationArrivals, knowledge=o.CurrentStorage.KingdomKnowledge }));
                var reply = Job(o,"generate");
                Test.True(reply.AuthorKingdomId == scenario.Item2 && reply.SourceDocumentId == doc.DocumentId,
                    "only delivered source queues the addressed participant's response");
                PublishDeclaration(o,reply,scenario.Item1,"statement",false);
                Test.True(o.CurrentStorage.DiplomaticThreats.Count == 0 && h.Effects == 0,
                    "a non-accepting reply to an ultimatum creates no scripted threat, forced escalation or war");
                h.CurrentDayValue += 8; o.ProcessPropagationArrivals();
                foreach (var decision in o.CurrentStorage.Jobs.Where(x => x.Kind == "player_decision").ToList())
                    Complete(o,decision,new JObject { ["participate"] = false, ["reason"] = "No stake in this dispute." });
            }
            h.CurrentDayValue++; o.ProcessRoundLifecycle();
            var finalCoreReply=o.CurrentStorage.Jobs.FirstOrDefault(x=>x.Kind=="generate");
            if(finalCoreReply!=null)
            {
                PublishDeclaration(o,finalCoreReply,finalCoreReply.TargetKingdomId,"statement",false);
                h.CurrentDayValue+=8;o.ProcessPropagationArrivals();o.ProcessRoundLifecycle();
                foreach(var decision in o.CurrentStorage.Jobs.Where(x=>x.Kind=="player_decision").ToList())
                    Complete(o,decision,new JObject{["participate"]=false,["reason"]="No additional stake."});
                h.CurrentDayValue++;o.ProcessRoundLifecycle();
            }
            var review = Job(o,"event_progress");
            Test.True(r.Participants.Any(x => x.State == "active"), "natural conclusion does not require forced withdrawal");
            Audited(o,review,new JObject { ["outcome"] = "deferred", ["reason"] = "Both claims remain stated; no requested answer or useful proposal remains.",
                ["positions"] = new JArray(r.Participants.Select(x => Position(x.KingdomId))), ["effect_update"] = null });
            Test.True(r.State == "active" && r.EventPhase == "closing", "verified disposition waits for real ending effect");
            Audited(o,Job(o,"event_boundary"),Boundary(scenario.Item1,scenario.Item2,r.RoundTopic));
            Test.True(r.State == "closed" && r.EventEndingId != "" && h.Writes == 2 && o.CurrentStorage.ActiveRound == null,
                "natural lifecycle commits ending announcement/effect and releases the ordinary slot without direct CloseRound");
            Test.True(o.CurrentStorage.EventAnnouncements.Count(x => x.State == "public") == 2, "only opening and ending create neutral announcements");
            o.TryScheduleNormalRound(); Test.True(o.CurrentStorage.ActiveRound != null, "next real opening can use released capacity");
        }
        Console.WriteLine("Event structure replay: deterministic model responses; no live model/game acceptance implied.");
    }
}
