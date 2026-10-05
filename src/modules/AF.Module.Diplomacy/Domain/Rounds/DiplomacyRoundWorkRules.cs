using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnimusForge;

// Deterministic transitions over the existing canonical records. The game host
// resolves live countries and executes effects; this owner handles response
// identity, coalescing, coverage and the shared-event request tail.
public static class DiplomacyRoundWorkRules
{
    public static List<string> PendingSources(WorldDiplomacyRound round, string author) =>
        (round?.PlayerResponses ?? new List<WorldDiplomacyPlayerResponse>())
            .Where(x => x != null && x.KingdomId == author && x.Status == "pending" && string.IsNullOrWhiteSpace(x.AnswerDocumentId))
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

    public static List<string> SelectResponseBatch(WorldDiplomacyRound round, string author, IEnumerable<WorldDiplomacyDocument> documents)
    {
        var index = documents.Where(x => x != null).GroupBy(x => x.DocumentId)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var selected = new List<string>();
        int characters = 0;
        foreach (string id in PendingSources(round, author))
        {
            if (!index.TryGetValue(id, out var document)) continue;
            int cost = (document.Body ?? "").Length + id.Length + 100;
            if (selected.Count > 0 && (characters + cost > 18000 || selected.Count >= 6)) break;
            selected.Add(id); characters += cost;
        }
        return selected; // The remaining sources stay pending for the next batch.
    }

    public static string BuildRequestTail(WorldDiplomacyRound round, IEnumerable<WorldDiplomacyDocument> documents, IReadOnlyList<string> sources)
    {
        if (round == null) return "";
        var material = documents.Where(x => x != null && x.IsReadyForPublication).ToList();
        var tail = new StringBuilder();
        tail.AppendLine("\n【本次共同交涉的最新公开上下文】");
        tail.AppendLine("事件=" + round.RoundId + "；这是同一场外交交涉，后续国家必须承接最新讨论。以下宣言只是公开表达，是否实际生效以既有机制结果为准。");
        if (!string.IsNullOrWhiteSpace(round.ExternalOpeningContext)) tail.AppendLine(round.ExternalOpeningContext);
        foreach (var document in material.Where(x => x.RoundId == round.RoundId).OrderByDescending(x => x.CreatedUtcTicks).Take(12).Reverse())
            tail.AppendLine(document.DocumentId + " | " + document.AuthorKingdomName + " | " + document.Title + "\n"
                + ((document.Body ?? "").Length > 2500 ? document.Body.Substring(0, 2500) : document.Body));
        if (sources.Count > 0)
        {
            tail.AppendLine("【本次必须实质回应的玩家宣言】");
            var index = material.GroupBy(x => x.DocumentId).ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
            foreach (string id in sources)
                if (index.TryGetValue(id, out var source)) tail.AppendLine("来源=" + id + "\n" + source.Body);
            tail.AppendLine("在本次公文正文中处理以上每份发言的实际诉求，可以同意、拒绝、评价或说明无法承诺；继续当前谈判，不另开旁支。JSON另加 answered_player_document_ids 数组，列出正文已实质回应的上述来源ID；不能把只看过、未回应的宣言列入。其余动作、条款和输出字段遵守原有规则。");
        }
        return tail.ToString();
    }
}
