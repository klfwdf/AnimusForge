using System;
using TaleWorlds.Core.ImageIdentifiers;

namespace TaleWorlds.Core.ViewModelCollection.ImageIdentifiers
{
	// Token: 0x0200001D RID: 29
	public class BannerImageIdentifierVM : ImageIdentifierVM
	{
		// Token: 0x06000199 RID: 409 RVA: 0x000059A9 File Offset: 0x00003BA9
		public BannerImageIdentifierVM(Banner banner, bool nineGrid = false)
		{
			base.ImageIdentifier = new BannerImageIdentifier(banner, nineGrid);
		}
	}
}
