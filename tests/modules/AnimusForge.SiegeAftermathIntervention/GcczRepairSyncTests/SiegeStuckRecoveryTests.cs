using System;
using AnimusForge.SiegeAftermathIntervention;

internal static class SiegeStuckRecoveryTests
{
    public static void Run(Action<bool, string> check)
    {
        var tracker = new SiegeStuckRecovery();
        var order = new SiegeNativeMovementOrders.Order("gather", 0, 20, 0, 0, 0);
        SiegeStuckRecoveryDecision At(float t, bool eligible = true, float x = 0) => tracker.Observe(1, eligible, t, x, 0, 0, order);
        int retries = 0, hops = 0;
        for (float t = 0; t <= 7; t += 0.5f)
        {
            var decision = At(t);
            if (decision == SiegeStuckRecoveryDecision.RetryNativePath) { retries++; tracker.RecordNativeRetry(1, true); }
            if (decision == SiegeStuckRecoveryDecision.TryShortHop) { hops++; check(t >= 7, "recovery never triggers before seven continuous seconds"); }
        }
        check(retries == 1 && hops == 1, "one native retry precedes a seven-second short-hop request");
        tracker.Suspend(1);
        for (float t = 7.5f; t < 17; t += 0.5f)
        {
            var decision = At(t);
            check(decision != SiegeStuckRecoveryDecision.TryShortHop, "recovery cooldown survives suspension");
        }
        check(At(17) == SiegeStuckRecoveryDecision.RetryNativePath, "cooldown expiration still retries native navigation first");
        tracker.RecordNativeRetry(1, true);
        check(At(17.5f) != SiegeStuckRecoveryDecision.TryShortHop, "a fresh native retry receives time before another hop");
        tracker.Reset(); At(0);
        check(At(7) == SiegeStuckRecoveryDecision.None, "one sparse seven-second sample does not prove continuous blockage");
        tracker.Reset();
        bool movedHop = false;
        for (float t = 0; t <= 10; t += 0.5f)
            movedHop |= At(t, x: t * 0.1f) == SiegeStuckRecoveryDecision.TryShortHop;
        check(!movedHop, "slow continuous walking never becomes a stall");
        tracker.Reset();
        for (float t = 0; t <= 6; t += 0.5f)
        { if (At(t) == SiegeStuckRecoveryDecision.RetryNativePath) tracker.RecordNativeRetry(1, true); }
        At(6.5f, eligible: false);
        check(At(7) == SiegeStuckRecoveryDecision.None, "pause conversation injury or lost command resets the evidence");
        tracker.Reset();
        for (float t = 0; t <= 8; t += 0.5f)
            check(At(t) != SiegeStuckRecoveryDecision.TryShortHop, "unacknowledged native retry cannot authorize a teleport");
        tracker.Reset();
        for (float t = 0; t <= 6; t += 0.5f)
        { if (At(t) == SiegeStuckRecoveryDecision.RetryNativePath) tracker.RecordNativeRetry(1, true); }
        order = new SiegeNativeMovementOrders.Order("retreat", 6.5f, 30, 0, 0, 0);
        check(At(6.5f) == SiegeStuckRecoveryDecision.None && At(7) == SiegeStuckRecoveryDecision.None,
            "changed command or target requires a fresh seven-second interval");
        tracker.Reset();
        check(At(0, x: 29.5f) == SiegeStuckRecoveryDecision.None && At(7, x: 29.5f) == SiegeStuckRecoveryDecision.None,
            "arrived actors are not eligible for recovery");
        check(!SiegeStuckRecovery.IsShortHopGeometryValid(100, 0), "distant destination cannot become a recovery hop");
        check(!SiegeStuckRecovery.IsShortHopGeometryValid(1, 2), "recovery cannot jump between floors");
        check(!SiegeStuckRecovery.IsShortHopGeometryValid(float.NaN, 0), "nonfinite landing coordinates are rejected");
        check(SiegeStuckRecovery.IsShortHopGeometryValid(4, 0.3f), "nearby same-floor landing geometry is accepted");
        tracker.Reset(); At(0); tracker.Forget(1);
        check(At(0.5f) == SiegeStuckRecoveryDecision.None, "agent removal discards previous stall evidence");
    }
}
