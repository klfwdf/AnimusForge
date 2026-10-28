using TaleWorlds.Core.ImageIdentifiers;

namespace TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;

public class ItemImageIdentifierVM : ImageIdentifierVM
{
	private readonly ItemObject _itemObject;

	private readonly string _bannerCode;

	public ItemImageIdentifierVM(ItemObject itemObject, string bannerCode = "")
	{
		_itemObject = itemObject;
		_bannerCode = bannerCode;
		base.ImageIdentifier = new ItemImageIdentifier(_itemObject, _bannerCode);
	}

	public ItemImageIdentifierVM Clone()
	{
		return new ItemImageIdentifierVM(_itemObject, _bannerCode);
	}
}
