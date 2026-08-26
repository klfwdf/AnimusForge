namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Owns authority and audience boundaries for the six GCCZ town dialogue roles.
/// The player is the only decisive authority; every NPC role may advise, but only
/// an allied ordinary soldier can execute military orders.
/// </summary>
public static class TownDialogueAuthorityPolicy
{
    public static bool HasIndependentSettlementAuthority(TownDialogueRole role)
    {
        return false;
    }

    public static bool MustObeyDirectPlayerCommand(TownDialogueRole role, bool isAlliedSoldier)
    {
        return TownDialogueRoleClassifier.CanExecuteAlliedSoldierOrders(role, isAlliedSoldier);
    }

    public static bool CanEmitPositiveSettlementOutcome(
        TownDialogueRole role,
        bool isAlliedSoldier,
        SiegeInterventionActionKind action)
    {
        role = TownDialogueRoleClassifier.NormalizeForRuntime(role);
        if (action == SiegeInterventionActionKind.Mercy)
        {
            return true;
        }

        if (action != SiegeInterventionActionKind.Relief
            && action != SiegeInterventionActionKind.Inspire
            && action != SiegeInterventionActionKind.RallyOath)
        {
            return false;
        }

        return role == TownDialogueRole.SettlementNotable
            || role == TownDialogueRole.OrdinaryCivilian;
    }

    public static TownAmbientReactionAudience ResolveAmbientAudience(
        TownDialogueRole role,
        bool isAlliedSoldier)
    {
        role = TownDialogueRoleClassifier.NormalizeForRuntime(role);
        if (role == TownDialogueRole.AccompanyingNoble
            || role == TownDialogueRole.PlayerCompanion
            || (role == TownDialogueRole.OrdinarySoldier && isAlliedSoldier))
        {
            return TownAmbientReactionAudience.Allied;
        }

        if (role == TownDialogueRole.NoblePrisoner
            || role == TownDialogueRole.SettlementNotable
            || role == TownDialogueRole.OrdinaryCivilian)
        {
            return TownAmbientReactionAudience.Civilian;
        }

        return TownAmbientReactionAudience.None;
    }

    public static bool CanOfferSuggestion(TownDialogueRole role, bool isAlliedSoldier)
    {
        return ResolveAmbientAudience(role, isAlliedSoldier) != TownAmbientReactionAudience.None;
    }

    public static bool CanExpressSoldierDiscontent(TownDialogueRole role, bool isAlliedSoldier)
    {
        return role == TownDialogueRole.OrdinarySoldier && isAlliedSoldier;
    }
}
