using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

internal static class WeeklyReportWaveCoordinationReplay
{
    internal static void Run(Type owner) => RunAsync(owner).GetAwaiter().GetResult();

    private static TaskCompletionSource<T> Gate<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException("weekly wave: " + name);
    }

    private static async Task RunAsync(Type owner)
    {
        MethodInfo run = owner.GetMethod("RunAsync", BindingFlags.Static | BindingFlags.NonPublic)
            .MakeGenericMethod(typeof(string), typeof(string));
        Task<string[]> Start(string[] batches, int burst, Func<string, bool> current,
            Func<List<string>, int, int, int, Task<List<Task<string>>>> launch, Func<int, Task> delay)
            => (Task<string[]>)run.Invoke(null, new object[] {
                batches, burst, (Func<string, bool>)(batch => batch != null), current, launch,
                (Func<string, int, string>)((batch, index) => "unsent:" + index + ":" + batch), delay });

        // Keep every provider request pending while advancing the actual coordinator
        // through two asynchronous minute boundaries. Clock/provider are explicit ports.
        var requests = Enumerable.Range(0, 5).Select(_ => Gate<string>()).ToArray();
        var delays = new[] { Gate<bool>(), Gate<bool>() };
        var enteredDelay = new[] { Gate<int>(), Gate<int>() };
        var lastWave = Gate<bool>();
        var seen = new List<string>();
        int delayCount = 0;
        Task<string[]> all = Start(new[] { "a", "b", "c", "d", "e" }, 2, _ => true,
            (wave, first, index, total) => {
                seen.Add($"{first}/{index}/{total}:" + string.Join(",", wave));
                if (index == 3) lastWave.TrySetResult(true);
                return Task.FromResult(wave.Select((_, offset) => requests[first + offset].Task).ToList());
            }, ms => {
                int index = delayCount++;
                enteredDelay[index].TrySetResult(ms);
                return delays[index].Task;
            });
        Check(await enteredDelay[0].Task.WaitAsync(TimeSpan.FromSeconds(5)) == 60000
            && seen.SequenceEqual(new[] { "0/1/3:a,b" }) && !all.IsCompleted,
            "first wave starts before any result and requests exactly 60000ms");
        delays[0].SetResult(true);
        Check(await enteredDelay[1].Task.WaitAsync(TimeSpan.FromSeconds(5)) == 60000
            && !requests[0].Task.IsCompleted && seen.Count == 2,
            "second wave overlaps unresolved first-wave requests after the delay");
        delays[1].SetResult(true);
        await lastWave.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Check(seen.SequenceEqual(new[] { "0/1/3:a,b", "2/2/3:c,d", "4/3/3:e" })
            && !all.IsCompleted && delayCount == 2, "last partial wave has stable indexes and no trailing delay");
        for (int i = 4; i >= 0; i--) requests[i].SetResult("done:" + i);
        Check((await all.WaitAsync(TimeSpan.FromSeconds(5))).SequenceEqual(
            Enumerable.Range(0, 5).Select(i => "done:" + i)), "completion order cannot reorder batch settlement");

        // Rejection of a later queued wave retains earlier in-flight results, and
        // creates exactly one failure per unsent batch without another minute wait.
        var firstResult = Gate<string>();
        int launches = 0, rejectedDelays = 0;
        Task<string[]> rejected = Start(new[] { "a", "b", "c", "d" }, 1, _ => true,
            (_, first, index, total) => Task.FromResult(++launches == 1
                ? new List<Task<string>> { firstResult.Task } : null),
            _ => { rejectedDelays++; return Task.CompletedTask; });
        Check(!rejected.IsCompleted && launches == 2 && rejectedDelays == 1,
            "source rejection stops later launches but awaits already sent work");
        firstResult.SetResult("accepted:a");
        Check((await rejected.WaitAsync(TimeSpan.FromSeconds(5))).SequenceEqual(
            new[] { "accepted:a", "unsent:1:b", "unsent:2:c", "unsent:3:d" }),
            "source rejection preserves partial success and all unsent failures");

        // Cancellation is checked at every asynchronous boundary; none of these
        // cases may produce a commit-ready result, even after successful responses.
        foreach (string canceledPhase in new[] { "weekly_report_before_wave", "weekly_report_after_wave_launch",
            "weekly_report_after_wave_delay", "weekly_report_before_commit_enqueue" })
        {
            int starts = 0;
            string[] result = await Start(new[] { "a", "b" }, 1, phase => phase != canceledPhase,
                (wave, first, index, total) => {
                    starts++;
                    return Task.FromResult(new List<Task<string>> { Task.FromResult(wave[0]) });
                }, _ => Task.CompletedTask);
            Check(result == null && starts == (canceledPhase == "weekly_report_before_wave" ? 0
                : canceledPhase == "weekly_report_before_commit_enqueue" ? 2 : 1),
                "no commit or extra launch after cancellation at " + canceledPhase);
        }
        bool current = true;
        var delayGate = Gate<bool>();
        int delayedLaunches = 0;
        Task<string[]> retired = Start(new[] { "a", "b" }, 1, _ => current,
            (wave, first, index, total) => {
                delayedLaunches++;
                return Task.FromResult(new List<Task<string>> { Task.FromResult(wave[0]) });
            }, _ => delayGate.Task);
        current = false;
        delayGate.SetResult(true);
        Check(await retired.WaitAsync(TimeSpan.FromSeconds(5)) == null && delayedLaunches == 1,
            "owner/load change during a real asynchronous delay cannot launch the next wave");

        current = true;
        var late = Gate<string>();
        Task<string[]> lateCompletion = Start(new[] { "a" }, 1, _ => current,
            (_, first, index, total) => Task.FromResult(new List<Task<string>> { late.Task }),
            _ => throw new InvalidOperationException("single wave must not delay"));
        current = false;
        late.SetResult("old response");
        Check(await lateCompletion.WaitAsync(TimeSpan.FromSeconds(5)) == null,
            "owner/load change while awaiting provider results cannot reach commit");

        int filteredLaunches = 0;
        string[] filtered = await Start(new string[] { null, "b" }, 0, _ => true,
            (wave, first, index, total) => {
                filteredLaunches++;
                Check(first == 1 && index == 2 && total == 2 && wave.Single() == "b", "filter preserves original slot indexes");
                return Task.FromResult(new List<Task<string>> { Task.FromResult("b") });
            }, _ => throw new InvalidOperationException("empty or final wave must not delay"));
        Check(filteredLaunches == 1 && filtered.SequenceEqual(new[] { "b" }), "invalid burst clamps to one without launching empty waves");
        Check((await Start(Array.Empty<string>(), 1, _ => true,
            (_, first, index, total) => throw new InvalidOperationException("empty launch"),
            _ => throw new InvalidOperationException("empty delay"))).Length == 0, "empty run completes without launch");
        Console.WriteLine("PASS WeeklyReportWaveCoordinationReplay three-wave-overlap/indexes/60000ms-port/partial-rejection/async-retirement/late-result/filter; clock-provider-fixture live=NOT_RUN");
    }
}
