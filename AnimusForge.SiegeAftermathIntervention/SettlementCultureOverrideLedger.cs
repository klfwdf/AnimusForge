using System;
using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Stores explicit GCCZ settlement culture overrides independently from Bannerlord runtime objects.
/// </summary>
public sealed class SettlementCultureOverrideLedger
{
    private readonly Dictionary<string, string> _cultureBySettlementId =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public int Count => _cultureBySettlementId.Count;

    public bool TryRecord(string settlementId, string cultureId)
    {
        if (!TryNormalizePair(settlementId, cultureId, out string normalizedSettlementId, out string normalizedCultureId))
        {
            return false;
        }

        _cultureBySettlementId[normalizedSettlementId] = normalizedCultureId;
        return true;
    }

    public bool TryRecordIfMissing(string settlementId, string cultureId)
    {
        if (!TryNormalizePair(settlementId, cultureId, out string normalizedSettlementId, out string normalizedCultureId)
            || _cultureBySettlementId.ContainsKey(normalizedSettlementId))
        {
            return false;
        }

        _cultureBySettlementId.Add(normalizedSettlementId, normalizedCultureId);
        return true;
    }

    public bool TryGetCultureId(string settlementId, out string cultureId)
    {
        return _cultureBySettlementId.TryGetValue(Normalize(settlementId), out cultureId);
    }

    public int Restore(IEnumerable<KeyValuePair<string, string>> entries)
    {
        _cultureBySettlementId.Clear();
        int rejected = 0;
        foreach (KeyValuePair<string, string> entry in entries ?? Array.Empty<KeyValuePair<string, string>>())
        {
            if (!TryRecord(entry.Key, entry.Value))
            {
                rejected++;
            }
        }

        return rejected;
    }

    public Dictionary<string, string> CopyEntries()
    {
        return new Dictionary<string, string>(_cultureBySettlementId, StringComparer.OrdinalIgnoreCase);
    }

    public void Clear()
    {
        _cultureBySettlementId.Clear();
    }

    private static bool TryNormalizePair(
        string settlementId,
        string cultureId,
        out string normalizedSettlementId,
        out string normalizedCultureId)
    {
        normalizedSettlementId = Normalize(settlementId);
        normalizedCultureId = Normalize(cultureId);
        return !string.IsNullOrWhiteSpace(normalizedSettlementId)
            && !string.IsNullOrWhiteSpace(normalizedCultureId);
    }

    private static string Normalize(string value)
    {
        return (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
    }
}
