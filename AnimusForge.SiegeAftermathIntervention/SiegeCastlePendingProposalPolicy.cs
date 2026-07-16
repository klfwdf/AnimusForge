namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Decides when a pending allied-soldier or regular-prisoner proposal must be discarded before the next postprocess pass.
/// </summary>
public static class SiegeCastlePendingProposalPolicy
{
    public static SiegeCastlePendingProposalDecision Evaluate(
        SiegeCastlePrisonerDispositionKind pendingProposal,
        bool replyIsDirectPlayerResponse,
        string playerText)
    {
        if (pendingProposal == SiegeCastlePrisonerDispositionKind.None)
        {
            return SiegeCastlePendingProposalDecision.Keep("no_pending_proposal");
        }

        if (!replyIsDirectPlayerResponse)
        {
            return SiegeCastlePendingProposalDecision.Keep("indirect_reply_does_not_change_proposal");
        }

        SiegeCastlePlayerAuthorizationDecision authorization = SiegeCastlePlayerAuthorizationPolicy.Evaluate(
            playerText,
            pendingProposal);
        if (authorization.ReasonCode == "player_rejected_or_cancelled")
        {
            return SiegeCastlePendingProposalDecision.Clear("player_rejected_pending_proposal");
        }

        if (authorization.IsAuthorized && authorization.Disposition != pendingProposal)
        {
            return SiegeCastlePendingProposalDecision.Clear("player_authorized_different_disposition");
        }

        return SiegeCastlePendingProposalDecision.Keep("pending_proposal_still_relevant");
    }
}

public sealed class SiegeCastlePendingProposalDecision
{
    private SiegeCastlePendingProposalDecision(bool shouldClear, string reasonCode)
    {
        ShouldClear = shouldClear;
        ReasonCode = reasonCode ?? string.Empty;
    }

    public bool ShouldClear { get; }

    public string ReasonCode { get; }

    internal static SiegeCastlePendingProposalDecision Keep(string reasonCode)
    {
        return new SiegeCastlePendingProposalDecision(false, reasonCode);
    }

    internal static SiegeCastlePendingProposalDecision Clear(string reasonCode)
    {
        return new SiegeCastlePendingProposalDecision(true, reasonCode);
    }
}
