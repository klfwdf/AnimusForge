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
		cueTerms: new[] { PolicyCompatibilityTexts.Get("Module_loyaltyPerDay_Original_Closeout_0"), PolicyCompatibilityTexts.Get("Module_loyaltyPerDay_Original_Closeout_1"), PolicyCompatibilityTexts.Get("Module_loyaltyPerDay_Original_Closeout_2"), PolicyCompatibilityTexts.Get("Module_loyaltyPerDay_Original_Closeout_3"), PolicyCompatibilityTexts.Get("Module_loyaltyPerDay_Original_Closeout_4"), "loyalty", "popular support", "oppression", "autonomy", "rebellion" },
		retrievalText: PolicyGenerationPromptTexts.Get("Module_loyaltyPerDay_retrievalText", "Town loyalty, popular support, identification, oppression, autonomy, cultural conflict, rebellion tendencies and pacifying the population; daily loyalty changes per target town."),
		catalogSummary: PolicyGenerationPromptTexts.Get("Module_loyaltyPerDay_catalogSummary", "Daily town loyalty change"),
		mainInstruction: PolicyGenerationPromptTexts.Get("Module_loyaltyPerDay_mainInstruction", "If the policy continuously changes target towns’ popular loyalty, political identification or resistance, explain the targets, direction, intensity and reasons in numericIntent; do not output module IDs or numbers."),
		postprocessRule: PolicyGenerationPromptTexts.Get("Module_loyaltyPerDay_postprocessRule", "payload contains only one finite numeric value. It is the fixed loyalty points settled daily per target town: positive raises loyalty, negative lowers it. Do not calculate the entire policy period first and then distribute it across days."),
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Governance,
		executionKind: PolicyEffectExecutionKind.ModelModifier,
		hook: PolicyEffectHook.TownLoyaltyDaily,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PointsPerDay,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Linear,
		payloadSchemaVersion: 1,
		playerDisplayName: PolicyCompatibilityTexts.Get("Module_loyaltyPerDay_playerDisplayName"),
		editableUnderstandingPrompt: PolicyGenerationPromptTexts.Get("Module_loyaltyPerDay_editableUnderstandingPrompt", "Loyalty is mainly affected by perceived fairness, cultural identification, autonomy, oppression, fear, honor and the distribution of interests. When policies directly and continuously affect towns' popular support, political identification or resistance, loyalty is a direct consequence. Economic prosperity alone cannot substitute for political identification."),
		editableEvaluationPrompt: PolicyGenerationPromptTexts.Get("Module_loyaltyPerDay_editableEvaluationPrompt", "Judge actual daily changes per affected town; do not first calculate the entire duration and divide by days. Mild impact: ±0.1 to ±0.4; regular reassurance, tax burdens, fairness, cultural treatment or autonomy adjustments: ±0.4 to ±1.2; major reforms, strong oppression, heavy-tax relief, broad relief programs, honorary privileges or clear discrimination: ±2 to ±6; extreme tyranny, major rescue of the population, severe betrayal, systematic persecution or near-rebellion stimuli: ±4 to ±12."),
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public System.Collections.Generic.IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(PolicyEffectPreparedInstance preparedInstance)
		=> PolicyEffectModuleRuntimeAdapters.BuildNumericModelContributions(this, preparedInstance);
}
