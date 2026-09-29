using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
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
        public void EnsureInitialized() => _owner.EnsureCanonicalHistoryInitialized();
        public string PublishedPolicyLedgerId() => DiplomacyModuleServices.Policy.GetPublishedPolicyHistoryLedgerId();
        public void RebuildPolicySignaturesThrough(long throughSequence) => _owner.RebuildPublishedPolicySignaturesThrough(throughSequence);
        public void AcknowledgePolicyArtifactsThrough(long throughSequence) => DiplomacyModuleServices.Policy.TryAcknowledgePublishedPolicyHistoryThrough(throughSequence);
        public void ClearSourceKeys() => _owner._canonicalHistorySourceKeys.Clear();
        public IEnumerable<WorldDiplomacyWeeklyArtifact> WeeklyArtifacts() => new HistoryCapturePort(_owner).WeeklyArtifacts();
        public IEnumerable<PublishedPolicyArtifactLedgerEntry> PolicyArtifacts() => ReadAllPublishedPolicyArtifactsForMigration();
        public void AppendDocumentEvents(WorldDiplomacyDocument document) => _owner.AppendCanonicalDocumentEvents(document);
        public void AppendWeekly(WorldDiplomacyWeeklyArtifact artifact) => new HistoryCapturePort(_owner).AppendWeekly(artifact);
        public void AppendPolicyArtifact(PublishedPolicyArtifactLedgerEntry artifact) => _owner.AppendPublishedPolicyArtifact(artifact);
        public void BackfillResponseLinks() => _owner.BackfillCanonicalResponseLinksV2();
        public void SetObservedWeeklyRevision(long revision) => _owner._lastObservedWorldWeeklyHistoryRevision = revision;
        public long WeeklyRevision() => MyBehavior.GetPublishedWorldWeeklyReportHistoryRevisionForExternal();
        public bool RebuildPendingJob(WorldDiplomacyJob job) => _owner.TryRebuildPendingWorldDiplomacyJob(job);
        public void CompleteExchange(string exchangeId, string reason) => _owner.CompleteExchange(exchangeId, reason);
        public WorldDiplomacyDocument ResolveDocument(string documentId) => _owner.ResolveDocument(documentId);
        public void CloseActiveRound(string reason) => _owner.CloseActiveRound(reason);
        public void InvalidateRenderCache() => _owner.InvalidateCanonicalHistoryRenderCache();
        public int EstimateTokens(string text) => Logger.EstimateTokens(text);
        public void Log(string message) => _owner.Log(message);
    }

    private readonly struct StorageNormalizationSource : IWorldDiplomacyStorageNormalizationSource
    {
        private readonly WorldDiplomacyBehavior _owner;
        internal StorageNormalizationSource(WorldDiplomacyBehavior owner) { _owner = owner; }
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
        public bool HasCompleteLegacyPropagationCoverage(WorldDiplomacyDocument document) => _owner.HasCompleteLegacyPropagationCoverage(document);
        public void PruneInvalidOffers(WorldDiplomacyRound round) => _owner.PruneInvalidOffers(round);
        public void NormalizeOfferCooldownStorage() => _owner.NormalizeOfferCooldownStorage();
        public void NormalizeDiplomaticThreats(bool allowWorldValidation) => _owner.NormalizeDiplomaticThreats(allowWorldValidation);
        public void MigrateAutonomousDecisionArchitecture() => _owner.MigrateAutonomousDecisionArchitectureIfNeeded();
        public void MigrateCanonicalHistory() => _owner.MigrateCanonicalHistoryIfNeeded();
        public void MigrateResultSettlementState() => _owner.MigrateResultSettlementStateIfNeeded();
        public void MigrateDiplomacyPromptContract() => _owner.MigrateDiplomacyPromptContractIfNeeded();
        public WorldDiplomacyRound ResolveRound(string roundId) => _owner.ResolveRound(roundId);
        public void CommitLocalRoundSummary(WorldDiplomacyRound round, List<WorldDiplomacyDocument> documents) => _owner.CommitLocalRoundSummary(round, documents);
        public void UpgradeRoundSummaryToStructuredArchive(WorldDiplomacyRoundSummary summary) => _owner.UpgradeRoundSummaryToStructuredArchive(summary);
        public void EnsureCanonicalHistoryInitialized() => _owner.EnsureCanonicalHistoryInitialized();
        public void TrimNativeSignals() => _owner.TrimNativeSignals();
        public void TrimRecentBattleFacts() => _owner.TrimRecentBattleFacts();
        public void Log(string message) => _owner.Log(message);
    }
}
