using System.Reflection;
using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json.Linq;

internal static class ReviewFixReplay
{
    private sealed class StrictIdentityHost : FakeOrchestrationHost
    {
        public override string ResolvePartyId(string id) => id == "a" || id == "b" ? id : null;
        public override string ResolvePropagationReceiverId(string kingdomId, string settlementId) => ResolvePartyId(kingdomId);
        public override string ResolveSettlementId(string id) => id == "town_A1" ? id : null;
        public override int MaxPropagationArrivalsPerDay() => 1200;
    }
    private static object Invoke(WorldDiplomacyOrchestration o, string name, params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(o, args)!;
    internal static void Run()
    {
        // Real orchestration; the host follows the production kingdom-only identity contract.
        var strict = new StrictIdentityHost();
        var actual = new WorldDiplomacyOrchestration(strict, new WorldDiplomacyRuntimeState());
        actual.CurrentStorage.Documents.Add(new() { DocumentId = "civil_doc", IsReadyForPublication = true });
        actual.CurrentStorage.PropagationArrivals.Add(new() { DocumentId = "civil_doc", SettlementId = "town_A1", Scope = "civilian", DueDay = 12 });
        actual.ProcessPropagationArrivals();
        Console.WriteLine($"CIVILIAN: queued={actual.CurrentStorage.PropagationArrivals.Count}; recorded={actual.CurrentStorage.SettlementKnowledge.Count}");
        if (actual.CurrentStorage.PropagationArrivals.Count != 0 || actual.CurrentStorage.SettlementKnowledge.Count != 1)
            throw new Exception("civilian propagation failed");
        actual.CurrentStorage.PropagationArrivals.Add(new() { DocumentId = "civil_doc", SettlementId = "missing", Scope = "civilian", DueDay = 12 });
        actual.CurrentStorage.PropagationArrivals.Add(new() { DocumentId = "civil_doc", KingdomId = "a", Scope = "court", DueDay = 12 });
        actual.ProcessPropagationArrivals();
        Test.True(actual.CurrentStorage.SettlementKnowledge.Count == 1 && actual.CurrentStorage.KingdomKnowledge.Count == 1,
            "unknown settlement is skipped while court uses kingdom identity");

        foreach (string scenario in new[] { "unknown", "unpublished", "wrong-round", "ambiguous", "counterproposal", "action" })
        {
            var (_, negative) = ConcurrentOralMigrationReplay.Fixture();
            var destination = negative.EnsureActiveRound("a", "b", false);
            var independent = negative.EnsureActiveRound("p", "b", true);
            var source = new WorldDiplomacyDocument { DocumentId = "root", RoundId = destination.RoundId,
                AuthorKingdomId = "a", TargetKingdomId = "b", IsReadyForPublication = scenario != "unpublished" };
            negative.CurrentStorage.Documents.Add(source);
            if (scenario != "unknown") WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(negative.CurrentStorage.KingdomKnowledge, "p", source.DocumentId, 12);
            var comment = new WorldDiplomacyDocument { DocumentId = "comment", RoundId = independent.RoundId,
                AuthorKingdomId = "p", TargetKingdomId = "b", IsPlayerAuthored = true, Intent = scenario == "counterproposal" ? "propose_peace" : "statement",
                DiscussionSourceDocumentId = source.DocumentId, DiscussionRoundId = scenario == "wrong-round" ? "other" : "" };
            if (scenario == "action") comment.Actions = new() { new() { Intent = "declare_war" } };
            if (scenario == "ambiguous")
            {
                var other = negative.EnsureActiveRound("c", "b", false);
                negative.CurrentStorage.Documents.Add(new() { DocumentId = "other-source", RoundId = other.RoundId, IsReadyForPublication = true });
                comment.SourceDocumentId = "other-source";
            }
            Invoke(negative, "BindPlayerDeclarationToSharedEvent", comment);
            Test.True(comment.RoundId == independent.RoundId && independent.State == "active", "unsafe or substantive statement remains independent: " + scenario);
        }

        var (h, o) = ConcurrentOralMigrationReplay.Fixture();
        var negotiation = o.EnsureActiveRound("a", "b", false);
        var provisional = o.EnsureActiveRound("p", "b", true);
        var root = new WorldDiplomacyDocument { DocumentId = "peace-root", RoundId = negotiation.RoundId,
            AuthorKingdomId = "a", TargetKingdomId = "b", Intent = "propose_peace", IsReadyForPublication = true };
        var guarantee = new WorldDiplomacyDocument { DocumentId = "guarantee", RoundId = provisional.RoundId,
            AuthorKingdomId = "p", TargetKingdomId = "b", IsPlayerAuthored = true, IsReadyForPublication = true,
            Intent = "statement", AnalysisStatus = "success", DiscussionRoundId = negotiation.RoundId,
            DiscussionSourceDocumentId = root.DocumentId, Body = "我愿意担保本次和平；B 能否取消赔款？" };
        o.CurrentStorage.Documents.AddRange(new[] { root, guarantee });
        WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(o.CurrentStorage.KingdomKnowledge, "p", root.DocumentId, 12);
        if (!WorldDiplomacyIntentVocabulary.IsSupportedDiplomacyIntent(guarantee.Intent)) throw new Exception("invalid input");
        Invoke(o, "BindPlayerDeclarationToSharedEvent", guarantee);
        Console.WriteLine($"GUARANTEE: original={negotiation.RoundId}; actual={guarantee.RoundId}; provisionalLive={provisional.State}");
        Test.True(guarantee.RoundId == negotiation.RoundId && provisional.State == "closed", "known explicit statement joins shared negotiation");

        // Both player documents already published/registered. A repair is frozen while the second travels.
        var d1 = new WorldDiplomacyDocument { DocumentId = "known-ask", RoundId = negotiation.RoundId,
            AuthorKingdomId = "p", TargetKingdomId = "b", IsReadyForPublication = true, IsPlayerAuthored = true, AnalysisStatus = "success" };
        var d2 = new WorldDiplomacyDocument { DocumentId = "late-ask", RoundId = negotiation.RoundId,
            AuthorKingdomId = "p", TargetKingdomId = "b", IsReadyForPublication = true, IsPlayerAuthored = true, AnalysisStatus = "success", Body = "请在已知条款之外明确回复这个新要求" };
        o.CurrentStorage.Documents.AddRange(new[] { d1, d2 });
        Invoke(o, "RegisterPlayerResponseWork", d1, false);
        Invoke(o, "RegisterPlayerResponseWork", d2, false);
        WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(o.CurrentStorage.KingdomKnowledge, "b", d1.DocumentId, 12);
        var repair = new WorldDiplomacyJob { JobId = "repair", Kind = "generate", RoundId = negotiation.RoundId,
            AuthorKingdomId = "b", SourceDocumentId = d1.DocumentId, SemanticRepairAttempts = 1,
            RoundConversationRevision = negotiation.ConversationRevision, UserPrompt = "frozen known-ask",
            PlayerResponseSourceIds = new() { d1.DocumentId }, LlmMessages = new() {
                new() { Role = "system", Content = "rules" }, new() { Role = "user", Content = "frozen known-ask" } } };
        o.CurrentStorage.Jobs.Add(repair);
        WorldDiplomacyPropagationApplication.ReceiveCourt(o.CurrentStorage, d2, "b", 12, () => false, () => o.ProcessCourtArrival("b", d2));
        o.PrepareSharedRequest(repair);
        bool allSourcesKnownToPrompt = repair.PlayerResponseSourceIds.All(id => repair.LlmMessages.Any(m => m.Content.Contains(id)));
        bool firstReplyAccepted = (bool)Invoke(o, "ValidatePlayerResponseCoverage", repair,
            new JObject { ["answered_player_document_ids"] = new JArray(d1.DocumentId) });
        Console.WriteLine($"FROZEN_REPAIR: sources={string.Join(',', repair.PlayerResponseSourceIds)}; containsNewBody={repair.LlmMessages.Any(m => m.Content.Contains(d2.Body))}; oldReplyAccepted={firstReplyAccepted}; revisionUnchanged={repair.RoundConversationRevision == negotiation.ConversationRevision}");
        Test.True(allSourcesKnownToPrompt && firstReplyAccepted && repair.PlayerResponseSourceIds.SequenceEqual(new[] { d1.DocumentId }), "repair keeps frozen source batch and accepts its original coverage");
        Test.True(negotiation.PlayerResponses.Any(x => x.SourceDocumentId == d2.DocumentId && x.Status == "pending"), "late source stays pending");
        DiplomacyRoundWorkRules.SealCoverage(negotiation, new WorldDiplomacyDocument { DocumentId = "reply", AuthorKingdomId = "b", IsReadyForPublication = true, AnsweredPlayerDocumentIds = new() { d1.DocumentId } });
        Test.True(DiplomacyRoundWorkRules.SelectResponseBatch(negotiation, "b", o.CurrentStorage.Documents).SequenceEqual(new[] { d2.DocumentId }), "next response batch includes the late source");
        repair.PlayerResponseSourceIds.Add(d2.DocumentId); // Legacy corrupted saved repair.
        o.ResetRuntimeState();
        Test.True(repair.SemanticRepairAttempts == 0 && repair.LlmMessages.Count == 0
            && negotiation.PlayerResponses.Any(x => x.SourceDocumentId == d2.DocumentId && x.Status == "pending"),
            "load reset discards unsafe frozen repair messages while retaining reply obligations");
    }
}
