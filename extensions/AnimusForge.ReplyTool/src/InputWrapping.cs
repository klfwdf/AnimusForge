using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge.ReplyTool;

internal static class InputWrapping
{
    internal const string ToolName = "read_user_input";

    // Only the final user turn in the detached outgoing payload is changed.
    // Earlier turns, system messages, images and AF's stored history stay intact.
    internal static bool WrapLatest(JArray messages, string instruction, bool preserveTextPositions = false)
    {
        int index = messages.Count - 1;
        while (index >= 0 && messages[index]?["role"]?.ToString() != "user") index--;
        if (index < 0) return false;
        if (HasPair(messages, index)) return true;
        if (index + 1 < messages.Count && messages[index + 1]?["tool_calls"] is JArray existing)
            foreach (var call in existing)
                if (call["function"]?["name"]?.ToString() == ToolName)
                    throw new InvalidDataException("reply_tool_invalid_input_pair");

        var user = (JObject)messages[index];
        string text;
        JArray textParts = null;
        if (user["content"]?.Type == JTokenType.String)
        {
            text = (string)user["content"];
            user["content"] = instruction;
        }
        else if (user["content"] is JArray parts)
        {
            var originalText = new StringBuilder();
            var replacement = new JArray();
            if (preserveTextPositions) textParts = new JArray();
            bool found = false;
            foreach (var part in parts)
            {
                if (part is JObject textPart && textPart["type"]?.ToString() == "text" && textPart["text"]?.Type == JTokenType.String)
                {
                    originalText.Append((string)textPart["text"]);
                    if (preserveTextPositions)
                    {
                        int position = replacement.Count;
                        textParts.Add(new JObject { ["index"] = position, ["text"] = textPart["text"].DeepClone() });
                        var marker = (JObject)textPart.DeepClone();
                        marker["text"] = (found ? "" : instruction + "\n") + "[af_image_text_" + position + "]";
                        replacement.Add(marker);
                    }
                    else if (!found) replacement.Add(new JObject { ["type"] = "text", ["text"] = instruction });
                    found = true;
                }
                else replacement.Add(part.DeepClone());
            }
            if (!found) return false;
            text = originalText.ToString(); user["content"] = replacement;
        }
        else return false;

        string id = "call_af_input_" + Guid.NewGuid().ToString("N");
        var callMessage = new JObject
        {
            ["role"] = "assistant", ["content"] = null,
            ["tool_calls"] = new JArray(new JObject
            {
                ["id"] = id, ["type"] = "function",
                ["function"] = new JObject { ["name"] = ToolName, ["arguments"] = "{}" }
            })
        };
        var result = new JObject { ["source"] = "user_input", ["text"] = text };
        if (textParts != null) result["text_parts"] = textParts;
        var resultMessage = new JObject
        {
            ["role"] = "tool", ["tool_call_id"] = id,
            ["content"] = result.ToString(Formatting.None)
        };
        messages.Insert(index + 1, callMessage); messages.Insert(index + 2, resultMessage);
        return true;
    }

    private static bool HasPair(JArray messages, int userIndex)
    {
        if (userIndex + 2 >= messages.Count) return false;
        var assistant = messages[userIndex + 1] as JObject;
        var result = messages[userIndex + 2] as JObject;
        if (assistant?["role"]?.ToString() != "assistant" || result?["role"]?.ToString() != "tool"
            || !(assistant["tool_calls"] is JArray calls) || calls.Count != 1
            || calls[0]?["function"]?["name"]?.ToString() != ToolName
            || calls[0]?["type"]?.ToString() != "function"
            || calls[0]?["id"]?.Type != JTokenType.String || string.IsNullOrEmpty((string)calls[0]["id"])
            || result["tool_call_id"]?.Type != JTokenType.String
            || (string)result["tool_call_id"] != (string)calls[0]["id"] || result["content"]?.Type != JTokenType.String)
            return false;
        try
        {
            var payload = JObject.Parse((string)result["content"]);
            return payload["source"]?.Type == JTokenType.String && (string)payload["source"] == "user_input"
                && payload["text"]?.Type == JTokenType.String;
        }
        catch (JsonException) { return false; }
    }
}
