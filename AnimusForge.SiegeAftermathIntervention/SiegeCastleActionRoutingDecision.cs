namespace AnimusForge.SiegeAftermathIntervention;

public sealed class SiegeCastleActionRoutingDecision
{
    public SiegeCastleActionRoutingDecision(
        bool hasRecognizedAction,
        bool isAllowed,
        SiegeCastleActionKind action,
        string reasonCode)
    {
        HasRecognizedAction = hasRecognizedAction;
        IsAllowed = isAllowed;
        Action = action;
        ReasonCode = reasonCode ?? string.Empty;
    }

    public bool HasRecognizedAction { get; }

    public bool IsAllowed { get; }

    public SiegeCastleActionKind Action { get; }

    public string ReasonCode { get; }
}
