using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

internal static class DiplomacyMaterialCaptureRules
{
    // Navigation is an identity probe. Full thematic retrieval retains its
    // independent generic and identity lanes, plus related-person expansion.
    internal static List<string> Queries(string topic, bool themed, string[] themes, int searchPass)
    {
        var queries = new List<string>();
        if (!string.IsNullOrWhiteSpace(topic))
        {
            queries.Add(topic);
            if (themes.Length > 0)
            {
                int start = Math.Max(0, Math.Min(1000000, searchPass)) * 3 % themes.Length;
                queries.AddRange(Enumerable.Range(0, themed ? 1 : Math.Min(3, themes.Length))
                    .Select(i => themes[(start + i) % themes.Length]));
            }
        }
        queries.Add("");
        return queries.Distinct(StringComparer.Ordinal).ToList();
    }
}
