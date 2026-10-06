using System;
using TaleWorlds.Library;

namespace RichExecutions.Scene;

internal static class ExecutionCrowdRoutePolicy
{
    internal static bool IsOutwardTarget(Vec2 start, Vec2 end, Vec2 center, float radius)
    {
        var from = start - center;
        var to = end - center;
        var delta = end - start;
        return to.LengthSquared >= radius * radius && delta.LengthSquared >= 9f
            && to.LengthSquared > from.LengthSquared
            && Vec2.DotProduct(from, delta) >= 0f;
    }

    // A spectator starting inside the exclusion radius may only leave outward.
    // Once outside, no navigation segment may cut back through the ceremony.
    internal static bool IsSafeSegment(Vec2 start, Vec2 end, Vec2 center, float radius)
    {
        var from = start - center;
        var delta = end - start;
        if (from.LengthSquared < radius * radius)
            return Vec2.DotProduct(from, delta) >= -0.001f
                && (end - center).LengthSquared >= from.LengthSquared - 0.001f;
        float length = delta.LengthSquared;
        float t = length <= 0.0001f ? 0f : Math.Max(0f, Math.Min(1f, -Vec2.DotProduct(from, delta) / length));
        return (from + delta * t).LengthSquared >= radius * radius - 0.001f;
    }
}
