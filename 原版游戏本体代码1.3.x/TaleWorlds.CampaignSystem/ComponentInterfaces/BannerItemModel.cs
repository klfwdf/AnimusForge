using System;
using System.Collections.Generic;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001F1 RID: 497
	public abstract class BannerItemModel : MBGameModel<BannerItemModel>
	{
		// Token: 0x06001F01 RID: 7937
		public abstract IEnumerable<ItemObject> GetPossibleRewardBannerItems();

		// Token: 0x06001F02 RID: 7938
		public abstract IEnumerable<ItemObject> GetPossibleRewardBannerItemsForHero(Hero hero);

		// Token: 0x06001F03 RID: 7939
		public abstract int GetBannerItemLevelForHero(Hero hero);

		// Token: 0x06001F04 RID: 7940
		public abstract bool CanBannerBeUpdated(ItemObject item);
	}
}
