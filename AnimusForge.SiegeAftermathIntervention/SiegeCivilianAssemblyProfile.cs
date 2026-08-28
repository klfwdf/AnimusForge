namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free runtime parameters for GCCZ native civilian assembly counts and layout.
/// AF adapters still own scene capacity checks, formation slot projection, and mission side effects.
/// </summary>
public static class SiegeCivilianAssemblyProfile
{
    public const int MinDesiredCivilianCount = 100;

    public const int MaxDesiredCivilianCount = 200;

    public const int TownSceneCap = MaxDesiredCivilianCount;

    // Villages have fewer navigation points and a much smaller native crowd.
    // Keep their cap separate so prosperity can make a village feel alive
    // without turning it into a second town.
    public const int VillageSceneCap = 100;

    public const int SceneTotalAgentSoftCap = 320;

    public const int MinimumSceneCap = 60;

    public const float ForwardDistance = 4.2f;

    public const float ColumnSpacing = 0.9f;

    public const float RowSpacing = 0.78f;

    public const int Columns = 14;

    public const string MissionAfterStartSource = "mission_after_start";

    public const string ControlTickSource = "control_tick";

    public const string NativeTownMaxPopulationSource = "native_town_max_population";

    public const float NativeTownPopulationRetrySeconds = 4f;

    public const float NativeTownPopulationProsperityForMaxCount = 8000f;

    public const int NativeTownPopulationRandomBand = 30;

    public const int NativeTownPopulationMaxSpawnAttempts = MaxDesiredCivilianCount * 2;
}
