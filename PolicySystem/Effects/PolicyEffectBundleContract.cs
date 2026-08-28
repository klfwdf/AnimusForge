using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.PolicyTargets;
using Newtonsoft.Json.Linq;

namespace AnimusForge.PolicyEffects;

internal static class PolicyEffectBundleContract
{
	internal static PolicyEffectCanonicalTargetSet NormalizeTargetSet(PolicyEffectCanonicalTargetSet targetSet)
	{
		return new PolicyEffectCanonicalTargetSet
		{
			StructureVersion = Math.Max(1, targetSet?.StructureVersion ?? 1),
			JurisdictionKind = targetSet?.JurisdictionKind ?? PolicyEffectTargetJurisdictionKind.LegacyCompiled,
			AuthorizedCrossKingdomIds = NormalizeIds(targetSet?.AuthorizedCrossKingdomIds),
			SelectorHandles = NormalizeIds(targetSet?.SelectorHandles),
			SelectorIds = NormalizeIds(targetSet?.SelectorIds),
			TargetPlans = PolicyTargetPlanResolver.NormalizePlans(targetSet?.TargetPlans),
			SettlementIds = NormalizeIds(targetSet?.SettlementIds),
			TownIds = NormalizeIds(targetSet?.TownIds),
			VillageIds = NormalizeIds(targetSet?.VillageIds),
			ClanIds = NormalizeIds(targetSet?.ClanIds),
			KingdomIds = NormalizeIds(targetSet?.KingdomIds),
			HeroIds = NormalizeIds(targetSet?.HeroIds),
			ParentSettlementIds = NormalizeIds(targetSet?.ParentSettlementIds),
			FollowCurrentRulingClan = targetSet?.FollowCurrentRulingClan == true
		};
	}

	internal static PolicyEffectCanonicalTargetSet MergeTargetSets(
		PolicyEffectCanonicalTargetSet left,
		PolicyEffectCanonicalTargetSet right)
	{
		return NormalizeTargetSet(new PolicyEffectCanonicalTargetSet
		{
			StructureVersion = Math.Max(left?.StructureVersion ?? 1, right?.StructureVersion ?? 1),
			JurisdictionKind = PolicyEffectTargetJurisdiction.MergeKind(
				left?.JurisdictionKind ?? PolicyEffectTargetJurisdictionKind.LegacyCompiled,
				right?.JurisdictionKind ?? PolicyEffectTargetJurisdictionKind.LegacyCompiled),
			AuthorizedCrossKingdomIds = (left?.AuthorizedCrossKingdomIds ?? new List<string>())
				.Concat(right?.AuthorizedCrossKingdomIds ?? new List<string>()).ToList(),
			SelectorHandles = (left?.SelectorHandles ?? new List<string>())
				.Concat(right?.SelectorHandles ?? new List<string>()).ToList(),
			SelectorIds = (left?.SelectorIds ?? new List<string>())
				.Concat(right?.SelectorIds ?? new List<string>()).ToList(),
			TargetPlans = PolicyTargetPlanResolver.NormalizePlans(
				(left?.TargetPlans ?? new List<PolicyTargetPlanSaveData>())
					.Concat(right?.TargetPlans ?? new List<PolicyTargetPlanSaveData>())),
			SettlementIds = (left?.SettlementIds ?? new List<string>())
				.Concat(right?.SettlementIds ?? new List<string>()).ToList(),
			TownIds = (left?.TownIds ?? new List<string>())
				.Concat(right?.TownIds ?? new List<string>()).ToList(),
			VillageIds = (left?.VillageIds ?? new List<string>())
				.Concat(right?.VillageIds ?? new List<string>()).ToList(),
			ClanIds = (left?.ClanIds ?? new List<string>())
				.Concat(right?.ClanIds ?? new List<string>()).ToList(),
			KingdomIds = (left?.KingdomIds ?? new List<string>())
				.Concat(right?.KingdomIds ?? new List<string>()).ToList(),
			HeroIds = (left?.HeroIds ?? new List<string>())
				.Concat(right?.HeroIds ?? new List<string>()).ToList(),
			ParentSettlementIds = (left?.ParentSettlementIds ?? new List<string>())
				.Concat(right?.ParentSettlementIds ?? new List<string>()).ToList(),
			FollowCurrentRulingClan = left?.FollowCurrentRulingClan == true
				|| right?.FollowCurrentRulingClan == true
		});
	}

