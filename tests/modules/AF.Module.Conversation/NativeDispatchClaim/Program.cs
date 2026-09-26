using System;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge;

internal static class Program
{
    private static int _checks;
    private static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException("FAIL " + name);
        _checks++;
        Console.WriteLine("PASS " + name);
    }
    private static TaskCompletionSource<bool> Gate() => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    private static async Task Main()
    {
        Check((int)NativeConversationDispatchState.Queued == 0 && (int)NativeConversationDispatchState.Started == 1
            && (int)NativeConversationDispatchState.ExpiredBeforeStart == 2, "original_numeric_states_preserved");
        var first = new NativeConversationDispatchClaim();
        Check(first.TryStart(), "default_claim_can_start");
        Check(!first.TryStart(), "duplicate_callback_cannot_start");
        Check(!first.TryExpireBeforeStart(), "started_operation_cannot_expire");
        Check(!first.TryStart(), "timeout_cannot_reset_started_claim");
        var expired = new NativeConversationDispatchClaim();
        Check(expired.TryExpireBeforeStart(), "queued_operation_can_expire");
        Check(!expired.TryExpireBeforeStart(), "duplicate_expiry_cannot_settle_again");
        Check(!expired.TryStart(), "expired_callback_cannot_run_late");

        var claimed = new NativeConversationDispatchClaim();
        var began = Gate(); var release = Gate(); int effects = 0;
        Task<int> operation = Task.Run(async () =>
        {
            if (!claimed.TryStart()) return -1;
            Interlocked.Increment(ref effects);
            began.SetResult(true);
            await release.Task.ConfigureAwait(false);
            return 17;
        });
        await began.Task.ConfigureAwait(false);
        Check(!operation.IsCompleted && !claimed.TryExpireBeforeStart(), "yielded_claim_keeps_actual_outcome_authority");
        Check(!claimed.TryStart(), "published_duplicate_cannot_repeat_effect");
        release.SetResult(true);
        Check(await operation.ConfigureAwait(false) == 17 && effects == 1, "actual_result_arrives_once_after_timeout_attempt");

        // All contenders use the same captured value, exactly as host enqueue/timeout closures do.
        var contested = new NativeConversationDispatchClaim();
        var ready = Gate(); int starts = 0, expiries = 0;
        var competitors = new Task[32];
        for (int i=0; i<competitors.Length; i++)
        {
            bool start = i % 2 == 0;
            competitors[i] = Task.Run(async () =>
            {
                await ready.Task.ConfigureAwait(false);
                if (start) { if (contested.TryStart()) Interlocked.Increment(ref starts); }
                else if (contested.TryExpireBeforeStart()) Interlocked.Increment(ref expiries);
            });
        }
        ready.SetResult(true);
        await Task.WhenAll(competitors).ConfigureAwait(false);
        Check(starts + expiries == 1, "single_atomic_winner_across_callback_and_retirement");
        Check(!contested.TryStart() && !contested.TryExpireBeforeStart(), "terminal_claim_never_reopens");
        Console.WriteLine($"NativeDispatchClaim PASS={_checks} FAIL=0 actualStruct=true forcedYield=true noClockSleep=true LIVE=NOT_RUN");
    }
}
