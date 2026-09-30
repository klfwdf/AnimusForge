using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.Refactor.Domain;
using static AnimusForge.Refactor.Domain.WorldDiplomacyRoundLifecycleRules;
namespace AnimusForge;

// Application owns dependency invocation and canonical workflow ordering.
internal static class WorldDiplomacyResultSlotApplication
{
public static void AddOrMergeResultSettlementSlot(
		WorldDiplomacyRound round,
		string kingdomId,
		string kind,
		string sourceDocumentId,
		string relatedKingdomId,
		bool prioritize,
		Func<WorldDiplomacyRound, string, bool> includeResultSettlementTarget,
		Func<string, string> createId)
	{
		if (round == null || string.IsNullOrWhiteSpace(kingdomId)
			|| !includeResultSettlementTarget(round, kingdomId)) return;
		round.ResultSettlementSlots ??= new List<WorldDiplomacyResultSettlementSlot>();
		WorldDiplomacyResultSettlementSlot slot = round.ResultSettlementSlots
			.FirstOrDefault(x => x != null && string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase));
		if (slot == null)
		{
			slot = new WorldDiplomacyResultSettlementSlot
			{
				SlotId = createId("diplomacy_result_slot"),
				KingdomId = kingdomId,
				Kind = WorldDiplomacyRoundLifecycleRules.DefaultSettlementSlotKind(kind),
				Status = "pending"
			};
			round.ResultSettlementSlots.Add(slot);
		}
		else if (!string.IsNullOrWhiteSpace(kind) && !(slot != null && WorldDiplomacyRoundLifecycleRules.SettlementSlotKindContains(slot.Kind, kind)))
		{
			slot.Kind = WorldDiplomacyRoundLifecycleRules.MergeSettlementSlotKind(slot.Kind, kind);
		}
		slot.SourceDocumentIds ??= new List<string>();
		slot.RelatedKingdomIds ??= new List<string>();
		if (!string.IsNullOrWhiteSpace(sourceDocumentId)
			&& !slot.SourceDocumentIds.Contains(sourceDocumentId, StringComparer.OrdinalIgnoreCase))
		{
			slot.SourceDocumentIds.Add(sourceDocumentId);
		}
		if (!string.IsNullOrWhiteSpace(relatedKingdomId)
			&& !WorldDiplomacyRoundLifecycleRules.IsSettlementSlotRelatedTo(slot, relatedKingdomId))
		{
			slot.RelatedKingdomIds.Add(relatedKingdomId);
		}
		if (prioritize)
		{
			round.ResultSettlementSlots.Remove(slot);
			round.ResultSettlementSlots.Insert(0, slot);
		}
	}

public static void AddWarResponseResultSettlementSlot(WorldDiplomacyRound round, WorldDiplomacyDocument document,
		Func<WorldDiplomacyRound, string, bool> includeResultSettlementTarget, Func<string, string> createId)
	{
		if (round == null || document == null) return;
		round.ResultSettlementWarDocumentIds ??= new List<string>();
		if (document.Actions?.Count > 0)
		{
			foreach (WorldDiplomacyDocumentAction action in document.Actions.Where(x => x != null
				&& WorldDiplomacyRoundLifecycleRules.IsWarResponseSlotAction(
					x.ChangedDiplomaticState, WorldDiplomacyIntentVocabulary.NormalizeIntent(x.Intent), x.TargetKingdomId)))
			{
				string actionKey = WorldDiplomacyRoundLifecycleRules.ComposeWarResponseActionKey(
					document.DocumentId, action.ActionId);
				if (round.ResultSettlementWarDocumentIds.Contains(actionKey, StringComparer.OrdinalIgnoreCase)) continue;
				round.ResultSettlementWarDocumentIds.Add(actionKey);
				AddOrMergeResultSettlementSlot(round, action.TargetKingdomId, "war_response",
					document.DocumentId, document.AuthorKingdomId, prioritize: true,
					includeResultSettlementTarget, createId);
			}
			return;
		}
		if (!WorldDiplomacyRoundLifecycleRules.IsWarResponseSlotAction(
			document.ChangedDiplomaticState, WorldDiplomacyIntentVocabulary.NormalizeIntent(document.Intent), document.TargetKingdomId)) return;
		if (round.ResultSettlementWarDocumentIds.Contains(document.DocumentId, StringComparer.OrdinalIgnoreCase)) return;
		round.ResultSettlementWarDocumentIds.Add(document.DocumentId);
		AddOrMergeResultSettlementSlot(round, document.TargetKingdomId, "war_response",
			document.DocumentId, document.AuthorKingdomId, prioritize: true,
				includeResultSettlementTarget, createId);
	}

public static void InitializeResultSettlementRouteSlots(WorldDiplomacyRound round, List<WorldDiplomacyDocument> documents, Func<WorldDiplomacyRound, string, bool> includeResultSettlementTarget, Func<string, string> createId)
	{
		if (round == null || round.ResultSettlementRouteInitialized || !round.RelayPlanned) return;
		HashSet<string> spoken = CollectSpokenAuthorIds(
			documents, round.RoundId);
		foreach (string kingdomId in round.RelayRouteKingdomIds ?? new List<string>())
		{
			if (!spoken.Contains(kingdomId))
			{
				AddOrMergeResultSettlementSlot(round, kingdomId, "route", round.ResultSettlementTriggerDocumentId, "", prioritize: false, includeResultSettlementTarget, createId);
			}
		}
		round.ResultSettlementRouteInitialized = true;
	}
}
