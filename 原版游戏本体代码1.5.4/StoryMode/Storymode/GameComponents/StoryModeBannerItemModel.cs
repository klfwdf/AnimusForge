using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;

namespace StoryMode.GameComponents;

public class StoryModeBannerItemModel : BannerItemModel
{
	public override IEnumerable<ItemObject> GetPossibleRewardBannerItems()
	{
		if (!StoryModeManager.Current.MainStoryLine.TutorialPhase.IsCompleted)
		{
			return new List<ItemObject>();
		}
		return base.BaseModel.GetPossibleRewardBannerItems().WhereQ((ItemObject i) => !IsItemDragonBanner(i));
	}

	public override bool CanBannerBeUpdated(ItemObject item)
	{
		if (IsItemDragonBanner(item))
		{
			return false;
		}
		return base.BaseModel.CanBannerBeUpdated(item);
	}

	private bool IsItemDragonBanner(ItemObject item)
	{
		if (!(item.StringId == "dragon_banner") && !(item.StringId == "dragon_banner_center") && !(item.StringId == "dragon_banner_dragonhead"))
		{
			return item.StringId == "dragon_banner_handle";
		}
		return true;
	}

	public override IEnumerable<ItemObject> GetPossibleRewardBannerItemsForHero(Hero hero)
	{
		return base.BaseModel.GetPossibleRewardBannerItemsForHero(hero).WhereQ((ItemObject b) => !IsItemDragonBanner(b));
	}

	public override int GetBannerItemLevelForHero(Hero hero)
	{
		return base.BaseModel.GetBannerItemLevelForHero(hero);
	}
}
