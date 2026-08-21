namespace AnimusForge.SiegeAftermathIntervention;

public enum TownDialogueMemoryScope
{
    SceneLocal = 0,
    PersistentPersonal = 1,
}

/// <summary>
/// Defines the memory lifetime for each GCCZ town dialogue role.
/// AF adapters still verify that a persistent role resolves to a named Hero.
/// </summary>
public static class TownDialogueMemoryPolicy
{
    public static TownDialogueMemoryScope ResolveScope(TownDialogueRole role)
    {
        role = TownDialogueRoleClassifier.NormalizeForRuntime(role);
        if (role == TownDialogueRole.AccompanyingNoble
            || role == TownDialogueRole.NoblePrisoner
            || role == TownDialogueRole.PlayerCompanion
            || role == TownDialogueRole.SettlementNotable)
        {
            return TownDialogueMemoryScope.PersistentPersonal;
        }

        return TownDialogueMemoryScope.SceneLocal;
    }

    public static bool CanUsePersistentPersonalMemory(TownDialogueRole role, bool hasNamedHero)
    {
        return hasNamedHero && ResolveScope(role) == TownDialogueMemoryScope.PersistentPersonal;
    }
}
