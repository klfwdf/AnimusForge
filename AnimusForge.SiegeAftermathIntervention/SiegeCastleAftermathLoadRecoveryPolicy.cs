namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>Only repairs missing transient location context; never authorizes or replays aftermath effects.</summary>
public static class SiegeCastleAftermathLoadRecoveryPolicy
{
    public static bool CanRestoreLocationEncounter(
        string menuId,
        bool isCastle,
        bool hasMatchingEncounter,
        bool hasLocationComplex,
        bool hasLocationEncounter,
        bool hasMission,
        bool hasBattle,
        bool isPlayerCaptive)
    {
        return SiegeAftermathMenuProfile.IsNativeOrContextualSummaryMenuId(menuId)
            && isCastle
            && hasMatchingEncounter
            && hasLocationComplex
            && !hasLocationEncounter
            && !hasMission
            && !hasBattle
            && !isPlayerCaptive;
    }

    public static bool IsCastleReturnMenu(string menuId)
    {
        return menuId == "castle_outside" || menuId == "castle";
    }

    public static bool IsDiagnosticMenu(string menuId)
    {
        return SiegeAftermathMenuProfile.IsNativeOrContextualSummaryMenuId(menuId)
            || IsCastleReturnMenu(menuId);
    }
}
