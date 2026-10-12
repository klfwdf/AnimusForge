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
		cueTerms: new[] { PolicyCompatibilityTexts.Get("Module_militiaPerDay_Original_Closeout_0"), PolicyCompatibilityTexts.Get("Module_militiaPerDay_Original_Closeout_1"), PolicyCompatibilityTexts.Get("Module_militiaPerDay_Original_Closeout_2"), PolicyCompatibilityTexts.Get("Module_militiaPerDay_Original_Closeout_3"), "militia", "local defense", "conscription", "militia training" },
		retrievalText: PolicyGenerationPromptTexts.Get("Module_militiaPerDay_retrievalText", "Militia, local armed forces, defense, conscription, local militia training, disbanding militia and settlement defense; daily militia changes per target settlement."),
		catalogSummary: PolicyGenerationPromptTexts.Get("Module_militiaPerDay_catalogSummary", "Daily settlement militia change"),
		mainInstruction: PolicyGenerationPromptTexts.Get("Module_militiaPerDay_mainInstruction", "If the policy continuously changes target settlements’ militia recruitment, training or disbanding speed, explain the targets, direction, intensity and reasons in numericIntent; do not output module IDs or numbers."),
		postprocessRule: PolicyGenerationPromptTexts.Get("Module_militiaPerDay_postprocessRule", "payload contains only one finite numeric value. It is the fixed militia headcount points settled daily per target settlement: positive adds militia, negative removes it."),
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Military,
		executionKind: PolicyEffectExecutionKind.ModelModifier,
		hook: PolicyEffectHook.SettlementMilitiaDaily,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PointsPerDay,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Linear,
		payloadSchemaVersion: 1,
		playerDisplayName: PolicyCompatibilityTexts.Get("Module_militiaPerDay_playerDisplayName"),
		editableUnderstandingPrompt: PolicyGenerationPromptTexts.Get("Module_militiaPerDay_editableUnderstandingPrompt", "Militia is mainly affected by training, levies, disbanding, local defense, morale, food and pay, and population pressures. When policies continuously change actual settlement defense headcounts through these mechanisms, militia is a direct consequence. Increasing militia incurs corresponding costs only when the policy actually requisitions food or labor or forces military service."),
		editableEvaluationPrompt: PolicyGenerationPromptTexts.Get("Module_militiaPerDay_editableEvaluationPrompt", "Judge daily changes per affected settlement. Mild impact: ±0.5 to ±1.5; regular training, levies or morale changes: ±1.5 to ±4; strong militia mobilization or local-defense reforms: ±4 to ±8; nationwide war mobilization, large-scale militarization or militia collapse: ±8 to ±12. More than ±12 is reserved for brief extreme war mobilization or severe military collapse."),
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public System.Collections.Generic.IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(PolicyEffectPreparedInstance preparedInstance)
		=> PolicyEffectModuleRuntimeAdapters.BuildNumericModelContributions(this, preparedInstance);
}
