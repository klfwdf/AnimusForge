using System;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.ArmyFormationTendencyPctEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class ArmyFormationTendencyPctPayload : NumericPolicyEffectPayload
{
}

internal sealed class ArmyFormationTendencyPctEffectModule
	: NumericPolicyEffectModuleBase<ArmyFormationTendencyPctPayload>, IModelModifierPolicyEffectModule
{
	internal const float MinimumPercent = -100f;
	internal const float MaximumPercent = 200f;

	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "armyFormationTendencyPct",
		order: 150,
		legacyIds: Array.Empty<string>(),
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[]
		{
			PolicyEffectTargetKind.Settlement,
			PolicyEffectTargetKind.Clan,
			PolicyEffectTargetKind.Kingdom,
			PolicyEffectTargetKind.Hero
		},
		targetKinds: new[] { PolicyEffectTargetKind.Clan },
		cueTerms: new[] { PolicyCompatibilityTexts.Get("Module_armyFormationTendencyPct_Original_0"), PolicyCompatibilityTexts.Get("Module_armyFormationTendencyPct_Original_1"), PolicyCompatibilityTexts.Get("Module_armyFormationTendencyPct_Original_2"), PolicyCompatibilityTexts.Get("Module_armyFormationTendencyPct_Original_3"), PolicyCompatibilityTexts.Get("Module_armyFormationTendencyPct_Original_4"), PolicyCompatibilityTexts.Get("Module_armyFormationTendencyPct_Original_5"), "army formation", "gather army", "army formation tendency" },
		retrievalText: PolicyGenerationPromptTexts.Get("Module_armyFormationTendencyPct_retrievalText", "The policy raises or lowers target clan lords' inclination to form armies proactively. It only adjusts WillGatherArmy candidate scores that already meet native eligibility; it does not change native gates involving influence, peace, food, party size, mercenary status, eligible members, or existing armies."),
		catalogSummary: PolicyGenerationPromptTexts.Get("Module_armyFormationTendencyPct_catalogSummary", "Relative army-formation score for eligible target-clan lords"),
		mainInstruction: PolicyGenerationPromptTexts.Get("Module_armyFormationTendencyPct_mainInstruction", "Only when the policy explicitly encourages, requires, discourages, or restricts target clan lords' proactive army formation should you specify a change relative to native eligible army-formation candidate scores. General claims of strengthening the military, increasing influence, expanding forces, enlisting, or improving army combat strength do not qualify unless they change lords' army-formation decisions. This effect cannot bypass native eligibility and is not an absolute army-formation probability. Do not output module IDs."),
		postprocessRule: PolicyGenerationPromptTexts.Get("Module_armyFormationTendencyPct_postprocessRule", "payload must be exactly {\"value\": number}. value is the percentage change relative to the native army-formation candidate score of target clan lords after native eligibility is satisfied, ranging from -100 to +200, and must be finite. +50 multiplies the score by 1.5, +100 multiplies it by 2, and -100 sets it to zero. This is not an absolute probability, does not disband existing armies, and does not bypass any native eligibility gate."),
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Military,
		executionKind: PolicyEffectExecutionKind.ModelModifier,
		hook: PolicyEffectHook.ArmyFormationScore,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.RelativePercent,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Linear,
		payloadSchemaVersion: 1,
		playerDisplayName: PolicyCompatibilityTexts.Get("Module_armyFormationTendencyPct_Original_6"),
		editableUnderstandingPrompt: PolicyGenerationPromptTexts.Get("Module_armyFormationTendencyPct_editableUnderstandingPrompt", "Army formation tendency means that target clan lords who already satisfy native army-forming eligibility become more or less inclined to form an army instead of choosing another AI behavior. It only scales native candidate scores already marked WillGatherArmy. It grants no influence, creates no summonable members, bypasses none of the native conditions involving peace, food, party size, non-mercenary status or not being in an existing army, and does not disband existing armies. Generic strengthening of military power, increasing influence, expanding parties or improving army combat strength does not qualify without changing lords' proactive army-formation decisions."),
		editableEvaluationPrompt: PolicyGenerationPromptTexts.Get("Module_armyFormationTendencyPct_editableEvaluationPrompt", "Judge percentage changes relative to native eligible army-formation candidate scores, not absolute probability. Mild encouragement or restrictions usually use ±10% to ±25%. Explicit encouragement, official obligations or regular institutional pushes suggest about +50%; strong army mobilization about +75% to +100%. Near-mandatory mobilization that retains native eligibility and AI competition may reach +150%; +200% is the total intensity ceiling. Inhibitory policies use negatives on the same scale; -100% zeroes the target clans' new army-formation candidate scores. Add active-policy percentages first; the runtime total multiplier is limited to 0 to 3. +100% is strong and should not be an ordinary default.")
	,
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	protected override bool TryNormalizeNumericValue(
		float rawValue,
		string scope,
		out float normalizedValue,
		out string error)
	{
		_ = scope;
		normalizedValue = rawValue;
		if (rawValue < MinimumPercent || rawValue > MaximumPercent)
		{
			error = "armyFormationTendencyPct value must be between -100 and +200";
			return false;
		}
		error = string.Empty;
		return true;
	}

	public System.Collections.Generic.IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(
		PolicyEffectPreparedInstance preparedInstance)
		=> PolicyEffectModuleRuntimeAdapters.BuildNumericModelContributions(this, preparedInstance);
}
