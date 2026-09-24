using System;
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
        Console.WriteLine("PASS WeeklyReportCommitQueueReplay FIFO/non-head/reset-waiters/retired-context live=NOT_RUN");
    }

    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException("WeeklyReportCommitQueueReplay: " + name);
    }
}
