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

	public string GameDate = "";

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
	// Facts captured after selection at the same game hour must remain eligible for the next window.
	public List<string> DeferredFactKeys = new List<string>();
}

// Everything the bulletin system persists, saved as one JSON chunk.
internal sealed class WorldBulletinSaveState
{
    // Optional, within the existing chunked JSON. Old saves adopt the current mode without replay.
    public bool? CollectionBulletinMode;
    public double WeeklyCollectionStartHour = -1;
    public int WeeklyCollectionStartSequence = -1;
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
	// Detached publication window, including facts below the front-page score threshold.
	public List<WorldBulletinEvent> WindowFacts = new List<WorldBulletinEvent>();
	public double WindowEndHour = -1;
	public HashSet<string> ReportedKeys = new HashSet<string>(StringComparer.Ordinal);
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
		// One draw per recorded story, not per supporting fact. Stable per issue, so reopening
		// or retrying uses the same frozen scene rather than requesting another random image.
		var stories = (selection.MajorFacts ?? new List<WorldBulletinEvent>()).Concat(new[] { selection.Major })
			.Where(f => f != null && !string.IsNullOrWhiteSpace(f.Sentence))
			.GroupBy(GroupOf, StringComparer.Ordinal).Select(g => g.First()).OrderBy(GroupOf, StringComparer.Ordinal).ToList();
		WorldBulletinEvent lead = stories.Count == 0 ? selection.Major : stories[IllustrationStoryIndex(identity, stories.Count)];
		// Other stories remain in the newspaper; only this story's facts and participants are illustrated.
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

