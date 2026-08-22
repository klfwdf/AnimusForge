using System;

namespace AnimusForge.SiegeAftermathIntervention;

public enum TownConstructiveCultureChangeStatus
{
    Allowed = 0,
    InactiveTownStage = 1,
    MissingSettlement = 2,
    MissingTargetCulture = 3,
    AlreadyTargetCulture = 4,
    IndirectReply = 5,
    DestructiveCombatActive = 6,
    ColonizationStateActive = 7,
    UnauthorizedRole = 8,
}

public readonly struct TownConstructiveCultureChangeFacts
{
    public TownConstructiveCultureChangeFacts(
        bool activeTownStage,
        string settlementId,
        string currentCultureId,
        string targetCultureId,
        TownDialogueRole dialogueRole,
        bool isAlliedSoldier,
        bool replyIsDirectPlayerResponse,
        bool massacreActive,
        bool colonizationStateActive)
    {
        ActiveTownStage = activeTownStage;
        SettlementId = settlementId ?? string.Empty;
        CurrentCultureId = currentCultureId ?? string.Empty;
        TargetCultureId = targetCultureId ?? string.Empty;
        DialogueRole = TownDialogueRoleClassifier.NormalizeForRuntime(dialogueRole);
        IsAlliedSoldier = isAlliedSoldier;
        ReplyIsDirectPlayerResponse = replyIsDirectPlayerResponse;
        MassacreActive = massacreActive;
        ColonizationStateActive = colonizationStateActive;
    }

    public bool ActiveTownStage { get; }

    public string SettlementId { get; }

    public string CurrentCultureId { get; }

    public string TargetCultureId { get; }

    public TownDialogueRole DialogueRole { get; }

    public bool IsAlliedSoldier { get; }

    public bool ReplyIsDirectPlayerResponse { get; }

    public bool MassacreActive { get; }

    public bool ColonizationStateActive { get; }
}

public readonly struct TownConstructiveCultureChangeDecision
{
    public TownConstructiveCultureChangeDecision(TownConstructiveCultureChangeStatus status)
    {
        Status = status;
    }

    public TownConstructiveCultureChangeStatus Status { get; }

    public bool CanApply => Status == TownConstructiveCultureChangeStatus.Allowed;
}

public static class TownConstructiveCultureChangePolicy
{
    public static TownConstructiveCultureChangeDecision Evaluate(TownConstructiveCultureChangeFacts facts)
    {
        if (!facts.ActiveTownStage)
        {
            return Deny(TownConstructiveCultureChangeStatus.InactiveTownStage);
        }

        if (string.IsNullOrWhiteSpace(facts.SettlementId))
        {
            return Deny(TownConstructiveCultureChangeStatus.MissingSettlement);
        }

        if (string.IsNullOrWhiteSpace(facts.TargetCultureId))
        {
            return Deny(TownConstructiveCultureChangeStatus.MissingTargetCulture);
        }

        if (string.Equals(facts.CurrentCultureId, facts.TargetCultureId, StringComparison.OrdinalIgnoreCase))
        {
            return Deny(TownConstructiveCultureChangeStatus.AlreadyTargetCulture);
        }

        if (!facts.ReplyIsDirectPlayerResponse)
        {
            return Deny(TownConstructiveCultureChangeStatus.IndirectReply);
        }

        if (facts.MassacreActive)
        {
            return Deny(TownConstructiveCultureChangeStatus.DestructiveCombatActive);
        }

        if (facts.ColonizationStateActive)
        {
            return Deny(TownConstructiveCultureChangeStatus.ColonizationStateActive);
        }

        if (!TownDialogueRoleClassifier.CanAuthorizeConstructiveCultureChange(facts.DialogueRole, facts.IsAlliedSoldier))
        {
            return Deny(TownConstructiveCultureChangeStatus.UnauthorizedRole);
        }

        return new TownConstructiveCultureChangeDecision(TownConstructiveCultureChangeStatus.Allowed);
    }

    private static TownConstructiveCultureChangeDecision Deny(TownConstructiveCultureChangeStatus status)
    {
        return new TownConstructiveCultureChangeDecision(status);
    }
}