	internal static PolicyEffectExecutionReceipt CloneReceipt(PolicyEffectExecutionReceipt receipt)
	{
		return receipt == null
			? null
			: new PolicyEffectExecutionReceipt
			{
				ReceiptId = receipt.ReceiptId ?? string.Empty,
				InstanceId = receipt.InstanceId ?? string.Empty,
				PolicyId = receipt.PolicyId ?? string.Empty,
				ModuleId = receipt.ModuleId ?? string.Empty,
				TargetSet = NormalizeTargetSet(receipt.TargetSet),
				Status = receipt.Status,
				RequestedValue = receipt.RequestedValue,
				AppliedValue = receipt.AppliedValue,
				RequestedPayload = receipt.RequestedPayload?.DeepClone(),
				AppliedPayload = receipt.AppliedPayload?.DeepClone(),
				CampaignDay = receipt.CampaignDay,
				Message = receipt.Message ?? string.Empty
			};
	}

	internal static PolicyEffectInstanceSaveData CloneInstance(PolicyEffectInstanceSaveData instance)
	{
		return instance == null
			? null
			: new PolicyEffectInstanceSaveData
			{
				MechanismContractVersion = instance.MechanismContractVersion,
				MechanismContractHash = instance.MechanismContractHash ?? string.Empty,
				ExpectedMechanismLegIds = new List<string>(instance.ExpectedMechanismLegIds ?? new List<string>()),
				EffectPlanVersion = instance.EffectPlanVersion,
				MechanismId = instance.MechanismId ?? string.Empty,
				MechanismKind = instance.MechanismKind,
				MechanismRole = instance.MechanismRole,
				SourceOmitted = instance.SourceOmitted,
				DestinationOmitted = instance.DestinationOmitted,
				InstanceId = instance.InstanceId ?? string.Empty,
				PolicyId = instance.PolicyId ?? string.Empty,
				ActorHeroId = instance.ActorHeroId ?? string.Empty,
				ModuleId = instance.ModuleId ?? string.Empty,
				SourceModuleId = instance.SourceModuleId ?? string.Empty,
				PayloadSchemaVersion = instance.PayloadSchemaVersion,
				Payload = instance.Payload?.DeepClone(),
				TargetSet = NormalizeTargetSet(instance.TargetSet),
				LifecycleState = instance.LifecycleState,
				StateSchemaVersion = instance.StateSchemaVersion,
				RuntimeState = instance.RuntimeState?.DeepClone(),
				ExecutionReceipt = CloneReceipt(instance.ExecutionReceipt),
				StartDay = instance.StartDay,
				EndDay = instance.EndDay,
				SourceScope = instance.SourceScope ?? string.Empty,
				Reason = instance.Reason ?? string.Empty
			};
	}

