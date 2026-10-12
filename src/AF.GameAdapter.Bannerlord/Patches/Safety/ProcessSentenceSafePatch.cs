using System;
using System.Reflection;
using HarmonyLib;

namespace AnimusForge;

public static class ProcessSentenceSafePatch
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
				MethodInfo methodInfo = AccessTools.Method(type, "ProcessSentence");
				if (!(methodInfo == null))
				{
					Harmony harmony = new Harmony("AnimusForge.processsentence.safety");
					HarmonyMethod finalizer = new HarmonyMethod(typeof(ProcessSentenceSafePatch).GetMethod("Finalizer", BindingFlags.Static | BindingFlags.Public));
					harmony.Patch(methodInfo, null, null, null, finalizer);
					_patched = true;
					Logger.LogTrace("System", "✅ ProcessSentenceSafePatch Patched ConversationManager.ProcessSentence (including Finalizer).");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ ProcessSentenceSafePatch patch failed: " + ex.Message);
		}
	}

	public static Exception Finalizer(Exception __exception, object __instance, MethodBase __originalMethod)
	{
		return ConversationExceptionGuard.Filter(__exception, __instance, "ProcessSentence", __originalMethod);
	}
}
