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
		cueTerms: new[] { "关系", "好感", "领主态度", "贵族支持", "得罪领主", "赢得拥护" },
		retrievalText: "政策发布者与目标家族领袖的关系、好感、态度、支持或敌意；在政策通过后的下一个游戏日对每位唯一家族领袖结算一次。",
		catalogSummary: "发布者与目标家族领袖的一次性关系变化",
		mainInstruction: "政策若会让受影响地区或目标王国的领主更支持或更反感政策发布者，请给出一次性关系变化。正数改善关系，负数恶化关系；效果在政策通过后的下一个游戏日结算一次。玩家政策的发布者固定为发布时玩家，统治者政策固定为通过政策的统治者。",
		postprocessRule: "value 必须是有限数字，最终按整数关系点结算。目标按家族去重，每个当前家族领袖只结算一次；发布者本人、已灭亡家族及无有效领袖目标跳过。正向变化保留原版外交模型加成和随机取整，关系仍受原版 -100～100 限制。",
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
		playerDisplayName: "家族领袖关系",
		editableUnderstandingPrompt: "家族领袖关系反映受影响地区或目标王国的领主对政策发布者的支持或反感。政策让这些领主直接受益、受损、受辱、获誉、受压迫或卷入利益冲突时，领袖关系就是一次性后果；发布者本人和没有有效领袖的家族不计入。",
		editableEvaluationPrompt: "关系改善为正、恶化为负，并按整数关系点判断。变化在政策通过后的下一游戏日发生，每个当前家族领袖只结算一次，强度应与受益、受损、荣誉、压迫和利益冲突程度相称，关系仍受游戏原有上下限约束。");

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
			error = "clanLeaderRelationOnce 的 value 必须是有限数字";
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
			return Failed("clanLeaderRelationOnce 缺少 game bridge", false);
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
			return Failed("clanLeaderRelationOnce compensation 缺少执行回执", false);
		}
		if (context.GameBridge == null)
		{
			return Failed("clanLeaderRelationOnce compensation 缺少 game bridge", false);
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
			error = "clanLeaderRelationOnce 缺少执行上下文或幂等键";
			return false;
		}
		if (!string.Equals(instance.ModuleId, ModuleDescriptor.Id, StringComparison.Ordinal) || payload == null)
		{
			error = "clanLeaderRelationOnce payload 与模块不匹配";
			return false;
		}
		if (string.IsNullOrWhiteSpace(instance.ActorHeroId))
		{
			error = "clanLeaderRelationOnce 缺少冻结的政策发布者 Hero ID";
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
