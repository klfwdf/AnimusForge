[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.ConstructionPerDayEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class ConstructionPerDayPayload : NumericPolicyEffectPayload
{
}

internal sealed class ConstructionPerDayEffectModule : NumericPolicyEffectModuleBase<ConstructionPerDayPayload>, IModelModifierPolicyEffectModule
{
	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "constructionPerDay",
		order: 80,
		legacyIds: new[] { "constructionPowerDailyDelta", "constructionSpeedPercent" },
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Settlement, PolicyEffectTargetKind.Clan, PolicyEffectTargetKind.Kingdom },
		targetKinds: new[] { PolicyEffectTargetKind.Settlement },
		cueTerms: new[] { "建造力", "工程", "基础设施", "修筑", "施工", "工匠" },
		retrievalText: "建造力、工程、建筑、基础设施、修筑、施工、工匠、城防建设、公共工程；每座目标城镇或城堡每日建造力固定点数变化。",
		catalogSummary: "城镇或城堡每日建造力点数变化",
		mainInstruction: "政策若会持续改变目标城镇或城堡的每日建造力、工程能力或施工投入，请在 numericIntent 中说明目标、方向、强弱与理由；不要输出模块 ID 或数值。",
		postprocessRule: "payload 只含一个有限数值。该值是每座目标城镇或城堡每日直接加入原版设施项目的固定建造力点数，正数加快、负数拖慢；不是百分比。",
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Construction,
		executionKind: PolicyEffectExecutionKind.ModelModifier,
		hook: PolicyEffectHook.SettlementConstructionDaily,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PointsPerDay,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Linear,
		payloadSchemaVersion: 1,
		playerDisplayName: "设施建造力",
		editableUnderstandingPrompt: "设施建造力只受明确的设施修建或修缮、工匠、劳力、建材、工程运输、施工组织、停工和工程破坏影响。发展、繁荣、补贴、免税、增税、一般财政投入以及对执行方式的合理补全，都不能自行解释成建设措施。",
		editableEvaluationPrompt: "按每座受影响城镇或城堡每日直接增加或减少的固定建造力判断，不按百分比换算。小规模为 ±20 到 ±60；持续扩充工程资源为 +60 到 +150；全国重大建设为 +300 到 +1000。极端动员、巨额专项投入或玩家明确要求极端强度，且执行路径与资源承诺足够清楚时，可以达到这一范围的 2 到 4 倍，超过 +1000 仍可成立。只有政策直接造成施工受阻、劳力流失、建材短缺或工程体系破坏时，才使用相称负数。",
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public System.Collections.Generic.IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(PolicyEffectPreparedInstance preparedInstance)
		=> PolicyEffectModuleRuntimeAdapters.BuildNumericModelContributions(this, preparedInstance);
}
