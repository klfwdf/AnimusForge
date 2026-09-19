using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using HarmonyLib;

internal static class Program
{
    private const string RegistrationOwner = "AnimusForge.Coup.RuntimeProbe.Registration";
    private const string LoggingOwner = "AnimusForge.Coup.RuntimeProbe.LogIsolation";
    private static readonly List<string> SearchDirectories = new List<string>();
    private static readonly HashSet<string> Resolving = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private static StreamWriter _output;
    private static string _logsDirectory;
    private static string _gameRoot;
    private static string _afPath;
    private static string _coupPath;

    private static int Main(string[] args)
    {
        if (args.Length != 4)
        {
            Console.Error.WriteLine("Usage: Coup.RuntimeProbe.exe <game-root> <installed-af.dll> <coup.dll> <workspace-log-directory>");
            return 2;
        }
        _gameRoot = Path.GetFullPath(args[0]);
        _afPath = Path.GetFullPath(args[1]);
        _coupPath = Path.GetFullPath(args[2]);
        string root = Path.GetFullPath(args[3]);
        if (root.StartsWith(_gameRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Probe logs must be outside the game installation.");
        Directory.CreateDirectory(root);
        _logsDirectory = Path.Combine(root, "af-logs");
        Directory.CreateDirectory(_logsDirectory);
        using (_output = new StreamWriter(Path.Combine(root, "registration.log"), false))
        {
            _output.AutoFlush = true;
            SearchDirectories.Add(Path.GetDirectoryName(_afPath));
            SearchDirectories.Add(Path.GetDirectoryName(_coupPath));
            SearchDirectories.Add(Path.Combine(_gameRoot, "bin", "Win64_Shipping_Client"));
            foreach (string module in new[] { "Native", "SandBox", "SandBoxCore", "StoryMode", "CustomBattle", "Bannerlord.Harmony", "Bannerlord.MBOptionScreen", "Bannerlord.UIExtenderEx", "Bannerlord.ButterLib" })
                SearchDirectories.Add(Path.Combine(_gameRoot, "Modules", module, "bin", "Win64_Shipping_Client"));
            SearchDirectories.Add(Path.Combine(_gameRoot, "Modules", "AnimusForge", "bin", "Win64_Shipping_Client"));
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
            try { return RunProbe(); }
            catch (Exception ex) { Write("FAIL " + ex); return 1; }
        }
    }

    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        string simple = new AssemblyName(args.Name).Name;
        if (simple.EndsWith(".resources", StringComparison.OrdinalIgnoreCase) || !Resolving.Add(simple)) return null;
        try
        {
            foreach (Assembly existing in AppDomain.CurrentDomain.GetAssemblies())
                if (string.Equals(existing.GetName().Name, simple, StringComparison.OrdinalIgnoreCase)) return existing;
            foreach (string directory in SearchDirectories)
            {
                string candidate = Path.Combine(directory, simple + ".dll");
                if (File.Exists(candidate)) return Assembly.LoadFrom(candidate);
            }
            Write("UNRESOLVED " + args.Name);
            return null;
        }
        finally { Resolving.Remove(simple); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int RunProbe()
    {
        Write("UTC " + DateTime.UtcNow.ToString("O"));
        Write("CLR " + Environment.Version + "; 64-bit=" + Environment.Is64BitProcess);
        string beforeAf = Hash(_afPath);
        string beforeCoup = Hash(_coupPath);
        Assembly af = Assembly.LoadFrom(_afPath);
        Assembly coup = Assembly.LoadFrom(_coupPath);
        Write("AF " + af.FullName + "; MVID=" + af.ManifestModule.ModuleVersionId + "; SHA256=" + beforeAf);
        Write("COUP " + coup.FullName + "; MVID=" + coup.ManifestModule.ModuleVersionId + "; SHA256=" + beforeCoup);

        // Only a log destination is redirected. All game/AF methods being tested remain
        // the actual binaries, with no fabricated Campaign, Mission, Agent or model.
        var logIsolation = new Harmony(LoggingOwner);
        MethodInfo logDirectory = af.GetType("AnimusForge.AnimusForgeModulePaths", true).GetMethod("GetLogsDirectory", BindingFlags.Public | BindingFlags.Static);
        logIsolation.Patch(logDirectory, prefix: new HarmonyMethod(typeof(Program), nameof(LogDirectoryPrefix)));
        Write("LOG_DESTINATION " + _logsDirectory);
        Type sets = coup.GetType("AnimusForge.CoupSystem.SettlementEntryTroopSelectionBehavior", true);
        Type guards = coup.GetType("AnimusForge.CoupSystem.CoupGuards", true);
        Type rebellion = coup.GetType("AnimusForge.CoupSystem.CoupRebellionBridge", true);
        var harmony = new Harmony(RegistrationOwner);

        Invoke(sets, "Register", harmony);
        Invoke(rebellion, "Initialize");
        Invoke(guards, "Register", harmony);

        bool ready = ReadFlag(sets, "IsAvailable") & ReadFlag(rebellion, "IsAvailable")
            & ReadFlag(guards, "MissionProtectionAvailable") & ReadFlag(guards, "CaptivityProtectionAvailable");
        int prefixCount = 0, transpilerCount = 0, targetCount = 0;
        foreach (MethodBase target in Harmony.GetAllPatchedMethods().OrderBy(m => m.DeclaringType.FullName).ThenBy(m => m.Name))
        {
            Patches patches = Harmony.GetPatchInfo(target);
            var prefixes = patches.Prefixes.Where(p => p.owner == RegistrationOwner).ToArray();
            var transpilers = patches.Transpilers.Where(p => p.owner == RegistrationOwner).ToArray();
            if (prefixes.Length + transpilers.Length == 0) continue;
            targetCount++;
            prefixCount += prefixes.Length;
            transpilerCount += transpilers.Length;
            Write("PATCH " + target.DeclaringType.FullName + "." + target.Name + " prefixes=" + prefixes.Length + " transpilers=" + transpilers.Length);
        }
        Write("PATCH_TOTAL targets=" + targetCount + " prefixes=" + prefixCount + " transpilers=" + transpilerCount);
        bool unchanged = beforeAf == Hash(_afPath) && beforeCoup == Hash(_coupPath);
        Write("SOURCE_DLLS_UNCHANGED " + unchanged);
        Write("Game/Campaign/mission not started; no LLM provider invoked; registration only.");
        Write(ready && unchanged && targetCount > 0 ? "PASS registration smoke" : "FAIL registration smoke");
        // Keep log redirection until process exit: AF may flush its background log queue.
        return ready && unchanged && targetCount > 0 ? 0 : 1;
    }

    private static bool LogDirectoryPrefix(ref string __result)
    {
        __result = _logsDirectory;
        return false;
    }

    private static object Invoke(Type type, string name, params object[] arguments)
    {
        Write("INVOKE " + type.FullName + "." + name);
        MethodInfo method = type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
            ?? throw new MissingMethodException(type.FullName, name);
        try { return method.Invoke(null, arguments); }
        catch (TargetInvocationException ex) { throw ex.InnerException ?? ex; }
    }

    private static bool ReadFlag(Type type, string name)
    {
        bool result = (bool)type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        Write("FLAG " + type.Name + "." + name + "=" + result);
        return result;
    }

    private static string Hash(string file)
    {
        using (var hash = SHA256.Create()) using (var stream = File.OpenRead(file))
            return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }

    private static void Write(string line)
    {
        Console.WriteLine(line);
        _output.WriteLine(line);
    }
}
