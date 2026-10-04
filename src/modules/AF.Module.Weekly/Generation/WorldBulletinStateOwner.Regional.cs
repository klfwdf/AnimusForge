using System;
using System.Collections.Generic;
using System.Linq;
using static AnimusForge.MyBehavior;

namespace AnimusForge;

internal sealed partial class WorldBulletinStateOwner
{
    // Only publication scans the detached window; the original campaign fact pool stays intact.
    internal List<EventRecordEntry> BuildRegionalPublication(WorldBulletinSelection selection)
    {
        var reported = new HashSet<string>(selection.ReportedKeys, StringComparer.Ordinal);
        foreach (var fact in selection.MajorFacts.Concat(new[] {selection.Major}).Concat(selection.Minors.SelectMany(m => m.Events))) reported.Add(fact.Key);
        var products = new Dictionary<string, EventRecordEntry>(StringComparer.OrdinalIgnoreCase);
        var existingRecords = new Dictionary<string,EventRecordEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var record in _port.Records()) if (record != null) existingRecords[record.EventId] = record;
        var sourceKeys = new Dictionary<string,HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var fact in selection.WindowFacts)
        {
            if (reported.Contains(fact.Key)) continue;
            var nations = WeeklyReportArchivePolicy.NormalizeKingdomIds(fact.KingdomIds);
            if (nations.Count == 0) nations.Add(WeeklyReportArchivePolicy.OtherKingdomId);
            foreach (string nation in nations)
            {
                int week = Math.Max(0, fact.Day / 7);
                string id = WeeklyReportArchivePolicy.RecentId(nation, week);
                if (!products.TryGetValue(id, out var entry))
                {
                    existingRecords.TryGetValue(id,out var existing);
                    entry = existing == null ? new EventRecordEntry {
                        EventId=id, EventKind="kingdom", ScopeKingdomId=nation, WeekIndex=week,
                        Title=nation == WeeklyReportArchivePolicy.OtherKingdomId ? "其他近况" : _port.ResolveKingdom(nation) + "近况"
                    } : WeeklyReportArchivePolicy.CloneForArchive(existing);
                    products.Add(id, entry);
                    sourceKeys.Add(id,WeeklyReportArchivePolicy.RecentSourceKeys(entry));
                }
                WeeklyReportArchivePolicy.AppendRecentMaterial(entry, WeeklyReportArchivePolicy.FactMaterial(fact, nation), sourceKeys[id]);
            }
        }
        foreach (var entry in products.Values) WeeklyReportArchivePolicy.RefreshRecentText(entry);
        return products.Values.ToList();
    }

    private void CommitRegionalPublication(List<EventRecordEntry> products)
    {
        var records = _port.Records();
        var indexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i=0; i<records.Count; i++)
            if (records[i] != null) indexes[records[i].EventId] = i;
        foreach (var product in products)
        {
            if (indexes.TryGetValue(product.EventId, out int index)) records[index] = product;
            else records.Add(product);
        }
        _port.Log("WorldBulletin", "[RegionalPublish] products=" + products.Count + " facts=" + products.Sum(p => p.Materials.Count));
    }
}
