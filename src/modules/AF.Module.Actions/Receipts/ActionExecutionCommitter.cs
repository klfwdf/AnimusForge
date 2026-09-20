using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Runtime;

/// <summary>
/// Executes one already-authorized ActionPlan and converts the channel/domain
/// owner's mutable receipt into a terminal, detached outcome. This owner does
/// not write conversation history or AFEF; <see cref="InteractionResultCommitter"/>
/// composes that separate memory transaction after execution is terminal.
/// </summary>
internal static class ActionExecutionCommitter
{
    internal static ActionExecutionCommitResult Execute(
        ActionPlan actionPlan,
        GameInteractionSnapshot snapshot,
        IActionPlanExecutor actionExecutor,
        string requestId,
        string actionFingerprint)
    {
        if (actionPlan == null || actionPlan.Actions.Count == 0)
        {
            return ActionExecutionCommitResult.NoActions();
        }
        if (snapshot == null || actionExecutor == null)
        {
            return ActionExecutionCommitResult.Rejected(
                InteractionStatus.RejectedByValidation,
                "missing_action_execution_boundary");
        }

        InteractionStatus actionStatus;
        try
        {
            actionStatus = actionExecutor is IRequestBoundActionPlanExecutor requestBound
                ? requestBound.ValidateAndExecute(
                    actionPlan,
                    snapshot,
                    requestId,
                    actionFingerprint)
                : actionExecutor.ValidateAndExecute(actionPlan, snapshot);
        }
        catch
        {
            // An owner callback can mutate game state before throwing. Treat
            // that boundary as terminal unknown; callers must never retry it.
            return new ActionExecutionCommitResult(
                InteractionStatus.NonRetryableFailure,
                false,
                Array.Empty<FactRecord>(),
                "action_executor_exception",
                ActionExecutionEffectState.UnknownAfterStart,
                TryReadDuelDispatchReceipt(actionExecutor));
        }

        DetachedDuelDispatchReceipt duelDispatch = TryReadDuelDispatchReceipt(actionExecutor);
        IActionPlanExecutionOutcomeReceipt outcome =
            actionExecutor as IActionPlanExecutionOutcomeReceipt;
        ActionExecutionEffectState effectState =
            actionExecutor is IActionPlanExecutionEffectReceipt effectReceipt
                ? effectReceipt.EffectState
                : (outcome?.AppliedActionCount ?? 0) > 0
                    ? ActionExecutionEffectState.ConfirmedEffect
                    : ActionExecutionEffectState.NoConfirmedEffect;

        if (actionStatus != InteractionStatus.Executed)
        {
            bool terminalOutcome = (outcome?.AppliedActionCount ?? 0) > 0
                || effectState == ActionExecutionEffectState.UnknownAfterStart
                || duelDispatch?.State == DetachedDuelDispatchState.Queued
                || duelDispatch?.State == DetachedDuelDispatchState.Started
                || duelDispatch?.State == DetachedDuelDispatchState.UnknownAfterStart;
            if (terminalOutcome)
            {
                bool hasConfirmedActions = (outcome?.AppliedActionCount ?? 0) > 0;
                IReadOnlyList<FactRecord> confirmedFacts = hasConfirmedActions
                    ? outcome?.ConfirmedFacts ?? Array.Empty<FactRecord>()
                    : Array.Empty<FactRecord>();
                string terminalError = !string.IsNullOrWhiteSpace(outcome?.ExecutionErrorCode)
                    ? outcome.ExecutionErrorCode
                    : !string.IsNullOrWhiteSpace(duelDispatch?.ErrorCode)
                        ? duelDispatch.ErrorCode
                        : effectState == ActionExecutionEffectState.UnknownAfterStart
                            ? "action_unknown_after_start"
                            : "partial_action_execution";
                return new ActionExecutionCommitResult(
                    InteractionStatus.NonRetryableFailure,
                    hasConfirmedActions,
                    confirmedFacts,
                    terminalError,
                    effectState,
                    duelDispatch);
            }

            return new ActionExecutionCommitResult(
                actionStatus,
                false,
                Array.Empty<FactRecord>(),
                !string.IsNullOrWhiteSpace(duelDispatch?.ErrorCode)
                    ? duelDispatch.ErrorCode
                    : "action_not_executed",
                ActionExecutionEffectState.NoConfirmedEffect,
                duelDispatch);
        }

        IReadOnlyList<FactRecord> successfulFacts =
            (actionExecutor as IActionPlanExecutionReceipt)?.ConfirmedFacts
            ?? Array.Empty<FactRecord>();
        return new ActionExecutionCommitResult(
            InteractionStatus.Executed,
            true,
            successfulFacts,
            string.Empty,
            effectState == ActionExecutionEffectState.UnknownAfterStart
                ? ActionExecutionEffectState.UnknownAfterStart
                : ActionExecutionEffectState.ConfirmedEffect,
            duelDispatch);
    }

    private static DetachedDuelDispatchReceipt TryReadDuelDispatchReceipt(
        IActionPlanExecutor actionExecutor)
    {
        try
        {
            return (actionExecutor as IDetachedDuelDispatchExecutionReceipt)
                ?.DuelDispatchReceipt
                ?.Clone();
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Terminal action-only receipt. It is deliberately independent from visible
/// conversation history so legacy channels can converge on action semantics
/// before J10 changes their presentation/session state machines.
/// </summary>
internal sealed class ActionExecutionCommitResult
{
    internal ActionExecutionCommitResult(
        InteractionStatus status,
        bool actionsExecuted,
        IReadOnlyList<FactRecord> confirmedFacts,
        string errorCode,
        ActionExecutionEffectState effectState,
        DetachedDuelDispatchReceipt duelDispatchReceipt)
    {
        Status = status;
        ActionsExecuted = actionsExecuted;
        ConfirmedFacts = confirmedFacts ?? Array.Empty<FactRecord>();
        ErrorCode = errorCode ?? string.Empty;
        EffectState = effectState;
        DuelDispatchReceipt = duelDispatchReceipt?.Clone();
    }

    internal InteractionStatus Status { get; }
    internal bool ActionsExecuted { get; }
    internal IReadOnlyList<FactRecord> ConfirmedFacts { get; }
    internal string ErrorCode { get; }
    internal ActionExecutionEffectState EffectState { get; }
    internal DetachedDuelDispatchReceipt DuelDispatchReceipt { get; }

    internal static ActionExecutionCommitResult NoActions()
        => new ActionExecutionCommitResult(
            InteractionStatus.Succeeded,
            false,
            Array.Empty<FactRecord>(),
            string.Empty,
            ActionExecutionEffectState.NoConfirmedEffect,
            null);

    internal static ActionExecutionCommitResult Rejected(
        InteractionStatus status,
        string errorCode)
        => new ActionExecutionCommitResult(
            status,
            false,
            Array.Empty<FactRecord>(),
            errorCode,
            ActionExecutionEffectState.NoConfirmedEffect,
            null);
}
