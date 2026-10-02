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
            (doc, intent, commitment, response, tone, confidence) => WorldDiplomacyDocumentExecutionApplication
                .ProcessAnalyzedDocument(execution, orchestration, doc, intent, commitment, response, tone, confidence),
            execution.Log);
    }
    internal static void Suppress(IWorldDiplomacyAnalysisPort port, IWorldDiplomacyOrchestration orchestration,
        WorldDiplomacyDocument document, string reason)
    {
        var execution = port.Execution;
        SuppressInvalidDocumentBeforePropagation(document, reason, port.Storage, execution.ResolveRound, execution.ResolveDocument,
            execution.ResolveKingdomId, () => execution.CurrentDay,
            (doc, why) => PreservePublishedPlayerDocumentAfterRejectedMechanic(execution, orchestration, doc, why),
            orchestration.ScheduleNextResultSettlementTurn, round => orchestration.AdvanceRelay(round),
            orchestration.CompleteExchange, orchestration.CloseActiveRound, execution.Log);
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
            Action<string> log)
        {
            WorldDiplomacyDocument document = resolveDocument?.Invoke(job.DocumentId);
            if (document == null)
            {
                return;
            }
            JObject json = WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(raw);
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
            if (!string.Equals(status, "success", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(status, "fallback", StringComparison.OrdinalIgnoreCase))
            {
                if (!document.IsPlayerAuthored)
                {
                    document.AnalysisStatus = "no_action";
                    suppressInvalid(document, "analysis_status_has_no_publishable_action");
                    return;
                }
                // Player speech is already public and authoritative. A no-action or malformed
                // classifier result means "public statement", never "permission denied".
                status = "fallback";
                intent = "statement";
                commitment = "non_binding";
                log("player declaration analysis downgraded to public statement document=" + document.DocumentId
                    + " reason=analysis_status_" + WorldDiplomacyIntentVocabulary.NormalizeToken(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "status")));
            }
            if (string.IsNullOrWhiteSpace(intent))
            {
                if (!document.IsPlayerAuthored)
                {
                    document.AnalysisStatus = "no_action";
                    suppressInvalid(document, "analysis_has_no_structured_intent");
                    return;
                }
                status = "fallback";
                intent = "statement";
                commitment = "non_binding";
                log("player declaration analysis supplied no intent; retained as public statement document=" + document.DocumentId);
            }
            if (document.IsPlayerAuthored)
            {
                ReconcilePlayerDeclarationWithOpenOffer(document, intent, resolveRound?.Invoke(document.RoundId), ref targetId, ref respondingToOfferDocumentId, log);
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
                if (!WorldDiplomacyIntentVocabulary.IsSupportedDiplomacyIntent(intent)) intent = "statement";
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
            WorldDiplomacyPeaceTerms analyzedPeaceTerms = parseAndValidatePeaceTerms?.Invoke(
                json,
                document.AuthorKingdomId,
                targetId);
            if (document.IsPlayerAuthored
                && string.Equals(intent, "accept_peace", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(respondingToOfferDocumentId))
            {
                WorldDiplomacyDocument source = resolveDocument?.Invoke(respondingToOfferDocumentId);
                document.PeaceTerms = WorldDiplomacyOfferContractRules.ClonePeaceTerms(WorldDiplomacyDocumentFactRules.ResolveOfferedPeaceTerms(
                    source,
                    document.RespondingToOfferActionId));
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
                : (document.IsPlayerAuthored ? WorldDiplomacyTextRules.BuildFallbackDocumentTitle(document, intent) : document.Title);
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
            document.Tone = tone;
            document.Confidence = confidence;
            document.RequiresResponse = WorldDiplomacyIntentVocabulary.ResolveValidatedResponseObligation(document, intent, requiresResponse, maxAutomaticReplyDepth);
            WorldDiplomacyReputationRules.ApplyInternationalReputationEvaluation(document, json);
            processAnalyzedDocument(document, intent, commitment, document.RequiresResponse, tone, confidence);
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
            storage.ActiveRound, round, storage.Documents, storage.Jobs, document.DocumentId))
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
		if (string.IsNullOrWhiteSpace(document.MechanicalResult))
		{
			document.MechanicalResult = "外交动作未执行：当前局势不支持解析出的动作。";
		}
		port.Log("published player declaration retained after mechanic rejection document=" + document.DocumentId
			+ " intent=" + normalizedIntent + " reason=" + (reason ?? ""));
		port.Notify(
			"外交宣言已经发布，但其中解析出的外交动作因当前局势不成立而未执行。");
		WorldDiplomacyDocumentExecutionApplication.FinalizePublishedDocumentAfterAnalysis(port, orchestration,
			document,
			port.ResolveKingdomId(document.AuthorKingdomId),
			port.ResolveKingdomId(document.TargetKingdomId),
			normalizedIntent,
			recordNoActionDecision: true);
	}
}
