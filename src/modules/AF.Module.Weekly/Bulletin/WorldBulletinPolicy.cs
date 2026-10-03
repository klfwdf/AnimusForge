using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AnimusForge;

// One observed campaign fact. Stored in the save; sentences are plain facts with no prompt instructions.
internal sealed class WorldBulletinParticipant
{
	public string HeroId = "";
	public string Name = "";
	public string Role = "";
}

// Persisted with the issue layout. Identity and facts are independent of the writer's wording.
internal sealed class WorldBulletinIllustrationPlan
{
	public string Identity = "";
	public string Title = "";
	public string DateText = "";
	public string Facts = "";
	public List<WorldBulletinParticipant> Participants = new List<WorldBulletinParticipant>();
}

internal sealed class WorldBulletinEvent
{
	public string Key = "";

	public string Kind = "";

	public int Day;

	public double Hour;

	public int Score;

	public string Sentence = "";

	// Facts that belong to one story (same executioner, same siege, same clash) share a group.
	public string Group = "";

	// Extra captured facts handed to the writer only; never shown verbatim.
	public string Detail = "";

	public List<string> KingdomIds = new List<string>();

	public bool InvolvesPlayer;

	public List<WorldBulletinParticipant> Participants = new List<WorldBulletinParticipant>();
}

// One minor line: several same-group events collapse into it.
internal sealed class WorldBulletinMinor
{
	public List<WorldBulletinEvent> Events = new List<WorldBulletinEvent>();

	public string Sentence = "";
}

// Per-scope trigger state: a major event opens a collect window; publishing starts a cooldown.
internal sealed class WorldBulletinScopeState
{
	public double WindowEndHour = -1;

	public double CooldownUntilHour;

	public double CutoffHour = -1;

	public int Sequence;

	public string LastMajorKey = "";

	public double LastPublishHour = -1;

	// A trigger arrived while cooling down; the hourly tick reopens a window once the cooldown ends.
	public bool PendingTrigger;
}

// Everything the bulletin system persists, saved as one JSON chunk.
internal sealed class WorldBulletinSaveState
{
	// Recovery copy only; never interpreted as current facts or injected into prompts.
	// Optional JSON field keeps the existing save key and old valid states compatible.
	public string PreservedUnreadableState;

	public List<WorldBulletinEvent> Events = new List<WorldBulletinEvent>();

	// The single bulletin's scope (named World for save compatibility).
	public WorldBulletinScopeState World = new WorldBulletinScopeState();

	// Unused since the two scopes merged; kept so older saves deserialize unchanged.
	public WorldBulletinScopeState Player = new WorldBulletinScopeState();

	// "week|kingdomId" -> stability already applied that week, capped by WeeklyStabilityCap.
	public Dictionary<string, int> WeeklyStability = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	public int LastKingdomWeek = -1;

	public int TrackingStartDay = -1;

	public List<WorldBulletinLayout> Layouts = new List<WorldBulletinLayout>();

	// Only notices waiting for illustration release; cleared after the durable unread queue accepts them.
	// Optional field in the existing JSON save, so old saves need no migration.
	public List<string> PendingNoticeEventIds = new List<string>();
}

internal sealed class WorldBulletinSelection
{
	// Lead fact; its key marks the bulletin.
	public WorldBulletinEvent Major;

	// Lead first, then facts merged into the same major story.
	public List<WorldBulletinEvent> MajorFacts = new List<WorldBulletinEvent>();

	public List<WorldBulletinMinor> Minors = new List<WorldBulletinMinor>();
}

internal sealed class WorldBulletinText
{
	public string Title = "";

	public string Major = "";

	public string Short = "";

	public List<string> Minors = new List<string>();
}

// Whose point of view the single bulletin leans toward; captured on the main thread per tick.
internal sealed class WorldBulletinFocus
{
	// The player's kingdom, or the nearest kingdom while the player has none.
	public string PlayerKingdomId = "";

	public HashSet<string> NearbyKingdomIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}

