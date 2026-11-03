using System;

namespace AnimusForge;

// Pure geometry used by the real scene consumers. All sampling runs once at camp initialization.
internal static class FieldCampPlacementRules
{
    // The full camp spans over 70 m. A 2.4 m cap rejected ordinary gentle fields;
    // local grade and per-tent fitting still reject hills, steps and uneven floors.
    internal const float MaxCampSpread = 6f;
    internal const float MaxCampGrade = 0.12f;
    internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    internal static bool TryTentScale(float halfWidth, float halfDepth, float requested,
        float parcelWidth, float parcelDepth, out float scale)
    {
        scale = 0f;
        if (!Finite(halfWidth) || !Finite(halfDepth) || !Finite(requested) ||
            halfWidth <= 0f || halfDepth <= 0f || requested <= 0f) return false;
        scale = Math.Min(requested, Math.Min(parcelWidth / halfWidth, parcelDepth / halfDepth));
        // Keep a usable human-sized interior instead of producing miniature tents.
        return scale >= 0.45f && halfWidth * scale >= 1.4f && halfDepth * scale >= 1.4f;
    }

    internal static bool IsPhysicallyClear(float distanceSquared, float radius) =>
        Finite(distanceSquared) && Finite(radius) && radius >= 0f &&
        distanceSquared >= (radius + 0.6f) * (radius + 0.6f);
}

internal struct FieldCampSurfaceProfile
{
    private int _count;
    private float _low, _high;
    internal float Grade { get; private set; }
    internal float Spread => _count == 0 ? 0f : _high - _low;

    internal bool Add(float height, float left, float behind, float stepX, float stepY)
    {
        if (!FieldCampPlacementRules.Finite(height)) return false;
        if (_count++ == 0) _low = _high = height;
        else { _low = Math.Min(_low, height); _high = Math.Max(_high, height); }
        if (FieldCampPlacementRules.Finite(left)) Grade = Math.Max(Grade, Math.Abs(height - left) / stepX);
        if (FieldCampPlacementRules.Finite(behind)) Grade = Math.Max(Grade, Math.Abs(height - behind) / stepY);
        return Spread <= FieldCampPlacementRules.MaxCampSpread && Grade <= FieldCampPlacementRules.MaxCampGrade;
    }
}
