using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static AnimusForge.MyBehavior;

namespace AnimusForge;

// Detached archive projection; original legacy records remain intact on disk.
internal static class WeeklyReportArchivePolicy
{
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
            MaterialType=type, KingdomId=nation, SnapshotText=fact.Sentence + (string.IsNullOrWhiteSpace(fact.Detail) ? "" : "\n补充事实：" + fact.Detail), Label=fact.GameDate,
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

    internal static void RefreshRecentText(EventRecordEntry entry)
    {
        var materials=entry.Materials.Where(m => m != null).OrderBy(m => m.ActionDay ?? 0).ToList();
        if (materials.Count == 0) return;
        string Date(EventMaterialReference m) => string.IsNullOrWhiteSpace(m.Label) ? "第 " + (m.ActionDay ?? 0) + " 日" : m.Label.Trim();
        string first=Date(materials[0]), last=Date(materials[materials.Count-1]);
        entry.CreatedDate=first == last ? first : first + " — " + last;
        entry.CreatedDay=materials.Max(m => m.ActionDay ?? 0);
        entry.Summary=string.Join("\n\n", materials.Select(m => Date(m) + (m.MaterialType == RegionalMaterialType ? " · 历史近况（原档保留）" : "")
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
                    WeekIndex=Math.Max(0,week), Title=string.IsNullOrWhiteSpace(title) ? (nation == OtherKingdomId ? "其他近况" : nation + "近况") : title, Materials=new List<EventMaterialReference>() };
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
            SnapshotText=material.SnapshotText, ActionDay=material.ActionDay ?? source.CreatedDay, ActionKind=material.ActionKind,
            Label=legacy ? (material.ActionDay == null || material.ActionDay == source.CreatedDay ? source.CreatedDate : "") : material.Label,
            SourceStableKeys=material.SourceStableKeys?.Any(k => !string.IsNullOrWhiteSpace(k)) == true
                ? material.SourceStableKeys.Where(k => !string.IsNullOrWhiteSpace(k)).ToList()
                : new List<string> { "historical:" + source.EventId + ":material:" + index }
        };

    internal static EventRecordEntry CloneForArchive(EventRecordEntry e) => new EventRecordEntry {
        EventId=e.EventId, EventKind=e.EventKind, ScopeKingdomId=e.ScopeKingdomId, WeekIndex=e.WeekIndex,
        Title=e.Title, Summary=e.Summary, ShortSummary=e.ShortSummary, BulletinAnecdote=e.BulletinAnecdote, TagText=e.TagText, PromptText=e.PromptText,
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
    internal static string KindLabel(string id) => IsBulletin(id) ? "即时快报" : IsRecent(id) ? "王国近况" : "周报档案";
    internal static long IssueNumber(string id)
    {
        id ??= "";
        int marker=id.IndexOf(":bulletin:",StringComparison.OrdinalIgnoreCase);
        if (marker < 0) return 0;
        int start=marker+":bulletin:".Length, end=id.IndexOf(':',start);
        return long.TryParse(end < 0 ? id.Substring(start) : id.Substring(start,end-start), NumberStyles.None,CultureInfo.InvariantCulture,out long issue) ? Math.Max(0,issue) : 0;
    }
    internal static string PeriodLabel(string id, int week) => IsRecent(id) ? "王国近况" : IsBulletin(id)
        ? "即时快报" + (IssueNumber(id)>0 ? " · 第 " + IssueNumber(id) + " 期" : "") : "周报档案";

    internal static string CalendarDate(string gameFormattedDate)
    {
        string date = (gameFormattedDate ?? "").Trim();
        if (date.Length == 0) return "日期未知";
        return date.StartsWith("卡拉迪亚", StringComparison.Ordinal) ? date : "卡拉迪亚" + date;
    }
}
