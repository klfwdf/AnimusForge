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
        // Image APIs may reject any custom function declaration, even with
        // tool_choice:none. Keep only the completed input history; there is no
        // callable tool or output function to translate to the image backend.
        prepared = body.ToString(Formatting.None);
        return prepared != json;
    }
}
