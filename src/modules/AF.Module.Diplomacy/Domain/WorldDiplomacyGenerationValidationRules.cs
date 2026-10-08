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
    if (duration > 0 && !WorldDiplomacyTextRules.ContainsWholeNumber(text, duration))
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
            "支付|缴纳|交付|给付"))
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
            "割让|移交|交还|归还"))
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
            bool enforcingRejectedUltimatum = WorldDiplomacyRoundLifecycleRules.IsEnforcingRejectedUltimatum(diplomaticThreats, authorId, targetId);
            var declareWar = declareWarVerdict?.Invoke(enforcingRejectedUltimatum) ?? (false, "declare_war_check_unavailable");
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
            if (WorldDiplomacyOfferCooldownRules.IsTradeAllianceProposalCoolingDown(offerCooldownLastFailedDay, authorId, targetId, normalized, offerCooldownDays, currentDay)) { reason = "intent_not_in_current_legal_action_list"; return true; }
            break;
        case "accept_alliance":
            if (!allianceAvailable) { reason = "alliance_system_unavailable"; return true; }
            if (atWar || allied) { reason = "alliance_intent_conflicts_with_current_state"; return true; }
            break;
        case "propose_trade":
            if (!tradeAvailable) { reason = "trade_system_unavailable"; return true; }
            if (atWar || trading) { reason = "trade_intent_conflicts_with_current_state"; return true; }
            if (WorldDiplomacyOfferCooldownRules.IsTradeAllianceProposalCoolingDown(offerCooldownLastFailedDay, authorId, targetId, normalized, offerCooldownDays, currentDay)) { reason = "intent_not_in_current_legal_action_list"; return true; }
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

    string enforcementBlockReason = "";
    bool canEnforce = partiesAtWar;
    if (!canEnforce)
    {
        var threatVerdict = issueThreatVerdict?.Invoke() ?? (false, "");
        canEnforce = threatVerdict.legal;
        enforcementBlockReason = threatVerdict.reason ?? "";
    }
    WorldDiplomacyThreat outbound = canEnforce && !partiesAtWar
        ? WorldDiplomacyRoundLifecycleRules.SelectOpenThreatIssuedBy(diplomaticThreats, authorId)
        : null;
    reason = WorldDiplomacyRoundLifecycleRules.EvaluateThreatEscalationViolation(
        normalized, partiesAtWar, canEnforce, enforcementBlockReason, outbound, targetId);
    return reason != "";
}
    public static bool TryGetRealmIdentityViolation(
        string authorKingdomId,
        string rulerName,
        string visibleText,
        out string reason)
{
    reason = "";
    if (string.IsNullOrWhiteSpace(visibleText)) return false;
    string kingdomId = (authorKingdomId ?? "").Trim().ToLowerInvariant();
    if (!string.Equals(kingdomId, "empire_n", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(kingdomId, "empire_w", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(kingdomId, "empire_s", StringComparison.OrdinalIgnoreCase)) return false;


    if (!string.IsNullOrWhiteSpace(rulerName))
    {
        string escapedName = Regex.Escape(rulerName);
        string invalidPersonalTitlePattern = "(?:" + escapedName + "(?:元老|议员|执政官|国王|女王|大公|可汗|苏丹)|(?:元老|议员|执政官|国王|女王|大公|可汗|苏丹)(?:阁下|大人)?" + escapedName
            + "|" + escapedName + "(?:身为|作为|乃是|是)(?:一名|帝国的?)?(?:元老|议员|执政官|国王|女王|大公|可汗|苏丹))";
        if (Regex.IsMatch(visibleText, invalidPersonalTitlePattern, RegexOptions.CultureInvariant))
        {
            reason = "realm_ruler_title_conflicts_with_hard_fact";
            return true;
        }
    }

    if (string.Equals(kingdomId, "empire_s", StringComparison.OrdinalIgnoreCase)
        && Regex.IsMatch(visibleText, @"(?:南帝国|我国|我朝|本国|本朝)(?:的|之)?(?:元老院|元老议会|元老们)", RegexOptions.CultureInvariant))
    {
        reason = "southern_empire_government_conflicts_with_hard_fact";
        return true;
    }
    if (string.Equals(kingdomId, "empire_w", StringComparison.OrdinalIgnoreCase)
        && Regex.IsMatch(visibleText, @"(?:西帝国|我国|我朝|本国|本朝)(?:的|之)?(?:元老院|元老议会|元老们)", RegexOptions.CultureInvariant))
    {
        reason = "western_empire_government_conflicts_with_hard_fact";
        return true;
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
        out string reason)
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
        if (!isOfferResponse) return false;
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
        WorldDiplomacyJob job,
        JObject json,
        int maxDiplomaticActionsPerDocument,
        int roundParticipantLimit,
        Func<JObject, bool, (bool Violation, string TargetKingdomId, string Reason)> singleActionViolation,
        Func<string, WorldDiplomacyRound> resolveRound,
        Func<WorldDiplomacyRound, WorldDiplomacyRoundOffer> resolveRequiredPeaceOffer,
        Func<string, WorldDiplomacyDocument> resolveDocument,
        out string generatedTargetKingdomId,
        out string reason, bool playerDiplomacy = false)
    {
        generatedTargetKingdomId = null;
        reason = "";
        if (job == null || json == null || json["actions"] is not JArray actions
            || actions.Count < 1 || (!playerDiplomacy && actions.Count > maxDiplomaticActionsPerDocument))
        {
            reason = "diplomatic_actions_envelope_invalid";
            return true;
        }
        HashSet<string> targetIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int statementCount = 0;
        int outgoingThreatCount = 0;
        for (int index = 0; index < actions.Count; index++)
        {
            if (actions[index] is not JObject action)
            {
                reason = "diplomatic_action_entry_invalid";
                return true;
            }
            string targetId = WorldDiplomacyEnvelopeJsonRules.ReadString(action, "target_kingdom_id", "target");
            if (string.IsNullOrWhiteSpace(targetId) || (!playerDiplomacy && !targetIds.Add(targetId)))
            {
                reason = "diplomatic_action_target_missing_or_duplicate";
                return true;
            }
            targetIds.Add(targetId);
            string intent = WorldDiplomacyIntentVocabulary.NormalizeIntent(WorldDiplomacyEnvelopeJsonRules.ReadString(action, "intent", "author_intent.intent"));
            if (intent == "statement") statementCount++;
            if (intent == "warning" || intent == "ultimatum") outgoingThreatCount++;
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
        WorldDiplomacyRound owningRound = resolveRound?.Invoke(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(job.RoundId, job.ExchangeId));
        WorldDiplomacyRoundOffer requiredPeaceOffer =
            resolveRequiredPeaceOffer?.Invoke(owningRound);
        if (!playerDiplomacy && !WorldDiplomacyOfferContractRules.GeneratedActionsContainRequiredPeaceOfferResponse(actions, requiredPeaceOffer))
        {
            reason = "required_peace_offer_response_missing";
            return true;
        }
        if (WorldDiplomacyDocumentFactRules.GeneratedActionsHaveUnsafeMultiplePeaceAcceptances(actions, resolveDocument))
        {
            reason = "multiple_peace_acceptances_have_cross_terms";
            return true;
        }
        if (!playerDiplomacy && ((statementCount > 0 && actions.Count != 1) || statementCount > 1))
        {
            reason = "statement_must_be_the_only_diplomatic_action";
            return true;
        }
        if (!playerDiplomacy && outgoingThreatCount > 1)
        {
            reason = "multiple_outgoing_threats_not_supported";
            return true;
        }
        if (!playerDiplomacy && WorldDiplomacyRoundLifecycleRules.IsAutonomousOpeningJob(job))
        {
            HashSet<string> planned = new HashSet<string>(
                WorldDiplomacyEnvelopeJsonRules.ReadStringList(json, "round_plan.selected_kingdom_ids"),
                StringComparer.OrdinalIgnoreCase);
            if (targetIds.Count + 1 > roundParticipantLimit)
            {
                reason = "autonomous_round_plan_exceeds_participant_limit";
                return true;
            }
            if (targetIds.Any(x => !planned.Contains(x)))
            {
                reason = "autonomous_round_plan_omits_direct_target";
                return true;
            }
        }
        WorldDiplomacyEnvelopeJsonRules.MirrorFirstGeneratedActionEnvelope(json, actions);
        json["addressed_kingdom_ids"] = new JArray(targetIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        return false;
    }

    public static bool TryGetGeneratedSingleActionLegalityViolation(
        WorldDiplomacyJob job,
        JObject json,
        string authorId,
        string fallbackTargetId,
        int roundParticipantLimit,
        Func<string, string> resolveKingdomId,
        Func<string, bool> kingdomEliminated,
        Func<string, bool> kingdomHasAuthority,
        Func<string, string, bool> isAtWar,
        Func<string, WorldDiplomacyRound> resolveRound,
        Func<string, WorldDiplomacyDocument> resolveDocument,
        Func<WorldDiplomacyRound, string, bool> canUseSettlementTarget,
        Func<WorldDiplomacyRound, string, WorldDiplomacyDocument, bool> nonRootRelayNoActionAllowed,
        Func<WorldDiplomacyRound, string, WorldDiplomacyDocument, List<string>> buildLegalDeclarationIntents,
        Func<WorldDiplomacyRound, string, string, (bool Violation, string Reason)> deriveStructure,
        Func<string, string, (bool Violation, string Reason)> stateViolation,
        Func<string, string, string, (bool Violation, string Reason)> threatViolation,
        Func<JObject, string, WorldDiplomacyPeaceTerms> parsePeaceTerms,
        Func<string, string, string, (bool Violation, string Reason)> peaceDisclosureViolation,
        Func<string, (bool Violation, string Reason)> realmIdentityViolation,
        Action<string> log,
        out string generatedTargetId,
        out string reason, bool playerDiplomacy = false)
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
                || !(json["round_plan"] is JObject roundPlanEnvelope)
                || !WorldDiplomacyEnvelopeJsonRules.IsJsonStringArray(roundPlanEnvelope["selected_kingdom_ids"])
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
            string commitment = WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent(intent);
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
                || (!playerDiplomacy && generatedTargetId != null && !kingdomHasAuthority(generatedTargetId)))
            {
                reason = "target_kingdom_not_eligible";
                return true;
            }
            if (!playerDiplomacy && !job.IsRelayTurn
                && !WorldDiplomacyRoundLifecycleRules.IsAutonomousOpeningJob(job)
                && !string.IsNullOrWhiteSpace(job.TargetKingdomId)
                && !string.Equals(generatedTargetId, job.TargetKingdomId, StringComparison.OrdinalIgnoreCase))
            {
                reason = "kingdom_not_in_targeted_generation_scope";
                return true;
            }
            WorldDiplomacyDocument responseSource = resolveDocument?.Invoke(job.SourceDocumentId);
            bool allowedRoundResponseNoAction = string.Equals(intent, "statement", StringComparison.OrdinalIgnoreCase)
                && (playerDiplomacy || nonRootRelayNoActionAllowed(owningRound, generatedTargetId, responseSource));
            if (!WorldDiplomacyIntentVocabulary.IsActionableDiplomacyIntent(intent) && !allowedRoundResponseNoAction && !playerDiplomacy)
            {
                reason = "non_actionable_diplomatic_intent";
                return true;
            }
            string negotiationMove = WorldDiplomacyIntentVocabulary.NormalizeNegotiationMove(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "negotiation_move"));
            if (!playerDiplomacy && string.Equals(intent, "statement", StringComparison.OrdinalIgnoreCase)
                && !WorldDiplomacyIntentVocabulary.IsSupportedNegotiationMove(negotiationMove))
            {
                reason = "statement_missing_negotiation_move";
                return true;
            }
            if (!playerDiplomacy && string.Equals(intent, "statement", StringComparison.OrdinalIgnoreCase)
                && WorldDiplomacyRoundLifecycleRules.ShouldForceTerminalMove(owningRound?.ConsecutiveNoActionPasses ?? 0)
                && !WorldDiplomacyIntentVocabulary.IsTerminalNegotiationMove(negotiationMove))
            {
                reason = "statement_requires_terminal_negotiation_move";
                return true;
            }
            List<string> addressedIds = WorldDiplomacyEnvelopeJsonRules.ReadStringList(json, "addressed_kingdom_ids", "addressed");
            List<string> mentionedIds = WorldDiplomacyEnvelopeJsonRules.ReadStringList(json, "mentioned_kingdom_ids", "mentioned");
            foreach (string id in addressedIds.Concat(mentionedIds))
            {
                string listedId = resolveKingdomId?.Invoke(id);
                if (string.IsNullOrWhiteSpace(id) || listedId == null || string.Equals(listedId, authorId, StringComparison.OrdinalIgnoreCase) || kingdomEliminated(listedId)
                    || (!playerDiplomacy && !kingdomHasAuthority(listedId)))
                {
                    reason = "referenced_kingdom_not_eligible";
                    return true;
                }
            }
            if (!playerDiplomacy && WorldDiplomacyRoundLifecycleRules.IsAutonomousOpeningJob(job))
            {
                HashSet<string> allowed = new HashSet<string>(job.CandidateKingdomIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                if ((generatedTargetId != null && !allowed.Contains(generatedTargetId))
                    || addressedIds.Any(id => !allowed.Contains(id))
                    || mentionedIds.Any(id => !allowed.Contains(id)))
                {
                    reason = "kingdom_not_in_autonomous_candidate_set";
                    return true;
                }
                if (!(json["round_plan"] is JObject roundPlan)
                    || !WorldDiplomacyEnvelopeJsonRules.IsJsonStringArray(roundPlan["selected_kingdom_ids"])
                    || string.IsNullOrWhiteSpace(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "round_plan.topic")))
                {
                    reason = "autonomous_round_plan_incomplete";
                    return true;
                }
                List<string> plannedIds = WorldDiplomacyEnvelopeJsonRules.ReadStringList(json, "round_plan.selected_kingdom_ids");
                if (plannedIds.Any(id => !allowed.Contains(id)
                    || resolveKingdomId?.Invoke(id) is not string plannedId
                    || kingdomEliminated(plannedId)
                    || !kingdomHasAuthority(plannedId)))
                {
                    reason = "autonomous_round_plan_has_invalid_participant";
                    return true;
                }
                HashSet<string> plannedSet = new HashSet<string>(plannedIds, StringComparer.OrdinalIgnoreCase);
                List<string> directIds = WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(addressedIds
                    .Concat(generatedTargetId == null ? Enumerable.Empty<string>() : new[] { generatedTargetId }));
                int participantLimit = roundParticipantLimit;
                if (plannedSet.Count + 1 > participantLimit || directIds.Count + 1 > participantLimit)
                {
                    reason = "autonomous_round_plan_exceeds_participant_limit";
                    return true;
                }
                if (directIds.Any(id => !plannedSet.Contains(id)))
                {
                    reason = "autonomous_round_plan_omits_direct_target";
                    return true;
                }
            }
            bool resultSettlementRelay = job.IsRelayTurn && owningRound?.ResultSettlementPending == true
                && !string.IsNullOrWhiteSpace(job.ResultSettlementSlotId);
            HashSet<string> presentedSettlementTargets = resultSettlementRelay
                ? new HashSet<string>(job.CandidateKingdomIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase)
                : null;
            bool generatedTargetOutsideScope = generatedTargetId != null
                && !(resultSettlementRelay
                    ? presentedSettlementTargets.Contains(generatedTargetId)
                        && canUseSettlementTarget(owningRound, generatedTargetId)
                    : WorldDiplomacyStructureRules.RoundRouteContainsKingdom(owningRound, generatedTargetId));
            string generatedTargetScopeId = generatedTargetId ?? "";
            bool addressedOutsideScope = addressedIds.Any(id => resultSettlementRelay
                ? !presentedSettlementTargets.Contains(id)
                    || (!string.IsNullOrWhiteSpace(generatedTargetScopeId)
                        && !string.Equals(id, generatedTargetScopeId, StringComparison.OrdinalIgnoreCase)
                        && !WorldDiplomacyStructureRules.RoundRouteContainsKingdom(owningRound, id))
                : !WorldDiplomacyStructureRules.RoundRouteContainsKingdom(owningRound, id));
            if (!playerDiplomacy && job.IsRelayTurn && (generatedTargetOutsideScope || addressedOutsideScope))
            {
                reason = resultSettlementRelay ? "kingdom_not_in_result_settlement_scope" : "kingdom_not_in_relay_route";
                return true;
            }
            bool targetRequired = WorldDiplomacyIntentVocabulary.IsActionableDiplomacyIntent(intent) || allowedRoundResponseNoAction;
            if (targetRequired && (generatedTargetId == null || string.IsNullOrWhiteSpace(targetId)))
            {
                reason = "diplomatic_action_has_no_target";
                return true;
            }
            if (!playerDiplomacy && generatedTargetId != null
                && !buildLegalDeclarationIntents(owningRound, generatedTargetId, responseSource)
                    .Contains(intent, StringComparer.OrdinalIgnoreCase))
            {
                reason = "intent_not_in_current_legal_action_list";
                return true;
            }
            (bool Violation, string Reason) structureResult = deriveStructure(owningRound, generatedTargetId, intent);
            if (structureResult.Violation && (!playerDiplomacy || intent == "withdraw_offer"
                || !string.IsNullOrEmpty(WorldDiplomacyIntentVocabulary.ResponseIntentToProposalIntent(intent))
                || intent == "comply_ultimatum"))
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
            (bool Violation, string Reason) stateResult = playerDiplomacy ? (false, "") : stateViolation(intent, generatedTargetId);
            if (stateResult.Violation) { reason = stateResult.Reason; return true; }
            (bool Violation, string Reason) threatResult = playerDiplomacy && intent != "comply_ultimatum" ? (false, "") : threatViolation(intent, generatedTargetId, claimedThreatDocumentId);
            if (threatResult.Violation) { reason = threatResult.Reason; return true; }

            string proposalIntent = WorldDiplomacyIntentVocabulary.ResponseIntentToProposalIntent(intent);
            if (!string.IsNullOrWhiteSpace(proposalIntent))
            {
                if (generatedTargetId == null || string.Equals(generatedTargetId, authorId, StringComparison.OrdinalIgnoreCase))
                {
                    reason = "offer_response_has_no_valid_proposer";
                    return true;
                }
                if (!WorldDiplomacyRoundLifecycleRules.TryResolveOpenProposalFor(job, authorId, generatedTargetId, proposalIntent, resolveRound, out string openOfferDocumentId))
                {
                    reason = "offer_response_without_matching_open_offer";
                    return true;
                }
                string claimedOfferDocumentId = WorldDiplomacyEnvelopeJsonRules.ReadString(json, "responding_to_offer_document_id");
                if (string.IsNullOrWhiteSpace(claimedOfferDocumentId))
                {
                    reason = "offer_response_missing_source_document";
                    return true;
                }
                if (!string.Equals(claimedOfferDocumentId, openOfferDocumentId, StringComparison.OrdinalIgnoreCase))
                {
                    reason = "offer_response_source_mismatch";
                    return true;
                }
                if (intent == "accept_peace")
                {
                    WorldDiplomacyPeaceTerms responseTerms = parsePeaceTerms(json, generatedTargetId);
                    WorldDiplomacyDocument source = resolveDocument?.Invoke(openOfferDocumentId);
                    WorldDiplomacyPeaceTerms offeredTerms = WorldDiplomacyDocumentFactRules.ResolveOfferedPeaceTerms(
                        source,
                        WorldDiplomacyEnvelopeJsonRules.ReadString(json, "responding_to_offer_action_id"));
                    if (responseTerms != null && !WorldDiplomacyOfferContractRules.ArePeaceTermsEquivalent(responseTerms, offeredTerms))
                    {
                        reason = "accept_peace_changes_offer_terms";
                        return true;
                    }
                }
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
            if (!playerDiplomacy && disclosureResult.Violation) { reason = disclosureResult.Reason; return true; }
            if (!playerDiplomacy && WorldDiplomacyTextRules.TryGetImmersionViolation(visibleText, out reason))
            {
                return true;
            }
            (bool Violation, string Reason) realmResult = realmIdentityViolation(visibleText);
            if (!playerDiplomacy && realmResult.Violation)
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
            if (!playerDiplomacy && !isAtWar(authorId, generatedTargetId))
            {
                reason = "peace_intent_between_kingdoms_not_at_war";
                return true;
            }
            return false;

        }
        public static bool TryApplyGeneratedSemanticEnvelope(
            WorldDiplomacyDocument document,
            JObject json,
            string authorId,
            string fallbackTargetId,
            bool allowUntargeted,
            bool relayTurn,
            int maxDiplomaticActionsPerDocument,
            Func<WorldDiplomacyDocument, JObject, string, bool, bool, bool> applySingleAction,
            Func<IEnumerable<string>, string, List<string>> normalizeIds)
        {
            if (document == null || json == null || authorId == null || json["actions"] is not JArray actions
                || actions.Count < 1 || actions.Count > maxDiplomaticActionsPerDocument) return false;
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
            WorldDiplomacyDocument document,
            JObject json,
            string authorId,
            string fallbackTargetId,
            bool allowUntargeted,
            bool relayTurn,
            int maxAutomaticReplyDepth,
            Func<string, string> resolveKingdomId,
            Func<string, string> resolveKingdomName,
            Func<string, WorldDiplomacyRound> resolveRound,
            Func<string, WorldDiplomacyDocument> resolveDocument,
            Func<WorldDiplomacyRound, string, string, string, bool, bool, WorldDiplomacyDocument, bool> isRoundResponseNoActionAllowed,
            Func<WorldDiplomacyRound, string, string, bool> canUseSettlementTarget,
            Func<JObject, string, string, WorldDiplomacyPeaceTerms> parsePeaceTerms,
            Func<IEnumerable<string>, string, List<string>> normalizeIds, bool playerDiplomacy = false)
        {
            if (document == null || json == null
                || !(json["author_intent"] is JObject)
                || !WorldDiplomacyEnvelopeJsonRules.IsJsonStringArray(json["addressed_kingdom_ids"])
                || !WorldDiplomacyEnvelopeJsonRules.IsJsonStringArray(json["mentioned_kingdom_ids"])
                || !(json["round_plan"] is JObject roundPlanEnvelope)
                || !WorldDiplomacyEnvelopeJsonRules.IsJsonStringArray(roundPlanEnvelope["selected_kingdom_ids"])
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
            WorldDiplomacyRound envelopeRound = resolveRound(document.RoundId);
            WorldDiplomacyDocument responseSource = resolveDocument(document.SourceDocumentId);
            bool allowedRoundResponseNoAction = string.Equals(intent, "statement", StringComparison.OrdinalIgnoreCase)
                && (playerDiplomacy || isRoundResponseNoActionAllowed(
                    envelopeRound,
                    document.ResultSettlementSlotId,
                    authorId,
                    targetId,
                    relayTurn,
                    document.IsExternalResponseOnly,
                    responseSource));
            bool allowedWarResponseNoAction = allowedRoundResponseNoAction
                && WorldDiplomacyRoundLifecycleRules.IsWarResponseNoActionAllowed(envelopeRound, document.ResultSettlementSlotId,
                    authorId, targetId, resolveDocument);
            if (!playerDiplomacy && string.Equals(intent, "statement", StringComparison.OrdinalIgnoreCase)
                && (!WorldDiplomacyIntentVocabulary.IsSupportedNegotiationMove(negotiationMove)
                    || (WorldDiplomacyRoundLifecycleRules.ShouldForceTerminalMove(envelopeRound?.ConsecutiveNoActionPasses ?? 0)
                        && !WorldDiplomacyIntentVocabulary.IsTerminalNegotiationMove(negotiationMove))))
            {
                return false;
            }
            if (!WorldDiplomacyIntentVocabulary.IsActionableDiplomacyIntent(intent) && !allowedRoundResponseNoAction
                && !(playerDiplomacy && WorldDiplomacyIntentVocabulary.IsSupportedDiplomacyIntent(intent)))
            {
                return false;
            }
            bool resultSettlementRelay = relayTurn && envelopeRound?.ResultSettlementPending == true
                && !string.IsNullOrWhiteSpace(document.ResultSettlementSlotId);
            if (!playerDiplomacy && relayTurn && targetId != null
                && !(resultSettlementRelay
                    ? canUseSettlementTarget(envelopeRound, authorId, targetId)
                    : WorldDiplomacyStructureRules.RoundRouteContainsKingdom(envelopeRound, targetId))) return false;
            document.TargetKingdomId = targetId ?? "";
            document.TargetKingdomName = targetId == null ? "" : resolveKingdomName(targetId);
            List<string> addressed = WorldDiplomacyEnvelopeJsonRules.ReadStringList(json, "addressed_kingdom_ids", "addressed");
            List<string> mentioned = WorldDiplomacyEnvelopeJsonRules.ReadStringList(json, "mentioned_kingdom_ids", "mentioned");
            if (addressed.Any(x => string.IsNullOrWhiteSpace(x) || resolveKingdomId(x) == null)
                || mentioned.Any(x => string.IsNullOrWhiteSpace(x) || resolveKingdomId(x) == null))
            {
                return false;
            }
            if (!playerDiplomacy && relayTurn && addressed.Any(x => resultSettlementRelay
                ? !string.Equals(x, targetId, StringComparison.OrdinalIgnoreCase)
                    && !WorldDiplomacyStructureRules.RoundRouteContainsKingdom(envelopeRound, x)
                : !WorldDiplomacyStructureRules.RoundRouteContainsKingdom(envelopeRound, x))) return false;
            document.AddressedKingdomIds = normalizeIds(addressed.Concat(targetId == null ? Enumerable.Empty<string>() : new[] { targetId }), authorId);
            document.MentionedKingdomIds = normalizeIds(mentioned, authorId);
            document.Intent = intent;
            document.NegotiationMove = string.Equals(intent, "statement", StringComparison.OrdinalIgnoreCase) ? negotiationMove : "";
            document.Commitment = commitment;
            document.IsRoundResponseNoActionDeclaration = allowedRoundResponseNoAction;
            document.IsWarResponseNoActionDeclaration = allowedWarResponseNoAction;
            document.Tone = WorldDiplomacyIntentVocabulary.NormalizeTone(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "tone"));
            document.Confidence = Math.Max(0f, Math.Min(1f, WorldDiplomacyEnvelopeJsonRules.ReadFloat(json, "confidence")));
            document.RequiresResponse = allowedRoundResponseNoAction
                ? false
                : WorldDiplomacyIntentVocabulary.ResolveValidatedResponseObligation(document, intent, WorldDiplomacyEnvelopeJsonRules.ReadBool(json, "requires_response"), playerDiplomacy ? int.MaxValue : maxAutomaticReplyDepth);
            document.PeaceTerms = targetId == null ? document.PeaceTerms : (parsePeaceTerms(json, authorId, targetId) ?? document.PeaceTerms);
            document.AnalysisStatus = "generation_envelope";
            return true;
        }

}
