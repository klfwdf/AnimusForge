namespace TaleWorlds.Core;

public abstract class ItemCategorySelector : MBGameModel<ItemCategorySelector>
{
	public abstract ItemCategory GetItemCategoryForItem(ItemObject itemObject);
}
