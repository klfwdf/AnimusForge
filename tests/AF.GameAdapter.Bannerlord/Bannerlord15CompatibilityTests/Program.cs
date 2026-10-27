using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using TaleWorlds.Library;

internal static class Program
{
    private static readonly List<string> Logs = new List<string>();
    private static int _checks;
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static int Main(string[] args)
    {
        try
        {
            // Each process binds exactly one reference line. No game, save,
            // mission, HTTP, configuration reader or native engine is started.
            var directories = args[0].Split('|');
            AppDomain.CurrentDomain.AssemblyResolve += (sender, request) =>
            {
                string name = new AssemblyName(request.Name).Name;
                foreach (var directory in directories)
                {
                    string path = Path.Combine(directory, name + ".dll");
                    if (File.Exists(path)) return Assembly.LoadFrom(path);
                }
                return null;
            };
            var af = Assembly.LoadFrom(args[1]);
            string api = args[3];
            Check(af.GetCustomAttributes<AssemblyMetadataAttribute>().Any(a =>
                a.Key == "AnimusForge.BannerlordApi" && a.Value == api), "implementation API metadata");
            Check(af.GetTypes().Length > 1000, "all managed AF types resolve");
            // Game startup loads the official view modules before AF's audit.
            // The audit intentionally searches loaded runtime assemblies.
            Assembly.Load("SandBox");
            Assembly.Load("SandBox.View");
            Assembly.Load("TaleWorlds.MountAndBlade.View");
            var harmony = new Harmony("AF.Bannerlord15CompatibilityTests");
            foreach (string name in new[] { "AnimusForge.Logger", "RichExecutions.Diagnostics.RexLog" })
                foreach (var method in af.GetType(name, true).GetMethods(Static).Where(m =>
                    m.ReturnType == typeof(void) && !m.ContainsGenericParameters))
                    harmony.Patch(method, prefix: new HarmonyMethod(typeof(Program), nameof(CaptureLog)));

            var bootstrap = Assembly.LoadFrom(args[2]);
            var runtimeType = bootstrap.GetType("AnimusForge.Bootstrap.BootstrapRuntime", true);
            object runtime = Activator.CreateInstance(runtimeType, true);
            object line = runtimeType.GetMethod("DetectRuntimeApiLineFromBuildInfo", Instance).Invoke(runtime, null);
            Check(line.ToString() == "V" + api.Replace(".", ""), "real BuildInfo selects exact implementation line");
            VerifyExplicitVersions(runtimeType, runtime, harmony);

            af.GetType("AnimusForge.CompatibilityAudit", true).GetMethod("RunStartupAudit", Static).Invoke(null, null);
            string summary = Logs.Single(s => s.StartsWith("Summary CriticalOK="));
            Console.WriteLine(summary);
            Check(summary.Contains("CriticalFail=0 "), "all startup critical API contracts");

            VerifyDeclaredPatchTargets(af);
            VerifyScope(af, harmony);
            if (api == "1.5")
            {
                VerifyBloodFeudPatch(af);
                VerifyFacialPatch(af);
            }
            Console.WriteLine("PASS assertions=" + _checks + "; managed references/fixtures only; no live game or save.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static bool CaptureLog(object[] __args)
    {
        foreach (object value in __args)
            if (value is string text)
            {
                Logs.Add(text);
                if (text.Contains("FAIL") || text.Contains("[MISSING]")) Console.WriteLine(text);
            }
        return false;
    }

    private static void VerifyExplicitVersions(Type type, object runtime, Harmony harmony)
    {
        var source = AccessTools.Method(typeof(ApplicationVersion), nameof(ApplicationVersion.FromParametersFile));
        harmony.Patch(source, prefix: new HarmonyMethod(typeof(VersionFixtures), nameof(VersionFixtures.Prefix)));
        try
        {
            var detect = type.GetMethod("DetectRuntimeApiLine", Instance);
            foreach (string minor in new[] { "3", "4", "5" })
            {
                VersionFixtures.Value = ApplicationVersion.FromString("v1." + minor + ".5.124170");
                Check(detect.Invoke(runtime, null).ToString() == "V1" + minor,
                    "explicit version 1." + minor + " selects its own line");
            }
            foreach (string version in new[] { "v1.6.0.1", "v2.0.0.1" })
            {
                VersionFixtures.Value = ApplicationVersion.FromString(version);
                bool rejected = false;
                try { detect.Invoke(runtime, null); }
                catch (TargetInvocationException e) { rejected = e.InnerException is NotSupportedException; }
                Check(rejected, "unsupported explicit version rejected: " + version);
            }
        }
        finally { harmony.Unpatch(source, HarmonyPatchType.Prefix, harmony.Id); }
    }

    // Keep game value types out of Program's static layout so the resolver is
    // installed before the CLR binds the requested reference line.
    private static class VersionFixtures
    {
        internal static ApplicationVersion Value;
        public static bool Prefix(ref ApplicationVersion __result)
        {
            __result = Value;
            return false;
        }
    }

    private static void VerifyFacialPatch(Assembly af)
    {
        af.GetType("AnimusForge.LipSyncFacialAnimSuppressPatch", true).GetMethod("EnsurePatched", Static).Invoke(null, null);
        var target = Assembly.Load("TaleWorlds.MountAndBlade").GetType(
            "TaleWorlds.MountAndBlade.Source.Missions.Handlers.MissionFacialAnimationHandler", true)
            .GetMethod("OnMissionTick", Instance);
        Check(Harmony.GetPatchInfo(target)?.Prefixes.Any(p => p.owner == "AnimusForge.lipsync.facialanimsuppress") == true,
            "facial suppression binds to real 1.5 handler");
    }

    private static void VerifyScope(Assembly af, Harmony harmony)
    {
        var campaign = Assembly.Load("TaleWorlds.CampaignSystem");
        var heroType = campaign.GetType("TaleWorlds.CampaignSystem.Hero", true);
        var clanType = campaign.GetType("TaleWorlds.CampaignSystem.Clan", true);
        var kingdomType = campaign.GetType("TaleWorlds.CampaignSystem.Kingdom", true);
        object victim = Blank(heroType), friend = Blank(heroType), foreign = Blank(heroType), unrelated = Blank(heroType);
        object clan = Blank(clanType), sameKingdom = Blank(clanType), otherClan = Blank(clanType);
        object kingdom = Blank(kingdomType);
        Field(heroType, "_clan").SetValue(victim, clan);
        Field(heroType, "_clan").SetValue(friend, sameKingdom);
        Field(heroType, "_clan").SetValue(foreign, otherClan);
        Field(clanType, "_kingdom").SetValue(clan, kingdom);
        Field(clanType, "_kingdom").SetValue(sameKingdom, kingdom);
        var scope = af.GetType("RichExecutions.Core.ExecutionRelationScope", true);
        var tokenType = scope.GetNestedType("ScopeToken", BindingFlags.NonPublic);
        harmony.Patch(tokenType.GetMethod("ShowRelationSummary", Static),
            prefix: new HarmonyMethod(typeof(Program), nameof(Skip)));
        var begin = scope.GetMethod("Begin", Static);
        var adjust = scope.GetMethod("AdjustRelationChange", Static);
        Func<object, object, int, int> change = (v, target, delta) => (int)adjust.Invoke(null, new[] { v, target, (object)delta });
        Check(change(victim, friend, -45) == -45, "ordinary execution untouched outside scope");
        using ((IDisposable)begin.Invoke(null, new[] { victim }))
        {
            Check(change(victim, friend, -45) == -23, "negative odd penalty halves away from zero");
            Check(change(victim, friend, -30) == -15, "even penalty halves");
            Check(change(victim, friend, -1) == -1, "small negative penalty preserved");
            Check(change(victim, foreign, -45) == 0, "foreign kingdom excluded");
            Check(change(victim, null, -45) == 0, "missing clan excluded");
            Check(change(victim, friend, 5) == 5, "positive in-kingdom result preserved");
            Check(change(victim, foreign, 5) == 0, "outside scope remains excluded for positive result");
            Check(change(unrelated, friend, -45) == -45, "nested unrelated victim untouched");
            var set = scope.GetMethod("AdjustBloodFeudRelationSet", Static);
            if (set != null)
            {
                var delta = scope.GetMethod("AdjustBloodFeudRelationChange", Static);
                Func<object, int, int, int> applyDelta = (target, current, value) =>
                    (int)delta.Invoke(null, new[] { victim, target, (object)current, value });
                Check((int)set.Invoke(null, new[] { victim, friend, (object)0, -100 }) == -50,
                    "forced minimum relation halves its actual decrease");
                Check(applyDelta(friend, -50, -45) == 0,
                    "second penalty at original minimum stays a no-op");
                Check(applyDelta(victim, -90, -45) == -5,
                    "near-minimum relation halves the clamped vanilla decrease");
                Check(applyDelta(foreign, -90, -45) == 0,
                    "clamped foreign penalty still excluded");
                Check((int)set.Invoke(null, new[] { victim, friend, (object)0, -50 }) == -25,
                    "nonminimum set retains original baseline");
                Check(applyDelta(friend, -25, -20) == -10,
                    "subsequent unclamped penalty halves once");
            }
            bool rejected = false;
            try { begin.Invoke(null, new[] { unrelated }); }
            catch (TargetInvocationException e) { rejected = e.InnerException is InvalidOperationException; }
            Check(rejected, "nested scope rejected");
        }
        Check(change(victim, friend, -45) == -45, "disposed scope restores ordinary behavior");
        Field(clanType, "_kingdom").SetValue(clan, null);
        Field(heroType, "_clan").SetValue(friend, clan);
        using ((IDisposable)begin.Invoke(null, new[] { victim }))
        {
            Check(change(victim, friend, -30) == -15, "kingdomless victim uses own clan");
            Check(change(victim, foreign, -30) == 0, "other kingdomless clan excluded");
        }
    }

    private static void VerifyBloodFeudPatch(Assembly af)
    {
        var adapter = af.GetType("RichExecutions.Core.ExecutionRelationCompatibility", true);
        adapter.GetMethod("Register", Static).Invoke(null, new object[] { null });
        var behavior = Assembly.Load("TaleWorlds.CampaignSystem").GetType(
            "TaleWorlds.CampaignSystem.CampaignBehaviors.ExecutionCampaignBehavior", true);
        var target = behavior.GetMethod("OnBloodFeudStateChanged", Instance);
        Check(Harmony.GetPatchInfo(target).Transpilers.Count(p =>
            p.owner == "AnimusForge.Vengeance.Bannerlord15.ExecutionRelations") == 1, "real blood-feud patch installed");
        adapter.GetMethod("Register", Static).Invoke(null, new object[] { null });
        Check(Harmony.GetPatchInfo(target).Transpilers.Count(p =>
            p.owner == "AnimusForge.Vengeance.Bannerlord15.ExecutionRelations") == 1, "registration idempotent");
        bool rejected = false;
        try
        {
            var enumerable = (IEnumerable<CodeInstruction>)adapter.GetMethod("AdaptRelationCalls", Static)
                .Invoke(null, new object[] { new List<CodeInstruction>() });
            enumerable.ToList();
        }
        catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "missing relation call fails closed");
    }

    private static void VerifyDeclaredPatchTargets(Assembly af)
    {
        int checkedTargets = 0, failures = 0;
        foreach (var type in af.GetTypes())
        {
            var attributes = type.GetCustomAttributes(typeof(HarmonyPatch), false).Cast<HarmonyPatch>().ToArray();
            if (attributes.Length == 0) continue;
            if (type.Name == "MilitaryExerciseRenownInfluenceSkipPatch")
            {
                var registration = af.GetType("AnimusForge.MilitaryExerciseBehavior", true)
                    .GetMethod("RegisterHarmonyPatches", Static);
                if (!PatchProcessor.GetOriginalInstructions(registration).Any(i => Equals(i.operand, type)))
                {
                    Console.WriteLine("SKIP inactive renown patch: verified no type token in actual registration IL");
                    continue;
                }
            }
            var patch = HarmonyMethod.Merge(attributes.Select(a => a.info).ToList());
            if (patch.declaringType == null || string.IsNullOrEmpty(patch.methodName)) continue;
            if (!patch.declaringType.Assembly.GetName().Name.StartsWith("TaleWorlds.") &&
                !patch.declaringType.Assembly.GetName().Name.StartsWith("SandBox")) continue;
            MethodInfo target;
            switch (patch.methodType ?? MethodType.Normal)
            {
                case MethodType.Getter: target = AccessTools.PropertyGetter(patch.declaringType, patch.methodName); break;
                case MethodType.Setter: target = AccessTools.PropertySetter(patch.declaringType, patch.methodName); break;
                case MethodType.Normal: target = AccessTools.Method(patch.declaringType, patch.methodName, patch.argumentTypes); break;
                default: continue;
            }
            checkedTargets++;
            if (target == null)
            {
                Console.WriteLine("PATCH_TARGET_MISSING " + type.FullName + " -> " + patch.declaringType.FullName + "." + patch.methodName);
                failures++;
                continue;
            }
            var names = new HashSet<string>(target.GetParameters().Select(p => p.Name));
            foreach (var handler in type.GetMethods(Static | BindingFlags.DeclaredOnly).Where(m =>
                m.Name == "Prefix" || m.Name == "Postfix" || m.IsDefined(typeof(HarmonyPrefix), false) || m.IsDefined(typeof(HarmonyPostfix), false)))
                foreach (var parameter in handler.GetParameters())
                {
                    if (parameter.Name.StartsWith("__") || parameter.IsDefined(typeof(HarmonyArgument), false)) continue;
                    if (names.Contains(parameter.Name)) continue;
                    Console.WriteLine("PATCH_PARAMETER_MISSING " + type.FullName + "." + handler.Name + ": " + parameter.Name);
                    failures++;
                }
        }
        Console.WriteLine("DeclarativePatchTargets=" + checkedTargets + "; failures=" + failures);
        Check(checkedTargets > 20 && failures == 0, "declared game Harmony targets and named parameters resolve");
    }

    private static bool Skip() => false;
    private static object Blank(Type type) => FormatterServices.GetUninitializedObject(type);
    private static FieldInfo Field(Type type, string name) => type.GetField(name, Instance)
        ?? throw new MissingFieldException(type.FullName, name);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("FAIL " + message);
        _checks++;
        Console.WriteLine("PASS " + message);
    }
}
