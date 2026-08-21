#nullable disable

using System;
using System.Collections.Generic;

namespace AnimusForge;

public enum WorldDiplomacyConfirmedResultKind : byte
{
	None = 0,
	OfferAccepted = 1,
	OfferRejected = 2,
	DiplomaticStateChanged = 3,
	ThreatComplied = 4,
	ThreatEnforced = 5,
	ThreatBreached = 6,
	ThreatResolvedByWar = 7
}

/// <summary>
/// Pure publication-boundary input. The caller must only pass an offer status when
/// it belongs to the exact offer answered by this document, and a threat status when
/// the document is the threat's recorded compliance/resolution document.
/// </summary>
public readonly struct WorldDiplomacyResultObservation
{
	public WorldDiplomacyResultObservation(
		string intent,
		bool changedDiplomaticState,
		bool hasMatchedOffer,
		string matchedOfferStatus,
		string linkedThreatStatus,
		bool isExternallyResolvedFact = false)
	{
		Intent = intent ?? "";
		ChangedDiplomaticState = changedDiplomaticState;
		HasMatchedOffer = hasMatchedOffer;
		MatchedOfferStatus = matchedOfferStatus ?? "";
		LinkedThreatStatus = linkedThreatStatus ?? "";
		IsExternallyResolvedFact = isExternallyResolvedFact;
	}

	public string Intent { get; }
	public bool ChangedDiplomaticState { get; }
	public bool HasMatchedOffer { get; }
	public string MatchedOfferStatus { get; }
	public string LinkedThreatStatus { get; }
	public bool IsExternallyResolvedFact { get; }
}

/// <summary>
/// Persistable, one-per-kingdom result-settlement slot. Multiple obligations for the
/// same kingdom are merged into the two id lists. Runtime code owns serialization.
/// </summary>
public sealed class WorldDiplomacyResultSettlementSlot
{
	public string SlotId { get; set; } = "";
	public string KingdomId { get; set; } = "";
	public string Kind { get; set; } = "route";
	public List<string> SourceDocumentIds { get; set; } = new List<string>();
	public List<string> RelatedKingdomIds { get; set; } = new List<string>();
	public string Status { get; set; } = "pending";
}

/// <summary>
/// Pure result-settlement queue rules. The queue is tiny (bounded by round
/// participants), so mutations use allocation-free linear scans and preserve route
/// order. Only a successful war response is promoted to the front.
/// </summary>
public static class WorldDiplomacyResultSettlementRules
{
	public const string RouteSlotKind = "route";
	public const string OfferResponseSlotKind = "offer_response";
	public const string ThreatResponseSlotKind = "threat_response";
	public const string ThreatFollowThroughSlotKind = "threat_followthrough";
	public const string WarResponseSlotKind = "war_response";
	public const string PendingStatus = "pending";
	public const string ConsumedStatus = "consumed";
	public const string SkippedStatus = "skipped";

	public static WorldDiplomacyConfirmedResultKind EvaluateConfirmedResult(
		WorldDiplomacyResultObservation observation)
	{
		string intent = NormalizeToken(observation.Intent);
		string threatStatus = NormalizeToken(observation.LinkedThreatStatus);
		if (threatStatus == "complied") return WorldDiplomacyConfirmedResultKind.ThreatComplied;
		if (threatStatus == "enforced") return WorldDiplomacyConfirmedResultKind.ThreatEnforced;
		if (threatStatus == "breached")
		{
			return intent == "declare_war" && observation.ChangedDiplomaticState
				? WorldDiplomacyConfirmedResultKind.ThreatResolvedByWar
				: WorldDiplomacyConfirmedResultKind.ThreatBreached;
		}

		if (observation.HasMatchedOffer)
		{
			string offerStatus = NormalizeToken(observation.MatchedOfferStatus);
			if (IsAcceptIntent(intent)
				&& (offerStatus == "accepted" || offerStatus == "partially_executed"))
			{
				return WorldDiplomacyConfirmedResultKind.OfferAccepted;
			}
			if (IsRejectIntent(intent) && offerStatus == "rejected")
			{
				return WorldDiplomacyConfirmedResultKind.OfferRejected;
			}
		}

		if (!observation.ChangedDiplomaticState) return WorldDiplomacyConfirmedResultKind.None;
		if (IsImmediateStateChangeIntent(intent))
		{
			return WorldDiplomacyConfirmedResultKind.DiplomaticStateChanged;
		}
		if (observation.IsExternallyResolvedFact && IsExternalResolvedIntent(intent))
		{
			return WorldDiplomacyConfirmedResultKind.DiplomaticStateChanged;
		}
		return WorldDiplomacyConfirmedResultKind.None;
	}

