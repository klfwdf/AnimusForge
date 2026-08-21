namespace AnimusForge.SiegeAftermathIntervention;

public enum TownDialogueMemoryScope
{
    None = 0,
    PersistentPersonal = 1,
    SceneLocal = 2,
}

/// <summary>
/// Defines the memory lifetime for each GCCZ town dialogue role.
/// AF adapters still verify that a persistent role resolves to a named Hero.
/// </summary>
public static class TownDialogueMemoryPolicy
{
    public static TownDialogueMemoryScope ResolveScope(TownDialogueRole role)
    {
        if (role == TownDialogueRole.AccompanyingNoble
            || role == TownDialogueRole.NoblePrisoner
            || role == TownDialogueRole.PlayerCompanion
            || role == TownDialogueRole.SettlementNotable)
        {
            return TownDialogueMemoryScope.PersistentPersonal;
        }

        if (role == TownDialogueRole.OrdinarySoldier
            || role == TownDialogueRole.OrdinaryCivilian)
        {
            return TownDialogueMemoryScope.SceneLocal;
        }

        return TownDialogueMemoryScope.None;
    }

    public static bool CanUsePersistentPersonalMemory(TownDialogueRole role, bool hasNamedHero)
    {
        return hasNamedHero && ResolveScope(role) == TownDialogueMemoryScope.PersistentPersonal;
    }
}
