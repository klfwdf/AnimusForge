namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// GCCZ action kinds currently recognized by the fused post-siege intervention scene.
/// </summary>
public enum SiegeInterventionActionKind
{
    Unknown = 0,
    Mercy = 1,
    Relief = 2,
    Inspire = 3,
    RallyOath = 4,
    AppeaseSoldiers = 5,
    GatherCivilians = 6,
    CivilianRobbery = 10,
    Plunder = 7,
    Massacre = 8,
    CulturalRepopulation = 9,
    StopMassacre = 11,
    ConstructiveCultureChange = 12
}
