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
            execution.Notify("外交宣言已发布，但分析失败，外交动作未执行。可打开该公文选择“重新解析”。");
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
                    document.MechanicalResult = "宣言已公开；解析同时给出接受原案和修改条款，外交动作未执行。可重新解析原文；修改条件应作为新提案。";
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
                document.MechanicalResult = "宣言已公开；解析未能辨明条约双方角色，提案未执行。可重新解析原文。";
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
        document.MechanicalResult = "宣言已公开；分析失败，外交动作未执行。可选择“重新解析”再次处理原文。";
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
			document.MechanicalResult = "外交动作未执行：" + readableReason;
		}
		port.Log("published player declaration retained after mechanic rejection document=" + document.DocumentId
			+ " intent=" + normalizedIntent + " reason=" + (reason ?? ""));
		port.Notify(
			"外交宣言已经发布，但其中解析出的外交动作未执行：" + readableReason);
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
		const string warPrefix = "declare_war_not_legal:";
		int warReasonIndex = code.IndexOf(warPrefix, StringComparison.Ordinal);
		if (warReasonIndex >= 0)
		{
			string warReason = code.Substring(warReasonIndex + warPrefix.Length).Trim();
			if (!string.IsNullOrWhiteSpace(warReason)) return "宣战未执行：" + warReason;
		}
		string detail =
            code.Contains("source_not_available") || code.Contains("source_not_known")
                ? "原提案尚未送达本国、已失效或不存在，不能执行这次回应。"
            : code.Contains("trade_acceptance_changes_terms")
                ? "接受贸易原案时不能修改期限或附加条款；修改条件请另发新提案。"
            : code.Contains("missing_source_offer") || code.Contains("without_exact_open_offer") || code.Contains("required_peace_offer_response_missing")
				? "未能对应到对方仍有效的正式提案。请确认对方的提案宣言已送达且仍开放，或在该宣言上直接回复接受。"
			: code.Contains("treaty_roles_must_match_participants") || code.Contains("treaty_requires_explicit_receiving_and_joining_roles")
				? "条约的接收国与并入国（或宗主国与臣属国）必须正好是本次交涉的双方。"
			: code.Contains("treaty_acceptance_cannot_change_source_roles") || code.Contains("accept_peace_changes_offer_terms")
				? "接受原案时不能修改原提案的条款；修改条件请另发新提案。"
			: code.Contains("treaty_would_create_cycle") ? "该条约会造成臣属关系循环。"
			: code.Contains("subject_already_has_treaty") ? "臣属方已存在臣属条约。"
			: code.Contains("treaty_participants_not_available") ? "条约一方的王国或统治者当前不可用。"
			: code.Contains("unsupported_extra_treaty_clause") ? "该条约不支持附加贡金、期限或割地条款。"
			: code.Contains("peace_intent_between_kingdoms_not_at_war") || code.Contains("peace_legality_guard") ? "双方当前并不处于战争状态。"
			: code.Contains("peace_terms") ? "和平条款当前无法原样执行。"
			: code.Contains("declare_war_not_legal") ? "当前不满足宣战条件。"
			: code.Contains("alliance_intent_conflicts") || code.Contains("trade_intent_conflicts") ? "与双方当前的同盟或贸易状态冲突。"
			: code.Contains("final_live_legal_action_guard") || code.Contains("intent_not_in_current_legal_action_list") ? "该动作不在双方当前可执行的外交动作之内。"
			: code.Contains("no_live_target") || code.Contains("no_eligible_parties") ? "对象王国不存在、已灭亡或没有独立外交权。"
			: "当前局势不支持解析出的动作。";
		return string.IsNullOrWhiteSpace(code) ? detail : detail + "（" + code + "）";
	}
}
