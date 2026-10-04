using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AnimusForge;

public sealed class WorldDiplomacyRound
{
	[JsonProperty("schemaVersion")] public int SchemaVersion { get; set; }
	[JsonProperty("roundId")] public string RoundId { get; set; } = "";
	[JsonProperty("initiatorKingdomId")] public string InitiatorKingdomId { get; set; } = "";
	[JsonProperty("rootDocumentId")] public string RootDocumentId { get; set; } = "";
	[JsonProperty("finalDocumentId")] public string FinalDocumentId { get; set; } = "";
	[JsonProperty("state")] public string State { get; set; } = "active";
	[JsonProperty("startedDay")] public int StartedDay { get; set; }
	[JsonProperty("lastActivityDay")] public int LastActivityDay { get; set; }
	[JsonProperty("softEndDay")] public int SoftEndDay { get; set; }
	[JsonProperty("completedDay")] public int CompletedDay { get; set; }
	[JsonProperty("closeReason")] public string CloseReason { get; set; } = "";
	[JsonProperty("isPlayerInsertion")] public bool IsPlayerInsertion { get; set; }
	[JsonProperty("automaticDocumentsStarted")] public int AutomaticDocumentsStarted { get; set; }
	[JsonProperty("automaticCircuitBreakerTripped")] public bool AutomaticCircuitBreakerTripped { get; set; }
	[JsonProperty("consecutiveTechnicalGenerationFailures")] public int ConsecutiveTechnicalGenerationFailures { get; set; }
	[JsonProperty("relayPlanned")] public bool RelayPlanned { get; set; }
	[JsonProperty("relayRouteKingdomIds")] public List<string> RelayRouteKingdomIds { get; set; } = new List<string>();
	[JsonProperty("relayCursor")] public int RelayCursor { get; set; }
	[JsonProperty("relayDirection")] public int RelayDirection { get; set; } = 1;
	[JsonProperty("relayPassNumber")] public int RelayPassNumber { get; set; }
	[JsonProperty("relayPassStartedDay")] public int RelayPassStartedDay { get; set; }
	[JsonProperty("relayPassDurationDays")] public int RelayPassDurationDays { get; set; }
	[JsonProperty("relaySequence")] public int RelaySequence { get; set; }
	[JsonProperty("relayWaiting")] public bool RelayWaiting { get; set; }
	[JsonProperty("hardEndDay")] public int HardEndDay { get; set; }
	[JsonProperty("roundTopic")] public string RoundTopic { get; set; } = "";
	[JsonProperty("topicCategory")] public string TopicCategory { get; set; } = "";
	[JsonProperty("topicFingerprint")] public string TopicFingerprint { get; set; } = "";
	[JsonProperty("topicSeedContext")] public string TopicSeedContext { get; set; } = "";
	[JsonProperty("eventSourceType")] public string EventSourceType { get; set; } = "";
	[JsonProperty("eventMotif")] public string EventMotif { get; set; } = "";
	[JsonProperty("eventLocation")] public string EventLocation { get; set; } = "";
	[JsonProperty("allowedFiction")] public string AllowedFiction { get; set; } = "";
	[JsonProperty("forbiddenFiction")] public string ForbiddenFiction { get; set; } = "";
	[JsonProperty("requiresSharedBorder")] public bool RequiresSharedBorder { get; set; }
	[JsonProperty("potentialActionIntents")] public List<string> PotentialActionIntents { get; set; } = new List<string>();
	[JsonProperty("commonContractSnapshot")] public string CommonContractSnapshot { get; set; } = "";
	[JsonProperty("commonContractSnapshotInitialized")] public bool CommonContractSnapshotInitialized { get; set; }
	[JsonProperty("cachePrefix")] public string CachePrefix { get; set; } = "";
	[JsonProperty("externalSignalKeys")] public List<string> ExternalSignalKeys { get; set; } = new List<string>();
	[JsonProperty("attachedPolicySignals")] public List<WorldDiplomacyPolicySignal> AttachedPolicySignals { get; set; } = new List<WorldDiplomacyPolicySignal>();
	[JsonProperty("externalOpeningContext")] public string ExternalOpeningContext { get; set; } = "";
	[JsonProperty("llmTranscript")] public List<WorldDiplomacyLlmMessage> LlmTranscript { get; set; } = new List<WorldDiplomacyLlmMessage>();
	[JsonProperty("llmProfiledKingdomIds")] public List<string> LlmProfiledKingdomIds { get; set; } = new List<string>();
	[JsonProperty("llmLastStateSignatureByKingdom")] public Dictionary<string, string> LlmLastStateSignatureByKingdom { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	[JsonProperty("roundStatus")] public string RoundStatus { get; set; } = "active";
	[JsonProperty("executedActionCount")] public int ExecutedActionCount { get; set; }
	[JsonProperty("substantiveProgressCount")] public int SubstantiveProgressCount { get; set; }
	[JsonProperty("diplomaticActionAttemptCount")] public int DiplomaticActionAttemptCount { get; set; }
	[JsonProperty("actionAttemptCountAtPassStart")] public int ActionAttemptCountAtPassStart { get; set; }
	[JsonProperty("consecutiveNoActionPasses")] public int ConsecutiveNoActionPasses { get; set; }
	[JsonProperty("lastAccountedRelayPassNumber")] public int LastAccountedRelayPassNumber { get; set; }
	[JsonProperty("lastSubstantiveProgressDay")] public int LastSubstantiveProgressDay { get; set; }
	[JsonProperty("finalActionOpportunityIssued")] public bool FinalActionOpportunityIssued { get; set; }
	[JsonProperty("pendingOffers")] public List<WorldDiplomacyRoundOffer> PendingOffers { get; set; } = new List<WorldDiplomacyRoundOffer>();
	[JsonProperty("participants")] public List<WorldDiplomacyRoundParticipant> Participants { get; set; } = new List<WorldDiplomacyRoundParticipant>();
	[JsonProperty("resultSettlementPending")] public bool ResultSettlementPending { get; set; }
	[JsonProperty("resultSettlementTriggerDocumentId")] public string ResultSettlementTriggerDocumentId { get; set; } = "";
	[JsonProperty("resultSettlementCloseReason")] public string ResultSettlementCloseReason { get; set; } = "";
	[JsonProperty("resultSettlementRoundStatus")] public string ResultSettlementRoundStatus { get; set; } = "resolved";
	[JsonProperty("resultSettlementRouteInitialized")] public bool ResultSettlementRouteInitialized { get; set; }
	[JsonProperty("resultSettlementCurrentSlotId")] public string ResultSettlementCurrentSlotId { get; set; } = "";
	[JsonProperty("resultSettlementPlayerWaitingSinceDay")] public int ResultSettlementPlayerWaitingSinceDay { get; set; }
	[JsonProperty("resultSettlementSlots")] public List<WorldDiplomacyResultSettlementSlot> ResultSettlementSlots { get; set; } = new List<WorldDiplomacyResultSettlementSlot>();
	[JsonProperty("resultSettlementWarDocumentIds")] public List<string> ResultSettlementWarDocumentIds { get; set; } = new List<string>();

    [JsonProperty("conversationRevision")] public int ConversationRevision { get; set; }
    [JsonProperty("playerResponses")] public List<WorldDiplomacyPlayerResponse> PlayerResponses { get; set; } = new List<WorldDiplomacyPlayerResponse>();
    [JsonProperty("playerWaitReminderDay")] public int PlayerWaitReminderDay { get; set; } = -1;
    [JsonProperty("dialogueArrangementId")] public string DialogueArrangementId { get; set; } = "";
}

public sealed class WorldDiplomacyLlmMessage
{
	[JsonProperty("role")] public string Role { get; set; } = "";
	[JsonProperty("content")] public string Content { get; set; } = "";
	[JsonProperty("strategicProfileKingdomId")] public string StrategicProfileKingdomId { get; set; } = "";
}

public sealed class WorldDiplomacyRoundOffer
{
	[JsonProperty("sourceDocumentId")] public string SourceDocumentId { get; set; } = "";
	[JsonProperty("sourceActionId")] public string SourceActionId { get; set; } = "";
	[JsonProperty("proposerKingdomId")] public string ProposerKingdomId { get; set; } = "";
	[JsonProperty("targetKingdomId")] public string TargetKingdomId { get; set; } = "";
	[JsonProperty("intent")] public string Intent { get; set; } = "";
	[JsonProperty("status")] public string Status { get; set; } = "open";
	[JsonProperty("createdDay")] public int CreatedDay { get; set; }
}

public sealed class WorldDiplomacyRoundParticipant
{
	[JsonProperty("kingdomId")] public string KingdomId { get; set; } = "";
	[JsonProperty("state")] public string State { get; set; } = "observer";
	[JsonProperty("mandatoryReplyPending")] public bool MandatoryReplyPending { get; set; }
	[JsonProperty("lastSpokeDay")] public int LastSpokeDay { get; set; }
	[JsonProperty("lastEvaluationDay")] public int LastEvaluationDay { get; set; }
	[JsonProperty("lastEvaluationMaterialDay")] public int LastEvaluationMaterialDay { get; set; }
	[JsonProperty("lastTriggeredDocumentId")] public string LastTriggeredDocumentId { get; set; } = "";
	[JsonProperty("mandatorySinceDay")] public int MandatorySinceDay { get; set; }
	[JsonProperty("reminderSent")] public bool ReminderSent { get; set; }
	[JsonProperty("selectedForRelay")] public bool SelectedForRelay { get; set; }
	[JsonProperty("isPlayerAsync")] public bool IsPlayerAsync { get; set; }
	[JsonProperty("turnCount")] public int TurnCount { get; set; }
	[JsonProperty("role")] public string Role { get; set; } = "";
	[JsonProperty("agenda")] public string Agenda { get; set; } = "";
	[JsonProperty("primaryTargetKingdomId")] public string PrimaryTargetKingdomId { get; set; } = "";
	[JsonProperty("preferredOutcome")] public string PreferredOutcome { get; set; } = "";
	[JsonProperty("redLine")] public string RedLine { get; set; } = "";
	[JsonProperty("leverage")] public string Leverage { get; set; } = "";
	[JsonProperty("requiredContribution")] public string RequiredContribution { get; set; } = "";
	[JsonProperty("contributionMade")] public bool ContributionMade { get; set; }
}
