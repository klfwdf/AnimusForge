using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Document-driven threat and pressure transitions over the canonical state.
// Only the host's synchronous world probe and effect port touch Bannerlord.
internal static class WorldDiplomacyThreatApplication
{
    internal static void SettleDiplomaticThreatFollowThroughAfterDeclaration(
        WorldDiplomacyDocument document,
        IReadOnlyList<WorldDiplomacyThreat> threats, string authorKingdomId,
        Action<WorldDiplomacyThreat, WorldDiplomacyDocument> applyReputationPenalty)
    {
        WorldDiplomacyThreat breached = WorldDiplomacyRoundLifecycleRules.SelectBreachedThreatFollowThrough(
            document, threats, authorKingdomId);
        if (breached != null) applyReputationPenalty?.Invoke(breached, document);
    }

    internal static void ApplyDocumentPressure(
		WorldDiplomacyDocument document,
		Func<string, string, WarPressureEntry> findPressure,
		Func<IEnumerable<string>, string, List<string>> normalizeKingdomIds,
		Action<string, string, int, string, string> applyPressure)
	{
		if (document == null || string.IsNullOrWhiteSpace(document.AuthorKingdomId)
			|| findPressure == null || normalizeKingdomIds == null || applyPressure == null)
		{
			return;
		}
		foreach (string targetId in normalizeKingdomIds((document.AddressedKingdomIds ?? new List<string>()).Concat(new[] { document.TargetKingdomId }), document.AuthorKingdomId))
		{
			WarPressureEntry existing = findPressure(document.AuthorKingdomId, targetId);
            int scaledDelta = WorldDiplomacyWarPressureRules.CalculateDocumentPressureDelta(
                document.Intent, document.Tone, existing?.LastIntent, existing?.ConsecutiveSimilarCount ?? 0);
			if (scaledDelta != 0) applyPressure(document.AuthorKingdomId, targetId, scaledDelta, "外交宣言：" + document.Title, document.Intent);
		}
	}

    internal static void ApplyPressure(
        WorldDiplomacyDocument document,
        Func<(bool Valid, string AuthorId, string TargetId)> resolveParties,
        Action<string, string, int, string, string> addWarPressure)
    {
        if (document == null || !string.IsNullOrWhiteSpace(document.MechanicalResult)) return;
        string intent = WorldDiplomacyIntentVocabulary.NormalizeIntent(document.Intent);
        if (intent != "apology" && intent != "concession") return;
        (bool valid, string authorId, string targetId) = resolveParties();
        if (!valid) return;
        int reduction = intent == "concession" ? -22 : -16;
        addWarPressure(authorId, targetId, reduction,
            "正式" + (intent == "concession" ? "让步" : "道歉") + "：" + document.Title, intent);
        addWarPressure(targetId, authorId, reduction / 2,
            "对方作出正式" + (intent == "concession" ? "让步" : "道歉"), intent);
    }

