using AnimusForge;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Refactor.Domain;

public static class WorldDiplomacyGenerationValidationRules
{
    public static bool TryGetPublicPeaceTermsDisclosureViolation(
        string intent,
        string visibleText,
        JObject json,
        string authorId,
        string targetId,
        Func<string, (string kingdomId, string name)> resolveKingdomIdentity,
        Func<string, string> settlementName,
        out string reason)
{
    reason = "";
    string normalized = WorldDiplomacyIntentVocabulary.NormalizeIntent(intent);
    string text = visibleText ?? "";
    if (normalized != "propose_peace" || json?.SelectToken("peace_terms") is not JObject terms) return false;
    int.TryParse(terms["daily_tribute"]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int tribute);
    int.TryParse(terms["duration_days"]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int duration);
    if (tribute > 0 && !WorldDiplomacyTextRules.ContainsWholeNumber(text, tribute))
    {
        reason = "peace_terms_not_visible:tribute";
        return true;
    }
    if (duration > 0 && !WorldDiplomacyDurationDisclosure.ContainsDays(text, duration))
    {
        reason = "peace_terms_not_visible:duration";
        return true;
    }
    (string payerId, string payerName) = resolveKingdomIdentity?.Invoke((terms["tribute_payer_kingdom_id"]?.ToString() ?? "").Trim()) ?? (null, null);
    (string receiverId, string receiverName) = resolveKingdomIdentity?.Invoke((terms["tribute_receiver_kingdom_id"]?.ToString() ?? "").Trim()) ?? (null, null);
    if (tribute > 0 && payerId != null && receiverId != null
        && !WorldDiplomacyDocumentFactRules.ContainsDirectedPeaceTerm(text,
            payerName ?? "", string.Equals(payerId, authorId, StringComparison.OrdinalIgnoreCase), string.Equals(payerId, targetId, StringComparison.OrdinalIgnoreCase),
            receiverName ?? "", string.Equals(receiverId, authorId, StringComparison.OrdinalIgnoreCase), string.Equals(receiverId, targetId, StringComparison.OrdinalIgnoreCase),
            DiplomacyCompatibilityTexts.Get("Regex_PayKeywords")))
    {
        reason = "peace_terms_not_visible:tribute_direction";
        return true;
    }
    string settlementId = (terms["cession_settlement_id"]?.ToString() ?? "").Trim();
    string resolvedSettlementName = settlementId.Length == 0 ? null : settlementName?.Invoke(settlementId);
    if (!string.IsNullOrWhiteSpace(settlementId)
        && text.IndexOf(settlementId, StringComparison.OrdinalIgnoreCase) < 0
        && (resolvedSettlementName == null || text.IndexOf(resolvedSettlementName, StringComparison.OrdinalIgnoreCase) < 0))
    {
        reason = "peace_terms_not_visible:cession";
        return true;
    }
    (string cessionFromId, string cessionFromName) = resolveKingdomIdentity?.Invoke((terms["cession_from_kingdom_id"]?.ToString() ?? "").Trim()) ?? (null, null);
    (string cessionToId, string cessionToName) = resolveKingdomIdentity?.Invoke((terms["cession_to_kingdom_id"]?.ToString() ?? "").Trim()) ?? (null, null);
    if (resolvedSettlementName != null && cessionFromId != null && cessionToId != null
        && !WorldDiplomacyDocumentFactRules.ContainsDirectedPeaceTerm(text,
            cessionFromName ?? "", string.Equals(cessionFromId, authorId, StringComparison.OrdinalIgnoreCase), string.Equals(cessionFromId, targetId, StringComparison.OrdinalIgnoreCase),
            cessionToName ?? "", string.Equals(cessionToId, authorId, StringComparison.OrdinalIgnoreCase), string.Equals(cessionToId, targetId, StringComparison.OrdinalIgnoreCase),
            DiplomacyCompatibilityTexts.Get("Regex_CedeKeywords")))
    {
        reason = "peace_terms_not_visible:cession_direction";
        return true;
    }
    return false;
}
    public static bool TryGetDiplomaticStateViolation(
        string intent,
        string authorId,
        string targetId,
        List<WorldDiplomacyThreat> diplomaticThreats,
        bool atWar,
        bool allied,
        bool trading,
        bool allianceAvailable,
        bool tradeAvailable,
        Func<bool, (bool legal, string reason)> declareWarVerdict,
        Func<WorldDiplomacyOfferCooldownKey, int> offerCooldownLastFailedDay,
        int offerCooldownDays,
        int currentDay,
        out string reason)
{
    reason = "";
    string normalized = WorldDiplomacyIntentVocabulary.NormalizeIntent(intent);
    if (authorId == null) return false;
    if (targetId == null) return false;
    switch (normalized)
    {
    case "declare_war":
            var declareWar = declareWarVerdict?.Invoke(false) ?? (false, "declare_war_check_unavailable");
            if (!declareWar.legal)
            {
                reason = "declare_war_not_legal:" + declareWar.reason;
                return true;
            }
            break;
        case "break_alliance":
            if (!allianceAvailable) { reason = "alliance_system_unavailable"; return true; }
            if (!allied) { reason = "break_alliance_without_alliance"; return true; }
            break;
        case "cancel_trade":
            if (!tradeAvailable) { reason = "trade_system_unavailable"; return true; }
            if (!trading) { reason = "cancel_trade_without_trade_agreement"; return true; }
            break;
        case "propose_peace":
        case "accept_peace":
        case "reject_peace":
            if (!atWar) { reason = "peace_intent_between_kingdoms_not_at_war"; return true; }
            break;
        case "propose_alliance":
            if (!allianceAvailable) { reason = "alliance_system_unavailable"; return true; }
            if (atWar || allied) { reason = "alliance_intent_conflicts_with_current_state"; return true; }
            break;
        case "accept_alliance":
            if (!allianceAvailable) { reason = "alliance_system_unavailable"; return true; }
            if (atWar || allied) { reason = "alliance_intent_conflicts_with_current_state"; return true; }
            break;
        case "propose_trade":
            if (!tradeAvailable) { reason = "trade_system_unavailable"; return true; }
            if (atWar || trading) { reason = "trade_intent_conflicts_with_current_state"; return true; }
            break;
        case "accept_trade":
            if (!tradeAvailable) { reason = "trade_system_unavailable"; return true; }
            if (atWar || trading) { reason = "trade_intent_conflicts_with_current_state"; return true; }
            break;
    }
    return false;
}
    public static bool TryGetDiplomaticThreatIntentViolation(
        string intent,
        bool hasAuthor,
        bool hasTarget,
        bool sameKingdom,
        string authorId,
        string targetId,
        string claimedThreatDocumentId,
        List<WorldDiplomacyThreat> diplomaticThreats,
        bool partiesAtWar,
        Func<(bool legal, string reason)> issueThreatVerdict,
        out string reason)
{
    reason = "";
    string normalized = WorldDiplomacyIntentVocabulary.NormalizeIntent(intent);
    if (!WorldDiplomacyRoundLifecycleRules.IsThreatIntent(normalized)) return false;
    reason = WorldDiplomacyRoundLifecycleRules.EvaluateThreatPartyEligibility(
        hasAuthor, hasTarget, sameKingdom);
    if (reason != "") return true;
    if (normalized == "comply_ultimatum")
    {
        WorldDiplomacyThreat incoming = partiesAtWar
            ? null
            : WorldDiplomacyRoundLifecycleRules.SelectOpenThreatBetween(diplomaticThreats, targetId, authorId);
        reason = WorldDiplomacyRoundLifecycleRules.EvaluateComplyUltimatumViolation(
            partiesAtWar, incoming, claimedThreatDocumentId);
        return reason != "";
    }

    // A warning or ultimatum is a declaration. It does not create a scripted
    // next-turn obligation or require a prior stage. Explicit executable terms
    // use the shared proposal/commitment owners.
    return false;
}
    public static bool TryGetRealmIdentityViolation(
        string currentRulerTitle,
        string rulerName,
        string visibleText,
        out string reason)
{
    reason = "";
    if (string.IsNullOrWhiteSpace(visibleText)) return false;
    // Only check known title contradictions when the live game actually supplies
    // an imperial title. IDs and world-book prose cannot establish this premise.
    if (string.IsNullOrWhiteSpace(currentRulerTitle)
        || !Regex.IsMatch(currentRulerTitle.Trim(), DiplomacyCompatibilityTexts.Get("Regex_ImperialRulerTitle"), RegexOptions.CultureInvariant)) return false;

    if (!string.IsNullOrWhiteSpace(rulerName))
    {
        string escapedName = Regex.Escape(rulerName);
        string invalidPersonalTitlePattern = "(?:" + escapedName + DiplomacyCompatibilityTexts.Get("Regex_NobleTitleList") + "|" + DiplomacyCompatibilityTexts.Get("Regex_NobleTitleList") + DiplomacyCompatibilityTexts.Get("Regex_HonorificSuffix") + escapedName + "|" + escapedName + DiplomacyCompatibilityTexts.Get("Regex_SelfTitlePrefix") + DiplomacyCompatibilityTexts.Get("Regex_NobleTitleList") + ")";
        invalidPersonalTitlePattern += "|" + DiplomacyCompatibilityTexts.Get("Regex_ImperialPersonalTitleEnglish").Replace("{RULER}", escapedName);
        if (Regex.IsMatch(visibleText, invalidPersonalTitlePattern, RegexOptions.CultureInvariant))
        {
            reason = "realm_ruler_title_conflicts_with_hard_fact";
            return true;
        }
    }

    return false;
}

