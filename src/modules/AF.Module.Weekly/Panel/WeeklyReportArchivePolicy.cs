using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static AnimusForge.MyBehavior;
using TaleWorlds.Localization;

namespace AnimusForge;

// Detached archive projection; original legacy records remain intact on disk.
internal static class WeeklyReportArchivePolicy
{
	private static string Localize(string id, string fallback)
		=> new TextObject("{=" + id + "}" + fallback).ToString();

	private static string Localize(string id, string fallback, params (string Name, string Value)[] variables)
	{
		TextObject text = new TextObject("{=" + id + "}" + fallback);
		foreach ((string Name, string Value) variable in variables ?? Array.Empty<(string Name, string Value)>())
		{
			text.SetTextVariable(variable.Name, variable.Value ?? string.Empty);
		}
		return text.ToString();
	}
    internal static string ProjectTitleForDisplay(string id, string title)
    {
        string value = title ?? "";
        if (id == "weekly_report:world:0:" && value == PromptComposerTexts.Raw("weekly_memory_compat_871fa8d0ce6a"))
            return Localize("AF_UI_Weekly_OpeningWorldTitle", "Day 0 world opening summary");
        if ((id ?? "").StartsWith("weekly_report:kingdom:0:", StringComparison.Ordinal))
        {
            string suffix = PromptComposerTexts.Raw("weekly_memory_compat_21d709ad6255");
            if (value.EndsWith(suffix, StringComparison.Ordinal))
                return Localize("AF_UI_Weekly_OpeningKingdomTitle", "{KINGDOM} Day 0 opening summary", ("KINGDOM", value.Substring(0, value.Length - suffix.Length)));
        }
        return value;
    }

    internal static string ProjectBulletinHeadingsForDisplay(string body)
    {
        string text = body ?? "";
        // Only complete structural heading lines are projected; quoted prose is untouched.
        string major = WorldBulletinPolicyTexts.Raw("BodyMajorHeader"), minor = WorldBulletinPolicyTexts.Raw("BodyMinorHeader");
        return System.Text.RegularExpressions.Regex.Replace(text, @"(?m)^(" + System.Text.RegularExpressions.Regex.Escape(major)
            + "|" + System.Text.RegularExpressions.Regex.Escape(minor) + @"|\[Major event\]|\[Minor news\])(?=\s|$)",
            match => match.Value == major || match.Value == "[Major event]"
                ? Localize("AF_UI_Weekly_MajorHeading", "[Major event]") : Localize("AF_UI_Weekly_MinorHeading", "[Minor news]"));
    }

    internal const string RegionalMaterialType = "bulletin_regional_news";
    internal const string RecentMaterialType = "bulletin_residual_fact";
    internal const string ReportedMaterialType = "bulletin_reported_fact";
    internal const string OtherKingdomId = "__other_recent__";
    internal static string RecentId(string nation, int week) => "weekly_report:kingdom:recent:" + Math.Max(0, week) + ":" + nation;
    internal static bool IsRecent(string id) => (id ?? "").StartsWith("weekly_report:kingdom:recent:", StringComparison.OrdinalIgnoreCase);
    internal static bool IsRegionalSummary(EventRecordEntry e) => e != null && !IsRecent(e.EventId)
        && string.Equals(e.EventKind, "kingdom", StringComparison.OrdinalIgnoreCase) && !IsBulletin(e.EventId)
        && ((e.EventId ?? "").EndsWith(":brief", StringComparison.OrdinalIgnoreCase)
            || (e.WeekIndex > 0 && string.IsNullOrWhiteSpace(e.Summary) && !string.IsNullOrWhiteSpace(e.ShortSummary)));

    internal static EventMaterialReference FactMaterial(WorldBulletinEvent fact, string nation, string type = RecentMaterialType)
        => new EventMaterialReference {
            MaterialType=type, KingdomId=nation, SnapshotText=WorldBulletinCampaignMaterialPolicy.ProjectRecordedBattle(fact.Sentence, fact.Kind) + (string.IsNullOrWhiteSpace(fact.Detail) ? "" : "\n" + Localize("AF_UI_Weekly_Browser_AdditionalFact", "Additional fact") + ": " + WorldBulletinCampaignMaterialPolicy.ProjectRecordedBattle(fact.Detail, fact.Kind)), Label=fact.GameDate,
            ActionDay=fact.Day, ActionKind=fact.Kind, SourceStableKeys=new List<string> { fact.Key }
        };
    internal static EventMaterialReference RegionalMaterial(EventRecordEntry entry) => new EventMaterialReference {
        MaterialType=RegionalMaterialType, Label=entry.CreatedDate, KingdomId=entry.ScopeKingdomId,
        SnapshotText=string.IsNullOrWhiteSpace(entry.Summary) ? entry.ShortSummary : entry.Summary,
        ActionDay=entry.CreatedDay, SourceStableKeys=new List<string> { entry.EventId }
    };

