using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.View;

public class BannerVisualCreator : IBannerVisualCreator
{
	IBannerVisual IBannerVisualCreator.CreateBannerVisual(Banner banner)
	{
		return new BannerVisual(banner);
	}
}
