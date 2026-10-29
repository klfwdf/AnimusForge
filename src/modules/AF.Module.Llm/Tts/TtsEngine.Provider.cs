using System;
using System.Net.Http;
using System.Threading;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge;

internal sealed partial class TtsEngine
{
    private static readonly HttpClient _openAiHttpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
    { Timeout = Timeout.InfiniteTimeSpan };

    // Private owner state captured before enqueue, never serialized or logged.
    internal const float MaxMainVolume = 2f;

    private sealed class ProviderSnapshot
    {
        internal readonly bool OpenAi, Enabled, SceneWinmm, Streaming;
        internal readonly int ProviderMode;
        internal readonly GptSoVitsLanguage SpeechLanguage;
        internal readonly string Url, Credential, AppId, ResourceId, Voice, Encoding, Extra, Model;
        internal readonly int SampleRate, TimeoutSeconds;
        internal readonly float Speed, Volume;
        internal ProviderSnapshot(DuelSettings settings)
        {
            OpenAi = settings?.UsesOpenAiTts ?? false;
            ProviderMode = settings?.TtsProviderDropdown?.SelectedIndex ?? 0;
            SpeechLanguage = GptSoVitsLanguage.Parse(settings?.TtsGptSoVitsOutputLanguage);
            Streaming = settings?.TtsStreamingPlaybackEnabled ?? true;
            Enabled = settings?.IsSelectedTtsEnabled ?? false;
            Url = OpenAi ? settings.TtsOpenAiApiUrl : settings?.TtsVolcDedicatedApiUrl;
            Credential = OpenAi ? settings.TtsOpenAiApiKey : settings?.TtsVolcDedicatedApiKey;
            Voice = OpenAi ? settings.TtsOpenAiVoice : settings?.TtsVolcDedicatedSpeaker;
            Speed = OpenAi ? settings.TtsOpenAiSpeed : settings?.TtsVolcDedicatedSpeed ?? 1f;
            AppId = settings?.TtsVolcDedicatedAppKey;
            ResourceId = settings?.TtsVolcDedicatedResourceId;
            Encoding = OpenAi ? "wav" : settings?.TtsVolcDedicatedAudioFormat ?? "wav";
            Extra = settings?.TtsVolcDedicatedAdditionsJson ?? "{}";
            SampleRate = settings?.TtsVolcDedicatedSampleRate ?? 24000;
            // 0..1 attenuates; 1..2 amplifies the PCM (clipped), used by winmm and the map tableau.
            float volume = settings?.TtsVolcDedicatedVolume ?? 1f;
            Volume = float.IsNaN(volume) || float.IsInfinity(volume) ? 1f : Math.Max(0f, Math.Min(MaxMainVolume, volume));
            SceneWinmm = settings?.TtsSceneUseWinmmAudible ?? true;
            Model = settings?.TtsOpenAiModel;
            TimeoutSeconds = settings?.TtsOpenAiTimeoutSeconds ?? 60;
        }
    }

    private byte[] CallOpenAiApi(ProviderSnapshot settings, string voice, string text, float speed, CancellationToken cancel, out string error)
    {
        var request = new OpenAiTtsSynthesisRequest(settings.Url, settings.Model, voice, text, speed, settings.TimeoutSeconds);
        var result = new OpenAiCompatibleTtsGateway(_openAiHttpClient).SynthesizeAsync(request, settings.Credential, cancel).GetAwaiter().GetResult();
        error = result.ErrorCode;
        if (!result.Success) Logger.Log("TtsEngine", "OpenAI-compatible synthesis failed: " + error);
        return result.Success ? result.AudioBytes : null;
    }
}
