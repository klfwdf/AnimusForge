namespace AnimusForge.SiegeAftermathIntervention;

public readonly struct TownDialogueRoleFacts
{
    public TownDialogueRoleFacts(
        bool isAccompanyingNoble,
        bool isNoblePrisoner,
        bool isPlayerCompanion,
        bool isSettlementNotable,
        bool isOrdinarySoldier,
        bool isOrdinaryCivilian)
    {
        IsAccompanyingNoble = isAccompanyingNoble;
        IsNoblePrisoner = isNoblePrisoner;
        IsPlayerCompanion = isPlayerCompanion;
        IsSettlementNotable = isSettlementNotable;
        IsOrdinarySoldier = isOrdinarySoldier;
        IsOrdinaryCivilian = isOrdinaryCivilian;
    }

    public bool IsAccompanyingNoble { get; }

    public bool IsNoblePrisoner { get; }

    public bool IsPlayerCompanion { get; }

    public bool IsSettlementNotable { get; }

    public bool IsOrdinarySoldier { get; }

    public bool IsOrdinaryCivilian { get; }
}
