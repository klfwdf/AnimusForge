using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge;
using AnimusForge.Refactor.Domain;

// Original daily method from 260ba51e; only world resolution/court effects are stubbed.
// SHA256 of the original method (LF UTF-8): 256f812c0eacb08dad527d88ae73917dc687054b68d9305d491c9486a9a2d453
internal static partial class PropagationApplicationReplay
{
    private sealed partial class Harness
    {
	private void ProcessPropagationArrivals()
	{
		int day = CurrentDay();
		List<WorldDiplomacyPropagationArrival> due = _storage.PropagationArrivals
			.TakeWhile(x => x != null && x.DueDay <= day)
			.Take(MaxPropagationArrivalsPerDay)
			.ToList();
		if (due.Count > 0) _storage.PropagationArrivals.RemoveRange(0, due.Count);
		foreach (WorldDiplomacyPropagationArrival arrival in due)
		{
			WorldDiplomacyDocument document = ResolveDocument(arrival.DocumentId);
			if (document == null)
			{
				continue;
			}
			if (WorldDiplomacyStructureRules.IsCourtArrival(arrival))
			{
				Kingdom receiver = ResolveKingdom(arrival.KingdomId) ?? ResolveSettlementById(arrival.SettlementId)?.OwnerClan?.Kingdom;
				if (receiver != null)
				{
					WorldDiplomacyDocumentFactRules.RecordNobleKnowledge(_storage.NobleKnowledge, receiver.StringId, document.DocumentId, day);
					bool newlyKnown = WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(_storage.KingdomKnowledge, receiver.StringId, document.DocumentId, day);
					if (newlyKnown || (IsPlayerAffiliatedKingdom(receiver) && !document.HasReachedPlayerCourt))
					{
						ProcessCourtArrival(receiver, document);
					}
				}
				continue;
			}
			Settlement settlement = ResolveSettlementById(arrival.SettlementId);
			if (settlement != null) WorldDiplomacyDocumentFactRules.RecordSettlementKnowledge(_storage.SettlementKnowledge, settlement.StringId, document.DocumentId, day);
		}
	}

	private void ProcessCurrentPropagationArrivals()
	{
		WorldDiplomacyPropagationApplication.ProcessDue(_storage, CurrentDay(), MaxPropagationArrivalsPerDay,
			ResolveDocument,
			(arrival, document, day) =>
			{
				Kingdom receiver = ResolveKingdom(arrival.KingdomId) ?? ResolveSettlementById(arrival.SettlementId)?.OwnerClan?.Kingdom;
				if (receiver == null) return;
				WorldDiplomacyPropagationApplication.ReceiveCourt(_storage, document, receiver.StringId, day,
					() => IsPlayerAffiliatedKingdom(receiver), () => ProcessCourtArrival(receiver, document));
			},
			id => ResolveSettlementById(id)?.StringId);
	}
    }
}
