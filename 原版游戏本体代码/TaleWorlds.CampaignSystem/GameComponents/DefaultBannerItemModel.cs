using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x020000F2 RID: 242
	public class DefaultBannerItemModel : BannerItemModel
	{
		// Token: 0x0600163B RID: 5691 RVA: 0x00065673 File Offset: 0x00063873
		public override IEnumerable<ItemObject> GetPossibleRewardBannerItems()
		{
			return Items.All.WhereQ((ItemObject i) => i.IsBannerItem && i.StringId != "campaign_banner_small");
		}

		// Token: 0x0600163C RID: 5692 RVA: 0x000656A0 File Offset: 0x000638A0
		public override IEnumerable<ItemObject> GetPossibleRewardBannerItemsForHero(Hero hero)
		{
			IEnumerable<ItemObject> possibleRewardBannerItems = this.GetPossibleRewardBannerItems();
			int bannerItemLevelForHero = this.GetBannerItemLevelForHero(hero);
			List<ItemObject> list = new List<ItemObject>();
			foreach (ItemObject itemObject in possibleRewardBannerItems)
			{
				if ((itemObject.Culture == null || itemObject.Culture == hero.Culture) && (itemObject.ItemComponent as BannerComponent).BannerLevel == bannerItemLevelForHero)
				{
					list.Add(itemObject);
				}
			}
			return list;
		}

		// Token: 0x0600163D RID: 5693 RVA: 0x00065728 File Offset: 0x00063928
		public override int GetBannerItemLevelForHero(Hero hero)
		{
			if (hero.Clan == null || hero.Clan.Leader != hero)
			{
				return 1;
			}
			if (hero.MapFaction.IsKingdomFaction && hero.Clan.Kingdom.RulingClan == hero.Clan)
			{
				return 3;
			}
			return 2;
		}

		// Token: 0x0600163E RID: 5694 RVA: 0x00065775 File Offset: 0x00063975
		public override bool CanBannerBeUpdated(ItemObject item)
		{
			return true;
		}

		// Token: 0x0400075D RID: 1885
		public const int BannerLevel1 = 1;

		// Token: 0x0400075E RID: 1886
		public const int BannerLevel2 = 2;

		// Token: 0x0400075F RID: 1887
		public const int BannerLevel3 = 3;

		// Token: 0x04000760 RID: 1888
		private const string MapBannerId = "campaign_banner_small";
	}
}
