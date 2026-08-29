using System;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.VillageProductionPctEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class VillageProductionPctPayload : NumericPolicyEffectPayload
{
}

internal sealed class VillageProductionPctEffectModule
	: NumericPolicyEffectModuleBase<VillageProductionPctPayload>, IModelModifierPolicyEffectModule
{
	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "villageProductionPct",
		order: 146,
		legacyIds: Array.Empty<string>(),
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[]
		{
			PolicyEffectTargetKind.Settlement,
			PolicyEffectTargetKind.Clan,
			PolicyEffectTargetKind.Kingdom
		},
		targetKinds: new[] { PolicyEffectTargetKind.Village },
		cueTerms: new[] { "村庄产量", "商品产量", "农产品产量", "畜牧产量", "马匹繁育", "粮食产出" },
		retrievalText: "政策直接提高或降低目标城镇、城堡行政附属村庄的全部原版商品产量，包括村庄类型产物、马匹等牲畜以及通用谷物、奶酪和黄油批次；不改变城镇粮食库存、工坊生产速度或建筑建造力。商品仍按原版村庄库存、村民运输和 TradeBound 市场机制流通。",
		catalogSummary: "行政附属村庄全部原版商品产量的相对变化",
		mainInstruction: "只有政策会直接改变目标城镇或城堡行政附属村庄的农业、畜牧、马匹繁育、原料或粮食商品产量时，才填写相对原版村庄商品产量的百分比变化。它同时影响村庄类型产物（包括马匹和其他牲畜）以及原版通用谷物、奶酪、黄油批次；不表示直接增加城镇粮食库存，不改变工坊生产速度或建筑建造力。商品仍按原版 TradeBound 物流进入市场，村庄没有独立目标句柄。不要输出模块 ID。",
		postprocessRule: "payload 必须严格为 {\"value\": number}，value 必须是有限数字，表示相对原版村庄商品产量最终结果的百分比变化。+20 表示原版产量乘 120%，+100 表示乘 200%，-100 及更低的叠加结果会使产量归零；不是每日增加固定件数。多个生效政策先加总百分比，再统一缩放村庄类型产物和通用粮食批次。该值由合法父级目标确定性展开到行政附属村庄，不直接向任何城镇库存添加商品。",
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Economy,
		executionKind: PolicyEffectExecutionKind.ModelModifier,
		hook: PolicyEffectHook.VillageGoodsProduction,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.RelativePercent,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Linear,
		payloadSchemaVersion: 1,
		playerDisplayName: "村庄商品产量",
		editableUnderstandingPrompt: "村庄商品产量表示行政附属村庄通过原版产量模型生成的全部实际商品，包括村庄类型产物、马匹和其他牲畜，以及通用谷物、奶酪和黄油批次。只有政策直接改变农业、畜牧、繁育、采集、加工原料或粮食产出时才选择；不要把城镇粮食库存、工坊生产速度、建筑建造力或单纯贸易流通误判为村庄产量。政策只改变产量模型，商品仍先进入村庄库存，并按原版村民运输和 TradeBound 市场机制流通。",
		editableEvaluationPrompt: "数值是相对当前原版村庄商品产量最终结果的百分比变化，不是固定件数。村庄基础日产量较低且实际入库会随机取整，因此正向政策应给出足以形成体感的幅度，不要把明确有效的政策只评为 +5% 到 +15%。+20 表示乘 1.2，+50 表示乘 1.5，+100 表示乘 2，-100 表示归零。轻微农具、道路维护或季节性支持通常为 +20% 到 +40%，负向为 -10% 到 -20%；明确的农业、畜牧或繁育制度约为 +40% 到 +80%，负向为 -20% 到 -40%；大规模灌溉、土地改革或强力生产改革约为 +80% 到 +150%，强制征收、严重劳力流失或灾害约为 -40% 到 -70%；足以改变地区经济结构的极端正向政策可达 +150% 到 +250%，毁灭性灾害或全面掠夺为 -70% 到 -100%。多个政策先加总，倍率下限为 0，不设置额外正向硬上限，但负向判定不得低于 -100%。",
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public System.Collections.Generic.IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(
		PolicyEffectPreparedInstance preparedInstance)
		=> PolicyEffectModuleRuntimeAdapters.BuildNumericModelContributions(this, preparedInstance);
}
