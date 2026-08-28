using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AnimusForge;

public sealed class MentionedWorldEntities
{
	public List<string> Entities = new List<string>();

	public bool IsEmpty
	{
		get
		{
			return IsEmptyList(Entities);
		}
	}

	public MentionedWorldEntities Clone()
	{
		return new MentionedWorldEntities
		{
			Entities = new List<string>(Entities ?? new List<string>())
		};
	}

	public void Merge(MentionedWorldEntities other)
	{
		if (other == null)
		{
			return;
		}
		MergeList(Entities, other.Entities);
	}

	private static bool IsEmptyList(List<string> values)
	{
		return values == null || values.All((string x) => string.IsNullOrWhiteSpace(x));
	}

	private static void MergeList(List<string> target, IEnumerable<string> source)
	{
		if (target == null || source == null)
		{
			return;
		}
		HashSet<string> seen = new HashSet<string>(target.Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim()), StringComparer.OrdinalIgnoreCase);
		foreach (string item in source)
		{
			string text = (item ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && seen.Add(text))
			{
				target.Add(text);
			}
		}
	}
}

public sealed class WorldEntityPromptContext
{
	public string MainPromptBlock = "";

	public string PostprocessPromptBlock = "";

	public List<string> ExplicitMentionedKingdomIds = new List<string>();

	public int MatchCount;

	public bool HasContent
	{
		get
		{
			return !string.IsNullOrWhiteSpace(MainPromptBlock) || !string.IsNullOrWhiteSpace(PostprocessPromptBlock);
		}
	}
}

public static class WorldEntityRetrievalService
{
	private const float MatchThreshold = 0.72f;

	private const float NearTopDelta = 0.07f;

	private const int DefaultMaxInjectedEntities = 6;

	private const int MaxInjectedEntitiesHardCap = 20;

	private const int MaxCandidatesPerMention = MaxInjectedEntitiesHardCap;

	private const int MaxSecondaryMatchesPerMention = 3;

	private const float MaxHeroProximityBonus = 0.15f;

	private const float HeroProximityDecayDistance = 30f;

	private const int MaxVisiblePartyCandidates = 10;

	private const float VisiblePartyMinRange = 18f;

	private const float VisiblePartyRangeMultiplier = 1.5f;

	private const int MainPromptClanMemberCap = 8;

	private const int MainPromptClanFiefCap = 8;

	private const int MainPromptKingdomClanCap = 6;

	private const int MainPromptKingdomEncyclopediaTextCap = 600;

	private const int EntityRetrievalSoftBudgetMs = 1500;

	private const int EntityRetrievalHardBudgetMs = 3000;

	private const int EntityRetrievalProgressLogInterval = 500;

	private const int EntityRetrievalBudgetCheckInterval = 64;

	private sealed class EntityMatch<T>
	{
		public T Value;

		public string Id;

		public string Name;

		public string Mention;

		public float Score;

		public int MentionPriority;

		public string RulerTitleKey;
	}

	private sealed class GlobalEntityCandidate
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

	private sealed class EntityScopeConstraint
	{
		public string ClanId;

		public string KingdomId;

		public int MentionPriority;

		public string Name;
	}

	private sealed class VisiblePartyCandidate
	{
		public MobileParty Party;

		public string Id;

		public string Name;

		public int Count;

		public string Affiliation;

		public string RelationToContextHero;

		public string RelationToPlayer;

		public string ShipInfo;

		public string Direction;

		public float Distance;
	}

	private sealed class WorldEntityRetrievalBudget
	{
		private readonly Stopwatch _stopwatch;
		private bool _softLogged;
		private bool _hardLogged;

		public WorldEntityRetrievalBudget(Stopwatch stopwatch)
		{
			_stopwatch = stopwatch;
		}

		public long ElapsedMs
		{
			get
			{
				return _stopwatch?.ElapsedMilliseconds ?? 0L;
			}
		}

		public bool IsSoftExceeded
		{
			get
			{
				return ElapsedMs >= EntityRetrievalSoftBudgetMs;
			}
		}

		public bool IsHardExceeded
		{
			get
			{
				return ElapsedMs >= EntityRetrievalHardBudgetMs;
			}
		}

		public bool TryMarkSoftExceeded()
		{
			if (!IsSoftExceeded || _softLogged)
			{
				return false;
			}
			_softLogged = true;
			return true;
		}

		public bool TryMarkHardExceeded()
		{
			if (!IsHardExceeded || _hardLogged)
			{
				return false;
			}
			_hardLogged = true;
			return true;
		}
	}

	private sealed class FuzzyTextProfile
	{
		public string Raw;

		public string Normalized;

		public List<string> Tokens;
	}

	private sealed class EntityCandidateSnapshot<T> where T : class
	{
		public T Value;

		public string Id;

		public string Name;

		public List<FuzzyTextProfile> Aliases;
	}

	private sealed class RulerTitleCandidate
	{
		public Kingdom Kingdom;

		public Hero Leader;

		public string KingdomId;

		public string KingdomName;

		public string Title;

		public List<string> Qualifiers;

		public List<string> Aliases;
	}

	private sealed class RulerTitleScoredMatch
	{
		public RulerTitleCandidate Candidate;

		public string MatchedAlias;

		public float Score;

		public bool IsQualified;
	}

	private sealed class RawRulerTitleMatchResult
	{
		public List<EntityMatch<Hero>> Matches = new List<EntityMatch<Hero>>();

		public HashSet<string> OverrideTitleKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	}

	public static WorldEntityPromptContext BuildPromptContext(MentionedWorldEntities mentions, string playerDisplayName, Hero contextHero = null, bool includeResidentKingdoms = false, IEnumerable<string> activeRuleIds = null, bool includeResidentPlayerEntities = false)
	{
		return BuildPromptContext(mentions, playerDisplayName, contextHero, includeResidentKingdoms, activeRuleIds, null, includeResidentPlayerEntities);
	}

