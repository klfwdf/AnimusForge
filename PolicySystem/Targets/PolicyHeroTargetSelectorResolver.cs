using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.PolicyEffects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge.PolicyTargets;

internal sealed class PolicyHeroTargetCandidate
{
	internal string SelectorId { get; set; } = string.Empty;

	internal string DisplayName { get; set; } = string.Empty;

	internal string AnchorKingdomId { get; set; } = string.Empty;

	internal IReadOnlyList<string> CurrentHeroIds { get; set; } = Array.Empty<string>();
}

internal static class PolicyHeroTargetSelectorResolver
{
	private const string Prefix = "hero:v1:";

	private static readonly Dictionary<string, Hero> HeroesById
		= new Dictionary<string, Hero>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<Occupation, Dictionary<string, Hero>> HeroesByOccupation
		= new Dictionary<Occupation, Dictionary<string, Hero>>();

	private static readonly Dictionary<string, Hero> NotablesById
		= new Dictionary<string, Hero>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, IReadOnlyList<string>> MaterializedByDayAndSelector
		= new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

	private static readonly Dictionary<string, IReadOnlyList<string>> MaterializedClanIdsByDayAndSelector
		= new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

	private static readonly HashSet<string> SupportedRoles = new HashSet<string>(StringComparer.Ordinal)
	{
		"ruler",
		"lords",
		"clan-leaders",
		"notables"
	};

	private static readonly IReadOnlyDictionary<Occupation, string[]> OccupationTerms
		= new Dictionary<Occupation, string[]>
		{
			[Occupation.Tavernkeeper] = new[] { "酒馆老板", "酒馆主" },
			[Occupation.Mercenary] = new[] { "佣兵" },
			[Occupation.GoodsTrader] = new[] { "货物商人", "贸易商" },
			[Occupation.ArenaMaster] = new[] { "竞技场主管", "竞技场大师" },
			[Occupation.Villager] = new[] { "村民" },
			[Occupation.Soldier] = new[] { "士兵" },
			[Occupation.Townsfolk] = new[] { "城镇居民", "市民" },
			[Occupation.RansomBroker] = new[] { "赎金经纪人", "赎金掮客" },
			[Occupation.Weaponsmith] = new[] { "武器匠" },
			[Occupation.Armorer] = new[] { "护甲商", "盔甲匠" },
			[Occupation.HorseTrader] = new[] { "马商" },
			[Occupation.TavernWench] = new[] { "酒馆侍女", "酒馆女招待" },
			[Occupation.TavernGameHost] = new[] { "酒馆游戏主持人", "棋局主持人" },
			[Occupation.Bandit] = new[] { "强盗", "匪徒" },
			[Occupation.Wanderer] = new[] { "流浪者", "游荡者" },
			[Occupation.Artisan] = new[] { "工匠" },
			[Occupation.Merchant] = new[] { "商人" },
			[Occupation.Preacher] = new[] { "传教士", "牧师" },
			[Occupation.Headman] = new[] { "头人", "村长" },
			[Occupation.GangLeader] = new[] { "帮派首领", "帮派头目" },
			[Occupation.RuralNotable] = new[] { "乡绅", "农村要人" },
			[Occupation.PrisonGuard] = new[] { "监狱守卫", "狱卒" },
			[Occupation.Guard] = new[] { "守卫", "卫兵" },
			[Occupation.ShopWorker] = new[] { "店员", "商店雇员" },
			[Occupation.Blacksmith] = new[] { "铁匠" },
			[Occupation.Musician] = new[] { "乐师", "音乐家" },
			[Occupation.Gangster] = new[] { "帮派成员", "歹徒" },
			[Occupation.BannerBearer] = new[] { "旗手" },
			[Occupation.CaravanGuard] = new[] { "商队护卫" },
			[Occupation.Special] = new[] { "特殊人物" },
			[Occupation.ShipWright] = new[] { "造船师", "船匠" }
		};

	private static Campaign _campaign;
	private static int _materializedCacheDay = int.MinValue;

