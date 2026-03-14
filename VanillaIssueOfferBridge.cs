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

internal static class VanillaIssueOfferBridge
{
	private sealed class CompanionCandidate
	{
		public Hero Hero;

		public string HeroId;

		public string PromptLine;
	}

	private sealed class PendingAlternativeDispatch
	{
		public Hero Giver;

		public Hero Companion;

		public IssueBase Issue;
	}

	private sealed class TurnInProbeResult
	{
		public IssueBase Issue;

		public QuestBase Quest;

		public string IntroText;

		public string ExplicitCompletionSummary;

		public string SuccessOptionId;

		public string SuccessOptionText;

		public string SuccessConsequenceName;

		public int SuccessOptionScore;

		public bool IsConfident;

		public List<string> VisibleOptions = new List<string>();
	}

	private sealed class ConversationManagerSnapshot
	{
		public List<ConversationSentence> Sentences;

		public Dictionary<string, int> StateMap;

		public int NumberOfStateIndices;

		public int AutoId;

		public int AutoToken;

		public HashSet<int> UsedIndices;

		public int ActiveToken;

		public int CurrentSentence;

		public TextObject CurrentSentenceText;

		public object LastSelectedDialogObject;

		public int CurrentRepeatedDialogSetIndex;

		public int CurrentRepeatIndex;

		public List<List<object>> DialogRepeatObjects;

		public List<TextObject> DialogRepeatLines;

		public bool IsActive;

		public int LastSelectedButtonIndex;

		public object MainAgent;

		public object SpeakerAgent;

		public object ListenerAgent;

		public List<object> ConversationAgents;

		public MobileParty ConversationParty;

		public List<ConversationSentenceOption> CurOptions;
	}

