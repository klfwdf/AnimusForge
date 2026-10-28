using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    // A submitted human matter or confirmed oral undertaking is not an NPC
    // story candidate. It still passes identity, authority and continuity review.
    private static bool IsBoundMatterPlanning(WorldDiplomacyJob job) => job.EventSelectionStage is "player" or "oral";

    private bool IsRequiredMatterHistory(WorldDiplomacyJob job, JObject history)
    {
        string id = (string)history["RoundId"] ?? "";
        if (id.Length == 0) return true;
        var root = ResolveDocument(job.DocumentId) ?? ResolveDocument(ResolveRound(job.RoundId)?.RootDocumentId);
        if (root?.DiscussionRoundId == id || ResolveDocument(root?.SourceDocumentId)?.RoundId == id
            || ResolveDocument(job.SourceDocumentId)?.RoundId == id) return true;
        if ((root?.AnsweredNpcDocumentIds ?? new()).Any(x => ResolveDocument(x)?.RoundId == id)
            || ResolveDocument(root?.RespondingToOfferDocumentId)?.RoundId == id
            || ResolveDocument(root?.RespondingToThreatDocumentId)?.RoundId == id) return true;
        // Keep cases cited by the current draft/trigger, including title-only
        // references. Optional whole cases may be omitted; never cut their terms.
        string context = (job.PrivacyDraft ?? "") + (job.EventTriggerMaterial ?? "");
        string title = (string)history["RoundTopic"] ?? "";
        return context.Contains(id) || (title.Length > 0 && context.Contains(title));
    }

    private sealed class EventPriorStory
    {
        public string RoundId, RoundTopic, EventBackground, EventOutcome, EventOutcomeReason, Fingerprint;
        public List<string> EventMaterialKeys;
        public int Day;
    }

    private static string EventConceptFingerprint(string background, IEnumerable<string> parties) =>
        MaterialHash(Regex.Replace(background ?? "", @"\s+", " ").Trim().ToLowerInvariant() + "|"
            + string.Join("|", parties.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal)));

    private static string StoryReviewDigest(WorldDiplomacyJob job, JArray prior) =>
        MaterialHash(job.PrivacyDraft + "|" + job.EventSelectedCandidate + "|" + prior.ToString(Formatting.None)
            + "|" + job.EventMaterialSnapshot);

    private static List<EventPriorStory> SelectPriorStoryWindow(List<EventPriorStory> prior, HashSet<string> keys, string fingerprint = "") =>
        prior.Where(x => fingerprint.Length > 0 && x.Fingerprint == fingerprint)
            .Concat(prior.OrderByDescending(x => x.Day).Take(6))
            .Concat(prior.OrderByDescending(x => (x.EventMaterialKeys ?? new()).Count(keys.Contains)).ThenByDescending(x => x.Day))
            .GroupBy(x => x.RoundId, StringComparer.Ordinal).Select(x => x.First()).Take(12).ToList();

    private static bool ReviewText(JObject row, string key) => row?[key]?.Type == JTokenType.String
        && !string.IsNullOrWhiteSpace((string)row[key]) && ((string)row[key]).Length <= 2400;

    // Validates evidence coverage and exact quotations, not the model's semantic
    // judgement. A separate audit request judges motives, choices and novelty.
    private bool ValidateStoryReview(WorldDiplomacyJob job, JObject result, out string reason, out bool rejected)
    {
        reason = "story_review_missing_or_invalid"; rejected = false;
        if (IsBoundMatterPlanning(job) || job.StoryReviewVersion < 1) return true;
        var request = JObject.Parse(job.UserPrompt);
        var prior = request["prior_events"] as JArray ?? new();
        var review = result["story_review"] as JObject;
        var draft = JObject.Parse(job.PrivacyDraft);
        string background = (string)draft["background"] ?? "";
        reason = "story_review_digest_mismatch:copy_story_review_required.digest_for_this_draft";
        if (review?["digest"]?.Type != JTokenType.String || (string)review["digest"] != StoryReviewDigest(job, prior)) return false;
        reason = "story_review_motive_shape:motive_preserved_boolean_and_nonempty_motive_reason_required";
        if (review["motive_preserved"]?.Type != JTokenType.Boolean || !ReviewText(review, "motive_reason")) return false;
        reason = "story_review_comparisons_shape:array_required";
        if (review["comparisons"] is not JArray comparisons) return false;
        var expectedIds = prior.OfType<JObject>().Select(x => (string)x["RoundId"]).ToList();
        var actualIds = comparisons.OfType<JObject>().Select(x => (string)x["event_id"]).ToList();
        reason = "story_review_case_coverage:missing=" + string.Join(",", expectedIds.Except(actualIds))
            + ";unexpected=" + string.Join(",", actualIds.Except(expectedIds)) + ";expected_count=" + prior.Count
            + ";actual_count=" + comparisons.Count + ";one_object_per_event_id";
        if (comparisons.Count != prior.Count || actualIds.Count != comparisons.Count
            || expectedIds.Except(actualIds).Any() || actualIds.Distinct().Count() != actualIds.Count) return false;
        if (!(bool)review["motive_preserved"])
        { rejected = true; reason = "story_motive_replaced:" + (string)review["motive_reason"]; return false; }
        var seed = WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(job.EventSelectedCandidate);
        if (!string.IsNullOrWhiteSpace((string)seed?["motive"]))
        {
            reason = "story_review_motive_quotes:seed_motive_quote_must_match_selected_motive;draft_motive_quote_must_match_background";
            if (!ReviewText(review, "seed_motive_quote") || !((string)seed["motive"]).Contains((string)review["seed_motive_quote"])
                || !ReviewText(review, "draft_motive_quote") || !background.Contains((string)review["draft_motive_quote"])) return false;
        }
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in comparisons.OfType<JObject>())
        {
            reason = "story_review_comparison_shape:event_id_and_verdict_strings_required";
            if (row["event_id"]?.Type != JTokenType.String || row["verdict"]?.Type != JTokenType.String) return false;
            string id = (string)row["event_id"], verdict = (string)row["verdict"];
            var old = prior.OfType<JObject>().FirstOrDefault(x => (string)x["RoundId"] == id);
            reason = "story_review_comparison_invalid:event_id=" + id + ";require_unique_supplied_id;verdict_distinct_duplicate_revisit;nonempty_reason";
            if (old == null || !seen.Add(id) || verdict is not ("distinct" or "duplicate" or "revisit")
                || !ReviewText(row, "reason")) return false;
            if (verdict == "duplicate")
            { rejected = true; reason = "story_structure_duplicate:" + id + ":" + (string)row["reason"]; return false; }
            reason = "story_review_prior_quote_invalid:event_id=" + id + ";copy_contiguous_EventBackground_or_EventOutcomeReason";
            if (!ReviewText(row, "prior_quote")
                || !new[] { (string)old["EventBackground"] ?? "", (string)old["EventOutcomeReason"] ?? "" }
                    .Any(x => x.Contains((string)row["prior_quote"]))) return false;
            reason = "story_review_draft_quote_invalid:event_id=" + id + ";use_comparisons[].draft_quote_not_draft_motive_quote;copy_contiguous_background";
            if (!ReviewText(row, "draft_quote") || !background.Contains((string)row["draft_quote"])) return false;
            reason = "story_review_new_development_quote_invalid:event_id=" + id + ";revisit_requires_exact_background_quote";
            if (verdict == "revisit" && (!ReviewText(row, "new_development_quote")
                || !background.Contains((string)row["new_development_quote"]))) return false;
        }
        reason = ""; return seen.Count == prior.Count;
    }

    // Cold concept-request boundary only. Public boundary receipts retain compact
    // identity after old completed-round records leave the display cache.
    private List<EventPriorStory> ReadPriorEventStories(WorldDiplomacyRound current)
    {
        var stories = new Dictionary<string, EventPriorStory>(StringComparer.Ordinal);
        foreach (var notice in Storage.EventAnnouncements.Where(x => x.EventId != current.RoundId && x.State == "public" && x.Stage == "started"))
            stories[notice.EventId] = new EventPriorStory {
                RoundId = notice.EventId, RoundTopic = notice.Title, EventBackground = notice.Body,
                EventMaterialKeys = notice.MaterialKeys?.Count > 0 ? notice.MaterialKeys : notice.Narrative.Where(x => x.Public).Select(x => x.Source).Distinct().ToList(),
                EventOutcome = "", EventOutcomeReason = "", Day = notice.Day, Fingerprint = notice.ConceptFingerprint ?? ""
            };
        foreach (var prior in Storage.CompletedRounds.Concat(GetLiveRounds()).Where(x => x != current
            && !string.IsNullOrEmpty(x.EventBackground) && (stories.ContainsKey(x.RoundId)
                || Storage.Documents.Any(d => d.RoundId == x.RoundId && d.IsReadyForPublication))))
            stories[prior.RoundId] = new EventPriorStory {
                RoundId = prior.RoundId, RoundTopic = prior.RoundTopic, EventBackground = prior.EventBackground,
                EventMaterialKeys = prior.EventMaterialKeys ?? new(), EventOutcome = prior.EventOutcome,
                EventOutcomeReason = prior.EventOutcomeReason, Day = prior.LastActivityDay,
                Fingerprint = EventConceptFingerprint(prior.EventBackground, prior.Participants.Select(x => x.KingdomId))
            };
        // Published endings are the detailed receipt. Compact live/completed
        // status must never overwrite their negotiated conditions and outcome.
        foreach (var notice in Storage.EventAnnouncements.Where(x => x.EventId != current.RoundId && x.State == "public" && x.Stage == "ended")
            .OrderBy(x => x.Day))
        {
            if (!stories.TryGetValue(notice.EventId, out var story))
                stories[notice.EventId] = story = new EventPriorStory { RoundId = notice.EventId, RoundTopic = notice.Title,
                    EventBackground = "", EventMaterialKeys = notice.MaterialKeys ?? new(), Fingerprint = notice.ConceptFingerprint ?? "" };
            if (string.IsNullOrEmpty(story.EventOutcome)) story.EventOutcome = "closed";
            if (!string.IsNullOrWhiteSpace(notice.Body)) story.EventOutcomeReason = notice.Body;
            story.Day = Math.Max(story.Day, notice.Day);
        }
        return stories.Values.ToList();
    }
}
