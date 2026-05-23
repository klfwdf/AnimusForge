using System.Collections.Generic;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem;

public sealed class GameModels : GameModelsManager
{
	public MapVisibilityModel MapVisibilityModel { get; private set; }

	public InformationRestrictionModel InformationRestrictionModel { get; private set; }

	public PartySpeedModel PartySpeedCalculatingModel { get; private set; }

	public PartyHealingModel PartyHealingModel { get; private set; }

	public CaravanModel CaravanModel { get; private set; }

	public PartyTrainingModel PartyTrainingModel { get; private set; }

	public BarterModel BarterModel { get; private set; }

	public PersuasionModel PersuasionModel { get; private set; }

	public DefectionModel DefectionModel { get; private set; }

	public CombatSimulationModel CombatSimulationModel { get; private set; }

	public CombatXpModel CombatXpModel { get; private set; }

	public GenericXpModel GenericXpModel { get; private set; }

	public TradeAgreementModel TradeAgreementModel { get; private set; }

	public SmithingModel SmithingModel { get; private set; }

	public PartyTradeModel PartyTradeModel { get; private set; }

	public RansomValueCalculationModel RansomValueCalculationModel { get; private set; }

	public RaidModel RaidModel { get; private set; }

	public MobilePartyFoodConsumptionModel MobilePartyFoodConsumptionModel { get; private set; }

	public PartyFoodBuyingModel PartyFoodBuyingModel { get; private set; }

	public PartyImpairmentModel PartyImpairmentModel { get; private set; }

	public PartyMoraleModel PartyMoraleModel { get; private set; }

	public PartyDesertionModel PartyDesertionModel { get; private set; }

	public PartyTransitionModel PartyTransitionModel { get; private set; }

	public DiplomacyModel DiplomacyModel { get; private set; }

	public AllianceModel AllianceModel { get; private set; }

	public MinorFactionsModel MinorFactionsModel { get; private set; }

	public HideoutModel HideoutModel { get; private set; }

	public KingdomCreationModel KingdomCreationModel { get; private set; }

	public KingdomDecisionPermissionModel KingdomDecisionPermissionModel { get; private set; }

	public EmissaryModel EmissaryModel { get; private set; }

	public CharacterDevelopmentModel CharacterDevelopmentModel { get; private set; }

	public CharacterStatsModel CharacterStatsModel { get; private set; }

	public EncounterModel EncounterModel { get; private set; }

	public SettlementPatrolModel SettlementPatrolModel { get; private set; }

	public ItemDiscardModel ItemDiscardModel { get; private set; }

	public ValuationModel ValuationModel { get; private set; }

	public PartySizeLimitModel PartySizeLimitModel { get; private set; }

	public PartyShipLimitModel PartyShipLimitModel { get; private set; }

	public InventoryCapacityModel InventoryCapacityModel { get; private set; }

	public PartyWageModel PartyWageModel { get; private set; }

	public VillageProductionCalculatorModel VillageProductionCalculatorModel { get; private set; }

	public VolunteerModel VolunteerModel { get; private set; }

	public RomanceModel RomanceModel { get; private set; }

	public MobilePartyAIModel MobilePartyAIModel { get; private set; }

	public ArmyManagementCalculationModel ArmyManagementCalculationModel { get; private set; }

	public BanditDensityModel BanditDensityModel { get; private set; }

	public EncounterGameMenuModel EncounterGameMenuModel { get; private set; }

	public BattleRewardModel BattleRewardModel { get; private set; }

	public MapTrackModel MapTrackModel { get; private set; }

	public MapDistanceModel MapDistanceModel { get; private set; }

	public PartyNavigationModel PartyNavigationModel { get; private set; }

	public MapWeatherModel MapWeatherModel { get; private set; }

	public TargetScoreCalculatingModel TargetScoreCalculatingModel { get; private set; }

	public TradeItemPriceFactorModel TradeItemPriceFactorModel { get; private set; }

	public SettlementEconomyModel SettlementEconomyModel { get; private set; }

	public SettlementFoodModel SettlementFoodModel { get; private set; }

	public SettlementValueModel SettlementValueModel { get; private set; }

	public SettlementMilitiaModel SettlementMilitiaModel { get; private set; }

	public SettlementLoyaltyModel SettlementLoyaltyModel { get; private set; }

	public SettlementSecurityModel SettlementSecurityModel { get; private set; }

	public SettlementProsperityModel SettlementProsperityModel { get; private set; }

	public SettlementGarrisonModel SettlementGarrisonModel { get; private set; }

	public ClanTierModel ClanTierModel { get; private set; }

	public VassalRewardsModel VassalRewardsModel { get; private set; }

	public ClanPoliticsModel ClanPoliticsModel { get; private set; }

