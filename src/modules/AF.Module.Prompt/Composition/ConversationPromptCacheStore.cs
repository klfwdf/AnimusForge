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

// Pool/lease boundary for the imported submodule algorithms. Scene transcripts
// are shared per scene/provider, as in PromptCacheSessionPool.GetOrCreateSceneSession.
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
                // This is the exact request-local core, never FIFO/system-text attribution.
                if (_candidate.IsSceneTranscript && _candidate.PendingCue != null)
                    _candidate.SentHistoryMessages.Add(_candidate.PendingCue);
                _candidate.PendingCue = null; _candidate.PendingCueText = null;
                _candidate.SentHistoryMessages.Add(PromptReassembler.CreateMessage("assistant", reply));
                _candidate.LastAssistantReply = reply; _candidate.LastActiveAt = now;
                _candidate.AwaitingReply = false;
                if ((long)Chars + reply.Length > _budget) { _owner._sessions.Remove(_key); return false; }
                _session.Core = _candidate; _session.Chars = Chars + reply.Length; _session.LastUse = now;
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
                if (_session != null && _session.Ticket == _ticket && _session.Core.SentHistoryMessages.Count == 0
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
            string reason = warm ? "append" : historyChanged ? "history_rebuilt" : "new_scope";
            Session Fresh() => new Session { Core = new ConversationSessionCache(actor, -1) {
                IsSceneTranscript = shared, SceneKey = key, GlobalRule = globalRule ?? "" },
                Policy = text.Policy, LastUse = now };
            if (!warm) { session = Fresh(); _sessions[key] = session; TrimPool(key); }
            long ticket = session.Ticket = ++_ticket;
            ConversationSessionCache candidate = session.Core.Copy();
            List<PromptCacheMessage> built = Build(candidate, original, actor);
            if (ConversationRequestBudget.Characters(built) > budget)
            {
                session = Fresh(); session.Ticket = ticket; _sessions[key] = session;
                candidate = session.Core.Copy(); built = Build(candidate, original, actor);
                warm = false; reason = "budget_reset";
            }
            if (ConversationRequestBudget.Characters(built) > budget)
            { _sessions.Remove(key); return Passthrough("snapshot_over_budget"); }
            return new Lease(this, key, actor, session, candidate, generation, ticket, built, budget, warm, reason, revision, history);
        }
    }

    private static List<PromptCacheMessage> Build(ConversationSessionCache core, List<PromptCacheMessage> original, string actor)
    {
        var source = new List<object>(original.Count);
        foreach (var item in original) source.Add(PromptReassembler.CreateMessage(item.Role, item.Content));
        List<object> built;
        if (core.IsSceneTranscript)
        {
            built = PromptReassembler.BuildSceneTranscriptMessages(core, source, actor, 0, 0, 0, out _);
        }
        else
        {
            if (core.FrozenSystemMessage == null)
            {
                PromptReassembler.InitializeSession(core, source);
                core.SourceSystem = original[0].Content;
                built = PromptReassembler.BuildInitialOptimizedMessages(core, source);
            }
            else
            {
                // A changed source system is a current tail update; never mutate the frozen prefix.
                if (core.SourceSystem != original[0].Content)
                    source.Insert(1, PromptReassembler.CreateMessage("user", PromptCacheCoreTexts.Get("Persona") + original[0].Content));
                core.SourceSystem = original[0].Content;
                built = PromptReassembler.ReassembleForCache(core, source);
            }
        }
        if (built == null) throw new InvalidOperationException("prompt_cache_core_build_failed");
        if (core.FrozenSystemMessage != null
            && PromptReassembler.TryGetRoleAndContent(core.FrozenSystemMessage, out _, out string system)
            && !system.Contains("[AF_PROMPT_CACHE_V1]\n"))
        {
            core.FrozenSystemMessage = PromptReassembler.CreateMessage("system", system + "\n\n[AF_PROMPT_CACHE_V1]\n" + PromptCacheCoreTexts.Get("Updates"));
            built[0] = core.FrozenSystemMessage;
        }
        core.LastFingerprints = PromptPrefixHasher.Build(built, out int chars);
        core.LastPromptChars = chars; core.LastPromptHash = PromptPrefixHasher.CombineHash(core.LastFingerprints);
        var result = new List<PromptCacheMessage>(built.Count);
        foreach (object item in built)
        {
            if (!PromptReassembler.TryGetRoleAndContent(item, out string role, out string content)) throw new InvalidOperationException("prompt_cache_core_message_invalid");
            result.Add(new PromptCacheMessage(role, content));
        }
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

