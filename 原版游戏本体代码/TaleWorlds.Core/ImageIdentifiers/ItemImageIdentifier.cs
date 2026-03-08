using System;

namespace TaleWorlds.Core.ImageIdentifiers
{
	// Token: 0x020000E8 RID: 232
	public class ItemImageIdentifier : ImageIdentifier
	{
		// Token: 0x06000B89 RID: 2953 RVA: 0x000250F6 File Offset: 0x000232F6
		public ItemImageIdentifier(ItemObject item, string bannerCode = "")
		{
			base.Id = ((item != null) ? item.StringId : null) ?? "";
			base.AdditionalArgs = bannerCode;
			base.TextureProviderName = "ItemImageTextureProvider";
		}
	}
}
