using System;
using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

public sealed class SiegeCastleActionRoutingFacts
{
    public SiegeCastleActionRoutingFacts(
        string rawActionText,
        SiegeCastleActionSpeakerRole speakerRole,
        bool replyIsDirectPlayerResponse,
        int remainingRegularPrisoners,
        bool soldierAppeasementRequired,
        bool soldierAppeasementApplied,
        string playerText = null,
        SiegeCastlePrisonerDispositionKind pendingProposalForSpeaker = SiegeCastlePrisonerDispositionKind.None,
        int speakerTrust = SiegeCastlePrisonerTrustProfile.DefaultDefeatedGarrisonTrust,
        IReadOnlyCollection<SiegeCastleActionKind> appliedActionsForTarget = null,
        SiegeCastleActionKind terminalActionForTarget = SiegeCastleActionKind.Unknown,
        bool speakerIsClanLeader = false,
        bool playerHasKingdom = false,
        bool playerRulesKingdom = false,
        bool isWitnessReaction = false,
        SiegeCastleActionKind reactionToAction = SiegeCastleActionKind.Unknown)
    {
        RawActionText = rawActionText ?? string.Empty;
        SpeakerRole = speakerRole;
        ReplyIsDirectPlayerResponse = replyIsDirectPlayerResponse;
        RemainingRegularPrisoners = remainingRegularPrisoners < 0 ? 0 : remainingRegularPrisoners;
        SoldierAppeasementRequired = soldierAppeasementRequired;
        SoldierAppeasementApplied = soldierAppeasementApplied;
        PlayerText = playerText ?? string.Empty;
        PendingProposalForSpeaker = pendingProposalForSpeaker;
        SpeakerTrust = SiegeCastlePrisonerTrustProfile.Clamp(speakerTrust);
        AppliedActionsForTarget = appliedActionsForTarget ?? Array.Empty<SiegeCastleActionKind>();
        TerminalActionForTarget = terminalActionForTarget;
        SpeakerIsClanLeader = speakerIsClanLeader;
        PlayerHasKingdom = playerHasKingdom;
        PlayerRulesKingdom = playerRulesKingdom;
        IsWitnessReaction = isWitnessReaction;
        ReactionToAction = reactionToAction;
    }

    public string RawActionText { get; }

    public SiegeCastleActionSpeakerRole SpeakerRole { get; }

    public bool ReplyIsDirectPlayerResponse { get; }

    public int RemainingRegularPrisoners { get; }

    public bool SoldierAppeasementRequired { get; }

    public bool SoldierAppeasementApplied { get; }

    public string PlayerText { get; }

    public SiegeCastlePrisonerDispositionKind PendingProposalForSpeaker { get; }

    public int SpeakerTrust { get; }

    public IReadOnlyCollection<SiegeCastleActionKind> AppliedActionsForTarget { get; }

    public SiegeCastleActionKind TerminalActionForTarget { get; }

    public bool SpeakerIsClanLeader { get; }

    public bool PlayerHasKingdom { get; }

    public bool PlayerRulesKingdom { get; }

    public bool IsWitnessReaction { get; }

    public SiegeCastleActionKind ReactionToAction { get; }

    public bool IsActionAlreadyApplied(SiegeCastleActionKind action)
    {
        foreach (SiegeCastleActionKind applied in AppliedActionsForTarget)
        {
            if (applied == action)
            {
                return true;
            }
        }
        return false;
    }
}
