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

    public const int DefaultGeneralApiMaxTokens = 5000;
    public const int DefaultEventAndRebellionApiMaxTokens = 4000;
    public const int LlmRequestTimeoutMilliseconds = 2000;
    public const string ReasoningEffortHigh = "high";
    public static DuelSettings Current;
    public static int SettingsReads;
    public static bool SettingsNull, SettingsThrow;
    public static DuelSettings GetSettings() { SettingsReads++; if (SettingsThrow) throw new InvalidOperationException("synthetic_settings"); return SettingsNull ? null : Current ?? new DuelSettings(); }
    public string ApiUrl, ApiKey, ModelName = "main-model";
    public string AuxiliaryApiUrl, AuxiliaryApiKey, AuxiliaryModelName, AuxiliarySelectedModelOption;
    public string EventAndRebellionApiUrl, EventAndRebellionApiKey, EventAndRebellionModelName, EventSelectedModelOption;
    public bool MainApiThinkingEnabled = true, AuxiliaryApiThinkingEnabled = true, EventAndRebellionApiThinkingEnabled = true;
    public bool UseDetailedSceneSpeechPrompt, PreserveSceneAsteriskActions;
    public int MainTokens = 512, AuxiliaryTokens = 256, EventTokens = 384;
    public int MemoryCompressionDenominator=5, MemoryOverviewStartBlockCount=5, MemoryOverviewTargetChars=200;
    public float MainTemperature = 0.8f, AuxiliaryTemperature = 0.2f, EventTemperature = 0.4f;
    public static string GetEffectiveApiUrl(string url) => url;
    public string GetEffectiveMainModelName() => ModelName;
    public string GetEffectiveAuxiliaryModelName() => AuxiliaryModelName;
    public string GetEffectiveEventAndRebellionModelName() => EventAndRebellionModelName;
    public string GetAuxiliarySelectedModelOption() => AuxiliarySelectedModelOption;
    public string GetEventAndRebellionSelectedModelOption() => EventSelectedModelOption;
    public int GetMainApiMaxTokens() => MainTokens;
    public int GetEventAndRebellionApiMaxTokens() => EventTokens;
    public float GetMainApiTemperature() => MainTemperature;
    public float GetEventAndRebellionApiTemperature() => EventTemperature;
    public string GetMainApiReasoningEffort() => "high";
    public string GetAuxiliaryApiReasoningEffort() => "low";
    public string GetEventAndRebellionApiReasoningEffort() => "high";


public int GetAuxiliaryApiMaxTokens() => AuxiliaryTokens;

public float GetAuxiliaryApiTemperature() => AuxiliaryTemperature;

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
    public static bool IsNonContentStreamChunk(JObject response) => string.IsNullOrEmpty(ExtractStreamDeltaText(response));
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

    public static string ExtractStreamDeltaText(JObject response)
    {
        if (response == null)
        {
            return string.Empty;
        }
        return response.SelectToken("choices[0].delta.content")?.ToString()
            ?? response.SelectToken("choices[0].message.content")?.ToString()
            ?? response.SelectToken("delta.content")?.ToString()
            ?? response.SelectToken("content")?.ToString()
            ?? string.Empty;
    }

    public static string ExtractStreamReasoningText(JObject response)
    {
        return response?.SelectToken("choices[0].delta.reasoning_content")?.ToString()
            ?? response?.SelectToken("choices[0].delta.reasoning")?.ToString()
            ?? string.Empty;
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

internal sealed class MyBehavior
{
    internal sealed class ApiCallResult
    {
        public bool Success;
        public string Content, ErrorMessage, ResponseBody;
        public int? StatusCode, RetryAfterSeconds;
        public bool IsRateLimit, IsRequestsPerMinuteLimit, IsQuotaLimit;
    }
}

internal static class RebellionNamingOwner { internal const int TimeoutMs = 60000; }
internal static class LlmRetryPrompt
{
    public static string BuildFailureDetail(string detail, string content, string body = "") => detail + content + body;
    public static int Popups;
    public static void ShowFailurePopup(string title, string detail) { Popups++; }
}
internal static class Logger
{
    public static int TokenStats;
    public static readonly List<string> Diagnostics = new List<string>();
    public static void Log(string category, string text) { Diagnostics.Add(category + ":" + text); }
    public static void LogEvent(string category, string text) { }
    public static int EstimateTokens(string text) => (text ?? "").Length;
    public static int EstimateTokensFromMessages(JArray messages) => messages.Count;
    public static void RecordTokenStats(int input, int output, JArray messages, string detail, string source, string request) { TokenStats++; }
}

// Controlled lower-network callbacks only. Current application/gateway wrappers execute unchanged.
internal static class ShoutNetwork
{
    internal static Func<List<object>, CancellationToken, Task<string>> NonStream;
    internal static Func<List<object>, Action<string>, Action<string>, Action<string>, CancellationToken, Task> Stream;
    internal static Task<string> CallApiWithMessages(List<object> messages, int maxTokens, bool recordTokenStats,
        int? overrideMaxTokens, bool forceDisableThinking, bool promptRetryOnError, CancellationToken cancellationToken, float? overrideTemperature)
    {
        if (maxTokens != 5000 || promptRetryOnError) throw new InvalidOperationException("Native attempt flags changed");
        return NonStream(messages, cancellationToken);
    }
    internal static Task CallApiWithMessagesStream(List<object> messages, int maxTokens, Action<string> chunk,
        Action<string> complete, Action<string> error, CancellationToken token, bool promptRetryOnError)
    {
        if (maxTokens != 5000 || promptRetryOnError) throw new InvalidOperationException("Native stream flags changed");
        return Stream(messages, chunk, complete, error, token);
    }
}
internal static class FreezeWatchdog
{
    internal static void Mark(string name, string details, bool immediate = false) { }
}