	internal static PolicyHeroTargetCandidate BuildSpecificHeroCandidate(Hero hero, string displayName)
	{
		EnsureIndex();
		if (!IsActiveHero(hero) || string.IsNullOrWhiteSpace(hero.StringId))
		{
			return null;
		}
		IndexHero(hero);
		return new PolicyHeroTargetCandidate
		{
			SelectorId = Prefix + "specific:" + hero.StringId.Trim(),
			DisplayName = (displayName ?? string.Empty).Trim().Length > 0
				? displayName.Trim()
				: BuildHeroDisplayName(hero),
			AnchorKingdomId = ResolveHeroKingdom(hero)?.StringId ?? string.Empty,
			CurrentHeroIds = new[] { hero.StringId.Trim() }
		};
	}

	internal static IReadOnlyList<PolicyHeroTargetCandidate> BuildCandidates(
		string policyText,
		IEnumerable<Kingdom> anchorKingdoms)
	{
		EnsureIndex();
		string text = (policyText ?? string.Empty).Trim();
		if (text.Length == 0)
		{
			return Array.Empty<PolicyHeroTargetCandidate>();
		}
		List<Kingdom> anchors = (anchorKingdoms ?? Enumerable.Empty<Kingdom>())
			.Where(kingdom => kingdom != null && !kingdom.IsEliminated && !string.IsNullOrWhiteSpace(kingdom.StringId))
			.GroupBy(kingdom => kingdom.StringId, StringComparer.OrdinalIgnoreCase)
			.Select(group => group.First())
			.ToList();
		HashSet<string> anchorIds = new HashSet<string>(anchors.Select(kingdom => kingdom.StringId), StringComparer.OrdinalIgnoreCase);
		List<PolicyHeroTargetCandidate> result = new List<PolicyHeroTargetCandidate>();

		foreach (Hero hero in HeroesById.Values.Where(IsActiveHero))
		{
			string heroId = (hero.StringId ?? string.Empty).Trim();
			string heroName = (hero.Name?.ToString() ?? string.Empty).Trim();
			if (heroId.Length == 0 || !IsHeroInAnchors(hero, anchorIds)
				|| (!Mentions(text, heroId) && !MentionsUniqueHeroName(text, heroName, heroId, anchorIds)))
			{
				continue;
			}
			string kingdomId = ResolveHeroKingdom(hero)?.StringId ?? string.Empty;
			result.Add(new PolicyHeroTargetCandidate
			{
				SelectorId = Prefix + "specific:" + heroId,
				DisplayName = BuildHeroDisplayName(hero),
				AnchorKingdomId = kingdomId,
				CurrentHeroIds = new[] { heroId }
			});
		}

		foreach (Kingdom kingdom in anchors)
		{
			AddRoleCandidate(result, text, kingdom, "ruler", new[] { "当前统治者", "统治者", "君主", "国王", "女王", "皇帝", "女皇", "可汗", "苏丹", "执政者" }, "当前统治者");
			AddRoleCandidate(result, text, kingdom, "lords", new[] { "所有领主", "全体领主", "领主们", "领主" }, "当前所有领主");
			AddRoleCandidate(result, text, kingdom, "clan-leaders", new[] { "所有族长", "全体族长", "家族领袖", "家族首领", "族长" }, "当前所有家族领袖");
			AddRoleCandidate(result, text, kingdom, "notables", new[] { "所有要人", "全体要人", "地方要人", "要人" }, "当前所有要人");

			foreach (Occupation occupation in Enum.GetValues(typeof(Occupation)).Cast<Occupation>())
			{
				if (occupation == Occupation.NotAssigned
					|| occupation == Occupation.NumberOfOccupations
					|| occupation == Occupation.Lord)
				{
					continue;
				}
				IEnumerable<string> terms = OccupationTerms.TryGetValue(occupation, out string[] aliases)
					? aliases.Concat(new[] { occupation.ToString() })
					: new[] { occupation.ToString() };
				if (!terms.Any(term => Mentions(text, term)))
				{
					continue;
				}
				string selectorId = Prefix + "occupation:" + occupation + ":" + kingdom.StringId;
				TryResolveSelector(selectorId, CurrentCampaignDay(), out IReadOnlyList<string> heroIds, out _);
				result.Add(new PolicyHeroTargetCandidate
				{
					SelectorId = selectorId,
					DisplayName = GetKingdomName(kingdom) + "的当前" + GetOccupationDisplayName(occupation),
					AnchorKingdomId = kingdom.StringId,
					CurrentHeroIds = heroIds
				});
			}
		}

		return result
			.Where(candidate => candidate != null && candidate.CurrentHeroIds.Count > 0)
			.GroupBy(candidate => candidate.SelectorId, StringComparer.Ordinal)
			.Select(group => group.First())
			.OrderBy(candidate => candidate.SelectorId, StringComparer.Ordinal)
			.ToList();
	}

