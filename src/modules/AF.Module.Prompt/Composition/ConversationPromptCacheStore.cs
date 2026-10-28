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

// Request-local snapshots and accepted prefix diagnostics, scoped by viewer/provider.
// The upstream history owner remains the only source of dialogue and recalled state.
internal sealed class ConversationPromptCacheStore
{
    internal sealed class Session
    {
        internal ConversationSessionCache Core;
        internal string Policy;
        internal long Ticket;
        internal int Chars;
        internal DateTime LastUse;
        internal readonly Dictionary<string, string> Revisions = new Dictionary<string, string>(StringComparer.Ordinal);
        internal readonly Dictionary<string, HashSet<PromptCacheMessage>> Histories = new Dictionary<string, HashSet<PromptCacheMessage>>(StringComparer.Ordinal);
    }
    private readonly object _gate = new object();
    private readonly Dictionary<string, Session> _sessions = new Dictionary<string, Session>(StringComparer.Ordinal);
    private long _generation, _ticket;
    internal int Count { get { lock (_gate) return _sessions.Count; } }
    internal void Clear() { lock (_gate) { _sessions.Clear(); _generation = 0; } }

    internal sealed class Lease : IDisposable
    {
        internal readonly List<PromptCacheMessage> Messages;
        internal readonly string Reason;
        internal readonly bool Warm, PrefixAppend;
        internal readonly int Chars, SharedChars;
        private readonly ConversationPromptCacheStore _owner;
        private readonly Session _session;
        private readonly ConversationSessionCache _candidate;
        private readonly string _key, _actor, _revision;
        private readonly HashSet<PromptCacheMessage> _history;
        private readonly long _generation, _ticket;
        private readonly int _budget;
        private bool _settled;

        internal Lease(ConversationPromptCacheStore owner, string key, string actor, Session session,
            ConversationSessionCache candidate, long generation, long ticket, List<PromptCacheMessage> messages,
            int budget, bool warm, string reason, string revision, HashSet<PromptCacheMessage> history)
        {
            _owner = owner; _key = key; _actor = actor; _session = session; _candidate = candidate;
            _generation = generation; _ticket = ticket; _budget = budget; _revision = revision; _history = history;
            Messages = messages; Reason = reason; Warm = warm;
            Chars = (int)Math.Min(int.MaxValue, ConversationRequestBudget.Characters(messages));
            int shared = 0;
            PrefixAppend = candidate != null && PromptPrefixHasher.IsPrefix(session.Core.LastFingerprints,
                candidate.LastFingerprints, out shared);
            SharedChars = shared;
        }
        internal bool Commit(string reply, bool sourceCurrent, DateTime now)
        {
            lock (_owner._gate)
            {
                if (_settled) return false;
                _settled = true;
                if (!sourceCurrent || string.IsNullOrWhiteSpace(reply) || _candidate == null
                    || _owner._generation != _generation || _session.Ticket != _ticket
                    || !_owner._sessions.TryGetValue(_key, out var live) || !ReferenceEquals(live, _session)) return false;
                // The history owner supplies accepted dialogue on the next request.
                // Retaining a second transcript here would replay stale runtime state
                // and could resurrect history the owner has edited or compressed.
                _candidate.LastActiveAt = now;
                if (Chars > _budget) { _owner._sessions.Remove(_key); return false; }
                _session.Core = _candidate; _session.Chars = Chars; _session.LastUse = now;
                _session.Revisions[_actor] = _revision; _session.Histories[_actor] = _history;
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
                if (_session != null && _session.Ticket == _ticket && _session.Chars == 0
                    && _owner._sessions.TryGetValue(_key, out var live) && ReferenceEquals(live, _session))
                    _owner._sessions.Remove(_key);
            }
        }
    }

