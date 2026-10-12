namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free UI wording for mercy-track transitions around destructive outcomes.
/// AF adapters still own live outcome flags, plunder state, logging, and display side effects.
/// </summary>
public static class SiegeMercyTrackTransitionProfile
{
    public const uint BlockedAfterDestructiveMessageColor = 0xFFFFD27Fu;

    public const uint ReversiblePlunderStoppedMessageColor = 0xFFB6F7A8u;

    public const string ReversiblePlunderStoppedMessage = "[Siege aftermath] Plunder was superseded by subsequent mercy/reassurance orders; soldiers ceased extortion, and the aftermath will be settled under the current constructive disposition.";
    public const string ReversiblePlunderStoppedMessageEn = "[Siege aftermath] Pillage was superseded by subsequent mercy/reassurance orders; soldiers stopped demanding wealth; departure settles according to current positive disposition.";

    public static string BuildBlockedAfterDestructiveMessage(string actionName)
        => BuildBlockedAfterDestructiveMessage(actionName, false);

    public static string BuildBlockedAfterDestructiveMessage(string actionName, bool isEnglish)
    {
        string safeActionName = string.IsNullOrWhiteSpace(actionName)
            ? "Reassurance"
            : actionName.Trim();
        if (isEnglish)
        {
            return "[Siege aftermath] " + safeActionName + " cannot override an already-escalated disposition; massacre cannot revert to plunder, mercy, or relief; colonization is the highest irrevocable settlement.";
        }
        return "[Siege aftermath] " + safeActionName + " cannot supersede an escalated disposition; massacre cannot be rolled back to plunder, mercy, or relief, and repopulation is final.";
    }
}
