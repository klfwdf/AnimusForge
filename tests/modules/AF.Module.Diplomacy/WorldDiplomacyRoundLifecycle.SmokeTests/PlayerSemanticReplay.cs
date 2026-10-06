using AnimusForge;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Production parser/composer/orchestration, using the existing detached host fixtures.
internal static class PlayerSemanticReplay
{
    internal static void Run()
    {
        foreach (string status in new[] { "success", "no_action", "rejected", "" })
        {
            var d = PlayerDocument(); int executions = 0;
            var job = new WorldDiplomacyJob { DocumentId = d.DocumentId };
            void Commit(string raw) => WorldDiplomacyAnalysisApplication.CommitAnalysis(job, raw, 3, Array.Empty<WorldDiplomacyThreat>(),
                _ => d, _ => null, id => id, id => id, (_, _, _) => null,
                (ids, excluded) => ids.Where(x => !string.IsNullOrWhiteSpace(x) && x != excluded).ToList(),
                (_, _) => throw new Exception("unexpected suppression"), (_, _, _, _, _, _) => executions++, _ => { });
            string raw = new JObject { ["status"] = status, ["intent"] = "declare_war", ["commitment"] = "binding",
                ["primary_target_kingdom_id"] = "b" }.ToString();
            Commit(raw); Commit(raw);
            Test.True(d.Intent == "declare_war" && d.PlayerAnalysisCommitted && executions == 1,
                "valid extracted war survives classifier status and duplicate completion: " + status);
            var restored = JsonConvert.DeserializeObject<WorldDiplomacyDocument>(JsonConvert.SerializeObject(d));
            Test.True(restored.PlayerAnalysisCommitted, "save retains execution admission marker");
        }
        var failed = PlayerDocument(); int failedEffects = 0;
        var failedJob = new WorldDiplomacyJob { DocumentId = failed.DocumentId };
        void RetryCommit(string raw) => WorldDiplomacyAnalysisApplication.CommitAnalysis(failedJob, raw, 3, Array.Empty<WorldDiplomacyThreat>(),
            _ => failed, _ => null, id => id, id => id, (_, _, _) => null,
            (ids, excluded) => ids.Where(x => !string.IsNullOrWhiteSpace(x) && x != excluded).ToList(),
            (_, _) => throw new Exception("player text must survive"), (_, _, _, _, _, _) => failedEffects++, _ => { });
        RetryCommit("{}");
        var player = new WorldDiplomacyPlayerContext(1, "a", true, true, "a");
        Test.True(failed.AnalysisStatus == "analysis_failed" && failedEffects == 0 && !failed.PlayerAnalysisCommitted
            && failed.Body == "原文" && WorldDiplomacyPlayerApplication.CanRetryAnalysis(failed, player),
            "unusable analysis is explicit, retains text, causes no effect and is retryable");
        Test.True(WorldDiplomacyPresentationQueries.Detail(failed, null, player, _ => "today").CanRetryAnalysis,
            "failed player document exposes retry in its detail view");
        var failureStorage = new WorldDiplomacyStorage(); failureStorage.Documents.Add(failed); int removed = 0;
        WorldDiplomacyFailureApplication.Commit(failedJob, "timeout", failureStorage, 24, 1, () => 1, null,
            _ => throw new Exception("player failure must not synthesize a statement"), (_, raw) => RetryCommit(raw),
            _ => { }, (_, _) => { }, _ => "", (_, _) => { }, _ => removed++, _ => { });
        Test.True(removed == 1 && failedEffects == 0 && failed.AnalysisStatus == "analysis_failed",
            "transport failure takes the same no-effects retryable path and retires its job");
        var failedRestored = JsonConvert.DeserializeObject<WorldDiplomacyDocument>(JsonConvert.SerializeObject(failed));
        Test.True(WorldDiplomacyPlayerApplication.CanRetryAnalysis(failedRestored, player), "failed analysis remains retryable after load");
        Test.True(WorldDiplomacyPresentationQueries.Archive(failureStorage, _ => "today", _ => null, _ => failed, player)
            .Single().CanRetryAnalysis, "archive offers recovery even after the original notification is read");
        int prepared = 0;
        void Prepare() => WorldDiplomacyJobPreparationApplication.PrepareAnalysisJob(failed, 100, failureStorage, 1, 100,
            _ => "analysis", _ => null, _ => "", _ => { prepared++; return "full body"; }, j => failureStorage.Jobs.Add(j));
        Prepare(); Prepare();
        Test.True(prepared == 1 && failureStorage.Jobs.Count == 1, "repeated retry admission creates one request and composes once");
        RetryCommit("{\"status\":\"success\",\"intent\":\"declare_war\",\"primary_target_kingdom_id\":\"b\"}");
        RetryCommit("{\"status\":\"success\",\"intent\":\"declare_war\",\"primary_target_kingdom_id\":\"b\"}");
        Test.True(failedEffects == 1 && !WorldDiplomacyPlayerApplication.CanRetryAnalysis(failed, player),
            "retry executes once and completed documents cannot be reinterpreted");
        VerifySourceBinding();
        VerifyCrossRoundOralAcceptance();
        VerifyAcceptanceRoutingAndExecution();
        VerifyPrompt();
        VerifyTreatiesAndWithdrawal();
        var port = new DocumentExecutionReplay.Port { RestrictRound = true,
            Round = new() { RoundId = "r", State = "active" }, RequiredPeace = new() { SourceDocumentId = "peace" } };
        var independent = PlayerDocument(); independent.Intent = "declare_war"; independent.Commitment = "binding";
        WorldDiplomacyDocumentExecutionApplication.ProcessAnalyzedDocument(port, new DocumentExecutionReplay.Orch(port),
            independent, "declare_war", "binding", false, "neutral", 1);
        Test.True(port.Effects == 1 && port.RequiredPeaceReads == 0,
            "player independent action bypasses round-only response requirement while retaining effect admission");
    }

