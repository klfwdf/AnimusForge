using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static AnimusForge.MyBehavior;

namespace AnimusForge;

// Archive work runs on open/reload, never per frame. Original legacy records stay intact.
internal static class WeeklyReportArchivePolicy
{
    internal const string RegionalMaterialType = "bulletin_regional_news";

    internal static bool IsRegionalSummary(EventRecordEntry entry)
        => entry != null && string.Equals(entry.EventKind, "kingdom", StringComparison.OrdinalIgnoreCase)
            && !IsBulletin(entry.EventId) && entry.WeekIndex > 0
            && ((entry.EventId ?? "").EndsWith(":brief", StringComparison.OrdinalIgnoreCase)
                || (string.IsNullOrWhiteSpace(entry.Summary) && !string.IsNullOrWhiteSpace(entry.ShortSummary)));

    internal static EventMaterialReference RegionalMaterial(EventRecordEntry entry)
        => new EventMaterialReference {
            MaterialType = RegionalMaterialType, Label = entry.Title, KingdomId = entry.ScopeKingdomId,
            SnapshotText = string.IsNullOrWhiteSpace(entry.Summary) ? entry.ShortSummary : entry.Summary,
            ActionDay = entry.CreatedDay, SourceStableKeys = new List<string> { entry.EventId }
        };

    internal static void AttachRegionalNews(EventRecordEntry issue, EventMaterialReference news)
    {
        issue.Materials ??= new List<EventMaterialReference>();
        string key = news.SourceStableKeys?.FirstOrDefault();
        int existing = issue.Materials.FindIndex(m => m?.MaterialType == RegionalMaterialType
            && m.SourceStableKeys?.Contains(key, StringComparer.OrdinalIgnoreCase) == true);
        if (existing >= 0) issue.Materials[existing] = news;
        else issue.Materials.Add(news);
        issue.BulletinKingdomIds = NormalizeKingdomIds((issue.BulletinKingdomIds ?? new List<string>()).Concat(new[] { news.KingdomId }));
    }

