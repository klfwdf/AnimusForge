using System;

namespace RichExecutions.Core;

public sealed class ExecutionStateMachine
{
    public ExecutionSessionState State { get; private set; } = ExecutionSessionState.Preparing;

    public bool CanTransitionTo(ExecutionSessionState next)
    {
        if (next == ExecutionSessionState.Cancelled)
        {
            return State != ExecutionSessionState.Aftermath && State != ExecutionSessionState.Cancelled;
        }

        return State switch
        {
            ExecutionSessionState.Preparing => next == ExecutionSessionState.WaitingForPlayer,
            ExecutionSessionState.WaitingForPlayer => next == ExecutionSessionState.CeremonyPreparation,
            ExecutionSessionState.CeremonyPreparation => next == ExecutionSessionState.Execution,
            ExecutionSessionState.Execution => next == ExecutionSessionState.CrowdReaction,
            ExecutionSessionState.CrowdReaction => next == ExecutionSessionState.Aftermath,
            _ => false
        };
    }

    public bool TryTransitionTo(ExecutionSessionState next)
    {
        if (!CanTransitionTo(next))
        {
            return false;
        }

        State = next;
        return true;
    }

    public void TransitionTo(ExecutionSessionState next)
    {
        if (!TryTransitionTo(next))
        {
            throw new InvalidOperationException($"Invalid execution state transition: {State} -> {next}.");
        }
    }
}

public sealed class ExecutionCommitGuard<TOutcome> where TOutcome : class
{
    private readonly object _sync = new();
    private bool _commitStarted;
    private TOutcome? _outcome;

    public bool TryBeginCommit()
    {
        lock (_sync)
        {
            if (_commitStarted)
            {
                return false;
            }

            _commitStarted = true;
            return true;
        }
    }

    public void Complete(TOutcome outcome)
    {
        lock (_sync)
        {
            _outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
        }
    }

    public bool TryGetOutcome(out TOutcome outcome)
    {
        lock (_sync)
        {
            outcome = _outcome!;
            return _outcome is not null;
        }
    }
}
