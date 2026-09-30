using System;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Adapters;
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
            var clans = new List<WorldDiplomacyClanSnapshot>();
            if (ruler != null && kingdom.Clans != null)
            {
                _heroes[ruler.StringId] = ruler;
                for (int index = 0; index < kingdom.Clans.Count; index++)
                {
                    Clan clan = kingdom.Clans[index];
                    Hero vassal = clan?.Leader;
                    if (vassal != null) _heroes[vassal.StringId] = vassal;
                    clans.Add(CaptureClanSnapshot(clan, kingdom, kingdom.RulingClan));
                }
            }
            return new WorldDiplomacyPrestigeCourt(kingdom.StringId, kingdom.IsEliminated, ruler?.StringId, clans);
        }
        public bool HasHero(string id)
        {
            Hero hero = ResolveHeroById(id);
            if (hero == null) return false;
            _heroes[id] = hero;
            return true;
        }
        public bool TryReadRelation(string first, string second, out int value)
        {
            value = 0;
            try { value = CharacterRelationManager.GetHeroRelation(_heroes[first], _heroes[second]); return true; }
            catch { return false; }
        }
        public WorldDiplomacyRelationEffectReceipt ChangeRelationAndMeasure(string first, string second, int difference)
        {
            if (!_heroes.TryGetValue(first, out Hero vassal) || !_heroes.TryGetValue(second, out Hero ruler))
                return new(true, 0, "hero unavailable");
            return DiplomacyRelationEffect.Apply(
                () => CharacterRelationManager.GetHeroRelation(vassal, ruler),
                () => ChangeRelationAction.ApplyRelationChangeBetweenHeroes(vassal, ruler, difference, showQuickNotification: false));
        }
        public string KingdomName(string id) => WorldDiplomacyBehavior.KingdomName(ResolveKingdomIncludingEliminated(id));
        public int CurrentDay() => WorldDiplomacyBehavior.CurrentDay();
        public void Log(string message) => WorldDiplomacyBehavior.Log(message);
    }
}
