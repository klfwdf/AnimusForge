using AnimusForge;
using System;
using System.Globalization;
using AnimusForge.DiplomacyDialogue;

namespace AnimusForge;

// Handles a late COMMIT from an in-flight reply or a custom prompt after AI diplomacy is disabled.
// Reuses the native oral executors and their live authority/state checks; never alters the archive.
internal static class DiplomacyNativeCommitmentApplication
{
    internal static string Execute<TSource>(TSource source, string actorId, string payload)
        where TSource : struct, IDiplomacyOralTagSource
    {
        if (!DialogueTagPayload.TryParse(payload, out var parsed) || string.IsNullOrWhiteSpace(actorId)
            || actorId.IndexOf(':') >= 0 || parsed.TargetKingdomId.IndexOf(':') >= 0
            || parsed.Terms.TributePayerKingdomId.IndexOf(':') >= 0 || parsed.Terms.TributeReceiverKingdomId.IndexOf(':') >= 0)
            return DiplomacyMiscPromptTexts.Get("Commitment_InvalidTerms", "Diplomatic action not executed: invalid terms format.");
        if (parsed.Move == DialogueDiplomaticMove.Discussion) return "";
        if (parsed.Move != DialogueDiplomaticMove.NewMatter || !string.IsNullOrEmpty(parsed.SupersedesArrangementId)
            || !string.IsNullOrEmpty(parsed.EventRoundId))
            return DiplomacyMiscPromptTexts.Get("Commitment_AiDisabled", "AI diplomacy is disabled; cannot directly execute replies or amendments to earlier documents. Clarify specific actions and terms.");
        var terms = parsed.Terms;
        bool cession = !string.IsNullOrEmpty(terms.CessionFromKingdomId) || !string.IsNullOrEmpty(terms.CessionToKingdomId)
            || !string.IsNullOrEmpty(terms.CessionSettlementId);
        bool roles = !string.IsNullOrEmpty(terms.ReceivingKingdomId) || !string.IsNullOrEmpty(terms.JoiningKingdomId);
        bool tribute = terms.DailyTribute > 0 || !string.IsNullOrEmpty(terms.TributePayerKingdomId)
            || !string.IsNullOrEmpty(terms.TributeReceiverKingdomId);
        if (cession || roles || (parsed.Action != DialogueDiplomaticAction.Peace && tribute))
            return DiplomacyMiscPromptTexts.Get("Commitment_UnsupportedAdditionalTerms", "Diplomatic action not executed: native diplomacy cannot fully execute these additional terms. Negotiate supported terms first.");
        string pair = actorId + ":" + parsed.TargetKingdomId;
        string days = terms.DurationDays.ToString(CultureInfo.InvariantCulture);
        switch (parsed.Action)
        {
            case DialogueDiplomaticAction.DeclareWar:
                return terms.DurationDays == 0 ? source.DeclareWar(pair) : Unsupported();
            case DialogueDiplomaticAction.Peace:
                if (terms.DailyTribute > 0 && (terms.DurationDays <= 0
                    || string.IsNullOrEmpty(terms.TributePayerKingdomId) || string.IsNullOrEmpty(terms.TributeReceiverKingdomId)))
                    return DiplomacyMiscPromptTexts.Get("Commitment_TributeMissingDetails", "Diplomatic action not executed: tribute must specify payer, recipient, amount, and duration.");
                if (terms.DailyTribute == 0 && terms.DurationDays != 0) return Unsupported();
                string payer = string.IsNullOrEmpty(terms.TributePayerKingdomId) ? actorId : terms.TributePayerKingdomId;
                string receiver = string.IsNullOrEmpty(terms.TributeReceiverKingdomId) ? parsed.TargetKingdomId : terms.TributeReceiverKingdomId;
                bool ownFirst = string.Equals(payer, actorId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(receiver, parsed.TargetKingdomId, StringComparison.OrdinalIgnoreCase);
                bool ownSecond = string.Equals(receiver, actorId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(payer, parsed.TargetKingdomId, StringComparison.OrdinalIgnoreCase);
                if (!ownFirst && !ownSecond) return Unsupported();
                return source.MakePeace(payer + ":" + receiver + ":" + terms.DailyTribute.ToString(CultureInfo.InvariantCulture) + ":" + days);
            case DialogueDiplomaticAction.Alliance: return source.FormAlliance(pair + ":" + (terms.DurationDays == 0 ? "default" : days));
            case DialogueDiplomaticAction.Trade: return source.MakeTrade(pair + ":" + (terms.DurationDays == 0 ? "default" : days));
            case DialogueDiplomaticAction.BreakAlliance: return terms.DurationDays == 0 ? source.BreakAlliance(pair) : Unsupported();
            case DialogueDiplomaticAction.CancelTrade: return terms.DurationDays == 0 ? source.CancelTrade(pair) : Unsupported();
            default: return Unsupported();
        }
    }

    private static string Unsupported() => DiplomacyMiscPromptTexts.Get("Commitment_FormalPipelineRequired", "Diplomatic action not executed: this action or clause requires the formal AI diplomacy document pipeline.");
}
