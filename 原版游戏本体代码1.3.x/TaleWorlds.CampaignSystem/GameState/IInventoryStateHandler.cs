using System;

namespace TaleWorlds.CampaignSystem.GameState
{
	// Token: 0x02000394 RID: 916
	public interface IInventoryStateHandler
	{
		// Token: 0x0600348E RID: 13454
		void ExecuteLootingScript();

		// Token: 0x0600348F RID: 13455
		void ExecuteSellAllLoot();

		// Token: 0x06003490 RID: 13456
		void ExecuteBuyConsumableItem();
	}
}
