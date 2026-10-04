using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AnimusForge;

public sealed class WorldDiplomacyDocumentAction
{
	[JsonProperty("actionId")] public string ActionId { get; set; } = "";
	[JsonProperty("targetKingdomId")] public string TargetKingdomId { get; set; } = "";
	[JsonProperty("targetKingdomName")] public string TargetKingdomName { get; set; } = "";
	[JsonProperty("intent")] public string Intent { get; set; } = "";
	[JsonProperty("negotiationMove")] public string NegotiationMove { get; set; } = "";
	[JsonProperty("commitment")] public string Commitment { get; set; } = "";
	[JsonProperty("requiresResponse")] public bool RequiresResponse { get; set; }
	[JsonProperty("respondingToOfferDocumentId")] public string RespondingToOfferDocumentId { get; set; } = "";
	[JsonProperty("respondingToOfferActionId")] public string RespondingToOfferActionId { get; set; } = "";
	[JsonProperty("respondingToThreatDocumentId")] public string RespondingToThreatDocumentId { get; set; } = "";
	[JsonProperty("respondingToThreatActionId")] public string RespondingToThreatActionId { get; set; } = "";
	[JsonProperty("peaceTerms")] public WorldDiplomacyPeaceTerms PeaceTerms { get; set; }
	[JsonProperty("mechanicalResult")] public string MechanicalResult { get; set; } = "";
	[JsonProperty("changedDiplomaticState")] public bool ChangedDiplomaticState { get; set; }
	[JsonProperty("historyResultRecorded")] public bool HistoryResultRecorded { get; set; }

    [JsonProperty("treatyTerms")] public WorldDiplomacyDialogueTerms TreatyTerms { get; set; }
}

public sealed class WorldDiplomacyDocument
{
	[JsonIgnore] internal Action NotificationSelectionChanged;
	private bool _hasReachedPlayerCourt;
	private string _documentId = "";
	private int _day;
	private long _createdUtcTicks;
	private bool _isPlayerAuthored;
	private bool _isRead;
	private bool _rumorNotified;
	private bool _formalNoticeShown;
	private bool _isReadyForPublication;
	[JsonProperty("actions", NullValueHandling = NullValueHandling.Ignore)]
	public List<WorldDiplomacyDocumentAction> Actions { get; set; }

	[JsonIgnore]
	public string ProcessingActionId { get; set; } = "";

	[JsonProperty("historyDeclarationRecorded")]
	public bool HistoryDeclarationRecorded { get; set; }

	[JsonProperty("historyResultRecorded")]
	public bool HistoryResultRecorded { get; set; }

	[JsonProperty("roundId")]
	public string RoundId { get; set; } = "";

	[JsonProperty("originSettlementId")]
	public string OriginSettlementId { get; set; } = "";

	[JsonProperty("addressedKingdomIds")]
	public List<string> AddressedKingdomIds { get; set; } = new List<string>();

	[JsonProperty("mentionedKingdomIds")]
	public List<string> MentionedKingdomIds { get; set; } = new List<string>();

	[JsonProperty("propagationStarted")]
	public bool PropagationStarted { get; set; }

	[JsonProperty("propagationCompleted")]
	public bool PropagationCompleted { get; set; }

	[JsonProperty("hasReachedPlayerCourt")]
	public bool HasReachedPlayerCourt { get => _hasReachedPlayerCourt; set { if (_hasReachedPlayerCourt == value) return; _hasReachedPlayerCourt = value; NotificationSelectionChanged?.Invoke(); } }

	[JsonProperty("documentId")]
	public string DocumentId { get => _documentId; set { if (string.Equals(_documentId, value, StringComparison.Ordinal)) return; _documentId = value; NotificationSelectionChanged?.Invoke(); } }

	[JsonProperty("exchangeId")]
	public string ExchangeId { get; set; } = "";

	[JsonProperty("sourceDocumentId")]
	public string SourceDocumentId { get; set; } = "";

