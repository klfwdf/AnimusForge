using System.Reflection;
using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Controlled model responses test production transitions, not model intelligence.
internal static class MaterialCountryReplay
{
    private static object Invoke(WorldDiplomacyOrchestration o, string name, params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(o, args)!;
    private static (EventStructureReplay.Host h, WorldDiplomacyOrchestration o) Owner()
    {
        var h = new EventStructureReplay.Host { PublishEnabled = true, PlayerAwareExecution = true, OralRange = (0,3000) };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o; return (h,o);
    }
    private static void Complete(WorldDiplomacyOrchestration o, WorldDiplomacyJob j, JObject value)
    { CountryAdmissionReplay.BeforeLegacyConcept(o, j, value); o.PrepareSharedRequest(j); Test.True(o.TryCompleteDiplomacyPipeline(j, new() { Success = true, Content = value.ToString() }), "production pipeline handles fixture"); }
    private static JObject Candidate(string id, params string[] countries) => new() {
        ["id"] = id, ["kingdoms"] = new JArray(countries), ["material_keys"] = new JArray("lore:succession"),
        ["conflict"] = id + " disputed responsibility", ["motive"] = "Public hearing", ["new_development"] = "New witness",
        ["theme"]="history", ["theme_reason"]="Conflicting historical accounts motivate the current demand for recognition", ["npc_creation"] = "None", ["negotiation_space"] = "Recognition and reparations" };
    private static JObject Assessment(string id) => new() { ["id"] = id, ["qualified"] = true, ["duplicate_of"] = "",
        ["strength"] = "Specific claim", ["weakness"] = "Contested evidence", ["novelty_reason"] = "New witness", ["urgency_reason"] = "Current evidence" };
    private static JObject Position() => new() { ["agenda"] = "Recognition", ["gain"] = "Public recognition", ["cost"] = "Joint review",
        ["red_line"] = "No confession", ["alternative"] = "Unresolved dispute" };
    private static void Selection()
    {
        foreach (string mode in new[] { "quality", "equivalent", "urgent", "duplicate" })
        {
            var (h,o) = Owner(); h.CurrentDayValue = 40;
            o.CurrentStorage.CountryParticipation["a"] = new() { LastExpressionDay = 39 };
            o.CurrentStorage.CountryParticipation["b"] = new() { LastExpressionDay = 38 };
            o.CurrentStorage.CountryParticipation["c"] = new() { LastExpressionDay = 1 };
            o.TryScheduleNormalRound(); var job = o.CurrentStorage.Jobs.Single(x => x.Kind == "event_concept");
            Complete(o,job,new() { ["candidates"] = new JArray(Candidate("popular","a","b"),Candidate("absent","b","c")) });
            o.PrepareSharedRequest(job);
            Test.True(JObject.Parse(job.UserPrompt)["investigation_starts"]!.Type == JTokenType.Null && !job.UserPrompt.Contains("LastExpressionDay"), "comparison is blind to absence metadata");
            Test.True(o.CurrentStorage.CountryParticipation["c"].LastExpressionDay == 1 && o.CurrentStorage.EventAnnouncements.Count == 0, "investigation does not count as speech/publication");
            var second = Assessment("absent"); if (mode == "duplicate") second["duplicate_of"] = "popular";
            Complete(o,job,new() { ["assessments"] = new JArray(Assessment("popular"),second),
                ["best_equivalent"] = mode is "quality" or "duplicate" ? new JArray("popular") : new JArray("popular","absent"),
                ["urgent"] = mode == "urgent" ? new JArray("popular") : new JArray(), ["comparison_reason"] = "Specific comparison" });
            Test.True((string)JObject.Parse(job.EventSelectedCandidate)["id"]! == (mode == "equivalent" ? "absent" : "popular"), "selection respects " + mode);
            Test.True(o.CurrentStorage.MaterialLeads.All(x => x.Status != "published") && h.Writes == 0, "unselected leads and unpublished drafts are not facts");
            o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
            Test.True(o.CurrentStorage.Jobs.Single(x => x.Kind == "event_concept").EventComparison.Length > 0 && o.CurrentStorage.CountryParticipation["c"].LastExpressionDay == 1,
                "comparison and country identity survive reload");
        }
    }
    private static void Invitations()
    {
        foreach (string sender in new[] { "p", "a" })
        foreach (string decision in new[] { "accept", "decline", "question", "conditional", "reserve" })
        {
            var (h,o) = Owner();
            var r = new WorldDiplomacyRound { RoundId = "case", EventSystemVersion = 1, SchedulingOwner = "player", BirthLane = "player",
                PlayerAuthorityKingdomId = "p", PlayerAuthorityRulerId = "ruler_p", RootDocumentId = "invite",
                Participants = new() { new() { KingdomId = "p", State = "active" },new() { KingdomId = "a", State = "active" },new() { KingdomId = "c", State = "observer" } } };
            r.EventBeginningId = "case:started";
            o.CurrentStorage.EventAnnouncements.Add(new() { Id = r.EventBeginningId, EventId = r.RoundId, Stage = "started", State = "public", CanonicalHistoryRecorded = true });
            o.CurrentStorage.ConcurrentRounds.Add(r); Invoke(o,"InvalidateDialogueIndex");
            var invite = new WorldDiplomacyDocument { DocumentId = "invite", RoundId = "case", AuthorKingdomId = sender, TargetKingdomId = "c", IsPlayerAuthored = sender == "p",
                IsReadyForPublication = true, AnalysisStatus = "success", Body = "Please join this discussion.", Title = "Invitation", ResponseRequestsClassified = true,
                ResponseRequests = new() { new() { KingdomId = "c", Kind = "invitation", Quote = "Please join this discussion." } } };
            o.AddDocument(invite); WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(o.CurrentStorage.KingdomKnowledge,"c",invite.DocumentId,h.CurrentDayValue);
            if (sender == "p") Invoke(o,"RegisterPlayerResponseWork",invite,true);
            else { o.HandleRoundDocumentProcessed(invite); Invoke(o,"SchedulePlayerResponseWork",r); }
            o.CloseRound("closed_disabled",r);
            Test.True(r.State == "active" && !o.CurrentStorage.Jobs.Any(x => x.Kind == "event_boundary"), "unanswered invitation blocks closure without fabricating refusal");
            var j = o.CurrentStorage.Jobs.Single(x => x.Kind == "player_decision");
            Test.True(j.ForcedIntent == "answer_invitation", "taskless invitation schedules formal answer");
            Complete(o,j,new() { ["participate"] = decision == "accept", ["decision"] = decision, ["reason"] = "Current interests", ["position"] = Position() });
            var obligation = r.PlayerResponses.Single(); var party = r.Participants.Single(x => x.KingdomId == "c");
            Test.True(obligation.Status == "pending" && party.State == "observer", "private decision never answers or admits");
            j = o.CurrentStorage.Jobs.Single(x => x.Kind == "generate"); o.PrepareSharedRequest(j);
            Complete(o,j,new() { ["safe"] = true, ["public_context"] = "Known public invitation and legal choices" }); o.PrepareSharedRequest(j);
            Test.True(j.UserPrompt.Contains(invite.Body) && j.UserPrompt.Contains(decision), "writer retains exact source and decision");
            var draft = new JObject { ["title"] = "Our reply", ["body"] = decision == "accept" ? "We accept participation in the discussion, seeking recognition through joint review without confession. No treaty or war is agreed."
                : "Our answer is " + decision + ". We have not joined or made a treaty commitment.",
                ["actions"] = new JArray(new JObject { ["target_kingdom_id"] = sender, ["intent"] = "statement", ["commitment"] = "none", ["negotiation_move"] = "state_position" }),
                ["requires_response"] = false, ["round_participation"] = "continue", ["round_status"] = "continue", ["response_requests"] = new JArray(),
                ["answered_player_document_ids"] = new JArray(invite.DocumentId) };
            o.CommitGeneratedDocument(j,draft.ToString()); Complete(o,j,new() { ["safe"] = true });
            Test.True(obligation.Status == "pending" && party.State == "observer" && j.Kind == "privacy_audit" && j.PrivacyRewrites == 0, "missing substantive audit preserves draft and blocks publication and membership");
            Complete(o,j,new() { ["safe"] = true, ["position_owner_valid"]=true,["negotiation_consistent"] = true,
                ["response_continuity"]=MaterialCountryThirdReplay.ContinuityReview(o,j) });
            Test.True(obligation.Status == "answered" && obligation.AnswerDocumentId.Length > 0, "formal reply publishes: " + decision + " "
                + JsonConvert.SerializeObject(new { j.Kind, j.SuspendedReason, j.PrivacyRewrites, j.PrivacyDraft, jobs=o.CurrentStorage.Jobs.Select(x=>new{x.Kind,x.SuspendedReason}),
                    docs=o.CurrentStorage.Documents.Select(x=>new{x.DocumentId,x.AnalysisStatus,x.MechanicalResult,x.IsReadyForPublication,x.AnsweredPlayerDocumentIds}),obligation }));
            Test.True(party.State == (decision == "accept" ? "active" : "observer"), "only published acceptance admits: " + decision + "/" + party.State);
            Test.True(!obligation.AnswerDelivered && o.CurrentStorage.NpcPlayerContactCount == 0, "reply in transit is not a new contact");
            if (decision == "accept") Test.True(party.AgendaExpressionDocumentId == "", "acceptance alone does not close discussion");
            r.State = "closed"; h.CurrentDayValue += 20; o.ProcessPropagationArrivals();
            Test.True(obligation.AnswerDelivered, "closed event retains reply delivery");
        }
    }
    private static void MemoryAndContact()
    {
        var (h,o) = Owner(); var r = new WorldDiplomacyRound { RoundId = "contact", NpcInitiatedPlayerEvent = true, RootDocumentId = "opening" };
        h.CurrentDayValue=10;
        var d = new WorldDiplomacyDocument { DocumentId = "opening", AuthorKingdomId = "a", TargetKingdomId = "p", Day = 10 };
        Invoke(o,"RecordPublishedPlayerContact",r,d); Test.True(o.CurrentStorage.NpcPlayerContactCount == 0,"unpublished contact does not count");
        d.IsReadyForPublication = true; Invoke(o,"RecordPublishedPlayerContact",r,d); Invoke(o,"RecordPublishedPlayerContact",r,d);
        var repeat = new WorldDiplomacyDocument { DocumentId = "reminder", PlayerContactId = d.PlayerContactId, AuthorKingdomId = "a", IsReadyForPublication = true, Day = 11,
            ResponseRequests = new() { new() { KingdomId = "p", Kind = "invitation", Quote = "Invitation" } } };
        Invoke(o,"RecordPublishedPlayerContact",r,repeat);
        Test.True(o.CurrentStorage.NpcPlayerContactCount == 1 && o.CurrentStorage.LastNpcPlayerContactDay == 10,"same contact identity counts once across reminder documents");
        Test.True(!(bool)Invoke(o,"CanInitiatePlayerContact"),"new contact cooldown starts at actual publication");
        h.CurrentDayValue=15;
        Test.True((bool)Invoke(o,"CanInitiatePlayerContact"),"default contact interval expires after five game days");
        o.CurrentStorage.EventAnnouncements.Add(new() { Id = "notice", EventId = "old", State = "public", Title = "Old hearing",
            Narrative = new() { new() { Id = "personal-act", Kind = "adopted_npc_story", Public = true, HeroIds = new() { "ordinary_lord" },
                Source = "adopted:hearing", Text = "This lord publicly submitted a forged witness statement." } } });
        for (int i=0;i<80;i++) o.CurrentStorage.EventAnnouncements.Add(new() { Id = "other-"+i, State = "public", Day = 20+i, Title = "Unrelated" });
        string memory = WorldDiplomacyMemoryApplication.Build(new(o.CurrentStorage,"b",new(),"ordinary_lord"),"What did you do at that hearing?",day=>day.ToString());
        Test.True(memory.Contains("personal-act") && memory.Contains("forged witness") && memory.Contains("personal_experience"),"personal fact recalled without vectors or country match");
        var restored = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!;
        Test.True(WorldDiplomacyMemoryApplication.Build(new(restored,"b",new(),"ordinary_lord"),"Explain yourself",day=>day.ToString()).Contains("personal-act"),"actor recall survives reload and paraphrase");
    }
    internal static void Run()
    { Selection(); Invitations(); PublicationBoundaries(); Recovery(); MemoryAndContact(); Console.WriteLine("Material/country owner replay passed; model/live acceptance NOT_RUN."); }

    private static void PublicationBoundaries()
    {
        foreach (string initial in new[] { "active", "observer" })
        {
            var (h,o) = Owner(); var r = o.EnsureActiveRound("a","c",false);
            var party = r.Participants.Single(x => x.KingdomId == "c"); party.State = initial;
            r.PlayerResponses.Add(new() { KingdomId = "c", SourceDocumentId = "question", RequestKind = "case_question" });
            var d = new WorldDiplomacyDocument { DocumentId = "answer", RoundId = r.RoundId, AuthorKingdomId = "c", TargetKingdomId = "a",
                SourceDocumentId = "question", IsResponse = true, IsReadyForPublication = true };
            o.AddDocument(d); o.StartDocumentPropagation(d,"c"); o.HandleRoundDocumentProcessed(d);
            Test.True(party.State == initial && r.PlayerResponses.Single().AnswerDocumentId == "answer", "formal limited answer preserves prior membership: " + initial);
        }
        foreach (string choice in new[] { "decline", "answer_only", "participate" })
        {
            var (_,o) = Owner(); var r = o.EnsureActiveRound("a","p",false);
            var d = new WorldDiplomacyDocument { AuthorKingdomId = "p", IsPlayerAuthored = true, PlayerEventParticipation = choice };
            var party = WorldDiplomacyStructureRules.EnsurePlayerDocumentParticipant(r,d);
            Test.True(party.State == (choice == "decline" ? "withdrawn" : choice == "answer_only" ? "observer" : "active"), "player participation follows actual declaration: " + choice);
        }
        const string body = "Join this discussion. If you accept arbitration, we will discuss payment. Separately, where is the envoy? Country b is only an example.";
        var json = new JObject { ["response_requests"] = new JArray(new JObject { ["kingdom"]="c",["kind"]="invitation",["quote"]="Join this discussion." },
            new JObject { ["kingdom"]="a",["kind"]="side_question",["quote"]="Separately, where is the envoy?" }) };
        Test.True(WorldDiplomacyAnalysisApplication.TryReadResponseRequests(json,body,x=>x,out var requests)
            && requests.Count==2 && requests.All(x=>x.KingdomId!="b"),"classified mixed requests preserve exact quotations without promoting mere mentions");
        json["response_requests"][0]["quote"]="We agree to pay unconditionally.";
        Test.True(!WorldDiplomacyAnalysisApplication.TryReadResponseRequests(json,body,x=>x,out _),"invented or condition-stripped request quotation rejected");
    }

    private static void Recovery()
    {
        foreach (bool protectedEvidence in new[] { false,true })
        {
            var (h,o)=Owner(); o.TryScheduleNormalRound(); var j=o.CurrentStorage.Jobs.Single(x=>x.Kind=="event_concept"); var r=o.ResolveRound(j.RoundId);
            if (protectedEvidence) o.CurrentStorage.EventEffects.Add(new() { Id="uncertain",EventId=r.RoundId,Status="uncertain" });
            for(int i=0;i<3;i++) Invoke(o,"HoldTechnicalRequest",j,"fixture_service_failure");
            Test.True((r.State=="active")==protectedEvidence && o.CurrentStorage.EventAnnouncements.Count==0 && o.CurrentStorage.NpcPlayerContactCount==0,
                "finite unused draft release preserves uncertain evidence: "+protectedEvidence);
        }
        var (host,owner)=Owner(); var round=owner.EnsureActiveRound("a","b",false);
        round.EventBeginningId=round.RoundId+":started";round.EventPhase="closing";
        owner.CurrentStorage.EventAnnouncements.Add(new() { Id=round.EventBeginningId,EventId=round.RoundId,Stage="started",State="public",CanonicalHistoryRecorded=true });
        var draft=new JObject { ["title"]="Unresolved claims",["body"]="The parties did not resolve their competing claims. No new agreement was reached.",
            ["effects"]=new JArray(),["remove_effects"]=new JArray(),["confirmed_documents"]=new JArray(),["narrative"]=new JArray() };
        Test.True((bool)Invoke(owner,"TryPublishEventBoundary",round,"ended",draft,"") && host.Writes==0,
            "neutral conclusion preserves unresolved result without executing another effect");
    }
}
