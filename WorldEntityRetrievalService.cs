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
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AnimusForge;

public sealed class MentionedWorldEntities
{
	public List<string> Heroes = new List<string>();

	public List<string> Settlements = new List<string>();

	public List<string> Clans = new List<string>();

	public List<string> Kingdoms = new List<string>();

	public List<string> Items = new List<string>();

	public List<string> Troops = new List<string>();

	public List<string> Terms = new List<string>();

	public bool IsEmpty
	{
		get
		{
			return IsEmptyList(Heroes) && IsEmptyList(Settlements) && IsEmptyList(Clans) && IsEmptyList(Kingdoms) && IsEmptyList(Items) && IsEmptyList(Troops) && IsEmptyList(Terms);
		}
	}

	public MentionedWorldEntities Clone()
	{
		return new MentionedWorldEntities
		{
			Heroes = new List<string>(Heroes ?? new List<string>()),
			Settlements = new List<string>(Settlements ?? new List<string>()),
			Clans = new List<string>(Clans ?? new List<string>()),
			Kingdoms = new List<string>(Kingdoms ?? new List<string>()),
			Items = new List<string>(Items ?? new List<string>()),
			Troops = new List<string>(Troops ?? new List<string>()),
			Terms = new List<string>(Terms ?? new List<string>())
		};
	}

	public void Merge(MentionedWorldEntities other)
	{
		if (other == null)
		{
			return;
		}
		MergeList(Heroes, other.Heroes);
		MergeList(Settlements, other.Settlements);
		MergeList(Clans, other.Clans);
		MergeList(Kingdoms, other.Kingdoms);
		MergeList(Items, other.Items);
		MergeList(Troops, other.Troops);
		MergeList(Terms, other.Terms);
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

	private const int MaxMatchesPerMention = 3;

	private const int DefaultMaxInjectedEntities = 6;

	private const int MaxInjectedEntitiesHardCap = 20;

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
	}

	private sealed class VisiblePartyCandidate
	{
		public MobileParty Party;

		public string Id;

		public string Name;

		public int Count;

		public string Affiliation;

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

