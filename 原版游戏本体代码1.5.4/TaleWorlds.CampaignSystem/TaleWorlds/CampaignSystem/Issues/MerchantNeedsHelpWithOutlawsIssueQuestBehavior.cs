using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues;

public class MerchantNeedsHelpWithOutlawsIssueQuestBehavior : CampaignBehaviorBase
{
	public class MerchantNeedsHelpWithOutlawsIssue : IssueBase
	{
		private const int IssueDuration = 15;

		private const int QuestTimeLimit = 20;

		private const int MinimumRequiredMenCount = 5;

		private const int AlternativeSolutionMinimumSkillValue = 120;

		private const int AlternativeSolutionTroopTierRequirement = 2;

		[SaveableField(10)]
		private Hideout RelatedHideout;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.Casualties | AlternativeSolutionScaleFlag.FailureRisk;

		private int TotalPartyCount => (int)(2f + 6f * base.IssueDifficultyMultiplier);

		public override int AlternativeSolutionBaseNeededMenCount => 8 + TaleWorlds.Library.MathF.Ceiling(11f * base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 5 + TaleWorlds.Library.MathF.Ceiling(7f * base.IssueDifficultyMultiplier);

		protected override int RewardGold => (int)(400f + 1500f * base.IssueDifficultyMultiplier);

		public override TextObject IssueBriefByIssueGiver => new TextObject("{=ib6ltlM0}Yes... We've always had trouble with bandits, but recently we've had a lot more than our share. The hills outside of town are infested. A lot of us are afraid to take their goods to market. Some have been murdered. People tell me, 'I'm getting so desperate, maybe I'll turn bandit myself.' It's bad...[ib:demure2][if:convo_dismayed]");

		public override TextObject IssueAcceptByPlayer => new TextObject("{=qNxdWLFY}So you want me to hunt them down?");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=DlRMT7XD}Well, {?PLAYER.GENDER}my lady{?}sir{\\?}, you'll never get all those outlaws,[if:convo_thinking] but if word gets around that you took down some of the most vicious ones - let's say {TOTAL_COUNT} bands of brigands - robbing us wouldn't seem so lucrative. Maybe the rest would go bother someone else... Do you think you can help us?");
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
				textObject.SetTextVariable("TOTAL_COUNT", TotalPartyCount);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=5RjvnQ3d}I bet even a party of {ALTERNATIVE_COUNT} properly trained men accompanied by one of your lieutenants can handle any band they find. Give them {TOTAL_DAYS} days, say... That will make a difference.[if:convo_undecided_open]");
				textObject.SetTextVariable("ALTERNATIVE_COUNT", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("TOTAL_DAYS", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		public override TextObject IssuePlayerResponseAfterAlternativeExplanation => new TextObject("{=BPfuSkCl}That depends. How many men do you think are required to get the job done?");

		public override TextObject IssueQuestSolutionAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=2ApU6iCB}I'll hunt down {TOTAL_COUNT} bands of brigands for you.");
				textObject.SetTextVariable("TOTAL_COUNT", TotalPartyCount);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=DLbFbYkR}I will have one of my companions and {ALTERNATIVE_COUNT} of my men patrol the area for {TOTAL_DAYS} days.");
				textObject.SetTextVariable("ALTERNATIVE_COUNT", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("TOTAL_DAYS", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		public override TextObject IssueDiscussAlternativeSolution => new TextObject("{=PexmGuOd}{?PLAYER.GENDER}Madam{?}Sir{\\?}, I am happy to tell that the men you left are patrolling, and already we feel safer. Thank you again.[ib:demure][if:convo_grateful]");

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=FYfZFve3}Thank you, {?PLAYER.GENDER}my lady{?}my lord{\\?}. Hopefully, we can travel safely again.");
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
				return textObject;
			}
		}

		public override bool IsThereAlternativeSolution => true;

		public override bool IsThereLordSolution => false;

		protected override int CompanionSkillRewardXP => (int)(600f + 800f * base.IssueDifficultyMultiplier);

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=Bdt41knf}You have accepted {QUEST_GIVER.LINK}'s request to find at least {TOTAL_COUNT} different parties of brigands around {QUEST_SETTLEMENT} and sent {COMPANION.LINK} and with {?COMPANION.GENDER}her{?}his{\\?} {ALTERNATIVE_COUNT} of your men to deal with them. They should return with the reward of {GOLD_AMOUNT}{GOLD_ICON} denars as promised by {QUEST_GIVER.LINK} after dealing with them in {RETURN_DAYS} days.");
				textObject.SetTextVariable("TOTAL_COUNT", TotalPartyCount);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_SETTLEMENT", base.IssueOwner.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("ALTERNATIVE_COUNT", AlternativeSolutionSentTroops.TotalManCount - 1);
				textObject.SetTextVariable("GOLD_AMOUNT", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=ABmCO23x}{QUEST_GIVER.NAME} Needs Help With Brigands");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject Description => new TextObject("{=sAobCa9U}Brigands are disturbing travelers outside the town. Someone needs to hunt them down.");

		internal static void AutoGeneratedStaticCollectObjectsMerchantNeedsHelpWithOutlawsIssue(object o, List<object> collectedObjects)
		{
			((MerchantNeedsHelpWithOutlawsIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(RelatedHideout);
		}

		internal static object AutoGeneratedGetMemberValueRelatedHideout(object o)
		{
			return ((MerchantNeedsHelpWithOutlawsIssue)o).RelatedHideout;
		}

		public MerchantNeedsHelpWithOutlawsIssue(Hero issueOwner, Hideout relatedHideout)
			: base(issueOwner, CampaignTime.DaysFromNow(15f))
		{
			RelatedHideout = relatedHideout;
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.SettlementProsperity)
			{
				return -0.2f;
			}
			if (issueEffect == DefaultIssueEffects.IssueOwnerPower)
			{
				return -0.1f;
			}
			if (issueEffect == DefaultIssueEffects.SettlementSecurity)
			{
				return -1f;
			}
			return 0f;
		}

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			return ((hero.GetSkillValue(DefaultSkills.Tactics) >= hero.GetSkillValue(DefaultSkills.Scouting)) ? DefaultSkills.Tactics : DefaultSkills.Scouting, 120);
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

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			RelationshipChangeWithIssueOwner = 3;
			if (base.IssueOwner.CurrentSettlement.IsVillage && base.IssueOwner.CurrentSettlement.Village.TradeBound != null)
			{
				base.IssueOwner.CurrentSettlement.Village.Bound.Town.Security += 5f;
				base.IssueOwner.CurrentSettlement.Village.Bound.Town.Prosperity += 5f;
			}
			else if (base.IssueOwner.CurrentSettlement.IsTown)
			{
				base.IssueOwner.CurrentSettlement.Town.Security += 5f;
				base.IssueOwner.CurrentSettlement.Town.Prosperity += 5f;
			}
			Hero.MainHero.Clan.AddRenown(1f);
		}

		protected override void AlternativeSolutionEndWithFailureConsequence()
		{
			if (base.IssueOwner.CurrentSettlement.IsVillage)
			{
				base.IssueOwner.CurrentSettlement.Village.Bound.Town.Prosperity -= 10f;
			}
			else if (base.IssueOwner.CurrentSettlement.IsTown)
			{
				base.IssueOwner.CurrentSettlement.Town.Prosperity -= 10f;
			}
			RelationshipChangeWithIssueOwner = -5;
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
			if (MobileParty.MainParty.MemberRoster.TotalHealthyCount < 5)
			{
				flag |= PreconditionFlags.NotEnoughTroops;
			}
			if (issueGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				flag |= PreconditionFlags.AtWar;
			}
			return flag == PreconditionFlags.None;
		}

		public override bool IssueStayAliveConditions()
		{
			if (!base.IssueOwner.CurrentSettlement.IsRaided && !base.IssueOwner.CurrentSettlement.IsUnderRaid && RelatedHideout != null)
			{
				return RelatedHideout.IsInfested;
			}
			return false;
		}

		protected override void OnGameLoad()
		{
			if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.9") && RelatedHideout == null)
			{
				CompleteIssueWithCancel();
			}
		}

		public override void IsSettlementBusy(Settlement settlement, object asker, ref int priority)
		{
			if (asker != this && settlement == RelatedHideout.Settlement)
			{
				priority = Math.Max(priority, 100);
			}
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new MerchantNeedsHelpWithOutlawsIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(20f), RewardGold, TotalPartyCount, RelatedHideout);
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}

		protected override void OnIssueFinalized()
		{
		}
	}

	public class MerchantNeedsHelpWithOutlawsIssueQuest : QuestBase
	{
		[SaveableField(10)]
		private readonly int _totalPartyCount;

		[SaveableField(30)]
		private int _destroyedPartyCount;

		[SaveableField(50)]
		private int _recruitedPartyCount;

		[SaveableField(40)]
		private List<MobileParty> _validPartiesList;

		[SaveableField(70)]
		private Hideout _relatedHideout;

		private const float ValidBanditPartyEnableAiChance = 0.05f;

		[SaveableField(60)]
		private JournalLog _questProgressLogTest;

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=PBGiIbEM}{ISSUE_GIVER.NAME} Needs Help With Brigands");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		public override bool IsRemainingTimeHidden => false;

		private int _questPartyProgress => _destroyedPartyCount + _recruitedPartyCount;

		private TextObject PlayerStartsQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=6iLxrDBa}You have accepted {QUEST_GIVER.LINK}'s request to find at least {TOTAL_COUNT} different parties of brigands around {QUEST_SETTLEMENT} and decided to hunt them down personally. {?QUEST_GIVER.GENDER}She{?}He{\\?} will reward you {AMOUNT}{GOLD_ICON} gold once you have dealt with them.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("TOTAL_COUNT", _totalPartyCount);
				textObject.SetTextVariable("QUEST_SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("AMOUNT", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		private TextObject SuccessQuestLogText1
		{
			get
			{
				TextObject textObject = new TextObject("{=cQ6CzXKM}You have defeated all the brigands as {QUEST_GIVER.LINK} has asked. {?QUEST_GIVER.GENDER}She{?}He{\\?} is grateful. And sends you the reward, {GOLD_AMOUNT}{GOLD_ICON} gold as promised.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", base.QuestGiver.CurrentSettlement.Name);
				textObject.SetTextVariable("GOLD_AMOUNT", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		private TextObject SuccessQuestLogText2
		{
			get
			{
				TextObject textObject = new TextObject("{=dSHgU9gD}You have defeated some of the brigands and recruited the rest into your party. {QUEST_GIVER.LINK} is grateful and sends you the {GOLD_AMOUNT}{GOLD_ICON} as promised. ");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("GOLD_AMOUNT", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		private TextObject SuccessQuestLogText3
		{
			get
			{
				TextObject textObject = new TextObject("{=3V5udYJO}You have recruited the brigands into your party. {QUEST_GIVER.LINK} finds your solution acceptable and sends you the {GOLD_AMOUNT}{GOLD_ICON} as promised.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("GOLD_AMOUNT", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		private TextObject TimeoutLog
		{
			get
			{
				TextObject textObject = new TextObject("{=Tcux6Sru}You have failed to defeat all {TOTAL_COUNT} outlaw parties in time as {QUEST_GIVER.LINK} asked. {?QUEST_GIVER.GENDER}She{?}He{\\?} is very disappointed.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("TOTAL_COUNT", _totalPartyCount);
				return textObject;
			}
		}

		private TextObject QuestGiverVillageRaided
		{
			get
			{
				TextObject textObject = new TextObject("{=4rCIZ6e5}{QUEST_SETTLEMENT} was raided, Your agreement with {QUEST_GIVER.LINK} is canceled.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
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
				TextObject textObject = new TextObject("{=DDur6mHb}Your actions have started a war with {ISSUE_GIVER.LINK}'s faction. Your agreement with {ISSUE_GIVER.LINK} is failed.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsMerchantNeedsHelpWithOutlawsIssueQuest(object o, List<object> collectedObjects)
		{
			((MerchantNeedsHelpWithOutlawsIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_validPartiesList);
			collectedObjects.Add(_relatedHideout);
			collectedObjects.Add(_questProgressLogTest);
		}

		internal static object AutoGeneratedGetMemberValue_totalPartyCount(object o)
		{
			return ((MerchantNeedsHelpWithOutlawsIssueQuest)o)._totalPartyCount;
		}

		internal static object AutoGeneratedGetMemberValue_destroyedPartyCount(object o)
		{
			return ((MerchantNeedsHelpWithOutlawsIssueQuest)o)._destroyedPartyCount;
		}

		internal static object AutoGeneratedGetMemberValue_recruitedPartyCount(object o)
		{
			return ((MerchantNeedsHelpWithOutlawsIssueQuest)o)._recruitedPartyCount;
		}

		internal static object AutoGeneratedGetMemberValue_validPartiesList(object o)
		{
			return ((MerchantNeedsHelpWithOutlawsIssueQuest)o)._validPartiesList;
		}

		internal static object AutoGeneratedGetMemberValue_relatedHideout(object o)
		{
			return ((MerchantNeedsHelpWithOutlawsIssueQuest)o)._relatedHideout;
		}

		internal static object AutoGeneratedGetMemberValue_questProgressLogTest(object o)
		{
			return ((MerchantNeedsHelpWithOutlawsIssueQuest)o)._questProgressLogTest;
		}

		public MerchantNeedsHelpWithOutlawsIssueQuest(string questId, Hero giverHero, CampaignTime duration, int rewardGold, int totalPartyCount, Hideout relatedHideout)
			: base(questId, giverHero, duration, rewardGold)
		{
			_totalPartyCount = totalPartyCount;
			_destroyedPartyCount = 0;
			_recruitedPartyCount = 0;
			_validPartiesList = new List<MobileParty>();
			_relatedHideout = relatedHideout;
			AddHideoutPartiesToValidPartiesList();
			SetDialogs();
			InitializeQuestOnCreation();
		}

		protected override void SetDialogs()
		{
			TextObject textObject = new TextObject("{=PQIYPCDn}Very good. I will be waiting for the good news then. Once you return, I'm ready to offer a reward of {REWARD_GOLD}{GOLD_ICON} denars. Just make sure that you defeat at least {TROOP_COUNT} bands no more than a day's ride away from here.[ib:normal][if:convo_bemused]");
			textObject.SetTextVariable("REWARD_GOLD", RewardGold);
			textObject.SetTextVariable("TROOP_COUNT", _totalPartyCount);
			textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(textObject).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.Consequence(QuestAcceptedConsequences)
				.CloseDialog();
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(new TextObject("{=jjTcNhKE}Have you been able to find any bandits yet?[if:convo_undecided_open]")).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=mU45Th70}We're off to hunt them now."))
				.NpcLine(new TextObject("{=u9vtceCV}You are a savior.[if:convo_astonished]"))
				.CloseDialog()
				.Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.PlayerOption(new TextObject("{=QPv1b7f8}I haven't had the time yet."))
				.NpcLine(new TextObject("{=6ba4n9n6}We are waiting for your good news {?PLAYER.GENDER}my lady{?}sir{\\?}.[if:convo_focused_happy]"))
				.CloseDialog()
				.Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.EndPlayerOptions()
				.CloseDialog();
		}

		private void QuestAcceptedConsequences()
		{
			StartQuest();
			_questProgressLogTest = AddDiscreteLog(PlayerStartsQuestLogText, new TextObject("{=HzcLsnYn}Destroyed parties"), _destroyedPartyCount, _totalPartyCount, null, hideInformation: true);
		}

		private void AddQuestStepLog()
		{
			_questProgressLogTest.UpdateCurrentProgress(_questPartyProgress);
			if (_questPartyProgress >= _totalPartyCount)
			{
				SuccessConsequences();
				return;
			}
			TextObject textObject = new TextObject("{=xbVCRbUu}You hunted {CURRENT_COUNT}/{TOTAL_COUNT} gang of brigands.");
			textObject.SetTextVariable("CURRENT_COUNT", _questPartyProgress);
			textObject.SetTextVariable("TOTAL_COUNT", _totalPartyCount);
			MBInformationManager.AddQuickInformation(textObject);
		}

		protected override void HourlyTick()
		{
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, MobilePartyDestroyed);
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
			CampaignEvents.VillageBeingRaided.AddNonSerializedListener(this, OnVillageRaided);
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
			CampaignEvents.BanditPartyRecruited.AddNonSerializedListener(this, OnBanditPartyRecruited);
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
			CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
			CampaignEvents.IsSettlementBusyEvent.AddNonSerializedListener(this, IsSettlementBusy);
		}

		private void IsSettlementBusy(Settlement settlement, object asker, ref int priority)
		{
			if (asker != this && settlement == _relatedHideout.Settlement)
			{
				priority = Math.Max(priority, 200);
			}
		}

		private void OnGameLoadFinished()
		{
			if (_relatedHideout == null && MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.9"))
			{
				Hideout hideout = SettlementHelper.FindNearestHideoutToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.Default, (Settlement x) => x.Hideout.IsInfested);
				if (hideout != null && Campaign.Current.Models.MapDistanceModel.GetDistance(base.QuestGiver.CurrentSettlement, hideout.Settlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default) < Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Default) * 1.25f)
				{
					_relatedHideout = hideout;
				}
				if (_relatedHideout != null)
				{
					AddHideoutPartiesToValidPartiesList();
				}
				else
				{
					CompleteQuestWithCancel();
				}
			}
			if (_relatedHideout != null && base.IsOngoing && MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.9") && _relatedHideout.Settlement.IsSettlementBusy(this))
			{
				CompleteQuestWithCancel();
			}
		}

		private void AddHideoutPartiesToValidPartiesList()
		{
			foreach (MobileParty party in _relatedHideout.Settlement.Parties)
			{
				if (party.IsBandit)
				{
					_validPartiesList.Add(party);
				}
			}
		}

		private void OnSettlementLeft(MobileParty party, Settlement settlement)
		{
			if (_validPartiesList.Contains(party) && settlement.IsHideout && settlement.Hideout == _relatedHideout)
			{
				_validPartiesList.Remove(party);
			}
		}

		private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
		{
			if (party != null && party.IsBandit && settlement.IsHideout && settlement.Hideout == _relatedHideout)
			{
				_validPartiesList.Add(party);
			}
		}

		private void OnBanditPartyRecruited(MobileParty banditParty)
		{
			if (_validPartiesList.Contains(banditParty))
			{
				_recruitedPartyCount++;
				_validPartiesList.Remove(banditParty);
				AddQuestStepLog();
			}
		}

		private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
			{
				QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
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

		private void MobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
		{
			if (destroyerParty == PartyBase.MainParty && _validPartiesList.Contains(mobileParty))
			{
				_destroyedPartyCount++;
				AddQuestStepLog();
			}
		}

		protected override void HourlyTickParty(MobileParty mobileParty)
		{
			if (!base.IsOngoing || !mobileParty.IsBandit || mobileParty.MapEvent != null || !mobileParty.MapFaction.IsBanditFaction || mobileParty.IsCurrentlyUsedByAQuest || mobileParty.IsCurrentlyAtSea)
			{
				return;
			}
			if (((mobileParty.CurrentSettlement != null) ? Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty.CurrentSettlement, base.QuestGiver.CurrentSettlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default, out var landRatio) : Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty, base.QuestGiver.CurrentSettlement, isTargetingPort: false, MobileParty.NavigationType.Default, out landRatio)) <= ValidBanditPartyDistance)
			{
				if (!_validPartiesList.Contains(mobileParty))
				{
					if (!IsTracked(mobileParty))
					{
						AddTrackedObject(mobileParty);
					}
					_validPartiesList.Add(mobileParty);
					if (mobileParty.CurrentSettlement == null && MBRandom.RandomFloat < 1f / (float)_validPartiesList.Count)
					{
						SetPartyAiAction.GetActionForPatrollingAroundSettlement(mobileParty, base.QuestGiver.CurrentSettlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
						mobileParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
						mobileParty.IgnoreForHours(500f);
					}
				}
				else if (MBRandom.RandomFloat < 0.05f)
				{
					mobileParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: false);
				}
			}
			else if (IsTracked(mobileParty))
			{
				RemoveTrackedObject(mobileParty);
				_validPartiesList.Remove(mobileParty);
				mobileParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: false);
			}
		}

		private void SuccessConsequences()
		{
			if (_destroyedPartyCount == _totalPartyCount)
			{
				AddLog(SuccessQuestLogText1);
			}
			else if (_recruitedPartyCount != 0 && _recruitedPartyCount < _totalPartyCount)
			{
				AddLog(SuccessQuestLogText2);
			}
			else
			{
				AddLog(SuccessQuestLogText3);
			}
			RelationshipChangeWithQuestGiver = 3;
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
			if (base.QuestGiver.CurrentSettlement.IsVillage && base.QuestGiver.CurrentSettlement.Village.TradeBound != null)
			{
				base.QuestGiver.CurrentSettlement.Village.Bound.Town.Security += 5f;
				base.QuestGiver.CurrentSettlement.Village.Bound.Town.Prosperity += 5f;
			}
			else if (base.QuestGiver.CurrentSettlement.IsTown)
			{
				base.QuestGiver.CurrentSettlement.Town.Security += 5f;
				base.QuestGiver.CurrentSettlement.Town.Prosperity += 5f;
			}
			Hero.MainHero.Clan.AddRenown(1f);
			CompleteQuestWithSuccess();
		}

		protected override void OnTimedOut()
		{
			RelationshipChangeWithQuestGiver = -5;
			if (base.QuestGiver.CurrentSettlement.IsVillage)
			{
				base.QuestGiver.CurrentSettlement.Village.Bound.Town.Prosperity -= 10f;
			}
			else if (base.QuestGiver.CurrentSettlement.IsTown)
			{
				base.QuestGiver.CurrentSettlement.Town.Prosperity -= 10f;
			}
			AddLog(TimeoutLog);
		}

		protected override void InitializeQuestOnGameLoad()
		{
			SetDialogs();
		}

		protected override void OnFinalize()
		{
			foreach (MobileParty validParties in _validPartiesList)
			{
				validParties.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: false);
				validParties.IgnoreForHours(0f);
				if (IsTracked(validParties))
				{
					RemoveTrackedObject(validParties);
				}
			}
			_validPartiesList.Clear();
		}
	}

	public class MerchantNeedsHelpWithOutlawsIssueTypeDefiner : SaveableTypeDefiner
	{
		public MerchantNeedsHelpWithOutlawsIssueTypeDefiner()
			: base(590000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(MerchantNeedsHelpWithOutlawsIssue), 1);
			AddClassDefinition(typeof(MerchantNeedsHelpWithOutlawsIssueQuest), 2);
		}
	}

	private const IssueBase.IssueFrequency MerchantNeedsHelpWithOutlawsIssueFrequency = IssueBase.IssueFrequency.VeryCommon;

	private readonly Dictionary<Settlement, List<Hideout>> _closestHideoutsToSettlements = new Dictionary<Settlement, List<Hideout>>();

	private static float ValidBanditPartyDistance => NeededHideoutDistanceToSpawnTheQuest * 0.75f;

	private static float NeededHideoutDistanceToSpawnTheQuest => Campaign.Current.EstimatedAverageBanditPartySpeed * (float)CampaignTime.HoursInDay;

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnGameEarlyLoadedEvent.AddNonSerializedListener(this, OnGameEarlyLoaded);
	}

	private void OnGameEarlyLoaded(CampaignGameStarter obj)
	{
		InitializeCache();
	}

	private void OnNewGameCreated(CampaignGameStarter obj)
	{
		InitializeCache();
	}

	private void InitializeCache()
	{
		foreach (Settlement item in Settlement.All)
		{
			if (!item.IsTown && !item.IsVillage)
			{
				continue;
			}
			foreach (Hideout item2 in Hideout.All)
			{
				if (Campaign.Current.Models.MapDistanceModel.GetDistance(item2.Settlement, item, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default) < Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Default) * 1.25f)
				{
					if (!_closestHideoutsToSettlements.ContainsKey(item))
					{
						_closestHideoutsToSettlements.Add(item, new List<Hideout> { item2 });
					}
					else
					{
						_closestHideoutsToSettlements[item].Add(item2);
					}
				}
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private bool ConditionsHold(Hero issueGiver, out Hideout hideout)
	{
		hideout = null;
		if ((issueGiver.IsMerchant || issueGiver.IsRuralNotable) && _closestHideoutsToSettlements.TryGetValue(issueGiver.CurrentSettlement, out var value))
		{
			foreach (Hideout item in value)
			{
				if (item.IsInfested && !item.Settlement.IsSettlementBusy(this))
				{
					hideout = item;
					return true;
				}
			}
		}
		return false;
	}

	public void OnCheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero, out var hideout))
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnSelected, typeof(MerchantNeedsHelpWithOutlawsIssue), IssueBase.IssueFrequency.VeryCommon, hideout));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(MerchantNeedsHelpWithOutlawsIssue), IssueBase.IssueFrequency.VeryCommon));
		}
	}

	private IssueBase OnSelected(in PotentialIssueData pid, Hero issueOwner)
	{
		Hideout relatedHideout = pid.RelatedObject as Hideout;
		return new MerchantNeedsHelpWithOutlawsIssue(issueOwner, relatedHideout);
	}
}
