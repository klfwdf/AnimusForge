using System.Collections.Generic;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
namespace AnimusForge;
public sealed partial class WorldDiplomacyBehavior
{
    private sealed class BattlePort : IWorldDiplomacyBattlePort
    {
        private readonly MapEvent mapEvent;
        internal BattlePort(MapEvent value) => mapEvent=value;
        public bool Exists => mapEvent != null;
        public bool HasWinner => mapEvent?.HasWinner == true;
        public bool IsHideout => mapEvent?.IsHideoutBattle == true;
        public string EventId => mapEvent?.StringId;
        public List<string> AttackerKingdomIds() => ResolveMapEventSideKingdomIds(mapEvent.AttackerSide);
        public List<string> DefenderKingdomIds() => ResolveMapEventSideKingdomIds(mapEvent.DefenderSide);
        public WorldDiplomacyBattleFact CaptureDetails(List<string> attackers, List<string> defenders)
        {
            int day=CurrentDay();
			return new WorldDiplomacyBattleFact
			{

				Day = day,
				GameDate = FormatCampaignDate(day),
				BattleType = ResolveMapEventBattleType(mapEvent),
				Location = mapEvent.MapEventSettlement?.Name?.ToString() ?? "閲庡",
				AttackerKingdomIds = attackers,
				DefenderKingdomIds = defenders,
				AttackerLeaderNames = ResolveMapEventSideLeaderNames(mapEvent.AttackerSide),
				DefenderLeaderNames = ResolveMapEventSideLeaderNames(mapEvent.DefenderSide),
				WinnerSide = mapEvent.WinningSide == BattleSideEnum.Attacker ? "attacker" : "defender",
				IsPlayerInvolved = mapEvent.IsPlayerMapEvent
			};
        }
        public void Log(string message) => WorldDiplomacyBehavior.Log(message);
    }
}
