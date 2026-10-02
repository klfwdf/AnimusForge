using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
namespace AnimusForge;
public sealed partial class WorldDiplomacyBehavior
{
    private sealed class PeaceAdmissionPort : IWorldDiplomacyPeaceAdmissionPort
    {
        private readonly WorldDiplomacyBehavior _owner;
        private readonly Dictionary<string, Kingdom> _kingdoms = new Dictionary<string, Kingdom>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Settlement> _settlements = new Dictionary<string, Settlement>(StringComparer.OrdinalIgnoreCase);
        internal PeaceAdmissionPort(WorldDiplomacyBehavior owner) { _owner = owner; }
        private Kingdom Party(string id)
        {
            if (id == null) return null;
            if (!_kingdoms.TryGetValue(id, out var kingdom)) _kingdoms[id] = kingdom = ResolveKingdom(id);
            return kingdom;
        }
        private Settlement Place(string id)
        {
            if (id == null) return null;
            if (!_settlements.TryGetValue(id, out var settlement)) _settlements[id] = settlement = ResolveSettlementById(id);
            return settlement;
        }
        public string KingdomId(string id) => Party(id)?.StringId;
        public bool AtWar(string first, string second) => FactionManager.IsAtWarAgainstFaction(Party(first), Party(second));
        public string SettlementId(string id) => Place(id)?.StringId;
        public string SettlementOwner(string id) => Place(id)?.OwnerClan?.Kingdom?.StringId;
        public bool HasRuler(string id) => Party(id)?.RulingClan?.Leader != null;
        public float CessionScore(string first, string second, string from)
        {
            var snapshot = _owner.GetWarSituation(Party(first), Party(second));
            return from == first ? snapshot.AuthorCessionScore : snapshot.TargetCessionScore;
        }
        private WorldDiplomacyCessionCandidate Capture(Settlement settlement, Kingdom receiver)
        {
            if (settlement == null) return default;
            _settlements[settlement.StringId] = settlement;
            return new WorldDiplomacyCessionCandidate(settlement.StringId, settlement.OwnerClan?.Kingdom?.StringId,
                settlement.Culture == receiver?.Culture, settlement.IsTown, settlement.IsTown || settlement.IsCastle, settlement.IsUnderSiege);
        }
        public IEnumerable<WorldDiplomacyCessionCandidate> LostSettlements(string originalOwner, string currentOwner)
        {
            var receiver = Party(originalOwner);
            foreach (var settlement in _owner.GetUnrecoveredLostSettlements(receiver, Party(currentOwner))) yield return Capture(settlement, receiver);
        }
        public IEnumerable<WorldDiplomacyCessionCandidate> OwnedSettlements(string owner, string receiver)
        {
            var receivingKingdom = Party(receiver);
            foreach (var fief in Party(owner).Fiefs) yield return Capture(fief?.Settlement, receivingKingdom);
        }
        internal List<Settlement> ResolveSelected(List<string> ids) => ids.Select(Place).ToList();
        public int FiefCount(string id) => Party(id).Fiefs.Count();
        public float CastleThreshold => CessionCastleUnlockThreshold;
        public float TownThreshold => CessionTownUnlockThreshold;
        public int MaxCandidates => MaxPeaceCessionCandidates;
        public int ClampTribute(string payer, int amount) => DiplomacyPeaceTermsService.ClampTributeAmount(Party(payer), amount);
        public int ResolveDuration(string token, bool hasTribute) => DiplomacyPeaceTermsService.ResolveDurationDays(token, hasTribute);
    }
}
