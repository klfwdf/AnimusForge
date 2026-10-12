[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.TaxIncomePctEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class TaxIncomePctPayload : NumericPolicyEffectPayload
{
}

internal sealed class TaxIncomePctEffectModule : NumericPolicyEffectModuleBase<TaxIncomePctPayload>, IModelModifierPolicyEffectModule
{
	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "taxIncomePct",
		order: 70,
		legacyIds: new[] { "townTaxPercent" },
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Settlement, PolicyEffectTargetKind.Clan, PolicyEffectTargetKind.Kingdom },
		targetKinds: new[] { PolicyEffectTargetKind.Town },
		cueTerms: new[] { PolicyCompatibilityTexts.Get("Module_taxIncomePct_Original_Closeout_0"), PolicyCompatibilityTexts.Get("Module_taxIncomePct_Original_Closeout_1"), PolicyCompatibilityTexts.Get("Module_taxIncomePct_Original_Closeout_2"), PolicyCompatibilityTexts.Get("Module_taxIncomePct_Original_Closeout_3"), PolicyCompatibilityTexts.Get("Module_taxIncomePct_Original_Closeout_4"), PolicyCompatibilityTexts.Get("Module_taxIncomePct_Original_Closeout_5"), PolicyCompatibilityTexts.Get("Module_taxIncomePct_Original_Closeout_6"), "tax income", "tax rate", "tax reduction", "tax exemption", "fief taxes" },
		retrievalText: PolicyGenerationPromptTexts.Get("Module_taxIncomePct_retrievalText", "Fief, town or castle taxation, tax rates, tax reductions, exemptions, fiscal tax revenue, fief tax income, collection and tax transfers; percentage-point changes to main tax income received by clans owning target towns or castles. General personal or lord income without fiefs or taxation is excluded."),
		catalogSummary: PolicyGenerationPromptTexts.Get("Module_taxIncomePct_catalogSummary", "Tax percentage change for clans owning towns or castles"),
		mainInstruction: PolicyGenerationPromptTexts.Get("Module_taxIncomePct_mainInstruction", "Only if the policy explicitly changes fiefs, towns, castles or taxation and directly affects main tax income of the clans owning target towns or castles, explain burden bearers, beneficiaries, direction, intensity and reasons in numericIntent. General income increases for lords, rulers or other professions without fiefs, towns, castles or taxation do not qualify. Do not output module IDs or numbers."),
		postprocessRule: PolicyGenerationPromptTexts.Get("Module_taxIncomePct_postprocessRule", "payload contains only one finite numeric value. It is the percentage-point change in the native main tax income finally received by clans owning target towns or castles: positive increases revenue, negative reduces it. It is not the local population’s tax rate and excludes independent village income and tariffs."),
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Fiscal,
		executionKind: PolicyEffectExecutionKind.ModelModifier,
		hook: PolicyEffectHook.TownTaxIncome,
		aggregation: PolicyEffectAggregationKind.PercentPoints,
		valueUnit: PolicyEffectValueUnit.PercentPoints,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Linear,
		payloadSchemaVersion: 1,
		playerDisplayName: PolicyCompatibilityTexts.Get("Module_taxIncomePct_playerDisplayName"),
		editableUnderstandingPrompt: PolicyGenerationPromptTexts.Get("Module_taxIncomePct_editableUnderstandingPrompt", "Main town and castle tax income means changes in native main tax revenue finally received by clans owning the target fiefs, not amounts extracted from the local population. Use only when the body explicitly involves fiefs, towns, castles or taxation. Raising owner-retained tax rates, collection efficiency or receipts remitted from other regions increases income; tax reductions, exemptions or withholding, transferring or remitting taxes to another party reduces income. General increases in lords', rulers' or other professions' incomes without fiefs or taxation do not qualify. When taxes transfer between parties, represent both the payer and beneficiary separately. Independent village income, tariffs and mere prosperity changes are excluded."),
		editableEvaluationPrompt: PolicyGenerationPromptTexts.Get("Module_taxIncomePct_editableEvaluationPrompt", "Judge percentage-point changes relative to final native main tax income: positive increases revenue and negative reduces it. Regular tax adjustments: ±5% to ±15%; clear adjustments: ±15% to ±35%; major nationwide taxation: ±20% to ±60%. Changes only to specific groups, industries, regions, periods, tax types or portions of revenue require a proportionate value based on actual coverage. Approach -100% only when all main town and castle taxation in the affected scope is explicitly abolished."),
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public System.Collections.Generic.IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(PolicyEffectPreparedInstance preparedInstance)
		=> PolicyEffectModuleRuntimeAdapters.BuildNumericModelContributions(this, preparedInstance);
}
