using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues;

public class HeadmanNeedsToDeliverAHerdIssueBehavior : CampaignBehaviorBase
{
	public class HeadmanNeedsToDeliverAHerdIssue : IssueBase
	{
		private const int IssueDuration = 30;

		private const int QuestTimeLimit = 30;

		private const int AlternativeSolutionTroopTierRequirement = 2;

		private const int RequiredSkillForSendingCompanion = 120;

		[CachedData]
		private readonly MBList<string> _possibleHerdTypes = new MBList<string> { "sheep", "cow", "hog" };

		[SaveableField(10)]
		private Settlement _targetSettlement;

		[SaveableField(20)]
		private Hero _targetHero;

		[SaveableField(30)]
		private ItemObject _herdTypeToDeliver;

		private float MaxDistanceForSettlementSelection => Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Default) * 3.75f;

		private float MinDistanceForSettlementSelection => Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Default) * 1.5f;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.Duration;

		private int AnimalCountToDeliver => (int)TaleWorlds.Library.MathF.Clamp(TaleWorlds.Library.MathF.Round(5000f * base.IssueDifficultyMultiplier / (float)_herdTypeToDeliver.Value), 10f, 75f);

		public override int AlternativeSolutionBaseNeededMenCount => 6 + TaleWorlds.Library.MathF.Ceiling(14f * base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 5 + TaleWorlds.Library.MathF.Ceiling(5f * base.IssueDifficultyMultiplier);

		protected override int RewardGold => 300 + (int)((float)_herdTypeToDeliver.Value * 0.75f * (float)AnimalCountToDeliver);

		public override TextObject Description
		{
			get
			{
				TextObject textObject = new TextObject("{=kDIi3bLN}The village needs someone to take a herd to {TARGET_SETTLEMENT}.");
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		public override TextObject IssueBriefByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=7H4HQNvF}Yes. Some of the families in this village need to raise a bit of money. [if:convo_calm_friendly][ib:normal2]They've put together a herd of {ANIMAL_COUNT_TO_DELIVER} {.%}{?ANIMAL_COUNT_TO_DELIVER > 1}{PLURAL(HERD_TYPE_TO_DELIVER)}{?}{HERD_TYPE_TO_DELIVER}{\\?}{.%} to sell in {TARGET_SETTLEMENT}, but with all the banditry on the roads, they can't drive it there on their own. We're not merchants or landowners. We can't afford any losses.");
				if (base.IssueOwner.CharacterObject.GetPersona() == DefaultTraits.PersonaCurt)
				{
					textObject = new TextObject("{=6kJ31qut}Yeah, well, some people here are a bit short of money these days. [if:convo_calm_friendly][ib:normal]They've put together a herd of {ANIMAL_COUNT_TO_DELIVER} {.%}{?ANIMAL_COUNT_TO_DELIVER > 1}{PLURAL(HERD_TYPE_TO_DELIVER)}{?}{HERD_TYPE_TO_DELIVER}{\\?}{.%} to sell in {TARGET_SETTLEMENT}. But they're poor folks. Not really fighters, and they can't afford to hire guards. If they go there by themselves they'd be sitting ducks for any bandits.");
				}
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("ANIMAL_COUNT_TO_DELIVER", AnimalCountToDeliver);
				textObject.SetTextVariable("HERD_TYPE_TO_DELIVER", _herdTypeToDeliver.Name);
				return textObject;
			}
		}

		public override TextObject IssueAcceptByPlayer => new TextObject("{=lmJYF6pQ}Tell me how I can help.");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=VbRyXBsv}If you're going in the direction of {TARGET_SETTLEMENT}, you can perhaps take our herd there to {TARGET_HERO.LINK}. I am willing to pay {REWARD_AMOUNT}{GOLD_ICON} if you deliver them safe and sound.[if:convo_calm_friendly][ib:normal]");
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("REWARD_AMOUNT", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				StringHelpers.SetCharacterProperties("TARGET_HERO", _targetHero.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=CqmoyrHH}You can assign a companion with {REQUIRED_SOLDIERS} men, they will be enough too. Both ways works fine for us. I promise if you or your men manage to deliver the herd safely, I will pay you {REWARD}{GOLD_ICON}. So what do you say?[if:convo_nonchalant]");
				textObject.SetTextVariable("REQUIRED_SOLDIERS", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("REWARD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		public override TextObject IssueQuestSolutionAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=MJdVfS4z}Don't worry. I will deliver your {ANIMAL_COUNT_TO_DELIVER} {.%}{?ANIMAL_COUNT_TO_DELIVER > 1}{PLURAL(HERD_TYPE_TO_DELIVER)}{?}{HERD_TYPE_TO_DELIVER}{\\?}{.%} personally to {TARGET_HERO.LINK} in {TARGET_SETTLEMENT}.");
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.Name);
				textObject.SetTextVariable("ANIMAL_COUNT_TO_DELIVER", AnimalCountToDeliver);
				textObject.SetTextVariable("HERD_TYPE_TO_DELIVER", _herdTypeToDeliver.Name);
				StringHelpers.SetCharacterProperties("TARGET_HERO", _targetHero.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=k8W02aj9}I will assign a companion with {NEEDED_MEN_COUNT} of my men to deliver {ANIMAL_COUNT_TO_DELIVER} {.%}{?ANIMAL_COUNT_TO_DELIVER > 1}{PLURAL(HERD_TYPE_TO_DELIVER)}{?}{HERD_TYPE_TO_DELIVER}{\\?}{.%} safely.");
				textObject.SetTextVariable("NEEDED_MEN_COUNT", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("ANIMAL_COUNT_TO_DELIVER", AnimalCountToDeliver);
				textObject.SetTextVariable("HERD_TYPE_TO_DELIVER", _herdTypeToDeliver.Name);
				return textObject;
			}
		}

		public override TextObject IssueDiscussAlternativeSolution
		{
			get
			{
				TextObject textObject = new TextObject("{=mwpY5Ylb}I am still waiting for news from {TARGET_SETTLEMENT}. Once again, I appreciate that you could spare the men to do this.[if:convo_calm_friendly]");
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.Name);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=BJxJwCc5}Thank you, {?PLAYER.GENDER}madam{?}sir{\\?}. Any brigands would be most unwise to tangle with you.");
				if (base.IssueOwner.CharacterObject.GetPersona() == DefaultTraits.PersonaCurt)
				{
					textObject = new TextObject("{=muE5fcOf}Good. Anyone gives you trouble... Well, you look like you could handle them.");
				}
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
				return textObject;
			}
		}

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=m2h6zMp7}{ISSUE_GIVER.LINK}, the headman from {SETTLEMENT}, has asked you to deliver some of the village's livestock to {SETTLEMENT_TARGET}. The villagers can't afford their own guards and also can't afford any losses. {ISSUE_GIVER.LINK} offers {REWARD_AMOUNT}{GOLD_ICON} for the herd's delivery. You sent {COMPANION.LINK} with {NEEDED_MEN_COUNT} of your men to protect the herd. They should return to you with news of their success in {RETURN_DAYS} days.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", base.IssueOwner.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("SETTLEMENT_TARGET", _targetSettlement.Name);
				textObject.SetTextVariable("NEEDED_MEN_COUNT", AlternativeSolutionSentTroops.TotalManCount - 1);
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				textObject.SetTextVariable("REWARD_AMOUNT", RewardGold);
				return textObject;
			}
		}

		public override bool IsThereAlternativeSolution => true;

		public override bool IsThereLordSolution => false;

		public override TextObject IssueAsRumorInSettlement
		{
			get
			{
				TextObject textObject = new TextObject("{=cvZH3cI1}I hope {QUEST_GIVER.NAME} has a plan to get that herd to market.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		protected override int CompanionSkillRewardXP => (int)(500f + 700f * base.IssueDifficultyMultiplier);

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=KhUkmIrH}Deliver the Herd to {TARGET_SETTLEMENT}");
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.Name);
				return textObject;
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsHeadmanNeedsToDeliverAHerdIssue(object o, List<object> collectedObjects)
		{
			((HeadmanNeedsToDeliverAHerdIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_targetSettlement);
			collectedObjects.Add(_targetHero);
			collectedObjects.Add(_herdTypeToDeliver);
		}

		internal static object AutoGeneratedGetMemberValue_targetSettlement(object o)
		{
			return ((HeadmanNeedsToDeliverAHerdIssue)o)._targetSettlement;
		}

		internal static object AutoGeneratedGetMemberValue_targetHero(object o)
		{
			return ((HeadmanNeedsToDeliverAHerdIssue)o)._targetHero;
		}

		internal static object AutoGeneratedGetMemberValue_herdTypeToDeliver(object o)
		{
			return ((HeadmanNeedsToDeliverAHerdIssue)o)._herdTypeToDeliver;
		}

		public HeadmanNeedsToDeliverAHerdIssue(Hero issueOwner)
			: base(issueOwner, CampaignTime.DaysFromNow(30f))
		{
			HeadmanNeedsToDeliverAHerdIssue headmanNeedsToDeliverAHerdIssue = this;
			Settlement settlement = SettlementHelper.FindRandomSettlement((Settlement x) => x.IsTown && x.Notables.Any((Hero y) => y.CanHaveCampaignIssues()) && !x.MapFaction.IsAtWarWith(issueOwner.MapFaction) && Campaign.Current.Models.MapDistanceModel.GetDistance(x, headmanNeedsToDeliverAHerdIssue.IssueSettlement, isFromPort: false, isTargetingPort: false, MobileParty.MainParty.NavigationCapability) > headmanNeedsToDeliverAHerdIssue.MinDistanceForSettlementSelection && Campaign.Current.Models.MapDistanceModel.GetDistance(x, headmanNeedsToDeliverAHerdIssue.IssueSettlement, isFromPort: false, isTargetingPort: false, MobileParty.MainParty.NavigationCapability) < headmanNeedsToDeliverAHerdIssue.MaxDistanceForSettlementSelection);
			_targetSettlement = settlement ?? base.IssueSettlement.Village.Bound;
			_herdTypeToDeliver = Campaign.Current.ObjectManager.GetObject<ItemObject>(_possibleHerdTypes.GetRandomElement());
			if (_targetSettlement != null)
			{
				_targetHero = _targetSettlement.Notables.GetRandomElementWithPredicate((Hero x) => x.CanHaveCampaignIssues()) ?? _targetSettlement.Notables.GetRandomElement();
			}
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.SettlementProsperity)
			{
				return -0.2f;
			}
			return 0f;
		}

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			return ((hero.GetSkillValue(DefaultSkills.Riding) >= hero.GetSkillValue(DefaultSkills.Scouting)) ? DefaultSkills.Riding : DefaultSkills.Scouting, 120);
		}

		public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
		{
			return character.Tier >= 2;
		}

		public override bool IssueStayAliveConditions()
		{
			if (!base.IssueOwner.CurrentSettlement.IsRaided && !base.IssueOwner.CurrentSettlement.IsUnderRaid && _targetHero.IsActive)
			{
				return base.IssueOwner.CurrentSettlement.Village.Bound.Town.Security <= 70f;
			}
			return false;
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}

		public override bool AlternativeSolutionCondition(out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			ApplySuccessRewards(base.IssueOwner);
		}

		private void ApplySuccessRewards(Hero issueGiver)
		{
			issueGiver.AddPower(5f);
			RelationshipChangeWithIssueOwner = 5;
			issueGiver.CurrentSettlement.Village.Hearth += 50f;
		}

		protected override void OnGameLoad()
		{
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new HeadmanNeedsToDeliverAHerdIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(30f), AnimalCountToDeliver, _herdTypeToDeliver, _targetSettlement, RewardGold, _targetHero);
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
			if (issueGiver.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				flag |= PreconditionFlags.AtWar;
			}
			return flag == PreconditionFlags.None;
		}
	}

	public class HeadmanNeedsToDeliverAHerdIssueQuest : QuestBase
	{
		[SaveableField(10)]
		private readonly Settlement _targetSettlement;

		[SaveableField(20)]
		private readonly Hero _targetHero;

		[SaveableField(30)]
		private readonly ItemObject _herdTypeToDeliver;

		[SaveableField(40)]
		private readonly int _animalCountToDeliver;

		[SaveableField(70)]
		private int _rewardGold;

		[SaveableField(215)]
		private JournalLog _playerStartsQuestLog;

		public sealed override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=KhUkmIrH}Deliver the Herd to {TARGET_SETTLEMENT}");
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.Name);
				return textObject;
			}
		}

		public override bool IsRemainingTimeHidden => false;

		private TextObject PlayerStartsQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=wsGDDXPN}{QUEST_GIVER.LINK}, the headman from {QUEST_GIVER_SETTLEMENT} has asked you to deliver {ANIMAL_COUNT_TO_DELIVER} {.%}{?ANIMAL_COUNT_TO_DELIVER > 1}{PLURAL(HERD_TYPE_TO_DELIVER)}{?}{HERD_TYPE_TO_DELIVER}{\\?}{.%} to {TARGET_HERO.LINK} in {TARGET_SETTLEMENT}. {?QUEST_GIVER.GENDER}She{?}He{\\?} fears such a large herd will attract attention from the brigands on the way. {QUEST_GIVER.LINK} offers {REWARD_AMOUNT}{GOLD_ICON} for the herd's delivery.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("TARGET_HERO", _targetHero.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_GIVER_SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("ANIMAL_COUNT_TO_DELIVER", _animalCountToDeliver);
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("HERD_TYPE_TO_DELIVER", _herdTypeToDeliver.Name);
				textObject.SetTextVariable("REWARD_AMOUNT", _rewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		private TextObject SuccessQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=6IKKFH3A}You have received a message and a large purse from {QUEST_GIVER.LINK}. The missive reads: ”The herd is safe. Thank you, and please accept these {REWARD}{GOLD_ICON} with our gratitude.”.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("REWARD", _rewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		private TextObject FailByTimeOutQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=hhrRnSvr}You failed to deliver the herd in time, as {QUEST_GIVER.LINK} has asked of you. The shepherds and the herd left you.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		private TextObject FailByRejectQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=MjwNHtQd}You rejected to deliver the herd, as {QUEST_GIVER.LINK} has asked of you. The shepherds have left you.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
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

		protected override TextObject TargetHeroDiedLogText => new TextObject("{=m41vdhSR}{QUEST_TARGET.LINK} is no longer alive. It might not be your fault that you didn't get the herd to {?QUEST_TARGET.GENDER}her{?}him{\\?} in time, but many people will consider this breach of trust.");

		internal static void AutoGeneratedStaticCollectObjectsHeadmanNeedsToDeliverAHerdIssueQuest(object o, List<object> collectedObjects)
		{
			((HeadmanNeedsToDeliverAHerdIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_targetSettlement);
			collectedObjects.Add(_targetHero);
			collectedObjects.Add(_herdTypeToDeliver);
			collectedObjects.Add(_playerStartsQuestLog);
		}

		internal static object AutoGeneratedGetMemberValue_targetSettlement(object o)
		{
			return ((HeadmanNeedsToDeliverAHerdIssueQuest)o)._targetSettlement;
		}

		internal static object AutoGeneratedGetMemberValue_targetHero(object o)
		{
			return ((HeadmanNeedsToDeliverAHerdIssueQuest)o)._targetHero;
		}

		internal static object AutoGeneratedGetMemberValue_herdTypeToDeliver(object o)
		{
			return ((HeadmanNeedsToDeliverAHerdIssueQuest)o)._herdTypeToDeliver;
		}

		internal static object AutoGeneratedGetMemberValue_animalCountToDeliver(object o)
		{
			return ((HeadmanNeedsToDeliverAHerdIssueQuest)o)._animalCountToDeliver;
		}

		internal static object AutoGeneratedGetMemberValue_rewardGold(object o)
		{
			return ((HeadmanNeedsToDeliverAHerdIssueQuest)o)._rewardGold;
		}

		internal static object AutoGeneratedGetMemberValue_playerStartsQuestLog(object o)
		{
			return ((HeadmanNeedsToDeliverAHerdIssueQuest)o)._playerStartsQuestLog;
		}

		public HeadmanNeedsToDeliverAHerdIssueQuest(string questId, Hero questGiver, CampaignTime duration, int animalCountToDeliver, ItemObject herdTypeToDeliver, Settlement targetSettlement, int rewardGold, Hero targetHero)
			: base(questId, questGiver, duration, rewardGold)
		{
			_animalCountToDeliver = animalCountToDeliver;
			_herdTypeToDeliver = herdTypeToDeliver;
			_targetSettlement = targetSettlement;
			_rewardGold = rewardGold;
			_targetHero = targetHero;
			SetDialogs();
			InitializeQuestOnCreation();
		}

		protected override void SetDialogs()
		{
			TextObject textObject = new TextObject("{=iUQuwAZY}Thank you, {?PLAYER.GENDER}madam{?}sir{\\?}. The village will be grateful to you. Good luck.");
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(textObject).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.Consequence(QuestAcceptedConsequences)
				.CloseDialog();
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(new TextObject("{=zB6elkn1}The herd is ready to depart.")).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=8tBET7S5}Good. We will be heading out soon."))
				.NpcLine(new TextObject("{=iAGx49nO}Good to hear that! Safe journeys.[if:convo_approving]"))
				.CloseDialog()
				.PlayerOption(new TextObject("{=EOaOlh39}In due time. Let's not be too hasty."))
				.NpcLine(new TextObject("{=79Te4duG}As you wish.[if:convo_normal][ib:demure]"))
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
		}

		private void QuestAcceptedConsequences()
		{
			StartQuest();
			_playerStartsQuestLog = AddLog(PlayerStartsQuestLogText);
			AddHerdAndShepherdsToMainParty();
			AddTrackedObject(_targetSettlement);
			AddTrackedObject(_targetHero);
			Campaign.Current.ConversationManager.AddDialogFlow(GetDeliveryDialogFlow(), this);
		}

		protected override void OnCompleteWithSuccess()
		{
			base.QuestGiver.AddPower(5f);
			RelationshipChangeWithQuestGiver = 5;
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 30)
			});
			base.QuestGiver.CurrentSettlement.Village.Hearth += 50f;
			_targetSettlement.Town.Prosperity += 50f;
			int availableRequestedItemCountOnPlayer = GetAvailableRequestedItemCountOnPlayer(_herdTypeToDeliver);
			int num = ((availableRequestedItemCountOnPlayer > _animalCountToDeliver) ? _animalCountToDeliver : availableRequestedItemCountOnPlayer);
			foreach (ItemRosterElement item in MobileParty.MainParty.ItemRoster)
			{
				if (item.EquipmentElement.Item == _herdTypeToDeliver)
				{
					int amount = item.Amount;
					if (amount >= num)
					{
						PartyBase.MainParty.ItemRoster.AddToCounts(item.EquipmentElement, -num);
						_targetSettlement.ItemRoster.AddToCounts(item.EquipmentElement, num);
						break;
					}
					num -= amount;
					PartyBase.MainParty.ItemRoster.AddToCounts(item.EquipmentElement, -amount);
					_targetSettlement.ItemRoster.AddToCounts(item.EquipmentElement, amount);
				}
			}
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, _rewardGold);
			AddLog(SuccessQuestLogText);
		}

		protected override void OnFinalize()
		{
			RemoveTrackedObject(_targetSettlement);
			RemoveTrackedObject(_targetHero);
		}

		private DialogFlow GetDeliveryDialogFlow()
		{
			TextObject textObject = new TextObject("{=8nwZXNTk}About the task {QUEST_GIVER.LINK} gave me...");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
			TextObject textObject2 = new TextObject("{=kwJBLl00}Yes {?PLAYER.GENDER}madam{?}sir{\\?}. Our mutual friend {QUEST_GIVER.LINK} sent word to us. {?QUEST_GIVER.GENDER}She{?}He{\\?} told us to expect you with {?QUEST_GIVER.GENDER}her{?}his{\\?} {HERD_AMOUNT} {.%}{?HERD_AMOUNT > 1}{PLURAL(HERD_TYPE)}{?}{HERD_TYPE}{\\?}{.%}.");
			TextObject npcText = new TextObject("{=vXCg3OYx}So, have you brought them?[if:convo_undecided_open]");
			textObject2.SetTextVariable("HERD_AMOUNT", _animalCountToDeliver);
			textObject2.SetTextVariable("HERD_TYPE", _herdTypeToDeliver.Name);
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject2);
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject2);
			TextObject textObject3 = new TextObject("{=wmZYHGb9}I brought {HERD_AMOUNT} {.%}{?HERD_AMOUNT > 1}{PLURAL(HERD_TYPE)}{?}{HERD_TYPE}{\\?}{.%} as we agreed.");
			textObject3.SetTextVariable("HERD_AMOUNT", _animalCountToDeliver);
			textObject3.SetTextVariable("HERD_TYPE", _herdTypeToDeliver.Name);
			TextObject textObject4 = new TextObject("{=VkUMPAfR}Thank you for your help. {QUEST_GIVER.LINK} will send your reward as soon as possible.[if:convo_calm_friendly][ib:demure]");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject4);
			TextObject text = new TextObject("{=1s54uxsA}Sorry. I don't have any animals for you this time.");
			TextObject npcText2 = new TextObject("{=1dUVrgQ8}I just hope you can deliver the herd soon.");
			TextObject text2 = new TextObject("{=HFZisEnI}I'm going to keep that herd for myself.");
			TextObject textObject5 = new TextObject("{=LpfQYLQo}What? That's straight-up theft. I guarantee you {QUEST_GIVER.LINK} will hear about this![if:convo_annoyed][ib:aggressive]");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject5);
			return DialogFlow.CreateDialogFlow("hero_main_options", 125).PlayerLine(textObject).Condition(() => Settlement.CurrentSettlement == _targetSettlement && CharacterObject.OneToOneConversationCharacter.IsHero && CharacterObject.OneToOneConversationCharacter.HeroObject == _targetHero)
				.NpcLine(textObject2)
				.NpcLine(npcText)
				.BeginPlayerOptions()
				.PlayerOption(textObject3)
				.Condition(delegate
				{
					int availableRequestedItemCountOnPlayer = GetAvailableRequestedItemCountOnPlayer(_herdTypeToDeliver);
					Campaign.Current.ConversationManager.GetCurrentDialogLine().SetTextVariable("HERD_AMOUNT", _animalCountToDeliver);
					Campaign.Current.ConversationManager.GetCurrentDialogLine().SetTextVariable("HERD_TYPE", _herdTypeToDeliver.Name);
					return availableRequestedItemCountOnPlayer >= _animalCountToDeliver;
				})
				.NpcLine(textObject4)
				.Consequence(DeliverHerdOnConsequence)
				.CloseDialog()
				.PlayerOption(text2)
				.NpcLine(textObject5)
				.Consequence(DeliverHerdRejectOnConsequence)
				.CloseDialog()
				.PlayerOption(text)
				.NpcLine(npcText2)
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
		}

		private void DeliverHerdRejectOnConsequence()
		{
			CompleteQuestWithFail();
			ChangeCrimeRatingAction.Apply(base.QuestGiver.CurrentSettlement.MapFaction, 20f);
		}

		private void DeliverHerdOnConsequence()
		{
			CompleteQuestWithSuccess();
		}

		private int GetAvailableRequestedItemCountOnPlayer(ItemObject item)
		{
			int num = 0;
			foreach (ItemRosterElement item2 in PartyBase.MainParty.ItemRoster)
			{
				if (item2.EquipmentElement.Item == item)
				{
					num += item2.Amount;
				}
			}
			return num;
		}

		public override void OnCanceled()
		{
			int availableRequestedItemCountOnPlayer = GetAvailableRequestedItemCountOnPlayer(_herdTypeToDeliver);
			PartyBase.MainParty.ItemRoster.AddToCounts(_herdTypeToDeliver, (availableRequestedItemCountOnPlayer > _animalCountToDeliver) ? (-_animalCountToDeliver) : (-availableRequestedItemCountOnPlayer));
		}

		protected override void OnTimedOut()
		{
			ApplyFailureEffects(isTimedOut: true);
			AddLog(FailByTimeOutQuestLogText);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -10)
			});
			RemoveRemainingHorses();
		}

		public override void OnFailed()
		{
			ApplyFailureEffects();
			AddLog(FailByRejectQuestLogText);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -30)
			});
		}

		private void ApplyFailureEffects(bool isTimedOut = false)
		{
			base.QuestGiver.AddPower(-5f);
			_targetSettlement.Town.Prosperity -= 10f;
			RelationshipChangeWithQuestGiver = (isTimedOut ? (-5) : (-10));
		}

		private void RemoveRemainingHorses()
		{
			int num = _animalCountToDeliver;
			foreach (ItemRosterElement item in MobileParty.MainParty.ItemRoster)
			{
				if (item.EquipmentElement.Item == _herdTypeToDeliver)
				{
					int amount = item.Amount;
					if (amount >= num)
					{
						PartyBase.MainParty.ItemRoster.AddToCounts(item.EquipmentElement, -num);
						break;
					}
					num -= amount;
					PartyBase.MainParty.ItemRoster.AddToCounts(item.EquipmentElement, -amount);
				}
			}
		}

		private void AddHerdAndShepherdsToMainParty()
		{
			MobileParty.MainParty.ItemRoster.AddToCounts(_herdTypeToDeliver, _animalCountToDeliver);
			MobileParty.MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, 5);
			TextObject textObject = new TextObject("{=GgBtpOEm}{.%}{ANIMAL_COUNT} {?ANIMAL_COUNT > 1}{PLURAL(ANIMAL_TYPE)}{?}{ANIMAL_TYPE}{\\?}{.%} added to your party.");
			textObject.SetTextVariable("ANIMAL_COUNT", _animalCountToDeliver);
			textObject.SetTextVariable("ANIMAL_TYPE", _herdTypeToDeliver.Name);
			MBInformationManager.AddQuickInformation(textObject);
		}

		protected override void InitializeQuestOnGameLoad()
		{
			SetDialogs();
			Campaign.Current.ConversationManager.AddDialogFlow(GetDeliveryDialogFlow(), this);
		}

		protected override void HourlyTick()
		{
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
		}

		public override void OnHeroCanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
		{
			if (hero == _targetHero)
			{
				result = false;
			}
		}

		private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
			{
				QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
			}
		}

		private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
		{
			if (victim == _targetHero)
			{
				TextObject textObject = ((detail == KillCharacterAction.KillCharacterActionDetail.Lost) ? TargetHeroDisappearedLogText : TargetHeroDiedLogText);
				StringHelpers.SetCharacterProperties("QUEST_TARGET", _targetHero.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				AddLog(textObject);
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
	}

	public class HeadmanNeedsToDeliverAHerdIssueTypeDefiner : SaveableTypeDefiner
	{
		public HeadmanNeedsToDeliverAHerdIssueTypeDefiner()
			: base(430000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(HeadmanNeedsToDeliverAHerdIssue), 1);
			AddClassDefinition(typeof(HeadmanNeedsToDeliverAHerdIssueQuest), 2);
		}
	}

	private const IssueBase.IssueFrequency HeadmanNeedsToDeliverAHerdIssueFrequency = IssueBase.IssueFrequency.VeryCommon;

	[SaveableField(216)]
	private HeadmanNeedsToDeliverAHerdIssue _headmanNeedsToDeliverAHerdIssue;

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	public void OnCheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero))
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnSelected, typeof(HeadmanNeedsToDeliverAHerdIssue), IssueBase.IssueFrequency.VeryCommon));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(HeadmanNeedsToDeliverAHerdIssue), IssueBase.IssueFrequency.VeryCommon));
		}
	}

	private bool ConditionsHold(Hero issueGiver)
	{
		if (issueGiver.CurrentSettlement != null && issueGiver.CurrentSettlement.IsVillage && IsVillageSuitableForIssue(issueGiver.CurrentSettlement.Village) && (issueGiver.IsHeadman || issueGiver.IsRuralNotable) && issueGiver.CurrentSettlement.Village.Bound.Notables.Count > 0)
		{
			return issueGiver.CurrentSettlement.Village.Bound.Town.Security <= 60f;
		}
		return false;
	}

	private static bool IsVillageSuitableForIssue(Village village)
	{
		if (!village.Bound.IsCastle)
		{
			if (village.VillageType != DefaultVillageTypes.BattanianHorseRanch && village.VillageType != DefaultVillageTypes.DesertHorseRanch && village.VillageType != DefaultVillageTypes.EuropeHorseRanch && village.VillageType != DefaultVillageTypes.SteppeHorseRanch && village.VillageType != DefaultVillageTypes.SturgianHorseRanch && village.VillageType != DefaultVillageTypes.VlandianHorseRanch && village.VillageType != DefaultVillageTypes.CattleRange && village.VillageType != DefaultVillageTypes.SheepFarm)
			{
				return village.VillageType == DefaultVillageTypes.HogFarm;
			}
			return true;
		}
		return false;
	}

	private IssueBase OnSelected(in PotentialIssueData pid, Hero issueOwner)
	{
		_headmanNeedsToDeliverAHerdIssue = new HeadmanNeedsToDeliverAHerdIssue(issueOwner);
		return _headmanNeedsToDeliverAHerdIssue;
	}
}
