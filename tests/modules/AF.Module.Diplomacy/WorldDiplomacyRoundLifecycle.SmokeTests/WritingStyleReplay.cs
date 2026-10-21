using AnimusForge;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Persistence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Production request rebuilding, message composition and migration; detached world inputs.
internal static class WritingStyleReplay
{
    private sealed class Fixture
    {
        internal readonly WorldDiplomacyRound Round = new() {
            RoundId = "style-round", State = "active", RootDocumentId = "style-source",
            RelayPlanned = true, RelayRouteKingdomIds = new() { "a", "b" }, RelayCursor = 1,
            RoundTopic = "Trade terms", ConversationRevision = 7, SubstantiveProgressCount = 2,
            PendingOffers = new() { new() { SourceDocumentId = "style-source", SourceActionId = "trade-action",
                ProposerKingdomId = "b", TargetKingdomId = "a", Intent = "propose_trade", Status = "open" } }
        };
        internal readonly WorldDiplomacyDocument Source = new() {
            DocumentId = "style-source", RoundId = "style-round", AuthorKingdomId = "b", TargetKingdomId = "a",
            Title = "Trade terms", Body = "Trade for fourteen days.", IsReadyForPublication = true,
            Intent = "propose_trade", Commitment = "proposal",
            Actions = new() { new() { ActionId = "trade-action", TargetKingdomId = "a", Intent = "propose_trade",
                Commitment = "proposal", PeaceTerms = new() { DurationDays = 14 } } }
        };
        internal readonly WorldDiplomacyStorage Storage = new() { PromptContractVersion = 30 };
        internal int Rebuilds;
        internal Fixture() { Storage.ActiveRound = Round; Storage.Documents.Add(Source); }
        internal WorldDiplomacyJob Job(string kind, bool relay = false) => new() {
            JobId = kind + (relay ? "-relay" : "-direct"), Kind = kind, IsRelayTurn = relay, IsResponse = true,
            AuthorKingdomId = "a", TargetKingdomId = "b", RoundId = Round.RoundId,
            DocumentId = Source.DocumentId, SourceDocumentId = Source.DocumentId, RoundConversationRevision = 7
        };
        internal bool Rebuild(WorldDiplomacyJob job)
        {
            Rebuilds++;
            return WorldDiplomacyJobPreparationApplication.RebuildPendingJob(job, Storage, 4000, 2000,
                () => (300, 1000), id => id is "a" or "b", (_, _) => new() { "b" },
                id => id == Round.RoundId ? Round : null,
                _ => "CURRENT_COMMON_CONTRACT\nPLAYER_WRITING_PREFERENCE",
                id => id == Source.DocumentId ? Source : null,
                (_, _, source) => "RELAY_SOURCE\n" + JsonConvert.SerializeObject(source),
                (_, _, source, _) => "GENERATION_SOURCE\n" + JsonConvert.SerializeObject(source),
                _ => "current-legal-actions", j => j.DeclarationHistoryBlock = "CURRENT_KNOWN_HISTORY",
                source => WorldDiplomacyPromptContractRules.BuildAnalysisModeContract(DiplomacyPromptTexts.Get)
                    + "\nMODE=ANALYZE\n" + JsonConvert.SerializeObject(source),
                _ => "unused-plan", (_, _) => "unused-plan", DiplomacyPromptTexts.Get);
        }
    }

    internal static void Run()
    {
        var f = new Fixture();
        foreach (var job in new[] { f.Job("generate"), f.Job("generate", true), f.Job("analyze") })
        {
            Test.True(f.Rebuild(job), "actual request owner rebuilds " + job.JobId);
            VerifyMessages(job);
            Test.True(job.UserPrompt.Contains("trade-action") && job.UserPrompt.Contains("14")
                && job.SourceDocumentId == f.Source.DocumentId,
                "request keeps source action identity and terms: " + job.JobId);
        }
        RunMigration();
        RunDispatchRepairRefresh();
    }

    private static void VerifyMessages(WorldDiplomacyJob job)
    {
        JArray messages = WorldDiplomacyLlmMessageApplication.BuildLlmMessageArray(job, _ => "CURRENT_HISTORY");
        string system = (string)messages[0]["content"]!;
        string style = DiplomacyMiscPromptTexts.GetRequired("Declaration_DefaultStyle");
        string stance = DiplomacyMiscPromptTexts.GetRequired("Declaration_SemanticStance");
        string language = PromptComposerTexts.Get("conversation_reply_language");
        Test.True((string)messages[0]["role"]! == "system" && system == job.SystemPrompt
            && system.Contains("CURRENT_COMMON_CONTRACT") && system.Contains("PLAYER_WRITING_PREFERENCE"),
            "actual message array retains current contract and player preference: " + job.JobId);
        Test.True(system.Contains(stance) && string.IsNullOrEmpty(language),
            "semantic stance reaches real system without restoring a retired output language lock: " + job.JobId);
        Test.True(system.Contains(style) == (job.Kind == "generate"),
            "shared writing baseline applies to generation and relay, not semantic analysis: " + job.JobId);
        Test.True(PromptComposerTexts.ApplyOutputLanguage(system) == system,
            "package selection leaves custom writing preferences and generated system text unchanged: " + job.JobId);
        Test.True(!system.Contains("OLD_FROZEN_STYLE") && !system.Contains("OLD_COMMON_CORE"),
            "retired frozen style never reaches rebuilt messages: " + job.JobId);
        if (job.Kind == "generate")
        {
            Test.True(system.Contains(SmokeLocalizationEnvironment.EnglishPackage ? "300 to 1000 words." : "300至1000字"),
                "real generation/relay contract uses EN words and CN characters independent of UI");
            Test.True(job.MaxTokens == 5024,
                "rebuilt long declaration reserves requested prose and action JSON capacity");
            Test.True(WorldDiplomacyPromptContractRules.HasCurrentCanonicalPromptContract(job)
                && system.Contains("actions") && system.Contains("conciliatory|neutral|firm|hostile")
                && job.UserPrompt.Contains("【MODE=DECLARE】"),
                "writing style retains declaration routing, action schema and semantic tone: " + job.JobId);
        }
        Test.True(WorldDiplomacyPromptContractRules.GetDeclarationOutputTokenBudget(100, 1800) == 1800
            && WorldDiplomacyPromptContractRules.GetDeclarationOutputTokenBudget(1000, 1800) == 5024,
            "generation budget keeps the existing floor and scales with the configured upper bound");
    }