	public static WorldEntityPromptContext BuildPromptContext(MentionedWorldEntities mentions, string playerDisplayName, Hero contextHero, bool includeResidentKingdoms, IEnumerable<string> activeRuleIds, string latestInput, bool includeResidentPlayerEntities = false)
	{
		WorldEntityPromptContext result = new WorldEntityPromptContext();
		Stopwatch totalSw = Stopwatch.StartNew();
		using FreezeWatchdog.ScopeToken freezeScope = FreezeWatchdog.Scope("WorldEntityRetrieval.BuildPromptContext");
		WorldEntityRetrievalBudget budget = new WorldEntityRetrievalBudget(totalSw);
		try
		{
			if (Campaign.Current == null)
			{
				return result;
			}
			List<VisiblePartyCandidate> visibleParties = BuildVisiblePartyCandidates(contextHero);
			List<string> allMentions = BuildUnifiedMentionList(mentions);
			HashSet<string> activeRuleIdSet = BuildActiveRuleIdSet(activeRuleIds);
			string rawInput = (latestInput ?? "").Trim();
			bool hasRawInput = !string.IsNullOrWhiteSpace(rawInput);
			string startDetail = "entities=" + allMentions.Count + " rawInputLen=" + rawInput.Length + " visibleParties=" + visibleParties.Count + " contextHero=" + (contextHero?.StringId ?? "");
			FreezeWatchdog.Mark("WorldEntityRetrieval.start", startDetail, immediate: true);
			Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] start entities=" + CountList(mentions?.Entities) + " rawInputLen=" + rawInput.Length + " visibleParties=" + visibleParties.Count + " contextHero=" + (contextHero?.StringId ?? "") + " includeResidentKingdoms=" + includeResidentKingdoms + " includeResidentPlayerEntities=" + includeResidentPlayerEntities + " activeRules=" + FormatMentionsForLog(activeRuleIdSet) + " " + FormatBudgetForLog(budget));
			List<EntityMatch<Hero>> heroes = new List<EntityMatch<Hero>>();
			List<EntityMatch<Settlement>> settlements = new List<EntityMatch<Settlement>>();
			List<EntityMatch<Clan>> clans = new List<EntityMatch<Clan>>();
			List<EntityMatch<Kingdom>> kingdoms = new List<EntityMatch<Kingdom>>();
			if (allMentions.Count > 0 || hasRawInput)
			{
				Stopwatch stageSw = Stopwatch.StartNew();
				int maxInjectedEntities = GetMaxInjectedEntitiesFromSettings();
				List<Hero> heroCandidates = new List<Hero>();
				List<Settlement> settlementCandidates = new List<Settlement>();
				List<Clan> clanCandidates = new List<Clan>();
				List<Kingdom> kingdomCandidates = new List<Kingdom>();
				if (allMentions.Count > 0)
				{
					heroCandidates = GetHeroCandidates().ToList();
					settlementCandidates = GetSettlementCandidates().ToList();
					clanCandidates = GetClanCandidates().ToList();
				}
				if (allMentions.Count > 0 || hasRawInput)
				{
					kingdomCandidates = GetKingdomCandidates().ToList();
				}
				Logger.Log("WorldEntityRetrieval", "entities total=" + allMentions.Count + " maxInject=" + maxInjectedEntities + " rawInputLen=" + rawInput.Length + " visibleParties=" + visibleParties.Count + " candidates hero=" + heroCandidates.Count + " settlement=" + settlementCandidates.Count + " clan=" + clanCandidates.Count + " kingdom=" + kingdomCandidates.Count + " names=" + FormatMentionsForLog(allMentions));
				Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] candidates_ready ms=" + Math.Round(stageSw.Elapsed.TotalMilliseconds, 2));
				Dictionary<string, int> mentionPriority = BuildMentionPriority(allMentions);
				List<EntityMatch<Hero>> rulerTitleMatches = allMentions.Count > 0 ? FindRulerTitleMatches(allMentions, mentionPriority, kingdomCandidates, "preprocess", budget) : new List<EntityMatch<Hero>>();
				if (hasRawInput && CanContinueWorldEntityMatch("ruler_title_raw", budget))
				{
					RawRulerTitleMatchResult rawRulerTitleMatches = FindRawRulerTitleMatches(rawInput, kingdomCandidates, budget);
					if (rawRulerTitleMatches.OverrideTitleKeys.Count > 0)
					{
						rulerTitleMatches = rulerTitleMatches.Where((EntityMatch<Hero> x) => x == null || string.IsNullOrWhiteSpace(x.RulerTitleKey) || !rawRulerTitleMatches.OverrideTitleKeys.Contains(x.RulerTitleKey)).ToList();
					}
					rulerTitleMatches = MergeEntityMatches(rulerTitleMatches, rawRulerTitleMatches.Matches);
				}
				heroes = MergeEntityMatches(heroes, rulerTitleMatches);
				if (allMentions.Count > 0)
				{
					if (CanContinueWorldEntityMatch("hero", budget))
					{
						List<EntityMatch<Hero>> directHeroMatches = FindMatches("hero", allMentions, mentionPriority, heroCandidates, GetHeroAliases, (Hero x) => "hero:" + SafeStringId(x?.StringId), (Hero x) => SafeName(x?.Name, x?.StringId ?? "Hero"), maxInjectedEntities, budget);
						heroes = ConcatEntityMatchCandidates(heroes, directHeroMatches);
					}
					if (CanContinueWorldEntityMatch("settlement", budget))
					{
						settlements = FindMatches("settlement", allMentions, mentionPriority, settlementCandidates, GetSettlementAliases, (Settlement x) => "settlement:" + SafeStringId(x?.StringId), (Settlement x) => SafeName(x?.Name, x?.StringId ?? "Settlement"), maxInjectedEntities, budget);
					}
					if (CanContinueWorldEntityMatch("clan", budget))
					{
						clans = FindMatches("clan", allMentions, mentionPriority, clanCandidates, GetClanAliases, (Clan x) => "clan:" + SafeStringId(x?.StringId), (Clan x) => SafeName(x?.Name, x?.StringId ?? "Clan"), maxInjectedEntities, budget);
					}
					if (CanContinueWorldEntityMatch("kingdom", budget))
					{
						kingdoms = FindMatches("kingdom", allMentions, mentionPriority, kingdomCandidates, GetKingdomAliases, (Kingdom x) => "kingdom:" + SafeStringId(x?.StringId), (Kingdom x) => SafeName(x?.Name, x?.StringId ?? "Kingdom"), maxInjectedEntities, budget);
					}
				}
				Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] all_match_done heroMatches=" + heroes.Count + " settlementMatches=" + settlements.Count + " clanMatches=" + clans.Count + " kingdomMatches=" + kingdoms.Count + " ms=" + Math.Round(stageSw.Elapsed.TotalMilliseconds, 2) + " hardBudgetExceeded=" + budget.IsHardExceeded);
				result.ExplicitMentionedKingdomIds = kingdoms
					.Where(match => match?.Value != null && !string.IsNullOrWhiteSpace(match.Value.StringId))
					.Select(match => match.Value.StringId.Trim())
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.ToList();
				stageSw.Restart();
				ApplyGlobalInjectionLimit(maxInjectedEntities, allMentions.Count, contextHero, ref heroes, ref settlements, ref clans, ref kingdoms);
				Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] global_limit_done heroMatches=" + heroes.Count + " settlementMatches=" + settlements.Count + " clanMatches=" + clans.Count + " kingdomMatches=" + kingdoms.Count + " ms=" + Math.Round(stageSw.Elapsed.TotalMilliseconds, 2));
			}
			else if (visibleParties.Count > 0)
			{
				Logger.Log("WorldEntityRetrieval", "visible_party_context_only count=" + visibleParties.Count);
			}
			Stopwatch residentSw = Stopwatch.StartNew();
			List<EntityMatch<Hero>> postprocessHeroes = CloneEntityMatches(heroes);
			List<EntityMatch<Settlement>> postprocessSettlements = CloneEntityMatches(settlements);
			List<EntityMatch<Clan>> postprocessClans = CloneEntityMatches(clans);
			List<EntityMatch<Kingdom>> postprocessKingdoms = CloneEntityMatches(kingdoms);
			AddResidentEntityMatches(contextHero, includeResidentKingdoms, includeResidentPlayerEntities, ref heroes, ref settlements, ref clans, ref kingdoms);
			AddPostprocessResidentEntityMatches(contextHero, includeResidentPlayerEntities, ref postprocessHeroes, ref postprocessSettlements, ref postprocessClans, ref postprocessKingdoms);
			Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] resident_done heroMatches=" + heroes.Count + " settlementMatches=" + settlements.Count + " clanMatches=" + clans.Count + " kingdomMatches=" + kingdoms.Count + " visibleParties=" + visibleParties.Count + " ms=" + Math.Round(residentSw.Elapsed.TotalMilliseconds, 2));
			int count = heroes.Count + settlements.Count + clans.Count + kingdoms.Count + visibleParties.Count;
			if (count <= 0)
			{
				Logger.Log("WorldEntityRetrieval", "no_match mentions=" + FormatMentionsForLog(allMentions) + " rawInputLen=" + rawInput.Length);
				return result;
			}
			result.MatchCount = count;
			Stopwatch buildSw = Stopwatch.StartNew();
			Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] build_blocks_start matchCount=" + count);
			FreezeWatchdog.Mark("WorldEntityRetrieval.build_blocks_start", "matchCount=" + count, immediate: true);
			result.MainPromptBlock = BuildMainPromptBlock(playerDisplayName, contextHero, heroes, settlements, clans, kingdoms, visibleParties);
			result.PostprocessPromptBlock = BuildPostprocessPromptBlock(postprocessHeroes, postprocessSettlements, postprocessClans, postprocessKingdoms, visibleParties);
			Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] build_blocks_done mainLen=" + ((result.MainPromptBlock ?? "").Length) + " postLen=" + ((result.PostprocessPromptBlock ?? "").Length) + " blockMs=" + Math.Round(buildSw.Elapsed.TotalMilliseconds, 2) + " totalMs=" + Math.Round(totalSw.Elapsed.TotalMilliseconds, 2));
			FreezeWatchdog.Mark("WorldEntityRetrieval.done", "matches=" + result.MatchCount + " totalMs=" + Math.Round(totalSw.Elapsed.TotalMilliseconds, 2) + " hardBudgetExceeded=" + budget.IsHardExceeded, immediate: true);
			return result;
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("WorldEntityRetrieval", "build_prompt_context failed afterMs=" + Math.Round(totalSw.Elapsed.TotalMilliseconds, 2) + ": " + ex.Message);
			}
			catch
			{
			}
			return result;
		}
	}

	private static List<string> BuildUnifiedMentionList(MentionedWorldEntities mentions)
	{
		List<string> result = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		AddMentionList(result, seen, mentions?.Entities);
		return result;
	}

	private static HashSet<string> BuildActiveRuleIdSet(IEnumerable<string> activeRuleIds)
	{
		HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string ruleId in activeRuleIds ?? Enumerable.Empty<string>())
		{
			string text = (ruleId ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text))
			{
				result.Add(text);
			}
		}
		return result;
	}


	private static void AddMentionList(List<string> result, HashSet<string> seen, IEnumerable<string> values)
	{
		if (result == null || seen == null || values == null)
		{
			return;
		}
		foreach (string value in values)
		{
			string text = (value ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (text.IndexOf('的') < 0 && text.IndexOf('之') < 0)
			{
				AddMention(result, seen, text);
				continue;
			}
			int segmentStart = 0;
			for (int i = 0; i <= text.Length; i++)
			{
				if (i < text.Length && text[i] != '的' && text[i] != '之')
				{
					continue;
				}
				AddMention(result, seen, text.Substring(segmentStart, i - segmentStart));
				segmentStart = i + 1;
			}
		}
	}

	private static void AddMention(List<string> result, HashSet<string> seen, string value)
	{
		string text = (value ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text) && seen.Add(text))
		{
			result.Add(text);
		}
	}

	private static Dictionary<string, int> BuildMentionPriority(List<string> mentions)
	{
		Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		if (mentions == null)
		{
			return result;
		}
		for (int i = 0; i < mentions.Count; i++)
		{
			string text = (mentions[i] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && !result.ContainsKey(text))
			{
				result[text] = i;
			}
		}
		return result;
	}

	private static int CountList(List<string> values)
	{
		return values?.Count((string x) => !string.IsNullOrWhiteSpace(x)) ?? 0;
	}

	private static string FormatMentionsForLog(IEnumerable<string> values)
	{
		List<string> names = (values ?? Enumerable.Empty<string>()).Select((string x) => (x ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Take(12).ToList();
		return names.Count == 0 ? "(none)" : string.Join("|", names);
	}

	private static int GetMaxInjectedEntitiesFromSettings()
	{
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings != null)
			{
				return ClampMaxInjectedEntities(settings.WorldEntityInjectMaxCount);
			}
		}
		catch
		{
		}
		return DefaultMaxInjectedEntities;
	}

	private static int ClampMaxInjectedEntities(int value)
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

	private static void ApplyGlobalInjectionLimit(int maxCount, int mentionCount, Hero contextHero, ref List<EntityMatch<Hero>> heroes, ref List<EntityMatch<Settlement>> settlements, ref List<EntityMatch<Clan>> clans, ref List<EntityMatch<Kingdom>> kingdoms)
	{
		maxCount = ClampMaxInjectedEntities(maxCount);
		mentionCount = Math.Max(0, mentionCount);
		CampaignVec2 contextPosition = CampaignVec2.Invalid;
		bool hasContextPosition = TryResolveHeroCampaignPosition(contextHero, out contextPosition);
		List<GlobalEntityCandidate> candidates = new List<GlobalEntityCandidate>();
		AddGlobalLimitItems(candidates, "hero", 0, heroes, hasContextPosition ? contextPosition : (CampaignVec2?)null);
		AddGlobalLimitItems(candidates, "settlement", 1, settlements);
		AddGlobalLimitItems(candidates, "clan", 2, clans);
		AddGlobalLimitItems(candidates, "kingdom", 3, kingdoms);
		List<GlobalEntityCandidate> selected = SelectGlobalInjectionCandidates(candidates, maxCount, mentionCount, out var allocationSummary);
		heroes = ExtractGlobalLimitMatches<Hero>(selected, "hero");
		settlements = ExtractGlobalLimitMatches<Settlement>(selected, "settlement");
		clans = ExtractGlobalLimitMatches<Clan>(selected, "clan");
		kingdoms = ExtractGlobalLimitMatches<Kingdom>(selected, "kingdom");
		Logger.Log("WorldEntityRetrieval", allocationSummary);
	}

	private static List<GlobalEntityCandidate> SelectGlobalInjectionCandidates(List<GlobalEntityCandidate> candidates, int maxCount, int mentionCount, out string allocationSummary)
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
				string scopeDetail = selected.HeroScopeScore > 0 ? ("#scope=" + selected.HeroScopeScore + ":" + PreviewWorldEntityLogValue(selected.HeroScopeEvidence, 40)) : "";
				string distanceDetail = selected.HeroDistanceBonus > 0f ? ("#distance=" + selected.HeroDistance.ToString("0.0", CultureInfo.InvariantCulture) + ":+" + selected.HeroDistanceBonus.ToString("0.000", CultureInfo.InvariantCulture) + ":final=" + selected.FinalScore.ToString("0.000", CultureInfo.InvariantCulture)) : "";
				assignments.Add((priority + 1) + ":" + PreviewWorldEntityLogValue(selected.Mention, 30) + "->" + selected.Type + ":" + PreviewWorldEntityLogValue(selected.Name, 30) + "@" + (selectedRank + 1) + scopeDetail + distanceDetail);
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

	private static HashSet<int> FindAmbiguousPersonNamePriorities(IEnumerable<GlobalEntityCandidate> candidates)
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

	private static List<EntityScopeConstraint> BuildEntityScopeConstraints(IEnumerable<GlobalEntityCandidate> candidates, HashSet<int> personNamePriorities)
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
					result.Add(new EntityScopeConstraint
					{
						ClanId = selected.ScopeClanId,
						MentionPriority = selected.MentionPriority,
						Name = selected.Name ?? ""
					});
				}
				continue;
			}
			if (!string.IsNullOrWhiteSpace(selected.ScopeKingdomId))
			{
				result.Add(new EntityScopeConstraint
				{
					KingdomId = selected.ScopeKingdomId,
					MentionPriority = selected.MentionPriority,
					Name = selected.Name ?? ""
				});
			}
		}
		return result;
	}

	private static int ApplyHeroScopePriorities(IEnumerable<GlobalEntityCandidate> candidates, HashSet<int> personNamePriorities, IEnumerable<EntityScopeConstraint> scopeConstraints)
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

	private static int ApplyHeroDistanceBonuses(IEnumerable<GlobalEntityCandidate> candidates, HashSet<int> personNamePriorities)
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

	private static bool TryAddFirstUniqueGlobalCandidate(List<GlobalEntityCandidate> result, HashSet<string> selectedKeys, List<GlobalEntityCandidate> ranked, out int selectedRank)
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

	private static void AddGlobalLimitItems<T>(List<GlobalEntityCandidate> target, string type, int typePriority, IEnumerable<EntityMatch<T>> matches, CampaignVec2? contextPosition = null) where T : class
	{
		if (target == null || matches == null)
		{
			return;
		}
		foreach (EntityMatch<T> match in matches)
		{
			if (match == null || match.Value == null)
			{
				continue;
			}
			string id = string.IsNullOrWhiteSpace(match.Id) ? match.Name : match.Id;
			if (string.IsNullOrWhiteSpace(id))
			{
				continue;
			}
			GlobalEntityCandidate candidate = new GlobalEntityCandidate
			{
				Type = type ?? "",
				Key = (type ?? "") + ":" + id,
				Name = match.Name ?? "",
				Mention = match.Mention ?? "",
				MentionPriority = match.MentionPriority,
				TypePriority = typePriority,
				Score = match.Score,
				FinalScore = match.Score,
				ExactNameMatch = IsExactEntityNameMatch(match.Mention, match.Name),
				Match = match
			};
			PopulateGlobalEntityScopeIds(candidate, match.Value);
			PopulateGlobalEntityDistanceMetadata(candidate, match.Value as Hero, contextPosition);
			target.Add(candidate);
		}
	}

	private static void PopulateGlobalEntityDistanceMetadata(GlobalEntityCandidate candidate, Hero hero, CampaignVec2? contextPosition)
	{
		if (candidate == null || hero == null || !contextPosition.HasValue || !contextPosition.Value.IsValid() || !TryResolveHeroCampaignPosition(hero, out var heroPosition))
		{
			return;
		}
		try
		{
			float distance = heroPosition.Distance(contextPosition.Value);
			if (float.IsNaN(distance) || float.IsInfinity(distance) || distance < 0f || distance >= float.MaxValue * 0.5f)
			{
				return;
			}
			candidate.HeroDistance = distance;
			candidate.HeroDistanceBonus = MaxHeroProximityBonus * (float)Math.Exp(0f - distance / HeroProximityDecayDistance);
		}
		catch
		{
		}
	}

	private static bool TryResolveHeroCampaignPosition(Hero hero, out CampaignVec2 position)
	{
		position = CampaignVec2.Invalid;
		try
		{
			if (hero == null || !hero.IsAlive)
			{
				return false;
			}
			Settlement settlement = hero.CurrentSettlement ?? hero.StayingInSettlement;
			if (settlement != null && settlement.GatePosition.IsValid())
			{
				position = settlement.GatePosition;
				return true;
			}
			MobileParty party = hero.PartyBelongedTo;
			if (party != null && party.Position.IsValid())
			{
				position = party.Position;
				return true;
			}
			PartyBase prisonerParty = hero.PartyBelongedToAsPrisoner;
			if (prisonerParty != null && prisonerParty.Position.IsValid())
			{
				position = prisonerParty.Position;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static void PopulateGlobalEntityScopeIds(GlobalEntityCandidate candidate, object value)
	{
		if (candidate == null || value == null)
		{
			return;
		}
		try
		{
			if (value is Hero hero)
			{
				Clan clan = hero.Clan;
				candidate.HeroClanId = NormalizeScopeEntityId(clan?.StringId);
				candidate.HeroKingdomId = NormalizeScopeEntityId(ResolveHeroKingdomForResidentEntity(hero, clan)?.StringId);
				return;
			}
			if (value is Clan clanScope)
			{
				candidate.ScopeClanId = NormalizeScopeEntityId(clanScope.StringId);
				return;
			}
			if (value is Kingdom kingdomScope)
			{
				candidate.ScopeKingdomId = NormalizeScopeEntityId(kingdomScope.StringId);
			}
		}
		catch
		{
			// Scope metadata is only a ranking aid; keep the original score as fallback.
		}
	}

	private static string NormalizeScopeEntityId(string stringId)
	{
		return (stringId ?? "").Trim();
	}

	private static List<EntityMatch<T>> ExtractGlobalLimitMatches<T>(IEnumerable<GlobalEntityCandidate> selected, string type) where T : class
	{
		List<EntityMatch<T>> result = new List<EntityMatch<T>>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (GlobalEntityCandidate candidate in selected ?? Enumerable.Empty<GlobalEntityCandidate>())
		{
			if (candidate == null || !string.Equals(candidate.Type, type, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(candidate.Key) || !seen.Add(candidate.Key))
			{
				continue;
			}
			EntityMatch<T> match = candidate.Match as EntityMatch<T>;
			if (match != null && match.Value != null)
			{
				if (string.Equals(candidate.Type, "hero", StringComparison.OrdinalIgnoreCase) && candidate.FinalScore > match.Score)
				{
					match.Score = candidate.FinalScore;
				}
				result.Add(match);
			}
		}
		SortEntityMatches(result);
		return result;
	}

	private static bool IsExactEntityNameMatch(string mention, string name)
	{
		string normalizedMention = NormalizeFuzzyText(mention);
		return !string.IsNullOrWhiteSpace(normalizedMention) && string.Equals(normalizedMention, NormalizeFuzzyText(name), StringComparison.OrdinalIgnoreCase);
	}

	private static List<EntityMatch<T>> ConcatEntityMatchCandidates<T>(IEnumerable<EntityMatch<T>> existingMatches, IEnumerable<EntityMatch<T>> additionalMatches) where T : class
	{
		List<EntityMatch<T>> result = new List<EntityMatch<T>>();
		result.AddRange((existingMatches ?? Enumerable.Empty<EntityMatch<T>>()).Where((EntityMatch<T> x) => x != null && x.Value != null));
		result.AddRange((additionalMatches ?? Enumerable.Empty<EntityMatch<T>>()).Where((EntityMatch<T> x) => x != null && x.Value != null));
		SortEntityMatches(result);
		return result;
	}

	private static List<EntityMatch<T>> MergeEntityMatches<T>(IEnumerable<EntityMatch<T>> existingMatches, IEnumerable<EntityMatch<T>> additionalMatches) where T : class
	{
		List<EntityMatch<T>> result = new List<EntityMatch<T>>();
		foreach (EntityMatch<T> match in existingMatches ?? Enumerable.Empty<EntityMatch<T>>())
		{
			AddOrUpdateEntityMatch(result, match);
		}
		foreach (EntityMatch<T> match in additionalMatches ?? Enumerable.Empty<EntityMatch<T>>())
		{
			AddOrUpdateEntityMatch(result, match);
		}
		SortEntityMatches(result);
		return result;
	}

	private static void AddOrUpdateEntityMatch<T>(List<EntityMatch<T>> matches, EntityMatch<T> incoming) where T : class
	{
		if (matches == null || incoming == null || incoming.Value == null)
		{
			return;
		}
		string key = string.IsNullOrWhiteSpace(incoming.Id) ? (incoming.Name ?? "").Trim() : incoming.Id.Trim();
		if (string.IsNullOrWhiteSpace(key))
		{
			return;
		}
		EntityMatch<T> existing = matches.FirstOrDefault((EntityMatch<T> x) => x != null && string.Equals(string.IsNullOrWhiteSpace(x.Id) ? (x.Name ?? "").Trim() : x.Id.Trim(), key, StringComparison.OrdinalIgnoreCase));
		if (existing == null)
		{
			matches.Add(new EntityMatch<T>
			{
				Value = incoming.Value,
				Id = incoming.Id ?? "",
				Name = incoming.Name ?? "",
				Mention = incoming.Mention ?? "",
				Score = incoming.Score,
				MentionPriority = incoming.MentionPriority,
				RulerTitleKey = incoming.RulerTitleKey ?? ""
			});
			return;
		}
		string mergedMention = MergeEntityMention(existing.Mention, incoming.Mention);
		if (incoming.MentionPriority < existing.MentionPriority || (incoming.MentionPriority == existing.MentionPriority && incoming.Score > existing.Score))
		{
			existing.Value = incoming.Value;
			existing.Id = incoming.Id ?? existing.Id;
			existing.Name = string.IsNullOrWhiteSpace(incoming.Name) ? existing.Name : incoming.Name;
			existing.Score = incoming.Score;
			existing.MentionPriority = incoming.MentionPriority;
			existing.RulerTitleKey = incoming.RulerTitleKey ?? "";
		}
		else if (string.IsNullOrWhiteSpace(existing.RulerTitleKey) && !string.IsNullOrWhiteSpace(incoming.RulerTitleKey))
		{
			existing.RulerTitleKey = incoming.RulerTitleKey;
		}
		existing.Mention = mergedMention;
	}

	private static List<EntityMatch<Hero>> FindRulerTitleMatches(IEnumerable<string> mentions, Dictionary<string, int> mentionPriority, IEnumerable<Kingdom> kingdoms, string source, WorldEntityRetrievalBudget budget)
	{
		using FreezeWatchdog.ScopeToken freezeScope = FreezeWatchdog.Scope("WorldEntityRetrieval.FindRulerTitleMatches");
		Stopwatch sw = Stopwatch.StartNew();
		List<RulerTitleCandidate> candidates = BuildRulerTitleCandidates(kingdoms);
		List<EntityMatch<Hero>> result = new List<EntityMatch<Hero>>();
		List<string> mentionList = (mentions ?? Enumerable.Empty<string>()).Select((string x) => (x ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)).ToList();
		Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] ruler_title_start source=" + (source ?? "") + " mentions=" + mentionList.Count + " candidates=" + candidates.Count + " " + FormatBudgetForLog(budget));
		foreach (string mention in mentionList)
		{
			if (IsHardBudgetExceeded(budget))
			{
				LogWorldEntityBudgetStop("ruler_title", "hero", mention, 0, candidates.Count, result.Count, budget);
				break;
			}
			string normalizedMention = NormalizeFuzzyText(mention);
			int longestContainedTitleLength = GetLongestContainedRulerTitleLength(normalizedMention, candidates);
			List<RulerTitleScoredMatch> scored = new List<RulerTitleScoredMatch>();
			foreach (RulerTitleCandidate candidate in candidates)
			{
				string normalizedTitle = NormalizeFuzzyText(candidate?.Title);
				bool directlyContainsTitle = RawTextContainsEntityPhrase(mention, candidate?.Title);
				if (directlyContainsTitle && IsRulerTitleShadowedByLongerContainedTitle(normalizedMention, normalizedTitle, longestContainedTitleLength, candidates))
				{
					continue;
				}
				float evidenceScore = CalculateRulerTitleEvidenceScore(mention, candidate);
				if (evidenceScore < MatchThreshold)
				{
					continue;
				}
				float aliasScore = CalculateBestRulerTitleAliasScore(mention, candidate, out var matchedAlias);
				float score = directlyContainsTitle ? 1f : Math.Max(evidenceScore, aliasScore);
				bool isQualified = MentionContainsRulerTitleQualifier(mention, candidate);
				if (isQualified)
				{
					score = 1f;
				}
				if (score >= MatchThreshold)
				{
					scored.Add(new RulerTitleScoredMatch
					{
						Candidate = candidate,
						MatchedAlias = matchedAlias,
						Score = score,
						IsQualified = isQualified
					});
				}
			}
			if (scored.Count == 0)
			{
				continue;
			}
			bool hasQualifiedMatches = scored.Any((RulerTitleScoredMatch x) => x.IsQualified);
			IEnumerable<RulerTitleScoredMatch> eligiblePool = hasQualifiedMatches ? scored.Where((RulerTitleScoredMatch x) => x.IsQualified) : scored;
			float best = eligiblePool.Max((RulerTitleScoredMatch x) => x.Score);
			float cutoff = Math.Max(MatchThreshold, best - NearTopDelta);
			List<RulerTitleScoredMatch> selected = eligiblePool.Where((RulerTitleScoredMatch x) => x.Score >= cutoff).OrderByDescending((RulerTitleScoredMatch x) => x.Score).ThenBy((RulerTitleScoredMatch x) => x.Candidate?.KingdomName ?? "", StringComparer.OrdinalIgnoreCase).ToList();
			int ambiguityCount = selected.Count;
			foreach (RulerTitleScoredMatch selectedMatch in selected)
			{
				RulerTitleCandidate candidate = selectedMatch.Candidate;
				Hero leader = candidate?.Leader;
				if (leader == null)
				{
					continue;
				}
				AddOrUpdateEntityMatch(result, new EntityMatch<Hero>
				{
					Value = leader,
					Id = "hero:" + SafeStringId(leader.StringId),
					Name = SafeName(leader.Name, leader.StringId ?? "Hero"),
					Mention = mention,
					Score = selectedMatch.Score,
					MentionPriority = GetMentionPriority(mentionPriority, mention),
					RulerTitleKey = NormalizeFuzzyText(candidate.Title)
				});
				Logger.Log("WorldEntityRetrieval", "ruler_title_match source=" + (source ?? "") + " mention=" + PreviewWorldEntityLogValue(mention, 100) + " title=" + PreviewWorldEntityLogValue(candidate.Title, 60) + " matchedAlias=" + PreviewWorldEntityLogValue(selectedMatch.MatchedAlias, 100) + " kingdom=" + candidate.KingdomId + " hero=" + (leader.StringId ?? "") + " score=" + selectedMatch.Score.ToString("0.###", CultureInfo.InvariantCulture) + " ambiguity=" + ambiguityCount);
			}
		}
		SortEntityMatches(result);
		Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] ruler_title_done source=" + (source ?? "") + " result=" + result.Count + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
		return result;
	}

	private static RawRulerTitleMatchResult FindRawRulerTitleMatches(string rawInput, IEnumerable<Kingdom> kingdoms, WorldEntityRetrievalBudget budget)
	{
		using FreezeWatchdog.ScopeToken freezeScope = FreezeWatchdog.Scope("WorldEntityRetrieval.FindRawRulerTitleMatches");
		Stopwatch sw = Stopwatch.StartNew();
		RawRulerTitleMatchResult result = new RawRulerTitleMatchResult();
		string input = (rawInput ?? "").Trim();
		if (string.IsNullOrWhiteSpace(input))
		{
			return result;
		}
		List<RulerTitleCandidate> candidates = BuildRulerTitleCandidates(kingdoms);
		Dictionary<string, List<RulerTitleCandidate>> titleGroups = candidates.Where((RulerTitleCandidate x) => x != null && !string.IsNullOrWhiteSpace(x.Title)).GroupBy((RulerTitleCandidate x) => NormalizeFuzzyText(x.Title), StringComparer.OrdinalIgnoreCase).Where((IGrouping<string, RulerTitleCandidate> x) => !string.IsNullOrWhiteSpace(x.Key)).ToDictionary((IGrouping<string, RulerTitleCandidate> x) => x.Key, (IGrouping<string, RulerTitleCandidate> x) => x.ToList(), StringComparer.OrdinalIgnoreCase);
		HashSet<string> matchedTitleKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (KeyValuePair<string, List<RulerTitleCandidate>> pair in titleGroups)
		{
			string title = pair.Value.FirstOrDefault()?.Title ?? "";
			if (RawTextContainsEntityPhrase(input, title))
			{
				matchedTitleKeys.Add(pair.Key);
			}
		}
		Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] ruler_title_raw_start inputLen=" + input.Length + " titleGroups=" + titleGroups.Count + " matchedGroups=" + matchedTitleKeys.Count + " " + FormatBudgetForLog(budget));
		foreach (string titleKey in matchedTitleKeys.OrderByDescending((string x) => x.Length).ThenBy((string x) => x, StringComparer.OrdinalIgnoreCase))
		{
			if (IsHardBudgetExceeded(budget))
			{
				LogWorldEntityBudgetStop("ruler_title_raw", "hero", "", 0, matchedTitleKeys.Count, result.Matches.Count, budget);
				break;
			}
			if (IsRawRulerTitleShadowed(input, titleKey, matchedTitleKeys, titleGroups))
			{
				result.OverrideTitleKeys.Add(titleKey);
				continue;
			}
			List<RulerTitleCandidate> group = titleGroups[titleKey];
			Dictionary<RulerTitleCandidate, string> qualifiedAliases = new Dictionary<RulerTitleCandidate, string>();
			foreach (RulerTitleCandidate candidate in group)
			{
				string qualifiedAlias = FindBestRawQualifiedRulerTitleAlias(input, candidate);
				if (!string.IsNullOrWhiteSpace(qualifiedAlias))
				{
					qualifiedAliases[candidate] = qualifiedAlias;
				}
			}
			bool hasQualifiedCandidates = qualifiedAliases.Count > 0;
			if (hasQualifiedCandidates)
			{
				result.OverrideTitleKeys.Add(titleKey);
			}
			IEnumerable<RulerTitleCandidate> selectedSource = hasQualifiedCandidates ? (IEnumerable<RulerTitleCandidate>)qualifiedAliases.Keys : group;
			List<RulerTitleCandidate> selected = selectedSource.OrderBy((RulerTitleCandidate x) => x.KingdomName ?? "", StringComparer.OrdinalIgnoreCase).ThenBy((RulerTitleCandidate x) => x.KingdomId ?? "", StringComparer.OrdinalIgnoreCase).ToList();
			int ambiguityCount = selected.Count;
			foreach (RulerTitleCandidate candidate in selected)
			{
				Hero leader = candidate?.Leader;
				if (leader == null)
				{
					continue;
				}
				string matchedAlias = hasQualifiedCandidates && qualifiedAliases.TryGetValue(candidate, out var alias) ? alias : candidate.Title;
				AddOrUpdateEntityMatch(result.Matches, new EntityMatch<Hero>
				{
					Value = leader,
					Id = "hero:" + SafeStringId(leader.StringId),
					Name = SafeName(leader.Name, leader.StringId ?? "Hero"),
					Mention = matchedAlias,
					Score = 1f,
					MentionPriority = 0,
					RulerTitleKey = titleKey
				});
				Logger.Log("WorldEntityRetrieval", "ruler_title_match source=raw_input mention=" + PreviewWorldEntityLogValue(matchedAlias, 100) + " title=" + PreviewWorldEntityLogValue(candidate.Title, 60) + " matchedAlias=" + PreviewWorldEntityLogValue(matchedAlias, 100) + " kingdom=" + candidate.KingdomId + " hero=" + (leader.StringId ?? "") + " score=1 ambiguity=" + ambiguityCount);
			}
		}
		SortEntityMatches(result.Matches);
		Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] ruler_title_raw_done result=" + result.Matches.Count + " overrides=" + result.OverrideTitleKeys.Count + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
		return result;
	}

	private static string FindBestRawQualifiedRulerTitleAlias(string rawInput, RulerTitleCandidate candidate)
	{
		if (candidate == null)
		{
			return "";
		}
		string titleKey = NormalizeFuzzyText(candidate.Title);
		string bestAlias = "";
		int bestLength = 0;
		foreach (string alias in candidate.Aliases ?? new List<string>())
		{
			string aliasKey = NormalizeFuzzyText(alias);
			if (string.IsNullOrWhiteSpace(aliasKey) || string.Equals(aliasKey, titleKey, StringComparison.OrdinalIgnoreCase) || !RawTextContainsEntityPhrase(rawInput, alias))
			{
				continue;
			}
			if (aliasKey.Length > bestLength)
			{
				bestLength = aliasKey.Length;
				bestAlias = alias;
			}
		}
		return bestAlias;
	}

	private static bool IsRawRulerTitleShadowed(string rawInput, string titleKey, IEnumerable<string> matchedTitleKeys, Dictionary<string, List<RulerTitleCandidate>> titleGroups)
	{
		if (string.IsNullOrWhiteSpace(titleKey) || titleGroups == null || !titleGroups.TryGetValue(titleKey, out var shortGroup))
		{
			return false;
		}
		List<string> longerKeys = (matchedTitleKeys ?? Enumerable.Empty<string>()).Where((string other) => !string.IsNullOrWhiteSpace(other) && other.Length > titleKey.Length && other.IndexOf(titleKey, StringComparison.OrdinalIgnoreCase) >= 0 && titleGroups.ContainsKey(other)).ToList();
		if (longerKeys.Count == 0)
		{
			return false;
		}
		string shortTitle = shortGroup.FirstOrDefault()?.Title ?? "";
		List<string> longerTitles = longerKeys.Select((string key) => titleGroups[key].FirstOrDefault()?.Title ?? "").Where((string x) => !string.IsNullOrWhiteSpace(x)).ToList();
		if (shortTitle.Any(IsCjk) || longerTitles.Any((string x) => x.Any(IsCjk)))
		{
			return true;
		}
		List<Tuple<int, int>> shortSpans = FindRawEntityPhraseSpans(rawInput, shortTitle);
		if (shortSpans.Count == 0)
		{
			return false;
		}
		List<Tuple<int, int>> longerSpans = new List<Tuple<int, int>>();
		foreach (string longerTitle in longerTitles)
		{
			longerSpans.AddRange(FindRawEntityPhraseSpans(rawInput, longerTitle));
		}
		return longerSpans.Count > 0 && shortSpans.All((Tuple<int, int> shortSpan) => longerSpans.Any((Tuple<int, int> longerSpan) => shortSpan.Item1 >= longerSpan.Item1 && shortSpan.Item1 + shortSpan.Item2 <= longerSpan.Item1 + longerSpan.Item2));
	}

	private static bool RawTextContainsEntityPhrase(string rawInput, string phrase)
	{
		string input = rawInput ?? "";
		string value = (phrase ?? "").Trim();
		if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(value))
		{
			return false;
		}
		if (value.Any(IsCjk))
		{
			string normalizedInput = NormalizeFuzzyText(input);
			string normalizedValue = NormalizeFuzzyText(value);
			return normalizedValue.Length >= 2 && normalizedInput.IndexOf(normalizedValue, StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return FindRawEntityPhraseSpans(input, value).Count > 0;
	}

	private static List<Tuple<int, int>> FindRawEntityPhraseSpans(string rawInput, string phrase)
	{
		List<Tuple<int, int>> result = new List<Tuple<int, int>>();
		string input = rawInput ?? "";
		string value = (phrase ?? "").Trim();
		if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(value) || value.Any(IsCjk))
		{
			return result;
		}
		List<string> words = Regex.Matches(value, "[\\p{L}\\p{Nd}]+", RegexOptions.CultureInvariant).Cast<Match>().Select((Match x) => x.Value).Where((string x) => !string.IsNullOrWhiteSpace(x)).ToList();
		if (words.Count == 0)
		{
			return result;
		}
		string pattern = @"(?<![\p{L}\p{Nd}])" + string.Join(@"[\s\p{P}\p{S}_]*", words.Select(Regex.Escape)) + @"(?![\p{L}\p{Nd}])";
		try
		{
			foreach (Match match in Regex.Matches(input, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
			{
				if (match != null && match.Success)
				{
					result.Add(Tuple.Create(match.Index, match.Length));
				}
			}
		}
		catch
		{
		}
		return result;
	}

	private static List<RulerTitleCandidate> BuildRulerTitleCandidates(IEnumerable<Kingdom> kingdoms)
	{
		List<RulerTitleCandidate> result = new List<RulerTitleCandidate>();
		foreach (Kingdom kingdom in kingdoms ?? Enumerable.Empty<Kingdom>())
		{
			try
			{
				if (kingdom == null || kingdom.IsEliminated)
				{
					continue;
				}
				Hero leader = kingdom.Leader;
				if (leader == null || !leader.IsAlive)
				{
					continue;
				}
				string title = SafeTextOrEmpty(kingdom.EncyclopediaRulerTitle);
				if (string.IsNullOrWhiteSpace(title))
				{
					continue;
				}
				List<string> qualifiers = new List<string>();
				AddAlias(qualifiers, SafeTextOrEmpty(kingdom.Name));
				AddAlias(qualifiers, SafeTextOrEmpty(kingdom.InformalName));
				List<string> aliases = new List<string>();
				AddAlias(aliases, title);
				foreach (string qualifier in qualifiers)
				{
					AddAlias(aliases, qualifier + title);
					AddAlias(aliases, qualifier + "的" + title);
					AddAlias(aliases, qualifier + " " + title);
					AddAlias(aliases, qualifier + "'s " + title);
					AddAlias(aliases, title + qualifier);
					AddAlias(aliases, title + " " + qualifier);
					AddAlias(aliases, title + " of " + qualifier);
				}
				result.Add(new RulerTitleCandidate
				{
					Kingdom = kingdom,
					Leader = leader,
					KingdomId = (kingdom.StringId ?? "").Trim(),
					KingdomName = SafeTextOrEmpty(kingdom.Name),
					Title = title,
					Qualifiers = qualifiers,
					Aliases = aliases
				});
			}
			catch
			{
			}
		}
		return result.OrderBy((RulerTitleCandidate x) => x.KingdomName ?? "", StringComparer.OrdinalIgnoreCase).ThenBy((RulerTitleCandidate x) => x.KingdomId ?? "", StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static int GetLongestContainedRulerTitleLength(string normalizedMention, IEnumerable<RulerTitleCandidate> candidates)
	{
		if (string.IsNullOrWhiteSpace(normalizedMention))
		{
			return 0;
		}
		int longest = 0;
		foreach (RulerTitleCandidate candidate in candidates ?? Enumerable.Empty<RulerTitleCandidate>())
		{
			string normalizedTitle = NormalizeFuzzyText(candidate?.Title);
			if (normalizedTitle.Length >= 2 && normalizedMention.IndexOf(normalizedTitle, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				longest = Math.Max(longest, normalizedTitle.Length);
			}
		}
		return longest;
	}

	private static bool IsRulerTitleShadowedByLongerContainedTitle(string normalizedMention, string normalizedTitle, int longestContainedTitleLength, IEnumerable<RulerTitleCandidate> candidates)
	{
		if (string.IsNullOrWhiteSpace(normalizedMention) || string.IsNullOrWhiteSpace(normalizedTitle) || normalizedTitle.Length >= longestContainedTitleLength)
		{
			return false;
		}
		foreach (RulerTitleCandidate candidate in candidates ?? Enumerable.Empty<RulerTitleCandidate>())
		{
			string longerTitle = NormalizeFuzzyText(candidate?.Title);
			if (longerTitle.Length > normalizedTitle.Length && longerTitle.IndexOf(normalizedTitle, StringComparison.OrdinalIgnoreCase) >= 0 && normalizedMention.IndexOf(longerTitle, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static float CalculateRulerTitleEvidenceScore(string mention, RulerTitleCandidate candidate)
	{
		if (candidate == null || string.IsNullOrWhiteSpace(candidate.Title))
		{
			return 0f;
		}
		float best = CalculateRulerTitlePhraseEvidenceScore(mention, candidate.Title);
		string normalizedMention = NormalizeFuzzyText(mention);
		foreach (string qualifier in candidate.Qualifiers ?? new List<string>())
		{
			string normalizedQualifier = NormalizeFuzzyText(qualifier);
			if (normalizedQualifier.Length < 2 || !RawTextContainsEntityPhrase(mention, qualifier))
			{
				continue;
			}
			string withoutQualifier = normalizedMention.Replace(normalizedQualifier, "");
			best = Math.Max(best, CalculateRulerTitlePhraseEvidenceScore(withoutQualifier, candidate.Title));
		}
		return best;
	}

	private static float CalculateRulerTitlePhraseEvidenceScore(string value, string title)
	{
		if (RawTextContainsEntityPhrase(value, title))
		{
			return 1f;
		}
		string normalizedValue = NormalizeFuzzyText(value);
		string normalizedTitle = NormalizeFuzzyText(title);
		if (string.IsNullOrWhiteSpace(normalizedValue) || string.IsNullOrWhiteSpace(normalizedTitle))
		{
			return 0f;
		}
		if ((title ?? "").Any(IsCjk))
		{
			return CalculateFuzzyScore(normalizedValue, normalizedTitle);
		}
		int distance = LevenshteinDistance(normalizedValue, normalizedTitle);
		return Math.Max(0f, Math.Min(1f, 1f - ((float)distance / Math.Max(normalizedValue.Length, normalizedTitle.Length))));
	}

	private static float CalculateBestRulerTitleAliasScore(string mention, RulerTitleCandidate candidate, out string matchedAlias)
	{
		matchedAlias = candidate?.Title ?? "";
		float best = 0f;
		foreach (string alias in candidate?.Aliases ?? new List<string>())
		{
			float score = CalculateFuzzyScore(mention, alias);
			if (score > best)
			{
				best = score;
				matchedAlias = alias;
			}
		}
		return best;
	}

	private static bool MentionContainsRulerTitleQualifier(string mention, RulerTitleCandidate candidate)
	{
		string normalizedMention = NormalizeFuzzyText(mention);
		if (string.IsNullOrWhiteSpace(normalizedMention) || candidate == null)
		{
			return false;
		}
		foreach (string qualifier in candidate.Qualifiers ?? new List<string>())
		{
			string normalizedQualifier = NormalizeFuzzyText(qualifier);
			if (normalizedQualifier.Length >= 2 && RawTextContainsEntityPhrase(mention, qualifier))
			{
				return true;
			}
		}
		return false;
	}

	private static List<EntityMatch<T>> FindMatches<T>(string category, IEnumerable<string> mentions, Dictionary<string, int> mentionPriority, IEnumerable<T> candidates, Func<T, IEnumerable<string>> aliases, Func<T, string> idSelector, Func<T, string> nameSelector, int candidateLimitPerMention, WorldEntityRetrievalBudget budget) where T : class
	{
		using FreezeWatchdog.ScopeToken freezeScope = FreezeWatchdog.Scope("WorldEntityRetrieval.FindMatches." + (string.IsNullOrWhiteSpace(category) ? "unknown" : category));
		Stopwatch categorySw = Stopwatch.StartNew();
		List<EntityMatch<T>> selected = new List<EntityMatch<T>>();
		int candidateLimit = Math.Max(1, Math.Min(MaxCandidatesPerMention, candidateLimitPerMention));
		List<T> candidateList = (candidates ?? Enumerable.Empty<T>()).Where((T x) => x != null).ToList();
		List<string> mentionList = (mentions ?? Enumerable.Empty<string>()).Select((string x) => (x ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)).ToList();
		Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] match_category_start category=" + (category ?? "") + " mentions=" + mentionList.Count + " candidates=" + candidateList.Count + " " + FormatBudgetForLog(budget));
		FreezeWatchdog.Mark("WorldEntityRetrieval.match_category_start", "category=" + (category ?? "") + " mentions=" + mentionList.Count + " candidates=" + candidateList.Count, immediate: true);
		List<EntityCandidateSnapshot<T>> snapshots = BuildCandidateSnapshots(category, candidateList, aliases, idSelector, nameSelector, budget);
		if (IsHardBudgetExceeded(budget))
		{
			LogWorldEntityBudgetStop("match_category_before_scoring", category, "", 0, snapshots.Count, selected.Count, budget);
			return selected.OrderBy((EntityMatch<T> x) => x.MentionPriority).ThenByDescending((EntityMatch<T> x) => x.Score).ThenByDescending((EntityMatch<T> x) => IsExactEntityNameMatch(x.Mention, x.Name)).ThenBy((EntityMatch<T> x) => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
		}
		foreach (string mentionRaw in mentionList)
		{
			if (IsHardBudgetExceeded(budget))
			{
				LogWorldEntityBudgetStop("match_category", category, "", 0, snapshots.Count, selected.Count, budget);
				break;
			}
			Stopwatch mentionSw = Stopwatch.StartNew();
			string mention = (mentionRaw ?? "").Trim();
			if (string.IsNullOrWhiteSpace(mention))
			{
				continue;
			}
			int priority = GetMentionPriority(mentionPriority, mention);
			FuzzyTextProfile mentionProfile = BuildFuzzyTextProfile(mention);
			List<EntityMatch<T>> scored = new List<EntityMatch<T>>();
			int scanned = 0;
			bool budgetStopped = false;
			Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] match_mention_start category=" + (category ?? "") + " mention=" + PreviewWorldEntityLogValue(mention, 80) + " candidates=" + snapshots.Count);
			foreach (EntityCandidateSnapshot<T> candidate in snapshots)
			{
				scanned++;
				float score = CalculateBestScore(mentionProfile, candidate.Aliases);
				if (score >= MatchThreshold)
				{
					scored.Add(new EntityMatch<T>
					{
						Value = candidate.Value,
						Id = candidate.Id ?? "",
						Name = candidate.Name ?? "",
						Mention = mention,
						Score = score,
						MentionPriority = priority
					});
				}
				if (snapshots.Count >= EntityRetrievalProgressLogInterval && scanned % EntityRetrievalProgressLogInterval == 0)
				{
					Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] match_scan_progress category=" + (category ?? "") + " mention=" + PreviewWorldEntityLogValue(mention, 80) + " scanned=" + scanned + "/" + snapshots.Count + " scored=" + scored.Count + " ms=" + Math.Round(mentionSw.Elapsed.TotalMilliseconds, 2) + " " + FormatBudgetForLog(budget));
				}
				if (budget != null && scanned % EntityRetrievalBudgetCheckInterval == 0)
				{
					LogSoftBudgetOnceIfNeeded("match_scan", category, mention, scanned, snapshots.Count, selected.Count, budget);
					if (IsHardBudgetExceeded(budget))
					{
						budgetStopped = true;
						LogWorldEntityBudgetStop("match_scan", category, mention, scanned, snapshots.Count, selected.Count, budget);
						break;
					}
				}
			}
			if (scored.Count == 0)
			{
				Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] match_mention_done category=" + (category ?? "") + " mention=" + PreviewWorldEntityLogValue(mention, 80) + " scored=0 selectedTotal=" + selected.Count + " scanned=" + scanned + "/" + snapshots.Count + " budgetStopped=" + budgetStopped + " ms=" + Math.Round(mentionSw.Elapsed.TotalMilliseconds, 2));
				if (budgetStopped)
				{
					break;
				}
				continue;
			}
			float best = scored.Max((EntityMatch<T> x) => x.Score);
			foreach (EntityMatch<T> match in scored.OrderByDescending((EntityMatch<T> x) => x.Score).ThenByDescending((EntityMatch<T> x) => IsExactEntityNameMatch(x.Mention, x.Name)).ThenBy((EntityMatch<T> x) => x.Name, StringComparer.OrdinalIgnoreCase).Take(candidateLimit))
			{
				if (match == null || match.Value == null || string.IsNullOrWhiteSpace(string.IsNullOrWhiteSpace(match.Id) ? match.Name : match.Id))
				{
					continue;
				}
				selected.Add(match);
			}
			Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] match_mention_done category=" + (category ?? "") + " mention=" + PreviewWorldEntityLogValue(mention, 80) + " scored=" + scored.Count + " selectedTotal=" + selected.Count + " best=" + best.ToString("0.###", CultureInfo.InvariantCulture) + " scanned=" + scanned + "/" + snapshots.Count + " budgetStopped=" + budgetStopped + " ms=" + Math.Round(mentionSw.Elapsed.TotalMilliseconds, 2));
			if (budgetStopped)
			{
				break;
			}
		}
		List<EntityMatch<T>> result = selected.OrderBy((EntityMatch<T> x) => x.MentionPriority).ThenByDescending((EntityMatch<T> x) => x.Score).ThenByDescending((EntityMatch<T> x) => IsExactEntityNameMatch(x.Mention, x.Name)).ThenBy((EntityMatch<T> x) => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
		Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] match_category_done category=" + (category ?? "") + " result=" + result.Count + " ms=" + Math.Round(categorySw.Elapsed.TotalMilliseconds, 2) + " hardBudgetExceeded=" + (budget?.IsHardExceeded == true));
		FreezeWatchdog.Mark("WorldEntityRetrieval.match_category_done", "category=" + (category ?? "") + " result=" + result.Count + " ms=" + Math.Round(categorySw.Elapsed.TotalMilliseconds, 2), immediate: true);
		return result;
	}

	private static List<EntityCandidateSnapshot<T>> BuildCandidateSnapshots<T>(string category, List<T> candidates, Func<T, IEnumerable<string>> aliases, Func<T, string> idSelector, Func<T, string> nameSelector, WorldEntityRetrievalBudget budget) where T : class
	{
		using FreezeWatchdog.ScopeToken freezeScope = FreezeWatchdog.Scope("WorldEntityRetrieval.AliasCache." + (string.IsNullOrWhiteSpace(category) ? "unknown" : category));
		Stopwatch sw = Stopwatch.StartNew();
		List<EntityCandidateSnapshot<T>> snapshots = new List<EntityCandidateSnapshot<T>>();
		List<T> candidateList = candidates ?? new List<T>();
		int scanned = 0;
		int aliasCount = 0;
		Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] alias_cache_start category=" + (category ?? "") + " candidates=" + candidateList.Count + " " + FormatBudgetForLog(budget));
		FreezeWatchdog.Mark("WorldEntityRetrieval.alias_cache_start", "category=" + (category ?? "") + " candidates=" + candidateList.Count, immediate: true);
		foreach (T candidate in candidateList)
		{
			if (candidate == null)
			{
				continue;
			}
			scanned++;
			EntityCandidateSnapshot<T> snapshot = new EntityCandidateSnapshot<T>
			{
				Value = candidate,
				Id = SafeSelectorValue(idSelector, candidate),
				Name = SafeSelectorValue(nameSelector, candidate),
				Aliases = BuildAliasProfiles(SafeAliases(aliases, candidate))
			};
			if (snapshot.Aliases.Count == 0 && !string.IsNullOrWhiteSpace(snapshot.Name))
			{
				snapshot.Aliases.Add(BuildFuzzyTextProfile(snapshot.Name));
			}
			aliasCount += snapshot.Aliases.Count;
			snapshots.Add(snapshot);
			if (candidateList.Count >= EntityRetrievalProgressLogInterval && scanned % EntityRetrievalProgressLogInterval == 0)
			{
				Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] alias_cache_progress category=" + (category ?? "") + " scanned=" + scanned + "/" + candidateList.Count + " snapshots=" + snapshots.Count + " aliases=" + aliasCount + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2) + " " + FormatBudgetForLog(budget));
			}
			if (budget != null && scanned % EntityRetrievalBudgetCheckInterval == 0)
			{
				LogSoftBudgetOnceIfNeeded("alias_cache", category, "", scanned, candidateList.Count, snapshots.Count, budget);
				if (IsHardBudgetExceeded(budget))
				{
					LogWorldEntityBudgetStop("alias_cache", category, "", scanned, candidateList.Count, snapshots.Count, budget);
					break;
				}
			}
		}
		Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] alias_cache_done category=" + (category ?? "") + " scanned=" + scanned + "/" + candidateList.Count + " snapshots=" + snapshots.Count + " aliases=" + aliasCount + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2) + " hardBudgetExceeded=" + (budget?.IsHardExceeded == true));
		FreezeWatchdog.Mark("WorldEntityRetrieval.alias_cache_done", "category=" + (category ?? "") + " snapshots=" + snapshots.Count + " aliases=" + aliasCount + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2), immediate: true);
		return snapshots;
	}

	private static string SafeSelectorValue<T>(Func<T, string> selector, T value) where T : class
	{
		try
		{
			return (selector?.Invoke(value) ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static IEnumerable<string> SafeAliases<T>(Func<T, IEnumerable<string>> aliases, T value) where T : class
	{
		List<string> result = new List<string>();
		try
		{
			foreach (string alias in aliases?.Invoke(value) ?? Enumerable.Empty<string>())
			{
				result.Add(alias);
			}
		}
		catch
		{
		}
		return result;
	}

	private static List<FuzzyTextProfile> BuildAliasProfiles(IEnumerable<string> aliases)
	{
		List<FuzzyTextProfile> result = new List<FuzzyTextProfile>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string alias in aliases ?? Enumerable.Empty<string>())
		{
			string text = (alias ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && seen.Add(text))
			{
				result.Add(BuildFuzzyTextProfile(text));
			}
		}
		return result;
	}

	private static bool CanContinueWorldEntityMatch(string category, WorldEntityRetrievalBudget budget)
	{
		if (!IsHardBudgetExceeded(budget))
		{
			return true;
		}
		LogWorldEntityBudgetStop("match_category_skipped", category, "", 0, 0, 0, budget);
		return false;
	}

	private static bool IsHardBudgetExceeded(WorldEntityRetrievalBudget budget)
	{
		return budget != null && budget.IsHardExceeded;
	}

	private static void LogSoftBudgetOnceIfNeeded(string phase, string category, string mention, int scanned, int total, int selectedTotal, WorldEntityRetrievalBudget budget)
	{
		if (budget == null || !budget.TryMarkSoftExceeded())
		{
			return;
		}
		Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] soft_budget_exceeded phase=" + (phase ?? "") + " category=" + (category ?? "") + " mention=" + PreviewWorldEntityLogValue(mention, 80) + " scanned=" + scanned + "/" + total + " selectedTotal=" + selectedTotal + " " + FormatBudgetForLog(budget));
		FreezeWatchdog.Mark("WorldEntityRetrieval.soft_budget_exceeded", "phase=" + (phase ?? "") + " category=" + (category ?? "") + " scanned=" + scanned + "/" + total + " selectedTotal=" + selectedTotal + " " + FormatBudgetForLog(budget), immediate: true);
	}

	private static void LogWorldEntityBudgetStop(string phase, string category, string mention, int scanned, int total, int selectedTotal, WorldEntityRetrievalBudget budget)
	{
		Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] hard_budget_stop phase=" + (phase ?? "") + " category=" + (category ?? "") + " mention=" + PreviewWorldEntityLogValue(mention, 80) + " scanned=" + scanned + "/" + total + " selectedTotal=" + selectedTotal + " " + FormatBudgetForLog(budget));
		if (budget == null || budget.TryMarkHardExceeded())
		{
			FreezeWatchdog.Mark("WorldEntityRetrieval.hard_budget_stop", "phase=" + (phase ?? "") + " category=" + (category ?? "") + " scanned=" + scanned + "/" + total + " selectedTotal=" + selectedTotal + " " + FormatBudgetForLog(budget), immediate: true);
		}
	}

	private static string FormatBudgetForLog(WorldEntityRetrievalBudget budget)
	{
		return "budgetMs=" + (budget?.ElapsedMs ?? 0L) + "/" + EntityRetrievalHardBudgetMs + " softMs=" + EntityRetrievalSoftBudgetMs;
	}

	private static string PreviewWorldEntityLogValue(string value, int maxLen)
	{
		string text = (value ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (maxLen <= 0 || text.Length <= maxLen)
		{
			return text;
		}
		return text.Substring(0, maxLen) + "...";
	}

	private static int GetMentionPriority(Dictionary<string, int> mentionPriority, string mention)
	{
		if (mentionPriority != null && !string.IsNullOrWhiteSpace(mention) && mentionPriority.TryGetValue(mention.Trim(), out var value))
		{
			return value;
		}
		return int.MaxValue / 2;
	}

	private static float CalculateBestScore(string mention, IEnumerable<string> aliases)
	{
		return CalculateBestScore(BuildFuzzyTextProfile(mention), BuildAliasProfiles(aliases));
	}

	private static float CalculateBestScore(FuzzyTextProfile mention, IEnumerable<FuzzyTextProfile> aliases)
	{
		float best = 0f;
		foreach (FuzzyTextProfile alias in aliases ?? Enumerable.Empty<FuzzyTextProfile>())
		{
			best = Math.Max(best, CalculateFuzzyScore(mention, alias));
		}
		return best;
	}

	public static float CalculateFuzzyScoreForExternal(string left, string right)
	{
		try
		{
			return CalculateFuzzyScore(left, right);
		}
		catch
		{
			return 0f;
		}
	}

	public static float CalculateBestAliasScoreForExternal(string mention, IEnumerable<string> aliases)
	{
		try
		{
			float best = 0f;
			foreach (string alias in aliases ?? Enumerable.Empty<string>())
			{
				best = Math.Max(best, CalculateFuzzyScore(mention, alias));
			}
			return Math.Max(0f, Math.Min(1f, best));
		}
		catch
		{
			return 0f;
		}
	}

	private static float CalculateFuzzyScore(string left, string right)
	{
		return CalculateFuzzyScore(BuildFuzzyTextProfile(left), BuildFuzzyTextProfile(right));
	}

	private static float CalculateFuzzyScore(FuzzyTextProfile left, FuzzyTextProfile right)
	{
		string a = left?.Normalized ?? "";
		string b = right?.Normalized ?? "";
		if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
		{
			return 0f;
		}
		if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
		{
			return 1f;
		}
		float best = 0f;
		int minLen = Math.Min(a.Length, b.Length);
		int maxLen = Math.Max(a.Length, b.Length);
		if (minLen >= 2 && (a.Contains(b) || b.Contains(a)))
		{
			best = Math.Max(best, 0.86f + 0.12f * ((float)minLen / Math.Max(1, maxLen)));
		}
		if (minLen >= 3 && (a.StartsWith(b, StringComparison.OrdinalIgnoreCase) || b.StartsWith(a, StringComparison.OrdinalIgnoreCase)))
		{
			best = Math.Max(best, 0.82f + 0.1f * ((float)minLen / Math.Max(1, maxLen)));
		}
		int distance = LevenshteinDistance(a, b);
		best = Math.Max(best, ShortCjkNearNameScore(a, b, distance));
		float distanceScore = 1f - ((float)distance / Math.Max(1, maxLen));
		best = Math.Max(best, distanceScore);
		best = Math.Max(best, TokenOverlapScore(left, right));
		return Math.Max(0f, Math.Min(1f, best));
	}

	private static FuzzyTextProfile BuildFuzzyTextProfile(string value)
	{
		string raw = (value ?? "").Trim();
		return new FuzzyTextProfile
		{
			Raw = raw,
			Normalized = NormalizeFuzzyText(raw),
			Tokens = SplitTokens(raw)
		};
	}

	private static string NormalizeFuzzyText(string value)
	{
		string text = (value ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		StringBuilder sb = new StringBuilder(text.Length);
		foreach (char c in text)
		{
			if (char.IsLetterOrDigit(c) || IsCjk(c))
			{
				sb.Append(c);
			}
		}
		return sb.ToString();
	}

	private static bool IsCjk(char c)
	{
		return (c >= 0x4e00 && c <= 0x9fff) || (c >= 0x3400 && c <= 0x4dbf) || (c >= 0xf900 && c <= 0xfaff);
	}

	private static float ShortCjkNearNameScore(string a, string b, int distance)
	{
		try
		{
			if (distance != 1 || string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
			{
				return 0f;
			}
			int minLen = Math.Min(a.Length, b.Length);
			int maxLen = Math.Max(a.Length, b.Length);
			if (minLen < 3 || maxLen > 6 || !IsAllCjkText(a) || !IsAllCjkText(b))
			{
				return 0f;
			}
			if (a.Length == b.Length)
			{
				int same = 0;
				for (int i = 0; i < a.Length; i++)
				{
					if (a[i] == b[i])
					{
						same++;
					}
				}
				if (same >= minLen - 1)
				{
					return maxLen <= 3 ? 0.82f : 0.86f;
				}
			}
			if (maxLen == minLen + 1 && IsOrderedSubsequence(a.Length <= b.Length ? a : b, a.Length <= b.Length ? b : a))
			{
				return 0.80f;
			}
		}
		catch
		{
		}
		return 0f;
	}

	private static bool IsAllCjkText(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}
		foreach (char c in value)
		{
			if (!IsCjk(c))
			{
				return false;
			}
		}
		return true;
	}

	private static bool IsOrderedSubsequence(string shortText, string longText)
	{
		if (string.IsNullOrWhiteSpace(shortText) || string.IsNullOrWhiteSpace(longText))
		{
			return false;
		}
		int j = 0;
		for (int i = 0; i < longText.Length && j < shortText.Length; i++)
		{
			if (shortText[j] == longText[i])
			{
				j++;
			}
		}
		return j == shortText.Length;
	}

	private static float TokenOverlapScore(string left, string right)
	{
		return TokenOverlapScore(BuildFuzzyTextProfile(left), BuildFuzzyTextProfile(right));
	}

	private static float TokenOverlapScore(FuzzyTextProfile left, FuzzyTextProfile right)
	{
		List<string> a = left?.Tokens ?? new List<string>();
		List<string> b = right?.Tokens ?? new List<string>();
		if (a.Count == 0 || b.Count == 0)
		{
			return 0f;
		}
		HashSet<string> setA = new HashSet<string>(a, StringComparer.OrdinalIgnoreCase);
		HashSet<string> setB = new HashSet<string>(b, StringComparer.OrdinalIgnoreCase);
		int intersection = setA.Count((string x) => setB.Contains(x));
		int union = setA.Count + setB.Count - intersection;
		return union <= 0 ? 0f : (0.65f + 0.25f * ((float)intersection / union));
	}

	private static List<string> SplitTokens(string value)
	{
		return Regex.Matches((value ?? "").ToLowerInvariant(), "[\\p{L}\\p{Nd}]+", RegexOptions.CultureInvariant).Cast<Match>().Select((Match x) => x.Value).Where((string x) => x.Length > 1).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static int LevenshteinDistance(string a, string b)
	{
		int n = a.Length;
		int m = b.Length;
		int[] previous = new int[m + 1];
		int[] current = new int[m + 1];
		for (int j = 0; j <= m; j++)
		{
			previous[j] = j;
		}
		for (int i = 1; i <= n; i++)
		{
			current[0] = i;
			for (int j = 1; j <= m; j++)
			{
				int cost = a[i - 1] == b[j - 1] ? 0 : 1;
				current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
			}
			int[] temp = previous;
			previous = current;
			current = temp;
		}
		return previous[m];
	}

	private static IEnumerable<Hero> GetHeroCandidates()
	{
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Hero hero in ((IEnumerable<Hero>)Hero.AllAliveHeroes ?? Enumerable.Empty<Hero>()))
		{
			if (hero != null && seen.Add(hero.StringId ?? SafeName(hero.Name, "")))
			{
				yield return hero;
			}
		}
		foreach (Hero hero in ((IEnumerable<Hero>)Hero.DeadOrDisabledHeroes ?? Enumerable.Empty<Hero>()))
		{
			if (hero != null && seen.Add(hero.StringId ?? SafeName(hero.Name, "")))
			{
				yield return hero;
			}
		}
	}

	private static IEnumerable<Settlement> GetSettlementCandidates()
	{
		return (IEnumerable<Settlement>)Settlement.All ?? Enumerable.Empty<Settlement>();
	}

	private static IEnumerable<Clan> GetClanCandidates()
	{
		return (IEnumerable<Clan>)Clan.All ?? Enumerable.Empty<Clan>();
	}

	private static IEnumerable<Kingdom> GetKingdomCandidates()
	{
		return (IEnumerable<Kingdom>)Kingdom.All ?? Enumerable.Empty<Kingdom>();
	}

	private static IEnumerable<string> GetHeroAliases(Hero hero)
	{
		return NonEmpty(SafeName(hero?.Name, ""), hero?.StringId, SafeName(hero?.CharacterObject?.Name, ""), hero?.CharacterObject?.StringId);
	}

	private static IEnumerable<string> GetSettlementAliases(Settlement settlement)
	{
		return NonEmpty(SafeName(settlement?.Name, ""), settlement?.StringId);
	}

	private static IEnumerable<string> GetClanAliases(Clan clan)
	{
		return NonEmpty(SafeName(clan?.Name, ""), SafeName(clan?.InformalName, ""), clan?.StringId);
	}

	private static IEnumerable<string> GetKingdomAliases(Kingdom kingdom)
	{
		return NonEmpty(SafeName(kingdom?.Name, ""), SafeName(kingdom?.InformalName, ""), kingdom?.StringId);
	}

	private static void AddAlias(List<string> aliases, string value)
	{
		if (aliases == null)
		{
			return;
		}
		string text = (value ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text) && !aliases.Any((string x) => string.Equals((x ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase)))
		{
			aliases.Add(text);
		}
	}

	private static IEnumerable<string> NonEmpty(params string[] values)
	{
		foreach (string value in values ?? Array.Empty<string>())
		{
			string text = (value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				yield return text;
			}
		}
	}

	private static string BuildMainPromptBlock(string playerDisplayName, Hero contextHero, List<EntityMatch<Hero>> heroes, List<EntityMatch<Settlement>> settlements, List<EntityMatch<Clan>> clans, List<EntityMatch<Kingdom>> kingdoms, List<VisiblePartyCandidate> visibleParties)
	{
		string player = ResolvePlayerDisplayNameForPrompt(playerDisplayName);
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("你和" + player + "交流可用的实体信息：");
		AppendHeroMainFacts(sb, heroes, player, contextHero);
		AppendSettlementMainFacts(sb, settlements);
		AppendClanMainFacts(sb, clans);
		AppendKingdomMainFacts(sb, kingdoms);
		AppendVisiblePartyFacts(sb, visibleParties);
		return StripEntityIdsFromMainPromptBlock(sb.ToString()).Trim();
	}

	private static void AddResidentEntityMatches(Hero contextHero, bool includeResidentKingdoms, bool includeResidentPlayerEntities, ref List<EntityMatch<Hero>> heroes, ref List<EntityMatch<Settlement>> settlements, ref List<EntityMatch<Clan>> clans, ref List<EntityMatch<Kingdom>> kingdoms)
	{
		heroes = heroes ?? new List<EntityMatch<Hero>>();
		settlements = settlements ?? new List<EntityMatch<Settlement>>();
		clans = clans ?? new List<EntityMatch<Clan>>();
		kingdoms = kingdoms ?? new List<EntityMatch<Kingdom>>();
		int priority = -1000;
		if (includeResidentPlayerEntities)
		{
			Hero player = Hero.MainHero;
			Clan playerClan = Clan.PlayerClan ?? player?.Clan;
			AddResidentClanMatch(clans, playerClan, "常驻：玩家当前家族", priority++);
			if (includeResidentKingdoms)
			{
				AddResidentKingdomMatch(kingdoms, ResolveHeroKingdomForResidentEntity(player, playerClan), "常驻：玩家当前王国", priority++);
			}
		}
		if (contextHero != null)
		{
			string contextName = SafeName(contextHero.Name, "当前对话人物");
			AddResidentHeroMatch(heroes, contextHero, "常驻：" + contextName + "本人", priority++);
			AddResidentClanMatch(clans, contextHero.Clan, "常驻：" + contextName + "家族", priority++);
			if (includeResidentKingdoms)
			{
				AddResidentKingdomMatch(kingdoms, ResolveHeroKingdomForResidentEntity(contextHero, contextHero.Clan), "常驻：" + contextName + "当前王国", priority++);
			}
		}
		SortEntityMatches(heroes);
		SortEntityMatches(settlements);
		SortEntityMatches(clans);
		SortEntityMatches(kingdoms);
	}

	private static void AddPostprocessResidentEntityMatches(Hero contextHero, bool includeResidentPlayerEntities, ref List<EntityMatch<Hero>> heroes, ref List<EntityMatch<Settlement>> settlements, ref List<EntityMatch<Clan>> clans, ref List<EntityMatch<Kingdom>> kingdoms)
	{
		heroes = heroes ?? new List<EntityMatch<Hero>>();
		settlements = settlements ?? new List<EntityMatch<Settlement>>();
		clans = clans ?? new List<EntityMatch<Clan>>();
		kingdoms = kingdoms ?? new List<EntityMatch<Kingdom>>();
		int priority = -1000;
		if (includeResidentPlayerEntities)
		{
			Hero player = Hero.MainHero;
			Clan playerClan = Clan.PlayerClan ?? player?.Clan;
			AddResidentClanMatch(clans, playerClan, "常驻：玩家当前家族", priority++);
			AddResidentKingdomMatch(kingdoms, ResolveHeroKingdomForResidentEntity(player, playerClan), "常驻：玩家当前王国", priority++);
		}
		if (contextHero != null)
		{
			string contextName = SafeName(contextHero.Name, "当前对话人物");
			AddResidentHeroMatch(heroes, contextHero, "常驻：" + contextName + "本人", priority++);
			AddResidentClanMatch(clans, contextHero.Clan, "常驻：" + contextName + "的家族", priority++);
			AddResidentKingdomMatch(kingdoms, ResolveHeroKingdomForResidentEntity(contextHero, contextHero.Clan), "常驻：" + contextName + "的王国", priority++);
		}
		SortEntityMatches(heroes);
		SortEntityMatches(settlements);
		SortEntityMatches(clans);
		SortEntityMatches(kingdoms);
	}

	private static List<EntityMatch<T>> CloneEntityMatches<T>(IEnumerable<EntityMatch<T>> matches) where T : class
	{
		List<EntityMatch<T>> result = new List<EntityMatch<T>>();
		if (matches == null)
		{
			return result;
		}
		foreach (EntityMatch<T> match in matches)
		{
			if (match == null)
			{
				continue;
			}
			result.Add(new EntityMatch<T>
			{
				Value = match.Value,
				Id = match.Id,
				Name = match.Name,
				Mention = match.Mention,
				Score = match.Score,
				MentionPriority = match.MentionPriority,
				RulerTitleKey = match.RulerTitleKey
			});
		}
		return result;
	}

	private static Kingdom ResolveHeroKingdomForResidentEntity(Hero hero, Clan fallbackClan = null)
	{
		try
		{
			return fallbackClan?.Kingdom ?? hero?.Clan?.Kingdom ?? hero?.MapFaction as Kingdom;
		}
		catch
		{
			return null;
		}
	}

	private static Settlement ResolveHeroCurrentLocationSettlementForResidentEntity(Hero hero)
	{
		try
		{
			if (hero == null)
			{
				return null;
			}
			if (hero.CurrentSettlement != null)
			{
				return hero.CurrentSettlement;
			}
			if (hero.IsPrisoner && hero.PartyBelongedToAsPrisoner != null)
			{
				PartyBase holder = hero.PartyBelongedToAsPrisoner;
				if (holder.IsSettlement && holder.Settlement != null)
				{
					return holder.Settlement;
				}
				if (holder.IsMobile && holder.MobileParty != null)
				{
					return ResolveMobilePartyLocationSettlementForResidentEntity(holder.MobileParty);
				}
			}
			Settlement partySettlement = ResolveMobilePartyLocationSettlementForResidentEntity(hero.PartyBelongedTo);
			if (partySettlement != null)
			{
				return partySettlement;
			}
			return hero.HomeSettlement;
		}
		catch
		{
			return null;
		}
	}

	private static Settlement ResolveMobilePartyLocationSettlementForResidentEntity(MobileParty party)
	{
		try
		{
			if (party == null)
			{
				return null;
			}
			if (party.CurrentSettlement != null)
			{
				return party.CurrentSettlement;
			}
			if (party.BesiegedSettlement != null)
			{
				return party.BesiegedSettlement;
			}
			if (party.TargetSettlement != null)
			{
				return party.TargetSettlement;
			}
			if (party.Position.IsValid())
			{
				Settlement nearest = FindNearestSettlement(party.Position, out var _);
				if (nearest != null)
				{
					return nearest;
				}
			}
			return party.LastVisitedSettlement ?? party.HomeSettlement;
		}
		catch
		{
			return null;
		}
	}

	private static void AddResidentHeroMatch(List<EntityMatch<Hero>> matches, Hero hero, string mention, int priority)
	{
		AddResidentMatch(matches, hero, "hero:" + SafeStringId(hero?.StringId), SafeName(hero?.Name, hero?.StringId ?? "人物"), mention, priority);
	}

	private static void AddResidentSettlementMatch(List<EntityMatch<Settlement>> matches, Settlement settlement, string mention, int priority)
	{
		AddResidentMatch(matches, settlement, "settlement:" + SafeStringId(settlement?.StringId), SafeName(settlement?.Name, settlement?.StringId ?? "地点"), mention, priority);
	}

	private static void AddResidentClanMatch(List<EntityMatch<Clan>> matches, Clan clan, string mention, int priority)
	{
		AddResidentMatch(matches, clan, "clan:" + SafeStringId(clan?.StringId), SafeName(clan?.Name, clan?.StringId ?? "家族"), mention, priority);
	}

	private static void AddResidentKingdomMatch(List<EntityMatch<Kingdom>> matches, Kingdom kingdom, string mention, int priority)
	{
		AddResidentMatch(matches, kingdom, "kingdom:" + SafeStringId(kingdom?.StringId), SafeName(kingdom?.Name, kingdom?.StringId ?? "王国"), mention, priority);
	}

	private static void AddResidentMatch<T>(List<EntityMatch<T>> matches, T value, string id, string name, string mention, int priority) where T : class
	{
		if (matches == null || value == null || string.IsNullOrWhiteSpace(id))
		{
			return;
		}
		EntityMatch<T> existing = matches.FirstOrDefault((EntityMatch<T> x) => x != null && string.Equals(string.IsNullOrWhiteSpace(x.Id) ? x.Name : x.Id, id, StringComparison.OrdinalIgnoreCase));
		if (existing == null)
		{
			matches.Add(new EntityMatch<T>
			{
				Value = value,
				Id = id,
				Name = name ?? "",
				Mention = mention ?? "",
				Score = 1f,
				MentionPriority = priority
			});
			return;
		}
		string mergedMention = MergeEntityMention(existing.Mention, mention);
		if (priority < existing.MentionPriority)
		{
			existing.Value = value;
			existing.Id = id;
			existing.Name = string.IsNullOrWhiteSpace(existing.Name) ? (name ?? "") : existing.Name;
			existing.Mention = mergedMention;
			existing.Score = Math.Max(existing.Score, 1f);
			existing.MentionPriority = priority;
			return;
		}
		existing.Mention = mergedMention;
		existing.Score = Math.Max(existing.Score, 1f);
	}

	private static string MergeEntityMention(string existing, string addition)
	{
		string left = (existing ?? "").Trim();
		string right = (addition ?? "").Trim();
		if (string.IsNullOrWhiteSpace(right))
		{
			return left;
		}
		if (string.IsNullOrWhiteSpace(left))
		{
			return right;
		}
		if (left.Split(new[] { '；' }, StringSplitOptions.RemoveEmptyEntries).Select((string x) => x.Trim()).Any((string x) => string.Equals(x, right, StringComparison.OrdinalIgnoreCase)))
		{
			return left;
		}
		return left + "；" + right;
	}

	private static void SortEntityMatches<T>(List<EntityMatch<T>> matches) where T : class
	{
		if (matches == null || matches.Count <= 1)
		{
			return;
		}
		matches.Sort(delegate(EntityMatch<T> left, EntityMatch<T> right)
		{
			if (left == null && right == null)
			{
				return 0;
			}
			if (left == null)
			{
				return 1;
			}
			if (right == null)
			{
				return -1;
			}
			int cmp = left.MentionPriority.CompareTo(right.MentionPriority);
			if (cmp != 0)
			{
				return cmp;
			}
			cmp = right.Score.CompareTo(left.Score);
			return cmp != 0 ? cmp : StringComparer.OrdinalIgnoreCase.Compare(left.Name ?? "", right.Name ?? "");
		});
	}

	private static string ResolvePlayerDisplayNameForPrompt(string playerDisplayName)
	{
		string text = (playerDisplayName ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		try
		{
			text = (MyBehavior.BuildPlayerPublicDisplayNameForExternal() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
		}
		catch
		{
		}
		return "玩家";
	}

	private static string StripEntityIdsFromMainPromptBlock(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string result = text;
		result = Regex.Replace(result, "（\\s*编号[:：][^；）]*(?:；\\s*)?", "（", RegexOptions.IgnoreCase);
		result = Regex.Replace(result, "；\\s*编号[:：][^；）\\r\\n]*", "", RegexOptions.IgnoreCase);
		result = Regex.Replace(result, "编号[:：][^；）\\r\\n]*(?:；\\s*)?", "", RegexOptions.IgnoreCase);
		result = Regex.Replace(result, "；\\s*部队ID[:：][^；）\\r\\n]*", "", RegexOptions.IgnoreCase);
		result = Regex.Replace(result, "部队ID[:：][^；）\\r\\n]*(?:；\\s*)?", "", RegexOptions.IgnoreCase);
		result = Regex.Replace(result, "（\\s*；", "（");
		result = Regex.Replace(result, "；\\s*）", "）");
		result = Regex.Replace(result, "（\\s*）", "");
		result = Regex.Replace(result, @"\b(?:hero|settlement|clan|kingdom|troop|party|mobile_party):[A-Za-z0-9_.\-]+\b", "未知", RegexOptions.IgnoreCase);
		result = Regex.Replace(result, @"(?<![\p{L}\p{N}_])(?:lord|lady|wanderer|companion|town|castle|village|settlement|clan|kingdom|troop|looters|bandits|mountain_bandits|forest_bandits|desert_bandits|sea_raiders|steppe_bandits|villagers|caravan|party|mobile_party)[A-Za-z0-9_\-]*\d[A-Za-z0-9_\-]*(?![\p{L}\p{N}_])", "未知", RegexOptions.IgnoreCase);
		return result.Trim();
	}

	private static string BuildPostprocessPromptBlock(List<EntityMatch<Hero>> heroes, List<EntityMatch<Settlement>> settlements, List<EntityMatch<Clan>> clans, List<EntityMatch<Kingdom>> kingdoms, List<VisiblePartyCandidate> visibleParties)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("可能有效的信息：");
		AppendPlayerPostprocessFacts(sb);
		List<EntityMatch<Hero>> postprocessHeroes = (heroes ?? new List<EntityMatch<Hero>>()).Where(IsPostprocessHeroMatchEligible).ToList();
		List<EntityMatch<Clan>> postprocessClans = (clans ?? new List<EntityMatch<Clan>>()).Where(IsPostprocessClanMatchEligible).ToList();
		List<EntityMatch<Kingdom>> postprocessKingdoms = (kingdoms ?? new List<EntityMatch<Kingdom>>()).Where(IsPostprocessKingdomMatchEligible).ToList();
		if (postprocessHeroes.Count > 0)
		{
			sb.AppendLine("【人物】");
			for (int i = 0; i < postprocessHeroes.Count; i++)
			{
				Hero hero = postprocessHeroes[i].Value;
				sb.AppendLine((i + 1) + ". 名称：" + SafeName(hero?.Name, postprocessHeroes[i].Name) + "；位置：" + FormatHeroLocation(hero) + "；ID：" + FormatPostprocessEntityId(postprocessHeroes[i].Id) + FormatPostprocessMentionHint(postprocessHeroes[i]));
			}
		}
		if (settlements != null && settlements.Count > 0)
		{
			sb.AppendLine("【地点】");
			for (int i = 0; i < settlements.Count; i++)
			{
				Settlement settlement = settlements[i].Value;
				sb.AppendLine((i + 1) + ". 名称：" + SafeName(settlement?.Name, settlements[i].Name) + "；ID：" + FormatPostprocessEntityId(settlements[i].Id) + FormatPostprocessMentionHint(settlements[i]));
			}
		}
		if (postprocessClans.Count > 0)
		{
			sb.AppendLine("【家族】");
			for (int i = 0; i < postprocessClans.Count; i++)
			{
				Clan clan = postprocessClans[i].Value;
				sb.AppendLine((i + 1) + ". 名称：" + SafeName(clan?.Name, postprocessClans[i].Name) + "；ID：" + FormatPostprocessEntityId(postprocessClans[i].Id) + FormatPostprocessMentionHint(postprocessClans[i]));
			}
		}
		if (postprocessKingdoms.Count > 0)
		{
			sb.AppendLine("【王国】");
			for (int i = 0; i < postprocessKingdoms.Count; i++)
			{
				Kingdom kingdom = postprocessKingdoms[i].Value;
				sb.AppendLine((i + 1) + ". 名称：" + SafeName(kingdom?.Name, postprocessKingdoms[i].Name) + "；ID：" + FormatPostprocessEntityId(postprocessKingdoms[i].Id) + FormatPostprocessMentionHint(postprocessKingdoms[i]));
			}
		}
		if (visibleParties != null && visibleParties.Count > 0)
		{
			sb.AppendLine("【附近可见部队】");
			for (int i = 0; i < visibleParties.Count; i++)
			{
				sb.AppendLine(BuildVisiblePartyPromptLine(i + 1, visibleParties[i]));
			}
		}
		return sb.ToString().Trim();
	}

	private static string FormatPostprocessEntityId(string id)
	{
		string text = (id ?? "").Trim();
		int separatorIndex = text.IndexOf(':');
		if (separatorIndex <= 0)
		{
			return text;
		}
		switch (text.Substring(0, separatorIndex).ToLowerInvariant())
		{
		case "hero":
		case "settlement":
		case "clan":
		case "kingdom":
		case "troop":
		case "party":
		case "mobile_party":
			return text.Substring(separatorIndex + 1).Trim();
		default:
			return text;
		}
	}

	private static string FormatPostprocessMentionHint<T>(EntityMatch<T> match) where T : class
	{
		string mention = (match?.Mention ?? "").Trim();
		return string.IsNullOrWhiteSpace(mention) ? "" : ("；提示：" + mention);
	}

	private static bool IsPostprocessHeroMatchEligible(EntityMatch<Hero> match)
	{
		try
		{
			Hero hero = match?.Value;
			return hero != null && hero.IsAlive;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPostprocessClanMatchEligible(EntityMatch<Clan> match)
	{
		try
		{
			Clan clan = match?.Value;
			return clan != null && !clan.IsEliminated;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPostprocessKingdomMatchEligible(EntityMatch<Kingdom> match)
	{
		try
		{
			Kingdom kingdom = match?.Value;
			return kingdom != null && !kingdom.IsEliminated;
		}
		catch
		{
			return false;
		}
	}

	private static void AppendPlayerPostprocessFacts(StringBuilder sb)
	{
		if (sb == null)
		{
			return;
		}
		try
		{
			Hero player = Hero.MainHero;
			string id = (player?.StringId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(id))
			{
				return;
			}
			sb.AppendLine("【玩家本人】");
			sb.AppendLine("1. 名称：" + SafeName(player.Name, "玩家") + "；固定ID：" + id + "；用于FOLLOW玩家时目标类型写hero，id填写" + id + "。");
		}
		catch
		{
		}
	}

	private static void AppendVisiblePartyFacts(StringBuilder sb, List<VisiblePartyCandidate> parties)
	{
		if (sb == null || parties == null || parties.Count == 0)
		{
			return;
		}
		sb.AppendLine();
		sb.AppendLine("【附近可见部队】");
		for (int i = 0; i < parties.Count; i++)
		{
			sb.AppendLine(BuildVisiblePartyPromptLine(i + 1, parties[i]));
		}
	}

	private static string BuildVisiblePartyPromptLine(int index, VisiblePartyCandidate party)
	{
		if (party == null)
		{
			return index + ". 名称：未知；数量：0";
		}
		string shipSegment = string.IsNullOrWhiteSpace(party.ShipInfo) ? "" : ("；舰船：" + party.ShipInfo.Trim());
		string relationSegment = BuildVisiblePartyRelationPromptSegment(party);
		return index + ". 名称：" + party.Name + "；数量：" + party.Count + shipSegment + "；部队ID：" + party.Id + "；从属：" + party.Affiliation + relationSegment + "；方位：" + party.Direction + "；距离：" + FormatDistance(party.Distance);
	}

	private static string BuildVisiblePartyRelationPromptSegment(VisiblePartyCandidate party)
	{
		if (party == null)
		{
			return "";
		}
		List<string> parts = new List<string>();
		if (!string.IsNullOrWhiteSpace(party.RelationToContextHero))
		{
			parts.Add("与NPC关系：" + party.RelationToContextHero.Trim());
		}
		if (!string.IsNullOrWhiteSpace(party.RelationToPlayer))
		{
			parts.Add("与玩家关系：" + party.RelationToPlayer.Trim());
		}
		return parts.Count == 0 ? "" : ("；" + string.Join("；", parts));
	}

	private static void AppendHeroMainFacts(StringBuilder sb, List<EntityMatch<Hero>> matches, string playerDisplayName, Hero contextHero)
	{
		if (matches == null || matches.Count == 0)
		{
			return;
		}
		sb.AppendLine();
		sb.AppendLine("【人物】");
		for (int i = 0; i < matches.Count; i++)
		{
			Hero hero = matches[i].Value;
			sb.AppendLine((i + 1) + ". " + SafeName(hero?.Name, matches[i].Name) + "（编号：" + matches[i].Id + "；匹配分：" + FormatScore(matches[i].Score) + "；提及：" + matches[i].Mention + "）");
			sb.AppendLine("所属家族：" + SafeName(hero?.Clan?.Name, "未知") + "；王国：" + FormatHeroKingdom(hero));
			string relationship = FormatHeroRelationshipForMainPrompt(playerDisplayName, contextHero, hero);
			if (!string.IsNullOrWhiteSpace(relationship))
			{
				sb.AppendLine(relationship);
			}
			sb.AppendLine("特质：" + FormatHeroTraits(hero) + "；亲属：" + FormatHeroRelatives(hero));
			sb.AppendLine("位置：" + FormatHeroLocation(hero) + "；状态：" + FormatHeroStatus(hero));
			sb.AppendLine("年龄：" + FormatAge(hero) + "；生死：" + FormatBool(hero != null && hero.IsAlive) + "；性别：" + FormatGender(hero) + "；职业/头衔：" + FormatHeroOccupation(hero));
		}
	}

	private static void AppendSettlementMainFacts(StringBuilder sb, List<EntityMatch<Settlement>> matches)
	{
		if (matches == null || matches.Count == 0)
		{
			return;
		}
		sb.AppendLine();
		sb.AppendLine("【地点】");
		for (int i = 0; i < matches.Count; i++)
		{
			Settlement settlement = matches[i].Value;
			string settlementDisplayName = settlement == null ? SafeName(settlement?.Name, matches[i].Name) : FormatSettlementNameWithType(settlement);
			sb.AppendLine((i + 1) + ". " + settlementDisplayName + "（编号：" + matches[i].Id + "；匹配分：" + FormatScore(matches[i].Score) + "；提及：" + matches[i].Mention + "）");
			sb.AppendLine("所属家族：" + SafeName(settlement?.OwnerClan?.Name, "未知") + "；王国：" + FormatSettlementKingdom(settlement) + "；文化：" + SafeName(settlement?.Culture?.Name, settlement?.Culture?.StringId ?? "未知"));
			sb.AppendLine("兵力：" + FormatSettlementStrength(settlement) + "；繁荣度：" + FormatSettlementProsperity(settlement) + "；人口：" + FormatSettlementPopulation(settlement) + "；忠诚度：" + FormatSettlementLoyalty(settlement));
			sb.AppendLine("下属村庄：" + FormatBoundVillages(settlement) + "；当前状态：" + FormatSettlementStatus(settlement));
		}
	}

	private static void AppendClanMainFacts(StringBuilder sb, List<EntityMatch<Clan>> matches)
	{
		if (matches == null || matches.Count == 0)
		{
			return;
		}
		sb.AppendLine();
		sb.AppendLine("【家族】");
		for (int i = 0; i < matches.Count; i++)
		{
			Clan clan = matches[i].Value;
			sb.AppendLine((i + 1) + ". " + SafeName(clan?.Name, matches[i].Name) + "（编号：" + matches[i].Id + "；匹配分：" + FormatScore(matches[i].Score) + "；提及：" + matches[i].Mention + "）");
			sb.AppendLine("族长：" + SafeName(clan?.Leader?.Name, "未知") + "；主要成员：" + FormatHeroList(clan?.Heroes, MainPromptClanMemberCap));
			sb.AppendLine("所属王国：" + SafeName(clan?.Kingdom?.Name, "无") + "；影响力：" + FormatFloat(clan?.Influence) + "；文化：" + SafeName(clan?.Culture?.Name, clan?.Culture?.StringId ?? "未知"));
			sb.AppendLine("财富：" + FormatInt(clan?.Gold) + "；等级：" + FormatInt(clan?.Tier) + "；是否灭亡：" + FormatEliminatedStatus(clan?.IsEliminated) + "；主要定居点：" + FormatClanFiefs(clan, MainPromptClanFiefCap));
		}
	}

	private static void AppendKingdomMainFacts(StringBuilder sb, List<EntityMatch<Kingdom>> matches)
	{
		if (matches == null || matches.Count == 0)
		{
			return;
		}
		sb.AppendLine();
		sb.AppendLine("【王国】");
		for (int i = 0; i < matches.Count; i++)
		{
			Kingdom kingdom = matches[i].Value;
			sb.AppendLine((i + 1) + ". " + SafeName(kingdom?.Name, matches[i].Name) + "（编号：" + matches[i].Id + "；匹配分：" + FormatScore(matches[i].Score) + "；提及：" + matches[i].Mention + "）");
			sb.AppendLine("国王：" + SafeName(kingdom?.Leader?.Name, "未知") + "；总兵力：" + FormatFloat(kingdom?.CurrentTotalStrength) + "；文化：" + SafeName(kingdom?.Culture?.Name, kingdom?.Culture?.StringId ?? "未知"));
			string encyclopediaBackground = FormatKingdomEncyclopediaBackground(kingdom);
			if (!string.IsNullOrWhiteSpace(encyclopediaBackground))
			{
				sb.AppendLine("百科背景：" + encyclopediaBackground);
			}
			sb.AppendLine("王国定居点概览：" + FormatKingdomSettlementSummary(kingdom));
			sb.AppendLine("主要家族：" + FormatKingdomClans(kingdom, MainPromptKingdomClanCap));
			sb.AppendLine("王国当前状态：" + FormatKingdomStatus(kingdom));
		}
	}

	private static string FormatHeroRelationshipForMainPrompt(string playerDisplayName, Hero contextHero, Hero targetHero)
	{
		if (targetHero == null)
		{
			return "";
		}
		try
		{
			List<string> relationships = new List<string>();
			string playerName = ResolvePlayerDisplayNameForPrompt(playerDisplayName);
			string playerRelationship = FormatHeroRelationshipToReference(playerName, Hero.MainHero, targetHero);
			if (!string.IsNullOrWhiteSpace(playerRelationship))
			{
				relationships.Add(playerRelationship);
			}
			if (contextHero != null && !IsSameHero(contextHero, Hero.MainHero))
			{
				string contextName = SafeName(contextHero.Name, "当前交谈对象");
				string contextRelationship = FormatHeroRelationshipToReference(contextName, contextHero, targetHero);
				if (!string.IsNullOrWhiteSpace(contextRelationship))
				{
					relationships.Add(contextRelationship);
				}
			}
			return relationships.Count == 0 ? "" : (string.Join("；", relationships) + "。");
		}
		catch
		{
			return "";
		}
	}

	private static string FormatHeroRelationshipToReference(string referenceName, Hero referenceHero, Hero targetHero)
	{
		if (referenceHero == null || targetHero == null)
		{
			return "";
		}
		string name = string.IsNullOrWhiteSpace(referenceName) ? "该人物" : referenceName.Trim();
		if (IsSameHero(referenceHero, targetHero))
		{
			return "与" + name + "的关系：本人";
		}
		List<string> parts = new List<string>();
		try
		{
			if (TryGetRelationValueForPrompt(referenceHero, targetHero, out int relation))
			{
				parts.Add("原版个人关系值：" + relation.ToString(CultureInfo.InvariantCulture) + "（" + FormatRelationBand(relation) + "）");
			}
			AddKinshipRelationship(parts, referenceHero, targetHero);
			AddPoliticalRelationship(parts, referenceHero, name, targetHero);
			return parts.Count == 0 ? "" : ("与" + name + "的关系：" + string.Join("；", parts));
		}
		catch
		{
			return "";
		}
	}

	private static bool TryGetRelationValueForPrompt(Hero referenceHero, Hero targetHero, out int relation)
	{
		relation = 0;
		try
		{
			if (referenceHero == null || targetHero == null)
			{
				return false;
			}
			if (IsSameHero(referenceHero, Hero.MainHero) && RomanceSystemBehavior.TryGetPrivateLoveAsPlayerRelation(targetHero, out relation))
			{
				return true;
			}
			if (IsSameHero(targetHero, Hero.MainHero) && RomanceSystemBehavior.TryGetPrivateLoveAsPlayerRelation(referenceHero, out relation))
			{
				return true;
			}
			relation = referenceHero.GetRelation(targetHero);
			return true;
		}
		catch
		{
			relation = 0;
			return false;
		}
	}

	private static bool IsSameHero(Hero a, Hero b)
	{
		if (a == null || b == null)
		{
			return false;
		}
		if (ReferenceEquals(a, b) || a == b)
		{
			return true;
		}
		string id = (a.StringId ?? "").Trim();
		string id2 = (b.StringId ?? "").Trim();
		return !string.IsNullOrWhiteSpace(id) && string.Equals(id, id2, StringComparison.OrdinalIgnoreCase);
	}

	private static void AddKinshipRelationship(List<string> parts, Hero contextHero, Hero targetHero)
	{
		try
		{
			if (contextHero.Spouse == targetHero)
			{
				parts.Add("配偶");
			}
			if (contextHero.Father == targetHero)
			{
				parts.Add("父亲");
			}
			if (contextHero.Mother == targetHero)
			{
				parts.Add("母亲");
			}
			if (targetHero.Father == contextHero || targetHero.Mother == contextHero)
			{
				parts.Add(targetHero.IsFemale ? "女儿" : "儿子");
			}
			if (contextHero.Siblings != null && contextHero.Siblings.Contains(targetHero))
			{
				parts.Add(targetHero.IsFemale ? "姐妹" : "兄弟");
			}
		}
		catch
		{
		}
	}

	private static void AddPoliticalRelationship(List<string> parts, Hero contextHero, string contextName, Hero targetHero)
	{
		try
		{
			if (contextHero.Clan != null && contextHero.Clan == targetHero.Clan)
			{
				parts.Add("同一家族：" + SafeName(contextHero.Clan.Name, "未知"));
				if (contextHero.Clan.Leader == targetHero)
				{
					parts.Add("该人物是" + contextName + "的家族族长");
				}
				else if (contextHero.Clan.Leader == contextHero)
				{
					parts.Add(contextName + "是该人物的家族族长");
				}
			}
			IFaction contextFaction = contextHero.MapFaction;
			IFaction targetFaction = targetHero.MapFaction;
			if (contextFaction != null && targetFaction != null)
			{
				if (contextFaction == targetFaction)
				{
					parts.Add("同一阵营：" + SafeName(contextFaction.Name, "未知"));
				}
				else if (contextFaction.IsAtWarWith(targetFaction))
				{
					parts.Add("敌对阵营：" + SafeName(contextFaction.Name, "未知") + " vs " + SafeName(targetFaction.Name, "未知"));
				}
				else
				{
					parts.Add("不同阵营：" + SafeName(contextFaction.Name, "未知") + " vs " + SafeName(targetFaction.Name, "未知"));
				}
			}
		}
		catch
		{
		}
	}

	private static string FormatRelationBand(int relation)
	{
		if (relation <= -80)
		{
			return "死敌";
		}
		if (relation <= -40)
		{
			return "敌对";
		}
		if (relation <= -10)
		{
			return "反感";
		}
		if (relation < 10)
		{
			return "中立";
		}
		if (relation < 40)
		{
			return "友好";
		}
		if (relation < 80)
		{
			return "亲近";
		}
		return "至交";
	}

	private static string FormatHeroKingdom(Hero hero)
	{
		if (hero == null)
		{
			return "未知";
		}
		try
		{
			return SafeName(hero.Clan?.Kingdom?.Name, SafeName(hero.MapFaction?.Name, "无"));
		}
		catch
		{
			return "未知";
		}
	}

	private static string FormatHeroTraits(Hero hero)
	{
		if (hero == null)
		{
			return "未知";
		}
		List<string> parts = new List<string>();
		AddTrait(parts, hero, DefaultTraits.Mercy, "Mercy");
		AddTrait(parts, hero, DefaultTraits.Valor, "Valor");
		AddTrait(parts, hero, DefaultTraits.Honor, "Honor");
		AddTrait(parts, hero, DefaultTraits.Generosity, "Generosity");
		AddTrait(parts, hero, DefaultTraits.Calculating, "Calculating");
		return parts.Count == 0 ? "无显著特质" : string.Join("，", parts);
	}

	private static void AddTrait(List<string> parts, Hero hero, TraitObject trait, string label)
	{
		try
		{
			int level = hero.GetTraitLevel(trait);
			if (level != 0)
			{
				parts.Add(label + "=" + level);
			}
		}
		catch
		{
		}
	}

	private static string FormatHeroRelatives(Hero hero)
	{
		if (hero == null)
		{
			return "未知";
		}
		List<string> parts = new List<string>();
		AddRelative(parts, "父亲", hero.Father);
		AddRelative(parts, "母亲", hero.Mother);
		AddRelative(parts, "配偶", hero.Spouse);
		AddHeroCollection(parts, "子女", hero.Children, 8);
		List<Hero> siblings = new List<Hero>();
		try
		{
			if (hero.Father != null)
			{
				siblings.AddRange(hero.Father.Children.Where((Hero x) => x != null && x != hero));
			}
			if (hero.Mother != null)
			{
				siblings.AddRange(hero.Mother.Children.Where((Hero x) => x != null && x != hero));
			}
			siblings = siblings.Distinct().ToList();
		}
		catch
		{
		}
		AddHeroCollection(parts, "兄弟姐妹", siblings, 8);
		return parts.Count == 0 ? "未记录" : string.Join("；", parts);
	}

	private static void AddRelative(List<string> parts, string label, Hero hero)
	{
		if (hero != null)
		{
			parts.Add(label + "：" + SafeName(hero.Name, hero.StringId));
		}
	}

	private static void AddHeroCollection(List<string> parts, string label, IEnumerable<Hero> heroes, int cap)
	{
		List<string> names = (heroes ?? Enumerable.Empty<Hero>()).Where((Hero x) => x != null).Select((Hero x) => SafeName(x.Name, x.StringId)).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(cap).ToList();
		if (names.Count > 0)
		{
			parts.Add(label + "：" + string.Join("、", names));
		}
	}

	private static string FormatHeroLocation(Hero hero)
	{
		if (hero == null)
		{
			return "未知";
		}
		try
		{
			if (hero.CurrentSettlement != null)
			{
				return FormatSettlementNameWithType(hero.CurrentSettlement);
			}
			if (hero.PartyBelongedTo != null)
			{
				MobileParty party = hero.PartyBelongedTo;
				if (party.CurrentSettlement != null)
				{
					return FormatSettlementNameWithType(party.CurrentSettlement) + "（定居点内）";
				}
				return FormatMobilePartyMapLocation(party);
			}
			if (hero.IsPrisoner && hero.PartyBelongedToAsPrisoner != null)
			{
				PartyBase holder = hero.PartyBelongedToAsPrisoner;
				if (holder.IsSettlement && holder.Settlement != null)
				{
					return FormatSettlementNameWithType(holder.Settlement) + "（囚禁中）";
				}
				if (holder.IsMobile && holder.MobileParty != null)
				{
					return FormatMobilePartyMapLocation(holder.MobileParty) + "（囚禁于该队伍）";
				}
			}
			if (hero.HomeSettlement != null)
			{
				return FormatSettlementNameWithType(hero.HomeSettlement);
			}
		}
		catch
		{
		}
		return "未知";
	}

	private static string FormatHeroStatus(Hero hero)
	{
		if (hero == null)
		{
			return "未知";
		}
		List<string> parts = new List<string>();
		try
		{
			if (!hero.IsAlive)
			{
				parts.Add("已死亡");
			}
			if (hero.IsPrisoner)
			{
				parts.Add("被俘虏" + FormatPrisonerHolder(hero));
			}
			if (hero.PartyBelongedTo != null)
			{
				MobileParty party = hero.PartyBelongedTo;
				if (party.CurrentSettlement != null)
				{
					parts.Add("在 " + FormatSettlementNameWithType(party.CurrentSettlement));
				}
				else if (party.TargetSettlement != null)
				{
					parts.Add("正在前往 " + FormatSettlementNameWithType(party.TargetSettlement));
				}
				else
				{
					string nearest = FormatNearestSettlementForParty(party);
					if (!string.IsNullOrWhiteSpace(nearest))
					{
						parts.Add("在 " + nearest + " 附近" + FormatMobilePartyMapTerrainSuffix(party) + "活动");
					}
				}
				if (party.Army != null)
				{
					parts.Add("隶属军团：" + SafeName(party.Army.Name, "军团"));
				}
				parts.Add("队伍行为：" + party.DefaultBehavior);
			}
		}
		catch
		{
		}
		return parts.Count == 0 ? "无特殊状态" : string.Join("；", parts);
	}

	private static string FormatHeroPrisonerFlag(Hero hero)
	{
		if (hero == null)
		{
			return "未知";
		}
		try
		{
			if (!hero.IsPrisoner)
			{
				return "false";
			}
			string holder = FormatPrisonerHolder(hero);
			return string.IsNullOrWhiteSpace(holder) ? "true" : ("true" + holder);
		}
		catch
		{
			return "未知";
		}
	}

	private static string FormatMobilePartyMapLocation(MobileParty party)
	{
		if (party == null)
		{
			return "未知";
		}
		try
		{
			if (party.CurrentSettlement != null)
			{
				return FormatSettlementNameWithType(party.CurrentSettlement) + "（定居点内）";
			}
			if (party.BesiegedSettlement != null)
			{
				return FormatSettlementNameWithType(party.BesiegedSettlement) + "外围（围攻相关）";
			}
			string nearest = FormatNearestSettlementForParty(party);
			string target = party.TargetSettlement == null ? "" : FormatSettlementNameWithType(party.TargetSettlement);
			string terrainSuffix = FormatMobilePartyMapTerrainSuffix(party);
			if (!string.IsNullOrWhiteSpace(nearest) && !string.IsNullOrWhiteSpace(target))
			{
				return "大地图，当前位置：" + nearest + "附近" + terrainSuffix + "；正在前往 " + target;
			}
			if (!string.IsNullOrWhiteSpace(nearest))
			{
				return "大地图，当前位置：" + nearest + "附近" + terrainSuffix;
			}
			if (!string.IsNullOrWhiteSpace(target))
			{
				string terrainLabel = FormatMobilePartyMapTerrainLabel(party);
				return string.IsNullOrWhiteSpace(terrainLabel) ? ("大地图，正在前往 " + target) : ("大地图，当前位置：" + terrainLabel + "；正在前往 " + target);
			}
			if (party.LastVisitedSettlement != null)
			{
				string terrainLabel = FormatMobilePartyMapTerrainLabel(party);
				return string.IsNullOrWhiteSpace(terrainLabel) ? ("大地图，最近离开 " + FormatSettlementNameWithType(party.LastVisitedSettlement)) : ("大地图，最近离开 " + FormatSettlementNameWithType(party.LastVisitedSettlement) + "；当前位置：" + terrainLabel);
			}
			return "大地图，队伍：" + SafeName(party.Name, party.StringId);
		}
		catch
		{
			return "大地图，队伍：" + SafeName(party.Name, party.StringId);
		}
	}

	private static string FormatMobilePartyMapTerrainSuffix(MobileParty party)
	{
		string terrainLabel = FormatMobilePartyMapTerrainLabel(party);
		return string.IsNullOrWhiteSpace(terrainLabel) ? "" : ("的" + terrainLabel);
	}

	private static string FormatMobilePartyMapTerrainLabel(MobileParty party)
	{
		try
		{
			if (MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(party))
			{
				return "海上";
			}
			return MapSeaContextGuard.BuildMobilePartyLandTerrainPromptLabel(party);
		}
		catch
		{
			return "";
		}
	}

	private static string FormatNearestSettlementForParty(MobileParty party)
	{
		try
		{
			if (party == null || !party.Position.IsValid())
			{
				return "";
			}
			Settlement nearest = FindNearestSettlement(party.Position, out var distance);
			if (nearest == null)
			{
				return "";
			}
			if (distance > 0.001f && distance < float.MaxValue)
			{
				return FormatSettlementNameWithType(nearest, distance);
			}
			return FormatSettlementNameWithType(nearest);
		}
		catch
		{
			return "";
		}
	}

	private static string FormatSettlementNameWithType(Settlement settlement, float distance = -1f)
	{
		if (settlement == null)
		{
			return "未知";
		}
		string name = SafeName(settlement.Name, settlement.StringId);
		List<string> suffixParts = new List<string>();
		string type = FormatSettlementType(settlement);
		if (!string.IsNullOrWhiteSpace(type))
		{
			suffixParts.Add(type);
		}
		if (distance > 0.001f && distance < float.MaxValue)
		{
			suffixParts.Add("约 " + distance.ToString("0.0", CultureInfo.InvariantCulture) + " 公里");
		}
		return suffixParts.Count == 0 ? name : (name + "（" + string.Join("，", suffixParts) + "）");
	}

	private static string FormatSettlementType(Settlement settlement)
	{
		try
		{
			if (settlement == null)
			{
				return "";
			}
			if (settlement.IsVillage)
			{
				return "村庄";
			}
			if (settlement.IsTown)
			{
				return "城镇";
			}
			if (settlement.IsCastle)
			{
				return "城堡";
			}
			if (settlement.IsHideout)
			{
				return "藏身处";
			}
			if (settlement.IsFortification)
			{
				return "要塞";
			}
		}
		catch
		{
		}
		return "定居点";
	}

	private static Settlement FindNearestSettlement(CampaignVec2 position, out float distance)
	{
		distance = float.MaxValue;
		Settlement nearest = null;
		try
		{
			if (!position.IsValid())
			{
				return null;
			}
			Vec2 origin = position.ToVec2();
			foreach (Settlement settlement in Settlement.All ?? Enumerable.Empty<Settlement>())
			{
				if (settlement == null || settlement.IsHideout)
				{
					continue;
				}
				string name = (settlement.Name?.ToString() ?? "").Trim();
				if (string.IsNullOrWhiteSpace(name))
				{
					continue;
				}
				Vec2 target = settlement.GatePosition.ToVec2();
				float dx = target.x - origin.x;
				float dy = target.y - origin.y;
				float d2 = dx * dx + dy * dy;
				if (d2 < distance)
				{
					distance = d2;
					nearest = settlement;
				}
			}
			if (nearest != null && distance < float.MaxValue)
			{
				distance = (float)Math.Sqrt(distance);
			}
		}
		catch
		{
			distance = float.MaxValue;
			nearest = null;
		}
		return nearest;
	}

	private static string FormatPrisonerHolder(Hero hero)
	{
		try
		{
			PartyBase holder = hero?.PartyBelongedToAsPrisoner;
			if (holder == null)
			{
				return "";
			}
			if (holder.IsSettlement && holder.Settlement != null)
			{
				return "，关押于 " + FormatSettlementNameWithType(holder.Settlement);
			}
			if (holder.IsMobile && holder.MobileParty != null)
			{
				return "，由 " + SafeName(holder.MobileParty.Name, holder.MobileParty.StringId) + " 控制";
			}
		}
		catch
		{
		}
		return "";
	}

	private static Kingdom ResolveCurrentActiveRulerKingdom(Hero hero)
	{
		if (hero == null)
		{
			return null;
		}
		try
		{
			Kingdom directKingdom = hero.Clan?.Kingdom ?? hero.MapFaction as Kingdom;
			if (directKingdom != null && !directKingdom.IsEliminated && directKingdom.Leader == hero && hero.IsAlive)
			{
				return directKingdom;
			}
			foreach (Kingdom kingdom in (IEnumerable<Kingdom>)Kingdom.All ?? Enumerable.Empty<Kingdom>())
			{
				if (kingdom != null && !kingdom.IsEliminated && kingdom.Leader == hero && hero.IsAlive)
				{
					return kingdom;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static string FormatHeroOccupation(Hero hero)
	{
		if (hero == null)
		{
			return "未知";
		}
		List<string> parts = new List<string>();
		try
		{
			Kingdom rulerKingdom = ResolveCurrentActiveRulerKingdom(hero);
			if (rulerKingdom != null)
			{
				string rulerTitle = SafeTextOrEmpty(rulerKingdom.EncyclopediaRulerTitle);
				string rulerKingdomName = SafeTextOrEmpty(rulerKingdom.Name);
				if (!string.IsNullOrWhiteSpace(rulerTitle))
				{
					parts.Add(string.IsNullOrWhiteSpace(rulerKingdomName) ? rulerTitle : (rulerTitle + "（" + rulerKingdomName + "）"));
				}
				parts.Add("王国领袖");
			}
			else if (hero.IsKingdomLeader)
			{
				parts.Add("国王/王国领袖");
			}
			if (hero.Clan != null && hero.Clan.Leader == hero)
			{
				parts.Add("家族族长");
			}
			if (hero.IsLord)
			{
				parts.Add("领主");
			}
			if (hero.IsWanderer)
			{
				parts.Add("流浪者");
			}
			if (hero.IsNotable)
			{
				parts.Add("名人/地方要人");
			}
			parts.Add(hero.Occupation.ToString());
		}
		catch
		{
		}
		return parts.Count == 0 ? "未知" : string.Join("，", parts.Distinct(StringComparer.OrdinalIgnoreCase));
	}

	private static string FormatSettlementKingdom(Settlement settlement)
	{
		try
		{
			return SafeName(settlement?.OwnerClan?.Kingdom?.Name, SafeName(settlement?.MapFaction?.Name, "无"));
		}
		catch
		{
			return "未知";
		}
	}

	private static string FormatSettlementStrength(Settlement settlement)
	{
		if (settlement == null)
		{
			return "未知";
		}
		List<string> parts = new List<string>();
		try
		{
			parts.Add("民兵 " + FormatFloat(settlement.Militia));
			if (settlement.Party != null)
			{
				parts.Add("驻守/成员 " + settlement.Party.NumberOfAllMembers);
			}
			if (settlement.Town?.GarrisonParty != null)
			{
				parts.Add("驻军 " + settlement.Town.GarrisonParty.Party.NumberOfAllMembers);
			}
		}
		catch
		{
		}
		return parts.Count == 0 ? "未知" : string.Join("，", parts);
	}

	private static string FormatSettlementProsperity(Settlement settlement)
	{
		try
		{
			if (settlement?.Town != null)
			{
				return FormatFloat(settlement.Town.Prosperity);
			}
			if (settlement?.Village != null)
			{
				return "村庄炉户/繁荣参考 " + FormatFloat(settlement.Village.Hearth);
			}
		}
		catch
		{
		}
		return "未知";
	}

	private static string FormatSettlementPopulation(Settlement settlement)
	{
		try
		{
			if (settlement?.Village != null)
			{
				return "炉户 " + FormatFloat(settlement.Village.Hearth);
			}
		}
		catch
		{
		}
		return "无直接人口字段";
	}

	private static string FormatSettlementLoyalty(Settlement settlement)
	{
		try
		{
			if (settlement?.Town != null)
			{
				return FormatFloat(settlement.Town.Loyalty);
			}
		}
		catch
		{
		}
		return "未知";
	}

	private static string FormatBoundVillages(Settlement settlement)
	{
		try
		{
			List<string> names = (((IEnumerable<Village>)settlement?.BoundVillages) ?? Enumerable.Empty<Village>()).Where((Village x) => x?.Settlement != null).Select((Village x) => FormatSettlementNameWithType(x.Settlement)).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToList();
			return names.Count == 0 ? "无" : string.Join("、", names);
		}
		catch
		{
			return "未知";
		}
	}

	private static string FormatSettlementStatus(Settlement settlement)
	{
		if (settlement == null)
		{
			return "未知";
		}
		List<string> parts = new List<string>();
		try
		{
			if (settlement.IsUnderSiege)
			{
				parts.Add("被围攻");
			}
			if (settlement.Village != null && settlement.Village.VillageState != Village.VillageStates.Normal)
			{
				parts.Add("村庄状态：" + settlement.Village.VillageState);
			}
			if (settlement.Town != null)
			{
				parts.Add("治安：" + FormatFloat(settlement.Town.Security));
			}
		}
		catch
		{
		}
		return parts.Count == 0 ? "无特殊状态" : string.Join("；", parts);
	}

	private static string FormatClanFiefs(Clan clan, int cap = MainPromptClanFiefCap)
	{
		try
		{
			List<string> names = (((IEnumerable<Town>)clan?.Fiefs) ?? Enumerable.Empty<Town>()).Where((Town x) => x?.Settlement != null).Select((Town x) => FormatSettlementNameWithType(x.Settlement)).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(Math.Max(1, cap)).ToList();
			int total = 0;
			try
			{
				total = (((IEnumerable<Town>)clan?.Fiefs) ?? Enumerable.Empty<Town>()).Count((Town x) => x?.Settlement != null);
			}
			catch
			{
				total = names.Count;
			}
			if (names.Count == 0)
			{
				return "无";
			}
			return total > names.Count ? (string.Join("、", names) + "等，共" + total.ToString(CultureInfo.InvariantCulture) + "处") : string.Join("、", names);
		}
		catch
		{
			return "未知";
		}
	}

	private static string FormatKingdomClans(Kingdom kingdom, int cap = MainPromptKingdomClanCap)
	{
		try
		{
			List<Clan> clans = (((IEnumerable<Clan>)kingdom?.Clans) ?? Enumerable.Empty<Clan>()).Where((Clan x) => x != null && !x.IsEliminated).ToList();
			List<string> names = clans.Select((Clan x) => SafeName(x.Name, x.StringId)).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(Math.Max(1, cap)).ToList();
			if (names.Count == 0)
			{
				return "无";
			}
			return clans.Count > names.Count ? (string.Join("、", names) + "等，共" + clans.Count.ToString(CultureInfo.InvariantCulture) + "个家族") : string.Join("、", names);
		}
		catch
		{
			return "未知";
		}
	}

	private static string FormatKingdomEncyclopediaBackground(Kingdom kingdom)
	{
		if (kingdom == null || !MyBehavior.IsModCreatedRebelKingdomForExternal(kingdom))
		{
			return "";
		}
		try
		{
			string text = (kingdom.EncyclopediaText?.ToString() ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return "";
			}
			text = Regex.Replace(text, @"\s+", " ").Trim();
			if (text.Length > MainPromptKingdomEncyclopediaTextCap)
			{
				text = text.Substring(0, MainPromptKingdomEncyclopediaTextCap).TrimEnd() + "...";
			}
			return text;
		}
		catch
		{
			return "";
		}
	}

	private static string FormatKingdomSettlementSummary(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return "未知";
		}
		try
		{
			List<Settlement> settlements = ((IEnumerable<Settlement>)Settlement.All ?? Enumerable.Empty<Settlement>())
				.Where((Settlement x) => x != null && x.MapFaction == kingdom && (x.IsTown || x.IsCastle || x.IsVillage))
				.OrderBy((Settlement x) => x.IsTown ? 0 : (x.IsCastle ? 1 : 2))
				.ThenBy((Settlement x) => x.Name?.ToString() ?? "", StringComparer.OrdinalIgnoreCase)
				.ToList();
			if (settlements.Count == 0)
			{
				return "未发现归属该王国的城镇、城堡或村庄。";
			}
			List<string> sampleNames = settlements.Take(5).Select((Settlement x) => SafeName(x.Name, "未知")).Where((string x) => !string.IsNullOrWhiteSpace(x) && x != "未知").Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			int townCount = settlements.Count((Settlement x) => x.IsTown);
			int castleCount = settlements.Count((Settlement x) => x.IsCastle);
			int villageCount = settlements.Count((Settlement x) => x.IsVillage);
			string sampleText = sampleNames.Count == 0 ? "若干定居点" : string.Join("、", sampleNames);
			if (settlements.Count > sampleNames.Count)
			{
				sampleText += "等";
			}
			return "此王国的定居点拥有" + sampleText + "；" + townCount.ToString(CultureInfo.InvariantCulture) + "个城镇，" + castleCount.ToString(CultureInfo.InvariantCulture) + "个城堡，" + villageCount.ToString(CultureInfo.InvariantCulture) + "个村庄。";
		}
		catch
		{
			return "未知";
		}
	}

	private static string FormatKingdomStatus(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return "未知";
		}
		List<string> parts = new List<string>();
		try
		{
			parts.Add("是否灭亡(IsEliminated)：" + FormatEliminatedStatus(kingdom.IsEliminated));
			List<string> wars = Kingdom.All.Where((Kingdom x) => x != null && x != kingdom && !x.IsEliminated && kingdom.IsAtWarWith(x)).Select((Kingdom x) => SafeName(x.Name, x.StringId)).ToList();
			if (wars.Count > 0)
			{
				parts.Add("战争对象：" + string.Join("、", wars));
			}
			List<string> allies = Kingdom.All.Where((Kingdom x) => x != null && x != kingdom && !x.IsEliminated && IsAlly(kingdom, x)).Select((Kingdom x) => SafeName(x.Name, x.StringId)).ToList();
			if (allies.Count > 0)
			{
				parts.Add("联盟对象：" + string.Join("、", allies));
			}
			List<string> trades = Kingdom.All.Where((Kingdom x) => x != null && x != kingdom && !x.IsEliminated && HasTradeAgreement(kingdom, x)).Select((Kingdom x) => SafeName(x.Name, x.StringId)).ToList();
			if (trades.Count > 0)
			{
				parts.Add("贸易协定：" + string.Join("、", trades));
			}
			if (kingdom.IsEliminated)
			{
				parts.Add("已灭亡/无效王国");
			}
		}
		catch
		{
		}
		return parts.Count == 0 ? "无已知战争、联盟或贸易协定" : string.Join("；", parts);
	}

	private static bool IsAlly(Kingdom kingdom, Kingdom other)
	{
		try
		{
			return kingdom != null && other != null && kingdom.IsAllyWith(other);
		}
		catch
		{
			return false;
		}
	}

	private static bool HasTradeAgreement(Kingdom kingdom, Kingdom other)
	{
		try
		{
			ITradeAgreementsCampaignBehavior behavior = Campaign.Current?.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
			if (behavior == null || kingdom == null || other == null)
			{
				return false;
			}
			return BannerlordApiCompat.HasTradeAgreement(behavior, kingdom, other);
		}
		catch
		{
			return false;
		}
	}

	private static string FormatHeroList(IEnumerable<Hero> heroes, int cap)
	{
		try
		{
			List<string> names = (heroes ?? Enumerable.Empty<Hero>()).Where((Hero x) => x != null).Select((Hero x) => SafeName(x.Name, x.StringId)).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(cap).ToList();
			return names.Count == 0 ? "无" : string.Join("、", names);
		}
		catch
		{
			return "未知";
		}
	}

	private static List<VisiblePartyCandidate> BuildVisiblePartyCandidates(Hero contextHero)
	{
		Dictionary<string, VisiblePartyCandidate> selected = new Dictionary<string, VisiblePartyCandidate>(StringComparer.OrdinalIgnoreCase);
		try
		{
			Hero playerHero = Hero.MainHero;
			List<MobileParty> observers = new List<MobileParty>();
			AddObserverParty(observers, MobileParty.MainParty);
			AddObserverParty(observers, contextHero?.PartyBelongedTo);
			if (observers.Count == 0)
			{
				return new List<VisiblePartyCandidate>();
			}
			foreach (MobileParty observer in observers)
			{
				foreach (MobileParty party in MobileParty.All ?? Enumerable.Empty<MobileParty>())
				{
					if (!IsVisiblePartyCandidate(party, observer))
					{
						continue;
					}
					float distance = GetPartyDistance(observer, party);
					bool visibleFromPlayerMap = observer == MobileParty.MainParty && IsPartyVisibleToPlayer(party);
					if (!visibleFromPlayerMap && distance > GetObserverPartyRange(observer))
					{
						continue;
					}
					string id = SafeStringId(party.StringId);
					if (string.IsNullOrWhiteSpace(id) || string.Equals(id, "unknown", StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					VisiblePartyCandidate candidate = new VisiblePartyCandidate
					{
						Party = party,
						Id = id,
						Name = SafeName(party.Name, id),
						Count = GetPartyMemberCount(party),
						Affiliation = FormatPartyAffiliation(party),
						RelationToContextHero = FormatVisiblePartyRelationToHero(party, contextHero),
						RelationToPlayer = FormatVisiblePartyRelationToHero(party, playerHero),
						ShipInfo = MapSeaContextGuard.BuildMobilePartyShipPromptText(party),
						Direction = FormatDirection(observer.Position, party.Position),
						Distance = distance
					};
					if (!selected.TryGetValue(id, out VisiblePartyCandidate existing) || candidate.Distance < existing.Distance)
					{
						selected[id] = candidate;
					}
				}
			}
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("WorldEntityRetrieval", "visible_party_candidates failed: " + ex.Message);
			}
			catch
			{
			}
		}
		return selected.Values.OrderBy((VisiblePartyCandidate x) => x.Distance).ThenBy((VisiblePartyCandidate x) => x.Name, StringComparer.OrdinalIgnoreCase).Take(MaxVisiblePartyCandidates).ToList();
	}

	private static void AddObserverParty(List<MobileParty> observers, MobileParty party)
	{
		if (observers == null || !IsPartyUsableForVisibility(party))
		{
			return;
		}
		if (!observers.Any((MobileParty x) => x == party))
		{
			observers.Add(party);
		}
	}

	private static bool IsVisiblePartyCandidate(MobileParty party, MobileParty observer)
	{
		try
		{
			if (!IsPartyUsableForVisibility(party) || !IsPartyUsableForVisibility(observer) || party == observer || party == MobileParty.MainParty || party.IsMainParty)
			{
				return false;
			}
			if (party.IsGarrison || party.IsMilitia || party.CurrentSettlement != null)
			{
				return false;
			}
			if (party.MapEvent != null && !party.MapEvent.IsFinalized)
			{
				return false;
			}
			return !string.IsNullOrWhiteSpace(party.StringId);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPartyUsableForVisibility(MobileParty party)
	{
		try
		{
			return party != null && party.IsActive && party.Party != null;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPartyVisibleToPlayer(MobileParty party)
	{
		try
		{
			return party?.IsVisible == true;
		}
		catch
		{
			return false;
		}
	}

	private static string FormatVisiblePartyRelationToHero(MobileParty party, Hero referenceHero)
	{
		if (party == null || referenceHero == null)
		{
			return "";
		}
		try
		{
			Hero partyHero = GetVisiblePartyHero(party);
			if (IsHeroParty(referenceHero, party) || IsSameHero(referenceHero, partyHero))
			{
				return "本人部队";
			}
			string stance = FormatFactionRelationBand(ResolveHeroPromptFaction(referenceHero), ResolvePartyPromptFaction(party));
			string personal = FormatPartyHeroPersonalRelationBand(referenceHero, partyHero);
			if (!string.IsNullOrWhiteSpace(personal))
			{
				return string.IsNullOrWhiteSpace(stance) ? ("个人" + personal) : (stance + "，个人" + personal);
			}
			return string.IsNullOrWhiteSpace(stance) ? "未知" : stance;
		}
		catch
		{
			return "";
		}
	}

	private static Hero GetVisiblePartyHero(MobileParty party)
	{
		try
		{
			return party?.LeaderHero ?? party?.Owner;
		}
		catch
		{
			return null;
		}
	}

	private static bool IsHeroParty(Hero hero, MobileParty party)
	{
		try
		{
			return hero != null && party != null && hero.PartyBelongedTo == party;
		}
		catch
		{
			return false;
		}
	}

	private static string FormatPartyHeroPersonalRelationBand(Hero referenceHero, Hero partyHero)
	{
		try
		{
			if (referenceHero == null || partyHero == null || IsSameHero(referenceHero, partyHero))
			{
				return "";
			}
			return TryGetRelationValueForPrompt(referenceHero, partyHero, out int relation) ? FormatRelationBand(relation) : "";
		}
		catch
		{
			return "";
		}
	}

	private static IFaction ResolveHeroPromptFaction(Hero hero)
	{
		try
		{
			if (hero?.MapFaction != null)
			{
				return hero.MapFaction;
			}
			if (hero?.Clan?.Kingdom != null)
			{
				return hero.Clan.Kingdom;
			}
			return hero?.Clan;
		}
		catch
		{
			return null;
		}
	}

	private static IFaction ResolvePartyPromptFaction(MobileParty party)
	{
		try
		{
			if (party?.MapFaction != null)
			{
				return party.MapFaction;
			}
			if (party?.ActualClan?.Kingdom != null)
			{
				return party.ActualClan.Kingdom;
			}
			if (party?.ActualClan != null)
			{
				return party.ActualClan;
			}
			return ResolveHeroPromptFaction(GetVisiblePartyHero(party));
		}
		catch
		{
			return null;
		}
	}

	private static string FormatFactionRelationBand(IFaction referenceFaction, IFaction partyFaction)
	{
		try
		{
			if (referenceFaction == null || partyFaction == null)
			{
				return "";
			}
			if (IsSameFaction(referenceFaction, partyFaction))
			{
				return "友好";
			}
			if (AreFactionsAtWar(referenceFaction, partyFaction))
			{
				return "敌对";
			}
			if (AreFactionKingdomsAllied(referenceFaction, partyFaction))
			{
				return "友好";
			}
			return "中立";
		}
		catch
		{
			return "";
		}
	}

	private static bool IsSameFaction(IFaction first, IFaction second)
	{
		try
		{
			if (first == null || second == null)
			{
				return false;
			}
			if (ReferenceEquals(first, second) || first == second)
			{
				return true;
			}
			string firstId = (first.StringId ?? "").Trim();
			string secondId = (second.StringId ?? "").Trim();
			return !string.IsNullOrWhiteSpace(firstId) && string.Equals(firstId, secondId, StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	private static bool AreFactionsAtWar(IFaction first, IFaction second)
	{
		try
		{
			return first != null && second != null && !IsSameFaction(first, second) && first.IsAtWarWith(second);
		}
		catch
		{
			try
			{
				return second != null && first != null && !IsSameFaction(first, second) && second.IsAtWarWith(first);
			}
			catch
			{
				return false;
			}
		}
	}

	private static bool AreFactionKingdomsAllied(IFaction first, IFaction second)
	{
		try
		{
			Kingdom firstKingdom = ResolveFactionKingdom(first);
			Kingdom secondKingdom = ResolveFactionKingdom(second);
			return firstKingdom != null && secondKingdom != null && firstKingdom != secondKingdom && IsAlly(firstKingdom, secondKingdom);
		}
		catch
		{
			return false;
		}
	}

	private static Kingdom ResolveFactionKingdom(IFaction faction)
	{
		try
		{
			if (faction is Kingdom kingdom)
			{
				return kingdom;
			}
			if (faction is Clan clan)
			{
				return clan.Kingdom;
			}
		}
		catch
		{
		}
		return null;
	}

	private static float GetObserverPartyRange(MobileParty observer)
	{
		try
		{
			return Math.Max(VisiblePartyMinRange, (observer?.SeeingRange ?? 0f) * VisiblePartyRangeMultiplier);
		}
		catch
		{
			return VisiblePartyMinRange;
		}
	}

	private static float GetPartyDistance(MobileParty observer, MobileParty party)
	{
		try
		{
			if (observer == null || party == null)
			{
				return float.MaxValue;
			}
			return observer.Position.Distance(party.Position);
		}
		catch
		{
			return float.MaxValue;
		}
	}

	private static int GetPartyMemberCount(MobileParty party)
	{
		try
		{
			return Math.Max(0, party?.MemberRoster?.TotalManCount ?? 0);
		}
		catch
		{
			return 0;
		}
	}

	private static string FormatPartyAffiliation(MobileParty party)
	{
		try
		{
			List<string> parts = new List<string>();
			if (party?.HomeSettlement != null)
			{
				parts.Add("村庄/据点：" + FormatSettlementNameWithType(party.HomeSettlement));
			}
			if (party?.MapFaction != null)
			{
				parts.Add("王国/阵营：" + SafeName(party.MapFaction.Name, party.MapFaction.StringId));
			}
			Hero owner = party?.LeaderHero ?? party?.Owner;
			if (owner != null)
			{
				parts.Add("要人：" + SafeName(owner.Name, owner.StringId));
			}
			return parts.Count == 0 ? "未知" : string.Join("；", parts);
		}
		catch
		{
			return "未知";
		}
	}

	private static string FormatDirection(CampaignVec2 from, CampaignVec2 to)
	{
		try
		{
			float dx = to.X - from.X;
			float dy = to.Y - from.Y;
			if (Math.Abs(dx) < 0.001f && Math.Abs(dy) < 0.001f)
			{
				return "当前位置";
			}
			double degrees = Math.Atan2(dy, dx) * 180.0 / Math.PI;
			if (degrees < 0.0)
			{
				degrees += 360.0;
			}
			string[] directions = { "东", "东北", "北", "西北", "西", "西南", "南", "东南" };
			int index = ((int)Math.Round(degrees / 45.0)) % directions.Length;
			return directions[index];
		}
		catch
		{
			return "未知";
		}
	}

	private static string FormatDistance(float distance)
	{
		if (float.IsNaN(distance) || float.IsInfinity(distance) || distance >= float.MaxValue * 0.5f)
		{
			return "未知";
		}
		return distance.ToString("0.0", CultureInfo.InvariantCulture);
	}

	private static string SafeName(TextObject textObject, string fallback)
	{
		try
		{
			string text = textObject?.ToString();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.Trim();
			}
		}
		catch
		{
		}
		return string.IsNullOrWhiteSpace(fallback) ? "未知" : fallback.Trim();
	}

	private static string SafeTextOrEmpty(TextObject textObject)
	{
		try
		{
			string text = textObject?.ToString();
			return string.IsNullOrWhiteSpace(text) ? "" : text.Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string SafeStringId(string stringId)
	{
		string text = (stringId ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? "unknown" : text;
	}

	private static string FormatAge(Hero hero)
	{
		try
		{
			return hero == null ? "未知" : Math.Floor(hero.Age).ToString(CultureInfo.InvariantCulture);
		}
		catch
		{
			return "未知";
		}
	}

	private static string FormatGender(Hero hero)
	{
		if (hero == null)
		{
			return "未知";
		}
		return hero.IsFemale ? "女" : "男";
	}

	private static string FormatBool(bool value)
	{
		return value ? "true" : "false";
	}

	private static string FormatEliminatedStatus(bool? value)
	{
		if (!value.HasValue)
		{
			return "未知";
		}
		return value.Value ? "true（已灭亡）" : "false（未灭亡）";
	}

	private static string FormatFloat(float? value)
	{
		if (!value.HasValue)
		{
			return "未知";
		}
		return value.Value.ToString("0.#", CultureInfo.InvariantCulture);
	}

	private static string FormatInt(int? value)
	{
		if (!value.HasValue)
		{
			return "未知";
		}
		return value.Value.ToString(CultureInfo.InvariantCulture);
	}

	private static string FormatScore(float value)
	{
		return value.ToString("0.00", CultureInfo.InvariantCulture);
	}
}