	public static bool IsConfirmedResult(WorldDiplomacyConfirmedResultKind kind)
	{
		// A refusal closes only that proposal. It is progress inside the negotiation,
		// not a terminal result for the entire diplomatic round.
		return kind != WorldDiplomacyConfirmedResultKind.None
			&& kind != WorldDiplomacyConfirmedResultKind.OfferRejected;
	}

	public static bool IsResolvedOutcome(WorldDiplomacyConfirmedResultKind kind)
	{
		return kind != WorldDiplomacyConfirmedResultKind.None
			&& kind != WorldDiplomacyConfirmedResultKind.OfferRejected
			&& kind != WorldDiplomacyConfirmedResultKind.ThreatBreached;
	}

	/// <summary>
	/// Adds route slots for selected kingdoms that have not published a document in the
	/// round. Returns the number of slots newly opened by this call.
	/// </summary>
	public static int InitializeUnspokenSelectedSlots(
		IReadOnlyList<string> selectedKingdomIds,
		ISet<string> spokenKingdomIds,
		List<WorldDiplomacyResultSettlementSlot> slots)
	{
		if (selectedKingdomIds == null || slots == null) return 0;
		int opened = 0;
		for (int index = 0; index < selectedKingdomIds.Count; index++)
		{
			string kingdomId = NormalizeId(selectedKingdomIds[index]);
			if (kingdomId.Length == 0 || ContainsId(spokenKingdomIds, kingdomId)) continue;
			bool wasPending = HasPendingSlot(slots, kingdomId);
			WorldDiplomacyResultSettlementSlot slot = AddOrPromotePendingTarget(
				slots,
				kingdomId,
				RouteSlotKind,
				sourceDocumentId: "",
				relatedKingdomId: "",
				prioritize: false);
			if (slot != null && !wasPending) opened++;
		}
		return opened;
	}

	/// <summary>
	/// Registers proposals and war-only threat actions as obligations for their target.
	/// A mechanically successful declaration of war creates a priority war response.
	/// Closed actions (accept/reject/break/cancel) deliberately create no new slot.
	/// </summary>
	public static bool TryAddPendingActionTarget(
		List<WorldDiplomacyResultSettlementSlot> slots,
		string intent,
		string targetKingdomId,
		string sourceDocumentId,
		string authorKingdomId,
		bool actionMechanicallySucceeded,
		out WorldDiplomacyResultSettlementSlot slot)
	{
		slot = null;
		if (slots == null) return false;
		string normalizedIntent = NormalizeToken(intent);
		if (normalizedIntent == "declare_war")
		{
			if (!actionMechanicallySucceeded) return false;
			slot = ApplySuccessfulWarTarget(
				slots,
				targetKingdomId,
				sourceDocumentId,
				authorKingdomId);
			return slot != null;
		}

		string kind;
		if (normalizedIntent == "propose_peace"
			|| normalizedIntent == "propose_alliance"
			|| normalizedIntent == "propose_trade")
		{
			kind = OfferResponseSlotKind;
		}
		else if (normalizedIntent == "warning" || normalizedIntent == "ultimatum")
		{
			kind = ThreatResponseSlotKind;
		}
		else
		{
			return false;
		}

		slot = AddOrPromotePendingTarget(
			slots,
			targetKingdomId,
			kind,
			sourceDocumentId,
			authorKingdomId,
			prioritize: false);
		return slot != null;
	}

	public static WorldDiplomacyResultSettlementSlot ApplySuccessfulWarTarget(
		List<WorldDiplomacyResultSettlementSlot> slots,
		string targetKingdomId,
		string warDocumentId,
		string aggressorKingdomId)
	{
		return AddOrPromotePendingTarget(
			slots,
			targetKingdomId,
			WarResponseSlotKind,
			warDocumentId,
			aggressorKingdomId,
			prioritize: true);
	}

