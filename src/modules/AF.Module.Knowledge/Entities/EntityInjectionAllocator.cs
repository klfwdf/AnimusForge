using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AnimusForge;

/// <summary>Detached, type-agnostic candidate for the global injection limit. <see cref="Match"/> is an opaque host payload.</summary>
internal sealed class GlobalEntityCandidate
{
	public string Type;
	public string Key;
	public string Name;
	public string Mention;
	public int MentionPriority;
	public int TypePriority;
	public float Score;
	public float FinalScore;
	public bool ExactNameMatch;
	public string HeroClanId;
	public string HeroKingdomId;
	public string ScopeClanId;
	public string ScopeKingdomId;
	public int HeroScopeScore;
	public string HeroScopeEvidence;
	public float HeroDistance = float.MaxValue;
	public float HeroDistanceBonus;
	public object Match;
}

internal sealed class EntityScopeConstraint
{
	public string ClanId;
	public string KingdomId;
	public int MentionPriority;
	public string Name;
}

/// <summary>
/// Pure allocation of matched entities under the global inject cap: ambiguous person-name detection, clan/kingdom
/// scope boosts for heroes, proximity bonus, per-mention ranking, primary pass (one per mention, collision fallback)
/// and secondary pass (up to 3 per mention when the cap exceeds the mention count).
/// </summary>
internal static class EntityInjectionAllocator
{
	internal const int DefaultMaxInjectedEntities = 6;
	internal const int MaxInjectedEntitiesHardCap = 20;
	internal const int MaxSecondaryMatchesPerMention = 3;
	internal const float MaxHeroProximityBonus = 0.15f;
	internal const float HeroProximityDecayDistance = 30f;

	internal static int ClampMaxInjectedEntities(int value)
	{
		if (value < 1)
		{
			return 1;
		}
		if (value > MaxInjectedEntitiesHardCap)
		{
			return MaxInjectedEntitiesHardCap;
		}
		return value;
	}

	/// <summary>Proximity bonus for a hero at <paramref name="distance"/> (exp decay); null when the distance is unusable.</summary>
	internal static float? ComputeDistanceBonus(float distance)
	{
		if (float.IsNaN(distance) || float.IsInfinity(distance) || distance < 0f || distance >= float.MaxValue * 0.5f)
		{
			return null;
		}
		return MaxHeroProximityBonus * (float)Math.Exp(0f - distance / HeroProximityDecayDistance);
	}

