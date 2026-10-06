using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem;

public class DefaultItems
{
	private const float TradeGoodWeight = 10f;

	private const float HalfWeight = 5f;

	private const float IngotWeight = 0.5f;

	private const float TrashWeight = 1f;

	private const int IngotValue = 20;

	private const int TrashValue = 1;

	private ItemObject _itemGrain;

	private ItemObject _itemPlanks;

	private ItemObject _itemFelt;

	private ItemObject _itemMeat;

	private ItemObject _itemHides;

	private ItemObject _itemTools;

	private ItemObject _itemIronOre;

	private ItemObject _itemHardwood;

	private ItemObject _itemCharcoal;

	private ItemObject _itemIronIngot1;

	private ItemObject _itemIronIngot2;

	private ItemObject _itemIronIngot3;

	private ItemObject _itemIronIngot4;

	private ItemObject _itemIronIngot5;

	private ItemObject _itemIronIngot6;

	private ItemObject _itemTrash;

	private static DefaultItems Instance => Campaign.Current.DefaultItems;

	public static ItemObject Grain => Instance._itemGrain;

	public static ItemObject Planks => Instance._itemPlanks;

	public static ItemObject Felt => Instance._itemFelt;

	public static ItemObject Meat => Instance._itemMeat;

	public static ItemObject Hides => Instance._itemHides;

	public static ItemObject Tools => Instance._itemTools;

	public static ItemObject IronOre => Instance._itemIronOre;

	public static ItemObject HardWood => Instance._itemHardwood;

	public static ItemObject Charcoal => Instance._itemCharcoal;

	public static ItemObject IronIngot1 => Instance._itemIronIngot1;

	public static ItemObject IronIngot2 => Instance._itemIronIngot2;

	public static ItemObject IronIngot3 => Instance._itemIronIngot3;

	public static ItemObject IronIngot4 => Instance._itemIronIngot4;

	public static ItemObject IronIngot5 => Instance._itemIronIngot5;

	public static ItemObject IronIngot6 => Instance._itemIronIngot6;

	public static ItemObject Trash => Instance._itemTrash;

	public DefaultItems()
	{
		RegisterAll();
	}

	private void RegisterAll()
	{
		_itemGrain = Create("grain");
		_itemFelt = Create("felt");
		_itemPlanks = Create("planks");
		_itemMeat = Create("meat");
		_itemHides = Create("hides");
		_itemTools = Create("tools");
		_itemIronOre = Create("iron");
		_itemHardwood = Create("hardwood");
		_itemCharcoal = Create("charcoal");
		_itemIronIngot1 = Create("ironIngot1");
		_itemIronIngot2 = Create("ironIngot2");
		_itemIronIngot3 = Create("ironIngot3");
		_itemIronIngot4 = Create("ironIngot4");
		_itemIronIngot5 = Create("ironIngot5");
		_itemIronIngot6 = Create("ironIngot6");
		_itemTrash = Create("trash");
		InitializeAll();
	}

	private ItemObject Create(string stringId)
	{
		return Game.Current.ObjectManager.RegisterPresumedObject(new ItemObject(stringId));
	}

