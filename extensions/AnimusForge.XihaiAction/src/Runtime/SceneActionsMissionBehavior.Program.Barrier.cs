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
        private void CompleteProgramStep(
            ProgramTargetExecution execution,
            ExecutionResultCode result,
            double now)
        {
            if (!_programExecutions.ContainsKey(execution.TargetKey))
            {
                return;
            }
            execution.LastSuccessResult = result;
            execution.ActiveStep = null;
            execution.State = ProgramExecutionState.BarrierReady;
            if (_programBatches.TryGetValue(
                    execution.RequestId,
                    out ProgramBatchExecution batch) &&
                batch.UseStepBarriers)
            {
                TryAdvanceProgramBarrier(batch, now);
                return;
            }
            AdvanceProgramTarget(execution, now, 0d);
        }
        private void AdvanceProgramTarget(
            ProgramTargetExecution execution,
            double now,
            double delay)
        {
            if (!_programExecutions.ContainsKey(execution.TargetKey))
            {
                return;
            }
            if (execution.CurrentStepIndex >= execution.Steps.Count - 1)
            {
                CompleteProgramTarget(
                    execution,
                    execution.LastSuccessResult,
                    "All frozen program steps completed by engine observation.",
                    now,
                    true);
                return;
            }
            execution.CurrentStepIndex++;
            execution.State = ProgramExecutionState.Waiting;
            ScheduleProgramStep(execution, now, delay);
        }
        private void ScheduleProgramStep(
            ProgramTargetExecution execution,
            double now,
            double delay)
        {
            if (!_programExecutions.ContainsKey(execution.TargetKey))
            {
                return;
            }
            PlannedTarget plan = CreateProgramPlan(execution, now);
            double due = now + Math.Max(0d, delay);
            if (!_scheduler.TryEnqueue(due, plan, out long sequence))
            {
                FailProgramTarget(
                    execution,
                    ExecutionResultCode.QueueFull,
                    "Scheduler rejected a subsequent program step.",
                    now);
                return;
            }
            plan.StableSequence = sequence;
            execution.State = ProgramExecutionState.Scheduled;
            SceneActionsLog.Info(
                "PROGRAM_QUEUE",
                FormatPlan(plan) +
                " Step=" + execution.CurrentStepIndex +
                " Due=" + due.ToString("F3"));
        }
        private void TryAdvanceProgramBarrier(
            ProgramBatchExecution batch,
            double now)
        {
            if (batch == null || !batch.UseStepBarriers || batch.Targets.Count == 0)
            {
                return;
            }
            ProgramTargetExecution[] active = batch.Targets.Values
                .Where(execution => execution.State != ProgramExecutionState.Terminal)
                .OrderBy(execution => execution.TargetOrdinal)
                .ToArray();
            if (active.Length == 0)
            {
                _programBatches.Remove(batch.RequestId);
                return;
            }
            if (active.Any(execution => execution.State != ProgramExecutionState.BarrierReady))
            {
                return;
            }

            SceneActionSettings settings = SceneActionsRuntimeHost.Settings;
            for (int rank = 0; rank < active.Length; rank++)
            {
                ProgramTargetExecution execution = active[rank];
                if (execution.CurrentStepIndex >= execution.Steps.Count - 1)
                {
                    CompleteProgramTarget(
                        execution,
                        execution.LastSuccessResult,
                        "Final forced-program barrier completed.",
                        now,
                        false);
                    continue;
                }
                int nextStep = execution.CurrentStepIndex + 1;
                double delay = DeterministicSelector.PickIndependentStaggerSeconds(
                    execution.RequestId.ToString("N"),
                    execution.Handle.StableId,
                    nextStep,
                    rank,
                    settings.ForceStaggerMinSeconds,
                    settings.ForceStaggerMaxSeconds);
                AdvanceProgramTarget(execution, now, delay);
            }
            if (batch.Targets.Count == 0)
            {
                _programBatches.Remove(batch.RequestId);
            }
        }
        private void DisableBatchBarriers(Guid requestId, double now)
        {
            if (!_programBatches.TryGetValue(
                requestId,
                out ProgramBatchExecution batch) ||
                !batch.UseStepBarriers)
            {
                return;
            }
            batch.UseStepBarriers = false;
            foreach (ProgramTargetExecution waiting in batch.Targets.Values
                         .Where(execution =>
                             execution.State == ProgramExecutionState.BarrierReady)
                         .ToArray())
            {
                AdvanceProgramTarget(waiting, now, 0d);
            }
        }
    }
}