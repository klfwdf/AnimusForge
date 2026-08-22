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
        TownDialogueRole dialogueRole,
        bool isAlliedSoldier,
        bool replyIsDirectPlayerResponse,
        bool massacreActive = false)
    {
        DestructiveLocked = destructiveLocked;
        SoldierAppeasementRequired = soldierAppeasementRequired;
        SoldierAppeasementApplied = soldierAppeasementApplied;
        DialogueRole = TownDialogueRoleClassifier.NormalizeForRuntime(dialogueRole);
        IsAlliedSoldier = isAlliedSoldier;
        ReplyIsDirectPlayerResponse = replyIsDirectPlayerResponse;
        MassacreActive = massacreActive;
    }

    public bool DestructiveLocked { get; }

    public bool SoldierAppeasementRequired { get; }

    public bool SoldierAppeasementApplied { get; }

    public TownDialogueRole DialogueRole { get; }

    public bool IsAlliedSoldier { get; }

    public bool ReplyIsDirectPlayerResponse { get; }

    public bool MassacreActive { get; }
}
