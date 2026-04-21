using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public static class SceneTauntNativeConversationBlockPatch
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
			Harmony harmony = new Harmony("AnimusForge.scene.taunt.nativeconversationblock");
			int num = 0;
			Type type = AccessTools.TypeByName("SandBox.Conversation.MissionLogics.MissionConversationLogic");
			MethodInfo methodInfo = AccessTools.Method(type, "StartConversation", new Type[3]
			{
				typeof(Agent),
				typeof(bool),
				typeof(bool)
			});
			MethodInfo method = typeof(SceneTauntNativeConversationBlockPatch).GetMethod("StartConversationPrefix", BindingFlags.Static | BindingFlags.Public);
			if (type != null && methodInfo != null && method != null)
			{
				harmony.Patch(methodInfo, new HarmonyMethod(method));
				num++;
			}
			Type type2 = AccessTools.TypeByName("SandBox.Missions.MissionLogics.MissionAlleyHandler");
			MethodInfo methodInfo2 = AccessTools.Method(type2, "CheckAndTriggerConversationWithRivalThug");
			MethodInfo methodInfo3 = AccessTools.Method(type2, "StartCommonAreaBattle");
			MethodInfo method2 = typeof(SceneTauntNativeConversationBlockPatch).GetMethod("AlleyPrefix", BindingFlags.Static | BindingFlags.Public);
			if (type2 != null && methodInfo2 != null && method2 != null)
			{
				harmony.Patch(methodInfo2, new HarmonyMethod(method2));
				num++;
			}
			if (type2 != null && methodInfo3 != null && method2 != null)
			{
				harmony.Patch(methodInfo3, new HarmonyMethod(method2));
				num++;
			}
			_patched = num > 0;
			if (_patched)
			{
				Logger.LogTrace("System", $"✅ SceneTauntNativeConversationBlockPatch 已打补丁。Patched={num}");
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ SceneTauntNativeConversationBlockPatch 打补丁失败: " + ex.Message);
		}
	}

	public static bool StartConversationPrefix(object __instance, Agent agent)
	{
		try
		{
			Mission mission = ((agent != null) ? agent.Mission : null) ?? Mission.Current;
			if (!SceneTauntMissionBehavior.ShouldSuppressNativeMissionConversationExternal(mission))
			{
				return true;
			}
			LogBlockedConversation(agent, "native_start_conversation");
			return false;
		}
		catch
		{
			return true;
		}
	}

	public static bool AlleyPrefix()
	{
		try
		{
			if (!SceneTauntMissionBehavior.ShouldSuppressNativeMissionConversationExternal(Mission.Current))
			{
				return true;
			}
			LogBlockedConversation(null, "native_alley_flow");
			return false;
		}
		catch
		{
			return true;
		}
	}

	private static void LogBlockedConversation(Agent agent, string reason)
	{
		try
		{
			float applicationTime = Time.ApplicationTime;
			if (applicationTime - _lastLogTime > 1f)
			{
				_lastLogTime = applicationTime;
				Logger.Log("SceneTaunt", "Blocked native mission conversation/alley flow during SceneTaunt escalation. Reason=" + reason + ", Agent=" + ((agent != null) ? agent.Name : null));
			}
		}
		catch
		{
		}
	}
}
