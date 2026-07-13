namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free decision for the fallback aftermath chosen when the GCCZ mission exits.
/// Runtime adapters still own Bannerlord aftermath mapping, plunder side effects, and mission transitions.
/// </summary>
public static class SiegeMissionExitOutcomeProfile
{
    public const string RepopulationExitTriggerSource = "场景离场屠民迁殖";

    public const string RepopulationExitTriggerDetail = "玩家已触发屠民迁殖处置，本次离场按最高级不可逆处置结算。";

    public const string MassacreExitTriggerSource = "场景离场血洗";

    public const string MassacreExitTriggerDetail = "玩家已触发血洗，本次离场按毁坏/血洗结算。";

    public const string PlunderExitTriggerSource = "场景离场搜掠";

    public const string PlunderExitTriggerDetail = "玩家已触发搜掠，本次离场按搜掠结算。";

    public const string NoDecisionExitTriggerSource = "未选择处置直接离场";

    public const string DefaultPlunderExitTriggerDetail = "玩家进入攻城后定居点场景后未明确安抚、宽恕或升级处置便离场，按默认搜掠结算。";

    public const string CastleNoDecisionExitTriggerSource = "城堡未选择处置直接离场";

    public const string CastleDefaultMercyExitTriggerDetail = "城堡处置场景没有可搜掠的城镇民生目标；玩家未选择其他处置便离场时，按宽恕结算。";

    public static SiegeMissionExitOutcomeDecision Resolve(
        bool culturalRepopulationRequested,
        bool massacreStarted,
        bool plunderStarted,
        bool hasPendingAftermath,
        bool destructiveAllowed,
        bool isCastle = false)
    {
        if (culturalRepopulationRequested)
        {
            return Mark(SiegeAftermathResolutionKind.Devastate, RepopulationExitTriggerSource, RepopulationExitTriggerDetail);
        }

        if (massacreStarted)
        {
            return Mark(SiegeAftermathResolutionKind.Devastate, MassacreExitTriggerSource, MassacreExitTriggerDetail);
        }

        if (plunderStarted)
        {
            return Mark(SiegeAftermathResolutionKind.Pillage, PlunderExitTriggerSource, PlunderExitTriggerDetail);
        }

        if (hasPendingAftermath)
        {
            return SiegeMissionExitOutcomeDecision.None;
        }

        if (isCastle)
        {
            return Mark(SiegeAftermathResolutionKind.ShowMercy, CastleNoDecisionExitTriggerSource, CastleDefaultMercyExitTriggerDetail);
        }

        return StartPlunder(NoDecisionExitTriggerSource, DefaultPlunderExitTriggerDetail);
    }

    private static SiegeMissionExitOutcomeDecision Mark(SiegeAftermathResolutionKind aftermathKind, string triggerSource, string triggerDetail)
    {
        return new SiegeMissionExitOutcomeDecision(
            hasDecision: true,
            shouldStartPlunder: false,
            aftermathKind: aftermathKind,
            triggerSource: triggerSource,
            triggerDetail: triggerDetail);
    }

    private static SiegeMissionExitOutcomeDecision StartPlunder(string triggerSource, string triggerDetail)
    {
        return new SiegeMissionExitOutcomeDecision(
            hasDecision: true,
            shouldStartPlunder: true,
            aftermathKind: SiegeAftermathResolutionKind.Pillage,
            triggerSource: triggerSource,
            triggerDetail: triggerDetail);
    }
}

public readonly struct SiegeMissionExitOutcomeDecision
{
    public SiegeMissionExitOutcomeDecision(
        bool hasDecision,
        bool shouldStartPlunder,
        SiegeAftermathResolutionKind aftermathKind,
        string triggerSource,
        string triggerDetail)
    {
        HasDecision = hasDecision;
        ShouldStartPlunder = shouldStartPlunder;
        AftermathKind = aftermathKind;
        TriggerSource = triggerSource ?? string.Empty;
        TriggerDetail = triggerDetail ?? string.Empty;
    }

    public static SiegeMissionExitOutcomeDecision None
    {
        get
        {
            return new SiegeMissionExitOutcomeDecision(
                hasDecision: false,
                shouldStartPlunder: false,
                aftermathKind: SiegeAftermathResolutionKind.Unknown,
                triggerSource: string.Empty,
                triggerDetail: string.Empty);
        }
    }

    public bool HasDecision { get; }

    public bool ShouldStartPlunder { get; }

    public SiegeAftermathResolutionKind AftermathKind { get; }

    public string TriggerSource { get; }

    public string TriggerDetail { get; }
}
