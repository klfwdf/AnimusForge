using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AnimusForge;

// Engine-independent rules shared by the campaign adapter and regression harness.
internal static class ExpeditionParadeRules
{
    internal const int WindowDays = 14;
    internal const int DisplayLimit = 96;
    internal const int FormationLimit = 24;
    internal const int AgentReserve = 32;
    internal static bool InWindow(double now, double start) => !double.IsNaN(now) && !double.IsNaN(start)
        && !double.IsInfinity(now) && !double.IsInfinity(start) && now >= start && now - start <= WindowDays;

    internal static readonly Regex ActionTags = new Regex(@"\[ACTION:PARADE:[^\]\r\n]*\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    internal static string NormalizeTags(string raw, IEnumerable<string> allowed)
    {
        var tags = ActionTags.Matches(raw ?? "").Cast<Match>().Select(m => m.Value.ToUpperInvariant()).Distinct().ToList();
        // Reject conflicting or unknown types before applying availability filtering.
        return tags.Count == 1 && (tags[0] == "[ACTION:PARADE:EXPEDITION]" || tags[0] == "[ACTION:PARADE:VICTORY]")
            && (allowed ?? Enumerable.Empty<string>()).Contains(tags[0], StringComparer.OrdinalIgnoreCase) ? tags[0] : "";
    }

    // Tribute is authoritative when paid; a neutral peace needs an exact territorial snapshot.
    internal static int Winner(int tributeFromFirstToSecond, bool exact, int firstDelta, int secondDelta)
        => tributeFromFirstToSecond > 0 ? -1 : tributeFromFirstToSecond < 0 ? 1 : exact ? Math.Sign((long)firstDelta - secondDelta) : 0;

    internal static int[] Allocate(IReadOnlyList<int> available, int cap)
    {
        int[] result = new int[available.Count];
        cap = Math.Max(0, cap);
        // First give every nonempty source representation; then weighted fair allocation.
        for (int i = 0; i < available.Count && cap > 0; i++)
            if (available[i] > 0) { result[i]++; cap--; }
        while (cap-- > 0)
        {
            int best = -1;
            double priority = -1;
            for (int i = 0; i < available.Count; i++)
            {
                if (result[i] >= available[i]) continue;
                double score = (double)available[i] / (result[i] + 1);
                if (score > priority) { priority = score; best = i; }
            }
            if (best < 0) break;
            result[best]++;
        }
        return result;
    }
}
