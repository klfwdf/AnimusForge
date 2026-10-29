using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.DiplomacyDialogue;
using AnimusForge.Refactor.Domain;
using static AnimusForge.Refactor.Domain.WorldDiplomacyTextRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyRoundLifecycleRules;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    // Request boundary only: one bounded candidate set and one pass over each
    // existing archive. No new transport job, timer, save key or live-world scan.
    private JArray SpeechEventCandidates(string actor)
    {
        var rounds = GetLiveRounds().Where(IsLiveRound).Take(12).ToList();
        var sources = rounds.ToDictionary(x => x.RoundId, _ => new JArray(), StringComparer.Ordinal);
        var known = CollectKnownDocumentIds(null, Storage.NobleKnowledge, Storage.KingdomKnowledge, null, actor, true, true);
        for (int i = Storage.EventAnnouncements.Count - 1; i >= 0; i--)
        {
            var notice = Storage.EventAnnouncements[i];
            if (notice.State != "public" || notice.Stage != "started" || string.IsNullOrWhiteSpace(notice.Body)
                || !sources.TryGetValue(notice.EventId, out var rows) || rows.Count > 0) continue;
            rows.Add(new JObject { ["id"] = notice.Id, ["body"] = Limit(notice.Body, 1000) });
        }
        for (int i = Storage.Documents.Count - 1; i >= 0; i--)
        {
            var document = Storage.Documents[i];
            if (!document.IsReadyForPublication || string.IsNullOrWhiteSpace(document.Body)
                || (document.AuthorKingdomId != actor && !known.Contains(document.DocumentId))
                || !sources.TryGetValue(document.RoundId, out var rows) || rows.Count >= 2) continue;
            rows.Add(new JObject { ["id"] = document.DocumentId, ["body"] = Limit(document.Body, 1000) });
        }
        return new JArray(rounds.Where(x => sources[x.RoundId].Count > 0).Select(x => new JObject {
            ["event_id"] = x.RoundId, ["topic"] = Limit(x.RoundTopic, 120), ["sources"] = sources[x.RoundId] }));
    }

    private string ReviewedSpeechEvent(WorldDiplomacyJob job, DiplomacySpeechReview speech, JObject entry,
        DialogueTagPayload parsed, JObject audit, int index, JArray currentCandidates)
    {
        // Proposal identity is resolved by its exact document, never by a model's event hint.
        if (parsed.Move != DialogueDiplomaticMove.NewMatter) return "";
        if (entry["event_link"] is not JObject link || audit["event_links"] is not JArray reviews || reviews.Count > 8)
            return "";
        var matches = reviews.OfType<JObject>().Where(x => x["commitment_index"]?.Type == JTokenType.Integer
            && x["commitment_index"].ToString() == index.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToList();
        if (matches.Count != 1 || matches[0]["related"]?.Type != JTokenType.Boolean || !(bool)matches[0]["related"])
            return "";
        string Text(JObject value, string key) => value[key]?.Type == JTokenType.String ? (string)value[key] : "";
        string eventId = Text(link, "event_id"), sourceId = Text(link, "source_id"), quote = Text(link, "source_quote"),
            speechQuote = Text(link, "dialogue_quote"), reason = Text(link, "reason");
        if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(sourceId) || Text(matches[0], "event_id") != eventId
            || string.IsNullOrWhiteSpace(Text(matches[0], "reason")) || Text(matches[0], "reason").Length > 1200
            || string.IsNullOrWhiteSpace(reason) || reason.Length > 1200
            || string.IsNullOrWhiteSpace(quote) || quote.Length < 8 || quote.Length > 1000
            || string.IsNullOrWhiteSpace(speechQuote) || speechQuote.Length < 8 || speechQuote.Length > 1000
            || (speech.PlayerText.IndexOf(speechQuote, StringComparison.Ordinal) < 0
                && speech.NpcText.IndexOf(speechQuote, StringComparison.Ordinal) < 0)) return "";
        // A supplied ID must belong to the actual audit request and still be usable
        // at completion. Missing/old/changed evidence selects a new event immediately.
        var request = WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(job.UserPrompt);
        JObject Find(JArray candidates) => candidates?.OfType<JObject>().FirstOrDefault(x => Text(x, "event_id") == eventId);
        JObject Source(JObject candidate) => (candidate?["sources"] as JArray)?.OfType<JObject>()
            .FirstOrDefault(x => Text(x, "id") == sourceId);
        var sent = Source(Find(request?["event_candidates"] as JArray));
        var current = Source(Find(currentCandidates));
        if (sent == null || current == null || !JToken.DeepEquals(sent, current)
            || Text(sent, "body").IndexOf(quote, StringComparison.Ordinal) < 0) return "";
        return eventId;
    }
}