    public static bool TryGetPlayerWorldStateIntentViolation(
        WorldDiplomacyDocument document,
        string intent,
        string commitment,
        string authorKingdomId,
        string targetKingdomId,
        bool partiesEligible,
        Func<string, (bool Violation, string Reason)> stateViolation,
        Func<string, string, (bool Violation, string Reason)> threatViolation,
        Func<string, WorldDiplomacyRound> resolveRound,
        Func<string, WorldDiplomacyDocument> resolveDocument,
        out string reason, bool acceptedOralAgreement = false)
    {
        reason = "";
        string normalizedIntent = WorldDiplomacyIntentVocabulary.NormalizeIntent(intent);
        string proposalIntent = WorldDiplomacyIntentVocabulary.ResponseIntentToProposalIntent(normalizedIntent);
        bool isOfferResponse = !string.IsNullOrWhiteSpace(proposalIntent);
        bool isNewProposal = WorldDiplomacyIntentVocabulary.IsProposalIntent(normalizedIntent);
        bool isQualitativeCommitment = normalizedIntent is "ultimatum" or "apology" or "concession"
            || (normalizedIntent == "warning" && !string.IsNullOrWhiteSpace(targetKingdomId));
        bool hasMechanicalEffect = WorldDiplomacyIntentVocabulary.IsImmediateIntent(normalizedIntent) || isOfferResponse || isNewProposal || isQualitativeCommitment;
        if (!hasMechanicalEffect) return false;
        if (document == null || !partiesEligible)
        {
            reason = "player_action_has_no_eligible_parties";
            return true;
        }
        if (!WorldDiplomacyOfferContractRules.CommitmentMatchesIntent(normalizedIntent, commitment))
        {
            reason = "player_action_commitment_mismatch";
            return true;
        }
        if (normalizedIntent == "comply_ultimatum" && string.IsNullOrWhiteSpace(document.RespondingToThreatDocumentId))
        {
            reason = "player_compliance_missing_source_threat";
            return true;
        }
        if (normalizedIntent == "comply_ultimatum"
            && !(document.PresentedThreatDocumentIds ?? new List<string>()).Contains(document.RespondingToThreatDocumentId, StringComparer.OrdinalIgnoreCase))
        {
            reason = "player_compliance_source_not_presented";
            return true;
        }
        if (normalizedIntent == "comply_ultimatum" && !string.IsNullOrWhiteSpace(document.RespondingToOfferDocumentId))
        {
            reason = "player_compliance_claims_offer_source";
            return true;
        }
        if (normalizedIntent != "comply_ultimatum" && !string.IsNullOrWhiteSpace(document.RespondingToThreatDocumentId))
        {
            reason = "player_non_compliance_claims_threat_source";
            return true;
        }
        if (!isOfferResponse && !string.IsNullOrWhiteSpace(document.RespondingToOfferDocumentId))
        {
            reason = "player_non_response_claims_offer_source";
            return true;
        }
        (bool Violation, string Reason) stateResult =
            stateViolation == null ? (false, "") : stateViolation(normalizedIntent);
        if (stateResult.Violation)
        {
            reason = "player_action_" + stateResult.Reason;
            return true;
        }
        (bool Violation, string Reason) threatResult = threatViolation == null
            ? (false, "")
            : threatViolation(normalizedIntent, document.RespondingToThreatDocumentId);
        if (threatResult.Violation)
        {
            reason = "player_action_" + threatResult.Reason;
            return true;
        }
        if (!isOfferResponse || acceptedOralAgreement) return false;
        if (string.IsNullOrWhiteSpace(document.RespondingToOfferDocumentId))
        {
            reason = "player_offer_response_missing_source_offer";
            return true;
        }
        WorldDiplomacyRound round = resolveRound?.Invoke(document.RoundId);
        bool hasExactOpenOffer = round?.PendingOffers?.Any(x => x != null
            && WorldDiplomacyRoundLifecycleRules.IsOpenDirectedOffer(x, targetKingdomId, authorKingdomId)
            && string.Equals(WorldDiplomacyIntentVocabulary.NormalizeIntent(x.Intent), proposalIntent, StringComparison.OrdinalIgnoreCase)
            && WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(x.SourceDocumentId, document.RespondingToOfferDocumentId)
            && string.Equals(x.SourceActionId ?? "", document.RespondingToOfferActionId ?? "", StringComparison.Ordinal)) == true;
        if (!hasExactOpenOffer)
        {
            reason = "player_offer_response_without_exact_open_offer";
            return true;
        }
        if (normalizedIntent == "accept_peace")
        {
            WorldDiplomacyDocument source = resolveDocument?.Invoke(document.RespondingToOfferDocumentId);
            WorldDiplomacyPeaceTerms offeredTerms = WorldDiplomacyDocumentFactRules.ResolveOfferedPeaceTerms(
                source,
                document.RespondingToOfferActionId);
            if (!WorldDiplomacyOfferContractRules.ArePeaceTermsEquivalent(document.PeaceTerms, offeredTerms))
            {
                reason = "player_accept_peace_changes_offer_terms";
                return true;
            }
        }
        return false;
    }

