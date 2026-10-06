using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Settlements;

public class DefaultVillageTypes
{
	private static DefaultVillageTypes Instance => Campaign.Current.DefaultVillageTypes;

	public IList<ItemObject> ConsumableRawItems { get; private set; }

	public static VillageType EuropeHorseRanch => Instance.VillageTypeEuropeHorseRanch;

	public static VillageType BattanianHorseRanch => Instance.VillageTypeBattanianHorseRanch;

	public static VillageType SturgianHorseRanch => Instance.VillageTypeSturgianHorseRanch;

	public static VillageType VlandianHorseRanch => Instance.VillageTypeVlandianHorseRanch;

	public static VillageType SteppeHorseRanch => Instance.VillageTypeSteppeHorseRanch;

	public static VillageType DesertHorseRanch => Instance.VillageTypeDesertHorseRanch;

	public static VillageType WheatFarm => Instance.VillageTypeWheatFarm;

	public static VillageType Lumberjack => Instance.VillageTypeLumberjack;

	public static VillageType ClayMine => Instance.VillageTypeClayMine;

	public static VillageType SaltMine => Instance.VillageTypeSaltMine;

	public static VillageType IronMine => Instance.VillageTypeIronMine;

	public static VillageType Fisherman => Instance.VillageTypeFisherman;

	public static VillageType CattleRange => Instance.VillageTypeCattleRange;

	public static VillageType SheepFarm => Instance.VillageTypeSheepFarm;

	public static VillageType HogFarm => Instance.VillageTypeHogFarm;

	public static VillageType VineYard => Instance.VillageTypeVineYard;

	public static VillageType FlaxPlant => Instance.VillageTypeFlaxPlant;

	public static VillageType DateFarm => Instance.VillageTypeDateFarm;

	public static VillageType OliveTrees => Instance.VillageTypeOliveTrees;

	public static VillageType SilkPlant => Instance.VillageTypeSilkPlant;

	public static VillageType SilverMine => Instance.VillageTypeSilverMine;

	internal VillageType VillageTypeEuropeHorseRanch { get; private set; }

	internal VillageType VillageTypeBattanianHorseRanch { get; private set; }

	internal VillageType VillageTypeSturgianHorseRanch { get; private set; }

	internal VillageType VillageTypeVlandianHorseRanch { get; private set; }

	internal VillageType VillageTypeSteppeHorseRanch { get; private set; }

	internal VillageType VillageTypeDesertHorseRanch { get; private set; }

	internal VillageType VillageTypeWheatFarm { get; private set; }

	internal VillageType VillageTypeLumberjack { get; private set; }

	internal VillageType VillageTypeClayMine { get; private set; }

	internal VillageType VillageTypeSaltMine { get; private set; }

	internal VillageType VillageTypeIronMine { get; private set; }

	internal VillageType VillageTypeFisherman { get; private set; }

	internal VillageType VillageTypeCattleRange { get; private set; }

	internal VillageType VillageTypeSheepFarm { get; private set; }

	internal VillageType VillageTypeHogFarm { get; private set; }

	internal VillageType VillageTypeTrapper { get; private set; }

	internal VillageType VillageTypeVineYard { get; private set; }

	internal VillageType VillageTypeFlaxPlant { get; private set; }

	internal VillageType VillageTypeDateFarm { get; private set; }

	internal VillageType VillageTypeOliveTrees { get; private set; }

	internal VillageType VillageTypeSilkPlant { get; private set; }

	internal VillageType VillageTypeSilverMine { get; private set; }

	public DefaultVillageTypes()
	{
		ConsumableRawItems = new List<ItemObject>();
		RegisterAll();
		AddProductions();
	}

