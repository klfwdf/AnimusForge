using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AnimusForge;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Persistence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class LiveDiplomacyFixReplay
{
    private static object Call(object owner, string name, params object[] args) => owner.GetType()
        .GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(owner, args)!;
    internal sealed class Prestige : IWorldDiplomacyPrestigePort
    {
        internal readonly Dictionary<string, int> Relations = new();
        internal int Writes;
        public bool CampaignAvailable => true;
        public IEnumerable<string> KingdomIds(bool active) => new[] { "a", "b", "p" };
        public WorldDiplomacyPrestigeCourt CaptureCourt(string id) => new(id, false, "ruler_" + id,
            new[] { WorldProfileReplay.Clan(id + "_clan", id + "_vassal") });
        public bool HasHero(string id) => true;
        public bool TryReadRelation(string a, string b, out int value) { value = Relations.GetValueOrDefault(a); return true; }
        public AnimusForge.Refactor.Contracts.WorldDiplomacyRelationEffectReceipt ChangeRelationAndMeasure(string a, string b, int delta)
        { int before = Relations.GetValueOrDefault(a); int after = Math.Clamp(before + delta, -100, 100); Relations[a] = after; Writes++; return new(true, after-before, ""); }
        public string KingdomName(string id) => id;
        public int CurrentDay() => 12;
        public void Log(string message) { }
    }
    private sealed class Host : EventStructureReplay.Host, IWorldDiplomacyThreatMechanicsHost, IDiplomacySilenceEffectHost
    {
        internal readonly Prestige Standing = new();
        internal readonly List<string> Diagnostic = new();
        public override void Log(string message) => Diagnostic.Add(message);
        public IWorldDiplomacyPrestigePort Prestige() => Standing;
        public IWorldDiplomacyThreatBindingPort ThreatBinding() => new Binding(this);
        public override IWorldDiplomacyThreatSettlementPort ThreatSettlement() => new ThreatSettlementReplay.Port(Owner.CurrentStorage);
        public IReadOnlyList<string> KingdomTownIds(string id) => new[] { "town_1", "town_2" };
    }
    private sealed class Binding : IWorldDiplomacyThreatBindingPort
    {
        private readonly Host host;
        internal Binding(Host h) { host = h; }
        public WorldDiplomacyRound ResolveRound(string id) => host.Owner.ResolveRound(id);
        public bool IsPolicyActive(string p, string o, string a) => false;
        public string RepresentativeId(string id) => id;
        public int CurrentDay() => host.CurrentDayValue;
        public string NewId(string kind) => host.NewId(kind);
        public int EscalationPrestigeReward => 3;
        public int WarPrestigeReward => 5;
        public void Log(string text) { }
    }
    private static (Host h, WorldDiplomacyOrchestration o, WorldDiplomacyRound r) Fixture(string target = "b")
    {
        var h = new Host { PublishEnabled = true, PlayerAwareExecution = true, OralEffectPortsEnabled = true };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        var r = o.EnsureActiveRound("a", target, false); r.EventPhase = "discussing";
        r.Participants.Single(x => x.KingdomId == target).State = "active";
        return (h, o, r);
    }
    private static WorldDiplomacyDocument Doc(WorldDiplomacyOrchestration o, WorldDiplomacyRound r, string id, string a, string b, string intent)
    {
        var d = new WorldDiplomacyDocument { DocumentId = id, RoundId = r.RoundId, AuthorKingdomId = a,
            TargetKingdomId = b, AddressedKingdomIds = new() { b }, Intent = intent, IsReadyForPublication = true,
            Title = "Public matter", Body = "Published evidence", Commitment = WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent(intent) };
        o.AddDocument(d); return d;
    }
    private static void Known(WorldDiplomacyOrchestration o, string actor, string id) => WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(o.CurrentStorage.KingdomKnowledge, actor, id, 12);
    internal static void Run()
    {
        OralThirdParty(); LegacySpeechRecovery(); SilenceAudit(); ThreatCycle(false); ThreatCycle(true); PrestigeRecovery(); LegacySilence(); Silence(false); Silence(true); PublishSilence(false); PublishSilence(true); PublishSilence(false, true); PublishSilence(true, true); PublishThreatCycle(); DelayedSilence(false); DelayedSilence(true);
        Test.True(DiplomacyPromptTexts.Get("HiddenThreatContract", "").Length > 100
            && DiplomacyPromptTexts.Get("PlayerSilenceAudit", "").Contains("silence_consistent"), "CN/EN real prompt readers include contracts");
    }
    private static void OralThirdParty()
    {
        var (_, o, _) = Fixture();
        var item = new DiplomacySpeechReview { PlayerKingdom = "p", PlayerRuler = "ruler_p", PlayerText = "We agree to war against b.", NpcText = "I will declare war on b." };
        var json = JObject.Parse("{\"unresolved\":false,\"commitments\":[{\"payload\":\"action=DeclareWar;move=NewMatter;target=b\",\"player_agreed\":true,\"npc_quote\":\"I will declare war on b.\",\"player_quote\":\"We agree to war against b.\"}]}");
        Test.True((bool)Call(o, "ValidateSpeechExtraction", item, json), "third-party war promise accepts the actual player's agreement");
        item.PlayerRuler = "former_ruler";
        Test.True(!(bool)Call(o, "ValidateSpeechExtraction", item, json), "authority validation remains mandatory");
    }
    private static void ThreatCycle(bool fulfill)
    {
        var (h, o, r) = Fixture();
        var warning = Doc(o,r,"warning","a","b","warning");
        o.ProcessDiplomaticThreatDocument(warning,"a","b");
        var t = o.CurrentStorage.DiplomaticThreats.Single();
        Test.True(t.Stage == "warning" && t.TargetDecision == "pending", "new warning opens obligation");
        var refusal = Doc(o,r,"refusal","b","a","statement"); refusal.PresentedThreatDocumentIds.Add(warning.DocumentId);
        o.SettleThreatDeclaration(refusal);
        Test.True(t.TargetDecision == "pending", "undelivered warning cannot create rejection");
        refusal.ThreatConsequencesSettled = false; Known(o,"b",warning.DocumentId); o.SettleThreatDeclaration(refusal);
        Test.True(t.TargetDecision == "noncomplied", "related first refusal records stage decision");
        Test.True(!WorldDiplomacyThreatScope.For(o.CurrentStorage,"a",r.RoundId).Any(), "issuer has no obligation until refusal reaches court");
        Known(o,"a",refusal.DocumentId);
        var ultimatum = Doc(o,r,"ultimatum","a","b","ultimatum"); o.ProcessDiplomaticThreatDocument(ultimatum,"a","b");
        Test.True(t.Stage == "ultimatum" && t.TargetDecision == "pending", "warning escalation advances same threat");
        Known(o,"b",ultimatum.DocumentId);
        var rejection = Doc(o,r,"rejection","b","a","statement"); rejection.PresentedThreatDocumentIds.Add(ultimatum.DocumentId);
        o.SettleThreatDeclaration(rejection); Known(o,"a",rejection.DocumentId);
        var decision = Doc(o,r,"decision","a","b",fulfill ? "declare_war" : "statement");
        decision.PresentedThreatFollowThroughDocumentIds.Add(ultimatum.DocumentId);
        if (fulfill)
        {
            o.SettleThreatDeclaration(decision);
            Test.True(t.Status == "open" && WorldDiplomacyReputationRules.GetNationalPrestige(o.CurrentStorage.NationalPrestigeByKingdom,"a") == 100,
                "technical war failure incurs no broken-promise penalty");
            decision.ChangedDiplomaticState = true; o.ProcessDiplomaticThreatDocument(decision,"a","b");
            Test.True(t.Status == "enforced", "confirmed war fulfils ultimatum");
        }
        else
        {
            string original = decision.RoundId; decision.RoundId = "unrelated"; o.SettleThreatDeclaration(decision);
            Test.True(t.Status == "open", "another event cannot breach this obligation");
            decision.RoundId = original; decision.ThreatConsequencesSettled = false; o.SettleThreatDeclaration(decision); o.SettleThreatDeclaration(decision);
            Test.True(t.Status == "breached" && o.CurrentStorage.NationalPrestigeByKingdom["a"] == 75, "retreat incurs exactly one25-point loss");
            Test.True(h.Standing.Relations["a_vassal"] == -2, "hidden prestige alters actual ruler-vassal relation");
            Test.True(WorldDiplomacyPresentationQueries.Standing(o.CurrentStorage,"a") == ""
                && !WorldDiplomacyPresentationQueries.BuildImpactText(decision).Contains("prestige", StringComparison.OrdinalIgnoreCase), "hidden score does not leak to current UI projections");
        }
    }
    private static void PrestigeRecovery()
    {
        var (h,o,_) = Fixture(); var d = new WorldDiplomacyDocument();
        o.ApplyNationalPrestigeDelta("a",-61,d,"breach"); o.ReconcileHiddenPrestige();
        Test.True(h.Standing.Relations["a_vassal"] == -10 && h.Standing.Writes == 1, "daily reconciliation does not stack relation modifier");
        o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
        o.ReconcileHiddenPrestige(); Test.True(h.Standing.Writes == 1, "save/load keeps applied relation receipt");
        o.ApplyNationalPrestigeDelta("a",61,d,"fulfilled"); Test.True(h.Standing.Relations["a_vassal"] == 0, "recovered prestige removes its own modifier");
    }
    private static void LegacySilence()
    {
        var (_,o,r) = Fixture("p"); var d = Doc(o,r,"old","a","p","ultimatum");
        o.CurrentStorage.DiplomacyNotificationStateSchemaVersion = 1;
        WorldDiplomacyNotificationStateMigration.Migrate(o.CurrentStorage);
        Test.True(d.LegacyNoticeSuppressed && d.RumorNotified && d.FormalNoticeShown && d.IsReadyForPublication,
            "upgrade keeps old archive and establishes quiet baseline");
        var wait = new WorldDiplomacyPlayerWait { SourceDocumentId = d.DocumentId, NotifiedHour = 0 };
        r.PlayerNegotiationWaits.Add(wait); Call(o,"IndexPlayerWait",r,wait);
        Test.True(!wait.RequiresAnswer, "quiet legacy notice starts no retrospective punishment");
        var fresh = Doc(o,r,"fresh","a","p","statement"); WorldDiplomacyNotificationStateMigration.Migrate(o.CurrentStorage);
        Test.True(!fresh.LegacyNoticeSuppressed && !fresh.FormalNoticeShown, "post-migration declarations still notify normally");
    }
    private static void Silence(bool conflict)
    {
        var (h,o,r) = Fixture("p"); var source = Doc(o,r,"request","a","p",conflict ? "ultimatum" : "statement");
        var wait = new WorldDiplomacyPlayerWait { SourceDocumentId = source.DocumentId, NotifiedHour = h.CurrentCampaignHours()-72 };
        r.PlayerNegotiationWaits.Add(wait);
        var job = new WorldDiplomacyJob { JobId="closure",Kind="generate",RoundId=r.RoundId,AuthorKingdomId="a",TargetKingdomId="p",
            SilenceSourceDocumentId=source.DocumentId,SilenceConsequence=conflict ? "conflict" : "ordinary", PrivacyApproved=true };
        o.EnqueueJob(job);
        string raw="{\"title\":\"Closing correspondence\",\"body\":\"Final declaration\",\"actions\":[],\"requires_response\":false,\"round_participation\":\"withdraw\",\"round_status\":\"continue\"}";
        object[] bound = { job, raw }; Test.True((bool)Call(o,"BindSilenceConsequence",bound), "fixed consequence binds to current overdue source");
        var json=JObject.Parse((string)bound[1]); Test.True((string)json["actions"]![0]!["intent"]! == (conflict ? "declare_war" : "statement"), "LLM cannot silently choose no consequence");
        Test.True((string)json["actions"]![0]!["target_kingdom_id"]! == "p", "consequence targets player kingdom only");
        var closure = Doc(o,r,"result","a","p",conflict ? "declare_war" : "statement"); job.DocumentId=closure.DocumentId;
        if (conflict)
        {
            closure.Actions ??= new();
            closure.Actions.Add(new() { Intent="declare_war",TargetKingdomId="p",ChangedDiplomaticState=false });
            Test.True(!o.FinalizeSilenceBeforePublication(closure) && !closure.IsReadyForPublication && wait.ClosureDocumentId == "", "failed war cannot publish successful closure");
            closure.IsReadyForPublication=true; closure.Actions[0].ChangedDiplomaticState=true;
        }
        Test.True(o.FinalizeSilenceBeforePublication(closure), "confirmed consequence allows closure");
        int writes=h.Writes; o.FinalizeSilenceBeforePublication(closure);
        Test.True(wait.ClosureDocumentId == "result" && !wait.RequiresAnswer && h.Writes==writes, "same source settles once");
        if (!conflict)
        {
            Test.True(o.CurrentStorage.EventEffects.Count==2 && h.Loyalty==49, "all towns receive first loyalty pulse");
            Call(o,"ApplySilenceLoyaltyPenalty",job); Test.True(h.Writes==writes, "overlapping correspondence does not stack strength or same-day pulse");
            o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
            for(int i=0;i<=7;i++) foreach(var e in o.CurrentStorage.EventEffects) DiplomacyEventEffects.Pulse(e,h.CurrentDayValue+i,h);
            Test.True(h.Loyalty==43 && h.Writes==14 && o.CurrentStorage.EventEffects.All(x=>x.Status=="expired"), "seven daily pulses total per town across save/load, then expire");
        }
        wait.RequiresAnswer=true; wait.ClosureDocumentId=""; wait.AnswerDocumentId="reply";
        Test.True(!(bool)Call(o,"BindSilenceConsequence",new object[]{job,raw}), "relevant player answer cancels consequence");
    }
    private static void PublishSilence(bool conflict, bool ending = false)
    {
        var (h,o,r)=Fixture("p");
        var source=Doc(o,r,"source-publish","a","p",conflict ? "ultimatum" : "statement");
        source.RequiresResponse=true; source.PlayerLifecycleClassified=true;
        r.RootDocumentId=source.DocumentId; r.EventOpeningOrigin="legacy_public_event";
        r.PlayerNegotiationWaits.Add(new() { SourceDocumentId=source.DocumentId,NotifiedHour=h.CurrentCampaignHours()-72 });
        if (ending) Call(o,"BeginEventEnding",r,"");
        var job=new WorldDiplomacyJob { JobId="publish-closure",Kind="generate",RoundId=r.RoundId,ExchangeId=r.RoundId,
            AuthorKingdomId="a",TargetKingdomId="p",SourceDocumentId=source.DocumentId,IsResponse=true,
            SilenceSourceDocumentId=source.DocumentId,SilenceConsequence=conflict ? "conflict" : "ordinary",
            PrivacyApproved=true, CandidateKingdomIds=new(){"p"} };
        o.EnqueueJob(job);
        string raw = "{\"title\":\"Final diplomatic response\",\"body\":\"The ignored correspondence is now concluded.\",\"actions\":[],\"response_requests\":[],\"requires_response\":false,\"round_participation\":\"withdraw\",\"round_status\":\"continue\"}";
        object[] input = { job, raw }; Test.True((bool)Call(o,"BindSilenceConsequence", input),"publication consequence bind");
        var envelope = JObject.Parse((string)input[1]); WorldDiplomacyEnvelopeJsonRules.NormalizeGeneratedDiplomaticEnvelopeShape(job,envelope);
        if (ending)
        {
            object[] endingCheck = { job, envelope, "" };
            Test.True((bool)Call(o,"ValidateEndingDraft",endingCheck),"ending preserves original audited silence consequence: "+endingCheck[2]);
        }
        Test.True(!o.TryGetGeneratedIntentLegalityViolation(job,envelope,"a","p",out _,out var reason),"silence envelope legality: " + reason);
        var probe = new WorldDiplomacyDocument { DocumentId="probe", RoundId=r.RoundId, AuthorKingdomId="a", SourceDocumentId=source.DocumentId };
        Test.True(o.TryApplyGeneratedSemanticEnvelope(probe,envelope,"a","p",false,false), "semantic application: " + WorldDiplomacyEnvelopeJsonRules.GetGeneratedEnvelopeApplicationFailureReason(probe,envelope,"p",false,o.ResolveRound));
        Test.True(!job.PrivacyApproved, "binding a different action invalidates prior prose approval");
        // This replay injects an audit approval; real semantic review is a model/live acceptance gate.
        job.PrivacyApproved=true;
        o.CommitGeneratedDocument(job,envelope.ToString());
        var d=o.ResolveDocument(job.DocumentId);
        Test.True(d?.IsReadyForPublication==true, "complete silence publication succeeds: " + job.SuspendedReason + " / " + d?.MechanicalResult + " / " + string.Join(";",h.Diagnostic));
        Test.True(r.PlayerNegotiationWaits[0].ClosureDocumentId==d.DocumentId, "publication owns the exact silence receipt");
        if (ending) Test.True(r.EventConvergence.Result == (conflict ? "war_executed" : "silence_concluded"),
            "completed silence consequence ends without selecting another dispute route");
        Test.True(conflict ? h.Effects==1 && d.Actions.Any(x=>x.Intent=="declare_war" && x.ChangedDiplomaticState) : h.Writes==2, "real execution route confirms war receipt or town effects");
        int effects=h.Effects,writes=h.Writes; o.CommitGeneratedDocument(job,"{}");
        Test.True(h.Effects==effects && h.Writes==writes,"duplicate completion cannot repeat a published consequence");
    }
    private static void PublishThreatCycle()
    {
        var (_,o,r)=Fixture();
        var opening=Doc(o,r,"opening","a","b","statement"); r.RootDocumentId=opening.DocumentId; r.EventOpeningOrigin="legacy_public_event";
        WorldDiplomacyDocument Execute(string id,string a,string b,string intent, string stage="", string follow="")
        {
            var d=Doc(o,r,id,a,b,intent); d.IsReadyForPublication=false;
            d.Actions=new(){new(){ActionId="action_1",TargetKingdomId=b,Intent=intent,Commitment=WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent(intent),NegotiationMove="state_position"}};
            if(stage!="") d.PresentedThreatDocumentIds.Add(stage);
            if(follow!="") d.PresentedThreatFollowThroughDocumentIds.Add(follow);
            o.ProcessAnalyzedDocument(d,intent,d.Commitment,false,"firm",1);
            Test.True(d.IsReadyForPublication,"multi-action document actually published: "+id);
            return d;
        }
        Execute("war-demand","a","b","ultimatum"); Known(o,"b","war-demand");
        Execute("no","b","a","statement","war-demand"); Known(o,"a","no");
        Test.True(o.CurrentStorage.DiplomaticThreats.Single().TargetDecision=="noncomplied", "real publication records refusal");
        Execute("retreat","a","b","statement",follow:"war-demand");
        Test.True(o.CurrentStorage.NationalPrestigeByKingdom["a"]==75,"real publication settles follow-through breach");
    }
    private static void DelayedSilence(bool answered)
    {
        var (h,o,r)=Fixture("p"); var source=Doc(o,r,"delayed-source","a","p","statement");
        r.RootDocumentId=source.DocumentId; r.EventOpeningOrigin="legacy_public_event";
        r.PlayerNegotiationWaits.Add(new(){SourceDocumentId=source.DocumentId,NotifiedHour=h.CurrentCampaignHours()-72});
        var d=Doc(o,r,"delayed-result","a","p","statement"); d.IsReadyForPublication=false;
        d.SilenceSourceDocumentId=source.DocumentId; d.SilenceConsequence="ordinary"; d.EventExecutionPending=true;
        d.Actions=new(){new(){ActionId="action_1",TargetKingdomId="p",Intent="statement",Commitment="non_binding",NegotiationMove="end_negotiation"}};
        o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
        r=o.ResolveRound(r.RoundId); d=o.ResolveDocument(d.DocumentId);
        if(answered) r.PlayerNegotiationWaits[0].AnswerDocumentId="reply";
        Call(o,"CommitPendingEventDocuments",r,false);
        Test.True(answered ? !d.IsReadyForPublication && h.Writes==0 : d.IsReadyForPublication && h.Writes==2 && d.SilenceConsequenceSettled,
            "deferred save replay " + (answered ? "cancels after reply" : "settles without transient generation job"));
        if(!answered)
        {
            string ui=WorldDiplomacyPresentationQueries.BuildImpactText(d);
            Test.True(ui.Contains("0.5") && (SmokeLocalizationEnvironment.EnglishUi ? ui.Contains("loyalty") : ui.Contains("忠诚度")),"penalty receipt follows UI language");
        }
    }
    private static void LegacySpeechRecovery()
    {
        var (h,o,_) = Fixture();
        var item=new DiplomacySpeechReview { Id="old-review",SourceKey="old-source",Actor="a",Ruler="ruler_a",Status="technical_hold",Reason="speech_extraction_invalid",Attempts=3,
            PlayerKingdom="p",PlayerRuler="ruler_p",PlayerText="agreement",NpcText="promise" };
        o.CurrentStorage.SpeechReviews.Add(item);
        Call(o,"MaintainSpeechReviews");
        Test.True(item.ExtractionContractVersion==3 && item.Status=="pending" && item.Attempts==0 && o.CurrentStorage.Jobs.Any(x=>x.DocumentId==item.Id),"legacy extraction hold gets one bounded re-evaluation");
        item.Status="technical_hold"; item.Attempts=3; item.Reason="speech_extraction_invalid"; h.CurrentDayValue++;
        item.ConfigurationFingerprint=(string)Call(o,"SpeechConfiguration");
        Call(o,"MaintainSpeechReviews");
        Test.True(item.Status=="technical_hold" && item.Attempts==3,"contract recovery is not an unlimited retry loop");
    }
    private static void SilenceAudit()
    {
        var (_,o,r)=Fixture("p"); var source=Doc(o,r,"audit-source","a","p","statement");
        var job=new WorldDiplomacyJob { JobId="audit-silence",Kind="privacy_material",RoundId=r.RoundId,AuthorKingdomId="a",TargetKingdomId="p",SilenceSourceDocumentId=source.DocumentId };
        o.EnqueueJob(job);
        o.TryCompleteDiplomacyPipeline(job,new(){Success=true,Content="{\"safe\":true,\"public_context\":\"public evidence\",\"silence_kind\":\"ordinary\"}"});
        Test.True(job.Kind=="generate" && job.SilenceConsequence=="ordinary","classification comes from strict response field");
        job.Kind="privacy_audit"; job.PrivacyDraft="{}";
        o.TryCompleteDiplomacyPipeline(job,new(){Success=true,Content="{\"safe\":true}"});
        Test.True(job.Kind=="privacy_audit" && job.PrivacyRewrites==0 && o.CurrentStorage.Documents.Count==1,
            "incomplete negotiation audit retries the same draft without executing a consequence");
        o.TryCompleteDiplomacyPipeline(job,new(){Success=true,Content="{\"safe\":true,\"position_owner_valid\":true,\"negotiation_consistent\":true,\"silence_consistent\":false}"});
        Test.True(job.Kind=="generate" && job.SilenceConsequence=="" && job.PrivacyRewrites==1 && o.CurrentStorage.Documents.Count==1,
            "privacy approval alone cannot authorize an unaudited consequence or classification");
    }
}
