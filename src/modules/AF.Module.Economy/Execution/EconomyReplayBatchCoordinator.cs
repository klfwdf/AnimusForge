using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge;

internal enum EconomyReplayStepState
{
    Rejected,
    Applied,
    UnknownAfterStart
}

internal sealed class EconomyReplayStepOutcome
{
    private EconomyReplayStepOutcome(EconomyReplayStepState state, string factText)
    {
        State = state;
        FactText = factText ?? string.Empty;
    }

    internal EconomyReplayStepState State { get; }
    internal string FactText { get; }
    internal static EconomyReplayStepOutcome Rejected()
        => new EconomyReplayStepOutcome(EconomyReplayStepState.Rejected, string.Empty);

    internal static EconomyReplayStepOutcome Applied(string factText)
        => new EconomyReplayStepOutcome(EconomyReplayStepState.Applied, factText);

    internal static EconomyReplayStepOutcome Unknown()
        => new EconomyReplayStepOutcome(EconomyReplayStepState.UnknownAfterStart, string.Empty);
}

internal sealed class EconomyReplayAppliedFact
{
    internal EconomyReplayAppliedFact(EconomyRewardDebtAction action, string text)
    {
        Action = action;
        Text = text ?? string.Empty;
    }

    internal EconomyRewardDebtAction Action { get; }
    internal string Text { get; }
}

internal sealed class EconomyReplayBatchOutcome
{
    internal EconomyReplayBatchOutcome(
        int appliedCount,
        int failedCount,
        bool unknownAfterStart,
        IList<EconomyReplayAppliedFact> facts)
    {
        AppliedCount = appliedCount;
        FailedCount = failedCount;
        UnknownAfterStart = unknownAfterStart;
        Facts = new List<EconomyReplayAppliedFact>(facts ?? Array.Empty<EconomyReplayAppliedFact>()).AsReadOnly();
    }

    internal int AppliedCount { get; }
    internal int FailedCount { get; }
    internal bool UnknownAfterStart { get; }
    internal IReadOnlyList<EconomyReplayAppliedFact> Facts { get; }
}

/// <summary>
/// Owns the shared batch terminal semantics for Hero, Party, and Merchant
/// economy replay. The supplied step remains the only game-thread mutator.
/// </summary>
internal static class EconomyReplayBatchCoordinator
{
    internal static EconomyReplayBatchOutcome Execute(
        IEnumerable<EconomyRewardDebtAction> actions,
        Func<EconomyRewardDebtAction, EconomyReplayStepOutcome> executeStep,
        Action<EconomyRewardDebtAction, Exception> observeUnexpectedException)
    {
        if (executeStep == null)
        {
            throw new ArgumentNullException(nameof(executeStep));
        }

        int appliedCount = 0;
        int failedCount = 0;
        bool unknownAfterStart = false;
        List<EconomyReplayAppliedFact> facts = new List<EconomyReplayAppliedFact>();
        foreach (EconomyRewardDebtAction action in actions ?? Array.Empty<EconomyRewardDebtAction>())
        {
            if (action == null)
            {
                failedCount++;
                continue;
            }

            EconomyReplayStepOutcome step;
            try
            {
                step = executeStep(action);
            }
            catch (Exception exception)
            {
                observeUnexpectedException?.Invoke(action, exception);
                step = EconomyReplayStepOutcome.Unknown();
            }

            if (step == null || step.State == EconomyReplayStepState.UnknownAfterStart)
            {
                failedCount++;
                unknownAfterStart = true;
                break;
            }
            if (step.State != EconomyReplayStepState.Applied)
            {
                failedCount++;
                continue;
            }

            appliedCount++;
            if (!string.IsNullOrWhiteSpace(step.FactText))
            {
                facts.Add(new EconomyReplayAppliedFact(action, step.FactText));
            }
        }
        return new EconomyReplayBatchOutcome(
            appliedCount,
            failedCount,
            unknownAfterStart,
            facts);
    }

    internal static EconomyRewardDebtReplayResult Complete(
        EconomyReplayBatchOutcome outcome,
        IEnumerable<FactRecord> confirmedFacts,
        string unknownErrorCode,
        string noActionAppliedErrorCode,
        string noActionsErrorCode,
        string partialErrorCode)
    {
        if (outcome == null)
        {
            return new EconomyRewardDebtReplayResult(
                EconomyRewardDebtReplayStatus.UnknownAfterStart,
                0,
                confirmedFacts,
                unknownErrorCode);
        }
        if (outcome.UnknownAfterStart)
        {
            return new EconomyRewardDebtReplayResult(
                EconomyRewardDebtReplayStatus.UnknownAfterStart,
                outcome.AppliedCount,
                confirmedFacts,
                unknownErrorCode);
        }
        if (outcome.AppliedCount <= 0)
        {
            return new EconomyRewardDebtReplayResult(
                EconomyRewardDebtReplayStatus.Failed,
                0,
                confirmedFacts,
                outcome.FailedCount > 0 ? noActionAppliedErrorCode : noActionsErrorCode);
        }
        return new EconomyRewardDebtReplayResult(
            outcome.FailedCount > 0
                ? EconomyRewardDebtReplayStatus.PartiallyApplied
                : EconomyRewardDebtReplayStatus.Applied,
            outcome.AppliedCount,
            confirmedFacts,
            outcome.FailedCount > 0 ? partialErrorCode : string.Empty);
    }
}
