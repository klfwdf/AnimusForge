using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    internal static readonly string[] EventThemeIds = { "government", "distribution", "personal_grievance", "honor",
        "trust", "ambition", "kinship", "conviction", "history", "mystery" };

    // Request boundary only; equal base weights, independent of material counts.
    // The stable job identity supplies reproducible entropy even before a save.
    private string ChooseExplorationTheme(WorldDiplomacyJob job)
    {
        var recent = Storage.EventAnnouncements.AsEnumerable().Reverse().Where(x => x.Stage == "started" && x.State == "public"
                && EventThemeIds.Contains(x.Theme)).OrderByDescending(x => x.Day)
            .GroupBy(x => x.EventId).Take(6).Select(x => x.First().Theme).ToList();
        string exhausted = ExhaustedExplorationTheme(job);
        var weights = EventThemeIds.Select(x => x == exhausted ? 0 : 1.0 / (1 + 3 * recent.Count(t => t == x))).ToArray();
        // Avalanche all bits before sampling: adjacent pass suffixes must not
        // retain the nearly identical high bits of the original FNV hash.
        ulong entropy = Convert.ToUInt64(MaterialHash(job.JobId + ":theme:" + job.EventThemePass), 16);
        unchecked
        {
            entropy ^= entropy >> 30; entropy *= 0xbf58476d1ce4e5b9UL;
            entropy ^= entropy >> 27; entropy *= 0x94d049bb133111ebUL;
            entropy ^= entropy >> 31;
        }
        double draw = (uint)(entropy >> 32)
            / ((double)uint.MaxValue + 1) * weights.Sum();
        for (int i = 0; i < weights.Length; i++)
        {
            draw -= weights[i];
            if (draw < 0) return EventThemeIds[i];
        }
        return EventThemeIds[EventThemeIds.Length - 1];
    }

    private static string ThemeQuery(WorldDiplomacyJob job) => string.IsNullOrEmpty(job.EventExplorationTheme) ? ""
        : DiplomacyPromptTexts.Get("EventThemeQuery_" + job.EventExplorationTheme, "");

    // A completed initial capture is retained while bounded thematic retrieval
    // runs. No model call or second material library is introduced here.
    private string CaptureExplorationSources(WorldDiplomacyJob job, IReadOnlyCollection<string> countries, IReadOnlyCollection<string> excluded)
    {
        if (IsPlayerDeclarationOpening(ResolveRound(job.RoundId))) return CapturePlanningSources(job, countries, excluded);
        if (job.EventSelectionStage == "oral") return CapturePlanningSources(job, countries, excluded, job.EventTriggerMaterial);
        // A proactive bilateral agenda already has its direction. Random theme
        // navigation over unrelated realms cannot improve that fixed trigger.
        if (WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject(job.EventTriggerMaterial, out var trigger)
            && !string.IsNullOrWhiteSpace((string)trigger["decision_reason"]))
            return CapturePlanningSources(job, countries, excluded, (string)trigger["decision_reason"]);
        if (string.IsNullOrEmpty(job.EventProbeSnapshot))
        {
            string probe = CapturePlanningSources(job, countries, excluded, "");
            if (probe == null) return null;
            var rows = JArray.Parse(probe);
            // Whole records only, enough navigation plus an unrestricted lane.
            var retained = new JArray(); int budget = 6000;
            foreach (var row in rows.OfType<JObject>())
            {
                int cost = _host.EstimateTokens(row.ToString(Formatting.None));
                if (cost > budget || retained.Count >= 12) continue;
                retained.Add(row.DeepClone()); budget -= cost;
            }
            job.EventProbeSnapshot = retained.ToString(Formatting.None);
            job.EventProbeVersion = MaterialHash(string.Join("|", countries.Select(MaterialInvestigationVersion)));
        }
        if (string.IsNullOrEmpty(job.EventExplorationTheme))
        {
            job.EventExplorationTheme = ChooseExplorationTheme(job);
            _host.Log("diplomacy.theme_selected job=" + job.JobId + " theme=" + job.EventExplorationTheme + " pass=" + job.EventThemePass);
        }
        string deep = CapturePlanningSources(job, countries, excluded, DirectedExplorationQuery(job));
        if (deep == null) return null;
        string version = MaterialHash(string.Join("|", countries.Select(MaterialInvestigationVersion)));
        if (!string.IsNullOrEmpty(job.EventProbeVersion) && job.EventProbeVersion != version)
        {
            // Deep capture independently validates its source revision and live
            // identities, and includes generic/identity retrieval. Discard only
            // obsolete navigation, never throw away this complete fresh result.
            job.EventProbeSnapshot = ""; job.EventProbeVersion = "";
            _host.Log("diplomacy.material_probe_superseded job=" + job.JobId);
            return deep;
        }
        var combined = JArray.Parse(deep);
        var keys = new HashSet<string>(combined.OfType<JObject>().Select(x => (string)x["key"]), StringComparer.Ordinal);
        foreach (var row in JArray.Parse(job.EventProbeSnapshot).OfType<JObject>())
            if (keys.Add((string)row["key"])) combined.Add(row);
        return combined.ToString(Formatting.None);
    }

    private string CapturePlanningSources(WorldDiplomacyJob job, IReadOnlyCollection<string> countries, IReadOnlyCollection<string> excluded, string topicOverride = null)
    {
        if (_host is not IDiplomacyEventMaterialHost host) return "[]";
        string topic = topicOverride ?? (IsPlayerDeclarationOpening(ResolveRound(job.RoundId))
            ? ResolveDocument(ResolveRound(job.RoundId).RootDocumentId)?.Body ?? "" : ThemeQuery(job));
        try
        {
            var cache = _completedPlanningCaptures.GetOrCreateValue(job);
            if (cache.Pass != job.MaterialSearchPass || cache.Day != _host.CurrentDay())
            { cache.Rows.Clear(); cache.Pass = job.MaterialSearchPass; cache.Day = _host.CurrentDay(); }
            // A same-day, same-topic subset can reuse complete raw records from
            // this job's earlier multi-country capture. No truncated request
            // projection, persisted cache, cross-job reuse or Tick work.
            string CacheKey(string country) => country + "\n" + topic;
            bool reusable = _host is IDiplomacyMaterialVersionHost && !string.IsNullOrWhiteSpace(topic);
            if (reusable && countries.Count > 0 && countries.All(country => cache.Rows.TryGetValue(CacheKey(country), out var item)
                && item.Version == MaterialInvestigationVersion(country)))
            {
                var rows = new JArray(); var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (string country in countries)
                    foreach (var row in cache.Rows[CacheKey(country)].Rows.OfType<JObject>())
                        if (seen.Add((string)row["key"]))
                        {
                            var copy = (JObject)row.DeepClone();
                            copy["previously_used"] = excluded.Contains((string)copy["key"]);
                            rows.Add(copy);
                        }
                _host.Log("diplomacy.material_capture_reused job=" + job.JobId + " countries=" + string.Join(",", countries));
                return rows.ToString(Formatting.None);
            }
            string raw = host.CaptureEventSources(countries, excluded, job.MaterialSearchPass, topic);
            if (raw != null && reusable)
            {
                var rows = JArray.Parse(raw);
                foreach (string country in countries)
                {
                    var complete = new JArray(rows.OfType<JObject>().Where(x => (string)x["known_by"] == country).Select(x => x.DeepClone()));
                    if (complete.Count == 0) continue;
                    if (cache.Rows.Count >= 18) cache.Rows.Clear();
                    cache.Rows[CacheKey(country)] = new CompletedPlanningCapture { Version = MaterialInvestigationVersion(country), Rows = complete };
                }
            }
            return raw;
        }
        catch (Exception ex)
        {
            SuspendDiplomacyJob(job, "event_material_capture_failed:" + ex.GetType().Name + ":" + ex.Message);
            return null;
        }
    }
    private sealed class CompletedPlanningCapture
    {
        internal string Version;
        internal JArray Rows;
    }
    private sealed class PlanningCaptureCache
    {
        internal int Pass = -1, Day = -1;
        internal readonly Dictionary<string, CompletedPlanningCapture> Rows = new(StringComparer.Ordinal);
    }
    private readonly ConditionalWeakTable<WorldDiplomacyJob, PlanningCaptureCache> _completedPlanningCaptures = new();
    // Request/completion boundaries only. Keep the search on its existing owner;
    // no daily scan of all archived events or new per-frame material reads.
    private bool CanDiscardUnpublishedMaterial(WorldDiplomacyRound round) =>
        IsLiveRound(round) && round.EventPhase is "forming" or "opening"
        && round.TechnicalHoldReason is not ("event_document_execution_uncertain" or "legacy_unverified_work_preserved")
        && round.TechnicalRequestHoldReason is not ("event_document_execution_uncertain" or "legacy_unverified_work_preserved")
        && !IsPlayerDeclarationOpening(round) && round.EventSourceType != "dialogue_commitment"
        && !Storage.Documents.Any(x => x.RoundId == round.RoundId)
        && !Storage.EventAnnouncements.Any(x => x.EventId == round.RoundId)
        && !Storage.EventEffects.Any(x => x.EventId == round.RoundId)
        && !Storage.DialogueArrangements.Any(x => x.RoundId == round.RoundId)
        && !Storage.AidRequests.Any(x => x.RoundId == round.RoundId && x.Status == "accepted")
        && !round.PendingOffers.Any(x => x.Status == "open")
        && !Storage.DiplomaticThreats.Any(x => x.StageRoundId == round.RoundId && x.Status == "open")
        && !round.PlayerResponses.Any(x => x.Status == "pending");

    private bool CanContinueMaterialSearch(WorldDiplomacyRound round) =>
        CanDiscardUnpublishedMaterial(round) && round.EventPhase == "forming";

    private bool TryCloseExhaustedAdmissionDraft(WorldDiplomacyJob job, WorldDiplomacyRound round)
    {
        string reason = job.SuspendedReason ?? "";
        bool admissionFailure = reason.StartsWith("admission_identity_", StringComparison.Ordinal)
            || reason.StartsWith("admission_core_", StringComparison.Ordinal)
            || reason.StartsWith("admission_seed_identity_changed", StringComparison.Ordinal)
            || reason.StartsWith("admission_capture_", StringComparison.Ordinal)
            || reason.StartsWith("admission_snapshot_", StringComparison.Ordinal)
            || reason.StartsWith("admission_country_material_", StringComparison.Ordinal)
            || reason.StartsWith("admission_material_host_", StringComparison.Ordinal);
        if (job.TechnicalRecoveryAttempts < 3 || !admissionFailure || job.EventSelectionStage is "oral" or "player"
            || !(IsEventPlanningJob(job) || job.Kind is "event_boundary" or "event_boundary_audit" && job.ForcedIntent == "started")
            || round == null || round.TechnicalHoldReason is "event_document_execution_uncertain" or "legacy_unverified_work_preserved"
            || !CanDiscardUnpublishedMaterial(round)) return false;
        // This is abandonment of an unpublishable proposal, not a factual ruling
        // that its actors are dead/invalid. Retain the exact inputs in the archived
        // owner and release only the disposable NPC slot. No gameplay effect ran.
        round.EventSelectionEvidence = JsonConvert.SerializeObject(new {
            outcome = "unpublished_admission_recovery_exhausted", reason = job.SuspendedReason,
            attempts = job.TechnicalRecoveryAttempts, stage = job.EventSelectionStage,
            seed = job.CountryAdmissionSeed, admission = string.IsNullOrEmpty(job.CountryAdmission) ? round.CountryAdmission : job.CountryAdmission,
            candidate = job.EventSelectedCandidate, material = job.EventMaterialSnapshot, draft = job.PrivacyDraft,
            previous_evidence = round.EventSelectionEvidence });
        _host.Log("diplomacy.admission_draft_closed event=" + round.RoundId + " job=" + job.JobId
            + " attempts=" + job.TechnicalRecoveryAttempts + " reason=" + job.SuspendedReason);
        CloseUnpublishedMaterial(job, round, "unpublished_admission_recovery_exhausted");
        return true;
    }

    private bool RetireConfirmedInvalidatedMaterial(WorldDiplomacyJob job, WorldDiplomacyRound round, string reason)
    {
        if (!CanDiscardUnpublishedMaterial(round)) return false;
        round.EventSelectionEvidence = JsonConvert.SerializeObject(new {
            outcome = "confirmed_identity_invalidated", reason, stage = job.EventSelectionStage,
            seed = job.CountryAdmissionSeed, admission = string.IsNullOrEmpty(job.CountryAdmission) ? round.CountryAdmission : job.CountryAdmission,
            source_digest = MaterialHash(job.EventMaterialSnapshot ?? "") });
        _host.Log("diplomacy.material_identity_invalidated event=" + round.RoundId + " evidence=" + round.EventSelectionEvidence);
        if (round.EventPhase == "forming") ContinueMaterialSearch(job, round, false);
        else CloseUnpublishedMaterial(job, round, "unpublished_identity_invalidated");
        return true;
    }

    private void CloseUnpublishedMaterial(WorldDiplomacyJob job, WorldDiplomacyRound round, string reason)
    {
        job.RequestPreparationPending = true; job.IsRunning = false; job.LlmMessages?.Clear();
        foreach (var lead in Storage.MaterialLeads.Where(x => x.EventId == round.RoundId && x.Status == "selected_unpublished"))
            lead.Status = "not_viable";
        WorldDiplomacyRoundApplication.Close(Storage, reason, _host.CurrentDay,
            SettleTradeAllianceOfferCooldownsForClosedRound, ScheduleNextNormalRoundAfter,
            CommitLocalRoundSummary, TryScheduleTokenCompression, _host.Log, round);
        Storage.NextNormalRoundDay = Math.Max(Storage.NextNormalRoundDay, _host.CurrentDay() + 1);
        InvalidateDialogueIndex();
    }

    private bool TryRemainingMaterialCandidates(WorldDiplomacyJob job, WorldDiplomacyRound round, string reason)
    {
        if (!CanContinueMaterialSearch(round) || job.EventSelectionStage is not "expand"
            || !WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject(job.EventSelectedCandidate, out var selected)
            || !WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject(job.EventComparison, out var comparison)
            || comparison["assessments"] is not JArray assessments) return false;
        string failedId = (string)selected["id"];
        var eligible = new HashSet<string>(assessments.OfType<JObject>()
            .Where(x => (bool?)x["qualified"] == true && string.IsNullOrEmpty((string)x["duplicate_of"]))
            .Select(x => (string)x["id"]), StringComparer.Ordinal);
        var pool = JArray.Parse(job.EventCandidatePool);
        var available = new HashSet<string>(JArray.Parse(job.EventMaterialSnapshot).OfType<JObject>()
            .Select(x => (string)x["key"]), StringComparer.Ordinal);
        var remaining = new JArray(pool.OfType<JObject>().Where(x => (string)x["id"] != failedId
            && eligible.Contains((string)x["id"]) && x["material_keys"] is JArray keys
            && keys.Values<string>().All(available.Contains)).Select(x => x.DeepClone()));
        foreach (var lead in Storage.MaterialLeads.Where(x => x.EventId == round.RoundId && x.Status == "selected_unpublished"))
            lead.Status = "not_viable";
        if (remaining.Count == 0) return false;
        job.EventCandidatePool = remaining.ToString(Formatting.None);
        job.EventSelectedCandidate = ""; job.EventComparison = "";
        job.CountryAdmission = ""; job.CountryAdmissionSeed = "";
        job.AdmissionSupplementedCountries.Clear(); job.AdmissionRequiredSourceKeys.Clear();
        ResetAdmissionRepairState(job);
        job.EventSelectionStage = "compare"; job.Kind = "event_concept";
        job.PrivacyDraft = ""; job.MaterialRepairFeedback = "";
        job.TechnicalRecoveryAttempts = 0; job.PrivacyAuditRetries = 0;
        job.SuspendedReason = ""; job.RetryAfterUtcTicks = 0; job.NextRecoveryProbeUtcTicks = 0;
        job.IsRunning = false; job.RequestPreparationPending = true; job.LlmMessages?.Clear();
        AccrueTechnicalPause(round);
        round.TechnicalHoldReason = ""; round.TechnicalRequestHoldReason = ""; round.TechnicalHoldStartedHour = -1;
        _host.Log("diplomacy.material_alternative_review job=" + job.JobId + " rejected=" + failedId
            + " remaining=" + remaining.Count + " reason=" + reason);
        return true;
    }

    private void ContinueMaterialSearch(WorldDiplomacyJob job, WorldDiplomacyRound round, bool delay, bool inspectedNoCandidate = false)
    {
        if (!CanContinueMaterialSearch(round))
        { HoldTechnicalRequest(job, "material_search_requires_existing_case_review"); return; }
        // Persisted pass count bounds both paid requests and slot occupancy,
        // including legacy jobs loaded after many unsuccessful search days.
        if (job.MaterialSearchPass >= 2)
        {
            // No same-day replacement loop when the campaign is paused. Do not
            // move the user's configured event interval farther into the future.
            CloseUnpublishedMaterial(job, round, "unpublished_material_search_exhausted");
            _host.Log("diplomacy.material_search_exhausted event=" + round.RoundId + " pass=" + job.MaterialSearchPass
                + " evidence=" + round.EventSelectionEvidence);
            return;
        }
        if (!string.IsNullOrEmpty(job.CountryAdmission))
            _host.Log("diplomacy.admission_seed_abandoned job=" + job.JobId + " reason=" + round.EventSelectionEvidence
                + " result=" + job.CountryAdmission);
        foreach (var lead in Storage.MaterialLeads.Where(x => x.EventId == round.RoundId && x.Status == "selected_unpublished"))
            lead.Status = "failed_unused";
        job.MaterialSearchPass = Math.Min(1000000, job.MaterialSearchPass + 1);
        // One further batch investigates the same direction before a permitted
        // change of direction. Technical retries/reload never reroll it.
        if (inspectedNoCandidate && ++job.EventThemeInvestigations >= 2)
        { job.EventThemePass++; job.EventThemeInvestigations = 0; job.EventExplorationTheme = ""; }
        job.EventProbeSnapshot = ""; job.EventProbeVersion = "";
        job.MaterialNextSearchDay = delay ? _host.CurrentDay() + 1 : 0;
        job.EventInvestigationKingdoms = SelectMaterialInvestigationBatch(job.CandidateKingdomIds, true);
        job.EventMaterialSnapshot = ""; job.EventCandidatePool = ""; job.EventComparison = "";
        job.EventSelectedCandidate = ""; job.PrivacyDraft = ""; job.MaterialRepairFeedback = "";
        job.CountryAdmission = ""; job.CountryAdmissionSeed = ""; job.AdmissionSupplementedCountries.Clear(); job.AdmissionRequiredSourceKeys.Clear();
        ResetAdmissionRepairState(job);
        job.EventSelectionStage = "candidates"; job.Kind = "event_concept"; job.IsRunning = false;
        job.TechnicalRecoveryAttempts = 0; job.SuspendedReason = ""; job.RetryAfterUtcTicks = 0;
        job.RequestPreparationPending = true; job.NextRecoveryProbeUtcTicks = 0; job.PrivacyAuditRetries = 0;
        job.LlmMessages?.Clear();
        AccrueTechnicalPause(round);
        round.TechnicalHoldReason = ""; round.TechnicalRequestHoldReason = ""; round.TechnicalHoldStartedHour = -1;
        _host.Log("diplomacy.material_search_continued job=" + job.JobId + " pass=" + job.MaterialSearchPass
            + " nextDay=" + job.MaterialNextSearchDay + " countries=" + string.Join(",", job.EventInvestigationKingdoms));
    }

    private void RejectMaterialCandidate(WorldDiplomacyJob job, string detail)
    {
        job.MaterialRepairFeedback = detail;
        SuspendDiplomacyJob(job, "event_candidate_evidence_invalid:" + detail);
    }

    private static JObject MaterialSearchOutcome(WorldDiplomacyRound round) =>
        WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject(round?.EventSelectionEvidence, out var evidence)
            ? new JObject { ["outcome"] = evidence["outcome"]?.DeepClone(), ["reason"] = evidence["reason"]?.DeepClone(),
                ["research_hint"] = ResearchHint(evidence), ["previous_theme"] = evidence["previous_theme"]?.DeepClone(),
                ["theme_pass"] = evidence["theme_pass"]?.DeepClone() } : null;

    // Capture every admitted realm, one cooperative request at a time. The
    // persisted cursor survives retries/reload; no top-two membership shortcut.
    private bool SupplementExpansionSources(WorldDiplomacyJob job)
    {
        var admitted = ReadAdmission(job.CountryAdmission).Admitted;
        string country = admitted.FirstOrDefault(x => !job.AdmissionSupplementedCountries.Contains(x));
        if (country == null) return true;
        if (_host is not IDiplomacyEventMaterialHost)
        { SuspendDiplomacyJob(job, "admission_material_host_missing"); return false; }
        var frozen = JArray.Parse(job.EventMaterialSnapshot);
        string selectedTheme = (string)JObject.Parse(job.EventSelectedCandidate)["theme"];
        string topic = IsPlayerDeclarationOpening(ResolveRound(job.RoundId))
            ? ResolveDocument(ResolveRound(job.RoundId).RootDocumentId)?.Body ?? ""
            : job.EventSelectionStage is "player" or "oral" ? job.EventTriggerMaterial
            : EventThemeIds.Contains(selectedTheme) ? DiplomacyPromptTexts.Get("EventThemeQuery_" + selectedTheme, "") : ThemeQuery(job);
        if (job.EventSelectionStage == "expand"
            && WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject(job.EventTriggerMaterial, out var trigger)
            && !string.IsNullOrWhiteSpace((string)trigger["decision_reason"])) topic = (string)trigger["decision_reason"];
        string raw = CapturePlanningSources(job, new[] { country }, Array.Empty<string>(), topic);
        if (raw == null)
        {
            job.RequestPreparationPending = true;
            if (string.IsNullOrEmpty(job.SuspendedReason)) job.RetryAfterUtcTicks = DateTime.UtcNow.AddMilliseconds(100).Ticks;
            return false;
        }
        JArray extra;
        try { extra = JArray.Parse(raw); }
        catch (JsonException) { SuspendDiplomacyJob(job, "event_material_invalid:admission_sources"); return false; }
        var rows = extra.OfType<JObject>().Where(x => (string)x["known_by"] == country
            && !string.IsNullOrWhiteSpace((string)x["key"]) && ((string)x["key"]).Length <= 300
            && !string.IsNullOrWhiteSpace((string)x["text"])).ToList();
        if (rows.Count == 0)
        { SuspendDiplomacyJob(job, "admission_country_material_missing:" + country); return false; }
        var known = new HashSet<string>(frozen.OfType<JObject>().Select(x => (string)x["key"]), StringComparer.Ordinal);
        // A complete persona/source record per admitted country is mandatory.
        // Additional complete records share the existing budget; an oversized
        // mandatory set fails explicitly at the request gate, never drops a realm.
        var required = rows.OrderByDescending(x => (string)x["source_kind"] == "character_setting").First();
        job.AdmissionRequiredSourceKeys.Add((string)required["key"]);
        int budget = Math.Max(0, 20000 - frozen.Sum(x => _host.EstimateTokens(x.ToString(Formatting.None)) + 128));
        foreach (var row in rows.OrderByDescending(x => ReferenceEquals(x, required)))
        {
            string key = (string)row["key"];
            if (known.Contains(key)) continue;
            int cost = _host.EstimateTokens(row.ToString(Formatting.None)) + 128;
            if (!ReferenceEquals(row, required) && (cost > budget || frozen.Count >= 48)) continue;
            var copy = (JObject)row.DeepClone();
            copy["version"] = MaterialHash(copy.ToString(Formatting.None)); copy["excerpt"] = false;
            frozen.Add(copy); known.Add(key); budget -= cost;
        }
        job.EventMaterialSnapshot = frozen.ToString(Formatting.None);
        job.AdmissionSupplementedCountries.Add(country);
        _host.Log("diplomacy.admission_sources job=" + job.JobId + " country=" + country + " captured=" + rows.Count);
        if (admitted.All(job.AdmissionSupplementedCountries.Contains)) return true;
        job.RequestPreparationPending = true;
        job.RetryAfterUtcTicks = DateTime.UtcNow.AddMilliseconds(100).Ticks;
        return false;
    }

    private static HashSet<string> RequiredPlanningSourceKeys(WorldDiplomacyJob job)
    {
        var required = new HashSet<string>(job.AdmissionRequiredSourceKeys, StringComparer.Ordinal) { "world:current" };
        if (!string.IsNullOrEmpty(job.CountryAdmissionSeed))
            foreach (var subject in JObject.Parse(job.CountryAdmissionSeed)["direct_subjects"].OfType<JObject>()) required.Add((string)subject["source"]);
        if (job.EventSelectionStage == "oral")
            foreach (var source in JArray.Parse(job.EventMaterialSnapshot).OfType<JObject>().Where(x => (string)x["source_kind"] == "confirmed_oral_terms"))
                required.Add((string)source["key"]);
        if (job.EventSelectionStage == "player" && !string.IsNullOrEmpty(job.DocumentId)) required.Add(job.DocumentId);
        if (job.EventSelectionStage == "player" && !string.IsNullOrEmpty(job.EventMaterialSnapshot))
            foreach (var source in SelectWorldBookEvidence(JArray.Parse(job.EventMaterialSnapshot))) required.Add((string)source["key"]);
        void Collect(JObject value)
        {
            foreach (string key in (value?["material_keys"] as JArray)?.Values<string>() ?? Enumerable.Empty<string>())
                required.Add(key);
            foreach (var party in (value?["participants"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
                foreach (string key in (party["source_keys"] as JArray)?.Values<string>() ?? Enumerable.Empty<string>())
                    required.Add(key);
            foreach (var fact in (value?["narrative"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
                foreach (string key in (fact["basis_keys"] as JArray)?.Values<string>() ?? Enumerable.Empty<string>()) required.Add(key);
        }
        if (WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject(job.EventSelectedCandidate, out var selected)) Collect(selected);
        if (WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject(job.PrivacyDraft, out var draft)) Collect(draft);
        if (job.EventSelectionStage == "compare" && !string.IsNullOrEmpty(job.EventCandidatePool))
            foreach (var candidate in JArray.Parse(job.EventCandidatePool).OfType<JObject>()) Collect(candidate);
        return required;
    }

    // Keep selected evidence first and diverse supporting dossiers afterwards.
    // The complete-request budget can remove optional whole records, but never
    // evidence referenced by the selected seed or current draft.
    private JArray PlanningSources(WorldDiplomacyJob job, JArray frozen)
    {
        if (job.EventSelectionStage is "candidates" or "compare") return (JArray)frozen.DeepClone();
        var required = RequiredPlanningSourceKeys(job);
        var rows = frozen.OfType<JObject>().ToList();
        var support = rows.Where(x => !required.Contains((string)x["key"])).GroupBy(x => (string)x["known_by"])
            .SelectMany(g => g.Select((value, index) => new { value, index })).OrderBy(x => x.index).Select(x => x.value);
        return new JArray(rows.Where(x => required.Contains((string)x["key"])).Concat(support).Select(x => x.DeepClone()));
    }

    private void FinishMaterialRequest(WorldDiplomacyJob job)
    {
        var request = JObject.Parse(job.UserPrompt);
        // These fields already contain JSON. Embed the same parsed values once
        // instead of sending an escaped JSON document inside another JSON string.
        // Stored evidence/drafts and their digests remain byte-for-byte unchanged.
        foreach (string field in new[] { "selected_candidate", "admission_seed", "trigger_material", "proposed_event", "rejected_draft" })
            if (request[field]?.Type == JTokenType.String
                && WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject((string)request[field], out var structured))
                request[field] = structured;
        bool boundMatter = IsBoundMatterPlanning(job);
        job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get(boundMatter ? "BoundMatterPlanning"
            : CanSelectEventStructure(job) ? "EventStructureSelection" : "EventStoryDiversity", "");
        if (boundMatter) job.StoryReviewVersion = 2;
        else if (job.Kind == "event_concept_audit")
        {
            job.StoryReviewVersion = 1;
            job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("EventStoryReview", "");
        }
        request["repair_feedback"] = job.MaterialRepairFeedback;
        if (request["sources"] is JArray supplied)
        {
            request["world_source_key"] = supplied.OfType<JObject>().Any(x => (string)x["key"] == "world:current") ? "world:current" : null;
            // The identity registry used by the audit need only cover the supplied
            // sources. Final validation still uses the authoritative identity owner.
            var heroes = new HashSet<string>(supplied.OfType<JObject>()
                .SelectMany(x => (x["hero_ids"] as JArray)?.Values<string>() ?? Enumerable.Empty<string>()), StringComparer.Ordinal);
            if (heroes.Count > 0 && request["narrative_actors"] is JObject actors)
                foreach (var actor in actors.Properties().Where(x => !heroes.Contains(x.Name)).ToList()) actor.Remove();
        }
        // Reserve space below the final 32k gate; use the same estimator as send.
        // Whole optional records only: never cut prose, conditions, a chosen
        // source, a reviewed draft, or actor audit bindings.
        long Size() => (long)_host.EstimateTokens(job.SystemPrompt) + _host.EstimateTokens(request.ToString(Formatting.None)) + 32;
        long size = Size();
        // Human requests/confirmed undertakings need relevant history, not a
        // mandatory novelty comparison against every recent unrelated case.
        if (boundMatter && request["prior_events"] is JArray prior)
            for (int i = prior.Count - 1; i >= 0 && size > 28000; i--)
            {
                if (prior[i] is not JObject old || IsRequiredMatterHistory(job, old)) continue;
                prior.RemoveAt(i);
                request["omitted_prior_event_count"] = ((int?)request["omitted_prior_event_count"] ?? 0) + 1;
                size = Size();
            }
        // Prior cases are audit evidence, not optional filler. Keep their full
        // motives, conditions and outcomes; shed unused dossiers first.
        foreach (string key in new[] { "reusable_leads" })
        {
            if (request[key] is not JArray optional) continue;
            while (size > 28000 && optional.Count > 0)
            {
                optional.RemoveAt(optional.Count - 1);
                request["omitted_" + key] = ((int?)request["omitted_" + key] ?? 0) + 1;
                size = Size();
            }
        }
        if (job.EventSelectionStage is "expand" or "player" or "oral" or "compare" && request["sources"] is JArray planning)
        {
            var required = RequiredPlanningSourceKeys(job);
            for (int i = planning.Count - 1; i >= 0 && size > 28000; i--)
            {
                if (required.Contains((string)planning[i]["key"])) continue;
                planning.RemoveAt(i);
                request["omitted_support_records"] = ((int?)request["omitted_support_records"] ?? 0) + 1;
                size = Size();
            }
            // The writer/auditor and final validator share only evidence actually
            // offered by this request, including its frozen supplemental dossiers.
            job.EventMaterialSnapshot = planning.ToString(Formatting.None);
        }
        if (job.EventSelectionStage == "candidates" && request["sources"] is JArray sources)
        {
            while (size > 28000 && sources.Count > 1)
            {
                var optional = sources.LastOrDefault(x => (string)x["key"] != "world:current");
                if (optional == null) break;
                optional.Remove();
                request["omitted_source_records"] = ((int?)request["omitted_source_records"] ?? 0) + 1;
                size = Size();
            }
            // The validator must accept only keys actually offered to this writer.
            job.EventMaterialSnapshot = sources.ToString(Formatting.None);
        }
        if (job.Kind == "event_concept_audit")
        {
            var fields = new JArray("approved", "actor_review", "position_reviews", "narrative_safety");
            if (!boundMatter)
            {
                request["story_review_required"] = new JObject {
                    ["digest"] = StoryReviewDigest(job, request["prior_events"] as JArray ?? new()),
                    ["comparison_event_ids"] = new JArray((request["prior_events"] as JArray ?? new()).Select(x => x["RoundId"])),
                    ["comparison_fields"] = new JArray("event_id", "verdict", "prior_quote", "draft_quote", "reason") };
                fields.Add("story_review");
            }
            else request.Remove("story_review_required");
            if (!string.IsNullOrEmpty(job.EventExplorationTheme)) fields.Add("theme_review");
            request["required_audit_fields"] = fields;
        }
        job.UserPrompt = request.ToString(Formatting.None);
        size = Size();
        AppendStructureObservation(job, size);
        size = Size();
        job.LlmMessages?.Clear();
        if (size > 32000)
        {
            job.RequestPreparationPending = true;
            SuspendDiplomacyJob(job, "event_required_evidence_budget_exceeded:retain_prior_cases_and_selected_sources");
        }
        _host.Log("diplomacy.material_request job=" + job.JobId + " stage=" + job.EventSelectionStage
            + " pass=" + job.MaterialSearchPass + " estimatedInputTokens=" + size
            + " sourceRecords=" + (request["sources"] as JArray)?.Count);
    }

    private string CandidateProblem(JObject candidate, HashSet<string> sourceIds, HashSet<string> ids, WorldDiplomacyJob job)
    {
        if (candidate == null) return "candidate_must_be_object";
        if (!string.IsNullOrEmpty(job.EventExplorationTheme)
            && (candidate["theme"]?.Type != JTokenType.String || !EventThemeIds.Contains((string)candidate["theme"])
                || candidate["theme_reason"]?.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)candidate["theme_reason"])
                || ((string)candidate["theme_reason"]).Length > 1800))
            return "theme_and_substantive_theme_reason_required_from_theme_catalog";
        if (candidate["id"]?.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)candidate["id"])
            || ((string)candidate["id"]).Length > 80 || ids.Contains((string)candidate["id"])) return "id_missing_long_or_duplicate";
        if (candidate["material_keys"] is not JArray keys || keys.Count == 0 || keys.Count > 16)
            return "material_keys_must_have_1_to_16_supplied_keys";
        var invalidKeys = keys.Where(x => x.Type != JTokenType.String || !sourceIds.Contains((string)x)).ToList();
        if (invalidKeys.Count > 0) return "material_keys_must_reference_supplied_sources:invalid="
            + string.Join(",", invalidKeys.Select(x => x.ToString(Formatting.None)))
            + ";allowed=" + string.Join(",", sourceIds.OrderBy(x => x, StringComparer.Ordinal));
        // These are material associations, not the final sovereign negotiating set.
        if (candidate["kingdoms"] is not JArray countries || countries.Count == 0
            || countries.Any(x => x.Type != JTokenType.String || !job.CandidateKingdomIds.Contains((string)x))
            || countries.Values<string>().Distinct().Count() != countries.Count) return "kingdoms_must_be_nonempty_distinct_supplied_ids";
        foreach (string field in new[] { "conflict", "motive", "new_development", "npc_creation", "negotiation_space" })
            if (candidate[field]?.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)candidate[field])
                || ((string)candidate[field]).Length > 1800) return field + "_must_be_nonempty_string_at_most_1800_chars";
        return "";
    }
}
