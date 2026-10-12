using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class PromptRetrievalTexts
{
    private static readonly Lazy<Dictionary<string, string>> Compatibility = new Lazy<Dictionary<string, string>>(() =>
    {
        using (Stream stream = typeof(PromptRetrievalTexts).Assembly.GetManifestResourceStream("AnimusForge.Prompt.RetrievalCompatibility.json"))
        using (StreamReader reader = new StreamReader(stream, new UTF8Encoding(false, true)))
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());
    }, true);
    private static readonly Lazy<Dictionary<string, string>> Values = new Lazy<Dictionary<string, string>>(() =>
        JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            AnimusForgeModulePaths.GetModuleDataFilePath("PromptRetrievalTexts.json"), new UTF8Encoding(false, true))), true);

    internal static string OptionalHeader(string rawTitle)
    {
        if (string.Equals(rawTitle, Raw("Raw_a7699de9c855"), StringComparison.Ordinal)) return Prompt("NpcItemsHeader");
        if (string.Equals(rawTitle, Raw("Raw_e22aa87ba2c7"), StringComparison.Ordinal)) return Prompt("PlayerEquipmentHeader");
        if (string.Equals(rawTitle, Raw("Raw_667cf12fa5e8"), StringComparison.Ordinal)) return Prompt("DebtHeader");
        if (string.Equals(rawTitle, Raw("Raw_674366dba38e"), StringComparison.Ordinal)) return Prompt("PlayerMarriageHeader");
        if (string.Equals(rawTitle, Raw("Raw_47744bca4c99"), StringComparison.Ordinal)) return Prompt("TargetMarriageHeader");
        if (string.Equals(rawTitle, Raw("Raw_edf50fd9fd51"), StringComparison.Ordinal)) return Prompt("RuntimeFactsHeader");
        return rawTitle;
    }

    internal static string Raw(string id) => Compatibility.Value[id];
    internal static string Prompt(string id) => Values.Value[id];
}
