namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free profile for destructive GCCZ choices.
/// AF adapters apply Bannerlord aftermath, troops, mission, UI, settlement, and memory side effects.
/// </summary>
public sealed class SiegeDestructiveChoiceProfile
{
    public const uint ValidationMessageColor = 0xFFFFD27Fu;


    private SiegeDestructiveChoiceProfile(
        SiegeAftermathResolutionKind aftermathKind,
        string assemblySource,
        string messageKey,
        uint messageColor)
    {
        AftermathKind = aftermathKind;
        AssemblySource = assemblySource;
        MessageKey = messageKey;
        MessageColor = messageColor;
    }

    public SiegeAftermathResolutionKind AftermathKind { get; }

    public string AssemblySource { get; }

    public string MessageKey { get; }

    public uint MessageColor { get; }

    public static SiegeDestructiveChoiceProfile BuildPlunder()
    {
        return new SiegeDestructiveChoiceProfile(
            aftermathKind: SiegeAftermathResolutionKind.Pillage,
            assemblySource: "plunder_started",
            messageKey: TownActionPresentationKeys.Plunder,
            messageColor: 0xFFFFC46Bu);
    }

    public static SiegeDestructiveChoiceProfile BuildMassacre()
    {
        return new SiegeDestructiveChoiceProfile(
            aftermathKind: SiegeAftermathResolutionKind.Devastate,
            assemblySource: string.Empty,
            messageKey: TownActionPresentationKeys.Massacre,
            messageColor: 0xFFFF7777u);
    }
}
