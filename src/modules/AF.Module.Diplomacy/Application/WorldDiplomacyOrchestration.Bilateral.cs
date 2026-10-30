using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using AnimusForge.DiplomacyDialogue;

namespace AnimusForge;

internal sealed class DiplomacyBilateralResult
{
    internal string State, Actor, Target, Diagnostic;
    internal WorldDiplomacyDialogueTerms Terms = new();
}

internal sealed partial class WorldDiplomacyOrchestration
{
    private WorldDiplomacyStorage _directResultIndexOwner;
    private int _directResultIndexCount = -1;
    private readonly Dictionary<string,WorldDiplomacyDialogueArrangement> _directResults = new(StringComparer.Ordinal);
    private WorldDiplomacyDialogueArrangement DirectDocumentResult(WorldDiplomacyDocument document)
    {
        if(document?.IsReadyForPublication!=true || string.IsNullOrEmpty(document.DialogueArrangementId))return null;
        var rows=Storage.DialogueArrangements;
        if(!ReferenceEquals(_directResultIndexOwner,Storage) || _directResultIndexCount!=rows.Count)
        {
            _directResults.Clear();foreach(var row in rows)
                if(!string.IsNullOrEmpty(row.DirectExecutionState))_directResults[row.ArrangementId]=row;
            _directResultIndexOwner=Storage;_directResultIndexCount=rows.Count;
        }
        return _directResults.TryGetValue(document.DialogueArrangementId,out var item)
            && item.DirectExecutionState=="applied" && item.Version==document.DialogueArrangementVersion
            && item.DocumentId==document.DocumentId && item.RoundId==document.RoundId ? item:null;
    }
    private bool HasConfirmedDocumentResult(WorldDiplomacyDocument document) => document?.ChangedDiplomaticState==true || DirectDocumentResult(document)!=null;
    private bool IsDirectWarResult(WorldDiplomacyDocument document) => DirectDocumentResult(document)?.Action==DialogueDiplomaticAction.DeclareWar;
    private string ConfirmedDocumentResultText(WorldDiplomacyDocument document) => DirectDocumentResult(document) is WorldDiplomacyDialogueArrangement item
        ? DirectResultContext(item):document.MechanicalResult;
    private JObject DirectDocumentEvidence(WorldDiplomacyDocument document)
    {
        var item=DirectDocumentResult(document);
        return item==null?null:JObject.FromObject(new { source=item.ArrangementId,state=item.DirectExecutionState,
            actor=item.DirectActorKingdomId,target=item.DirectTargetKingdomId,action=item.Action.ToString(),actual_terms=item.Terms,
            announcement_author=item.ActorKingdomId,executed_before_publication=true });
    }
    internal bool IsBilateralConversation(string ruler, string actor) => _host.WorldDiplomacyEnabled()
        && !string.IsNullOrEmpty(CurrentPlayerAuthority()) && actor != CurrentPlayerAuthority()
        && DialogueRulerIsCurrent(actor, ruler) && _host.HasIndependentAuthority(actor);

    internal bool OwnsDirectBilateralMatter(DialogueTagPayload parsed, string actor, string player) =>
        DiplomacyBilateralRouting.OwnsMatter(parsed, actor, player)
        && !RequiresFormalBilateralSource(parsed.SourceDocumentId, parsed.SourceActionId);

