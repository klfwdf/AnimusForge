using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

// Request-frequency overlap only. Memory owns persistence; the final message builder owns the shared budget.
internal static class NativeMemoryHistoryMergeOwner
{
    internal static List<ConversationMessage> Merge(IEnumerable<ConversationMessage> persistent,
        IEnumerable<ConversationMessage> native, long currentPlayerEventSequence, string playerName,
        IEnumerable<ConversationMessage> pendingCurrentFacts = null)
    {
        var result = persistent?.Where(x => x != null).ToList() ?? new List<ConversationMessage>();
        var occurrences = new Dictionary<HistoryIdentity, Stack<int>>();
        for (int i = 0; i < result.Count; i++)
        {
            if (!TryIdentity(result[i], playerName, out var identity)) continue;
            if (!occurrences.TryGetValue(identity, out var positions))
                occurrences[identity] = positions = new Stack<int>();
            positions.Push(i);
        }
        ConversationMessage current = null;
        var unmatched = new List<ConversationMessage>();
        var tail = native?.Where(x => x != null).ToList() ?? new List<ConversationMessage>();
        // Align the most recent occurrences: a trimmed Native tail cannot consume an older repeat.
        for (int i = tail.Count - 1; i >= 0; i--)
        {
            var message = tail[i];
            if (currentPlayerEventSequence > 0 && message.EventSequence == currentPlayerEventSequence
                && string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase))
            {
                current = message;
                continue;
            }
            if (TryIdentity(message, playerName, out var identity)
                && occurrences.TryGetValue(identity, out var positions) && positions.Count > 0)
                result[positions.Pop()] = message;
            else
                unmatched.Add(message);
        }
        unmatched.Reverse();
        result.AddRange(unmatched);
        // Native-only entries may predate newer raw from another channel. Do not let their cache order evict it.
        result = result.Select((message, index) => new { Message = message, Index = index })
            .OrderBy(x => x.Message.GameDayIndex < 0 ? int.MaxValue : x.Message.GameDayIndex)
            .ThenBy(x => x.Message.GameHour < 0 ? 24 : x.Message.GameHour)
            .ThenBy(x => x.Index).Select(x => x.Message).ToList();
        var factIdentities = new Dictionary<HistoryIdentity, Stack<ConversationMessage>>();
        var factEvents = new Dictionary<long, ConversationMessage>();
        foreach (var fact in result)
        {
            if (!IsAfef(fact)) continue;
            fact.PromptFactScopeCaptured = true;
            fact.PromptIsCurrentFact = false;
            if (fact.EventSequence > 0) factEvents[fact.EventSequence] = fact;
            if (!TryIdentity(fact, playerName, out var identity)) continue;
            if (!factIdentities.TryGetValue(identity, out var facts))
                factIdentities[identity] = facts = new Stack<ConversationMessage>();
            facts.Push(fact);
        }
        foreach (var pending in pendingCurrentFacts ?? Enumerable.Empty<ConversationMessage>())
        {
            if (!IsAfef(pending)) continue;
            ConversationMessage matched = null;
            if (pending.EventSequence > 0 && factEvents.TryGetValue(pending.EventSequence, out var sameEvent)
                && !sameEvent.PromptIsCurrentFact && pending.GameDayIndex == sameEvent.GameDayIndex
                && pending.PromptMemorySessionKey == sameEvent.PromptMemorySessionKey)
                matched = sameEvent;
            if (matched == null && TryIdentity(pending, playerName, out var identity)
                && factIdentities.TryGetValue(identity, out var facts))
            {
                while (facts.Count > 0 && facts.Peek().PromptIsCurrentFact) facts.Pop();
                if (facts.Count > 0) matched = facts.Pop();
            }
            if (matched == null)
            {
                // Unknown provenance cannot prove overlap. Keep the explicit current fact instead of downgrading it.
                matched = SceneHistoryProjectionOwner.CapturePromptMessages(new[] { pending })[0];
                result.Add(matched);
            }
            matched.PromptFactScopeCaptured = true;
            matched.PromptIsCurrentFact = true;
        }
        if (current != null) result.Add(current);
        return result;
    }

    private static bool IsAfef(ConversationMessage message) => message != null
        && string.Equals(message.Role, "system", StringComparison.OrdinalIgnoreCase)
        && SceneHistoryMessageAssemblyOwner.TryNormalizeAfefFactLineForPrompt(message.Content, out _);

    private static bool TryIdentity(ConversationMessage message, string playerName, out HistoryIdentity identity)
    {
        identity = default;
        string session = (message.PromptMemorySessionKey ?? "").Trim();
        string scene = (message.Scene ?? "").Trim();
        string role = (message.Role ?? "").Trim().ToLowerInvariant();
        string speaker = (message.SpeakerName ?? "").Trim();
        string target = (message.TargetName ?? "").Trim();
        string content = (message.Content ?? "").Replace("\r", "").Trim();
        // Unproven old/foreign identities are retained. Content equality alone never proves overlap.
        if (session.Length == 0 || message.GameDayIndex < 0 || message.GameHour < 0
            || UncompressedMemoryMessageAssemblyOwner.IsUnknownMemorySceneLabel(scene)
            || role.Length == 0 || content.Length == 0) return false;
        if (role == "user")
        {
            bool player = ConversationRoleClassificationOwner.IsLikelyPlayerHistorySpeaker(speaker)
                || string.Equals(speaker, (playerName ?? "").Trim(), StringComparison.Ordinal);
            if (!player) return false;
            // Only remove the storage wrapper whose exact speaker/recipient are in the captured identity.
            string addressed = speaker + "对" + target + "说";
            if (target.Length > 0 && content.StartsWith(addressed + ":", StringComparison.Ordinal))
                content = content.Substring(addressed.Length + 1).Trim();
            else if (target.Length > 0 && content.StartsWith(addressed + "：", StringComparison.Ordinal))
                content = content.Substring(addressed.Length + 1).Trim();
            speaker = "player";
        }
        else if (role == "assistant")
        {
            if (speaker.Length == 0) return false;
            if (content.StartsWith(speaker + ":", StringComparison.Ordinal)
                || content.StartsWith(speaker + "：", StringComparison.Ordinal))
                content = content.Substring(speaker.Length + 1).Trim();
            target = "";
        }
        else if (role == "system")
        {
            if (!SceneHistoryMessageAssemblyOwner.TryNormalizeAfefFactLineForPrompt(content, out var fact)) return false;
            content = fact;
            speaker = "AFEF";
            target = "";
        }
        else return false;
        identity = new HistoryIdentity(session, message.GameDayIndex, message.GameHour,
            message.GameDate ?? "", scene, role, speaker, target, content);
        return true;
    }

    private readonly struct HistoryIdentity : IEquatable<HistoryIdentity>
    {
        private readonly string _session, _date, _scene, _role, _speaker, _target, _content;
        private readonly int _day, _hour;
        internal HistoryIdentity(string session, int day, int hour, string date, string scene,
            string role, string speaker, string target, string content)
        { _session = session; _day = day; _hour = hour; _date = date; _scene = scene;
            _role = role; _speaker = speaker; _target = target; _content = content; }
        public bool Equals(HistoryIdentity other) => _day == other._day && _hour == other._hour
            && _session == other._session && _date == other._date && _scene == other._scene
            && _role == other._role && _speaker == other._speaker && _target == other._target && _content == other._content;
        public override bool Equals(object obj) => obj is HistoryIdentity other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = _day * 31 + _hour;
                hash = hash * 31 + (_session?.GetHashCode() ?? 0);
                hash = hash * 31 + (_date?.GetHashCode() ?? 0);
                hash = hash * 31 + (_scene?.GetHashCode() ?? 0);
                hash = hash * 31 + (_role?.GetHashCode() ?? 0);
                hash = hash * 31 + (_speaker?.GetHashCode() ?? 0);
                hash = hash * 31 + (_target?.GetHashCode() ?? 0);
                hash = hash * 31 + (_content?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
