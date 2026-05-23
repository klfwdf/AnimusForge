using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues;

public class ArtisanCantSellProductsAtAFairPriceIssueBehavior : CampaignBehaviorBase
{
	public class ArtisanCantSellProductsAtAFairPriceIssue : IssueBase
	{
		private const int BaseRewardGold = 500;

		private const int IssueDuration = 30;

		private const int QuestTimeLimit = 18;

		private const int RequiredSkillLevelForCompanion = 120;

		[CachedData]
		private readonly MBList<string> _possibleDeliveryItems = new MBList<string> { "olives", "clay", "flax", "grape", "wool", "hardwood", "hides" };

		[SaveableField(10)]
		private ItemObject _rawMaterialsToBeDelivered;

		[SaveableField(20)]
		private Settlement _targetSettlement;

		[SaveableField(40)]
		private Hero _targetHero;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.Duration;

		[SaveableProperty(30)]
		public override Hero CounterOfferHero { get; protected set; }

		private int RawMaterialCountToBeDelivered => (int)(60f * base.IssueDifficultyMultiplier);

		public override int AlternativeSolutionBaseNeededMenCount => 3 + TaleWorlds.Library.MathF.Ceiling(6f * base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 3 + TaleWorlds.Library.MathF.Ceiling(5f * base.IssueDifficultyMultiplier);

		protected override int RewardGold => (int)(500f + 1500f * base.IssueDifficultyMultiplier);

		public override int NeededInfluenceForLordSolution => 10 + TaleWorlds.Library.MathF.Round(40f * base.IssueDifficultyMultiplier);

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=4M6jlZVa}{ISSUE_GIVER.LINK} an artisan from {ISSUE_GIVER_SETTLEMENT} has complained to you about not being able to sell their goods at the price they want because of local laws. {?ISSUE_GIVER.GENDER}She{?}He{\\?} will pay {REWARD}{GOLD_ICON} for you to take the goods to another town. You have tasked {COMPANION.LINK} and {ALTERNATIVE_SOLUTION_TROOP_COUNT} men to smuggle {?ISSUE_GIVER.GENDER}her{?}his{\\?} goods to a “friend” in {TARGET_TOWN}. They will return to you in {RETURN_DAYS} days with a sizable sum of {REWARD}{GOLD_ICON}.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				textObject.SetTextVariable("ISSUE_GIVER_SETTLEMENT", base.IssueOwner.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("TARGET_TOWN", _targetSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("ALTERNATIVE_SOLUTION_TROOP_COUNT", AlternativeSolutionSentTroops.TotalManCount - 1);
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				textObject.SetTextVariable("REWARD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		protected override int CompanionSkillRewardXP => (int)(400f + 1700f * base.IssueDifficultyMultiplier);

		public override bool IsThereAlternativeSolution => true;

		public override bool IsThereLordSolution => true;

		public override TextObject IssueBriefByIssueGiver => new TextObject("{=Vg7Ftrdl}You might say that... I work from dawn to late into the night but even so I can barely put bread on the table. Why's that? Because I can't sell my product at a fair price. The law says that I can only sell to local merchants, and at a fixed rate too, so that even when other prices are high I'm still making the same.[if:convo_annoyed]");

		public override TextObject IssueAcceptByPlayer => new TextObject("{=ioF9aJBJ}How can I help?");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=bOyjQ4yr}If you can deliver {REQUESTED_AMOUNT} {.%}{?(REQUESTED_AMOUNT > 1)}{PLURAL(RAW_MATERIALS)}{?}{RAW_MATERIALS}{\\?}{.%} to my contact {TARGET_HERO.LINK}, who you can find in {TARGET_SETTLEMENT}, that would help me survive for a time. The merchants of the town could be furious and lodge legal complaints of course, but hopefully you won't have to deal with that part.");
				StringHelpers.SetCharacterProperties("TARGET_HERO", _targetHero.CharacterObject, textObject);
				textObject.SetTextVariable("REQUESTED_AMOUNT", RawMaterialCountToBeDelivered);
				textObject.SetTextVariable("RAW_MATERIALS", _rawMaterialsToBeDelivered.Name);
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=Gzx35bRY}If you aren't going towards {TARGET_SETTLEMENT}, maybe you could have some of your men take my goods and bring back the profit? It shouldn't take more than {ALTERNATIVE_SOLUTION_MEN_AMOUNT} men and one of your trusted lieutenants with decent grasp of trade. They can go and be back in about {RETURN_DAYS} days.[if:convo_annoyed]");
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("ALTERNATIVE_SOLUTION_MEN_AMOUNT", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		public override TextObject IssueLordSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=tgj9BuCL}You could change this law, my {?PLAYER.GENDER}lady{?}lord{\\?}. All of us would be grateful. and we're willing to donate {REWARD}{GOLD_ICON} to show our thanks...");
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				textObject.SetTextVariable("REWARD", RewardGold);
				return textObject;
			}
		}

		public override TextObject IssueLordSolutionAcceptByPlayer => new TextObject("{=azHy8uwl}I will issue a decree to remove the restrictions on the sale of goods.");

		public override TextObject IssueLordSolutionResponseByIssueGiver => new TextObject("{=hAyFpmxq}My {?PLAYER.GENDER}lady{?}lord{\\?}, that would be very good. The merchants won't like it but hard-working artisans like me will be grateful to you.");

