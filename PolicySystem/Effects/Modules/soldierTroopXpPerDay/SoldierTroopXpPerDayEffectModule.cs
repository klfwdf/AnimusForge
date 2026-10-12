using System;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.SoldierTroopXpPerDayEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class SoldierTroopXpPerDayEffectModule
	: PolicyEffectModuleBase<SoldierTroopXpDailyPayload>,
		IDailyPolicyEffectModule,
		ICompensatingDailyPolicyEffectModule
{
	private const int ReceiptFrameworkReserveBytes = 4 * 1024;

	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "soldierTroopXpPerDay",
		order: 142,
		legacyIds: Array.Empty<string>(),
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[]
		{
			PolicyEffectTargetKind.Settlement,
			PolicyEffectTargetKind.Clan,
			PolicyEffectTargetKind.Kingdom,
			PolicyEffectTargetKind.Hero,
		},
		targetKinds: new[] { PolicyEffectTargetKind.Clan },
		cueTerms: new[] { PolicyCompatibilityTexts.Get("Module_soldierTroopXpPerDay_Original_21") },
		retrievalText: PolicyGenerationPromptTexts.Get("Module_soldierTroopXpPerDay_retrievalText", "Internal runtime module: grant ordinary soldiers in the target clan's lord parties and fief garrisons daily troop experience while the policy is active."),
		catalogSummary: PolicyGenerationPromptTexts.Get("Module_soldierTroopXpPerDay_catalogSummary", "Internal: daily troop experience for clan soldiers"),
		mainInstruction: PolicyGenerationPromptTexts.Get("Module_soldierTroopXpPerDay_mainInstruction", "Internal module; not exposed to the model."),
		postprocessRule: PolicyGenerationPromptTexts.Get("Module_soldierTroopXpPerDay_postprocessRule", "Internal module; do not output it directly."),
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateIntegerValueSchema(),
		family: PolicyEffectFamily.Military,
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
		displayGroup: "soldierTroopXp",
		playerDisplayName: PolicyCompatibilityTexts.Get("Module_soldierTroopXpOnce_Original_20"),
		targetProjection: PolicyEffectTargetProjectionKind.None,
		targetRefresh: PolicyEffectTargetRefreshKind.FrozenCanonicalIds,
		allowIndependentClanTargets: true,
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	protected override bool TryValidateRawPayload(JToken rawPayload, out string error)
	{
		JToken value = rawPayload?["value"];
		if (value?.Type != JTokenType.Integer)
		{
			error = "soldierTroopXpPerDay value must be an integer";
			return false;
		}
		try
		{
			long parsed = value.Value<long>();
			if (parsed <= 0 || parsed > SoldierTroopXpEffectModule.MaximumDailyXpPerTroop)
			{
				error = "soldierTroopXpPerDay value must be between 1 and 100";
				return false;
			}
		}
		catch
		{
			error = "soldierTroopXpPerDay value exceeds the integer range";
			return false;
		}
		error = string.Empty;
		return true;
	}

	public override bool TryNormalizeTypedPayload(
		SoldierTroopXpDailyPayload payload,
		string scope,
		out SoldierTroopXpDailyPayload normalizedPayload,
		out string error)
	{
		_ = scope;
		normalizedPayload = payload;
		if (!TryValidateEnvelope(payload, out error))
		{
			return false;
		}
		if (payload.Value <= 0 || payload.Value > SoldierTroopXpEffectModule.MaximumDailyXpPerTroop)
		{
			error = "soldierTroopXpPerDay value must be between 1 and 100";
			return false;
		}
		error = string.Empty;
		return true;
	}

	public PolicyEffectExecutionResult ExecuteDaily(PolicyEffectExecutionContext context)
	{
		if (!TryValidateContext(context, out PolicyEffectInstance instance, out SoldierTroopXpDailyPayload payload, out string clanId, out string error))
		{
			return Failed(error, false);
		}
		if (context.GameBridge == null)
		{
			return Failed("soldierTroopXpPerDay requires a game bridge", false);
		}
		if (!context.GameBridge.TryPrepareClanTroopXp(new[] { clanId }, payload.Value, out JToken plan, out string prepareError))
		{
			return Failed("soldierTroopXpPerDay preflight failed for " + clanId + ": " + prepareError, true);
		}
		if (!TryValidateJournal(plan, payload.Value, out int plannedParties, out int plannedStacks, out long plannedXp, out string planError))
		{
			return Failed("soldierTroopXpPerDay bridge returned an invalid plan: " + planError, false);
		}
		if (plannedParties == 0 || plannedStacks == 0)
		{
			return Skipped(context, instance, payload.Value, clanId, "no eligible clan troop stack");
		}

		PolicyEffectExecutionReceipt plannedReceipt = BuildAppliedReceipt(
			context, instance, payload.Value, clanId, plan, plannedParties, plannedStacks, plannedXp, "planned");
		if (!FitsReceiptBudget(plannedReceipt))
		{
			return Skipped(context, instance, payload.Value, clanId,
				"rollback journal exceeds the bounded receipt budget; zero mutation");
		}

		bool appliedExactly = context.GameBridge.TryApplyClanTroopXp(
			plan,
			instance.Reason,
			out JToken journal,
			out int appliedParties,
			out int appliedStacks,
			out long totalAppliedXp,
			out string bridgeError);
		bool hasAppliedJournal = TryValidateJournal(
			journal,
			payload.Value,
			out int journalParties,
			out int journalStacks,
			out long journalTotalXp,
			out string journalError)
			&& journalParties > 0
			&& journalStacks > 0;

		if (!appliedExactly)
		{
			if (!hasAppliedJournal)
			{
				return Failed("soldierTroopXpPerDay apply failed before mutation: "
					+ FirstNonEmpty(bridgeError, journalError), true);
			}
			PolicyEffectExecutionReceipt partialReceipt = BuildAppliedReceipt(
				context, instance, payload.Value, clanId, journal,
				journalParties, journalStacks, journalTotalXp,
				"partial failure: " + bridgeError);
			return Failed(
				"soldierTroopXpPerDay apply failed after partial mutation: " + bridgeError,
				true,
				partialReceipt);
		}

		if (!hasAppliedJournal
			|| appliedParties != journalParties
			|| appliedStacks != journalStacks
			|| totalAppliedXp != journalTotalXp)
		{
			if (hasAppliedJournal)
			{
				PolicyEffectExecutionReceipt recoveryReceipt = BuildAppliedReceipt(
					context, instance, payload.Value, clanId, journal,
					journalParties, journalStacks, journalTotalXp,
					"summary mismatch");
				return Failed("soldierTroopXpPerDay apply result and rollback journal disagree", false, recoveryReceipt);
			}
			return Failed("soldierTroopXpPerDay apply result lacks a valid rollback journal: "
				+ FirstNonEmpty(journalError, "summary mismatch"), false);
		}

		PolicyEffectExecutionReceipt receipt = BuildAppliedReceipt(
			context, instance, payload.Value, clanId, journal,
			journalParties, journalStacks, journalTotalXp, "applied");
		if (!FitsReceiptBudget(receipt))
		{
			return Failed("soldierTroopXpPerDay applied rollback journal exceeds receipt budget", false, receipt);
		}
		return new PolicyEffectExecutionResult
		{
			Status = PolicyEffectExecutionStatus.Applied,
			Receipt = receipt
		};
	}

	public PolicyEffectExecutionResult CompensateDaily(PolicyEffectExecutionContext context)
	{
		if (!TryValidateContext(context, out PolicyEffectInstance instance, out SoldierTroopXpDailyPayload payload, out string clanId, out string error))
		{
			return Failed(error, false);
		}
		PolicyEffectExecutionReceipt existing = context.ExistingReceipt;
		JObject applied = existing?.AppliedPayload as JObject;
		if (context.GameBridge == null || existing == null || applied == null)
		{
			return Failed("soldierTroopXpPerDay compensation requires game bridge and receipt", false);
		}
		if (existing.Status != PolicyEffectExecutionStatus.Applied
			|| !string.Equals(existing.ModuleId, ModuleDescriptor.Id, StringComparison.Ordinal)
			|| !string.Equals(existing.InstanceId, instance.InstanceId, StringComparison.Ordinal)
			|| !string.Equals(applied.Value<string>("clanId"), clanId, StringComparison.OrdinalIgnoreCase)
			|| applied.Value<int?>("xpPerTroop") != payload.Value
			|| applied["journal"] == null)
		{
			return Failed("soldierTroopXpPerDay compensation receipt identity or payload does not match", false);
		}

		JToken journal = applied["journal"];
		string digest = SoldierTroopXpOnceEffectModule.ComputeDigest(journal);
		bool validJournal = TryValidateJournal(
			journal, payload.Value, out int parties, out int stacks, out long totalXp, out string journalError);
		if (!string.Equals(applied.Value<string>("journalSha256"), digest, StringComparison.Ordinal)
			|| !validJournal
			|| parties != applied.Value<int?>("parties")
			|| stacks != applied.Value<int?>("stacks")
			|| totalXp != applied.Value<long?>("totalXp"))
		{
			return Failed("soldierTroopXpPerDay compensation journal mismatch: " + journalError, false);
		}
		if (!context.GameBridge.TryRestoreClanTroopXp(
			journal,
			"compensation:" + instance.Reason,
			out int restoredStacks,
			out string bridgeError))
		{
			return Failed("soldierTroopXpPerDay compensation failed: " + bridgeError, false);
		}

		return new PolicyEffectExecutionResult
		{
			Status = PolicyEffectExecutionStatus.RolledBack,
			Receipt = new PolicyEffectExecutionReceipt
			{
				ReceiptId = context.IdempotencyKey + ":compensate",
				InstanceId = instance.InstanceId,
				PolicyId = instance.PolicyId,
				ModuleId = ModuleDescriptor.Id,
				TargetSet = instance.TargetSet,
				Status = PolicyEffectExecutionStatus.RolledBack,
				RequestedValue = existing.AppliedValue,
				AppliedValue = -existing.AppliedValue,
				RequestedPayload = applied.DeepClone(),
				AppliedPayload = new JObject
				{
					["clanId"] = clanId,
					["restoredStacks"] = restoredStacks,
					["journalSha256"] = digest
				},
				CampaignDay = context.CampaignDay,
				Message = "compensated"
			}
		};
	}

	private static bool TryValidateContext(
		PolicyEffectExecutionContext context,
		out PolicyEffectInstance instance,
		out SoldierTroopXpDailyPayload payload,
		out string clanId,
		out string error)
	{
		instance = context?.PreparedInstance?.Instance;
		payload = instance?.Payload as SoldierTroopXpDailyPayload;
		clanId = (context?.TargetId ?? string.Empty).Trim();
		if (instance == null || string.IsNullOrWhiteSpace(context.PreparedInstance.IdempotencyKey))
		{
			error = "soldierTroopXpPerDay requires an execution context and idempotency key";
			return false;
		}
		if (!string.Equals(instance.ModuleId, ModuleDescriptor.Id, StringComparison.Ordinal) || payload == null)
		{
			error = "soldierTroopXpPerDay payload does not match the module";
			return false;
		}
		string normalizedClanId = clanId;
		if (context.TargetKind != PolicyEffectTargetKind.Clan
			|| clanId.Length == 0
			|| instance.TargetSet?.ClanIds?.Exists(id => string.Equals(id, normalizedClanId, StringComparison.OrdinalIgnoreCase)) != true)
		{
			error = "soldierTroopXpPerDay requires valid frozen clan targets";
			return false;
		}
		error = string.Empty;
		return true;
	}

	private static bool TryValidateJournal(
		JToken journal,
		int xpPerTroop,
		out int parties,
		out int stacks,
		out long totalXp,
		out string error)
	{
		if (journal is not JObject root || root.Value<int?>("x") != xpPerTroop)
		{
			parties = 0;
			stacks = 0;
			totalXp = 0;
			error = "journal XP value does not match the daily payload";
			return false;
		}
		return SoldierTroopXpOnceEffectModule.TrySummarizeJournal(
			journal, out parties, out stacks, out totalXp, out error);
	}

	private static PolicyEffectExecutionReceipt BuildAppliedReceipt(
		PolicyEffectExecutionContext context,
		PolicyEffectInstance instance,
		int xpPerTroop,
		string clanId,
		JToken journal,
		int parties,
		int stacks,
		long totalXp,
		string message)
	{
		JToken journalClone = journal?.DeepClone() ?? JValue.CreateNull();
		return new PolicyEffectExecutionReceipt
		{
			ReceiptId = context.IdempotencyKey + ":apply",
			InstanceId = instance.InstanceId,
			PolicyId = instance.PolicyId,
			ModuleId = ModuleDescriptor.Id,
			TargetSet = instance.TargetSet,
			Status = PolicyEffectExecutionStatus.Applied,
			RequestedValue = xpPerTroop,
			AppliedValue = totalXp,
			RequestedPayload = new JObject { ["value"] = xpPerTroop },
			AppliedPayload = new JObject
			{
				["clanId"] = clanId,
				["xpPerTroop"] = xpPerTroop,
				["parties"] = parties,
				["stacks"] = stacks,
				["totalXp"] = totalXp,
				["journalSha256"] = SoldierTroopXpOnceEffectModule.ComputeDigest(journalClone),
				["journal"] = journalClone
			},
			CampaignDay = context.CampaignDay,
			Message = message ?? string.Empty
		};
	}

	private static PolicyEffectExecutionResult Skipped(
		PolicyEffectExecutionContext context,
		PolicyEffectInstance instance,
		int xpPerTroop,
		string clanId,
		string message)
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
				RequestedValue = xpPerTroop,
				AppliedValue = 0,
				RequestedPayload = new JObject { ["value"] = xpPerTroop },
				AppliedPayload = new JObject { ["clanId"] = clanId, ["parties"] = 0, ["stacks"] = 0, ["totalXp"] = 0 },
				CampaignDay = context.CampaignDay,
				Message = message ?? string.Empty
			}
		};
	}

	private static bool FitsReceiptBudget(PolicyEffectExecutionReceipt receipt)
	{
		int maximumReceiptBytes = PolicyEffectSaveCodec.MaxReceiptPayloadBytes - ReceiptFrameworkReserveBytes;
		return Encoding.UTF8.GetByteCount(JObject.FromObject(receipt).ToString(Formatting.None)) <= maximumReceiptBytes;
	}

	private static string FirstNonEmpty(params string[] values)
	{
		foreach (string value in values ?? Array.Empty<string>())
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				return value;
			}
		}
		return "unknown error";
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
