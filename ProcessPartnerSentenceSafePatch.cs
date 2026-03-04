using System;
using System.Reflection;
using HarmonyLib;

namespace AnimusForge;

public static class ProcessPartnerSentenceSafePatch
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
				MethodInfo methodInfo = AccessTools.Method(type, "ProcessPartnerSentence");
				if (!(methodInfo == null))
				{
					Harmony harmony = new Harmony("AnimusForge.processpartnersentence.safety");
					HarmonyMethod finalizer = new HarmonyMethod(typeof(ProcessPartnerSentenceSafePatch).GetMethod("Finalizer", BindingFlags.Static | BindingFlags.Public));
					harmony.Patch(methodInfo, null, null, null, finalizer);
					_patched = true;
					Logger.LogTrace("System", "✅ ProcessPartnerSentenceSafePatch 已打补丁。");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ ProcessPartnerSentenceSafePatch 打补丁失败: " + ex.Message);
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
