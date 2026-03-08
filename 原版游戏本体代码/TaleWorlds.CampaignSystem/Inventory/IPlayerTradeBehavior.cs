using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Inventory
{
	// Token: 0x020000D4 RID: 212
	public interface IPlayerTradeBehavior
	{
		// Token: 0x06001468 RID: 5224
		int GetProjectedProfit(ItemRosterElement itemRosterElement, int itemCost);
	}
}