	private void RegisterAll()
	{
		VillageTypeWheatFarm = Create("wheat_farm");
		VillageTypeEuropeHorseRanch = Create("europe_horse_ranch");
		VillageTypeSteppeHorseRanch = Create("steppe_horse_ranch");
		VillageTypeDesertHorseRanch = Create("desert_horse_ranch");
		VillageTypeBattanianHorseRanch = Create("battanian_horse_ranch");
		VillageTypeSturgianHorseRanch = Create("sturgian_horse_ranch");
		VillageTypeVlandianHorseRanch = Create("vlandian_horse_ranch");
		VillageTypeLumberjack = Create("lumberjack");
		VillageTypeClayMine = Create("clay_mine");
		VillageTypeSaltMine = Create("salt_mine");
		VillageTypeIronMine = Create("iron_mine");
		VillageTypeFisherman = Create("fisherman");
		VillageTypeCattleRange = Create("cattle_farm");
		VillageTypeSheepFarm = Create("sheep_farm");
		VillageTypeHogFarm = Create("swine_farm");
		VillageTypeVineYard = Create("vineyard");
		VillageTypeFlaxPlant = Create("flax_plant");
		VillageTypeDateFarm = Create("date_farm");
		VillageTypeOliveTrees = Create("olive_trees");
		VillageTypeSilkPlant = Create("silk_plant");
		VillageTypeSilverMine = Create("silver_mine");
		VillageTypeTrapper = Create("trapper");
		InitializeAll();
	}

	private VillageType Create(string stringId)
	{
		return Game.Current.ObjectManager.RegisterPresumedObject(new VillageType(stringId));
	}