    internal static string BodyWithRegionalNews(EventRecordEntry entry)
    {
        var news = entry.Materials?.Where(m => m?.MaterialType == RegionalMaterialType).ToList();
        string body = (string.IsNullOrWhiteSpace(entry.Summary) ? entry.ShortSummary : entry.Summary) ?? "";
        if (news == null || news.Count == 0) return body;
        if (IsBulletin(entry.EventId) && body.IndexOf("【其他消息】", StringComparison.Ordinal) < 0)
            body += "\n\n【其他消息】";
        var text = new System.Text.StringBuilder(body);
        foreach (var item in news)
            text.Append("\n\n").Append(item.Label ?? "各地消息").Append('：')
                .Append((item.SnapshotText ?? "").Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' '));
        return text.ToString().Trim();
    }

    internal static List<EventMaterialReference> CountryNews(EventRecordEntry entry, string kingdomId)
        => (entry.Materials ?? new List<EventMaterialReference>()).Where(m => m?.MaterialType == RegionalMaterialType
            && string.Equals(m.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase)).ToList();

    internal static string CountryBody(EventRecordEntry entry, string kingdomId)
    {
        var news = CountryNews(entry, kingdomId);
        if (news.Count > 0)
            return string.Join("\n\n", news.Select(m => (m.SnapshotText ?? "").Trim()));
        // Only genuinely associated nations without a regional excerpt share the original issue.
        return (string.IsNullOrWhiteSpace(entry.Summary) ? entry.ShortSummary : entry.Summary) ?? "";
    }

    internal static List<EventRecordEntry> BuildArchiveSnapshot(List<EventRecordEntry> records,
        IReadOnlyDictionary<string, List<string>> legacyAssociations = null)
    {
        var result = (records ?? new List<EventRecordEntry>()).Where(e => e != null && !IsRegionalSummary(e))
            .Select(CloneForArchive).ToList();
        foreach (var entry in result)
            entry.BulletinKingdomIds = NormalizeKingdomIds(RelatedKingdomIds(entry, legacyAssociations));
        var bulletins = result.Where(e => IsBulletin(e.EventId) && string.Equals(e.EventKind, "world", StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.CreatedDay).ThenBy(e => IssueNumber(e.EventId)).ToList();
        var worldWeeks = result.Where(e => !IsBulletin(e.EventId) && string.Equals(e.EventKind, "world", StringComparison.OrdinalIgnoreCase))
            .GroupBy(e => e.WeekIndex).ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.CreatedDay).First());
        var orphanWeeks = new Dictionary<int, EventRecordEntry>();
        foreach (var summary in (records ?? new List<EventRecordEntry>()).Where(IsRegionalSummary))
        {
            bool brief = (summary.EventId ?? "").EndsWith(":brief", StringComparison.OrdinalIgnoreCase);
            int endDay = Math.Max(summary.CreatedDay, summary.WeekIndex * 7);
            int startDay = brief ? (summary.WeekIndex - 1) * 7 : Math.Max(0, endDay - 7);
            // Weekly short reports belong to their exact weekly edition; bulletin briefs to the latest issue in their covered week.
            EventRecordEntry parent = !brief && worldWeeks.TryGetValue(summary.WeekIndex, out var weekly) ? weekly : null;
            if (parent == null)
            {
                int low = 0, high = bulletins.Count;
                while (low < high) { int mid = low + (high - low) / 2; if (bulletins[mid].CreatedDay <= endDay) low = mid + 1; else high = mid; }
                if (low > 0 && bulletins[low - 1].CreatedDay >= startDay) parent = bulletins[low - 1];
            }
            if (parent == null && worldWeeks.TryGetValue(summary.WeekIndex, out weekly)) parent = weekly;
            if (parent == null && !orphanWeeks.TryGetValue(summary.WeekIndex, out parent))
            {
                // An old save may have only local records. Keep their full text in one browseable edition per week.
                parent = new EventRecordEntry { EventId = "weekly_report:world:bulletin:archive:" + summary.WeekIndex,
                    EventKind = "world", ScopeKingdomId = "", WeekIndex = summary.WeekIndex,
                    Title = "第" + summary.WeekIndex + "周各地消息", Summary = "【其他消息】",
                    CreatedDay = endDay, CreatedDate = summary.CreatedDate };
                orphanWeeks.Add(summary.WeekIndex, parent); result.Add(parent);
            }
            AttachRegionalNews(parent, RegionalMaterial(summary));
        }
        return result;
    }

    private static EventRecordEntry CloneForArchive(EventRecordEntry e)
        => new EventRecordEntry { EventId = e.EventId, EventKind = e.EventKind, ScopeKingdomId = e.ScopeKingdomId,
            WeekIndex = e.WeekIndex, Title = e.Title, Summary = e.Summary, ShortSummary = e.ShortSummary,
            TagText = e.TagText, PromptText = e.PromptText, CreatedDay = e.CreatedDay, CreatedDate = e.CreatedDate,
            BulletinKingdomIds = NormalizeKingdomIds(e.BulletinKingdomIds),
            Materials = new List<EventMaterialReference>(e.Materials ?? new List<EventMaterialReference>()) };

    internal static bool IsBulletin(string eventId)
        => (eventId ?? "").IndexOf(":bulletin:", StringComparison.OrdinalIgnoreCase) >= 0;

    internal static List<string> NormalizeKingdomIds(IEnumerable<string> ids)
        => (ids ?? Enumerable.Empty<string>()).Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    internal static List<string> CaptureKingdomIds(WorldBulletinSelection selection)
    {
        var ids = new List<string>();
        if (selection?.Major?.KingdomIds != null) ids.AddRange(selection.Major.KingdomIds);
        foreach (var fact in selection?.MajorFacts ?? new List<WorldBulletinEvent>())
            if (fact?.KingdomIds != null) ids.AddRange(fact.KingdomIds);
        foreach (var minor in selection?.Minors ?? new List<WorldBulletinMinor>())
            foreach (var fact in minor?.Events ?? new List<WorldBulletinEvent>())
                if (fact?.KingdomIds != null) ids.AddRange(fact.KingdomIds);
        return NormalizeKingdomIds(ids);
    }

    internal static IReadOnlyList<string> RelatedKingdomIds(EventRecordEntry entry,
        IReadOnlyDictionary<string, List<string>> legacyAssociations)
    {
        if (entry == null) return Array.Empty<string>();
        if (entry.BulletinKingdomIds?.Count > 0) return entry.BulletinKingdomIds;
        if (!IsBulletin(entry.EventId)) return Array.Empty<string>();
        if (legacyAssociations != null && legacyAssociations.TryGetValue((entry.EventId ?? "").Trim(), out var ids) && ids != null)
            return ids;
        return Array.Empty<string>(); // No trustworthy old metadata: do not guess from prose.
    }

    internal static bool Matches(EventRecordEntry entry, string eventKind, string kingdomId,
        IReadOnlyDictionary<string, List<string>> legacyAssociations)
    {
        if (entry == null) return false;
        if (string.Equals((entry.EventKind ?? "").Trim(), eventKind, StringComparison.OrdinalIgnoreCase)
            && string.Equals((entry.ScopeKingdomId ?? "").Trim(), kingdomId, StringComparison.OrdinalIgnoreCase)) return true;
        return string.Equals(eventKind, "kingdom", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(kingdomId)
            && RelatedKingdomIds(entry, legacyAssociations).Any(id => string.Equals((id ?? "").Trim(), kingdomId, StringComparison.OrdinalIgnoreCase));
    }

    internal static string KindLabel(string eventId)
        => IsBulletin(eventId) ? "即时快报"
            : (eventId ?? "").EndsWith(":brief", StringComparison.OrdinalIgnoreCase) ? "王国局势提要" : "周报档案";

    internal static long IssueNumber(string eventId)
    {
        string id = eventId ?? "";
        int marker = id.IndexOf(":bulletin:", StringComparison.OrdinalIgnoreCase);
        if (marker < 0) return 0;
        int start = marker + ":bulletin:".Length;
        int end = id.IndexOf(':', start);
        return long.TryParse(end < 0 ? id.Substring(start) : id.Substring(start, end - start),
            NumberStyles.None, CultureInfo.InvariantCulture, out long issue) ? Math.Max(0, issue) : 0;
    }

    internal static string PeriodLabel(string eventId, int week)
    {
        long issue = IssueNumber(eventId);
        return IsBulletin(eventId) ? "即时快报" + (issue > 0 ? " · 第 " + issue + " 期" : "")
            : KindLabel(eventId) + " · 第 " + Math.Max(0, week) + " 周";
    }
}
