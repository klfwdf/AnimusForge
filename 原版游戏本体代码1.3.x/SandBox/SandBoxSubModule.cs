using System;
using SandBox.AI;
using SandBox.CampaignBehaviors;
using SandBox.GameComponents;
using SandBox.Issues;
using SandBox.Objects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Load;

namespace SandBox
{
	// Token: 0x02000028 RID: 40
	public class SandBoxSubModule : MBSubModuleBase
	{
		// Token: 0x0600012A RID: 298 RVA: 0x00007D36 File Offset: 0x00005F36
		protected override void OnSubModuleLoad()
		{
			base.OnSubModuleLoad();
			Module.CurrentModule.SetEditorMissionTester(new SandBoxEditorMissionTester());
			TauntUsageManager.Initialize();
		}

		// Token: 0x0600012B RID: 299 RVA: 0x00007D54 File Offset: 0x00005F54
		protected override void InitializeGameStarter(Game game, IGameStarter gameStarterObject)
		{
			if (game.GameType is Campaign)
			{
				gameStarterObject.AddModel<AgentStatCalculateModel>(new SandboxAgentStatCalculateModel());
				gameStarterObject.AddModel<StrikeMagnitudeCalculationModel>(new SandboxStrikeMagnitudeModel());
				gameStarterObject.AddModel<AgentApplyDamageModel>(new SandboxAgentApplyDamageModel());
				gameStarterObject.AddModel<MissionDifficultyModel>(new SandboxMissionDifficultyModel());
				gameStarterObject.AddModel<ApplyWeatherEffectsModel>(new SandboxApplyWeatherEffectsModel());
				gameStarterObject.AddModel<AutoBlockModel>(new SandboxAutoBlockModel());
				gameStarterObject.AddModel<AgentDecideKilledOrUnconsciousModel>(new SandboxAgentDecideKilledOrUnconsciousModel());
				gameStarterObject.AddModel<BattleBannerBearersModel>(new SandboxBattleBannerBearersModel());
				gameStarterObject.AddModel<FormationArrangementModel>(new DefaultFormationArrangementModel());
				gameStarterObject.AddModel<BattleMoraleModel>(new SandboxBattleMoraleModel());
				gameStarterObject.AddModel<BattleInitializationModel>(new SandboxBattleInitializationModel());
				gameStarterObject.AddModel<BattleSpawnModel>(new SandboxBattleSpawnModel());
				gameStarterObject.AddModel<DamageParticleModel>(new DefaultDamageParticleModel());
				gameStarterObject.AddModel<ItemPickupModel>(new DefaultItemPickupModel());
				gameStarterObject.AddModel<MissionSiegeEngineCalculationModel>(new DefaultSiegeEngineCalculationModel());
				CampaignGameStarter campaignGameStarter = gameStarterObject as CampaignGameStarter;
				if (campaignGameStarter != null)
				{
					campaignGameStarter.AddBehavior(new HideoutConversationsCampaignBehavior());
					campaignGameStarter.AddBehavior(new AlleyCampaignBehavior());
					campaignGameStarter.AddBehavior(new CommonTownsfolkCampaignBehavior());
					campaignGameStarter.AddBehavior(new CompanionDismissCampaignBehavior());
					campaignGameStarter.AddBehavior(new DefaultNotificationsCampaignBehavior());
					campaignGameStarter.AddBehavior(new ClanMemberRolesCampaignBehavior());
					campaignGameStarter.AddBehavior(new PrisonBreakCampaignBehavior());
					campaignGameStarter.AddBehavior(new GuardsCampaignBehavior());
					campaignGameStarter.AddBehavior(new SettlementMusiciansCampaignBehavior());
					campaignGameStarter.AddBehavior(new BoardGameCampaignBehavior());
					campaignGameStarter.AddBehavior(new TradersCampaignBehavior());
					campaignGameStarter.AddBehavior(new ArenaMasterCampaignBehavior());
					campaignGameStarter.AddBehavior(new CommonVillagersCampaignBehavior());
					campaignGameStarter.AddBehavior(new HeirSelectionCampaignBehavior());
					campaignGameStarter.AddBehavior(new DefaultCutscenesCampaignBehavior());
					campaignGameStarter.AddBehavior(new RivalGangMovingInIssueBehavior());
					campaignGameStarter.AddBehavior(new RuralNotableInnAndOutIssueBehavior());
					campaignGameStarter.AddBehavior(new FamilyFeudIssueBehavior());
					campaignGameStarter.AddBehavior(new NotableWantsDaughterFoundIssueBehavior());
					campaignGameStarter.AddBehavior(new TheSpyPartyIssueQuestBehavior());
					campaignGameStarter.AddBehavior(new ProdigalSonIssueBehavior());
					campaignGameStarter.AddBehavior(new BarberCampaignBehavior());
					campaignGameStarter.AddBehavior(new SnareTheWealthyIssueBehavior());
					campaignGameStarter.AddBehavior(new RetirementCampaignBehavior());
					campaignGameStarter.AddBehavior(new StatisticsCampaignBehavior());
					campaignGameStarter.AddBehavior(new DumpIntegrityCampaignBehavior());
					campaignGameStarter.AddBehavior(new CheckpointCampaignBehavior());
					campaignGameStarter.AddBehavior(new StealthCharactersCampaignBehavior());
					campaignGameStarter.AddBehavior(new TavernEmployeesCampaignBehavior());
					campaignGameStarter.AddBehavior(new TownMerchantsCampaignBehavior());
					campaignGameStarter.AddBehavior(new RecruitmentAgentSpawnBehavior());
				}
			}
		}

