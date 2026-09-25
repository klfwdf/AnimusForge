using System.Collections.Generic;
using Newtonsoft.Json;

namespace AnimusForge;

public sealed class WorldDiplomacyExchange
{
	[JsonProperty("exchangeId")]
	public string ExchangeId { get; set; } = "";

	[JsonProperty("initiatorKingdomId")]
	public string InitiatorKingdomId { get; set; } = "";

	[JsonProperty("targetKingdomId")]
	public string TargetKingdomId { get; set; } = "";

	[JsonProperty("sourceDocumentId")]
	public string SourceDocumentId { get; set; } = "";

	[JsonProperty("responseDocumentId")]
	public string ResponseDocumentId { get; set; } = "";

	[JsonProperty("pendingAction")]
	public string PendingAction { get; set; } = "";

	[JsonProperty("pendingPeaceTerms")]
	public WorldDiplomacyPeaceTerms PendingPeaceTerms { get; set; }

	[JsonProperty("negotiationRevision")]
	public int NegotiationRevision { get; set; }

	[JsonProperty("state")]
	public string State { get; set; } = "";

	[JsonProperty("stateBeforeSuspension")]
	public string StateBeforeSuspension { get; set; } = "";

	[JsonProperty("startedDay")]
	public int StartedDay { get; set; }

	[JsonProperty("responseDueDay")]
	public int ResponseDueDay { get; set; }

	[JsonProperty("closeDueDay")]
	public int CloseDueDay { get; set; }

	[JsonProperty("suspendedDay")]
	public int SuspendedDay { get; set; }

	[JsonProperty("completedDay")]
	public int CompletedDay { get; set; }

	[JsonProperty("closeReason")]
	public string CloseReason { get; set; } = "";

	[JsonProperty("isForced")]
	public bool IsForced { get; set; }

	[JsonProperty("isPlayerInsertion")]
	public bool IsPlayerInsertion { get; set; }

	[JsonProperty("reminderSent")]
	public bool ReminderSent { get; set; }
}

public sealed class WorldDiplomacyJob
{
	[JsonProperty("awaitingHistoryCompression")]
	public bool AwaitingHistoryCompression { get; set; }

	[JsonProperty("inputBudgetHistoryTargetTokens")]
	public int InputBudgetHistoryTargetTokens { get; set; }

	[JsonProperty("historyThroughSequence")]
	public long HistoryThroughSequence { get; set; }

	[JsonProperty("historyRevision")]
	public long HistoryRevision { get; set; }

	[JsonProperty("historyPrefixHash")]
	public string HistoryPrefixHash { get; set; } = "";

	[JsonProperty("historyEstimatedTokens")]
	public long HistoryEstimatedTokens { get; set; }

	[JsonProperty("historySnapshotThroughSequence")]
	public long HistorySnapshotThroughSequence { get; set; }

	[JsonProperty("historySnapshotHash")]
	public string HistorySnapshotHash { get; set; } = "";

	[JsonProperty("roundId")]
	public string RoundId { get; set; } = "";

	[JsonProperty("candidateKingdomIds")]
	public List<string> CandidateKingdomIds { get; set; } = new List<string>();

	[JsonProperty("triggerDocumentIds")]
	public List<string> TriggerDocumentIds { get; set; } = new List<string>();

	[JsonProperty("presentedThreatDocumentIds")]
	public List<string> PresentedThreatDocumentIds { get; set; } = new List<string>();

	[JsonProperty("presentedThreatFollowThroughDocumentIds")]
	public List<string> PresentedThreatFollowThroughDocumentIds { get; set; } = new List<string>();

	[JsonProperty("presentedLegalActionSignature")]
	public string PresentedLegalActionSignature { get; set; } = "";

	[JsonProperty("resultSettlementSlotId")]
	public string ResultSettlementSlotId { get; set; } = "";

	[JsonProperty("allowAutonomousNoAction")]
	public bool AllowAutonomousNoAction { get; set; }

	[JsonProperty("jobId")]
	public string JobId { get; set; } = "";

	[JsonProperty("kind")]
	public string Kind { get; set; } = "";

	[JsonProperty("priority")]
	public int Priority { get; set; }

	[JsonProperty("createdDay")]
	public int CreatedDay { get; set; }

	[JsonProperty("exchangeId")]
	public string ExchangeId { get; set; } = "";

	[JsonProperty("documentId")]
	public string DocumentId { get; set; } = "";

	[JsonProperty("sourceDocumentId")]
	public string SourceDocumentId { get; set; } = "";

	[JsonProperty("authorKingdomId")]
	public string AuthorKingdomId { get; set; } = "";

	[JsonProperty("targetKingdomId")]
	public string TargetKingdomId { get; set; } = "";

	[JsonProperty("forcedIntent")]
	public string ForcedIntent { get; set; } = "";

	[JsonProperty("isResponse")]
	public bool IsResponse { get; set; }

	[JsonProperty("isExternalResponseOnly")]
	public bool IsExternalResponseOnly { get; set; }

	[JsonProperty("isReminder")]
	public bool IsReminder { get; set; }

	[JsonProperty("isRelayTurn")]
	public bool IsRelayTurn { get; set; }

	[JsonProperty("allowUntargeted")]
	public bool AllowUntargeted { get; set; }

	[JsonProperty("previousKingdomId")]
	public string PreviousKingdomId { get; set; } = "";

	[JsonProperty("wasAtWarWhenQueued")]
	public bool WasAtWarWhenQueued { get; set; }

	[JsonProperty("semanticRepairAttempts")]
	public int SemanticRepairAttempts { get; set; }

	[JsonProperty("isRunning")]
	public bool IsRunning { get; set; }

	[JsonProperty("systemPrompt")]
	public string SystemPrompt { get; set; } = "";

	[JsonProperty("userPrompt")]
	public string UserPrompt { get; set; } = "";

	[JsonProperty("llmMessages")]
	public List<WorldDiplomacyLlmMessage> LlmMessages { get; set; } = new List<WorldDiplomacyLlmMessage>();

	[JsonProperty("profiledKingdomId")]
	public string ProfiledKingdomId { get; set; } = "";

	[JsonProperty("strategicProfileKingdomId")]
	public string StrategicProfileKingdomId { get; set; } = "";

	[JsonProperty("cacheAffinityKey")]
	public string CacheAffinityKey { get; set; } = "";

	[JsonProperty("maxTokens")]
	public int MaxTokens { get; set; }

	[JsonProperty("compressionYear")]
	public int CompressionYear { get; set; }

	[JsonProperty("compressionDocumentIds")]
	public List<string> CompressionDocumentIds { get; set; } = new List<string>();

	[JsonProperty("compressionBatchId")]
	public string CompressionBatchId { get; set; } = "";

	[JsonProperty("compressionRoundIds")]
	public List<string> CompressionRoundIds { get; set; } = new List<string>();

	[JsonProperty("compressionTokenCount")]
	public long CompressionTokenCount { get; set; }

	[JsonProperty("compressionThroughSequence")]
	public long CompressionThroughSequence { get; set; }

	[JsonProperty("compressionTargetTokens")]
	public int CompressionTargetTokens { get; set; }

	[JsonProperty("compressionOverallTargetTokens")]
	public int CompressionOverallTargetTokens { get; set; }
}
