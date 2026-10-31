using System;
using System.Text.RegularExpressions;

namespace AnimusForge;

// Known persisted calendar syntax only; names, draft prose and unknown dates are opaque.
// The calendar vocabulary is language-invariant persisted syntax, so it is matched against the Chinese source.
internal static class CampaignDateProjection
{
    private static readonly (string Name, string UiKey)[] Seasons =
    {
        ("Spring", "AF_DIP_UI_Vassalage_SeasonSpring"),
        ("Summer", "AF_DIP_UI_Vassalage_SeasonSummer"),
        ("Autumn", "AF_DIP_UI_Vassalage_SeasonAutumn"),
        ("Winter", "AF_DIP_UI_Vassalage_SeasonWinter")
    };
    private static readonly Lazy<Regex> Calendar = new Lazy<Regex>(() => new Regex(
        @"^(?<year>\d+)" + Regex.Escape(DiplomacyCompatibilityTexts.Source("KingdomProfile_YearSuffix"))
        + "(?<season>" + string.Join("|", Array.ConvertAll(Seasons, s => Regex.Escape(DiplomacyCompatibilityTexts.Source("KingdomProfile_Season" + s.Name)))) + ")"
        + Regex.Escape(DiplomacyCompatibilityTexts.Source("KingdomProfile_SeasonSuffix")) + @"(?<day>\d+)"
        + Regex.Escape(DiplomacyCompatibilityTexts.Source("KingdomProfile_DaySuffix")) + "$", RegexOptions.Compiled | RegexOptions.CultureInvariant));

    internal static string ForUi(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw;
        Match match = Calendar.Value.Match(raw.Trim());
        if (!match.Success) return raw;
        foreach (var season in Seasons)
            if (match.Groups["season"].Value == DiplomacyCompatibilityTexts.Source("KingdomProfile_Season" + season.Name))
                return DiplomacyUiLocalization.Format("AF_DIP_UI_Vassalage_CampaignDate", "Year {YEAR}, {SEASON} {DAY}",
                    ("YEAR", match.Groups["year"].Value), ("DAY", match.Groups["day"].Value),
                    ("SEASON", DiplomacyUiLocalization.Text(season.UiKey, season.Name.ToLowerInvariant())));
        return raw;
    }

}
