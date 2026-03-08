using System;

namespace TaleWorlds.Core.ImageIdentifiers
{
	// Token: 0x020000E3 RID: 227
	public class BannerImageIdentifier : ImageIdentifier
	{
		// Token: 0x06000B7D RID: 2941 RVA: 0x00024F9E File Offset: 0x0002319E
		public BannerImageIdentifier(Banner banner, bool nineGrid = false)
		{
			base.Id = ((banner != null) ? banner.BannerCode : "");
			base.AdditionalArgs = (nineGrid ? "ninegrid" : "");
			base.TextureProviderName = "BannerImageTextureProvider";
		}
	}
}