		// Token: 0x0600012C RID: 300 RVA: 0x00007F78 File Offset: 0x00006178
		public override void OnCampaignStart(Game game, object starterObject)
		{
			Campaign campaign = game.GameType as Campaign;
			if (campaign != null)
			{
				SandBoxManager sandBoxManager = campaign.SandBoxManager;
				sandBoxManager.SandBoxMissionManager = new SandBoxMissionManager();
				sandBoxManager.AgentBehaviorManager = new AgentBehaviorManager();
				sandBoxManager.SandBoxSaveManager = new SandBoxSaveManager();
			}
		}

		// Token: 0x0600012D RID: 301 RVA: 0x00007FBC File Offset: 0x000061BC
		private void OnRegisterTypes()
		{
			MBObjectManager.Instance.RegisterType<InstrumentData>("MusicInstrument", "MusicInstruments", 54U, true, false);
			MBObjectManager.Instance.RegisterType<SettlementMusicData>("MusicTrack", "MusicTracks", 55U, true, false);
			new DefaultMusicInstrumentData();
			MBObjectManager.Instance.LoadXML("MusicInstruments", false);
			MBObjectManager.Instance.LoadXML("MusicTracks", false);
		}

		// Token: 0x0600012E RID: 302 RVA: 0x00008020 File Offset: 0x00006220
		public override void OnGameInitializationFinished(Game game)
		{
			Campaign campaign = game.GameType as Campaign;
			if (campaign != null)
			{
				campaign.CampaignMissionManager = new CampaignMissionManager();
				campaign.MapSceneCreator = new MapSceneCreator();
				campaign.EncyclopediaManager.CreateEncyclopediaPages();
				this.OnRegisterTypes();
			}
		}

		// Token: 0x0600012F RID: 303 RVA: 0x00008063 File Offset: 0x00006263
		public override void RegisterSubModuleObjects(bool isSavedCampaign)
		{
			Campaign.Current.SandBoxManager.InitializeSandboxXMLs(isSavedCampaign);
		}

		// Token: 0x06000130 RID: 304 RVA: 0x00008075 File Offset: 0x00006275
		public override void AfterRegisterSubModuleObjects(bool isSavedCampaign)
		{
			Campaign.Current.SandBoxManager.InitializeCharactersAfterLoad(isSavedCampaign);
		}

		// Token: 0x06000131 RID: 305 RVA: 0x00008087 File Offset: 0x00006287
		private void StartGame(LoadResult loadResult)
		{
			MBGameManager.StartNewGame(new SandBoxGameManager(loadResult));
			MouseManager.ShowCursor(false);
		}

		// Token: 0x06000132 RID: 306 RVA: 0x0000809C File Offset: 0x0000629C
		public override void OnGameLoaded(Game game, object starterObject)
		{
			Campaign campaign = game.GameType as Campaign;
			if (campaign != null)
			{
				SandBoxManager sandBoxManager = campaign.SandBoxManager;
				sandBoxManager.SandBoxMissionManager = new SandBoxMissionManager();
				sandBoxManager.AgentBehaviorManager = new AgentBehaviorManager();
				sandBoxManager.SandBoxSaveManager = new SandBoxSaveManager();
			}
		}

		// Token: 0x06000133 RID: 307 RVA: 0x000080DE File Offset: 0x000062DE
		protected override void OnBeforeInitialModuleScreenSetAsRoot()
		{
			base.OnBeforeInitialModuleScreenSetAsRoot();
			if (!this._initialized)
			{
				MBSaveLoad.Initialize(Module.CurrentModule.GlobalTextManager);
				this._initialized = true;
			}
		}

		// Token: 0x06000134 RID: 308 RVA: 0x00008104 File Offset: 0x00006304
		public override void OnConfigChanged()
		{
			if (Campaign.Current != null)
			{
				CampaignEventDispatcher.Instance.OnConfigChanged();
			}
		}

		// Token: 0x06000135 RID: 309 RVA: 0x00008117 File Offset: 0x00006317
		protected override void OnNewModuleLoad()
		{
			SaveManager.InitializeGlobalDefinitionContext();
		}

		// Token: 0x0400005C RID: 92
		private bool _initialized;
	}
}