	[JsonProperty("respondingToOfferDocumentId")]
	public string RespondingToOfferDocumentId { get; set; } = "";

	[JsonProperty("respondingToOfferActionId")]
	public string RespondingToOfferActionId { get; set; } = "";

	[JsonProperty("respondingToThreatDocumentId")]
	public string RespondingToThreatDocumentId { get; set; } = "";

	[JsonProperty("respondingToThreatActionId")]
	public string RespondingToThreatActionId { get; set; } = "";

	[JsonProperty("presentedThreatDocumentIds")]
	public List<string> PresentedThreatDocumentIds { get; set; } = new List<string>();

	[JsonProperty("presentedThreatFollowThroughDocumentIds")]
	public List<string> PresentedThreatFollowThroughDocumentIds { get; set; } = new List<string>();

	[JsonProperty("authorKingdomId")]
	public string AuthorKingdomId { get; set; } = "";

	[JsonProperty("authorKingdomName")]
	public string AuthorKingdomName { get; set; } = "";

	[JsonProperty("authorRulerId")]
	public string AuthorRulerId { get; set; } = "";

	[JsonProperty("authorRulerName")]
	public string AuthorRulerName { get; set; } = "";

	[JsonProperty("targetKingdomId")]
	public string TargetKingdomId { get; set; } = "";

	[JsonProperty("targetKingdomName")]
	public string TargetKingdomName { get; set; } = "";

	[JsonProperty("title")]
	public string Title { get; set; } = "";

	[JsonProperty("body")]
	public string Body { get; set; } = "";

	[JsonProperty("origin")]
	public string Origin { get; set; } = "";

	[JsonProperty("intent")]
	public string Intent { get; set; } = "";

	[JsonProperty("negotiationMove")]
	public string NegotiationMove { get; set; } = "";

	[JsonProperty("commitment")]
	public string Commitment { get; set; } = "";

	[JsonProperty("tone")]
	public string Tone { get; set; } = "";

	[JsonProperty("confidence")]
	public float Confidence { get; set; }

	[JsonProperty("analysisStatus")]
	public string AnalysisStatus { get; set; } = "";

	[JsonProperty("hiddenIntent")]
	public string HiddenIntent { get; set; } = "";

	[JsonProperty("hiddenCommitment")]
	public string HiddenCommitment { get; set; } = "";

	[JsonProperty("mechanicalResult")]
	public string MechanicalResult { get; set; } = "";

	[JsonProperty("changedDiplomaticState")]
	public bool ChangedDiplomaticState { get; set; }

	[JsonProperty("peaceTerms")]
	public WorldDiplomacyPeaceTerms PeaceTerms { get; set; }

	[JsonProperty("day")]
	public int Day { get => _day; set { if (_day == value) return; _day = value; NotificationSelectionChanged?.Invoke(); } }

	[JsonProperty("gameDate")]
	public string GameDate { get; set; } = "";

	[JsonProperty("createdUtcTicks")]
	public long CreatedUtcTicks { get => _createdUtcTicks; set { if (_createdUtcTicks == value) return; _createdUtcTicks = value; NotificationSelectionChanged?.Invoke(); } }

	[JsonProperty("isPlayerAuthored")]
	public bool IsPlayerAuthored { get => _isPlayerAuthored; set { if (_isPlayerAuthored == value) return; _isPlayerAuthored = value; NotificationSelectionChanged?.Invoke(); } }

	[JsonProperty("isResponse")]
	public bool IsResponse { get; set; }

	[JsonProperty("requiresResponse")]
	public bool RequiresResponse { get; set; }

	[JsonProperty("isExternalResponseOnly")]
	public bool IsExternalResponseOnly { get; set; }

	[JsonProperty("isReminder")]
	public bool IsReminder { get; set; }

	[JsonProperty("isRelayTurn")]
	public bool IsRelayTurn { get; set; }

	[JsonProperty("automaticReplyDepth")]
	public int AutomaticReplyDepth { get; set; }

