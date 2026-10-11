using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AnimusForge;
using HarmonyLib;
using TaleWorlds.CampaignSystem;

internal static class Program
{
    private static int _checks, _cases, _failures;
    private static readonly FieldInfo Applied = typeof(WorldDiplomacyBehavior).GetField("_patchesApplied", BindingFlags.NonPublic | BindingFlags.Static);
    private static readonly string[] Targets = { "Kingdom.AddDecision/2", "ProposalVM..ctor/0", "ProposalVM..ctor/1", "ProposalVM.ExecuteAction/0", "ProposalVM.RefreshValues/0", "MyBehavior.BuildShoutPromptContextForExternal/0" };
    private static readonly string[] Events = { "OnNewGameCreatedEvent", "OnGameLoadedEvent", "OnSessionLaunchedEvent", "DailyTickEvent", "TickEvent", "WarDeclared", "MakePeace", "OnSettlementOwnerChangedEvent", "MapEventEnded" };
    private static bool IsApplied => (bool)Applied.GetValue(null);
    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("FAIL " + message);
        _checks++;
    }
    private static void Reset()
    {
        Applied.SetValue(null, false);
        var installed = typeof(WorldDiplomacyBehavior).GetField("_installedPatchTargets", BindingFlags.NonPublic | BindingFlags.Static);
        (installed?.GetValue(null) as HashSet<MethodBase>)?.Clear();
        Harmony.Attempts.Clear(); Harmony.Successes.Clear(); Harmony.Failures.Clear();
        Harmony.ConstructorFailures = 0;
        AccessTools.MissingType = false; AccessTools.MissingMethod = null;
        Logger.FailMessage = null; WorldDiplomacyUiSprites.Failures = WorldDiplomacyUiSprites.Calls = 0;
        CampaignEvents.Calls.Clear();
    }
    private static void Case(string name, Action test)
    {
        Reset(); _cases++;
        try { test(); Console.WriteLine("PASS " + name); }
        catch (Exception error) { _failures++; Console.Error.WriteLine(name + ": " + error.Message); }
    }
    private static void Bootstrap() => WorldDiplomacyBehavior.RegisterHarmonyPatches(new Harmony("com.AnimusForge.spy"));
    private static void Campaign()
    {
        CampaignEvents.Calls.Clear();
        var owner = new WorldDiplomacyBehavior(); owner.RegisterEvents();
        Check(ReferenceEquals(WorldDiplomacyBehavior.Instance, owner), "campaign publishes actual owner");
        Check(CampaignEvents.Calls.Select(x => x.Name).SequenceEqual(Events), "nine actual campaign listeners keep their order");
        Check(CampaignEvents.Calls.All(x => ReferenceEquals(x.Owner, owner)), "listeners belong to current owner");
    }
    private static void CompleteOnce()
    {
        Check(IsApplied, "complete installation state published");
        Check(Harmony.Successes.Count == Targets.Length, "all six target identities installed");
        foreach (string target in Targets)
            Check(Harmony.Count(Harmony.Successes, target) == 1, "successful target never duplicated: " + target);
    }
    private static int Main()
    {
        Case("normal startup/campaign/repeated registration", () => {
            Bootstrap(); CompleteOnce(); Campaign();
            WorldDiplomacyBehavior.RegisterHarmonyPatches(null); CompleteOnce();
            Check(Harmony.Attempts.Values.Sum() == Targets.Length, "completed registration skips reflection/patch work");
        });
        foreach (string target in Targets)
            Case("recover partial failure " + target, () => {
                Harmony.Failures[target] = 1; Bootstrap();
                Check(!IsApplied, "partial startup does not claim complete");
                if (target != Targets[0]) Check(Harmony.Count(Harmony.Successes, Targets[0]) == 1, "decision hook remains installed");
                if (target != Targets[5]) Check(Harmony.Count(Harmony.Successes, Targets[5]) == 1, "unrelated memory hook still installs");
                Campaign(); CompleteOnce();
                Check(Harmony.Count(Harmony.Attempts, target) == 2, "only failed target is retried");
                Campaign(); CompleteOnce();
            });
        Case("repeated failure across campaign lifecycles", () => {
            Harmony.Failures[Targets[5]] = 2; Bootstrap(); Campaign();
            Check(!IsApplied, "repeated missing memory hook keeps retry open");
            Check(Harmony.Count(Harmony.Attempts, Targets[5]) == 2, "bounded retry at campaign registration only");
            Campaign(); CompleteOnce();
            Check(Harmony.Count(Harmony.Attempts, Targets[5]) == 3, "later campaign can finish failed hook");
        });
        Case("late proposal VM availability", () => {
            AccessTools.MissingType = true; Bootstrap();
            Check(!IsApplied, "missing VM does not claim complete");
            Check(Harmony.Successes.Count == 2, "other two groups remain available");
            AccessTools.MissingType = false; Campaign(); CompleteOnce();
        });
        foreach (string missing in new[] { "Kingdom.AddDecision", "ProposalVM.ExecuteAction", "ProposalVM.RefreshValues" })
            Case("late method availability " + missing, () => {
                AccessTools.MissingMethod = missing; Bootstrap();
                Check(!IsApplied, "missing target keeps registration incomplete");
                AccessTools.MissingMethod = null; Campaign(); CompleteOnce();
            });
        Case("patcher constructor failure at bootstrap", () => {
            Harmony.ConstructorFailures = 1;
            try { WorldDiplomacyBehavior.RegisterHarmonyPatches(null); }
            catch (InvalidOperationException) { }
            Check(!IsApplied && Harmony.Attempts.Count == 0, "constructor failure cannot claim installed");
            Campaign(); CompleteOnce();
        });
        Case("patcher constructor failure cannot suppress campaign events", () => {
            Harmony.ConstructorFailures = 1; Campaign();
            Check(!IsApplied, "failed campaign installation remains retryable");
            Campaign(); CompleteOnce();
        });
        foreach (string message in new[] { "Kingdom.AddDecision diplomacy interception patch applied.", "kingdom diplomacy proposal button disable patches applied.", "shared three-channel diplomacy memory patch applied." })
            Case("logging failure " + message, () => {
                Logger.FailMessage = message; Bootstrap(); Campaign(); CompleteOnce();
                Check(Harmony.Attempts.Values.Sum() == Targets.Length, "logging failure never duplicates installed hook");
            });
        Case("sprite exception preserves already installed hooks", () => {
            WorldDiplomacyUiSprites.Failures = 1;
            try { Bootstrap(); } catch (InvalidOperationException) { }
            Check(!IsApplied, "failed final startup phase cannot claim completed");
            Campaign(); CompleteOnce();
        });
        Console.WriteLine($"{(_failures == 0 ? "PASS" : "FAIL")} world diplomacy registration cases={_cases} checks={_checks} failures={_failures}");
        Console.WriteLine("Boundary: whole actual registration and campaign methods; controlled leaves, no Bannerlord runtime.");
        return _failures == 0 ? 0 : 1;
    }
}

