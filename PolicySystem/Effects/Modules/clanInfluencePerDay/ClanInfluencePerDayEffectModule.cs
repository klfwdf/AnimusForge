using System;
using Newtonsoft.Json.Linq;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.ClanInfluencePerDayEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class ClanInfluencePerDayPayload : NumericPolicyEffectPayload
{
}

internal sealed class ClanInfluencePerDayEffectModule : NumericPolicyEffectModuleBase<ClanInfluencePerDayPayload>, IDailyPolicyEffectModule, ICompensatingDailyPolicyEffectModule
{
	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "clanInfluencePerDay",
		order: 102,
		legacyIds: Array.Empty<string>(),
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Settlement, PolicyEffectTargetKind.Clan, PolicyEffectTargetKind.Kingdom, PolicyEffectTargetKind.Hero },
		targetKinds: new[] { PolicyEffectTargetKind.Clan },
		cueTerms: new[] { PolicyCompatibilityTexts.Get("Module_clanInfluencePerDay_Original_2") },
		retrievalText: PolicyGenerationPromptTexts.Get("Module_clanInfluencePerDay_retrievalText", "Internal runtime module: daily clan influence changes while the policy is active."),
		catalogSummary: PolicyGenerationPromptTexts.Get("Module_clanInfluencePerDay_catalogSummary", "Internal: daily clan influence change"),
		playerDisplayName: PolicyCompatibilityTexts.Get("Module_clanInfluencePerDay_OriginalName"),
		mainInstruction: PolicyGenerationPromptTexts.Get("Module_clanInfluencePerDay_mainInstruction", "Internal module; not exposed to the model."),
		postprocessRule: PolicyGenerationPromptTexts.Get("Module_clanInfluencePerDay_postprocessRule", "Internal module; do not output it directly."),
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Governance,
		executionKind: PolicyEffectExecutionKind.DailyMutation,
		hook: PolicyEffectHook.DailyScheduler,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PointsPerDay,
		fundingMode: PolicyEffectFundingMode.Unscaled,
		fundingStrategy: PolicyEffectFundingStrategy.None,
		payloadSchemaVersion: 1,
		supportsRollback: true,
		supportsIdempotency: true,
		promptVisible: false,
		displayGroup: "clanInfluence",
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public PolicyEffectExecutionResult ExecuteDaily(PolicyEffectExecutionContext context)
	{
		if (!TryValidateContext(context, out PolicyEffectInstance instance, out ClanInfluencePerDayPayload payload, out string clanId, out string error))
		{
			return Failed(error, false);
		}
		if (context.GameBridge == null)
		{
			return Failed("clanInfluencePerDay requires a game bridge", false);
		}
		if (!context.GameBridge.TryChangeClanInfluence(
			clanId,
			payload.Value,
			instance.Reason,
			out float before,
			out float after,
			out string bridgeError))
		{
			return Failed("clanInfluencePerDay apply failed for " + clanId + ": " + bridgeError, true);
		}

		float actual = after - before;
		PolicyEffectExecutionReceipt receipt = new PolicyEffectExecutionReceipt
		{
			ReceiptId = context.IdempotencyKey + ":apply",
			InstanceId = instance.InstanceId,
			PolicyId = instance.PolicyId,
			ModuleId = ModuleDescriptor.Id,
			TargetSet = instance.TargetSet,
			Status = PolicyEffectExecutionStatus.Applied,
			RequestedValue = payload.Value,
			AppliedValue = actual,
			RequestedPayload = new JObject { ["value"] = payload.Value },
			AppliedPayload = new JObject
			{
				["target"] = new JObject
				{
					["clanId"] = clanId,
					["requestedDelta"] = payload.Value,
					["before"] = before,
					["after"] = after,
					["actualDelta"] = actual
				}
			},
			CampaignDay = context.CampaignDay,
			Message = "applied"
		};
		return new PolicyEffectExecutionResult
		{
			Status = PolicyEffectExecutionStatus.Applied,
			Receipt = receipt
		};
	}

	public PolicyEffectExecutionResult CompensateDaily(PolicyEffectExecutionContext context)
	{
		PolicyEffectInstance instance = context?.PreparedInstance?.Instance;
		JObject target = context?.ExistingReceipt?.AppliedPayload?["target"] as JObject;
		if (instance == null || target == null || context.GameBridge == null)
		{
			return Failed("clanInfluencePerDay compensation requires execution context, receipt and game bridge", false);
		}
		string clanId = ((string)target["clanId"] ?? string.Empty).Trim();
		float expectedAfter = (float?)target["after"] ?? float.NaN;
		float before = (float?)target["before"] ?? float.NaN;
		if (clanId.Length <= 0 || float.IsNaN(expectedAfter) || float.IsNaN(before))
		{
			return Failed("clanInfluencePerDay compensation receipt is invalid", false);
		}
		if (!context.GameBridge.TryRestoreClanInfluence(
			clanId,
			expectedAfter,
			before,
			"compensation:" + instance.Reason,
			out float restored,
			out string bridgeError)
			|| !NearlyEqual(restored, before))
		{
			return Failed("clanInfluencePerDay compensation failed for " + clanId + ": " + bridgeError
				+ " expected=" + before.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
				+ " actual=" + restored.ToString("R", System.Globalization.CultureInfo.InvariantCulture), false);
		}
		PolicyEffectExecutionReceipt receipt = new PolicyEffectExecutionReceipt
		{
			ReceiptId = context.IdempotencyKey + ":compensate",
			InstanceId = instance.InstanceId,
			PolicyId = instance.PolicyId,
			ModuleId = ModuleDescriptor.Id,
			TargetSet = instance.TargetSet,
			Status = PolicyEffectExecutionStatus.RolledBack,
			RequestedValue = before,
			AppliedValue = restored,
			RequestedPayload = context.ExistingReceipt.AppliedPayload?.DeepClone(),
			AppliedPayload = new JObject { ["clanId"] = clanId, ["restored"] = restored },
			CampaignDay = context.CampaignDay,
			Message = "compensated"
		};
		return new PolicyEffectExecutionResult { Status = PolicyEffectExecutionStatus.RolledBack, Receipt = receipt };
	}

	private static bool TryValidateContext(
		PolicyEffectExecutionContext context,
		out PolicyEffectInstance instance,
		out ClanInfluencePerDayPayload payload,
		out string clanId,
		out string error)
	{
		instance = context?.PreparedInstance?.Instance;
		payload = instance?.Payload as ClanInfluencePerDayPayload;
		clanId = (context?.TargetId ?? string.Empty).Trim();
		if (instance == null || string.IsNullOrWhiteSpace(context.PreparedInstance.IdempotencyKey))
		{
			error = "clanInfluencePerDay requires an execution context and idempotency key";
			return false;
		}
		if (!string.Equals(instance.ModuleId, ModuleDescriptor.Id, StringComparison.Ordinal) || payload == null)
		{
			error = "clanInfluencePerDay payload does not match the module";
			return false;
		}
		if (context.TargetKind != PolicyEffectTargetKind.Clan || clanId.Length <= 0)
		{
			error = "clanInfluencePerDay requires valid clan targets";
			return false;
		}
		error = string.Empty;
		return true;
	}

	private static PolicyEffectExecutionResult Failed(string error, bool retryable)
	{
		return new PolicyEffectExecutionResult
		{
			Status = PolicyEffectExecutionStatus.Failed,
			Error = error ?? string.Empty,
			Retryable = retryable
		};
	}

	private static bool NearlyEqual(float left, float right)
	{
		return Math.Abs(left - right) <= Math.Max(0.0001f, Math.Max(Math.Abs(left), Math.Abs(right)) * 0.00001f);
	}
}
