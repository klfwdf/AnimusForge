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
        var p = TeamModuleServices.Policy; var g = TeamModuleServices.Gathering; var s = TeamModuleServices.Siege;
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
        Check(ReferenceEquals(TeamModuleServices.Policy, TeamModuleServices.Policy)
            && ReferenceEquals(TeamModuleServices.Gathering, TeamModuleServices.Gathering)
            && ReferenceEquals(TeamModuleServices.Siege, TeamModuleServices.Siege), "single cached adapter per port");
        foreach (Type adapter in new[] { typeof(PolicyModuleAdapter), typeof(GatheringModuleAdapter), typeof(SiegeModuleAdapter) })
            Check(adapter.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length == 0, "adapter holds no game/session state");
        Signatures(typeof(IPolicyModulePort), typeof(PolicyModuleAdapter), method =>
            method.Name == "BuildActivePolicyDialogueContextForExternal" ? typeof(NpcRulerPolicyBehavior) : typeof(KingdomAgendaCustomPolicyBehavior));
        Signatures(typeof(IGatheringModulePort), typeof(GatheringModuleAdapter), _ => typeof(NobleGatheringBehavior));
        Signatures(typeof(ISiegeModulePort), typeof(SiegeModuleAdapter), _ => typeof(AfGcczShoutBridge));
        Exercise(false); Exercise(true);
        string content = "default input";
        TeamModuleServices.Siege.TryProcessActionTags(null, null, -1, ref content, out _);
        Check(Recorder.LastArguments.Skip(4).SequenceEqual(new object[] { false, null, null }), "omitted siege defaults preserved");
        Console.WriteLine($"PASS {checks} source-linked port assertions; 13 methods, populated/null inputs, return/ref/out and original exceptions.");
        Console.WriteLine("NOT TESTED: gameplay implementations, module policy outcomes, real game thread ownership, live-save acceptance.");
    }
}