	public static WorldEntityPromptContext BuildPromptContext(MentionedWorldEntities mentions, string playerDisplayName, Hero contextHero = null, bool includeResidentKingdoms = false, IEnumerable<string> activeRuleIds = null)
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
			List<string> allMentions = BuildMergedMentionList(mentions);
			HashSet<string> activeRuleIdSet = BuildActiveRuleIdSet(activeRuleIds);
			bool searchItemsFromTerms = ShouldSearchItemEntitiesFromTerms(activeRuleIdSet);
			bool searchTroopsFromTerms = ShouldSearchTroopEntitiesFromTerms(activeRuleIdSet);
			List<string> itemMentions = BuildTypedMentionList(mentions?.Items, searchItemsFromTerms ? mentions?.Terms : null);
			List<string> troopMentions = BuildTypedMentionList(mentions?.Troops, searchTroopsFromTerms ? mentions?.Terms : null);
			string startDetail = "mentions=" + allMentions.Count + " itemMentions=" + itemMentions.Count + " troopMentions=" + troopMentions.Count + " visibleParties=" + visibleParties.Count + " contextHero=" + (contextHero?.StringId ?? "");
			FreezeWatchdog.Mark("WorldEntityRetrieval.start", startDetail, immediate: true);
			Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] start mentions=" + allMentions.Count + " heroes=" + CountList(mentions?.Heroes) + " settlements=" + CountList(mentions?.Settlements) + " clans=" + CountList(mentions?.Clans) + " kingdoms=" + CountList(mentions?.Kingdoms) + " items=" + CountList(mentions?.Items) + " troops=" + CountList(mentions?.Troops) + " terms=" + CountList(mentions?.Terms) + " itemMentions=" + itemMentions.Count + " troopMentions=" + troopMentions.Count + " visibleParties=" + visibleParties.Count + " contextHero=" + (contextHero?.StringId ?? "") + " includeResidentKingdoms=" + includeResidentKingdoms + " activeRules=" + FormatMentionsForLog(activeRuleIdSet) + " " + FormatBudgetForLog(budget));
			List<EntityMatch<Hero>> heroes = new List<EntityMatch<Hero>>();
			List<EntityMatch<Settlement>> settlements = new List<EntityMatch<Settlement>>();
			List<EntityMatch<Clan>> clans = new List<EntityMatch<Clan>>();
			List<EntityMatch<Kingdom>> kingdoms = new List<EntityMatch<Kingdom>>();
			List<EntityMatch<ItemObject>> items = new List<EntityMatch<ItemObject>>();
			List<EntityMatch<CharacterObject>> troops = new List<EntityMatch<CharacterObject>>();
			if (allMentions.Count > 0 || itemMentions.Count > 0 || troopMentions.Count > 0)
			{
				Stopwatch stageSw = Stopwatch.StartNew();
				int maxInjectedEntities = GetMaxInjectedEntitiesFromSettings();
				List<Hero> heroCandidates = new List<Hero>();
				List<Settlement> settlementCandidates = new List<Settlement>();
				List<Clan> clanCandidates = new List<Clan>();
				List<Kingdom> kingdomCandidates = new List<Kingdom>();
				List<ItemObject> itemCandidates = new List<ItemObject>();
				List<CharacterObject> troopCandidates = new List<CharacterObject>();
				if (allMentions.Count > 0)
				{
					heroCandidates = GetHeroCandidates().ToList();
					settlementCandidates = GetSettlementCandidates().ToList();
					clanCandidates = GetClanCandidates().ToList();
					kingdomCandidates = GetKingdomCandidates().ToList();
				}
				if (itemMentions.Count > 0)
				{
					itemCandidates = GetItemCandidates().ToList();
				}
				if (troopMentions.Count > 0)
				{
					troopCandidates = GetTroopCandidates().ToList();
				}
				Logger.Log("WorldEntityRetrieval", "mentions total=" + allMentions.Count + " maxInject=" + maxInjectedEntities + " heroes=" + CountList(mentions?.Heroes) + " settlements=" + CountList(mentions?.Settlements) + " clans=" + CountList(mentions?.Clans) + " kingdoms=" + CountList(mentions?.Kingdoms) + " items=" + CountList(mentions?.Items) + " troops=" + CountList(mentions?.Troops) + " terms=" + CountList(mentions?.Terms) + " itemMentions=" + itemMentions.Count + " troopMentions=" + troopMentions.Count + " visibleParties=" + visibleParties.Count + " candidates hero=" + heroCandidates.Count + " settlement=" + settlementCandidates.Count + " clan=" + clanCandidates.Count + " kingdom=" + kingdomCandidates.Count + " item=" + itemCandidates.Count + " troop=" + troopCandidates.Count + " names=" + FormatMentionsForLog(allMentions) + " itemNames=" + FormatMentionsForLog(itemMentions) + " troopNames=" + FormatMentionsForLog(troopMentions));
				Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] candidates_ready ms=" + Math.Round(stageSw.Elapsed.TotalMilliseconds, 2));
				if (allMentions.Count > 0)
				{
					Dictionary<string, int> mentionPriority = BuildMentionPriority(allMentions);
					if (CanContinueWorldEntityMatch("hero", budget))
					{
						heroes = FindMatches("hero", allMentions, mentionPriority, heroCandidates, GetHeroAliases, (Hero x) => "hero:" + SafeStringId(x?.StringId), (Hero x) => SafeName(x?.Name, x?.StringId ?? "Hero"), budget);
					}
					if (CanContinueWorldEntityMatch("settlement", budget))
					{
						settlements = FindMatches("settlement", allMentions, mentionPriority, settlementCandidates, GetSettlementAliases, (Settlement x) => "settlement:" + SafeStringId(x?.StringId), (Settlement x) => SafeName(x?.Name, x?.StringId ?? "Settlement"), budget);
					}
					if (CanContinueWorldEntityMatch("clan", budget))
					{
						clans = FindMatches("clan", allMentions, mentionPriority, clanCandidates, GetClanAliases, (Clan x) => "clan:" + SafeStringId(x?.StringId), (Clan x) => SafeName(x?.Name, x?.StringId ?? "Clan"), budget);
					}
					if (CanContinueWorldEntityMatch("kingdom", budget))
					{
						kingdoms = FindMatches("kingdom", allMentions, mentionPriority, kingdomCandidates, GetKingdomAliases, (Kingdom x) => "kingdom:" + SafeStringId(x?.StringId), (Kingdom x) => SafeName(x?.Name, x?.StringId ?? "Kingdom"), budget);
					}
				}
				if (itemMentions.Count > 0)
				{
					Dictionary<string, int> itemMentionPriority = BuildMentionPriority(itemMentions);
					if (CanContinueWorldEntityMatch("item", budget))
					{
						items = FindMatches("item", itemMentions, itemMentionPriority, itemCandidates, GetItemAliases, (ItemObject x) => "item:" + SafeStringId(x?.StringId), (ItemObject x) => SafeName(x?.Name, x?.StringId ?? "Item"), budget);
					}
				}
				if (troopMentions.Count > 0)
				{
					Dictionary<string, int> troopMentionPriority = BuildMentionPriority(troopMentions);
					if (CanContinueWorldEntityMatch("troop", budget))
					{
						troops = FindMatches("troop", troopMentions, troopMentionPriority, troopCandidates, GetTroopAliases, (CharacterObject x) => "troop:" + SafeStringId(x?.StringId), (CharacterObject x) => SafeName(x?.Name, x?.StringId ?? "Troop"), budget);
					}
				}
				Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] all_match_done heroMatches=" + heroes.Count + " settlementMatches=" + settlements.Count + " clanMatches=" + clans.Count + " kingdomMatches=" + kingdoms.Count + " itemMatches=" + items.Count + " troopMatches=" + troops.Count + " ms=" + Math.Round(stageSw.Elapsed.TotalMilliseconds, 2) + " hardBudgetExceeded=" + budget.IsHardExceeded);
				stageSw.Restart();
				ApplyGlobalInjectionLimit(maxInjectedEntities, ref heroes, ref settlements, ref clans, ref kingdoms, ref items, ref troops);
				Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] global_limit_done heroMatches=" + heroes.Count + " settlementMatches=" + settlements.Count + " clanMatches=" + clans.Count + " kingdomMatches=" + kingdoms.Count + " itemMatches=" + items.Count + " troopMatches=" + troops.Count + " ms=" + Math.Round(stageSw.Elapsed.TotalMilliseconds, 2));
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
			List<EntityMatch<ItemObject>> postprocessItems = CloneEntityMatches(items);
			List<EntityMatch<CharacterObject>> postprocessTroops = CloneEntityMatches(troops);
			AddResidentEntityMatches(contextHero, includeResidentKingdoms, ref heroes, ref settlements, ref clans, ref kingdoms);
			AddPostprocessResidentEntityMatches(contextHero, ref postprocessHeroes, ref postprocessSettlements, ref postprocessClans, ref postprocessKingdoms);
			Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] resident_done heroMatches=" + heroes.Count + " settlementMatches=" + settlements.Count + " clanMatches=" + clans.Count + " kingdomMatches=" + kingdoms.Count + " itemMatches=" + items.Count + " troopMatches=" + troops.Count + " visibleParties=" + visibleParties.Count + " ms=" + Math.Round(residentSw.Elapsed.TotalMilliseconds, 2));
			int count = heroes.Count + settlements.Count + clans.Count + kingdoms.Count + items.Count + troops.Count + visibleParties.Count;
			if (count <= 0)
			{
				Logger.Log("WorldEntityRetrieval", "no_match mentions=" + FormatMentionsForLog(allMentions));
				return result;
			}
			result.MatchCount = count;
			Stopwatch buildSw = Stopwatch.StartNew();
			Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] build_blocks_start matchCount=" + count);
			FreezeWatchdog.Mark("WorldEntityRetrieval.build_blocks_start", "matchCount=" + count, immediate: true);
			result.MainPromptBlock = BuildMainPromptBlock(playerDisplayName, contextHero, heroes, settlements, clans, kingdoms, items, troops, visibleParties);
			result.PostprocessPromptBlock = BuildPostprocessPromptBlock(postprocessHeroes, postprocessSettlements, postprocessClans, postprocessKingdoms, postprocessItems, postprocessTroops, visibleParties);
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

	private static List<string> BuildMergedMentionList(MentionedWorldEntities mentions)
	{
		List<string> result = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		AddMentionList(result, seen, mentions?.Heroes);
		AddMentionList(result, seen, mentions?.Settlements);
		AddMentionList(result, seen, mentions?.Clans);
		AddMentionList(result, seen, mentions?.Kingdoms);
		AddMentionList(result, seen, mentions?.Terms);
		return result;
	}

	private static List<string> BuildTypedMentionList(IEnumerable<string> primaryValues, IEnumerable<string> ruleGatedTerms)
	{
		List<string> result = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		AddMentionList(result, seen, primaryValues);
		AddMentionList(result, seen, ruleGatedTerms);
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

	private static bool ShouldSearchItemEntitiesFromTerms(HashSet<string> activeRuleIds)
	{
		return ActiveRuleLooksLike(activeRuleIds, "reward", "loan", "debt", "barter", "trade", "exchange", "gift", "item", "goods", "equipment", "asset", "courier", "delivery");
	}

	private static bool ShouldSearchTroopEntitiesFromTerms(HashSet<string> activeRuleIds)
	{
		return ActiveRuleLooksLike(activeRuleIds, "party_transfer", "troop", "unit", "soldier", "prisoner", "captive", "recruit", "worldmap_party_command", "hero_join_party");
	}

	private static bool ActiveRuleLooksLike(HashSet<string> activeRuleIds, params string[] fragments)
	{
		if (activeRuleIds == null || activeRuleIds.Count == 0 || fragments == null || fragments.Length == 0)
		{
			return false;
		}
		foreach (string ruleId in activeRuleIds)
		{
			string text = (ruleId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			foreach (string fragment in fragments)
			{
				string needle = (fragment ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(needle) && text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}
		}
		return false;
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
			if (!string.IsNullOrWhiteSpace(text) && seen.Add(text))
			{
				result.Add(text);
			}
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

	private static void ApplyGlobalInjectionLimit(int maxCount, ref List<EntityMatch<Hero>> heroes, ref List<EntityMatch<Settlement>> settlements, ref List<EntityMatch<Clan>> clans, ref List<EntityMatch<Kingdom>> kingdoms, ref List<EntityMatch<ItemObject>> items, ref List<EntityMatch<CharacterObject>> troops)
	{
		maxCount = ClampMaxInjectedEntities(maxCount);
		List<Tuple<string, string, int, float, string>> ordered = new List<Tuple<string, string, int, float, string>>();
		AddGlobalLimitItems(ordered, "hero", heroes);
		AddGlobalLimitItems(ordered, "settlement", settlements);
		AddGlobalLimitItems(ordered, "clan", clans);
		AddGlobalLimitItems(ordered, "kingdom", kingdoms);
		AddGlobalLimitItems(ordered, "item", items);
		AddGlobalLimitItems(ordered, "troop", troops);
		HashSet<string> keep = new HashSet<string>(ordered.OrderBy((Tuple<string, string, int, float, string> x) => x.Item3).ThenByDescending((Tuple<string, string, int, float, string> x) => x.Item4).ThenBy((Tuple<string, string, int, float, string> x) => x.Item5, StringComparer.OrdinalIgnoreCase).Take(maxCount).Select((Tuple<string, string, int, float, string> x) => x.Item1 + ":" + x.Item2), StringComparer.OrdinalIgnoreCase);
		heroes = FilterGlobalLimitList("hero", heroes, keep);
		settlements = FilterGlobalLimitList("settlement", settlements, keep);
		clans = FilterGlobalLimitList("clan", clans, keep);
		kingdoms = FilterGlobalLimitList("kingdom", kingdoms, keep);
		items = FilterGlobalLimitList("item", items, keep);
		troops = FilterGlobalLimitList("troop", troops, keep);
	}

	private static void AddGlobalLimitItems<T>(List<Tuple<string, string, int, float, string>> target, string type, IEnumerable<EntityMatch<T>> matches) where T : class
	{
		if (target == null || matches == null)
		{
			return;
		}
		foreach (EntityMatch<T> match in matches)
		{
			if (match == null)
			{
				continue;
			}
			string id = string.IsNullOrWhiteSpace(match.Id) ? match.Name : match.Id;
			if (!string.IsNullOrWhiteSpace(id))
			{
				target.Add(Tuple.Create(type, id, match.MentionPriority, match.Score, match.Name ?? ""));
			}
		}
	}

	private static List<EntityMatch<T>> FilterGlobalLimitList<T>(string type, IEnumerable<EntityMatch<T>> matches, HashSet<string> keep) where T : class
	{
		if (matches == null || keep == null || keep.Count == 0)
		{
			return new List<EntityMatch<T>>();
		}
		return matches.Where(delegate(EntityMatch<T> match)
		{
			if (match == null)
			{
				return false;
			}
			string id = string.IsNullOrWhiteSpace(match.Id) ? match.Name : match.Id;
			return !string.IsNullOrWhiteSpace(id) && keep.Contains(type + ":" + id);
		}).OrderBy((EntityMatch<T> x) => x.MentionPriority).ThenByDescending((EntityMatch<T> x) => x.Score).ThenBy((EntityMatch<T> x) => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static List<EntityMatch<T>> FindMatches<T>(string category, IEnumerable<string> mentions, Dictionary<string, int> mentionPriority, IEnumerable<T> candidates, Func<T, IEnumerable<string>> aliases, Func<T, string> idSelector, Func<T, string> nameSelector, WorldEntityRetrievalBudget budget) where T : class
	{
		using FreezeWatchdog.ScopeToken freezeScope = FreezeWatchdog.Scope("WorldEntityRetrieval.FindMatches." + (string.IsNullOrWhiteSpace(category) ? "unknown" : category));
		Stopwatch categorySw = Stopwatch.StartNew();
		Dictionary<string, EntityMatch<T>> selected = new Dictionary<string, EntityMatch<T>>(StringComparer.OrdinalIgnoreCase);
		List<T> candidateList = (candidates ?? Enumerable.Empty<T>()).Where((T x) => x != null).ToList();
		List<string> mentionList = (mentions ?? Enumerable.Empty<string>()).Select((string x) => (x ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)).ToList();
		Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] match_category_start category=" + (category ?? "") + " mentions=" + mentionList.Count + " candidates=" + candidateList.Count + " " + FormatBudgetForLog(budget));
		FreezeWatchdog.Mark("WorldEntityRetrieval.match_category_start", "category=" + (category ?? "") + " mentions=" + mentionList.Count + " candidates=" + candidateList.Count, immediate: true);
		List<EntityCandidateSnapshot<T>> snapshots = BuildCandidateSnapshots(category, candidateList, aliases, idSelector, nameSelector, budget);
		if (IsHardBudgetExceeded(budget))
		{
			LogWorldEntityBudgetStop("match_category_before_scoring", category, "", 0, snapshots.Count, selected.Count, budget);
			return selected.Values.OrderBy((EntityMatch<T> x) => x.MentionPriority).ThenByDescending((EntityMatch<T> x) => x.Score).ThenBy((EntityMatch<T> x) => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
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
			float cutoff = Math.Max(MatchThreshold, best - NearTopDelta);
			foreach (EntityMatch<T> match in scored.Where((EntityMatch<T> x) => x.Score >= cutoff).OrderByDescending((EntityMatch<T> x) => x.Score).ThenBy((EntityMatch<T> x) => x.Name, StringComparer.OrdinalIgnoreCase).Take(MaxMatchesPerMention))
			{
				string key = string.IsNullOrWhiteSpace(match.Id) ? match.Name : match.Id;
				if (string.IsNullOrWhiteSpace(key))
				{
					continue;
				}
				if (!selected.TryGetValue(key, out var existing) || match.MentionPriority < existing.MentionPriority || (match.MentionPriority == existing.MentionPriority && match.Score > existing.Score))
				{
					selected[key] = match;
				}
			}
			Logger.Log("WorldEntityRetrieval", "[WorldEntityPerf] match_mention_done category=" + (category ?? "") + " mention=" + PreviewWorldEntityLogValue(mention, 80) + " scored=" + scored.Count + " selectedTotal=" + selected.Count + " best=" + best.ToString("0.###", CultureInfo.InvariantCulture) + " scanned=" + scanned + "/" + snapshots.Count + " budgetStopped=" + budgetStopped + " ms=" + Math.Round(mentionSw.Elapsed.TotalMilliseconds, 2));
			if (budgetStopped)
			{
				break;
			}
		}
		List<EntityMatch<T>> result = selected.Values.OrderBy((EntityMatch<T> x) => x.MentionPriority).ThenByDescending((EntityMatch<T> x) => x.Score).ThenBy((EntityMatch<T> x) => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
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

	private static IEnumerable<ItemObject> GetItemCandidates()
	{
		IEnumerable<ItemObject> items = null;
		try
		{
			items = Game.Current?.ObjectManager?.GetObjectTypeList<ItemObject>();
		}
		catch
		{
			items = null;
		}
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (ItemObject item in items ?? Enumerable.Empty<ItemObject>())
		{
			if (item == null)
			{
				continue;
			}
			string id = (item.StringId ?? SafeName(item.Name, "")).Trim();
			if (!string.IsNullOrWhiteSpace(id) && seen.Add(id))
			{
				yield return item;
			}
		}
	}

	private static IEnumerable<CharacterObject> GetTroopCandidates()
	{
		IEnumerable<CharacterObject> troops = null;
		try
		{
			troops = CharacterObject.All;
		}
		catch
		{
			troops = null;
		}
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (CharacterObject troop in troops ?? Enumerable.Empty<CharacterObject>())
		{
			if (troop == null || troop.IsHero || troop == CharacterObject.PlayerCharacter)
			{
				continue;
			}
			string id = (troop.StringId ?? SafeName(troop.Name, "")).Trim();
			if (!string.IsNullOrWhiteSpace(id) && seen.Add(id))
			{
				yield return troop;
			}
		}
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

	private static IEnumerable<string> GetItemAliases(ItemObject item)
	{
		List<string> aliases = new List<string>();
		if (item == null)
		{
			return aliases;
		}
		AddAlias(aliases, SafeName(item.Name, ""));
		AddAlias(aliases, item.StringId);
		try
		{
			AddAlias(aliases, item.Type.ToString());
			AddAlias(aliases, RewardSystemBehavior.GetItemPromptTypeLabelForExternal(item));
			AddAlias(aliases, item.ItemCategory?.StringId);
			AddAlias(aliases, item.ItemCategory?.GetName()?.ToString());
			AddItemTypeAliases(aliases, item);
		}
		catch
		{
		}
		try
		{
			AddAlias(aliases, item.Culture?.Name?.ToString());
			AddAlias(aliases, item.Culture?.StringId);
		}
		catch
		{
		}
		return aliases;
	}

	private static IEnumerable<string> GetTroopAliases(CharacterObject troop)
	{
		List<string> aliases = new List<string>();
		if (troop == null)
		{
			return aliases;
		}
		AddAlias(aliases, SafeName(troop.Name, ""));
		AddAlias(aliases, troop.StringId);
		try
		{
			AddAlias(aliases, MyBehavior.GetPartyTransferTroopTypeLabelForExternal(troop));
			AddAlias(aliases, troop.DefaultFormationClass.ToString());
			AddTroopTypeAliases(aliases, troop);
			int tier = Math.Max(0, troop.Tier);
			AddAlias(aliases, "T" + tier.ToString(CultureInfo.InvariantCulture));
			AddAlias(aliases, tier.ToString(CultureInfo.InvariantCulture) + "阶");
			AddAlias(aliases, tier.ToString(CultureInfo.InvariantCulture) + "级");
			if (tier >= 4)
			{
				AddAlias(aliases, "高阶");
				AddAlias(aliases, "高级");
				AddAlias(aliases, "精锐");
			}
			else if (tier > 0 && tier <= 2)
			{
				AddAlias(aliases, "低阶");
				AddAlias(aliases, "低级");
				AddAlias(aliases, "新兵");
			}
		}
		catch
		{
		}
		try
		{
			AddAlias(aliases, troop.Culture?.Name?.ToString());
			AddAlias(aliases, troop.Culture?.StringId);
		}
		catch
		{
		}
		AddAlias(aliases, "部队");
		AddAlias(aliases, "士兵");
		AddAlias(aliases, "兵种");
		return aliases;
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

	private static void AddItemTypeAliases(List<string> aliases, ItemObject item)
	{
		if (item == null)
		{
			return;
		}
		try
		{
			if (item.IsFood)
			{
				AddAlias(aliases, "食物");
				AddAlias(aliases, "粮食");
				AddAlias(aliases, "粮草");
			}
			if (item.IsAnimal)
			{
				AddAlias(aliases, "牲畜");
				AddAlias(aliases, "动物");
			}
			if (item.HasHorseComponent)
			{
				AddAlias(aliases, "马");
				AddAlias(aliases, "马匹");
				AddAlias(aliases, "坐骑");
			}
		}
		catch
		{
		}
		switch (item.Type)
		{
		case ItemObject.ItemTypeEnum.Horse:
			AddAlias(aliases, "马");
			AddAlias(aliases, "马匹");
			AddAlias(aliases, "坐骑");
			break;
		case ItemObject.ItemTypeEnum.OneHandedWeapon:
			AddAlias(aliases, "单手武器");
			AddAlias(aliases, "剑");
			AddAlias(aliases, "刀");
			break;
		case ItemObject.ItemTypeEnum.TwoHandedWeapon:
			AddAlias(aliases, "双手武器");
			AddAlias(aliases, "大剑");
			break;
		case ItemObject.ItemTypeEnum.Polearm:
			AddAlias(aliases, "长柄武器");
			AddAlias(aliases, "长枪");
			AddAlias(aliases, "枪矛");
			break;
		case ItemObject.ItemTypeEnum.Bow:
		case ItemObject.ItemTypeEnum.Crossbow:
			AddAlias(aliases, "弓");
			AddAlias(aliases, "弓箭");
			AddAlias(aliases, "弩");
			AddAlias(aliases, "远程武器");
			break;
		case ItemObject.ItemTypeEnum.Arrows:
		case ItemObject.ItemTypeEnum.Bolts:
			AddAlias(aliases, "箭");
			AddAlias(aliases, "箭矢");
			AddAlias(aliases, "弹药");
			break;
		case ItemObject.ItemTypeEnum.Shield:
			AddAlias(aliases, "盾");
			AddAlias(aliases, "盾牌");
			break;
		case ItemObject.ItemTypeEnum.Thrown:
			AddAlias(aliases, "投掷武器");
			AddAlias(aliases, "标枪");
			break;
		case ItemObject.ItemTypeEnum.HeadArmor:
			AddAlias(aliases, "头盔");
			AddAlias(aliases, "盔");
			break;
		case ItemObject.ItemTypeEnum.BodyArmor:
		case ItemObject.ItemTypeEnum.ChestArmor:
			AddAlias(aliases, "甲");
			AddAlias(aliases, "盔甲");
			AddAlias(aliases, "护甲");
			AddAlias(aliases, "铠甲");
			break;
		case ItemObject.ItemTypeEnum.LegArmor:
			AddAlias(aliases, "腿甲");
			break;
		case ItemObject.ItemTypeEnum.HandArmor:
			AddAlias(aliases, "手甲");
			break;
		case ItemObject.ItemTypeEnum.HorseHarness:
			AddAlias(aliases, "马具");
			AddAlias(aliases, "马甲");
			break;
		case ItemObject.ItemTypeEnum.Goods:
			AddAlias(aliases, "货物");
			AddAlias(aliases, "商品");
			AddAlias(aliases, "贸易品");
			break;
		case ItemObject.ItemTypeEnum.Animal:
			AddAlias(aliases, "牲畜");
			AddAlias(aliases, "动物");
			break;
		}
	}

	private static void AddTroopTypeAliases(List<string> aliases, CharacterObject troop)
	{
		if (troop == null)
		{
			return;
		}
		string type = MyBehavior.GetPartyTransferTroopTypeLabelForExternal(troop);
		AddAlias(aliases, type);
		if (string.Equals(type, "弓手", StringComparison.Ordinal))
		{
			AddAlias(aliases, "弓箭手");
			AddAlias(aliases, "射手");
			AddAlias(aliases, "远程");
		}
		if (string.Equals(type, "骑兵", StringComparison.Ordinal))
		{
			AddAlias(aliases, "马兵");
			AddAlias(aliases, "骑手");
			AddAlias(aliases, "骑士");
		}
		if (string.Equals(type, "骑射手", StringComparison.Ordinal))
		{
			AddAlias(aliases, "骑射");
			AddAlias(aliases, "马弓手");
			AddAlias(aliases, "骑弓手");
		}
		if (string.Equals(type, "步兵", StringComparison.Ordinal))
		{
			AddAlias(aliases, "步卒");
			AddAlias(aliases, "近战");
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

	private static string BuildMainPromptBlock(string playerDisplayName, Hero contextHero, List<EntityMatch<Hero>> heroes, List<EntityMatch<Settlement>> settlements, List<EntityMatch<Clan>> clans, List<EntityMatch<Kingdom>> kingdoms, List<EntityMatch<ItemObject>> items, List<EntityMatch<CharacterObject>> troops, List<VisiblePartyCandidate> visibleParties)
	{
		string player = ResolvePlayerDisplayNameForPrompt(playerDisplayName);
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("你和" + player + "交流可用的实体信息：");
		AppendHeroMainFacts(sb, heroes, player, contextHero);
		AppendSettlementMainFacts(sb, settlements);
		AppendClanMainFacts(sb, clans);
		AppendKingdomMainFacts(sb, kingdoms);
		AppendItemMainFacts(sb, items);
		AppendTroopMainFacts(sb, troops);
		AppendVisiblePartyFacts(sb, visibleParties);
		return StripEntityIdsFromMainPromptBlock(sb.ToString()).Trim();
	}

	private static void AddResidentEntityMatches(Hero contextHero, bool includeResidentKingdoms, ref List<EntityMatch<Hero>> heroes, ref List<EntityMatch<Settlement>> settlements, ref List<EntityMatch<Clan>> clans, ref List<EntityMatch<Kingdom>> kingdoms)
	{
		heroes = heroes ?? new List<EntityMatch<Hero>>();
		settlements = settlements ?? new List<EntityMatch<Settlement>>();
		clans = clans ?? new List<EntityMatch<Clan>>();
		kingdoms = kingdoms ?? new List<EntityMatch<Kingdom>>();
		int priority = -1000;
		Hero player = Hero.MainHero;
		Clan playerClan = Clan.PlayerClan ?? player?.Clan;
		AddResidentClanMatch(clans, playerClan, "常驻：玩家当前家族", priority++);
		if (includeResidentKingdoms)
		{
			AddResidentKingdomMatch(kingdoms, ResolveHeroKingdomForResidentEntity(player, playerClan), "常驻：玩家当前王国", priority++);
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

	private static void AddPostprocessResidentEntityMatches(Hero contextHero, ref List<EntityMatch<Hero>> heroes, ref List<EntityMatch<Settlement>> settlements, ref List<EntityMatch<Clan>> clans, ref List<EntityMatch<Kingdom>> kingdoms)
	{
		heroes = heroes ?? new List<EntityMatch<Hero>>();
		settlements = settlements ?? new List<EntityMatch<Settlement>>();
		clans = clans ?? new List<EntityMatch<Clan>>();
		kingdoms = kingdoms ?? new List<EntityMatch<Kingdom>>();
		int priority = -1000;
		Hero player = Hero.MainHero;
		Clan playerClan = Clan.PlayerClan ?? player?.Clan;
		AddResidentClanMatch(clans, playerClan, "常驻：玩家当前家族", priority++);
		AddResidentKingdomMatch(kingdoms, ResolveHeroKingdomForResidentEntity(player, playerClan), "常驻：玩家当前王国", priority++);
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
				MentionPriority = match.MentionPriority
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
		result = Regex.Replace(result, @"\b(?:hero|settlement|clan|kingdom|item|troop|party|mobile_party):[A-Za-z0-9_.\-]+\b", "未知", RegexOptions.IgnoreCase);
		result = Regex.Replace(result, @"(?<![\p{L}\p{N}_])(?:lord|lady|wanderer|companion|town|castle|village|settlement|clan|kingdom|item|troop|looters|bandits|mountain_bandits|forest_bandits|desert_bandits|sea_raiders|steppe_bandits|villagers|caravan|party|mobile_party)[A-Za-z0-9_\-]*\d[A-Za-z0-9_\-]*(?![\p{L}\p{N}_])", "未知", RegexOptions.IgnoreCase);
		return result.Trim();
	}

	private static string BuildPostprocessPromptBlock(List<EntityMatch<Hero>> heroes, List<EntityMatch<Settlement>> settlements, List<EntityMatch<Clan>> clans, List<EntityMatch<Kingdom>> kingdoms, List<EntityMatch<ItemObject>> items, List<EntityMatch<CharacterObject>> troops, List<VisiblePartyCandidate> visibleParties)
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
				sb.AppendLine((i + 1) + ". 名称：" + SafeName(hero?.Name, postprocessHeroes[i].Name) + "；位置：" + FormatHeroLocation(hero) + "；编号：" + postprocessHeroes[i].Id + FormatPostprocessMentionHint(postprocessHeroes[i]));
			}
		}
		if (settlements != null && settlements.Count > 0)
		{
			sb.AppendLine("【地点】");
			for (int i = 0; i < settlements.Count; i++)
			{
				Settlement settlement = settlements[i].Value;
				sb.AppendLine((i + 1) + ". 名称：" + SafeName(settlement?.Name, settlements[i].Name) + "；编号：" + settlements[i].Id + FormatPostprocessMentionHint(settlements[i]));
			}
		}
		if (postprocessClans.Count > 0)
		{
			sb.AppendLine("【家族】");
			for (int i = 0; i < postprocessClans.Count; i++)
			{
				Clan clan = postprocessClans[i].Value;
				sb.AppendLine((i + 1) + ". 名称：" + SafeName(clan?.Name, postprocessClans[i].Name) + "；编号：" + postprocessClans[i].Id + FormatPostprocessMentionHint(postprocessClans[i]));
			}
		}
		if (postprocessKingdoms.Count > 0)
		{
			sb.AppendLine("【王国】");
			for (int i = 0; i < postprocessKingdoms.Count; i++)
			{
				Kingdom kingdom = postprocessKingdoms[i].Value;
				sb.AppendLine((i + 1) + ". 名称：" + SafeName(kingdom?.Name, postprocessKingdoms[i].Name) + "；编号：" + postprocessKingdoms[i].Id + FormatPostprocessMentionHint(postprocessKingdoms[i]));
			}
		}
		if (items != null && items.Count > 0)
		{
			sb.AppendLine("【物品】");
			for (int i = 0; i < items.Count; i++)
			{
				ItemObject item = items[i].Value;
				sb.AppendLine((i + 1) + ". 名称：" + SafeName(item?.Name, items[i].Name) + "；类型：" + FormatItemTypeLabel(item) + "；编号：" + items[i].Id + FormatPostprocessMentionHint(items[i]));
			}
		}
		if (troops != null && troops.Count > 0)
		{
			sb.AppendLine("【兵种】");
			for (int i = 0; i < troops.Count; i++)
			{
				CharacterObject troop = troops[i].Value;
				sb.AppendLine((i + 1) + ". 名称：" + SafeName(troop?.Name, troops[i].Name) + "；类型：" + FormatTroopTypeLabel(troop) + "；编号：" + troops[i].Id + FormatPostprocessMentionHint(troops[i]));
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
			sb.AppendLine("1. 名称：" + SafeName(player.Name, "玩家") + "；固定ID：hero:" + id + "；用于FOLLOW玩家时目标类型写hero，id填写" + id + "。");
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
		return index + ". 名称：" + party.Name + "；数量：" + party.Count + shipSegment + "；部队ID：" + party.Id + "；从属：" + party.Affiliation + "；方位：" + party.Direction + "；距离：" + FormatDistance(party.Distance);
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

	private static void AppendItemMainFacts(StringBuilder sb, List<EntityMatch<ItemObject>> matches)
	{
		if (matches == null || matches.Count == 0)
		{
			return;
		}
		sb.AppendLine();
		sb.AppendLine("【物品】");
		for (int i = 0; i < matches.Count; i++)
		{
			ItemObject item = matches[i].Value;
			sb.AppendLine((i + 1) + ". " + SafeName(item?.Name, matches[i].Name) + "（编号：" + matches[i].Id + "；匹配分：" + FormatScore(matches[i].Score) + "；提及：" + matches[i].Mention + "）");
			sb.AppendLine("类型：" + FormatItemTypeLabel(item) + "；分类：" + FormatItemCategory(item) + "；文化：" + FormatItemCulture(item) + "；价值：" + FormatInt(item?.Value));
			sb.AppendLine("用途标签：" + FormatItemTags(item));
		}
	}

	private static void AppendTroopMainFacts(StringBuilder sb, List<EntityMatch<CharacterObject>> matches)
	{
		if (matches == null || matches.Count == 0)
		{
			return;
		}
		sb.AppendLine();
		sb.AppendLine("【兵种】");
		for (int i = 0; i < matches.Count; i++)
		{
			CharacterObject troop = matches[i].Value;
			sb.AppendLine((i + 1) + ". " + SafeName(troop?.Name, matches[i].Name) + "（编号：" + matches[i].Id + "；匹配分：" + FormatScore(matches[i].Score) + "；提及：" + matches[i].Mention + "）");
			sb.AppendLine("文化：" + FormatTroopCulture(troop) + "；类型：" + FormatTroopTypeLabel(troop) + "；阶级：" + FormatInt(troop?.Tier) + "；等级：" + FormatInt(troop?.Level) + "；工资：" + FormatInt(troop?.TroopWage));
			sb.AppendLine("升级目标：" + FormatTroopUpgradeTargets(troop));
		}
	}

	private static string FormatItemTypeLabel(ItemObject item)
	{
		try
		{
			string label = RewardSystemBehavior.GetItemPromptTypeLabelForExternal(item);
			if (!string.IsNullOrWhiteSpace(label))
			{
				return label.Trim();
			}
			return item == null ? "未知" : item.Type.ToString();
		}
		catch
		{
			return "未知";
		}
	}

	private static string FormatItemCategory(ItemObject item)
	{
		try
		{
			string name = item?.ItemCategory?.GetName()?.ToString();
			if (!string.IsNullOrWhiteSpace(name))
			{
				return name.Trim();
			}
			string id = item?.ItemCategory?.StringId;
			return string.IsNullOrWhiteSpace(id) ? "未知" : id.Trim();
		}
		catch
		{
			return "未知";
		}
	}

	private static string FormatItemCulture(ItemObject item)
	{
		try
		{
			string name = item?.Culture?.Name?.ToString();
			if (!string.IsNullOrWhiteSpace(name))
			{
				return name.Trim();
			}
			string id = item?.Culture?.StringId;
			return string.IsNullOrWhiteSpace(id) ? "未知" : id.Trim();
		}
		catch
		{
			return "未知";
		}
	}

	private static string FormatItemTags(ItemObject item)
	{
		if (item == null)
		{
			return "未知";
		}
		List<string> parts = new List<string>();
		try
		{
			if (item.IsFood)
			{
				parts.Add("食物");
			}
			if (item.IsAnimal)
			{
				parts.Add("动物");
			}
			if (item.HasHorseComponent)
			{
				parts.Add("马匹/坐骑");
			}
		}
		catch
		{
		}
		if (parts.Count == 0)
		{
			parts.Add("普通物品");
		}
		return string.Join("，", parts);
	}

	private static string FormatTroopTypeLabel(CharacterObject troop)
	{
		try
		{
			string label = MyBehavior.GetPartyTransferTroopTypeLabelForExternal(troop);
			return string.IsNullOrWhiteSpace(label) ? "兵种" : label.Trim();
		}
		catch
		{
			return "兵种";
		}
	}

	private static string FormatTroopCulture(CharacterObject troop)
	{
		try
		{
			string name = troop?.Culture?.Name?.ToString();
			if (!string.IsNullOrWhiteSpace(name))
			{
				return name.Trim();
			}
			string id = troop?.Culture?.StringId;
			return string.IsNullOrWhiteSpace(id) ? "未知" : id.Trim();
		}
		catch
		{
			return "未知";
		}
	}

	private static string FormatTroopUpgradeTargets(CharacterObject troop)
	{
		try
		{
			List<string> names = (troop?.UpgradeTargets ?? new CharacterObject[0])
				.Where((CharacterObject x) => x != null)
				.Select((CharacterObject x) => SafeName(x.Name, x.StringId))
				.Where((string x) => !string.IsNullOrWhiteSpace(x))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.Take(4)
				.ToList();
			return names.Count == 0 ? "无" : string.Join("、", names);
		}
		catch
		{
			return "未知";
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

	private static string FormatHeroOccupation(Hero hero)
	{
		if (hero == null)
		{
			return "未知";
		}
		List<string> parts = new List<string>();
		try
		{
			if (hero.IsKingdomLeader)
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
		if (kingdom == null)
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
