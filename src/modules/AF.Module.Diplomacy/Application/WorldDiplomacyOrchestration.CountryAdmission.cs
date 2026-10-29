using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal interface IDiplomacyCountryAdmissionHost
{
    int AdmissionCountryCountPreference { get; }
    string AdmissionSubjectCountry(string kind, string id);
    DiplomacyAdmissionInput CaptureCountryAdmission(IReadOnlyCollection<string> direct,
        IReadOnlyCollection<string> heroes, IReadOnlyCollection<string> candidates);
}

// Optional cold-boundary port: an absent object or owner is not proof of loss.
internal interface IDiplomacyAdmissionIdentityHost
{
    DiplomacyAdmissionIdentity ReadAdmissionIdentity(string kind, string id, string expectedCountry);
}

internal enum DiplomacyAdmissionIdentityState { Unknown, Valid, ConfirmedInvalidated }

internal readonly struct DiplomacyAdmissionIdentity
{
    internal readonly DiplomacyAdmissionIdentityState State;
    internal readonly string Country, Reason;
    internal DiplomacyAdmissionIdentity(DiplomacyAdmissionIdentityState state, string country = null, string reason = null)
    { State = state; Country = country; Reason = reason; }

    internal static DiplomacyAdmissionIdentity Hero(bool found, bool dead, bool alive, string country, string expected)
    {
        if (!found) return new(DiplomacyAdmissionIdentityState.Unknown, reason: "hero_lookup_missing");
        if (dead) return new(DiplomacyAdmissionIdentityState.ConfirmedInvalidated, reason: "hero_dead");
        if (!alive) return new(DiplomacyAdmissionIdentityState.Unknown, reason: "hero_not_alive_unconfirmed_death");
        if (string.IsNullOrWhiteSpace(country)) return new(DiplomacyAdmissionIdentityState.Unknown, reason: "hero_country_unavailable");
        if (!string.IsNullOrEmpty(expected) && country != expected)
            return new(DiplomacyAdmissionIdentityState.ConfirmedInvalidated, country, "hero_country_changed:" + expected + ":" + country);
        return new(DiplomacyAdmissionIdentityState.Valid, country);
    }

    internal static DiplomacyAdmissionIdentity Kingdom(bool found, bool eliminated, string id) => !found ? new(DiplomacyAdmissionIdentityState.Unknown, reason: "kingdom_lookup_missing")
        : eliminated ? new(DiplomacyAdmissionIdentityState.ConfirmedInvalidated, id, "kingdom_eliminated")
        : new(DiplomacyAdmissionIdentityState.Valid, id);
}

internal sealed partial class WorldDiplomacyOrchestration
{
    private List<string> AdmissionRequiredParties(WorldDiplomacyRound round)
    {
        var oral = Storage.DialogueArrangements.FirstOrDefault(x => x.ArrangementId == round.DialogueArrangementId);
        if (oral != null) return new[] { oral.ActorKingdomId, oral.TargetKingdomId }.Distinct().ToList();
        if (IsPlayerDeclarationOpening(round))
        {
            var root = ResolveDocument(round.RootDocumentId);
            // Broadcast recipients and mentions do not become direct parties.
            return new[] { root.AuthorKingdomId, root.TargetKingdomId }
                .Concat((root.Actions ?? new()).Select(x => x.TargetKingdomId))
                .Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        }
        return round.Participants.Select(x => x.KingdomId).Distinct().ToList();
    }

    private bool FreezeConfirmedOralCountries(WorldDiplomacyJob job, WorldDiplomacyRound round)
    {
        if (job.EventSelectionStage != "oral" || !string.IsNullOrEmpty(job.CountryAdmission)) return true;
        var oral = Storage.DialogueArrangements.FirstOrDefault(x => x.ArrangementId == round.DialogueArrangementId);
        if (oral == null) { HoldTechnicalRequest(job, "oral_selection_arrangement_missing"); return false; }
        try
        {
            var direct = AdmissionRequiredParties(round);
            if (direct.Count != 2 || direct.Any(x => string.IsNullOrEmpty(x) || !_host.HasIndependentAuthority(x) || _host.IsEliminatedParty(x)))
                throw new InvalidOperationException("oral_selection_direct_identity_invalid");
            var host = _host as IDiplomacyCountryAdmissionHost ?? throw new InvalidOperationException("admission_snapshot_host_missing");
            var candidates = job.CandidateKingdomIds.Where(x => _host.HasIndependentAuthority(x) && !_host.IsEliminatedParty(x)
                && (!_host.IsPlayerParty(x) || direct.Contains(x))).Distinct().ToList();
            var heroes = direct.Select(_host.PartyRulerId).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
            var selected = DiplomacyCountryAdmission.Calculate(host.CaptureCountryAdmission(direct, heroes, candidates),
                host.AdmissionCountryCountPreference, job.JobId);
            // Confirmed structured parties are authoritative. An LLM need not
            // identify or approve them again before planning the consequences.
            selected.Seed = job.EventTriggerMaterial;
            job.CountryAdmission = JsonConvert.SerializeObject(selected);
            round.CountryAdmissionVersion = DiplomacyCountryAdmission.CurrentVersion;
            _host.Log("diplomacy.oral_country_selection job=" + job.JobId + " result=" + job.CountryAdmission);
            return true;
        }
        catch (Exception ex)
        { HoldTechnicalRequest(job, "oral_selection_capture_failed:" + ex.Message); return false; }
    }

