using System;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x0200019C RID: 412
	public abstract class CombatSimulationModel : MBGameModel<CombatSimulationModel>
	{
		// Token: 0x06001C46 RID: 7238
		public abstract ExplainedNumber SimulateHit(CharacterObject strikerTroop, CharacterObject struckTroop, PartyBase strikerParty, PartyBase struckParty, float strikerAdvantage, MapEvent battle, float strikerSideMorale, float struckSideMorale);

		// Token: 0x06001C47 RID: 7239
		public abstract ExplainedNumber SimulateHit(Ship strikerShip, Ship struckShip, PartyBase strikerParty, PartyBase struckParty, SiegeEngineType siegeEngine, float strikerAdvantage, MapEvent battle, out int troopCasualties);

		// Token: 0x06001C48 RID: 7240
		[return: TupleElementNames(new string[] { "defenderRounds", "attackerRounds" })]
		public abstract ValueTuple<int, int> GetSimulationTicksForBattleRound(MapEvent mapEvent);

		// Token: 0x06001C49 RID: 7241
		public abstract int GetNumberOfEquipmentsBuilt(Settlement settlement);

		// Token: 0x06001C4A RID: 7242
		public abstract float GetMaximumSiegeEquipmentProgress(Settlement settlement);

		// Token: 0x06001C4B RID: 7243
		public abstract float GetSettlementAdvantage(Settlement settlement);

		// Token: 0x06001C4C RID: 7244
		public abstract void GetBattleAdvantage(MapEvent mapEvent, out ExplainedNumber defenderAdvantage, out ExplainedNumber attackerAdvantage);

		// Token: 0x06001C4D RID: 7245
		public abstract float GetShipSiegeEngineHitChance(Ship ship, SiegeEngineType siegeEngineType, BattleSideEnum battleSide);

		// Token: 0x06001C4E RID: 7246
		public abstract int GetPursuitRoundCount(MapEvent mapEvent);

		// Token: 0x06001C4F RID: 7247
		public abstract float GetBluntDamageChance(CharacterObject strikerTroop, CharacterObject strikedTroop, PartyBase strikerParty, PartyBase strikedParty, MapEvent battle);
	}
}
