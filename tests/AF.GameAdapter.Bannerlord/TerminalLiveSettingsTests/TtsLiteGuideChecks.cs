using System;
using System.Linq;
using System.Reflection;
using AnimusForge;
using MCM.Abstractions;

static class TtsLiteGuideChecks
{
    static readonly string[] Chapters = { "Download", "Install", "Use", "Hardware", "Recordings", "Manifest", "Language", "Troubleshoot" };
    static readonly BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
    internal static void Run(DuelSettings settings, string selected, Action<bool, string> check)
    {
        var guide = typeof(DuelSettings).Assembly.GetType("AnimusForge.GptSoVitsLiteGuide", true);
        var policy = typeof(DuelSettings).Assembly.GetType("AnimusForge.TtsSettingsVisibility", true).GetMethod("IsVisible", Flags);
        check((int)guide.GetField("ChapterCount", Flags).GetRawConstantValue() == Chapters.Length, "L tutorial has eight chapter entries");
        bool enabled = settings.EnableTtsSpeech;
        int provider = settings.TtsProviderDropdown.SelectedIndex;
        var native = settings.GetAllSettingPropertyDefinitions().ToArray();
        for (int chapter = 0; chapter < Chapters.Length; chapter++)
        {
            string id = "TtsLiteGuide" + Chapters[chapter];
            var property = typeof(DuelSettings).GetProperty(id);
            var button = property.GetCustomAttributes(false).Single(a => a.GetType().Name == "SettingPropertyButtonAttribute");
            check((int)button.GetType().GetProperty("Order").GetValue(button) == -19 + chapter, "Tutorial chapter follows provider in native order: " + id);
            check(native.Count(p => p.Id == id) == 1, "Tutorial native metadata unique: " + id);
            var definition = TerminalSettingsRegistry.GetById(id);
            check(definition != null && definition.Invoker != null && definition.Setter == null, "Tutorial is a read-only terminal action: " + id);
            string title = (string)guide.GetMethod("Title", Flags).Invoke(null, new object[] { chapter });
            string body = (string)guide.GetMethod("Body", Flags).Invoke(null, new object[] { chapter });
            check(title == definition.DisplayName && !body.Contains("{=") && body.Length > 100 && body.Length < 1500, "Localized real tutorial title/body and bounded page " + chapter + ":" + selected);
            var page = guide.GetMethod("CreatePage", Flags).Invoke(null, new object[] { chapter });
            var textProperty = page.GetType().GetProperty("Text");
            var textField = page.GetType().GetField("Text");
            check((string)(textProperty != null ? textProperty.GetValue(page) : textField.GetValue(page)) == body, "Native inquiry renders tutorial body " + chapter);
            var saved = property.GetValue(settings);
            bool invoked = false;
            try { property.SetValue(settings, (Action)(() => invoked = true)); definition.Invoker(settings); }
            finally { property.SetValue(settings, saved); }
            check(invoked, "Terminal tutorial invokes original owner callback: " + id);
            for (int mode = 0; mode < 4; mode++)
                check((bool)policy.Invoke(null, new object[] { id, mode }) == (mode == 2), "L help visible only for managed local package " + mode + ":" + id);
        }
        string download = (string)guide.GetMethod("Body", Flags).Invoke(null, new object[] { 0 });
        check(download.Contains("AF-GPTSOVITS-L_0.1.0.zip") && download.Contains("https://pan.baidu.com/s/1Wsl1hXm5Mz2_7DRetX94FA?pwd=5qkj") && download.Contains("5qkj"), "Supplied download filename URL and code retained exactly");
        string budget = (string)guide.GetMethod("Body", Flags).Invoke(null, new object[] { 3 });
        check(budget.Contains("1546MiB") && budget.Contains("100ms") && budget.Contains(selected == "English" ? "NOT a hard cap" : "不是硬上限"), "Measured VRAM and target limitations stay together");
        check(settings.EnableTtsSpeech == enabled && settings.TtsProviderDropdown.SelectedIndex == provider, "Reading tutorial construction does not change provider or enable speech");
    }
}
