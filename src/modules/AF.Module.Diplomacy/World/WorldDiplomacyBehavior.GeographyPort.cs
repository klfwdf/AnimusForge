using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
namespace AnimusForge;
public sealed partial class WorldDiplomacyBehavior
{
    private sealed class GeographyPort : IWorldDiplomacyGeographyPort
    {
        private readonly WorldDiplomacyBehavior owner;
        private readonly Kingdom author;
        private readonly Settlement origin;
        private readonly List<Kingdom> kingdoms = new List<Kingdom>();
        private readonly List<Settlement> settlements = new List<Settlement>();
        internal GeographyPort(WorldDiplomacyBehavior owner, string authorId, string originId)
        { this.owner=owner; author=ResolveKingdom(authorId); origin=ResolveSettlementById(originId); }
        public bool OriginAvailable => origin != null;
        public IReadOnlyList<WorldDiplomacySettlementDistance> Settlements()
        {
            var result = new List<WorldDiplomacySettlementDistance>();
            settlements.Clear();
            foreach (Settlement settlement in Settlement.All)
                if (settlement != null)
                {
                    result.Add(new WorldDiplomacySettlementDistance
                    { Id=settlement.StringId, Index=settlements.Count, IsHideout=settlement.IsHideout, IsOrigin=settlement==origin });
                    settlements.Add(settlement);
                }
            return result;
        }
        public float SettlementDistance(int index) => origin == null ? 0f : origin.GatePosition.Distance(settlements[index].GatePosition);
        public IReadOnlyList<WorldDiplomacyKingdomDestination> Kingdoms()
        {
            kingdoms.Clear(); var result = new List<WorldDiplomacyKingdomDestination>();
            foreach (Kingdom kingdom in Kingdom.All)
                if (kingdom != null)
                {
                    result.Add(new WorldDiplomacyKingdomDestination
                    { Id=kingdom.StringId, Index=kingdoms.Count, IsEliminated=kingdom.IsEliminated, IsAuthor=kingdom==author });
                    kingdoms.Add(kingdom);
                }
            return result;
        }
        public WorldDiplomacyCourtDistance Court(int index)
        {
            Kingdom kingdom=kingdoms[index]; Settlement court=owner.ResolveCourtSettlement(kingdom);
            return new WorldDiplomacyCourtDistance
            { KingdomId=kingdom.StringId, SettlementId=court?.StringId ?? "", IsPlayerAffiliated=IsPlayerAffiliatedKingdom(kingdom),
                DistanceKnown=origin != null && court != null, Distance=origin == null || court == null ? 0f : origin.GatePosition.Distance(court.GatePosition) };
        }
    }
}
