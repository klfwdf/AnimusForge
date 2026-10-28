namespace TaleWorlds.Core.ImageIdentifiers;

public class ItemImageIdentifier : ImageIdentifier
{
	public ItemImageIdentifier(ItemObject item, string bannerCode = "")
	{
		base.Id = item?.StringId ?? "";
		base.AdditionalArgs = bannerCode;
		base.TextureProviderName = "ItemImageTextureProvider";
	}
}
