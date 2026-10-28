using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    // Load/replace boundary only. The retired fields are an import format, never
    // a second scheduler. Published documents and execution receipts stay intact.
    private void RestoreEventBaselineAfterLoad()
    {
        var rounds = GetLiveRounds().Where(x => x.EventConvergence?.IssueVersion == 1).ToList();
        if (rounds.Count == 0) return;
        var documents = Storage.Documents.Where(x => x != null).ToLookup(x => x.RoundId);
        foreach (var round in rounds)
        {
            var state = round.EventConvergence;
            var jobs = Storage.Jobs.Where(x => x.RoundId == round.RoundId).ToList();
            if (jobs.Any(x => x.IsRunning)) continue;
            var records = documents[round.RoundId].ToList();
            bool stagedEnding = Storage.EventAnnouncements.Any(x => x.EventId == round.RoundId && x.Stage == "ended");
            bool executionHold = !string.IsNullOrEmpty(state.TechnicalFailure)
                || records.Any(x => x.EventExecutionPending)
                || Storage.EventEffects.Any(x => x.EventId == round.RoundId && x.Status is "applying" or "uncertain");
            foreach (var job in jobs)
            {
                bool unpublished = !records.Any(x => x.DocumentId == job.DocumentId);
                bool declaration = job.Kind is "generate" or "privacy_material" or "privacy_audit" or "privacy_repair";
                bool duty = !string.IsNullOrEmpty(job.DialogueArrangementId) || !string.IsNullOrEmpty(job.SilenceSourceDocumentId)
                    || !string.IsNullOrEmpty(job.EndingRecoveryDocumentId) || job.PlayerResponseSourceIds?.Count > 0
                    || records.Any(x => x.DocumentId == job.SourceDocumentId && x.IsPlayerAuthored)
                    || round.PlayerResponses.Any(x => x.KingdomId == job.AuthorKingdomId && x.Status == "pending")
                    || round.PendingOffers.Any(x => x.Status == "open" && (x.ProposerKingdomId == job.AuthorKingdomId || x.TargetKingdomId == job.AuthorKingdomId))
                    || Storage.DiplomaticThreats.Any(x => x.StageRoundId == round.RoundId && x.Status == "open"
                        && (x.IssuerKingdomId == job.AuthorKingdomId || x.TargetKingdomId == job.AuthorKingdomId));
                if (!executionHold && !stagedEnding && (IsEventProgressJob(job)
                    || job.ForcedIntent == "ended" && job.Kind is "event_boundary" or "event_boundary_audit"
                    || declaration && unpublished && !duty))
                {
                    RemoveJob(job.JobId);
                    continue;
                }
                if (declaration && unpublished)
                {
                    // Keep the original duty identity; replace only its uncommitted
                    // prose/audit request so retired prompts cannot survive a load.
                    job.Kind = "generate"; job.PrivacyApproved = false; job.PrivacyRevision = -1;
                    job.PrivacyPublicContext = ""; job.PrivacyDraft = ""; job.PrivacySourceMaterial = "";
                    job.PrivacyDeclarationSystem = ""; job.PrivacyRepairFeedback = "";
                    job.LlmMessages?.Clear(); job.RequestPreparationPending = false;
                    job.PrivacyAuditRetries = 0; job.PrivacyRewrites = 0;
                    if (IsRetiredIssueFailure(job.SuspendedReason))
                    { job.SuspendedReason = ""; job.TechnicalRecoveryAttempts = 0; job.RetryAfterUtcTicks = 0; }
                    var range = _host.DeclarationCharacterRange();
                    job.SystemPrompt = WorldDiplomacyPromptContractRules.BuildGenerationSystemPrompt(
                        _host.CommonDiplomacyContract(round), range.minimum, range.maximum, _host.PromptWorld().LocalizePrompt);
                    job.UserPrompt = ""; // Real request preparation rebuilds context from the preserved source.
                }
                job.NegotiationIssueId = ""; job.MaterialReactionId = "";
                job.DeclarationAnswerSources?.Clear(); job.DeclarationAnswerSnapshotVersion = 0;
            }
            if (!stagedEnding)
            {
                state.Version = 0; state.Ending = false; state.CompletedRounds = 0;
                state.Receipts = new(); state.ReceiptIndex = null;
                state.RequiredSources = new(); state.RequiredJobIds = new(); state.FinalDocumentIds = new();
                state.Route = ""; state.Actor = ""; state.Target = ""; state.Intent = ""; state.DecisionDocumentId = "";
                state.Result = "";
                if (round.EventPhase == "closing") round.EventPhase = "discussing";
                round.EventOutcome = ""; round.EventOutcomeReason = ""; round.EventCloseReason = "";
                round.EventProgressRevision = -1; round.EventWait = null;
            }
            foreach (var work in round.PlayerCourtWork.Where(x => x.Status == "event_progress_owned")) work.Status = "pending";
            state.IssueVersion = 0;
            state.Issues?.Clear(); state.Reactions?.Clear(); state.IssuePublicationIds?.Clear();
            state.TechnicalUnresolvedSpeakers?.Clear(); state.IssuesFrozen = false;
            if (!executionHold && !Storage.Jobs.Any(x => x.RoundId == round.RoundId && !string.IsNullOrEmpty(x.SuspendedReason))
                && IsRetiredIssueFailure(round.TechnicalHoldReason))
            {
                AccrueTechnicalPause(round);
                round.TechnicalHoldReason = ""; round.TechnicalRequestHoldReason = ""; round.TechnicalHoldStartedHour = -1;
            }
            round.ConversationRevision++;
            MarkEventDirty(round);
            _host.Log("diplomacy.event_baseline_restored event=" + round.RoundId + " staged_ending=" + stagedEnding + " execution_hold=" + executionHold);
        }
    }

    private static bool IsRetiredIssueFailure(string reason) => !string.IsNullOrEmpty(reason)
        && (reason.StartsWith("declaration_audit_invalid:", StringComparison.Ordinal)
            || reason.StartsWith("declaration_rewrites_exhausted:", StringComparison.Ordinal)
            || reason.Contains("material_reaction") || reason.Contains("event_ending_issue_work_unfinished"));
}
