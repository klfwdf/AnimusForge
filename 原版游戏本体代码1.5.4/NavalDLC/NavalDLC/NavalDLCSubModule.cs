using NavalDLC.CampaignBehaviors;
using NavalDLC.GameComponents;
using NavalDLC.Missions;
using NavalDLC.Storyline;
using NavalDLC.Storyline.CampaignBehaviors;
using SandBox.GameComponents;
using StoryMode;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace NavalDLC;

public class NavalDLCSubModule : MBSubModuleBase
{
	public const string ShipPhysicsReferencesXMLPath = "ShipPhysicsReferences";

	public const string MissionShipsXMLPath = "MissionShips";

	public const string ShipHullsXMLPath = "ShipHulls";

	public const string ShipSlotsXMLPath = "ShipSlots";

	public const string ShipUpgradePiecesXMLPath = "ShipUpgradePieces";

	public const string ModuleName = "NavalDLC";

	public const string FigureheadSlotTag = "figurehead";

	protected override void OnSubModuleLoad()
	{
		TauntUsageManager.Initialize();
	}

	protected override void RegisterSubModuleTypes()
	{
	}

	protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
	{
		game.AddGameHandler<NavalDLCManager>();
		NavalDLCManager.Instance = Game.Current.GetGameHandler<NavalDLCManager>();
		NavalDLCManager.Instance.OnGameStart(game, gameStarterObject);
		string applicationVersionBuildNumber = NavalVersion.GetApplicationVersionBuildNumber();
		Utilities.SetWatchdogValue("crash_tags.txt", "ModuleVersion", "NavalDLC", applicationVersionBuildNumber);
	}

	public override void OnGameEnd(Game game)
	{
		NavalDLCManager.Instance.OnGameEnd(game);
	}

	public override void InitializeSubModuleGameObjects(Game game)
	{
		NavalDLCManager.Instance.InitializeNavalGameObjects(game);
	}

	public override void RegisterSubModuleObjects(bool isSavedCampaign)
	{
		MBObjectManager.Instance.LoadXML("ShipUpgradePieces");
		MBObjectManager.Instance.LoadXML("ShipSlots");
		MBObjectManager.Instance.LoadXML("ShipHulls");
		MBObjectManager.Instance.LoadXML("ShipPhysicsReferences");
		MBObjectManager.Instance.LoadXML("MissionShips");
	}

	protected override void InitializeGameStarter(Game game, IGameStarter gameStarterObject)
	{
		if (game.GameType is Campaign)
		{
			CampaignGameStarter campaignGameStarter = gameStarterObject as CampaignGameStarter;
			AddBehaviors(campaignGameStarter, game);
			AddModels(campaignGameStarter);
		}
		else if (game.GameType is EditorGame)
		{
			gameStarterObject.AddModel(new NavalDLCShipPhysicsParametersModel());
		}
	}

	public override void OnAfterGameInitializationFinished(Game game, object starterObject)
	{
		if (game.GameType is Campaign campaign)
		{
			campaign.CampaignMissionManager = new NavalMissionManager(campaign.CampaignMissionManager);
		}
	}

	public override void OnGameInitializationFinished(Game game)
	{
		if (game.GameType is Campaign && game.GameType is CampaignStoryMode && StoryModeManager.Current != null)
		{
			NavalDLCManager.Instance.NavalStorylineData.Initialize();
		}
	}

