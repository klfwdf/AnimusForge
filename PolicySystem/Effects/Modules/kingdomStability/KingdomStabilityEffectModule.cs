using System;
using System.Collections.Generic;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.KingdomStabilityEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class KingdomStabilityPayload : NumericPolicyEffectPayload
{
}

internal sealed class KingdomStabilityEffectModule : NumericPolicyEffectModuleBase<KingdomStabilityPayload>, IPolicyEffectCompositeModule
{
	private static readonly IReadOnlyCollection<string> RuntimeModules = new[]
	{
		"kingdomStabilityNextDayOnce"
	};

	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "kingdomStability",
		order: 120,
		legacyIds: Array.Empty<string>(),
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Kingdom },
		targetKinds: new[] { PolicyEffectTargetKind.Kingdom },
		cueTerms: new[] { "稳定度", "政局稳定", "中央权威", "国家动荡", "叛乱风险", "政治危机" },
		retrievalText: "王国稳定度、政局稳定、中央权威、国家动荡、叛乱风险、制度冲击和政治危机；政策通过后的下一游戏日一次性结算。",
		catalogSummary: "下一游戏日一次性王国稳定度变化",
		mainInstruction: "政策若会直接改变王国层面的政治稳定、中央权威或动荡风险，请给出一次性稳定度变化。该效果在政策通过后的下一个游戏日结算，不按城镇数量叠加；地方政策不可用。",
		postprocessRule: "payload 只含一个有限数值，最终按整数稳定度点在下一游戏日结算一次，正数提高、负数降低；不随持续天数重复，也不按城镇数量叠加，地方作用域禁止使用。",
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Stability,
		executionKind: PolicyEffectExecutionKind.Composite,
		hook: PolicyEffectHook.DailyScheduler,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PointsOnce,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Linear,
		payloadSchemaVersion: 1,
		promptVisible: true,
		displayGroup: "kingdomStability",
		playerDisplayName: "王国稳定",
		editableUnderstandingPrompt: "王国稳定度表示国家级政治结构与统治秩序，主要受民众信心、财政威望、封臣信任、王权合法性、战争胜败、贵族利益、国内分裂风险和决定性胜利影响。纯行政或普通地方数值变化通常不是王国稳定后果；正负方向必须有直接政治因果。",
		editableEvaluationPrompt: "稳定提高为正、动荡加剧为负，并按整数稳定度点判断。明显改变民众信心、财政威望、封臣信任、王权合法性或分裂风险时为 ±4 到 ±7；重大改革、全国动员、广泛贵族冲突或严重政治危机为 ±7 到 ±14；内战边缘、国家存亡、统治体系崩溃或决定性胜利为 ±14 到 ±22。变化在政策通过后的下一游戏日只结算一次，不按城镇数量叠加。",
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public IReadOnlyCollection<string> RuntimeModuleIds => RuntimeModules;

	protected override bool TryNormalizeNumericValue(float rawValue, string scope, out float normalizedValue, out string error)
	{
		normalizedValue = rawValue;
		if (string.Equals(scope, PolicyEffectScopes.Local, StringComparison.OrdinalIgnoreCase))
		{
			error = "kingdomStability 不支持地方政策作用域";
			return false;
		}
		if (float.IsNaN(rawValue) || float.IsInfinity(rawValue))
		{
			error = "kingdomStability 的 value 必须是有限数字";
			return false;
		}
		normalizedValue = (float)Math.Round(rawValue, MidpointRounding.AwayFromZero);
		error = string.Empty;
		return true;
	}

	public bool TryExpand(
		PolicyEffectCompileContext context,
		PolicyEffectPayload payload,
		out IReadOnlyList<PolicyEffectCompositeChild> children,
		out string error)
	{
		children = Array.Empty<PolicyEffectCompositeChild>();
		if (context == null || !(payload is KingdomStabilityPayload typed))
		{
			error = "kingdomStability 缺少有效的编译上下文或 payload";
			return false;
		}
		if (!TryNormalizeTypedPayload(typed, context.SourceScope, out KingdomStabilityPayload normalized, out error))
		{
			return false;
		}
		if (normalized.Value == 0f)
		{
			children = Array.Empty<PolicyEffectCompositeChild>();
			error = string.Empty;
			return true;
		}
		children = new[]
		{
			new PolicyEffectCompositeChild
			{
				ModuleId = "kingdomStabilityNextDayOnce",
				Payload = new KingdomStabilityNextDayOncePayload
				{
					ModuleId = "kingdomStabilityNextDayOnce",
					SchemaVersion = 1,
					Value = normalized.Value
				}
			}
		};
		error = string.Empty;
		return true;
	}
}
