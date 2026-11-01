using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using AnimusForge.DiplomacyDialogue;
using System.Reflection;

internal static class AcceptanceTermsRepairReplay
{
    private sealed class Host : ConcurrentOralMigrationReplay.Host
    {
        public override string ResolveKingdomIdOrNull(string id) => string.IsNullOrWhiteSpace(id) ? null : id;
    }
    internal static void Run()
    {
        Test.True(WorldDiplomacyOfferContractRules.ArePeaceTermsEquivalent(null, new WorldDiplomacyPeaceTerms()),
            "an empty oral peace contract and absent additional terms describe the same offer");
        foreach (var changed in new[] {
            new WorldDiplomacyPeaceTerms { DurationDays = 14 },
            new WorldDiplomacyPeaceTerms { DailyTribute = 100 },
            new WorldDiplomacyPeaceTerms { TributePayerKingdomId = "a" },
            new WorldDiplomacyPeaceTerms { CessionSettlementId = "town" } })
            Test.True(!WorldDiplomacyOfferContractRules.ArePeaceTermsEquivalent(null, changed), "nonempty clauses remain binding");
        var (_, owner) = ConcurrentOralMigrationReplay.Fixture();
        var parse = typeof(WorldDiplomacyOrchestration).GetMethod("ParseFormalTreatyTerms", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var source = new WorldDiplomacyDocument { TreatyTerms = new() { DurationDays = 84 } };
        foreach (string payload in new[] { "{}", "{\"treaty_terms\":null}", "{\"treaty_terms\":{}}", "{\"treaty_terms\":{\"duration_days\":84}}" })
        {
            var parsed = (WorldDiplomacyDialogueTerms)parse.Invoke(owner, new object[] { JObject.Parse(payload), "accept_trade", "a", "p" })!;
            Test.True(WorldDiplomacyOfferActionApplication.TryResolveTradeAcceptanceTerms(source, "", parsed, out var original, out _)
                && original.DurationDays == 84, "omitted trade clauses inherit the exact original duration: " + payload);
        }
        foreach (string value in new[] { "0", "30", "-1", "null", "\"bad\"" })
        {
            var parsed = (WorldDiplomacyDialogueTerms)parse.Invoke(owner, new object[] {
                JObject.Parse("{\"treaty_terms\":{\"duration_days\":" + value + "}}"), "accept_trade", "a", "p" })!;
            Test.True(!WorldDiplomacyOfferActionApplication.TryResolveTradeAcceptanceTerms(source, "", parsed, out _, out _),
                "explicit altered or malformed trade duration cannot become consent: " + value);
        }
        Disclosure();
        Recovery();
        GeneratedTradeValidation();
        OralPeace();
    }

    private static void Disclosure()
    {
        var json = new JObject { ["peace_terms"] = WorldDiplomacyEnvelopeJsonRules.BuildPeaceTermsJson(new() {
            TributePayerKingdomId = "a", TributeReceiverKingdomId = "b", DailyTribute = 100, DurationDays = 14 }) };
        foreach (var text in new[] { "我方每日向贵方支付100第纳尔，持续14天。", "我国每日向贵国支付100第纳尔，持续14天。",
            "We will pay you 100 denars daily for 14 days.", "Our kingdom will pay your kingdom 100 denars daily for 14 days." })
            Test.True(!WorldDiplomacyGenerationValidationRules.TryGetPublicPeaceTermsDisclosureViolation("propose_peace", text, json,
                "a", "b", id => (id, id == "a" ? "Alpha" : "Beta"), _ => "", out var reason), "CN/EN equivalent direction: " + text + ":" + reason);
        foreach (var text in new[] { "贵方每日向我方支付100第纳尔，持续14天。", "You will pay our kingdom 100 denars daily for 14 days.",
            "我方每日向贵方支付50第纳尔，持续14天。", "We will pay you 100 denars daily for 30 days." })
            Test.True(WorldDiplomacyGenerationValidationRules.TryGetPublicPeaceTermsDisclosureViolation("propose_peace", text, json,
                "a", "b", id => (id, id == "a" ? "Alpha" : "Beta"), _ => "", out _), "changed direction/amount/duration still rejected: " + text);
    }

    private static (ConcurrentOralMigrationReplay.Host h, WorldDiplomacyOrchestration o, WorldDiplomacyRound r,
        WorldDiplomacyDocument source, WorldDiplomacyDocument reply) RejectedFixture(string intent)
    {
        var h = new Host(); var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        h.PublishEnabled = true; h.PlayerAwareExecution = true; h.OralEffectPortsEnabled = true; h.WorldWar = intent == "accept_peace";
        var r = o.EnsureActiveRound("a", "b", false); r.EventPhase = "discussing";
        foreach (var participant in r.Participants) participant.State = "active";
        r.EventBeginningId = r.RoundId + ":started";
        o.CurrentStorage.EventAnnouncements.Add(new() { Id = r.EventBeginningId, EventId = r.RoundId,
            Stage = "started", State = "public", Title = "Terms", Body = "The two kingdoms are negotiating." });
        var source = new WorldDiplomacyDocument { DocumentId = "original", RoundId = r.RoundId, AuthorKingdomId = "a", TargetKingdomId = "b",
            IsReadyForPublication = true, Intent = WorldDiplomacyIntentVocabulary.ResponseIntentToProposalIntent(intent),
            Commitment = "proposal", Title = "Offer", Body = "Our original offer remains open.",
            PeaceTerms = new(), TreatyTerms = intent == "accept_trade" ? new() { DurationDays = 84 } : null };
        r.RootDocumentId = source.DocumentId; o.AddDocument(source);
        r.PendingOffers.Add(new() { SourceDocumentId = source.DocumentId, SourceActionId = "", ProposerKingdomId = "a",
            TargetKingdomId = "b", Intent = source.Intent, Status = "open" });
        WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(o.CurrentStorage.KingdomKnowledge, "b", source.DocumentId, 12);
        var reply = new WorldDiplomacyDocument { DocumentId = "rejected", RoundId = r.RoundId, AuthorKingdomId = "b", TargetKingdomId = "a",
            IsResponse = true, SourceDocumentId = source.DocumentId, RespondingToOfferDocumentId = source.DocumentId,
            Intent = intent, Commitment = "acceptance", AnalysisStatus = "rejected_draft", Title = "Answer", Body = "We accept the original offer." };
        o.AddDocument(reply);
        r.TechnicalHoldReason = intent == "accept_peace" ? "invalid_draft:action_not_executable:player_accept_peace_changes_offer_terms"
            : "invalid_draft:trade_acceptance_changes_terms";
        return (h, o, r, source, reply);
    }

    private static void Recovery()
    {
        foreach (string intent in new[] { "accept_peace", "accept_trade" })
        foreach (string defect in new[] { "none", "executed", "receipt", "uncertain", "closed_offer", "unknown_source", "other_hold", "player", "closed_round", "ambiguous" })
        {
            var (h, o, r, source, reply) = RejectedFixture(intent);
            switch (defect)
            {
                case "executed": reply.ChangedDiplomaticState = true; break;
                case "receipt": reply.MechanicalResult = "uncertain result"; break;
                case "uncertain": reply.Actions = new() { new() { Intent = intent, ExecutionState = "uncertain" } }; break;
                case "closed_offer": r.PendingOffers.Single().Status = "rejected"; break;
                case "unknown_source": o.CurrentStorage.KingdomKnowledge.Clear(); break;
                case "other_hold": r.TechnicalHoldReason = "event_document_execution_uncertain"; break;
                case "player": reply.IsPlayerAuthored = true; break;
                case "closed_round": r.State = "closed"; break;
                case "ambiguous": var other = JsonConvert.DeserializeObject<WorldDiplomacyDocument>(JsonConvert.SerializeObject(reply))!;
                    other.DocumentId = "other-rejected"; o.AddDocument(other); break;
            }
            o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
            var recovered = o.ResolveDocument(reply.DocumentId);
            bool expected = defect == "none";
            Test.True((recovered.AcceptanceRepairVersion == 1) == expected, "only proven unpublished acceptance rejection recovers: " + intent + "/" + defect);
            Test.True(!recovered.IsReadyForPublication && !recovered.EventExecutionPending && h.Effects == 0,
                "recovery never publishes, executes or adopts rejected terms");
            if (!expected) continue;
            var job = o.CurrentStorage.Jobs.Single(x => x.AuthorKingdomId == "b");
            Test.True(job.SourceDocumentId == source.DocumentId && job.TargetKingdomId == "a"
                && o.ResolveRound(r.RoundId).TechnicalHoldReason == "", "recovered reply keeps source and clears only its obsolete hold");
            string id = job.JobId;
            o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
            Test.True(o.CurrentStorage.Jobs.Count(x => x.JobId == id) == 1 && o.ResolveDocument(reply.DocumentId).AcceptanceRepairVersion == 1,
                "reloading neither duplicates work nor replenishes recovery");
        }
    }

    private static void GeneratedTradeValidation()
    {
        foreach (int? duration in new int?[] { null, 84, 30, 0 })
        {
            var (_, o, r, source, _) = RejectedFixture("accept_trade");
            var action = new JObject { ["intent"] = "accept_trade", ["commitment"] = "acceptance", ["target_kingdom_id"] = "a",
                ["responding_to_offer_document_id"] = source.DocumentId, ["responding_to_offer_action_id"] = "",
                ["peace_terms"] = WorldDiplomacyEnvelopeJsonRules.BuildPeaceTermsJson(null),
                ["treaty_terms"] = duration.HasValue ? new JObject { ["duration_days"] = duration.Value } : new JObject() };
            var json = new JObject { ["title"] = "Trade reply", ["body"] = "We accept the original trade offer.",
                ["actions"] = new JArray(action), ["mentioned_kingdom_ids"] = new JArray(), ["requires_response"] = false,
                ["addressed_kingdom_ids"] = new JArray("a"), ["tone"] = "neutral", ["confidence"] = 1.0 };
            var job = new WorldDiplomacyJob { RoundId = r.RoundId, AuthorKingdomId = "b", TargetKingdomId = "a", SourceDocumentId = source.DocumentId,
                IsResponse = true, CandidateKingdomIds = new() { "a" } };
            bool rejected = o.TryGetGeneratedIntentLegalityViolation(job, json, "b", "a", out _, out string reason);
            Test.True(rejected == (duration.HasValue && duration != 84), "trade admission agrees with execution before document creation: " + duration + ":" + reason);
            if (rejected) Test.True(reason.Contains("trade_acceptance_changes_terms"), "trade rejection uses bounded draft repair before execution");
        }
    }

    private static void OralPeace()
    {
        foreach (string mode in new[] { "new", "source_null", "source_empty", "recovery" })
        {
            var h = new Host { PublishEnabled = true, PlayerAwareExecution = true, OralEffectPortsEnabled = true, WorldWar = true };
            var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
            string payload = "action=Peace;move=NewMatter;target=p";
            if (mode != "new")
            {
                var r = o.EnsureActiveRound("p", "a", false); r.EventPhase = "discussing";
                foreach (var participant in r.Participants) participant.State = "active";
                r.EventBeginningId = "beginning";
                o.CurrentStorage.EventAnnouncements.Add(new() { Id = r.EventBeginningId, EventId = r.RoundId, Stage = "started", State = "public", Title = "Peace talks", Body = "Peace talks are in progress." });
                var source = new WorldDiplomacyDocument { DocumentId = "original", RoundId = r.RoundId, AuthorKingdomId = "p", TargetKingdomId = "a",
                    Intent = "propose_peace", Commitment = "proposal", IsPlayerAuthored = true, IsReadyForPublication = true,
                    PeaceTerms = mode == "source_empty" ? new() : null, Title = "Peace", Body = "Let us end this war without additional terms." };
                r.RootDocumentId = source.DocumentId; o.AddDocument(source);
                r.PendingOffers.Add(new() { SourceDocumentId = source.DocumentId, Intent = "propose_peace", Status = "open", ProposerKingdomId = "p", TargetKingdomId = "a" });
                WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(o.CurrentStorage.KingdomKnowledge, "a", source.DocumentId, 12);
                payload = "action=Peace;move=AcceptProposal;target=p;source_document=original";
            }
            o.SubmitOralDiplomaticCommitment("ruler_a", "a", payload,
                new DialogueInteractionOrigin("native", playerText: "I agree to peace without tribute or cession.", npcText: "I agree to those exact peace terms."));
            var item = o.CurrentStorage.DialogueArrangements.Single();
            if (mode == "recovery")
            {
                item.DocumentId = "rejected-oral"; item.Status = "cancelled"; item.GenerationFailureAttempts = 3;
                o.AddDocument(new() { DocumentId = item.DocumentId, RoundId = item.RoundId, AuthorKingdomId = "a", TargetKingdomId = "p",
                    Intent = "accept_peace", AnalysisStatus = "rejected_draft", SourceDocumentId = "original", RespondingToOfferDocumentId = "original",
                    DialogueArrangementId = item.ArrangementId, DialogueArrangementVersion = item.Version });
                o.CurrentStorage.Jobs.Clear();
                o.ResolveRound(item.RoundId).TechnicalHoldReason = "invalid_draft:action_not_executable:player_accept_peace_changes_offer_terms";
                o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
                item = o.CurrentStorage.DialogueArrangements.Single();
                Test.True(h.Effects == 0 && !o.ResolveDocument(item.DocumentId).IsReadyForPublication && o.CurrentStorage.Jobs.Count == 1,
                    "old oral rejection queues audited writing even after old fallback budget, with no load-time execution");
            }
            var job = o.CurrentStorage.Jobs.Single(); job.Kind = "generate"; job.PrivacyApproved = true;
            o.CommitGeneratedDocument(job, new JObject { ["title"] = "Peace declaration", ["body"] = "Our two kingdoms end the war immediately, without tribute or cession. We publicly confirm the agreed peace.", ["actions"] = new JArray() }.ToString());
            Test.True(item.Status == "executed" && h.Effects == 1 && o.ResolveDocument(item.DocumentId).IsReadyForPublication,
                "real oral peace publication and one execution: " + mode + ":" + item.Status + ":" + item.Reason);
        }
    }
}
