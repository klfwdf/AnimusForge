using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public static class SceneTauntLeaveMissionBlockPatch
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
			Harmony harmony = new Harmony("AnimusForge.scene.taunt.leavemissionblock");
			int num = 0;
			Type type = AccessTools.TypeByName("TaleWorlds.MountAndBlade.BasicLeaveMissionLogic");
			MethodInfo methodInfo = AccessTools.Method(type, "OnEndMissionRequest");
			MethodInfo method = typeof(SceneTauntLeaveMissionBlockPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
			if (type != null && methodInfo != null && method != null)
			{
				harmony.Patch(methodInfo, new HarmonyMethod(method));
				num++;
			}
			Type type2 = AccessTools.TypeByName("SandBox.Missions.MissionLogics.LeaveMissionLogic");
			MethodInfo methodInfo2 = AccessTools.Method(type2, "OnEndMissionRequest");
			if (type2 != null && methodInfo2 != null && method != null)
			{
				harmony.Patch(methodInfo2, new HarmonyMethod(method));
				num++;
			}
			_patched = num > 0;
			if (_patched)
			{
				Logger.LogTrace("System", $"✅ SceneTauntLeaveMissionBlockPatch 已打补丁。Patched={num}");
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ SceneTauntLeaveMissionBlockPatch 打补丁失败: " + ex.Message);
		}
	}

	public static bool Prefix(ref bool canPlayerLeave, ref InquiryData __result)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected O, but got Unknown
		try
		{
			if (!SceneTauntMissionBehavior.ShouldBlockSceneExitExternal(Mission.Current))
			{
				return true;
			}
			canPlayerLeave = false;
			__result = new InquiryData("无法离开", "这场冲突还没结束，不能离开场景。", false, true, "", "确定", (Action)null, (Action)null, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null);
			return false;
		}
		catch
		{
			return true;
		}
	}
}
