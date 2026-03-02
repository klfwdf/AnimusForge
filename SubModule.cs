using System;
using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Voxforge;

public class SubModule : MBSubModuleBase
{
	protected override void OnBeforeInitialModuleScreenSetAsRoot()
	{
		base.OnBeforeInitialModuleScreenSetAsRoot();
		Logger.LogTrace("SubModule", "====== 游戏主界面即将显示，开始加载模组数据 ======");
		try
		{
			Logger.LogTrace("SubModule", ">>> 正在尝试激活 Harmony 补丁...");
			Harmony harmony = new Harmony("com.Voxforge.spy");
			try
			{
				PatchClassProcessor patchClassProcessor = harmony.CreateClassProcessor(typeof(Patch_Starter_AddPlayerLine_Filter));
				patchClassProcessor.Patch();
			}
			catch (Exception ex)
			{
				Logger.LogTrace("SubModule", ">>> ⚠\ufe0f Patch_Starter_AddPlayerLine_Filter 失败: " + ex.Message);
			}
			try
			{
				PatchClassProcessor patchClassProcessor2 = harmony.CreateClassProcessor(typeof(Patch_TriggerMassiveHook));
				patchClassProcessor2.Patch();
			}
			catch (Exception ex2)
			{
				Logger.LogTrace("SubModule", ">>> ⚠\ufe0f Patch_TriggerMassiveHook 失败: " + ex2.Message);
			}
			try
			{
				PatchClassProcessor patchClassProcessor3 = harmony.CreateClassProcessor(typeof(Patch_GlobalUI_Click));
				patchClassProcessor3.Patch();
			}
			catch (Exception ex3)
			{
				Logger.LogTrace("SubModule", ">>> ⚠\ufe0f Patch_GlobalUI_Click 失败: " + ex3.Message);
			}
			try
			{
				PatchClassProcessor patchClassProcessor4 = harmony.CreateClassProcessor(typeof(Patch_Conversation_Start_Intercept));
				patchClassProcessor4.Patch();
			}
			catch (Exception ex4)
			{
				Logger.LogTrace("SubModule", ">>> ⚠\ufe0f Patch_Conversation_Start_Intercept 失败: " + ex4.Message);
			}
			try
			{
				PatchClassProcessor patchClassProcessor5 = harmony.CreateClassProcessor(typeof(Patch_PlayerEncounter_Start));
				patchClassProcessor5.Patch();
			}
			catch (Exception ex5)
			{
				Logger.LogTrace("SubModule", ">>> ⚠\ufe0f Patch_PlayerEncounter_Start 失败: " + ex5.Message);
			}
			try
			{
				PatchClassProcessor patchClassProcessor6 = harmony.CreateClassProcessor(typeof(Patch_GameMenu_ActivateGameMenu));
				patchClassProcessor6.Patch();
			}
			catch (Exception ex6)
			{
				Logger.LogTrace("SubModule", ">>> ⚠\ufe0f Patch_GameMenu_ActivateGameMenu 失败: " + ex6.Message);
			}
			try
			{
				PatchClassProcessor patchClassProcessor7 = harmony.CreateClassProcessor(typeof(Patch_Meeting_SuppressDeclareWarAction));
				patchClassProcessor7.Patch();
			}
			catch (Exception ex7)
			{
				Logger.LogTrace("SubModule", ">>> ⚠\ufe0f Patch_Meeting_SuppressDeclareWarAction 失败: " + ex7.Message);
			}
			try
			{
				PatchClassProcessor patchClassProcessor8 = harmony.CreateClassProcessor(typeof(Patch_Meeting_SuppressChangeRelationAction));
				patchClassProcessor8.Patch();
			}
			catch (Exception ex8)
			{
				Logger.LogTrace("SubModule", ">>> ⚠\ufe0f Patch_Meeting_SuppressChangeRelationAction 失败: " + ex8.Message);
			}
			try
			{
				PatchClassProcessor patchClassProcessor9 = harmony.CreateClassProcessor(typeof(Patch_Meeting_SuppressEncounterHostileAction));
				patchClassProcessor9.Patch();
			}
			catch (Exception ex9)
			{
				Logger.LogTrace("SubModule", ">>> ⚠\ufe0f Patch_Meeting_SuppressEncounterHostileAction 失败: " + ex9.Message);
			}
			try
			{
				Patch_ConversationManager_OpenMapConversation.ManualPatch(harmony);
			}
			catch (Exception ex10)
			{
				Logger.LogTrace("SubModule", ">>> ⚠\ufe0f 手动注册 OpenMapConversation 失败: " + ex10.Message);
			}
			try
			{
				Patch_ConversationManager_SetupAndStartMapConversation.ManualPatch(harmony);
			}
			catch (Exception ex11)
			{
				Logger.LogTrace("SubModule", ">>> ⚠\ufe0f 手动注册 SetupAndStartMapConversation 失败: " + ex11.Message);
			}
			Logger.LogTrace("SubModule", ">>> ✅ Harmony 补丁激活成功！(手动 Patch 完成)");
		}
		catch (Exception ex12)
		{
			Logger.LogTrace("SubModule", ">>> ❌ Harmony 激活失败: " + ex12.ToString());
		}
		AIConfigHandler.ReloadConfig();
		try
		{
			TtsEngine.Instance.Initialize();
			Logger.LogTrace("SubModule", ">>> ✅ 在线 TTS 引擎初始化完成（后台工作线程已启动）");
		}
		catch (Exception ex13)
		{
			Logger.LogTrace("SubModule", ">>> ⚠\ufe0f TTS 引擎初始化失败（非致命）: " + ex13.Message);
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
			campaignGameStarter.AddBehavior(new KnowledgeLibraryBehavior());
			campaignGameStarter.AddBehavior(new LordEncounterBehavior());
		}
	}

	protected override void OnApplicationTick(float dt)
	{
		DuelBehavior.Instance?.OnEngineTick();
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("reload", "Voxforge")]
	public static string CommandReloadConfig(List<string> strings)
	{
		AIConfigHandler.ReloadConfig();
		return "Config Reloaded Successfully!";
	}
}
