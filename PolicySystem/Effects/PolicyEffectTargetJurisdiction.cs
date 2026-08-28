using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.PolicyTargets;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge.PolicyEffects;

internal static class PolicyEffectTargetJurisdiction
{
	internal static bool TryApply(
		PolicyEffectCanonicalTargetSet source,
		IPolicyEffectModule module,
		string targetKingdomId,
		string issuerKingdomId,
		IReadOnlyCollection<string> authorizedCrossKingdomIds,
		bool preserveLegacyCrossKingdoms,
		bool failOnUnauthorized,
		out PolicyEffectCanonicalTargetSet targetSet,
		out string error)
	{
		targetSet = Normalize(source);
		error = string.Empty;
		if (source == null || module?.Descriptor == null)
		{
			return true;
		}

		string homeKingdomId = ResolveHomeKingdomId(module, targetKingdomId, issuerKingdomId);
		if (homeKingdomId.Length == 0)
		{
			if (failOnUnauthorized && HasAnyTarget(targetSet))
			{
				targetSet = null;
				error = "政策效果目标缺少发布地王国边界。";
				return false;
			}
			return true;
		}

		HashSet<string> allowedCrossKingdomIds = new HashSet<string>(
			NormalizeIds(authorizedCrossKingdomIds)
				.Where(id => !SameId(id, homeKingdomId)),
			StringComparer.OrdinalIgnoreCase);
		if (preserveLegacyCrossKingdoms
			&& source.JurisdictionKind == PolicyEffectTargetJurisdictionKind.LegacyCompiled
			&& module.Descriptor.AllowCrossKingdomTargets)
		{
			foreach (string kingdomId in CollectReferencedForeignKingdomIds(source, homeKingdomId))
			{
				allowedCrossKingdomIds.Add(kingdomId);
			}
		}

		bool moduleAllowsCross = module.Descriptor.AllowCrossKingdomTargets;
		List<string> rejected = new List<string>();
		targetSet.KingdomIds = FilterIds(
			targetSet.KingdomIds,
			PolicyEffectTargetKind.Kingdom,
			id => id,
			homeKingdomId,
			allowedCrossKingdomIds,
			moduleAllowsCross,
			allowKingdomlessTarget: false,
			rejected);
		targetSet.SettlementIds = FilterIds(
			targetSet.SettlementIds,
			PolicyEffectTargetKind.Settlement,
			ResolveSettlementOwnerKingdomId,
			homeKingdomId,
			allowedCrossKingdomIds,
			moduleAllowsCross,
			allowKingdomlessTarget: false,
			rejected);
		targetSet.TownIds = FilterIds(
			targetSet.TownIds,
			PolicyEffectTargetKind.Town,
			ResolveSettlementOwnerKingdomId,
			homeKingdomId,
			allowedCrossKingdomIds,
			moduleAllowsCross,
			allowKingdomlessTarget: false,
			rejected);
		targetSet.VillageIds = FilterIds(
			targetSet.VillageIds,
			PolicyEffectTargetKind.Village,
			ResolveSettlementOwnerKingdomId,
			homeKingdomId,
			allowedCrossKingdomIds,
			moduleAllowsCross,
			allowKingdomlessTarget: false,
			rejected);
		targetSet.ParentSettlementIds = FilterIds(
			targetSet.ParentSettlementIds,
			PolicyEffectTargetKind.Settlement,
			ResolveSettlementOwnerKingdomId,
			homeKingdomId,
			allowedCrossKingdomIds,
			moduleAllowsCross,
			allowKingdomlessTarget: false,
			rejected);
		targetSet.ClanIds = FilterIds(
			targetSet.ClanIds,
			PolicyEffectTargetKind.Clan,
			ResolveClanOwnerKingdomId,
			homeKingdomId,
			allowedCrossKingdomIds,
			moduleAllowsCross,
			module.Descriptor.AllowIndependentClanTargets,
			rejected);
		targetSet.HeroIds = FilterIds(
			targetSet.HeroIds,
			PolicyEffectTargetKind.Hero,
			ResolveHeroOwnerKingdomId,
			homeKingdomId,
			allowedCrossKingdomIds,
			moduleAllowsCross,
			allowKingdomlessTarget: false,
			rejected);

		List<string> usedCrossKingdomIds = NormalizeIds(CollectReferencedForeignKingdomIds(targetSet, homeKingdomId));
		targetSet.JurisdictionKind = usedCrossKingdomIds.Count > 0
			? PolicyEffectTargetJurisdictionKind.CrossKingdom
			: PolicyEffectTargetJurisdictionKind.Domestic;
		targetSet.AuthorizedCrossKingdomIds = moduleAllowsCross
			? usedCrossKingdomIds.Where(allowedCrossKingdomIds.Contains).ToList()
			: new List<string>();
		if (failOnUnauthorized && rejected.Count > 0)
		{
			string firstRejected = string.Join(", ", rejected.Take(6));
			targetSet = null;
			error = "政策效果目标越过发布地管辖边界"
				+ (moduleAllowsCross ? "或缺少明确跨国授权" : "，且该模块未开放跨国目标")
				+ "：" + firstRejected;
			return false;
		}
		return true;
	}

