using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class AdapterSceneTexts
{
    private static readonly PromptLanguageFile<Dictionary<string, string>> Values = new PromptLanguageFile<Dictionary<string, string>>("AdapterSceneTexts.json", path =>
        JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            path, Encoding.UTF8)));
    private static readonly BilingualTextTable Compatibility =
        BilingualTextTable.FromEmbedded(typeof(AdapterSceneTexts).Assembly, "AnimusForge.Adapter.SceneCompatibility.json");
    private static readonly Regex Placeholder = new Regex(@"\{([A-Z_]+)\}", RegexOptions.Compiled);
    private static readonly (string Key, string English)[] SceneLabels =
    {
        ("Scene_Guide_Armorer", "armorer"), ("Scene_Guide_Weaponsmith", "weaponsmith"),
        ("Scene_Guide_Blacksmith", "blacksmith"), ("Scene_Guide_HorseTrader", "horse trader"),
        ("Scene_Guide_Merchant", "merchant"), ("Scene_Guide_WorkshopWorker", "workshop worker"),
        ("Scene_Guide_Barber", "barber"), ("Scene_Guide_ArenaHost", "arena master"),
        ("Scene_Guide_Tavernkeeper", "tavernkeeper"), ("Scene_Guide_RansomBroker", "ransom broker"),
        ("Scene_Guide_Gambler", "board-game host"), ("Scene_Guide_TavernWench", "tavern wench"),
        ("Scene_Guide_Musician", "musician"), ("Scene_Guide_Mercenary", "mercenary"),
        ("Scene_Location_TownCenter", "town streets"), ("Scene_Location_VillageCenter", "village center"),
        ("Scene_Location_Tavern", "tavern"), ("Scene_Location_LordsHall", "lord's hall"),
        ("Scene_Location_Prison", "dungeon"), ("Scene_Location_Arena", "arena"),
        ("Scene_Location_Alley", "alley"), ("Scene_Location_Port", "port")
    };
    private static readonly Lazy<Dictionary<string, string>> LabelKeys = new Lazy<Dictionary<string, string>>(() =>
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var label in SceneLabels)
            foreach (string variant in RawVariants(label.Key)) result[variant] = label.Key;
        return result;
    });
    private static readonly Lazy<Dictionary<string, string>> LabelAliases = new Lazy<Dictionary<string, string>>(() =>
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var label in SceneLabels) result[label.English] = RawSource(label.Key);
        return result;
    });

    internal static string Get(string key, string englishFallback = "")
        => Values.Value.TryGetValue(key, out string text) ? text : englishFallback;
    internal static string Raw(string key) => Compatibility.Get(key);
    /// <summary>Chinese source value, for matching text written before the language switch.</summary>
    internal static string RawSource(string key) => Compatibility.Chinese(key);
    internal static string[] RawVariants(string key) => Compatibility.Variants(key);
    internal static bool IsRaw(string value, string key) => Compatibility.Is(value, key);
    internal static string ProjectLabelForPrompt(string value)
        => value != null && LabelKeys.Value.TryGetValue(value.Trim(), out string key) ? Get(key) : value;
    internal static string ResolveRawLabel(string value)
        => value != null && LabelAliases.Value.TryGetValue(value.Trim(), out string raw) ? raw : value;
    internal static string Format(string key, string englishFallback, params (string Name, object Value)[] variables)
        => Render(Get(key, englishFallback), variables);
    internal static string RawFormat(string key, params (string Name, object Value)[] variables)
        => Render(Raw(key), variables);
    private static string Render(string template, (string Name, object Value)[] variables)
        => Placeholder.Replace(template, match =>
        {
            foreach (var variable in variables)
                if (variable.Name == match.Groups[1].Value)
                    return Convert.ToString(variable.Value, CultureInfo.CurrentCulture) ?? "";
            return match.Value;
        });
}
