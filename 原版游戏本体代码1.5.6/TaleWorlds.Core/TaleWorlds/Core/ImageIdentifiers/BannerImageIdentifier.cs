namespace TaleWorlds.Core.ImageIdentifiers;

public class BannerImageIdentifier : ImageIdentifier
{
	public BannerImageIdentifier(Banner banner, bool nineGrid = false)
	{
		base.Id = ((banner != null) ? banner.BannerCode : "");
		base.AdditionalArgs = (nineGrid ? "ninegrid" : "");
		base.TextureProviderName = "BannerImageTextureProvider";
	}
}
