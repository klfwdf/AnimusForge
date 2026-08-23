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
        private void ExecuteProgramStep(PlannedTarget plan, double now)
        {
            ProgramTargetExecution execution = plan.ProgramExecution;
            if (execution == null ||
                !_programExecutions.TryGetValue(
                    execution.TargetKey,
                    out ProgramTargetExecution current) ||
                !ReferenceEquals(current, execution) ||
                execution.CurrentStepIndex != plan.ProgramStepIndex ||
                execution.State != ProgramExecutionState.Scheduled)
            {
                return;
            }
            if (now > plan.ExpiresAtMissionTime)
            {
                FailProgramTarget(
                    execution,
                    ExecutionResultCode.Expired,
                    "Program step exceeded its scheduling TTL.",
                    now);
                return;
            }

            bool allowOwnedChannelZero = execution.PersistentKneel?.Channel == 0;
            if (!TryValidateAgent(
                execution.Handle,
                out Agent agent,
                out ExecutionResultCode failure,
                allowOwnedChannelZero))
            {
                FailProgramTarget(
                    execution,
                    failure,
                    "Agent validation failed before a program step.",
                    now);
                return;
            }
            FrozenProgramStep step = execution.Steps[execution.CurrentStepIndex];
            bool stepContainsKneel = step.Actions.Any(action => string.Equals(
                action.Intent.Key,
                SceneActionFrameworkV4.Kneel,
                StringComparison.Ordinal));
            bool stepExitsState = step.Actions.Count == 1 &&
                                  step.Actions[0].Intent.Kind == IntentKind.ExitOwnedState;
            if (!TryPrepareForPlayback(
                agent,
                !stepContainsKneel && !stepExitsState,
                out string prepareReason))
            {
                FailProgramTarget(
                    execution,
                    ExecutionResultCode.PreviousActionNotReleased,
                    prepareReason,
                    now);
                return;
            }
            if (execution.PersistentKneel != null &&
                !stepContainsKneel &&
                !stepExitsState)
            {
                if (!TryReleaseProgramKneel(execution, agent))
                {
                    FailProgramTarget(
                        execution,
                        ExecutionResultCode.Interrupted,
                        "The program-owned kneel channel could not be released safely.",
                        now);
                    return;
                }
            }

            execution.ActivePlan = plan;
            execution.State = ProgramExecutionState.Running;
            execution.ActiveStep = new ProgramActiveStep
            {
                StartedAtMissionTime = now,
                IsDualChannel = step.Actions.Count == 2
            };
            if (step.Actions.Count == 2)
            {
                StartDualChannelStep(execution, step, agent, now);
                return;
            }

            FrozenProgramAction action = step.Actions[0];
            if (action.Intent.Kind == IntentKind.ExitOwnedState)
            {
                StartProgramExit(execution, action, agent, now);
                return;
            }
            if (action.SelectedAction.Definition.Mode == ActionMode.Stateful)
            {
                StartProgramKneel(execution, action, agent, now, false);
                return;
            }
            StartProgramPlayback(execution, action, agent, now);
        }
        private void StartDualChannelStep(
            ProgramTargetExecution execution,
            FrozenProgramStep step,
            Agent agent,
            double now)
        {
            FrozenProgramAction kneel = step.Actions.SingleOrDefault(action => string.Equals(
                action.Intent.Key,
                SceneActionFrameworkV4.Kneel,
                StringComparison.Ordinal));
            FrozenProgramAction overlay = step.Actions.SingleOrDefault(action =>
                !string.Equals(
                    action.Intent.Key,
                    SceneActionFrameworkV4.Kneel,
                    StringComparison.Ordinal));
            if (!SceneActionsRuntimeHost.Settings.DualChannelExperimentalEnabled ||
                kneel == null || overlay == null ||
                !SceneActionFrameworkV4.CanOverlayKneel(overlay.Intent.Key))
            {
                BeginSequentialFallback(
                    execution,
                    agent,
                    now,
                    "Controlled dual-channel playback is unavailable for this step.");
                return;
            }

            if (execution.PersistentKneel == null)
            {
                if (!TryResolveStateChain(
                    kneel.SelectedAction,
                    out ActionIndexCache enter,
                    out ActionIndexCache hold,
                    out ActionIndexCache exit,
                    out ExecutionResultCode chainFailure,
                    out string chainReason))
                {
                    FailProgramTarget(execution, chainFailure, chainReason, now);
                    return;
                }
                execution.PersistentKneel = new ProgramKneelRuntime
                {
                    SelectedAction = kneel.SelectedAction,
                    EnterAction = enter,
                    HoldAction = hold,
                    ExitAction = exit,
                    Channel = 0,
                    AdditionalFlags = AnimFlags.anf_enforce_lowerbody,
                    StartedAtMissionTime = now
                };
            }

            ProgramKneelRuntime kneelRuntime = execution.PersistentKneel;
            if (!kneelRuntime.Holding && !kneelRuntime.AcceptedByEngine)
            {
                if (!TrySetAction(
                    agent,
                    kneelRuntime.SelectedAction.Variant,
                    kneelRuntime.EnterAction,
                    kneelRuntime.Channel,
                    kneelRuntime.AdditionalFlags,
                    out string kneelReason))
                {
                    BeginSequentialFallback(execution, agent, now, kneelReason);
                    return;
                }
                kneelRuntime.AcceptedByEngine = true;
                kneelRuntime.OwnedAction = kneelRuntime.EnterAction;
                SetCooldown(agent, kneelRuntime.SelectedAction, now);
            }

            if (!_providerSession.TryResolve(
                overlay.SelectedAction.Definition.ProviderId,
                overlay.FrozenActionId,
                out ActionIndexCache overlayAction,
                out ExecutionResultCode overlayFailure,
                out string overlayReason))
            {
                BeginSequentialFallback(execution, agent, now, overlayReason);
                return;
            }
            if (!TrySetAction(
                agent,
                overlay.SelectedAction.Variant,
                overlayAction,
                1,
                0,
                out string setReason))
            {
                BeginSequentialFallback(execution, agent, now, setReason);
                return;
            }
            execution.ActiveStep.Playback = new ProgramPlaybackRuntime
            {
                LogicalIntentKey = overlay.Intent.Key,
                SelectedAction = overlay.SelectedAction,
                Action = overlayAction,
                Channel = 1,
                Kind = ProgramPlaybackKind.OneShot,
                StartedAtMissionTime = now,
                AcceptedByEngine = true
            };
            SetCooldown(agent, overlay.SelectedAction, now);
            SceneActionsLog.Info(
                "PROGRAM",
                FormatPlan(execution.ActivePlan) +
                " Step=" + execution.CurrentStepIndex +
                " Mode=DualChannel Result=AcceptedByEngine");
        }
        private void StartProgramKneel(
            ProgramTargetExecution execution,
            FrozenProgramAction action,
            Agent agent,
            double now,
            bool lowerBody)
        {
            if (!TryResolveStateChain(
                action.SelectedAction,
                out ActionIndexCache enter,
                out ActionIndexCache hold,
                out ActionIndexCache exit,
                out ExecutionResultCode failure,
                out string reason))
            {
                FailProgramTarget(execution, failure, reason, now);
                return;
            }
            ProgramKneelRuntime kneel = new ProgramKneelRuntime
            {
                SelectedAction = action.SelectedAction,
                EnterAction = enter,
                HoldAction = hold,
                ExitAction = exit,
                Channel = lowerBody ? 0 : action.SelectedAction.Variant.Channel,
                AdditionalFlags = lowerBody ? AnimFlags.anf_enforce_lowerbody : 0,
                StartedAtMissionTime = now
            };
            execution.PersistentKneel = kneel;
            if (!TrySetAction(
                agent,
                kneel.SelectedAction.Variant,
                kneel.EnterAction,
                kneel.Channel,
                kneel.AdditionalFlags,
                out string setReason))
            {
                FailProgramTarget(
                    execution,
                    ExecutionResultCode.SetActionRejected,
                    setReason,
                    now);
                return;
            }
            kneel.AcceptedByEngine = true;
            kneel.OwnedAction = kneel.EnterAction;
            SetCooldown(agent, kneel.SelectedAction, now);
        }
        private void StartProgramPlayback(
            ProgramTargetExecution execution,
            FrozenProgramAction action,
            Agent agent,
            double now)
        {
            if (!_providerSession.TryResolve(
                action.SelectedAction.Definition.ProviderId,
                action.FrozenActionId,
                out ActionIndexCache actionIndex,
                out ExecutionResultCode failure,
                out string reason))
            {
                FailProgramTarget(execution, failure, reason, now);
                return;
            }
            if (!TrySetAction(
                agent,
                action.SelectedAction.Variant,
                actionIndex,
                out string setReason))
            {
                FailProgramTarget(
                    execution,
                    ExecutionResultCode.SetActionRejected,
                    setReason,
                    now);
                return;
            }
            execution.ActiveStep.Playback = new ProgramPlaybackRuntime
            {
                LogicalIntentKey = action.Intent.Key,
                SelectedAction = action.SelectedAction,
                Action = actionIndex,
                Channel = action.SelectedAction.Variant.Channel,
                Kind = action.SelectedAction.Definition.Mode == ActionMode.Looping
                    ? ProgramPlaybackKind.Dance
                    : ProgramPlaybackKind.OneShot,
                StartedAtMissionTime = now,
                AcceptedByEngine = true
            };
            SetCooldown(agent, action.SelectedAction, now);
        }
        private void StartProgramExit(
            ProgramTargetExecution execution,
            FrozenProgramAction action,
            Agent agent,
            double now)
        {
            ProgramKneelRuntime programKneel = execution.PersistentKneel;
            OwnedActionState owned = null;
            if (programKneel == null)
            {
                _ownedStates.TryGetValue(agent.Index, out owned);
                if (owned == null ||
                    !ReferenceEquals(owned.Handle.Agent, agent) ||
                    !action.Intent.AcceptedStateTags.Contains(
                        owned.Definition.Definition.StateTag,
                        StringComparer.Ordinal))
                {
                    execution.ActiveStep.Playback = new ProgramPlaybackRuntime
                    {
                        LogicalIntentKey = action.Intent.Key,
                        Kind = ProgramPlaybackKind.Exit,
                        Completed = true,
                        StartedAtMissionTime = now
                    };
                    return;
                }
            }

            SelectedAction selected = programKneel?.SelectedAction ?? owned.Definition;
            ActionIndexCache exitAction = programKneel?.ExitAction ?? owned.ExitAction;
            int channel = programKneel?.Channel ?? owned.Channel;
            AnimFlags flags = programKneel?.AdditionalFlags ?? owned.AdditionalFlags;
            if (!TrySetAction(
                agent,
                selected.Variant,
                exitAction,
                channel,
                flags,
                out string reason))
            {
                execution.PersistentKneel = null;
                _ownedStates.Remove(agent.Index);
                FailProgramTarget(
                    execution,
                    ExecutionResultCode.SetActionRejected,
                    reason,
                    now);
                return;
            }
            execution.PersistentKneel = null;
            _ownedStates.Remove(agent.Index);
            execution.ActiveStep.Playback = new ProgramPlaybackRuntime
            {
                LogicalIntentKey = action.Intent.Key,
                SelectedAction = selected,
                Action = exitAction,
                Channel = channel,
                AdditionalFlags = flags,
                Kind = ProgramPlaybackKind.Exit,
                StartedAtMissionTime = now,
                AcceptedByEngine = true
            };
        }
        private void BeginSequentialFallback(
            ProgramTargetExecution execution,
            Agent agent,
            double now,
            string reason)
        {
            if (execution.UsingSequentialFallback)
            {
                FailProgramTarget(
                    execution,
                    ExecutionResultCode.SetActionRejected,
                    "Sequential fallback also failed: " + reason,
                    now);
                return;
            }
            if (!TryReleaseProgramOwnedChannels(execution, agent))
            {
                FailProgramTarget(
                    execution,
                    ExecutionResultCode.Cancelled,
                    "Dual-channel fallback could not release module-owned channels safely.",
                    now);
                return;
            }

            int fallbackIndex = 0;
            for (int index = 0; index < execution.CurrentStepIndex; index++)
            {
                fallbackIndex += execution.Steps[index].Actions.Count;
            }
            execution.UsingSequentialFallback = true;
            execution.Steps = execution.SequentialFallbackSteps;
            execution.CurrentStepIndex = fallbackIndex;
            execution.PersistentKneel = null;
            execution.ActiveStep = null;
            execution.State = ProgramExecutionState.Waiting;
            DisableBatchBarriers(execution.RequestId, now);
            SceneActionsLog.Warning(
                "PROGRAM",
                "RequestId=" + execution.RequestId.ToString("N") +
                " Agent=" + execution.Handle.AgentIndex +
                " Result=SequentialFallback Reason=" + reason);
            ScheduleProgramStep(execution, now, 0d);
        }
    }
}