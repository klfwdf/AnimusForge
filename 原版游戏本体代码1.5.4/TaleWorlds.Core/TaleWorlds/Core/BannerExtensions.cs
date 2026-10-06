namespace TaleWorlds.Core;

public static class BannerExtensions
{
	public static bool IsContentsSameWith(this Banner banner, Banner otherBanner)
	{
		if (banner == null && otherBanner == null)
		{
			return true;
		}
		if (banner == null || otherBanner == null)
		{
			return false;
		}
		if (banner.GetBannerDataListCount() != otherBanner.GetBannerDataListCount())
		{
			return false;
		}
		for (int i = 0; i < banner.GetBannerDataListCount(); i++)
		{
			BannerData bannerDataAtIndex = banner.GetBannerDataAtIndex(i);
			BannerData bannerDataAtIndex2 = otherBanner.GetBannerDataAtIndex(i);
			if (!bannerDataAtIndex.Equals(bannerDataAtIndex2))
			{
				return false;
			}
		}
		return true;
	}
}
