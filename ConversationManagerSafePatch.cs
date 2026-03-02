using System;
using System.Reflection;
using HarmonyLib;

namespace Voxforge;

public static class ConversationManagerSafePatch
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
			Type type = AccessTools.TypeByName("TaleWorlds.CampaignSystem.Conversation.ConversationManager");
			if (!(type == null))
			{
				MethodInfo methodInfo = AccessTools.Method(type, "UpdateSpeakerAndListenerAgents");
				if (!(methodInfo == null))
				{
					Harmony harmony = new Harmony("Voxforge.conversationmanager.safety");
					HarmonyMethod finalizer = new HarmonyMethod(typeof(ConversationManagerSafePatch).GetMethod("Finalizer", BindingFlags.Static | BindingFlags.Public));
					harmony.Patch(methodInfo, null, null, null, finalizer);
					_patched = true;
					Logger.LogTrace("System", "✅ ConversationManagerSafePatch 已对 ConversationManager.UpdateSpeakerAndListenerAgents 打补丁（含 Finalizer）。");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ ConversationManagerSafePatch 打补丁失败: " + ex.Message);
		}
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
