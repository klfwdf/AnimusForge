using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AnimusForge.DiplomacyHotfix;
using HarmonyLib;

internal static class Program
{
    private static string Root, Api;
    private static int Main(string[] args)
    {
        Root = Path.GetFullPath(args[0]); Api = args[2];
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        try
        {
            var assembly = Assembly.LoadFrom(Path.GetFullPath(args[1]));
            if (args.Length > 3 && args[3] == "negative")
            {
                ActualMaintenanceReplay.Run(assembly, Root);
                throw new Exception("Original vulnerable AF unexpectedly passed");
            }
            if (args.Length > 3 && args[3] == "dump")
            {
                var method = AccessTools.DeclaredMethod(assembly.GetType("AnimusForge.WorldDiplomacyOrchestration"), "MaintainPlayerEvents");
                int index = 0;
                foreach (var instruction in PatchProcessor.GetOriginalInstructions(method)) Console.WriteLine((index++) + ": " + instruction);
                return 0;
            }
            string result = HotfixInstaller.Install(assembly, Console.WriteLine);
            Console.WriteLine("RESULT " + result);
            string repeated = HotfixInstaller.Install(assembly, Console.WriteLine);
            if (result != repeated) throw new Exception("Installation is not idempotent");
            string expected = args.Length > 3 ? args[3] : "maintenance=applied;requests=applied";
            if (result != expected) throw new Exception("Expected " + expected);
            if (result.StartsWith("maintenance=applied") || result.StartsWith("maintenance=already_fixed")) ActualMaintenanceReplay.Run(assembly, Root);
            Console.WriteLine("PASS actual .NET Framework AF assembly patch installation and repeated installation");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        string name = new AssemblyName(args.Name).Name + ".dll";
        string game = @"F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord";
        foreach (string directory in new[] {
            Path.Combine(Root, Api == "1.3" ? "_deps_auto" : ".tmp/build_check/1.4"),
            Path.Combine(Root, "_deps_auto"), Path.Combine(game,"bin/Win64_Shipping_Client"),
            Path.Combine(game,"Modules/Native/bin/Win64_Shipping_Client"),
            Path.Combine(game,"Modules/AnimusForge/bin/Win64_Shipping_Client") })
        {
            string path = Path.Combine(directory, name);
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        return null;
    }
}
