using System;
using System.IO;
using System.Collections.Generic;

namespace AnimusForge.DialogueUI
{
    internal static class DialogueUiRuntime
    {
        internal const string ModuleId = "AnimusForge_DialogueUI";
        private static readonly HashSet<string> Logged = new HashSet<string>();
        internal static string ModuleRoot { get; private set; }
        internal static bool Enabled { get; private set; }

        internal static void Initialize()
        {
            ModuleRoot = null;
            string hostRoot = AnimusForgeModulePaths.GetCurrentModuleRoot();
            if (!string.IsNullOrWhiteSpace(hostRoot) &&
                File.Exists(Path.Combine(hostRoot, "GUI", "SpriteParts", "afdui_parchment_panel.png")))
            {
                ModuleRoot = hostRoot;
            }
            else
            {
                var directory = new DirectoryInfo(Path.GetDirectoryName(typeof(DialogueUiRuntime).Assembly.Location));
                for (int i = 0; directory != null && i < 5; i++, directory = directory.Parent)
                {
                    string manifest = Path.Combine(directory.FullName, "SubModule.xml");
                    if (File.Exists(manifest) && File.ReadAllText(manifest).Contains("AnimusForge_DialogueUI"))
                    {
                        ModuleRoot = directory.FullName;
                        break;
                    }
                }
            }
            Enabled = ModuleRoot != null;
            Log(Enabled ? "Presentation module loaded; root=" + ModuleRoot : "Module resources unavailable; original UI retained.");
        }

        internal static void Disable() { Enabled = false; }
        internal static void Log(string message)
        {
            try { AnimusForge.Logger.Log("DialogueUI", message); }
            catch { TaleWorlds.Library.Debug.Print("[DialogueUI] " + message); }
        }
        internal static void LogOnce(string key, string message)
        {
            if (Logged.Add(key)) Log(message);
        }
    }
}
