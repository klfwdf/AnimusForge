namespace TaleWorlds.Core;

public class DefaultItemCategories
{
	private ItemCategory _itemCategoryGrain;

	private ItemCategory _itemCategoryWood;

	private ItemCategory _itemCategoryMeat;

	private ItemCategory _itemCategoryLinen;

	private ItemCategory _itemCategoryHorseEquipment;

	private ItemCategory _itemCategoryHorseEquipment2;

	private ItemCategory _itemCategoryJewelry;

	private ItemCategory _itemCategoryHorseEquipment3;

	private ItemCategory _itemCategoryFur;

	private ItemCategory _itemCategoryHorseEquipment4;

	private ItemCategory _itemCategoryButter;

	private ItemCategory _itemCategoryHorseEquipment5;

	private ItemCategory _itemCategoryUnassigned;

	private ItemCategory _itemCategoryArrows;

	private ItemCategory _itemCategoryUltraArmor;

	private ItemCategory _itemCategoryHeavyArmor;

	private ItemCategory _itemCategoryMediumArmor;

	private ItemCategory _itemCategoryLightArmor;

	private ItemCategory _itemCategoryGarment;

	private ItemCategory _itemCategoryShield5;

	private ItemCategory _itemCategoryShield4;

	private ItemCategory _itemCategoryShield3;

	private ItemCategory _itemCategoryShield2;

	private ItemCategory _itemCategoryShield;

	private ItemCategory _itemCategoryRangedWeapons5;

	private ItemCategory _itemCategoryRangedWeapons4;

	private ItemCategory _itemCategoryRangedWeapons3;

	private ItemCategory _itemCategoryRangedWeapons2;

	private ItemCategory _itemCategoryRangedWeapons1;

	private ItemCategory _itemCategoryMeleeWeapons5;

	private ItemCategory _itemCategoryMeleeWeapons4;

	private ItemCategory _itemCategoryMeleeWeapons3;

	private ItemCategory _itemCategoryMeleeWeapons2;

	private ItemCategory _itemCategoryMeleeWeapons1;

	private ItemCategory _itemCategoryWarHorse;

	private ItemCategory _itemCategoryNobleHorse;

	private ItemCategory _itemCategoryPackAnimal;

	private ItemCategory _itemCategoryHorse;

	private ItemCategory _itemCategoryHog;

	private ItemCategory _itemCategoryCow;

	private ItemCategory _itemCategorySheep;

	private ItemCategory _itemCategoryVelvet;

	private ItemCategory _itemCategoryLeather;

	private ItemCategory _itemCategoryCloth;

	private ItemCategory _itemCategoryPottery;

	private ItemCategory _itemCategoryTools;

	private ItemCategory _itemCategoryOil;

	private ItemCategory _itemCategoryWine;

	private ItemCategory _itemCategoryBeer;

	private ItemCategory _itemCategoryOlives;

	private ItemCategory _itemCategoryDateFruit;

	private ItemCategory _itemCategoryClay;

	private ItemCategory _itemCategoryHides;

	private ItemCategory _itemCategoryGrape;

	private ItemCategory _itemCategoryFlax;

	private ItemCategory _itemCategoryFish;

	private ItemCategory _itemCategoryCotton;

	private ItemCategory _itemCategorySilver;

	private ItemCategory _itemCategorySalt;

	private ItemCategory _itemCategoryIron;

	private ItemCategory _itemCategoryCheese;

	private ItemCategory _itemCategoryWool;

	private ItemCategory _itemCategoryBanners;

	private ItemCategory _itemCategoryFelt;

	private ItemCategory _itemCategoryPlanks;

	private static DefaultItemCategories Instance => Game.Current.DefaultItemCategories;

	public static ItemCategory Grain => Instance._itemCategoryGrain;

	public static ItemCategory Wood => Instance._itemCategoryWood;

	public static ItemCategory Meat => Instance._itemCategoryMeat;

	public static ItemCategory Wool => Instance._itemCategoryWool;

	public static ItemCategory Cheese => Instance._itemCategoryCheese;

	public static ItemCategory Iron => Instance._itemCategoryIron;

