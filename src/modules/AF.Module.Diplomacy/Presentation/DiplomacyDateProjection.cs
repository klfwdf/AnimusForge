using System;
using System.Text.RegularExpressions;

namespace AnimusForge;

// Known persisted calendar syntax only; names, draft prose and unknown dates are opaque.
internal static class DiplomacyDateProjection
{
    private static readonly (string Name, string UiKey)[] Seasons =
    {
        ("Spring", "AF_DIP_UI_Vassalage_SeasonSpring"),
        ("Summer", "AF_DIP_UI_Vassalage_SeasonSummer"),
        ("Autumn", "AF_DIP_UI_Vassalage_SeasonAutumn"),
        ("Winter", "AF_DIP_UI_Vassalage_SeasonWinter")
    };
    private static readonly Lazy<Regex> Calendar = new Lazy<Regex>(() => new Regex(
        @"^(?<year>\d+)" + Regex.Escape(DiplomacyCompatibilityTexts.Get("KingdomProfile_YearSuffix"))
        + "(?<season>" + string.Join("|", Array.ConvertAll(Seasons, s => Regex.Escape(DiplomacyCompatibilityTexts.Get("KingdomProfile_Season" + s.Name)))) + ")"
        + Regex.Escape(DiplomacyCompatibilityTexts.Get("KingdomProfile_SeasonSuffix")) + @"(?<day>\d+)"
        + Regex.Escape(DiplomacyCompatibilityTexts.Get("KingdomProfile_DaySuffix")) + "$", RegexOptions.Compiled | RegexOptions.CultureInvariant));

    internal static string ForUi(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw;
        Match match = Calendar.Value.Match(raw.Trim());
        if (!match.Success) return raw;
        foreach (var season in Seasons)
            if (match.Groups["season"].Value == DiplomacyCompatibilityTexts.Get("KingdomProfile_Season" + season.Name))
                return DiplomacyUiLocalization.Format("AF_DIP_UI_Vassalage_CampaignDate", "Year {YEAR}, {SEASON} {DAY}",
                    ("YEAR", match.Groups["year"].Value), ("DAY", match.Groups["day"].Value),
                    ("SEASON", DiplomacyUiLocalization.Text(season.UiKey, season.Name.ToLowerInvariant())));
        return raw;
    }

    internal static string ForPrompt(string raw, Func<string, string, string> localize)
    {
        if (localize == null || string.IsNullOrWhiteSpace(raw)) return raw;
        Match match = Calendar.Value.Match(raw.Trim());
        if (!match.Success) return raw;
        foreach (var season in Seasons)
            if (match.Groups["season"].Value == DiplomacyCompatibilityTexts.Get("KingdomProfile_Season" + season.Name))
                return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    localize("PromptCampaignDate", "Year {0}, {1} {2}"), match.Groups["year"].Value,
                    localize("KingdomProfile_Season" + season.Name, season.Name.ToLowerInvariant()), match.Groups["day"].Value);
        return raw;
    }
}
