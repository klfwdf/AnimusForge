using System;
using System.Reflection;
using HarmonyLib;

namespace AnimusForge;

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
					Harmony harmony = new Harmony("AnimusForge.conversationmanager.safety");
					HarmonyMethod finalizer = new HarmonyMethod(typeof(ConversationManagerSafePatch).GetMethod("Finalizer", BindingFlags.Static | BindingFlags.Public));
					harmony.Patch(methodInfo, null, null, null, finalizer);
					_patched = true;
					Logger.LogTrace("System", "✅ ConversationManagerSafePatch Patched ConversationManager.UpdateSpeakerAndListenerAgents (including Finalizer).");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ ConversationManagerSafePatch patch failed: " + ex.Message);
		}
	}

	public static Exception Finalizer(Exception __exception, object __instance, MethodBase __originalMethod)
	{
		return ConversationExceptionGuard.Filter(__exception, __instance, "UpdateSpeakerAndListenerAgents", __originalMethod);
	}
}
