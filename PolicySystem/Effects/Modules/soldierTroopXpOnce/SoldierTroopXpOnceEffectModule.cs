using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.SoldierTroopXpOnceEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class SoldierTroopXpOncePayload : NumericPolicyEffectPayload
{
}

internal sealed class SoldierTroopXpOnceEffectModule
	: NumericPolicyEffectModuleBase<SoldierTroopXpOncePayload>, IScheduledOncePolicyEffectModule
{
	private const int MaximumXpPerTroop = 5000;
	private const int RuntimeStateVersion = 1;
	private const int RuntimeStateFrameworkReserveBytes = 4 * 1024;

	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "soldierTroopXpOnce",
		order: 141,
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
		cueTerms: new[] { PolicyCompatibilityTexts.Get("Module_soldierTroopXpOnce_Original_19") },
		retrievalText: PolicyGenerationPromptTexts.Get("Module_soldierTroopXpOnce_retrievalText", "Internal runtime module: grant ordinary soldiers in the target clan's lord parties and fief garrisons one-time troop experience on the next game day."),
		catalogSummary: PolicyGenerationPromptTexts.Get("Module_soldierTroopXpOnce_catalogSummary", "Internal: one-time troop experience for clan soldiers"),
		mainInstruction: PolicyGenerationPromptTexts.Get("Module_soldierTroopXpOnce_mainInstruction", "Internal module; not exposed to the model."),
		postprocessRule: PolicyGenerationPromptTexts.Get("Module_soldierTroopXpOnce_postprocessRule", "Internal module; do not output it directly."),
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateIntegerValueSchema(),
		family: PolicyEffectFamily.Military,
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
		displayGroup: "soldierTroopXp",
		playerDisplayName: PolicyCompatibilityTexts.Get("Module_soldierTroopXpOnce_Original_20"),
		editableUnderstandingPrompt: PolicyGenerationPromptTexts.Get("Module_soldierTroopXpOnce_editableUnderstandingPrompt", "Internal one-time execution module: on the next game day, grants the same native troop experience to eligible ordinary soldiers in all current official lord parties and town/castle garrisons of target clans."),
		editableEvaluationPrompt: PolicyGenerationPromptTexts.Get("Module_soldierTroopXpOnce_editableEvaluationPrompt", "value is one-time native experience per eligible ordinary soldier, ranging from 1 to 5000. Do not dilute by clan, party, fief or soldier counts; it does not directly upgrade troops."),
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
			error = "soldierTroopXpOnce value must be an integer";
			return false;
		}
		try
		{
			long parsed = value.Value<long>();
			if (parsed <= 0L || parsed > MaximumXpPerTroop)
			{
				error = "soldierTroopXpOnce value must be between 1 and "
					+ MaximumXpPerTroop.ToString(CultureInfo.InvariantCulture) + " inclusive";
				return false;
			}
		}
		catch
		{
			error = "soldierTroopXpOnce value exceeds the integer range";
			return false;
		}
		error = string.Empty;
		return true;
	}

	protected override bool TryNormalizeNumericValue(
		float rawValue,
		string scope,
		out float normalizedValue,
		out string error)
	{
		_ = scope;
		normalizedValue = rawValue;
		if (float.IsNaN(rawValue) || float.IsInfinity(rawValue))
		{
			error = "soldierTroopXpOnce value must be finite";
			return false;
		}
		normalizedValue = (float)Math.Round(rawValue, MidpointRounding.AwayFromZero);
		// Zero is accepted only after funding scale; raw model payload validation
		// above still requires a strictly positive integer.
		if (normalizedValue < 0f || normalizedValue > MaximumXpPerTroop)
		{
			error = "soldierTroopXpOnce funded value is outside 0 to "
				+ MaximumXpPerTroop.ToString(CultureInfo.InvariantCulture);
			return false;
		}
		error = string.Empty;
		return true;
	}

	public PolicyEffectExecutionResult ExecuteScheduledOnce(PolicyEffectExecutionContext context)
	{
		if (!TryValidateContext(context, out PolicyEffectInstance instance, out SoldierTroopXpOncePayload payload, out string error))
		{
			return Failed(error, false);
		}
		if (IsCommitted(context.ExistingReceipt))
		{
			return new PolicyEffectExecutionResult
			{
				Status = PolicyEffectExecutionStatus.AlreadyApplied,
				Receipt = context.ExistingReceipt,
				RuntimeState = context.RuntimeState?.DeepClone()
			};
		}
		if (context.GameBridge == null)
		{
			return Failed("soldierTroopXpOnce requires a game bridge", false);
		}

		int xpPerTroop = (int)payload.Value;
		if (xpPerTroop <= 0)
		{
			return Skipped(context, instance, xpPerTroop, "funding reduced troop XP to zero");
		}
		string[] clanIds = DistinctIds(instance.TargetSet?.ClanIds).ToArray();
		if (clanIds.Length == 0)
		{
			return Skipped(context, instance, xpPerTroop, "target set empty on due day");
		}

		if (!context.GameBridge.TryPrepareClanTroopXp(clanIds, xpPerTroop, out JToken plan, out string prepareError))
		{
			return Failed("soldierTroopXpOnce preflight failed: " + prepareError, true);
		}
		if (!TrySummarizeJournal(plan, out int plannedParties, out int plannedStacks, out _, out string planError))
		{
			return Failed("soldierTroopXpOnce bridge returned an invalid plan: " + planError, false);
		}
		if (plannedParties == 0 || plannedStacks == 0)
		{
			return Skipped(context, instance, xpPerTroop, "no eligible clan troop stack");
		}

		JObject plannedRuntimeState = BuildRuntimeState(plan);
		if (!FitsRuntimeStateBudget(plannedRuntimeState))
		{
			return Skipped(
				context,
				instance,
				xpPerTroop,
				"rollback journal exceeds the bounded RuntimeState budget; zero mutation");
		}

		bool appliedExactly = context.GameBridge.TryApplyClanTroopXp(
			plan,
			instance.Reason,
			out JToken journal,
			out int appliedParties,
			out int appliedStacks,
			out long totalAppliedXp,
			out string bridgeError);
		bool hasAppliedJournal = TrySummarizeJournal(
			journal,
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
				return Failed("soldierTroopXpOnce apply failed before mutation: "
					+ FirstNonEmpty(bridgeError, journalError), true);
			}
			JObject partialState = BuildRuntimeState(journal);
			if (!FitsRuntimeStateBudget(partialState))
			{
				return Failed("soldierTroopXpOnce partial rollback journal exceeds RuntimeState budget", false);
			}
			PolicyEffectExecutionReceipt partialReceipt = BuildAppliedReceipt(
				context,
				instance,
				xpPerTroop,
				journalParties,
				journalStacks,
				journalTotalXp,
				partialState.Value<string>("d"));
			partialReceipt.Message = "partial failure: " + bridgeError;
			return Failed(
				"soldierTroopXpOnce apply failed after partial mutation: " + bridgeError,
				true,
				partialReceipt,
				partialState);
		}

		if (!hasAppliedJournal
			|| appliedParties != journalParties
			|| appliedStacks != journalStacks
			|| totalAppliedXp != journalTotalXp)
		{
			if (hasAppliedJournal)
			{
				JObject recoveryState = BuildRuntimeState(journal);
				PolicyEffectExecutionReceipt recoveryReceipt = BuildAppliedReceipt(
					context,
					instance,
					xpPerTroop,
					journalParties,
					journalStacks,
					journalTotalXp,
					recoveryState.Value<string>("d"));
				return Failed(
					"soldierTroopXpOnce apply result and rollback journal disagree: summary mismatch",
					false,
					recoveryReceipt,
					recoveryState);
			}
			return Failed("soldierTroopXpOnce apply result lacks a valid rollback journal: "
				+ FirstNonEmpty(journalError, "summary mismatch"), false);
		}

		JObject runtimeState = BuildRuntimeState(journal);
		if (!FitsRuntimeStateBudget(runtimeState))
		{
			PolicyEffectExecutionReceipt emergencyReceipt = BuildAppliedReceipt(
				context,
				instance,
				xpPerTroop,
				journalParties,
				journalStacks,
				journalTotalXp,
				runtimeState.Value<string>("d"));
			return Failed(
				"soldierTroopXpOnce applied rollback journal exceeds RuntimeState budget",
				false,
				emergencyReceipt,
				runtimeState);
		}
		PolicyEffectExecutionReceipt receipt = BuildAppliedReceipt(
			context,
			instance,
			xpPerTroop,
			journalParties,
			journalStacks,
			journalTotalXp,
			runtimeState.Value<string>("d"));
		return new PolicyEffectExecutionResult
		{
			Status = PolicyEffectExecutionStatus.Applied,
			Receipt = receipt,
			RuntimeState = runtimeState
		};
	}

	public PolicyEffectExecutionResult CompensateScheduledOnce(PolicyEffectExecutionContext context)
	{
		if (!TryValidateContext(context, out PolicyEffectInstance instance, out SoldierTroopXpOncePayload payload, out string error))
		{
			return Failed(error, false);
		}
		if (context.GameBridge == null || context.ExistingReceipt == null)
		{
			return Failed("soldierTroopXpOnce compensation requires game bridge and receipt", false);
		}
		if (context.ExistingReceipt.Status == PolicyEffectExecutionStatus.RolledBack)
		{
			return new PolicyEffectExecutionResult
			{
				Status = PolicyEffectExecutionStatus.AlreadyApplied,
				Receipt = context.ExistingReceipt,
				RuntimeState = context.RuntimeState?.DeepClone()
			};
		}
		if (context.ExistingReceipt.Status != PolicyEffectExecutionStatus.Applied
			|| !string.Equals(context.ExistingReceipt.ModuleId, ModuleDescriptor.Id, StringComparison.Ordinal)
			|| !string.Equals(context.ExistingReceipt.InstanceId, instance.InstanceId, StringComparison.Ordinal))
		{
			return Failed("soldierTroopXpOnce compensation receipt identity or status mismatch", false);
		}
		if (context.RuntimeState is not JObject runtimeState
			|| runtimeState.Value<int?>("v") != RuntimeStateVersion
			|| runtimeState["j"] == null
			|| string.IsNullOrWhiteSpace(runtimeState.Value<string>("d")))
		{
			return Failed("soldierTroopXpOnce compensation rollback journal is missing", false);
		}

		JToken journal = runtimeState["j"];
		string digest = ComputeDigest(journal);
		string receiptDigest = context.ExistingReceipt.AppliedPayload?.Value<string>("journalSha256") ?? string.Empty;
		if (!string.Equals(digest, runtimeState.Value<string>("d"), StringComparison.Ordinal)
			|| !string.Equals(digest, receiptDigest, StringComparison.Ordinal))
		{
			return Failed("soldierTroopXpOnce compensation journal digest mismatch", false);
		}
		if (!TrySummarizeJournal(journal, out int parties, out int stacks, out long totalXp, out string journalError)
			|| parties != context.ExistingReceipt.AppliedPayload?.Value<int>("parties")
			|| stacks != context.ExistingReceipt.AppliedPayload?.Value<int>("stacks")
			|| totalXp != context.ExistingReceipt.AppliedPayload?.Value<long>("totalXp"))
		{
			return Failed("soldierTroopXpOnce compensation journal summary mismatch: " + journalError, false);
		}

		if (!context.GameBridge.TryRestoreClanTroopXp(
			journal,
			"compensation:" + instance.Reason,
			out int restoredStacks,
			out string bridgeError))
		{
			return Failed("soldierTroopXpOnce compensation failed: " + bridgeError, false);
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
			AppliedPayload = new JObject
			{
				["restoredStacks"] = restoredStacks,
				["journalSha256"] = digest
			},
			CampaignDay = context.CampaignDay,
			Message = "compensated"
		};
		return new PolicyEffectExecutionResult
		{
			Status = PolicyEffectExecutionStatus.RolledBack,
			Receipt = receipt,
			RuntimeState = new JObject
			{
				["v"] = RuntimeStateVersion,
				["compensated"] = true,
				["d"] = digest
			}
		};
	}

	private static PolicyEffectExecutionReceipt BuildAppliedReceipt(
		PolicyEffectExecutionContext context,
		PolicyEffectInstance instance,
		int xpPerTroop,
		int parties,
		int stacks,
		long totalXp,
		string digest)
	{
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
				["parties"] = parties,
				["stacks"] = stacks,
				["totalXp"] = totalXp,
				["journalSha256"] = digest ?? string.Empty
			},
			CampaignDay = context.CampaignDay,
			Message = "applied"
		};
	}

	private static PolicyEffectExecutionResult Skipped(
		PolicyEffectExecutionContext context,
		PolicyEffectInstance instance,
		int xpPerTroop,
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
				AppliedValue = 0f,
				RequestedPayload = new JObject { ["value"] = xpPerTroop },
				AppliedPayload = new JObject { ["parties"] = 0, ["stacks"] = 0, ["totalXp"] = 0 },
				CampaignDay = context.CampaignDay,
				Message = message ?? string.Empty
			}
		};
	}

	internal static bool TrySummarizeJournal(
		JToken journal,
		out int parties,
		out int stacks,
		out long totalXp,
		out string error)
	{
		parties = 0;
		stacks = 0;
		totalXp = 0L;
		error = string.Empty;
		if (journal is not JObject root
			|| root.Value<int?>("v") != RuntimeStateVersion
			|| root["p"] is not JArray partyArray)
		{
			error = "journal envelope is invalid";
			return false;
		}
		HashSet<string> seenParties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (JToken partyToken in partyArray)
		{
			if (partyToken is not JObject party || party["s"] is not JArray stackArray)
			{
				error = "journal party entry is invalid";
				return false;
			}
			string partyId = ((string)party["i"] ?? string.Empty).Trim();
			if (partyId.Length == 0 || !seenParties.Add(partyId) || stackArray.Count == 0)
			{
				error = "journal party identity is missing, duplicated, or empty";
				return false;
			}
			parties++;
			HashSet<string> seenTroops = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (JToken stackToken in stackArray)
			{
				if (stackToken is not JObject stack)
				{
					error = "journal stack entry is invalid";
					return false;
				}
				string troopId = ((string)stack["i"] ?? string.Empty).Trim();
				if (troopId.Length == 0
					|| !seenTroops.Add(troopId)
					|| !TryReadInt(stack["b"], out int before)
					|| !TryReadInt(stack["a"], out int after)
					|| after <= before)
				{
					error = "journal stack values are invalid";
					return false;
				}
				stacks++;
				totalXp += (long)after - before;
			}
		}
		return true;
	}

	private static JObject BuildRuntimeState(JToken journal)
	{
		JToken clone = journal?.DeepClone() ?? JValue.CreateNull();
		return new JObject
		{
			["v"] = RuntimeStateVersion,
			["d"] = ComputeDigest(clone),
			["j"] = clone
		};
	}

	private static bool FitsRuntimeStateBudget(JToken runtimeState)
	{
		int maximumModuleBytes = PolicyEffectSaveCodec.MaxRuntimeStateBytes - RuntimeStateFrameworkReserveBytes;
		return Encoding.UTF8.GetByteCount(runtimeState?.ToString(Formatting.None) ?? string.Empty)
			<= maximumModuleBytes;
	}

	internal static string ComputeDigest(JToken journal)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(journal?.ToString(Formatting.None) ?? "null");
		using SHA256 sha256 = SHA256.Create();
		return Convert.ToBase64String(sha256.ComputeHash(bytes));
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
		out SoldierTroopXpOncePayload payload,
		out string error)
	{
		instance = context?.PreparedInstance?.Instance;
		payload = instance?.Payload as SoldierTroopXpOncePayload;
		if (instance == null || string.IsNullOrWhiteSpace(context.PreparedInstance.IdempotencyKey))
		{
			error = "soldierTroopXpOnce requires an execution context and idempotency key";
			return false;
		}
		if (!string.Equals(instance.ModuleId, ModuleDescriptor.Id, StringComparison.Ordinal) || payload == null)
		{
			error = "soldierTroopXpOnce payload does not match the module";
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
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(value => value, StringComparer.Ordinal);
	}

	private static bool IsCommitted(PolicyEffectExecutionReceipt receipt)
	{
		return receipt != null
			&& (receipt.Status == PolicyEffectExecutionStatus.Applied
				|| receipt.Status == PolicyEffectExecutionStatus.AlreadyApplied);
	}

	private static string FirstNonEmpty(params string[] values)
	{
		return (values ?? Array.Empty<string>()).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
			?? "unknown error";
	}

	private static PolicyEffectExecutionResult Failed(
		string error,
		bool retryable,
		PolicyEffectExecutionReceipt receipt = null,
		JToken runtimeState = null)
	{
		return new PolicyEffectExecutionResult
		{
			Status = PolicyEffectExecutionStatus.Failed,
			Error = error ?? string.Empty,
			Retryable = retryable,
			Receipt = receipt,
			RuntimeState = runtimeState
		};
	}
}
