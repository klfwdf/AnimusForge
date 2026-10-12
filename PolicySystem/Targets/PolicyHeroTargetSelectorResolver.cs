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
			[Occupation.Tavernkeeper] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_62634b5f8429"), PolicyCompatibilityTexts.Get("Targets_Compat_56895b93f64b") },
			[Occupation.Mercenary] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_f03f3f90563f") },
			[Occupation.GoodsTrader] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_f62095a76fe5"), PolicyCompatibilityTexts.Get("Targets_Compat_9b3b994dec9f") },
			[Occupation.ArenaMaster] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_050c0d064eaf"), PolicyCompatibilityTexts.Get("Targets_Compat_cfb2d2cb2a3b") },
			[Occupation.Villager] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_4781f6b2a158") },
			[Occupation.Soldier] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_eaa8dd04fd68") },
			[Occupation.Townsfolk] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_e7ef1d83900c"), PolicyCompatibilityTexts.Get("Targets_Compat_58a3651d5dd1") },
			[Occupation.RansomBroker] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_73bdecb324fb"), PolicyCompatibilityTexts.Get("Targets_Compat_72e49be94073") },
			[Occupation.Weaponsmith] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_0f29a12ac910") },
			[Occupation.Armorer] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_dd37254ad900"), PolicyCompatibilityTexts.Get("Targets_Compat_8383d798ac75") },
			[Occupation.HorseTrader] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_e09722db7b4b") },
			[Occupation.TavernWench] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_908f4bf3b5ac"), PolicyCompatibilityTexts.Get("Targets_Compat_def9d6a4c82f") },
			[Occupation.TavernGameHost] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_97888e769f84"), PolicyCompatibilityTexts.Get("Targets_Compat_9e1d2b040537") },
			[Occupation.Bandit] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_596d489dcae6"), PolicyCompatibilityTexts.Get("Targets_Compat_b1547b6d3c61") },
			[Occupation.Wanderer] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_51f50f3dabd7"), PolicyCompatibilityTexts.Get("Targets_Compat_81718282f669") },
			[Occupation.Artisan] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_d8d04c518ebb") },
			[Occupation.Merchant] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_3f1784ce8b75") },
			[Occupation.Preacher] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_6ed922a17105"), PolicyCompatibilityTexts.Get("Targets_Compat_f2e8819849c7") },
			[Occupation.Headman] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_7b92ddb92fc9"), PolicyCompatibilityTexts.Get("Targets_Compat_611147038c8e") },
			[Occupation.GangLeader] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_dac627c37c95"), PolicyCompatibilityTexts.Get("Targets_Compat_6fd73d464c20") },
			[Occupation.RuralNotable] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_a1a3082778e4"), PolicyCompatibilityTexts.Get("Targets_Compat_980ac5d86536") },
			[Occupation.PrisonGuard] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_6ce36f5298f7"), PolicyCompatibilityTexts.Get("Targets_Compat_f27a9ebfdabd") },
			[Occupation.Guard] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_4059fb16b1c5"), PolicyCompatibilityTexts.Get("Targets_Compat_8cc470d04fc9") },
			[Occupation.ShopWorker] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_285a519dade1"), PolicyCompatibilityTexts.Get("Targets_Compat_b0b775a5bdd7") },
			[Occupation.Blacksmith] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_a245622e6f94") },
			[Occupation.Musician] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_729aca2cfff6"), PolicyCompatibilityTexts.Get("Targets_Compat_2624cbbcd3e3") },
			[Occupation.Gangster] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_275756ee5540"), PolicyCompatibilityTexts.Get("Targets_Compat_c41b82316ace") },
			[Occupation.BannerBearer] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_e82c1337ebbb") },
			[Occupation.CaravanGuard] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_e10bd08c2c4f") },
			[Occupation.Special] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_1b97d369c173") },
			[Occupation.ShipWright] = new[] { PolicyCompatibilityTexts.Get("Targets_Compat_ca22ae47b98e"), PolicyCompatibilityTexts.Get("Targets_Compat_0346d206c41b") }
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
			AddRoleCandidate(result, text, kingdom, "ruler", new[] { PolicyCompatibilityTexts.Get("Targets_Compat_137ca5f45b6f"), PolicyCompatibilityTexts.Get("Generation_RulerFallback"), PolicyCompatibilityTexts.Get("Targets_Compat_37ab35120d1a"), PolicyCompatibilityTexts.Get("Targets_Compat_38e917f02192"), PolicyCompatibilityTexts.Get("Targets_Compat_8655bf651c8c"), PolicyCompatibilityTexts.Get("Targets_Compat_3ad2039e3ec0"), PolicyCompatibilityTexts.Get("Targets_Compat_63312de22843"), PolicyCompatibilityTexts.Get("Targets_Compat_42fa36440e74"), PolicyCompatibilityTexts.Get("Targets_Compat_e4a69ce9ceac"), PolicyCompatibilityTexts.Get("Targets_Compat_9dc32e0c873d") }, PolicyGenerationPromptTexts.Get("Targets_Text_137ca5f45b6f", "current ruler"));
			AddRoleCandidate(result, text, kingdom, "lords", new[] { PolicyCompatibilityTexts.Get("Targets_Compat_8b032bde3294"), PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyWholeKingdomLordTargetLabel_107"), PolicyCompatibilityTexts.Get("Targets_Compat_8c69ba6ace29"), PolicyCompatibilityTexts.Get("Targets_Compat_90c42cbd6c54") }, PolicyGenerationPromptTexts.Get("Targets_Text_1171396f25cb", "all current lords"));
			AddRoleCandidate(result, text, kingdom, "clan-leaders", new[] { PolicyCompatibilityTexts.Get("Targets_Compat_4fc5566eed7e"), PolicyCompatibilityTexts.Get("Targets_Compat_fc97f7889295"), PolicyCompatibilityTexts.Get("Targets_Compat_0e57ea8634d0"), PolicyCompatibilityTexts.Get("Targets_Compat_54b4d4fc1c82"), PolicyCompatibilityTexts.Get("Targets_Compat_c646abd11391") }, PolicyGenerationPromptTexts.Get("Targets_Text_bd19c7e26e2e", "all current clan leaders"));
			AddRoleCandidate(result, text, kingdom, "notables", new[] { PolicyCompatibilityTexts.Get("Targets_Compat_65874efb7391"), PolicyCompatibilityTexts.Get("Targets_Compat_28eb64b5045b"), PolicyCompatibilityTexts.Get("Targets_Compat_99d3071e54b8"), PolicyCompatibilityTexts.Get("Targets_Compat_277123eb2215") }, PolicyGenerationPromptTexts.Get("Targets_Text_8c48e34b8885", "all current notables"));

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
					DisplayName = GetKingdomName(kingdom) + PolicyGenerationPromptTexts.Get("Targets_Text_51509c6effc7", "'s current ") + GetOccupationDisplayName(occupation),
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
			AddResolvedRoleCandidate(result, kingdom, "ruler", PolicyGenerationPromptTexts.Get("Targets_Text_137ca5f45b6f", "current ruler"));
			AddResolvedRoleCandidate(result, kingdom, "lords", PolicyGenerationPromptTexts.Get("Targets_Text_1171396f25cb", "all current lords"));
			AddResolvedRoleCandidate(result, kingdom, "clan-leaders", PolicyGenerationPromptTexts.Get("Targets_Text_bd19c7e26e2e", "all current clan leaders"));
			AddResolvedRoleCandidate(result, kingdom, "notables", PolicyGenerationPromptTexts.Get("Targets_Text_8c48e34b8885", "all current notables"));
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
			if (requestedRoles.Contains("ruler")) AddResolvedRoleCandidate(result, kingdom, "ruler", PolicyGenerationPromptTexts.Get("Targets_Text_137ca5f45b6f", "current ruler"));
			if (requestedRoles.Contains("lords")) AddResolvedRoleCandidate(result, kingdom, "lords", PolicyGenerationPromptTexts.Get("Targets_Text_1171396f25cb", "all current lords"));
			if (requestedRoles.Contains("clan-leaders")) AddResolvedRoleCandidate(result, kingdom, "clan-leaders", PolicyGenerationPromptTexts.Get("Targets_Text_bd19c7e26e2e", "all current clan leaders"));
			if (requestedRoles.Contains("notables")) AddResolvedRoleCandidate(result, kingdom, "notables", PolicyGenerationPromptTexts.Get("Targets_Text_8c48e34b8885", "all current notables"));
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
				? new[] { PolicyCompatibilityTexts.Get("Targets_Compat_137ca5f45b6f"), PolicyCompatibilityTexts.Get("Generation_RulerFallback"), PolicyCompatibilityTexts.Get("Targets_Compat_37ab35120d1a"), PolicyCompatibilityTexts.Get("Targets_Compat_38e917f02192"), PolicyCompatibilityTexts.Get("Targets_Compat_8655bf651c8c"), PolicyCompatibilityTexts.Get("Targets_Compat_3ad2039e3ec0"), PolicyCompatibilityTexts.Get("Targets_Compat_63312de22843"), PolicyCompatibilityTexts.Get("Targets_Compat_42fa36440e74"), PolicyCompatibilityTexts.Get("Targets_Compat_e4a69ce9ceac"), PolicyCompatibilityTexts.Get("Targets_Compat_9dc32e0c873d") }
				: string.Equals(value, "lords", StringComparison.Ordinal)
					? new[] { PolicyCompatibilityTexts.Get("Targets_Compat_8b032bde3294"), PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyWholeKingdomLordTargetLabel_107"), PolicyCompatibilityTexts.Get("Targets_Compat_8c69ba6ace29"), PolicyCompatibilityTexts.Get("Targets_Compat_90c42cbd6c54") }
					: string.Equals(value, "clan-leaders", StringComparison.Ordinal)
						? new[] { PolicyCompatibilityTexts.Get("Targets_Compat_4fc5566eed7e"), PolicyCompatibilityTexts.Get("Targets_Compat_fc97f7889295"), PolicyCompatibilityTexts.Get("Targets_Compat_0e57ea8634d0"), PolicyCompatibilityTexts.Get("Targets_Compat_54b4d4fc1c82"), PolicyCompatibilityTexts.Get("Targets_Compat_c646abd11391") }
						: new[] { PolicyCompatibilityTexts.Get("Targets_Compat_65874efb7391"), PolicyCompatibilityTexts.Get("Targets_Compat_28eb64b5045b"), PolicyCompatibilityTexts.Get("Targets_Compat_99d3071e54b8"), PolicyCompatibilityTexts.Get("Targets_Compat_277123eb2215") };
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
			DisplayName = GetKingdomName(kingdom) + PolicyGenerationPromptTexts.Get("Targets_Text_8af2350cfd65", "'s ") + displayName,
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
			DisplayName = GetKingdomName(kingdom) + PolicyGenerationPromptTexts.Get("Targets_Text_51509c6effc7", "'s current ") + GetOccupationDisplayName(occupation),
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
		string clanName = hero?.Clan?.Name?.ToString() ?? PolicyGenerationPromptTexts.Get("Targets_Text_e71c56cb896f", "no clan");
		return (hero?.Name?.ToString() ?? hero?.StringId ?? PolicyGenerationPromptTexts.Get("Targets_Text_33c3073f535a", "unknown character"))
			+ "（" + clanName + "，" + GetOccupationDisplayName(hero?.Occupation ?? Occupation.NotAssigned) + "）";
	}

	private static string GetOccupationDisplayName(Occupation occupation)
	{
		return OccupationTerms.TryGetValue(occupation, out string[] aliases) && aliases.Length > 0
			? PolicyGenerationPromptTexts.Get("Targets_Occupation_" + occupation, occupation.ToString())
			: occupation == Occupation.Lord ? PolicyGenerationPromptTexts.Get("Targets_Text_90c42cbd6c54", "lord") : occupation.ToString();
	}

	private static string GetKingdomName(Kingdom kingdom)
	{
		return kingdom?.Name?.ToString() ?? kingdom?.StringId ?? PolicyGenerationPromptTexts.Get("UnknownKingdom", "unknown kingdom");
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
