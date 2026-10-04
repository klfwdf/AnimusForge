using System;
using System.Collections.Generic;

namespace AnimusForge.DiplomacyDialogue;

public static class DialogueRoutingRules
{
    public static DialogueRouteDecision Resolve(DialogueCommitmentRequest request, DialogueAuthoritySnapshot authority,
        DialogueProposalBatch batch, bool isDuplicate = false)
    {
        if (request == null) return Reject("missing_request");
        if (request.Move == DialogueDiplomaticMove.Discussion)
            return new DialogueRouteDecision(DialogueRoute.Discussion, "discussion_is_not_a_commitment");
        if (!Enum.IsDefined(typeof(DialogueDiplomaticMove), request.Move)
            || !Enum.IsDefined(typeof(DialogueDiplomaticAction), request.Action)) return Reject("unsupported_move_or_action");
        if (authority == null || !authority.HasNationalAuthority || authority.IsPlayerAuthor
            || !Same(authority.ActorKingdomId, request.ActorKingdomId)
            || !Same(authority.CurrentRulerId, request.RulerId)) return Reject("current_npc_ruler_authority_required");
        if (string.IsNullOrWhiteSpace(request.ArrangementId) || request.Version <= 0
            || string.IsNullOrWhiteSpace(request.SourceInteractionId)
            || string.IsNullOrWhiteSpace(request.RulerId) || string.IsNullOrWhiteSpace(request.ActorKingdomId)
            || string.IsNullOrWhiteSpace(request.TargetKingdomId)
            || Same(request.ActorKingdomId, request.TargetKingdomId)) return Reject("invalid_commitment_identity");
        if (isDuplicate) return new DialogueRouteDecision(DialogueRoute.Duplicate, "same_arrangement_version");
        if (!authority.IsCurrentActionLegal) return Reject("live_action_not_legal");
        if (request.Move == DialogueDiplomaticMove.NewMatter)
        {
            if (request.ClaimedProposal != null) return Reject("new_matter_cannot_claim_original_proposal");
            if (RequiresTreatyRoles(request.Action) && !TermsMatchParticipants(request))
                return Reject("treaty_roles_must_match_participants");
            return new DialogueRouteDecision(DialogueRoute.NewRound, "new_independent_event_required");
        }
        if (!CanRespondToProposal(request.Action)) return Reject("unilateral_action_has_no_proposal_response");
        if (batch == null || !batch.IsComplete)
            return new DialogueRouteDecision(DialogueRoute.Clarification, "proposal_query_incomplete");
        DialogueProposalSnapshot selected = null;
        HashSet<string> identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (DialogueProposalSnapshot candidate in batch.Proposals)
        {
            if (candidate?.Source == null || !candidate.IsOpen || !candidate.IsKnownByRuler || !candidate.IsCurrentlyExecutable
                || candidate.Action != request.Action || !Same(candidate.ProposerKingdomId, request.TargetKingdomId)
                || !Same(candidate.ResponderKingdomId, request.ActorKingdomId)
                || string.IsNullOrWhiteSpace(candidate.Source.RoundId) || string.IsNullOrWhiteSpace(candidate.Source.DocumentId)) continue;
            if (request.ClaimedProposal != null && (!Same(candidate.Source.DocumentId, request.ClaimedProposal.DocumentId)
                || !Same(candidate.Source.ActionId, request.ClaimedProposal.ActionId)
                || (!string.IsNullOrWhiteSpace(request.ClaimedProposal.RoundId)
                    && !Same(candidate.Source.RoundId, request.ClaimedProposal.RoundId)))) continue;
            string key = candidate.Source.DocumentId + "\n" + candidate.Source.ActionId;
            if (!identities.Add(key))
            {
                // Duplicate delivery is harmless; contradictory snapshots are not.
                if (selected != null && Same(selected.Source.DocumentId, candidate.Source.DocumentId)
                    && Same(selected.Source.ActionId, candidate.Source.ActionId)
                    && (!Same(selected.Source.RoundId, candidate.Source.RoundId) || !selected.Terms.Equals(candidate.Terms)))
                    return Reject("conflicting_proposal_snapshots");
                continue;
            }
            if (selected != null) return new DialogueRouteDecision(DialogueRoute.Clarification, "multiple_matching_proposals");
            selected = candidate;
        }
        if (selected == null) return Reject("proposal_not_known_open_or_valid");
        if (request.Move == DialogueDiplomaticMove.AcceptProposal && !request.Terms.Equals(selected.Terms))
            return Reject("acceptance_cannot_change_original_terms");
        return new DialogueRouteDecision(DialogueRoute.OriginalProposalRound, "exact_source_proposal", selected);
    }