	public ClanFinanceModel ClanFinanceModel { get; private set; }

	public SettlementTaxModel SettlementTaxModel { get; private set; }

	public HeroAgentLocationModel HeroAgentLocationModel { get; private set; }

	public HeirSelectionCalculationModel HeirSelectionCalculationModel { get; private set; }

	public HeroDeathProbabilityCalculationModel HeroDeathProbabilityCalculationModel { get; private set; }

	public BuildingConstructionModel BuildingConstructionModel { get; private set; }

	public BuildingEffectModel BuildingEffectModel { get; private set; }

	public WallHitPointCalculationModel WallHitPointCalculationModel { get; private set; }

	public MarriageModel MarriageModel { get; private set; }

	public AgeModel AgeModel { get; private set; }

	public PlayerProgressionModel PlayerProgressionModel { get; private set; }

	public DailyTroopXpBonusModel DailyTroopXpBonusModel { get; private set; }

	public PregnancyModel PregnancyModel { get; private set; }

	public NotablePowerModel NotablePowerModel { get; private set; }

	public MilitaryPowerModel MilitaryPowerModel { get; private set; }

	public PrisonerDonationModel PrisonerDonationModel { get; private set; }

	public NotableSpawnModel NotableSpawnModel { get; private set; }

	public TournamentModel TournamentModel { get; private set; }

	public CrimeModel CrimeModel { get; private set; }

	public DisguiseDetectionModel DisguiseDetectionModel { get; private set; }

	public BribeCalculationModel BribeCalculationModel { get; private set; }

	public TroopSacrificeModel TroopSacrificeModel { get; private set; }

	public SiegeStrategyActionModel SiegeStrategyActionModel { get; private set; }

	public SiegeEventModel SiegeEventModel { get; private set; }

	public SiegeAftermathModel SiegeAftermathModel { get; private set; }

	public SiegeLordsHallFightModel SiegeLordsHallFightModel { get; private set; }

	public CompanionHiringPriceCalculationModel CompanionHiringPriceCalculationModel { get; private set; }

	public BuildingScoreCalculationModel BuildingScoreCalculationModel { get; private set; }

	public SettlementAccessModel SettlementAccessModel { get; private set; }

	public IssueModel IssueModel { get; private set; }

	public PrisonerRecruitmentCalculationModel PrisonerRecruitmentCalculationModel { get; private set; }

	public PartyTroopUpgradeModel PartyTroopUpgradeModel { get; private set; }

	public TavernMercenaryTroopsModel TavernMercenaryTroopsModel { get; private set; }

	public WorkshopModel WorkshopModel { get; private set; }

	public DifficultyModel DifficultyModel { get; private set; }

	public LocationModel LocationModel { get; private set; }

	public PrisonBreakModel PrisonBreakModel { get; private set; }

	public BattleCaptainModel BattleCaptainModel { get; private set; }

	public ExecutionRelationModel ExecutionRelationModel { get; private set; }

	public BannerItemModel BannerItemModel { get; private set; }

	public DelayedTeleportationModel DelayedTeleportationModel { get; private set; }

	public TroopSupplierProbabilityModel TroopSupplierProbabilityModel { get; private set; }

	public CutsceneSelectionModel CutsceneSelectionModel { get; private set; }

	public EquipmentSelectionModel EquipmentSelectionModel { get; private set; }

	public AlleyModel AlleyModel { get; private set; }

	public VoiceOverModel VoiceOverModel { get; private set; }

	public CampaignTimeModel CampaignTimeModel { get; private set; }

	public VillageTradeModel VillageTradeModel { get; private set; }

	public HeroCreationModel HeroCreationModel { get; private set; }

	public CampaignShipDamageModel CampaignShipDamageModel { get; private set; }

	public CampaignShipParametersModel CampaignShipParametersModel { get; private set; }

	public BuildingModel BuildingModel { get; private set; }

	public ShipCostModel ShipCostModel { get; private set; }

	public ShipStatModel ShipStatModel { get; private set; }

	public SceneModel SceneModel { get; private set; }

	public BodyPropertiesModel BodyPropertiesModel { get; private set; }

	public IncidentModel IncidentModel { get; private set; }

	public FleetManagementModel FleetManagementModel { get; private set; }

	public ClanMemberPartyRoleModel ClanMemberPartyRoleModel { get; private set; }

