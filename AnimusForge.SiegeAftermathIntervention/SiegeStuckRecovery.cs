using System;
using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

public enum SiegeStuckRecoveryDecision { None, RetryNativePath, TryShortHop }

/// <summary>
/// Mission-scoped continuous movement evidence. Scene geometry and native effects stay in the adapter.
/// Pauses and sampling gaps invalidate evidence but do not bypass the recovery cooldown.
/// </summary>
public sealed class SiegeStuckRecovery
{
    public const float SampleSeconds = 0.5f;
    public const float MaximumSampleGapSeconds = 1.5f;
    public const float StuckSeconds = 7f;
    public const float NativeRetrySeconds = 3.5f;
    public const float CooldownSeconds = 10f;
    public const float ProgressDistance = 0.3f;
    public const float ArrivalDistance = 1f;
    public const float MinimumHopDistance = 0.8f;
    public const float MaximumHopDistance = 2.5f;
    public const float MaximumHeightDifference = 0.75f;
    public const int LandingSamples = 12;
    private const int MinimumSamples = 8;
    private readonly Dictionary<int, Probe> _probes = new Dictionary<int, Probe>();

    public SiegeStuckRecoveryDecision Observe(int agentIndex, bool eligible, float time,
        float x, float y, float z, SiegeNativeMovementOrders.Order order)
    {
        if (!eligible || agentIndex < 0 || !Finite(time) || time < 0 || !Finite(x) || !Finite(y) || !Finite(z)
            || !Finite(order.X) || !Finite(order.Y) || !Finite(order.Z) || string.IsNullOrWhiteSpace(order.Purpose)
            || DistanceSquared(x, y, z, order.X, order.Y, order.Z) <= ArrivalDistance * ArrivalDistance)
        {
            Suspend(agentIndex);
            return SiegeStuckRecoveryDecision.None;
        }
        if (!_probes.TryGetValue(agentIndex, out Probe probe))
        {
            probe = new Probe();
            _probes.Add(agentIndex, probe);
        }
        if (!probe.Active || time < probe.LastTime || time - probe.LastTime > MaximumSampleGapSeconds
            || probe.Order.Purpose != order.Purpose || probe.Order.Flags != order.Flags
            || DistanceSquared(order.X, order.Y, order.Z, probe.Order.X, probe.Order.Y, probe.Order.Z)
                > SiegeNativeMovementOrders.TargetChangeDistance * SiegeNativeMovementOrders.TargetChangeDistance
            || DistanceSquared(x, y, z, probe.X, probe.Y, probe.Z) >= ProgressDistance * ProgressDistance)
        {
            ResetProbe(probe, time, x, y, z, order);
            return SiegeStuckRecoveryDecision.None;
        }
        if (time - probe.LastTime < SampleSeconds) return SiegeStuckRecoveryDecision.None;
        probe.LastTime = time;
        probe.Samples++;
        if (time < probe.NextRecoveryTime) return SiegeStuckRecoveryDecision.None;
        float stalled = time - probe.StartTime;
        if (!probe.NativeRetryRequested && stalled >= NativeRetrySeconds)
        {
            probe.NativeRetryRequested = true;
            probe.NativeRetryTime = time;
            return SiegeStuckRecoveryDecision.RetryNativePath;
        }
        if (stalled < StuckSeconds || probe.Samples < MinimumSamples || !probe.NativeRetrySucceeded
            || time - probe.NativeRetryTime < StuckSeconds - NativeRetrySeconds)
            return SiegeStuckRecoveryDecision.None;
        probe.NextRecoveryTime = time + CooldownSeconds;
        probe.Active = false;
        return SiegeStuckRecoveryDecision.TryShortHop;
    }

    public void RecordNativeRetry(int agentIndex, bool succeeded)
    {
        if (!_probes.TryGetValue(agentIndex, out Probe probe) || !probe.NativeRetryRequested) return;
        probe.NativeRetrySucceeded = succeeded;
        if (!succeeded) probe.Active = false;
    }

    public void Suspend(int agentIndex)
    {
        if (_probes.TryGetValue(agentIndex, out Probe probe)) probe.Active = false;
    }

    public void Forget(int agentIndex) => _probes.Remove(agentIndex);
    public void Reset() => _probes.Clear();

    public static bool IsShortHopGeometryValid(float horizontalDistanceSquared, float heightDifference)
    {
        return Finite(horizontalDistanceSquared) && Finite(heightDifference)
            && horizontalDistanceSquared >= MinimumHopDistance * MinimumHopDistance
            && horizontalDistanceSquared <= MaximumHopDistance * MaximumHopDistance
            && Math.Abs(heightDifference) <= MaximumHeightDifference;
    }

    private static void ResetProbe(Probe probe, float time, float x, float y, float z, SiegeNativeMovementOrders.Order order)
    {
        probe.Active = true;
        probe.StartTime = probe.LastTime = time;
        probe.X = x; probe.Y = y; probe.Z = z; probe.Order = order;
        probe.Samples = 1;
        probe.NativeRetryRequested = probe.NativeRetrySucceeded = false;
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static float DistanceSquared(float x, float y, float z, float tx, float ty, float tz)
    { float dx = x - tx, dy = y - ty, dz = z - tz; return dx * dx + dy * dy + dz * dz; }

    private sealed class Probe
    {
        public bool Active, NativeRetryRequested, NativeRetrySucceeded;
        public float StartTime, LastTime, NextRecoveryTime, NativeRetryTime, X, Y, Z;
        public int Samples;
        public SiegeNativeMovementOrders.Order Order;
    }
}
