using System;

namespace AnimusForge;

public enum WorldDiplomacyThreatStateRuleResult : byte
{
	None = 0,
	MarkTargetComplied = 1,
	MarkTargetNoncomplied = 2,
	RejectLateCompliance = 3,
	MarkFollowThroughSatisfied = 4,
	MarkFollowThroughBreached = 5,
	DeferFollowThroughForTechnicalFailure = 6,
	RebuildStaleStageSnapshot = 7
}

/// <summary>
/// Pure publication-boundary rules for the warning/ultimatum state machine.
/// Callers retain ownership of persistence and side effects. Intent, stage, and
/// decision tokens are expected to be normalized; comparisons remain case-insensitive.
/// </summary>
public static class WorldDiplomacyThreatStateRules
{
	/// <summary>
	/// Evaluates the target kingdom's first declaration after a threat was presented.
	/// Every declaration crossing the publication boundary, including a fallback
	/// declaration, must pass through this method exactly once for each incoming threat.
	/// </summary>
	public static WorldDiplomacyThreatStateRuleResult EvaluateTargetDeclaration(
		string targetDecision,
		string currentStageDocumentId,
		bool currentStageWasPresented,
		string declarationIntent,
		string respondingToThreatDocumentId,
		bool declarationTargetsIssuer)
	{
		bool isCompliance = EqualsToken(declarationIntent, "comply_ultimatum");
		if (EqualsToken(targetDecision, "noncomplied") || EqualsToken(targetDecision, "complied"))
		{
			return isCompliance
				? WorldDiplomacyThreatStateRuleResult.RejectLateCompliance
				: WorldDiplomacyThreatStateRuleResult.None;
		}
		if (!EqualsToken(targetDecision, "pending"))
		{
			return WorldDiplomacyThreatStateRuleResult.RebuildStaleStageSnapshot;
		}
		if (string.IsNullOrEmpty(currentStageDocumentId) || !currentStageWasPresented)
		{
			return WorldDiplomacyThreatStateRuleResult.RebuildStaleStageSnapshot;
		}
		if (isCompliance
			&& declarationTargetsIssuer
			&& EqualsToken(respondingToThreatDocumentId, currentStageDocumentId))
		{
			return WorldDiplomacyThreatStateRuleResult.MarkTargetComplied;
		}
		return WorldDiplomacyThreatStateRuleResult.MarkTargetNoncomplied;
	}

	/// <summary>
	/// Evaluates the issuer's first declaration after target noncompliance.
	/// A rejected warning requires an ultimatum to the same target. A rejected
	/// ultimatum requires a mechanically successful declaration of war.
	/// </summary>
	public static WorldDiplomacyThreatStateRuleResult EvaluateIssuerFollowThrough(
		string targetDecision,
		string stage,
		string currentStageDocumentId,
		bool currentStageWasPresented,
		string declarationIntent,
		bool declarationTargetsThreatTarget,
		bool warActionMechanicallySucceeded)
	{
		if (EqualsToken(targetDecision, "pending") || EqualsToken(targetDecision, "complied"))
		{
			return WorldDiplomacyThreatStateRuleResult.None;
		}
		if (!EqualsToken(targetDecision, "noncomplied")
			|| string.IsNullOrEmpty(currentStageDocumentId)
			|| !currentStageWasPresented)
		{
			return WorldDiplomacyThreatStateRuleResult.RebuildStaleStageSnapshot;
		}

		if (EqualsToken(stage, "warning"))
		{
			return declarationTargetsThreatTarget && EqualsToken(declarationIntent, "ultimatum")
				? WorldDiplomacyThreatStateRuleResult.MarkFollowThroughSatisfied
				: WorldDiplomacyThreatStateRuleResult.MarkFollowThroughBreached;
		}
		if (!EqualsToken(stage, "ultimatum"))
		{
			return WorldDiplomacyThreatStateRuleResult.RebuildStaleStageSnapshot;
		}
		if (!declarationTargetsThreatTarget || !EqualsToken(declarationIntent, "declare_war"))
		{
			return WorldDiplomacyThreatStateRuleResult.MarkFollowThroughBreached;
		}
		return warActionMechanicallySucceeded
			? WorldDiplomacyThreatStateRuleResult.MarkFollowThroughSatisfied
			: WorldDiplomacyThreatStateRuleResult.DeferFollowThroughForTechnicalFailure;
	}

	private static bool EqualsToken(string left, string right)
	{
		return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
	}
}