	/// <summary>
	/// Adds, merges, or reopens a target slot. There is never more than one slot per
	/// kingdom after this method returns, including when repairing malformed old data.
	/// </summary>
	public static WorldDiplomacyResultSettlementSlot AddOrPromotePendingTarget(
		List<WorldDiplomacyResultSettlementSlot> slots,
		string targetKingdomId,
		string kind,
		string sourceDocumentId,
		string relatedKingdomId,
		bool prioritize)
	{
		if (slots == null) return null;
		string kingdomId = NormalizeId(targetKingdomId);
		if (kingdomId.Length == 0) return null;
		string normalizedKind = NormalizeKind(kind);
		int slotIndex = FindAndMergeSlotIndex(slots, kingdomId);
		WorldDiplomacyResultSettlementSlot slot;
		if (slotIndex < 0)
		{
			slot = new WorldDiplomacyResultSettlementSlot
			{
				SlotId = BuildStableSlotId(kingdomId),
				KingdomId = kingdomId,
				Kind = normalizedKind,
				Status = PendingStatus
			};
			slots.Add(slot);
			slotIndex = slots.Count - 1;
		}
		else
		{
			slot = slots[slotIndex];
			EnsureSlotCollections(slot);
			if (string.IsNullOrWhiteSpace(slot.SlotId)) slot.SlotId = BuildStableSlotId(kingdomId);
			if (!IsPending(slot))
			{
				slot.Kind = normalizedKind;
				slot.Status = PendingStatus;
			}
			else if (GetKindPriority(normalizedKind) > GetKindPriority(slot.Kind))
			{
				slot.Kind = normalizedKind;
			}
		}

		AddUniqueId(slot.SourceDocumentIds, sourceDocumentId);
		if (!EqualsId(kingdomId, relatedKingdomId))
		{
			AddUniqueId(slot.RelatedKingdomIds, relatedKingdomId);
		}
		if (prioritize && slotIndex > 0)
		{
			slots.RemoveAt(slotIndex);
			slots.Insert(0, slot);
		}
		return slot;
	}

	public static bool ConsumeSpeakerSlot(
		List<WorldDiplomacyResultSettlementSlot> slots,
		string kingdomId,
		out WorldDiplomacyResultSettlementSlot consumedSlot)
	{
		consumedSlot = null;
		if (slots == null) return false;
		int index = FindAndMergeSlotIndex(slots, NormalizeId(kingdomId));
		if (index < 0 || !IsPending(slots[index])) return false;
		consumedSlot = slots[index];
		consumedSlot.Status = ConsumedStatus;
		return true;
	}

	public static WorldDiplomacyResultSettlementSlot GetNextPendingSlot(
		IReadOnlyList<WorldDiplomacyResultSettlementSlot> slots)
	{
		int count = slots?.Count ?? 0;
		for (int index = 0; index < count; index++)
		{
			WorldDiplomacyResultSettlementSlot slot = slots[index];
			if (IsPending(slot) && NormalizeId(slot.KingdomId).Length > 0) return slot;
		}
		return null;
	}

	public static bool CanClose(
		IReadOnlyList<WorldDiplomacyResultSettlementSlot> slots,
		bool hasUnresolvedActions)
	{
		return !hasUnresolvedActions && GetNextPendingSlot(slots) == null;
	}

	private static int FindAndMergeSlotIndex(
		List<WorldDiplomacyResultSettlementSlot> slots,
		string kingdomId)
	{
		if (kingdomId.Length == 0) return -1;
		int primaryIndex = -1;
		for (int index = 0; index < slots.Count; index++)
		{
			WorldDiplomacyResultSettlementSlot candidate = slots[index];
			if (candidate == null || !EqualsId(candidate.KingdomId, kingdomId)) continue;
			if (primaryIndex < 0)
			{
				primaryIndex = index;
				if (string.IsNullOrWhiteSpace(candidate.KingdomId)) candidate.KingdomId = kingdomId;
				EnsureSlotCollections(candidate);
				continue;
			}

			WorldDiplomacyResultSettlementSlot primary = slots[primaryIndex];
			MergeSlot(primary, candidate);
			slots.RemoveAt(index);
			index--;
		}
		return primaryIndex;
	}

