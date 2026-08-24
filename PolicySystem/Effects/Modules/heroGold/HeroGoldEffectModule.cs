using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.HeroGoldEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class HeroGoldPayload : PolicyEffectPayload
{
	[JsonProperty("onceDelta", Required = Required.Always)]
	public int OnceDelta { get; set; }

	[JsonProperty("dailyDelta", Required = Required.Always)]
	public int DailyDelta { get; set; }
}

internal sealed class HeroGoldDeltaPayload : PolicyEffectPayload
{
	[JsonProperty("value", Required = Required.Always)]
	public int Value { get; set; }
}

internal sealed class HeroGoldEffectModule : PolicyEffectModuleBase<HeroGoldPayload>, IPolicyEffectCompositeModule
{
	private static readonly IReadOnlyCollection<string> RuntimeModules = new[]
	{
		"heroGoldNextDayOnce",
		"heroGoldPerDay"
	};

	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "heroGold",
		order: 130,
		legacyIds: Array.Empty<string>(),
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Hero, PolicyEffectTargetKind.Settlement },
		targetKinds: new[] { PolicyEffectTargetKind.Hero },
		cueTerms: new[] { "人物收入", "个人收入", "职业收入", "行业收益", "贸易获利", "领主收入", "军饷", "俸禄", "薪俸", "津贴", "地方财政补助", "个人补助", "个人第纳尔", "发放第纳尔", "玩家获得第纳尔", "发布者获得第纳尔", "个人补贴", "个人税收", "罚没", "赏金" },
		retrievalText: "玩家本人、政策发布者、统治者、领主、家族领袖、要人或原版职业人物因军饷、薪酬、贸易经营、行业收益、补贴、赏罚或罚没产生的个人收入与第纳尔余额变化；地方政策未明确其他个人接收者的军饷、薪俸、津贴或地方财政补助，表示受影响定居点当前所有者家族领袖获得正人物第纳尔。可填写下一游戏日一次性变化与此后每日变化。政策启动费、维护费、建设投入或行政预算不属于人物第纳尔效果。",
		catalogSummary: "人物与原版职业群体的个人第纳尔变化",
		mainInstruction: "政策通过军饷、薪酬、贸易经营、行业收益、补贴、赏罚或罚没等途径合理直接改变某些人物或原版职业群体的个人经济收益时，应由模型自行判断受影响身份、方向和强度，不要求正文逐字写出职业或具体第纳尔金额。地方政策中的军饷、薪俸、津贴或地方财政补助若未明确其他个人接收者，默认是受影响定居点当前所有者家族领袖获得正人物第纳尔；必须保留定居点绑定并选择对应定居点句柄，不得改写成全国领主或士兵人物集合。正文直接点名具体人物或职业群体时才使用对应人物目标。持续收入、俸禄、薪俸、津贴和定期补助默认解释为 dailyDelta，onceDelta 为 0；赏赐、奖金、补发欠饷、罚款、没收等明确单次事件默认解释为 onceDelta，dailyDelta 为 0，正文明确兼有两者时才同时填写。两项均按每位最终人物结算，可正可负，也可为 0；同一领主拥有多座命中定居点仍只结算一次。每项变化都是独立效果，不生成付款方或人物间转账。只有明确涉及领地、城镇、城堡或税制的收入才按领地税收理解。普通政策花费、启动费、维护费、建设投入和行政预算不得解释为人物第纳尔变化。不要输出内部模块 ID 或选择器语法。",
		postprocessRule: "输出结构必须是 {\"onceDelta\": integer, \"dailyDelta\": integer}，禁止未知字段。正文不必给出具体金额；模型必须按语义强弱选择整数。onceDelta 在政策通过后的下一个游戏日结算一次；dailyDelta 从同一天起每日结算。地方军饷或补助收益必须为正数，政策成本不得重复映射成负人物第纳尔。两项均为 0 时省略该效果。",
		payloadPromptSchema: CreatePayloadSchema(),
		family: PolicyEffectFamily.Fiscal,
		executionKind: PolicyEffectExecutionKind.Composite,
		hook: PolicyEffectHook.DailyScheduler,
		aggregation: PolicyEffectAggregationKind.IntegerDelta,
		valueUnit: PolicyEffectValueUnit.GoldPerDay,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Custom,
		payloadSchemaVersion: 1,
		promptVisible: true,
		displayGroup: "heroGold",
		playerDisplayName: "人物第纳尔",
		editableUnderstandingPrompt: "人物第纳尔是英雄个人持有的资金，不是家族、部队或定居点的虚拟金库。玩家本人和政策发布者本人也可以直接成为人物目标。政策通过军饷、薪酬、贸易经营、行业收益、补贴、赏罚或罚没等途径合理直接改变人物个人经济收益时，人物第纳尔就是实际后果；模型应自行判断受影响的人物或原版职业群体，不要求政策逐字写出职业或具体金额。地方政策未明确其他个人接收者的军饷、薪俸、津贴或地方财政补助，默认由受影响定居点当前所有者家族领袖获得，应保留定居点绑定目标；直接点名具体人物或职业群体时才使用人物目标。持续收入、俸禄、薪俸、津贴和定期补助通常属于持续变化；赏赐、奖金、补发欠饷、罚款和没收通常属于单次变化。只有正文明确涉及领地、城镇、城堡或税制时才按领地税收处理。政策发布与维护费用、建设投资和一般行政预算不属于此效果。",
		editableEvaluationPrompt: "一次性变化在政策通过后的下一游戏日结算，每日变化在政策有效期内逐日结算。数值按每位最终人物计算，不是群体总额；正数为独立收入或补贴，负数为个人负担、罚款或罚没，不生成人物间转账。正文未给金额时，根据措辞强度、政策覆盖面和资源承诺选择具体整数，不要机械取区间中值：轻微或有限的每日收入变化通常为每人 ±50 到 ±150；常规且明确的收入政策为 ±150 到 ±500；重大或慷慨的长期政策为 ±500 到 ±1500；只有极端且有充分政策依据时才达到 ±1500 到 ±5000。一次性小额赏赐或罚款通常为每人 ±500 到 ±2000，显著处置为 ±2000 到 ±10000，重大赏赐或罚没为 ±10000 到 ±50000。泛称“提高领主收入”应给出正的每日变化且不附加一次性变化；不得因为目标人数多就把群体总额平均到每人，也不得把政策成本重复计入人物余额变化。",
		targetProjection: PolicyEffectTargetProjectionKind.SettlementOwnerClanLeader);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public IReadOnlyCollection<string> RuntimeModuleIds => RuntimeModules;

	protected override bool TryValidateRawPayload(JToken rawPayload, out string error)
	{
		if (!HeroGoldPayloadValidation.HasIntegerProperty(rawPayload, "onceDelta")
			|| !HeroGoldPayloadValidation.HasIntegerProperty(rawPayload, "dailyDelta"))
		{
			error = "heroGold 的 onceDelta 与 dailyDelta 必须是 32 位整数";
			return false;
		}
		error = string.Empty;
		return true;
	}

	public override bool TryNormalizeTypedPayload(
		HeroGoldPayload payload,
		string scope,
		out HeroGoldPayload normalizedPayload,
		out string error)
	{
		normalizedPayload = payload;
		if (!TryValidateEnvelope(payload, out error))
		{
			return false;
		}
		if (payload.OnceDelta == int.MinValue || payload.DailyDelta == int.MinValue)
		{
			error = "heroGold 不接受 int.MinValue";
			return false;
		}
		error = string.Empty;
		return true;
	}

	public override bool TryApplyTypedFunding(
		HeroGoldPayload payload,
		PolicyEffectFundingContext funding,
		out HeroGoldPayload fundedPayload,
		out string error)
	{
		fundedPayload = null;
		if (!TryNormalizeTypedPayload(payload, string.Empty, out HeroGoldPayload normalized, out error))
		{
			return false;
		}
		double scale = funding?.ResolveScale(ModuleDescriptor.FundingMode) ?? 1d;
		if (!HeroGoldPayloadValidation.TryScale(normalized.OnceDelta, scale, out int onceDelta)
			|| !HeroGoldPayloadValidation.TryScale(normalized.DailyDelta, scale, out int dailyDelta))
		{
			error = "heroGold funding scale 结果超出 32 位整数范围";
			return false;
		}
		HeroGoldPayload candidate = new HeroGoldPayload
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
		if (context == null || payload is not HeroGoldPayload typed)
		{
			error = "heroGold 缺少有效的编译上下文或 payload";
			return false;
		}
		if (!TryNormalizeTypedPayload(typed, context.SourceScope, out HeroGoldPayload normalized, out error))
		{
			return false;
		}

		List<PolicyEffectCompositeChild> expanded = new List<PolicyEffectCompositeChild>(2);
		if (normalized.OnceDelta != 0)
		{
			expanded.Add(new PolicyEffectCompositeChild
			{
				ModuleId = "heroGoldNextDayOnce",
				Payload = new HeroGoldDeltaPayload
				{
					ModuleId = "heroGoldNextDayOnce",
					SchemaVersion = 1,
					Value = normalized.OnceDelta
				}
			});
		}
		if (normalized.DailyDelta != 0)
		{
			expanded.Add(new PolicyEffectCompositeChild
			{
				ModuleId = "heroGoldPerDay",
				Payload = new HeroGoldDeltaPayload
				{
					ModuleId = "heroGoldPerDay",
					SchemaVersion = 1,
					Value = normalized.DailyDelta
				}
			});
		}
		children = expanded;
		error = string.Empty;
		return true;
	}

	public override string DescribeTypedPayload(HeroGoldPayload payload)
	{
		return payload == null
			? string.Empty
			: "一次 " + payload.OnceDelta.ToString(CultureInfo.InvariantCulture)
				+ " / 每日 " + payload.DailyDelta.ToString(CultureInfo.InvariantCulture);
	}

	private static JObject CreatePayloadSchema()
	{
		return new JObject
		{
			["type"] = "object",
			["required"] = new JArray("onceDelta", "dailyDelta"),
			["properties"] = new JObject
			{
				["onceDelta"] = new JObject { ["type"] = "integer" },
				["dailyDelta"] = new JObject { ["type"] = "integer" }
			},
			["additionalProperties"] = false
		};
	}
}

internal static class HeroGoldPayloadValidation
{
	internal static bool HasIntegerProperty(JToken rawPayload, string propertyName)
	{
		JToken value = rawPayload?[propertyName];
		if (value?.Type != JTokenType.Integer)
		{
			return false;
		}
		try
		{
			long number = value.Value<long>();
			return number >= int.MinValue && number <= int.MaxValue;
		}
		catch
		{
			return false;
		}
	}

	internal static bool TryScale(int value, double scale, out int result)
	{
		result = 0;
		if (double.IsNaN(scale) || double.IsInfinity(scale))
		{
			return false;
		}
		double rounded = Math.Round(value * scale, MidpointRounding.AwayFromZero);
		if (rounded < int.MinValue || rounded > int.MaxValue || rounded == int.MinValue)
		{
			return false;
		}
		result = (int)rounded;
		return true;
	}
}
