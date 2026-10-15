using System;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class DuelSettings
{
    public int TtsGptSoVitsMood { get; set; } = 0;

    private const string GptVoiceGroup = "{=AF_GPT_UI_Group}7. Voice output/2. GPT-SoVITS";

    [SettingPropertyBool("{=AF_GPT_UI_UseLocal}Use GPT-SoVITS (otherwise Volcengine)", Order = 0, RequireRestart = false,
        HintText = "{=AF_GPT_UI_UseLocalHint}Uses your already-running local service. Failure falls back to text, never to paid cloud speech.")]
    [SettingPropertyGroup(GptVoiceGroup)]
    public bool TtsUseGptSoVits { get; set; } = false;

    [SettingPropertyText("{=AF_GPT_UI_ApiUrl}Local GPT-SoVITS API URL", -1, true, "", Order = 1, RequireRestart = false,
        HintText = "{=AF_GPT_UI_ApiUrlHint}For example http://127.0.0.1:9880/tts. Local HTTP only; AF does not start or stop Python.")]
    [SettingPropertyGroup(GptVoiceGroup)]
    public string TtsGptSoVitsApiUrl { get; set; } = "http://127.0.0.1:9880/tts";

    [SettingPropertyText("{=AF_GPT_UI_Manifest}Voice manifest.json path", -1, true, "", Order = 2, RequireRestart = false,
        HintText = "{=AF_GPT_UI_ManifestHint}Full manifest.json path, or blank for the module VoicePacks folder. Hero choices are saved; other actors use scene-scoped gender-matched voices.")]
    [SettingPropertyGroup(GptVoiceGroup)]
    public string TtsGptSoVitsManifestPath { get; set; } = "";

    [SettingPropertyFloatingInteger("{=AF_GPT_UI_Speed}Local speech speed", 0.5f, 2f, "0.00", Order = 3, RequireRestart = false)]
    [SettingPropertyGroup(GptVoiceGroup)]
    public float TtsGptSoVitsSpeed { get; set; } = 1f;

    [SettingPropertyButton("{=AF_GPT_UI_Test}Test local speech", -1, true, "", Content = "{=AF_GPT_UI_PlayTest}Play test", Order = 4, RequireRestart = false,
        HintText = "{=AF_GPT_UI_TestHint}Requires your local service to be running. No model launch or cloud fallback; disabled in real battles.")]
    [SettingPropertyGroup(GptVoiceGroup)]
    public Action TestTtsGptSoVitsVoice { get; set; }

    private void InitializeGptSoVitsVoiceActions()
    {
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
                bool accepted = engine.SpeakTestAsync(GptSoVitsUiText.Text("TestPhrase", "At your service, traveler!"),
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
