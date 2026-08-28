using System;
using Newtonsoft.Json.Linq;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.HeroGoldPerDayEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class HeroGoldPerDayEffectModule : PolicyEffectModuleBase<HeroGoldDeltaPayload>,
	IDailyPolicyEffectModule,
	ICompensatingDailyPolicyEffectModule,
	IAtomicHeroGoldPolicyEffectModule
{
	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "heroGoldPerDay",
		order: 132,
		legacyIds: Array.Empty<string>(),
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Hero, PolicyEffectTargetKind.Settlement },
		targetKinds: new[] { PolicyEffectTargetKind.Hero },
		cueTerms: new[] { "内部人物第纳尔每日" },
		retrievalText: "内部运行模块：政策有效期间每日人物第纳尔变化。",
		catalogSummary: "内部：人物第纳尔每日变化",
		mainInstruction: "内部模块，不向模型暴露。",
		postprocessRule: "内部模块，不得直接输出。",
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateIntegerValueSchema(),
		family: PolicyEffectFamily.Fiscal,
		executionKind: PolicyEffectExecutionKind.DailyMutation,
		hook: PolicyEffectHook.DailyScheduler,
		aggregation: PolicyEffectAggregationKind.IntegerDelta,
		valueUnit: PolicyEffectValueUnit.GoldPerDay,
		fundingMode: PolicyEffectFundingMode.Unscaled,
		fundingStrategy: PolicyEffectFundingStrategy.None,
		payloadSchemaVersion: 1,
		supportsRollback: true,
		supportsIdempotency: true,
		promptVisible: false,
		displayGroup: "heroGold",
		targetProjection: PolicyEffectTargetProjectionKind.SettlementOwnerClanLeader,
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	protected override bool TryValidateRawPayload(JToken rawPayload, out string error)
	{
		if (!HeroGoldPayloadValidation.HasIntegerProperty(rawPayload, "value"))
		{
			error = "heroGoldPerDay 的 value 必须是 32 位整数";
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
			error = "heroGoldPerDay 不接受 int.MinValue";
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

	public PolicyEffectExecutionResult ExecuteDaily(PolicyEffectExecutionContext context)
	{
		if (!TryValidateContext(context, out PolicyEffectInstance instance, out HeroGoldDeltaPayload payload, out string heroId, out string error))
		{
			return Failed(error, false);
		}
		if (context.GameBridge == null)
		{
			return Failed("heroGoldPerDay 缺少 game bridge", false);
		}
		if (!context.GameBridge.TryReadHeroGold(heroId, out bool available, out int current, out string readError))
		{
			return Failed("heroGoldPerDay preflight failed for " + heroId + ": " + readError, true);
		}
		long expected = (long)current + payload.Value;
		if (!available || expected < 0 || expected > int.MaxValue)
		{
			return Skipped(context, instance, payload.Value, heroId, available ? "insufficient balance or overflow" : "target unavailable");
		}
		if (!context.GameBridge.TryChangeHeroGoldExact(
			heroId,
			payload.Value,
			instance.Reason,
			out available,
			out int before,
			out int after,
			out string bridgeError)
			|| !available
			|| (long)after - before != payload.Value)
		{
			return Failed("heroGoldPerDay apply failed for " + heroId + ": " + bridgeError, true);
		}

		PolicyEffectExecutionReceipt receipt = BuildReceipt(
			context,
			instance,
			PolicyEffectExecutionStatus.Applied,
			payload.Value,
			heroId,
			before,
			after,
			"applied");
		return new PolicyEffectExecutionResult { Status = PolicyEffectExecutionStatus.Applied, Receipt = receipt };
	}

	public PolicyEffectExecutionResult CompensateDaily(PolicyEffectExecutionContext context)
	{
		PolicyEffectInstance instance = context?.PreparedInstance?.Instance;
		HeroGoldDeltaPayload payload = instance?.Payload as HeroGoldDeltaPayload;
		JObject target = context?.ExistingReceipt?.AppliedPayload?["target"] as JObject;
		if (instance == null || payload == null || target == null || context.GameBridge == null)
		{
			return Failed("heroGoldPerDay compensation 缺少执行上下文、回执或 game bridge", false);
		}
		if (!string.Equals(instance.ModuleId, ModuleDescriptor.Id, StringComparison.Ordinal)
			|| !string.Equals(context.ExistingReceipt.ModuleId, ModuleDescriptor.Id, StringComparison.Ordinal)
			|| !string.Equals(context.ExistingReceipt.InstanceId, instance.InstanceId, StringComparison.Ordinal)
			|| context.ExistingReceipt.Status != PolicyEffectExecutionStatus.Applied)
		{
			return Failed("heroGoldPerDay compensation 回执身份或状态不匹配", false);
		}
		string heroId = ((string)target["heroId"] ?? string.Empty).Trim();
		if (heroId.Length == 0
			|| instance.TargetSet?.HeroIds?.Exists(id => string.Equals(id, heroId, StringComparison.OrdinalIgnoreCase)) != true
			|| target["requestedDelta"]?.Type != JTokenType.Integer
			|| target["before"]?.Type != JTokenType.Integer
			|| target["after"]?.Type != JTokenType.Integer
			|| target["actualDelta"]?.Type != JTokenType.Integer)
		{
			return Failed("heroGoldPerDay compensation 回执无效", false);
		}
		int before;
		int expectedAfter;
		int requestedDelta;
		int actualDelta;
		try
		{
			requestedDelta = target["requestedDelta"].Value<int>();
			before = target["before"].Value<int>();
			expectedAfter = target["after"].Value<int>();
			actualDelta = target["actualDelta"].Value<int>();
		}
		catch
		{
			return Failed("heroGoldPerDay compensation 回执数值越界", false);
		}
		if (requestedDelta != payload.Value
			|| actualDelta != requestedDelta
			|| (long)expectedAfter - before != actualDelta)
		{
			return Failed("heroGoldPerDay compensation 回执不守恒", false);
		}
		if (!context.GameBridge.TryRestoreHeroGold(
			heroId,
			expectedAfter,
			before,
			"compensation:" + instance.Reason,
			out int restored,
			out string bridgeError)
			|| restored != before)
		{
			return Failed("heroGoldPerDay compensation failed for " + heroId + ": " + bridgeError, false);
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
			AppliedPayload = new JObject { ["heroId"] = heroId, ["restored"] = restored },
			CampaignDay = context.CampaignDay,
			Message = "compensated"
		};
		return new PolicyEffectExecutionResult { Status = PolicyEffectExecutionStatus.RolledBack, Receipt = receipt };
	}

	private static bool TryValidateContext(
		PolicyEffectExecutionContext context,
		out PolicyEffectInstance instance,
		out HeroGoldDeltaPayload payload,
		out string heroId,
		out string error)
	{
		instance = context?.PreparedInstance?.Instance;
		payload = instance?.Payload as HeroGoldDeltaPayload;
		heroId = (context?.TargetId ?? string.Empty).Trim();
		if (instance == null || string.IsNullOrWhiteSpace(context.PreparedInstance.IdempotencyKey))
		{
			error = "heroGoldPerDay 缺少执行上下文或幂等键";
			return false;
		}
		if (!string.Equals(instance.ModuleId, ModuleDescriptor.Id, StringComparison.Ordinal) || payload == null)
		{
			error = "heroGoldPerDay payload 与模块不匹配";
			return false;
		}
		if (context.TargetKind != PolicyEffectTargetKind.Hero || heroId.Length == 0)
		{
			error = "heroGoldPerDay 缺少有效人物目标";
			return false;
		}
		error = string.Empty;
		return true;
	}

	private static PolicyEffectExecutionResult Skipped(
		PolicyEffectExecutionContext context,
		PolicyEffectInstance instance,
		int requestedDelta,
		string heroId,
		string message)
	{
		return new PolicyEffectExecutionResult
		{
			Status = PolicyEffectExecutionStatus.Skipped,
			Receipt = BuildReceipt(context, instance, PolicyEffectExecutionStatus.Skipped, requestedDelta, heroId, 0, 0, message)
		};
	}

	private static PolicyEffectExecutionReceipt BuildReceipt(
		PolicyEffectExecutionContext context,
		PolicyEffectInstance instance,
		PolicyEffectExecutionStatus status,
		int requestedDelta,
		string heroId,
		int before,
		int after,
		string message)
	{
		return new PolicyEffectExecutionReceipt
		{
			ReceiptId = context.IdempotencyKey + (status == PolicyEffectExecutionStatus.Skipped ? ":skip" : ":apply"),
			InstanceId = instance.InstanceId,
			PolicyId = instance.PolicyId,
			ModuleId = ModuleDescriptor.Id,
			TargetSet = instance.TargetSet,
			Status = status,
			RequestedValue = requestedDelta,
			AppliedValue = status == PolicyEffectExecutionStatus.Applied ? after - before : 0f,
			RequestedPayload = new JObject { ["value"] = requestedDelta },
			AppliedPayload = new JObject
			{
				["target"] = new JObject
				{
					["heroId"] = heroId,
					["requestedDelta"] = requestedDelta,
					["before"] = before,
					["after"] = after,
					["actualDelta"] = status == PolicyEffectExecutionStatus.Applied ? after - before : 0
				}
			},
			CampaignDay = context.CampaignDay,
			Message = message
		};
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
}
