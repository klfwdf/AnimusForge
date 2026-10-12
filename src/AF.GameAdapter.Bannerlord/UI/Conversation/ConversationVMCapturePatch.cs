using System;
using System.Reflection;
using HarmonyLib;

namespace AnimusForge;

public static class ConversationVMCapturePatch
{
	private static bool _patched;

	private static readonly string[] _vmTypeNames = new string[4] { "TaleWorlds.CampaignSystem.ViewModelCollection.Conversation.MissionConversationVM", "TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapConversation.MapConversationVM", "TaleWorlds.CampaignSystem.ViewModelCollection.MissionConversationVM", "TaleWorlds.CampaignSystem.ViewModelCollection.MapConversationVM" };

	public static void EnsurePatched()
	{
		if (_patched)
		{
			return;
		}
		_patched = true;
		Harmony harmony = new Harmony("AnimusForge.conversationvm.capture.v2");
		int num = 0;
		string[] vmTypeNames = _vmTypeNames;
		foreach (string text in vmTypeNames)
		{
			try
			{
				Type type = AccessTools.TypeByName(text);
				if (type == null)
				{
					continue;
				}
				Logger.LogTrace("System", "\ud83d\udd0d ConversationVMCapturePatch: Found type " + text);
				try
				{
					ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					ConstructorInfo[] array = constructors;
					foreach (ConstructorInfo original in array)
					{
						try
						{
							HarmonyMethod postfix = new HarmonyMethod(typeof(ConversationVMCapturePatch).GetMethod("CtorPostfix", BindingFlags.Static | BindingFlags.Public));
							harmony.Patch(original, null, postfix);
							num++;
							Logger.LogTrace("System", "✅ ConversationVMCapturePatch: Patched constructor " + type.FullName + ".ctor");
						}
						catch (Exception ex)
						{
							Logger.LogTrace("System", "❌ ConversationVMCapturePatch: Constructor patch failed: " + ex.Message);
						}
					}
				}
				catch
				{
				}
				try
				{
					MethodInfo methodInfo = AccessTools.Method(type, "Refresh");
					if (methodInfo != null)
					{
						HarmonyMethod postfix2 = new HarmonyMethod(typeof(ConversationVMCapturePatch).GetMethod("RefreshPostfix", BindingFlags.Static | BindingFlags.Public));
						harmony.Patch(methodInfo, null, postfix2);
						num++;
						Logger.LogTrace("System", "✅ ConversationVMCapturePatch: Patched " + type.FullName + ".Refresh");
					}
				}
				catch
				{
				}
				string[] array2 = new string[3] { "OnConversationContinue", "ExecuteContinue", "RefreshValues" };
				foreach (string text2 in array2)
				{
					try
					{
						MethodInfo methodInfo2 = AccessTools.Method(type, text2);
						if (methodInfo2 != null)
						{
							HarmonyMethod postfix3 = new HarmonyMethod(typeof(ConversationVMCapturePatch).GetMethod("RefreshPostfix", BindingFlags.Static | BindingFlags.Public));
							harmony.Patch(methodInfo2, null, postfix3);
							num++;
							Logger.LogTrace("System", "✅ ConversationVMCapturePatch: Patched " + type.FullName + "." + text2);
						}
					}
					catch
					{
					}
				}
			}
			catch (Exception ex2)
			{
				Logger.LogTrace("System", "❌ ConversationVMCapturePatch: Failed to process VM type: " + ex2.Message);
			}
		}
		if (num > 0)
		{
			Logger.LogTrace("System", $"✅ ConversationVMCapturePatch completed; patched {num} methods/constructors.");
		}
		else
		{
			Logger.LogTrace("System", "❌ ConversationVMCapturePatch: No patchable conversation VM types found.");
		}
	}

	public static void CtorPostfix(object __instance)
	{
		Logger.LogTrace("ConversationHelper", "\ud83c\udd95 VM constructor called: " + (__instance?.GetType()?.Name ?? "null"));
		ConversationHelper.SetCurrentVM(__instance);
	}

	public static void RefreshPostfix(object __instance)
	{
		ConversationHelper.SetCurrentVM(__instance);
		ConversationHelper.OnRefreshPostfix();
	}
}