    internal bool TryProcessBilateralTag(string ruler, string actor, string tag, string payload,
        DialogueInteractionOrigin origin, Func<DiplomacyBilateralPayload, DiplomacyBilateralResult> apply)
    {
        if (!IsBilateralConversation(ruler, actor)) return false;
        string player = CurrentPlayerAuthority();
        if (string.Equals(tag, "COMMITMENT", StringComparison.OrdinalIgnoreCase))
        { ControlOralDiplomaticCommitment(ruler, actor, payload, origin); return true; }
        DiplomacyBilateralPayload request;
        if (string.Equals(tag, "COMMIT", StringComparison.OrdinalIgnoreCase))
        {
            if (!DialogueTagPayload.TryParse(payload, out var parsed) || !OwnsDirectBilateralMatter(parsed, actor, player)) return false;
            if (origin?.PlayerConsentConfirmed == false && parsed.Action is DialogueDiplomaticAction.Peace or DialogueDiplomaticAction.Alliance or DialogueDiplomaticAction.Trade)
            { _host.Log("bilateral routing rejected: player_consent_not_confirmed"); return true; }
            if (!DiplomacyBilateralPayload.TryFromCommit(parsed, player, actor, out request))
            { _host.Log("bilateral routing rejected: incomplete_or_non_equivalent_commit"); return true; }
            _host.Log("diplomacy route=bilateral normalized=COMMIT action=" + request.Action);
        }
        else
        {
            if (!DiplomacyBilateralRouting.TryAction(tag, out _)) return false;
            string[] p = (payload ?? "").Split('|')[0].Split(':');
            if (p.Length < 2 || !DiplomacyBilateralRouting.IsPair(p[0].Trim(), p[1].Trim(), player, actor)) return false;
            if (!DiplomacyBilateralPayload.TryParse(tag, payload, player, actor, out request))
            { _host.Log("bilateral payload rejected action=" + tag); return true; }
        }
        ExecuteBilateral(ruler, actor, request.Action, origin, () => apply(request), request);
        return true;
    }

    private bool RequiresFormalBilateralSource(string documentId, string actionId)
    {
        if (string.IsNullOrEmpty(documentId)) return false;
        var acceptance = new WorldDiplomacyDocumentAction { ActionId = "direct_result_acceptance",
            RespondingToOfferDocumentId = documentId, RespondingToOfferActionId = actionId };
        // Reuse the existing condition authority, including reverse dependencies.
        // A coupled exchange cannot be reduced to one irreversible direct effect.
        return DiplomacyConditionalExecutionRules.DeferredActions(new[] { acceptance }, ResolveDocument).Count > 0;
    }

