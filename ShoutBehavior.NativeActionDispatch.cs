using System;
using System.Diagnostics;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge;

public partial class ShoutBehavior
{
    // This outcome covers this dispatch boundary, not earlier raw/taunt/natural actions in the turn.
    internal sealed class NativeConversationActionDispatchException : InvalidOperationException
    {
        internal NativeConversationActionDispatchException(bool ownerStarted, Exception cause, bool queueTimedOut = false)
            : base(ownerStarted
                ? "对话动作或收尾处理异常，部分操作可能已经执行。请检查实际游戏结果，不要自动重试整轮。"
                : queueTimedOut
                    ? "主线程未及时开始本次后处理动作派发，已停止等待；此队列动作不会随后补做。请检查此前已发生的游戏结果，不要自动重试整轮。"
                    : "本次后处理动作派发未开始，当前回复未完成处理。请检查游戏状态后再继续。", cause)
        {
            EffectState = ownerStarted ? ActionExecutionEffectState.UnknownAfterStart : ActionExecutionEffectState.NoConfirmedEffect;
            ErrorCode = ownerStarted ? "native.actions.outcome_unknown"
                : queueTimedOut ? "native.actions.dispatch_timeout" : "native.actions.dispatch_not_started";
        }
        internal ActionExecutionEffectState EffectState { get; }
        internal string ErrorCode { get; }
        internal bool CanRetryAutomatically => false;
    }

    private NativeConversationGameActionResult ExecuteNativeConversationActionDispatch(
        NativeConversationAdmission admission, Func<NativeConversationGameActionResult> execute,
        string targetLog, int targetAgentIndex, Action beforeOwner = null, Action onDiscard = null)
    {
        bool ownerStarted = false;
        Stopwatch watch = Stopwatch.StartNew();
        try
        {
            ObserveNativeActionDispatch("mainthread_start", targetLog, targetAgentIndex, watch);
            if (!IsNativeConversationAdmissionCurrent(admission, out _))
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

    private static void ObserveNativeActionDispatch(string stage, string targetLog, int targetAgentIndex,
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
