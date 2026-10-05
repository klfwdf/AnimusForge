using System;using System.Collections.Generic;using System.Linq;using static AnimusForge.MyBehavior;
namespace AnimusForge;
internal sealed class WorldBulletinLayout
{
	public string EventId = "";

	public string MajorKind = "";

	public List<string> KingdomIds = new List<string>();

	public List<string> MinorKinds = new List<string>();

	public WorldBulletinIllustrationPlan IllustrationPlan;
}
internal sealed partial class WorldBulletinStateOwner {
internal List<EventRecordEntry> CachedRecords;internal int CachedRecordCount=-1,CachedRecordIndex=-1;
private const int WorldBulletinMaxLayouts=48,WorldBulletinMaxMetaKingdoms=3;private const string WorldBulletinMajorHeader="【大事件】",WorldBulletinMinorHeader="【其他消息】";
internal void RecordWorldBulletinLayout(string eventId, WorldBulletinSelection selection)
	{
		if (string.IsNullOrWhiteSpace(eventId) || selection?.Major == null)
		{
			return;
		}
		WorldBulletinSaveState state = EnsureWorldBulletinState();
		state.Layouts ??= new List<WorldBulletinLayout>();
		state.Layouts.RemoveAll(x => x == null || string.Equals(x.EventId, eventId, StringComparison.OrdinalIgnoreCase));
		List<WorldBulletinEvent> facts = selection.MajorFacts?.Count > 0 ? selection.MajorFacts : new List<WorldBulletinEvent> { selection.Major };
		state.Layouts.Add(new WorldBulletinLayout
		{
			EventId = eventId,
			MajorKind = selection.Major.Kind ?? "",
			KingdomIds = facts.Where(e => e?.KingdomIds != null).SelectMany(e => e.KingdomIds)
				.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase)
				.Take(WorldBulletinMaxMetaKingdoms).ToList(),
			// MergeMinors keeps one line per selected minor, so indexes line up with the published list.
			MinorKinds = (selection.Minors ?? new List<WorldBulletinMinor>()).Select(m => m?.Events?.FirstOrDefault()?.Kind ?? "").ToList()
		});
		if (state.Layouts.Count > WorldBulletinMaxLayouts)
		{
			state.Layouts.RemoveRange(0, state.Layouts.Count - WorldBulletinMaxLayouts);
		}
	}
// Legacy issues have no record-level association field. Build this bounded snapshot
// only when an archive opens; do not touch current facts or mutate the save.
internal IReadOnlyDictionary<string, List<string>> SnapshotLegacyBulletinKingdomAssociations()
{
    var index = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    foreach (var layout in State?.Layouts ?? new List<WorldBulletinLayout>())
    {
        string id = (layout?.EventId ?? "").Trim();
        if (id.Length > 0 && WeeklyReportArchivePolicy.IsBulletin(id))
            index[id] = WeeklyReportArchivePolicy.NormalizeKingdomIds(layout.KingdomIds);
    }
    return index;
}

internal WorldBulletinLayout FindWorldBulletinLayout(string eventId)
	{
		List<WorldBulletinLayout> layouts = State?.Layouts;
		if (layouts == null)
		{
			return null;
		}
		for (int i = layouts.Count - 1; i >= 0; i--)
		{
			if (layouts[i] != null && string.Equals(layouts[i].EventId, eventId, StringComparison.OrdinalIgnoreCase))
			{
				return layouts[i];
			}
		}
		return null;
	}
