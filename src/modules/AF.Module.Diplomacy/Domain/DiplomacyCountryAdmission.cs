using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

// Detached request-boundary input. Missing relations are absent; failed reads
// throw in the host and never become neutral relations in this calculation.
internal sealed class DiplomacyAdmissionInput
{
    public List<string> Direct = new();
    public List<string> Candidates = new();
    public Dictionary<string, string> Heroes = new(StringComparer.Ordinal);
    public List<DiplomacyAdmissionPair> Pairs = new();
}

internal sealed class DiplomacyAdmissionPair
{
    public string Country, Origin, Ruler, OriginRuler;
    public bool Border, War;
    public Dictionary<string, int> Relations = new(StringComparer.Ordinal);
}

internal sealed class DiplomacyAdmissionScore
{
    public string Country, Origin, CoreHero;
    public int Border, War, RulerRelation, CoreRelation, Total;
}

internal sealed class DiplomacyAdmissionResult
{
    public int Version = 2;
    // Read-only legacy evidence; version 2 has no score threshold.
    public int Threshold;
    public int Preference = 1, SampledCount, TargetCount;
    public string DrawSeed;
    public string Seed;
    public DiplomacyAdmissionInput Input;
    public List<DiplomacyAdmissionScore> Scores = new();
    public List<string> Admitted = new();
}

internal static class DiplomacyCountryAdmission
{
    internal const int CurrentVersion = 2;

    internal static int SampleCount(int preference, int roll)
    {
        if (roll < 0 || roll >= 100) throw new ArgumentOutOfRangeException(nameof(roll));
        preference = preference >= 0 && preference <= 2 ? preference : 1;
        int small = preference == 0 ? 70 : 15;
        int medium = preference == 1 ? 70 : 15;
        return roll < small ? 2 : roll < small + medium ? 3 : 4;
    }
    internal static int RelationPoints(int relation, bool core)
    {
        int strength = (int)Math.Min(100L, Math.Abs((long)relation));
        return strength < 20 ? 0 : strength < 40 ? (core ? 10 : 5)
            : strength < 60 ? (core ? 20 : 10) : strength < 80 ? (core ? 30 : 15) : (core ? 35 : 20);
    }

    internal static DiplomacyAdmissionResult Calculate(DiplomacyAdmissionInput input, int preference = 1, string drawSeed = "")
    {
        if (input?.Direct == null || input.Direct.Count == 0 || input.Candidates == null
            || input.Pairs == null || input.Heroes == null) throw new ArgumentException("admission_direct_parties_missing");
        var random = new SelectionRandom(drawSeed ?? "");
        var result = new DiplomacyAdmissionResult { Input = input, DrawSeed = drawSeed ?? "",
            Preference = preference >= 0 && preference <= 2 ? preference : 1,
            Admitted = input.Direct.Distinct(StringComparer.Ordinal).ToList() };
        if (result.Admitted.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("admission_direct_identity_empty");
        foreach (var pair in input.Pairs)
        {
            if (pair == null || pair.Relations == null || !input.Direct.Contains(pair.Origin)
                || input.Direct.Contains(pair.Country) || !input.Candidates.Contains(pair.Country))
                throw new ArgumentException("admission_pair_not_original_seed");
            int ruler = pair.OriginRuler != null && pair.Relations.TryGetValue(pair.OriginRuler, out int raw)
                ? RelationPoints(raw, false) : 0;
            var score = new DiplomacyAdmissionScore { Country = pair.Country, Origin = pair.Origin,
                Border = pair.Border ? 20 : 0, War = pair.War ? 25 : 0, RulerRelation = ruler };
            foreach (var hero in input.Heroes.Where(x => x.Value == pair.Origin))
            {
                if (!pair.Relations.TryGetValue(hero.Key, out int relation)) continue;
                int core = RelationPoints(relation, true);
                // Evaluate every legal combination. When the core is the ruler,
                // use the stronger category once; a different core may win by
                // combining its points with the ruler's separate pair.
                int separateRuler = hero.Key == pair.OriginRuler ? 0 : ruler;
                if (separateRuler + core > score.RulerRelation + score.CoreRelation)
                { score.RulerRelation = separateRuler; score.CoreRelation = core; score.CoreHero = hero.Key; }
            }
            score.Total = score.Border + score.War + score.RulerRelation + score.CoreRelation;
            result.Scores.Add(score);
        }
        var scores = result.Scores.GroupBy(x => x.Country, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.Max(s => s.Total), StringComparer.Ordinal);
        var remaining = input.Candidates
            .Where(x => !string.IsNullOrWhiteSpace(x) && !result.Admitted.Contains(x))
            .Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();
        result.SampledCount = SampleCount(result.Preference, (int)(random.Next() * 100));
        result.TargetCount = Math.Min(result.Admitted.Count + remaining.Count,
            Math.Max(result.Admitted.Count, result.SampledCount));
        while (result.Admitted.Count < result.TargetCount)
        {
            int Weight(string id) => 10 + (scores.TryGetValue(id, out int score) ? Math.Max(0, score) : 0);
            double ticket = random.Next() * remaining.Sum(Weight);
            int selected = remaining.Count - 1;
            for (int i = 0; i < remaining.Count; i++)
            {
                ticket -= Weight(remaining[i]);
                if (ticket < 0) { selected = i; break; }
            }
            result.Admitted.Add(remaining[selected]);
            remaining.RemoveAt(selected);
        }
        return result;
    }

    // Stable across processes/game versions. Job IDs provide fresh entropy;
    // the seed and selected list are persisted, never sampled per retry/tick.
    private sealed class SelectionRandom
    {
        private ulong _state = 14695981039346656037UL;
        internal SelectionRandom(string seed)
        {
            foreach (char c in seed) { _state ^= c; _state = unchecked(_state * 1099511628211UL); }
        }
        internal double Next()
        {
            ulong value = _state = unchecked(_state + 0x9E3779B97F4A7C15UL);
            value = unchecked((value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL);
            value = unchecked((value ^ (value >> 27)) * 0x94D049BB133111EBUL);
            return ((value ^ (value >> 31)) >> 11) * (1.0 / 9007199254740992.0);
        }
    }
}
