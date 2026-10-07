using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Persistence;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    private void RecoverPlayerDocumentRoutingAndRetireClosedJobs()
    {
        // Old saves may contain pending player analysis in a completed/missing
        // round. Move the same public artifact before retiring obsolete work.
        foreach (var document in Storage.Documents.Where(x => x?.IsPlayerAuthored == true && x.IsReadyForPublication
            && x.AnalysisStatus == "pending_analysis" && !x.PlayerAnalysisCommitted).ToList())
        {
            EnsurePlayerDocumentRound(document);
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
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (Storage.ActiveRound != null) ids.Add(Storage.ActiveRound.RoundId);
        Storage.ConcurrentRounds = Storage.ConcurrentRounds.Where(x => x != null
            && !string.IsNullOrWhiteSpace(x.RoundId) && ids.Add(x.RoundId)).ToList();
        Storage.DialogueArrangements = Storage.DialogueArrangements.Where(x => x != null
            && !string.IsNullOrWhiteSpace(x.ArrangementId)).GroupBy(x => x.ArrangementId, StringComparer.Ordinal)
            .Select(x => x.OrderByDescending(y => y.Version).First()).ToList();
        foreach (var round in GetLiveRounds())
        {
            round.PlayerResponses = (round.PlayerResponses ?? new List<WorldDiplomacyPlayerResponse>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.SourceDocumentId) && !string.IsNullOrWhiteSpace(x.KingdomId))
                .GroupBy(x => x.SourceDocumentId + "|" + x.KingdomId, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.OrderByDescending(y => !string.IsNullOrWhiteSpace(y.AnswerDocumentId)).First()).ToList();
            foreach (var item in round.PlayerResponses)
                if (string.IsNullOrWhiteSpace(item.OriginalRoundId)) item.OriginalRoundId = round.RoundId;
        }
        foreach (var doc in Storage.Documents.Where(x => x != null))
        {
            doc.AnsweredPlayerDocumentIds ??= new List<string>();
            doc.PersonalMemoryReceipts ??= new List<string>();
            doc.PendingPersonalMemoryRulers ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
        InvalidateDialogueIndex();
        _diplomacyWorkNeedsReconcile = true;
    }

}
