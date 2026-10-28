using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class MaterialSupplyReplay
{
    internal static void ActualPlanningIncident(string path)
    {
        var records = JArray.Parse(File.ReadAllText(path)).OfType<JObject>().ToList();
        foreach (string time in new[] { "16:22:16", "16:22:20", "16:22:59", "16:25:25" })
        {
            var record = records.Single(x => (string)x["time"]! == time && (x["draft"]!["assessments"] != null || time == "16:22:59"));
            var input = (JObject)record["input"]!; var original = (JObject)record["draft"]!;
            var (_,o,j) = Start(); var round = o.ResolveRound(j.RoundId);
            j.CandidateKingdomIds = ((JArray)input["candidates"]!).Values<string>().ToList()!;
            round.Participants = ((JArray)input["required_participants"]!).Values<string>()
                .Select(id => new WorldDiplomacyRoundParticipant { KingdomId = id! }).ToList();
            j.EventMaterialSnapshot = input["sources"]!.ToString(Formatting.None);
            j.EventSelectionStage = original["assessments"] != null ? "compare" : "expand";
            j.EventCandidatePool = input["candidate_pool"]?.ToString(Formatting.None) ?? "[]";
            j.EventSelectedCandidate = input["selected_candidate"]?.ToString() ?? "{}";
            j.EventComparison = "{}";
            void Feed(JObject draft)
            {
                typeof(WorldDiplomacyOrchestration).GetMethod("CompleteEventConcept", BindingFlags.Instance|BindingFlags.NonPublic)!
                    .Invoke(o, new object[] { j, draft });
            }
            Feed(original);
            if (time == "16:25:25")
            {
                Test.True(j.MaterialSearchPass == 1 && j.SuspendedReason == "", "actual unqualified comparison starts wider investigation without a technical retry");
                continue;
            }
            Test.True(j.Kind == "event_concept" && !o.CurrentStorage.EventAnnouncements.Any(), "actual invalid draft never publishes before correction and independent audit");
            if (time == "16:22:16")
                Test.True(j.MaterialRepairFeedback.Contains("fields=duplicate_of"), "actual omitted comparison field is identified precisely");
            else if (time == "16:22:20")
                Test.True(j.MaterialRepairFeedback.Contains("aserai_empire_w_trade_rule_security")
                    && j.MaterialRepairFeedback.Contains("aserai_empire_w_trade_route_security"), "actual typo feedback carries invalid and eligible IDs");
            else
            {
                Test.True(j.MaterialRepairFeedback.Contains("basis_keys") && j.MaterialRepairFeedback.Contains("narrative[0].subject=lord_2_1")
                    && j.MaterialRepairFeedback.Contains("narrative[2].subject=lord_6_1"), "actual concept gets all independent country/hero binding errors in its first repair");
                // Controlled correction proves only structural admission, not a real model's
                // semantic judgement. Preserve original prose and require independent audit.
                var repaired = (JObject)original.DeepClone();
                repaired["narrative"]![0]!["subject"] = "sturgia";
                repaired["narrative"]![1]!["subject"] = "sturgia";
                repaired["narrative"]![2]!["subject"] = "khuzait";
                repaired["narrative"]![2]!["basis_keys"] = new JArray("lore:2537355b1a5b7ddb", "character:lord_S8_u");
                repaired["narrative"]![2]!["hero_ids"] = new JArray("lord_S8_u");
                j.SuspendedReason = ""; j.RetryAfterUtcTicks = 0;
                Feed(repaired);
                Test.True(j.Kind == "event_concept_audit" && !o.CurrentStorage.EventAnnouncements.Any(), "actual prose with controlled bindings reaches audit, never bypasses it: " + j.SuspendedReason);
            }
        }
        Console.WriteLine("PASS actual planning incident: four recorded failure responses; corrected binding is controlled, live model/game NOT_RUN.");
    }

    private static void PlanningPerformanceRepairs()
    {
        DeferredCaptureAndDispatch(); DeferredRequestRecovery(); ComparisonRepairDetails(); GroundedParticipation();
        Console.WriteLine("Planning repair: deferred work, dispatch budget, qualified/unqualified comparison, grounded NPC participation and publication PASS; live model/game NOT_RUN.");
    }
    private static void DeferredCaptureAndDispatch()
    {
        using var queue = new WorldDiplomacyMaterialCaptureQueue();
        var worker = new TaskCompletionSource<string>(); int steps = 0, cleanup = 0;
        CancellationToken oldToken = default;
        IEnumerator<string> Work(CancellationToken token)
        {
            oldToken = token;
            try
            {
                while (!worker.Task.IsCompleted) { steps++; yield return null; }
                token.ThrowIfCancellationRequested();
                steps++; yield return worker.Task.GetAwaiter().GetResult();
            }
            finally { cleanup++; }
        }
        var sw = Stopwatch.StartNew();
        Test.True(queue.Advance("job", 1, Work, _ => { }) == null && steps == 1, "one resumable step, no partial evidence");
        Test.True(sw.ElapsedMilliseconds < 1000 && !worker.Task.IsCompleted, "unfinished worker cannot block the game thread");
        Test.True(queue.Advance("job", 1, Work, _ => { }) == null && steps == 2, "same capture is resumed, not restarted");
        worker.SetResult("[{\"key\":\"ready\"}]");
        Test.True(queue.Advance("job", 1, Work, _ => { })!.Contains("ready") && queue.Count == 0 && cleanup == 1,
            "only complete snapshot is published, resources released exactly once");
        worker = new(); steps = 0;
        queue.Advance("old", 1, Work, _ => { }); var previous = oldToken;
        queue.Advance("new", 2, Work, _ => { });
        Test.True(previous.IsCancellationRequested && queue.Count == 1 && cleanup == 2, "load cancels old generation without waiting on worker");
        queue.Dispose(); Test.True(oldToken.IsCancellationRequested && queue.Count == 0 && cleanup == 3, "disable/end cancels transient capture");
        for (int i=0;i<4;i++) queue.Advance("capacity"+i, 3, Work, _ => { });
        Test.True(queue.Count==3,"bounded captures cannot flood the worker queue");
        queue.Dispose();
        int captured = 0;
        IEnumerator<string> ManySteps(CancellationToken token)
        {
            for (int i=0;i<30;i++) { token.ThrowIfCancellationRequested(); captured++; yield return null; }
            yield return "complete";
        }
        queue.Advance("fast", 4, ManySteps, _=>{});
        sw.Restart();
        while (captured<30 && sw.ElapsedMilliseconds<4000) { queue.Pump(4,_=>{}); Thread.Sleep(10); }
        Test.True(captured==30 && sw.ElapsedMilliseconds<4000,
            "thirty capture steps finish independently of the one-second HTTP dispatcher");
        Test.True(queue.Advance("fast",4,ManySteps,_=>{})=="complete" && queue.Count==0,
            "fast pump still releases only the completed snapshot");
        queue.Advance("stale",4,Work,_=>{});var staleToken=oldToken;
        queue.Pump(5,_=>{});
        Test.True(staleToken.IsCancellationRequested && queue.Count==0,"pump cancels old game generation before any further read");

        worker = new();
        queue.Advance("hung", 5, Work, _=>{}); var hungToken = oldToken;
        var entries = (System.Collections.IDictionary)typeof(WorldDiplomacyMaterialCaptureQueue)
            .GetField("_entries", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(queue)!;
        // Age the monotonic timestamp, without sleeping or changing the machine clock.
        var entry = entries["hung"]!;
        entry.GetType().GetField("Started", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(entry, Stopwatch.GetTimestamp() - 181L * Stopwatch.Frequency);
        queue.Pump(5, _=>{});
        bool timedOut = false;
        try { queue.Advance("hung", 5, Work, _=>{}); }
        catch (TimeoutException ex) { timedOut = ex.Message == "material_capture_timeout"; }
        Test.True(timedOut && hungToken.IsCancellationRequested && queue.Count == 0,
            "continuously polled hung worker becomes a recoverable error and releases its capture slot");
        worker.SetResult("late-stale-result");
        Test.True(queue.Advance("replacement", 5, ManySteps, _=>{}) == null,
            "late worker result cannot become replacement evidence");
        queue.Dispose();
        worker = new(); queue.Advance("ready", 6, Work, _=>{});
        worker.SetResult("already-complete"); queue.Pump(6, _=>{});
        entry = entries["ready"]!;
        entry.GetType().GetField("Started", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(entry, Stopwatch.GetTimestamp() - 181L * Stopwatch.Frequency);
        Test.True(queue.Advance("ready", 6, Work, _=>{}) == "already-complete",
            "completed evidence is retained when its consumer polls after the deadline");

        var storage = new WorldDiplomacyStorage();
        var job = new WorldDiplomacyJob { JobId="deferred",Kind="event_concept",SystemPrompt="stale",UserPrompt="stale" };
        storage.Jobs.Add(job); int budgetCalls=0, prepared=0;
        var chosen=WorldDiplomacyLlmDispatchApplication.SelectAndPrepareLlmJob(storage,100,"",_=>"",_=>true,_=>true,_=>true,_=>"",(_,_)=>{},()=>"",
            _=>{budgetCalls++;return true;},_=>{},_=>{prepared++;return new JArray();},out var messages,32000,4800,s=>s.Length,_=>"",()=>{},
            (_,error)=>throw new Exception(error),_=>{},_=>{},eligible:_=>true,prepareShared:j=>{j.RequestPreparationPending=true;j.RetryAfterUtcTicks=0;});
        Test.True(chosen==null && messages==null && budgetCalls==0 && prepared==0,"pending materials never spend an API budget or send the previous prompt");
        Test.True(!JsonConvert.DeserializeObject<WorldDiplomacyJob>(JsonConvert.SerializeObject(job))!.RequestPreparationPending,"transient preparation flag never becomes persisted save state");
    }
    private static void DeferredRequestRecovery()
    {
        var (h,o,j)=Start(); h.DeferCapture=true;
        o.PrepareSharedRequest(j);
        Test.True(j.RequestPreparationPending && j.EventMaterialSnapshot=="" && j.SuspendedReason=="", "pending capture is cooperative work, not an invalid empty snapshot");
        // A deliberately unreadable sentinel makes a repeated archive traversal
        // fail deterministically; the pending poll must only resume capture.
        o.CurrentStorage.EventAnnouncements.Add(null!);
        h.CurrentDayValue++; j.RetryAfterUtcTicks=0; o.PrepareSharedRequest(j);
        o.CurrentStorage.EventAnnouncements.Clear();
        Test.True(h.Searches[0].Countries.SequenceEqual(h.Searches[1].Countries), "fast-forward preserves capture identity while background work is pending");
        h.DeferCapture=false; j.RetryAfterUtcTicks=0; o.PrepareSharedRequest(j);
        Test.True(!j.RequestPreparationPending && j.EventMaterialSnapshot.Length>0 && j.SystemPrompt.Length>0,"completed capture admits the new prompt");
        var (failedHost,failedOwner,failedJob)=Start(); failedHost.FailCapture=true;
        failedOwner.PrepareSharedRequest(failedJob);
        Test.True(failedJob.RequestPreparationPending && failedJob.SuspendedReason.Contains("event_material_capture_failed")
            && failedJob.RetryAfterUtcTicks>DateTime.UtcNow.AddSeconds(20).Ticks,"capture exception preserves bounded technical backoff, not a fast poll loop");
    }
    private static void ComparisonRepairDetails()
    {
        var(_,o,j)=Start();Complete(o,j,new(){["candidates"]=new JArray(Candidate())});
        var comparison=Compare();((JObject)comparison["assessments"]![0]!).Remove("duplicate_of");
        Complete(o,j,comparison);o.PrepareSharedRequest(j);
        var request=JObject.Parse(j.UserPrompt);
        Test.True(j.SuspendedReason.Contains("assessment=domestic;fields=duplicate_of"),"feedback identifies the missing field, not vague reasons");
        Test.True((string)request["rejected_response"]! == comparison.ToString(Formatting.None),"repair receives exact rejected assessment");
        j.SuspendedReason="";j.RetryAfterUtcTicks=0;
        comparison=Compare();comparison["urgent"]=new JArray("domsetic");Complete(o,j,comparison);
        Test.True(j.SuspendedReason.Contains("eligible_ids=domestic")&&j.SuspendedReason.Contains("invalid_selected_ids=domsetic"),"typo diagnosed without guessing a sovereign decision");
        j.SuspendedReason="";j.RetryAfterUtcTicks=0;
        comparison=Compare();var row=comparison["assessments"]![0]!;row["qualified"]=false;row["strength"]="";row["novelty_reason"]="";row["urgency_reason"]="";
        comparison["best_equivalent"]=new JArray();comparison["urgent"]=new JArray();Complete(o,j,comparison);
        Test.True(j.EventSelectionStage=="candidates"&&j.MaterialSearchPass==1&&j.SuspendedReason=="","valid rejection with no novelty/urgency continues investigation, not technical retries");
    }
    private static void GroundedParticipation()
    {
        var(_,o,j)=Start();Complete(o,j,new(){["candidates"]=new JArray(Candidate())});Complete(o,j,Compare());
        var parties=new JArray(Position("b","a"),Position("a","b"));
        parties[1]!["source_keys"]=new JArray("adopted:new_petition");
        var draft=new JObject {
            ["title"]="The envoy requests a hearing",["background"]="An unnamed envoy asks the other court to clarify its old undertaking.",
            ["initiator"]="b",["material_keys"]=new JArray("lore:succession","adopted:new_petition"),["participants"]=parties,
            ["party_review"]=new JArray(j.CandidateKingdomIds.Select(id=>new JObject{["kingdom"]=id,["included"]=id is "a" or "b",["reason"]="Standing under the disputed oath"})),
            ["narrative"]=new JArray(new JObject{["kind"]="adopted_npc_story",["subject"]="b",["source"]="adopted:new_petition",
                ["text"]="An unnamed envoy requests a hearing on the old oath.",["actor_scope"]="unnamed",["hero_ids"]=new JArray()})};
        Complete(o,j,draft);
        Test.True(j.SuspendedReason.Contains("basis_keys"),"invented plot cannot ground participation without real evidence");
        j.SuspendedReason="";j.RetryAfterUtcTicks=0;j.TechnicalRecoveryAttempts=0;
        draft["narrative"]![0]!["basis_keys"]=new JArray("adopted:another_invention");Complete(o,j,draft);
        Test.True(j.SuspendedReason.Contains("basis_keys"),"circular invented evidence remains blocked");
        j.SuspendedReason="";j.RetryAfterUtcTicks=0;j.TechnicalRecoveryAttempts=0;
        draft["narrative"]![0]!["basis_keys"]=new JArray("lore:succession");Complete(o,j,draft);
        Test.True(j.Kind=="event_concept_audit","grounded new NPC action reaches independent audit");
        o.PrepareSharedRequest(j);var req=JObject.Parse(j.UserPrompt);
        Test.True(req["sources"]!.Any(x=>(string)x["key"]! == "lore:succession"),"actual audit retains supporting source");
        var audit=MaterialCountryThirdReplay.Audit(j,Array.Empty<string>());Complete(o,j,audit);
        var round=o.ResolveRound(j.RoundId);Test.True(round.EventPhase=="opening","grounded creation can become an event after controlled independent approval");
        Test.True(round.Participants.Single(x=>x.KingdomId=="a").AgendaSourceKeys.Contains("adopted:new_petition"),"new event basis survives adoption");
        o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
        round=o.ResolveRound(round.RoundId);
        var boundary=o.CurrentStorage.Jobs.Single(x=>x.Kind=="event_boundary");
        Complete(o,boundary,new(){["title"]="The hearing opens",["body"]="The courts acknowledge the request.",
            ["effects"]=new JArray(new JObject{["kind"]="ruler_relation",["kingdom"]="b",["other"]="a",["delta"]=-1,["days"]=0,["reason"]="Disputed obligations strain relations."}),
            ["remove_effects"]=new JArray(),["confirmed_documents"]=new JArray(),["narrative"]=new JArray()});
        o.PrepareSharedRequest(boundary);
        Complete(o,boundary,MaterialCountryThirdReplay.Approved(boundary));
        Test.True(o.CurrentStorage.EventAnnouncements.Any(x=>x.EventId==round.RoundId&&x.Stage=="started"),"newly grounded event actually publishes after reload: "+boundary.SuspendedReason+" "+boundary.PrivacyDraft);
    }
}
