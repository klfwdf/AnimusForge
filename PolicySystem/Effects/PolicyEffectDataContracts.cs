using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using AnimusForge.PolicyTargets;

namespace AnimusForge.PolicyEffects;

internal static class PolicyEffectDataVersions
{
	internal const int WireSchemaVersion = 1;
	internal const int SaveSchemaVersion = 2;
}

internal static class PolicyEffectPlanVersions
{
	internal const int CurrentVersion = 1;
	internal const int MaximumMechanisms = 6;
	internal const int MaximumMechanismIdLength = 24;
}

internal static class PolicyEffectMechanismContract
{
	internal const int CurrentVersion = 1;

	internal static bool TryFreeze(IEnumerable<PolicyEffectInstanceSaveData> instances, out string error)
	{
		error = string.Empty;
		foreach (IGrouping<string, PolicyEffectInstanceSaveData> group in (instances
			?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
			.Where(instance => instance != null
				&& instance.EffectPlanVersion == PolicyEffectPlanVersions.CurrentVersion
				&& instance.MechanismKind == PolicyEffectMechanismKind.Linked)
			.GroupBy(instance => (instance.PolicyId ?? string.Empty) + "\u001f" + (instance.MechanismId ?? string.Empty),
				StringComparer.Ordinal))
		{
			List<PolicyEffectInstanceSaveData> legs = group.ToList();
			List<string> expectedIds = legs
				.Select(leg => (leg.InstanceId ?? string.Empty).Trim())
				.OrderBy(value => value, StringComparer.Ordinal)
				.ToList();
			if (expectedIds.Count == 0
				|| expectedIds.Any(string.IsNullOrWhiteSpace)
				|| expectedIds.Distinct(StringComparer.Ordinal).Count() != expectedIds.Count)
			{
				error = "linked mechanism contract contains an empty or duplicated instanceId";
				return false;
			}
			string hash = ComputeHash(legs);
			foreach (PolicyEffectInstanceSaveData leg in legs)
			{
				leg.MechanismContractVersion = CurrentVersion;
				leg.MechanismContractHash = hash;
				leg.ExpectedMechanismLegIds = new List<string>(expectedIds);
			}
		}
		return true;
	}

	internal static bool TryValidateLinkedGroup(
		IEnumerable<PolicyEffectInstanceSaveData> instances,
		out string error)
	{
		error = string.Empty;
		List<PolicyEffectInstanceSaveData> legs = (instances
			?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
			.Where(instance => instance != null)
			.ToList();
		if (legs.Count == 0)
		{
			error = "linked mechanism contract has no legs";
			return false;
		}
		PolicyEffectInstanceSaveData first = legs[0];
		List<string> expected = NormalizeExpectedIds(first.ExpectedMechanismLegIds);
		if (first.MechanismContractVersion != CurrentVersion
			|| expected.Count == 0
			|| expected.Distinct(StringComparer.Ordinal).Count() != expected.Count
			|| string.IsNullOrWhiteSpace(first.MechanismContractHash))
		{
			error = "linked mechanism contract is missing or unsupported";
			return false;
		}
		if (legs.Any(leg => leg.EffectPlanVersion != PolicyEffectPlanVersions.CurrentVersion
			|| leg.MechanismKind != PolicyEffectMechanismKind.Linked
			|| !string.Equals(leg.PolicyId ?? string.Empty, first.PolicyId ?? string.Empty, StringComparison.Ordinal)
			|| !string.Equals(leg.MechanismId ?? string.Empty, first.MechanismId ?? string.Empty, StringComparison.Ordinal)
			|| leg.MechanismContractVersion != CurrentVersion
			|| !string.Equals(leg.MechanismContractHash ?? string.Empty, first.MechanismContractHash ?? string.Empty, StringComparison.Ordinal)
			|| !NormalizeExpectedIds(leg.ExpectedMechanismLegIds).SequenceEqual(expected, StringComparer.Ordinal)))
		{
			error = "linked mechanism contract metadata is inconsistent";
			return false;
		}
		List<string> actual = legs.Select(leg => (leg.InstanceId ?? string.Empty).Trim())
			.OrderBy(value => value, StringComparer.Ordinal)
			.ToList();
		if (actual.Any(string.IsNullOrWhiteSpace)
			|| actual.Distinct(StringComparer.Ordinal).Count() != actual.Count
			|| !actual.SequenceEqual(expected, StringComparer.Ordinal))
		{
			error = "linked mechanism contract is missing or contains unexpected legs";
			return false;
		}
		string computed = ComputeHash(legs);
		if (!string.Equals(computed, first.MechanismContractHash, StringComparison.Ordinal))
		{
			error = "linked mechanism contract hash mismatch";
			return false;
		}
		return true;
	}

	private static List<string> NormalizeExpectedIds(IEnumerable<string> values)
	{
		return (values ?? Enumerable.Empty<string>())
			.Select(value => (value ?? string.Empty).Trim())
			.Where(value => value.Length > 0)
			.OrderBy(value => value, StringComparer.Ordinal)
			.ToList();
	}

	private static string ComputeHash(IEnumerable<PolicyEffectInstanceSaveData> instances)
	{
		StringBuilder canonical = new StringBuilder();
		foreach (PolicyEffectInstanceSaveData leg in instances
			.OrderBy(value => value.InstanceId ?? string.Empty, StringComparer.Ordinal))
		{
			Append(canonical, leg.PolicyId);
			Append(canonical, leg.MechanismId);
			Append(canonical, leg.InstanceId);
			Append(canonical, leg.ModuleId);
			Append(canonical, ((int)leg.MechanismRole).ToString(System.Globalization.CultureInfo.InvariantCulture));
			Append(canonical, leg.SourceOmitted ? "1" : "0");
			Append(canonical, leg.DestinationOmitted ? "1" : "0");
		}
		using (SHA256 sha256 = SHA256.Create())
		{
			byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString()));
			return string.Concat(hash.Select(value => value.ToString("x2", System.Globalization.CultureInfo.InvariantCulture)));
		}
	}

