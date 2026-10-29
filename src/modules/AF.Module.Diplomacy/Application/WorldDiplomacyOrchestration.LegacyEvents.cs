using System;
using System.Linq;
using AnimusForge.DiplomacyDialogue;
using static AnimusForge.Refactor.Domain.WorldDiplomacyStructureRules;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    private void PreserveLegacyJobsBeforeNormalization()
    {
        foreach (var round in GetLiveRounds().Where(x => x.EventSystemVersion == 0)) PreserveLegacyRoundJobs(round);
    }

    private void PreserveLegacyRoundJobs(WorldDiplomacyRound round)
    {
        RequestLeases.InvalidateRound(round.RoundId);
        foreach (var job in Storage.Jobs.Where(x => x != null && (x.RoundId == round.RoundId || x.ExchangeId == round.RoundId)
            && x.Kind != "compress").ToList())
        {
            job.IsRunning = false;
            Storage.LegacyPreservedJobs.Add(job);
            Storage.Jobs.Remove(job);
        }
    }

    // Once per legacy event at load. Preserved jobs remain save evidence only;
    // they are never included in dispatch or model history.
    private void MigrateLegacyEvent(WorldDiplomacyRound round)
    {
        var publicDocuments = Storage.Documents.Where(x => x?.RoundId == round.RoundId && x.IsReadyForPublication).ToList();
        var obligations = Storage.DialogueArrangements.Where(x => x.RoundId == round.RoundId && DiplomacyCommitmentRules.IsPending(x)).ToList();
        bool acceptedAid = Storage.AidRequests?.Any(x => x.RoundId == round.RoundId && x.Status == "accepted") == true;
        InitializeEventOwner(round, round.IsPlayerInsertion || round.NpcInitiatedPlayerEvent
            || round.EventSourceType is "player" or "npc_player_agenda");
        round.SchemaVersion = _host.RelaySchemaVersion();
        round.OwnershipEpoch++; round.ConversationRevision++;
        PreserveLegacyRoundJobs(round);
        foreach (var item in obligations)
        {
            var old = ResolveDocument(item.DocumentId);
            if (old == null || old.IsReadyForPublication || item.Status == "execution_uncertain") continue;
            if (old.ChangedDiplomaticState || old.Actions?.Any(x => x.ChangedDiplomaticState) == true)
            { item.Status = "execution_uncertain"; item.Reason = "legacy_unpublished_execution_evidence"; continue; }
            // Keep the original draft and association; only the accepted terms
            // may seed a new declaration, never its unvalidated old prose/actions.
            old.EventExecutionPending = false; old.AnalysisStatus = "legacy_preserved_draft";
            old.DialogueArrangementId = item.ArrangementId; old.DialogueArrangementVersion = item.Version;
            item.DocumentId = "";
            item.GenerationRetryAfterDay = Math.Min(item.GenerationRetryAfterDay, _host.CurrentDay());
        }
        if (publicDocuments.Count == 0 && obligations.Count == 0 && !acceptedAid)
        {
            round.State = "legacy_preserved"; round.CloseReason = "legacy_unverified_work_preserved";
            round.TechnicalHoldReason = "legacy_unverified_work_preserved";
            if (ReferenceEquals(Storage.ActiveRound, round)) Storage.ActiveRound = null;
            Storage.ConcurrentRounds.Remove(round);
            if (!Storage.CompletedRounds.Contains(round)) Storage.CompletedRounds.Add(round);
            InvalidateDialogueIndex();
            _host.Log("diplomacy.legacy_preserved round=" + round.RoundId);
            return;
        }
        if (publicDocuments.Count > 0)
        {
            if (ResolveDocument(round.RootDocumentId)?.IsReadyForPublication != true)
                round.RootDocumentId = publicDocuments.OrderBy(x => x.Day).ThenBy(x => x.CreatedUtcTicks).First().DocumentId;
            round.EventOpeningOrigin = "legacy_public_event";
            foreach (var document in publicDocuments.GroupBy(x => x.AuthorKingdomId)
                .Select(x => x.OrderByDescending(d => d.Day).ThenByDescending(d => d.CreatedUtcTicks).First()))
            {
                if (document.IsPlayerAuthored || document.PlayerLifecycleClassified) continue;
                var job = new WorldDiplomacyJob { JobId = _host.NewId("legacy_lifecycle_check"), Kind = "player_lifecycle",
                    RoundId = round.RoundId, ExchangeId = round.RoundId, DocumentId = document.DocumentId,
                    AuthorKingdomId = document.AuthorKingdomId, TargetKingdomId = document.TargetKingdomId,
                    ForcedIntent = "recover_published_lifecycle", Priority = 100, CreatedDay = _host.CurrentDay() };
                EnqueueJob(job);
            }
        }
        foreach (var work in round.PlayerCourtWork.Where(x => x.Status == "queued")) work.Status = "pending";
        // A former reply deadline cannot retroactively penalize a restored save.
        foreach (var wait in round.PlayerNegotiationWaits.Where(x => string.IsNullOrEmpty(x.AnswerDocumentId)))
        {
            wait.NotifiedHour = -1;
            var source = ResolveDocument(wait.SourceDocumentId);
            if (source != null) { source.LegacyNoticeSuppressed = true; source.FormalNoticeShown = true; }
            wait.RequiresAnswer = false;
        }
        InvalidateDialogueIndex();
        _host.Log("diplomacy.legacy_recovered round=" + round.RoundId + " published=" + publicDocuments.Count + " obligations=" + obligations.Count);
    }
}
