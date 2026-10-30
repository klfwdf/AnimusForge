using AnimusForge;
using AnimusForge.DiplomacyDialogue;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class SpeechReviewReplay
{
    private static void EventAttribution()
    {
        foreach (string channel in new[] { "native", "scene", "courier" })
        foreach (string mode in new[] { "related", "unrelated", "missing", "malformed", "unknown", "closed", "changed", "duplicate", "reload", "bad_boolean" })
        {
            var (h, o) = Fixture();
            var prior = o.EnsureActiveRound("a", "b", false); prior.RoundTopic = "Border trade dispute";
            const string basis = "The two rulers dispute the closing of the border market and seek a trade agreement.";
            o.CurrentStorage.EventAnnouncements.Add(new DiplomacyEventAnnouncement {
                Id = "public-opening", EventId = prior.RoundId, State = "public", Stage = "started", Body = basis });
            var origin = new DialogueInteractionOrigin(channel, "route-" + mode, "session",
                "Resolve this border market dispute with trade for fourteen days.",
                "I agree to trade for fourteen days to settle this border market dispute.");
            o.CaptureSpeechReview("ruler_a", "a", origin, "[ACTION:DIPLOMACY:REVIEW_REMAINING]" + ("event=" + prior.RoundId));
            var job = o.CurrentStorage.Jobs.Single(); o.PrepareSharedRequest(job);
            Test.True(job.SystemPrompt.Contains(DiplomacyPromptTexts.Get("SpeechReview_EventAttribution", "MISSING"))
                && job.UserPrompt.Contains(basis), "real paired prompt receives public case evidence: " + channel);
            var extraction = Extraction(origin.FullNpcText, origin.FullPlayerText);
            var entry = (JObject)extraction["commitments"][0];
            entry["payload"] = (string)entry["payload"] + ";event=" + prior.RoundId;
            entry["event_link"] = new JObject { ["event_id"] = prior.RoundId, ["source_id"] = "public-opening",
                ["source_quote"] = basis, ["dialogue_quote"] = origin.FullNpcText, ["reason"] = "The agreed trade resolves the same border closure." };
            if (mode == "missing") entry.Remove("event_link");
            if (mode == "malformed") entry["event_link"]["source_quote"] = "Invented source with no real evidence.";
            if (mode == "unknown") entry["event_link"]["event_id"] = "unknown-case";
            Complete(o, job, extraction); o.PrepareSharedRequest(job);
            var audit = new JObject { ["approved"] = true, ["event_links"] = new JArray(new JObject {
                ["commitment_index"] = 0, ["event_id"] = prior.RoundId, ["related"] = mode != "unrelated",
                ["reason"] = "The same specific border-market dispute is explicitly addressed." }) };
            if (mode == "missing") audit.Remove("event_links");
            if (mode == "bad_boolean") audit["event_links"][0]["related"] = "true";
            if (mode == "duplicate") ((JArray)audit["event_links"]).Add(audit["event_links"][0].DeepClone());
            if (mode == "closed") prior.State = "closed";
            if (mode == "changed") o.CurrentStorage.EventAnnouncements.Single().Body = "The public evidence has changed.";
            if (mode == "reload")
            {
                o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
                job = o.CurrentStorage.Jobs.Single();
            }
            Complete(o, job, audit);
            var agreement = o.CurrentStorage.DialogueArrangements.Single();
            bool linked = mode is "related" or "reload";
            Test.True((agreement.RoundId == prior.RoundId) == linked && agreement.Status == "accepted"
                && o.CurrentStorage.SpeechReviews.Count == 0 && agreement.Terms.DurationDays == 14,
                "valid promise survives optional attribution and chooses the correct case: " + channel + ":" + mode);
            Test.True(linked || o.ResolveRound(agreement.RoundId).BirthLane == "oral", "fallback creates an independent oral event");
            // Repeated completion and a second direct submission after reload cannot
            // re-route the same accepted interaction even if candidate knowledge changes.
            Complete(o, job, audit);
            o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
            o.SubmitOralDiplomaticCommitment("ruler_a", "a", (string)entry["payload"], origin, out bool duplicateRegistered);
            Test.True(duplicateRegistered && o.CurrentStorage.DialogueArrangements.Count == 1
                && o.CurrentStorage.DialogueArrangements.Single().RoundId == agreement.RoundId, "reload and repeated callback retain one case");
            agreement = o.CurrentStorage.DialogueArrangements.Single();
            var writing = o.CurrentStorage.Jobs.Single(x => x.DialogueArrangementId == agreement.ArrangementId);
            o.PrepareSharedRequest(writing);
            Complete(o, writing, JObject.Parse("{\"safe\":true,\"public_context\":\"The rulers agreed to trade for fourteen days.\"}"));
            o.PrepareSharedRequest(writing);
            var prose = JObject.Parse(OralDeclarationReplay.Draft(agreement, "propose_trade")); prose["actions"] = new JArray();
            o.CommitGeneratedDocument(writing, prose.ToString());
            Test.True(writing.Kind == "privacy_audit" && h.TradeExecutions == 0, "normal prose-only declaration still awaits confidentiality review");
            Complete(o, writing, JObject.Parse("{\"safe\":true,\"agreement_consistent\":true}"));
            var document = o.ResolveDocument(agreement.DocumentId);
            Test.True(h.TradeExecutions == 0 && document?.IsReadyForPublication == true && document.RequiresResponse
                && document.RoundId == agreement.RoundId, "third-country commitment publishes a proposal without inventing target consent: " + mode);
            o.MaintainDialogueAndMemory();
            Test.True(h.TradeExecutions == 0, "maintenance never invents third-party assent");
        }
        ExactProposalAttribution();
        WarAttribution();
    }

    private static void ExactProposalAttribution()
    {
        foreach (string move in new[] { "AcceptProposal", "RejectProposal" })
        {
            var (h, o) = Fixture(); var round = o.EnsureActiveRound("p", "a", false);
            var proposal = new WorldDiplomacyDocument { DocumentId = "original-proposal", RoundId = round.RoundId,
                AuthorKingdomId = "p", TargetKingdomId = "a", Intent = "propose_trade", IsReadyForPublication = true,
                TreatyTerms = new() { DurationDays = 14 } };
            o.CurrentStorage.Documents.Add(proposal);
            round.PendingOffers.Add(new WorldDiplomacyRoundOffer { SourceDocumentId = proposal.DocumentId,
                ProposerKingdomId = "p", TargetKingdomId = "a", Intent = "propose_trade", Status = "open" });
            WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(o.CurrentStorage.KingdomKnowledge, "a", proposal.DocumentId, 12);
            o.SubmitOralDiplomaticCommitment("ruler_a", "a", "action=Trade;move=" + move
                + ";target=p;source_document=original-proposal;event=invented-other-event",
                new DialogueInteractionOrigin("native", "reply", "session", "Please reply to my proposal.", "I reply to that exact proposal."), out bool registered);
            Test.True(registered && o.CurrentStorage.DialogueArrangements.Single().RoundId == round.RoundId
                && o.CurrentStorage.DialogueArrangements.Single().SourceDocumentId == proposal.DocumentId
                && o.CurrentStorage.ConcurrentRounds.Count == 0,
                "exact proposal reply keeps its original case despite wrong optional event hint: " + move);
        }
    }

    private static void WarAttribution()
    {
        // Reproduce the incident's accepted war + unrelated known event + bare
        // approved:true audit. Review semantics and game effect ports are controlled.
        var (h, o) = Fixture(); var north = o.EnsureActiveRound("b", "c", false); north.RoundTopic = "Northern internal unrest";
        o.CurrentStorage.EventAnnouncements.Add(new DiplomacyEventAnnouncement { Id = "north-opening", EventId = north.RoundId,
            State = "public", Stage = "started", Body = "The northern ruler faces a tax revolt among his own nobles." });
        var origin = new DialogueInteractionOrigin("native", "paid-war", "session", "Declare war on the Southern Empire.",
            "I agree to declare war on the Southern Empire.");
        o.CaptureSpeechReview("ruler_a", "a", origin, "[ACTION:DIPLOMACY:REVIEW_REMAINING]" + ("event=" + north.RoundId));
        var job = o.CurrentStorage.Jobs.Single(); o.PrepareSharedRequest(job);
        var extraction = Extraction(origin.FullNpcText, origin.FullPlayerText);
        extraction["commitments"][0]["payload"] = "action=DeclareWar;move=NewMatter;target=empire_s;event=" + north.RoundId;
        Complete(o, job, extraction); o.PrepareSharedRequest(job); Complete(o, job, JObject.Parse("{\"approved\":true}"));
        var agreement = o.CurrentStorage.DialogueArrangements.Single();
        Test.True(agreement.RoundId != north.RoundId && o.ResolveRound(agreement.RoundId).BirthLane == "oral",
            "actual wrong-event pattern creates independent oral event without losing accepted war");
        var writing = o.CurrentStorage.Jobs.Single(); o.PrepareSharedRequest(writing);
        Complete(o, writing, JObject.Parse("{\"safe\":true,\"public_context\":\"The ruler agreed to declare war on the Southern Empire.\"}"));
        o.PrepareSharedRequest(writing);
        var prose = JObject.Parse(OralDeclarationReplay.Draft(agreement, "declare_war", "The ruler of a formally declares war on the Southern Empire. Our armies will prepare for this war."));
        prose["actions"] = new JArray(); o.CommitGeneratedDocument(writing, prose.ToString());
        Complete(o, writing, JObject.Parse("{\"safe\":true,\"agreement_consistent\":true}"));
        Test.True(h.Effects == 1 && o.ResolveDocument(agreement.DocumentId)?.IsReadyForPublication == true
            && o.ResolveDocument(agreement.DocumentId).RoundId != north.RoundId, "independent war reaches the original real effect owner and publication");
    }
}
