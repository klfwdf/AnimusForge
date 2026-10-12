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
		cueTerms: new[] { PolicyCompatibilityTexts.Get("Module_hearthPerDay_Original_Closeout_0"), PolicyCompatibilityTexts.Get("Module_hearthPerDay_Original_Closeout_1"), PolicyCompatibilityTexts.Get("Module_hearthPerDay_Original_Closeout_2"), PolicyCompatibilityTexts.Get("Module_hearthPerDay_Original_Closeout_3"), PolicyCompatibilityTexts.Get("Module_hearthPerDay_Original_Closeout_4"), "hearths", "rural population", "resettlement", "land reclamation", "population loss" },
		retrievalText: PolicyGenerationPromptTexts.Get("Module_hearthPerDay_retrievalText", "Village hearths, population, rural development, migrant resettlement, land reclamation, population loss and village prosperity; daily hearth changes in villages attached to lawful parent targets."),
		catalogSummary: PolicyGenerationPromptTexts.Get("Module_hearthPerDay_catalogSummary", "Daily village hearth change"),
		mainInstruction: PolicyGenerationPromptTexts.Get("Module_hearthPerDay_mainInstruction", "If the policy continuously changes hearths, rural population or land reclamation in villages attached to target towns or castles, explain the target scope, direction, intensity and reasons naturally in numericIntent. Villages have no independent targets; do not output module IDs or numbers."),
		postprocessRule: PolicyGenerationPromptTexts.Get("Module_hearthPerDay_postprocessRule", "payload contains only one finite numeric value. Lawful parent targets deterministically expand into their attached villages; the value is fixed daily hearth points per village, positive for increases and negative for decreases. Villages do not accept independent target handles."),
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Population,
		executionKind: PolicyEffectExecutionKind.ModelModifier,
		hook: PolicyEffectHook.VillageHearthDaily,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PointsPerDay,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Linear,
		payloadSchemaVersion: 1,
		playerDisplayName: PolicyCompatibilityTexts.Get("Module_hearthPerDay_playerDisplayName"),
		editableUnderstandingPrompt: PolicyGenerationPromptTexts.Get("Module_hearthPerDay_editableUnderstandingPrompt", "Village hearths represent population and labor. They are mainly affected by labor availability, safety, corvée, resettlement, migration, flight, land reclamation, slaughter and disasters. When policies continuously change population in villages attached to the relevant towns or castles through these mechanisms, hearths are a direct consequence."),
		editableEvaluationPrompt: PolicyGenerationPromptTexts.Get("Module_hearthPerDay_editableEvaluationPrompt", "Judge daily changes per affected village. Mild impact: ±0.1 to ±0.5; regular corvée, resettlement, labor recovery or migration: ±0.5 to ±1.5; strong immigration, wartime flight, oppressive taxes or large-scale forced labor: ±2 to ±4; nationwide population support, slaughter, disasters or forced migration: ±4 to ±7; extreme population changes or catastrophes: ±7 to ±10."),
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public System.Collections.Generic.IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(PolicyEffectPreparedInstance preparedInstance)
		=> PolicyEffectModuleRuntimeAdapters.BuildNumericModelContributions(this, preparedInstance);
}