	private void InitializeAll()
	{
		VillageTypeWheatFarm.Initialize(new TextObject("{=BPPG2XF7}Wheat Farm"), "wheat_farm", "wheat_farm_ucon", "wheat_farm_burned", new(ItemObject, float)[1] { (DefaultItems.Grain, 50f) });
		VillageTypeEuropeHorseRanch.Initialize(new TextObject("{=eEh752CZ}Horse Farm"), "europe_horse_ranch", "ranch_ucon", "europe_horse_ranch_burned", new(ItemObject, float)[1] { (DefaultItems.Grain, 3f) });
		VillageTypeSteppeHorseRanch.Initialize(new TextObject("{=eEh752CZ}Horse Farm"), "steppe_horse_ranch", "ranch_ucon", "steppe_horse_ranch_burned", new(ItemObject, float)[1] { (DefaultItems.Grain, 3f) });
		VillageTypeDesertHorseRanch.Initialize(new TextObject("{=eEh752CZ}Horse Farm"), "desert_horse_ranch", "ranch_ucon", "desert_horse_ranch_burned", new(ItemObject, float)[1] { (DefaultItems.Grain, 3f) });
		VillageTypeBattanianHorseRanch.Initialize(new TextObject("{=eEh752CZ}Horse Farm"), "battanian_horse_ranch", "ranch_ucon", "desert_horse_ranch_burned", new(ItemObject, float)[1] { (DefaultItems.Grain, 3f) });
		VillageTypeSturgianHorseRanch.Initialize(new TextObject("{=eEh752CZ}Horse Farm"), "sturgian_horse_ranch", "ranch_ucon", "desert_horse_ranch_burned", new(ItemObject, float)[1] { (DefaultItems.Grain, 3f) });
		VillageTypeVlandianHorseRanch.Initialize(new TextObject("{=eEh752CZ}Horse Farm"), "vlandian_horse_ranch", "ranch_ucon", "desert_horse_ranch_burned", new(ItemObject, float)[1] { (DefaultItems.Grain, 3f) });
		VillageTypeLumberjack.Initialize(new TextObject("{=YYl1W2jU}Forester"), "lumberjack", "lumberjack_ucon", "lumberjack_burned", new(ItemObject, float)[1] { (DefaultItems.Grain, 3f) });
		VillageTypeClayMine.Initialize(new TextObject("{=myuzMhOn}Clay Pits"), "clay_mine", "clay_mine_ucon", "clay_mine_burned", new(ItemObject, float)[1] { (DefaultItems.Grain, 3f) });
		VillageTypeSaltMine.Initialize(new TextObject("{=3aOIY6wl}Salt Mine"), "salt_mine", "salt_mine_ucon", "salt_mine_burned", new(ItemObject, float)[1] { (DefaultItems.Grain, 3f) });
		VillageTypeIronMine.Initialize(new TextObject("{=rHcVKSbA}Iron Mine"), "iron_mine", "iron_mine_ucon", "iron_mine_burned", new(ItemObject, float)[1] { (DefaultItems.Grain, 3f) });
		VillageTypeFisherman.Initialize(new TextObject("{=XpREJNHD}Fishers"), "fisherman", "fisherman_ucon", "fisherman_burned", new(ItemObject, float)[1] { (DefaultItems.Grain, 3f) });
		VillageTypeCattleRange.Initialize(new TextObject("{=bW3csuSZ}Cattle Farms"), "cattle_farm", "ranch_ucon", "cattle_farm_burned", new(ItemObject, float)[1] { (DefaultItems.Grain, 3f) });
		VillageTypeSheepFarm.Initialize(new TextObject("{=QbKbGu2h}Sheep Farms"), "sheep_farm", "ranch_ucon", "sheep_farm_burned", new(ItemObject, float)[1] { (DefaultItems.Grain, 3f) });
		VillageTypeHogFarm.Initialize(new TextObject("{=vqSHB7mJ}Swine Farm"), "swine_farm", "swine_farm_ucon", "swine_farm_burned", new(ItemObject, float)[1] { (DefaultItems.Grain, 3f) });
		VillageTypeVineYard.Initialize(new TextObject("{=ZtxWTS9V}Vineyard"), "vineyard", "vineyard_ucon", "vineyard_burned", new(ItemObject, float)[1] { (DefaultItems.Grain, 3f) });
		VillageTypeFlaxPlant.Initialize(new TextObject("{=Z8ntYx0Y}Flax Field"), "flax_plant", "flax_plant_ucon", "flax_plant_burned", new(ItemObject, float)[1] { (DefaultItems.Grain, 3f) });
		VillageTypeDateFarm.Initialize(new TextObject("{=2NR2E663}Palm Orchard"), "date_farm", "date_farm_ucon", "date_farm_burned", new(ItemObject, float)[1] { (DefaultItems.Grain, 3f) });
		VillageTypeOliveTrees.Initialize(new TextObject("{=ewrkbwI9}Olive Trees"), "date_farm", "date_farm_ucon", "date_farm_burned", new(ItemObject, float)[1] { (DefaultItems.Grain, 3f) });
		VillageTypeSilkPlant.Initialize(new TextObject("{=wTyq7LaM}Silkworm Farm"), "silk_plant", "silk_plant_ucon", "silk_plant_burned", new(ItemObject, float)[1] { (DefaultItems.Grain, 3f) });
		VillageTypeSilverMine.Initialize(new TextObject("{=aJLQz9iZ}Silver Mine"), "silver_mine", "silver_mine_ucon", "silver_mine_burned", new(ItemObject, float)[1] { (DefaultItems.Grain, 3f) });
		VillageTypeTrapper.Initialize(new TextObject("{=RREyouKr}Trapper"), "trapper", "trapper_ucon", "trapper_burned", new(ItemObject, float)[1] { (DefaultItems.Grain, 3f) });
	}

