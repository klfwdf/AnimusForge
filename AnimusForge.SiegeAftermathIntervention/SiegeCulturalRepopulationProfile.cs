namespace AnimusForge.SiegeAftermathIntervention;

public sealed class SiegeCulturalRepopulationProfile
{
    public const uint ValidationMessageColor = 0xFFFFD27Fu;

    public const string VictoryAlreadyReachedApplySource = "victory_already_reached";

    public const string MassacreVictoryApplySource = "massacre_victory";

    public const string FinalizeAftermathApplySource = "finalize_aftermath";

    public const string DirectMassacreLootMessageApplySource = "direct_massacre_loot_message";

    public SiegeAftermathResolutionKind AftermathKind { get; } = SiegeAftermathResolutionKind.Devastate;

    public string MessageKey { get; } = TownActionPresentationKeys.CulturalRepopulation;

    public string MassacreTriggerSource { get; } = "cultural_repopulation";

    public string MassacreTriggerDetail { get; } = "destructive colonization requested through an allied occupation soldier";

    public uint PendingMessageColor { get; } = 0xFFFF7777u;

    public uint SceneTransitionMessageColor { get; } = 0xFFFF7777u;
}
