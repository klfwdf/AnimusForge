using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.SoldierTroopXpEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class SoldierTroopXpPayload : PolicyEffectPayload
{
	[JsonProperty("onceDelta", Required = Required.Always)]
	public int OnceDelta { get; set; }

	[JsonProperty("dailyDelta", Required = Required.Always)]
	public int DailyDelta { get; set; }
}

internal sealed class SoldierTroopXpDailyPayload : PolicyEffectPayload
{
	[JsonProperty("value", Required = Required.Always)]
	public int Value { get; set; }
}

// Composite children are normalized again by their runtime module. Keep this
// compiler-only shape integral so the once module's strict raw contract does
// not receive a JSON floating-point token from NumericPolicyEffectPayload.
internal sealed class SoldierTroopXpOnceChildPayload : PolicyEffectPayload<int>
{
}

internal sealed class SoldierTroopXpEffectModule
	: PolicyEffectModuleBase<SoldierTroopXpPayload>, IPolicyEffectCompositeModule
{
	internal const int MaximumOnceXpPerTroop = 5000;
	internal const int MaximumDailyXpPerTroop = 100;

	private static readonly IReadOnlyCollection<string> RuntimeModules = new[]
	{
		"soldierTroopXpOnce",
		"soldierTroopXpPerDay"
	};

	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "soldierTroopXp",
		order: 140,
		legacyIds: Array.Empty<string>(),
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[]
		{
			PolicyEffectTargetKind.Settlement,
			PolicyEffectTargetKind.Clan,
			PolicyEffectTargetKind.Kingdom,
			PolicyEffectTargetKind.Hero,
		},
		targetKinds: new[] { PolicyEffectTargetKind.Clan },
		cueTerms: new[] { "士兵精锐化", "精锐化", "精锐部队", "家族练兵", "训练领主部队", "驻军训练", "操练部队", "军事训练", "老兵化", "提升兵员素质", "常设教官", "训练制度" },
		retrievalText: "政策通过训练、整编、激励、军纪、福利、军事改革或其他合理机制，使目标家族当前全部正式领主队伍及其城镇、城堡驻军中的普通士兵获得原版兵种经验并更快达到原版升级条件；是否存在这一效果由模型结合完整政策语义、投入和因果关系判断。不直接升级或替换兵种，不作用于英雄、俘虏、民兵或招募增员。",
		catalogSummary: "目标家族领主队伍与驻军普通士兵的一次性或每日经验",
		mainInstruction: "模型应结合完整政策语义、目标、投入、执行机制和合理因果关系，自主判断政策是否让目标家族的领主队伍和封地驻军普通士兵共同获得原版兵种经验；不得仅因出现或未出现军饷、训练、士气、军纪等单个关键词就机械选择或排除此效果。明确的一次集训、集中整编、短期会操或单次改革成果填写 onceDelta，dailyDelta 为 0；常设、每日、长期、持续或制度化收益填写 dailyDelta，onceDelta 为 0。只有明确表达‘先集中整编、后持续操练’或同等前期集中成果与后续持续收益时才同时填写。模糊的‘提高精锐程度’若没有持续制度或两阶段证据，默认只填写 onceDelta；设立常设教官团、永久训练制度或固定训练营则按 dailyDelta。不得解释为 Hero 技能、俘虏、民兵、直接兵种替换、招募或增员；只强化民兵、招募或俘虏时本效果不适用。同一家族的领主队伍与驻军是一次共同训练结果，不拆成多条效果。两项都按每名士兵计算，不按家族、队伍、封地或士兵数量摊薄，游戏端按每栈人数只乘算一次。不得在 onceDelta 与 dailyDelta 之间按政策天数换算。不要输出内部模块 ID 或选择器语法。",
		postprocessRule: "严格输出 {\"onceDelta\": integer, \"dailyDelta\": integer}，禁止未知字段、小数和负数。onceDelta 范围 0～5000，dailyDelta 范围 0～100；两项均为每名合格普通士兵的原版兵种经验，两项均为 0 时省略效果。一次集训只填 onceDelta，常设或每日训练只填 dailyDelta，只有明确‘先集中整编、后持续操练’才同时填写。不得把 dailyDelta 乘政策天数转成 onceDelta，也不得把 onceDelta 除以天数转成 dailyDelta。两项均非零时会在首个执行日叠加，必须同时校准首日总强度和后续每日强度。",
		payloadPromptSchema: CreatePayloadSchema(),
		family: PolicyEffectFamily.Military,
		executionKind: PolicyEffectExecutionKind.Composite,
		hook: PolicyEffectHook.DailyScheduler,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PointsPerDay,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Custom,
		payloadSchemaVersion: 1,
		promptVisible: true,
		displayGroup: "soldierTroopXp",
		playerDisplayName: "士兵精锐化",
		editableUnderstandingPrompt: "士兵精锐化表示政策通过训练、整编、激励、军纪、福利、军事改革或其他合理机制，让目标家族当前全部正式领主队伍以及该家族城镇、城堡驻军中的普通士兵共同获得原版兵种经验。玩家主部队在玩家家族成为目标时也属于正式领主队伍。onceDelta 表达单次成果；dailyDelta 表达从同一生效时点开始、在政策有效期内持续发生的收益。只有政策同时表达前期集中成果和后续持续收益时才同时填写。泛称‘提高精锐程度’但没有持续制度或日常收益措辞时，默认只使用 onceDelta；明确设立常设教官团、永久训练制度或固定训练营时使用 dailyDelta。它不增加英雄技能，不作用于俘虏、民兵、商队、村民、强盗、信使或临时队伍，也不直接替换兵种、升级兵种、招募或增加士兵数量。",
		editableEvaluationPrompt: "两项数值都是领主队伍及驻军中每名合格普通士兵的经验，不是 Party 或 Clan 总 XP，也不因目标家族、队伍、封地或士兵数量多而摊薄。原版 AI 每日训练基线约为普通领主 10+tier*2、家族领袖 15+tier*3 XP/兵/日；常规相邻阶升级成本约为 100/300/550/900/1300/1700。一次性变化：1～99 为象征性，100～150 较弱，151～300 常规，301～600 明显精锐化，601～1200 重大整训，1201～2500 非常强，2501～5000 仅限依据充分的极端政策。每日变化：1～5 轻微，6～15 温和，16～30 明确制度化，31～45 强力常设，46～60 极强，61～80 非常规战时强训，81～100 仅限极端政策；daily=60 已属极强，因为 5 日累计 300、10 日累计 600，并且会叠加原版训练。数值必须结合措辞、投入、组织和持续性判断。不得在两种时间语义之间按政策天数换算。若一次性与每日均非零，两者会在首个执行日叠加。达到 XP 阈值只产生原版可升级人数，不直接升级，仍受健康人数、工资、资金、物品和特殊条件约束。",
		targetProjection: PolicyEffectTargetProjectionKind.None,
		targetRefresh: PolicyEffectTargetRefreshKind.FrozenCanonicalIds,
		allowIndependentClanTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public IReadOnlyCollection<string> RuntimeModuleIds => RuntimeModules;

	protected override bool TryValidateRawPayload(JToken rawPayload, out string error)
	{
		if (rawPayload is not JObject obj
			|| !HasBoundedInteger(obj["onceDelta"], 0, MaximumOnceXpPerTroop)
			|| !HasBoundedInteger(obj["dailyDelta"], 0, MaximumDailyXpPerTroop))
		{
			error = "soldierTroopXp 必须且只能包含 onceDelta 0～5000 与 dailyDelta 0～100 两个整数";
			return false;
		}
		error = string.Empty;
		return true;
	}

	public override bool TryNormalizeTypedPayload(
		SoldierTroopXpPayload payload,
		string scope,
		out SoldierTroopXpPayload normalizedPayload,
		out string error)
	{
		_ = scope;
		normalizedPayload = payload;
		if (!TryValidateEnvelope(payload, out error))
		{
			return false;
		}
		if (payload.OnceDelta < 0 || payload.OnceDelta > MaximumOnceXpPerTroop
			|| payload.DailyDelta < 0 || payload.DailyDelta > MaximumDailyXpPerTroop)
		{
			error = "soldierTroopXp 数值超出允许范围";
			return false;
		}
		error = string.Empty;
		return true;
	}

	public override bool TryApplyTypedFunding(
		SoldierTroopXpPayload payload,
		PolicyEffectFundingContext funding,
		out SoldierTroopXpPayload fundedPayload,
		out string error)
	{
		fundedPayload = null;
		if (!TryNormalizeTypedPayload(payload, string.Empty, out SoldierTroopXpPayload normalized, out error))
		{
			return false;
		}
		double scale = funding?.ResolveScale(ModuleDescriptor.FundingMode) ?? 1d;
		int onceDelta = (int)Math.Round(normalized.OnceDelta * scale, MidpointRounding.AwayFromZero);
		int dailyDelta = (int)Math.Round(normalized.DailyDelta * scale, MidpointRounding.AwayFromZero);
		SoldierTroopXpPayload candidate = new SoldierTroopXpPayload
		{
			ModuleId = ModuleDescriptor.Id,
			SchemaVersion = ModuleDescriptor.PayloadSchemaVersion,
			OnceDelta = onceDelta,
			DailyDelta = dailyDelta
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
		if (context == null || payload is not SoldierTroopXpPayload typed)
		{
			error = "soldierTroopXp 缺少有效的编译上下文或 payload";
			return false;
		}
		if (!TryNormalizeTypedPayload(typed, context.SourceScope, out SoldierTroopXpPayload normalized, out error))
		{
			return false;
		}
		List<PolicyEffectCompositeChild> expanded = new List<PolicyEffectCompositeChild>(2);
		if (normalized.OnceDelta > 0)
		{
			expanded.Add(new PolicyEffectCompositeChild
			{
				ModuleId = "soldierTroopXpOnce",
				Payload = new SoldierTroopXpOnceChildPayload
				{
					ModuleId = "soldierTroopXpOnce",
					SchemaVersion = 1,
					Value = normalized.OnceDelta
				}
			});
		}
		if (normalized.DailyDelta > 0)
		{
			expanded.Add(new PolicyEffectCompositeChild
			{
				ModuleId = "soldierTroopXpPerDay",
				Payload = new SoldierTroopXpDailyPayload
				{
					ModuleId = "soldierTroopXpPerDay",
					SchemaVersion = 1,
					Value = normalized.DailyDelta
				}
			});
		}
		children = expanded;
		error = string.Empty;
		return true;
	}

	public override string DescribeTypedPayload(SoldierTroopXpPayload payload)
	{
		return payload == null
			? string.Empty
			: "一次 " + payload.OnceDelta.ToString(CultureInfo.InvariantCulture)
				+ " / 每日 " + payload.DailyDelta.ToString(CultureInfo.InvariantCulture) + " XP/兵";
	}

	private static bool HasBoundedInteger(JToken token, int minimum, int maximum)
	{
		if (token?.Type != JTokenType.Integer)
		{
			return false;
		}
		try
		{
			long value = token.Value<long>();
			return value >= minimum && value <= maximum;
		}
		catch
		{
			return false;
		}
	}

	private static JObject CreatePayloadSchema()
	{
		return new JObject
		{
			["type"] = "object",
			["required"] = new JArray("onceDelta", "dailyDelta"),
			["properties"] = new JObject
			{
				["onceDelta"] = new JObject
				{
					["type"] = "integer",
					["minimum"] = 0,
					["maximum"] = MaximumOnceXpPerTroop
				},
				["dailyDelta"] = new JObject
				{
					["type"] = "integer",
					["minimum"] = 0,
					["maximum"] = MaximumDailyXpPerTroop
				}
			},
			["additionalProperties"] = false
		};
	}
}
