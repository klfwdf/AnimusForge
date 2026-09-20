using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Illustrator.Core;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Illustrator.Engine
{
    // One capture owns the slot. Only OnApplicationTick advances it, once per frame;
    // each callback retains its own native-operation/time budget. No Scene.Tick.
    internal sealed class PanoramaBatchPump
    {
        private static PanoramaBatchPump _active;
        private readonly CancellationToken _token;
        private readonly Stopwatch _elapsed = Stopwatch.StartNew();
        private readonly TaskCompletionSource<bool> _done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private Func<bool> _batch;
        private int _batches;
        private double _workMilliseconds, _maxBatchMilliseconds;
        private bool _complete;

        private PanoramaBatchPump(Func<bool> batch, CancellationToken token)
        { _batch = batch; _token = token; }

        internal Task Completion => _done.Task;

        internal static PanoramaBatchPump Start(Func<bool> batch, CancellationToken token)
        {
            IllustratorRuntime.AssertMainThread();
            token.ThrowIfCancellationRequested();
            if (batch == null) throw new ArgumentNullException(nameof(batch));
            if (_active != null) throw new InvalidOperationException("已有环境快照批次正在执行。");
            return _active = new PanoramaBatchPump(batch, token);
        }

        internal static void Tick()
        {
            IllustratorRuntime.AssertMainThread();
            var pump = _active;
            if (pump == null) return;
            try
            {
                pump._token.ThrowIfCancellationRequested();
                long started = Stopwatch.GetTimestamp();
                bool complete;
                try { complete = pump._batch(); }
                finally
                {
                    double ms = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
                    pump._batches++;
                    pump._workMilliseconds += ms;
                    pump._maxBatchMilliseconds = Math.Max(pump._maxBatchMilliseconds, ms);
                }
                // Publish completion only after the native batch has returned. Cleanup
                // must never race a running batch when a timer cancels from another thread.
                pump._token.ThrowIfCancellationRequested();
                if (complete) pump.Finish(null, false);
            }
            catch (OperationCanceledException) { pump.Finish(null, true); }
            catch (Exception ex) { pump.Finish(ex, false); }
        }

        // Reset/Shutdown must wake the waiter even if no further application tick runs.
        // Both this call and Tick run on the game thread, before native snapshot disposal.
        internal static void CancelActive()
        {
            IllustratorRuntime.AssertMainThread();
            _active?.Finish(null, true);
        }

        private void Finish(Exception error, bool cancelled)
        {
            if (!ReferenceEquals(_active, this)) return;
            _active = null;
            _batch = null;
            _elapsed.Stop();
            _complete = error == null && !cancelled;
            if (cancelled) _done.TrySetCanceled();
            else if (error != null) _done.TrySetException(error);
            else _done.TrySetResult(true);
        }

        // Read on the worker only after Completion, including exceptional completion.
        internal JObject Describe(string phase) => new JObject
        {
            ["phase"] = phase, ["scheduler"] = "once_per_application_tick", ["complete"] = _complete,
            ["batches"] = _batches, ["elapsedMs"] = _elapsed.Elapsed.TotalMilliseconds,
            ["workMs"] = _workMilliseconds, ["maxBatchMs"] = _maxBatchMilliseconds,
            ["betweenBatchesMs"] = Math.Max(0, _elapsed.Elapsed.TotalMilliseconds - _workMilliseconds)
        };
    }
}
