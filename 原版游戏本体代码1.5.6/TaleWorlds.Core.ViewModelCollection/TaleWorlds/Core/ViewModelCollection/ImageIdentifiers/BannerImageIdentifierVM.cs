using TaleWorlds.Core.ImageIdentifiers;

namespace TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;

public class BannerImageIdentifierVM : ImageIdentifierVM
{
	public BannerImageIdentifierVM(Banner banner, bool nineGrid = false)
	{
		base.ImageIdentifier = new BannerImageIdentifier(banner, nineGrid);
	}
}
