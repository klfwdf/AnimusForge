using System;

namespace AnimusForge.DiplomacyDialogue;

public static class DialogueTermsValidation
{
    public static bool Validate(DialogueDiplomaticAction action, DialogueDiplomaticTerms terms, string first, string second, out string reason)
    {
        reason = "";
        if (terms == null || string.IsNullOrEmpty(first) || string.IsNullOrEmpty(second) || Same(first, second))
        { reason = "missing_terms_or_participants"; return false; }
        bool Pair(string a, string b) => (Same(a, first) && Same(b, second)) || (Same(a, second) && Same(b, first));
        bool treaty = action == DialogueDiplomaticAction.Annexation || action == DialogueDiplomaticAction.Tributary
            || action == DialogueDiplomaticAction.Garrison || action == DialogueDiplomaticAction.Vassal;
        if (treaty && !Pair(terms.ReceivingKingdomId, terms.JoiningKingdomId))
        { reason = "treaty_roles_must_match_participants"; return false; }
        if (!treaty && (!string.IsNullOrEmpty(terms.ReceivingKingdomId) || !string.IsNullOrEmpty(terms.JoiningKingdomId)))
        { reason = "unexpected_treaty_roles"; return false; }
        if (terms.DailyTribute > 0 && (action != DialogueDiplomaticAction.Peace || !Pair(terms.TributePayerKingdomId, terms.TributeReceiverKingdomId)))
        { reason = "tribute_requires_peace_participant_roles"; return false; }
        if (!string.IsNullOrEmpty(terms.CessionSettlementId)
            && (action != DialogueDiplomaticAction.Peace || !Pair(terms.CessionFromKingdomId, terms.CessionToKingdomId)))
        { reason = "cession_requires_peace_participant_roles"; return false; }
        if (string.IsNullOrEmpty(terms.CessionSettlementId)
            && (!string.IsNullOrEmpty(terms.CessionFromKingdomId) || !string.IsNullOrEmpty(terms.CessionToKingdomId)))
        { reason = "cession_settlement_required"; return false; }
        if (terms.DurationDays < 0 || terms.DurationDays > 252)
        { reason = "duration_out_of_range"; return false; }
        if (action == DialogueDiplomaticAction.Peace && ((terms.DailyTribute > 0 && terms.DurationDays == 0)
            || (terms.DailyTribute == 0 && terms.DurationDays != 0)))
        { reason = "peace_duration_requires_explicit_tribute_period"; return false; }
        if (terms.DurationDays > 0 && action != DialogueDiplomaticAction.Peace && action != DialogueDiplomaticAction.Trade)
        { reason = "action_has_no_fixed_duration"; return false; }
        return true;
    }
    private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
