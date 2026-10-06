using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues;

public class GangLeaderNeedsRecruitsIssueBehavior : CampaignBehaviorBase
{
	public class GangLeaderNeedsRecruitsIssue : IssueBase
	{
		private const int IssueAndQuestDuration = 30;

		private const int AlternativeSolutionTroopTierRequirement = 2;

		private const int AlternativeSolutionRelationBonus = 5;

		private const int AlternativeSolutionNotablePowerBonus = 10;

		private const int AlternativeSolutionPlayerHonorBonus = 30;

		private const int AlternativeSolutionRewardPerRecruit = 100;

		private const int CompanionRequiredSkillLevel = 120;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.RequiredTroops;

		private int RequestedRecruitCount => 6 + TaleWorlds.Library.MathF.Ceiling(10f * base.IssueDifficultyMultiplier);

		public override int AlternativeSolutionBaseNeededMenCount => 11 + TaleWorlds.Library.MathF.Ceiling(9f * base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 6 + TaleWorlds.Library.MathF.Ceiling(7f * base.IssueDifficultyMultiplier);

		protected override int RewardGold => 2000 + RequestedRecruitCount * 100;

		public override TextObject IssueBriefByIssueGiver => new TextObject("{=YxtiyxSf}Yes... As you no doubt know, this is rough work, and I've lost a lot of good lads recently. I haven't had much luck replacing them. I need men who understand how things work in our business, and that's not always easy to find. I could use bandits and looters. They usually know their stuff. But if I take them in as prisoners, they'll just slip away as soon as I get the chance. I need volunteers...[ib:hip][if:convo_undecided_closed]");

		public override TextObject IssueAcceptByPlayer => new TextObject("{=jGpBZDvC}I see. What do you want from me?");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver => new TextObject("{=Qh26ReAv}Look, I know that warriors like you can sometimes recruit bandits to your party. Some of those men might want to take their chances working for me. More comfortable in living in town, where there's always drink and women on hand, then roaming endlessly about the countryside, eh? For each one that signs up with me I'll give you a bounty, more if they have some experience.[if:convo_innocent_smile][ib:hip]");

		public override TextObject IssueQuestSolutionAcceptByPlayer => new TextObject("{=ekLDmgS7}I'll find your recruits.");

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver => new TextObject("{=bKfaMFVK}You can also send me a recruiter: a trustworthy companion who is good at leading men, and also enough of a rogue to win the trust of other rogues...[if:convo_undecided_open][ib:confident]");

		public override TextObject IssueAlternativeSolutionAcceptByPlayer => new TextObject("{=kxvnA811}All right, I will send you someone from my party who fits your bill.");

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver => new TextObject("{=8sDjwsnW}I'm sure your lieutenant will solve my problem. Thank you for your help.[if:convo_nonchalant][ib:demure2]");

		public override TextObject IssueDiscussAlternativeSolution => new TextObject("{=TkvsBd4H}Your companion seems to have a knack with the local never-do-wells. I hear a lot of fine lads have already signed up.[if:convo_relaxed_happy][ib:hip2]");

		public override bool IsThereAlternativeSolution => true;

		public override bool IsThereLordSolution => false;

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=wX14wxqF}You asked {COMPANION.LINK} to deliver at least {WANTED_RECRUIT_AMOUNT} looters and bandits to {ISSUE_GIVER.LINK} in {SETTLEMENT}. They should rejoin your party in {RETURN_DAYS} days.");
				textObject.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject);
				textObject.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject);
				textObject.SetTextVariable("WANTED_RECRUIT_AMOUNT", RequestedRecruitCount);
				textObject.SetTextVariable("SETTLEMENT", base.IssueOwner.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		public override TextObject Title => new TextObject("{=rrh7rSLs}Gang Needs Recruits");

		public override TextObject Description
		{
			get
			{
				TextObject textObject = new TextObject("{=0kYaAb7c}A gang leader needs recruits for {?ISSUE_GIVER.GENDER}her{?}his{\\?} gang.");
				textObject.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject);
				return textObject;
			}
		}

		protected override int CompanionSkillRewardXP => (int)(500f + 700f * base.IssueDifficultyMultiplier);

		internal static void AutoGeneratedStaticCollectObjectsGangLeaderNeedsRecruitsIssue(object o, List<object> collectedObjects)
		{
			((GangLeaderNeedsRecruitsIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		public GangLeaderNeedsRecruitsIssue(Hero issueOwner)
			: base(issueOwner, CampaignTime.DaysFromNow(30f))
		{
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.IssueOwnerPower)
			{
				return -0.1f;
			}
			return 0f;
		}

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			return ((hero.GetSkillValue(DefaultSkills.Leadership) >= hero.GetSkillValue(DefaultSkills.Roguery)) ? DefaultSkills.Leadership : DefaultSkills.Roguery, 120);
		}

		public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		public override bool AlternativeSolutionCondition(out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
		{
			return character.Tier >= 2;
		}

		protected override void OnGameLoad()
		{
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new GangLeaderNeedsRecruitsIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(30f), RequestedRecruitCount);
		}

		public override IssueFrequency GetFrequency()
		{
			return IssueFrequency.VeryCommon;
		}

		protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out PreconditionFlags flag, out Hero relationHero, out SkillObject skill, out int requiredGold)
		{
			flag = PreconditionFlags.None;
			relationHero = null;
			requiredGold = 0;
			skill = null;
			if (issueGiver.GetRelationWithPlayer() < -10f)
			{
				flag |= PreconditionFlags.Relation;
				relationHero = issueGiver;
			}
			return flag == PreconditionFlags.None;
		}

		public override bool IssueStayAliveConditions()
		{
			return true;
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.IssueOwner, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 30)
			});
			base.IssueOwner.AddPower(10f);
			RelationshipChangeWithIssueOwner = 5;
		}
	}

	public class GangLeaderNeedsRecruitsIssueQuest : QuestBase
	{
		private const int QuestGiverRelationBonusOnSuccess = 5;

		private const int QuestGiverNotablePowerBonusOnSuccess = 10;

		private const int QuestGiverRelationPenaltyOnFail = -5;

		private const int NotablePowerPenaltyOnFail = -10;

		private const int PlayerHonorBonusOnSuccess = 30;

		[SaveableField(1)]
		private int _requestedRecruitCount;

		[SaveableField(5)]
		private int _deliveredRecruitCount;

		[SaveableField(6)]
		private int _rewardGold;

		[SaveableField(9)]
		private bool _playerReachedRequestedAmount;

		[SaveableField(7)]
		private JournalLog _questProgressLogTest;

		public override TextObject Title => new TextObject("{=rrh7rSLs}Gang Needs Recruits");

		public override bool IsRemainingTimeHidden => false;

		private TextObject QuestStartedLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=PZI9Smv3}{QUEST_GIVER.LINK}, a gang leader in {SETTLEMENT}, told you that {?QUEST_GIVER.GENDER}she{?}he{\\?} needs recruits for {?QUEST_GIVER.GENDER}her{?}his{\\?} gang. {?QUEST_GIVER.GENDER}She{?}He{\\?} asked you to recruit {NEEDED_RECRUIT_AMOUNT} looters or bandits into your party, then transfer them to {?QUEST_GIVER.GENDER}her{?}him{\\?}. You will be paid for the recruits depending on their experience.");
				textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
				textObject.SetTextVariable("SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("NEEDED_RECRUIT_AMOUNT", _requestedRecruitCount);
				return textObject;
			}
		}

		private TextObject QuestSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=3ApJ6LaX}You have transferred the recruits to {QUEST_GIVER.LINK} as promised.");
				textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
				return textObject;
			}
		}

		private TextObject QuestFailedWithTimeOutLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=iUmWTmQz}You have failed to deliver enough recruits in time. {QUEST_GIVER.LINK} must be disappointed.");
				textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
				return textObject;
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsGangLeaderNeedsRecruitsIssueQuest(object o, List<object> collectedObjects)
		{
			((GangLeaderNeedsRecruitsIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_questProgressLogTest);
		}

		internal static object AutoGeneratedGetMemberValue_requestedRecruitCount(object o)
		{
			return ((GangLeaderNeedsRecruitsIssueQuest)o)._requestedRecruitCount;
		}

		internal static object AutoGeneratedGetMemberValue_deliveredRecruitCount(object o)
		{
			return ((GangLeaderNeedsRecruitsIssueQuest)o)._deliveredRecruitCount;
		}

		internal static object AutoGeneratedGetMemberValue_rewardGold(object o)
		{
			return ((GangLeaderNeedsRecruitsIssueQuest)o)._rewardGold;
		}

		internal static object AutoGeneratedGetMemberValue_playerReachedRequestedAmount(object o)
		{
			return ((GangLeaderNeedsRecruitsIssueQuest)o)._playerReachedRequestedAmount;
		}

		internal static object AutoGeneratedGetMemberValue_questProgressLogTest(object o)
		{
			return ((GangLeaderNeedsRecruitsIssueQuest)o)._questProgressLogTest;
		}

		public GangLeaderNeedsRecruitsIssueQuest(string questId, Hero questGiver, CampaignTime duration, int requestedRecruitCount)
			: base(questId, questGiver, duration, 0)
		{
			_requestedRecruitCount = requestedRecruitCount;
			_deliveredRecruitCount = 0;
			_rewardGold = 2000;
			_playerReachedRequestedAmount = false;
			SetDialogs();
			InitializeQuestOnCreation();
		}

		private void QuestAcceptedConsequences()
		{
			StartQuest();
			AddTrackedObject(base.QuestGiver.CurrentSettlement);
			_questProgressLogTest = AddDiscreteLog(QuestStartedLogText, new TextObject("{=r8rwl9ZS}Delivered Recruits"), _deliveredRecruitCount, _requestedRecruitCount);
		}

		protected override void SetDialogs()
		{
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(new TextObject("{=0QuAZ8YO}I'll be waiting. Good luck.[if:convo_relaxed_happy][ib:confident]")).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.Consequence(QuestAcceptedConsequences)
				.CloseDialog();
			TextObject npcDiscussLine = new TextObject("{=!}{GANG_LEADER_NEEDS_RECRUITS_QUEST_NOTABLE_DISCUSS}");
			TextObject npcResponseLine = new TextObject("{=!}{GANG_LEADER_NEEDS_RECRUITS_QUEST_NOTABLE_RESPONSE}");
			bool changeDialogAfterTransfer = false;
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").BeginNpcOptions().NpcOption(new TextObject("{=BGgDjRcW}I think that's enough. Here is your payment."), () => Hero.OneToOneConversationHero == base.QuestGiver && _playerReachedRequestedAmount)
				.Consequence(delegate
				{
					ApplyQuestSuccessConsequences();
					CompleteQuestWithSuccess();
				})
				.CloseDialog()
				.NpcOption(npcDiscussLine, delegate
				{
					if (Hero.OneToOneConversationHero != base.QuestGiver)
					{
						return false;
					}
					if (!changeDialogAfterTransfer)
					{
						npcDiscussLine.SetTextVariable("GANG_LEADER_NEEDS_RECRUITS_QUEST_NOTABLE_DISCUSS", new TextObject("{=1hpeeCJD}Have you found any good men?[ib:confident3]"));
						changeDialogAfterTransfer = true;
					}
					else
					{
						npcDiscussLine.SetTextVariable("GANG_LEADER_NEEDS_RECRUITS_QUEST_NOTABLE_DISCUSS", new TextObject("{=ds294zxi}Anything else?"));
						changeDialogAfterTransfer = false;
					}
					return true;
				})
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=QbaOoilS}Yes, I have brought you a few men."))
				.Condition(() => CheckIfThereIsSuitableRecruitInPlayer() && !_playerReachedRequestedAmount && changeDialogAfterTransfer)
				.NpcLine(npcResponseLine)
				.Condition(delegate
				{
					if (_playerReachedRequestedAmount)
					{
						return false;
					}
					npcResponseLine.SetTextVariable("GANG_LEADER_NEEDS_RECRUITS_QUEST_NOTABLE_RESPONSE", new TextObject("{=70LnOZzo}Very good. Keep searching. We still need more men.[ib:hip2]"));
					return true;
				})
				.Consequence(OpenRecruitDeliveryScreen)
				.PlayerLine(new TextObject("{=IULW8h03}Sure."))
				.Consequence(delegate
				{
					if (_playerReachedRequestedAmount && Campaign.Current.ConversationManager.IsConversationInProgress)
					{
						Campaign.Current.ConversationManager.ContinueConversation();
					}
				})
				.GotoDialogState("quest_discuss")
				.PlayerOption(new TextObject("{=PZqGagXt}No, not yet. I'm still looking for them."))
				.Condition(() => !_playerReachedRequestedAmount && changeDialogAfterTransfer)
				.Consequence(delegate
				{
					changeDialogAfterTransfer = false;
				})
				.NpcLine(new TextObject("{=L1JyetPq}I am glad to hear that.[ib:closed2]"))
				.CloseDialog()
				.PlayerOption(new TextObject("{=OlOhuO7X}No thank you. Good day to you."))
				.Condition(() => !_playerReachedRequestedAmount && !changeDialogAfterTransfer)
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog()
				.EndNpcOptions();
		}

		private void OpenRecruitDeliveryScreen()
		{
			PartyScreenHelper.OpenScreenWithCondition(IsTroopTransferable, DoneButtonCondition, DoneClicked, null, PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.NotTransferable, base.QuestGiver.Name, _requestedRecruitCount - _deliveredRecruitCount, showProgressBar: false, isDonating: false, PartyScreenHelper.PartyScreenMode.TroopsManage);
		}

		private Tuple<bool, TextObject> DoneButtonCondition(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum)
		{
			if (_requestedRecruitCount - _deliveredRecruitCount < leftMemberRoster.TotalManCount)
			{
				int num = _requestedRecruitCount - _deliveredRecruitCount;
				TextObject textObject = new TextObject("{=VOr3uoRZ}You can only transfer {X} recruit{?IS_PLURAL}s{?}{\\?}.");
				textObject.SetTextVariable("IS_PLURAL", (num > 1) ? 1 : 0);
				textObject.SetTextVariable("X", num);
				return new Tuple<bool, TextObject>(item1: false, textObject);
			}
			return new Tuple<bool, TextObject>(item1: true, null);
		}

		private bool DoneClicked(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, PartyBase leftParty, PartyBase rightParty)
		{
			foreach (TroopRosterElement item in leftMemberRoster.GetTroopRoster())
			{
				_rewardGold += RewardForEachRecruit(item.Character) * item.Number;
				_deliveredRecruitCount += item.Number;
			}
			_questProgressLogTest.UpdateCurrentProgress(_deliveredRecruitCount);
			_questProgressLogTest.TaskName.SetTextVariable("TOTAL_REWARD", _rewardGold);
			if (_deliveredRecruitCount == _requestedRecruitCount)
			{
				_playerReachedRequestedAmount = true;
				if (Campaign.Current.ConversationManager.IsConversationInProgress)
				{
					Campaign.Current.ConversationManager.ContinueConversation();
				}
			}
			return true;
		}

		private int RewardForEachRecruit(CharacterObject recruit)
		{
			return (int)(100f * ((recruit.Tier <= 1) ? 1f : ((recruit.Tier <= 3) ? 1.5f : 2f)));
		}

		private bool IsTroopTransferable(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftOwnerParty)
		{
			if (_requestedRecruitCount - _deliveredRecruitCount >= 0)
			{
				if (side != PartyScreenLogic.PartyRosterSide.Left)
				{
					if (MobileParty.MainParty.MemberRoster.Contains(character))
					{
						return character.Occupation == Occupation.Bandit;
					}
					return false;
				}
				return true;
			}
			return false;
		}

		private bool CheckIfThereIsSuitableRecruitInPlayer()
		{
			bool result = false;
			foreach (TroopRosterElement item in MobileParty.MainParty.MemberRoster.GetTroopRoster())
			{
				if (item.Character.Occupation == Occupation.Bandit)
				{
					result = true;
					break;
				}
			}
			return result;
		}

		private void ApplyQuestSuccessConsequences()
		{
			AddLog(QuestSuccessLog);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 30)
			});
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, _rewardGold);
			base.QuestGiver.AddPower(10f);
			RelationshipChangeWithQuestGiver = 5;
		}

		protected override void OnBeforeTimedOut(ref bool completeWithSuccess, ref bool doNotResolveTheQuest)
		{
			if (_deliveredRecruitCount >= _requestedRecruitCount)
			{
				completeWithSuccess = true;
				ApplyQuestSuccessConsequences();
			}
		}

		protected override void OnTimedOut()
		{
			AddLog(QuestFailedWithTimeOutLogText);
			base.QuestGiver.AddPower(-10f);
			RelationshipChangeWithQuestGiver = -5;
		}

		protected override void InitializeQuestOnGameLoad()
		{
			SetDialogs();
		}

		protected override void HourlyTick()
		{
		}
	}

	public class GangLeaderNeedsRecruitsIssueBehaviorTypeDefiner : SaveableTypeDefiner
	{
		public GangLeaderNeedsRecruitsIssueBehaviorTypeDefiner()
			: base(820000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(GangLeaderNeedsRecruitsIssue), 1);
			AddClassDefinition(typeof(GangLeaderNeedsRecruitsIssueQuest), 2);
		}
	}

	private const IssueBase.IssueFrequency GangLeaderNeedsRecruitsIssueFrequency = IssueBase.IssueFrequency.VeryCommon;

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private static bool ConditionsHold(Hero issueGiver)
	{
		if (issueGiver.CurrentSettlement != null)
		{
			return issueGiver.IsGangLeader;
		}
		return false;
	}

	public void OnCheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero))
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnSelected, typeof(GangLeaderNeedsRecruitsIssue), IssueBase.IssueFrequency.VeryCommon));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(GangLeaderNeedsRecruitsIssue), IssueBase.IssueFrequency.VeryCommon));
		}
	}

	private static IssueBase OnSelected(in PotentialIssueData pid, Hero issueOwner)
	{
		return new GangLeaderNeedsRecruitsIssue(issueOwner);
	}
}
