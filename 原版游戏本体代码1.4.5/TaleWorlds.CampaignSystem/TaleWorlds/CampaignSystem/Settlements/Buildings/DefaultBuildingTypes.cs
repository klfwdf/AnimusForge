using System;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Settlements.Buildings;

public class DefaultBuildingTypes
{
	public const int MaxBuildingLevel = 3;

	private BuildingType _buildingSettlementFortifications;

	private BuildingType _buildingSettlementMarketplace;

	private BuildingType _buildingSettlementTrainingFields;

	private BuildingType _buildingSettlementBarracks;

	private BuildingType _buildingSettlementSiegeWorkshop;

	private BuildingType _buildingSettlementGuardHouse;

	private BuildingType _buildingSettlementTaxOffice;

	private BuildingType _buildingSettlementWarehouse;

	private BuildingType _buildingSettlementMason;

	private BuildingType _buildingSettlementCourthouse;

	private BuildingType _buildingSettlementWaterworks;

	private BuildingType _buildingSettlementRoadsAndPaths;

	private BuildingType _buildingCastleFortifications;

	private BuildingType _buildingCastleBarracks;

	private BuildingType _buildingCastleTrainingFields;

	private BuildingType _buildingCastleGranary;

	private BuildingType _buildingCastleGuardHouse;

	private BuildingType _buildingCastleCastallansOffice;

	private BuildingType _buildingCastleSiegeWorkshop;

	private BuildingType _buildingCastleCraftmansQuarters;

	private BuildingType _buildingCastleFarmlands;

	private BuildingType _buildingSettlementDailyHousing;

	private BuildingType _buildingCastleMason;

	private BuildingType _buildingCastleRoadsAndPaths;

	private BuildingType _buildingSettlementDailyIrrigation;

	private BuildingType _buildingSettlementDailyTrainMilitia;

	private BuildingType _buildingCastleDailySlackenGarrison;

	private BuildingType _buildingSettlementDailyFestivalAndGames;

	private BuildingType _buildingCastleDailyRaiseTroops;

	private BuildingType _buildingCastleDailyDrills;

	private BuildingType _buildingCastleDailyIrrigation;

	private static DefaultBuildingTypes Instance => Campaign.Current.DefaultBuildingTypes;

	public static BuildingType SettlementFortifications => Instance._buildingSettlementFortifications;

	public static BuildingType SettlementBarracks => Instance._buildingSettlementBarracks;

	public static BuildingType SettlementTrainingFields => Instance._buildingSettlementTrainingFields;

	public static BuildingType SettlementGuardHouse => Instance._buildingSettlementGuardHouse;

	public static BuildingType SettlementTaxOffice => Instance._buildingSettlementTaxOffice;

	public static BuildingType SettlementWarehouse => Instance._buildingSettlementWarehouse;

	public static BuildingType SettlementMason => Instance._buildingSettlementMason;

	public static BuildingType SettlementSiegeWorkshop => Instance._buildingSettlementSiegeWorkshop;

	public static BuildingType SettlementWaterworks => Instance._buildingSettlementWaterworks;

	public static BuildingType SettlementCourthouse => Instance._buildingSettlementCourthouse;

	public static BuildingType SettlementMarketplace => Instance._buildingSettlementMarketplace;

	public static BuildingType SettlementRoadsAndPaths => Instance._buildingSettlementRoadsAndPaths;

	public static BuildingType CastleFortifications => Instance._buildingCastleFortifications;

	public static BuildingType CastleBarracks => Instance._buildingCastleBarracks;

	public static BuildingType CastleTrainingFields => Instance._buildingCastleTrainingFields;

	public static BuildingType CastleGuardHouse => Instance._buildingCastleGuardHouse;

	public static BuildingType CastleCastallansOffice => Instance._buildingCastleCastallansOffice;

	public static BuildingType CastleSiegeWorkshop => Instance._buildingCastleSiegeWorkshop;

	public static BuildingType CastleCraftmansQuarters => Instance._buildingCastleCraftmansQuarters;

	public static BuildingType CastleFarmlands => Instance._buildingCastleFarmlands;

	public static BuildingType CastleGranary => Instance._buildingCastleGranary;

	public static BuildingType CastleMason => Instance._buildingCastleMason;

	public static BuildingType CastleRoadsAndPaths => Instance._buildingCastleRoadsAndPaths;

	public static BuildingType SettlementDailyHousing => Instance._buildingSettlementDailyHousing;

	public static BuildingType SettlementDailyTrainMilitia => Instance._buildingSettlementDailyTrainMilitia;

