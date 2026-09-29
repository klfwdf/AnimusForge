using System;
using System.Reflection;
using HarmonyLib;

namespace AnimusForge.DialogueUI.Native;

internal static class IllustratorBridge
{
    private const string PatchTypeName = "AnimusForge.Illustrator.UI.Patches.ConversationIllustrationPatch";
    private const string RuntimeTypeName = "AnimusForge.Illustrator.Core.IllustratorRuntime";
    private static bool _resolved;
    private static MethodInfo _callback;
    private static MethodInfo _isEnabled;

    internal static bool IsAvailable()
    {
        Resolve();
        if (_callback == null) return false;
        if (_isEnabled == null) return true;
        try { return _isEnabled.Invoke(null, new object[] { "conversation" }) is bool enabled && enabled; }
        catch { return false; }
    }

    internal static void Invoke()
    {
        if (!IsAvailable()) return;
        try { _callback.Invoke(null, null); }
        catch (Exception ex) { DialogueUiRuntime.Log("Illustrator callback failed: " + ex.GetType().Name + ": " + ex.Message); }
    }

    private static void Resolve()
    {
        if (_resolved) return;
        _resolved = true;
        try
        {
            Type patchType = AccessTools.TypeByName(PatchTypeName);
            _callback = AccessTools.Method(patchType, "HandleConversationIllustrateClicked");
            Type runtimeType = AccessTools.TypeByName(RuntimeTypeName);
            _isEnabled = AccessTools.Method(runtimeType, "IsEnabled", new[] { typeof(string) });
        }
        catch { _callback = null; _isEnabled = null; }
    }
}
