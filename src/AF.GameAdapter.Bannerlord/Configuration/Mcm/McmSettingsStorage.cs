using System;
using System.IO;
using System.Text;
using BUTR.DependencyInjection;
using HarmonyLib;
using MCM.Abstractions.Base;
using MCM.Abstractions;
using MCM.Abstractions.GameFeatures;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

// MCM owns its standard User directory, serializers and presets. Terminal
// controls save the same settings instances; no AF directory override.
internal static class McmSettingsStorage
{
    private static readonly object Gate = new object();
    private static bool _installed;
    [ThreadStatic] private static DuelSettings _savingOwner;
    [ThreadStatic] private static bool _saveFailed;

    internal static void EnsureReady()
    {
        lock (Gate)
        {
            if (_installed) return;
            var format = typeof(BaseSettings).Assembly.GetType("MCM.Implementation.BaseJsonSettingsFormat");
            var load = format == null ? null : AccessTools.Method(format, "Load", new[] { typeof(BaseSettings), typeof(GameDirectory), typeof(string) });
            var save = format == null ? null : AccessTools.Method(format, "Save", new[] { typeof(BaseSettings), typeof(GameDirectory), typeof(string) });
            if (load == null || save == null) throw new MissingMethodException("MCM shared settings persistence boundary is unavailable.");
            var harmony = new Harmony("AnimusForge.SharedMcmSettings");
            harmony.Patch(load,
                postfix: new HarmonyMethod(typeof(McmSettingsStorage), nameof(RestoreTerminalEntry)));
            harmony.Patch(save, postfix: new HarmonyMethod(typeof(McmSettingsStorage), nameof(ObserveSaveResult)));
            _installed = true;
        }
    }

    internal static bool SaveSharedSettings(DuelSettings settings)
    {
        var previousOwner = _savingOwner;
        bool previousFailure = _saveFailed;
        _savingOwner = settings; _saveFailed = false;
        try
        {
            BaseSettingsProvider.Instance.SaveSettings(settings);
            return !_saveFailed;
        }
        finally { _savingOwner = previousOwner; _saveFailed = previousFailure; }
    }

    private static void ObserveSaveResult(BaseSettings settings, bool __result)
    {
        // MCM's provider returns void; retain the real serializer failure signal
        // for the synchronous main-owner save without adding another writer.
        if (ReferenceEquals(settings, _savingOwner) && !__result) _saveFailed = true;
    }

    internal static string GetSharedSettingsFilePath()
    {
        var files = GenericServiceProvider.GetService<IFileSystemProvider>();
        if (files == null) throw new InvalidOperationException("MCM shared filesystem is unavailable.");
        var global = files.GetDirectory(files.GetModSettingsDirectory(), "Global");
        var owner = files.GetDirectory(global, "AnimusForge");
        return files.GetSystemPath(new GameFile(owner, "AnimusForge_global_settings.json"))
            ?? throw new InvalidOperationException("MCM shared settings path is unavailable.");
    }

    private static void RestoreTerminalEntry(GameDirectory directory, string filename, BaseSettings __result)
    {
        if (!(__result is DuelSettings settings) || filename != settings.Id || directory == null
            || !directory.Path.Replace('\\', '/').TrimEnd('/').EndsWith("/ModSettings/Global/AnimusForge", StringComparison.OrdinalIgnoreCase)) return;
        try
        {
            var files = GenericServiceProvider.GetService<IFileSystemProvider>();
            if (files == null) return;
            var file = files.GetFile(directory, filename + ".json");
            byte[] bytes = file == null ? null : files.ReadData(file);
            var stored = bytes == null ? new JObject() : JObject.Parse(Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF'));
            if (stored[nameof(DuelSettings.TerminalHotkeyEnabled)] != null && stored[nameof(DuelSettings.TerminalMapIconEnabled)] != null) return;
            string legacy = Path.Combine(AnimusForgeDataPaths.GetSettingsDirectory(AnimusForgeModulePaths.GetCurrentModuleRoot()), "TerminalSettings.json");
            if (!AnimusForgeTerminalSettings.TryLoadSettingsFile(legacy, out var entry)) return;
            // Only missing entry fields on load. Existing MCM values win; no
            // business configuration is merged, overwritten or deleted.
            if (stored[nameof(DuelSettings.TerminalHotkeyEnabled)] == null) settings.TerminalHotkeyEnabled = entry.IsHotkeyEnabled;
            if (stored[nameof(DuelSettings.TerminalMapIconEnabled)] == null) settings.TerminalMapIconEnabled = entry.IsMapIconEnabled;
        }
        catch (Exception ex) { Logger.Log("TerminalSettings", "Shared entry compatibility skipped: " + ex.GetType().Name); }
    }
}
