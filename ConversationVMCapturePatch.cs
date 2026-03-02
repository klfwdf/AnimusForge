using System;
using System.Reflection;
using HarmonyLib;

namespace Voxforge;

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
		Harmony harmony = new Harmony("Voxforge.conversationvm.capture.v2");
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
				Logger.LogTrace("System", "\ud83d\udd0d ConversationVMCapturePatch: 找到类型 " + text);
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
							Logger.LogTrace("System", "✅ ConversationVMCapturePatch: 已 Patch 构造函数 " + type.FullName + ".ctor");
						}
						catch (Exception ex)
						{
							Logger.LogTrace("System", "❌ ConversationVMCapturePatch: Patch 构造函数失败: " + ex.Message);
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
						Logger.LogTrace("System", "✅ ConversationVMCapturePatch: 已 Patch " + type.FullName + ".Refresh");
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
							Logger.LogTrace("System", "✅ ConversationVMCapturePatch: 已 Patch " + type.FullName + "." + text2);
						}
					}
					catch
					{
					}
				}
			}
			catch (Exception ex2)
			{
				Logger.LogTrace("System", "❌ ConversationVMCapturePatch: 处理 VM 类型失败: " + ex2.Message);
			}
		}
		if (num > 0)
		{
			Logger.LogTrace("System", $"✅ ConversationVMCapturePatch 完成，共 Patched {num} 个方法/构造函数。");
		}
		else
		{
			Logger.LogTrace("System", "❌ ConversationVMCapturePatch: 未找到任何可 Patch 的对话 VM 类型。");
		}
	}

	public static void CtorPostfix(object __instance)
	{
		Logger.LogTrace("ConversationHelper", "\ud83c\udd95 VM 构造函数被调用: " + (__instance?.GetType()?.Name ?? "null"));
		ConversationHelper.SetCurrentVM(__instance);
	}

	public static void RefreshPostfix(object __instance)
	{
		ConversationHelper.SetCurrentVM(__instance);
		ConversationHelper.OnRefreshPostfix();
	}
}
