using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    internal sealed class PublicationPort : IWorldDiplomacyPublicationPort
    {
        private readonly WorldDiplomacyBehavior _owner;
        internal PublicationPort(WorldDiplomacyBehavior owner) => _owner = owner;
        public WorldDiplomacyStorage Storage => _owner._storage;
        public string ResolveKingdomId(string id) => ResolveKingdom(id)?.StringId;
        public bool CanAiAuthor(string id, out string reason) => CanAiAuthorDiplomaticDocument(ResolveKingdom(id), out reason);
        public bool HasAuthority(string id) => HasIndependentWorldDiplomacyAuthority(ResolveKingdom(id));
        public bool IsPlayerAffiliated(string id) => IsPlayerAffiliatedKingdom(ResolveKingdom(id));
        public bool IsPlayerKingdom(string id) => WorldDiplomacyBehavior.IsPlayerKingdom(ResolveKingdom(id));
        public bool RepresentsAddressedVassal(string id, WorldDiplomacyDocument document) => _owner.IsDiplomaticRepresentativeForAddressedVassal(ResolveKingdom(id), document);
        public WorldDiplomacyRound ResolveRound(string id) => _owner.ResolveRound(id);
        public WorldDiplomacyRound EnsureRound(string author, string target, bool player) => _owner.EnsureActiveRound(ResolveKingdom(author), ResolveKingdom(target), player);
        public string ResolveOriginSettlementId(string author) => _owner.ResolveCourtSettlement(ResolveKingdom(author))?.StringId;
        public int CurrentDay => WorldDiplomacyBehavior.CurrentDay();
        public int ParticipantLimit => GetRoundParticipantLimit();
        public int CivilianSpreadDays => GetCivilianSpreadDays();
        public int CourtDeliveryDays => GetCourtMaxDeliveryDays();
        public void RecordWeeklyMaterial(WorldDiplomacyDocument document) => _owner.RecordDiplomacyWeeklyMaterial(document);
        public void Reject(WorldDiplomacyDocument document, string reason) => _owner.SuppressInvalidDocumentBeforePropagation(document, reason);
        public void ScheduleMandatoryResponse(WorldDiplomacyRound round, WorldDiplomacyRoundParticipant participant, string receiver, WorldDiplomacyDocument document) => _owner.TryScheduleMandatoryCourtResponse(round, participant, ResolveKingdom(receiver), document);
        public void Log(string message) => WorldDiplomacyBehavior.Log(message);
        public WorldDiplomacyPublicationSnapshot CaptureDestinations(string authorId, string originId)
        {
            Kingdom author = ResolveKingdom(authorId);
            Settlement origin = ResolveSettlementById(originId);
		List<Settlement> settlements = Settlement.All
			.Where(x => x != null && !x.IsHideout && !string.IsNullOrWhiteSpace(x.StringId))
			.OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase)
			.ToList();
		float maxCivilianDistance = origin == null || settlements.Count == 0
			? 0f
			: settlements.Max(x => origin.GatePosition.Distance(x.GatePosition));
		List<WorldDiplomacyPropagationApplication.SettlementTarget> settlementTargets =
			new List<WorldDiplomacyPropagationApplication.SettlementTarget>(settlements.Count);
		foreach (Settlement settlement in settlements)
		{
			bool isOrigin = origin != null && settlement == origin;
			settlementTargets.Add(new WorldDiplomacyPropagationApplication.SettlementTarget
			{
				Id = settlement.StringId,
				IsOrigin = isOrigin,
				Distance = isOrigin || origin == null
					? maxCivilianDistance
					: origin.GatePosition.Distance(settlement.GatePosition)
			});
		}
		List<Tuple<Kingdom, Settlement>> courtDestinations = Kingdom.All
			.Where(x => x != null && !x.IsEliminated && x != author && !string.IsNullOrWhiteSpace(x.StringId))
			.OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase)
            .Select(x => Tuple.Create(x, _owner.ResolveCourtSettlement(x)))
			.ToList();
		float maxCourtDistance = origin == null
			? 0f
			: courtDestinations.Where(x => x.Item2 != null).Select(x => origin.GatePosition.Distance(x.Item2.GatePosition)).DefaultIfEmpty(0f).Max();
		List<WorldDiplomacyPropagationApplication.CourtTarget> courtTargets =
			new List<WorldDiplomacyPropagationApplication.CourtTarget>(courtDestinations.Count);
		foreach (Tuple<Kingdom, Settlement> destination in courtDestinations)
		{
			courtTargets.Add(new WorldDiplomacyPropagationApplication.CourtTarget
			{
				KingdomId = destination.Item1.StringId,
				SettlementId = destination.Item2?.StringId ?? "",
				IsPlayerAffiliated = IsPlayerAffiliatedKingdom(destination.Item1),
				Distance = origin == null || destination.Item2 == null
					? maxCourtDistance
					: origin.GatePosition.Distance(destination.Item2.GatePosition)
			});
		}

            return new WorldDiplomacyPublicationSnapshot(settlementTargets, courtTargets, maxCivilianDistance, maxCourtDistance);
        }
    }
}
