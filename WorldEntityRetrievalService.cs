using System;
using System.Collections.Generic;
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
	public List<string> Heroes = new List<string>();

	public List<string> Settlements = new List<string>();

	public List<string> Clans = new List<string>();

	public List<string> Kingdoms = new List<string>();

	public bool IsEmpty
	{
		get
		{
			return IsEmptyList(Heroes) && IsEmptyList(Settlements) && IsEmptyList(Clans) && IsEmptyList(Kingdoms);
		}
	}

	public MentionedWorldEntities Clone()
	{
		return new MentionedWorldEntities
		{
			Heroes = new List<string>(Heroes ?? new List<string>()),
			Settlements = new List<string>(Settlements ?? new List<string>()),
			Clans = new List<string>(Clans ?? new List<string>()),
			Kingdoms = new List<string>(Kingdoms ?? new List<string>())
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

	private sealed class EntityMatch<T>
	{
		public T Value;

		public string Id;

		public string Name;

		public string Mention;

		public float Score;

		public int MentionPriority;
	}

	public static WorldEntityPromptContext BuildPromptContext(MentionedWorldEntities mentions, string playerDisplayName)
	{
		WorldEntityPromptContext result = new WorldEntityPromptContext();
		try
		{
			if (mentions == null || mentions.IsEmpty || Campaign.Current == null)
			{
				return result;
			}
			List<string> allMentions = BuildMergedMentionList(mentions);
			if (allMentions.Count == 0)
			{
				return result;
			}
			Dictionary<string, int> mentionPriority = BuildMentionPriority(allMentions);
			int maxInjectedEntities = GetMaxInjectedEntitiesFromSettings();
			List<Hero> heroCandidates = GetHeroCandidates().ToList();
			List<Settlement> settlementCandidates = GetSettlementCandidates().ToList();
			List<Clan> clanCandidates = GetClanCandidates().ToList();
			List<Kingdom> kingdomCandidates = GetKingdomCandidates().ToList();
			Logger.Log("WorldEntityRetrieval", "mentions total=" + allMentions.Count + " maxInject=" + maxInjectedEntities + " heroes=" + CountList(mentions.Heroes) + " settlements=" + CountList(mentions.Settlements) + " clans=" + CountList(mentions.Clans) + " kingdoms=" + CountList(mentions.Kingdoms) + " candidates hero=" + heroCandidates.Count + " settlement=" + settlementCandidates.Count + " clan=" + clanCandidates.Count + " kingdom=" + kingdomCandidates.Count + " names=" + FormatMentionsForLog(allMentions));
			List<EntityMatch<Hero>> heroes = FindMatches(allMentions, mentionPriority, heroCandidates, GetHeroAliases, (Hero x) => "hero:" + SafeStringId(x?.StringId), (Hero x) => SafeName(x?.Name, x?.StringId ?? "Hero"));
			List<EntityMatch<Settlement>> settlements = FindMatches(allMentions, mentionPriority, settlementCandidates, GetSettlementAliases, (Settlement x) => "settlement:" + SafeStringId(x?.StringId), (Settlement x) => SafeName(x?.Name, x?.StringId ?? "Settlement"));
			List<EntityMatch<Clan>> clans = FindMatches(allMentions, mentionPriority, clanCandidates, GetClanAliases, (Clan x) => "clan:" + SafeStringId(x?.StringId), (Clan x) => SafeName(x?.Name, x?.StringId ?? "Clan"));
			List<EntityMatch<Kingdom>> kingdoms = FindMatches(allMentions, mentionPriority, kingdomCandidates, GetKingdomAliases, (Kingdom x) => "kingdom:" + SafeStringId(x?.StringId), (Kingdom x) => SafeName(x?.Name, x?.StringId ?? "Kingdom"));
			ApplyGlobalInjectionLimit(maxInjectedEntities, ref heroes, ref settlements, ref clans, ref kingdoms);
			int count = heroes.Count + settlements.Count + clans.Count + kingdoms.Count;
			if (count <= 0)
			{
				Logger.Log("WorldEntityRetrieval", "no_match mentions=" + FormatMentionsForLog(allMentions));
				return result;
			}
			result.MatchCount = count;
			result.MainPromptBlock = BuildMainPromptBlock(playerDisplayName, heroes, settlements, clans, kingdoms);
			result.PostprocessPromptBlock = BuildPostprocessPromptBlock(heroes, settlements, clans, kingdoms);
			return result;
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("WorldEntityRetrieval", "build_prompt_context failed: " + ex.Message);
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

	private static void ApplyGlobalInjectionLimit(int maxCount, ref List<EntityMatch<Hero>> heroes, ref List<EntityMatch<Settlement>> settlements, ref List<EntityMatch<Clan>> clans, ref List<EntityMatch<Kingdom>> kingdoms)
	{
		maxCount = ClampMaxInjectedEntities(maxCount);
		List<Tuple<string, string, int, float, string>> ordered = new List<Tuple<string, string, int, float, string>>();
		AddGlobalLimitItems(ordered, "hero", heroes);
		AddGlobalLimitItems(ordered, "settlement", settlements);
		AddGlobalLimitItems(ordered, "clan", clans);
		AddGlobalLimitItems(ordered, "kingdom", kingdoms);
		HashSet<string> keep = new HashSet<string>(ordered.OrderBy((Tuple<string, string, int, float, string> x) => x.Item3).ThenByDescending((Tuple<string, string, int, float, string> x) => x.Item4).ThenBy((Tuple<string, string, int, float, string> x) => x.Item5, StringComparer.OrdinalIgnoreCase).Take(maxCount).Select((Tuple<string, string, int, float, string> x) => x.Item1 + ":" + x.Item2), StringComparer.OrdinalIgnoreCase);
		heroes = FilterGlobalLimitList("hero", heroes, keep);
		settlements = FilterGlobalLimitList("settlement", settlements, keep);
		clans = FilterGlobalLimitList("clan", clans, keep);
		kingdoms = FilterGlobalLimitList("kingdom", kingdoms, keep);
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

	private static List<EntityMatch<T>> FindMatches<T>(IEnumerable<string> mentions, Dictionary<string, int> mentionPriority, IEnumerable<T> candidates, Func<T, IEnumerable<string>> aliases, Func<T, string> idSelector, Func<T, string> nameSelector) where T : class
	{
		Dictionary<string, EntityMatch<T>> selected = new Dictionary<string, EntityMatch<T>>(StringComparer.OrdinalIgnoreCase);
		List<T> candidateList = (candidates ?? Enumerable.Empty<T>()).Where((T x) => x != null).ToList();
		foreach (string mentionRaw in mentions ?? Enumerable.Empty<string>())
		{
			string mention = (mentionRaw ?? "").Trim();
			if (string.IsNullOrWhiteSpace(mention))
			{
				continue;
			}
			int priority = GetMentionPriority(mentionPriority, mention);
			List<EntityMatch<T>> scored = new List<EntityMatch<T>>();
			foreach (T candidate in candidateList)
			{
				float score = CalculateBestScore(mention, aliases(candidate));
				if (score >= MatchThreshold)
				{
					scored.Add(new EntityMatch<T>
					{
						Value = candidate,
						Id = idSelector(candidate) ?? "",
						Name = nameSelector(candidate) ?? "",
						Mention = mention,
						Score = score,
						MentionPriority = priority
					});
				}
			}
			if (scored.Count == 0)
			{
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
		}
		return selected.Values.OrderBy((EntityMatch<T> x) => x.MentionPriority).ThenByDescending((EntityMatch<T> x) => x.Score).ThenBy((EntityMatch<T> x) => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
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
		float best = 0f;
		foreach (string alias in aliases ?? Enumerable.Empty<string>())
		{
			best = Math.Max(best, CalculateFuzzyScore(mention, alias));
		}
		return best;
	}

	private static float CalculateFuzzyScore(string left, string right)
	{
		string a = NormalizeFuzzyText(left);
		string b = NormalizeFuzzyText(right);
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
		List<string> a = SplitTokens(left);
		List<string> b = SplitTokens(right);
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

	private static string BuildMainPromptBlock(string playerDisplayName, List<EntityMatch<Hero>> heroes, List<EntityMatch<Settlement>> settlements, List<EntityMatch<Clan>> clans, List<EntityMatch<Kingdom>> kingdoms)
	{
		string player = string.IsNullOrWhiteSpace(playerDisplayName) ? "玩家" : playerDisplayName.Trim();
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("你和" + player + "交流可能提到了以下信息：");
		AppendHeroMainFacts(sb, heroes);
		AppendSettlementMainFacts(sb, settlements);
		AppendClanMainFacts(sb, clans);
		AppendKingdomMainFacts(sb, kingdoms);
		return sb.ToString().Trim();
	}

	private static string BuildPostprocessPromptBlock(List<EntityMatch<Hero>> heroes, List<EntityMatch<Settlement>> settlements, List<EntityMatch<Clan>> clans, List<EntityMatch<Kingdom>> kingdoms)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("可能有效的信息：");
		if (heroes != null && heroes.Count > 0)
		{
			sb.AppendLine("【人物】");
			for (int i = 0; i < heroes.Count; i++)
			{
				Hero hero = heroes[i].Value;
				sb.AppendLine((i + 1) + ". 名称：" + SafeName(hero?.Name, heroes[i].Name) + "；位置：" + FormatHeroLocation(hero) + "；编号：" + heroes[i].Id);
			}
		}
		if (settlements != null && settlements.Count > 0)
		{
			sb.AppendLine("【地点】");
			for (int i = 0; i < settlements.Count; i++)
			{
				Settlement settlement = settlements[i].Value;
				sb.AppendLine((i + 1) + ". 名称：" + SafeName(settlement?.Name, settlements[i].Name) + "；编号：" + settlements[i].Id);
			}
		}
		if (clans != null && clans.Count > 0)
		{
			sb.AppendLine("【家族】");
			for (int i = 0; i < clans.Count; i++)
			{
				Clan clan = clans[i].Value;
				sb.AppendLine((i + 1) + ". 名称：" + SafeName(clan?.Name, clans[i].Name) + "；编号：" + clans[i].Id);
			}
		}
		if (kingdoms != null && kingdoms.Count > 0)
		{
			sb.AppendLine("【王国】");
			for (int i = 0; i < kingdoms.Count; i++)
			{
				Kingdom kingdom = kingdoms[i].Value;
				sb.AppendLine((i + 1) + ". 名称：" + SafeName(kingdom?.Name, kingdoms[i].Name) + "；编号：" + kingdoms[i].Id);
			}
		}
		return sb.ToString().Trim();
	}

	private static void AppendHeroMainFacts(StringBuilder sb, List<EntityMatch<Hero>> matches)
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
			sb.AppendLine("所属家族：" + SafeName(hero?.Clan?.Name, "未知") + "；王国：" + FormatHeroKingdom(hero) + "；家族族长：" + SafeName(hero?.Clan?.Leader?.Name, "未知"));
			sb.AppendLine("特质：" + FormatHeroTraits(hero) + "；所有亲人：" + FormatHeroRelatives(hero));
			sb.AppendLine("现在的位置：" + FormatHeroLocation(hero) + "；目前的状态：" + FormatHeroStatus(hero));
			sb.AppendLine("年龄(Age)：" + FormatAge(hero) + "；生死状态(IsAlive)：" + FormatBool(hero != null && hero.IsAlive) + "；性别：" + FormatGender(hero) + "；职业/头衔(Occupation/Title)：" + FormatHeroOccupation(hero));
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
			sb.AppendLine((i + 1) + ". " + SafeName(settlement?.Name, matches[i].Name) + "（编号：" + matches[i].Id + "；匹配分：" + FormatScore(matches[i].Score) + "；提及：" + matches[i].Mention + "）");
			sb.AppendLine("所属家族：" + SafeName(settlement?.OwnerClan?.Name, "未知") + "；王国：" + FormatSettlementKingdom(settlement) + "；文化(Culture)：" + SafeName(settlement?.Culture?.Name, settlement?.Culture?.StringId ?? "未知") + "；家族族长：" + SafeName(settlement?.OwnerClan?.Leader?.Name, "未知"));
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
			sb.AppendLine("所有成员：" + FormatHeroList(clan?.Heroes, 24));
			sb.AppendLine("所属王国：" + SafeName(clan?.Kingdom?.Name, "无") + "；家族影响力(Influence)：" + FormatFloat(clan?.Influence) + "；家族文化(Culture)：" + SafeName(clan?.Culture?.Name, clan?.Culture?.StringId ?? "未知"));
			sb.AppendLine("家族财富：" + FormatInt(clan?.Gold) + "；家族等级：" + FormatInt(clan?.Tier) + "；家族拥有的所有定居点：" + FormatClanFiefs(clan));
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
			sb.AppendLine("国王名称：" + SafeName(kingdom?.Leader?.Name, "未知") + "；王国总兵力：" + FormatFloat(kingdom?.CurrentTotalStrength) + "；国家文化(Culture)：" + SafeName(kingdom?.Culture?.Name, kingdom?.Culture?.StringId ?? "未知"));
			sb.AppendLine("所有家族与族长：" + FormatKingdomClans(kingdom));
			sb.AppendLine("王国当前状态：" + FormatKingdomStatus(kingdom));
		}
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
				return SafeName(hero.CurrentSettlement.Name, hero.CurrentSettlement.StringId);
			}
			if (hero.PartyBelongedTo != null)
			{
				MobileParty party = hero.PartyBelongedTo;
				if (party.CurrentSettlement != null)
				{
					return SafeName(party.CurrentSettlement.Name, party.CurrentSettlement.StringId) + "（定居点内）";
				}
				return FormatMobilePartyMapLocation(party);
			}
			if (hero.IsPrisoner && hero.PartyBelongedToAsPrisoner != null)
			{
				PartyBase holder = hero.PartyBelongedToAsPrisoner;
				if (holder.IsSettlement && holder.Settlement != null)
				{
					return SafeName(holder.Settlement.Name, holder.Settlement.StringId) + "（囚禁中）";
				}
				if (holder.IsMobile && holder.MobileParty != null)
				{
					return FormatMobilePartyMapLocation(holder.MobileParty) + "（囚禁于该队伍）";
				}
			}
			if (hero.HomeSettlement != null)
			{
				return SafeName(hero.HomeSettlement.Name, hero.HomeSettlement.StringId);
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
					parts.Add("在 " + SafeName(party.CurrentSettlement.Name, party.CurrentSettlement.StringId));
				}
				else if (party.TargetSettlement != null)
				{
					parts.Add("正在前往 " + SafeName(party.TargetSettlement.Name, party.TargetSettlement.StringId));
				}
				else
				{
					string nearest = FormatNearestSettlementForParty(party);
					if (!string.IsNullOrWhiteSpace(nearest))
					{
						parts.Add("在 " + nearest + " 附近活动");
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
				return SafeName(party.CurrentSettlement.Name, party.CurrentSettlement.StringId) + "（定居点内）";
			}
			if (party.BesiegedSettlement != null)
			{
				return SafeName(party.BesiegedSettlement.Name, party.BesiegedSettlement.StringId) + "外围（围攻相关）";
			}
			string nearest = FormatNearestSettlementForParty(party);
			string target = party.TargetSettlement == null ? "" : SafeName(party.TargetSettlement.Name, party.TargetSettlement.StringId);
			if (!string.IsNullOrWhiteSpace(nearest) && !string.IsNullOrWhiteSpace(target))
			{
				return "大地图，当前位置：" + nearest + "附近；正在前往 " + target;
			}
			if (!string.IsNullOrWhiteSpace(nearest))
			{
				return "大地图，当前位置：" + nearest + "附近";
			}
			if (!string.IsNullOrWhiteSpace(target))
			{
				return "大地图，正在前往 " + target;
			}
			if (party.LastVisitedSettlement != null)
			{
				return "大地图，最近离开 " + SafeName(party.LastVisitedSettlement.Name, party.LastVisitedSettlement.StringId);
			}
			return "大地图，队伍：" + SafeName(party.Name, party.StringId);
		}
		catch
		{
			return "大地图，队伍：" + SafeName(party.Name, party.StringId);
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
			string name = SafeName(nearest.Name, nearest.StringId);
			if (distance > 0.001f && distance < float.MaxValue)
			{
				return name + "（约 " + distance.ToString("0.0", CultureInfo.InvariantCulture) + " 公里）";
			}
			return name;
		}
		catch
		{
			return "";
		}
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
				return "，关押于 " + SafeName(holder.Settlement.Name, holder.Settlement.StringId);
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
			List<string> names = (((IEnumerable<Village>)settlement?.BoundVillages) ?? Enumerable.Empty<Village>()).Where((Village x) => x?.Settlement != null).Select((Village x) => SafeName(x.Settlement.Name, x.Settlement.StringId)).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToList();
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

	private static string FormatClanFiefs(Clan clan)
	{
		try
		{
			List<string> names = (((IEnumerable<Town>)clan?.Fiefs) ?? Enumerable.Empty<Town>()).Where((Town x) => x?.Settlement != null).Select((Town x) => SafeName(x.Settlement.Name, x.Settlement.StringId)).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			return names.Count == 0 ? "无" : string.Join("、", names);
		}
		catch
		{
			return "未知";
		}
	}

	private static string FormatKingdomClans(Kingdom kingdom)
	{
		try
		{
			List<string> names = (((IEnumerable<Clan>)kingdom?.Clans) ?? Enumerable.Empty<Clan>()).Where((Clan x) => x != null).Select((Clan x) => SafeName(x.Name, x.StringId) + "（族长：" + SafeName(x.Leader?.Name, "未知") + "）").Where((string x) => !string.IsNullOrWhiteSpace(x)).Take(24).ToList();
			return names.Count == 0 ? "无" : string.Join("、", names);
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
#if BANNERLORD_1_4_OR_GREATER
			return behavior.HasTradeAgreement(kingdom, other, out var _);
#else
			return behavior.HasTradeAgreement(kingdom, other);
#endif
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
