using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.SceneActions.Core;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    internal sealed partial class SceneActionsMissionBehavior
    {
        private void ExecuteStatefulEnter(PlannedTarget plan, Agent agent, double now)
        {
            SelectedAction selected = plan.SelectedAction;
            if (_ownedStates.TryGetValue(agent.Index, out OwnedActionState existing))
            {
                if (!ReferenceEquals(existing.Handle.Agent, agent))
                {
                    if (existing.ActivePlan != null)
                    {
                        FinishPlan(
                            existing.ActivePlan,
                            ExecutionResultCode.MissionChanged,
                            "Agent index was reused.");
                    }
                    _ownedStates.Remove(agent.Index);
                }
                else if (string.Equals(
                    existing.Definition.Definition.StateTag,
                    selected.Definition.StateTag,
                    StringComparison.Ordinal))
                {
                    if (existing.Phase == OwnedStatePhase.Holding)
                    {
                        FinishPlan(
                            plan,
                            ExecutionResultCode.HoldingObserved,
                            "State is already owned and holding; Enter was not replayed.");
                    }
                    else
                    {
                        FinishPlan(
                            plan,
                            ExecutionResultCode.AcceptedByEngine,
                            "The same state transition is already in progress.");
                    }
                    return;
                }
                else
                {
                    if (existing.ActivePlan != null)
                    {
                        FinishPlan(
                            existing.ActivePlan,
                            ExecutionResultCode.Interrupted,
                            "Owned state was replaced.");
                    }
                    _ownedStates.Remove(agent.Index);
                }
            }

            if (!TryResolveStateChain(
                selected,
                out ActionIndexCache enter,
                out ActionIndexCache hold,
                out ActionIndexCache exit,
                out ExecutionResultCode failure,
                out string reason))
            {
                FinishPlan(plan, failure, reason);
                return;
            }
            if (!TrySetAction(agent, selected.Variant, enter, out string setReason))
            {
                FinishPlan(plan, ExecutionResultCode.SetActionRejected, setReason);
                return;
            }

            SetCooldown(agent, selected, now);
            _ownedStates[agent.Index] = new OwnedActionState
            {
                Handle = plan.Handle,
                Definition = selected,
                Phase = OwnedStatePhase.Entering,
                StateGeneration = 1,
                EnterAction = enter,
                HoldAction = hold,
                ExitAction = exit,
                Channel = selected.Variant.Channel,
                AdditionalFlags = selected.Variant.EnforceAll
                    ? AnimFlags.anf_enforce_all
                    : 0,
                TransitionStartedAt = now,
                ActivePlan = plan
            };
            SceneActionsLog.Info(
                "STATE",
                FormatPlan(plan) + " Transition=EnterRequested Result=AcceptedByEngine");
        }
        private void ExecuteExitOwnedState(PlannedTarget plan, Agent agent, double now)
        {
            if (!_ownedStates.TryGetValue(agent.Index, out OwnedActionState state) ||
                !ReferenceEquals(state.Handle.Agent, agent) ||
                !plan.Intent.AcceptedStateTags.Contains(
                    state.Definition.Definition.StateTag,
                    StringComparer.Ordinal))
            {
                FinishPlan(
                    plan,
                    ExecutionResultCode.AlreadyStanding,
                    "No matching SceneActions-owned state exists; Exit action was not played.");
                return;
            }
            if (state.Phase == OwnedStatePhase.Exiting)
            {
                FinishPlan(
                    plan,
                    ExecutionResultCode.AcceptedByEngine,
                    "Exit transition is already in progress; it was not replayed.");
                return;
            }
            if (state.ActivePlan != null)
            {
                FinishPlan(
                    state.ActivePlan,
                    ExecutionResultCode.Interrupted,
                    "Enter transition was superseded by ExitOwnedState.");
            }
            if (!TrySetAction(
                agent,
                state.Definition.Variant,
                state.ExitAction,
                state.Channel,
                state.AdditionalFlags,
                out string setReason))
            {
                _ownedStates.Remove(agent.Index);
                FinishPlan(plan, ExecutionResultCode.SetActionRejected, setReason);
                return;
            }
            state.Phase = OwnedStatePhase.Exiting;
            state.StateGeneration++;
            state.TransitionStartedAt = now;
            state.ExitObserved = false;
            state.ActivePlan = plan;
            SceneActionsLog.Info(
                "STATE",
                FormatPlan(plan) + " Transition=ExitRequested Result=AcceptedByEngine");
        }
        private bool TryResolveStateChain(
            SelectedAction selected,
            out ActionIndexCache enter,
            out ActionIndexCache hold,
            out ActionIndexCache exit,
            out ExecutionResultCode failure,
            out string reason)
        {
            enter = hold = exit = ActionIndexCache.act_none;
            if (!_providerSession.TryResolve(
                    selected.Definition.ProviderId,
                    selected.Variant.EnterActionId,
                    out enter,
                    out failure,
                    out reason) ||
                !_providerSession.TryResolve(
                    selected.Definition.ProviderId,
                    selected.Variant.HoldActionId,
                    out hold,
                    out failure,
                    out reason) ||
                !_providerSession.TryResolve(
                    selected.Definition.ProviderId,
                    selected.Variant.ExitActionId,
                    out exit,
                    out failure,
                    out reason))
            {
                return false;
            }
            failure = ExecutionResultCode.Queued;
            reason = null;
            return true;
        }
        private void ProgressOwnedStates(double now)
        {
            foreach (KeyValuePair<int, OwnedActionState> entry in _ownedStates.ToArray())
            {
                OwnedActionState state = entry.Value;
                if (!TryValidateAgent(
                    state.Handle,
                    out Agent agent,
                    out ExecutionResultCode failure,
                    state.Channel == 0))
                {
                    FailAndReleaseState(entry.Key, state, failure, "Owned agent became invalid.");
                    continue;
                }

                try
                {
                    ActionIndexCache current = agent.GetCurrentAction(state.Channel);
                    float progress = agent.GetCurrentActionProgress(state.Channel);
                    if (state.Phase == OwnedStatePhase.Entering)
                    {
                        if (current == state.HoldAction)
                        {
                            state.Phase = OwnedStatePhase.Holding;
                            CompleteStateTransition(
                                state,
                                ExecutionResultCode.HoldingObserved,
                                "Hold action was observed on the configured channel.");
                            continue;
                        }
                        if (current == state.EnterAction)
                        {
                            state.EnterObserved = true;
                            if (progress >= 0.94f && !state.HoldRequested)
                            {
                                if (TrySetAction(
                                    agent,
                                    state.Definition.Variant,
                                    state.HoldAction,
                                    state.Channel,
                                    state.AdditionalFlags,
                                    out string setReason))
                                {
                                    state.HoldRequested = true;
                                    SceneActionsLog.Info(
                                        "STATE",
                                        FormatPlan(state.ActivePlan) +
                                        " Transition=HoldRequested Result=AcceptedByEngine");
                                }
                                else
                                {
                                    FailAndReleaseState(
                                        entry.Key,
                                        state,
                                        ExecutionResultCode.SetActionRejected,
                                        setReason);
                                }
                            }
                        }
                        else if (state.EnterObserved &&
                                 now - state.TransitionStartedAt > 0.3d &&
                                 !state.HoldRequested)
                        {
                            FailAndReleaseState(
                                entry.Key,
                                state,
                                ExecutionResultCode.Interrupted,
                                "Enter action was replaced before Hold was requested.");
                            continue;
                        }
                        if (now - state.TransitionStartedAt >
                            state.Definition.Variant.EnterSafetyTimeoutSeconds)
                        {
                            FailAndReleaseState(
                                entry.Key,
                                state,
                                ExecutionResultCode.Interrupted,
                                "Enter/Hold observation safety timeout elapsed.");
                        }
                    }
                    else if (state.Phase == OwnedStatePhase.Holding)
                    {
                        if (current != state.HoldAction)
                        {
                            _ownedStates.Remove(entry.Key);
                            SceneActionsLog.Info(
                                "STATE",
                                "SessionGeneration=" + _sessionGeneration +
                                " Agent=" + state.Handle.AgentIndex +
                                " Transition=OwnershipReleased Result=Interrupted");
                        }
                    }
                    else
                    {
                        if (current == state.ExitAction)
                        {
                            state.ExitObserved = true;
                            if (progress >= 0.98f)
                            {
                                CompleteExit(entry.Key, state, "Exit progress reached completion.");
                                continue;
                            }
                        }
                        else if (state.ExitObserved)
                        {
                            CompleteExit(entry.Key, state, "Exit action left the channel after observation.");
                            continue;
                        }
                        if (now - state.TransitionStartedAt >
                            state.Definition.Variant.ExitSafetyTimeoutSeconds)
                        {
                            FailAndReleaseState(
                                entry.Key,
                                state,
                                ExecutionResultCode.Interrupted,
                                "Exit observation safety timeout elapsed; ownership released.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    FailAndReleaseState(
                        entry.Key,
                        state,
                        ExecutionResultCode.ExecutorException,
                        ex.GetType().Name + ": " + ex.Message);
                }
            }
        }
        private void CompleteStateTransition(
            OwnedActionState state,
            ExecutionResultCode result,
            string reason)
        {
            PlannedTarget plan = state.ActivePlan;
            state.ActivePlan = null;
            if (plan != null)
            {
                FinishPlan(plan, result, reason);
            }
        }
        private void CompleteExit(int key, OwnedActionState state, string reason)
        {
            _ownedStates.Remove(key);
            PlannedTarget plan = state.ActivePlan;
            state.ActivePlan = null;
            if (plan != null)
            {
                FinishPlan(plan, ExecutionResultCode.CompletedObserved, reason);
            }
        }
        private void FailAndReleaseState(
            int key,
            OwnedActionState state,
            ExecutionResultCode result,
            string reason)
        {
            _ownedStates.Remove(key);
            PlannedTarget plan = state.ActivePlan;
            state.ActivePlan = null;
            if (plan != null)
            {
                FinishPlan(plan, result, reason);
            }
        }
        private void ReleaseOwnedStateForAgent(
            Agent agent,
            ExecutionResultCode result,
            string reason)
        {
            if (agent == null ||
                !_ownedStates.TryGetValue(agent.Index, out OwnedActionState state) ||
                !ReferenceEquals(state.Handle.Agent, agent))
            {
                return;
            }
            FailAndReleaseState(agent.Index, state, result, reason);
        }
    }
}