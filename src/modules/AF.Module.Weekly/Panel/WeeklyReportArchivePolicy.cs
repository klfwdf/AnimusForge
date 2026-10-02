using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static AnimusForge.MyBehavior;

namespace AnimusForge;

// Read/display policy only. One archive snapshot per open/reload, no per-frame scan,
// no duplicate records, additional LLM calls or inference from generated prose.
internal static class WeeklyReportArchivePolicy
{
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
        if (entry == null || !IsBulletin(entry.EventId)) return Array.Empty<string>();
        if (entry.BulletinKingdomIds?.Count > 0) return entry.BulletinKingdomIds;
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
            && !string.IsNullOrWhiteSpace(kingdomId) && IsBulletin(entry.EventId)
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