    internal void ExecuteBilateral(string ruler, string actor, DialogueDiplomaticAction action,
        DialogueInteractionOrigin origin, Func<DiplomacyBilateralResult> execute, DiplomacyBilateralPayload request = null)
    {
        if (!_persistenceHealthy || !IsBilateralConversation(ruler, actor) || origin == null
            || string.IsNullOrEmpty(origin.InteractionId) || string.IsNullOrWhiteSpace(origin.FullPlayerText)
            || string.IsNullOrWhiteSpace(origin.FullNpcText)) return;
        // One action kind per pair/interaction. Different payloads cannot turn a
        // duplicate into a second payment or reverse an earlier effect this turn.
        string player = CurrentPlayerAuthority();
        string key = "direct|" + origin.Channel + "|" + origin.SessionId + "|" + origin.InteractionId
            + "|" + ruler + "|" + player + "|" + actor + "|" + action;
        if (Storage.DialogueArrangements.Any(x => x.Fingerprint == key)) return;
        if (Storage.DialogueArrangements.Any(x => x.Action == action
            && DiplomacyBilateralRouting.IsPair(x.ActorKingdomId, x.TargetKingdomId, player, actor)
            && x.DirectExecutionState is "started" or "partial" or "unknown"))
        { _host.Log("bilateral held by unresolved execution action=" + action); return; }
        WorldDiplomacyDocument linkedDocument = null;
        if (!string.IsNullOrEmpty(request?.SourceDocument))
        {
            linkedDocument = ResolveDocument(request.SourceDocument);
            var linkedRound = ResolveRound(linkedDocument?.RoundId);
            var offer = linkedRound?.PendingOffers.SingleOrDefault(x => x.SourceDocumentId == request.SourceDocument
                && x.SourceActionId == request.SourceAction && x.Status == "open" && x.Intent == DialogueIntent(action)
                && DiplomacyBilateralRouting.IsPair(x.ProposerKingdomId,x.TargetKingdomId,player,actor));
            if (linkedDocument?.IsReadyForPublication != true || !IsLiveRound(linkedRound) || offer == null
                || linkedDocument.AuthorKingdomId != offer.ProposerKingdomId
                || (linkedDocument.Actions?.Count > 0
                    ? !linkedDocument.Actions.Any(x => x.ActionId == request.SourceAction && x.TargetKingdomId == offer.TargetKingdomId)
                    : request.SourceAction != "" || linkedDocument.TargetKingdomId != offer.TargetKingdomId)
                || !DialogueDocumentKnown(actor, linkedDocument.DocumentId)
                || RequiresFormalBilateralSource(request.SourceDocument, request.SourceAction)
                || !SameDirectSourceTerms(ResolveDialogueTerms(linkedDocument, request.SourceAction), request.Terms.ToTerms()))
            { _host.Log("bilateral source rejected: source, terms, knowledge or open offer mismatch"); return; }
        }
        // Ordinary formal proposals need not have an oral-arrangement record.
        // Omitting a source binding must not bypass their existing conditions or
        // leave a conflicting live proposal silently orphaned after execution.
        if (linkedDocument == null && GetLiveRounds().Any(round => IsLiveRound(round) && round.PendingOffers.Any(offer => offer.Status == "open"
            && offer.Intent == DialogueIntent(action)
            && DiplomacyBilateralRouting.IsPair(offer.ProposerKingdomId, offer.TargetKingdomId, player, actor)
            && ResolveDocument(offer.SourceDocumentId)?.IsReadyForPublication == true
            && DialogueDocumentKnown(actor, offer.SourceDocumentId))))
        { _host.Log("bilateral held by unlinked live proposal action=" + action); return; }
        // Do not guess links to an older offer. A conflicting pending promise
        // holds the new action until that exact work is resolved explicitly.
        if (Storage.DialogueArrangements.Any(x => x.DirectExecutionState == "" && x.ActorKingdomId == actor
            && x.TargetKingdomId == player && x.Action == action && (x.Status is "accepted" or "deferred"
                || linkedDocument == null && x.Status == "published" && IsLiveRound(ResolveRound(x.RoundId))
                    && ResolveRound(x.RoundId).PendingOffers.Any(o => o.SourceDocumentId == x.DocumentId && o.Status == "open"))
            && (linkedDocument == null || x.DocumentId != linkedDocument.DocumentId
                && (x.SourceDocumentId != linkedDocument.DocumentId || x.SourceActionId != request.SourceAction))))
        { _host.Log("bilateral blocked by existing unlinked commitment action=" + action); return; }
        var item = new WorldDiplomacyDialogueArrangement {
            ArrangementId = _host.NewId("oral_result"), Fingerprint = key,
            RulerId = ruler, ActorKingdomId = actor, TargetKingdomId = player, Action = action,
            Move = DialogueDiplomaticMove.NewMatter, SourceChannel = origin.Channel,
            SourceSessionId = origin.SessionId, SourceInteractionId = origin.InteractionId,
            SourcePlayerText = origin.PlayerText, SourceNpcText = origin.NpcText,
            SourceDocumentId = linkedDocument?.DocumentId ?? "", SourceActionId = request?.SourceAction ?? "",
            RoundId = linkedDocument?.RoundId ?? "",
            CreatedDay = _host.CurrentDay(), Status = "execution_uncertain", DirectExecutionState = "started" };
        Storage.DialogueArrangements.Add(item);
        DiplomacyBilateralResult result;
        try { result = execute(); }
        catch (Exception ex) { result = new() { State = "unknown", Diagnostic = ex.GetType().Name }; }
        if (result == null) result = new() { State="unknown",Diagnostic="missing_execution_receipt" };
        if(result.State=="applied" && !DiplomacyBilateralRouting.IsPair(result.Actor,result.Target,player,actor))
        {result.State="unknown";result.Diagnostic="execution_receipt_pair_mismatch";}
        item.DirectExecutionState = result.State;
        item.DirectActorKingdomId = result.Actor ?? ""; item.DirectTargetKingdomId = result.Target ?? "";
        item.DirectExecutionDiagnostic = result.Diagnostic ?? ""; item.Terms = result.Terms;
        item.Status = result.State == "applied" ? "accepted" : result.State == "rejected" ? "rejected" : "execution_uncertain";
        if(result.State is "partial" or "unknown") HoldDirectResultSource(item);
        _host.Log("bilateral receipt id=" + item.ArrangementId + " action=" + action + " state=" + result.State + " diagnostic=" + result.Diagnostic);
        if (result.State != "applied")
        {
            RecordDialogueArrangementFact(item, "direct_" + result.State,
                DiplomacyPromptTexts.Get("Bilateral_Result_" + result.State, "Diplomatic action was not confirmed complete."));
            return;
        }
        // Fact is committed before any creative work. Failure here can retry
        // fact delivery/publication, never the diplomatic action.
        RecoverDirectResultPublication(item);
    }