    public static bool TryGetGeneratedIntentLegalityViolation(
        WorldDiplomacyJob job, JObject json,
        Func<JObject, bool, (bool Violation, string TargetKingdomId, string Reason)> singleActionViolation,
        Func<string, WorldDiplomacyDocument> resolveDocument,
        out string generatedTargetKingdomId, out string reason)
    {
        generatedTargetKingdomId = null;
        reason = "";
        if (job == null || json == null || json["actions"] is not JArray actions
            || actions.Count < 1 || actions.Count > 64)
        {
            reason = "diplomatic_actions_envelope_invalid";
            return true;
        }
        if (!DiplomacyConditionalExecutionRules.Validate(actions, resolveDocument, out reason)) return true;
        if (!DiplomacyConditionalExecutionRules.ValidateImmediateWar(actions, out reason)) return true;
        HashSet<string> targetIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var actionKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < actions.Count; index++)
        {
            if (actions[index] is not JObject action)
            {
                reason = "diplomatic_action_entry_invalid";
                return true;
            }
            // JSON null denotes absent optional terms; actual objects still pass
            // through the original offer, exact-terms and execution guards below.
            string invalidTerms = action["peace_terms"] is JToken peace && peace.Type != JTokenType.Null && peace is not JObject
                ? "peace_terms"
                : action["treaty_terms"] is JToken treaty && treaty.Type != JTokenType.Null && treaty is not JObject ? "treaty_terms" : null;
            if (invalidTerms != null)
            {
                generatedTargetKingdomId = WorldDiplomacyEnvelopeJsonRules.ReadString(action, "target_kingdom_id", "target");
                reason = "action[" + index.ToString(CultureInfo.InvariantCulture) + "]:diplomatic_action_terms_shape_invalid:" + invalidTerms;
                return true;
            }
            string targetId = WorldDiplomacyEnvelopeJsonRules.ReadString(action, "target_kingdom_id", "target");
            if (string.IsNullOrWhiteSpace(targetId))
            {
                reason = "diplomatic_action_target_missing_or_duplicate";
                return true;
            }
            targetIds.Add(targetId);
            string intent = WorldDiplomacyIntentVocabulary.NormalizeIntent(WorldDiplomacyEnvelopeJsonRules.ReadString(action, "intent", "author_intent.intent"));
            if (!actionKeys.Add(targetId + "|" + intent))
            { reason = "duplicate_diplomatic_action"; return true; }
            JObject single = WorldDiplomacyEnvelopeJsonRules.BuildGeneratedSingleActionEnvelope(json, action);
            (bool Violation, string TargetKingdomId, string Reason) actionResult =
                singleActionViolation == null
                    ? (false, "", "")
                    : singleActionViolation(single, actions.Count == 1);
            if (actionResult.Violation)
            {
                reason = "action[" + index.ToString(CultureInfo.InvariantCulture) + "]:" + actionResult.Reason;
                generatedTargetKingdomId = actionResult.TargetKingdomId;
                return true;
            }
            string actionTargetKingdomId = actionResult.TargetKingdomId;
            WorldDiplomacyEnvelopeJsonRules.CopyDerivedGeneratedActionEnvelope(single, action);
            if (index == 0) generatedTargetKingdomId = actionTargetKingdomId;
        }
        if (WorldDiplomacyDocumentFactRules.GeneratedActionsHaveUnsafeMultiplePeaceAcceptances(actions, resolveDocument))
        {
            reason = "multiple_peace_acceptances_have_cross_terms";
            return true;
        }
        WorldDiplomacyEnvelopeJsonRules.MirrorFirstGeneratedActionEnvelope(json, actions);
        json["addressed_kingdom_ids"] = new JArray(targetIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        return false;
    }

