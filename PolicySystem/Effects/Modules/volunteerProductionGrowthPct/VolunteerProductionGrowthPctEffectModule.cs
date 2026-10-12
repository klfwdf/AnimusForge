using System;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.VolunteerProductionGrowthPctEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class VolunteerProductionGrowthPctPayload : NumericPolicyEffectPayload
{
}

internal sealed class VolunteerProductionGrowthPctEffectModule
	: NumericPolicyEffectModuleBase<VolunteerProductionGrowthPctPayload>, IModelModifierPolicyEffectModule
{
	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "volunteerProductionGrowthPct",
		order: 145,
		legacyIds: Array.Empty<string>(),
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[]
		{
			PolicyEffectTargetKind.Settlement,
			PolicyEffectTargetKind.Clan,
			PolicyEffectTargetKind.Kingdom
		},
		targetKinds: new[] { PolicyEffectTargetKind.Settlement },
		cueTerms: new[] { PolicyCompatibilityTexts.Get("Module_volunteerProductionGrowthPct_Original_Closeout_0"), PolicyCompatibilityTexts.Get("Module_volunteerProductionGrowthPct_Original_Closeout_1"), PolicyCompatibilityTexts.Get("Module_volunteerProductionGrowthPct_Original_Closeout_2"), PolicyCompatibilityTexts.Get("Module_volunteerProductionGrowthPct_Original_Closeout_3"), PolicyCompatibilityTexts.Get("Module_volunteerProductionGrowthPct_Original_Closeout_4"), PolicyCompatibilityTexts.Get("Module_volunteerProductionGrowthPct_Original_Closeout_5"), PolicyCompatibilityTexts.Get("Module_volunteerProductionGrowthPct_Original_Closeout_6"), "volunteer replenishment", "volunteer growth", "recruitable slots" },
		retrievalText: PolicyGenerationPromptTexts.Get("Module_volunteerProductionGrowthPct_retrievalText", "The policy directly raises or lowers replenishment and growth speed of unrecruited volunteers in target fiefs. The same relative percentage scales empty-slot replenishment checks and the frequency at which existing volunteers enter native upgrade attempts. This is an abstract policy result, requires no recruitment station or training camp, and does not directly recruit, grant troop experience or guarantee upgrades."),
		catalogSummary: PolicyGenerationPromptTexts.Get("Module_volunteerProductionGrowthPct_catalogSummary", "Relative volunteer refill and growth-attempt frequency"),
		mainInstruction: PolicyGenerationPromptTexts.Get("Module_volunteerProductionGrowthPct_mainInstruction", "Only when the policy directly changes replenishment of unrecruited volunteers, recovery of recruitable slots or volunteer growth in target fiefs, specify a change relative to native daily check frequency. Empty and occupied slots use the same value: empty slots affect replenishment checks; occupied slots affect only entry into native upgrade attempts and retain subsequent native upgrade checks. It is not recruitment-pool refreshing, requires no station or camp, does not directly add party members or grant experience to lord parties or garrisons. General army expansion, training or better active troops without changing volunteers does not qualify. Do not output module IDs."),
		postprocessRule: PolicyGenerationPromptTexts.Get("Module_volunteerProductionGrowthPct_postprocessRule", "payload must be exactly {\"value\": number}; value must be finite and expresses percentage change relative to native daily check frequency. +20 multiplies native probability by 120%, +100 by 200%; -100 or lower stacked totals set probability to zero. These are not absolute probability points. Sum percentages from active policies, scale using native formulas, and clamp final probability to 0 to 1. Empty-slot replenishment and existing-volunteer entry into native upgrade attempts share one value."),
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Military,
		executionKind: PolicyEffectExecutionKind.ModelModifier,
		hook: PolicyEffectHook.VolunteerProductionProbability,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.RelativePercent,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Linear,
		payloadSchemaVersion: 1,
		playerDisplayName: PolicyCompatibilityTexts.Get("Module_volunteerProductionGrowthPct_playerDisplayName"),
		editableUnderstandingPrompt: PolicyGenerationPromptTexts.Get("Module_volunteerProductionGrowthPct_editableUnderstandingPrompt", "Volunteer replenishment and growth directly changes unrecruited notable volunteer slots in target fiefs. One value affects two states: empty slots change native volunteer-replenishment checks; occupied slots change the frequency of entering native upgrade attempts. The latter still requires subsequent native upgrade checks, so upgrades are not guaranteed. This is an abstract policy result: recruitment stations, training camps or other fixed facilities need not be specified, and it must not be described as recruitment-pool refreshing. It does not directly recruit soldiers, grant hero skills, grant troop experience to ordinary soldiers in lord parties or garrisons, or affect militia."),
		editableEvaluationPrompt: PolicyGenerationPromptTexts.Get("Module_volunteerProductionGrowthPct_editableEvaluationPrompt", "The value is a percentage change relative to native daily check frequency, not absolute probability points. +20 multiplies native probability by 1.2, +50 by 1.5, +100 by 2, and -100 sets it to zero. Mild improvements or impediments usually use ±10% to ±25%; explicit institutional changes about ±25% to ±60%; strong reforms about ±60% to ±100%. More than +100% requires very strong policy evidence, but runtime follows the old policy system without an extra positive hard cap. Add policies first; the multiplier minimum is 0, and final probability is limited to 0 to 1. Use soldier elite training alongside this only if the policy separately grants experience to ordinary soldiers already in lord parties or garrisons."),
		targetProjection: PolicyEffectTargetProjectionKind.PrimaryFiefAndBoundSettlements,
		targetRefresh: PolicyEffectTargetRefreshKind.Dynamic,
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public System.Collections.Generic.IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(
		PolicyEffectPreparedInstance preparedInstance)
		=> PolicyEffectModuleRuntimeAdapters.BuildNumericModelContributions(this, preparedInstance);
}
