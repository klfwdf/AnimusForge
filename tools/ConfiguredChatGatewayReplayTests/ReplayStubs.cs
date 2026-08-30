using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal sealed class DuelSettings
{
    public static readonly HttpClient GlobalClient = new HttpClient
    {
        Timeout = Timeout.InfiniteTimeSpan
    };

    public const string ReasoningEffortLow = "low";

    public static DuelSettings GetSettings() => new DuelSettings();

    public int GetAuxiliaryApiMaxTokens() => 256;

    public float GetAuxiliaryApiTemperature() => 0.2f;

    public static string ResolveThinkingControlFormat(string endpoint, string model) => "structured";

    public static bool ApplyThinkingControls(
        JObject payload,
        string endpoint,
        string model,
        bool thinkingEnabled,
        string reasoningEffort,
        out string mode)
    {
        mode = thinkingEnabled ? "structured" : "disabled";
        payload["thinking"] = new JObject
        {
            ["enabled"] = thinkingEnabled,
            ["effort"] = reasoningEffort ?? ReasoningEffortLow
        };
        return true;
    }

    public static void RemoveThinkingControls(JObject payload)
    {
        payload.Remove("thinking");
    }
}

internal static class LlmApiCompat
{
    public static string PrepareChatRequestJson(string endpoint, JObject payload) => payload.ToString(Formatting.None);

    public static void ApplyAuthenticationHeaders(HttpRequestMessage request, string endpoint, string apiKey)
    {
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
    }

    public static string ExtractAssistantText(JObject response)
    {
        return response?["choices"]?[0]?["message"]?["content"]?.ToString() ?? string.Empty;
    }

    public static string ExtractAssistantText(string response)
    {
        return ExtractAssistantText(JObject.Parse(response ?? "{}"));
    }
}

public static class AIConfigHandler
{
    public static bool LooksLikeAuxiliaryThinkingControlErrorForExternal(string responseBody)
    {
        string text = responseBody ?? string.Empty;
        return text.Contains("thinking", StringComparison.OrdinalIgnoreCase)
            && (text.Contains("reject", StringComparison.OrdinalIgnoreCase)
                || text.Contains("unsupported", StringComparison.OrdinalIgnoreCase));
    }
}
