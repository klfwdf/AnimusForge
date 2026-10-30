using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class KingdomCivilWarTexts
{
    private static readonly PromptLanguageFile<Dictionary<string, string>> Prompts = new PromptLanguageFile<Dictionary<string, string>>(
        "KingdomCivilWarPromptTexts.json", path => JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path, Encoding.UTF8)));
    private static readonly BilingualTextTable Compatibility =
        BilingualTextTable.FromModuleData("KingdomCivilWarCompatibility.json");

    internal static string Date(int day)
    {
        int seasonDays = TaleWorlds.CampaignSystem.CampaignTime.DaysInSeason;
        int yearDays = TaleWorlds.CampaignSystem.CampaignTime.DaysInYear;
        day = Math.Max(0, day);
        string season = Prompt("KW_CalendarSeason_" + ((day % yearDays / seasonDays) % 4));
        return Prompt("KW_CalendarDate").Replace("{YEAR}", (day / yearDays).ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Replace("{SEASON}", season).Replace("{DAY}", (day % seasonDays + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    internal static string Prompt(string id) => Prompts.Value[id];
    internal static string Raw(string key) => Compatibility.Get(key);
    /// <summary>Chinese source value, for matching text written before the language switch.</summary>
    internal static string RawSource(string key) => Compatibility.Chinese(key);
    internal static string[] RawVariants(string key) => Compatibility.Variants(key);
    internal static bool IsRaw(string value, string key) => Compatibility.Is(value, key);
}
