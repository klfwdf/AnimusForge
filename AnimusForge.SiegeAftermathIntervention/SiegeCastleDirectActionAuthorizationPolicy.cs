namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Non-semantic guard applied after AF's action postprocessor has selected a castle tag.
/// The AI owns intent, consent, refusal and proposal-versus-settlement interpretation;
/// this policy only rejects malformed routing facts that cannot represent a player turn.
/// </summary>
public static class SiegeCastleDirectActionAuthorizationPolicy
{
    public static SiegeCastleDirectActionAuthorizationDecision Evaluate(
        SiegeCastleActionKind action,
        string playerText)
    {
        if (action == SiegeCastleActionKind.Unknown)
        {
            return SiegeCastleDirectActionAuthorizationDecision.Denied("castle_action_unknown");
        }

        if (string.IsNullOrWhiteSpace(playerText))
        {
            return SiegeCastleDirectActionAuthorizationDecision.Denied("player_text_missing");
        }

        return SiegeCastleDirectActionAuthorizationDecision.Authorized("castle_ai_postprocess_tag_authorized");
    }
}

public sealed class SiegeCastleDirectActionAuthorizationDecision
{
    private SiegeCastleDirectActionAuthorizationDecision(bool isAuthorized, string reasonCode)
    {
        IsAuthorized = isAuthorized;
        ReasonCode = reasonCode ?? string.Empty;
    }

    public bool IsAuthorized { get; }

    public string ReasonCode { get; }

    internal static SiegeCastleDirectActionAuthorizationDecision Authorized(string reasonCode)
        => new SiegeCastleDirectActionAuthorizationDecision(true, reasonCode);

    internal static SiegeCastleDirectActionAuthorizationDecision Denied(string reasonCode)
        => new SiegeCastleDirectActionAuthorizationDecision(false, reasonCode);
}