	private void InitializeAll()
	{
		ItemObject.InitializeTradeGood(_itemGrain, new TextObject("{=Itv3fgJm}Grain{@Plural}loads of grain{\\@}"), "merchandise_grain", DefaultItemCategories.Grain, 10, 10f, ItemObject.ItemTypeEnum.Goods, isFood: true);
		ItemObject.InitializeTradeGood(_itemMeat, new TextObject("{=LmwhFv5p}Meat{@Plural}loads of meat{\\@}"), "merchandise_meat", DefaultItemCategories.Meat, 30, 10f, ItemObject.ItemTypeEnum.Goods, isFood: true);
		ItemObject.InitializeTradeGood(_itemPlanks, new TextObject("{=5ac8Boz1}Planks{@Plural}loads of planks{\\@}"), "bd_planks_a", DefaultItemCategories.Planks, 180, 10f, ItemObject.ItemTypeEnum.Goods);
		ItemObject.InitializeTradeGood(_itemFelt, new TextObject("{=hNwjpCVP}Felt{@Plural}rolls of felt{\\@}"), "merchandise_hides_b", DefaultItemCategories.Felt, 230, 10f, ItemObject.ItemTypeEnum.Goods);
		ItemObject.InitializeTradeGood(_itemHides, new TextObject("{=4kvKQuXM}Hides{@Plural}loads of hide{\\@}"), "merchandise_hides_b", DefaultItemCategories.Hides, 50, 10f, ItemObject.ItemTypeEnum.Goods);
		ItemObject.InitializeTradeGood(_itemTools, new TextObject("{=n3cjEB0X}Tools{@Plural}loads of tools{\\@}"), "bd_pickaxe_b", DefaultItemCategories.Tools, 250, 10f, ItemObject.ItemTypeEnum.Goods);
		ItemObject.InitializeTradeGood(_itemIronOre, new TextObject("{=Kw6BkhIf}Iron Ore{@Plural}loads of iron ore{\\@}"), "iron_ore", DefaultItemCategories.Iron, 50, 10f, ItemObject.ItemTypeEnum.Goods);
		ItemObject.InitializeTradeGood(_itemHardwood, new TextObject("{=ExjMoUiT}Hardwood{@Plural}hardwood logs{\\@}"), "hardwood", DefaultItemCategories.Wood, 25, 10f, ItemObject.ItemTypeEnum.Goods);
		ItemObject.InitializeTradeGood(_itemCharcoal, new TextObject("{=iQadPYNe}Charcoal{@Plural}loads of charcoal{\\@}"), "charcoal", DefaultItemCategories.Wood, 50, 5f, ItemObject.ItemTypeEnum.Goods);
		ItemObject.InitializeTradeGood(_itemIronIngot1, new TextObject("{=gOpodlt1}Crude Iron{@Plural}loads of crude iron{\\@}"), "crude_iron", DefaultItemCategories.Iron, 20, 0.5f, ItemObject.ItemTypeEnum.Goods);
		ItemObject.InitializeTradeGood(_itemIronIngot2, new TextObject("{=7HvtT8bm}Wrought Iron{@Plural}loads of wrought iron{\\@}"), "wrought_iron", DefaultItemCategories.Iron, 30, 0.5f, ItemObject.ItemTypeEnum.Goods);
		ItemObject.InitializeTradeGood(_itemIronIngot3, new TextObject("{=XHmmbnbB}Iron{@Plural}loads of iron{\\@}"), "iron_a", DefaultItemCategories.Iron, 60, 0.5f, ItemObject.ItemTypeEnum.Goods);
		ItemObject.InitializeTradeGood(_itemIronIngot4, new TextObject("{=UfuLKuaI}Steel{@Plural}loads of steel{\\@}"), "steel", DefaultItemCategories.Iron, 100, 0.5f, ItemObject.ItemTypeEnum.Goods);
		ItemObject.InitializeTradeGood(_itemIronIngot5, new TextObject("{=azjMBa86}Fine Steel{@Plural}loads of fine steel{\\@}"), "fine_steel", DefaultItemCategories.Iron, 160, 0.5f, ItemObject.ItemTypeEnum.Goods);
		ItemObject.InitializeTradeGood(_itemIronIngot6, new TextObject("{=vLVAfcta}Thamaskene Steel{@Plural}loads of thamaskene steel{\\@}"), "thamaskene_steel", DefaultItemCategories.Iron, 260, 0.5f, ItemObject.ItemTypeEnum.Goods);
		ItemObject.InitializeTradeGood(_itemTrash, new TextObject("{=ZvZN6UkU}Trash Item"), "iron_ore", DefaultItemCategories.Unassigned, 1, 1f, ItemObject.ItemTypeEnum.Goods);
	}
}
