using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    private sealed class PrestigePort : IWorldDiplomacyPrestigePort
    {
        private readonly Dictionary<string, Hero> _heroes = new Dictionary<string, Hero>(StringComparer.OrdinalIgnoreCase);
        private Kingdom _enumeratedKingdom;
        public bool CampaignAvailable => Campaign.Current != null;
        public IEnumerable<string> KingdomIds(bool activeOnly)
        {
            foreach (Kingdom kingdom in Kingdom.All)
                if (kingdom != null && (!activeOnly || (!kingdom.IsEliminated && !string.IsNullOrWhiteSpace(kingdom.StringId))))
                {
                    _enumeratedKingdom = kingdom;
                    yield return kingdom.StringId;
                }
        }
        public WorldDiplomacyPrestigeCourt CaptureCourt(string kingdomId)
        {
            Kingdom kingdom = _enumeratedKingdom != null && _enumeratedKingdom.StringId == kingdomId
                ? _enumeratedKingdom : ResolveKingdomIncludingEliminated(kingdomId);
            if (kingdom == null || string.IsNullOrWhiteSpace(kingdom.StringId)) return null;
            Hero ruler = kingdom.RulingClan?.Leader;
            var vassals = new List<string>();
            if (ruler != null && kingdom.Clans != null)
            {
                _heroes[ruler.StringId] = ruler;
                for (int index = 0; index < kingdom.Clans.Count; index++)
                {
                    Clan clan = kingdom.Clans[index];
                    Hero vassal = clan?.Leader;
                    if (clan == null || clan == kingdom.RulingClan || clan.Kingdom != kingdom || clan.IsEliminated
                        || clan.IsUnderMercenaryService || clan.IsClanTypeMercenary || vassal == null || vassal == ruler) continue;
                    _heroes[vassal.StringId] = vassal;
                    vassals.Add(vassal.StringId);
                }
            }
            return new WorldDiplomacyPrestigeCourt(kingdom.StringId, kingdom.IsEliminated, ruler?.StringId, vassals);
        }
        public bool HasHero(string id)
        {
            Hero hero = ResolveHeroById(id);
            if (hero == null) return false;
            _heroes[id] = hero;
            return true;
        }
        public int ChangeRelationAndMeasure(string first, string second, int difference)
        {
            try
            {
                Hero vassal = _heroes[first], ruler = _heroes[second];
                int before = CharacterRelationManager.GetHeroRelation(vassal, ruler);
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(vassal, ruler, difference, showQuickNotification: false);
                int after = CharacterRelationManager.GetHeroRelation(vassal, ruler);
                return after - before;
            }
            catch { return 0; }
        }
        public void ChangeRelation(string first, string second, int difference)
        {
            try { ChangeRelationAction.ApplyRelationChangeBetweenHeroes(_heroes[first], _heroes[second], difference, showQuickNotification: false); }
            catch { }
        }
        public string KingdomName(string id) => WorldDiplomacyBehavior.KingdomName(ResolveKingdomIncludingEliminated(id));
        public int CurrentDay() => WorldDiplomacyBehavior.CurrentDay();
        public void Log(string message) => WorldDiplomacyBehavior.Log(message);
    }
}
