using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

internal static class WeeklyReportOwnerReplay
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags StaticMembers = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal static void Run(Assembly af)
    {
        Type ownerType = af.GetType("AnimusForge.MyBehavior", true);
        Type guardType = af.GetType("AnimusForge.SaveRuntimeGuard", true);
        FieldInfo instanceField = ownerType.GetField("<Instance>k__BackingField", StaticMembers);
        FieldInfo generationField = guardType.GetField("_generation", StaticMembers);
        object previousOwner = instanceField.GetValue(null);
        long previousGeneration = (long)generationField.GetValue(null);
        object owner = Activator.CreateInstance(ownerType);
        MethodInfo enqueue = ownerType.GetMethod("QueueWeeklyFullReportCompletionAsync", Members);
        MethodInfo process = ownerType.GetMethod("ProcessWeeklyFullReportCompletions", Members);
        MethodInfo cancel = ownerType.GetMethod("CancelWeeklyFullReportCompletions", Members);

        Task<bool> Queue(long generation, Func<bool> apply)
            => (Task<bool>)enqueue.Invoke(owner, new object[] { generation, apply });
        void Pump() => process.Invoke(owner, null);

        try
        {
            int applied = 0;
            int applyThread = 0;
            int ownerThread = Thread.CurrentThread.ManagedThreadId;
            Task<bool> first = Task.Factory.StartNew(() => Queue(previousGeneration, () =>
            {
                applyThread = Thread.CurrentThread.ManagedThreadId;
                applied++;
                return true;
            }), CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).GetAwaiter().GetResult();
            Check(!first.IsCompleted && applied == 0, "worker only queues; no live commit before engine pump");
            Pump();
            Check(first.GetAwaiter().GetResult() && applied == 1 && applyThread == ownerThread,
                "queued result executes on engine pump thread");
            Pump();
            Check(applied == 1, "pumping twice never replays accepted effects");

            Task<bool> second = Queue(previousGeneration, () => { applied++; return true; });
            Task<bool> third = Queue(previousGeneration, () => { applied++; return true; });
            Task<bool> fourth = Queue(previousGeneration, () => { applied++; return true; });
            Pump();
            Check(second.IsCompleted && third.IsCompleted && !fourth.IsCompleted && applied == 3,
                "engine pump commits at most two on-demand requests");
            Pump();
            Check(fourth.GetAwaiter().GetResult() && applied == 4, "bounded pump preserves remaining result");

            Task<bool> stale = Queue(previousGeneration, () => { applied++; return true; });
            guardType.GetMethod("AdvanceGeneration", StaticMembers).Invoke(null, new object[] { "weekly_owner_replay" });
            Pump();
            Check(!stale.GetAwaiter().GetResult() && applied == 4, "read-load boundary rejects already queued old-save result");
            Check(!Queue(previousGeneration, () => { applied++; return true; }).GetAwaiter().GetResult(),
                "old network reply cannot enqueue after generation change");

            long currentGeneration = (long)generationField.GetValue(null);
            Task<bool> abandoned = Queue(currentGeneration, () => { applied++; return true; });
            cancel.Invoke(owner, null);
            Check(!abandoned.GetAwaiter().GetResult(), "reset completes abandoned awaiter rather than hanging it");
            Pump();
            Check(applied == 4, "cancelled queued result has no late effects");

            Task<bool> failure = Queue(currentGeneration, () => throw new InvalidOperationException("fixture commit failure"));
            Pump();
            Check(failure.IsFaulted && failure.Exception.InnerException.Message == "fixture commit failure",
                "commit failure propagates to awaiting owner failure handler");

            Activator.CreateInstance(ownerType);
            Check(!Queue(currentGeneration, () => { applied++; return true; }).GetAwaiter().GetResult(),
                "retired behavior cannot accept completion for the new owner");
            Console.WriteLine("PASS WeeklyReportOwnerReplay queued-main-thread/bounded/idempotent/stale-before-after/reset-await/exception/retired-owner live=NOT_RUN");
        }
        finally
        {
            cancel.Invoke(owner, null);
            instanceField.SetValue(null, previousOwner);
            generationField.SetValue(null, previousGeneration);
        }
    }

    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException("WeeklyReportOwnerReplay: " + name);
    }
}
