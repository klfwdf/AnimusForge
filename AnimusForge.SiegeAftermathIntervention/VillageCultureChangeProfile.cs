using System;

namespace AnimusForge.SiegeAftermathIntervention;

public static class VillageCultureChangeProfile
{
    public const int GradualEducationDays = 180;

    public const float MigrantResettlementHearthMultiplier = 0.75f;

    public const float PurgeColonizationHearthMultiplier = 0.40f;

    public const int MigrantResettlementOwnerRelationDelta = -25;

    public const int PurgeColonizationOwnerRelationDelta = -70;

    public static float ApplyImmediateHearth(VillageCultureChangeMode mode, float hearth)
    {
        float safe = float.IsNaN(hearth) || float.IsInfinity(hearth) ? 0f : Math.Max(0f, hearth);
        switch (mode)
        {
            case VillageCultureChangeMode.MigrantResettlement:
                return safe * MigrantResettlementHearthMultiplier;
            case VillageCultureChangeMode.PurgeColonization:
                return safe * PurgeColonizationHearthMultiplier;
            default:
                return safe;
        }
    }

    public static string GetDisplayName(VillageCultureChangeMode mode)
    {
        switch (mode)
        {
            case VillageCultureChangeMode.GradualEducation:
                return "教化改俗";
            case VillageCultureChangeMode.MigrantResettlement:
                return "迁民改俗";
            case VillageCultureChangeMode.PurgeColonization:
                return "屠村迁殖";
            default:
                return "不改变";
        }
    }
}
