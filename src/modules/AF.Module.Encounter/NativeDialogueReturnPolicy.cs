namespace AnimusForge.Refactor.Modules;

// Returning to encounter options is not release authorization or a combat action.
internal static class NativeDialogueReturnPolicy
{
    internal static bool ShouldReturn(bool intercepted, bool currentScope, bool released,
        bool combatOrResult, bool nativeActivity)
        => intercepted && currentScope && !released && !combatOrResult && !nativeActivity;
}
