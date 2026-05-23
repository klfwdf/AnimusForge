using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues;

public class LandLordNeedsManualLaborersIssueBehavior : CampaignBehaviorBase
{
	public class LandLordNeedsManualLaborersIssue : IssueBase
	{
		private const int IssueDuration = 30;

		private const int QuestDuration = 20;

		private const int AlternativeSolutionTroopTierRequirement = 2;

		private const int CompanionRequiredSkillLevel = 120;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.RequiredTroops;

		private int RequestedPrisonerCount => 6 + TaleWorlds.Library.MathF.Ceiling(30f * base.IssueDifficultyMultiplier);

		public override int AlternativeSolutionBaseNeededMenCount => 8 + TaleWorlds.Library.MathF.Ceiling(12f * base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 5 + TaleWorlds.Library.MathF.Ceiling(5f * base.IssueDifficultyMultiplier);

		protected override int RewardGold => RequestedPrisonerCount * 50;

		public override TextObject IssueBriefByIssueGiver
		{
			get
			{
				if (base.IssueOwner.CharacterObject.GetTraitLevel(DefaultTraits.Mercy) < 0)
				{
					return new TextObject("{=QEhapwtN}I own a mine near here. Normally I can find willing villagers to work it, but these days they've been demanding higher and higher wages. Fine. They're out of a job, but I still need to work the mine. If you could perhaps find me some prisoners...[if:convo_normal][ib:confident2]");
				}
				return new TextObject("{=1LFcSRPw}I have a mine near here. We had an unfortunate accident a week back. Two workers were crushed to death. It's a great shame... but work must go on. Trouble is, no one wants to come back. If perhaps you could find me some prisoners...[if:convo_thinking][ib:hip]");
			}
		}

		public override TextObject IssueAcceptByPlayer => new TextObject("{=DSMMIrz9}Prisoners... You want criminals or war captives?");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=ji5kkqXy}They need to be criminals, bandits, for me to do this legally. I need at least {WANTED_PRISONER_AMOUNT} of them. But if you can bring more I will gladly accept. I'm willing to pay five times more than their market price for each. What do you say?[if:convo_nonchalant]");
				textObject.SetTextVariable("WANTED_PRISONER_AMOUNT", RequestedPrisonerCount);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=WpidwcAV}If you have a companion who understands this type of work and {ALTERNATIVE_TROOP_AMOUNT} men, I can tell them where to go to get their hands on some prisoners.");
				textObject.SetTextVariable("ALTERNATIVE_TROOP_AMOUNT", GetTotalAlternativeSolutionNeededMenCount());
				return textObject;
			}
		}

		public override TextObject IssueQuestSolutionAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=C06TxxnM}I'll bring you at least {WANTED_PRISONER_AMOUNT} prisoners as soon as possible.");
				textObject.SetTextVariable("WANTED_PRISONER_AMOUNT", RequestedPrisonerCount);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionAcceptByPlayer => new TextObject("{=QH3shVzb}My people can bring you your laborers.");

		public override TextObject IssueDiscussAlternativeSolution => new TextObject("{=XXOlu6z0}Thank you for sparing some of your men to save my business. I am looking forward to resuming work.[if:convo_mocking_teasing]");

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver => new TextObject("{=PrzguaEq}Thank you. I appreciate your people's help.");

		public override bool IsThereAlternativeSolution => true;

		public override bool IsThereLordSolution => false;

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=MYeCnHTb}You asked {COMPANION.LINK} to deliver at least {WANTED_PRISONER_AMOUNT} prisoners to {ISSUE_GIVER.LINK} in {SETTLEMENT}. They should rejoin your party in {RETURN_DAYS} days.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				textObject.SetTextVariable("WANTED_PRISONER_AMOUNT", RequestedPrisonerCount);
				textObject.SetTextVariable("SETTLEMENT", base.IssueOwner.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		public override TextObject Title => new TextObject("{=hVIsGd2y}Landowner Needs Manual Laborers");

		public override TextObject Description
		{
			get
			{
				TextObject result = new TextObject("{=5of4a1kg}A landowner needs your help to find prisoners to use in {?ISSUE_GIVER.GENDER}her{?}his{\\?} mines as manual laborers.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject);
				return result;
			}
		}

		protected override int CompanionSkillRewardXP => (int)(500f + 700f * base.IssueDifficultyMultiplier);

		internal static void AutoGeneratedStaticCollectObjectsLandLordNeedsManualLaborersIssue(object o, List<object> collectedObjects)
		{
			((LandLordNeedsManualLaborersIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		public LandLordNeedsManualLaborersIssue(Hero issueOwner)
			: base(issueOwner, CampaignTime.DaysFromNow(30f))
		{
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.VillageHearth)
			{
				return -0.3f;
			}
			if (issueEffect == DefaultIssueEffects.IssueOwnerPower)
			{
				return -0.1f;
			}
			return 0f;
		}

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			return ((hero.GetSkillValue(DefaultSkills.Athletics) >= hero.GetSkillValue(DefaultSkills.Leadership)) ? DefaultSkills.Athletics : DefaultSkills.Leadership, 120);
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

		public override IssueFrequency GetFrequency()
		{
			return IssueFrequency.VeryCommon;
		}

		public override bool IssueStayAliveConditions()
		{
			if (!base.IssueOwner.CurrentSettlement.IsRaided)
			{
				return !base.IssueOwner.CurrentSettlement.IsUnderRaid;
			}
			return false;
		}

		protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out PreconditionFlags flags, out Hero relationHero, out SkillObject skill, out int requiredGold)
		{
			flags = PreconditionFlags.None;
			relationHero = null;
			requiredGold = 0;
			skill = null;
			if (issueGiver.GetRelationWithPlayer() < -10f)
			{
				flags |= PreconditionFlags.Relation;
				relationHero = issueGiver;
			}
			if (FactionManager.IsAtWarAgainstFaction(issueGiver.MapFaction, Hero.MainHero.MapFaction))
			{
				flags |= PreconditionFlags.AtWar;
			}
			return flags == PreconditionFlags.None;
		}

		protected override void OnGameLoad()
		{
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new LandLordNeedsManualLaborersIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(20f), RequestedPrisonerCount);
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

	public class LandLordNeedsManualLaborersIssueQuest : QuestBase
	{
		private const int MaximumPrisonerLimitMinValue = 40;

		private const int MaximumPrisonerLimitMaxValue = 60;

		[SaveableField(1)]
		private int _requestedPrisonerCount;

		[SaveableField(2)]
		private bool _shareProfit;

		[SaveableField(4)]
		private bool _counterOfferGiven;

		[SaveableField(5)]
		private int _deliveredPrisonerCount;

		[SaveableField(6)]
		private int _rewardGold;

		[SaveableField(8)]
		private int _maximumPrisonerCount;

		[SaveableField(9)]
		private bool _playerReachedMaximumAmount;

		private const float _questPrisonerValueMultiplier = 5f;

		[SaveableField(7)]
		private JournalLog _questProgressLogTest;

		public override TextObject Title => new TextObject("{=hVIsGd2y}Landowner Needs Manual Laborers");

		public override bool IsRemainingTimeHidden => false;

		private bool IsQuestWithCounterOffer
		{
			get
			{
				if (base.QuestGiver.CharacterObject.GetTraitLevel(DefaultTraits.Mercy) < 0)
				{
					return base.QuestGiver.CurrentSettlement.Notables.FirstOrDefault((Hero x) => x.IsHeadman) != null;
				}
				return false;
			}
		}

		private TextObject QuestStartedLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=7XR4MJci}{QUEST_GIVER.LINK}, a landowner in {SETTLEMENT}, told you that {?QUEST_GIVER.GENDER}she{?}he{\\?} needs prisoners to use in {?QUEST_GIVER.GENDER}her{?}his{\\?} mines as manual laborers. {?QUEST_GIVER.GENDER}She{?}He{\\?} asked you to bring at least {NEEDED_PRISONER_AMOUNT} bandit prisoners, but {?QUEST_GIVER.GENDER}she{?}he{\\?} will pay extra if you bring more. You have agreed to bring {?QUEST_GIVER.GENDER}her{?}him{\\?} at least {NEEDED_PRISONER_AMOUNT} bandit prisoner and you will be paid five times more than their market price for each prisoner you bring.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("NEEDED_PRISONER_AMOUNT", _requestedPrisonerCount);
				return textObject;
			}
		}

		private TextObject QuestCanceledWarDeclaredLog
		{
			get
			{
				TextObject textObject = new TextObject("{=vW6kBki9}Your clan is now at war with {QUEST_GIVER.LINK}'s realm. Your agreement with {QUEST_GIVER.LINK} is canceled.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject PlayerDeclaredWarQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=bqeWVVEE}Your actions have started a war with {QUEST_GIVER.LINK}'s faction. {?QUEST_GIVER.GENDER}She{?}He{\\?} cancels your agreement and the quest is a failure.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject QuestGiverVillageRaided
		{
			get
			{
				TextObject textObject = new TextObject("{=gJG0xmAq}{QUEST_GIVER.LINK}'s village {QUEST_SETTLEMENT} was raided. Your agreement with {QUEST_GIVER.LINK} is canceled.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject QuestSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=ucu9u1nS}You delivered the prisoners to {QUEST_GIVER.LINK} as promised.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject PlayerSharedTheProfitWithHeadManLogText => new TextObject("{=ULpP07bg}You promised to share half of the profit with the headman and the villager.");

		private TextObject QuestSuccessWithProfitShareLog
		{
			get
			{
				TextObject textObject = new TextObject("{=SyuxD1aY}You delivered the prisoners to {QUEST_GIVER.LINK} as promised and you shared half of the profit with the headman and the villagers.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject QuestFailPlayerAcceptedCounterOfferLog
		{
			get
			{
				TextObject textObject = new TextObject("{=u8DJ8a2D}You agreed with the headman to break your agreement with {QUEST_GIVER.LINK}. By doing so you protected the villagers' jobs and now they are grateful to you.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject QuestFailedWithTimeOutLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=b1lsZNxE}You failed to deliver enough prisoners in time. {QUEST_GIVER.LINK} must be disappointed.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject QuestDeliveredRequestedPrisonersLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=YiPUbIII}You have delivered the requested number of prisoners to {QUEST_GIVER.LINK}. You may settle your accounts with {?QUEST_GIVER.GENDER}her{?}him{\\?} at any point to receive your pay, or continue to bring more captives.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsLandLordNeedsManualLaborersIssueQuest(object o, List<object> collectedObjects)
		{
			((LandLordNeedsManualLaborersIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_questProgressLogTest);
		}

		internal static object AutoGeneratedGetMemberValue_requestedPrisonerCount(object o)
		{
			return ((LandLordNeedsManualLaborersIssueQuest)o)._requestedPrisonerCount;
		}

		internal static object AutoGeneratedGetMemberValue_shareProfit(object o)
		{
			return ((LandLordNeedsManualLaborersIssueQuest)o)._shareProfit;
		}

		internal static object AutoGeneratedGetMemberValue_counterOfferGiven(object o)
		{
			return ((LandLordNeedsManualLaborersIssueQuest)o)._counterOfferGiven;
		}

		internal static object AutoGeneratedGetMemberValue_deliveredPrisonerCount(object o)
		{
			return ((LandLordNeedsManualLaborersIssueQuest)o)._deliveredPrisonerCount;
		}

		internal static object AutoGeneratedGetMemberValue_rewardGold(object o)
		{
			return ((LandLordNeedsManualLaborersIssueQuest)o)._rewardGold;
		}

		internal static object AutoGeneratedGetMemberValue_maximumPrisonerCount(object o)
		{
			return ((LandLordNeedsManualLaborersIssueQuest)o)._maximumPrisonerCount;
		}

		internal static object AutoGeneratedGetMemberValue_playerReachedMaximumAmount(object o)
		{
			return ((LandLordNeedsManualLaborersIssueQuest)o)._playerReachedMaximumAmount;
		}

		internal static object AutoGeneratedGetMemberValue_questProgressLogTest(object o)
		{
			return ((LandLordNeedsManualLaborersIssueQuest)o)._questProgressLogTest;
		}

		public LandLordNeedsManualLaborersIssueQuest(string questId, Hero giverHero, CampaignTime duration, int requestedPrisonerCount)
			: base(questId, giverHero, duration, 0)
		{
			_requestedPrisonerCount = requestedPrisonerCount;
			_shareProfit = false;
			_deliveredPrisonerCount = 0;
			_rewardGold = 0;
			_counterOfferGiven = false;
			_maximumPrisonerCount = MBRandom.RandomInt(40, 60);
			_playerReachedMaximumAmount = false;
			SetDialogs();
			InitializeQuestOnCreation();
		}

		private void QuestAcceptedConsequences()
		{
			StartQuest();
			AddTrackedObject(base.QuestGiver.CurrentSettlement);
			TextObject textObject = new TextObject("{=N4eLGduQ}Delivered Prisoners ({TOTAL_REWARD}{GOLD_ICON})");
			textObject.SetTextVariable("TOTAL_REWARD", _rewardGold);
			textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
			_questProgressLogTest = AddDiscreteLog(QuestStartedLogText, textObject, _deliveredPrisonerCount, _requestedPrisonerCount);
		}

		private DialogFlow GetCounterOfferDialogFlow()
		{
			TextObject textObject = new TextObject("{=aJHMafam}{?PLAYER.GENDER}Madam{?}Sir{\\?} - a moment of your time! [if:convo_undecided_open][ib:nervous2]You're on a job for {ISSUE_GIVER.LINK}, am I right? Look - our people are depending on those jobs. {?ISSUE_GIVER.GENDER}She{?}He{\\?} doesn't need to pay us a living wage if {?ISSUE_GIVER.GENDER}she{?}he{\\?} can do the work with prisoners. Please - break your agreement. You don't have to do this.");
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
			StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.QuestGiver.CharacterObject, textObject);
			TextObject textObject2 = new TextObject("{=8BdJ2MZj}I don't want to hurt your people. I'll forget my deal with {ISSUE_GIVER.LINK}.");
			StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.QuestGiver.CharacterObject, textObject2);
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject).Condition(() => !_counterOfferGiven && Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero == base.QuestGiver.CurrentSettlement.Notables.FirstOrDefault((Hero x) => x.IsHeadman))
				.Consequence(delegate
				{
					_counterOfferGiven = true;
				})
				.BeginPlayerOptions()
				.PlayerOption("{=rOWyAHPo}Mind your own business, Headman.")
				.NpcLine("{=tLapkQqg}We won't forget this![if:convo_thinking][ib:aggressive]")
				.Consequence(delegate
				{
					ChangeRelationAction.ApplyPlayerRelation(Hero.OneToOneConversationHero, -3);
				})
				.CloseDialog()
				.PlayerOption("{=I3GUAb9a}I understand your concern, Headman, but I made an agreement. How about I share the profit with you and your people?")
				.NpcLine("{=MmQt0TD3}This is not we want, but it's more than nothing.[if:convo_dismayed][ib:normal]")
				.Consequence(ShareTheProfit)
				.CloseDialog()
				.PlayerOption(textObject2)
				.NpcLine("{=E5yAFR6y}Good to hear that. Thank you.[if:convo_relaxed_happy][ib:normal]")
				.Consequence(QuestFailPlayerAcceptedCounterOffer)
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
		}

		private void ShareTheProfit()
		{
			AddLog(PlayerSharedTheProfitWithHeadManLogText);
			ChangeRelationAction.ApplyPlayerRelation(Hero.OneToOneConversationHero, 3);
			_shareProfit = true;
		}

		protected override void SetDialogs()
		{
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(new TextObject("{=l7dL6arZ}Thank you. Remember - they need to be looters or bandits. Anyone else I can't put to work.[if:convo_mocking_teasing][ib:closed]")).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.Consequence(QuestAcceptedConsequences)
				.CloseDialog();
			TextObject npcDiscussLine = new TextObject("{=!}{MANUAL_LABORERS_QUEST_NOTABLE_DISCUSS}");
			TextObject npcResponseLine = new TextObject("{=!}{MANUAL_LABORERS_QUEST_NOTABLE_RESPONSE}");
			bool changeDialogAfterTransfer = false;
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").BeginNpcOptions().NpcOption(new TextObject("{=VRLbAaYe}Thank you for saving my business {?PLAYER.GENDER}madam{?}sir{\\?}. Here is your payment."), () => Hero.OneToOneConversationHero == base.QuestGiver && _playerReachedMaximumAmount)
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
						npcDiscussLine.SetTextVariable("MANUAL_LABORERS_QUEST_NOTABLE_DISCUSS", new TextObject("{=B0YOpGsZ}Any news about my prisoners?[if:convo_mocking_teasing][ib:hip]"));
					}
					else
					{
						npcDiscussLine.SetTextVariable("MANUAL_LABORERS_QUEST_NOTABLE_DISCUSS", new TextObject("{=ds294zxi}Anything else?"));
						changeDialogAfterTransfer = false;
					}
					return true;
				})
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=QbaOoilS}Yes, I have brought you a few men."))
				.Condition(() => CheckIfThereIsSuitablePrisonerInPlayer() && !_playerReachedMaximumAmount)
				.NpcLine(npcResponseLine)
				.Condition(delegate
				{
					if (_playerReachedMaximumAmount)
					{
						return false;
					}
					if ((float)_deliveredPrisonerCount < (float)_maximumPrisonerCount * 0.75f)
					{
						npcResponseLine.SetTextVariable("MANUAL_LABORERS_QUEST_NOTABLE_RESPONSE", new TextObject("{=0ewaZnfe}Very good. Keep them coming.[if:convo_mocking_aristocratic]"));
					}
					else
					{
						TextObject textObject = new TextObject("{=CBBPWMZd}Thanks to you, my mines are full {?PLAYER.GENDER}madam{?}sir{\\?}. I will only buy {X} more, then we're done.");
						textObject.SetTextVariable("X", _maximumPrisonerCount - _deliveredPrisonerCount);
						npcResponseLine.SetTextVariable("MANUAL_LABORERS_QUEST_NOTABLE_RESPONSE", textObject);
						changeDialogAfterTransfer = false;
					}
					return true;
				})
				.PlayerLine(new TextObject("{=IULW8h03}Sure."))
				.Consequence(delegate
				{
					changeDialogAfterTransfer = true;
					OpenPrisonerDeliveryScreen();
				})
				.NpcLine(new TextObject("{=!}Party screen goes here."))
				.GotoDialogState("quest_discuss")
				.PlayerOption(new TextObject("{=UOE7ejgq}Here is all I've got. Let's settle up and finish this business."))
				.Condition(() => !_playerReachedMaximumAmount && _deliveredPrisonerCount >= _requestedPrisonerCount)
				.NpcLine(new TextObject("{=YZ6UmX5o}Certainly. Here is your payment as I promised. Thank you.[if:convo_mocking_teasing][ib:confident3]"))
				.Consequence(delegate
				{
					ApplyQuestSuccessConsequences();
					CompleteQuestWithSuccess();
				})
				.CloseDialog()
				.PlayerOption(new TextObject("{=wErSpkjy}I'm still working on it."))
				.NpcLine(new TextObject("{=oiLdjqwe}I am glad to hear that.[if:convo_nonchalant]"))
				.CloseDialog()
				.EndPlayerOptions();
			if (IsQuestWithCounterOffer)
			{
				QuestCharacterDialogFlow = GetCounterOfferDialogFlow();
			}
		}

		private void OpenPrisonerDeliveryScreen()
		{
			PartyScreenHelper.OpenScreenWithCondition(IsTroopTransferable, DoneButtonCondition, OnDoneClicked, OnCancelClicked, PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.Transferable, base.QuestGiver.Name, _maximumPrisonerCount - _deliveredPrisonerCount, showProgressBar: true, isDonating: false, PartyScreenHelper.PartyScreenMode.PrisonerManage);
		}

		private void OnCancelClicked()
		{
			Campaign.Current.ConversationManager.ContinueConversation();
		}

		private Tuple<bool, TextObject> DoneButtonCondition(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum)
		{
			if (_maximumPrisonerCount - _deliveredPrisonerCount < leftPrisonRoster.TotalManCount)
			{
				int num = _maximumPrisonerCount - _deliveredPrisonerCount;
				TextObject textObject = new TextObject("{=bgXebaRF}You can only transfer {X} prisoner{?IS_PLURAL}s{?}{\\?}.");
				textObject.SetTextVariable("IS_PLURAL", (num > 1) ? 1 : 0);
				textObject.SetTextVariable("X", num);
				return new Tuple<bool, TextObject>(item1: false, textObject);
			}
			return new Tuple<bool, TextObject>(item1: true, null);
		}

		private bool OnDoneClicked(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, PartyBase leftParty, PartyBase rightParty)
		{
			foreach (TroopRosterElement item in leftPrisonRoster.GetTroopRoster())
			{
				_rewardGold += (int)((float)(Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(item.Character, Hero.MainHero) * item.Number) * 5f);
			}
			int deliveredPrisonerCount = _deliveredPrisonerCount;
			_deliveredPrisonerCount += releasedPrisonerRoster.Count();
			int deliveredPrisonerCount2 = _deliveredPrisonerCount;
			bool flag = deliveredPrisonerCount < _requestedPrisonerCount && deliveredPrisonerCount2 >= _requestedPrisonerCount;
			if (deliveredPrisonerCount2 != _maximumPrisonerCount && flag)
			{
				AddLog(QuestDeliveredRequestedPrisonersLogText);
			}
			_questProgressLogTest.UpdateCurrentProgress(_deliveredPrisonerCount);
			_questProgressLogTest.TaskName.SetTextVariable("TOTAL_REWARD", _rewardGold);
			if (_deliveredPrisonerCount == _maximumPrisonerCount)
			{
				_playerReachedMaximumAmount = true;
			}
			Campaign.Current.ConversationManager.ContinueConversation();
			return true;
		}

		private bool IsTroopTransferable(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftOwnerParty)
		{
			if (side != PartyScreenLogic.PartyRosterSide.Left)
			{
				if (_maximumPrisonerCount - _deliveredPrisonerCount >= 0 && MobileParty.MainParty.PrisonRoster.Contains(character))
				{
					return character.Occupation == Occupation.Bandit;
				}
				return false;
			}
			return true;
		}

		private bool CheckIfThereIsSuitablePrisonerInPlayer()
		{
			bool result = false;
			foreach (TroopRosterElement item in MobileParty.MainParty.PrisonRoster.GetTroopRoster())
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
			if (_shareProfit)
			{
				AddLog(QuestSuccessWithProfitShareLog);
				TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, 30)
				});
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, _rewardGold / 2);
				if (IsQuestWithCounterOffer)
				{
					base.QuestGiver.CurrentSettlement.Notables.First((Hero x) => x.IsHeadman).AddPower(5f);
				}
			}
			else
			{
				AddLog(QuestSuccessLog);
				TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[2]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, 30),
					new Tuple<TraitObject, int>(DefaultTraits.Mercy, -20)
				});
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, _rewardGold);
				if (IsQuestWithCounterOffer && _counterOfferGiven)
				{
					ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver.CurrentSettlement.Notables.First((Hero x) => x.IsHeadman), -5);
					base.QuestGiver.CurrentSettlement.Notables.First((Hero x) => x.IsHeadman).AddPower(-10f);
				}
			}
			base.QuestGiver.AddPower(10f);
			RelationshipChangeWithQuestGiver = 5;
		}

		private void QuestFailPlayerAcceptedCounterOffer()
		{
			AddLog(QuestFailPlayerAcceptedCounterOfferLog);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[3]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Mercy, 20),
				new Tuple<TraitObject, int>(DefaultTraits.Generosity, 20),
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -10)
			});
			base.QuestGiver.AddPower(-10f);
			RelationshipChangeWithQuestGiver = -3;
			Hero hero = base.QuestGiver.CurrentSettlement.Notables.First((Hero x) => x.IsHeadman);
			hero.AddPower(10f);
			ChangeRelationAction.ApplyPlayerRelation(hero, 5);
			CompleteQuestWithFail();
		}

		protected override void OnBeforeTimedOut(ref bool completeWithSuccess, ref bool doNotResolveTheQuest)
		{
			if (_deliveredPrisonerCount >= _requestedPrisonerCount)
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

		protected override void HourlyTick()
		{
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
			CampaignEvents.VillageBeingRaided.AddNonSerializedListener(this, OnVillageRaided);
			CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
		}

		private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
			{
				QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
			}
		}

		private void OnGameMenuOpened(MenuCallbackArgs args)
		{
			if (IsQuestWithCounterOffer && !_counterOfferGiven && args.MenuContext.GameMenu.StringId == "village" && Settlement.CurrentSettlement == base.QuestGiver.CurrentSettlement && Campaign.Current.GameMenuManager.NextLocation == null && GameStateManager.Current.ActiveState is MapState && PlayerEncounter.EncounterSettlement != null && CheckIfThereIsSuitablePrisonerInPlayer())
			{
				Hero hero = base.QuestGiver.CurrentSettlement.Notables.First((Hero x) => x.IsHeadman);
				CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty), new ConversationCharacterData(hero.CharacterObject));
			}
		}

		private void OnVillageRaided(Village village)
		{
			if (village == base.QuestGiver.CurrentSettlement.Village)
			{
				AddLog(QuestGiverVillageRaided);
				CompleteQuestWithCancel();
			}
		}

		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (base.QuestGiver.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				CompleteQuestWithCancel(QuestCanceledWarDeclaredLog);
			}
		}

		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
		{
			QuestHelper.CheckWarDeclarationAndFailOrCancelTheQuest(this, faction1, faction2, detail, PlayerDeclaredWarQuestLogText, QuestCanceledWarDeclaredLog);
		}

		protected override void OnFinalize()
		{
		}

		protected override void InitializeQuestOnGameLoad()
		{
			SetDialogs();
		}
	}

	public class LandLordNeedsManualLaborersIssueBehaviorTypeDefiner : SaveableTypeDefiner
	{
		public LandLordNeedsManualLaborersIssueBehaviorTypeDefiner()
			: base(810000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(LandLordNeedsManualLaborersIssue), 1);
			AddClassDefinition(typeof(LandLordNeedsManualLaborersIssueQuest), 2);
		}
	}

	private const IssueBase.IssueFrequency NeedsLaborersIssueFrequency = IssueBase.IssueFrequency.VeryCommon;

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private bool ConditionsHold(Hero issueGiver)
	{
		if (issueGiver.CurrentSettlement != null && issueGiver.IsRuralNotable && issueGiver.GetTraitLevel(DefaultTraits.Mercy) <= 0)
		{
			Village village = issueGiver.CurrentSettlement.Village;
			if (village != null)
			{
				if (village.VillageType != DefaultVillageTypes.IronMine && village.VillageType != DefaultVillageTypes.ClayMine && village.VillageType != DefaultVillageTypes.SaltMine)
				{
					return village.VillageType == DefaultVillageTypes.SilverMine;
				}
				return true;
			}
			return false;
		}
		return false;
	}

	public void OnCheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero))
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnSelected, typeof(LandLordNeedsManualLaborersIssue), IssueBase.IssueFrequency.VeryCommon));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(LandLordNeedsManualLaborersIssue), IssueBase.IssueFrequency.VeryCommon));
		}
	}

	private IssueBase OnSelected(in PotentialIssueData pid, Hero issueOwner)
	{
		return new LandLordNeedsManualLaborersIssue(issueOwner);
	}
}
