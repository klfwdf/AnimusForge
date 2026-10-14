using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnimusForge;

// Confirmed source material only. Never store article prose or model anecdotes here.
internal sealed class WorldNewsFact
{
    public string Key = "", Kind = "", Date = "", Sentence = "", Detail = "", Group = "";
    public int Day, Score;
    public bool Published;
    public List<string> KingdomIds = new List<string>();
    public List<string> ParticipantIds = new List<string>();
}

internal sealed class WorldNewsObserver
{
    internal string HeroId = "", KingdomId = "", LocalKingdomId = "";
    internal int Day;
    internal ISet<string> KnownDocuments;
}

internal sealed class WorldNewsRecallCandidate
{
    internal WorldNewsFact Fact;
    internal double LexicalScore;
    internal bool Participant;
    internal string SearchText;
}

internal sealed class WorldNewsRecallRequest
{
    internal long Generation;
    internal WorldNewsObserver Observer;
    internal string Query = "";
    internal List<WorldNewsRecallCandidate> Candidates = new List<WorldNewsRecallCandidate>();
}

internal sealed class WorldNewsRecallResult
{
    internal long Generation;
    internal WorldNewsObserver Observer;
    internal string Mode = "empty";
    internal List<WorldNewsRecallCandidate> Selected = new List<WorldNewsRecallCandidate>();
}

// Game-thread-owned index. Rebuilt once after load; incrementally maintained on confirmed
// event changes. Requests use postings, not a scan of every historical record.
internal sealed class WorldNewsFactArchive
{
    internal const int CandidateLimit = 48;
    private readonly List<WorldNewsFact> _facts;
    private readonly Dictionary<string, int> _keys = new Dictionary<string, int>(StringComparer.Ordinal);
    private readonly Dictionary<string, SortedSet<int>> _words = new Dictionary<string, SortedSet<int>>(StringComparer.Ordinal);
    private readonly Dictionary<string, SortedSet<int>> _kingdoms = new Dictionary<string, SortedSet<int>>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SortedSet<int>> _participants = new Dictionary<string, SortedSet<int>>(StringComparer.Ordinal);
    private readonly SortedSet<int> _published = new SortedSet<int>();

    internal WorldNewsFactArchive(List<WorldNewsFact> facts)
    {
        _facts = facts ?? throw new ArgumentNullException(nameof(facts));
        // Exact-key normalization is load-boundary work, not request-frequency work.
        var original = facts.ToArray();
        facts.Clear();
        foreach (var fact in original) Upsert(fact);
    }

    internal static bool Durable(string kind, int score, bool player)
        => !string.IsNullOrWhiteSpace(kind) && (score >= 40 || player
            || kind == "lord_captured" || kind == "fief_grant" || kind == "ruler_policy"
            || kind == "diplomatic_declaration" || kind == "execution_last_words");

