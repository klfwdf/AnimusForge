using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AnimusForge.PolicyEffects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed class NpcRulerPolicyRecord
{
	public int Version { get; set; } = 6;

	[JsonProperty("policyId")]
	public string PolicyId { get; set; }

	public string ReReviewRootRecordId { get; set; }

	public string ReReviewSourceRecordId { get; set; }

	public string SupersedesRecordId { get; set; }

	public bool ReReviewReplacementCommitted { get; set; }

	[JsonProperty("policyObjectId")]
	public string PolicyObjectId { get; set; }

	[JsonProperty("agendaStatus")]
	public string AgendaStatus { get; set; }

	[JsonProperty("batchId")]
	public string BatchId { get; set; }

	[JsonProperty("kingdomId")]
	public string KingdomId { get; set; }

	[JsonProperty("kingdomName")]
	public string KingdomName { get; set; }

	[JsonProperty("policyKind")]
	public string PolicyKind { get; set; }

	[JsonProperty("issuerKingdomId")]
	public string IssuerKingdomId { get; set; }

	[JsonProperty("issuerKingdomName")]
	public string IssuerKingdomName { get; set; }

	[JsonProperty("policyCooldownDay")]
	public int PolicyCooldownDay { get; set; } = -1;

	[JsonProperty("rulerHeroId")]
	public string RulerHeroId { get; set; }

	[JsonProperty("rulerName")]
	public string RulerName { get; set; }

	[JsonProperty("creativePremise")]
	public string CreativePremise { get; set; }

	[JsonProperty("policyName")]
	public string PolicyName { get; set; }

	[JsonProperty("policyContent")]
	public string PolicyContent { get; set; }

	[JsonProperty("policyDigest")]
	public string PolicyDigest { get; set; }

	[JsonProperty("eventPremise")]
	public string EventPremise { get; set; }

	[JsonProperty("publicFeedback")]
	public string PublicFeedback { get; set; }

	[JsonProperty("feedbackTitle")]
	public string FeedbackTitle { get; set; }

	[JsonProperty("feedbackDigest")]
	public string FeedbackDigest { get; set; }

	[JsonProperty("publicFeedbackNoticeDueHour")]
	public int PublicFeedbackNoticeDueHour { get; set; } = -1;

	[JsonProperty("publicFeedbackNoticeShown")]
	public bool PublicFeedbackNoticeShown { get; set; }

	[JsonProperty("approvalAnnouncementPublished")]
	public bool ApprovalAnnouncementPublished { get; set; }

	[JsonProperty("approvalPolicyEventPublished")]
	public bool ApprovalPolicyEventPublished { get; set; }

	[JsonProperty("approvalPublicFeedbackPublished")]
	public bool ApprovalPublicFeedbackPublished { get; set; }

	[JsonProperty("approvalWeeklyMaterialRecorded")]
	public bool ApprovalWeeklyMaterialRecorded { get; set; }

	[JsonProperty("approvalCoreCommitFailureCount")]
	public int ApprovalCoreCommitFailureCount { get; set; }

	[JsonProperty("approvalFailureCallbackFailureCount")]
	public int ApprovalFailureCallbackFailureCount { get; set; }

	[JsonProperty("approvalCommitFailureReason")]
	public string ApprovalCommitFailureReason { get; set; }

	[JsonProperty("approvalFailureFinalizationPending")]
	public bool ApprovalFailureFinalizationPending { get; set; }

	[JsonProperty("approvalCommitIsRenewal")]
	public bool ApprovalCommitIsRenewal { get; set; }

	[JsonProperty("effectBundleRollbackPending")]
	public bool EffectBundleRollbackPending { get; set; }

	[JsonProperty("isPlayerPolicy")]
	public bool IsPlayerPolicy { get; set; }

	[JsonProperty("eventType")]
	public string EventType { get; set; }

	[JsonProperty("impactSummary")]
	public string ImpactSummary { get; set; }

	[JsonProperty("isPlayerSuggested")]
	public bool IsPlayerSuggested { get; set; }

	[JsonProperty("suggestionChainName")]
	public string SuggestionChainName { get; set; }

	[JsonProperty("playerProposalDigest")]
	public string PlayerProposalDigest { get; set; }

	[JsonProperty("authoritarianWeight")]
	public float? AuthoritarianWeight { get; set; }

	[JsonProperty("oligarchicWeight")]
	public float? OligarchicWeight { get; set; }

	[JsonProperty("egalitarianWeight")]
	public float? EgalitarianWeight { get; set; }

	public int Day { get; set; }

	public string GameDate { get; set; }

	public long CreatedUtcTicks { get; set; }

	[JsonProperty("effects")]
	public List<NpcRulerPolicyEffectDto> Effects { get; set; } = new List<NpcRulerPolicyEffectDto>();

	[JsonProperty("executionReceipts")]
	internal List<PolicyEffectExecutionReceipt> ExecutionReceipts { get; set; } = new List<PolicyEffectExecutionReceipt>();

	[JsonProperty("durationDays")]
	public int DurationDays { get; set; }

	[JsonIgnore]
	internal List<PolicyEffectWireEffect> WireEffects { get; set; } = new List<PolicyEffectWireEffect>();
}

