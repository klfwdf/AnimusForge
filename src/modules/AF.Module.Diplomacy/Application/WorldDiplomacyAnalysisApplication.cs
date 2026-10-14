using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Domain;
using static AnimusForge.Refactor.Domain.WorldDiplomacyRoundLifecycleRules;
namespace AnimusForge;
internal interface IWorldDiplomacyAnalysisPort
{
    WorldDiplomacyStorage Storage { get; }
    IWorldDiplomacyDocumentExecutionPort Execution { get; }
    int MaxAutomaticReplyDepth { get; }
    string KingdomName(string id);
}
internal static class WorldDiplomacyAnalysisApplication
{
    internal static void Commit(IWorldDiplomacyAnalysisPort port, IWorldDiplomacyOrchestration orchestration,
        WorldDiplomacyJob job, string raw)
    {
        var execution = port.Execution;
        CommitAnalysis(job, raw, port.MaxAutomaticReplyDepth, port.Storage?.DiplomaticThreats,
            execution.ResolveDocument, execution.ResolveRound, execution.ResolveKingdomId, port.KingdomName,
            orchestration.ParseAndValidatePeaceTerms, orchestration.NormalizeKingdomIdList,
            (doc, reason) => Suppress(port, orchestration, doc, reason),
            orchestration.ProcessAnalyzedDocument,
            execution.Log,
            orchestration is WorldDiplomacyOrchestration live ? live.PlayerAnalysisOffers : null);
        var document = execution.ResolveDocument(job.DocumentId);
        if (document?.IsPlayerAuthored == true && document.AnalysisStatus == "analysis_failed")
            execution.Notify(ProjectMechanicalResultForDisplay(document.MechanicalResult));
    }
    internal static void Suppress(IWorldDiplomacyAnalysisPort port, IWorldDiplomacyOrchestration orchestration,
        WorldDiplomacyDocument document, string reason)
    {
        var execution = port.Execution;
        SuppressInvalidDocumentBeforePropagation(document, reason, port.Storage, execution.ResolveRound, execution.ResolveDocument,
            execution.ResolveKingdomId, () => execution.CurrentDay,
            (doc, why) => PreservePublishedPlayerDocumentAfterRejectedMechanic(execution, orchestration, doc, why),
            orchestration.ScheduleNextResultSettlementTurn, round => orchestration.AdvanceRelay(round),
            orchestration.CompleteExchange, reason => {
                if (orchestration is WorldDiplomacyOrchestration owner) owner.CloseRound(reason, orchestration.ResolveRound(document.RoundId));
                else orchestration.CloseActiveRound(reason);
            }, execution.Log);
    }

