using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.DiplomacyDialogue;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    private const int CurrentSpeechExtractionContractVersion = 3;
    private const string SpeechConsentAuthorityFailure = "speech_player_consent_without_authority";
    private const string SpeechPayloadFailure = "speech_payload_invalid";
    private WorldDiplomacyStorage _speechReviewStorage;
    private readonly Dictionary<string, DiplomacySpeechReview> _speechReviews = new(StringComparer.Ordinal);
    private readonly HashSet<string> _reviewedSpeech = new(StringComparer.Ordinal);
    private int _speechReviewHour = -1, _speechReviewCursor;
    private static bool IsSpeechReviewJob(WorldDiplomacyJob job) => job?.Kind is "oral_review" or "oral_review_audit";
    private void EnsureSpeechReviewIndex()
    {
        if (ReferenceEquals(_speechReviewStorage, Storage)) return;
        _speechReviewStorage = Storage; _speechReviews.Clear(); _reviewedSpeech.Clear();
        Storage.SpeechReviews ??= new(); Storage.ReviewedSpeechSources ??= new();
        foreach (var item in Storage.SpeechReviews.Where(x => x != null))
        {
            item.AppliedPayloadKeys ??= new(); item.WatchedKingdoms ??= new();
            if (item.Status == "applying") item.Status = "execution_uncertain";
            _speechReviews[item.Id] = item;
        }
        foreach (string key in Storage.ReviewedSpeechSources) _reviewedSpeech.Add(key);
        _speechReviewHour = -1; _speechReviewCursor = 0;
    }
    internal bool CaptureSpeechReview(string ruler, string actor, DialogueInteractionOrigin origin, string tagHints)
    {
        if (!_persistenceHealthy || !_host.WorldDiplomacyEnabled() || origin == null || string.IsNullOrWhiteSpace(origin.FullNpcText)
            || !DialogueRulerIsCurrent(actor, ruler) || _host.IsPlayerParty(actor) || !_host.HasIndependentAuthority(actor)) return false;
        EnsureSpeechReviewIndex();
        string key = ruler + "|" + origin.InteractionId;
        if (_reviewedSpeech.Contains(key) || Storage.SpeechReviews.Any(x => x.SourceKey == key)) return true;
        var item = new DiplomacySpeechReview { ExtractionContractVersion = CurrentSpeechExtractionContractVersion, Id = _host.NewId("speech_review"), SourceKey = key, Actor = actor, Ruler = ruler,
            PlayerKingdom = _host.PlayerKingdomId(), PlayerRuler = string.IsNullOrEmpty(CurrentPlayerAuthority()) ? "" : _host.PartyRulerId(CurrentPlayerAuthority()),
            ConfigurationFingerprint = SpeechConfiguration(), ServiceRecoveryEpoch = Storage.SpeechServiceRecoveryEpoch,
            Channel = origin.Channel, Interaction = origin.InteractionId, Session = origin.SessionId,
            PlayerText = origin.FullPlayerText, NpcText = origin.FullNpcText, TagHints = tagHints ?? "", RetryDay = _host.CurrentDay() };
        Storage.SpeechReviews.Add(item); _speechReviews[item.Id] = item;
        // A real new conversation is an opportunity to clarify a held statement.
        // Timers alone never restart exhausted or unrepresentable semantics.
        foreach (var prior in Storage.SpeechReviews.Where(x => x != item && x.Ruler == ruler && x.Status is "technical_hold" or "conditional"))
        { ResumeSpeechReview(prior, "new_conversation"); }
        _speechReviewHour = -1;
        QueueSpeechReview(item);
        return true;
    }
    private void QueueSpeechReview(DiplomacySpeechReview item)
    {
        if (item.Status != "pending" || item.RetryDay > _host.CurrentDay()
            || Storage.Jobs.Any(x => IsSpeechReviewJob(x) && x.DocumentId == item.Id)) return;
        EnqueueJob(new WorldDiplomacyJob { JobId = _host.NewId("oral_review"), Kind = "oral_review", DocumentId = item.Id,
            AuthorKingdomId = item.Actor, PersonalMemoryRulerId = item.Ruler, RequestLane = "oral", SchedulingOwner = "oral",
            Priority = 95, CreatedDay = _host.CurrentDay(), MaxTokens = _host.AnalysisMaxTokens() });
    }
    private void MaintainSpeechReviews()
    {
        EnsureSpeechReviewIndex();
        int hour = _host.CurrentHour(); if (hour == _speechReviewHour) return; _speechReviewHour = hour;
        int count = Math.Min(4, Storage.SpeechReviews.Count);
        for (int n = 0; n < count; n++)
        {
            if (_speechReviewCursor >= Storage.SpeechReviews.Count) _speechReviewCursor = 0;
            var item = Storage.SpeechReviews[_speechReviewCursor++];
            if (!DialogueRulerIsCurrent(item.Actor, item.Ruler) || _host.IsEliminatedParty(item.Actor))
            {
                item.Status = "authority_unavailable"; item.Reason = "original_ruler_no_longer_authorized";
                foreach (var obsolete in Storage.Jobs.Where(x => IsSpeechReviewJob(x) && x.DocumentId == item.Id).ToList()) RemoveJob(obsolete.JobId);
                continue;
            }
            if (item.ExtractionContractVersion < CurrentSpeechExtractionContractVersion)
            {
                item.ExtractionContractVersion = CurrentSpeechExtractionContractVersion;
                if (item.Reason == "speech_extraction_invalid" && item.Status is "technical_hold" or "pending"
                    && item.Applied == 0 && item.AppliedPayloadKeys.Count == 0)
                    ResumeSpeechReview(item, "speech_extraction_contract_recovered");
            }
            RefreshHeldSpeechReview(item);
            QueueSpeechReview(item);
        }
    }
    private bool IsCurrentSpeechReview(WorldDiplomacyJob job)
    {
        EnsureSpeechReviewIndex();
        return _speechReviews.TryGetValue(job.DocumentId, out var item) && item.Status == "pending"
            && DialogueRulerIsCurrent(item.Actor, item.Ruler) && !_host.IsEliminatedParty(item.Actor);
    }
    private IEnumerable<WorldDiplomacyDialogueArrangement> RecentRegisteredSpeechCommitments(DiplomacySpeechReview item)
    {
        // Request boundary only; newest first without sorting/copying the archive.
        int remaining = 12;
        for (int i = Storage.DialogueArrangements.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var entry = Storage.DialogueArrangements[i];
            if (entry.RulerId != item.Ruler || entry.ActorKingdomId != item.Actor || !IsRegisteredDialogueCommitment(entry)) continue;
            remaining--; yield return entry;
        }
    }
    private void PrepareSpeechReview(WorldDiplomacyJob job)
    {
        EnsureSpeechReviewIndex();
        if (!_speechReviews.TryGetValue(job.DocumentId, out var item)) return;
        string evidence = MaterialEvidence(_host.AllKingdomIds().Concat(new[] { item.Actor, item.PlayerKingdom }));
        item.ReviewWorldFingerprint = MaterialHash(evidence);
        job.SystemPrompt = DiplomacyPromptTexts.Get(job.Kind == "oral_review_audit" ? "SpeechReview_Audit" : "SpeechReview_Extract", "")
            + "\n" + DiplomacyPromptTexts.Get("SpeechReview_Conditions", "");
        job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("SpeechReview_OriginalVassalage",
            "When the player is the ruler, new oral agreements making the current NPC's kingdom a tributary, garrison or vassal of the player's kingdom belong to the original direct vassalage mechanism. Do not extract them as publication commitments or unresolved formal terms, whether or not a signing tag was produced. Do not infer success. Continue extracting other commitments in the same speech, explicit replies to existing formal proposals, and other national directions normally.");
        bool correcting = job.Kind == "oral_review" && job.PrivacyRewrites > 0;
        string correction = correcting ? job.PrivacySourceMaterial : item.Reason;
        // Version 2 jobs retained a consent draft without a diagnostic field.
        if (correcting && string.IsNullOrEmpty(correction)) correction = SpeechConsentAuthorityFailure;
        bool correctingConsent = job.Kind == "oral_review" && correction == SpeechConsentAuthorityFailure;
        if (correctingConsent) job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("SpeechReview_RepairConsent", "");
        bool correctingPayload = job.Kind == "oral_review" && correction == SpeechPayloadFailure;
        if (correctingPayload) job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("SpeechReview_RepairPayload", "");
        job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("SpeechReview_EventAttribution", "");
        job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("SovereignControlContract", "");
        job.UserPrompt = JsonConvert.SerializeObject(new {
            speaker = item.Actor, ruler = item.Ruler, player_kingdom = item.PlayerKingdom,
            player_is_ruler = SpeechPlayerHasAuthority(item),
            player_controlled_kingdom = CurrentPlayerAuthority(),
            sovereign_speakers = SovereignSpeakersForModel(_host.AllKingdomIds()),
            current_public_facts = JArray.Parse(evidence),
            actual_player_text = item.PlayerText, actual_npc_text = item.NpcText, untrusted_tag_hints = item.TagHints,
            known_agreements = BuildOralArrangementContext(item.Ruler, item.Actor),
            event_candidates = SpeechEventCandidates(item.Actor),
            registered_commitments = RecentRegisteredSpeechCommitments(item)
                .Select(x => new { x.ArrangementId, x.Version, x.RoundId, x.DocumentId, x.ActorKingdomId,
                    x.TargetKingdomId, action = x.Action.ToString(), move = x.Move.ToString(), x.Status, x.Terms }),
            available_parties = _host.AllKingdomIds().Where(x => _host.PartyResolved(x) && !_host.IsEliminatedParty(x))
                .Select(x => new { id = x, name = _host.PartyNameOrEmpty(x), at_war = _host.PartiesAtWar(item.Actor, x), allied = _host.PartiesAllied(item.Actor, x) }),
            proposed = job.Kind == "oral_review_audit" ? job.PrivacyDraft : "",
            rejected_extraction = correctingConsent || correctingPayload ? job.PrivacyDraft : "",
            validation_error = correctingConsent || correctingPayload ? correction : "" });
        job.LlmMessages?.Clear(); job.MaxTokens = _host.AnalysisMaxTokens(); job.PersonalMemoryRulerId = item.Ruler;
    }
    private void FailSpeechReview(WorldDiplomacyJob job, string reason, bool serviceFailure = false)
    {
        EnsureSpeechReviewIndex();
        if (_speechReviews.TryGetValue(job.DocumentId, out var item))
        {
            item.Attempts++; item.Reason = reason; item.RecoverOnService = serviceFailure;
            item.ConfigurationFingerprint = SpeechConfiguration(); item.ServiceRecoveryEpoch = Storage.SpeechServiceRecoveryEpoch;
            item.RetryDay = _host.CurrentDay() + Math.Min(7, item.Attempts);
            if (item.Attempts >= 3) item.Status = "technical_hold";
            _host.Log("speech_review_failed id=" + item.Id + " reason=" + reason + " attempts=" + item.Attempts);
        }
        RemoveJob(job.JobId);
    }
    private bool SpeechPlayerHasAuthority(DiplomacySpeechReview item)
    {
        string authority = CurrentPlayerAuthority();
        return !string.IsNullOrEmpty(authority) && !string.IsNullOrEmpty(item.PlayerRuler)
            && item.PlayerKingdom == authority && item.PlayerRuler == _host.PartyRulerId(authority);
    }
    private bool HasUnauthorizedSpeechConsent(DiplomacySpeechReview item, JObject json) =>
        !SpeechPlayerHasAuthority(item) && json["commitments"] is JArray rows
        && rows.OfType<JObject>().Any(row => row["player_agreed"]?.Type == JTokenType.Boolean && row["player_agreed"].Value<bool>());
    private bool ValidateSpeechExtraction(DiplomacySpeechReview item, JObject json)
        => TryValidateSpeechExtraction(item, json, out _);
    private bool TryValidateSpeechExtraction(DiplomacySpeechReview item, JObject json, out string reason)
    {
        reason = "speech_extraction_invalid";
        if (json["unresolved"]?.Type != JTokenType.Boolean || json["commitments"] is not JArray rows || rows.Count > 8) return false;
        if (json["condition_watch"] != null && (json["condition_watch"] is not JArray watched || watched.Count > 8
            || watched.Any(x => x.Type != JTokenType.String || !_host.PartyResolved((string)x)))) return false;
        foreach (var row in rows.OfType<JObject>())
        {
            if (row["payload"]?.Type != JTokenType.String || !DialogueTagPayload.TryParse((string)row["payload"], out var parsed))
            { reason = SpeechPayloadFailure; return false; }
            if (parsed.Move == DialogueDiplomaticMove.Discussion || row["player_agreed"]?.Type != JTokenType.Boolean
                || row["npc_quote"]?.Type != JTokenType.String || row["player_quote"]?.Type != JTokenType.String) return false;
            string npcQuote = (string)row["npc_quote"] ?? "", playerQuote = (string)row["player_quote"] ?? "";
            if (string.IsNullOrWhiteSpace(npcQuote) || item.NpcText.IndexOf(npcQuote, StringComparison.Ordinal) < 0) return false;
            if (row["player_agreed"].Value<bool>() && (string.IsNullOrWhiteSpace(playerQuote)
                || item.PlayerText.IndexOf(playerQuote, StringComparison.Ordinal) < 0 || !SpeechPlayerHasAuthority(item))) return false;
            if (!_host.PartyResolved(parsed.TargetKingdomId) || _host.IsEliminatedParty(parsed.TargetKingdomId)) return false;
        }
        return rows.OfType<JObject>().Count() == rows.Count;
    }
    private void CompleteSpeechReview(WorldDiplomacyJob job, JObject json)
    {
        EnsureSpeechReviewIndex();
        if (!IsCurrentSpeechReview(job)) { RemoveJob(job.JobId); return; }
        var item = _speechReviews[job.DocumentId];
        if (job.Kind == "oral_review")
        {
            if (!TryValidateSpeechExtraction(item, json, out string invalid))
            {
                bool wrongConsent = HasUnauthorizedSpeechConsent(item, json);
                // Reuse the persisted pipeline rewrite counter/draft. One repair
                // per review job; scheduling, request budgets and authority stay unchanged.
                if ((wrongConsent || invalid == SpeechPayloadFailure) && job.PrivacyRewrites < 1)
                {
                    job.PrivacyRewrites++;
                    job.PrivacySourceMaterial = wrongConsent ? SpeechConsentAuthorityFailure : invalid;
                    job.PrivacyDraft = json.ToString(Formatting.None);
                    job.IsRunning = false; job.LlmMessages?.Clear();
                    _host.Log("speech_review_repair_queued id=" + item.Id + " reason=" + job.PrivacySourceMaterial);
                    return;
                }
                FailSpeechReview(job, wrongConsent ? SpeechConsentAuthorityFailure : invalid);
                return;
            }
            job.PrivacyDraft = json.ToString(Formatting.None); job.Kind = "oral_review_audit"; job.IsRunning = false; job.LlmMessages?.Clear();
            return;
        }
        if (json["approved"]?.Type != JTokenType.Boolean || !json["approved"].Value<bool>()
            || !AnimusForge.Refactor.Domain.WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject(job.PrivacyDraft, out var draft)
            || !ValidateSpeechExtraction(item, draft)) { FailSpeechReview(job, "speech_audit_rejected"); return; }
        if (!string.IsNullOrEmpty(item.ReviewWorldFingerprint) && item.ReviewWorldFingerprint !=
            MaterialHash(MaterialEvidence(_host.AllKingdomIds().Concat(new[] { item.Actor, item.PlayerKingdom }))))
        { FailSpeechReview(job, "condition_evidence_changed_during_review"); return; }
        item.Approved = draft.ToString(Formatting.None);
        item.WatchedKingdoms = (draft["condition_watch"] as JArray)?.Values<string>().Distinct().ToList() ?? new();
        var entries = (JArray)draft["commitments"];
        var currentEventCandidates = SpeechEventCandidates(item.Actor);
        try
        {
            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                var row = (JObject)entries[entryIndex];
                DialogueTagPayload.TryParse((string)row["payload"], out var parsed);
                // The reward owner applies the original tag independently of this async review.
                // Skipping only this action prevents a second signature or a technical hold;
                // mixed peace/trade promises and formal proposal replies still proceed.
                if (DialogueRoutingRules.UsesLegacyOralVassalage(parsed, item.Actor,
                    string.IsNullOrEmpty(item.PlayerRuler) ? "" : item.PlayerKingdom))
                {
                    _host.Log("speech review skipped original oral vassalage id=" + item.Id + " action=" + parsed.Action);
                    continue;
                }
                string key = JsonConvert.SerializeObject(parsed);
                if (item.AppliedPayloadKeys.Contains(key)) continue;
                var origin = new DialogueInteractionOrigin(item.Channel, item.Interaction, item.Session, item.PlayerText, item.NpcText,
                    row["player_agreed"].Value<bool>());
                item.Status = "applying";
                string eventId = ReviewedSpeechEvent(job, item, row, parsed, json, entryIndex, currentEventCandidates);
                SubmitReviewedOralCommitment(item.Ruler, item.Actor, (string)row["payload"], origin, out bool registered, eventId);
                if (!registered)
                { HoldSpeechReview(item, "accepted_statement_not_currently_representable"); RemoveJob(job.JobId); return; }
                item.AppliedPayloadKeys.Add(key); item.Applied = item.AppliedPayloadKeys.Count;
                item.Status = "pending";
            }
        }
        catch (Exception ex)
        { item.Status = "execution_uncertain"; item.Reason = ex.GetType().Name; RemoveJob(job.JobId); return; }
        RemoveJob(job.JobId);
        if (draft["unresolved"].Value<bool>()) { HoldSpeechReview(item, "unresolved_terms_or_condition"); return; }
        _reviewedSpeech.Add(item.SourceKey); Storage.ReviewedSpeechSources.Add(item.SourceKey);
        Storage.SpeechReviews.Remove(item); _speechReviews.Remove(item.Id);
    }
}
