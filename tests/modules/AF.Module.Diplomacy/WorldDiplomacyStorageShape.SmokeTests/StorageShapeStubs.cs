using System.Collections.Generic;

namespace AnimusForge;

public sealed class WorldDiplomacyStorage
{
    public List<WorldDiplomacyRound> ConcurrentRounds { get; set; }
    public List<WorldDiplomacyDialogueArrangement> DialogueArrangements { get; set; }
    public DiplomacyRequestBudget RequestBudget { get; set; }
    public List<WorldDiplomacyOfferCooldown> OfferCooldowns { get; set; }
    public List<WorldDiplomacyThreat> DiplomaticThreats { get; set; }
    public Dictionary<string, int> NationalPrestigeByKingdom { get; set; }
    public Dictionary<string, int> InternationalReputationByKingdom { get; set; }
    public Dictionary<string, int> InternationalReputationNaturalChangeLastDayByKingdom { get; set; }
    public List<WorldDiplomacyPrestigeRelationModifier> NationalPrestigeRelationModifiers { get; set; }
    public WorldDiplomacyCanonicalHistoryState CanonicalHistory { get; set; }
    public List<WorldDiplomacyRound> CompletedRounds { get; set; }
    public List<WorldDiplomacyPropagationArrival> PropagationArrivals { get; set; }
    public List<WorldDiplomacySettlementKnowledge> SettlementKnowledge { get; set; }
    public List<WorldDiplomacyKingdomKnowledge> KingdomKnowledge { get; set; }
    public List<WorldDiplomacyKingdomKnowledge> NobleKnowledge { get; set; }
    public List<WorldDiplomacyParticipationRequest> PendingParticipationEvaluations { get; set; }
    public List<WorldDiplomacyPendingSpeech> PendingSpeeches { get; set; }
    public List<WorldDiplomacyRelayArrival> RelayArrivals { get; set; }
    public List<WorldDiplomacyPlayerOpportunity> PlayerOpportunities { get; set; }
    public List<WorldDiplomacyRoundSummary> RoundSummaries { get; set; }
    public List<WorldDiplomacyPolicySignal> PendingPolicySignals { get; set; }
    public List<string> ProcessedPolicySignalKeys { get; set; }
    public List<WorldDiplomacyTopicUse> RecentTopicUses { get; set; }
    public List<WorldDiplomacyDocument> Documents { get; set; }
    public List<WorldDiplomacyAnnualSummary> AnnualSummaries { get; set; }
    public List<WorldDiplomacyCompressionSummary> CompressionSummaries { get; set; }
    public List<WarPressureEntry> WarPressure { get; set; }
    public List<WorldDiplomacyWarLedger> ActiveWarLedgers { get; set; }
    public List<WorldDiplomacyBattleFact> RecentBattles { get; set; }
    public List<NativeDiplomacySignal> NativeSignals { get; set; }
    public List<WorldDiplomacyJob> Jobs { get; set; }
    public List<WorldDiplomacyExchange> SuspendedExchanges { get; set; }
    public Dictionary<string, int> LastOffensiveWarDayByKingdom { get; set; }
    public Dictionary<string, int> LastPeaceDayByPair { get; set; }
}

public sealed class WorldDiplomacyOfferCooldown { }
public sealed class WorldDiplomacyDialogueArrangement { }
public sealed class DiplomacyRequestBudget { }
public sealed class WorldDiplomacyThreat { }
public sealed class WorldDiplomacyPrestigeRelationModifier { }
public sealed class WorldDiplomacyCanonicalHistoryState { }
public sealed class WorldDiplomacyRound { }
public sealed class WorldDiplomacyPropagationArrival { }
public sealed class WorldDiplomacySettlementKnowledge { }
public sealed class WorldDiplomacyKingdomKnowledge { }
public sealed class WorldDiplomacyParticipationRequest { }
public sealed class WorldDiplomacyPendingSpeech { }
public sealed class WorldDiplomacyRelayArrival { }
public sealed class WorldDiplomacyPlayerOpportunity { }
public sealed class WorldDiplomacyRoundSummary { }
public sealed class WorldDiplomacyPolicySignal { }
public sealed class WorldDiplomacyTopicUse { }
public sealed class WorldDiplomacyDocument { }
public sealed class WorldDiplomacyAnnualSummary { }
public sealed class WorldDiplomacyCompressionSummary { }
public sealed class WarPressureEntry { }
public sealed class WorldDiplomacyWarLedger { }
public sealed class WorldDiplomacyBattleFact { }
public sealed class NativeDiplomacySignal { }
public sealed class WorldDiplomacyJob { }
public sealed class WorldDiplomacyExchange { }
