using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.KingdomStabilityNextDayOnceEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class KingdomStabilityNextDayOncePayload : NumericPolicyEffectPayload
{
}

internal sealed class KingdomStabilityNextDayOnceEffectModule : NumericPolicyEffectModuleBase<KingdomStabilityNextDayOncePayload>, IScheduledOncePolicyEffectModule
{
	private sealed class AppliedTarget
	{
		internal string KingdomId { get; set; } = string.Empty;

		internal int RequestedDelta { get; set; }

		internal int BeforeValue { get; set; }

		internal int AfterValue { get; set; }

		internal int ActualDelta => AfterValue - BeforeValue;
	}

	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "kingdomStabilityNextDayOnce",
		order: 121,
		legacyIds: Array.Empty<string>(),
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Kingdom },
		targetKinds: new[] { PolicyEffectTargetKind.Kingdom },
		cueTerms: new[] { "内部稳定度次日一次性" },
		retrievalText: "内部运行模块：政策通过后的下一游戏日一次性王国稳定度变化。",
		catalogSummary: "内部：次日王国稳定度变化",
		mainInstruction: "内部模块，不向模型暴露。",
		postprocessRule: "内部模块，不得直接输出。",
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Stability,
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
		displayGroup: "kingdomStability");

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	protected override bool TryNormalizeNumericValue(float rawValue, string scope, out float normalizedValue, out string error)
	{
		normalizedValue = rawValue;
		if (string.Equals(scope, PolicyEffectScopes.Local, StringComparison.OrdinalIgnoreCase))
		{
			error = "kingdomStabilityNextDayOnce 不支持地方政策作用域";
			return false;
		}
		if (float.IsNaN(rawValue) || float.IsInfinity(rawValue))
		{
			error = "kingdomStabilityNextDayOnce 的 value 必须是有限数字";
			return false;
		}
		normalizedValue = (float)Math.Round(rawValue, MidpointRounding.AwayFromZero);
		error = string.Empty;
		return true;
	}

	public PolicyEffectExecutionResult ExecuteScheduledOnce(PolicyEffectExecutionContext context)
	{
		if (IsCommitted(context?.ExistingReceipt))
		{
			return new PolicyEffectExecutionResult
			{
				Status = PolicyEffectExecutionStatus.AlreadyApplied,
				Receipt = context.ExistingReceipt
			};
		}
		if (!TryValidateContext(context, out PolicyEffectInstance instance, out KingdomStabilityNextDayOncePayload payload, out string error))
		{
			return Failed(error, false);
		}
		if (context.GameBridge == null)
		{
			return Failed("kingdomStabilityNextDayOnce 缺少 game bridge", false);
		}

		int requestedDelta = (int)payload.Value;
		List<AppliedTarget> applied = new List<AppliedTarget>();
		foreach (string kingdomId in DistinctIds(instance.TargetSet.KingdomIds))
		{
			if (!context.GameBridge.TryAdjustKingdomStability(
				kingdomId,
				requestedDelta,
				instance.Reason,
				out int beforeValue,
				out int afterValue,
				out string bridgeError))
			{
				if (afterValue != beforeValue)
				{
					// A bridge failure is normally side-effect free. If the host could not
					// restore itself exactly, include the residual mutation in the same
					// transaction receipt so coordinator compensation can finish it safely.
					applied.Add(new AppliedTarget
					{
						KingdomId = kingdomId,
						RequestedDelta = requestedDelta,
						BeforeValue = beforeValue,
						AfterValue = afterValue
					});
				}
				PolicyEffectExecutionReceipt partialReceipt = BuildAppliedReceipt(
					context, instance, requestedDelta, applied);
				partialReceipt.Message = "partial failure: " + bridgeError;
				return Failed(
					"kingdomStabilityNextDayOnce apply failed for " + kingdomId + ": " + bridgeError,
					true,
					partialReceipt);
			}
			applied.Add(new AppliedTarget
			{
				KingdomId = kingdomId,
				RequestedDelta = requestedDelta,
				BeforeValue = beforeValue,
				AfterValue = afterValue
			});
		}

		PolicyEffectExecutionReceipt receipt = BuildAppliedReceipt(context, instance, requestedDelta, applied);
		return new PolicyEffectExecutionResult { Status = PolicyEffectExecutionStatus.Applied, Receipt = receipt };
	}

	private static PolicyEffectExecutionReceipt BuildAppliedReceipt(
		PolicyEffectExecutionContext context,
		PolicyEffectInstance instance,
		int requestedDelta,
		IEnumerable<AppliedTarget> applied)
	{
		List<AppliedTarget> values = applied.ToList();
		JArray targets = new JArray(values.Select(target => new JObject
		{
			["kingdomId"] = target.KingdomId,
			["requestedDelta"] = target.RequestedDelta,
			["before"] = target.BeforeValue,
			["after"] = target.AfterValue,
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

	public PolicyEffectExecutionResult CompensateScheduledOnce(PolicyEffectExecutionContext context)
	{
		if (!TryValidateContext(context, out PolicyEffectInstance instance, out KingdomStabilityNextDayOncePayload payload, out string error))
		{
			return Failed(error, false);
		}
		PolicyEffectExecutionReceipt appliedReceipt = context.ExistingReceipt;
		if (appliedReceipt == null)
		{
			return Failed("kingdomStabilityNextDayOnce compensation 缺少执行回执", false);
		}
		if (context.GameBridge == null)
		{
			return Failed("kingdomStabilityNextDayOnce compensation 缺少 game bridge", false);
		}
		if (appliedReceipt.Status == PolicyEffectExecutionStatus.RolledBack)
		{
			return new PolicyEffectExecutionResult
			{
				Status = PolicyEffectExecutionStatus.AlreadyApplied,
				Receipt = appliedReceipt
			};
		}

		if (!TryReadTargets(appliedReceipt, out List<AppliedTarget> targets, out string receiptError))
		{
			return Failed("kingdomStabilityNextDayOnce compensation receipt invalid: " + receiptError, false);
		}
		if (!TryRestoreApplied(context, instance, targets, out string compensationError))
		{
			return Failed("kingdomStabilityNextDayOnce compensation incomplete: " + compensationError, false);
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

	private static bool TryRestoreApplied(
		PolicyEffectExecutionContext context,
		PolicyEffectInstance instance,
		IReadOnlyList<AppliedTarget> applied,
		out string error)
	{
		List<string> failures = new List<string>();
		for (int index = applied.Count - 1; index >= 0; index--)
		{
			AppliedTarget target = applied[index];
			if (!context.GameBridge.TryRestoreKingdomStability(
				target.KingdomId,
				target.AfterValue,
				target.BeforeValue,
				"compensation:" + instance.Reason,
				out int restoredValue,
				out string bridgeError))
			{
				failures.Add(target.KingdomId + ": " + bridgeError
					+ " expectedAfter=" + target.AfterValue.ToString(CultureInfo.InvariantCulture)
					+ " restoreTo=" + target.BeforeValue.ToString(CultureInfo.InvariantCulture)
					+ " actual=" + restoredValue.ToString(CultureInfo.InvariantCulture));
			}
		}
		error = string.Join("; ", failures);
		return failures.Count == 0;
	}

	private static bool TryReadTargets(
		PolicyEffectExecutionReceipt receipt,
		out List<AppliedTarget> result,
		out string error)
	{
		result = new List<AppliedTarget>();
		error = string.Empty;
		if (receipt?.AppliedPayload?["targets"] is not JArray targets)
		{
			error = "targets are missing";
			return false;
		}
		HashSet<string> seenKingdomIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (JToken targetToken in targets)
		{
			if (targetToken is not JObject target)
			{
				error = "target entry is not an object";
				return false;
			}
			string kingdomId = ((string)target["kingdomId"] ?? string.Empty).Trim();
			if (kingdomId.Length == 0)
			{
				error = "target kingdomId is missing";
				return false;
			}
			if (!seenKingdomIds.Add(kingdomId))
			{
				error = "duplicate target kingdomId: " + kingdomId;
				return false;
			}
			if (!TryReadInt(target["before"], out int beforeValue)
				|| !TryReadInt(target["after"], out int afterValue))
			{
				error = "target " + kingdomId
					+ " lacks exact before/after values; legacy inverse-delta compensation is unsafe";
				return false;
			}
			TryReadInt(target["requestedDelta"], out int requestedDelta);
			result.Add(new AppliedTarget
			{
				KingdomId = kingdomId,
				RequestedDelta = requestedDelta,
				BeforeValue = beforeValue,
				AfterValue = afterValue
			});
		}
		return true;
	}

	private static bool TryReadInt(JToken token, out int value)
	{
		value = 0;
		if (token?.Type != JTokenType.Integer)
		{
			return false;
		}
		try
		{
			long parsed = token.Value<long>();
			if (parsed < int.MinValue || parsed > int.MaxValue)
			{
				return false;
			}
			value = (int)parsed;
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryValidateContext(
		PolicyEffectExecutionContext context,
		out PolicyEffectInstance instance,
		out KingdomStabilityNextDayOncePayload payload,
		out string error)
	{
		instance = context?.PreparedInstance?.Instance;
		payload = instance?.Payload as KingdomStabilityNextDayOncePayload;
		if (instance == null || string.IsNullOrWhiteSpace(context.PreparedInstance.IdempotencyKey))
		{
			error = "kingdomStabilityNextDayOnce 缺少执行上下文或幂等键";
			return false;
		}
		if (!string.Equals(instance.ModuleId, ModuleDescriptor.Id, StringComparison.Ordinal) || payload == null)
		{
			error = "kingdomStabilityNextDayOnce payload 与模块不匹配";
			return false;
		}
		if (instance.TargetSet?.KingdomIds == null || !DistinctIds(instance.TargetSet.KingdomIds).Any())
		{
			error = "kingdomStabilityNextDayOnce 缺少有效王国目标";
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

	private static bool IsCommitted(PolicyEffectExecutionReceipt receipt)
	{
		return receipt != null
			&& (receipt.Status == PolicyEffectExecutionStatus.Applied
				|| receipt.Status == PolicyEffectExecutionStatus.AlreadyApplied);
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
}
