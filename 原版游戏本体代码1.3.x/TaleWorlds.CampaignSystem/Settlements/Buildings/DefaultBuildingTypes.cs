using System;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Settlements.Buildings
{
	// Token: 0x020003CB RID: 971
	public class DefaultBuildingTypes
	{
		// Token: 0x17000DDE RID: 3550
		// (get) Token: 0x06003987 RID: 14727 RVA: 0x000EA20D File Offset: 0x000E840D
		private static DefaultBuildingTypes Instance
		{
			get
			{
				return Campaign.Current.DefaultBuildingTypes;
			}
		}

		// Token: 0x17000DDF RID: 3551
		// (get) Token: 0x06003988 RID: 14728 RVA: 0x000EA219 File Offset: 0x000E8419
		public static BuildingType SettlementFortifications
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingSettlementFortifications;
			}
		}

		// Token: 0x17000DE0 RID: 3552
		// (get) Token: 0x06003989 RID: 14729 RVA: 0x000EA225 File Offset: 0x000E8425
		public static BuildingType SettlementBarracks
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingSettlementBarracks;
			}
		}

		// Token: 0x17000DE1 RID: 3553
		// (get) Token: 0x0600398A RID: 14730 RVA: 0x000EA231 File Offset: 0x000E8431
		public static BuildingType SettlementTrainingFields
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingSettlementTrainingFields;
			}
		}

		// Token: 0x17000DE2 RID: 3554
		// (get) Token: 0x0600398B RID: 14731 RVA: 0x000EA23D File Offset: 0x000E843D
		public static BuildingType SettlementGuardHouse
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingSettlementGuardHouse;
			}
		}

		// Token: 0x17000DE3 RID: 3555
		// (get) Token: 0x0600398C RID: 14732 RVA: 0x000EA249 File Offset: 0x000E8449
		public static BuildingType SettlementTaxOffice
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingSettlementTaxOffice;
			}
		}

		// Token: 0x17000DE4 RID: 3556
		// (get) Token: 0x0600398D RID: 14733 RVA: 0x000EA255 File Offset: 0x000E8455
		public static BuildingType SettlementWarehouse
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingSettlementWarehouse;
			}
		}

		// Token: 0x17000DE5 RID: 3557
		// (get) Token: 0x0600398E RID: 14734 RVA: 0x000EA261 File Offset: 0x000E8461
		public static BuildingType SettlementMason
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingSettlementMason;
			}
		}

		// Token: 0x17000DE6 RID: 3558
		// (get) Token: 0x0600398F RID: 14735 RVA: 0x000EA26D File Offset: 0x000E846D
		public static BuildingType SettlementSiegeWorkshop
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingSettlementSiegeWorkshop;
			}
		}

		// Token: 0x17000DE7 RID: 3559
		// (get) Token: 0x06003990 RID: 14736 RVA: 0x000EA279 File Offset: 0x000E8479
		public static BuildingType SettlementWaterworks
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingSettlementWaterworks;
			}
		}

		// Token: 0x17000DE8 RID: 3560
		// (get) Token: 0x06003991 RID: 14737 RVA: 0x000EA285 File Offset: 0x000E8485
		public static BuildingType SettlementCourthouse
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingSettlementCourthouse;
			}
		}

		// Token: 0x17000DE9 RID: 3561
		// (get) Token: 0x06003992 RID: 14738 RVA: 0x000EA291 File Offset: 0x000E8491
		public static BuildingType SettlementMarketplace
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingSettlementMarketplace;
			}
		}

		// Token: 0x17000DEA RID: 3562
		// (get) Token: 0x06003993 RID: 14739 RVA: 0x000EA29D File Offset: 0x000E849D
		public static BuildingType SettlementRoadsAndPaths
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingSettlementRoadsAndPaths;
			}
		}

		// Token: 0x17000DEB RID: 3563
		// (get) Token: 0x06003994 RID: 14740 RVA: 0x000EA2A9 File Offset: 0x000E84A9
		public static BuildingType CastleFortifications
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingCastleFortifications;
			}
		}

		// Token: 0x17000DEC RID: 3564
		// (get) Token: 0x06003995 RID: 14741 RVA: 0x000EA2B5 File Offset: 0x000E84B5
		public static BuildingType CastleBarracks
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingCastleBarracks;
			}
		}

		// Token: 0x17000DED RID: 3565
		// (get) Token: 0x06003996 RID: 14742 RVA: 0x000EA2C1 File Offset: 0x000E84C1
		public static BuildingType CastleTrainingFields
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingCastleTrainingFields;
			}
		}

		// Token: 0x17000DEE RID: 3566
		// (get) Token: 0x06003997 RID: 14743 RVA: 0x000EA2CD File Offset: 0x000E84CD
		public static BuildingType CastleGuardHouse
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingCastleGuardHouse;
			}
		}

		// Token: 0x17000DEF RID: 3567
		// (get) Token: 0x06003998 RID: 14744 RVA: 0x000EA2D9 File Offset: 0x000E84D9
		public static BuildingType CastleCastallansOffice
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingCastleCastallansOffice;
			}
		}

		// Token: 0x17000DF0 RID: 3568
		// (get) Token: 0x06003999 RID: 14745 RVA: 0x000EA2E5 File Offset: 0x000E84E5
		public static BuildingType CastleSiegeWorkshop
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingCastleSiegeWorkshop;
			}
		}

		// Token: 0x17000DF1 RID: 3569
		// (get) Token: 0x0600399A RID: 14746 RVA: 0x000EA2F1 File Offset: 0x000E84F1
		public static BuildingType CastleCraftmansQuarters
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingCastleCraftmansQuarters;
			}
		}

		// Token: 0x17000DF2 RID: 3570
		// (get) Token: 0x0600399B RID: 14747 RVA: 0x000EA2FD File Offset: 0x000E84FD
		public static BuildingType CastleFarmlands
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingCastleFarmlands;
			}
		}

		// Token: 0x17000DF3 RID: 3571
		// (get) Token: 0x0600399C RID: 14748 RVA: 0x000EA309 File Offset: 0x000E8509
		public static BuildingType CastleGranary
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingCastleGranary;
			}
		}

		// Token: 0x17000DF4 RID: 3572
		// (get) Token: 0x0600399D RID: 14749 RVA: 0x000EA315 File Offset: 0x000E8515
		public static BuildingType CastleMason
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingCastleMason;
			}
		}

		// Token: 0x17000DF5 RID: 3573
		// (get) Token: 0x0600399E RID: 14750 RVA: 0x000EA321 File Offset: 0x000E8521
		public static BuildingType CastleRoadsAndPaths
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingCastleRoadsAndPaths;
			}
		}

		// Token: 0x17000DF6 RID: 3574
		// (get) Token: 0x0600399F RID: 14751 RVA: 0x000EA32D File Offset: 0x000E852D
		public static BuildingType SettlementDailyHousing
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingSettlementDailyHousing;
			}
		}

		// Token: 0x17000DF7 RID: 3575
		// (get) Token: 0x060039A0 RID: 14752 RVA: 0x000EA339 File Offset: 0x000E8539
		public static BuildingType SettlementDailyTrainMilitia
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingSettlementDailyTrainMilitia;
			}
		}

		// Token: 0x17000DF8 RID: 3576
		// (get) Token: 0x060039A1 RID: 14753 RVA: 0x000EA345 File Offset: 0x000E8545
		public static BuildingType SettlementDailyFestivalAndGames
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingSettlementDailyFestivalAndGames;
			}
		}

		// Token: 0x17000DF9 RID: 3577
		// (get) Token: 0x060039A2 RID: 14754 RVA: 0x000EA351 File Offset: 0x000E8551
		public static BuildingType SettlementDailyIrrigation
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingSettlementDailyIrrigation;
			}
		}

		// Token: 0x17000DFA RID: 3578
		// (get) Token: 0x060039A3 RID: 14755 RVA: 0x000EA35D File Offset: 0x000E855D
		public static BuildingType CastleDailySlackenGarrison
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingCastleDailySlackenGarrison;
			}
		}

		// Token: 0x17000DFB RID: 3579
		// (get) Token: 0x060039A4 RID: 14756 RVA: 0x000EA369 File Offset: 0x000E8569
		public static BuildingType CastleDailyRaiseTroops
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingCastleDailyRaiseTroops;
			}
		}

		// Token: 0x17000DFC RID: 3580
		// (get) Token: 0x060039A5 RID: 14757 RVA: 0x000EA375 File Offset: 0x000E8575
		public static BuildingType CastleDailyDrills
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingCastleDailyDrills;
			}
		}

		// Token: 0x17000DFD RID: 3581
		// (get) Token: 0x060039A6 RID: 14758 RVA: 0x000EA381 File Offset: 0x000E8581
		public static BuildingType CastleDailyIrrigation
		{
			get
			{
				return DefaultBuildingTypes.Instance._buildingCastleDailyIrrigation;
			}
		}

		// Token: 0x060039A7 RID: 14759 RVA: 0x000EA38D File Offset: 0x000E858D
		public DefaultBuildingTypes()
		{
			this.RegisterAll();
		}

		// Token: 0x060039A8 RID: 14760 RVA: 0x000EA39C File Offset: 0x000E859C
		private void RegisterAll()
		{
			this._buildingSettlementFortifications = this.Create("building_settlement_fortifications");
			this._buildingSettlementBarracks = this.Create("building_settlement_barracks");
			this._buildingSettlementTrainingFields = this.Create("building_settlement_training_fields");
			this._buildingSettlementGuardHouse = this.Create("building_settlement_guard_house");
			this._buildingSettlementSiegeWorkshop = this.Create("building_settlement_siege_workshop");
			this._buildingSettlementTaxOffice = this.Create("building_settlement_tax_office");
			this._buildingSettlementMarketplace = this.Create("building_settlement_marketplace");
			this._buildingSettlementWarehouse = this.Create("building_settlement_warehouse");
			this._buildingSettlementMason = this.Create("building_settlement_mason");
			this._buildingSettlementWaterworks = this.Create("building_settlement_waterworks");
			this._buildingSettlementCourthouse = this.Create("building_settlement_courthouse");
			this._buildingSettlementRoadsAndPaths = this.Create("building_settlement_roads_and_paths");
			this._buildingCastleFortifications = this.Create("building_castle_fortifications");
			this._buildingCastleBarracks = this.Create("building_castle_barracks");
			this._buildingCastleTrainingFields = this.Create("building_castle_training_fields");
			this._buildingCastleGuardHouse = this.Create("building_castle_guard_house");
			this._buildingCastleSiegeWorkshop = this.Create("building_castle_siege_workshop");
			this._buildingCastleCastallansOffice = this.Create("building_castle_castallans_office");
			this._buildingCastleGranary = this.Create("building_castle_granary");
			this._buildingCastleCraftmansQuarters = this.Create("building_castle_craftmans_quarters");
			this._buildingCastleFarmlands = this.Create("building_castle_farmlands");
			this._buildingCastleMason = this.Create("building_castle_mason");
			this._buildingCastleRoadsAndPaths = this.Create("building_castle_roads_and_paths");
			this._buildingSettlementDailyHousing = this.Create("building_settlement_daily_housing");
			this._buildingSettlementDailyTrainMilitia = this.Create("building_settlement_daily_train_militia");
			this._buildingSettlementDailyFestivalAndGames = this.Create("building_settlement_daily_festival_and_games");
			this._buildingSettlementDailyIrrigation = this.Create("building_settlement_daily_irrigation");
			this._buildingCastleDailySlackenGarrison = this.Create("building_castle_daily_slacken_garrison");
			this._buildingCastleDailyRaiseTroops = this.Create("building_castle_daily_raise_troops");
			this._buildingCastleDailyDrills = this.Create("building_castle_daily_drills");
			this._buildingCastleDailyIrrigation = this.Create("building_castle_daily_irrigation");
			this.InitializeAll();
		}

		// Token: 0x060039A9 RID: 14761 RVA: 0x000EA5BE File Offset: 0x000E87BE
		private BuildingType Create(string stringId)
		{
			return Game.Current.ObjectManager.RegisterPresumedObject<BuildingType>(new BuildingType(stringId));
		}

		// Token: 0x060039AA RID: 14762 RVA: 0x000EA5D8 File Offset: 0x000E87D8
		private void InitializeAll()
		{
			this._buildingSettlementFortifications.Initialize(new TextObject("{=CVdK1ax1}Fortifications", null), new TextObject("{=dIM6xa2O}Better fortifications and higher walls around town, also increases the max garrison limit since it provides more space for the resident troops.", null), new int[] { 0, 6000, 12000 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.GarrisonCapacity, BuildingEffectIncrementType.Add, 60f, 90f, 120f),
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.PrisonCapacity, BuildingEffectIncrementType.Add, 50f, 75f, 100f)
			}, true, 0f, 1);
			this._buildingSettlementBarracks.Initialize(new TextObject("{=x2B0OjhI}Barracks", null), new TextObject("{=JalrbDBC}Lodgings for garrison troops. Each level increases garrison limit and decreases garrison wage.", null), new int[] { 1800, 3000, 4200 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.GarrisonCapacity, BuildingEffectIncrementType.Add, 60f, 90f, 120f),
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.GarrisonWageReduction, BuildingEffectIncrementType.AddFactor, -0.05f, -0.1f, -0.15f)
			}, true, 0f, 0);
			this._buildingSettlementTrainingFields.Initialize(new TextObject("{=BkTiRPT4}Training Fields", null), new TextObject("{=NYzORuQm}Provides experience for garrison troops and increases militia veterancy.", null), new int[] { 1500, 2100, 2700 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.ExperiencePerDay, BuildingEffectIncrementType.Add, 1f, 2f, 3f),
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.MilitiaVeterancyChance, BuildingEffectIncrementType.Add, 0.1f, 0.15f, 0.2f)
			}, true, 0f, 0);
			this._buildingSettlementGuardHouse.Initialize(new TextObject("{=OHEiwoHC}Guard House", null), new TextObject("{=doojtAwr}Increases prisoner limit and provides a patrol party that improves security.", null), new int[] { 1500, 2100, 2700 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.PatrolPartyStrength, BuildingEffectIncrementType.Add, 1f, 2f, 3f),
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.PrisonCapacity, BuildingEffectIncrementType.Add, 30f, 60f, 90f)
			}, true, 0f, 0);
			this._buildingSettlementSiegeWorkshop.Initialize(new TextObject("{=9Bnwttn6}Siege Workshop", null), new TextObject("{=MharAceZ}Builds and maintains siege engines for defense of the settlement.", null), new int[] { 1200, 1800, 3000 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.BallistaOnSiegeStart, BuildingEffectIncrementType.Add, 1f, 1f, 2f),
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.CatapultOnSiegeStart, BuildingEffectIncrementType.Add, 0f, 1f, 1f),
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.SiegeEngineSpeed, BuildingEffectIncrementType.AddFactor, 0.3f, 0.6f, 1f)
			}, false, 0f, 0);
			this._buildingSettlementTaxOffice.Initialize(new TextObject("{=LG84byW0}Tax Office", null), new TextObject("{=nQ6ytZeF}Increases tax income.", null), new int[] { 1800, 3000, 4200 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.TaxPerDay, BuildingEffectIncrementType.AddFactor, 0.05f, 0.1f, 0.15f)
			}, false, 0f, 0);
			this._buildingSettlementMarketplace.Initialize(new TextObject("{=zLdXCpne}Marketplace", null), new TextObject("{=Z0xf3Bbd}Increases the tariff collected from trades made in town", null), new int[] { 2400, 3600, 4800 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.TariffIncome, BuildingEffectIncrementType.AddFactor, 0.1f, 0.2f, 0.3f),
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.CaravanAccessibility, BuildingEffectIncrementType.AddFactor, 1.02f, 1.04f, 1.06f)
			}, false, 0f, 0);
			this._buildingSettlementWarehouse.Initialize(new TextObject("{=anTRftmb}Warehouse", null), new TextObject("{=hhKDZJeM}Increases Food storage limits and improves workshop productivity.", null), new int[] { 1800, 2400, 3000 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.FoodStock, BuildingEffectIncrementType.Add, 100f, 300f, 500f),
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.WorkshopProduction, BuildingEffectIncrementType.AddFactor, 0.05f, 0.1f, 0.15f)
			}, false, 0f, 0);
			this._buildingSettlementMason.Initialize(new TextObject("{=R7ssoDHW}Mason", null), new TextObject("{=hqUPvnaj}Increase bricks per day, increasing building and repair speed.", null), new int[] { 2400, 3000, 4800 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.ConstructionPerDay, BuildingEffectIncrementType.Add, 3f, 6f, 9f),
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.WallRepairSpeed, BuildingEffectIncrementType.AddFactor, 0.05f, 0.15f, 0.3f)
			}, false, 0f, 0);
			this._buildingSettlementWaterworks.Initialize(new TextObject("{=DA0y7B3S}Waterworks", null), new TextObject("{=SfbwSASh}Waterways and sanitation, decrease food consumption.", null), new int[] { 1800, 3600, 5400 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.FoodConsumption, BuildingEffectIncrementType.AddFactor, -0.05f, -0.1f, -0.15f)
			}, false, 0f, 0);
			this._buildingSettlementCourthouse.Initialize(new TextObject("{=Bw8kAvGY}Courthouse", null), new TextObject("{=tmLJvPlz}Local judges manage disputes and maintain law and order. Provides influence and loyalty per day.", null), new int[] { 2400, 3600, 5400 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.Loyalty, BuildingEffectIncrementType.Add, 0.3f, 0.6f, 1f),
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.Influence, BuildingEffectIncrementType.Add, 0.2f, 0.5f, 1f)
			}, false, 0f, 0);
			this._buildingSettlementRoadsAndPaths.Initialize(new TextObject("{=maEmutDP}Roads and Paths", null), new TextObject("{=YPFDiwuy}Increase village production and village hearth growth.", null), new int[] { 2400, 3600, 4800 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.VillageProduction, BuildingEffectIncrementType.AddFactor, 0.05f, 0.1f, 0.15f),
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.VillageHeartsPerDay, BuildingEffectIncrementType.Add, 0.1f, 0.2f, 0.3f)
			}, false, 0f, 0);
			this._buildingCastleFortifications.Initialize(new TextObject("{=CVdK1ax1}Fortifications", null), new TextObject("{=oS5Nesmi}Better fortifications and higher walls around the keep, also increases the max garrison limit since it provides more space for the resident troops.", null), new int[] { 0, 1400, 2800 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.GarrisonCapacity, BuildingEffectIncrementType.Add, 50f, 75f, 100f),
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.PrisonCapacity, BuildingEffectIncrementType.Add, 30f, 45f, 60f)
			}, true, 0f, 1);
			this._buildingCastleBarracks.Initialize(new TextObject("{=x2B0OjhI}Barracks", null), new TextObject("{=JalrbDBC}Lodgings for garrison troops. Each level increases garrison limit and decreases garrison wage.", null), new int[] { 420, 700, 1120 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.GarrisonCapacity, BuildingEffectIncrementType.Add, 20f, 40f, 80f),
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.GarrisonWageReduction, BuildingEffectIncrementType.AddFactor, -0.1f, -0.2f, -0.3f)
			}, true, 0f, 0);
			this._buildingCastleTrainingFields.Initialize(new TextObject("{=BkTiRPT4}Training Fields", null), new TextObject("{=otWlERkc}A field for military drills that increases the daily experience gain of all garrisoned units.", null), new int[] { 420, 560, 700 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.ExperiencePerDay, BuildingEffectIncrementType.Add, 3f, 4f, 5f),
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.MilitiaVeterancyChance, BuildingEffectIncrementType.Add, 0.1f, 0.15f, 0.2f)
			}, true, 0f, 0);
			this._buildingCastleGuardHouse.Initialize(new TextObject("{=OHEiwoHC}Guard House", null), new TextObject("{=K0cbj7o3}Increase militia recruitment, and prisoner limit.", null), new int[] { 350, 490, 630 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.Militia, BuildingEffectIncrementType.Add, 1f, 2f, 3f),
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.PrisonCapacity, BuildingEffectIncrementType.Add, 10f, 30f, 50f)
			}, true, 0f, 0);
			this._buildingCastleSiegeWorkshop.Initialize(new TextObject("{=9Bnwttn6}Siege Workshop", null), new TextObject("{=YRCW0oFd}Builds and maintains siege engines for defense of the settlement.", null), new int[] { 280, 420, 700 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.BallistaOnSiegeStart, BuildingEffectIncrementType.Add, 1f, 2f, 3f),
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.CatapultOnSiegeStart, BuildingEffectIncrementType.Add, 0f, 1f, 2f),
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.SiegeEngineSpeed, BuildingEffectIncrementType.AddFactor, 0.2f, 0.4f, 0.8f)
			}, true, 0f, 0);
			this._buildingCastleCastallansOffice.Initialize(new TextObject("{=kLNnFMR9}Castellan's Office", null), new TextObject("{=GDsI6daq}Increases auto recruitment, and decreases garrison wage.", null), new int[] { 560, 840, 1260 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.GarrisonWageReduction, BuildingEffectIncrementType.AddFactor, -0.1f, -0.2f, -0.3f),
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.GarrisonAutoRecruitment, BuildingEffectIncrementType.Add, 1f, 2f, 3f)
			}, true, 0f, 0);
			this._buildingCastleGranary.Initialize(new TextObject("{=PstO2f5I}Granary", null), new TextObject("{=iazij7fO}Increases food storage limits.", null), new int[] { 420, 560, 700 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.FoodStock, BuildingEffectIncrementType.Add, 100f, 200f, 300f)
			}, false, 0f, 0);
			this._buildingCastleCraftmansQuarters.Initialize(new TextObject("{=KE1KUayw}Craftmans Quarters", null), new TextObject("{=2qZ14G9p}Provides income based on bound village hearts", null), new int[] { 350, 490, 630 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.DenarByBoundVillageHeartPerDay, BuildingEffectIncrementType.Add, 0.2f, 0.4f, 0.6f)
			}, false, 0f, 0);
			this._buildingCastleFarmlands.Initialize(new TextObject("{=l4eZqegY}Farmlands", null), new TextObject("{=tajCl8Bg}Provides daily food.", null), new int[] { 420, 630, 840 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.FoodProduction, BuildingEffectIncrementType.Add, 6f, 12f, 18f)
			}, false, 0f, 0);
			this._buildingCastleMason.Initialize(new TextObject("{=R7ssoDHW}Mason", null), new TextObject("{=hqUPvnaj}Increase bricks per day, increasing building and repair speed.", null), new int[] { 560, 700, 1120 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.ConstructionPerDay, BuildingEffectIncrementType.Add, 2f, 4f, 6f),
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.WallRepairSpeed, BuildingEffectIncrementType.AddFactor, 0.1f, 0.3f, 0.6f)
			}, false, 0f, 0);
			this._buildingCastleRoadsAndPaths.Initialize(new TextObject("{=maEmutDP}Roads and Paths", null), new TextObject("{=YPFDiwuy}Increase village production and village hearth growth.", null), new int[] { 560, 840, 1120 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.VillageProduction, BuildingEffectIncrementType.AddFactor, 0.05f, 0.1f, 0.15f),
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.VillageHeartsPerDay, BuildingEffectIncrementType.Add, 0.1f, 0.2f, 0.3f)
			}, false, 0f, 0);
			this._buildingSettlementDailyHousing.InitializeDailyProject(new TextObject("{=F4V7oaVx}Housing", null), new TextObject("{=yWXtcxqb}Construct housing so that more folks can settle, increasing population.", null), new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.Prosperity, BuildingEffectIncrementType.Add, 2f, 2f, 2f)
			});
			this._buildingSettlementDailyTrainMilitia.InitializeDailyProject(new TextObject("{=p1Y3EU5O}Train Militia", null), new TextObject("{=61J1wa6k}Schedule drills for commoners, increasing militia recruitment.", null), new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.Militia, BuildingEffectIncrementType.Add, 2f, 2f, 2f),
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.GarrisonAutoRecruitment, BuildingEffectIncrementType.Add, 1f, 1f, 1f)
			});
			this._buildingSettlementDailyFestivalAndGames.InitializeDailyProject(new TextObject("{=aEmYZadz}Festival and Games", null), new TextObject("{=ovDbQIo9}Organize festivals and games in the settlement, increasing loyalty.", null), new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.Loyalty, BuildingEffectIncrementType.Add, 3f, 3f, 3f)
			});
			this._buildingSettlementDailyIrrigation.InitializeDailyProject(new TextObject("{=O4cknzhW}Irrigation", null), new TextObject("{=CU9g49fo}Provide irrigation, increasing hearth growth in bound villages.", null), new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.VillageHeartsPerDay, BuildingEffectIncrementType.Add, 1f, 1f, 1f)
			});
			this._buildingCastleDailySlackenGarrison.InitializeDailyProject(new TextObject("{=cHIa0Xty}Slacken Garrison", null), new TextObject("{=5VBbLVBt}Decrease garrison wages.", null), new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.GarrisonWageReduction, BuildingEffectIncrementType.AddFactor, -0.05f, -0.05f, -0.05f)
			});
			this._buildingCastleDailyRaiseTroops.InitializeDailyProject(new TextObject("{=jm1ScaoK}Raise Troops", null), new TextObject("{=UsHhePdk}Increase militia recruitment, and auto recruitment.", null), new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.Militia, BuildingEffectIncrementType.Add, 3f, 3f, 3f),
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.GarrisonAutoRecruitment, BuildingEffectIncrementType.Add, 2f, 2f, 2f)
			});
			this._buildingCastleDailyDrills.InitializeDailyProject(new TextObject("{=JpiQagYa}Drills", null), new TextObject("{=e9V1W7nW}Provides experience to garrison.", null), new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.ExperiencePerDay, BuildingEffectIncrementType.Add, 8f, 8f, 8f)
			});
			this._buildingCastleDailyIrrigation.InitializeDailyProject(new TextObject("{=O4cknzhW}Irrigation", null), new TextObject("{=CU9g49fo}Provide irrigation, increasing hearth growth in bound villages.", null), new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[]
			{
				new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.VillageHeartsPerDay, BuildingEffectIncrementType.AddFactor, 0.5f, 0.5f, 0.5f)
			});
		}

		// Token: 0x040011E0 RID: 4576
		public const int MaxBuildingLevel = 3;

		// Token: 0x040011E1 RID: 4577
		private BuildingType _buildingSettlementFortifications;

		// Token: 0x040011E2 RID: 4578
		private BuildingType _buildingSettlementMarketplace;

		// Token: 0x040011E3 RID: 4579
		private BuildingType _buildingSettlementTrainingFields;

		// Token: 0x040011E4 RID: 4580
		private BuildingType _buildingSettlementBarracks;

		// Token: 0x040011E5 RID: 4581
		private BuildingType _buildingSettlementSiegeWorkshop;

		// Token: 0x040011E6 RID: 4582
		private BuildingType _buildingSettlementGuardHouse;

		// Token: 0x040011E7 RID: 4583
		private BuildingType _buildingSettlementTaxOffice;

		// Token: 0x040011E8 RID: 4584
		private BuildingType _buildingSettlementWarehouse;

		// Token: 0x040011E9 RID: 4585
		private BuildingType _buildingSettlementMason;

		// Token: 0x040011EA RID: 4586
		private BuildingType _buildingSettlementCourthouse;

		// Token: 0x040011EB RID: 4587
		private BuildingType _buildingSettlementWaterworks;

		// Token: 0x040011EC RID: 4588
		private BuildingType _buildingSettlementRoadsAndPaths;

		// Token: 0x040011ED RID: 4589
		private BuildingType _buildingCastleFortifications;

		// Token: 0x040011EE RID: 4590
		private BuildingType _buildingCastleBarracks;

		// Token: 0x040011EF RID: 4591
		private BuildingType _buildingCastleTrainingFields;

		// Token: 0x040011F0 RID: 4592
		private BuildingType _buildingCastleGranary;

		// Token: 0x040011F1 RID: 4593
		private BuildingType _buildingCastleGuardHouse;

		// Token: 0x040011F2 RID: 4594
		private BuildingType _buildingCastleCastallansOffice;

		// Token: 0x040011F3 RID: 4595
		private BuildingType _buildingCastleSiegeWorkshop;

		// Token: 0x040011F4 RID: 4596
		private BuildingType _buildingCastleCraftmansQuarters;

		// Token: 0x040011F5 RID: 4597
		private BuildingType _buildingCastleFarmlands;

		// Token: 0x040011F6 RID: 4598
		private BuildingType _buildingSettlementDailyHousing;

		// Token: 0x040011F7 RID: 4599
		private BuildingType _buildingCastleMason;

		// Token: 0x040011F8 RID: 4600
		private BuildingType _buildingCastleRoadsAndPaths;

		// Token: 0x040011F9 RID: 4601
		private BuildingType _buildingSettlementDailyIrrigation;

		// Token: 0x040011FA RID: 4602
		private BuildingType _buildingSettlementDailyTrainMilitia;

		// Token: 0x040011FB RID: 4603
		private BuildingType _buildingCastleDailySlackenGarrison;

		// Token: 0x040011FC RID: 4604
		private BuildingType _buildingSettlementDailyFestivalAndGames;

		// Token: 0x040011FD RID: 4605
		private BuildingType _buildingCastleDailyRaiseTroops;

		// Token: 0x040011FE RID: 4606
		private BuildingType _buildingCastleDailyDrills;

		// Token: 0x040011FF RID: 4607
		private BuildingType _buildingCastleDailyIrrigation;
	}
}
