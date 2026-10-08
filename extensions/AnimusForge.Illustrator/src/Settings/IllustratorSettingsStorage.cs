using System;
using System.IO;
using HarmonyLib;
using MCM.Abstractions.Base;
using MCM.Abstractions.GameFeatures;
using AnimusForge.Illustrator.Engine;

namespace AnimusForge.Illustrator
{
    // Only this settings object's folder and presets are redirected. MCM keeps
    // its existing serializer, keys, dropdown handling and all other mod paths.
    internal static class IllustratorSettingsStorage
    {
        internal const string DirectoryKey = "AnimusForge.Illustrator.ModuleStorage";
        private const string SettingsId = "AnimusForge_Illustrator_v1";
        private static readonly object Gate = new object();
        private static bool _installed;

        internal static void EnsureReady()
        {
            lock (Gate)
            {
                if (_installed) return;
                IllustratorStoragePaths.EnsureDirectory(IllustratorStoragePaths.ConfigDirectory);
                var provider = typeof(BaseSettings).Assembly.GetType("MCM.Internal.GameFeatures.FileSystemProvider");
                if (provider == null) throw new InvalidOperationException("MCM file provider is unavailable; Illustrator settings stay disabled.");
                var create = AccessTools.Method(provider, "GetOrCreateDirectory", new[] { typeof(GameDirectory), typeof(string) });
                var get = AccessTools.Method(provider, "GetDirectory", new[] { typeof(GameDirectory), typeof(string) });
                if (create == null || get == null) throw new MissingMethodException("MCM module storage routing is unavailable.");
                var format = typeof(BaseSettings).Assembly.GetType("MCM.Implementation.BaseJsonSettingsFormat");
                var signature = new[] { typeof(BaseSettings), typeof(GameDirectory), typeof(string) };
                var load = format == null ? null : AccessTools.Method(format, "Load", signature);
                var save = format == null ? null : AccessTools.Method(format, "Save", signature);
                if (load == null || save == null) throw new MissingMethodException("MCM JSON storage routing is unavailable.");
                var harmony = new Harmony("AnimusForge.Illustrator.ModuleStorage");
                var prefix = new HarmonyMethod(typeof(IllustratorSettingsStorage), nameof(RouteDirectory));
                harmony.Patch(create, prefix: prefix);
                harmony.Patch(get, prefix: prefix);
                // Also route at the serializer boundary: do not depend on a
                // tiny directory helper remaining uninlined by the runtime.
                var jsonPrefix = new HarmonyMethod(typeof(IllustratorSettingsStorage), nameof(RouteJson));
                harmony.Patch(load, prefix: jsonPrefix);
                harmony.Patch(save, prefix: jsonPrefix);
                IllustratorPromptSettingsPersistence.Install(harmony, format);
                _installed = true;
            }
        }

        private static void RouteJson(BaseSettings settings, ref GameDirectory directory)
        {
            if (settings is IllustratorSettings)
                directory = ModuleDirectory(IllustratorStoragePaths.ConfigDirectory);
        }

        private static bool RouteDirectory(GameDirectory directory, string name, ref GameDirectory __result)
        {
            if (string.Equals(name, DirectoryKey, StringComparison.Ordinal))
            {
                __result = ModuleDirectory(IllustratorStoragePaths.ConfigDirectory);
                return false;
            }
            if (string.Equals(name, SettingsId, StringComparison.Ordinal) && directory != null
                && directory.Path.Replace((char)92, '/').TrimEnd('/').EndsWith("/ModSettings/Presets", StringComparison.OrdinalIgnoreCase))
            {
                __result = ModuleDirectory(Path.Combine(IllustratorStoragePaths.ConfigDirectory, "Presets"));
                return false;
            }
            return true;
        }

        private static GameDirectory ModuleDirectory(string path)
            => new GameDirectory(PlatformDirectoryType.Application, Path.GetFullPath(path) + Path.DirectorySeparatorChar);

    }
}
