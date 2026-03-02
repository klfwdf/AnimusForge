using System;
using System.Reflection;
using HarmonyLib;

namespace Voxforge;

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
					Harmony harmony = new Harmony("Voxforge.continueconversation.safety");
					HarmonyMethod finalizer = new HarmonyMethod(typeof(ContinueConversationSafePatch).GetMethod("Finalizer", BindingFlags.Static | BindingFlags.Public));
					harmony.Patch(methodInfo, null, null, null, finalizer);
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

	public static Exception Finalizer(Exception __exception)
	{
		if (__exception != null)
		{
			return null;
		}
		return null;
	}
}
