using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public static class ConversationCameraSafePatch
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
			Type type = AccessTools.TypeByName("SandBox.View.Missions.MissionConversationCameraView");
			if (type == null)
			{
				Logger.LogTrace("System", "❌ ConversationCameraSafePatch: 找不到 MissionConversationCameraView 类型。");
				return;
			}
			Harmony harmony = new Harmony("AnimusForge.conversationcamera.safety");
			HarmonyMethod prefix = new HarmonyMethod(typeof(ConversationCameraSafePatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public));
			MethodInfo methodInfo = AccessTools.Method(type, "UpdateAgentLooksForConversation");
			if (methodInfo != null)
			{
				harmony.Patch(methodInfo, prefix);
			}
			MethodInfo methodInfo2 = AccessTools.Method(type, "SetFocusedObjectForCameraFocus");
			if (methodInfo2 != null)
			{
				harmony.Patch(methodInfo2, prefix);
			}
			_patched = true;
			Logger.LogTrace("System", "✅ ConversationCameraSafePatch 已对 MissionConversationCameraView 相关方法打补丁。");
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ ConversationCameraSafePatch 打补丁失败: " + ex.Message);
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
			if (current.Scene == null || current.Agents == null)
			{
				return false;
			}
			ConversationManager conversationManager = Campaign.Current?.ConversationManager;
			if (conversationManager == null || conversationManager.OneToOneConversationAgent == null)
			{
				return false;
			}
		}
		catch
		{
		}
		return true;
	}
}
