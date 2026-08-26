[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.MilitiaPerDayEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class MilitiaPerDayPayload : NumericPolicyEffectPayload
{
}

internal sealed class MilitiaPerDayEffectModule : NumericPolicyEffectModuleBase<MilitiaPerDayPayload>, IModelModifierPolicyEffectModule
{
	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "militiaPerDay",
		order: 60,
		legacyIds: new[] { "militiaDailyDeltaPerTown" },
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Settlement, PolicyEffectTargetKind.Clan, PolicyEffectTargetKind.Kingdom },
		targetKinds: new[] { PolicyEffectTargetKind.Settlement },
		cueTerms: new[] { "民兵", "乡勇", "地方武装", "裁撤民兵" },
		retrievalText: "民兵、地方武装、守备、征召、训练乡勇、裁撤民兵、定居点防卫；每座目标定居点每日民兵变化。",
		catalogSummary: "定居点每日民兵变化",
		mainInstruction: "政策若会持续改变目标定居点民兵招募、训练或裁撤速度，请在 numericIntent 中说明目标、方向、强弱与理由；不要输出模块 ID 或数值。",
		postprocessRule: "payload 只含一个有限数值。该值是每座目标定居点每日结算的民兵人数固定点数，正数增加、负数减少。",
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Military,
		executionKind: PolicyEffectExecutionKind.ModelModifier,
		hook: PolicyEffectHook.SettlementMilitiaDaily,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PointsPerDay,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Linear,
		payloadSchemaVersion: 1,
		playerDisplayName: "民兵",
		editableUnderstandingPrompt: "民兵主要受训练、征召、裁撤、地方防务、士气、粮饷和人口压力影响。政策通过这些途径持续改变定居点实际防务人数时，民兵就是直接后果；增加民兵只有在政策确实征用粮食、劳力或强迫服役时才产生相应代价。",
		editableEvaluationPrompt: "按每座受影响定居点的每日变化判断。轻微影响为 ±0.5 到 ±1.5；普通训练、征召或士气变化为 ±1.5 到 ±4；强力民兵动员或地方防务改革为 ±4 到 ±8；全国战争动员、大规模军事化或民兵溃散为 ±8 到 ±12；超过 ±12 只用于短期极端战争动员或严重军事崩溃。",
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public System.Collections.Generic.IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(PolicyEffectPreparedInstance preparedInstance)
		=> PolicyEffectModuleRuntimeAdapters.BuildNumericModelContributions(this, preparedInstance);
}
