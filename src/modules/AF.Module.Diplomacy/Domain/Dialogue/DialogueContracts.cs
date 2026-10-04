using System;
using System.Collections.Generic;

namespace AnimusForge.DiplomacyDialogue;

public enum DialogueDiplomaticAction
{
    Peace, Alliance, Trade, DeclareWar, BreakAlliance, CancelTrade,
    Annexation, Tributary, Garrison, Vassal
}

public enum DialogueDiplomaticMove { Discussion, NewMatter, AcceptProposal, RejectProposal }
public enum DialogueRoute { Discussion, Duplicate, NewRound, OriginalProposalRound, Clarification, Rejected }
public enum DialogueArrangementStatus { Accepted, Submitted, Published, Executed, Deferred, Cancelled, Failed }

/// <summary>Stable public values only. A receiving kingdom is the annexation
/// recipient or suzerain; a joining kingdom is the annexed kingdom or subject.</summary>
public sealed class DialogueDiplomaticTerms : IEquatable<DialogueDiplomaticTerms>
{
    public string ReceivingKingdomId { get; }
    public string JoiningKingdomId { get; }
    public string TributePayerKingdomId { get; }
    public string TributeReceiverKingdomId { get; }
    public int DailyTribute { get; }
    public int DurationDays { get; }
    public string CessionFromKingdomId { get; }
    public string CessionToKingdomId { get; }
    public string CessionSettlementId { get; }

    public DialogueDiplomaticTerms(string receivingKingdomId = "", string joiningKingdomId = "",
        string tributePayerKingdomId = "", string tributeReceiverKingdomId = "", int dailyTribute = 0,
        int durationDays = 0, string cessionFromKingdomId = "", string cessionToKingdomId = "", string cessionSettlementId = "")
    {
        if (dailyTribute < 0 || durationDays < 0) throw new ArgumentOutOfRangeException(nameof(dailyTribute));
        ReceivingKingdomId = Clean(receivingKingdomId); JoiningKingdomId = Clean(joiningKingdomId);
        TributePayerKingdomId = Clean(tributePayerKingdomId); TributeReceiverKingdomId = Clean(tributeReceiverKingdomId);
        DailyTribute = dailyTribute; DurationDays = durationDays;
        CessionFromKingdomId = Clean(cessionFromKingdomId); CessionToKingdomId = Clean(cessionToKingdomId);
        CessionSettlementId = Clean(cessionSettlementId);
    }

    private static string Clean(string value) => (value ?? "").Trim();
    public bool Equals(DialogueDiplomaticTerms other) => other != null
        && ReceivingKingdomId == other.ReceivingKingdomId && JoiningKingdomId == other.JoiningKingdomId
        && TributePayerKingdomId == other.TributePayerKingdomId && TributeReceiverKingdomId == other.TributeReceiverKingdomId
        && DailyTribute == other.DailyTribute && DurationDays == other.DurationDays
        && CessionFromKingdomId == other.CessionFromKingdomId && CessionToKingdomId == other.CessionToKingdomId
        && CessionSettlementId == other.CessionSettlementId;
    public override bool Equals(object obj) => Equals(obj as DialogueDiplomaticTerms);
    public override int GetHashCode() => ReceivingKingdomId.GetHashCode() ^ JoiningKingdomId.GetHashCode()
        ^ DailyTribute.GetHashCode() ^ DurationDays.GetHashCode();
}

public sealed class DialogueProposalReference
{
    public string RoundId { get; }
    public string DocumentId { get; }
    public string ActionId { get; }
    public DialogueProposalReference(string roundId, string documentId, string actionId = "")
    {
        RoundId = (roundId ?? "").Trim(); DocumentId = (documentId ?? "").Trim(); ActionId = (actionId ?? "").Trim();
    }
}

public sealed class DialogueProposalSnapshot
{
    public DialogueProposalReference Source { get; }
    public string ProposerKingdomId { get; }
    public string ResponderKingdomId { get; }
    public DialogueDiplomaticAction Action { get; }
    public DialogueDiplomaticTerms Terms { get; }
    public bool IsOpen { get; }
    public bool IsKnownByRuler { get; }
    public bool IsCurrentlyExecutable { get; }
    public DialogueProposalSnapshot(DialogueProposalReference source, string proposerKingdomId, string responderKingdomId,
        DialogueDiplomaticAction action, DialogueDiplomaticTerms terms, bool isOpen, bool isKnownByRuler, bool isCurrentlyExecutable)
    {
        Source = source; ProposerKingdomId = proposerKingdomId; ResponderKingdomId = responderKingdomId;
        Action = action; Terms = terms ?? new DialogueDiplomaticTerms(); IsOpen = isOpen;
        IsKnownByRuler = isKnownByRuler; IsCurrentlyExecutable = isCurrentlyExecutable;
    }
}

