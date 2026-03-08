using System;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x0200018E RID: 398
	public abstract class ShipCostModel : MBGameModel<ShipCostModel>
	{
		// Token: 0x06001BF1 RID: 7153
		public abstract float GetShipTradeValue(Ship ship, PartyBase seller, PartyBase buyer);

		// Token: 0x06001BF2 RID: 7154
		public abstract float GetShipRepairCost(Ship ship, PartyBase owner);

		// Token: 0x06001BF3 RID: 7155
		public abstract int GetShipUpgradePieceCost(Ship ship, ShipUpgradePiece piece, PartyBase owner);

		// Token: 0x06001BF4 RID: 7156
		public abstract float GetShipSellingPenalty();
	}
}