	internal static bool TryCoalesceShellInstances(
		IEnumerable<PolicyEffectInstanceSaveData> shellInstances,
		out List<PolicyEffectInstanceSaveData> instances,
		out string error)
	{
		// Publication, save loading, and agenda adoption are cold paths. Display shells
		// remain separate; this view guarantees one executable instance per stable id.
		instances = new List<PolicyEffectInstanceSaveData>();
		error = string.Empty;
		Dictionary<string, PolicyEffectInstanceSaveData> byInstanceId
			= new Dictionary<string, PolicyEffectInstanceSaveData>(StringComparer.Ordinal);
		foreach (PolicyEffectInstanceSaveData shell in shellInstances ?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
		{
			if (shell == null)
			{
				continue;
			}
			string instanceId = (shell.InstanceId ?? string.Empty).Trim();
			if (instanceId.Length == 0 || !IsFinite(shell.StartDay) || !IsFinite(shell.EndDay))
			{
				error = "policy effect 目标壳缺少有效 instanceId 或期限";
				return false;
			}
			if (!byInstanceId.TryGetValue(instanceId, out PolicyEffectInstanceSaveData aggregate))
			{
				aggregate = CloneInstance(shell);
				aggregate.InstanceId = instanceId;
				byInstanceId.Add(instanceId, aggregate);
				instances.Add(aggregate);
				continue;
			}
			if (!AreCompatibleShellInstances(aggregate, shell))
			{
				error = "同一 policy effect instanceId 的目标壳数据不一致: " + instanceId;
				return false;
			}
			aggregate.TargetSet = MergeTargetSets(aggregate.TargetSet, shell.TargetSet);
			aggregate.ActorHeroId = FirstNonEmpty(aggregate.ActorHeroId, shell.ActorHeroId);
			if (aggregate.ExecutionReceipt != null)
			{
				aggregate.ExecutionReceipt.TargetSet = MergeTargetSets(
					aggregate.ExecutionReceipt.TargetSet,
					shell.ExecutionReceipt?.TargetSet);
			}
		}
		return true;
	}

	internal static bool HasTargetsForModule(IPolicyEffectModule module, PolicyEffectCanonicalTargetSet targetSet)
	{
		if (module?.Descriptor?.TargetKinds == null || targetSet == null)
		{
			return false;
		}
		if (module.Descriptor.ExcludeActorClanTargets
			&& module.Descriptor.TargetKinds.Contains(PolicyEffectTargetKind.Clan)
			&& (targetSet.ClanIds?.Count ?? 0) == 0)
		{
			return false;
		}
		foreach (PolicyEffectTargetKind kind in module.Descriptor.TargetKinds)
		{
			switch (kind)
			{
				case PolicyEffectTargetKind.Settlement: if ((targetSet.SettlementIds?.Count ?? 0) > 0) return true; break;
				case PolicyEffectTargetKind.Town: if ((targetSet.TownIds?.Count ?? 0) > 0) return true; break;
				case PolicyEffectTargetKind.Village: if ((targetSet.VillageIds?.Count ?? 0) > 0) return true; break;
				case PolicyEffectTargetKind.Clan: if ((targetSet.ClanIds?.Count ?? 0) > 0) return true; break;
				case PolicyEffectTargetKind.Kingdom: if ((targetSet.KingdomIds?.Count ?? 0) > 0) return true; break;
				case PolicyEffectTargetKind.Hero: if ((targetSet.HeroIds?.Count ?? 0) > 0) return true; break;
			}
		}
		return module.Descriptor.ExecutionKind != PolicyEffectExecutionKind.OneShot
			&& PolicyTargetPlanResolver.NormalizePlans(targetSet.TargetPlans).Count > 0;
	}

	internal static bool AreSameTargetSets(
		PolicyEffectCanonicalTargetSet left,
		PolicyEffectCanonicalTargetSet right)
	{
		PolicyEffectCanonicalTargetSet normalizedLeft = NormalizeTargetSet(left);
		PolicyEffectCanonicalTargetSet normalizedRight = NormalizeTargetSet(right);
		return normalizedLeft.JurisdictionKind == normalizedRight.JurisdictionKind
			&& normalizedLeft.AuthorizedCrossKingdomIds.SequenceEqual(normalizedRight.AuthorizedCrossKingdomIds, StringComparer.Ordinal)
			&& normalizedLeft.SelectorHandles.SequenceEqual(normalizedRight.SelectorHandles, StringComparer.Ordinal)
			&& normalizedLeft.SelectorIds.SequenceEqual(normalizedRight.SelectorIds, StringComparer.Ordinal)
			&& normalizedLeft.TargetPlans.Select(plan => plan.NormalizedSignature).SequenceEqual(
				normalizedRight.TargetPlans.Select(plan => plan.NormalizedSignature),
				StringComparer.Ordinal)
			&& normalizedLeft.SettlementIds.SequenceEqual(normalizedRight.SettlementIds, StringComparer.Ordinal)
			&& normalizedLeft.TownIds.SequenceEqual(normalizedRight.TownIds, StringComparer.Ordinal)
			&& normalizedLeft.VillageIds.SequenceEqual(normalizedRight.VillageIds, StringComparer.Ordinal)
			&& normalizedLeft.ClanIds.SequenceEqual(normalizedRight.ClanIds, StringComparer.Ordinal)
			&& normalizedLeft.KingdomIds.SequenceEqual(normalizedRight.KingdomIds, StringComparer.Ordinal)
			&& normalizedLeft.HeroIds.SequenceEqual(normalizedRight.HeroIds, StringComparer.Ordinal)
			&& normalizedLeft.ParentSettlementIds.SequenceEqual(normalizedRight.ParentSettlementIds, StringComparer.Ordinal)
			&& normalizedLeft.FollowCurrentRulingClan == normalizedRight.FollowCurrentRulingClan;
	}

	internal static bool AreCompatibleShellInstances(
		PolicyEffectInstanceSaveData left,
		PolicyEffectInstanceSaveData right)
	{
		return left != null
			&& right != null
			&& left.MechanismContractVersion == right.MechanismContractVersion
			&& string.Equals(left.MechanismContractHash ?? string.Empty, right.MechanismContractHash ?? string.Empty,
				StringComparison.Ordinal)
			&& (left.ExpectedMechanismLegIds ?? new List<string>()).SequenceEqual(
				right.ExpectedMechanismLegIds ?? new List<string>(), StringComparer.Ordinal)
			&& left.EffectPlanVersion == right.EffectPlanVersion
			&& string.Equals(left.MechanismId ?? string.Empty, right.MechanismId ?? string.Empty, StringComparison.Ordinal)
			&& left.MechanismKind == right.MechanismKind
			&& left.MechanismRole == right.MechanismRole
			&& left.SourceOmitted == right.SourceOmitted
			&& left.DestinationOmitted == right.DestinationOmitted
			&& string.Equals(left.InstanceId ?? string.Empty, right.InstanceId ?? string.Empty, StringComparison.Ordinal)
			&& string.Equals(left.PolicyId ?? string.Empty, right.PolicyId ?? string.Empty, StringComparison.Ordinal)
			&& string.Equals(left.ModuleId ?? string.Empty, right.ModuleId ?? string.Empty, StringComparison.Ordinal)
			&& string.Equals(left.SourceModuleId ?? string.Empty, right.SourceModuleId ?? string.Empty, StringComparison.Ordinal)
			&& AreCompatibleActorIds(left.ActorHeroId, right.ActorHeroId)
			&& left.PayloadSchemaVersion == right.PayloadSchemaVersion
			&& TokensEqual(left.Payload, right.Payload)
			&& left.LifecycleState == right.LifecycleState
			&& left.StateSchemaVersion == right.StateSchemaVersion
			&& TokensEqual(left.RuntimeState, right.RuntimeState)
			&& AreCompatibleReceipts(left.ExecutionReceipt, right.ExecutionReceipt)
			&& left.StartDay.Equals(right.StartDay)
			&& left.EndDay.Equals(right.EndDay)
			&& string.Equals(left.SourceScope ?? string.Empty, right.SourceScope ?? string.Empty, StringComparison.Ordinal)
			&& string.Equals(left.Reason ?? string.Empty, right.Reason ?? string.Empty, StringComparison.Ordinal);
	}

	private static bool AreCompatibleActorIds(string left, string right)
	{
		string normalizedLeft = (left ?? string.Empty).Trim();
		string normalizedRight = (right ?? string.Empty).Trim();
		return normalizedLeft.Length == 0
			|| normalizedRight.Length == 0
			|| string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
	}

	internal static bool AreCompatibleReceipts(
		PolicyEffectExecutionReceipt left,
		PolicyEffectExecutionReceipt right)
	{
		if (left == null || right == null)
		{
			return left == null && right == null;
		}
		return string.Equals(left.ReceiptId ?? string.Empty, right.ReceiptId ?? string.Empty, StringComparison.Ordinal)
			&& string.Equals(left.InstanceId ?? string.Empty, right.InstanceId ?? string.Empty, StringComparison.Ordinal)
			&& string.Equals(left.PolicyId ?? string.Empty, right.PolicyId ?? string.Empty, StringComparison.Ordinal)
			&& string.Equals(left.ModuleId ?? string.Empty, right.ModuleId ?? string.Empty, StringComparison.Ordinal)
			&& left.Status == right.Status
			&& left.RequestedValue.Equals(right.RequestedValue)
			&& left.AppliedValue.Equals(right.AppliedValue)
			&& TokensEqual(left.RequestedPayload, right.RequestedPayload)
			&& TokensEqual(left.AppliedPayload, right.AppliedPayload)
			&& left.CampaignDay.Equals(right.CampaignDay)
			&& string.Equals(left.Message ?? string.Empty, right.Message ?? string.Empty, StringComparison.Ordinal);
	}

	internal static bool TokensEqual(JToken left, JToken right)
	{
		return ReferenceEquals(left, right)
			|| (left != null && right != null && JToken.DeepEquals(left, right));
	}

	private static bool IsFinite(float value)
	{
		return !float.IsNaN(value) && !float.IsInfinity(value);
	}

	private static List<string> NormalizeIds(IEnumerable<string> values)
	{
		return (values ?? Enumerable.Empty<string>())
			.Select(value => (value ?? string.Empty).Trim())
			.Where(value => value.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(value => value, StringComparer.Ordinal)
			.ToList();
	}

	private static string FirstNonEmpty(params string[] values)
	{
		return (values ?? Array.Empty<string>())
			.Select(value => (value ?? string.Empty).Trim())
			.FirstOrDefault(value => value.Length > 0)
			?? string.Empty;
	}
}
