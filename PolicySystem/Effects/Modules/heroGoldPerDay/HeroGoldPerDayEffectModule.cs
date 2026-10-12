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
		cueTerms: new[] { PolicyCompatibilityTexts.Get("Module_heroGoldPerDay_Original_6") },
		retrievalText: PolicyGenerationPromptTexts.Get("Module_heroGoldPerDay_retrievalText", "Internal runtime module: daily changes to personal denars while the policy is active."),
		catalogSummary: PolicyGenerationPromptTexts.Get("Module_heroGoldPerDay_catalogSummary", "Internal: daily personal denar change"),
		playerDisplayName: PolicyCompatibilityTexts.Get("Module_heroGoldPerDay_OriginalName"),
		mainInstruction: PolicyGenerationPromptTexts.Get("Module_heroGoldPerDay_mainInstruction", "Internal module; not exposed to the model."),
		postprocessRule: PolicyGenerationPromptTexts.Get("Module_heroGoldPerDay_postprocessRule", "Internal module; do not output it directly."),
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
			error = "heroGoldPerDay value must be a 32-bit integer";
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
			error = "heroGoldPerDay does not accept int.MinValue";
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
			return Failed("heroGoldPerDay requires a game bridge", false);
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
			return Failed("heroGoldPerDay compensation requires execution context, receipt and game bridge", false);
		}
		if (!string.Equals(instance.ModuleId, ModuleDescriptor.Id, StringComparison.Ordinal)
			|| !string.Equals(context.ExistingReceipt.ModuleId, ModuleDescriptor.Id, StringComparison.Ordinal)
			|| !string.Equals(context.ExistingReceipt.InstanceId, instance.InstanceId, StringComparison.Ordinal)
			|| context.ExistingReceipt.Status != PolicyEffectExecutionStatus.Applied)
		{
			return Failed("heroGoldPerDay compensation receipt identity or status does not match", false);
		}
		string heroId = ((string)target["heroId"] ?? string.Empty).Trim();
		if (heroId.Length == 0
			|| instance.TargetSet?.HeroIds?.Exists(id => string.Equals(id, heroId, StringComparison.OrdinalIgnoreCase)) != true
			|| target["requestedDelta"]?.Type != JTokenType.Integer
			|| target["before"]?.Type != JTokenType.Integer
			|| target["after"]?.Type != JTokenType.Integer
			|| target["actualDelta"]?.Type != JTokenType.Integer)
		{
			return Failed("heroGoldPerDay compensation receipt is invalid", false);
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
			return Failed("heroGoldPerDay compensation receipt values are out of range", false);
		}
		if (requestedDelta != payload.Value
			|| actualDelta != requestedDelta
			|| (long)expectedAfter - before != actualDelta)
		{
			return Failed("heroGoldPerDay compensation receipt does not conserve the balance", false);
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
			error = "heroGoldPerDay requires an execution context and idempotency key";
			return false;
		}
		if (!string.Equals(instance.ModuleId, ModuleDescriptor.Id, StringComparison.Ordinal) || payload == null)
		{
			error = "heroGoldPerDay payload does not match the module";
			return false;
		}
		if (context.TargetKind != PolicyEffectTargetKind.Hero || heroId.Length == 0)
		{
			error = "heroGoldPerDay requires valid character targets";
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
