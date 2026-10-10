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
				ShowInfo(ConversationUiLocalization.Text("AF_CONV_UI_Issue_NoOfferable", "No vanilla quest is currently available to accept."), isError: true);
				return false;
			}
			if (!TryCheckQuestPreconditions(issue, giver, out var text))
			{
				Logger.Log("Logic", "[IssueOffer] TryAcceptIssueSelf fail=preconditions giver=" + (giver?.StringId ?? "") + " issue=" + (issue?.StringId ?? "") + " reason=" + (text ?? ""));
				ShowInfo(string.IsNullOrWhiteSpace(text) ? ConversationUiLocalization.Text("AF_CONV_UI_Issue_PreconditionsNotMet", "You do not currently meet the requirements to accept this quest.") : ConversationUiLocalization.Text("AF_CONV_UI_Issue_CannotAcceptReason", "Cannot accept this quest right now: {REASON}", ("REASON", IssueDisplayTexts.ForDisplay(text))), isError: true);
				return false;
			}
			try
			{
				if (Campaign.Current?.IssueManager == null)
				{
					Logger.Log("Logic", "[IssueOffer] TryAcceptIssueSelf fail=issue_manager_null giver=" + (giver?.StringId ?? "") + " issue=" + (issue?.StringId ?? ""));
					ShowInfo(ConversationUiLocalization.Text("AF_CONV_UI_Issue_ManagerUnavailable", "IssueManager is unavailable; cannot start quest."), isError: true);
					return false;
				}
				bool flag = Campaign.Current.IssueManager.StartIssueQuest(giver);
				Logger.Log("Logic", "[IssueOffer] TryAcceptIssueSelf after_StartIssueQuest giver=" + (giver?.StringId ?? "") + " issue=" + (issue?.StringId ?? "") + " started=" + flag + " questPresentNow=" + ((issue?.IssueQuest != null) ? "true" : "false") + " questId=" + (issue?.IssueQuest?.StringId ?? ""));
				if (!flag)
				{
					Logger.Log("Logic", "[IssueOffer] TryAcceptIssueSelf fail=start_issue_quest_false giver=" + (giver?.StringId ?? "") + " issue=" + (issue?.StringId ?? ""));
					ShowInfo(ConversationUiLocalization.Text("AF_CONV_UI_Issue_StartFailed", "Failed to start vanilla quest."), isError: true);
					return false;
				}
				if (!FinalizeClassicQuestAcceptance(issue, out var text2))
				{
					Logger.Log("Logic", "[IssueOffer] TryAcceptIssueSelf fail=finalize giver=" + (giver?.StringId ?? "") + " issue=" + (issue?.StringId ?? "") + " questId=" + (issue?.IssueQuest?.StringId ?? "") + " error=" + (text2 ?? ""));
					ShowInfo(string.IsNullOrWhiteSpace(text2) ? ConversationUiLocalization.Text("AF_CONV_UI_Issue_FinalizeFailed", "Quest was created, but failed to finalize vanilla acceptance.") : IssueDisplayTexts.ForDisplay(text2), isError: true);
					return false;
				}
				string text3 = GetSafeIssueTitle(issue);
				MyBehavior.AppendExternalNpcFact(giver, "你已经把任务“" + text3 + "”正式交给了玩家。");
				MyBehavior.AppendExternalPlayerFact(giver, "你已经正式接下了对方交给你的任务“" + text3 + "”。");
				ShowInfo(ConversationUiLocalization.Text("AF_CONV_UI_Issue_AcceptedSelf", "Accepted quest: {TITLE}", ("TITLE", text3)), isError: false);
				Logger.Log("Logic", "[IssueOffer] 自身接取成功 giver=" + (giver.StringId ?? "") + " issue=" + text3);
				return true;
			}
			catch (Exception ex)
			{
				Logger.Log("Logic", "[IssueOffer] 自身接取异常: " + ex);
				ShowInfo(ConversationUiLocalization.Text("AF_CONV_UI_Issue_AcceptException", "An error occurred while accepting the quest."), isError: true);
				return false;
			}
		}

		internal static bool TryAcceptIssueWithCompanion(Hero giver, string companionId)
		{
			if (!IssueRuntimeStateOwner.TryGetOfferableIssue(giver, out var issue))
			{
				ShowInfo(ConversationUiLocalization.Text("AF_CONV_UI_Issue_NoOfferable", "No vanilla quest is currently available to accept."), isError: true);
				return false;
			}
			if (_dispatchOwner.HasPending)
			{
				ShowInfo(ConversationUiLocalization.Text("AF_CONV_UI_Issue_AltDispatchPending", "A companion dispatch screen is already pending confirmation."), isError: true);
				return false;
			}
			if (!TryCheckQuestPreconditions(issue, giver, out var text))
			{
				ShowInfo(string.IsNullOrWhiteSpace(text) ? ConversationUiLocalization.Text("AF_CONV_UI_Issue_PreconditionsNotMet", "You do not currently meet the requirements to accept this quest.") : ConversationUiLocalization.Text("AF_CONV_UI_Issue_CannotAcceptReason", "Cannot accept this quest right now: {REASON}", ("REASON", IssueDisplayTexts.ForDisplay(text))), isError: true);
				return false;
			}
			if (!TryBuildAlternativeCandidates(issue, out var list, out var text2))
			{
				ShowInfo(string.IsNullOrWhiteSpace(text2) ? ConversationUiLocalization.Text("AF_CONV_UI_Issue_NoAltSolution", "This quest cannot be solved by a companion right now.") : ConversationUiLocalization.Text("AF_CONV_UI_Issue_CannotAltReason", "Cannot assign to companion: {REASON}", ("REASON", IssueDisplayTexts.ForDisplay(text2))), isError: true);
				return false;
			}
			CompanionCandidate companionCandidate = list.FirstOrDefault((CompanionCandidate x) => string.Equals(x.HeroId, (companionId ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
			if (companionCandidate == null || companionCandidate.Hero == null)
			{
				ShowInfo(ConversationUiLocalization.Text("AF_CONV_UI_Issue_CompanionNotAllowed", "The chosen companion is not in the allowed list."), isError: true);
				return false;
			}
			if (!IsValidAlternativeCompanion(companionCandidate.Hero, out var text3))
			{
				ShowInfo(string.IsNullOrWhiteSpace(text3) ? ConversationUiLocalization.Text("AF_CONV_UI_Issue_CompanionUnavailable", "This companion is currently unavailable.") : IssueDisplayTexts.ForDisplay(text3), isError: true);
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
					ShowInfo(ConversationUiLocalization.Text("AF_CONV_UI_Issue_CompanionConfirmedSelectTroops", "Confirmed that {HERO} will lead. Please select the accompanying troops.", ("HERO", GetHeroName(companionCandidate.Hero))), isError: false);
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
				ShowInfo(ConversationUiLocalization.Text("AF_CONV_UI_Issue_AltStartException", "An error occurred while starting the companion solution."), isError: true);
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
				ShowInfo(ConversationUiLocalization.Text("AF_CONV_UI_Issue_AltAccepted", "{HERO} has taken over the quest: {TITLE}", ("HERO", heroName), ("TITLE", safeIssueTitle)), isError: false);
				Logger.Log("Logic", "[IssueOffer] 同伴代办启动成功 giver=" + (giver?.StringId ?? "") + " companion=" + (companion?.StringId ?? "") + " issue=" + safeIssueTitle);
				return true;
			}
			catch (Exception ex)
			{
				Logger.Log("Logic", "[IssueOffer] CompleteAlternativeDispatch 异常: " + ex);
				SafeRestoreAlternativeRoster(issue);
				ShowInfo(ConversationUiLocalization.Text("AF_CONV_UI_Issue_AltConfirmException", "An error occurred while confirming companion assignment."), isError: true);
				return false;
			}
		}

		internal static bool TryBuildAlternativeCandidates(IssueBase issue, out List<CompanionCandidate> candidates, out string failureReason)
		{
			candidates = new List<CompanionCandidate>();
			failureReason = "";
			if (issue == null || !issue.IsThereAlternativeSolution)
			{
				failureReason = ConversationUiLocalization.Text("AF_CONV_UI_Issue_NoAltSolution", "This quest cannot be solved by a companion right now.");
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
				failureReason = ConversationUiLocalization.Text("AF_CONV_UI_Issue_PartyUnavailable", "Player party is currently unavailable.");
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
				failureReason = ConversationUiLocalization.Text("AF_CONV_UI_Issue_NoEligibleCompanion", "No eligible companion is available in your party.");
				return false;
			}
			return true;
		}

		private static string GetUnavailableCompanionReason(Hero hero)
		{
			if (hero == null)
			{
				return ConversationUiLocalization.Text("AF_CONV_UI_Issue_CompanionNotFound", "Companion not found.");
			}
			if (hero.PartyBelongedTo != MobileParty.MainParty)
			{
				return ConversationUiLocalization.Text("AF_CONV_UI_Issue_CompanionNotInParty", "This companion is not in your main party.");
			}
			if (!hero.CanHaveCampaignIssues())
			{
				return ConversationUiLocalization.Text("AF_CONV_UI_Issue_CompanionUnavailable", "This companion is currently unavailable.");
			}
			if (hero.IsWounded)
			{
				return ConversationUiLocalization.Text("AF_CONV_UI_Issue_CompanionWounded", "This companion is currently wounded.");
			}
			if (hero.IsPregnant)
			{
				return ConversationUiLocalization.Text("AF_CONV_UI_Issue_CompanionPregnant", "This companion is currently pregnant.");
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
				ShowInfo(ConversationUiLocalization.Text("AF_CONV_UI_Issue_NoTurnInQuest", "No quest available to turn in via conversation."), isError: true);
				return false;
			}
			if (!TryProbeQuestTurnIn(giver, issue, execute: true, out _, out var executionError))
			{
				ShowInfo(string.IsNullOrWhiteSpace(executionError) ? ConversationUiLocalization.Text("AF_CONV_UI_Issue_TurnInFailed", "Failed to turn in vanilla quest.") : IssueDisplayTexts.ForDisplay(executionError), isError: true);
				return false;
			}
			string safeIssueTitle = GetSafeIssueTitle(issue);
			MyBehavior.AppendExternalNpcFact(giver, "你已确认玩家完成了任务“" + safeIssueTitle + "”。该任务奖励会按原版流程自动发放，无需你手动再次支付。");
			MyBehavior.AppendExternalPlayerFact(giver, "你已向对方交付并验收任务“" + safeIssueTitle + "”。该任务奖励会按原版流程自动结算，无需NPC手动再次发放。");
			ShowInfo(ConversationUiLocalization.Text("AF_CONV_UI_Issue_TurnedIn", "Turned in quest: {TITLE}", ("TITLE", safeIssueTitle)), isError: false);
			Logger.Log("Logic", "[IssueOffer] 任务交付成功 giver=" + (giver?.StringId ?? "") + " issue=" + safeIssueTitle + " option=" + (probe?.SuccessOptionId ?? ""));
			return true;
		}

	}
}