namespace HarmonyLib
{
    public sealed class ProposalVM { public ProposalVM() { } public ProposalVM(int value) { } public void ExecuteAction() { } public void RefreshValues() { } }
    public sealed class HarmonyMethod
    {
        internal readonly MethodInfo Method;
        public HarmonyMethod(Type owner, string name) { Method = owner.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static) ?? throw new InvalidOperationException("Missing actual patch method " + name); }
    }
    public static class AccessTools
    {
        internal static bool MissingType;
        internal static string MissingMethod;
        public static Type TypeByName(string name)
        {
            if (name != "TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy.KingdomDiplomacyProposalActionItemVM") throw new InvalidOperationException("Wrong actual VM name");
            return MissingType ? null : typeof(ProposalVM);
        }
        public static MethodInfo Method(Type owner, string name, Type[] types = null) => MissingMethod == owner.Name + "." + name ? null : types == null ? owner.GetMethod(name) : owner.GetMethod(name, types);
    }
    public sealed class Harmony
    {
        internal static readonly Dictionary<string, int> Attempts = new(), Successes = new(), Failures = new();
        internal static int ConstructorFailures;
        public Harmony(string id) { if (ConstructorFailures > 0) { ConstructorFailures--; throw new InvalidOperationException("Controlled constructor failure"); } }
        internal static int Count(Dictionary<string, int> counts, string key) => counts.TryGetValue(key, out int count) ? count : 0;
        public void Patch(MethodBase target, HarmonyMethod prefix = null, HarmonyMethod postfix = null)
        {
            if (target == null) throw new InvalidOperationException("Missing target");
            string key = target.DeclaringType.Name + "." + target.Name + "/" + target.GetParameters().Length;
            string expected = target.Name == "AddDecision" ? "Patch_Kingdom_AddDecision_Prefix" : target.Name == "ExecuteAction" ? "Patch_DiplomacyProposalActionItem_Execute_Prefix" : target.Name == "BuildShoutPromptContextForExternal" ? "Patch_BuildSharedDiplomacyMemory_Postfix" : "Patch_DiplomacyProposalActionItem_Constructed_Postfix";
            if ((prefix?.Method ?? postfix?.Method)?.Name != expected || (prefix != null) != (target.Name == "AddDecision" || target.Name == "ExecuteAction")) throw new InvalidOperationException("Incorrect prefix/postfix binding");
            Attempts[key] = Count(Attempts, key) + 1;
            if (Count(Failures, key) > 0) { Failures[key]--; throw new InvalidOperationException("Controlled patch failure " + key); }
            Successes[key] = Count(Successes, key) + 1;
        }
    }
}
namespace TaleWorlds.CampaignSystem.Election { public sealed class KingdomDecision { } }
namespace TaleWorlds.CampaignSystem
{
    public abstract class CampaignBehaviorBase { public abstract void RegisterEvents(); }
    public sealed class Kingdom { public void AddDecision(Election.KingdomDecision decision, bool ignoreInfluenceCost) { } }
    public static class CampaignEvents
    {
        internal static readonly List<(string Name, object Owner)> Calls = new();
        public sealed class Event
        {
            private readonly string _name;
            public Event(string name) { _name = name; }
            public void AddNonSerializedListener(object owner, Action callback) { Calls.Add((_name, owner)); }
        }
        public static readonly Event OnNewGameCreatedEvent = new(nameof(OnNewGameCreatedEvent)), OnGameLoadedEvent = new(nameof(OnGameLoadedEvent)), OnSessionLaunchedEvent = new(nameof(OnSessionLaunchedEvent)), DailyTickEvent = new(nameof(DailyTickEvent)), TickEvent = new(nameof(TickEvent)), WarDeclared = new(nameof(WarDeclared)), MakePeace = new(nameof(MakePeace)), OnSettlementOwnerChangedEvent = new(nameof(OnSettlementOwnerChangedEvent)), MapEventEnded = new(nameof(MapEventEnded));
    }
}
namespace AnimusForge
{
    public sealed partial class WorldDiplomacyBehavior : CampaignBehaviorBase
    {
        private const string Source = "WorldDiplomacy";
        public static WorldDiplomacyBehavior Instance { get; private set; }
        private static void Patch_Kingdom_AddDecision_Prefix() { } private static void Patch_DiplomacyProposalActionItem_Constructed_Postfix() { }
        private static void Patch_DiplomacyProposalActionItem_Execute_Prefix() { } private static void Patch_BuildSharedDiplomacyMemory_Postfix() { }
        private void OnNewGameCreated() { } private void OnGameLoaded() { } private void OnSessionLaunched() { } private void OnDailyTick() { }
        private void OnCampaignTick() { } private void OnWarDeclared() { } private void OnMakePeace() { } private void OnSettlementOwnerChanged() { } private void OnMapEventEnded() { }
    }
    public static class MyBehavior { public static void BuildShoutPromptContextForExternal() { } }
    internal static class WorldDiplomacyUiSprites
    {
        internal static int Calls, Failures;
        internal static void EnsurePatched(Harmony harmony) { Calls++; if (Failures > 0) { Failures--; throw new InvalidOperationException("Controlled sprite exception"); } }
    }
    internal static class Logger
    {
        internal static string FailMessage;
        internal static void Log(string source, string message) { if (FailMessage != null && message == "[AF-WORLD-DIPLOMACY] " + FailMessage) { FailMessage = null; throw new InvalidOperationException("Controlled logging failure"); } }
    }
}
