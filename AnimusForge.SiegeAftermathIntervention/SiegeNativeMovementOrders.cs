using System;
using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

public enum SiegeNativeMovementOrderDecision
{
    Reject,
    KeepNativeOrder,
    IssueNativeOrder,
}

/// <summary>
/// Mission-local order deduplication only. Native navigation owns movement and obstacle avoidance.
/// Order deduplication does not authorize recovery; a separate continuous-stall check owns that decision.
/// </summary>
public sealed class SiegeNativeMovementOrders
{
    public const float RefreshSeconds = 1.25f;
    public const float TargetChangeDistance = 0.6f;
    private readonly Dictionary<int, Order> _orders = new Dictionary<int, Order>();

    public SiegeNativeMovementOrderDecision Request(int agentIndex, bool eligible, string purpose,
        float missionTime, float x, float y, float z, int flags, bool hasMatchingNativeTarget)
    {
        if (!eligible || agentIndex < 0 || string.IsNullOrWhiteSpace(purpose)
            || !IsFinite(missionTime) || missionTime < 0 || !IsFinite(x) || !IsFinite(y) || !IsFinite(z))
        {
            _orders.Remove(agentIndex);
            return SiegeNativeMovementOrderDecision.Reject;
        }
        if (_orders.TryGetValue(agentIndex, out Order previous)
            && previous.Purpose == purpose && previous.Flags == flags && missionTime >= previous.Time)
        {
            float dx = x - previous.X, dy = y - previous.Y, dz = z - previous.Z;
            bool unchanged = dx * dx + dy * dy + dz * dz <= TargetChangeDistance * TargetChangeDistance;
            if ((unchanged && hasMatchingNativeTarget) || missionTime - previous.Time < RefreshSeconds)
                return SiegeNativeMovementOrderDecision.KeepNativeOrder;
        }
        _orders[agentIndex] = new Order(purpose, missionTime, x, y, z, flags);
        return SiegeNativeMovementOrderDecision.IssueNativeOrder;
    }

    public int Count => _orders.Count;
    public bool TryGetOrder(int agentIndex, out Order order) => _orders.TryGetValue(agentIndex, out order);

    public void Forget(int agentIndex) => _orders.Remove(agentIndex);
    public void Reset() => _orders.Clear();
    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    public readonly struct Order
    {
        public Order(string purpose, float time, float x, float y, float z, int flags)
        { Purpose = purpose; Time = time; X = x; Y = y; Z = z; Flags = flags; }
        public string Purpose { get; }
        public float Time { get; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public int Flags { get; }
    }
}