	private void AddBehaviors(CampaignGameStarter gameStarter, Game game)
	{
		gameStarter.AddBehavior(new NavalTransitionCampaignBehavior());
		gameStarter.AddBehavior(new NavalCharacterCreationCampaignBehavior());
		gameStarter.AddBehavior(new SeaDamageCampaignBehavior());
		gameStarter.AddBehavior(new ShipProductionCampaignBehavior());
		gameStarter.AddBehavior(new ShipTradeCampaignBehavior());
		gameStarter.AddBehavior(new ShipRepairCampaignBehavior());
		gameStarter.AddBehavior(new RaftStateCampaignBehavior());
		gameStarter.AddBehavior(new ShipUpgradeCampaignBehavior());
		gameStarter.AddBehavior(new PortCharactersCampaignBehavior());
		gameStarter.AddBehavior(new ClanFleetManagementCampaignBehavior());
		gameStarter.AddBehavior(new NavalPatrolPartiesCampaignBehavior());
		gameStarter.AddBehavior(new NavalVeteransWisdomCampaignBehaviour());
		gameStarter.AddBehavior(new NavalFishingCampaignBehaviour());
		gameStarter.AddBehavior(new NavalNimbleSurgeCampaignBehaviour());
		gameStarter.AddBehavior(new NavalStormriderCampaignBehaviour());
		gameStarter.AddBehavior(new NavalOrderOfBattleCampaignBehavior());
		gameStarter.AddBehavior(new NavalDLCTutorialBoxCampaignBehavior());
		gameStarter.AddBehavior(new PiratesCampaignBehavior());
		gameStarter.AddBehavior(new NavalKingdomPolicyCampaignBehaviour());
		gameStarter.AddBehavior(new FishingPartyCampaignBehavior());
		gameStarter.AddBehavior(new StormCampaignBehavior());
		gameStarter.AddBehavior(new NavalDLCFigureheadCampaignBehavior());
		gameStarter.AddBehavior(new NavalShipDistributionCampaignBehavior());
		gameStarter.AddBehavior(new ShipNameCampaignBehavior());
		gameStarter.AddBehavior(new NavalInitializationCampaignBehavior());
		gameStarter.AddBehavior(new NavalCompanionRolesCampaignBehavior());
		gameStarter.AddBehavior(new NavalAdvancedStartingPlayerOptionsCampaignBehavior());
		gameStarter.AddBehavior(new FerryCampaignBehavior());
		gameStarter.AddBehavior(new NavalIncidentsCampaignBehaviour());
		if (game.GameType is CampaignStoryMode && StoryModeManager.Current != null)
		{
			gameStarter.AddBehavior(new NavalStorylineCampaignBehavior());
			gameStarter.AddBehavior(new NavalStorylineFirstActCampaignBehavior());
			gameStarter.AddBehavior(new NavalStorylineSecondActCampaignBehavior());
			gameStarter.AddBehavior(new NavalStorylineThirdActSecondQuestBehavior());
			gameStarter.AddBehavior(new NavalStorylineThirdActThirdQuestBehavior());
			gameStarter.AddBehavior(new NavalStorylineTravelCommentaryCampaignBehavior());
			gameStarter.AddBehavior(new NavalStorylinePlayerTownVisitCampaignBehavior());
			gameStarter.AddBehavior(new NavalStorylineHeroAgentSpawnBehavior());
			gameStarter.AddBehavior(new NavalStorylineThirdActFirstQuestBehavior());
			gameStarter.AddBehavior(new NavalStorylineThirdActFourthQuestBehavior());
			gameStarter.AddBehavior(new DefeatTheCaptorsQuestBehavior());
			gameStarter.AddBehavior(new NavalStorylineThirdActFifthQuestBehaviour());
		}
	}

