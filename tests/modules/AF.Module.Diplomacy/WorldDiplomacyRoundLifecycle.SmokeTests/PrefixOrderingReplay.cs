using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class PrefixOrderingReplay
{
    internal static void Run()
    {
        foreach (string evidence in new[]
        {
            "Original evidence: no consent; tribute 0.123456789; {NAME} $1 \"quoted\".\r\n",
            "原案证据：未同意；贡金0.123456789；{NAME} $1 \"原文\"。\r\n"
        })
        {
            var originalCase = new JObject { ["known_by"] = "a", ["source_is_private"] = false,
                ["original_evidence"] = string.Concat(Enumerable.Repeat(evidence, 100)),
                ["known_public_facts"] = "{\"source\":\"original\"}", ["excerpt"] = false };
            var first = Request(originalCase, "digest_first", 89.52135m);
            var second = Request(originalCase, "digest_second", 89.6136551m);
            string raw = first.ToString(Formatting.None), changed = second.ToString(Formatting.None);
            foreach (string kind in new[] { "event_concept", "event_concept_audit", "event_boundary",
                "event_boundary_audit", "event_progress", "event_progress_audit" })
            {
                var job = new WorldDiplomacyJob { Kind = kind, SystemPrompt = "Keep every rule and fact.", UserPrompt = raw };
                var messages = WorldDiplomacyLlmMessageApplication.BuildLlmMessageArray(job,
                    _ => throw new Exception("Unexpected history read"));
                string sent = (string)messages.Last!["content"]!;
                var actual = JObject.Parse(sent);
                Test.True(JToken.DeepEquals(first, actual), "root ordering preserves every value and unknown field: " + kind);
                Test.True(job.UserPrompt == raw && job.SystemPrompt == "Keep every rule and fact."
                    && (string)messages[0]!["content"]! == job.SystemPrompt,
                    "ordering reaches equal-size requests without rewriting stored prompts or rules: " + kind);
                Test.True(actual.Properties().First().Name == "trigger_material"
                    && actual.Properties().Last().Name == "repair_feedback", "original case precedes all dynamic tails: " + kind);
                Test.True(actual.Property("sources")!.Previous != null
                    && actual.Properties().Select(p => p.Name).ToList().IndexOf("world")
                        > actual.Properties().Select(p => p.Name).ToList().IndexOf("unknown"),
                    "current values follow fixed root data: " + kind);
                Test.True((decimal)actual["world"]!["security"]! == 89.52135m
                    && (string)actual["trigger_material"]!["original_evidence"]! == (string)originalCase["original_evidence"]!
                    && JToken.DeepEquals(first["sources"], actual["sources"]),
                    "numeric precision, exact bilingual evidence and source array order are preserved: " + kind);
                string next = DiplomacyRequestProjection.OrderForStablePrefix(kind, changed);
                Test.True(CommonPrefix(sent, next) > originalCase.ToString(Formatting.None).Length
                    && CommonPrefix(sent, next) > CommonPrefix(raw, changed),
                    "changed digests and current numbers no longer interrupt the repeated case: " + kind);
                Test.True(DiplomacyRequestProjection.OrderForStablePrefix(kind, sent) == sent,
                    "ordering is idempotent: " + kind);
            }
            foreach (string kind in new[] { "generate", "compress", "privacy_audit", "unknown" })
                Test.True(DiplomacyRequestProjection.OrderForStablePrefix(kind, raw) == raw,
                    "unrelated and canonical message contracts stay unchanged: " + kind);
        }
        foreach (string invalid in new[] { "{broken", "[1,2]", "{\"trigger_material\":1} trailing", "", new string('x', 512 * 1024 + 1) })
            Test.True(DiplomacyRequestProjection.OrderForStablePrefix("event_concept", invalid) == invalid,
                "invalid, nonobject and oversized input retain the original failure boundary");
        Console.WriteLine("PASS prefix ordering: real message consumer, all six structured kinds, CN/EN exact evidence, values, source order, dynamic tails and frozen-contract exclusions");
    }

    private static JObject Request(JObject evidence, string digest, decimal security) => new()
    {
        ["seed_digest"] = digest, ["repair_feedback"] = digest,
        ["world"] = new JObject { ["security"] = security, ["map_x"] = 557.2634m },
        ["sources"] = new JArray(new JObject { ["key"] = "second", ["version"] = digest },
            new JObject { ["key"] = "first", ["text"] = "original untrusted source" }),
        ["unknown"] = new JObject { ["z"] = 1, ["a"] = 2 },
        ["trigger_material"] = evidence.DeepClone(), ["response_contract"] = new JObject { ["seed_digest"] = digest },
        ["previous_search_result"] = digest
    };

    internal static int CommonPrefix(string first, string second)
    {
        int i = 0, end = Math.Min(first.Length, second.Length);
        while (i < end && first[i] == second[i]) i++;
        return i;
    }

    internal static void RunLog(string path)
    {
        var requests = new List<(JObject body, string session, int line)>();
        string session = ""; bool readBody = false, diplomacy = false; int lineNumber = 0;
        foreach (string line in File.ReadLines(path))
        {
            lineNumber++;
            if (line.StartsWith("====== Game started ")) session = line;
            if (line.StartsWith("[") && line.Contains(" mode="))
                diplomacy = line.EndsWith(" mode=world_diplomacy_api");
            if (readBody)
            {
                readBody = false;
                if (diplomacy)
                    try { requests.Add((JObject.Parse(line), session, lineNumber)); } catch (JsonException) { }
            }
            if (line == "REQUEST_BODY:") readBody = true;
        }
        Test.True(requests.Count > 0, "selected local log contains complete diplomacy requests");
        string latest = requests.Last().session;
        var previous = new Dictionary<string, (string before, string after, int line)>();
        int replayed = 0, improved = 0;
        foreach (var row in requests.Where(x => x.session == latest))
        {
            var messages = row.body["messages"] as JArray;
            string text = (string?)messages?.LastOrDefault(x => (string?)x["role"] == "user")?["content"] ?? "";
            JObject data; try { data = JObject.Parse(text); } catch (JsonException) { continue; }
            string kind = data["sources"] is JArray ? "event_concept" : data["stage"] != null ? "event_boundary"
                : data["proposed_progress"] != null ? "event_progress" : "";
            if (kind == "") continue;
            string system = (string)messages!.First(x => (string?)x["role"] == "system")["content"]!;
            string baseline = DiplomacyRequestProjection.Project(kind, text, out bool world);
            string contract = world ? DiplomacyPromptTexts.Get("RequestWorldProjection", "") : "";
            bool compacted = (!world || !string.IsNullOrWhiteSpace(contract)) && baseline.Length + contract.Length + 64 < text.Length;
            if (!compacted) baseline = text;
            string expectedSystem = compacted && world ? system + "\n" + contract : system;
            var job = new WorldDiplomacyJob { Kind = kind, UserPrompt = text, SystemPrompt = system };
            var sent = WorldDiplomacyLlmMessageApplication.BuildLlmMessageArray(job, _ => throw new Exception("Unexpected history"));
            string actual = (string)sent.Last!["content"]!;
            Test.True(JToken.DeepEquals(JObject.Parse(baseline), JObject.Parse(actual)), "logged request values remain identical at line " + row.line);
            Test.True((string)sent.First!["content"]! == expectedSystem, "logged system rules remain identical at line " + row.line);
            Test.True(job.UserPrompt == text && job.SystemPrompt == system, "logged stored source remains unchanged at line " + row.line);
            replayed++;
            if (previous.TryGetValue(expectedSystem, out var prior))
            {
                int before = CommonPrefix(prior.before, baseline), after = CommonPrefix(prior.after, actual);
                if (after > before) improved++;
                Console.WriteLine(JsonConvert.SerializeObject(new { previousLine = prior.line, currentLine = row.line,
                    systemChars = expectedSystem.Length, userPrefixBeforeChars = before, userPrefixAfterChars = after }));
            }
            previous[expectedSystem] = (baseline, actual, row.line);
        }
        Test.True(replayed > 0, "latest nonempty session has structured diplomacy requests");
        Console.WriteLine("PASS original log replay: session=" + latest + " structuredRequests=" + replayed
            + " improvedRepeatedPairs=" + improved + " assertions=" + Test.Assertions
            + "; provider cache hits NOT_RUN; no private prompt text exported");
    }
}
