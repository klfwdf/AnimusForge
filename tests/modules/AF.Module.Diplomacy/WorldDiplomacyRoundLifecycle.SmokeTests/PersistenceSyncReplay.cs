using AnimusForge;
using AnimusForge.Refactor.Domain;
using System.Collections.Generic;
using System.Linq;

// Drives the real orchestration SyncData path through fake leaf ports:
// the application owner must sequence load/save/replace/reset/normalize and
// keep legacy propagation snapshots behind the validation gate.
internal static class PersistenceSyncReplay
{
    private sealed class NormSource : IWorldDiplomacyStorageNormalizationSource
    {
        internal readonly List<string> Events = new();
        internal int Day = 42;
        internal bool Captured;
        public int CurrentDay => Day;
        public int CivilianSpreadDays => 3;
        public int CourtMaxDeliveryDays => 4;
        public int MaxDiplomaticActionsPerDocument => 8;
        public int MaxPendingPolicySignals => 8;
        public int MaxProcessedPolicySignalKeys => 64;
        public int MaxConsecutiveTechnicalGenerationFailuresPerRound => 2;
        public int MaxStoredRoundSummaries => 16;
        public int MaxStoredAnnualSummaries => 8;
        public int MaxStoredCompressionSummaries => 8;
        public int DecisionArchitectureVersion => 1;
        public int RelaySchemaVersion => 1;
        public int RelayTargetDurationDays => 7;
        public long HistoryCompressionTriggerTokens => long.MaxValue;
        public int RoundHardDurationDays(int targetDurationDays) => targetDurationDays + 7;
        public string FormatCampaignDate(int day) => "day_" + day;
        public string ResolveKingdomNameOrEmpty(string kingdomId) => "";
        public List<string> NormalizeKingdomIdList(IEnumerable<string> values, string excludedId) =>
            (values ?? Enumerable.Empty<string>())
                .Select(x => (x ?? "").Trim())
                .Where(x => x.Length > 0 && !string.Equals(x, excludedId, System.StringComparison.OrdinalIgnoreCase))
                .Distinct(System.StringComparer.OrdinalIgnoreCase).ToList();
        public IReadOnlyCollection<string> CaptureNonHideoutSettlementIds()
        {
            Events.Add("capture:settlements");
            return new List<string> { "town_x" };
        }
        public IReadOnlyCollection<string> CaptureNonEliminatedKingdomIds()
        {
            Events.Add("capture:kingdoms");
            return new List<string> { "k_live" };
        }
        public bool HasCampaignWorld => false;
        public int DiplomacyPromptContractVersion => 1;
        public int ResultSettlementStateSchemaVersion => 1;
        public string ValidateThreatWorldEligibility(WorldDiplomacyThreat threat) => null;
        public string NewThreatId() => "threat_new";
        public int ThreatComplianceIssuerRewardMax => 10;
        public WorldDiplomacyDocument ResolveDocument(string documentId) => null;
        public string ResolveEligibleKingdomId(string kingdomId) => kingdomId;
        public bool IsAtWarByKingdomIds(string firstKingdomId, string secondKingdomId) => false;
        public void ClearLlmCacheAffinityKey() { }
        public WorldDiplomacyRound ResolveRound(string roundId) => null;
        public void CommitLocalRoundSummary(WorldDiplomacyRound round, List<WorldDiplomacyDocument> documents) { }
        public void UpgradeRoundSummaryToStructuredArchive(WorldDiplomacyRoundSummary summary) { }
        public void TrimNativeSignals() => Events.Add("trim:signals");
        public void TrimRecentBattleFacts() => Events.Add("trim:battles");
        public void Log(string message) => Events.Add("log:" + message);
    }

    private sealed class MigrationSource : IWorldDiplomacyCanonicalHistoryMigrationSource
    {
        public bool HasCampaignWorld => false;
        public int CurrentDay => 42;
        public long HistoryCompressionTriggerTokens => long.MaxValue;
        public int TargetHistoryMemorySchemaVersion => 1;
        public int RelaySchemaVersion => 1;
        public string PublishedPolicyLedgerId() => "";
        public void AcknowledgePolicyArtifactsThrough(long throughSequence) { }
        public void ClearSourceKeys() { }
        public IEnumerable<WorldDiplomacyWeeklyArtifact> WeeklyArtifacts() => Enumerable.Empty<WorldDiplomacyWeeklyArtifact>();
        public IEnumerable<PublishedPolicyArtifactLedgerEntry> PolicyArtifacts() => Enumerable.Empty<PublishedPolicyArtifactLedgerEntry>();
        public long WeeklyRevision() => 0;
        public void SetObservedWeeklyRevision(long revision) { }
        public WorldDiplomacyDocument ResolveDocument(string documentId) => null;
        public int EstimateTokens(string text) => 0;
        public void Log(string message) { }
    }

