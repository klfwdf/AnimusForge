using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge.PolicyEffects;

 [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
internal sealed class PolicyEffectModuleRegistrationAttribute : Attribute
{
	internal PolicyEffectModuleRegistrationAttribute(Type moduleType)
	{
		ModuleType = moduleType ?? throw new ArgumentNullException(nameof(moduleType));
	}

	internal Type ModuleType { get; }
}

internal static class PolicyEffectScopes
{
	internal const string Kingdom = "kingdom";
	internal const string Local = "local";
	internal const string Vassal = "vassal";
}

internal enum PolicyEffectFamily
{
	Economy,
	Supply,
	Population,
	Governance,
	Security,
	Military,
	Fiscal,
	Construction,
	Stability
}

internal enum PolicyEffectExecutionKind
{
	ModelModifier = 0,
	DailyMutation = 1,
	OneShot = 2,
	Composite = 3,
	ScheduledOnce = 4
}

internal enum PolicyEffectHook
{
	SettlementProsperityDaily,
	TownFoodDaily,
	VillageHearthDaily,
	TownLoyaltyDaily,
	TownSecurityDaily,
	SettlementMilitiaDaily,
	TownTaxIncome,
	SettlementConstructionDaily,
	KingdomStabilityOnActivation,
	DailyScheduler,
	ArmyFormationScore,
	KingdomVillageRaidBlock,
	VolunteerProductionProbability
}

internal enum PolicyEffectAggregationKind
{
	Additive,
	PercentPoints,
	IntegerDelta,
	AnyBlock
}

internal enum PolicyEffectValueUnit
{
	PointsPerDay,
	PercentPoints,
	PointsOnce,
	GoldOnce,
	GoldPerDay,
	RelativePercent,
	BooleanFlag
}

internal enum PolicyEffectTargetKind
{
	Settlement,
	Town,
	Village,
	Clan,
	Kingdom,
	Hero
}

internal enum PolicyEffectTargetProjectionKind
{
	None,
	SettlementOwnerClanLeader,
	PrimaryFiefAndBoundSettlements
}

internal enum PolicyEffectTargetRefreshKind
{
	Dynamic,
	FrozenCanonicalIds
}

internal enum PolicyEffectTargetBindingKind
{
	Selectable,
	IssuerKingdom
}

internal enum PolicyEffectPrimaryTargetOrigin
{
	SettlementSelector,
	TargetPlanPrimarySettlement,
	AggregateSelector,
	LegacyCanonicalSettlement
}

internal enum PolicyEffectFundingMode
{
	InheritPolicy,
	Gold,
	Influence,
	Unscaled
}

internal enum PolicyEffectFundingStrategy
{
	None,
	Linear,
	FullOnly,
	Custom
}

internal sealed class PolicyEffectModuleDescriptor
{
	internal PolicyEffectModuleDescriptor(
		string id,
		int order,
		IReadOnlyCollection<string> legacyIds,
		IReadOnlyCollection<string> allowedScopes,
		IReadOnlyCollection<PolicyEffectTargetKind> allowedSelectorKinds,
		IReadOnlyCollection<PolicyEffectTargetKind> targetKinds,
		IReadOnlyCollection<string> cueTerms,
		string retrievalText,
		string catalogSummary,
		string mainInstruction,
		string postprocessRule,
		JObject payloadPromptSchema,
		PolicyEffectFamily family,
		PolicyEffectExecutionKind executionKind,
		PolicyEffectHook hook,
		PolicyEffectAggregationKind aggregation,
		PolicyEffectValueUnit valueUnit,
		PolicyEffectFundingMode fundingMode,
		int payloadSchemaVersion,
		bool supportsRollback = false,
		bool supportsIdempotency = false,
		int minimumReadablePayloadSchemaVersion = 0,
		IReadOnlyCollection<int> payloadMigrationSourceVersions = null,
		int runtimeStateSchemaVersion = 1,
		int minimumReadableRuntimeStateSchemaVersion = 0,
		IReadOnlyCollection<int> runtimeStateMigrationSourceVersions = null,
		bool promptVisible = true,
		string displayGroup = "",
		string playerDisplayName = "",
		string editableUnderstandingPrompt = "",
		string editableEvaluationPrompt = "",
		PolicyEffectTargetProjectionKind targetProjection = PolicyEffectTargetProjectionKind.None,
		PolicyEffectTargetRefreshKind targetRefresh = PolicyEffectTargetRefreshKind.Dynamic,
		bool allowIndependentClanTargets = false,
		PolicyEffectTargetBindingKind targetBinding = PolicyEffectTargetBindingKind.Selectable,
		bool excludeActorClanTargets = false,
		bool allowCrossKingdomTargets = false)
		: this(
			id,
			order,
			legacyIds,
			allowedScopes,
			allowedSelectorKinds,
			targetKinds,
			cueTerms,
			retrievalText,
			catalogSummary,
			mainInstruction,
			postprocessRule,
			payloadPromptSchema,
			family,
			executionKind,
			hook,
			aggregation,
			valueUnit,
			fundingMode,
			PolicyEffectFundingStrategy.None,
			payloadSchemaVersion,
			supportsRollback,
			supportsIdempotency,
			minimumReadablePayloadSchemaVersion,
			payloadMigrationSourceVersions,
			runtimeStateSchemaVersion,
			minimumReadableRuntimeStateSchemaVersion,
			runtimeStateMigrationSourceVersions,
			promptVisible,
			displayGroup,
			playerDisplayName,
			editableUnderstandingPrompt,
			editableEvaluationPrompt,
			targetProjection,
			targetRefresh,
			allowIndependentClanTargets,
			targetBinding,
			excludeActorClanTargets,
			allowCrossKingdomTargets)
	{
	}

	internal PolicyEffectModuleDescriptor(
		string id,
		int order,
		IReadOnlyCollection<string> legacyIds,
		IReadOnlyCollection<string> allowedScopes,
		IReadOnlyCollection<PolicyEffectTargetKind> allowedSelectorKinds,
		IReadOnlyCollection<PolicyEffectTargetKind> targetKinds,
		IReadOnlyCollection<string> cueTerms,
		string retrievalText,
		string catalogSummary,
		string mainInstruction,
		string postprocessRule,
		JObject payloadPromptSchema,
		PolicyEffectFamily family,
		PolicyEffectExecutionKind executionKind,
		PolicyEffectHook hook,
		PolicyEffectAggregationKind aggregation,
		PolicyEffectValueUnit valueUnit,
		PolicyEffectFundingMode fundingMode,
		PolicyEffectFundingStrategy fundingStrategy,
		int payloadSchemaVersion,
		bool supportsRollback = false,
		bool supportsIdempotency = false,
		int minimumReadablePayloadSchemaVersion = 0,
		IReadOnlyCollection<int> payloadMigrationSourceVersions = null,
		int runtimeStateSchemaVersion = 1,
		int minimumReadableRuntimeStateSchemaVersion = 0,
		IReadOnlyCollection<int> runtimeStateMigrationSourceVersions = null,
		bool promptVisible = true,
		string displayGroup = "",
		string playerDisplayName = "",
		string editableUnderstandingPrompt = "",
		string editableEvaluationPrompt = "",
		PolicyEffectTargetProjectionKind targetProjection = PolicyEffectTargetProjectionKind.None,
		PolicyEffectTargetRefreshKind targetRefresh = PolicyEffectTargetRefreshKind.Dynamic,
		bool allowIndependentClanTargets = false,
		PolicyEffectTargetBindingKind targetBinding = PolicyEffectTargetBindingKind.Selectable,
		bool excludeActorClanTargets = false,
		bool allowCrossKingdomTargets = false)
	{
		Id = id;
		Order = order;
		LegacyIds = legacyIds;
		AllowedScopes = allowedScopes;
		AllowedSelectorKinds = allowedSelectorKinds;
		TargetKinds = targetKinds;
		CueTerms = cueTerms;
		RetrievalText = retrievalText;
		CatalogSummary = catalogSummary;
		MainInstruction = mainInstruction;
		PostprocessRule = postprocessRule;
		PayloadPromptSchema = payloadPromptSchema;
		Family = family;
		ExecutionKind = executionKind;
		Hook = hook;
		Aggregation = aggregation;
		ValueUnit = valueUnit;
		FundingMode = fundingMode;
		FundingStrategy = fundingStrategy;
		PayloadSchemaVersion = payloadSchemaVersion;
		MinimumReadablePayloadSchemaVersion = minimumReadablePayloadSchemaVersion > 0
			? minimumReadablePayloadSchemaVersion
			: payloadSchemaVersion;
		PayloadMigrationSourceVersions = (payloadMigrationSourceVersions ?? Array.Empty<int>()).ToArray();
		RuntimeStateSchemaVersion = runtimeStateSchemaVersion;
		MinimumReadableRuntimeStateSchemaVersion = minimumReadableRuntimeStateSchemaVersion > 0
			? minimumReadableRuntimeStateSchemaVersion
			: runtimeStateSchemaVersion;
		RuntimeStateMigrationSourceVersions = (runtimeStateMigrationSourceVersions ?? Array.Empty<int>()).ToArray();
		SupportsRollback = supportsRollback;
		SupportsIdempotency = supportsIdempotency;
		PromptVisible = promptVisible;
		DisplayGroup = (displayGroup ?? string.Empty).Trim();
		PlayerDisplayName = string.IsNullOrWhiteSpace(playerDisplayName)
			? (catalogSummary ?? string.Empty).Trim()
			: playerDisplayName.Trim();
		EditableUnderstandingPrompt = string.IsNullOrWhiteSpace(editableUnderstandingPrompt)
			? "仅当政策措施会直接影响“" + PlayerDisplayName + "”时，才把它列为实际政策后果；应说明受影响对象、变化方向、强弱和直接原因。"
			: editableUnderstandingPrompt.Trim();
		EditableEvaluationPrompt = string.IsNullOrWhiteSpace(editableEvaluationPrompt)
			? "根据政策原文、执行范围和资源承诺，判断“" + PlayerDisplayName + "”是否发生变化，并给出符合游戏尺度的方向与强度；没有直接因果时不要添加。"
			: editableEvaluationPrompt.Trim();
		TargetProjection = targetProjection;
		TargetRefresh = targetRefresh;
		AllowIndependentClanTargets = allowIndependentClanTargets;
		TargetBinding = targetBinding;
		ExcludeActorClanTargets = excludeActorClanTargets;
		AllowCrossKingdomTargets = allowCrossKingdomTargets;
	}

	internal string Id { get; }

	internal string ValueKey => Id;

	internal int Order { get; }

	internal IReadOnlyCollection<string> LegacyIds { get; }

	internal IReadOnlyCollection<string> AllowedScopes { get; }

	internal IReadOnlyCollection<PolicyEffectTargetKind> AllowedSelectorKinds { get; }

	internal IReadOnlyCollection<PolicyEffectTargetKind> TargetKinds { get; }

	internal PolicyEffectTargetProjectionKind TargetProjection { get; }

	internal PolicyEffectTargetRefreshKind TargetRefresh { get; }

	internal bool AllowIndependentClanTargets { get; }

	internal PolicyEffectTargetBindingKind TargetBinding { get; }

	internal bool ExcludeActorClanTargets { get; }

	internal bool AllowCrossKingdomTargets { get; }

	internal IReadOnlyCollection<string> CueTerms { get; }

	internal string RetrievalText { get; }

	internal string CatalogSummary { get; }

	internal string MainInstruction { get; }

	internal string PostprocessRule { get; }

	internal JObject PayloadPromptSchema { get; }

	internal PolicyEffectFamily Family { get; }

	internal PolicyEffectExecutionKind ExecutionKind { get; }

	internal PolicyEffectHook Hook { get; }

	internal PolicyEffectAggregationKind Aggregation { get; }

	internal PolicyEffectValueUnit ValueUnit { get; }

	internal PolicyEffectFundingMode FundingMode { get; }

	internal PolicyEffectFundingStrategy FundingStrategy { get; }

	internal int PayloadSchemaVersion { get; }

	internal int MinimumReadablePayloadSchemaVersion { get; }

	internal IReadOnlyCollection<int> PayloadMigrationSourceVersions { get; }

	internal int RuntimeStateSchemaVersion { get; }

	internal int MinimumReadableRuntimeStateSchemaVersion { get; }

	internal IReadOnlyCollection<int> RuntimeStateMigrationSourceVersions { get; }

	internal bool SupportsRollback { get; }

	internal bool SupportsIdempotency { get; }

	internal bool PromptVisible { get; }

	internal string DisplayGroup { get; }

	internal string PlayerDisplayName { get; }

	internal string EditableUnderstandingPrompt { get; }

	internal string EditableEvaluationPrompt { get; }
}

internal abstract class PolicyEffectPayload
{
	[JsonProperty("moduleId")]
	public string ModuleId { get; set; } = string.Empty;

	[JsonProperty("schemaVersion")]
	public int SchemaVersion { get; set; }
}

internal abstract class PolicyEffectPayload<TValue> : PolicyEffectPayload
{
	[JsonProperty("value", Required = Required.Always)]
	public TValue Value { get; set; }
}

internal abstract class NumericPolicyEffectPayload : PolicyEffectPayload<float>
{
}

internal interface IPolicyEffectModule
{
	PolicyEffectModuleDescriptor Descriptor { get; }

	string Id { get; }

	int Order { get; }

	IReadOnlyCollection<string> AllowedScopes { get; }

	IReadOnlyCollection<string> CueTerms { get; }

	string RetrievalText { get; }

	string CatalogSummary { get; }

	string MainInstruction { get; }

	string PostprocessRule { get; }

	Type PayloadType { get; }

	bool TryNormalizePayload(JToken rawPayload, string scope, out PolicyEffectPayload normalizedPayload, out string error);

	bool TryMigratePayload(JToken persistedPayload, int sourceVersion, out PolicyEffectPayload migratedPayload, out string error);

	bool TryMigrateRuntimeState(JToken persistedState, int sourceVersion, out JToken migratedState, out string error);

	bool TryApplyFunding(PolicyEffectPayload payload, PolicyEffectFundingContext funding, out PolicyEffectPayload fundedPayload, out string error);

	PolicyEffectPrepareResult Prepare(PolicyEffectCompileContext context, PolicyEffectPayload payload);

	string DescribePayload(PolicyEffectPayload payload);

	// Legacy numeric bridge only. New compile/execution code must use typed payload APIs above.
	bool TryNormalizeValue(float rawValue, string scope, out float normalizedValue, out string error);
}

internal interface IPolicyEffectModule<TPayload> : IPolicyEffectModule
	where TPayload : PolicyEffectPayload, new()
{
	bool TryNormalizeTypedPayload(TPayload payload, string scope, out TPayload normalizedPayload, out string error);

	bool TryMigrateTypedPayload(JToken persistedPayload, int sourceVersion, out TPayload migratedPayload, out string error);

	bool TryApplyTypedFunding(TPayload payload, PolicyEffectFundingContext funding, out TPayload fundedPayload, out string error);

	PolicyEffectPrepareResult PrepareTyped(PolicyEffectCompileContext context, TPayload payload);

	string DescribeTypedPayload(TPayload payload);
}

internal interface IModelModifierPolicyEffectModule
{
	IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(PolicyEffectPreparedInstance preparedInstance);
}

internal interface IDailyPolicyEffectModule
{
	PolicyEffectExecutionResult ExecuteDaily(PolicyEffectExecutionContext context);
}

internal interface ICompensatingDailyPolicyEffectModule
{
	PolicyEffectExecutionResult CompensateDaily(PolicyEffectExecutionContext context);
}

internal interface IOneShotPolicyEffectModule
{
	PolicyEffectExecutionResult ApplyOnce(PolicyEffectExecutionContext context);

	PolicyEffectExecutionResult RollbackOnce(PolicyEffectExecutionContext context);
}

internal interface IPolicyEffectCompositeModule
{
	IReadOnlyCollection<string> RuntimeModuleIds { get; }

	bool TryExpand(
		PolicyEffectCompileContext context,
		PolicyEffectPayload payload,
		out IReadOnlyList<PolicyEffectCompositeChild> children,
		out string error);
}

internal interface IScheduledOncePolicyEffectModule
{
	PolicyEffectExecutionResult ExecuteScheduledOnce(PolicyEffectExecutionContext context);

	PolicyEffectExecutionResult CompensateScheduledOnce(PolicyEffectExecutionContext context);
}

// Optional contract for independent Hero gold mutations whose complete target
// batch must be preflighted before any Hero balance is changed.
internal interface IAtomicHeroGoldPolicyEffectModule
{
	bool TryReadDelta(PolicyEffectPayload payload, out int delta);
}

internal interface IPolicyEffectLifecycleModule
{
	PolicyEffectExecutionResult OnActivated(PolicyEffectExecutionContext context);

	PolicyEffectExecutionResult OnRenewed(PolicyEffectExecutionContext context);

	PolicyEffectExecutionResult OnExpired(PolicyEffectExecutionContext context);

	PolicyEffectExecutionResult OnAbolished(PolicyEffectExecutionContext context);
}

internal abstract class PolicyEffectModuleBase : IPolicyEffectModule
{
	public abstract PolicyEffectModuleDescriptor Descriptor { get; }

	public string Id => Descriptor.Id;

	public int Order => Descriptor.Order;

	public IReadOnlyCollection<string> AllowedScopes => Descriptor.AllowedScopes;

	public IReadOnlyCollection<string> CueTerms => Descriptor.CueTerms;

	public string RetrievalText => Descriptor.RetrievalText;

	public string CatalogSummary => Descriptor.CatalogSummary;

	public string MainInstruction => Descriptor.MainInstruction;

	public string PostprocessRule => Descriptor.PostprocessRule;

	public abstract Type PayloadType { get; }

	public abstract bool TryNormalizePayload(JToken rawPayload, string scope, out PolicyEffectPayload normalizedPayload, out string error);

	public abstract bool TryMigratePayload(JToken persistedPayload, int sourceVersion, out PolicyEffectPayload migratedPayload, out string error);

	public virtual bool TryMigrateRuntimeState(
		JToken persistedState,
		int sourceVersion,
		out JToken migratedState,
		out string error)
	{
		migratedState = null;
		if (sourceVersion != Descriptor.RuntimeStateSchemaVersion)
		{
			error = "效果 runtime state 版本不受支持: " + sourceVersion;
			return false;
		}
		migratedState = persistedState?.DeepClone();
		error = string.Empty;
		return true;
	}

	public abstract bool TryApplyFunding(PolicyEffectPayload payload, PolicyEffectFundingContext funding, out PolicyEffectPayload fundedPayload, out string error);

	public abstract PolicyEffectPrepareResult Prepare(PolicyEffectCompileContext context, PolicyEffectPayload payload);

	public abstract string DescribePayload(PolicyEffectPayload payload);

	public virtual bool TryNormalizeValue(float rawValue, string scope, out float normalizedValue, out string error)
	{
		normalizedValue = 0f;
		error = "该模块不是 legacy 数值效果";
		return false;
	}

}

internal abstract class PolicyEffectModuleBase<TPayload> : PolicyEffectModuleBase, IPolicyEffectModule<TPayload>
	where TPayload : PolicyEffectPayload, new()
{
	private static readonly JsonSerializer SafeSerializer = JsonSerializer.Create(new JsonSerializerSettings
	{
		TypeNameHandling = TypeNameHandling.None,
		MissingMemberHandling = MissingMemberHandling.Error
	});

	public sealed override Type PayloadType => typeof(TPayload);

	public virtual bool TryNormalizeTypedPayload(TPayload payload, string scope, out TPayload normalizedPayload, out string error)
	{
		normalizedPayload = payload;
		return TryValidateEnvelope(payload, out error);
	}

	public virtual bool TryMigrateTypedPayload(JToken persistedPayload, int sourceVersion, out TPayload migratedPayload, out string error)
	{
		if (sourceVersion != Descriptor.PayloadSchemaVersion)
		{
			migratedPayload = null;
			error = "效果 payload 版本不受支持: " + sourceVersion;
			return false;
		}
		return TryDeserializeTypedPayload(persistedPayload, out migratedPayload, out error)
			&& TryNormalizeTypedPayload(migratedPayload, string.Empty, out migratedPayload, out error);
	}

	public virtual bool TryApplyTypedFunding(TPayload payload, PolicyEffectFundingContext funding, out TPayload fundedPayload, out string error)
	{
		fundedPayload = null;
		if (Descriptor.FundingStrategy != PolicyEffectFundingStrategy.None)
		{
			error = "结构化效果的非 None funding 策略必须由模块覆写 TryApplyTypedFunding";
			return false;
		}
		fundedPayload = payload;
		return TryValidateEnvelope(payload, out error);
	}

	public virtual PolicyEffectPrepareResult PrepareTyped(PolicyEffectCompileContext context, TPayload payload)
	{
		if (context == null)
		{
			return PolicyEffectPrepareResult.Rejected("效果编译上下文不能为空");
		}
		if (!TryValidateEnvelope(payload, out string error))
		{
			return PolicyEffectPrepareResult.Rejected(error);
		}
		return PolicyEffectPrepareResult.Accepted(new PolicyEffectPreparedInstance
		{
			Instance = new PolicyEffectInstance
			{
				InstanceId = context.InstanceId,
				PolicyId = context.PolicyId,
				ActorHeroId = context.ActorHeroId,
				ModuleId = Id,
				SourceModuleId = string.IsNullOrWhiteSpace(context.SourceModuleId) ? Id : context.SourceModuleId,
				TargetSet = context.TargetSet,
				Payload = payload,
				LifecycleState = PolicyEffectLifecycleState.Prepared,
				StartDay = context.StartDay,
				EndDay = context.EndDay,
				SourceScope = context.SourceScope,
				Reason = context.Reason
			},
			Descriptor = Descriptor,
			IdempotencyKey = context.IdempotencyKey
		});
	}

	public virtual string DescribeTypedPayload(TPayload payload)
	{
		return payload == null ? string.Empty : JToken.FromObject(payload, SafeSerializer).ToString(Formatting.None);
	}

	public sealed override bool TryNormalizePayload(JToken rawPayload, string scope, out PolicyEffectPayload normalizedPayload, out string error)
	{
		normalizedPayload = null;
		if (!TryValidateRawPayload(rawPayload, out error)
			|| !TryDeserializeTypedPayload(rawPayload, out TPayload typedPayload, out error)
			|| !TryNormalizeTypedPayload(typedPayload, scope, out TPayload normalizedTypedPayload, out error))
		{
			return false;
		}
		normalizedPayload = normalizedTypedPayload;
		return true;
	}

	protected virtual bool TryValidateRawPayload(JToken rawPayload, out string error)
	{
		error = string.Empty;
		return true;
	}

	public sealed override bool TryMigratePayload(JToken persistedPayload, int sourceVersion, out PolicyEffectPayload migratedPayload, out string error)
	{
		migratedPayload = null;
		if (!TryMigrateTypedPayload(persistedPayload, sourceVersion, out TPayload migratedTypedPayload, out error))
		{
			return false;
		}
		migratedPayload = migratedTypedPayload;
		return true;
	}

	public sealed override bool TryApplyFunding(PolicyEffectPayload payload, PolicyEffectFundingContext funding, out PolicyEffectPayload fundedPayload, out string error)
	{
		fundedPayload = null;
		if (!(payload is TPayload typedPayload))
		{
			error = "效果 payload 类型与模块不匹配";
			return false;
		}
		if (!TryApplyTypedFunding(typedPayload, funding, out TPayload fundedTypedPayload, out error))
		{
			return false;
		}
		fundedPayload = fundedTypedPayload;
		return true;
	}

	public sealed override PolicyEffectPrepareResult Prepare(PolicyEffectCompileContext context, PolicyEffectPayload payload)
	{
		return payload is TPayload typedPayload
			? PrepareTyped(context, typedPayload)
			: PolicyEffectPrepareResult.Rejected("效果 payload 类型与模块不匹配");
	}

	public sealed override string DescribePayload(PolicyEffectPayload payload)
	{
		return payload is TPayload typedPayload ? DescribeTypedPayload(typedPayload) : string.Empty;
	}

	protected bool TryValidateEnvelope(TPayload payload, out string error)
	{
		if (payload == null)
		{
			error = "效果 payload 不能为空";
			return false;
		}
		if (string.IsNullOrWhiteSpace(payload.ModuleId))
		{
			payload.ModuleId = Id;
		}
		if (payload.SchemaVersion <= 0)
		{
			payload.SchemaVersion = Descriptor.PayloadSchemaVersion;
		}
		if (!string.Equals(payload.ModuleId, Id, StringComparison.Ordinal))
		{
			error = "效果 payload 的 moduleId 与模块不匹配";
			return false;
		}
		if (payload.SchemaVersion != Descriptor.PayloadSchemaVersion)
		{
			error = "效果 payload 版本不受支持";
			return false;
		}
		error = string.Empty;
		return true;
	}

	private static bool TryDeserializeTypedPayload(JToken token, out TPayload payload, out string error)
	{
		payload = null;
		error = string.Empty;
		if (token == null || token.Type == JTokenType.Null)
		{
			error = "效果 payload 不能为空";
			return false;
		}
		try
		{
			payload = token.ToObject<TPayload>(SafeSerializer);
			if (payload == null)
			{
				error = "效果 payload 无法反序列化";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			error = "效果 payload 无法反序列化: " + ex.Message;
			return false;
		}
	}
}

internal abstract class NumericPolicyEffectModuleBase<TPayload> : PolicyEffectModuleBase<TPayload>
	where TPayload : NumericPolicyEffectPayload, new()
{
	public sealed override bool TryNormalizeValue(float rawValue, string scope, out float normalizedValue, out string error)
	{
		normalizedValue = rawValue;
		error = string.Empty;
		if (float.IsNaN(rawValue) || float.IsInfinity(rawValue))
		{
			normalizedValue = 0f;
			error = "必须是有限数字";
			return false;
		}
		return TryNormalizeNumericValue(rawValue, scope, out normalizedValue, out error);
	}

	protected virtual bool TryNormalizeNumericValue(float rawValue, string scope, out float normalizedValue, out string error)
	{
		normalizedValue = rawValue;
		error = string.Empty;
		return true;
	}

	public sealed override bool TryNormalizeTypedPayload(TPayload payload, string scope, out TPayload normalizedPayload, out string error)
	{
		normalizedPayload = payload;
		if (!TryValidateEnvelope(payload, out error)
			|| !TryNormalizeValue(payload.Value, scope, out float normalizedValue, out error))
		{
			return false;
		}
		payload.Value = normalizedValue;
		return true;
	}

	public override bool TryApplyTypedFunding(TPayload payload, PolicyEffectFundingContext funding, out TPayload fundedPayload, out string error)
	{
		fundedPayload = null;
		if (!TryValidateEnvelope(payload, out error))
		{
			return false;
		}
		float resolvedScale = funding?.ResolveScale(Descriptor.FundingMode) ?? 1f;
		float scale;
		switch (Descriptor.FundingStrategy)
		{
			case PolicyEffectFundingStrategy.None:
				scale = 1f;
				break;
			case PolicyEffectFundingStrategy.Linear:
				scale = resolvedScale;
				break;
			case PolicyEffectFundingStrategy.FullOnly:
				scale = resolvedScale >= 0.9999f ? 1f : 0f;
				break;
			case PolicyEffectFundingStrategy.Custom:
				fundedPayload = null;
				error = "Custom funding 策略必须由模块覆写 TryApplyTypedFunding";
				return false;
			default:
				fundedPayload = null;
				error = "未知 funding 策略";
				return false;
		}
		TPayload candidate = new TPayload
		{
			ModuleId = Id,
			SchemaVersion = Descriptor.PayloadSchemaVersion,
			Value = payload.Value * scale
		};
		return TryNormalizeTypedPayload(candidate, string.Empty, out fundedPayload, out error);
	}

	public override string DescribeTypedPayload(TPayload payload)
	{
		return payload == null ? string.Empty : payload.Value.ToString("0.###", CultureInfo.InvariantCulture);
	}

	internal TPayload CreateLegacyNumericPayload(float normalizedValue)
	{
		return new TPayload
		{
			ModuleId = Id,
			SchemaVersion = Descriptor.PayloadSchemaVersion,
			Value = normalizedValue
		};
	}
}
