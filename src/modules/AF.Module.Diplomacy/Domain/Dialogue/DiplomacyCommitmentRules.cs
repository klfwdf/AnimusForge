using System;

namespace AnimusForge.DiplomacyDialogue;

// Accepted decisions are authoritative. Drafts, later conversations and transport
// completion order cannot renegotiate them. Called at submission/commit boundaries.
internal static class DiplomacyCommitmentRules
{
    internal static bool IsPending(WorldDiplomacyDialogueArrangement item) => item != null
        && (item.Status == "accepted" || item.Status == "deferred" || item.Status == "execution_uncertain");

    internal static bool Conflicts(WorldDiplomacyDialogueArrangement existing, DialogueCommitmentRequest next)
    {
        if (!IsPending(existing) || next == null || existing.ActorKingdomId != next.ActorKingdomId
            || existing.TargetKingdomId != next.TargetKingdomId) return false;
        if (existing.Action == next.Action)
            return existing.Move != next.Move || !existing.Terms.ToTerms().Equals(next.Terms);
        bool WarPeace(DialogueDiplomaticAction a) => a == DialogueDiplomaticAction.Peace || a == DialogueDiplomaticAction.DeclareWar;
        bool Alliance(DialogueDiplomaticAction a) => a == DialogueDiplomaticAction.Alliance || a == DialogueDiplomaticAction.BreakAlliance;
        bool Trade(DialogueDiplomaticAction a) => a == DialogueDiplomaticAction.Trade || a == DialogueDiplomaticAction.CancelTrade;
        return (WarPeace(existing.Action) && WarPeace(next.Action))
            || (Alliance(existing.Action) && Alliance(next.Action)) || (Trade(existing.Action) && Trade(next.Action));
    }

    // Bounded backoff, never an infinite deadline. A failed attempt retains the
    // exact agreement; no additional model request is made until the due day.
    internal static int RetryDay(int day, int attempts) => day + Math.Min(7, Math.Max(1, attempts));
}
