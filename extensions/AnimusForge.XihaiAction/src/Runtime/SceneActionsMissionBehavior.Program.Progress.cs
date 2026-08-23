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
        private bool TryReleaseProgramKneel(
            ProgramTargetExecution execution,
            Agent agent)
        {
            ProgramKneelRuntime kneel = execution.PersistentKneel;
            if (kneel == null)
            {
                return true;
            }
            if (!TryReleaseOwnedChannel(
                agent,
                kneel.Channel,
                kneel.AcceptedByEngine,
                kneel.EnterAction,
                kneel.HoldAction,
                kneel.ExitAction))
            {
                return false;
            }
            execution.PersistentKneel = null;
            return true;
        }
        private bool TryReleaseProgramOwnedChannels(
            ProgramTargetExecution execution,
            Agent agent)
        {
            ProgramPlaybackRuntime playback = execution.ActiveStep?.Playback;
            if (playback != null &&
                !TryReleaseOwnedChannel(
                    agent,
                    playback.Channel,
                    playback.AcceptedByEngine,
                    playback.Action))
            {
                return false;
            }
            return TryReleaseProgramKneel(execution, agent);
        }
        private static bool TryReleaseOwnedChannel(
            Agent agent,
            int channel,
            bool ownershipAccepted,
            params ActionIndexCache[] ownedActions)
        {
            return SceneActionChannelOwner.TryReleaseOwnedChannelWithContext(
                agent,
                channel,
                ownershipAccepted,
                "SceneActionsMissionBehavior.TryReleaseOwnedChannel",
                ownedActions);
        }
        private void ProgressActionPrograms(double now)
        {
            foreach (ProgramTargetExecution execution in
                     _programExecutions.Values.ToArray())
            {
                if (execution.State != ProgramExecutionState.Running ||
                    execution.ActiveStep == null)
                {
                    continue;
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
                        "Agent became invalid while an action program was running.",
                        now);
                    continue;
                }

                if (execution.PersistentKneel != null &&
                    !execution.PersistentKneel.Holding &&
                    !ProgressProgramKneel(execution, agent, now))
                {
                    continue;
                }
                if (!_programExecutions.ContainsKey(execution.TargetKey) ||
                    execution.State != ProgramExecutionState.Running)
                {
                    continue;
                }

                ProgramPlaybackRuntime playback = execution.ActiveStep.Playback;
                if (playback != null &&
                    !playback.Completed &&
                    !ProgressProgramPlayback(execution, playback, agent, now))
                {
                    continue;
                }
                if (!_programExecutions.ContainsKey(execution.TargetKey) ||
                    execution.State != ProgramExecutionState.Running)
                {
                    continue;
                }

                bool isLast = execution.CurrentStepIndex == execution.Steps.Count - 1;
                if (execution.ActiveStep.IsDualChannel)
                {
                    if (execution.PersistentKneel?.Holding == true &&
                        execution.ActiveStep.Playback?.Completed == true)
                    {
                        if (isLast)
                        {
                            TransferProgramKneelOwnership(execution, agent);
                        }
                        CompleteProgramStep(
                            execution,
                            ExecutionResultCode.CompletedObserved,
                            now);
                    }
                    continue;
                }

                if (execution.PersistentKneel?.Holding == true && playback == null)
                {
                    if (isLast)
                    {
                        TransferProgramKneelOwnership(execution, agent);
                        CompleteProgramStep(
                            execution,
                            ExecutionResultCode.HoldingObserved,
                            now);
                    }
                    else if (now - execution.PersistentKneel.HoldingSinceMissionTime >=
                             SceneActionsRuntimeHost.Settings.IntermediateKneelHoldSeconds)
                    {
                        CompleteProgramStep(
                            execution,
                            ExecutionResultCode.HoldingObserved,
                            now);
                    }
                    continue;
                }

                if (playback?.Kind == ProgramPlaybackKind.Dance && playback.Observed)
                {
                    if (isLast)
                    {
                        TransferProgramDanceOwnership(execution, playback, agent);
                        CompleteProgramStep(
                            execution,
                            ExecutionResultCode.HoldingObserved,
                            now);
                    }
                    else if (now - playback.ObservedAtMissionTime >=
                             SceneActionsRuntimeHost.Settings.IntermediateDanceSeconds)
                    {
                        if (!TryReleaseOwnedChannel(
                            agent,
                            playback.Channel,
                            playback.AcceptedByEngine,
                            playback.Action))
                        {
                            FailProgramTarget(
                                execution,
                                ExecutionResultCode.Interrupted,
                                "Intermediate dance channel could not be released safely.",
                                now);
                        }
                        else
                        {
                            playback.AcceptedByEngine = false;
                            playback.Completed = true;
                            CompleteProgramStep(
                                execution,
                                ExecutionResultCode.CompletedObserved,
                                now);
                        }
                    }
                    continue;
                }

                if (playback?.Completed == true)
                {
                    if (isLast &&
                        playback.Kind == ProgramPlaybackKind.OneShot &&
                        playback.AcceptedByEngine)
                    {
                        RegisterOwnedPlayback(
                            execution.Handle,
                            playback.LogicalIntentKey,
                            playback.SelectedAction,
                            playback.Action,
                            playback.Channel,
                            playback.StartedAtMissionTime,
                            Guid.Empty);
                        playback.AcceptedByEngine = false;
                    }
                    CompleteProgramStep(
                        execution,
                        ExecutionResultCode.CompletedObserved,
                        now);
                }
            }
        }
        private bool ProgressProgramKneel(
            ProgramTargetExecution execution,
            Agent agent,
            double now)
        {
            ProgramKneelRuntime kneel = execution.PersistentKneel;
            try
            {
                ActionIndexCache current = agent.GetCurrentAction(kneel.Channel);
                float progress = agent.GetCurrentActionProgress(kneel.Channel);
                if (current == kneel.HoldAction)
                {
                    kneel.Holding = true;
                    kneel.OwnedAction = kneel.HoldAction;
                    kneel.HoldingSinceMissionTime = now;
                    return true;
                }
                if (current == kneel.EnterAction)
                {
                    kneel.EnterObserved = true;
                    kneel.OwnedAction = kneel.EnterAction;
                    if (progress >= 0.94f && !kneel.HoldRequested)
                    {
                        if (!TrySetAction(
                            agent,
                            kneel.SelectedAction.Variant,
                            kneel.HoldAction,
                            kneel.Channel,
                            kneel.AdditionalFlags,
                            out string holdReason))
                        {
                            return HandleProgramPlaybackFailure(
                                execution,
                                agent,
                                now,
                                holdReason);
                        }
                        kneel.HoldRequested = true;
                        kneel.OwnedAction = kneel.HoldAction;
                    }
                }
                else if (kneel.EnterObserved &&
                         now - kneel.StartedAtMissionTime > 0.3d &&
                         !kneel.HoldRequested)
                {
                    return HandleProgramPlaybackFailure(
                        execution,
                        agent,
                        now,
                        "Kneel Enter was interrupted before Hold.");
                }
                if (now - kneel.StartedAtMissionTime >
                    SceneActionsRuntimeHost.Settings.StepTimeoutSeconds)
                {
                    return HandleProgramPlaybackFailure(
                        execution,
                        agent,
                        now,
                        "Kneel Enter/Hold observation timed out.");
                }
                return true;
            }
            catch (Exception ex)
            {
                return HandleProgramPlaybackFailure(
                    execution,
                    agent,
                    now,
                    ex.GetType().Name + ": " + ex.Message);
            }
        }
        private bool ProgressProgramPlayback(
            ProgramTargetExecution execution,
            ProgramPlaybackRuntime playback,
            Agent agent,
            double now)
        {
            if (playback.Completed || !playback.AcceptedByEngine)
            {
                return true;
            }
            try
            {
                ActionIndexCache current = agent.GetCurrentAction(playback.Channel);
                float progress = agent.GetCurrentActionProgress(playback.Channel);
                if (current == playback.Action)
                {
                    if (!playback.Observed)
                    {
                        playback.Observed = true;
                        playback.ObservedAtMissionTime = now;
                    }
                    if (playback.Kind != ProgramPlaybackKind.Dance && progress >= 0.98f)
                    {
                        playback.Completed = true;
                    }
                }
                else if (playback.Observed)
                {
                    if (playback.Kind == ProgramPlaybackKind.Dance)
                    {
                        return HandleProgramPlaybackFailure(
                            execution,
                            agent,
                            now,
                            "Dance was interrupted before its required hold duration.");
                    }
                    playback.Completed = true;
                }
                if (!playback.Completed &&
                    now - playback.StartedAtMissionTime >
                    SceneActionsRuntimeHost.Settings.StepTimeoutSeconds)
                {
                    return HandleProgramPlaybackFailure(
                        execution,
                        agent,
                        now,
                        "One program action was not observed to complete before timeout.");
                }
                return true;
            }
            catch (Exception ex)
            {
                return HandleProgramPlaybackFailure(
                    execution,
                    agent,
                    now,
                    ex.GetType().Name + ": " + ex.Message);
            }
        }
        private bool HandleProgramPlaybackFailure(
            ProgramTargetExecution execution,
            Agent agent,
            double now,
            string reason)
        {
            if (execution.ActiveStep?.IsDualChannel == true &&
                !execution.UsingSequentialFallback)
            {
                BeginSequentialFallback(execution, agent, now, reason);
                return false;
            }
            FailProgramTarget(
                execution,
                ExecutionResultCode.Interrupted,
                reason,
                now);
            return false;
        }
    }
}
