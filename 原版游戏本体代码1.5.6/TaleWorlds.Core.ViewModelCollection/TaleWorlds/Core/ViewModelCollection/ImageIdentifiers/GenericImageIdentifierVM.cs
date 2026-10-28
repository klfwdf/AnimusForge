using TaleWorlds.Core.ImageIdentifiers;

namespace TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;

public class GenericImageIdentifierVM : ImageIdentifierVM
{
	public GenericImageIdentifierVM(ImageIdentifier imageIdentifier)
	{
		if (imageIdentifier == null)
		{
			base.ImageIdentifier = new EmptyImageIdentifier();
		}
		else
		{
			base.ImageIdentifier = imageIdentifier;
		}
	}
}
