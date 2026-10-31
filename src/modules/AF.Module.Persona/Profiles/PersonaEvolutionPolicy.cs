using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal static class PersonaEvolutionPolicy
{
    internal const int BatchSize = 4, MaxPending = 64, MaxSeen = 300;
    internal const double MergeDelayHours = 10.0;
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
        => state?.Pending?.Count > 0 && hour >= state.Pending[0].Hour + MergeDelayHours
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
