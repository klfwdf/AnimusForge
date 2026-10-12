using System;
using System.Collections.Generic;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.KingdomStabilityEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class KingdomStabilityPayload : NumericPolicyEffectPayload
{
}

internal sealed class KingdomStabilityEffectModule : NumericPolicyEffectModuleBase<KingdomStabilityPayload>, IPolicyEffectCompositeModule
{
	private static readonly IReadOnlyCollection<string> RuntimeModules = new[]
	{
		"kingdomStabilityNextDayOnce"
	};

	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "kingdomStability",
		order: 120,
		legacyIds: Array.Empty<string>(),
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Kingdom },
		targetKinds: new[] { PolicyEffectTargetKind.Kingdom },
		cueTerms: new[] { PolicyCompatibilityTexts.Get("Module_kingdomStability_Original_Closeout_0"), PolicyCompatibilityTexts.Get("Module_kingdomStability_Original_Closeout_1"), PolicyCompatibilityTexts.Get("Module_kingdomStability_Original_Closeout_2"), PolicyCompatibilityTexts.Get("Module_kingdomStability_Original_Closeout_3"), PolicyCompatibilityTexts.Get("Module_kingdomStability_Original_Closeout_4"), PolicyCompatibilityTexts.Get("Module_kingdomStability_Original_Closeout_5"), "kingdom stability", "political stability", "unrest", "rebellion risk" },
		retrievalText: PolicyGenerationPromptTexts.Get("Module_kingdomStability_retrievalText", "Kingdom stability, political stability, central authority, national unrest, rebellion risk, institutional shocks and political crises; settles once on the game day following policy adoption."),
		catalogSummary: PolicyGenerationPromptTexts.Get("Module_kingdomStability_catalogSummary", "One-time kingdom stability change on the next game day"),
		mainInstruction: PolicyGenerationPromptTexts.Get("Module_kingdomStability_mainInstruction", "If the policy directly changes kingdom-level political stability, central authority or unrest risk, specify a one-time stability change. It settles on the next game day after adoption, does not stack by town count, and is unavailable to local policies."),
		postprocessRule: PolicyGenerationPromptTexts.Get("Module_kingdomStability_postprocessRule", "payload contains only one finite numeric value, settled once as integer stability points on the next game day: positive raises stability, negative lowers it. It does not repeat with duration or stack by town count, and is prohibited in local scope."),
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Stability,
		executionKind: PolicyEffectExecutionKind.Composite,
		hook: PolicyEffectHook.DailyScheduler,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PointsOnce,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Linear,
		payloadSchemaVersion: 1,
		promptVisible: true,
		displayGroup: "kingdomStability",
		playerDisplayName: PolicyCompatibilityTexts.Get("Module_kingdomStability_playerDisplayName"),
		editableUnderstandingPrompt: PolicyGenerationPromptTexts.Get("Module_kingdomStability_editableUnderstandingPrompt", "Kingdom stability means national political structure and governing order. It is mainly affected by public confidence, fiscal prestige, vassal trust, royal legitimacy, war victories and defeats, noble interests, domestic fragmentation risk and decisive victories. Pure administrative or ordinary local numeric changes are usually not kingdom stability consequences; positive or negative direction requires direct political causality."),
		editableEvaluationPrompt: PolicyGenerationPromptTexts.Get("Module_kingdomStability_editableEvaluationPrompt", "Positive means improved stability; negative means greater unrest. Judge integer stability points. Clear changes to public confidence, fiscal prestige, vassal trust, royal legitimacy or fragmentation risk: ±4 to ±7; major reforms, nationwide mobilization, broad noble conflicts or severe political crises: ±7 to ±14; the verge of civil war, national survival, collapse of governance or decisive victory: ±14 to ±22. Settle only once on the game day after policy adoption; do not stack by town count."),
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public IReadOnlyCollection<string> RuntimeModuleIds => RuntimeModules;

	protected override bool TryNormalizeNumericValue(float rawValue, string scope, out float normalizedValue, out string error)
	{
		normalizedValue = rawValue;
		if (string.Equals(scope, PolicyEffectScopes.Local, StringComparison.OrdinalIgnoreCase))
		{
			error = "kingdomStability does not support local policy scope";
			return false;
		}
		if (float.IsNaN(rawValue) || float.IsInfinity(rawValue))
		{
			error = "kingdomStability value must be finite";
			return false;
		}
		normalizedValue = (float)Math.Round(rawValue, MidpointRounding.AwayFromZero);
		error = string.Empty;
		return true;
	}

	public bool TryExpand(
		PolicyEffectCompileContext context,
		PolicyEffectPayload payload,
		out IReadOnlyList<PolicyEffectCompositeChild> children,
		out string error)
	{
		children = Array.Empty<PolicyEffectCompositeChild>();
		if (context == null || !(payload is KingdomStabilityPayload typed))
		{
			error = "kingdomStability requires a valid compilation context and payload";
			return false;
		}
		if (!TryNormalizeTypedPayload(typed, context.SourceScope, out KingdomStabilityPayload normalized, out error))
		{
			return false;
		}
		if (normalized.Value == 0f)
		{
			children = Array.Empty<PolicyEffectCompositeChild>();
			error = string.Empty;
			return true;
		}
		children = new[]
		{
			new PolicyEffectCompositeChild
			{
				ModuleId = "kingdomStabilityNextDayOnce",
				Payload = new KingdomStabilityNextDayOncePayload
				{
					ModuleId = "kingdomStabilityNextDayOnce",
					SchemaVersion = 1,
					Value = normalized.Value
				}
			}
		};
		error = string.Empty;
		return true;
	}
}
