using System;
using System.Reflection;
using HarmonyLib;
using SandBox.View.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public static class MissionUiInterruptionPatch
{
	private static bool _patched;

	public static void EnsurePatched()
	{
		if (_patched)
		{
			return;
		}
		try
		{
			MethodInfo methodInfo = AccessTools.Method(typeof(MissionSingleplayerViewHandler), "OnMissionScreenTick");
			if (methodInfo == null)
			{
				Logger.LogTrace("System", "❌ MissionUiInterruptionPatch: 找不到 MissionSingleplayerViewHandler.OnMissionScreenTick。");
				return;
			}
			Harmony harmony = new Harmony("AnimusForge.missionui.interruption");
			HarmonyMethod prefix = new HarmonyMethod(typeof(MissionUiInterruptionPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public));
			harmony.Patch(methodInfo, prefix);
			_patched = true;
			Logger.LogTrace("System", "✅ MissionUiInterruptionPatch 已启用任务界面切换保护。");
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ MissionUiInterruptionPatch 打补丁失败: " + ex.Message);
		}
	}

	public static void Prefix(MissionSingleplayerViewHandler __instance)
	{
		try
		{
			Mission current = Mission.Current;
			if (current == null || __instance == null || current.MissionEnded || __instance.MissionScreen?.IsPhotoModeEnabled == true)
			{
				return;
			}
			IInputContext input = __instance.Input;
			if (input == null)
			{
				return;
			}
			string text = DetectUpcomingMissionUiOpen(current, input);
			if (!string.IsNullOrWhiteSpace(text))
			{
				ShoutBehavior.NotifyUiInterruption(text);
			}
		}
		catch
		{
		}
	}

	private static string DetectUpcomingMissionUiOpen(Mission mission, IInputContext input)
	{
		if (input.IsGameKeyPressed(38) && mission.IsInventoryAccessAllowed)
		{
			return "INVENTORY";
		}
		if (input.IsGameKeyPressed(42) && mission.IsQuestScreenAccessAllowed)
		{
			return "QUEST";
		}
		if (!input.IsControlDown() && input.IsGameKeyPressed(43) && mission.IsPartyWindowAccessAllowed)
		{
			return "PARTY";
		}
		if (input.IsGameKeyPressed(39) && mission.IsEncyclopediaWindowAccessAllowed)
		{
			return "ENCYCLOPEDIA";
		}
		if (input.IsGameKeyPressed(40) && mission.IsKingdomWindowAccessAllowed && Hero.MainHero?.MapFaction?.IsKingdomFaction == true)
		{
			return "KINGDOM";
		}
		if (input.IsGameKeyPressed(41) && mission.IsClanWindowAccessAllowed)
		{
			return "CLAN";
		}
		if (input.IsGameKeyPressed(37) && mission.IsCharacterWindowAccessAllowed)
		{
			return "CHARACTER";
		}
		if (input.IsGameKeyPressed(36) && mission.IsBannerWindowAccessAllowed && Campaign.Current?.IsBannerEditorEnabled == true)
		{
			return "BANNER";
		}
		return null;
	}
}
