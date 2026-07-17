namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Pure configuration for the post-siege defensive-device cleanup. The live adapter
/// must target gates and siege weapons only; wall sections are deliberately excluded
/// so the campaign wall-breach ratios remain authoritative.
/// </summary>
public static class SiegeCastleDefensiveDeviceCleanupProfile
{
    public const float PreparationDelaySeconds = 1.25f;

    public const int DestructionDamageMargin = 1000;

    public const string RuntimeSource = "castle_aftermath_defensive_device_cleanup";

    public static string BuildSummary(int gatesDestroyed, int siegeWeaponsDestroyed, int failures)
    {
        return "城堡战后防御器械清理：城门=" + Clamp(gatesDestroyed)
            + "，攻防器械=" + Clamp(siegeWeaponsDestroyed)
            + "，失败=" + Clamp(failures)
            + "；城墙段未纳入清理，继续采用围城结算保存的破坏比例。";
    }

    private static int Clamp(int value) => value < 0 ? 0 : value;
}
