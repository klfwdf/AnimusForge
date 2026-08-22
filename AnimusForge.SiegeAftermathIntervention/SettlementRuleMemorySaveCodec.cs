using System;
using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Converts the primitive AF save dictionary to and from validated town-memory records.
/// Missing optional storage is an empty batch; malformed entries are isolated from valid towns.
/// </summary>
public static class SettlementRuleMemorySaveCodec
{
    public static Dictionary<string, string> Encode(
        IEnumerable<SettlementRuleMemoryRecord> records)
    {
        var encoded = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (SettlementRuleMemoryRecord record in records ?? Array.Empty<SettlementRuleMemoryRecord>())
        {
            string settlementId = (record?.SettlementId ?? string.Empty).Trim();
            string payload = SettlementRuleMemoryCodec.Encode(record);
            if (!string.IsNullOrWhiteSpace(settlementId) && !string.IsNullOrWhiteSpace(payload))
            {
                encoded[settlementId] = payload;
            }
        }
        return encoded;
    }

    public static SettlementRuleMemorySaveDecodeResult Decode(
        IEnumerable<KeyValuePair<string, string>> serializedRecords)
    {
        var records = new List<SettlementRuleMemoryRecord>();
        int rejectedCount = 0;
        foreach (KeyValuePair<string, string> entry in serializedRecords
            ?? Array.Empty<KeyValuePair<string, string>>())
        {
            if (SettlementRuleMemoryCodec.TryDecode(entry.Key, entry.Value, out SettlementRuleMemoryRecord record))
            {
                records.Add(record);
            }
            else
            {
                rejectedCount++;
            }
        }
        return new SettlementRuleMemorySaveDecodeResult(records, rejectedCount);
    }
}

public sealed class SettlementRuleMemorySaveDecodeResult
{
    public SettlementRuleMemorySaveDecodeResult(
        IReadOnlyList<SettlementRuleMemoryRecord> records,
        int rejectedCount)
    {
        Records = records == null
            ? Array.Empty<SettlementRuleMemoryRecord>()
            : new List<SettlementRuleMemoryRecord>(records).AsReadOnly();
        RejectedCount = Math.Max(0, rejectedCount);
    }

    public IReadOnlyList<SettlementRuleMemoryRecord> Records { get; }

    public int RejectedCount { get; }
}
