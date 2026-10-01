using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace AnimusForge;

internal sealed class ExecutionTranscriptLine
{
    public int Sequence { get; set; }
    public string Phase { get; set; }
    public string Speaker { get; set; }
    public string Text { get; set; }
    public bool LastStatement { get; set; }
}

internal sealed class ExecutionTranscript
{
    public int Version { get; set; } = 1;
    public string SessionId { get; set; }
    public string VictimId { get; set; }
    public string VictimName { get; set; }
    public string ExecutorId { get; set; }
    public string ExecutorName { get; set; }
    public string Actor { get; set; }
    public string VenueId { get; set; }
    public string VenueName { get; set; }
    public string Method { get; set; }
    public string Charge { get; set; }
    public int Day { get; set; }
    public long Order { get; set; }
    public string Outcome { get; set; } = "statement";
    public List<ExecutionTranscriptLine> Lines { get; set; } = new List<ExecutionTranscriptLine>();

    // Quotes are always original displayed text, never an LLM-written summary.
    internal string LastStatement => string.Join("\n", Lines.Where(x => x.LastStatement).Select(x => x.Text));
    internal string PublicLastWords => Outcome == "executed" && !string.IsNullOrWhiteSpace(LastStatement)
        ? "公开消息转述：" + VictimName + "临刑最后陈述原文节选：『" + string.Join("\n", Lines.Where(x => x.LastStatement).Take(3).Select(x => x.Text))
            + "』。只确认本人说过这些话，指控、愿望和托付未经证实；未在场者不能声称亲眼目睹。"
        : string.Empty;
}

// One authority for original transcripts; opaque unknown/corrupt records survive round trips.
internal sealed class ExecutionTranscriptStore
{
    private readonly Dictionary<string, ExecutionTranscript> _records = new Dictionary<string, ExecutionTranscript>(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _opaque = new Dictionary<string, string>(StringComparer.Ordinal);
    internal ExecutionTranscript Find(string id) => id != null && _records.TryGetValue(id, out var value) ? value : null;
    internal ExecutionTranscript Ensure(ExecutionTranscript record)
    {
        var existing = Find(record.SessionId);
        if (existing != null) return existing;
        if (_opaque.ContainsKey(record.SessionId)) return null;
        record.Order = _records.Count == 0 ? 1 : _records.Values.Max(x => x.Order) + 1;
        _records[record.SessionId] = record;
        foreach (var old in _records.Values.OrderByDescending(x => x.Order).Skip(100).ToArray())
            _records.Remove(old.SessionId);
        return record;
    }
    internal bool Append(string id, ExecutionTranscriptLine line)
    {
        var record = Find(id);
        if (record == null || record.Outcome != "statement" || line == null || line.Sequence <= 0 ||
            string.IsNullOrWhiteSpace(line.Text) || line.Text.Length > 240 || record.Lines.Count >= 24 ||
            record.Lines.Any(x => x.Sequence == line.Sequence)) return false;
        record.Lines.Add(line);
        return true;
    }
    internal Dictionary<string, string> Save()
    {
        var result = new Dictionary<string, string>(_opaque, StringComparer.Ordinal);
        foreach (var item in _records) result[item.Key] = JsonConvert.SerializeObject(item.Value);
        return result;
    }
    internal void Load(Dictionary<string, string> records)
    {
        _records.Clear(); _opaque.Clear();
        foreach (var pair in records ?? new Dictionary<string, string>())
        {
            try
            {
                var record = JsonConvert.DeserializeObject<ExecutionTranscript>(pair.Value);
                if (record?.Version != 1 || record.SessionId != pair.Key || record.Lines == null || record.Lines.Count > 24 ||
                    record.Lines.Any(x => x == null || string.IsNullOrWhiteSpace(x.Text) || x.Text.Length > 240))
                    throw new FormatException("Unknown transcript");
                _records[pair.Key] = record;
            }
            catch { _opaque[pair.Key] = pair.Value; }
        }
    }
}
