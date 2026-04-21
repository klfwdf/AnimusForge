using System;
using System.Reflection;
using HarmonyLib;
using SandBox.Missions.MissionLogics;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public static class SceneTauntFightAutoEndDelayPatch
{
	private static bool _patched;

	private static readonly FieldInfo FinishTimerField = AccessTools.Field(typeof(MissionFightHandler), "_finishTimer");

	public static void EnsurePatched()
	{
		if (_patched)
		{
			return;
		}
		try
		{
			MethodInfo methodInfo = AccessTools.Method(typeof(MissionFightHandler), "OnMissionTick");
			MethodInfo method = typeof(SceneTauntFightAutoEndDelayPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
			if (!(methodInfo == null) && !(method == null))
			{
				Harmony harmony = new Harmony("AnimusForge.scene.taunt.fightautodelay");
				harmony.Patch(methodInfo, new HarmonyMethod(method));
				_patched = true;
				Logger.LogTrace("System", "✅ SceneTauntFightAutoEndDelayPatch 已打补丁。");
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ SceneTauntFightAutoEndDelayPatch 打补丁失败: " + ex.Message);
		}
	}

	public static bool Prefix(MissionFightHandler __instance)
	{
		try
		{
			Mission val = ((__instance != null) ? ((MissionBehavior)__instance).Mission : null) ?? Mission.Current;
			if (!SceneTauntMissionBehavior.ShouldDelayNativeFightAutoEndLongExternal(val))
			{
				return true;
			}
			object obj = FinishTimerField?.GetValue(__instance);
			BasicMissionTimer val2 = (BasicMissionTimer)((obj is BasicMissionTimer) ? obj : null);
			if (__instance != null && val != null && val.CurrentTime > __instance.MinMissionEndTime && val2 != null && val2.ElapsedTime > 3600f)
			{
				FinishTimerField?.SetValue(__instance, null);
				__instance.EndFight(false);
			}
			return false;
		}
		catch
		{
			return true;
		}
	}
}
