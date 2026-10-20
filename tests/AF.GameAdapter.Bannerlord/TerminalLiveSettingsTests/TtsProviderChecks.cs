using System;
using System.IO;
using System.Linq;
using AnimusForge;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using Newtonsoft.Json.Linq;

// Actual terminal setters and native MCM JSON. Credentials here are fixture values only.
static class TtsProviderChecks
{
    internal static void Run(BaseSettings owner, string path, Func<BaseSettings, bool> save, Action<BaseSettings> load, Action<bool, string> check)
    {
        var settings = (DuelSettings)owner;
        string[] ids = { "TtsProviderDropdown", "TtsOpenAiApiUrl", "TtsOpenAiApiKey", "TtsOpenAiModel", "TtsOpenAiVoice", "TtsOpenAiSpeed", "TtsOpenAiTimeoutSeconds", "TtsStreamingPlaybackEnabled" };
        object[] values = { 1, "https://tts-fixture.invalid/v1/audio/speech", "fixture-key", "fixture-model", "zh_default", 1.25f, 75, false };
        object[] original = { settings.TtsProviderDropdown.SelectedIndex, settings.TtsOpenAiApiUrl, settings.TtsOpenAiApiKey, settings.TtsOpenAiModel, settings.TtsOpenAiVoice, settings.TtsOpenAiSpeed, settings.TtsOpenAiTimeoutSeconds, settings.TtsStreamingPlaybackEnabled };
        string oldVoice = settings.TtsVolcDedicatedSpeaker;
        string oldLanguage = settings.TtsGptSoVitsOutputLanguage;
        settings.TtsVolcDedicatedSpeaker = "preserved-volc-voice";
        try
        {
            var localization = System.Reflection.Assembly.Load("TaleWorlds.Localization");
            localization.GetType("TaleWorlds.Localization.LocalizedTextManager", true).GetMethod("LoadLocalizationXmls")
                .Invoke(null, new object[] { new[] { Environment.GetEnvironmentVariable("AF_TEST_MODULE_ROOT") } });
            var language = localization.GetType("TaleWorlds.Localization.MBTextManager", true).GetMethod("ChangeLanguage");
            foreach (string selected in new[] { "English", "简体中文" })
            {
                language.Invoke(null, new object[] { selected });
                check(TerminalSettingsRegistry.GetById("TtsProviderDropdown").DisplayName == (selected == "English" ? "TTS service" : "TTS 服务"), "Actual native TTS service label " + selected);
                check(TerminalSettingsRegistry.GetById("TtsOpenAiVoice").DisplayName == (selected == "English" ? "Speech voice" : "语音音色"), "Actual native TTS voice label " + selected);
                check(TerminalSettingsRegistry.GetById("TtsGptSoVitsOutputLanguage").DisplayName == (selected == "English" ? "Speech output language" : "语音输出语言"), "Speech language localized through actual terminal consumer");
                string languageHint = TerminalSettingsRegistry.GetById("TtsGptSoVitsOutputLanguage").HintText;
                check(languageHint.Contains(selected == "English" ? "All four TTS services" : "四种 TTS 服务共用"), "shared language hint through actual consumer " + selected);
                var guideDefinition = TerminalSettingsRegistry.GetById("OpenTtsUserGuide");
                check(guideDefinition != null && guideDefinition.Invoker != null && guideDefinition.DisplayName == (selected == "English" ? "TTS user guide" : "语音使用说明"), "Guide native/terminal localized action " + selected);
                var guide = typeof(DuelSettings).Assembly.GetType("AnimusForge.TtsUserGuide", true);
                var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
                check(settings.OpenTtsUserGuide.Method.DeclaringType == guide, "Native action binds production guide");
                var savedAction = settings.OpenTtsUserGuide;
                bool invoked = false;
                try { settings.OpenTtsUserGuide = () => invoked = true; guideDefinition.Invoker(settings); }
                finally { settings.OpenTtsUserGuide = savedAction; }
                check(invoked, "Terminal invokes original owner action, no duplicate guide state");
                check(guide.GetMethod("CreateContents", flags).Invoke(null, null) != null, "Real native chapter menu constructed " + selected);
                for (int chapter = 0; chapter < 8; chapter++)
                {
                    string title = (string)guide.GetMethod("Title", flags).Invoke(null, new object[] { chapter });
                    string body = (string)guide.GetMethod("Body", flags).Invoke(null, new object[] { chapter });
                    check(!title.Contains("{=") && !body.Contains("{=") && body.Length > 100 && body.Length < 1500, "Guide title/body resolve, bounded chapter " + chapter + ":" + selected);
                    check(body.Contains(selected == "English" ? new[] { "Modules/AnimusForge", "Volcengine", "Custom", "8 MiB", "profiles", "postprocess", "NOT a hard", "SHA-256" }[chapter] : new[] { "安装", "火山", "百科", "单声道", "profiles", "后处理", "硬上限", "哈希" }[chapter]) || (selected != "English" && chapter == 0 && body.Contains("模组")), "Guide chapter meaning/localization " + chapter + ":" + selected);
                    check(guide.GetMethod("CreatePage", flags).Invoke(null, new object[] { chapter }) != null, "Real native inquiry page constructed " + chapter);
                }
                TtsLiteGuideChecks.Run(settings, selected, check);
                string streamHint = TerminalSettingsRegistry.GetById("TtsStreamingPlaybackEnabled").HintText;
                check(streamHint.Contains("stream=true") && streamHint.Contains("stream=false"), "Actual native streaming hint describes upstream on/off flags " + selected);
            }
            language.Invoke(null, new object[] { "English" });
            for (int i = 0; i < ids.Length; i++)
            {
                var definition = TerminalSettingsRegistry.GetById(ids[i]);
                check(definition != null && definition.TabCategory == TerminalSettingsRegistry.GetById("EnableTtsSpeech").TabCategory, "TTS setting is discoverable in terminal AI: " + ids[i]);
                definition.Setter(settings, values[i]);
            }
            check(save(settings), "TTS native MCM saves original owner");
            var restored = new DuelSettings(); load(restored);
            check(restored.TtsProviderDropdown.SelectedIndex == 1 && restored.TtsOpenAiApiUrl == (string)values[1] && restored.TtsOpenAiApiKey == "fixture-key" && restored.TtsOpenAiModel == "fixture-model", "TTS provider/address/key/model JSON roundtrip");
            check(restored.TtsOpenAiVoice == "zh_default" && Math.Abs(restored.TtsOpenAiSpeed - 1.25f) < 0.001f && restored.TtsOpenAiTimeoutSeconds == 75, "TTS voice/speed/deadline JSON roundtrip");
            check(!restored.TtsStreamingPlaybackEnabled, "TTS streaming playback switch JSON roundtrip");
            check(restored.TtsVolcDedicatedSpeaker == "preserved-volc-voice", "TTS switch preserves old voice");
            foreach (int mode in new[] { 2, 3 })
            {
                TerminalSettingsRegistry.GetById("TtsProviderDropdown").Setter(settings, mode);
                settings.TtsGptSoVitsApiUrl = "http://127.0.0.1:9881/tts";
                check(save(settings), "Local mode native MCM save " + mode);
                var local = new DuelSettings(); load(local);
                check(local.TtsProviderDropdown.SelectedIndex == mode && local.TtsGptSoVitsApiUrl == settings.TtsGptSoVitsApiUrl,
                    "Local provider JSON roundtrip " + mode);
                check(local.TtsOpenAiApiKey == "fixture-key" && local.TtsVolcDedicatedSpeaker == "preserved-volc-voice", "Local selection preserves cloud credentials and voice");
            }
            foreach (int speechProvider in new[] { 0, 1, 2, 3 })
            foreach (string speechLanguage in new[] { "中文", "粤语", "英语", "日语", "Japanese", "" })
            {
                TerminalSettingsRegistry.GetById("TtsProviderDropdown").Setter(settings, speechProvider);
                TerminalSettingsRegistry.GetById("TtsGptSoVitsOutputLanguage").Setter(settings, speechLanguage);
                check(save(settings), "Language saves via original owner " + speechLanguage);
                var languageRoundtrip = new DuelSettings(); load(languageRoundtrip);
                check(languageRoundtrip.TtsGptSoVitsOutputLanguage == speechLanguage, "Language native JSON roundtrip " + speechLanguage);
            }
            var json = JObject.Parse(File.ReadAllText(path));
            json.Remove("TtsGptSoVitsOutputLanguage");
            foreach (string id in ids) json.Remove(id);
            File.WriteAllText(path, json.ToString());
            var legacy = new DuelSettings(); load(legacy);
            check(legacy.TtsProviderDropdown.SelectedIndex == 0 && legacy.TtsOpenAiApiKey == "" && legacy.TtsVolcDedicatedSpeaker == "preserved-volc-voice", "Old JSON retains Volcengine and does not invent credentials");
            check(legacy.TtsGptSoVitsOutputLanguage == "", "Old JSON remains automatic, never implicitly changes language");
            check(legacy.TtsStreamingPlaybackEnabled, "Missing streaming field defaults to enabled");
            var pages = (MCM.Abstractions.Base.Global.FluentGlobalSettings[])typeof(DuelSettings).Assembly.GetType("AnimusForge.AnimusForgeMcmPages", true)
                .GetMethod("CreatePages", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .Invoke(null, new object[] { TerminalProviderProbe.Owners.Values.Cast<BaseSettings>().ToArray() });
            var projected = pages.SelectMany(p => p.GetAllSettingPropertyDefinitions()).ToArray();
            foreach (string id in ids) check(projected.Count(p => p.Id == id) == 1, "TTS MCM projection exists exactly once: " + id);
            string[] localIds = { "OpenTtsUserGuide", "TtsGptSoVitsOutputLanguage", "TtsGptSoVitsApiUrl", "TtsGptSoVitsManifestPath", "TtsGptSoVitsSpeed", "TtsGptSoVitsMood", "TestTtsGptSoVitsVoice", "RefreshTtsGptSoVitsRuntime" };
            foreach (string id in localIds)
            {
                check(TerminalSettingsRegistry.GetById(id) != null, "Local control has actual terminal consumer: " + id);
                check(projected.Count(p => p.Id == id) == 1, "Local control has one MCM projection: " + id);
            }
            string[] allTts = ids.Concat(localIds).Concat(new[] { "EnableTtsSpeech", "TtsVolcDedicatedEnabled", "TtsSceneUseWinmmAudible", "TtsLipSyncSoundEventVolume", "TestTtsVolcDedicatedVoice" }).ToArray();
            foreach (string id in allTts)
            {
                var apiItem = pages[0].GetAllSettingPropertyDefinitions().Single(p => p.Id == id);
                check(apiItem.GroupName.Contains("AF_TERMINAL_GROUP_TTS_API") && !pages[1].GetAllSettingPropertyDefinitions().Any(p => p.Id == id), "TTS appears only in API / TTS API: " + id);
            }
            var provider = TerminalSettingsRegistry.GetById("TtsProviderDropdown");
            check(provider.DropdownGetter(settings).Count == 4, "TTS dropdown preserves cloud entries and adds automatic/manual local");
        }
        finally
        {
            for (int i = 0; i < ids.Length; i++) TerminalSettingsRegistry.GetById(ids[i]).Setter(settings, original[i]);
            settings.TtsVolcDedicatedSpeaker = oldVoice;
            settings.TtsGptSoVitsOutputLanguage = oldLanguage;
            check(save(settings), "TTS fixture restores original settings");
        }
    }
}
