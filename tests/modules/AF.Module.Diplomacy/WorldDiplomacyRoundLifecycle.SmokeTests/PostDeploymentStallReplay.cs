using System.Reflection;
using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class MaterialSupplyReplay
{
    internal static void PostDeploymentStalls()
    {
        ImmediateCitationRepair(); EmptyResponseRepair(); ReuseReviewedAlternatives();
        ProtectedInputSurvives(); FixedAgendaCapture(); RestoredRepairBudget(); InvalidatedDraftRetirement();
        Console.WriteLine("Post-deployment stalls: actual citation shape, empty response, alternative reuse, bounded exhaustion and protected input PASS; model semantics/live game NOT_RUN.");
    }

    private static void ImmediateCitationRepair()
    {
        var (_, o, j) = Start(); o.PrepareSharedRequest(j);
        var material = JArray.Parse(j.EventMaterialSnapshot);
        material.Add(new JObject { ["key"] = "character:lord_2_4_1", ["known_by"] = "b", ["text"] = "A current relation record, not a persona." });
        j.EventMaterialSnapshot = material.ToString(Formatting.None);
        var bad = Candidate(); bad["material_keys"] = new JArray("persona:lord_2_4_1", "character:lord_2_4_1");
        Complete(o, j, new() { ["candidates"] = new JArray(bad) });
        Test.True(j.MaterialRepairFeedback.Contains("invalid=\"persona:lord_2_4_1\"")
            && j.MaterialRepairFeedback.Contains("character:lord_2_4_1") && j.RetryAfterUtcTicks == 0,
            "06:10:22/55 persona versus character typo identifies exact missing key without 30/60 second waits");
        string frozen = j.EventMaterialSnapshot;
        Test.True(o.CanDispatchDiplomacyJob(j), "actual scheduler immediately resumes first schema repair");
        Complete(o, j, new() { ["candidates"] = new JArray(bad) });
        Test.True(j.RetryAfterUtcTicks == 0 && j.EventMaterialSnapshot == frozen
            && o.CanDispatchDiplomacyJob(j), "second identical typo retains sources and consumes its finite attempt without sleeping");
        bad["material_keys"] = new JArray("character:lord_2_4_1");
        Complete(o, j, new() { ["candidates"] = new JArray(bad) });
        Test.True(j.EventSelectionStage == "compare" && j.TechnicalRecoveryAttempts == 0
            && j.MaterialSearchPass == 0, "corrected citation reaches independent comparison without source reroll");
    }

    private static void EmptyResponseRepair()
    {
        var (_, o, j) = Start(); o.PrepareSharedRequest(j);
        string frozen = j.EventMaterialSnapshot;
        Complete(o, j, new() { ["candidates"] = new JArray() });
        Test.True(j.MaterialSearchPass == 0 && j.EventMaterialSnapshot == frozen
            && j.MaterialRepairFeedback.Contains("empty_reason_missing") && o.CanDispatchDiplomacyJob(j),
            "06:09:58 empty response without reason is corrected on the same evidence, not accepted as a no-result");
        o.PrepareSharedRequest(j);
        Test.True(JObject.Parse(j.UserPrompt)["repair_feedback"]!.ToString().Contains("specific_obstacle"),
            "corrective request requires an actionable obstacle");
        Complete(o, j, new() { ["candidates"] = new JArray(Candidate()) });
        Test.True(j.EventSelectionStage == "compare", "empty-response repair can recover a grounded candidate");
    }

    private static void ReuseReviewedAlternatives()
    {
        var (h, o, j) = Start();
        Complete(o, j, new() { ["candidates"] = new JArray(Candidate(), Candidate("alternative")) });
        var compare = Compare();
        var second = (JObject)compare["assessments"]![0]!.DeepClone(); second["id"] = "alternative";
        ((JArray)compare["assessments"]!).Add(second);
        Complete(o, j, compare);
        string frozen = j.EventMaterialSnapshot; int reads = h.MaterialReads;
        typeof(WorldDiplomacyOrchestration).GetMethod("NoQualifiedMaterial", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(o, new object[] { j, o.ResolveRound(j.RoundId), "single_admitted_npc_has_no_external_counterparty" });
        Test.True(j.EventSelectionStage == "compare" && j.MaterialSearchPass == 0
            && JArray.Parse(j.EventCandidatePool).Count == 1 && j.EventMaterialSnapshot == frozen,
            "one failed candidate cannot erase a reviewed sibling or its evidence");
        Test.True(j.RequestPreparationPending && j.CountryAdmission == "" && j.CountryAdmissionSeed == "",
            "fallback cannot dispatch the stale admission request or inherit the old frozen countries");
        o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
        j = o.CurrentStorage.Jobs.Single(x => x.JobId == j.JobId);
        Test.True(o.CanDispatchDiplomacyJob(j), "reloaded alternative remains dispatchable without a day delay");
        o.PrepareSharedRequest(j);
        var revised = Compare(); revised["assessments"] = new JArray(second); revised["best_equivalent"] = new JArray("alternative");
        Complete(o, j, revised);
        Test.True((string)JObject.Parse(j.EventSelectedCandidate)["id"]! == "alternative" && h.MaterialReads == reads,
            "independent comparison selects remaining evidence without rerunning retrieval");
        typeof(WorldDiplomacyOrchestration).GetMethod("NoQualifiedMaterial", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(o, new object[] { j, o.ResolveRound(j.RoundId), "no_external_recipient" });
        Test.True(j.MaterialSearchPass == 1 && j.EventSelectionStage == "candidates",
            "exhausted sibling pool changes batch once; failed ID cannot cycle back");
    }

    private static void ProtectedInputSurvives()
    {
        var (_, o, j) = Start(); var round = o.ResolveRound(j.RoundId);
        round.EventSourceType = "dialogue_commitment";
        j.MaterialSearchPass = 10;
        typeof(WorldDiplomacyOrchestration).GetMethod("NoQualifiedMaterial", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(o, new object[] { j, round, "controlled_no_result" });
        Test.True(round.State == "active" && o.CurrentStorage.Jobs.Contains(j) && j.MaterialSearchPass == 10
            && o.CurrentStorage.EventAnnouncements.Count == 0, "bounded NPC search never erases oral commitments or invents publication");
    }

    private static void FixedAgendaCapture()
    {
        var(h,o,j)=Start(); var round=o.ResolveRound(j.RoundId);
        round.Participants = new() { new() { KingdomId="a" }, new() { KingdomId="b" } };
        j.EventTriggerMaterial = new JObject { ["known_by"]="b",["decision_reason"]="Request clarification of the prior oath" }.ToString();
        o.PrepareSharedRequest(j);
        Test.True(h.Searches.Count==1 && h.Searches[0].Countries.OrderBy(x=>x).SequenceEqual(new[]{"a","b"})
            && j.EventExplorationTheme=="", "fixed bilateral agenda retrieves its actual parties directly without random-theme navigation or unrelated countries");
    }

    private static void RestoredRepairBudget()
    {
        var(h,o,j)=Start(); o.PrepareSharedRequest(j);
        j.SuspendedReason="event_candidate_evidence_invalid:material_keys_must_reference_supplied_sources";
        j.TechnicalRecoveryAttempts=2; j.RetryAfterUtcTicks=DateTime.UtcNow.AddSeconds(60).Ticks;
        j.NextRecoveryProbeUtcTicks=DateTime.UtcNow.AddSeconds(60).Ticks;
        Test.True(o.CanDispatchDiplomacyJob(j) && j.TechnicalRecoveryAttempts==2,
            "restored pre-fix candidate backoff is removed without refilling its failure budget");
        j.MaterialSearchPass=5; j.SuspendedReason="admission_seed_audit_incomplete:seed_digest_mismatch";
        j.TechnicalRecoveryAttempts=3;
        Test.True(!o.CanDispatchDiplomacyJob(j) && !o.CurrentStorage.Jobs.Contains(j)
            && o.CurrentStorage.CompletedRounds.Any(x=>x.RoundId==j.RoundId),
            "old exhausted ordinary draft releases its slot instead of remaining a permanent hold");
        Test.True(o.CurrentStorage.EventAnnouncements.Count==0 && h.Writes==0,
            "restored draft cleanup cannot manufacture publication or execute a diplomatic effect");
    }

    private static void InvalidatedDraftRetirement()
    {
        var retire=typeof(WorldDiplomacyOrchestration).GetMethod("RetireConfirmedInvalidatedMaterial",BindingFlags.Instance|BindingFlags.NonPublic)!;
        foreach(string phase in new[]{"forming","opening"})
        {
            var(h,o,j)=Start();
            Complete(o,j,new(){["candidates"]=new JArray(Candidate())}); Complete(o,j,Compare());
            CountryAdmissionReplay.FreezeFixture(o,j,new[]{"a","b"},false);
            string frozen=j.CountryAdmission,seed=j.CountryAdmissionSeed;
            var round=o.ResolveRound(j.RoundId);round.EventPhase=phase;
            Test.True((bool)retire.Invoke(o,new object[]{j,round,"confirmed_fixture_owner_change"})!,"confirmed invalidation has a bounded pure-draft exit: "+phase);
            var evidence=JObject.Parse(round.EventSelectionEvidence);
            Test.True((string)evidence["admission"]! == frozen && (string)evidence["seed"]! == seed,
                "retiring an invalidated draft retains exact previous admission evidence: "+phase);
            Test.True(j.RequestPreparationPending && o.CurrentStorage.EventAnnouncements.Count==0 && h.Writes==0,
                "invalidation never dispatches the stale prompt or invents an opening: "+phase);
            Test.True(phase=="forming" ? round.State=="active"&&j.MaterialSearchPass==1&&j.CountryAdmission==""
                : round.State=="closed"&&!o.CurrentStorage.Jobs.Contains(j)&&o.CurrentStorage.NextNormalRoundDay>=h.CurrentDayValue+1,
                "forming recaptures; prepublication opening releases its slot: "+phase);
        }
        var(_,protectedOwner,work)=Start();var protectedRound=protectedOwner.ResolveRound(work.RoundId);
        protectedRound.PendingOffers.Add(new(){Status="open"});
        Test.True(!(bool)retire.Invoke(protectedOwner,new object[]{work,protectedRound,"confirmed_fixture_owner_change"})!
            && protectedRound.State=="active"&&protectedOwner.CurrentStorage.Jobs.Contains(work),
            "even a confirmed identity change cannot erase an existing open obligation");
    }
}
