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
		cueTerms: new[] { PolicyCompatibilityTexts.Get("Module_securityPerDay_Original_Closeout_0"), PolicyCompatibilityTexts.Get("Module_securityPerDay_Original_Closeout_1"), PolicyCompatibilityTexts.Get("Module_securityPerDay_Original_Closeout_2"), PolicyCompatibilityTexts.Get("Module_securityPerDay_Original_Closeout_3"), PolicyCompatibilityTexts.Get("Module_securityPerDay_Original_Closeout_4"), PolicyCompatibilityTexts.Get("Module_securityPerDay_Original_Closeout_5"), "security", "crime", "patrols", "law enforcement", "corruption" },
		retrievalText: PolicyGenerationPromptTexts.Get("Module_securityPerDay_retrievalText", "Town security, crime, patrols, bandits, law enforcement, order, corruption and public safety; daily security changes per target town."),
		catalogSummary: PolicyGenerationPromptTexts.Get("Module_securityPerDay_catalogSummary", "Daily town security change"),
		mainInstruction: PolicyGenerationPromptTexts.Get("Module_securityPerDay_mainInstruction", "If the policy continuously changes target towns’ security, crime control or public order, explain the targets, direction, intensity and reasons in numericIntent; do not output module IDs or numbers."),
		postprocessRule: PolicyGenerationPromptTexts.Get("Module_securityPerDay_postprocessRule", "payload contains only one finite numeric value. It is the fixed security points settled daily per target town: positive improves security, negative worsens it."),
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Security,
		executionKind: PolicyEffectExecutionKind.ModelModifier,
		hook: PolicyEffectHook.TownSecurityDaily,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PointsPerDay,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Linear,
		payloadSchemaVersion: 1,
		playerDisplayName: PolicyCompatibilityTexts.Get("Module_securityPerDay_playerDisplayName"),
		editableUnderstandingPrompt: PolicyGenerationPromptTexts.Get("Module_securityPerDay_editableUnderstandingPrompt", "Security is mainly affected by banditry, patrols, law enforcement, justice, corruption, military rule, suppression and local order. When policies continuously change local safety through these mechanisms, security is a direct consequence."),
		editableEvaluationPrompt: PolicyGenerationPromptTexts.Get("Module_securityPerDay_editableEvaluationPrompt", "Judge daily changes per affected town. Mild impact: ±0.1 to ±0.3; regular patrols, law enforcement, anti-corruption or banditry changes: ±0.3 to ±0.7; strong security campaigns, military rule, border disorder or banditry outbreaks: ±0.8 to ±1.5; severe disorder or high-pressure suppression: ±1.5 to ±2.5. More than ±2.5 is reserved for brief extreme internal unrest, bloody suppression or large-scale banditry."),
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public System.Collections.Generic.IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(PolicyEffectPreparedInstance preparedInstance)
		=> PolicyEffectModuleRuntimeAdapters.BuildNumericModelContributions(this, preparedInstance);
}
