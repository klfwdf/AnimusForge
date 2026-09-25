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
	private static class IssueActionOwner
	{
		internal static bool TryAcceptIssueSelf(Hero giver)
		{
			IssueBase issue2 = giver?.Issue;
			Logger.Log("Logic", "[IssueOffer] TryAcceptIssueSelf enter giver=" + (giver?.StringId ?? "") + " issuePresent=" + (issue2 != null) + " issueOwner=" + (issue2?.IssueOwner?.StringId ?? "") + " isOngoingWithoutQuest=" + ((issue2 != null) ? issue2.IsOngoingWithoutQuest.ToString() : "false") + " questPresent=" + ((issue2?.IssueQuest != null) ? "true" : "false"));
			if (!IssueRuntimeStateOwner.TryGetOfferableIssue(giver, out var issue))
			{
				Logger.Log("Logic", "[IssueOffer] TryAcceptIssueSelf fail=no_offerable_issue giver=" + (giver?.StringId ?? ""));
				ShowInfo("当前没有可接取的原版任务。", isError: true);
				return false;
			}
			if (!TryCheckQuestPreconditions(issue, giver, out var text))
			{
				Logger.Log("Logic", "[IssueOffer] TryAcceptIssueSelf fail=preconditions giver=" + (giver?.StringId ?? "") + " issue=" + (issue?.StringId ?? "") + " reason=" + (text ?? ""));
				ShowInfo(string.IsNullOrWhiteSpace(text) ? "当前不满足该任务的接取条件。" : ("当前还不能接这项任务：" + text), isError: true);
				return false;
			}
			try
			{
				if (Campaign.Current?.IssueManager == null)
				{
					Logger.Log("Logic", "[IssueOffer] TryAcceptIssueSelf fail=issue_manager_null giver=" + (giver?.StringId ?? "") + " issue=" + (issue?.StringId ?? ""));
					ShowInfo("IssueManager 不可用，无法启动任务。", isError: true);
					return false;
				}
				bool flag = Campaign.Current.IssueManager.StartIssueQuest(giver);
				Logger.Log("Logic", "[IssueOffer] TryAcceptIssueSelf after_StartIssueQuest giver=" + (giver?.StringId ?? "") + " issue=" + (issue?.StringId ?? "") + " started=" + flag + " questPresentNow=" + ((issue?.IssueQuest != null) ? "true" : "false") + " questId=" + (issue?.IssueQuest?.StringId ?? ""));
				if (!flag)
				{
					Logger.Log("Logic", "[IssueOffer] TryAcceptIssueSelf fail=start_issue_quest_false giver=" + (giver?.StringId ?? "") + " issue=" + (issue?.StringId ?? ""));
					ShowInfo("原版任务启动失败。", isError: true);
					return false;
				}
				if (!FinalizeClassicQuestAcceptance(issue, out var text2))
				{
					Logger.Log("Logic", "[IssueOffer] TryAcceptIssueSelf fail=finalize giver=" + (giver?.StringId ?? "") + " issue=" + (issue?.StringId ?? "") + " questId=" + (issue?.IssueQuest?.StringId ?? "") + " error=" + (text2 ?? ""));
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

		internal static bool TryAcceptIssueWithCompanion(Hero giver, string companionId)
		{
			if (!IssueRuntimeStateOwner.TryGetOfferableIssue(giver, out var issue))
			{
				ShowInfo("当前没有可接取的原版任务。", isError: true);
				return false;
			}
			if (_dispatchOwner.HasPending)
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
					var pending = new PendingAlternativeDispatch
					{
						Giver = giver,
						Companion = companionCandidate.Hero,
						Issue = issue
					};
					if (!_dispatchOwner.TryBegin(pending))
					{
						SafeRestoreAlternativeRoster(issue);
						return false;
					}
					PartyScreenHelper.OpenScreenAsQuest(issue.AlternativeSolutionSentTroops, new TextObject("{=FbLOFO88}Select troops for mission", null), totalAlternativeSolutionNeededMenCount + 1, issue.GetTotalAlternativeSolutionDurationInDays(),
						(leftMembers, leftPrisoners, rightMembers, rightPrisoners, leftLimit, rightLimit) => AlternativePartyScreenDoneCondition(leftMembers, leftPrisoners, rightMembers, rightPrisoners, leftLimit, rightLimit, pending),
						(leftParty, leftMembers, leftPrisoners, rightParty, rightMembers, rightPrisoners, fromCancel) => OnAlternativePartyScreenClosed(leftParty, leftMembers, leftPrisoners, rightParty, rightMembers, rightPrisoners, fromCancel, pending),
						(character, type, side, leftParty) => AlternativeTroopTransferableDelegate(character, type, side, leftParty, pending), null);
					ShowInfo("已确认由 " + GetHeroName(companionCandidate.Hero) + " 带队，接下来请选择随行士兵。", isError: false);
					Logger.Log("Logic", "[IssueOffer] 打开同伴代办派兵界面 giver=" + (giver.StringId ?? "") + " companion=" + (companionCandidate.Hero.StringId ?? ""));
					return true;
				}
				return CompleteAlternativeDispatch(giver, issue, companionCandidate.Hero);
			}
			catch (Exception ex)
			{
				_dispatchOwner.Clear();
				Logger.Log("Logic", "[IssueOffer] 同伴代办启动异常: " + ex);
				SafeRestoreAlternativeRoster(issue);
				ShowInfo("启动同伴代办流程时出现异常。", isError: true);
				return false;
			}
		}

		internal static bool CompleteAlternativeDispatch(Hero giver, IssueBase issue, Hero companion)
		{
			try
			{
				if (companion == null || !IssueRuntimeStateOwner.TryGetOfferableIssue(giver, out IssueBase currentIssue) || !ReferenceEquals(currentIssue, issue))
				{
					SafeRestoreAlternativeRoster(issue);
					return false;
				}
				issue.AlternativeSolutionStartConsequence();
				issue.StartIssueWithAlternativeSolution();
				string safeIssueTitle = GetSafeIssueTitle(issue);
				string heroName = GetHeroName(companion);
				MyBehavior.AppendExternalNpcFact(giver, "你已经同意让玩家派 " + heroName + " 率队代办任务“" + safeIssueTitle + "”。");
				MyBehavior.AppendExternalPlayerFact(giver, "你已经与对方谈妥，让 " + heroName + " 率队代办任务“" + safeIssueTitle + "”。");
				ShowInfo("已由 " + heroName + " 接手任务：" + safeIssueTitle, isError: false);
				Logger.Log("Logic", "[IssueOffer] 同伴代办启动成功 giver=" + (giver?.StringId ?? "") + " companion=" + (companion?.StringId ?? "") + " issue=" + safeIssueTitle);
				return true;
			}
			catch (Exception ex)
			{
				Logger.Log("Logic", "[IssueOffer] CompleteAlternativeDispatch 异常: " + ex);
				SafeRestoreAlternativeRoster(issue);
				ShowInfo("确认同伴代办时出现异常。", isError: true);
				return false;
			}
		}

		internal static bool TryBuildAlternativeCandidates(IssueBase issue, out List<CompanionCandidate> candidates, out string failureReason)
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
			TroopRoster mainPartyRoster = MobileParty.MainParty?.MemberRoster ?? PartyBase.MainParty?.MemberRoster;
			if (mainPartyRoster == null)
			{
				failureReason = "当前玩家队伍不可用。";
				return false;
			}
			foreach (TroopRosterElement troopRosterElement in mainPartyRoster.GetTroopRoster())
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

		internal static bool TryTurnInIssue(Hero giver)
		{
			if (!IssueRuntimeStateOwner.TryGetReadyToTurnInIssue(giver, out var issue, out var probe))
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
			MyBehavior.AppendExternalNpcFact(giver, "你已确认玩家完成了任务“" + safeIssueTitle + "”。该任务奖励会按原版流程自动发放，无需你手动再次支付。");
			MyBehavior.AppendExternalPlayerFact(giver, "你已向对方交付并验收任务“" + safeIssueTitle + "”。该任务奖励会按原版流程自动结算，无需NPC手动再次发放。");
			ShowInfo("已交付任务：" + safeIssueTitle, isError: false);
			Logger.Log("Logic", "[IssueOffer] 任务交付成功 giver=" + (giver?.StringId ?? "") + " issue=" + safeIssueTitle + " option=" + (probe?.SuccessOptionId ?? ""));
			return true;
		}

	}
}
