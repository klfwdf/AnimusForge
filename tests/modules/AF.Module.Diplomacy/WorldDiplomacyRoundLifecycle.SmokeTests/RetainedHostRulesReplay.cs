using AnimusForge;
internal static class RetainedHostRulesReplay
{
    internal static void Run()
    {
        Test.True(WorldDiplomacyEventRules.RoundHardDurationDays(15)==18 && WorldDiplomacyEventRules.RoundHardDurationDays(21)==24
            && WorldDiplomacyEventRules.RoundHardDurationDays(28)==32, "one hard-duration owner keeps all MCM choices");
        Test.True(WorldDiplomacyEventRules.NativeSignalBaseValue("declare_war")==24 && WorldDiplomacyEventRules.NativeSignalBaseValue("propose_peace")==42,"native signal weights preserved");
        var incoming=new WorldDiplomacyNativeDecisionSnapshot { HasHost=true,HasTarget=true,HostId="player",TargetId="npc",HostIsPlayer=true,Action="propose_trade",ProposerKingdomId="proposer" };
        Test.True(WorldDiplomacyEventRules.TryNativeSignal(incoming,out var source,out var target) && source=="npc" && target=="player","incoming player offer reverses source and target");
        incoming.Action="declare_war";
        Test.True(WorldDiplomacyEventRules.TryNativeSignal(incoming,out source,out target) && source=="proposer" && target=="npc","incoming war keeps proposer source");
        incoming.ProposerEliminated=true;
        Test.True(!WorldDiplomacyEventRules.TryNativeSignal(incoming,out _,out _),"eliminated proposer cannot emit native signal");
        incoming.Action="propose_peace";
        Test.True(WorldDiplomacyEventRules.TryNativeSignal(incoming,out _,out _),"incoming offer uses target eligibility instead of proposer eligibility");
        incoming.TargetEliminated=true;
        Test.True(!WorldDiplomacyEventRules.TryNativeSignal(incoming,out _,out _),"eliminated target cannot emit incoming offer");
        var queue=new QueuePort(); var orchestration=new Orchestration(queue.Events);
        WorldDiplomacyNativeDecisionApplication.Sanitize(queue,orchestration);
        Test.True(string.Join(",",queue.Events)=="capture0,remove0,capture1,reason1,record1,remove1,capture2,capture3,reason3,record3,remove3,capture4,reason4,record4,remove4",
            "queue preserves false capture removal, capture exception skip, remove exception continuation and ordering");
        Test.True(queue.Logs.Count==3 && queue.Logs.Last().EndsWith("count=3"),"queue removal totals count only completed effects");
        var battle=new BattlePort();
        battle.HasWinner=false; WorldDiplomacyBattleApplication.Record(battle,orchestration);
        Test.True(battle.Reads==0,"battle with no winner does not enumerate sides");
        battle.HasWinner=true; battle.IsHideout=true; WorldDiplomacyBattleApplication.Record(battle,orchestration);
        Test.True(battle.Reads==0,"hideout battle does not enumerate sides");
        battle.IsHideout=false; battle.Defenders=new(){"a"}; WorldDiplomacyBattleApplication.Record(battle,orchestration);
        Test.True(battle.Details==0,"same-kingdom sides do not capture detailed facts");
        battle.Defenders=new(){"b"}; WorldDiplomacyBattleApplication.Record(battle,orchestration);
        Test.True(orchestration.Battle?.BattleId=="battle:7:event:a:b" && battle.Details==1,"admitted battle receives stable fact identity once");
        var geo=new GeographyPort(); var publication=WorldDiplomacyGeographyApplication.Publication(geo);
        Test.True(publication.Settlements.Count==2 && publication.MaximumCivilianDistance==10 && publication.Settlements[0].Distance==10,"publication excludes hideout/blank IDs and retains origin fallback distance");
        Test.True(publication.Courts.Count==2 && publication.MaximumCourtDistance==20 && publication.Courts.Single(x=>x.KingdomId=="missing").Distance==20,"publication excludes author/eliminated courts and missing court uses maximum");
        Test.True(geo.SettlementReads.SequenceEqual(new[]{0,1}),"publication does not read hideout or empty-ID distance");
        Test.True(geo.CourtReads.SequenceEqual(new[]{3,1}),"only selected courts are resolved in sorted ID order");
        geo.CourtReads.Clear(); var recalc=WorldDiplomacyGeographyApplication.Recalculation(geo);
        Test.True(recalc.SettlementDistances["hideout"]==99 && recalc.MaxCivilianDistance==10,"recalculation retains hideout lookup but excludes hideout from civilian maximum");
        Test.True(recalc.CourtDistances.ContainsKey("author") && recalc.MaxCourtDistance==50 && !recalc.CourtDistances.ContainsKey("missing"),"recalculation includes author court and omits unknown distance");
        geo.Available=false;
        Test.True(WorldDiplomacyGeographyApplication.Recalculation(geo)==null,"recalculation without origin is unavailable");
    }
    private sealed class Orchestration : FakeOrchestration
    {
        private readonly List<string> events;
        internal WorldDiplomacyBattleFact Battle;
        internal Orchestration(List<string> events)=>this.events=events;
        public override bool RecordNativeSignal(string source,string target,string action,string reason)
        { events.Add("record"+source);return source!="4"; }
        public override void RecordBattleFact(WorldDiplomacyBattleFact fact)=>Battle=fact;
    }
    private sealed class QueuePort : IWorldDiplomacyNativeDecisionPort
    {
        internal List<string> Events=new(),Logs=new();
        public IEnumerable<IReadOnlyList<int>> Queues() { yield return new[]{0,1,2,3,4}; }
        public WorldDiplomacyNativeDecisionSnapshot Capture(int token)
        { Events.Add("capture"+token); if(token==2)throw new Exception("capture"); return token==0?null:new(){HasHost=true,HasTarget=true,HostId=token.ToString(),TargetId="other",Action="declare_war"}; }
        public string Reason(int token,bool incoming,string action) { Events.Add("reason"+token);return "reason"; }
        public void Remove(int token) { Events.Add("remove"+token);if(token==3)throw new Exception("remove"); }
        public string Describe(int token)=>token.ToString(); public void Log(string text)=>Logs.Add(text);
    }
    private sealed class BattlePort : IWorldDiplomacyBattlePort
    {
        public bool Exists=>true; public bool HasWinner{get;set;} public bool IsHideout{get;set;} public string EventId=>"event";
        internal int Reads,Details; internal List<string> Defenders=new(){"b"};
        public List<string> AttackerKingdomIds(){Reads++;return new(){"a"};}
        public List<string> DefenderKingdomIds(){Reads++;return Defenders;}
        public WorldDiplomacyBattleFact CaptureDetails(List<string> a,List<string> b){Details++;return new(){Day=7,AttackerKingdomIds=a,DefenderKingdomIds=b};}
        public void Log(string message)=>throw new Exception(message);
    }
    private sealed class GeographyPort : IWorldDiplomacyGeographyPort
    {
        internal bool Available=true; internal List<int> CourtReads=new(),SettlementReads=new(); public bool OriginAvailable=>Available;
        public IReadOnlyList<WorldDiplomacySettlementDistance> Settlements()=>new WorldDiplomacySettlementDistance[]{new(){Id="a",IsOrigin=true},new(){Id="town",Index=1,Distance=10},new(){Id="hideout",Index=2,IsHideout=true,Distance=99},new(){Id="",Index=3,Distance=100}};
        public float SettlementDistance(int i){SettlementReads.Add(i);return i==0?0:i==1?10:i==2?99:100;}
        public IReadOnlyList<WorldDiplomacyKingdomDestination> Kingdoms()=>new WorldDiplomacyKingdomDestination[]{new(){Id="author",Index=0,IsAuthor=true},new(){Id="other",Index=1},new(){Id="gone",Index=2,IsEliminated=true},new(){Id="missing",Index=3}};
        public WorldDiplomacyCourtDistance Court(int i){CourtReads.Add(i);return new(){KingdomId=i==0?"author":i==1?"other":"missing",SettlementId=i==3?"":"court",DistanceKnown=Available&&i!=3,Distance=i==0?50:20};}
    }
}