    private static WorldDiplomacyDocument PlayerDocument() => new() { DocumentId = "player", RoundId = "r", AuthorKingdomId = "a",
        TargetKingdomId = "b", IsPlayerAuthored = true, IsReadyForPublication = true, Body = "原文", AnalysisStatus = "pending_analysis" };

    private static void VerifySourceBinding()
    {
        var round = new WorldDiplomacyRound { RoundId = "source-round" };
        var source = new WorldDiplomacyDocument { DocumentId = "source", RoundId = round.RoundId, AuthorKingdomId = "b",
            IsReadyForPublication = true, Actions = new() {
                new() { ActionId = "v1", Intent = "propose_peace", PeaceTerms = new() { DailyTribute = 10 } },
                new() { ActionId = "v2", Intent = "propose_peace", PeaceTerms = new() { DailyTribute = 150 } } } };
        foreach (string id in new[] { "v1", "v2" }) round.PendingOffers.Add(new() { SourceDocumentId = "source", SourceActionId = id,
            Intent = "propose_peace", Status = "open", ProposerKingdomId = "b", TargetKingdomId = "a" });
        foreach (string id in new[] { "v2", "missing", "" })
        {
            var d = PlayerDocument();
            var raw = new JObject { ["status"] = "success", ["intent"] = "accept_peace", ["commitment"] = "acceptance",
                ["primary_target_kingdom_id"] = "b", ["responding_to_offer_document_id"] = "source", ["responding_to_offer_action_id"] = id };
            WorldDiplomacyAnalysisApplication.CommitAnalysis(new() { DocumentId = d.DocumentId }, raw.ToString(), 3,
                Array.Empty<WorldDiplomacyThreat>(), key => key == "source" ? source : d, key => key == round.RoundId ? round : null,
                key => key, key => key, (_, _, _) => null, (ids, excluded) => ids.Where(x => !string.IsNullOrWhiteSpace(x)).ToList(),
                (_, _) => { }, (_, _, _, _, _, _) => { }, _ => { });
            Test.True(d.RespondingToOfferActionId == id && (id != "v2" || d.PeaceTerms.DailyTribute == 150),
                "source action is respected before inheriting terms; ambiguity never selects another action: " + id);
        }
        var changedAcceptance = PlayerDocument(); int processed = 0;
        WorldDiplomacyAnalysisApplication.CommitAnalysis(new() { DocumentId = changedAcceptance.DocumentId },
            "{\"status\":\"success\",\"intent\":\"accept_peace\",\"responding_to_offer_document_id\":\"source\",\"responding_to_offer_action_id\":\"v2\"}",
            3, Array.Empty<WorldDiplomacyThreat>(), key => key == "source" ? source : changedAcceptance, _ => round,
            id => id, id => id, (_, _, _) => new() { DailyTribute = 80 }, (ids, _) => ids.ToList(),
            (_, _) => { }, (_, _, _, _, _, _) => processed++, _ => { });
        Test.True(changedAcceptance.AnalysisStatus == "analysis_failed" && !changedAcceptance.PlayerAnalysisCommitted && processed == 0,
            "acceptance with changed explicit clauses is not silently converted to the original offer");
    }