	private static readonly Regex IssueAcceptSelfRegex = new Regex("\\[ACTION:ISSUE_ACCEPT_SELF\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex IssueAcceptAltRegex = new Regex("\\[ACTION:ISSUE_ACCEPT_ALT:COMPANION=([^\\]\\r\\n]+)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex QuestTurnInRegex = new Regex("\\[ACTION:QUEST_TURN_IN\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly MethodInfo CheckPreconditionsMethod = AccessTools.Method(typeof(IssueBase), "CheckPreconditions");

	private static readonly PropertyInfo RewardGoldProperty = typeof(IssueBase).GetProperty("RewardGold", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

	private static readonly FieldInfo SentencesField = AccessTools.Field(typeof(ConversationManager), "_sentences");

	private static readonly FieldInfo StateMapField = AccessTools.Field(typeof(ConversationManager), "stateMap");

	private static readonly FieldInfo NumberOfStateIndicesField = AccessTools.Field(typeof(ConversationManager), "_numberOfStateIndices");

	private static readonly FieldInfo AutoIdField = AccessTools.Field(typeof(ConversationManager), "_autoId");

	private static readonly FieldInfo AutoTokenField = AccessTools.Field(typeof(ConversationManager), "_autoToken");

	private static readonly FieldInfo UsedIndicesField = AccessTools.Field(typeof(ConversationManager), "_usedIndices");

	private static readonly FieldInfo CurrentSentenceField = AccessTools.Field(typeof(ConversationManager), "_currentSentence");

	private static readonly FieldInfo CurrentSentenceTextField = AccessTools.Field(typeof(ConversationManager), "_currentSentenceText");

	private static readonly FieldInfo LastSelectedDialogObjectField = AccessTools.Field(typeof(ConversationManager), "_lastSelectedDialogObject");

	private static readonly FieldInfo CurrentRepeatedDialogSetIndexField = AccessTools.Field(typeof(ConversationManager), "_currentRepeatedDialogSetIndex");

	private static readonly FieldInfo CurrentRepeatIndexField = AccessTools.Field(typeof(ConversationManager), "_currentRepeatIndex");

	private static readonly FieldInfo DialogRepeatObjectsField = AccessTools.Field(typeof(ConversationManager), "_dialogRepeatObjects");

	private static readonly FieldInfo DialogRepeatLinesField = AccessTools.Field(typeof(ConversationManager), "_dialogRepeatLines");

	private static readonly FieldInfo IsActiveField = AccessTools.Field(typeof(ConversationManager), "_isActive");

	private static readonly FieldInfo MainAgentField = AccessTools.Field(typeof(ConversationManager), "_mainAgent");

	private static readonly FieldInfo SpeakerAgentField = AccessTools.Field(typeof(ConversationManager), "_speakerAgent");

	private static readonly FieldInfo ListenerAgentField = AccessTools.Field(typeof(ConversationManager), "_listenerAgent");

	private static readonly FieldInfo ConversationAgentsField = AccessTools.Field(typeof(ConversationManager), "_conversationAgents");

	private static readonly FieldInfo ConversationPartyField = AccessTools.Field(typeof(ConversationManager), "_conversationParty");

	private static readonly PropertyInfo CurOptionsProperty = AccessTools.Property(typeof(ConversationManager), "CurOptions");

	private static readonly MethodInfo ProcessPartnerSentenceMethod = AccessTools.Method(typeof(ConversationManager), "ProcessPartnerSentence");

	private static readonly MethodInfo ProcessSentenceMethod = AccessTools.Method(typeof(ConversationManager), "ProcessSentence");

	private static readonly MethodInfo ResetRepeatedDialogSystemMethod = AccessTools.Method(typeof(ConversationManager), "ResetRepeatedDialogSystem");

	private static readonly FieldInfo DiscussDialogFlowField = AccessTools.Field(typeof(QuestBase), "DiscussDialogFlow");

	private static PendingAlternativeDispatch _pendingAlternativeDispatch;

	public static string BuildPromptBlock(Hero targetHero)
	{
		if (TryGetOfferableIssue(targetHero, out var issue))
		{
			return BuildOfferPromptBlock(targetHero, issue);
		}
		if (TryGetReadyToTurnInIssue(targetHero, out var issue2, out var probe))
		{
			return BuildReadyToTurnInPromptBlock(targetHero, issue2, probe);
		}
		if (TryGetInProgressIssue(targetHero, out var issue3))
		{
			return BuildInProgressPromptBlock(targetHero, issue3);
		}
		return BuildRecentCompletionPromptBlock(targetHero);
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
			stringBuilder.AppendLine("当前玩家满足这项任务的原版接取前提。如果玩家同意接这个任务，你必须在回复末尾附加标签 [ACTION:ISSUE_ACCEPT_SELF]。");
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
			stringBuilder.AppendLine("若玩家明确要求由同伴代办，并且明确指定了下列候选中的某一人，你才可以在回复末尾附加隐藏标签 [ACTION:ISSUE_ACCEPT_ALT:COMPANION=<HeroId>]。");
			stringBuilder.AppendLine("若玩家只说“派个同伴去”但没有明确指名，你必须先追问，不得输出任何同伴代办标签。");
			stringBuilder.AppendLine("你只能从下列候选名单中选择，绝对不能编造新的同伴：");
			foreach (CompanionCandidate item in list)
			{
				stringBuilder.AppendLine(item.PromptLine);
			}
			stringBuilder.AppendLine("当你输出同伴代办标签后，系统会接着弹出原生派兵界面来选择随行士兵；你只负责决定是否接受以及由谁带队。");
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
		stringBuilder.AppendLine("严禁输出 [ACTION:ISSUE_ACCEPT_SELF] 或 [ACTION:ISSUE_ACCEPT_ALT:*]。");
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
		stringBuilder.AppendLine("玩家已经满足了任务完成条件，如果玩家说要任务已经做完，或者要交付任务之类的，你必须在回复尾部输出 [ACTION:QUEST_TURN_IN]。使得任务实际完成。输出之后，会自动向玩家发放奖励，请不要自行转账！");
		stringBuilder.AppendLine("如果玩家只是询问进度、还没有完成、或者表述不清，请不要附加该标签。");
		stringBuilder.AppendLine("严禁输出 [ACTION:ISSUE_ACCEPT_SELF] 或 [ACTION:ISSUE_ACCEPT_ALT:*]。");
		return stringBuilder.ToString().Trim();
	}

	private static string BuildRecentCompletionPromptBlock(Hero targetHero)
	{
		if (targetHero == null || targetHero.Issue != null)
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
		stringBuilder.AppendLine("你现在应当把语气放在“任务已经有结果”上：表示感谢、评价结果、讨论后续影响，但不要再次发放这项任务。");
		stringBuilder.AppendLine("严禁输出 [ACTION:ISSUE_ACCEPT_SELF] 或 [ACTION:ISSUE_ACCEPT_ALT:*]。");
		return stringBuilder.ToString().Trim();
	}

	public static void ApplyIssueOfferTags(Hero speaker, ref string responseText)
	{
		if (speaker == null || string.IsNullOrWhiteSpace(responseText))
		{
			return;
		}
		try
		{
			Match match = IssueAcceptSelfRegex.Match(responseText);
			Match match2 = IssueAcceptAltRegex.Match(responseText);
			Match match3 = QuestTurnInRegex.Match(responseText);
			int num = -1;
			int num2 = int.MaxValue;
			if (match.Success && match.Index < num2)
			{
				num = 0;
				num2 = match.Index;
			}
			if (match2.Success && match2.Index < num2)
			{
				num = 1;
				num2 = match2.Index;
			}
			if (match3.Success && match3.Index < num2)
			{
				num = 2;
			}
			if (num == 0)
			{
				TryAcceptIssueSelf(speaker);
			}
			else if (num == 1)
			{
				TryAcceptIssueWithCompanion(speaker, match2.Groups[1].Value);
			}
			else if (num == 2)
			{
				TryTurnInIssue(speaker);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Logic", "[IssueOffer] ApplyIssueOfferTags 异常: " + ex);
		}
		responseText = IssueAcceptSelfRegex.Replace(responseText ?? "", "").Trim();
		responseText = IssueAcceptAltRegex.Replace(responseText, "").Trim();
		responseText = QuestTurnInRegex.Replace(responseText, "").Trim();
	}

	private static bool TryAcceptIssueSelf(Hero giver)
	{
		if (!TryGetOfferableIssue(giver, out var issue))
		{
			ShowInfo("当前没有可接取的原版任务。", isError: true);
			return false;
		}
		if (!TryCheckQuestPreconditions(issue, giver, out var text))
		{
			ShowInfo(string.IsNullOrWhiteSpace(text) ? "当前不满足该任务的接取条件。" : ("当前还不能接这项任务：" + text), isError: true);
			return false;
		}
		try
		{
			if (Campaign.Current?.IssueManager == null)
			{
				ShowInfo("IssueManager 不可用，无法启动任务。", isError: true);
				return false;
			}
			bool flag = Campaign.Current.IssueManager.StartIssueQuest(giver);
			if (!flag)
			{
				ShowInfo("原版任务启动失败。", isError: true);
				return false;
			}
			if (!FinalizeClassicQuestAcceptance(issue, out var text2))
			{
				ShowInfo(string.IsNullOrWhiteSpace(text2) ? "任务已生成，但未能完成原版接受收尾。" : text2, isError: true);
				return false;
			}
			string text3 = GetSafeIssueTitle(issue);
			MyBehavior.AppendExternalNpcFact(giver, "你已经把任务“" + text3 + "”正式交给了玩家。");
			MyBehavior.AppendExternalPlayerFact(giver, "你已经正式接下了对方交给你的任务“" + text3 + "”。");
			ShowInfo("已接取任务：" + text3, isError: false);
			Logger.Log("Logic", "[IssueOffer] 自身接取成功 giver=" + (giver.StringId ?? "") + " issue=" + text3);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("Logic", "[IssueOffer] 自身接取异常: " + ex);
			ShowInfo("接取任务时出现异常。", isError: true);
			return false;
		}
	}

	private static bool TryAcceptIssueWithCompanion(Hero giver, string companionId)
	{
		if (!TryGetOfferableIssue(giver, out var issue))
		{
			ShowInfo("当前没有可接取的原版任务。", isError: true);
			return false;
		}
		if (_pendingAlternativeDispatch != null)
		{
			ShowInfo("当前已有一个待确认的同伴代办派兵界面。", isError: true);
			return false;
		}
		if (!TryCheckQuestPreconditions(issue, giver, out var text))
		{
			ShowInfo(string.IsNullOrWhiteSpace(text) ? "当前还不能接这项任务。" : ("当前还不能接这项任务：" + text), isError: true);
			return false;
		}
		if (!TryBuildAlternativeCandidates(issue, out var list, out var text2))
		{
			ShowInfo(string.IsNullOrWhiteSpace(text2) ? "当前不能通过同伴代办这项任务。" : ("当前不能同伴代办：" + text2), isError: true);
			return false;
		}
		CompanionCandidate companionCandidate = list.FirstOrDefault((CompanionCandidate x) => string.Equals(x.HeroId, (companionId ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
		if (companionCandidate == null || companionCandidate.Hero == null)
		{
			ShowInfo("LLM 选择的同伴不在当前允许列表中。", isError: true);
			return false;
		}
		if (!IsValidAlternativeCompanion(companionCandidate.Hero, out var text3))
		{
			ShowInfo(string.IsNullOrWhiteSpace(text3) ? "该同伴当前不可用。" : text3, isError: true);
			return false;
		}
		try
		{
			MobileParty.MainParty.MemberRoster.AddToCounts(companionCandidate.Hero.CharacterObject, -1, insertAtFront: false, 0, 0, removeDepleted: true, -1);
			issue.AlternativeSolutionSentTroops.AddToCounts(companionCandidate.Hero.CharacterObject, 1, insertAtFront: false, 0, 0, removeDepleted: true, -1);
			CampaignEventDispatcher.Instance.OnHeroGetsBusy(companionCandidate.Hero, HeroGetsBusyReasons.SolvesIssue);
			int totalAlternativeSolutionNeededMenCount = issue.GetTotalAlternativeSolutionNeededMenCount();
			if (totalAlternativeSolutionNeededMenCount > 1)
			{
				_pendingAlternativeDispatch = new PendingAlternativeDispatch
				{
					Giver = giver,
					Companion = companionCandidate.Hero,
					Issue = issue
				};
				PartyScreenHelper.OpenScreenAsQuest(issue.AlternativeSolutionSentTroops, new TextObject("{=FbLOFO88}Select troops for mission", null), totalAlternativeSolutionNeededMenCount + 1, issue.GetTotalAlternativeSolutionDurationInDays(), AlternativePartyScreenDoneCondition, OnAlternativePartyScreenClosed, AlternativeTroopTransferableDelegate, null);
				ShowInfo("已确认由 " + GetHeroName(companionCandidate.Hero) + " 带队，接下来请选择随行士兵。", isError: false);
				Logger.Log("Logic", "[IssueOffer] 打开同伴代办派兵界面 giver=" + (giver.StringId ?? "") + " companion=" + (companionCandidate.Hero.StringId ?? ""));
				return true;
			}
			CompleteAlternativeDispatch(giver, issue, companionCandidate.Hero);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("Logic", "[IssueOffer] 同伴代办启动异常: " + ex);
			SafeRestoreAlternativeRoster(issue);
			ShowInfo("启动同伴代办流程时出现异常。", isError: true);
			return false;
		}
	}

	private static Tuple<bool, TextObject> AlternativePartyScreenDoneCondition(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum)
	{
		TextObject item;
		return new Tuple<bool, TextObject>(DoTroopsSatisfyAlternativeSolution(leftMemberRoster, out item), item);
	}

	private static void OnAlternativePartyScreenClosed(PartyBase leftOwnerParty, TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, bool fromCancel)
	{
		PendingAlternativeDispatch pendingAlternativeDispatch = _pendingAlternativeDispatch;
		_pendingAlternativeDispatch = null;
		if (pendingAlternativeDispatch?.Issue == null)
		{
			return;
		}
		if (fromCancel)
		{
			SafeRestoreAlternativeRoster(pendingAlternativeDispatch.Issue);
			ShowInfo("已取消同伴代办派兵。", isError: true);
			return;
		}
		TextObject explanation;
		if (!DoTroopsSatisfyAlternativeSolution(leftMemberRoster, out explanation))
		{
			SafeRestoreAlternativeRoster(pendingAlternativeDispatch.Issue);
			ShowInfo("当前派兵结果不满足任务要求" + (string.IsNullOrWhiteSpace(GetText(explanation)) ? "。" : ("：" + GetText(explanation))), isError: true);
			return;
		}
		CompleteAlternativeDispatch(pendingAlternativeDispatch.Giver, pendingAlternativeDispatch.Issue, pendingAlternativeDispatch.Companion);
	}

	private static bool AlternativeTroopTransferableDelegate(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftOwnerParty)
	{
		IssueBase issue = _pendingAlternativeDispatch?.Issue;
		return issue != null && !character.IsHero && !character.IsNotTransferableInPartyScreen && type != PartyScreenLogic.TroopType.Prisoner && issue.IsTroopTypeNeededByAlternativeSolution(character);
	}

	private static void CompleteAlternativeDispatch(Hero giver, IssueBase issue, Hero companion)
	{
		try
		{
			issue.AlternativeSolutionStartConsequence();
			issue.StartIssueWithAlternativeSolution();
			string safeIssueTitle = GetSafeIssueTitle(issue);
			string heroName = GetHeroName(companion);
			MyBehavior.AppendExternalNpcFact(giver, "你已经同意让玩家派 " + heroName + " 率队代办任务“" + safeIssueTitle + "”。");
			MyBehavior.AppendExternalPlayerFact(giver, "你已经与对方谈妥，让 " + heroName + " 率队代办任务“" + safeIssueTitle + "”。");
			ShowInfo("已由 " + heroName + " 接手任务：" + safeIssueTitle, isError: false);
			Logger.Log("Logic", "[IssueOffer] 同伴代办启动成功 giver=" + (giver?.StringId ?? "") + " companion=" + (companion?.StringId ?? "") + " issue=" + safeIssueTitle);
		}
		catch (Exception ex)
		{
			Logger.Log("Logic", "[IssueOffer] CompleteAlternativeDispatch 异常: " + ex);
			SafeRestoreAlternativeRoster(issue);
			ShowInfo("确认同伴代办时出现异常。", isError: true);
		}
	}

	private static bool TryGetOfferableIssue(Hero targetHero, out IssueBase issue)
	{
		issue = targetHero?.Issue;
		return issue != null && issue.IssueOwner == targetHero && issue.IsOngoingWithoutQuest;
	}

	private static bool TryGetInProgressIssue(Hero targetHero, out IssueBase issue)
	{
		issue = targetHero?.Issue;
		return issue != null && issue.IssueOwner == targetHero && issue.IssueQuest != null && issue.IssueQuest.IsOngoing;
	}

	private static bool TryGetReadyToTurnInIssue(Hero targetHero, out IssueBase issue, out TurnInProbeResult probe)
	{
		issue = null;
		probe = null;
		if (!TryGetInProgressIssue(targetHero, out issue))
		{
			return false;
		}
		if (issue.IsSolvingWithAlternative)
		{
			return false;
		}
		string text = "";
		bool flag = TryGetExplicitTurnInSignal(issue.IssueQuest, out text);
		if (TryProbeQuestTurnIn(targetHero, issue, execute: false, out probe, out _))
		{
			probe.ExplicitCompletionSummary = text;
			return true;
		}
		if (flag)
		{
			probe = new TurnInProbeResult
			{
				Issue = issue,
				Quest = issue.IssueQuest,
				ExplicitCompletionSummary = text,
				IntroText = text,
				IsConfident = false
			};
			return true;
		}
		return false;
	}

	private static bool TryGetExplicitTurnInSignal(QuestBase quest, out string summary)
	{
		summary = "";
		MBReadOnlyList<JournalLog> journalEntries = quest?.JournalEntries;
		if (journalEntries == null || journalEntries.Count == 0)
		{
			return false;
		}
		for (int num = journalEntries.Count - 1; num >= 0; num--)
		{
			JournalLog journalLog = journalEntries[num];
			if (journalLog == null)
			{
				continue;
			}
			string text = NormalizePromptText(GetText(journalLog.TaskName));
			string text2 = NormalizePromptText(GetText(journalLog.LogText));
			if (journalLog.Range > 0 && journalLog.CurrentProgress >= journalLog.Range)
			{
				string arg = string.IsNullOrWhiteSpace(text) ? text2 : text;
				summary = string.IsNullOrWhiteSpace(arg) ? ("任务进度已满足：" + journalLog.CurrentProgress + "/" + journalLog.Range) : (arg + "（当前进度 " + journalLog.CurrentProgress + "/" + journalLog.Range + "，已满足）");
				return true;
			}
			string text3 = (text + " " + text2).Trim().ToLowerInvariant();
			if (ContainsAny(text3, "you have enough", "return back to", "return to", "go back to", "report back", "speak to", "回去找", "回到", "回去向", "你有足够", "已满足", "返回"))
			{
				summary = string.IsNullOrWhiteSpace(text2) ? text : text2;
				return true;
			}
		}
		return false;
	}

	private static bool TryCheckQuestPreconditions(IssueBase issue, Hero giver, out string failureReason)
	{
		failureReason = "";
		if (issue == null || giver == null)
		{
			failureReason = "未找到任务或发放者。";
			return false;
		}
		try
		{
			if (CheckPreconditionsMethod == null)
			{
				return true;
			}
			object[] array = new object[2] { giver, null };
			bool flag = (bool)CheckPreconditionsMethod.Invoke(issue, array);
			failureReason = NormalizePromptText(GetText(array[1] as TextObject));
			return flag;
		}
		catch (Exception ex)
		{
			Logger.Log("Logic", "[IssueOffer] CheckPreconditions 反射失败: " + ex.Message);
			return true;
		}
	}

	private static bool FinalizeClassicQuestAcceptance(IssueBase issue, out string error)
	{
		error = "";
		QuestBase issueQuest = issue?.IssueQuest;
		if (issueQuest == null)
		{
			error = "任务对象未创建。";
			return false;
		}
		try
		{
			bool flag = TryInvokeQuestAcceptHook(issueQuest, "QuestAcceptedConsequences");
			flag = TryInvokeQuestAcceptHook(issueQuest, "OnQuestAccepted") || flag;
			flag = TryInvokeQuestAcceptHook(issueQuest, "OfferDialogFlowConsequence") || flag;
			if (!issueQuest.IsOngoing)
			{
				issueQuest.StartQuest();
			}
			if (!issueQuest.IsOngoing)
			{
				error = "任务没有进入进行中状态。";
				return false;
			}
			Logger.Log("Logic", "[IssueOffer] 经典任务接受收尾完成 quest=" + (issueQuest.StringId ?? "") + " hook=" + flag + " logs=" + issueQuest.JournalEntries.Count);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("Logic", "[IssueOffer] FinalizeClassicQuestAcceptance 异常: " + ex);
			error = "原版任务接受收尾异常。";
			return false;
		}
	}

	private static bool TryInvokeQuestAcceptHook(QuestBase quest, string methodName)
	{
		if (quest == null || string.IsNullOrWhiteSpace(methodName))
		{
			return false;
		}
		try
		{
			MethodInfo method = quest.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (method == null || method.GetParameters().Length != 0)
			{
				return false;
			}
			method.Invoke(quest, null);
			Logger.Log("Logic", "[IssueOffer] 调用任务接受钩子 quest=" + (quest.StringId ?? "") + " method=" + methodName);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("Logic", "[IssueOffer] 调用任务接受钩子失败 quest=" + (quest?.StringId ?? "") + " method=" + methodName + " ex=" + ex.Message);
			return false;
		}
	}

	private static bool TryBuildAlternativeCandidates(IssueBase issue, out List<CompanionCandidate> candidates, out string failureReason)
	{
		candidates = new List<CompanionCandidate>();
		failureReason = "";
		if (issue == null || !issue.IsThereAlternativeSolution)
		{
			failureReason = "该任务不支持同伴代办。";
			return false;
		}
		TextObject explanation;
		if (!issue.AlternativeSolutionCondition(out explanation))
		{
			failureReason = NormalizePromptText(GetText(explanation));
			return false;
		}
		IssueModel issueModel = Campaign.Current?.Models?.IssueModel;
		foreach (TroopRosterElement troopRosterElement in MobileParty.MainParty.MemberRoster.GetTroopRoster())
		{
			CharacterObject character = troopRosterElement.Character;
			Hero heroObject = character?.HeroObject;
			if (heroObject == null || !character.IsHero || character.IsPlayerCharacter || !string.IsNullOrWhiteSpace(GetUnavailableCompanionReason(heroObject)))
			{
				continue;
			}
			List<string> list = new List<string>();
			if (issueModel != null)
			{
				ValueTuple<SkillObject, int> issueAlternativeSolutionSkill = issueModel.GetIssueAlternativeSolutionSkill(heroObject, issue);
				if (issueAlternativeSolutionSkill.Item1 != null)
				{
					list.Add((issueAlternativeSolutionSkill.Item1.Name?.ToString() ?? "技能") + "=" + heroObject.GetSkillValue(issueAlternativeSolutionSkill.Item1));
				}
				if (issue.AlternativeSolutionHasFailureRisk)
				{
					float num = issueModel.GetFailureRiskForHero(heroObject, issue);
					list.Add("失败风险=" + MathF.Round(num * 100f, 1) + "%");
				}
				if (issue.AlternativeSolutionHasScaledDuration)
				{
					list.Add("预计耗时=" + Math.Max(1, (int)Math.Round(issueModel.GetDurationOfResolutionForHero(heroObject, issue).ToDays)) + "天");
				}
				else
				{
					list.Add("预计耗时=" + Math.Max(1, issue.GetTotalAlternativeSolutionDurationInDays()) + "天");
				}
				if (issue.AlternativeSolutionHasScaledRequiredTroops)
				{
					list.Add("需士兵=" + Math.Max(0, issueModel.GetTroopsRequiredForHero(heroObject, issue)));
				}
				else
				{
					list.Add("需士兵=" + Math.Max(0, issue.GetTotalAlternativeSolutionNeededMenCount()));
				}
				if (issue.AlternativeSolutionHasCasualties)
				{
					ValueTuple<int, int> causalityForHero = issueModel.GetCausalityForHero(heroObject, issue);
					list.Add((causalityForHero.Item1 == causalityForHero.Item2) ? ("预计伤亡=" + causalityForHero.Item1) : ("预计伤亡=" + causalityForHero.Item1 + "-" + causalityForHero.Item2));
				}
			}
			else
			{
				list.Add("预计耗时=" + Math.Max(1, issue.GetTotalAlternativeSolutionDurationInDays()) + "天");
				list.Add("需士兵=" + Math.Max(0, issue.GetTotalAlternativeSolutionNeededMenCount()));
			}
			candidates.Add(new CompanionCandidate
			{
				Hero = heroObject,
				HeroId = heroObject.StringId ?? "",
				PromptLine = "- " + GetHeroName(heroObject) + " | HeroId=" + (heroObject.StringId ?? "") + (list.Count > 0 ? (" | " + string.Join(" | ", list)) : "")
			});
		}
		if (candidates.Count == 0)
		{
			failureReason = "玩家队伍里当前没有符合条件的可用同伴。";
			return false;
		}
		return true;
	}

	private static string GetUnavailableCompanionReason(Hero hero)
	{
		if (hero == null)
		{
			return "未找到同伴。";
		}
		if (hero.PartyBelongedTo != MobileParty.MainParty)
		{
			return "该同伴当前不在主队。";
		}
		if (!hero.CanHaveCampaignIssues())
		{
			return "该同伴当前不可用。";
		}
		if (hero.IsWounded)
		{
			return "该同伴当前负伤。";
		}
		if (hero.IsPregnant)
		{
			return "该同伴当前怀孕。";
		}
		return "";
	}

	private static bool IsValidAlternativeCompanion(Hero hero, out string reason)
	{
		reason = GetUnavailableCompanionReason(hero);
		return string.IsNullOrWhiteSpace(reason);
	}

	private static bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
	{
		IssueBase issue = _pendingAlternativeDispatch?.Issue;
		if (issue == null)
		{
			explanation = new TextObject("{=!}No pending issue.", null);
			return false;
		}
		int totalAlternativeSolutionNeededMenCount = issue.GetTotalAlternativeSolutionNeededMenCount();
		if (troopRoster.TotalRegulars >= totalAlternativeSolutionNeededMenCount && troopRoster.TotalRegulars - troopRoster.TotalWoundedRegulars < totalAlternativeSolutionNeededMenCount)
		{
			explanation = new TextObject("{=fjmGXcLW}You have to send healthy troops to this quest.", null);
			return false;
		}
		return issue.DoTroopsSatisfyAlternativeSolution(troopRoster, out explanation);
	}

	private static void SafeRestoreAlternativeRoster(IssueBase issue)
	{
		try
		{
			if (issue?.AlternativeSolutionSentTroops == null)
			{
				return;
			}
			MobileParty.MainParty.MemberRoster.Add(issue.AlternativeSolutionSentTroops);
			issue.AlternativeSolutionSentTroops.Clear();
		}
		catch (Exception ex)
		{
			Logger.Log("Logic", "[IssueOffer] RestoreAlternativeRoster 异常: " + ex.Message);
		}
	}

	private static int GetIssueRewardGold(IssueBase issue)
	{
		try
		{
			return ((issue != null && RewardGoldProperty != null) ? Convert.ToInt32(RewardGoldProperty.GetValue(issue, null)) : 0);
		}
		catch
		{
			return 0;
		}
	}

	private static string GetSafeIssueTitle(IssueBase issue)
	{
		string text = NormalizePromptText(GetText(issue?.Title));
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return NormalizePromptText(GetText(issue?.Description)) ?? "未命名任务";
	}

	private static string GetHeroName(Hero hero)
	{
		return (hero?.Name?.ToString() ?? hero?.StringId ?? "未知同伴").Trim();
	}

	private static string GetText(TextObject textObject)
	{
		try
		{
			return textObject?.ToString() ?? "";
		}
		catch
		{
			return "";
		}
	}

	private static string NormalizePromptText(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return Regex.Replace(text.Replace("\r", " ").Replace("\n", " "), "\\s+", " ").Trim();
	}

	private static void AppendRecentJournalLines(StringBuilder sb, MBReadOnlyList<JournalLog> journalEntries)
	{
		if (sb == null || journalEntries == null || journalEntries.Count == 0)
		{
			return;
		}
		List<string> list = new List<string>();
		for (int i = 0; i < journalEntries.Count; i++)
		{
			string text = NormalizePromptText(GetText(journalEntries[i]?.LogText));
			if (!string.IsNullOrWhiteSpace(text))
			{
				list.Add(text);
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		sb.AppendLine("原版任务日志摘要：");
		int num = Math.Max(0, list.Count - 3);
		for (int j = num; j < list.Count; j++)
		{
			sb.AppendLine("- " + list[j]);
		}
	}

	private static void ShowInfo(string text, bool isError)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		InformationManager.DisplayMessage(new InformationMessage(text, isError ? Color.FromUint(4294923605u) : new Color(0f, 1f, 0f)));
	}

	private static bool TryTurnInIssue(Hero giver)
	{
		if (!TryGetReadyToTurnInIssue(giver, out var issue, out var probe))
		{
			ShowInfo("当前没有可通过原版 discuss 流交付的任务。", isError: true);
			return false;
		}
		if (!TryProbeQuestTurnIn(giver, issue, execute: true, out _, out var executionError))
		{
			ShowInfo(string.IsNullOrWhiteSpace(executionError) ? "原版任务交付执行失败。" : executionError, isError: true);
			return false;
		}
		string safeIssueTitle = GetSafeIssueTitle(issue);
		MyBehavior.AppendExternalNpcFact(giver, "你已确认玩家完成了任务“" + safeIssueTitle + "”。");
		MyBehavior.AppendExternalPlayerFact(giver, "你已向对方交付并验收任务“" + safeIssueTitle + "”。");
		ShowInfo("已交付任务：" + safeIssueTitle, isError: false);
		Logger.Log("Logic", "[IssueOffer] 任务交付成功 giver=" + (giver?.StringId ?? "") + " issue=" + safeIssueTitle + " option=" + (probe?.SuccessOptionId ?? ""));
		return true;
	}

	private static bool TryProbeQuestTurnIn(Hero giver, IssueBase issue, bool execute, out TurnInProbeResult probe, out string error)
	{
		probe = null;
		error = "";
		QuestBase issueQuest = issue?.IssueQuest;
		if (giver == null || issue == null || issueQuest == null || !issueQuest.IsOngoing)
		{
			error = "当前没有进行中的原版任务。";
			return false;
		}
		DialogFlow dialogFlow = GetDiscussDialogFlow(issueQuest);
		if (dialogFlow == null)
		{
			error = "当前任务没有可用的原版 discuss 流。";
			return false;
		}
		Agent agent = FindAgentForHeroInMission(giver);
		Agent agent2 = Mission.Current?.MainAgent ?? Agent.Main;
		if (agent == null || agent2 == null)
		{
			error = "当前场景中找不到原版 discuss 探测所需的 Agent。";
			return false;
		}
		ConversationManager conversationManager = Campaign.Current?.ConversationManager;
		if (conversationManager == null || conversationManager.IsConversationInProgress)
		{
			error = "当前原版 ConversationManager 不可用于静默任务交付。";
			return false;
		}
		ConversationManagerSnapshot conversationManagerSnapshot = CaptureConversationManagerSnapshot(conversationManager);
		bool flag = false;
		try
		{
			PrepareSilentConversation(conversationManager, agent, agent2, dialogFlow, "quest_discuss", issueQuest);
			TurnInProbeResult turnInProbeResult = AnalyzeTurnInOptions(conversationManager, issue, issueQuest);
			if (turnInProbeResult == null || !turnInProbeResult.IsConfident)
			{
				probe = turnInProbeResult;
				error = "当前原版 discuss 流里没有足够明确的可交付成功选项。";
				return false;
			}
			probe = turnInProbeResult;
			if (!execute)
			{
				return true;
			}
			flag = ExecuteTurnInOption(conversationManager, issueQuest, turnInProbeResult, out error);
			return flag;
		}
		catch (Exception ex)
		{
			error = "静默原版任务交付异常: " + ex.Message;
			Logger.Log("Logic", "[IssueOffer] TryProbeQuestTurnIn 异常: " + ex);
			return false;
		}
		finally
		{
			if (flag)
			{
				TryConsumeConversationEnd(conversationManager);
			}
			RestoreConversationManagerSnapshot(conversationManager, conversationManagerSnapshot);
		}
	}

	private static TurnInProbeResult AnalyzeTurnInOptions(ConversationManager conversationManager, IssueBase issue, QuestBase quest)
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

	private static int ScorePotentialTurnInOption(string optionText, ConversationSentence sentence, List<ConversationSentence> allSentences)
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

	private static bool ExecuteTurnInOption(ConversationManager conversationManager, QuestBase quest, TurnInProbeResult probe, out string error)
	{
		error = "";
		if (conversationManager == null || quest == null || probe == null || string.IsNullOrWhiteSpace(probe.SuccessOptionId))
		{
			error = "缺少原版任务交付执行参数。";
			return false;
		}
		int num = 0;
		while (quest.IsOngoing && num++ < 12)
		{
			List<ConversationSentenceOption> list = conversationManager.CurOptions ?? new List<ConversationSentenceOption>();
			ConversationSentenceOption? conversationSentenceOption = null;
			if (num == 1)
			{
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i].IsClickable && string.Equals(list[i].Id, probe.SuccessOptionId, StringComparison.Ordinal))
					{
						conversationSentenceOption = list[i];
						break;
					}
				}
			}
			if (!conversationSentenceOption.HasValue)
			{
				List<ConversationSentence> list2 = SentencesField?.GetValue(conversationManager) as List<ConversationSentence> ?? new List<ConversationSentence>();
				int num2 = int.MinValue;
				for (int j = 0; j < list.Count; j++)
				{
					ConversationSentenceOption conversationSentenceOption2 = list[j];
					if (!conversationSentenceOption2.IsClickable || conversationSentenceOption2.SentenceNo < 0 || conversationSentenceOption2.SentenceNo >= list2.Count)
					{
						continue;
					}
					int num3 = ScorePotentialTurnInOption(NormalizePromptText(GetText(conversationSentenceOption2.Text)), list2[conversationSentenceOption2.SentenceNo], list2);
					if (num3 > num2)
					{
						num2 = num3;
						conversationSentenceOption = conversationSentenceOption2;
					}
				}
				if (num2 < 0 && list.Count > 1)
				{
					error = "原版交付链在中途出现多条分支，且无法高置信度自动选择。";
					return false;
				}
			}
			if (!conversationSentenceOption.HasValue)
			{
				break;
			}
			ProcessSentenceMethod?.Invoke(conversationManager, new object[1] { conversationSentenceOption.Value });
			if (!quest.IsOngoing)
			{
				break;
			}
			ProcessPartnerSentenceMethod?.Invoke(conversationManager, null);
			conversationManager.GetPlayerSentenceOptions();
		}
		if (!quest.IsOngoing)
		{
			return true;
		}
		error = "原版交付链已运行，但任务仍未完成。";
		return false;
	}

	private static void PrepareSilentConversation(ConversationManager conversationManager, Agent targetAgent, Agent mainAgent, DialogFlow dialogFlow, string startToken, object relatedObject)
	{
		List<ConversationSentence> value = new List<ConversationSentence>();
		SentencesField?.SetValue(conversationManager, value);
		UsedIndicesField?.SetValue(conversationManager, new HashSet<int>());
		CurrentSentenceField?.SetValue(conversationManager, -1);
		CurrentSentenceTextField?.SetValue(conversationManager, null);
		LastSelectedDialogObjectField?.SetValue(conversationManager, null);
		CurrentRepeatedDialogSetIndexField?.SetValue(conversationManager, 0);
		CurrentRepeatIndexField?.SetValue(conversationManager, 0);
		ClearListField(DialogRepeatObjectsField?.GetValue(conversationManager) as System.Collections.IList);
		ClearListField(DialogRepeatLinesField?.GetValue(conversationManager) as System.Collections.IList);
		IsActiveField?.SetValue(conversationManager, false);
		MainAgentField?.SetValue(conversationManager, mainAgent);
		SpeakerAgentField?.SetValue(conversationManager, null);
		ListenerAgentField?.SetValue(conversationManager, null);
		ConversationPartyField?.SetValue(conversationManager, null);
		List<IAgent> list = ConversationAgentsField?.GetValue(conversationManager) as List<IAgent>;
		if (list != null)
		{
			list.Clear();
			list.Add(targetAgent);
		}
		SetConversationCurrentOptions(conversationManager, new List<ConversationSentenceOption>());
		conversationManager.AddDialogFlow(dialogFlow, relatedObject);
		if (ResetRepeatedDialogSystemMethod != null)
		{
			ResetRepeatedDialogSystemMethod.Invoke(conversationManager, null);
		}
		conversationManager.ActiveToken = conversationManager.GetStateIndex(startToken);
		ProcessPartnerSentenceMethod?.Invoke(conversationManager, null);
		conversationManager.GetPlayerSentenceOptions();
	}

	private static ConversationManagerSnapshot CaptureConversationManagerSnapshot(ConversationManager conversationManager)
	{
		ConversationManagerSnapshot conversationManagerSnapshot = new ConversationManagerSnapshot
		{
			Sentences = CloneList(SentencesField?.GetValue(conversationManager) as List<ConversationSentence>),
			StateMap = CloneDictionary(StateMapField?.GetValue(conversationManager) as Dictionary<string, int>),
			NumberOfStateIndices = (int)(NumberOfStateIndicesField?.GetValue(conversationManager) ?? 0),
			AutoId = (int)(AutoIdField?.GetValue(conversationManager) ?? 0),
			AutoToken = (int)(AutoTokenField?.GetValue(conversationManager) ?? 0),
			UsedIndices = CloneHashSet(UsedIndicesField?.GetValue(conversationManager) as HashSet<int>),
			ActiveToken = conversationManager.ActiveToken,
			CurrentSentence = (int)(CurrentSentenceField?.GetValue(conversationManager) ?? (-1)),
			CurrentSentenceText = CurrentSentenceTextField?.GetValue(conversationManager) as TextObject,
			LastSelectedDialogObject = LastSelectedDialogObjectField?.GetValue(conversationManager),
			CurrentRepeatedDialogSetIndex = (int)(CurrentRepeatedDialogSetIndexField?.GetValue(conversationManager) ?? 0),
			CurrentRepeatIndex = (int)(CurrentRepeatIndexField?.GetValue(conversationManager) ?? 0),
			DialogRepeatObjects = CloneNestedObjectList(DialogRepeatObjectsField?.GetValue(conversationManager) as List<List<object>>),
			DialogRepeatLines = CloneList(DialogRepeatLinesField?.GetValue(conversationManager) as List<TextObject>),
			IsActive = (bool)(IsActiveField?.GetValue(conversationManager) ?? false),
			LastSelectedButtonIndex = conversationManager.LastSelectedButtonIndex,
			MainAgent = MainAgentField?.GetValue(conversationManager),
			SpeakerAgent = SpeakerAgentField?.GetValue(conversationManager),
			ListenerAgent = ListenerAgentField?.GetValue(conversationManager),
			ConversationAgents = CloneObjectList(ConversationAgentsField?.GetValue(conversationManager) as List<IAgent>),
			ConversationParty = ConversationPartyField?.GetValue(conversationManager) as MobileParty,
			CurOptions = CloneList(conversationManager.CurOptions)
		};
		return conversationManagerSnapshot;
	}

	private static void RestoreConversationManagerSnapshot(ConversationManager conversationManager, ConversationManagerSnapshot snapshot)
	{
		if (conversationManager == null || snapshot == null)
		{
			return;
		}
		SentencesField?.SetValue(conversationManager, snapshot.Sentences ?? new List<ConversationSentence>());
		StateMapField?.SetValue(conversationManager, snapshot.StateMap ?? new Dictionary<string, int>());
		NumberOfStateIndicesField?.SetValue(conversationManager, snapshot.NumberOfStateIndices);
		AutoIdField?.SetValue(conversationManager, snapshot.AutoId);
		AutoTokenField?.SetValue(conversationManager, snapshot.AutoToken);
		UsedIndicesField?.SetValue(conversationManager, snapshot.UsedIndices ?? new HashSet<int>());
		conversationManager.ActiveToken = snapshot.ActiveToken;
		CurrentSentenceField?.SetValue(conversationManager, snapshot.CurrentSentence);
		CurrentSentenceTextField?.SetValue(conversationManager, snapshot.CurrentSentenceText);
		LastSelectedDialogObjectField?.SetValue(conversationManager, snapshot.LastSelectedDialogObject);
		CurrentRepeatedDialogSetIndexField?.SetValue(conversationManager, snapshot.CurrentRepeatedDialogSetIndex);
		CurrentRepeatIndexField?.SetValue(conversationManager, snapshot.CurrentRepeatIndex);
		RestoreNestedObjectList(DialogRepeatObjectsField?.GetValue(conversationManager) as List<List<object>>, snapshot.DialogRepeatObjects);
		RestoreList(DialogRepeatLinesField?.GetValue(conversationManager) as List<TextObject>, snapshot.DialogRepeatLines);
		IsActiveField?.SetValue(conversationManager, snapshot.IsActive);
		conversationManager.LastSelectedButtonIndex = snapshot.LastSelectedButtonIndex;
		MainAgentField?.SetValue(conversationManager, snapshot.MainAgent);
		SpeakerAgentField?.SetValue(conversationManager, snapshot.SpeakerAgent);
		ListenerAgentField?.SetValue(conversationManager, snapshot.ListenerAgent);
		RestoreObjectList(ConversationAgentsField?.GetValue(conversationManager) as List<IAgent>, snapshot.ConversationAgents);
		ConversationPartyField?.SetValue(conversationManager, snapshot.ConversationParty);
		SetConversationCurrentOptions(conversationManager, snapshot.CurOptions ?? new List<ConversationSentenceOption>());
	}

	private static void TryConsumeConversationEnd(ConversationManager conversationManager)
	{
		try
		{
			LordEncounterRedirectGuard.SuppressForSeconds(1f);
			conversationManager?.EndConversation();
		}
		catch (Exception ex)
		{
			Logger.Log("Logic", "[IssueOffer] EndConversation 异常: " + ex.Message);
		}
	}

	private static DialogFlow GetDiscussDialogFlow(QuestBase quest)
	{
		try
		{
			return DiscussDialogFlowField?.GetValue(quest) as DialogFlow;
		}
		catch
		{
			return null;
		}
	}

	private static Agent FindAgentForHeroInMission(Hero hero)
	{
		if (hero == null || Mission.Current?.Agents == null)
		{
			return null;
		}
		foreach (Agent agent in Mission.Current.Agents)
		{
			if (agent?.Character is CharacterObject characterObject && characterObject.HeroObject == hero)
			{
				return agent;
			}
		}
		return null;
	}

	private static bool ContainsAny(string source, params string[] patterns)
	{
		if (string.IsNullOrWhiteSpace(source) || patterns == null)
		{
			return false;
		}
		for (int i = 0; i < patterns.Length; i++)
		{
			if (!string.IsNullOrWhiteSpace(patterns[i]) && source.IndexOf(patterns[i], StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static List<T> CloneList<T>(List<T> source)
	{
		return (source != null) ? new List<T>(source) : new List<T>();
	}

	private static Dictionary<string, int> CloneDictionary(Dictionary<string, int> source)
	{
		return (source != null) ? new Dictionary<string, int>(source, StringComparer.Ordinal) : new Dictionary<string, int>(StringComparer.Ordinal);
	}

	private static HashSet<int> CloneHashSet(HashSet<int> source)
	{
		return (source != null) ? new HashSet<int>(source) : new HashSet<int>();
	}

	private static List<List<object>> CloneNestedObjectList(List<List<object>> source)
	{
		List<List<object>> list = new List<List<object>>();
		if (source == null)
		{
			return list;
		}
		for (int i = 0; i < source.Count; i++)
		{
			list.Add((source[i] != null) ? new List<object>(source[i]) : new List<object>());
		}
		return list;
	}

	private static List<object> CloneObjectList(List<IAgent> source)
	{
		List<object> list = new List<object>();
		if (source != null)
		{
			for (int i = 0; i < source.Count; i++)
			{
				list.Add(source[i]);
			}
		}
		return list;
	}

	private static void ClearListField(System.Collections.IList list)
	{
		list?.Clear();
	}

	private static void RestoreNestedObjectList(List<List<object>> target, List<List<object>> snapshot)
	{
		if (target == null)
		{
			return;
		}
		target.Clear();
		if (snapshot == null)
		{
			return;
		}
		for (int i = 0; i < snapshot.Count; i++)
		{
			target.Add((snapshot[i] != null) ? new List<object>(snapshot[i]) : new List<object>());
		}
	}

	private static void RestoreList<T>(List<T> target, List<T> snapshot)
	{
		if (target == null)
		{
			return;
		}
		target.Clear();
		if (snapshot != null)
		{
			target.AddRange(snapshot);
		}
	}

	private static void RestoreObjectList(List<IAgent> target, List<object> snapshot)
	{
		if (target == null)
		{
			return;
		}
		target.Clear();
		if (snapshot == null)
		{
			return;
		}
		for (int i = 0; i < snapshot.Count; i++)
		{
			if (snapshot[i] is IAgent item)
			{
				target.Add(item);
			}
		}
	}

	private static void SetConversationCurrentOptions(ConversationManager conversationManager, List<ConversationSentenceOption> options)
	{
		try
		{
			CurOptionsProperty?.SetValue(conversationManager, options, null);
		}
		catch
		{
			conversationManager?.ClearCurrentOptions();
		}
	}
}