	private static void Append(StringBuilder builder, string value)
	{
		string normalized = value ?? string.Empty;
		builder.Append(normalized.Length).Append(':').Append(normalized).Append('|');
	}
}

internal static class PolicyEffectSemanticContract
{
	internal const int CurrentVersion = 1;
	internal const int CurrentMappingVersion = 1;
	internal const int MaximumIntents = PolicyEffectPlanVersions.MaximumMechanisms;
	internal const int MaximumLegs = 12;
	internal const int MaximumTextLength = 240;
	internal const int MaximumStrengthLength = 80;
	internal const string SameMetricLifecyclePromptRule = "同一自然目标、同一机械指标和同一变化方向，只因一次性、每日、即时、持续或先后阶段等生命周期安排不同，仍只占一条语义腿；生命周期差异不是新的机械指标，由后续可表达该指标的 Composite payload 字段承载。不同自然目标、不同机械指标或不同变化方向仍必须拆成独立语义腿。";
	internal const string PlayerFundingAndExcludedTargetPromptRule = "startupGoldCost 与 dailyMaintenanceGoldCost 是政策资金元数据。普通拨款、预算、经费、行政投入、建设投入、启动费和维护费只能反映在这两个政策成本字段，不得因此重复生成执行腿或虚构国库、财政账户等目标；它们不妨碍正文明确的受益变化生成可表达的效果腿。cost 是非执行性的政策支出；真实可执行资源减少必须写为 source，不能写为 cost。正文明确的正向资源变化若只由 startupGoldCost 或 dailyMaintenanceGoldCost 这类政策资金承担、没有正文明确的可执行 source，必须写为 subject 独立机制，不得写成 beneficiary 加 omitted source；只有正文确实表达了当前未知但真实存在的外部资源来源时，才允许省略 source 的 linked 机制。粮食调拨、税收转移、个人账户转账等当前 Candidate 能表达的真实资源流必须保留 source 与 beneficiary/destination。若正文机械目标只属于本次全部 Candidate 的排除范围或全部不可表达，effectIntents 必须为空，不得改写成其他可用目标。";
	internal const string PlayerLocalOwnerPayAndTrainingPromptRule = "玩家地方政策中的军饷、薪俸、津贴或地方财政补助，若正文未明确其他个人接收者，默认形成受影响定居点当前所有者家族领袖的正人物第纳尔收益；目标描述必须保留定居点绑定语义，不得泛化成全国领主、士兵或无关发布者。启动费、维护费和普通拨款仍只属于政策资金元数据，不得重复生成人物负金币。这条规则只确定地方金币收益的目标与方向，不限制政策还可产生哪些其他效果；其他效果必须由模型根据完整政策语义、投入、机制和因果关系独立判断，不得仅凭军饷、训练等单个关键词机械追加或排除。金币、兵种经验及其他不同机械指标若同时成立，必须分别保留。";
}

