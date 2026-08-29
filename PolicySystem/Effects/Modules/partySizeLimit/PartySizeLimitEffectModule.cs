using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.PolicyTargets;
using Newtonsoft.Json.Linq;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.PartySizeLimitEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class PartySizeLimitPayload : PolicyEffectPayload<int>
{
}

internal sealed class PartySizeLimitRuntimePayload : PolicyEffectPayload<int>
{
}

internal sealed class PartySizeLimitEffectModule
	: PolicyEffectModuleBase<PartySizeLimitPayload>, IPolicyEffectCompositeModule
{
	internal const int MinimumValue = -100;
	internal const int MaximumValue = 100;
	internal const string ClanLeaderRuntimeModuleId = "partySizeLimitClanLeader";
	internal const string ClanLordsRuntimeModuleId = "partySizeLimitClanLords";

	private static readonly IReadOnlyCollection<string> RuntimeModules = new[]
	{
		ClanLeaderRuntimeModuleId,
		ClanLordsRuntimeModuleId
	};

	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "partySizeLimit",
		order: 170,
		legacyIds: Array.Empty<string>(),
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[]
		{
			PolicyEffectTargetKind.Settlement,
			PolicyEffectTargetKind.Clan,
			PolicyEffectTargetKind.Kingdom,
			PolicyEffectTargetKind.Hero
		},
		targetKinds: new[] { PolicyEffectTargetKind.Clan },
		cueTerms: new[] { "部队上限", "带兵上限", "领主亲兵", "王室卫队", "家族军队规模" },
		retrievalText: "直接增加或减少统治者、地方政策发布地当前所有者家族、指定家族领主或目标王国全体领主的原版部队人数上限；只影响正式领主部队，不影响驻军、民兵、商队、村民、信使或其他非领主队伍。",
		catalogSummary: "调整统治者、家族或全国领主的部队人数上限",
		mainInstruction: "仅当政策明确直接改变统治者、某一家族领主或全国领主可带领的部队人数上限时选择本模块。地方政策的发布地或选定定居点映射为该定居点当前所有者家族，并影响该家族全部正式领主部队；统治者目标只影响当前统治者亲自率领的正式领主部队，并随继位动态转移；家族目标影响该家族全部正式领主部队；王国目标影响当前属于该王国的全部家族领主，包括佣兵家族。不要用于军团组建倾向、驻军容量、民兵、商队、村民、信使、部队工资、招募数量或士兵经验。",
		postprocessRule: "payload 严格为 {\"value\": integer}，禁止未知字段、小数和非有限数。value 是每支命中领主部队在原版上限基础上的固定人数增减，范围 -100 到 +100；正数提高，负数降低。轻微调整取 5 到 10，常规改革取 10 到 25，重大改革取 25 到 50，极端改革取 50 到 80；80 到 100 仅用于统治者专属或依据极强的政策。全国政策会同时作用于大量领主部队，通常取 5 到 20，一般不得超过 30。负数按相同尺度谨慎评估。资金不足时按实际资金比例线性缩放并四舍五入为整数。目标必须明确为地方政策发布地或选定定居点的当前所有者家族、当前统治者、一个或多个家族的领主、或一个王国的全国领主；不得混合统治者专属目标与更广的家族、定居点或王国目标。",
		payloadPromptSchema: CreatePayloadSchema(),
		family: PolicyEffectFamily.Military,
		executionKind: PolicyEffectExecutionKind.Composite,
		hook: PolicyEffectHook.PartyMemberSizeLimit,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PartySizePoints,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Custom,
		payloadSchemaVersion: 1,
		promptVisible: true,
		displayGroup: "partySizeLimit",
		playerDisplayName: "领主部队上限",
		editableUnderstandingPrompt: "领主部队上限表示政策直接改变统治者、地方政策发布地当前所有者家族、指定家族领主或全国领主每支正式领主部队的原版人数上限。地方政策的发布地或选定定居点映射为该定居点当前所有者家族，并影响该家族全部正式领主队伍；统治者目标只影响当前统治者亲自率领的队伍并随继位转移；家族目标影响该家族全部领主队伍；全国目标影响王国当前全部家族，包括佣兵家族。驻军、民兵、商队、村民、信使及其他非正式领主队伍不受影响。不要把军团组建意愿、军队数量、招募、驻军容量、工资或士兵经验解释为部队上限。",
		editableEvaluationPrompt: "value 是每支命中领主部队叠加在原版计算结果上的固定人数，必须为 -100 到 +100 的整数。+20 表示每支部队上限增加 20，-20 表示减少 20；多项政策相加，最终上限最低为 1。轻微编制调整通常取 5 到 10，常规扩军或裁军取 10 到 25，重大常备军改革取 25 到 50，极端禁军或总动员改革取 50 到 80；80 到 100 仅用于统治者专属或政策依据极强的情况。全国效果会同时改变大量领主部队，通常取 5 到 20，一般不要超过 30。负数应按相同尺度谨慎评估，避免普通政策让领主部队失去基本作战能力。",
		targetProjection: PolicyEffectTargetProjectionKind.None,
		targetRefresh: PolicyEffectTargetRefreshKind.Dynamic,
		allowIndependentClanTargets: true,
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public IReadOnlyCollection<string> RuntimeModuleIds => RuntimeModules;

	protected override bool TryValidateRawPayload(JToken rawPayload, out string error)
	{
		if (rawPayload is not JObject obj
			|| !TryReadBoundedInteger(obj["value"], out _))
		{
			error = "partySizeLimit 的 value 必须是 -100 到 +100 之间的整数";
			return false;
		}
		error = string.Empty;
		return true;
	}

	public override bool TryNormalizeTypedPayload(
		PartySizeLimitPayload payload,
		string scope,
		out PartySizeLimitPayload normalizedPayload,
		out string error)
	{
		_ = scope;
		normalizedPayload = payload;
		if (!TryValidateEnvelope(payload, out error))
		{
			return false;
		}
		if (payload.Value < MinimumValue || payload.Value > MaximumValue)
		{
			error = "partySizeLimit 的 value 必须在 -100 到 +100 之间";
			return false;
		}
		error = string.Empty;
		return true;
	}

	public override bool TryApplyTypedFunding(
		PartySizeLimitPayload payload,
		PolicyEffectFundingContext funding,
		out PartySizeLimitPayload fundedPayload,
		out string error)
	{
		fundedPayload = null;
		if (!TryNormalizeTypedPayload(payload, string.Empty, out PartySizeLimitPayload normalized, out error))
		{
			return false;
		}
		double scale = funding?.ResolveScale(ModuleDescriptor.FundingMode) ?? 1d;
		PartySizeLimitPayload candidate = new PartySizeLimitPayload
		{
			ModuleId = ModuleDescriptor.Id,
			SchemaVersion = ModuleDescriptor.PayloadSchemaVersion,
			Value = (int)Math.Round(normalized.Value * scale, MidpointRounding.AwayFromZero)
		};
		return TryNormalizeTypedPayload(candidate, string.Empty, out fundedPayload, out error);
	}

	public bool TryExpand(
		PolicyEffectCompileContext context,
		PolicyEffectPayload payload,
		out IReadOnlyList<PolicyEffectCompositeChild> children,
		out string error)
	{
		children = Array.Empty<PolicyEffectCompositeChild>();
		error = string.Empty;
		if (context?.TargetSet == null || payload is not PartySizeLimitPayload typed)
		{
			error = "partySizeLimit 缺少有效的编译目标或 payload";
			return false;
		}
		if (!TryNormalizeTypedPayload(typed, context.SourceScope, out PartySizeLimitPayload normalized, out error))
		{
			return false;
		}

		PolicyEffectCanonicalTargetSet targetSet = context.TargetSet;
		bool hasDirectRuler = HasSelectorHandle(targetSet.SelectorHandles, 'R');
		bool hasBroadSelector = HasSelectorHandle(targetSet.SelectorHandles, 'C')
			|| HasSelectorHandle(targetSet.SelectorHandles, 'K')
			|| HasSelectorHandle(targetSet.SelectorHandles, 'S')
			|| HasSelectorHandle(targetSet.SelectorHandles, 'L');
		bool hasRulerRole = false;
		foreach (string selectorId in targetSet.SelectorIds ?? new List<string>())
		{
			if (!PolicyHeroTargetSelectorResolver.TryDescribeSelector(
				selectorId,
				out string kind,
				out string value,
				out _)
				|| !string.Equals(kind, "role", StringComparison.Ordinal))
			{
				error = "partySizeLimit 只支持 Hero 角色选择器 ruler 或 lords";
				return false;
			}
			if (string.Equals(value, "ruler", StringComparison.Ordinal))
			{
				hasRulerRole = true;
			}
			else if (string.Equals(value, "lords", StringComparison.Ordinal))
			{
				hasBroadSelector = true;
			}
			else
			{
				error = "partySizeLimit 只支持 Hero 角色选择器 ruler 或 lords";
				return false;
			}
		}

		if (hasDirectRuler && !targetSet.FollowCurrentRulingClan)
		{
			error = "partySizeLimit 的统治者目标必须动态跟随当前统治家族";
			return false;
		}
		bool rulerOnly = targetSet.FollowCurrentRulingClan || hasDirectRuler || hasRulerRole;
		if (!rulerOnly && !hasBroadSelector
			&& ((targetSet.ClanIds?.Count ?? 0) > 0 || (targetSet.KingdomIds?.Count ?? 0) > 0))
		{
			hasBroadSelector = true;
		}
		if (rulerOnly == hasBroadSelector)
		{
			error = rulerOnly
				? "partySizeLimit 不允许混合统治者专属目标与家族或王国领主目标"
				: "partySizeLimit 缺少可判定的统治者、家族或王国领主目标";
			return false;
		}
		if (normalized.Value == 0)
		{
			return true;
		}

		string runtimeModuleId = rulerOnly ? ClanLeaderRuntimeModuleId : ClanLordsRuntimeModuleId;
		children = new[]
		{
			new PolicyEffectCompositeChild
			{
				ModuleId = runtimeModuleId,
				Payload = new PartySizeLimitRuntimePayload
				{
					ModuleId = runtimeModuleId,
					SchemaVersion = 1,
					Value = normalized.Value
				}
			}
		};
		return true;
	}

	public override string DescribeTypedPayload(PartySizeLimitPayload payload)
	{
		return payload == null
			? string.Empty
			: (payload.Value > 0 ? "+" : string.Empty)
				+ payload.Value.ToString(CultureInfo.InvariantCulture) + " 人";
	}

	internal static bool TryReadBoundedInteger(JToken token, out int value)
	{
		value = 0;
		if (token?.Type != JTokenType.Integer)
		{
			return false;
		}
		try
		{
			long parsed = token.Value<long>();
			if (parsed < MinimumValue || parsed > MaximumValue)
			{
				return false;
			}
			value = (int)parsed;
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool HasSelectorHandle(IEnumerable<string> handles, char kind)
	{
		char expected = char.ToUpperInvariant(kind);
		return (handles ?? Array.Empty<string>()).Any(handle =>
		{
			string normalized = (handle ?? string.Empty).Trim();
			return normalized.Length > 0
				&& char.ToUpperInvariant(normalized[0]) == expected
				&& (normalized.Length == 1 || char.IsDigit(normalized[1]) || normalized[1] == ':');
		});
	}

	private static JObject CreatePayloadSchema()
	{
		return new JObject
		{
			["type"] = "object",
			["required"] = new JArray("value"),
			["properties"] = new JObject
			{
				["value"] = new JObject
				{
					["type"] = "integer",
					["minimum"] = MinimumValue,
					["maximum"] = MaximumValue
				}
			},
			["additionalProperties"] = false
		};
	}
}

internal abstract class PartySizeLimitRuntimeEffectModuleBase
	: PolicyEffectModuleBase<PartySizeLimitRuntimePayload>, IModelModifierPolicyEffectModule
{
	protected override bool TryValidateRawPayload(JToken rawPayload, out string error)
	{
		if (rawPayload is not JObject obj
			|| !PartySizeLimitEffectModule.TryReadBoundedInteger(obj["value"], out _))
		{
			error = Id + " 的 value 必须是 -100 到 +100 之间的整数";
			return false;
		}
		error = string.Empty;
		return true;
	}

	public override bool TryNormalizeTypedPayload(
		PartySizeLimitRuntimePayload payload,
		string scope,
		out PartySizeLimitRuntimePayload normalizedPayload,
		out string error)
	{
		_ = scope;
		normalizedPayload = payload;
		if (!TryValidateEnvelope(payload, out error))
		{
			return false;
		}
		if (payload.Value < PartySizeLimitEffectModule.MinimumValue
			|| payload.Value > PartySizeLimitEffectModule.MaximumValue)
		{
			error = Id + " 的 value 必须在 -100 到 +100 之间";
			return false;
		}
		error = string.Empty;
		return true;
	}

	public IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(
		PolicyEffectPreparedInstance preparedInstance)
	{
		if (preparedInstance?.Instance?.Payload is not PartySizeLimitRuntimePayload payload
			|| preparedInstance.Instance.TargetSet?.ClanIds == null
			|| payload.Value == 0)
		{
			return Array.Empty<PolicyEffectModelContribution>();
		}
		return preparedInstance.Instance.TargetSet.ClanIds
			.Select(id => (id ?? string.Empty).Trim())
			.Where(id => id.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(id => id, StringComparer.Ordinal)
			.Select(id => new PolicyEffectModelContribution
			{
				InstanceId = preparedInstance.Instance.InstanceId,
				ModuleId = Id,
				Hook = Descriptor.Hook,
				TargetKind = PolicyEffectTargetKind.Clan,
				TargetId = id,
				Value = payload.Value,
				DisplayText = DescribeTypedPayload(payload)
			})
			.ToArray();
	}

	public override string DescribeTypedPayload(PartySizeLimitRuntimePayload payload)
	{
		return payload == null
			? string.Empty
			: (payload.Value > 0 ? "+" : string.Empty)
				+ payload.Value.ToString(CultureInfo.InvariantCulture) + " 人";
	}
}
