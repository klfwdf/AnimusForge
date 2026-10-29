using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AnimusForge;

public sealed class WorldDiplomacyStorage
{
    [JsonProperty("materialCountrySchemaVersion")] public int MaterialCountrySchemaVersion { get; set; }
    [JsonProperty("lastNpcPlayerContactDay")] public int LastNpcPlayerContactDay { get; set; } = -1;
    [JsonProperty("npcPlayerContactCount")] public int NpcPlayerContactCount { get; set; }
    [JsonProperty("publishedPlayerContacts")] public HashSet<string> PublishedPlayerContacts { get; set; } = new(StringComparer.Ordinal);
    [JsonProperty("materialInvestigations")] public Dictionary<string, DiplomacyMaterialInvestigation> MaterialInvestigations { get; set; } = new(StringComparer.Ordinal);
    [JsonProperty("materialLeads")] public List<DiplomacyMaterialLead> MaterialLeads { get; set; } = new();
    [JsonProperty("countryParticipation")] public Dictionary<string, DiplomacyCountryParticipation> CountryParticipation { get; set; } = new(StringComparer.Ordinal);
    [JsonProperty("speechReviews")] public List<DiplomacySpeechReview> SpeechReviews { get; set; } = new();
    [JsonProperty("reviewedSpeechSources")] public List<string> ReviewedSpeechSources { get; set; } = new();
    [JsonProperty("legacyPreservedJobs")] public List<WorldDiplomacyJob> LegacyPreservedJobs { get; set; } = new();
    [JsonProperty("aidRequests")] public List<DiplomacyAidRequest> AidRequests { get; set; } = new();
    [JsonProperty("eventAnnouncements")] public List<DiplomacyEventAnnouncement> EventAnnouncements { get; set; } = new();
    [JsonProperty("eventEffects")] public List<DiplomacyEventEffectState> EventEffects { get; set; } = new();
    [JsonProperty("recentEventImages")] public List<string> RecentEventImages { get; set; } = new();
    [JsonProperty("eventsPausedAtHour")] public double EventsPausedAtHour { get; set; } = -1;
    [JsonProperty("playerLifecycleSchemaVersion")] public int PlayerLifecycleSchemaVersion { get; set; }
    [JsonProperty("playerLifecycleRecoveryRoundIds")] public List<string> PlayerLifecycleRecoveryRoundIds { get; set; } = new();
    [JsonProperty("playerDiplomacySchemaVersion")] public int PlayerDiplomacySchemaVersion { get; set; }
    [JsonProperty("nextPlayerAgendaDay")] public int NextPlayerAgendaDay { get; set; }
    [JsonProperty("playerAgendaCursor")] public int PlayerAgendaCursor { get; set; }
    [JsonProperty("oralServiceRetryAfterUtcTicks")] public long OralServiceRetryAfterUtcTicks { get; set; }
    [JsonProperty("playerServiceRetryAfterUtcTicks")] public long PlayerServiceRetryAfterUtcTicks { get; set; }
    [JsonProperty("lastPlayerAgendaEvaluationDay")] public int LastPlayerAgendaEvaluationDay { get; set; } = -1;
    [JsonProperty("playerAgendaCandidates")] public List<WorldDiplomacyPlayerAgendaCandidate> PlayerAgendaCandidates { get; set; } = new();
    [JsonProperty("playerDisputeRefusals")] public List<WorldDiplomacyPlayerDisputeRefusal> PlayerDisputeRefusals { get; set; } = new();
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

	// Null identifies an old save; initialize once from ordinary event start dates.
	[JsonProperty("lastOrdinaryRoundStartedDay")]
	public int? LastOrdinaryRoundStartedDay { get; set; }

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

    [JsonProperty("historyFragmentWork")] public DiplomacyHistoryFragmentWork HistoryFragmentWork { get; set; }
    [JsonProperty("compressionInputBudgetTokens")] public int CompressionInputBudgetTokens { get; set; }
    [JsonProperty("speechServiceOutage")] public bool SpeechServiceOutage { get; set; }
    [JsonProperty("speechServiceRecoveryEpoch")] public long SpeechServiceRecoveryEpoch { get; set; }
    [JsonProperty("compressionConfigurationFingerprint")] public string CompressionConfigurationFingerprint { get; set; } = "";
    [JsonProperty("compressionHoldReason")] public string CompressionHoldReason { get; set; } = "";

	[JsonProperty("compressionRetryAfterHour")]
	public int CompressionRetryAfterHour { get; set; }

	[JsonProperty("compressionRetryAttempts")]
	public int CompressionRetryAttempts { get; set; }

	[JsonProperty("serviceCooldownUntilHour")]
	public int ServiceCooldownUntilHour { get; set; }

	[JsonProperty("consecutiveServiceFailures")]
	public int ConsecutiveServiceFailures { get; set; }

    [JsonProperty("roundSchedulingSchemaVersion")] public int RoundSchedulingSchemaVersion { get; set; }
    [JsonProperty("requestBudget")] public DiplomacyRequestBudget RequestBudget { get; set; } = new DiplomacyRequestBudget();
    [JsonProperty("serviceRetryAfterUtcTicks")] public long ServiceRetryAfterUtcTicks { get; set; }
    [JsonProperty("concurrentRounds")] public List<WorldDiplomacyRound> ConcurrentRounds { get; set; } = new List<WorldDiplomacyRound>();
    [JsonProperty("dialogueArrangements")] public List<WorldDiplomacyDialogueArrangement> DialogueArrangements { get; set; } = new List<WorldDiplomacyDialogueArrangement>();
}
