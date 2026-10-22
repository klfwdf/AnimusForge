using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal static class ConversationContextSettingsCapture
{
    internal static ConversationContextOptions Capture()
    {
        var settings = DuelSettings.GetSettings();
        return new ConversationContextOptions {
            Enabled = settings?.ConversationContextEnabled ?? true,
            RecentTurns = Math.Max(2, Math.Min(50, settings?.ConversationContextRecentTurns ?? 12)),
            RecentChars = Math.Max(2000, Math.Min(100000, settings?.ConversationContextRecentChars ?? 20000)),
            InputChars = Math.Max(20000, Math.Min(1000000, settings?.ConversationContextInputChars ?? 100000)),
            ContextTokens = Math.Max(16000, Math.Min(1000000, settings?.ConversationContextEstimatedTokens ?? 128000))
        };
    }
    internal static string PreparePrimaryJson(string apiUrl, JObject original)
    {
        JObject payload = LlmApiCompat.PrepareChatRequestPayload(apiUrl, original);
        var options = Capture();
        if (options.Enabled)
        {
            var parts = new List<PromptCacheMessage>();
            Action<string, JToken> append = (role, value) => {
                if (value == null) return;
                if (value.Type == JTokenType.String) parts.Add(new PromptCacheMessage(role, value.Value<string>()));
                else if (value is JArray array)
                    foreach (JToken block in array)
                    {
                        string text = block.Type == JTokenType.String ? block.Value<string>() : block["text"]?.Value<string>();
                        if (text != null) parts.Add(new PromptCacheMessage(role, text));
                        else parts.Add(new PromptCacheMessage(role, block.ToString(Formatting.None)));
                    }
                else parts.Add(new PromptCacheMessage(role, value.ToString(Formatting.None)));
            };
            append("system", payload["system"]);
            foreach (JToken message in payload["messages"] as JArray ?? new JArray())
                append(message["role"]?.Value<string>() ?? "user", message["content"]);
            int output = payload["max_tokens"]?.Value<int>() ?? 0;
            long characters = ConversationRequestBudget.Characters(parts), tokens = ConversationRequestBudget.Estimate(parts);
            bool fits = characters <= options.InputChars && tokens + Math.Max(0, output) <= options.ContextTokens;
            // Local diagnostic/cache targets never remove selected history or block sending.
            Logger.Log("ConversationBudget", "mode=" + (fits ? "within_target" : "continue_over_target")
                + " wire_chars=" + characters
                + " estimated_input_tokens=" + tokens + " reserved_output_tokens=" + output
                + " target_chars=" + options.InputChars + " target_context_tokens=" + options.ContextTokens);
        }
        return payload.ToString(Formatting.None);
    }
}
