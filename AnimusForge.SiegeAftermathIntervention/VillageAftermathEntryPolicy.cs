namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// GCCZ village is a noble-administration scene, not a raid replacement.
/// Ordinary vassals may visit another friendly village without gaining governance rights.
/// </summary>
public static class VillageAftermathEntryPolicy
{
    public static VillageAftermathEntryDecision Evaluate(VillageAftermathEntryFacts facts)
    {
        if (facts == null || !facts.IsVillage)
        {
            return Deny("not_village");
        }

        if (!facts.HasBoundSettlement)
        {
            return Deny("missing_bound_settlement");
        }

        if (!facts.VillageStateIsNormal || facts.IsUnderSiegeOrRaid)
        {
            return Deny("village_not_in_peaceful_normal_state");
        }

        if (facts.PlayerClanOwnsBoundSettlement)
        {
            return new VillageAftermathEntryDecision(true, VillageAftermathAuthorityKind.DirectOwner, "direct_owner");
        }

        if (facts.PlayerIsKingdomRuler && facts.BoundSettlementBelongsToPlayerKingdom)
        {
            return new VillageAftermathEntryDecision(true, VillageAftermathAuthorityKind.KingdomRuler, "kingdom_ruler_over_vassal_village");
        }

        if (facts.BoundSettlementBelongsToPlayerKingdom)
            return new VillageAftermathEntryDecision(true, VillageAftermathAuthorityKind.FriendlyVisitor, "friendly_visitor");

        return Deny("no_village_administration_authority");
    }

    private static VillageAftermathEntryDecision Deny(string reasonCode)
    {
        return new VillageAftermathEntryDecision(false, VillageAftermathAuthorityKind.None, reasonCode);
    }
}
