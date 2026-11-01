using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

// Detached request copies only. Original source text, versions and audit digests
// stay in their existing owners. No game access, cross-request cache or Tick work.
internal static class DiplomacyRequestProjection
{
    internal const string WorldFormat = "af_world_defaults_v1";

    internal static string Project(string kind, string text, out bool worldProjected)
    {
        worldProjected = false;
        if (kind is not ("event_concept" or "event_concept_audit" or "event_boundary"
            or "event_boundary_audit" or "event_progress" or "event_progress_audit")
            || string.IsNullOrWhiteSpace(text) || text.Length > 512 * 1024) return text;
        try
        {
            if (Parse(text) is not JObject request) return text;
            foreach (var source in (request["sources"] as JArray ?? new JArray()).OfType<JObject>())
                if ((string)source["source_kind"] == "current_world" && (string)source["key"] == "world:current"
                    && source["text"]?.Type == JTokenType.String
                    && TryWorld((string)source["text"], out var world))
                { source["text"] = world; worldProjected = true; }
            if (request["world"]?.Type == JTokenType.String && TryWorld((string)request["world"], out var directWorld))
            { request["world"] = directWorld; worldProjected = true; }
            // These are producer-owned JSON documents, not arbitrary player prose.
            foreach (string field in new[] { "proposed_boundary", "rejected_boundary", "proposed_progress", "rejected_progress" })
                if (request[field]?.Type == JTokenType.String && TryObject((string)request[field], out var value))
                    request[field] = value;
            return request.ToString(Formatting.None);
        }
        catch (JsonException) { worldProjected = false; return text; }
    }

    private static JToken Parse(string text)
    {
        using var reader = new JsonTextReader(new StringReader(text)) { DateParseHandling = DateParseHandling.None, MaxDepth = 64 };
        var value = JToken.ReadFrom(reader);
        if (reader.Read()) throw new JsonReaderException("Multiple request roots");
        return value;
    }

    private static bool TryObject(string text, out JObject value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text) || !text.TrimStart().StartsWith("{", StringComparison.Ordinal)) return false;
        try { value = Parse(text) as JObject; return value != null; }
        catch (JsonException) { return false; }
    }

    internal static bool TryWorld(string text, out JObject projected)
    {
        projected = null;
        if (string.IsNullOrWhiteSpace(text) || !text.TrimStart().StartsWith("[", StringComparison.Ordinal)) return false;
        JArray kingdoms;
        try { kingdoms = Parse(text) as JArray; }
        catch (JsonException) { return false; }
        if (kingdoms == null || kingdoms.Count == 0 || kingdoms.Any(x => x is not JObject || x["kingdom_id"]?.Type != JTokenType.String)) return false;
        var relations = kingdoms.OfType<JObject>().SelectMany(x => (x["ruler_relations"] as JArray ?? new JArray()).OfType<JObject>()).ToArray();
        var towns = kingdoms.OfType<JObject>().SelectMany(x => (x["towns"] as JArray ?? new JArray()).OfType<JObject>()).ToArray();
        var relationDefaults = ExtractShared(relations, new[] { "minimum", "maximum" });
        var townDefaults = ExtractShared(towns, new[] { "loyalty_minimum", "loyalty_maximum", "security_minimum", "security_maximum",
            "prosperity_minimum", "food_minimum", "food_maximum" });
        projected = new JObject { ["format"] = WorldFormat, ["ruler_relation_defaults"] = relationDefaults,
            ["town_defaults"] = townDefaults, ["kingdoms"] = kingdoms };
        return true;
    }

    private static JObject ExtractShared(JObject[] rows, string[] fields)
    {
        var defaults = new JObject();
        if (rows.Length < 2) return defaults;
        foreach (string field in fields)
        {
            var first = rows[0][field];
            // Missing, unknown or differing bounds stay exactly where they were.
            if (first == null || first.Type is not (JTokenType.Integer or JTokenType.Float)
                || rows.Any(x => !JToken.DeepEquals(x[field], first))) continue;
            defaults[field] = first.DeepClone();
            foreach (var row in rows) row.Remove(field);
        }
        return defaults;
    }

    internal static string AppendContextOnce(string prompt, string rawSource, string context)
    {
        if (string.IsNullOrEmpty(context)) return prompt;
        // Compare whole generated blocks, never substrings selected by the model.
        // JSON escaping means the raw source must be checked before serialization.
        if ((rawSource ?? "").IndexOf(context, StringComparison.Ordinal) >= 0
            || (prompt ?? "").IndexOf(context, StringComparison.Ordinal) >= 0) return prompt;
        return prompt + "\n" + context;
    }
}
