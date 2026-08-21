namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Resolves one stable GCCZ town dialogue role from live facts supplied by the AF adapter.
/// Priority prevents Hero identities from being collapsed into ordinary troop roles.
/// </summary>
public static class TownDialogueRoleClassifier
{
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

        return TownDialogueRole.Unknown;
    }

    public static bool CanExecuteAlliedSoldierOrders(TownDialogueRole role, bool isAlliedSoldier)
    {
        return role == TownDialogueRole.OrdinarySoldier && isAlliedSoldier;
    }

    public static bool CanBeRobberyTarget(TownDialogueRole role)
    {
        return role == TownDialogueRole.SettlementNotable
            || role == TownDialogueRole.OrdinaryCivilian;
    }
}
