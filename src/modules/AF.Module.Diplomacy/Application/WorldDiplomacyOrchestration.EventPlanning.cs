using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal interface IDiplomacyEventMaterialHost
{
    // Main-thread, request-boundary snapshot. Never queried by a background writer.
    string CaptureEventSources(IReadOnlyCollection<string> kingdoms, IReadOnlyCollection<string> excludedKeys, int searchPass = 0, string topic = "");
}

internal interface IDiplomacyWorldBookHost
{
    // Constant-time main-thread metadata; no retrieval, IO or model call.
    (string state, int count, string revision) ReadWorldBookAvailability();
}

internal interface IDiplomacyPlayerContactHost
{
    int PlayerContactIntervalDays();
}

internal interface IDiplomacyMaterialVersionHost
{
    string CaptureMaterialVersion(string kingdomId);
}

internal interface IDiplomacyNarrativeActorHost
{
    IReadOnlyDictionary<string, string> CaptureNarrativeActors(IReadOnlyCollection<string> kingdoms);
}

internal sealed partial class WorldDiplomacyOrchestration
{
    private sealed class PlanningCaptureInputs
    {
        internal string Scope;
        internal List<string> Countries, ExcludedKeys;
    }
    // Weak job ownership: pending work reuses its bounded capture inputs without
    // scanning event history on each poll. Removed/reloaded jobs retain no cache.
    private readonly ConditionalWeakTable<WorldDiplomacyJob, PlanningCaptureInputs> _planningCaptureInputs = new();
    private static bool IsEventPlanningJob(WorldDiplomacyJob job) => job?.Kind is "event_concept" or "event_concept_audit";

    private static bool LeadStillApplicable(DiplomacyMaterialLead lead, JArray sources, List<string> countries)
    {
        if (lead.SourceVersions?.Count == 0 || !WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject(lead.Candidate, out var candidate)) return false;
        if (candidate["kingdoms"] is not JArray parties || parties.Values<string>().Any(x => !countries.Contains(x))) return false;
        var versions = sources.OfType<JObject>().ToDictionary(x => (string)x["key"], x => (string)x["version"], StringComparer.Ordinal);
        return lead.SourceVersions != null && lead.SourceVersions.All(x => versions.TryGetValue(x.Key, out var version) && version == x.Value);
    }

    private List<string> SelectMaterialInvestigations(IEnumerable<string> candidates) => SelectMaterialInvestigationBatch(candidates, false);

    private List<string> SelectMaterialInvestigationBatch(IEnumerable<string> candidates, bool broaden)
    {
        Storage.MaterialInvestigations ??= new(StringComparer.Ordinal);
        Storage.CountryParticipation ??= new(StringComparer.Ordinal);
        // Revisit only changed material or a due seven-day exploration window.
        // Unvisited countries precede revisits; least-recent sequence prevents
        // the same absent countries monopolizing repeated opportunities on one day.
        return candidates.Where(x => !_host.IsPlayerParty(x)).Distinct()
            .Select(x => new { Id=x, Prior=Storage.MaterialInvestigations.TryGetValue(x,out var q)?q:null })
            .Where(x => broaden || x.Prior == null || x.Prior.Day + 7 <= _host.CurrentDay()
                || x.Prior.SourceVersion != MaterialInvestigationVersion(x.Id))
            .OrderBy(x => x.Prior == null ? 0 : 1).ThenBy(x => x.Prior?.Sequence ?? 0)
            .ThenBy(x => x.Prior?.Day ?? -1)
            .ThenBy(x => Storage.CountryParticipation.TryGetValue(x.Id,out var p)?p.LastExpressionDay:-1)
            .ThenBy(x => x.Id,StringComparer.Ordinal).Take(2).Select(x=>x.Id).ToList();
    }

    private string MaterialInvestigationVersion(string country) => (_host as IDiplomacyMaterialVersionHost)?.CaptureMaterialVersion(country)
        ?? MaterialHash(EventHost?.CaptureEventWorld(new[] { country }) ?? "");

    private void RecordMaterialInvestigation(WorldDiplomacyJob job)
    {
        long sequence=Storage.MaterialInvestigations.Values.Select(x=>x.Sequence).DefaultIfEmpty(0).Max()+1;
        foreach(string country in job.EventInvestigationKingdoms)
            Storage.MaterialInvestigations[country]=new() { Day=_host.CurrentDay(),Sequence=sequence,SourceVersion=MaterialInvestigationVersion(country) };
    }

    private void NoQualifiedMaterial(WorldDiplomacyJob job, WorldDiplomacyRound round, string reason)
        => NoQualifiedMaterialReviewed(job, round, reason, null);

    private void NoQualifiedMaterialReviewed(WorldDiplomacyJob job, WorldDiplomacyRound round, string reason, JObject result)
    {
        RecordMaterialSearchFailure(job, round, reason, result);
        if (job.EventSelectionStage == "oral" || job.EventSelectionStage == "player" && IsPlayerDeclarationOpening(round))
        {
            // A published human declaration is already the trigger. Repair the
            // grounded response plan on this owner; never reroll a new NPC case.
            HoldTechnicalRequest(job, "event_player_plan_requires_grounded_response:" + reason);
            return;
        }
        // Existing published/accepted work can never be erased by a no-result.
        if (!CanContinueMaterialSearch(round)) { HoldTechnicalRequest(job,"material_search_requires_existing_case_review"); return; }
        if (TryRemainingMaterialCandidates(job, round, reason)) return;
        // A failed seed does not discard its already reviewed siblings. Once the
        // batch is exhausted, allow two further batches, never an endless owner.
        ContinueMaterialSearch(job, round, false, true);
    }

    private void PrepareMaterialSelection(WorldDiplomacyJob job, WorldDiplomacyRound round,
        List<string> candidates, JArray sources, List<EventPriorStory> prior)
    {
        bool compare = job.EventSelectionStage == "compare";
        var relevantKeys = new HashSet<string>(sources.OfType<JObject>().Select(x => (string)x["key"]), StringComparer.Ordinal);
        job.SystemPrompt = DiplomacyPromptTexts.Get(compare ? "EventCandidateCompare" : "EventCandidateWriter", "");
        if (!string.IsNullOrEmpty(job.EventExplorationTheme))
            job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get(compare ? "EventThemeComparison" : "EventThemeExploration", "");
        // Comparison contains identity and evidence, but no lane, absence history or priority flag.
        job.UserPrompt = JsonConvert.SerializeObject(new {
            candidates, sources, world_source_key = "world:current",
            exploration_theme = compare ? null : job.EventExplorationTheme,
            theme_catalog = string.IsNullOrEmpty(job.EventExplorationTheme) ? null
                : EventThemeIds.Select(id => new { id, meaning = DiplomacyPromptTexts.Get("EventThemeQuery_" + id, "") }),
            search_pass = compare ? (int?)null : job.MaterialSearchPass,
            grounded_creation = !compare && job.MaterialSearchPass > 0,
            previous_search_result = MaterialSearchOutcome(round),
            required_participants = round.Participants.Select(x => x.KingdomId),
            player_kingdom = CurrentPlayerAuthority(), trigger_material = job.EventTriggerMaterial,
            investigation_starts = compare ? null : job.EventInvestigationKingdoms,
            candidate_pool = compare ? JArray.Parse(job.EventCandidatePool) : null,
            rejected_response = string.IsNullOrEmpty(job.MaterialRepairFeedback) ? "" : job.PrivacyDraft,
            prior_events = SelectPriorStoryWindow(prior, relevantKeys),
            omitted_prior_event_count = Math.Max(0, prior.Count - 12),
            reusable_leads = compare ? null : (Storage.MaterialLeads ?? new()).Where(x => x.Status is "not_selected" or "failed_unused")
                .Where(x => LeadStillApplicable(x, sources, candidates))
                .OrderByDescending(x => x.Day).Take(4).Select(x => x.Candidate).ToList()
        });
        job.RoundConversationRevision = round.ConversationRevision;
        FinishMaterialRequest(job);
    }

