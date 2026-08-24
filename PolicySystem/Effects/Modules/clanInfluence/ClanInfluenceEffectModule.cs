using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.ClanInfluenceEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class ClanInfluencePayload : PolicyEffectPayload
{
	[JsonProperty("onceDelta", Required = Required.Always)]
	public float OnceDelta { get; set; }

	[JsonProperty("dailyDelta", Required = Required.Always)]
	public float DailyDelta { get; set; }
}

internal sealed class ClanInfluenceEffectModule : PolicyEffectModuleBase<ClanInfluencePayload>, IPolicyEffectCompositeModule
{
	private static readonly IReadOnlyCollection<string> RuntimeModules = new[]
	{
		"clanInfluenceNextDayOnce",
		"clanInfluencePerDay"
	};

	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "clanInfluence",
		order: 100,
		legacyIds: Array.Empty<string>(),
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Settlement, PolicyEffectTargetKind.Clan, PolicyEffectTargetKind.Kingdom, PolicyEffectTargetKind.Hero },
		targetKinds: new[] { PolicyEffectTargetKind.Clan },
		cueTerms: new[] { "影响力", "贵族权势", "领主权力", "中央集权", "削藩", "王权" },
		retrievalText: "家族影响力、贵族权势、领主权力、中央集权、削弱封臣、加强王权；可同时给出下一游戏日的一次性变化与此后每日变化。",
		catalogSummary: "家族影响力：一次性与每日变化",
		mainInstruction: "政策若会改变发布者家族或目标王国家族的影响力，请分别填写 onceDelta 与 dailyDelta；两者都允许为 0。一次性部分在政策通过后的下一个游戏日结算，每日部分也从该日开始。中央集权通常给其他家族负值、发布者家族正值。不要输出内部模块 ID。",
		postprocessRule: "输出结构必须是 {\"onceDelta\": number, \"dailyDelta\": number}。数值必须有限，可正可负，也可为 0。onceDelta 只在下一个游戏日执行一次；dailyDelta 在政策有效期间每日执行，不回收历史累计。若两项均为 0，应省略该效果。",
		payloadPromptSchema: CreatePayloadSchema(),
		family: PolicyEffectFamily.Governance,
		executionKind: PolicyEffectExecutionKind.Composite,
		hook: PolicyEffectHook.DailyScheduler,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PointsPerDay,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Custom,
		payloadSchemaVersion: 1,
		promptVisible: true,
		displayGroup: "clanInfluence",
		playerDisplayName: "家族影响力",
		editableUnderstandingPrompt: "家族影响力代表发布者家族或目标王国家族的政治资本、贵族权势、中央集权程度和封臣地位。政策直接改变这些权力关系时，家族影响力就是实际后果；中央集权通常削弱其他家族并加强发布者家族，但仍须符合政策原文和执行路径。",
		editableEvaluationPrompt: "即时变化在政策通过后的下一游戏日结算一次；持续变化从同一天起在政策有效期间每日结算。两部分可以分别为正、负或不发生变化，强度应与权力转移、利益得失和持续执行力度相称。");

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public IReadOnlyCollection<string> RuntimeModuleIds => RuntimeModules;

	public override bool TryNormalizeTypedPayload(
		ClanInfluencePayload payload,
		string scope,
		out ClanInfluencePayload normalizedPayload,
		out string error)
	{
		normalizedPayload = payload;
		if (!TryValidateEnvelope(payload, out error))
		{
			return false;
		}
		if (!IsFinite(payload.OnceDelta) || !IsFinite(payload.DailyDelta))
		{
			error = "clanInfluence 的 onceDelta 与 dailyDelta 必须是有限数字";
			return false;
		}
		error = string.Empty;
		return true;
	}

	public override bool TryApplyTypedFunding(
		ClanInfluencePayload payload,
		PolicyEffectFundingContext funding,
		out ClanInfluencePayload fundedPayload,
		out string error)
	{
		fundedPayload = null;
		if (!TryValidateEnvelope(payload, out error))
		{
			return false;
		}
		float scale = funding?.ResolveScale(ModuleDescriptor.FundingMode) ?? 1f;
		ClanInfluencePayload candidate = new ClanInfluencePayload
		{
			ModuleId = ModuleDescriptor.Id,
			SchemaVersion = ModuleDescriptor.PayloadSchemaVersion,
			OnceDelta = payload.OnceDelta * scale,
			DailyDelta = payload.DailyDelta * scale
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
		if (context == null || !(payload is ClanInfluencePayload typed))
		{
			error = "clanInfluence 缺少有效的编译上下文或 payload";
			return false;
		}
		if (!TryNormalizeTypedPayload(typed, context.SourceScope, out ClanInfluencePayload normalized, out error))
		{
			return false;
		}

		List<PolicyEffectCompositeChild> expanded = new List<PolicyEffectCompositeChild>(2);
		if (normalized.OnceDelta != 0f)
		{
			expanded.Add(new PolicyEffectCompositeChild
			{
				ModuleId = "clanInfluenceNextDayOnce",
				Payload = new ClanInfluenceNextDayOncePayload
				{
					ModuleId = "clanInfluenceNextDayOnce",
					SchemaVersion = 1,
					Value = normalized.OnceDelta
				}
			});
		}
		if (normalized.DailyDelta != 0f)
		{
			expanded.Add(new PolicyEffectCompositeChild
			{
				ModuleId = "clanInfluencePerDay",
				Payload = new ClanInfluencePerDayPayload
				{
					ModuleId = "clanInfluencePerDay",
					SchemaVersion = 1,
					Value = normalized.DailyDelta
				}
			});
		}

		children = expanded;
		error = string.Empty;
		return true;
	}

	public override string DescribeTypedPayload(ClanInfluencePayload payload)
	{
		return payload == null
			? string.Empty
			: "一次 " + payload.OnceDelta.ToString("0.###", CultureInfo.InvariantCulture)
				+ " / 每日 " + payload.DailyDelta.ToString("0.###", CultureInfo.InvariantCulture);
	}

	private static JObject CreatePayloadSchema()
	{
		return new JObject
		{
			["type"] = "object",
			["required"] = new JArray("onceDelta", "dailyDelta"),
			["properties"] = new JObject
			{
				["onceDelta"] = new JObject { ["type"] = "number" },
				["dailyDelta"] = new JObject { ["type"] = "number" }
			},
			["additionalProperties"] = false
		};
	}

	private static bool IsFinite(float value)
	{
		return !float.IsNaN(value) && !float.IsInfinity(value);
	}
}
