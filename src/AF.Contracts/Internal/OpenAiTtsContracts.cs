namespace AnimusForge.Refactor.Contracts;

/// <summary>Detached binary-WAV request. Authentication is injected at send time.</summary>
public sealed class OpenAiTtsSynthesisRequest
{
    public OpenAiTtsSynthesisRequest(string endpoint, string model, string voice, string text, float speed, int timeoutSeconds)
    { Endpoint = endpoint; Model = model; Voice = voice; Text = text; Speed = speed; TimeoutSeconds = timeoutSeconds; }
    public string Endpoint { get; }
    public string Model { get; }
    public string Voice { get; }
    public string Text { get; }
    public float Speed { get; }
    public int TimeoutSeconds { get; }
}
