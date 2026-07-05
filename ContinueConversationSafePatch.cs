using System;
using System.Reflection;
using HarmonyLib;

namespace AnimusForge;

public static class ContinueConversationSafePatch
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
				MethodInfo methodInfo = AccessTools.Method(type, "ContinueConversation");
				if (!(methodInfo == null))
				{
					Harmony harmony = new Harmony("AnimusForge.continueconversation.safety");
					HarmonyMethod prefix = new HarmonyMethod(typeof(ContinueConversationSafePatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public));
					HarmonyMethod finalizer = new HarmonyMethod(typeof(ContinueConversationSafePatch).GetMethod("Finalizer", BindingFlags.Static | BindingFlags.Public));
					harmony.Patch(methodInfo, prefix, null, null, finalizer);
					_patched = true;
					Logger.LogTrace("System", "✅ ContinueConversationSafePatch 已打补丁。");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ ContinueConversationSafePatch 打补丁失败: " + ex.Message);
		}
	}

	public static bool Prefix(object __instance, MethodBase __originalMethod)
	{
		return !ConversationExceptionGuard.TryPreemptStaleConversation(__instance, "ContinueConversation", __originalMethod);
	}

	public static Exception Finalizer(Exception __exception, object __instance, MethodBase __originalMethod)
	{
		return ConversationExceptionGuard.Filter(__exception, __instance, "ContinueConversation", __originalMethod);
	}
}
