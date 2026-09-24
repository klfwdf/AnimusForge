using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

internal sealed class KingdomStabilityOwner
{
    // Host save adapters bind these same dictionaries; there is no second ledger.
    internal Dictionary<string, int> Values = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    internal Dictionary<string, int> RelationOffsets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    internal Dictionary<string, int> WeeklyDeltas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    internal int Get(string kingdomId)
    {
        string id = (kingdomId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(id)) return KingdomStabilityPolicy.KingdomStabilityDefaultValue;
        Values ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        return Values.TryGetValue(id, out int value) ? KingdomStabilityPolicy.ClampKingdomStabilityValue(value) : KingdomStabilityPolicy.KingdomStabilityDefaultValue;
    }

    internal void Set(string kingdomId, int value)
    {
        string id = (kingdomId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(id)) return;
        Values ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Values[id] = KingdomStabilityPolicy.ClampKingdomStabilityValue(value);
    }

    // Caller resolves the live pair and changes its relation on the Campaign thread.
    // Null means an unresolvable pair: drop an old offset, skip a new one.
    internal void Reconcile(string kingdomId, Dictionary<string, int> desired, Func<string, int, int?> apply)
    {
        RelationOffsets ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        List<string> oldKeys = RelationOffsets.Keys.Where(key => !string.IsNullOrWhiteSpace(key)
            && key.StartsWith(kingdomId + "|", StringComparison.OrdinalIgnoreCase)).ToList();
        var existing = new HashSet<string>(oldKeys, StringComparer.OrdinalIgnoreCase);
        foreach (string key in oldKeys)
        {
            desired.TryGetValue(key, out int target);
            int? applied = apply(key, target);
            if (!applied.HasValue || applied.Value == 0) RelationOffsets.Remove(key);
            else RelationOffsets[key] = applied.Value;
        }
        foreach (KeyValuePair<string, int> target in desired)
        {
            if (existing.Contains(target.Key)) continue;
            int? applied = apply(target.Key, target.Value);
            if (applied.HasValue && applied.Value != 0) RelationOffsets[target.Key] = applied.Value;
        }
    }

    internal void ApplyWeeklyDelta(string eventId, int currentStability, int desired, Action<int> writeStability)
    {
        WeeklyDeltas ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        WeeklyDeltas.TryGetValue(eventId, out int previous);
        if (desired == previous) return;
        writeStability(currentStability - previous + desired);
        if (desired == 0) WeeklyDeltas.Remove(eventId);
        else WeeklyDeltas[eventId] = desired;
    }

    internal static int ResolveRelation(int current, int previousOffset, int desiredOffset, out int appliedOffset)
    {
        int baseline = current - previousOffset;
        int target = Math.Max(-100, Math.Min(100, baseline + desiredOffset));
        appliedOffset = target - baseline;
        return target;
    }
}
