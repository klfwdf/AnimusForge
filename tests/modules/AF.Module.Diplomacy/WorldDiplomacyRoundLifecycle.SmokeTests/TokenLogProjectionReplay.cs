using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

// Optional read-only replay of user-selected local logs. Outputs aggregate sizes
// only; no prompt text, credentials or private conversation is copied to fixtures.
internal static class TokenLogProjectionReplay
{
    internal static void Run(string path)
    {
        string date = "", mode = "";
        bool request = false;
        var groups = new Dictionary<string, (int count, long before, long after)>();
        foreach (string line in File.ReadLines(path))
        {
            var start = Regex.Match(line, @"^======.*?(2026-\d\d-\d\d)");
            if (start.Success) date = start.Groups[1].Value;
            var stat = Regex.Match(line, @"^\[[\d:]+\] in=\d+ out=\d+ mode=(\S+)");
            if (stat.Success) mode = stat.Groups[1].Value;
            if (request)
            {
                request = false;
                if (string.CompareOrdinal(date, "2026-10-23") < 0 || mode != "world_diplomacy_api") continue;
                JObject body;
                try { body = JObject.Parse(line); } catch (JsonException) { continue; }
                var messages = body["messages"] as JArray;
                if (messages == null) continue;
                long before = messages.Sum(x => ((string)x["content"] ?? "").Length), after = before;
                string kind = "other";
                foreach (var msg in messages.OfType<JObject>().Where(x => (string)x["role"] == "user"))
                {
                    string text = (string)msg["content"] ?? "";
                    JObject data;
                    try { data = JObject.Parse(text); } catch (JsonException) { continue; }
                    kind = data["sources"] is JArray ? "event_concept" : data["stage"] != null ? "event_boundary"
                        : data["proposed_progress"] != null ? "event_progress" : "other";
                    string projected = DiplomacyRequestProjection.Project(kind, text, out bool world);
                    string contract = world ? DiplomacyPromptTexts.Get("RequestWorldProjection", "") : "";
                    if ((!world || !string.IsNullOrWhiteSpace(contract)) && projected.Length + contract.Length + 64 < text.Length)
                        after -= text.Length - projected.Length - contract.Length - 1;
                }
                foreach (string key in new[] { "all", kind })
                {
                    groups.TryGetValue(key, out var g);
                    groups[key] = (g.count + 1, g.before + before, g.after + after);
                }
            }
            if (line == "REQUEST_BODY:") request = true;
        }
        foreach (var pair in groups.OrderBy(x => x.Key))
            Console.WriteLine(JsonConvert.SerializeObject(new { category = pair.Key, requests = pair.Value.count,
                beforeChars = pair.Value.before, afterChars = pair.Value.after,
                reductionPercent = Math.Round(100d * (pair.Value.before - pair.Value.after) / Math.Max(1, pair.Value.before), 2) }));
        Console.WriteLine("Historical request projection only; character counts include the new contract, not provider token counts. Audit context reuse and saved retries are excluded.");
    }
}
