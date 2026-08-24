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
		cueTerms: new[] { "稳定度", "政局稳定", "中央权威", "国家动荡", "叛乱风险", "制度冲击", "政治危机" },
		retrievalText: "王国稳定度、政局稳定、中央权威、国家动荡、叛乱风险、制度冲击、政治危机；政策正式生效时的一次性王国稳定度变化，地方政策不可用。",
		catalogSummary: "政策生效时一次性王国稳定度变化",
		mainInstruction: "政策若会在正式生效时直接造成王国层面的政治稳定或动荡冲击，请在 numericIntent 中说明目标、方向、强弱与理由；地方政策不可用，不要输出模块 ID 或数值。",
		postprocessRule: "目标王国正式生效时一次性稳定度变化，正数提高、负数降低，最终 changes 值必须是整数；只用于王国或附庸国政策，地方政策禁止，不随持续天数每日累加，也不按城镇数量叠加。纯行政或普通地方数值变化通常为 0；明显改变民众信心、财政威望、封臣信任或王权合法性通常 ±4～7，重大改革、全国动员、贵族冲突或严重危机 ±7～14，内战边缘、国家存亡、体系崩溃或决定性胜利通常 ±14～22；极端且直接支持时可相称更高。",
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
		displayGroup: "kingdomStability");

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	protected override bool TryNormalizeNumericValue(float rawValue, string scope, out float normalizedValue, out string error)
	{
		normalizedValue = rawValue;
		error = string.Empty;
		if (string.Equals(scope, PolicyEffectScopes.Local, StringComparison.OrdinalIgnoreCase))
		{
			error = "kingdomStabilityOnce 不支持地方政策作用域";
			return false;
		}
		if (float.IsNaN(rawValue) || float.IsInfinity(rawValue))
		{
			error = "数值必须是有限数字";
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
			return Failed("kingdomStabilityOnce 缺少共享 game bridge");
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
			return Failed("kingdomStabilityOnce rollback 缺少共享 game bridge");
		}
		PolicyEffectExecutionReceipt appliedReceipt = context.ExistingReceipt;
		if (appliedReceipt == null)
		{
			return Failed("kingdomStabilityOnce rollback 缺少执行回执");
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
			return "kingdomStabilityOnce 缺少执行上下文";
		}
		if (string.IsNullOrWhiteSpace(prepared.IdempotencyKey))
		{
			return "kingdomStabilityOnce 缺少幂等键";
		}
		if (!string.Equals(instance.ModuleId, ModuleDescriptor.Id, StringComparison.Ordinal) || payload == null)
		{
			return "kingdomStabilityOnce payload 与模块不匹配";
		}
		if (instance.TargetSet?.KingdomIds == null || instance.TargetSet.KingdomIds.Count <= 0)
		{
			return "kingdomStabilityOnce 缺少王国目标";
		}
		foreach (string kingdomId in instance.TargetSet.KingdomIds)
		{
			if (string.IsNullOrWhiteSpace(kingdomId))
			{
				return "kingdomStabilityOnce 王国目标无效";
			}
		}
		return string.Empty;
	}
}
