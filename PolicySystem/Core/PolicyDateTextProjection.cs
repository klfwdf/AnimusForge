using System;
using System.Text.RegularExpressions;

namespace AnimusForge;

/// <summary>Projects canonical saved dates only at UI and prompt boundaries.</summary>
internal static class PolicyDateTextProjection
{
    private static readonly Lazy<string[]> Seasons = new Lazy<string[]>(() => new[]
    {
        PolicyCompatibilityTexts.Get("Closeout_CustomPolicyBehavior_Legacy_Closeout_4"),
        PolicyCompatibilityTexts.Get("Closeout_CustomPolicyBehavior_Legacy_Closeout_5"),
        PolicyCompatibilityTexts.Get("Closeout_CustomPolicyBehavior_Legacy_Closeout_6"),
        PolicyCompatibilityTexts.Get("Closeout_CustomPolicyBehavior_Legacy_Closeout_7")
    });
    private static readonly Lazy<Regex> SavedDate = new Lazy<Regex>(() => new Regex(
        "\\A(?<YEAR>[0-9]+)" + Regex.Escape(PolicyCompatibilityTexts.Get("Closeout_CustomPolicyBehavior_Legacy_Closeout_0"))
        + "(?<SEASON>" + string.Join("|", Array.ConvertAll(Seasons.Value, Regex.Escape)) + ")"
        + Regex.Escape(PolicyCompatibilityTexts.Get("Closeout_CustomPolicyBehavior_Legacy_Closeout_1"))
        + "(?<DAY>[0-9]+)" + Regex.Escape(PolicyCompatibilityTexts.Get("Closeout_CustomPolicyBehavior_Legacy_Closeout_2")) + "\\z",
        RegexOptions.CultureInvariant));
    private static readonly string[] SeasonIds = { "Spring", "Summer", "Autumn", "Winter" };

    internal static string ForUi(string value) => Project(value, true);
    internal static string ForPrompt(string value) => Project(value, false);

    private static string Project(string value, bool ui)
    {
        if (string.IsNullOrEmpty(value)) return value;
        if (string.Equals(value, PolicyCompatibilityTexts.Get("Closeout_CustomPolicyBehavior_Legacy_Closeout_3"), StringComparison.Ordinal))
            return ui ? PolicyUiTexts.Get("AFPOL_UI_UnknownDate", "Unknown Date")
                      : PolicyGenerationPromptTexts.Get("Closeout_DateUnknown", "Unknown date");
        Match match = SavedDate.Value.Match(value);
        if (!match.Success) return value;
        int seasonIndex = Array.IndexOf(Seasons.Value, match.Groups["SEASON"].Value);
        if (seasonIndex < 0) return value;
        string season = ui ? PolicyUiTexts.Get("AFPOL_UI_Season" + SeasonIds[seasonIndex], SeasonIds[seasonIndex])
                           : PolicyGenerationPromptTexts.Get("Closeout_Season" + SeasonIds[seasonIndex], SeasonIds[seasonIndex]);
        return ui ? PolicyUiTexts.Format("AFPOL_UI_CampaignDate", "Year {YEAR}, {SEASON}, day {DAY}",
                        "YEAR", match.Groups["YEAR"].Value, "SEASON", season, "DAY", match.Groups["DAY"].Value)
                  : PolicyGenerationPromptTexts.Format("Closeout_CampaignDate", "Year {YEAR}, {SEASON}, day {DAY}",
                        "YEAR", match.Groups["YEAR"].Value, "SEASON", season, "DAY", match.Groups["DAY"].Value);
    }
}
