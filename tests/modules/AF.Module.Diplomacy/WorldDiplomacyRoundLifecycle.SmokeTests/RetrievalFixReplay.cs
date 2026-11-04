using AnimusForge;
using AnimusForge.Refactor.Domain;

// Diplomatic recall: bekko threshold, vector failure fallback, live-conversation
// partial vectors, empty-topic NPC agenda query, exact identifier boost, aid privacy.
internal static class RetrievalFixReplay
{
    internal static void Run()
    {
        Threshold(); TimeoutFallsBackToLexical(); ConversationUsesCompletedVectors();
        AgendaQuery(); ExactIdentifierBoost(); AidPrivacy(); DocumentIndexReplacement();
        Console.WriteLine("PASS retrieval fixes: bekko threshold, lexical fallback, agenda query, id boost, aid privacy, index replacement");
    }

    private static WorldDiplomacyMemorySnapshot View(WorldDiplomacyStorage storage, string kingdom, params string[] known)
        => new(storage, kingdom, new HashSet<string>(known), kingdom + "_ruler");

    private static void Threshold()
    {
        var storage = new WorldDiplomacyStorage();
        storage.EventAnnouncements.Add(new() { Id = "seal", EventId = "seal", State = "public", Title = "The broken seal", Body = "harbor dispute" });
        // cos = 0.5: passes the bekko calibration, failed the old 0.65 gate.
        float[] Embed(string text) => text == "silent emissary" ? new[] { 1f, 0f } : new[] { 0.5f, 0.8660254f };
        // Mirrors WorldNewsRecallPolicy.SemanticOnlyMinCosine (bekko-a25m calibration).
        Test.True(WorldDiplomacyMemoryApplication.SemanticMinCosine == 0.40, "diplomatic recall uses the bekko-calibrated cosine gate");
        Test.True(WorldDiplomacyMemoryApplication.Build(View(storage, "x"), "silent emissary", d => d.ToString(), 7000, Embed).Contains("The broken seal"),
            "a 0.5 cosine semantic-only match is recalled");
    }

    private static void TimeoutFallsBackToLexical()
    {
        var storage = new WorldDiplomacyStorage();
        storage.EventAnnouncements.Add(new() { Id = "envoy", EventId = "envoy", State = "public", Title = "Envoy dispute", Body = "envoy seized" });
        int calls = 0;
        float[] Timeout(string text) { calls++; if (text == "envoy") return new[] { 1f, 0f }; throw new WorldDiplomacyPreparationException("diplomacy_embedding_timeout"); }
        string first = WorldDiplomacyMemoryApplication.Build(View(storage, "x"), "envoy", d => d.ToString(), 7000, Timeout);
        Test.True(first.Contains("Envoy dispute"), "a timed-out record vector degrades to lexical recall instead of failing the request");
        int after = calls;
        WorldDiplomacyMemoryApplication.Build(View(storage, "x"), "envoy", d => d.ToString(), 7000, Timeout);
        Test.True(calls == after + 1, "a failed record is not re-embedded every request; only the query is");
        Test.True(WorldDiplomacyMemoryApplication.Build(View(storage, "x"), "envoy", d => d.ToString(), 7000,
            _ => throw new WorldDiplomacyPreparationException("diplomacy_embedding_timeout")).Contains("Envoy dispute"),
            "a timed-out query vector keeps lexical recall");
    }

    private sealed class Conversation : IWorldDiplomacyMemorySource, IWorldDiplomacySemanticSource
    {
        internal readonly WorldDiplomacyStorage Storage = new();
        public bool TryCapture(string heroId, string kingdom, out WorldDiplomacyMemorySnapshot snapshot)
        { snapshot = new(Storage, kingdom, new HashSet<string>()); return true; }
        public string FormatDate(int day) => day.ToString();
        public float[] Embed(string text) => throw new WorldDiplomacyPreparationPendingException();
    }

    private static void ConversationUsesCompletedVectors()
    {
        var source = new Conversation();
        source.Storage.EventAnnouncements.Add(new() { Id = "toll", EventId = "toll", State = "public", Title = "Bridge toll", Body = "bridge toll raised" });
        string text = "";
        bool threw = false;
        try { text = WorldDiplomacyMemoryApplication.Build(source, "hero", "x", "bridge toll", null, false); }
        catch (WorldDiplomacyPreparationPendingException) { threw = true; }
        Test.True(!threw && text.Contains("Bridge toll"), "a live conversation never throws pending; it answers with lexical/completed vectors");
    }

