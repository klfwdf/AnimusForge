namespace AnimusForge.SiegeAftermathIntervention;

public sealed class SiegeCompletedInterventionSummaryFacts
{
    public SiegeCompletedInterventionSummaryFacts(
        string settlementName,
        SiegeAftermathResolutionKind aftermathKind,
        bool culturalRepopulationApplied,
        bool massacreStarted,
        bool plunderStarted,
        string targetCultureText,
        int marketItemTotal,
        int marketStackKinds,
        int marketItemValue,
        int marketGold,
        int civilianGold,
        int civilianTargetsLooted)
    {
        SettlementName = settlementName;
        AftermathKind = aftermathKind;
        CulturalRepopulationApplied = culturalRepopulationApplied;
        MassacreStarted = massacreStarted;
        PlunderStarted = plunderStarted;
        TargetCultureText = targetCultureText;
        MarketItemTotal = marketItemTotal;
        MarketStackKinds = marketStackKinds;
        MarketItemValue = marketItemValue;
        MarketGold = marketGold;
        CivilianGold = civilianGold;
        CivilianTargetsLooted = civilianTargetsLooted;
    }

    public string SettlementName { get; }

    public SiegeAftermathResolutionKind AftermathKind { get; }

    public bool CulturalRepopulationApplied { get; }

    public bool MassacreStarted { get; }

    public bool PlunderStarted { get; }

    public string TargetCultureText { get; }

    public int MarketItemTotal { get; }

    public int MarketStackKinds { get; }

    public int MarketItemValue { get; }

    public int MarketGold { get; }

    public int CivilianGold { get; }

    public int CivilianTargetsLooted { get; }
}
