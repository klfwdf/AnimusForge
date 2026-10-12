using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.KingdomStabilityOnceEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class KingdomStabilityOncePayload : NumericPolicyEffectPayload
{
}

internal sealed class KingdomStabilityOnceEffectModule : NumericPolicyEffectModuleBase<KingdomStabilityOncePayload>, IOneShotPolicyEffectModule
{
	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "kingdomStabilityOnce",
		order: 90,
		legacyIds: new[] { "kingdomStabilityDailyDelta" },
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Kingdom },
		targetKinds: new[] { PolicyEffectTargetKind.Kingdom },
		cueTerms: new[] { PolicyCompatibilityTexts.Get("Module_kingdomStabilityOnce_Original_10"), PolicyCompatibilityTexts.Get("Module_kingdomStabilityOnce_Original_11"), PolicyCompatibilityTexts.Get("Module_kingdomStabilityOnce_Original_12"), PolicyCompatibilityTexts.Get("Module_kingdomStabilityOnce_Original_13"), PolicyCompatibilityTexts.Get("Module_kingdomStabilityOnce_Original_14"), PolicyCompatibilityTexts.Get("Module_kingdomStabilityOnce_Original_15"), PolicyCompatibilityTexts.Get("Module_kingdomStabilityOnce_Original_16") },
		retrievalText: PolicyGenerationPromptTexts.Get("Module_kingdomStabilityOnce_retrievalText", "Kingdom stability, political stability, central authority, national unrest, rebellion risk, institutional shock, and political crises; a one-time kingdom stability change when the policy formally takes effect. Not available to local policies."),
		catalogSummary: PolicyGenerationPromptTexts.Get("Module_kingdomStabilityOnce_catalogSummary", "One-time kingdom stability on policy activation"),
		playerDisplayName: PolicyCompatibilityTexts.Get("Module_kingdomStabilityOnce_OriginalName"),
		mainInstruction: PolicyGenerationPromptTexts.Get("Module_kingdomStabilityOnce_mainInstruction", "If the policy directly creates a kingdom-level shock to political stability or unrest when it formally takes effect, describe the target, direction, intensity, and reason in numericIntent. Local policies cannot use it. Do not output module IDs or values."),
		postprocessRule: PolicyGenerationPromptTexts.Get("Module_kingdomStabilityOnce_postprocessRule", "A one-time stability change when the policy formally takes effect in the target kingdom. Positive values raise stability and negative values lower it; the final changes value must be an integer. Only kingdom or vassal policies may use it; local policies may not. It does not accumulate daily over the duration or stack by town count. Pure administration or ordinary local numeric changes usually warrant 0. Clear changes to public confidence, fiscal prestige, vassal trust, or royal legitimacy usually warrant ±4 to 7; major reforms, national mobilization, noble conflict, or severe crises warrant ±7 to 14; the brink of civil war, national survival, systemic collapse, or decisive victory usually warrant ±14 to 22. Extreme cases with direct support may be proportionately higher."),
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Stability,
		executionKind: PolicyEffectExecutionKind.OneShot,
		hook: PolicyEffectHook.KingdomStabilityOnActivation,
		aggregation: PolicyEffectAggregationKind.IntegerDelta,
		valueUnit: PolicyEffectValueUnit.PointsOnce,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Linear,
		payloadSchemaVersion: 1,
		supportsRollback: true,
		supportsIdempotency: true,
		promptVisible: false,
		displayGroup: "kingdomStability",
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	protected override bool TryNormalizeNumericValue(float rawValue, string scope, out float normalizedValue, out string error)
	{
		normalizedValue = rawValue;
		error = string.Empty;
		if (string.Equals(scope, PolicyEffectScopes.Local, StringComparison.OrdinalIgnoreCase))
		{
			error = "kingdomStabilityOnce does not support local policy scope";
			return false;
		}
		if (float.IsNaN(rawValue) || float.IsInfinity(rawValue))
		{
			error = "The value must be finite";
			return false;
		}
		normalizedValue = (float)Math.Round(rawValue, MidpointRounding.AwayFromZero);
		return true;
	}

	public PolicyEffectExecutionResult ApplyOnce(PolicyEffectExecutionContext context)
	{
		if (context?.ExistingReceipt != null
			&& (context.ExistingReceipt.Status == PolicyEffectExecutionStatus.Applied
				|| context.ExistingReceipt.Status == PolicyEffectExecutionStatus.AlreadyApplied))
		{
			return new PolicyEffectExecutionResult
			{
				Status = PolicyEffectExecutionStatus.AlreadyApplied,
				Receipt = context.ExistingReceipt
			};
		}
		if (!TryValidateContext(context, out PolicyEffectPreparedInstance prepared, out PolicyEffectInstance instance, out KingdomStabilityOncePayload payload, out string error))
		{
			return Failed(error);
		}
		if (context.GameBridge == null)
		{
			return Failed("kingdomStabilityOnce requires a shared game bridge");
		}
		int requestedDelta = (int)payload.Value;
		List<KeyValuePair<string, int>> applied = new List<KeyValuePair<string, int>>();
		foreach (string kingdomId in instance.TargetSet.KingdomIds.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			if (!context.GameBridge.TryAdjustKingdomStability(
				kingdomId,
				requestedDelta,
				instance.Reason,
				out int actualDelta,
				out string bridgeError))
			{
				RollbackAppliedTargets(context, instance, applied, out string rollbackError);
				return Failed("kingdomStabilityOnce apply failed for " + kingdomId + ": " + bridgeError
					+ (string.IsNullOrWhiteSpace(rollbackError) ? string.Empty : "; rollback=" + rollbackError));
			}
			applied.Add(new KeyValuePair<string, int>(kingdomId, actualDelta));
		}
		JArray targetReceipts = new JArray(applied.Select(item => new JObject
		{
			["kingdomId"] = item.Key,
			["requestedDelta"] = requestedDelta,
			["actualDelta"] = item.Value
		}));
		PolicyEffectExecutionReceipt receipt = new PolicyEffectExecutionReceipt
		{
			ReceiptId = context.IdempotencyKey + ":apply",
			InstanceId = instance.InstanceId,
			PolicyId = instance.PolicyId,
			ModuleId = ModuleDescriptor.Id,
			TargetSet = instance.TargetSet,
			Status = PolicyEffectExecutionStatus.Applied,
			RequestedValue = requestedDelta,
			AppliedValue = applied.Sum(item => item.Value),
			RequestedPayload = new JObject { ["value"] = requestedDelta },
			AppliedPayload = new JObject { ["targets"] = targetReceipts },
			CampaignDay = context.CampaignDay,
			Message = "applied"
		};
		return new PolicyEffectExecutionResult { Status = PolicyEffectExecutionStatus.Applied, Receipt = receipt };
	}

	public PolicyEffectExecutionResult RollbackOnce(PolicyEffectExecutionContext context)
	{
		if (!TryValidateContext(context, out PolicyEffectPreparedInstance prepared, out PolicyEffectInstance instance, out KingdomStabilityOncePayload payload, out string error))
		{
			return Failed(error);
		}
		if (context.GameBridge == null)
		{
			return Failed("kingdomStabilityOnce rollback requires a shared game bridge");
		}
		PolicyEffectExecutionReceipt appliedReceipt = context.ExistingReceipt;
		if (appliedReceipt == null)
		{
			return Failed("kingdomStabilityOnce rollback requires an execution receipt");
		}
		if (appliedReceipt.Status == PolicyEffectExecutionStatus.RolledBack)
		{
			return new PolicyEffectExecutionResult
			{
				Status = PolicyEffectExecutionStatus.AlreadyApplied,
				Receipt = appliedReceipt
			};
		}
		List<KeyValuePair<string, int>> appliedTargets = ReadAppliedTargets(appliedReceipt, instance, payload);
		List<string> failures = new List<string>();
		int rolledBackTotal = 0;
		for (int index = appliedTargets.Count - 1; index >= 0; index--)
		{
			KeyValuePair<string, int> target = appliedTargets[index];
			if (!context.GameBridge.TryAdjustKingdomStability(
				target.Key,
				-target.Value,
				"rollback:" + instance.Reason,
				out int actualDelta,
				out string bridgeError))
			{
				failures.Add(target.Key + ": " + bridgeError);
				continue;
			}
			rolledBackTotal += actualDelta;
			if (actualDelta != -target.Value)
			{
				failures.Add(target.Key + ": requested=" + (-target.Value).ToString(CultureInfo.InvariantCulture)
					+ " actual=" + actualDelta.ToString(CultureInfo.InvariantCulture));
			}
		}
		if (failures.Count > 0)
		{
			return Failed("kingdomStabilityOnce rollback incomplete: " + string.Join("; ", failures));
		}
		PolicyEffectExecutionReceipt receipt = new PolicyEffectExecutionReceipt
		{
			ReceiptId = context.IdempotencyKey + ":rollback",
			InstanceId = instance.InstanceId,
			PolicyId = instance.PolicyId,
			ModuleId = ModuleDescriptor.Id,
			TargetSet = instance.TargetSet,
			Status = PolicyEffectExecutionStatus.RolledBack,
			RequestedValue = appliedReceipt.AppliedValue,
			AppliedValue = rolledBackTotal,
			RequestedPayload = appliedReceipt.AppliedPayload?.DeepClone(),
			AppliedPayload = new JObject { ["rolledBackTotal"] = rolledBackTotal },
			CampaignDay = context.CampaignDay,
			Message = "rolledBack"
		};
		return new PolicyEffectExecutionResult { Status = PolicyEffectExecutionStatus.RolledBack, Receipt = receipt };
	}

	private static bool TryValidateContext(
		PolicyEffectExecutionContext context,
		out PolicyEffectPreparedInstance prepared,
		out PolicyEffectInstance instance,
		out KingdomStabilityOncePayload payload,
		out string error)
	{
		prepared = context?.PreparedInstance;
		instance = prepared?.Instance;
		payload = instance?.Payload as KingdomStabilityOncePayload;
		error = ValidateProposal(prepared, instance, payload);
		return error.Length == 0;
	}

	private static PolicyEffectExecutionResult Failed(string error)
	{
		return new PolicyEffectExecutionResult
		{
			Status = PolicyEffectExecutionStatus.Failed,
			Error = error ?? string.Empty
		};
	}

	private static bool RollbackAppliedTargets(
		PolicyEffectExecutionContext context,
		PolicyEffectInstance instance,
		IEnumerable<KeyValuePair<string, int>> applied,
		out string error)
	{
		List<string> failures = new List<string>();
		foreach (KeyValuePair<string, int> target in applied.Reverse())
		{
			if (!context.GameBridge.TryAdjustKingdomStability(
				target.Key,
				-target.Value,
				"rollback:" + instance.Reason,
				out int actual,
				out string bridgeError)
				|| actual != -target.Value)
			{
				failures.Add(target.Key + ": " + bridgeError);
			}
		}
		error = string.Join("; ", failures);
		return failures.Count == 0;
	}

	private static List<KeyValuePair<string, int>> ReadAppliedTargets(
		PolicyEffectExecutionReceipt receipt,
		PolicyEffectInstance instance,
		KingdomStabilityOncePayload payload)
	{
		List<KeyValuePair<string, int>> result = new List<KeyValuePair<string, int>>();
		if (receipt?.AppliedPayload?["targets"] is JArray targets)
		{
			foreach (JObject target in targets.OfType<JObject>())
			{
				string kingdomId = ((string)target["kingdomId"] ?? string.Empty).Trim();
				int actualDelta = (int?)target["actualDelta"] ?? 0;
				if (kingdomId.Length > 0)
				{
					result.Add(new KeyValuePair<string, int>(kingdomId, actualDelta));
				}
			}
		}
		if (result.Count == 0)
		{
			int fallback = (int)Math.Round(payload.Value, MidpointRounding.AwayFromZero);
			foreach (string kingdomId in instance.TargetSet.KingdomIds.Distinct(StringComparer.OrdinalIgnoreCase))
			{
				result.Add(new KeyValuePair<string, int>(kingdomId, fallback));
			}
		}
		return result;
	}

	private static string ValidateProposal(
		PolicyEffectPreparedInstance prepared,
		PolicyEffectInstance instance,
		KingdomStabilityOncePayload payload)
	{
		if (prepared == null || instance == null)
		{
			return "kingdomStabilityOnce requires an execution context";
		}
		if (string.IsNullOrWhiteSpace(prepared.IdempotencyKey))
		{
			return "kingdomStabilityOnce requires an idempotency key";
		}
		if (!string.Equals(instance.ModuleId, ModuleDescriptor.Id, StringComparison.Ordinal) || payload == null)
		{
			return "kingdomStabilityOnce payload does not match the module";
		}
		if (instance.TargetSet?.KingdomIds == null || instance.TargetSet.KingdomIds.Count <= 0)
		{
			return "kingdomStabilityOnce requires a kingdom target";
		}
		foreach (string kingdomId in instance.TargetSet.KingdomIds)
		{
			if (string.IsNullOrWhiteSpace(kingdomId))
			{
				return "kingdomStabilityOnce kingdom target is invalid";
			}
		}
		return string.Empty;
	}
}