	internal static bool IsExplicitKingdomTargetSetAuthorized(
		IPolicyEffectModule module,
		PolicyEffectCanonicalTargetSet targetSet,
		string targetKingdomId,
		IReadOnlyCollection<string> authorizedCrossKingdomIds,
		out string error)
	{
		return TryAuthorizeExplicitKingdomTargets(
			module,
			targetSet,
			targetKingdomId,
			issuerKingdomId: null,
			authorizedCrossKingdomIds,
			out _,
			out error);
	}

	internal static bool TryAuthorizeExplicitKingdomTargets(
		IPolicyEffectModule module,
		PolicyEffectCanonicalTargetSet source,
		string targetKingdomId,
		string issuerKingdomId,
		IReadOnlyCollection<string> authorizedCrossKingdomIds,
		out PolicyEffectCanonicalTargetSet targetSet,
		out string error)
	{
		error = string.Empty;
		targetSet = Normalize(source);
		if (module?.Descriptor == null || source == null)
		{
			return true;
		}
		string homeKingdomId = ResolveHomeKingdomId(module, targetKingdomId, issuerKingdomId);
		if (homeKingdomId.Length == 0)
		{
			return true;
		}
		HashSet<string> allowedCross = new HashSet<string>(
			NormalizeIds(authorizedCrossKingdomIds)
				.Where(id => !SameId(id, homeKingdomId)),
			StringComparer.OrdinalIgnoreCase);
		List<string> explicitForeignKingdomIds = NormalizeIds(targetSet.KingdomIds)
			.Where(id => !SameId(id, homeKingdomId))
			.ToList();
		foreach (string kingdomId in explicitForeignKingdomIds)
		{
			if (module.Descriptor.AllowCrossKingdomTargets && allowedCross.Contains(kingdomId))
			{
				continue;
			}
			error = "目标王国 " + kingdomId + " 未被当前政策效果模块授权为可执行跨国目标。";
			return false;
		}
		if (explicitForeignKingdomIds.Count > 0)
		{
			targetSet.JurisdictionKind = PolicyEffectTargetJurisdictionKind.CrossKingdom;
			targetSet.AuthorizedCrossKingdomIds = explicitForeignKingdomIds
				.Where(allowedCross.Contains)
				.ToList();
			return true;
		}
		if (targetSet.JurisdictionKind == PolicyEffectTargetJurisdictionKind.CrossKingdom)
		{
			if (!module.Descriptor.AllowCrossKingdomTargets)
			{
				error = "模块 " + module.Id + " 未开放跨国目标。";
				return false;
			}
			List<string> persistedAuthorized = NormalizeIds(targetSet.AuthorizedCrossKingdomIds);
			if (persistedAuthorized.Count == 0)
			{
				error = "跨国目标缺少明确授权王国集合。";
				return false;
			}
			string unauthorized = persistedAuthorized.FirstOrDefault(id => !allowedCross.Contains(id));
			if (!string.IsNullOrWhiteSpace(unauthorized))
			{
				error = "目标王国 " + unauthorized + " 未被当前政策请求授权为可执行跨国目标。";
				return false;
			}
			targetSet.AuthorizedCrossKingdomIds = persistedAuthorized;
			return true;
		}
		if (NormalizeIds(targetSet.KingdomIds).Count > 0)
		{
			targetSet.JurisdictionKind = PolicyEffectTargetJurisdictionKind.Domestic;
		}
		targetSet.AuthorizedCrossKingdomIds = new List<string>();
		return true;
	}

	internal static PolicyEffectTargetJurisdictionKind MergeKind(
		PolicyEffectTargetJurisdictionKind left,
		PolicyEffectTargetJurisdictionKind right)
	{
		if (left == PolicyEffectTargetJurisdictionKind.CrossKingdom
			|| right == PolicyEffectTargetJurisdictionKind.CrossKingdom)
		{
			return PolicyEffectTargetJurisdictionKind.CrossKingdom;
		}
		if (left == PolicyEffectTargetJurisdictionKind.Domestic
			|| right == PolicyEffectTargetJurisdictionKind.Domestic)
		{
			return PolicyEffectTargetJurisdictionKind.Domestic;
		}
		return PolicyEffectTargetJurisdictionKind.LegacyCompiled;
	}