	public static BuildingType SettlementDailyFestivalAndGames => Instance._buildingSettlementDailyFestivalAndGames;

	public static BuildingType SettlementDailyIrrigation => Instance._buildingSettlementDailyIrrigation;

	public static BuildingType CastleDailySlackenGarrison => Instance._buildingCastleDailySlackenGarrison;

	public static BuildingType CastleDailyRaiseTroops => Instance._buildingCastleDailyRaiseTroops;

	public static BuildingType CastleDailyDrills => Instance._buildingCastleDailyDrills;

	public static BuildingType CastleDailyIrrigation => Instance._buildingCastleDailyIrrigation;

	public DefaultBuildingTypes()
	{
		RegisterAll();
	}

	private void RegisterAll()
	{
		_buildingSettlementFortifications = Create("building_settlement_fortifications");
		_buildingSettlementBarracks = Create("building_settlement_barracks");
		_buildingSettlementTrainingFields = Create("building_settlement_training_fields");
		_buildingSettlementGuardHouse = Create("building_settlement_guard_house");
		_buildingSettlementSiegeWorkshop = Create("building_settlement_siege_workshop");
		_buildingSettlementTaxOffice = Create("building_settlement_tax_office");
		_buildingSettlementMarketplace = Create("building_settlement_marketplace");
		_buildingSettlementWarehouse = Create("building_settlement_warehouse");
		_buildingSettlementMason = Create("building_settlement_mason");
		_buildingSettlementWaterworks = Create("building_settlement_waterworks");
		_buildingSettlementCourthouse = Create("building_settlement_courthouse");
		_buildingSettlementRoadsAndPaths = Create("building_settlement_roads_and_paths");
		_buildingCastleFortifications = Create("building_castle_fortifications");
		_buildingCastleBarracks = Create("building_castle_barracks");
		_buildingCastleTrainingFields = Create("building_castle_training_fields");
		_buildingCastleGuardHouse = Create("building_castle_guard_house");
		_buildingCastleSiegeWorkshop = Create("building_castle_siege_workshop");
		_buildingCastleCastallansOffice = Create("building_castle_castallans_office");
		_buildingCastleGranary = Create("building_castle_granary");
		_buildingCastleCraftmansQuarters = Create("building_castle_craftmans_quarters");
		_buildingCastleFarmlands = Create("building_castle_farmlands");
		_buildingCastleMason = Create("building_castle_mason");
		_buildingCastleRoadsAndPaths = Create("building_castle_roads_and_paths");
		_buildingSettlementDailyHousing = Create("building_settlement_daily_housing");
		_buildingSettlementDailyTrainMilitia = Create("building_settlement_daily_train_militia");
		_buildingSettlementDailyFestivalAndGames = Create("building_settlement_daily_festival_and_games");
		_buildingSettlementDailyIrrigation = Create("building_settlement_daily_irrigation");
		_buildingCastleDailySlackenGarrison = Create("building_castle_daily_slacken_garrison");
		_buildingCastleDailyRaiseTroops = Create("building_castle_daily_raise_troops");
		_buildingCastleDailyDrills = Create("building_castle_daily_drills");
		_buildingCastleDailyIrrigation = Create("building_castle_daily_irrigation");
		InitializeAll();
	}

	private BuildingType Create(string stringId)
	{
		return Game.Current.ObjectManager.RegisterPresumedObject(new BuildingType(stringId));
	}

