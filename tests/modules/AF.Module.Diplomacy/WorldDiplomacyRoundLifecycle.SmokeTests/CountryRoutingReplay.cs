using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using AnimusForge;
using AnimusForge.DiplomacyDialogue;

// Real shared dispatcher, parsers, registration and direct-execution owner.
// Native effect leaves and campaign identities are controlled, not a live game.
internal static class CountryRoutingReplay
{
    sealed class Host : ConcurrentOralMigrationReplay.Host
    {
        internal bool Enabled = true;
        internal bool Allied, Trading;
        internal bool NoticeThrows;
        internal readonly List<string> Messages = new();
        public override bool WorldDiplomacyEnabled() => Enabled;
        public override void Log(string message) => Messages.Add(message);
        public override void Notify(string message)
        { if (NoticeThrows) throw new InvalidOperationException("notification unavailable"); base.Notify(message); }
        public override IReadOnlyList<string> AllKingdomIds() => new[] { "a", "b", "c", "p", "khuzait", "empire_s", "empire_w" };
        public override bool PartyResolved(string id) => AllKingdomIds().Contains(id);
        public override bool PartiesAllied(string first, string second) => Allied;
        public override bool TradeAgreementExists(string first, string second) => Trading;
        public override string PartyRulerId(string id) => id == "khuzait" ? "lord_6_1" : base.PartyRulerId(id);
    }
    sealed class State
    {
        internal Host Host;
        internal WorldDiplomacyOrchestration Owner;
        internal DialogueInteractionOrigin Origin;
        internal string Actor = "a", Ruler = "ruler_a";
        internal int Native, Direct;
        internal DiplomacyBilateralPayload Applied;
    }
    readonly struct Source : IDiplomacyOralTagSource, IDiplomacyCommitmentTagSource,
        IDiplomacyCommitmentReviewSource, IDiplomacyLobbyistTagSource, IDiplomacyBilateralTagSource
    {
        readonly State s;
        internal Source(State state) => s = state;
        public bool HasSpeaker => true;
        public bool IsAvailable => true;
        public string SpeakerHeroId => s.Ruler;
        public string SpeakerKingdomId => s.Actor;
        public bool UseFormalCommitments => s.Host.Enabled;
        public bool CaptureCommitmentReview(string tags) => s.Owner.CaptureSpeechReview(s.Ruler,s.Actor,s.Origin,tags);
        public bool TryProcessLobbyist(string tag,string payload) => s.Owner.TryProcessLobbyistTag(s.Ruler,s.Actor,tag,payload,s.Origin);
        public bool TryProcessBilateral(string tag,string payload)
        {
            var state=s;
            return s.Owner.TryProcessBilateralTag(s.Ruler,s.Actor,tag,payload,s.Origin, request =>
            {
                state.Direct++; state.Applied = request;
                return new() { State="applied", Actor=request.Parts[0], Target=request.Parts[1], Terms=request.Terms, Diagnostic="test_effect" };
            });
        }
        public string SubmitCommitment(string payload) => s.Owner.SubmitOralDiplomaticCommitment(s.Ruler,s.Actor,payload,s.Origin);
        public string SubmitLegacyCommitment(string action,string payload) => s.Owner.SubmitLegacyOralCommitment(s.Ruler,s.Actor,action,payload,s.Origin);
        public string ControlCommitment(string payload) => "";
        public string DeclareWar(string p) { s.Native++; return ""; }
        public string MakePeace(string p) { s.Native++; return ""; }
        public string IndependentClanPeace(string p) { s.Native++; return ""; }
        public string FormAlliance(string p) { s.Native++; return ""; }
        public string BreakAlliance(string p) { s.Native++; return ""; }
        public string MakeTrade(string p) { s.Native++; return ""; }
        public string CancelTrade(string p) { s.Native++; return ""; }
        public void Log(string message) => s.Host.Log(message);
    }
    static State Fixture(string channel="native", bool monarch=true)
    {
        var h = new Host { PlayerSovereign=monarch?"p":"", PlayerAffiliation="p", OralEffectPortsEnabled=true };
        var o = new WorldDiplomacyOrchestration(h,new WorldDiplomacyRuntimeState()); h.Owner=o;
        return new() { Host=h,Owner=o,Origin=new(channel,"turn","session","We agree to these terms.","I finally agree to these terms.") };
    }
    static void Dispatch(State s, string tags)
    {
        string text="Visible reply "+tags;
        DiplomacyOralTagApplication.Process(new Source(s),ref text);
        Test.True(text.Trim()=="Visible reply","routing keeps tags out of speech");
    }
    static string Native(string text)=>"[ACTION:DIPLOMACY:"+text+"]";
    static string Commit(string text)=>Native("COMMIT:"+text);
    internal static void Run()
    {
        string[] positional={"DECLARE_WAR:a:b","MAKE_PEACE:b:a:100:14","FORM_ALLIANCE:a:b:default","MAKE_TRADE:a:b:14","BREAK_ALLIANCE:a:b","CANCEL_TRADE:a:b"};
        string[] actions={"DeclareWar","Peace","Alliance","Trade","BreakAlliance","CancelTrade"};
        foreach(var channel in new[]{"native","scene","courier"})
        {
            foreach(bool monarch in new[]{false,true})
            for(int i=0;i<positional.Length;i++)
            {
                var s=Fixture(channel,monarch);s.Host.WorldWar=i==1;s.Host.Allied=i==4;s.Host.Trading=i==5;
                Dispatch(s,Native(positional[i]));
                var row=s.Owner.CurrentStorage.DialogueArrangements.Single();
                Test.True(row.IsLobbyistCommitment && row.ActorKingdomId=="a" && row.TargetKingdomId=="b" && row.Action.ToString()==actions[i],"positional country routing "+channel+" "+monarch+" "+actions[i]);
                Test.True(s.Direct==0 && s.Native==0 && s.Owner.CurrentStorage.SpeechReviews.Count==0,"lobbyist never invokes direct/legacy review/effects");
                Test.True(row.Status=="accepted" && s.Owner.CurrentStorage.ConcurrentRounds.Count==1,"same lobbyist event registration "+actions[i]+" reason="+row.Reason);
                Test.True(s.Host.Notices.Count==1 && s.Host.Notices[0].Contains("ruler_a") && s.Host.Notices[0].Contains("b"),"successful registration notifies player with ruler and target");
                if(i==1)Test.True(row.Terms.TributePayerKingdomId=="b" && row.Terms.TributeReceiverKingdomId=="a" && row.Terms.DailyTribute==100 && row.Terms.DurationDays==14,"payer may be target without reversing NPC actor");
                Dispatch(s,Native(positional[i]));
                Test.True(s.Owner.CurrentStorage.DialogueArrangements.Count==1,"positional repeats are idempotent");
                Test.True(s.Host.Notices.Count==1,"duplicate tags do not repeat registration notice");
            }
            for(int i=0;i<actions.Length;i++)
            {
                var s=Fixture(channel);s.Host.WorldWar=i==1;
                string terms=i==1?";payer=p;receiver=a;tribute=100;days=14":i==3?";days=14":"";
                Dispatch(s,Commit("action="+actions[i]+";move=NewMatter;target=p"+terms));
                Test.True(s.Direct==1 && s.Native==0 && !s.Owner.CurrentStorage.DialogueArrangements.Single().IsLobbyistCommitment,"COMMIT actual player pair uses existing direct owner "+actions[i]);
                Test.True(s.Host.Notices.Count==0,"direct diplomacy does not emit lobbyist registration notice");
                Dispatch(s,Native(positional[i].Replace(":b",":p")));
                Test.True(s.Direct==1,"equivalent direct formats cannot duplicate effect");
            }
            var mixed=Fixture(channel);
            Dispatch(mixed,Native("MAKE_TRADE:a:p:14")+Native("DECLARE_WAR:a:b"));
            Test.True(mixed.Direct==1 && mixed.Native==0 && mixed.Owner.CurrentStorage.DialogueArrangements.Count==2 && mixed.Owner.CurrentStorage.DialogueArrangements.Count(x=>x.IsLobbyistCommitment)==1,"mixed per-item routing");
            var affiliation=Fixture(channel,false);
            Dispatch(affiliation,Native("DECLARE_WAR:a:p"));
            Test.True(affiliation.Direct==0 && affiliation.Owner.CurrentStorage.DialogueArrangements.Single().IsLobbyistCommitment,"affiliation is not sovereignty");
        }
        var actual=Fixture();actual.Actor="khuzait";actual.Ruler="lord_6_1";actual.Host.PlayerAffiliation="empire_w";actual.Host.PlayerSovereign="empire_w";
        Dispatch(actual,Native("DECLARE_WAR:khuzait:empire_s"));
        Test.True(actual.Owner.CurrentStorage.DialogueArrangements.Single().Status=="accepted" && actual.Native==0 && actual.Direct==0,"00:01:50 actual rejected tag now registers one NPC declaration");
        Dispatch(actual,Commit("action=DeclareWar;move=NewMatter;target=empire_s"));
        Test.True(actual.Owner.CurrentStorage.DialogueArrangements.Count==1,"canonical and positional lobbyist tags deduplicate");
        bool english=SmokeLocalizationEnvironment.EnglishUi || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AF_SMOKE_UI_MODULE"));
        Test.True(actual.Host.Notices.Single()==(english?"lord_6_1, ruler of khuzait, has committed to declaring war on empire_s.":"khuzait君主lord_6_1已承诺向empire_s宣战。"),"actual UI-language notice describes a promise, not completed war");
        actual.Owner.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(actual.Owner.CurrentStorage))!);
        Dispatch(actual,Native("DECLARE_WAR:khuzait:empire_s"));
        Test.True(actual.Host.Notices.Count==1,"save/load does not repeat notification");
        actual.Host.PublishEnabled=true;
        var actualRow=actual.Owner.CurrentStorage.DialogueArrangements.Single();
        var writing=actual.Owner.CurrentStorage.Jobs.Single(x=>x.DialogueArrangementId==actualRow.ArrangementId);
        actual.Owner.PrepareSharedRequest(writing);
        actual.Owner.TryCompleteDiplomacyPipeline(writing,new LlmJobResult{Success=true,Content="{\"safe\":true,\"public_context\":\"The khan decided to declare war.\"}"});
        actual.Owner.PrepareSharedRequest(writing);
        string draft=(string)typeof(WorldDiplomacyOrchestration).GetMethod("BuildCommittedDeclaration",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.Invoke(actual.Owner,new object[]{actualRow})!;
        actual.Owner.CommitGeneratedDocument(writing,draft);
        actual.Owner.TryCompleteDiplomacyPipeline(writing,new LlmJobResult{Success=true,Content="{\"safe\":true,\"agreement_consistent\":true}"});
        var declaration=actual.Owner.ResolveDocument(actualRow.DocumentId);
        Test.True(declaration?.IsReadyForPublication==true && declaration.AuthorKingdomId=="khuzait" && declaration.TargetKingdomId=="empire_s" && declaration.ChangedDiplomaticState && actual.Host.Effects==1,"actual positional war reaches the existing publication and unilateral effect owner");
        Test.True(actual.Host.Notices.Count==1 && actual.Native==0 && actual.Direct==0,"formal publication does not repeat lobbyist notice or use alternate executor");
        actual.Host.CurrentDayValue++;
        Dispatch(actual,Commit("target=empire_s;move=NewMatter;action=DeclareWar;days=0;condition=none"));
        Test.True(actual.Owner.CurrentStorage.DialogueArrangements.Count==1 && actual.Host.Notices.Count==1 && actual.Host.Effects==1,"equivalent reordered/default fields in the same interaction stay idempotent after execution and another day");
        foreach(string invalid in new[]{"DECLARE_WAR:b:a","DECLARE_WAR:p:b","DECLARE_WAR:b:c","DECLARE_WAR:a:a","DECLARE_WAR:a:missing","DECLARE_WAR:a:b:14","MAKE_PEACE:a:b:100","MAKE_PEACE:a:b:100:14:x:a:city","MAKE_TRADE:a:b:auto","FORM_ALLIANCE:a:b:40","DECLARE_WAR:a:b;condition=clarify"})
        {
            var s=Fixture();Dispatch(s,Native(invalid));
            Test.True(s.Direct==0 && s.Native==0 && s.Owner.CurrentStorage.DialogueArrangements.Count==0,"invalid positional tag rejected: "+invalid);
            Test.True(s.Host.Messages.Any(x=>x.Contains("rejected")),"invalid tag has diagnostic: "+invalid);
            Test.True(s.Host.Notices.Count==0,"rejection must not announce success");
        }
        foreach(string payload in new[]{"action=DeclareWar;move=NewMatter;target=p;condition=clarify","action=Peace;move=NewMatter;target=p;tribute=100","action=Trade;move=NewMatter;target=p;event=unknown","action=DeclareWar;move=NewMatter;target=p;days=14","action=Alliance;move=NewMatter;target=p;days=40","action=Peace;move=NewMatter;target=p;cession_from=a;cession_to=p"})
        {
            var s=Fixture();Dispatch(s,Commit(payload));
            Test.True(s.Direct==0 && s.Native==0 && s.Host.Messages.Any(x=>x.Contains("rejected")),"non-equivalent COMMIT not weakened: "+payload);
        }
        var impostor=Fixture();impostor.Ruler="not_the_ruler";Dispatch(impostor,Native("DECLARE_WAR:a:b"));
        Test.True(impostor.Owner.CurrentStorage.DialogueArrangements.Count==0 && impostor.Native==0,"current ruler revalidated");
        var off=Fixture();off.Host.Enabled=false;Dispatch(off,Native("DECLARE_WAR:a:b"));
        Test.True(off.Native==1 && off.Owner.CurrentStorage.DialogueArrangements.Count==0,"AI-off native path retained");
        var pending=Fixture();Dispatch(pending,Commit("action=DeclareWar;move=NewMatter;target=b;condition=clarify"));
        Test.True(pending.Host.Notices.Count==0 && pending.Owner.CurrentStorage.DialogueArrangements.Single().Status=="waiting_clarification","pending clarification is not success");
        var unavailable=Fixture();unavailable.Host.NoticeThrows=true;Dispatch(unavailable,Native("DECLARE_WAR:a:b"));
        Test.True(unavailable.Owner.CurrentStorage.DialogueArrangements.Single().Status=="accepted" && unavailable.Host.Messages.Any(x=>x.Contains("notification failed")),"notification failure cannot undo registration or duplicate execution");
        var noConsent=Fixture();noConsent.Origin=new("native","turn","session","Discuss trade.","I agree.",false);
        Dispatch(noConsent,Commit("action=Trade;move=NewMatter;target=p;days=14"));
        Test.True(noConsent.Direct==0 && noConsent.Host.Messages.Any(x=>x.Contains("player_consent_not_confirmed")),"known absent player consent cannot be promoted by format conversion");
        Parsing();
    }
    static void Parsing()
    {
        Test.True(DiplomacyBilateralPayload.TryLobbyistCommit("MAKE_PEACE","b:a:100:14:b:a:city|document=offer;action=peace","a","p",out var text,out var error) && error=="" && DialogueTagPayload.TryParse(text,out _),"complete positional peace source/cession representable");
        DialogueTagPayload.TryParse(text,out var parsed);
        Test.True(parsed.Move==DialogueDiplomaticMove.AcceptProposal && parsed.SourceDocumentId=="offer" && parsed.SourceActionId=="peace" && parsed.Terms.CessionFromKingdomId=="b" && parsed.Terms.CessionToKingdomId=="a" && parsed.Terms.CessionSettlementId=="city","normalization preserves exact source and cession direction");
        DialogueTagPayload.TryParse("action=Peace;move=AcceptProposal;target=p;source_document=offer;source_action=peace;payer=p;receiver=a;tribute=100;days=14;cession_from=p;cession_to=a;settlement=city",out parsed);
        Test.True(DiplomacyBilateralPayload.TryFromCommit(parsed,"p","a",out var request) && request.Payload=="p:a:100:14:p:a:city" && request.SourceDocument=="offer" && request.SourceAction=="peace","direct normalization preserves all terms and source");
        var s=Fixture();Dispatch(s,Native("MAKE_PEACE:a:b:0:0"));Dispatch(s,Commit("action=Peace;move=NewMatter;target=b"));
        Test.True(s.Owner.CurrentStorage.DialogueArrangements.Count==1,"zero tribute format equivalence is idempotent");
        s=Fixture();Dispatch(s,Native("DECLARE_WAR:p:a"));
        Test.True(s.Direct==1 && s.Applied.Parts[0]=="p" && s.Applied.Parts[1]=="a","authorized player-origin direct declaration retains direction");
    }
}
