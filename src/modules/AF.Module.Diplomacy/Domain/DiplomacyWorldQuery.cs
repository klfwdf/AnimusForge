using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

// Cold request-boundary query. No persistent global matrix and no Tick work.
internal static class DiplomacyWorldQuery
{
    internal static List<string> Select(IReadOnlyCollection<string> roots, IReadOnlyCollection<string> available,
        Func<string, string, bool> atWar, Func<string, string, float> relation, int extraLimit = 6)
    {
        var allowed = new HashSet<string>(available, StringComparer.Ordinal);
        var seeds = roots.Where(allowed.Contains).Distinct(StringComparer.Ordinal).ToList();
        var seedSet = new HashSet<string>(seeds, StringComparer.Ordinal);
        var ranked = new List<(string id, int wars, float strength)>();
        foreach (string id in available.Distinct(StringComparer.Ordinal))
        {
            if (seedSet.Contains(id)) continue;
            int wars = 0; float strength = 0;
            foreach (string seed in seeds)
            {
                if (atWar(seed, id)) wars++;
                strength = Math.Max(strength, Math.Abs(relation(seed, id)));
            }
            // Two war links discover a common enemy; strong positive and negative
            // ruler relationships both remain useful leads, never proof of alliance.
            if (wars > 0 || strength >= 20) ranked.Add((id, wars, strength));
        }
        return seeds.Concat(ranked.OrderByDescending(x => x.wars).ThenByDescending(x => x.strength)
            .ThenBy(x => x.id, StringComparer.Ordinal).Take(Math.Max(0, extraLimit)).Select(x => x.id)).ToList();
    }
}
