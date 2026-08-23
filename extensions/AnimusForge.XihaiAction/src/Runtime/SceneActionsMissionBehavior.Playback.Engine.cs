using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AnimusForge.SceneActions.Core;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    internal sealed partial class SceneActionsMissionBehavior
    {
        private bool TryValidateAgent(
            SessionAgentHandle handle,
            out Agent agent,
            out ExecutionResultCode failure,
            bool allowOwnedChannelZero = false)
        {
            agent = handle?.Agent;
            failure = ExecutionResultCode.AgentNotFound;
            try
            {
                if (handle == null ||
                    handle.SessionGeneration != _sessionGeneration ||
                    !ReferenceEquals(handle.Mission, Mission))
                {
                    failure = ExecutionResultCode.MissionChanged;
                    return false;
                }
                if (agent == null || agent.Index != handle.AgentIndex ||
                    !ReferenceEquals(agent.Mission, Mission))
                {
                    return false;
                }
                if (!agent.IsActive() || agent.Health <= 0f)
                {
                    failure = ExecutionResultCode.AgentInactive;
                    return false;
                }
                if (!agent.IsHuman)
                {
                    failure = ExecutionResultCode.AgentNonHuman;
                    return false;
                }
                if (agent.MountAgent != null ||
                    agent.IsInBeingStruckAction ||
                    (!allowOwnedChannelZero &&
                     agent.GetCurrentActionStage(0) != Agent.ActionStage.None))
                {
                    failure = ExecutionResultCode.EngineCriticalState;
                    return false;
                }
                failure = ExecutionResultCode.Queued;
                return true;
            }
            catch
            {
                failure = ExecutionResultCode.ExecutorException;
                return false;
            }
        }
        private static bool TrySetAction(
            Agent agent,
            ActionVariant variant,
            ActionIndexCache action,
            out string reason)
        {
            return TrySetAction(
                agent,
                variant,
                action,
                variant.Channel,
                variant.EnforceAll ? AnimFlags.anf_enforce_all : 0,
                out reason);
        }
        private static bool TrySetAction(
            Agent agent,
            ActionVariant variant,
            ActionIndexCache action,
            int channel,
            AnimFlags additionalFlags,
            out string reason)
        {
            reason = null;
            try
            {
                float blend = variant.BlendInSeconds;
                SceneActionSettings settings = SceneActionsRuntimeHost.Settings;
                if (settings.ActionOverrides.TryGetValue(
                    FindActionKeyForVariant(variant),
                    out ActionOverride actionOverride) &&
                    actionOverride?.BlendInSeconds.HasValue == true)
                {
                    blend = actionOverride.BlendInSeconds.Value;
                }
                bool accepted = agent.SetActionChannel(
                    channel,
                    in action,
                    ignorePriority: false,
                    additionalFlags: additionalFlags,
                    blendWithNextActionFactor: 0f,
                    actionSpeed: variant.ActionSpeed,
                    blendInPeriod: blend);
                if (!accepted)
                {
                    reason = "SetActionChannel returned false with ignorePriority=false.";
                }
                return accepted;
            }
            catch (Exception ex)
            {
                reason = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }
        private static string FindActionKeyForVariant(ActionVariant variant)
        {
            foreach (ActionDefinition definition in SceneActionsRuntimeHost.Catalog.Actions.Values)
            {
                if (definition.RuntimeVariants.Any(candidate => ReferenceEquals(candidate, variant)))
                {
                    return definition.Key;
                }
            }
            return string.Empty;
        }
    }
}