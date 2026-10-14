using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal static class PersonaEvolutionPolicy
{
    internal const int BatchSize = 4, MaxPending = 64, MaxSeen = 300;
    internal static bool Accepts(string kind) => kind == "settlement_siege" || kind == "settlement_transfer"
        || kind == "fief_grant" || kind == "lord_killed" || kind == "ruler_killed";

    internal static bool Enqueue(PersonaEvolutionState state, PersonaEvolutionFact fact)
    {
        state.Pending ??= new List<PersonaEvolutionFact>();
        state.Seen ??= new List<string>();
        if (fact == null || string.IsNullOrWhiteSpace(fact.Key) || state.Seen.Contains(fact.Key)
            || state.Pending.Count >= MaxPending) return false;
        state.Pending.Add(fact);
        state.Seen.Add(fact.Key);
        if (state.Seen.Count > MaxSeen) state.Seen.RemoveAt(0);
        return true;
    }
    internal static PersonaEvolutionFact[] TakeBatch(PersonaEvolutionState state, double hour)
        => state?.Pending?.Count > 0 && hour >= state.Pending[0].Hour + 1
            ? state.Pending.Take(BatchSize).ToArray() : Array.Empty<PersonaEvolutionFact>();
    internal static void Complete(PersonaEvolutionState state, PersonaEvolutionFact[] batch)
    {
        var keys = new HashSet<string>(batch.Select(x => x.Key), StringComparer.Ordinal);
        state.Pending.RemoveAll(x => keys.Contains(x.Key));
        state.Attempts = 0;
    }
    internal static JObject Snapshot(MyBehavior.NpcPersonaProfile profile) => new JObject
    {
        ["description"] = profile.Description ?? "",
        ["traits"] = JToken.FromObject(profile.Traits ?? new List<NpcPersonaTrait>()),
        ["motivations"] = JToken.FromObject(profile.Motivations ?? new List<string>()),
        ["values"] = JToken.FromObject(profile.Values ?? new List<string>()),
        ["fears"] = JToken.FromObject(profile.Fears ?? new List<string>()),
        ["speech_style"] = profile.SpeechStyle ?? "",
        ["change_reason"] = profile.RecentChangeReason ?? ""
    };
    internal static bool IsCurrent(MyBehavior.NpcPersonaProfile profile, JObject original)
        => profile != null && JToken.DeepEquals(Snapshot(profile), original);

    // Reject the whole patch on invalid fields; omission preserves, explicit empty clears.
    internal static bool TryParse(string raw, IReadOnlyCollection<string> traitIds, out JObject patch)
    {
        patch = null;
        try
        {
            string text = (raw ?? "").Trim();
            if (text.Length > 12000) return false;
            var json = JObject.Parse(text, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if (json["changed"]?.Type != JTokenType.Boolean) return false;
            var allowed = new HashSet<string> { "changed", "traits", "motivations", "values", "fears", "speech_style", "change_reason" };
            if (json.Properties().Any(p => !allowed.Contains(p.Name))) return false;
            if (!(bool)json["changed"]) { patch = new JObject { ["changed"] = false }; return true; }
            if (!ValidText(json["change_reason"], 240) || string.IsNullOrWhiteSpace((string)json["change_reason"])) return false;
            bool hasField = false;
            foreach (string field in new[] { "traits", "motivations", "values", "fears", "speech_style" })
            {
                JToken token = json[field];
                if (token == null) continue;
                hasField = true;
                if (field == "speech_style") { if (!ValidText(token, 240)) return false; continue; }
                int limit = field == "traits" ? 5 : field == "fears" ? 2 : 3;
                if (!(token is JArray items) || items.Count > limit) return false;
                if (field != "traits")
                {
                    if (items.Any(x => !ValidText(x, 180) || string.IsNullOrWhiteSpace((string)x))) return false;
                    continue;
                }
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (JToken item in items)
                {
                    if (!(item is JObject trait) || trait.Properties().Count() != 2
                        || trait["id"]?.Type != JTokenType.String || trait["strength"]?.Type != JTokenType.Integer) return false;
                    string id = (string)trait["id"];
                    int strength = (int)trait["strength"];
                    if (!traitIds.Contains(id) || !seen.Add(id) || strength < 1 || strength > 3) return false;
                }
            }
            if (!hasField) return false;
            patch = json;
            return true;
        }
        catch (JsonException) { return false; }
        catch (OverflowException) { return false; }
        catch (ArgumentException) { return false; }
    }
    private static bool ValidText(JToken value, int max) => value?.Type == JTokenType.String && ((string)value).Length <= max;
    internal static bool Apply(MyBehavior.NpcPersonaProfile profile, JObject patch)
    {
        if (!(bool)patch["changed"]) return false;
        JObject before = Snapshot(profile);
        if (patch["traits"] is JArray traits)
            profile.Traits = traits.Select(t => new NpcPersonaTrait { Id = (string)t["id"], Strength = (int)t["strength"] }).ToList();
        if (patch["motivations"] is JArray motivations) profile.Motivations = motivations.Values<string>().ToList();
        if (patch["values"] is JArray values) profile.Values = values.Values<string>().ToList();
        if (patch["fears"] is JArray fears) profile.Fears = fears.Values<string>().ToList();
        if (patch["speech_style"] != null) profile.SpeechStyle = (string)patch["speech_style"];
        if (JToken.DeepEquals(before, Snapshot(profile))) return false;
        profile.RecentChangeReason = ((string)patch["change_reason"]).Trim();
        return true;
    }
    // Bound request projection without truncating stored player-authored text.
    internal static JObject RequestPersona(JObject snapshot)
    {
        var result = (JObject)snapshot.DeepClone();
        result["description"] = Clip((string)result["description"], 1000);
        result["speech_style"] = Clip((string)result["speech_style"], 240);
        result["change_reason"] = Clip((string)result["change_reason"], 240);
        foreach (string field in new[] { "motivations", "values", "fears" })
            result[field] = new JArray(((JArray)result[field]).Take(field == "fears" ? 2 : 3).Select(t => Clip((string)t, 180)));
        result["traits"] = new JArray(((JArray)result["traits"]).Take(5));
        return result;
    }
    internal static string Clip(string text, int length) => (text ?? "").Length <= length ? text ?? "" : text.Substring(0, length);
}
