using System;

namespace AnimusForge.SceneActions.Core
{
    /// <summary>
    /// Runtime-only controls are deliberately outside the frozen V1-V4 public
    /// action contracts and are never accepted from AF classifier output.
    /// </summary>
    public static class SceneActionRuntimeControlsV1
    {
        public const string StopAction = "stop_action";
        public const string DrawWeapon = "draw_weapon";
        public const string SheatheWeapon = "sheathe_weapon";

        public static bool IsControlIntent(string intentKey)
        {
            return string.Equals(intentKey, StopAction, StringComparison.Ordinal) ||
                   string.Equals(intentKey, DrawWeapon, StringComparison.Ordinal) ||
                   string.Equals(intentKey, SheatheWeapon, StringComparison.Ordinal);
        }
    }
}
