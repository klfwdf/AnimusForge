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
                threat.TargetDecision = "noncomplied";
                threat.TargetDecisionDocumentId = document.DocumentId;
                threat.TargetDecisionRoundId = document.RoundId;
                threat.TargetDecisionDay = threat.UpdatedDay = _host.CurrentDay();
                threat.ResolutionReason = "target_did_not_comply_in_first_declaration";
                WorldDiplomacyRoundLifecycleRules.CaptureThreatNonComplianceEvent(threat);
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
