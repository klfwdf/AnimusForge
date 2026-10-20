using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AnimusForge;

// Disposable derived text. Saved daily drafts remain the complete source of truth.
// Request frequency only; network/embedding callbacks receive detached strings.
internal sealed class IntradayConversationMemory
{
    internal sealed class Result
    {
        internal string Context = "", Revision = "";
        internal List<ConversationMessage> Recall = new List<ConversationMessage>();
    }
    private sealed class Segment
    {
        internal string Id, Text, Summary;
        internal int Index;
        internal DateTime RetryAfter;
        internal bool Pending;
    }
    private sealed class Scope
    {
        internal readonly Dictionary<string, Segment> Segments = new Dictionary<string, Segment>(StringComparer.Ordinal);
        internal DateTime LastUse;
        internal DateTime RetryAfter;
    }
    private readonly object _gate = new object();
    private readonly Dictionary<string, Scope> _scopes = new Dictionary<string, Scope>(StringComparer.Ordinal);
    internal void Clear() { lock (_gate) _scopes.Clear(); }
    internal static string Digest(string text)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? ""))).Replace("-", "");
    }

    internal Result Build(string key, List<ConversationMessage> source, string query, ConversationContextOptions options,
        Func<string, string> summarize, Func<bool> isCurrent, Func<string, float[]> embedding = null, DateTime? utcNow = null)
    {
        var result = new Result();
        if (!options.Enabled || !isCurrent()) return result;
        var turns = ConversationContextWindow.Group(source);
        int end = ConversationContextWindow.RecentStart(turns, options);
        if (end == 0) return result;
        DateTime now = utcNow ?? DateTime.UtcNow;
        var segments = new List<Segment>();
        var buffer = new StringBuilder();
        Action flush = () => {
            if (buffer.Length == 0) return;
            string text = buffer.ToString();
            segments.Add(new Segment { Id = Digest(segments.Count + "|" + text), Text = text, Index = segments.Count }); buffer.Clear();
        };
        for (int i = 0; i < end; i++)
        {
            // Never split a speech to make a summary. Oversized turns remain available to retrieval.
            string text = string.Join("\n", turns[i].Messages.Select(x => x.Role + " | " + x.SpeakerName + ": " + x.Content));
            if (text.Length > 8000) { flush(); continue; }
            if (buffer.Length + text.Length + 1 > 8000) flush();
            buffer.AppendLine(text);
            if (buffer.Length >= 6000) flush();
        }
        // Do not summarize the moving short tail on every request.
        Scope scope;
        Segment work = null;
        var visible = new List<Segment>();
        lock (_gate)
        {
            if (!_scopes.TryGetValue(key, out scope)) _scopes[key] = scope = new Scope();
            scope.LastUse = now;
            foreach (Segment segment in segments)
            {
                if (!scope.Segments.TryGetValue(segment.Id, out Segment cached))
                    scope.Segments[segment.Id] = cached = segment;
                cached.Index = segment.Index;
                visible.Add(cached);
            }
            // One provider call per request and per scope. Prefer the newest closed segment.
            if (summarize != null && now >= scope.RetryAfter && !scope.Segments.Values.Any(x => x.Pending))
                work = visible.LastOrDefault(x => x.Summary == null && now >= x.RetryAfter);
            if (work != null) work.Pending = true;
            // No source text retained between requests beyond 128 bounded segments per owner.
            var activeIds = new HashSet<string>(segments.Select(x => x.Id), StringComparer.Ordinal);
            foreach (string obsolete in scope.Segments.Where(x => !x.Value.Pending && !activeIds.Contains(x.Key))
                .Select(x => x.Key).ToArray()) scope.Segments.Remove(obsolete);
            while (scope.Segments.Count > 128)
            {
                Segment oldest = scope.Segments.Values.Where(x => !x.Pending).OrderBy(x => x.Index).FirstOrDefault();
                if (oldest == null) break; scope.Segments.Remove(oldest.Id);
            }
            while (_scopes.Count > 32)
            {
                string oldest = _scopes.Where(x => x.Key != key).OrderBy(x => x.Value.LastUse).First().Key;
                _scopes.Remove(oldest);
            }
        }
        if (work != null)
        {
            string summary = null;
            try { if (isCurrent()) summary = summarize(work.Text); }
            catch (OperationCanceledException) { throw; }
            catch { /* This derived cache must never discard raw history on provider failure. */ }
            finally
            {
                lock (_gate)
                {
                    work.Pending = false;
                    bool current = isCurrent() && _scopes.TryGetValue(key, out Scope active) && ReferenceEquals(active, scope)
                        && scope.Segments.TryGetValue(work.Id, out Segment same) && ReferenceEquals(same, work);
                    if (current && !string.IsNullOrWhiteSpace(summary) && summary.Length <= 1200) work.Summary = summary.Trim();
                    else work.RetryAfter = scope.RetryAfter = now.AddMinutes(1);
                }
            }
        }
        if (!isCurrent()) return result;
        var terms = ConversationContextWindow.Terms(query);
        // Snapshots of published immutable summaries; never hold a lock across a callback.
        List<Segment> summaries;
        lock (_gate) summaries = visible.Where(x => x.Summary != null).Select(x => new Segment { Id = x.Id, Index = x.Index, Summary = x.Summary }).ToList();
        Segment latest = summaries.OrderByDescending(x => x.Index).FirstOrDefault();
        var picked = summaries.Where(x => x != latest).Select(x => new { Segment = x, Score = ConversationContextWindow.Score(x.Summary, terms) })
            .Where(x => x.Score > 0).OrderByDescending(x => x.Score).ThenByDescending(x => x.Segment.Index)
            .Take(3).Select(x => x.Segment).ToList();
        if (latest != null) picked.Add(latest);
        picked = picked.OrderBy(x => x.Index).ToList();
        result.Context = string.Join("\n\n", picked.Select(x => "[" + x.Id.Substring(0, 12) + "] " + x.Summary));
        result.Revision = Digest(string.Join("|", picked.Select(x => x.Id)));

        // Original role-bearing speech, not a summary, answers requests for precise earlier details.
        string embeddingQuery = query ?? "";
        if (embeddingQuery.Length > 1200) embeddingQuery = embeddingQuery.Substring(embeddingQuery.Length - 1200);
        float[] queryVector = embedding?.Invoke(embeddingQuery);
        var scored = turns.Take(end).Select((turn, index) => new { Turn = turn, Index = index,
            Score = ConversationContextWindow.Score(turn.Text, terms) }).ToList();
        // Lexical preselection bounds new embeddings to at most 20 whole turns per request.
        int chars = 0;
        var recalledTurns = new List<(int Index, ConversationContextWindow.Turn Turn)>();
        foreach (var item in scored.Where(x => x.Score > 0).OrderByDescending(x => x.Score).ThenByDescending(x => x.Index).Take(20)
            .Select(x => new { x.Turn, x.Index, Score = x.Score + Similarity(queryVector, embedding, x.Turn.Text) * 0.25 })
            .OrderByDescending(x => x.Score).ThenByDescending(x => x.Index))
        {
            if (recalledTurns.Count >= ConversationContextWindow.RecallGroups) break;
            if (item.Turn.Chars > ConversationContextWindow.RecallChars - chars) continue;
            recalledTurns.Add((item.Index, item.Turn)); chars += item.Turn.Chars;
        }
        foreach (var recalledTurn in recalledTurns.OrderBy(x => x.Index)) result.Recall.AddRange(recalledTurn.Turn.Messages);
        if (!isCurrent()) return new Result();
        return result;
    }

    private static double Similarity(float[] query, Func<string, float[]> embedding, string text)
    {
        if (query == null || embedding == null) return 0;
        float[] vector = embedding(text.Length > 1200 ? text.Substring(0, 1200) : text);
        if (vector == null || vector.Length != query.Length) return 0;
        double dot = 0;
        for (int i = 0; i < vector.Length; i++) dot += query[i] * vector[i];
        return Math.Max(0, Math.Min(1, dot));
    }
}
