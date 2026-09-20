using AnimusForge;

int passed = 0;
int failed = 0;

void Case(string name, Action body)
{
    try
    {
        body();
        passed++;
        Console.WriteLine("PASS " + name);
    }
    catch (Exception exception)
    {
        failed++;
        Console.WriteLine("FAIL " + name + ": " + exception.Message);
    }
}

void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

Case("one-worker-lease-and-fifo", () =>
{
    SceneSpeechQueueOwner<string> owner = new SceneSpeechQueueOwner<string>();
    Require(owner.EnqueueAndTryStartWorker("first"), "first enqueue did not own the worker start");
    Require(!owner.EnqueueAndTryStartWorker("second"), "second enqueue started a duplicate worker");
    Require(owner.TryDequeueOrStopWorker(out string first) && first == "first", "first item/order changed");
    Require(owner.TryDequeueOrStopWorker(out string second) && second == "second", "second item/order changed");
});

Case("empty-dequeue-retires-worker", () =>
{
    SceneSpeechQueueOwner<string> owner = new SceneSpeechQueueOwner<string>();
    Require(owner.EnqueueAndTryStartWorker("one"), "worker did not start");
    Require(owner.TryDequeueOrStopWorker(out _), "queued item missing");
    Require(!owner.TryDequeueOrStopWorker(out _), "empty queue returned an item");
    Require(!owner.HasQueuedOrWorker(), "empty worker lease was not retired");
    Require(owner.EnqueueAndTryStartWorker("two"), "later enqueue could not start a new worker");
});

Case("clear-queued-preserves-worker-lease", () =>
{
    SceneSpeechQueueOwner<string> owner = new SceneSpeechQueueOwner<string>();
    owner.EnqueueAndTryStartWorker("one");
    owner.EnqueueAndTryStartWorker("two");
    owner.ClearQueued();
    Require(owner.HasQueuedOrWorker(), "conversation clear cancelled the active worker lease");
    Require(!owner.TryDequeueOrStopWorker(out _), "cleared item was replayed");
    Require(!owner.HasQueuedOrWorker(), "empty worker did not retire after clear");
});

Case("reset-clears-queue-and-worker", () =>
{
    SceneSpeechQueueOwner<string> owner = new SceneSpeechQueueOwner<string>();
    owner.EnqueueAndTryStartWorker("one");
    owner.EnqueueAndTryStartWorker("two");
    owner.Reset();
    Require(!owner.HasQueuedOrWorker(), "reset retained queue or worker state");
    Require(owner.EnqueueAndTryStartWorker("new"), "reset did not release worker ownership");
});

Case("snapshot-reports-current-state", () =>
{
    SceneSpeechQueueOwner<string> owner = new SceneSpeechQueueOwner<string>();
    owner.EnqueueAndTryStartWorker("one");
    owner.EnqueueAndTryStartWorker("two");
    Require(owner.TryGetSnapshot(out int count, out bool running), "uncontended snapshot failed");
    Require(count == 2 && running, "snapshot did not report queue/worker state");
});

Case("concurrent-enqueue-has-one-starter", () =>
{
    SceneSpeechQueueOwner<string> owner = new SceneSpeechQueueOwner<string>();
    int starters = 0;
    Parallel.For(0, 64, index =>
    {
        if (owner.EnqueueAndTryStartWorker(index.ToString()))
        {
            Interlocked.Increment(ref starters);
        }
    });
    Require(starters == 1, "concurrent enqueue created " + starters + " worker starters");
    int drained = 0;
    while (owner.TryDequeueOrStopWorker(out _))
    {
        drained++;
    }
    Require(drained == 64 && !owner.HasQueuedOrWorker(), "concurrent queue lost items or worker state");
});

Console.WriteLine($"SceneSpeechQueueOwner cases={passed + failed} PASS={passed} FAIL={failed}");
return failed == 0 ? 0 : 1;
