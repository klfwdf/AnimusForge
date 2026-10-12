using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.KingdomVillageRaidBanEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class KingdomVillageRaidBanPayload : PolicyEffectPayload
{
}

internal sealed class KingdomVillageRaidBanEffectModule
	: PolicyEffectModuleBase<KingdomVillageRaidBanPayload>, IModelModifierPolicyEffectModule
{
	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "kingdomVillageRaidBan",
		order: 160,
		legacyIds: Array.Empty<string>(),
		allowedScopes: new[] { PolicyEffectScopes.Kingdom },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Kingdom },
		targetKinds: new[] { PolicyEffectTargetKind.Kingdom },
		cueTerms: new[] { PolicyCompatibilityTexts.Get("Module_kingdomVillageRaidBan_Original_Closeout_0"), PolicyCompatibilityTexts.Get("Module_kingdomVillageRaidBan_Original_Closeout_1"), PolicyCompatibilityTexts.Get("Module_kingdomVillageRaidBan_Original_Closeout_2"), PolicyCompatibilityTexts.Get("Module_kingdomVillageRaidBan_Original_Closeout_3"), PolicyCompatibilityTexts.Get("Module_kingdomVillageRaidBan_Original_Closeout_4"), "village raid ban", "prohibit village raids", "stop raiding villages" },
		retrievalText: PolicyGenerationPromptTexts.Get("Module_kingdomVillageRaidBan_retrievalText", "The policy prohibits lords and lord parties of the issuer’s own kingdom from proactively raiding any village. The target is fixed automatically to the issuer’s kingdom; it does not protect its villages from foreign raids or affect other kingdoms."),
		catalogSummary: PolicyGenerationPromptTexts.Get("Module_kingdomVillageRaidBan_catalogSummary", "Ban proactive village raids by the issuing kingdom's lords"),
		mainInstruction: PolicyGenerationPromptTexts.Get("Module_kingdomVillageRaidBan_mainInstruction", "Select this effect only if the policy explicitly prohibits, stops or restricts lords of the issuer’s kingdom from burning, attacking or raiding villages. There is no selectable target kingdom: selection affects only the issuer’s kingdom. It does not protect domestic villages from foreign lords or constrain any other kingdom. Do not output module IDs."),
		postprocessRule: PolicyGenerationPromptTexts.Get("Module_kingdomVillageRaidBan_postprocessRule", "payload must be exactly {}. Presence means that lord parties of the issuer’s kingdom are prohibited from proactively raiding any Village; there is no number, level or stacked intensity. targetHandles may only copy the sole issuer-kingdom handle provided for this module by the execution directory."),
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateMarkerSchema(),
		family: PolicyEffectFamily.Military,
		executionKind: PolicyEffectExecutionKind.ModelModifier,
		hook: PolicyEffectHook.KingdomVillageRaidBlock,
		aggregation: PolicyEffectAggregationKind.AnyBlock,
		valueUnit: PolicyEffectValueUnit.BooleanFlag,
		fundingMode: PolicyEffectFundingMode.Unscaled,
		fundingStrategy: PolicyEffectFundingStrategy.None,
		payloadSchemaVersion: 1,
		playerDisplayName: PolicyCompatibilityTexts.Get("Module_kingdomVillageRaidBan_playerDisplayName"),
		editableUnderstandingPrompt: PolicyGenerationPromptTexts.Get("Module_kingdomVillageRaidBan_editableUnderstandingPrompt", "A kingdom village raid ban is a national rule from the publisher prohibiting its own kingdom's lords and lord parties from proactively burning, attacking or raiding any village. The target is automatically fixed to the publisher's kingdom; foreign kingdoms cannot be selected. It is not a global shield protecting the publisher's villages and does not prevent foreign lords from burning villages."),
		editableEvaluationPrompt: PolicyGenerationPromptTexts.Get("Module_kingdomVillageRaidBan_editableEvaluationPrompt", "Judge only whether the policy explicitly establishes a continuing rule against village raids by lords of the publisher's own kingdom. When matched, activate the complete prohibition without intensity or probability assessment. Multiple active policies still produce only one prohibition. General care for the population, improved security, protected agriculture, punishment of enemies or defense of certain villages does not qualify without prohibiting one's own lords from burning villages."),
		targetRefresh: PolicyEffectTargetRefreshKind.FrozenCanonicalIds,
		targetBinding: PolicyEffectTargetBindingKind.IssuerKingdom,
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public override bool TryNormalizeTypedPayload(
		KingdomVillageRaidBanPayload payload,
		string scope,
		out KingdomVillageRaidBanPayload normalizedPayload,
		out string error)
	{
		normalizedPayload = payload;
		if (!string.IsNullOrWhiteSpace(scope)
			&& !string.Equals(scope, PolicyEffectScopes.Kingdom, StringComparison.OrdinalIgnoreCase))
		{
			error = "kingdomVillageRaidBan supports kingdom policy scope only";
			return false;
		}
		return base.TryNormalizeTypedPayload(payload, scope, out normalizedPayload, out error);
	}

	public override PolicyEffectPrepareResult PrepareTyped(
		PolicyEffectCompileContext context,
		KingdomVillageRaidBanPayload payload)
	{
		if (!string.Equals(context?.SourceScope, PolicyEffectScopes.Kingdom, StringComparison.OrdinalIgnoreCase)
			|| !TryGetSingleKingdomId(context.TargetSet, out _))
		{
			return PolicyEffectPrepareResult.Rejected(
				"kingdomVillageRaidBan must bind exactly one publisher kingdom target");
		}
		return base.PrepareTyped(context, payload);
	}

	public override string DescribeTypedPayload(KingdomVillageRaidBanPayload payload)
	{
		return payload == null ? string.Empty : PolicyCompatibilityTexts.Get("RawPayloadVillageRaidBan");
	}

	public IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(
		PolicyEffectPreparedInstance preparedInstance)
	{
		if (preparedInstance?.Instance?.Payload is not KingdomVillageRaidBanPayload
			|| !TryGetSingleKingdomId(preparedInstance.Instance.TargetSet, out string kingdomId))
		{
			return Array.Empty<PolicyEffectModelContribution>();
		}

		return new[]
		{
			new PolicyEffectModelContribution
			{
				InstanceId = preparedInstance.Instance.InstanceId,
				ModuleId = Id,
				Hook = Descriptor.Hook,
				TargetKind = PolicyEffectTargetKind.Kingdom,
				TargetId = kingdomId,
				Value = 1f,
				DisplayText = DescribeTypedPayload((KingdomVillageRaidBanPayload)preparedInstance.Instance.Payload)
			}
		};
	}

	protected override bool TryValidateRawPayload(JToken rawPayload, out string error)
	{
		if (rawPayload is not JObject payloadObject)
		{
			error = "kingdomVillageRaidBan payload must be an empty object {}";
			return false;
		}
		foreach (JProperty property in payloadObject.Properties())
		{
			if (!string.Equals(property.Name, "moduleId", StringComparison.Ordinal)
				&& !string.Equals(property.Name, "schemaVersion", StringComparison.Ordinal))
			{
				error = "kingdomVillageRaidBan payload does not allow field: " + property.Name;
				return false;
			}
		}
		error = string.Empty;
		return true;
	}

	private static bool TryGetSingleKingdomId(
		PolicyEffectCanonicalTargetSet targetSet,
		out string kingdomId)
	{
		kingdomId = string.Empty;
		HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string candidate in targetSet?.KingdomIds ?? new List<string>())
		{
			string normalized = (candidate ?? string.Empty).Trim();
			if (normalized.Length > 0)
			{
				ids.Add(normalized);
			}
		}
		if (ids.Count != 1)
		{
			return false;
		}
		foreach (string id in ids)
		{
			kingdomId = id;
		}
		return true;
	}
}