internal WorldBulletinPanelData BuildWorldBulletinPanelData(EventRecordEntry entry, string eventId)
	{
		WorldBulletinLayout layout = FindWorldBulletinLayout(eventId);
		SplitWorldBulletinBody(WeeklyReportArchivePolicy.BodyWithRegionalNews(entry), out string major, out List<string> minorLines);
		if (major.Length == 0)
		{
			major = (entry.ShortSummary ?? "").Trim();
		}
		bool worldScope = string.Equals((entry.EventKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase);
		string kingdomLabel = _port.ResolveKingdom(entry.ScopeKingdomId);
		// Kingdom-scope issues only exist in saves from earlier builds.
		string scopeLabel = worldScope ? "时事快报" : (string.IsNullOrWhiteSpace(kingdomLabel) ? "王国快报" : kingdomLabel + "快报");
		List<string> masthead = new List<string> { scopeLabel };
		if (!string.IsNullOrWhiteSpace(entry.CreatedDate))
		{
			masthead.Add(entry.CreatedDate.Trim());
		}
		string issue = ParseWorldBulletinIssueNumber(eventId);
		if (issue.Length > 0)
		{
			masthead.Add("第" + issue + "期");
		}
		string majorKind = layout?.MajorKind ?? "";
		string kindLabel = WorldBulletinPolicy.TitleForKind(majorKind);
		WorldBulletinPanelData data = new WorldBulletinPanelData
		{
			EventId = eventId,
			MastheadText = string.Join(" · ", masthead),
			KindText = majorKind.Length > 0 ? WorldBulletinCategoryForKind(majorKind) + " · " + kindLabel : "时 事",
			HeadlineText = _port.NoticeTitle(entry),
			MetaText = BuildWorldBulletinMetaText(entry, layout, worldScope),
			BodyText = major,
			IllustrationSubtitle = _port.PopupSubtitle(entry),
			IllustrationBody = _port.PopupBody(entry),
			IllustrationPlan = layout?.IllustrationPlan
		};
		for (int i = 0; i < minorLines.Count; i++)
		{
			string kind = layout?.MinorKinds != null && i < layout.MinorKinds.Count ? layout.MinorKinds[i] : "";
			// Old issues predate layouts; their minors get the neutral tag.
			data.Minors.Add(new KeyValuePair<string, string>(kind.Length > 0 ? WorldBulletinPolicy.TitleForKind(kind) : "消息", minorLines[i]));
		}
		return data;
	}
internal static void SplitWorldBulletinBody(string summary, out string major, out List<string> minors)
	{
		minors = new List<string>();
		string text = (summary ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		int minorAt = text.IndexOf(WorldBulletinMinorHeader, StringComparison.Ordinal);
		string majorPart = minorAt >= 0 ? text.Substring(0, minorAt) : text;
		if (majorPart.StartsWith(WorldBulletinMajorHeader, StringComparison.Ordinal))
		{
			majorPart = majorPart.Substring(WorldBulletinMajorHeader.Length);
		}
		major = majorPart.Trim();
		if (minorAt < 0)
		{
			return;
		}
		foreach (string rawLine in text.Substring(minorAt + WorldBulletinMinorHeader.Length).Split('\n'))
		{
			string line = rawLine.Trim().TrimStart('·', '•', '-', ' ').Trim();
			if (line.Length > 0)
			{
				minors.Add(line);
			}
		}
	}
internal static string ParseWorldBulletinIssueNumber(string eventId)
	{
		string[] parts = (eventId ?? "").Split(':');
		return parts.Length >= 5 && int.TryParse(parts[parts.Length - 2], out int seq) && seq > 0 ? seq.ToString() : "";
	}
internal string BuildWorldBulletinMetaText(EventRecordEntry entry, WorldBulletinLayout layout, bool worldScope) => BuildWorldBulletinMetaText(entry,layout,worldScope,_port.ResolveKingdom);
internal static string BuildWorldBulletinMetaText(EventRecordEntry entry, WorldBulletinLayout layout, bool worldScope, Func<string,string> resolveKingdom)
	{
		List<string> names = (layout?.KingdomIds ?? new List<string>())
			.Select(resolveKingdom)
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Distinct(StringComparer.Ordinal)
			.ToList();
		if (names.Count == 0 && !worldScope && !string.IsNullOrWhiteSpace(entry.ScopeKingdomId))
		{
			names.Add(resolveKingdom(entry.ScopeKingdomId));
		}
		string date = (entry.CreatedDate ?? "").Trim();
		string kingdoms = names.Count > 0 ? "涉及 " + string.Join("、", names) : "";
		return string.Join(" · ", new[] { date, kingdoms }.Where(x => x.Length > 0));
	}
internal static string WorldBulletinCategoryForKind(string kind)
	{
		switch ((kind ?? "").Trim())
		{
		case "war_declared":
		case "peace_made":
		case "alliance_formed":
		case "alliance_ended":
		case "vassalage_established":
		case "vassalage_ended":
			return "外 交";
		case "settlement_siege":
		case "battle":
		case "siege_battle":
		case "army_gathered":
			return "战 事";
		case "raid":
			return "劫 掠";
		case "settlement_transfer":
		case "fief_grant":
			return "领 地";
		case "ruler_killed":
		case "lord_killed":
			return "讣 闻";
		case "ruler_captured":
		case "lord_captured":
			return "被 俘";
		case "kingdom_destroyed":
		case "kingdom_created":
		case "kingdom_rebellion":
		case "civil_war":
		case "civil_war_resolution":
		case "civil_war_politics":
		case "ruler_changed":
		case "royal_marriage":
		case "noble_marriage":
		case "clan_defection":
		case "clan_destroyed":
		case "town_unrest":
		case "town_rebellion":
		case "kingdom_annexed":
			return "国 事";
		default:
			return "时 事";
		}
	}
internal EventRecordEntry FindLatestWorldBulletinRecord()
	{
		if ((State?.World?.Sequence ?? 0) <= 0)
		{
			return null;
		}
		List<EventRecordEntry> entries = _port.Records();
		if (entries == null)
		{
			return null;
		}
		if (ReferenceEquals(entries, CachedRecords) && entries.Count == CachedRecordCount)
		{
			if (CachedRecordIndex < 0)
			{
				return null;
			}
			EventRecordEntry cached = entries[CachedRecordIndex];
			if (cached != null && string.Equals(cached.EventId, LatestEventId, StringComparison.OrdinalIgnoreCase))
			{
				return cached;
			}
		}
		EventRecordEntry latest = null;
		int latestIndex = -1;
		int latestSequence = -1;
		for (int i = 0; i < entries.Count; i++)
		{
			EventRecordEntry entry = entries[i];
			if (entry != null && IsWorldBulletinEventId(entry.EventId) && string.Equals((entry.EventKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase))
			{
				int sequence = GetWorldBulletinRecordSequence(entry.EventId);
				if (latest == null || entry.CreatedDay > latest.CreatedDay || (entry.CreatedDay == latest.CreatedDay && sequence > latestSequence))
				{
					latest = entry;
					latestIndex = i;
					latestSequence = sequence;
				}
			}
		}
		CachedRecords = entries;
		CachedRecordCount = entries.Count;
		CachedRecordIndex = latestIndex;
		LatestEventId = latest?.EventId ?? "";
		return latest;
	}
internal static int GetWorldBulletinRecordSequence(string eventId)
	{
		string[] parts = (eventId ?? "").Split(':');
		return parts.Length >= 5 && int.TryParse(parts[parts.Length - 2], out int sequence) ? sequence : -1;
	}
}
