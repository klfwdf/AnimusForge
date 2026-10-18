using System;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class DuelSettings
{
    [SettingPropertyInteger("{=AF_TTS_UI_Mood}Audition tone: 0 Calm / 1 Happy / 2 Angry / 3 Sad", 0, 3, "0", Order = 34, RequireRestart = false)]
    [SettingPropertyGroup(GptVoiceGroup)]
    public int TtsGptSoVitsMood { get; set; } = 0;

    private const string GptVoiceGroup = "{=AF_MCM_Tts_Group}7. Speech synthesis";

    [SettingPropertyText("{=AF_GPT_UI_OutputLanguage}Speech output language", -1, true, "", Order = 1, RequireRestart = false,
        HintText = "{=AF_GPT_UI_OutputLanguageHint}All four TTS services. Enter Chinese, Cantonese, English or Japanese (Chinese names also work). Blank/auto follows the reply. Matching speech stays unchanged; otherwise postprocess translates it privately before synthesis. Subtitles stay original. Invalid or missing translation stays text-only. The selected remote model and voice must support the language.")]
    [SettingPropertyGroup(GptVoiceGroup)]
    public string TtsGptSoVitsOutputLanguage { get; set; } = "";

    // Existing cloud indices 0/1 are unchanged; local providers are additive.
    internal bool TtsUseGptSoVits => (TtsProviderDropdown?.SelectedIndex ?? 0) >= 2;
    internal bool UsesManagedGptSoVits => TtsProviderDropdown?.SelectedIndex == 2;

    [SettingPropertyText("{=AF_GPT_UI_ApiUrl}Local GPT-SoVITS API URL", -1, true, "", Order = 30, RequireRestart = false,
        HintText = "{=AF_GPT_UI_ApiUrlHint}For GPT-SoVITS (manual API), for example http://127.0.0.1:9880/tts. AF-GPTSOVITS manages the installed service automatically.")]
    [SettingPropertyGroup(GptVoiceGroup)]
    public string TtsGptSoVitsApiUrl { get; set; } = "http://127.0.0.1:9880/tts";

    [SettingPropertyText("{=AF_GPT_UI_Manifest}Voice manifest.json path", -1, true, "", Order = 31, RequireRestart = false,
        HintText = "{=AF_GPT_UI_ManifestHint}AF-GPTSOVITS reads the module VoicePacks directory. Manual API may use an explicit manifest. Assign voices in the encyclopedia before chatting; Custom voices are not random defaults.")]
    [SettingPropertyGroup(GptVoiceGroup)]
    public string TtsGptSoVitsManifestPath { get; set; } = "";

    [SettingPropertyFloatingInteger("{=AF_GPT_UI_Speed}Local speech speed", 0.5f, 2f, "0.00", Order = 32, RequireRestart = false)]
    [SettingPropertyGroup(GptVoiceGroup)]
    public float TtsGptSoVitsSpeed { get; set; } = 1f;

    [SettingPropertyButton("{=AF_GPT_UI_Test}Test local speech", -1, true, "", Content = "{=AF_GPT_UI_PlayTest}Play test", Order = 33, RequireRestart = false,
        HintText = "{=AF_GPT_UI_TestHint}Enable speech and select AF-GPTSOVITS or manual API first. The managed service must be ready; real battles disable local synthesis.")]
    [SettingPropertyGroup(GptVoiceGroup)]
    public Action TestTtsGptSoVitsVoice { get; set; }

    private void InitializeGptSoVitsVoiceActions()
    {
        InitializeGptSoVitsRuntimeActions();
        TestTtsGptSoVitsVoice = () =>
        {
            try
            {
                var settings = GetSettings() ?? this;
                if (!settings.TtsUseGptSoVits || !settings.EnableTtsSpeech)
                {
                    InformationManager.DisplayMessage(new InformationMessage(GptSoVitsUiText.Text("EnableFirst", "Enable TTS and select GPT-SoVITS first.")));
                    return;
                }
                if (GptSoVitsVoiceRuntime.IsBattleBlocked)
                {
                    InformationManager.DisplayMessage(new InformationMessage(GptSoVitsUiText.Text("BattleBlocked", "Local speech is disabled in real battles.")));
                    return;
                }
                var voice = GptSoVitsVoiceRuntime.GetCatalog(settings).ResolveAssignment(null, false);
                var engine = TtsEngine.Instance;
                engine.Initialize();
                bool accepted = engine.SpeakTestAsync(GptSoVitsUiText.TestPhrase(settings.TtsGptSoVitsOutputLanguage),
                    settings.TtsGptSoVitsSpeed, TtsTestFeedback.ReportFailure, voice.Id);
                InformationManager.DisplayMessage(new InformationMessage(accepted
                    ? GptSoVitsUiText.Text("Queued", "Local speech queued; the service must be running. Failure falls back to text.")
                    : GptSoVitsUiText.Text("NotQueued", "Local speech was not queued: engine unavailable, queue full, or battle active.")));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(GptSoVitsUiText.Text("TestFailed", "Local speech test was not submitted: ") + ex.Message));
            }
        };
    }
}
