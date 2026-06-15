namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free representation of the native Bannerlord siege aftermath choices used by GCCZ.
/// AF adapters map TaleWorlds enum values into this core enum.
/// </summary>
public enum SiegeAftermathResolutionKind
{
    Unknown = 0,
    ShowMercy = 1,
    Pillage = 2,
    Devastate = 3,
}