public sealed class NpcRulerPolicyEffectDto
{
	[JsonProperty("effectId")]
	public string EffectId { get; set; }

	[JsonProperty("targetKingdomId")]
	public string TargetKingdomId { get; set; }

	[JsonProperty("targetKingdomName")]
	public string TargetKingdomName { get; set; }

	[JsonProperty("durationDays")]
	public int DurationDays { get; set; }

	[JsonProperty("remainingDays")]
	public int RemainingDays { get; set; }

	[JsonProperty("isEnded")]
	public bool IsEnded { get; set; }

	[JsonProperty("reason")]
	public string Reason { get; set; }

	[JsonProperty("moduleEffects")]
	internal List<PolicyEffectInstanceSaveData> ModuleEffects { get; set; } = new List<PolicyEffectInstanceSaveData>();
}

internal sealed class NpcRulerPolicyWireRecord
{
	[JsonProperty("effectSchemaVersion", Required = Required.Always)]
	internal int EffectSchemaVersion { get; set; } = PolicyEffectDataVersions.WireSchemaVersion;

	[JsonProperty("durationDays", Required = Required.Always)]
	internal int DurationDays { get; set; }

	[JsonProperty("effects", Required = Required.Always)]
	internal List<PolicyEffectWireEffect> Effects { get; set; } = new List<PolicyEffectWireEffect>();

	[JsonExtensionData]
	internal IDictionary<string, JToken> AdditionalFields { get; set; } = new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);

	internal NpcRulerPolicyRecord ToPersistedRecord()
	{
		JObject raw = new JObject();
		foreach (KeyValuePair<string, JToken> field in AdditionalFields ?? new Dictionary<string, JToken>())
		{
			if (!string.IsNullOrWhiteSpace(field.Key))
			{
				raw[field.Key] = field.Value?.DeepClone();
			}
		}
		raw["durationDays"] = DurationDays;
		raw["effects"] = new JArray();
		NpcRulerPolicyRecord record = raw.ToObject<NpcRulerPolicyRecord>() ?? new NpcRulerPolicyRecord();
		record.Version = 6;
		record.DurationDays = DurationDays;
		record.ExecutionReceipts = new List<PolicyEffectExecutionReceipt>();
		record.WireEffects = (Effects ?? new List<PolicyEffectWireEffect>()).Where(effect => effect != null).ToList();
		return record;
	}
}

internal sealed class NpcRulerPolicyWireResponse
{
	[JsonProperty("policies", Required = Required.Always)]
	internal List<NpcRulerPolicyWireRecord> Policies { get; set; } = new List<NpcRulerPolicyWireRecord>();
}