	[JsonProperty("isRead")]
	public bool IsRead { get => _isRead; set { if (_isRead == value) return; _isRead = value; NotificationSelectionChanged?.Invoke(); } }

	[JsonProperty("isNotified")]
	public bool IsNotified { get; set; }

	[JsonProperty("rumorNotified")]
	public bool RumorNotified { get => _rumorNotified; set { if (_rumorNotified == value) return; _rumorNotified = value; NotificationSelectionChanged?.Invoke(); } }

	[JsonProperty("formalNoticeShown")]
	public bool FormalNoticeShown { get => _formalNoticeShown; set { if (_formalNoticeShown == value) return; _formalNoticeShown = value; NotificationSelectionChanged?.Invoke(); } }

	[JsonProperty("isCompressed")]
	public bool IsCompressed { get; set; }

	[JsonProperty("isReadyForPublication")]
	public bool IsReadyForPublication { get => _isReadyForPublication; set { if (_isReadyForPublication == value) return; _isReadyForPublication = value; NotificationSelectionChanged?.Invoke(); } }

	[JsonProperty("roundParticipation")]
	public string RoundParticipation { get; set; } = "continue";

	[JsonProperty("roundStatus")]
	public string RoundStatus { get; set; } = "continue";

	[JsonProperty("madeDiplomaticProgress")]
	public bool MadeDiplomaticProgress { get; set; }

	[JsonProperty("internationalReputationEvaluationDelta")]
	public int InternationalReputationEvaluationDelta { get; set; }

	[JsonProperty("internationalReputationEvaluationReason")]
	public string InternationalReputationEvaluationReason { get; set; } = "";

	[JsonProperty("internationalReputationEvaluationSource")]
	public string InternationalReputationEvaluationSource { get; set; } = "";

	[JsonProperty("internationalReputationSettled")]
	public bool InternationalReputationSettled { get; set; }

	[JsonProperty("diplomaticStandingChanges")]
	public List<WorldDiplomacyStandingChange> DiplomaticStandingChanges { get; set; } = new List<WorldDiplomacyStandingChange>();

	[JsonProperty("roundProgressHandled")]
	public bool RoundProgressHandled { get; set; }

	[JsonProperty("roundAccountingHandled")]
	public bool RoundAccountingHandled { get; set; }

	[JsonProperty("hasEmbeddedRoundPlan")]
	public bool HasEmbeddedRoundPlan { get; set; }

	[JsonProperty("plannedRoundTopic")]
	public string PlannedRoundTopic { get; set; } = "";

	[JsonProperty("plannedKingdomIds")]
	public List<string> PlannedKingdomIds { get; set; } = new List<string>();

	[JsonProperty("resultSettlementSlotId")]
	public string ResultSettlementSlotId { get; set; } = "";

	[JsonProperty("isAutonomousNoActionDeclaration")]
	public bool IsAutonomousNoActionDeclaration { get; set; }

	[JsonProperty("isRoundResponseNoActionDeclaration")]
	public bool IsRoundResponseNoActionDeclaration { get; set; }

	[JsonProperty("isWarResponseNoActionDeclaration")]
	public bool IsWarResponseNoActionDeclaration { get; set; }

    [JsonProperty("discussionRoundId")] public string DiscussionRoundId { get; set; } = "";
    [JsonProperty("discussionSourceDocumentId")] public string DiscussionSourceDocumentId { get; set; } = "";
    [JsonProperty("answeredPlayerDocumentIds")] public List<string> AnsweredPlayerDocumentIds { get; set; } = new List<string>();
    public string DialogueArrangementId { get; set; } = "";
    public int DialogueArrangementVersion { get; set; }
    [JsonProperty("treatyTerms")] public WorldDiplomacyDialogueTerms TreatyTerms { get; set; }
    [JsonProperty("personalMemoryReceipts")]
	public List<string> PersonalMemoryReceipts { get; set; } = new List<string>();
    [JsonProperty("pendingPersonalMemoryRulers")]
	public Dictionary<string, int> PendingPersonalMemoryRulers { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
}