    private static bool SameDirectSourceTerms(DialogueDiplomaticTerms source, DialogueDiplomaticTerms requested)
    {
        var a=WorldDiplomacyDialogueTerms.From(source);var b=WorldDiplomacyDialogueTerms.From(requested);
        if(a.DailyTribute==0)a.TributePayerKingdomId=a.TributeReceiverKingdomId="";
        if(b.DailyTribute==0)b.TributePayerKingdomId=b.TributeReceiverKingdomId="";
        return a.ToTerms().Equals(b.ToTerms());
    }

    private void HoldDirectResultSource(WorldDiplomacyDialogueArrangement item)
    {
        if(string.IsNullOrEmpty(item.SourceDocumentId))return;
        var offer=ResolveRound(item.RoundId)?.PendingOffers.FirstOrDefault(x=>x.SourceDocumentId==item.SourceDocumentId && x.SourceActionId==item.SourceActionId);
        if(offer?.Status=="open")offer.Status="execution_uncertain";
        foreach(var previous in Storage.DialogueArrangements.Where(x=>x!=item && string.IsNullOrEmpty(x.DirectExecutionState)
            && x.Action==item.Action && DiplomacyBilateralRouting.IsPair(x.ActorKingdomId,x.TargetKingdomId,item.ActorKingdomId,item.TargetKingdomId)
            && (x.DocumentId==item.SourceDocumentId || x.SourceDocumentId==item.SourceDocumentId && x.SourceActionId==item.SourceActionId)))
        {
            previous.Status="execution_uncertain";previous.TreatyExecutionState="uncertain";
            foreach(var job in Storage.Jobs.Where(x=>x.DialogueArrangementId==previous.ArrangementId).ToList())RemoveJob(job.JobId);
            var document=ResolveDocument(previous.DocumentId);if(document!=null)document.EventExecutionPending=false;
        }
    }

    private void RecoverDirectResultPublication(WorldDiplomacyDialogueArrangement item)
    {
        if (item.DirectExecutionState != "applied") return;
        try
        {
            // Same stable fact ID deduplicates the existing durable memory receipt.
            RecordDialogueArrangementFact(item, "direct_applied", DirectResultContext(item));
            CompleteDirectResultSource(item);
            if (string.IsNullOrEmpty(item.RoundId))
            {
                var existing = GetLiveRounds().FirstOrDefault(x => x.DialogueArrangementId == item.ArrangementId);
                item.RoundId = (existing ?? CreateIndependentDialogueRound(item.ActorKingdomId, item.TargetKingdomId, item.ArrangementId)).RoundId;
            }
            PublishDialogueArrangement(item);
            // The legacy publication boundary may contain its own exception or
            // decline invalid authorship. Back off only if no owner took work.
            if (item.Status is "accepted" or "deferred" && item.GenerationRetryAfterDay <= _host.CurrentDay()
                && ResolveDocument(item.DocumentId)?.EventExecutionPending != true
                && !Storage.Jobs.Any(x => x.DialogueArrangementId == item.ArrangementId && x.DialogueArrangementVersion == item.Version))
                DeferDirectResultRecovery(item);
        }
        catch { DeferDirectResultRecovery(item); throw; }
    }

