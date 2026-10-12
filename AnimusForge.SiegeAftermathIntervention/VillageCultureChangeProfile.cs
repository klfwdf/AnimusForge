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
        => GetDisplayName(mode, false);

    public static string GetDisplayName(VillageCultureChangeMode mode, bool isEnglish)
    {
        if (isEnglish)
        {
            switch (mode)
            {
                case VillageCultureChangeMode.GradualEducation:
                    return "Gradual education";
                case VillageCultureChangeMode.MigrantResettlement:
                    return "Migrant resettlement";
                case VillageCultureChangeMode.PurgeColonization:
                    return "Purge and colonization";
                default:
                    return "No change";
            }
        }
        switch (mode)
        {
            case VillageCultureChangeMode.GradualEducation:
                return SiegeAftermathCompatibilityTexts.Raw("village_culture_gradual");
            case VillageCultureChangeMode.MigrantResettlement:
                return SiegeAftermathCompatibilityTexts.Raw("village_culture_migration");
            case VillageCultureChangeMode.PurgeColonization:
                return SiegeAftermathCompatibilityTexts.Raw("village_culture_purge");
            default:
                return SiegeAftermathCompatibilityTexts.Raw("village_culture_none");
        }
    }
}