	private void AddModels(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddModel(new NavalPartyNavigationModel(campaignGameStarter.GetModel<PartyNavigationModel>()));
		campaignGameStarter.AddModel(new NavalDLCBanditDensityModel());
		campaignGameStarter.AddModel(new NavalDLCCampaignShipDamageModel());
		campaignGameStarter.AddModel(new NavalDLCCampaignShipParametersModel());
		campaignGameStarter.AddModel(new NavalDLCShipDeploymentModel());
		campaignGameStarter.AddModel(new NavalDLCArmyManagementCalculationModel());
		campaignGameStarter.AddModel(new NavalDLCPartySpeedCalculationModel());
		campaignGameStarter.AddModel(new NavalDLCRaidModel());
		campaignGameStarter.AddModel(new NavalDLCBuildingModel());
		campaignGameStarter.AddModel(new NavalDLCBattleRewardModel());
		campaignGameStarter.AddModel(new NavalDLCMilitaryPowerModel());
		campaignGameStarter.AddModel(new NavalDLCShipCostModel());
		campaignGameStarter.AddModel(new NavalDLCCombatSimulationModel());
		campaignGameStarter.AddModel(new NavalDLCIncidentModel());
		campaignGameStarter.AddModel(new NavalEncounterMenuModel());
		campaignGameStarter.AddModel(new NavalDLCCaravanModel());
		campaignGameStarter.AddModel(new NavalDLCShipLimitModel());
		campaignGameStarter.AddModel(new NavalDLCPartySizeLimitModel());
		campaignGameStarter.AddModel(new NavalDLCMobilePartyAIModel());
		campaignGameStarter.AddModel(new NavalDLCEncounterModel());
		campaignGameStarter.AddModel(new NavalDLCVoiceOverModel());
		campaignGameStarter.AddModel(new NavalDLCHeroAgentLocationModel());
		campaignGameStarter.AddModel(new NavalDLCTournamentModel());
		campaignGameStarter.AddModel(new NavalDLCSettlementAccessModel());
		campaignGameStarter.AddModel(new NavalDLCFleetManagementModel());
		campaignGameStarter.AddModel(new NavalDLCTroopSacrificeModel());
		campaignGameStarter.AddModel(new NavalDLCCombatXpModel());
		campaignGameStarter.AddModel(new NavalDLCInventoryCapacityModel());
		campaignGameStarter.AddModel(new NavalDLCMobilePartyFoodConsumptionModel());
		campaignGameStarter.AddModel(new NavalDLCPartyHealingModel());
		campaignGameStarter.AddModel(new NavalDLCPartyMoraleModel());
		campaignGameStarter.AddModel(new NavalDLCPartyTrainingModel());
		campaignGameStarter.AddModel(new NavalDLCPartyTroopUpgradeModel());
		campaignGameStarter.AddModel(new NavalDLCPartyWageModel());
		campaignGameStarter.AddModel(new NavalDLCPrisonerRecruitmentCalculationModel());
		campaignGameStarter.AddModel(new NavalDLCSettlementGarrisonModel());
		campaignGameStarter.AddModel(new NavalDLCSettlementMilitiaModel());
		campaignGameStarter.AddModel(new NavalDLCVillageProductionCalculatorModel());
		campaignGameStarter.AddModel(new NavalDLCTroopSacrificeModel());
		campaignGameStarter.AddModel(new NavalDLCMapDistanceModel());
		campaignGameStarter.AddModel(new NavalDLCMapVisibilityModel());
		campaignGameStarter.AddModel(new NavalDLCPartyImpairmentModel());
		campaignGameStarter.AddModel(new NavalDLCPartyTransitionModel());
		campaignGameStarter.AddModel(new NavalDLCSettlementProsperityModel());
		campaignGameStarter.AddModel(new NavalDLCWorkshopModel());
		campaignGameStarter.AddModel(new NavalDLCBuildingConstructionModel());
		campaignGameStarter.AddModel(new NavalDLCSettlementSecurityModel());
		campaignGameStarter.AddModel(new NavalDLCClanFinanceModel());
		campaignGameStarter.AddModel(new NavalDLCClanPoliticsModel());
		campaignGameStarter.AddModel(new NavalDLCShipStatModel());
		campaignGameStarter.AddModel(new NavalDLCStormModel());
		campaignGameStarter.AddModel(new NavalDLCShipPhysicsParametersModel());
		campaignGameStarter.AddModel(new NavalDLCClanShipOwnershipModel());
		campaignGameStarter.AddModel(new NavalSettlementPatrolModel());
		campaignGameStarter.AddModel(new NavalCharacterDevelopmentModel());
		campaignGameStarter.AddModel(new NavalTradeAgreementModel());
		campaignGameStarter.AddModel(new NavalAgentStatCalculateModel());
		campaignGameStarter.AddModel(new NavalAgentApplyDamageModel());
		campaignGameStarter.AddModel(new NavalStrikeMagnitudeModel());
		campaignGameStarter.AddModel(new NavalBattleMoraleModel());
		campaignGameStarter.AddModel(new NavalMissionShipParametersModel());
		campaignGameStarter.AddModel(new NavalMissionSiegeEngineCalculationModel());
		campaignGameStarter.AddModel(new NavalBattleInitializationModel());
		campaignGameStarter.AddModel(new NavalDLCShipDistributionModel());
		campaignGameStarter.AddModel(new NavalDLCClanMemberPartyRoleModel());
		campaignGameStarter.AddModel(new NavalTargetScoreCalculatingModel());
		campaignGameStarter.AddModel(new NavalDLCBattleWreckageModel());
		campaignGameStarter.AddModel(new NavalFerryModel());
		if (Game.Current.GameType is Campaign)
		{
			campaignGameStarter.AddModel(new NavalDLCMapWeatherModel());
		}
	}
}
