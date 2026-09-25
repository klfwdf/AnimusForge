using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal static partial class VanillaIssueOfferBridge
{
	private static class IssueTurnInDecisionOwner
	{
		internal static TurnInProbeResult AnalyzeTurnInOptions(ConversationManager conversationManager, IssueBase issue, QuestBase quest)
		{
			TurnInProbeResult turnInProbeResult = new TurnInProbeResult
			{
				Issue = issue,
				Quest = quest,
				IntroText = NormalizePromptText(conversationManager.CurrentSentenceText)
			};
			List<ConversationSentenceOption> list = conversationManager.CurOptions ?? new List<ConversationSentenceOption>();
			List<ConversationSentence> list2 = SentencesField?.GetValue(conversationManager) as List<ConversationSentence> ?? new List<ConversationSentence>();
			int num = int.MinValue;
			foreach (ConversationSentenceOption item in list)
			{
				string text = NormalizePromptText(GetText(item.Text));
				if (!string.IsNullOrWhiteSpace(text))
				{
					turnInProbeResult.VisibleOptions.Add(text);
				}
				if (!item.IsClickable || item.SentenceNo < 0 || item.SentenceNo >= list2.Count)
				{
					continue;
				}
				ConversationSentence conversationSentence = list2[item.SentenceNo];
				int num2 = ScorePotentialTurnInOption(text, conversationSentence, list2);
				if (num2 > num)
				{
					num = num2;
					turnInProbeResult.SuccessOptionId = item.Id ?? "";
					turnInProbeResult.SuccessOptionText = text;
					turnInProbeResult.SuccessConsequenceName = conversationSentence.OnConsequence?.Method?.Name ?? "";
					turnInProbeResult.SuccessOptionScore = num2;
				}
			}
			turnInProbeResult.IsConfident = turnInProbeResult.SuccessOptionScore >= 120;
			return turnInProbeResult;
		}

		internal static int ScorePotentialTurnInOption(string optionText, ConversationSentence sentence, List<ConversationSentence> allSentences)
		{
			string text = (optionText ?? "").Trim();
			string text2 = sentence?.OnConsequence?.Method?.Name ?? "";
			string text3 = text.ToLowerInvariant();
			string text4 = text2.ToLowerInvariant();
			int num = 0;
			if (string.IsNullOrWhiteSpace(text))
			{
				num -= 20;
			}
			if (ContainsAny(text3, "not yet", "still", "different", "later", "back", "never mind", "another", "can't", "cannot", "haven't", "have not"))
			{
				num -= 220;
			}
			if (ContainsAny(text4, "fail", "cancel", "reject", "decline", "betray", "back", "go_back"))
			{
				num -= 260;
			}
			if (ContainsAny(text4, "success", "complete", "completed", "deliver", "delivered", "agreement", "paid", "return", "rescue", "captur"))
			{
				num += 140;
			}
			if (ContainsAny(text3, "i have", "i've", "here", "done", "finished", "completed", "brought", "brought you", "delivered", "captured", "rescued", "found", "paid", "deal", "took care"))
			{
				num += 95;
			}
			if (ContainsAny(text3, "thank you", "goodbye"))
			{
				num -= 40;
			}
			num += ScoreTurnInFollowUpLines(sentence, allSentences);
			return num;
		}

		private static int ScoreTurnInFollowUpLines(ConversationSentence sentence, List<ConversationSentence> allSentences)
		{
			if (sentence == null || allSentences == null || allSentences.Count == 0)
			{
				return 0;
			}
			int num = 0;
			for (int i = 0; i < allSentences.Count; i++)
			{
				ConversationSentence conversationSentence = allSentences[i];
				if (conversationSentence == null || conversationSentence.IsPlayer || conversationSentence.InputToken != sentence.OutputToken)
				{
					continue;
				}
				string text = NormalizePromptText(GetText(conversationSentence.Text)).ToLowerInvariant();
				string text2 = (conversationSentence.OnConsequence?.Method?.Name ?? "").ToLowerInvariant();
				if (ContainsAny(text2, "success", "complete", "completed", "deliver", "delivered", "agreement", "paid", "return", "rescue", "captur"))
				{
					num += 140;
				}
				if (ContainsAny(text, "thank you", "here is", "purse", "promised", "farewell", "reward", "payment"))
				{
					num += 70;
				}
			}
			return num;
		}

	}
}
