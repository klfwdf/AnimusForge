using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    // Stack-local synchronous snapshot source; never retained by Application.
    private readonly struct WarAdmissionPort : IWorldDiplomacyWarAdmissionPort
    {
        private readonly WorldDiplomacyBehavior _owner;
        private readonly Kingdom _initiator;
        private readonly Kingdom _target;
        internal WarAdmissionPort(WorldDiplomacyBehavior owner, Kingdom initiator, Kingdom target)
        { _owner = owner; _initiator = initiator; _target = target; }
        public bool ValidPair => _initiator != null && _target != null && _initiator != _target && !_initiator.IsEliminated && !_target.IsEliminated;
        public bool HasIndependentAuthority => HasIndependentWorldDiplomacyAuthority(_initiator) && HasIndependentWorldDiplomacyAuthority(_target);
        public bool BlocksNewOffensiveWar => AnimusForge.Refactor.Modules.TeamModuleServices.CivilWar.BlocksNewOffensiveWar(_initiator);
        public bool AtWar => FactionManager.IsAtWarAgainstFaction(_initiator, _target);
        public bool Allied => Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>()?.IsAllyWithKingdom(_initiator, _target) == true;
        public bool PendingThreatDecision => WorldDiplomacyRoundLifecycleRules.IsThreatDecisionPending(
            WorldDiplomacyRoundLifecycleRules.SelectOpenThreatBetween(_owner._storage?.DiplomaticThreats, _initiator.StringId, _target.StringId));
        public int CurrentDay => WorldDiplomacyBehavior.CurrentDay();
        public int PeaceProtectionDays => GetPeaceProtectionDays();
        public bool TryGetLastPeaceDay(out int day) => _owner._storage.LastPeaceDayByPair.TryGetValue(WorldDiplomacyRoundLifecycleRules.PairKey(_initiator.StringId, _target.StringId), out day);
        public int OffensiveWarCooldownDays => GetOffensiveWarCooldownDays();
        public bool TryGetLastOffensiveWarDay(out int day) => _owner._storage.LastOffensiveWarDayByKingdom.TryGetValue(_initiator.StringId, out day);
        public int MaxConcurrentOffensiveWars => FixedMaxConcurrentOffensiveWars;
        public int ActiveWars
        {
            get
            {
                Kingdom initiator = _initiator;
                return Kingdom.All.Count(x => x != null && !x.IsEliminated && x != initiator && FactionManager.IsAtWarAgainstFaction(initiator, x));
            }
        }
    }
}