    private static bool TermsMatchParticipants(DialogueCommitmentRequest request) =>
        (Same(request.Terms.ReceivingKingdomId, request.ActorKingdomId) && Same(request.Terms.JoiningKingdomId, request.TargetKingdomId))
        || (Same(request.Terms.ReceivingKingdomId, request.TargetKingdomId) && Same(request.Terms.JoiningKingdomId, request.ActorKingdomId));
    private static bool RequiresTreatyRoles(DialogueDiplomaticAction action) => action == DialogueDiplomaticAction.Annexation
        || action == DialogueDiplomaticAction.Tributary || action == DialogueDiplomaticAction.Garrison || action == DialogueDiplomaticAction.Vassal;
    private static bool CanRespondToProposal(DialogueDiplomaticAction action) => action != DialogueDiplomaticAction.DeclareWar
        && action != DialogueDiplomaticAction.BreakAlliance && action != DialogueDiplomaticAction.CancelTrade;
    private static bool Same(string left, string right) => string.Equals(left ?? "", right ?? "", StringComparison.OrdinalIgnoreCase);
    private static DialogueRouteDecision Reject(string reason) => new DialogueRouteDecision(DialogueRoute.Rejected, reason);
}

/// <summary>A caller-owned arrangement value, persisted inside WorldDiplomacy's
/// existing save owner when runtime integration is enabled. Never stores heroes,
/// private dialogue bodies, game objects, action delegates or a second archive.</summary>
public sealed class DialogueArrangementProgress
{
    public string ArrangementId { get; }
    public int Version { get; }
    public string RulerId { get; }
    public DialogueArrangementStatus Status { get; private set; } = DialogueArrangementStatus.Accepted;
    public string RoundId { get; private set; } = "";
    public string DocumentId { get; private set; } = "";
    public string Reason { get; private set; } = "";
    public DialogueArrangementProgress(string arrangementId, int version, string rulerId)
    { ArrangementId = arrangementId; Version = version; RulerId = rulerId; }

    public bool ApplySubmission(DialogueWorkReceipt receipt)
    {
        if (receipt == null || receipt.ArrangementId != ArrangementId || receipt.Version != Version
            || !receipt.Submitted || string.IsNullOrWhiteSpace(receipt.RoundId)) return false;
        if (Status == DialogueArrangementStatus.Submitted) return receipt.RoundId == RoundId;
        if (Status != DialogueArrangementStatus.Accepted) return false;
        RoundId = receipt.RoundId; Status = DialogueArrangementStatus.Submitted; return true;
    }

    public bool RevalidateUnpublishedRuler(string currentRulerId, bool alive)
    {
        if (Status == DialogueArrangementStatus.Published || Status == DialogueArrangementStatus.Executed) return true;
        if (!alive || currentRulerId != RulerId)
        { Status = DialogueArrangementStatus.Cancelled; Reason = "original_ruler_no_longer_authorized"; return false; }
        return Status == DialogueArrangementStatus.Accepted || Status == DialogueArrangementStatus.Submitted;
    }

    public bool ApplyPublication(int version, string roundId, string documentId, string currentRulerId, bool alive)
    {
        if (version != Version || roundId != RoundId || string.IsNullOrWhiteSpace(documentId)) return false;
        if (Status == DialogueArrangementStatus.Published || Status == DialogueArrangementStatus.Executed)
            return DocumentId == documentId;
        if (Status != DialogueArrangementStatus.Submitted || !RevalidateUnpublishedRuler(currentRulerId, alive)) return false;
        DocumentId = documentId; Status = DialogueArrangementStatus.Published; return true;
    }

    // A publication cannot acknowledge treaty effects. Only the mechanism's
    // confirmed receipt can advance this state; duplicate receipts do no work.
    public bool ApplyConfirmedEffect(int version, string documentId, bool mechanismConfirmed)
    {
        if (version != Version || DocumentId != documentId || !mechanismConfirmed) return false;
        if (Status == DialogueArrangementStatus.Executed) return true;
        if (Status != DialogueArrangementStatus.Published) return false;
        Status = DialogueArrangementStatus.Executed; return true;
    }
}
