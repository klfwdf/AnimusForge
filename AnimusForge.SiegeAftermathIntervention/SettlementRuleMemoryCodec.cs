using System;
using System.Globalization;
using System.Text;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Versioned primitive-string codec used by the AF save adapter.
/// </summary>
public static class SettlementRuleMemoryCodec
{
    private const string VersionToken = "v1";
    private const int FieldCount = 17;

    public static string Encode(SettlementRuleMemoryRecord record)
    {
        if (record == null || string.IsNullOrWhiteSpace(record.SettlementId))
        {
            return string.Empty;
        }

        return string.Join("|", new[]
        {
            VersionToken,
            EncodeText(record.SettlementName),
            EncodeText(record.RulerId),
            EncodeText(record.RulerName),
            EncodeText(record.CultureId),
            EncodeText(record.CultureName),
            EncodeText(record.RulerPersonality),
            Math.Max(0, record.RuleStartDay).ToString(CultureInfo.InvariantCulture),
            Math.Max(0, record.CultureStartDay).ToString(CultureInfo.InvariantCulture),
            Math.Max(0, record.MinimumRuleDurationDays).ToString(CultureInfo.InvariantCulture),
            EncodeText(record.PreviousRulerId),
            EncodeText(record.PreviousRulerName),
            EncodeText(record.PreviousCultureId),
            EncodeText(record.PreviousCultureName),
            EncodeText(record.PreviousRulerPersonality),
            Math.Max(0, record.PreviousRuleDurationDays).ToString(CultureInfo.InvariantCulture),
            record.PreviousDurationWasMinimum ? "1" : "0",
        });
    }

    public static bool TryDecode(string settlementId, string payload, out SettlementRuleMemoryRecord record)
    {
        record = null;
        if (string.IsNullOrWhiteSpace(settlementId) || string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        string[] fields = payload.Split('|');
        if (fields.Length != FieldCount
            || !string.Equals(fields[0], VersionToken, StringComparison.Ordinal)
            || !TryParseNonNegative(fields[7], out int ruleStartDay)
            || !TryParseNonNegative(fields[8], out int cultureStartDay)
            || !TryParseNonNegative(fields[9], out int minimumDurationDays)
            || !TryParseNonNegative(fields[15], out int previousDurationDays)
            || (fields[16] != "0" && fields[16] != "1"))
        {
            return false;
        }

        try
        {
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
                fields[16] == "1");
            return true;
        }
        catch (FormatException)
        {
            record = null;
            return false;
        }
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
}
