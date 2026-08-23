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
        private void BuildAndQueuePlans(
            CapturedSceneActionEvent captured,
            ParseDecision decision,
            double now)
        {
            if (decision?.ProgramV4 == null)
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    ExecutionResultCode.InvalidCommand,
                    "Resolved decision has no V4 action program.");
                return;
            }
            if (!decision.ProgramV4.IsSingleAction ||
                string.Equals(
                    decision.ProgramV4.SingleIntentKey,
                    SceneActionFrameworkV4.Dance,
                    StringComparison.Ordinal))
            {
                BuildAndQueueProgramPlans(captured, decision, now);
                return;
            }
            if (!SceneActionsRuntimeHost.Catalog.TryGetIntent(
                decision.IntentKey,
                out IntentDefinition intent))
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    ExecutionResultCode.InvalidCommand,
                    "Resolved intent is absent from the catalog.");
                return;
            }

            SelectedAction selected = null;
            if (intent.Kind == IntentKind.PlayAction &&
                !SceneActionsRuntimeHost.Catalog.TrySelectAction(
                    intent.ActionKey,
                    SceneActionsRuntimeHost.Runtime,
                    SceneActionsRuntimeHost.Settings,
                    out selected,
                    out ExecutionResultCode selectionFailure))
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    selectionFailure,
                    "Action variant is not production-enabled for the exact runtime.");
                return;
            }

            if (!SceneActionPermissionRouter.TryResolveTargetMode(
                    decision,
                    intent,
                    out TargetMode mode))
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    ExecutionResultCode.InvalidCommand,
                    "Resolved intent or decision is missing; permission routing failed closed.");
                return;
            }
            List<Agent> targets = ResolveTargets(captured, mode);
            if (targets.Count == 0)
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    ExecutionResultCode.NoTarget,
                    "The requested target snapshot is empty or invalid.");
                return;
            }

            SceneActionSettings settings = SceneActionsRuntimeHost.Settings;
            int requestedCount = targets.Count;
            int permittedCount = requestedCount;
            if (requestedCount > settings.MaxBatchTargets)
            {
                if (settings.OverflowPolicy == SchedulerOverflowPolicy.Reject)
                {
                    FinishAcceptedRequestWithoutTargets(
                        captured.EventId,
                        ExecutionResultCode.BatchTooLarge,
                        "Target count exceeds maxBatchTargets.");
                    return;
                }
                permittedCount = settings.MaxBatchTargets;
            }
            int availableSlots = _scheduler.Capacity - _scheduler.Count;
            if (permittedCount > availableSlots &&
                settings.OverflowPolicy == SchedulerOverflowPolicy.Reject)
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    ExecutionResultCode.QueueFull,
                    "Stable scheduler has insufficient capacity for the whole batch.");
                return;
            }
            permittedCount = Math.Min(permittedCount, Math.Max(0, availableSlots));

            List<SessionAgentHandle> handles = targets.Select(agent => new SessionAgentHandle
            {
                SessionGeneration = _sessionGeneration,
                Mission = Mission,
                Agent = agent,
                AgentIndex = agent.Index
            }).ToList();
            RequestTracker tracker = new RequestTracker(
                captured.EventId,
                decision.IntentKey,
                decision.Resolver ?? ResolverSource.ExactCommand,
                captured.InputSource,
                handles);
            _trackers.Add(captured.EventId, tracker);

            bool stagger = SceneActionPermissionRouter.ShouldStaggerNpcBatch(
                decision,
                requestedCount,
                settings);
            bool forceIndependentStagger = SceneActionPermissionRouter.ShouldUseForcedIndependentStagger(
                decision,
                requestedCount,
                settings);
            for (int index = 0; index < handles.Count; index++)
            {
                PlannedTarget plan = new PlannedTarget
                {
                    RequestId = captured.EventId,
                    InputSource = captured.InputSource,
                    Resolver = decision.Resolver ?? ResolverSource.ExactCommand,
                    Intent = intent,
                    SelectedAction = selected,
                    Handle = handles[index],
                    TargetKey = MakeTargetKey(handles[index], index),
                    TargetOrdinal = index,
                    QueuedAtMissionTime = now,
                    ExpiresAtMissionTime = captured.SubmittedAtMissionTime +
                                           (settings.RequestTtlMs / 1000d)
                };
                if (selected != null &&
                    selected.Definition.Mode != ActionMode.Stateful)
                {
                    int variantIndex = selected.Definition.Mode == ActionMode.RandomGroup
                        ? DeterministicSelector.PickIndex(
                            captured.EventId.ToString("N"),
                            handles[index].StableId,
                            index,
                            selected.Variant.ActionIds.Count)
                        : 0;
                    plan.FrozenActionId = selected.Variant.ActionIds[variantIndex];
                }

                if (index >= permittedCount)
                {
                    FinishPlan(
                        plan,
                        requestedCount > settings.MaxBatchTargets
                            ? ExecutionResultCode.BatchTooLarge
                            : ExecutionResultCode.QueueFull,
                        "Stable truncation omitted this target.");
                    continue;
                }
                double delay = 0d;
                if (forceIndependentStagger)
                {
                    delay = DeterministicSelector.PickIndependentStaggerSeconds(
                        captured.EventId.ToString("N"),
                        handles[index].StableId,
                        0,
                        index,
                        settings.ForceStaggerMinSeconds,
                        settings.ForceStaggerMaxSeconds);
                }
                else if (stagger)
                {
                    delay = index * settings.StaggerSeconds;
                }
                double due = now + delay;
                if (!_scheduler.TryEnqueue(due, plan, out long sequence))
                {
                    FinishPlan(plan, ExecutionResultCode.QueueFull, "Scheduler enqueue failed.");
                    continue;
                }
                plan.StableSequence = sequence;
                SceneActionsLog.Info(
                    "QUEUE",
                    FormatPlan(plan) + " Result=Queued Due=" + due.ToString("F3"));
            }
        }
        private void BuildAndQueueProgramPlans(
            CapturedSceneActionEvent captured,
            ParseDecision decision,
            double now)
        {
            ActionProgramV4 program = null;
            string normalizationError = null;
            if (decision?.ProgramV4 == null ||
                !decision.ProgramV4.TryNormalizeForExecution(
                    out program,
                    out normalizationError))
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    ExecutionResultCode.InvalidCommand,
                    normalizationError ?? "Program normalization failed.");
                return;
            }
            if (!TryValidateProgramReady(
                program,
                out ExecutionResultCode readinessFailure,
                out string readinessReason))
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    readinessFailure,
                    readinessReason);
                return;
            }

            string firstIntentKey = program.Steps[0].IntentKeys[0];
            if (!SceneActionsRuntimeHost.Catalog.TryGetIntent(
                firstIntentKey,
                out IntentDefinition firstIntent))
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    ExecutionResultCode.InvalidCommand,
                    "Program first intent is absent from the catalog.");
                return;
            }
            if (!SceneActionPermissionRouter.TryResolveTargetMode(
                    decision,
                    firstIntent,
                    out TargetMode mode))
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    ExecutionResultCode.InvalidCommand,
                    "Program intent or decision is missing; permission routing failed closed.");
                return;
            }
            List<Agent> targets = ResolveTargets(captured, mode);
            if (targets.Count == 0)
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    ExecutionResultCode.NoTarget,
                    "The frozen program target snapshot is empty or invalid.");
                return;
            }

            SceneActionSettings settings = SceneActionsRuntimeHost.Settings;
            int requestedCount = targets.Count;
            int permittedCount = requestedCount;
            if (requestedCount > settings.MaxBatchTargets)
            {
                if (settings.OverflowPolicy == SchedulerOverflowPolicy.Reject)
                {
                    FinishAcceptedRequestWithoutTargets(
                        captured.EventId,
                        ExecutionResultCode.BatchTooLarge,
                        "Program target count exceeds maxBatchTargets.");
                    return;
                }
                permittedCount = settings.MaxBatchTargets;
            }
            int availableSlots = _scheduler.Capacity - _scheduler.Count;
            if (permittedCount > availableSlots &&
                settings.OverflowPolicy == SchedulerOverflowPolicy.Reject)
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    ExecutionResultCode.QueueFull,
                    "Scheduler has insufficient capacity for the program target snapshot.");
                return;
            }
            permittedCount = Math.Min(permittedCount, Math.Max(0, availableSlots));

            List<SessionAgentHandle> handles = targets.Select(agent => new SessionAgentHandle
            {
                SessionGeneration = _sessionGeneration,
                Mission = Mission,
                Agent = agent,
                AgentIndex = agent.Index
            }).ToList();
            RequestTracker tracker = new RequestTracker(
                captured.EventId,
                "program:" + program.ProtocolExpression,
                decision.Resolver ?? ResolverSource.AiClassifier,
                captured.InputSource,
                handles);
            _trackers.Add(captured.EventId, tracker);

            ProgramBatchExecution batch = new ProgramBatchExecution
            {
                RequestId = captured.EventId,
                UseStepBarriers = SceneActionPermissionRouter.ShouldUseForcedStepBarriers(
                    decision,
                    requestedCount,
                    settings)
            };
            _programBatches[captured.EventId] = batch;
            bool legacyStagger = SceneActionPermissionRouter.ShouldStaggerNpcBatch(
                decision,
                requestedCount,
                settings);
            for (int index = 0; index < handles.Count; index++)
            {
                SessionAgentHandle handle = handles[index];
                string targetKey = MakeTargetKey(handle, index);
                ProgramTargetExecution execution = new ProgramTargetExecution
                {
                    RequestId = captured.EventId,
                    InputSource = captured.InputSource,
                    Resolver = decision.Resolver ?? ResolverSource.AiClassifier,
                    Handle = handle,
                    TargetKey = targetKey,
                    TargetOrdinal = index,
                    Program = program,
                    Steps = FreezeProgramSteps(
                        program,
                        captured.EventId,
                        handle,
                        index),
                    SequentialFallbackSteps = FreezeProgramSteps(
                        program.ToSequentialProgram(),
                        captured.EventId,
                        handle,
                        index),
                    State = ProgramExecutionState.Waiting,
                    CurrentStepIndex = 0
                };
                PlannedTarget plan = CreateProgramPlan(execution, now, captured);

                if (index >= permittedCount)
                {
                    FinishPlan(
                        plan,
                        requestedCount > settings.MaxBatchTargets
                            ? ExecutionResultCode.BatchTooLarge
                            : ExecutionResultCode.QueueFull,
                        "Stable truncation omitted this program target.");
                    continue;
                }

                _programExecutions.Add(targetKey, execution);
                batch.Targets.Add(targetKey, execution);
                double delay = 0d;
                if (batch.UseStepBarriers)
                {
                    delay = DeterministicSelector.PickIndependentStaggerSeconds(
                        captured.EventId.ToString("N"),
                        handle.StableId,
                        0,
                        index,
                        settings.ForceStaggerMinSeconds,
                        settings.ForceStaggerMaxSeconds);
                }
                else if (legacyStagger)
                {
                    delay = index * settings.StaggerSeconds;
                }
                double due = now + delay;
                if (!_scheduler.TryEnqueue(due, plan, out long sequence))
                {
                    _programExecutions.Remove(targetKey);
                    batch.Targets.Remove(targetKey);
                    FinishPlan(plan, ExecutionResultCode.QueueFull, "Program enqueue failed.");
                    continue;
                }
                plan.StableSequence = sequence;
                execution.State = ProgramExecutionState.Scheduled;
                SceneActionsLog.Info(
                    "PROGRAM_QUEUE",
                    FormatPlan(plan) +
                    " Program=" + program.ProtocolExpression +
                    " Step=0 Due=" + due.ToString("F3"));
            }
            if (batch.Targets.Count == 0)
            {
                _programBatches.Remove(captured.EventId);
            }
        }
        private List<FrozenProgramStep> FreezeProgramSteps(
            ActionProgramV4 program,
            Guid requestId,
            SessionAgentHandle handle,
            int targetOrdinal)
        {
            List<FrozenProgramStep> result = new List<FrozenProgramStep>();
            int actionOrdinal = 0;
            foreach (ActionProgramStepV4 sourceStep in program.Steps)
            {
                FrozenProgramStep step = new FrozenProgramStep();
                foreach (string key in sourceStep.IntentKeys)
                {
                    SceneActionsRuntimeHost.Catalog.TryGetIntent(
                        key,
                        out IntentDefinition intent);
                    SelectedAction selected = null;
                    string frozenActionId = null;
                    if (intent.Kind == IntentKind.PlayAction)
                    {
                        SceneActionsRuntimeHost.Catalog.TrySelectAction(
                            intent.ActionKey,
                            SceneActionsRuntimeHost.Runtime,
                            SceneActionsRuntimeHost.Settings,
                            out selected,
                            out _);
                        if (selected.Definition.Mode != ActionMode.Stateful)
                        {
                            int variantIndex = selected.Definition.Mode == ActionMode.RandomGroup
                                ? DeterministicSelector.PickIndex(
                                    requestId.ToString("N"),
                                    handle.StableId,
                                    (targetOrdinal * ActionProgramV4.MaximumActionCount) +
                                    actionOrdinal,
                                    selected.Variant.ActionIds.Count)
                                : 0;
                            frozenActionId = selected.Variant.ActionIds[variantIndex];
                        }
                    }
                    step.Actions.Add(new FrozenProgramAction
                    {
                        Intent = intent,
                        SelectedAction = selected,
                        FrozenActionId = frozenActionId
                    });
                    actionOrdinal++;
                }
                result.Add(step);
            }
            return result;
        }
        private PlannedTarget CreateProgramPlan(
            ProgramTargetExecution execution,
            double now,
            CapturedSceneActionEvent captured = null)
        {
            FrozenProgramAction first = execution.Steps[execution.CurrentStepIndex].Actions[0];
            return new PlannedTarget
            {
                RequestId = execution.RequestId,
                InputSource = execution.InputSource,
                Resolver = execution.Resolver,
                Intent = first.Intent,
                SelectedAction = first.SelectedAction,
                Handle = execution.Handle,
                TargetKey = execution.TargetKey,
                TargetOrdinal = execution.TargetOrdinal,
                FrozenActionId = first.FrozenActionId,
                QueuedAtMissionTime = now,
                ExpiresAtMissionTime = captured == null
                    ? now + SceneActionsRuntimeHost.Settings.StepTimeoutSeconds
                    : captured.SubmittedAtMissionTime +
                      (SceneActionsRuntimeHost.Settings.RequestTtlMs / 1000d),
                ProgramExecution = execution,
                ProgramStepIndex = execution.CurrentStepIndex
            };
        }
    }
}