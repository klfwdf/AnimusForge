using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    // Stack-local synchronous snapshot source; never retained by Application.
    private readonly struct WarAdmissionPort : IWorldDiplomacyWarAdmissionPort
    {
        private readonly Kingdom _initiator;
        private readonly Kingdom _target;
        internal WarAdmissionPort(WorldDiplomacyBehavior owner, Kingdom initiator, Kingdom target)
        { _initiator = initiator; _target = target; }
        public bool ValidPair => _initiator != null && _target != null && _initiator != _target && !_initiator.IsEliminated && !_target.IsEliminated;
        public bool HasIndependentAuthority => HasIndependentWorldDiplomacyAuthority(_initiator) && HasIndependentWorldDiplomacyAuthority(_target);
        public bool BlocksNewOffensiveWar => AnimusForge.Refactor.Modules.TeamModuleServices.CivilWar.BlocksNewOffensiveWar(_initiator);
        public bool AtWar => FactionManager.IsAtWarAgainstFaction(_initiator, _target);
        public bool Allied => Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>()?.IsAllyWithKingdom(_initiator, _target) == true;
    }
}
