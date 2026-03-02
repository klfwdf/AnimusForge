using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace Voxforge;

public static class MainAgentControllerSafePatch
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
			Type type = AccessTools.TypeByName("TaleWorlds.MountAndBlade.View.MissionViews.MissionMainAgentController");
			if (!(type == null))
			{
				MethodInfo methodInfo = AccessTools.Method(type, "OnPreMissionTick");
				if (!(methodInfo == null))
				{
					Harmony harmony = new Harmony("Voxforge.mainagentcontroller.safety");
					HarmonyMethod prefix = new HarmonyMethod(typeof(MainAgentControllerSafePatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public));
					HarmonyMethod finalizer = new HarmonyMethod(typeof(MainAgentControllerSafePatch).GetMethod("Finalizer", BindingFlags.Static | BindingFlags.Public));
					harmony.Patch(methodInfo, prefix, null, null, finalizer);
					_patched = true;
					Logger.LogTrace("System", "✅ MainAgentControllerSafePatch 已对 MissionMainAgentController.OnPreMissionTick 打补丁（含 Finalizer）。");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ MainAgentControllerSafePatch 打补丁失败: " + ex.Message);
		}
	}

	public static bool Prefix()
	{
		try
		{
			Mission current = Mission.Current;
			if (current == null)
			{
				return false;
			}
			bool flag = false;
			try
			{
				flag = current.MissionEnded;
			}
			catch
			{
			}
			if (flag)
			{
				bool flag2 = false;
				try
				{
					flag2 = LordEncounterBehavior.IsEncounterMeetingMissionActive || DuelBehavior.IsArenaMissionActive;
				}
				catch
				{
				}
				if (flag2)
				{
					return false;
				}
			}
			if (current.Scene == null)
			{
				return false;
			}
		}
		catch
		{
		}
		return true;
	}

	public static Exception Finalizer(Exception __exception)
	{
		if (__exception != null)
		{
			return null;
		}
		return null;
	}
}
