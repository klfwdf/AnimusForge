using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge.ReplyTool;

internal sealed class ReplyStreamState
{
    private readonly ReplyStringDecoder _decoder = new ReplyStringDecoder();
    private readonly StringBuilder _pending = new StringBuilder();
    private string _name = "", _id;
    private int? _index;
    private bool _plainPublished, _terminal;
    internal bool UsedTool => _index.HasValue;
    internal string Transform(string data)
    {
        if (data == "[DONE]") { Finish(); _terminal = true; return data; }
        var body = JObject.Parse(data);
        if (body["error"] != null) throw new InvalidDataException("reply_tool_provider_stream_error");
        if (!(body["choices"] is JArray choices)) return data;
        if (choices.Count > 1) throw new InvalidDataException("reply_tool_multiple_choices");
        foreach (JObject choice in choices)
        {
            var delta = choice["delta"] as JObject;
            if (delta?["tool_calls"] is JArray calls && calls.Count > 0)
            {
                if (_terminal || _plainPublished) throw new InvalidDataException("reply_tool_mixed_output");
                var text = new StringBuilder();
                foreach (JObject call in calls)
                {
                    if (call["index"]?.Type != JTokenType.Integer) throw new InvalidDataException("reply_tool_missing_index");
                    int index = (int)call["index"];
                    if (index < 0 || _index.HasValue && _index != index) throw new InvalidDataException("reply_tool_multiple_calls");
                    _index = index;
                    string id = (string)call["id"];
                    if (!string.IsNullOrEmpty(id))
                    {
                        if (_id != null && _id != id) throw new InvalidDataException("reply_tool_id_changed");
                        _id = id;
                    }
                    var function = call["function"] as JObject;
                    string name = (string)function?["name"];
                    if (!string.IsNullOrEmpty(name))
                    {
                        if (name != _name || _name != ReplyProtocol.ToolName) _name += name;
                        if (!ReplyProtocol.ToolName.StartsWith(_name, StringComparison.Ordinal)) throw new InvalidDataException("reply_tool_unexpected_name");
                    }
                    var args = function?["arguments"];
                    if (args != null && args.Type != JTokenType.String) throw new InvalidDataException("reply_tool_invalid_arguments");
                    if (_name == ReplyProtocol.ToolName)
                    {
                        if (_pending.Length > 0) { text.Append(_decoder.Push(_pending.ToString())); _pending.Clear(); }
                        if (args != null) text.Append(_decoder.Push((string)args));
                    }
                    else if (args != null) _pending.Append((string)args);
                }
                delta.Remove("tool_calls"); delta["content"] = text.ToString();
            }
            else if (delta != null)
            {
                if (UsedTool) delta.Remove("content");
                else if (!string.IsNullOrEmpty(delta["content"]?.ToString())) _plainPublished = true;
            }
            string finish = (string)choice["finish_reason"];
            if (UsedTool && (!string.IsNullOrWhiteSpace(delta?["refusal"]?.ToString()) || finish == "content_filter" || finish == "length"))
                throw new InvalidDataException("reply_tool_incomplete_or_refused");
            if (!string.IsNullOrEmpty(finish))
            {
                Finish(); _terminal = true;
                if (UsedTool && finish == "tool_calls") choice["finish_reason"] = "stop";
            }
        }
        return body.ToString(Formatting.None);
    }
    private void Finish()
    {
        if (!UsedTool) return;
        if (_name != ReplyProtocol.ToolName || _pending.Length != 0) throw new InvalidDataException("reply_tool_incomplete_name");
        _decoder.Finish();
    }
    internal void EndOfStream()
    {
        Finish();
        if (UsedTool && !_terminal) throw new InvalidDataException("reply_tool_stream_interrupted");
    }
}
