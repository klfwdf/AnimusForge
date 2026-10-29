using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class MaterialCountryThirdReplay
{
    // External driver supplies actual provider responses over stdio. No model
    // result is synthesized here; campaign leaves remain detached test doubles.
    internal static void PlayerOpeningModel()
    {
        var(h,o)=Owner(new ModelHost { PublishEnabled=true,PlayerAwareExecution=true,OralRange=(0,3000),CurrentDayValue=40 });h.OralEffectPortsEnabled=true;
        if(Environment.GetEnvironmentVariable("AF_MODEL_RELATION_FLOOR")=="1")h.Relation=-100;
        // This acceptance run exercises submitted cases, not unrelated autonomous
        // opportunities. All submitted cases still share the real work queues.
        o.CurrentStorage.NextPlayerAgendaDay=int.MaxValue;
        h.MaterialOverride=JsonConvert.SerializeObject(new[]{new {key="imperial:succession",known_by="public",
            text="p is the Western Empire ruled by the human player Garios (ruler_p). a is the Southern Empire ruled by Rhagaea (ruler_a); b is the Northern Empire ruled by Lucon (ruler_b); c is Battania ruled by Caladog (ruler_c). The three empires dispute imperial legitimacy. Garios claims military authority; Rhagaea claims dynastic legitimacy; Lucon defends senatorial authority. These are existing political positions, not new acts or accepted claims. All four realms currently have independent authority. No diplomatic agreement or war has been executed by this new declaration.",
            hero_ids=new[]{"ruler_p","ruler_a","ruler_b","ruler_c"}}});
        string scenario=Environment.GetEnvironmentVariable("AF_MODEL_SCENARIO") ?? "statement";
        string Declaration(string kind) => kind switch {
            "war"=>"致南帝国：我以西帝国皇帝加里俄斯的身份正式宣布，西帝国即日起对南帝国宣战。这是明确的宣战命令，不是威胁或附条件提案。",
            "condemn"=>"致南帝国：我加里俄斯代表西帝国，谴责你方对我方正统的否认。请说明你方立场。此次声明不宣战，也不提出条约。",
            _=>"卡拉迪亚的诸位，西帝国承天景命，正统所在。今日重宣大义，当扫清寰宇，一统帝国。顺之者昌，逆之者亡，诸君好自为之。" };
        var cases=new List<(WorldDiplomacyDocument root,string kind)>();
        foreach(string kind in scenario=="burst" ? new[]{"statement","war","condemn"} : new[]{scenario})
        {
            string declaration=Declaration(kind);
            WorldDiplomacyPlayerApplication.SubmitPlayerDocument(new SubmissionWorld(o),declaration,o);
            cases.Add((o.CurrentStorage.Documents.Last(x=>x.IsPlayerAuthored&&x.Body==declaration),kind));
        }
        Test.True(cases.Select(x=>x.root.RoundId).Distinct().Count()==cases.Count,"burst submissions create separate owning cases");
        var caseIds=cases.Select(x=>x.root.RoundId).ToHashSet();
        foreach(var item in cases) RunCase(item.root,item.kind);
        void RunCase(WorldDiplomacyDocument root,string scenario)
        {
        int requests=0;bool reloadedOpening=false,reloadedReply=false;
        for(int step=0;step<180;step++)
        {
            var round=o.ResolveRound(root.RoundId);
            bool opened=o.CurrentStorage.EventAnnouncements.Any(x=>x.EventId==round.RoundId&&x.Stage=="started"&&x.State=="public");
            bool replied=o.CurrentStorage.Documents.Any(x=>x.RoundId==round.RoundId&&!x.IsPlayerAuthored&&x.IsReadyForPublication);
            if(opened&&!reloadedOpening || replied&&!reloadedReply)
            {
                int effects=h.Effects,writes=h.Writes;
                reloadedOpening|=opened;reloadedReply|=replied;
                Reload(o);o.ReconcileActiveDiplomacyAfterLoad();round=o.ResolveRound(root.RoundId);
                Test.True(h.Effects==effects&&h.Writes==writes,"provider-driven save/reload preserves exactly-once receipts");
            }
            if(round.State!="active")
            {
                Test.True(o.CurrentStorage.Documents.Where(x=>x.RoundId==round.RoundId&&x.IsReadyForPublication
                    && !string.IsNullOrEmpty(x.SilenceSourceDocumentId)).All(x=>
                        !string.Equals(x.Body.Trim(),o.ResolveDocument(x.SilenceSourceDocumentId)?.Body.Trim(),StringComparison.Ordinal)),
                    "provider silence consequences must announce new prose, not republish the unchanged original question");
                Test.True(o.CurrentStorage.EventAnnouncements.Any(x=>x.EventId==round.RoundId&&x.Stage=="started"&&x.State=="public")
                    && o.CurrentStorage.EventAnnouncements.Any(x=>x.EventId==round.RoundId&&x.Stage=="ended"&&x.State=="public")
                    && o.CurrentStorage.Documents.Any(x=>x.RoundId==round.RoundId&&!x.IsPlayerAuthored&&x.IsReadyForPublication),
                    "provider-driven run must publish both boundaries and an actual NPC response");
                if(scenario=="war")Test.True(o.ResolveDocument(root.DocumentId).ChangedDiplomaticState,"explicit player declaration of war executes");
                Console.WriteLine("MODEL_RESULT "+JsonConvert.SerializeObject(new {scenario,requests,reloadedOpening,reloadedReply,round.State,round.EventOutcome,
                    documents=o.CurrentStorage.Documents.Where(x=>x.RoundId==round.RoundId).Select(x=>new{x.DocumentId,x.Title,x.AuthorKingdomId,x.Body}),
                    announcements=o.CurrentStorage.EventAnnouncements.Where(x=>x.EventId==round.RoundId),h.Writes,h.Effects}));
                return;
            }
            var job=o.CurrentStorage.Jobs.Where(x=>caseIds.Contains(x.RoundId) && x.Kind!="compress")
                .OrderByDescending(x=>x.Priority).FirstOrDefault(o.CanDispatchDiplomacyJob);
            if(job!=null)
            {
                o.PrepareSharedRequest(job);
                if(job.RequestPreparationPending)continue;
                if(!string.IsNullOrEmpty(job.SuspendedReason))throw new Exception("Model request suspended: "+job.SuspendedReason);
                if(++requests>180)throw new Exception("Provider run safety limit reached without closing the cases");
                var messages=WorldDiplomacyLlmMessageApplication.BuildLlmMessageArray(job,o.BuildCanonicalHistoryBlock);
                bool overBudget=false;
                Test.True(WorldDiplomacyLlmDispatchApplication.EnsureRequestFitsInputBudget(job,messages,32000,10000,
                    h.EstimateTokens,_=>"",_=>false,(_,_)=>overBudget=true,()=>{},_=>{})&&!overBudget,
                    "provider request passes the actual production input-budget gate");
                Console.WriteLine("MODEL_REQUEST "+JsonConvert.SerializeObject(new{job.JobId,job.Kind,
                    messages,job.MaxTokens}));
                var raw=JsonConvert.DeserializeObject<string>(Console.ReadLine() ?? throw new Exception("Provider transport closed"));
                if(job.Kind=="analyze"){o.CommitAnalysis(job,raw);o.RemoveJob(job.JobId);}
                else if(job.Kind=="generate")
                {
                    var effects=new CompletionEffects(o);
                    WorldDiplomacyCompletionApplication.Complete(job,raw,true,false,false,"",o.CurrentStorage,0,1,
                        ref effects,o);
                }
                else if(!o.TryCompleteDiplomacyPipeline(job,new(){Success=true,Content=raw}))
                    throw new Exception("Unhandled provider phase: "+job.Kind);
                Console.WriteLine("MODEL_STATE "+JsonConvert.SerializeObject(new{job.Kind,job.SuspendedReason,job.PrivacySourceMaterial,
                    job.PrivacyRepairFeedback,round.EventPhase,round.State}));
                if(!string.IsNullOrEmpty(job.SuspendedReason))
                {job.RetryAfterUtcTicks=job.NextRecoveryProbeUtcTicks=0;Call(o,"TryResumeTechnicalRequest",job,false);}
                continue;
            }
            if(step==0)Console.WriteLine("MODEL_BLOCKED "+JsonConvert.SerializeObject(new {root.RoundId,jobs=o.CurrentStorage.Jobs.Select(x=>new{x.JobId,x.Kind,x.RoundId,x.OwnershipEpoch,x.SchedulingOwner,x.PlayerAuthorityRulerId,x.IsRunning,x.SuspendedReason}),round.OwnershipEpoch,round.SchedulingOwner,round.PlayerAuthorityRulerId}));
            if(o.CurrentStorage.Jobs.Any(x=>x.RoundId==round.RoundId&&!string.IsNullOrEmpty(x.SuspendedReason)))
                throw new Exception("Provider pipeline stopped: "+string.Join(";",o.CurrentStorage.Jobs.Select(x=>x.SuspendedReason)));
            var next=o.CurrentStorage.PropagationArrivals.Where(x=>caseIds.Contains(x.RoundId)).Select(x=>x.DueDay).DefaultIfEmpty(h.CurrentDayValue+1).Min();
            h.CurrentDayValue=Math.Max(h.CurrentDayValue+1,next);o.ProcessPropagationArrivals();
            foreach(var wait in cases.Select(x=>o.ResolveRound(x.root.RoundId)).SelectMany(x=>x.PlayerNegotiationWaits).Where(x=>x.NotifiedHour<0))
                if(o.CurrentStorage.KingdomKnowledge.Any(x=>x.KingdomId=="p" && x.DocumentIds.Contains(wait.SourceDocumentId)))
                    o.RecordActualPlayerNotice(o.ResolveDocument(wait.SourceDocumentId),h.CurrentCampaignHours());
            Call(o,"MaintainEvents");Call(o,"ProcessDuePlayerSilence");
            Call(o,"ReconcileEventQueue");
            foreach(var item in cases) Call(o,"AdvanceEventProgress",o.ResolveRound(item.root.RoundId));
        }
        Console.WriteLine("MODEL_DIAGNOSTIC "+JsonConvert.SerializeObject(new{o.CurrentStorage.Jobs,round=o.ResolveRound(root.RoundId),o.CurrentStorage.PropagationArrivals}));
        throw new Exception("Provider replay did not close within bounded scheduling steps");
        }
    }
}