    private static void AgendaQuery()
    {
        var storage = new WorldDiplomacyStorage();
        storage.Documents.Add(new() { DocumentId = "old", AuthorKingdomId = "a", TargetKingdomId = "p", RoundId = "past",
            Title = "Grain embargo", Body = "We stop the grain.", IsReadyForPublication = true, Day = 3 });
        storage.Documents.Add(new() { DocumentId = "other", AuthorKingdomId = "c", TargetKingdomId = "d", RoundId = "x",
            Title = "Unrelated title", Body = "UNRELATED", IsReadyForPublication = true, Day = 4 });
        storage.RoundSummaries.Add(new() { RoundId = "past", KingdomIds = new() { "a", "p" }, SourceDocumentIds = new() { "old" },
            Summary = "Grain embargo AGENDA_HISTORY" });
        storage.ConcurrentRounds.Add(new() { RoundId = "agenda", State = "active", RoundTopic = "" });
        var job = new WorldDiplomacyJob { Kind = "generate", RoundId = "agenda", AuthorKingdomId = "a", TargetKingdomId = "p" };
        string context = WorldDiplomacyRequestHistoryApplication.Build(storage, job, 10);
        Test.True(context.Contains("AGENDA_HISTORY") && !context.Contains("UNRELATED"),
            "an empty-topic NPC agenda recalls the pair's history through its own known titles");
        var round = storage.ConcurrentRounds[0];
        round.TopicSeedContext = "harbor rights";
        Test.True(WorldDiplomacyRequestHistoryApplication.BuildRecallQuery(storage, job, round, null, _ => true) == "harbor rights",
            "the agenda seed is the recall subject before document titles");
        round.RoundTopic = new string('议', 3000);
        Test.True(WorldDiplomacyRequestHistoryApplication.BuildRecallQuery(storage, job, round, null, _ => true).Length
            == WorldDiplomacyRequestHistoryApplication.RecallQueryCharacterLimit, "recall query is bounded");
        Test.True(WorldDiplomacyRequestHistoryApplication.BuildRecallQuery(storage, job, null, null, _ => true) == "",
            "without a live round there is still no historical filler");
        Test.True(WorldDiplomacyMemoryApplication.ClipEmbeddingInput(new string('a', 1399) + "😀").Length == 1399,
            "embedding input clipping never splits a surrogate pair");
    }

    private static void ExactIdentifierBoost()
    {
        var storage = new WorldDiplomacyStorage();
        storage.Documents.Add(new() { DocumentId = "doc_1", RoundId = "r", Title = "Treaty alpha", Body = "treaty", IsReadyForPublication = true, Day = 9 });
        storage.Documents.Add(new() { DocumentId = "doc_12", RoundId = "r", Title = "Treaty beta", Body = "treaty", IsReadyForPublication = true, Day = 9 });
        string text = WorldDiplomacyMemoryApplication.Build(View(storage, "x", "doc_1", "doc_12"), "treaty doc_12", d => d.ToString(), 700);
        int beta = text.IndexOf("Treaty beta", StringComparison.Ordinal), alpha = text.IndexOf("Treaty alpha", StringComparison.Ordinal);
        Test.True(beta >= 0 && (alpha < 0 || beta < alpha), "only the exactly cited id is boosted; doc_1 is not a substring hit of doc_12");
    }

    private static void AidPrivacy()
    {
        var storage = new WorldDiplomacyStorage();
        storage.AidRequests.Add(new() { Id = "aid1", CallingKingdomId = "a", CalledKingdomId = "b", EnemyKingdomId = "e",
            Status = "accepted", AcceptedRulerId = "b_ruler", RequestDocumentId = "req", AnswerDocumentId = "ans",
            LegacyCost = 777777, LegacyPlayerPaying = true });
        string Third(params string[] known) => WorldDiplomacyMemoryApplication.Build(View(storage, "c", known), "ally aid", d => d.ToString());
        string requestOnly = Third("req");
        Test.True(requestOnly.Contains("aid_request") && !requestOnly.Contains("accepted") && !requestOnly.Contains("777777"),
            "a third realm that saw only the request learns no answer, cost or payer");
        string answered = Third("req", "ans");
        Test.True(answered.Contains("accepted") && !answered.Contains("777777") && !answered.Contains("LegacyPlayerPaying"),
            "after the answer is delivered, status is public but cost and payer stay private");
        Test.True(WorldDiplomacyMemoryApplication.Build(View(storage, "a"), "ally aid", d => d.ToString()).Contains("777777"),
            "the calling realm keeps the complete record");
    }

    private static void DocumentIndexReplacement()
    {
        var storage = new WorldDiplomacyStorage();
        storage.Documents.Add(new() { DocumentId = "s", AuthorKingdomId = "b", RoundId = "r", Title = "t", Body = "FIRST", IsReadyForPublication = true });
        WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(storage.KingdomKnowledge, "a", "s", 1);
        var job = new WorldDiplomacyJob { Kind = "generate", RoundId = "r", AuthorKingdomId = "a", TargetKingdomId = "b", SourceDocumentId = "s" };
        Test.True(WorldDiplomacyRequestHistoryApplication.Build(storage, job, 1).Contains("FIRST"), "initial source indexed");
        storage.Documents[0] = new() { DocumentId = "s", AuthorKingdomId = "b", RoundId = "r", Title = "t", Body = "REPLACED", IsReadyForPublication = true };
        Test.True(WorldDiplomacyRequestHistoryApplication.Build(storage, job, 1).Contains("REPLACED"),
            "same-count in-place replacement rebuilds the document index");
    }
}
