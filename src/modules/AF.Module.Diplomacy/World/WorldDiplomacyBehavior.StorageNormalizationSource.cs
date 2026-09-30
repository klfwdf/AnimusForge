using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    private readonly struct CanonicalHistoryMigrationSource : IWorldDiplomacyCanonicalHistoryMigrationSource
    {
        private readonly WorldDiplomacyBehavior _owner;
        internal CanonicalHistoryMigrationSource(WorldDiplomacyBehavior owner) { _owner = owner; }
        public bool HasCampaignWorld => Campaign.Current != null && Kingdom.All.Any();
        public int CurrentDay => WorldDiplomacyBehavior.CurrentDay();
        public long HistoryCompressionTriggerTokens => GetHistoryCompressionTriggerTokens();
        public int TargetHistoryMemorySchemaVersion => HistoryMemorySchemaVersion;
        public int RelaySchemaVersion => WorldDiplomacyBehavior.RelaySchemaVersion;
        public string PublishedPolicyLedgerId() => DiplomacyModuleServices.Policy.GetPublishedPolicyHistoryLedgerId();
        public void AcknowledgePolicyArtifactsThrough(long throughSequence) => DiplomacyModuleServices.Policy.TryAcknowledgePublishedPolicyHistoryThrough(throughSequence);
        public void ClearSourceKeys() => _owner._runtime.CanonicalHistorySourceKeys.Clear();
        public IEnumerable<WorldDiplomacyWeeklyArtifact> WeeklyArtifacts() => new HistoryCapturePort(_owner).WeeklyArtifacts();
        public IEnumerable<PublishedPolicyArtifactLedgerEntry> PolicyArtifacts() => ReadAllPublishedPolicyArtifactsForMigration();
        public void SetObservedWeeklyRevision(long revision) => _owner._runtime.LastObservedWorldWeeklyHistoryRevision = revision;
        public long WeeklyRevision() => MyBehavior.GetPublishedWorldWeeklyReportHistoryRevisionForExternal();
        public WorldDiplomacyDocument ResolveDocument(string documentId) => _owner.ResolveDocument(documentId);
        public int EstimateTokens(string text) => Logger.EstimateTokens(text);
        public void Log(string message) => WorldDiplomacyBehavior.Log(message);
    }

    private readonly struct StorageNormalizationSource : IWorldDiplomacyStorageNormalizationSource
    {
        private readonly WorldDiplomacyBehavior _owner;
        internal StorageNormalizationSource(WorldDiplomacyBehavior owner)
        {
            _owner = owner;
        }
        public int CurrentDay => WorldDiplomacyBehavior.CurrentDay();
        public int CivilianSpreadDays => GetCivilianSpreadDays();
        public int CourtMaxDeliveryDays => GetCourtMaxDeliveryDays();
        public int MaxDiplomaticActionsPerDocument => WorldDiplomacyBehavior.MaxDiplomaticActionsPerDocument;
        public int MaxPendingPolicySignals => WorldDiplomacyBehavior.MaxPendingPolicySignals;
        public int MaxProcessedPolicySignalKeys => WorldDiplomacyBehavior.MaxProcessedPolicySignalKeys;
        public int MaxConsecutiveTechnicalGenerationFailuresPerRound => WorldDiplomacyBehavior.MaxConsecutiveTechnicalGenerationFailuresPerRound;
        public int MaxStoredRoundSummaries => WorldDiplomacyBehavior.MaxStoredRoundSummaries;
        public int MaxStoredAnnualSummaries => WorldDiplomacyBehavior.MaxStoredAnnualSummaries;
        public int MaxStoredCompressionSummaries => WorldDiplomacyBehavior.MaxStoredCompressionSummaries;
        public int DecisionArchitectureVersion => WorldDiplomacyBehavior.DecisionArchitectureVersion;
        public int RelaySchemaVersion => WorldDiplomacyBehavior.RelaySchemaVersion;
        public int RelayTargetDurationDays => WorldDiplomacyBehavior.RelayTargetDurationDays;
        public long HistoryCompressionTriggerTokens => GetHistoryCompressionTriggerTokens();
        public int RoundHardDurationDays(int targetDurationDays) => GetRoundHardDurationDays(targetDurationDays);
        public string FormatCampaignDate(int day) => WorldDiplomacyBehavior.FormatCampaignDate(day);
        public string ResolveKingdomNameOrEmpty(string kingdomId) => WorldDiplomacyBehavior.ResolveKingdomNameOrEmpty(kingdomId);
        public List<string> NormalizeKingdomIdList(IEnumerable<string> values, string excludedId) => WorldDiplomacyBehavior.NormalizeKingdomIdList(values, excludedId);
        public IReadOnlyCollection<string> CaptureNonHideoutSettlementIds() =>
            Settlement.All.Where(x => x != null && !x.IsHideout && !string.IsNullOrWhiteSpace(x.StringId))
                .Select(x => x.StringId).ToList();
        public IReadOnlyCollection<string> CaptureNonEliminatedKingdomIds() =>
            Kingdom.All.Where(x => x != null && !x.IsEliminated)
                .Select(x => x.StringId).ToList();
        public bool HasCampaignWorld => Campaign.Current != null && Kingdom.All.Any();
        public int DiplomacyPromptContractVersion => WorldDiplomacyBehavior.DiplomacyPromptContractVersion;
        public int ResultSettlementStateSchemaVersion => WorldDiplomacyBehavior.ResultSettlementStateSchemaVersion;
        public string ValidateThreatWorldEligibility(WorldDiplomacyThreat threat) =>
            _owner.ValidateOpenThreatWorldEligibility(threat,
                Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>());
        public string NewThreatId() => WorldDiplomacyBehavior.NewId("diplomacy_threat");
        public int ThreatComplianceIssuerRewardMax => DuelSettings.WorldDiplomacyThreatComplianceIssuerRelationRewardMax;
        public WorldDiplomacyDocument ResolveDocument(string documentId) => _owner.ResolveDocument(documentId);
        public string ResolveEligibleKingdomId(string kingdomId) => _owner.ResolveEligibleDiplomacyKingdomId(kingdomId);
        public bool IsAtWarByKingdomIds(string firstKingdomId, string secondKingdomId) => _owner.IsAtWarByKingdomIds(firstKingdomId, secondKingdomId);
        public void ClearLlmCacheAffinityKey() => _owner._runtime.LastLlmCacheAffinityKey = "";
        public WorldDiplomacyRound ResolveRound(string roundId) => _owner.ResolveRound(roundId);
        public void CommitLocalRoundSummary(WorldDiplomacyRound round, List<WorldDiplomacyDocument> documents) => _owner._orchestration.CommitLocalRoundSummary(round, documents);
        public void UpgradeRoundSummaryToStructuredArchive(WorldDiplomacyRoundSummary summary) => _owner._orchestration.UpgradeRoundSummaryToStructuredArchive(summary);
        public void TrimNativeSignals() => _owner._orchestration.TrimNativeSignals();
        public void TrimRecentBattleFacts() => _owner._orchestration.TrimRecentBattleFacts();
        public void Log(string message) => WorldDiplomacyBehavior.Log(message);
    }
}
