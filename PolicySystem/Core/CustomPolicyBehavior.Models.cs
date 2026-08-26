using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.PolicyEffects;
using AnimusForge.PolicyTargets;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions.ItemTypes;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Policies;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public sealed partial class CustomPolicyBehavior
{
	private sealed class ActivePolicyEffectRuntimeEntry
	{
		public string Raw;

		public ActivePolicyEffectSaveData Effect;
	}

	private sealed class PolicyEligibility
	{
		public bool CanPublish;

		public string Reason;

		public static PolicyEligibility Allowed()
		{
			return new PolicyEligibility { CanPublish = true, Reason = "" };
		}

		public static PolicyEligibility Blocked(string reason)
		{
			return new PolicyEligibility { CanPublish = false, Reason = reason ?? "" };
		}
	}

	private sealed class PolicyRuntimeOptions
	{
		public int GoldCost;

		public bool UseAiEvaluatedCost;

		public string EvaluatorPrompt;

		public bool EvaluatorPromptIsDefault;

		public int PublicFeedbackTargetChars;
	}

	private sealed class PolicyGenerationSettingsSnapshot
	{
		[JsonIgnore]
		public PolicyApiExecutionProfile ApiProfile;

		public long RuntimeGeneration;

		public string ScopeKind = PolicyScopeKingdom;

		public string IssuerKingdomId = string.Empty;

		public string ProposerClanId = string.Empty;

		public string TargetKingdomId = string.Empty;

		public string PolicyName = string.Empty;

		public string PolicyContent = string.Empty;

		public string DateText = string.Empty;

		public string KnowledgeContext = string.Empty;

		[JsonIgnore]
		public PolicyPromptContextBundle PromptContext;

		public List<string> SelectedFiefIds = new List<string>();

		public List<string> MentionedClanIds = new List<string>();

		public List<string> MentionedSettlementIds = new List<string>();

		public bool FollowCurrentRulingClan;

		public string EvaluatorPrompt = string.Empty;

		public bool EvaluatorPromptIsDefault;

		public bool UseAiEvaluatedCost;

		public int PublicFeedbackTargetChars;

		public int ManualDurationDays;

		public int ConfiguredDetailCount;

		public int EffectiveDetailCount;

		public int EffectPostprocessMaxTokens;

		public PolicyEffectRetrievalContext RetrievalContext;

		public List<string> EnabledModuleIds = new List<string>();

		[JsonIgnore]
		public PolicyTargetWorldSnapshot TargetWorldSnapshot;

		[JsonIgnore]
		public List<NpcPolicyHistoryEntry> HistoryEntries = new List<NpcPolicyHistoryEntry>();

		[JsonIgnore]
		public List<PolicyEnemyKingdomSnapshot> EnemyKingdoms = new List<PolicyEnemyKingdomSnapshot>();

		[JsonIgnore]
		public List<PolicyTargetHandleSaveData> InitialTargetHandles = new List<PolicyTargetHandleSaveData>();
	}

	internal sealed class DynamicPolicySaveData
	{
		public int Version { get; set; } = 4;

		public string PolicyObjectId { get; set; }

		public string RecordId { get; set; }

		public string ActiveEffectId { get; set; }

		public bool RequiresEffectBundle { get; set; }

		public string CommitState { get; set; } = "pending";

		public string Source { get; set; }

		public string OwnerKingdomId { get; set; }

		public string ProposerClanId { get; set; }

		public string IssuerKingdomId { get; set; }

		public string PolicyName { get; set; }

		public string PolicyContent { get; set; }

		public string LogEntryDescription { get; set; }

		public string SecondaryEffects { get; set; }

		public float AuthoritarianWeight { get; set; }

		public float OligarchicWeight { get; set; }

		public float EgalitarianWeight { get; set; }

		public string Status { get; set; }

		public bool NaturalExpiryAgendaRejected { get; set; }

		public long CreatedUtcTicks { get; set; }

		public string PlayerPayloadJson { get; set; }

		public bool PlayerStewardXpAwarded { get; set; }

		public string ReReviewRootRecordId { get; set; }

		public string ReReviewSourceRecordId { get; set; }

		public string SupersedesRecordId { get; set; }

		public bool ReReviewReplacementCommitted { get; set; }
	}

	private sealed class PendingPlayerPolicyAgendaSaveData
	{
		public int Version { get; set; } = 5;

		public PolicyDraftRequest Request { get; set; }

		public PolicyMainAssessmentResult Assessment { get; set; }

		[JsonProperty("moduleEffects", NullValueHandling = NullValueHandling.Ignore)]
		public List<PolicyEffectInstanceSaveData> ModuleEffects { get; set; }

		[JsonProperty("candidateModuleIds", NullValueHandling = NullValueHandling.Ignore)]
		public List<string> CandidateModuleIds { get; set; }

		[JsonProperty("detailedModuleIds", NullValueHandling = NullValueHandling.Ignore)]
		public List<string> DetailedModuleIds { get; set; }

		[JsonProperty("objectSnapshot", NullValueHandling = NullValueHandling.Ignore)]
		public JObject ObjectSnapshot { get; set; }

		public string Feedback { get; set; }
	}

	private sealed class PlayerPolicyTargetAuthorization
	{
		public string CacheKey { get; set; } = string.Empty;

		public HashSet<string> KingdomIds { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		public HashSet<string> ExplicitCrossKingdomIds { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		public HashSet<string> EntityKeys { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		public HashSet<string> PlanSignatures { get; } = new HashSet<string>(StringComparer.Ordinal);

		public HashSet<string> AllowedEntityReferenceIds { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		public HashSet<string> AllowedKingdomReferenceIds { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		public PolicyTargetPlanRouteResult PlanRoute { get; set; }
			= new PolicyTargetPlanRouteResult();
	}

	private sealed class PolicyDraftRequest
	{
		[JsonIgnore]
		public PolicyGenerationSettingsSnapshot GenerationSettings;

		public string RequestId;

		public string ReReviewRootRecordId { get; set; }

		public string ReReviewSourceRecordId { get; set; }

		public string SupersedesRecordId { get; set; }

		public bool ReReviewReplacementCommitted { get; set; }

		public string ScopeKind = PolicyScopeKingdom;

		public string IssuerKingdomId = "";

		public string IssuerKingdomName = "";

		public string ProposerClanId = "";

		public int VassalIndependenceBefore;

		public int VassalPublicationIndependenceCost;

		public int VassalQualityIndependenceDelta;

		public int VassalIndependenceAfter;

		public string VassalIndependenceReason = "";

		public List<string> SelectedFiefIds = new List<string>();

		public List<string> LocalMentionedClanIds = new List<string>();

		public List<string> LocalMentionedSettlementIds = new List<string>();

		public bool LocalMentionedCurrentRulingClan;

		public string LocalMentionSummary = "";

		public List<PolicyTargetHandleSaveData> TargetHandles = new List<PolicyTargetHandleSaveData>();

		[JsonIgnore]
		public PolicyEffectMechanismHint EffectMechanismHint;

		[JsonIgnore]
		public PolicyEffectSemanticPlan SemanticEffectPlan;

		[JsonIgnore]
		public string EffectPromptHash;

		[JsonIgnore]
		public int EffectPromptChars;

		[JsonIgnore]
		public PolicyTargetHandleDirectory EffectTargetDirectory;

		[JsonIgnore]
		public PlayerPolicyTargetAuthorization TargetAuthorization;

		public int ManualDurationDays;

		public bool IsPermanentEffect;

		public string PolicyName;

		public string PolicyContent;

		public string DateText;

		public int SubmittedDay;

		public string PlayerKingdomId;

		public string PlayerKingdomName;

		public bool UseAiEvaluatedCost;

		public int RequiredGoldCost;

		public int DailyMaintenanceGoldCost;

		public int TotalMaintenancePaidGold = 0;

		public bool MaintenanceFunded = true;

		public int LastMaintenanceSettlementDay = -1;

		public int LastEffectProcessedDay = -1;

		public float RequiredInfluenceCost;

		public float GoldEffectScale = 1f;

		public float InfluenceEffectScale = 1f;

		public int GoldCost;

		public float InfluenceCost;

		public string EvaluatorPrompt;

		public bool EvaluatorPromptIsDefault;

		public int PublicFeedbackTargetChars;

		public PolicyPromptContextBundle PromptContext;

		public MentionedWorldEntities KnowledgeMentionedEntities;

		public string KnowledgeContext;

		[JsonIgnore]
		public List<string> CandidateEffectModuleIds = new List<string>();

		[JsonIgnore]
		public List<string> SelectedEffectModuleIds = new List<string>();

		[JsonIgnore]
		public PolicyTargetWorldSnapshot SemanticTargetSnapshot;

		[JsonIgnore]
		public List<NpcPolicyHistoryEntry> PolicyHistoryEntries = new List<NpcPolicyHistoryEntry>();

		[JsonIgnore]
		public List<PolicyEnemyKingdomSnapshot> EnemyKingdoms = new List<PolicyEnemyKingdomSnapshot>();

		[JsonIgnore]
		public PolicyHistoryRetrievalResult PolicyHistoryRetrieval;
	}

	private sealed class PolicyReReviewContext
	{
		public string ScopeKind { get; set; } = PolicyScopeKingdom;

		public string SourceRecordId { get; set; } = string.Empty;

		public string RootRecordId { get; set; } = string.Empty;

		public string SupersedesRecordId { get; set; } = string.Empty;

		public string PolicyName { get; set; } = string.Empty;

		public string PolicyContent { get; set; } = string.Empty;

		public bool DurationKnown { get; set; }

		public bool IsPermanentEffect { get; set; }

		public int DurationDays { get; set; }

		public string SelectedTargetId { get; set; } = string.Empty;

		public List<string> SelectedFiefIds { get; set; } = new List<string>();
	}

	private sealed class PolicyPromptContextBundle
	{
		public string PolicyRuleContext = "";

		public string WorldContextCompact = "";

		public string WorldContextFull = "";

		public string ExtensionContext = "";
	}

	private sealed class PolicyGenerationResult
	{
		public string MainRaw;

		public PolicyMainAssessmentResult MainAssessment;

		public string KnowledgeContext;

		public string PostprocessRaw;

		public PolicyPostprocessResult Postprocess;

		public string FailureStage;

		public string Error;
	}

	private sealed class PolicyMainAssessmentResult
	{
		[JsonProperty("publicFeedback")]
		public string PublicFeedback { get; set; }

		[JsonProperty("impactSummary")]
		public string ImpactSummary { get; set; }

		[JsonProperty("requiredGoldCost")]
		public float? RequiredGoldCost { get; set; }

		[JsonProperty("startupGoldCost")]
		public float? StartupGoldCost { get; set; }

		[JsonProperty("dailyMaintenanceGoldCost")]
		public float? DailyMaintenanceGoldCost { get; set; }

		[JsonProperty("requiredInfluenceCost")]
		public float? RequiredInfluenceCost { get; set; }

		[JsonProperty("effectIntensity")]
		public string EffectIntensity { get; set; }

		[JsonProperty("executionReach")]
		public string ExecutionReach { get; set; }

		[JsonProperty("durationLogic")]
		public string DurationLogic { get; set; }

		[JsonProperty("numericIntent")]
		public string NumericIntent { get; set; }

		[JsonProperty("effectIntentVersion")]
		public int? EffectIntentVersion { get; set; }

		[JsonProperty("effectIntents")]
		public JArray EffectIntents { get; set; }

		[JsonProperty("confirmedTargetHandles", NullValueHandling = NullValueHandling.Ignore)]
		public List<string> ConfirmedTargetHandles { get; set; }

		[JsonProperty("policyContentDigest")]
		public string PolicyContentDigest { get; set; }

		[JsonProperty("feedbackDigest")]
		public string FeedbackDigest { get; set; }

		[JsonProperty("vassalIndependenceDelta")]
		public float? VassalIndependenceDelta { get; set; }

		[JsonProperty("vassalIndependenceReason")]
		public string VassalIndependenceReason { get; set; }

		[JsonProperty("authoritarianWeight")]
		public float? AuthoritarianWeight { get; set; }

		[JsonProperty("oligarchicWeight")]
		public float? OligarchicWeight { get; set; }

		[JsonProperty("egalitarianWeight")]
		public float? EgalitarianWeight { get; set; }

		[JsonProperty("durationDays")]
		public int? DurationDays { get; set; }

		[JsonProperty("effectDurationMode")]
		public string EffectDurationMode { get; set; }

		[JsonProperty("effects", NullValueHandling = NullValueHandling.Ignore)]
		public List<PolicyEffectDto> Effects { get; set; }

		[JsonIgnore]
		public bool UsesSparseEffectIr { get; set; }

		[JsonIgnore]
		public string EffectIrValidationError { get; set; }

		[JsonIgnore]
		public string EffectDisposition { get; set; }

		[JsonIgnore]
		public string EffectDispositionReason { get; set; }
	}

	private sealed class PolicyPostprocessResult
	{
		[JsonProperty("effectPlanVersion")]
		public int? EffectPlanVersion { get; set; }

		[JsonProperty("impactSummary")]
		public string ImpactSummary { get; set; }

		[JsonProperty("disposition")]
		public string Disposition { get; set; }

		[JsonProperty("reason")]
		public string Reason { get; set; }

		[JsonProperty("durationDays")]
		public int? DurationDays { get; set; }

		[JsonProperty("effects")]
		public List<PolicyEffectDto> Effects { get; set; }
	}

	private sealed class PolicyEffectDto
	{
		[JsonIgnore]
		public int EffectPlanVersion { get; set; }

		[JsonProperty("mechanismId", NullValueHandling = NullValueHandling.Ignore)]
		public string MechanismId { get; set; }

		[JsonProperty("mechanismKind", NullValueHandling = NullValueHandling.Ignore)]
		public PolicyEffectMechanismKind MechanismKind { get; set; }

		[JsonProperty("role", NullValueHandling = NullValueHandling.Ignore)]
		public PolicyEffectMechanismRole MechanismRole { get; set; }

		[JsonProperty("sourceOmitted", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public bool SourceOmitted { get; set; }

		[JsonProperty("destinationOmitted", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public bool DestinationOmitted { get; set; }

		[JsonProperty("moduleId", NullValueHandling = NullValueHandling.Ignore)]
		public string ModuleId { get; set; }

		[JsonIgnore]
		public string SourceModuleId { get; set; }

		[JsonProperty("targetHandles", NullValueHandling = NullValueHandling.Ignore)]
		public List<string> TargetHandles { get; set; }

		[JsonProperty("payload", NullValueHandling = NullValueHandling.Ignore)]
		public JToken Payload { get; set; }

		[JsonIgnore]
		public PolicyEffectPreparedInstance PreparedModuleEffect { get; set; }

		[JsonProperty("targets", NullValueHandling = NullValueHandling.Ignore)]
		public List<string> Targets { get; set; }

		[JsonProperty("changes", NullValueHandling = NullValueHandling.Ignore)]
		public Dictionary<string, float> Changes { get; set; }

		[JsonProperty("targetHandle", NullValueHandling = NullValueHandling.Ignore)]
		public string TargetHandle { get; set; }

		[JsonProperty("targetScope")]
		public string TargetScope { get; set; }

		[JsonProperty("targetKingdomId")]
		public string TargetKingdomId { get; set; }

		[JsonProperty("targetKingdomName")]
		public string TargetKingdomName { get; set; }

		[JsonExtensionData]
		public IDictionary<string, JToken> LegacyFields { get; set; }

		[JsonProperty("durationDays")]
		public int DurationDays { get; set; }

		[JsonProperty("reason")]
		public string Reason { get; set; }
	}

	private sealed class PolicyApplicationResult
	{
		public int AppliedEffectCount;

		public List<AppliedKingdomEffect> KingdomEffects = new List<AppliedKingdomEffect>();

		public List<string> NoticeLines = new List<string>();
	}

	internal sealed class AppliedKingdomEffect
	{
		[JsonProperty("moduleEffects")]
		internal List<PolicyEffectInstanceSaveData> ModuleEffects = new List<PolicyEffectInstanceSaveData>();

		[JsonProperty("executionReceipts")]
		internal List<PolicyEffectExecutionReceipt> ExecutionReceipts = new List<PolicyEffectExecutionReceipt>();

		public string EffectId;

		public string ScopeKind = PolicyScopeKingdom;

		public string LocalTargetScope = "";

		public string TargetHandle = "";

		public string TargetLabel = "";

		public List<string> TargetFiefIds = new List<string>();

		public List<string> TargetSettlementIds = new List<string>();

		public List<string> TargetClanIds = new List<string>();

		public List<string> DirectTargetSettlementIds = new List<string>();

		public bool FollowCurrentRulingClan;

		public string KingdomId;

		public string KingdomName;

		public int TownCount;

		public int VillageCount;

		public int DurationDays;

		public int RemainingDays;

		public bool IsPermanentEffect;

		public string Reason;

		public List<string> DetailLines = new List<string>();
	}

	internal sealed class ActivePolicyEffectSaveData
	{
		public int Version { get; set; } = 8;

		public List<PolicyEffectInstanceSaveData> ModuleEffects { get; set; } = new List<PolicyEffectInstanceSaveData>();

		public List<PolicyEffectExecutionReceipt> ExecutionReceipts { get; set; } = new List<PolicyEffectExecutionReceipt>();

		public string ScopeKind { get; set; }

		public string LocalTargetScope { get; set; }

		public string TargetHandle { get; set; }

		public string TargetLabel { get; set; }

		public List<string> TargetFiefIds { get; set; } = new List<string>();

		private List<string> _targetSettlementIds = new List<string>();

		public List<string> TargetSettlementIds
		{
			get => _targetSettlementIds;
			set
			{
				_targetSettlementIds = value ?? new List<string>();
				TargetSettlementIdSet = null;
			}
		}

		[JsonIgnore]
		private HashSet<string> TargetSettlementIdSet { get; set; }

		public List<string> TargetClanIds { get; set; } = new List<string>();

		public List<string> DirectTargetSettlementIds { get; set; } = new List<string>();

		public bool FollowCurrentRulingClan { get; set; }

		public string EffectId { get; set; }

		public string RecordId { get; set; }

		public string ProposerClanId { get; set; }

		public string IssuerKingdomId { get; set; }

		public string PolicyName { get; set; }

		public string DateText { get; set; }

		public int SubmittedDay { get; set; }

		public long CreatedUtcTicks { get; set; }

		public string TargetKingdomId { get; set; }

		public string TargetKingdomName { get; set; }

		public int TotalDurationDays { get; set; }

		public int RemainingDays { get; set; }

		public bool IsPermanentEffect { get; set; }

		public int DailyMaintenanceGoldCost { get; set; }

		public int TotalMaintenancePaidGold { get; set; }

		public bool MaintenanceChargeEnabled { get; set; }

		public bool MaintenanceFunded { get; set; } = true;

		public int LastMaintenanceSettlementDay { get; set; } = -1;

		public int LastEffectProcessedDay { get; set; } = -1;

		public int LastAppliedDay { get; set; }

		public string Reason { get; set; }

		public bool Ended { get; set; }

		public string EndReason { get; set; }

		[JsonProperty("pendingApplication")]
		internal PendingActivePolicyApplicationSaveData PendingApplication { get; set; }

		public bool ContainsTargetSettlementId(string settlementId)
		{
			if (string.IsNullOrWhiteSpace(settlementId))
			{
				return false;
			}
			TargetSettlementIdSet ??= new HashSet<string>(TargetSettlementIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
			return TargetSettlementIdSet.Contains(settlementId);
		}
	}

	internal sealed class PendingActivePolicyApplicationSaveData
	{
		public int Day { get; set; }

		public List<string> SettlementIds { get; set; } = new List<string>();

		public int NextSettlementIndex { get; set; }

		public int NextNonSettlementIndex { get; set; }

		// Null means a legacy/new pending application has not frozen its non-settlement
		// daily work yet. Once initialized (including to an empty list), the keys remain
		// stable for this logical policy day so structural refreshes cannot replay a
		// committed prefix or add mid-day targets.
		public List<string> NonSettlementEntryKeys { get; set; }

		public List<string> SkippedNonSettlementEntryKeys { get; set; }

		[JsonProperty("appliedEffect")]
		internal AppliedKingdomEffect AppliedEffect { get; set; }
	}

	private sealed class PendingActivePolicyEffectWork
	{
		public string EffectId;

		public int RuntimeGeneration;
	}

	internal sealed class PolicyRecordSaveData
	{
		public int Version { get; set; } = 3;

		public string RecordId { get; set; }

		public int SubmittedDay { get; set; }

		public long CreatedUtcTicks { get; set; }

		public string DateText { get; set; }

		public string PolicyName { get; set; }

		public string PolicyContentSummary { get; set; }

		public string PublicFeedbackSummary { get; set; }

		public string ImpactSummary { get; set; }

		public string ImpactEffectsSummary { get; set; }

		public string PlayerKingdomId { get; set; }

		public string PlayerKingdomName { get; set; }

		public bool UseAiEvaluatedCost { get; set; }

		public int RequiredGoldCost { get; set; }

		public bool IsPermanentEffect { get; set; }

		public int DailyMaintenanceGoldCost { get; set; }

		public int TotalMaintenancePaidGold { get; set; }

		public bool MaintenanceFunded { get; set; } = true;

		public int LastMaintenanceSettlementDay { get; set; } = -1;

		public int LastEffectProcessedDay { get; set; } = -1;

		public float RequiredInfluenceCost { get; set; }

		public float GoldEffectScale { get; set; } = 1f;

		public float InfluenceEffectScale { get; set; } = 1f;

		public int GoldCost { get; set; }

		public float InfluenceCost { get; set; }

		public bool EvaluatorPromptIsDefault { get; set; }

		public List<PolicyRecordEffectSaveData> Effects { get; set; } = new List<PolicyRecordEffectSaveData>();
	}

	internal sealed class PolicyRecordEffectSaveData
	{
		public List<PolicyEffectInstanceSaveData> ModuleEffects { get; set; } = new List<PolicyEffectInstanceSaveData>();

		public List<PolicyEffectExecutionReceipt> ExecutionReceipts { get; set; } = new List<PolicyEffectExecutionReceipt>();

		public string KingdomId { get; set; }

		public string KingdomName { get; set; }

		public string TargetHandle { get; set; }

		public string TargetLabel { get; set; }

		public int TownCount { get; set; }

		public int VillageCount { get; set; }

		public string EffectId { get; set; }

		public int TotalDurationDays { get; set; }

		public int RemainingDays { get; set; }

		public bool IsPermanentEffect { get; set; }

		public int LastAppliedDay { get; set; }

		public bool IsEnded { get; set; }

		public string EndReason { get; set; }

		public string Reason { get; set; }
	}

	internal sealed class LocalPolicyRecordSaveData
	{
		public int Version { get; set; } = 6;

		public string ScopeKind { get; set; } = PolicyScopeLocal;

		public string RecordId { get; set; }

		public string ReReviewRootRecordId { get; set; }

		public string ReReviewSourceRecordId { get; set; }

		public string SupersedesRecordId { get; set; }

		public bool ReReviewReplacementCommitted { get; set; }

		public string ActiveEffectId { get; set; }

		public string ExternalTransactionId { get; set; }

		public string ExternalAgreementId { get; set; }

		public string ExternalIdempotencyKey { get; set; }

		public string ExternalCommitState { get; set; }

		public bool ExternalInputsCaptured { get; set; }

		public int ExternalPublicationCost { get; set; }

		public int ExternalQualityDelta { get; set; }

		public int ExternalIndependenceBefore { get; set; }

		public int ExternalIndependenceExpected { get; set; }

		public int ExternalIndependenceActual { get; set; }

		public bool ExternalBreakawayExpected { get; set; }

		public bool ExternalBreakawayActual { get; set; }

		public int ExternalCommitAttempts { get; set; }

		public int ExternalLastAttemptDay { get; set; } = -1;

		public string ExternalLastError { get; set; }

		public int SubmittedDay { get; set; }

		public long CreatedUtcTicks { get; set; }

		public string DateText { get; set; }

		public string PolicyName { get; set; }

		public string PolicyContent { get; set; }

		public string PublicFeedback { get; set; }

		public string ImpactSummary { get; set; }

		public string Status { get; set; } = LocalPolicyStatusActive;

		public string EffectStatus { get; set; } = LocalPolicyStatusActive;

		public string EndReason { get; set; }

		public string TargetKingdomId { get; set; }

		public string TargetKingdomName { get; set; }

		public string IssuerKingdomId { get; set; }

		public string IssuerKingdomName { get; set; }

		public int InitialIndependenceCost { get; set; }

		public int TotalIndependenceCost { get; set; }

		public int VassalQualityIndependenceDelta { get; set; }

		public int IndependenceBefore { get; set; }

		public int IndependenceAfter { get; set; }

		public string IndependenceReason { get; set; }

		public bool UseAiEvaluatedCost { get; set; }

		public int RequiredGoldCost { get; set; }

		public int InitialActualGoldCost { get; set; }

		public int TotalPaidGold { get; set; }

		public bool IsPermanentEffect { get; set; }

		public int DailyMaintenanceGoldCost { get; set; }

		public int TotalMaintenancePaidGold { get; set; }

		public bool MaintenanceFunded { get; set; } = true;

		public int LastMaintenanceSettlementDay { get; set; } = -1;

		public int LastEffectProcessedDay { get; set; } = -1;

		public float GoldEffectScale { get; set; } = 1f;

		public int OriginalDurationDays { get; set; }

		public int RemainingDays { get; set; }

		public int RenewalCount { get; set; }

		public List<string> OriginalTargetFiefIds { get; set; } = new List<string>();

		public List<string> TargetFiefIds { get; set; } = new List<string>();

		[JsonProperty("originalTargets")]
		internal List<LocalPolicyTargetSnapshotSaveData> OriginalTargets { get; set; } = new List<LocalPolicyTargetSnapshotSaveData>();

		[JsonProperty("renewals")]
		internal List<LocalPolicyRenewalSaveData> Renewals { get; set; } = new List<LocalPolicyRenewalSaveData>();

		public List<LocalPolicyEffectRecordSaveData> Effects { get; set; } = new List<LocalPolicyEffectRecordSaveData>();

		public string EffectReason { get; set; }
	}

	internal sealed class LocalPolicyEffectRecordSaveData
	{
		public List<PolicyEffectInstanceSaveData> ModuleEffects { get; set; } = new List<PolicyEffectInstanceSaveData>();

		public List<PolicyEffectExecutionReceipt> ExecutionReceipts { get; set; } = new List<PolicyEffectExecutionReceipt>();

		public string TargetScope { get; set; }

		public string TargetHandle { get; set; }

		public string TargetLabel { get; set; }

		public string ActiveEffectId { get; set; }

		public string TargetKingdomId { get; set; }

		public string TargetKingdomName { get; set; }

		public List<string> TargetClanIds { get; set; } = new List<string>();

		public List<string> DirectTargetSettlementIds { get; set; } = new List<string>();

		public bool FollowCurrentRulingClan { get; set; }

		public int RemainingDays { get; set; }

		public bool IsPermanentEffect { get; set; }

		public bool IsEnded { get; set; }

		public string EndReason { get; set; }

		public string Reason { get; set; }
	}

	private sealed class LocalPolicyMentionTargetSelection
	{
		public List<string> ClanIds { get; } = new List<string>();

		public List<string> SettlementIds { get; } = new List<string>();

		public List<PolicyTargetHandleSaveData> TargetHandles { get; } = new List<PolicyTargetHandleSaveData>();

		public bool FollowCurrentRulingClan;

		public int CurrentSettlementCount;

		public bool HasSelectors => ClanIds.Count > 0 || SettlementIds.Count > 0 || FollowCurrentRulingClan;
	}

	private sealed class PolicyTargetHandleSaveData
	{
		public string Key { get; set; }

		public string Kind { get; set; }

		public string EntityId { get; set; }

		[JsonProperty("selectorId", NullValueHandling = NullValueHandling.Ignore)]
		public string SelectorId { get; set; }

		[JsonProperty("targetPlan", NullValueHandling = NullValueHandling.Ignore)]
		public PolicyTargetPlanSaveData TargetPlan { get; set; }

		public string DisplayName { get; set; }

		public string KingdomId { get; set; }

		public string KingdomName { get; set; }

		public bool FollowCurrentRulingClan { get; set; }

		public int CurrentSettlementCount { get; set; }

		[JsonIgnore]
		public bool IsSemanticTarget { get; set; }

		[JsonIgnore]
		public string SemanticEvidence { get; set; }
	}

	private enum PlayerPolicyEffectValidationErrorKind
	{
		None,
		InvalidStructure,
		IncompleteLinkedMechanism,
		UnknownOrUnauthorizedTargetHandle,
		UnauthorizedModuleTargetPair,
		MissingIssuerKingdomEffect,
		CompilationOrSafety
	}

	internal sealed class LocalPolicyTargetSnapshotSaveData
	{
		public string FiefId { get; set; }

		public string Name { get; set; }

		public string TypeText { get; set; }

		public List<string> BoundVillageNames { get; set; } = new List<string>();
	}

	internal sealed class LocalPolicyRenewalSaveData
	{
		public int Day { get; set; }

		public string DateText { get; set; }

		public int PaidGold { get; set; }

		public int IndependenceCost { get; set; }

		public int IndependenceBefore { get; set; }

		public int IndependenceAfter { get; set; }

		public int AddedDays { get; set; }
	}
}
