using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Settlements
{
	// Token: 0x020003BD RID: 957
	public interface IMarketData
	{
		// Token: 0x0600388D RID: 14477
		int GetPrice(ItemObject item, MobileParty tradingParty, bool isSelling, PartyBase merchantParty);

		// Token: 0x0600388E RID: 14478
		int GetPrice(EquipmentElement itemRosterElement, MobileParty tradingParty, bool isSelling, PartyBase merchantParty);
	}
}