    internal static void RecordTargetDecision(
        WorldDiplomacyStorage storage,
        WorldDiplomacyDocument document,
        string authorId,
        string selectedIssuerId,
        string intent,
        Func<int> currentDay,
        Action<string> log)
    {
        if (document == null || authorId == null) return;
        HashSet<string> presented = new HashSet<string>(
            document.PresentedThreatDocumentIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        if (presented.Count == 0) return;
        string normalizedIntent = WorldDiplomacyIntentVocabulary.NormalizeIntent(intent);
        string selectedSourceId = WorldDiplomacyRoundLifecycleRules.ResolveThreatDecisionSourceDocumentId(
            normalizedIntent, document.RespondingToThreatDocumentId);
        foreach (WorldDiplomacyThreat threat in (storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
                     .Where(x => WorldDiplomacyRoundLifecycleRules.IsThreatTargetDecisionCandidate(x, authorId, presented))
                     .ToList())
        {
            bool targetsIssuer = selectedIssuerId != null
                && string.Equals(threat.IssuerKingdomId, selectedIssuerId, StringComparison.OrdinalIgnoreCase);
            WorldDiplomacyThreatStateRuleResult decision = WorldDiplomacyThreatStateRules.EvaluateTargetDeclaration(
                threat.TargetDecision, threat.StageDocumentId,
                currentStageWasPresented: true, normalizedIntent, selectedSourceId, targetsIssuer);
            if (decision != WorldDiplomacyThreatStateRuleResult.MarkTargetNoncomplied) continue;
            threat.TargetDecision = "noncomplied";
            threat.TargetDecisionDocumentId = document.DocumentId ?? "";
            threat.TargetDecisionRoundId = document.RoundId ?? "";
            threat.TargetDecisionDay = currentDay();
            threat.ResolutionReason = "target_did_not_comply_in_first_declaration";
            threat.UpdatedDay = currentDay();
            WorldDiplomacyRoundLifecycleRules.CaptureThreatNonComplianceEvent(threat);
            log("diplomatic threat target noncompliance confirmed threat=" + threat.ThreatId
                + " issuer=" + threat.IssuerKingdomId + " target=" + threat.TargetKingdomId
                + " document=" + document.DocumentId + " intent=" + normalizedIntent);
        }
    }

    internal static void RecordTargetDecisionsForActions(
        WorldDiplomacyStorage storage,
        WorldDiplomacyDocument document,
        string authorId,
        Func<int> currentDay,
        Action<string> log)
    {
        if (document == null || authorId == null || document.Actions == null) return;
        HashSet<string> presented = new HashSet<string>(
            document.PresentedThreatDocumentIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        if (presented.Count == 0) return;
        foreach (WorldDiplomacyThreat threat in (storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
                     .Where(x => WorldDiplomacyRoundLifecycleRules.IsThreatTargetDecisionCandidate(x, authorId, presented))
                     .ToList())
        {
            WorldDiplomacyDocumentAction compliance = document.Actions
                .FirstOrDefault(x => WorldDiplomacyRoundLifecycleRules.IsThreatComplianceAction(x, threat));
            if (compliance != null) continue;
            WorldDiplomacyDocumentAction decisionAction = WorldDiplomacyRoundLifecycleRules.SelectThreatDecisionAction(
                document.Actions, threat.IssuerKingdomId);
            threat.TargetDecision = "noncomplied";
            threat.TargetDecisionDocumentId = document.DocumentId ?? "";
            threat.TargetDecisionActionId = decisionAction?.ActionId ?? "";
            threat.TargetDecisionRoundId = document.RoundId ?? "";
            threat.TargetDecisionDay = currentDay();
            threat.ResolutionReason = "target_did_not_comply_in_first_declaration";
            threat.UpdatedDay = currentDay();
            WorldDiplomacyRoundLifecycleRules.CaptureThreatNonComplianceEvent(threat);
            log("diplomatic threat target noncompliance confirmed threat=" + threat.ThreatId
                + " issuer=" + threat.IssuerKingdomId + " target=" + threat.TargetKingdomId
                + " document=" + document.DocumentId + " actions="
                + document.Actions.Count.ToString(CultureInfo.InvariantCulture));
        }
    }

    internal static bool DeferUnresolvedRequiredAction(
        WorldDiplomacyStorage storage,
        WorldDiplomacyDocument document,
        string authorId,
        string targetId,
        bool authorAndTargetAreSame,
        string intent,
        Func<int> currentDay,
        Action<string> log)
    {
        if (document == null || authorId == null) return false;
        WorldDiplomacyThreat threat = WorldDiplomacyRoundLifecycleRules.SelectOpenThreatIssuedBy(
            storage?.DiplomaticThreats, authorId);
        if (!WorldDiplomacyRoundLifecycleRules.IsThreatFollowThroughObligationPending(
            threat, document.PresentedThreatFollowThroughDocumentIds)) return false;
        WorldDiplomacyDocumentAction matchingAction = WorldDiplomacyRoundLifecycleRules.SelectThreatDecisionAction(
            document.Actions, threat.TargetKingdomId);
        if (document.Actions?.Count > 0 && matchingAction == null) return false;
        if (matchingAction == null && (targetId == null || authorAndTargetAreSame
            || !string.Equals(threat.TargetKingdomId, targetId, StringComparison.OrdinalIgnoreCase))) return false;
        string normalizedIntent = WorldDiplomacyIntentVocabulary.NormalizeIntent(matchingAction?.Intent ?? intent);
        bool changedState = matchingAction?.ChangedDiplomaticState ?? document.ChangedDiplomaticState;
        WorldDiplomacyThreatStateRuleResult result = WorldDiplomacyThreatStateRules.EvaluateIssuerFollowThrough(
            threat.TargetDecision, threat.Stage, threat.StageDocumentId,
            currentStageWasPresented: true, normalizedIntent,
            declarationTargetsThreatTarget: true, warActionMechanicallySucceeded: changedState);
        if (result != WorldDiplomacyThreatStateRuleResult.MarkFollowThroughSatisfied
            && result != WorldDiplomacyThreatStateRuleResult.DeferFollowThroughForTechnicalFailure) return false;
        threat.ResolutionReason = "required_action_mechanical_retry";
        threat.UpdatedDay = currentDay();
        log("diplomatic threat next-declaration obligation deferred after unresolved required action threat="
            + threat.ThreatId + " stage=" + threat.Stage + " document=" + document.DocumentId
            + " intent=" + normalizedIntent);
        return true;
    }
}