    private void PrepareAdmissionSeed(WorldDiplomacyJob job, WorldDiplomacyRound round, List<string> candidates, JArray sources)
    {
        bool audit = !string.IsNullOrEmpty(job.CountryAdmissionSeed);
        var sourceIndex = AdmissionSourceIndex(sources);
        if (audit) PinAdmissionTargetSources(job, JObject.Parse(job.CountryAdmissionSeed), sourceIndex);
        job.SystemPrompt = DiplomacyPromptTexts.Get("CountryAdmissionSeed", "") + "\n"
            + DiplomacyPromptTexts.Get(audit ? "CountryAdmissionAudit" : "CountryAdmissionIdentify", "");
        job.UserPrompt = JsonConvert.SerializeObject(new {
            phase = audit ? "audit" : "identify",
            response_contract = audit ? new JObject { ["approved"] = "boolean", ["seed_digest"] = MaterialHash(job.CountryAdmissionSeed),
                ["reason"] = "nonempty_string" } : new JObject { ["direct_subjects"] = "array_of_kind_id_source_action_target" },
            hero_source_keys = sourceIndex["hero"],
            entity_source_keys = new JObject { ["kingdom"] = sourceIndex["kingdom"], ["settlement"] = sourceIndex["settlement"] },
            candidates, required_parties = AdmissionRequiredParties(round),
            selected_candidate = job.EventSelectedCandidate, trigger_material = job.EventTriggerMaterial,
            sources, narrative_actors = NarrativeActors(candidates),
            seed = job.CountryAdmissionSeed,
            seed_digest = MaterialHash(job.CountryAdmissionSeed), repair_feedback = job.MaterialRepairFeedback
        });
        job.RoundConversationRevision = round.ConversationRevision;
        FinishMaterialRequest(job);
        // FinishMaterialRequest may remove optional sources. This index must not
        // advertise evidence absent from the final request; filtering only shrinks it.
        var request = JObject.Parse(job.UserPrompt);
        sourceIndex = AdmissionSourceIndex((JArray)request["sources"]);
        request["hero_source_keys"] = sourceIndex["hero"];
        request["entity_source_keys"] = new JObject { ["kingdom"] = sourceIndex["kingdom"], ["settlement"] = sourceIndex["settlement"] };
        job.UserPrompt = request.ToString(Formatting.None);
    }

