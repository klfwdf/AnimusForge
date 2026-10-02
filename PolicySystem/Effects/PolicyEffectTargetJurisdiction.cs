using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.PolicyTargets;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge.PolicyEffects;

internal delegate string PolicyEffectOwnerKingdomResolver(PolicyEffectTargetKind targetKind, string targetId);

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
		out string error,
		PolicyEffectJurisdictionContext context = null)
	{
		return TryApply(
			source,
			module,
			targetKingdomId,
			issuerKingdomId,
			authorizedCrossKingdomIds,
			preserveLegacyCrossKingdoms,
			failOnUnauthorized,
			ownerKingdomResolver: null,
			out targetSet,
			out error,
			context);
	}

	internal static bool TryApply(
		PolicyEffectCanonicalTargetSet source,
		IPolicyEffectModule module,
		string targetKingdomId,
		string issuerKingdomId,
		IReadOnlyCollection<string> authorizedCrossKingdomIds,
		bool preserveLegacyCrossKingdoms,
		bool failOnUnauthorized,
		PolicyEffectOwnerKingdomResolver ownerKingdomResolver,
		out PolicyEffectCanonicalTargetSet targetSet,
		out string error,
		PolicyEffectJurisdictionContext context = null)
	{
		targetSet = Normalize(source);
		error = string.Empty;
		if (source == null || module?.Descriptor == null)
		{
			return true;
		}
		if (string.Equals(context?.Scope, PolicyEffectScopes.Local, StringComparison.OrdinalIgnoreCase))
		{
			return TryApplyLocal(targetSet, module, context, targetKingdomId, issuerKingdomId,
				authorizedCrossKingdomIds, preserveLegacyCrossKingdoms, failOnUnauthorized,
				ownerKingdomResolver, out targetSet, out error);
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
			foreach (string kingdomId in CollectReferencedForeignKingdomIds(source, homeKingdomId, ownerKingdomResolver))
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
			id => ResolveOwnerKingdomId(PolicyEffectTargetKind.Settlement, id, ownerKingdomResolver),
			homeKingdomId,
			allowedCrossKingdomIds,
			moduleAllowsCross,
			allowKingdomlessTarget: false,
			rejected);
		targetSet.TownIds = FilterIds(
			targetSet.TownIds,
			PolicyEffectTargetKind.Town,
			id => ResolveOwnerKingdomId(PolicyEffectTargetKind.Town, id, ownerKingdomResolver),
			homeKingdomId,
			allowedCrossKingdomIds,
			moduleAllowsCross,
			allowKingdomlessTarget: false,
			rejected);
		targetSet.VillageIds = FilterIds(
			targetSet.VillageIds,
			PolicyEffectTargetKind.Village,
			id => ResolveOwnerKingdomId(PolicyEffectTargetKind.Village, id, ownerKingdomResolver),
			homeKingdomId,
			allowedCrossKingdomIds,
			moduleAllowsCross,
			allowKingdomlessTarget: false,
			rejected);
		targetSet.ParentSettlementIds = FilterIds(
			targetSet.ParentSettlementIds,
			PolicyEffectTargetKind.Settlement,
			id => ResolveOwnerKingdomId(PolicyEffectTargetKind.Settlement, id, ownerKingdomResolver),
			homeKingdomId,
			allowedCrossKingdomIds,
			moduleAllowsCross,
			allowKingdomlessTarget: false,
			rejected);
		targetSet.ClanIds = FilterIds(
			targetSet.ClanIds,
			PolicyEffectTargetKind.Clan,
			id => ResolveOwnerKingdomId(PolicyEffectTargetKind.Clan, id, ownerKingdomResolver),
			homeKingdomId,
			allowedCrossKingdomIds,
			moduleAllowsCross,
			module.Descriptor.AllowIndependentClanTargets,
			rejected);
		targetSet.HeroIds = FilterIds(
			targetSet.HeroIds,
			PolicyEffectTargetKind.Hero,
			id => ResolveOwnerKingdomId(PolicyEffectTargetKind.Hero, id, ownerKingdomResolver),
			homeKingdomId,
			allowedCrossKingdomIds,
			moduleAllowsCross,
			allowKingdomlessTarget: false,
			rejected);

		List<string> usedCrossKingdomIds = NormalizeIds(CollectReferencedForeignKingdomIds(targetSet, homeKingdomId, ownerKingdomResolver));
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

	private static bool TryApplyLocal(
		PolicyEffectCanonicalTargetSet targets, IPolicyEffectModule module,
		PolicyEffectJurisdictionContext context, string targetKingdomId, string issuerKingdomId,
		IReadOnlyCollection<string> authorizedCrossKingdomIds, bool preserveLegacyCrossKingdoms,
		bool failOnUnauthorized, PolicyEffectOwnerKingdomResolver ownerKingdomResolver,
		out PolicyEffectCanonicalTargetSet targetSet, out string error)
	{
		targetSet = targets;
		error = string.Empty;
		string home = ResolveHomeKingdomId(module, targetKingdomId, issuerKingdomId);
		var cross = new HashSet<string>(NormalizeIds(authorizedCrossKingdomIds), StringComparer.OrdinalIgnoreCase);
		if (preserveLegacyCrossKingdoms && targets.JurisdictionKind == PolicyEffectTargetJurisdictionKind.LegacyCompiled
			&& module.Descriptor.AllowCrossKingdomTargets && home.Length > 0)
		{
			cross.UnionWith(CollectReferencedForeignKingdomIds(targets, home, ownerKingdomResolver));
		}
		bool AllowsKingdom(string id) => !string.IsNullOrWhiteSpace(id)
			&& ((home.Length > 0 && SameId(home, id)) || (module.Descriptor.AllowCrossKingdomTargets && cross.Contains(id)));

		// S means exactly the selected publication fiefs. Other handles/plans have
		// already passed request authorization; persisted expressions retain that identity.
		bool explicitTargets = targets.TargetPlans.Count > 0 || targets.SelectorHandles.Count == 0
			|| targets.SelectorHandles.Any(handle => !SameId(handle, "S"));
		var parents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var settlements = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var villages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var clans = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var leaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var usedCross = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var selected = new HashSet<string>(context.SourceSettlementIds, StringComparer.OrdinalIgnoreCase);
		bool localModule = PolicyEffectModuleCatalog.IsAllowedForScope(module, PolicyEffectScopes.Local);
		foreach (string parentId in NormalizeIds(targets.ParentSettlementIds.Concat(context.SourceSettlementIds)))
		{
			PolicyEffectJurisdictionFief fief = context.ResolveFief(parentId);
			if (!localModule || fief == null || !SameId(fief.Id, parentId) || string.IsNullOrWhiteSpace(fief.OwnerClanId)) continue;
			bool owned = context.ProposerClanId.Length > 0 && SameId(fief.OwnerClanId, context.ProposerClanId);
			bool publication = selected.Contains(parentId) && owned;
			bool mentioned = explicitTargets && targets.ParentSettlementIds.Contains(parentId, StringComparer.OrdinalIgnoreCase)
				&& (owned || AllowsKingdom(fief.OwnerKingdomId));
			if (!publication && !mentioned) continue;
			parents.Add(parentId);
			settlements.Add(parentId);
			villages.UnionWith(NormalizeIds(fief.VillageIds));
			clans.Add(fief.OwnerClanId);
			if (!string.IsNullOrWhiteSpace(fief.OwnerLeaderId)) leaders.Add(fief.OwnerLeaderId);
			if (!publication && !owned && !SameId(fief.OwnerKingdomId, home)
				&& !string.IsNullOrWhiteSpace(fief.OwnerKingdomId)) usedCross.Add(fief.OwnerKingdomId);
		}
		settlements.UnionWith(villages);
		var rejected = new List<string>();
		bool AllowsExplicit(PolicyEffectTargetKind kind, string id)
		{
			if (!localModule || !explicitTargets) return false;
			string kingdomId = ResolveOwnerKingdomId(kind, id, ownerKingdomResolver);
			if (kind == PolicyEffectTargetKind.Clan && kingdomId.Length == 0
				&& module.Descriptor.AllowIndependentClanTargets) return true;
			if (!AllowsKingdom(kingdomId)) return false;
			if (!SameId(kingdomId, home)) usedCross.Add(kingdomId);
			return true;
		}
		List<string> Filter(IEnumerable<string> ids, PolicyEffectTargetKind kind, Func<string, bool> allowed)
		{
			var result = new List<string>();
			foreach (string id in NormalizeIds(ids))
			{
				if (allowed(id)) result.Add(id);
				else rejected.Add(kind + ":" + id);
			}
			return result;
		}
		targets.ParentSettlementIds = Filter(targets.ParentSettlementIds, PolicyEffectTargetKind.Settlement, parents.Contains);
		targets.SettlementIds = Filter(targets.SettlementIds, PolicyEffectTargetKind.Settlement, settlements.Contains);
		targets.TownIds = Filter(targets.TownIds, PolicyEffectTargetKind.Town, parents.Contains);
		targets.VillageIds = Filter(targets.VillageIds, PolicyEffectTargetKind.Village, villages.Contains);
		targets.ClanIds = Filter(targets.ClanIds, PolicyEffectTargetKind.Clan,
			id => clans.Contains(id) || AllowsExplicit(PolicyEffectTargetKind.Clan, id));
		targets.HeroIds = Filter(targets.HeroIds, PolicyEffectTargetKind.Hero,
			id => (module.Descriptor.TargetProjection == PolicyEffectTargetProjectionKind.SettlementOwnerClanLeader && leaders.Contains(id))
				|| AllowsExplicit(PolicyEffectTargetKind.Hero, id));
		targets.KingdomIds = Filter(targets.KingdomIds, PolicyEffectTargetKind.Kingdom,
			id => AllowsExplicit(PolicyEffectTargetKind.Kingdom, id));
		targets.JurisdictionKind = usedCross.Count > 0 ? PolicyEffectTargetJurisdictionKind.CrossKingdom : PolicyEffectTargetJurisdictionKind.Domestic;
		targets.AuthorizedCrossKingdomIds = NormalizeIds(usedCross);
		if (failOnUnauthorized && rejected.Count > 0)
		{
			targetSet = null;
			error = "政策效果目标越过地方封地及明确授权目标边界：" + string.Join(", ", rejected.Take(6));
			return false;
		}
		return true;
	}

	internal static bool CanProjectSettlementOwnerClan(IPolicyEffectModule module, string scope)
	{
		return string.Equals(scope, PolicyEffectScopes.Local, StringComparison.OrdinalIgnoreCase)
			|| module?.Descriptor?.AllowIndependentClanTargets == true;
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
		string homeKingdomId,
		PolicyEffectOwnerKingdomResolver ownerKingdomResolver = null)
	{
		foreach (string kingdomId in NormalizeIds(targetSet?.KingdomIds))
		{
			if (!SameId(kingdomId, homeKingdomId))
			{
				yield return kingdomId;
			}
		}
		foreach (string kingdomId in NormalizeIds(targetSet?.SettlementIds)
			.Select(id => ResolveOwnerKingdomId(PolicyEffectTargetKind.Settlement, id, ownerKingdomResolver))
			.Concat(NormalizeIds(targetSet?.TownIds)
				.Select(id => ResolveOwnerKingdomId(PolicyEffectTargetKind.Town, id, ownerKingdomResolver)))
			.Concat(NormalizeIds(targetSet?.VillageIds)
				.Select(id => ResolveOwnerKingdomId(PolicyEffectTargetKind.Village, id, ownerKingdomResolver)))
			.Concat(NormalizeIds(targetSet?.ParentSettlementIds)
				.Select(id => ResolveOwnerKingdomId(PolicyEffectTargetKind.Settlement, id, ownerKingdomResolver)))
			.Concat(NormalizeIds(targetSet?.ClanIds)
				.Select(id => ResolveOwnerKingdomId(PolicyEffectTargetKind.Clan, id, ownerKingdomResolver)))
			.Concat(NormalizeIds(targetSet?.HeroIds)
				.Select(id => ResolveOwnerKingdomId(PolicyEffectTargetKind.Hero, id, ownerKingdomResolver))))
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

	private static string ResolveOwnerKingdomId(
		PolicyEffectTargetKind targetKind,
		string targetId,
		PolicyEffectOwnerKingdomResolver ownerKingdomResolver)
	{
		if (ownerKingdomResolver != null)
		{
			try
			{
				string resolved = NormalizeId(ownerKingdomResolver(targetKind, targetId));
				if (resolved.Length > 0)
				{
					return resolved;
				}
			}
			catch
			{
			}
		}
		switch (targetKind)
		{
			case PolicyEffectTargetKind.Kingdom:
				return NormalizeId(targetId);
			case PolicyEffectTargetKind.Clan:
				return ResolveClanOwnerKingdomId(targetId);
			case PolicyEffectTargetKind.Hero:
				return ResolveHeroOwnerKingdomId(targetId);
			default:
				return ResolveSettlementOwnerKingdomId(targetId);
		}
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
