using System;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Common;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class DuelSettings
{
    private Dropdown<string> _ttsProviderDropdown = BuildTtsProviderDropdown(0);
    private static Dropdown<string> BuildTtsProviderDropdown(int index) => new Dropdown<string>(new[]
    { "{=AF_MCM_Tts_ProviderVolc}Volcengine", "{=AF_MCM_Tts_ProviderOpenAi}OpenAI-compatible" }, index);

    [SettingPropertyDropdown("{=AF_MCM_Tts_Provider}TTS service", Order = -1, RequireRestart = false, HintText = "{=AF_MCM_Tts_Provider_Hint}Choose one service. Existing configurations default to Volcengine. Switching keeps both services' settings; no automatic fallback.")]
    [SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Speech synthesis")]
    public Dropdown<string> TtsProviderDropdown
    {
        get => _ttsProviderDropdown;
        set => _ttsProviderDropdown = BuildTtsProviderDropdown(value?.SelectedIndex ?? 0);
    }

    internal bool UsesOpenAiTts => TtsProviderDropdown?.SelectedIndex == 1;
    internal bool IsSelectedTtsEnabled => EnableTtsSpeech &&
        (UsesOpenAiTts || ((TtsProviderDropdown?.SelectedIndex ?? 0) == 0 && TtsVolcDedicatedEnabled));

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
        bool queued = engine.SpeakTestAsync(Ui("AF_MCM_Tts_TestPhrase", "At your service, traveler!"), TtsOpenAiSpeed, TtsTestFeedback.ReportFailure);
        InformationManager.DisplayMessage(new InformationMessage(queued
            ? Ui("AF_MCM_Tts_OpenAiTesting", "[TTS] Testing the selected OpenAI-compatible speech service; synthesis may incur charges.")
            : Ui("AF_MCM_Tts_TestNotQueued", "[TTS] Test voice was not queued: the queue is full or the engine has stopped. Try again later."),
            Color.FromUint(queued ? 4294967040u : 4294901760u)));
    }
}
