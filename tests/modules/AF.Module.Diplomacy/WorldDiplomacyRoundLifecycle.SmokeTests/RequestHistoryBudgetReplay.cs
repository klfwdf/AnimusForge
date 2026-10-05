using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class RequestHistoryBudgetReplay
{
    private sealed class Port : IWorldDiplomacyHistoryCapturePort
    {
        public int CurrentHour() => 100 * 24;
        public long WeeklyRevision() => 0;
        public IEnumerable<WorldDiplomacyWeeklyArtifact> WeeklyArtifacts() => Array.Empty<WorldDiplomacyWeeklyArtifact>();
    }
    private sealed class Orch : FakeOrchestration
    {
        public override void EnsureCanonicalHistoryInitialized() { }
        public override string BuildCanonicalHistoryBlock(long throughSequence) => throw new Exception("declaration read full archive");
    }
    private static WorldDiplomacyDocument Doc(string id, string author, string target, string round, int day, string body) =>
        new() { DocumentId = id, AuthorKingdomId = author, TargetKingdomId = target, RoundId = round,
            Day = day, CreatedUtcTicks = day, Body = body, IsReadyForPublication = true };

    internal static void Run()
    {
        var storage = new WorldDiplomacyStorage();
        storage.CanonicalHistory.Snapshot.Content = new string('界', 800000);
        storage.CanonicalHistory.Snapshot.CoveredThroughSequence = 6205;
        storage.CanonicalHistory.NextSequence = 7920;
        for (int i = 0; i < 4000; i++)
            storage.Documents.Add(Doc("other" + i, "c", "d", "unrelated", 100, "UNRELATED_WORLD_RULES"));
        storage.Documents.Add(Doc("old", "a", "b", "old-round", 1, "OLD_RAW_DECLARATION"));
        storage.Documents.Add(Doc("source", "b", "a", "current", 2, "EXACT_ORIGINAL_PROPOSAL"));
        WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(storage.KingdomKnowledge, "a", "source", 2);
        storage.Documents.Add(Doc("recent", "a", "b", "current", 99, "RECENT_REPLY"));
        storage.Documents.Add(Doc("recent-other-event", "a", "b", "another", 98, "RECENT_BILATERAL_EVENT"));
        for (int i = 0; i < 100; i++) storage.Documents.Add(Doc("third" + i, "a", "c", "other", 100, "UNRELATED_BILATERAL"));
        storage.RoundSummaries.Add(new() { RoundId = "old-round", CreatedDay = 3, KingdomIds = new() { "a", "b" }, Summary = "RELEVANT_OLD_SUMMARY" });
        storage.RoundSummaries.Add(new() { RoundId = "other-summary", CreatedDay = 100, KingdomIds = new() { "c", "d" }, Summary = "UNRELATED_SUMMARY" });
        var job = new WorldDiplomacyJob { Kind = "generate", AuthorKingdomId = "a", TargetKingdomId = "b",
            RoundId = "current", SourceDocumentId = "source", SystemPrompt = "rules", UserPrompt = "current facts" };
        WorldDiplomacyHistoryCaptureApplication.Capture(storage, job, false, long.MaxValue, new Port(), new Orch());
        string context = job.DeclarationHistoryBlock;
        Test.True(context.Contains("EXACT_ORIGINAL_PROPOSAL") && context.Contains("RECENT_REPLY") && context.Contains("RELEVANT_OLD_SUMMARY"), "reading window retains original old source, current event and related summary");
        Test.True(context.Contains("RECENT_BILATERAL_EVENT") && !context.Contains("UNRELATED_BILATERAL"), "bilateral index finds relevant event even when third-party declarations dominate recent country records");
        Test.True(!context.Contains("UNRELATED_WORLD_RULES") && !context.Contains("UNRELATED_SUMMARY") && !context.Contains("OLD_RAW_DECLARATION"), "reading window excludes unrelated and stale raw history");
        Test.True(storage.CanonicalHistory.Snapshot.Content.Length == 800000 && storage.Documents.Count == 4104
            && storage.CanonicalHistory.Snapshot.CoveredThroughSequence == 6205, "selection leaves archive and compression coverage intact");
        var messages = WorldDiplomacyLlmMessageApplication.BuildLlmMessageArray(job, _ => throw new Exception("full history renderer called"));
        Test.True(messages.Count == 3 && messages[1]?["content"]?.ToString() == context && messages.ToString().Length < 5000, "800k archive produces a small real declaration request");
        Test.True(!JsonConvert.SerializeObject(job).Contains("RELEVANT_OLD_SUMMARY"), "reading window is runtime-only and not another persisted history copy");
        var loaded = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(storage))!;
        Test.True(WorldDiplomacyRequestHistoryApplication.Build(loaded, job, 100) == context, "save/load rebuilds the same reading window without rewriting history");
        storage.Documents.Add(Doc("new", "b", "a", "current", 100, "APPENDED_REPLY"));
        Test.True(!WorldDiplomacyRequestHistoryApplication.Build(storage, job, 100).Contains("APPENDED_REPLY"),
            "recent context does not leak an unreceived same-event reply");
        WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(storage.KingdomKnowledge, "a", "new", 100);
        Test.True(WorldDiplomacyRequestHistoryApplication.Build(storage, job, 100).Contains("APPENDED_REPLY"), "incremental index consumes new documents");
        for (int i = 0; i < 40; i++) storage.Documents.Add(Doc("large" + i, "a", "b", "current", 100, new string('文', 100000)));
        string bounded = WorldDiplomacyRequestHistoryApplication.Build(storage, job, 100);
        Test.True(bounded.Length <= WorldDiplomacyRequestHistoryApplication.ContextCharacterLimit && bounded.Contains("EXACT_ORIGINAL_PROPOSAL"), "large current history is bounded with source document retained first");
        var loadedJob = JsonConvert.DeserializeObject<WorldDiplomacyJob>(JsonConvert.SerializeObject(job))!;
        loadedJob.SystemPrompt = new string('s', 10000);
        loadedJob.UserPrompt = new string('u', 13000);
        WorldDiplomacyHistoryCaptureApplication.Capture(storage, loadedJob, false, long.MaxValue, new Port(), new Orch(), s => s.Length);
        Test.True(loadedJob.DeclarationHistoryBlock.Length <= 4904 && loadedJob.SystemPrompt.Length == 10000
            && loadedJob.UserPrompt.Length == 13000, "request window adapts to rules/current-tail size and reserves correction room without truncating either");
        int failures = 0, rebuilds = 0, compactCalls = 0;
        bool Fits(WorldDiplomacyJob request, int length) => WorldDiplomacyLlmDispatchApplication.EnsureRequestFitsInputBudget(
            request, new JArray(new JObject { ["role"] = "user", ["content"] = new string('x', length) }),
            800000, 48000, s => s.Length, _ => throw new Exception("full history budget scan"),
            _ => { rebuilds++; return true; }, (_, _) => failures++, () => compactCalls++, _ => { });
        Test.True(Fits(job, 31000) && !Fits(job, 40000), "declaration total budget is independent of 800k archive threshold");
        var repair = new WorldDiplomacyJob { Kind = "generate", SemanticRepairAttempts = 1 };
        Test.True(!Fits(repair, 600000) && repair.SemanticRepairAttempts == 1 && rebuilds == 0 && compactCalls == 0, "oversized repair never sends, recompacts or resets retry allowance");
        var compact = new WorldDiplomacyJob { Kind = "compress" };
        Test.True(Fits(compact, 120000) && !Fits(compact, 140000), "compression retains a separate bounded archive input budget");
        var waiting = new WorldDiplomacyJob { Kind = "generate", JobId = "old-waiting", AwaitingHistoryCompression = true, InputBudgetHistoryTargetTokens = 4000 };
        var queue = new WorldDiplomacyStorage(); queue.Jobs.Add(waiting);
        Test.True(ReferenceEquals(WorldDiplomacyJobSelectionView.For(queue).Select(queue, 1, ""), waiting)
            && !waiting.AwaitingHistoryCompression && waiting.InputBudgetHistoryTargetTokens == 0, "old declaration waiting on global compression becomes runnable");
        Console.WriteLine("Request history replay: 800k archive -> " + context.Length + " context characters; bounded large window=" + bounded.Length);
        CompressionBatches();
        LegacyRequest();
        DeliveredVisibility();
    }

    private static void DeliveredVisibility()
    {
        var storage = new WorldDiplomacyStorage();
        storage.Documents.Add(Doc("undelivered", "b", "a", "current", 100, "NOT_YET_DELIVERED"));
        storage.RoundSummaries.Add(new() { RoundId = "old", KingdomIds = new() { "a", "b" },
            SourceDocumentIds = new() { "undelivered" }, Summary = "HIDDEN_LINKED_SUMMARY" });
        storage.CanonicalHistory.Snapshot.ProtectedFacts.Add(new() { SourceId = "undelivered",
            AuthorKingdomId = "b", TargetKingdomIds = new() { "a" }, Text = "HIDDEN_LINKED_FACT" });
        var job = new WorldDiplomacyJob { Kind = "generate", AuthorKingdomId = "a", TargetKingdomId = "b",
            RoundId = "current", SourceDocumentId = "undelivered" };
        WorldDiplomacyHistoryCaptureApplication.Capture(storage, job, false, long.MaxValue, new Port(), new Orch());
        Test.True(!job.DeclarationHistoryBlock.Contains("NOT_YET_DELIVERED")
            && !job.DeclarationHistoryBlock.Contains("HIDDEN_LINKED"), "real request capture cannot bypass delivery through a source, summary or protected fact");
        WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(storage.KingdomKnowledge, "a", "undelivered", 100);
        WorldDiplomacyHistoryCaptureApplication.Capture(storage, job, false, long.MaxValue, new Port(), new Orch());
        Test.True(job.DeclarationHistoryBlock.Contains("NOT_YET_DELIVERED")
            && job.DeclarationHistoryBlock.Contains("HIDDEN_LINKED_SUMMARY") && job.DeclarationHistoryBlock.Contains("HIDDEN_LINKED_FACT"),
            "delivery changes the reading window without rewriting canonical history or its index");
        Test.True(storage.RoundSummaries.Count == 1 && storage.CanonicalHistory.Snapshot.ProtectedFacts.Count == 1,
            "visibility selection retains long-term summary and protected fact storage");
    }

    private static void CompressionBatches()
    {
        var store = new WorldDiplomacyStorage();
        for (int i = 1; i <= 100; i++)
        {
            var entry = new WorldDiplomacyCanonicalHistoryEntry { Sequence = i, Day = i, SourceId = "entry" + i,
                Kind = "declaration", Text = new string('x', 9000) };
            entry.EstimatedTokens = WorldDiplomacyCanonicalRenderRules.RenderCanonicalHistoryEntry(entry).Length;
            store.CanonicalHistory.DeltaEntries.Add(entry);
        }
        store.CanonicalHistory.NextSequence = 101;
        store.CanonicalHistory.EstimatedTokens = store.CanonicalHistory.DeltaEntries.Sum(e => e.EstimatedTokens);
        WorldDiplomacyJob Enqueue()
        {
            WorldDiplomacyHistoryCompressionApplication.EnqueueCompressionJob(store, 100, store.CanonicalHistory.EstimatedTokens, 48000,
                () => { }, () => (120, 240), () => "rules", s => s.Length, 800000, 100, 2048, 48, 24,
                () => 2400, () => 100, () => 64000, p => p + store.CompressionSequence,
                (j, cutoff) => j.HistoryThroughSequence = cutoff, _ => { });
            return store.Jobs.Last();
        }
        var first = Enqueue();
        long cutoff = first.CompressionThroughSequence;
        Test.True(cutoff > 0 && cutoff < 100 && first.CompressionTokenCount < 128000,
            "800k compression trigger selects a bounded continuous input batch");
        var messages = WorldDiplomacyLlmMessageApplication.BuildLlmMessageArray(first,
            through => WorldDiplomacyCanonicalHistoryRules.RenderCanonicalHistoryBlock(store.CanonicalHistory, through));
        Test.True(WorldDiplomacyLlmDispatchApplication.EnsureRequestFitsInputBudget(first, messages, 800000, 48000,
            s => s.Length, _ => "", _ => true, (_, reason) => throw new Exception(reason), () => { }, _ => { }),
            "actual selected compression messages pass the separate total input cap");
        // A new event arrives while the batch is in flight.
        store.CanonicalHistory.DeltaEntries.Add(new() { Sequence = 101, SourceId = "late", Kind = "declaration", Text = "LATE_EVENT", EstimatedTokens = 30 });
        store.CanonicalHistory.NextSequence = 102;
        WorldDiplomacyHistoryCompressionApplication.CommitCompression(store, first,
            new JObject { ["summary"] = "BATCH_ONE_SUMMARY", ["covered_through_sequence"] = cutoff }.ToString(),
            () => { }, s => s.Length, () => 100, 48000, 800000, () => { }, _ => { });
        Test.True(store.CanonicalHistory.Snapshot.CoveredThroughSequence == cutoff
            && store.CanonicalHistory.DeltaEntries.All(e => e.Sequence > cutoff)
            && store.CanonicalHistory.DeltaEntries.Any(e => e.SourceId == "late"),
            "bounded batch commit advances coverage and preserves suffix plus in-flight arrival");
        store = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(store))!;
        var second = Enqueue();
        Test.True(second.CompressionThroughSequence > cutoff, "compression resumes with forward progress after save/load");
        WorldDiplomacyHistoryCompressionApplication.CommitCompression(store, second,
            new JObject { ["summary"] = "BATCH_TWO_SUMMARY", ["covered_through_sequence"] = second.CompressionThroughSequence }.ToString(),
            () => { }, s => s.Length, () => 100, 48000, 800000, () => { }, _ => { });
        Test.True(store.CanonicalHistory.Snapshot.CoveredThroughSequence == second.CompressionThroughSequence
            && store.CanonicalHistory.DeltaEntries.Any(e => e.SourceId == "late"), "second bounded compaction commits without losing later facts");
        Console.WriteLine("Compression batches: first cutoff=" + cutoff + ", second=" + second.CompressionThroughSequence);
    }

    private static void LegacyRequest()
    {
        string system = WorldDiplomacyPromptContractRules.BuildGenerationSystemPrompt("rules", 120, 240);
        var job = new WorldDiplomacyJob { JobId = "legacy-repair", Kind = "generate", SemanticRepairAttempts = 1,
            SystemPrompt = system, UserPrompt = "fix 【MODE=DECLARE】",
            CacheAffinityKey = WorldDiplomacyPromptContractRules.CanonicalHistoryCacheAffinityKey };
        string full = new string('旧', 600000);
        job.HistoryPrefixHash = WorldDiplomacyPromptContractRules.StablePromptHashPair(system, full);
        job.LlmMessages = new() { new() { Role = "system", Content = system }, new() { Role = "system", Content = full },
            new() { Role = "user", Content = "original 【MODE=DECLARE】" }, new() { Role = "assistant", Content = "rejected" },
            new() { Role = "user", Content = job.UserPrompt } };
        Test.True(WorldDiplomacyPromptContractRules.IsValidSemanticRepairMessageChain(job), "legacy persisted frozen chain is structurally valid before window migration");
        var storage = new WorldDiplomacyStorage(); storage.Jobs.Add(job);
        int rebuilds = 0, sends = 0;
        var selected = WorldDiplomacyLlmDispatchApplication.SelectAndPrepareLlmJob(storage, 2400, "",
            _ => "", _ => true, _ => true, j => { rebuilds++; j.UserPrompt = "fresh 【MODE=DECLARE】"; return true; },
            _ => null, (_, _) => { }, _ => true, () => "", consume => { if (consume) sends++; return true; },
            j => WorldDiplomacyHistoryCaptureApplication.Capture(storage, j, false, long.MaxValue, new Port(), new Orch()),
            j => WorldDiplomacyLlmMessageApplication.BuildLlmMessageArray(j, _ => throw new Exception("legacy request read full archive")),
            out var prepared, 800000, 48000, s => s.Length, _ => throw new Exception("legacy budget read archive"),
            () => throw new Exception("legacy declaration scheduled global compression"), (_, error) => throw new Exception(error),
            _ => { }, _ => { });
        Test.True(ReferenceEquals(selected, job) && rebuilds == 1 && sends == 1 && job.SemanticRepairAttempts == 0
            && prepared.Count == 3 && prepared[1]?["content"]?.ToString().StartsWith(WorldDiplomacyRequestHistoryApplication.ContextMarker) == true,
            "actual dispatch rebuilds a legacy full-history repair once and admits only the recent-window request");
        job.IsRunning = false;
        job.SemanticRepairAttempts = 1;
        string correction = "correct 【MODE=DECLARE】";
        job.LlmMessages = WorldDiplomacyPromptContractRules.CloneLlmMessages(
            WorldDiplomacyLlmMessageApplication.BuildLlmMessagesForJob(new WorldDiplomacyJob { Kind = "generate",
                SystemPrompt = job.SystemPrompt, UserPrompt = job.UserPrompt, DeclarationHistoryBlock = job.DeclarationHistoryBlock },
                _ => throw new Exception("repair read full archive")));
        job.LlmMessages.Add(new() { Role = "assistant", Content = "rejected" });
        job.LlmMessages.Add(new() { Role = "user", Content = correction });
        job.UserPrompt = correction;
        var loaded = JsonConvert.DeserializeObject<WorldDiplomacyJob>(JsonConvert.SerializeObject(job))!;
        Test.True(WorldDiplomacyPromptContractRules.IsValidSemanticRepairMessageChain(loaded)
            && ReferenceEquals(WorldDiplomacyLlmMessageApplication.BuildLlmMessagesForJob(loaded,
                _ => throw new Exception("frozen repair read full archive")), loaded.LlmMessages),
            "new frozen repair survives save/load and reuses only its bounded rejected prefix");
    }
}
