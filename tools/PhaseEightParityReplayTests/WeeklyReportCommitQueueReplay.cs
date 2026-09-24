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
        Console.WriteLine("PASS WeeklyReportCommitRecordStateReplay absent/edit/winner/materials/retry-admission live=NOT_RUN");
    }

    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException("WeeklyReportCommitQueueReplay: " + name);
    }
}
