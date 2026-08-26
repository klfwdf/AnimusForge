[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.LoyaltyPerDayEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class LoyaltyPerDayPayload : NumericPolicyEffectPayload
{
}

internal sealed class LoyaltyPerDayEffectModule : NumericPolicyEffectModuleBase<LoyaltyPerDayPayload>, IModelModifierPolicyEffectModule
{
	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "loyaltyPerDay",
		order: 40,
		legacyIds: new[] { "loyaltyDailyDeltaPerTown" },
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Settlement, PolicyEffectTargetKind.Clan, PolicyEffectTargetKind.Kingdom },
		targetKinds: new[] { PolicyEffectTargetKind.Town },
		cueTerms: new[] { "忠诚", "民心", "政治认同", "文化矛盾", "叛乱倾向" },
		retrievalText: "城镇忠诚、民心、认同、压迫、自治、文化矛盾、叛乱倾向、安抚民众；每座目标城镇每日忠诚变化。",
		catalogSummary: "城镇每日忠诚变化",
		mainInstruction: "政策若会持续改变目标城镇民众忠诚、政治认同或反抗情绪，请在 numericIntent 中说明目标、方向、强弱与理由；不要输出模块 ID 或数值。",
		postprocessRule: "payload 只含一个有限数值。该值是每座目标城镇每日结算的忠诚度固定点数，正数提高、负数降低；不得先计算整个政策周期再摊回每日。",
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Governance,
		executionKind: PolicyEffectExecutionKind.ModelModifier,
		hook: PolicyEffectHook.TownLoyaltyDaily,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PointsPerDay,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Linear,
		payloadSchemaVersion: 1,
		playerDisplayName: "城镇忠诚",
		editableUnderstandingPrompt: "忠诚度主要受公平感、文化认同、自治、压迫、恐惧、荣誉和利益分配影响。政策直接持续作用于城镇民心、政治认同或反抗情绪时，忠诚度就是直接后果；经济繁荣本身不能代替政治认同。",
		editableEvaluationPrompt: "按每座受影响城镇的每日实际变化判断，不先计算整个持续期再除以天数。轻微影响为 ±0.1 到 ±0.4；普通安抚、税负、公平感、文化待遇或自治调整为 ±0.4 到 ±1.2；重大改革、强力压迫、重税减免、广泛赈济、荣誉优待或明显歧视为 ±2 到 ±6；极端暴政、重大救民、严重背叛、系统性迫害或接近叛乱级刺激为 ±4 到 ±12。",
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public System.Collections.Generic.IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(PolicyEffectPreparedInstance preparedInstance)
		=> PolicyEffectModuleRuntimeAdapters.BuildNumericModelContributions(this, preparedInstance);
}