    public static void CommitAnalysis(
            WorldDiplomacyJob job,
            string raw,
            int maxAutomaticReplyDepth,
            IEnumerable<WorldDiplomacyThreat> threats,
            Func<string, WorldDiplomacyDocument> resolveDocument,
            Func<string, WorldDiplomacyRound> resolveRound,
            Func<string, string> resolveKingdomCanonicalId,
            Func<string, string> resolveKingdomName,
            Func<JObject, string, string, WorldDiplomacyPeaceTerms> parseAndValidatePeaceTerms,
            Func<IEnumerable<string>, string, List<string>> normalizeKingdomIdList,
            Action<WorldDiplomacyDocument, string> suppressInvalid,
            Action<WorldDiplomacyDocument, string, string, bool, string, float> processAnalyzedDocument,
            Action<string> log,
            Func<string, IEnumerable<WorldDiplomacyRoundOffer>> liveOpenOffers = null)
        {
            WorldDiplomacyDocument document = resolveDocument?.Invoke(job.DocumentId);
            if (document == null)
            {
                return;
            }
            if (document.IsPlayerAuthored && (document.PlayerAnalysisCommitted || document.ChangedDiplomaticState
                || document.AnalysisStatus is "success" or "published_action_rejected")) return;
            JObject json = WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(raw);
            if (document.IsPlayerAuthored) document.AnsweredNpcDocumentIds = WorldDiplomacyEnvelopeJsonRules.ReadStringList(json, "answered_npc_document_ids");
            if (document.IsPlayerAuthored && WorldDiplomacyEnvelopeJsonRules.ReadString(json, "failure_reason") == "output_truncated")
            {
                MarkPlayerAnalysisFailed(document, log);
                document.MechanicalResult = PlayerAnalysisTruncatedResult;
                return;
            }
            if (document.IsPlayerAuthored && json?["actions"] != null)
            {
                CommitPlayerActions();
                return;
            }
            // Parse all entries before submitting one ordered execution command.
            // Transient records reuse the scalar parser; they never enter storage or publish.
            void CommitPlayerActions()
            {
                if (json["actions"] is not JArray entries || entries.Count == 0)
                { MarkPlayerAnalysisFailed(document, log); return; }
                var parsed = new List<WorldDiplomacyDocumentAction>(entries.Count);
                WorldDiplomacyDocument primary = null;
                foreach (JToken entry in entries)
                {
                    if (entry is not JObject action || action["actions"] != null)
                    { MarkPlayerAnalysisFailed(document, log); return; }
                    var single = (JObject)action.DeepClone();
                    foreach (string field in new[] { "status", "title_summary", "tone", "confidence" })
                        if (single[field] == null && json[field] != null) single[field] = json[field].DeepClone();
                    string requestedTarget = WorldDiplomacyEnvelopeJsonRules.ReadString(single,
                        "primary_target_kingdom_id", "target_kingdom_id", "target");
                    string extractedIntent = WorldDiplomacyIntentVocabulary.NormalizeIntent(WorldDiplomacyEnvelopeJsonRules.ReadString(single, "intent"));
                    if (WorldDiplomacyIntentVocabulary.IsActionableDiplomacyIntent(extractedIntent)
                        && (string.IsNullOrWhiteSpace(requestedTarget) || string.IsNullOrWhiteSpace(resolveKingdomCanonicalId(requestedTarget))))
                    { MarkPlayerAnalysisFailed(document, log); return; }
                    var draft = new WorldDiplomacyDocument {
                        DocumentId = document.DocumentId, RoundId = document.RoundId, ExchangeId = document.ExchangeId,
                        AuthorKingdomId = document.AuthorKingdomId, AuthorKingdomName = document.AuthorKingdomName,
                        TargetKingdomId = document.TargetKingdomId, TargetKingdomName = document.TargetKingdomName,
                        Body = document.Body, Title = document.Title, IsPlayerAuthored = true,
                        IsReadyForPublication = document.IsReadyForPublication, SourceDocumentId = document.SourceDocumentId,
                        IsResponse = document.IsResponse, AutomaticReplyDepth = document.AutomaticReplyDepth,
                        SubjectReleaseTokens = document.SubjectReleaseTokens
                    };
                    bool accepted = false; string rejection = null;
                    CommitAnalysis(job, single.ToString(), maxAutomaticReplyDepth, threats,
                        id => id == document.DocumentId ? draft : resolveDocument(id), resolveRound,
                        resolveKingdomCanonicalId, resolveKingdomName, parseAndValidatePeaceTerms, normalizeKingdomIdList,
                        (_, reason) => rejection = reason,
                        (d, i, c, response, t, score) => {
                            parsed.Add(new WorldDiplomacyDocumentAction {
                                ActionId = "action_" + (parsed.Count + 1), TargetKingdomId = d.TargetKingdomId,
                                DependencyActionIds = WorldDiplomacyEnvelopeJsonRules.ReadStringList(action, "dependency_action_ids"),
                                TargetKingdomName = d.TargetKingdomName, Intent = i, Commitment = c, RequiresResponse = response,
                                PeaceTerms = d.PeaceTerms, TreatyTerms = d.TreatyTerms,
                                RespondingToOfferDocumentId = d.RespondingToOfferDocumentId, RespondingToOfferActionId = d.RespondingToOfferActionId,
                                RespondingToThreatDocumentId = d.RespondingToThreatDocumentId, RespondingToThreatActionId = d.RespondingToThreatActionId
                            }); accepted = true;
                        }, log, liveOpenOffers);
                    if (!accepted || rejection != null)
                    {
                        MarkPlayerAnalysisFailed(document, log);
                        if (draft.AnalysisStatus == "analysis_failed") document.MechanicalResult = draft.MechanicalResult;
                        log?.Invoke("player compound analysis failed action=" + parsed.Count + " reason=" + (rejection ?? draft.AnalysisStatus));
                        return;
                    }
                    primary ??= draft;
                }
                document.Actions = parsed;
                WorldDiplomacyDocumentFactRules.MirrorPrimaryActionToDocument(document, parsed[0]);
                document.Title = primary.Title;
                document.Tone = primary.Tone; document.Confidence = primary.Confidence;
                document.AnalysisStatus = primary.AnalysisStatus;
                document.PresentedThreatDocumentIds = primary.PresentedThreatDocumentIds;
                document.PresentedThreatFollowThroughDocumentIds = primary.PresentedThreatFollowThroughDocumentIds;
                document.AddressedKingdomIds = normalizeKingdomIdList(parsed.Select(x => x.TargetKingdomId)
                    .Concat(WorldDiplomacyEnvelopeJsonRules.ReadStringList(json, "addressed_kingdom_ids")), document.AuthorKingdomId);
                document.MentionedKingdomIds = normalizeKingdomIdList(WorldDiplomacyEnvelopeJsonRules.ReadStringList(json, "mentioned_kingdom_ids"), document.AuthorKingdomId);
                document.DiscussionRoundId = WorldDiplomacyEnvelopeJsonRules.ReadString(json, "related_round_id");
                document.DiscussionSourceDocumentId = WorldDiplomacyEnvelopeJsonRules.ReadString(json, "related_public_document_id");
                document.RequiresResponse = parsed.Any(x => x.RequiresResponse);
                WorldDiplomacyReputationRules.ApplyInternationalReputationEvaluation(document, json);
                document.PlayerAnalysisCommitted = true;
                processAnalyzedDocument(document, document.Intent, document.Commitment, document.RequiresResponse, document.Tone, document.Confidence);
            }
            if (document.IsPlayerAuthored)
            {
                document.DiscussionRoundId = WorldDiplomacyEnvelopeJsonRules.ReadString(json, "related_round_id");
                document.DiscussionSourceDocumentId = WorldDiplomacyEnvelopeJsonRules.ReadString(json, "related_public_document_id");
            }
            string status = WorldDiplomacyIntentVocabulary.NormalizeToken(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "status"));
            string intent = WorldDiplomacyIntentVocabulary.NormalizeIntent(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "intent", "diplomatic_intent"));
            string titleSummary = WorldDiplomacyEnvelopeJsonRules.ReadString(json, "title_summary", "summary_title");
            string targetId = WorldDiplomacyEnvelopeJsonRules.ReadString(json, "primary_target_kingdom_id", "target_kingdom_id", "target");
            List<string> addressedIds = WorldDiplomacyEnvelopeJsonRules.ReadStringList(json, "addressed_kingdom_ids", "addressed");
            List<string> mentionedIds = WorldDiplomacyEnvelopeJsonRules.ReadStringList(json, "mentioned_kingdom_ids", "mentioned");
            string commitment = WorldDiplomacyIntentVocabulary.NormalizeCommitment(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "commitment"));
            string tone = WorldDiplomacyIntentVocabulary.NormalizeTone(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "tone"));
            float confidence = WorldDiplomacyEnvelopeJsonRules.ReadFloat(json, "confidence");
            bool requiresResponse = WorldDiplomacyEnvelopeJsonRules.ReadBool(json, "requires_response");
            string respondingToOfferDocumentId = WorldDiplomacyEnvelopeJsonRules.ReadString(json, "responding_to_offer_document_id");
            string respondingToThreatDocumentId = WorldDiplomacyEnvelopeJsonRules.ReadString(json, "responding_to_threat_document_id");
            bool unavailablePlayerOfferSource = false;
            if (document.IsPlayerAuthored && !WorldDiplomacyIntentVocabulary.IsSupportedDiplomacyIntent(intent))
            {
                MarkPlayerAnalysisFailed(document, log);
                return;
            }
            document.RespondingToOfferActionId = WorldDiplomacyEnvelopeJsonRules.ReadString(json, "responding_to_offer_action_id");
            document.RespondingToThreatActionId = WorldDiplomacyEnvelopeJsonRules.ReadString(json, "responding_to_threat_action_id");
            if (!string.Equals(status, "success", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(status, "fallback", StringComparison.OrdinalIgnoreCase))
            {
                if (!document.IsPlayerAuthored)
                {
                    document.AnalysisStatus = "no_action";
                    suppressInvalid(document, "analysis_status_has_no_publishable_action");
                    return;
                }
                // Supported extracted semantics remain authoritative despite classifier status.
                status = "fallback";
                log("player declaration analysis status normalized without changing intent=" + intent + " document=" + document.DocumentId
                    + " reason=analysis_status_" + WorldDiplomacyIntentVocabulary.NormalizeToken(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "status")));
            }
            if (string.IsNullOrWhiteSpace(intent))
            {
                document.AnalysisStatus = "no_action";
                suppressInvalid(document, "analysis_has_no_structured_intent");
                return;
            }
            if (document.IsPlayerAuthored)
            {
                // Match offers with the canonical IDs used by final execution.
                string normalizedTarget = resolveKingdomCanonicalId?.Invoke(
                    FirstNonEmpty(targetId, document.TargetKingdomId));
                if (!string.IsNullOrWhiteSpace(normalizedTarget)) targetId = normalizedTarget;
                // Reuse the owner's knowledge-filtered live offers for normalization and binding.
                // Never broaden global document identity matching or inspect the saved archive.
                bool offerResponse = !string.IsNullOrEmpty(WorldDiplomacyIntentVocabulary.ResponseIntentToProposalIntent(intent));
                IEnumerable<WorldDiplomacyRoundOffer> knownOffers = offerResponse
                    ? liveOpenOffers?.Invoke(document.AuthorKingdomId)?.ToList() : null;
                NormalizePlayerOfferSourcePrefix(document, intent, targetId, ref knownOffers,
                    resolveDocument, ref respondingToOfferDocumentId, log);
                if (knownOffers != null)
                {
                    ReconcilePlayerDeclarationWithOpenOffer(document, intent, knownOffers, ref targetId, ref respondingToOfferDocumentId, log);
                    unavailablePlayerOfferSource = !string.IsNullOrWhiteSpace(respondingToOfferDocumentId)
                        && !knownOffers.Any(x => MatchesDocumentId(x.SourceDocumentId, respondingToOfferDocumentId));
                }
                else if (liveOpenOffers == null)
                {
                    // Detached legacy callers have no knowledge provider. Production always supplies the owner.
                    var sourceRound = string.IsNullOrWhiteSpace(respondingToOfferDocumentId) ? null
                        : resolveRound?.Invoke(resolveDocument?.Invoke(respondingToOfferDocumentId)?.RoundId);
                    ReconcilePlayerDeclarationWithOpenOffer(document, intent, sourceRound ?? resolveRound?.Invoke(document.RoundId), ref targetId, ref respondingToOfferDocumentId, log);
                }
            }
            bool playerPublicIntent = document.IsPlayerAuthored && WorldDiplomacyIntentVocabulary.IsSupportedDiplomacyIntent(intent);
            if ((!WorldDiplomacyIntentVocabulary.IsActionableDiplomacyIntent(intent) && !playerPublicIntent)
                || !WorldDiplomacyOfferContractRules.CommitmentMatchesIntent(intent, commitment))
            {
                if (!document.IsPlayerAuthored)
                {
                    document.AnalysisStatus = "no_action";
                    suppressInvalid(document, "analysis_has_no_actionable_intent");
                    return;
                }
                commitment = WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent(intent);
                status = "fallback";
                log("player declaration analysis normalized without suppressing publication document=" + document.DocumentId
                    + " intent=" + intent + " commitment=" + commitment);
            }
            if (string.IsNullOrWhiteSpace(targetId))
            {
                targetId = document.TargetKingdomId;
            }
            string canonicalTargetId = resolveKingdomCanonicalId?.Invoke(targetId);
            if (!string.IsNullOrWhiteSpace(canonicalTargetId) && !string.Equals(canonicalTargetId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase))
            {
                document.TargetKingdomId = canonicalTargetId;
                document.TargetKingdomName = resolveKingdomName?.Invoke(targetId) ?? string.Empty;
            }
            if (document.IsPlayerAuthored && intent == "propose_peace"
                && json?["peace_terms"] != null && json["peace_terms"].Type != JTokenType.Null
                && json["peace_terms"] is not JObject)
            {
                MarkPlayerAnalysisFailed(document, log);
                return;
            }
            WorldDiplomacyPeaceTerms analyzedPeaceTerms = parseAndValidatePeaceTerms?.Invoke(
                json,
                document.AuthorKingdomId,
                targetId);
            if (document.IsPlayerAuthored
                && string.Equals(intent, "accept_peace", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(respondingToOfferDocumentId))
            {
                WorldDiplomacyDocument source = resolveDocument?.Invoke(respondingToOfferDocumentId);
                var offeredTerms = WorldDiplomacyDocumentFactRules.ResolveOfferedPeaceTerms(
                    source,
                    document.RespondingToOfferActionId);
                if (analyzedPeaceTerms != null && !WorldDiplomacyOfferContractRules.ArePeaceTermsEquivalent(analyzedPeaceTerms, offeredTerms))
                {
                    MarkPlayerAnalysisFailed(document, log);
                    document.MechanicalResult = PlayerAnalysisAcceptAndAmendResult;
                    return;
                }
                document.PeaceTerms = WorldDiplomacyOfferContractRules.ClonePeaceTerms(offeredTerms);
            }
            else
            {
                document.PeaceTerms = analyzedPeaceTerms ?? document.PeaceTerms;
            }
            IEnumerable<string> directTargets = addressedIds.Concat(new[] { document.TargetKingdomId });
            document.AddressedKingdomIds = normalizeKingdomIdList(directTargets, document.AuthorKingdomId);
            document.MentionedKingdomIds = normalizeKingdomIdList(mentionedIds, document.AuthorKingdomId);
            document.AnalysisStatus = status == "success" ? "success" : "fallback";
            document.Title = !string.IsNullOrWhiteSpace(titleSummary)
                ? WorldDiplomacyTextRules.Limit(WorldDiplomacyTextRules.SanitizePublicDiplomacyText(titleSummary), 36)
                : (document.IsPlayerAuthored ? WorldDiplomacyTextRules.BuildFallbackDocumentTitle(document, intent, DiplomacyPromptTexts.Get) : document.Title);
            document.Intent = intent;
            document.Commitment = commitment;
            // Player text is immutable once submitted, but publication order is not. A
            // threat decision or follow-through that became due while analysis was queued
            // still applies to this next published declaration.
            document.PresentedThreatDocumentIds = WorldDiplomacyRoundLifecycleRules.SelectPresentedThreatStageDocumentIds(threats, document.AuthorKingdomId);
            document.PresentedThreatFollowThroughDocumentIds = WorldDiplomacyRoundLifecycleRules.SelectNoncompliedThreatStageDocumentIds(threats, document.AuthorKingdomId);
            document.RespondingToOfferDocumentId = respondingToOfferDocumentId ?? "";
            document.RespondingToThreatDocumentId = respondingToThreatDocumentId ?? "";
            if (!string.IsNullOrWhiteSpace(document.RespondingToOfferDocumentId))
            {
                document.SourceDocumentId = document.RespondingToOfferDocumentId;
                document.IsResponse = true;
            }
            else if (!string.IsNullOrWhiteSpace(document.RespondingToThreatDocumentId))
            {
                document.SourceDocumentId = document.RespondingToThreatDocumentId;
                document.IsResponse = true;
            }
            if (json?["treaty_terms"] is JObject treaty
                && (!string.IsNullOrWhiteSpace(WorldDiplomacyEnvelopeJsonRules.ReadString(treaty, "receiving_kingdom_id"))
                    || !string.IsNullOrWhiteSpace(WorldDiplomacyEnvelopeJsonRules.ReadString(treaty, "joining_kingdom_id"))
                    || WorldDiplomacyEnvelopeJsonRules.ReadInteger(treaty, "daily_tribute") != 0
                    || WorldDiplomacyEnvelopeJsonRules.ReadInteger(treaty, "duration_days") != 0
                    || (intent == "accept_trade" && treaty["duration_days"] != null)
                    || !string.IsNullOrWhiteSpace(WorldDiplomacyEnvelopeJsonRules.ReadString(treaty, "cession_settlement_id"))))
                document.TreatyTerms = new WorldDiplomacyDialogueTerms {
                    ReceivingKingdomId = WorldDiplomacyEnvelopeJsonRules.ReadString(treaty, "receiving_kingdom_id"),
                    JoiningKingdomId = WorldDiplomacyEnvelopeJsonRules.ReadString(treaty, "joining_kingdom_id"),
                    DailyTribute = WorldDiplomacyEnvelopeJsonRules.ReadInteger(treaty, "daily_tribute"),
                    DurationDays = intent != "accept_trade" ? WorldDiplomacyEnvelopeJsonRules.ReadInteger(treaty, "duration_days")
                        : treaty["duration_days"] == null ? 0
                        : WorldDiplomacyEnvelopeJsonRules.TryReadInteger(treaty, "duration_days", out int treatyDays) ? treatyDays : -1,
                    CessionSettlementId = WorldDiplomacyEnvelopeJsonRules.ReadString(treaty, "cession_settlement_id") };
            if (document.IsPlayerAuthored && WorldDiplomacyIntentVocabulary.IsFormalTreatyIntent(intent)
                && intent.StartsWith("propose_", StringComparison.Ordinal)
                && (string.IsNullOrWhiteSpace(document.TreatyTerms?.ReceivingKingdomId)
                    || string.IsNullOrWhiteSpace(document.TreatyTerms?.JoiningKingdomId)))
            {
                MarkPlayerAnalysisFailed(document, log);
                document.MechanicalResult = PlayerAnalysisTreatyRolesResult;
                return;
            }
            if (WorldDiplomacyIntentVocabulary.IsFormalTreatyIntent(intent) && document.TreatyTerms != null
                && json?["peace_terms"] is JObject incompatiblePeace)
            {
                if (WorldDiplomacyEnvelopeJsonRules.TryReadInteger(incompatiblePeace, "daily_tribute", out int formalTribute)
                    && formalTribute != 0 && document.TreatyTerms.DailyTribute == 0) document.TreatyTerms.DailyTribute = formalTribute;
                if (WorldDiplomacyEnvelopeJsonRules.TryReadInteger(incompatiblePeace, "duration_days", out int formalDays)
                    && formalDays != 0 && document.TreatyTerms.DurationDays == 0) document.TreatyTerms.DurationDays = formalDays;
                if (string.IsNullOrWhiteSpace(document.TreatyTerms.CessionSettlementId))
                    document.TreatyTerms.CessionSettlementId = WorldDiplomacyEnvelopeJsonRules.ReadString(incompatiblePeace, "cession_settlement_id");
            }
            document.Tone = tone;
            document.Confidence = confidence;
            document.RequiresResponse = WorldDiplomacyIntentVocabulary.ResolveValidatedResponseObligation(document, intent, requiresResponse, maxAutomaticReplyDepth);
            WorldDiplomacyReputationRules.ApplyInternationalReputationEvaluation(document, json);
            if (document.IsPlayerAuthored) document.PlayerAnalysisCommitted = true;
            if (unavailablePlayerOfferSource)
            {
                suppressInvalid(document, "player_offer_response_source_not_available");
                return;
            }
            processAnalyzedDocument(document, intent, commitment, document.RequiresResponse, tone, confidence);
        }

    private static void NormalizePlayerOfferSourcePrefix(WorldDiplomacyDocument document, string intent,
        string targetId, ref IEnumerable<WorldDiplomacyRoundOffer> knownOffers,
        Func<string, WorldDiplomacyDocument> resolveDocument, ref string sourceId, Action<string> log)
    {
        const string prefix = "diplomacy_document:";
        string proposalIntent = WorldDiplomacyIntentVocabulary.ResponseIntentToProposalIntent(intent);
        if (knownOffers == null || string.IsNullOrEmpty(proposalIntent) || string.IsNullOrWhiteSpace(targetId)
            || string.IsNullOrWhiteSpace(sourceId) || sourceId.IndexOf(':') >= 0) return;
        // Only malformed-source replies need a reusable snapshot; normal replies keep the lazy path.
        knownOffers = knownOffers as IList<WorldDiplomacyRoundOffer> ?? knownOffers.ToList();
        string candidateId = prefix + sourceId;
        WorldDiplomacyRoundOffer match = null;
        foreach (var offer in knownOffers)
        {
            if (offer == null || !IsOpenDirectedOffer(offer, targetId, document.AuthorKingdomId)
                || !string.Equals(WorldDiplomacyIntentVocabulary.NormalizeIntent(offer.Intent), proposalIntent, StringComparison.OrdinalIgnoreCase)
                || !MatchesDocumentId(offer.SourceDocumentId, candidateId)
                || (!string.IsNullOrWhiteSpace(document.RespondingToOfferActionId)
                    && !string.Equals(offer.SourceActionId, document.RespondingToOfferActionId, StringComparison.Ordinal))) continue;
            if (match != null) return; // Multiple actions/records must not be guessed.
            match = offer;
        }
        if (match == null) return;
        var source = resolveDocument?.Invoke(match.SourceDocumentId);
        if (source?.IsReadyForPublication != true
            || !MatchesDocumentId(source.DocumentId, match.SourceDocumentId)
            || !string.Equals(source.AuthorKingdomId, match.ProposerKingdomId, StringComparison.OrdinalIgnoreCase)) return;
        log?.Invoke("player declaration offer source prefix normalized document=" + document.DocumentId
            + " source=" + sourceId + " canonical=" + match.SourceDocumentId + " action=" + match.SourceActionId);
        sourceId = match.SourceDocumentId;
        document.RespondingToOfferActionId = match.SourceActionId ?? "";
    }

    internal static void MarkPlayerAnalysisFailed(WorldDiplomacyDocument document, Action<string> log)
    {
        if (document == null || document.PlayerAnalysisCommitted || document.ChangedDiplomaticState) return;
        document.AnalysisStatus = "analysis_failed";
        document.MechanicalResult = PlayerAnalysisFailedResult;
        log?.Invoke("player declaration analysis failed without executing or rewriting speech document=" + document.DocumentId);
    }

    public static void SuppressInvalidDocumentBeforePropagation(
        WorldDiplomacyDocument document,
        string reason,
        WorldDiplomacyStorage storage,
        Func<string, WorldDiplomacyRound> resolveRound,
        Func<string, WorldDiplomacyDocument> resolveDocument,
        Func<string, string> resolveKingdomId,
        Func<int> currentDay,
        Action<WorldDiplomacyDocument, string> preservePublishedPlayerDocument,
        Action<WorldDiplomacyRound> scheduleResultSettlementTurn,
        Action<WorldDiplomacyRound> advanceRelay,
        Action<string, string> completeExchange,
        Action<string> closeActiveRound,
        Action<string> log)
    {
        if (document == null) return;
        if (document.IsPlayerAuthored && document.IsReadyForPublication)
        {
            preservePublishedPlayerDocument(document, reason);
            return;
        }
        log("invalid generated document suppressed before propagation document=" + document.DocumentId
            + " author=" + (document.AuthorKingdomId ?? "") + " target=" + (document.TargetKingdomId ?? "")
            + " reason=" + (reason ?? ""));
        WorldDiplomacyRound round = resolveRound(document.RoundId);
        bool wasRoundRoot = round != null
            && WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(round.RootDocumentId, document.DocumentId);
        storage.Documents.RemoveAll(x => x != null && WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(x.DocumentId, document.DocumentId));
        if (wasRoundRoot) round.RootDocumentId = "";
        if (round != null && resolveDocument(round.RootDocumentId) == null)
        {
            round.RootDocumentId = WorldDiplomacyRoundLifecycleRules.OrderDocumentsChronologically(storage.Documents
                    .Where(x => x != null && WorldDiplomacyRoundLifecycleRules.IsRecordInRound(x.RoundId, round.RoundId)))
                .Select(x => x.DocumentId)
                .FirstOrDefault() ?? "";
        }
        round?.LlmProfiledKingdomIds?.RemoveAll(x => string.Equals(x, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase));
        if (document.IsPlayerAuthored && document.IsResponse && round != null)
        {
            WorldDiplomacyRoundParticipant participant = WorldDiplomacyStructureRules.EnsureRoundParticipant(round, document.AuthorKingdomId, "active", mandatoryReply: true);
            participant.MandatoryReplyPending = true;
            WorldDiplomacyPlayerOpportunity opportunity = (storage.PlayerOpportunities ?? new List<WorldDiplomacyPlayerOpportunity>())
                .FirstOrDefault(x => x != null && WorldDiplomacyRoundLifecycleRules.IsRecordInRound(x.RoundId, round.RoundId));
            if (opportunity != null) opportunity.Status = "open";
        }
        if (document.IsRelayTurn && round != null && WorldDiplomacyRoundLifecycleRules.IsActiveRoundState(round.State))
        {
            if (round.ResultSettlementPending)
            {
                if (document.IsPlayerAuthored)
                {
                    round.RelayWaiting = true;
                    WorldDiplomacyRoundLifecycleRules.RecordPlayerOpportunity(round, resolveKingdomId(document.AuthorKingdomId), storage?.PlayerOpportunities, storage?.Documents, currentDay());
                }
                else
                {
                    round.RelayWaiting = false;
                    WorldDiplomacyRoundLifecycleRules.SkipResultSettlementSlot(round, document.ResultSettlementSlotId, document.AuthorKingdomId, "invalid_document", storage?.DiplomaticThreats, currentDay(), log);
                    scheduleResultSettlementTurn(round);
                }
            }
            else
            {
                round.RelayWaiting = false;
                advanceRelay(round);
            }
            return;
        }
        completeExchange(document.ExchangeId, "technical_invalid_document_suppressed");
        if (WorldDiplomacyRoundLifecycleRules.ShouldCloseRoundAfterInvalidSuppression(
            WorldDiplomacyLiveRoundRules.Contains(storage, round) ? round : null, round, storage.Documents, storage.Jobs, document.DocumentId))
        {
            closeActiveRound(document.IsPlayerAuthored
                ? "player_declaration_rejected"
                : "technical_invalid_document_suppressed");
        }
    }

    internal static void PreservePublishedPlayerDocumentAfterRejectedMechanic(IWorldDiplomacyDocumentExecutionPort port,
        IWorldDiplomacyOrchestration orchestration,
		WorldDiplomacyDocument document,
		string reason)
	{
		if (document == null) return;
		string normalizedIntent = WorldDiplomacyIntentVocabulary.NormalizeIntent(document.Intent);
		if (!WorldDiplomacyIntentVocabulary.IsSupportedDiplomacyIntent(normalizedIntent))
		{
			normalizedIntent = "statement";
			document.Intent = normalizedIntent;
		}
		if (!WorldDiplomacyOfferContractRules.CommitmentMatchesIntent(normalizedIntent, document.Commitment))
		{
			document.Commitment = WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent(normalizedIntent);
		}
		document.AnalysisStatus = "published_action_rejected";
		string readableReason = DescribeRejectedPlayerMechanic(reason);
		if (string.IsNullOrWhiteSpace(document.MechanicalResult))
		{
			// Stored wording stays byte-identical: saves, history snapshots and LLM prompts read it verbatim.
			document.MechanicalResult = MechanicNotExecutedPrefix + readableReason;
		}
		port.Log("published player declaration retained after mechanic rejection document=" + document.DocumentId
			+ " intent=" + normalizedIntent + " reason=" + (reason ?? ""));
		// The notification is a display boundary: show localized wording and pass the dynamic reason
		// as a placeholder argument so it is never dropped.
		port.Notify(
			DiplomacyUiLocalization.Format("AF_DIP_UI_WorldDiplomacy_DeclarePublishedMechanicRejectedDetail", "The diplomatic declaration has been published, but the diplomatic action parsed from it was not executed: {REASON}", ("REASON", DescribeRejectedPlayerMechanicForDisplay(reason))));
		WorldDiplomacyDocumentExecutionApplication.FinalizePublishedDocumentAfterAnalysis(port, orchestration,
			document,
			port.ResolveKingdomId(document.AuthorKingdomId),
			port.ResolveKingdomId(document.TargetKingdomId),
			normalizedIntent,
			recordNoActionDecision: true);
	}

	// Player-facing wording for a rejected published action; the raw code stays in the log.
	internal static string DescribeRejectedPlayerMechanic(string reason)
	{
		string code = reason ?? "";
		const string peacePrefix = "peace_terms_not_executable_without_changes:";
		int peaceReasonIndex = code.IndexOf(peacePrefix, StringComparison.Ordinal);
		if (peaceReasonIndex >= 0)
		{
			string peaceReason = code.Substring(peaceReasonIndex + peacePrefix.Length).Trim();
			if (!string.IsNullOrWhiteSpace(peaceReason)) return DiplomacyCompatibilityTexts.Get("Analysis_RejectedPeacePrefix") + peaceReason;
		}
		const string warPrefix = "declare_war_not_legal:";
		int warReasonIndex = code.IndexOf(warPrefix, StringComparison.Ordinal);
		if (warReasonIndex >= 0)
		{
			string warReason = code.Substring(warReasonIndex + warPrefix.Length).Trim();
			if (!string.IsNullOrWhiteSpace(warReason)) return DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_Reject_DeclareWarNotExecuted", "Declaration of war not executed: ") + warReason;
		}
		string detail = TryMatchRejectedPlayerMechanic(code, out string detailKey, out string detailFallback)
			? DiplomacyUiLocalization.Text(detailKey, detailFallback)
			: DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_Reject_UnsupportedByState", "The current situation does not support the parsed action.");
		return string.IsNullOrWhiteSpace(code) ? detail : detail + "（" + code + "）";
	}

	// MechanicalResult is persisted ([JsonProperty("mechanicalResult")] on the declaration and on each
	// of its actions) and is read back verbatim by save data, history snapshots, publication and LLM
	// prompts, so the player analysis-failure texts below keep their original wording byte-for-byte.
	// Only the notification boundary projects them onto localized text; see
	// ProjectMechanicalResultForDisplay.
	internal static string PlayerAnalysisTruncatedResult => DiplomacyMiscPromptTexts.Get("Analysis_TruncatedResult", "Declaration published; model output truncated due to token limit, diplomatic action not executed. Increase the API output limit and choose Reparse.");
	internal static string PlayerAnalysisAcceptAndAmendResult => DiplomacyMiscPromptTexts.Get("Analysis_AcceptAndAmendResult", "Declaration published; analysis returned both acceptance and amendment, diplomatic action not executed. Original text can be reparsed; amended terms should be submitted as a new proposal.");
	internal static string PlayerAnalysisTreatyRolesResult => DiplomacyMiscPromptTexts.Get("Analysis_TreatyRolesResult", "Declaration published; analysis failed to distinguish party roles, proposal not executed. Original text can be reparsed.");
	internal static string PlayerAnalysisFailedResult => DiplomacyMiscPromptTexts.Get("Analysis_FailedResult", "Declaration published; analysis failed, diplomatic action not executed. Choose Reparse to process the original text again.");
	internal static string MechanicNotExecutedPrefix => DiplomacyMiscPromptTexts.Get("Analysis_MechanicNotExecutedPrefix", "Diplomatic action not executed: ");

	// Display projection of a stored player analysis-failure text. Returns false for every value this
	// class did not write, so the caller can show the real stored text instead of a generic sentence;
	// the stored text itself is never rewritten.
	internal static bool TryProjectMechanicalResultForDisplay(string mechanicalResult, out string display)
	{
		if (string.Equals(mechanicalResult, PlayerAnalysisTruncatedResult, StringComparison.Ordinal))
		{
			display = DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_AnalysisFailedTruncated", "The declaration has been published; the model output was cut off by the length limit, so no diplomatic action has been executed yet. Raise the output limit of the corresponding API and then choose Re-analyze.");
			return true;
		}
		if (string.Equals(mechanicalResult, PlayerAnalysisAcceptAndAmendResult, StringComparison.Ordinal))
		{
			display = DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_AnalysisFailedAcceptAndAmend", "The declaration has been published; the analysis returned both acceptance of the original proposal and amended terms, so no diplomatic action was executed. You can re-analyze the original text; the amended terms should be submitted as a new proposal.");
			return true;
		}
		if (string.Equals(mechanicalResult, PlayerAnalysisTreatyRolesResult, StringComparison.Ordinal))
		{
			display = DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_AnalysisFailedTreatyRoles", "The declaration has been published; the analysis could not tell the two parties' roles in the treaty apart, so the proposal was not executed. You can re-analyze the original text.");
			return true;
		}
		if (string.Equals(mechanicalResult, PlayerAnalysisFailedResult, StringComparison.Ordinal))
		{
			display = DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_AnalysisFailed", "The declaration has been published; the analysis failed and no diplomatic action was executed. You can choose Re-analyze to process the original text again.");
			return true;
		}
		display = null;
		return false;
	}

	internal static string ProjectMechanicalResultForDisplay(string mechanicalResult)
	{
		return TryProjectMechanicalResultForDisplay(mechanicalResult, out string display) ? display : mechanicalResult;
	}

	// Display-only projection of a rejected published action. The stored MechanicalResult keeps
	// DescribeRejectedPlayerMechanic's wording byte-for-byte, so the notification builds its own
	// localized wording and passes every dynamic part as a placeholder argument. A reason this class
	// cannot describe falls back to the raw reason text instead of a generic sentence.
	internal static string DescribeRejectedPlayerMechanicForDisplay(string reason)
	{
		string code = reason ?? "";
		const string peacePrefix = "peace_terms_not_executable_without_changes:";
		int peaceReasonIndex = code.IndexOf(peacePrefix, StringComparison.Ordinal);
		if (peaceReasonIndex >= 0)
		{
			string peaceReason = code.Substring(peaceReasonIndex + peacePrefix.Length).Trim();
			if (!string.IsNullOrWhiteSpace(peaceReason))
				return DiplomacyUiLocalization.Format("AF_DIP_UI_WorldDiplomacy_Reject_PeaceNotExecutedDetail", "Peace proposal not executed: {REASON}", ("REASON", peaceReason));
		}
		const string warPrefix = "declare_war_not_legal:";
		int warReasonIndex = code.IndexOf(warPrefix, StringComparison.Ordinal);
		if (warReasonIndex >= 0)
		{
			string warReason = code.Substring(warReasonIndex + warPrefix.Length).Trim();
			if (!string.IsNullOrWhiteSpace(warReason))
				return DiplomacyUiLocalization.Format("AF_DIP_UI_WorldDiplomacy_Reject_DeclareWarNotExecutedDetail", "Declaration of war not executed: {REASON}", ("REASON", warReason));
		}
		if (TryMatchRejectedPlayerMechanic(code, out string detailKey, out string detailFallback))
			return DiplomacyUiLocalization.Format("AF_DIP_UI_WorldDiplomacy_Reject_WithCode", "{DETAIL} ({CODE})",
				("DETAIL", DiplomacyUiLocalization.Text(detailKey, detailFallback)), ("CODE", code));
		// Not describable: show the real reason text. A blank reason has no text of its own, so the
		// generic sentence stays the best wording available.
		return string.IsNullOrWhiteSpace(code)
			? DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_Reject_UnsupportedByState", "The current situation does not support the parsed action.")
			: code;
	}

	// Single wording table for a rejected published action, shared by the stored wording
	// (DescribeRejectedPlayerMechanic) and the display projection. Returns false when the reason code
	// cannot be described; callers then decide what to show instead of getting a generic sentence.
	private static bool TryMatchRejectedPlayerMechanic(string code, out string detailKey, out string detailFallback)
	{
		if (code.Contains("source_not_available") || code.Contains("source_not_known"))
		{
			detailKey = "AF_DIP_UI_WorldDiplomacy_Reject_SourceUnavailable";
			detailFallback = "The original proposal has not reached your kingdom, has expired, or does not exist, so this reply cannot be executed.";
			return true;
		}
		if (code.Contains("trade_acceptance_changes_terms"))
		{
			detailKey = "AF_DIP_UI_WorldDiplomacy_Reject_TradeTermsChanged";
			detailFallback = "Accepting a trade proposal cannot change its duration or add clauses; issue a new proposal for different terms.";
			return true;
		}
		if (code.Contains("missing_source_offer") || code.Contains("without_exact_open_offer") || code.Contains("required_peace_offer_response_missing"))
		{
			detailKey = "AF_DIP_UI_WorldDiplomacy_Reject_NoOpenOffer";
			detailFallback = "No still-valid formal proposal from the other party could be matched. Confirm their proposal declaration has arrived and is still open, or accept directly on that declaration.";
			return true;
		}
		if (code.Contains("treaty_roles_must_match_participants") || code.Contains("treaty_requires_explicit_receiving_and_joining_roles"))
		{
			detailKey = "AF_DIP_UI_WorldDiplomacy_Reject_TreatyRoles";
			detailFallback = "The treaty's receiving and joining kingdoms (or suzerain and subject) must be exactly the two parties to this negotiation.";
			return true;
		}
		if (code.Contains("treaty_acceptance_cannot_change_source_roles") || code.Contains("accept_peace_changes_offer_terms"))
		{
			detailKey = "AF_DIP_UI_WorldDiplomacy_Reject_AcceptChangesTerms";
			detailFallback = "Accepting a proposal cannot change its terms; issue a new proposal for different terms.";
			return true;
		}
		if (code.Contains("treaty_would_create_cycle"))
		{
			detailKey = "AF_DIP_UI_WorldDiplomacy_Reject_TreatyCycle";
			detailFallback = "That treaty would create a cycle of vassalage.";
			return true;
		}
		if (code.Contains("subject_already_has_treaty"))
		{
			detailKey = "AF_DIP_UI_WorldDiplomacy_Reject_SubjectHasTreaty";
			detailFallback = "The subject kingdom already has a vassalage treaty.";
			return true;
		}
		if (code.Contains("treaty_participants_not_available"))
		{
			detailKey = "AF_DIP_UI_WorldDiplomacy_Reject_TreatyParticipantsUnavailable";
			detailFallback = "A kingdom or ruler party to the treaty is currently unavailable.";
			return true;
		}
		if (code.Contains("unsupported_extra_treaty_clause"))
		{
			detailKey = "AF_DIP_UI_WorldDiplomacy_Reject_UnsupportedClause";
			detailFallback = "That treaty does not support extra tribute, duration, or cession clauses.";
			return true;
		}
		if (code.Contains("peace_intent_between_kingdoms_not_at_war") || code.Contains("peace_legality_guard"))
		{
			detailKey = "AF_DIP_UI_WorldDiplomacy_Reject_NotAtWar";
			detailFallback = "The two parties are not currently at war.";
			return true;
		}
		if (code.Contains("peace_terms"))
		{
			detailKey = "AF_DIP_UI_WorldDiplomacy_Reject_PeaceTerms";
			detailFallback = "The peace terms cannot currently be executed as written.";
			return true;
		}
		if (code.Contains("declare_war_not_legal"))
		{
			detailKey = "AF_DIP_UI_WorldDiplomacy_Reject_DeclareWarNotLegal";
			detailFallback = "The conditions for declaring war are not currently met.";
			return true;
		}
		if (code.Contains("alliance_intent_conflicts") || code.Contains("trade_intent_conflicts"))
		{
			detailKey = "AF_DIP_UI_WorldDiplomacy_Reject_StateConflict";
			detailFallback = "This conflicts with the two parties' current alliance or trade status.";
			return true;
		}
		if (code.Contains("final_live_legal_action_guard") || code.Contains("intent_not_in_current_legal_action_list"))
		{
			detailKey = "AF_DIP_UI_WorldDiplomacy_Reject_NotInLegalActions";
			detailFallback = "That action is not among the diplomatic actions the two parties can currently execute.";
			return true;
		}
		if (code.Contains("no_live_target") || code.Contains("no_eligible_parties"))
		{
			detailKey = "AF_DIP_UI_WorldDiplomacy_Reject_NoLiveTarget";
			detailFallback = "The target kingdom does not exist, has been destroyed, or has no independent diplomatic authority.";
			return true;
		}
		detailKey = null;
		detailFallback = null;
		return false;
	}
}
