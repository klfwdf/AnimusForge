using System;
using System.Text.RegularExpressions;
using AnimusForge.DiplomacyDialogue;

namespace AnimusForge;

// Evaluated per matter at request/commit boundaries, never by conversation alone.
internal static class DiplomacyBilateralRouting
{
    private static readonly Regex CommitTags = new(@"\[ACTION:DIPLOMACY:COMMIT:([^\]]+)\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    internal static bool NeedsRemainingReview(string tags, string actor, string player, Func<DialogueTagPayload,bool> ownsMatter = null)
    {
        if (string.IsNullOrEmpty(tags)) return false;
        if (tags.IndexOf("[ACTION:DIPLOMACY:REVIEW_REMAINING]", StringComparison.OrdinalIgnoreCase)>=0) return true;
        foreach (Match match in CommitTags.Matches(tags))
            if (DialogueTagPayload.TryParse(match.Groups[1].Value, out var parsed)
                && !(ownsMatter?.Invoke(parsed) ?? OwnsMatter(parsed,actor,player))) return true;
        return false;
    }
    internal static bool IsDirectAction(DialogueDiplomaticAction action) => action is
        DialogueDiplomaticAction.Peace or DialogueDiplomaticAction.Alliance or DialogueDiplomaticAction.Trade
        or DialogueDiplomaticAction.DeclareWar or DialogueDiplomaticAction.BreakAlliance or DialogueDiplomaticAction.CancelTrade;

    internal static bool TryAction(string tag, out DialogueDiplomaticAction action)
    {
        action = tag?.ToUpperInvariant() switch {
            "MAKE_PEACE" => DialogueDiplomaticAction.Peace, "FORM_ALLIANCE" => DialogueDiplomaticAction.Alliance,
            "MAKE_TRADE" => DialogueDiplomaticAction.Trade, "DECLARE_WAR" => DialogueDiplomaticAction.DeclareWar,
            "BREAK_ALLIANCE" => DialogueDiplomaticAction.BreakAlliance, "CANCEL_TRADE" => DialogueDiplomaticAction.CancelTrade,
            _ => (DialogueDiplomaticAction)(-1) };
        return IsDirectAction(action);
    }

    internal static bool IsPair(string first, string second, string player, string npc) =>
        !string.IsNullOrEmpty(player) && !string.IsNullOrEmpty(npc) && !Same(player, npc)
        && (Same(first, player) && Same(second, npc) || Same(second, player) && Same(first, npc));

    internal static bool OwnsMatter(DialogueTagPayload parsed, string actor, string player) => parsed != null
        && parsed.Move is DialogueDiplomaticMove.NewMatter or DialogueDiplomaticMove.AcceptProposal
        && IsDirectAction(parsed.Action) && IsPair(actor, parsed.TargetKingdomId, player, actor);

    private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
