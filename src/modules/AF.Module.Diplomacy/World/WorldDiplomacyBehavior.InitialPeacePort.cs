using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace AnimusForge;
public sealed partial class WorldDiplomacyBehavior
{
    private struct InitialPeacePort : IWorldDiplomacyInitialPeacePort
    {
        private readonly WorldDiplomacyBehavior _owner;
        private Dictionary<string, Kingdom> _kingdoms;
        internal InitialPeacePort(WorldDiplomacyBehavior owner) { _owner = owner; _kingdoms = null; }
        public bool CampaignAvailable => Campaign.Current != null;
        public bool Enabled => IsWorldDiplomacyEnabled();
        public IEnumerable<string> ActiveKingdomIds()
        {
            _kingdoms = new Dictionary<string, Kingdom>(StringComparer.OrdinalIgnoreCase);
            var ids = new List<string>();
            foreach (Kingdom kingdom in Kingdom.All)
                if (kingdom != null && !kingdom.IsEliminated)
                { _kingdoms[kingdom.StringId] = kingdom; ids.Add(kingdom.StringId); }
            return ids;
        }
        public bool IsAtWar(string first, string second) => FactionManager.IsAtWarAgainstFaction(_kingdoms[first], _kingdoms[second]);
        public void MakePeace(string first, string second)
        {
            Kingdom firstKingdom = _kingdoms[first], secondKingdom = _kingdoms[second];
            RunDiplomaticAction("world_diplomacy_initial_peace", () => MakePeaceAction.Apply(firstKingdom, secondKingdom));
        }
        public void SanitizeNativeQueue() => _owner.RemoveQueuedNativeDiplomacyDecisions();
        public void ClearWarSituationCache() => _owner._warSituationCache.Clear();
        public int CurrentDay() => WorldDiplomacyBehavior.CurrentDay();
        public void Log(string message) => WorldDiplomacyBehavior.Log(message);
    }
}
