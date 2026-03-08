using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001C0 RID: 448
	public abstract class TradeItemPriceFactorModel : MBGameModel<TradeItemPriceFactorModel>
	{
		// Token: 0x06001D97 RID: 7575
		public abstract float GetTradePenalty(ItemObject item, MobileParty clientParty, PartyBase merchant, bool isSelling, float inStore, float supply, float demand);

		// Token: 0x06001D98 RID: 7576
		public abstract float GetBasePriceFactor(ItemCategory itemCategory, float inStoreValue, float supply, float demand, bool isSelling, int transferValue);

		// Token: 0x06001D99 RID: 7577
		public abstract int GetPrice(EquipmentElement itemRosterElement, MobileParty clientParty, PartyBase merchant, bool isSelling, float inStoreValue, float supply, float demand);

		// Token: 0x06001D9A RID: 7578
		public abstract int GetTheoreticalMaxItemMarketValue(ItemObject item);
	}
}
