# Memory summary main-thread boundary tests

```powershell
python tools/MemorySummaryMainThreadBoundaryTests/run.py
python tools/MemorySummaryMainThreadBoundaryTests/run.py --original
python tools/MemorySummaryMainThreadBoundaryTests/run.py --mutate ignore-generation
```

The runtime harness compiles the production `MyBehavior.MemorySummaryMainThread.cs`
boundary with a small Campaign fixture. It covers direct and queued execution, bounded
drain, owner/Campaign/generation rejection, reset completion, wrong-thread drains and
exceptions. The old-source control reads the exact `e40c92d7` method and must fail;
mutation controls must also fail at runtime. Source checks keep `ProcessMemorySummaryQueueAsync` result application,
failure marking, queue cleanup and final processing release behind the boundary.

This is an offline concurrency/ownership replay. It does not call a provider, load a
save, or prove Bannerlord live acceptance.