    public static bool TryGetGeneratedSingleActionLegalityViolation(
        WorldDiplomacyJob job, JObject json, string authorId, string fallbackTargetId,
        Func<string, string> resolveKingdomId, Func<string, bool> kingdomEliminated,
        Func<string, bool> kingdomHasAuthority, Func<string, string, bool> isAtWar,
        Func<string, WorldDiplomacyRound> resolveRound, Func<string, WorldDiplomacyDocument> resolveDocument,
        Func<WorldDiplomacyRound, string, string, (bool Violation, string Reason)> deriveStructure,
        Func<string, string, (bool Violation, string Reason)> stateViolation,
        Func<string, string, string, (bool Violation, string Reason)> threatViolation,
        Func<JObject, string, WorldDiplomacyPeaceTerms> parsePeaceTerms,
        Func<string, string, string, (bool Violation, string Reason)> peaceDisclosureViolation,
        Func<string, (bool Violation, string Reason)> realmIdentityViolation,
        Action<string> log, out string generatedTargetId, out string reason)
    {

            generatedTargetId = null;
            reason = "";
            if (job == null || json == null || authorId == null)
            {
                reason = "semantic_envelope_incomplete";
                return true;
            }
            if (!(json["author_intent"] is JObject)
                || !WorldDiplomacyEnvelopeJsonRules.IsJsonStringArray(json["addressed_kingdom_ids"])
                || !WorldDiplomacyEnvelopeJsonRules.IsJsonStringArray(json["mentioned_kingdom_ids"])
                || !(json["peace_terms"] is JObject)
                || json["primary_target_kingdom_id"] == null
                || json["requires_response"] == null
                || json["tone"] == null
                || json["confidence"] == null
                || string.IsNullOrWhiteSpace(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "body", "public_document", "document")))
            {
                reason = "semantic_envelope_incomplete";
                return true;
            }
            string intent = WorldDiplomacyIntentVocabulary.NormalizeIntent(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "author_intent.intent", "intent", "author_intent"));
            string commitment = WorldDiplomacyIntentVocabulary.NormalizeCommitment(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "author_intent.commitment", "commitment"));
            if (string.IsNullOrWhiteSpace(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "author_intent.commitment", "commitment")))
                commitment = WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent(intent);
            if (json["author_intent"] is JObject generatedIntentEnvelope)
            {
                generatedIntentEnvelope["intent"] = intent;
                generatedIntentEnvelope["commitment"] = commitment;
            }
            WorldDiplomacyRound owningRound = resolveRound?.Invoke(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(job.RoundId, job.ExchangeId));
            if (!WorldDiplomacyIntentVocabulary.IsSupportedDiplomacyIntent(intent) || !WorldDiplomacyIntentVocabulary.IsSupportedCommitment(commitment))
            {
                reason = "unsupported_intent_or_commitment";
                return true;
            }
            string title = WorldDiplomacyEnvelopeJsonRules.ReadString(json, "title");
            string body = WorldDiplomacyEnvelopeJsonRules.ReadString(json, "body", "public_document", "document");
            string visibleText = title + "\n" + body;
            string targetId = WorldDiplomacyEnvelopeJsonRules.ReadString(json, "primary_target_kingdom_id", "target_kingdom_id", "target");
            if (!string.IsNullOrWhiteSpace(targetId))
            {
                generatedTargetId = resolveKingdomId?.Invoke(targetId);
                if (generatedTargetId == null)
                {
                    reason = "target_kingdom_not_found";
                    return true;
                }
            }
            else if (!job.AllowUntargeted)
            {
                generatedTargetId = fallbackTargetId;
            }
            if (string.Equals(generatedTargetId, authorId, StringComparison.OrdinalIgnoreCase)
                || (generatedTargetId != null && kingdomEliminated(generatedTargetId))
                || (intent != "release_subject" && generatedTargetId != null && !kingdomHasAuthority(generatedTargetId)))
            {
                reason = "target_kingdom_not_eligible";
                return true;
            }
            WorldDiplomacyDocument responseSource = resolveDocument?.Invoke(job.SourceDocumentId);
            List<string> addressedIds = WorldDiplomacyEnvelopeJsonRules.ReadStringList(json, "addressed_kingdom_ids", "addressed");
            List<string> mentionedIds = WorldDiplomacyEnvelopeJsonRules.ReadStringList(json, "mentioned_kingdom_ids", "mentioned");
            foreach (string id in addressedIds.Concat(mentionedIds))
            {
                string listedId = resolveKingdomId?.Invoke(id);
                if (string.IsNullOrWhiteSpace(id) || listedId == null || string.Equals(listedId, authorId, StringComparison.OrdinalIgnoreCase))
                {
                    reason = "referenced_kingdom_not_eligible";
                    return true;
                }
            }
            if (generatedTargetId == null || string.IsNullOrWhiteSpace(targetId))
            { reason = "diplomatic_action_has_no_target"; return true; }
            (bool Violation, string Reason) structureResult = deriveStructure(owningRound, generatedTargetId, intent);
            if (structureResult.Violation)
            {
                reason = structureResult.Reason;
                return true;
            }
            if (!WorldDiplomacyOfferContractRules.CommitmentMatchesIntent(intent, commitment))
            {
                reason = "intent_commitment_mismatch";
                return true;
            }
            string claimedThreatDocumentId = WorldDiplomacyEnvelopeJsonRules.ReadString(json, "responding_to_threat_document_id");
            if (intent == "comply_ultimatum")
            {
                if (string.IsNullOrWhiteSpace(claimedThreatDocumentId))
                {
                    reason = "comply_ultimatum_missing_source_document";
                    return true;
                }
                if (!(job.PresentedThreatDocumentIds ?? new List<string>()).Contains(claimedThreatDocumentId, StringComparer.OrdinalIgnoreCase))
                {
                    reason = "comply_ultimatum_source_not_presented";
                    return true;
                }
            }
            else if (!string.IsNullOrWhiteSpace(claimedThreatDocumentId))
            {
                reason = "non_compliance_claims_threat_source";
                return true;
            }
            (bool Violation, string Reason) stateResult = stateViolation(intent, generatedTargetId);
            if (stateResult.Violation) { reason = stateResult.Reason; return true; }
            (bool Violation, string Reason) threatResult = threatViolation(intent, generatedTargetId, claimedThreatDocumentId);
            if (threatResult.Violation) { reason = threatResult.Reason; return true; }

            string proposalIntent = WorldDiplomacyIntentVocabulary.ResponseIntentToProposalIntent(intent);
            if (!string.IsNullOrWhiteSpace(proposalIntent))
            {
                if (generatedTargetId == null || string.Equals(generatedTargetId, authorId, StringComparison.OrdinalIgnoreCase))
                {
                    reason = "offer_response_has_no_valid_proposer";
                    return true;
                }
                string claimedOfferDocumentId = WorldDiplomacyEnvelopeJsonRules.ReadString(json, "responding_to_offer_document_id");
                if (string.IsNullOrWhiteSpace(claimedOfferDocumentId))
                {
                    reason = "offer_response_missing_source_document";
                    return true;
                }
                string claimedActionId = WorldDiplomacyEnvelopeJsonRules.ReadString(json, "responding_to_offer_action_id");
                string resolvedProposer = generatedTargetId;
                if (owningRound?.PendingOffers.Any(x => x.Status == "open" && x.SourceDocumentId == claimedOfferDocumentId
                    && x.SourceActionId == claimedActionId && x.ProposerKingdomId == resolvedProposer && x.TargetKingdomId == authorId
                    && WorldDiplomacyIntentVocabulary.NormalizeIntent(x.Intent) == proposalIntent) != true)
                {
                    reason = "offer_response_source_mismatch";
                    return true;
                }
                if (intent == "accept_peace")
                {
                    WorldDiplomacyPeaceTerms responseTerms = parsePeaceTerms(json, generatedTargetId);
                    WorldDiplomacyDocument source = resolveDocument?.Invoke(claimedOfferDocumentId);
                    WorldDiplomacyPeaceTerms offeredTerms = WorldDiplomacyDocumentFactRules.ResolveOfferedPeaceTerms(
                        source,
                        WorldDiplomacyEnvelopeJsonRules.ReadString(json, "responding_to_offer_action_id"));
                    if (((bool?)json["_af_explicit_peace_terms"] == true || responseTerms != null)
                        && !WorldDiplomacyOfferContractRules.ArePeaceTermsEquivalent(responseTerms, offeredTerms))
                    {
                        reason = "accept_peace_changes_offer_terms";
                        return true;
                    }
                    json["peace_terms"] = WorldDiplomacyEnvelopeJsonRules.BuildPeaceTermsJson(offeredTerms);
                }
                if (intent == "accept_trade" && !WorldDiplomacyOfferContractRules.TryResolveTradeAcceptanceTerms(
                    resolveDocument?.Invoke(claimedOfferDocumentId), claimedActionId,
                    WorldDiplomacyOfferContractRules.ParseTreatyTerms(json, intent), out _, out reason))
                    return true;
            }
            else if (intent != "withdraw_offer" && !string.IsNullOrWhiteSpace(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "responding_to_offer_document_id")))
            {
                string claimedOfferDocumentId = WorldDiplomacyEnvelopeJsonRules.ReadString(json, "responding_to_offer_document_id");
                if (WorldDiplomacyIntentVocabulary.IsProposalIntent(intent)
                    && generatedTargetId != null
                    && !string.Equals(generatedTargetId, authorId, StringComparison.OrdinalIgnoreCase)
                    && WorldDiplomacyRoundLifecycleRules.TryResolveOpenProposalFor(job, authorId, generatedTargetId, intent, resolveRound, out string openOfferDocumentId)
                    && string.Equals(claimedOfferDocumentId, openOfferDocumentId, StringComparison.OrdinalIgnoreCase))
                {
                    // A counter-proposal is a new offer, not an acceptance/rejection. DeepSeek often keeps the
                    // incoming offer id to express continuity; ownership is already proven above, so normalize
                    // the bookkeeping field instead of discarding an otherwise legal public document.
                    json["responding_to_offer_document_id"] = "";
                    log?.Invoke("counter-proposal source normalized job=" + job.JobId
                        + " author=" + authorId + " target=" + generatedTargetId
                        + " intent=" + intent + " source=" + openOfferDocumentId);
                }
                else
                {
                    reason = "non_response_claims_offer_source";
                    return true;
                }
            }
            // The LLM's structured author_intent is authoritative for generated declarations.
            // Do not re-infer an action from literary wording: the structured intent is always
            // exposed to players through DocumentTypeLabel, while C# still owns legality and execution.
            (bool Violation, string Reason) disclosureResult = peaceDisclosureViolation(intent, visibleText, generatedTargetId);
            if (disclosureResult.Violation) { reason = disclosureResult.Reason; return true; }
            if (WorldDiplomacyTextRules.TryGetImmersionViolation(visibleText, out reason))
            {
                return true;
            }
            (bool Violation, string Reason) realmResult = realmIdentityViolation(visibleText);
            if (realmResult.Violation)
            {
                reason = realmResult.Reason;
                return true;
            }
            if (!WorldDiplomacyIntentVocabulary.IsPeaceIntent(intent)) return false;
            if (generatedTargetId == null || string.Equals(generatedTargetId, authorId, StringComparison.OrdinalIgnoreCase))
            {
                reason = "peace_intent_has_no_valid_target";
                return true;
            }
            if (!isAtWar(authorId, generatedTargetId))
            {
                reason = "peace_intent_between_kingdoms_not_at_war";
                return true;
            }
            return false;

        }
        public static bool TryApplyGeneratedSemanticEnvelope(
        WorldDiplomacyDocument document, JObject json, string authorId, string fallbackTargetId,
        bool allowUntargeted, bool relayTurn,
        Func<WorldDiplomacyDocument, JObject, string, bool, bool, bool> applySingleAction,
        Func<IEnumerable<string>, string, List<string>> normalizeIds)
    {
            if (document == null || json == null || authorId == null || json["actions"] is not JArray actions
                || actions.Count < 1 || actions.Count > 64) return false;
            List<WorldDiplomacyDocumentAction> applied = new List<WorldDiplomacyDocumentAction>(actions.Count);
            bool anyRoundResponseNoAction = false;
            bool anyWarResponseNoAction = false;
            for (int index = 0; index < actions.Count; index++)
            {
                if (actions[index] is not JObject actionEnvelope) return false;
                JObject single = WorldDiplomacyEnvelopeJsonRules.BuildGeneratedSingleActionEnvelope(json, actionEnvelope);
                WorldDiplomacyDocument actionDocument = new WorldDiplomacyDocument
                {
                    DocumentId = document.DocumentId,
                    RoundId = document.RoundId,
                    AuthorKingdomId = document.AuthorKingdomId,
                    SourceDocumentId = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(
                        WorldDiplomacyEnvelopeJsonRules.ReadString(actionEnvelope, "responding_to_offer_document_id"),
                        WorldDiplomacyEnvelopeJsonRules.ReadString(actionEnvelope, "responding_to_threat_document_id"),
                        document.SourceDocumentId),
                    ResultSettlementSlotId = document.ResultSettlementSlotId,
                    IsExternalResponseOnly = document.IsExternalResponseOnly,
                    IsRelayTurn = document.IsRelayTurn,
                    AnsweredPlayerDocumentIds = document.AnsweredPlayerDocumentIds
                };
                if (!applySingleAction(
                    actionDocument,
                    single,
                    actions.Count == 1 ? fallbackTargetId : null,
                    actions.Count == 1 && allowUntargeted,
                    relayTurn)) return false;
                WorldDiplomacyDocumentAction action = new WorldDiplomacyDocumentAction
                {
                    ActionId = "action_" + (index + 1).ToString(CultureInfo.InvariantCulture),
                    DependencyActionIds = WorldDiplomacyEnvelopeJsonRules.ReadStringList(actionEnvelope, "dependency_action_ids"),
                    TargetKingdomId = actionDocument.TargetKingdomId ?? "",
                    TargetKingdomName = actionDocument.TargetKingdomName ?? "",
                    Intent = WorldDiplomacyIntentVocabulary.NormalizeIntent(actionDocument.Intent),
                    NegotiationMove = WorldDiplomacyIntentVocabulary.NormalizeNegotiationMove(actionDocument.NegotiationMove),
                    Commitment = WorldDiplomacyIntentVocabulary.NormalizeCommitment(actionDocument.Commitment),
                    RequiresResponse = actionDocument.RequiresResponse,
                    RespondingToOfferDocumentId = WorldDiplomacyEnvelopeJsonRules.ReadString(actionEnvelope, "responding_to_offer_document_id"),
                    RespondingToOfferActionId = WorldDiplomacyEnvelopeJsonRules.ReadString(actionEnvelope, "responding_to_offer_action_id"),
                    RespondingToThreatDocumentId = WorldDiplomacyEnvelopeJsonRules.ReadString(actionEnvelope, "responding_to_threat_document_id"),
                    RespondingToThreatActionId = WorldDiplomacyEnvelopeJsonRules.ReadString(actionEnvelope, "responding_to_threat_action_id"),
                    TreatyTerms = actionDocument.TreatyTerms,
                    PeaceTerms = actionDocument.PeaceTerms
                };
                applied.Add(action);
                anyRoundResponseNoAction |= actionDocument.IsRoundResponseNoActionDeclaration;
                anyWarResponseNoAction |= actionDocument.IsWarResponseNoActionDeclaration;
            }
            document.Actions = applied;
            document.AddressedKingdomIds = normalizeIds(
                applied.Select(x => x.TargetKingdomId),
                authorId);
            document.MentionedKingdomIds = normalizeIds(
                WorldDiplomacyEnvelopeJsonRules.ReadStringList(json, "mentioned_kingdom_ids", "mentioned"),
                authorId);
            document.Tone = WorldDiplomacyIntentVocabulary.NormalizeTone(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "tone"));
            document.Confidence = Math.Max(0f, Math.Min(1f, WorldDiplomacyEnvelopeJsonRules.ReadFloat(json, "confidence")));
            document.RequiresResponse = applied.Any(x => x.RequiresResponse);
            document.IsRoundResponseNoActionDeclaration = anyRoundResponseNoAction;
            document.IsWarResponseNoActionDeclaration = anyWarResponseNoAction;
            document.AnalysisStatus = "generation_envelope";
            WorldDiplomacyDocumentFactRules.MirrorPrimaryActionToDocument(document, applied[0]);
            return true;
        }

        public static bool TryApplyGeneratedSingleActionSemanticEnvelope(
        WorldDiplomacyDocument document, JObject json, string authorId, string fallbackTargetId,
        bool allowUntargeted, Func<string, string> resolveKingdomId, Func<string, string> resolveKingdomName,
        Func<JObject, string, string, WorldDiplomacyPeaceTerms> parsePeaceTerms,
        Func<IEnumerable<string>, string, List<string>> normalizeIds)
    {
            if (document == null || json == null
                || !(json["author_intent"] is JObject)
                || !WorldDiplomacyEnvelopeJsonRules.IsJsonStringArray(json["addressed_kingdom_ids"])
                || !WorldDiplomacyEnvelopeJsonRules.IsJsonStringArray(json["mentioned_kingdom_ids"])
                || !(json["peace_terms"] is JObject)
                || json["requires_response"] == null
                || json["tone"] == null
                || json["confidence"] == null
                || json["primary_target_kingdom_id"] == null)
            {
                return false;
            }
            string intent = WorldDiplomacyIntentVocabulary.NormalizeIntent(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "author_intent.intent", "intent"));
            string negotiationMove = WorldDiplomacyIntentVocabulary.NormalizeNegotiationMove(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "negotiation_move"));
            string commitment = WorldDiplomacyIntentVocabulary.NormalizeCommitment(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "author_intent.commitment", "commitment"));
            if (!WorldDiplomacyIntentVocabulary.IsSupportedCommitment(commitment))
            {
                return false;
            }
            string generatedTargetId = WorldDiplomacyEnvelopeJsonRules.ReadString(json, "primary_target_kingdom_id");
            string targetId = resolveKingdomId(generatedTargetId);
            if (targetId == null && string.IsNullOrWhiteSpace(generatedTargetId) && !allowUntargeted) targetId = fallbackTargetId;
            if ((targetId == null && !allowUntargeted) || string.Equals(targetId, authorId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (!WorldDiplomacyIntentVocabulary.IsSupportedDiplomacyIntent(intent)
                || !WorldDiplomacyOfferContractRules.CommitmentMatchesIntent(intent, commitment)) return false;
            document.TargetKingdomId = targetId ?? "";
            document.TargetKingdomName = targetId == null ? "" : resolveKingdomName(targetId);
            List<string> addressed = WorldDiplomacyEnvelopeJsonRules.ReadStringList(json, "addressed_kingdom_ids", "addressed");
            List<string> mentioned = WorldDiplomacyEnvelopeJsonRules.ReadStringList(json, "mentioned_kingdom_ids", "mentioned");
            if (addressed.Any(x => string.IsNullOrWhiteSpace(x) || resolveKingdomId(x) == null)
                || mentioned.Any(x => string.IsNullOrWhiteSpace(x) || resolveKingdomId(x) == null))
            {
                return false;
            }
            document.AddressedKingdomIds = normalizeIds(addressed.Concat(targetId == null ? Enumerable.Empty<string>() : new[] { targetId }), authorId);
            document.MentionedKingdomIds = normalizeIds(mentioned, authorId);
            document.Intent = intent;
            document.NegotiationMove = string.Equals(intent, "statement", StringComparison.OrdinalIgnoreCase) ? negotiationMove : "";
            document.Commitment = commitment;
            document.IsRoundResponseNoActionDeclaration = intent == "statement";
            document.IsWarResponseNoActionDeclaration = false;
            document.Tone = WorldDiplomacyIntentVocabulary.NormalizeTone(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "tone"));
            document.Confidence = Math.Max(0f, Math.Min(1f, WorldDiplomacyEnvelopeJsonRules.ReadFloat(json, "confidence")));
            document.RequiresResponse = WorldDiplomacyEnvelopeJsonRules.ReadBool(json, "requires_response");
            document.PeaceTerms = targetId == null ? document.PeaceTerms : (parsePeaceTerms(json, authorId, targetId) ?? document.PeaceTerms);
            document.AnalysisStatus = "generation_envelope";
            return true;
        }

}
