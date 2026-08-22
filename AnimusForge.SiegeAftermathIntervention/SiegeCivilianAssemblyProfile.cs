namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free runtime parameters for GCCZ civilian assembly layout and scene capacity.
/// AF adapters still own scene capacity checks, formation slot projection, and mission side effects.
/// </summary>
public static class SiegeCivilianAssemblyProfile
{
    public const int TownSceneCap = 200;

    public const int SceneTotalAgentSoftCap = 320;

    public const int MinimumSceneCap = 60;

    public const float ForwardDistance = 4.2f;

    public const float ColumnSpacing = 0.9f;

    public const float RowSpacing = 0.78f;

    public const int Columns = 14;

    public const string MissionAfterStartSource = "mission_after_start";

    public const string ControlTickSource = "control_tick";
}
