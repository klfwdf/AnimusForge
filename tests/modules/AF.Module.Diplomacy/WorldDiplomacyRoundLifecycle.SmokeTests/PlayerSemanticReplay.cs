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
