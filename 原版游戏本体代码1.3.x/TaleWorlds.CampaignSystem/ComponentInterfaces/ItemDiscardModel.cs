using System;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x02000189 RID: 393
	public abstract class ItemDiscardModel : MBGameModel<ItemDiscardModel>
	{
		// Token: 0x06001BD9 RID: 7129
		public abstract int GetXpBonusForDiscardingItems(ItemRoster itemRoster);

		// Token: 0x06001BDA RID: 7130
		public abstract int GetXpBonusForDiscardingItem(ItemObject item, int amount = 1);

		// Token: 0x06001BDB RID: 7131
		public abstract bool PlayerCanDonateItem(ItemObject item);
	}
}
