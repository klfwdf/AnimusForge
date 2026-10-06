using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class CombatSimulationModel : MBGameModel<CombatSimulationModel>
{
	public abstract ExplainedNumber SimulateHit(CharacterObject strikerTroop, CharacterObject struckTroop, PartyBase strikerParty, PartyBase struckParty, float strikerAdvantage, MapEvent battle, BattleEnvironment battleEnvironment, float strikerSideMorale, float struckSideMorale);

	public abstract ExplainedNumber SimulateHit(Ship strikerShip, Ship struckShip, PartyBase strikerParty, PartyBase struckParty, SiegeEngineType siegeEngine, float strikerAdvantage, MapEvent battle, out int troopCasualties);

	public abstract (int defenderRounds, int attackerRounds) GetSimulationTicksForBattleRound(MapEvent mapEvent);

	public abstract int GetNumberOfEquipmentsBuilt(Settlement settlement);

	public abstract float GetMaximumSiegeEquipmentProgress(Settlement settlement);

	public abstract float GetSettlementAdvantage(Settlement settlement);

	public abstract void GetBattleAdvantage(MapEvent mapEvent, out ExplainedNumber defenderAdvantage, out ExplainedNumber attackerAdvantage);

	public abstract float GetShipSiegeEngineHitChance(Ship ship, SiegeEngineType siegeEngineType, BattleSideEnum battleSide);

	public abstract int GetPursuitRoundCount(MapEvent mapEvent);

	public abstract float GetBluntDamageChance(CharacterObject strikerTroop, CharacterObject strikedTroop, PartyBase strikerParty, PartyBase strikedParty, MapEvent battle);

	public abstract CampaignTime GetSimulationTickInterval(MapEvent mapEvent);

	public abstract int GetParticipatingTroopCount(MapEventSide side);

	public abstract float GetShipCombatImportance(Ship ship);

	public abstract float GetShipCombatScore(Ship ship);
}
