using System;
using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public class SubModule : MBSubModuleBase
{
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
				PatchClassProcessor patchClassProcessorClipboard = harmony.CreateClassProcessor(typeof(InputClipboardUnicodePatch));
				patchClassProcessorClipboard.Patch();
			}
			catch (Exception exClipboard)
			{
				Logger.LogTrace("SubModule", ">>> InputClipboardUnicodePatch failed: " + exClipboard.Message);
			}
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
	}

	protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
	{
		if (starterObject is CampaignGameStarter campaignGameStarter)
		{
			campaignGameStarter.AddBehavior(new ModOnboardingBehavior());
			campaignGameStarter.AddBehavior(new MyBehavior());
			campaignGameStarter.AddBehavior(new ShoutBehavior());
			campaignGameStarter.AddBehavior(new DuelBehavior());
			campaignGameStarter.AddBehavior(new RewardSystemBehavior());
			campaignGameStarter.AddBehavior(new AnimusForgeTerminalBehavior());
			campaignGameStarter.AddBehavior(new RomanceSystemBehavior());
			campaignGameStarter.AddBehavior(new KnowledgeLibraryBehavior());
			campaignGameStarter.AddBehavior(new LordEncounterBehavior());
			campaignGameStarter.AddBehavior(new SceneTauntBehavior());
			campaignGameStarter.AddBehavior(new VanillaIssuePromptBehavior());
		}
	}

	protected override void OnApplicationTick(float dt)
	{
		BannerlordExceptionSentinel.OnApplicationTick();
		ModOnboardingBehavior.Instance?.OnEngineTick();
		MyBehavior.Instance?.OnEngineTick();
		DuelBehavior.Instance?.OnEngineTick();
		AnimusForgeTerminalBehavior.Instance?.OnEngineTick();
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("reload", "AnimusForge")]
	public static string CommandReloadConfig(List<string> strings)
	{
		AIConfigHandler.ReloadConfig();
		return "Config Reloaded Successfully!";
	}
}