    // Oral commitment publishes its proposal in an independent round; the player's acceptance is filed under a
    // fresh provisional round. Binding must find the offer across live rounds once the player court knows it.
    private static void VerifyCrossRoundOralAcceptance()
    {
        foreach (string kind in new[] { "annexation", "peace" })
        {
            var (_, owner) = ConcurrentOralMigrationReplay.Fixture();
            var oral = owner.EnsureActiveRound("a", "p", false);
            var source = new WorldDiplomacyDocument { DocumentId = "oral-" + kind, RoundId = oral.RoundId, AuthorKingdomId = "a",
                TargetKingdomId = "p", IsReadyForPublication = true, Intent = "propose_" + kind,
                TreatyTerms = kind == "annexation" ? new() { ReceivingKingdomId = "a", JoiningKingdomId = "p" } : null };
            owner.CurrentStorage.Documents.Add(source);
            oral.PendingOffers.Add(new() { SourceDocumentId = source.DocumentId, SourceActionId = "", ProposerKingdomId = "a",
                TargetKingdomId = "p", Intent = "propose_" + kind, Status = "open" });
            var provisional = owner.EnsureActiveRound("p", null, true);
            Test.True(provisional != oral, "player acceptance starts in its own provisional round: " + kind);
            WorldDiplomacyDocument Accept(string id)
            {
                var d = new WorldDiplomacyDocument { DocumentId = id, RoundId = provisional.RoundId, AuthorKingdomId = "p",
                    IsPlayerAuthored = true, IsReadyForPublication = true, AnalysisStatus = "pending_analysis", Body = "我方接受。" };
                owner.CurrentStorage.Documents.Add(d);
                var raw = new JObject { ["status"] = "success", ["intent"] = "accept_" + kind, ["commitment"] = "acceptance",
                    ["primary_target_kingdom_id"] = "a" };
                WorldDiplomacyAnalysisApplication.CommitAnalysis(new() { DocumentId = d.DocumentId }, raw.ToString(), 3,
                    owner.CurrentStorage.DiplomaticThreats, owner.ResolveDocument, owner.ResolveRound, x => x, x => x,
                    (_, _, _) => null, (ids, excluded) => ids.Where(x => !string.IsNullOrWhiteSpace(x) && x != excluded).ToList(),
                    (_, _) => { }, (_, _, _, _, _, _) => { }, _ => { }, owner.PlayerAnalysisOffers);
                return d;
            }
            var early = Accept("early-" + kind);
            Test.True(string.IsNullOrEmpty(early.RespondingToOfferDocumentId),
                "an offer the player court has not received is never bound: " + kind);
            WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(owner.CurrentStorage.KingdomKnowledge, "p", source.DocumentId, 12);
            var bound = Accept("bound-" + kind);
            Test.True(bound.RespondingToOfferDocumentId == source.DocumentId && bound.TargetKingdomId == "a" && bound.IsResponse,
                "known oral offer in another live round binds the player's acceptance: " + kind);
        }
        Test.True(WorldDiplomacyAnalysisApplication.DescribeRejectedPlayerMechanic("player_action_player_offer_response_missing_source_offer")
                .Contains("正式提案")
            && WorldDiplomacyAnalysisApplication.DescribeRejectedPlayerMechanic("treaty_roles_must_match_participants").Contains("接收国"),
            "rejected player mechanics expose a specific readable reason instead of one generic message");
    }

