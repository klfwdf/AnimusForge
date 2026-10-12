using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Deterministic transitions over the existing canonical records. The game host
// resolves live countries and executes effects; this owner handles response
// identity, coalescing, coverage and the shared-event request tail.
public static class DiplomacyRoundWorkRules
{
    public static List<string> PendingSources(WorldDiplomacyRound round, string author, int currentDay = int.MaxValue) =>
        (round?.PlayerResponses ?? new List<WorldDiplomacyPlayerResponse>())
            .Where(x => x != null && x.KingdomId == author && x.Status == "pending" && x.RetryNotBeforeDay <= currentDay && string.IsNullOrWhiteSpace(x.AnswerDocumentId))
            .Select(x => x.SourceDocumentId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    public static void MergeQueuedSpeaker(WorldDiplomacyJob job, IEnumerable<string> sources)
    {
        // A semantic repair's source batch must remain identical to its frozen
        // messages. Later arrivals keep their pending obligations for the next job.
        if (job == null || job.IsRunning) return;
        if (job.SemanticRepairAttempts > 0) return;
        job.PlayerResponseSourceIds = sources.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (job.PlayerResponseSourceIds.Count > 0) job.Priority = Math.Max(job.Priority, 95);
    }

    public static void SealCoverage(WorldDiplomacyRound round, WorldDiplomacyDocument document)
    {
        if (round == null || document == null || !document.IsReadyForPublication || document.IsPlayerAuthored) return;
        foreach (var receipt in round.PlayerResponses ?? Enumerable.Empty<WorldDiplomacyPlayerResponse>())
            if (receipt.Status == "pending" && receipt.KingdomId == document.AuthorKingdomId
                && (document.AnsweredPlayerDocumentIds ?? new List<string>()).Contains(receipt.SourceDocumentId, StringComparer.OrdinalIgnoreCase))
            { receipt.AnswerDocumentId = document.DocumentId; receipt.Status = "answered"; }
    }

    public static List<string> SelectResponseBatch(WorldDiplomacyRound round, string author, IEnumerable<WorldDiplomacyDocument> documents, int currentDay = int.MaxValue)
    {
        var index = documents.Where(x => x != null).GroupBy(x => x.DocumentId)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var selected = new List<string>();
        int characters = 0;
        foreach (string id in PendingSources(round, author, currentDay))
        {
            if (!index.TryGetValue(id, out var document)) continue;
            int cost = (document.Body ?? "").Length + id.Length + 100;
            if (selected.Count > 0 && (characters + cost > 18000 || selected.Count >= 6)) break;
            selected.Add(id); characters += cost;
        }
        return selected; // The remaining sources stay pending for the next batch.
    }

    public static string BuildRequestTail(WorldDiplomacyRound round, IEnumerable<WorldDiplomacyDocument> documents, IReadOnlyList<string> sources, Func<string, string, string> localize = null)
    {
        if (round == null) return "";
        var material = documents.Where(x => x != null && x.IsReadyForPublication).ToList();
        var tail = new StringBuilder();
        tail.AppendLine(WorldDiplomacyPromptContractRules.ContractText(localize, "HistoryPrompt_006", "\n[Latest public context for this shared negotiation]"));
        tail.AppendLine(WorldDiplomacyPromptContractRules.ContractText(localize, "HistoryPrompt_007", "Event=") + round.RoundId + WorldDiplomacyPromptContractRules.ContractText(localize, "HistoryPrompt_008", "; this is the same diplomatic negotiation, and subsequent kingdoms must continue from the latest discussion. The declarations below are public statements only; actual effect depends on existing mechanical results."));
        if (!string.IsNullOrWhiteSpace(round.ExternalOpeningContext)) tail.AppendLine(round.ExternalOpeningContext);
        foreach (var document in material.Where(x => x.RoundId == round.RoundId).OrderByDescending(x => x.CreatedUtcTicks).Take(12).Reverse())
            tail.AppendLine(document.DocumentId + " | " + document.AuthorKingdomName + " | " + document.Title + "\n"
                + ((document.Body ?? "").Length > 2500 ? document.Body.Substring(0, 2500) : document.Body));
        if (sources.Count > 0)
        {
            tail.AppendLine(WorldDiplomacyPromptContractRules.ContractText(localize, "HistoryPrompt_009", "[Player declarations that require substantive responses this time]"));
            var index = material.GroupBy(x => x.DocumentId).ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
            foreach (string id in sources)
                if (index.TryGetValue(id, out var source)) tail.AppendLine(WorldDiplomacyPromptContractRules.ContractText(localize, "HistoryPrompt_010", "Source=") + id + "\n" + source.Body);
            tail.AppendLine(WorldDiplomacyPromptContractRules.ContractText(localize, "HistoryPrompt_011", "Address the actual requests in every statement above in this declaration’s body: agree, reject, assess or explain inability to commit. Continue the current negotiation without opening side branches. Add an answered_player_document_ids array to JSON listing only the source IDs above that the body has substantively answered; do not include declarations merely read without response. All other actions, terms and output fields follow existing rules."));
        }
        return tail.ToString();
    }
}
