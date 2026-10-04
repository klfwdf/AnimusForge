using System;
using AnimusForge.DiplomacyDialogue;

namespace AnimusForge;

// Synchronous main-thread action boundaries only: never keep this scope across an
// await or enqueue. Nested legacy adapters retain their enclosing turn's identity.
internal sealed class DiplomacyDialogueSourceScope : IDisposable
{
    [ThreadStatic] private static DialogueInteractionOrigin _current;
    private readonly DialogueInteractionOrigin _previous;
    internal static DialogueInteractionOrigin Current => _current;
    internal static bool IsRelevant(string content) => !string.IsNullOrEmpty(content)
        && (content.IndexOf("DIPLOMACY", StringComparison.OrdinalIgnoreCase) >= 0
            || content.IndexOf("VASSALAGE", StringComparison.OrdinalIgnoreCase) >= 0
            || content.IndexOf("KINGDOM_ANNEX", StringComparison.OrdinalIgnoreCase) >= 0);
    internal static DialogueInteractionOrigin Capture(string content, string channel, string sessionId,
        string playerText = "", string npcText = "", string interactionId = "")
    {
        if (_current != null) return _current;
        if (!IsRelevant(content)) return null;
        return new DialogueInteractionOrigin(channel, interactionId, sessionId, playerText, npcText);
    }
    internal static DiplomacyDialogueSourceScope Begin(string content, string channel, string sessionId,
        string playerText = "", string npcText = "", string interactionId = "")
    {
        var origin = Capture(content, channel, sessionId, playerText, npcText, interactionId);
        return origin == null ? null : new DiplomacyDialogueSourceScope(origin);
    }
    internal DiplomacyDialogueSourceScope(DialogueInteractionOrigin origin)
    { _previous = _current; _current = _current ?? origin; }
    public void Dispose() { _current = _previous; }
}