    internal static HashSet<string> RecentSourceKeys(EventRecordEntry entry) => new HashSet<string>(
        (entry.Materials ?? new List<EventMaterialReference>()).Where(m => m != null)
        .SelectMany(m => m.SourceStableKeys ?? new List<string>()).Where(k => !string.IsNullOrWhiteSpace(k)),StringComparer.Ordinal);

    // Project only frozen reported materials; legacy model summaries/titles are not facts.
    // Runs at existing publication/import/NPC/history boundaries, never in a new tick scan.
    internal static string BulletinFactSummary(EventRecordEntry entry)
        => FactSummary(entry?.Materials ?? Enumerable.Empty<EventMaterialReference>());

    private static string FactSummary(IEnumerable<EventMaterialReference> materials)
    {
        var sentences = materials
            .Where(m => m != null && m.MaterialType == ReportedMaterialType && !string.IsNullOrWhiteSpace(m.SnapshotText))
            .Select(m => {
                string text = m.SnapshotText;
                int newline = text.IndexOf('\n');
                return (newline < 0 ? text : text.Substring(0, newline)).Trim();
            }).ToList();
        return sentences.Count == 0 ? "" : WorldBulletinPolicy.BuildShortFromFacts(sentences[0], sentences.Skip(1));
    }

    internal static string BulletinFactTitle(EventRecordEntry entry)
        => WorldBulletinPolicy.TitleForKind(entry?.Materials?.FirstOrDefault(m => m?.MaterialType == ReportedMaterialType)?.ActionKind);

    internal static bool RequiresNpcDiplomacyKnowledge(EventRecordEntry entry)
        => entry?.Materials?.Any(m => m != null && (WorldBulletinPolicy.IsDiplomacyFact(m.ActionKind, null)
            || m.SourceStableKeys?.Any(k => WorldBulletinPolicy.IsDiplomacyFact(null, k)) == true)) == true;

    // A mixed issue's title/digest has no per-sentence provenance. Hide that narrative if any
    // declaration is unknown, while retaining visible factual materials. Never mutate the archive.
    internal static (string Title, string Facts, string Anecdote) ProjectBulletinForNpc(EventRecordEntry entry, ISet<string> knownDocumentIds)
    {
        var visible = new List<EventMaterialReference>();
        bool hidden = false;
        foreach (var material in entry?.Materials ?? Enumerable.Empty<EventMaterialReference>())
        {
            if (material == null) continue;
            bool diplomacy = WorldBulletinPolicy.IsDiplomacyFact(material.ActionKind, null)
                || material.SourceStableKeys?.Any(k => WorldBulletinPolicy.IsDiplomacyFact(null, k)) == true;
            if (diplomacy && (material.SourceStableKeys == null || material.SourceStableKeys.Count == 0
                || !material.SourceStableKeys.All(k => WorldBulletinPolicy.IsNpcFactVisible("diplomatic_declaration", k, knownDocumentIds))))
            {
                hidden = true;
                continue;
            }
            if (material.MaterialType == ReportedMaterialType) visible.Add(material);
        }
        // Untraceable legacy prose cannot establish permission to reveal diplomatic content.
        bool narrativeVisible = !hidden && visible.Count > 0;
        return (narrativeVisible ? entry?.Title ?? "" : "", FactSummary(visible),
            narrativeVisible ? entry?.BulletinAnecdote ?? "" : "");
    }

    internal static void AppendRecentMaterial(EventRecordEntry entry, EventMaterialReference material, HashSet<string> keys)
    {
        var sources=material.SourceStableKeys ?? new List<string>();
        if (sources.Any(k => keys.Contains(k))) return;
        foreach (var key in sources) if (!string.IsNullOrWhiteSpace(key)) keys.Add(key);
        entry.Materials.Add(material);
    }

