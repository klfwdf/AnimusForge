using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class TroopSacrificeModel : MBGameModel<TroopSacrificeModel>
{
	public abstract int BreakOutArmyLeaderRelationPenalty { get; }

	public abstract int BreakOutArmyMemberRelationPenalty { get; }

	public abstract ExplainedNumber GetLostTroopCountForBreakingInBesiegedSettlement(MobileParty party, SiegeEvent siegeEvent);

	public abstract ExplainedNumber GetLostTroopCountForBreakingOutOfBesiegedSettlement(MobileParty party, SiegeEvent siegeEvent, bool isBreakingOutFromPort);

	public abstract int GetNumberOfTroopsSacrificedForTryingToGetAway(BattleSideEnum playerBattleSide, MapEvent mapEvent);

	public abstract void GetShipsToSacrificeForTryingToGetAway(BattleSideEnum playerBattleSide, MapEvent mapEvent, out MBList<Ship> shipsToCapture, out Ship shipToTakeDamage, out float damageToApplyForLastShip);

	public abstract bool CanPlayerGetAwayFromEncounter(out TextObject explanation);
}
