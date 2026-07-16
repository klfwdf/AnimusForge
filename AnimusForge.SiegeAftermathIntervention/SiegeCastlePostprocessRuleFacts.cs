namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Runtime facts used to expose only role-valid, current-turn castle action tags.
/// </summary>
public sealed class SiegeCastlePostprocessRuleFacts
{
    public SiegeCastlePostprocessRuleFacts(
        SiegeCastleActionSpeakerRole speakerRole,
        bool replyIsDirectPlayerResponse,
        int remainingRegularPrisoners,
        bool soldierAppeasementRequired,
        bool soldierAppeasementApplied,
        string playerText,
        SiegeCastlePrisonerDispositionKind pendingProposalForSpeaker)
    {
        SpeakerRole = speakerRole;
        ReplyIsDirectPlayerResponse = replyIsDirectPlayerResponse;
        RemainingRegularPrisoners = remainingRegularPrisoners < 0 ? 0 : remainingRegularPrisoners;
        SoldierAppeasementRequired = soldierAppeasementRequired;
        SoldierAppeasementApplied = soldierAppeasementApplied;
        PlayerText = playerText ?? string.Empty;
        PendingProposalForSpeaker = pendingProposalForSpeaker;
    }

    public static SiegeCastlePostprocessRuleFacts Empty => new SiegeCastlePostprocessRuleFacts(
        SiegeCastleActionSpeakerRole.Unknown,
        replyIsDirectPlayerResponse: false,
        remainingRegularPrisoners: 0,
        soldierAppeasementRequired: false,
        soldierAppeasementApplied: false,
        playerText: string.Empty,
        pendingProposalForSpeaker: SiegeCastlePrisonerDispositionKind.None);

    public SiegeCastleActionSpeakerRole SpeakerRole { get; }

    public bool ReplyIsDirectPlayerResponse { get; }

    public int RemainingRegularPrisoners { get; }

    public bool SoldierAppeasementRequired { get; }

    public bool SoldierAppeasementApplied { get; }

    public string PlayerText { get; }

    public SiegeCastlePrisonerDispositionKind PendingProposalForSpeaker { get; }
}
