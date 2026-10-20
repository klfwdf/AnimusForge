using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace AnimusForge;

// Request-time text only. No game objects, IO, provider calls or save state.
// A section is an entire authoritative message, never a guessed line or sentence.
internal sealed class PromptCacheMessage : IEquatable<PromptCacheMessage>
{
    internal readonly string Role, Content;
    internal readonly int HistoryGroup;
    internal readonly string Revision;
    private readonly int _hash;
    internal PromptCacheMessage(string role, string content, int historyGroup = 0, string revision = "")
    {
        Role = role; Content = content;
        HistoryGroup = historyGroup; Revision = revision ?? "";
        _hash = unchecked(StringComparer.Ordinal.GetHashCode(role) * 397 ^ StringComparer.Ordinal.GetHashCode(content));
    }
    public bool Equals(PromptCacheMessage other) => other != null && Role == other.Role && Content == other.Content;
    public override bool Equals(object other) => Equals(other as PromptCacheMessage);
    public override int GetHashCode() => _hash;
}

internal sealed class PromptCacheText
{
    internal string Policy, Snapshot, Section, Input;
    internal string Format(string template, string actor, string sections)
        => PromptComposerTexts.FormatTemplate(template, ("ACTOR", actor), ("SECTIONS", sections));
}

internal sealed class ConversationPromptCacheStore
{
    internal sealed class Session
    {
        internal string System;
        internal string Revision = "";
        internal HashSet<PromptCacheMessage> History = new HashSet<PromptCacheMessage>();
        internal List<PromptCacheMessage> Transcript = new List<PromptCacheMessage>();
        internal Dictionary<string, Dictionary<PromptCacheMessage, int>> Sections =
            new Dictionary<string, Dictionary<PromptCacheMessage, int>>(StringComparer.Ordinal);
        internal long Ticket;
        internal int Chars, NextSection;
        internal DateTime LastUse;
    }
    private readonly object _gate = new object();
    private readonly Dictionary<string, Session> _sessions = new Dictionary<string, Session>(StringComparer.Ordinal);
    private long _generation, _ticket;
    private const int MaxSessions = 32, MaxPoolChars = 4000000;

    internal int Count { get { lock (_gate) return _sessions.Count; } }
    internal void Clear() { lock (_gate) { _sessions.Clear(); _generation = 0; } }

    internal sealed class Lease : IDisposable
    {
        internal readonly List<PromptCacheMessage> Messages;
        internal readonly string Reason;
        internal readonly bool Warm;
        internal readonly int Chars;
        private readonly ConversationPromptCacheStore _owner;
        private readonly string _key;
        private readonly Session _session;
        private readonly long _generation, _ticket;
        private readonly Dictionary<string, Dictionary<PromptCacheMessage, int>> _sections;
        private readonly int _nextSection, _budget;
        private readonly HashSet<PromptCacheMessage> _history;
        private bool _settled;

        internal Lease(ConversationPromptCacheStore owner, string key, Session session, long generation, long ticket,
            List<PromptCacheMessage> messages, Dictionary<string, Dictionary<PromptCacheMessage, int>> sections,
            int nextSection, int budget, bool warm, string reason, HashSet<PromptCacheMessage> history = null)
        {
            _owner = owner; _key = key; _session = session; _generation = generation; _ticket = ticket;
            Messages = messages; _sections = sections; _nextSection = nextSection; _budget = budget;
            Warm = warm; Reason = reason; Chars = CountChars(messages);
            _history = history;
        }
        internal bool Commit(string acceptedReply, bool sourceCurrent, DateTime now)
        {
            lock (_owner._gate)
            {
                if (_settled) return false;
                _settled = true;
                if (!sourceCurrent || string.IsNullOrWhiteSpace(acceptedReply) || _session == null
                    || _owner._generation != _generation || _session.Ticket != _ticket
                    || !_owner._sessions.TryGetValue(_key, out Session current) || !ReferenceEquals(current, _session)) return false;
                // Check the completed turn too. Never retain an oversized transcript.
                if ((long)Chars + acceptedReply.Length > _budget)
                { _owner._sessions.Remove(_key); return false; }
                _session.Transcript = new List<PromptCacheMessage>(Messages.GetRange(1, Messages.Count - 1))
                    { new PromptCacheMessage("assistant", acceptedReply) };
                _session.Sections = _sections; _session.NextSection = _nextSection;
                if (_history != null) _session.History = _history;
                _session.Chars = Chars + acceptedReply.Length; _session.LastUse = now;
                _owner.TrimPool(_key);
                return true;
            }
        }
        public void Dispose()
        {
            lock (_owner._gate)
            {
                if (_settled) return;
                _settled = true;
                // A failed/cancelled turn has never entered the committed transcript.
                if (_session != null && _session.Ticket == _ticket && _session.Transcript.Count == 0
                    && _owner._sessions.TryGetValue(_key, out Session current) && ReferenceEquals(current, _session))
                    _owner._sessions.Remove(_key);
            }
        }
    }