	internal static IReadOnlyList<PolicyHeroTargetCandidate> BuildAvailableGroupCandidates(
		IEnumerable<Kingdom> anchorKingdoms)
	{
		EnsureIndex();
		List<Kingdom> anchors = (anchorKingdoms ?? Enumerable.Empty<Kingdom>())
			.Where(kingdom => kingdom != null && !kingdom.IsEliminated && !string.IsNullOrWhiteSpace(kingdom.StringId))
			.GroupBy(kingdom => kingdom.StringId, StringComparer.OrdinalIgnoreCase)
			.Select(group => group.First())
			.ToList();
		List<PolicyHeroTargetCandidate> result = new List<PolicyHeroTargetCandidate>();
		foreach (Kingdom kingdom in anchors)
		{
			AddResolvedRoleCandidate(result, kingdom, "ruler", "当前统治者");
			AddResolvedRoleCandidate(result, kingdom, "lords", "当前所有领主");
			AddResolvedRoleCandidate(result, kingdom, "clan-leaders", "当前所有家族领袖");
			AddResolvedRoleCandidate(result, kingdom, "notables", "当前所有要人");
			foreach (Occupation occupation in Enum.GetValues(typeof(Occupation)).Cast<Occupation>())
			{
				if (occupation == Occupation.NotAssigned
					|| occupation == Occupation.NumberOfOccupations
					|| occupation == Occupation.Lord)
				{
					continue;
				}
				AddResolvedOccupationCandidate(result, kingdom, occupation);
			}
		}
		return result
			.Where(candidate => candidate != null && candidate.CurrentHeroIds.Count > 0)
			.GroupBy(candidate => candidate.SelectorId, StringComparer.Ordinal)
			.Select(group => group.First())
			.OrderBy(candidate => candidate.SelectorId, StringComparer.Ordinal)
			.ToList();
	}

	internal static IReadOnlyList<PolicyHeroTargetCandidate> BuildAvailableRoleCandidates(
		IEnumerable<Kingdom> anchorKingdoms,
		IEnumerable<string> roles)
	{
		EnsureIndex();
		HashSet<string> requestedRoles = new HashSet<string>(
			roles ?? Array.Empty<string>(),
			StringComparer.Ordinal);
		List<PolicyHeroTargetCandidate> result = new List<PolicyHeroTargetCandidate>();
		foreach (Kingdom kingdom in (anchorKingdoms ?? Enumerable.Empty<Kingdom>())
			.Where(kingdom => kingdom != null && !kingdom.IsEliminated && !string.IsNullOrWhiteSpace(kingdom.StringId))
			.GroupBy(kingdom => kingdom.StringId, StringComparer.OrdinalIgnoreCase)
			.Select(group => group.First()))
		{
			if (requestedRoles.Contains("ruler")) AddResolvedRoleCandidate(result, kingdom, "ruler", "当前统治者");
			if (requestedRoles.Contains("lords")) AddResolvedRoleCandidate(result, kingdom, "lords", "当前所有领主");
			if (requestedRoles.Contains("clan-leaders")) AddResolvedRoleCandidate(result, kingdom, "clan-leaders", "当前所有家族领袖");
			if (requestedRoles.Contains("notables")) AddResolvedRoleCandidate(result, kingdom, "notables", "当前所有要人");
		}
		return result
			.Where(candidate => candidate != null && candidate.CurrentHeroIds.Count > 0)
			.GroupBy(candidate => candidate.SelectorId, StringComparer.Ordinal)
			.Select(group => group.First())
			.OrderBy(candidate => candidate.SelectorId, StringComparer.Ordinal)
			.ToList();
	}