internal static class WorldBulletinPolicy
{
	public static WorldBulletinIllustrationPlan BuildIllustrationPlan(WorldBulletinSelection selection, string identity, string dateText)
	{
		if (selection?.Major == null) return null;
		WorldBulletinEvent lead = selection.Major;
		// One illustration depicts the lead story only; other headlines stay in the newspaper.
		List<WorldBulletinEvent> facts = new List<WorldBulletinEvent> { lead };
		foreach (var fact in selection.MajorFacts ?? new List<WorldBulletinEvent>())
			if (fact != null && fact.Key != lead.Key && facts.Count < MaxMajorFacts &&
				!string.IsNullOrWhiteSpace(lead.Group) && fact.Group == lead.Group && !facts.Any(x => x.Key == fact.Key)) facts.Add(fact);
		var plan = new WorldBulletinIllustrationPlan { Identity = identity ?? "", Title = TitleForKind(lead.Kind), DateText = dateText ?? "" };
		plan.Facts = string.Join("\n", facts.Select(f => (f.Sentence ?? "") + (string.IsNullOrWhiteSpace(f.Detail) ? "" : "\n补充事实：" + f.Detail)));
		foreach (var fact in facts)
			foreach (var person in fact.Participants ?? new List<WorldBulletinParticipant>())
				if (person != null && !string.IsNullOrWhiteSpace(person.HeroId) && plan.Participants.Count < 4 && !plan.Participants.Any(x => x.HeroId == person.HeroId))
					plan.Participants.Add(new WorldBulletinParticipant { HeroId = person.HeroId, Name = person.Name, Role = person.Role });
		return plan;
	}

	public const int WorldMembershipScore = 40;

	public const int HomeMembershipScore = 25;

	public const int WorldTriggerScore = 60;

	public const int PlayerTriggerBaseScore = 30;

	public const int PlayerKingdomBonus = 20;

	public const int PlayerHeroBonus = 30;

	public const int NearbyKingdomBonus = 10;

	// NPC regional knowledge (local text, no LLM).
	public const int NpcDigestDays = 7;

	public const int NpcBriefFactsPerKingdom = 3;

	public const int NpcDetailFacts = 8;

	public const int NpcWorldFacts = 5;

	public const double CollectHours = 24.0;

	public const double CooldownHours = 72.0;

	public const int RetentionDays = 8;

	public const int MaxEvents = 300;

	public const int MaxMinors = 4;
	public const int MinimumMinors = 2;

	public static bool HasEnoughMinorNews(WorldBulletinSelection selection)
	{
		return selection?.Major != null && selection.Minors.Count(m => m?.Events != null && m.Events.Count > 0 && !string.IsNullOrWhiteSpace(m.Sentence)) >= MinimumMinors;
	}

	public const int MaxMajorFacts = 5;

	// Same-group events folded into one minor line before "等N起".
	public const int MaxMinorGroupSentences = 2;

	public const int WeeklyStabilityCap = 15;

	public const int MaxKingdomTemplateFacts = 4;

	private static readonly Regex MinorPrefix = new Regex("^[\\s·•\\-\\*、\\d\\.\\)）]+", RegexOptions.Compiled);

