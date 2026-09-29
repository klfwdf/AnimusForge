using System;

namespace AnimusForge.DialogueUI.Scene;

// Which wheel sector a point is in. Numbers match tools/wheel_geometry.py, measured from
// afdui_wheel_chassis_symmetric.png (spokes at 0/90/180/240/300°, ring r 170..440 of 512).
internal static class WheelSectors
{
    internal const float InnerRatio = 170f / 512f;
    internal const float OuterRatio = 440f / 512f;

    // nx, ny: point relative to the wheel centre, divided by the half size; ny grows downward (screen).
    internal static string At(float nx, float ny)
    {
        float r = (float)Math.Sqrt(nx * nx + ny * ny);
        if (float.IsNaN(r) || r < InnerRatio || r > OuterRatio) return null;
        double degrees = Math.Atan2(-ny, nx) * 180.0 / Math.PI;
        if (degrees < 0) degrees += 360.0;
        if (degrees < 90) return "actions";
        if (degrees < 180) return "talk";
        if (degrees < 240) return "rumor";
        if (degrees < 300) return "leave";
        return "give";
    }
}
