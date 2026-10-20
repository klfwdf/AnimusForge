using System;using System.Collections.Generic;using System.Linq;using TaleWorlds.Localization;using static AnimusForge.MyBehavior;
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
private const int WorldBulletinMaxLayouts=48,WorldBulletinMaxMetaKingdoms=3;private static readonly string WorldBulletinMajorHeader=WorldBulletinPolicyTexts.Raw("BodyMajorHeader"),WorldBulletinMinorHeader=WorldBulletinPolicyTexts.Raw("BodyMinorHeader");
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
		string scopeLabel = worldScope
			? BulletinText("AF_UI_Weekly_Bulletin_ScopeWorld", "World bulletin")
			: string.IsNullOrWhiteSpace(kingdomLabel)
				? BulletinText("AF_UI_Weekly_Bulletin_ScopeKingdom", "Kingdom bulletin")
				: BulletinText("AF_UI_Weekly_Bulletin_ScopeNamedKingdom", "{KINGDOM} bulletin", "KINGDOM", kingdomLabel);
		List<string> masthead = new List<string> { scopeLabel };
		masthead.Add(WeeklyReportArchivePolicy.ProjectDateForDisplay(entry?.CreatedDate));
		string issue = ParseWorldBulletinIssueNumber(eventId);
		if (issue.Length > 0)
		{
			masthead.Add(BulletinText("AF_UI_Weekly_Bulletin_Issue", "Issue {ISSUE}", "ISSUE", issue));
		}
		string majorKind = layout?.MajorKind ?? "";
		string kindLabel = majorKind.Length > 0 ? WorldBulletinTitleForKind(majorKind) : "";
		WorldBulletinPanelData data = new WorldBulletinPanelData
		{
			EventId = eventId,
			MastheadText = string.Join(" · ", masthead),
			KindText = majorKind.Length > 0
				? WorldBulletinCategoryForKind(majorKind) + " · " + kindLabel
				: BulletinText("AF_UI_Weekly_Bulletin_CategoryCurrentAffairs", "Current affairs"),
			HeadlineText = _port.NoticeTitle(entry),
			MetaText = BuildWorldBulletinMetaText(entry, layout, worldScope),
			BodyText = major,
			IllustrationSubtitle = _port.PopupSubtitle(entry),
			IllustrationBody = _port.PopupBody(entry),
			IllustrationPlan = layout?.IllustrationPlan
		};
		Dictionary<string, string> minorKindLabels = null;
		for (int i = 0; i < minorLines.Count; i++)
		{
			// Old issues predate layouts; their minors get the neutral tag.
			string kind = layout?.MinorKinds != null && i < layout.MinorKinds.Count ? layout.MinorKinds[i] : "";
			string kindLabelForDisplay;
			if (kind.Length == 0)
			{
				kindLabelForDisplay = BulletinText("AF_UI_Weekly_Bulletin_MinorMessage", "Message");
			}
			else
			{
				minorKindLabels ??= new Dictionary<string, string>(StringComparer.Ordinal);
				if (!minorKindLabels.TryGetValue(kind, out kindLabelForDisplay))
				{
					kindLabelForDisplay = WorldBulletinTitleForKind(kind);
					minorKindLabels.Add(kind, kindLabelForDisplay);
				}
			}
			data.Minors.Add(new KeyValuePair<string, string>(kindLabelForDisplay, minorLines[i]));
		}
		return data;
	}
