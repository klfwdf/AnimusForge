using System;
using System.Diagnostics;
using AnimusForge.Refactor.Contracts;

using static AnimusForge.ShoutBehavior;

namespace AnimusForge;

internal sealed partial class NativeConversationGameEffectsRuntime
{
    // This outcome covers this dispatch boundary, not earlier raw/taunt/natural actions in the turn.


    private NativeConversationGameActionResult ExecuteNativeConversationActionDispatch(
        NativeConversationAdmission admission, Func<NativeConversationGameActionResult> execute,
        string targetLog, int targetAgentIndex, Action beforeOwner = null, Action onDiscard = null)
    {
        bool ownerStarted = false;
        Stopwatch watch = Stopwatch.StartNew();
        try
        {
            ObserveNativeActionDispatch("mainthread_start", targetLog, targetAgentIndex, watch);
            if (!_ports.IsNativeConversationAdmissionCurrent(admission, out _))
            {
                onDiscard?.Invoke();
                return new NativeConversationGameActionResult { Content = "", ResponseDiscarded = true };
            }
            beforeOwner?.Invoke();
            ownerStarted = true;
            NativeConversationGameActionResult result = execute();
            if (result == null)
                throw new InvalidOperationException("native.action_result_missing");
            if (result.ResponseDiscarded)
                onDiscard?.Invoke();
            ObserveNativeActionDispatch(result.ResponseDiscarded ? "discarded" : "mainthread_done", targetLog, targetAgentIndex, watch);
            return result;
        }
        catch (Exception ex)
        {
            ObserveNativeActionDispatch("mainthread_exception", targetLog, targetAgentIndex, watch, ex);
            throw new NativeConversationActionDispatchException(ownerStarted, ex);
        }
    }

    internal static void ObserveNativeActionDispatch(string stage, string targetLog, int targetAgentIndex,
        Stopwatch watch = null, Exception error = null)
    {
        try
        {
            string detail = "target=" + targetLog + " agent=" + targetAgentIndex
                + " stage=" + stage + (watch == null ? "" : " ms=" + Math.Round(watch.Elapsed.TotalMilliseconds, 2))
                + (error == null ? "" : " error=" + error);
            Logger.Log("ShoutBehavior", "[NativeConversation] game_actions " + detail);
            FreezeWatchdog.Mark("NativeConversation.game_actions_" + stage, detail, immediate: true);
        }
        catch (Exception)
        {
            // Observability must never change whether actions run or how their Task completes.
            return;
        }
    }
}

public partial class ShoutBehavior
{
internal sealed class NativeConversationActionDispatchException : InvalidOperationException
    {
        internal NativeConversationActionDispatchException(bool ownerStarted, Exception cause, bool queueTimedOut = false)
            : base(ownerStarted
                ? cause is NativeConversationGameEffectsRuntime.NativeConversationHistoryCommitException
                    ? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0644")
                    : ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0645")
                : queueTimedOut
                    ? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0646")
                    : ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0647"), cause)
        {
            EffectState = ownerStarted ? ActionExecutionEffectState.UnknownAfterStart : ActionExecutionEffectState.NoConfirmedEffect;
            ErrorCode = ownerStarted ? (cause is NativeConversationGameEffectsRuntime.NativeConversationHistoryCommitException ? "native.memory.commit_unconfirmed" : "native.actions.outcome_unknown")
                : queueTimedOut ? "native.actions.dispatch_timeout" : "native.actions.dispatch_not_started";
        }
        internal ActionExecutionEffectState EffectState { get; }
        internal string ErrorCode { get; }
        internal bool CanRetryAutomatically => false;
    }
}
