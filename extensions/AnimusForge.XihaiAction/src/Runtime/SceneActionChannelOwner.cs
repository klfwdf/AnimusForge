using System;
using System.Linq;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    /// <summary>
    /// Owns the narrow engine operation used to release an action channel.
    /// Higher-level state dictionaries remain in the MissionBehavior for now;
    /// this component is the replacement seam for the next ownership split.
    /// </summary>
    internal static class SceneActionChannelOwner
    {
        public static bool TryReleaseOwnedChannel(
            Agent agent,
            int channel,
            bool ownershipAccepted,
            params ActionIndexCache[] ownedActions)
        {
            return TryReleaseOwnedChannelWithContext(
                agent,
                channel,
                ownershipAccepted,
                "SceneActionChannelOwner",
                ownedActions);
        }

        public static bool TryReleaseOwnedChannelWithContext(
            Agent agent,
            int channel,
            bool ownershipAccepted,
            string diagnosticContext,
            params ActionIndexCache[] ownedActions)
        {
            if (!ownershipAccepted)
            {
                return true;
            }
            try
            {
                ActionIndexCache current = agent.GetCurrentAction(channel);
                if (current == ActionIndexCache.act_none)
                {
                    return true;
                }
                if (!(ownedActions ?? Array.Empty<ActionIndexCache>()).Any(action =>
                    action != ActionIndexCache.act_none && current == action))
                {
                    return false;
                }
                ActionIndexCache none = ActionIndexCache.act_none;
                bool released = agent.SetActionChannel(
                    channel,
                    in none,
                    // The ownership check above is the safety boundary. Once the
                    // current native action is still the one we submitted, a
                    // priority-aware clear can be rejected after Advance, leaving
                    // a cheer or gesture stuck on a soldier.
                    ignorePriority: true,
                    additionalFlags: 0,
                    blendWithNextActionFactor: 0f,
                    actionSpeed: 1f,
                    blendInPeriod: 0.1f);
                if (!released)
                {
                    SceneActionsLog.Warning(
                        "CHANNEL",
                        (diagnosticContext ?? "SceneActionChannelOwner") +
                        " failed to release channel=" + channel +
                        " currentAction=" + current);
                }
                else
                {
                    SceneActionsLog.Info(
                        "CHANNEL",
                        (diagnosticContext ?? "SceneActionChannelOwner") +
                        " released channel=" + channel +
                        " currentAction=" + current +
                        " forced=true");
                }
                return released;
            }
            catch (Exception ex)
            {
                SceneActionsLog.Warning(
                    "CHANNEL",
                    (diagnosticContext ?? "SceneActionChannelOwner") +
                    " threw while releasing channel=" + channel +
                    ": " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }
    }
}
