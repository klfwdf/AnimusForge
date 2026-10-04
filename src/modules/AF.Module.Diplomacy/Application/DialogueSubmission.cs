using System;

namespace AnimusForge.DiplomacyDialogue;

public sealed class DialogueSubmissionResult
{
    public DialogueRouteDecision Decision { get; }
    public DialogueWorkReceipt Receipt { get; }
    public bool Submitted => Receipt?.Submitted == true;
    internal DialogueSubmissionResult(DialogueRouteDecision decision, DialogueWorkReceipt receipt = null)
    { Decision = decision; Receipt = receipt; }
}

public static class DialogueSubmission
{
    /// <summary>The interaction adapter calls this after explicit NPC consent,
    /// on the campaign main thread. It does not execute a game effect, publish
    /// player text, maintain a second store, or fall back to oral execution.</summary>
    public static DialogueSubmissionResult Submit(DialogueCommitmentRequest request,
        DialogueAuthoritySnapshot authority, IDiplomacyDialogueRoundPort port, bool isDuplicate = false)
    {
        DialogueRouteDecision decision = DialogueRoutingRules.Resolve(request, authority, null, isDuplicate);
        if (decision.Route == DialogueRoute.Discussion || decision.Route == DialogueRoute.Duplicate
            || decision.Route == DialogueRoute.Rejected) return new DialogueSubmissionResult(decision);
        if (port == null) return Reject("independent_round_port_unavailable");
        try
        {
            if (decision.Route == DialogueRoute.Clarification)
            {
                DialogueProposalBatch proposals = port.QueryAnswerableProposals(request.RulerId,
                    request.ActorKingdomId, request.TargetKingdomId, request.Action, request.ClaimedProposal);
                decision = DialogueRoutingRules.Resolve(request, authority, proposals);
                if (decision.Route != DialogueRoute.OriginalProposalRound) return new DialogueSubmissionResult(decision);
            }
            DialogueWorkReceipt receipt = decision.Route == DialogueRoute.NewRound
                ? port.StartNewRoundFromCommitment(request)
                : port.SubmitFormalResponseToProposal(request, decision.Proposal.Source);
            if (receipt == null || !receipt.Submitted)
                return Reject(receipt?.Reason ?? "formal_submission_receipt_missing");
            if (receipt.ArrangementId != request.ArrangementId || receipt.Version != request.Version
                || string.IsNullOrWhiteSpace(receipt.RoundId)
                || (decision.Route == DialogueRoute.OriginalProposalRound && receipt.RoundId != decision.Proposal.Source.RoundId))
                return Reject("formal_submission_receipt_identity_mismatch");
            return new DialogueSubmissionResult(decision, receipt);
        }
        catch (Exception)
        {
            // The same stable request may be retried only through the port's
            // idempotent submission contract. Never synthesize a success or effect.
            return Reject("formal_submission_port_failed");
        }
    }

    private static DialogueSubmissionResult Reject(string reason) =>
        new DialogueSubmissionResult(new DialogueRouteDecision(DialogueRoute.Rejected, reason));
}