    // Real analysis, owner binding, legal-list, execution and offer-settlement code;
    // the game effect is a counted fake, never a live campaign or LLM acceptance claim.
    private sealed class AcceptanceExecution : FakeOrchestration
    {
        internal readonly WorldDiplomacyOrchestration Owner;
        internal readonly Dictionary<string, int> Reputation = new() { ["p"] = 50 };
        internal int Effects;
        internal string Rejection;
        internal AcceptanceExecution(WorldDiplomacyOrchestration owner) => Owner = owner;
        public override bool TryGetPlayerWorldStateIntentViolation(WorldDiplomacyDocument document,
            string intent, string commitment, string author, string target, out string reason)
            => WorldDiplomacyGenerationValidationRules.TryGetPlayerWorldStateIntentViolation(
                document, intent, commitment, author, target, true, _ => (false, ""), (_, _) => (false, ""),
                Owner.ResolveRound, Owner.ResolveDocument, out reason);
        public override List<string> BuildLegalDiplomaticActionIntents(WorldDiplomacyRound round, string author, string target)
            => WorldDiplomacyRoundLifecycleRules.BuildLegalDiplomaticActionIntents(round, author, target,
                () => new() { "propose_peace" }, Owner.ResolveDocument);
        public override WorldDiplomacyOfferOutcome TrySettleRelayOffer(WorldDiplomacyDocument document)
            => WorldDiplomacyOfferApplication.Settle(Owner.ResolveRound(document.RoundId), document, _ => { },
                (_, _) => (false, ""), Owner.ResolveDocument, _ => true,
                (_, _, _, response) => { Effects++; response.ChangedDiplomaticState = true; return WorldDiplomacyOfferOutcome.Applied; },
                (_, _) => false, _ => { });
        public override void SettleInternationalReputationForDocument(WorldDiplomacyDocument document)
            => WorldDiplomacyReputationRules.SettleInternationalReputationForDocument(Reputation, document, x => x, _ => { });
        public override void SuppressInvalidDocumentBeforePropagation(WorldDiplomacyDocument document, string reason)
        {
            Rejection = reason;
            WorldDiplomacyAnalysisApplication.PreservePublishedPlayerDocumentAfterRejectedMechanic(
                new DocumentExecutionReplay.Port { Owner = Owner }, this, document, reason);
        }
    }