	private static void MergeSlot(
		WorldDiplomacyResultSettlementSlot target,
		WorldDiplomacyResultSettlementSlot source)
	{
		if (target == null || source == null) return;
		EnsureSlotCollections(target);
		EnsureSlotCollections(source);
		if (string.IsNullOrWhiteSpace(target.SlotId)) target.SlotId = source.SlotId ?? "";
		if (GetKindPriority(source.Kind) > GetKindPriority(target.Kind)) target.Kind = NormalizeKind(source.Kind);
		if (IsPending(source)) target.Status = PendingStatus;
		for (int index = 0; index < source.SourceDocumentIds.Count; index++)
		{
			AddUniqueId(target.SourceDocumentIds, source.SourceDocumentIds[index]);
		}
		for (int index = 0; index < source.RelatedKingdomIds.Count; index++)
		{
			AddUniqueId(target.RelatedKingdomIds, source.RelatedKingdomIds[index]);
		}
	}

	private static void EnsureSlotCollections(WorldDiplomacyResultSettlementSlot slot)
	{
		if (slot == null) return;
		slot.SourceDocumentIds ??= new List<string>();
		slot.RelatedKingdomIds ??= new List<string>();
	}

	private static bool HasPendingSlot(
		IReadOnlyList<WorldDiplomacyResultSettlementSlot> slots,
		string kingdomId)
	{
		int count = slots?.Count ?? 0;
		for (int index = 0; index < count; index++)
		{
			WorldDiplomacyResultSettlementSlot slot = slots[index];
			if (IsPending(slot) && EqualsId(slot.KingdomId, kingdomId)) return true;
		}
		return false;
	}

	private static bool IsPending(WorldDiplomacyResultSettlementSlot slot)
	{
		return slot != null && string.Equals(slot.Status, PendingStatus, StringComparison.OrdinalIgnoreCase);
	}

	private static bool ContainsId(ISet<string> ids, string expected)
	{
		if (ids == null || expected.Length == 0) return false;
		if (ids.Contains(expected)) return true;
		foreach (string value in ids)
		{
			if (EqualsId(value, expected)) return true;
		}
		return false;
	}

	private static void AddUniqueId(List<string> ids, string value)
	{
		string normalized = NormalizeId(value);
		if (normalized.Length == 0) return;
		for (int index = 0; index < ids.Count; index++)
		{
			if (EqualsId(ids[index], normalized)) return;
		}
		ids.Add(normalized);
	}

	private static string BuildStableSlotId(string kingdomId)
	{
		return "result_settlement:" + kingdomId;
	}

	private static string NormalizeKind(string kind)
	{
		string normalized = NormalizeToken(kind);
		return normalized.Length == 0 ? RouteSlotKind : normalized;
	}

	private static int GetKindPriority(string kind)
	{
		return NormalizeKind(kind) switch
		{
			WarResponseSlotKind => 500,
			ThreatFollowThroughSlotKind => 400,
			ThreatResponseSlotKind => 300,
			OfferResponseSlotKind => 200,
			RouteSlotKind => 100,
			_ => 150
		};
	}

	private static bool IsAcceptIntent(string intent)
	{
		return intent == "accept_peace" || intent == "accept_alliance" || intent == "accept_trade";
	}

	private static bool IsRejectIntent(string intent)
	{
		return intent == "reject_peace" || intent == "reject_alliance" || intent == "reject_trade";
	}

	private static bool IsImmediateStateChangeIntent(string intent)
	{
		return intent == "declare_war" || intent == "break_alliance" || intent == "cancel_trade";
	}

	private static bool IsExternalResolvedIntent(string intent)
	{
		return IsImmediateStateChangeIntent(intent) || IsAcceptIntent(intent);
	}

	private static string NormalizeToken(string value)
	{
		return string.IsNullOrWhiteSpace(value) ? "" : value.Trim().ToLowerInvariant();
	}

	private static string NormalizeId(string value)
	{
		return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
	}

	private static bool EqualsId(string left, string right)
	{
		return string.Equals(NormalizeId(left), NormalizeId(right), StringComparison.OrdinalIgnoreCase);
	}
}
