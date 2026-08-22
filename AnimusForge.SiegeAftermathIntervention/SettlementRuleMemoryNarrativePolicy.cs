using System;
using System.Text;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Validates generated town-memory prose without inferring player intent.
/// </summary>
public static class SettlementRuleMemoryNarrativePolicy
{
    public const int MinimumGeneratedLength = 40;
    public const int MaximumStoredLength = 180;

    public static bool TryParseGeneratedResponse(string response, out string narrative)
    {
        narrative = string.Empty;
        string value = (response ?? string.Empty).Trim();
        const string property = "\"memory\"";
        int propertyIndex = value.IndexOf(property, StringComparison.Ordinal);
        if (propertyIndex < 0
            || !value.StartsWith("{", StringComparison.Ordinal)
            || !value.EndsWith("}", StringComparison.Ordinal)
            || !string.IsNullOrWhiteSpace(value.Substring(1, propertyIndex - 1))
            || value.IndexOf('<') >= 0
            || value.IndexOf('>') >= 0)
        {
            return false;
        }
        int colonIndex = value.IndexOf(':', propertyIndex + property.Length);
        int quoteIndex = colonIndex < 0 ? -1 : value.IndexOf('"', colonIndex + 1);
        if (quoteIndex < 0)
        {
            return false;
        }

        var decoded = new StringBuilder();
        bool escaped = false;
        bool closed = false;
        int closingQuoteIndex = -1;
        for (int i = quoteIndex + 1; i < value.Length; i++)
        {
            char character = value[i];
            if (escaped)
            {
                decoded.Append(character == 'n' || character == 'r' || character == 't' ? ' ' : character);
                escaped = false;
                continue;
            }
            if (character == '\\')
            {
                escaped = true;
                continue;
            }
            if (character == '"')
            {
                closed = true;
                closingQuoteIndex = i;
                break;
            }
            decoded.Append(character);
        }
        if (!closed
            || !string.Equals(value.Substring(closingQuoteIndex + 1).Trim(), "}", StringComparison.Ordinal))
        {
            return false;
        }

        narrative = NormalizeForStorage(decoded.ToString());
        if (narrative.Length < MinimumGeneratedLength)
        {
            narrative = string.Empty;
            return false;
        }
        return true;
    }

    public static string NormalizeForStorage(string value)
    {
        string normalized = CollapseWhitespace(value);
        if (normalized.Length <= MaximumStoredLength)
        {
            return normalized;
        }

        int sentenceEnd = normalized.LastIndexOfAny(
            new[] { '\u3002', '\uFF01', '\uFF1F', '.', '!', '?' },
            MaximumStoredLength - 1);
        int length = sentenceEnd >= MinimumGeneratedLength ? sentenceEnd + 1 : MaximumStoredLength;
        return normalized.Substring(0, length).Trim();
    }

    private static string CollapseWhitespace(string value)
    {
        var result = new StringBuilder();
        bool previousWasWhitespace = false;
        foreach (char character in value ?? string.Empty)
        {
            bool isWhitespace = char.IsWhiteSpace(character);
            if (isWhitespace)
            {
                if (!previousWasWhitespace && result.Length > 0)
                {
                    result.Append(' ');
                }
            }
            else
            {
                result.Append(character);
            }
            previousWasWhitespace = isWhitespace;
        }
        return result.ToString().Trim();
    }
}
