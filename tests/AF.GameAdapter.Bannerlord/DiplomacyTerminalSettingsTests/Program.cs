using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using AnimusForge;
using BUTR.DependencyInjection;
using BUTR.DependencyInjection.Logger;
using MCM.Abstractions.Properties;
using MCM.Implementation;
using Newtonsoft.Json.Linq;
using TaleWorlds.Localization;

internal static class Program
{
    private static readonly ISettingsPropertyDiscoverer Discoverer = (ISettingsPropertyDiscoverer)
        Activator.CreateInstance(typeof(JsonSettingsFormat).Assembly.GetType("MCM.Implementation.AttributeSettingsPropertyDiscoverer", true), true);
    private sealed class Services : IGenericServiceProvider, IGenericServiceProviderScope
    {
        public TService GetService<TService>() where TService : class
        {
            if (typeof(TService) == typeof(IEnumerable<ISettingsPropertyDiscoverer>))
                return (TService)(object)new ISettingsPropertyDiscoverer[] { Discoverer };
            return default;
        }
        public IGenericServiceProviderScope CreateScope() => this;
        public void Dispose() { }
    }
    private static int checks;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new InvalidOperationException(message);
    }

    private static int Main(string[] args)
    {
        Check(new TextObject("Settings fixture").ToString() == "Settings fixture",
              "MCM group labels have the real managed localization fallback");
        // Supply the actual discoverer through MCM's normal service lookup.
        // No game settings provider or disk storage is registered.
        typeof(GenericServiceProvider).GetField("GlobalServiceProvider", BindingFlags.Static | BindingFlags.NonPublic)
            .SetValue(null, new Services());
        // The production properties/defaults are linked without the game host.
        // Loading and saving use MCM's real format and settings converter.
        var format = new JsonSettingsFormat(new DefaultBUTRLogger<JsonSettingsFormat>());
        var defaults = new DuelSettings();
        Check(Discoverer.GetProperties(defaults).Count() == 7,
              "The real MCM discoverer recognizes all seven active fixture settings");
        Check(JObject.Parse(format.SaveJson(defaults)).Properties().Count() == 7,
              "The actual MCM format discovers the active properties through its service lookup");
        Check(defaults.WorldDiplomacyDeclarationMinCharacters == 100
              && defaults.WorldDiplomacyDeclarationMaxCharacters == 300, "New defaults are 100 to 300");

        string[] retired = {
            "EnableWorldDiplomacyForcedWar", "WorldDiplomacyWarPressureThreshold",
            "WorldDiplomacyPeacePressureThreshold", "WorldDiplomacyNativeIntentInfluencePercent",
            "WorldDiplomacyDocumentInfluencePercent", "WorldDiplomacyCompressionThresholdTenThousands"
        };
        foreach (string name in retired)
            Check(typeof(DuelSettings).GetProperty(name) == null, "Retired property is absent: " + name);

        var old = new JObject {
            ["WorldDiplomacyDeclarationMinCharacters"] = 230,
            ["WorldDiplomacyDeclarationMaxCharacters"] = 875,
            ["ModelName"] = "fixture-model",
            ["MainApiTemperature"] = 0.45,
            ["EnableWorldDiplomacy"] = false,
            ["WorldDiplomacyPrompt"] = "Keep this user preference.",
            [retired[0]] = false,
            [retired[1]] = 75,
            [retired[2]] = 90,
            [retired[3]] = 40,
            [retired[4]] = 60,
            [retired[5]] = 12
        };
        var loaded = (DuelSettings)format.LoadFromJson(new DuelSettings(), old.ToString());
        Check(loaded.WorldDiplomacyPlayerContactIntervalDays == 5, "Missing contact interval receives default without changing existing fields");
        loaded.WorldDiplomacyPlayerContactIntervalDays = 17;
        Check(loaded != null, "MCM accepts old JSON containing all six removed fields");
        Check(loaded.WorldDiplomacyDeclarationMinCharacters == 230
              && loaded.WorldDiplomacyDeclarationMaxCharacters == 875,
              "Saved length values survive the new default");
        Check(loaded.ModelName == "fixture-model" && Math.Abs(loaded.MainApiTemperature - 0.45f) < 0.001f
              && !loaded.EnableWorldDiplomacy && loaded.WorldDiplomacyPrompt == "Keep this user preference.",
              "Other valid settings and user preference survive old JSON loading");

        JObject saved = JObject.Parse(format.SaveJson(loaded));
        foreach (string name in retired)
            Check(saved[name] == null, "MCM stops writing the removed field: " + name);
        Check((int)saved["WorldDiplomacyDeclarationMaxCharacters"] == 875,
              "Saving does not replace the user's upper bound with the new default");
        var roundTrip = (DuelSettings)format.LoadFromJson(new DuelSettings(), saved.ToString());
        Check(roundTrip.WorldDiplomacyPlayerContactIntervalDays == 17, "Original MCM owner saves and restores contact interval");
        Check(roundTrip.WorldDiplomacyDeclarationMinCharacters == 230
              && roundTrip.WorldDiplomacyDeclarationMaxCharacters == 875
              && roundTrip.WorldDiplomacyPrompt == loaded.WorldDiplomacyPrompt,
              "Save and reload preserve active diplomatic settings");

        JObject partial = new JObject { [retired[0]] = false, [retired[1]] = 120 };
        var missing = (DuelSettings)format.LoadFromJson(new DuelSettings(), partial.ToString());
        Check(missing.WorldDiplomacyDeclarationMinCharacters == 100
              && missing.WorldDiplomacyDeclarationMaxCharacters == 300,
              "Only missing active values receive new defaults");
        VerifyUiLanguages(args[0], args[1]);
        Console.WriteLine("Diplomacy terminal settings: PASS (" + checks
            + " assertions). Real MCM JSON format/converters; source-linked length properties; game/provider/UI not started.");
        return 0;
    }

    private static void VerifyUiLanguages(string directory, string expectedPath)
    {
        var expected = JObject.Parse(File.ReadAllText(expectedPath));
        foreach (var pair in new[] { ("English", "af_ui_strings.xml", "en"),
                                    ("简体中文", "CNs/af_ui_strings-zh-CN.xml", "cn") })
        {
            string path = Path.Combine(directory, pair.Item2);
            var metadata = new XmlDocument();
            metadata.LoadXml("<LanguageData id=\"" + pair.Item1 + "\" name=\"" + pair.Item1
                + "\" supported_iso=\"" + (pair.Item3 == "en" ? "en" : "zh-CN")
                + "\" under_development=\"false\" text_processor=\"TaleWorlds.Localization.TextProcessor.LanguageProcessors.EnglishTextProcessor\">"
                + "<LanguageFile xml_path=\"" + Path.GetFileName(path) + "\" /></LanguageData>");
            typeof(TextObject).Assembly.GetType("TaleWorlds.Localization.LanguageData", true)
                .GetMethod("LoadFromXml", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { metadata, Path.GetDirectoryName(path) });
            Check(MBTextManager.ChangeLanguage(pair.Item1), "Actual UI language switches: " + pair.Item1);
            foreach (var property in ((JObject)expected[pair.Item3]).Properties())
            {
                string fallback = (string)expected["en"][property.Name];
                Check(new TextObject("{=" + property.Name + "}" + fallback).ToString() == (string)property.Value,
                      "Actual native UI text resolves: " + pair.Item1 + "/" + property.Name);
            }
        }
    }
}
