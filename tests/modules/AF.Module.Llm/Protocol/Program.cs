using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using AnimusForge;
using Newtonsoft.Json.Linq;

internal static class Program
{
    private static int Main(string[] args)
    {
        string only = args.Length == 2 && args[0] == "--only" ? args[1] : null;
        if (args.Length != 0 && only == null)
        {
            Console.Error.WriteLine("Invalid test selector");
            return 2;
        }
        var cases = new (string Name, Action Test)[]
        {
            ("message-shapes", MessageShapes),
            ("tail-required", TailRequired),
            ("input-list-unchanged", InputListUnchanged),
            ("retry-marker-detected", RetryMarkerDetected),
            ("thinking-needs-both", ThinkingNeedsBoth),
            ("anthropic-empty-user", AnthropicEmptyUser),
            ("request-urls-and-payload", RequestUrlsAndPayload),
            ("authentication-headers", AuthenticationHeaders),
            ("response-text", ResponseText),
            ("visible-envelope", VisibleEnvelope),
            ("stream-no-duplicate", StreamNoDuplicate),
            ("stream-existing-unicode-boundary", StreamExistingUnicodeBoundary),
            ("stream-isolation", StreamIsolation),
        };
        if (only != null && !cases.Any(item => item.Name == only))
        {
            Console.Error.WriteLine("Unknown test selector");
            return 2;
        }
        try
        {
            foreach (var item in cases)
            {
                if (only != null && item.Name != only)
                {
                    continue;
                }
                item.Test();
                Console.WriteLine("PASS:" + item.Name);
            }
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error.Message);
            return 1;
        }
    }

    private static void Check(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException("FAIL:" + name);
        }
    }

    private static Dictionary<string, object> Message(string role, string content) =>
        new Dictionary<string, object> { ["role"] = role, ["content"] = content };

    private sealed class UpperMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }

    private sealed class ThrowingMessage
    {
        public string Role => throw new InvalidOperationException("synthetic getter failure");
        public string Content => "not visible";
    }

    private sealed class PartiallyThrowingMessage
    {
        public string Role => "user";
        public string Content => throw new InvalidOperationException("synthetic getter failure");
    }

    private static void MessageShapes()
    {
        Check(!PrimaryChatMessagePolicy.TryReadMessage(null, out _, out _), "message-shapes");
        Check(PrimaryChatMessagePolicy.TryReadMessage(JObject.Parse("{\"role\":\"user\",\"content\":\"json\"}"), out string jsonRole, out string jsonText)
            && jsonRole == "user" && jsonText == "json", "message-shapes");
        Check(PrimaryChatMessagePolicy.TryReadMessage(Message("assistant", "dict"), out string dictRole, out string dictText)
            && dictRole == "assistant" && dictText == "dict", "message-shapes");
        Check(PrimaryChatMessagePolicy.TryReadMessage(new UpperMessage { Role = "system", Content = "upper" }, out string upperRole, out string upperText)
            && upperRole == "system" && upperText == "upper", "message-shapes");
        Check(PrimaryChatMessagePolicy.TryReadMessage(new { role = "user", content = "lower" }, out string lowerRole, out string lowerText)
            && lowerRole == "user" && lowerText == "lower", "message-shapes");
        Check(PrimaryChatMessagePolicy.TryReadMessage(new Dictionary<string, object> { ["Role"] = "user", ["Content"] = "upper keys" }, out string missingRole, out string missingText)
            && missingRole == "" && missingText == "", "message-shapes");
        Check(!PrimaryChatMessagePolicy.TryReadMessage(new ThrowingMessage(), out _, out _), "message-shapes");
        Check(!PrimaryChatMessagePolicy.TryReadMessage(new PartiallyThrowingMessage(), out string partialRole, out _)
            && partialRole == "user", "message-shapes");
        Check(PrimaryChatMessagePolicy.GetLastMessageRole(Array.Empty<object>(), out string emptyContent) == ""
            && emptyContent == "", "message-shapes");
        Check(PrimaryChatMessagePolicy.GetLastMessageRole(new object[] { null, new object(), Message("user", "one"), Message("assistant", "two") }, out string lastText) == "assistant"
            && lastText == "two", "message-shapes");
    }

    private static void TailRequired()
    {
        var empty = PrimaryChatMessagePolicy.EnsureFinalUserTurn(null, out string emptyRole);
        Check(emptyRole == "" && empty.Count == 1
            && PrimaryChatMessagePolicy.TryReadMessage(empty[0], out string role, out string content)
            && role == "user" && content == "请继续完成当前请求，只输出最终结果。", "tail-required");
        var system = PrimaryChatMessagePolicy.EnsureFinalUserTurn(new object[] { Message("system", "instruction") }, out string originalRole);
        Check(originalRole == "system" && system.Count == 2
            && PrimaryChatMessagePolicy.GetLastMessageRole(system, out string continuation) == "user"
            && continuation == "请继续完成当前请求，只输出最终结果。", "tail-required");
        var existing = PrimaryChatMessagePolicy.EnsureFinalUserTurn(new object[] { Message("user", "request") }, out string userRole);
        Check(userRole == "user" && existing.Count == 1, "tail-required");
        var blank = PrimaryChatMessagePolicy.EnsureFinalUserTurn(new object[] { Message("user", " \n") }, out string blankRole);
        Check(blankRole == "user" && blank.Count == 2, "tail-required");
        var battle = PrimaryChatMessagePolicy.EnsureFinalUserTurn(new object[] { Message("system", "SPEECH_BEGIN") }, out _);
        Check(PrimaryChatMessagePolicy.GetLastMessageRole(battle, out string battleTail) == "user"
            && battleTail == "请继续完成当前阵前演讲请求，只输出协议规定的最终结果，不要生成普通NPC回复。", "tail-required");
        var battleChinese = PrimaryChatMessagePolicy.EnsureFinalUserTurn(new object[] { Message("system", "【阵前演讲") }, out _);
        Check(PrimaryChatMessagePolicy.GetLastMessageRole(battleChinese, out string chineseTail) == "user"
            && chineseTail == battleTail, "tail-required");
    }

    private static void InputListUnchanged()
    {
        var message = Message("assistant", "original");
        var input = new List<object> { message };
        var result = PrimaryChatMessagePolicy.EnsureFinalUserTurn(input, out _);
        Check(input.Count == 1 && ReferenceEquals(input[0], message) && (string)message["content"] == "original"
            && result.Count == 2 && !ReferenceEquals(input, result), "input-list-unchanged");
        var retried = PrimaryChatMessagePolicy.BuildEmptyResponseRetryMessages(input);
        Check(input.Count == 1 && (string)message["content"] == "original" && retried.Count >= 2, "input-list-unchanged");
    }

    private static void RetryMarkerDetected()
    {
        var original = new List<object> { Message("system", "system"), Message("assistant", "reply") };
        Check(!PrimaryChatMessagePolicy.HasEmptyResponseRetryMarker(original), "retry-marker-detected");
        var retry = PrimaryChatMessagePolicy.BuildEmptyResponseRetryMessages(original);
        Check(retry.Count == 3 && PrimaryChatMessagePolicy.HasEmptyResponseRetryMarker(retry), "retry-marker-detected");
        Check(PrimaryChatMessagePolicy.TryReadMessage(retry[0], out string role, out string content)
            && role == "system" && content.StartsWith("system\n\n", StringComparison.Ordinal)
            && content.Contains("[AF_EMPTY_RESPONSE_RETRY]", StringComparison.Ordinal), "retry-marker-detected");
        Check(ReferenceEquals(retry[1], original[1]) && original.Count == 2
            && PrimaryChatMessagePolicy.GetLastMessageRole(retry, out _) == "user", "retry-marker-detected");
        var withoutSystem = PrimaryChatMessagePolicy.BuildEmptyResponseRetryMessages(new List<object> { Message("user", "question") });
        Check(withoutSystem.Count == 2 && PrimaryChatMessagePolicy.HasEmptyResponseRetryMarker(withoutSystem)
            && PrimaryChatMessagePolicy.GetLastMessageRole(withoutSystem, out _) == "user", "retry-marker-detected");
        Check(!PrimaryChatMessagePolicy.HasEmptyResponseRetryMarker(new List<object> { Message("system", "[af_empty_response_retry]") }), "retry-marker-detected");
        var nullRetry = PrimaryChatMessagePolicy.BuildEmptyResponseRetryMessages(null);
        Check(nullRetry.Count == 2 && PrimaryChatMessagePolicy.HasEmptyResponseRetryMarker(nullRetry)
            && PrimaryChatMessagePolicy.GetLastMessageRole(nullRetry, out string nullTail) == "user"
            && nullTail == "请继续完成当前请求，只输出最终结果。", "retry-marker-detected");
    }

    private static void ThinkingNeedsBoth()
    {
        Check(!PrimaryChatMessagePolicy.LooksLikeThinkingControlError(null), "thinking-needs-both");
        Check(!PrimaryChatMessagePolicy.LooksLikeThinkingControlError("ordinary bad request"), "thinking-needs-both");
        Check(!PrimaryChatMessagePolicy.LooksLikeThinkingControlError("thinking"), "thinking-needs-both");
        Check(!PrimaryChatMessagePolicy.LooksLikeThinkingControlError("unsupported"), "thinking-needs-both");
        Check(PrimaryChatMessagePolicy.LooksLikeThinkingControlError("REASONING_EFFORT is NOT SUPPORTED"), "thinking-needs-both");
    }

    private static void AnthropicEmptyUser()
    {
        JObject source = JObject.Parse("{\"model\":\"synthetic\",\"messages\":[{\"role\":\"system\",\"content\":\"rule\"}]}");
        JObject converted = LlmApiCompat.PrepareChatRequestPayload("https://api.anthropic.com/v1/messages", source);
        Check((string)converted["system"] == "rule"
            && converted["messages"] is JArray messages && messages.Count == 1
            && (string)messages[0]["role"] == "user" && (string)messages[0]["content"] == " ", "anthropic-empty-user");
        Check(source["messages"] is JArray original && original.Count == 1, "anthropic-empty-user");
    }

    private static void RequestUrlsAndPayload()
    {
        Check(LlmApiCompat.GetEffectiveChatApiUrl("https://example.invalid/v1") == "https://example.invalid/v1/chat/completions", "request-urls-and-payload");
        Check(LlmApiCompat.GetEffectiveChatApiUrl("https://api.anthropic.com/v1") == "https://api.anthropic.com/v1/messages", "request-urls-and-payload");
        Check(LlmApiCompat.BuildModelListApiUrl("https://example.invalid/v1/chat/completions") == "https://example.invalid/v1/models", "request-urls-and-payload");
        JObject original = JObject.Parse("{\"model\":\"synthetic\",\"max_tokens\":4096,\"thinking\":{\"type\":\"enabled\"},\"messages\":[{\"role\":\"system\",\"content\":\"one\"},{\"role\":\"developer\",\"content\":\"two\"},{\"role\":\"user\",\"content\":\"a\"},{\"role\":\"user\",\"content\":\"b\"},{\"role\":\"assistant\",\"content\":\"done\"}]}");
        string snapshot = original.ToString();
        JObject clone = LlmApiCompat.PrepareChatRequestPayload("https://example.invalid/v1", original);
        Check(!ReferenceEquals(clone, original) && JToken.DeepEquals(clone, original), "request-urls-and-payload");
        JObject anthropic = LlmApiCompat.PrepareChatRequestPayload("https://api.anthropic.com/v1/messages", original);
        Check(original.ToString() == snapshot && (string)anthropic["system"] == "one\n\ntwo"
            && (int)anthropic["max_tokens"] == 4096 && anthropic["messages"] is JArray messages && messages.Count == 2
            && (string)messages[0]["role"] == "user" && (string)messages[0]["content"] == "a\n\nb"
            && (string)messages[1]["role"] == "assistant" && anthropic["thinking"] != null, "request-urls-and-payload");
        Check(LlmApiCompat.PrepareChatRequestJson("https://api.anthropic.com/v1/messages", null) == "{}", "request-urls-and-payload");
    }

    private static void AuthenticationHeaders()
    {
        const string key = "synthetic-only-key";
        using var official = new HttpRequestMessage();
        LlmApiCompat.ApplyAuthenticationHeaders(official, "https://api.anthropic.com/v1/messages", key);
        Check(official.Headers.Contains("x-api-key") && official.Headers.Contains("anthropic-version")
            && official.Headers.GetValues("x-api-key").Single() == key
            && official.Headers.GetValues("anthropic-version").Single() == "2023-06-01"
            && official.Headers.Authorization == null, "authentication-headers");
        LlmApiCompat.ApplyAuthenticationHeaders(official, "https://api.anthropic.com/v1/messages", key);
        Check(official.Headers.GetValues("x-api-key").Count() == 1, "authentication-headers");
        using var proxy = new HttpRequestMessage();
        LlmApiCompat.ApplyAuthenticationHeaders(proxy, "https://proxy.invalid/anthropic/v1/messages", key);
        Check(proxy.Headers.GetValues("x-api-key").Single() == key
            && proxy.Headers.GetValues("Authorization").Single() == "Bearer " + key, "authentication-headers");
        using var openAi = new HttpRequestMessage();
        LlmApiCompat.ApplyAuthenticationHeaders(openAi, "https://example.invalid/v1/chat/completions", key);
        Check(openAi.Headers.Authorization?.Scheme == "Bearer" && openAi.Headers.Authorization.Parameter == key
            && !openAi.Headers.Contains("x-api-key"), "authentication-headers");
        LlmApiCompat.ApplyAuthenticationHeaders(openAi, "https://example.invalid/v1/chat/completions", "synthetic-replacement");
        Check(openAi.Headers.Authorization?.Parameter == "synthetic-replacement", "authentication-headers");
    }

    private static void ResponseText()
    {
        Check(LlmApiCompat.ExtractAssistantText("{\"choices\":[{\"message\":{\"content\":\"answer\"}}]}") == "answer", "response-text");
        Check(LlmApiCompat.ExtractAssistantText("{\"content\":[{\"type\":\"text\",\"text\":\"anthropic\"}]}") == "anthropic", "response-text");
        Check(LlmApiCompat.ExtractAssistantText("{\"output\":[{\"content\":[{\"type\":\"output_text\",\"text\":\"response\"}]}]}") == "response", "response-text");
        Check(LlmApiCompat.ExtractAssistantText("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"gemini\"}]}}]}") == "geminigemini", "response-text");
        Check(LlmApiCompat.ExtractAssistantText("bad json") == "" && LlmApiCompat.ExtractAssistantText((string)null) == "", "response-text");
        Check(LlmApiCompat.ExtractStreamDeltaText(JObject.Parse("{\"choices\":[{\"delta\":{\"content\":\"chunk\"}}]}")) == "chunk", "response-text");
        Check(LlmApiCompat.ExtractStreamReasoningText(JObject.Parse("{\"choices\":[{\"delta\":{\"reasoning_content\":\"thought\"}}]}")) == "thought", "response-text");
        Check(LlmApiCompat.IsNonContentStreamChunk(JObject.Parse("{\"choices\":[{\"delta\":{\"reasoning_content\":\"thought\"}}]}")), "response-text");
        Check(LlmApiCompat.IsNonContentStreamChunk(JObject.Parse("{\"choices\":[]}"))
            && LlmApiCompat.IsNonContentStreamChunk(JObject.Parse("{\"choices\":[{\"finish_reason\":\"stop\"}]}"))
            && LlmApiCompat.IsNonContentStreamChunk(JObject.Parse("{\"type\":\"message_start\"}"))
            && LlmApiCompat.IsNonContentStreamChunk(JObject.Parse("{\"usage\":{\"total_tokens\":2}}")), "response-text");
        Check(!LlmApiCompat.IsNonContentStreamChunk(JObject.Parse("{\"choices\":[{\"delta\":{\"content\":\"chunk\"}}]}")), "response-text");
        Check(LlmApiCompat.IsReasoningOnlyTokenLimitResponse("{\"choices\":[{\"finish_reason\":\"length\",\"message\":{\"reasoning_content\":\"thought\"}}],\"usage\":{\"completion_tokens\":10,\"completion_tokens_details\":{\"reasoning_tokens\":10}}}", out int completion, out int reasoning)
            && completion == 10 && reasoning == 10, "response-text");
    }

    private static void VisibleEnvelope()
    {
        Check(LlmVisibleReplyNormalizer.NormalizeComplete("{\"response\":\"hello\"}") == "hello", "visible-envelope");
        Check(LlmVisibleReplyNormalizer.NormalizeComplete("```json\n{\"reply\":\"hello\"}\n```") == "hello", "visible-envelope");
        Check(LlmVisibleReplyNormalizer.NormalizeComplete("ordinary text") == "ordinary text", "visible-envelope");
        Check(LlmVisibleReplyNormalizer.NormalizeComplete("{\"response\":{\"reply\":\"nested\"}}") == "nested", "visible-envelope");
        Check(LlmVisibleReplyNormalizer.NormalizeComplete("{\"reply\":[\"one\",\"two\"]}") == "one\ntwo", "visible-envelope");
        Check(LlmVisibleReplyNormalizer.NormalizeComplete("{\"unknown\":\"value\"}") == "{\"unknown\":\"value\"}", "visible-envelope");
        Check(LlmVisibleReplyNormalizer.NormalizeComplete("{\"reply\":\"a\\u4F60\\nline\\\"quote\"}") == "a你\nline\"quote", "visible-envelope");
        Check(LlmVisibleReplyNormalizer.NormalizeComplete("{bad json") == "{bad json", "visible-envelope");
        Check(LlmVisibleReplyNormalizer.NormalizeComplete(null) == "", "visible-envelope");
    }

    private static void StreamNoDuplicate()
    {
        var filter = new LlmVisibleReplyNormalizer.StreamFilter();
        string emitted = filter.Push("{\"reply\":\"he") + filter.Push("llo\"}") + filter.Complete("{\"reply\":\"hello\"}");
        Check(emitted == "hello" && filter.NormalizedText == "hello", "stream-no-duplicate");
        var withNullFinal = new LlmVisibleReplyNormalizer.StreamFilter();
        string second = withNullFinal.Push("{\"response\":\"x") + withNullFinal.Push("y\"}") + withNullFinal.Complete(null);
        Check(second == "xy", "stream-no-duplicate");
    }

    private static void StreamExistingUnicodeBoundary()
    {
        const string escaped = "{\"reply\":\"a\\u4F60\\nq\"}";
        var characterSlices = new LlmVisibleReplyNormalizer.StreamFilter();
        string characterOutput = "";
        foreach (char character in escaped)
        {
            characterOutput += characterSlices.Push(character.ToString());
        }
        characterOutput += characterSlices.Complete(escaped);
        // Existing preview behavior emits partial escape digits before Complete repairs NormalizedText.
        Check(characterOutput == "a4F6" && characterSlices.NormalizedText == "a你\nq", "stream-existing-unicode-boundary");
    }

    private static void StreamIsolation()
    {
        var first = new LlmVisibleReplyNormalizer.StreamFilter();
        var second = new LlmVisibleReplyNormalizer.StreamFilter();
        string left = first.Push("{\"reply\":\"le");
        string right = second.Push("{\"reply\":\"ri");
        left += first.Push("ft\"}");
        right += second.Push("ght\"}");
        left += first.Complete("{\"reply\":\"left\"}");
        right += second.Complete("{\"reply\":\"right\"}");
        Check(left == "left" && right == "right" && first.NormalizedText == "left" && second.NormalizedText == "right", "stream-isolation");
        var plain = new LlmVisibleReplyNormalizer.StreamFilter();
        Check(plain.Push("plain") == "plain" && plain.Push(" text") == " text", "stream-isolation");
        Check(plain.Complete(null) == "" && plain.NormalizedText == "", "stream-isolation");
    }
}