    private static void VerifyAcceptanceRoutingAndExecution()
    {
        foreach (string scenario in new[] { "normal", "wrong-discussion", "wrong-discussion-round", "closed", "unknown-source", "ambiguous" })
        {
            var (_, owner) = ConcurrentOralMigrationReplay.Fixture();
            var original = owner.EnsureActiveRound("a", "p", false);
            var source = new WorldDiplomacyDocument { DocumentId = "peace-source", RoundId = original.RoundId,
                AuthorKingdomId = "a", TargetKingdomId = "p", IsReadyForPublication = true, Intent = "propose_peace" };
            original.PendingOffers.Add(new() { SourceDocumentId = source.DocumentId, Intent = "propose_peace",
                ProposerKingdomId = "a", TargetKingdomId = "p", Status = scenario == "closed" ? "withdrawn" : "open" });
            var unrelated = owner.EnsureActiveRound("b", "p", false);
            var discussion = new WorldDiplomacyDocument { DocumentId = "unrelated", RoundId = unrelated.RoundId,
                AuthorKingdomId = "b", TargetKingdomId = "p", IsReadyForPublication = true, Intent = "propose_trade" };
            owner.CurrentStorage.Documents.Add(source); owner.CurrentStorage.Documents.Add(discussion);
            WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(owner.CurrentStorage.KingdomKnowledge, "p", source.DocumentId, 12);
            var provisional = owner.EnsureActiveRound("p", null, true);
            var document = new WorldDiplomacyDocument { DocumentId = "acceptance", RoundId = provisional.RoundId,
                AuthorKingdomId = "p", IsPlayerAuthored = true, IsReadyForPublication = true, Body = "接受原案", AnalysisStatus = "pending_analysis" };
            owner.CurrentStorage.Documents.Add(document);
            if (scenario == "ambiguous")
            {
                owner.CurrentStorage.Documents.Add(new() { DocumentId = "peace-second", RoundId = unrelated.RoundId,
                    AuthorKingdomId = "a", TargetKingdomId = "p", IsReadyForPublication = true, Intent = "propose_peace" });
                unrelated.PendingOffers.Add(new() { SourceDocumentId = "peace-second", Intent = "propose_peace",
                    ProposerKingdomId = "a", TargetKingdomId = "p", Status = "open" });
                WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(owner.CurrentStorage.KingdomKnowledge, "p", "peace-second", 12);
            }
            var raw = new JObject { ["status"] = "success", ["intent"] = "accept_peace", ["commitment"] = "acceptance",
                ["primary_target_kingdom_id"] = "Realm-A", ["international_reputation_delta"] = 2,
                ["international_reputation_reason"] = "履行和平承诺",
                ["responding_to_offer_document_id"] = scenario == "ambiguous" ? "" : scenario == "unknown-source" ? "missing" : source.DocumentId };
            if (scenario == "wrong-discussion") raw["related_public_document_id"] = discussion.DocumentId;
            if (scenario.StartsWith("wrong-discussion")) raw["related_round_id"] = unrelated.RoundId;
            var execution = new AcceptanceExecution(owner);
            var port = new DocumentExecutionReplay.Port { Owner = owner };
            void Process(WorldDiplomacyDocument d, string intent, string commitment, bool response, string tone, float confidence)
            {
                typeof(WorldDiplomacyOrchestration).GetMethod("BindPlayerDeclarationToSharedEvent",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(owner, new object[] { d });
                WorldDiplomacyDocumentExecutionApplication.ProcessAnalyzedDocument(port, execution, d, intent, commitment, response, tone, confidence);
            }
            void Commit() => WorldDiplomacyAnalysisApplication.CommitAnalysis(new() { DocumentId = document.DocumentId }, raw.ToString(), 3,
                owner.CurrentStorage.DiplomaticThreats, owner.ResolveDocument, owner.ResolveRound,
                x => x == "Realm-A" ? "a" : x, x => x, (_, _, _) => null,
                (ids, excluded) => ids.Where(x => !string.IsNullOrWhiteSpace(x) && x != excluded).ToList(),
                execution.SuppressInvalidDocumentBeforePropagation, Process, _ => { }, owner.PlayerAnalysisOffers);
            Commit();
            bool valid = scenario == "normal" || scenario.StartsWith("wrong-discussion");
            string expectedRejection = scenario == "ambiguous"
                ? "player_action_not_executable:player_offer_response_missing_source_offer"
                : "final_live_legal_action_guard";
            Test.True(document.TargetKingdomId == "a", "target alias canonicalized before source binding: " + scenario);
            Test.True(valid ? document.RoundId == original.RoundId && original.PendingOffers[0].Status == "accepted"
                && execution.Effects == 1 && document.ChangedDiplomaticState && execution.Reputation["p"] == 52
                : execution.Rejection == expectedRejection && execution.Effects == 0
                && !document.ChangedDiplomaticState && execution.Reputation["p"] == 50
                && document.InternationalReputationEvaluationDelta == 0,
                "analysis through settlement respects exact source and actual effects: " + scenario
                + $" rejection={execution.Rejection} effects={execution.Effects} changed={document.ChangedDiplomaticState} reputation={execution.Reputation["p"]} delta={document.InternationalReputationEvaluationDelta} source={document.RespondingToOfferDocumentId} round={document.RoundId}");
            Commit(); execution.SettleInternationalReputationForDocument(document);
            Test.True(execution.Effects == (valid ? 1 : 0) && execution.Reputation["p"] == (valid ? 52 : 50),
                "repeated analysis and reputation settlement cannot replay effects: " + scenario);
        }
    }

    private static void VerifyPrompt()
    {
        var world = new PromptWorldFixture(); var orch = new PromptOrch(world);
        world.Document.Body = new string('甲', 5900) + "自今日起，我国正式向西帝国宣战。";
        string prompt = WorldDiplomacyPromptComposer.BuildAnalysisPrompt(world, orch, world.Document);
        Test.True(prompt.Contains(world.Document.Body), "full accepted player body including trailing action reaches analysis");
        var schema = JObject.Parse(WorldDiplomacyPromptContractRules.BuildAnalysisModeContract().Split('\n').Last());
        var intents = ((string)schema["intent"]).Split('|');
        foreach (string kind in new[] { "annexation", "tributary", "garrison", "vassal" })
            foreach (string move in new[] { "propose_", "accept_", "reject_" })
                Test.True(intents.Contains(move + kind), "analysis schema includes " + move + kind);
        Test.True(intents.Contains("withdraw_offer") && schema["treaty_terms"] != null
            && schema["responding_to_offer_action_id"] != null && prompt.Contains("|动作=action-b"),
            "analysis schema and actual offer context contain executable source identity");
    }

    private static void VerifyTreatiesAndWithdrawal()
    {
        var (host, owner) = ConcurrentOralMigrationReplay.Fixture(); host.PublishEnabled = true;
        foreach (string body in new[] { "p愿奉a为宗主。", "a将成为p的宗主。", "我方愿在贵国庇护下归附。" })
        {
            var d = new WorldDiplomacyDocument { IsPlayerAuthored = true, Body = body };
            Test.True(owner.ValidateFormalTreatyDeclaration(d, "propose_vassal", new() { JoiningKingdomId = "p", ReceivingKingdomId = "a" },
                "p", "a", "", "", out _), "valid player roles are not vetoed by wording: " + body);
        }
        host.Cycle = true;
        Test.True(!owner.ValidateFormalTreatyDeclaration(new() { IsPlayerAuthored = true }, "propose_vassal",
            new() { JoiningKingdomId = "p", ReceivingKingdomId = "a" }, "p", "a", "", "", out _), "real treaty cycle remains blocked");
        host.Cycle = false;
        var round = owner.EnsureActiveRound("p", "a", true);
        var source = new WorldDiplomacyDocument { DocumentId = "own-offer", RoundId = round.RoundId, AuthorKingdomId = "p",
            TargetKingdomId = "a", IsReadyForPublication = true, Intent = "propose_trade" };
        owner.CurrentStorage.Documents.Add(source);
        var offer = new WorldDiplomacyRoundOffer { SourceDocumentId = source.DocumentId, SourceActionId = "own-v2", ProposerKingdomId = "p",
            TargetKingdomId = "a", Intent = "propose_trade", Status = "open" };
        round.PendingOffers.Add(offer);
        var ownRound = owner.EnsureActiveRound("p", "a", true);
        var ownProposal = new WorldDiplomacyDocument { DocumentId = "new-proposal", RoundId = ownRound.RoundId,
            AuthorKingdomId = "p", TargetKingdomId = "a", Intent = "propose_trade", IsPlayerAuthored = true, IsReadyForPublication = true };
        owner.CurrentStorage.Documents.Add(ownProposal);
        typeof(WorldDiplomacyOrchestration).GetMethod("BindPlayerDeclarationToSharedEvent",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(owner, new object[] { ownProposal });
        Test.True(ownProposal.RoundId == ownRound.RoundId && ownRound.State == "active",
            "same parties and subject do not annex a new player proposal into an old AI round");
        var withdrawal = new WorldDiplomacyDocument { DocumentId = "withdraw", AuthorKingdomId = "p", TargetKingdomId = "a", IsPlayerAuthored = true,
            IsReadyForPublication = true, Intent = "withdraw_offer", RespondingToOfferDocumentId = source.DocumentId, RespondingToOfferActionId = "own-v2" };
        owner.CurrentStorage.Documents.Add(withdrawal);
        WorldDiplomacyAnalysisApplication.CommitAnalysis(new() { DocumentId = withdrawal.DocumentId },
            "{\"status\":\"success\",\"intent\":\"withdraw_offer\",\"primary_target_kingdom_id\":\"a\",\"responding_to_offer_document_id\":\"own-offer\",\"responding_to_offer_action_id\":\"own-v2\"}",
            3, owner.CurrentStorage.DiplomaticThreats, owner.ResolveDocument, owner.ResolveRound, id => id, id => id,
            (_, _, _) => null, (ids, excluded) => ids.Where(x => !string.IsNullOrWhiteSpace(x)).ToList(), (_, _) => { },
            owner.ProcessAnalyzedDocument, _ => { });
        Test.True(offer.Status == "withdrawn", "actual orchestration withdraws exact player source action");
    }
}
