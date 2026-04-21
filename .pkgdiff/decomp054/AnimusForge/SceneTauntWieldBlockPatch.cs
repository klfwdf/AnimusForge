using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public static class SceneTauntWieldBlockPatch
{
	private static bool _patched;

	private static float _lastLogTime;

	public static void EnsurePatched()
	{
		if (_patched)
		{
			return;
		}
		try
		{
			Type typeFromHandle = typeof(Agent);
			MethodInfo method = typeof(SceneTauntWieldBlockPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
			if (typeFromHandle == null || method == null)
			{
				return;
			}
			Harmony harmony = new Harmony("AnimusForge.scene.taunt.wieldblock");
			int num = 0;
			MethodInfo[] methods = typeFromHandle.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo methodInfo in methods)
			{
				if (methodInfo == null)
				{
					continue;
				}
				string name = methodInfo.Name;
				if (!(name != "TryToWieldWeaponInSlot") || !(name != "TryToWieldWeaponInHand") || !(name != "WieldInitialWeapons"))
				{
					try
					{
						harmony.Patch(methodInfo, new HarmonyMethod(method));
						num++;
					}
					catch
					{
					}
				}
			}
			_patched = num > 0;
			if (_patched)
			{
				Logger.LogTrace("System", $"✅ SceneTauntWieldBlockPatch 已打补丁。Patched={num}");
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ SceneTauntWieldBlockPatch 打补丁失败: " + ex.Message);
		}
	}

	public static bool Prefix(Agent __instance)
	{
		try
		{
			if (!SceneTauntMissionBehavior.ShouldBlockAgentWeaponWieldExternal(__instance))
			{
				return true;
			}
			float applicationTime = Time.ApplicationTime;
			if (applicationTime - _lastLogTime > 1f)
			{
				_lastLogTime = applicationTime;
				Logger.Log("SceneTaunt", "Blocked AI wield attempt during unarmed scene conflict.");
			}
			return false;
		}
		catch
		{
			return true;
		}
	}
}