    internal Lease Begin(long generation, string key, string actor, List<PromptCacheMessage> original,
        string globalRule, PromptCacheText text, int budget, int ttlMinutes, DateTime now,
        int tokenBudget = int.MaxValue, int outputTokens = 0)
    {
        lock (_gate)
        {
            if (generation < _generation)
                return new Lease(this, key, null, generation, 0, original, null, 0, budget, false, "stale_generation");
            if (_generation != generation) { _sessions.Clear(); _generation = generation; }
            if (original == null || original.Count < 2 || original[0].Role != "system"
                || original[original.Count - 1].Role != "user")
                return new Lease(this, key, null, generation, 0, original ?? new List<PromptCacheMessage>(), null, 0, budget, false, "unsupported");
            // Exact source-owned block only. No fuzzy matching, trimming or CR normalization.
            string system = original[0].Content;
            bool shared = !string.IsNullOrEmpty(globalRule) && system.StartsWith(globalRule, StringComparison.Ordinal);
            string stable = (shared ? globalRule : system) + "\n\n" + text.Policy;
            string revision = original[0].Revision;
            var history = new HashSet<PromptCacheMessage>(original.Where(x => x.HistoryGroup > 0));
            bool existed = _sessions.TryGetValue(key, out Session session);
            bool warm = existed && session.System == stable
                && session.Revision == revision && session.History.IsSubsetOf(history)
                && (now - session.LastUse).TotalMinutes <= ttlMinutes;
            string reason = warm ? "append" : existed && (session.Revision != revision || !session.History.IsSubsetOf(history))
                ? "history_rebuilt" : "new_scope";
            if (!warm)
            {
                session = new Session { System = stable, LastUse = now, Revision = revision, History = history };
                _sessions[key] = session;
                TrimPool(key);
            }
            // A new request supersedes any pending lease, without waiting on network work.
            long ticket = session.Ticket = ++_ticket;
            List<PromptCacheMessage> built = Build(session, actor, original, shared ? system.Substring(globalRule.Length) : "", text,
                out var sections, out int nextSection);
            if (!ConversationRequestBudget.Fits(built, budget, tokenBudget, outputTokens))
            {
                // Reset BEFORE de-duplication, so this very turn carries every needed section.
                session = new Session { System = stable, LastUse = now, Ticket = ticket, Revision = revision, History = history };
                _sessions[key] = session;
                built = Build(session, actor, original, shared ? system.Substring(globalRule.Length) : "", text, out sections, out nextSection);
                warm = false; reason = "budget_reset";
            }
            if (!ConversationRequestBudget.Fits(built, budget, tokenBudget, outputTokens))
            {
                // This setting bounds cache storage, not model capacity. Preserve the original
                // request in full instead of truncating rules/facts or sending a partial snapshot.
                _sessions.Remove(key);
                return new Lease(this, key, null, generation, ticket, original, null, 0, budget, false, "snapshot_over_budget");
            }
            return new Lease(this, key, session, generation, ticket, built, sections, nextSection, budget, warm, reason, history);
        }
    }

    private static List<PromptCacheMessage> Build(Session session, string actor, List<PromptCacheMessage> original,
        string systemRemainder, PromptCacheText text,
        out Dictionary<string, Dictionary<PromptCacheMessage, int>> sections, out int nextSection)
    {
        sections = new Dictionary<string, Dictionary<PromptCacheMessage, int>>(session.Sections, StringComparer.Ordinal);
        sections.TryGetValue(actor, out var previous);
        var known = previous == null ? new Dictionary<PromptCacheMessage, int>() : new Dictionary<PromptCacheMessage, int>(previous);
        sections[actor] = known;
        nextSection = session.NextSection;
        var additions = new List<PromptCacheMessage>();
        var manifest = new StringBuilder();
        var snapshot = new List<PromptCacheMessage>();
        if (systemRemainder.Length > 0) snapshot.Add(new PromptCacheMessage("system", systemRemainder));
        for (int i = 1; i < original.Count - 1; i++) snapshot.Add(original[i]);
        foreach (PromptCacheMessage part in snapshot)
        {
            if (!known.TryGetValue(part, out int id))
            {
                id = ++nextSection; known.Add(part, id);
                additions.Add(new PromptCacheMessage("user", text.Format(text.Section, actor, id.ToString(CultureInfo.InvariantCulture) + ":" + part.Role)));
                // Preserve actual assistant/user roles and bytes. System-source material is
                // explicitly identified by its section metadata under the stable policy.
                additions.Add(new PromptCacheMessage(part.Role == "system" ? "user" : part.Role, part.Content));
            }
            if (manifest.Length > 0) manifest.Append(',');
            manifest.Append(id).Append(':').Append(part.Role);
        }
        var result = new List<PromptCacheMessage>(session.Transcript.Count + additions.Count + 4)
            { new PromptCacheMessage("system", session.System) };
        result.AddRange(session.Transcript);
        result.Add(new PromptCacheMessage("user", text.Format(text.Snapshot, actor, manifest.ToString())));
        result.AddRange(additions);
        result.Add(new PromptCacheMessage("user", text.Format(text.Input, actor, "")));
        result.Add(original[original.Count - 1]);
        return result;
    }

    private static int CountChars(List<PromptCacheMessage> messages)
    {
        long count = 0;
        foreach (var message in messages) count += message.Content.Length;
        return (int)Math.Min(int.MaxValue, count);
    }
    private void TrimPool(string keep)
    {
        int total = 0;
        foreach (var item in _sessions.Values) total += item.Chars;
        while (_sessions.Count > MaxSessions || total > MaxPoolChars)
        {
            string oldestKey = null; DateTime oldest = DateTime.MaxValue;
            foreach (var item in _sessions)
                if (item.Key != keep && item.Value.LastUse < oldest) { oldest = item.Value.LastUse; oldestKey = item.Key; }
            if (oldestKey == null) break;
            total -= _sessions[oldestKey].Chars; _sessions.Remove(oldestKey);
        }
    }
}
