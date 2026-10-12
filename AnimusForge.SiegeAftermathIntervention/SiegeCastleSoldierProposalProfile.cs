namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Player-facing wording for a non-decisive soldier or prisoner proposal awaiting player approval.
/// </summary>
public static class SiegeCastleSoldierProposalProfile
{
    public const uint PendingMessageColor = 0xFFFFD27Fu;

    public static string BuildPendingMessage(SiegeCastlePrisonerDispositionKind disposition)
        => BuildPendingMessage( disposition, false);

    public static string BuildPendingMessage(SiegeCastlePrisonerDispositionKind disposition, bool isEnglish)
    {
        if (isEnglish)
        {
            return "[Castle aftermath] The current speaker proposed \u0027"
                + SiegeCastlePrisonerDispositionKindProfile.Describe( disposition, true)
                + "\u0027; not yet executed, explicit player approval is required.";
        }
        return CastleAftermathCompatibilityTexts.Raw("proposal_msg_prefix")
            + SiegeCastlePrisonerDispositionKindProfile.Describe( disposition, false)
            + CastleAftermathCompatibilityTexts.Raw("proposal_msg_suffix");
    }

    public static string BuildPendingContext(SiegeCastlePrisonerDispositionKind disposition)
        => BuildPendingContext( disposition, false);

    public static string BuildPendingContext(SiegeCastlePrisonerDispositionKind disposition, bool isEnglish)
    {
        if (isEnglish)
        {
            return disposition == SiegeCastlePrisonerDispositionKind.None
                ? "There is no soldier proposal awaiting player confirmation currently."
                : "The current speaker previously proposed \u0027"
                    + SiegeCastlePrisonerDispositionKindProfile.Describe( disposition, true)
                    + "\u0027; not yet executed, settlement can only occur after the player explicitly agrees this turn.";
        }
        return disposition == SiegeCastlePrisonerDispositionKind.None
            ? CastleAftermathCompatibilityTexts.Raw("proposal_ctx_empty")
            : CastleAftermathCompatibilityTexts.Raw("proposal_ctx_prefix")
                + SiegeCastlePrisonerDispositionKindProfile.Describe( disposition, false)
                + CastleAftermathCompatibilityTexts.Raw("proposal_ctx_suffix");
    }
}
