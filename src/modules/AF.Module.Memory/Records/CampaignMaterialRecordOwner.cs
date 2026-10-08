using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Runtime;
using EventSourceMaterialEntry = AnimusForge.MyBehavior.EventSourceMaterialEntry;

namespace AnimusForge;

// Event-time record flow. Index owns misses as well as hits; no historical scan on append.
internal sealed class CampaignMaterialRecordOwner
{
    internal List<EventSourceMaterialEntry> Materials = new List<EventSourceMaterialEntry>();
    internal Dictionary<string, EventSourceMaterialEntry> Index = new Dictionary<string, EventSourceMaterialEntry>(StringComparer.OrdinalIgnoreCase);
    internal readonly EventSourceMaterialIndex<EventSourceMaterialEntry> Binding =
        new EventSourceMaterialIndex<EventSourceMaterialEntry>(x => x.Day, x => x.StableKey, BuildEventSourceMaterialIndexKey);

    internal void Record(EventSourceMaterialEntry capture, Func<int> nextSequence, Action<int> markDay, Action<int> markAppend = null)
    {
        if (capture == null || string.IsNullOrWhiteSpace(capture.SnapshotText)) return;
        Materials ??= new List<EventSourceMaterialEntry>();
        if (!Binding.IsCurrent(Materials, Index)) RebuildEventSourceMaterialIndex();
        string key = NpcActionLedger.NormalizeStableKey(capture.StableKey, capture.Label + ":" + capture.SnapshotText);
        string indexKey = BuildEventSourceMaterialIndexKey(capture.Day, key);
        Index.TryGetValue(indexKey, out var existing);
        if (existing != null)
        {
            existing.Label = capture.Label;
            existing.SnapshotText = capture.SnapshotText;
            existing.MaterialKind = capture.MaterialKind;
            existing.KingdomId = (capture.KingdomId ?? "").Trim();
            existing.SettlementId = (capture.SettlementId ?? "").Trim();
            existing.ActorHeroId = (capture.ActorHeroId ?? "").Trim();
            existing.ActorKingdomId = (capture.ActorKingdomId ?? "").Trim();
            existing.IncludeInWorld |= capture.IncludeInWorld;
            existing.IncludeInKingdom |= capture.IncludeInKingdom;
            markDay(capture.Day);
            return;
        }
        capture.Sequence = nextSequence();
        capture.StableKey = key;
        capture.KingdomId = (capture.KingdomId ?? "").Trim();
        capture.SettlementId = (capture.SettlementId ?? "").Trim();
        capture.ActorHeroId = (capture.ActorHeroId ?? "").Trim();
        capture.ActorKingdomId = (capture.ActorKingdomId ?? "").Trim();
        Materials.Add(capture);
        (markAppend ?? markDay)(capture.Day);
        Index[indexKey] = capture;
        // Publish binding only after both writes; an exception leaves it stale.
        Binding.Bind(Materials, Index);
    }

	internal static string BuildEventSourceMaterialIndexKey(int day, string stableKey)
	{
		string text = (stableKey ?? "").Trim();
		return Math.Max(0, day) + "|" + text;
	}

	internal void RebuildEventSourceMaterialIndex()
	{
		var source = Materials;
		var rebuilt = Binding.Build(source);
		if (!ReferenceEquals(source, Materials)) throw new InvalidOperationException("Event material source changed during index rebuild.");
		Index = rebuilt;
		Binding.Bind(source, rebuilt);
	}

    // Daily recovery only needs keys. Keep the sanitizer's blank-body and fallback-key
    // semantics, without copying every record/body or sorting historical materials.
    internal static HashSet<string> BuildStableKeySet(List<EventSourceMaterialEntry> source)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (source == null) return keys;
        foreach (var item in source)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.SnapshotText)) continue;
            string key = NpcActionLedger.NormalizeStableKey(item.StableKey, item.SnapshotText);
            if (!string.IsNullOrWhiteSpace(key)) keys.Add(key);
        }
        return keys;
    }

	internal static List<EventSourceMaterialEntry> SanitizeEventSourceMaterials(List<EventSourceMaterialEntry> source)
	{
		List<EventSourceMaterialEntry> list = new List<EventSourceMaterialEntry>();
		if (source == null)
		{
			return list;
		}
		foreach (EventSourceMaterialEntry item in source)
		{
			string text = (item?.SnapshotText ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (item == null || string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			list.Add(new EventSourceMaterialEntry
			{
				Day = Math.Max(0, item.Day),
				Sequence = Math.Max(0, item.Sequence),
				GameDate = (item.GameDate ?? "").Trim(),
				MaterialKind = (item.MaterialKind ?? "").Trim(),
				Label = (item.Label ?? "").Trim(),
				SnapshotText = text,
				StableKey = NpcActionLedger.NormalizeStableKey(item.StableKey, text),
				KingdomId = (item.KingdomId ?? "").Trim(),
				SettlementId = (item.SettlementId ?? "").Trim(),
				ActorHeroId = (item.ActorHeroId ?? "").Trim(),
				ActorKingdomId = (item.ActorKingdomId ?? "").Trim(),
				IncludeInWorld = item.IncludeInWorld,
				IncludeInKingdom = item.IncludeInKingdom
			});
		}
		return list.OrderBy((EventSourceMaterialEntry x) => x.Day).ThenBy((EventSourceMaterialEntry x) => (x.Sequence > 0) ? x.Sequence : int.MaxValue).ThenBy((EventSourceMaterialEntry x) => x.Label ?? "", StringComparer.OrdinalIgnoreCase).ToList();
	}

    internal void ResetMaterials() => Materials = new List<EventSourceMaterialEntry>();
    internal void EnsureMaterials() => Materials ??= new List<EventSourceMaterialEntry>();

}
