using AnimusForge;

internal static class NativeDiplomacyFallbackReplay
{
    private sealed class State
    {
        internal bool Formal;
        internal bool ThrowFormal;
        internal readonly List<string> Calls = new();
    }
    private readonly struct Source : IDiplomacyOralTagSource, IDiplomacyCommitmentTagSource
    {
        private readonly State _state;
        internal Source(State state) => _state=state;
        public bool HasSpeaker => true;
        public bool IsAvailable => true;
        public string SpeakerHeroId => "ruler";
        public string SpeakerKingdomId => "npc";
        public bool UseFormalCommitments => _state.Formal;
        private string Call(string action,string payload){_state.Calls.Add(action+":"+payload);return "";}
        public string SubmitCommitment(string payload){if(_state.ThrowFormal)throw new InvalidOperationException();return Call("formal",payload);}
        public string ControlCommitment(string payload)=>Call("control",payload);
        public string SubmitLegacyCommitment(string action,string payload)=>Call("legacy-"+action,payload);
        public string DeclareWar(string payload)=>Call("war",payload);
        public string MakePeace(string payload)=>Call("peace",payload);
        public string IndependentClanPeace(string payload)=>Call("independent",payload);
        public string FormAlliance(string payload)=>Call("alliance",payload);
        public string BreakAlliance(string payload)=>Call("break",payload);
        public string MakeTrade(string payload)=>Call("trade",payload);
        public string CancelTrade(string payload)=>Call("cancel",payload);
        public void Log(string message){}
    }
    internal static void Run()
    {
        var state=new State(); var source=new Source(state);
        string Run(string tag){string text="答应了 [ACTION:DIPLOMACY:"+tag+"]";DiplomacyOralTagApplication.Process(source,ref text);Test.True(!text.Contains("[ACTION:"),"native/formal routing hides internal tag");return text;}
        var tags=new[]{("DECLARE_WAR:npc:third","war:npc:third"),("MAKE_PEACE:npc:player:10:21","peace:npc:player:10:21"),("FORM_ALLIANCE:npc:player:21","alliance:npc:player:21"),("BREAK_ALLIANCE:npc:player","break:npc:player"),("MAKE_TRADE:npc:player:84","trade:npc:player:84"),("CANCEL_TRADE:npc:player","cancel:npc:player")};
        foreach(var(tag,expected) in tags){state.Calls.Clear();Run(tag);Test.True(state.Calls.SequenceEqual(new[]{expected}),"disabled AI diplomacy routes legacy tag only to existing native executor");}
        foreach(var(action,expected) in new[]{("DeclareWar","war:npc:player"),("Peace","peace:npc:player:0:0"),("Alliance","alliance:npc:player:default"),("Trade","trade:npc:player:default"),("BreakAlliance","break:npc:player"),("CancelTrade","cancel:npc:player")})
        {state.Calls.Clear();Run("COMMIT:action="+action+";move=NewMatter;target=player");Test.True(state.Calls.SequenceEqual(new[]{expected}),"late COMMIT or custom prompt falls back without formal publication");}
        state.Calls.Clear();Run("COMMIT:action=Peace;move=NewMatter;target=player;payer=player;receiver=npc;tribute=12;days=84");
        Test.True(state.Calls.SequenceEqual(new[]{"peace:player:npc:12:84"}),"native fallback preserves exact reversed payment direction and agreed duration");
        foreach(string payload in new[]{"action=Peace;move=AcceptProposal;target=player;source_document=old","action=Peace;move=RejectProposal;target=player;source_document=old","action=Peace;move=NewMatter;target=player;supersedes=old;version=1;reason=changed","action=Peace;move=NewMatter;target=player;tribute=12","action=Peace;move=NewMatter;target=player;cession_from=npc;cession_to=player;settlement=town","action=Peace;move=NewMatter;target=player;payer=other;receiver=player;tribute=12;days=84","action=Trade;move=NewMatter;target=player;tribute=12;days=21","action=DeclareWar;move=NewMatter;target=player;days=21","action=Annexation;move=NewMatter;target=player;receiving=npc;joining=player","broken"})
        {state.Calls.Clear();string result=Run("COMMIT:"+payload);Test.True(state.Calls.Count==0&&(result.Contains("未执行")||result.Contains("已关闭")),"complex, malformed or old-proposal payload cannot silently become native action");}
        state.Calls.Clear();Run("COMMITMENT:arrangement=old;state=cancelled");Test.True(state.Calls.Count==0,"disabled mode does not mutate persisted formal arrangements");
        state.Formal=true;
        foreach(var(tag,_) in tags){state.Calls.Clear();Run(tag);Test.True(state.Calls.Count==1&&state.Calls[0].StartsWith("legacy-"),"enabled AI diplomacy keeps NPC legacy actions on formal path");}
        state.Calls.Clear();Run("DECLARE_WAR:player:npc");Test.True(state.Calls.SequenceEqual(new[]{"war:player:npc"}),"player king's explicit war remains immediate in formal mode");
        state.Calls.Clear();Run("COMMIT:action=DeclareWar;move=NewMatter;target=third");Test.True(state.Calls.Single().StartsWith("formal:"),"enabled COMMIT submits formal document only");
        state.ThrowFormal=true;state.Calls.Clear();Run("COMMIT:action=DeclareWar;move=NewMatter;target=third");Test.True(state.Calls.Count==0,"formal submission failure never falls back to immediate native action");
        foreach(bool enabled in new[]{true,false}){state.Formal=enabled;state.Calls.Clear();Run("INDEPENDENT_CLAN_PEACE");Test.True(state.Calls.SequenceEqual(new[]{"independent:"}),"independent clan peace unchanged in both modes");}
        string root=Directory.GetCurrentDirectory();while(!File.Exists(Path.Combine(root,"AnimusForge.csproj")))root=Directory.GetParent(root)!.FullName;
        var rules=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Path.Combine(root,"content/modules/AF.Module.Prompt/ModuleData/RuleBehaviorPrompts.json")));
        var rule=rules.Descendants().OfType<Newtonsoft.Json.Linq.JObject>().Single(x=>(string?)x["Id"]=="diplomacy");
        string instruction=(string)rule["Instruction"]!;
        Test.True(instruction.Contains("AI外交关闭")&&!instruction.Contains("[ACTION:"),"actual main-chain resource describes native mode without directive tags");
        string nativeTemplate=(string)rule["RuntimeInstructionTemplates"]!["native_action_postprocess"]!;
        foreach(string tag in new[]{"DECLARE_WAR","MAKE_PEACE","FORM_ALLIANCE","BREAK_ALLIANCE","MAKE_TRADE","CANCEL_TRADE"})
            Test.True(nativeTemplate.Contains("DIPLOMACY:"+tag+":")&&rule["PostprocessRules"]!.Any(x=>((string?)x["Tag"]??"").Contains("DIPLOMACY:"+tag+":")),"actual native runtime and topic postprocessing resources expose each supported tag");
    }
}