internal sealed class NpcRulerPolicyDraftWireRecord
{
	[JsonProperty("kingdomId", Required = Required.Always)]
	internal string KingdomId { get; set; } = string.Empty;

	[JsonProperty("kingdomName", Required = Required.Always)]
	internal string KingdomName { get; set; } = string.Empty;

	[JsonProperty("rulerHeroId", Required = Required.Always)]
	internal string RulerHeroId { get; set; } = string.Empty;

	[JsonProperty("rulerName", Required = Required.Always)]
	internal string RulerName { get; set; } = string.Empty;

	[JsonProperty("creativePremise", Required = Required.Always)]
	internal string CreativePremise { get; set; } = string.Empty;

	[JsonProperty("policyName", Required = Required.Always)]
	internal string PolicyName { get; set; } = string.Empty;

	[JsonProperty("policyContent", Required = Required.Always)]
	internal string PolicyContent { get; set; } = string.Empty;

	[JsonProperty("policyDigest", Required = Required.Always)]
	internal string PolicyDigest { get; set; } = string.Empty;

	[JsonProperty("eventPremise", Required = Required.Always)]
	internal string EventPremise { get; set; } = string.Empty;

	[JsonProperty("feedbackTitle", Required = Required.Always)]
	internal string FeedbackTitle { get; set; } = string.Empty;

	[JsonProperty("publicFeedback", Required = Required.Always)]
	internal string PublicFeedback { get; set; } = string.Empty;

	[JsonProperty("feedbackDigest", Required = Required.Always)]
	internal string FeedbackDigest { get; set; } = string.Empty;

	[JsonProperty("impactSummary", Required = Required.Always)]
	internal string ImpactSummary { get; set; } = string.Empty;

	[JsonProperty("numericIntent", Required = Required.Always)]
	internal string NumericIntent { get; set; } = string.Empty;

	[JsonProperty("authoritarianWeight", Required = Required.Always)]
	internal float? AuthoritarianWeight { get; set; }

	[JsonProperty("oligarchicWeight", Required = Required.Always)]
	internal float? OligarchicWeight { get; set; }

	[JsonProperty("egalitarianWeight", Required = Required.Always)]
	internal float? EgalitarianWeight { get; set; }

	[JsonProperty("durationDays", Required = Required.Always)]
	internal int DurationDays { get; set; }
}

internal sealed class NpcRulerPolicyDraftWireResponse
{
	[JsonProperty("policy", Required = Required.Always)]
	internal NpcRulerPolicyDraftWireRecord Policy { get; set; }
}

internal sealed class NpcRulerPolicyEffectPlanWireResponse
{
	[JsonProperty("effectPlanVersion", Required = Required.Always)]
	internal int EffectPlanVersion { get; set; }

	[JsonProperty("durationDays", Required = Required.Always)]
	internal int DurationDays { get; set; }

	[JsonProperty("effects", Required = Required.Always)]
	internal List<PolicyEffectWireEffect> Effects { get; set; } = new List<PolicyEffectWireEffect>();
}

internal sealed class NpcPolicyHistoryEntry
{
	internal string EntryId { get; set; } = string.Empty;

	internal string SourceKind { get; set; } = string.Empty;

	internal string ScopeKind { get; set; } = string.Empty;

	internal string OwnerKingdomId { get; set; } = string.Empty;

	internal string OwnerKingdomName { get; set; } = string.Empty;

	internal string OwnerClanId { get; set; } = string.Empty;

	internal string IssuerKingdomId { get; set; } = string.Empty;

	internal string IssuerKingdomName { get; set; } = string.Empty;

	internal List<string> TargetKingdomIds { get; set; } = new List<string>();

	internal List<string> TargetClanIds { get; set; } = new List<string>();

	internal List<string> TargetSettlementIds { get; set; } = new List<string>();

