namespace AnimusForge;

// Display policy only. Never filter discovery, persistence, presets or provider dispatch.
internal static class TtsSettingsVisibility
{
    internal static int Provider(DuelSettings settings) => settings?.TtsProviderDropdown?.SelectedIndex ?? 0;
    internal static bool IsProviderSpecific(string id) => Mask(id) != 15;
    internal static bool IsVisible(string id, int provider) => (Mask(id) & (1 << (provider >= 0 && provider <= 3 ? provider : 0))) != 0;

    private static int Mask(string id)
    {
        switch (id)
        {
            case nameof(DuelSettings.TtsVolcDedicatedEnabled):
            case nameof(DuelSettings.TtsVolcDedicatedApiUrl):
            case nameof(DuelSettings.TtsVolcDedicatedApiKey):
            case nameof(DuelSettings.TtsVolcDedicatedAppKey):
            case nameof(DuelSettings.TtsVolcDedicatedResourceId):
            case nameof(DuelSettings.TtsVolcDedicatedSpeaker):
            case nameof(DuelSettings.TtsVolcDedicatedAdditionsJson):
            case nameof(DuelSettings.TtsVolcDedicatedAudioFormat):
            case nameof(DuelSettings.TtsVolcDedicatedSampleRate):
            case nameof(DuelSettings.TtsVolcDedicatedSpeed): return 1;
            case nameof(DuelSettings.TtsStreamingPlaybackEnabled):
            case nameof(DuelSettings.TtsOpenAiApiUrl):
            case nameof(DuelSettings.TtsOpenAiApiKey):
            case nameof(DuelSettings.TtsOpenAiModel):
            case nameof(DuelSettings.TtsOpenAiVoice):
            case nameof(DuelSettings.TtsOpenAiSpeed):
            case nameof(DuelSettings.TtsOpenAiTimeoutSeconds): return 2;
            case nameof(DuelSettings.TestTtsVolcDedicatedVoice): return 3;
            case nameof(DuelSettings.TtsLiteGuideDownload):
            case nameof(DuelSettings.TtsLiteGuideInstall):
            case nameof(DuelSettings.TtsLiteGuideUse):
            case nameof(DuelSettings.TtsLiteGuideHardware):
            case nameof(DuelSettings.TtsLiteGuideRecordings):
            case nameof(DuelSettings.TtsLiteGuideManifest):
            case nameof(DuelSettings.TtsLiteGuideLanguage):
            case nameof(DuelSettings.TtsLiteGuideTroubleshoot):
            case nameof(DuelSettings.RefreshTtsGptSoVitsRuntime): return 4;
            case nameof(DuelSettings.TtsGptSoVitsApiUrl):
            case nameof(DuelSettings.TtsGptSoVitsManifestPath): return 8;
            case nameof(DuelSettings.TtsGptSoVitsSpeed):
            case nameof(DuelSettings.TtsGptSoVitsMood):
            case nameof(DuelSettings.TestTtsGptSoVitsVoice): return 12;
            // Playback volume/routing, provider selection and master switch are common.
            default: return 15;
        }
    }
}
