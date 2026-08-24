[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.SecurityPerDayEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class SecurityPerDayPayload : NumericPolicyEffectPayload
{
}

internal sealed class SecurityPerDayEffectModule : NumericPolicyEffectModuleBase<SecurityPerDayPayload>, IModelModifierPolicyEffectModule
{
	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "securityPerDay",
		order: 50,
		legacyIds: new[] { "securityDailyDeltaPerTown" },
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Settlement, PolicyEffectTargetKind.Clan, PolicyEffectTargetKind.Kingdom },
		targetKinds: new[] { PolicyEffectTargetKind.Town },
		cueTerms: new[] { "治安", "犯罪", "巡逻", "盗匪", "执法", "腐败" },
		retrievalText: "城镇治安、犯罪、巡逻、盗匪、执法、秩序、腐败、社会安全；每座目标城镇每日治安变化。",
		catalogSummary: "城镇每日治安变化",
		mainInstruction: "政策若会持续改变目标城镇治安、犯罪控制或社会秩序，请在 numericIntent 中说明目标、方向、强弱与理由；不要输出模块 ID 或数值。",
		postprocessRule: "payload 只含一个有限数值。该值是每座目标城镇每日结算的治安度固定点数，正数改善、负数恶化。",
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Security,
		executionKind: PolicyEffectExecutionKind.ModelModifier,
		hook: PolicyEffectHook.TownSecurityDaily,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PointsPerDay,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Linear,
		payloadSchemaVersion: 1,
		playerDisplayName: "城镇治安",
		editableUnderstandingPrompt: "治安度主要受匪患、巡逻、执法、公正、腐败、军管、镇压和地方秩序影响。政策通过这些途径持续改变地方安全时，治安度就是直接后果。",
		editableEvaluationPrompt: "按每座受影响城镇的每日变化判断。轻微影响为 ±0.1 到 ±0.3；普通巡逻、执法、腐败整顿或匪患变化为 ±0.3 到 ±0.7；强力治安运动、军管、边境混乱或匪患爆发为 ±0.8 到 ±1.5；严重失序或高压镇压为 ±1.5 到 ±2.5；超过 ±2.5 只用于短期极端内乱、血腥镇压或大规模匪患。");

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public System.Collections.Generic.IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(PolicyEffectPreparedInstance preparedInstance)
		=> PolicyEffectModuleRuntimeAdapters.BuildNumericModelContributions(this, preparedInstance);
}