	internal string PolicyName { get; set; } = string.Empty;

	internal string PolicyContent { get; set; } = string.Empty;

	internal string ImpactSummary { get; set; } = string.Empty;

	internal string PolicyStatus { get; set; } = string.Empty;

	internal string RawPolicyStatus { get; set; } = string.Empty;

	internal string HistoryBucket { get; set; } = string.Empty;

	internal string EffectStatus { get; set; } = string.Empty;

	internal List<string> EffectSummaries { get; set; } = new List<string>();

	internal int PublishedDay { get; set; }

	internal long CreatedUtcTicks { get; set; }

	internal float RecallScore { get; set; }

	internal string RetrievalText => "政策状态：" + BuildPolicyStatusRetrievalText(HistoryBucket, RawPolicyStatus, PolicyStatus)
		+ "；机械效果状态：" + BuildEffectStatusRetrievalText(EffectStatus) + "。\n"
		+ (PolicyName ?? string.Empty).Trim()
		+ "\n" + (PolicyContent ?? string.Empty).Trim()
		+ "\n" + (ImpactSummary ?? string.Empty).Trim()
		+ "\n" + string.Join("；", EffectSummaries ?? new List<string>());

	internal string DialogueRetrievalText => PolicyHistoryRetrievalService.BuildDialogueRetrievalText(this);

