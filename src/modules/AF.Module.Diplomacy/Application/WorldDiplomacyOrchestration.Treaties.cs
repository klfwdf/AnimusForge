using System;
using System.Linq;
using AnimusForge.DiplomacyDialogue;
using Newtonsoft.Json.Linq;

using AnimusForge.Refactor.Domain;
using static AnimusForge.Refactor.Domain.WorldDiplomacyIntentVocabulary;
using static AnimusForge.Refactor.Domain.WorldDiplomacyTextRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyRoundLifecycleRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyStructureRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyEnvelopeJsonRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyDocumentFactRules;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    private static bool IsFormalTreatyIntent(string intent) => WorldDiplomacyIntentVocabulary.IsFormalTreatyIntent(intent);

    private bool ValidateFormalTreatyTerms(string intent, DialogueDiplomaticTerms terms, string author, string target, out string reason)
    {
        reason = "";
        string kind = NormalizeIntent(intent).Replace("propose_", "").Replace("accept_", "").Replace("reject_", "");
        if (Enum.TryParse(kind, true, out DialogueDiplomaticAction action)
            && !NormalizeIntent(intent).StartsWith("reject_")
            && !DialogueTermsValidation.Validate(action, terms, author, target, out reason)) return false;
        if (kind == "peace" && !NormalizeIntent(intent).StartsWith("reject_"))
        {
            // Reuse the exact source-term validator before a new public artifact
            // exists. This value is a validation input, never registered/published.
            var validationInput = new WorldDiplomacyDocument { AuthorKingdomId = author,
                IsReadyForPublication = true, PeaceTerms = WorldDiplomacyDialogueTerms.From(terms).ToPeaceTerms() };
            if (!AreOfferedPeaceTermsCurrentlyExecutable(new WorldDiplomacyRoundOffer(), validationInput))
            { reason = "peace_terms_not_currently_executable_without_changes"; return false; }
        }
        if (!IsFormalTreatyIntent(intent)) return true;
        if (author == null || target == null || author == target || _host.IsEliminatedParty(author) || _host.IsEliminatedParty(target)
            || DialogueHost?.RulerAlive(_host.PartyRulerId(author)) != true || DialogueHost?.RulerAlive(_host.PartyRulerId(target)) != true)
        { reason = "treaty_participants_not_available"; return false; }
        if (NormalizeIntent(intent).StartsWith("reject_")) return true;
        if (terms == null || !((terms.ReceivingKingdomId == author && terms.JoiningKingdomId == target)
            || (terms.ReceivingKingdomId == target && terms.JoiningKingdomId == author)))
        { reason = "treaty_requires_explicit_receiving_and_joining_roles"; return false; }
        // These treaty types use the existing type-specific tribute/military
        // rules. Extra clauses must not be promised and then silently dropped.
        if (terms.DailyTribute != 0 || terms.DurationDays != 0 || !string.IsNullOrEmpty(terms.CessionSettlementId))
        { reason = "unsupported_extra_treaty_clause"; return false; }
        var snapshot = DialogueHost?.TreatyState(terms.ReceivingKingdomId, terms.JoiningKingdomId) ?? default;
        if (!snapshot.available) { reason = "vassalage_runtime_unavailable"; return false; }
        if (snapshot.cycle) { reason = "treaty_would_create_cycle"; return false; }
        if (snapshot.existing && !NormalizeIntent(intent).EndsWith("annexation"))
        { reason = "subject_already_has_treaty"; return false; }
        return true;
    }

    private WorldDiplomacyDialogueTerms ParseFormalTreatyTerms(JObject json, string intent, string author, string target)
    {
        if (!IsFormalTreatyIntent(intent) || json == null) return null;
        JObject value = json["treaty_terms"] as JObject;
        if (value == null) return null;
        var peace = json["peace_terms"] as JObject;
        int.TryParse(peace?["daily_tribute"]?.ToString(), out int tribute);
        int.TryParse(peace?["duration_days"]?.ToString(), out int days);
        var terms = new DialogueDiplomaticTerms(ReadString(value, "receiving_kingdom_id"), ReadString(value, "joining_kingdom_id"),
            dailyTribute: Math.Max(0, tribute), durationDays: Math.Max(0, days), cessionSettlementId: peace?["cession_settlement_id"]?.ToString());
        return ValidateFormalTreatyTerms(intent, terms, author, target, out _) ? WorldDiplomacyDialogueTerms.From(terms) : null;
    }

    internal bool ValidateFormalTreatyDeclaration(WorldDiplomacyDocument document, string intent, WorldDiplomacyDialogueTerms terms,
        string author, string target, string sourceId, string sourceActionId, out string reason)
    {
        reason = "";
        if (!IsFormalTreatyIntent(intent)) return true;
        if (NormalizeIntent(intent).StartsWith("reject_")) return true;
        if (NormalizeIntent(intent).StartsWith("accept_"))
        {
            var original = ResolveDialogueTerms(ResolveDocument(sourceId), sourceActionId);
            if (terms != null && !terms.ToTerms().Equals(original))
            { reason = "treaty_acceptance_cannot_change_source_roles"; return false; }
            return ValidateFormalTreatyTerms(intent, original, author, target, out reason);
        }
        if (!ValidateFormalTreatyTerms(intent, terms?.ToTerms(), author, target, out reason)) return false;
        var receiving = ResolveDialogueParty(terms.ReceivingKingdomId); var joining = ResolveDialogueParty(terms.JoiningKingdomId);
        string pattern = NormalizeIntent(intent).EndsWith("annexation") ? "并入|归入|加入|吞并"
            : "臣服|臣属|附庸|宗主|朝贡|驻军|卫戍";
        if (!ContainsDirectedPeaceTerm(document.Body, _host.PartyNameOrEmpty(joining), joining == author, joining == target,
            _host.PartyNameOrEmpty(receiving), receiving == author, receiving == target, pattern))
        { reason = "treaty_roles_not_disclosed_in_public_body"; return false; }
        return true;
    }

    private WorldDiplomacyOfferOutcome ExecuteFormalTreatyAcceptance(WorldDiplomacyDocument response, WorldDiplomacyDocument source,
        WorldDiplomacyRoundOffer offer, string proposer, string responder)
    {
        var terms = ResolveDialogueTerms(source, offer.SourceActionId);
        if (!ValidateFormalTreatyTerms(offer.Intent, terms, proposer, responder, out string reason))
        { response.MechanicalResult = "条约未执行：" + reason; return WorldDiplomacyOfferOutcome.Invalidated; }
        response.TreatyTerms = WorldDiplomacyDialogueTerms.From(terms);
        var receipt = DialogueHost?.ExecuteTreaty(NormalizeIntent(offer.Intent), terms.ReceivingKingdomId, terms.JoiningKingdomId)
            ?? (success: false, changed: false, reason: "treaty_runtime_unavailable");
        response.ChangedDiplomaticState = receipt.changed;
        response.MechanicalResult = !receipt.success && receipt.changed
            ? "并入交割失败，已发生的部分转移不重复执行：" + receipt.reason : receipt.reason;
        return !receipt.changed ? WorldDiplomacyOfferOutcome.Failed : receipt.success ? WorldDiplomacyOfferOutcome.Applied : WorldDiplomacyOfferOutcome.Partial;
    }
    internal WorldDiplomacyOfferOutcome ExecuteFormalTreatyOffer(string intent, WorldDiplomacyRoundOffer offer,
        WorldDiplomacyDocument source, WorldDiplomacyDocument response)
    {
        return ExecuteFormalTreatyAcceptance(response, source, offer, offer.ProposerKingdomId, offer.TargetKingdomId);
    }
}