	private void GetSpecificGameBehaviors()
	{
		if (Campaign.Current.GameMode == CampaignGameMode.Campaign || Campaign.Current.GameMode == CampaignGameMode.Tutorial)
		{
			CharacterDevelopmentModel = GetGameModel<CharacterDevelopmentModel>();
			CharacterStatsModel = GetGameModel<CharacterStatsModel>();
			EncounterModel = GetGameModel<EncounterModel>();
			SettlementPatrolModel = GetGameModel<SettlementPatrolModel>();
			ItemDiscardModel = GetGameModel<ItemDiscardModel>();
			ValuationModel = GetGameModel<ValuationModel>();
			MapVisibilityModel = GetGameModel<MapVisibilityModel>();
			InformationRestrictionModel = GetGameModel<InformationRestrictionModel>();
			PartySpeedCalculatingModel = GetGameModel<PartySpeedModel>();
			PartyHealingModel = GetGameModel<PartyHealingModel>();
			CaravanModel = GetGameModel<CaravanModel>();
			PartyTrainingModel = GetGameModel<PartyTrainingModel>();
			PartyTradeModel = GetGameModel<PartyTradeModel>();
			RansomValueCalculationModel = GetGameModel<RansomValueCalculationModel>();
			RaidModel = GetGameModel<RaidModel>();
			CombatSimulationModel = GetGameModel<CombatSimulationModel>();
			CombatXpModel = GetGameModel<CombatXpModel>();
			GenericXpModel = GetGameModel<GenericXpModel>();
			TradeAgreementModel = GetGameModel<TradeAgreementModel>();
			SmithingModel = GetGameModel<SmithingModel>();
			MobilePartyFoodConsumptionModel = GetGameModel<MobilePartyFoodConsumptionModel>();
			PartyImpairmentModel = GetGameModel<PartyImpairmentModel>();
			PartyFoodBuyingModel = GetGameModel<PartyFoodBuyingModel>();
			PartyMoraleModel = GetGameModel<PartyMoraleModel>();
			PartyDesertionModel = GetGameModel<PartyDesertionModel>();
			HideoutModel = GetGameModel<HideoutModel>();
			DiplomacyModel = GetGameModel<DiplomacyModel>();
			AllianceModel = GetGameModel<AllianceModel>();
			PartyTransitionModel = GetGameModel<PartyTransitionModel>();
			MinorFactionsModel = GetGameModel<MinorFactionsModel>();
			KingdomCreationModel = GetGameModel<KingdomCreationModel>();
			EmissaryModel = GetGameModel<EmissaryModel>();
			KingdomDecisionPermissionModel = GetGameModel<KingdomDecisionPermissionModel>();
			VillageProductionCalculatorModel = GetGameModel<VillageProductionCalculatorModel>();
			RomanceModel = GetGameModel<RomanceModel>();
			VolunteerModel = GetGameModel<VolunteerModel>();
			ArmyManagementCalculationModel = GetGameModel<ArmyManagementCalculationModel>();
			BanditDensityModel = GetGameModel<BanditDensityModel>();
			EncounterGameMenuModel = GetGameModel<EncounterGameMenuModel>();
			BattleRewardModel = GetGameModel<BattleRewardModel>();
			MapTrackModel = GetGameModel<MapTrackModel>();
			MapDistanceModel = GetGameModel<MapDistanceModel>();
			PartyNavigationModel = GetGameModel<PartyNavigationModel>();
			MapWeatherModel = GetGameModel<MapWeatherModel>();
			TargetScoreCalculatingModel = GetGameModel<TargetScoreCalculatingModel>();
			PartySizeLimitModel = GetGameModel<PartySizeLimitModel>();
			PartyShipLimitModel = GetGameModel<PartyShipLimitModel>();
			PartyWageModel = GetGameModel<PartyWageModel>();
			PlayerProgressionModel = GetGameModel<PlayerProgressionModel>();
			InventoryCapacityModel = GetGameModel<InventoryCapacityModel>();
			TradeItemPriceFactorModel = GetGameModel<TradeItemPriceFactorModel>();
			SettlementValueModel = GetGameModel<SettlementValueModel>();
			SettlementEconomyModel = GetGameModel<SettlementEconomyModel>();
			SettlementMilitiaModel = GetGameModel<SettlementMilitiaModel>();
			SettlementFoodModel = GetGameModel<SettlementFoodModel>();
			SettlementLoyaltyModel = GetGameModel<SettlementLoyaltyModel>();
			SettlementSecurityModel = GetGameModel<SettlementSecurityModel>();
			SettlementProsperityModel = GetGameModel<SettlementProsperityModel>();
			SettlementGarrisonModel = GetGameModel<SettlementGarrisonModel>();
			SettlementTaxModel = GetGameModel<SettlementTaxModel>();
			HeroAgentLocationModel = GetGameModel<HeroAgentLocationModel>();
			BarterModel = GetGameModel<BarterModel>();
			PersuasionModel = GetGameModel<PersuasionModel>();
			DefectionModel = GetGameModel<DefectionModel>();
			ClanTierModel = GetGameModel<ClanTierModel>();
			VassalRewardsModel = GetGameModel<VassalRewardsModel>();
			ClanPoliticsModel = GetGameModel<ClanPoliticsModel>();
			ClanFinanceModel = GetGameModel<ClanFinanceModel>();
			HeirSelectionCalculationModel = GetGameModel<HeirSelectionCalculationModel>();
			HeroDeathProbabilityCalculationModel = GetGameModel<HeroDeathProbabilityCalculationModel>();
			BuildingConstructionModel = GetGameModel<BuildingConstructionModel>();
			BuildingEffectModel = GetGameModel<BuildingEffectModel>();
			WallHitPointCalculationModel = GetGameModel<WallHitPointCalculationModel>();
			MarriageModel = GetGameModel<MarriageModel>();
			AgeModel = GetGameModel<AgeModel>();
			DailyTroopXpBonusModel = GetGameModel<DailyTroopXpBonusModel>();
			PregnancyModel = GetGameModel<PregnancyModel>();
			NotablePowerModel = GetGameModel<NotablePowerModel>();
			NotableSpawnModel = GetGameModel<NotableSpawnModel>();
			TournamentModel = GetGameModel<TournamentModel>();
			SiegeStrategyActionModel = GetGameModel<SiegeStrategyActionModel>();
			SiegeEventModel = GetGameModel<SiegeEventModel>();
			SiegeAftermathModel = GetGameModel<SiegeAftermathModel>();
			SiegeLordsHallFightModel = GetGameModel<SiegeLordsHallFightModel>();
			CrimeModel = GetGameModel<CrimeModel>();
			DisguiseDetectionModel = GetGameModel<DisguiseDetectionModel>();
			BribeCalculationModel = GetGameModel<BribeCalculationModel>();
			CompanionHiringPriceCalculationModel = GetGameModel<CompanionHiringPriceCalculationModel>();
			TroopSacrificeModel = GetGameModel<TroopSacrificeModel>();
			BuildingScoreCalculationModel = GetGameModel<BuildingScoreCalculationModel>();
			SettlementAccessModel = GetGameModel<SettlementAccessModel>();
			IssueModel = GetGameModel<IssueModel>();
			PrisonerRecruitmentCalculationModel = GetGameModel<PrisonerRecruitmentCalculationModel>();
			PartyTroopUpgradeModel = GetGameModel<PartyTroopUpgradeModel>();
			TavernMercenaryTroopsModel = GetGameModel<TavernMercenaryTroopsModel>();
			WorkshopModel = GetGameModel<WorkshopModel>();
			DifficultyModel = GetGameModel<DifficultyModel>();
			LocationModel = GetGameModel<LocationModel>();
			MilitaryPowerModel = GetGameModel<MilitaryPowerModel>();
			PrisonerDonationModel = GetGameModel<PrisonerDonationModel>();
			PrisonBreakModel = GetGameModel<PrisonBreakModel>();
			BattleCaptainModel = GetGameModel<BattleCaptainModel>();
			ExecutionRelationModel = GetGameModel<ExecutionRelationModel>();
			BannerItemModel = GetGameModel<BannerItemModel>();
			DelayedTeleportationModel = GetGameModel<DelayedTeleportationModel>();
			TroopSupplierProbabilityModel = GetGameModel<TroopSupplierProbabilityModel>();
			CutsceneSelectionModel = GetGameModel<CutsceneSelectionModel>();
			EquipmentSelectionModel = GetGameModel<EquipmentSelectionModel>();
			AlleyModel = GetGameModel<AlleyModel>();
			VoiceOverModel = GetGameModel<VoiceOverModel>();
			CampaignTimeModel = GetGameModel<CampaignTimeModel>();
			VillageTradeModel = GetGameModel<VillageTradeModel>();
			PartyNavigationModel = GetGameModel<PartyNavigationModel>();
			MobilePartyAIModel = GetGameModel<MobilePartyAIModel>();
			HeroCreationModel = GetGameModel<HeroCreationModel>();
			CampaignShipDamageModel = GetGameModel<CampaignShipDamageModel>();
			CampaignShipParametersModel = GetGameModel<CampaignShipParametersModel>();
			BuildingModel = GetGameModel<BuildingModel>();
			ShipCostModel = GetGameModel<ShipCostModel>();
			SceneModel = GetGameModel<SceneModel>();
			IncidentModel = GetGameModel<IncidentModel>();
			BodyPropertiesModel = GetGameModel<BodyPropertiesModel>();
			FleetManagementModel = GetGameModel<FleetManagementModel>();
			ShipStatModel = GetGameModel<ShipStatModel>();
			ClanMemberPartyRoleModel = GetGameModel<ClanMemberPartyRoleModel>();
		}
	}

	public GameModels(IEnumerable<GameModel> inputComponents)
		: base(inputComponents)
	{
		GetSpecificGameBehaviors();
	}
}
