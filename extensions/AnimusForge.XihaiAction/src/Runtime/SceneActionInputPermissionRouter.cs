using AnimusForge.SceneActions.Core;

namespace AnimusForge.XihaiAction
{
    /// <summary>
    /// Keeps source-channel enablement and source routing out of the Mission executor.
    /// This is deliberately pure: it cannot inspect or mutate an Agent or a Mission.
    /// </summary>
    internal static class SceneActionInputRouter
    {
        public static bool IsEnabled(SceneActionSettings settings, SceneInputSource source)
        {
            if (settings == null || !settings.Enabled)
            {
                return false;
            }
            return source == SceneInputSource.PlayerSceneShout
                ? settings.PlayerSceneShoutEnabled
                : source == SceneInputSource.NpcSceneShoutReply &&
                  settings.NpcSceneShoutReplyEnabled;
        }

        public static bool IsPlayer(SceneInputSource source)
        {
            return source == SceneInputSource.PlayerSceneShout;
        }
    }

    /// <summary>
    /// Freezes actor authority and batch policy before the Mission layer touches targets.
    /// No method in this class can grant a target, force flag, or consent by itself.
    /// </summary>
    internal static class SceneActionPermissionRouter
    {
        public static bool TryResolveTargetMode(
            ParseDecision decision,
            IntentDefinition intent,
            out TargetMode mode)
        {
            mode = TargetMode.Player;
            if (decision == null || intent == null)
            {
                return false;
            }

            mode = decision.TargetOverride ?? intent.DefaultTargetMode;
            return true;
        }

        public static bool RequiresNpcConsent(
            ParseDecision decision,
            TargetMode mode)
        {
            return mode != TargetMode.Player && decision?.BypassNpcConsent != true;
        }

        public static bool ShouldStaggerNpcBatch(
            ParseDecision decision,
            int requestedCount,
            SceneActionSettings settings)
        {
            return decision?.BypassNpcConsent != true &&
                   settings != null &&
                   requestedCount >= settings.StaggerFromTargetCount;
        }

        public static bool ShouldUseForcedStepBarriers(
            ParseDecision decision,
            int requestedCount,
            SceneActionSettings settings)
        {
            return decision?.BypassNpcConsent == true &&
                   settings != null &&
                   requestedCount >= settings.ForceMultiTargetThreshold;
        }

        public static bool ShouldUseForcedIndependentStagger(
            ParseDecision decision,
            int requestedCount,
            SceneActionSettings settings)
        {
            return decision?.BypassNpcConsent == true &&
                   settings != null &&
                   requestedCount >= settings.ForceMultiTargetThreshold;
        }
    }
}