	public static ItemCategory Salt => Instance._itemCategorySalt;

	public static ItemCategory Silver => Instance._itemCategorySilver;

	public static ItemCategory Cotton => Instance._itemCategoryCotton;

	public static ItemCategory Fish => Instance._itemCategoryFish;

	public static ItemCategory Flax => Instance._itemCategoryFlax;

	public static ItemCategory Grape => Instance._itemCategoryGrape;

	public static ItemCategory Hides => Instance._itemCategoryHides;

	public static ItemCategory Clay => Instance._itemCategoryClay;

	public static ItemCategory DateFruit => Instance._itemCategoryDateFruit;

	public static ItemCategory Olives => Instance._itemCategoryOlives;

	public static ItemCategory Beer => Instance._itemCategoryBeer;

	public static ItemCategory Wine => Instance._itemCategoryWine;

	public static ItemCategory Oil => Instance._itemCategoryOil;

	public static ItemCategory Tools => Instance._itemCategoryTools;

	public static ItemCategory Pottery => Instance._itemCategoryPottery;

	public static ItemCategory Cloth => Instance._itemCategoryCloth;

	public static ItemCategory Linen => Instance._itemCategoryLinen;

	public static ItemCategory Leather => Instance._itemCategoryLeather;

	public static ItemCategory Velvet => Instance._itemCategoryVelvet;

	public static ItemCategory Sheep => Instance._itemCategorySheep;

	public static ItemCategory Cow => Instance._itemCategoryCow;

	public static ItemCategory Hog => Instance._itemCategoryHog;

	public static ItemCategory Horse => Instance._itemCategoryHorse;

	public static ItemCategory WarHorse => Instance._itemCategoryWarHorse;

	public static ItemCategory NobleHorse => Instance._itemCategoryNobleHorse;

	public static ItemCategory Butter => Instance._itemCategoryButter;

	public static ItemCategory Fur => Instance._itemCategoryFur;

	public static ItemCategory Jewelry => Instance._itemCategoryJewelry;

	public static ItemCategory PackAnimal => Instance._itemCategoryPackAnimal;

	public static ItemCategory MeleeWeapons1 => Instance._itemCategoryMeleeWeapons1;

	public static ItemCategory MeleeWeapons2 => Instance._itemCategoryMeleeWeapons2;

	public static ItemCategory MeleeWeapons3 => Instance._itemCategoryMeleeWeapons3;

	public static ItemCategory MeleeWeapons4 => Instance._itemCategoryMeleeWeapons4;

	public static ItemCategory MeleeWeapons5 => Instance._itemCategoryMeleeWeapons5;

	public static ItemCategory RangedWeapons1 => Instance._itemCategoryRangedWeapons1;

	public static ItemCategory RangedWeapons2 => Instance._itemCategoryRangedWeapons2;

	public static ItemCategory RangedWeapons3 => Instance._itemCategoryRangedWeapons3;

	public static ItemCategory RangedWeapons4 => Instance._itemCategoryRangedWeapons4;

	public static ItemCategory RangedWeapons5 => Instance._itemCategoryRangedWeapons5;

	public static ItemCategory Shield1 => Instance._itemCategoryShield;

	public static ItemCategory Shield2 => Instance._itemCategoryShield2;

	public static ItemCategory Shield3 => Instance._itemCategoryShield3;

	public static ItemCategory Shield4 => Instance._itemCategoryShield4;

	public static ItemCategory Shield5 => Instance._itemCategoryShield5;

	public static ItemCategory Garment => Instance._itemCategoryGarment;

	public static ItemCategory LightArmor => Instance._itemCategoryLightArmor;

	public static ItemCategory MediumArmor => Instance._itemCategoryMediumArmor;

	public static ItemCategory HeavyArmor => Instance._itemCategoryHeavyArmor;

	public static ItemCategory UltraArmor => Instance._itemCategoryUltraArmor;

	public static ItemCategory HorseEquipment => Instance._itemCategoryHorseEquipment;

	public static ItemCategory HorseEquipment2 => Instance._itemCategoryHorseEquipment2;

	public static ItemCategory HorseEquipment3 => Instance._itemCategoryHorseEquipment3;