    // Index only the bounded evidence already supplied by the real material
    // owner. Prose mentions are not identity evidence and never enter this map.
    private static JObject AdmissionSourceIndex(JArray sources)
    {
        var index = new JObject { ["hero"] = new JObject(), ["kingdom"] = new JObject(), ["settlement"] = new JObject() };
        void Add(string kind, string id, string key)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(key)) return;
            if (index[kind][id] is not JArray keys) index[kind][id] = keys = new JArray();
            if (!keys.Values<string>().Contains(key)) keys.Add(key);
        }
        foreach (var source in sources.OfType<JObject>())
        {
            string key = (string)source["key"], knownBy = (string)source["known_by"];
            foreach (string hero in (source["hero_ids"] as JArray)?.Values<string>() ?? Enumerable.Empty<string>()) Add("hero", hero, key);
            if (!string.IsNullOrEmpty(knownBy) && knownBy != "public") Add("kingdom", knownBy, key);
            if (key != "world:current" || (string)source["source_kind"] != "current_world") continue;
            JArray world;
            try { world = JArray.Parse((string)source["text"] ?? "[]"); }
            catch (JsonException) { continue; }
            foreach (var row in world)
            {
                if (row.Type == JTokenType.String) { Add("kingdom", (string)row, key); continue; }
                if (row is not JObject realm) continue;
                Add("kingdom", (string)realm["kingdom_id"], key);
                foreach (var town in (realm["towns"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
                    Add("settlement", (string)town["id"], key);
            }
        }
        return index;
    }

    private bool ResolveAdmissionTarget(WorldDiplomacyJob job, JToken target, JObject index, out JObject entity, out string reason)
    {
        entity = null; reason = "target_requires_entity_kind_id_source_or_nonempty_description";
        if (target is JObject structured)
        {
            if (structured["kind"]?.Type != JTokenType.String || structured["id"]?.Type != JTokenType.String
                || structured["source"]?.Type != JTokenType.String) return false;
            string kind = (string)structured["kind"], id = (string)structured["id"], source = (string)structured["source"];
            if (kind is not "hero" and not "kingdom" and not "settlement" || string.IsNullOrWhiteSpace(id) || id.Length > 300) return false;
            var keys = index[kind][id] as JArray;
            if (keys?.Values<string>().Contains(source) != true)
            {
                reason = "target_identity_not_in_source:" + JsonConvert.SerializeObject(new {
                    kind, id, supplied_source = source?.Length > 300 ? source.Substring(0, 300) : source,
                    allowed_source_keys = keys ?? new JArray() });
                return false;
            }
            entity = structured; reason = ""; return true;
        }
        if (target?.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)target)) return false;
        string legacy = (string)target;
        // Preserve descriptive legacy targets verbatim. Only an exact ID in the
        // supplied identity index can name an entity. Do not probe every entity
        // type: a failed native hero lookup can fall back to a full hero scan.
        if (legacy.Length > 300 || legacy.Any(char.IsWhiteSpace)) { reason = ""; return true; }
        var kinds = new List<string>();
        if (job.CandidateKingdomIds.Contains(legacy) || index["kingdom"][legacy] != null) kinds.Add("kingdom");
        foreach (string kind in new[] { "hero", "settlement" })
            if (index[kind][legacy] != null) kinds.Add(kind);
        if (kinds.Count == 0) { reason = ""; return true; }
        if (kinds.Count != 1) { reason = "target_identity_ambiguous:use_explicit_kind_id_source"; return false; }
        string selectedKind = kinds[0];
        var available = index[selectedKind][legacy] as JArray;
        if (available == null || available.Count == 0)
        { reason = "target_identity_source_missing:" + selectedKind + ":" + legacy; return false; }
        entity = new JObject { ["kind"] = selectedKind, ["id"] = legacy, ["source"] = available[0].DeepClone() };
        reason = ""; return true;
    }

    private void PinAdmissionTargetSources(WorldDiplomacyJob job, JObject seed, JObject index)
    {
        foreach (var subject in (seed["direct_subjects"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
            if (ResolveAdmissionTarget(job, subject["target"], index, out var target, out _) && target != null)
            {
                string key = (string)target["source"];
                if (!job.AdmissionRequiredSourceKeys.Contains(key)) job.AdmissionRequiredSourceKeys.Add(key);
            }
    }

    // Shared Recovery owns suspension, timing and the three-attempt cap. Keep
    // the same evidence during bounded correction; exhausted disposable drafts
    // can advance bounded research or release their slot, protected cases cannot.
    private static bool IsAdmissionProtocolFailure(WorldDiplomacyJob job, string reason) =>
        reason?.StartsWith("admission_seed_protocol_", StringComparison.Ordinal) == true
        || reason == "pipeline_json_invalid" && job.Kind == "event_concept"
            && job.EventSelectionStage is "expand" or "player" or "oral" && string.IsNullOrEmpty(job.CountryAdmission);

    private static bool IsLegacyAdmissionProtocolFailure(WorldDiplomacyJob job, string reason) =>
        job.Kind == "event_concept" && string.IsNullOrEmpty(job.CountryAdmission)
        && (reason == "admission_seed_audit_incomplete" || reason?.StartsWith("admission_seed_invalid:", StringComparison.Ordinal) == true);

    private static void ResetAdmissionRepairState(WorldDiplomacyJob job) => job.AdmissionSeedRewrites = 0;

    private bool HandleSingleCountryAdmission(WorldDiplomacyJob job, WorldDiplomacyRound round)
    {
        var admission = ReadAdmission(job.CountryAdmission);
        if (admission.Admitted.Count != 1 || _host.IsPlayerParty(admission.Admitted[0])) return false;
        string country = admission.Admitted[0];
        string identityFailure = FrozenAdmissionIdentityFailure(admission);
        if (identityFailure != null)
        {
            if (TryRetireInvalidatedAdmission(job, round)) return true;
            HoldTechnicalRequest(job, "admission_identity_single_country_changed:" + identityFailure);
            return true;
        }
        // An existing requested answer has its own limited respondent lane.
        if (round.PlayerResponses.Any(x => x.Status == "pending" && x.KingdomId != country
            && _host.HasIndependentAuthority(x.KingdomId) && !_host.IsEliminatedParty(x.KingdomId))) return false;
        const string reason = "single_admitted_npc_has_no_external_counterparty";
        if (job.EventSelectionStage is not "player" and not "oral" && CanContinueMaterialSearch(round))
        {
            // Preparation changed stage without composing its new request. Do
            // not dispatch the old audit prompt as a candidate-writing request.
            job.RequestPreparationPending = true;
            NoQualifiedMaterial(job, round, reason);
        }
        else
            HoldTechnicalRequest(job, "admission_identity_existing_case_review:" + reason);
        return true;
    }

    private bool CompleteAdmissionSeed(WorldDiplomacyJob job, WorldDiplomacyRound round, JObject response)
    {
        if (!string.IsNullOrEmpty(job.CountryAdmission)) return false;
        if (string.IsNullOrEmpty(job.CountryAdmissionSeed))
        {
            if (!ValidateAdmissionSeed(job, round, response, out _, out _, out string reason))
            { SuspendDiplomacyJob(job, "admission_seed_protocol_invalid:" + reason); return true; }
            job.CountryAdmissionSeed = response.ToString(Formatting.None);
            PinAdmissionTargetSources(job, response, AdmissionSourceIndex(JArray.Parse(job.EventMaterialSnapshot)));
            job.MaterialRepairFeedback = ""; job.TechnicalRecoveryAttempts = 0;
            job.IsRunning = false; job.LlmMessages?.Clear();
            return true;
        }
        // Independent review covers whether the seed's subjects are direct
        // actors/objects, not whether any scored third country is worthwhile.
        string digest = MaterialHash(job.CountryAdmissionSeed);
        string auditError = response["approved"]?.Type != JTokenType.Boolean ? "approved_requires_boolean"
            : response["seed_digest"]?.Type != JTokenType.String || (string)response["seed_digest"] != digest
                ? "seed_digest_must_equal_input_seed_digest:" + digest
            : response["reason"]?.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)response["reason"])
                ? "reason_requires_nonempty_string" : "";
        if (auditError.Length > 0)
        { SuspendDiplomacyJob(job, "admission_seed_protocol_audit:" + auditError); return true; }
        if (!(bool)response["approved"])
        {
            // Semantic re-identification is separately bounded across audit/identify
            // transitions and reload. A format correction never consumes this count.
            job.AdmissionSeedRewrites++;
            bool exhausted = job.AdmissionSeedRewrites >= 3;
            if (!exhausted) job.CountryAdmissionSeed = "";
            job.TechnicalRecoveryAttempts = exhausted ? 2 : 0;
            SuspendDiplomacyJob(job, "admission_seed_protocol_rejected:"
                + (exhausted ? "seed_rewrites_exhausted:" : "reidentify_direct_subjects:") + (string)response["reason"]);
            // The rejection consumed the semantic budget, not an attempt in the
            // next identification phase. Its protocol budget starts at zero.
            if (!exhausted) job.TechnicalRecoveryAttempts = 0;
            return true;
        }
        var seed = JObject.Parse(job.CountryAdmissionSeed);
        if (!ValidateAdmissionSeed(job, round, seed, out var direct, out var heroes, out string error))
        {
            if (!TryRetireInvalidatedAdmission(job, round))
                SuspendDiplomacyJob(job, "admission_identity_seed_changed:" + error);
            return true;
        }
        try
        {
            var host = _host as IDiplomacyCountryAdmissionHost ?? throw new InvalidOperationException("admission_snapshot_host_missing");
            var candidates = job.CandidateKingdomIds.Where(x => _host.HasIndependentAuthority(x) && !_host.IsEliminatedParty(x)
                && (!_host.IsPlayerParty(x) || direct.Contains(x))).Distinct().ToList();
            var result = DiplomacyCountryAdmission.Calculate(host.CaptureCountryAdmission(direct, heroes, candidates),
                host.AdmissionCountryCountPreference, job.JobId);
            result.Seed = job.CountryAdmissionSeed;
            job.CountryAdmission = JsonConvert.SerializeObject(result);
            round.CountryAdmissionVersion = DiplomacyCountryAdmission.CurrentVersion;
            job.Kind = "event_concept";
            job.PrivacyDraft = ""; job.MaterialRepairFeedback = ""; job.TechnicalRecoveryAttempts = 0;
            job.IsRunning = false; job.LlmMessages?.Clear();
            _host.Log("diplomacy.country_admission job=" + job.JobId + " result=" + job.CountryAdmission);
        }
        catch (Exception ex) { SuspendDiplomacyJob(job, "admission_capture_failed:" + ex.GetType().Name + ":" + ex.Message); }
        return true;
    }

    private bool ValidateAdmissionSeed(WorldDiplomacyJob job, WorldDiplomacyRound round, JObject seed,
        out List<string> direct, out List<string> heroes, out string reason)
    {
        direct = AdmissionRequiredParties(round); heroes = new(); reason = "direct_subjects_action_target_source_required";
        if (_host is not IDiplomacyCountryAdmissionHost host) { reason = "snapshot_host_missing"; return false; }
        if (seed["direct_subjects"] is not JArray subjects || subjects.Count == 0 || subjects.Count > 16) return false;
        var material = JArray.Parse(job.EventMaterialSnapshot);
        var sources = material.OfType<JObject>().ToDictionary(x => (string)x["key"], StringComparer.Ordinal);
        var sourceIndex = AdmissionSourceIndex(material);
        var authorizedPlayerParties = new HashSet<string>(direct, StringComparer.Ordinal);
        foreach (var token in subjects)
        {
            if (token is not JObject subject || subject["kind"]?.Type != JTokenType.String || subject["id"]?.Type != JTokenType.String
                || subject["source"]?.Type != JTokenType.String || subject["action"]?.Type != JTokenType.String
                || string.IsNullOrWhiteSpace((string)subject["action"]) || ((string)subject["action"]).Length > 1000) return false;
            string kind = (string)subject["kind"], id = (string)subject["id"];
            if (!sources.TryGetValue((string)subject["source"], out var source))
            { reason = AdmissionSourceError("source_not_supplied", subject, sources.Values); return false; }
            string country = host.AdmissionSubjectCountry(kind, id);
            if (string.IsNullOrEmpty(country) || !job.CandidateKingdomIds.Contains(country)
                || !_host.HasIndependentAuthority(country) || _host.IsEliminatedParty(country))
            { reason = "subject_identity_or_authority_invalid:" + id; return false; }
            if (kind == "hero")
            {
                if ((source["hero_ids"] as JArray)?.Values<string>().Contains(id) != true)
                { reason = AdmissionSourceError("core_hero_not_in_source", subject, sources.Values); return false; }
                heroes.Add(id);
            }
            if (_host.IsPlayerParty(country) && !authorizedPlayerParties.Contains(country))
            { reason = "player_contact_not_authorized"; return false; }
            direct.Add(country);
            if (!ResolveAdmissionTarget(job, subject["target"], sourceIndex, out var target, out reason)) return false;
            if (target == null) continue;
            string targetKind = (string)target["kind"], targetId = (string)target["id"];
            string targetCountry = host.AdmissionSubjectCountry(targetKind, targetId);
            if (string.IsNullOrEmpty(targetCountry) || !job.CandidateKingdomIds.Contains(targetCountry)
                || !_host.HasIndependentAuthority(targetCountry) || _host.IsEliminatedParty(targetCountry))
            { reason = "target_identity_or_authority_invalid:" + targetId; return false; }
            if (_host.IsPlayerParty(targetCountry) && !authorizedPlayerParties.Contains(targetCountry))
            { reason = "player_target_contact_not_authorized"; return false; }
            direct.Add(targetCountry);
            if (targetKind == "hero") heroes.Add(targetId);
        }
        direct = direct.Distinct(StringComparer.Ordinal).ToList(); heroes = heroes.Distinct(StringComparer.Ordinal).ToList();
        reason = ""; return true;
    }

    private static string AdmissionSourceError(string code, JObject subject, IEnumerable<JObject> sources)
    {
        string id = (string)subject["id"], supplied = (string)subject["source"];
        var allowed = sources.Where(x => (string)subject["kind"] != "hero"
            || (x["hero_ids"] as JArray)?.Values<string>().Contains(id) == true).Select(x => (string)x["key"]);
        return code + ":" + JsonConvert.SerializeObject(new {
            subject_kind = (string)subject["kind"], subject_id = id,
            supplied_source = supplied.Length <= 300 ? supplied : supplied.Substring(0, 300),
            allowed_source_keys = allowed.ToArray()
        });
    }

    private DiplomacyAdmissionResult ReadAdmission(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        var result = JsonConvert.DeserializeObject<DiplomacyAdmissionResult>(json);
        if (result == null || (result.Version != 1 && result.Version != DiplomacyCountryAdmission.CurrentVersion)
            || result.Input?.Direct == null || result.Input.Direct.Count == 0
            || result.Input.Heroes == null || result.Input.Pairs == null || result.Scores == null || result.Admitted == null
            || result.Admitted.Any(string.IsNullOrWhiteSpace) || result.Admitted.Distinct().Count() != result.Admitted.Count
            || result.Input.Direct.Any(x => !result.Admitted.Contains(x))
            || result.Input.Heroes.Any(x => string.IsNullOrWhiteSpace(x.Key) || string.IsNullOrWhiteSpace(x.Value)
                || !result.Input.Direct.Contains(x.Value))
            || result.Input.Pairs.Any(x => x == null || x.Relations == null))
            throw new InvalidOperationException("invalid_frozen_admission_snapshot");
        // Version 1 is read-only historical evidence. Never rerun its threshold
        // algorithm; only the explicit unpublished-job migration can replace it.
        if (result.Version == DiplomacyCountryAdmission.CurrentVersion
            && (result.Threshold != 0 || result.Preference < 0 || result.Preference > 2
                || result.SampledCount < 2 || result.SampledCount > 4
                || result.TargetCount != result.Admitted.Count || result.DrawSeed == null
                || result.Input.Candidates == null
                || result.TargetCount != Math.Min(result.Input.Candidates.Concat(result.Input.Direct).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Count(),
                    Math.Max(result.Input.Direct.Distinct().Count(), result.SampledCount))
                || result.Admitted.Any(x => !result.Input.Direct.Contains(x) && !result.Input.Candidates.Contains(x))))
            throw new InvalidOperationException("invalid_frozen_selection_snapshot");
        return result;
    }

    private bool ValidateFrozenAdmissionSnapshot(WorldDiplomacyJob job)
    {
        try { return ReadAdmission(job.CountryAdmission) != null; }
        catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException)
        { HoldTechnicalRequest(job, "admission_snapshot_invalid:" + ex.Message); return false; }
    }

    // One-way upgrade of unpublished planning work. Historical public events
    // retain their cast; version 2 retries never enter this migration again.
    private bool UpgradeLegacyCountrySelection(WorldDiplomacyJob job, WorldDiplomacyRound round)
    {
        if (round == null || round.CountryAdmissionVersion != 1 || round.EventPhase != "forming"
            || !IsEventPlanningJob(job) || string.IsNullOrEmpty(job.CountryAdmission)
            || round.TechnicalHoldReason is "event_document_execution_uncertain" or "legacy_unverified_work_preserved"
            || Storage.EventAnnouncements.Any(x => x.EventId == round.RoundId && x.Stage == "started")
            || Storage.EventEffects.Any(x => x.EventId == round.RoundId && x.Status is "applying" or "uncertain")) return false;
        try
        {
            var previous = ReadAdmission(job.CountryAdmission);
            if (previous.Version != 1 || string.IsNullOrEmpty(previous.Seed)) return false;
            if (FrozenAdmissionIdentityFailure(previous) != null) return false;
            var host = _host as IDiplomacyCountryAdmissionHost;
            if (host == null) return false;
            var comparison = JObject.Parse(job.EventComparison);
            var direct = previous.Input.Direct;
            var candidates = job.CandidateKingdomIds.Where(x => _host.HasIndependentAuthority(x) && !_host.IsEliminatedParty(x)
                && (!_host.IsPlayerParty(x) || direct.Contains(x))).Distinct().ToList();
            var selected = DiplomacyCountryAdmission.Calculate(host.CaptureCountryAdmission(direct, previous.Input.Heroes.Keys.ToList(), candidates),
                host.AdmissionCountryCountPreference, job.JobId);
            selected.Seed = previous.Seed;
            job.CountryAdmission = JsonConvert.SerializeObject(selected);
            round.CountryAdmissionVersion = DiplomacyCountryAdmission.CurrentVersion;
            job.Kind = "event_concept"; job.PrivacyDraft = ""; job.MaterialRepairFeedback = "";
            job.PrivacyAuditRetries = 0; job.TechnicalRecoveryAttempts = 0;
            job.SuspendedReason = ""; job.RetryAfterUtcTicks = 0; job.NextRecoveryProbeUtcTicks = 0;
            job.IsRunning = false; job.LlmMessages?.Clear();
            job.AdmissionSupplementedCountries.Clear();
            comparison["supplemented"] = false;
            job.EventComparison = comparison.ToString(Formatting.None);
            if (!Storage.Jobs.Any(x => x.RoundId == round.RoundId && !string.IsNullOrEmpty(x.SuspendedReason)))
            {
                AccrueTechnicalPause(round); round.TechnicalHoldStartedHour = -1;
                round.TechnicalHoldReason = ""; round.TechnicalRequestHoldReason = "";
            }
            MarkEventDirty(round);
            _host.Log("diplomacy.country_selection_upgraded job=" + job.JobId + " result=" + job.CountryAdmission);
            return true;
        }
        catch (Exception ex)
        { HoldTechnicalRequest(job, "admission_selection_upgrade_failed:" + ex.GetType().Name); return false; }
    }

    // Cold preparation/completion boundaries. Include the failing identity and
    // both owners in diagnostics; a generic "changed" cannot explain a hold.
    private string FrozenAdmissionIdentityFailure(DiplomacyAdmissionResult admission)
    {
        foreach (string country in admission.Admitted)
        {
            if (_host.IsEliminatedParty(country)) return "kingdom_eliminated:" + country;
            if (!_host.HasIndependentAuthority(country)) return "kingdom_independent_authority_unavailable:" + country;
        }
        if (_host is not IDiplomacyCountryAdmissionHost identity) return "country_admission_host_missing";
        foreach (var hero in admission.Input.Heroes)
        {
            string actual = identity.AdmissionSubjectCountry("hero", hero.Key);
            if (actual == hero.Value) continue;
            string detail = "";
            if (_host is IDiplomacyAdmissionIdentityHost reader)
            {
                var state = reader.ReadAdmissionIdentity("hero", hero.Key, hero.Value);
                detail = ":state=" + state.State + ":detail=" + state.Reason;
            }
            return "hero_country_mismatch:" + hero.Key + ":expected=" + hero.Value + ":actual=" + (actual ?? "<unavailable>") + detail;
        }
        return null;
    }

    // Called only by the throttled recovery probe, after uncertain-effect guards.
    // A formerly unknown identity becoming valid is a changed input even when
    // the conversation/configuration did not change. Never refill a format,
    // membership, single-country-policy or corrupt-snapshot failure this way.
    private bool HasRecoveredAdmissionIdentity(WorldDiplomacyJob job, WorldDiplomacyRound round)
    {
        string reason = job.SuspendedReason ?? "";
        bool seedFailure = reason.StartsWith("admission_identity_seed_changed:", StringComparison.Ordinal)
            || reason.StartsWith("admission_seed_identity_changed", StringComparison.Ordinal);
        bool frozenFailure = reason == "admission_identity_single_country_changed"
            || reason == "admission_identity_unavailable_requires_recovery"
            || reason.StartsWith("admission_identity_single_country_changed:", StringComparison.Ordinal)
            || reason.StartsWith("admission_identity_unavailable_requires_recovery:", StringComparison.Ordinal)
            || reason.StartsWith("admission_core_identity_changed_requires_recovery", StringComparison.Ordinal)
            || reason.StartsWith("admission_identity_publication_hold:hero_country_mismatch:", StringComparison.Ordinal)
            || reason.StartsWith("admission_identity_publication_hold:kingdom_independent_authority_unavailable:", StringComparison.Ordinal);
        if (!seedFailure && !frozenFailure) return false;
        try
        {
            if (seedFailure)
                return !string.IsNullOrEmpty(job.CountryAdmissionSeed)
                    && ValidateAdmissionSeed(job, round, JObject.Parse(job.CountryAdmissionSeed), out _, out _, out _);
            string snapshot = round.EventPhase == "opening" ? round.CountryAdmission : job.CountryAdmission;
            var admission = ReadAdmission(snapshot);
            return admission != null && FrozenAdmissionIdentityFailure(admission) == null;
        }
        catch (Exception) { return false; } // Unreadable/unknown evidence retains its hold.
    }

    // Cold request/completion boundaries and the held-job recovery probe only.
    // Inspect the already bounded seed/frozen membership, never all game objects.
    private bool TryRetireInvalidatedAdmission(WorldDiplomacyJob job, WorldDiplomacyRound round)
    {
        if (round == null || _host is not IDiplomacyAdmissionIdentityHost identities) return false;
        var subjects = new List<(string Kind, string Id, string Expected)>();
        string invalidationReason = null;
        try
        {
            bool opening = round.EventPhase == "opening";
            if (opening && (round.CountryAdmissionVersion == 0 || string.IsNullOrEmpty(round.CountryAdmission))) return false;
            string snapshot = opening ? round.CountryAdmission : job.CountryAdmission;
            if (!string.IsNullOrEmpty(snapshot))
            {
                var admission = ReadAdmission(snapshot);
                if (admission == null || admission.Input.Heroes.Any(x => string.IsNullOrWhiteSpace(x.Key) || string.IsNullOrWhiteSpace(x.Value))) return false;
                subjects.AddRange(admission.Admitted.Select(x => ("kingdom", x, (string)null)));
                subjects.AddRange(admission.Input.Heroes.Select(x => ("hero", x.Key, x.Value)));
            }
            else
            {
                // Missing a required frozen snapshot is recovery, never retirement.
                if (round.EventPhase != "forming" || job.Kind != "event_concept" || !string.IsNullOrEmpty(round.CountryAdmission)
                    || job.AdmissionSupplementedCountries.Count != 0 || string.IsNullOrEmpty(job.CountryAdmissionSeed)) return false;
                var seed = JObject.Parse(job.CountryAdmissionSeed);
                if (seed["direct_subjects"] is not JArray actors || actors.Count == 0 || actors.Count > 16) return false;
                var material = JArray.Parse(job.EventMaterialSnapshot);
                var index = AdmissionSourceIndex(material);
                foreach (var actor in actors)
                {
                    if (actor is not JObject item || item["kind"]?.Type != JTokenType.String || item["id"]?.Type != JTokenType.String
                        || item["source"]?.Type != JTokenType.String || item["action"]?.Type != JTokenType.String
                        || string.IsNullOrWhiteSpace((string)item["action"]) || ((string)item["action"]).Length > 1000) return false;
                    string kind = (string)item["kind"], id = (string)item["id"], key = (string)item["source"];
                    if (kind is not "hero" and not "kingdom" and not "settlement" || string.IsNullOrWhiteSpace(id) || id.Length > 300
                        || !material.OfType<JObject>().Any(x => (string)x["key"] == key)
                        || kind == "hero" && (index["hero"][id] as JArray)?.Values<string>().Contains(key) != true) return false;
                    subjects.Add((kind, id, null));
                    if (!ResolveAdmissionTarget(job, item["target"], index, out var target, out _)) return false;
                    if (target != null) subjects.Add(((string)target["kind"], (string)target["id"], null));
                }
            }
            foreach (var subject in subjects.Distinct())
            {
                var state = identities.ReadAdmissionIdentity(subject.Kind, subject.Id, subject.Expected);
                if (state.State == DiplomacyAdmissionIdentityState.ConfirmedInvalidated && !string.IsNullOrEmpty(state.Reason))
                { invalidationReason = "admission_confirmed_invalidated:" + subject.Kind + ":" + subject.Id + ":" + state.Reason; break; }
                if (subject.Kind == "kingdom" && state.State == DiplomacyAdmissionIdentityState.Valid
                    && !_host.HasIndependentAuthority(subject.Id))
                { invalidationReason = "admission_confirmed_invalidated:kingdom:" + subject.Id + ":independent_authority_lost"; break; }
                // A hero may still exist in an explicitly eliminated kingdom.
                if (state.State != DiplomacyAdmissionIdentityState.Valid || string.IsNullOrWhiteSpace(state.Country) || subject.Kind == "kingdom") continue;
                var country = identities.ReadAdmissionIdentity("kingdom", state.Country, null);
                if (country.State == DiplomacyAdmissionIdentityState.ConfirmedInvalidated && !string.IsNullOrEmpty(country.Reason))
                { invalidationReason = "admission_confirmed_invalidated:kingdom:" + state.Country + ":" + country.Reason; break; }
            }
        }
        catch (Exception) { return false; } // Unknown read/shape failure retains the original hold and evidence.
        return invalidationReason != null && RetireConfirmedInvalidatedMaterial(job, round, invalidationReason);
    }

    private bool CheckOpeningAdmission(WorldDiplomacyJob job, WorldDiplomacyRound round)
    {
        if (job.ForcedIntent != "started" || round.CountryAdmissionVersion == 0
            || Storage.EventAnnouncements.Any(x => x.EventId == round.RoundId && x.Stage == "started")) return true;
        try
        {
            var admission = ReadAdmission(round.CountryAdmission);
            if (admission == null || admission.Input == null || admission.Admitted == null)
                throw new InvalidOperationException("snapshot_missing_or_invalid");
            string identityFailure = FrozenAdmissionIdentityFailure(admission);
            if (identityFailure != null) throw new InvalidOperationException(identityFailure);
            if (admission.Admitted.Any(x => !round.Participants.Any(p => p.KingdomId == x))
                || round.Participants.Any(x => x.State != "observer" && !admission.Admitted.Contains(x.KingdomId)))
                throw new InvalidOperationException("frozen_membership_or_identity_changed");
            return true;
        }
        catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException)
        {
            if (!TryRetireInvalidatedAdmission(job, round))
                HoldTechnicalRequest(job, "admission_identity_publication_hold:" + ex.Message);
            return false;
        }
    }

    private bool CheckFrozenAdmission(WorldDiplomacyJob job, JObject concept, out string reason)
    {
        reason = "";
        var admission = ReadAdmission(job.CountryAdmission);
        if (admission == null) { reason = "admission_missing"; return false; }
        if (concept["participants"] is not JArray parties || parties.Any(x => x is not JObject)
            || !new HashSet<string>(parties.OfType<JObject>().Select(x => (string)x["kingdom"]), StringComparer.Ordinal).SetEquals(admission.Admitted))
        { reason = "admission_participants_must_match_frozen_list:" + string.Join(",", admission.Admitted); return false; }
        string identityFailure = FrozenAdmissionIdentityFailure(admission);
        if (identityFailure != null)
        { reason = "admission_identity_unavailable_requires_recovery:" + identityFailure; return false; }
        // Compatibility projection only. Model-written included/reason values
        // have no authority; the full narrative and position audit still runs.
        concept["party_review"] = new JArray(job.CandidateKingdomIds.Select(x => new JObject {
            ["kingdom"] = x, ["included"] = admission.Admitted.Contains(x), ["reason"] = "frozen_program_selection",
            ["reply_only"] = !admission.Admitted.Contains(x) && ResolveRound(job.RoundId).PlayerResponses.Any(p => p.KingdomId == x && p.Status == "pending")
        }));
        return true;
    }

    private bool EnsureOralAdmissionPlan(WorldDiplomacyRound round)
    {
        if (round.CountryAdmissionVersion == 0 || round.EventSourceType != "dialogue_commitment"
            || !string.IsNullOrEmpty(round.EventPartyReview)) return true;
        if (Storage.Jobs.Any(x => x.RoundId == round.RoundId && IsEventPlanningJob(x))) return false;
        var oral = Storage.DialogueArrangements.FirstOrDefault(x => x.ArrangementId == round.DialogueArrangementId);
        if (oral == null) return false;
        var candidates = _host.AllKingdomIds().Where(x => _host.HasIndependentAuthority(x) && !_host.IsEliminatedParty(x)).Distinct().ToList();
        round.EventPhase = "forming";
        EnqueueJob(new WorldDiplomacyJob { JobId = _host.NewId("event_concept"), Kind = "event_concept",
            RoundId = round.RoundId, ExchangeId = round.RoundId, EventSelectionStage = "oral",
            AuthorKingdomId = oral.ActorKingdomId, CandidateKingdomIds = candidates,
            EventInvestigationKingdoms = new() { oral.ActorKingdomId, oral.TargetKingdomId },
            EventTriggerMaterial = JsonConvert.SerializeObject(new { oral.ActorKingdomId, oral.TargetKingdomId,
                oral.Action, oral.Terms, oral.SourcePlayerText, oral.SourceNpcText, conditions_are_confirmed = true }),
            EventSelectedCandidate = "{}", EventComparison = "{}", Priority = 110,
            CreatedDay = _host.CurrentDay(), MaxTokens = _host.AnalysisMaxTokens() });
        return false;
    }
}
