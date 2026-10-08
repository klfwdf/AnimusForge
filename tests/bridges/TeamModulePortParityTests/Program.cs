using System.Reflection;
using AnimusForge;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;

internal static class Program
{
    private static int checks;
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("FAIL " + name);
        checks++;
    }
    private static void Invocation(string name, Func<object> action, object expected,
        object[] args, Action verifyOutputs = null)
    {
        int calls = Recorder.Calls;
        object actual = action();
        Check(Equals(actual, expected), name + " return passthrough");
        Check(Recorder.Calls == calls + 1 && Recorder.LastMethod == name, name + " exactly one original owner call");
        Check(Recorder.LastArguments.Length == args.Length, name + " argument count");
        for (int i = 0; i < args.Length; i++)
            Check(Equals(Recorder.LastArguments[i], args[i]), name + " argument " + i);
        verifyOutputs?.Invoke();
        var failure = new InvalidOperationException("original owner exception: " + name);
        Recorder.Exception = failure;
        try { action(); throw new Exception("FAIL " + name + " swallowed exception"); }
        catch (InvalidOperationException observed)
        { Check(ReferenceEquals(observed, failure), name + " original exception identity"); }
        finally { Recorder.Exception = null; }
    }
    private static void Signatures(Type port, Type adapter, Func<MethodInfo, Type> owner)
    {
        foreach (MethodInfo method in port.GetMethods())
        {
            MethodInfo original = owner(method).GetMethod(method.Name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo implementation = adapter.GetMethod(method.Name);
            Check(original != null && implementation != null, method.Name + " owner and adapter exist");
            Check(original.ReturnType == method.ReturnType && implementation.ReturnType == method.ReturnType,
                method.Name + " return type preserved");
            var current = method.GetParameters(); var previous = original.GetParameters();
            Check(current.Length == previous.Length, method.Name + " signature arity");
            for (int i = 0; i < current.Length; i++)
            {
                Check(current[i].ParameterType == previous[i].ParameterType && current[i].IsOut == previous[i].IsOut,
                    method.Name + " typed ref/out " + i);
                Check(current[i].HasDefaultValue == previous[i].HasDefaultValue
                    && (!current[i].HasDefaultValue || Equals(current[i].DefaultValue, previous[i].DefaultValue)),
                    method.Name + " optional default " + i);
            }
        }
    }
    private static void Exercise(bool nullInputs)
    {
        var hero = nullInputs ? null : new Hero(); var character = nullInputs ? null : new CharacterObject();
        int index = nullInputs ? -1 : 49; bool selected = !nullInputs; bool direct = nullInputs;
        string chain = nullInputs ? null : "native-conversation";
        string player = nullInputs ? null : "player proposal";
        string npc = nullInputs ? null : "npc reply";
        string raw = nullInputs ? null : "raw [TAG] body";
        string kingdom = nullInputs ? null : "kingdom-42";
        var inputRules = nullInputs ? null : new List<PostprocessRuleEntry> { new PostprocessRuleEntry() };
        Recorder.BoolResult = !nullInputs; Recorder.HandledResult = nullInputs;
        Recorder.TextResult = nullInputs ? null : "returned text";
        Recorder.RefResult = nullInputs ? null : "rewritten body";
        Recorder.FailureResult = nullInputs ? null : "original.owner.reason";
        Recorder.Rules = nullInputs ? null : new List<PostprocessRuleEntry> { new PostprocessRuleEntry() };
        Recorder.Facts = nullInputs ? null : new List<string> { "fact" };
        Recorder.Notifications = nullInputs ? null : new List<string> { "notification" };
#if ADAPTER_ONLY
        IPolicyModulePort p = new PolicyModuleAdapter();
        IGatheringModulePort g = new GatheringModuleAdapter();
        ISiegeModulePort s = new SiegeModuleAdapter();
#else
        var p = TeamModuleServices.Policy; var g = TeamModuleServices.Gathering; var s = TeamModuleServices.Siege;
#endif
        string failure = null; string content = raw; List<string> facts = null, notifications = null; bool handled = false;
        Invocation("Policy.Eligible", () => p.IsEligibleTargetForExternal(hero, out failure), Recorder.BoolResult,
            new object[] { hero }, () => Check(failure == Recorder.FailureResult, "eligibility out reason"));
        Invocation("Policy.Rules", () => p.BuildRuntimePostprocessRulesForExternal(hero), Recorder.Rules, new object[] { hero });
        Invocation("Policy.Apply", () => { content = raw; return p.TryProcessAcceptedAgendaTag(hero, chain, player, npc, ref content, out failure); },
            Recorder.BoolResult, new object[] { hero, chain, player, npc, raw },
            () => { Check(content == Recorder.RefResult, "policy ref body"); Check(failure == Recorder.FailureResult, "policy out reason"); });
        Invocation("Policy.Context", () => p.BuildActivePolicyDialogueContextForExternal(hero, character, kingdom), Recorder.TextResult,
            new object[] { hero, character, kingdom });
        Invocation("Gathering.Rules", () => g.BuildRuntimePostprocessRulesForExternal(hero), Recorder.Rules, new object[] { hero });
        Invocation("Gathering.Context", () => g.BuildPostprocessContextForExternal(hero), Recorder.TextResult, new object[] { hero });
        Invocation("Gathering.Normalize", () => g.NormalizeNobleGatheringPostprocessTagsForExternal(raw), Recorder.TextResult, new object[] { raw });
        Invocation("Gathering.Feast", () => g.BuildFeastAttendanceContext(hero), Recorder.TextResult, new object[] { hero });
        Invocation("Gathering.Apply", () => { content = raw; return g.TryApplyNobleGatheringTagsForExternal(hero, ref content, out facts, out notifications); },
            Recorder.BoolResult, new object[] { hero, raw },
            () => { Check(content == Recorder.RefResult, "gathering ref body");
                Check(ReferenceEquals(facts, Recorder.Facts) && ReferenceEquals(notifications, Recorder.Notifications), "gathering facts/notifications same list references"); });
        Invocation("Siege.Rules", () => s.BuildPostprocessRules(selected, index, direct, player), Recorder.Rules,
            new object[] { selected, index, direct, player });
        Invocation("Siege.Context", () => s.BuildPostprocessContext(selected, index, direct, player), Recorder.TextResult,
            new object[] { selected, index, direct, player });
        Invocation("Siege.Normalize", () => s.NormalizePostprocessTags(selected, raw, inputRules), Recorder.TextResult,
            new object[] { selected, raw, inputRules });
        Invocation("Siege.Apply", () => { content = raw; return s.TryProcessActionTags(hero, character, index,
                ref content, out handled, direct, player, npc); }, Recorder.BoolResult,
            new object[] { hero, character, index, raw, direct, player, npc },
            () => { Check(content == Recorder.RefResult, "siege ref body"); Check(handled == Recorder.HandledResult, "handled independent of return flag"); });
    }
    static void Main()
    {
        // Fail deterministically without invoking native crash reporting for expected mutations.
        try { Run(); }
        catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
    }

#if ADAPTER_ONLY
    private static void ExerciseCivilWarObservation()
    {
        var realm = new Kingdom { Name="realm", StringId="realm-id" };
        Clan.PlayerClan = new Clan { Kingdom=realm }; MyBehavior.Instance=new MyBehavior();
        MyBehavior.CivilWarCalls.Clear();
        MyBehavior.RecordCivilWarPoliticalResult(null,"coup:1","outcome",true);
        MyBehavior.RecordCivilWarPoliticalResult(realm,"coup:1"," ",true);
        Check(MyBehavior.CivilWarCalls.Count==0,"civil-war rejects missing realm/empty observation");
        MyBehavior.RecordCivilWarPoliticalResult(realm,"coup:1","outcome",false);
        Check(MyBehavior.CivilWarCalls.Count==1 && MyBehavior.CivilWarCalls[0]=="material:civil_war|内战政治 - realm|outcome|coup:1|realm-id|True|True","civil-war actual hook material args and flags");
        MyBehavior.CivilWarCalls.Clear();
        MyBehavior.RecordCivilWarPoliticalResult(realm,"coup:2","outcome",true);
        Check(MyBehavior.CivilWarCalls.Count==2 && MyBehavior.CivilWarCalls[0].StartsWith("material:"),"civil-war record precedes optional bulletin once");
        Check(MyBehavior.CivilWarCalls[1].Contains("|True|realm:realm-id|outcome|realm-id"),"civil-war player realm and record group preserved");
        MyBehavior.Instance=null;MyBehavior.CivilWarCalls.Clear();
        MyBehavior.RecordCivilWarPoliticalResult(realm,"coup:3","outcome",true);
        Check(MyBehavior.CivilWarCalls.Count==1,"civil-war missing bulletin owner never repeats material");
        var method=typeof(MyBehavior).GetMethod("RecordCivilWarPoliticalResult",BindingFlags.Static|BindingFlags.NonPublic);
        Check(method.GetParameters().Select(p=>p.ParameterType).SequenceEqual(new[]{typeof(Kingdom),typeof(string),typeof(string),typeof(bool)}),"civil-war original reflection ABI retained");
        Console.WriteLine("PASS 6 current CivilWar hook/application observation assertions (record and bulletin leaves are substitutes)");
    }
#endif
#if ADAPTER_ONLY
    private static void ExerciseRpCraftObservation() {
        MyBehavior.RpCalls.Clear();MyBehavior.Instance=new();var app=new ExternalActionObservationApplication((h,t,k,a,m,r,target,place,loc,allow,won)=>{},(text,key,kind,major,target,place,location,won)=>MyBehavior.RpCalls.Add(new object[]{"action",text,key,kind,major,location}), (h,t,k,f,allow)=>{},(h,t,k,d,f,allow)=>{},(kind,label,text,key,kingdom,settlement,world,realm,hero,actorRealm,day,date)=>MyBehavior.RpCalls.Add(new object[]{"weekly",text,key,kind,world,realm,hero}));MyBehavior.Instance.ExternalActionObservations=new ExternalActionObservationBannerlordAdapter(app,()=>throw new Exception("RP must not allocate its own sequence"),(h,n)=>{});
        MyBehavior.RecordPlayerHighValueRpCraftForExternal("batch","requested","final",10000,0,"player","","good");Check(MyBehavior.RpCalls.Count==0,"RP original investment threshold rejects 10000");
        MyBehavior.RecordPlayerHighValueRpCraftForExternal("batch","requested","final",10001,0,"player","","good");Check(MyBehavior.RpCalls.Count==2&&Equals(MyBehavior.RpCalls[0][0],"action")&&Equals(MyBehavior.RpCalls[1][0],"weekly"),"RP typed external hook actual capture application record order");Check(Equals(MyBehavior.RpCalls[0][2],"player_rp_craft_high_value:batch:action")&&Equals(MyBehavior.RpCalls[1][2],"player_rp_craft_high_value:batch:weekly"),"RP stable record identities remain unique existing authority");Check(MyBehavior.RpCalls.All(row=>((string)row[1]).Contains("10001"))&&Equals(MyBehavior.RpCalls[1][6],"player"),"RP captured investment/player identity retained");
        MyBehavior.RpCalls.Clear();MyBehavior.RecordPlayerHighValueRpCraftForExternal(" ","requested","final",10001,1,"player","","good");Check(MyBehavior.RpCalls.Count==0,"RP empty batch cannot fabricate record");
        var hook=typeof(MyBehavior).GetMethod("RecordPlayerHighValueRpCraftForExternal",BindingFlags.Public|BindingFlags.Static);hook.Invoke(null,new object[]{"reflection","requested","final",10001,10,"other","Crafter","good"});Check(MyBehavior.RpCalls.Count==2&&((string)MyBehavior.RpCalls[0][1]).Contains("Crafter"),"RP real reflection ABI reaches same actual observation application");
        MyBehavior.Instance=null;MyBehavior.RpCalls.Clear();MyBehavior.RecordPlayerHighValueRpCraftForExternal("none","requested","final",10001,1,"player","","good");Check(MyBehavior.RpCalls.Count==0,"RP missing record owner retains null protocol");
        Console.WriteLine("PASS 7 current RP typed/reflection hook actual capture/application assertions (A record and engine foothold leaves substituted)");
    }
#endif
    private static void Run()
    {
#if ADAPTER_ONLY
        ExerciseCivilWarObservation();
        ExerciseRpCraftObservation();
#endif
#if !ADAPTER_ONLY
        Check(ReferenceEquals(TeamModuleServices.Policy, TeamModuleServices.Policy)
            && ReferenceEquals(TeamModuleServices.Gathering, TeamModuleServices.Gathering)
            && ReferenceEquals(TeamModuleServices.Siege, TeamModuleServices.Siege), "single cached adapter per port");
#endif
        foreach (Type adapter in new[] { typeof(PolicyModuleAdapter), typeof(GatheringModuleAdapter), typeof(SiegeModuleAdapter) })
            Check(adapter.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length == 0, "adapter holds no game/session state");
        Signatures(typeof(IPolicyModulePort), typeof(PolicyModuleAdapter), method =>
            method.Name == "BuildActivePolicyDialogueContextForExternal" ? typeof(NpcRulerPolicyBehavior) : typeof(KingdomAgendaCustomPolicyBehavior));
        Signatures(typeof(IGatheringModulePort), typeof(GatheringModuleAdapter), _ => typeof(NobleGatheringBehavior));
        Signatures(typeof(ISiegeModulePort), typeof(SiegeModuleAdapter), _ => typeof(AfGcczShoutBridge));
        Exercise(false); Exercise(true);
        string content = "default input";
#if ADAPTER_ONLY
        new SiegeModuleAdapter().TryProcessActionTags(null, null, -1, ref content, out _);
        Console.WriteLine("NOT TESTED: TeamModuleServices whole composition (CivilWar/WorldDiplomacy excluded); no service substitute compiled.");
#else
        TeamModuleServices.Siege.TryProcessActionTags(null, null, -1, ref content, out _);
#endif
        Check(Recorder.LastArguments.Skip(4).SequenceEqual(new object[] { false, null, null }), "omitted siege defaults preserved");
        Console.WriteLine($"PASS {checks} source-linked port assertions; 13 methods, populated/null inputs, return/ref/out and original exceptions.");
        Console.WriteLine("NOT TESTED: gameplay implementations, module policy outcomes, real game thread ownership, live-save acceptance.");
    }
}
