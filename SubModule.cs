using System;
using System.Collections.Generic;
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
				PatchClassProcessor patchClassProcessor6 = harmony.CreateClassProcessor(typeof(Patch_Meeting_SuppressDeclareWarAction));
				patchClassProcessor6.Patch();
			}
			catch (Exception ex6)
			{
				Logger.LogTrace("SubModule", ">>> Patch_Meeting_SuppressDeclareWarAction failed: " + ex6.Message);
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
				TroopInspectionBehavior.RegisterHarmonyPatches(harmony);
			}
			catch (Exception ex18b)
			{
				Logger.LogTrace("SubModule", ">>> TroopInspection patches init failed: " + ex18b.Message);
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
				CourierDeliveryBehavior.RegisterHarmonyPatches(harmony);
			}
			catch (Exception ex18d)
			{
				Logger.LogTrace("SubModule", ">>> CourierDelivery patches init failed: " + ex18d.Message);
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
			RegisterAnimusForgeSettlementLoyaltyModel(campaignGameStarter);
			campaignGameStarter.AddBehavior(new ModOnboardingBehavior());
			campaignGameStarter.AddBehavior(new MyBehavior());
			campaignGameStarter.AddBehavior(new ShoutBehavior());
			campaignGameStarter.AddBehavior(new CourierDeliveryBehavior());
			campaignGameStarter.AddBehavior(new DuelBehavior());
			campaignGameStarter.AddBehavior(new RewardSystemBehavior());
			campaignGameStarter.AddBehavior(new AnimusForgeTerminalBehavior());
			campaignGameStarter.AddBehavior(new RomanceSystemBehavior());
			campaignGameStarter.AddBehavior(new KnowledgeLibraryBehavior());
			campaignGameStarter.AddBehavior(new LordEncounterBehavior());
			campaignGameStarter.AddBehavior(new SceneTauntBehavior());
			campaignGameStarter.AddBehavior(new VoteDealBehavior());
			campaignGameStarter.AddBehavior(new VanillaIssuePromptBehavior());
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

	protected override void OnApplicationTick(float dt)
	{
		ShoutTextInputPopup.ProcessDeferredCloseIfNeeded();
		ShoutTextInputPopup.CloseForSystemInterruptionIfNeeded();
		ShoutTextInputPopup.KeepMissionPausedIfOpen();
		ProcessPendingInitialApiGuideNotice();
		Logger.OnApplicationTick();
		BannerlordExceptionSentinel.OnApplicationTick();
		McmDropdownRuntimeRefresh.OnApplicationTick();
		EncyclopediaHeroPersonaPatch.OnApplicationTick();
		ModOnboardingBehavior.Instance?.OnEngineTick();
		MyBehavior.Instance?.OnEngineTick();
		CourierDeliveryBehavior.Instance?.OnEngineTick();
		DuelBehavior.Instance?.OnEngineTick();
		AnimusForgeTerminalBehavior.Instance?.OnEngineTick();
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
			_pendingInitialApiGuideNotice = false;
			_initialApiGuideNoticeShown = true;
			InformationManager.DisplayMessage(new InformationMessage("欢迎使用 AnimusForge。若要配置 API 信息，你无需进入 MCM 页面；进入存档之后的首次引导会引导你填写 API 信息。", Colors.Yellow));
		}
		catch
		{
		}
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("reload", "AnimusForge")]
	public static string CommandReloadConfig(List<string> strings)
	{
		AIConfigHandler.ReloadConfig();
		return "Config Reloaded Successfully!";
	}
}
