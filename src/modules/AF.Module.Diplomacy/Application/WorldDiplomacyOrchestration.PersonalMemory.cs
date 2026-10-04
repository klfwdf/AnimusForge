using System;
using System.Collections.Generic;
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


    private readonly Queue<string> _personalMemoryRetryDocuments = new Queue<string>();
    private readonly HashSet<string> _personalMemoryRetryDocumentSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    // Publication/result boundaries only. Durable memory recovery deduplicates
    // each source and recovers memory writes without replaying diplomacy effects.
    private void RecordDiplomaticDocumentPersonalMemories(WorldDiplomacyDocument document)
    {
        string author = document.AuthorRulerId;
        if (DialogueHost?.RulerAlive(author) == true)
            RecordDiplomaticDocumentPersonalMemory(author, document, true);

        foreach (WorldDiplomacyKingdomKnowledge knowledge in Storage.KingdomKnowledge)
        {
            if (knowledge == null || knowledge.KingdomId == document.AuthorKingdomId
                || knowledge.DocumentIds == null || !knowledge.DocumentIds.Contains(document.DocumentId)) continue;
            string receiver = _host.PartyRulerId(knowledge.KingdomId);
            if (DialogueHost?.RulerAlive(receiver) == true)
                RecordDiplomaticDocumentPersonalMemory(receiver, document, false);
        }
    }

    private void RecordDiplomaticDocumentPersonalMemory(string ruler, WorldDiplomacyDocument document, bool author, int receivedDay = -1)
    {
        if (ruler == null || DialogueHost?.IsPlayerRuler(ruler) == true || DialogueHost?.RulerAlive(ruler) != true || document?.IsReadyForPublication != true) return;
        int memoryDay = author ? document.Day : (receivedDay >= 0 ? receivedDay : _host.CurrentDay());
        document.PendingPersonalMemoryRulers ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (document.PendingPersonalMemoryRulers.TryGetValue(ruler, out int originalDay)) memoryDay = originalDay;
        bool committed = true;
        if (!string.IsNullOrWhiteSpace(document.Body))
        {
            string fact = (author ? "本人已正式发布" : "本人所在王庭已正式收到")
                + "外交公文，来源=" + document.DocumentId + "，日期=" + document.GameDate
                + "，发文国=" + document.AuthorKingdomName + "，标题=" + document.Title
                + "。原文：" + Limit(document.Body, 4000) + "。此项只确认公文已发布或收到，不证明提案已经被接受或外交动作已经执行。";
            committed &= CommitDiplomaticDocumentMemoryOnce(ruler, document, author ? "publication" : "receipt", fact, memoryDay);
        }
        if (document.Actions?.Count > 0)
        {
            foreach (WorldDiplomacyDocumentAction action in document.Actions)
                if (action != null && action.ChangedDiplomaticState && !string.IsNullOrWhiteSpace(action.MechanicalResult))
                    committed &= CommitDiplomaticDocumentMemoryOnce(ruler, document,
                        "action:" + action.ActionId + ":result",
                        "经游戏机制确认的外交结果，来源=" + document.DocumentId + "，动作=" + action.ActionId
                        + "，对象=" + action.TargetKingdomId + "。" + action.MechanicalResult,
                        memoryDay);
        }
        else if (document.ChangedDiplomaticState && !string.IsNullOrWhiteSpace(document.MechanicalResult))
            committed &= CommitDiplomaticDocumentMemoryOnce(ruler, document, "result",
                "经游戏机制确认的外交结果，来源=" + document.DocumentId + "。" + document.MechanicalResult,
                memoryDay);
        if (committed) document.PendingPersonalMemoryRulers.Remove(ruler);
        else
        {
            document.PendingPersonalMemoryRulers[ruler] = memoryDay;
            EnqueuePersonalMemoryRetry(document.DocumentId);
        }
    }

    private bool CommitDiplomaticDocumentMemoryOnce(string ruler, WorldDiplomacyDocument document,
        string phase, string fact, int day)
    {
        document.PersonalMemoryReceipts ??= new List<string>();
        string receipt = ruler + ":" + phase;
        if (document.PersonalMemoryReceipts.Contains(receipt)) return true;
        MemoryCommitResult result = DialogueHost?.CommitFact(ruler,
            document.DocumentId + ":" + phase, fact, day, document.OriginSettlementId);
        // Durable pending seeds are retried by the memory owner, never by replaying effects.
        if (result?.Status == MemoryCommitStatus.Applied || result?.Status == MemoryCommitStatus.Duplicate
            || result?.ErrorCode == "memory_recovery_pending")
        {
            document.PersonalMemoryReceipts.Add(receipt);
            return true;
        }
        _host.Log("diplomacy_memory_not_committed source=" + document.DocumentId + " phase=" + phase
            + " ruler=" + ruler + " reason=" + result?.ErrorCode);
        return false;
    }

    private void EnqueuePersonalMemoryRetry(string documentId)
    {
        if (!string.IsNullOrWhiteSpace(documentId) && _personalMemoryRetryDocumentSet.Add(documentId))
            _personalMemoryRetryDocuments.Enqueue(documentId);
    }

    private void RetryPendingDiplomaticPersonalMemories()
    {
        // Existing daily maintenance, maximum four queued documents; no frame polling.
        int count = Math.Min(4, _personalMemoryRetryDocuments.Count);
        for (int i = 0; i < count; i++)
        {
            string id = _personalMemoryRetryDocuments.Dequeue();
            _personalMemoryRetryDocumentSet.Remove(id);
            WorldDiplomacyDocument document = ResolveDocument(id);
            if (document?.PendingPersonalMemoryRulers == null) continue;
            foreach (KeyValuePair<string, int> pending in new List<KeyValuePair<string, int>>(document.PendingPersonalMemoryRulers))
            {
                string ruler = pending.Key;
                if (ruler == null || DialogueHost?.RulerAlive(ruler) != true || DialogueHost?.IsPlayerRuler(ruler) == true)
                {
                    document.PendingPersonalMemoryRulers.Remove(pending.Key);
                    continue;
                }
                RecordDiplomaticDocumentPersonalMemory(ruler, document,
                    string.Equals(ruler, document.AuthorRulerId, StringComparison.OrdinalIgnoreCase), pending.Value);
            }
        }
    }
}
