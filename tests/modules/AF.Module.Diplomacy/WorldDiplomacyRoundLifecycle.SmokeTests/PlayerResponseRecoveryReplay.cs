using System.Reflection;
using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class PlayerResponseRecoveryReplay
{
    private sealed class Host : ConcurrentOralMigrationReplay.Host
    {
        internal readonly HashSet<string> Unqualified = new();
        public override bool HasIndependentAuthority(string id) => !Unqualified.Contains(id) && base.HasIndependentAuthority(id);
        public override IWorldDiplomacyPromptWorld PromptWorld() => new PromptWorldFixture();
        public override IWorldDiplomacyJobPreparationPort JobPreparation() => new Preparation(this);
        public override string CommonDiplomacyContract(WorldDiplomacyRound round) => "real-orchestration-fixture-contract";
    }
    private sealed class Preparation : IWorldDiplomacyJobPreparationPort
    {
        private readonly Host _h; internal Preparation(Host h) => _h=h;
        public WorldDiplomacyStorage Storage => _h.Owner.CurrentStorage;
        public int GenerationMaxTokens => 100;
        public int AnalysisMaxTokens => 50;
        public (int minimum,int maximum) CharacterRange() => (10,1000);
        public bool KingdomExists(string id) => _h.PartyResolved(id);
        public WorldDiplomacyRound ResolveRound(string id) => _h.Owner.ResolveRound(id);
        public string CommonContract(WorldDiplomacyRound round) => "fixture-contract";
        public WorldDiplomacyDocument ResolveDocument(string id) => _h.Owner.ResolveDocument(id);
        public bool TryBuildProfile(string author,string marker,out string prompt) { prompt=marker+" profile";return true; }
        public void LogProfile(WorldDiplomacyJob job,string prompt) { }

    }
    private static object Invoke(WorldDiplomacyOrchestration o, string name, params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(o, args)!;
    private static (Host h, WorldDiplomacyOrchestration o, WorldDiplomacyRound r, WorldDiplomacyDocument d) Fixture(bool followup = false)
    {
        var h = new Host { PublishEnabled=true }; var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        var r = o.EnsureActiveRound("p", "a", true);
        r.EventSourceType = followup ? "player_followup" : "player_declaration";
        r.RelayPlanned = true; r.HardEndDay = 30;
        r.RelayRouteKingdomIds = new() { "a", "p" };
        var d = new WorldDiplomacyDocument { DocumentId = "original-player-source", RoundId = r.RoundId,
            AuthorKingdomId = "p", TargetKingdomId = "a", IsPlayerAuthored = true, IsReadyForPublication = true,
            AnalysisStatus = "success", Intent = "statement", Body = "请回应我的公开宣言", Day = h.CurrentDayValue };
        r.RootDocumentId = d.DocumentId; o.CurrentStorage.Documents.Add(d);
        r.PlayerResponses.Add(new() { SourceDocumentId = d.DocumentId, KingdomId = "a", OriginalRoundId = r.RoundId, CreatedDay = h.CurrentDayValue });
        WorldDiplomacyPropagationApplication.ReceiveCourt(o.CurrentStorage, d, "a", h.CurrentDayValue, () => false, () => { });
        return (h, o, r, d);
    }
    private static WorldDiplomacyJob Job(WorldDiplomacyRound r, WorldDiplomacyDocument d) => new()
    { JobId = "old-saved-followup-job", Kind = "generate", RoundId = r.RoundId, AuthorKingdomId = "a", TargetKingdomId = "p",
      IsExternalResponseOnly = true, IsRelayTurn = true, SourceDocumentId = d.DocumentId, PlayerResponseSourceIds = new() { d.DocumentId } };

    internal static void Run()
    {
        var (h, o, r, d) = Fixture(true); h.Unqualified.Add("p"); var old = Job(r,d); o.CurrentStorage.Jobs.Add(old);
        Test.True(!o.CanDispatchDiplomacyJob(old), "old queued request rejects original player country authority before API");
        Test.True(r.PlayerResponses.Single().Status == "unavailable" && h.Notices.Count == 1,
            "receiver eligible but original source author lost authority terminates once with explanation");
        for (int i = 0; i < 100; i++) { o.ProcessRoundLifecycle(); o.CanDispatchDiplomacyJob(old); }
        Test.True(h.Notices.Count == 1 && o.CurrentStorage.ConcurrentRounds.All(x => x.EventSourceType != "player_followup"),
            "unavailable source cannot manufacture another followup or notifications");
        Test.True(!o.CurrentStorage.Jobs.Contains(old), "old saved unavailable job retired on reconciliation without API");
        h.Unqualified.Clear(); h.CurrentDayValue++;
        o.ProcessRoundLifecycle();
        Test.True(r.PlayerResponses.Single().Status == "unavailable" && o.CurrentStorage.Jobs.Count == 0,
            "restored authority never silently resumes old declaration or transfers its promise to a suzerain");

        foreach (bool source in new[] {false,true})
        {
            var f = Fixture(); f.h.Unqualified.Add(source ? "p" : "a");
            Invoke(f.o, "SchedulePlayerResponseWork", f.r);
            Test.True(f.r.PlayerResponses[0].Status == "unavailable" && !f.o.CurrentStorage.Jobs.Any(),
                "schedule validates both original parties without request: " + source);
            f.o.CloseRound("relay_all_ai_withdrew", f.r);
            Test.True(!f.o.CurrentStorage.ConcurrentRounds.Any(x => x.EventSourceType == "player_followup"),
                "original close never carries unavailable obligations: " + source);
        }
        var bad = Fixture(true); bad.d.IsReadyForPublication = false;
        Test.True(!bad.o.CanDispatchDiplomacyJob(Job(bad.r,bad.d)), "unpublished original source cannot dispatch");

        var f2 = Fixture(true); h=f2.h; o=f2.o; r=f2.r; d=f2.d;
        var oldRelay = Job(r,d); oldRelay.JobId="old-ordinary-relay"; oldRelay.IsExternalResponseOnly=false;
        var oldPlan = new WorldDiplomacyJob {JobId="old-followup-plan",Kind="round_plan",RoundId=r.RoundId};
        o.CurrentStorage.Jobs.AddRange(new[]{oldRelay,oldPlan});
        Test.True(!o.CanDispatchDiplomacyJob(oldRelay) && !o.CanDispatchDiplomacyJob(oldPlan),
            "old-save optional relay and planning jobs cannot issue API requests for a response-only followup");
        var failed = Job(r,d); o.CurrentStorage.Jobs.Add(failed);
        o.AbandonRejectedGeneration(failed,"a","p","model returned no publishable response"); o.RemoveJob(failed.JobId);
        Test.True(r.PlayerResponses[0].Status == "pending" && r.PlayerResponses[0].RetryNotBeforeDay == h.CurrentDayValue+1,
            "unpublished response keeps obligation and defers one game day");
        foreach (var participant in r.Participants) if (!participant.IsPlayerAsync) participant.State="withdrawn";
        int counter=h.Counter;
        for(int i=0;i<100;i++) { o.ProcessRoundLifecycle(); Invoke(o,"SchedulePlayerResponseWork",r); o.ScheduleNextRelayHop(r,true); o.AdvanceRelay(r,true); }
        o.ReconcileActiveDiplomacyAfterLoad();
        Test.True(o.ResolveRound(r.RoundId)==r && r.State=="active" && h.Counter==counter && !o.CurrentStorage.Jobs.Any(),
            "repeated no-progress pumps keep exactly the same live obligation, no round/job/API storm");
        Test.True(!o.CurrentStorage.Jobs.Contains(oldRelay) && !o.CurrentStorage.Jobs.Contains(oldPlan),
            "old optional relay jobs retired during owned reconciliation");
        var saved=JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!;
        var h3=new Host { PublishEnabled=true }; var o3=new WorldDiplomacyOrchestration(h3,new WorldDiplomacyRuntimeState()); h3.Owner=o3; o3.ReplaceStorage(saved);
        var restored=o3.ResolveRound(r.RoundId); o3.ProcessRoundLifecycle();
        Test.True(!o3.CurrentStorage.Jobs.Any() && restored.PlayerResponses[0].RetryNotBeforeDay==h3.CurrentDayValue+1,
            "save reload cannot bypass same-day deferral");
        h3.CurrentDayValue++; o3.ProcessRoundLifecycle();
        var retry=o3.CurrentStorage.Jobs.Single(x=>x.Kind=="generate");
        Test.True(retry.RoundId==r.RoundId && retry.SourceDocumentId==d.DocumentId && retry.TargetKingdomId=="p",
            "next day resumes original source/party in the same followup");
        for(int i=0;i<50;i++) o3.ProcessRoundLifecycle();
        Test.True(o3.CurrentStorage.Jobs.Count==1 && o3.CanDispatchDiplomacyJob(retry), "next-day pumps coalesce one eligible request");
        retry.PlayerResponseSourceIds=new(){d.DocumentId};
        Test.True(!(bool)Invoke(o3,"ValidatePlayerResponseCoverage",retry,JObject.Parse("{\"answered_player_document_ids\":[]}")),
            "empty coverage cannot discharge retained obligation");
        var answer=new WorldDiplomacyDocument {DocumentId="legal-answer",AuthorKingdomId="a",TargetKingdomId="p",
            IsReadyForPublication=true,Body="合法的公开答复"};
        o3.CurrentStorage.Documents.Add(answer); Invoke(o3,"CommitPlayerResponseCoverage",retry,answer); o3.RemoveJob(retry.JobId);
        Test.True(restored.PlayerResponses[0].Status=="answered" && restored.PlayerResponses[0].AnswerDocumentId==answer.DocumentId,
            "valid published answer commits source coverage normally");
        o3.ProcessRoundLifecycle();
        Test.True(restored.State=="closed" && !o3.CurrentStorage.ConcurrentRounds.Any(x=>x.EventSourceType=="player_followup"),
            "answered followup closes without a successor");

        var final=Fixture(true); final.r.HardEndDay=final.h.CurrentDayValue;
        final.o.ProcessRoundLifecycle();
        Test.True(final.r.State=="closed" && final.r.PlayerResponses[0].Status=="unanswered"
            && final.r.PlayerResponses[0].SourceDocumentId==final.d.DocumentId && final.h.Notices.Count==1,
            "deadline is a bounded visible terminal state retaining original unfinished evidence");
        for(int i=0;i<50;i++) { final.h.CurrentDayValue++; final.o.ProcessRoundLifecycle(); }
        Test.True(final.h.Notices.Count==1 && final.o.CurrentStorage.Jobs.Count==0,"terminal deadline never reopens on future days");
        var repeat=Fixture(true); repeat.o.CloseRound("relay_all_ai_withdrew",repeat.r);
        Test.True(repeat.r.PlayerResponses[0].Status=="unanswered" && repeat.o.CurrentStorage.ConcurrentRounds.Count==0,
            "explicit legacy followup closure never carries another followup");
        var legacy=JsonConvert.DeserializeObject<WorldDiplomacyPlayerResponse>("{\"sourceDocumentId\":\"s\",\"kingdomId\":\"a\"}")!;
        Test.True(legacy.RetryNotBeforeDay==-1 && legacy.Status=="pending", "normal old JSON defaults remain schedulable");
    }
}
