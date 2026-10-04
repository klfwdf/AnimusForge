using System;
using System.Collections.Generic;

namespace AnimusForge.Refactor.Persistence;

internal static class WorldDiplomacyStorageShapeNormalizer
{
    public static WorldDiplomacyStorage EnsureInitialized(WorldDiplomacyStorage storage)
    {
        storage ??= new WorldDiplomacyStorage();
        storage.OfferCooldowns ??= new List<WorldDiplomacyOfferCooldown>();
        storage.DiplomaticThreats ??= new List<WorldDiplomacyThreat>();
        storage.NationalPrestigeByKingdom ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        storage.InternationalReputationByKingdom ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        storage.InternationalReputationNaturalChangeLastDayByKingdom ??=
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        storage.NationalPrestigeRelationModifiers ??= new List<WorldDiplomacyPrestigeRelationModifier>();
        storage.CanonicalHistory ??= new WorldDiplomacyCanonicalHistoryState();
        storage.ConcurrentRounds ??= new List<WorldDiplomacyRound>();
        storage.DialogueArrangements ??= new List<WorldDiplomacyDialogueArrangement>();
        storage.RequestBudget ??= new DiplomacyRequestBudget();
        storage.CompletedRounds ??= new List<WorldDiplomacyRound>();
        storage.PropagationArrivals ??= new List<WorldDiplomacyPropagationArrival>();
        storage.SettlementKnowledge ??= new List<WorldDiplomacySettlementKnowledge>();
        storage.KingdomKnowledge ??= new List<WorldDiplomacyKingdomKnowledge>();
        storage.NobleKnowledge ??= new List<WorldDiplomacyKingdomKnowledge>();
        storage.PendingParticipationEvaluations ??= new List<WorldDiplomacyParticipationRequest>();
        storage.PendingSpeeches ??= new List<WorldDiplomacyPendingSpeech>();
        storage.RelayArrivals ??= new List<WorldDiplomacyRelayArrival>();
        storage.PlayerOpportunities ??= new List<WorldDiplomacyPlayerOpportunity>();
        storage.RoundSummaries ??= new List<WorldDiplomacyRoundSummary>();
        storage.PendingPolicySignals ??= new List<WorldDiplomacyPolicySignal>();
        storage.ProcessedPolicySignalKeys ??= new List<string>();
        storage.RecentTopicUses ??= new List<WorldDiplomacyTopicUse>();
        storage.Documents ??= new List<WorldDiplomacyDocument>();
        storage.AnnualSummaries ??= new List<WorldDiplomacyAnnualSummary>();
        storage.CompressionSummaries ??= new List<WorldDiplomacyCompressionSummary>();
        storage.WarPressure ??= new List<WarPressureEntry>();
        storage.ActiveWarLedgers ??= new List<WorldDiplomacyWarLedger>();
        storage.RecentBattles ??= new List<WorldDiplomacyBattleFact>();
        storage.NativeSignals ??= new List<NativeDiplomacySignal>();
        storage.Jobs ??= new List<WorldDiplomacyJob>();
        storage.SuspendedExchanges ??= new List<WorldDiplomacyExchange>();
        storage.LastOffensiveWarDayByKingdom ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        storage.LastPeaceDayByPair ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        return storage;
    }
}
