using System;
using System.Linq;
using System.Text.RegularExpressions;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    private static readonly string[] PositionFields = { "agenda", "gain", "cost", "red_line", "alternative" };
    private static string PublicSpeechPattern => DiplomacyCompatibilityTexts.Get("Event_PublicSpeechPattern");
    private static readonly Lazy<Regex> PublicSpeech = new(() => new Regex(PublicSpeechPattern,
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
    private static readonly Regex PublicInternalIdentity = new Regex(
        @"(?<![a-zA-Z0-9_])(?:diplomacy_(?:document|round)[_:][a-zA-Z0-9_:-]+|ruler_[a-zA-Z0-9_]+|lord_\d+_\d+)(?![a-zA-Z0-9_])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Lazy<Regex> CollectiveDecision = new(() => new Regex(
        DiplomacyCompatibilityTexts.Get("Event_CollectiveDecisionPattern"),
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));

    private void ProjectEventPublicIdentities(WorldDiplomacyRound round, JObject draft)
    {
        if (!IsPlayerDeclarationOpening(round)) return;
        var fields = new[] { "title", "background", "body" };
        var ids = fields.Where(x => draft[x]?.Type == JTokenType.String)
            .SelectMany(x => PublicInternalIdentity.Matches((string)draft[x]).Cast<Match>().Select(m => m.Value))
            .Distinct(StringComparer.Ordinal).ToList();
        if (ids.Count == 0) return;
        // Request-completion boundary only. Resolve exact, authoritative IDs;
        // never infer identity from prose or modify stored human/source records.
        var names = new System.Collections.Generic.Dictionary<string,string>(StringComparer.Ordinal);
        foreach (string kingdom in _host.AllKingdomIds())
        {
            string id = _host.PartyRulerId(kingdom), name = _host.PartyRulerName(kingdom);
            if (ids.Contains(id) && !string.IsNullOrWhiteSpace(name)) names[id] = name;
        }
        foreach (string id in ids.Where(x => !names.ContainsKey(x)))
        {
            var doc = ResolveDocument(id);
            if (doc?.IsReadyForPublication == true && doc.RoundId == round.RoundId && !string.IsNullOrWhiteSpace(doc.Title))
                names[id] = doc.Title;
        }
        if (ids.Any(x => !names.ContainsKey(x)))
            foreach (var actor in NarrativeActors(_host.AllKingdomIds()))
                if (ids.Contains(actor.Key) && !string.IsNullOrWhiteSpace(actor.Value)) names[actor.Key] = actor.Value;
        foreach (string field in fields.Where(x => draft[x]?.Type == JTokenType.String))
            draft[field] = PublicInternalIdentity.Replace((string)draft[field], m => names.TryGetValue(m.Value, out string name) ? name : m.Value);
        // Unknown IDs remain visible to the rejection guard. Audit sees this
        // exact projected draft and its digest before anything can be published.
    }

    private bool ValidateUnansweredPlayerNarration(WorldDiplomacyRound round, JObject draft, out string reason)
    {
        reason = "";
        foreach (string field in new[] { "title", "background", "body" })
        {
            string text = draft[field]?.Type == JTokenType.String ? (string)draft[field] : "";
            if (Regex.IsMatch(text, DiplomacyCompatibilityTexts.Get("Event_InternalHeadingPattern")))
            { reason = "public_narrative_internal_heading:" + field + ";use_natural_attributed_narrative_only"; return false; }
            if (Regex.IsMatch(text, @"(?i)\b(?:lore:[a-f0-9]{16,}|persona:[a-z0-9_]+|world:current)\b"))
            { reason = "public_narrative_source_id:" + field + ";keep_source_keys_in_internal_fields_only"; return false; }
        }
        if (!IsPlayerDeclarationOpening(round)) return true;
        bool unanswered = !Storage.Documents.Any(x => x.RoundId == round.RoundId
            && x.IsPlayerAuthored && x.IsReadyForPublication && x.DocumentId != round.RootDocumentId);
        // A conservative, request-boundary guard for the observed collective
        // decision failure. It never edits prose or infers a human response.
        foreach (string field in new[] { "title", "background", "body", "reason" })
        {
            string text = draft[field]?.Type == JTokenType.String ? (string)draft[field] : "";
            // Public prose uses names/titles. Structured source fields continue
            // to carry exact identities; the original human document is untouched.
            var identity = PublicInternalIdentity.Match(text);
            if (field != "reason" && identity.Success)
            {
                reason = "public_narrative_internal_identity:field=" + field + ";identity=" + identity.Value
                    + ";use_supplied_display_name_or_document_title_in_public_prose;retain_exact_IDs_only_in_structured_source_fields";
                return false;
            }
            if (!unanswered) continue;
            var match = CollectiveDecision.Value.Match(text);
            if (!match.Success) continue;
            reason = "unanswered_player_collective_decision:field=" + field + ";phrase=" + match.Value
                + ";human_has_no_further_published_reply;attribute_each_actual_NPC_position_separately_and_describe_unresolved_outcomes_without_inventing_a_collective_human_choice";
            return false;
        }
        return true;
    }

    private static string FreezeEventDraft(JObject draft)
    {
        // A writer's self-approval is not evidence for the independent reviewer.
        // Never offer it as an example to copy, including when resuming old jobs.
        foreach (string key in new[] { "approved", "narrative_safety", "actor_review", "position_reviews", "closure_consistent", "theme_review", "story_review" })
            draft.Remove(key);
        return draft.ToString(Formatting.None);
    }

    private static string EventAuditFeedback(JObject audit)
    {
        if (audit["story_review"] is JObject story)
        {
            if (story["motive_preserved"]?.Type == JTokenType.Boolean && !(bool)story["motive_preserved"] && ReviewText(story, "motive_reason"))
                return "story_motive_replaced:" + (string)story["motive_reason"];
            var duplicate = (story["comparisons"] as JArray)?.OfType<JObject>().FirstOrDefault(x => x["verdict"]?.Type == JTokenType.String
                && (string)x["verdict"] == "duplicate" && x["event_id"]?.Type == JTokenType.String && ReviewText(x, "reason"));
            if (duplicate != null) return "story_structure_duplicate:" + (string)duplicate["event_id"] + ":" + (string)duplicate["reason"];
        }
        foreach (JToken value in new[] { audit["reason"], audit["repair_feedback"], (audit["narrative_safety"] as JObject)?["reason"] })
            if (value?.Type == JTokenType.String && !string.IsNullOrWhiteSpace((string)value))
                return (string)value;
        return "reviewer_must_explain_the_evidence_or_semantic_problem";
    }

    private bool ValidatePlayerOpeningNarrative(WorldDiplomacyRound round, string stage, JObject draft, out string reason)
    {
        reason = "";
        if (stage != "started" || !IsPlayerDeclarationOpening(round)) return true;
        foreach (var fact in (draft["narrative"] as JArray ?? new JArray()).OfType<JObject>())
        {
            if (!TryReadNarrativeSubject(fact, out string subject, out _)
                || _host.IsPlayerParty(subject)) continue;
            if ((string)fact["kind"] != "allegation" && !PublicSpeech.Value.IsMatch((string)fact["text"] ?? "")) continue;
            var source = ResolveDocument((string)fact["source"]);
            if (source?.IsReadyForPublication == true && source.AuthorKingdomId == subject) continue;
            reason = "player_opening_npc_allegation_requires_published_source_document:subject=" + subject
                + ";an_internal_position_or_lore_is_not_a_published_reply;describe_existing_interests_without_claiming_an_accusation_or_response";
            return false;
        }
        // Public prose must obey the same source rule even when a writer omits
        // the claim from narrative[] or labels it public_fact/world:current.
        // Bound this additional check to the actual sovereigns at cold completion.
        foreach (string country in _host.AllKingdomIds().Where(x => !_host.IsPlayerParty(x)))
        {
            if (Storage.Documents.Any(x => x.RoundId == round.RoundId && x.IsReadyForPublication && x.AuthorKingdomId == country)) continue;
            foreach (string name in new[] { _host.PartyNameOrEmpty(country), _host.PartyRulerName(country) }.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
            {
                string pattern = Regex.Escape(name) + @"[^，。！？；,.;!?\r\n]{0,24}(?:" + PublicSpeechPattern + ")";
                foreach (string field in new[] { "title", "background", "body" })
                {
                    if (!Regex.IsMatch((string)draft[field] ?? "", pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) continue;
                    reason = "player_opening_unpublished_npc_speech:field=" + field + ";subject=" + country
                        + ";describe_existing_interests_not_an_already_sent_reply;world_identity_is_not_publication_evidence";
                    return false;
                }
            }
        }
        return true;
    }

    private object EventPositionsForModel(WorldDiplomacyRound round) => round.Participants.Select(x => new {
        kingdom = x.KingdomId, state = x.State, human_controlled = _host.IsPlayerParty(x.KingdomId),
        position_required = !_host.IsPlayerParty(x.KingdomId) && (x.State == "active" || !string.IsNullOrEmpty(x.Agenda)),
        agenda = _host.IsPlayerParty(x.KingdomId) ? "" : x.Agenda,
        gain = _host.IsPlayerParty(x.KingdomId) ? "" : x.PreferredOutcome,
        cost = _host.IsPlayerParty(x.KingdomId) ? "" : x.ExpectedCost,
        red_line = _host.IsPlayerParty(x.KingdomId) ? "" : x.RedLine,
        alternative = _host.IsPlayerParty(x.KingdomId) ? "" : x.Alternative,
        x.AgendaExpressionDocumentId, x.AgendaAnswerDocumentIds, x.AgendaAddressTo
    });

    private static bool IsEndingEvidenceRequest(WorldDiplomacyJob job) => job.ForcedIntent == "ended"
        && job.Kind is "event_boundary" or "event_boundary_audit";

    private object NarrativeSafetyRequest(WorldDiplomacyJob job, JObject evidence = null)
    {
        bool ending = IsEndingEvidenceRequest(job);
        job.EventNarrativeReviewVersion = ending ? 6 : IsPlayerDeclarationOpening(ResolveRound(job.RoundId)) ? 5 : 3;
        var draft = WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(job.PrivacyDraft);
        if (draft != null) job.PrivacyDraft = FreezeEventDraft(draft);
        bool audit = job.Kind.EndsWith("_audit", StringComparison.Ordinal);
        var round = ResolveRound(job.RoundId);
        var documents = (evidence?["documents"] as JArray)?.OfType<JObject>().ToList()
            ?? Storage.Documents.Where(x => x.RoundId == job.RoundId && x.IsReadyForPublication)
                .OrderByDescending(x => x.CreatedUtcTicks).Take(32).Select(x => new JObject {
                    ["DocumentId"] = x.DocumentId, ["Title"] = x.Title, ["Body"] = x.Body,
                    ["AuthorKingdomId"] = x.AuthorKingdomId, ["IsPlayerAuthored"] = x.IsPlayerAuthored }).ToList();
        return new {
            mode = audit ? "independent_review" : "write_without_self_review",
            digest = audit ? NarrativeSafetyDigest(job, draft) : null,
            player_kingdom = CurrentPlayerAuthority(),
            player_ruler_hero_id = _host.PartyRulerId(CurrentPlayerAuthority()),
            event_id = job.RoundId,
            stage = job.ForcedIntent,
            // Capture on the request boundary, never infer current war from a
            // political proposal, narrative history or intercepted native action.
            current_war_pairs = _host.ActiveWarKingdomPairs().Select(x => new { first = x.firstId, second = x.secondId }).ToArray(),
            diplomatic_state_review_required = true,
            player_documents = documents
                .Where(x => x["IsPlayerAuthored"]?.Value<bool>() == true)
                .Select(x => new { DocumentId = (string)x["DocumentId"], Title = (string)x["Title"], Body = (string)x["Body"] }),
            published_npc_documents = documents.Where(x => x["IsPlayerAuthored"]?.Value<bool>() != true)
                .Select(x => new { DocumentId = (string)x["DocumentId"], Author = (string)x["AuthorKingdomId"], Body = (string)x["Body"] }),
            unexpressed_npc_positions = round?.Participants.Where(x => !_host.IsPlayerParty(x.KingdomId)
                && string.IsNullOrEmpty(x.AgendaExpressionDocumentId)).Select(x => x.KingdomId).ToArray(),
            required_checks = audit ? (ending
                ? new[] { "player_agency_valid", "continuity_valid", "geography_valid", "publication_order_valid", "world_identity_valid", "event_record_basis_valid", "diplomatic_state_valid" }
                : job.EventNarrativeReviewVersion >= 4
                ? new[] { "player_agency_valid", "continuity_valid", "geography_valid", "publication_order_valid", "world_identity_valid", "world_book_basis", "diplomatic_state_valid" }
                : new[] { "player_agency_valid", "continuity_valid", "geography_valid", "publication_order_valid", "diplomatic_state_valid" }) : Array.Empty<string>()
        };
    }

    private static string NarrativeSafetyDigest(WorldDiplomacyJob job, JObject draft) =>
        MaterialHash(job.RoundId + "\n" + job.ForcedIntent + "\n" + (draft?.ToString(Formatting.None) ?? ""));

    private static bool ValidateNarrativeSafety(WorldDiplomacyJob job, JObject draft, JObject audit,
        out string reason, out bool rejected)
    {
        rejected = false; reason = "narrative_safety_review_incomplete:review_player_agency_and_event_continuity_for_the_same_draft";
        if (audit["narrative_safety"] is not JObject review
            || review["digest"]?.Type != JTokenType.String || (string)review["digest"] != NarrativeSafetyDigest(job, draft)
            || review["player_agency_valid"]?.Type != JTokenType.Boolean
            || review["continuity_valid"]?.Type != JTokenType.Boolean
            || review["reason"]?.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)review["reason"])
            || ((string)review["reason"]).Length > 2000) return false;
        if (review["geography_valid"]?.Type != JTokenType.Boolean)
        { reason = "narrative_geography_review_missing:verify_current_map_in_same_draft"; return false; }
        if (WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(job.UserPrompt)?["narrative_safety_review_required"]?["diplomatic_state_review_required"]?.Value<bool>() == true)
        {
            if (review["diplomatic_state_valid"]?.Type != JTokenType.Boolean)
            { reason = "narrative_diplomatic_state_review_missing:compare_current_war_pairs;intercepted_requests_are_not_execution_receipts"; return false; }
            if (!(bool)review["diplomatic_state_valid"])
            { rejected = true; reason = "narrative_diplomatic_state_rejected:" + (string)review["reason"]; return false; }
        }
        if (job.EventNarrativeReviewVersion >= 3 && review["publication_order_valid"]?.Type != JTokenType.Boolean)
        { reason = "narrative_publication_order_review_missing:distinguish_unexpressed_positions_from_actual_replies_and_unknown_player_choices"; return false; }
        if (job.EventNarrativeReviewVersion >= 4)
        {
            reason = "narrative_world_identity_boolean_missing:provide_narrative_safety.world_identity_valid";
            if (review["world_identity_valid"]?.Type != JTokenType.Boolean) return false;
            // A substantive rejection needs a corrected draft, not invented
            // positive evidence. Only an approval must carry exact quotations.
            if (!(bool)review["world_identity_valid"])
            { rejected = true; reason = "narrative_world_identity_rejected:" + (string)review["reason"]; return false; }
        }
        if (IsEndingEvidenceRequest(job))
        {
            reason = "narrative_event_record_basis_missing:verify_public_documents_exact_commitments_receipts_and_no_new_world_facts";
            if (review["event_record_basis_valid"]?.Type != JTokenType.Boolean) return false;
            if (!(bool)review["event_record_basis_valid"])
            { rejected = true; reason = "narrative_event_record_basis_rejected:" + (string)review["reason"]; return false; }
        }
        else if (job.EventNarrativeReviewVersion >= 4)
        {
            reason = "narrative_world_book_basis_shape:provide_1_to_8_source_key_and_exact_quote_entries_in_narrative_safety.world_book_basis";
            if (review["world_book_basis"] is not JArray basis || basis.Count == 0 || basis.Count > 8) return false;
            JArray supplied;
            reason = "narrative_world_book_snapshot_invalid:cannot_verify_quotes_without_frozen_sources";
            try
            {
                var request = JObject.Parse(job.UserPrompt);
                supplied = SelectWorldBookEvidence(request["world_book_sources"] as JArray ?? request["sources"] as JArray ?? new JArray());
            }
            catch (JsonException) { return false; }
            for (int index = 0; index < basis.Count; index++)
            {
                var entry = basis[index];
                reason = "narrative_world_book_entry_shape:index=" + index + ";source_key_and_quote_must_be_strings";
                if (entry is not JObject || entry["source_key"]?.Type != JTokenType.String
                    || entry["quote"]?.Type != JTokenType.String) return false;
                string key = (string)entry["source_key"], quote = (string)entry["quote"];
                var matching = supplied.OfType<JObject>().Where(x => (string)x["key"] == key).ToList();
                string failure = matching.Count == 0 ? "source_key_not_supplied"
                    : string.IsNullOrWhiteSpace(quote) || quote.Length < 6 || quote.Length > 600 ? "quote_length_must_be_6_to_600"
                    : matching.Any(x => ((string)x["text"]).IndexOf(quote, StringComparison.Ordinal) >= 0) ? null : "quote_not_contiguous_source_text";
                if (failure != null)
                {
                    // Bound feedback and retain the actual failed citation. A
                    // generic missing-review error made live retries repeat the
                    // same paraphrase without ever identifying what was wrong.
                    reason = "narrative_world_book_citation_invalid:" + new JObject {
                        ["index"] = index, ["failure"] = failure,
                        ["source_key"] = key.Length <= 160 ? key : key.Substring(0, 160),
                        ["rejected_quote"] = quote.Length <= 600 ? quote : quote.Substring(0, 600),
                        ["repair_action"] = "recheck_same_draft;copy_exact_contiguous_source_text;split_separate_passages;reject_unsupported_claim"
                    }.ToString(Formatting.None);
                    return false;
                }
            }
        }
        if (!(bool)review["player_agency_valid"] || !(bool)review["continuity_valid"]
            || !(bool)review["geography_valid"] || job.EventNarrativeReviewVersion >= 3 && !(bool)review["publication_order_valid"])
        {
            rejected = true;
            reason = "narrative_safety_rejected:" + (string)review["reason"];
            return false;
        }
        reason = ""; return true;
    }

    private bool TryReadConceptPosition(string kingdom, JObject row, out WorldDiplomacyRoundParticipant position, out string reason)
    {
        reason = "event_concept_invalid:participants[" + kingdom + "].agenda_gain_cost_red_line_alternative_require_nonempty_strings_at_most_1000_chars";
        if (!_host.IsPlayerParty(kingdom)) return TryReadEventPosition(row, out position);
        position = null;
        reason = "event_concept_player_agency:participants[" + kingdom + "];player_agenda_gain_cost_red_line_alternative_and_address_to_must_be_empty;do_not_author_player_intentions";
        if (PositionFields.Any(x => row[x]?.Type != JTokenType.String || !string.IsNullOrEmpty((string)row[x]))
            || row["address_to"] is not JArray targets || targets.Count != 0) return false;
        position = new WorldDiplomacyRoundParticipant(); reason = ""; return true;
    }

    private bool ValidatePlayerNarrativeSource(JObject fact, out string reason)
    {
        reason = "";
        // A generic world/personality record cannot be evidence that the human
        // issued an accusation. The source must be their actual published words.
        // Subject is the speaker. Mentioning the human as an allegation's target
        // must not turn an NPC's statement into invented human speech.
        if ((string)fact["kind"] != "allegation"
            || !TryReadNarrativeSubject(fact, out string subject, out _) || !_host.IsPlayerParty(subject)) return true;
        var source = ResolveDocument((string)fact["source"]);
        if (source?.IsPlayerAuthored == true && source.IsReadyForPublication) return true;
        reason = "narrative_player_allegation_source_required:player_ruler_speech_requires_an_actual_published_player_document;world_or_persona_is_not_a_player_action";
        return false;
    }
}
