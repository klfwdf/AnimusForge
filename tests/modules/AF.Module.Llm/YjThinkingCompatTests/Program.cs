using System;
using System.Net.Http;
using AnimusForge;
using Newtonsoft.Json.Linq;

static void Check(bool value, string message)
{
    if (!value) throw new InvalidOperationException(message);
}

string[] urls = {
    "https://asia.shenlanqaq.com/v1",
    "https://yjapi.shenlanqaq.com/v1",
    "https://www.shenlanqaq.com/v1/chat/completions",
    "https://WWW.SHENLANQAQ.COM/v1",
    "https://yjapi.manqiaotechnology.com/v1/chat/completions"
};
foreach (string url in urls)
{
    Check(YjThinkingCompat.IsYjGeminiEndpoint(url, "gemini-3.7-flash-high"), "YJ host not recognized: " + url);
    JObject plain = new JObject {
        ["thinking"] = new JObject { ["type"] = "enabled" },
        ["output_config"] = new JObject(), ["reasoning_effort"] = "high"
    };
    Check(YjThinkingCompat.TryApply(plain, url, "gemini-3.7-flash-high", false, "none", out _), "plain not handled");
    Check(plain.Count == 0, "unsupported controls retained");
    JObject enabled = new JObject { ["thinking"] = new JObject(), ["output_config"] = new JObject() };
    Check(YjThinkingCompat.TryApply(enabled, url, "gemini-3.7-flash-high", true, "high", out _), "thinking not handled");
    Check(enabled.Count == 1 && (string)enabled["reasoning_effort"] == "high", "reasoning effort not preserved");
    JObject disabled = new JObject();
    Check(YjThinkingCompat.TryApply(disabled, url, "gemini-other", false, "high", out _)
        && (string)disabled["reasoning_effort"] == "none", "ordinary Gemini disabled behavior changed");
}
foreach (string url in new[] { "https://asia.shenlanqaq.com.evil.test/v1", "https://yjapi.shenlanqaq.com.evil.test/v1", "https://api.openai.com/v1", "https://www.shenlanqaq.com.evil.test/v1", "https://evil.test/www.shenlanqaq.com", "not-a-url", "", null })
{
    JObject payload = new JObject { ["thinking"] = new JObject { ["type"] = "enabled" } };
    string before = payload.ToString();
    Check(!YjThinkingCompat.TryApply(payload, url, "gemini-3.7-flash-high", false, "none", out _), "non-YJ URL accepted");
    Check(payload.ToString() == before, "non-YJ payload mutated");
}
Check(!YjThinkingCompat.IsYjGeminiEndpoint(urls[0], "claude-test"), "non-Gemini accepted");
Check(!YjThinkingCompat.TryApply(null, urls[0], "gemini-test", true, "high", out _), "null payload accepted");
Console.WriteLine("PASS YjThinkingCompat: new/legacy hosts, thinking enabled/disabled, exact-host negative cases, non-Gemini, null payload");

// Inspect actual shared production URL/header/payload helpers with synthetic credentials.
const string key = "synthetic-audit-key";
foreach (string url in new[] { urls[0], "https://api.openai.com/v1/chat/completions" })
{
    using var request = new HttpRequestMessage(HttpMethod.Post, url);
    LlmApiCompat.ApplyAuthenticationHeaders(request, url, "  " + key + "  ");
    Check(request.Headers.Authorization?.Scheme == "Bearer" && request.Headers.Authorization.Parameter == key, "Bearer header mismatch");
    Check(!request.Headers.Contains("x-api-key") && !request.Headers.Contains("anthropic-version"), "OpenAI request gained Anthropic headers");
}
foreach (string url in new[] { "https://api.anthropic.com/v1/messages", "https://relay.example/anthropic/v1/messages" })
{
    using var request = new HttpRequestMessage(HttpMethod.Post, url);
    LlmApiCompat.ApplyAuthenticationHeaders(request, url, key);
    Check(string.Join("", request.Headers.GetValues("x-api-key")) == key, "Anthropic key mismatch");
    Check(string.Join("", request.Headers.GetValues("anthropic-version")) == "2023-06-01", "Anthropic version mismatch");
    Check(request.Headers.Contains("Authorization") == url.Contains("relay.example"), "Anthropic official/relay auth mismatch");
}
Check(LlmApiCompat.GetEffectiveChatApiUrl("https://www.shenlanqaq.com/v1") == "https://www.shenlanqaq.com/v1/chat/completions", "preset chat URL mismatch");
Check(LlmApiCompat.BuildModelListApiUrl("https://www.shenlanqaq.com/v1/chat/completions") == "https://www.shenlanqaq.com/v1/models", "preset model list URL mismatch");
JObject openAi = new JObject {
    ["model"] = "synthetic-model", ["max_tokens"] = 4096, ["temperature"] = 0.8,
    ["messages"] = new JArray { new JObject { ["role"] = "system", ["content"] = "system" },
        new JObject { ["role"] = "user", ["content"] = "hello" } }
};
JObject anthropic = JObject.Parse(LlmApiCompat.PrepareChatRequestJson("https://api.anthropic.com/v1/messages", openAi));
Check((string)anthropic["system"] == "system" && ((JArray)anthropic["messages"]).Count == 1, "Anthropic message conversion mismatch");
Check(!anthropic.ToString().Contains(key), "credential leaked into body");
Console.WriteLine("PASS API audit: shared auth headers, preset chat/models URL, Anthropic payload conversion, synthetic credential boundary");

Check(LlmApiCompat.GetEffectiveChatApiUrl("https://relay.example/v1?route=blue") == "https://relay.example/v1/chat/completions?route=blue", "query must remain outside chat path");
Check(LlmApiCompat.GetEffectiveChatApiUrl("https://relay.example/proxy/?route=blue#ui") == "https://relay.example/proxy/v1/chat/completions?route=blue", "prefix/query/fragment chat URL mismatch");
Check(LlmApiCompat.BuildModelListApiUrl("https://relay.example/proxy/v1/chat/completions") == "https://relay.example/proxy/v1/models", "proxy /v1 must be preserved for model list");
Console.WriteLine("PASS URL regressions: query placement, proxy prefix, fragment removal, /v1 model-list preservation");

// Findings are observations, not PASS assertions for desired behavior.
Console.WriteLine("FINDING query-chat-url: " + LlmApiCompat.GetEffectiveChatApiUrl("https://relay.example/v1?route=blue"));
Console.WriteLine("FINDING prefixed-models-url: " + LlmApiCompat.BuildModelListApiUrl("https://relay.example/proxy/v1/chat/completions"));
JObject handshake = (JObject)openAi.DeepClone();
handshake.Remove("max_tokens");
handshake["thinking"] = new JObject { ["type"] = "enabled" };
JObject handshakeConverted = JObject.Parse(LlmApiCompat.PrepareChatRequestJson("https://api.anthropic.com/v1/messages", handshake));
Console.WriteLine("FINDING handshake-thinking: max_tokens=" + handshakeConverted["max_tokens"] + "; thinking-present=" + (handshakeConverted["thinking"] != null));

namespace AnimusForge
{
    // Only constants are stubbed; the production compatibility implementation is linked unchanged.
    internal static class DuelSettings
    {
        public const string ReasoningEffortNone = "none", ReasoningEffortMinimal = "minimal",
            ReasoningEffortLow = "low", ReasoningEffortMedium = "medium", ReasoningEffortHigh = "high",
            ReasoningEffortXHigh = "xhigh", ReasoningEffortMax = "max";
    }
}
