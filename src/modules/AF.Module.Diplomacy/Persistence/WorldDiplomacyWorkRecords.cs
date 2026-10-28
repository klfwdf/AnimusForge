using System;
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
    [JsonProperty("storyReviewVersion")] public int StoryReviewVersion { get; set; }
    [JsonProperty("responseContinuityReviewVersion")] public int ResponseContinuityReviewVersion { get; set; }
    public string EndingRecoveryDocumentId { get; set; } = "";
    public string PlayerContactId { get; set; } = "";
    public int ContactNotBeforeDay { get; set; }
    public string DeferredContactDraft { get; set; } = "";
    public string AuditedAgendaQuote { get; set; } = "";
    public List<string> AuditedAgendaAnswers { get; set; } = new();
    public int MaterialSearchPass { get; set; }
    public int MaterialNextSearchDay { get; set; }
    public string MaterialRepairFeedback { get; set; } = "";
    [JsonProperty("materialCountryContractVersion")] public int MaterialCountryContractVersion { get; set; }
    [JsonProperty("declarationAuditEvidenceVersion")] public int DeclarationAuditEvidenceVersion { get; set; }
    [JsonProperty("declarationAnswerSources")] public List<string> DeclarationAnswerSources { get; set; } = new();
    [JsonProperty("declarationAnswerSnapshotVersion")] public int DeclarationAnswerSnapshotVersion { get; set; }
    [JsonProperty("declarationRepairVersion")] public int DeclarationRepairVersion { get; set; }
    [JsonProperty("declarationRepairField")] public string DeclarationRepairField { get; set; } = "";
    [JsonProperty("declarationRepairAttempts")] public int DeclarationRepairAttempts { get; set; }
    [JsonProperty("positionCorrectionRequired")] public bool PositionCorrectionRequired { get; set; }
    [JsonProperty("positionOwnerReviewVersion")] public int PositionOwnerReviewVersion { get; set; }
    [JsonProperty("eventNarrativeReviewVersion")] public int EventNarrativeReviewVersion { get; set; }
    [JsonProperty("eventProgressContractVersion")] public int EventProgressContractVersion { get; set; }
    [JsonProperty("endingContextVersion")] public int EndingContextVersion { get; set; }
    [JsonProperty("worldBookWaitRevision")] public string WorldBookWaitRevision { get; set; } = "";
    [JsonProperty("worldBookWaitNotice")] public string WorldBookWaitNotice { get; set; } = "";
    [JsonProperty("negotiationIssueId")] public string NegotiationIssueId { get; set; } = "";
    [JsonProperty("materialReactionId")] public string MaterialReactionId { get; set; } = "";
    [JsonProperty("lifecycleClassificationAttempts")] public int LifecycleClassificationAttempts { get; set; }
    [JsonProperty("replyRoutingCandidateDocumentIds")] public List<string> ReplyRoutingCandidateDocumentIds { get; set; } = new();
    [JsonProperty("ownershipEpoch")] public int OwnershipEpoch { get; set; }
    [JsonProperty("schedulingOwner")] public string SchedulingOwner { get; set; } = "";
    [JsonProperty("playerAuthorityRulerId")] public string PlayerAuthorityRulerId { get; set; } = "";
    [JsonProperty("silenceConsequence")] public string SilenceConsequence { get; set; } = "";
    [JsonProperty("silenceSourceDocumentId")] public string SilenceSourceDocumentId { get; set; } = "";
    [JsonProperty("suspendedReason")] public string SuspendedReason { get; set; } = "";
    [JsonProperty("technicalRecoveryAttempts")] public int TechnicalRecoveryAttempts { get; set; }
    [JsonProperty("recoveryConfiguration")] public string RecoveryConfiguration { get; set; } = "";
    [JsonProperty("recoveryRevision")] public int RecoveryRevision { get; set; }
    [JsonProperty("recoveryKnowledgeRevision")] public int RecoveryKnowledgeRevision { get; set; }
    [JsonProperty("recoveryServiceEpoch")] public long RecoveryServiceEpoch { get; set; }
    [JsonProperty("recoverOnService")] public bool RecoverOnService { get; set; }
    [JsonIgnore] internal long NextRecoveryProbeUtcTicks;
    [JsonProperty("privacySourceMaterial")] public string PrivacySourceMaterial { get; set; } = "";
    [JsonProperty("privacyPublicContext")] public string PrivacyPublicContext { get; set; } = "";
    [JsonProperty("privacyRevision")] public int PrivacyRevision { get; set; } = -1;
    [JsonProperty("privacyKnowledgeRevision")] public int PrivacyKnowledgeRevision { get; set; } = -1;
    [JsonProperty("courtKnowledgeRevision")] public int CourtKnowledgeRevision { get; set; }
    [JsonProperty("privacyRewrites")] public int PrivacyRewrites { get; set; }
    [JsonProperty("privacyAuditRetries")] public int PrivacyAuditRetries { get; set; }
    [JsonProperty("privacyRepairFeedback")] public string PrivacyRepairFeedback { get; set; } = "";
    [JsonProperty("privacyDraft")] public string PrivacyDraft { get; set; } = "";
    [JsonProperty("eventMaterialSnapshot")] public string EventMaterialSnapshot { get; set; } = "";
    [JsonProperty("eventProbeSnapshot")] public string EventProbeSnapshot { get; set; } = "";
    [JsonProperty("eventProbeVersion")] public string EventProbeVersion { get; set; } = "";
    [JsonProperty("eventExplorationTheme")] public string EventExplorationTheme { get; set; } = "";
    [JsonProperty("eventThemePass")] public int EventThemePass { get; set; }
    [JsonProperty("eventThemeInvestigations")] public int EventThemeInvestigations { get; set; }
    [JsonProperty("playerLoreContractVersion")] public int PlayerLoreContractVersion { get; set; }
    [JsonProperty("eventSelectionStage")] public string EventSelectionStage { get; set; } = "";
    [JsonProperty("eventCandidatePool")] public string EventCandidatePool { get; set; } = "";
    [JsonProperty("eventSelectedCandidate")] public string EventSelectedCandidate { get; set; } = "";
    [JsonProperty("admissionSeedRewrites")] public int AdmissionSeedRewrites { get; set; }
    [JsonProperty("countryAdmissionSeed")] public string CountryAdmissionSeed { get; set; } = "";
    [JsonProperty("countryAdmission")] public string CountryAdmission { get; set; } = "";
    [JsonProperty("admissionSupplementedCountries")] public List<string> AdmissionSupplementedCountries { get; set; } = new();
    [JsonProperty("admissionRequiredSourceKeys")] public List<string> AdmissionRequiredSourceKeys { get; set; } = new();
    [JsonProperty("eventComparison")] public string EventComparison { get; set; } = "";
    [JsonProperty("eventInvestigationKingdoms")] public List<string> EventInvestigationKingdoms { get; set; } = new();
    [JsonProperty("eventTriggerMaterial")] public string EventTriggerMaterial { get; set; } = "";
    [JsonProperty("privacyDeclarationSystem")] public string PrivacyDeclarationSystem { get; set; } = "";
    [JsonIgnore] internal bool PrivacyApproved;
    // Optional binding for oral writing in the existing generation queue.
    [JsonProperty("dialogueArrangementId")]
    public string DialogueArrangementId { get; set; } = "";
    [JsonProperty("dialogueArrangementVersion")]
    public int DialogueArrangementVersion { get; set; }
    // Zero identifies drafts from before fixed oral execution data survived isolation.
    [JsonProperty("oralDraftingContractVersion")]
    public int OralDraftingContractVersion { get; set; }
	// Request-only reading window. Rebuilt on load/send; never persisted as another archive.
	[JsonIgnore] internal string DeclarationHistoryBlock;
	// Runtime-only invalidation for the bounded queue view. The persisted property
	// names and values below remain unchanged.
	[JsonIgnore] internal Action SelectionChanged;
	private bool _awaitingHistoryCompression;
	private int _inputBudgetHistoryTargetTokens;
	private string _jobId = "";
	private string _kind = "";
	private int _priority;
	private int _createdDay;
	private bool _isRunning;
	private string _cacheAffinityKey = "";
	[JsonProperty("awaitingHistoryCompression")]
	public bool AwaitingHistoryCompression { get => _awaitingHistoryCompression; set { if (_awaitingHistoryCompression == value) return; _awaitingHistoryCompression = value; SelectionChanged?.Invoke(); } }

	[JsonProperty("inputBudgetHistoryTargetTokens")]
	public int InputBudgetHistoryTargetTokens { get => _inputBudgetHistoryTargetTokens; set { if (_inputBudgetHistoryTargetTokens == value) return; _inputBudgetHistoryTargetTokens = value; SelectionChanged?.Invoke(); } }

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
	public string JobId { get => _jobId; set { if (string.Equals(_jobId, value, StringComparison.Ordinal)) return; _jobId = value; SelectionChanged?.Invoke(); } }

	[JsonProperty("kind")]
	public string Kind { get => _kind; set { if (string.Equals(_kind, value, StringComparison.Ordinal)) return; _kind = value; SelectionChanged?.Invoke(); } }

	[JsonProperty("priority")]
	public int Priority { get => _priority; set { if (_priority == value) return; _priority = value; SelectionChanged?.Invoke(); } }

	[JsonProperty("createdDay")]
	public int CreatedDay { get => _createdDay; set { if (_createdDay == value) return; _createdDay = value; SelectionChanged?.Invoke(); } }

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
	public bool IsRunning { get => _isRunning; set { if (_isRunning == value) return; _isRunning = value; SelectionChanged?.Invoke(); } }

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
	public string CacheAffinityKey { get => _cacheAffinityKey; set { if (string.Equals(_cacheAffinityKey, value, StringComparison.Ordinal)) return; _cacheAffinityKey = value; SelectionChanged?.Invoke(); } }

	[JsonProperty("maxTokens")]
	public int MaxTokens { get; set; }

	[JsonProperty("compressionYear")]
	public int CompressionYear { get; set; }

	[JsonProperty("compressionDocumentIds")]
	public List<string> CompressionDocumentIds { get; set; } = new List<string>();

    [JsonProperty("compressionFragmentId")] public string CompressionFragmentId { get; set; } = "";
    [JsonProperty("compressionFragmentHash")] public string CompressionFragmentHash { get; set; } = "";
    [JsonProperty("compressionFragmentOffset")] public int CompressionFragmentOffset { get; set; }
    [JsonProperty("compressionFragmentLength")] public int CompressionFragmentLength { get; set; }
    [JsonProperty("compressionFragmentDraftHash")] public string CompressionFragmentDraftHash { get; set; } = "";
    [JsonProperty("compressionFragmentAudit")] public bool CompressionFragmentAudit { get; set; }
    [JsonProperty("compressionConfigurationFingerprint")] public string CompressionConfigurationFingerprint { get; set; } = "";
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

    [JsonProperty("requestLane")] public string RequestLane { get; set; } = "normal";
    [JsonProperty("playerResponseSourceIds")] public List<string> PlayerResponseSourceIds { get; set; } = new List<string>();
    [JsonProperty("roundConversationRevision")] public int RoundConversationRevision { get; set; }
    [JsonProperty("requestAttempt")] public int RequestAttempt { get; set; }
    [JsonProperty("transportRetryAttempts")] public int TransportRetryAttempts { get; set; }
    [JsonProperty("retryAfterUtcTicks")] public long RetryAfterUtcTicks { get; set; }
    [JsonIgnore] internal bool RequestPreparationPending { get; set; }
    [JsonProperty("personalMemoryRulerId")]
	public string PersonalMemoryRulerId { get; set; } = "";
}