	internal static string PreviewLogValue(string value, int maxLen)
	{
		string text = (value ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (maxLen <= 0 || text.Length <= maxLen)
		{
			return text;
		}
		return text.Substring(0, maxLen) + "...";
	}

	internal static List<GlobalEntityCandidate> Select(List<GlobalEntityCandidate> candidates, int maxCount, int mentionCount, out string allocationSummary)
	{
		List<GlobalEntityCandidate> result = new List<GlobalEntityCandidate>();
		HashSet<string> selectedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<GlobalEntityCandidate> candidateList = (candidates ?? new List<GlobalEntityCandidate>())
			.Where((GlobalEntityCandidate x) => x != null && !string.IsNullOrWhiteSpace(x.Key))
			.ToList();
		HashSet<int> ambiguousPersonNamePriorities = FindAmbiguousPersonNamePriorities(candidateList);
		List<EntityScopeConstraint> scopeConstraints = BuildEntityScopeConstraints(candidateList, ambiguousPersonNamePriorities);
		int scopeBoostedHeroes = ApplyHeroScopePriorities(candidateList, ambiguousPersonNamePriorities, scopeConstraints);
		int distanceBoostedHeroes = ApplyHeroDistanceBonuses(candidateList, ambiguousPersonNamePriorities);
		Dictionary<int, List<GlobalEntityCandidate>> rankedByMention = candidateList
			.GroupBy((GlobalEntityCandidate x) => x.MentionPriority)
			.ToDictionary(
				(IGrouping<int, GlobalEntityCandidate> x) => x.Key,
				(IGrouping<int, GlobalEntityCandidate> x) => x
					.OrderByDescending((GlobalEntityCandidate y) => y.HeroScopeScore)
					.ThenByDescending((GlobalEntityCandidate y) => y.FinalScore)
					.ThenByDescending((GlobalEntityCandidate y) => y.Score)
					.ThenByDescending((GlobalEntityCandidate y) => y.ExactNameMatch)
					.ThenBy((GlobalEntityCandidate y) => y.TypePriority)
					.ThenBy((GlobalEntityCandidate y) => y.Name ?? "", StringComparer.OrdinalIgnoreCase)
					.ThenBy((GlobalEntityCandidate y) => y.Key ?? "", StringComparer.OrdinalIgnoreCase)
					.ToList());
		List<int> mentionPriorities = new List<int>();
		HashSet<int> knownPriorities = new HashSet<int>();
		for (int i = 0; i < mentionCount; i++)
		{
			mentionPriorities.Add(i);
			knownPriorities.Add(i);
		}
		foreach (int priority in rankedByMention.Keys.OrderBy((int x) => x))
		{
			if (knownPriorities.Add(priority))
			{
				mentionPriorities.Add(priority);
			}
		}
		int primarySelected = 0;
		int primaryCollisionFallbacks = 0;
		List<string> assignments = new List<string>();
		foreach (int priority in mentionPriorities)
		{
			if (result.Count >= maxCount)
			{
				break;
			}
			if (!rankedByMention.TryGetValue(priority, out var ranked) || ranked == null || ranked.Count == 0)
			{
				continue;
			}
			if (TryAddFirstUniqueGlobalCandidate(result, selectedKeys, ranked, out var selectedRank))
			{
				primarySelected++;
				if (selectedRank > 0)
				{
					primaryCollisionFallbacks++;
				}
				GlobalEntityCandidate selected = result[result.Count - 1];
				string scopeDetail = selected.HeroScopeScore > 0 ? ("#scope=" + selected.HeroScopeScore + ":" + PreviewLogValue(selected.HeroScopeEvidence, 40)) : "";
				string distanceDetail = selected.HeroDistanceBonus > 0f ? ("#distance=" + selected.HeroDistance.ToString("0.0", CultureInfo.InvariantCulture) + ":+" + selected.HeroDistanceBonus.ToString("0.000", CultureInfo.InvariantCulture) + ":final=" + selected.FinalScore.ToString("0.000", CultureInfo.InvariantCulture)) : "";
				assignments.Add((priority + 1) + ":" + PreviewLogValue(selected.Mention, 30) + "->" + selected.Type + ":" + PreviewLogValue(selected.Name, 30) + "@" + (selectedRank + 1) + scopeDetail + distanceDetail);
			}
		}
		bool allowSecondary = maxCount > mentionCount;
		int secondarySelected = 0;
		if (allowSecondary && result.Count < maxCount)
		{
			foreach (int priority in mentionPriorities)
			{
				if (result.Count >= maxCount)
				{
					break;
				}
				if (!rankedByMention.TryGetValue(priority, out var ranked) || ranked == null || ranked.Count == 0)
				{
					continue;
				}
				int addedForMention = 0;
				foreach (GlobalEntityCandidate candidate in ranked)
				{
					if (result.Count >= maxCount || addedForMention >= MaxSecondaryMatchesPerMention)
					{
						break;
					}
					if (candidate == null || string.IsNullOrWhiteSpace(candidate.Key) || !selectedKeys.Add(candidate.Key))
					{
						continue;
					}
					result.Add(candidate);
					addedForMention++;
					secondarySelected++;
				}
			}
		}
		allocationSummary = "[WorldEntityPerf] noun_allocation nouns=" + mentionCount + " maxInject=" + maxCount + " candidates=" + (candidates?.Count ?? 0) + " ambiguousPersonNouns=" + ambiguousPersonNamePriorities.Count + " scopeConstraints=" + scopeConstraints.Count + " scopeBoostedHeroes=" + scopeBoostedHeroes + " distanceBoostedHeroes=" + distanceBoostedHeroes + " primary=" + primarySelected + " collisionFallbacks=" + primaryCollisionFallbacks + " secondary=" + secondarySelected + " allowSecondary=" + allowSecondary + " selected=" + result.Count + " assignments=" + (assignments.Count == 0 ? "(none)" : string.Join("|", assignments));
		return result;
	}

	internal static HashSet<int> FindAmbiguousPersonNamePriorities(IEnumerable<GlobalEntityCandidate> candidates)
	{
		HashSet<int> result = new HashSet<int>();
		foreach (IGrouping<int, GlobalEntityCandidate> group in (candidates ?? Enumerable.Empty<GlobalEntityCandidate>()).Where((GlobalEntityCandidate x) => x != null).GroupBy((GlobalEntityCandidate x) => x.MentionPriority))
		{
			List<GlobalEntityCandidate> heroCandidates = group
				.Where((GlobalEntityCandidate x) => string.Equals(x.Type, "hero", StringComparison.OrdinalIgnoreCase))
				.GroupBy((GlobalEntityCandidate x) => x.Key ?? "", StringComparer.OrdinalIgnoreCase)
				.Select((IGrouping<string, GlobalEntityCandidate> x) => x.OrderByDescending((GlobalEntityCandidate y) => y.Score).First())
				.ToList();
			if (heroCandidates.Count < 2)
			{
				continue;
			}
			float bestHeroScore = heroCandidates.Max((GlobalEntityCandidate x) => x.Score);
			bool hasExactHeroName = heroCandidates.Any((GlobalEntityCandidate x) => x.ExactNameMatch);
			bool hasCompetingExactNonHero = group.Any((GlobalEntityCandidate x) => !string.Equals(x.Type, "hero", StringComparison.OrdinalIgnoreCase) && x.ExactNameMatch && x.Score >= bestHeroScore - 0.0001f);
			float bestNonHeroScore = group.Where((GlobalEntityCandidate x) => !string.Equals(x.Type, "hero", StringComparison.OrdinalIgnoreCase)).Select((GlobalEntityCandidate x) => x.Score).DefaultIfEmpty(0f).Max();
			bool stronglyHeroShaped = bestHeroScore >= 0.9f && bestHeroScore > bestNonHeroScore + 0.05f;
			if (!hasCompetingExactNonHero && (hasExactHeroName || stronglyHeroShaped))
			{
				result.Add(group.Key);
			}
		}
		return result;
	}

	internal static List<EntityScopeConstraint> BuildEntityScopeConstraints(IEnumerable<GlobalEntityCandidate> candidates, HashSet<int> personNamePriorities)
	{
		List<EntityScopeConstraint> result = new List<EntityScopeConstraint>();
		foreach (IGrouping<int, GlobalEntityCandidate> group in (candidates ?? Enumerable.Empty<GlobalEntityCandidate>())
			.Where((GlobalEntityCandidate x) => x != null && (personNamePriorities == null || !personNamePriorities.Contains(x.MentionPriority)))
			.GroupBy((GlobalEntityCandidate x) => x.MentionPriority))
		{
			GlobalEntityCandidate selected = group
				.Where((GlobalEntityCandidate x) => (string.Equals(x.Type, "clan", StringComparison.OrdinalIgnoreCase) || string.Equals(x.Type, "kingdom", StringComparison.OrdinalIgnoreCase)) && (x.ExactNameMatch || x.Score >= 0.999f))
				.OrderByDescending((GlobalEntityCandidate x) => x.Score)
				.ThenByDescending((GlobalEntityCandidate x) => x.ExactNameMatch)
				.ThenBy((GlobalEntityCandidate x) => x.TypePriority)
				.ThenBy((GlobalEntityCandidate x) => x.Name ?? "", StringComparer.OrdinalIgnoreCase)
				.FirstOrDefault();
			if (selected == null)
			{
				continue;
			}
			if (string.Equals(selected.Type, "clan", StringComparison.OrdinalIgnoreCase))
			{
				if (!string.IsNullOrWhiteSpace(selected.ScopeClanId))
				{
					result.Add(new EntityScopeConstraint { ClanId = selected.ScopeClanId, MentionPriority = selected.MentionPriority, Name = selected.Name ?? "" });
				}
				continue;
			}
			if (!string.IsNullOrWhiteSpace(selected.ScopeKingdomId))
			{
				result.Add(new EntityScopeConstraint { KingdomId = selected.ScopeKingdomId, MentionPriority = selected.MentionPriority, Name = selected.Name ?? "" });
			}
		}
		return result;
	}

	internal static int ApplyHeroScopePriorities(IEnumerable<GlobalEntityCandidate> candidates, HashSet<int> personNamePriorities, IEnumerable<EntityScopeConstraint> scopeConstraints)
	{
		if (personNamePriorities == null || personNamePriorities.Count == 0)
		{
			return 0;
		}
		List<EntityScopeConstraint> scopes = (scopeConstraints ?? Enumerable.Empty<EntityScopeConstraint>()).Where((EntityScopeConstraint x) => x != null).ToList();
		if (scopes.Count == 0)
		{
			return 0;
		}
		int boosted = 0;
		foreach (GlobalEntityCandidate candidate in candidates ?? Enumerable.Empty<GlobalEntityCandidate>())
		{
			if (candidate == null || !personNamePriorities.Contains(candidate.MentionPriority) || !string.Equals(candidate.Type, "hero", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			int score = 0;
			List<string> evidence = new List<string>();
			foreach (EntityScopeConstraint scope in scopes)
			{
				if (scope.MentionPriority == candidate.MentionPriority)
				{
					continue;
				}
				int priorityBonus = Math.Max(0, 100 - Math.Min(100, Math.Max(0, scope.MentionPriority)));
				if (!string.IsNullOrWhiteSpace(scope.ClanId) && string.Equals(candidate.HeroClanId, scope.ClanId, StringComparison.OrdinalIgnoreCase))
				{
					score += 10000 + priorityBonus;
					evidence.Add("家族:" + scope.Name);
					continue;
				}
				if (!string.IsNullOrWhiteSpace(scope.KingdomId) && string.Equals(candidate.HeroKingdomId, scope.KingdomId, StringComparison.OrdinalIgnoreCase))
				{
					score += 1000 + priorityBonus;
					evidence.Add("王国:" + scope.Name);
				}
			}
			if (score <= 0)
			{
				continue;
			}
			candidate.HeroScopeScore = score;
			candidate.HeroScopeEvidence = string.Join("+", evidence.Distinct(StringComparer.OrdinalIgnoreCase));
			boosted++;
		}
		return boosted;
	}

	internal static int ApplyHeroDistanceBonuses(IEnumerable<GlobalEntityCandidate> candidates, HashSet<int> personNamePriorities)
	{
		int boosted = 0;
		foreach (GlobalEntityCandidate candidate in candidates ?? Enumerable.Empty<GlobalEntityCandidate>())
		{
			if (candidate == null)
			{
				continue;
			}
			candidate.FinalScore = candidate.Score;
			if (personNamePriorities == null || !personNamePriorities.Contains(candidate.MentionPriority) || !string.Equals(candidate.Type, "hero", StringComparison.OrdinalIgnoreCase) || candidate.HeroDistanceBonus <= 0f)
			{
				candidate.HeroDistanceBonus = 0f;
				continue;
			}
			candidate.HeroDistanceBonus = Math.Min(MaxHeroProximityBonus, candidate.HeroDistanceBonus);
			candidate.FinalScore = candidate.Score + candidate.HeroDistanceBonus;
			boosted++;
		}
		return boosted;
	}

	internal static bool TryAddFirstUniqueGlobalCandidate(List<GlobalEntityCandidate> result, HashSet<string> selectedKeys, List<GlobalEntityCandidate> ranked, out int selectedRank)
	{
		selectedRank = -1;
		if (result == null || selectedKeys == null || ranked == null)
		{
			return false;
		}
		for (int i = 0; i < ranked.Count; i++)
		{
			GlobalEntityCandidate candidate = ranked[i];
			if (candidate == null || string.IsNullOrWhiteSpace(candidate.Key) || !selectedKeys.Add(candidate.Key))
			{
				continue;
			}
			result.Add(candidate);
			selectedRank = i;
			return true;
		}
		return false;
	}
}
