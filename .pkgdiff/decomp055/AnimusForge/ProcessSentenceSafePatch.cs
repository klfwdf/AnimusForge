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
					Logger.LogTrace("System", "✅ ProcessSentenceSafePatch 已对 ConversationManager.ProcessSentence 打补丁（含 Finalizer）。");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ ProcessSentenceSafePatch 打补丁失败: " + ex.Message);
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