		public override TextObject IssueLordSolutionCounterOfferExplanationByOtherNpc => new TextObject("{=nbeCbTH6}My {?PLAYER.GENDER}lady{?}lord{\\?}, we heard that you wish to issue a decree permitting artisans to behave like merchants. This will undo an arrangement here that's worked for generations. Please reconsider this.");

		public override TextObject IssueLordSolutionCounterOfferBriefByOtherNpc => new TextObject("{=KSeIOHDh}(One of the merchants in the town comes to talk as you are preparing to depart.)");

		public override TextObject IssueLordSolutionCounterOfferAcceptByPlayer => new TextObject("{=dpFFo2U2}I understand. I'll hold off on the decree, then.");

		public override TextObject IssueLordSolutionCounterOfferAcceptResponseByOtherNpc => new TextObject("{=6ZTNuF30}That's the right call. Thank you for listening to the voice of reason.");

		public override TextObject IssueLordSolutionCounterOfferDeclineByPlayer => new TextObject("{=K1J6Xqht}Sorry, I have to do it.");

		public override TextObject IssueLordSolutionCounterOfferDeclineResponseByOtherNpc => new TextObject("{=YjsRb88D}That's your right, my {?PLAYER.GENDER}lady{?}lord{\\?}. But if you can't get the support of the merchants here when you next need it, well, don't say no one said anything.");

		protected override TextObject LordSolutionStartLog => new TextObject("{=FT9V47bd}You issued a decree to remove the restrictions on the sale of goods, as the artisans had requested.");

		protected override TextObject LordSolutionCounterOfferAcceptLog
		{
			get
			{
				TextObject textObject = new TextObject("{=SpIn5pr4}After listening to the town merchants, you decided to keep the restrictions on the sale of goods. Your decision made the merchants happy but disappointed the {QUEST_GIVER.LINK}.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject IssueQuestSolutionAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=ueUsg9OL}I can take your goods to {TARGET_HERO.LINK} in {TARGET_SETTLEMENT} myself.");
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				StringHelpers.SetCharacterProperties("TARGET_HERO", _targetHero.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=qfClYYjK}Artisans Can't Sell Their Products in {ISSUE_GIVER_SETTLEMENT}");
				textObject.SetTextVariable("ISSUE_GIVER_SETTLEMENT", base.IssueSettlement.Name);
				return textObject;
			}
		}

		public override TextObject Description
		{
			get
			{
				TextObject textObject = new TextObject("{=3KpgHPlo}The laws of {ISSUE_GIVER_SETTLEMENT} dictate that artisans sell their goods at a fixed rate to the merchants in town. And now they complain the price set by the merchants is not fair.");
				textObject.SetTextVariable("ISSUE_GIVER_SETTLEMENT", base.IssueOwner.CurrentSettlement.Name);
				return textObject;
			}
		}