	internal static bool TryResolveSelector(
		string selectorId,
		int campaignDay,
		out IReadOnlyList<string> heroIds,
		out string error)
	{
		EnsureIndex();
		heroIds = Array.Empty<string>();
		error = string.Empty;
		string normalized = (selectorId ?? string.Empty).Trim();
		if (!TryParse(normalized, out string kind, out string value, out string kingdomId)
			|| !IsSupportedSelector(kind, value))
		{
			error = "unknown hero selector";
			return false;
		}
		if (_materializedCacheDay != campaignDay)
		{
			MaterializedByDayAndSelector.Clear();
			MaterializedClanIdsByDayAndSelector.Clear();
			_materializedCacheDay = campaignDay;
		}
		string cacheKey = campaignDay.ToString(CultureInfo.InvariantCulture) + "\u001f" + normalized;
		if (MaterializedByDayAndSelector.TryGetValue(cacheKey, out heroIds))
		{
			return true;
		}

		IEnumerable<Hero> heroes;
		if (string.Equals(kind, "specific", StringComparison.Ordinal))
		{
			heroes = HeroesById.TryGetValue(value, out Hero hero)
				? new[] { hero }
				: Array.Empty<Hero>();
		}
		else
		{
			Kingdom kingdom = ResolveKingdom(kingdomId);
			if (kingdom == null || kingdom.IsEliminated)
			{
				heroes = Array.Empty<Hero>();
			}
			else if (string.Equals(kind, "role", StringComparison.Ordinal))
			{
				heroes = ResolveRole(value, kingdom);
			}
			else if (string.Equals(kind, "occupation", StringComparison.Ordinal)
				&& Enum.TryParse(value, ignoreCase: false, out Occupation occupation)
				&& occupation != Occupation.NotAssigned
				&& occupation != Occupation.NumberOfOccupations)
			{
				heroes = ResolveOccupation(occupation, kingdom);
			}
			else
			{
				error = "hero selector kind or occupation is invalid";
				return false;
			}
		}

		if (string.Equals(kind, "specific", StringComparison.Ordinal))
		{
			heroIds = new[] { value };
		}
		else
		{
			heroIds = heroes
				.Where(IsActiveHero)
				.Select(hero => (hero.StringId ?? string.Empty).Trim())
				.Where(id => id.Length > 0)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(id => id, StringComparer.Ordinal)
				.ToList();
		}
		MaterializedByDayAndSelector[cacheKey] = heroIds;
		return true;
	}