internal static class PolicyEffectPlanDefaults
{
	internal static string BuildIndependentMechanismId(string stableKey)
	{
		ulong hash = 14695981039346656037UL;
		foreach (byte value in Encoding.UTF8.GetBytes(stableKey ?? string.Empty))
		{
			hash ^= value;
			hash *= 1099511628211UL;
		}
		return "I" + hash.ToString("x16", System.Globalization.CultureInfo.InvariantCulture);
	}
}

[JsonConverter(typeof(StringEnumConverter), true)]
internal enum PolicyEffectMechanismKind
{
	Independent,
	Linked
}

[JsonConverter(typeof(StringEnumConverter), true)]
internal enum PolicyEffectMechanismRole
{
	Subject,
	Source,
	Destination,
	Cost,
	Beneficiary
}

internal sealed class PolicyEffectSemanticPlan
{
	internal int Version { get; set; } = PolicyEffectSemanticContract.CurrentVersion;

	internal List<PolicyEffectSemanticIntent> Intents { get; set; } = new List<PolicyEffectSemanticIntent>();
}

internal sealed class PolicyEffectSemanticIntent
{
	internal string MechanismId { get; set; } = string.Empty;

	internal PolicyEffectMechanismKind MechanismKind { get; set; }

	internal bool SourceOmitted { get; set; }

	internal bool DestinationOmitted { get; set; }

	internal List<PolicyEffectSemanticLeg> Legs { get; set; } = new List<PolicyEffectSemanticLeg>();
}

internal sealed class PolicyEffectSemanticLeg
{
	internal string IntentLegId { get; set; } = string.Empty;

	internal PolicyEffectMechanismRole Role { get; set; }

	internal string TargetDescription { get; set; } = string.Empty;

	internal string EffectDescription { get; set; } = string.Empty;

	internal string Strength { get; set; } = string.Empty;

	internal string Reason { get; set; } = string.Empty;
}

internal static class PolicyEffectRuntimeStateEnvelope
{
	internal const string FrameworkProperty = "_policyEffectFramework";

	internal const string ModuleProperty = "moduleState";

	internal const int FrameworkSchemaVersion = 1;
}

internal enum PolicyEffectLifecycleState
{
	Prepared,
	Active,
	Suspended,
	Completed,
	RolledBack,
	Failed
}

internal enum PolicyEffectExecutionStatus
{
	Applied,
	Skipped,
	Failed,
	AlreadyApplied,
	RolledBack
}

internal enum PolicyEffectLifecycleEventKind
{
	Renewed,
	Expired,
	Abolished,
	Activated
}

