namespace AnimusForge.SiegeAftermathIntervention;

public sealed class VillageAftermathEntryFacts
{
    public VillageAftermathEntryFacts(
        bool isVillage,
        bool hasBoundSettlement,
        bool villageStateIsNormal,
        bool isUnderSiegeOrRaid,
        bool playerClanOwnsBoundSettlement,
        bool playerIsKingdomRuler,
        bool boundSettlementBelongsToPlayerKingdom)
    {
        IsVillage = isVillage;
        HasBoundSettlement = hasBoundSettlement;
        VillageStateIsNormal = villageStateIsNormal;
        IsUnderSiegeOrRaid = isUnderSiegeOrRaid;
        PlayerClanOwnsBoundSettlement = playerClanOwnsBoundSettlement;
        PlayerIsKingdomRuler = playerIsKingdomRuler;
        BoundSettlementBelongsToPlayerKingdom = boundSettlementBelongsToPlayerKingdom;
    }

    public bool IsVillage { get; }

    public bool HasBoundSettlement { get; }

    public bool VillageStateIsNormal { get; }

    public bool IsUnderSiegeOrRaid { get; }

    public bool PlayerClanOwnsBoundSettlement { get; }

    public bool PlayerIsKingdomRuler { get; }

    public bool BoundSettlementBelongsToPlayerKingdom { get; }
}