	internal static bool TryProjectSelector(
		string selectorId,
		IPolicyEffectModule module,
		int campaignDay,
		out PolicyEffectCanonicalTargetSet targetSet,
		out string error)
	{
		targetSet = null;
		error = string.Empty;
		if (module?.Descriptor == null
			|| !TryResolveSelector(selectorId, campaignDay, out IReadOnlyList<string> heroIds, out error))
		{
			return false;
		}
		PolicyEffectCanonicalTargetSet projected = new PolicyEffectCanonicalTargetSet
		{
			StructureVersion = 1,
			SelectorIds = new List<string> { selectorId }
		};
		if (module.Descriptor.TargetKinds.Contains(PolicyEffectTargetKind.Hero))
		{
			projected.HeroIds = heroIds.ToList();
		}
		if (module.Descriptor.TargetKinds.Contains(PolicyEffectTargetKind.Clan))
		{
			bool allowIndependentClans = module.Descriptor.AllowIndependentClanTargets;
			string clanCacheKey = campaignDay.ToString(CultureInfo.InvariantCulture)
				+ "\u001f" + selectorId
				+ "\u001f" + (allowIndependentClans ? "independent" : "kingdomOnly");
			if (!MaterializedClanIdsByDayAndSelector.TryGetValue(clanCacheKey, out IReadOnlyList<string> clanIds))
			{
				clanIds = heroIds
					.Select(id => HeroesById.TryGetValue(id, out Hero hero) ? hero?.Clan : null)
					.Where(clan => clan != null
						&& !clan.IsEliminated
						&& (allowIndependentClans || clan.Kingdom != null))
					.Select(clan => clan.StringId)
					.Where(id => !string.IsNullOrWhiteSpace(id))
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.OrderBy(id => id, StringComparer.Ordinal)
					.ToList();
				MaterializedClanIdsByDayAndSelector[clanCacheKey] = clanIds;
			}
			projected.ClanIds = clanIds.ToList();
		}
		targetSet = projected;
		if ((projected.HeroIds.Count == 0 && module.Descriptor.TargetKinds.Contains(PolicyEffectTargetKind.Hero))
			|| (projected.ClanIds.Count == 0 && module.Descriptor.TargetKinds.Contains(PolicyEffectTargetKind.Clan)))
		{
			error = "hero selector has no executable target for module " + module.Id;
			return false;
		}
		error = string.Empty;
		return true;
	}

	internal static bool IsKnownSelector(string selectorId)
	{
		return TryParse((selectorId ?? string.Empty).Trim(), out string kind, out string value, out _)
			&& IsSupportedSelector(kind, value);
	}

	internal static bool TryDescribeSelector(
		string selectorId,
		out string kind,
		out string value,
		out string kingdomId)
	{
		return TryParse((selectorId ?? string.Empty).Trim(), out kind, out value, out kingdomId)
			&& IsSupportedSelector(kind, value);
	}