    private void CompleteMaterialSelection(WorldDiplomacyJob job, WorldDiplomacyRound round, JObject json)
    {
        job.PrivacyDraft = json.ToString(Formatting.None);
        if (job.EventSelectionStage == "candidates")
        {
            if (json["candidates"] is not JArray pool || pool.Count > 6)
            { SuspendDiplomacyJob(job, "event_candidates_missing"); return; }
            if (pool.Count == 0)
            {
                if (json["reason"]?.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)json["reason"]))
                { SuspendDiplomacyJob(job, "event_candidates_empty_reason_missing:explain_specific_obstacle_after_grounded_npc_creation"); return; }
                NoQualifiedMaterialReviewed(job, round, (string)json["reason"], json); return;
            }
            var sourceIds = new HashSet<string>(JArray.Parse(job.EventMaterialSnapshot).OfType<JObject>().Select(x => (string)x["key"]), StringComparer.Ordinal);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var normalized = new JArray();
            var rejected = new List<string>();
            foreach (var token in pool)
            {
                var candidate = token as JObject;
                string problem = CandidateProblem(candidate, sourceIds, ids, job);
                if (problem.Length > 0) { rejected.Add("candidate[" + (normalized.Count + rejected.Count) + "]:" + problem); continue; }
                string id = (string)candidate["id"];
                ids.Add(id);
                // Whitelist the comparison schema: no model-supplied fairness hints survive.
                normalized.Add(new JObject { ["id"] = id, ["material_keys"] = candidate["material_keys"].DeepClone(),
                    ["kingdoms"] = candidate["kingdoms"].DeepClone(), ["conflict"] = candidate["conflict"].DeepClone(),
                    ["motive"] = candidate["motive"].DeepClone(), ["new_development"] = candidate["new_development"].DeepClone(),
                    ["theme"] = candidate["theme"]?.DeepClone(), ["theme_reason"] = candidate["theme_reason"]?.DeepClone(),
                    ["npc_creation"] = candidate["npc_creation"].DeepClone(), ["negotiation_space"] = candidate["negotiation_space"].DeepClone() });
            }
            if (normalized.Count == 0) { RejectMaterialCandidate(job, string.Join(";", rejected)); return; }
            if (rejected.Count > 0) _host.Log("diplomacy.invalid_candidates_skipped count=" + rejected.Count + " reasons=" + string.Join(";", rejected));
            job.MaterialRepairFeedback = ""; job.PrivacyDraft = ""; job.TechnicalRecoveryAttempts = 0;
            job.EventCandidatePool = normalized.ToString(Formatting.None);
            job.EventSelectionStage = "compare"; job.IsRunning = false; job.LlmMessages?.Clear();
            return;
        }
        var candidates = JArray.Parse(job.EventCandidatePool).OfType<JObject>().ToDictionary(x => (string)x["id"], StringComparer.Ordinal);
        if (json["assessments"] is not JArray assessments || assessments.Count != candidates.Count
            || json["best_equivalent"] is not JArray best || json["urgent"] is not JArray urgent)
        { SuspendDiplomacyJob(job, "event_comparison_invalid"); return; }
        var assessed = new HashSet<string>(StringComparer.Ordinal);
        var eligible = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in assessments.OfType<JObject>())
        {
            string id = (string)row["id"];
            string duplicate = (string)row["duplicate_of"];
            if (id == null || !candidates.ContainsKey(id) || !assessed.Add(id) || row["qualified"]?.Type != JTokenType.Boolean)
            { SuspendDiplomacyJob(job, "event_comparison_invalid:assessment_id_or_qualified:" + id + ";valid_ids=" + string.Join(",", candidates.Keys)); return; }
            bool qualified = (bool)row["qualified"];
            // A rejected seed need not invent novelty or urgency. A qualified seed
            // still needs all reasons; duplication remains an explicit decision.
            var missing = new[] { "strength", "weakness", "novelty_reason", "urgency_reason", "duplicate_of" }
                .Where(k => row[k]?.Type != JTokenType.String
                    || (k != "duplicate_of" && (qualified || k == "weakness") && string.IsNullOrWhiteSpace((string)row[k]))).ToList();
            if (missing.Count > 0)
            { SuspendDiplomacyJob(job, "event_comparison_reasons_missing:assessment=" + id + ";fields=" + string.Join(",", missing)); return; }
            if (!string.IsNullOrEmpty(duplicate) && (duplicate == id || !candidates.ContainsKey(duplicate)))
            { SuspendDiplomacyJob(job, "event_comparison_invalid:duplicate_of=" + duplicate + ";valid_ids=" + string.Join(",", candidates.Keys)); return; }
            if ((bool)row["qualified"] && string.IsNullOrEmpty(duplicate)) eligible.Add(id);
        }
        if (assessed.Count != candidates.Count || best.Concat(urgent).Any(x => x.Type != JTokenType.String || !eligible.Contains((string)x))
            || json["comparison_reason"]?.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)json["comparison_reason"]))
        { SuspendDiplomacyJob(job, "event_comparison_selection_invalid:eligible_ids=" + string.Join(",", eligible)
            + ";invalid_selected_ids=" + string.Join(",", best.Concat(urgent).Where(x => x.Type != JTokenType.String || !eligible.Contains((string)x)))
            + ";comparison_reason_required"); return; }
        // A sibling may already have a reusable lead from an earlier comparison
        // of this batch. A later explicit rejection must retire that copy too.
        var rejectedCandidates = new HashSet<string>(candidates.Where(x => !eligible.Contains(x.Key))
            .Select(x => x.Value.ToString(Formatting.None)), StringComparer.Ordinal);
        foreach (var lead in Storage.MaterialLeads.Where(x => x.EventId == round.RoundId && rejectedCandidates.Contains(x.Candidate)))
            lead.Status = "not_viable";
        if(eligible.Count==0 && best.Count==0 && urgent.Count==0)
        { job.EventComparison=json.ToString(Formatting.None);NoQualifiedMaterialReviewed(job,round,(string)json["comparison_reason"],json);return; }
        if(best.Count==0 && urgent.Count==0) { SuspendDiplomacyJob(job,"event_comparison_selection_missing");return; }
        // Urgency first; only an explicitly equivalent best-quality band can consult absence.
        var finalists = (urgent.Count > 0 ? urgent : best).Values<string>().Distinct().ToList();
        string selected = finalists.OrderBy(id => ((JArray)candidates[id]["kingdoms"]).Values<string>()
                .Where(x => !_host.IsPlayerParty(x)).Select(x => Storage.CountryParticipation.TryGetValue(x, out var p) ? p.LastExpressionDay : -1)
                .DefaultIfEmpty(int.MaxValue).Min())
            .ThenBy(id => id, StringComparer.Ordinal).First();
        job.EventSelectedCandidate = candidates[selected].ToString(Formatting.None);
        job.EventComparison = json.ToString(Formatting.None);
        Storage.MaterialLeads ??= new();
        var sourceVersions = JArray.Parse(job.EventMaterialSnapshot).OfType<JObject>()
            .ToDictionary(x => (string)x["key"], x => (string)x["version"], StringComparer.Ordinal);
        foreach (var candidate in candidates)
        {
            string id = round.RoundId + ":" + job.MaterialSearchPass + ":" + candidate.Key + ":"
                + MaterialHash(candidate.Value.ToString(Formatting.None) + "|" + job.EventMaterialSnapshot);
            var existing = Storage.MaterialLeads.FirstOrDefault(x => x.Id == id);
            if (existing != null)
            {
                existing.Status = candidate.Key == selected ? "selected_unpublished" : eligible.Contains(candidate.Key) ? "not_selected" : "not_viable";
                continue;
            }
            Storage.MaterialLeads.Add(new DiplomacyMaterialLead { Id = id, EventId = round.RoundId,
                Candidate = candidate.Value.ToString(Formatting.None), SourceVersion = MaterialHash(job.EventMaterialSnapshot),
                SourceVersions = ((JArray)candidate.Value["material_keys"]).Values<string>().Distinct()
                    .ToDictionary(x => x, x => sourceVersions[x], StringComparer.Ordinal),
                Status = candidate.Key == selected ? "selected_unpublished" : eligible.Contains(candidate.Key) ? "not_selected" : "not_viable", Day = _host.CurrentDay() });
        }
        // Only disposable leads are evicted. Publication and obligations have separate durable owners.
        while (Storage.MaterialLeads.Count > 64)
        {
            var disposable = Storage.MaterialLeads.FirstOrDefault(x => x.Status != "selected_unpublished");
            if (disposable == null) break;
            Storage.MaterialLeads.Remove(disposable);
        }
        job.EventSelectionStage = "expand"; job.MaterialRepairFeedback = ""; job.TechnicalRecoveryAttempts = 0; job.IsRunning = false; job.LlmMessages?.Clear();
        // Supplemental capture is resumed by request preparation, not performed in completion.
    }

    private void TryScheduleEventConcept(bool policyOnly)
    {
        int day = _host.CurrentDay();
        if (!_host.WorldDiplomacyEnabled() || EventHost == null || Storage.Jobs.Count >= _host.MaxPendingJobs()
            || !WorldDiplomacyLiveRoundRules.CanStartOrdinary(Storage, day, 1)
            || day < Storage.NextNormalRoundDay) return;
        var signals = Storage.PendingPolicySignals?.Where(x => x != null).Take(8).ToList() ?? new();
        if (policyOnly && signals.Count == 0) return;
        var candidates = _host.AllKingdomIds().Where(x => !string.IsNullOrEmpty(x)
            && !_host.IsPlayerParty(x) && _host.HasIndependentAuthority(x)
            && !_host.IsEliminatedParty(x)).Distinct(StringComparer.Ordinal).ToList();
        if (candidates.Count < 2) { ScheduleNextNormalRoundAfter(day); return; }
        // Reserve the ordinary event slot, not a kingdom or a mechanical action.
        var round = new WorldDiplomacyRound { RoundId = _host.NewId("diplomacy_event"),
            SchemaVersion = _host.RelaySchemaVersion(), State = "active", StartedDay = day,
            LastActivityDay = day, BirthLane = "normal", EventPhase = "forming",
            EventOpeningOrigin = "npc_material", EventSourceType = policyOnly ? "policy_material" : "npc_material", CountryAdmissionVersion = DiplomacyCountryAdmission.CurrentVersion };
        InitializeEventOwner(round, false);
        if (Storage.ActiveRound == null) Storage.ActiveRound = round; else Storage.ConcurrentRounds.Add(round);
        InvalidateDialogueIndex();
        round.AttachedPolicySignals.AddRange(signals);
        Storage.LastOrdinaryRoundStartedDay = day;
        ScheduleNextNormalRoundAfter(day);
        EnqueueEventConcept(round, candidates);
    }

    private void EnqueueEventConcept(WorldDiplomacyRound round, List<string> candidates, WorldDiplomacyJob decision = null)
    {
        round.EventPhase = "forming";
        EnqueueJob(new WorldDiplomacyJob { JobId = _host.NewId("event_concept"), Kind = "event_concept",
            RoundId = round.RoundId, ExchangeId = round.RoundId, CandidateKingdomIds = candidates,
            EventSelectionStage = "candidates", EventInvestigationKingdoms = SelectMaterialInvestigations(candidates),
            EventTriggerMaterial = decision == null ? "" : JsonConvert.SerializeObject(new {
                known_by = decision.AuthorKingdomId, source_is_private = true,
                decision_reason = round.TopicSeedContext,
                known_public_facts = decision.EventMaterialSnapshot,
                original_evidence = decision.UserPrompt, excerpt = false }),
            RequestLane = round.BirthLane, Priority = 70, CreatedDay = _host.CurrentDay(), MaxTokens = _host.AnalysisMaxTokens() });
    }

    private void PrepareEventConceptRequest(WorldDiplomacyJob job)
    {
        job.RequestPreparationPending = false;
        var round = ResolveRound(job.RoundId);
        if (!IsLiveRound(round)) return;
        if (!CheckPlayerWorldBookAvailability(job, round)) return;
        // Old unpublished drafts are reconstructed through the single current selector.
        // Published events never pass this boundary again.
        if (string.IsNullOrEmpty(job.EventSelectionStage))
        {
            job.EventSelectionStage = "candidates"; job.Kind = "event_concept"; job.PrivacyDraft = "";
            job.EventInvestigationKingdoms = SelectMaterialInvestigations(job.CandidateKingdomIds);
        }
        var candidates = job.CandidateKingdomIds.Where(x => _host.HasIndependentAuthority(x)
            && !_host.IsEliminatedParty(x)).Distinct().ToList();
        if (string.IsNullOrEmpty(job.EventMaterialSnapshot))
        {
            var material = new JArray();
            bool fixedAgenda = job.EventSelectionStage is not "player" and not "oral" && !IsPlayerDeclarationOpening(round)
                && round.Participants.Count > 0
                && WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject(job.EventTriggerMaterial, out var fixedTrigger)
                && !string.IsNullOrWhiteSpace((string)fixedTrigger["decision_reason"]);
            if (fixedAgenda) job.EventInvestigationKingdoms = round.Participants.Select(x => x.KingdomId).Distinct().ToList();
            string scope = job.MaterialSearchPass + ":" + string.Join(",", candidates) + ":"
                + string.Join(",", round.Participants.Select(x => x.KingdomId)) + ":" + string.Join(",", job.EventInvestigationKingdoms);
            if (!_planningCaptureInputs.TryGetValue(job, out var capture) || capture.Scope != scope)
            {
                var ordinaryStarts = candidates.OrderBy(x => MaterialHash(job.CreatedDay + ":ordinary:" + job.MaterialSearchPass + ":" + x), StringComparer.Ordinal);
                capture = new PlanningCaptureInputs { Scope = scope,
                    Countries = IsPlayerDeclarationOpening(round)
                        ? AdmissionRequiredParties(round).Concat(ResolveDocument(round.RootDocumentId).MentionedKingdomIds ?? new())
                            .Where(candidates.Contains).Distinct(StringComparer.Ordinal).ToList()
                        : fixedAgenda ? round.Participants.Select(x => x.KingdomId).Distinct(StringComparer.Ordinal).ToList()
                        : round.Participants.Select(x => x.KingdomId).Concat(job.EventInvestigationKingdoms)
                        .Concat(ordinaryStarts).Distinct(StringComparer.Ordinal).Take(6).ToList(),
                    ExcludedKeys = ReadPriorEventStories(round).SelectMany(x => x.EventMaterialKeys ?? new()).Distinct().ToList() };
                _planningCaptureInputs.Remove(job); _planningCaptureInputs.Add(job, capture);
            }
            string raw;
            try { raw = CaptureExplorationSources(job, capture.Countries, capture.ExcludedKeys); }
            catch (JsonException) { SuspendDiplomacyJob(job, "event_material_invalid"); return; }
            if (raw == null && _host is IDiplomacyEventMaterialHost)
            {
                job.RequestPreparationPending = true;
                if (string.IsNullOrEmpty(job.SuspendedReason)) job.RetryAfterUtcTicks = DateTime.UtcNow.AddMilliseconds(100).Ticks;
                return;
            }
            _planningCaptureInputs.Remove(job);
            raw ??= "[]";
            JArray captured;
            try { captured = JArray.Parse(raw); }
            catch (JsonException) { SuspendDiplomacyJob(job, "event_material_invalid"); return; }
            RecordMaterialInvestigation(job);
            captured.Add(new JObject { ["key"] = "world:current", ["known_by"] = "public",
                ["source_kind"] = "current_world", ["text"] = CompactPlanningWorld(EventHost.CaptureEventWorld(candidates), 6000),
                ["hero_ids"] = new JArray(candidates.Select(_host.PartyRulerId).Where(x => !string.IsNullOrEmpty(x))) });
            if (WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject(job.EventTriggerMaterial, out var trigger)
                && !string.IsNullOrWhiteSpace((string)trigger["known_public_facts"]))
                captured.Add(new JObject { ["key"] = "agenda:" + round.RoundId, ["known_by"] = (string)trigger["known_by"],
                    ["source_kind"] = "recorded_agenda_facts", ["text"] = (string)trigger["known_public_facts"] });
            foreach (var policy in round.AttachedPolicySignals.Take(8))
                captured.Add(new JObject { ["key"] = "policy:" + policy.SignalKey, ["known_by"] = "public",
                    ["source_kind"] = "published_policy", ["text"] = JsonConvert.SerializeObject(policy) });
            AddPlayerOpeningSource(round, captured);
            if (job.EventSelectionStage == "oral")
                captured.Insert(0, new JObject { ["key"] = "oral:" + round.DialogueArrangementId,
                    ["known_by"] = round.InitiatorKingdomId, ["source_kind"] = "confirmed_oral_terms",
                    ["text"] = job.EventTriggerMaterial, ["hero_ids"] = new JArray(AdmissionRequiredParties(round)
                        .Select(_host.PartyRulerId).Where(x => !string.IsNullOrEmpty(x))) });
            int budget = IsPlayerDeclarationOpening(round) ? 14000 : 16000;
            int loreBudget = 8000;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            // Freeze one bounded evidence set across writing, audit, retry and save/load.
            // Prefer unused sources; rotate equal-priority sources by day rather than kingdom ID.
            foreach (var row in captured.OfType<JObject>().OrderByDescending(x => (string)x["key"] == round.RootDocumentId)
                .ThenByDescending(x => (string)x["key"] == "world:current")
                .ThenByDescending(x => (string)x["source_kind"] == "world_book_account")
                .ThenByDescending(x => (bool?)x["topic_query"] == true)
                .ThenByDescending(x => round.Participants.Any(p => p.KingdomId == (string)x["known_by"]))
                .ThenByDescending(x => job.EventInvestigationKingdoms.Contains((string)x["known_by"]))
                .ThenBy(x => (bool?)x["previously_used"] == true)
                .ThenBy(x => MaterialHash(_host.CurrentDay() + ":" + (string)x["key"]), StringComparer.Ordinal))
            {
                string key = (string)row["key"], knower = (string)row["known_by"], body = (string)row["text"];
                if (string.IsNullOrWhiteSpace(key) || key.Length > 300 || (knower != "public" && !candidates.Contains(knower))
                    || string.IsNullOrWhiteSpace(body) || !seen.Add(key)) continue;
                // Skip a whole record when it does not fit; never cut a condition or negation.
                string text = body;
                int recordTokens = _host.EstimateTokens(row.ToString(Formatting.None)) + 128;
                bool mandatoryPlayerSource = key == "world:current" || job.EventSelectionStage == "oral" && key == "oral:" + round.DialogueArrangementId
                    || IsPlayerDeclarationOpening(round) && key == round.RootDocumentId;
                // The supporting-dossier budget cannot erase the human trigger
                // or authoritative world. The final request budget still applies.
                bool lore = (string)row["source_kind"] == "world_book_account";
                if (material.Count >= 32 || !mandatoryPlayerSource && (recordTokens > budget || lore && recordTokens > loreBudget)) continue;
                if (!mandatoryPlayerSource) budget -= recordTokens;
                if (lore) loreBudget -= recordTokens;
                material.Add(new JObject { ["key"] = key, ["known_by"] = knower,
                    ["source_kind"] = (string)row["source_kind"] ?? "attributed_account", ["text"] = text,
                    ["excerpt"] = false, ["version"] = MaterialHash(JsonConvert.SerializeObject(new { body, knower,
                        kind = (string)row["source_kind"], heroes = row["hero_ids"] })),
                    ["hero_ids"] = row["hero_ids"]?.DeepClone(),
                    ["previously_used"] = (bool?)row["previously_used"] == true });
            }
            job.EventMaterialSnapshot = material.ToString(Formatting.None);
            _host.Log("diplomacy.material_composition job=" + job.JobId + " captured_lore="
                + captured.OfType<JObject>().Count(x => (string)x["source_kind"] == "world_book_account")
                + " selected_lore=" + material.OfType<JObject>().Count(x => (string)x["source_kind"] == "world_book_account")
                + " selected_personal=" + material.OfType<JObject>().Count(x => (string)x["source_kind"] is "character_setting" or "current_character_relation")
                + " selected_total=" + material.Count);
            job.EventProbeSnapshot = ""; job.EventProbeVersion = "";
        }
        JArray frozenMaterial;
        try { frozenMaterial = JArray.Parse(job.EventMaterialSnapshot); }
        catch (JsonException) { SuspendDiplomacyJob(job, "event_material_snapshot_invalid"); return; }
        if (job.EventSelectionStage is "candidates" or "compare")
        {
            PrepareMaterialSelection(job, round, candidates, frozenMaterial, ReadPriorEventStories(round));
            return;
        }
        if (!FreezeConfirmedOralCountries(job, round)) return;
        if (string.IsNullOrEmpty(job.CountryAdmission))
        {
            PrepareAdmissionSeed(job, round, candidates, frozenMaterial);
            return;
        }
        if (!ValidateFrozenAdmissionSnapshot(job)) return;
        UpgradeLegacyCountrySelection(job, round);
        if (job.SuspendedReason.StartsWith("admission_selection_upgrade_failed:", StringComparison.Ordinal)) return;
        if (TryRetireInvalidatedAdmission(job, round)) return;
        if (HandleSingleCountryAdmission(job, round)) return;
        if (job.EventSelectionStage is "expand" or "player" or "oral" && job.Kind == "event_concept"
            && WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject(job.EventComparison, out var comparison)
            && comparison["supplemented"]?.Value<bool>() != true)
        {
            if (!SupplementExpansionSources(job)) return;
            comparison["supplemented"] = true;
            job.EventComparison = comparison.ToString(Formatting.None);
            frozenMaterial = JArray.Parse(job.EventMaterialSnapshot);
        }
        // A directed player seed can be identified from the declaration and live
        // world alone. Require lore after every admitted realm has been captured,
        // before writing; an empty initial dossier must not exclude other realms.
        if (IsPlayerDeclarationOpening(round) && !HasWorldBookEvidence(frozenMaterial))
        { WaitForPlayerWorldBook(job, round); return; }
        var prior = ReadPriorEventStories(round);
        var relevantKeys = new HashSet<string>(frozenMaterial.OfType<JObject>().Select(x => (string)x["key"]), StringComparer.Ordinal);
        string proposedFingerprint = "";
        if (job.Kind == "event_concept_audit" && WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject(job.PrivacyDraft, out var proposed))
        {
            if (proposed["material_keys"] is JArray proposedKeys) relevantKeys = new HashSet<string>(proposedKeys.Values<string>(), StringComparer.Ordinal);
            if (proposed["participants"] is JArray proposedParties)
                proposedFingerprint = EventConceptFingerprint((string)proposed["background"], proposedParties.OfType<JObject>().Select(x => (string)x["kingdom"] ?? ""));
        }
        var selectedHistory = SelectPriorStoryWindow(prior, relevantKeys, proposedFingerprint);
        job.SystemPrompt = DiplomacyPromptTexts.Get(job.Kind == "event_concept_audit" ? "EventConceptAudit" : "EventConceptWriter", "");
        job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("SovereignPositionContract", "");
        job.PositionOwnerReviewVersion = 1;
        if (job.Kind == "event_concept_audit") job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("PositionOwnerReview", "");
        job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("EventNarrativeSafety", "");
        job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("CountryAdmissionContract", "");
        if (!string.IsNullOrEmpty(job.EventExplorationTheme))
            job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("EventThemeContinuity", "");
        if (IsPlayerDeclarationOpening(round)) job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("PlayerWorldBookEvidence", "");
        if (job.EventSelectionStage == "player") job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("PlayerOpeningPlan", "");
        if (job.EventSelectionStage == "oral") job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("CountryAdmissionOral", "");
        var admission = ReadAdmission(job.CountryAdmission);
        job.UserPrompt = JsonConvert.SerializeObject(new {
            round.RoundId, round.EventOpeningOrigin, round.ExternalOpeningContext,
            candidates, world_source_key = "world:current",
            sovereign_speakers = candidates.Select(x => new { kingdom = x, ruler_hero_id = _host.PartyRulerId(x) }),
            narrative_actors = NarrativeActors(candidates),
            sources = PlanningSources(job, frozenMaterial),
            trigger_material = job.EventTriggerMaterial,
            selected_candidate = job.EventSelectedCandidate,
            admission_seed = job.CountryAdmissionSeed,
            frozen_participants = admission.Admitted,
            admission_relations = admission.Input.Pairs.Where(x => admission.Admitted.Contains(x.Country)),
            policies = round.AttachedPolicySignals,
            prior_events = selectedHistory.Select(x => new { x.RoundId, x.RoundTopic, x.EventBackground, x.EventMaterialKeys, x.EventOutcome, x.EventOutcomeReason }),
            omitted_prior_event_count = prior.Count - selectedHistory.Count,
            required_participants = admission.Admitted,
            player_kingdom = CurrentPlayerAuthority(),
            narrative_safety_review_required = NarrativeSafetyRequest(job),
            actor_review_required = job.Kind == "event_concept_audit"
                ? BuildNarrativeActorAuditRequest(WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(job.PrivacyDraft)) : new JArray(),
            position_review_required = job.Kind == "event_concept_audit"
                ? BuildPositionOwnerReview(WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(job.PrivacyDraft)?["participants"] as JArray) : new JArray(),
            proposed_event = job.Kind == "event_concept_audit" ? job.PrivacyDraft : "",
            rejected_draft = job.Kind == "event_concept" && !string.IsNullOrEmpty(job.MaterialRepairFeedback) ? job.PrivacyDraft : ""
        });
        job.RoundConversationRevision = round.ConversationRevision;
        FinishMaterialRequest(job);
    }

    private bool CompleteEventConcept(WorldDiplomacyJob job, JObject json)
    {
        if (!IsEventPlanningJob(job)) return false;
        var round = ResolveRound(job.RoundId);
        if (!IsLiveRound(round)) { RemoveJob(job.JobId); return true; }
        if (job.EventSelectionStage is "candidates" or "compare")
        { CompleteMaterialSelection(job, round, json); return true; }
        if (CompleteAdmissionSeed(job, round, json)) return true;
        if (!ValidateFrozenAdmissionSnapshot(job)) return true;
        if (HandleSingleCountryAdmission(job, round)) return true;
        if (job.Kind == "event_concept")
        {
            if (json["no_negotiation"]?.Type == JTokenType.Boolean && (bool)json["no_negotiation"]
                && json["reason"]?.Type == JTokenType.String && !string.IsNullOrWhiteSpace((string)json["reason"]))
            { NoQualifiedMaterialReviewed(job, round, (string)json["reason"], json); return true; }
            ProjectEventPublicIdentities(round, json);
            if (!CheckFrozenAdmission(job, json, out string admissionError))
            { job.PrivacyDraft = FreezeEventDraft(json); SuspendDiplomacyJob(job, admissionError); return true; }
            job.PrivacyDraft = FreezeEventDraft(json);
            if (!CheckEventConcept(round, job.CandidateKingdomIds, job.EventMaterialSnapshot, json, out var problem, false))
            {
                if (problem != "event_story_already_used_without_new_motive" || !RejectStoryCandidate(job, problem, null))
                    SuspendDiplomacyJob(job, ExplainConceptDraftErrors(job, json, problem));
                return true;
            }
            job.Kind = "event_concept_audit";
            // A corrected draft gets a fresh independent review. The writer's
            // previous failure is not a finding about this replacement draft.
            job.MaterialRepairFeedback = "";
            job.PrivacyAuditRetries = 0;
            job.IsRunning = false; job.LlmMessages?.Clear(); return true;
        }
        if (json["approved"]?.Type != JTokenType.Boolean) return RetryConceptAudit(job, "approved_boolean_missing");
        if (IsBoundMatterPlanning(job) && json["approved"]?.Value<bool>() == false
            && (string)json["rejection_category"] == "duplicate")
            return RetryConceptAudit(job, "bound_matter_not_novelty_candidate:review_same_draft_for_facts_authority_and_continuity;repeated_human_request_or_confirmed_terms_are_not_rejection_reasons");
        if (!(bool)json["approved"] && !new[] { "fact", "geography", "identity", "permission", "secrecy", "position", "quality", "duplicate" }
            .Contains((string)json["rejection_category"]))
            return RetryConceptAudit(job, "admission_audit_cannot_reselect_countries:content_rejection_category_required");
        if (!(bool)json["approved"] && (string)json["rejection_category"] is "duplicate" or "quality")
        {
            string feedback = EventAuditFeedback(json);
            if (feedback != "reviewer_must_explain_the_evidence_or_semantic_problem"
                && RejectStoryCandidate(job, "event_concept_audit_failed:" + feedback, json)) return true;
        }
        if (json["approved"]?.Type != JTokenType.Boolean || !json["approved"].Value<bool>()
            || !WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject(job.PrivacyDraft, out var concept))
        {
            SuspendDiplomacyJob(job, "event_concept_audit_failed:" + EventAuditFeedback(json));
            return true;
        }
        if (job.PositionOwnerReviewVersion >= 1
            && !ValidatePositionOwnerReview(concept["participants"] as JArray, json, out var ownershipError, out bool rejectedOwner))
        {
            if (rejectedOwner) SuspendDiplomacyJob(job, "event_concept_" + ownershipError);
            else RetryConceptAudit(job, ownershipError);
            return true;
        }
        if (!ValidateNarrativeSafety(job, concept, json, out string reason, out bool rejectedSafety))
        {
            if (rejectedSafety) SuspendDiplomacyJob(job, reason); else RetryConceptAudit(job, reason);
            return true;
        }
        if (!ValidateNarrativeActorAudit(concept, json, out reason))
        {
            if (reason.StartsWith("narrative_actor_audit_mismatch", StringComparison.Ordinal)) SuspendDiplomacyJob(job, reason);
            else RetryConceptAudit(job, reason);
            return true;
        }
        if (!string.IsNullOrEmpty(job.EventExplorationTheme))
        {
            var selected = JObject.Parse(job.EventSelectedCandidate);
            var review = json["theme_review"] as JObject;
            if (review?["valid"]?.Type != JTokenType.Boolean || review["theme"]?.Type != JTokenType.String
                || (string)review["theme"] != (string)selected["theme"] || review["reason"]?.Type != JTokenType.String
                || string.IsNullOrWhiteSpace((string)review["reason"]) || ((string)review["reason"]).Length > 2000)
                return RetryConceptAudit(job, "theme_review_required:theme_valid_reason_for_selected_candidate_and_current_draft");
            if (!(bool)review["valid"])
            {
                string mismatch = "event_concept_audit_failed:theme_motive_mismatch:" + (string)review["reason"];
                if (!RejectStoryCandidate(job, mismatch, json)) SuspendDiplomacyJob(job, mismatch);
                return true;
            }
        }
        if (!CheckFrozenAdmission(job, concept, out reason))
        { SuspendDiplomacyJob(job, reason); return true; }
        if (!ValidateStoryReview(job, json, out reason, out bool rejectedStory))
        {
            if (rejectedStory)
            {
                if (!RejectStoryCandidate(job, "event_concept_audit_failed:" + reason, json))
                    SuspendDiplomacyJob(job, "event_concept_audit_failed:" + reason);
            }
            else RetryConceptAudit(job, reason);
            return true;
        }
        if (!TryAdoptEventConcept(round, job.CandidateKingdomIds, job.EventMaterialSnapshot, concept, out reason))
        { SuspendDiplomacyJob(job, reason); return true; }
        round.CountryAdmissionVersion = DiplomacyCountryAdmission.CurrentVersion; round.CountryAdmission = job.CountryAdmission;
        RegisterPlayerOpeningResponses(round);
        round.EventSelectionEvidence = new JObject { ["selected"] = JToken.Parse(job.EventSelectedCandidate),
            ["comparison"] = JToken.Parse(job.EventComparison) }.ToString(Formatting.None);
        if (WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject(job.EventSelectedCandidate, out var adoptedCandidate))
            round.EventTheme = (string)adoptedCandidate["theme"] ?? "";
        round.EventLoreEvidence = SelectWorldBookEvidence(JArray.Parse(job.EventMaterialSnapshot)).ToString(Formatting.None);
        RemoveJob(job.JobId);
        foreach (var signal in round.AttachedPolicySignals) CompletePolicySignal(signal, "event_material_adopted");
        EnsureEventBoundary(round, "started");
        return true;
    }

    private bool TryAdoptEventConcept(WorldDiplomacyRound round, List<string> candidates, string materialSnapshot, JObject concept, out string reason)
        => CheckEventConcept(round, candidates, materialSnapshot, concept, out reason, true);

    private string ExplainConceptDraftErrors(WorldDiplomacyJob job, JObject concept, string firstError)
    {
        // Report independent identity errors together so a bounded repair budget
        // is not consumed revealing one field per model call. Never repair identities here.
        if (concept["narrative"] is not JArray facts || concept["participants"] is not JArray parties) return firstError;
        var countries = new HashSet<string>(parties.OfType<JObject>().Where(x => x["kingdom"]?.Type == JTokenType.String)
            .Select(x => (string)x["kingdom"]), StringComparer.Ordinal);
        JArray sources;
        try { sources = JArray.Parse(job.EventMaterialSnapshot ?? ""); }
        catch (JsonException) { return firstError; }
        var heroes = new HashSet<string>(sources.OfType<JObject>().SelectMany(x =>
            (x["hero_ids"] as JArray)?.Values<string>() ?? Enumerable.Empty<string>()), StringComparer.Ordinal);
        var errors = new List<string>();
        for (int i = 0; i < facts.Count && errors.Count < 6; i++)
        {
            if (facts[i] is not JObject row) continue;
            if (!TryReadNarrativeSubject(row, out string subject, out string reason)) errors.Add("narrative[" + i + "]:" + reason);
            else if (!countries.Contains(subject)) errors.Add("narrative[" + i + "].subject=" + subject
                + ";must_be_participant_kingdom_id_not_hero_id;allowed=" + string.Join(",", countries));
            if (!TryReadNarrativeActors(row, heroes, out _, out reason)) errors.Add("narrative[" + i + "]:" + reason);
        }
        return errors.Count == 0 ? firstError : firstError + ";additional_errors=" + string.Join(" | ", errors.Take(6));
    }

    // The writer and adoption boundary share one structural contract. Preflight
    // does not mutate the round or replace independent semantic/identity audit.
    private bool CheckEventConcept(WorldDiplomacyRound round, List<string> candidates, string materialSnapshot, JObject concept, out string reason, bool adopt)
    {
        if (!ValidateUnansweredPlayerNarration(round, concept, out reason)) return false;
        if (!ValidatePlayerOpeningNarrative(round, "started", concept, out reason)) return false;
        reason = "event_concept_invalid:title_background_participants_material_keys_or_narrative_shape";
        string title = (string)concept["title"], background = (string)concept["background"], initiator = (string)concept["initiator"];
        if (string.IsNullOrWhiteSpace(title) || title.Length > 100 || string.IsNullOrWhiteSpace(background)
            || background.Length > 6000 || concept["participants"] is not JArray rows || rows.Count < 1
            || rows.Count > candidates.Count || concept["material_keys"] is not JArray keys || keys.Count == 0 || keys.Count > 16
            || concept["narrative"] is not JArray facts || facts.Count == 0 || facts.Count > 16) return false;
        var parties = new List<WorldDiplomacyRoundParticipant>();
        foreach (var row in rows.OfType<JObject>())
        {
            string id = (string)row["kingdom"];
            reason = "event_concept_invalid:participants.kingdom_must_be_unique_available_candidate";
            if (string.IsNullOrWhiteSpace(id) || !candidates.Contains(id) || !_host.HasIndependentAuthority(id)
                || _host.IsEliminatedParty(id) || parties.Any(x => x.KingdomId == id)) return false;
            reason = "event_concept_invalid:participants[" + id + "].agenda_gain_cost_red_line_alternative_require_nonempty_strings_at_most_1000_chars";
            if (!TryReadConceptPosition(id, row, out var party, out reason)) return false;
            reason = "event_concept_invalid:participants[" + id + "].source_keys_require_supplied_keys";
            if (row["source_keys"] is not JArray sourceKeys || sourceKeys.Count == 0
                || sourceKeys.Any(x => x.Type != JTokenType.String)) return false;
            reason = "event_concept_invalid:participants[" + id + "].address_to_requires_different_admitted_country;use_frozen_participants_or_existing_requested_respondents_or_return_no_negotiation";
            if (row["address_to"] is not JArray addressTo || (!_host.IsPlayerParty(id) && addressTo.Count == 0)
                || addressTo.Any(x => x.Type != JTokenType.String || (string)x == id || !candidates.Contains((string)x))) return false;
            party.AgendaSourceKeys = sourceKeys.Values<string>().Distinct().ToList();
            party.AgendaAddressTo = addressTo.Values<string>().Distinct().ToList();
            party.KingdomId = id; party.State = _host.IsPlayerParty(id) ? "observer" : "active"; parties.Add(party);
        }
        reason = "event_concept_invalid:initiator_must_be_nonplayer_participant_and_required_participants_preserved";
        bool playerOpening = IsPlayerDeclarationOpening(round);
        if (parties.Count != rows.Count || !parties.Any(x => x.KingdomId == initiator)
            || (playerOpening ? initiator != ResolveDocument(round.RootDocumentId).AuthorKingdomId : _host.IsPlayerParty(initiator))
            || round.Participants.Where(x => !playerOpening || x.State != "observer").Any(x => !parties.Any(p => p.KingdomId == x.KingdomId))) return false;
        if (playerOpening)
        {
            reason = "player_opening_requires_grounded_npc_participant_and_actual_declaration_source";
            if (!parties.Any(x => !_host.IsPlayerParty(x.KingdomId)) || !keys.Values<string>().Contains(round.RootDocumentId)) return false;
            var root = ResolveDocument(round.RootDocumentId);
            var directed = (root.Actions ?? new()).Where(x => x != null && NeedsDirectedPlayerReaction(x.Intent))
                .Select(x => x.TargetKingdomId).Where(x => candidates.Contains(x) && !_host.IsPlayerParty(x));
            reason = "player_opening_must_include_directly_affected_sovereign";
            if (directed.Any(x => !parties.Any(p => p.KingdomId == x))) return false;
            parties.Single(x => x.KingdomId == initiator).State = "active";
        }
        var materials = new List<string>();
        JArray evidence;
        try { evidence = JArray.Parse(materialSnapshot ?? ""); }
        catch (JsonException) { reason = "event_material_snapshot_missing"; return false; }
        var sources = evidence.OfType<JObject>().Where(x => x["key"]?.Type == JTokenType.String)
            .GroupBy(x => (string)x["key"], StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
        if (playerOpening && !SelectWorldBookEvidence(evidence).Any(x => keys.Values<string>().Contains((string)x["key"])))
        { reason = "player_opening_requires_world_book_basis"; return false; }
        foreach (var party in parties)
            foreach (string key in party.AgendaSourceKeys.Where(x => !sources.ContainsKey(x)))
            {
                // New NPC developments may ground participation only through a
                // uniquely declared story with real, supplied supporting evidence.
                if (key.StartsWith("adopted:", StringComparison.Ordinal))
                {
                    var stories = facts.OfType<JObject>().Where(x => (string)x["source"] == key && (string)x["kind"] == "adopted_npc_story").ToList();
                    if (keys.Values<string>().Contains(key) && stories.Count == 1
                        && stories[0]["basis_keys"] is JArray basis && basis.Count > 0 && basis.Count <= 16
                        && basis.All(x => x.Type == JTokenType.String && sources.TryGetValue((string)x, out var evidenceRow)
                            && ((string)evidenceRow["known_by"] == "public" || parties.Any(p => p.KingdomId == (string)evidenceRow["known_by"])))) continue;
                    reason = "event_concept_invalid:participants[" + party.KingdomId + "].source_keys:" + key
                        + ";adopted_reference_requires_unique_adopted_npc_story_with_supplied_known_basis_keys;existing_facts_must_keep_supplied_keys";
                    return false;
                }
                // Diagnose the exact field without silently authorizing a guessed alias.
                reason = "event_concept_invalid:participant_source_keys_not_supplied:" + JsonConvert.SerializeObject(new {
                    kingdom = party.KingdomId, invalid_key = key,
                    possible_exact_keys = sources.Keys.Where(x => x.EndsWith(":" + key, StringComparison.Ordinal)).Take(8),
                    supplied_keys = sources.Keys.Take(64)
                });
                return false;
            }
        reason = "event_concept_invalid:party_review_must_cover_every_candidate_once:" + string.Join(",", candidates);
        if (concept["party_review"] is not JArray reviews || reviews.Count != candidates.Count) return false;
        var reviewed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var review in reviews.OfType<JObject>())
        {
            string id = (string)review["kingdom"];
            reason = "event_concept_invalid:party_review_requires_unique_candidate_included_matching_participants_and_reason";
            if (id == null || !candidates.Contains(id) || !reviewed.Add(id) || review["included"]?.Type != JTokenType.Boolean
                || (bool)review["included"] != parties.Any(x => x.KingdomId == id)
                || string.IsNullOrWhiteSpace((string)review["reason"])) return false;
        }
        if (reviewed.Count != candidates.Count) return false;
        // Exclusion from the dispute is not permission to require an answer.
        // A reviewed limited respondent gets only the existing observer/reply lane.
        var limitedRespondents = new List<string>();
        foreach (string target in parties.SelectMany(x => x.AgendaAddressTo).Distinct())
        {
            if (!_host.HasIndependentAuthority(target) || _host.IsEliminatedParty(target))
            { reason = "event_agenda_addressee_unavailable"; return false; }
            if (parties.Any(x => x.KingdomId == target)) continue;
            if (!reviews.OfType<JObject>().Any(x => (string)x["kingdom"] == target
                && x["reply_only"]?.Type == JTokenType.Boolean && (bool)x["reply_only"]))
            { reason = "event_agenda_addressee_not_admitted"; return false; }
            limitedRespondents.Add(target);
        }
        var knownHeroes = new HashSet<string>(sources.Values.SelectMany(x => (x["hero_ids"] as JArray)?.Values<string>() ?? Enumerable.Empty<string>()), StringComparer.Ordinal);
        foreach (var key in keys)
        {
            reason = "event_concept_invalid:material_keys_require_nonempty_strings_at_most_300_chars";
            if (key.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)key) || ((string)key).Length > 300) return false;
            string id = (string)key;
            bool adopted = id.StartsWith("adopted:", StringComparison.Ordinal) && id.Length > 8;
            if (!adopted && (!sources.TryGetValue(id, out var record)
                || ((string)record["known_by"] != "public" && !parties.Any(x => x.KingdomId == (string)record["known_by"]))))
            { reason = "event_material_source_not_supplied_or_known:key=" + id
                + ";known_by=" + (sources.TryGetValue(id, out var failedSource) ? (string)failedSource["known_by"] : "not_supplied")
                + ";participants=" + string.Join(",", parties.Select(x => x.KingdomId)); return false; }
            materials.Add((string)key);
        }
        var narrative = new List<DiplomacyEventNarrativeFact>();
        foreach (var fact in facts.OfType<JObject>())
        {
            if (!TryReadNarrativeSubject(fact, out string subject, out reason)) return false;
            string kind = (string)fact["kind"], source = (string)fact["source"], text = (string)fact["text"];
            reason = "event_concept_invalid:narrative_requires_valid_kind_participant_subject_source_and_text;no_player_fiction";
            if (kind is not "public_fact" and not "allegation" and not "adopted_npc_story"
                || string.IsNullOrWhiteSpace(source) || source.Length > 600 || string.IsNullOrWhiteSpace(text) || text.Length > 1600
                || !parties.Any(x => x.KingdomId == subject) || (kind == "adopted_npc_story" && _host.IsPlayerParty(subject))) return false;
            if (!materials.Contains(source) || (source.StartsWith("adopted:", StringComparison.Ordinal) && kind != "adopted_npc_story"))
            { reason = "event_narrative_source_mismatch:index=" + narrative.Count + ";source=" + source
                + ";kind=" + kind + ";material_keys=" + string.Join(",", materials)
                + ";source_must_match_material_keys_and_adopted_source_requires_adopted_npc_story"; return false; }
            if (!TryReadNarrativeActors(fact, knownHeroes, out var heroes, out reason))
            { reason += ";narrative_index=" + narrative.Count + ";source=" + source; return false; }
            if (!ValidatePlayerNarrativeSource(fact, out reason)) return false;
            narrative.Add(new DiplomacyEventNarrativeFact { Id = round.RoundId + ":source:" + narrative.Count, HeroIds = heroes,
                Kind = kind, SubjectKingdomId = subject, Source = source, Text = text, Public = true,
                KnownKingdomIds = sources.TryGetValue(source, out var sourceRecord)
                    ? (string)sourceRecord["known_by"] == "public" ? parties.Select(x => x.KingdomId).ToList()
                        : new List<string> { (string)sourceRecord["known_by"] }
                    : new List<string> { subject } });
        }
        if (narrative.Count != facts.Count) { reason = "event_concept_invalid:narrative_rows_must_be_objects"; return false; }
        string fingerprint = EventConceptFingerprint(background, parties.Select(x => x.KingdomId));
        if (!IsPlayerDeclarationOpening(round) && round.EventSourceType != "dialogue_commitment"
            && ReadPriorEventStories(round).Any(x => x.Fingerprint == fingerprint))
        { reason = "event_story_already_used_without_new_motive"; return false; }
        if (!adopt) { reason = ""; return true; }
        // Publish state only after the complete concept has passed structural and independent semantic validation.
        round.RoundTopic = title; round.EventBackground = background; round.InitiatorKingdomId = initiator;
        round.Participants = parties; round.EventMaterialKeys = materials.Distinct().ToList(); round.EventNarrative = narrative;
        foreach (string target in limitedRespondents.Concat(round.PlayerResponses.Where(x => x.Status == "pending").Select(x => x.KingdomId))
            .Distinct().Where(x => !parties.Any(p => p.KingdomId == x)))
            WorldDiplomacyStructureRules.EnsureRoundParticipant(round, target, "observer", false);
        round.EventPartyReview = reviews.ToString(Formatting.None);
        round.EventPhase = "opening"; round.ConversationRevision++;
        reason = ""; return true;
    }

    private static bool TryReadEventPosition(JObject row, out WorldDiplomacyRoundParticipant participant)
    {
        participant = null;
        foreach (string name in new[] { "agenda", "gain", "cost", "red_line", "alternative" })
            if (row[name]?.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)row[name]) || ((string)row[name]).Length > 1000) return false;
        participant = new WorldDiplomacyRoundParticipant { Agenda = (string)row["agenda"], PreferredOutcome = (string)row["gain"],
            ExpectedCost = (string)row["cost"], RedLine = (string)row["red_line"], Alternative = (string)row["alternative"] };
        return true;
    }

    private void StartEventDiscussion(WorldDiplomacyRound round)
    {
        if (!IsLiveRound(round) || !EventBoundaryComplete(round, "started")) return;
        CommitPendingEventDocuments(round);
        if (round.EventPhase != "opening") return;
        round.EventPhase = "discussing";
        if (IsPlayerDeclarationOpening(round) || round.EventSourceType == "dialogue_commitment")
        {
            RegisterPlayerOpeningResponses(round);
            SchedulePlayerResponseWork(round);
            MarkEventDirty(round);
            return;
        }
        var initiator = round.Participants.FirstOrDefault(x => x.KingdomId == round.InitiatorKingdomId);
        string target = initiator?.AgendaAddressTo.FirstOrDefault(x => _host.HasIndependentAuthority(x) && !_host.IsEliminatedParty(x))
            ?? round.Participants.FirstOrDefault(x => x.KingdomId != round.InitiatorKingdomId)?.KingdomId;
        EnqueueGeneration(round.InitiatorKingdomId, target, null, false, null, 70, roundId: round.RoundId, allowUntargeted: true);
    }
}
