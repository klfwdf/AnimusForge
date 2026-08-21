namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Resolves one stable GCCZ town dialogue role from live facts supplied by the AF adapter.
/// Priority prevents Hero identities from being collapsed into ordinary troop roles.
/// </summary>
public static class TownDialogueRoleClassifier
{
    public const TownDialogueRole SafeFallbackRole = TownDialogueRole.OrdinaryCivilian;

    public static TownDialogueRole Resolve(TownDialogueRoleFacts facts)
    {
        if (facts.IsNoblePrisoner)
        {
            return TownDialogueRole.NoblePrisoner;
        }

        if (facts.IsPlayerCompanion)
        {
            return TownDialogueRole.PlayerCompanion;
        }

        if (facts.IsAccompanyingNoble)
        {
            return TownDialogueRole.AccompanyingNoble;
        }

        if (facts.IsSettlementNotable)
        {
            return TownDialogueRole.SettlementNotable;
        }

        if (facts.IsOrdinarySoldier)
        {
            return TownDialogueRole.OrdinarySoldier;
        }

        if (facts.IsOrdinaryCivilian)
        {
            return TownDialogueRole.OrdinaryCivilian;
        }

        return SafeFallbackRole;
    }

    public static TownDialogueRole NormalizeForRuntime(TownDialogueRole role)
    {
        if (role == TownDialogueRole.AccompanyingNoble
            || role == TownDialogueRole.NoblePrisoner
            || role == TownDialogueRole.PlayerCompanion
            || role == TownDialogueRole.SettlementNotable
            || role == TownDialogueRole.OrdinarySoldier
            || role == TownDialogueRole.OrdinaryCivilian)
        {
            return role;
        }

        return SafeFallbackRole;
    }

    public static bool CanExecuteAlliedSoldierOrders(TownDialogueRole role, bool isAlliedSoldier)
    {
        role = NormalizeForRuntime(role);
        return role == TownDialogueRole.OrdinarySoldier && isAlliedSoldier;
    }

    public static bool CanBeRobberyTarget(TownDialogueRole role)
    {
        role = NormalizeForRuntime(role);
        return role == TownDialogueRole.SettlementNotable
            || role == TownDialogueRole.OrdinaryCivilian;
    }
}