	private void InitializeAll()
	{
		_buildingSettlementFortifications.Initialize(new TextObject("{=CVdK1ax1}Fortifications"), new TextObject("{=dIM6xa2O}Better fortifications and higher walls around town, also increases the max garrison limit since it provides more space for the resident troops."), new int[3] { 0, 6000, 12000 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[2]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.GarrisonCapacity, BuildingEffectIncrementType.Add, 60f, 90f, 120f),
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.PrisonCapacity, BuildingEffectIncrementType.Add, 50f, 75f, 100f)
		}, isMilitaryProject: true, 0f, 1);
		_buildingSettlementBarracks.Initialize(new TextObject("{=x2B0OjhI}Barracks"), new TextObject("{=JalrbDBC}Lodgings for garrison troops. Each level increases garrison limit and decreases garrison wage."), new int[3] { 1800, 3000, 4200 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[2]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.GarrisonCapacity, BuildingEffectIncrementType.Add, 60f, 90f, 120f),
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.GarrisonWageReduction, BuildingEffectIncrementType.AddFactor, -0.05f, -0.1f, -0.15f)
		}, isMilitaryProject: true, 0f);
		_buildingSettlementTrainingFields.Initialize(new TextObject("{=BkTiRPT4}Training Fields"), new TextObject("{=NYzORuQm}Provides experience for garrison troops and increases militia veterancy."), new int[3] { 1500, 2100, 2700 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[2]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.ExperiencePerDay, BuildingEffectIncrementType.Add, 1f, 2f, 3f),
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.MilitiaVeterancyChance, BuildingEffectIncrementType.Add, 0.1f, 0.15f, 0.2f)
		}, isMilitaryProject: true, 0f);
		_buildingSettlementGuardHouse.Initialize(new TextObject("{=OHEiwoHC}Guard House"), new TextObject("{=doojtAwr}Increases prisoner limit and provides a patrol party that improves security."), new int[3] { 1500, 2100, 2700 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[2]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.PatrolPartyStrength, BuildingEffectIncrementType.Add, 1f, 2f, 3f),
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.PrisonCapacity, BuildingEffectIncrementType.Add, 30f, 60f, 90f)
		}, isMilitaryProject: true, 0f);
		_buildingSettlementSiegeWorkshop.Initialize(new TextObject("{=9Bnwttn6}Siege Workshop"), new TextObject("{=MharAceZ}Builds and maintains siege engines for defense of the settlement."), new int[3] { 1200, 1800, 3000 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[3]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.BallistaOnSiegeStart, BuildingEffectIncrementType.Add, 1f, 1f, 2f),
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.CatapultOnSiegeStart, BuildingEffectIncrementType.Add, 0f, 1f, 1f),
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.SiegeEngineSpeed, BuildingEffectIncrementType.AddFactor, 0.3f, 0.6f, 1f)
		}, isMilitaryProject: false, 0f);
		_buildingSettlementTaxOffice.Initialize(new TextObject("{=LG84byW0}Tax Office"), new TextObject("{=nQ6ytZeF}Increases tax income."), new int[3] { 1800, 3000, 4200 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[1]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.TaxPerDay, BuildingEffectIncrementType.AddFactor, 0.05f, 0.1f, 0.15f)
		}, isMilitaryProject: false, 0f);
		_buildingSettlementMarketplace.Initialize(new TextObject("{=zLdXCpne}Marketplace"), new TextObject("{=Z0xf3Bbd}Increases the tariff collected from trades made in town"), new int[3] { 2400, 3600, 4800 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[2]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.TariffIncome, BuildingEffectIncrementType.AddFactor, 0.1f, 0.2f, 0.3f),
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.CaravanAccessibility, BuildingEffectIncrementType.AddFactor, 1.02f, 1.04f, 1.06f)
		}, isMilitaryProject: false, 0f);
		_buildingSettlementWarehouse.Initialize(new TextObject("{=anTRftmb}Warehouse"), new TextObject("{=hhKDZJeM}Increases Food storage limits and improves workshop productivity."), new int[3] { 1800, 2400, 3000 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[2]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.FoodStock, BuildingEffectIncrementType.Add, 100f, 300f, 500f),
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.WorkshopProduction, BuildingEffectIncrementType.AddFactor, 0.05f, 0.1f, 0.15f)
		}, isMilitaryProject: false, 0f);
		_buildingSettlementMason.Initialize(new TextObject("{=R7ssoDHW}Mason"), new TextObject("{=hqUPvnaj}Increase bricks per day, increasing building and repair speed."), new int[3] { 2400, 3000, 4800 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[2]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.ConstructionPerDay, BuildingEffectIncrementType.Add, 3f, 6f, 9f),
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.WallRepairSpeed, BuildingEffectIncrementType.AddFactor, 0.1f, 0.2f, 0.3f)
		}, isMilitaryProject: false, 0f);
		_buildingSettlementWaterworks.Initialize(new TextObject("{=DA0y7B3S}Waterworks"), new TextObject("{=SfbwSASh}Waterways and sanitation, decrease food consumption."), new int[3] { 1800, 3600, 5400 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[1]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.FoodConsumption, BuildingEffectIncrementType.AddFactor, -0.05f, -0.1f, -0.15f)
		}, isMilitaryProject: false, 0f);
		_buildingSettlementCourthouse.Initialize(new TextObject("{=Bw8kAvGY}Courthouse"), new TextObject("{=tmLJvPlz}Local judges manage disputes and maintain law and order. Provides influence and loyalty per day."), new int[3] { 2400, 3600, 5400 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[2]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.Loyalty, BuildingEffectIncrementType.Add, 0.3f, 0.6f, 1f),
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.Influence, BuildingEffectIncrementType.Add, 0.2f, 0.5f, 1f)
		}, isMilitaryProject: false, 0f);
		_buildingSettlementRoadsAndPaths.Initialize(new TextObject("{=maEmutDP}Roads and Paths"), new TextObject("{=YPFDiwuy}Increase village production and village hearth growth."), new int[3] { 2400, 3600, 4800 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[2]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.VillageProduction, BuildingEffectIncrementType.AddFactor, 0.05f, 0.1f, 0.15f),
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.VillageHeartsPerDay, BuildingEffectIncrementType.Add, 0.1f, 0.2f, 0.3f)
		}, isMilitaryProject: false, 0f);
		_buildingCastleFortifications.Initialize(new TextObject("{=CVdK1ax1}Fortifications"), new TextObject("{=oS5Nesmi}Better fortifications and higher walls around the keep, also increases the max garrison limit since it provides more space for the resident troops."), new int[3] { 0, 1400, 2800 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[2]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.GarrisonCapacity, BuildingEffectIncrementType.Add, 50f, 75f, 100f),
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.PrisonCapacity, BuildingEffectIncrementType.Add, 30f, 45f, 60f)
		}, isMilitaryProject: true, 0f, 1);
		_buildingCastleBarracks.Initialize(new TextObject("{=x2B0OjhI}Barracks"), new TextObject("{=JalrbDBC}Lodgings for garrison troops. Each level increases garrison limit and decreases garrison wage."), new int[3] { 420, 700, 1120 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[2]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.GarrisonCapacity, BuildingEffectIncrementType.Add, 20f, 40f, 80f),
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.GarrisonWageReduction, BuildingEffectIncrementType.AddFactor, -0.1f, -0.2f, -0.3f)
		}, isMilitaryProject: true, 0f);
		_buildingCastleTrainingFields.Initialize(new TextObject("{=BkTiRPT4}Training Fields"), new TextObject("{=otWlERkc}A field for military drills that increases the daily experience gain of all garrisoned units."), new int[3] { 420, 560, 700 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[2]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.ExperiencePerDay, BuildingEffectIncrementType.Add, 3f, 4f, 5f),
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.MilitiaVeterancyChance, BuildingEffectIncrementType.Add, 0.1f, 0.15f, 0.2f)
		}, isMilitaryProject: true, 0f);
		_buildingCastleGuardHouse.Initialize(new TextObject("{=OHEiwoHC}Guard House"), new TextObject("{=K0cbj7o3}Increase militia recruitment, and prisoner limit."), new int[3] { 350, 490, 630 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[2]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.Militia, BuildingEffectIncrementType.Add, 1f, 2f, 3f),
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.PrisonCapacity, BuildingEffectIncrementType.Add, 10f, 30f, 50f)
		}, isMilitaryProject: true, 0f);
		_buildingCastleSiegeWorkshop.Initialize(new TextObject("{=9Bnwttn6}Siege Workshop"), new TextObject("{=YRCW0oFd}Builds and maintains siege engines for defense of the settlement."), new int[3] { 280, 420, 700 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[3]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.BallistaOnSiegeStart, BuildingEffectIncrementType.Add, 1f, 2f, 3f),
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.CatapultOnSiegeStart, BuildingEffectIncrementType.Add, 0f, 1f, 2f),
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.SiegeEngineSpeed, BuildingEffectIncrementType.AddFactor, 0.2f, 0.4f, 0.8f)
		}, isMilitaryProject: true, 0f);
		_buildingCastleCastallansOffice.Initialize(new TextObject("{=kLNnFMR9}Castellan's Office"), new TextObject("{=GDsI6daq}Increases auto recruitment, and decreases garrison wage."), new int[3] { 560, 840, 1260 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[2]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.GarrisonWageReduction, BuildingEffectIncrementType.AddFactor, -0.1f, -0.2f, -0.3f),
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.GarrisonAutoRecruitment, BuildingEffectIncrementType.Add, 1f, 2f, 3f)
		}, isMilitaryProject: true, 0f);
		_buildingCastleGranary.Initialize(new TextObject("{=PstO2f5I}Granary"), new TextObject("{=iazij7fO}Increases food storage limits."), new int[3] { 420, 560, 700 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[1]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.FoodStock, BuildingEffectIncrementType.Add, 100f, 200f, 300f)
		}, isMilitaryProject: false, 0f);
		_buildingCastleCraftmansQuarters.Initialize(new TextObject("{=KE1KUayw}Craftmans Quarters"), new TextObject("{=2qZ14G9p}Provides income based on bound village hearts"), new int[3] { 350, 490, 630 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[1]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.DenarByBoundVillageHeartPerDay, BuildingEffectIncrementType.Add, 0.2f, 0.4f, 0.6f)
		}, isMilitaryProject: false, 0f);
		_buildingCastleFarmlands.Initialize(new TextObject("{=l4eZqegY}Farmlands"), new TextObject("{=tajCl8Bg}Provides daily food."), new int[3] { 420, 630, 840 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[1]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.FoodProduction, BuildingEffectIncrementType.Add, 6f, 12f, 18f)
		}, isMilitaryProject: false, 0f);
		_buildingCastleMason.Initialize(new TextObject("{=R7ssoDHW}Mason"), new TextObject("{=hqUPvnaj}Increase bricks per day, increasing building and repair speed."), new int[3] { 560, 700, 1120 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[2]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.ConstructionPerDay, BuildingEffectIncrementType.Add, 2f, 4f, 6f),
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.WallRepairSpeed, BuildingEffectIncrementType.AddFactor, 0.1f, 0.3f, 0.6f)
		}, isMilitaryProject: false, 0f);
		_buildingCastleRoadsAndPaths.Initialize(new TextObject("{=maEmutDP}Roads and Paths"), new TextObject("{=YPFDiwuy}Increase village production and village hearth growth."), new int[3] { 560, 840, 1120 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[2]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.VillageProduction, BuildingEffectIncrementType.AddFactor, 0.05f, 0.1f, 0.15f),
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.VillageHeartsPerDay, BuildingEffectIncrementType.Add, 0.1f, 0.2f, 0.3f)
		}, isMilitaryProject: false, 0f);
		_buildingSettlementDailyHousing.InitializeDailyProject(new TextObject("{=F4V7oaVx}Housing"), new TextObject("{=yWXtcxqb}Construct housing so that more folks can settle, increasing population."), new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[1]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.Prosperity, BuildingEffectIncrementType.Add, 2f, 2f, 2f)
		});
		_buildingSettlementDailyTrainMilitia.InitializeDailyProject(new TextObject("{=p1Y3EU5O}Train Militia"), new TextObject("{=61J1wa6k}Schedule drills for commoners, increasing militia recruitment."), new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[2]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.Militia, BuildingEffectIncrementType.Add, 2f, 2f, 2f),
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.GarrisonAutoRecruitment, BuildingEffectIncrementType.Add, 1f, 1f, 1f)
		});
		_buildingSettlementDailyFestivalAndGames.InitializeDailyProject(new TextObject("{=aEmYZadz}Festival and Games"), new TextObject("{=ovDbQIo9}Organize festivals and games in the settlement, increasing loyalty."), new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[1]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.Loyalty, BuildingEffectIncrementType.Add, 3f, 3f, 3f)
		});
		_buildingSettlementDailyIrrigation.InitializeDailyProject(new TextObject("{=O4cknzhW}Irrigation"), new TextObject("{=CU9g49fo}Provide irrigation, increasing hearth growth in bound villages."), new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[1]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.VillageHeartsPerDay, BuildingEffectIncrementType.Add, 1f, 1f, 1f)
		});
		_buildingCastleDailySlackenGarrison.InitializeDailyProject(new TextObject("{=cHIa0Xty}Slacken Garrison"), new TextObject("{=5VBbLVBt}Decrease garrison wages."), new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[1]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.GarrisonWageReduction, BuildingEffectIncrementType.AddFactor, -0.05f, -0.05f, -0.05f)
		});
		_buildingCastleDailyRaiseTroops.InitializeDailyProject(new TextObject("{=jm1ScaoK}Raise Troops"), new TextObject("{=UsHhePdk}Increase militia recruitment, and auto recruitment."), new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[2]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.Militia, BuildingEffectIncrementType.Add, 3f, 3f, 3f),
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.GarrisonAutoRecruitment, BuildingEffectIncrementType.Add, 2f, 2f, 2f)
		});
		_buildingCastleDailyDrills.InitializeDailyProject(new TextObject("{=JpiQagYa}Drills"), new TextObject("{=e9V1W7nW}Provides experience to garrison."), new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[1]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.ExperiencePerDay, BuildingEffectIncrementType.Add, 8f, 8f, 8f)
		});
		_buildingCastleDailyIrrigation.InitializeDailyProject(new TextObject("{=O4cknzhW}Irrigation"), new TextObject("{=CU9g49fo}Provide irrigation, increasing hearth growth in bound villages."), new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[1]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.VillageHeartsPerDay, BuildingEffectIncrementType.AddFactor, 0.5f, 0.5f, 0.5f)
		});
	}
}
