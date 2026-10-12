namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free decision for the fallback aftermath chosen when the GCCZ mission exits.
/// Runtime adapters still own Bannerlord aftermath mapping, plunder side effects, and mission transitions.
/// </summary>
public static class SiegeMissionExitOutcomeProfile
{
    public const string RepopulationExitTriggerSource = "Scene exit colonization";
    public const string RepopulationExitTriggerSourceEn = "mission_exit_colonization";

    public const string RepopulationExitTriggerDetail = "Colonization was triggered; scene departure commits the highest irreversible disposition.";
    public const string RepopulationExitTriggerDetailEn = "Player triggered colonization disposition; departure settles as highest irreversible outcome.";

    public const string MassacreExitTriggerSource = "Scene exit massacre";
    public const string MassacreExitTriggerSourceEn = "mission_exit_massacre";

    public const string MassacreExitTriggerDetail = "Massacre was triggered; scene departure commits devastation/massacre.";
    public const string MassacreExitTriggerDetailEn = "Player triggered massacre; departure settles as devastation/massacre.";

    public const string PlunderExitTriggerSource = "Scene exit plunder";
    public const string PlunderExitTriggerSourceEn = "mission_exit_pillage";

    public const string PlunderExitTriggerDetail = "Plunder was triggered; scene departure commits plunder.";
    public const string PlunderExitTriggerDetailEn = "Player triggered pillage; departure settles as plunder.";

    public const string NoDecisionExitTriggerSource = "Scene exit without decision";
    public const string NoDecisionExitTriggerSourceEn = "mission_exit_default_no_decision";

    public const string DefaultPlunderExitTriggerDetail = "The player left without an explicit constructive or escalated disposition; commits default plunder.";
    public const string DefaultPlunderExitTriggerDetailEn = "Player entered siege aftermath scene and departed without explicit mercy or escalation; settles as default pillage.";

    public const string CastleNoDecisionExitTriggerSource = "Castle scene exit without decision";
    public const string CastleNoDecisionExitTriggerSourceEn = "castle_mission_exit_default_no_decision";

    public const string CastleDefaultMercyExitTriggerDetail = "Castle scenes have no town plunder targets; departure without other orders commits mercy.";
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
