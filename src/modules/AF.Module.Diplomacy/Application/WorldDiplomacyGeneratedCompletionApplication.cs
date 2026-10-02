using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Main-thread generated-document admission and state transition.
internal static class WorldDiplomacyGeneratedCompletionApplication
{
    internal static void Commit(
            WorldDiplomacyJob job,
            string raw,
            WorldDiplomacyStorage storage,
            Func<string, WorldDiplomacyRound> resolveRound,
            Func<string, WorldDiplomacyDocument> resolveDocument,
            Func<string, string> resolveKingdomId,
            Func<string, string> getAuthorBlockReason,
            WorldDiplomacyRoundLifecycleRules.TryGetGeneratedLegalityViolation tryGetLegalityViolation,
            Func<JObject, string, string, WorldDiplomacyPeaceTerms> parsePeaceTerms,
            Func<WorldDiplomacyDocument, JObject, string, string, bool, bool, bool> tryApplySemanticEnvelope,
            Func<string, string, string, string, string, bool, bool, string, WorldDiplomacyDocument> createDocument,
            Func<int, string> formatCampaignDate,
            Action<WorldDiplomacyRound> scheduleResultSettlement,
            Action<WorldDiplomacyRound> pruneInvalidOffers,
            Action<WorldDiplomacyJob, string, string, string> abandonRejectedGeneration,
            Action<WorldDiplomacyJob, string, string, string, string, JObject> rejectDraft,
            Action<WorldDiplomacyDocument> addDocument,
            Action<WorldDiplomacyDocument, string, string, bool, string, float> processAnalyzedDocument,
            Action<string> log)
        {
            if (job == null) return;
            WorldDiplomacyRound jobRound = resolveRound?.Invoke(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(job.RoundId, job.ExchangeId));
            if (jobRound?.ResultSettlementPending == true)
            {
                WorldDiplomacyResultSettlementSlot currentSlot = (jobRound.ResultSettlementSlots ?? new List<WorldDiplomacyResultSettlementSlot>())
                    .FirstOrDefault(x => x != null
                        && string.Equals(x.SlotId, job.ResultSettlementSlotId, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(x.KingdomId, job.AuthorKingdomId, StringComparison.OrdinalIgnoreCase));
                if (currentSlot == null
                    || !string.Equals(jobRound.ResultSettlementCurrentSlotId, job.ResultSettlementSlotId, StringComparison.OrdinalIgnoreCase))
                {
                    log?.Invoke("stale result-settlement generation discarded job=" + job.JobId
                        + " round=" + jobRound.RoundId + " author=" + (job.AuthorKingdomId ?? ""));
                    jobRound.RelayWaiting = false;
                    scheduleResultSettlement?.Invoke(jobRound);
                    return;
                }
            }
            string authorId = resolveKingdomId?.Invoke(job.AuthorKingdomId);
            string fallbackTargetId = resolveKingdomId?.Invoke(job.TargetKingdomId);
            if (string.IsNullOrWhiteSpace(authorId))
            {
                abandonRejectedGeneration?.Invoke(job, null, fallbackTargetId, "generated_party_missing");
                return;
            }
            string authorBlockReason = getAuthorBlockReason?.Invoke(authorId);
            if (!string.IsNullOrEmpty(authorBlockReason))
            {
                log?.Invoke("generated declaration discarded at commit job=" + job.JobId + " author=" + authorId
                    + " reason=" + authorBlockReason);
                abandonRejectedGeneration?.Invoke(job, authorId, fallbackTargetId, authorBlockReason);
                return;
            }
            pruneInvalidOffers?.Invoke(jobRound);
            if (!WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject(raw, out JObject json))
            {
                rejectDraft?.Invoke(job, raw, authorId, fallbackTargetId, "json_parse_failed", null);
                return;
            }
            if (WorldDiplomacyEnvelopeJsonRules.TryNormalizeInlineResponseBinding(json, out string normalizedBindingKind))
            {
                log?.Invoke("normalized inline response binding job=" + job.JobId + " kind=" + normalizedBindingKind);
            }
            WorldDiplomacyEnvelopeJsonRules.NormalizeGeneratedDiplomaticEnvelopeShape(job, json);
            int removedRedundantStatements = WorldDiplomacyEnvelopeJsonRules.RemoveRedundantStatementActions(json);
            if (removedRedundantStatements > 0)
            {
                log?.Invoke("normalized redundant statement actions job=" + job.JobId
                    + " removed=" + removedRedundantStatements.ToString(CultureInfo.InvariantCulture)
                    + " remaining=" + ((json["actions"] as JArray)?.Count ?? 0).ToString(CultureInfo.InvariantCulture));
            }
            if (tryGetLegalityViolation(job, json, authorId, fallbackTargetId, out string resolvedTargetId, out string legalityReason))
            {
                rejectDraft?.Invoke(
                    job,
                    raw,
                    authorId,
                    resolvedTargetId ?? fallbackTargetId,
                    legalityReason,
                    json);
                return;
            }
            string targetId = resolvedTargetId;
            WorldDiplomacyDocument sourceDocument = resolveDocument?.Invoke(job.SourceDocumentId);
            string title = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(
                WorldDiplomacyEnvelopeJsonRules.ReadString(json, "title"),
                job.IsResponse ? "外交回应" : "王国外交宣言");
            title = WorldDiplomacyTextRules.Limit(WorldDiplomacyTextRules.SanitizePublicDiplomacyText(title), 100);
            string body = WorldDiplomacyTextRules.NormalizeBody(WorldDiplomacyTextRules.SanitizePublicDiplomacyText(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "body", "public_document", "document")));
            if (string.IsNullOrWhiteSpace(body))
            {
                rejectDraft?.Invoke(job, raw, authorId, targetId, "empty_public_document", json);
                return;
            }
            WorldDiplomacyDocument document = createDocument?.Invoke(
                authorId,
                targetId,
                title,
                body,
                job.IsResponse ? "ai_response" : "ai",
                false,
                job.IsResponse,
                job.ExchangeId);
            document.RoundId = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(job.RoundId, job.ExchangeId);
            if (job.IsRelayTurn && job.CreatedDay >= 0)
            {
                document.Day = job.CreatedDay;
                document.GameDate = formatCampaignDate?.Invoke(job.CreatedDay);
            }
            document.HiddenIntent = WorldDiplomacyIntentVocabulary.NormalizeIntent(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "author_intent.intent", "intent", "author_intent"));
            document.HiddenCommitment = WorldDiplomacyIntentVocabulary.NormalizeCommitment(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "author_intent.commitment", "commitment"));
            document.PeaceTerms = targetId == null ? null : parsePeaceTerms?.Invoke(json, authorId, targetId);
            document.SourceDocumentId = job.SourceDocumentId ?? "";
            document.RespondingToOfferDocumentId = WorldDiplomacyEnvelopeJsonRules.ReadString(json, "responding_to_offer_document_id");
            document.RespondingToThreatDocumentId = WorldDiplomacyEnvelopeJsonRules.ReadString(json, "responding_to_threat_document_id");
            document.SourceDocumentId = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(
                document.RespondingToOfferDocumentId,
                document.RespondingToThreatDocumentId,
                document.SourceDocumentId);
            document.PresentedThreatDocumentIds = new List<string>(job.PresentedThreatDocumentIds ?? new List<string>());
            document.PresentedThreatFollowThroughDocumentIds = new List<string>(job.PresentedThreatFollowThroughDocumentIds ?? new List<string>());
            document.IsExternalResponseOnly = job.IsExternalResponseOnly;
            document.IsReminder = job.IsReminder;
            document.IsRelayTurn = job.IsRelayTurn;
            document.ResultSettlementSlotId = job.ResultSettlementSlotId ?? "";
            document.RoundParticipation = WorldDiplomacyIntentVocabulary.NormalizeToken(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "round_participation"));
            if (document.RoundParticipation != "withdraw") document.RoundParticipation = "continue";
            document.RoundStatus = WorldDiplomacyIntentVocabulary.NormalizeToken(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "round_status"));
            if (document.RoundStatus != "resolved" && document.RoundStatus != "deadlocked") document.RoundStatus = "continue";
            document.MadeDiplomaticProgress = WorldDiplomacyEnvelopeJsonRules.ReadBool(json, "made_progress");
            document.HasEmbeddedRoundPlan = WorldDiplomacyRoundLifecycleRules.IsAutonomousOpeningJob(job);
            // Kept only for old saves. New opening documents are always actionable.
            document.IsAutonomousNoActionDeclaration = false;
            if (document.HasEmbeddedRoundPlan)
            {
                // The public title is the authoritative topic. This prevents a hidden round_plan label
                // from leaking a private long-term strategy into later prompts or the player archive.
                document.PlannedRoundTopic = WorldDiplomacyTextRules.Limit(title, 120);
                document.PlannedKingdomIds = WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(WorldDiplomacyEnvelopeJsonRules.ReadStringList(json, "round_plan.selected_kingdom_ids")
                    .Where(x => job.CandidateKingdomIds.Contains(x, StringComparer.OrdinalIgnoreCase)));
            }
            document.AutomaticReplyDepth = job.IsResponse ? Math.Max(1, (sourceDocument?.AutomaticReplyDepth ?? 0) + 1) : 0;
            if (tryApplySemanticEnvelope?.Invoke(document, json, authorId, targetId, job.AllowUntargeted,
                job.IsRelayTurn) != true)
            {
                rejectDraft?.Invoke(job, raw, authorId, targetId,
                    WorldDiplomacyEnvelopeJsonRules.GetGeneratedEnvelopeApplicationFailureReason(document, json, targetId, job.IsRelayTurn, resolveRound), json);
                return;
            }
            if (jobRound != null) jobRound.ConsecutiveTechnicalGenerationFailures = 0;
            WorldDiplomacyReputationRules.ApplyInternationalReputationEvaluation(document, json);
            if (string.Equals(document.RoundParticipation, "withdraw", StringComparison.OrdinalIgnoreCase)
                && !WorldDiplomacyIntentVocabulary.IsTerminalNegotiationMove(document.NegotiationMove))
            {
                document.RoundParticipation = "continue";
            }
            addDocument?.Invoke(document);
            WorldDiplomacyExchange exchange = WorldDiplomacyRoundLifecycleRules.ResolveExchange(storage?.ActiveExchange, storage?.SuspendedExchanges, job.ExchangeId);
            if (exchange != null)
            {
                if (job.IsResponse)
                {
                    exchange.ResponseDocumentId = document.DocumentId;
                    exchange.State = "analyzing_response";
                }
                else
                {
                    exchange.SourceDocumentId = document.DocumentId;
                    exchange.State = "analyzing_source";
                }
            }
            processAnalyzedDocument?.Invoke(document, document.Intent, document.Commitment, document.RequiresResponse, document.Tone, document.Confidence);
        }
}
