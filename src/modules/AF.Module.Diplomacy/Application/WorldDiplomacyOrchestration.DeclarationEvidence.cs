using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    private JObject ResponseContinuityEvidence(WorldDiplomacyJob job)
    {
        var source = ResolveDocument(job.SourceDocumentId);
        if (source?.IsReadyForPublication != true || source.RoundId != job.RoundId
            || source.AuthorKingdomId == job.AuthorKingdomId || string.IsNullOrWhiteSpace(source.Body)
            || !DialogueDocumentKnown(job.AuthorKingdomId, source.DocumentId)) return null;
        string body = (string)WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(job.PrivacyDraft)?["body"] ?? "";
        return new JObject { ["source_id"] = source.DocumentId, ["author"] = source.AuthorKingdomId,
            ["source_body"] = source.Body, ["digest"] = MaterialHash(source.DocumentId + "|" + source.Body + "|" + body) };
    }

    private bool ValidateResponseContinuity(WorldDiplomacyJob job, JObject result, out string reason, out bool rejected)
    {
        reason = "response_continuity_review_missing_or_invalid"; rejected = false;
        if (job.ResponseContinuityReviewVersion < 1) return true;
        var evidence = ResponseContinuityEvidence(job);
        if (evidence == null) return true;
        var review = result["response_continuity"] as JObject;
        if (review?["digest"]?.Type != JTokenType.String || review["source_id"]?.Type != JTokenType.String
            || (string)review["digest"] != (string)evidence["digest"]
            || (string)review?["source_id"] != (string)evidence["source_id"]
            || review?["consistent"]?.Type != JTokenType.Boolean || !ReviewText(review, "reason")) return false;
        if (!(bool)review["consistent"])
        { rejected = true; reason = "response_disregards_prior_position:" + (string)review["reason"]; return false; }
        string body = (string)WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(job.PrivacyDraft)?["body"] ?? "";
        return review["stance"]?.Type == JTokenType.String && (string)review["stance"] is "accepted" or "disputed" or "acknowledged"
            && ReviewText(review, "source_quote") && ((string)evidence["source_body"]).Contains((string)review["source_quote"])
            && ReviewText(review, "draft_quote") && body.Contains((string)review["draft_quote"]);
    }

    private bool RequiresDeclarationAgenda(WorldDiplomacyJob job, WorldDiplomacyRound round) =>
        WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(job.PrivacyDraft)?["position_correction"] is JObject
        || round?.Participants.Any(x => x.KingdomId == job.AuthorKingdomId && x.State == "active"
            && x.AgendaAddressTo.Count > 0 && string.IsNullOrEmpty(x.AgendaExpressionDocumentId)
            && (!job.IsExternalResponseOnly || IsPlayerDeclarationOpening(round))) == true;

    private bool IsDeclarationAnswerSource(WorldDiplomacyRound round, WorldDiplomacyJob job, WorldDiplomacyDocument doc) =>
        round != null && doc?.IsReadyForPublication == true && doc.RoundId == round.RoundId
        && doc.AuthorKingdomId != job.AuthorKingdomId && DialogueDocumentKnown(job.AuthorKingdomId, doc.DocumentId)
        && (round.Participants.Any(x => x.KingdomId != job.AuthorKingdomId
                && x.AgendaExpressionDocumentId == doc.DocumentId && x.AgendaAddressTo.Contains(job.AuthorKingdomId))
            // Human public words are answerable evidence without fabricating an
            // internal human position. Bind only actual obligations on this case.
            || round.EventOpeningOrigin == "player_declaration_v2" && doc.IsPlayerAuthored && round.PlayerResponses.Any(x => x.SourceDocumentId == doc.DocumentId
                && x.KingdomId == job.AuthorKingdomId && x.Status == "pending"));

    // Request/completion boundaries only. Offsets retain punctuation and whitespace;
    // the reviewer selects evidence, while the program copies the exact draft bytes.
    private static List<string> DeclarationBodySegments(string body)
    {
        var result = new List<string>();
        int start = 0;
        for (int i = 0; i < body.Length; i++)
            if ("。！？!?\n;；".IndexOf(body[i]) >= 0 || body[i] == '.' && (i + 1 == body.Length || char.IsWhiteSpace(body[i + 1])))
            { result.Add(body.Substring(start, i + 1 - start)); start = i + 1; }
        if (start < body.Length) result.Add(body.Substring(start));
        return result;
    }

    private JObject BuildDeclarationAuditEvidence(WorldDiplomacyJob job)
    {
        var round = ResolveRound(job.RoundId);
        string body = (string)WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(job.PrivacyDraft)?["body"] ?? "";
        var sources = new JArray();
        var ids = (round?.Participants ?? new()).Select(x => x.AgendaExpressionDocumentId)
            .Concat((round?.PlayerResponses ?? new()).Where(x => x.KingdomId == job.AuthorKingdomId && x.Status == "pending")
                .Select(x => x.SourceDocumentId)).Where(x => !string.IsNullOrEmpty(x)).Distinct();
        foreach (string id in ids)
        {
            var doc = ResolveDocument(id);
            if (!IsDeclarationAnswerSource(round, job, doc)) continue;
            sources.Add(new JObject { ["source"] = doc.DocumentId, ["author"] = doc.AuthorKingdomId,
                ["body"] = doc.Body, ["required_answer"] = doc.DocumentId == job.SourceDocumentId });
        }
        bool requiresAgenda = RequiresDeclarationAgenda(job, round);
        return new JObject { ["version"] = 1, ["draft_digest"] = MaterialHash(body), ["required_agenda"] = requiresAgenda,
            ["body_segments"] = new JArray(DeclarationBodySegments(body).Select((text, index) =>
                new JObject { ["index"] = index, ["text"] = text })), ["allowed_answer_sources"] = sources };
    }

    private static bool TryReadDeclarationSpan(JToken token, string body, out string quote)
    {
        quote = "";
        if (token is not JArray span) return false;
        if (span.Count == 0) return true;
        if (span.Any(x => x.Type != JTokenType.Integer)) return false;
        // Read as long so malformed model integers cannot overflow the dispatcher.
        // A single segment index is an unambiguous one-segment range. Preserve
        // digest, bounds and conflicting-quote checks; never infer missing evidence.
        if (!long.TryParse(span[0].ToString(), out long first) || !long.TryParse(span[span.Count - 1].ToString(), out long last)) return false;
        var segments = DeclarationBodySegments(body);
        if (first < 0 || last < first || last >= segments.Count) return false;
        // Enumerating a continuous range denotes exactly the same evidence.
        // Never join gaps, reorder indices or infer missing evidence.
        if (span.Count > 2)
        {
            if (last - first + 1 != span.Count) return false;
            for (int i = 1; i < span.Count; i++)
                if (!long.TryParse(span[i].ToString(), out long index) || index != first + i) return false;
        }
        quote = string.Concat(segments.Skip((int)first).Take((int)(last - first + 1)));
        return true;
    }

    private static bool ResolveDeclarationAuditSpans(JObject review, string body, out string reason)
    {
        reason = "";
        bool hasSpans = review["agenda_span"] != null
            || (review["agenda_answers"] as JArray)?.OfType<JObject>().Any(x => x["span"] != null) == true;
        if (!hasSpans) return true; // Compatible with strictly verified legacy verbatim reviews.
        if (review["draft_digest"]?.Type != JTokenType.String || (string)review["draft_digest"] != MaterialHash(body))
        { reason = "audit_draft_digest_mismatch"; return false; }
        bool Resolve(JObject row, string spanKey, string quoteKey)
        {
            if (row[spanKey] == null) return true;
            if (!TryReadDeclarationSpan(row[spanKey], body, out string quote)) return false;
            if (row[quoteKey] != null && (row[quoteKey].Type != JTokenType.String || (string)row[quoteKey] != quote)) return false;
            row[quoteKey] = quote;
            return true;
        }
        if (!Resolve(review, "agenda_span", "agenda_quote"))
        { reason = "agenda_span_invalid_or_conflicting_quote"; return false; }
        foreach (var answer in (review["agenda_answers"] as JArray ?? new()).OfType<JObject>())
            if (!Resolve(answer, "span", "quote"))
            { reason = "agenda_answer_span_invalid_or_conflicting_quote"; return false; }
        return true;
    }
}
