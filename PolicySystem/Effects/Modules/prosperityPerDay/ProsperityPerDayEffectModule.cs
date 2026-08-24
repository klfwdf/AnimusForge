[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.ProsperityPerDayEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class ProsperityPerDayPayload : NumericPolicyEffectPayload
{
}

internal sealed class ProsperityPerDayEffectModule : NumericPolicyEffectModuleBase<ProsperityPerDayPayload>, IModelModifierPolicyEffectModule
{
	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "prosperityPerDay",
		order: 10,
		legacyIds: new[] { "prosperityDailyDeltaPerTown" },
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Settlement, PolicyEffectTargetKind.Clan, PolicyEffectTargetKind.Kingdom },
		targetKinds: new[] { PolicyEffectTargetKind.Town },
		cueTerms: new[] { "繁荣", "商业发展", "经济萧条", "市场振兴" },
		retrievalText: "城镇繁荣、商业发展、人口与财富增长、经济萧条、投资发展、市场振兴、长期发展；每座目标城镇每日繁荣变化。",
		catalogSummary: "城镇每日繁荣变化",
		mainInstruction: "政策若会持续改变目标城镇的繁荣、商业活力或总体经济发展，请在 numericIntent 中说明目标、方向、强弱与理由；不要输出模块 ID 或数值。",
		postprocessRule: "payload 只含一个有限数值。该值是每座目标城镇每日结算的繁荣度固定点数，正数增加、负数减少；不得改成整段政策的累计值或百分比。",
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Economy,
		executionKind: PolicyEffectExecutionKind.ModelModifier,
		hook: PolicyEffectHook.SettlementProsperityDaily,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PointsPerDay,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Linear,
		payloadSchemaVersion: 1,
		playerDisplayName: "城镇繁荣变化",
		editableUnderstandingPrompt: "繁荣度主要受贸易、税负、工商业、市场信心、发展投入和战争破坏影响。政策通过这些途径持续改变城镇经济体量时，繁荣度就是直接后果。",
		editableEvaluationPrompt: "按每座受影响城镇的每日变化判断。轻微影响为 ±0.5 到 ±2；普通政策为 ±2 到 ±5；明显经济政策、贸易刺激、税负打击或战时破坏为 ±5 到 ±10；全国重大投资或灾难为 ±12 到 ±24；极端繁荣工程、严重劫掠、封锁、饥荒或国家级经济灾难为 ±24 到 ±36。");

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public System.Collections.Generic.IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(PolicyEffectPreparedInstance preparedInstance)
		=> PolicyEffectModuleRuntimeAdapters.BuildNumericModelContributions(this, preparedInstance);
}
