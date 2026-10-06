using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

namespace TaleWorlds.MountAndBlade.GauntletUI.TextureProviders.ImageIdentifiers;

public class BannerImageTextureProvider : ImageIdentifierTextureProvider
{
	protected override void OnCreateImageWithId(string id, string additionalArgs)
	{
		if (string.IsNullOrEmpty(id))
		{
			OnTextureCreated(null);
			return;
		}
		Banner banner = new Banner(id);
		BannerDebugInfo debugInfo = BannerDebugInfo.CreateWidget(base.SourceInfo ?? GetType().Name);
		if (additionalArgs == "ninegrid")
		{
			base.ThumbnailCreationData = new BannerThumbnailCreationData(banner, base.OnTextureCreated, base.OnTextureCreationCancelled, debugInfo, isTableauOrNineGrid: true, isLarge: true);
		}
		else
		{
			base.ThumbnailCreationData = new BannerThumbnailCreationData(banner, base.OnTextureCreated, base.OnTextureCreationCancelled, debugInfo, isTableauOrNineGrid: false, isLarge: false);
		}
		ThumbnailCacheManager.Current.CreateTexture(base.ThumbnailCreationData);
	}
}
