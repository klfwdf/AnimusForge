using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace MBHelpers;

public static class BannerHelper
{
	public static void AddBannerBonusForBanner(BannerEffect bannerEffect, BannerComponent bannerComponent, ref FactoredNumber bonuses)
	{
		if (bannerComponent != null && bannerComponent.BannerEffect == bannerEffect)
		{
			AddBannerEffectToStat(ref bonuses, bannerEffect.IncrementType, bannerComponent.GetBannerEffectBonus());
		}
	}

	private static void AddBannerEffectToStat(ref FactoredNumber stat, EffectIncrementType effectIncrementType, float number)
	{
		switch (effectIncrementType)
		{
		case EffectIncrementType.Add:
			stat.Add(number);
			break;
		case EffectIncrementType.AddFactor:
			stat.AddFactor(number);
			break;
		}
	}
}