	private void AddProductions()
	{
		AddProductions(VillageTypeWheatFarm, new(string, float)[3]
		{
			("cow", 0.2f),
			("sheep", 0.4f),
			("hog", 0.8f)
		});
		AddProductions(VillageTypeEuropeHorseRanch, new(string, float)[9]
		{
			("empire_horse", 2.1f),
			("t2_empire_horse", 0.5f),
			("t3_empire_horse", 0.07f),
			("sumpter_horse", 0.5f),
			("mule", 0.5f),
			("saddle_horse", 0.5f),
			("old_horse", 0.5f),
			("hunter", 0.2f),
			("charger", 0.2f)
		});
		AddProductions(VillageTypeSturgianHorseRanch, new(string, float)[9]
		{
			("sturgia_horse", 2.5f),
			("t2_sturgia_horse", 0.7f),
			("t3_sturgia_horse", 0.1f),
			("sumpter_horse", 0.5f),
			("mule", 0.5f),
			("saddle_horse", 0.5f),
			("old_horse", 0.5f),
			("hunter", 0.2f),
			("charger", 0.2f)
		});
		AddProductions(VillageTypeVlandianHorseRanch, new(string, float)[9]
		{
			("vlandia_horse", 2.1f),
			("t2_vlandia_horse", 0.4f),
			("t3_vlandia_horse", 0.08f),
			("sumpter_horse", 0.5f),
			("mule", 0.5f),
			("saddle_horse", 0.5f),
			("old_horse", 0.5f),
			("hunter", 0.2f),
			("charger", 0.2f)
		});
		AddProductions(VillageTypeBattanianHorseRanch, new(string, float)[9]
		{
			("battania_horse", 2.3f),
			("t2_battania_horse", 0.7f),
			("t3_battania_horse", 0.09f),
			("sumpter_horse", 0.5f),
			("mule", 0.5f),
			("saddle_horse", 0.5f),
			("old_horse", 0.5f),
			("hunter", 0.2f),
			("charger", 0.2f)
		});
		AddProductions(VillageTypeSteppeHorseRanch, new(string, float)[5]
		{
			("khuzait_horse", 1.8f),
			("t2_khuzait_horse", 0.4f),
			("t3_khuzait_horse", 0.05f),
			("sumpter_horse", 0.5f),
			("mule", 0.5f)
		});
		AddProductions(VillageTypeDesertHorseRanch, new(string, float)[8]
		{
			("aserai_horse", 1.7f),
			("t2_aserai_horse", 0.3f),
			("t3_aserai_horse", 0.05f),
			("camel", 0.3f),
			("war_camel", 0.08f),
			("pack_camel", 0.3f),
			("sumpter_horse", 0.4f),
			("mule", 0.5f)
		});
		AddProductions(VillageTypeCattleRange, new(string, float)[3]
		{
			("cow", 2f),
			("butter", 4f),
			("cheese", 4f)
		});
		AddProductions(VillageTypeSheepFarm, new(string, float)[4]
		{
			("sheep", 4f),
			("wool", 10f),
			("butter", 2f),
			("cheese", 2f)
		});
		AddProductions(VillageTypeHogFarm, new(string, float)[3]
		{
			("hog", 8f),
			("butter", 2f),
			("cheese", 2f)
		});
		AddProductions(VillageTypeLumberjack, new(string, float)[1] { ("hardwood", 18f) });
		AddProductions(VillageTypeClayMine, new(string, float)[1] { ("clay", 10f) });
		AddProductions(VillageTypeSaltMine, new(string, float)[1] { ("salt", 15f) });
		AddProductions(VillageTypeIronMine, new(string, float)[1] { ("iron", 10f) });
		AddProductions(VillageTypeFisherman, new(string, float)[1] { ("fish", 28f) });
		AddProductions(VillageTypeVineYard, new(string, float)[1] { ("grape", 11f) });
		AddProductions(VillageTypeFlaxPlant, new(string, float)[1] { ("flax", 18f) });
		AddProductions(VillageTypeDateFarm, new(string, float)[1] { ("date_fruit", 8f) });
		AddProductions(VillageTypeOliveTrees, new(string, float)[1] { ("olives", 12f) });
		AddProductions(VillageTypeSilkPlant, new(string, float)[1] { ("cotton", 8f) });
		AddProductions(VillageTypeSilverMine, new(string, float)[1] { ("silver", 3f) });
		AddProductions(VillageTypeTrapper, new(string, float)[1] { ("fur", 1.4f) });
		ConsumableRawItems.Add(Game.Current.ObjectManager.GetObject<ItemObject>("grain"));
		ConsumableRawItems.Add(Game.Current.ObjectManager.GetObject<ItemObject>("cheese"));
		ConsumableRawItems.Add(Game.Current.ObjectManager.GetObject<ItemObject>("butter"));
	}

	private void AddProductions(VillageType villageType, (string, float)[] productions)
	{
		villageType.AddProductions(productions.Select(((string, float) p) => (Game.Current.ObjectManager.GetObject<ItemObject>(p.Item1), p.Item2)));
	}
}
