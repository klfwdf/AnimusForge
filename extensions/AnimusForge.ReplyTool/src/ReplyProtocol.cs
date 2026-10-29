using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge.ReplyTool;

internal sealed class ReplyPrompt
{
    internal readonly string Instruction, Description, ContentDescription;
    internal ReplyPrompt(string json)
    {
        var value = JObject.Parse(json);
        Instruction = Required(value, "instruction"); Description = Required(value, "description");
        ContentDescription = Required(value, "contentDescription");
    }
    private static string Required(JObject value, string key)
    {
        if (value[key]?.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)value[key]))
            throw new InvalidDataException("invalid_prompt_resource:" + key);
        return (string)value[key];
    }
}

internal static class ReplyProtocol
{
    internal const string ToolName = "output_reply";
    internal static bool TryPrepare(Uri endpoint, string json, ReplyPrompt prompt, out string prepared, out bool streaming)
    {
        prepared = json; streaming = false;
        if (endpoint == null || !endpoint.AbsolutePath.TrimEnd('/').EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)) return false;
        JObject body;
        try { body = JObject.Parse(json); } catch (JsonException) { return false; }
        string model = body["model"]?.Type == JTokenType.String ? (string)body["model"] : "";
        if (model.IndexOf("gemini", StringComparison.OrdinalIgnoreCase) < 0
            || !(body["messages"] is JArray messages) || messages.Count == 0
            || body["tools"] != null || body["functions"] != null || body["tool_choice"] != null
            || body["function_call"] != null || body["response_format"] != null
            || body["n"] != null && body["n"].ToString() != "1") return false;
        streaming = body["stream"]?.Type == JTokenType.Boolean && (bool)body["stream"];
        messages.Insert(0, new JObject { ["role"] = "system", ["content"] = prompt.Instruction });
        body["tools"] = new JArray(new JObject
        {
            ["type"] = "function", ["function"] = new JObject
            {
                ["name"] = ToolName, ["description"] = prompt.Description,
                ["parameters"] = new JObject
                {
                    ["type"] = "object", ["properties"] = new JObject { ["content"] = new JObject
                    { ["type"] = "string", ["description"] = prompt.ContentDescription } },
                    ["required"] = new JArray("content"), ["additionalProperties"] = false
                }
            }
        });
        body["tool_choice"] = new JObject { ["type"] = "function", ["function"] = new JObject { ["name"] = ToolName } };
        body["parallel_tool_calls"] = false;
        prepared = body.ToString(Formatting.None); return true;
    }

    internal static string Restore(string json)
    {
        var body = JObject.Parse(json);
        if (!(body["choices"] is JArray choices)) return json;
        foreach (var choice in choices.OfType<JObject>())
        {
            var message = choice["message"] as JObject;
            if (!(message?["tool_calls"] is JArray calls) || calls.Count == 0) continue;
            if (choice["finish_reason"]?.ToString() == "content_filter" || !string.IsNullOrWhiteSpace(message["refusal"]?.ToString()))
                throw new InvalidDataException("reply_tool_provider_refusal");
            if (calls.Count != 1 || calls[0]?["function"]?["name"]?.ToString() != ToolName)
                throw new InvalidDataException("reply_tool_unexpected_calls");
            if (choice["finish_reason"]?.ToString() == "length") throw new InvalidDataException("reply_tool_output_limit");
            var arguments = calls[0]["function"]["arguments"];
            if (arguments?.Type != JTokenType.String) throw new InvalidDataException("reply_tool_invalid_arguments");
            var decoder = new ReplyStringDecoder();
            string text = decoder.Push((string)arguments); decoder.Finish();
            message["content"] = text; message.Remove("tool_calls");
            if (choice["finish_reason"]?.ToString() == "tool_calls") choice["finish_reason"] = "stop";
        }
        return body.ToString(Formatting.None);
    }
}
