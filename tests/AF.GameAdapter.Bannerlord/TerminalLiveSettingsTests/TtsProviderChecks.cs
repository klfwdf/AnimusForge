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
            var json = JObject.Parse(File.ReadAllText(path));
            foreach (string id in ids) json.Remove(id);
            File.WriteAllText(path, json.ToString());
            var legacy = new DuelSettings(); load(legacy);
            check(legacy.TtsProviderDropdown.SelectedIndex == 0 && legacy.TtsOpenAiApiKey == "" && legacy.TtsVolcDedicatedSpeaker == "preserved-volc-voice", "Old JSON retains Volcengine and does not invent credentials");
            check(legacy.TtsStreamingPlaybackEnabled, "Missing streaming field defaults to enabled");
            var pages = (MCM.Abstractions.Base.Global.FluentGlobalSettings[])typeof(DuelSettings).Assembly.GetType("AnimusForge.AnimusForgeMcmPages", true)
                .GetMethod("CreatePages", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .Invoke(null, new object[] { TerminalProviderProbe.Owners.Values.Cast<BaseSettings>().ToArray() });
            var projected = pages.SelectMany(p => p.GetAllSettingPropertyDefinitions()).ToArray();
            foreach (string id in ids) check(projected.Count(p => p.Id == id) == 1, "TTS MCM projection exists exactly once: " + id);
            string[] allTts = ids.Concat(new[] { "EnableTtsSpeech", "TtsVolcDedicatedEnabled", "TtsSceneUseWinmmAudible", "TtsLipSyncSoundEventVolume", "TestTtsVolcDedicatedVoice" }).ToArray();
            foreach (string id in allTts)
            {
                var apiItem = pages[0].GetAllSettingPropertyDefinitions().Single(p => p.Id == id);
                check(apiItem.GroupName.Contains("AF_TERMINAL_GROUP_TTS_API") && !pages[1].GetAllSettingPropertyDefinitions().Any(p => p.Id == id), "TTS appears only in API / TTS API: " + id);
            }
            var provider = TerminalSettingsRegistry.GetById("TtsProviderDropdown");
            check(provider.DropdownGetter(settings).Count == 2, "TTS provider dropdown has two services");
        }
        finally
        {
            for (int i = 0; i < ids.Length; i++) TerminalSettingsRegistry.GetById(ids[i]).Setter(settings, original[i]);
            settings.TtsVolcDedicatedSpeaker = oldVoice;
            check(save(settings), "TTS fixture restores original settings");
        }
    }
}
