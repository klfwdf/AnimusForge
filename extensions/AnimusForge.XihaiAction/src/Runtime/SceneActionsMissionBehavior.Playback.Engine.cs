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
                // Voiced native clips start past their embedded voice trigger
                // (in-game tested: 0.5 removes the yell).
                float startProgress = IsVoicedNativeClip(action)
                    ? VoicedNativeClipStartProgress
                    : 0f;
                bool accepted = agent.SetActionChannel(
                    channel,
                    in action,
                    ignorePriority: false,
                    additionalFlags: additionalFlags,
                    blendWithNextActionFactor: 0f,
                    actionSpeed: variant.ActionSpeed,
                    blendInPeriod: blend,
                    startProgress: startProgress);
                if (startProgress > 0f &&
                    SceneActionsRuntimeHost.Settings?.DeveloperDiagnosticsEnabled == true)
                {
                    SceneActionsLog.Info(
                        "VOICE_SKIP",
                        "Agent=" + agent.Index + " ActionIndex=" + action.Index +
                        " StartProgress=" + startProgress.ToString("0.00") +
                        " Accepted=" + accepted);
                }
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
        // Native clips whose animation data embeds a voice type (scanned from
        // Native animation_clips.tpac): taunt_afraid/scared=Fear,
        // taunt_dissapointed*=Debacle, taunt_invite_mad/taunt_rage=Yell,
        // taunt_laugh=Focus, taunt_respect=Grunt, cheer_1/taunt_cheer_1=victory.
        // taunt_bow (deep_bow) has only a foley event; kept for salute parity.
        private const float VoicedNativeClipStartProgress = 0.5f;

        private static readonly string[] VoicedNativeClipActionIds =
        {
            "act_taunt_01", "act_taunt_02", "act_taunt_04", "act_taunt_05",
            "act_taunt_06", "act_taunt_07", "act_taunt_14", "act_taunt_15",
            "act_taunt_18", "act_taunt_20", "act_taunt_21", "act_cheer_1",
            "act_taunt_cheer_1"
        };

        private static HashSet<int> _voicedNativeClipIndices;
        private static Mission _voicedNativeClipMission;

        // Resolved once per Mission (action indices are Mission-scoped) on the
        // Mission thread; afterwards a hash lookup per play.
        private static bool IsVoicedNativeClip(ActionIndexCache action)
        {
            Mission mission = Mission.Current;
            HashSet<int> indices = _voicedNativeClipIndices;
            if (indices == null || !ReferenceEquals(_voicedNativeClipMission, mission))
            {
                indices = new HashSet<int>();
                foreach (string actionId in VoicedNativeClipActionIds)
                {
                    int index = ActionIndexCache.Create(actionId).Index;
                    if (index >= 0)
                    {
                        indices.Add(index);
                    }
                }
                _voicedNativeClipIndices = indices;
                _voicedNativeClipMission = mission;
            }
            return indices.Contains(action.Index);
        }

        private static void ResetVoicedNativeClipCache()
        {
            _voicedNativeClipIndices = null;
            _voicedNativeClipMission = null;
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