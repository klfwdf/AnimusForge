using System;

namespace AnimusForge.Refactor.Contracts;

/// <summary>
/// String-only request for the existing dedicated TTS provider. Credentials
/// are intentionally absent; the legacy owner resolves them immediately
/// before sending through its gateway.
/// </summary>
public sealed class TtsSynthesisRequest
{
    public TtsSynthesisRequest(string endpoint, string appId, string resourceId, string voiceId, string text, string encoding, int sampleRate, float speedRatio, float loudnessRatio, string extraParametersJson)
    {
        Endpoint = ContractGuard.Required(endpoint, nameof(endpoint));
        AppId = ContractGuard.Required(appId, nameof(appId));
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
    {
        Success = success;
        AudioBytes = audioBytes == null ? Array.Empty<byte>() : (byte[])audioBytes.Clone();
        StatusCode = statusCode;
        ErrorCode = errorCode ?? string.Empty;
    }

    public bool Success { get; }
    public byte[] AudioBytes { get; }
    public int? StatusCode { get; }
    public string ErrorCode { get; }
}

public interface ITtsGateway
{
    System.Threading.Tasks.Task<TtsSynthesisResult> SynthesizeAsync(TtsSynthesisRequest request, string credentialToken, System.Threading.CancellationToken cancellationToken);
}
