using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Completion of a round-compression job owns the replacement of its archive record.
// The host supplies only the campaign clock and date formatter at the main-thread boundary.
internal static class WorldDiplomacyRoundCompressionApplication
{
    internal static void Commit(WorldDiplomacyStorage storage, WorldDiplomacyJob job, string raw,
        int currentDay, Func<int, string> formatCampaignDate)
    {
        JObject json = WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(raw);
        WorldDiplomacyRoundSummary summary = new WorldDiplomacyRoundSummary
        {
            RoundId = job.RoundId ?? "", CreatedDay = currentDay,
            Summary = WorldDiplomacyTextRules.NormalizeBody(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "summary")),
            SourceDocumentIds = job.CompressionDocumentIds ?? new List<string>()
        };
        if (string.IsNullOrWhiteSpace(summary.Summary))
            summary.Summary = WorldDiplomacyDocumentFactRules.BuildFallbackRoundSummary(
                storage.Documents, job.CompressionDocumentIds, formatCampaignDate);
        if (json["facts"] is JArray facts)
        {
            foreach (JToken token in facts.Take(32))
            {
                summary.Facts.Add(new WorldDiplomacyRoundFact
                {
                    Text = WorldDiplomacyTextRules.Limit(token?["text"]?.ToString(), 360),
                    SourceDocumentIds = WorldDiplomacyEnvelopeJsonRules.ReadTokenStringList(token?["source_document_ids"]),
                    KingdomIds = WorldDiplomacyEnvelopeJsonRules.ReadTokenStringList(token?["kingdom_ids"])
                });
            }
        }
        storage.RoundSummaries.RemoveAll(x => x != null
            && WorldDiplomacyRoundLifecycleRules.IsRecordInRound(x.RoundId, summary.RoundId));
        storage.RoundSummaries.Add(summary);
    }
}
