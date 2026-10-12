using System;
using System.IO;
using HarmonyLib;
using MCM.Abstractions.Base;
using MCM.Abstractions.GameFeatures;

namespace AnimusForge;

// Route only AF's three settings IDs. Keep MCM's serializers, fields, indices,
// snapshot semantics and the existing Illustrator-specific route unchanged.
internal static class McmSettingsStorage
{
    private static readonly object Gate = new object();
    private static bool _installed;

    internal static void EnsureReady()
    {
        lock (Gate)
        {
            if (_installed) return;
            string root = AnimusForgeDataPaths.GetCurrentRoot();
            string settingsRoot = Path.Combine(root, "Configs", "ModSettings");
            AnimusForgeDataPaths.ValidatePath(settingsRoot);
            Directory.CreateDirectory(settingsRoot);
            AnimusForgeDataPaths.ValidatePath(settingsRoot);
            var assembly = typeof(BaseSettings).Assembly;
            var provider = assembly.GetType("MCM.Internal.GameFeatures.FileSystemProvider");
            var format = assembly.GetType("MCM.Implementation.BaseJsonSettingsFormat");
            var directorySignature = new[] { typeof(GameDirectory), typeof(string) };
            var jsonSignature = new[] { typeof(BaseSettings), typeof(GameDirectory), typeof(string) };
            var create = provider == null ? null : AccessTools.Method(provider, "GetOrCreateDirectory", directorySignature);
            var get = provider == null ? null : AccessTools.Method(provider, "GetDirectory", directorySignature);
            var load = format == null ? null : AccessTools.Method(format, "Load", jsonSignature);
            var save = format == null ? null : AccessTools.Method(format, "Save", jsonSignature);
            if (create == null || get == null || load == null || save == null)
                throw new MissingMethodException("MCM module settings storage routing is unavailable.");
            var harmony = new Harmony("AnimusForge.ModuleSettingsStorage");
            var directoryPrefix = new HarmonyMethod(typeof(McmSettingsStorage), nameof(RoutePresetDirectory));
            harmony.Patch(create, prefix: directoryPrefix);
            harmony.Patch(get, prefix: directoryPrefix);
            var jsonPrefix = new HarmonyMethod(typeof(McmSettingsStorage), nameof(RouteJson));
            harmony.Patch(load, prefix: jsonPrefix);
            harmony.Patch(save, prefix: jsonPrefix);
            _installed = true;
        }
    }

    private static bool IsOwnedId(string id)
        => string.Equals(id, "AnimusForge_global_settings", StringComparison.Ordinal)
            || string.Equals(id, "AnimusForge_DialogueUI_v1", StringComparison.Ordinal)
            || string.Equals(id, "AnimusForge_Coup_v1", StringComparison.Ordinal);

    private static void RouteJson(BaseSettings settings, ref GameDirectory directory, string filename)
    {
        if (settings == null || !IsOwnedId(settings.Id)) return;
        string root = AnimusForgeDataPaths.GetCurrentRoot();
        string folder = settings.Id == "AnimusForge_Coup_v1" ? "AnimusForge_Coup" : "AnimusForge";
        string target = Path.Combine(root, "Configs", "ModSettings", "Global", folder);
        // Preset serializers may also receive BaseSettings. Keep their existing
        // per-ID directory rather than turning every preset into global settings.
        if (IsPresetsDirectory(directory))
            target = Path.Combine(root, "Configs", "ModSettings", "Presets", settings.Id);
        AnimusForgeDataPaths.ValidatePath(target);
        Directory.CreateDirectory(target);
        AnimusForgeDataPaths.ValidatePath(target);
        string fileName = filename + ".json";
        if (!string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal))
            throw new InvalidOperationException("MCM settings filename must remain inside its owned directory.");
        string destination = Path.Combine(target, fileName);
        AnimusForgeDataPaths.ValidatePath(destination);
        AnimusForgeDataPaths.ValidatePath(destination);
        directory = ModuleDirectory(target);
    }

    private static bool RoutePresetDirectory(GameDirectory directory, string name, ref GameDirectory __result)
    {
        if (!IsOwnedId(name) || directory == null
            || !directory.Path.Replace('\u005c', '/').TrimEnd('/').EndsWith("/ModSettings/Presets", StringComparison.OrdinalIgnoreCase)) return true;
        string root = AnimusForgeDataPaths.GetCurrentRoot();
        string target = Path.Combine(root, "Configs", "ModSettings", "Presets", name);
        AnimusForgeDataPaths.ValidatePath(target);
        Directory.CreateDirectory(target);
        AnimusForgeDataPaths.ValidatePath(target);
        __result = ModuleDirectory(target);
        return false;
    }

    private static bool IsPresetsDirectory(GameDirectory directory)
        => directory != null && directory.Path.Replace('\u005c', '/').IndexOf("/ModSettings/Presets/", StringComparison.OrdinalIgnoreCase) >= 0;

    private static GameDirectory ModuleDirectory(string path)
        => new GameDirectory(PlatformDirectoryType.Application, Path.GetFullPath(path) + Path.DirectorySeparatorChar);
}