    internal void Upsert(WorldNewsFact input)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.Key) || string.IsNullOrWhiteSpace(input.Sentence)) return;
        WorldNewsFact fact = Copy(input);
        if (_keys.TryGetValue(fact.Key, out int slot))
        {
            WorldNewsFact previous = _facts[slot];
            fact.Published |= previous.Published;
            // Legacy frozen materials often lack identities; they must not erase live provenance.
            if (fact.KingdomIds.Count == 0) fact.KingdomIds = previous.KingdomIds.ToList();
            if (fact.ParticipantIds.Count == 0) fact.ParticipantIds = previous.ParticipantIds.ToList();
            if (string.IsNullOrWhiteSpace(fact.Group)) fact.Group = previous.Group;
            fact.Score = Math.Max(fact.Score, previous.Score);
            Unindex(slot, previous);
            _facts[slot] = fact;
        }
        else
        {
            slot = _facts.Count;
            _keys.Add(fact.Key, slot);
            _facts.Add(fact);
        }
        Index(slot, fact);
    }

    internal void MarkPublished(IEnumerable<string> keys)
    {
        foreach (string key in keys ?? Enumerable.Empty<string>())
        {
            if (key == null || !_keys.TryGetValue(key, out int slot)) continue;
            _facts[slot].Published = true;
            if (_facts[slot].Score >= 60) _published.Add(slot);
        }
    }

    internal bool Contains(string key) => key != null && _keys.ContainsKey(key);

    internal static bool Visible(WorldNewsFact fact, WorldNewsObserver observer)
    {
        if (fact == null || observer == null || string.IsNullOrWhiteSpace(fact.Key) || fact.Day < 0 || fact.Day > observer.Day) return false;
        // Being a participant or ruler is not permission to bypass diplomacy propagation.
        if (string.Equals(fact.Kind, "diplomatic_declaration", StringComparison.OrdinalIgnoreCase)
            || fact.Key.StartsWith("declaration:", StringComparison.OrdinalIgnoreCase))
            return fact.Key.StartsWith("declaration:", StringComparison.Ordinal)
                && observer.KnownDocuments != null && observer.KnownDocuments.Contains(fact.Key.Substring(12));
        if (!string.IsNullOrEmpty(observer.HeroId) && fact.ParticipantIds.Contains(observer.HeroId)) return true;
        if (fact.KingdomIds.Any(k => !string.IsNullOrEmpty(k)
            && (string.Equals(k, observer.KingdomId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(k, observer.LocalKingdomId, StringComparison.OrdinalIgnoreCase)))) return true;
        return fact.Published && fact.Score >= 60;
    }

    internal WorldNewsRecallRequest Capture(WorldNewsObserver observer, string query, long generation)
    {
        var request = new WorldNewsRecallRequest { Query = (query ?? "").Trim(), Generation = generation };
        if (observer == null || request.Query.Length < 2) return request;
        request.Observer = new WorldNewsObserver { HeroId=observer.HeroId, KingdomId=observer.KingdomId,
            LocalKingdomId=observer.LocalKingdomId, Day=observer.Day,
            KnownDocuments=observer.KnownDocuments == null ? null : new HashSet<string>(observer.KnownDocuments, StringComparer.Ordinal) };
        var scores = new Dictionary<int, double>();
        // Bounded postings per query token; specific names/dates bring old records back
        // even when they have long left the recent-event window.
        foreach (string word in Terms(request.Query).Take(32))
        {
            if (!_words.TryGetValue(word, out var slots)) continue;
            double weight = 1.0 + 2.0 / Math.Log(2.0 + slots.Count);
            foreach (int slot in slots.Reverse().Take(256))
            {
                if (!Visible(_facts[slot], observer)) continue;
                scores.TryGetValue(slot, out double old);
                scores[slot] = old + weight;
            }
        }
        void Seed(IEnumerable<int> slots)
        {
            foreach (int slot in slots.Reverse().Take(48))
                if (Visible(_facts[slot], observer) && !scores.ContainsKey(slot)) scores.Add(slot, 0);
        }
        if (!string.IsNullOrEmpty(observer.HeroId) && _participants.TryGetValue(observer.HeroId, out var personal)) Seed(personal);
        if (!string.IsNullOrEmpty(observer.KingdomId) && _kingdoms.TryGetValue(observer.KingdomId, out var home)) Seed(home);
        if (!string.IsNullOrEmpty(observer.LocalKingdomId) && _kingdoms.TryGetValue(observer.LocalKingdomId, out var local)) Seed(local);
        Seed(_published);
        // Known declarations can belong to any kingdom, including a former one.
        foreach (string document in observer.KnownDocuments ?? new HashSet<string>())
            if (_keys.TryGetValue("declaration:" + document, out int slot) && Visible(_facts[slot], observer) && !scores.ContainsKey(slot))
                scores.Add(slot, 0);
        request.Candidates = scores.OrderByDescending(p => p.Value)
            .ThenByDescending(p => _facts[p.Key].ParticipantIds.Contains(observer.HeroId ?? ""))
            .ThenByDescending(p => _facts[p.Key].Score).ThenByDescending(p => _facts[p.Key].Day)
            .ThenBy(p => _facts[p.Key].Key, StringComparer.Ordinal).Take(CandidateLimit)
            .Select(p => new WorldNewsRecallCandidate {
                Fact = Copy(_facts[p.Key]), LexicalScore = p.Value,
                Participant = !string.IsNullOrEmpty(observer.HeroId) && _facts[p.Key].ParticipantIds.Contains(observer.HeroId),
                SearchText = SearchText(_facts[p.Key])
            }).ToList();
        return request;
    }

    internal static string SearchText(WorldNewsFact fact)
        => (fact.Date + "\n" + fact.Sentence + "\n" + fact.Detail).Trim();

    internal static WorldNewsFact Copy(WorldNewsFact f) => new WorldNewsFact {
        Key = f.Key ?? "", Kind = f.Kind ?? "", Date = f.Date ?? "", Day = f.Day, Score = f.Score,
        Sentence = f.Sentence ?? "", Detail = f.Detail ?? "", Group = f.Group ?? "", Published = f.Published,
        KingdomIds = (f.KingdomIds ?? new List<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
        ParticipantIds = (f.ParticipantIds ?? new List<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToList()
    };

    internal static HashSet<string> Terms(string text)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        var word = new StringBuilder();
        char previousCjk = '\0';
        void Flush() { if (word.Length > 1) result.Add(word.ToString()); word.Clear(); }
        foreach (char raw in (text ?? "").Take(1600))
        {
            char c = char.ToLowerInvariant(raw);
            if (c >= '\u3400' && c <= '\u9fff')
            {
                Flush();
                if (previousCjk != '\0') result.Add(new string(new[] { previousCjk, c }));
                previousCjk = c;
            }
            else
            {
                previousCjk = '\0';
                if (char.IsLetterOrDigit(c)) word.Append(c); else Flush();
            }
        }
        Flush();
        return result;
    }
    private static void Add(Dictionary<string, SortedSet<int>> map, string key, int slot)
    {
        if (!map.TryGetValue(key, out var slots)) map[key] = slots = new SortedSet<int>();
        slots.Add(slot);
    }
    private static void Remove(Dictionary<string, SortedSet<int>> map, string key, int slot)
    {
        if (!map.TryGetValue(key, out var slots)) return;
        slots.Remove(slot); if (slots.Count == 0) map.Remove(key);
    }
    private void Index(int slot, WorldNewsFact f)
    {
        foreach (string word in Terms(SearchText(f))) Add(_words, word, slot);
        foreach (string k in f.KingdomIds) Add(_kingdoms, k, slot);
        foreach (string h in f.ParticipantIds) Add(_participants, h, slot);
        if (f.Published && f.Score >= 60) _published.Add(slot);
    }
    private void Unindex(int slot, WorldNewsFact f)
    {
        foreach (string word in Terms(SearchText(f))) Remove(_words, word, slot);
        foreach (string k in f.KingdomIds) Remove(_kingdoms, k, slot);
        foreach (string h in f.ParticipantIds) Remove(_participants, h, slot);
        _published.Remove(slot);
    }
}
