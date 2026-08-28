[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.HearthPerDayEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class HearthPerDayPayload : NumericPolicyEffectPayload
{
}

internal sealed class HearthPerDayEffectModule : NumericPolicyEffectModuleBase<HearthPerDayPayload>, IModelModifierPolicyEffectModule
{
	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "hearthPerDay",
		order: 30,
		legacyIds: new[] { "hearthDailyDeltaPerVillage" },
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Settlement, PolicyEffectTargetKind.Clan, PolicyEffectTargetKind.Kingdom },
		targetKinds: new[] { PolicyEffectTargetKind.Village },
		cueTerms: new[] { "户数", "农村人口", "移民安置", "开垦", "人口流失" },
		retrievalText: "村庄户数、村民人口、农村发展、移民安置、开垦、人口流失、村庄繁荣；合法父级目标范围内附属村庄的每日户数变化。",
		catalogSummary: "村庄每日户数变化",
		mainInstruction: "政策若会持续改变目标城镇或城堡覆盖范围内附属村庄的户数、农村人口或开垦规模，请在 numericIntent 中用自然语言说明目标范围、方向、强弱与理由；村庄没有独立目标，不要输出模块 ID 或数值。",
		postprocessRule: "payload 只含一个有限数值。该值由合法父级目标确定性展开到其附属村庄，并按每个村庄每日户数固定点数结算，正数增加、负数减少；村庄不接受独立目标句柄。",
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Population,
		executionKind: PolicyEffectExecutionKind.ModelModifier,
		hook: PolicyEffectHook.VillageHearthDaily,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PointsPerDay,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Linear,
		payloadSchemaVersion: 1,
		playerDisplayName: "村庄户数",
		editableUnderstandingPrompt: "村庄户数代表人口和劳力，主要受劳动力、安全、徭役、安置、迁徙、逃亡、开垦、屠掠和灾荒影响。政策通过这些途径持续改变相关城镇或城堡附属村庄的人口时，户数就是直接后果。",
		editableEvaluationPrompt: "按每个受影响村庄的每日变化判断。轻微影响为 ±0.1 到 ±0.5；普通徭役、安置、劳力恢复或迁徙为 ±0.5 到 ±1.5；强力移民、战乱逃亡、重税压迫或大规模劳役为 ±2 到 ±4；全国人口扶持、屠掠、灾荒或强制迁徙为 ±4 到 ±7；极端人口变化或灾难为 ±7 到 ±10。",
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public System.Collections.Generic.IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(PolicyEffectPreparedInstance preparedInstance)
		=> PolicyEffectModuleRuntimeAdapters.BuildNumericModelContributions(this, preparedInstance);
}
