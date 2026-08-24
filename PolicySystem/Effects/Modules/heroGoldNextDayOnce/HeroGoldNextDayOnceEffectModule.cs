using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.HeroGoldNextDayOnceEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class HeroGoldNextDayOnceEffectModule : PolicyEffectModuleBase<HeroGoldDeltaPayload>,
	IScheduledOncePolicyEffectModule,
	IAtomicHeroGoldPolicyEffectModule
{
	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "heroGoldNextDayOnce",
		order: 131,
		legacyIds: Array.Empty<string>(),
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Hero, PolicyEffectTargetKind.Settlement },
		targetKinds: new[] { PolicyEffectTargetKind.Hero },
		cueTerms: new[] { "内部人物第纳尔一次性" },
		retrievalText: "内部运行模块：政策通过后下一游戏日的一次性人物第纳尔变化。",
		catalogSummary: "内部：人物第纳尔一次性变化",
		mainInstruction: "内部模块，不向模型暴露。",
		postprocessRule: "内部模块，不得直接输出。",
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateIntegerValueSchema(),
		family: PolicyEffectFamily.Fiscal,
		executionKind: PolicyEffectExecutionKind.ScheduledOnce,
		hook: PolicyEffectHook.DailyScheduler,
		aggregation: PolicyEffectAggregationKind.IntegerDelta,
		valueUnit: PolicyEffectValueUnit.GoldOnce,
		fundingMode: PolicyEffectFundingMode.Unscaled,
		fundingStrategy: PolicyEffectFundingStrategy.None,
		payloadSchemaVersion: 1,
		supportsRollback: true,
		supportsIdempotency: true,
		promptVisible: false,
		displayGroup: "heroGold",
		targetProjection: PolicyEffectTargetProjectionKind.SettlementOwnerClanLeader);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	protected override bool TryValidateRawPayload(JToken rawPayload, out string error)
	{
		if (!HeroGoldPayloadValidation.HasIntegerProperty(rawPayload, "value"))
		{
			error = "heroGoldNextDayOnce 的 value 必须是 32 位整数";
			return false;
		}
		error = string.Empty;
		return true;
	}

	public override bool TryNormalizeTypedPayload(
		HeroGoldDeltaPayload payload,
		string scope,
		out HeroGoldDeltaPayload normalizedPayload,
		out string error)
	{
		normalizedPayload = payload;
		if (!TryValidateEnvelope(payload, out error))
		{
			return false;
		}
		if (payload.Value == int.MinValue)
		{
			error = "heroGoldNextDayOnce 不接受 int.MinValue";
			return false;
		}
		error = string.Empty;
		return true;
	}

	public bool TryReadDelta(PolicyEffectPayload payload, out int delta)
	{
		delta = payload is HeroGoldDeltaPayload typed ? typed.Value : 0;
		return payload is HeroGoldDeltaPayload;
	}

	public PolicyEffectExecutionResult ExecuteScheduledOnce(PolicyEffectExecutionContext context)
	{
		if (!TryValidateContext(context, out PolicyEffectInstance instance, out HeroGoldDeltaPayload payload, out string error))
		{
			return Failed(error, false);
		}
		if (context.ExistingReceipt != null
			&& (context.ExistingReceipt.Status == PolicyEffectExecutionStatus.Applied
				|| context.ExistingReceipt.Status == PolicyEffectExecutionStatus.AlreadyApplied))
		{
			return new PolicyEffectExecutionResult { Status = PolicyEffectExecutionStatus.AlreadyApplied, Receipt = context.ExistingReceipt };
		}
		if (context.GameBridge == null)
		{
			return Failed("heroGoldNextDayOnce 缺少 game bridge", false);
		}
		List<string> heroIds = DistinctIds(instance.TargetSet?.HeroIds).ToList();
		if (heroIds.Count == 0)
		{
			return Skipped(context, instance, payload.Value, string.Empty, "target set empty on due day");
		}

		List<HeroGoldTargetReceipt> targets = new List<HeroGoldTargetReceipt>();
		foreach (string heroId in heroIds)
		{
			if (!context.GameBridge.TryReadHeroGold(heroId, out bool available, out int before, out string readError))
			{
				return Failed("heroGoldNextDayOnce preflight failed for " + heroId + ": " + readError, true);
			}
			if (!available || !CanApply(before, payload.Value))
			{
				return Skipped(context, instance, payload.Value, heroId, available ? "insufficient balance or overflow" : "target unavailable");
			}
			targets.Add(new HeroGoldTargetReceipt(heroId, payload.Value, before, before));
		}

		List<HeroGoldTargetReceipt> applied = new List<HeroGoldTargetReceipt>();
		foreach (HeroGoldTargetReceipt target in targets)
		{
			if (!context.GameBridge.TryChangeHeroGoldExact(
				target.HeroId,
				payload.Value,
				instance.Reason,
				out bool available,
				out int before,
				out int after,
				out string bridgeError)
				|| !available
				|| (long)after - before != payload.Value)
			{
				PolicyEffectExecutionReceipt partial = BuildAppliedReceipt(context, instance, payload.Value, applied);
				partial.Message = "partial failure: " + bridgeError;
				return Failed("heroGoldNextDayOnce apply failed for " + target.HeroId + ": " + bridgeError, true, partial);
			}
			applied.Add(new HeroGoldTargetReceipt(target.HeroId, payload.Value, before, after));
		}

		PolicyEffectExecutionReceipt receipt = BuildAppliedReceipt(context, instance, payload.Value, applied);
		return new PolicyEffectExecutionResult { Status = PolicyEffectExecutionStatus.Applied, Receipt = receipt };
	}

	public PolicyEffectExecutionResult CompensateScheduledOnce(PolicyEffectExecutionContext context)
	{
		PolicyEffectInstance instance = context?.PreparedInstance?.Instance;
		HeroGoldDeltaPayload payload = instance?.Payload as HeroGoldDeltaPayload;
		if (instance == null || payload == null || context.GameBridge == null || context.ExistingReceipt == null)
		{
			return Failed("heroGoldNextDayOnce compensation 缺少执行上下文、回执或 game bridge", false);
		}
		string receiptError = string.Empty;
		if (!string.Equals(instance.ModuleId, ModuleDescriptor.Id, StringComparison.Ordinal)
			|| !string.Equals(context.ExistingReceipt.ModuleId, ModuleDescriptor.Id, StringComparison.Ordinal)
			|| !string.Equals(context.ExistingReceipt.InstanceId, instance.InstanceId, StringComparison.Ordinal)
			|| context.ExistingReceipt.Status != PolicyEffectExecutionStatus.Applied
			|| !TryReadTargets(
				context.ExistingReceipt,
				payload.Value,
				out List<HeroGoldTargetReceipt> targets,
				out receiptError))
		{
			return Failed("heroGoldNextDayOnce compensation receipt invalid: "
				+ (string.IsNullOrWhiteSpace(receiptError) ? "identity or status mismatch" : receiptError), false);
		}
		HashSet<string> expectedHeroIds = new HashSet<string>(
			DistinctIds(instance.TargetSet?.HeroIds),
			StringComparer.OrdinalIgnoreCase);
		if (targets.Any(target => !expectedHeroIds.Contains(target.HeroId)))
		{
			return Failed("heroGoldNextDayOnce compensation receipt contains an unexpected Hero target", false);
		}
		for (int index = targets.Count - 1; index >= 0; index--)
		{
			HeroGoldTargetReceipt target = targets[index];
			if (!context.GameBridge.TryRestoreHeroGold(
				target.HeroId,
				target.After,
				target.Before,
				"compensation:" + instance.Reason,
				out int restored,
				out string bridgeError)
				|| restored != target.Before)
			{
				return Failed("heroGoldNextDayOnce compensation failed for " + target.HeroId + ": " + bridgeError, false);
			}
		}
		PolicyEffectExecutionReceipt receipt = new PolicyEffectExecutionReceipt
		{
			ReceiptId = context.IdempotencyKey + ":compensate",
			InstanceId = instance.InstanceId,
			PolicyId = instance.PolicyId,
			ModuleId = ModuleDescriptor.Id,
			TargetSet = instance.TargetSet,
			Status = PolicyEffectExecutionStatus.RolledBack,
			RequestedValue = context.ExistingReceipt.AppliedValue,
			AppliedValue = -context.ExistingReceipt.AppliedValue,
			RequestedPayload = context.ExistingReceipt.AppliedPayload?.DeepClone(),
			AppliedPayload = new JObject { ["restoredTargets"] = targets.Count },
			CampaignDay = context.CampaignDay,
			Message = "compensated"
		};
		return new PolicyEffectExecutionResult { Status = PolicyEffectExecutionStatus.RolledBack, Receipt = receipt };
	}

	private static bool TryValidateContext(
		PolicyEffectExecutionContext context,
		out PolicyEffectInstance instance,
		out HeroGoldDeltaPayload payload,
		out string error)
	{
		instance = context?.PreparedInstance?.Instance;
		payload = instance?.Payload as HeroGoldDeltaPayload;
		if (instance == null || string.IsNullOrWhiteSpace(context.PreparedInstance.IdempotencyKey))
		{
			error = "heroGoldNextDayOnce 缺少执行上下文或幂等键";
			return false;
		}
		if (!string.Equals(instance.ModuleId, ModuleDescriptor.Id, StringComparison.Ordinal) || payload == null)
		{
			error = "heroGoldNextDayOnce payload 与模块不匹配";
			return false;
		}
		error = string.Empty;
		return true;
	}

	private static bool CanApply(int before, int delta)
	{
		long after = (long)before + delta;
		return after >= 0 && after <= int.MaxValue;
	}

	private static PolicyEffectExecutionResult Skipped(
		PolicyEffectExecutionContext context,
		PolicyEffectInstance instance,
		int requestedDelta,
		string heroId,
		string reason)
	{
		return new PolicyEffectExecutionResult
		{
			Status = PolicyEffectExecutionStatus.Skipped,
			Receipt = new PolicyEffectExecutionReceipt
			{
				ReceiptId = context.IdempotencyKey + ":skip",
				InstanceId = instance.InstanceId,
				PolicyId = instance.PolicyId,
				ModuleId = ModuleDescriptor.Id,
				TargetSet = instance.TargetSet,
				Status = PolicyEffectExecutionStatus.Skipped,
				RequestedValue = requestedDelta,
				AppliedValue = 0f,
				RequestedPayload = new JObject { ["value"] = requestedDelta },
				AppliedPayload = new JObject { ["heroId"] = heroId, ["actualDelta"] = 0 },
				CampaignDay = context.CampaignDay,
				Message = reason
			}
		};
	}

	private static PolicyEffectExecutionReceipt BuildAppliedReceipt(
		PolicyEffectExecutionContext context,
		PolicyEffectInstance instance,
		int requestedDelta,
		IEnumerable<HeroGoldTargetReceipt> values)
	{
		List<HeroGoldTargetReceipt> targets = values.ToList();
		return new PolicyEffectExecutionReceipt
		{
			ReceiptId = context.IdempotencyKey + ":apply",
			InstanceId = instance.InstanceId,
			PolicyId = instance.PolicyId,
			ModuleId = ModuleDescriptor.Id,
			TargetSet = instance.TargetSet,
			Status = PolicyEffectExecutionStatus.Applied,
			RequestedValue = requestedDelta,
			AppliedValue = targets.Sum(target => (float)target.ActualDelta),
			RequestedPayload = new JObject { ["value"] = requestedDelta },
			AppliedPayload = new JObject
			{
				["targets"] = new JArray(targets.Select(target => target.ToJson()))
			},
			CampaignDay = context.CampaignDay,
			Message = "applied"
		};
	}

	private static bool TryReadTargets(
		PolicyEffectExecutionReceipt receipt,
		int expectedDelta,
		out List<HeroGoldTargetReceipt> result,
		out string error)
	{
		result = new List<HeroGoldTargetReceipt>();
		if (receipt?.AppliedPayload?["targets"] is not JArray targets)
		{
			error = "targets are missing";
			return false;
		}
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (JToken token in targets)
		{
			string heroId = ((string)token?["heroId"] ?? string.Empty).Trim();
			if (heroId.Length == 0 || !seen.Add(heroId)
				|| token?["requestedDelta"]?.Type != JTokenType.Integer
				|| token?["before"]?.Type != JTokenType.Integer
				|| token?["after"]?.Type != JTokenType.Integer
				|| token?["actualDelta"]?.Type != JTokenType.Integer)
			{
				error = "invalid or duplicate hero target";
				return false;
			}
			try
			{
				int requestedDelta = token["requestedDelta"].Value<int>();
				int before = token["before"].Value<int>();
				int after = token["after"].Value<int>();
				int actualDelta = token["actualDelta"].Value<int>();
				if (requestedDelta != expectedDelta
					|| actualDelta != requestedDelta
					|| (long)after - before != actualDelta)
				{
					error = "hero target receipt does not conserve the requested delta";
					return false;
				}
				result.Add(new HeroGoldTargetReceipt(heroId, requestedDelta, before, after));
			}
			catch
			{
				error = "hero target values are outside Int32";
				return false;
			}
		}
		if (result.Count == 0)
		{
			error = "targets are empty";
			return false;
		}
		error = string.Empty;
		return true;
	}

	private static IEnumerable<string> DistinctIds(IEnumerable<string> ids)
	{
		return (ids ?? Array.Empty<string>())
			.Select(id => (id ?? string.Empty).Trim())
			.Where(id => id.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase);
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

	private sealed class HeroGoldTargetReceipt
	{
		internal HeroGoldTargetReceipt(string heroId, int requestedDelta, int before, int after)
		{
			HeroId = heroId;
			RequestedDelta = requestedDelta;
			Before = before;
			After = after;
		}

		internal string HeroId { get; }
		internal int RequestedDelta { get; }
		internal int Before { get; }
		internal int After { get; }
		internal int ActualDelta => After - Before;

		internal JObject ToJson()
		{
			return new JObject
			{
				["heroId"] = HeroId,
				["requestedDelta"] = RequestedDelta,
				["before"] = Before,
				["after"] = After,
				["actualDelta"] = ActualDelta
			};
		}
	}
}
