using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using AnimusForge.Refactor.Domain;
using static AnimusForge.Refactor.Domain.WorldDiplomacyStructureRules;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    public int ApplyNationalPrestigeDelta(string kingdomId, int delta, WorldDiplomacyDocument document, string reason)
    {
        var storage = Storage;
        if (_host is not IWorldDiplomacyThreatMechanicsHost host)
            return WorldDiplomacyReputationRules.GetNationalPrestige(storage.NationalPrestigeByKingdom, kingdomId);
        return WorldDiplomacyPrestigeApplication.Apply(ref storage, host.Prestige(), kingdomId, delta, document, reason);
    }

    public void ApplyZeroPrestigeBreachRelationPenalty(string kingdomId, int amount)
    {
        if (_host is IWorldDiplomacyThreatMechanicsHost host)
            WorldDiplomacyPrestigeApplication.ApplyZeroPrestigePenalty(host.Prestige(), kingdomId, amount);
    }

    internal void ReconcileHiddenPrestige()
    {
        if (_host is IWorldDiplomacyThreatMechanicsHost host)
        {
            WorldDiplomacyPrestigeApplication.ReconcileAll(Storage, host.Prestige());
            WorldDiplomacyThreatSettlementApplication.RetryDiplomaticThreatDomesticPenalties(Storage, _host.ThreatSettlement(), this);
        }
    }

    internal void SettleThreatDeclaration(WorldDiplomacyDocument document)
    {
        if (document == null || document.LegacyNoticeSuppressed || document.ThreatConsequencesSettled
            || _host is not IWorldDiplomacyThreatMechanicsHost) return;
        var threats = WorldDiplomacyThreatScope.For(Storage, document.AuthorKingdomId, document.RoundId).ToList();
        var targets = GetDocumentTargetIds(document);
        foreach (var threat in threats)
        {
            string other = threat.IssuerKingdomId == document.AuthorKingdomId ? threat.TargetKingdomId : threat.IssuerKingdomId;
            if (!targets.Contains(other)) continue;
            if (threat.TargetKingdomId == document.AuthorKingdomId
                && document.PresentedThreatDocumentIds?.Contains(threat.StageDocumentId) == true
                && threat.TargetDecision == "pending")
            {
                var compliance = (document.Actions ?? new()).FirstOrDefault(x => WorldDiplomacyRoundLifecycleRules.IsThreatComplianceAction(x, threat));
                if (compliance != null || WorldDiplomacyIntentVocabulary.NormalizeIntent(document.Intent) == "comply_ultimatum") continue;
                RecordThreatNonCompliance(threat, document, _host.CurrentDay());
            }
            if (threat.IssuerKingdomId != document.AuthorKingdomId
                || document.PresentedThreatFollowThroughDocumentIds?.Contains(threat.StageDocumentId) != true) continue;
            var action = WorldDiplomacyRoundLifecycleRules.SelectThreatDecisionAction(document.Actions, threat.TargetKingdomId);
            var result = WorldDiplomacyThreatStateRules.EvaluateIssuerFollowThrough(threat.TargetDecision, threat.Stage,
                threat.StageDocumentId, true, WorldDiplomacyIntentVocabulary.NormalizeIntent(action?.Intent ?? document.Intent),
                true, action?.ChangedDiplomaticState ?? document.ChangedDiplomaticState);
            if (result == WorldDiplomacyThreatStateRuleResult.MarkFollowThroughBreached)
                WorldDiplomacyThreatSettlementApplication.ApplyDiplomaticThreatReputationPenalty(Storage, _host.ThreatSettlement(), this, threat, document);
            else if (result == WorldDiplomacyThreatStateRuleResult.DeferFollowThroughForTechnicalFailure)
                _host.Log("threat follow-through deferred after technical failure threat=" + threat.ThreatId + " document=" + document.DocumentId);
        }
        document.ThreatConsequencesSettled = true;
    }

    private static void RecordThreatNonCompliance(WorldDiplomacyThreat threat, WorldDiplomacyDocument document, int day)
    {
        threat.TargetDecision = "noncomplied";
        threat.TargetDecisionDocumentId = document.DocumentId;
        threat.TargetDecisionRoundId = document.RoundId;
        threat.TargetDecisionDay = threat.UpdatedDay = day;
        threat.ResolutionReason = "target_did_not_comply_in_first_declaration";
        WorldDiplomacyRoundLifecycleRules.CaptureThreatNonComplianceEvent(threat);
    }

    // Publication/routing only. Reply ownership is independent of whether the
    // model considers the player's words relevant or cooperative.
    private void BindPlayerThreatAnswerSources(WorldDiplomacyDocument reply)
    {
        if (reply?.IsPlayerAuthored != true || !reply.IsReadyForPublication || reply.LegacyNoticeSuppressed
            || reply.AnalysisStatus == "pending_analysis" || reply.AuthorKingdomId != CurrentPlayerAuthority()) return;
        var explicitSources = new HashSet<string>((reply.AnsweredNpcDocumentIds ?? new()).Concat(new[] {
            reply.SourceDocumentId, reply.DiscussionSourceDocumentId, reply.RespondingToThreatDocumentId })
            .Concat((reply.Actions ?? new()).Where(x => x != null).Select(x => x.RespondingToThreatDocumentId))
            .Where(x => !string.IsNullOrEmpty(x)), StringComparer.OrdinalIgnoreCase);
        var candidates = Storage.DiplomaticThreats.Where(x => x.Status == "open" && x.TargetDecision == "pending"
            && x.TargetKingdomId == reply.AuthorKingdomId && IsLiveRound(ResolveRound(x.StageRoundId))).ToList();
        bool exact = candidates.Any(x => explicitSources.Contains(x.StageDocumentId));
        var targets = GetDocumentTargetIds(reply);
        foreach (var threat in candidates)
        {
            bool bound = explicitSources.Contains(threat.StageDocumentId);
            if (!bound && (exact || threat.StageRoundId != reply.RoundId || !targets.Contains(threat.IssuerKingdomId))) continue;
            var source = ResolveDocument(threat.StageDocumentId);
            if (!IsPublishedPlayerThreatAnswer(reply, source, threat) || !PlayerKnowsPublicSource(source, reply)) continue;
            reply.AnsweredNpcDocumentIds ??= new();
            if (!reply.AnsweredNpcDocumentIds.Contains(source.DocumentId)) reply.AnsweredNpcDocumentIds.Add(source.DocumentId);
        }
    }

    private bool IsPublishedPlayerThreatAnswer(WorldDiplomacyDocument reply, WorldDiplomacyDocument source, WorldDiplomacyThreat threat) =>
        reply?.IsPlayerAuthored == true && reply.IsReadyForPublication && !reply.LegacyNoticeSuppressed
        && reply.AnalysisStatus is "success" or "published_action_rejected" && !reply.EventExecutionPending
        && reply.AuthorKingdomId == CurrentPlayerAuthority() && threat.TargetKingdomId == reply.AuthorKingdomId
        && source?.IsReadyForPublication == true && !source.LegacyNoticeSuppressed
        && source.AuthorKingdomId == threat.IssuerKingdomId && source.RoundId == threat.StageRoundId
        && source.DocumentId != reply.DocumentId && source.Day <= reply.Day
        && (source.CreatedUtcTicks <= 0 || reply.CreatedUtcTicks <= 0 || source.CreatedUtcTicks <= reply.CreatedUtcTicks)
        && IsLiveRound(ResolveRound(threat.StageRoundId))
        && string.IsNullOrEmpty(_host.ValidateOpenThreatWorldEligibility(threat));

    private void ReconcilePlayerThreatAnswer(WorldDiplomacyDocument reply, WorldDiplomacyDocument source)
    {
        if (_host is not IWorldDiplomacyThreatMechanicsHost) return;
        foreach (var threat in Storage.DiplomaticThreats.Where(x => x.Status == "open" && x.TargetDecision == "pending"
            && x.StageDocumentId == source.DocumentId && x.StageRoundId == reply.RoundId))
        {
            if (!IsPublishedPlayerThreatAnswer(reply, source, threat)) continue;
            // Compliance stays with its existing validated execution owner. Never
            // convert an unfinished/failed compliance attempt into a refusal.
            if ((reply.Actions ?? new()).Any(x => WorldDiplomacyRoundLifecycleRules.IsThreatComplianceAction(x, threat))
                || reply.Intent == "comply_ultimatum" && reply.RespondingToThreatDocumentId == source.DocumentId) continue;
            RecordThreatNonCompliance(threat, reply, reply.Day);
            try
            {
                WorldDiplomacyThreatHistoryApplication.FinalizeDiplomaticThreatNonComplianceHistoryAfterDocument(
                    reply, new[] { threat }, TryAppendDiplomaticThreatNonComplianceHistoryResult);
            }
            catch (Exception ex)
            {
                ScheduleDeferredCanonicalHistoryRetry(reply.DocumentId);
                _host.Log("player threat answer history deferred document=" + reply.DocumentId + " error=" + ex.Message);
            }
            _host.Log("diplomacy.player_threat_answer_reconciled threat=" + threat.ThreatId + " document=" + reply.DocumentId);
        }
    }

    private void RecoverPublishedPlayerThreatAnswers()
    {
        // Load boundary: only an existing exact answer receipt can repair an old
        // pending threat. Do not reclassify archived prose or reopen closed cases.
        foreach (var threat in Storage.DiplomaticThreats.Where(x => x.Status == "open" && x.TargetDecision == "pending"
            && x.TargetKingdomId == CurrentPlayerAuthority()).ToList())
        {
            var round = ResolveRound(threat.StageRoundId);
            if (!IsLiveRound(round)) continue;
            var wait = round.PlayerNegotiationWaits.FirstOrDefault(x => x.SourceDocumentId == threat.StageDocumentId
                && !string.IsNullOrEmpty(x.AnswerDocumentId) && string.IsNullOrEmpty(x.ClosureDocumentId));
            var reply = ResolveDocument(wait?.AnswerDocumentId); var source = ResolveDocument(threat.StageDocumentId);
            if (!IsPublishedPlayerThreatAnswer(reply, source, threat) || reply.RoundId != round.RoundId) continue;
            ReconcilePlayerThreatAnswer(reply, source);
            MarkEventDirty(round);
        }
    }

    private string BuildHiddenThreatContext(WorldDiplomacyJob job)
    {
        var threats = WorldDiplomacyThreatScope.For(Storage, job.AuthorKingdomId, job.RoundId).ToList();
        return DiplomacyPromptTexts.Get("HiddenThreatContract", "") + "\n" + JsonConvert.SerializeObject(new {
            hidden_national_prestige = WorldDiplomacyReputationRules.GetNationalPrestige(Storage.NationalPrestigeByKingdom, job.AuthorKingdomId),
            obligations = threats.Select(x => new { x.IssuerKingdomId, x.TargetKingdomId, x.Stage, x.TargetDecision,
                x.StageDocumentId, x.StageActionId, x.TargetDecisionDocumentId,
                expected_follow_through = x.Stage == "warning" ? "ultimatum" : "declare_war" })
        });
    }
}

// Bounded threat set, evaluated only at request/publication boundaries. Knowledge
// must include the rejection before an issuer acquires a follow-through duty.
internal static class WorldDiplomacyThreatScope
{
    internal static IEnumerable<WorldDiplomacyThreat> For(WorldDiplomacyStorage storage, string actor, string round)
    {
        bool Known(string id) => !string.IsNullOrEmpty(id) && (
            storage.KingdomKnowledge?.Any(x => x.KingdomId == actor && x.DocumentIds?.Contains(id) == true) == true
            || storage.NobleKnowledge?.Any(x => x.KingdomId == actor && x.DocumentIds?.Contains(id) == true) == true);
        return (storage?.DiplomaticThreats ?? new()).Where(x => x != null && x.Status == "open" && x.StageRoundId == round
            && (x.IssuerKingdomId == actor || x.TargetKingdomId == actor)
            && (x.IssuerKingdomId == actor || Known(x.StageDocumentId))
            && (x.IssuerKingdomId != actor || x.TargetDecision != "noncomplied" || Known(x.TargetDecisionDocumentId)));
    }
}