	internal static List<string> NormalizeIds(IEnumerable<string> values)
	{
		return (values ?? Array.Empty<string>())
			.Select(NormalizeId)
			.Where(value => value.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(value => value, StringComparer.Ordinal)
			.ToList();
	}

	private static string ResolveHomeKingdomId(
		IPolicyEffectModule module,
		string targetKingdomId,
		string issuerKingdomId)
	{
		if (module?.Descriptor?.TargetBinding == PolicyEffectTargetBindingKind.IssuerKingdom)
		{
			string issuer = NormalizeId(issuerKingdomId);
			if (issuer.Length > 0)
			{
				return issuer;
			}
		}
		return NormalizeId(targetKingdomId);
	}

	private static List<string> FilterIds(
		IEnumerable<string> ids,
		PolicyEffectTargetKind kind,
		Func<string, string> ownerKingdomResolver,
		string homeKingdomId,
		HashSet<string> allowedCrossKingdomIds,
		bool moduleAllowsCross,
		bool allowKingdomlessTarget,
		List<string> rejected)
	{
		List<string> result = new List<string>();
		foreach (string id in NormalizeIds(ids))
		{
			string ownerKingdomId = NormalizeId(ownerKingdomResolver(id));
			if (ownerKingdomId.Length == 0 && allowKingdomlessTarget)
			{
				result.Add(id);
				continue;
			}
			if (SameId(ownerKingdomId, homeKingdomId)
				|| (moduleAllowsCross && allowedCrossKingdomIds.Contains(ownerKingdomId)))
			{
				result.Add(id);
				continue;
			}
			rejected?.Add(kind + ":" + id);
		}
		return result;
	}

	private static IEnumerable<string> CollectReferencedForeignKingdomIds(
		PolicyEffectCanonicalTargetSet targetSet,
		string homeKingdomId)
	{
		foreach (string kingdomId in NormalizeIds(targetSet?.KingdomIds))
		{
			if (!SameId(kingdomId, homeKingdomId))
			{
				yield return kingdomId;
			}
		}
		foreach (string kingdomId in NormalizeIds(targetSet?.SettlementIds).Select(ResolveSettlementOwnerKingdomId)
			.Concat(NormalizeIds(targetSet?.TownIds).Select(ResolveSettlementOwnerKingdomId))
			.Concat(NormalizeIds(targetSet?.VillageIds).Select(ResolveSettlementOwnerKingdomId))
			.Concat(NormalizeIds(targetSet?.ParentSettlementIds).Select(ResolveSettlementOwnerKingdomId))
			.Concat(NormalizeIds(targetSet?.ClanIds).Select(ResolveClanOwnerKingdomId))
			.Concat(NormalizeIds(targetSet?.HeroIds).Select(ResolveHeroOwnerKingdomId)))
		{
			string normalized = NormalizeId(kingdomId);
			if (normalized.Length > 0 && !SameId(normalized, homeKingdomId))
			{
				yield return normalized;
			}
		}
	}

	private static bool HasAnyTarget(PolicyEffectCanonicalTargetSet targetSet)
	{
		return (targetSet?.SettlementIds?.Count ?? 0) > 0
			|| (targetSet?.TownIds?.Count ?? 0) > 0
			|| (targetSet?.VillageIds?.Count ?? 0) > 0
			|| (targetSet?.ClanIds?.Count ?? 0) > 0
			|| (targetSet?.KingdomIds?.Count ?? 0) > 0
			|| (targetSet?.HeroIds?.Count ?? 0) > 0;
	}

	private static PolicyEffectCanonicalTargetSet Normalize(PolicyEffectCanonicalTargetSet targetSet)
	{
		return PolicyEffectBundleContract.NormalizeTargetSet(targetSet);
	}

	private static string ResolveSettlementOwnerKingdomId(string settlementId)
	{
		Settlement settlement = ResolveSettlement(settlementId);
		Clan ownerClan = settlement?.OwnerClan ?? settlement?.Village?.Bound?.OwnerClan;
		return NormalizeId(ownerClan?.Kingdom?.StringId);
	}

	private static Settlement ResolveSettlement(string settlementId)
	{
		string normalized = NormalizeId(settlementId);
		if (normalized.Length == 0)
		{
			return null;
		}
		try
		{
			return (Settlement.All ?? Enumerable.Empty<Settlement>()).FirstOrDefault(settlement =>
				settlement != null
				&& SameId(settlement.StringId, normalized));
		}
		catch
		{
			return null;
		}
	}

	private static string ResolveClanOwnerKingdomId(string clanId)
	{
		Clan clan = ResolveClan(clanId);
		return NormalizeId(clan?.Kingdom?.StringId);
	}

	private static Clan ResolveClan(string clanId)
	{
		string normalized = NormalizeId(clanId);
		if (normalized.Length == 0)
		{
			return null;
		}
		try
		{
			return (Clan.All ?? Enumerable.Empty<Clan>()).FirstOrDefault(clan =>
				clan != null
				&& SameId(clan.StringId, normalized));
		}
		catch
		{
			return null;
		}
	}

	private static string ResolveHeroOwnerKingdomId(string heroId)
	{
		try
		{
			Hero hero = Hero.Find(NormalizeId(heroId));
			return NormalizeId(hero?.Clan?.Kingdom?.StringId);
		}
		catch
		{
			return string.Empty;
		}
	}

	private static string NormalizeId(string value)
	{
		return (value ?? string.Empty).Trim();
	}

	private static bool SameId(string left, string right)
	{
		return string.Equals(
			NormalizeId(left),
			NormalizeId(right),
			StringComparison.OrdinalIgnoreCase);
	}
}