internal static void SplitWorldBulletinBody(string summary, out string major, out List<string> minors)
	{
		minors = new List<string>();
		string text = (summary ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		string minorHeader = text.IndexOf(WorldBulletinMinorHeader, StringComparison.Ordinal) >= 0 ? WorldBulletinMinorHeader : "[Minor news]";
		int minorAt = text.IndexOf(minorHeader, StringComparison.Ordinal);
		string majorPart = minorAt >= 0 ? text.Substring(0, minorAt) : text;
		string majorHeader = majorPart.StartsWith(WorldBulletinMajorHeader, StringComparison.Ordinal) ? WorldBulletinMajorHeader : "[Major event]";
		if (majorPart.StartsWith(majorHeader, StringComparison.Ordinal))
		{
			majorPart = majorPart.Substring(majorHeader.Length);
		}
		major = majorPart.Trim();
		if (minorAt < 0)
		{
			return;
		}
		foreach (string rawLine in text.Substring(minorAt + minorHeader.Length).Split('\n'))
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
		string date = WeeklyReportArchivePolicy.ProjectDateForDisplay(entry?.CreatedDate);
		string kingdoms = names.Count > 0
			? BulletinText("AF_UI_Weekly_Bulletin_InvolvedKingdoms", "Involved: {KINGDOMS}", "KINGDOMS", string.Join(BulletinText("AF_UI_Weekly_Bulletin_KingdomSeparator", ", "), names))
			: "";
		return string.Join(" · ", new[] { date, kingdoms }.Where(x => x.Length > 0));
	}
private static string BulletinText(string id, string fallback) => new TextObject("{=" + id + "}" + fallback).ToString();
private static string BulletinText(string id, string fallback, string variable, string value)
	{
		var text = new TextObject("{=" + id + "}" + fallback);
		text.SetTextVariable(variable, value ?? string.Empty);
		return text.ToString();
	}
internal static string WorldBulletinCategoryForKind(string kind)
	{
		switch ((kind ?? "").Trim())
		{
		case "war_declared":
		case "diplomatic_declaration":
		case "peace_made":
		case "alliance_formed":
		case "alliance_ended":
		case "vassalage_established":
		case "vassalage_ended":
			return BulletinText("AF_UI_Weekly_Bulletin_CategoryDiplomacy", "Diplomacy");
		case "settlement_siege":
		case "battle":
		case "siege_battle":
		case "army_gathered":
			return BulletinText("AF_UI_Weekly_Bulletin_CategoryConflict", "Conflict");
		case "raid":
			return BulletinText("AF_UI_Weekly_Bulletin_CategoryRaids", "Raids");
		case "settlement_transfer":
		case "fief_grant":
			return BulletinText("AF_UI_Weekly_Bulletin_CategoryTerritory", "Territory");
		case "ruler_killed":
		case "lord_killed":
			return BulletinText("AF_UI_Weekly_Bulletin_CategoryObituaries", "Obituaries");
		case "ruler_captured":
		case "lord_captured":
			return BulletinText("AF_UI_Weekly_Bulletin_CategoryCaptivity", "Captivity");
		case "kingdom_destroyed":
		case "kingdom_created":
		case "kingdom_rebellion":
		case "civil_war":
		case "civil_war_resolution":
		case "civil_war_politics":
		case "ruler_changed":
		case "ruler_policy":
		case "noble_gathering":
		case "tournament_finished":
		case "royal_marriage":
		case "noble_marriage":
		case "clan_defection":
		case "clan_destroyed":
		case "town_unrest":
		case "town_rebellion":
		case "kingdom_annexed":
			return BulletinText("AF_UI_Weekly_Bulletin_CategoryStateAffairs", "State affairs");
		default:
			return BulletinText("AF_UI_Weekly_Bulletin_CategoryCurrentAffairs", "Current affairs");
		}
	}
internal static string WorldBulletinTitleForKind(string kind)
	{
		switch ((kind ?? "").Trim())
		{
		case "war_declared": return BulletinText("AF_UI_Weekly_Bulletin_KindWarDeclared", "War breaks out");
		case "diplomatic_declaration": return BulletinText("AF_UI_Weekly_Bulletin_KindDiplomaticDeclaration", "Diplomatic declaration");
		case "ruler_policy": return BulletinText("AF_UI_Weekly_Bulletin_KindRulerPolicy", "New ruler policy");
		case "noble_gathering": return BulletinText("AF_UI_Weekly_Bulletin_KindNobleGathering", "Noble gathering");
		case "tournament_finished": return BulletinText("AF_UI_Weekly_Bulletin_KindTournamentFinished", "Tournament results");
		case "peace_made": return BulletinText("AF_UI_Weekly_Bulletin_KindPeaceMade", "Peace concluded");
		case "settlement_siege": return BulletinText("AF_UI_Weekly_Bulletin_KindSettlementSiege", "Settlement changes hands");
		case "settlement_transfer": return BulletinText("AF_UI_Weekly_Bulletin_KindSettlementTransfer", "Territory transferred");
		case "fief_grant": return BulletinText("AF_UI_Weekly_Bulletin_KindFiefGrant", "Fief granted");
		case "battle":
		case "siege_battle": return BulletinText("AF_UI_Weekly_Bulletin_KindBattle", "Battle report");
		case "sally_out_battle": return BulletinText("AF_UI_Weekly_Bulletin_KindSallyOutBattle", "Sally-out battle report");
		case "ruler_killed": return BulletinText("AF_UI_Weekly_Bulletin_KindRulerKilled", "Ruler slain");
		case "lord_killed": return BulletinText("AF_UI_Weekly_Bulletin_KindLordKilled", "Noble slain");
		case "lord_captured": return BulletinText("AF_UI_Weekly_Bulletin_KindLordCaptured", "Noble captured");
		case "kingdom_destroyed": return BulletinText("AF_UI_Weekly_Bulletin_KindKingdomDestroyed", "Kingdom falls");
		case "kingdom_created": return BulletinText("AF_UI_Weekly_Bulletin_KindKingdomCreated", "Kingdom founded");
		case "kingdom_rebellion": return BulletinText("AF_UI_Weekly_Bulletin_KindKingdomRebellion", "Kingdom rebellion");
		case "civil_war": return BulletinText("AF_UI_Weekly_Bulletin_KindCivilWar", "Civil war erupts");
		case "civil_war_resolution": return BulletinText("AF_UI_Weekly_Bulletin_KindCivilWarResolution", "Civil war ends");
		case "civil_war_politics": return BulletinText("AF_UI_Weekly_Bulletin_KindCivilWarPolitics", "Faction negotiations");
		case "coup_success": return BulletinText("AF_UI_Weekly_Bulletin_KindCoupSuccess", "Coup succeeds");
		case "coup_failure": return BulletinText("AF_UI_Weekly_Bulletin_KindCoupFailure", "Coup fails");
		case "raid": return BulletinText("AF_UI_Weekly_Bulletin_KindRaid", "Village raided");
		case "alliance_formed": return BulletinText("AF_UI_Weekly_Bulletin_KindAllianceFormed", "Alliance formed");
		case "alliance_ended": return BulletinText("AF_UI_Weekly_Bulletin_KindAllianceEnded", "Alliance ended");
		case "ruler_changed": return BulletinText("AF_UI_Weekly_Bulletin_KindRulerChanged", "New ruler");
		case "royal_marriage": return BulletinText("AF_UI_Weekly_Bulletin_KindRoyalMarriage", "Royal marriage");
		case "noble_marriage": return BulletinText("AF_UI_Weekly_Bulletin_KindNobleMarriage", "Noble marriage");
		case "clan_defection": return BulletinText("AF_UI_Weekly_Bulletin_KindClanDefection", "Clan defects");
		case "clan_destroyed": return BulletinText("AF_UI_Weekly_Bulletin_KindClanDestroyed", "Clan destroyed");
		case "army_gathered": return BulletinText("AF_UI_Weekly_Bulletin_KindArmyGathered", "Army assembled");
		case "town_unrest": return BulletinText("AF_UI_Weekly_Bulletin_KindTownUnrest", "Town unrest");
		case "town_rebellion": return BulletinText("AF_UI_Weekly_Bulletin_KindTownRebellion", "Town rebellion");
		case "kingdom_annexed": return BulletinText("AF_UI_Weekly_Bulletin_KindKingdomAnnexed", "Kingdom annexed");
		case "vassalage_established": return BulletinText("AF_UI_Weekly_Bulletin_KindVassalageEstablished", "Vassalage established");
		case "vassalage_ended": return BulletinText("AF_UI_Weekly_Bulletin_KindVassalageEnded", "Vassalage ends");
		default: return BulletinText("AF_UI_Weekly_Bulletin_KindDefault", "Current affairs bulletin");
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