    internal static string ProjectDateForDisplay(string rawDate)
    {
        if (string.IsNullOrWhiteSpace(rawDate)) return Localize("AF_UI_Timeline_UnknownDate", "Unknown date");
        string value = rawDate.Trim();
        if (string.Equals(value, "Unknown date", StringComparison.Ordinal) || string.Equals(value, WorldBulletinPolicyTexts.Raw("WBLT_Raw_044"), StringComparison.Ordinal))
            return Localize("AF_UI_Timeline_UnknownDate", "Unknown date");
        if (TryProjectKnownDay(value, out string display)) return display;
        const string separator = " — ";
        int separatorIndex = value.IndexOf(separator, StringComparison.Ordinal);
        if (separatorIndex >= 0 && value.IndexOf(separator, separatorIndex + separator.Length, StringComparison.Ordinal) < 0
            && TryProjectKnownDay(value.Substring(0, separatorIndex), out string first)
            && TryProjectKnownDay(value.Substring(separatorIndex + separator.Length), out string last))
            return first + separator + last;
        return rawDate;
    }

    private static bool TryProjectKnownDay(string value, out string display)
    {
        display = "";
        string number;
        if (value.StartsWith("Day ", StringComparison.Ordinal))
            number = value.Substring(4);
        else if (value.Length >= 4 && value.StartsWith(WorldBulletinPolicyTexts.Raw("WBLT_Raw_045"), StringComparison.Ordinal) && value.EndsWith(WorldBulletinPolicyTexts.Raw("WBLT_Raw_046"), StringComparison.Ordinal))
            number = value.Substring(2, value.Length - 4);
        else if (value.Length >= 2 && value.StartsWith(WorldBulletinPolicyTexts.Raw("WBLT_Raw_047"), StringComparison.Ordinal) && value.EndsWith(WorldBulletinPolicyTexts.Raw("WBLT_Raw_048"), StringComparison.Ordinal))
            number = value.Substring(1, value.Length - 2);
        else
            return false;
        if (!int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out int day)
            || !string.Equals(number, day.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
            return false;
        display = Localize("AF_UI_Timeline_Day", "Day {DAY}", ("DAY", day.ToString(CultureInfo.InvariantCulture)));
        return true;
    }

    internal static void RefreshRecentText(EventRecordEntry entry)
    {
        var materials=entry.Materials.Where(m => m != null).OrderBy(m => m.ActionDay ?? 0).ToList();
        if (materials.Count == 0) return;
        string Date(EventMaterialReference m) => string.IsNullOrWhiteSpace(m.Label)
            ? Localize("AF_UI_Weekly_Browser_DayNumber", "Day {DAY}", ("DAY", (m.ActionDay ?? 0).ToString(CultureInfo.InvariantCulture)))
            : m.Label.Trim();
        string first=Date(materials[0]), last=Date(materials[materials.Count-1]);
        entry.CreatedDate=first == last ? first : first + " — " + last;
        entry.CreatedDay=materials.Max(m => m.ActionDay ?? 0);
        entry.Summary=string.Join("\n\n", materials.Select(m => Date(m) + (m.MaterialType == RegionalMaterialType ? " · " + Localize("AF_UI_Weekly_Browser_HistoricalNews", "Historical news (archive retained)") : "")
            + "\n" + (m.SnapshotText ?? "").Trim()));
        entry.ShortSummary=WorldBulletinPolicy.Truncate(materials[materials.Count-1].SnapshotText ?? "", 120);
    }

    // The original front page never includes country digests.
    internal static string BodyWithRegionalNews(EventRecordEntry entry)
        => (string.IsNullOrWhiteSpace(entry.Summary) ? entry.ShortSummary : entry.Summary) ?? "";

    internal static List<EventRecordEntry> BuildArchiveSnapshot(List<EventRecordEntry> records,
        IReadOnlyDictionary<string, List<string>> legacyAssociations = null)
    {
        var result=new List<EventRecordEntry>();
        var recent=new Dictionary<string, EventRecordEntry>(StringComparer.OrdinalIgnoreCase);
        var sourceKeys=new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        void AddRecent(string nation, int week, string title, EventMaterialReference material)
        {
            nation=string.IsNullOrWhiteSpace(nation) ? OtherKingdomId : nation.Trim();
            string id=RecentId(nation, week);
            if (!recent.TryGetValue(id, out var entry))
            {
                entry=new EventRecordEntry { EventId=id, EventKind="kingdom", ScopeKingdomId=nation,
                    WeekIndex=Math.Max(0,week), Title=string.IsNullOrWhiteSpace(title) ? (nation == OtherKingdomId ? Localize("AF_UI_Weekly_Browser_OtherNews", "Other news") : nation + " " + Localize("AF_UI_Weekly_Browser_NewsSuffix", "news")) : title, Materials=new List<EventMaterialReference>() };
                recent.Add(id,entry);
                sourceKeys.Add(id,new HashSet<string>(StringComparer.Ordinal));
            }
            if (!string.IsNullOrWhiteSpace(title)) entry.Title=title;
            AppendRecentMaterial(entry,material,sourceKeys[id]);
        }
        foreach (var source in records ?? new List<EventRecordEntry>())
        {
            if (source == null) continue;
            if (IsRecent(source.EventId))
            {
                var materials=source.Materials ?? new List<EventMaterialReference>();
                for (int i=0;i<materials.Count;i++)
                    if (materials[i] != null) AddRecent(source.ScopeKingdomId,source.WeekIndex,source.Title,ArchiveMaterial(source,materials[i],i,false));
                if (materials.Count == 0 && !string.IsNullOrWhiteSpace(BodyWithRegionalNews(source)))
                    AddRecent(source.ScopeKingdomId,source.WeekIndex,source.Title,RegionalMaterial(source));
                continue;
            }
            if (IsRegionalSummary(source))
            {
                AddRecent(source.ScopeKingdomId,source.WeekIndex,"",RegionalMaterial(source));
                continue;
            }
            var entry=CloneForArchive(source);
            entry.BulletinKingdomIds=NormalizeKingdomIds(RelatedKingdomIds(source,legacyAssociations));
            var oldMaterials=source.Materials ?? new List<EventMaterialReference>();
            for (int i=0;i<oldMaterials.Count;i++)
            {
                var material=oldMaterials[i];
                if (material?.MaterialType == RegionalMaterialType)
                {
                    int week=Math.Max(0,(material.ActionDay ?? source.CreatedDay)/7);
                    // The old :brief identity is authoritative; its publication date may be in the next week.
                    foreach (var key in material.SourceStableKeys ?? new List<string>())
                    {
                        var parts=(key ?? "").Split(':');
                        if (parts.Length == 5 && parts[0] == "weekly_report" && parts[1] == "kingdom" && parts[4] == "brief"
                            && int.TryParse(parts[2],out int oldWeek)) { week=Math.Max(0,oldWeek); break; }
                    }
                    AddRecent(material.KingdomId,week,"",ArchiveMaterial(source,material,i,true));
                }
            }
            entry.Materials.RemoveAll(m => m?.MaterialType == RegionalMaterialType);
            result.Add(entry);
        }
        foreach (var entry in recent.Values) { RefreshRecentText(entry); result.Add(entry); }
        return result;
    }

    private static EventMaterialReference ArchiveMaterial(EventRecordEntry source, EventMaterialReference material, int index, bool legacy)
        => new EventMaterialReference {
            MaterialType=material.MaterialType, KingdomId=material.KingdomId,
            SnapshotText=WorldBulletinCampaignMaterialPolicy.ProjectRecordedBattle(material.SnapshotText, material.ActionKind), ActionDay=material.ActionDay ?? source.CreatedDay, ActionKind=material.ActionKind,
            Label=legacy ? (material.ActionDay == null || material.ActionDay == source.CreatedDay ? source.CreatedDate : "") : material.Label,
            SourceStableKeys=material.SourceStableKeys?.Any(k => !string.IsNullOrWhiteSpace(k)) == true
                ? material.SourceStableKeys.Where(k => !string.IsNullOrWhiteSpace(k)).ToList()
                : new List<string> { "historical:" + source.EventId + ":material:" + index }
        };

    internal static EventRecordEntry CloneForArchive(EventRecordEntry e) => new EventRecordEntry {
        EventId=e.EventId, EventKind=e.EventKind, ScopeKingdomId=e.ScopeKingdomId, WeekIndex=e.WeekIndex,
        Title=ProjectTitleForDisplay(e.EventId,e.Title), Summary=e.Summary, ShortSummary=e.ShortSummary, BulletinAnecdote=e.BulletinAnecdote, TagText=e.TagText, PromptText=e.PromptText,
        CreatedDay=e.CreatedDay, CreatedDate=e.CreatedDate, BulletinKingdomIds=NormalizeKingdomIds(e.BulletinKingdomIds),
        Materials=new List<EventMaterialReference>(e.Materials ?? new List<EventMaterialReference>())
    };

    internal static bool IsBulletin(string id) => (id ?? "").IndexOf(":bulletin:", StringComparison.OrdinalIgnoreCase) >= 0;
    internal static List<string> NormalizeKingdomIds(IEnumerable<string> ids) => (ids ?? Enumerable.Empty<string>())
        .Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    internal static List<string> CaptureKingdomIds(WorldBulletinSelection selection) => NormalizeKingdomIds(
        (selection?.Major == null ? Enumerable.Empty<WorldBulletinEvent>() : new[] {selection.Major}).Concat(selection?.MajorFacts ?? new List<WorldBulletinEvent>())
        .Concat((selection?.Minors ?? new List<WorldBulletinMinor>()).SelectMany(m => m.Events)).SelectMany(f => f.KingdomIds ?? new List<string>()));
    internal static IReadOnlyList<string> RelatedKingdomIds(EventRecordEntry entry, IReadOnlyDictionary<string, List<string>> legacyAssociations)
    {
        if (entry == null) return Array.Empty<string>();
        if (entry.BulletinKingdomIds?.Count > 0) return entry.BulletinKingdomIds;
        if (IsBulletin(entry.EventId) && legacyAssociations != null && legacyAssociations.TryGetValue((entry.EventId ?? "").Trim(), out var ids) && ids != null) return ids;
        return Array.Empty<string>();
    }
    internal static bool Matches(EventRecordEntry entry, string kind, string nation, IReadOnlyDictionary<string, List<string>> legacyAssociations)
    {
        if (entry == null) return false;
        if (string.Equals(kind,"world",StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(nation)) return true;
        if (string.Equals(entry.EventKind,kind,StringComparison.OrdinalIgnoreCase) && string.Equals(entry.ScopeKingdomId,nation,StringComparison.OrdinalIgnoreCase)) return true;
        return string.Equals(kind,"kingdom",StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(nation)
            && RelatedKingdomIds(entry,legacyAssociations).Any(id => string.Equals(id?.Trim(),nation,StringComparison.OrdinalIgnoreCase));
    }
    internal static string EntryKind(string id) => IsBulletin(id) ? "bulletin" : IsRecent(id) ? "recent" : "weekly";
    internal static string KindLabel(string id) => IsBulletin(id) ? Localize("AF_UI_Weekly_Browser_KindBulletin", "Bulletin") : IsRecent(id) ? Localize("AF_UI_Weekly_Browser_KindRecent", "Kingdom news") : Localize("AF_UI_Weekly_Browser_KindWeekly", "Weekly archive");
    internal static long IssueNumber(string id)
    {
        id ??= "";
        int marker=id.IndexOf(":bulletin:",StringComparison.OrdinalIgnoreCase);
        if (marker < 0) return 0;
        int start=marker+":bulletin:".Length, end=id.IndexOf(':',start);
        return long.TryParse(end < 0 ? id.Substring(start) : id.Substring(start,end-start), NumberStyles.None,CultureInfo.InvariantCulture,out long issue) ? Math.Max(0,issue) : 0;
    }
    internal static string PeriodLabel(string id, int week) => IsRecent(id) ? Localize("AF_UI_Weekly_Browser_KindRecent", "Kingdom news") : IsBulletin(id)
        ? Localize("AF_UI_Weekly_Browser_KindBulletin", "Bulletin") + (IssueNumber(id)>0 ? " · " + Localize("AF_UI_Weekly_Browser_Issue", "Issue {NUMBER}", ("NUMBER", IssueNumber(id).ToString(CultureInfo.InvariantCulture))) : "") : Localize("AF_UI_Weekly_Browser_KindWeekly", "Weekly archive");

    internal static string CalendarDate(string gameFormattedDate)
    {
        string date = (gameFormattedDate ?? "").Trim();
        if (date.Length == 0) return Localize("AF_UI_Timeline_UnknownDate", "Unknown date");
        if (date.StartsWith(WorldBulletinPolicyTexts.Raw("WBLT_Raw_032"), StringComparison.Ordinal) || date.StartsWith("Calradia", StringComparison.OrdinalIgnoreCase)) return date;
        return Localize("AF_UI_Weekly_Browser_CalradiaDate", "Calradia {DATE}", ("DATE", date));
    }
}
