namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free source identifiers for AF give-item/give-gold capture into the GCCZ shared relief pool.
/// The fused AF adapter owns live inventory/gold mutation and only passes successful scene transfers here.
/// </summary>
public static class SiegeSharedReliefBridgeProfile
{
    public const string ShoutGiveGoldSource = "af_shout_give_shared_relief_gold";

    public const string ShoutGiveItemSource = "af_shout_give_shared_relief_item";

    public const string CaptureContract =
        "GCCZ active scene AF give transfers to in-scene allied soldiers, civilians, merchants, artisans, headmen, or notables are centralized into the settlement-wide shared civilian relief pool instead of becoming private receiver property.";
}
