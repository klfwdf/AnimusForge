using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000152 RID: 338
	public class DefaultShipCostModel : ShipCostModel
	{
		// Token: 0x06001A38 RID: 6712 RVA: 0x000840C7 File Offset: 0x000822C7
		public override float GetShipTradeValue(Ship ship, PartyBase seller, PartyBase buyer)
		{
			return 0f;
		}

		// Token: 0x06001A39 RID: 6713 RVA: 0x000840CE File Offset: 0x000822CE
		public override float GetShipRepairCost(Ship ship, PartyBase owner)
		{
			return 0f;
		}

		// Token: 0x06001A3A RID: 6714 RVA: 0x000840D5 File Offset: 0x000822D5
		public override int GetShipUpgradePieceCost(Ship ship, ShipUpgradePiece piece, PartyBase owner)
		{
			return 0;
		}

		// Token: 0x06001A3B RID: 6715 RVA: 0x000840D8 File Offset: 0x000822D8
		public override float GetShipSellingPenalty()
		{
			return 0f;
		}
	}
}