	internal static int IllustrationStoryIndex(string identity, int count)
	{
		if (count <= 1) return 0;
		using (var hash = System.Security.Cryptography.SHA256.Create())
		{
			byte[] bytes = hash.ComputeHash(Encoding.UTF8.GetBytes("bulletin-art-story-v1:" + (identity ?? "")));
			uint value = (uint)bytes[0] | (uint)bytes[1] << 8 | (uint)bytes[2] << 16 | (uint)bytes[3] << 24;
			return (int)(value % (uint)count);
		}
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


	private static readonly Regex MinorPrefix = new Regex("^[\\s·•\\-\\*、\\d\\.\\)）]+", RegexOptions.Compiled);

	// Models sometimes put several tags on one line; each tag starts a new line before parsing.
	private static readonly Regex InlineTag = new Regex("(?<!^)(?=\\[(?:TITLE|MAJOR|MINOR|SHORT|M\\d{1,2})\\])", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

	private static readonly Regex IndexedMinorTag = new Regex("^\\[M(\\d{1,2})\\]\\s*(.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private const string BattleQuantity = @"(?:\d+(?:[,，]\d{3})*(?:\.\d+)?|[零〇○一二两三四五六七八九十百千万亿壹贰叁肆伍陆柒捌玖拾佰仟萬億廿卅]+|几|数|若干)(?:余|多|来|左右|上下)?";
    private const string BattlePersonUnit = @"(?:人|名|位|兵|骑|骑兵|步兵|士兵|兵卒|军士|将士|军人|精兵)";
    private const string BattleCountTerm = @"(?:兵力|兵马|参战|投入战斗|阵亡|战死|负伤|受伤|伤亡|死伤|伤员|伤者|亡者|折损|损失|丧命)";
    private const string BattleCountGap = @"[^\d零〇○一二两三四五六七八九十百千万亿壹贰叁肆伍陆柒捌玖拾佰仟萬億廿卅，,、。.!！?？；;\r\n]{0,10}";
    private static readonly Regex BattleNumericRecital = new Regex(
        BattleCountTerm + BattleCountGap + @"(?:百分之)?" + BattleQuantity + @"\s*(?:" + BattlePersonUnit + @"|[%％成]|(?=[，,、。.!！?？；;\s]|$))"
        + "|" + BattleQuantity + @"\s*" + BattlePersonUnit + BattleCountGap + BattleCountTerm
        + "|" + BattleQuantity + @"\s*(?:名|位)?(?:士兵|兵卒|骑兵|步兵|军士|军人|将士|精兵|兵马|守军)"
        + @"|(?:率领|领军|领兵|出动|调集|集结|派出|守军|敌军|大军|军团|部队)" + BattleCountGap + BattleQuantity + @"\s*" + BattlePersonUnit
        + "|" + BattleQuantity + @"\s*" + BattlePersonUnit + @"\s*(?:对|打|迎战|对阵)\s*" + BattleQuantity + @"\s*" + BattlePersonUnit
        + "|" + BattleQuantity + @"\s*(?:" + BattlePersonUnit + @")?\s*(?:对|打|迎战|对阵)\s*" + BattleQuantity + @"\s*(?:" + BattlePersonUnit + @")?(?=[，,、。.!！?？；;\s]|$)",
        RegexOptions.Compiled);
    private static readonly Regex LegacyBattleTroopSuffix = new Regex(
        @"[，,]\s*双方\s*(?:约|共|总计)?\s*" + BattleQuantity + @"\s*人\s*参战(?=[。.!！?？]?\s*$)", RegexOptions.Compiled);
    private static readonly Regex CapturedBattleCountPrefix = new Regex(
        @"^\s*(?:胜方|败方)\s*" + BattleQuantity + @"\s*" + BattlePersonUnit, RegexOptions.Compiled);

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
	// Apply current editorial limits when reading retained old-save facts as well as at capture.
	// No migration scan, saved record rewrite, or change to already published issue text.
	internal static int AdjustBaseScore(string kind, int score) => kind switch
	{
		"alliance_formed" => Math.Min(score, 45),
		"alliance_ended" => Math.Min(score, 40),
		"diplomatic_declaration" => Math.Min(score, 30),
		_ => score
	};
	private static int EffectiveBaseScore(WorldBulletinEvent e) => e == null ? 0 : AdjustBaseScore(e.Kind, e.Score);

	public static int FocusScore(WorldBulletinEvent e, WorldBulletinFocus focus)
	{
		if (e == null)
		{
			return 0;
		}
		int score = EffectiveBaseScore(e);
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
		return e != null && EffectiveBaseScore(e) >= (IsHome(e, focus) ? HomeMembershipScore : WorldMembershipScore);
	}

	private static bool IsMinorCandidate(WorldBulletinEvent e, WorldBulletinFocus focus)
	{
		return e.InvolvesPlayer || EffectiveBaseScore(e) >= HomeMembershipScore;
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
		return InScope(e, focus) && EffectiveBaseScore(e) >= PlayerTriggerBaseScore && FocusScore(e, focus) >= WorldTriggerScore;
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
		var deferred = new HashSet<string>(scope.DeferredFactKeys ?? new List<string>(),StringComparer.Ordinal);
		double best = -1;
		foreach (WorldBulletinEvent e in events)
		{
			if (e != null && (e.Hour > minHour || (deferred.Contains(e.Key) && e.Hour > nowHour - RetentionDays * 24.0)) && e.Hour <= nowHour && IsTrigger(e, focus) && (best < 0 || e.Hour < best))
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
		var deferred = new HashSet<string>(scope.DeferredFactKeys ?? new List<string>(),StringComparer.Ordinal);
		List<WorldBulletinEvent> window = events
			.Where(e => e != null && (e.Hour > minHour || (deferred.Contains(e.Key) && e.Hour > nowHour - RetentionDays * 24.0)) && e.Hour <= scope.WindowEndHour && !string.IsNullOrWhiteSpace(e.Sentence))
			.Select(e => new WorldBulletinEvent { Key=e.Key, Kind=e.Kind, Day=e.Day, Hour=e.Hour,
				Score=EffectiveBaseScore(e), Sentence=e.Sentence, GameDate=e.GameDate, Group=e.Group, Detail=e.Detail,
				InvolvesPlayer=e.InvolvesPlayer, KingdomIds=new List<string>(e.KingdomIds ?? new List<string>()),
				Participants=(e.Participants ?? new List<WorldBulletinParticipant>()).Where(p => p != null).Select(p => new WorldBulletinParticipant { HeroId=p.HeroId, Name=p.Name, Role=p.Role }).ToList() })
			.ToList();
		List<WorldBulletinEvent> candidates = window.Where(e => InScope(e, focus))
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
		WorldBulletinSelection selection = new WorldBulletinSelection { Major = lead, WindowFacts = window, WindowEndHour = scope.WindowEndHour };
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
			// Only facts actually handed to the short-news writer are reported; group overflow goes to recent records.
			if (minor.Events.Count < MaxMinorGroupSentences) minor.Events.Add(e);
		}
		foreach (WorldBulletinMinor minor in selection.Minors)
		{
			minor.Sentence = BuildMinorSentence(minor.Events);
		}
		foreach (var fact in selection.MajorFacts.Concat(selection.Minors.SelectMany(m => m.Events)))
			selection.ReportedKeys.Add(fact.Key);
		return selection;
	}

	public static string BuildMinorSentence(IList<WorldBulletinEvent> events)
	{
		List<string> parts = (events ?? new List<WorldBulletinEvent>()).Take(MaxMinorGroupSentences).Select(e => TrimSentenceEnd(BuildWriterFactSentence(e))).ToList();
		int extra = (events?.Count ?? 0) - parts.Count;
		return string.Join("；", parts) + (extra > 0 ? "，另有" + extra + "起同类事件" : "") + "。";
	}

	// ---------- NPC regional knowledge: raw facts, assembled locally, no LLM ----------

    internal static bool IsDiplomacyFact(string kind, string key)
        => string.Equals(kind, "diplomatic_declaration", StringComparison.OrdinalIgnoreCase)
            || (key ?? "").StartsWith("declaration:", StringComparison.OrdinalIgnoreCase);

    // A declaration being captured/published does not grant an NPC knowledge of its contents.
    // Missing identity or diplomacy owner is closed; unrelated news retains its existing rules.
    internal static bool IsNpcFactVisible(string kind, string key, ISet<string> knownDocumentIds)
    {
        if (!IsDiplomacyFact(kind, key)) return true;
        const string prefix = "declaration:";
        return knownDocumentIds != null && !string.IsNullOrWhiteSpace(key)
            && key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && key.Length > prefix.Length && knownDocumentIds.Contains(key.Substring(prefix.Length));
    }

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
	public static string BuildNpcWorldBlock(IEnumerable<WorldBulletinEvent> events, int currentDay, string latestBulletinTitle, string latestBulletinShort, int latestBulletinDay, string excludeKingdomId = null, string latestBulletinAnecdote = "")
	{
		List<WorldBulletinEvent> facts = RecentWorldFacts(
			(events ?? Enumerable.Empty<WorldBulletinEvent>()).Where(e => string.IsNullOrWhiteSpace(excludeKingdomId) || !InvolvesKingdom(e, excludeKingdomId)),
			currentDay, NpcWorldFacts);
		List<string> lines = facts.Select(e => FormatFactLine(e, currentDay)).ToList();
		if (latestBulletinDay >= 0 && latestBulletinDay <= currentDay && latestBulletinDay >= currentDay - NpcDigestDays)
		{
			if (!string.IsNullOrWhiteSpace(latestBulletinShort))
				lines.Add("- 最新快报事实摘要：" + latestBulletinShort.Trim());
			string anecdote = NormalizeBulletinAnecdote(latestBulletinAnecdote);
			if (anecdote.Length > 0)
				lines.Add("- 【快报轶闻】来源：快报《" + (latestBulletinTitle ?? "").Trim() + "》。" + anecdote
					+ "（报刊叙事加工，可以用“听说”“报上说”转述；不作为已确认事实，不据此认定伤病、关系变化或外交执行结果。）");
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
			Major = string.Join("", facts.Select(e => TrimSentenceEnd(BuildWriterFactSentence(e)) + "。"))
		};
		foreach (WorldBulletinMinor minor in selection?.Minors ?? new List<WorldBulletinMinor>())
		{
			text.Minors.Add(BuildWriterMinorSentence(minor));
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
			result.Add(value.Length > 0 ? value : BuildWriterMinorSentence(selected[i]));
		}
		return result;
	}

    internal static bool IsBattleKind(string kind)
        => string.Equals(kind, "battle", StringComparison.OrdinalIgnoreCase)
            || string.Equals(kind, "siege_battle", StringComparison.OrdinalIgnoreCase)
            || string.Equals(kind, "sally_out_battle", StringComparison.OrdinalIgnoreCase);

    internal static bool ContainsBattleNumericRecital(string text)
        => !string.IsNullOrWhiteSpace(text) && BattleNumericRecital.IsMatch(text);

    // Project retained old facts for writing without rewriting their saved numeric evidence.
    private static string BuildWriterFactSentence(WorldBulletinEvent fact)
        => IsBattleKind(fact?.Kind) ? LegacyBattleTroopSuffix.Replace(fact.Sentence ?? "", "") : fact?.Sentence ?? "";

    private static string BuildWriterMinorSentence(WorldBulletinMinor minor)
        => minor?.Events?.Any(f => IsBattleKind(f?.Kind)) == true
            ? BuildMinorSentence(minor.Events) : minor?.Sentence ?? "";

    private static string BuildWriterFactDetail(WorldBulletinEvent fact)
    {
        if (!IsBattleKind(fact?.Kind)) return fact?.Detail ?? "";
        return string.Join("；", (fact.Detail ?? "").Split(new[] { '；', ';' })
            .Where(part => !CapturedBattleCountPrefix.IsMatch(part) && !ContainsBattleNumericRecital(part)));
    }

    // A rejected issue falls back as a whole, so its SHORT cannot retell rejected prose.
    internal static WorldBulletinText ValidateGeneratedText(WorldBulletinSelection selection, WorldBulletinText generated, out string rejectedSection)
    {
        rejectedSection = "";
        if (generated == null || selection == null) return generated;
        bool majorBattle = IsBattleKind(selection.Major?.Kind) || selection.MajorFacts.Any(f => IsBattleKind(f?.Kind));
        bool anyBattle = majorBattle || selection.Minors.Any(m => m?.Events?.Any(f => IsBattleKind(f?.Kind)) == true);
        if (!anyBattle) return generated;
        if (ContainsBattleNumericRecital(generated.Title)) rejectedSection = "TITLE";
        else if (majorBattle && ContainsBattleNumericRecital(generated.Major)) rejectedSection = "MAJOR";
        else if (ContainsBattleNumericRecital(generated.Short)) rejectedSection = "SHORT";
        else
        {
            for (int i = 0; i < selection.Minors.Count && i < (generated.Minors?.Count ?? 0); i++)
            {
                if (selection.Minors[i]?.Events?.Any(f => IsBattleKind(f?.Kind)) == true && ContainsBattleNumericRecital(generated.Minors[i]))
                {
                    rejectedSection = "M" + (i + 1);
                    break;
                }
            }
        }
        return rejectedSection.Length == 0 ? generated : null;
    }

    internal const string BattleWritingRequirements = "战事正文禁止兵力、参战、阵亡、负伤或伤亡数字对账，不得写“X人对Y人”“几人打几人”，也不得把这些数字改成中文数量词继续罗列。只叙述交战方、地点、行动、胜负和已确认的后果；此限制不涉及日期、标题序号等非战事数量。";

    internal const string LegacyDefaultWritingRequirements = "战斗报道不要写成几人对几人、多少人打多少人的兵力对账，也不要连续罗列双方参战、阵亡和负伤人数。用交战方、地点、行动、胜负及已经确认的后果组织叙述；人数只用于核对事实，不机械照抄。不得仅凭人数自行宣称全歼、惨胜或改变国运。";
    internal const string LegacyNarrativeWritingRequirements = "以事件素材为骨架，写成生动的中世纪报刊报道，不要机械复述数据。\n"
        + "可以合理补写现场动作、短对白、人物反应和战场轶事，例如拳脚冲突、牙齿被打落、怒骂、惊慌、嘲讽或狼狈退场；这些是报道的叙事加工，不代表游戏机制实际发生变化。\n"
        + "每篇自然选用一到两个有趣细节，变化叙述角度，不要每场都使用同一桥段，也不要固定照抄示例。对白应短，符合人物身份和当前情境，避免整篇变成小说对话。\n"
        + "战事不要写成几人对几人，不连续罗列参战和伤亡人数；重点写交战方、现场交锋、胜负与后续反应。\n"
        + "宴会、竞技、政策和外交也可以补写相应的现场气氛、言语与反应。\n"
        + "保留素材中的主体、地点和重大结果，不另造死亡、俘虏、领土易主、宣战或结盟。出城战不能扩大成整支军团覆灭；外交宣言中的主张不能直接变成已经执行的结果。\n"
        + "多件事件有关联时串联，无关时自然转场，不为了衔接编造因果。已有明确记录的对白或遗言不得被补写内容替换。";

    internal const string DefaultWritingRequirements = "以事件素材为骨架，写成生动的中世纪报刊报道，不要机械复述数据。\n"
        + "可以合理补写现场动作、短对白、人物反应和战场轶事；这些是报道的叙事加工，不代表游戏机制实际发生变化。只有素材确认两人确实参与同场交战，且身份与情境合理时，才可以补写两人直接交锋，例如拳脚冲突、牙齿被打落或怒骂退开；不能把远处君主或其他未参战人物拉到现场，也不能改变胜负、生死或俘虏结果。\n"
        + "允许适度讥讽和粗粝感，可以写怒骂、嘲弄、丢脸与狼狈，也可以写勇气、机智和体面。对白应短，符合中世纪人物身份与当前情境，避免现代段子，避免整篇变成小说对话。\n"
        + "每篇自然选用一到两个有趣细节，变化叙述角度，不要每场都使用同一桥段，不固定照抄示例，也不要每篇都靠打架取乐。\n"
        + "自由选择切入点，可从一个动作、一句短对白、旁观者反应或战后场面开篇，再自然交代主体、地点、经过和结果；不固定按背景、经过、影响的顺序写，没有值得写的局势影响时，不强行补一段宏大评价。\n"
        + BattleWritingRequirements + "\n"
        + "宴会、竞技、政策和外交也可以补写相应的现场气氛、言语与反应。\n"
        + "保留素材中的主体、地点和重大结果，不另造死亡、俘虏、领土易主、宣战或结盟。出城战不能扩大成整支军团覆灭；外交宣言中的主张不能直接变成已经执行的结果。\n"
        + "多件事件有关联时串联，无关时自然转场，不为了衔接编造因果。已有明确记录的对白或遗言不得被补写内容替换。";

    internal static string NormalizeBulletinAnecdote(string text)
    {
        string value = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        if (value.Length <= 100) return value;
        // Do not cut an emoji/non-BMP character into an invalid save-string surrogate.
        int length = char.IsHighSurrogate(value[99]) && char.IsLowSurrogate(value[100]) ? 99 : 100;
        return value.Substring(0, length).TrimEnd();
    }

	public static string BuildSystemPrompt(int majorFactCount, int minorCount, string writingRequirements = null)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("你为一个中世纪世界撰写即时快报，以给出的事件与背景为骨架，保持报刊纪实语气。允许合理补写动作、短对白和现场反应，正文自然呈现，无需每句加“据说”或“未经核实”；叙事加工不代表游戏机制实际发生变化。保留素材中的主体、地点和重大结果，不另造死亡、俘虏、领土易主、宣战或结盟，不编造伤亡数字；已有明确记录的对白或遗言不得被补写替换。外交宣言的主张不等于已经执行的结果；推测影响时只能用“或将”“恐怕”这类审慎措辞。");
        sb.AppendLine(BattleWritingRequirements);
		sb.AppendLine("各类事件每篇自然选用一到两个有趣细节，变化叙述角度，不固定照抄示例或反复套用同一桥段，也不强制发生人物冲突。");
		sb.AppendLine("只有素材确认两人确实参与同场交战，且身份与情境合理时，才可以作为报刊轶事补写两人直接交锋；不能把远处君主或其他未参战人物拉到现场，也不能改变胜负、生死或俘虏结果。允许适度讥讽和粗粝感，也可表现勇气、机智和体面；保持中世纪人物口吻，避免现代段子，不要每篇都靠打架取乐。");
		sb.AppendLine("以“交易/买卖”“王国决议”等方式移交、写明并非攻城的领地，不得写成攻陷或夺城。不要使用原版默认大陆名，需要指代大范围时只写“大陆”或具体王国名。");
		sb.AppendLine("标记为出城战的事实只涉及本次出城交战的参战部队：只能写该部队被击退、击败或伤亡，不得扩大为围城军、守军或整支军团覆灭；除非事实明确确认整支军团被消灭，否则禁止写“击败全军”“击溃军团”等结论。");
		if (majorFactCount > 1)
		{
			sb.AppendLine("【大事件】给出的" + majorFactCount + "条事实必须合写成同一篇纪要：以第1条为主线，有明确关联时串联，其余无关事实自然转场并入，不为了衔接编造因果；不得分成几篇，也不得遗漏任何一条。");
		}
		sb.AppendLine("严格按以下格式输出，不要输出任何其他内容：");
		sb.AppendLine("[TITLE] 不超过14个字的标题，概括整篇大事件");
		sb.AppendLine("[MAJOR] " + (majorFactCount > 1 ? "320到480字" : "260到400字") + "的纪要，分两到三段；自由选择切入点，可从一个动作、一句短对白、旁观者反应或战后场面开篇，再自然交代主体、地点、经过和结果，不固定叙述顺序；没有值得写的局势影响时，不强行补一段宏大评价；善用细节与背景，避免整篇变成小说对话");
		for (int i = 1; i <= minorCount; i++)
		{
			sb.AppendLine("[M" + i + "] 第" + i + "条小消息改写成一句通顺的话，不超过60字；一条里有多件同类事实时合并成一句");
		}
		sb.Append("[SHORT] 不超过100字，从本期正文摘取可转述的轶闻；只能概括正文已有内容，不另生成另一套故事，供旁人按报刊轶闻转述");
        string writing = writingRequirements ?? DefaultWritingRequirements;
        if (!string.IsNullOrWhiteSpace(writing)) sb.Append("\n\n【快报写作要求】\n").Append(writing.Trim());
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
			sb.Append(i + 1).Append(". ").Append(BuildWriterFactSentence(facts[i]).Trim());
            string detail = BuildWriterFactDetail(facts[i]);
			if (!string.IsNullOrWhiteSpace(detail))
			{
				sb.Append("（细节：").Append(detail.Trim()).Append("）");
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
			sb.AppendLine("M" + (i + 1) + ". " + BuildWriterMinorSentence(selection.Minors[i]));
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

	public static string TitleForKind(string kind)
	{
		switch ((kind ?? "").Trim())
		{
		case "war_declared":
			return "烽烟再起";
        case "diplomatic_declaration": return "外交宣言";
        case "ruler_policy": return "施政新令";
        case "noble_gathering": return "贵族宴集";
        case "tournament_finished": return "竞技捷报";
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
		case "alliance_formed":
			return "盟约缔结";
		case "alliance_ended":
			return "盟约解除";
		case "ruler_changed":
			return "新君即位";
		case "royal_marriage":
			return "王室联姻";
		case "noble_marriage":
			return "贵胄联姻";
		case "clan_defection":
			return "家族改投";
		case "clan_destroyed":
			return "家族覆亡";
		case "army_gathered":
			return "大军集结";
		case "town_unrest":
			return "民心浮动";
		case "town_rebellion":
			return "城中民变";
		case "kingdom_annexed":
			return "王国并吞";
		case "vassalage_established":
			return "称臣纳贡";
		case "vassalage_ended":
			return "宗藩决裂";
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
