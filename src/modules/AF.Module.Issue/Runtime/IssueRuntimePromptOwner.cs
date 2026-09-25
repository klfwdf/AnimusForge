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
	private static class IssueRuntimePromptOwner
	{
		internal static List<PostprocessRuleEntry> BuildRuntimePostprocessRules(Hero targetHero)
		{
			List<PostprocessRuleEntry> list = new List<PostprocessRuleEntry>();
			try
			{
				if (!IssueRuntimeStateOwner.TryGetRuntimeState(targetHero, out var stateKey, out var _, out var _))
				{
					return list;
				}
				List<PostprocessRuleEntry> guardrailRulePostprocessRules = AIConfigHandler.GetGuardrailRulePostprocessRules("vanilla_issue") ?? new List<PostprocessRuleEntry>();
				foreach (PostprocessRuleEntry item in guardrailRulePostprocessRules)
				{
					string text = (item?.Tag ?? "").Trim();
					if (string.IsNullOrWhiteSpace(text))
					{
						continue;
					}
					bool flag = false;
					switch ((stateKey ?? "").Trim().ToLowerInvariant())
					{
					case "offer":
						flag = text.Equals("[ACTION:ISSUE_ACCEPT_SELF]", StringComparison.OrdinalIgnoreCase) || text.StartsWith("[ACTION:ISSUE_ACCEPT_ALT:COMPANION=", StringComparison.OrdinalIgnoreCase);
						break;
					case "ready_to_turn_in":
						flag = text.Equals("[ACTION:QUEST_TURN_IN]", StringComparison.OrdinalIgnoreCase);
						break;
					}
					if (flag)
					{
						list.Add(new PostprocessRuleEntry
						{
							Tag = item.Tag,
							Description = item.Description
						});
					}
				}
			}
			catch
			{
			}
			return list;
		}

		internal static bool TryBuildRuntimePromptBlock(Hero targetHero, out string stateKey, out string promptText)
		{
			promptText = "";
			if (!IssueRuntimeStateOwner.TryGetRuntimeState(targetHero, out stateKey, out var issue, out var probe))
			{
				return false;
			}
			switch (stateKey)
			{
			case "offer":
				promptText = BuildOfferPromptBlock(targetHero, issue);
				break;
			case "ready_to_turn_in":
				promptText = BuildReadyToTurnInPromptBlock(targetHero, issue, probe);
				break;
			case "in_progress":
			case "in_progress_alternative":
				promptText = BuildInProgressPromptBlock(targetHero, issue);
				break;
			}
			return !string.IsNullOrWhiteSpace(promptText);
		}

		private static string BuildOfferPromptBlock(Hero targetHero, IssueBase issue)
		{
			string text = NormalizePromptText(GetText(issue.Title));
			string text2 = NormalizePromptText(GetText(issue.Description));
			string text3 = NormalizePromptText(GetText(issue.IssueBriefByIssueGiver));
			string text4 = NormalizePromptText(GetText(issue.IssueQuestSolutionExplanationByIssueGiver));
			bool flag = TryCheckQuestPreconditions(issue, targetHero, out var text5);
			bool flag2 = issue.IsThereAlternativeSolution;
			List<CompanionCandidate> list = new List<CompanionCandidate>();
			bool flag3 = false;
			string text6 = "";
			if (flag && flag2)
			{
				flag3 = TryBuildAlternativeCandidates(issue, out list, out text6);
			}
			else if (flag2)
			{
				flag3 = false;
				text6 = "你当前还不能把这项任务正式交给玩家，所以也不能进入同伴代办。";
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("【原版任务上下文（仅供理解，不要逐字照抄）】");
			stringBuilder.AppendLine("你当前有一项原版任务可向玩家委托");
			if (!string.IsNullOrWhiteSpace(text))
			{
				stringBuilder.AppendLine("任务标题：" + text);
			}
			if (!string.IsNullOrWhiteSpace(text2))
			{
				stringBuilder.AppendLine("任务摘要：" + text2);
			}
			if (!string.IsNullOrWhiteSpace(text3))
			{
				stringBuilder.AppendLine("你向玩家开口时的原版要点：" + text3);
			}
			if (!string.IsNullOrWhiteSpace(text4))
			{
				stringBuilder.AppendLine("原版对“玩家亲自去做”的补充说明：" + text4);
			}
			int issueRewardGold = GetIssueRewardGold(issue);
			if (issueRewardGold > 0)
			{
				stringBuilder.AppendLine("原版参考基础报酬：" + issueRewardGold + " 第纳尔。这里只是参考，不要求你逐字报数。");
			}
			if (flag)
			{
				stringBuilder.AppendLine("当前玩家满足接取条件。如果玩家明确同意接这个任务，你在正文里只需自然口头确认或答应即可，不要输出任何动作标签；是否真正接取，由后处理依据你的表态判断。");
			}
			else
			{
				stringBuilder.AppendLine("当前玩家还不满足这项任务的原版接取前提,请严词拒绝！");
				if (!string.IsNullOrWhiteSpace(text5))
				{
					stringBuilder.AppendLine("当前不能交付的原版原因：" + text5);
				}
			}
			if (flag2 && flag3)
			{
				stringBuilder.AppendLine("这项任务也支持“由玩家的一名同伴率队代办”。");
				stringBuilder.AppendLine("若玩家明确要求由同伴代办，并且明确指定了下列候选中的某一人，你在正文里只需自然口头同意由该同伴代办即可，不要输出任何动作标签；是否真正按该同伴代办，由后处理判断。");
				stringBuilder.AppendLine("若玩家只说“派个同伴去”但没有明确指名，你必须先追问，不得把事情当成已经定下。");
				stringBuilder.AppendLine("你只能从下列候选名单中选择，绝对不能编造新的同伴：");
				foreach (CompanionCandidate item in list)
				{
					stringBuilder.AppendLine(item.PromptLine);
				}
				stringBuilder.AppendLine("若后处理最终判定为同伴代办，系统会接着弹出原生派兵界面来选择随行士兵；你正文里只负责决定是否接受以及由谁带队。");
			}
			else
			{
			}
			return stringBuilder.ToString().Trim();
		}

		private static string BuildInProgressPromptBlock(Hero targetHero, IssueBase issue)
		{
			QuestBase issueQuest = issue?.IssueQuest;
			if (targetHero == null || issue == null || issueQuest == null || !issueQuest.IsOngoing)
			{
				return "";
			}
			string safeIssueTitle = GetSafeIssueTitle(issue);
			string text = NormalizePromptText(GetText(issueQuest.Title));
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("【原版任务上下文：进行中】");
			stringBuilder.AppendLine("你已经把一项原版任务交给了玩家。现在不要再像第一次那样重新发任务，也不要再输出任何接任务标签。");
			if (!string.IsNullOrWhiteSpace(safeIssueTitle))
			{
				stringBuilder.AppendLine("任务标题：" + safeIssueTitle);
			}
			if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, safeIssueTitle, StringComparison.Ordinal))
			{
				stringBuilder.AppendLine("原版任务名：" + text);
			}
			stringBuilder.AppendLine("当前状态：" + (issue.IsSolvingWithAlternative ? "玩家已委派同伴代办，如果玩家说类似任务已完成的话，那他就是在骗人" : "玩家亲自执行中,任务暂未完成，如果玩家说类似任务已完成的话，那他就是在骗人"));
			if (issue.IsSolvingWithAlternative)
			{
				stringBuilder.AppendLine("当前带队同伴：" + GetHeroName(issue.AlternativeSolutionHero));
				stringBuilder.AppendLine("原版预计总耗时：" + Math.Max(1, issue.GetTotalAlternativeSolutionDurationInDays()) + " 天。");
			}
			try
			{
				stringBuilder.AppendLine("原版任务截止时间：" + issueQuest.QuestDueTime.ToString());
			}
			catch
			{
			}
			AppendRecentJournalLines(stringBuilder, issueQuest.JournalEntries);
			stringBuilder.AppendLine("你现在应当根据玩家的话讨论进度、提醒要求、回答是否快办成，而不是重新介绍“要不要接任务”。");
			stringBuilder.AppendLine("正文严禁输出 [ACTION:ISSUE_ACCEPT_SELF]、[ACTION:ISSUE_ACCEPT_ALT:*] 或 [ACTION:QUEST_TURN_IN]；是否触发只交给后处理判断。");
			return stringBuilder.ToString().Trim();
		}

		private static string BuildReadyToTurnInPromptBlock(Hero targetHero, IssueBase issue, TurnInProbeResult probe)
		{
			QuestBase issueQuest = issue?.IssueQuest;
			if (targetHero == null || issue == null || issueQuest == null || probe == null)
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("【原版任务上下文：待交付】");
			stringBuilder.AppendLine("原版系统已确认：这项任务当前存在可执行的交付路径。现在不要再重新发布任务，而是把语气放在验收、确认结果、讨论完成情况上。");
			string safeIssueTitle = GetSafeIssueTitle(issue);
			if (!string.IsNullOrWhiteSpace(safeIssueTitle))
			{
				stringBuilder.AppendLine("任务标题：" + safeIssueTitle);
			}
			string text = NormalizePromptText(GetText(issueQuest.Title));
			if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, safeIssueTitle, StringComparison.Ordinal))
			{
				stringBuilder.AppendLine("原版任务名：" + text);
			}
			if (!string.IsNullOrWhiteSpace(probe.IntroText))
			{
				stringBuilder.AppendLine("你此刻原版 discuss 流里的开场语义：" + probe.IntroText);
			}
			if (!string.IsNullOrWhiteSpace(probe.ExplicitCompletionSummary))
			{
				stringBuilder.AppendLine("系统检测到的显式完成信号：" + probe.ExplicitCompletionSummary);
			}
			AppendRecentJournalLines(stringBuilder, issueQuest.JournalEntries);
			if (probe.VisibleOptions != null && probe.VisibleOptions.Count > 0)
			{
				stringBuilder.AppendLine("当前原版可见的玩家选项摘要：");
				foreach (string visibleOption in probe.VisibleOptions)
				{
					stringBuilder.AppendLine("- " + visibleOption);
				}
			}
			stringBuilder.AppendLine("玩家已经满足了任务完成条件。如果玩家说要交付任务、确认已经做完，或来让你验收，你在正文里只需自然确认验收完成或指出仍未完成，不要输出任何动作标签；是否真正交付，由后处理依据你的表态判断。");
			stringBuilder.AppendLine("如果玩家只是询问进度、还没有完成、或者表述不清，你就只按对话回应，不要把事情说成已经验收。");
			stringBuilder.AppendLine("正文严禁输出 [ACTION:ISSUE_ACCEPT_SELF]、[ACTION:ISSUE_ACCEPT_ALT:*] 或 [ACTION:QUEST_TURN_IN]；是否触发只交给后处理判断。");
			return stringBuilder.ToString().Trim();
		}

		private static string BuildRecentCompletionPromptBlock(Hero targetHero)
		{
			if (targetHero == null)
			{
				return "";
			}
			if (VanillaIssuePromptBehavior.Instance == null || !VanillaIssuePromptBehavior.Instance.TryGetRecentCompletionRecord(targetHero, out var questTitle, out var completionDetail, out var rewardGold, out var recentJournalEntries, consumeOnRead: true))
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("【原版任务上下文：刚完成】");
			stringBuilder.AppendLine("玩家最近刚完成了一项你交给他的原版任务。现在不要再重新发任务，也不要再输出任何接任务标签。");
			if (!string.IsNullOrWhiteSpace(questTitle))
			{
				stringBuilder.AppendLine("最近完成的任务：" + NormalizePromptText(questTitle));
			}
			if (!string.IsNullOrWhiteSpace(completionDetail))
			{
				stringBuilder.AppendLine("原版完成结果：" + completionDetail);
			}
			if (rewardGold > 0)
			{
				stringBuilder.AppendLine("原版奖励参考：" + rewardGold + " 第纳尔。");
			}
			if (recentJournalEntries != null && recentJournalEntries.Count > 0)
			{
				stringBuilder.AppendLine("最近的原版任务记录：");
				foreach (string recentJournalEntry in recentJournalEntries)
				{
					stringBuilder.AppendLine("- " + NormalizePromptText(recentJournalEntry));
				}
			}
			stringBuilder.AppendLine("这条“最近完成”事实只用于下一次与你的对话，表示那项旧任务已经有结果了。");
			stringBuilder.AppendLine("如果你现在还有别的原版任务可谈，可以继续谈新的任务；但不要把这项刚完成的旧任务当成还没完成，也不要把同一件旧任务重新发给玩家。");
			return stringBuilder.ToString().Trim();
		}

		internal static string BuildNoAvailableIssuePromptBlock(Hero targetHero)
		{
			if (targetHero == null)
			{
				return "";
			}
			string text = targetHero.Name?.ToString();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "你";
			}
			return $"【原版任务上下文：当前无任务】{text}当前没有任何可派发的原版任务。";
		}

	}
}
