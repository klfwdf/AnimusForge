using TaleWorlds.Core;

namespace NavalDLC;

public class NavalItemCategories
{
	private ItemCategory _itemCategoryWalrusTusk;

	private ItemCategory _itemCategoryWhaleOil;

	private static NavalItemCategories Instance => NavalDLCManager.Instance.NavalItemCategories;

	public static ItemCategory WalrusTusk => Instance._itemCategoryWalrusTusk;

	public static ItemCategory WhaleOil => Instance._itemCategoryWhaleOil;

	public NavalItemCategories()
	{
		RegisterAll();
		InitializeAll();
	}

	private static ItemCategory Create(string stringId)
	{
		return Game.Current.ObjectManager.RegisterPresumedObject(new ItemCategory(stringId));
	}

	private void RegisterAll()
	{
		_itemCategoryWalrusTusk = Create("walrus_tusk");
		_itemCategoryWhaleOil = Create("whale_oil");
	}

	private void InitializeAll()
	{
		_itemCategoryWalrusTusk.InitializeObject(isTradeGood: true, 10, 38, ItemCategory.Property.BonusToProsperity);
		_itemCategoryWhaleOil.InitializeObject(isTradeGood: true, 10, 38, ItemCategory.Property.BonusToProsperity);
	}
}
