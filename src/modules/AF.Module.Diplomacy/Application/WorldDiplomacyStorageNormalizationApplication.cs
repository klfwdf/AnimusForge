using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Persistence;

namespace AnimusForge;

internal interface IWorldDiplomacyStorageNormalizationSource
{
    int CurrentDay { get; }
    int CivilianSpreadDays { get; }
    int CourtMaxDeliveryDays { get; }
    int MaxDiplomaticActionsPerDocument { get; }
    int MaxPendingPolicySignals { get; }
    int MaxProcessedPolicySignalKeys { get; }
    int MaxConsecutiveTechnicalGenerationFailuresPerRound { get; }
    int MaxStoredRoundSummaries { get; }
    int MaxStoredAnnualSummaries { get; }
    int MaxStoredCompressionSummaries { get; }
    int DecisionArchitectureVersion { get; }
    int RelaySchemaVersion { get; }
    int RelayTargetDurationDays { get; }
    long HistoryCompressionTriggerTokens { get; }
    int RoundHardDurationDays(int targetDurationDays);
    string FormatCampaignDate(int day);
    string ResolveKingdomNameOrEmpty(string kingdomId);
    List<string> NormalizeKingdomIdList(IEnumerable<string> values, string excludedId);
    bool HasCompleteLegacyPropagationCoverage(WorldDiplomacyDocument document);
    void PruneInvalidOffers(WorldDiplomacyRound round);
    void NormalizeOfferCooldownStorage();
    void NormalizeDiplomaticThreats(bool allowWorldValidation);
    void MigrateAutonomousDecisionArchitecture();
    void MigrateCanonicalHistory();
    void MigrateResultSettlementState();
    void MigrateDiplomacyPromptContract();
    WorldDiplomacyRound ResolveRound(string roundId);
    void CommitLocalRoundSummary(WorldDiplomacyRound round, List<WorldDiplomacyDocument> documents);
    void UpgradeRoundSummaryToStructuredArchive(WorldDiplomacyRoundSummary summary);
    void EnsureCanonicalHistoryInitialized();
    void TrimNativeSignals();
    void TrimRecentBattleFacts();
    void Log(string message);
}

