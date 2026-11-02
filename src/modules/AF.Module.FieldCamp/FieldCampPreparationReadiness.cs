using System;
namespace AnimusForge;
// The real safety boundary is cleared physical space, not every optional audience animation mark.
internal static class FieldCampPreparationReadiness
{
    internal const float OptionalSpectatorWaitSeconds = 12f;
    internal static bool CanParkOutside(bool spectator, bool arrived, float elapsed, float distanceSquared, float radius)
        => !arrived && elapsed >= 0f && (!spectator || elapsed >= OptionalSpectatorWaitSeconds)
        && !float.IsNaN(distanceSquared) && !float.IsInfinity(distanceSquared)
        && !float.IsNaN(radius) && !float.IsInfinity(radius) && radius >= 0f
        && distanceSquared >= (radius + 0.6f) * (radius + 0.6f);
}
