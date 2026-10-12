using System;

namespace AnimusForge.Refactor.Contracts;

/// <summary>
/// String-only request for the existing dedicated TTS provider. Credentials
/// are intentionally absent; the playback owner resolves them immediately
/// before sending through its gateway.
/// </summary>
public sealed class TtsSynthesisRequest
{
    public TtsSynthesisRequest(string endpoint, string appId, string resourceId, string voiceId, string text, string encoding, int sampleRate, float speedRatio, float loudnessRatio, string extraParametersJson)
    {
        Endpoint = ContractGuard.Required(endpoint, nameof(endpoint));
        // V3 API-Key authentication does not use AppID; the V1 gateway enforces it.
        AppId = appId?.Trim() ?? string.Empty;
        ResourceId = ContractGuard.Required(resourceId, nameof(resourceId));
        VoiceId = ContractGuard.Required(voiceId, nameof(voiceId));
        Text = text ?? string.Empty;
        Encoding = ContractGuard.Required(encoding, nameof(encoding));
        SampleRate = sampleRate;
        SpeedRatio = speedRatio;
        LoudnessRatio = loudnessRatio;
        ExtraParametersJson = extraParametersJson ?? "{}";
    }

    public string Endpoint { get; }
    public string AppId { get; }
    public string ResourceId { get; }
    public string VoiceId { get; }
    public string Text { get; }
    public string Encoding { get; }
    public int SampleRate { get; }
    public float SpeedRatio { get; }
    public float LoudnessRatio { get; }
    public string ExtraParametersJson { get; }
}

public sealed class TtsSynthesisResult
{
    public TtsSynthesisResult(bool success, byte[] audioBytes, int? statusCode, string errorCode)
        : this(success, audioBytes, statusCode, errorCode, string.Empty) { }

    public TtsSynthesisResult(bool success, byte[] audioBytes, int? statusCode, string errorCode, string logId)
    {
        Success = success;
        AudioBytes = audioBytes == null ? Array.Empty<byte>() : (byte[])audioBytes.Clone();
        StatusCode = statusCode;
        ErrorCode = errorCode ?? string.Empty;
        LogId = logId ?? string.Empty;
    }

    public bool Success { get; }
    public byte[] AudioBytes { get; }
    public int? StatusCode { get; }
    public string ErrorCode { get; }
    public string LogId { get; }
}

public interface ITtsGateway
{
    System.Threading.Tasks.Task<TtsSynthesisResult> SynthesizeAsync(TtsSynthesisRequest request, string credentialToken, System.Threading.CancellationToken cancellationToken);
}