	private static string BuildPolicyStatusRetrievalText(string bucket, string rawStatus, string status)
	{
		string raw = (rawStatus ?? string.Empty).Trim();
		if (string.Equals(bucket, PolicyHistoryRetrievalService.CurrentBucket, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
		{
			return "现行有效";
		}
		if (string.Equals(raw, "abolished", StringComparison.OrdinalIgnoreCase)
			|| (raw.Length == 0 && string.Equals(status, "abolished", StringComparison.OrdinalIgnoreCase)))
		{
			return "已经废除";
		}
		if (string.Equals(bucket, PolicyHistoryRetrievalService.HistoricalBucket, StringComparison.OrdinalIgnoreCase))
		{
			return "历史结束（" + (raw.Length == 0 ? status : raw) + "）";
		}
		return "未知";
	}

	private static string BuildEffectStatusRetrievalText(string status)
	{
		if (string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
		{
			return "仍在运行";
		}
		if (string.Equals(status, "expired", StringComparison.OrdinalIgnoreCase))
		{
			return "已经到期但政策仍可现行";
		}
		if (string.Equals(status, "ended_by_abolition", StringComparison.OrdinalIgnoreCase))
		{
			return "随政策废除而终止";
		}
		return string.IsNullOrWhiteSpace(status) ? "未知" : status.Trim();
	}
}

internal sealed class NpcPolicyHistorySelectionFilter
{
	internal string QueryText { get; set; } = string.Empty;

	internal List<string> AllowedOwnerKingdomIds { get; set; } = new List<string>();

	internal List<string> AllowedSources { get; set; } = new List<string>();

	internal List<string> AllowedTargetKingdomIds { get; set; } = new List<string>();

	internal List<string> AllowedTargetClanIds { get; set; } = new List<string>();

	internal List<string> AllowedTargetSettlementIds { get; set; } = new List<string>();

	internal string RequiredStatus { get; set; } = string.Empty;

	internal string RequiredBucket { get; set; } = string.Empty;

	internal string RequiredEffectStatus { get; set; } = string.Empty;

	internal bool RequireOwnerMatch { get; set; }

	internal int MaxCount { get; set; }

	internal float MinimumScore { get; set; } = float.NegativeInfinity;
}

internal sealed class PolicyEnemyKingdomSnapshot
{
	internal string KingdomId { get; set; } = string.Empty;

	internal string KingdomName { get; set; } = string.Empty;
}

internal sealed class PolicyEnemyLatestPolicy
{
	internal PolicyEnemyKingdomSnapshot Enemy { get; set; }

	internal NpcPolicyHistoryEntry Entry { get; set; }
}

internal sealed class PolicyHistoryRetrievalResult
{
	internal List<NpcPolicyHistoryEntry> RelatedCurrentPolicies { get; set; } = new List<NpcPolicyHistoryEntry>();

	internal List<NpcPolicyHistoryEntry> RelatedHistoricalPolicies { get; set; } = new List<NpcPolicyHistoryEntry>();

	internal List<PolicyEnemyLatestPolicy> EnemyLatestPolicies { get; set; } = new List<PolicyEnemyLatestPolicy>();

	internal string EnemyPrompt { get; set; } = string.Empty;

	internal string SemanticPrompt { get; set; } = string.Empty;

	internal string CombinedPrompt { get; set; } = string.Empty;

	internal string DialoguePrompt { get; set; } = string.Empty;

	internal string DialoguePromptHash { get; set; } = string.Empty;

	internal string DialogueFailureCode { get; set; } = string.Empty;

	internal List<string> DialogueOwnerKingdomIds { get; set; } = new List<string>();

	internal string DialogueQueryHash { get; set; } = string.Empty;

	internal int EnemyCount { get; set; }

	internal int EnemyWithPolicyCount { get; set; }

	internal int DocumentVectorCacheHits { get; set; }

	internal int DocumentVectorCacheMisses { get; set; }

	internal int DialogueMentionTermCount { get; set; }

	internal int DialogueAttemptedQueryCount { get; set; }

	internal int DialogueSuccessfulQueryCount { get; set; }

	internal int DialogueCandidateCount { get; set; }

	internal int DialogueHitCount { get; set; }

	internal int DialogueQueryChars { get; set; }
}

internal sealed class NpcPolicyPrompt
{
	public string SystemPrompt { get; set; } = "";

	public string Preview => "System:\n" + (SystemPrompt ?? "");
}

internal sealed class NpcPolicyApiCallResult
{
	public bool Success;

	public string Content = "";

	public string ErrorMessage = "";

	public string FinishReason = "";

	public bool IsOutputTruncated;

	public int? PromptTokens;

	public int? CompletionTokens;

	public int? TotalTokens;

	public int? PromptCacheHitTokens;

	public int? PromptCacheMissTokens;

	public int? StatusCode;

	public string ResponseBody = "";

	public bool IsRateLimit;

	public bool IsRequestsPerMinuteLimit;

	public bool IsQuotaLimit;

	public bool IsAuthFailure;

	public bool IsTimeout;

	public int? RetryAfterSeconds;

	public int? RetryAfterSecondsRaw;

	public bool RetryAfterSecondsCapped;

	public int AttemptsUsed;

	public string ResolvedRoute = "";

	public bool ThinkingRetryPlain;
}

internal sealed class PolicyApiExecutionProfile
{
	public string RequestedSource = DuelSettings.PolicyApiSourceMain;

	public string ResolvedRoute = "";

	public string EffectiveApiUrl = "";

	public string ApiKey = "";

	public string ModelName = "";

	public int MaxTokens = DuelSettings.DefaultPolicyApiMaxTokens;

	public float Temperature = 0.8f;

	public bool ThinkingEnabled;

	public string ReasoningEffort = DuelSettings.ReasoningEffortHigh;

	public bool UseJsonObjectResponse;

	public PolicyApiExecutionProfile Clone()
	{
		return new PolicyApiExecutionProfile
		{
			RequestedSource = RequestedSource,
			ResolvedRoute = ResolvedRoute,
			EffectiveApiUrl = EffectiveApiUrl,
			ApiKey = ApiKey,
			ModelName = ModelName,
			MaxTokens = MaxTokens,
			Temperature = Temperature,
			ThinkingEnabled = ThinkingEnabled,
			ReasoningEffort = ReasoningEffort,
			UseJsonObjectResponse = UseJsonObjectResponse
		};
	}
}
