namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free decision for the fallback aftermath chosen when the GCCZ mission exits.
/// Runtime adapters still own Bannerlord aftermath mapping, plunder side effects, and mission transitions.
/// </summary>
public static class SiegeMissionExitOutcomeProfile
{
    public static readonly string RepopulationExitTriggerSource = SiegeAftermathCompatibilityTexts.Raw("exit_repopulation_source");
    public const string RepopulationExitTriggerSourceEn = "mission_exit_colonization";

    public static readonly string RepopulationExitTriggerDetail = SiegeAftermathCompatibilityTexts.Raw("exit_repopulation_detail");
    public const string RepopulationExitTriggerDetailEn = "Player triggered colonization disposition; departure settles as highest irreversible outcome.";

    public static readonly string MassacreExitTriggerSource = SiegeAftermathCompatibilityTexts.Raw("exit_massacre_source");
    public const string MassacreExitTriggerSourceEn = "mission_exit_massacre";

    public static readonly string MassacreExitTriggerDetail = SiegeAftermathCompatibilityTexts.Raw("exit_massacre_detail");
    public const string MassacreExitTriggerDetailEn = "Player triggered massacre; departure settles as devastation/massacre.";

    public static readonly string PlunderExitTriggerSource = SiegeAftermathCompatibilityTexts.Raw("exit_plunder_source");
    public const string PlunderExitTriggerSourceEn = "mission_exit_pillage";

    public static readonly string PlunderExitTriggerDetail = SiegeAftermathCompatibilityTexts.Raw("exit_plunder_detail");
    public const string PlunderExitTriggerDetailEn = "Player triggered pillage; departure settles as plunder.";

    public static readonly string NoDecisionExitTriggerSource = SiegeAftermathCompatibilityTexts.Raw("exit_no_decision_source");
    public const string NoDecisionExitTriggerSourceEn = "mission_exit_default_no_decision";

    public static readonly string DefaultPlunderExitTriggerDetail = SiegeAftermathCompatibilityTexts.Raw("exit_default_plunder_detail");
    public const string DefaultPlunderExitTriggerDetailEn = "Player entered siege aftermath scene and departed without explicit mercy or escalation; settles as default pillage.";

    public static readonly string CastleNoDecisionExitTriggerSource = SiegeAftermathCompatibilityTexts.Raw("exit_castle_no_decision_source");
    public const string CastleNoDecisionExitTriggerSourceEn = "castle_mission_exit_default_no_decision";

    public static readonly string CastleDefaultMercyExitTriggerDetail = SiegeAftermathCompatibilityTexts.Raw("exit_castle_default_mercy_detail");
    public const string CastleDefaultMercyExitTriggerDetailEn = "Castle scene has no civilian livelihood targets to pillage; leaving without other disposition settles as mercy.";

    public static SiegeMissionExitOutcomeDecision Resolve(
        bool culturalRepopulationRequested,
        bool massacreStarted,
        bool plunderStarted,
        bool hasPendingAftermath,
        bool destructiveAllowed,
        bool isCastle = false)
        => Resolve(culturalRepopulationRequested, massacreStarted, plunderStarted, hasPendingAftermath, destructiveAllowed, isCastle, false);

    public static SiegeMissionExitOutcomeDecision Resolve(
        bool culturalRepopulationRequested,
        bool massacreStarted,
        bool plunderStarted,
        bool hasPendingAftermath,
        bool destructiveAllowed,
        bool isCastle,
        bool isEnglish)
    {
        if (culturalRepopulationRequested)
        {
            return Mark(SiegeAftermathResolutionKind.Devastate,
                isEnglish ? RepopulationExitTriggerSourceEn : RepopulationExitTriggerSource,
                isEnglish ? RepopulationExitTriggerDetailEn : RepopulationExitTriggerDetail);
        }

        if (massacreStarted)
        {
            return Mark(SiegeAftermathResolutionKind.Devastate,
                isEnglish ? MassacreExitTriggerSourceEn : MassacreExitTriggerSource,
                isEnglish ? MassacreExitTriggerDetailEn : MassacreExitTriggerDetail);
        }

        if (plunderStarted)
        {
            return Mark(SiegeAftermathResolutionKind.Pillage,
                isEnglish ? PlunderExitTriggerSourceEn : PlunderExitTriggerSource,
                isEnglish ? PlunderExitTriggerDetailEn : PlunderExitTriggerDetail);
        }

        if (hasPendingAftermath)
        {
            return SiegeMissionExitOutcomeDecision.None;
        }

        if (isCastle)
        {
            return Mark(SiegeAftermathResolutionKind.ShowMercy,
                isEnglish ? CastleNoDecisionExitTriggerSourceEn : CastleNoDecisionExitTriggerSource,
                isEnglish ? CastleDefaultMercyExitTriggerDetailEn : CastleDefaultMercyExitTriggerDetail);
        }

        return StartPlunder(
            isEnglish ? NoDecisionExitTriggerSourceEn : NoDecisionExitTriggerSource,
            isEnglish ? DefaultPlunderExitTriggerDetailEn : DefaultPlunderExitTriggerDetail);
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
