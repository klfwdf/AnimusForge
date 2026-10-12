using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.ClanLeaderRelationOnceEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class ClanLeaderRelationOncePayload : NumericPolicyEffectPayload
{
}

internal sealed class ClanLeaderRelationOnceEffectModule : NumericPolicyEffectModuleBase<ClanLeaderRelationOncePayload>, IScheduledOncePolicyEffectModule
{
	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "clanLeaderRelationOnce",
		order: 110,
		legacyIds: Array.Empty<string>(),
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Settlement, PolicyEffectTargetKind.Clan, PolicyEffectTargetKind.Kingdom, PolicyEffectTargetKind.Hero },
		targetKinds: new[] { PolicyEffectTargetKind.Clan },
		cueTerms: new[] { PolicyCompatibilityTexts.Get("Module_clanLeaderRelationOnce_Original_Closeout_0"), PolicyCompatibilityTexts.Get("Module_clanLeaderRelationOnce_Original_Closeout_1"), PolicyCompatibilityTexts.Get("Module_clanLeaderRelationOnce_Original_Closeout_2"), PolicyCompatibilityTexts.Get("Module_clanLeaderRelationOnce_Original_Closeout_3"), PolicyCompatibilityTexts.Get("Module_clanLeaderRelationOnce_Original_Closeout_4"), PolicyCompatibilityTexts.Get("Module_clanLeaderRelationOnce_Original_Closeout_5"), "clan leader relations", "lord support", "noble approval", "hostility" },
		retrievalText: PolicyGenerationPromptTexts.Get("Module_clanLeaderRelationOnce_retrievalText", "Personal relations, favor, support, aversion, resentment or gratitude of other clan leaders, lords or vassals toward the policy publisher or issuer. Attitude changes caused by benefits or harm, loss of privileges, recognition of honor, political offenses or oppression; does not change the numerical political influence of a clan. Excludes the issuer's clan and settles relation points once per unique clan leader on the next game day."),
		catalogSummary: PolicyGenerationPromptTexts.Get("Module_clanLeaderRelationOnce_catalogSummary", "One-time issuer relation change with other clan leaders"),
		mainInstruction: PolicyGenerationPromptTexts.Get("Module_clanLeaderRelationOnce_mainInstruction", "If the policy makes lords in the affected area or target kingdom, excluding the publisher’s clan, more supportive of or hostile toward the publisher, specify a one-time relation change. Positive improves relations, negative worsens them; settle on the next game day after adoption. Player-policy publishers are fixed to the player at publication; ruler-policy publishers are fixed to the ruler who adopted the policy."),
		postprocessRule: PolicyGenerationPromptTexts.Get("Module_clanLeaderRelationOnce_postprocessRule", "value must be finite and settles as integer relation points. Deduplicate by clan; settle once per current clan leader. Skip the publisher’s entire clan, eliminated clans and targets without valid leaders. Positive changes retain native diplomacy bonuses and randomized rounding, and relations remain limited to the native -100 to 100 range."),
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Governance,
		executionKind: PolicyEffectExecutionKind.ScheduledOnce,
		hook: PolicyEffectHook.DailyScheduler,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PointsOnce,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Linear,
		payloadSchemaVersion: 1,
		supportsRollback: true,
		supportsIdempotency: true,
		promptVisible: true,
		displayGroup: "clanLeaderRelationOnce",
		playerDisplayName: PolicyCompatibilityTexts.Get("Module_clanLeaderRelationOnce_playerDisplayName"),
		editableUnderstandingPrompt: PolicyGenerationPromptTexts.Get("Module_clanLeaderRelationOnce_editableUnderstandingPrompt", "Clan leader relation reflects other lords' support for or resentment toward the publisher in affected areas or target kingdoms. When policies directly benefit, harm, insult, honor or oppress these lords or involve them in conflicts of interest, leader relations are a one-time consequence. Exclude the publisher's entire clan and clans without a valid leader."),
		editableEvaluationPrompt: PolicyGenerationPromptTexts.Get("Module_clanLeaderRelationOnce_editableEvaluationPrompt", "Positive improves relations and negative worsens them; judge integer relation points. Changes occur on the game day after adoption. Exclude the publisher's entire clan, and settle once per other current clan leader. Mild courtesies, inconvenience or limited benefits usually warrant ±1 to ±3 per leader; clear benefits, losses, honors or political offense ±4 to ±8; major privileges, heavy losses, public humiliation, strong oppression or significant interest conflicts ±9 to ±15; lifesaving gratitude, severe betrayal, systematic persecution or events capable of changing political alignment ±16 to ±30. Do not dilute each leader's change to 1 point because many clans are targeted or the effect settles only once. Do not use high values merely for ornate wording. Intensity follows each leader's actual benefits, losses, honor, oppression and political risks; native relation limits still apply."),
		excludeActorClanTargets: true,
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public override PolicyEffectPrepareResult PrepareTyped(
		PolicyEffectCompileContext context,
		ClanLeaderRelationOncePayload payload)
	{
		PolicyEffectPrepareResult prepared = base.PrepareTyped(context, payload);
		if (prepared?.Success == true
			&& !HasClanTargets(prepared.PreparedInstance?.Instance?.TargetSet))
		{
			return PolicyEffectPrepareResult.Rejected(
				"clanLeaderRelationOnce requires at least one non-empty clan target ID");
		}
		return prepared;
	}

	protected override bool TryNormalizeNumericValue(float rawValue, string scope, out float normalizedValue, out string error)
	{
		normalizedValue = rawValue;
		if (float.IsNaN(rawValue) || float.IsInfinity(rawValue))
		{
			error = "clanLeaderRelationOnce value must be finite";
			return false;
		}
		normalizedValue = (float)Math.Round(rawValue, MidpointRounding.AwayFromZero);
		error = string.Empty;
		return true;
	}

	public PolicyEffectExecutionResult ExecuteScheduledOnce(PolicyEffectExecutionContext context)
	{
		if (!TryValidateContext(context, out PolicyEffectInstance instance, out ClanLeaderRelationOncePayload payload, out string error))
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
			return Failed("clanLeaderRelationOnce requires a game bridge", false);
		}

		int requestedDelta = (int)payload.Value;
		List<RelationTargetReceipt> applied = new List<RelationTargetReceipt>();
		HashSet<string> seenHeroes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		int skipped = 0;
		foreach (string clanId in DistinctIds(instance.TargetSet.ClanIds))
		{
			if (!context.GameBridge.TryChangeClanLeaderRelation(
				instance.ActorHeroId,
				clanId,
				requestedDelta,
				instance.Reason,
				out string targetHeroId,
				out int before,
				out int after,
				out string bridgeError))
			{
				PolicyEffectExecutionReceipt partialReceipt = BuildAppliedReceipt(
					context, instance, requestedDelta, applied, skipped);
				partialReceipt.Message = "partial failure: " + bridgeError;
				return Failed(
					"clanLeaderRelationOnce apply failed for " + clanId + ": " + bridgeError,
					true,
					partialReceipt);
			}
			targetHeroId = (targetHeroId ?? string.Empty).Trim();
			if (targetHeroId.Length <= 0)
			{
				skipped++;
				continue;
			}

			RelationTargetReceipt current = new RelationTargetReceipt(clanId, targetHeroId, requestedDelta, before, after);
			if (!seenHeroes.Add(targetHeroId))
			{
				if (TryRestoreTarget(context, instance, current, out string duplicateRestoreError))
				{
					skipped++;
					continue;
				}
				applied.Add(current);
				PolicyEffectExecutionReceipt duplicateReceipt = BuildAppliedReceipt(
					context, instance, requestedDelta, applied, skipped);
				duplicateReceipt.Message = "duplicate target leader compensation failed";
				return Failed(
					"clanLeaderRelationOnce duplicate leader compensation failed for "
						+ targetHeroId + ": " + duplicateRestoreError,
					false,
					duplicateReceipt);
			}
			applied.Add(current);
		}

		PolicyEffectExecutionReceipt receipt = BuildAppliedReceipt(
			context, instance, requestedDelta, applied, skipped);
		return new PolicyEffectExecutionResult { Status = PolicyEffectExecutionStatus.Applied, Receipt = receipt };
	}

	private static PolicyEffectExecutionReceipt BuildAppliedReceipt(
		PolicyEffectExecutionContext context,
		PolicyEffectInstance instance,
		int requestedDelta,
		IEnumerable<RelationTargetReceipt> applied,
		int skipped)
	{
		List<RelationTargetReceipt> values = applied.ToList();
		JArray targets = new JArray(values.Select(target => new JObject
		{
			["clanId"] = target.ClanId,
			["heroId"] = target.HeroId,
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
			RequestedPayload = new JObject { ["value"] = requestedDelta, ["actorHeroId"] = instance.ActorHeroId },
			AppliedPayload = new JObject { ["targets"] = targets, ["skippedTargets"] = skipped },
			CampaignDay = context.CampaignDay,
			Message = "applied"
		};
	}

	public PolicyEffectExecutionResult CompensateScheduledOnce(PolicyEffectExecutionContext context)
	{
		if (!TryValidateContext(context, out PolicyEffectInstance instance, out ClanLeaderRelationOncePayload payload, out string error))
		{
			return Failed(error, false);
		}
		PolicyEffectExecutionReceipt appliedReceipt = context.ExistingReceipt;
		if (appliedReceipt == null)
		{
			return Failed("clanLeaderRelationOnce compensation requires an execution receipt", false);
		}
		if (context.GameBridge == null)
		{
			return Failed("clanLeaderRelationOnce compensation requires a game bridge", false);
		}
		if (appliedReceipt.Status == PolicyEffectExecutionStatus.RolledBack)
		{
			return new PolicyEffectExecutionResult
			{
				Status = PolicyEffectExecutionStatus.AlreadyApplied,
				Receipt = appliedReceipt
			};
		}

		if (!TryReadTargets(appliedReceipt, out List<RelationTargetReceipt> targets, out string receiptError))
		{
			return Failed("clanLeaderRelationOnce compensation receipt invalid: " + receiptError, false);
		}
		if (!TryRestoreApplied(context, instance, targets, out string compensationError))
		{
			return Failed("clanLeaderRelationOnce compensation incomplete: " + compensationError, false);
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
		IReadOnlyList<RelationTargetReceipt> applied,
		out string error)
	{
		List<string> failures = new List<string>();
		for (int index = applied.Count - 1; index >= 0; index--)
		{
			if (!TryRestoreTarget(context, instance, applied[index], out string targetError))
			{
				failures.Add(applied[index].HeroId + ": " + targetError);
			}
		}
		error = string.Join("; ", failures);
		return failures.Count == 0;
	}

	private static bool TryRestoreTarget(
		PolicyEffectExecutionContext context,
		PolicyEffectInstance instance,
		RelationTargetReceipt target,
		out string error)
	{
		if (!context.GameBridge.TryRestoreHeroRelation(
			instance.ActorHeroId,
			target.HeroId,
			target.After,
			target.Before,
			"compensation:" + instance.Reason,
			out int restored,
			out string bridgeError))
		{
			error = bridgeError;
			return false;
		}
		if (restored != target.Before)
		{
			error = "expected=" + target.Before + " actual=" + restored;
			return false;
		}
		error = string.Empty;
		return true;
	}

	private static bool TryReadTargets(
		PolicyEffectExecutionReceipt receipt,
		out List<RelationTargetReceipt> result,
		out string error)
	{
		result = new List<RelationTargetReceipt>();
		error = string.Empty;
		if (receipt?.AppliedPayload?["targets"] is not JArray targets)
		{
			error = "targets are missing";
			return false;
		}
		HashSet<string> seenClanIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> seenHeroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (JToken targetToken in targets)
		{
			if (targetToken is not JObject target)
			{
				error = "target entry is not an object";
				return false;
			}
			string clanId = ((string)target["clanId"] ?? string.Empty).Trim();
			string heroId = ((string)target["heroId"] ?? string.Empty).Trim();
			if (clanId.Length == 0 || heroId.Length == 0)
			{
				error = "target clanId or heroId is missing";
				return false;
			}
			if (!seenClanIds.Add(clanId))
			{
				error = "duplicate target clanId: " + clanId;
				return false;
			}
			if (!seenHeroIds.Add(heroId))
			{
				error = "duplicate target heroId: " + heroId;
				return false;
			}
			if (!TryReadInt(target["before"], out int before)
				|| !TryReadInt(target["after"], out int after))
			{
				error = "target " + heroId
					+ " lacks exact before/after values; legacy inverse-delta compensation is unsafe";
				return false;
			}
			TryReadInt(target["requestedDelta"], out int requestedDelta);
			result.Add(new RelationTargetReceipt(
				clanId,
				heroId,
				requestedDelta,
				before,
				after));
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
		out ClanLeaderRelationOncePayload payload,
		out string error)
	{
		instance = context?.PreparedInstance?.Instance;
		payload = instance?.Payload as ClanLeaderRelationOncePayload;
		if (instance == null || string.IsNullOrWhiteSpace(context.PreparedInstance.IdempotencyKey))
		{
			error = "clanLeaderRelationOnce requires an execution context and idempotency key";
			return false;
		}
		if (!string.Equals(instance.ModuleId, ModuleDescriptor.Id, StringComparison.Ordinal) || payload == null)
		{
			error = "clanLeaderRelationOnce payload does not match the module";
			return false;
		}
		if (string.IsNullOrWhiteSpace(instance.ActorHeroId))
		{
			error = "clanLeaderRelationOnce requires the frozen policy publisher Hero ID";
			return false;
		}
		if (!HasClanTargets(instance.TargetSet))
		{
			error = "clanLeaderRelationOnce requires at least one non-empty clan target ID";
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

	private sealed class RelationTargetReceipt
	{
		internal RelationTargetReceipt(string clanId, string heroId, int requestedDelta, int before, int after)
		{
			ClanId = clanId;
			HeroId = heroId;
			RequestedDelta = requestedDelta;
			Before = before;
			After = after;
		}

		internal string ClanId { get; }
		internal string HeroId { get; }
		internal int RequestedDelta { get; }
		internal int Before { get; }
		internal int After { get; }
		internal int ActualDelta => After - Before;
	}
}
