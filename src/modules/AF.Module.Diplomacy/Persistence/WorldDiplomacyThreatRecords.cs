using System.Collections.Generic;
using Newtonsoft.Json;

namespace AnimusForge;

public sealed class WorldDiplomacyThreatNonComplianceEvent
{
	[JsonProperty("stage")] public string Stage { get; set; } = "warning";
	[JsonProperty("stageDocumentId")] public string StageDocumentId { get; set; } = "";
	[JsonProperty("stageActionId")] public string StageActionId { get; set; } = "";
	[JsonProperty("decisionDocumentId")] public string DecisionDocumentId { get; set; } = "";
	[JsonProperty("decisionActionId")] public string DecisionActionId { get; set; } = "";
	[JsonProperty("decisionRoundId")] public string DecisionRoundId { get; set; } = "";
	[JsonProperty("decisionDay")] public int DecisionDay { get; set; }
	[JsonProperty("historyRecorded")] public bool HistoryRecorded { get; set; }
}

public sealed class WorldDiplomacyThreat
{
	[JsonProperty("threatId")] public string ThreatId { get; set; } = "";
	[JsonProperty("issuerKingdomId")] public string IssuerKingdomId { get; set; } = "";
	[JsonProperty("targetKingdomId")] public string TargetKingdomId { get; set; } = "";
	[JsonProperty("stage")] public string Stage { get; set; } = "warning";
	[JsonProperty("status")] public string Status { get; set; } = "open";
	[JsonProperty("warningDocumentId")] public string WarningDocumentId { get; set; } = "";
	[JsonProperty("warningActionId")] public string WarningActionId { get; set; } = "";
	[JsonProperty("ultimatumDocumentId")] public string UltimatumDocumentId { get; set; } = "";
	[JsonProperty("ultimatumActionId")] public string UltimatumActionId { get; set; } = "";
	[JsonProperty("stageDocumentId")] public string StageDocumentId { get; set; } = "";
	[JsonProperty("stageActionId")] public string StageActionId { get; set; } = "";
	[JsonProperty("stageRoundId")] public string StageRoundId { get; set; } = "";
	[JsonProperty("createdDay")] public int CreatedDay { get; set; }
	[JsonProperty("stageIssuedDay")] public int StageIssuedDay { get; set; }
	[JsonProperty("updatedDay")] public int UpdatedDay { get; set; }
	[JsonProperty("targetDecision")] public string TargetDecision { get; set; } = "pending";
	[JsonProperty("targetDecisionDocumentId")] public string TargetDecisionDocumentId { get; set; } = "";
	[JsonProperty("targetDecisionActionId")] public string TargetDecisionActionId { get; set; } = "";
	[JsonProperty("targetDecisionRoundId")] public string TargetDecisionRoundId { get; set; } = "";
	[JsonProperty("targetDecisionDay")] public int TargetDecisionDay { get; set; }
	[JsonProperty("nonComplianceHistoryRecorded")] public bool NonComplianceHistoryRecorded { get; set; }
	[JsonProperty("nonComplianceEvents")] public List<WorldDiplomacyThreatNonComplianceEvent> NonComplianceEvents { get; set; } = new List<WorldDiplomacyThreatNonComplianceEvent>();
	[JsonProperty("obligationRoundId")] public string ObligationRoundId { get; set; } = "";
	[JsonProperty("obligationClaimedDay")] public int ObligationClaimedDay { get; set; }
	[JsonProperty("complianceDocumentId")] public string ComplianceDocumentId { get; set; } = "";
	[JsonProperty("complianceActionId")] public string ComplianceActionId { get; set; } = "";
	[JsonProperty("resolutionRoundId")] public string ResolutionRoundId { get; set; } = "";
	[JsonProperty("resolutionDocumentId")] public string ResolutionDocumentId { get; set; } = "";
	[JsonProperty("resolutionActionId")] public string ResolutionActionId { get; set; } = "";
	[JsonProperty("resolutionReason")] public string ResolutionReason { get; set; } = "";
	[JsonProperty("reputationPenaltyApplied")] public bool ReputationPenaltyApplied { get; set; }
	[JsonProperty("reputationPenaltyAmount")] public int ReputationPenaltyAmount { get; set; }
	[JsonProperty("issuerResolutionNoticePending")] public bool IssuerResolutionNoticePending { get; set; }
	[JsonProperty("historyResultRecorded")] public bool HistoryResultRecorded { get; set; }
	[JsonProperty("domesticPenaltyRulingClanId")] public string DomesticPenaltyRulingClanId { get; set; } = "";
	[JsonProperty("domesticPenaltyEligibleClanIds")] public List<string> DomesticPenaltyEligibleClanIds { get; set; } = new List<string>();
	[JsonProperty("domesticPenaltyAppliedClanIds")] public List<string> DomesticPenaltyAppliedClanIds { get; set; } = new List<string>();
	[JsonProperty("domesticPenaltySkippedClanIds")] public List<string> DomesticPenaltySkippedClanIds { get; set; } = new List<string>();
	[JsonProperty("domesticPenaltySnapshotCaptured")] public bool DomesticPenaltySnapshotCaptured { get; set; }
	[JsonProperty("domesticPenaltyCompleted")] public bool DomesticPenaltyCompleted { get; set; }
	[JsonProperty("domesticPenaltyHistoryRecorded")] public bool DomesticPenaltyHistoryRecorded { get; set; }
	[JsonProperty("policyConditionSignalKey")] public string PolicyConditionSignalKey { get; set; } = "";
	[JsonProperty("policyConditionPolicyId")] public string PolicyConditionPolicyId { get; set; } = "";
	[JsonProperty("policyConditionPolicyName")] public string PolicyConditionPolicyName { get; set; } = "";
	[JsonProperty("policyConditionOwnerKingdomId")] public string PolicyConditionOwnerKingdomId { get; set; } = "";
	[JsonProperty("policyConditionAffectedKingdomId")] public string PolicyConditionAffectedKingdomId { get; set; } = "";
	[JsonProperty("policyConditionBoundDay")] public int PolicyConditionBoundDay { get; set; }
	[JsonProperty("policyConditionCancellationCompleted")] public bool PolicyConditionCancellationCompleted { get; set; }
	[JsonProperty("policyConditionCancellationStatus")] public string PolicyConditionCancellationStatus { get; set; } = "";
	[JsonProperty("policyConditionCancellationDay")] public int PolicyConditionCancellationDay { get; set; }
	[JsonProperty("issuerRewardRulingClanId")] public string IssuerRewardRulingClanId { get; set; } = "";
	[JsonProperty("issuerRewardEligibleClanIds")] public List<string> IssuerRewardEligibleClanIds { get; set; } = new List<string>();
	[JsonProperty("issuerRewardAppliedClanIds")] public List<string> IssuerRewardAppliedClanIds { get; set; } = new List<string>();
	[JsonProperty("issuerRewardSkippedClanIds")] public List<string> IssuerRewardSkippedClanIds { get; set; } = new List<string>();
	[JsonProperty("issuerRewardSnapshotCaptured")] public bool IssuerRewardSnapshotCaptured { get; set; }
	[JsonProperty("issuerRewardCompleted")] public bool IssuerRewardCompleted { get; set; }
	[JsonProperty("issuerRewardAmount")] public int IssuerRewardAmount { get; set; }
	[JsonProperty("issuerRewardHistoryRecorded")] public bool IssuerRewardHistoryRecorded { get; set; }
}
