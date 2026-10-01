using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Runtime;
using static AnimusForge.ShoutBehavior;
namespace AnimusForge;
internal sealed class ConversationGameThreadDispatcher
{
    private readonly PendingOperationRegistry _pendingOperations;
    private readonly Action<Action> _postMainThread;
    private readonly Func<bool> _isMainThread;
    internal ConversationGameThreadDispatcher(PendingOperationRegistry pendingOperations, Action<Action> postMainThread, Func<bool> isMainThread)
    { _pendingOperations = pendingOperations ?? throw new ArgumentNullException(nameof(pendingOperations)); _postMainThread = postMainThread ?? throw new ArgumentNullException(nameof(postMainThread)); _isMainThread = isMainThread ?? throw new ArgumentNullException(nameof(isMainThread)); }
internal Task<T> RunAsync<T>(string operationName, string targetLog, int targetAgentIndex, Func<T> func, T fallback)
    {
        long retirementVersion = _pendingOperations.Version;
        if (func == null || !_pendingOperations.Accepting) return Task.FromResult(fallback);
        string op = string.IsNullOrWhiteSpace(operationName) ? "operation" : operationName.Trim();
        string target = string.IsNullOrWhiteSpace(targetLog) ? "unknown" : targetLog.Trim();

        void Observe(string phase, string detail = "", Exception error = null)
        {
            try
            {
                string context = "target=" + target + " agent=" + targetAgentIndex + " " + detail
                    + (error == null ? "" : " " + error.GetType().Name + ": " + error.Message);
                Logger.Log("Logic", "[NativePerf] " + op + "_" + phase + " " + context);
                FreezeWatchdog.Mark("NativeConversation." + op + "_" + phase, context, immediate: true);
            }
            catch (Exception)
            {
                // Optional diagnostics never own execution or completion.
                return;
            }
        }

        T Execute(string phase)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Observe(phase + "_start");
            try { return func(); }
            // 格式错误由原上层失败提示处理；排队和直接执行必须一致。
            catch (PreprocessFormatException ex)
            {
                Observe(phase + "_exception", error: ex);
                throw;
            }
            catch (Exception ex)
            {
                Observe(phase + "_exception", error: ex);
                return fallback;
            }
            finally
            {
                sw.Stop();
                Observe(phase + "_finished", "ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
            }
        }

        if (_isMainThread())
        {
            try { return Task.FromResult(Execute("direct")); }
            catch (Exception ex) { return Task.FromException<T>(ex); }
        }

        TaskCompletionSource<T> tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        // 0 queued, 1 claimed, 2 retired: deadline 只能取消未开始的操作。
        int state = 0;
        bool Retire()
        {
            if (Interlocked.CompareExchange(ref state, 2, 0) != 0) return false;
            tcs.TrySetResult(fallback);
            return true;
        }
        IDisposable registration = _pendingOperations.Register(retirementVersion, () => Retire());
        if (registration == null) return tcs.Task;
        try
        {
            _postMainThread(delegate
            {
                if (Interlocked.CompareExchange(ref state, 1, 0) != 0) return;
                try { tcs.TrySetResult(Execute("mainthread")); }
                catch (Exception ex) { tcs.TrySetException(ex); }
            });
            Observe("queued", "callerThread=" + Thread.CurrentThread.ManagedThreadId);
        }
        catch (Exception ex)
        {
            // 若发布时已被消费，只有消费方可以确定结果；不能把它改成失败/取消。
            if (Interlocked.CompareExchange(ref state, 2, 0) == 0) tcs.TrySetResult(fallback);
            Observe("queue_exception", error: ex);
        }
        return AnimusForge.Refactor.Runtime.PendingOperationRegistry.AwaitRelease(
            AwaitNativeConversationMainThreadFuncAsync(tcs.Task, op, target, targetAgentIndex, Retire), registration);
    }
private static async Task<T> AwaitNativeConversationMainThreadFuncAsync<T>(Task<T> task, string operationName, string targetLog, int targetAgentIndex, Func<bool> tryExpire)
    {
        using (CancellationTokenSource timeout = new CancellationTokenSource())
        {
            try
            {
                Task completed = await Task.WhenAny(task,
                    Task.Delay(NativeConversationMainThreadPreprocessTimeoutMs, timeout.Token)).ConfigureAwait(false);
                if (completed != task && tryExpire())
                {
                    try
                    {
                        string detail = "target=" + targetLog + " agent=" + targetAgentIndex + " timeoutMs=" + NativeConversationMainThreadPreprocessTimeoutMs;
                        Logger.Log("ShoutBehavior", "[NativeConversation] " + operationName + " main-thread queue expired before start " + detail);
                        FreezeWatchdog.Mark("NativeConversation." + operationName + "_mainthread_timeout", detail, immediate: true);
                    }
                    catch (Exception)
                    {
                        // Only diagnostic failure is ignored; operation exceptions propagate below.
                    }
                }
                // 已开始不代表可取消；等待真实结果，避免 fallback 后游戏状态又被迟到操作修改。
                return await task.ConfigureAwait(false);
            }
            finally { timeout.Cancel(); }
        }
    }
}
