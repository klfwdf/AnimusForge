using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class PromptRetrievalTexts
{
    private static readonly BilingualTextTable Compatibility =
        BilingualTextTable.FromEmbedded(typeof(PromptRetrievalTexts).Assembly, "AnimusForge.Prompt.RetrievalCompatibility.json");
    private static readonly PromptLanguageFile<Dictionary<string, string>> Values = new PromptLanguageFile<Dictionary<string, string>>("PromptRetrievalTexts.json", path =>
        JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            path, new UTF8Encoding(false, true))));

    internal static string OptionalHeader(string rawTitle)
    {
        if (IsRaw(rawTitle, "Raw_a7699de9c855")) return Prompt("NpcItemsHeader");
        if (IsRaw(rawTitle, "Raw_e22aa87ba2c7")) return Prompt("PlayerEquipmentHeader");
        if (IsRaw(rawTitle, "Raw_667cf12fa5e8")) return Prompt("DebtHeader");
        if (IsRaw(rawTitle, "Raw_674366dba38e")) return Prompt("PlayerMarriageHeader");
        if (IsRaw(rawTitle, "Raw_47744bca4c99")) return Prompt("TargetMarriageHeader");
        if (IsRaw(rawTitle, "Raw_edf50fd9fd51")) return Prompt("RuntimeFactsHeader");
        return rawTitle;
    }

    internal static string Raw(string key) => Compatibility.Get(key);
    /// <summary>Chinese source value, for matching text written before the language switch.</summary>
    internal static string RawSource(string key) => Compatibility.Chinese(key);
    internal static string[] RawVariants(string key) => Compatibility.Variants(key);
    internal static bool IsRaw(string value, string key) => Compatibility.Is(value, key);
    internal static string Prompt(string id) => Values.Value[id];
}
