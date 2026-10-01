using System;
using System.Reflection;
using HarmonyLib;

namespace AnimusForge;

public static class PassageUsePointSafePatch
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
			Type type = AccessTools.TypeByName("SandBox.Objects.PassageUsePoint");
			if (!(type == null))
			{
				MethodInfo methodInfo = AccessTools.Method(type, "AfterMissionStart");
				MethodInfo methodInfo2 = AccessTools.Method(type, "OnUse", new Type[2]
				{
					typeof(TaleWorlds.MountAndBlade.Agent),
					typeof(sbyte)
				});
				Harmony harmony = new Harmony("AnimusForge.passageusepoint.safety");
				if (!(methodInfo == null))
				{
					HarmonyMethod prefix = new HarmonyMethod(typeof(PassageUsePointSafePatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public));
					harmony.Patch(methodInfo, prefix);
				}
				if (!(methodInfo2 == null))
				{
					HarmonyMethod prefix2 = new HarmonyMethod(typeof(PassageUsePointSafePatch).GetMethod("OnUsePrefix", BindingFlags.Static | BindingFlags.Public));
					harmony.Patch(methodInfo2, prefix2);
				}
				_patched = methodInfo != null || methodInfo2 != null;
				if (_patched)
				{
					Logger.LogTrace("System", "✅ PassageUsePointSafePatch 已对 PassageUsePoint 打补丁。");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ PassageUsePointSafePatch 打补丁失败: " + ex.Message);
		}
	}

	public static bool Prefix()
	{
		try
		{
			if (DuelBehavior.IsArenaMissionActive)
			{
				return false;
			}
		}
		catch
		{
		}
		return true;
	}

	public static void OnUsePrefix(object __instance, TaleWorlds.MountAndBlade.Agent userAgent)
	{
		try
		{
			if (userAgent == null || !userAgent.IsMainAgent || __instance == null)
			{
				return;
			}
			PropertyInfo property = __instance.GetType().GetProperty("IsMissionExit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			bool flag = property != null && property.GetValue(__instance) is bool value && value;
			if (flag)
			{
				SceneTauntBehavior.ClearArmedCarryoverForExternal("player_used_scene_exit");
			}
		}
		catch
		{
		}
	}
}