	internal static bool IsSelectorExplicitlyMentioned(
		string selectorId,
		string policyText,
		ISet<string> allowedAnchorKingdomIds)
	{
		EnsureIndex();
		string text = (policyText ?? string.Empty).Trim();
		if (text.Length == 0
			|| !TryDescribeSelector(selectorId, out string kind, out string value, out string kingdomId))
		{
			return false;
		}
		if (string.Equals(kind, "specific", StringComparison.Ordinal))
		{
			if (!HeroesById.TryGetValue(value, out Hero hero) || !IsActiveHero(hero))
			{
				return false;
			}
			string ownerKingdomId = ResolveHeroKingdom(hero)?.StringId ?? string.Empty;
			if (allowedAnchorKingdomIds != null && !allowedAnchorKingdomIds.Contains(ownerKingdomId))
			{
				return false;
			}
			return MentionsIdentifier(text, value)
				|| MentionsUniqueHeroName(
					text,
					hero.Name?.ToString() ?? string.Empty,
					value,
					allowedAnchorKingdomIds ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase));
		}
		if (allowedAnchorKingdomIds != null && !allowedAnchorKingdomIds.Contains(kingdomId))
		{
			return false;
		}
		if (string.Equals(kind, "role", StringComparison.Ordinal))
		{
			IEnumerable<string> terms = string.Equals(value, "ruler", StringComparison.Ordinal)
				? new[] { "当前统治者", "统治者", "君主", "国王", "女王", "皇帝", "女皇", "可汗", "苏丹", "执政者" }
				: string.Equals(value, "lords", StringComparison.Ordinal)
					? new[] { "所有领主", "全体领主", "领主们", "领主" }
					: string.Equals(value, "clan-leaders", StringComparison.Ordinal)
						? new[] { "所有族长", "全体族长", "家族领袖", "家族首领", "族长" }
						: new[] { "所有要人", "全体要人", "地方要人", "要人" };
			return terms.Any(term => Mentions(text, term));
		}
		return Enum.TryParse(value, ignoreCase: false, out Occupation occupation)
			&& OccupationTerms.TryGetValue(occupation, out string[] aliases)
			&& aliases.Concat(new[] { occupation.ToString() }).Any(term => Mentions(text, term));
	}

	internal static void OnHeroChanged(Hero hero)
	{
		EnsureIndex();
		RemoveHero(hero?.StringId);
		IndexHero(hero);
	}

	internal static void OnHeroRemoved(Hero hero)
	{
		EnsureIndex();
		RemoveHero(hero?.StringId);
	}

	private static void EnsureIndex()
	{
		if (ReferenceEquals(_campaign, Campaign.Current))
		{
			return;
		}
		_campaign = Campaign.Current;
		HeroesById.Clear();
		HeroesByOccupation.Clear();
		NotablesById.Clear();
		MaterializedByDayAndSelector.Clear();
		MaterializedClanIdsByDayAndSelector.Clear();
		_materializedCacheDay = int.MinValue;
		foreach (Hero hero in Hero.AllAliveHeroes ?? Enumerable.Empty<Hero>())
		{
			IndexHero(hero);
		}
	}

	private static void IndexHero(Hero hero)
	{
		if (!IsActiveHero(hero) || string.IsNullOrWhiteSpace(hero.StringId))
		{
			return;
		}
		string id = hero.StringId.Trim();
		HeroesById[id] = hero;
		if (hero.IsNotable)
		{
			NotablesById[id] = hero;
		}
		if (!HeroesByOccupation.TryGetValue(hero.Occupation, out Dictionary<string, Hero> bucket))
		{
			bucket = new Dictionary<string, Hero>(StringComparer.OrdinalIgnoreCase);
			HeroesByOccupation[hero.Occupation] = bucket;
		}
		bucket[id] = hero;
	}

	private static void RemoveHero(string heroId)
	{
		string id = (heroId ?? string.Empty).Trim();
		if (id.Length == 0)
		{
			return;
		}
		HeroesById.Remove(id);
		NotablesById.Remove(id);
		foreach (Dictionary<string, Hero> bucket in HeroesByOccupation.Values)
		{
			bucket.Remove(id);
		}
	}

	private static void AddRoleCandidate(
		ICollection<PolicyHeroTargetCandidate> result,
		string text,
		Kingdom kingdom,
		string role,
		IEnumerable<string> terms,
		string displayName)
	{
		if (!(terms ?? Array.Empty<string>()).Any(term => Mentions(text, term)))
		{
			return;
		}
		AddResolvedRoleCandidate(result, kingdom, role, displayName);
	}

	private static void AddResolvedRoleCandidate(
		ICollection<PolicyHeroTargetCandidate> result,
		Kingdom kingdom,
		string role,
		string displayName)
	{
		string selectorId = Prefix + "role:" + role + ":" + kingdom.StringId;
		TryResolveSelector(selectorId, CurrentCampaignDay(), out IReadOnlyList<string> ids, out _);
		result.Add(new PolicyHeroTargetCandidate
		{
			SelectorId = selectorId,
			DisplayName = GetKingdomName(kingdom) + "的" + displayName,
			AnchorKingdomId = kingdom.StringId,
			CurrentHeroIds = ids
		});
	}

	private static void AddResolvedOccupationCandidate(
		ICollection<PolicyHeroTargetCandidate> result,
		Kingdom kingdom,
		Occupation occupation)
	{
		string selectorId = Prefix + "occupation:" + occupation + ":" + kingdom.StringId;
		TryResolveSelector(selectorId, CurrentCampaignDay(), out IReadOnlyList<string> heroIds, out _);
		result.Add(new PolicyHeroTargetCandidate
		{
			SelectorId = selectorId,
			DisplayName = GetKingdomName(kingdom) + "的当前" + GetOccupationDisplayName(occupation),
			AnchorKingdomId = kingdom.StringId,
			CurrentHeroIds = heroIds
		});
	}

	private static IEnumerable<Hero> ResolveRole(string role, Kingdom kingdom)
	{
		if (string.Equals(role, "ruler", StringComparison.Ordinal))
		{
			return kingdom.Leader == null ? Array.Empty<Hero>() : new[] { kingdom.Leader };
		}
		IEnumerable<Clan> clans = ((IEnumerable<Clan>)kingdom.Clans ?? Enumerable.Empty<Clan>())
			.Where(clan => clan != null && !clan.IsEliminated && clan.Kingdom == kingdom);
		if (string.Equals(role, "lords", StringComparison.Ordinal))
		{
			IEnumerable<Hero> lords = clans.SelectMany(clan => clan.Heroes ?? Enumerable.Empty<Hero>())
				.Where(hero => hero?.Occupation == Occupation.Lord);
			return kingdom.Leader == null ? lords : lords.Concat(new[] { kingdom.Leader });
		}
		if (string.Equals(role, "clan-leaders", StringComparison.Ordinal))
		{
			return clans.Select(clan => clan.Leader).Where(hero => hero != null);
		}
		if (string.Equals(role, "notables", StringComparison.Ordinal))
		{
			return NotablesById.Values.Where(hero => IsHeroInKingdom(hero, kingdom));
		}
		return Array.Empty<Hero>();
	}

	private static IEnumerable<Hero> ResolveOccupation(Occupation occupation, Kingdom kingdom)
	{
		if (occupation == Occupation.Lord)
		{
			return ResolveRole("lords", kingdom);
		}
		if (!HeroesByOccupation.TryGetValue(occupation, out Dictionary<string, Hero> bucket))
		{
			return Array.Empty<Hero>();
		}
		if (occupation == Occupation.Wanderer)
		{
			return bucket.Values.Where(hero => SettlementBelongsToKingdom(hero.CurrentSettlement, kingdom));
		}
		return bucket.Values.Where(hero => SettlementBelongsToKingdom(hero.HomeSettlement, kingdom));
	}

	private static bool TryParse(string selectorId, out string kind, out string value, out string kingdomId)
	{
		kind = string.Empty;
		value = string.Empty;
		kingdomId = string.Empty;
		if (!selectorId.StartsWith(Prefix, StringComparison.Ordinal))
		{
			return false;
		}
		string[] parts = selectorId.Substring(Prefix.Length).Split(':');
		if (parts.Length == 2
			&& string.Equals(parts[0], "specific", StringComparison.Ordinal))
		{
			kind = parts[0];
			value = parts[1].Trim();
			return value.Length > 0;
		}
		if (parts.Length == 3
			&& (string.Equals(parts[0], "role", StringComparison.Ordinal)
				|| string.Equals(parts[0], "occupation", StringComparison.Ordinal)))
		{
			kind = parts[0];
			value = parts[1].Trim();
			kingdomId = parts[2].Trim();
			return value.Length > 0 && kingdomId.Length > 0;
		}
		return false;
	}

	private static bool IsSupportedSelector(string kind, string value)
	{
		if (string.Equals(kind, "specific", StringComparison.Ordinal))
		{
			return !string.IsNullOrWhiteSpace(value);
		}
		if (string.Equals(kind, "role", StringComparison.Ordinal))
		{
			return SupportedRoles.Contains(value ?? string.Empty);
		}
		return string.Equals(kind, "occupation", StringComparison.Ordinal)
			&& Enum.TryParse(value, ignoreCase: false, out Occupation occupation)
			&& Enum.IsDefined(typeof(Occupation), occupation)
			&& occupation != Occupation.NotAssigned
			&& occupation != Occupation.NumberOfOccupations;
	}

	private static bool MentionsUniqueHeroName(
		string text,
		string heroName,
		string heroId,
		ISet<string> anchorKingdomIds)
	{
		if (!Mentions(text, heroName))
		{
			return false;
		}
		return HeroesById.Values.Count(candidate => IsActiveHero(candidate)
			&& IsHeroInAnchors(candidate, anchorKingdomIds)
			&& string.Equals(candidate.Name?.ToString()?.Trim(), heroName, StringComparison.OrdinalIgnoreCase)) == 1
			&& HeroesById.ContainsKey(heroId);
	}

	private static bool Mentions(string text, string term)
	{
		string value = (term ?? string.Empty).Trim();
		return value.Length >= 2 && (text ?? string.Empty).IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static bool MentionsIdentifier(string text, string identifier)
	{
		string source = text ?? string.Empty;
		string value = (identifier ?? string.Empty).Trim();
		if (value.Length < 2)
		{
			return false;
		}
		int index = 0;
		while ((index = source.IndexOf(value, index, StringComparison.OrdinalIgnoreCase)) >= 0)
		{
			int end = index + value.Length;
			bool leftBoundary = index == 0 || !IsIdentifierCharacter(source[index - 1]);
			bool rightBoundary = end >= source.Length || !IsIdentifierCharacter(source[end]);
			if (leftBoundary && rightBoundary)
			{
				return true;
			}
			index = end;
		}
		return false;
	}

	private static bool IsIdentifierCharacter(char value)
	{
		return value <= 127 && (char.IsLetterOrDigit(value) || value == '_');
	}

	private static bool IsActiveHero(Hero hero)
	{
		return hero != null && !hero.IsDead && !hero.IsDisabled;
	}

	private static bool IsHeroInAnchors(Hero hero, ISet<string> anchorKingdomIds)
	{
		Kingdom kingdom = ResolveHeroKingdom(hero);
		return kingdom != null && anchorKingdomIds.Contains(kingdom.StringId ?? string.Empty);
	}

	private static bool IsHeroInKingdom(Hero hero, Kingdom kingdom)
	{
		if (hero?.Clan?.Kingdom == kingdom)
		{
			return true;
		}
		if (hero?.Occupation == Occupation.Wanderer)
		{
			return SettlementBelongsToKingdom(hero.CurrentSettlement, kingdom);
		}
		return SettlementBelongsToKingdom(hero?.HomeSettlement, kingdom);
	}

	private static Kingdom ResolveHeroKingdom(Hero hero)
	{
		return hero?.Clan?.Kingdom
			?? hero?.CurrentSettlement?.OwnerClan?.Kingdom
			?? hero?.HomeSettlement?.OwnerClan?.Kingdom;
	}

	private static bool SettlementBelongsToKingdom(Settlement settlement, Kingdom kingdom)
	{
		return settlement != null && kingdom != null && settlement.OwnerClan?.Kingdom == kingdom;
	}

	private static Kingdom ResolveKingdom(string kingdomId)
	{
		return (Kingdom.All ?? Enumerable.Empty<Kingdom>()).FirstOrDefault(kingdom => kingdom != null
			&& string.Equals(kingdom.StringId, kingdomId, StringComparison.OrdinalIgnoreCase));
	}

	private static string BuildHeroDisplayName(Hero hero)
	{
		string clanName = hero?.Clan?.Name?.ToString() ?? "无家族";
		return (hero?.Name?.ToString() ?? hero?.StringId ?? "未知人物")
			+ "（" + clanName + "，" + GetOccupationDisplayName(hero?.Occupation ?? Occupation.NotAssigned) + "）";
	}

	private static string GetOccupationDisplayName(Occupation occupation)
	{
		return OccupationTerms.TryGetValue(occupation, out string[] aliases) && aliases.Length > 0
			? aliases[0]
			: occupation == Occupation.Lord ? "领主" : occupation.ToString();
	}

	private static string GetKingdomName(Kingdom kingdom)
	{
		return kingdom?.Name?.ToString() ?? kingdom?.StringId ?? "未知王国";
	}

	private static int CurrentCampaignDay()
	{
		try
		{
			return Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
		}
		catch
		{
			return 0;
		}
	}
}
