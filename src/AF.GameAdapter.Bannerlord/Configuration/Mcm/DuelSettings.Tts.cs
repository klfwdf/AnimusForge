using System;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Common;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class DuelSettings
{
    [SettingPropertyButton("{=AF_GPT_UI_Guide}TTS user guide", -1, true, "", Content = "{=AF_GPT_UI_GuideRead}Read guide", Order = -30, RequireRestart = false,
        HintText = "{=AF_GPT_UI_GuideHint}Installation, services, voice packs, encyclopedia selection, language, tone and troubleshooting. Reading does not start a service or change settings.")]
    [SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Speech synthesis")]
    public Action OpenTtsUserGuide { get; set; } = TtsUserGuide.Open;

    private Dropdown<string> _ttsProviderDropdown = BuildTtsProviderDropdown(0);
    private static Dropdown<string> BuildTtsProviderDropdown(int index) => new Dropdown<string>(new[]
    { "{=AF_MCM_Tts_ProviderVolc}Volcengine", "{=AF_MCM_Tts_ProviderOpenAi}OpenAI-compatible", "{=AF_MCM_Tts_ProviderAf}AF-GPTSOVITS-L / U (auto)", "{=AF_MCM_Tts_ProviderManual}GPT-SoVITS (manual API)" }, Math.Max(0, Math.Min(3, index)));

    [SettingPropertyDropdown("{=AF_MCM_Tts_Provider}TTS service", Order = -20, RequireRestart = false, HintText = "{=AF_MCM_Tts_Provider_Hint}Only the selected service parameters and common playback controls are shown. Switching preserves other service settings. AF-GPTSOVITS automatically detects L/U packages in the mod folder (L first); manual API requires a running service. Failures remain text-only.")]
    [SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Speech synthesis")]
    public Dropdown<string> TtsProviderDropdown
    {
        get => _ttsProviderDropdown;
        set => _ttsProviderDropdown = BuildTtsProviderDropdown(value?.SelectedIndex ?? 0);
    }

    internal bool UsesOpenAiTts => TtsProviderDropdown?.SelectedIndex == 1;
    internal bool IsSelectedTtsEnabled => EnableTtsSpeech &&
        (UsesOpenAiTts || TtsUseGptSoVits || ((TtsProviderDropdown?.SelectedIndex ?? 0) == 0 && TtsVolcDedicatedEnabled));

    [SettingPropertyBool("{=AF_MCM_Tts_Streaming}Stream audio playback with lip-sync", Order = 2, RequireRestart = false, HintText = "{=AF_MCM_Tts_Streaming_Hint}Default: on. Sends stream=true to the OpenAI-compatible service and plays incoming WAV segments with lip-sync. Turning off sends stream=false and waits for the complete utterance. Upstream streaming support and first-audio latency depend on the service. Applies only to the OpenAI-compatible service.")]
    [SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Speech synthesis")]
    public bool TtsStreamingPlaybackEnabled { get; set; } = true;

    [SettingPropertyText("{=AF_MCM_Tts_OpenAiUrl}OpenAI-compatible speech API URL", -1, true, "", Order = 20, RequireRestart = false, HintText = "{=AF_MCM_Tts_OpenAiUrl_Hint}Full speech endpoint, for example https://www.shenlanqaq.com/v1/audio/speech. Requests PCM16 WAV; the server must support it.")]
    [SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Speech synthesis")]
    public string TtsOpenAiApiUrl { get; set; } = "https://www.shenlanqaq.com/v1/audio/speech";

    [SettingPropertyText("{=AF_MCM_Tts_OpenAiKey}OpenAI-compatible speech API key", -1, true, "", Order = 21, RequireRestart = false, HintText = "{=AF_MCM_Tts_OpenAiKey_Hint}Independent speech credential sent as Authorization: Bearer. Does not reuse the chat or Volcengine key.")]
    [SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Speech synthesis")]
    public string TtsOpenAiApiKey { get; set; } = "";

    [SettingPropertyText("{=AF_MCM_Tts_OpenAiModel}Speech model", -1, true, "", Order = 22, RequireRestart = false, HintText = "{=AF_MCM_Tts_OpenAiModel_Hint}Model accepted by this speech endpoint. Default: gpt-sovits-v4.")]
    [SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Speech synthesis")]
    public string TtsOpenAiModel { get; set; } = "gpt-sovits-v4";

    [SettingPropertyText("{=AF_MCM_Tts_OpenAiVoice}Speech voice", -1, true, "", Order = 23, RequireRestart = false, HintText = "{=AF_MCM_Tts_OpenAiVoice_Hint}Default: zh_default. This service uses one configured voice for all NPCs. Existing NPC/Volcengine voice mappings are retained but not sent to this service.")]
    [SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Speech synthesis")]
    public string TtsOpenAiVoice { get; set; } = "zh_default";

    [SettingPropertyFloatingInteger("{=AF_MCM_Tts_OpenAiSpeed}OpenAI-compatible speech speed", 0.25f, 4f, "0.00", Order = 24, RequireRestart = false, HintText = "{=AF_MCM_Tts_OpenAiSpeed_Hint}Requested speed ratio, 0.25–4.0; default 1.0. Actual support depends on the upstream model.")]
    [SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Speech synthesis")]
    public float TtsOpenAiSpeed { get; set; } = 1f;

    [SettingPropertyInteger("{=AF_MCM_Tts_OpenAiTimeout}OpenAI-compatible speech timeout (seconds)", 10, 120, "0", Order = 25, RequireRestart = false, HintText = "{=AF_MCM_Tts_OpenAiTimeout_Hint}Total request/read deadline for this service. Default: 60 seconds. No automatic retry; failure falls back to text.")]
    [SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Speech synthesis")]
    public int TtsOpenAiTimeoutSeconds { get; set; } = 60;

    private void TestOpenAiTtsVoice()
    {
        TtsEngine engine = TtsEngine.Instance;
        engine.Initialize();
        if (!EnableTtsSpeech)
        {
            InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Tts_EnableSpeech", "Enable TTS speech"), Color.FromUint(4294901760u)));
            return;
        }
        bool queued = engine.SpeakTestAsync(GptSoVitsUiText.TestPhrase(TtsGptSoVitsOutputLanguage), TtsOpenAiSpeed, TtsTestFeedback.ReportFailure);
        InformationManager.DisplayMessage(new InformationMessage(queued
            ? Ui("AF_MCM_Tts_OpenAiTesting", "[TTS] Testing the selected OpenAI-compatible speech service; synthesis may incur charges.")
            : Ui("AF_MCM_Tts_TestNotQueued", "[TTS] Test voice was not queued: the queue is full or the engine has stopped. Try again later."),
            Color.FromUint(queued ? 4294967040u : 4294901760u)));
    }
}
