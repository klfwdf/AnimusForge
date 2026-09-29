using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
namespace AnimusForge;
public sealed partial class WorldDiplomacyBehavior
{
    private sealed class ActionSelectionPort : IWorldDiplomacyActionSelectionPort
    {
        private readonly WorldDiplomacyBehavior _owner;
        private readonly Dictionary<string, Kingdom> _parties = new Dictionary<string, Kingdom>(StringComparer.OrdinalIgnoreCase);
        internal ActionSelectionPort(WorldDiplomacyBehavior owner, Kingdom first, Kingdom second = null)
        {
            _owner = owner;
            if (first != null) _parties[first.StringId] = first;
            if (second != null) _parties[second.StringId] = second;
        }
        private Kingdom Party(string id)
        {
            if (id == null) return null;
            if (!_parties.TryGetValue(id, out Kingdom value)) _parties[id] = value = ResolveKingdom(id);
            return value;
        }
        // One enumeration per target-selection request; cache live identities only for this synchronous call.
        public IEnumerable<string> KingdomIds()
        {
            foreach (Kingdom kingdom in Kingdom.All)
            {
                if (kingdom == null) { yield return null; continue; }
                _parties[kingdom.StringId] = kingdom;
                yield return kingdom.StringId;
            }
        }
        internal List<Kingdom> ResolveSelected(List<string> ids) => ids.Select(Party).ToList();
        public bool HasAuthority(string id) => HasIndependentWorldDiplomacyAuthority(Party(id));
        public bool IsEliminated(string id) => Party(id)?.IsEliminated == true;
        public WorldDiplomacyPairFacts CapturePair(string firstId, string secondId)
        {
            Kingdom first = Party(firstId), second = Party(secondId);
            bool atWar = FactionManager.IsAtWarAgainstFaction(first, second);
            IAllianceCampaignBehavior alliance = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
            ITradeAgreementsCampaignBehavior trade = Campaign.Current?.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
            return new WorldDiplomacyPairFacts(atWar, alliance != null, trade != null,
                alliance != null && alliance.IsAllyWithKingdom(first, second),
                trade != null && BannerlordApiCompat.HasTradeAgreement(trade, first, second));
        }
        public IReadOnlyList<WorldDiplomacyThreat> Threats => _owner._storage?.DiplomaticThreats;
        public bool CanIssueWarThreat(string first, string second) => _owner.CanIssueWarThreat(Party(first), Party(second), out _);
        public bool CanDeclareWar(string first, string second, bool enforcing) => _owner.CanDeclareWar(Party(first), Party(second), out _, enforcing);
        public int LastFailedRoundDay(WorldDiplomacyOfferCooldownKey key) => _owner.GetOfferCooldownLastFailedRoundDay(key);
        public int CooldownDays() => GetTradeAllianceFailedProposalCooldownDays();
        public int CurrentDay() => WorldDiplomacyBehavior.CurrentDay();
        public WorldDiplomacyDocument ResolveDocument(string id) => _owner.ResolveDocument(id);
        public bool IsNonRootAiRelayNoActionAllowed(WorldDiplomacyRound round, string slot, string author, string target, bool relay, bool external, WorldDiplomacyDocument source)
            => WorldDiplomacyNoActionApplication.IsAllowed(round, slot, new NoActionPort(_owner, Party(author), Party(target)), relay, external, source);
        public bool CanUseResultSettlementTarget(WorldDiplomacyRound round, string author, string target)
            => WorldDiplomacyNoActionApplication.CanUseSettlementTarget(round, new NoActionPort(_owner, Party(author), Party(target)));
    }
}