internal sealed class PolicyEffectWireEnvelope
{
	[JsonProperty("schemaVersion", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public int SchemaVersion { get; set; } = PolicyEffectDataVersions.WireSchemaVersion;

	[JsonProperty("effects", Required = Required.Always)]
	public List<PolicyEffectWireEffect> Effects { get; set; } = new List<PolicyEffectWireEffect>();
}

internal sealed class PolicyEffectWireEffect
{
	[JsonIgnore]
	public string SourceModuleId { get; set; } = string.Empty;

	[JsonProperty("effectPlanVersion", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public int EffectPlanVersion { get; set; }

	[JsonProperty("mechanismId", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public string MechanismId { get; set; } = string.Empty;

	[JsonProperty("mechanismKind", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public PolicyEffectMechanismKind MechanismKind { get; set; }

	[JsonProperty("mechanismRole", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public PolicyEffectMechanismRole MechanismRole { get; set; }

	[JsonProperty("sourceOmitted", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public bool SourceOmitted { get; set; }

	[JsonProperty("destinationOmitted", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public bool DestinationOmitted { get; set; }

	[JsonProperty("moduleId", Required = Required.Always)]
	public string ModuleId { get; set; } = string.Empty;

	[JsonProperty("targetHandles", Required = Required.Always)]
	public List<string> TargetHandles { get; set; } = new List<string>();

	[JsonProperty("targets", NullValueHandling = NullValueHandling.Ignore)]
	private List<string> LegacyTargets { get; set; }

	[JsonProperty("payload", Required = Required.Always)]
	public JToken Payload { get; set; }

	[JsonProperty("reason")]
	public string Reason { get; set; } = string.Empty;

	[System.Runtime.Serialization.OnDeserialized]
	private void RestoreLegacyTargets(System.Runtime.Serialization.StreamingContext context)
	{
		if ((TargetHandles == null || TargetHandles.Count == 0) && LegacyTargets != null)
		{
			TargetHandles = new List<string>(LegacyTargets);
		}
		LegacyTargets = null;
	}
}

internal sealed class PolicyEffectTarget
{
	[JsonProperty("kind", Required = Required.Always)]
	public PolicyEffectTargetKind Kind { get; set; }

	[JsonProperty("id", Required = Required.Always)]
	public string Id { get; set; } = string.Empty;

	[JsonProperty("handle")]
	public string Handle { get; set; } = string.Empty;

	[JsonProperty("parentId")]
	public string ParentId { get; set; } = string.Empty;

	[JsonProperty("scope")]
	public string Scope { get; set; } = string.Empty;
}

internal sealed class PolicyEffectCanonicalTargetSet
{
	[JsonProperty("structureVersion")]
	public int StructureVersion { get; set; } = 1;

	[JsonProperty("selectorHandles")]
	public List<string> SelectorHandles { get; set; } = new List<string>();

	[JsonProperty("selectorIds", NullValueHandling = NullValueHandling.Ignore)]
	public List<string> SelectorIds { get; set; } = new List<string>();

	[JsonProperty("targetPlans", NullValueHandling = NullValueHandling.Ignore)]
	public List<PolicyTargetPlanSaveData> TargetPlans { get; set; } = new List<PolicyTargetPlanSaveData>();

	[JsonProperty("settlementIds")]
	public List<string> SettlementIds { get; set; } = new List<string>();

	[JsonProperty("townIds")]
	public List<string> TownIds { get; set; } = new List<string>();

	[JsonProperty("villageIds")]
	public List<string> VillageIds { get; set; } = new List<string>();

	[JsonProperty("clanIds")]
	public List<string> ClanIds { get; set; } = new List<string>();

	[JsonProperty("kingdomIds")]
	public List<string> KingdomIds { get; set; } = new List<string>();

	[JsonProperty("heroIds", NullValueHandling = NullValueHandling.Ignore)]
	public List<string> HeroIds { get; set; } = new List<string>();

	public bool ShouldSerializeHeroIds()
	{
		return HeroIds != null && HeroIds.Count > 0;
	}

	[JsonProperty("parentSettlementIds")]
	public List<string> ParentSettlementIds { get; set; } = new List<string>();

	[JsonProperty("followCurrentRulingClan")]
	public bool FollowCurrentRulingClan { get; set; }
}

internal sealed class PolicyEffectInstance
{
	public int MechanismContractVersion { get; set; }

	public string MechanismContractHash { get; set; } = string.Empty;

	public List<string> ExpectedMechanismLegIds { get; set; } = new List<string>();

	public int EffectPlanVersion { get; set; } = PolicyEffectPlanVersions.CurrentVersion;

	public string MechanismId { get; set; } = string.Empty;

	public PolicyEffectMechanismKind MechanismKind { get; set; } = PolicyEffectMechanismKind.Independent;

	public PolicyEffectMechanismRole MechanismRole { get; set; } = PolicyEffectMechanismRole.Subject;

	public bool SourceOmitted { get; set; }

	public bool DestinationOmitted { get; set; }

	public string InstanceId { get; set; } = string.Empty;

	public string PolicyId { get; set; } = string.Empty;

	public string ActorHeroId { get; set; } = string.Empty;

	public string ModuleId { get; set; } = string.Empty;

	public string SourceModuleId { get; set; } = string.Empty;

	public PolicyEffectCanonicalTargetSet TargetSet { get; set; }

	public PolicyEffectPayload Payload { get; set; }

	public PolicyEffectLifecycleState LifecycleState { get; set; } = PolicyEffectLifecycleState.Prepared;

	public float StartDay { get; set; }

	public float EndDay { get; set; }

	public string SourceScope { get; set; } = string.Empty;

	public string Reason { get; set; } = string.Empty;
}

internal sealed class PolicyEffectInstanceSaveData
{
	[JsonProperty("mechanismContractVersion", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public int MechanismContractVersion { get; set; }

	[JsonProperty("mechanismContractHash", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public string MechanismContractHash { get; set; } = string.Empty;

	[JsonProperty("expectedMechanismLegIds", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public List<string> ExpectedMechanismLegIds { get; set; } = new List<string>();

	[JsonProperty("effectPlanVersion", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public int EffectPlanVersion { get; set; }

	[JsonProperty("mechanismId", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public string MechanismId { get; set; } = string.Empty;

	[JsonProperty("mechanismKind", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public PolicyEffectMechanismKind MechanismKind { get; set; }

	[JsonProperty("mechanismRole", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public PolicyEffectMechanismRole MechanismRole { get; set; }

	[JsonProperty("sourceOmitted", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public bool SourceOmitted { get; set; }

	[JsonProperty("destinationOmitted", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public bool DestinationOmitted { get; set; }

	[JsonProperty("instanceId", Required = Required.Always)]
	public string InstanceId { get; set; } = string.Empty;

	[JsonProperty("policyId")]
	public string PolicyId { get; set; } = string.Empty;

	[JsonProperty("actorHeroId", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public string ActorHeroId { get; set; } = string.Empty;

	[JsonProperty("moduleId", Required = Required.Always)]
	public string ModuleId { get; set; } = string.Empty;

	[JsonProperty("sourceModuleId", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public string SourceModuleId { get; set; } = string.Empty;

	[JsonProperty("payloadSchemaVersion", Required = Required.Always)]
	public int PayloadSchemaVersion { get; set; }

	[JsonProperty("payload", Required = Required.Always)]
	public JToken Payload { get; set; }

	[JsonProperty("targetSet", Required = Required.Always)]
	public PolicyEffectCanonicalTargetSet TargetSet { get; set; }

	[JsonProperty("lifecycleState", Required = Required.Always)]
	public PolicyEffectLifecycleState LifecycleState { get; set; }

	[JsonProperty("stateSchemaVersion")]
	public int StateSchemaVersion { get; set; }

	[JsonProperty("runtimeState", NullValueHandling = NullValueHandling.Ignore)]
	public JToken RuntimeState { get; set; }

	[JsonProperty("executionReceipt", NullValueHandling = NullValueHandling.Ignore)]
	public PolicyEffectExecutionReceipt ExecutionReceipt { get; set; }

	[JsonProperty("startDay")]
	public float StartDay { get; set; }

	[JsonProperty("endDay")]
	public float EndDay { get; set; }

	[JsonProperty("sourceScope")]
	public string SourceScope { get; set; } = string.Empty;

	[JsonProperty("reason")]
	public string Reason { get; set; } = string.Empty;
}

internal sealed class PolicyEffectSaveEnvelope
{
	[JsonProperty("schemaVersion", Required = Required.Always)]
	public int SchemaVersion { get; set; } = PolicyEffectDataVersions.SaveSchemaVersion;

	[JsonProperty("instances", Required = Required.Always)]
	public List<PolicyEffectInstanceSaveData> Instances { get; set; } = new List<PolicyEffectInstanceSaveData>();

	[JsonProperty("receipts")]
	public List<PolicyEffectExecutionReceipt> Receipts { get; set; } = new List<PolicyEffectExecutionReceipt>();
}

internal sealed class PolicyEffectExecutionReceipt
{
	[JsonProperty("receiptId", Required = Required.Always)]
	public string ReceiptId { get; set; } = string.Empty;

	[JsonProperty("instanceId", Required = Required.Always)]
	public string InstanceId { get; set; } = string.Empty;

	[JsonProperty("policyId")]
	public string PolicyId { get; set; } = string.Empty;

	[JsonProperty("moduleId", Required = Required.Always)]
	public string ModuleId { get; set; } = string.Empty;

	[JsonProperty("targetSet", Required = Required.Always)]
	public PolicyEffectCanonicalTargetSet TargetSet { get; set; }

	[JsonProperty("status", Required = Required.Always)]
	public PolicyEffectExecutionStatus Status { get; set; }

	[JsonProperty("requestedValue")]
	public float RequestedValue { get; set; }

	[JsonProperty("appliedValue")]
	public float AppliedValue { get; set; }

	[JsonProperty("requestedPayload", NullValueHandling = NullValueHandling.Ignore)]
	public JToken RequestedPayload { get; set; }

	[JsonProperty("appliedPayload", NullValueHandling = NullValueHandling.Ignore)]
	public JToken AppliedPayload { get; set; }

	[JsonProperty("campaignDay")]
	public float CampaignDay { get; set; }

	[JsonProperty("message")]
	public string Message { get; set; } = string.Empty;
}

internal sealed class PolicyEffectFundingContext
{
	public int RequiredGold { get; set; }

	public int PaidGold { get; set; }

	public int RequiredInfluence { get; set; }

	public int PaidInfluence { get; set; }

	public float GoldScale { get; set; } = 1f;

	public float InfluenceScale { get; set; } = 1f;

	public float ResolveScale(PolicyEffectFundingMode mode)
	{
		switch (mode)
		{
			case PolicyEffectFundingMode.Gold:
				return ClampScale(GoldScale);
			case PolicyEffectFundingMode.Influence:
				return ClampScale(InfluenceScale);
			case PolicyEffectFundingMode.Unscaled:
				return 1f;
			default:
				return Math.Min(ClampScale(GoldScale), ClampScale(InfluenceScale));
		}
	}

	private static float ClampScale(float value)
	{
		if (float.IsNaN(value) || float.IsInfinity(value))
		{
			return 0f;
		}
		return Math.Max(0f, Math.Min(1f, value));
	}
}

internal sealed class PolicyEffectCompileContext
{
	public string InstanceId { get; set; } = string.Empty;

	public string PolicyId { get; set; } = string.Empty;

	public string ActorHeroId { get; set; } = string.Empty;

	public IPolicyEffectModule Module { get; set; }

	public string SourceModuleId { get; set; } = string.Empty;

	public PolicyEffectCanonicalTargetSet TargetSet { get; set; }

	public PolicyEffectPayload Payload { get; set; }

	public PolicyEffectFundingContext Funding { get; set; }

	public string IdempotencyKey { get; set; } = string.Empty;

	public float StartDay { get; set; }

	public float EndDay { get; set; }

	public string SourceScope { get; set; } = string.Empty;

	public string Reason { get; set; } = string.Empty;
}

internal sealed class PolicyEffectCompositeChild
{
	public string ModuleId { get; set; } = string.Empty;

	public PolicyEffectPayload Payload { get; set; }
}

internal sealed class PolicyEffectPreparedInstance
{
	public PolicyEffectInstance Instance { get; set; }

	public PolicyEffectModuleDescriptor Descriptor { get; set; }

	public string IdempotencyKey { get; set; } = string.Empty;
}

internal sealed class PolicyEffectModelContribution
{
	public string InstanceId { get; set; } = string.Empty;

	public string ModuleId { get; set; } = string.Empty;

	public PolicyEffectHook Hook { get; set; }

	public PolicyEffectTargetKind TargetKind { get; set; }

	public string TargetId { get; set; } = string.Empty;

	public float Value { get; set; }

	public string DisplayText { get; set; } = string.Empty;
}

internal sealed class PolicyEffectPrepareResult
{
	public bool Success { get; set; }

	public PolicyEffectPreparedInstance PreparedInstance { get; set; }

	public string Error { get; set; } = string.Empty;

	internal static PolicyEffectPrepareResult Accepted(PolicyEffectPreparedInstance preparedInstance)
	{
		return new PolicyEffectPrepareResult { Success = true, PreparedInstance = preparedInstance };
	}

	internal static PolicyEffectPrepareResult Rejected(string error)
	{
		return new PolicyEffectPrepareResult { Success = false, Error = error ?? string.Empty };
	}
}

internal sealed class PolicyEffectExecutionContext
{
	public PolicyEffectPreparedInstance PreparedInstance { get; set; }

	public PolicyEffectFundingContext Funding { get; set; }

	public float CampaignDay { get; set; }

	public IPolicyEffectGameBridge GameBridge { get; set; }

	public PolicyEffectExecutionReceipt ExistingReceipt { get; set; }

	public string IdempotencyKey { get; set; } = string.Empty;

	// Daily mutations execute once for each canonical target. Lifecycle, one-shot,
	// and scheduled-once callbacks leave TargetKind null and TargetId empty.
	public PolicyEffectTargetKind? TargetKind { get; set; }

	public string TargetId { get; set; } = string.Empty;

	// One-based execution attempt. Retryable daily and scheduled mutations are capped
	// by the coordinator at 3.
	public int Attempt { get; set; } = 1;

	// Module-owned state. Framework idempotency bookkeeping is kept separately inside
	// PolicyEffectInstanceSaveData.RuntimeState and is never exposed through this value.
	public JToken RuntimeState { get; set; }
}

internal sealed class PolicyEffectExecutionResult
{
	public bool Success => Status == PolicyEffectExecutionStatus.Applied
		|| Status == PolicyEffectExecutionStatus.AlreadyApplied
		|| Status == PolicyEffectExecutionStatus.RolledBack;

	public PolicyEffectExecutionStatus Status { get; set; }

	public PolicyEffectExecutionReceipt Receipt { get; set; }

	public string Error { get; set; } = string.Empty;

	// False is deliberately the default: an unclassified failure is fatal. Daily
	// modules must explicitly opt in when the same target/day may be retried safely.
	public bool Retryable { get; set; }

	// Optional replacement for the module-owned portion of RuntimeState.
	public JToken RuntimeState { get; set; }
}

internal interface IPolicyEffectFundingResolver
{
	float ResolveScale(IPolicyEffectModule module, PolicyEffectFundingContext funding);
}

internal interface IPolicyEffectPreparer
{
	PolicyEffectPrepareResult Prepare(PolicyEffectCompileContext context);
}

internal interface IPolicyEffectLifecycleExecutor
{
	PolicyEffectExecutionResult Activate(PolicyEffectExecutionContext context);

	PolicyEffectExecutionResult Execute(PolicyEffectExecutionContext context);

	PolicyEffectExecutionResult Complete(PolicyEffectExecutionContext context);

	PolicyEffectExecutionResult Rollback(PolicyEffectExecutionContext context);
}

internal interface IPolicyEffectGameBridge
{
	bool TryAdjustKingdomStability(
		string kingdomId,
		int delta,
		string reason,
		out int actualDelta,
		out string error);

	bool TryAdjustKingdomStability(
		string kingdomId,
		int delta,
		string reason,
		out int beforeValue,
		out int afterValue,
		out string error);

	bool TryRestoreKingdomStability(
		string kingdomId,
		int expectedAfterValue,
		int beforeValue,
		string reason,
		out int afterValue,
		out string error);

	bool TryChangeClanInfluence(
		string clanId,
		float delta,
		string reason,
		out float beforeValue,
		out float afterValue,
		out string error);

	bool TryRestoreClanInfluence(
		string clanId,
		float expectedAfterValue,
		float beforeValue,
		string reason,
		out float afterValue,
		out string error);

	bool TryChangeClanLeaderRelation(
		string actorHeroId,
		string targetClanId,
		int delta,
		string reason,
		out string targetHeroId,
		out int beforeValue,
		out int afterValue,
		out string error);

	bool TryRestoreHeroRelation(
		string actorHeroId,
		string targetHeroId,
		int expectedAfterValue,
		int beforeValue,
		string reason,
		out int afterValue,
		out string error);

	bool TryReadHeroGold(
		string heroId,
		out bool available,
		out int gold,
		out string error);

	bool TryChangeHeroGoldExact(
		string heroId,
		int delta,
		string reason,
		out bool available,
		out int beforeValue,
		out int afterValue,
		out string error);

	bool TryRestoreHeroGold(
		string heroId,
		int expectedAfterValue,
		int beforeValue,
		string reason,
		out int afterValue,
		out string error);

	bool TryPrepareClanTroopXp(
		string[] clanIds,
		int xpPerTroop,
		out JToken plan,
		out string error);

	bool TryApplyClanTroopXp(
		JToken plan,
		string reason,
		out JToken journal,
		out int appliedPartyCount,
		out int appliedStackCount,
		out long totalAppliedXp,
		out string error);

	bool TryRestoreClanTroopXp(
		JToken journal,
		string reason,
		out int restoredStackCount,
		out string error);
}