    internal Lease Begin(long generation, string key, string actor, List<PromptCacheMessage> original,
        string globalRule, PromptCacheText text, int budget, int ttlMinutes, DateTime now,
        bool sceneTranscript = false)
    {
        lock (_gate)
        {
            Lease Passthrough(string reason) => new Lease(this, key, actor, null, null, generation, 0,
                original ?? new List<PromptCacheMessage>(), budget, false, reason, "", null);
            if (generation < _generation) return Passthrough("stale_generation");
            if (_generation != generation) { _sessions.Clear(); _generation = generation; }
            if (original == null || original.Count < 2 || original[0].Role != "system"
                || original[original.Count - 1].Role != "user") return Passthrough("unsupported");
            // Original submodule fallback: no configured rule => frozen system per NPC.
            bool shared = sceneTranscript && !string.IsNullOrWhiteSpace(globalRule)
                && original[0].Content.Contains(globalRule);
            if (sceneTranscript && !shared) key += "\nactor:" + actor;
            string revision = original[0].Revision;
            var history = new HashSet<PromptCacheMessage>(original.Where(x => x.HistoryGroup > 0));
            bool existed = _sessions.TryGetValue(key, out Session session);
            bool historyChanged = existed && session.Revisions.TryGetValue(actor, out string previousRevision)
                && (previousRevision != revision || !session.Histories[actor].IsSubsetOf(history));
            bool warm = existed && !historyChanged && session.Policy == text.Policy
                && session.Core.GlobalRule == (globalRule ?? "")
                && (now - session.LastUse).TotalMinutes <= ttlMinutes;
            string reason = warm ? "snapshot_replace" : historyChanged ? "history_rebuilt" : "new_scope";
            Session Fresh() => new Session { Core = new ConversationSessionCache(actor, -1) {
                IsSceneTranscript = shared, SceneKey = key, GlobalRule = globalRule ?? "" },
                Policy = text.Policy, LastUse = now };
            if (!warm) { session = Fresh(); _sessions[key] = session; TrimPool(key); }
            long ticket = session.Ticket = ++_ticket;
            ConversationSessionCache candidate = session.Core.Copy();
            List<PromptCacheMessage> built = Build(candidate, original, actor);
            if (ConversationRequestBudget.Characters(built) > budget)
            { _sessions.Remove(key); return Passthrough("snapshot_over_budget"); }
            return new Lease(this, key, actor, session, candidate, generation, ticket, built, budget, warm, reason, revision, history);
        }
    }

    private static List<PromptCacheMessage> Build(ConversationSessionCache core, List<PromptCacheMessage> original, string actor)
    {
        // The channel's complete, authoritative request is the replacement snapshot.
        // Never infer state/history boundaries from headings in player-authored text,
        // and never feed a previous optimized request back into the next request.
        // Unchanged system text is reused byte for byte; changed persona/state replaces
        // its old version instead of sending both versions plus a removal manifest.
        string system = original[0].Content;
        if (core.FrozenSystemMessage == null || core.SourceSystem != system)
        {
            core.SourceSystem = system;
            core.FrozenSystemMessage = new PromptCacheMessage("system",
                system + "\n\n[AF_PROMPT_CACHE_V1]\n" + PromptCacheCoreTexts.Get("Updates"));
        }
        var result = new List<PromptCacheMessage>(original.Count) { (PromptCacheMessage)core.FrozenSystemMessage };
        for (int i = 1; i < original.Count; i++) result.Add(original[i]);
        core.LastFingerprints = PromptPrefixHasher.Build(result, out int chars);
        core.LastPromptChars = chars; core.LastPromptHash = PromptPrefixHasher.CombineHash(core.LastFingerprints);
        return result;
    }

    private void TrimPool(string keep)
    {
        long total = _sessions.Values.Sum(x => (long)x.Chars);
        while (_sessions.Count > 32 || total > 4000000)
        {
            // Single scan avoids sorting the bounded pool on every trim.
            KeyValuePair<string, Session> oldest = default;
            bool found = false;
            foreach (var pair in _sessions)
            {
                if (pair.Key == keep) continue;
                if (!found || pair.Value.LastUse < oldest.Value.LastUse)
                {
                    oldest = pair; found = true;
                }
            }
            if (!found) break;
            total -= oldest.Value.Chars; _sessions.Remove(oldest.Key);
        }
    }
}

