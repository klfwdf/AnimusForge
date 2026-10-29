using AnimusForge;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        if (ResolveRound(document.RoundId)?.EventSystemVersion > 0) return;
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
        if (ResolveRound(document?.RoundId)?.EventSystemVersion > 0) return;
        if (ruler == null || DialogueHost?.IsPlayerRuler(ruler) == true || DialogueHost?.RulerAlive(ruler) != true || document?.IsReadyForPublication != true) return;
        int memoryDay = author ? document.Day : (receivedDay >= 0 ? receivedDay : _host.CurrentDay());
        document.PendingPersonalMemoryRulers ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (document.PendingPersonalMemoryRulers.TryGetValue(ruler, out int originalDay)) memoryDay = originalDay;
        bool committed = true;
        if (!string.IsNullOrWhiteSpace(document.Body))
        {
            string template = author ? DiplomacyMiscPromptTexts.Get("PersonalMemory_DocumentPublished", "I have officially published a diplomatic document, source={0}, date={1}, publishing kingdom={2}, title={3}. Original text: {4}. This entry only confirms the document was published or received; it does not prove the proposal was accepted or diplomatic actions executed.") : DiplomacyMiscPromptTexts.Get("PersonalMemory_DocumentReceived", "The court where I reside has officially received a diplomatic document, source={0}, date={1}, publishing kingdom={2}, title={3}. Original text: {4}. This entry only confirms the document was published or received; it does not prove the proposal was accepted or diplomatic actions executed.");
            string displayDate = template.StartsWith("I have officially", StringComparison.Ordinal)
                || template.StartsWith("The court where I reside", StringComparison.Ordinal)
                ? "game day " + document.Day.ToString(CultureInfo.InvariantCulture)
                : document.GameDate ?? "";
            string fact = string.Format(template, document.DocumentId ?? "", displayDate, document.AuthorKingdomName ?? "", document.Title ?? "", Limit(document.Body, 4000));
            committed &= CommitDiplomaticDocumentMemoryOnce(ruler, document, author ? "publication" : "receipt", fact, memoryDay);
        }
        if (document.Actions?.Count > 0)
        {
            foreach (WorldDiplomacyDocumentAction action in document.Actions)
                if (action != null && action.ChangedDiplomaticState && !string.IsNullOrWhiteSpace(action.MechanicalResult))
                    committed &= CommitDiplomaticDocumentMemoryOnce(ruler, document,
                        "action:" + action.ActionId + ":result",
                        string.Format(DiplomacyMiscPromptTexts.Get("PersonalMemory_MechanicResultWithTarget", "Diplomatic result confirmed by game mechanics, source={0}, action={1}, target={2}: {3}"), document.DocumentId, action.ActionId, action.TargetKingdomId, action.MechanicalResult),
                        memoryDay);
        }
        else if (document.ChangedDiplomaticState && !string.IsNullOrWhiteSpace(document.MechanicalResult))
            committed &= CommitDiplomaticDocumentMemoryOnce(ruler, document, "result",
                string.Format(DiplomacyMiscPromptTexts.Get("PersonalMemory_MechanicResult", "Diplomatic result confirmed by game mechanics, source={0}, action={1}: {2}"), document.DocumentId, "result", document.MechanicalResult),
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
