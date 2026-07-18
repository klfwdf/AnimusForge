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
        return "城堡战后防御设施已移除：城门=" + Clamp(gatesRemoved)
            + "，攻防器械=" + Clamp(siegeWeaponsRemoved)
            + "，失败=" + Clamp(failures)
            + "；城墙段未纳入清理，继续采用围城结算保存的破坏比例。";
    }

    private static int Clamp(int value) => value < 0 ? 0 : value;
}
