using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

// Run in a fresh .NET Framework process for each final implementation DLL.
// No Campaign, settings instance, game launch or player configuration is needed.
internal static class LegacySettingsInitializationProbe
{
    private static int Main(string[] args)
    {
        if (args.Length < 3) return 2;
        // Explicit isolated fixture; never use the player's installed module.
        Environment.SetEnvironmentVariable("ANIMUSFORGE_DATA_ROOT", Path.GetFullPath(args[1]));
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs request)
        {
            string name = new AssemblyName(request.Name).Name;
            if (name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase)) return null;
            for (int i = 2; i < args.Length; i++)
            {
                string path = Path.Combine(args[i], name + ".dll");
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        };
        try
        {
            Assembly assembly = Assembly.LoadFrom(Path.GetFullPath(args[0]));
            Type settings = assembly.GetType("AnimusForge.DuelSettings", true);
            RuntimeHelpers.RunClassConstructor(settings.TypeHandle);
            Type reader = settings.GetNestedType("LegacySettingsPrompts", BindingFlags.NonPublic);
            var values = (Dictionary<string, string>)reader.GetField("Values", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            if (values.Count != 56) throw new Exception("Expected all 56 historical migration values.");
            foreach (var pair in values)
            {
                var field = settings.GetField(pair.Key, BindingFlags.NonPublic | BindingFlags.Static);
                if (field == null || !string.Equals((string)field.GetValue(null), pair.Value, StringComparison.Ordinal))
                    throw new Exception("Historical initializer mismatch: " + pair.Key);
            }
            foreach (string suffix in new[] { "Cn", "En" })
                if (string.IsNullOrEmpty(values["PreviousDefaultWorldDiplomacyContractCoreV25" + suffix]))
                    throw new Exception("Missing bilingual V25 migration input.");
            Console.WriteLine("PASS actual DuelSettings static initialization; 56 exact field/resource pairs; V25 CN/EN present; MVID=" + assembly.ManifestModule.ModuleVersionId);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
