using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

internal static class WeeklyReportCommitQueueReplay
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal static void Run(Assembly af)
    {
        Type ownerType = af.GetType("AnimusForge.WeeklyReportCommitQueueOwner`2", true)
            .MakeGenericType(typeof(string), typeof(string));
        var waiters = new Dictionary<string, TaskCompletionSource<string>>();
        Action<string, string> settle = (context, result) => waiters[context].TrySetResult(result);
        Func<string> canceled = () => "canceled";
        object owner = Activator.CreateInstance(ownerType, Members, null, new object[] { settle, canceled }, null);
        string Context(string name, out Task<string> task)
        {
            var waiter = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            waiters[name] = waiter;
            task = waiter.Task;
            return name;
        }
        bool Pending() => (bool)ownerType.GetProperty("HasPending", Members).GetValue(owner);
        object Peek() => ownerType.GetMethod("Peek", Members).Invoke(owner, null);
        void Enqueue(object context) => ownerType.GetMethod("Enqueue", Members).Invoke(owner, new[] { context });
        void Processed(object context) => ownerType.GetMethod("CompleteProcessed", Members).Invoke(owner, new[] { context });
        void Cancel() => ownerType.GetMethod("CancelAll", Members).Invoke(owner, null);

        Check(!Pending() && Peek() == null, "empty fast path");
        string first = Context("first", out Task<string> firstTask), second = Context("second", out Task<string> secondTask);
        Enqueue(first); Enqueue(second);
        Check(Pending() && ReferenceEquals(Peek(), first), "FIFO head");
        Processed(second);
        Check(ReferenceEquals(Peek(), first), "non-head completion cannot remove head");
        ownerType.GetMethod("Complete", Members).Invoke(owner, new object[] { first, "applied" });
        Processed(first);
        Check(firstTask.IsCompletedSuccessfully && firstTask.Result == "applied" && Equals(Peek(), second),
            "settled head advances FIFO");
        Cancel();
        Check(secondTask.IsCompletedSuccessfully && secondTask.Result == "canceled" && !Pending() && Peek() == null,
            "reset settles abandoned waiter and clears flag");

        string newer = Context("newer", out Task<string> newerTask);
        Enqueue(newer);
        Processed(second);
        Check(Pending() && Equals(Peek(), newer) && !newerTask.IsCompleted,
            "retired context cannot release new request");
        Cancel();
        Check(newerTask.IsCompletedSuccessfully && newerTask.Result == "canceled" && !Pending(),
            "second reset settles new waiter");
        RunRecordStateReplay(af);
        RunBatchApiLaunchReplay(af);
        RunCommitTargetOwnerReplay(af);
        RunPartialCommitReplay(af);
        Console.WriteLine("PASS WeeklyReportCommitQueueReplay FIFO/non-head/reset-waiters/retired-context/batch-api-waiter-cancel-stale/partial-missing-recovery/rpm-metadata/retry-clear/real-partial-commit live=NOT_RUN");
    }

    private static void RunPartialCommitReplay(Assembly af)
    {
        Type behavior = af.GetType("AnimusForge.MyBehavior", true);
        Type groupType = behavior.GetNestedType("WeeklyEventMaterialPreviewGroup", BindingFlags.NonPublic);
        object world = Activator.CreateInstance(groupType, true);
        groupType.GetField("GroupKind", Members).SetValue(world, "world");
        object kingdom = Activator.CreateInstance(groupType, true);
        groupType.GetField("GroupKind", Members).SetValue(kingdom, "kingdom");
        groupType.GetField("KingdomId", Members).SetValue(kingdom, "k1");
        Type batchType = behavior.GetNestedType("WeeklyReportBatchRequest", BindingFlags.NonPublic);
        object worldBatch = Activator.CreateInstance(batchType, true);
        ((IList)batchType.GetField("Groups", Members).GetValue(worldBatch)).Add(world);
        object kingdomBatch = Activator.CreateInstance(batchType, true);
        ((IList)batchType.GetField("Groups", Members).GetValue(kingdomBatch)).Add(kingdom);
        Type batchResultType = behavior.GetNestedType("WeeklyReportBatchRequestResult", BindingFlags.NonPublic);
        object worldResult = Activator.CreateInstance(batchResultType, true);
        batchResultType.GetField("Success", Members).SetValue(worldResult, true);
        Type blockType = behavior.GetNestedType("WeeklyReportBatchBlockResult", BindingFlags.NonPublic);
        object worldBlock = Activator.CreateInstance(blockType, true);
        blockType.GetField("ReportId", Members).SetValue(worldBlock, "world");
        blockType.GetField("Parsed", Members).SetValue(worldBlock, true);
        ((IList)batchResultType.GetField("Blocks", Members).GetValue(worldResult)).Add(worldBlock);
        object kingdomResult = Activator.CreateInstance(batchResultType, true);
        batchResultType.GetField("MissingReportIds", Members).SetValue(kingdomResult, new List<string> { "kingdom:k1" });
        batchResultType.GetField("FailureReason", Members).SetValue(kingdomResult, "RPM limited");
        batchResultType.GetField("AttemptsUsed", Members).SetValue(kingdomResult, 3);
        batchResultType.GetField("IsRequestsPerMinuteLimit", Members).SetValue(kingdomResult, true);
        Type executionType = behavior.GetNestedType("WeeklyReportBatchExecutionResult", BindingFlags.NonPublic);
        object Execution(int index, object batch, object result)
        {
            object execution = Activator.CreateInstance(executionType, true);
            executionType.GetField("BatchIndex", Members).SetValue(execution, index);
            executionType.GetField("Batch", Members).SetValue(execution, batch);
            executionType.GetField("Result", Members).SetValue(execution, result);
            return execution;
        }
        Type contextType = behavior.GetNestedType("PendingWeeklyReportCommitContext", BindingFlags.NonPublic);
        object context = Activator.CreateInstance(contextType, true);
        contextType.GetField("WeekIndex", Members).SetValue(context, 1);
        contextType.GetField("StartDay", Members).SetValue(context, 0);
        contextType.GetField("EndDay", Members).SetValue(context, 6);
        contextType.GetField("DisplayLabel", Members).SetValue(context, "fixture week");
        ((IList)contextType.GetField("Groups", Members).GetValue(context)).Add(world);
        ((IList)contextType.GetField("Groups", Members).GetValue(context)).Add(kingdom);
        ((IList)contextType.GetField("Executions", Members).GetValue(context)).Add(Execution(0, worldBatch, worldResult));
        ((IList)contextType.GetField("Executions", Members).GetValue(context)).Add(Execution(1, kingdomBatch, kingdomResult));
        IDictionary groupMap = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(string), groupType));
        groupMap.Add("world", world);
        groupMap.Add("kingdom:k1", kingdom);
        contextType.GetField("GroupMap", Members).SetValue(context, groupMap);
        contextType.GetField("CapturedRecordStates", Members).SetValue(context,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["world"] = null, ["kingdom:k1"] = null });
        Type generationType = behavior.GetNestedType("WeeklyReportGenerationResult", BindingFlags.NonPublic);
        Type completionType = typeof(TaskCompletionSource<>).MakeGenericType(generationType);
        object completion = Activator.CreateInstance(completionType, TaskCreationOptions.RunContinuationsAsynchronously);
        contextType.GetField("CompletionSource", Members).SetValue(context, completion);
        Type queueType = af.GetType("AnimusForge.WeeklyReportCommitQueueOwner`2", true).MakeGenericType(contextType, generationType);
        Type completeType = typeof(Action<,>).MakeGenericType(contextType, generationType);
        Delegate complete = Delegate.CreateDelegate(completeType, behavior.GetMethod("CompletePendingWeeklyReportCommit", Members));
        Delegate canceled = Expression.Lambda(typeof(Func<>).MakeGenericType(generationType), Expression.Constant(null, generationType)).Compile();
        object queue = Activator.CreateInstance(queueType, Members, null, new object[] { complete, canceled }, null);
        object host = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(behavior);
        behavior.GetField("_weeklyReportCommitQueue", Members).SetValue(host, queue);
        Type revisionsType = af.GetType("AnimusForge.WeeklyReportMaterialRevisionOwner", true);
        object revisions = Activator.CreateInstance(revisionsType, true);
        behavior.GetField("_weeklyReportMaterialRevisions", Members).SetValue(host, revisions);
        contextType.GetField("SourceSnapshot", Members).SetValue(context,
            revisionsType.GetMethod("Capture", Members).Invoke(revisions, new object[] { 0, 6 }));
        Type entryType = behavior.GetNestedType("EventRecordEntry", BindingFlags.NonPublic);
        object existingWorld = Activator.CreateInstance(entryType, true);
        entryType.GetField("EventId", Members).SetValue(existingWorld, "weekly_report:world:1:");
        entryType.GetField("EventKind", Members).SetValue(existingWorld, "world");
        entryType.GetField("WeekIndex", Members).SetValue(existingWorld, 1);
        entryType.GetField("Summary", Members).SetValue(existingWorld, "already published");
        IList records = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(entryType));
        records.Add(existingWorld);
        behavior.GetField("_eventRecordEntries", Members).SetValue(host, records);
        FieldInfo activeOwner = behavior.GetField("<Instance>k__BackingField", Members);
        object previousOwner = activeOwner.GetValue(null);
        activeOwner.SetValue(null, host);
        try
        {
            bool processed = (bool)behavior.GetMethod("ProcessPendingWeeklyReportCommitContext", Members).Invoke(host,
                new object[] { context, Stopwatch.GetTimestamp(), 1000.0 });
            Task task = (Task)completionType.GetProperty("Task", Members).GetValue(completion);
            Check(processed && task.IsCompletedSuccessfully, "real pending commit settles mixed winner/failure context");
            object generation = task.GetType().GetProperty("Result", Members).GetValue(task);
            object retry = generationType.GetField("RetryContext", Members).GetValue(generation);
            IList failedGroups = (IList)retry?.GetType().GetField("Groups", Members).GetValue(retry);
            Check((int)generationType.GetField("SuccessCount", Members).GetValue(generation) == 1
                && (int)generationType.GetField("FailureCount", Members).GetValue(generation) == 1
                && (bool)generationType.GetField("BlockedByFatalFailure", Members).GetValue(generation)
                && failedGroups?.Count == 1 && ReferenceEquals(failedGroups[0], kingdom)
                && (bool)retry.GetType().GetField("IsRequestsPerMinuteLimit", Members).GetValue(retry)
                && !(bool)retry.GetType().GetField("RequiresFreshMaterials", Members).GetValue(retry)
                && (string)entryType.GetField("Summary", Members).GetValue(existingWorld) == "already published"
                && behavior.GetField("_unreadWeeklyReportNoticeEventIds", Members).GetValue(host) == null,
                "accepted winner remains untouched while only failed kingdom enters RPM recovery without duplicate notice");
        }
        finally
        {
            activeOwner.SetValue(null, previousOwner);
        }
    }

    private static void RunCommitTargetOwnerReplay(Assembly af)
    {
        Type type = af.GetType("AnimusForge.WeeklyReportCommitTargetOwner`1", true).MakeGenericType(typeof(string));
        object owner = Activator.CreateInstance(type, true);
        MethodInfo missing = type.GetMethod("RecordMissing", Members);
        MethodInfo settle = type.GetMethod("Settle", Members);
        MethodInfo isSettled = type.GetMethod("IsSettled", Members);
        int PendingCount()
        {
            int count = 0;
            foreach (object _ in (IEnumerable)type.GetProperty("PendingMissingTargets", Members).GetValue(owner)) count++;
            return count;
        }
        Check((bool)missing.Invoke(owner, new object[] { "world:1", "world", "first failure" }), "first partial miss recorded");
        Check(!(bool)missing.Invoke(owner, new object[] { "WORLD:1", "world", "duplicate" }) && PendingCount() == 1,
            "duplicate missing ID across batches is not counted twice");
        Check((bool)settle.Invoke(owner, new object[] { "World:1" }) && PendingCount() == 0,
            "later parsed success removes prior missing result");
        Check(!(bool)missing.Invoke(owner, new object[] { "world:1", "world", "late miss" })
            && (bool)isSettled.Invoke(owner, new object[] { "world:1" }),
            "late missing result cannot undo settled success");
        Check((bool)missing.Invoke(owner, new object[] { "kingdom:1", "kingdom", "unrecovered" }) && PendingCount() == 1,
            "unrecovered target remains pending for final failure");

        Type behavior = af.GetType("AnimusForge.MyBehavior", true);
        Type groupType = behavior.GetNestedType("WeeklyEventMaterialPreviewGroup", BindingFlags.NonPublic);
        object world = Activator.CreateInstance(groupType, true);
        groupType.GetField("GroupKind", Members).SetValue(world, "world");
        Type batchType = behavior.GetNestedType("WeeklyReportBatchRequest", BindingFlags.NonPublic);
        object batch = Activator.CreateInstance(batchType, true);
        batchType.GetField("DisplayLabel", Members).SetValue(batch, "world batch");
        ((IList)batchType.GetField("Groups", Members).GetValue(batch)).Add(world);
        Type resultType = behavior.GetNestedType("WeeklyReportBatchRequestResult", BindingFlags.NonPublic);
        object result = Activator.CreateInstance(resultType, true);
        resultType.GetField("MissingReportIds", Members).SetValue(result, new List<string> { "world", "WORLD" });
        Type contextType = behavior.GetNestedType("PendingWeeklyReportCommitContext", BindingFlags.NonPublic);
        object context = Activator.CreateInstance(contextType, true);
        IDictionary map = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(string), groupType));
        map.Add("world", world);
        contextType.GetField("GroupMap", Members).SetValue(context, map);
        object host = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(behavior);
        MethodInfo finalizeBatch = behavior.GetMethod("FinalizePendingWeeklyReportCommitBatch", Members);
        finalizeBatch.Invoke(host, new[] { context, batch, result });
        finalizeBatch.Invoke(host, new[] { context, batch, result });
        object targets = contextType.GetField("Targets", Members).GetValue(context);
        int hostPending = 0;
        foreach (object _ in (IEnumerable)targets.GetType().GetProperty("PendingMissingTargets", Members).GetValue(targets)) hostPending++;
        Check(hostPending == 1 && (int)contextType.GetField("FailureCount", Members).GetValue(context) == 0,
            "real batch finalizer defers and deduplicates repeated missing IDs");
        targets.GetType().GetMethod("Settle", Members).Invoke(targets, new object[] { "world" });
        hostPending = 0;
        foreach (object _ in (IEnumerable)targets.GetType().GetProperty("PendingMissingTargets", Members).GetValue(targets)) hostPending++;
        Check(hostPending == 0, "later parsed block recovers real finalizer's pending miss");

        resultType.GetField("AttemptsUsed", Members).SetValue(result, 3);
        resultType.GetField("IsRateLimit", Members).SetValue(result, true);
        resultType.GetField("IsRequestsPerMinuteLimit", Members).SetValue(result, true);
        resultType.GetField("RetryAfterSeconds", Members).SetValue(result, (int?)17);
        Type executionType = behavior.GetNestedType("WeeklyReportBatchExecutionResult", BindingFlags.NonPublic);
        object execution = Activator.CreateInstance(executionType, true);
        executionType.GetField("Batch", Members).SetValue(execution, batch);
        executionType.GetField("Result", Members).SetValue(execution, result);
        object otherGroup = Activator.CreateInstance(groupType, true);
        groupType.GetField("GroupKind", Members).SetValue(otherGroup, "kingdom");
        groupType.GetField("KingdomId", Members).SetValue(otherGroup, "k1");
        object otherBatch = Activator.CreateInstance(batchType, true);
        ((IList)batchType.GetField("Groups", Members).GetValue(otherBatch)).Add(otherGroup);
        object otherResult = Activator.CreateInstance(resultType, true);
        resultType.GetField("MissingReportIds", Members).SetValue(otherResult, new List<string> { "kingdom:k1" });
        resultType.GetField("IsQuotaLimit", Members).SetValue(otherResult, true);
        object otherExecution = Activator.CreateInstance(executionType, true);
        executionType.GetField("Batch", Members).SetValue(otherExecution, otherBatch);
        executionType.GetField("Result", Members).SetValue(otherExecution, otherResult);
        IList executions = (IList)contextType.GetField("Executions", Members).GetValue(context);
        executions.Add(otherExecution);
        executions.Add(execution);
        ((List<string>)contextType.GetField("FailureMessages", Members).GetValue(context)).Add("world batch failed");
        object failedRequest = behavior.GetMethod("BuildWeeklyReportFailedRequest", Members).Invoke(null, new[] { context, world });
        Type requestType = failedRequest.GetType();
        Check((int)requestType.GetField("AttemptsUsed", Members).GetValue(failedRequest) == 3
            && (bool)requestType.GetField("IsRequestsPerMinuteLimit", Members).GetValue(failedRequest)
            && !(bool)requestType.GetField("IsQuotaLimit", Members).GetValue(failedRequest)
            && (int?)requestType.GetField("RetryAfterSeconds", Members).GetValue(failedRequest) == 17,
            "real failed target retains its own batch RPM and Retry-After metadata, not another target's quota classification");
        object retryContext = behavior.GetMethod("CreateWeeklyReportRetryContext", Members).Invoke(null,
            new object[] { batchType.GetField("Groups", Members).GetValue(batch), 1, 0, 6, "week", false, true, world, failedRequest, null, null, null });
        Check((bool)retryContext.GetType().GetField("IsRequestsPerMinuteLimit", Members).GetValue(retryContext),
            "RPM recovery option receives the matched failed batch classification");

        Type apiType = behavior.GetNestedType("ApiCallResult", BindingFlags.NonPublic);
        object api = Activator.CreateInstance(apiType, true);
        apiType.GetField("IsRateLimit", Members).SetValue(api, true);
        apiType.GetField("IsRequestsPerMinuteLimit", Members).SetValue(api, true);
        apiType.GetField("RetryAfterSeconds", Members).SetValue(api, (int?)17);
        MethodInfo captureMetadata = behavior.GetMethod("CaptureWeeklyReportBatchAttemptFailureMetadata", Members);
        captureMetadata.Invoke(null, new[] { result, api });
        apiType.GetField("Success", Members).SetValue(api, true);
        captureMetadata.Invoke(null, new[] { result, api });
        Check(!(bool)resultType.GetField("IsRateLimit", Members).GetValue(result)
            && !(bool)resultType.GetField("IsRequestsPerMinuteLimit", Members).GetValue(result)
            && resultType.GetField("RetryAfterSeconds", Members).GetValue(result) == null,
            "later HTTP success clears a previous retry's RPM classification");
    }

    private static void RunBatchApiLaunchReplay(Assembly af)
    {
        Type behavior = af.GetType("AnimusForge.MyBehavior", true);
        Type contextType = behavior.GetNestedType("PendingWeeklyBatchApiAttemptContext", BindingFlags.NonPublic);
        Type resultType = behavior.GetNestedType("ApiCallResult", BindingFlags.NonPublic);
        Type attemptType = typeof(Task<>).MakeGenericType(resultType);
        Type queueType = af.GetType("AnimusForge.WeeklyReportCommitQueueOwner`2", true).MakeGenericType(contextType, attemptType);
        Type completeType = typeof(Action<,>).MakeGenericType(contextType, attemptType);
        Type canceledType = typeof(Func<>).MakeGenericType(attemptType);
        Delegate complete = Delegate.CreateDelegate(completeType, behavior.GetMethod("CompletePendingWeeklyBatchApiAttempt", Members));
        Delegate canceled = Expression.Lambda(canceledType, Expression.Constant(null, attemptType)).Compile();
        object queue = Activator.CreateInstance(queueType, Members, null, new object[] { complete, canceled }, null);
        object host = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(behavior);
        behavior.GetField("_weeklyBatchApiAttemptQueue", Members).SetValue(host, queue);
        FieldInfo activeOwner = behavior.GetField("<Instance>k__BackingField", Members);
        object previousOwner = activeOwner.GetValue(null);
        Type guard = af.GetType("AnimusForge.SaveRuntimeGuard", true);
        long generation = (long)guard.GetMethod("CaptureGeneration", Members).Invoke(null, null);
        MethodInfo launch = behavior.GetMethod("CallWeeklyReportBatchApiAttemptAsync", Members);
        string staleText = (string)guard.GetMethod("BuildStaleRequestErrorText", Members).Invoke(null, null);
        activeOwner.SetValue(null, host);
        try
        {
            Task pending = Task.Factory.StartNew(
                () => (Task)launch.Invoke(host, new object[] { "system", "user", generation, false }),
                TaskCreationOptions.LongRunning).GetAwaiter().GetResult();
            Check((bool)queueType.GetProperty("HasPending", Members).GetValue(queue) && !pending.IsCompleted,
                "background retry waits for campaign tick instead of reading API settings");
            queueType.GetMethod("CancelAll", Members).Invoke(queue, null);
            pending.GetAwaiter().GetResult();
            object canceledResult = pending.GetType().GetProperty("Result").GetValue(pending);
            Check((string)resultType.GetField("ErrorMessage", Members).GetValue(canceledResult) == staleText
                && !(bool)queueType.GetProperty("HasPending", Members).GetValue(queue),
                "reset settles queued API attempt without starting a request");
            Task rejected = (Task)launch.Invoke(host, new object[] { "system", "user", generation + 1L, false });
            rejected.GetAwaiter().GetResult();
            object staleResult = rejected.GetType().GetProperty("Result").GetValue(rejected);
            Check((string)resultType.GetField("ErrorMessage", Members).GetValue(staleResult) == staleText
                && !(bool)queueType.GetProperty("HasPending", Members).GetValue(queue),
                "stale attempt is refused before queuing or reading settings");
        }
        finally
        {
            activeOwner.SetValue(null, previousOwner);
        }
        Task retired = (Task)launch.Invoke(host, new object[] { "system", "user", generation, false });
        retired.GetAwaiter().GetResult();
        object retiredResult = retired.GetType().GetProperty("Result").GetValue(retired);
        Check((string)resultType.GetField("ErrorMessage", Members).GetValue(retiredResult) == staleText
            && !(bool)queueType.GetProperty("HasPending", Members).GetValue(queue),
            "retired owner refuses attempt without waiting for its former tick");
    }

    private static void RunRecordStateReplay(Assembly af)
    {
        Type behavior = af.GetType("AnimusForge.MyBehavior", true);
        Type entryType = behavior.GetNestedType("EventRecordEntry", BindingFlags.NonPublic);
        var state = behavior.GetMethod("BuildWeeklyReportCommitRecordState", Members);
        object entry = Activator.CreateInstance(entryType, true);
        string Read() => (string)state.Invoke(null, new[] { entry });
        Check(state.Invoke(null, new object[] { null }) == null, "absent target sentinel");
        entryType.GetField("EventId", Members).SetValue(entry, "weekly_report:world:1:");
        entryType.GetField("Title", Members).SetValue(entry, "original");
        string captured = Read();
        Check(captured == Read(), "unchanged target retains state");
        entryType.GetField("Title", Members).SetValue(entry, "user edit");
        Check(captured != Read(), "edited target invalidates old response");
        entryType.GetField("Title", Members).SetValue(entry, "original");
        entryType.GetField("Summary", Members).SetValue(entry, "other completed winner");
        Check(captured != Read(), "newly completed winner invalidates old response");
        entryType.GetField("Summary", Members).SetValue(entry, null);
        IList materials = (IList)entryType.GetField("Materials", Members).GetValue(entry);
        object material = Activator.CreateInstance(af.GetType("AnimusForge.MyBehavior+EventMaterialReference", true), true);
        material.GetType().GetField("SnapshotText", Members).SetValue(material, "new source");
        materials.Add(material);
        Check(captured != Read(), "edited saved source materials invalidate old response");
        Type groupType = behavior.GetNestedType("WeeklyEventMaterialPreviewGroup", BindingFlags.NonPublic);
        IDictionary groups = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(string), groupType));
        groups.Add("world", null);
        var admission = behavior.GetMethod("AreWeeklyReportCommitRecordStatesCurrent", Members);
        var expected = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["world"] = captured };
        var current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["world"] = captured };
        bool Allowed() => (bool)admission.Invoke(null, new object[] { groups, expected, current });
        Check(Allowed(), "same target state permits explicit retry");
        current["world"] = Read();
        Check(!Allowed(), "changed saved source refuses retry before request");
        current.Clear();
        Check(!Allowed(), "missing current state refuses retry");
        current["world"] = captured; expected.Clear();
        Check(!Allowed(), "missing original state refuses retry");
        object group = Activator.CreateInstance(groupType, true);
        groupType.GetField("GroupKind", Members).SetValue(group, "world");
        entryType.GetField("WeekIndex", Members).SetValue(entry, 1);
        entryType.GetField("EventKind", Members).SetValue(entry, "world");
        entryType.GetField("Summary", Members).SetValue(entry, "other completed winner");
        var winner = behavior.GetMethod("IsWeeklyReportCommitWinner", Members);
        bool Won() => (bool)winner.Invoke(null, new[] { entry, group, (object)1 });
        Check(Won(), "completed full report wins without old overwrite");
        entryType.GetField("Summary", Members).SetValue(entry, "");
        Check(!Won(), "edited but incomplete full report is not a winner");
        Type mode = behavior.GetNestedType("WeeklyReportOutputMode", BindingFlags.NonPublic);
        groupType.GetField("OutputMode", Members).SetValue(group, Enum.Parse(mode, "TitleShortTagsOnly"));
        entryType.GetField("ShortSummary", Members).SetValue(entry, "saved short report");
        Check(Won(), "completed short report wins without requiring full body");
        entryType.GetField("WeekIndex", Members).SetValue(entry, 2);
        Check(!Won(), "wrong week cannot win");
        Type listType = typeof(List<>).MakeGenericType(groupType);
        IList fresh = (IList)Activator.CreateInstance(listType);
        IList failed = (IList)Activator.CreateInstance(listType);
        object freshWorld = Activator.CreateInstance(groupType, true);
        groupType.GetField("GroupKind", Members).SetValue(freshWorld, "world");
        object freshKingdom = Activator.CreateInstance(groupType, true);
        groupType.GetField("GroupKind", Members).SetValue(freshKingdom, "kingdom");
        groupType.GetField("KingdomId", Members).SetValue(freshKingdom, "k1");
        fresh.Add(freshWorld); fresh.Add(freshKingdom); failed.Add(group);
        var selectFresh = behavior.GetMethod("SelectFreshWeeklyReportRetryGroups", Members);
        IList Selected() => (IList)selectFresh.Invoke(null, new object[] { fresh, failed });
        Check(Selected().Count == 1 && ReferenceEquals(Selected()[0], freshWorld),
            "fresh retry targets only failed identity, not previously completed groups");
        object missing = Activator.CreateInstance(groupType, true);
        groupType.GetField("GroupKind", Members).SetValue(missing, "kingdom");
        groupType.GetField("KingdomId", Members).SetValue(missing, "missing");
        failed.Add(missing);
        Check(Selected() == null, "missing failed group refuses partial fresh retry");
        Type batchType = behavior.GetNestedType("WeeklyReportBatchRequest", BindingFlags.NonPublic);
        object unpreparedBatch = Activator.CreateInstance(batchType, true);
        object uninitializedHost = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(behavior);
        Task rejected = (Task)behavior.GetMethod("GenerateWeeklyReportBatchWithRetriesAsync", Members)
            .Invoke(uninitializedHost, new[] { unpreparedBatch, (object)3, (object)0L });
        rejected.GetAwaiter().GetResult();
        object rejection = rejected.GetType().GetProperty("Result").GetValue(rejected);
        Check(!(bool)rejection.GetType().GetField("Success", Members).GetValue(rejection)
            && ((string)rejection.GetType().GetField("FailureReason", Members).GetValue(rejection)).Contains("not prepared"),
            "worker refuses unprepared prompt before API or live-state access");
        batchType.GetField("SystemPrompt", Members).SetValue(unpreparedBatch, "system");
        batchType.GetField("UserPrompt", Members).SetValue(unpreparedBatch, "user");
        Type guardType = af.GetType("AnimusForge.SaveRuntimeGuard", true);
        long staleGeneration = (long)guardType.GetMethod("CaptureGeneration", Members).Invoke(null, null) + 1L;
        Task stale = (Task)behavior.GetMethod("GenerateWeeklyReportBatchWithRetriesAsync", Members)
            .Invoke(uninitializedHost, new[] { unpreparedBatch, (object)3, (object)staleGeneration });
        stale.GetAwaiter().GetResult();
        object staleResult = stale.GetType().GetProperty("Result").GetValue(stale);
        Check(!(bool)staleResult.GetType().GetField("Success", Members).GetValue(staleResult)
            && ((string)staleResult.GetType().GetField("FailureReason", Members).GetValue(staleResult)).Contains("读档失效"),
            "stale generation refuses next retry before API");
        Console.WriteLine("PASS WeeklyReportBatchPromptWorkerReplay unprepared/stale-rejected-before-network live=NOT_RUN");
        Console.WriteLine("PASS WeeklyReportCommitRecordStateReplay absent/edit/winner/materials/retry-admission/full-short-winner/fresh-targets live=NOT_RUN");
    }

    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException("WeeklyReportCommitQueueReplay: " + name);
    }
}