/// <summary>The host provides an indexed, knowledge-filtered query. Incomplete
/// batches cannot prove uniqueness, even if they contain only one visible item.</summary>
public sealed class DialogueProposalBatch
{
    public IReadOnlyList<DialogueProposalSnapshot> Proposals { get; }
    public bool IsComplete { get; }
    public DialogueProposalBatch(IEnumerable<DialogueProposalSnapshot> proposals, bool isComplete)
    {
        Proposals = new List<DialogueProposalSnapshot>(proposals ?? Array.Empty<DialogueProposalSnapshot>()).AsReadOnly();
        IsComplete = isComplete;
    }
}

public sealed class DialogueCommitmentRequest
{
    public string ArrangementId { get; }
    public int Version { get; }
    public string SourceInteractionId { get; }
    public string RulerId { get; }
    public string ActorKingdomId { get; }
    public string TargetKingdomId { get; }
    public DialogueDiplomaticAction Action { get; }
    public DialogueDiplomaticMove Move { get; }
    public DialogueDiplomaticTerms Terms { get; }
    public DialogueProposalReference ClaimedProposal { get; }
    public DialogueInteractionOrigin Origin { get; }
    public string SupersedesArrangementId { get; }
    public int SupersedesVersion { get; }
    public string RevisionReason { get; }
    public DialogueCommitmentRequest(string arrangementId, int version, string sourceInteractionId, string rulerId,
        string actorKingdomId, string targetKingdomId, DialogueDiplomaticAction action, DialogueDiplomaticMove move,
        DialogueDiplomaticTerms terms = null, DialogueProposalReference claimedProposal = null,
        DialogueInteractionOrigin origin = null, string supersedesArrangementId = "", int supersedesVersion = 0, string revisionReason = "")
    {
        ArrangementId = arrangementId; Version = version; SourceInteractionId = sourceInteractionId; RulerId = rulerId;
        ActorKingdomId = actorKingdomId; TargetKingdomId = targetKingdomId; Action = action; Move = move;
        Terms = terms ?? new DialogueDiplomaticTerms(); ClaimedProposal = claimedProposal;
        Origin = origin; SupersedesArrangementId = supersedesArrangementId ?? "";
        SupersedesVersion = supersedesVersion; RevisionReason = revisionReason ?? "";
    }
}

public sealed class DialogueAuthoritySnapshot
{
    public string ActorKingdomId { get; }
    public string CurrentRulerId { get; }
    public bool HasNationalAuthority { get; }
    public bool IsPlayerAuthor { get; }
    public bool IsCurrentActionLegal { get; }
    public DialogueAuthoritySnapshot(string actorKingdomId, string currentRulerId, bool hasNationalAuthority,
        bool isPlayerAuthor, bool isCurrentActionLegal)
    {
        ActorKingdomId = actorKingdomId; CurrentRulerId = currentRulerId; HasNationalAuthority = hasNationalAuthority;
        IsPlayerAuthor = isPlayerAuthor; IsCurrentActionLegal = isCurrentActionLegal;
    }
}

public sealed class DialogueRouteDecision
{
    public DialogueRoute Route { get; }
    public string Reason { get; }
    public DialogueProposalSnapshot Proposal { get; }
    internal DialogueRouteDecision(DialogueRoute route, string reason, DialogueProposalSnapshot proposal = null)
    { Route = route; Reason = reason; Proposal = proposal; }
}

public sealed class DialogueWorkReceipt
{
    public bool Submitted { get; }
    public string ArrangementId { get; }
    public int Version { get; }
    public string RoundId { get; }
    public string Reason { get; }
    public DialogueWorkReceipt(bool submitted, string arrangementId, int version, string roundId, string reason = "")
    { Submitted = submitted; ArrangementId = arrangementId; Version = version; RoundId = roundId; Reason = reason; }
}

/// <summary>Implemented by the single WorldDiplomacy storage/scheduler owner.
/// Calls and receipts are on the campaign main thread. Submission atomically
/// revalidates authority, live state, knowledge, proposal identity and version.
/// No default adapter may reuse ActiveRound or wait for it to close.</summary>
public interface IDiplomacyDialogueRoundPort
{
    DialogueProposalBatch QueryAnswerableProposals(string rulerId, string responderKingdomId,
        string proposerKingdomId, DialogueDiplomaticAction action, DialogueProposalReference exactSource = null);
    DialogueWorkReceipt StartNewRoundFromCommitment(DialogueCommitmentRequest request);
    DialogueWorkReceipt SubmitFormalResponseToProposal(DialogueCommitmentRequest request, DialogueProposalReference source);
    void CancelUnpublishedCommitmentWork(string arrangementId, int version, string reason);
}