	public static ItemCategory HorseEquipment4 => Instance._itemCategoryHorseEquipment4;

	public static ItemCategory HorseEquipment5 => Instance._itemCategoryHorseEquipment5;

	public static ItemCategory Arrows => Instance._itemCategoryArrows;

	public static ItemCategory Banners => Instance._itemCategoryBanners;

	public static ItemCategory Unassigned => Instance._itemCategoryUnassigned;

	public static ItemCategory Felt => Instance._itemCategoryFelt;

	public static ItemCategory Planks => Instance._itemCategoryPlanks;

	private ItemCategory Create(string stringId)
	{
		return Game.Current.ObjectManager.RegisterPresumedObject(new ItemCategory(stringId));
	}

	public DefaultItemCategories()
	{
		RegisterAll();
	}

	private void RegisterAll()
	{
		_itemCategoryGrain = Create("grain");
		_itemCategoryFish = Create("fish");
		_itemCategoryMeat = Create("meat");
		_itemCategoryCheese = Create("cheese");
		_itemCategoryGrape = Create("grape");
		_itemCategoryDateFruit = Create("date_fruit");
		_itemCategoryOlives = Create("olives");
		_itemCategoryButter = Create("butter");
		_itemCategoryOil = Create("oil");
		_itemCategoryFlax = Create("flax");
		_itemCategoryLinen = Create("linen");
		_itemCategoryWool = Create("wool");
		_itemCategoryCloth = Create("cloth");
		_itemCategoryCotton = Create("cotton");
		_itemCategoryVelvet = Create("velvet");
		_itemCategoryWood = Create("hardwood");
		_itemCategoryIron = Create("iron");
		_itemCategorySalt = Create("salt");
		_itemCategorySilver = Create("silver");
		_itemCategoryHides = Create("hides");
		_itemCategoryClay = Create("clay");
		_itemCategoryBeer = Create("beer");
		_itemCategoryWine = Create("wine");
		_itemCategoryTools = Create("tools");
		_itemCategoryPottery = Create("pottery");
		_itemCategoryLeather = Create("leather");
		_itemCategoryFur = Create("fur");
		_itemCategoryJewelry = Create("jewelry");
		_itemCategoryArrows = Create("arrows");
		_itemCategorySheep = Create("sheep");
		_itemCategoryCow = Create("cow");
		_itemCategoryHog = Create("hog");
		_itemCategoryPackAnimal = Create("sumpter_horse");
		_itemCategoryHorse = Create("horse");
		_itemCategoryWarHorse = Create("war_horse");
		_itemCategoryNobleHorse = Create("noble_horse");
		_itemCategoryHorseEquipment = Create("horse_equipment");
		_itemCategoryHorseEquipment2 = Create("horse_equipment_2");
		_itemCategoryHorseEquipment3 = Create("horse_equipment_3");
		_itemCategoryHorseEquipment4 = Create("horse_equipment_4");
		_itemCategoryHorseEquipment5 = Create("horse_equipment_5");
		_itemCategoryMeleeWeapons1 = Create("melee_weapons");
		_itemCategoryMeleeWeapons2 = Create("melee_weapons_2");
		_itemCategoryMeleeWeapons3 = Create("melee_weapons_3");
		_itemCategoryMeleeWeapons4 = Create("melee_weapons_4");
		_itemCategoryMeleeWeapons5 = Create("melee_weapons_5");
		_itemCategoryRangedWeapons1 = Create("ranged_weapons");
		_itemCategoryRangedWeapons2 = Create("ranged_weapons_2");
		_itemCategoryRangedWeapons3 = Create("ranged_weapons_3");
		_itemCategoryRangedWeapons4 = Create("ranged_weapons_4");
		_itemCategoryRangedWeapons5 = Create("ranged_weapons_5");
		_itemCategoryShield = Create("shield");
		_itemCategoryShield2 = Create("shield_2");
		_itemCategoryShield3 = Create("shield_3");
		_itemCategoryShield4 = Create("shield_4");
		_itemCategoryShield5 = Create("shield_5");
		_itemCategoryGarment = Create("garment");
		_itemCategoryLightArmor = Create("light_armor");
		_itemCategoryMediumArmor = Create("medium_armor");
		_itemCategoryHeavyArmor = Create("heavy_armor");
		_itemCategoryUltraArmor = Create("ultra_armor");
		_itemCategoryBanners = Create("banner");
		_itemCategoryUnassigned = Create("unassigned");
		_itemCategoryFelt = Create("felt");
		_itemCategoryPlanks = Create("planks");
		InitializeAll();
	}

