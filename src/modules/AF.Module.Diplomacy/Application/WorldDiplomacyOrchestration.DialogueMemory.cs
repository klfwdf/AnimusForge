using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Contracts;

using AnimusForge.Refactor.Domain;
using static AnimusForge.Refactor.Domain.WorldDiplomacyIntentVocabulary;
using static AnimusForge.Refactor.Domain.WorldDiplomacyTextRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyRoundLifecycleRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyStructureRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyEnvelopeJsonRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyDocumentFactRules;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    private readonly Queue<string> _dialogueMemoryRetry = new Queue<string>();
    private readonly HashSet<string> _dialogueMemoryRetrySet = new HashSet<string>(StringComparer.Ordinal);

    private void RecordDialogueArrangementFact(WorldDiplomacyDialogueArrangement item, string phase, string fact)
    {
        item.MemoryReceipts ??= new List<WorldDiplomacyDialogueMemoryReceipt>();
        string source = item.ArrangementId + ":v" + item.Version + ":" + phase;
        var receipt = item.MemoryReceipts.FirstOrDefault(x => x.SourceId == source);
        if (receipt == null)
        {
            string npcName = "统治者";
            npcName = DialogueHost?.RulerName(item.RulerId) ?? npcName;
            receipt = new WorldDiplomacyDialogueMemoryReceipt { SourceId = source, RulerId = item.RulerId,
                Fact = fact, Day = _host.CurrentDay(), Hour = _host.CurrentHour(),
                GameDate = _host.FormatCampaignDate(_host.CurrentDay()), NpcName = npcName, LocationId = "口头外交",
                SourceInteractionId = item.SourceInteractionId,
                SourceChannel = item.SourceChannel,
                SourceSessionId = item.SourceSessionId };
            item.MemoryReceipts.Add(receipt);
        }
        // Persist the immutable seed first; retries always submit this exact fact
        // and original day, even when the arrangement's status/reason has changed.
        TryDeliverDialogueMemoryReceipt(receipt);
        if (!receipt.Delivered) EnqueueDialogueMemoryRetry(item.ArrangementId);
    }

    private void TryDeliverDialogueMemoryReceipt(WorldDiplomacyDialogueMemoryReceipt receipt)
    {
        if (receipt.Delivered) return;
        try
        {
            var result = DialogueHost?.CommitFact(receipt.RulerId,
                receipt.SourceId, receipt.Fact, receipt.Day, receipt.LocationId, receipt.Hour, receipt.NpcName, receipt.GameDate);
            AcknowledgeDialogueMemoryReceipt(receipt, result);
        }
        catch (Exception ex) { receipt.LastError = Limit(ex.Message, 180); }
    }

    private static void AcknowledgeDialogueMemoryReceipt(WorldDiplomacyDialogueMemoryReceipt receipt, MemoryCommitResult result)
    {
        if (receipt.Delivered) return;
        // Pending means the existing AF memory owner accepted a durable seed;
        // it owns further recovery. Rejected/unavailable seeds remain here.
        receipt.Delivered = result != null && (result.Status == MemoryCommitStatus.Applied
            || result.Status == MemoryCommitStatus.Duplicate || result.ErrorCode == "memory_recovery_pending");
        receipt.LastError = receipt.Delivered ? "" : result?.ErrorCode ?? "memory_owner_unavailable";
    }

    private void EnqueueDialogueMemoryRetry(string arrangementId)
    { if (_dialogueMemoryRetrySet.Add(arrangementId)) _dialogueMemoryRetry.Enqueue(arrangementId); }

    private void RestoreDialogueMemoryRetryQueue()
    {
        _dialogueMemoryRetry.Clear(); _dialogueMemoryRetrySet.Clear();
        foreach (var item in Storage.DialogueArrangements ?? Enumerable.Empty<WorldDiplomacyDialogueArrangement>())
            if (item?.MemoryReceipts?.Any(x => !x.Delivered) == true) EnqueueDialogueMemoryRetry(item.ArrangementId);
    }

    private void RetryPendingDialoguePersonalMemories()
    {
        // Daily only, at most four arrangements/eight facts. No action callback,
        // no declaration publication, no frame polling or world-history scan.
        int count = Math.Min(4, _dialogueMemoryRetry.Count), budget = 8;
        for (int i = 0; i < count && budget > 0; i++)
        {
            string id = _dialogueMemoryRetry.Dequeue(); _dialogueMemoryRetrySet.Remove(id);
            var item = Storage.DialogueArrangements?.FirstOrDefault(x => x.ArrangementId == id);
            if (item?.MemoryReceipts == null) continue;
            foreach (var receipt in item.MemoryReceipts.Where(x => !x.Delivered))
            {
                if (budget-- <= 0) break;
                TryDeliverDialogueMemoryReceipt(receipt);
            }
            if (item.MemoryReceipts.Any(x => !x.Delivered)) EnqueueDialogueMemoryRetry(id);
        }
    }
}
