using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

// Detached inputs only. Scoring adapters may use local ONNX; tests supply controlled
// scores to verify candidate limits, permissions and fallback without a remote provider.
internal static class WorldNewsRecallPolicy
{
    internal const int RerankLimit = 12;
    internal const int FinalLimit = 4;

    internal static WorldNewsRecallResult Retrieve(WorldNewsRecallRequest request,
        Func<string, float[]> embed, Func<string, IReadOnlyList<string>, float[]> rerank)
    {
        var result = new WorldNewsRecallResult { Generation = request?.Generation ?? 0, Observer = request?.Observer };
        if (request == null || string.IsNullOrWhiteSpace(request.Query) || request.Candidates.Count == 0) return result;
        float[] query = SafeEmbed(embed, request.Query);
        bool semantic = query != null;
        int semanticDocuments = 0;
        var ranked = new List<(WorldNewsRecallCandidate Candidate, double Score)>();
        foreach (var candidate in request.Candidates.Take(WorldNewsFactArchive.CandidateLimit))
        {
            double cosine = semantic ? Cosine(query, SafeEmbed(embed, candidate.SearchText)) : -1;
            if (cosine > -1) semanticDocuments++;
            // Missing/failed vectors never expand eligibility or manufacture relevance.
            if (candidate.LexicalScore < 1 && cosine < 0.45) continue;
            double lexical = Math.Min(candidate.LexicalScore, 12) / 12.0;
            ranked.Add((candidate, Math.Max(0, cosine) * 0.75 + lexical * 0.25
                + (candidate.Participant ? 0.025 : 0)));
        }
        var shortlist = ranked.OrderByDescending(x => x.Score).ThenByDescending(x => x.Candidate.Fact.Day)
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Candidate.Fact.Group) ? x.Candidate.Fact.Key : x.Candidate.Fact.Group,
                StringComparer.Ordinal).Select(g => g.First()).Take(RerankLimit).ToList();
        result.Mode = semantic && semanticDocuments > 0 ? "embedding" : "lexical_fallback";
        if (shortlist.Count > 0 && rerank != null)
        {
            float[] scores = null;
            try { scores = rerank(request.Query, shortlist.Select(x => x.Candidate.SearchText).ToArray()); } catch { }
            if (scores != null && scores.Length == shortlist.Count && scores.All(x => !float.IsNaN(x) && !float.IsInfinity(x)))
            {
                shortlist = shortlist.Select((x, i) => (x.Candidate, Score: (double)scores[i]))
                    .OrderByDescending(x => x.Score).ThenByDescending(x => x.Candidate.Fact.Day).ToList();
                result.Mode += "+rerank";
            }
            else result.Mode += "+rerank_unavailable";
        }
        var groups = new HashSet<string>(StringComparer.Ordinal);
        result.Selected = shortlist.Where(x => groups.Add(string.IsNullOrWhiteSpace(x.Candidate.Fact.Group)
            ? x.Candidate.Fact.Key : x.Candidate.Fact.Group)).Take(FinalLimit).Select(x => x.Candidate).ToList();
        return result;
    }

    private static float[] SafeEmbed(Func<string, float[]> embed, string text)
    {
        try { return embed?.Invoke(text); } catch { return null; }
    }
    internal static double Cosine(float[] a, float[] b)
    {
        if (a == null || b == null || a.Length == 0 || a.Length != b.Length) return -1;
        double dot = 0, aa = 0, bb = 0;
        for (int i = 0; i < a.Length; i++) { dot += a[i] * b[i]; aa += a[i] * a[i]; bb += b[i] * b[i]; }
        if (aa <= 0 || bb <= 0) return -1;
        double value = dot / Math.Sqrt(aa * bb);
        return double.IsNaN(value) || double.IsInfinity(value) ? -1 : Math.Max(-1, Math.Min(1, value));
    }
}
