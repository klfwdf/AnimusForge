[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.ProsperityPerDayEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class ProsperityPerDayPayload : NumericPolicyEffectPayload
{
}

internal sealed class ProsperityPerDayEffectModule : NumericPolicyEffectModuleBase<ProsperityPerDayPayload>, IModelModifierPolicyEffectModule
{
	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "prosperityPerDay",
		order: 10,
		legacyIds: new[] { "prosperityDailyDeltaPerTown" },
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Settlement, PolicyEffectTargetKind.Clan, PolicyEffectTargetKind.Kingdom },
		targetKinds: new[] { PolicyEffectTargetKind.Town },
		cueTerms: new[] { PolicyCompatibilityTexts.Get("Module_prosperityPerDay_Original_Closeout_0"), PolicyCompatibilityTexts.Get("Module_prosperityPerDay_Original_Closeout_1"), PolicyCompatibilityTexts.Get("Module_prosperityPerDay_Original_Closeout_2"), PolicyCompatibilityTexts.Get("Module_prosperityPerDay_Original_Closeout_3"), "prosperity", "commercial growth", "economic depression", "market revival" },
		retrievalText: PolicyGenerationPromptTexts.Get("Module_prosperityPerDay_retrievalText", "Town prosperity, commercial development, population and wealth growth, economic depression, investment, market revitalization and long-term development; daily prosperity changes per target town."),
		catalogSummary: PolicyGenerationPromptTexts.Get("Module_prosperityPerDay_catalogSummary", "Daily town prosperity change"),
		mainInstruction: PolicyGenerationPromptTexts.Get("Module_prosperityPerDay_mainInstruction", "If the policy continuously changes target towns’ prosperity, commercial vitality or overall economic development, explain the targets, direction, intensity and reasons in numericIntent; do not output module IDs or numbers."),
		postprocessRule: PolicyGenerationPromptTexts.Get("Module_prosperityPerDay_postprocessRule", "payload contains only one finite numeric value. It is the fixed prosperity points settled daily per target town: positive increases and negative decreases prosperity. Do not convert it into a cumulative policy value or percentage."),
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Economy,
		executionKind: PolicyEffectExecutionKind.ModelModifier,
		hook: PolicyEffectHook.SettlementProsperityDaily,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PointsPerDay,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Linear,
		payloadSchemaVersion: 1,
		playerDisplayName: PolicyCompatibilityTexts.Get("Module_prosperityPerDay_playerDisplayName"),
		editableUnderstandingPrompt: PolicyGenerationPromptTexts.Get("Module_prosperityPerDay_editableUnderstandingPrompt", "Prosperity is mainly affected by trade, tax burdens, industry and commerce, market confidence, development investment and war damage. When policies continuously change the scale of towns' economies through these mechanisms, prosperity is a direct consequence."),
		editableEvaluationPrompt: PolicyGenerationPromptTexts.Get("Module_prosperityPerDay_editableEvaluationPrompt", "Judge daily changes per affected town. Mild impact: ±0.5 to ±2; regular policies: ±2 to ±5; clear economic policies, trade stimulus, damage from tax burdens or war damage: ±5 to ±10; major nationwide investment or disasters: ±12 to ±24; extreme prosperity projects, severe plunder, blockades, famine or national economic catastrophes: ±24 to ±36."),
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public System.Collections.Generic.IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(PolicyEffectPreparedInstance preparedInstance)
		=> PolicyEffectModuleRuntimeAdapters.BuildNumericModelContributions(this, preparedInstance);
}
