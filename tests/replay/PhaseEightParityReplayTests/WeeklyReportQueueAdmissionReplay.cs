using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

internal static class WeeklyReportQueueAdmissionReplay
{
    internal static void Run(Type openOwner)
    {
        const BindingFlags members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        Type ownerType = openOwner.MakeGenericType(typeof(TaskCompletionSource<string>), typeof(string));
        Action<TaskCompletionSource<string>, string> complete = (waiter, result) => waiter.TrySetResult(result);
        object owner = Activator.CreateInstance(ownerType, members, null,
            new object[] { complete, (Func<string>)(() => "canceled") }, null);
        bool Admit(TaskCompletionSource<string> waiter, Func<bool> current) =>
            (bool)ownerType.GetMethod("EnqueueIfCurrent", members).Invoke(owner, new object[] { waiter, current });
        void Reset() => ownerType.GetMethod("CancelAll", members).Invoke(owner, null);
        bool Pending() => (bool)ownerType.GetProperty("HasPending", members).GetValue(owner);
        TaskCompletionSource<string> Waiter() => new(TaskCreationOptions.RunContinuationsAsynchronously);

        var before = Waiter();
        Check(Admit(before, () => true) && Pending() && !before.Task.IsCompleted, "current admission awaits pump");
        Reset();
        Check(before.Task.IsCompletedSuccessfully && before.Task.Result == "canceled" && !Pending(),
            "admission before reset is drained and settled");
        var after = Waiter();
        Check(!Admit(after, () => false) && after.Task.IsCompletedSuccessfully
            && after.Task.Result == "canceled" && !Pending(), "late stale admission settles without a future pump");
        var newer = Waiter();
        Check(Admit(newer, () => true) && !newer.Task.IsCompleted && Pending(), "reset does not permanently close new-generation admission");
        ownerType.GetMethod("CompleteProcessed", members).Invoke(owner, new object[] { before });
        Check(Pending() && !newer.Task.IsCompleted, "old completion cannot dequeue the new generation");
        Reset();

        // Force load's generation advance between producer admission and reset.
        // Neither participant may leave an old waiter behind or wait for a tick.
        using var inAdmission = new ManualResetEventSlim();
        using var changedGeneration = new ManualResetEventSlim();
        int generation = 1;
        var raced = Waiter();
        Task<bool> producer = Task.Run(() => Admit(raced, () => {
            inAdmission.Set();
            if (!changedGeneration.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("generation advance");
            return Volatile.Read(ref generation) == 1;
        }));
        if (!inAdmission.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("admission entry");
        Task reset = Task.Run(() => {
            Volatile.Write(ref generation, 2);
            changedGeneration.Set();
            Reset();
        });
        Task.WhenAll(producer, reset).WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        Check(!producer.Result && raced.Task.IsCompletedSuccessfully && !Pending(),
            "generation advance racing admission cannot orphan a pending context");
        Console.WriteLine("PASS WeeklyReportQueueAdmissionReplay before-reset/after-reset/new-generation/forced-admission-race; live=NOT_RUN");
    }

    private static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException("weekly queue admission: " + name);
    }
}