		public override TextObject IssueAsRumorInSettlement
		{
			get
			{
				TextObject textObject = new TextObject("{=TuIyqMWG}The artisans and the merchants are at each other's throats right now. I bet {QUEST_GIVER.NAME} is planning something.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=srbMyPXl}You can have one of my companions and {REQUIRED_TROOP_AMOUNT} of my men to take your goods to {TARGET_TOWN}");
				textObject.SetTextVariable("REQUIRED_TROOP_AMOUNT", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("TARGET_TOWN", _targetSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		public override TextObject IssueDiscussAlternativeSolution
		{
			get
			{
				TextObject textObject = new TextObject("{=GE4DqFyl}I am still waiting for news from {TARGET_SETTLEMENT}. Thanks again for sparing your men.");
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=EnS6NwaH}Thank you my {?PLAYER.GENDER}lady{?}lord{\\?}. I am sure your men will take care of the problem.");
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
				return textObject;
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsArtisanCantSellProductsAtAFairPriceIssue(object o, List<object> collectedObjects)
		{
			((ArtisanCantSellProductsAtAFairPriceIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_rawMaterialsToBeDelivered);
			collectedObjects.Add(_targetSettlement);
			collectedObjects.Add(_targetHero);
			collectedObjects.Add(CounterOfferHero);
		}

		internal static object AutoGeneratedGetMemberValueCounterOfferHero(object o)
		{
			return ((ArtisanCantSellProductsAtAFairPriceIssue)o).CounterOfferHero;
		}

		internal static object AutoGeneratedGetMemberValue_rawMaterialsToBeDelivered(object o)
		{
			return ((ArtisanCantSellProductsAtAFairPriceIssue)o)._rawMaterialsToBeDelivered;
		}

		internal static object AutoGeneratedGetMemberValue_targetSettlement(object o)
		{
			return ((ArtisanCantSellProductsAtAFairPriceIssue)o)._targetSettlement;
		}

		internal static object AutoGeneratedGetMemberValue_targetHero(object o)
		{
			return ((ArtisanCantSellProductsAtAFairPriceIssue)o)._targetHero;
		}

		public ArtisanCantSellProductsAtAFairPriceIssue(Hero issueOwner)
			: base(issueOwner, CampaignTime.DaysFromNow(30f))
		{
			_targetSettlement = SelectTargetSettlement(issueOwner);
			_targetHero = _targetSettlement.Notables.GetRandomElementWithPredicate((Hero x) => x.CanHaveCampaignIssues());
			_rawMaterialsToBeDelivered = Campaign.Current.ObjectManager.GetObject<ItemObject>(_possibleDeliveryItems.GetRandomElement());
			CounterOfferHero = SelectCounterOfferHero(issueOwner);
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.SettlementProsperity)
			{
				return -0.2f;
			}
			if (issueEffect == DefaultIssueEffects.IssueOwnerPower)
			{
				return -0.2f;
			}
			return 0f;
		}

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			return ((hero.GetSkillValue(DefaultSkills.Charm) >= hero.GetSkillValue(DefaultSkills.Trade)) ? DefaultSkills.Charm : DefaultSkills.Trade, 120);
		}

		protected override void LordSolutionConsequenceWithAcceptCounterOffer()
		{
			foreach (Hero notable in base.IssueOwner.CurrentSettlement.Notables)
			{
				if (notable.IsMerchant)
				{
					ChangeRelationAction.ApplyPlayerRelation(notable, (int)(2f * base.IssueDifficultyMultiplier));
				}
			}
			RelationshipChangeWithIssueOwner = (int)(-5f * base.IssueDifficultyMultiplier);
			TraitLevelingHelper.OnIssueSolvedThroughBetrayal(base.IssueOwner, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, TaleWorlds.Library.MathF.Round(-50f * base.IssueDifficultyMultiplier))
			});
			ApplyFailureEffects();
		}

		protected override void LordSolutionConsequenceWithRefuseCounterOffer()
		{
			ApplyLordSolutionSuccessRewards();
		}

		public override bool LordSolutionCondition(out TextObject explanation)
		{
			if (base.IssueOwner.CurrentSettlement.OwnerClan == Clan.PlayerClan)
			{
				explanation = null;
				return true;
			}
			explanation = new TextObject("{=9y0zpKUF}You need to be the owner of this settlement!");
			return false;
		}

		public override bool AlternativeSolutionCondition(out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation);
		}

		public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation);
		}

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			ApplySuccessRewards();
		}

		private void ApplySuccessRewards()
		{
			base.IssueOwner.AddPower(10f);
			RelationshipChangeWithIssueOwner = 5;
			foreach (Hero item in base.IssueOwner.CurrentSettlement.Notables.Where((Hero x) => x.IsMerchant))
			{
				item.AddPower((int)(-10f * base.IssueDifficultyMultiplier));
				ChangeRelationAction.ApplyPlayerRelation(item, -10);
			}
			base.IssueOwner.CurrentSettlement.Town.Prosperity += 30f;
		}

		private void ApplyLordSolutionSuccessRewards()
		{
			base.IssueOwner.AddPower(10f);
			RelationshipChangeWithIssueOwner = 10;
			CounterOfferHero.AddPower(-10f);
			foreach (Hero notable in base.IssueOwner.CurrentSettlement.Notables)
			{
				if (notable.IsMerchant)
				{
					if (notable != CounterOfferHero)
					{
						ChangeRelationAction.ApplyPlayerRelation(notable, -5);
					}
					else
					{
						ChangeRelationAction.ApplyPlayerRelation(notable, -10);
					}
				}
			}
			base.IssueOwner.CurrentSettlement.Town.Prosperity += 30f;
		}

		private void ApplyFailureEffects()
		{
			base.IssueOwner.AddPower(-10f);
			foreach (Hero item in base.IssueOwner.CurrentSettlement.Notables.Where((Hero x) => x.IsMerchant))
			{
				item.AddPower(3f);
			}
			base.IssueOwner.CurrentSettlement.Town.Prosperity -= 10f;
		}

		protected override void OnGameLoad()
		{
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new ArtisanCantSellProductsAtAFairPriceIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(18f), _targetSettlement, _rawMaterialsToBeDelivered, RawMaterialCountToBeDelivered, RewardGold, _targetHero, CounterOfferHero);
		}

		public override IssueFrequency GetFrequency()
		{
			return IssueFrequency.Common;
		}

		protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out PreconditionFlags flags, out Hero relationHero, out SkillObject skill, out int requiredGold)
		{
			relationHero = null;
			requiredGold = 0;
			flags = PreconditionFlags.None;
			if (issueGiver.GetRelationWithPlayer() < -10f)
			{
				flags |= PreconditionFlags.Relation;
				relationHero = issueGiver;
			}
			relationHero = issueGiver;
			skill = null;
			return flags == PreconditionFlags.None;
		}

		public override bool IssueStayAliveConditions()
		{
			if (CounterOfferHero != null && _targetHero.IsActive && CounterOfferHero.IsActive)
			{
				return CounterOfferHero.CurrentSettlement == base.IssueSettlement;
			}
			return false;
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}
	}

	public class ArtisanCantSellProductsAtAFairPriceIssueQuest : QuestBase
	{
		[SaveableField(10)]
		private readonly ItemObject _rawMaterialsToBeDelivered;

		[SaveableField(20)]
		private readonly int _amountOfRawGoodsToBeDelivered;

		[SaveableField(30)]
		private readonly Settlement _targetSettlement;

		[SaveableField(40)]
		private int _deliveredRawGoods;

		[SaveableField(100)]
		private Hero _counterOfferHero;

		[SaveableField(60)]
		private Hero _targetHero;

		[SaveableField(70)]
		private bool _counterOfferGiven;

		[SaveableField(80)]
		private bool _counterOfferRefused;

		[SaveableField(90)]
		private int _rewardGold;

		[SaveableField(212)]
		private JournalLog _playerStartsQuestLog;

		public override bool IsRemainingTimeHidden => false;

		public override TextObject Title => new TextObject("{=sfW7oT9e}Artisan Can't Sell Products");

		private TextObject PlayerStartsQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=N0E8gd5H}{QUEST_GIVER.LINK} an artisan from {QUEST_GIVER_SETTLEMENT} has complained to you about not being able to sell their goods at the price they want because of the unfair local laws. {?QUEST_GIVER.GENDER}She{?}He{\\?} will pay {REWARD_AMOUNT}{GOLD_ICON} for you to take the goods to another town. You agreed to take {?QUEST_GIVER.GENDER}her{?}his{\\?} goods to {TARGET_HERO.LINK} in {TARGET_SETTLEMENT}.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("TARGET_HERO", _targetHero.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_GIVER_SETTLEMENT", base.QuestGiver.CurrentSettlement.Name);
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("REWARD_AMOUNT", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		private TextObject SuccessQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=7fr8QDYi}{QUEST_GIVER.LINK} sends {?QUEST_GIVER.GENDER}her{?}his{\\?} regards for a fine job done with the {REWARD}{GOLD_ICON} {?QUEST_GIVER.GENDER}she{?}he{\\?} promised.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("REWARD", _rewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		private TextObject FailQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=um4UaneZ}You have failed to deliver {REQUESTED_AMOUNT} {.%}{?(REQUESTED_AMOUNT > 1)}{PLURAL(RAW_MATERIALS)}{?}{RAW_MATERIALS}{\\?}{.%} to {TARGET_SETTLEMENT} in time as {QUEST_GIVER.LINK} asked you.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("REMAINING_AMOUNT", _amountOfRawGoodsToBeDelivered - _deliveredRawGoods);
				textObject.SetTextVariable("REQUESTED_AMOUNT", _amountOfRawGoodsToBeDelivered);
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("RAW_MATERIALS", _rawMaterialsToBeDelivered.Name);
				return textObject;
			}
		}

		private TextObject FailQuestLogCounterOfferText
		{
			get
			{
				TextObject textObject = new TextObject("{=pskiLikd}After listening to the town merchants, you decided to keep the restrictions on the sale of goods. Your decision made the merchants happy but disappointed {QUEST_GIVER.LINK}.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject OnQuestCancelledDueToWarLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=vaUlAZba}Your clan is now at war with {QUEST_GIVER.LINK}. Your agreement with {QUEST_GIVER.LINK} was canceled.");
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

		internal static void AutoGeneratedStaticCollectObjectsArtisanCantSellProductsAtAFairPriceIssueQuest(object o, List<object> collectedObjects)
		{
			((ArtisanCantSellProductsAtAFairPriceIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_rawMaterialsToBeDelivered);
			collectedObjects.Add(_targetSettlement);
			collectedObjects.Add(_counterOfferHero);
			collectedObjects.Add(_targetHero);
			collectedObjects.Add(_playerStartsQuestLog);
		}

		internal static object AutoGeneratedGetMemberValue_rawMaterialsToBeDelivered(object o)
		{
			return ((ArtisanCantSellProductsAtAFairPriceIssueQuest)o)._rawMaterialsToBeDelivered;
		}

		internal static object AutoGeneratedGetMemberValue_amountOfRawGoodsToBeDelivered(object o)
		{
			return ((ArtisanCantSellProductsAtAFairPriceIssueQuest)o)._amountOfRawGoodsToBeDelivered;
		}

		internal static object AutoGeneratedGetMemberValue_targetSettlement(object o)
		{
			return ((ArtisanCantSellProductsAtAFairPriceIssueQuest)o)._targetSettlement;
		}

		internal static object AutoGeneratedGetMemberValue_deliveredRawGoods(object o)
		{
			return ((ArtisanCantSellProductsAtAFairPriceIssueQuest)o)._deliveredRawGoods;
		}

		internal static object AutoGeneratedGetMemberValue_counterOfferHero(object o)
		{
			return ((ArtisanCantSellProductsAtAFairPriceIssueQuest)o)._counterOfferHero;
		}

		internal static object AutoGeneratedGetMemberValue_targetHero(object o)
		{
			return ((ArtisanCantSellProductsAtAFairPriceIssueQuest)o)._targetHero;
		}

		internal static object AutoGeneratedGetMemberValue_counterOfferGiven(object o)
		{
			return ((ArtisanCantSellProductsAtAFairPriceIssueQuest)o)._counterOfferGiven;
		}

		internal static object AutoGeneratedGetMemberValue_counterOfferRefused(object o)
		{
			return ((ArtisanCantSellProductsAtAFairPriceIssueQuest)o)._counterOfferRefused;
		}

		internal static object AutoGeneratedGetMemberValue_rewardGold(object o)
		{
			return ((ArtisanCantSellProductsAtAFairPriceIssueQuest)o)._rewardGold;
		}

		internal static object AutoGeneratedGetMemberValue_playerStartsQuestLog(object o)
		{
			return ((ArtisanCantSellProductsAtAFairPriceIssueQuest)o)._playerStartsQuestLog;
		}

		public ArtisanCantSellProductsAtAFairPriceIssueQuest(string questId, Hero questGiver, CampaignTime duration, Settlement targetSettlement, ItemObject rawGoodsToBeDelivered, int amountOfRawGoodsToBeDelivered, int rewardGold, Hero targetHero, Hero counterOfferHero)
			: base(questId, questGiver, duration, rewardGold)
		{
			_targetSettlement = targetSettlement;
			_rawMaterialsToBeDelivered = rawGoodsToBeDelivered;
			_amountOfRawGoodsToBeDelivered = amountOfRawGoodsToBeDelivered;
			_deliveredRawGoods = 0;
			_counterOfferHero = counterOfferHero;
			_targetHero = targetHero;
			_rewardGold = rewardGold;
			SetDialogs();
			InitializeQuestOnCreation();
		}

		protected override void InitializeQuestOnGameLoad()
		{
			SetDialogs();
			Campaign.Current.ConversationManager.AddDialogFlow(GetCounterOfferDialogFlow(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetDeliveryDialogFlow(), this);
		}

		protected override void HourlyTick()
		{
		}

		protected override void SetDialogs()
		{
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(new TextObject("{=7KshRCtM}Excellent. I'll have the goods delivered to you right away.[if:convo_nonchalant]")).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.Consequence(QuestAcceptedConsequences)
				.CloseDialog();
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(new TextObject("{=6qKJ6Uzr}I believe the goods have been delivered to you.")).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=6ZCAQ1S9}Great. I will be heading out soon."))
				.NpcLine(new TextObject("{=3SBDbPjD}Good to hear that! Safe journeys."))
				.CloseDialog()
				.PlayerOption(new TextObject("{=JOaD2BlP}In due time. Let's not be too hasty here."))
				.NpcLine(new TextObject("{=ppi6eVos}As you wish."))
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
		}

		private void QuestAcceptedConsequences()
		{
			StartQuest();
			TextObject textObject = new TextObject("{=L700FNht}Delivered {RAW_MATERIAL}");
			textObject.SetTextVariable("RAW_MATERIAL", _rawMaterialsToBeDelivered.Name);
			_playerStartsQuestLog = AddDiscreteLog(PlayerStartsQuestLogText, textObject, _deliveredRawGoods, _amountOfRawGoodsToBeDelivered, null, hideInformation: true);
			PartyBase.MainParty.ItemRoster.AddToCounts(_rawMaterialsToBeDelivered, _amountOfRawGoodsToBeDelivered);
			AddTrackedObject(_targetSettlement);
			AddTrackedObject(_targetHero);
			Campaign.Current.ConversationManager.AddDialogFlow(GetCounterOfferDialogFlow(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetDeliveryDialogFlow(), this);
		}

		private int GetAvailableRequestedItemCountOnPlayer(ItemObject item)
		{
			return PartyBase.MainParty.ItemRoster.GetItemNumber(item);
		}

		private void BeforeGameMenuOpened(MenuCallbackArgs args)
		{
			if (!_counterOfferGiven && _counterOfferHero != null && Campaign.Current.GameMenuManager.NextLocation == null && GameStateManager.Current.ActiveState is MapState)
			{
				CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter), new ConversationCharacterData(_counterOfferHero.CharacterObject));
				_counterOfferGiven = true;
			}
		}

		private DialogFlow GetCounterOfferDialogFlow()
		{
			if (base.QuestGiver.CurrentSettlement.Owner != Hero.MainHero)
			{
				TextObject textObject = new TextObject("{=riYecgOn}We have heard rumors that you have purchased goods from {QUEST_GIVER.NAME}. Well, our laws require that only merchants resident in the city can buy goods directly from the artisans.[if:convo_annoyed][ib:hip]");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				TextObject npcText = new TextObject("{=5AvvGkk4}I'm sure what you did was an honest mistake, but there are laws. Hand over the contraband to me, and this will be the end of it.");
				if (_counterOfferHero.CharacterObject.GetPersona() == DefaultTraits.PersonaCurt)
				{
					npcText = new TextObject("{=PyjCZGlG}This town has laws about unauthorized trade. Turn over the goods to me now.");
				}
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=KSeIOHDh}(One of the merchants in the town comes to talk as you are preparing to depart.)")).Condition(() => _counterOfferHero == Hero.OneToOneConversationHero && !_counterOfferRefused)
					.NpcLine(textObject)
					.NpcLine(npcText)
					.BeginPlayerOptions()
					.PlayerOption(new TextObject("{=mWLA9sfT}I don't want to break the law. You can take the goods."))
					.NpcLine(new TextObject("{=xeboujso}That's the right call. You seem a responsible type.[if:convo_nonchalant]"))
					.Consequence(delegate
					{
						Campaign.Current.ConversationManager.ConversationEndOneShot += QuestFailedWithRefusal;
					})
					.CloseDialog()
					.PlayerOption(new TextObject("{=fe0uGUZb}That's just robbery under the cover of law. I'm not giving you anything."))
					.NpcLine(new TextObject("{=U9z7rvX5}Respectfully, you're making a big mistake and I think you're going to regret it.[if:convo_furious]"))
					.Consequence(delegate
					{
						Campaign.Current.ConversationManager.ConversationEndOneShot += RefuseCounterOfferConsequences;
					})
					.CloseDialog()
					.EndPlayerOptions();
			}
			TextObject textObject2 = new TextObject("{=DidANRmb}My {?PLAYER.GENDER}lady{?}lord{\\?}, we have heard rumors that you have purchased goods from {QUEST_GIVER.NAME}.[ib:demure] Surely this cannot be true. Our laws require that only merchants resident in the city can buy goods directly from the artisans.");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject2);
			TextObject npcText2 = new TextObject("{=bbz2eyAO}Perhaps, my {?PLAYER.GENDER}lady{?}lord{\\?}, it was a mistake? If you were to relinquish the goods, it would avoid a situation that might appear, well, a bit unseemly.");
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=KSeIOHDh}(One of the merchants in the town comes to talk as you are preparing to depart.)")).Condition(() => _counterOfferHero == Hero.OneToOneConversationHero && !_counterOfferRefused)
				.NpcLine(textObject2)
				.NpcLine(npcText2)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=EKvwhGtE}I suppose the laws of this town bind me as well. You can take the goods."))
				.NpcLine(new TextObject("{=dSy9Hawq}A just decision, my {?PLAYER.GENDER}lady{?}lord{\\?}."))
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += QuestFailedWithRefusal;
				})
				.CloseDialog()
				.PlayerOption(new TextObject("{=IUCkbHkT}I decide how the law here is applied, merchant."))
				.NpcLine(new TextObject("{=RYOuDxdu}Respectfully, my {?PLAYER.GENDER}lady{?}lord{\\?}, I must protest this. But of course you are free to do what you will."))
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += RefuseCounterOfferConsequences;
				})
				.CloseDialog()
				.EndPlayerOptions();
		}

		private DialogFlow GetDeliveryDialogFlow()
		{
			TextObject textObject = new TextObject("{=8nwZXNTk}About the task {QUEST_GIVER.LINK} gave me...");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
			TextObject textObject2 = new TextObject("{=mr7SgjAq}Yes... {QUEST_GIVER.LINK} sent word to us. We are expecting the {.%}{RAW_MATERIALS}{.%} that {?QUEST_GIVER.GENDER}she{?}he{\\?} had.");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject2);
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject2);
			textObject2.SetTextVariable("RAW_MATERIALS", _rawMaterialsToBeDelivered.Name);
			TextObject npcSecondLine = new TextObject("{=g7hZw8LI}Have you brought {REQUESTED_AMOUNT} {.%}{?(REQUESTED_AMOUNT > 1)}{PLURAL(RAW_MATERIALS)}{?}{RAW_MATERIALS}{\\?}{.%}? I have a fat purse of {REWARD}{GOLD_ICON} for you as promised.");
			npcSecondLine.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
			npcSecondLine.SetTextVariable("REWARD", _rewardGold);
			npcSecondLine.SetTextVariable("REQUESTED_AMOUNT", _amountOfRawGoodsToBeDelivered - _deliveredRawGoods);
			npcSecondLine.SetTextVariable("RAW_MATERIALS", _rawMaterialsToBeDelivered.Name);
			TextObject text = new TextObject("{=jidYZW2s}Most of the goods are here, but I lost some of them along the way. I have {AVAILABLE_AMOUNT} with me.");
			TextObject textObject3 = new TextObject("{=bPcO3Km2}Yes, things come up, but my agreement with {QUEST_GIVER.LINK} was for a fixed amount of goods. I don't want to negotiate a new deal. Please come back when you can get the full amount.[if:convo_nervous2][ib:closed]");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject3);
			TextObject playerDeliverItemsFully = new TextObject("{=yKz5e5H4}Yes. I have the goods right here. I brought {REQUESTED_AMOUNT} {.%}{?(REQUESTED_AMOUNT > 1)}{PLURAL(RAW_MATERIALS)}{?}{RAW_MATERIALS}{\\?}{.%} as we agreed.");
			playerDeliverItemsFully.SetTextVariable("REQUESTED_AMOUNT", _amountOfRawGoodsToBeDelivered - _deliveredRawGoods);
			playerDeliverItemsFully.SetTextVariable("RAW_MATERIALS", _rawMaterialsToBeDelivered.Name);
			TextObject textObject4 = new TextObject("{=S60sDU3j}Very good! Here is your money. Thank you.[if:convo_grateful]");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject4);
			TextObject text2 = new TextObject("{=4uKTfTg9}Sorry. I don't have anything for you this time.");
			TextObject npcText = new TextObject("{=JTfaqKyX}Well, try to bring it soon.[if:convo_thinking][ib:closed]");
			TextObject text3 = new TextObject("{=GVJS9ewr}You know, these goods are worth more to me than what you'll paying. I will keep them.");
			TextObject textObject5 = new TextObject("{=kWZ3cskl}What? That's a breach of contract. {QUEST_GIVER.LINK} will certainly hear about this...[if:convo_grave][ib:aggressive]");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject5);
			return DialogFlow.CreateDialogFlow("hero_main_options", 125).PlayerLine(textObject).Condition(delegate
			{
				if (_deliveredRawGoods > 0)
				{
					npcSecondLine.SetTextVariable("REQUESTED_AMOUNT", _amountOfRawGoodsToBeDelivered - _deliveredRawGoods);
					playerDeliverItemsFully.SetTextVariable("REQUESTED_AMOUNT", _amountOfRawGoodsToBeDelivered - _deliveredRawGoods);
				}
				return Settlement.CurrentSettlement == _targetSettlement && CharacterObject.OneToOneConversationCharacter.IsHero && CharacterObject.OneToOneConversationCharacter.HeroObject == _targetHero;
			})
				.NpcLine(textObject2)
				.NpcLine(npcSecondLine)
				.BeginPlayerOptions()
				.PlayerOption(text)
				.Condition(delegate
				{
					int availableRequestedItemCountOnPlayer = GetAvailableRequestedItemCountOnPlayer(_rawMaterialsToBeDelivered);
					int num = _amountOfRawGoodsToBeDelivered - _deliveredRawGoods;
					Campaign.Current.ConversationManager.GetCurrentDialogLine().SetTextVariable("AVAILABLE_AMOUNT", availableRequestedItemCountOnPlayer);
					return availableRequestedItemCountOnPlayer < num && availableRequestedItemCountOnPlayer != 0;
				})
				.NpcLine(textObject3)
				.Consequence(DeliverItemsPartiallyOnConsequence)
				.CloseDialog()
				.PlayerOption(playerDeliverItemsFully)
				.Condition(delegate
				{
					int availableRequestedItemCountOnPlayer = GetAvailableRequestedItemCountOnPlayer(_rawMaterialsToBeDelivered);
					int num = _amountOfRawGoodsToBeDelivered - _deliveredRawGoods;
					Campaign.Current.ConversationManager.GetCurrentDialogLine().SetTextVariable("REQUESTED_AMOUNT", _amountOfRawGoodsToBeDelivered - _deliveredRawGoods);
					Campaign.Current.ConversationManager.GetCurrentDialogLine().SetTextVariable("AVAILABLE_AMOUNT", num);
					return availableRequestedItemCountOnPlayer >= num && availableRequestedItemCountOnPlayer != 0;
				})
				.NpcLine(textObject4)
				.Consequence(DeliverItemsFullyOnConsequence)
				.CloseDialog()
				.PlayerOption(text3)
				.NpcLine(textObject5)
				.Consequence(DeliverItemsRejectOnConsequence)
				.CloseDialog()
				.PlayerOption(text2)
				.NpcLine(npcText)
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
		}

		private void DeliverItemsRejectOnConsequence()
		{
			TextObject textObject = new TextObject("{=yoiOIuI9}You refused to hand over the items. {QUEST_GIVER.LINK} must be furious.");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
			RelationshipChangeWithQuestGiver = -20;
			CompleteQuestWithFail(textObject);
			ChangeRelationAction.ApplyPlayerRelation(_targetHero, -15);
		}

		private void DeliverItemsPartiallyOnConsequence()
		{
			int availableRequestedItemCountOnPlayer = GetAvailableRequestedItemCountOnPlayer(_rawMaterialsToBeDelivered);
			_deliveredRawGoods += availableRequestedItemCountOnPlayer;
			PartyBase.MainParty.ItemRoster.AddToCounts(_rawMaterialsToBeDelivered, -availableRequestedItemCountOnPlayer);
			UpdateQuestTaskStage(_playerStartsQuestLog, _deliveredRawGoods);
			int variable = _amountOfRawGoodsToBeDelivered - _deliveredRawGoods;
			TextObject textObject = new TextObject("{=ffoqz6yP}You have delivered {DELIVERED_ITEM_COUNT} units of goods from {QUEST_GIVER.NAME}.");
			textObject.SetTextVariable("DELIVERED_ITEM_COUNT", availableRequestedItemCountOnPlayer);
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
			MBInformationManager.AddQuickInformation(textObject);
			TextObject textObject2 = new TextObject("{=3zdK4sm2}You have to bring in amount {REQUIRED_AMOUNT} more to fulfill your end of the deal.");
			textObject2.SetTextVariable("REQUIRED_AMOUNT", variable);
			MBInformationManager.AddQuickInformation(textObject2);
		}

		private void DeliverItemsFullyOnConsequence()
		{
			int num = _amountOfRawGoodsToBeDelivered - _deliveredRawGoods;
			_deliveredRawGoods = _amountOfRawGoodsToBeDelivered;
			PartyBase.MainParty.ItemRoster.AddToCounts(_rawMaterialsToBeDelivered, -num);
			UpdateQuestTaskStage(_playerStartsQuestLog, _deliveredRawGoods);
			TextObject textObject = new TextObject("{=WSLHfiwg}You have delivered the goods from {QUEST_GIVER.NAME}.");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
			MBInformationManager.AddQuickInformation(textObject);
			CompleteQuestWithSuccess();
		}

		private void QuestFailedWithRefusal()
		{
			foreach (Hero item in base.QuestGiver.CurrentSettlement.Notables.Where((Hero x) => x.IsMerchant))
			{
				ChangeRelationAction.ApplyPlayerRelation(item, 2);
				item.AddPower(10f);
			}
			RelationshipChangeWithQuestGiver = -5;
			TraitLevelingHelper.OnIssueFailed(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -50)
			});
			CompleteQuestWithFail(FailQuestLogCounterOfferText);
			RemoveRequestedItemsFromPlayer();
		}

		private void RefuseCounterOfferConsequences()
		{
			if (base.QuestGiver.CurrentSettlement.Owner != Hero.MainHero)
			{
				ChangeCrimeRatingAction.Apply(base.QuestGiver.CurrentSettlement.MapFaction, 5f);
			}
			_counterOfferHero.AddPower(-10f);
			_counterOfferRefused = true;
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.BeforeGameMenuOpenedEvent.AddNonSerializedListener(this, BeforeGameMenuOpened);
			CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
		}

		public override void OnHeroCanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
		{
			if (hero == _targetHero)
			{
				result = false;
			}
		}

		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (base.QuestGiver.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				CompleteQuestWithCancel(OnQuestCancelledDueToWarLogText);
			}
		}

		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
		{
			QuestHelper.CheckWarDeclarationAndFailOrCancelTheQuest(this, faction1, faction2, detail, PlayerDeclaredWarQuestLogText, OnQuestCancelledDueToWarLogText);
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
			else if (victim == _counterOfferHero)
			{
				CompleteQuestWithCancel();
			}
		}

		private void RemoveRequestedItemsFromPlayer()
		{
			int availableRequestedItemCountOnPlayer = GetAvailableRequestedItemCountOnPlayer(_rawMaterialsToBeDelivered);
			int num = _amountOfRawGoodsToBeDelivered - _deliveredRawGoods;
			PartyBase.MainParty.ItemRoster.AddToCounts(_rawMaterialsToBeDelivered, (availableRequestedItemCountOnPlayer < num) ? (-availableRequestedItemCountOnPlayer) : (-num));
		}

		public override void OnFailed()
		{
			base.QuestGiver.AddPower(-10f);
			base.QuestGiver.CurrentSettlement.Town.Prosperity -= 10f;
			foreach (Hero item in base.QuestGiver.CurrentSettlement.Notables.Where((Hero x) => x.IsMerchant))
			{
				item.AddPower(5f);
			}
		}

		protected override void OnFinalize()
		{
		}

		protected override void AfterLoad()
		{
		}

		protected override void OnCompleteWithSuccess()
		{
			base.QuestGiver.AddPower(15f);
			RelationshipChangeWithQuestGiver = 10;
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 50)
			});
			foreach (Hero item in base.QuestGiver.CurrentSettlement.Notables.Where((Hero x) => x.IsMerchant))
			{
				item.AddPower(-5f);
				ChangeRelationAction.ApplyPlayerRelation(item, -5);
			}
			base.QuestGiver.CurrentSettlement.Town.Prosperity += 30f;
			AddLog(SuccessQuestLogText);
			RemoveTrackedObject(_targetSettlement);
			RemoveTrackedObject(_targetHero);
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, _rewardGold);
		}

		protected override void OnTimedOut()
		{
			OnFailed();
			RelationshipChangeWithQuestGiver = -5;
			AddLog(FailQuestLogText);
		}
	}

	public class ArtisanCantSellProductsAtAFairPriceIssueTypeDefiner : SaveableTypeDefiner
	{
		public ArtisanCantSellProductsAtAFairPriceIssueTypeDefiner()
			: base(480000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(ArtisanCantSellProductsAtAFairPriceIssue), 1);
			AddClassDefinition(typeof(ArtisanCantSellProductsAtAFairPriceIssueQuest), 2);
		}
	}

	private const IssueBase.IssueFrequency ArtisanCantSellProductsAtAFairPriceIssueFrequency = IssueBase.IssueFrequency.Common;

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
	}

	public void OnCheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero))
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnStartIssue, typeof(ArtisanCantSellProductsAtAFairPriceIssue), IssueBase.IssueFrequency.Common));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(ArtisanCantSellProductsAtAFairPriceIssue), IssueBase.IssueFrequency.Common));
		}
	}

	private bool ConditionsHold(Hero issueGiver)
	{
		if (issueGiver.IsArtisan && SelectCounterOfferHero(issueGiver) != null)
		{
			return SelectTargetSettlement(issueGiver) != null;
		}
		return false;
	}

	private static Hero SelectCounterOfferHero(Hero issueGiver)
	{
		return issueGiver.CurrentSettlement.Notables.FirstOrDefault((Hero x) => x.CharacterObject.IsHero && x.CanHaveCampaignIssues() && x.CharacterObject.HeroObject != issueGiver && x.CharacterObject.HeroObject.IsMerchant);
	}

	private static Settlement SelectTargetSettlement(Hero issueGiver)
	{
		Settlement issueSettlement = issueGiver.CurrentSettlement;
		MobileParty.NavigationType navigationType = ((!issueSettlement.OwnerClan.HasNavalNavigationCapability) ? MobileParty.NavigationType.Default : MobileParty.NavigationType.All);
		float maximumDistanceForSettlementSelection = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(navigationType) * 2.25f;
		return SettlementHelper.FindNearestSettlementToMobileParty(MobileParty.MainParty, navigationType, (Settlement x) => x.IsTown && x != issueSettlement && x.Notables.Any((Hero y) => y.CanHaveCampaignIssues()) && Campaign.Current.Models.MapDistanceModel.GetDistance(x, issueSettlement, isFromPort: false, isTargetingPort: false, navigationType) < maximumDistanceForSettlementSelection);
	}

	private IssueBase OnStartIssue(in PotentialIssueData pid, Hero issueOwner)
	{
		return new ArtisanCantSellProductsAtAFairPriceIssue(issueOwner);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
