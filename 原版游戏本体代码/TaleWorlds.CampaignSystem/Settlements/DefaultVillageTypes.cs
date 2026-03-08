using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Settlements
{
	// Token: 0x020003BB RID: 955
	public class DefaultVillageTypes
	{
		// Token: 0x17000D75 RID: 3445
		// (get) Token: 0x06003841 RID: 14401 RVA: 0x000E5F4A File Offset: 0x000E414A
		private static DefaultVillageTypes Instance
		{
			get
			{
				return Campaign.Current.DefaultVillageTypes;
			}
		}

		// Token: 0x17000D76 RID: 3446
		// (get) Token: 0x06003842 RID: 14402 RVA: 0x000E5F56 File Offset: 0x000E4156
		// (set) Token: 0x06003843 RID: 14403 RVA: 0x000E5F5E File Offset: 0x000E415E
		public IList<ItemObject> ConsumableRawItems { get; private set; }

		// Token: 0x17000D77 RID: 3447
		// (get) Token: 0x06003844 RID: 14404 RVA: 0x000E5F67 File Offset: 0x000E4167
		public static VillageType EuropeHorseRanch
		{
			get
			{
				return DefaultVillageTypes.Instance.VillageTypeEuropeHorseRanch;
			}
		}

		// Token: 0x17000D78 RID: 3448
		// (get) Token: 0x06003845 RID: 14405 RVA: 0x000E5F73 File Offset: 0x000E4173
		public static VillageType BattanianHorseRanch
		{
			get
			{
				return DefaultVillageTypes.Instance.VillageTypeBattanianHorseRanch;
			}
		}

		// Token: 0x17000D79 RID: 3449
		// (get) Token: 0x06003846 RID: 14406 RVA: 0x000E5F7F File Offset: 0x000E417F
		public static VillageType SturgianHorseRanch
		{
			get
			{
				return DefaultVillageTypes.Instance.VillageTypeSturgianHorseRanch;
			}
		}

		// Token: 0x17000D7A RID: 3450
		// (get) Token: 0x06003847 RID: 14407 RVA: 0x000E5F8B File Offset: 0x000E418B
		public static VillageType VlandianHorseRanch
		{
			get
			{
				return DefaultVillageTypes.Instance.VillageTypeVlandianHorseRanch;
			}
		}

		// Token: 0x17000D7B RID: 3451
		// (get) Token: 0x06003848 RID: 14408 RVA: 0x000E5F97 File Offset: 0x000E4197
		public static VillageType SteppeHorseRanch
		{
			get
			{
				return DefaultVillageTypes.Instance.VillageTypeSteppeHorseRanch;
			}
		}

		// Token: 0x17000D7C RID: 3452
		// (get) Token: 0x06003849 RID: 14409 RVA: 0x000E5FA3 File Offset: 0x000E41A3
		public static VillageType DesertHorseRanch
		{
			get
			{
				return DefaultVillageTypes.Instance.VillageTypeDesertHorseRanch;
			}
		}

		// Token: 0x17000D7D RID: 3453
		// (get) Token: 0x0600384A RID: 14410 RVA: 0x000E5FAF File Offset: 0x000E41AF
		public static VillageType WheatFarm
		{
			get
			{
				return DefaultVillageTypes.Instance.VillageTypeWheatFarm;
			}
		}

		// Token: 0x17000D7E RID: 3454
		// (get) Token: 0x0600384B RID: 14411 RVA: 0x000E5FBB File Offset: 0x000E41BB
		public static VillageType Lumberjack
		{
			get
			{
				return DefaultVillageTypes.Instance.VillageTypeLumberjack;
			}
		}

		// Token: 0x17000D7F RID: 3455
		// (get) Token: 0x0600384C RID: 14412 RVA: 0x000E5FC7 File Offset: 0x000E41C7
		public static VillageType ClayMine
		{
			get
			{
				return DefaultVillageTypes.Instance.VillageTypeClayMine;
			}
		}

		// Token: 0x17000D80 RID: 3456
		// (get) Token: 0x0600384D RID: 14413 RVA: 0x000E5FD3 File Offset: 0x000E41D3
		public static VillageType SaltMine
		{
			get
			{
				return DefaultVillageTypes.Instance.VillageTypeSaltMine;
			}
		}

		// Token: 0x17000D81 RID: 3457
		// (get) Token: 0x0600384E RID: 14414 RVA: 0x000E5FDF File Offset: 0x000E41DF
		public static VillageType IronMine
		{
			get
			{
				return DefaultVillageTypes.Instance.VillageTypeIronMine;
			}
		}

		// Token: 0x17000D82 RID: 3458
		// (get) Token: 0x0600384F RID: 14415 RVA: 0x000E5FEB File Offset: 0x000E41EB
		public static VillageType Fisherman
		{
			get
			{
				return DefaultVillageTypes.Instance.VillageTypeFisherman;
			}
		}

		// Token: 0x17000D83 RID: 3459
		// (get) Token: 0x06003850 RID: 14416 RVA: 0x000E5FF7 File Offset: 0x000E41F7
		public static VillageType CattleRange
		{
			get
			{
				return DefaultVillageTypes.Instance.VillageTypeCattleRange;
			}
		}

		// Token: 0x17000D84 RID: 3460
		// (get) Token: 0x06003851 RID: 14417 RVA: 0x000E6003 File Offset: 0x000E4203
		public static VillageType SheepFarm
		{
			get
			{
				return DefaultVillageTypes.Instance.VillageTypeSheepFarm;
			}
		}

		// Token: 0x17000D85 RID: 3461
		// (get) Token: 0x06003852 RID: 14418 RVA: 0x000E600F File Offset: 0x000E420F
		public static VillageType HogFarm
		{
			get
			{
				return DefaultVillageTypes.Instance.VillageTypeHogFarm;
			}
		}

		// Token: 0x17000D86 RID: 3462
		// (get) Token: 0x06003853 RID: 14419 RVA: 0x000E601B File Offset: 0x000E421B
		public static VillageType VineYard
		{
			get
			{
				return DefaultVillageTypes.Instance.VillageTypeVineYard;
			}
		}

		// Token: 0x17000D87 RID: 3463
		// (get) Token: 0x06003854 RID: 14420 RVA: 0x000E6027 File Offset: 0x000E4227
		public static VillageType FlaxPlant
		{
			get
			{
				return DefaultVillageTypes.Instance.VillageTypeFlaxPlant;
			}
		}

		// Token: 0x17000D88 RID: 3464
		// (get) Token: 0x06003855 RID: 14421 RVA: 0x000E6033 File Offset: 0x000E4233
		public static VillageType DateFarm
		{
			get
			{
				return DefaultVillageTypes.Instance.VillageTypeDateFarm;
			}
		}

		// Token: 0x17000D89 RID: 3465
		// (get) Token: 0x06003856 RID: 14422 RVA: 0x000E603F File Offset: 0x000E423F
		public static VillageType OliveTrees
		{
			get
			{
				return DefaultVillageTypes.Instance.VillageTypeOliveTrees;
			}
		}

		// Token: 0x17000D8A RID: 3466
		// (get) Token: 0x06003857 RID: 14423 RVA: 0x000E604B File Offset: 0x000E424B
		public static VillageType SilkPlant
		{
			get
			{
				return DefaultVillageTypes.Instance.VillageTypeSilkPlant;
			}
		}

		// Token: 0x17000D8B RID: 3467
		// (get) Token: 0x06003858 RID: 14424 RVA: 0x000E6057 File Offset: 0x000E4257
		public static VillageType SilverMine
		{
			get
			{
				return DefaultVillageTypes.Instance.VillageTypeSilverMine;
			}
		}

		// Token: 0x17000D8C RID: 3468
		// (get) Token: 0x06003859 RID: 14425 RVA: 0x000E6063 File Offset: 0x000E4263
		// (set) Token: 0x0600385A RID: 14426 RVA: 0x000E606B File Offset: 0x000E426B
		internal VillageType VillageTypeEuropeHorseRanch { get; private set; }

		// Token: 0x17000D8D RID: 3469
		// (get) Token: 0x0600385B RID: 14427 RVA: 0x000E6074 File Offset: 0x000E4274
		// (set) Token: 0x0600385C RID: 14428 RVA: 0x000E607C File Offset: 0x000E427C
		internal VillageType VillageTypeBattanianHorseRanch { get; private set; }

		// Token: 0x17000D8E RID: 3470
		// (get) Token: 0x0600385D RID: 14429 RVA: 0x000E6085 File Offset: 0x000E4285
		// (set) Token: 0x0600385E RID: 14430 RVA: 0x000E608D File Offset: 0x000E428D
		internal VillageType VillageTypeSturgianHorseRanch { get; private set; }

		// Token: 0x17000D8F RID: 3471
		// (get) Token: 0x0600385F RID: 14431 RVA: 0x000E6096 File Offset: 0x000E4296
		// (set) Token: 0x06003860 RID: 14432 RVA: 0x000E609E File Offset: 0x000E429E
		internal VillageType VillageTypeVlandianHorseRanch { get; private set; }

		// Token: 0x17000D90 RID: 3472
		// (get) Token: 0x06003861 RID: 14433 RVA: 0x000E60A7 File Offset: 0x000E42A7
		// (set) Token: 0x06003862 RID: 14434 RVA: 0x000E60AF File Offset: 0x000E42AF
		internal VillageType VillageTypeSteppeHorseRanch { get; private set; }

		// Token: 0x17000D91 RID: 3473
		// (get) Token: 0x06003863 RID: 14435 RVA: 0x000E60B8 File Offset: 0x000E42B8
		// (set) Token: 0x06003864 RID: 14436 RVA: 0x000E60C0 File Offset: 0x000E42C0
		internal VillageType VillageTypeDesertHorseRanch { get; private set; }

		// Token: 0x17000D92 RID: 3474
		// (get) Token: 0x06003865 RID: 14437 RVA: 0x000E60C9 File Offset: 0x000E42C9
		// (set) Token: 0x06003866 RID: 14438 RVA: 0x000E60D1 File Offset: 0x000E42D1
		internal VillageType VillageTypeWheatFarm { get; private set; }

		// Token: 0x17000D93 RID: 3475
		// (get) Token: 0x06003867 RID: 14439 RVA: 0x000E60DA File Offset: 0x000E42DA
		// (set) Token: 0x06003868 RID: 14440 RVA: 0x000E60E2 File Offset: 0x000E42E2
		internal VillageType VillageTypeLumberjack { get; private set; }

		// Token: 0x17000D94 RID: 3476
		// (get) Token: 0x06003869 RID: 14441 RVA: 0x000E60EB File Offset: 0x000E42EB
		// (set) Token: 0x0600386A RID: 14442 RVA: 0x000E60F3 File Offset: 0x000E42F3
		internal VillageType VillageTypeClayMine { get; private set; }

		// Token: 0x17000D95 RID: 3477
		// (get) Token: 0x0600386B RID: 14443 RVA: 0x000E60FC File Offset: 0x000E42FC
		// (set) Token: 0x0600386C RID: 14444 RVA: 0x000E6104 File Offset: 0x000E4304
		internal VillageType VillageTypeSaltMine { get; private set; }

		// Token: 0x17000D96 RID: 3478
		// (get) Token: 0x0600386D RID: 14445 RVA: 0x000E610D File Offset: 0x000E430D
		// (set) Token: 0x0600386E RID: 14446 RVA: 0x000E6115 File Offset: 0x000E4315
		internal VillageType VillageTypeIronMine { get; private set; }

		// Token: 0x17000D97 RID: 3479
		// (get) Token: 0x0600386F RID: 14447 RVA: 0x000E611E File Offset: 0x000E431E
		// (set) Token: 0x06003870 RID: 14448 RVA: 0x000E6126 File Offset: 0x000E4326
		internal VillageType VillageTypeFisherman { get; private set; }

		// Token: 0x17000D98 RID: 3480
		// (get) Token: 0x06003871 RID: 14449 RVA: 0x000E612F File Offset: 0x000E432F
		// (set) Token: 0x06003872 RID: 14450 RVA: 0x000E6137 File Offset: 0x000E4337
		internal VillageType VillageTypeCattleRange { get; private set; }

		// Token: 0x17000D99 RID: 3481
		// (get) Token: 0x06003873 RID: 14451 RVA: 0x000E6140 File Offset: 0x000E4340
		// (set) Token: 0x06003874 RID: 14452 RVA: 0x000E6148 File Offset: 0x000E4348
		internal VillageType VillageTypeSheepFarm { get; private set; }

		// Token: 0x17000D9A RID: 3482
		// (get) Token: 0x06003875 RID: 14453 RVA: 0x000E6151 File Offset: 0x000E4351
		// (set) Token: 0x06003876 RID: 14454 RVA: 0x000E6159 File Offset: 0x000E4359
		internal VillageType VillageTypeHogFarm { get; private set; }

		// Token: 0x17000D9B RID: 3483
		// (get) Token: 0x06003877 RID: 14455 RVA: 0x000E6162 File Offset: 0x000E4362
		// (set) Token: 0x06003878 RID: 14456 RVA: 0x000E616A File Offset: 0x000E436A
		internal VillageType VillageTypeTrapper { get; private set; }

		// Token: 0x17000D9C RID: 3484
		// (get) Token: 0x06003879 RID: 14457 RVA: 0x000E6173 File Offset: 0x000E4373
		// (set) Token: 0x0600387A RID: 14458 RVA: 0x000E617B File Offset: 0x000E437B
		internal VillageType VillageTypeVineYard { get; private set; }

		// Token: 0x17000D9D RID: 3485
		// (get) Token: 0x0600387B RID: 14459 RVA: 0x000E6184 File Offset: 0x000E4384
		// (set) Token: 0x0600387C RID: 14460 RVA: 0x000E618C File Offset: 0x000E438C
		internal VillageType VillageTypeFlaxPlant { get; private set; }

		// Token: 0x17000D9E RID: 3486
		// (get) Token: 0x0600387D RID: 14461 RVA: 0x000E6195 File Offset: 0x000E4395
		// (set) Token: 0x0600387E RID: 14462 RVA: 0x000E619D File Offset: 0x000E439D
		internal VillageType VillageTypeDateFarm { get; private set; }

		// Token: 0x17000D9F RID: 3487
		// (get) Token: 0x0600387F RID: 14463 RVA: 0x000E61A6 File Offset: 0x000E43A6
		// (set) Token: 0x06003880 RID: 14464 RVA: 0x000E61AE File Offset: 0x000E43AE
		internal VillageType VillageTypeOliveTrees { get; private set; }

		// Token: 0x17000DA0 RID: 3488
		// (get) Token: 0x06003881 RID: 14465 RVA: 0x000E61B7 File Offset: 0x000E43B7
		// (set) Token: 0x06003882 RID: 14466 RVA: 0x000E61BF File Offset: 0x000E43BF
		internal VillageType VillageTypeSilkPlant { get; private set; }

		// Token: 0x17000DA1 RID: 3489
		// (get) Token: 0x06003883 RID: 14467 RVA: 0x000E61C8 File Offset: 0x000E43C8
		// (set) Token: 0x06003884 RID: 14468 RVA: 0x000E61D0 File Offset: 0x000E43D0
		internal VillageType VillageTypeSilverMine { get; private set; }

		// Token: 0x06003885 RID: 14469 RVA: 0x000E61D9 File Offset: 0x000E43D9
		public DefaultVillageTypes()
		{
			this.ConsumableRawItems = new List<ItemObject>();
			this.RegisterAll();
			this.AddProductions();
		}

		// Token: 0x06003886 RID: 14470 RVA: 0x000E61F8 File Offset: 0x000E43F8
		private void RegisterAll()
		{
			this.VillageTypeWheatFarm = this.Create("wheat_farm");
			this.VillageTypeEuropeHorseRanch = this.Create("europe_horse_ranch");
			this.VillageTypeSteppeHorseRanch = this.Create("steppe_horse_ranch");
			this.VillageTypeDesertHorseRanch = this.Create("desert_horse_ranch");
			this.VillageTypeBattanianHorseRanch = this.Create("battanian_horse_ranch");
			this.VillageTypeSturgianHorseRanch = this.Create("sturgian_horse_ranch");
			this.VillageTypeVlandianHorseRanch = this.Create("vlandian_horse_ranch");
			this.VillageTypeLumberjack = this.Create("lumberjack");
			this.VillageTypeClayMine = this.Create("clay_mine");
			this.VillageTypeSaltMine = this.Create("salt_mine");
			this.VillageTypeIronMine = this.Create("iron_mine");
			this.VillageTypeFisherman = this.Create("fisherman");
			this.VillageTypeCattleRange = this.Create("cattle_farm");
			this.VillageTypeSheepFarm = this.Create("sheep_farm");
			this.VillageTypeHogFarm = this.Create("swine_farm");
			this.VillageTypeVineYard = this.Create("vineyard");
			this.VillageTypeFlaxPlant = this.Create("flax_plant");
			this.VillageTypeDateFarm = this.Create("date_farm");
			this.VillageTypeOliveTrees = this.Create("olive_trees");
			this.VillageTypeSilkPlant = this.Create("silk_plant");
			this.VillageTypeSilverMine = this.Create("silver_mine");
			this.VillageTypeTrapper = this.Create("trapper");
			this.InitializeAll();
		}

		// Token: 0x06003887 RID: 14471 RVA: 0x000E6381 File Offset: 0x000E4581
		private VillageType Create(string stringId)
		{
			return Game.Current.ObjectManager.RegisterPresumedObject<VillageType>(new VillageType(stringId));
		}

		// Token: 0x06003888 RID: 14472 RVA: 0x000E6398 File Offset: 0x000E4598
		private void InitializeAll()
		{
			this.VillageTypeWheatFarm.Initialize(new TextObject("{=BPPG2XF7}Wheat Farm", null), "wheat_farm", "wheat_farm_ucon", "wheat_farm_burned", new ValueTuple<ItemObject, float>[]
			{
				new ValueTuple<ItemObject, float>(DefaultItems.Grain, 50f)
			});
			this.VillageTypeEuropeHorseRanch.Initialize(new TextObject("{=eEh752CZ}Horse Farm", null), "europe_horse_ranch", "ranch_ucon", "europe_horse_ranch_burned", new ValueTuple<ItemObject, float>[]
			{
				new ValueTuple<ItemObject, float>(DefaultItems.Grain, 3f)
			});
			this.VillageTypeSteppeHorseRanch.Initialize(new TextObject("{=eEh752CZ}Horse Farm", null), "steppe_horse_ranch", "ranch_ucon", "steppe_horse_ranch_burned", new ValueTuple<ItemObject, float>[]
			{
				new ValueTuple<ItemObject, float>(DefaultItems.Grain, 3f)
			});
			this.VillageTypeDesertHorseRanch.Initialize(new TextObject("{=eEh752CZ}Horse Farm", null), "desert_horse_ranch", "ranch_ucon", "desert_horse_ranch_burned", new ValueTuple<ItemObject, float>[]
			{
				new ValueTuple<ItemObject, float>(DefaultItems.Grain, 3f)
			});
			this.VillageTypeBattanianHorseRanch.Initialize(new TextObject("{=eEh752CZ}Horse Farm", null), "battanian_horse_ranch", "ranch_ucon", "desert_horse_ranch_burned", new ValueTuple<ItemObject, float>[]
			{
				new ValueTuple<ItemObject, float>(DefaultItems.Grain, 3f)
			});
			this.VillageTypeSturgianHorseRanch.Initialize(new TextObject("{=eEh752CZ}Horse Farm", null), "sturgian_horse_ranch", "ranch_ucon", "desert_horse_ranch_burned", new ValueTuple<ItemObject, float>[]
			{
				new ValueTuple<ItemObject, float>(DefaultItems.Grain, 3f)
			});
			this.VillageTypeVlandianHorseRanch.Initialize(new TextObject("{=eEh752CZ}Horse Farm", null), "vlandian_horse_ranch", "ranch_ucon", "desert_horse_ranch_burned", new ValueTuple<ItemObject, float>[]
			{
				new ValueTuple<ItemObject, float>(DefaultItems.Grain, 3f)
			});
			this.VillageTypeLumberjack.Initialize(new TextObject("{=YYl1W2jU}Forester", null), "lumberjack", "lumberjack_ucon", "lumberjack_burned", new ValueTuple<ItemObject, float>[]
			{
				new ValueTuple<ItemObject, float>(DefaultItems.Grain, 3f)
			});
			this.VillageTypeClayMine.Initialize(new TextObject("{=myuzMhOn}Clay Pits", null), "clay_mine", "clay_mine_ucon", "clay_mine_burned", new ValueTuple<ItemObject, float>[]
			{
				new ValueTuple<ItemObject, float>(DefaultItems.Grain, 3f)
			});
			this.VillageTypeSaltMine.Initialize(new TextObject("{=3aOIY6wl}Salt Mine", null), "salt_mine", "salt_mine_ucon", "salt_mine_burned", new ValueTuple<ItemObject, float>[]
			{
				new ValueTuple<ItemObject, float>(DefaultItems.Grain, 3f)
			});
			this.VillageTypeIronMine.Initialize(new TextObject("{=rHcVKSbA}Iron Mine", null), "iron_mine", "iron_mine_ucon", "iron_mine_burned", new ValueTuple<ItemObject, float>[]
			{
				new ValueTuple<ItemObject, float>(DefaultItems.Grain, 3f)
			});
			this.VillageTypeFisherman.Initialize(new TextObject("{=XpREJNHD}Fishers", null), "fisherman", "fisherman_ucon", "fisherman_burned", new ValueTuple<ItemObject, float>[]
			{
				new ValueTuple<ItemObject, float>(DefaultItems.Grain, 3f)
			});
			this.VillageTypeCattleRange.Initialize(new TextObject("{=bW3csuSZ}Cattle Farms", null), "cattle_farm", "ranch_ucon", "cattle_farm_burned", new ValueTuple<ItemObject, float>[]
			{
				new ValueTuple<ItemObject, float>(DefaultItems.Grain, 3f)
			});
			this.VillageTypeSheepFarm.Initialize(new TextObject("{=QbKbGu2h}Sheep Farms", null), "sheep_farm", "ranch_ucon", "sheep_farm_burned", new ValueTuple<ItemObject, float>[]
			{
				new ValueTuple<ItemObject, float>(DefaultItems.Grain, 3f)
			});
			this.VillageTypeHogFarm.Initialize(new TextObject("{=vqSHB7mJ}Swine Farm", null), "swine_farm", "swine_farm_ucon", "swine_farm_burned", new ValueTuple<ItemObject, float>[]
			{
				new ValueTuple<ItemObject, float>(DefaultItems.Grain, 3f)
			});
			this.VillageTypeVineYard.Initialize(new TextObject("{=ZtxWTS9V}Vineyard", null), "vineyard", "vineyard_ucon", "vineyard_burned", new ValueTuple<ItemObject, float>[]
			{
				new ValueTuple<ItemObject, float>(DefaultItems.Grain, 3f)
			});
			this.VillageTypeFlaxPlant.Initialize(new TextObject("{=Z8ntYx0Y}Flax Field", null), "flax_plant", "flax_plant_ucon", "flax_plant_burned", new ValueTuple<ItemObject, float>[]
			{
				new ValueTuple<ItemObject, float>(DefaultItems.Grain, 3f)
			});
			this.VillageTypeDateFarm.Initialize(new TextObject("{=2NR2E663}Palm Orchard", null), "date_farm", "date_farm_ucon", "date_farm_burned", new ValueTuple<ItemObject, float>[]
			{
				new ValueTuple<ItemObject, float>(DefaultItems.Grain, 3f)
			});
			this.VillageTypeOliveTrees.Initialize(new TextObject("{=ewrkbwI9}Olive Trees", null), "date_farm", "date_farm_ucon", "date_farm_burned", new ValueTuple<ItemObject, float>[]
			{
				new ValueTuple<ItemObject, float>(DefaultItems.Grain, 3f)
			});
			this.VillageTypeSilkPlant.Initialize(new TextObject("{=wTyq7LaM}Silkworm Farm", null), "silk_plant", "silk_plant_ucon", "silk_plant_burned", new ValueTuple<ItemObject, float>[]
			{
				new ValueTuple<ItemObject, float>(DefaultItems.Grain, 3f)
			});
			this.VillageTypeSilverMine.Initialize(new TextObject("{=aJLQz9iZ}Silver Mine", null), "silver_mine", "silver_mine_ucon", "silver_mine_burned", new ValueTuple<ItemObject, float>[]
			{
				new ValueTuple<ItemObject, float>(DefaultItems.Grain, 3f)
			});
			this.VillageTypeTrapper.Initialize(new TextObject("{=RREyouKr}Trapper", null), "trapper", "trapper_ucon", "trapper_burned", new ValueTuple<ItemObject, float>[]
			{
				new ValueTuple<ItemObject, float>(DefaultItems.Grain, 3f)
			});
		}

		// Token: 0x06003889 RID: 14473 RVA: 0x000E6954 File Offset: 0x000E4B54
		private void AddProductions()
		{
			this.AddProductions(this.VillageTypeWheatFarm, new ValueTuple<string, float>[]
			{
				new ValueTuple<string, float>("cow", 0.2f),
				new ValueTuple<string, float>("sheep", 0.4f),
				new ValueTuple<string, float>("hog", 0.8f)
			});
			this.AddProductions(this.VillageTypeEuropeHorseRanch, new ValueTuple<string, float>[]
			{
				new ValueTuple<string, float>("empire_horse", 2.1f),
				new ValueTuple<string, float>("t2_empire_horse", 0.5f),
				new ValueTuple<string, float>("t3_empire_horse", 0.07f),
				new ValueTuple<string, float>("sumpter_horse", 0.5f),
				new ValueTuple<string, float>("mule", 0.5f),
				new ValueTuple<string, float>("saddle_horse", 0.5f),
				new ValueTuple<string, float>("old_horse", 0.5f),
				new ValueTuple<string, float>("hunter", 0.2f),
				new ValueTuple<string, float>("charger", 0.2f)
			});
			this.AddProductions(this.VillageTypeSturgianHorseRanch, new ValueTuple<string, float>[]
			{
				new ValueTuple<string, float>("sturgia_horse", 2.5f),
				new ValueTuple<string, float>("t2_sturgia_horse", 0.7f),
				new ValueTuple<string, float>("t3_sturgia_horse", 0.1f),
				new ValueTuple<string, float>("sumpter_horse", 0.5f),
				new ValueTuple<string, float>("mule", 0.5f),
				new ValueTuple<string, float>("saddle_horse", 0.5f),
				new ValueTuple<string, float>("old_horse", 0.5f),
				new ValueTuple<string, float>("hunter", 0.2f),
				new ValueTuple<string, float>("charger", 0.2f)
			});
			this.AddProductions(this.VillageTypeVlandianHorseRanch, new ValueTuple<string, float>[]
			{
				new ValueTuple<string, float>("vlandia_horse", 2.1f),
				new ValueTuple<string, float>("t2_vlandia_horse", 0.4f),
				new ValueTuple<string, float>("t3_vlandia_horse", 0.08f),
				new ValueTuple<string, float>("sumpter_horse", 0.5f),
				new ValueTuple<string, float>("mule", 0.5f),
				new ValueTuple<string, float>("saddle_horse", 0.5f),
				new ValueTuple<string, float>("old_horse", 0.5f),
				new ValueTuple<string, float>("hunter", 0.2f),
				new ValueTuple<string, float>("charger", 0.2f)
			});
			this.AddProductions(this.VillageTypeBattanianHorseRanch, new ValueTuple<string, float>[]
			{
				new ValueTuple<string, float>("battania_horse", 2.3f),
				new ValueTuple<string, float>("t2_battania_horse", 0.7f),
				new ValueTuple<string, float>("t3_battania_horse", 0.09f),
				new ValueTuple<string, float>("sumpter_horse", 0.5f),
				new ValueTuple<string, float>("mule", 0.5f),
				new ValueTuple<string, float>("saddle_horse", 0.5f),
				new ValueTuple<string, float>("old_horse", 0.5f),
				new ValueTuple<string, float>("hunter", 0.2f),
				new ValueTuple<string, float>("charger", 0.2f)
			});
			this.AddProductions(this.VillageTypeSteppeHorseRanch, new ValueTuple<string, float>[]
			{
				new ValueTuple<string, float>("khuzait_horse", 1.8f),
				new ValueTuple<string, float>("t2_khuzait_horse", 0.4f),
				new ValueTuple<string, float>("t3_khuzait_horse", 0.05f),
				new ValueTuple<string, float>("sumpter_horse", 0.5f),
				new ValueTuple<string, float>("mule", 0.5f)
			});
			this.AddProductions(this.VillageTypeDesertHorseRanch, new ValueTuple<string, float>[]
			{
				new ValueTuple<string, float>("aserai_horse", 1.7f),
				new ValueTuple<string, float>("t2_aserai_horse", 0.3f),
				new ValueTuple<string, float>("t3_aserai_horse", 0.05f),
				new ValueTuple<string, float>("camel", 0.3f),
				new ValueTuple<string, float>("war_camel", 0.08f),
				new ValueTuple<string, float>("pack_camel", 0.3f),
				new ValueTuple<string, float>("sumpter_horse", 0.4f),
				new ValueTuple<string, float>("mule", 0.5f)
			});
			this.AddProductions(this.VillageTypeCattleRange, new ValueTuple<string, float>[]
			{
				new ValueTuple<string, float>("cow", 2f),
				new ValueTuple<string, float>("butter", 4f),
				new ValueTuple<string, float>("cheese", 4f)
			});
			this.AddProductions(this.VillageTypeSheepFarm, new ValueTuple<string, float>[]
			{
				new ValueTuple<string, float>("sheep", 4f),
				new ValueTuple<string, float>("wool", 10f),
				new ValueTuple<string, float>("butter", 2f),
				new ValueTuple<string, float>("cheese", 2f)
			});
			this.AddProductions(this.VillageTypeHogFarm, new ValueTuple<string, float>[]
			{
				new ValueTuple<string, float>("hog", 8f),
				new ValueTuple<string, float>("butter", 2f),
				new ValueTuple<string, float>("cheese", 2f)
			});
			this.AddProductions(this.VillageTypeLumberjack, new ValueTuple<string, float>[]
			{
				new ValueTuple<string, float>("hardwood", 18f)
			});
			this.AddProductions(this.VillageTypeClayMine, new ValueTuple<string, float>[]
			{
				new ValueTuple<string, float>("clay", 10f)
			});
			this.AddProductions(this.VillageTypeSaltMine, new ValueTuple<string, float>[]
			{
				new ValueTuple<string, float>("salt", 15f)
			});
			this.AddProductions(this.VillageTypeIronMine, new ValueTuple<string, float>[]
			{
				new ValueTuple<string, float>("iron", 10f)
			});
			this.AddProductions(this.VillageTypeFisherman, new ValueTuple<string, float>[]
			{
				new ValueTuple<string, float>("fish", 28f)
			});
			this.AddProductions(this.VillageTypeVineYard, new ValueTuple<string, float>[]
			{
				new ValueTuple<string, float>("grape", 11f)
			});
			this.AddProductions(this.VillageTypeFlaxPlant, new ValueTuple<string, float>[]
			{
				new ValueTuple<string, float>("flax", 18f)
			});
			this.AddProductions(this.VillageTypeDateFarm, new ValueTuple<string, float>[]
			{
				new ValueTuple<string, float>("date_fruit", 8f)
			});
			this.AddProductions(this.VillageTypeOliveTrees, new ValueTuple<string, float>[]
			{
				new ValueTuple<string, float>("olives", 12f)
			});
			this.AddProductions(this.VillageTypeSilkPlant, new ValueTuple<string, float>[]
			{
				new ValueTuple<string, float>("cotton", 8f)
			});
			this.AddProductions(this.VillageTypeSilverMine, new ValueTuple<string, float>[]
			{
				new ValueTuple<string, float>("silver", 3f)
			});
			this.AddProductions(this.VillageTypeTrapper, new ValueTuple<string, float>[]
			{
				new ValueTuple<string, float>("fur", 1.4f)
			});
			this.ConsumableRawItems.Add(Game.Current.ObjectManager.GetObject<ItemObject>("grain"));
			this.ConsumableRawItems.Add(Game.Current.ObjectManager.GetObject<ItemObject>("cheese"));
			this.ConsumableRawItems.Add(Game.Current.ObjectManager.GetObject<ItemObject>("butter"));
		}

		// Token: 0x0600388A RID: 14474 RVA: 0x000E71AA File Offset: 0x000E53AA
		private void AddProductions(VillageType villageType, ValueTuple<string, float>[] productions)
		{
			villageType.AddProductions(from p in productions
				select new ValueTuple<ItemObject, float>(Game.Current.ObjectManager.GetObject<ItemObject>(p.Item1), p.Item2));
		}
	}
}
