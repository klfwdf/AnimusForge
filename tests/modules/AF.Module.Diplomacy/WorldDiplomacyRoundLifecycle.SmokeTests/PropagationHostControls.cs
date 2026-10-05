using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge;
using AnimusForge.Refactor.Domain;

// Original daily method from 260ba51e; only world resolution/court effects are stubbed.
// SHA256 of the original method (LF UTF-8): 256f812c0eacb08dad527d88ae73917dc687054b68d9305d491c9486a9a2d453
// The current adapter lives in Application/WorldDiplomacyOrchestration.cs; the fixture
// below mirrors it verbatim so the boundary check compares production text directly.
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

    public void ProcessCurrentPropagationArrivals()
    {
        WorldDiplomacyPropagationApplication.ProcessDue(Storage, _host.CurrentDay(), _host.MaxPropagationArrivalsPerDay(),
            ResolveDocument,
            (arrival, document, day) =>
            {
                string receiverId = _host.ResolvePropagationReceiverId(arrival.KingdomId, arrival.SettlementId);
                if (receiverId == null) return;
                WorldDiplomacyPropagationApplication.ReceiveCourt(Storage, document, receiverId, day,
                    () => _host.IsPlayerAffiliatedParty(receiverId), () => ProcessCourtArrival(receiverId, document));
            },
            _host.ResolveSettlementId);
    }

        private WorldDiplomacyStorage Storage => _storage;

        private void ProcessCourtArrival(string receiverId, WorldDiplomacyDocument document)
        {
            Trace.Add("court:" + receiverId + ":" + document.DocumentId + ":pending=" + _storage.PropagationArrivals.Count);
            Receipts++; ReceiptTitles.Add(document.Title);
            if (receiverId == "player") document.HasReachedPlayerCourt = true;
            OnReceipt?.Invoke(_storage);
        }

        private sealed class FixtureHost
        {
            private readonly Harness _h;
            internal FixtureHost(Harness harness) { _h = harness; }
            internal int CurrentDay() => _h.CurrentDay();
            internal int MaxPropagationArrivalsPerDay() => Harness.MaxPropagationArrivalsPerDay;
            internal string ResolvePropagationReceiverId(string kingdomId, string settlementId)
                => _h.ResolveKingdom(kingdomId)?.StringId ?? _h.ResolveSettlementById(settlementId)?.OwnerClan?.Kingdom?.StringId;
            internal bool IsPlayerAffiliatedParty(string kingdomId)
                => _h.IsPlayerAffiliatedKingdom(new Kingdom { StringId = kingdomId });
            internal string ResolveSettlementId(string settlementId) => _h.ResolveSettlementById(settlementId)?.StringId;
        }
    }
}
