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
		cueTerms: new[] { PolicyCompatibilityTexts.Get("Module_foodPerDay_Original_Closeout_0"), PolicyCompatibilityTexts.Get("Module_foodPerDay_Original_Closeout_1"), PolicyCompatibilityTexts.Get("Module_foodPerDay_Original_Closeout_2"), PolicyCompatibilityTexts.Get("Module_foodPerDay_Original_Closeout_3"), PolicyCompatibilityTexts.Get("Module_foodPerDay_Original_Closeout_4"), "food stocks", "granary", "famine", "relief", "army rations" },
		retrievalText: PolicyGenerationPromptTexts.Get("Module_foodPerDay_retrievalText", "Town food, granaries, food stocks, supply, famine, relief, rationing, grain requisitions and transport supplies; daily food changes per target town."),
		catalogSummary: PolicyGenerationPromptTexts.Get("Module_foodPerDay_catalogSummary", "Daily town food stock change"),
		mainInstruction: PolicyGenerationPromptTexts.Get("Module_foodPerDay_mainInstruction", "If the policy continuously changes target towns’ food stocks or daily supplies, explain the targets, direction, intensity and reasons in numericIntent; do not output module IDs or numbers."),
		postprocessRule: PolicyGenerationPromptTexts.Get("Module_foodPerDay_postprocessRule", "payload contains only one finite numeric value. It is the fixed food-stock points settled daily per target town: positive adds food, negative removes it. Do not convert it into a cumulative value for the entire policy."),
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Supply,
		executionKind: PolicyEffectExecutionKind.ModelModifier,
		hook: PolicyEffectHook.TownFoodDaily,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PointsPerDay,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Linear,
		payloadSchemaVersion: 1,
		playerDisplayName: PolicyCompatibilityTexts.Get("Module_foodPerDay_playerDisplayName"),
		editableUnderstandingPrompt: PolicyGenerationPromptTexts.Get("Module_foodPerDay_editableUnderstandingPrompt", "Food stocks are mainly affected by production, procurement, storage, transport, requisition, relief, army-ration consumption, blockades and destruction. When policies continuously change town food supplies through these mechanisms, food is a direct consequence. Procurement, subsidies and economic development do not themselves imply stock depletion. Only direct requisition, consumption or destruction of local stocks produces negative changes."),
		editableEvaluationPrompt: PolicyGenerationPromptTexts.Get("Module_foodPerDay_editableEvaluationPrompt", "Judge daily stock changes per affected town. Mild impact: ±1 to ±3; regular food policies, transport, requisition or storage adjustments: ±3 to ±8; large-scale relief, forced requisitions or army-ration allocation: ±8 to ±15; major nationwide supplies, famine, blockades or comprehensive grain requisition: ±18 to ±30; extreme disasters or nationwide food operations: ±30 to ±45."),
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public System.Collections.Generic.IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(PolicyEffectPreparedInstance preparedInstance)
		=> PolicyEffectModuleRuntimeAdapters.BuildNumericModelContributions(this, preparedInstance);
}
