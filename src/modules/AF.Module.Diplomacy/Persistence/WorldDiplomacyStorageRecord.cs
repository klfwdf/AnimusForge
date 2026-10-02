using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AnimusForge;

public sealed class WorldDiplomacyStorage
{
	[JsonProperty("diplomacyNotificationStateSchemaVersion")]
	public int DiplomacyNotificationStateSchemaVersion { get; set; }

	[JsonProperty("resultSettlementStateSchemaVersion")]
	public int ResultSettlementStateSchemaVersion { get; set; }

	[JsonProperty("offerCooldownStateSchemaVersion")]
	public int OfferCooldownStateSchemaVersion { get; set; }

	[JsonProperty("offerCooldowns")]
	public List<WorldDiplomacyOfferCooldown> OfferCooldowns { get; set; } = new List<WorldDiplomacyOfferCooldown>();

	[JsonProperty("diplomaticThreatStateSchemaVersion")]
	public int DiplomaticThreatStateSchemaVersion { get; set; }

	[JsonProperty("diplomaticThreats")]
	public List<WorldDiplomacyThreat> DiplomaticThreats { get; set; } = new List<WorldDiplomacyThreat>();

	[JsonProperty("diplomaticReputationByKingdom")]
	public Dictionary<string, int> NationalPrestigeByKingdom { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	[JsonProperty("internationalReputationByKingdom")]
	public Dictionary<string, int> InternationalReputationByKingdom { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	[JsonProperty("internationalReputationNaturalChangeLastDayByKingdom")]
	public Dictionary<string, int> InternationalReputationNaturalChangeLastDayByKingdom { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	[JsonProperty("nationalPrestigeRelationModifiers")]
	public List<WorldDiplomacyPrestigeRelationModifier> NationalPrestigeRelationModifiers { get; set; } = new List<WorldDiplomacyPrestigeRelationModifier>();

	[JsonProperty("historyMemorySchemaVersion")]
	public int HistoryMemorySchemaVersion { get; set; }

	[JsonProperty("promptContractVersion")]
	public int PromptContractVersion { get; set; }

	[JsonProperty("canonicalHistory")]
	public WorldDiplomacyCanonicalHistoryState CanonicalHistory { get; set; } = new WorldDiplomacyCanonicalHistoryState();

	[JsonProperty("decisionArchitectureVersion")]
	public int DecisionArchitectureVersion { get; set; }

	[JsonProperty("propagationReliabilityVersion")]
	public int PropagationReliabilityVersion { get; set; }

	[JsonProperty("initialPeacePending")]
	public bool InitialPeacePending { get; set; }

	[JsonProperty("initialPeaceApplied")]
	public bool InitialPeaceApplied { get; set; }

	[JsonProperty("activeRound")]
	public WorldDiplomacyRound ActiveRound { get; set; }

	[JsonProperty("completedRounds")]
	public List<WorldDiplomacyRound> CompletedRounds { get; set; } = new List<WorldDiplomacyRound>();

	[JsonProperty("propagationArrivals")]
	public List<WorldDiplomacyPropagationArrival> PropagationArrivals { get; set; } = new List<WorldDiplomacyPropagationArrival>();

	[JsonProperty("settlementKnowledge")]
	public List<WorldDiplomacySettlementKnowledge> SettlementKnowledge { get; set; } = new List<WorldDiplomacySettlementKnowledge>();

	[JsonProperty("kingdomKnowledge")]
	public List<WorldDiplomacyKingdomKnowledge> KingdomKnowledge { get; set; } = new List<WorldDiplomacyKingdomKnowledge>();

	[JsonProperty("nobleKnowledge")]
	public List<WorldDiplomacyKingdomKnowledge> NobleKnowledge { get; set; } = new List<WorldDiplomacyKingdomKnowledge>();

	[JsonProperty("courtKnowledgeMigratedToNobles")]
	public bool CourtKnowledgeMigratedToNobles { get; set; }

	[JsonProperty("pendingParticipationEvaluations")]
	public List<WorldDiplomacyParticipationRequest> PendingParticipationEvaluations { get; set; } = new List<WorldDiplomacyParticipationRequest>();

	[JsonProperty("pendingSpeeches")]
	public List<WorldDiplomacyPendingSpeech> PendingSpeeches { get; set; } = new List<WorldDiplomacyPendingSpeech>();

	[JsonProperty("relayArrivals")]
	public List<WorldDiplomacyRelayArrival> RelayArrivals { get; set; } = new List<WorldDiplomacyRelayArrival>();

	[JsonProperty("playerOpportunities")]
	public List<WorldDiplomacyPlayerOpportunity> PlayerOpportunities { get; set; } = new List<WorldDiplomacyPlayerOpportunity>();

	[JsonProperty("roundSummaries")]
	public List<WorldDiplomacyRoundSummary> RoundSummaries { get; set; } = new List<WorldDiplomacyRoundSummary>();

	[JsonProperty("pendingPolicySignals")]
	public List<WorldDiplomacyPolicySignal> PendingPolicySignals { get; set; } = new List<WorldDiplomacyPolicySignal>();

	[JsonProperty("processedPolicySignalKeys")]
	public List<string> ProcessedPolicySignalKeys { get; set; } = new List<string>();

	[JsonProperty("recentTopicUses")]
	public List<WorldDiplomacyTopicUse> RecentTopicUses { get; set; } = new List<WorldDiplomacyTopicUse>();

	[JsonProperty("forcedWarToggleWasEnabled")]
	public bool ForcedWarToggleWasEnabled { get; set; } = true;

	[JsonProperty("lastAppliedContinentSpreadDays")]
	public int LastAppliedContinentSpreadDays { get; set; }

	[JsonProperty("lastAppliedCourtDeliveryDays")]
	public int LastAppliedCourtDeliveryDays { get; set; }

	[JsonProperty("lastAppliedCivilianSpreadDays")]
	public int LastAppliedCivilianSpreadDays { get; set; }

	[JsonProperty("documents")]
	public List<WorldDiplomacyDocument> Documents { get; set; } = new List<WorldDiplomacyDocument>();

	[JsonProperty("annualSummaries")]
	public List<WorldDiplomacyAnnualSummary> AnnualSummaries { get; set; } = new List<WorldDiplomacyAnnualSummary>();

	[JsonProperty("compressionSummaries")]
	public List<WorldDiplomacyCompressionSummary> CompressionSummaries { get; set; } = new List<WorldDiplomacyCompressionSummary>();

	[JsonProperty("warPressure")]
	public List<WarPressureEntry> WarPressure { get; set; } = new List<WarPressureEntry>();

	[JsonProperty("activeWarLedgers")]
	public List<WorldDiplomacyWarLedger> ActiveWarLedgers { get; set; } = new List<WorldDiplomacyWarLedger>();

	[JsonProperty("recentBattles")]
	public List<WorldDiplomacyBattleFact> RecentBattles { get; set; } = new List<WorldDiplomacyBattleFact>();

	[JsonProperty("nativeSignals")]
	public List<NativeDiplomacySignal> NativeSignals { get; set; } = new List<NativeDiplomacySignal>();

	[JsonProperty("jobs")]
	public List<WorldDiplomacyJob> Jobs { get; set; } = new List<WorldDiplomacyJob>();

	[JsonProperty("activeExchange")]
	public WorldDiplomacyExchange ActiveExchange { get; set; }

	[JsonProperty("suspendedExchanges")]
	public List<WorldDiplomacyExchange> SuspendedExchanges { get; set; } = new List<WorldDiplomacyExchange>();

	[JsonProperty("lastOffensiveWarDayByKingdom")]
	public Dictionary<string, int> LastOffensiveWarDayByKingdom { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	[JsonProperty("lastPeaceDayByPair")]
	public Dictionary<string, int> LastPeaceDayByPair { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	[JsonProperty("nextNormalRoundDay")]
	public int NextNormalRoundDay { get; set; }

	[JsonProperty("lastAppliedRoundIntervalDays")]
	public int LastAppliedRoundIntervalDays { get; set; }

	[JsonProperty("rotationIndex")]
	public int RotationIndex { get; set; }

	[JsonProperty("lastCompressedYear")]
	public int LastCompressedYear { get; set; } = -1;

	[JsonProperty("diplomacyTokensSinceCompression")]
	public long DiplomacyTokensSinceCompression { get; set; }

	[JsonProperty("diplomacyCompressionPending")]
	public bool DiplomacyCompressionPending { get; set; }

	[JsonProperty("lastDiplomacyCompressionDay")]
	public int LastDiplomacyCompressionDay { get; set; } = -1;

	[JsonProperty("compressionSequence")]
	public int CompressionSequence { get; set; }

	[JsonProperty("compressionRetryAfterHour")]
	public int CompressionRetryAfterHour { get; set; }

	[JsonProperty("compressionRetryAttempts")]
	public int CompressionRetryAttempts { get; set; }

	[JsonProperty("serviceCooldownUntilHour")]
	public int ServiceCooldownUntilHour { get; set; }

	[JsonProperty("consecutiveServiceFailures")]
	public int ConsecutiveServiceFailures { get; set; }
}