    private sealed class Host : FakeOrchestrationHost
    {
        internal readonly NormSource Norm = new();
        internal readonly MigrationSource Migration = new();
        public override IWorldDiplomacyStorageNormalizationSource StorageNormalizationSource() => Norm;
        public override IWorldDiplomacyCanonicalHistoryMigrationSource CanonicalHistoryMigrationSource() => Migration;
    }

    internal static void Run()
    {
        // Save: normalize the live state once, then hand the canonical record to the writer.
        var host = new Host();
        var orch = new WorldDiplomacyOrchestration(host, new WorldDiplomacyRuntimeState());
        var events = new List<string>();
        WorldDiplomacyStorage saved = null;
        orch.SyncData(isSaving: true, isLoading: false,
            () => { events.Add("load"); return null; },
            () => null,
            storage => { saved = storage; events.Add("save"); },
            message => events.Add("log:" + message),
            () => events.Add("reset"));
        Test.True(ReferenceEquals(saved, orch.CurrentStorage)
            && events.SequenceEqual(new[] { "save" })
            && host.Norm.Events.Contains("trim:signals"),
            "save must normalize the canonical record once and write it without loading or resetting");

        // A rejected load is not a successful empty domain. Never normalize it
        // or merge partial data/previous campaign state into the current owner.
        var loaded = new WorldDiplomacyStorage
        {
            ServiceCooldownUntilHour = 77,
            PropagationReliabilityVersion = 0
        };
        host.Norm.Events.Clear();
        events.Clear();
        orch.SyncData(isSaving: false, isLoading: true,
            () => { events.Add("load"); return loaded; },
            () => "io-error",
            storage => events.Add("save"),
            message => events.Add("log:" + message),
            () => events.Add("reset"));
        Test.True(!ReferenceEquals(orch.CurrentStorage, loaded)
            && !orch.IsPersistenceHealthy && orch.CurrentStorage.ServiceCooldownUntilHour != 77,
            "rejected load must isolate the domain and discard partial/prior campaign live state");
        int loadIx = events.IndexOf("load");
        int logIx = events.IndexOf("log:load rejected; diplomacy quarantined: io-error");
        int resetIx = events.IndexOf("reset");
        Test.True(loadIx == 0 && logIx == 1 && resetIx == 2
            && host.Norm.Events.Count == 0,
            "rejected load reports the error and resets runtime without normalizing invalid storage");
        Test.True(!host.Norm.Events.Any(e => e.StartsWith("capture:")),
            "load-time normalization without world validation must not enumerate live world ids");
        Test.True(!events.Contains("save"),
            "the load lane must never invoke the save adapter");
        host.Norm.Events.Clear();events.Clear();
        orch.ProcessCompletedJobs();orch.TryStartNextLlmJob();orch.PollNotifications();orch.NormalizeStorage(true);
        Test.True(host.Norm.Events.Count == 0 && !orch.IsPersistenceHealthy,
            "quarantined owner rejects cached job/notice/normalization entry points");
        bool rejectedWriter=false;
        orch.SyncData(true,false,null,null,_=>rejectedWriter=true,_=>{},()=>{});
        Test.True(rejectedWriter&&host.Norm.Events.Count==0,
            "quarantined Save calls the evidence writer without canonical normalization or budget snapshot");

        // Neither flag set and both flags set must be no-ops for the write lane.
        events.Clear();
        orch.SyncData(false, false, () => null, () => null, s => events.Add("save"), e => { }, () => events.Add("reset"));
        Test.True(events.Count == 0, "a non-save non-load store event must do nothing");

        // Reload over an existing record swaps the canonical reference exactly once.
        var reloaded = new WorldDiplomacyStorage { ServiceCooldownUntilHour = 5 };
        events.Clear();
        orch.SyncData(false, true, () => reloaded, () => null, s => { }, e => { }, () => { });
        Test.True(ReferenceEquals(orch.CurrentStorage, reloaded)
            && orch.IsPersistenceHealthy && orch.CurrentStorage.ServiceCooldownUntilHour == 5,
            "a second load must replace the canonical record, not merge a second store");

        // Null loaded storage falls back to a fresh canonical record.
        orch.SyncData(false, true, () => null, () => null, s => { }, e => { }, () => { });
        Test.True(orch.CurrentStorage != null, "a null load must still yield one canonical record");
    }
}
