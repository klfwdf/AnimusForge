using AnimusForge;
using MCM.Abstractions.Base.Global;
using MCM.Abstractions.Attributes.v2;
using MCM.Common;

static class Program
{
    static int checks;
    static void Check(bool ok, string why) { if (!ok) throw new Exception(why); checks++; }
    static void Main()
    {
        var old = new DuelSettings { EnableTtsSpeech = true, TtsVolcDedicatedEnabled = true,
            TtsVolcDedicatedApiKey = "fixture-old-token", TtsVolcDedicatedApiUrl = "https://fixture.invalid/v3",
            TtsVolcDedicatedSpeaker = "fixture-voice", TtsGptSoVitsManifestPath = "old-external-manifest" };
        DuelSettings.Current = old;
        var page = new SpeechSettings(); GlobalSettings<SpeechSettings>.Instance = page;
        SpeechSettings.Synchronize(old);
        Check(page.LegacyMigrationCompleted && page.ProviderMode.SelectedIndex == 1 && page.EnableTtsSpeech, "legacy cloud selection imported");
        Check(page.TtsVolcDedicatedApiKey == "fixture-old-token" && page.TtsVolcDedicatedSpeaker == "fixture-voice", "credentials and voice preserved");
        Check(page.TtsVolcDedicatedApiUrl == "https://fixture.invalid/v3", "protocol address preserved");
        Check(page.TtsGptSoVitsManifestPath == "", "requested installed catalog is new default");
        int writes = MCM.Abstractions.BaseSettingsProvider.Instance.Saves;
        for (int i = 0; i < 100; i++) SpeechSettings.Synchronize(old);
        Check(MCM.Abstractions.BaseSettingsProvider.Instance.Saves == writes, "unchanged reads do not repeat migration IO");
        page.ProviderMode.SelectedIndex = 0; SpeechSettings.Synchronize(old);
        Check(old.TtsUseGptSoVits && !old.TtsVolcDedicatedEnabled, "local exclusively selected");
        page.ProviderMode.SelectedIndex = 2; SpeechSettings.Synchronize(old);
        Check(!old.TtsUseGptSoVits && !old.TtsVolcDedicatedEnabled, "reserved custom mode cannot call either provider");
        Check(old.TtsVolcDedicatedApiKey == "fixture-old-token", "switching preserves cloud credentials");
        page.ProviderMode.SelectedIndex = 42; Check(page.ProviderMode.SelectedIndex == 2, "provider upper bound");
        page.ProviderMode.SelectedIndex = -4; Check(page.ProviderMode.SelectedIndex == 0, "provider lower bound");
        var choices = page.ProviderMode;
        page.ProviderMode = new Dropdown<string>(new[] { "untrusted label", "other", "third" }, 1);
        SpeechSettings.Synchronize(old);
        Check(ReferenceEquals(choices, page.ProviderMode) && page.ProviderMode.Count == 3, "preset keeps the owned dropdown and fixed options");
        Check(page.ProviderMode[1] == "{=AF_TTS_UI_ProviderCloud}Volcengine TTS API" && old.TtsVolcDedicatedEnabled && !old.TtsUseGptSoVits, "preset index selects cloud without replacing localized labels");
        page.ProviderMode = null; SpeechSettings.Synchronize(old);
        Check(page.ProviderMode.SelectedIndex == 0 && old.TtsUseGptSoVits && !old.TtsVolcDedicatedEnabled, "null preset falls back to local");
        page.TtsGptSoVitsMood = 42; Check(page.TtsGptSoVitsMood == 3, "tone upper bound");
        page.TtsGptSoVitsMood = -1; Check(page.TtsGptSoVitsMood == 0, "tone lower bound");
        page.TtsGptSoVitsMood = 2; SpeechSettings.Synchronize(old);
        Check(old.TtsGptSoVitsMood == 2, "manual tone projected to real capture field");
        var clone = new SpeechSettings { TtsVolcDedicatedApiKey = "unsaved-clone-token" };
        clone.ProviderMode.SelectedIndex = 1;
        clone.TestLocal(); clone.NotifySave();
        Check(old.TtsVolcDedicatedApiKey == "fixture-old-token" && old.LocalTests == 0, "editing/test/save clone does not apply or make a request");
        Check(page.ProviderMode.SelectedIndex == 0 && old.TtsUseGptSoVits, "unsaved dropdown clone cannot change live provider");
        page.TtsVolcDedicatedApiKey = "fixture-new-token"; page.NotifySave();
        Check(old.TtsVolcDedicatedApiKey == "fixture-new-token" && DuelSettings.SaveCount == 1, "live save projects and persists legacy compatibility fields");
        page.LegacyMigrationCompleted = false;
        Check(page.LegacyMigrationCompleted, "preset reset cannot reopen credential migration");
        var reload = new SpeechSettings { LegacyMigrationCompleted = true, TtsVolcDedicatedApiKey = "fixture-reloaded-token" };
        GlobalSettings<SpeechSettings>.Instance = reload;
        old.TtsVolcDedicatedApiKey = "stale-legacy-token";
        SpeechSettings.Synchronize(old);
        Check(old.TtsVolcDedicatedApiKey == "fixture-reloaded-token", "new-page reload wins over stale legacy config");
        var disabledLegacy = new DuelSettings { EnableTtsSpeech = true };
        GlobalSettings<SpeechSettings>.Instance = new SpeechSettings();
        SpeechSettings.Synchronize(disabledLegacy);
        Check(!disabledLegacy.EnableTtsSpeech, "legacy no-provider state stays silent on migration");
        var metadata = typeof(SpeechSettings).GetProperty("ProviderMode").GetCustomAttributes(false);
        Check(metadata.OfType<SettingPropertyDropdownAttribute>().Count() == 1 && !metadata.OfType<SettingPropertyIntegerAttribute>().Any(), "provider uses real dropdown metadata, not a numeric slider");
        Console.WriteLine($"PASS {checks} source-linked TTS settings assertions; MCM/game/native/provider hosts are stubs, no network or audio.");
    }
}