    private static void FreezeV30Repair(WorldDiplomacyJob job)
    {
        job.CacheAffinityKey = "diplomacy-history:v30";
        job.SystemPrompt = "OLD_COMMON_CORE\nOLD_FROZEN_STYLE";
        job.UserPrompt = "repair\n【MODE=DECLARE】";
        job.SemanticRepairAttempts = 1;
        job.IsRunning = true;
        job.HistoryPrefixHash = WorldDiplomacyPromptContractRules.StablePromptHashPair(job.SystemPrompt, "old-history");
        job.LlmMessages = new() {
            new() { Role = "system", Content = job.SystemPrompt }, new() { Role = "system", Content = "old-history" },
            new() { Role = "user", Content = "original\n【MODE=DECLARE】" },
            new() { Role = "assistant", Content = "old-rejected-draft" }, new() { Role = "user", Content = job.UserPrompt }
        };
    }

    private static string PoliticalState(Fixture f) => JsonConvert.SerializeObject(new {
        f.Source, f.Round.State, f.Round.RootDocumentId, f.Round.RoundTopic, f.Round.ConversationRevision,
        f.Round.PendingOffers, f.Round.Participants, f.Round.RelayRouteKingdomIds, f.Round.RelayCursor,
        f.Round.SubstantiveProgressCount, f.Round.ExecutedActionCount, f.Round.SchedulingOwner, f.Round.OwnershipEpoch
    });

    private static void RunMigration()
    {
        var f = new Fixture();
        var generation = f.Job("generate"); var relay = f.Job("generate", true); var analysis = f.Job("analyze");
        FreezeV30Repair(generation); FreezeV30Repair(relay);
        Test.True(WorldDiplomacyPromptContractRules.IsValidSemanticRepairMessageChain(generation),
            "migration fixture starts with a valid frozen old repair chain");
        f.Round.CommonContractSnapshot = "OLD_COMMON_CORE"; f.Round.CommonContractSnapshotInitialized = true;
        f.Round.CachePrefix = "old-prefix"; f.Round.LlmTranscript.Add(new() { Role = "system", Content = "OLD_FROZEN_STYLE" });
        f.Storage.Jobs.AddRange(new[] { generation, relay, analysis });
        string before = PoliticalState(f); int clears = 0;
        WorldDiplomacyStorageMigration.MigrateDiplomacyPromptContractIfNeeded(f.Storage, 31, true,
            f.Rebuild, (_, _) => throw new InvalidOperationException("valid source job retired"), () => clears++, _ => { });
        Test.True(f.Storage.PromptContractVersion == 31 && f.Storage.Jobs.Count == 3 && clears == 1,
            "v30 save migrates once to v31 while retaining valid declaration and analysis work");
        foreach (var job in f.Storage.Jobs)
        {
            Test.True(!job.IsRunning && job.SemanticRepairAttempts == 0 && job.LlmMessages.Count == 0
                && job.HistoryPrefixHash == "", "migration discards frozen repair state: " + job.JobId);
            VerifyMessages(job);
        }
        Test.True(PoliticalState(f) == before && f.Round.CommonContractSnapshot == ""
            && !f.Round.CommonContractSnapshotInitialized && f.Round.LlmTranscript.Count == 0 && f.Round.CachePrefix == "",
            "prompt migration changes caches only, preserving source prose, actions, terms, open offers and round progress");
        int rebuilds = f.Rebuilds;
        WorldDiplomacyStorageMigration.MigrateDiplomacyPromptContractIfNeeded(f.Storage, 31, true,
            f.Rebuild, null, () => clears++, null);
        Test.True(f.Rebuilds == rebuilds && clears == 1 && PoliticalState(f) == before,
            "current-version migration is idempotent");
    }

    private static void RunDispatchRepairRefresh()
    {
        var f = new Fixture(); var job = f.Job("generate", true); FreezeV30Repair(job); f.Storage.Jobs.Add(job);
        string before = PoliticalState(f);
        Test.True(WorldDiplomacyLlmDispatchApplication.EnsureCurrentCanonicalPromptContractBeforeSend(job, f.Storage,
            f.Rebuild, (_, reason) => throw new InvalidOperationException(reason), _ => throw new InvalidOperationException("removed"), _ => { }),
            "before-send guard refreshes a restored v30 frozen repair through actual request rebuilding");
        Test.True(job.SemanticRepairAttempts == 0 && job.LlmMessages.Count == 0 && job.HistoryPrefixHash == ""
            && PoliticalState(f) == before, "before-send repair refresh preserves diplomatic state");
        VerifyMessages(job);
    }
}
