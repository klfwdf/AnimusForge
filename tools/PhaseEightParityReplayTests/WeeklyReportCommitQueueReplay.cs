using System;
using System.Collections;
using System.Collections.Generic;
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
        Console.WriteLine("PASS WeeklyReportCommitQueueReplay FIFO/non-head/reset-waiters/retired-context live=NOT_RUN");
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