	// Models sometimes put several tags on one line; each tag starts a new line before parsing.
	private static readonly Regex InlineTag = new Regex("(?<!^)(?=\\[(?:TITLE|MAJOR|MINOR|SHORT|M\\d{1,2})\\])", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

	private static readonly Regex IndexedMinorTag = new Regex("^\\[M(\\d{1,2})\\]\\s*(.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private const string ChineseNumeralDigits = "零〇○一二两三四五六七八九壹贰叁肆伍陆柒捌玖";
	private const string ChineseNumeralUnits = "十百千万亿兆拾佰仟萬億";
	private const string NumericMeasureCharacters = "人名位个队军城座村镇日天月年次起件场战门支艘户";
	private static readonly Dictionary<char, int> ChineseNumeralDigitValues = new Dictionary<char, int>
	{
		['零'] = 0, ['〇'] = 0, ['○'] = 0, ['一'] = 1, ['二'] = 2, ['两'] = 2, ['三'] = 3,
		['四'] = 4, ['五'] = 5, ['六'] = 6, ['七'] = 7, ['八'] = 8, ['九'] = 9,
		['壹'] = 1, ['贰'] = 2, ['叁'] = 3, ['肆'] = 4, ['伍'] = 5, ['陆'] = 6,
		['柒'] = 7, ['捌'] = 8, ['玖'] = 9
	};
	private static readonly Dictionary<char, long> ChineseNumeralUnitValues = new Dictionary<char, long>
	{
		['十'] = 10, ['百'] = 100, ['千'] = 1000, ['万'] = 10000, ['亿'] = 100000000,
		['兆'] = 1000000000000L, ['拾'] = 10, ['佰'] = 100, ['仟'] = 1000,
		['萬'] = 10000, ['億'] = 100000000
	};

	public static bool InvolvesKingdom(WorldBulletinEvent e, string kingdomId)
	{
		string id = (kingdomId ?? "").Trim();
		return e?.KingdomIds != null && id.Length > 0 && e.KingdomIds.Any(x => string.Equals((x ?? "").Trim(), id, StringComparison.OrdinalIgnoreCase));
	}

	private static bool IsHome(WorldBulletinEvent e, WorldBulletinFocus focus)
	{
		return e != null && (e.InvolvesPlayer || InvolvesKingdom(e, focus?.PlayerKingdomId));
	}

	private static bool IsNearby(WorldBulletinEvent e, WorldBulletinFocus focus)
	{
		return e?.KingdomIds != null && focus?.NearbyKingdomIds != null && e.KingdomIds.Any(x => !string.IsNullOrWhiteSpace(x) && focus.NearbyKingdomIds.Contains(x.Trim()));
	}

	// One bulletin, leaning toward the player: own deeds +30, home kingdom +20, neighbours +10.
	public static int FocusScore(WorldBulletinEvent e, WorldBulletinFocus focus)
	{
		if (e == null)
		{
			return 0;
		}
		int score = e.Score;
		if (e.InvolvesPlayer)
		{
			score += PlayerHeroBonus;
		}
		if (InvolvesKingdom(e, focus?.PlayerKingdomId))
		{
			score += PlayerKingdomBonus;
		}
		else if (IsNearby(e, focus))
		{
			score += NearbyKingdomBonus;
		}
		return score;
	}

	// Far-away noise needs world-level weight; the player's own and home news gets a lower bar.
	public static bool InScope(WorldBulletinEvent e, WorldBulletinFocus focus)
	{
		return e != null && e.Score >= (IsHome(e, focus) ? HomeMembershipScore : WorldMembershipScore);
	}

	private static bool IsMinorCandidate(WorldBulletinEvent e, WorldBulletinFocus focus)
	{
		return e.InvolvesPlayer || e.Score >= HomeMembershipScore;
	}

	private static string GroupOf(WorldBulletinEvent e)
	{
		string group = (e?.Group ?? "").Trim();
		return group.Length > 0 ? group : "key:" + (e?.Key ?? "");
	}

	private static bool SharesKingdom(WorldBulletinEvent a, WorldBulletinEvent b)
	{
		return a?.KingdomIds != null && b?.KingdomIds != null
			&& a.KingdomIds.Any(x => !string.IsNullOrWhiteSpace(x) && InvolvesKingdom(b, x));
	}


	// Bonuses alone must not turn a skirmish or fief grant into a headline.
	public static bool IsTrigger(WorldBulletinEvent e, WorldBulletinFocus focus)
	{
		return InScope(e, focus) && e.Score >= PlayerTriggerBaseScore && FocusScore(e, focus) >= WorldTriggerScore;
	}

	public static bool TryOpenWindow(WorldBulletinScopeState scope, double triggerHour, double nowHour)
	{
		if (scope == null || scope.WindowEndHour >= 0 || nowHour < scope.CooldownUntilHour)
		{
			return false;
		}
		scope.WindowEndHour = Math.Max(nowHour, triggerHour + CollectHours);
		return true;
	}

	public static bool IsWindowDue(WorldBulletinScopeState scope, double nowHour)
	{
		return scope != null && scope.WindowEndHour >= 0 && nowHour >= scope.WindowEndHour;
	}

	// Earliest unconsumed trigger, used after a cooldown ends while majors were waiting.
	public static double FindPendingTriggerHour(IEnumerable<WorldBulletinEvent> events, WorldBulletinScopeState scope, WorldBulletinFocus focus, double nowHour)
	{
		if (scope == null || events == null)
		{
			return -1;
		}
		double minHour = Math.Max(scope.CutoffHour, nowHour - RetentionDays * 24.0);
		double best = -1;
		foreach (WorldBulletinEvent e in events)
		{
			if (e != null && e.Hour > minHour && e.Hour <= nowHour && IsTrigger(e, focus) && (best < 0 || e.Hour < best))
			{
				best = e.Hour;
			}
		}
		return best;
	}

	// The major story takes the lead's whole group plus every other headline-level fact in the window
	// (related ones first), so two executions in one day become one story instead of lead + footnote.
	// Remaining facts collapse by group into at most MaxMinors lines.
	public static WorldBulletinSelection Select(IEnumerable<WorldBulletinEvent> events, WorldBulletinScopeState scope, WorldBulletinFocus focus, double nowHour)
	{
		if (scope == null || events == null || scope.WindowEndHour < 0)
		{
			return null;
		}
		double minHour = Math.Max(scope.CutoffHour, nowHour - RetentionDays * 24.0);
		HashSet<string> seenSentences = new HashSet<string>(StringComparer.Ordinal);
		List<WorldBulletinEvent> candidates = events
			.Where(e => e != null && e.Hour > minHour && e.Hour <= scope.WindowEndHour && InScope(e, focus) && !string.IsNullOrWhiteSpace(e.Sentence))
			.OrderByDescending(e => FocusScore(e, focus))
			.ThenBy(e => e.Hour)
			.Where(e => seenSentences.Add(e.Sentence.Trim()))
			.ToList();
		// The lead is the strongest headline, not the strongest candidate: a player's raid can outrank a far war
		// by focus bonus yet never be a headline itself, and must not sink the window.
		WorldBulletinEvent lead = candidates.FirstOrDefault(e => IsTrigger(e, focus));
		if (lead == null)
		{
			return null;
		}
		string leadGroup = GroupOf(lead);
		WorldBulletinSelection selection = new WorldBulletinSelection { Major = lead };
		HashSet<WorldBulletinEvent> used = new HashSet<WorldBulletinEvent>();
		void AddMajor(WorldBulletinEvent e)
		{
			if (selection.MajorFacts.Count < MaxMajorFacts && used.Add(e))
			{
				selection.MajorFacts.Add(e);
			}
		}
		AddMajor(lead);
		// The lead's whole story first (its headlines, then supporting facts such as the siege's captives),
		// then other headlines sharing a kingdom, then unrelated headlines.
		foreach (WorldBulletinEvent e in candidates.Where(e => GroupOf(e) == leadGroup).OrderByDescending(e => IsTrigger(e, focus)))
		{
			AddMajor(e);
		}
		foreach (WorldBulletinEvent e in candidates.Where(e => IsTrigger(e, focus)).OrderByDescending(e => SharesKingdom(e, lead)))
		{
			AddMajor(e);
		}
		Dictionary<string, WorldBulletinMinor> minorsByGroup = new Dictionary<string, WorldBulletinMinor>(StringComparer.Ordinal);
		foreach (WorldBulletinEvent e in candidates)
		{
			if (used.Contains(e) || !IsMinorCandidate(e, focus))
			{
				continue;
			}
			string group = GroupOf(e);
			if (!minorsByGroup.TryGetValue(group, out WorldBulletinMinor minor))
			{
				if (selection.Minors.Count >= MaxMinors)
				{
					continue;
				}
				minor = new WorldBulletinMinor();
				minorsByGroup[group] = minor;
				selection.Minors.Add(minor);
			}
			minor.Events.Add(e);
		}
		foreach (WorldBulletinMinor minor in selection.Minors)
		{
			minor.Sentence = BuildMinorSentence(minor.Events);
		}
		return selection;
	}

	public static string BuildMinorSentence(IList<WorldBulletinEvent> events)
	{
		List<string> parts = (events ?? new List<WorldBulletinEvent>()).Take(MaxMinorGroupSentences).Select(e => TrimSentenceEnd(e.Sentence)).ToList();
		int extra = (events?.Count ?? 0) - parts.Count;
		return string.Join("；", parts) + (extra > 0 ? "，另有" + extra + "起同类事件" : "") + "。";
	}

	// ---------- NPC regional knowledge: raw facts, assembled locally, no LLM ----------

	// Recent facts about one kingdom, strongest first (newest breaks ties).
	// onePerGroup keeps the always-on layer short; the on-demand layer lists every fact of a story.
	public static List<WorldBulletinEvent> RecentKingdomFacts(IEnumerable<WorldBulletinEvent> events, string kingdomId, int currentDay, int max, bool onePerGroup = true)
	{
		HashSet<string> groups = new HashSet<string>(StringComparer.Ordinal);
		return (events ?? Enumerable.Empty<WorldBulletinEvent>())
			.Where(e => e != null && e.Day >= currentDay - NpcDigestDays && !string.IsNullOrWhiteSpace(e.Sentence) && InvolvesKingdom(e, kingdomId))
			.OrderByDescending(e => e.Score).ThenByDescending(e => e.Hour)
			.Where(e => !onePerGroup || groups.Add(GroupOf(e)))
			.Take(Math.Max(0, max)).ToList();
	}

	// Headline-level facts anywhere; NPCs only get these when asked about great events.
	public static List<WorldBulletinEvent> RecentWorldFacts(IEnumerable<WorldBulletinEvent> events, int currentDay, int max)
	{
		HashSet<string> groups = new HashSet<string>(StringComparer.Ordinal);
		return (events ?? Enumerable.Empty<WorldBulletinEvent>())
			.Where(e => e != null && e.Day >= currentDay - NpcDigestDays && e.Score >= WorldTriggerScore && !string.IsNullOrWhiteSpace(e.Sentence))
			.OrderByDescending(e => e.Score).ThenByDescending(e => e.Hour)
			.Where(e => groups.Add(GroupOf(e)))
			.Take(Math.Max(0, max)).ToList();
	}

	public static string DaysAgoLabel(int eventDay, int currentDay)
	{
		int days = Math.Max(0, currentDay - eventDay);
		return days == 0 ? "今日" : days == 1 ? "昨日" : days + "日前";
	}

	// Always-on layer, same header as the legacy weekly short block so the prompt contract stays put.
	public static string BuildNpcBriefBlock(IList<KeyValuePair<string, string>> kingdoms, IEnumerable<WorldBulletinEvent> events, int currentDay)
	{
		List<WorldBulletinEvent> source = (events ?? Enumerable.Empty<WorldBulletinEvent>()).ToList();
		StringBuilder sb = new StringBuilder();
		int written = 0;
		foreach (KeyValuePair<string, string> kingdom in kingdoms ?? new List<KeyValuePair<string, string>>())
		{
			List<WorldBulletinEvent> facts = RecentKingdomFacts(source, kingdom.Key, currentDay, NpcBriefFactsPerKingdom);
			if (facts.Count == 0)
			{
				continue;
			}
			if (written == 0)
			{
				sb.AppendLine("【近期三个王国发生的事】");
				sb.AppendLine("以下为最近三个相关王国的事");
			}
			written++;
			sb.Append("- ").Append(kingdom.Value).Append("：")
				.AppendLine(string.Join("；", facts.Select(e => DaysAgoLabel(e.Day, currentDay) + TrimSentenceEnd(e.Sentence))) + "。");
		}
		return written > 0 ? sb.ToString().TrimEnd() : "";
	}

	// On-demand layer: one kingdom's facts with the captured details. header is one of the Npc*Header constants.
	public static string BuildNpcDetailBlock(string header, string kingdomName, string kingdomId, IEnumerable<WorldBulletinEvent> events, int currentDay)
	{
		List<WorldBulletinEvent> facts = RecentKingdomFacts(events, kingdomId, currentDay, NpcDetailFacts, onePerGroup: false);
		if (facts.Count == 0)
		{
			return "";
		}
		return "【" + header + "】\n标题：" + (kingdomName ?? "").Trim() + "近" + NpcDigestDays + "日大事\n" + string.Join("\n", facts.Select(e => FormatFactLine(e, currentDay)));
	}

	// Header names reuse the legacy "完整周报" headers: the scene prompt splitter (ShoutBehavior.IsSceneWeeklyFullReportHeader)
	// ends the rule section on exactly these, and the NPC dedup keys stay unchanged.
	public const string NpcKingdomHeader = "NPC所属王国完整周报";

	public const string NpcWorldHeader = "世界完整周报";

	public const string NpcSurroundingsHeader = "周边相关王国完整周报";

	// excludeKingdomId: the NPC's own kingdom block already lists those facts; world headlines skip them.
	public static string BuildNpcWorldBlock(IEnumerable<WorldBulletinEvent> events, int currentDay, string latestBulletinTitle, string latestBulletinShort, int latestBulletinDay, string excludeKingdomId = null)
	{
		List<WorldBulletinEvent> facts = RecentWorldFacts(
			(events ?? Enumerable.Empty<WorldBulletinEvent>()).Where(e => string.IsNullOrWhiteSpace(excludeKingdomId) || !InvolvesKingdom(e, excludeKingdomId)),
			currentDay, NpcWorldFacts);
		List<string> lines = facts.Select(e => FormatFactLine(e, currentDay)).ToList();
		if (latestBulletinDay >= 0 && latestBulletinDay <= currentDay && latestBulletinDay >= currentDay - NpcDigestDays && !string.IsNullOrWhiteSpace(latestBulletinShort))
		{
			lines.Add("- 最新快报《" + (latestBulletinTitle ?? "").Trim() + "》：" + latestBulletinShort.Trim());
		}
		return lines.Count == 0 ? "" : "【" + NpcWorldHeader + "】\n标题：近" + NpcDigestDays + "日天下大事\n" + string.Join("\n", lines);
	}

	private static string FormatFactLine(WorldBulletinEvent e, int currentDay)
	{
		string detail = string.IsNullOrWhiteSpace(e.Detail) ? "" : "（" + e.Detail.Trim() + "）";
		return "- " + DaysAgoLabel(e.Day, currentDay) + TrimSentenceEnd(e.Sentence) + detail + "。";
	}

	public static void CompletePublish(WorldBulletinScopeState scope, double nowHour, string majorKey)
	{
		scope.CutoffHour = scope.WindowEndHour;
		scope.WindowEndHour = -1;
		scope.CooldownUntilHour = nowHour + CooldownHours;
		scope.Sequence++;
		scope.LastMajorKey = majorKey ?? "";
		scope.LastPublishHour = nowHour;
	}

	public static void AbandonWindow(WorldBulletinScopeState scope)
	{
		scope.CutoffHour = scope.WindowEndHour;
		scope.WindowEndHour = -1;
	}

	// Returns the delta that may still be applied this week without exceeding the per-kingdom cap.
	public static int ClampWeeklyStability(int currentTotal, int delta, out int newTotal)
	{
		int target = Math.Max(-WeeklyStabilityCap, Math.Min(WeeklyStabilityCap, currentTotal + delta));
		newTotal = target;
		return target - currentTotal;
	}

	public static void Prune(List<WorldBulletinEvent> events, int currentDay)
	{
		events?.RemoveAll(e => e == null || e.Day < currentDay - RetentionDays);
		if (events != null && events.Count > MaxEvents)
		{
			events.RemoveRange(0, events.Count - MaxEvents);
		}
	}

	// Minors come back as [M1]..[Mn] and land in their slot; an unindexed [MINOR] fills the next empty slot.
	// Result.Minors always has expectedMinors entries, "" meaning the model skipped that line.
	public static WorldBulletinText ParseResponse(string raw, int expectedMinors)
	{
		string text = (raw ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		if (text.Length == 0)
		{
			return null;
		}
		text = InlineTag.Replace(text, "\n");
		WorldBulletinText result = new WorldBulletinText();
		for (int i = 0; i < expectedMinors; i++)
		{
			result.Minors.Add("");
		}
		StringBuilder major = new StringBuilder();
		string section = "";
		int minorSlot = -1;
		foreach (string rawLine in text.Split('\n'))
		{
			string line = rawLine.Trim();
			if (line.Length == 0)
			{
				continue;
			}
			if (TryStripTag(line, "[TITLE]", out string rest))
			{
				section = "title";
				result.Title = rest;
			}
			else if (TryStripTag(line, "[MAJOR]", out rest))
			{
				section = "major";
				AppendLine(major, rest);
			}
			else if (IndexedMinorTag.Match(line) is Match indexed && indexed.Success)
			{
				section = "minor";
				minorSlot = int.Parse(indexed.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) - 1;
				SetMinor(result, minorSlot, indexed.Groups[2].Value);
			}
			else if (TryStripTag(line, "[MINOR]", out rest))
			{
				section = "minor";
				minorSlot = result.Minors.FindIndex(x => x.Length == 0);
				SetMinor(result, minorSlot, rest);
			}
			else if (TryStripTag(line, "[SHORT]", out rest))
			{
				section = "short";
				result.Short = rest;
			}
			else if (section == "major")
			{
				AppendLine(major, line);
			}
			else if (section == "short")
			{
				result.Short = (result.Short + line).Trim();
			}
			else if (section == "minor" && minorSlot >= 0 && minorSlot < result.Minors.Count && result.Minors[minorSlot].Length == 0)
			{
				SetMinor(result, minorSlot, line);
			}
		}
		result.Major = major.ToString().Trim();
		if (result.Major.Length == 0)
		{
			return null;
		}
		result.Title = Truncate(result.Title, 20);
		return result;
	}

	public static WorldBulletinText BuildTemplate(WorldBulletinSelection selection)
	{
		List<WorldBulletinEvent> facts = selection?.MajorFacts?.Count > 0
			? selection.MajorFacts
			: (selection?.Major != null ? new List<WorldBulletinEvent> { selection.Major } : new List<WorldBulletinEvent>());
		WorldBulletinText text = new WorldBulletinText
		{
			Title = TitleForKind(selection?.Major?.Kind),
			Major = string.Join("", facts.Select(e => TrimSentenceEnd(e.Sentence) + "。"))
		};
		foreach (WorldBulletinMinor minor in selection?.Minors ?? new List<WorldBulletinMinor>())
		{
			text.Minors.Add(minor.Sentence);
		}
		text.Short = BuildShortFromFacts(text.Major, text.Minors);
		return text;
	}

	// LLM output may drop minors; template sentences fill the gap so no selected fact is lost.
	public static List<string> MergeMinors(IList<string> generated, IList<WorldBulletinMinor> selected, out int polished)
	{
		polished = 0;
		List<string> result = new List<string>();
		for (int i = 0; i < (selected?.Count ?? 0); i++)
		{
			string value = (generated != null && i < generated.Count) ? (generated[i] ?? "").Trim() : "";
			if (value.Length > 0)
			{
				polished++;
			}
			result.Add(value.Length > 0 ? value : selected[i].Sentence);
		}
		return result;
	}

	public static string BuildSystemPrompt(int majorFactCount, int minorCount)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("你为一个中世纪世界撰写即时快报。只能使用给出的事实与背景，不得添加其中没有的人物、地点、结果或伤亡数字；推测影响时只能用“或将”“恐怕”这类审慎措辞。");
		sb.AppendLine("以“交易/买卖”“王国决议”等方式移交、写明并非攻城的领地，不得写成攻陷或夺城。不要使用原版默认大陆名，需要指代大范围时只写“大陆”或具体王国名。");
		sb.AppendLine("标记为出城战的事实只涉及本次出城交战的参战部队：只能写该部队被击退、击败或伤亡，不得扩大为围城军、守军或整支军团覆灭；除非事实明确确认整支军团被消灭，否则禁止写“击败全军”“击溃军团”等结论。");
		if (majorFactCount > 1)
		{
			sb.AppendLine("【大事件】给出的" + majorFactCount + "条事实必须合写成同一篇纪要：以第1条为主线，其余事实写明与主线的关联（同一人所为、同一战事、同一王国的连锁反应）；确实无关时用“与此同时”并入，不得分成几篇，也不得遗漏任何一条。");
		}
		sb.AppendLine("严格按以下格式输出，不要输出任何其他内容：");
		sb.AppendLine("[TITLE] 不超过14个字的标题，概括整篇大事件");
		sb.AppendLine("[MAJOR] " + (majorFactCount > 1 ? "320到480字" : "260到400字") + "的纪要，分两到三段：先交代来龙去脉与相关人物身份，再写经过与结果，最后写对相关王国、家族或局势的可能影响；善用给出的细节与背景，文风简练庄重，不写对白");
		for (int i = 1; i <= minorCount; i++)
		{
			sb.AppendLine("[M" + i + "] 第" + i + "条小消息改写成一句通顺的话，不超过60字；一条里有多件同类事实时合并成一句");
		}
		sb.Append("[SHORT] 不超过100字的局势摘要，供旁人转述");
		return sb.ToString();
	}

	// facts' Detail and kingdomContext are writer-only background; nothing here is shown verbatim.
	public static string BuildUserPrompt(string scopeLine, string dateText, WorldBulletinSelection selection, IEnumerable<string> kingdomContext)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine(scopeLine);
		sb.AppendLine("当前日期：" + dateText);
		sb.AppendLine("【大事件】");
		List<WorldBulletinEvent> facts = selection.MajorFacts.Count > 0 ? selection.MajorFacts : new List<WorldBulletinEvent> { selection.Major };
		for (int i = 0; i < facts.Count; i++)
		{
			sb.Append(i + 1).Append(". ").Append(facts[i].Sentence.Trim());
			if (!string.IsNullOrWhiteSpace(facts[i].Detail))
			{
				sb.Append("（细节：").Append(facts[i].Detail.Trim()).Append("）");
			}
			sb.AppendLine();
		}
		List<string> context = (kingdomContext ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
		if (context.Count > 0)
		{
			sb.AppendLine("【相关王国现状】");
			foreach (string line in context)
			{
				sb.AppendLine("· " + line.Trim());
			}
		}
		for (int i = 0; i < selection.Minors.Count; i++)
		{
			if (i == 0)
			{
				sb.AppendLine("【小消息】");
			}
			sb.AppendLine("M" + (i + 1) + ". " + selection.Minors[i].Sentence);
		}
		return sb.ToString().TrimEnd();
	}

	public static string BuildBody(string major, IEnumerable<string> minors)
	{
		StringBuilder sb = new StringBuilder();
		sb.Append("【大事件】\n").Append((major ?? "").Trim());
		List<string> list = (minors ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
		if (list.Count > 0)
		{
			sb.Append("\n\n【其他消息】");
			foreach (string minor in list)
			{
				sb.Append("\n· ").Append(minor.Trim());
			}
		}
		return sb.ToString();
	}

	public static string BuildShortFromFacts(string major, IEnumerable<string> minors)
	{
		List<string> parts = new List<string> { TrimSentenceEnd(major) };
		parts.AddRange((minors ?? Enumerable.Empty<string>()).Select(TrimSentenceEnd));
		return Truncate(string.Join("；", parts.Where(x => x.Length > 0)) + "。", 140);
	}

	public static string BuildKingdomTemplate(string kingdomName, IEnumerable<WorldBulletinEvent> events)
	{
		string name = string.IsNullOrWhiteSpace(kingdomName) ? "该王国" : kingdomName.Trim();
		List<string> facts = (events ?? Enumerable.Empty<WorldBulletinEvent>())
			.Where(e => e != null && !string.IsNullOrWhiteSpace(e.Sentence))
			.OrderByDescending(e => e.Score).ThenByDescending(e => e.Hour)
			.Select(e => TrimSentenceEnd(e.Sentence)).Distinct(StringComparer.Ordinal)
			.Take(MaxKingdomTemplateFacts).ToList();
		if (facts.Count == 0)
		{
			return name + "近来局势平稳，未见重大变故。";
		}
		return name + "近况：" + string.Join("；", facts) + "。";
	}

	public static string TitleForKind(string kind)
	{
		switch ((kind ?? "").Trim())
		{
		case "war_declared":
			return "烽烟再起";
		case "peace_made":
			return "干戈暂息";
		case "settlement_siege":
			return "城池易主";
		case "settlement_transfer":
			return "领地移交";
		case "fief_grant":
			return "封地授予";
		case "battle":
		case "siege_battle":
			return "战场急报";
		case "sally_out_battle":
			return "出城战报";
		case "ruler_killed":
			return "君王陨落";
		case "lord_killed":
			return "贵族殒命";
		case "lord_captured":
			return "贵胄被俘";
		case "kingdom_destroyed":
			return "王国覆灭";
		case "kingdom_created":
			return "新国崛起";
		case "kingdom_rebellion":
			return "叛旗高举";
		case "civil_war":
			return "内战爆发";
		case "civil_war_resolution":
			return "内战结束";
		case "civil_war_politics":
			return "派系交涉";
		case "coup_success":
			return "政变夺位";
		case "coup_failure":
			return "政变失败";
		case "raid":
			return "村庄遭劫";
		default:
			return "时事快报";
		}
	}

	public static string Truncate(string text, int max)
	{
		string value = (text ?? "").Trim();
		return value.Length <= max ? value : value.Substring(0, max).TrimEnd();
	}

	private static string TrimSentenceEnd(string text)
	{
		return (text ?? "").Trim().TrimEnd('。', '；', '，', '.', ';', ',').Trim();
	}

	private static bool TryStripTag(string line, string tag, out string rest)
	{
		rest = "";
		if (!line.StartsWith(tag, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		rest = line.Substring(tag.Length).Trim();
		return true;
	}

	private static void AppendLine(StringBuilder builder, string line)
	{
		if (string.IsNullOrWhiteSpace(line))
		{
			return;
		}
		if (builder.Length > 0)
		{
			builder.Append('\n');
		}
		builder.Append(line.Trim());
	}

	private static void SetMinor(WorldBulletinText result, int slot, string line)
	{
		string value = MinorPrefix.Replace(line ?? "", "").Trim();
		if (value.Length > 0 && slot >= 0 && slot < result.Minors.Count)
		{
			result.Minors[slot] = Truncate(value, 80);
		}
	}
}
