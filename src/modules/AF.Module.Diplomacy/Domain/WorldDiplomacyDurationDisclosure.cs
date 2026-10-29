using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AnimusForge.Refactor.Domain;

// Pure bounded text validation at document admission, never a Tick operation.
// Recognition vocabulary is shared by CN/EN packages; it is not output language policy.
internal static class WorldDiplomacyDurationDisclosure
{
    private const RegexOptions Options = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;
    private static readonly Regex Durations = new(DiplomacyCompatibilityTexts.Get("Duration_Expression"), Options);
    private static readonly Regex Negative = new(DiplomacyCompatibilityTexts.Get("Duration_NegativePrefix"), Options);
    private static readonly Regex NonTermSuffix = new(DiplomacyCompatibilityTexts.Get("Duration_NonTermSuffix"), Options);
    private static readonly string Separators = DiplomacyCompatibilityTexts.Get("Duration_ClauseSeparator");
    private static readonly Regex EnglishSpacing = new(@"[\s-]+", Options);
    private static readonly string Digits = DiplomacyCompatibilityTexts.Get("Duration_ChineseDigits");
    private static readonly string[] Units = DiplomacyCompatibilityTexts.Get("Duration_ChineseUnits").Split('|');
    private static readonly string[] Ones = DiplomacyCompatibilityTexts.Get("Duration_EnglishOnes").Split('|');
    private static readonly string[] Tens = DiplomacyCompatibilityTexts.Get("Duration_EnglishTens").Split('|');

    internal static bool ContainsDays(string text, int days)
    {
        if (days <= 0 || string.IsNullOrWhiteSpace(text)) return false;
        // The diplomatic terms owner permits 1..252 days. Keep Arabic matching
        // exact for older callers, and recognize written cardinals up to 999.
        string chinese = days <= 999 ? Chinese(days) : "";
        string english = days <= 999 ? English(days) : "";
        int clauseStart = 0, cursor = 0;
        foreach (Match match in Durations.Matches(text))
        {
            while (cursor < match.Index)
            {
                if (Separators.IndexOf(text[cursor]) >= 0) clauseStart = cursor + 1;
                cursor++;
            }
            string token = match.Groups["number"].Value;
            bool same = int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out int numeric)
                ? numeric == days
                : token == chinese || EnglishSpacing.Replace(token.ToLowerInvariant(), " ").Replace(" and ", " ") == english;
            if (!same) continue;
            int start = match.Index;
            // Inspect only the containing clause, so an unrelated no-tribute
            // sentence does not negate a positive treaty duration.
            string prefix = text.Substring(clauseStart, start - clauseStart);
            string suffix = text.Substring(start + match.Length);
            if (!Negative.IsMatch(prefix) && !NonTermSuffix.IsMatch(suffix)) return true;
        }
        return false;
    }

    private static string Chinese(int value)
    {
        if (value < 10) return Digits[value].ToString();
        if (value < 100) return (value < 20 ? "" : Digits[value / 10].ToString()) + Units[0]
            + (value % 10 == 0 ? "" : Digits[value % 10].ToString());
        int rest = value % 100;
        return Digits[value / 100] + Units[1] + (rest == 0 ? ""
            : rest < 10 ? Digits[0] + Chinese(rest)
            : rest < 20 ? Digits[1] + Chinese(rest) : Chinese(rest));
    }

    private static string English(int value)
    {
        if (value < 20) return Ones[value];
        if (value < 100) return Tens[value / 10] + (value % 10 == 0 ? "" : " " + Ones[value % 10]);
        return Ones[value / 100] + " hundred" + (value % 100 == 0 ? "" : " " + English(value % 100));
    }
}
