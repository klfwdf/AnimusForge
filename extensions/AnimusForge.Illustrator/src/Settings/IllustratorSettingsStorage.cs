using System;
using HarmonyLib;
using MCM.Abstractions.Base;

namespace AnimusForge.Illustrator
{
    internal static class IllustratorSettingsStorage
    {
        internal const string DirectoryKey = "AnimusForge";
        private static readonly object Gate = new object();
        private static bool _installed;

        internal static void EnsureReady()
        {
            lock (Gate)
            {
                if (_installed) return;
                var format = typeof(BaseSettings).Assembly.GetType("MCM.Implementation.BaseJsonSettingsFormat");
                if (format == null) throw new MissingMethodException("MCM JSON settings format is unavailable.");
                // Preserve editor-only prompt persistence. Settings/presets use
                // MCM's normal User store, not the Illustrator cache directory.
                IllustratorPromptSettingsPersistence.Install(new Harmony("AnimusForge.Illustrator.SharedMcmSettings"), format);
                _installed = true;
            }
        }
    }
}
