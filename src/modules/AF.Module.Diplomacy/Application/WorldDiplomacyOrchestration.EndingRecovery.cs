using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    private void ClearEndingExecutionHold(WorldDiplomacyRound round)
    {
        string ownedReason = "event_ending_execution:" + round.EventConvergence.TechnicalFailure;
        round.EventConvergence.TechnicalFailure = "";
        if (round.TechnicalHoldReason == ownedReason)
        {
            AccrueTechnicalPause(round);
            round.TechnicalHoldReason = "";
            round.TechnicalHoldStartedHour = -1;
        }
        round.ConversationRevision++;
        MarkEventDirty(round);
        _host.Log("diplomacy.ending_execution_reconciled event=" + round.RoundId);
    }

    // Called by the existing bounded event queue, never by an archive scan.
    // Attempts and backoff survive reload; transport/draft repair use the normal
    // job owner. Unknown or partial effects are never automatically replayed.
    private void TryRecoverEndingExecution(WorldDiplomacyRound round)
    {
        var state = round.EventConvergence;
        if (string.IsNullOrEmpty(state.TechnicalFailure)
            || DateTime.UtcNow.Ticks < state.NextExecutionRecoveryUtcTicks) return;
        state.NextExecutionRecoveryUtcTicks = DateTime.UtcNow.AddSeconds(30).Ticks;
        if (!string.IsNullOrEmpty(round.TechnicalHoldReason)
            && round.TechnicalHoldReason != "event_ending_execution:" + state.TechnicalFailure) return;
        if (Storage.Documents.Any(x => x.RoundId == round.RoundId && x.EventExecutionPending)
            || Storage.EventEffects.Any(x => x.EventId == round.RoundId && x.Status is "applying" or "uncertain")) return;
        if (state.TechnicalFailure.StartsWith("agreement_execution_failed", StringComparison.Ordinal))
        {
            // Only the agreement owner can resolve its failed/partial receipt.
            // Observing that resolution is safe; executing its batch here is not.
            string prefix = "agreement_execution_failed:";
            var receipt = state.TechnicalFailure.StartsWith(prefix, StringComparison.Ordinal)
                ? ResolveDocument(state.TechnicalFailure.Substring(prefix.Length)) : null;
            var accepted = receipt?.Actions?.Where(x => x.Intent.StartsWith("accept_", StringComparison.Ordinal)).ToList();
            if (receipt?.RoundId == round.RoundId && accepted?.Count > 0
                && !round.PendingOffers.Any(x => x.Status is "execution_failed" or "partially_executed")
                && accepted.All(x => x.ChangedDiplomaticState && x.ExecutionState == "applied"
                    && round.PendingOffers.Any(p => p.SourceDocumentId == x.RespondingToOfferDocumentId
                        && p.SourceActionId == x.RespondingToOfferActionId && p.Status == "accepted")))
            { ClearEndingExecutionHold(round); BeginEventEnding(round, "action_executed"); }
            return;
        }
        const string warPrefix = "war_execution_failed:";
        if (!state.TechnicalFailure.StartsWith(warPrefix, StringComparison.Ordinal)) return;
        var original = ResolveDocument(state.TechnicalFailure.Substring(warPrefix.Length));
        if (original?.RoundId != round.RoundId) return;
        if (original.Actions?.Count == 1 && original.Actions[0].Intent == "declare_war"
            && original.Actions[0].ChangedDiplomaticState && original.Actions[0].ExecutionState == "applied")
        { ClearEndingExecutionHold(round); state.ActualWar = true; BeginEventEnding(round, "war_executed"); return; }
        if (!CanRecoverEndingWar(original) || state.ExecutionRecoveryAttempts >= 3
            || Storage.Jobs.Any(x => x.RoundId == round.RoundId && x.EndingRecoveryDocumentId == original.DocumentId)) return;
        if (!_host.HasIndependentAuthority(original.AuthorKingdomId) || !_host.HasIndependentAuthority(original.TargetKingdomId)
            || !_host.CanAiAuthorParty(original.AuthorKingdomId, out _)
            || _host.IsPlayerParty(original.AuthorKingdomId)
            || (!string.IsNullOrEmpty(original.AuthorRulerId) && original.AuthorRulerId != _host.PartyRulerId(original.AuthorKingdomId))
            || _host.PartiesAtWar(original.AuthorKingdomId, original.TargetKingdomId)) return;
        // Reuse the public drafting and independent audit pipeline. The original
        // artifact and its failure receipt remain immutable historical evidence.
        try
        {
            _schedulingFinalDecision = true;
            QueueEventDeclaration(round, original.AuthorKingdomId, original.TargetKingdomId, original, 100, false, false);
        }
        finally { _schedulingFinalDecision = false; }
        var job = Storage.Jobs.FirstOrDefault(x => x.RoundId == round.RoundId && x.AuthorKingdomId == original.AuthorKingdomId
            && x.SourceDocumentId == original.DocumentId && x.Kind == "generate" && !x.IsRunning);
        if (job == null) return;
        job.EndingRecoveryDocumentId = original.DocumentId;
        state.ExecutionRecoveryAttempts++;
        if (!state.RequiredJobIds.Contains(job.JobId)) state.RequiredJobIds.Add(job.JobId);
        if (!state.RequiredSources.Contains(original.DocumentId)) state.RequiredSources.Add(original.DocumentId);
        _host.Log("diplomacy.ending_execution_repair_queued event=" + round.RoundId + " source=" + original.DocumentId
            + " attempt=" + state.ExecutionRecoveryAttempts);
    }

    private bool CanRecoverEndingWar(WorldDiplomacyDocument original)
    {
        if (original?.IsReadyForPublication != true || original.IsPlayerAuthored || original.EventExecutionPending
            || original.ChangedDiplomaticState || original.Actions?.Count != 1
            || !string.IsNullOrEmpty(original.DialogueArrangementId)) return false;
        var action = original.Actions[0];
        if (action == null || action.Intent != "declare_war" || action.Commitment != "binding"
            || action.ChangedDiplomaticState || action.TargetKingdomId != original.TargetKingdomId
            || action.PeaceTerms != null || action.TreatyTerms != null) return false;
        if (action.ExecutionState is "not_attempted" or "not_applied")
            return action.DependencyActionIds == null || action.DependencyActionIds.Count == 0;
        // Old releases persisted this explicit non-execution receipt. Valid
        // conditional bargains remain deferred; only malformed graphs qualify.
        return action.ExecutionState == "deferred_conditions"
            && !DiplomacyConditionalExecutionRules.Validate(EndingRecoveryActions(original), ResolveDocument, out _);
    }

    private static JArray EndingRecoveryActions(WorldDiplomacyDocument original) => new JArray(original.Actions.Select(x =>
        new JObject { ["target_kingdom_id"] = x.TargetKingdomId, ["intent"] = x.Intent, ["commitment"] = x.Commitment,
            ["dependency_action_ids"] = new JArray(x.DependencyActionIds ?? new()),
            ["responding_to_threat_document_id"] = x.RespondingToThreatDocumentId,
            ["responding_to_threat_action_id"] = x.RespondingToThreatActionId }));

    private string BuildEndingRecoveryContext(WorldDiplomacyJob job)
    {
        if (string.IsNullOrEmpty(job.EndingRecoveryDocumentId)) return "";
        var original = ResolveDocument(job.EndingRecoveryDocumentId);
        return DiplomacyPromptTexts.Get("EndingExecutionRepair", "") + "\n" + JsonConvert.SerializeObject(new {
            source = job.EndingRecoveryDocumentId, title = original?.Title, body = original?.Body,
            actions = original?.Actions == null ? null : EndingRecoveryActions(original),
            execution_state = original?.Actions?.FirstOrDefault()?.ExecutionState });
    }

    private bool ValidateEndingRecoveryDraft(WorldDiplomacyJob job, JObject draft, out string reason)
    {
        reason = "";
        if (string.IsNullOrEmpty(job.EndingRecoveryDocumentId)) return true;
        var original = ResolveDocument(job.EndingRecoveryDocumentId);
        var round = ResolveRound(job.RoundId);
        if (!CanRecoverEndingWar(original) || original.RoundId != job.RoundId
            || round?.EventConvergence.TechnicalFailure != "war_execution_failed:" + original.DocumentId
            || original.AuthorKingdomId != job.AuthorKingdomId || original.TargetKingdomId != job.TargetKingdomId
            || (!string.IsNullOrEmpty(original.AuthorRulerId) && original.AuthorRulerId != _host.PartyRulerId(job.AuthorKingdomId)))
        { reason = "ending_recovery_receipt_or_authority_changed"; return false; }
        var rows = draft?["actions"] as JArray;
        var row = rows?.Count == 1 ? rows[0] as JObject : null;
        if ((string)draft?["title"] != original.Title || (string)draft?["body"] != original.Body || row == null
            || (string)row["intent"] != "declare_war" || (string)row["commitment"] != "binding"
            || (string)row["target_kingdom_id"] != original.TargetKingdomId
            || row["peace_terms"] is JObject || row["treaty_terms"] is JObject
            || ((string)row["responding_to_threat_document_id"] ?? "") != original.Actions[0].RespondingToThreatDocumentId
            || ((string)row["responding_to_threat_action_id"] ?? "") != original.Actions[0].RespondingToThreatActionId)
        { reason = "ending_recovery_preserve_original_text_action_target_and_binding"; return false; }
        return true;
    }
}
