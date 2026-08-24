using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.ClanInfluenceNextDayOnceEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class ClanInfluenceNextDayOncePayload : NumericPolicyEffectPayload
{
}

internal sealed class ClanInfluenceNextDayOnceEffectModule : NumericPolicyEffectModuleBase<ClanInfluenceNextDayOncePayload>, IScheduledOncePolicyEffectModule
{
	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "clanInfluenceNextDayOnce",
		order: 101,
		legacyIds: Array.Empty<string>(),
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Settlement, PolicyEffectTargetKind.Clan, PolicyEffectTargetKind.Kingdom, PolicyEffectTargetKind.Hero },
		targetKinds: new[] { PolicyEffectTargetKind.Clan },
		cueTerms: new[] { "内部影响力一次性" },
		retrievalText: "内部运行模块：政策通过后下一游戏日的一次性家族影响力变化。",
		catalogSummary: "内部：家族影响力一次性变化",
		mainInstruction: "内部模块，不向模型暴露。",
		postprocessRule: "内部模块，不得直接输出。",
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Governance,
		executionKind: PolicyEffectExecutionKind.ScheduledOnce,
		hook: PolicyEffectHook.DailyScheduler,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PointsOnce,
		fundingMode: PolicyEffectFundingMode.Unscaled,
		fundingStrategy: PolicyEffectFundingStrategy.None,
		payloadSchemaVersion: 1,
		supportsRollback: true,
		supportsIdempotency: true,
		promptVisible: false,
		displayGroup: "clanInfluence");

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public override PolicyEffectPrepareResult PrepareTyped(
		PolicyEffectCompileContext context,
		ClanInfluenceNextDayOncePayload payload)
	{
		PolicyEffectPrepareResult prepared = base.PrepareTyped(context, payload);
		if (prepared?.Success == true
			&& !HasClanTargets(prepared.PreparedInstance?.Instance?.TargetSet))
		{
			return PolicyEffectPrepareResult.Rejected(
				"clanInfluenceNextDayOnce requires at least one non-empty clan target ID");
		}
		return prepared;
	}

	public PolicyEffectExecutionResult ExecuteScheduledOnce(PolicyEffectExecutionContext context)
	{
		if (!TryValidateContext(context, out PolicyEffectInstance instance, out ClanInfluenceNextDayOncePayload payload, out string error))
		{
			return Failed(error, false);
		}
		if (IsCommitted(context.ExistingReceipt))
		{
			return new PolicyEffectExecutionResult
			{
				Status = PolicyEffectExecutionStatus.AlreadyApplied,
				Receipt = context.ExistingReceipt
			};
		}
		if (context.GameBridge == null)
		{
			return Failed("clanInfluenceNextDayOnce 缺少 game bridge", false);
		}

		List<InfluenceTargetReceipt> applied = new List<InfluenceTargetReceipt>();
		foreach (string clanId in DistinctIds(instance.TargetSet.ClanIds))
		{
			if (!context.GameBridge.TryChangeClanInfluence(
				clanId,
				payload.Value,
				instance.Reason,
				out float before,
				out float after,
				out string bridgeError))
			{
				PolicyEffectExecutionReceipt partialReceipt = BuildAppliedReceipt(context, instance, payload.Value, applied);
				partialReceipt.Message = "partial failure: " + bridgeError;
				return Failed(
					"clanInfluenceNextDayOnce apply failed for " + clanId + ": " + bridgeError,
					true,
					partialReceipt);
			}
			applied.Add(new InfluenceTargetReceipt(clanId, payload.Value, before, after));
		}

		PolicyEffectExecutionReceipt receipt = BuildAppliedReceipt(context, instance, payload.Value, applied);
		return new PolicyEffectExecutionResult
		{
			Status = PolicyEffectExecutionStatus.Applied,
			Receipt = receipt
		};
	}

	public PolicyEffectExecutionResult CompensateScheduledOnce(PolicyEffectExecutionContext context)
	{
		if (!TryValidateContext(context, out PolicyEffectInstance instance, out ClanInfluenceNextDayOncePayload payload, out string error))
		{
			return Failed(error, false);
		}
		PolicyEffectExecutionReceipt appliedReceipt = context.ExistingReceipt;
		if (appliedReceipt == null)
		{
			return Failed("clanInfluenceNextDayOnce compensation 缺少执行回执", false);
		}
		if (context.GameBridge == null)
		{
			return Failed("clanInfluenceNextDayOnce compensation 缺少 game bridge", false);
		}
		if (appliedReceipt.Status == PolicyEffectExecutionStatus.RolledBack)
		{
			return new PolicyEffectExecutionResult
			{
				Status = PolicyEffectExecutionStatus.AlreadyApplied,
				Receipt = appliedReceipt
			};
		}
		if (!TryReadTargets(appliedReceipt, out List<InfluenceTargetReceipt> targets, out string receiptError))
		{
			return Failed("clanInfluenceNextDayOnce compensation receipt invalid: " + receiptError, false);
		}
		if (!TryRestoreApplied(context, instance, targets, out string compensationError))
		{
			return Failed("clanInfluenceNextDayOnce compensation incomplete: " + compensationError, false);
		}
		PolicyEffectExecutionReceipt receipt = new PolicyEffectExecutionReceipt
		{
			ReceiptId = context.IdempotencyKey + ":compensate",
			InstanceId = instance.InstanceId,
			PolicyId = instance.PolicyId,
			ModuleId = ModuleDescriptor.Id,
			TargetSet = instance.TargetSet,
			Status = PolicyEffectExecutionStatus.RolledBack,
			RequestedValue = appliedReceipt.AppliedValue,
			AppliedValue = -appliedReceipt.AppliedValue,
			RequestedPayload = appliedReceipt.AppliedPayload?.DeepClone(),
			AppliedPayload = new JObject { ["restoredTargets"] = targets.Count },
			CampaignDay = context.CampaignDay,
			Message = "compensated"
		};
		return new PolicyEffectExecutionResult { Status = PolicyEffectExecutionStatus.RolledBack, Receipt = receipt };
	}

	private static PolicyEffectExecutionReceipt BuildAppliedReceipt(
		PolicyEffectExecutionContext context,
		PolicyEffectInstance instance,
		float requestedDelta,
		IEnumerable<InfluenceTargetReceipt> applied)
	{
		List<InfluenceTargetReceipt> values = applied.ToList();
		JArray targets = new JArray(values.Select(target => new JObject
		{
			["clanId"] = target.ClanId,
			["requestedDelta"] = target.RequestedDelta,
			["before"] = target.Before,
			["after"] = target.After,
			["actualDelta"] = target.ActualDelta
		}));
		return new PolicyEffectExecutionReceipt
		{
			ReceiptId = context.IdempotencyKey + ":apply",
			InstanceId = instance.InstanceId,
			PolicyId = instance.PolicyId,
			ModuleId = ModuleDescriptor.Id,
			TargetSet = instance.TargetSet,
			Status = PolicyEffectExecutionStatus.Applied,
			RequestedValue = requestedDelta,
			AppliedValue = values.Sum(target => target.ActualDelta),
			RequestedPayload = new JObject { ["value"] = requestedDelta },
			AppliedPayload = new JObject { ["targets"] = targets },
			CampaignDay = context.CampaignDay,
			Message = "applied"
		};
	}

	private static bool TryRestoreApplied(
		PolicyEffectExecutionContext context,
		PolicyEffectInstance instance,
		IReadOnlyList<InfluenceTargetReceipt> applied,
		out string error)
	{
		List<string> failures = new List<string>();
		for (int index = applied.Count - 1; index >= 0; index--)
		{
			InfluenceTargetReceipt target = applied[index];
			if (!context.GameBridge.TryRestoreClanInfluence(
				target.ClanId,
				target.After,
				target.Before,
				"compensation:" + instance.Reason,
				out float restored,
				out string bridgeError)
				|| !NearlyEqual(restored, target.Before))
			{
				failures.Add(target.ClanId + ": " + bridgeError
					+ " expected=" + target.Before.ToString("R", CultureInfo.InvariantCulture)
					+ " actual=" + restored.ToString("R", CultureInfo.InvariantCulture));
			}
		}
		error = string.Join("; ", failures);
		return failures.Count == 0;
	}

	private static bool TryReadTargets(
		PolicyEffectExecutionReceipt receipt,
		out List<InfluenceTargetReceipt> result,
		out string error)
	{
		result = new List<InfluenceTargetReceipt>();
		error = string.Empty;
		if (receipt?.AppliedPayload?["targets"] is not JArray targets)
		{
			error = "targets are missing";
			return false;
		}
		HashSet<string> seenClanIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (JToken targetToken in targets)
		{
			if (targetToken is not JObject target)
			{
				error = "target entry is not an object";
				return false;
			}
			string clanId = ((string)target["clanId"] ?? string.Empty).Trim();
			if (clanId.Length == 0)
			{
				error = "target clanId is missing";
				return false;
			}
			if (!seenClanIds.Add(clanId))
			{
				error = "duplicate target clanId: " + clanId;
				return false;
			}
			if (!TryReadFiniteFloat(target["before"], out float before)
				|| !TryReadFiniteFloat(target["after"], out float after))
			{
				error = "target " + clanId
					+ " lacks finite before/after values; legacy inverse-delta compensation is unsafe";
				return false;
			}
			TryReadFiniteFloat(target["requestedDelta"], out float requestedDelta);
			result.Add(new InfluenceTargetReceipt(
				clanId,
				requestedDelta,
				before,
				after));
		}
		return true;
	}

	private static bool TryReadFiniteFloat(JToken token, out float value)
	{
		value = 0f;
		if (token == null || (token.Type != JTokenType.Float && token.Type != JTokenType.Integer))
		{
			return false;
		}
		try
		{
			value = token.Value<float>();
			return !float.IsNaN(value) && !float.IsInfinity(value);
		}
		catch
		{
			return false;
		}
	}

	private static bool TryValidateContext(
		PolicyEffectExecutionContext context,
		out PolicyEffectInstance instance,
		out ClanInfluenceNextDayOncePayload payload,
		out string error)
	{
		instance = context?.PreparedInstance?.Instance;
		payload = instance?.Payload as ClanInfluenceNextDayOncePayload;
		if (instance == null || string.IsNullOrWhiteSpace(context.PreparedInstance.IdempotencyKey))
		{
			error = "clanInfluenceNextDayOnce 缺少执行上下文或幂等键";
			return false;
		}
		if (!string.Equals(instance.ModuleId, ModuleDescriptor.Id, StringComparison.Ordinal) || payload == null)
		{
			error = "clanInfluenceNextDayOnce payload 与模块不匹配";
			return false;
		}
		if (!HasClanTargets(instance.TargetSet))
		{
			error = "clanInfluenceNextDayOnce requires at least one non-empty clan target ID";
			return false;
		}
		error = string.Empty;
		return true;
	}

	private static IEnumerable<string> DistinctIds(IEnumerable<string> values)
	{
		return (values ?? Array.Empty<string>())
			.Select(value => (value ?? string.Empty).Trim())
			.Where(value => value.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase);
	}

	private static bool HasClanTargets(PolicyEffectCanonicalTargetSet targetSet)
	{
		return targetSet?.ClanIds != null && DistinctIds(targetSet.ClanIds).Any();
	}

	private static bool IsCommitted(PolicyEffectExecutionReceipt receipt)
	{
		return receipt != null
			&& (receipt.Status == PolicyEffectExecutionStatus.Applied
				|| receipt.Status == PolicyEffectExecutionStatus.AlreadyApplied);
	}

	private static bool NearlyEqual(float left, float right)
	{
		return Math.Abs(left - right) <= Math.Max(0.0001f, Math.Max(Math.Abs(left), Math.Abs(right)) * 0.00001f);
	}

	private static PolicyEffectExecutionResult Failed(
		string error,
		bool retryable,
		PolicyEffectExecutionReceipt receipt = null)
	{
		return new PolicyEffectExecutionResult
		{
			Status = PolicyEffectExecutionStatus.Failed,
			Error = error ?? string.Empty,
			Retryable = retryable,
			Receipt = receipt
		};
	}

	private sealed class InfluenceTargetReceipt
	{
		internal InfluenceTargetReceipt(string clanId, float requestedDelta, float before, float after)
		{
			ClanId = clanId;
			RequestedDelta = requestedDelta;
			Before = before;
			After = after;
		}

		internal string ClanId { get; }
		internal float RequestedDelta { get; }
		internal float Before { get; }
		internal float After { get; }
		internal float ActualDelta => After - Before;
	}
}
