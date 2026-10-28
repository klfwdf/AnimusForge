using System.Collections.Generic;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultBannerItemModel : BannerItemModel
{
	public const int BannerLevel1 = 1;

	public const int BannerLevel2 = 2;

	public const int BannerLevel3 = 3;

	private const string MapBannerId = "campaign_banner_small";

	public override IEnumerable<ItemObject> GetPossibleRewardBannerItems()
	{
		return Items.All.WhereQ((ItemObject i) => i.IsBannerItem && i.StringId != "campaign_banner_small");
	}

	public override IEnumerable<ItemObject> GetPossibleRewardBannerItemsForHero(Hero hero)
	{
		IEnumerable<ItemObject> possibleRewardBannerItems = GetPossibleRewardBannerItems();
		int bannerItemLevelForHero = GetBannerItemLevelForHero(hero);
		List<ItemObject> list = new List<ItemObject>();
		foreach (ItemObject item in possibleRewardBannerItems)
		{
			if ((item.Culture == null || item.Culture == hero.Culture) && (item.ItemComponent as BannerComponent).BannerLevel == bannerItemLevelForHero)
			{
				list.Add(item);
			}
		}
		return list;
	}

	public override int GetBannerItemLevelForHero(Hero hero)
	{
		if (hero.Clan != null && hero.Clan.Leader == hero)
		{
			if (hero.MapFaction.IsKingdomFaction && hero.Clan.Kingdom.RulingClan == hero.Clan)
			{
				return 3;
			}
			return 2;
		}
		return 1;
	}

	public override bool CanBannerBeUpdated(ItemObject item)
	{
		return true;
	}
}
