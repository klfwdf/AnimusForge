using System;
using System.Reflection;
using HarmonyLib;
using SandBox.View.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;

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
			if (current == null || __instance == null || current.MissionEnded)
			{
				return;
			}
			MissionScreen missionScreen = ((MissionView)__instance).MissionScreen;
			if (missionScreen != null && missionScreen.IsPhotoModeEnabled)
			{
				return;
			}
			IInputContext input = ((MissionView)__instance).Input;
			if (input != null)
			{
				string text = DetectUpcomingMissionUiOpen(current, input);
				if (!string.IsNullOrWhiteSpace(text))
				{
					ShoutBehavior.NotifyUiInterruption(text);
				}
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
		if (input.IsGameKeyPressed(40) && mission.IsKingdomWindowAccessAllowed)
		{
			Hero mainHero = Hero.MainHero;
			if (mainHero != null)
			{
				IFaction mapFaction = mainHero.MapFaction;
				if (((mapFaction != null) ? new bool?(mapFaction.IsKingdomFaction) : ((bool?)null)) == true)
				{
					return "KINGDOM";
				}
			}
		}
		if (input.IsGameKeyPressed(41) && mission.IsClanWindowAccessAllowed)
		{
			return "CLAN";
		}
		if (input.IsGameKeyPressed(37) && mission.IsCharacterWindowAccessAllowed)
		{
			return "CHARACTER";
		}
		if (input.IsGameKeyPressed(36) && mission.IsBannerWindowAccessAllowed)
		{
			Campaign current = Campaign.Current;
			if (current != null && current.IsBannerEditorEnabled)
			{
				return "BANNER";
			}
		}
		return null;
	}
}
