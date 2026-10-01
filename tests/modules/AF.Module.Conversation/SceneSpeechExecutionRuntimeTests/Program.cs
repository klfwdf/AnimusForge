using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge;

internal static class Program
{
    private sealed class Payload
    {
        internal readonly TaskCompletionSource<bool> Completion = new TaskCompletionSource<bool>();
        internal int Effects;
        internal int Rejections;
    }

    private sealed class Rig
    {
        internal bool Processing;
        internal bool FailPublication;
        internal bool FailPost;
        internal readonly ConcurrentQueue<Action> Posted = new ConcurrentQueue<Action>();
        internal readonly ConcurrentQueue<TaskCompletionSource<bool>> Delays = new ConcurrentQueue<TaskCompletionSource<bool>>();
        internal readonly SceneSpeechExecutionRuntime<Payload> Runtime;
        internal Rig()
        {
            Runtime = new SceneSpeechExecutionRuntime<Payload>(() => Volatile.Read(ref Processing),
                callback => { if (FailPost) throw new InvalidOperationException("post failed"); Posted.Enqueue(callback); },
                item => { item.Effects++; if (FailPublication) throw new InvalidOperationException("partial effect"); item.Completion.TrySetResult(true); },
                item => { item.Rejections++; item.Completion.TrySetResult(false); },
                milliseconds => { var delay = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); Delays.Enqueue(delay); return delay.Task; });
        }
    }

    private static void Require(bool value, string reason)
    { if (!value) throw new InvalidOperationException(reason); }

    private static async Task Until(Func<bool> ready)
    {
        for (int i = 0; i < 3000; i++) { if (ready()) return; await Task.Delay(1); }
        throw new InvalidOperationException("bounded test wait expired");
    }

    private static async Task<Action> Posted(Rig rig)
    { await Until(() => !rig.Posted.IsEmpty); Require(rig.Posted.TryDequeue(out Action action), "missing dispatch"); return action; }

    private static async Task ReleaseDelay(Rig rig)
    { await Until(() => !rig.Delays.IsEmpty); Require(rig.Delays.TryDequeue(out var delay), "missing delay"); delay.SetResult(true); }

    private static async Task Main()
    {
        int passed = 0;
        async Task Case(string name, Func<Task> test)
        { await test(); passed++; Console.WriteLine("PASS " + name); }

        await Case("duplicate-dispatch-executes-and-completes-once", async () =>
        {
            var rig = new Rig(); var item = new Payload(); rig.Runtime.Enqueue(item);
            Action publish = await Posted(rig); publish(); publish();
            Require(item.Effects == 1 && await item.Completion.Task && item.Rejections == 0, "dispatch replayed");
            rig.Runtime.Reset(); await ReleaseDelay(rig);
        });
        await Case("reset-retires-dispatched-payload-even-if-main-queue-is-discarded", async () =>
        {
            var rig = new Rig(); var item = new Payload(); rig.Runtime.Enqueue(item);
            Action stale = await Posted(rig); rig.Runtime.Reset();
            Require(!await item.Completion.Task, "reset orphaned completion"); stale(); stale();
            Require(item.Effects == 0 && item.Rejections == 1, "late callback crossed mission generation");
            await ReleaseDelay(rig);
        });
        await Case("old-worker-cannot-drain-or-stop-new-generation", async () =>
        {
            var queue = new SceneSpeechQueueOwner<Payload>(); var old = new Payload(); var fresh = new Payload();
            Require(queue.EnqueueAndTryStartWorker(old, out long prior), "old lease"); queue.Reset();
            Require(queue.EnqueueAndTryStartWorker(fresh, out long current), "new lease");
            queue.StopWorker(prior);
            Require(!queue.TryDequeueOrStopWorker(prior, out _) && queue.HasQueuedOrWorker(), "old lease changed new worker");
            Require(queue.TryDequeueOrStopWorker(current, out var item) && ReferenceEquals(item, fresh), "new payload lost");
            await Task.CompletedTask;
        });
        await Case("enqueue-during-processing-remains-fifo", async () =>
        {
            var rig = new Rig { Processing = true }; var first = new Payload(); var second = new Payload();
            rig.Runtime.Enqueue(first); rig.Runtime.Enqueue(second); await Until(() => !rig.Delays.IsEmpty);
            Require(rig.Posted.IsEmpty, "published during processing"); Volatile.Write(ref rig.Processing, false);
            await ReleaseDelay(rig); (await Posted(rig))(); Require(first.Effects == 1 && second.Effects == 0, "primary-first changed");
            await ReleaseDelay(rig); (await Posted(rig))(); Require(second.Effects == 1, "relay lost");
            rig.Runtime.Reset(); await ReleaseDelay(rig);
        });
        await Case("nontransactional-failure-does-not-retry-effects", async () =>
        {
            var rig = new Rig { FailPublication = true }; var item = new Payload(); rig.Runtime.Enqueue(item);
            Action publish = await Posted(rig); publish(); publish();
            Require(item.Effects == 1 && item.Rejections == 1 && !await item.Completion.Task, "partial effect retried or accepted");
            rig.Runtime.Reset(); await ReleaseDelay(rig);
        });
        await Case("post-failure-rejects-once-and-releases-worker", async () =>
        {
            var rig = new Rig { FailPost = true }; var item = new Payload(); rig.Runtime.Enqueue(item);
            await Until(() => item.Completion.Task.IsCompleted);
            Require(!await item.Completion.Task && item.Rejections == 1 && item.Effects == 0, "post failure outcome");
            Require(!rig.Runtime.HasQueuedOrWorker(), "post failure orphaned lease");
            rig.Runtime.Reset(); Require(item.Rejections == 1, "post failure retained dispatch");
        });
        await Case("clear-queued-does-not-replay-already-dispatched-payload", async () =>
        {
            var rig = new Rig(); var first = new Payload(); var second = new Payload(); rig.Runtime.Enqueue(first);
            Action publish = await Posted(rig); rig.Runtime.Enqueue(second);
            Payload[] dropped = rig.Runtime.ClearQueued(); Require(dropped.Length == 1 && ReferenceEquals(dropped[0], second), "clear queue changed lease");
            publish(); Require(await first.Completion.Task && second.Effects == 0, "clear changed accepted dispatch");
            rig.Runtime.Reset(); await ReleaseDelay(rig);
        });
        Console.WriteLine($"SceneSpeechExecutionRuntime PASS={passed} FAIL=0");
    }
}
