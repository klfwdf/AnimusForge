using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.Localization;

namespace Voxforge;

[HarmonyPatch(typeof(CampaignGameStarter), "AddPlayerLine", new Type[]
{
	typeof(string),
	typeof(string),
	typeof(string),
	typeof(string),
	typeof(ConversationSentence.OnConditionDelegate),
	typeof(ConversationSentence.OnConsequenceDelegate),
	typeof(int),
	typeof(ConversationSentence.OnClickableConditionDelegate),
	typeof(ConversationSentence.OnPersuasionOptionDelegate)
})]
public static class Patch_Starter_AddPlayerLine_Filter
{
	public static bool Prefix(string id, string inputToken, string outputToken, string text)
	{
		if (inputToken == "hero_main_options" && !string.IsNullOrEmpty(text))
		{
			string text2 = text;
			try
			{
				text2 = new TextObject(text).ToString();
			}
			catch
			{
			}
			if (text2.Contains("我是来向你传达我的需求的") || text2.Contains("有件事我想和你谈谈") || text2.Contains("投降或者去死") || text2.Contains("投降或去死") || (text2.Contains("投降") && text2.Contains("去死")))
			{
				Logger.Log("Patch_Starter_AddPlayerLine_Filter", "屏蔽原版 hero_main_options 选项: Id=" + id + ", Text=" + text2);
				return false;
			}
		}
		return true;
	}
}
