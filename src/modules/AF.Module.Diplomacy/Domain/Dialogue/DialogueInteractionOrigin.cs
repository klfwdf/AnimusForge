using System;

namespace AnimusForge.DiplomacyDialogue;

/// <summary>Private provenance, separate from content fingerprints and public documents.
/// Detached turns use their real trace ID; legacy turns get a stable dispatch receipt
/// plus the actual channel/session and bounded user/assistant excerpts.</summary>
public sealed class DialogueInteractionOrigin
{
    public string InteractionId { get; }
    public string Channel { get; }
    public string SessionId { get; }
    public string PlayerText { get; }
    public string NpcText { get; }
    internal string FullPlayerText { get; }
    internal string FullNpcText { get; }
    internal bool? PlayerConsentConfirmed { get; }
    public DialogueInteractionOrigin(string channel, string interactionId = "", string sessionId = "", string playerText = "", string npcText = "", bool? playerConsentConfirmed = null)
    {
        Channel = Clip(channel, 40);
        InteractionId = string.IsNullOrWhiteSpace(interactionId) ? "legacy-turn:" + Guid.NewGuid().ToString("N") : Clip(interactionId, 200);
        SessionId = Clip(sessionId, 200); PlayerText = Clip(playerText, 600); NpcText = Clip(npcText, 600);
        FullPlayerText = playerText ?? ""; FullNpcText = npcText ?? "";
        PlayerConsentConfirmed = playerConsentConfirmed;
    }
    private static string Clip(string value, int limit) => string.IsNullOrEmpty(value) ? "" : value.Substring(0, Math.Min(value.Length, limit));
}
