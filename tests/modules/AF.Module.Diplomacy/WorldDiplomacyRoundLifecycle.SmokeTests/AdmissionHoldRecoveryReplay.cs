using System.Reflection;
using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class MaterialSupplyReplay
{
    internal static void AdmissionHoldRecovery()
    {
        foreach (bool restored in new[] { false, true })
        {
            var (h, o, j) = Start();
            Complete(o, j, new() { ["candidates"] = new JArray(Candidate()) }); Complete(o, j, Compare());
            j.CountryAdmission = JsonConvert.SerializeObject(DiplomacyCountryAdmission.Calculate(new() {
                Direct = new() { "b" }, Heroes = new() { ["unregistered_core"] = "b" } }));
            j.CountryAdmissionSeed = "retained seed";
            string material = j.EventMaterialSnapshot, admission = j.CountryAdmission, oldId = j.RoundId;
            if (restored)
            {
                // Exact old diagnostic and exhausted counter from the live incident.
                j.SuspendedReason = "admission_identity_single_country_changed";
                j.TechnicalRecoveryAttempts = 3;
                o.ResolveRound(oldId).TechnicalHoldReason = j.SuspendedReason;
                ReloadAdmissionOwner(o); j = o.CurrentStorage.Jobs.Single(x => x.JobId == j.JobId);
                j.NextRecoveryProbeUtcTicks = DateTime.UtcNow.AddMinutes(1).Ticks;
                Test.True(!o.CanDispatchDiplomacyJob(j) && o.CurrentStorage.Jobs.Contains(j), "legacy recovery obeys the existing cold probe deadline");
                j.NextRecoveryProbeUtcTicks = 0;
                Test.True(!o.CanDispatchDiplomacyJob(j), "restored exhausted owner is never sent to the provider");
            }
            else
            {
                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    if (attempt > 1)
                    {
                        j.RetryAfterUtcTicks = 0; j.NextRecoveryProbeUtcTicks = 0;
                        Test.True(o.CanDispatchDiplomacyJob(j), "bounded identity retry remains dispatchable before exhaustion");
                    }
                    o.PrepareSharedRequest(j);
                    Test.True(j.TechnicalRecoveryAttempts == attempt, "identity failures consume one persisted attempt");
                    Test.True(j.SuspendedReason.Contains("hero_country_mismatch:unregistered_core:expected=b:actual=<unavailable>"),
                        "diagnostic identifies the actual failed read instead of claiming an unexplained identity change");
                    if (attempt < 3) Test.True(o.CurrentStorage.Jobs.Contains(j) && j.CountryAdmission == admission,
                        "transient uncertainty retains frozen evidence until its bounded recovery budget expires");
                }
            }
            Test.True(!o.CurrentStorage.Jobs.Contains(j) && o.CurrentStorage.CompletedRounds.Any(x => x.RoundId == oldId),
                "fresh and legacy exhausted unpublished identity holds release their event slot");
            var archived = o.CurrentStorage.CompletedRounds.Single(x => x.RoundId == oldId);
            var evidence = JObject.Parse(archived.EventSelectionEvidence);
            Test.True((string)evidence["material"]! == material && (string)evidence["admission"]! == admission
                && (string)evidence["seed"]! == "retained seed" && (string)evidence["outcome"]! == "unpublished_admission_recovery_exhausted",
                "unknown identity archives exact original evidence, never invents confirmed death or validity");
            Test.True(o.CurrentStorage.EventAnnouncements.Count == 0 && h.Writes == 0, "failed candidate publishes nothing and executes no effect");
            o.TryScheduleNormalRound();
            Test.True(!o.CurrentStorage.Jobs.Any(x => x.Kind == "event_concept"), "paused time cannot start a paid replacement loop");
            ReloadAdmissionOwner(o);
            Test.True(!o.CurrentStorage.Jobs.Any(x => x.RoundId == oldId)
                && o.CurrentStorage.CompletedRounds.Single(x => x.RoundId == oldId).EventSelectionEvidence == archived.EventSelectionEvidence,
                "reload retains the archived evidence and cannot revive the failed job");
            Test.True(o.CurrentStorage.NextNormalRoundDay <= h.CurrentDayValue + h.NewEventIntervalDays(),
                "abandonment does not add an extra event interval");
            h.CurrentDayValue = o.CurrentStorage.NextNormalRoundDay;
            o.TryScheduleNormalRound();
            var next = o.CurrentStorage.Jobs.Single(x => x.Kind == "event_concept");
            Test.True(next.RoundId != oldId && o.CanDispatchDiplomacyJob(next), "next configured opening admits a different real event owner");
            PublishAfterAdmissionHold(o, next);
            Test.True(o.CurrentStorage.EventAnnouncements.Any(x => x.EventId == next.RoundId && x.Stage == "started"),
                "the replacement completes actual preparation, scoring, independent review and opening publication");
        }
        foreach (string protection in new[] { "document", "announcement", "effect", "arrangement", "offer", "threat", "aid", "reply", "oral", "player", "uncertain", "legacy" })
        {
            var (h, o, j) = Start(); var r = o.ResolveRound(j.RoundId);
            j.SuspendedReason = "admission_identity_single_country_changed"; j.TechnicalRecoveryAttempts = 3;
            if (protection == "document") o.CurrentStorage.Documents.Add(new() { RoundId = r.RoundId });
            if (protection == "announcement") o.CurrentStorage.EventAnnouncements.Add(new() { EventId = r.RoundId });
            if (protection == "effect") o.CurrentStorage.EventEffects.Add(new() { EventId = r.RoundId, Status = "uncertain" });
            if (protection == "arrangement") o.CurrentStorage.DialogueArrangements.Add(new() { RoundId = r.RoundId });
            if (protection == "offer") r.PendingOffers.Add(new() { Status = "open" });
            if (protection == "threat") o.CurrentStorage.DiplomaticThreats.Add(new() { StageRoundId = r.RoundId, Status = "open" });
            if (protection == "aid") o.CurrentStorage.AidRequests.Add(new() { RoundId = r.RoundId, Status = "accepted" });
            if (protection == "reply") r.PlayerResponses.Add(new() { Status = "pending" });
            if (protection == "oral") r.EventSourceType = "dialogue_commitment";
            if (protection == "player") j.EventSelectionStage = "player";
            if (protection == "uncertain") r.TechnicalHoldReason = "event_document_execution_uncertain";
            if (protection == "legacy") r.TechnicalHoldReason = "legacy_unverified_work_preserved";
            Test.True(!o.CanDispatchDiplomacyJob(j) && r.State == "active" && o.CurrentStorage.Jobs.Contains(j)
                && h.Writes == 0, "exhaustion cannot erase protected work: " + protection);
        }
        foreach (string reason in new[] { "admission_snapshot_invalid:missing", "admission_capture_failed:read", "admission_material_host_missing", "admission_core_identity_changed_requires_recovery" })
        {
            var (_, o, j) = Start(); j.SuspendedReason = reason; j.TechnicalRecoveryAttempts = 3;
            Test.True(!o.CanDispatchDiplomacyJob(j) && !o.CurrentStorage.Jobs.Contains(j), "adjacent exhausted admission failure has a disposable exit: " + reason);
        }
    }

    private static void ReloadAdmissionOwner(WorldDiplomacyOrchestration o) =>
        o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);

    private static void PublishAfterAdmissionHold(WorldDiplomacyOrchestration o, WorldDiplomacyJob j)
    {
        Complete(o, j, new() { ["candidates"] = new JArray(Candidate()) }); Complete(o, j, Compare());
        Complete(o, j, new() {
            ["title"] = "The proposed hearing", ["background"] = "The other court is asked to clarify an old oath.",
            ["initiator"] = "b", ["material_keys"] = new JArray("lore:succession"),
            ["participants"] = new JArray(Position("b", "a"), Position("a", "b")),
            ["narrative"] = new JArray(new JObject { ["kind"] = "allegation", ["subject"] = "b", ["source"] = "lore:succession", ["text"] = "The oath is disputed." }),
            ["party_review"] = new JArray(j.CandidateKingdomIds.Select(id => new JObject { ["kingdom"] = id,
                ["included"] = id is "a" or "b", ["reply_only"] = false, ["reason"] = "Controlled standing review" })) });
        o.PrepareSharedRequest(j); Complete(o, j, MaterialCountryThirdReplay.Approved(j));
        var boundary = o.CurrentStorage.Jobs.Single(x => x.Kind == "event_boundary" && x.RoundId == j.RoundId);
        Complete(o, boundary, new() { ["title"] = "The hearing opens", ["body"] = "The courts acknowledge a disputed undertaking.",
            ["effects"] = new JArray(new JObject { ["kind"] = "ruler_relation", ["kingdom"] = "b", ["other"] = "a", ["delta"] = -1, ["days"] = 0, ["reason"] = "The dispute strains relations." }),
            ["remove_effects"] = new JArray(), ["confirmed_documents"] = new JArray(), ["narrative"] = new JArray() });
        o.PrepareSharedRequest(boundary); Complete(o, boundary, MaterialCountryThirdReplay.Approved(boundary));
    }
}
