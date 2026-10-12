namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Pure configuration for post-siege defensive-device removal. The live adapter must
/// target gates and siege weapons only; wall sections are deliberately excluded so
/// the campaign wall-breach ratios remain authoritative.
/// </summary>
public static class SiegeCastleDefensiveDeviceCleanupProfile
{
    public const float PreparationDelaySeconds = 1.25f;

    public const string RuntimeSource = "castle_aftermath_defensive_device_cleanup";

    public static string BuildSummary(int gatesRemoved, int siegeWeaponsRemoved, int failures)
    {
        return BuildSummary(gatesRemoved, siegeWeaponsRemoved, failures, isEnglish: true);
    }

    public static string BuildSummary(int gatesRemoved, int siegeWeaponsRemoved, int failures, bool isEnglish)
    {
        if (isEnglish)
        {
            return "Castle aftermath defensive devices removed: gates=" + Clamp(gatesRemoved)
                + ", siege weapons=" + Clamp(siegeWeaponsRemoved)
                + ", failures=" + Clamp(failures)
                + "; wall sections excluded from cleanup, preserving siege resolution damage ratio.";
        }
        return "Castle aftermath defensive devices removed: gates=" + Clamp(gatesRemoved)
            + ", siege weapons=" + Clamp(siegeWeaponsRemoved)
            + ", failures=" + Clamp(failures)
            + "; wall sections excluded from cleanup, preserving siege resolution damage ratio.";
    }

    private static int Clamp(int value) => value < 0 ? 0 : value;
}
