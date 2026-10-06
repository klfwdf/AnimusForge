using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Helpers;

public static class BannerHelper
{
	public static ItemObject GetRandomBannerItemForHero(Hero hero)
	{
		return Campaign.Current.Models.BannerItemModel.GetPossibleRewardBannerItemsForHero(hero).GetRandomElementInefficiently();
	}

	public static void AddBannerBonusForBanner(BannerEffect bannerEffect, BannerComponent bannerComponent, ref ExplainedNumber bonuses)
	{
		if (bannerComponent != null && bannerComponent.BannerEffect == bannerEffect)
		{
			AddBannerEffectToStat(ref bonuses, bannerEffect.IncrementType, bannerComponent.GetBannerEffectBonus(), bannerEffect.Name);
		}
	}

	private static void AddBannerEffectToStat(ref ExplainedNumber stat, EffectIncrementType effectIncrementType, float number, TextObject effectName)
	{
		switch (effectIncrementType)
		{
		case EffectIncrementType.Add:
			stat.Add(number, effectName);
			break;
		case EffectIncrementType.AddFactor:
			stat.AddFactor(number, effectName);
			break;
		}
	}
}
