using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Versioned primitive-string codec used by the AF save adapter.
/// </summary>
public static class SettlementRuleMemoryCodec
{
    private const string CurrentVersionToken = "v2";
    private const string LegacyVersionToken = "v1";
    private const int CurrentHeaderFieldCount = 4;
    private const int CurrentEntryFieldCount = 11;
    private const int LegacyFieldCount = 17;

    public static string Encode(SettlementRuleMemoryRecord record)
    {
        if (record == null || string.IsNullOrWhiteSpace(record.SettlementId) || record.CurrentRule == null)
        {
            return string.Empty;
        }

        var fields = new List<string>
        {
            CurrentVersionToken,
            EncodeText(record.SettlementName),
            Math.Max(0, record.CultureStartDay).ToString(CultureInfo.InvariantCulture),
            Math.Min(record.RulerMemories.Count, SettlementRuleMemoryStore.MaximumRulerMemories).ToString(CultureInfo.InvariantCulture),
        };
        for (int index = 0; index < record.RulerMemories.Count
            && index < SettlementRuleMemoryStore.MaximumRulerMemories; index++)
        {
            SettlementRuleMemoryEntry entry = record.RulerMemories[index];
            fields.Add(EncodeText(entry.RulerId));
            fields.Add(EncodeText(entry.RulerName));
            fields.Add(EncodeText(entry.CultureId));
            fields.Add(EncodeText(entry.CultureName));
            fields.Add(EncodeText(entry.RulerPersonality));
            fields.Add(entry.RuleStartDay.ToString(CultureInfo.InvariantCulture));
            fields.Add(entry.MinimumRuleDurationDays.ToString(CultureInfo.InvariantCulture));
            fields.Add(entry.RecordedRuleDurationDays.ToString(CultureInfo.InvariantCulture));
            fields.Add(entry.DurationWasMinimum ? "1" : "0");
            fields.Add(EncodeText(entry.Narrative));
            fields.Add(entry.NarrativeIsManual ? "1" : "0");
        }
        return string.Join("|", fields);
    }

    public static bool TryDecode(string settlementId, string payload, out SettlementRuleMemoryRecord record)
    {
        record = null;
        if (string.IsNullOrWhiteSpace(settlementId) || string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        string[] fields = payload.Split('|');
        try
        {
            if (fields.Length > 0 && string.Equals(fields[0], CurrentVersionToken, StringComparison.Ordinal))
            {
                return TryDecodeCurrent(settlementId, fields, out record);
            }
            if (fields.Length > 0 && string.Equals(fields[0], LegacyVersionToken, StringComparison.Ordinal))
            {
                return TryDecodeLegacy(settlementId, fields, out record);
            }
        }
        catch (FormatException)
        {
        }
        catch (ArgumentException)
        {
        }

        record = null;
        return false;
    }

    private static bool TryDecodeCurrent(
        string settlementId,
        string[] fields,
        out SettlementRuleMemoryRecord record)
    {
        record = null;
        if (fields.Length < CurrentHeaderFieldCount
            || !TryParseNonNegative(fields[2], out int cultureStartDay)
            || !int.TryParse(fields[3], NumberStyles.None, CultureInfo.InvariantCulture, out int entryCount)
            || entryCount < 1
            || entryCount > SettlementRuleMemoryStore.MaximumRulerMemories
            || fields.Length != CurrentHeaderFieldCount + (entryCount * CurrentEntryFieldCount))
        {
            return false;
        }

        var entries = new List<SettlementRuleMemoryEntry>(entryCount);
        int offset = CurrentHeaderFieldCount;
        for (int index = 0; index < entryCount; index++, offset += CurrentEntryFieldCount)
        {
            if (!TryParseNonNegative(fields[offset + 5], out int ruleStartDay)
                || !TryParseNonNegative(fields[offset + 6], out int minimumDurationDays)
                || !TryParseNonNegative(fields[offset + 7], out int recordedDurationDays)
                || !TryParseBoolean(fields[offset + 8], out bool durationWasMinimum)
                || !TryParseBoolean(fields[offset + 10], out bool narrativeIsManual))
            {
                return false;
            }
            entries.Add(new SettlementRuleMemoryEntry(
                DecodeText(fields[offset]),
                DecodeText(fields[offset + 1]),
                DecodeText(fields[offset + 2]),
                DecodeText(fields[offset + 3]),
                DecodeText(fields[offset + 4]),
                ruleStartDay,
                minimumDurationDays,
                recordedDurationDays,
                durationWasMinimum,
                DecodeText(fields[offset + 9]),
                narrativeIsManual));
        }

        record = new SettlementRuleMemoryRecord(
            SettlementRuleMemoryStore.CurrentSchemaVersion,
            settlementId.Trim(),
            DecodeText(fields[1]),
            cultureStartDay,
            entries);
        return record.CurrentRule != null;
    }

    private static bool TryDecodeLegacy(
        string settlementId,
        string[] fields,
        out SettlementRuleMemoryRecord record)
    {
        record = null;
        if (fields.Length != LegacyFieldCount
            || !TryParseNonNegative(fields[7], out int ruleStartDay)
            || !TryParseNonNegative(fields[8], out int cultureStartDay)
            || !TryParseNonNegative(fields[9], out int minimumDurationDays)
            || !TryParseNonNegative(fields[15], out int previousDurationDays)
            || !TryParseBoolean(fields[16], out bool previousDurationWasMinimum))
        {
            return false;
        }

        record = new SettlementRuleMemoryRecord(
            SettlementRuleMemoryStore.CurrentSchemaVersion,
            settlementId.Trim(),
            DecodeText(fields[1]),
            DecodeText(fields[2]),
            DecodeText(fields[3]),
            DecodeText(fields[4]),
            DecodeText(fields[5]),
            DecodeText(fields[6]),
            ruleStartDay,
            cultureStartDay,
            minimumDurationDays,
            DecodeText(fields[10]),
            DecodeText(fields[11]),
            DecodeText(fields[12]),
            DecodeText(fields[13]),
            DecodeText(fields[14]),
            previousDurationDays,
            previousDurationWasMinimum);
        return record.CurrentRule != null;
    }

    private static string EncodeText(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
    }

    private static string DecodeText(string value)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(value ?? string.Empty));
    }

    private static bool TryParseNonNegative(string value, out int parsed)
    {
        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out parsed) && parsed >= 0;
    }

    private static bool TryParseBoolean(string value, out bool parsed)
    {
        parsed = string.Equals(value, "1", StringComparison.Ordinal);
        return parsed || string.Equals(value, "0", StringComparison.Ordinal);
    }
}
