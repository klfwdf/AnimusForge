namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Runtime eligibility facts used to reduce GCCZ postprocess candidates before
/// they are shown to the language model.
/// </summary>
public readonly struct SiegePostprocessRuleEligibilityFacts
{
    public SiegePostprocessRuleEligibilityFacts(
        bool destructiveLocked,
        bool soldierAppeasementRequired,
        bool soldierAppeasementApplied,
        bool isAlliedSoldier,
        bool isCivilian,
        bool replyIsDirectPlayerResponse)
    {
        DestructiveLocked = destructiveLocked;
        SoldierAppeasementRequired = soldierAppeasementRequired;
        SoldierAppeasementApplied = soldierAppeasementApplied;
        IsAlliedSoldier = isAlliedSoldier;
        IsCivilian = isCivilian;
        ReplyIsDirectPlayerResponse = replyIsDirectPlayerResponse;
    }

    public bool DestructiveLocked { get; }

    public bool SoldierAppeasementRequired { get; }

    public bool SoldierAppeasementApplied { get; }

    public bool IsAlliedSoldier { get; }

    public bool IsCivilian { get; }

    public bool ReplyIsDirectPlayerResponse { get; }
}
