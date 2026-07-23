namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free policy and source identifiers for AF give-item/give-gold capture into the
/// town GCCZ shared relief pool. Castle aftermath transfers remain private gifts to the receiver.
/// The fused AF adapter owns live inventory/gold mutation and only passes successful scene transfers here.
/// </summary>
public static class SiegeSharedReliefBridgeProfile
{
    public const string ShoutGiveGoldSource = "af_shout_give_shared_relief_gold";

    public const string ShoutGiveItemSource = "af_shout_give_shared_relief_item";

    public const string CaptureContract =
        "Town GCCZ AF give transfers are centralized into the settlement-wide shared civilian relief pool; castle aftermath transfers remain private receiver property.";

    public static bool ShouldCapture(
        bool interventionActive,
        bool settlementIsTown,
        bool settlementIsCastle,
        bool captureBlockedByNegativeOutcome)
    {
        return interventionActive
            && settlementIsTown
            && !settlementIsCastle
            && !captureBlockedByNegativeOutcome;
    }
}
