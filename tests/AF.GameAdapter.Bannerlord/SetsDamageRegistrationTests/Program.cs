using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using HarmonyLib;

// Actual candidate DLL, game references and Harmony; no Game/Campaign/native mission is started.
internal static class Program
{
    private const string Owner = "AnimusForge.Tests.SetsDamageRegistration";
    private static readonly List<string> Search = new List<string>();
    private static readonly List<string> References = new List<string>();
    private static string LogDirectory;
    private static int Assertions, Failures;
    private static bool FailHall;

    private static int Main(string[] args)
    {
        if (args.Length < 5)
        {
            Console.Error.WriteLine("Usage: SetsDamageRegistrationTests <af.dll> <reference-dirs separated by |> <game-root> <output-dir> <expected-game-version> [--fail-hall]");
            return 2;
        }
        string afPath = Path.GetFullPath(args[0]), gameRoot = Path.GetFullPath(args[2]);
        LogDirectory = Path.GetFullPath(args[3]);
        if (LogDirectory.StartsWith(gameRoot.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Probe logs must be outside the game installation.");
        Directory.CreateDirectory(LogDirectory);
        References.AddRange(args[1].Split('|').Select(Path.GetFullPath));
        Search.AddRange(References);
        Search.Add(Path.GetDirectoryName(afPath));
        Search.Add(Path.Combine(gameRoot, "bin", "Win64_Shipping_Client"));
        foreach (string module in new[] { "AnimusForge", "Native", "SandBox", "SandBoxCore", "StoryMode", "CustomBattle", "Bannerlord.Harmony", "Bannerlord.MBOptionScreen", "Bannerlord.UIExtenderEx", "Bannerlord.ButterLib" })
            Search.Add(Path.Combine(gameRoot, "Modules", module, "bin", "Win64_Shipping_Client"));
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        FailHall = args.Contains("--fail-hall");
        try { return Run(afPath, args[4]); }
        catch (Exception ex) { Console.WriteLine("FAIL probe setup: " + ex); return 1; }
    }

    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        string name = new AssemblyName(args.Name).Name;
        if (name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase)) return null;
        Assembly existing = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name);
        if (existing != null) return existing;
        foreach (string dir in IsGameAssembly(name) ? References : Search)
        {
            string path = Path.Combine(dir, name + ".dll");
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        Console.WriteLine("UNRESOLVED " + name);
        return null;
    }

    private static int Run(string afPath, string expectedVersion)
    {
        string before = Hash(afPath);
        Console.WriteLine("CANDIDATE SHA256=" + before);
        Assembly af = Assembly.LoadFrom(afPath);
        var isolation = new Harmony(Owner + ".Isolation");
        MethodInfo logs = af.GetType("AnimusForge.AnimusForgeModulePaths", true).GetMethod("GetLogsDirectory", BindingFlags.Public | BindingFlags.Static);
        isolation.Patch(logs, prefix: new HarmonyMethod(typeof(Program), nameof(RedirectLogs)));
        if (FailHall)
        {
            // Simulate only a missing optional hall target, without modifying any candidate file.
            var lookup = typeof(AccessTools).GetMethod("Method", new[] { typeof(Type), typeof(string), typeof(Type[]), typeof(Type[]) });
            isolation.Patch(lookup, prefix: new HarmonyMethod(typeof(Program), nameof(MissingHallTarget)));
        }
        Type sets = af.GetType("AnimusForge.SettlementEntryTroopSelectionBehavior", true);
        Exception failure = null;
        try { sets.GetMethod("RegisterHarmonyPatches").Invoke(null, new object[] { new Harmony(Owner) }); }
        catch (TargetInvocationException ex) { failure = ex.InnerException ?? ex; }
        if (failure != null) Console.WriteLine("REGISTRATION_EXCEPTION " + failure);
        Check(FailHall ? failure != null : failure == null, FailHall ? "optional hall failure was injected" : "registration completes without exception");

        var registered = Harmony.GetAllPatchedMethods().SelectMany(target => Harmony.GetPatchInfo(target).Prefixes.Concat(Harmony.GetPatchInfo(target).Postfixes).Concat(Harmony.GetPatchInfo(target).Finalizers)
            .Where(p => p.owner == Owner).Select(p => new { Target = target, Handler = p.PatchMethod.Name })).ToArray();
        foreach (var patch in registered)
            Console.WriteLine("PATCH " + patch.Handler + " => " + patch.Target.DeclaringType.FullName + "." + patch.Target);
        foreach (string prefix in new[] { "TownCreateAndOpenMissionControllerPrefix", "CastleCreateAndOpenMissionControllerPrefix", "VillageCreateAndOpenMissionControllerPrefix", "AllowSetsEntryPlayerDamagePrefix", "SetsAgentNavigatorSetTargetPrefix", "SetsUsableMissionObjectMoveToUsePrefix", "SetsNativeAlleyStartPostfix", "SetsNativeAlleyEndFightFinalizer", "SetsMissionFightEndPostfix" })
            Check(registered.Any(p => p.Handler == prefix), "actual hook installed: " + prefix);
        Check(registered.Count(p => p.Handler == "AllowSetsVictoryEndMissionGuardPrefix") == 2, "both native leave guards installed");
        if (!FailHall)
        {
            Check(registered.Any(p => p.Handler == "CoupHallPlayerSpawnPrefix"), "hall spawn patch remains installed");
            var hall = registered.Where(p => p.Handler == "CoupHallSceneLayerPrefix").ToArray();
            Check(hall.Length == 1 && hall[0].Target.GetParameters().Any(p => p.Name == "sceneLevels"), "hall layer patch binds the sceneLevels overload");
        }
        var damage = sets.GetMethod("AllowSetsEntryPlayerDamagePrefix", BindingFlags.Static | BindingFlags.NonPublic);
        object[] missingContext = { null, null, null, true };
        Check((bool)damage.Invoke(null, missingContext) && (bool)missingContext[3], "missing Mission bypasses the AF damage override");
        Assembly library = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "TaleWorlds.Library");
        string version = (string)library.GetType("BuildInfo", true).GetField("GameVersion").GetValue(null);
        Check(version == expectedVersion, "reference version: " + version);
        foreach (Assembly loaded in AppDomain.CurrentDomain.GetAssemblies().Where(a => IsGameAssembly(a.GetName().Name)))
            if (!References.Contains(Path.GetDirectoryName(loaded.Location), StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("Unexpected cross-version reference: " + loaded.Location);
        Check(before == Hash(afPath), "candidate DLL unchanged");
        Console.WriteLine("Assertions=" + Assertions + "; Failures=" + Failures + "; Harmony=REAL; NativeMission=NOT_RUN");
        return Failures == 0 ? 0 : 1;
    }

    private static bool RedirectLogs(ref string __result) { __result = LogDirectory; return false; }
    private static bool MissingHallTarget(Type type, string name, ref MethodInfo __result)
    {
        if (name != "SpawnPlayer" || type?.Name != "MissionHelper") return true;
        __result = null;
        return false;
    }
    private static bool IsGameAssembly(string name) => name.StartsWith("TaleWorlds.", StringComparison.Ordinal)
        || name == "SandBox" || name.StartsWith("SandBox.", StringComparison.Ordinal)
        || name == "StoryMode" || name.StartsWith("StoryMode.", StringComparison.Ordinal);
    private static string Hash(string path)
    {
        using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
    }
    private static void Check(bool value, string name)
    {
        Assertions++; if (!value) Failures++;
        Console.WriteLine((value ? "PASS " : "FAIL ") + name);
    }
}