    private void DeferDirectResultRecovery(WorldDiplomacyDialogueArrangement item)
    {
        item.GenerationFailureAttempts++;
        item.GenerationRetryAfterDay = DiplomacyCommitmentRules.RetryDay(_host.CurrentDay(), item.GenerationFailureAttempts);
    }

    private void CompleteDirectResultSource(WorldDiplomacyDialogueArrangement item)
    {
        if (string.IsNullOrEmpty(item.SourceDocumentId)) return;
        var round=ResolveRound(item.RoundId);
        var offer=round?.PendingOffers.FirstOrDefault(x=>x.SourceDocumentId==item.SourceDocumentId
            && x.SourceActionId==item.SourceActionId && x.Intent==DialogueIntent(item.Action)
            && DiplomacyBilateralRouting.IsPair(x.ProposerKingdomId,x.TargetKingdomId,item.ActorKingdomId,item.TargetKingdomId));
        if(offer==null)return;
        if(offer.Status=="open")offer.Status="accepted";
        foreach(var previous in Storage.DialogueArrangements.Where(x=>x!=item && string.IsNullOrEmpty(x.DirectExecutionState)
            && x.Action==item.Action && DiplomacyBilateralRouting.IsPair(x.ActorKingdomId,x.TargetKingdomId,item.ActorKingdomId,item.TargetKingdomId)
            && (x.DocumentId==item.SourceDocumentId || x.SourceDocumentId==item.SourceDocumentId && x.SourceActionId==item.SourceActionId)))
        {
            previous.Status="executed";previous.TreatyExecutionState="satisfied";previous.Reason="direct_result:"+item.ArrangementId;
            foreach(var pending in Storage.Jobs.Where(x=>x.DialogueArrangementId==previous.ArrangementId).ToList())RemoveJob(pending.JobId);
            var document=ResolveDocument(previous.DocumentId);if(document!=null)document.EventExecutionPending=false;
        }
        // Whole-document obligations are discharged only for a single-action
        // source. Other actions in a multi-action document keep their own reply.
        var source=ResolveDocument(item.SourceDocumentId);
        if((source?.Actions?.Count??0)<=1)
        {
            foreach(var response in round.PlayerResponses.Where(x=>x.SourceDocumentId==item.SourceDocumentId && x.Status=="pending"
                && (x.KingdomId==item.ActorKingdomId || x.KingdomId==item.TargetKingdomId)))response.Status="answered";
            foreach(var participant in round.Participants.Where(x=>x.LastTriggeredDocumentId==item.SourceDocumentId
                && (x.KingdomId==item.ActorKingdomId || x.KingdomId==item.TargetKingdomId)))participant.MandatoryReplyPending=false;
        }
        InvalidateDialogueIndex();
    }

    private string DirectResultContext(WorldDiplomacyDialogueArrangement item) => DiplomacyPromptTexts.Format(
        "Bilateral_Result", "Executed result: {ACTOR} → {TARGET}; action={ACTION}. {TERMS}",
        "ACTOR", _host.PartyNameOrEmpty(item.DirectActorKingdomId), "TARGET", _host.PartyNameOrEmpty(item.DirectTargetKingdomId),
        "ACTION", DiplomacyPromptTexts.Get("CommittedDeclaration_Action_" + item.Action, item.Action.ToString()),
        "TERMS", DescribeDialogueTerms(item.Terms.ToTerms()));
}
