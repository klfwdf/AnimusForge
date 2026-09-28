using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    // Short-lived synchronous adapter: one Clan.All scan per consequence attempt.
    private sealed class ThreatSettlementPort : IWorldDiplomacyThreatSettlementPort
    {
        private readonly WorldDiplomacyBehavior _owner;
        private Dictionary<string, Clan> _clans;
        internal ThreatSettlementPort(WorldDiplomacyBehavior owner) { _owner = owner; }
        public bool CampaignAvailable => Campaign.Current != null;
        public int IssuerRelationRewardMax => DuelSettings.WorldDiplomacyThreatComplianceIssuerRelationRewardMax;
        public int UltimatumComplianceRoyalRelationPenalty => WorldDiplomacyBehavior.UltimatumComplianceRoyalRelationPenalty;
        public int UltimatumCompliancePrestigeChange => WorldDiplomacyBehavior.UltimatumCompliancePrestigeChange;
        public int WarningCompliancePrestigeChange => WorldDiplomacyBehavior.WarningCompliancePrestigeChange;
        public int UltimatumFollowThroughPrestigePenalty => WorldDiplomacyBehavior.UltimatumFollowThroughPrestigePenalty;
        public int WarningFollowThroughPrestigePenalty => WorldDiplomacyBehavior.WarningFollowThroughPrestigePenalty;
        public int ZeroPrestigeUltimatumBreachRelationPenalty => WorldDiplomacyBehavior.ZeroPrestigeUltimatumBreachRelationPenalty;
        public int ZeroPrestigeWarningBreachRelationPenalty => WorldDiplomacyBehavior.ZeroPrestigeWarningBreachRelationPenalty;
        public int GetThreatComplianceIssuerRelationReward() => WorldDiplomacyBehavior.GetThreatComplianceIssuerRelationReward();
        public int CurrentDay() => WorldDiplomacyBehavior.CurrentDay();
        public void Log(string message) => WorldDiplomacyBehavior.Log(message);
        public string KingdomName(string id) => WorldDiplomacyBehavior.KingdomName(ResolveKingdomIncludingEliminated(id));
        public WorldDiplomacyConsequenceParty ReadParty(string id)
        {
            Kingdom kingdom = ResolveKingdomIncludingEliminated(id);
            return kingdom == null ? null : new WorldDiplomacyConsequenceParty(kingdom.StringId, kingdom.IsEliminated, kingdom.RulingClan != null);
        }
        public WorldDiplomacyConsequenceSnapshot CaptureConsequenceSnapshot(string id)
        {
            Kingdom kingdom = ResolveKingdomIncludingEliminated(id);
            Clan ruler = kingdom?.RulingClan;
            if (ruler == null || string.IsNullOrWhiteSpace(ruler.StringId)) return null;
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (kingdom.Clans != null)
                for (int index = 0; index < kingdom.Clans.Count; index++)
                {
                    Clan clan = kingdom.Clans[index];
                    if (IsThreatConsequenceClanEligible(clan, kingdom, ruler)) ids.Add(clan.StringId);
                }
            return new WorldDiplomacyConsequenceSnapshot(ruler.StringId, ids);
        }
        public void PrepareClans(HashSet<string> requiredClanIds)
        {
            _clans = new Dictionary<string, Clan>(requiredClanIds.Count, StringComparer.OrdinalIgnoreCase);
            foreach (Clan clan in Clan.All)
                if (clan != null && !string.IsNullOrWhiteSpace(clan.StringId) && requiredClanIds.Contains(clan.StringId))
                    _clans[clan.StringId] = clan;
        }
        public WorldDiplomacyConsequenceClan ReadClan(string id)
        {
            if (!_clans.TryGetValue(id, out Clan clan) || clan == null) return null;
            Hero leader = clan.Leader;
            return new WorldDiplomacyConsequenceClan(clan.IsEliminated, leader?.StringId);
        }
        public int ReadRelation(string first, string second) => CharacterRelationManager.GetHeroRelation(_clans[first].Leader, _clans[second].Leader);
        public void ChangeRelation(string first, string second, int amount) =>
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(_clans[first].Leader, _clans[second].Leader, amount, showQuickNotification: false);
        public bool CancelPolicy(string policyId, string ownerId, string reason, out string policyName, out string result) =>
            CustomPolicyBehavior.TryCancelActiveKingdomPolicyForExternal(policyId, ownerId, reason, out policyName, out result);
        public int ApplyNationalPrestigeDelta(string id, int delta, WorldDiplomacyDocument doc, string reason) => _owner.ApplyNationalPrestigeDelta(id, delta, doc, reason);
        public void ApplyZeroPrestigeBreachRelationPenalty(string id, int amount) => _owner.ApplyZeroPrestigeBreachRelationPenalty(ResolveKingdomIncludingEliminated(id), amount);
        public WorldDiplomacyDocument ResolveDocument(string id) => _owner.ResolveDocument(id);
        public void AppendCanonicalDocumentEvents(WorldDiplomacyDocument document) => _owner.AppendCanonicalDocumentEvents(document);
        public void ScheduleDeferredCanonicalHistoryRetry(string id) => _owner.ScheduleDeferredCanonicalHistoryRetry(id);
        public void TryAppendDiplomaticThreatHistoryResult(WorldDiplomacyThreat threat) => _owner.TryAppendDiplomaticThreatHistoryResult(threat);
        public void TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(WorldDiplomacyThreat threat) => _owner.TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(threat);
        public void TryAppendDiplomaticThreatIssuerRewardHistoryResult(WorldDiplomacyThreat threat) => _owner.TryAppendDiplomaticThreatIssuerRewardHistoryResult(threat);
        public void TryAppendDiplomaticThreatNonComplianceHistoryResult(WorldDiplomacyThreat threat, WorldDiplomacyThreatNonComplianceEvent decision) => _owner.TryAppendDiplomaticThreatNonComplianceHistoryResult(threat, decision);
    }
}

