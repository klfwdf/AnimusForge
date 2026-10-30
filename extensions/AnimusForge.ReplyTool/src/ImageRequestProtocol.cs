using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge.ReplyTool;

internal static class ImageRequestProtocol
{
    // This protocol is used only at Illustrator's image-generation HTTP call site.
    // Image bytes stay in the provider's native image fields, never output_reply.
    internal static bool TryPrepare(Uri endpoint, string json, ReplyPrompt prompt, out string prepared)
    {
        prepared = json;
        if (endpoint == null || !endpoint.AbsolutePath.TrimEnd('/').EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)) return false;
        JObject body;
        try { body = JObject.Parse(json); } catch (JsonException) { return false; }
        if (body["model"]?.Type != JTokenType.String || ((string)body["model"]).IndexOf("gemini", StringComparison.OrdinalIgnoreCase) < 0
            || !(body["messages"] is JArray messages) || messages.Count == 0
            || body["tools"] != null || body["functions"] != null || body["tool_choice"] != null
            || body["function_call"] != null || body["response_format"] != null
            || body["stream"] != null && (body["stream"].Type != JTokenType.Boolean || (bool)body["stream"])
            || body["n"] != null && body["n"].ToString() != "1") return false;
        if (!InputWrapping.WrapLatest(messages, prompt.ImageInputInstruction, preserveTextPositions: true)) return false;
        body["tools"] = new JArray(new JObject
        {
            ["type"] = "function", ["function"] = new JObject
            {
                ["name"] = InputWrapping.ToolName, ["description"] = prompt.InputDescription,
                ["parameters"] = new JObject { ["type"] = "object", ["properties"] = new JObject(), ["additionalProperties"] = false }
            }
        });
        // The input call is already complete. Do not request a new function call
        // from an image model, or ask it to produce textual base64.
        body["tool_choice"] = "none";
        prepared = body.ToString(Formatting.None);
        return true;
    }
}
