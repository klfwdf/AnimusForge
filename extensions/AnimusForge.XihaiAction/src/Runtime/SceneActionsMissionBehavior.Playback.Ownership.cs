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
        private void RegisterOwnedPlayback(
            SessionAgentHandle handle,
            string logicalIntentKey,
            SelectedAction selectedAction,
            ActionIndexCache action,
            int channel,
            double startedAtMissionTime,
            Guid ownerToken)
        {
            Agent agent = handle?.Agent;
            if (agent == null)
            {
                return;
            }
            _ownedLoops[agent.Index] = new OwnedLoopState
            {
                Handle = handle,
                LogicalIntentKey = logicalIntentKey,
                SelectedAction = selectedAction,
                Action = action,
                Channel = channel,
                StartedAtMissionTime = startedAtMissionTime,
                OwnerToken = ownerToken
            };
        }
        private void ProgressOwnedLoops(double now)
        {
            foreach (KeyValuePair<int, OwnedLoopState> pair in _ownedLoops.ToArray())
            {
                OwnedLoopState loop = pair.Value;
                if (!TryValidateAgent(
                    loop.Handle,
                    out Agent agent,
                    out ExecutionResultCode validationFailure,
                    true))
                {
                    if (agent != null &&
                        validationFailure == ExecutionResultCode.EngineCriticalState)
                    {
                        ReleaseOwnedLoopForAgent(agent, true);
                    }
                    else
                    {
                        _ownedLoops.Remove(pair.Key);
                    }
                    continue;
                }
                bool currentMatches;
                bool forceRelease;
                try
                {
                    currentMatches =
                        agent.GetCurrentAction(loop.Channel) == loop.Action;
                    forceRelease = agent.IsInBeingStruckAction ||
                                   agent.MovementInputVector.LengthSquared > 0.0001f;
                }
                catch
                {
                    currentMatches = false;
                    forceRelease = true;
                }
                if (!currentMatches)
                {
                    if (now - loop.StartedAtMissionTime < 0.35d)
                    {
                        continue;
                    }
                    ReleaseOwnedLoopForAgent(agent, false);
                    continue;
                }
                if (forceRelease)
                {
                    ReleaseOwnedLoopForAgent(agent, true);
                }
            }
        }
        private bool ReleaseOwnedLoopForAgent(Agent agent, bool releaseChannel)
        {
            if (agent == null ||
                !_ownedLoops.TryGetValue(agent.Index, out OwnedLoopState loop) ||
                !ReferenceEquals(loop.Handle.Agent, agent))
            {
                return false;
            }
            bool released = true;
            if (releaseChannel)
            {
                released = TryReleaseOwnedChannel(
                    agent,
                    loop.Channel,
                    true,
                    loop.Action);
            }
            _ownedLoops.Remove(agent.Index);
            return released;
        }
        private bool TryPrepareForPlayback(
            Agent agent,
            bool clearOwnedState,
            out string reason)
        {
            reason = null;
            if (agent == null)
            {
                reason = "Agent is missing while preparing the next action.";
                return false;
            }

            if (_ownedLoops.TryGetValue(agent.Index, out OwnedLoopState loop) &&
                ReferenceEquals(loop.Handle.Agent, agent))
            {
                if (!TryReleaseOwnedChannelForReplacement(
                    agent,
                    loop.Channel,
                    loop.Action))
                {
                    reason = "Previous SceneActions playback could not be released safely.";
                    return false;
                }
                _ownedLoops.Remove(agent.Index);
            }

            if (!clearOwnedState ||
                !_ownedStates.TryGetValue(agent.Index, out OwnedActionState state) ||
                !ReferenceEquals(state.Handle.Agent, agent))
            {
                return true;
            }

            if (state.ActivePlan != null)
            {
                FinishPlan(
                    state.ActivePlan,
                    ExecutionResultCode.Interrupted,
                    "Owned state was replaced by a new action.");
            }
            if (!TryReleaseOwnedChannelForReplacement(
                agent,
                state.Channel,
                state.EnterAction,
                state.HoldAction,
                state.ExitAction))
            {
                reason = "Previous SceneActions state channel could not be released safely.";
                return false;
            }
            _ownedStates.Remove(agent.Index);
            return true;
        }
        private static bool TryReleaseOwnedChannelForReplacement(
            Agent agent,
            int channel,
            params ActionIndexCache[] ownedActions)
        {
            ActionIndexCache current;
            try
            {
                current = agent.GetCurrentAction(channel);
            }
            catch (Exception ex)
            {
                SceneActionsLog.Warning(
                    "CHANNEL",
                    "Replacement probe failed for channel=" + channel +
                    ": " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
            if (current == ActionIndexCache.act_none)
            {
                return true;
            }
            bool isStillOwned = (ownedActions ?? Array.Empty<ActionIndexCache>()).Any(
                action => action != ActionIndexCache.act_none && current == action);
            if (!isStillOwned)
            {
                return true;
            }
            return TryReleaseOwnedChannel(agent, channel, true, ownedActions);
        }
    }
}