	private void InitializeAll()
	{
		_itemCategoryGrain.InitializeObject(isTradeGood: true, 140, 0, ItemCategory.Property.BonusToFoodStores, _itemCategoryFish, 0.9f);
		_itemCategoryFish.InitializeObject(isTradeGood: true, 15, 15, ItemCategory.Property.BonusToFoodStores, _itemCategoryGrain, 0.9f);
		_itemCategoryMeat.InitializeObject(isTradeGood: true, 30, 50, ItemCategory.Property.BonusToFoodStores, _itemCategoryFish, 0.1f);
		_itemCategoryCheese.InitializeObject(isTradeGood: true, 10, 20, ItemCategory.Property.BonusToFoodStores, _itemCategoryGrain, 0.01f);
		_itemCategoryButter.InitializeObject(isTradeGood: true, 10, 25, ItemCategory.Property.BonusToFoodStores);
		_itemCategoryGrape.InitializeObject(isTradeGood: true, 5, 20, ItemCategory.Property.BonusToFoodStores, _itemCategoryGrain, 0.1f);
		_itemCategoryOlives.InitializeObject(isTradeGood: true, 5, 20, ItemCategory.Property.BonusToFoodStores, _itemCategoryGrain, 0.01f);
		_itemCategoryDateFruit.InitializeObject(isTradeGood: true, 7, 32, ItemCategory.Property.BonusToFoodStores, _itemCategoryGrape, 0.01f);
		_itemCategoryOil.InitializeObject(isTradeGood: true, 26, 30, ItemCategory.Property.None, _itemCategoryButter, 0.1f);
		_itemCategoryFlax.InitializeObject(isTradeGood: true, 10, 20);
		_itemCategoryLinen.InitializeObject(isTradeGood: true, 28, 30);
		_itemCategoryWool.InitializeObject(isTradeGood: true, 12);
		_itemCategoryCloth.InitializeObject(isTradeGood: false, 12, 6);
		_itemCategoryCotton.InitializeObject(isTradeGood: true, 10, 3);
		_itemCategoryVelvet.InitializeObject(isTradeGood: true, 15, 32, ItemCategory.Property.BonusToLoyalty);
		_itemCategoryWood.InitializeObject(isTradeGood: true, 10, 10);
		_itemCategoryIron.InitializeObject(isTradeGood: true, 10, 20);
		_itemCategorySalt.InitializeObject(isTradeGood: true, 25, 25, ItemCategory.Property.BonusToTax);
		_itemCategorySilver.InitializeObject(isTradeGood: true, 10, 20, ItemCategory.Property.BonusToTax);
		_itemCategoryHides.InitializeObject(isTradeGood: true, 17, 10);
		_itemCategoryClay.InitializeObject(isTradeGood: true, 8, 5);
		_itemCategoryBeer.InitializeObject(isTradeGood: true, 46, 20, ItemCategory.Property.BonusToFoodStores);
		_itemCategoryWine.InitializeObject(isTradeGood: true, 15, 30, ItemCategory.Property.BonusToLoyalty);
		_itemCategoryTools.InitializeObject(isTradeGood: true, 30, 30, ItemCategory.Property.BonusToProduction);
		_itemCategoryPottery.InitializeObject(isTradeGood: true, 22, 20);
		_itemCategoryLeather.InitializeObject(isTradeGood: true, 15, 10);
		_itemCategoryFur.InitializeObject(isTradeGood: true, 10, 38, ItemCategory.Property.BonusToProsperity);
		_itemCategoryJewelry.InitializeObject(isTradeGood: true, 15, 32, ItemCategory.Property.BonusToLoyalty);
		_itemCategorySheep.InitializeObject(isTradeGood: true, 8, 0, ItemCategory.Property.None, null, 0f, isAnimal: true);
		_itemCategoryCow.InitializeObject(isTradeGood: true, 8, 0, ItemCategory.Property.None, null, 0f, isAnimal: true);
		_itemCategoryHog.InitializeObject(isTradeGood: true, 6, 0, ItemCategory.Property.None, null, 0f, isAnimal: true);
		_itemCategoryPackAnimal.InitializeObject(isTradeGood: true, 20, 3, ItemCategory.Property.BonusToProsperity, null, 0f, isAnimal: true);
		_itemCategoryHorse.InitializeObject(isTradeGood: true, 140, 0, ItemCategory.Property.BonusToProsperity, null, 0f, isAnimal: true);
		_itemCategoryWarHorse.InitializeObject(isTradeGood: true, 120, 20, ItemCategory.Property.BonusToGarrison, null, 0f, isAnimal: true);
		_itemCategoryNobleHorse.InitializeObject(isTradeGood: false, 120, 50, ItemCategory.Property.BonusToGarrison, null, 0f, isAnimal: true);
		_itemCategoryArrows.InitializeObject(isTradeGood: false, 30, 30);
		_itemCategoryHorseEquipment.InitializeObject(isTradeGood: false, 9, 5);
		_itemCategoryHorseEquipment2.InitializeObject(isTradeGood: false, 7, 6);
		_itemCategoryHorseEquipment3.InitializeObject(isTradeGood: false, 5, 7);
		_itemCategoryHorseEquipment4.InitializeObject(isTradeGood: false, 5, 8);
		_itemCategoryHorseEquipment5.InitializeObject(isTradeGood: false, 5, 9);
		_itemCategoryMeleeWeapons1.InitializeObject(isTradeGood: false, 9, 7);
		_itemCategoryMeleeWeapons2.InitializeObject(isTradeGood: false, 7, 7);
		_itemCategoryMeleeWeapons3.InitializeObject(isTradeGood: false, 5, 10);
		_itemCategoryMeleeWeapons4.InitializeObject(isTradeGood: false, 4, 10);
		_itemCategoryMeleeWeapons5.InitializeObject(isTradeGood: false, 4, 10);
		_itemCategoryRangedWeapons1.InitializeObject(isTradeGood: false, 9, 7);
		_itemCategoryRangedWeapons2.InitializeObject(isTradeGood: false, 7, 7);
		_itemCategoryRangedWeapons3.InitializeObject(isTradeGood: false, 5, 10);
		_itemCategoryRangedWeapons4.InitializeObject(isTradeGood: false, 4, 10);
		_itemCategoryRangedWeapons5.InitializeObject(isTradeGood: false, 4, 10);
		_itemCategoryShield.InitializeObject(isTradeGood: false, 9, 7);
		_itemCategoryShield2.InitializeObject(isTradeGood: false, 7, 7);
		_itemCategoryShield3.InitializeObject(isTradeGood: false, 5, 10);
		_itemCategoryShield4.InitializeObject(isTradeGood: false, 4, 10);
		_itemCategoryShield5.InitializeObject(isTradeGood: false, 4, 10);
		_itemCategoryGarment.InitializeObject(isTradeGood: false, 9, 15);
		_itemCategoryLightArmor.InitializeObject(isTradeGood: false, 7, 16);
		_itemCategoryMediumArmor.InitializeObject(isTradeGood: false, 5, 17);
		_itemCategoryHeavyArmor.InitializeObject(isTradeGood: false, 4, 17);
		_itemCategoryUltraArmor.InitializeObject(isTradeGood: false, 3, 10);
		_itemCategoryBanners.InitializeObject();
		_itemCategoryUnassigned.InitializeObject(isTradeGood: false, 0, 0, ItemCategory.Property.None, null, 0f, isAnimal: false, isValid: false);
		_itemCategoryFelt.InitializeObject(isTradeGood: true, 34, 23, ItemCategory.Property.BonusToFoodStores);
		_itemCategoryPlanks.InitializeObject(isTradeGood: true, 25, 15);
	}
}
