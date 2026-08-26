[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.FoodPerDayEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class FoodPerDayPayload : NumericPolicyEffectPayload
{
}

internal sealed class FoodPerDayEffectModule : NumericPolicyEffectModuleBase<FoodPerDayPayload>, IModelModifierPolicyEffectModule
{
	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "foodPerDay",
		order: 20,
		legacyIds: new[] { "foodDailyDeltaPerTown" },
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Settlement, PolicyEffectTargetKind.Clan, PolicyEffectTargetKind.Kingdom },
		targetKinds: new[] { PolicyEffectTargetKind.Town },
		cueTerms: new[] { "粮食", "粮仓", "饥荒", "赈济", "军粮" },
		retrievalText: "城镇粮食、粮仓、粮食储备、供给、饥荒、赈济、配给、粮食征收、运输补给；每座目标城镇每日粮食变化。",
		catalogSummary: "城镇每日粮食库存变化",
		mainInstruction: "政策若会持续改变目标城镇粮食储备或日常供给，请在 numericIntent 中说明目标、方向、强弱与理由；不要输出模块 ID 或数值。",
		postprocessRule: "payload 只含一个有限数值。该值是每座目标城镇每日结算的粮食库存固定点数，正数增加、负数减少；不得改成整段政策的累计值。",
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Supply,
		executionKind: PolicyEffectExecutionKind.ModelModifier,
		hook: PolicyEffectHook.TownFoodDaily,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PointsPerDay,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Linear,
		payloadSchemaVersion: 1,
		playerDisplayName: "粮食储备",
		editableUnderstandingPrompt: "粮食库存主要受生产、采购、储备、运输、征收、赈济、军粮消耗、封锁和破坏影响。政策通过这些途径持续改变城镇粮食供给时，粮食就是直接后果。采购、补贴和经济发展本身不代表库存减少；直接从当地库存征走、消耗或破坏粮食才会造成负向变化。",
		editableEvaluationPrompt: "按每座受影响城镇的每日库存变化判断。轻微影响为 ±1 到 ±3；普通粮政、运输、征收或仓储调整为 ±3 到 ±8；大规模赈济、强征或军粮调拨为 ±8 到 ±15；全国重大补给、饥荒、封锁或全面征粮为 ±18 到 ±30；极端灾害或全国级粮食行动为 ±30 到 ±45。",
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public System.Collections.Generic.IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(PolicyEffectPreparedInstance preparedInstance)
		=> PolicyEffectModuleRuntimeAdapters.BuildNumericModelContributions(this, preparedInstance);
}
