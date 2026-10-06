using System;
using AnimusForge.SiegeAftermathIntervention;

internal static class SiegeNativeMovementOrdersTests
{
    public static void Run(Action<bool, string> check)
    {
        var orders = new SiegeNativeMovementOrders();
        SiegeNativeMovementOrderDecision Request(float time, bool eligible = true, bool nativeTarget = true,
            float x = 20f, string purpose = "gather", int flags = 0) =>
            orders.Request(1, eligible, purpose, time, x, 2f, 5f, flags, nativeTarget);
        check(Request(0) == SiegeNativeMovementOrderDecision.IssueNativeOrder, "first valid movement starts native navigation");
        check(Request(0.9f) == SiegeNativeMovementOrderDecision.KeepNativeOrder,
            "a single old stall interval does not reset native navigation");
        check(Request(20) == SiegeNativeMovementOrderDecision.KeepNativeOrder,
            "order deduplication retains native targets while recovery requires separate stall evidence");
        check(Request(21, nativeTarget: false) == SiegeNativeMovementOrderDecision.IssueNativeOrder,
            "native target loss permits a normal navigation retry");
        check(Request(21.1f, nativeTarget: false) == SiegeNativeMovementOrderDecision.KeepNativeOrder,
            "missing target retries are throttled rather than issued every tick");
        check(Request(22.3f, x: 30f) == SiegeNativeMovementOrderDecision.IssueNativeOrder,
            "changed target follows native navigation after the refresh interval");
        check(Request(22.4f, x: 30f, purpose: "stop") == SiegeNativeMovementOrderDecision.IssueNativeOrder,
            "a new stop purpose does not wait for the previous command throttle");
        check(Request(22.5f, x: 30f, purpose: "stop", flags: 1) == SiegeNativeMovementOrderDecision.IssueNativeOrder,
            "changed action flags replace the prior order");
        check(Request(23, eligible: false) == SiegeNativeMovementOrderDecision.Reject,
            "inactive paused conversation or foreign-controlled actor cannot acquire movement");
        check(Request(23.1f) == SiegeNativeMovementOrderDecision.IssueNativeOrder,
            "returning eligibility does not inherit suspended command state");
        check(Request(24, x: float.NaN) == SiegeNativeMovementOrderDecision.Reject,
            "nonfinite target is not forwarded to native navigation");
        check(Request(float.PositiveInfinity) == SiegeNativeMovementOrderDecision.Reject,
            "nonfinite mission time cannot create an order");
        Request(30); orders.Reset();
        check(Request(30.1f) == SiegeNativeMovementOrderDecision.IssueNativeOrder, "scene exit clears movement order state");
        orders.Forget(1);
        check(Request(30.2f) == SiegeNativeMovementOrderDecision.IssueNativeOrder, "removed agent identity can be reused safely");
        check(Request(0.1f) == SiegeNativeMovementOrderDecision.IssueNativeOrder, "reset mission clock does not keep stale throttles");
        check(orders.Request(-1, true, "gather", 0, 0, 0, 0, 0, false) == SiegeNativeMovementOrderDecision.Reject,
            "invalid agent index cannot acquire movement");
    }
}
