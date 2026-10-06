using StoryMode.GameComponents;
using StoryMode.GameComponents.CampaignBehaviors;
using StoryMode.Quests.PlayerClanQuests;
using StoryMode.Quests.SecondPhase;
using StoryMode.Quests.ThirdPhase;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace StoryMode;

public class StoryModeSubModule : MBSubModuleBase
{
	protected override void InitializeGameStarter(Game game, IGameStarter gameStarterObject)
	{
		if (game.GameType is CampaignStoryMode campaignStoryMode)
		{
			CampaignGameStarter campaignGameStarter = (CampaignGameStarter)gameStarterObject;
			campaignStoryMode.AddCampaignEventReceiver(StoryModeEvents.Instance);
			AddGameMenus(campaignGameStarter);
			AddModels(campaignGameStarter);
			AddBehaviors(campaignGameStarter);
		}
	}

	public override void OnGameEnd(Game game)
	{
		base.OnGameEnd(game);
		if (game.GameType is CampaignStoryMode && StoryModeManager.Current != null)
		{
			StoryModeManager.Current.Destroy();
		}
	}

	private void AddGameMenus(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddGameMenu("menu_story_mode_welcome", "{=GGfM1HKn}Welcome to MBII Bannerlord", null);
		campaignGameStarter.AddGameMenuOption("menu_story_mode_welcome", "mno_continue", "{=str_continue}Continue...", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}, null);
	}

	private void AddBehaviors(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddBehavior(new LordConversationsStoryModeBehavior());
		campaignGameStarter.AddBehavior(new MainStorylineCampaignBehavior());
		if (!StoryModeManager.Current.MainStoryLine.IsCompleted)
		{
			if (!StoryModeManager.Current.MainStoryLine.TutorialPhase.IsCompleted)
			{
				campaignGameStarter.AddBehavior(new TutorialPhaseCampaignBehavior());
			}
			if (!StoryModeManager.Current.MainStoryLine.IsFirstPhaseCompleted)
			{
				campaignGameStarter.AddBehavior(new FirstPhaseCampaignBehavior());
			}
			if (!StoryModeManager.Current.MainStoryLine.IsSecondPhaseCompleted)
			{
				campaignGameStarter.AddBehavior(new SecondPhaseCampaignBehavior());
			}
			campaignGameStarter.AddBehavior(new ThirdPhaseCampaignBehavior());
		}
		campaignGameStarter.AddBehavior(new TrainingFieldCampaignBehavior());
		campaignGameStarter.AddBehavior(new StoryModeTutorialBoxCampaignBehavior());
		campaignGameStarter.AddBehavior(new StoryModeCharacterCreationCampaignBehavior());
		campaignGameStarter.AddBehavior(new StoryModeBanditSpawnCampaignBehavior());
		Debug.Print("campaignGameStarter.AddBehavior(AchievementsCampaignBehavior)");
		campaignGameStarter.AddBehavior(new AchievementsCampaignBehavior());
		campaignGameStarter.AddBehavior(new WeakenEmpireQuestBehavior());
		campaignGameStarter.AddBehavior(new AssembleEmpireQuestBehavior());
		campaignGameStarter.AddBehavior(new DefeatTheConspiracyQuestBehavior());
		campaignGameStarter.AddBehavior(new RescueFamilyQuestBehavior());
	}

	private void AddModels(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddModel(new StoryModeBanditDensityModel());
		campaignGameStarter.AddModel(new StoryModeEncounterGameMenuModel());
		campaignGameStarter.AddModel(new StoryModeBattleRewardModel());
		campaignGameStarter.AddModel(new StoryModeTargetScoreCalculatingModel());
		campaignGameStarter.AddModel(new StoryModePartyWageModel());
		campaignGameStarter.AddModel(new StoryModeKingdomDecisionPermissionModel());
		campaignGameStarter.AddModel(new StoryModeCombatXpModel());
		campaignGameStarter.AddModel(new StoryModeGenericXpModel());
		campaignGameStarter.AddModel(new StoryModeNotableSpawnModel());
		campaignGameStarter.AddModel(new StoryModeHeroDeathProbabilityCalculationModel());
		campaignGameStarter.AddModel(new StoryModeAgentDecideKilledOrUnconsciousModel());
		campaignGameStarter.AddModel(new StoryModePartySizeLimitModel());
		campaignGameStarter.AddModel(new StoryModeBannerItemModel());
		campaignGameStarter.AddModel(new StoryModePrisonerRecruitmentCalculationModel());
		campaignGameStarter.AddModel(new StoryModeTroopSupplierProbabilityModel());
		campaignGameStarter.AddModel(new StoryModeCutsceneSelectionModel());
		campaignGameStarter.AddModel(new StoryModeVoiceOverModel());
		campaignGameStarter.AddModel(new StoryModeIncidentModel());
	}
}
