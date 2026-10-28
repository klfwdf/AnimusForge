using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Persistence;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    private void HoldTechnicalRequest(WorldDiplomacyJob job, string reason)
    {
        job.IsRunning = false;
        job.SuspendedReason = string.IsNullOrWhiteSpace(reason) ? "technical_request_failed" : reason;
        job.TechnicalRecoveryAttempts++;
        // Internal repair state belongs in diagnostics, never in diplomatic news.
        // Log before recovery selection without falsely claiming that it stopped.
        _host.Log("diplomacy.request_repair event=" + job.RoundId + " job=" + job.JobId
            + " kind=" + job.Kind + " attempt=" + job.TechnicalRecoveryAttempts + " reason=" + job.SuspendedReason);
        job.RecoveryConfiguration = SpeechConfiguration();
        job.RecoveryServiceEpoch = Storage.SpeechServiceRecoveryEpoch;
        var round = ResolveRound(job.RoundId);
        if ((job.SuspendedReason.StartsWith("admission_identity_", StringComparison.Ordinal)
                || job.SuspendedReason.StartsWith("admission_core_", StringComparison.Ordinal)
                || job.SuspendedReason.StartsWith("admission_seed_identity_changed", StringComparison.Ordinal))
            && TryRetireInvalidatedAdmission(job, round)) return;
        if (TryCloseExhaustedAdmissionDraft(job, round)) return;
        bool admissionProtocolFailure = IsAdmissionProtocolFailure(job, job.SuspendedReason);
        if (admissionProtocolFailure)
            job.MaterialRepairFeedback = job.SuspendedReason.Length <= 3200 ? job.SuspendedReason : job.SuspendedReason.Substring(0, 3200);
        bool materialFailure = !admissionProtocolFailure && IsEventPlanningJob(job) && (job.SuspendedReason.StartsWith("event_", StringComparison.Ordinal)
            || (job.SuspendedReason.StartsWith("admission_seed_", StringComparison.Ordinal)
                && !job.SuspendedReason.StartsWith("admission_seed_identity_changed", StringComparison.Ordinal))
            || job.SuspendedReason.StartsWith("admission_participants_", StringComparison.Ordinal)
            || job.SuspendedReason.StartsWith("narrative_", StringComparison.Ordinal)
            || job.SuspendedReason.StartsWith("player_opening_", StringComparison.Ordinal)
            || job.SuspendedReason.StartsWith("public_narrative_", StringComparison.Ordinal)
            || job.SuspendedReason.StartsWith("unanswered_player_", StringComparison.Ordinal)
            || job.SuspendedReason == "pipeline_json_invalid"
            || job.SuspendedReason.StartsWith("input budget exceeded", StringComparison.Ordinal));
        if (materialFailure || admissionProtocolFailure)
        {
            job.MaterialRepairFeedback = job.SuspendedReason.Length <= 3200 ? job.SuspendedReason : job.SuspendedReason.Substring(0, 3200);
            if (CanContinueMaterialSearch(round) && (job.TechnicalRecoveryAttempts >= 3
                || job.SuspendedReason.StartsWith("input budget exceeded", StringComparison.Ordinal)))
            {
                _host.Log("diplomacy.material_repair_exhausted job=" + job.JobId + " reason=" + job.SuspendedReason);
                round.EventSelectionEvidence = Newtonsoft.Json.JsonConvert.SerializeObject(new {
                    outcome = "unpublished_material_failure", reason = job.MaterialRepairFeedback, stage = job.EventSelectionStage });
                if (!TryRemainingMaterialCandidates(job, round, job.SuspendedReason))
                    ContinueMaterialSearch(job, round, false);
                return;
            }
        }
        if (!admissionProtocolFailure && !job.SuspendedReason.StartsWith("admission_capture_", StringComparison.Ordinal)
            && !job.SuspendedReason.StartsWith("admission_seed_identity_changed", StringComparison.Ordinal)
            && !job.SuspendedReason.StartsWith("admission_identity_", StringComparison.Ordinal)
            && !job.SuspendedReason.StartsWith("admission_core_", StringComparison.Ordinal)
            && !job.SuspendedReason.StartsWith("admission_country_material_", StringComparison.Ordinal)
            && !job.SuspendedReason.StartsWith("admission_material_host_", StringComparison.Ordinal)
            && !job.SuspendedReason.StartsWith("admission_snapshot_", StringComparison.Ordinal)
            && job.TechnicalRecoveryAttempts >= 3 && (IsEventPlanningJob(job)
                || (job.Kind is "event_boundary" or "event_boundary_audit" && job.ForcedIntent == "started"))
            && !IsBoundMatterPlanning(job) && string.IsNullOrEmpty(job.DialogueArrangementId)
            && CanDiscardUnpublishedMaterial(round))
        {
            foreach (var lead in Storage.MaterialLeads.Where(x => x.EventId == round.RoundId && x.Status == "selected_unpublished"))
                lead.Status = "failed_unused";
            WorldDiplomacyRoundApplication.Close(Storage, "unpublished_material_failure:" + job.SuspendedReason, _host.CurrentDay,
                SettleTradeAllianceOfferCooldownsForClosedRound, ScheduleNextNormalRoundAfter,
                CommitLocalRoundSummary, TryScheduleTokenCompression, _host.Log, round);
            InvalidateDialogueIndex();
            _host.Log("diplomacy.unpublished_draft_released event=" + round.RoundId);
            return;
        }
        job.RecoveryRevision = round?.ConversationRevision ?? 0;
        job.RecoveryKnowledgeRevision = DiplomacyEventRules.KnowledgeRevision(round, job.AuthorKingdomId);
        job.RetryAfterUtcTicks = DateTime.UtcNow.AddSeconds(30 * Math.Min(3, job.TechnicalRecoveryAttempts)).Ticks;
        // Schema/semantic correction is not a transport outage. Keep the same
        // finite recovery budget and frozen sources without waiting 30/60 seconds.
        if (admissionProtocolFailure || (materialFailure
            && !job.SuspendedReason.StartsWith("event_material_capture_failed", StringComparison.Ordinal))
            || (job.Kind is "event_boundary" or "event_boundary_audit"
                && (job.SuspendedReason.StartsWith("boundary_", StringComparison.Ordinal)
                    || job.SuspendedReason.StartsWith("event_boundary_", StringComparison.Ordinal)
                    || job.SuspendedReason.StartsWith("opening_", StringComparison.Ordinal)
                    || job.SuspendedReason.StartsWith("narrative_", StringComparison.Ordinal)
                    || job.SuspendedReason.StartsWith("public_narrative_", StringComparison.Ordinal)
                    || job.SuspendedReason == "pipeline_json_invalid"))
            || (job.Kind == "privacy_audit" && string.IsNullOrEmpty(job.DialogueArrangementId)
                && job.SuspendedReason.StartsWith("declaration_audit_invalid:", StringComparison.Ordinal)))
            job.RetryAfterUtcTicks = 0;
        job.NextRecoveryProbeUtcTicks = 0;
        if (round != null && (string.IsNullOrEmpty(round.TechnicalHoldReason)
            || Storage.Jobs.Any(x => x.RoundId == round.RoundId && x.SuspendedReason == round.TechnicalHoldReason)))
        {
            round.TechnicalHoldReason = job.SuspendedReason;
            round.TechnicalRequestHoldReason = job.SuspendedReason;
            if (round.TechnicalHoldStartedHour < 0) round.TechnicalHoldStartedHour = _host.CurrentCampaignHours();
        }
    }

    // Bounded automatic retries; thereafter only a changed input/configuration,
    // confirmed service recovery or explicit disable/enable starts another budget.
    // The scheduler already visits the bounded job queue. Expensive probes run at
    // most once per 30 seconds per held job, never scan retained document history.
    private void TryResumeTechnicalRequest(WorldDiplomacyJob job, bool explicitResume = false)
    {
        if (!_host.WorldDiplomacyEnabled() || string.IsNullOrEmpty(job.SuspendedReason)
            || IsSpeechReviewJob(job) || !string.IsNullOrEmpty(job.DialogueArrangementId)) return;
        var round = ResolveRound(job.RoundId);
        if (ResumeRejectedStory(job)) return;
        bool exhaustedDraftRepair = IsAdmissionProtocolFailure(job, job.SuspendedReason)
            || job.SuspendedReason.StartsWith("event_candidate", StringComparison.Ordinal)
            || job.SuspendedReason.StartsWith("event_comparison", StringComparison.Ordinal)
            || job.SuspendedReason.StartsWith("event_concept", StringComparison.Ordinal)
            || (job.SuspendedReason.StartsWith("admission_seed_", StringComparison.Ordinal)
                && !job.SuspendedReason.StartsWith("admission_seed_identity_changed", StringComparison.Ordinal))
            || job.SuspendedReason.StartsWith("admission_participants_", StringComparison.Ordinal)
            || job.SuspendedReason.StartsWith("narrative_", StringComparison.Ordinal)
            || job.SuspendedReason == "pipeline_json_invalid";
        if (IsEventPlanningJob(job) && exhaustedDraftRepair && job.TechnicalRecoveryAttempts >= 3 && CanContinueMaterialSearch(round))
        {
            // Old saves may already be parked at the retired permanent hold.
            // Consume the persisted batch budget rather than refilling attempts.
            round.EventSelectionEvidence = Newtonsoft.Json.JsonConvert.SerializeObject(new {
                outcome = "unpublished_material_failure", reason = "restored_planning_repair_exhausted:" + job.SuspendedReason,
                stage = job.EventSelectionStage });
            if (!TryRemainingMaterialCandidates(job, round, job.SuspendedReason)) ContinueMaterialSearch(job, round, false);
            return;
        }
        long now = DateTime.UtcNow.Ticks;
        bool localSchemaRepair = IsEventPlanningJob(job) && (IsAdmissionProtocolFailure(job, job.SuspendedReason)
            || IsLegacyAdmissionProtocolFailure(job, job.SuspendedReason)
            || job.SuspendedReason.StartsWith("event_candidate", StringComparison.Ordinal)
            || job.SuspendedReason.StartsWith("event_comparison", StringComparison.Ordinal));
        if (!explicitResume && !localSchemaRepair && (now < job.RetryAfterUtcTicks || now < job.NextRecoveryProbeUtcTicks)) return;
        job.NextRecoveryProbeUtcTicks = DateTime.UtcNow.AddSeconds(30).Ticks;
        if (!IsLiveRound(round) || !DiplomacyEventRules.IsEvent(round)) return;
        // Unknown execution results need their evidence owner, never an LLM reroll.
        if (round.TechnicalHoldReason is "event_document_execution_uncertain" or "legacy_unverified_work_preserved"
            || Storage.EventEffects.Any(x => x.EventId == round.RoundId && x.Status is "applying" or "uncertain")) return;
        bool resumedWorldBook = false;
        if (IsWorldBookWait(job) && _host is IDiplomacyWorldBookHost)
        {
            resumedWorldBook = ResumePlayerWorldBook(job, round);
            if (!resumedWorldBook) return;
        }
        if (UpgradeLegacyCountrySelection(job, round)) return;
        bool recoveredAdmissionIdentity = HasRecoveredAdmissionIdentity(job, round);
        // Also consumes already-exhausted legacy saves, without replenishing
        // retries on reload or treating unknown identity as a valid participant.
        if (!recoveredAdmissionIdentity && TryCloseExhaustedAdmissionDraft(job, round)) return;
        if ((job.SuspendedReason.StartsWith("admission_identity_", StringComparison.Ordinal)
                || job.SuspendedReason.StartsWith("admission_core_", StringComparison.Ordinal)
                || job.SuspendedReason.StartsWith("admission_seed_identity_changed", StringComparison.Ordinal))
            && TryRetireInvalidatedAdmission(job, round)) return;
        string configuration = SpeechConfiguration();
        // The former commit guard rejected a queued final duty solely because
        // ordinary participation had ended. Recheck its exact live obligation;
        // clearing this reason consumes recovery without refilling other holds.
        bool repairedEndingAuthor = job.SuspendedReason == "author_is_not_current_event_participant"
            && HasBoundEndingJob(round, job, job.AuthorKingdomId);
        // The old writer omitted foundation fields from its main schema and
        // retried without feedback. Re-evaluate that exact held contract once;
        // new failures use specific diagnostics and cannot repeat this migration.
        bool repairedFoundationContract = job.Kind is "event_boundary" or "event_boundary_audit"
            && job.ForcedIntent == "started" && job.SuspendedReason == "opening_background_or_positions_missing";
        bool repairedOpeningEffects = job.Kind is "event_boundary" or "event_boundary_audit"
            && job.ForcedIntent == "started" && (job.SuspendedReason.StartsWith("event_boundary_requires_grounded_effect", StringComparison.Ordinal)
                || job.SuspendedReason.Contains("target_already_at_bound;omit_unavailable_change;neutral_ending_may_have_empty_effects"));
        // The retired diagnostic identifies the old impossible empty-NPC contract.
        // New failures have a distinct diagnostic, so reload cannot refill retries.
        bool repairedEmptyPlayerOpening = job.Kind is "event_boundary" or "event_boundary_audit"
            && job.ForcedIntent == "started" && HasPublishedPlayerOpening(round)
            && job.SuspendedReason == "opening_positions_participant_mismatch"
            && !round.Participants.Any(x => x.State != "observer" && !_host.IsPlayerParty(x.KingdomId));
        bool repairedDeclarationContract = job.Kind == "privacy_audit" && job.SuspendedReason == "privacy_rewrites_exhausted";
        // The new evidence request can repair old exhausted citation audits once.
        // Its persisted version prevents a reload from replenishing retries again.
        bool repairedEvidenceContract = job.Kind == "privacy_audit" && job.DeclarationAuditEvidenceVersion < 3
            && job.SuspendedReason.StartsWith("declaration_audit_invalid:", StringComparison.Ordinal);
        // This unqualified error is retired: new validation includes action and
        // field. Clearing the old reason consumes this one-time recovery without
        // adding a save field or resetting unrelated exhausted failures.
        bool repairedOptionalTermsContract = (job.Kind == "generate" || IsPrivacyPipelineJob(job))
            && job.SuspendedReason == "diplomatic_action_terms_shape_invalid";
        bool repairedAnswerContract = job.Kind == "privacy_audit"
            && job.SuspendedReason == "declaration_rewrites_exhausted:required_agenda_answer_missing";
        bool repairedDraftContract = job.DeclarationRepairVersion < 1 && (job.Kind == "generate" || IsPrivacyPipelineJob(job))
            && (job.SuspendedReason.StartsWith("declaration_rewrites_exhausted:", StringComparison.Ordinal)
                || job.SuspendedReason.StartsWith("declaration_audit_invalid:", StringComparison.Ordinal));
        bool repairedProgressRoute = IsEventProgressJob(job) && job.EventProgressContractVersion < 1
            && job.SuspendedReason == "event_progress_repair_exhausted:event_progress_invalid";
        // The retired generic diagnostic could never identify a paraphrased
        // quotation. Consume the existing persisted version once; later failures
        // retain their finite budget, including across save/load.
        bool repairedWorldBookAudit = job.EventNarrativeReviewVersion < 5 && IsPlayerDeclarationOpening(round)
            && job.Kind is "event_concept_audit" or "event_boundary_audit" or "event_progress_audit"
            && job.SuspendedReason.Contains("narrative_world_book_review_missing:provide_world_identity_valid_and_exact_source_quotes");
        bool repairedProgressBudget = IsEventProgressJob(job)
            && job.SuspendedReason.StartsWith("input budget exceeded before send:", StringComparison.Ordinal);
        bool repairedEndingBudget = job.EndingContextVersion < 1 && job.ForcedIntent == "ended"
            && job.Kind is "event_boundary" or "event_boundary_audit"
            && job.SuspendedReason.StartsWith("input budget exceeded before send:", StringComparison.Ordinal);
        bool repairedBoundMatter = IsEventPlanningJob(job) && IsBoundMatterPlanning(job) && job.StoryReviewVersion < 2
            && (job.SuspendedReason.Contains("story_review") || job.SuspendedReason.Contains("story_structure_duplicate")
                || job.SuspendedReason.Contains("story_motive_replaced")
                || job.SuspendedReason == "event_story_already_used_without_new_motive"
                || job.SuspendedReason.StartsWith("event_required_evidence_budget_exceeded:", StringComparison.Ordinal)
                || job.SuspendedReason.StartsWith("input budget exceeded before send:", StringComparison.Ordinal));
        bool repairedEndingEvidence = IsEndingEvidenceRequest(job) && job.EndingContextVersion < 2
            && (job.SuspendedReason.Contains("narrative_world_book_")
                || job.SuspendedReason.StartsWith("boundary_audit_rejected:", StringComparison.Ordinal)
                    && job.SuspendedReason.Contains("world_book_basis"));
        bool changed = resumedWorldBook || repairedEndingEvidence || repairedBoundMatter || repairedEndingBudget || repairedProgressBudget || repairedEndingAuthor || recoveredAdmissionIdentity || IsLegacyAdmissionProtocolFailure(job, job.SuspendedReason) || repairedWorldBookAudit || repairedOpeningEffects || repairedEmptyPlayerOpening || repairedProgressRoute || repairedDraftContract || repairedFoundationContract || repairedDeclarationContract || repairedEvidenceContract || repairedOptionalTermsContract || repairedAnswerContract || explicitResume || job.RecoveryConfiguration != configuration
            || job.RecoveryRevision != round.ConversationRevision
            || job.RecoveryKnowledgeRevision != DiplomacyEventRules.KnowledgeRevision(round, job.AuthorKingdomId)
            || job.RecoverOnService && job.RecoveryServiceEpoch < Storage.SpeechServiceRecoveryEpoch;
        if (!changed && job.TechnicalRecoveryAttempts >= 3) return;
        if (!changed && job.SuspendedReason.StartsWith("event_required_input_budget_exceeded:", StringComparison.Ordinal)) return;
        if (changed) job.TechnicalRecoveryAttempts = 0;
        if (repairedBoundMatter) { job.StoryReviewVersion = 2; job.MaterialRepairFeedback = ""; }
        if (repairedEndingBudget) job.EndingContextVersion = 1;
        if (repairedEndingEvidence)
        { job.EndingContextVersion = 2; job.PrivacySourceMaterial = ""; }
        // Consume the corrected audit contract once, including held v1 saves.
        if (repairedEvidenceContract) job.DeclarationAuditEvidenceVersion = 3;
        // Legacy held jobs get the same finite budget; reload cannot reset it.
        string reason = job.SuspendedReason;
        if (repairedWorldBookAudit) job.EventNarrativeReviewVersion = 5;
        if (repairedProgressRoute)
        {
            job.EventProgressContractVersion = 1;
            job.PrivacySourceMaterial = "event_progress_route_contract_changed:recheck_available_continuations_and_exact_open_offers;never_generate_player_decisions";
        }
        bool resumeBoundaryAudit = job.Kind == "event_boundary_audit"
            && reason.StartsWith("boundary_audit_invalid:", StringComparison.Ordinal)
            && !string.IsNullOrEmpty(job.PrivacyDraft) && job.RoundConversationRevision == round.ConversationRevision;
        bool resumeAudit = (job.Kind == "privacy_audit" || repairedDraftContract)
            && (repairedDraftContract || repairedDeclarationContract || repairedAnswerContract || reason.StartsWith("declaration_audit_invalid:", StringComparison.Ordinal))
            && !string.IsNullOrEmpty(job.PrivacyDraft)
            && job.RoundConversationRevision == round.ConversationRevision
            && job.CourtKnowledgeRevision == DiplomacyEventRules.KnowledgeRevision(round, job.AuthorKingdomId);
        if (repairedFoundationContract || repairedEmptyPlayerOpening) job.PrivacySourceMaterial = reason;
        job.SuspendedReason = ""; job.RetryAfterUtcTicks = 0; job.TransportRetryAttempts = 0;
        job.SemanticRepairAttempts = 0; job.LifecycleClassificationAttempts = 0;
        job.LlmMessages?.Clear(); job.PrivacyRewrites = 0; job.PrivacyApproved = false;
        job.PrivacyAuditRetries = 0;
        job.DeclarationRepairAttempts = 0;
        if (repairedDraftContract) job.DeclarationRepairVersion = 1;
        bool resumeConceptAudit = job.Kind == "event_concept_audit"
            && (repairedBoundMatter || reason.StartsWith("event_concept_audit_invalid:", StringComparison.Ordinal)
                || reason.StartsWith("event_required_evidence_budget_exceeded:", StringComparison.Ordinal))
            && !string.IsNullOrEmpty(job.PrivacyDraft) && job.RoundConversationRevision == round.ConversationRevision;
        if (!resumeAudit && job.Kind != "privacy_repair") { job.PrivacyPublicContext = ""; job.PrivacyRevision = -1; }
        // Metadata completion owns this draft, not a disposable audit response.
        if (!resumeAudit && !resumeBoundaryAudit && !IsUnpublishedPlayerLifecycleJob(job) && !IsEventPlanningJob(job)
            && job.Kind is not "privacy_repair" and not "event_boundary" and not "event_boundary_audit" and not "event_progress" and not "event_progress_audit") job.PrivacyDraft = "";
        if (resumeAudit && WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(job.PrivacyDraft)?["response_requests"] is not Newtonsoft.Json.Linq.JArray
            && job.MaterialCountryContractVersion >= 1 && !IsOralDeclarationJob(job))
            BeginDraftRepair(job, "response_requests", reason);
        else if (resumeAudit) PrepareDeclarationAudit(job);
        else if (job.Kind == "privacy_repair") PrepareDraftRepair(job);
        else if (IsPrivacyPipelineJob(job)) job.Kind = "generate";
        if (job.Kind == "event_concept_audit" && !resumeConceptAudit) job.Kind = "event_concept";
        if (job.Kind == "event_progress_audit" && !((repairedProgressBudget && !string.IsNullOrEmpty(job.PrivacyDraft)
                && job.RoundConversationRevision == round.ConversationRevision)
            || reason.StartsWith("event_progress_audit_invalid:", StringComparison.Ordinal)
            && !string.IsNullOrEmpty(job.PrivacyDraft) && job.RoundConversationRevision == round.ConversationRevision)) job.Kind = "event_progress";
        if (job.Kind == "event_boundary_audit" && !resumeBoundaryAudit) job.Kind = "event_boundary";
        if ((round.TechnicalHoldReason == reason || round.TechnicalHoldReason == round.TechnicalRequestHoldReason)
            && !Storage.Jobs.Any(x => x.RoundId == round.RoundId && !string.IsNullOrEmpty(x.SuspendedReason)))
        {
            AccrueTechnicalPause(round);
            round.TechnicalHoldStartedHour = -1;
            round.TechnicalHoldReason = "";
            round.TechnicalRequestHoldReason = "";
        }
        MarkEventDirty(round);
        _host.Log("diplomacy.technical_request_resumed job=" + job.JobId + " attempts=" + job.TechnicalRecoveryAttempts + " changed=" + changed);
    }

    private void AccrueTechnicalPause(WorldDiplomacyRound round)
    {
        if (round.TechnicalHoldStartedHour < 0) return;
        double now = _host.CurrentCampaignHours();
        foreach (var wait in round.PlayerNegotiationWaits.Where(x => x.RequiresAnswer && x.NotifiedHour >= 0
            && string.IsNullOrEmpty(x.AnswerDocumentId)))
            wait.NotifiedHour += Math.Max(0, now - Math.Max(round.TechnicalHoldStartedHour, wait.NotifiedHour));
        round.TechnicalHoldStartedHour = now;
    }

    private void RecoverPlayerDocumentRoutingAndRetireClosedJobs()
    {
        // Old saves may contain pending player analysis in a completed/missing
        // round. Move the same public artifact before retiring obsolete work.
        foreach (var document in Storage.Documents.Where(x => x?.IsPlayerAuthored == true && x.IsReadyForPublication
            && x.AnalysisStatus == "pending_analysis" && !x.PlayerAnalysisCommitted).ToList())
        {
            var round = EnsurePlayerDocumentRound(document);
            // Older analysis jobs only stored ExchangeId. Restore their actual
            // case owner before dispatch checks, without replacing the job or
            // resetting its persisted failure budget.
            foreach (var job in Storage.Jobs.Where(x => x?.Kind == "analyze"
                && x.DocumentId == document.DocumentId && string.IsNullOrEmpty(x.RoundId)))
            {
                job.RoundId = job.ExchangeId = round.RoundId;
                DiplomacyEventRules.Bind(round, job);
            }
            EnqueueAnalysisJob(document, 100);
        }
        Storage.Jobs.RemoveAll(job => job != null && job.Kind != "compress"
            && !string.IsNullOrWhiteSpace(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(job.RoundId, job.ExchangeId))
            && !IsLiveRound(ResolveRound(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(job.RoundId, job.ExchangeId))));
    }

    // Load/replace only; no saved-archive scan on campaign frames.
    private void NormalizeConcurrentWork()
    {
        WorldDiplomacyStorageShapeNormalizer.EnsureInitialized(Storage);
        Storage.MaterialInvestigations ??= new(StringComparer.Ordinal);
        Storage.MaterialLeads ??= new();
        Storage.CountryParticipation ??= new(StringComparer.Ordinal);
        Storage.PublishedPlayerContacts ??= new(StringComparer.Ordinal);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (Storage.ActiveRound != null) ids.Add(Storage.ActiveRound.RoundId);
        Storage.ConcurrentRounds = Storage.ConcurrentRounds.Where(x => x != null
            && !string.IsNullOrWhiteSpace(x.RoundId) && ids.Add(x.RoundId)).ToList();
        Storage.DialogueArrangements = Storage.DialogueArrangements.Where(x => x != null
            && !string.IsNullOrWhiteSpace(x.ArrangementId)).GroupBy(x => x.ArrangementId, StringComparer.Ordinal)
            .Select(x => x.OrderByDescending(y => y.Version).First()).ToList();
        foreach (var round in GetLiveRounds().Concat(Storage.CompletedRounds).Where(x => x != null))
        {
            // Reconsider only an uncommitted old closure. Published or staged
            // receipts (including unknown effects) must never be erased/replayed.
            if (IsLiveRound(round) && round.EventPhase == "closing" && round.EventClosureReviewVersion < 1
                && string.IsNullOrEmpty(round.EventEndingId)
                && !Storage.EventAnnouncements.Any(x => x.EventId == round.RoundId && x.Stage == "ended")
                && !Storage.EventEffects.Any(x => x.EventId == round.RoundId && (x.Stage == "ended" || x.Status is "applying" or "uncertain"))
                && !Storage.Documents.Any(x => x.RoundId == round.RoundId && x.EventExecutionPending))
            {
                var retired = Storage.Jobs.Where(x => x.RoundId == round.RoundId
                    && x.Kind is "event_boundary" or "event_boundary_audit" && x.ForcedIntent == "ended").ToList();
                if (retired.All(x => !x.IsRunning))
                {
                    foreach (var job in retired) RemoveJob(job.JobId);
                    round.EventPhase = "discussing"; round.EventOutcome = ""; round.EventOutcomeReason = "";
                    round.EventCloseReason = ""; round.EventProgressRevision = -1; round.EventClosureReviewVersion = 1;
                    if (retired.Any(x => x.SuspendedReason == round.TechnicalHoldReason)
                        && !Storage.Jobs.Any(x => x.RoundId == round.RoundId && !string.IsNullOrEmpty(x.SuspendedReason)))
                    { AccrueTechnicalPause(round); round.TechnicalHoldReason = ""; round.TechnicalRequestHoldReason = ""; round.TechnicalHoldStartedHour = -1; }
                    _host.Log("diplomacy.uncommitted_closure_reconsidered event=" + round.RoundId);
                }
            }
            foreach (var participant in round.Participants ?? new())
            {
                participant.AgendaSourceKeys ??= new(); participant.AgendaAddressTo ??= new();
                participant.AgendaAnswerDocumentIds ??= new();
            }
            foreach (var fact in round.EventNarrative ?? new()) fact.HeroIds ??= new();
            round.PlayerResponses = (round.PlayerResponses ?? new List<WorldDiplomacyPlayerResponse>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.SourceDocumentId) && !string.IsNullOrWhiteSpace(x.KingdomId))
                .GroupBy(x => x.SourceDocumentId + "|" + x.KingdomId, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.OrderByDescending(y => !string.IsNullOrWhiteSpace(y.AnswerDocumentId)).First()).ToList();
            foreach (var item in round.PlayerResponses)
                if (string.IsNullOrWhiteSpace(item.OriginalRoundId)) item.OriginalRoundId = round.RoundId;
        }
        var priorContacts = GetLiveRounds().Concat(Storage.CompletedRounds).Where(x => x != null && !string.IsNullOrEmpty(x.RoundId))
            .GroupBy(x => x.RoundId,StringComparer.Ordinal).ToDictionary(x => x.Key,x => x.First().PlayerContactPublicationId,StringComparer.Ordinal);
        foreach (var job in Storage.Jobs.Where(x => x != null))
        { job.AuditedAgendaAnswers ??= new(); job.DeclarationAnswerSources ??= new(); }
        foreach (var doc in Storage.Documents.Where(x => x != null))
        {
            doc.AnsweredPlayerDocumentIds ??= new List<string>();
            doc.ResponseRequests ??= new();
            doc.AuditedAgendaAnswers ??= new();
            if (doc.PlayerContactCounted)
            {
                if (string.IsNullOrEmpty(doc.PlayerContactId))
                {
                    priorContacts.TryGetValue(doc.RoundId ?? "",out var prior);
                    doc.PlayerContactId = string.IsNullOrEmpty(prior) ? doc.DocumentId : prior;
                }
                Storage.PublishedPlayerContacts.Add(doc.PlayerContactId);
            }
            doc.PersonalMemoryReceipts ??= new List<string>();
            doc.PendingPersonalMemoryRulers ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
        if (Storage.MaterialCountrySchemaVersion < 1)
        {
            // Only explicit core-expression evidence plus its actual answer establishes
            // participation. Preserve unknown legacy prose without guessing semantics.
            foreach (var doc in Storage.Documents.Where(x => x != null && !x.IsPlayerAuthored
                && x.IsReadyForPublication && x.RoundProgressHandled && !string.IsNullOrWhiteSpace(x.AuditedAgendaQuote)
                && Storage.Documents.Any(answer => answer.IsReadyForPublication && answer.AuditedAgendaAnswers.Contains(x.DocumentId))))
                RecordCountryParticipation(doc);
            foreach (var job in Storage.Jobs.Where(x => x != null))
            {
                job.MaterialCountryContractVersion = 1;
                if (ResolveRound(job.RoundId)?.EventSystemVersion >= 1 && job.Kind is "generate" or "privacy_material" or "privacy_audit")
                { job.Kind = "generate"; job.PrivacyApproved = false; job.PrivacyRevision = -1;
                    job.PrivacyPublicContext = ""; job.PrivacyDraft = ""; job.LlmMessages?.Clear(); }
            }
            Storage.MaterialCountrySchemaVersion = 1;
        }
        InvalidateDialogueIndex();
        _diplomacyWorkNeedsReconcile = true;
        NormalizePlayerEventState();
        NormalizePlayerLifecycleRecovery();
        RestoreEventBaselineAfterLoad();
    }

}
