using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RichExecutions.Core;

public sealed class ExecutionStartingEventArgs : EventArgs
{
    internal ExecutionStartingEventArgs(ExecutionRequest request)
    {
        Request = request;
    }

    public ExecutionRequest Request { get; }
    public bool IsCancelled { get; private set; }
    public TextObject CancellationReason { get; private set; } = TextObject.GetEmpty();

    public void Cancel(TextObject reason)
    {
        IsCancelled = true;
        CancellationReason = reason ?? TextObject.GetEmpty();
    }
}

public sealed class ExecutionDeathCommittingEventArgs : EventArgs
{
    internal ExecutionDeathCommittingEventArgs(ExecutionRequest request, ExecutionActor actor)
    {
        Request = request;
        Actor = actor;
    }

    public ExecutionRequest Request { get; }
    public ExecutionActor Actor { get; }
}

public sealed class ExecutionCompletedEventArgs : EventArgs
{
    internal ExecutionCompletedEventArgs(ExecutionOutcome outcome)
    {
        Outcome = outcome;
    }

    public ExecutionOutcome Outcome { get; }
}

public sealed class ExecutionCancelledEventArgs : EventArgs
{
    internal ExecutionCancelledEventArgs(
        ExecutionRequest request,
        ExecutionFailureReason reason,
        TextObject message)
    {
        Request = request;
        Reason = reason;
        Message = message;
    }

    public ExecutionRequest Request { get; }
    public ExecutionFailureReason Reason { get; }
    public TextObject Message { get; }
}

public static class RichExecutionEvents
{
    public static event EventHandler<ExecutionStartingEventArgs>? ExecutionStarting;
    public static event EventHandler<ExecutionDeathCommittingEventArgs>? ExecutionDeathCommitting;
    public static event EventHandler<ExecutionCompletedEventArgs>? ExecutionCompleted;
    public static event EventHandler<ExecutionCancelledEventArgs>? ExecutionCancelled;

    internal static ExecutionStartingEventArgs RaiseStarting(ExecutionRequest request)
    {
        var args = new ExecutionStartingEventArgs(request);
        InvokeSafely(ExecutionStarting, args, nameof(ExecutionStarting));
        return args;
    }

    internal static void RaiseDeathCommitting(ExecutionRequest request, ExecutionActor actor) =>
        InvokeSafely(
            ExecutionDeathCommitting,
            new ExecutionDeathCommittingEventArgs(request, actor),
            nameof(ExecutionDeathCommitting));

    internal static void RaiseCompleted(ExecutionOutcome outcome) =>
        InvokeSafely(
            ExecutionCompleted,
            new ExecutionCompletedEventArgs(outcome),
            nameof(ExecutionCompleted));

    internal static void RaiseCancelled(
        ExecutionRequest request,
        ExecutionFailureReason reason,
        TextObject message) =>
        InvokeSafely(
            ExecutionCancelled,
            new ExecutionCancelledEventArgs(request, reason, message),
            nameof(ExecutionCancelled));

    private static void InvokeSafely<TEventArgs>(
        EventHandler<TEventArgs>? handlers,
        TEventArgs args,
        string eventName)
        where TEventArgs : EventArgs
    {
        if (handlers is null)
        {
            return;
        }

        foreach (EventHandler<TEventArgs> handler in handlers.GetInvocationList())
        {
            try
            {
                handler(null, args);
            }
            catch (Exception exception)
            {
                Debug.Print($"[Vengeance] Extension handler failed in {eventName}: {exception}");
            }
        }
    }
}
