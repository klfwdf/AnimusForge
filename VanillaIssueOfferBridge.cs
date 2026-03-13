using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

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

	private static readonly Regex IssueAcceptSelfRegex = new Regex("\\[ACTION:ISSUE_ACCEPT_SELF\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex IssueAcceptAltRegex = new Regex("\\[ACTION:ISSUE_ACCEPT_ALT:COMPANION=([^\\]\\r\\n]+)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly MethodInfo CheckPreconditionsMethod = AccessTools.Method(typeof(IssueBase), "CheckPreconditions");

	private static readonly PropertyInfo RewardGoldProperty = typeof(IssueBase).GetProperty("RewardGold", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

	private static PendingAlternativeDispatch _pendingAlternativeDispatch;

	public static string BuildPromptBlock(Hero targetHero)
	{
		if (!TryGetOfferableIssue(targetHero, out var issue))
		{
			return "";
		}
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
		stringBuilder.AppendLine("你当前确实有一项原版任务可向玩家说明。请用你自己的口吻讲清任务，不要机械复述原版文本。");
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
			stringBuilder.AppendLine("当前玩家满足这项任务的原版接取前提。只有在玩家明确同意亲自接下时，你才可以在回复末尾附加隐藏标签 [ACTION:ISSUE_ACCEPT_SELF]。");
		}
		else
		{
			stringBuilder.AppendLine("当前玩家还不满足这项任务的原版接取前提。严禁输出任何接任务标签。");
			if (!string.IsNullOrWhiteSpace(text5))
			{
				stringBuilder.AppendLine("当前不能交付的原版原因：" + text5);
			}
		}
		if (!flag2)
		{
			stringBuilder.AppendLine("这项任务不支持同伴代办。严禁输出 [ACTION:ISSUE_ACCEPT_ALT:*]。");
		}
		else if (flag3)
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
			stringBuilder.AppendLine("这项任务原版支持同伴代办，但当前不可进入同伴代办流程。严禁输出 [ACTION:ISSUE_ACCEPT_ALT:*]。");
			if (!string.IsNullOrWhiteSpace(text6))
			{
				stringBuilder.AppendLine("当前不能同伴代办的原版原因：" + text6);
			}
		}
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
			int num = ((match.Success && match2.Success) ? ((match.Index <= match2.Index) ? 0 : 1) : (match.Success ? 0 : (match2.Success ? 1 : (-1))));
			if (num == 0)
			{
				TryAcceptIssueSelf(speaker);
			}
			else if (num == 1)
			{
				TryAcceptIssueWithCompanion(speaker, match2.Groups[1].Value);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Logic", "[IssueOffer] ApplyIssueOfferTags 异常: " + ex);
		}
		responseText = IssueAcceptSelfRegex.Replace(responseText ?? "", "").Trim();
		responseText = IssueAcceptAltRegex.Replace(responseText, "").Trim();
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
			string text2 = GetSafeIssueTitle(issue);
			MyBehavior.AppendExternalNpcFact(giver, "你已经把任务“" + text2 + "”正式交给了玩家。");
			MyBehavior.AppendExternalPlayerFact(giver, "你已经正式接下了对方交给你的任务“" + text2 + "”。");
			ShowInfo("已接取任务：" + text2, isError: false);
			Logger.Log("Logic", "[IssueOffer] 自身接取成功 giver=" + (giver.StringId ?? "") + " issue=" + text2);
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

	private static void ShowInfo(string text, bool isError)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		InformationManager.DisplayMessage(new InformationMessage(text, isError ? Color.FromUint(4294923605u) : new Color(0f, 1f, 0f)));
	}
}
