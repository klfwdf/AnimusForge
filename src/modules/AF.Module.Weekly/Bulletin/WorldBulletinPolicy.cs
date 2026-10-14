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
	internal const int IllustrationParticipantLimit = 2;

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
		plan.Facts = string.Join("\n", facts.Select(f => BuildNpcFactSentence(f) + (string.IsNullOrWhiteSpace(f.Detail) ? "" : "\n" + WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_AdditionalFact", "Additional fact: ") + BuildCoupDetailForPrompt(f))));
		// Only the selected event's first two participants become portrait subjects;
		// supporting facts remain factual context and do not add more references.
		foreach (var person in lead?.Participants ?? new List<WorldBulletinParticipant>())
			if (person != null && !string.IsNullOrWhiteSpace(person.HeroId) && plan.Participants.Count < IllustrationParticipantLimit && !plan.Participants.Any(x => x.HeroId == person.HeroId))
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

	// Keep the main story focused; unrelated major facts are still written into the same report.
	public const int MaxMajorFacts = 3;

	// Same-group events folded into one minor line before "等N起".
	public const int MaxMinorGroupSentences = 2;

	public const int WeeklyStabilityCap = 15;


	private static readonly Regex MinorPrefix = new Regex("^[\\s·•\\-\\*、\\d\\.\\)）]+", RegexOptions.Compiled);

	// Models sometimes put several tags on one line; each tag starts a new line before parsing.
	private static readonly Regex InlineTag = new Regex("(?<!^)(?=\\[(?:TITLE|MAJOR|MINOR|SHORT|M\\d{1,2})\\])", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

	private static readonly Regex IndexedMinorTag = new Regex("^\\[M(\\d{1,2})\\]\\s*(.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly string BattleQuantity = WorldBulletinPolicyTexts.Raw("WBLT_Raw_011");
    private static readonly string BattlePersonUnit = WorldBulletinPolicyTexts.Raw("WBLT_Raw_012");
    private static readonly string BattleCountTerm = WorldBulletinPolicyTexts.Raw("WBLT_Raw_013");
    private static readonly string BattleCountGap = WorldBulletinPolicyTexts.Raw("WBLT_Raw_014");
    private static readonly Regex BattleNumericRecital = new Regex(
        BattleCountTerm + BattleCountGap + WorldBulletinPolicyTexts.Raw("WBLT_Raw_015") + BattleQuantity + @"\s*(?:" + BattlePersonUnit + WorldBulletinPolicyTexts.Raw("WBLT_Raw_016")
        + "|" + BattleQuantity + @"\s*" + BattlePersonUnit + BattleCountGap + BattleCountTerm
        + "|" + BattleQuantity + WorldBulletinPolicyTexts.Raw("WBLT_Raw_017")
        + WorldBulletinPolicyTexts.Raw("WBLT_Raw_018") + BattleCountGap + BattleQuantity + @"\s*" + BattlePersonUnit
        + "|" + BattleQuantity + @"\s*" + BattlePersonUnit + WorldBulletinPolicyTexts.Raw("WBLT_Raw_019") + BattleQuantity + @"\s*" + BattlePersonUnit
        + "|" + BattleQuantity + @"\s*(?:" + BattlePersonUnit + WorldBulletinPolicyTexts.Raw("WBLT_Raw_020") + BattleQuantity + @"\s*(?:" + BattlePersonUnit + @")?(?=[，,、。.!！?？；;\s]|$)",
        RegexOptions.Compiled);
    private static readonly Regex LegacyBattleTroopSuffix = new Regex(
        WorldBulletinPolicyTexts.Raw("WBLT_Raw_021") + BattleQuantity + WorldBulletinPolicyTexts.Raw("WBLT_Raw_022"), RegexOptions.Compiled);
    private static readonly Regex CapturedBattleCountPrefix = new Regex(
        WorldBulletinPolicyTexts.Raw("WBLT_Raw_023") + BattleQuantity + @"\s*" + BattlePersonUnit, RegexOptions.Compiled);

	private static readonly string ChineseNumeralDigits = WorldBulletinPolicyTexts.Raw("WBLT_Raw_024");
	private static readonly string ChineseNumeralUnits = WorldBulletinPolicyTexts.Raw("WBLT_Raw_025");
	private static readonly string NumericMeasureCharacters = WorldBulletinPolicyTexts.Raw("WBLT_Raw_026");
	private static readonly Dictionary<char, int> ChineseNumeralDigitValues = new Dictionary<char, int>
	{
		[PromptComposerTexts.Raw("weekly_memory_char_96f6")[0]] = 0, ['〇'] = 0, ['○'] = 0, [PromptComposerTexts.Raw("weekly_memory_char_4e00")[0]] = 1, [PromptComposerTexts.Raw("weekly_memory_char_4e8c")[0]] = 2, [PromptComposerTexts.Raw("weekly_memory_char_4e24")[0]] = 2, [PromptComposerTexts.Raw("weekly_memory_char_4e09")[0]] = 3,
		[PromptComposerTexts.Raw("weekly_memory_char_56db")[0]] = 4, [PromptComposerTexts.Raw("weekly_memory_char_4e94")[0]] = 5, [PromptComposerTexts.Raw("weekly_memory_char_516d")[0]] = 6, [PromptComposerTexts.Raw("weekly_memory_char_4e03")[0]] = 7, [PromptComposerTexts.Raw("weekly_memory_char_516b")[0]] = 8, [PromptComposerTexts.Raw("weekly_memory_char_4e5d")[0]] = 9,
		[PromptComposerTexts.Raw("weekly_memory_char_58f9")[0]] = 1, [PromptComposerTexts.Raw("weekly_memory_char_8d30")[0]] = 2, [PromptComposerTexts.Raw("weekly_memory_char_53c1")[0]] = 3, [PromptComposerTexts.Raw("weekly_memory_char_8086")[0]] = 4, [PromptComposerTexts.Raw("weekly_memory_char_4f0d")[0]] = 5, [PromptComposerTexts.Raw("weekly_memory_char_9646")[0]] = 6,
		[PromptComposerTexts.Raw("weekly_memory_char_67d2")[0]] = 7, [PromptComposerTexts.Raw("weekly_memory_char_634c")[0]] = 8, [PromptComposerTexts.Raw("weekly_memory_char_7396")[0]] = 9
	};
	private static readonly Dictionary<char, long> ChineseNumeralUnitValues = new Dictionary<char, long>
	{
		[PromptComposerTexts.Raw("weekly_memory_char_5341")[0]] = 10, [PromptComposerTexts.Raw("weekly_memory_char_767e")[0]] = 100, [PromptComposerTexts.Raw("weekly_memory_char_5343")[0]] = 1000, [PromptComposerTexts.Raw("weekly_memory_char_4e07")[0]] = 10000, [PromptComposerTexts.Raw("weekly_memory_char_4ebf")[0]] = 100000000,
		[PromptComposerTexts.Raw("weekly_memory_char_5146")[0]] = 1000000000000L, [PromptComposerTexts.Raw("weekly_memory_char_62fe")[0]] = 10, [PromptComposerTexts.Raw("weekly_memory_char_4f70")[0]] = 100, [PromptComposerTexts.Raw("weekly_memory_char_4edf")[0]] = 1000,
		[PromptComposerTexts.Raw("weekly_memory_char_842c")[0]] = 10000, [PromptComposerTexts.Raw("weekly_memory_char_5104")[0]] = 100000000
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
		return string.Join(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_MinorSeparator", "; "), parts) + (extra > 0 ? WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_MinorExtra", ", with {COUNT} more of the same kind", ("COUNT", extra.ToString(System.Globalization.CultureInfo.InvariantCulture))) : "") + WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_SentenceEnd", ".");
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
		return days == 0 ? WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_DaysToday", "Today, ") : days == 1 ? WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_DaysYesterday", "Yesterday, ") : WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_DaysAgo", "{DAYS} days ago, ", ("DAYS", days.ToString(System.Globalization.CultureInfo.InvariantCulture)));
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
				sb.AppendLine(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_BriefHeader", "[Recent events in three relevant kingdoms]"));
				sb.AppendLine(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_BriefIntro", "The following are recent events from the three most relevant kingdoms"));
			}
			written++;
			sb.Append("- ").Append(kingdom.Value).Append(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_LabelColon", ": "))
				.AppendLine(string.Join(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_MinorSeparator", "; "), facts.Select(e => DaysAgoLabel(e.Day, currentDay) + TrimSentenceEnd(BuildNpcFactSentence(e)))) + WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_SentenceEnd", "."));
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
		return "【" + header + "】\n" + WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_DetailTitle", "Title: Major events in {KINGDOM} over the past {DAYS} days", ("KINGDOM", (kingdomName ?? "").Trim()), ("DAYS", NpcDigestDays.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "\n" + string.Join("\n", facts.Select(e => FormatFactLine(e, currentDay)));
	}

	// Header names reuse the legacy "完整周报" headers: the scene prompt splitter (ShoutBehavior.IsSceneWeeklyFullReportHeader)
	// ends the rule section on exactly these, and the NPC dedup keys stay unchanged.
	public static readonly string NpcKingdomHeader = WorldBulletinPolicyTexts.Raw("NpcKingdomHeader");

	public static readonly string NpcWorldHeader = WorldBulletinPolicyTexts.Raw("NpcWorldHeader");

	public static readonly string NpcSurroundingsHeader = WorldBulletinPolicyTexts.Raw("NpcSurroundingsHeader");

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
				lines.Add(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_LatestBulletinFacts", "- Latest bulletin fact summary: {FACTS}", ("FACTS", latestBulletinShort.Trim())));
			string anecdote = NormalizeBulletinAnecdote(latestBulletinAnecdote);
			if (anecdote.Length > 0)
				lines.Add(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_BulletinAnecdote",
					"- [Bulletin anecdote] Source: the bulletin \"{TITLE}\". {ANECDOTE} (Newspaper narrative embellishment: it may be retold as \"I heard\" or \"the paper says\", but it is not a confirmed fact and must not be used to conclude injuries, relationship changes, or diplomatic outcomes.)",
					("TITLE", (latestBulletinTitle ?? "").Trim()), ("ANECDOTE", anecdote)));
		}
		return lines.Count == 0 ? "" : "【" + NpcWorldHeader + "】\n" + WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_WorldTitle", "Title: Major events across the realm over the past {DAYS} days", ("DAYS", NpcDigestDays.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "\n" + string.Join("\n", lines);
	}

	private static string FormatFactLine(WorldBulletinEvent e, int currentDay)
	{
		string detail = string.IsNullOrWhiteSpace(e.Detail) ? "" : WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_DetailWrap", " ({DETAIL})", ("DETAIL", BuildCoupDetailForPrompt(e).Trim()));
		return "- " + DaysAgoLabel(e.Day, currentDay) + TrimSentenceEnd(BuildNpcFactSentence(e)) + detail + WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_SentenceEnd", ".");
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
		// English headlines use words rather than the compact Chinese character budget;
		// the parser keeps a bounded 96-character metadata field for both packages.
		int titleLimit = 96;
		result.Title = Truncate(result.Title, titleLimit);
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
        => IsBattleKind(fact?.Kind) ? LegacyBattleTroopSuffix.Replace(fact.Sentence ?? "", "") : BuildNpcFactSentence(fact);

    private static bool IsCoupKind(string kind)
        => string.Equals(kind, "coup_success", StringComparison.Ordinal) || string.Equals(kind, "coup_failure", StringComparison.Ordinal);

    private static string BuildNpcFactSentence(WorldBulletinEvent fact)
        => IsCoupKind(fact?.Kind) ? AnimusForge.CoupSystem.CoupSourceTexts.ReportForPrompt(fact.Sentence ?? "") : CampaignSystemFactProjection.Project(fact?.Kind, fact?.Sentence ?? "");

    private static string BuildCoupDetailForPrompt(WorldBulletinEvent fact)
        => IsCoupKind(fact?.Kind) ? AnimusForge.CoupSystem.CoupSourceTexts.TimeForPrompt(fact.Detail ?? "") : CampaignSystemFactProjection.Project(fact?.Kind, fact?.Detail ?? "");

    private static string BuildWriterMinorSentence(WorldBulletinMinor minor)
        => minor?.Events?.Any(f => IsBattleKind(f?.Kind) || IsCoupKind(f?.Kind) || CampaignSystemFactProjection.IsSupported(f?.Kind)) == true
            ? BuildMinorSentence(minor.Events) : minor?.Sentence ?? "";

    private static string BuildWriterFactDetail(WorldBulletinEvent fact)
    {
        if (!IsBattleKind(fact?.Kind)) return BuildCoupDetailForPrompt(fact);
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

    internal static readonly string BattleWritingRequirements = WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_SysBattle");

    internal static readonly string LegacyDefaultWritingRequirements = WorldBulletinPolicyTexts.Raw("LegacyDefaultWritingRequirements");
    internal static readonly string LegacyNarrativeWritingRequirements = WorldBulletinPolicyTexts.Raw("LegacyNarrativeWritingRequirements");
	internal static readonly string LegacyConciseWritingRequirementsCn = WorldBulletinPolicyTexts.Raw("LegacyConciseWritingRequirementsCn");
	internal static readonly string LegacyConciseWritingRequirementsEn = WorldBulletinPolicyTexts.Raw("LegacyConciseWritingRequirementsEn");

    internal static readonly string LegacyCurrentWritingRequirements = WorldBulletinPolicyTexts.Raw("LegacyCurrentWritingRequirements");
    internal static readonly string DefaultWritingRequirements = WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_DefaultWritingRequirements");

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
		sb.AppendLine(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_SysIntro", "Write immediate bulletins for a medieval world. Use confirmed events as the skeleton and develop a concrete, retellable incident with a setup, development and payoff; merely restating facts is insufficient. Independently invent actions, brief dialogue, reactions, private arguments, misunderstandings, embarrassment and clever responses absent from the material without requiring a source for each detail. Keep them plausible for the participants, period and setting and present them naturally without labeling every sentence as rumor. Preserve confirmed actors, places, times, actions and outcomes; do not change victory, survival, wounds, captivity, relationship values, territorial ownership or executed diplomatic outcomes, or invent casualty figures. Anecdotes do not change game mechanics. Never replace recorded dialogue or last words; declarations and promises remain statements, and speculation about future effects requires cautious wording."));
		sb.AppendLine(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_SysBattle", "Use the belligerents, place, result and confirmed consequences as the factual skeleton, while still independently inventing actions, dialogue and on-scene incidents around them. Do not recite troop strength, participation, deaths, wounds or casualty figures, write “X men against Y men”, or turn such numbers into spelled-out quantities and list them anyway. This limit does not cover non-battle quantities such as dates or title numbering."));
		sb.AppendLine(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_SysVariety", "Develop one complete incident around the lead event, with concrete actions, brief dialogue or reactions. Vary openings, viewpoints, rhythms and emotions to suit the event without repeating a fixed device or requiring conflict, jokes or conspiracies."));
		sb.AppendLine(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_SysClash", "Only when the source confirms that two people actually fought in the same engagement, and their identities and the situation make it plausible, may you add a direct clash between them as a newspaper anecdote; never bring a distant ruler or any other person who did not take part onto the scene, and never change the victory, death or captive outcome. Moderate sarcasm and grit are allowed, and so are courage, wit and dignity; keep a medieval voice, avoid modern jokes, and do not rely on brawling for amusement in every piece."));
		sb.AppendLine(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_SysTransfer", "A territory handed over by means such as \"trade/purchase\" or \"kingdom decree\", or one stated not to have been taken by siege, must not be written as stormed or captured. Do not use the vanilla default name of the continent; to refer to a wide area, write only \"the continent\" or a specific kingdom name."));
		sb.AppendLine(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_SysSally", "A fact marked as a sally-out battle involves only the troops that fought in this sally: write only that this force was repelled, defeated or took losses, and do not expand it into the besieging army, the garrison or a whole army being wiped out; unless the fact explicitly confirms that a whole army was destroyed, do not write conclusions such as \"the entire army was defeated\" or \"the army was routed\"."));
		if (majorFactCount > 1)
		{
			sb.AppendLine(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_SysMajorMerge", "Include all {COUNT} supplied major facts in the same main report; do not omit them or move them to minor news. Develop fact 1 as the main thread with a complete incident. Connect related events naturally and incorporate unrelated major events through shifts of time, place or viewpoint without inventing causal links. Do not divide the space equally into separate short notices.", ("COUNT", majorFactCount.ToString(System.Globalization.CultureInfo.InvariantCulture))));
		}
		sb.AppendLine(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_ContextSafety"));
		sb.AppendLine(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_SysFormat", "Output strictly in the following format and nothing else:"));
		sb.AppendLine("[TITLE] " + WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_SysTitleSpec", "A headline of at most 14 characters that summarizes the whole major event"));
		sb.AppendLine("[MAJOR] " + (majorFactCount > 1 ? WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_SysMajorSpecMulti", "A broadsheet story of 330 to 500 words in three to five paragraphs. Develop a complete incident around fact 1 while naturally including every other major fact. Connect related events and incorporate unrelated ones through shifts of time, place or viewpoint without inventing causal links. Use actions and brief dialogue to reveal character rather than merely restating facts or piling on decorative details. End on a concrete consequence, reaction or meaningful detail instead of a forced grand verdict. Silently repair factual conflicts, undeveloped incidents and lists of events before delivering only the finished copy") : WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_SysMajorSpecSingle", "A broadsheet story of 330 to 500 words in three to five paragraphs. Develop a concrete incident around the lead event with a setup, development and payoff, using actions and brief dialogue to reveal character rather than merely restating facts or piling on decorative details. Choose the opening and narrative order freely while establishing the participants, place, events and outcomes. End on a concrete consequence, reaction or meaningful detail instead of a forced grand verdict. Silently repair factual conflicts, undeveloped incidents and lists of events before delivering only the finished copy")));
		for (int i = 1; i <= minorCount; i++)
		{
			sb.AppendLine("[M" + i + "] " + WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_SysMinorSpec", "Rewrite minor item {INDEX} as one fluent sentence of at most 60 characters; if an item contains several facts of the same kind, merge them into one sentence", ("INDEX", i.ToString(System.Globalization.CultureInfo.InvariantCulture))));
		}
		sb.Append("[SHORT] " + WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_SysShortSpec", "At most 100 characters; pick out an anecdote from this issue's body that others could retell. Summarize only what the body already says and do not invent a separate story, so that bystanders can pass it on as a newspaper anecdote"));
		string writing = writingRequirements ?? DefaultWritingRequirements;
		if (!string.IsNullOrWhiteSpace(writing)) sb.Append("\n\n").Append(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_SysWritingHeader", "[Bulletin writing requirements]")).Append("\n").Append(writing.Trim());
		return PromptComposerTexts.ApplyOutputLanguage(sb.ToString());
	}

	// facts' Detail and kingdomContext are writer-only background; nothing here is shown verbatim.
	public static string BuildUserPrompt(string scopeLine, string dateText, WorldBulletinSelection selection, IEnumerable<string> kingdomContext)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine(scopeLine);
		sb.AppendLine(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_UserDateLine", "Current date: {DATE}", ("DATE", dateText)));
		sb.AppendLine(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_UserMajorHeader", "[Major event]"));
		List<WorldBulletinEvent> facts = selection.MajorFacts.Count > 0 ? selection.MajorFacts : new List<WorldBulletinEvent> { selection.Major };
		for (int i = 0; i < facts.Count; i++)
		{
			sb.Append(i + 1).Append(". ").Append(BuildWriterFactSentence(facts[i]).Trim());
            string detail = BuildWriterFactDetail(facts[i]);
			if (!string.IsNullOrWhiteSpace(detail))
			{
				sb.Append(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_UserFactDetail", " (details: {DETAIL})", ("DETAIL", detail.Trim())));
			}
			sb.AppendLine();
		}
		List<string> context = (kingdomContext ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
		if (context.Count > 0)
		{
			sb.AppendLine(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_UserContextHeader", "[Related kingdom status]"));
			foreach (string line in context)
			{
				sb.AppendLine("· " + line.Trim());
			}
		}
		for (int i = 0; i < selection.Minors.Count; i++)
		{
			if (i == 0)
			{
				sb.AppendLine(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_UserMinorHeader", "[Minor news]"));
			}
			sb.AppendLine("M" + (i + 1) + ". " + BuildWriterMinorSentence(selection.Minors[i]));
		}
		return sb.ToString().TrimEnd();
	}

	public static string BuildBody(string major, IEnumerable<string> minors)
	{
		StringBuilder sb = new StringBuilder();
		sb.Append(WorldBulletinPolicyTexts.Raw("BodyMajorHeader")).Append("\n").Append((major ?? "").Trim());
		List<string> list = (minors ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
		if (list.Count > 0)
		{
			sb.Append("\n\n").Append(WorldBulletinPolicyTexts.Raw("BodyMinorHeader"));
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
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_WarDeclared", "War breaks out");
		case "diplomatic_declaration":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_DiplomaticDeclaration", "Diplomatic declaration");
		case "ruler_policy":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_RulerPolicy", "New ruler policy");
		case "noble_gathering":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_NobleGathering", "Noble gathering");
		case "tournament_finished":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_TournamentFinished", "Tournament results");
		case "peace_made":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_PeaceMade", "Peace concluded");
		case "settlement_siege":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_SettlementSiege", "Settlement changes hands");
		case "settlement_transfer":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_SettlementTransfer", "Territory transferred");
		case "fief_grant":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_FiefGrant", "Fief granted");
		case "battle":
		case "siege_battle":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_Battle", "Battle report");
		case "sally_out_battle":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_SallyOutBattle", "Sally-out battle report");
		case "ruler_killed":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_RulerKilled", "Ruler slain");
		case "lord_killed":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_LordKilled", "Noble slain");
		case "lord_captured":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_LordCaptured", "Noble captured");
		case "kingdom_destroyed":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_KingdomDestroyed", "Kingdom falls");
		case "kingdom_created":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_KingdomCreated", "Kingdom founded");
		case "kingdom_rebellion":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_KingdomRebellion", "Kingdom rebellion");
		case "civil_war":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_CivilWar", "Civil war erupts");
		case "civil_war_resolution":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_CivilWarResolution", "Civil war ends");
		case "civil_war_politics":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_CivilWarPolitics", "Faction negotiations");
		case "coup_success":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_CoupSuccess", "Coup succeeds");
		case "coup_failure":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_CoupFailure", "Coup fails");
		case "raid":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_Raid", "Village raided");
		case "alliance_formed":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_AllianceFormed", "Alliance formed");
		case "alliance_ended":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_AllianceEnded", "Alliance ended");
		case "ruler_changed":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_RulerChanged", "New ruler");
		case "royal_marriage":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_RoyalMarriage", "Royal marriage");
		case "noble_marriage":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_NobleMarriage", "Noble marriage");
		case "clan_defection":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_ClanDefection", "Clan defects");
		case "clan_destroyed":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_ClanDestroyed", "Clan destroyed");
		case "army_gathered":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_ArmyGathered", "Army assembled");
		case "town_unrest":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_TownUnrest", "Town unrest");
		case "town_rebellion":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_TownRebellion", "Town rebellion");
		case "kingdom_annexed":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_KingdomAnnexed", "Kingdom annexed");
		case "vassalage_established":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_VassalageEstablished", "Vassalage established");
		case "vassalage_ended":
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_VassalageEnded", "Vassalage ends");
		default:
			return WorldBulletinPolicyTexts.Title("AF_WBLT_Title_Default", "Current affairs bulletin");
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
