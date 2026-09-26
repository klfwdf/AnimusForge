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

	private static readonly FieldInfo OfferDialogFlowField = AccessTools.Field(typeof(QuestBase), "OfferDialogFlow");

	private static readonly FieldInfo DialogFlowLinesField = AccessTools.Field(typeof(DialogFlow), "Lines");

	private static readonly IssueAlternativeDispatchOwner _dispatchOwner = new IssueAlternativeDispatchOwner();

	internal static void ClearPendingAlternativeDispatchForCampaign()
	{
		_dispatchOwner.Clear();
	}

	public static bool IsRagEligibleForExternal(Hero targetHero)
	{
		return IssueRuntimeStateOwner.TryGetRuntimeState(targetHero, out var _, out var _, out var _);
	}

	public static string BuildRagSemanticStateForExternal(Hero targetHero)
	{
		return IssueRuntimeStateOwner.TryGetRuntimeState(targetHero, out var stateKey, out var _, out var _) ? ("vanilla_issue:" + stateKey) : "";
	}

	public static string BuildRuntimePromptBlockForExternal(Hero targetHero)
	{
		if (IssueRuntimePromptOwner.TryBuildRuntimePromptBlock(targetHero, out var _, out var promptText))
		{
			return promptText;
		}
		return IssueRuntimePromptOwner.BuildNoAvailableIssuePromptBlock(targetHero);
	}

	public static List<PostprocessRuleEntry> BuildRuntimePostprocessRulesForExternal(Hero targetHero)
	{
		return IssueRuntimePromptOwner.BuildRuntimePostprocessRules(targetHero);
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
			Logger.Log("Logic", "[IssueOffer] ApplyIssueOfferTags enter speaker=" + (speaker.StringId ?? "") + " hasSelf=" + match.Success + " hasAlt=" + match2.Success + " hasTurnIn=" + match3.Success + " raw=" + ((responseText ?? "").Replace("\r", "\\r").Replace("\n", "\\n")));
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
				bool flag = IssueActionOwner.TryAcceptIssueSelf(speaker);
				Logger.Log("Logic", "[IssueOffer] ApplyIssueOfferTags self_result=" + flag + " speaker=" + (speaker.StringId ?? ""));
			}
			else if (num == 1)
			{
				bool flag2 = IssueActionOwner.TryAcceptIssueWithCompanion(speaker, match2.Groups[1].Value);
				Logger.Log("Logic", "[IssueOffer] ApplyIssueOfferTags alt_result=" + flag2 + " speaker=" + (speaker.StringId ?? "") + " companion=" + match2.Groups[1].Value);
			}
			else if (num == 2)
			{
				bool flag3 = IssueActionOwner.TryTurnInIssue(speaker);
				Logger.Log("Logic", "[IssueOffer] ApplyIssueOfferTags turnin_result=" + flag3 + " speaker=" + (speaker.StringId ?? ""));
			}
			else
			{
				Logger.Log("Logic", "[IssueOffer] ApplyIssueOfferTags no_action speaker=" + (speaker.StringId ?? ""));
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

	private static Tuple<bool, TextObject> AlternativePartyScreenDoneCondition(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum, PendingAlternativeDispatch expected)
	{
		TextObject item;
		return new Tuple<bool, TextObject>(DoTroopsSatisfyAlternativeSolution(leftMemberRoster, expected, out item), item);
	}

	private static void OnAlternativePartyScreenClosed(PartyBase leftOwnerParty, TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, bool fromCancel, PendingAlternativeDispatch expected)
	{
		if (!_dispatchOwner.IsCurrent(expected) || expected.Issue == null)
		{
			return;
		}
		if (fromCancel)
		{
			_dispatchOwner.TryTake(expected, out _);
			SafeRestoreAlternativeRoster(expected.Issue);
			ShowInfo("已取消同伴代办派兵。", isError: true);
			return;
		}
		TextObject explanation;
		if (!DoTroopsSatisfyAlternativeSolution(leftMemberRoster, expected, out explanation))
		{
			_dispatchOwner.TryTake(expected, out _);
			SafeRestoreAlternativeRoster(expected.Issue);
			ShowInfo("当前派兵结果不满足任务要求" + (string.IsNullOrWhiteSpace(GetText(explanation)) ? "。" : ("：" + GetText(explanation))), isError: true);
			return;
		}
		if (_dispatchOwner.TryTake(expected, out PendingAlternativeDispatch pendingAlternativeDispatch))
		{
			CompleteAlternativeDispatch(pendingAlternativeDispatch.Giver, pendingAlternativeDispatch.Issue, pendingAlternativeDispatch.Companion);
		}
	}

	private static bool AlternativeTroopTransferableDelegate(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftOwnerParty, PendingAlternativeDispatch expected)
	{
		IssueBase issue = _dispatchOwner.IsCurrent(expected) ? expected.Issue : null;
		return issue != null && !character.IsHero && !character.IsNotTransferableInPartyScreen && type != PartyScreenLogic.TroopType.Prisoner && issue.IsTroopTypeNeededByAlternativeSolution(character);
	}

	private static bool CompleteAlternativeDispatch(Hero giver, IssueBase issue, Hero companion)
	{
		return IssueActionOwner.CompleteAlternativeDispatch(giver, issue, companion);
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
			Logger.Log("Logic", "[IssueOffer] FinalizeClassicQuestAcceptance fail=no_issueQuest issue=" + (issue?.StringId ?? ""));
			error = "任务对象未创建。";
			return false;
		}
		try
		{
			bool flag = TryInvokeQuestAcceptHook(issueQuest, out var text);
			if (!issueQuest.IsOngoing && IsIssueQuestStillAttached(issue, issueQuest) && !flag)
			{
				flag = TryInvokeOfferDialogFlowAutoConsequence(issueQuest, out text);
			}
			if (!issueQuest.IsOngoing && IsIssueQuestStillAttached(issue, issueQuest))
			{
				Logger.Log("Logic", "[IssueOffer] FinalizeClassicQuestAcceptance fallback_StartQuest issue=" + (issue?.StringId ?? "") + " quest=" + (issueQuest?.StringId ?? "") + " hook=" + (text ?? "") + " logs=" + issueQuest.JournalEntries.Count);
				issueQuest.StartQuest();
				text = string.IsNullOrWhiteSpace(text) ? "StartQuestFallback" : (text + "+StartQuestFallback");
			}
			if (!IsIssueQuestStillAttached(issue, issueQuest))
			{
				Logger.Log("Logic", "[IssueOffer] FinalizeClassicQuestAcceptance fail=quest_detached_or_finalized issue=" + (issue?.StringId ?? "") + " quest=" + (issueQuest?.StringId ?? "") + " hook=" + (text ?? "") + " questOngoing=" + issueQuest.IsOngoing + " logs=" + issueQuest.JournalEntries.Count);
				error = "原版接取分支已立即结束或移除了任务，未能进入普通进行中状态。";
				return false;
			}
			if (!issueQuest.IsOngoing)
			{
				error = "任务没有进入进行中状态。";
				return false;
			}
			Logger.Log("Logic", "[IssueOffer] 经典任务接受收尾完成 quest=" + (issueQuest.StringId ?? "") + " hook=" + (text ?? "") + " logs=" + issueQuest.JournalEntries.Count);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("Logic", "[IssueOffer] FinalizeClassicQuestAcceptance 异常: " + ex);
			error = "原版任务接受收尾异常。";
			return false;
		}
	}

	private static bool IsIssueQuestStillAttached(IssueBase issue, QuestBase quest)
	{
		return issue != null && quest != null && issue.IssueQuest == quest && issue.IsSolvingWithQuest;
	}

	private static bool TryInvokeQuestAcceptHook(QuestBase quest, out string methodName)
	{
		methodName = "";
		string[] array = new string[4] { "QuestAcceptedConsequences", "QuestAcceptedByPlayerConsequences", "OnQuestAccepted", "OfferDialogFlowConsequence" };
		for (int i = 0; i < array.Length; i++)
		{
			if (TryInvokeQuestAcceptHook(quest, array[i]))
			{
				methodName = array[i];
				return true;
			}
		}
		return false;
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

	private static bool TryInvokeOfferDialogFlowAutoConsequence(QuestBase quest, out string consequenceName)
	{
		consequenceName = "";
		try
		{
			DialogFlow dialogFlow = OfferDialogFlowField?.GetValue(quest) as DialogFlow;
			if (dialogFlow == null)
			{
				return false;
			}
			System.Collections.IEnumerable enumerable = DialogFlowLinesField?.GetValue(dialogFlow) as System.Collections.IEnumerable;
			if (enumerable == null)
			{
				return false;
			}
			List<ConversationSentence.OnConsequenceDelegate> list = new List<ConversationSentence.OnConsequenceDelegate>();
			foreach (object item in enumerable)
			{
				if (item == null || GetDialogFlowLineBool(item, "ByPlayer"))
				{
					continue;
				}
				string dialogFlowLineString = GetDialogFlowLineString(item, "InputToken");
				if (!string.Equals(dialogFlowLineString, "issue_classic_quest_start", StringComparison.Ordinal))
				{
					continue;
				}
				ConversationSentence.OnConsequenceDelegate dialogFlowLineDelegate = GetDialogFlowLineDelegate<ConversationSentence.OnConsequenceDelegate>(item, "ConsequenceDelegate");
				if (dialogFlowLineDelegate != null)
				{
					list.Add(dialogFlowLineDelegate);
				}
			}
			if (list.Count != 1)
			{
				if (list.Count > 1)
				{
					Logger.Log("Logic", "[IssueOffer] OfferDialogFlow auto consequence skipped: ambiguous count=" + list.Count + " quest=" + (quest?.StringId ?? ""));
				}
				return false;
			}
			ConversationSentence.OnConsequenceDelegate onConsequenceDelegate = list[0];
			consequenceName = "OfferDialogFlow." + (onConsequenceDelegate.Method?.Name ?? "anonymous");
			onConsequenceDelegate();
			Logger.Log("Logic", "[IssueOffer] 调用 OfferDialogFlow 自动接取收尾 quest=" + (quest.StringId ?? "") + " consequence=" + consequenceName);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("Logic", "[IssueOffer] OfferDialogFlow 自动接取收尾失败 quest=" + (quest?.StringId ?? "") + " ex=" + ex.Message);
			return false;
		}
	}

	private static string GetDialogFlowLineString(object line, string fieldName)
	{
		try
		{
			return (line?.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(line) as string) ?? "";
		}
		catch
		{
			return "";
		}
	}

	private static bool GetDialogFlowLineBool(object line, string fieldName)
	{
		try
		{
			object value = line?.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(line);
			return value is bool flag && flag;
		}
		catch
		{
			return false;
		}
	}

	private static T GetDialogFlowLineDelegate<T>(object line, string fieldName) where T : class
	{
		try
		{
			return line?.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(line) as T;
		}
		catch
		{
			return null;
		}
	}

	private static bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, PendingAlternativeDispatch expected, out TextObject explanation)
	{
		IssueBase issue = _dispatchOwner.IsCurrent(expected) ? expected.Issue : null;
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

	private static string GetPlayerDisplayNameForPrompt()
	{
		string text = "";
		try
		{
			text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
		}
		catch
		{
			text = "";
		}
		text = (text ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? "玩家" : text;
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
			TurnInProbeResult turnInProbeResult = IssueTurnInDecisionOwner.AnalyzeTurnInOptions(conversationManager, issue, issueQuest);
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

	private static string JoinPromptBlocks(string first, string second)
	{
		string text = (first ?? "").Trim();
		string text2 = (second ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return text2;
		}
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		return text + Environment.NewLine + Environment.NewLine + text2;
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
					int num3 = IssueTurnInDecisionOwner.ScorePotentialTurnInOption(NormalizePromptText(GetText(conversationSentenceOption2.Text)), list2[conversationSentenceOption2.SentenceNo], list2);
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
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (hero == null || agents == null)
		{
			return null;
		}
		foreach (Agent agent in agents)
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
