using System;
using System.Linq;
using System.Reflection;
using AnimusForge;
using MCM.Abstractions;

static class TtsUltraGuideChecks
{
    static readonly BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
    internal static void Run(DuelSettings settings, string selected, Action<bool, string> check)
    {
        const string id = "TtsUltraUserGuide";
        var guide = typeof(DuelSettings).Assembly.GetType("AnimusForge.GptSoVitsUltraGuide", true);
        var definition = TerminalSettingsRegistry.GetById(id);
        var policy = typeof(DuelSettings).Assembly.GetType("AnimusForge.TtsSettingsVisibility", true).GetMethod("IsVisible", Flags);
        bool enabled = settings.EnableTtsSpeech;
        int provider = settings.TtsProviderDropdown.SelectedIndex;
        check(definition != null && definition.Invoker != null && definition.Setter == null, "U guide is a read-only terminal action");
        check(definition.DisplayName == (selected == "English" ? "U edition - quality, installation and voices" : "U 版教程 — 音质、安装与语音包"), "U guide localized through real terminal consumer " + selected);
        check(settings.GetAllSettingPropertyDefinitions().Count(p => p.Id == id) == 1, "U native metadata unique");
        check(settings.TtsUltraUserGuide.Method.DeclaringType == guide, "U native action invokes production guide");
        var saved = settings.TtsUltraUserGuide;
        bool invoked = false;
        try { settings.TtsUltraUserGuide = () => invoked = true; definition.Invoker(settings); }
        finally { settings.TtsUltraUserGuide = saved; }
        check(invoked, "U terminal invokes original owner action");
        int count = (int)guide.GetField("ChapterCount", Flags).GetRawConstantValue();
        check(count == 6 && guide.GetMethod("CreateContents", Flags).Invoke(null, null) != null, "U native six-chapter menu");
        string[] anchors = { "gpt-sovits-v5", "runtime.json", "Custom", "2292MiB", "emotion_variants", "MOOD" };
        for (int chapter = 0; chapter < count; chapter++)
        {
            string title = (string)guide.GetMethod("Title", Flags).Invoke(null, new object[] { chapter });
            string body = (string)guide.GetMethod("Body", Flags).Invoke(null, new object[] { chapter });
            check(!title.Contains("{=") && !body.Contains("{=") && body.Length > 100 && body.Length < 1500 && body.Contains(anchors[chapter]), "U bounded localized chapter " + chapter + ":" + selected);
            var page = guide.GetMethod("CreatePage", Flags).Invoke(null, new object[] { chapter });
            var prop = page.GetType().GetProperty("Text");
            check((string)(prop != null ? prop.GetValue(page) : page.GetType().GetField("Text").GetValue(page)) == body, "U native inquiry consumes chapter " + chapter);
        }
        string budget = (string)guide.GetMethod("Body", Flags).Invoke(null, new object[] { 3 });
        check(budget.Contains(selected == "English" ? "NOT validated" : "未验证") && budget.Contains("6GiB"), "U hardware limits do not borrow L guarantee");
        for (int mode = 0; mode < 4; mode++)
            check((bool)policy.Invoke(null, new object[] { id, mode }) == (mode == 2), "U help only for managed local provider " + mode);
        check(settings.EnableTtsSpeech == enabled && settings.TtsProviderDropdown.SelectedIndex == provider, "U guide construction does not change service");
    }
}
