[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.ConstructionPerDayEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class ConstructionPerDayPayload : NumericPolicyEffectPayload
{
}

internal sealed class ConstructionPerDayEffectModule : NumericPolicyEffectModuleBase<ConstructionPerDayPayload>, IModelModifierPolicyEffectModule
{
	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "constructionPerDay",
		order: 80,
		legacyIds: new[] { "constructionPowerDailyDelta", "constructionSpeedPercent" },
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Settlement, PolicyEffectTargetKind.Clan, PolicyEffectTargetKind.Kingdom },
		targetKinds: new[] { PolicyEffectTargetKind.Settlement },
		cueTerms: new[] { PolicyCompatibilityTexts.Get("Module_constructionPerDay_Original_Closeout_0"), PolicyCompatibilityTexts.Get("Module_constructionPerDay_Original_Closeout_1"), PolicyCompatibilityTexts.Get("Module_constructionPerDay_Original_Closeout_2"), PolicyCompatibilityTexts.Get("Module_constructionPerDay_Original_Closeout_3"), PolicyCompatibilityTexts.Get("Module_constructionPerDay_Original_Closeout_4"), PolicyCompatibilityTexts.Get("Module_constructionPerDay_Original_Closeout_5"), "construction power", "engineering", "building", "infrastructure", "public works" },
		retrievalText: PolicyGenerationPromptTexts.Get("Module_constructionPerDay_retrievalText", "Construction power, engineering, buildings, infrastructure, construction work, artisans, fortifications and public works; fixed daily construction-point changes per target town or castle."),
		catalogSummary: PolicyGenerationPromptTexts.Get("Module_constructionPerDay_catalogSummary", "Daily construction point change for towns or castles"),
		mainInstruction: PolicyGenerationPromptTexts.Get("Module_constructionPerDay_mainInstruction", "If the policy continuously changes target towns’ or castles’ daily construction power, engineering capacity or construction investment, explain targets, direction, intensity and reasons in numericIntent; do not output module IDs or numbers."),
		postprocessRule: PolicyGenerationPromptTexts.Get("Module_constructionPerDay_postprocessRule", "payload contains only one finite numeric value. It is the fixed construction points directly added to native building projects daily per target town or castle: positive speeds construction, negative slows it. It is not a percentage."),
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Construction,
		executionKind: PolicyEffectExecutionKind.ModelModifier,
		hook: PolicyEffectHook.SettlementConstructionDaily,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PointsPerDay,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Linear,
		payloadSchemaVersion: 1,
		playerDisplayName: PolicyCompatibilityTexts.Get("Module_constructionPerDay_playerDisplayName"),
		editableUnderstandingPrompt: PolicyGenerationPromptTexts.Get("Module_constructionPerDay_editableUnderstandingPrompt", "Building construction power is affected only by explicit building construction or repairs, artisans, labor, building materials, engineering transport, construction organization, stoppages or damage to works. Development, prosperity, subsidies, tax exemptions, tax increases, general fiscal spending and reasonable elaboration of execution methods must not independently be interpreted as construction measures."),
		editableEvaluationPrompt: PolicyGenerationPromptTexts.Get("Module_constructionPerDay_editableEvaluationPrompt", "Judge fixed construction power added or removed directly per affected town or castle daily, without percentage conversion. Small-scale changes: ±20 to ±60; sustained expansion of construction resources: +60 to +150; major nationwide construction: +300 to +1000. Extreme mobilization, huge dedicated investment or a player's explicit demand for extreme intensity may reach 2 to 4 times this range, including more than +1000, only when execution paths and resource commitments are sufficiently clear. Use proportionate negatives only if the policy directly obstructs construction, causes labor loss or material shortages, or damages the construction system."),
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public System.Collections.Generic.IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(PolicyEffectPreparedInstance preparedInstance)
		=> PolicyEffectModuleRuntimeAdapters.BuildNumericModelContributions(this, preparedInstance);
}
