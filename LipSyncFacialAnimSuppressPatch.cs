using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace AnimusForge;

public static class LipSyncFacialAnimSuppressPatch
{
	private static bool _patched;

	public static void EnsurePatched()
	{
		if (_patched)
		{
			return;
		}
		_patched = true;
		try
		{
			Type type = AccessTools.TypeByName("SandBox.Missions.MissionLogics.MissionFacialAnimationHandler");
			if (type == null)
			{
				Logger.LogTrace("System", "❌ LipSyncFacialAnimSuppressPatch: 找不到 MissionFacialAnimationHandler 类型。");
				return;
			}
			MethodInfo methodInfo = AccessTools.Method(type, "OnMissionTick");
			if (methodInfo == null)
			{
				Logger.LogTrace("System", "❌ LipSyncFacialAnimSuppressPatch: 找不到 OnMissionTick 方法。");
				return;
			}
			Harmony harmony = new Harmony("AnimusForge.lipsync.facialanimsuppress");
			HarmonyMethod prefix = new HarmonyMethod(typeof(LipSyncFacialAnimSuppressPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public));
			harmony.Patch(methodInfo, prefix);
			Logger.LogTrace("System", "✅ LipSyncFacialAnimSuppressPatch 已对 MissionFacialAnimationHandler.OnMissionTick 打补丁。");
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ LipSyncFacialAnimSuppressPatch 打补丁失败: " + ex.Message);
		}
	}

	public static bool Prefix()
	{
		try
		{
			ShoutBehavior currentInstance = ShoutBehavior.CurrentInstance;
			if (currentInstance == null)
			{
				return true;
			}
			HashSet<int> speakingAgentIndicesSnapshot = currentInstance.GetSpeakingAgentIndicesSnapshot();
			if (speakingAgentIndicesSnapshot == null || speakingAgentIndicesSnapshot.Count == 0)
			{
				return true;
			}
			return false;
		}
		catch
		{
			return true;
		}
	}
}
