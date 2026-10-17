using System;
namespace AnimusForge;

// Native text is resolved only at the final main-thread UI consumer.
internal static class TtsErrorUiText
{
    internal static string Describe(string reason)
    {
        switch (reason)
        {
            case "tts_openai_configuration_incomplete": return LlmUiTexts.Text("AF_LLM_UI_TtsError_OpenAiConfiguration", "Enter the speech API key, model and voice.");
            case "tts_openai_endpoint_invalid": return LlmUiTexts.Text("AF_LLM_UI_TtsError_OpenAiEndpoint", "Enter a valid full HTTP/HTTPS speech endpoint.");
            case "tts_openai_text_invalid": return LlmUiTexts.Text("AF_LLM_UI_TtsError_OpenAiText", "Speech text must contain 1–4096 characters.");
            case "tts_openai_speed_invalid": return LlmUiTexts.Text("AF_LLM_UI_TtsError_OpenAiSpeed", "Speech speed must be 0.25–4.0.");
            case "tts_openai_timeout_invalid": return LlmUiTexts.Text("AF_LLM_UI_TtsError_OpenAiTimeoutInvalid", "Speech timeout must be 10–120 seconds.");
            case "tts_openai_timeout": return LlmUiTexts.Text("AF_LLM_UI_TtsError_OpenAiTimeout", "Speech synthesis exceeded the configured timeout; no automatic retry.");
            case "tts_openai_audio_invalid": return LlmUiTexts.Text("AF_LLM_UI_TtsError_OpenAiAudio", "The service did not return a supported PCM16 WAV (mono/stereo, 8000–48000 Hz). Compressed audio is not played.");
            case "tts_audio_silent": return LlmUiTexts.Text("AF_LLM_UI_TtsError_SilentAudio", "The synthesized audio is completely silent; falling back to text.");
            case "tts_endpoint_unsupported": return LlmUiTexts.Text("AF_LLM_UI_TtsError_EndpointUnsupported", "Invalid API URL or unsupported protocol; V3 requires /api/v3/tts/unidirectional and Agent Plan requires /api/v3/plan/tts/unidirectional (HTTP, not SSE/WS).");
            case "tts_configuration_incomplete": return LlmUiTexts.Text("AF_LLM_UI_TtsError_ConfigurationIncomplete", "Check the credential, resource ID, and voice; V1 also requires an App ID.");
            case "tts_extra_parameters_invalid": return LlmUiTexts.Text("AF_LLM_UI_TtsError_ExtraParametersInvalid", "Extra parameters must be valid JSON; V3 requires a JSON object.");
            case "tts_v3_speed_invalid": return LlmUiTexts.Text("AF_LLM_UI_TtsError_V3SpeedInvalid", "V3 speed must be 0.5–2.0 times normal; the setting was not changed automatically.");
            case "tts_v3_sample_rate_invalid": return LlmUiTexts.Text("AF_LLM_UI_TtsError_V3SampleRateInvalid", "V3 sample rate must be 8000, 16000, 22050, 24000, 32000, 44100, or 48000 Hz.");
            case "tts_audio_format_unsupported": return LlmUiTexts.Text("AF_LLM_UI_TtsError_AudioFormatUnsupported", "The player supports wav or pcm only.");
            case "tts_v3_text_invalid": return LlmUiTexts.Text("AF_LLM_UI_TtsError_V3TextInvalid", "V3 text is empty or exceeds the AF limit of 64 KiB UTF-8 per request.");
            case "tts_v3_loudness_invalid": return LlmUiTexts.Text("AF_LLM_UI_TtsError_V3LoudnessInvalid", "V3 cloud volume must be 0.5–2.0 times normal.");
            case "tts_timeout": return LlmUiTexts.Text("AF_LLM_UI_TtsError_Timeout", "Synthesis timed out after 30 seconds; no automatic retry.");
            case "tts_cancelled": return LlmUiTexts.Text("AF_LLM_UI_TtsError_Cancelled", "The voice request was cancelled.");
            case "tts_http_401": return LlmUiTexts.Text("AF_LLM_UI_TtsError_Http401", "Authentication failed; V3 requires the matching service API key, Agent Plan requires its dedicated API key, and resource/voice authorization must match.");
            case "tts_http_403": return LlmUiTexts.Text("AF_LLM_UI_TtsError_Http403", "Authentication failed; V3 requires the matching service API key, Agent Plan requires its dedicated API key, and resource/voice authorization must match.");
            case "tts_http_429": return LlmUiTexts.Text("AF_LLM_UI_TtsError_Http429", "Rate limited; check quota/concurrency. No automatic retry.");
            case "tts_response_too_large": return LlmUiTexts.Text("AF_LLM_UI_TtsError_ResponseTooLarge", "The response exceeded the AF audio or transfer size limit.");
            case "tts_url_missing": return LlmUiTexts.Text("AF_LLM_UI_TtsError_UrlMissing", "The TTS API URL is missing.");
            case "tts_credential_missing": return LlmUiTexts.Text("AF_LLM_UI_TtsError_CredentialMissing", "The TTS token / API key is missing; V1 needs a token and V3 needs the matching service or plan API key.");
            case "tts_app_id_missing": return LlmUiTexts.Text("AF_LLM_UI_TtsError_AppIdMissing", "The V1 TTS App ID is missing.");
            case "tts_resource_id_missing": return LlmUiTexts.Text("AF_LLM_UI_TtsError_ResourceIdMissing", "The TTS resource ID is missing.");
            case "tts_voice_id_missing": return LlmUiTexts.Text("AF_LLM_UI_TtsError_VoiceIdMissing", "The TTS voice ID is missing.");
            case "TTS disabled during playback; fallback to text bubble.": return LlmUiTexts.Text("AF_LLM_UI_TtsError_DisabledDuringPlayback", "TTS disabled during playback; fallback to text bubble.");
            case "Unsupported TTS audio format. Only wav/pcm is supported.": return LlmUiTexts.Text("AF_LLM_UI_TtsError_UnsupportedPlaybackFormat", "Unsupported TTS audio format. Only wav/pcm is supported.");
            case "Scene speech target became unavailable before synthesis.": return LlmUiTexts.Text("AF_LLM_UI_TtsError_TargetBeforeSynthesis", "Scene speech target became unavailable before synthesis.");
            case "TTS synthesis returned empty audio.": return LlmUiTexts.Text("AF_LLM_UI_TtsError_EmptyAudio", "TTS synthesis returned empty audio.");
            case "TTS audio parse failed.": return LlmUiTexts.Text("AF_LLM_UI_TtsError_AudioParseFailed", "TTS audio parse failed.");
            case "Scene speech target became unavailable before playback.": return LlmUiTexts.Text("AF_LLM_UI_TtsError_TargetBeforePlayback", "Scene speech target became unavailable before playback.");
        }
        // Unknown old or user-supplied messages keep their literal content.
        if (reason != null && reason.StartsWith("tts_", StringComparison.Ordinal))
            return DuelSettingsUiLocalization.Format("AF_LLM_UI_TtsError_Unknown", "Synthesis failed ({CODE}); check the TtsEngine log. No automatic retry.", ("CODE", reason));
        return reason ?? string.Empty;
    }
}
