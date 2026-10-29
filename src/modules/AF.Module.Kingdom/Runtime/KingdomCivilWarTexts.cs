using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class KingdomCivilWarTexts
{
    private static readonly Lazy<Dictionary<string, string>> Prompts = new Lazy<Dictionary<string, string>>(
        () => Load("KingdomCivilWarPromptTexts.json"));
    private static readonly Lazy<Dictionary<string, string>> Compatibility = new Lazy<Dictionary<string, string>>(
        () => Load("KingdomCivilWarCompatibility.json"));

    private static Dictionary<string, string> Load(string file)
        => JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            AnimusForgeModulePaths.GetModuleDataFilePath(file), Encoding.UTF8));

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
    internal static string Raw(string id) => Compatibility.Value[id];
}
