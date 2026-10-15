using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;

// Actual prefab and localization runtime; no Campaign, native UI or network.
public static class InteriorLocalizationChecks
{
    private static int checks;
    private static void Check(bool ok, string name)
    {
        if (!ok) throw new Exception("FAIL " + name);
        checks++;
    }
    private static Type Find(Assembly dll, string name)
    {
        return dll.GetType("AnimusForge." + name) ?? dll.GetType("AnimusForge.VoteDealBehavior+" + name, true);
    }
    private static string Owner(XmlNode node)
    {
        while (node != null)
        {
            var source = node.Attributes == null ? null : node.Attributes["DataSource"];
            if (source != null)
            {
                switch (source.Value)
                {
                    case "{CivilWarFactions}": return "KingdomFactionPanelVM";
                    case "{Agenda}": return "KingdomAgendaVM";
                    case "{SelectedItem}": return "KingdomAgendaItemVM";
                    case "{Options}": return "AgendaOptionVM";
                    default: throw new Exception("Unverified label scope: " + source.Value);
                }
            }
            node = node.ParentNode;
        }
        return "KingdomAgendaVMMixin";
    }
    public static void Run(string dllPath, string moduleRoot)
    {
        checks = 0;
        var dll = Assembly.LoadFrom(Path.GetFullPath(dllPath));
        var patchType = Find(dll, "KingdomInteriorPanelPatch");
        // Build once before registering any languages: the original freeze trigger.
        var patch = Activator.CreateInstance(patchType, true);
        var layout = (XmlDocument)patchType.GetMethod("GetPrefabExtension").Invoke(patch, null);
        var localization = Assembly.Load("TaleWorlds.Localization");
        localization.GetType("TaleWorlds.Localization.LocalizedTextManager", true)
            .GetMethod("LoadLocalizationXmls").Invoke(null, new object[] { new[] { moduleRoot } });
        var language = localization.GetType("TaleWorlds.Localization.MBTextManager", true).GetMethod("ChangeLanguage");
        var objects = new Dictionary<Type, object>();
        var unique = new HashSet<string>();
        int occurrences = 0;
        foreach (string selected in new[] { "简体中文", "English", "简体中文" })
        {
            Check((bool)language.Invoke(null, new object[] { selected }), "fixture language registered: " + selected);
            var strings = new XmlDocument();
            strings.Load(Path.Combine(moduleRoot, "ModuleData/Languages/" + (selected == "English" ? "afkingdom_ui_strings.xml" : "CNs/afkingdom_ui_strings-zh-CN.xml")));
            occurrences = 0;
            foreach (XmlElement label in layout.SelectNodes("//*[@Text]"))
            {
                string binding = label.GetAttribute("Text");
                if (!binding.StartsWith("@Interior", StringComparison.Ordinal)) continue;
                occurrences++;
                string propertyName = binding.Substring(1);
                unique.Add(propertyName);
                var type = Find(dll, Owner(label));
                var property = type.GetProperty(propertyName);
                Check(property != null, "binding exists on actual scope: " + type.Name + "." + propertyName);
                Check(Array.Exists(property.GetCustomAttributes(false), a => a.GetType().Name == "DataSourceProperty" || a.GetType().Name == "DataSourcePropertyAttribute"), "binding exported: " + propertyName);
                object instance;
                if (!objects.TryGetValue(type, out instance)) objects[type] = instance = FormatterServices.GetUninitializedObject(type);
                string key = "AF_KINGDOM_UI_Interior_" + propertyName.Substring(8, propertyName.Length - 12);
                var expected = strings.SelectSingleNode("//string[@id='" + key + "']/@text");
                string actual = (string)property.GetValue(instance, null);
                Check(expected != null && actual == expected.Value, selected + ": " + propertyName + " expected=" + (expected == null ? "<missing>" : expected.Value) + " actual=" + actual);
            }
            Check(unique.Count == 21 && occurrences >= 21, "all 21 fixed labels bound across repeated panel branches");
        }
        var after = (XmlDocument)patchType.GetMethod("GetPrefabExtension").Invoke(patch, null);
        Check(object.ReferenceEquals(layout, after), "same prefab survives language changes; no layout regeneration");
        Console.WriteLine("PASS " + checks + " actual-DLL checks; early-built prefab, 5 real scopes, CN/EN/CN, no native UI or HTTP.");
    }
}
