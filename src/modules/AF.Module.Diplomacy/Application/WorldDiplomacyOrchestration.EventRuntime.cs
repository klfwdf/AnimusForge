using AnimusForge.Refactor.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    private bool TryReadNarrativeActors(JObject fact, HashSet<string> known, out List<string> heroes, out string reason)
    {
        heroes = new(); reason = "narrative_actor_identity_invalid";
        if (fact["hero_ids"] != null && fact["hero_ids"] is not JArray)
        { reason += ":hero_ids_must_be_array"; return false; }
        if (fact["hero_ids"] is JArray ids)
        {
            if (ids.Count > 16 || ids.Any(x => x.Type != JTokenType.String))
            { reason += ":hero_ids_require_at_most_16_string_ids"; return false; }
            var unknown = ids.Values<string>().Where(x => !known.Contains(x)).ToList();
            if (unknown.Count > 0)
            { reason += ":unknown_ids=" + string.Join(",", unknown) + ";use_narrative_actors_and_sources.hero_ids_not_display_names"; return false; }
            heroes = ids.Values<string>().Distinct(StringComparer.Ordinal).ToList();
        }
        // Collective accounts may have no named actor. An adopted personal
        // experience must bind to supplied identities before it becomes fact.
        if ((string)fact["kind"] == "adopted_npc_story" && heroes.Count == 0
            && (string)fact["actor_scope"] is not "collective" and not "unnamed")
        { reason += ":adopted_story_needs_actual_hero_ids_or_explicit_collective_or_unnamed_actor_scope"; return false; }
        if ((string)fact["kind"] == "adopted_npc_story" && heroes.Contains(_host.PartyRulerId(CurrentPlayerAuthority())))
        { reason += ":player_ruler_cannot_be_assigned_invented_conduct"; return false; }
        reason = ""; return true;
    }

    private static JArray BuildNarrativeActorAuditRequest(JObject draft)
    {
        var required = new JArray();
        if (draft?["narrative"] is not JArray facts) return required;
        for (int i = 0; i < Math.Min(16, facts.Count); i++)
            if (facts[i] is JObject fact && (string)fact["kind"] == "adopted_npc_story")
                required.Add(new JObject { ["index"] = i, ["digest"] = MaterialHash(fact.ToString(Formatting.None)) });
        return required;
    }

    private static bool ValidateNarrativeActorAudit(JObject draft, JObject audit, out string reason)
    {
        var required = BuildNarrativeActorAuditRequest(draft);
        reason = "narrative_actor_audit_incomplete:actor_review_must_match_exactly=" + required.ToString(Formatting.None)
            + ";empty_required_means_actor_review_must_be_empty;only_adopted_npc_story_requires_actor_rows;do_not_copy_narrative_safety_digest";
        if (required.Count == 0 && audit["actor_review"] == null) { reason = ""; return true; }
        if (audit["actor_review"] is not JArray reviews || reviews.Count != required.Count) return false;
        var expected = required.OfType<JObject>().ToDictionary(x => (int)x["index"], x => (string)x["digest"]);
        var seen = new HashSet<int>();
        foreach (var token in reviews)
        {
            if (token is not JObject review || review["index"]?.Type != JTokenType.Integer
                || review["complete"]?.Type != JTokenType.Boolean || !(bool)review["complete"]
                || review["hero_ids"] is not JArray ids || ids.Count > 16
                || ids.Any(x => x.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)x))) return false;
            if (!int.TryParse((string)review["index"], System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out int index)) return false;
            if (!seen.Add(index) || !expected.TryGetValue(index, out string digest)
                || review["digest"]?.Type != JTokenType.String || (string)review["digest"] != digest) return false;
            var actors = new HashSet<string>(ids.Values<string>(), StringComparer.Ordinal);
            if (actors.Count != ids.Count) return false;
            var fact = (JObject)draft["narrative"][index];
            if (fact["hero_ids"] != null && fact["hero_ids"] is not JArray) return false;
            var declared = fact["hero_ids"] as JArray ?? new JArray();
            if (declared.Any(x => x.Type != JTokenType.String) || !actors.SetEquals(declared.Values<string>()))
            { reason = "narrative_actor_audit_mismatch:" + JsonConvert.SerializeObject(new {
                index, declared, reviewed = actors
            }); return false; }
        }
        // The independent reviewer resolves names, aliases and ordinary words in
        // context. Code verifies complete, current-draft-bound identity coverage;
        // authoritative IDs and player-fiction restrictions are checked on adoption.
        reason = ""; return true;
    }

    private IReadOnlyDictionary<string,string> NarrativeActors(IEnumerable<string> kingdoms) =>
        (_host as IDiplomacyNarrativeActorHost)?.CaptureNarrativeActors(kingdoms.Distinct().ToList())
        ?? kingdoms.Distinct().Select(_host.PartyRulerId).Where(x => !string.IsNullOrEmpty(x)).Distinct()
            .ToDictionary(x => x, x => x, StringComparer.Ordinal);
    private IDiplomacyEventHost EventHost => _host as IDiplomacyEventHost;
    private WorldDiplomacyStorage _indexedEventStorage;
    private readonly Dictionary<string, DiplomacyEventAnnouncement> _eventAnnouncements = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DiplomacyEventEffectState> _eventEffects = new(StringComparer.Ordinal);
    private readonly List<DiplomacyEventEffectState> _activeEventEffects = new();
    private readonly Queue<string> _pendingEventNotices = new();
    private long _nextEventRuntimeUtc;
    private int _eventEffectsDay = -1;
    private bool _committingEventDocuments;

    private void EnsureEventRuntimeIndex()
    {
        if (ReferenceEquals(_indexedEventStorage, Storage)) return;
        _indexedEventStorage = Storage;
        _eventAnnouncements.Clear(); _eventEffects.Clear(); _activeEventEffects.Clear(); _pendingEventNotices.Clear();
        Storage.EventAnnouncements ??= new(); Storage.EventEffects ??= new(); Storage.RecentEventImages ??= new();
        foreach (var item in Storage.EventAnnouncements.Where(x => x != null && !string.IsNullOrEmpty(x.Id)))
        {
            item.EffectIds ??= new(); item.RemovedEffectIds ??= new(); item.ConfirmedDocumentIds ??= new(); item.Narrative ??= new();
            item.ProgressEvidenceIds ??= new();
            _eventAnnouncements[item.Id] = item;
            if (item.State == "public" && !item.Acknowledged) _pendingEventNotices.Enqueue(item.Id);
        }
        foreach (var effect in Storage.EventEffects.Where(x => x != null && !string.IsNullOrEmpty(x.Id)))
        {
            _eventEffects[effect.Id] = effect;
            if (effect.Status == "applying") effect.Status = "uncertain";
            if (effect.Status == "active") _activeEventEffects.Add(effect);
        }
        foreach (var notice in Storage.EventAnnouncements.Where(x => x?.State == "public" && !x.CanonicalHistoryRecorded)) ArchiveEventBoundary(notice);
        _eventEffectsDay = -1; _nextEventRuntimeUtc = 0;
    }

    internal void AdvanceEventRuntime()
    {
        if (!_persistenceHealthy || EventHost == null) return;
        EnsureEventRuntimeIndex();
        long now = DateTime.UtcNow.Ticks;
        if (now < _nextEventRuntimeUtc) return;
        _nextEventRuntimeUtc = now + TimeSpan.TicksPerSecond;
        int day = _host.CurrentDay();
        if (day != _eventEffectsDay)
        {
            _eventEffectsDay = day;
            // Only live effects, once per game day. No archive scan, backdated
            // catch-up, policy runner or expiry reset when diplomacy is disabled.
            foreach (var effect in _activeEventEffects) DiplomacyEventEffects.Pulse(effect, day, EventHost);
            _activeEventEffects.RemoveAll(x => x.Status != "active");
        }
        if (!_host.WorldDiplomacyEnabled()) return;
        ResumeDiplomacyEvents();
        while (_pendingEventNotices.Count > 0)
        {
            string id = _pendingEventNotices.Peek();
            if (!_eventAnnouncements.TryGetValue(id, out var notice) || notice.Acknowledged || notice.State != "public")
            { _pendingEventNotices.Dequeue(); continue; }
            var captured = Storage;
            var effects = notice.EffectIds.Where(_eventEffects.ContainsKey).Select(x => _eventEffects[x])
                .Where(x => Math.Abs(x.AppliedTotal) > 0.0001f).Select(x => new DiplomacyEventEffect(DescribeEventEffect(x),
                    x.AppliedTotal > 0 ? DiplomacyEventEffectTone.Beneficial : DiplomacyEventEffectTone.Harmful)).ToList();
            foreach (var removed in notice.RemovedEffectIds.Where(_eventEffects.ContainsKey))
                effects.Add(new DiplomacyEventEffect(DiplomacyUiLocalization.Format("AF_DIP_UI_EventEffect_Removed", "Ended: {EFFECT}", ("EFFECT", DescribeEventEffect(_eventEffects[removed])))));
            foreach (var document in notice.ConfirmedDocumentIds.Select(ResolveDocument).Where(x => x?.ChangedDiplomaticState == true))
                effects.Add(new DiplomacyEventEffect(document.MechanicalResult));
            if (Enum.TryParse(notice.Scene, out DiplomacyEventScene scene) == false) scene = DiplomacyEventScene.Unspecified;
            var snapshot = new DiplomacyEventPresentation(notice.EventId, EventHost.EventGeneration,
                notice.Stage == "started" ? DiplomacyEventStage.Started : DiplomacyEventStage.Ended,
                notice.Title, notice.Body, notice.ParticipantIds.Select(_host.PartyNameOrEmpty), effects, scene,
                cultureId: (_host as IDiplomacyEventCultureHost)?.EventCulture(ResolveRound(notice.EventId)?.InitiatorKingdomId) ?? "",
                selectedImageId: notice.ImageId);
            if (EventHost.ShowEvent(snapshot, out string image, () => {
                    if (ReferenceEquals(Storage, captured)) notice.Acknowledged = true;
                }, Storage.RecentEventImages))
            {
                notice.ImageId = image ?? "";
                if (!string.IsNullOrEmpty(image))
                {
                    Storage.RecentEventImages.Remove(image); Storage.RecentEventImages.Add(image);
                    if (Storage.RecentEventImages.Count > 6) Storage.RecentEventImages.RemoveAt(0);
                }
            }
            return;
        }
    }

    private string DescribeEventEffect(DiplomacyEventEffectState effect) => DiplomacyUiLocalization.Format(
        effect.Kind == "ruler_relation" ? "AF_DIP_UI_EventEffect_InstantDescription" : "AF_DIP_UI_EventEffect_Description",
        effect.Kind == "ruler_relation" ? "{TARGET}: {KIND} {TOTAL}. {REASON}"
            : "{TARGET}: {KIND}; applied {TOTAL}; scheduled {DELTA} per day, ending on {DAY}. {REASON}",
        ("TARGET", effect.Kind == "ruler_relation" ? _host.PartyNameOrEmpty(effect.KingdomId) + " / " + _host.PartyNameOrEmpty(effect.OtherKingdomId)
            : _host.SettlementValidationName(effect.SettlementId)),
        ("KIND", DiplomacyUiLocalization.Text("AF_DIP_UI_EventEffect_" + effect.Kind, effect.Kind switch {
            "ruler_relation" => "ruler relations", "town_loyalty" => "loyalty", "town_security" => "security",
            "town_prosperity" => "prosperity", "town_food" => "food stocks", _ => effect.Kind })),
        ("DELTA", effect.Delta.ToString("+0.##;-0.##;0", System.Globalization.CultureInfo.InvariantCulture)),
        ("TOTAL", effect.AppliedTotal.ToString("+0.##;-0.##;0", System.Globalization.CultureInfo.InvariantCulture)),
        ("DAY", (EventHost as IDiplomacyEventCalendarHost)?.FormatEventDate(effect.ExpiresDay) ?? _host.FormatCampaignDate(effect.ExpiresDay)), ("REASON", effect.Reason));

    private bool EventBoundaryComplete(WorldDiplomacyRound round, string stage)
    {
        // A carried reply keeps its original public source; it is not a new
        // political event requiring a fabricated opening or ending effect.
        if (round?.EventSourceType == "player_followup" && ResolveDocument(round.RootDocumentId)?.IsReadyForPublication == true) return true;
        // Existing public legacy negotiations already began. Never fabricate a
        // retrospective opening or replay an old action to manufacture its effect.
        if (stage == "started" && round?.EventOpeningOrigin == "legacy_public_event"
            && ResolveDocument(round.RootDocumentId)?.IsReadyForPublication == true) return true;
        EnsureEventRuntimeIndex();
        string id = stage == "started" ? round?.EventBeginningId : round?.EventEndingId;
        return !string.IsNullOrEmpty(id) && _eventAnnouncements.TryGetValue(id, out var notice) && notice.State == "public";
    }

    private bool EnsureEventBoundary(WorldDiplomacyRound round, string stage)
    {
        if (EventHost == null || round == null || round.EventSystemVersion == 0) return true;
        if (EventBoundaryComplete(round, stage)) return true;
        if (!_host.WorldDiplomacyEnabled() || !IsLiveRound(round)) return false;
        if (stage == "started" && !EnsurePlayerOpeningPlan(round)) return false;
        if (stage == "started" && !EnsureOralAdmissionPlan(round)) return false;
        if (Storage.Jobs.Any(x => x.RoundId == round.RoundId && x.Kind.StartsWith("event_boundary", StringComparison.Ordinal))) return false;
        var job = new WorldDiplomacyJob { JobId = _host.NewId("event_boundary"), Kind = "event_boundary", RoundId = round.RoundId,
            ExchangeId = round.RoundId, AuthorKingdomId = round.InitiatorKingdomId, ForcedIntent = stage,
            RequestLane = round.BirthLane, CreatedDay = _host.CurrentDay(), Priority = 110, MaxTokens = _host.AnalysisMaxTokens() };
        // Build the model context at dispatch, not inside a game action callback.
        EnqueueJob(job);
        return false;
    }

    private void PrepareEventBoundaryRequest(WorldDiplomacyJob job)
    {
        var round = ResolveRound(job.RoundId); if (round == null || EventHost == null) return;
        bool ending = job.ForcedIntent == "ended";
        if (ending) job.EndingContextVersion = 2;
        if (!CheckOpeningAdmission(job, round)) return;
        var participants = round.Participants
            .Select(x => x.KingdomId).Append(round.InitiatorKingdomId).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        if (!ending && !EnsureBoundaryWorldBook(job, round, participants)) return;
        if (IsPlayerDeclarationOpening(round)) job.EventMaterialSnapshot = ending ? "[]" : ReadEventLore(round).ToString(Formatting.None);
        var evidence = BuildEventRequestEvidence(round, ending);
        EnsureEventRuntimeIndex();
        if (job.Kind == "event_boundary_audit" && job.RoundConversationRevision != round.ConversationRevision)
        { job.Kind = "event_boundary"; job.PrivacyDraft = ""; job.PrivacySourceMaterial = ""; }
        job.SystemPrompt = DiplomacyPromptTexts.Get(job.Kind == "event_boundary_audit" ? "EventBoundaryAudit" : "EventBoundaryWriter", "");
        job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("EventEffectPayloadContract", "");
        if (job.ForcedIntent == "ended") job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("EventEndingContract", "");
        job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("SovereignPositionContract", "");
        job.PositionOwnerReviewVersion = 1;
        if (job.Kind == "event_boundary_audit" && job.ForcedIntent == "started" && !HasEventFoundation(round))
            job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("PositionOwnerReview", "");
        if (job.Kind == "event_boundary_audit") job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("EventBoundaryAuditOutput", "");
        if (job.Kind == "event_boundary" && !string.IsNullOrEmpty(job.PrivacySourceMaterial))
            job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("EventBoundaryRepair", "");
        job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("EventNarrativeSafety", "");
        if (!ending && IsPlayerDeclarationOpening(round)) job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("PlayerWorldBookEvidence", "");
        if (ending) job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("EventEndingEvidence", "");
        job.UserPrompt = JsonConvert.SerializeObject(new {
            stage = job.ForcedIntent, needs_foundation = job.ForcedIntent == "started" && !HasEventFoundation(round), round.RoundId, round.RoundTopic, round.EventSourceType, round.EventOpeningOrigin,
            round.EventBackground, round.EventNarrative, round.EventOutcome, round.EventOutcomeReason,
            ending_decision = round.EventConvergence,
            positions = EventPositionsForModel(round),
            frozen_admission = string.IsNullOrEmpty(round.CountryAdmission) ? null : ReadAdmission(round.CountryAdmission)?.Admitted,
            source_material = ending ? "" : round.ExternalOpeningContext,
            world_book_sources = ending ? new JArray() : ReadEventLore(round),
            setting_and_known_history = ending || IsPlayerDeclarationOpening(round) ? "" : BuildGenerationPromptForJob(round.InitiatorKingdomId,
                participants.FirstOrDefault(x => x != round.InitiatorKingdomId), null, false, null, false, round.RoundId,
                false, participants, false),
            pending_declarations = Storage.Documents.Where(x => x.RoundId == round.RoundId && x.EventExecutionPending)
                .Select(x => new { x.AuthorKingdomId, x.TargetKingdomId, x.Title, x.Body, x.Intent, x.TreatyTerms }),
            world = ending ? "" : CompactPlanningWorld(EventHost.CaptureEventWorld(participants), 6000),
            narrative_actors = NarrativeActors(participants),
            participants, sovereign_speakers = participants.Select(x => new { kingdom = x, ruler_hero_id = _host.PartyRulerId(x) }),
            public_documents = evidence["documents"], exact_commitments = evidence["commitments"],
            pending_offers = evidence["pending_offers"], history_selection = evidence["history_selection"],
            prior_public_boundaries = Storage.EventAnnouncements.Where(x => x.EventId == round.RoundId && x.State == "public"),
            live_effects = _activeEventEffects.Where(x => x.EventId == round.RoundId),
            // Instant effects have already left live_effects. The conclusion
            // must still see their receipts rather than infer no change.
            applied_effect_receipts = Storage.EventEffects.Where(x => x.EventId == round.RoundId)
                .Select(x => new { x.Id, x.Kind, x.KingdomId, x.OtherKingdomId, x.SettlementId, x.AppliedTotal, x.Status, x.Reason }),
            proposed_boundary = job.Kind == "event_boundary_audit" ? job.PrivacyDraft : "",
            narrative_safety_review_required = NarrativeSafetyRequest(job, evidence),
            actor_review_required = job.Kind == "event_boundary_audit"
                ? BuildNarrativeActorAuditRequest(WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(job.PrivacyDraft)) : new JArray(),
            position_review_required = job.Kind == "event_boundary_audit" && job.ForcedIntent == "started" && !HasEventFoundation(round)
                ? BuildPositionOwnerReview(WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(job.PrivacyDraft)?["positions"] as JArray) : new JArray(),
            validation_error = job.PrivacySourceMaterial,
            rejected_boundary = job.Kind == "event_boundary" ? job.PrivacyDraft : "",
            required_foundation_participants = round.Participants.Where(x => x.State != "observer" && !_host.IsPlayerParty(x.KingdomId)).Select(x => x.KingdomId)
        });
        if (ending)
        {
            // The conclusion cites published event evidence, not newly fetched
            // world facts. Remove duplicate bodies; preserve original records.
            var request = JObject.Parse(job.UserPrompt);
            if (request["narrative_safety_review_required"] is JObject safety)
            {
                safety["evidence_mode"] = "event_records";
                foreach (string field in new[] { "player_documents", "published_npc_documents" })
                    foreach (var row in (safety[field] as JArray ?? new()).OfType<JObject>()) row.Remove("Body");
            }
            request["ending_evidence_contract"] = "public_documents_contains_labeled_excerpts_and_exact_action_receipts;no_new_world_facts;confirmed_documents_only_executed_receipts";
            job.UserPrompt = request.ToString(Formatting.None);
        }
        job.RoundConversationRevision = round.ConversationRevision;
        job.CourtKnowledgeRevision = DiplomacyEventRules.KnowledgeRevision(round, job.AuthorKingdomId);
        job.PersonalMemoryRulerId = _host.PartyRulerId(job.AuthorKingdomId);
        job.LlmMessages?.Clear();
    }

    private bool CompleteEventBoundary(WorldDiplomacyJob job, JObject json)
    {
        if (job.Kind != "event_boundary" && job.Kind != "event_boundary_audit") return false;
        var round = ResolveRound(job.RoundId);
        if (!IsLiveRound(round)) { RemoveJob(job.JobId); return true; }
        if (!CheckOpeningAdmission(job, round)) return true;
        if (job.Kind == "event_boundary")
        {
            job.PrivacyDraft = FreezeEventDraft(json);
            // A missing foundation cannot pass even with model approval. Check
            // without mutating the event before spending an independent audit.
            ProjectEventPublicIdentities(round, json);
            job.PrivacyDraft = FreezeEventDraft(json);
            if (!ValidateUnansweredPlayerNarration(round, json, out string narrationError))
            { RetryEventBoundary(job, narrationError); return true; }
            if (!TryReadOpeningFoundation(round, job.ForcedIntent, json, out _, out _, out string invalid))
            { RetryEventBoundary(job, invalid); return true; }
            if (job.ForcedIntent == "started" && !ValidateBoundaryExecutionTargetsCore(round, json, out string targetError, true))
            { RetryEventBoundary(job, targetError); return true; }
            job.PrivacyDraft = FreezeEventDraft(json);
            if (!ValidateBoundaryEffectRequirement(round, job.ForcedIntent, json, out invalid))
            { RetryEventBoundary(job, invalid); return true; }
            if (!ValidatePlayerOpeningNarrative(round, job.ForcedIntent, json, out invalid))
            { RetryEventBoundary(job, invalid); return true; }
            if (!Storage.EventAnnouncements.Any(x => x.Id == round.RoundId + ":" + job.ForcedIntent)
                && !ValidateBoundaryExecutionTargets(round, json, out invalid))
            { RetryEventBoundary(job, invalid); return true; }
            job.Kind = "event_boundary_audit";
            job.PrivacyAuditRetries = 0; job.PrivacySourceMaterial = "";
            job.IsRunning = false; job.LlmMessages?.Clear(); return true;
        }
        if (json["approved"]?.Type != JTokenType.Boolean)
        { RetryEventBoundaryAudit(job, "approved_boolean_missing"); return true; }
        if (!json["approved"].Value<bool>())
        { RetryEventBoundary(job, "boundary_audit_rejected:" + EventAuditFeedback(json)); return true; }
        if (!WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject(job.PrivacyDraft, out var draft))
        { RetryEventBoundary(job, "boundary_draft_missing"); return true; }
        if (job.PositionOwnerReviewVersion >= 1 && job.ForcedIntent == "started" && !HasEventFoundation(round)
            && !ValidatePositionOwnerReview(draft["positions"] as JArray, json, out string ownerError, out bool rejectedOwner))
        {
            if (rejectedOwner) RetryEventBoundary(job, ownerError); else RetryEventBoundaryAudit(job, ownerError);
            return true;
        }
        if (!ValidateNarrativeSafety(job, draft, json, out string reason, out bool rejectedSafety))
        {
            if (rejectedSafety) RetryEventBoundary(job, reason); else RetryEventBoundaryAudit(job, reason);
            return true;
        }
        if (!ValidateNarrativeActorAudit(draft, json, out reason))
        {
            if (reason.StartsWith("narrative_actor_audit_mismatch", StringComparison.Ordinal)) RetryEventBoundary(job, reason);
            else RetryEventBoundaryAudit(job, reason);
            return true;
        }
        // Recheck after the asynchronous audit. If only clamped effects changed,
        // keep the prose and valid effects and audit the exact reduced payload.
        int effectCount = (draft["effects"] as JArray)?.Count ?? -1;
        if (job.ForcedIntent == "started" && !Storage.EventAnnouncements.Any(x => x.Id == round.RoundId + ":started"))
        {
            if (!ValidateBoundaryExecutionTargetsCore(round, draft, out reason, true))
            { RetryEventBoundary(job, reason); return true; }
            if (effectCount != (draft["effects"] as JArray)?.Count)
            {
                job.PrivacyDraft = FreezeEventDraft(draft); job.IsRunning = false; job.LlmMessages?.Clear();
                job.PrivacyAuditRetries = 0; return true;
            }
        }
        if (!TryPublishEventBoundary(round, job.ForcedIntent, draft, out reason))
        { RetryEventBoundary(job, reason); return true; }
        RemoveJob(job.JobId);
        if (job.ForcedIntent == "ended") CloseRound(round.EventCloseReason, round);
        else StartEventDiscussion(round);
        return true;
    }

    private void RetryEventBoundary(WorldDiplomacyJob job, string reason)
    {
        job.IsRunning = false; job.SemanticRepairAttempts++;
        // These persisted pipeline fields retain a rejected draft and a stable
        // diagnostic, never approval. The existing finite retry budget applies.
        job.Kind = "event_boundary"; job.PrivacySourceMaterial = reason; job.LlmMessages?.Clear();
        // A rejected, unexecuted payload gets a bounded immediate correction;
        // transport failures and exhausted budgets retain technical backoff.
        job.RetryAfterUtcTicks = 0;
        if (job.SemanticRepairAttempts >= 3) HoldTechnicalRequest(job, reason);
        _host.Log("event_boundary_retry event=" + job.RoundId + " reason=" + reason);
    }

    private bool ValidateBoundaryExecutionTargets(WorldDiplomacyRound round, JObject draft, out string reason)
        => ValidateBoundaryExecutionTargetsCore(round, draft, out reason, false);

    private bool ValidateBoundaryExecutionTargetsCore(WorldDiplomacyRound round, JObject draft, out string reason, bool omitClamped)
    {
        reason = "boundary_effects_invalid:effects_requires_array_at_most_8_objects";
        if (draft["effects"] is not JArray rows || rows.Count > 8) return false;
        var parties = new HashSet<string>(round.Participants.Select(x => x.KingdomId).Append(round.InitiatorKingdomId));
        var failures = new List<string>(); var clamped = new List<JToken>(); int day = _host.CurrentDay();
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i] is not JObject row)
            { failures.Add("effects[" + i + "]:object_required"); continue; }
            if (row["delta"]?.Type is not JTokenType.Integer and not JTokenType.Float)
            { failures.Add("effects[" + i + "].delta:number_required;use_delta_not_value;use_kind_not_type;use_other_not_target"); continue; }
            if (row["days"]?.Type != JTokenType.Integer)
            { failures.Add("effects[" + i + "].days:integer_required;use_0_for_ruler_relation"); continue; }
            foreach (string field in new[] { "kind", "kingdom", "other", "settlement", "reason" })
                if (row[field] != null && row[field].Type != JTokenType.String)
                { reason = "boundary_effect_field_invalid:effects[" + i + "]." + field + ":string_required"; return false; }
            if (!int.TryParse((string)row["days"], out int days) || days < 0 || days > 21)
            { failures.Add("effects[" + i + "]:days_out_of_range_0_to_21"); continue; }
            var effect = new DiplomacyEventEffectState { Id = "preflight:" + i, EventId = round.RoundId,
                Kind = (string)row["kind"] ?? "", KingdomId = (string)row["kingdom"] ?? "",
                OtherKingdomId = (string)row["other"] ?? "", SettlementId = (string)row["settlement"] ?? "",
                Delta = (float)row["delta"], Reason = (string)row["reason"] ?? "", StartedDay = day, ExpiresDay = day + days };
            effect.RulerId = _host.PartyRulerId(effect.KingdomId); effect.OtherRulerId = _host.PartyRulerId(effect.OtherKingdomId);
            if (!DiplomacyEventEffects.Validate(effect, parties, day, out string error))
            { failures.Add("effects[" + i + "]:" + error); continue; }
            if (!EventHost.TryReadEventEffect(effect, out float value, out float minimum, out float maximum))
            {
                var allowed = (_host as IDiplomacySilenceEffectHost)?.KingdomTownIds(effect.KingdomId) ?? Array.Empty<string>();
                failures.Add("effects[" + i + "]:target_invalid:kingdom=" + effect.KingdomId + ";settlement=" + effect.SettlementId
                    + ";settlement_requires_exact_world_towns_id_not_name;allowed_ids=" + string.Join(",", allowed.Take(16)));
            }
            else if (Math.Abs(Math.Max(minimum, Math.Min(maximum, value + effect.Delta)) - value) < 0.0001f)
            {
                if (omitClamped) clamped.Add(row);
                else failures.Add("effects[" + i + "]:target_already_at_bound;omit_unavailable_change;opening_and_ending_may_have_empty_effects");
            }
        }
        if (draft["confirmed_documents"] is JArray docs)
        {
            if (docs.Count > 16) failures.Add("confirmed_documents:at_most_16_ids");
            foreach (var id in docs)
                if (id.Type != JTokenType.String || ResolveDocument((string)id) is not WorldDiplomacyDocument doc
                    || !doc.IsReadyForPublication || doc.RoundId != round.RoundId || !doc.ChangedDiplomaticState)
                    failures.Add("confirmed_documents:unexecuted_or_invalid=" + id + ";statements_are_not_mechanical_results;use_empty_array_when_none");
        }
        else if (draft["confirmed_documents"] != null) failures.Add("confirmed_documents:array_required");
        reason = failures.Count == 0 ? "" : "boundary_execution_preflight:" + string.Join("|", failures);
        if (failures.Count == 0 && clamped.Count > 0)
        {
            foreach (var row in clamped) row.Remove();
            _host.Log("diplomacy.opening_clamped_effects_omitted event=" + round.RoundId + " count=" + clamped.Count);
        }
        return failures.Count == 0;
    }

    private void RetryEventBoundaryAudit(WorldDiplomacyJob job, string reason)
    {
        job.PrivacySourceMaterial = "boundary_audit_invalid:" + reason;
        job.IsRunning = false; job.LlmMessages?.Clear(); job.TransportRetryAttempts = 0;
        // Preserve the independently written draft. Bad review syntax is not a
        // political or narrative failure; retries use the existing saved budget.
        if (job.PrivacyAuditRetries++ >= 2) HoldTechnicalRequest(job, job.PrivacySourceMaterial);
        _host.Log("diplomacy.boundary_audit_repair job=" + job.JobId + " reason=" + reason);
    }

    private static bool TryReadNarrativeSubject(JObject row, out string subject, out string reason)
    {
        subject = ""; reason = "narrative_subject_must_be_string";
        var canonical = row["subject"]; var alias = row["subject_kingdom_id"];
        if (canonical != null && canonical.Type != JTokenType.String || alias != null && alias.Type != JTokenType.String) return false;
        if (canonical != null && alias != null && (string)canonical != (string)alias)
        { reason = "narrative_subject_fields_conflict"; return false; }
        subject = (string)(canonical ?? alias) ?? "";
        reason = ""; return true;
    }

    private bool ValidateBoundaryEffectRequirement(WorldDiplomacyRound round, string stage, JObject draft, out string reason)
    {
        reason = "";
        if (stage == "started") return true; // A grounded dispute need not change a numeric stat.
        if (stage == "ended" && round.EventPhase == "closing" && EventBoundaryComplete(round, "started")) return true;
        if ((draft["effects"] as JArray)?.Count > 0 || (draft["remove_effects"] as JArray)?.Count > 0
            || !stage.StartsWith("progress:", StringComparison.Ordinal) && (draft["confirmed_documents"] as JArray)?.Count > 0) return true;
        reason = "event_boundary_requires_grounded_effect:stage=" + stage
            + ";effects_remove_effects_and_confirmed_documents_are_all_empty;progress_requires_actual_change;grounded_openings_and_actual_endings_may_be_neutral";
        return false;
    }

    private bool TryPublishEventBoundary(WorldDiplomacyRound round, string stage, JObject draft, out string reason)
        => TryPublishEventBoundaryCore(round, stage, draft, out reason, false);

    private bool TryPublishEventBoundaryCore(WorldDiplomacyRound round, string stage, JObject draft, out string reason, bool validateOnly)
    {
        reason = "boundary_invalid"; EnsureEventRuntimeIndex();
        bool progress = stage.StartsWith("progress:", StringComparison.Ordinal);
        if (stage != "started" && stage != "ended" && !progress) return false;
        if (!progress && EventBoundaryComplete(round, stage)) return true;
        if (stage == "ended" && IsEnding(round) && (HasUnfinishedEndingObligations(round)
            || !string.IsNullOrEmpty(round.EventConvergence.TechnicalFailure)))
        { reason = "event_ending_original_obligation_unfinished"; return false; }
        if (!TryReadOpeningFoundation(round, stage, draft, out string background, out var positions, out reason)) return false;
        reason = "boundary_invalid";
        string boundaryId = round.RoundId + ":" + stage;
        // A staged receipt freezes the entire accepted payload across retries.
        // Never reroll targets after any side effect has been attempted.
        if (_eventAnnouncements.TryGetValue(boundaryId, out var staged))
        {
            if (validateOnly) { reason = ""; return true; }
            ApplyOpeningFoundation(round, background, positions);
            if (positions != null)
            {
                round.EventNarrative = staged.Narrative;
                staged.ConceptFingerprint = EventConceptFingerprint(background, round.Participants.Select(x => x.KingdomId));
            }
            return staged.State is "public" or "applied" || PublishStagedEventBoundary(round, staged, out reason);
        }
        if (!ValidateBoundaryExecutionTargets(round, draft, out reason)) return false;
        if (!ValidateUnansweredPlayerNarration(round, draft, out reason)) return false;
        if (!ValidateBoundaryEffectRequirement(round, stage, draft, out reason)) return false;
        if (!ValidatePlayerOpeningNarrative(round, stage, draft, out reason)) return false;
        string title = draft["title"]?.Type == JTokenType.String ? draft["title"].Value<string>() : "";
        string body = draft["body"]?.Type == JTokenType.String ? draft["body"].Value<string>() : "";
        if (string.IsNullOrWhiteSpace(title) || title.Length > 100 || string.IsNullOrWhiteSpace(body) || body.Length > 6000
            || draft["effects"] is not JArray rows || rows.Count > 8) return false;
        // A declaration can affect its recipient without securing their consent
        // to participate. Effect scope and membership are distinct.
        var participants = new HashSet<string>(round.Participants
            .Select(x => x.KingdomId).Append(round.InitiatorKingdomId), StringComparer.Ordinal);
        string id = round.RoundId + ":" + stage;
        var effects = new List<DiplomacyEventEffectState>(); int index = 0, day = _host.CurrentDay();
        foreach (var row in rows.OfType<JObject>())
        {
            if (row["delta"]?.Type is not JTokenType.Integer and not JTokenType.Float || row["days"]?.Type != JTokenType.Integer) return false;
            var effect = new DiplomacyEventEffectState { Id = id + ":" + index++, EventId = round.RoundId, Stage = stage,
                Kind = (string)row["kind"] ?? "", KingdomId = (string)row["kingdom"] ?? "", OtherKingdomId = (string)row["other"] ?? "",
                SettlementId = (string)row["settlement"] ?? "", Delta = row["delta"].Value<float>(), Reason = (string)row["reason"] ?? "",
                StartedDay = day, ExpiresDay = day + row["days"].Value<int>() };
            effect.RulerId = _host.PartyRulerId(effect.KingdomId); effect.OtherRulerId = _host.PartyRulerId(effect.OtherKingdomId);
            if (!DiplomacyEventEffects.Validate(effect, participants, day, out reason)) return false;
            if (!EventHost.TryReadEventEffect(effect, out _, out _, out _))
            { reason = "effect_target_unavailable:" + effect.Kind + ":" + effect.KingdomId + ":" + effect.SettlementId; return false; }
            effects.Add(effect);
        }
        if (effects.Count != rows.Count) return false;
        var confirmed = (draft["confirmed_documents"] as JArray)?.Values<string>().ToList() ?? new();
        if (confirmed.Count > 16 || confirmed.Any(x => ResolveDocument(x)?.ChangedDiplomaticState != true
            || ResolveDocument(x).RoundId != round.RoundId || !ResolveDocument(x).IsReadyForPublication)) return false;
        if (stage == "started" && confirmed.Count > 0) return false;
        var evidence = (draft["progress_evidence"] as JArray)?.Values<string>().Distinct().ToList() ?? new();
        if (evidence.Count > 16 || evidence.Any(x => !(ResolveDocument(x) is { IsReadyForPublication: true } doc && doc.RoundId == round.RoundId)
            && !round.EventNarrative.Any(f => f.Id == x && f.Public))) return false;
        if (progress && confirmed.Count == 0 && evidence.Count == 0) return false;
        var removals = (draft["remove_effects"] as JArray)?.Values<string>().ToList() ?? new();
        if (removals.Any(x => !_eventEffects.TryGetValue(x, out var e) || e.EventId != round.RoundId || e.Stage == "silence" || e.Status != "active")) return false;
        if (progress)
        {
            var used = new HashSet<string>(Storage.EventAnnouncements.Where(x => x.EventId == round.RoundId
                && x.Stage.StartsWith("progress:", StringComparison.Ordinal))
                .SelectMany(x => (x.ProgressEvidenceIds ?? new()).Concat(x.ConfirmedDocumentIds ?? new())), StringComparer.Ordinal);
            if (!evidence.Concat(confirmed).Any(x => !used.Contains(x))) { reason = "progress_evidence_already_applied"; return false; }
            // Executed actions remain true before propagation. Any additional
            // progress effect justified by their declaration still needs informed
            // targets, just like other negotiation evidence; never replay the action.
            foreach (string sourceId in evidence.Concat(confirmed).Distinct(StringComparer.Ordinal))
            {
                var doc = ResolveDocument(sourceId);
                if (doc != null && effects.Concat(removals.Select(x => _eventEffects[x])).Any(e => !DialogueDocumentKnown(e.KingdomId, sourceId)
                    || !string.IsNullOrEmpty(e.OtherKingdomId) && !DialogueDocumentKnown(e.OtherKingdomId, sourceId)))
                { reason = "progress_evidence_not_delivered"; return false; }
            }
        }
        if (progress && effects.Count == 0 && removals.Count == 0)
        { reason = "event_progress_effect_update_requires_actual_effect_or_removal"; return false; }
        bool neutralEnding = stage == "started" || stage == "ended" && round.EventPhase == "closing" && EventBoundaryComplete(round, "started");
        if (!neutralEnding && effects.Count == 0 && removals.Count == 0 && confirmed.Count == 0)
        { reason = "event_boundary_requires_grounded_effect"; return false; }
        bool canChange = effects.Any(e => EventHost.TryReadEventEffect(e, out float v, out float lo, out float hi)
            && Math.Abs(Math.Max(lo, Math.Min(hi, v + e.Delta)) - v) > 0.0001f);
        if (!neutralEnding && !canChange && confirmed.Count == 0 && removals.Count == 0) { reason = "boundary_effects_already_clamped"; return false; }
        var narrative = new List<DiplomacyEventNarrativeFact>();
        var actors = NarrativeActors(participants);
        var knownActors = new HashSet<string>(actors.Keys, StringComparer.Ordinal);
        foreach (var hero in round.EventNarrative.SelectMany(x => x.HeroIds ?? new())) knownActors.Add(hero);
        if (draft["narrative"] is not JArray facts || facts.Count > 16)
        { reason = "boundary_narrative_invalid:narrative_requires_array_at_most_16_objects_not_string"; return false; }
        foreach (var row in facts.OfType<JObject>())
        {
            if (!TryReadNarrativeSubject(row, out string subject, out reason)) return false;
            reason = "boundary_narrative_invalid:requires_kind_participant_subject_source_text;no_player_fiction";
            if (row["kind"]?.Type != JTokenType.String || row["source"]?.Type != JTokenType.String || row["text"]?.Type != JTokenType.String) return false;
            string kind = (string)row["kind"], source = (string)row["source"], text = (string)row["text"];
            if (kind is not "public_fact" and not "allegation" and not "adopted_npc_story" || string.IsNullOrWhiteSpace(source)
                || source.Length > 600 || string.IsNullOrWhiteSpace(text) || text.Length > 1600
                || (!string.IsNullOrEmpty(subject) && !participants.Contains(subject))
                || (kind == "adopted_npc_story" && (string.IsNullOrEmpty(subject) || _host.IsPlayerParty(subject)))) return false;
            if (!TryReadNarrativeActors(row, knownActors, out var heroes, out reason)) return false;
            if (!ValidatePlayerNarrativeSource(row, out reason)) return false;
            narrative.Add(new DiplomacyEventNarrativeFact { Id = id + ":fact:" + narrative.Count, Kind = kind,
                SubjectKingdomId = subject, Source = source, Text = text, Public = true, HeroIds = heroes });
        }
        if (narrative.Count != facts.Count) { reason = "boundary_narrative_rows_must_be_objects"; return false; }
        // The approved opening experiences are authoritative publication input.
        // A later presentation rewrite cannot silently omit the actors' memories.
        if (stage == "started" && round.EventNarrative.Count > 0)
            narrative = new List<DiplomacyEventNarrativeFact>(round.EventNarrative);
        if (validateOnly) { reason = ""; return true; }
        ApplyOpeningFoundation(round, background, positions);
        if (positions != null) round.EventNarrative = narrative;
        var notice = new DiplomacyEventAnnouncement { Id = id, EventId = round.RoundId, Stage = stage, Title = stage == "started" ? title : round.RoundTopic,
            Theme = stage == "started" ? round.EventTheme : "",
            Body = body, Day = day, Scene = (string)draft["scene"] ?? "Negotiation", State = "pending",
            ParticipantIds = participants.ToList(), EffectIds = effects.Select(x => x.Id).ToList(), ConfirmedDocumentIds = confirmed,
            ProgressEvidenceIds = evidence,
            RemovedEffectIds = removals, Narrative = narrative,
            MaterialKeys = stage == "started" ? new List<string>(round.EventMaterialKeys ?? new()) : new(),
            ConceptFingerprint = stage == "started" ? EventConceptFingerprint(round.EventBackground, round.Participants.Select(x => x.KingdomId)) : "" };
        Storage.EventAnnouncements.Add(notice); _eventAnnouncements[id] = notice;
        foreach (var candidate in effects)
        {
            Storage.EventEffects.Add(candidate); _eventEffects.Add(candidate.Id, candidate);
        }
        return PublishStagedEventBoundary(round, notice, out reason);
    }

    private bool PublishStagedEventBoundary(WorldDiplomacyRound round, DiplomacyEventAnnouncement notice, out string reason)
    {
        bool changed = false;
        foreach (string effectId in notice.EffectIds)
        {
            if (!_eventEffects.TryGetValue(effectId, out var effect)) { reason = "boundary_receipt_missing"; return false; }
            changed |= DiplomacyEventEffects.Pulse(effect, _host.CurrentDay(), EventHost) || Math.Abs(effect.AppliedTotal) > 0.0001f;
            if (effect.Status == "active" && !_activeEventEffects.Contains(effect)) _activeEventEffects.Add(effect);
            if (effect.Status is not "active" and not "complete"
                || !string.IsNullOrEmpty(effect.Failure) && Math.Abs(effect.AppliedTotal) < 0.0001f)
            { reason = "boundary_effect_not_confirmed:" + effect.Id + ":" + effect.Status; return false; }
        }
        foreach (string remove in notice.RemovedEffectIds)
        {
            if (!_eventEffects.TryGetValue(remove, out var effect)) continue;
            if (effect.Status is "active" or "removed") { effect.Status = "removed"; changed = true; }
        }
        bool neutralEnding = notice.Stage == "started" || notice.Stage == "ended" && round.EventPhase == "closing" && EventBoundaryComplete(round, "started");
        if (!neutralEnding && !changed && notice.ConfirmedDocumentIds.Count == 0) { reason = "boundary_has_no_actual_effect"; return false; }
        bool progress = notice.Stage.StartsWith("progress:", StringComparison.Ordinal);
        notice.State = progress ? "applied" : "public";
        WorldDiplomacyMemoryApplication.Invalidate(Storage);
        if (!progress) _pendingEventNotices.Enqueue(notice.Id);
        if (notice.Stage == "started")
        {
            round.EventBeginningId = notice.Id; round.RoundTopic = notice.Title;
            _host.Log("diplomacy.event_generation_completed event=" + round.RoundId
                + " origin=" + round.EventOpeningOrigin + " startedDay=" + round.StartedDay
                + " publishedDay=" + _host.CurrentDay() + " elapsedGameDays=" + Math.Max(0, _host.CurrentDay() - round.StartedDay)
                + " targetGameDays=3");
        }
        else if (notice.Stage == "ended") round.EventEndingId = notice.Id;
        // This public record is committed before UI. No propagation arrival or
        // acknowledgement is required for any kingdom to know a boundary.
        round.ConversationRevision++;
        MarkEventDirty(round);
        if (!progress) ArchiveEventBoundary(notice);
        if (notice.Stage == "started")
            foreach (var lead in Storage.MaterialLeads.Where(x => x.EventId == round.RoundId && x.Status == "selected_unpublished"))
                lead.Status = "published";
        reason = ""; return true;
    }

    private void ArchiveEventBoundary(DiplomacyEventAnnouncement notice)
    {
        if (notice == null || notice.State != "public" || notice.CanonicalHistoryRecorded) return;
        string sourceKey = "event_boundary:" + notice.Id;
        string narrative = JsonConvert.SerializeObject(new { notice.Id, notice.EventId, notice.Stage, notice.Title, notice.Body,
            narrative = notice.Narrative.Where(x => x.Public).Select(x => new { x.Kind, x.Source, x.Text, x.SubjectKingdomId, x.HeroIds }),
            confirmed_documents = notice.ConfirmedDocumentIds });
        bool recorded = AppendCanonicalHistoryEntry("event_boundary", sourceKey, notice.Id, notice.Day, "", "",
            notice.ParticipantIds, notice.Stage, "", narrative, false)
            || _runtime.CanonicalHistorySourceKeys.Contains(sourceKey);
        var receipts = notice.EffectIds.Concat(notice.RemovedEffectIds).Distinct().Where(_eventEffects.ContainsKey)
            .Select(x => _eventEffects[x]).Where(x => Math.Abs(x.AppliedTotal) > 0.0001f || x.Status == "removed")
            .Select(x => new { x.Id, x.Kind, x.KingdomId, x.OtherKingdomId, x.SettlementId, x.AppliedTotal,
                x.LastAppliedDay, x.ExpiresDay, x.Status }).ToList();
        if (receipts.Count > 0)
        {
            string effectKey = sourceKey + ":effects";
            recorded &= AppendCanonicalHistoryEntry("diplomatic_result", effectKey, notice.Id, notice.Day, "", "",
                notice.ParticipantIds, "event_effect", "", JsonConvert.SerializeObject(receipts), true)
                || _runtime.CanonicalHistorySourceKeys.Contains(effectKey);
        }
        notice.CanonicalHistoryRecorded = recorded;
    }

    private bool IsAcceptedEventDocument(WorldDiplomacyDocument document)
        => Storage.DialogueArrangements.Any(x => x.DocumentId == document.DocumentId
            && x.RoundId == document.RoundId && x.Status is "accepted" or "deferred")
            || Storage.AidRequests.Any(x => x.RoundId == document.RoundId && x.Status == "accepted"
                && (x.RequestDocumentId == document.DocumentId || x.AnswerDocumentId == document.DocumentId));

    private void CommitPendingEventDocuments(WorldDiplomacyRound round, bool acceptedOnly = false)
    {
        // Recovery is subject to the same publication boundary as first execution.
        // acceptedOnly selects durable promises; it never grants a secret-execution bypass.
        if (!IsLiveRound(round) || (EventHost != null && !EventBoundaryComplete(round, "started"))) return;
        foreach (var document in Storage.Documents.Where(x => x.RoundId == round.RoundId && x.EventExecutionPending
            && (!acceptedOnly || IsAcceptedEventDocument(x))).ToList())
        {
            var arrangement = Storage.DialogueArrangements.FirstOrDefault(x => x.DocumentId == document.DocumentId);
            if (arrangement != null && (!TryValidateOralDeclarationWork(arrangement, out string expectedIntent)
                || expectedIntent != document.Intent))
            {
                document.EventExecutionPending = false;
                if (arrangement.TreatyExecutionState == "pending") arrangement.TreatyExecutionState = "not_applied";
                _host.Log("oral declaration pending revalidation rejected document=" + document.DocumentId);
                continue;
            }
            document.EventExecutionPending = false;
            try
            {
                _committingEventDocuments = true;
                _oralAgreementExecutionDocumentId = arrangement == null ? "" : document.DocumentId;
                ProcessAnalyzedDocument(document, document.Intent, document.Commitment, document.RequiresResponse, document.EventExecutionTone, document.EventExecutionConfidence);
                if (arrangement != null)
                {
                    arrangement.Status = arrangement.TreatyExecutionState == "uncertain" ? "execution_uncertain"
                        : document.IsReadyForPublication ? document.ChangedDiplomaticState ? "executed" : "published" : "execution_uncertain";
                    arrangement.Reason = document.MechanicalResult;
                    if (document.IsReadyForPublication) DeliverDialogueDeclarationToCounterparty(document, arrangement.TargetKingdomId);
                }
            }
            catch (Exception ex)
            {
                if (arrangement != null) { arrangement.Status = "execution_uncertain"; arrangement.Reason = ex.GetType().Name; }
                round.TechnicalHoldReason = "event_document_execution_uncertain";
                _host.Log(round.TechnicalHoldReason + " document=" + document.DocumentId);
            }
            finally { _oralAgreementExecutionDocumentId = ""; _committingEventDocuments = false; }
        }
    }
}
