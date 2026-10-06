using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues;

public class ArtisanOverpricedGoodsIssueBehavior : CampaignBehaviorBase
{
	public class ArtisanOverpricedGoodsIssue : IssueBase
	{
		private const int RequiredTradeSkillLevelForSendingComp = 120;

		private const int BaseRewardGold = 300;

		private const int IssueDuration = 30;

		private const int QuestTimeLimit = 30;

		private const int MinimumAlternativeTroopTier = 2;

		[SaveableField(13)]
		private int _goldReward;

		[SaveableField(10)]
		private ItemObject _requestedTradeGood;

		public override int AlternativeSolutionBaseNeededMenCount => 4 + TaleWorlds.Library.MathF.Ceiling(6f * base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 3 + TaleWorlds.Library.MathF.Ceiling(5f * base.IssueDifficultyMultiplier);

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.Duration;

		protected override int RewardGold => _goldReward;

		[SaveableProperty(12)]
		private int RequestedTradeGoodAmount { get; set; }

		private int RequiredGoldForAlternativeSolution => TaleWorlds.Library.MathF.Floor((float)(_requestedTradeGood.Value * RequestedTradeGoodAmount) * 0.75f);

		[SaveableProperty(11)]
		public override Hero CounterOfferHero { get; protected set; }

		public override int NeededInfluenceForLordSolution => 15 + TaleWorlds.Library.MathF.Ceiling(35f * base.IssueDifficultyMultiplier);

		public override TextObject IssueBriefByIssueGiver => new TextObject("{=FKtkmwtb}I don't know if you know much about the law here... Craftsmen like me are required to buy our raw materials from local merchants. The other side of the bargain is that they offer us reasonable prices. But they're not doing that! They've come together and agreed on a price that's just too high. They don't care if it ruins us - they can always sell the goods elsewhere.[ib:hip][if:convo_thinking]");

		public override TextObject IssueAcceptByPlayer => new TextObject("{=j4V4fVBd}I see... How can I help?");

		protected override TextObject LordSolutionStartLog => new TextObject("{=p5OHK0Lh}You decided to issue a decree banning the merchants' price-fixing arrangement.");

		protected override TextObject LordSolutionCounterOfferRefuseLog => new TextObject("{=wrliXWLc}You approved the law which prevents price fixing in the markets and rejected the merchants' counter-offer.");

		protected override TextObject LordSolutionCounterOfferAcceptLog => new TextObject("{=bSguu34C}After listening to the town merchants, you decided to let the merchants continue price-fixing.");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=epcc2knY}Well, you get around, right? It wouldn't be hard for you to collect the {.%}{SELECTED_GOOD}{.%} we need. If you could bring, say {SELECTED_AMOUNT} {.%}{?SELECTED_AMOUNT > 1}{PLURAL(SELECTED_GOOD)}{?}{SELECTED_GOOD}{\\?}{.%} directly to me instead of selling to the merchants, I would gladly pay {REWARD_AMOUNT}{GOLD_ICON} for them. With this, the merchants would have to lower their prices.[if:convo_calm_friendly][ib:confident3]");
				textObject.SetTextVariable("SELECTED_GOOD", _requestedTradeGood.Name);
				textObject.SetTextVariable("SELECTED_AMOUNT", RequestedTradeGoodAmount);
				textObject.SetTextVariable("REWARD_AMOUNT", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=XvC1eLR1}Or, if you will not be able to acquire the goods yourself, you can perhaps assign one of your trusted companions to the task. Somewhat with a good understanding of trade and and around {TROOP_NUMBER} {?TROOP_NUMBER}troops{?}troop{\\?} could do it easily enough, along with {GOLD_COST}{GOLD_ICON} denars to make the purchases.[ib:normal2][if:convo_undecided_open]");
				textObject.SetTextVariable("TROOP_NUMBER", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("GOLD_COST", RequiredGoldForAlternativeSolution);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		public override TextObject IssueLordSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=JeL0aUOa}You could stop them with a decree, my {?PLAYER.GENDER}lady{?}lord{\\?}. All the craftsmen here would be very grateful.");
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
				return textObject;
			}
		}

		public override TextObject IssueQuestSolutionAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=mTuIMcLA}All right. I will bring {SELECTED_AMOUNT} {.%}{?SELECTED_AMOUNT > 1}{PLURAL(SELECTED_GOOD)}{?}{SELECTED_GOOD}{\\?}{.%} to you.");
				textObject.SetTextVariable("SELECTED_GOOD", _requestedTradeGood.Name);
				textObject.SetTextVariable("SELECTED_AMOUNT", RequestedTradeGoodAmount);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionAcceptByPlayer => new TextObject("{=79h7xNeL}I will have one of my companions take care of this.");

		public override TextObject IssueDiscussAlternativeSolution => new TextObject("{=E9GhODyh}I am very grateful to you for sparing your men to help us get what we need for a fair price.[if:convo_approving][ib:normal2]");

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=2YiPE05P}Thank you, my {?PLAYER.GENDER}lady{?}lord{\\?}. Your companion will no doubt be very helpful.");
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
				return textObject;
			}
		}

		public override TextObject IssueLordSolutionAcceptByPlayer => new TextObject("{=xTKHO53L}This is outrageous. I declare that from now on that there shall be no price fixing.");

		public override TextObject IssueLordSolutionResponseByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=6vPCEaSR}Thank you, my {?PLAYER.GENDER}lady{?}lord{\\?} that would be very good. Merchants will not like this but the hard-working artisans like me will be grateful to you for this.");
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
				return textObject;
			}
		}

		public override TextObject IssueLordSolutionCounterOfferBriefByOtherNpc => new TextObject("{=KSeIOHDh}(One of the merchants in the town comes to talk as you are preparing to depart.)");

		public override TextObject IssueLordSolutionCounterOfferExplanationByOtherNpc => new TextObject("{=8Tqv9ezH}We heard you wish to issue a decree changing our longstanding rules on pricing. Of course this is your right, but I'd like to remind you that we merchants pay significantly more taxes than the artisans. I would ask you to think of the finances of the town, and reconsider this.");

		public override TextObject IssueLordSolutionCounterOfferAcceptByPlayer => new TextObject("{=FyTGbvLS}On second thought, I have decided not to interfere with the merchant's business.");

		public override TextObject IssueLordSolutionCounterOfferAcceptResponseByOtherNpc
		{
			get
			{
				TextObject textObject = new TextObject("{=wynCFpsT}This is a wise decision, my {?PLAYER.GENDER}lady{?}lord{\\?}. Thank you.");
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
				return textObject;
			}
		}

		public override TextObject IssueLordSolutionCounterOfferDeclineByPlayer => new TextObject("{=IXuaflOe}I stand by my decision.");

		public override TextObject IssueLordSolutionCounterOfferDeclineResponseByOtherNpc => new TextObject("{=A8dyXgLz}That's a pity.");

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=aeKdXKnO}{ISSUE_GIVER.LINK}, an artisan from {SETTLEMENT}, has told you that a local merchant {COUNTER_OFFER.LINK} is asking ridiculous amounts of money for raw materials which {?ISSUE_GIVER.GENDER}she{?}he{\\?} needs to continue {?ISSUE_GIVER.GENDER}her{?}his{\\?} work. {?ISSUE_GIVER.GENDER}She{?}He{\\?} is willing to pay {REWARD_AMOUNT}{GOLD_ICON} for help obtaining the needed goods.{newline}You asked {COMPANION.LINK} to stay with {?ISSUE_GIVER.GENDER}her{?}him{\\?} and try to solve {?ISSUE_GIVER.GENDER}her{?}his{\\?} problems. You expect {COMPANION.LINK} to report back to you in {RETURN_DAYS} days.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("COUNTER_OFFER", CounterOfferHero.CharacterObject, textObject);
				textObject.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject);
				textObject.SetTextVariable("SETTLEMENT", base.IssueOwner.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("REWARD_AMOUNT", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		public override bool IsThereAlternativeSolution => true;

		public override bool IsThereLordSolution => true;

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=dt6kKXSL}Overpriced Raw Materials at {SETTLEMENT}");
				textObject.SetTextVariable("SETTLEMENT", base.IssueSettlement.Name);
				return textObject;
			}
		}

		public override TextObject Description
		{
			get
			{
				TextObject textObject = new TextObject("{=U90asNb9}Artisans in {SETTLEMENT} cannot continue their work because of the overpriced raw materials.");
				textObject.SetTextVariable("SETTLEMENT", base.IssueOwner.CurrentSettlement.Name);
				return textObject;
			}
		}

		public override TextObject IssueAsRumorInSettlement
		{
			get
			{
				TextObject textObject = new TextObject("{=FPbmUpy7}{QUEST_GIVER.NAME} is in quite a stew over the merchants' monopoly, so I hear.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=3r2HLPQ1}Your {COMPANION.LINK} has delivered {ISSUE_GIVER.LINK}'s goods as you promised.");
				textObject.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject);
				textObject.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject);
				return textObject;
			}
		}

		protected override int CompanionSkillRewardXP => (int)(400f + 1700f * base.IssueDifficultyMultiplier);

		internal static void AutoGeneratedStaticCollectObjectsArtisanOverpricedGoodsIssue(object o, List<object> collectedObjects)
		{
			((ArtisanOverpricedGoodsIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_requestedTradeGood);
			collectedObjects.Add(CounterOfferHero);
		}

		internal static object AutoGeneratedGetMemberValueRequestedTradeGoodAmount(object o)
		{
			return ((ArtisanOverpricedGoodsIssue)o).RequestedTradeGoodAmount;
		}

		internal static object AutoGeneratedGetMemberValueCounterOfferHero(object o)
		{
			return ((ArtisanOverpricedGoodsIssue)o).CounterOfferHero;
		}

		internal static object AutoGeneratedGetMemberValue_goldReward(object o)
		{
			return ((ArtisanOverpricedGoodsIssue)o)._goldReward;
		}

		internal static object AutoGeneratedGetMemberValue_requestedTradeGood(object o)
		{
			return ((ArtisanOverpricedGoodsIssue)o)._requestedTradeGood;
		}

		protected override void LordSolutionConsequenceWithRefuseCounterOffer()
		{
			ApplySuccessRewards(base.IssueOwner);
		}

		protected override void LordSolutionConsequenceWithAcceptCounterOffer()
		{
			ChangeRelationAction.ApplyPlayerRelation(CounterOfferHero, 5);
			RelationshipChangeWithIssueOwner = -5;
			base.IssueSettlement.Town.Prosperity -= 30f;
			TraitLevelingHelper.OnIssueSolvedThroughBetrayal(base.IssueOwner, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -50)
			});
			base.IssueOwner.AddPower(-5f);
		}

		public ArtisanOverpricedGoodsIssue(Hero issueOwner, Hero counterOfferHero, ItemObject requestedTradeGood)
			: base(issueOwner, CampaignTime.DaysFromNow(30f))
		{
			CounterOfferHero = counterOfferHero;
			_requestedTradeGood = requestedTradeGood;
			CalculateTradeGoodsAmountAndReward();
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.SettlementProsperity)
			{
				return -0.4f;
			}
			return 0f;
		}

		public override bool IssueStayAliveConditions()
		{
			if (base.IssueOwner.CurrentSettlement.Town.GetItemCategoryPriceIndex(_requestedTradeGood.ItemCategory) > 1.8f && CounterOfferHero.IsActive)
			{
				return CounterOfferHero.CurrentSettlement == base.IssueSettlement;
			}
			return false;
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			return ((hero.GetSkillValue(DefaultSkills.Roguery) >= hero.GetSkillValue(DefaultSkills.Trade)) ? DefaultSkills.Roguery : DefaultSkills.Trade, 120);
		}

		public override bool AlternativeSolutionCondition(out TextObject explanation)
		{
			if (QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2))
			{
				return QuestHelper.CheckGoldForAlternativeSolution(RequiredGoldForAlternativeSolution, out explanation);
			}
			return false;
		}

		public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
		{
			return character.Tier >= 2;
		}

		public override void AlternativeSolutionStartConsequence()
		{
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, RequiredGoldForAlternativeSolution);
		}

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			ApplySuccessRewards(base.IssueOwner);
		}

		public override bool LordSolutionCondition(out TextObject explanation)
		{
			if (Clan.PlayerClan == Settlement.CurrentSettlement.OwnerClan)
			{
				explanation = null;
				return true;
			}
			explanation = new TextObject("{=bItEf0WN}You need to be the {?PLAYER.GENDER}lady{?}lord{\\?} of this settlement.");
			StringHelpers.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject, explanation);
			return false;
		}

		private void ApplySuccessRewards(Hero issueGiver)
		{
			issueGiver.AddPower(10f);
			ChangeRelationAction.ApplyPlayerRelation(issueGiver, 5);
			ChangeRelationAction.ApplyPlayerRelation(CounterOfferHero, -10);
			CounterOfferHero.AddPower(-10f);
			issueGiver.CurrentSettlement.Town.Prosperity += 30f;
		}

		public override IssueFrequency GetFrequency()
		{
			return IssueFrequency.Common;
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

		private void CalculateTradeGoodsAmountAndReward()
		{
			RequestedTradeGoodAmount = TaleWorlds.Library.MathF.Max((int)(10000f / (float)_requestedTradeGood.Value * base.IssueDifficultyMultiplier), 1);
			_goldReward = (int)((float)QuestHelper.GetAveragePriceOfItemInTheWorld(_requestedTradeGood) * 1.5f * (float)RequestedTradeGoodAmount);
		}

		protected override void OnGameLoad()
		{
			if (RequestedTradeGoodAmount == 0 || _goldReward == 0)
			{
				CalculateTradeGoodsAmountAndReward();
			}
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new ArtisanOverpricedGoodsIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(30f), _requestedTradeGood, RewardGold, RequestedTradeGoodAmount, CounterOfferHero);
		}
	}

	public class ArtisanOverpricedGoodsIssueQuest : QuestBase
	{
		[SaveableField(20)]
		private int _rewardGold;

		[SaveableField(30)]
		private readonly ItemObject _requestedTradeGood;

		[SaveableField(60)]
		private readonly int _requestedTradeGoodAmount;

		[SaveableField(40)]
		private int _givenTradeGoods;

		[SaveableField(50)]
		private JournalLog _playerStartsQuestLog;

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=dt6kKXSL}Overpriced Raw Materials at {SETTLEMENT}");
				textObject.SetTextVariable("SETTLEMENT", base.QuestGiver.CurrentSettlement.Name);
				return textObject;
			}
		}

		private Hero AntagonistHero => base.QuestGiver.CurrentSettlement.Notables.FirstOrDefault((Hero x) => x != base.QuestGiver && x.IsMerchant && x.GetTraitLevel(DefaultTraits.Mercy) <= 0);

		public override bool IsRemainingTimeHidden => false;

		private TextObject PlayerStartsQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=IL3QWZ93}{QUEST_GIVER.LINK}, an artisan from {SETTLEMENT}, has told you about local merchant asking ridiculous amounts of money for the raw materials that {?QUEST_GIVER.GENDER}she{?}he{\\?} needs to continue {?QUEST_GIVER.GENDER}her{?}his{\\?} work. You said that you can bring {?QUEST_GIVER.GENDER}her{?}him{\\?} {REQUESTED_AMOUNT} {.%}{?REQUESTED_AMOUNT > 1}{PLURAL(REQUESTED_GOOD)}{?}{REQUESTED_GOOD}{\\?}{.%}. {?QUEST_GIVER.GENDER}She{?}He{\\?} is willing to pay {REWARD_AMOUNT}{GOLD_ICON} for these items.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("REQUESTED_AMOUNT", _requestedTradeGoodAmount);
				textObject.SetTextVariable("REQUESTED_GOOD", _requestedTradeGood.Name);
				textObject.SetTextVariable("REWARD_AMOUNT", _rewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		private TextObject SuccessQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=6XvszYj2}You brought {REQUESTED_AMOUNT} {.%}{?REQUESTED_AMOUNT > 1}{PLURAL(REQUESTED_GOOD)}{?}{REQUESTED_GOOD}{\\?}{.%} to {?QUEST_GIVER.GENDER}her{?}him{\\?} as promised.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("REQUESTED_AMOUNT", _requestedTradeGoodAmount);
				textObject.SetTextVariable("REQUESTED_GOOD", _requestedTradeGood.Name);
				return textObject;
			}
		}

		private TextObject TimeoutQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=Ts5dZih4}You couldn't fully bring {.%}{REQUESTED_GOOD}{.%} to {?QUEST_GIVER.GENDER}her{?}him{\\?} in time.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("REQUESTED_GOOD", _requestedTradeGood.Name);
				return textObject;
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsArtisanOverpricedGoodsIssueQuest(object o, List<object> collectedObjects)
		{
			((ArtisanOverpricedGoodsIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_requestedTradeGood);
			collectedObjects.Add(_playerStartsQuestLog);
		}

		internal static object AutoGeneratedGetMemberValue_rewardGold(object o)
		{
			return ((ArtisanOverpricedGoodsIssueQuest)o)._rewardGold;
		}

		internal static object AutoGeneratedGetMemberValue_requestedTradeGood(object o)
		{
			return ((ArtisanOverpricedGoodsIssueQuest)o)._requestedTradeGood;
		}

		internal static object AutoGeneratedGetMemberValue_requestedTradeGoodAmount(object o)
		{
			return ((ArtisanOverpricedGoodsIssueQuest)o)._requestedTradeGoodAmount;
		}

		internal static object AutoGeneratedGetMemberValue_givenTradeGoods(object o)
		{
			return ((ArtisanOverpricedGoodsIssueQuest)o)._givenTradeGoods;
		}

		internal static object AutoGeneratedGetMemberValue_playerStartsQuestLog(object o)
		{
			return ((ArtisanOverpricedGoodsIssueQuest)o)._playerStartsQuestLog;
		}

		protected override void InitializeQuestOnGameLoad()
		{
			SetDialogs();
			Campaign.Current.ConversationManager.AddDialogFlow(GetMerchantDialogFlow(), this);
		}

		protected override void HourlyTick()
		{
		}

		public ArtisanOverpricedGoodsIssueQuest(string questId, Hero questGiver, CampaignTime dueTime, ItemObject requestedTradeGood, int rewardGold, int requestedTradeGoodAmount, Hero counterOfferHero)
			: base(questId, questGiver, dueTime, rewardGold)
		{
			_requestedTradeGood = requestedTradeGood;
			_requestedTradeGoodAmount = requestedTradeGoodAmount;
			_rewardGold = rewardGold;
			SetDialogs();
			InitializeQuestOnCreation();
		}

		protected override void SetDialogs()
		{
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(new TextObject("{=e1sSXEOh}This is excellent news. If you could bring the goods, it would be an immense help, and the merchants would have to lower their prices. Thank you.[if:convo_excited][ib:hip2]")).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.Consequence(QuestAcceptedConsequences)
				.CloseDialog();
			TextObject textObject = new TextObject("{=fKnkkWH1}Have you brought any {.%}{REQUESTED_GOOD}{.%}? My stocks are running out and I need some very soon.");
			textObject.SetTextVariable("REQUESTED_GOOD", _requestedTradeGood.Name);
			TextObject textObject2 = new TextObject("{=bPbzwMat}I could only deliver {AMOUNT_TO_DELIVER} {.%}{?AMOUNT_TO_DELIVER > 1}{PLURAL(REQUESTED_GOOD)}{?}{REQUESTED_GOOD}{\\?}{.%}.");
			textObject2.SetTextVariable("REQUESTED_GOOD", _requestedTradeGood.Name);
			textObject2.SetTextVariable("AMOUNT_TO_DELIVER", GetAvailableRequestedItemCountOnPlayer());
			TextObject npcText = new TextObject("{=IfF3OHNT}Thank you. These will be very useful. If you can get your hands on more, please bring them directly to me.");
			TextObject textObject3 = new TextObject("{=1LzohiMf}I brought {AMOUNT_TO_DELIVER} {.%}{?AMOUNT_TO_DELIVER > 1}{PLURAL(REQUESTED_GOOD)}{?}{REQUESTED_GOOD}{\\?}{.%} as we agreed.");
			textObject3.SetTextVariable("AMOUNT_TO_DELIVER", _requestedTradeGoodAmount - _givenTradeGoods);
			textObject3.SetTextVariable("REQUESTED_GOOD", _requestedTradeGood.Name);
			TextObject npcText2 = new TextObject("{=cxQGUbUD}I'm so grateful! You've kept my workshop running and my family fed. Please take this money and some extra to cover your costs.[if:convo_grateful][ib:normal2]");
			TextObject text = new TextObject("{=4uKTfTg9}Sorry. I don't have anything for you this time.");
			TextObject npcText3 = new TextObject("{=YWorGaI1}Ah... We'll get by. I won't lie. It will be hard. I just hope you can deliver some soon.[if:convo_normal][ib:closed]");
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(textObject).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.BeginPlayerOptions()
				.PlayerOption(textObject2)
				.Condition(delegate
				{
					int availableRequestedItemCountOnPlayer = GetAvailableRequestedItemCountOnPlayer();
					Campaign.Current.ConversationManager.GetCurrentDialogLine().SetTextVariable("REQUESTED_GOOD", _requestedTradeGood.Name);
					Campaign.Current.ConversationManager.GetCurrentDialogLine().SetTextVariable("AMOUNT_TO_DELIVER", availableRequestedItemCountOnPlayer);
					return availableRequestedItemCountOnPlayer > 0 && availableRequestedItemCountOnPlayer < _requestedTradeGoodAmount - _givenTradeGoods;
				})
				.Consequence(DeliverItemsPartiallyOnConsequence)
				.NpcLine(npcText)
				.CloseDialog()
				.PlayerOption(textObject3)
				.Condition(delegate
				{
					int availableRequestedItemCountOnPlayer = GetAvailableRequestedItemCountOnPlayer();
					Campaign.Current.ConversationManager.GetCurrentDialogLine().SetTextVariable("AMOUNT_TO_DELIVER", _requestedTradeGoodAmount - _givenTradeGoods);
					Campaign.Current.ConversationManager.GetCurrentDialogLine().SetTextVariable("REQUESTED_GOOD", _requestedTradeGood.Name);
					return availableRequestedItemCountOnPlayer >= _requestedTradeGoodAmount - _givenTradeGoods;
				})
				.Consequence(DeliverItemsFullyOnConsequence)
				.NpcLine(npcText2)
				.Consequence(base.CompleteQuestWithSuccess)
				.CloseDialog()
				.PlayerOption(text)
				.NpcLine(npcText3)
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
		}

		private int GetAvailableRequestedItemCountOnPlayer()
		{
			return PartyBase.MainParty.ItemRoster.GetItemNumber(_requestedTradeGood);
		}

		private void DeliverItemsPartiallyOnConsequence()
		{
			int num = GetAvailableRequestedItemCountOnPlayer();
			if (PartyBase.MainParty.ItemRoster.ElementAt(PartyBase.MainParty.ItemRoster.FindIndex((ItemObject x) => x == _requestedTradeGood)).Amount <= 0)
			{
				num = 0;
			}
			PartyBase.MainParty.ItemRoster.AddToCounts(_requestedTradeGood, -num);
			_givenTradeGoods += num;
			UpdateQuestTaskStage(_playerStartsQuestLog, _givenTradeGoods);
		}

		private void DeliverItemsFullyOnConsequence()
		{
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 50)
			});
			ChangeCrimeRatingAction.Apply(base.QuestGiver.MapFaction, 5f);
			PartyBase.MainParty.ItemRoster.AddToCounts(_requestedTradeGood, -(_requestedTradeGoodAmount - _givenTradeGoods));
			_givenTradeGoods = _requestedTradeGoodAmount;
			UpdateQuestTaskStage(_playerStartsQuestLog, _givenTradeGoods);
		}

		private void QuestAcceptedConsequences()
		{
			StartQuest();
			_playerStartsQuestLog = AddDiscreteLog(PlayerStartsQuestLogText, new TextObject("{=yw62BLhy}Delivered Goods"), _givenTradeGoods, _requestedTradeGoodAmount, null, hideInformation: true);
			Campaign.Current.ConversationManager.AddDialogFlow(GetMerchantDialogFlow(), this);
		}

		private DialogFlow GetMerchantDialogFlow()
		{
			TextObject playerText = new TextObject("{=YNxGcaJI}I want to talk to you about the outlandish prices you're asking for the goods you sell the artisans.");
			TextObject npcText = new TextObject("{=UwEbBanm}These are the laws of our town. The artisans don't complain when the laws require us to buy tools from them.[if:convo_bored][ib:closed2]");
			TextObject npcText2 = new TextObject("{=sPEZq0Yk}What's sauce for the goose is sauce for the gander. We've done business for years like this and we're not just going to change things because one party complains.[if:convo_mocking_teasing][ib:hip]");
			TextObject text = new TextObject("{=VzzQX4EZ}Maybe you're right. I won't get involved in this.");
			TextObject text2 = new TextObject("{=KbFJJfl6}I am sorry. I will do what I must.");
			TextObject npcText3 = new TextObject("{=yccVH4KD}Thank you for listening to the voice of reason.");
			TextObject npcText4 = new TextObject("{=A8dyXgLz}That's a pity.");
			return DialogFlow.CreateDialogFlow("hero_main_options", 125).PlayerLine(playerText).Condition(IsSuitableToTalk)
				.NpcLine(npcText)
				.NpcLine(npcText2)
				.BeginPlayerOptions()
				.PlayerOption(text)
				.NpcLine(npcText3)
				.Consequence(AcceptCounterOffer)
				.CloseDialog()
				.PlayerOption(text2)
				.NpcLine(npcText4)
				.CloseDialog();
		}

		private bool IsSuitableToTalk()
		{
			if (Settlement.CurrentSettlement == base.QuestGiver.CurrentSettlement && CharacterObject.OneToOneConversationCharacter.IsHero)
			{
				return CharacterObject.OneToOneConversationCharacter.HeroObject == AntagonistHero;
			}
			return false;
		}

		private void AcceptCounterOffer()
		{
			Hero antagonistHero = AntagonistHero;
			TextObject textObject = new TextObject("{=q4wWEbsa}You decided not to deliver the items after listening to {COUNTER_OFFER_HERO.LINK}.");
			StringHelpers.SetCharacterProperties("COUNTER_OFFER_HERO", antagonistHero.CharacterObject, textObject);
			ChangeRelationAction.ApplyPlayerRelation(antagonistHero, 5);
			antagonistHero.AddPower(5f);
			TraitLevelingHelper.OnIssueSolvedThroughBetrayal(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -50)
			});
			base.QuestGiver.CurrentSettlement.Town.Prosperity -= 30f;
			CompleteQuestWithFail(textObject);
		}

		protected override void OnCompleteWithSuccess()
		{
			base.QuestGiver.AddPower(10f);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 50)
			});
			base.QuestGiver.CurrentSettlement.Town.Prosperity += 30f;
			RelationshipChangeWithQuestGiver = 5;
			if (AntagonistHero != null)
			{
				AntagonistHero.AddPower(-10f);
				ChangeRelationAction.ApplyPlayerRelation(AntagonistHero, -10);
			}
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, _rewardGold);
			AddLog(SuccessQuestLogText);
		}

		public override void OnFailed()
		{
			base.QuestGiver.AddPower(-10f);
			RelationshipChangeWithQuestGiver = -5;
			base.QuestGiver.CurrentSettlement.Town.Prosperity -= 20f;
		}

		protected override void OnTimedOut()
		{
			AddLog(TimeoutQuestLogText);
			base.QuestGiver.AddPower(-20f);
			RelationshipChangeWithQuestGiver = -5;
			base.QuestGiver.CurrentSettlement.Town.Prosperity -= 50f;
		}
	}

	public class ArtisanOverpricedGoodsIssueTypeDefiner : SaveableTypeDefiner
	{
		public ArtisanOverpricedGoodsIssueTypeDefiner()
			: base(470000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(ArtisanOverpricedGoodsIssue), 1);
			AddClassDefinition(typeof(ArtisanOverpricedGoodsIssueQuest), 2);
		}
	}

	private const IssueBase.IssueFrequency ArtisanOverpricedGoodsIssueFrequency = IssueBase.IssueFrequency.Common;

	private const float HighestPriceIndexAtTown = 2f;

	private static IEnumerable<ItemObject> PossibleRequestedItems
	{
		get
		{
			yield return MBObjectManager.Instance.GetObject<ItemObject>("cow");
			yield return MBObjectManager.Instance.GetObject<ItemObject>("sheep");
			yield return MBObjectManager.Instance.GetObject<ItemObject>("wool");
			yield return MBObjectManager.Instance.GetObject<ItemObject>("iron");
			yield return MBObjectManager.Instance.GetObject<ItemObject>("leather");
			yield return MBObjectManager.Instance.GetObject<ItemObject>("hardwood");
		}
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	public void OnCheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero, out var antagonistMerchant, out var requestedItem))
		{
			KeyValuePair<Hero, ItemObject> keyValuePair = new KeyValuePair<Hero, ItemObject>(antagonistMerchant, requestedItem);
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnStartIssue, typeof(ArtisanOverpricedGoodsIssue), IssueBase.IssueFrequency.Common, keyValuePair));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(ArtisanOverpricedGoodsIssue), IssueBase.IssueFrequency.Common));
		}
	}

	private Hero GetAntagonistMerchant(Hero issueOwner)
	{
		return issueOwner.CurrentSettlement.Notables.GetRandomElementWithPredicate((Hero x) => x != issueOwner && x.IsMerchant && x.GetTraitLevel(DefaultTraits.Mercy) <= 0 && x.CanHaveCampaignIssues());
	}

	private bool ConditionsHold(Hero IssueOwner, out Hero antagonistMerchant, out ItemObject requestedItem)
	{
		antagonistMerchant = null;
		requestedItem = null;
		if (IssueOwner.CurrentSettlement != null && IssueOwner.CurrentSettlement.IsTown && IssueOwner.IsArtisan)
		{
			antagonistMerchant = GetAntagonistMerchant(IssueOwner);
			if (antagonistMerchant != null)
			{
				foreach (ItemObject possibleRequestedItem in PossibleRequestedItems)
				{
					if (IssueOwner.CurrentSettlement.Town.GetItemCategoryPriceIndex(possibleRequestedItem.ItemCategory) > 2f)
					{
						requestedItem = possibleRequestedItem;
						return true;
					}
				}
			}
			return false;
		}
		return false;
	}

	private IssueBase OnStartIssue(in PotentialIssueData pid, Hero issueOwner)
	{
		KeyValuePair<Hero, ItemObject> keyValuePair = (KeyValuePair<Hero, ItemObject>)pid.RelatedObject;
		return new ArtisanOverpricedGoodsIssue(issueOwner, keyValuePair.Key, keyValuePair.Value);
	}
}
