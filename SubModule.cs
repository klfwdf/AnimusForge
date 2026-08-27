using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Bannerlord.UIExtenderEx;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public class SubModule : MBSubModuleBase
{
	// 此标记存于模块日志目录，独立于任何存档，用于跨游戏重启去重主界面欢迎弹窗。
	private const string InitialApiGuideNoticeMarkerFileName = ".initial_api_guide_notice_v1";

	private const string InitialApiGuideNoticeMarkerValue = "animusforge-main-menu-welcome-v1";

	private UIExtender _uiExtender;

	private static bool _uiExtenderInitialized;

	private bool _pendingInitialApiGuideNotice;

	private bool _initialApiGuideNoticeShown;

	private long _initialApiGuideNoticeAfterUtcTicks;

	public override void OnInitialState()
	{
		base.OnInitialState();
		MarkPendingInitialApiGuideNotice();
	}

	protected override void OnSubModuleLoad()
	{
		base.OnSubModuleLoad();
		SceneActionsIntegrationBoundary.InitializeRuntime();
		if (_uiExtenderInitialized)
		{
			return;
		}
		_uiExtenderInitialized = true;
		try
		{
			_uiExtender = UIExtender.Create("AnimusForge");
			if (_uiExtender != null)
			{
				_uiExtender.Register(typeof(SubModule).Assembly);
				_uiExtender.Enable();
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("SubModule", ">>> UIExtenderEx init failed: " + ex.Message);
			_uiExtenderInitialized = false;
		}
	}

	public override void OnConfigChanged()
	{
		base.OnConfigChanged();
		SceneActionsIntegrationBoundary.RefreshMcmOverrides();
	}
	public override void OnBeforeMissionBehaviorInitialize(Mission mission)
	{
		base.OnBeforeMissionBehaviorInitialize(mission);
		SceneActionsIntegrationBoundary.RegisterBeforeMissionInitialization(mission);
	}

	public override void OnMissionBehaviorInitialize(Mission mission)
	{
		base.OnMissionBehaviorInitialize(mission);
		SceneActionsIntegrationBoundary.VerifyMissionInitialization(mission);
	}

	protected override void OnSubModuleUnloaded()
	{
		SceneActionsIntegrationBoundary.ShutdownRuntime();
		base.OnSubModuleUnloaded();
	}

	protected override void OnBeforeInitialModuleScreenSetAsRoot()
	{
		base.OnBeforeInitialModuleScreenSetAsRoot();
		Logger.LogTrace("SubModule", "====== Game root screen is about to show, loading module data ======");
		try
		{
			Logger.LogTrace("SubModule", ">>> Applying Harmony patches...");
			Harmony harmony = new Harmony("com.AnimusForge.spy");
			try
			{
				PatchClassProcessor patchClassProcessor2 = harmony.CreateClassProcessor(typeof(Patch_TriggerMassiveHook));
				patchClassProcessor2.Patch();
			}
			catch (Exception ex2)
			{
				Logger.LogTrace("SubModule", ">>> Patch_TriggerMassiveHook failed: " + ex2.Message);
			}
			try
			{
				PatchClassProcessor patchClassProcessor3 = harmony.CreateClassProcessor(typeof(Patch_GlobalUI_Click));
				patchClassProcessor3.Patch();
			}
			catch (Exception ex3)
			{
				Logger.LogTrace("SubModule", ">>> Patch_GlobalUI_Click failed: " + ex3.Message);
			}
			try
			{
				AiErrorAnalysisInquiry.EnsurePatched(harmony);
			}
			catch (Exception ex3a)
			{
				Logger.LogTrace("SubModule", ">>> AI error analysis inquiry patch failed: " + ex3a.Message);
			}
			try
			{
				PatchClassProcessor patchClassProcessor4 = harmony.CreateClassProcessor(typeof(Patch_PlayerEncounter_Start));
				patchClassProcessor4.Patch();
			}
			catch (Exception ex4)
			{
				Logger.LogTrace("SubModule", ">>> Patch_PlayerEncounter_Start failed: " + ex4.Message);
			}
			try
			{
				PatchClassProcessor patchClassProcessor5 = harmony.CreateClassProcessor(typeof(Patch_GameMenu_ActivateGameMenu));
				patchClassProcessor5.Patch();
			}
			catch (Exception ex5)
			{
				Logger.LogTrace("SubModule", ">>> Patch_GameMenu_ActivateGameMenu failed: " + ex5.Message);
			}
			try
			{
				harmony.CreateClassProcessor(typeof(Patch_MenuHelper_EncounterAttackConsequence_RaidVillageRestore)).Patch();
			}
			catch (Exception ex5r)
			{
				Logger.LogTrace("SubModule", ">>> Raid village encounter restore patch failed: " + ex5r.Message);
			}
			try
			{
				harmony.CreateClassProcessor(typeof(Patch_NpcSurrender_SkipCapturedLordConversation)).Patch();
				harmony.CreateClassProcessor(typeof(Patch_NpcSurrender_SkipFreeOrCapturePrisonerHeroConversation)).Patch();
			}
			catch (Exception ex5n)
			{
				Logger.LogTrace("SubModule", ">>> NPC surrender hero capture conversation skip patches failed: " + ex5n.Message);
			}
			try
			{
				harmony.CreateClassProcessor(typeof(Patch_PrisonBreakRescue_RecordSuccess)).Patch();
			}
			catch (Exception ex5p)
			{
				Logger.LogTrace("SubModule", ">>> Prison break rescue record patch failed: " + ex5p.Message);
			}
			try
			{
				SiegeAftermathPatchBootstrap.Apply(harmony);
			}
			catch (Exception ex5a)
			{
				Logger.LogTrace("SubModule", ">>> Siege aftermath GCCZ bridge patches failed: " + ex5a.Message);
			}
			try
			{
				PatchClassProcessor patchClassProcessor6 = harmony.CreateClassProcessor(typeof(Patch_Meeting_SuppressDeclareWarAction));
				patchClassProcessor6.Patch();
			}
			catch (Exception ex6)
			{
				Logger.LogTrace("SubModule", ">>> Patch_Meeting_SuppressDeclareWarAction failed: " + ex6.Message);
			}
			try
			{
				PermanentAllianceGuard.RegisterHarmonyPatches(harmony);
			}
			catch (Exception permanentAllianceEx)
			{
				Logger.LogTrace("SubModule", ">>> Permanent alliance guard patches failed: " + permanentAllianceEx.Message);
			}
			try
			{
				PatchClassProcessor vassalageDeclareWarPatch = harmony.CreateClassProcessor(typeof(Patch_Vassalage_DeclareWarAction));
				vassalageDeclareWarPatch.Patch();
			}
			catch (Exception ex6a)
			{
				Logger.LogTrace("SubModule", ">>> Patch_Vassalage_DeclareWarAction failed: " + ex6a.Message);
			}
			try
			{
				PatchClassProcessor vassalageMakePeacePatch = harmony.CreateClassProcessor(typeof(Patch_Vassalage_MakePeaceAction));
				vassalageMakePeacePatch.Patch();
			}
			catch (Exception ex6b)
			{
				Logger.LogTrace("SubModule", ">>> Patch_Vassalage_MakePeaceAction failed: " + ex6b.Message);
			}
			try
			{
				NpcTributeVassalageBehavior.RegisterHarmonyPatches(harmony);
			}
			catch (Exception ex6b2)
			{
				Logger.LogTrace("SubModule", ">>> NpcTributeVassalage patches failed: " + ex6b2.Message);
			}
			try
			{
				AnimusForgeVassalageUiSprites.EnsurePatched(harmony);
			}
			catch (Exception ex6c)
			{
				Logger.LogTrace("SubModule", ">>> Vassalage UI sprite bootstrap failed: " + ex6c.Message);
			}
			try
			{
				AnimusForgeWeeklyReportUiSprites.EnsurePatched(harmony);
			}
			catch (Exception ex6d)
			{
				Logger.LogTrace("SubModule", ">>> Weekly report UI sprite bootstrap failed: " + ex6d.Message);
			}
			try
			{
				AnimusForgePlayerNotorietyUiSprites.EnsurePatched(harmony);
			}
			catch (Exception ex6e)
			{
				Logger.LogTrace("SubModule", ">>> Player notoriety UI sprite bootstrap failed: " + ex6e.Message);
			}
			try
			{
				AnimusForgePlayerRpForgeUiSprites.EnsurePatched(harmony);
			}
			catch (Exception ex6f)
			{
				Logger.LogTrace("SubModule", ">>> Player RP forge UI sprite bootstrap failed: " + ex6f.Message);
			}
			try
			{
				PatchClassProcessor patchClassProcessor7 = harmony.CreateClassProcessor(typeof(Patch_Meeting_SuppressChangeRelationAction));
				patchClassProcessor7.Patch();
			}
			catch (Exception ex7)
			{
				Logger.LogTrace("SubModule", ">>> Patch_Meeting_SuppressChangeRelationAction failed: " + ex7.Message);
			}
			try
			{
				PatchClassProcessor patchClassProcessor8 = harmony.CreateClassProcessor(typeof(Patch_Meeting_SuppressEncounterHostileAction));
				patchClassProcessor8.Patch();
			}
			catch (Exception ex8)
			{
				Logger.LogTrace("SubModule", ">>> Patch_Meeting_SuppressEncounterHostileAction failed: " + ex8.Message);
			}
			try
			{
				PlayerEncounterPropertySafePatch.EnsurePatched();
			}
			catch (Exception ex8a)
			{
				Logger.LogTrace("SubModule", ">>> PlayerEncounterPropertySafePatch init failed: " + ex8a.Message);
			}
			try
			{
				AgentVictoryRetreatNullTeamSafePatch.EnsurePatched();
			}
			catch (Exception ex8aa)
			{
				Logger.LogTrace("SubModule", ">>> AgentVictoryRetreatNullTeamSafePatch init failed: " + ex8aa.Message);
			}
			try
			{
				BattleObserverInspectionPrisonerSafePatch.EnsurePatched();
			}
			catch (Exception ex8aaa)
			{
				Logger.LogTrace("SubModule", ">>> BattleObserverInspectionPrisonerSafePatch init failed: " + ex8aaa.Message);
			}
			try
			{
				KingdomDecisionCleanupSafePatch.EnsurePatched(harmony);
			}
			catch (Exception ex8ab)
			{
				Logger.LogTrace("SubModule", ">>> KingdomDecisionCleanupSafePatch init failed: " + ex8ab.Message);
			}
			try
			{
				WorkshopDailyTickSafetyPatch.EnsurePatched(harmony);
			}
			catch (Exception ex8ac)
			{
				Logger.LogTrace("SubModule", ">>> WorkshopDailyTickSafetyPatch init failed: " + ex8ac.Message);
			}
			try
			{
				MapSceneTerrainTypeSafePatch.EnsurePatched(harmony);
			}
			catch (Exception ex8ad)
			{
				Logger.LogTrace("SubModule", ">>> MapSceneTerrainTypeSafePatch init failed: " + ex8ad.Message);
			}
			try
			{
				HeroClosestSettlementSafePatch.EnsurePatched(harmony);
			}
			catch (Exception ex8ae)
			{
				Logger.LogTrace("SubModule", ">>> HeroClosestSettlementSafePatch init failed: " + ex8ae.Message);
			}
			try
			{
				AnimusForgeMobilePartyAiSafetyPatch.EnsurePatched(harmony);
			}
			catch (Exception ex8af)
			{
				Logger.LogTrace("SubModule", ">>> AnimusForgeMobilePartyAiSafetyPatch init failed: " + ex8af.Message);
			}
			try
			{
				WorldMapGovernorExpeditionNativeLifecyclePatch.EnsurePatched(harmony);
			}
			catch (Exception ex8af2)
			{
				Logger.LogTrace("SubModule", ">>> Governor expedition native lifecycle patch failed: " + ex8af2.Message);
			}
			try
			{
				WorldMapOrderedArmySurvivalPatch.EnsurePatched(harmony);
			}
			catch (Exception ex8af3)
			{
				Logger.LogTrace("SubModule", ">>> Ordered army survival patch failed: " + ex8af3.Message);
			}
			try
			{
				CampaignTickDiagnosticsPatch.EnsurePatched(harmony);
			}
			catch (Exception ex8ag)
			{
				Logger.LogTrace("SubModule", ">>> CampaignTickDiagnosticsPatch init failed: " + ex8ag.Message);
			}
			try
			{
				Patch_Conversation_Start_Intercept.ManualPatch(harmony);
			}
			catch (Exception ex8b)
			{
				Logger.LogTrace("SubModule", ">>> Manual conversation start intercept patch failed: " + ex8b.Message);
			}
			try
			{
				PatchClassProcessor shoutTextInputFocusPatch = harmony.CreateClassProcessor(typeof(ShoutTextInputFocusChangePatch));
				shoutTextInputFocusPatch.Patch();
			}
			catch (Exception ex8c)
			{
				Logger.LogTrace("SubModule", ">>> ShoutTextInputFocusChangePatch failed: " + ex8c.Message);
			}
			try
			{
				Patch_ConversationManager_OpenMapConversation.ManualPatch(harmony);
			}
			catch (Exception ex9)
			{
				Logger.LogTrace("SubModule", ">>> Manual OpenMapConversation patch failed: " + ex9.Message);
			}
			try
			{
				Patch_ConversationManager_SetupAndStartMapConversation.ManualPatch(harmony);
			}
			catch (Exception ex10)
			{
				Logger.LogTrace("SubModule", ">>> Manual SetupAndStartMapConversation patch failed: " + ex10.Message);
			}
			try
			{
				ConversationVMCapturePatch.EnsurePatched();
			}
			catch (Exception ex10a)
			{
				Logger.LogTrace("SubModule", ">>> Conversation VM capture patch failed: " + ex10a.Message);
			}
			try
			{
				NativeConversationAnswerAreaController.EnsurePatched();
			}
			catch (Exception ex10b)
			{
				Logger.LogTrace("SubModule", ">>> Native conversation answer area patch failed: " + ex10b.Message);
			}
			try
			{
				PassageUsePointSafePatch.EnsurePatched();
			}
			catch (Exception ex11)
			{
				Logger.LogTrace("SubModule", ">>> PassageUsePointSafePatch init failed: " + ex11.Message);
			}
			try
			{
				SceneTauntWieldBlockPatch.EnsurePatched();
			}
			catch (Exception ex12)
			{
				Logger.LogTrace("SubModule", ">>> SceneTauntWieldBlockPatch init failed: " + ex12.Message);
			}
			try
			{
				SceneTauntMissionDifficultyPatch.EnsurePatched();
			}
			catch (Exception ex13)
			{
				Logger.LogTrace("SubModule", ">>> SceneTauntMissionDifficultyPatch init failed: " + ex13.Message);
			}
			try
			{
				SceneTauntNativeConversationBlockPatch.EnsurePatched();
			}
			catch (Exception ex14)
			{
				Logger.LogTrace("SubModule", ">>> SceneTauntNativeConversationBlockPatch init failed: " + ex14.Message);
			}
			try
			{
				SceneTauntLeaveMissionBlockPatch.EnsurePatched();
			}
			catch (Exception ex15)
			{
				Logger.LogTrace("SubModule", ">>> SceneTauntLeaveMissionBlockPatch init failed: " + ex15.Message);
			}
			try
			{
				SceneTauntFightAutoEndDelayPatch.EnsurePatched();
			}
			catch (Exception ex16)
			{
				Logger.LogTrace("SubModule", ">>> SceneTauntFightAutoEndDelayPatch init failed: " + ex16.Message);
			}
			try
			{
				BannerlordExceptionSentinel.Initialize(harmony);
			}
			catch (Exception ex17)
			{
				Logger.LogTrace("SubModule", ">>> BannerlordExceptionSentinel init failed: " + ex17.Message);
			}
			try
			{
				CraftingOrderLoadSafetyPatch.EnsurePatched(harmony);
			}
			catch (Exception ex17a)
			{
				Logger.LogTrace("SubModule", ">>> CraftingOrderLoadSafetyPatch init failed: " + ex17a.Message);
			}
			try
			{
				McmDropdownRuntimeRefresh.EnsurePatched();
			}
			catch (Exception ex18a)
			{
				Logger.LogTrace("SubModule", ">>> McmDropdownRuntimeRefresh init failed: " + ex18a.Message);
			}
			try
			{
				EncyclopediaHeroPersonaPatch.EnsurePatched(harmony);
			}
			catch (Exception ex18aa)
			{
				Logger.LogTrace("SubModule", ">>> EncyclopediaHeroPersonaPatch init failed: " + ex18aa.Message);
			}
			try
			{
				EncyclopediaTownRuleMemoryPatch.EnsurePatched(harmony);
			}
			catch (Exception ex18aab)
			{
				Logger.LogTrace("SubModule", ">>> EncyclopediaTownRuleMemoryPatch init failed: " + ex18aab.Message);
			}
			try
			{
				EncyclopediaKingdomStabilityPatch.EnsurePatched(harmony);
			}
			catch (Exception ex18aaa)
			{
				Logger.LogTrace("SubModule", ">>> EncyclopediaKingdomStabilityPatch init failed: " + ex18aaa.Message);
			}
			try
			{
				PlayerNotorietyCharacterDeveloperPatch.EnsurePatched(harmony);
			}
			catch (Exception ex18ab)
			{
				Logger.LogTrace("SubModule", ">>> PlayerNotorietyCharacterDeveloperPatch init failed: " + ex18ab.Message);
			}
			try
			{
				harmony.CreateClassProcessor(typeof(Patch_PlayerKingdomNameChange_RecordMaterials)).Patch();
			}
			catch (Exception ex18ac)
			{
				Logger.LogTrace("SubModule", ">>> Player kingdom rename material patch init failed: " + ex18ac.Message);
			}
			try
			{
				TroopInspectionBehavior.RegisterHarmonyPatches(harmony);
			}
			catch (Exception ex18b)
			{
				Logger.LogTrace("SubModule", ">>> TroopInspection patches init failed: " + ex18b.Message);
			}
			try
			{
				CastleAftermathConstructionSpeedPatchRegistrar.Register(harmony);
			}
			catch (Exception ex18bCastleConstruction)
			{
				Logger.LogTrace("SubModule", ">>> Castle aftermath construction patches init failed: " + ex18bCastleConstruction.Message);
			}
			try
			{
				SettlementEntryTroopSelectionBehavior.RegisterHarmonyPatches(harmony);
			}
			catch (Exception ex18ba)
			{
				Logger.LogTrace("SubModule", ">>> SETS patches init failed: " + ex18ba.Message);
			}
			try
			{
				MilitaryExerciseBehavior.RegisterHarmonyPatches(harmony);
			}
			catch (Exception ex18c)
			{
				Logger.LogTrace("SubModule", ">>> MilitaryExercise patches init failed: " + ex18c.Message);
			}
			try
			{
				DuelBehavior.RegisterHarmonyPatches(harmony);
			}
			catch (Exception ex18d0)
			{
				Logger.LogTrace("SubModule", ">>> DuelBehavior patches init failed: " + ex18d0.Message);
			}
			try
			{
				FourberieDuelCompatibility.EnsurePatched(harmony);
			}
			catch (Exception ex18d0Fourberie)
			{
				Logger.LogTrace("SubModule", ">>> Fourberie duel compatibility init failed: " + ex18d0Fourberie.Message);
			}
			try
			{
				CourierDeliveryBehavior.RegisterHarmonyPatches(harmony);
			}
			catch (Exception ex18d)
			{
				Logger.LogTrace("SubModule", ">>> CourierDelivery patches init failed: " + ex18d.Message);
			}
			try
			{
				WorldDiplomacyBehavior.RegisterHarmonyPatches(harmony);
			}
			catch (Exception worldDiplomacyPatchEx)
			{
				Logger.LogTrace("SubModule", ">>> WorldDiplomacy patches init failed: " + worldDiplomacyPatchEx.Message);
			}
			try
			{
				RewardSystemBehavior.RegisterHarmonyPatches(harmony);
			}
			catch (Exception ex18e)
			{
				Logger.LogTrace("SubModule", ">>> RewardSystem patches init failed: " + ex18e.Message);
			}
			try
			{
				NobleGatheringBehavior.RegisterHarmonyPatches(harmony);
			}
			catch (Exception ex18f)
			{
				Logger.LogTrace("SubModule", ">>> NobleGathering patches init failed: " + ex18f.Message);
			}
			try
			{
				SexualConceptionBehavior.RegisterHarmonyPatches(harmony);
			}
			catch (Exception ex18g)
			{
				Logger.LogTrace("SubModule", ">>> SexualConception patches init failed: " + ex18g.Message);
			}
			Logger.LogTrace("SubModule", ">>> Harmony patches applied.");
		}
		catch (Exception ex18)
		{
			Logger.LogTrace("SubModule", ">>> Harmony patch bootstrap failed: " + ex18);
		}
		AIConfigHandler.ReloadConfig();
		try
		{
			TtsEngine.Instance.Initialize();
			Logger.LogTrace("SubModule", ">>> Online TTS engine initialized.");
		}
		catch (Exception ex19)
		{
			Logger.LogTrace("SubModule", ">>> TTS engine initialization failed (non-fatal): " + ex19.Message);
		}
		try
		{
			CompatibilityAudit.RunStartupAudit();
		}
		catch (Exception ex20)
		{
			Logger.LogCompatibilityAudit("CompatAudit", "Startup compatibility audit failed: " + ex20.Message);
		}
	}

	protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
	{
		if (starterObject is CampaignGameStarter campaignGameStarter)
		{
			RegisterCourierFoodConsumptionModel(campaignGameStarter);
			RegisterCourierMobilePartyAiModel(campaignGameStarter);
			RegisterAnimusForgeSettlementAccessModel(campaignGameStarter);
			RegisterAnimusForgeSettlementLoyaltyModel(campaignGameStarter);
			campaignGameStarter.AddBehavior(new ModOnboardingBehavior());
			campaignGameStarter.AddBehavior(new MyBehavior());
			campaignGameStarter.AddBehavior(new KingdomStrategicProfileBehavior());
			campaignGameStarter.AddBehavior(new ShoutBehavior());
			campaignGameStarter.AddBehavior(new CourierDeliveryBehavior());
			campaignGameStarter.AddBehavior(new DuelBehavior());
			campaignGameStarter.AddBehavior(new RewardSystemBehavior());
			campaignGameStarter.AddBehavior(new PlayerNotorietyBehavior());
			campaignGameStarter.AddBehavior(new AnimusForgeTerminalBehavior());
			campaignGameStarter.AddBehavior(new AnimusForgeUniqueCosmeticItemBehavior());
			campaignGameStarter.AddBehavior(new CustomPolicyBehavior());
			campaignGameStarter.AddBehavior(new NpcRulerPolicyBehavior());
			campaignGameStarter.AddBehavior(new AnimusForgeWorldEventBehavior());
			campaignGameStarter.AddBehavior(new WorldMessageTimelineMenuBehavior());
			campaignGameStarter.AddBehavior(new ExpeditionParade.ExpeditionParadeCampaignBehavior());
			campaignGameStarter.AddBehavior(new RomanceSystemBehavior());
			campaignGameStarter.AddBehavior(new KnowledgeLibraryBehavior());
			campaignGameStarter.AddBehavior(new LordEncounterBehavior());
			campaignGameStarter.AddBehavior(new ProactiveNpcRequestBehavior());
			campaignGameStarter.AddBehavior(new CompanionProactiveChatBehavior());
			campaignGameStarter.AddBehavior(new SceneTauntBehavior());
			campaignGameStarter.AddBehavior(new GcczSettlementCulturePersistenceBehavior());
			campaignGameStarter.AddBehavior(new SiegeAiInterventionBehavior());
			campaignGameStarter.AddBehavior(new VillageAftermathBehavior());
			campaignGameStarter.AddBehavior(new SettlementEntryTroopSelectionBehavior());
			campaignGameStarter.AddBehavior(new NoblePrisonerEscortBehavior());
			campaignGameStarter.AddBehavior(new VoteDealBehavior());
			campaignGameStarter.AddBehavior(new WorldDiplomacyBehavior());
			campaignGameStarter.AddBehavior(new DiplomacyBehavior());
			campaignGameStarter.AddBehavior(new VanillaIssuePromptBehavior());
			campaignGameStarter.AddBehavior(new WorldMapPartyCommandBehavior());
			campaignGameStarter.AddBehavior(new NobleGatheringBehavior());
			campaignGameStarter.AddBehavior(new VassalageBehavior());
			campaignGameStarter.AddBehavior(new NpcTributeVassalageBehavior());
			campaignGameStarter.AddBehavior(new KingdomAnnexationBehavior());
		}
	}

	private static void RegisterAnimusForgeSettlementLoyaltyModel(CampaignGameStarter campaignGameStarter)
	{
		if (campaignGameStarter == null)
		{
			return;
		}
		try
		{
			SettlementLoyaltyModel inner = null;
			foreach (GameModel model in campaignGameStarter.Models)
			{
				if (model is SettlementLoyaltyModel loyaltyModel && !(loyaltyModel is AnimusForgeSettlementLoyaltyModel))
				{
					inner = loyaltyModel;
				}
			}
			inner ??= new DefaultSettlementLoyaltyModel();
			campaignGameStarter.AddModel<SettlementLoyaltyModel>(new AnimusForgeSettlementLoyaltyModel(inner));
			Logger.LogTrace("SubModule", ">>> AnimusForge settlement loyalty model registered.");
		}
		catch (Exception ex)
		{
			Logger.LogTrace("SubModule", ">>> AnimusForge settlement loyalty model registration failed: " + ex);
		}
	}

	private static void RegisterAnimusForgeSettlementAccessModel(CampaignGameStarter campaignGameStarter)
	{
		if (campaignGameStarter == null)
		{
			return;
		}
		try
		{
			SettlementAccessModel inner = null;
			foreach (GameModel model in campaignGameStarter.Models)
			{
				if (model is SettlementAccessModel accessModel && !(accessModel is AnimusForgeSettlementAccessModel))
				{
					inner = accessModel;
				}
			}
			inner ??= new DefaultSettlementAccessModel();
			campaignGameStarter.AddModel<SettlementAccessModel>(new AnimusForgeSettlementAccessModel(inner));
			Logger.LogTrace("SubModule", ">>> AnimusForge settlement access model registered.");
		}
		catch (Exception ex)
		{
			Logger.LogTrace("SubModule", ">>> AnimusForge settlement access model registration failed: " + ex);
		}
	}

	protected override void OnApplicationTick(float dt)
	{
		long perfFrame = PerfProbe.BeginFrame(dt);
		FreezeWatchdog.BeginFrame(dt);
		CampaignTickDiagnosticsPatch.RefreshCheckpointWriteBudget();
		try
		{
			if (!FreezeWatchdog.IsScopeRecordingActive() && !PerfProbe.IsDetailedScopeRecordingActive())
			{
				RunFastApplicationTickPhases();
			}
			else
			{
				RunWatchedApplicationTickPhases();
			}
		}
		catch (Exception ex)
		{
			FreezeWatchdog.Mark("SubModule.OnApplicationTick.exception", ex.GetType().Name + ": " + ex.Message, immediate: true);
			throw;

		}
		finally
		{
			using (FreezeWatchdog.Scope("SubModule.PerfProbe.EndFrame"))
			{
				PerfProbe.EndFrame(perfFrame, "SubModule.OnApplicationTick.total");
			}
			FreezeWatchdog.EndFrame();
		}
	}

	private void RunFastApplicationTickPhases()
	{
		ShoutTextInputPopup.ProcessDeferredCloseIfNeeded();
		ShoutTextInputPopup.CloseForSystemInterruptionIfNeeded();
		ShoutTextInputPopup.KeepMissionPausedIfOpen();
		DevWeeklyReportPopup.ProcessDeferredCloseIfNeeded();
		// RichText handlers queue layer changes so encyclopedia navigation never mutates layers during widget event dispatch.
		EncyclopediaEntityLinkNavigationCoordinator.ProcessPending();
		PlayerNotorietyPopup.ProcessDeferredCloseIfNeeded();
		PlayerRpForgePopup.ProcessDeferredCloseIfNeeded();
		AnimusForgeConversationHistoryLogPopup.OnApplicationTick();
		AnimusForgeNativeConversationOverlay.OnApplicationTick();
		AiErrorAnalysisInquiry.OnApplicationTick();
		ShoutBehavior.OnApplicationTickForMainThreadActionsExternal();
		NativeConversationAnswerAreaController.OnApplicationTick();
		ShoutBehavior.OnApplicationTickForNativeConversationTtsExternal();
		ConversationHelper.Tick();
		ProcessPendingInitialApiGuideNotice();
		Logger.OnApplicationTick();
		BannerlordExceptionSentinel.OnApplicationTick();
		McmDropdownRuntimeRefresh.OnApplicationTick();
		EncyclopediaHeroPersonaPatch.OnApplicationTick();
		EncyclopediaTownRuleMemoryPatch.OnApplicationTick();
		SiegeAiInterventionBehavior.OnEngineTickForExternal();
		ModOnboardingBehavior.Instance?.OnEngineTick();
		MyBehavior.Instance?.OnEngineTick();
		CourierDeliveryBehavior.Instance?.OnEngineTick();
		DuelBehavior.Instance?.OnEngineTick();
		RewardSystemBehavior.Instance?.OnEngineTick();
		LordEncounterBehavior.OnEngineTick();
		AnimusForgeTerminalBehavior.Instance?.OnEngineTick();
		CustomPolicyBehavior.Instance?.OnEngineTick();
		NpcRulerPolicyBehavior.Instance?.OnEngineTick();
		WorldDiplomacyBehavior.Instance?.OnEngineTick();
		PolicySystemUi.OnApplicationTick();
		NobleGatheringBehavior.Instance?.OnEngineTick();
		VassalageBehavior.Instance?.OnEngineTick();
	}

	private void RunWatchedApplicationTickPhases()
	{
			RunWatchedTickPhase("SubModule.ShoutTextInputPopup.ProcessDeferredCloseIfNeeded", () => ShoutTextInputPopup.ProcessDeferredCloseIfNeeded());
			RunWatchedTickPhase("SubModule.ShoutTextInputPopup.CloseForSystemInterruptionIfNeeded", () => ShoutTextInputPopup.CloseForSystemInterruptionIfNeeded());
			RunWatchedTickPhase("SubModule.ShoutTextInputPopup.KeepMissionPausedIfOpen", () => ShoutTextInputPopup.KeepMissionPausedIfOpen());
			RunWatchedTickPhase("SubModule.DevWeeklyReportPopup.ProcessDeferredCloseIfNeeded", () => DevWeeklyReportPopup.ProcessDeferredCloseIfNeeded());
			// Preserve the same next-frame RichText navigation ordering while detailed performance scopes are enabled.
			RunWatchedTickPhase("SubModule.EncyclopediaEntityLinkNavigationCoordinator.ProcessPending", () => EncyclopediaEntityLinkNavigationCoordinator.ProcessPending());
			RunWatchedTickPhase("SubModule.PlayerNotorietyPopup.ProcessDeferredCloseIfNeeded", () => PlayerNotorietyPopup.ProcessDeferredCloseIfNeeded());
			RunWatchedTickPhase("SubModule.PlayerRpForgePopup.ProcessDeferredCloseIfNeeded", () => PlayerRpForgePopup.ProcessDeferredCloseIfNeeded());
			RunWatchedTickPhase("SubModule.AnimusForgeConversationHistoryLogPopup.OnApplicationTick", () => AnimusForgeConversationHistoryLogPopup.OnApplicationTick());
			RunWatchedTickPhase("SubModule.AnimusForgeNativeConversationOverlay.OnApplicationTick", () => AnimusForgeNativeConversationOverlay.OnApplicationTick());
			RunWatchedTickPhase("SubModule.AiErrorAnalysisInquiry.OnApplicationTick", () => AiErrorAnalysisInquiry.OnApplicationTick());
			RunWatchedTickPhase("SubModule.ShoutBehavior.MainThreadActions.OnApplicationTick", () => ShoutBehavior.OnApplicationTickForMainThreadActionsExternal());
			RunWatchedTickPhase("SubModule.NativeConversationAnswerAreaController.OnApplicationTick", () => NativeConversationAnswerAreaController.OnApplicationTick());
			RunWatchedTickPhase("SubModule.ShoutBehavior.NativeConversationTts.OnApplicationTick", () => ShoutBehavior.OnApplicationTickForNativeConversationTtsExternal());
			RunWatchedTickPhase("SubModule.ConversationHelper.Tick", () => ConversationHelper.Tick());
			RunWatchedTickPhase("SubModule.ProcessPendingInitialApiGuideNotice", () => ProcessPendingInitialApiGuideNotice());
			RunWatchedTickPhase("SubModule.Logger.OnApplicationTick", () => Logger.OnApplicationTick());
			RunWatchedTickPhase("SubModule.BannerlordExceptionSentinel.OnApplicationTick", () => BannerlordExceptionSentinel.OnApplicationTick());
			RunWatchedTickPhase("SubModule.McmDropdownRuntimeRefresh.OnApplicationTick", () => McmDropdownRuntimeRefresh.OnApplicationTick());
			RunWatchedTickPhase("SubModule.EncyclopediaHeroPersonaPatch.OnApplicationTick", () => EncyclopediaHeroPersonaPatch.OnApplicationTick());
			RunWatchedTickPhase("SubModule.EncyclopediaTownRuleMemoryPatch.OnApplicationTick", () => EncyclopediaTownRuleMemoryPatch.OnApplicationTick());
			RunWatchedTickPhase("SubModule.SiegeAiInterventionBehavior.OnEngineTick", () => SiegeAiInterventionBehavior.OnEngineTickForExternal());
			RunWatchedTickPhase("SubModule.ModOnboardingBehavior.OnEngineTick", () => ModOnboardingBehavior.Instance?.OnEngineTick());
			RunWatchedTickPhase("SubModule.MyBehavior.OnEngineTick", () => MyBehavior.Instance?.OnEngineTick());
			RunWatchedTickPhase("SubModule.CourierDeliveryBehavior.OnEngineTick", () => CourierDeliveryBehavior.Instance?.OnEngineTick());
			RunWatchedTickPhase("SubModule.DuelBehavior.OnEngineTick", () => DuelBehavior.Instance?.OnEngineTick());
			RunWatchedTickPhase("SubModule.RewardSystemBehavior.OnEngineTick", () => RewardSystemBehavior.Instance?.OnEngineTick());
			RunWatchedTickPhase("SubModule.LordEncounterBehavior.OnEngineTick", () => LordEncounterBehavior.OnEngineTick());
			RunWatchedTickPhase("SubModule.AnimusForgeTerminalBehavior.OnEngineTick", () => AnimusForgeTerminalBehavior.Instance?.OnEngineTick());
			RunWatchedTickPhase("SubModule.CustomPolicyBehavior.OnEngineTick", () => CustomPolicyBehavior.Instance?.OnEngineTick());
			RunWatchedTickPhase("SubModule.NpcRulerPolicyBehavior.OnEngineTick", () => NpcRulerPolicyBehavior.Instance?.OnEngineTick());
			RunWatchedTickPhase("SubModule.WorldDiplomacyBehavior.OnEngineTick", () => WorldDiplomacyBehavior.Instance?.OnEngineTick());
			RunWatchedTickPhase("SubModule.PolicySystemUi.OnApplicationTick", () => PolicySystemUi.OnApplicationTick());
			RunWatchedTickPhase("SubModule.NobleGatheringBehavior.OnEngineTick", () => NobleGatheringBehavior.Instance?.OnEngineTick());
			RunWatchedTickPhase("SubModule.VassalageBehavior.OnEngineTick", () => VassalageBehavior.Instance?.OnEngineTick());
	}

	private static void RunWatchedTickPhase(string name, Action action)
	{
		if (!FreezeWatchdog.IsScopeRecordingActive() && !PerfProbe.IsDetailedScopeRecordingActive())
		{
			action?.Invoke();
			return;
		}
		using (FreezeWatchdog.Scope(name))
		using (PerfProbe.Scope(name))
		{
			action?.Invoke();
		}
	}

	private static void RegisterCourierFoodConsumptionModel(CampaignGameStarter campaignGameStarter)
	{
		if (campaignGameStarter == null)
		{
			return;
		}
		try
		{
			MobilePartyFoodConsumptionModel inner = null;
			foreach (GameModel model in campaignGameStarter.Models)
			{
				if (model is MobilePartyFoodConsumptionModel foodModel && !(foodModel is CourierFoodConsumptionModel))
				{
					inner = foodModel;
				}
			}
			inner ??= new DefaultMobilePartyFoodConsumptionModel();
			campaignGameStarter.AddModel<MobilePartyFoodConsumptionModel>(new CourierFoodConsumptionModel(inner));
			Logger.LogTrace("SubModule", ">>> Courier food consumption model registered.");
		}
		catch (Exception ex)
		{
			Logger.LogTrace("SubModule", ">>> Courier food consumption model registration failed: " + ex);
		}
	}

	private static void RegisterCourierMobilePartyAiModel(CampaignGameStarter campaignGameStarter)
	{
		if (campaignGameStarter == null)
		{
			return;
		}
		try
		{
			MobilePartyAIModel inner = null;
			foreach (GameModel model in campaignGameStarter.Models)
			{
				if (model is MobilePartyAIModel aiModel && !(aiModel is CourierMobilePartyAIModel))
				{
					inner = aiModel;
				}
			}
			inner ??= new DefaultMobilePartyAIModel();
			campaignGameStarter.AddModel<MobilePartyAIModel>(new CourierMobilePartyAIModel(inner));
			Logger.LogTrace("SubModule", ">>> Courier mobile party AI model registered.");
		}
		catch (Exception ex)
		{
			Logger.LogTrace("SubModule", ">>> Courier mobile party AI model registration failed: " + ex);
		}
	}

	private void MarkPendingInitialApiGuideNotice()
	{
		try
		{
			if (_initialApiGuideNoticeShown)
			{
				return;
			}
			_pendingInitialApiGuideNotice = true;
			_initialApiGuideNoticeAfterUtcTicks = DateTime.UtcNow.Ticks + TimeSpan.FromSeconds(1.0).Ticks;
		}
		catch
		{
		}
	}

	private void ProcessPendingInitialApiGuideNotice()
	{
		try
		{
			if (!_pendingInitialApiGuideNotice || _initialApiGuideNoticeShown || DateTime.UtcNow.Ticks < _initialApiGuideNoticeAfterUtcTicks)
			{
				return;
			}
			// 仅在启动延迟结束后读取一次标记；已展示过时不再创建或排队弹窗。
			if (HasInitialApiGuideNoticeMarker())
			{
				_pendingInitialApiGuideNotice = false;
				_initialApiGuideNoticeShown = true;
				return;
			}
			_pendingInitialApiGuideNotice = false;
			_initialApiGuideNoticeShown = true;
			InformationManager.ShowInquiry(new InquiryData("欢迎使用 AnimusForge", "若要配置 API 信息，你无需进入 MCM 页面；进入存档之后的首次引导会引导你填写 API 信息。", isAffirmativeOptionShown: true, isNegativeOptionShown: false, "知道了", "", null, null), pauseGameActiveState: false, prioritize: false);
			TryWriteInitialApiGuideNoticeMarker();
		}
		catch
		{
		}
	}

	private static bool HasInitialApiGuideNoticeMarker()
	{
		try
		{
			string markerPath = AnimusForgeModulePaths.GetLogFilePath(InitialApiGuideNoticeMarkerFileName);
			return File.Exists(markerPath) && string.Equals(File.ReadAllText(markerPath, Encoding.UTF8).Trim(), InitialApiGuideNoticeMarkerValue, StringComparison.Ordinal);
		}
		catch (Exception ex)
		{
			Logger.LogTrace("SubModule", ">>> Initial API guide marker read failed: " + ex.Message);
			return false;
		}
	}

	private static void TryWriteInitialApiGuideNoticeMarker()
	{
		try
		{
			string markerPath = AnimusForgeModulePaths.GetLogFilePath(InitialApiGuideNoticeMarkerFileName);
			string directoryName = Path.GetDirectoryName(markerPath);
			if (!string.IsNullOrWhiteSpace(directoryName) && !Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			File.WriteAllText(markerPath, InitialApiGuideNoticeMarkerValue, Encoding.UTF8);
		}
		catch (Exception ex)
		{
			Logger.LogTrace("SubModule", ">>> Initial API guide marker write failed: " + ex.Message);
		}
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("reload", "AnimusForge")]
	public static string CommandReloadConfig(List<string> strings)
	{
		AIConfigHandler.ReloadConfig();
		return "Config Reloaded Successfully!";
	}
}