internal static class WorldDiplomacyStorageNormalizationApplication
{
    internal static void Normalize<TSource>(ref WorldDiplomacyStorage storage, bool allowWorldValidation, ref TSource source)
        where TSource : struct, IWorldDiplomacyStorageNormalizationSource
    {
        storage = WorldDiplomacyStorageShapeNormalizer.EnsureInitialized(storage);
        WorldDiplomacyNotificationStateMigration.Migrate(storage);
        source.NormalizeOfferCooldownStorage();
        storage.CompressionRetryAfterHour = Math.Max(0, storage.CompressionRetryAfterHour);
        storage.CompressionRetryAttempts = Math.Max(0, Math.Min(31, storage.CompressionRetryAttempts));
        source.NormalizeDiplomaticThreats(allowWorldValidation);
        if (allowWorldValidation)
        {
            try
            {
                source.MigrateAutonomousDecisionArchitecture();
            }
            catch (Exception ex)
            {
                // Leave the version unstamped so OnSessionLaunched or the next daily tick can retry.
                source.Log("autonomous diplomacy architecture migration deferred after error=" + ex.Message);
            }
        }
        storage.PendingPolicySignals = WorldDiplomacyRoundLifecycleRules.NormalizePendingPolicySignals(
            storage.PendingPolicySignals, source.MaxPendingPolicySignals);
        storage.ProcessedPolicySignalKeys = WorldDiplomacyRoundLifecycleRules.NormalizeProcessedSignalKeys(
            storage.ProcessedPolicySignalKeys, source.MaxProcessedPolicySignalKeys);
        // 旧选题历史仅为反序列化兼容保留，不再参与自主决策。
        storage.RecentTopicUses.Clear();
        storage.PropagationArrivals = WorldDiplomacyRoundLifecycleRules.NormalizePropagationArrivalList(
            storage.PropagationArrivals);
        WorldDiplomacyRoundLifecycleRules.NormalizeWarLedgerList(storage.ActiveWarLedgers);
        storage.Documents.RemoveAll(x => x == null || string.IsNullOrWhiteSpace(x.DocumentId));
        WorldDiplomacyRoundLifecycleRules.NormalizeBattleRecords(storage.RecentBattles);
        foreach (WorldDiplomacyBattleFact battle in storage.RecentBattles)
        {
            if (string.IsNullOrWhiteSpace(battle.GameDate))
            {
                battle.GameDate = source.FormatCampaignDate(battle.Day);
            }
        }
        bool migrateLegacyPropagationState = allowWorldValidation && storage.PropagationReliabilityVersion < 1;
        int legacyPropagationRecoveryWindow = Math.Max(source.CivilianSpreadDays, source.CourtMaxDeliveryDays) + 7;
        foreach (WorldDiplomacyDocument document in storage.Documents)
        {
            WorldDiplomacyStorageMigration.NormalizeStoredDocumentRecord(
                document, storage, migrateLegacyPropagationState, legacyPropagationRecoveryWindow,
                source.CurrentDay, source.MaxDiplomaticActionsPerDocument, source.ResolveKingdomNameOrEmpty,
                source.NormalizeKingdomIdList, source.FormatCampaignDate, source.HasCompleteLegacyPropagationCoverage);
        }
        if (allowWorldValidation)
        {
            try
            {
                source.MigrateCanonicalHistory();
            }
            catch (Exception ex)
            {
                source.Log("canonical diplomacy history migration deferred after error=" + ex.Message);
            }
        }
        foreach (WorldDiplomacyJob legacyRoundCompression in storage.Jobs
            .Where(x => WorldDiplomacyRoundLifecycleRules.IsJobOfKind(x, "round_compress")).ToList())
        {
            WorldDiplomacyRound round = source.ResolveRound(legacyRoundCompression.RoundId);
            List<WorldDiplomacyDocument> documents = WorldDiplomacyRoundLifecycleRules.SelectRoundCompressionDocuments(
                storage.Documents, legacyRoundCompression.RoundId, legacyRoundCompression.CompressionDocumentIds);
            if (round != null && documents.Count > 0) source.CommitLocalRoundSummary(round, documents);
        }
        storage.Jobs.RemoveAll(x => WorldDiplomacyRoundLifecycleRules.IsRetiredJob(x));
        foreach (WorldDiplomacyJob job in storage.Jobs)
        {
            WorldDiplomacyRoundLifecycleRules.NormalizeJobRecord(job);
        }
        Dictionary<string, WorldDiplomacyDocument> normalizedDocumentsById = WorldDiplomacyDocumentFactRules.BuildDocumentIndex(storage.Documents);
        foreach (WorldDiplomacyRound round in storage.CompletedRounds.Concat(storage.ActiveRound == null ? Enumerable.Empty<WorldDiplomacyRound>() : new[] { storage.ActiveRound }).Where(x => x != null))
        {
            WorldDiplomacyStorageMigration.NormalizeStoredRoundRecord(
                round, storage, normalizedDocumentsById, allowWorldValidation,
                source.DecisionArchitectureVersion, source.RelaySchemaVersion, source.RelayTargetDurationDays,
                source.CourtMaxDeliveryDays, source.MaxPendingPolicySignals,
                source.MaxConsecutiveTechnicalGenerationFailuresPerRound,
                source.RoundHardDurationDays, source.PruneInvalidOffers, source.Log);
        }
        if (allowWorldValidation)
        {
            try
            {
                source.MigrateResultSettlementState();
            }
            catch (Exception ex)
            {
                source.Log("round result-settlement migration deferred after error=" + ex.Message);
            }
            try
            {
                source.MigrateDiplomacyPromptContract();
            }
            catch (Exception ex)
            {
                source.Log("diplomacy prompt contract migration deferred after error=" + ex.Message);
            }
        }
        if (migrateLegacyPropagationState) storage.PropagationReliabilityVersion = 1;
        WorldDiplomacyRoundLifecycleRules.NormalizeSettlementKnowledgeRecords(storage.SettlementKnowledge);
        WorldDiplomacyRoundLifecycleRules.NormalizeKingdomKnowledgeRecords(storage.KingdomKnowledge);
        WorldDiplomacyRoundLifecycleRules.NormalizeKingdomKnowledgeRecords(storage.NobleKnowledge);
        if (!storage.CourtKnowledgeMigratedToNobles)
        {
            foreach (WorldDiplomacyKingdomKnowledge courtKnowledge in storage.KingdomKnowledge.Where(x => x != null))
            {
                foreach (string documentId in courtKnowledge.DocumentIds ?? new List<string>()) WorldDiplomacyDocumentFactRules.RecordNobleKnowledge(storage.NobleKnowledge, courtKnowledge.KingdomId, documentId, courtKnowledge.LastUpdatedDay);
            }
            storage.CourtKnowledgeMigratedToNobles = true;
        }
        WorldDiplomacyRoundLifecycleRules.NormalizeParticipationRequestRecords(storage.PendingParticipationEvaluations);
        storage.PendingParticipationEvaluations.Clear();
        storage.PendingSpeeches.Clear();
        storage.RelayArrivals = WorldDiplomacyRoundLifecycleRules.NormalizeRelayArrivalList(storage.RelayArrivals);
        storage.PlayerOpportunities = WorldDiplomacyRoundLifecycleRules.NormalizePlayerOpportunityList(
            storage.PlayerOpportunities, 16);
        foreach (WorldDiplomacyRoundSummary summary in storage.RoundSummaries.Where(x => x != null))
        {
            source.UpgradeRoundSummaryToStructuredArchive(summary);
            WorldDiplomacyRoundLifecycleRules.NormalizeRoundSummaryRecord(summary, storage.Documents);
        }
        foreach (WorldDiplomacyCompressionSummary summary in storage.CompressionSummaries.Where(x => x != null))
        {
            WorldDiplomacyRoundLifecycleRules.NormalizeCompressionSummaryRecord(summary);
        }
        storage.CompletedRounds = WorldDiplomacyRoundLifecycleRules.SelectRetainedCompletedRounds(
            storage.CompletedRounds, 64);
        storage.RoundSummaries = WorldDiplomacyRoundLifecycleRules.SelectRetainedRoundSummaries(
            storage.RoundSummaries, source.MaxStoredRoundSummaries);
        storage.AnnualSummaries = WorldDiplomacyRoundLifecycleRules.SelectRetainedAnnualSummaries(
            storage.AnnualSummaries, source.MaxStoredAnnualSummaries);
        storage.CompressionSummaries = WorldDiplomacyRoundLifecycleRules.SelectRetainedCompressionSummaries(
            storage.CompressionSummaries, source.MaxStoredCompressionSummaries);
        source.EnsureCanonicalHistoryInitialized();
        WorldDiplomacyRoundLifecycleRules.RecalculateCanonicalHistoryTokens(storage, source.HistoryCompressionTriggerTokens);
        source.TrimNativeSignals();
        source.TrimRecentBattleFacts();
    }
}
