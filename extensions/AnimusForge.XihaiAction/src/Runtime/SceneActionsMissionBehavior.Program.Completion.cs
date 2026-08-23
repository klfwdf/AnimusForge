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
        private void CompleteProgramTarget(
            ProgramTargetExecution execution,
            ExecutionResultCode result,
            string reason,
            double now,
            bool recheckBarrier)
        {
            if (execution == null ||
                !_programExecutions.Remove(execution.TargetKey))
            {
                return;
            }
            execution.State = ProgramExecutionState.Terminal;
            execution.ActiveStep = null;
            ProgramBatchExecution batch = null;
            if (_programBatches.TryGetValue(
                execution.RequestId,
                out batch))
            {
                batch.Targets.Remove(execution.TargetKey);
            }
            FinishPlan(execution.ActivePlan, result, reason);
            if (batch != null)
            {
                if (batch.Targets.Count == 0)
                {
                    _programBatches.Remove(batch.RequestId);
                }
                else if (recheckBarrier && batch.UseStepBarriers)
                {
                    TryAdvanceProgramBarrier(batch, now);
                }
            }
        }
        private void FailProgramTarget(
            ProgramTargetExecution execution,
            ExecutionResultCode result,
            string reason,
            double now)
        {
            if (execution == null ||
                !_programExecutions.ContainsKey(execution.TargetKey))
            {
                return;
            }
            Agent agent = execution.Handle?.Agent;
            if (agent != null && ReferenceEquals(agent.Mission, Mission))
            {
                TryReleaseProgramOwnedChannels(execution, agent);
            }
            CompleteProgramTarget(execution, result, reason, now, true);
        }
        private void CancelProgramForAgent(
            Agent agent,
            ExecutionResultCode result,
            string reason)
        {
            if (agent == null)
            {
                return;
            }
            double now = Mission?.CurrentTime ?? 0d;
            foreach (ProgramTargetExecution execution in _programExecutions.Values
                         .Where(candidate => ReferenceEquals(candidate.Handle.Agent, agent))
                         .ToArray())
            {
                FailProgramTarget(execution, result, reason, now);
            }
        }
        private void TransferProgramKneelOwnership(
            ProgramTargetExecution execution,
            Agent agent)
        {
            ProgramKneelRuntime kneel = execution.PersistentKneel;
            if (kneel == null || !kneel.Holding)
            {
                return;
            }
            _ownedStates[agent.Index] = new OwnedActionState
            {
                Handle = execution.Handle,
                Definition = kneel.SelectedAction,
                Phase = OwnedStatePhase.Holding,
                StateGeneration = 1,
                EnterAction = kneel.EnterAction,
                HoldAction = kneel.HoldAction,
                ExitAction = kneel.ExitAction,
                Channel = kneel.Channel,
                AdditionalFlags = kneel.AdditionalFlags,
                TransitionStartedAt = kneel.HoldingSinceMissionTime,
                EnterObserved = true,
                HoldRequested = true,
                ActivePlan = null
            };
            execution.PersistentKneel = null;
        }
        private void TransferProgramDanceOwnership(
            ProgramTargetExecution execution,
            ProgramPlaybackRuntime playback,
            Agent agent)
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
    }
}
