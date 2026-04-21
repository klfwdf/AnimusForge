using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public static class MissionScreenSafePatch
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
			Type type = AccessTools.TypeByName("TaleWorlds.MountAndBlade.View.Screens.MissionScreen");
			if (type == null)
			{
				Logger.LogTrace("System", "❌ MissionScreenSafePatch: 找不到 MissionScreen 类型。");
				return;
			}
			MethodInfo methodInfo = null;
			try
			{
				MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				MethodInfo[] array = methods;
				MethodInfo[] array2 = array;
				foreach (MethodInfo methodInfo2 in array2)
				{
					if (methodInfo2.Name.Contains("BeforeMissionTick"))
					{
						ParameterInfo[] parameters = methodInfo2.GetParameters();
						if (parameters.Length == 2 && parameters[0].ParameterType.FullName == "TaleWorlds.MountAndBlade.Mission" && parameters[1].ParameterType == typeof(float))
						{
							methodInfo = methodInfo2;
							break;
						}
					}
				}
			}
			catch
			{
			}
			if (methodInfo == null)
			{
				Logger.LogTrace("System", "❌ MissionScreenSafePatch: 未能找到 BeforeMissionTick 目标方法。");
				return;
			}
			Harmony harmony = new Harmony("AnimusForge.missionscreen.safety");
			HarmonyMethod prefix = new HarmonyMethod(typeof(MissionScreenSafePatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public));
			harmony.Patch(methodInfo, prefix);
			_patched = true;
			Logger.LogTrace("System", "✅ MissionScreenSafePatch 已对 " + methodInfo.DeclaringType.FullName + "." + methodInfo.Name + " 打补丁。");
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ MissionScreenSafePatch 打补丁失败: " + ex.Message);
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
		}
		catch
		{
		}
		return true;
	}
}
