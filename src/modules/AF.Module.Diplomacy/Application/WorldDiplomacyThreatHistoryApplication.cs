using AnimusForge.Refactor.Contracts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal static class WorldDiplomacyThreatHistoryApplication
{
public static void FinalizeDiplomaticThreatHistoryAfterDocument(WorldDiplomacyDocument document,
		IReadOnlyList<WorldDiplomacyThreat> threats,
		Action<WorldDiplomacyThreat> appendBreachResult,
		Action<WorldDiplomacyThreat> appendDomesticPenaltyResult,
		Action<WorldDiplomacyThreat> appendIssuerRewardResult)
	{
		if (document == null || string.IsNullOrWhiteSpace(document.DocumentId)) return;
		foreach (WorldDiplomacyThreat threat in (threats ?? new List<WorldDiplomacyThreat>())
			.Where(x => WorldDiplomacyRoundLifecycleRules.IsThreatLinkedToDocument(x, document.DocumentId)))
		{
			WorldDiplomacyThreatHistoryFinalization finalization =
				WorldDiplomacyRoundLifecycleRules.EvaluateThreatHistoryFinalization(
					threat, document.HistoryDeclarationRecorded, document.HistoryResultRecorded);
			if (finalization == WorldDiplomacyThreatHistoryFinalization.AppendBreachResult)
			{
				appendBreachResult?.Invoke(threat);
			}
			else if (finalization == WorldDiplomacyThreatHistoryFinalization.MarkResultRecorded)
			{
				threat.HistoryResultRecorded = true;
			}
			if (threat.DomesticPenaltyCompleted)
			{
				appendDomesticPenaltyResult?.Invoke(threat);
			}
			if (threat.IssuerRewardCompleted)
			{
				appendIssuerRewardResult?.Invoke(threat);
			}
		}
	}

public static void FinalizeDiplomaticThreatNonComplianceHistoryAfterDocument(WorldDiplomacyDocument document,
		IReadOnlyList<WorldDiplomacyThreat> threats,
		Action<WorldDiplomacyThreat, WorldDiplomacyThreatNonComplianceEvent> appendNonComplianceEvent)
	{
		if (document?.HistoryDeclarationRecorded != true || string.IsNullOrWhiteSpace(document.DocumentId)) return;
		foreach (WorldDiplomacyThreat threat in (threats ?? new List<WorldDiplomacyThreat>())
			.Where(x => WorldDiplomacyRoundLifecycleRules.IsThreatNonComplianceLinkedToDocument(x, document.DocumentId)))
		{
			TryAppendDiplomaticThreatNonComplianceHistoryResult(threat, appendNonComplianceEvent);
		}
	}

public static void TryAppendDiplomaticThreatNonComplianceHistoryResult(
		WorldDiplomacyThreat threat,
		Action<WorldDiplomacyThreat, WorldDiplomacyThreatNonComplianceEvent> appendNonComplianceEvent)
	{
		if (threat == null) return;
		WorldDiplomacyRoundLifecycleRules.CaptureThreatNonComplianceEvent(threat);
		foreach (WorldDiplomacyThreatNonComplianceEvent decision in
			WorldDiplomacyRoundLifecycleRules.SelectUnrecordedNonComplianceEvents(threat))
		{
			appendNonComplianceEvent?.Invoke(threat, decision);
		}
		WorldDiplomacyThreatNonComplianceEvent current =
			WorldDiplomacyRoundLifecycleRules.SelectCurrentNonComplianceEvent(threat);
		threat.NonComplianceHistoryRecorded = current?.HistoryRecorded == true;
	}
}
