using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues;

public class GangLeaderNeedsToOffloadStolenGoodsIssueBehavior : CampaignBehaviorBase
{
	public class GangLeaderNeedsToOffloadStolenGoodsIssue : IssueBase
	{
		private const int QuestDurationInDays = 20;

		private static readonly string[] PossibleStolenItems = new string[4] { "jewelry", "fur", "silver", "velvet" };

		[SaveableField(20)]
		private readonly int _randomForStolenTradeGood;

		[SaveableField(30)]
		private readonly Settlement _issueHideout;

		private const int AlternativeSolutionMinimumTroopLevel = 2;

		private const int CompanionRequiredSkillLevel = 120;

		private ItemObject StolenTradeGood => Game.Current.ObjectManager.GetObject<ItemObject>(PossibleStolenItems[_randomForStolenTradeGood]);

		private int StolenTradeGoodAmount => TaleWorlds.Library.MathF.Ceiling(10000f / (float)base.IssueOwner.CurrentSettlement.Town.GetItemPrice(StolenTradeGood) * base.IssueDifficultyMultiplier);

		private int StolenTradeGoodPrice => TaleWorlds.Library.MathF.Round((float)base.IssueOwner.CurrentSettlement.Town.GetItemPrice(StolenTradeGood) * 0.5f * (float)StolenTradeGoodAmount);

		protected override int RewardGold => TaleWorlds.Library.MathF.Round((float)base.IssueOwner.CurrentSettlement.Town.GetItemPrice(StolenTradeGood) * 0.4f * (float)StolenTradeGoodAmount);

		[SaveableProperty(10)]
		public override Hero CounterOfferHero { get; protected set; }

		public override bool IsThereAlternativeSolution => true;

		protected override bool IssueQuestCanBeDuplicated => false;

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=cODzH0Ke}Well, if you don't want to go yourself to meet them, I'm sure you can send a trusted aide with 10 or so men to load and guard those {STOLEN_GOODS_SIZE} {.%}{?(STOLEN_GOODS_SIZE > 1)}{PLURAL(STOLEN_GOOD)}{?}{STOLEN_GOOD}{\\?}{.%}. I am sure our people can seal the deal by themselves.");
				textObject.SetTextVariable("STOLEN_GOODS_SIZE", StolenTradeGoodAmount);
				textObject.SetTextVariable("STOLEN_GOOD", StolenTradeGood.Name);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionAcceptByPlayer => new TextObject("{=ltiz6e11}Let them know my companion will be meeting them shortly.");

		public override bool IsThereLordSolution => false;

		public override TextObject IssueQuestSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=qh1Ot0F9}They have {STOLEN_GOODS_SIZE} {.%}{?(STOLEN_GOODS_SIZE > 1)}{PLURAL(STOLEN_GOOD)}{?}{STOLEN_GOOD}{\\?}{.%}. They're asking {REQUESTED_PRICE}{GOLD_ICON}. You know that this is the best price you can find anywhere in this district.");
				textObject.SetTextVariable("STOLEN_GOODS_SIZE", StolenTradeGoodAmount);
				textObject.SetTextVariable("STOLEN_GOOD", StolenTradeGood.Name);
				textObject.SetTextVariable("REQUESTED_PRICE", StolenTradeGoodPrice);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		public override TextObject IssueBriefByIssueGiver => new TextObject("{=babDRxdu}I am trying to help some friends. They want a buyer for some goods, that, well, let's say that my friends might not have paid for these goods. These lads are camped close by, but would prefer not to come into town. You see?[if:convo_mocking_aristocratic][ib:hip]");

		public override TextObject IssueAcceptByPlayer => new TextObject("{=rMySUGLu}I am interested. What are they selling?");

		public override TextObject IssueQuestSolutionAcceptByPlayer => new TextObject("{=t1tNPPNf}I can go meet them and seal this deal myself.");

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver => new TextObject("{=ov0u6ahh}All right. It was a pleasure doing business with you.");

		public override TextObject IssueDiscussAlternativeSolution => new TextObject("{=A2JWExWL}I hope our respective friends come to an agreement.");

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=izuhKnXy}Purchase Stolen Goods from {ISSUE_GIVER.NAME}");
				textObject.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject);
				return textObject;
			}
		}

		public override TextObject Description
		{
			get
			{
				TextObject textObject = new TextObject("{=3WFgPigk}{ISSUE_GIVER.NAME} wants to sell you stolen goods. The price is low because of the added risk.");
				textObject.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject);
				return textObject;
			}
		}

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=riKyIyaa}{QUEST_GIVER.LINK}, a gang leader from {QUEST_GIVER_SETTLEMENT}, has offered you to sell {STOLEN_GOODS_SIZE} {.%}{?(STOLEN_GOODS_SIZE > 1)}{PLURAL(STOLEN_GOOD)}{?}{STOLEN_GOOD}{\\?}{.%} for half the normal price, because they were stolen... {?QUEST_GIVER.GENDER}She{?}He{\\?} says they are hidden in a remote location. You asked your companion to go buy the goods.");
				textObject.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject);
				textObject.SetTextVariable("QUEST_GIVER_SETTLEMENT", base.IssueOwner.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("STOLEN_GOODS_SIZE", StolenTradeGoodAmount);
				textObject.SetTextVariable("STOLEN_GOOD", StolenTradeGood.Name);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=XOB0SMXC}{COMPANION.LINK} has successfully offloaded the stolen goods. {?QUEST_GIVER.GENDER}She{?}He{\\?} returned to the party with {STOLEN_GOODS_SIZE} {.%}{?(STOLEN_GOODS_SIZE > 1)}{PLURAL(STOLEN_GOOD)}{?}{STOLEN_GOOD}{\\?}{.%}.");
				textObject.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject);
				textObject.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject);
				textObject.SetTextVariable("STOLEN_GOODS_SIZE", StolenTradeGoodAmount);
				textObject.SetTextVariable("STOLEN_GOOD", StolenTradeGood.Name);
				return textObject;
			}
		}

		protected override int CompanionSkillRewardXP => 1000 + (int)(1250f * base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => (int)(7f + 10f * base.IssueDifficultyMultiplier);

		public override int AlternativeSolutionBaseNeededMenCount => 10;

		internal static void AutoGeneratedStaticCollectObjectsGangLeaderNeedsToOffloadStolenGoodsIssue(object o, List<object> collectedObjects)
		{
			((GangLeaderNeedsToOffloadStolenGoodsIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_issueHideout);
			collectedObjects.Add(CounterOfferHero);
		}

		internal static object AutoGeneratedGetMemberValueCounterOfferHero(object o)
		{
			return ((GangLeaderNeedsToOffloadStolenGoodsIssue)o).CounterOfferHero;
		}

		internal static object AutoGeneratedGetMemberValue_randomForStolenTradeGood(object o)
		{
			return ((GangLeaderNeedsToOffloadStolenGoodsIssue)o)._randomForStolenTradeGood;
		}

		internal static object AutoGeneratedGetMemberValue_issueHideout(object o)
		{
			return ((GangLeaderNeedsToOffloadStolenGoodsIssue)o)._issueHideout;
		}

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			List<SkillObject> alternativeSolutionMeleeSkills = QuestHelper.GetAlternativeSolutionMeleeSkills();
			alternativeSolutionMeleeSkills.Add(DefaultSkills.Roguery);
			alternativeSolutionMeleeSkills.Add(DefaultSkills.Tactics);
			return (TaleWorlds.Core.Extensions.MaxBy(alternativeSolutionMeleeSkills, hero.GetSkillValue), 120);
		}

		public override bool AlternativeSolutionCondition(out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
			RelationshipChangeWithIssueOwner = 10;
			base.IssueOwner.AddPower(5f);
			CounterOfferHero.AddPower(-5f);
			foreach (Hero item in base.IssueOwner.CurrentSettlement.Notables.WhereQ((Hero notable) => notable.IsMerchant))
			{
				item.AddPower(-3f);
			}
			MobileParty.MainParty.ItemRoster.AddToCounts(StolenTradeGood, StolenTradeGoodAmount);
			TraitLevelingHelper.OnIssueSolvedThroughAlternativeSolution(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Calculating, 50)
			});
		}

		public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
		{
			return character.Tier >= 2;
		}

		public GangLeaderNeedsToOffloadStolenGoodsIssue(Hero issueOwner, Settlement hideout)
			: base(issueOwner, CampaignTime.DaysFromNow(15f))
		{
			_randomForStolenTradeGood = MBRandom.RandomInt(0, PossibleStolenItems.Length);
			_issueHideout = hideout;
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.IssueOwnerPower)
			{
				return -1f;
			}
			if (issueEffect == DefaultIssueEffects.SettlementSecurity)
			{
				return 1f;
			}
			return 0f;
		}

		public override bool IssueStayAliveConditions()
		{
			if (base.IssueOwner.CurrentSettlement.Town.Security < 90f && CounterOfferHero.IsActive && CounterOfferHero.CurrentSettlement == base.IssueSettlement && _issueHideout != null)
			{
				return _issueHideout.Hideout.IsInfested;
			}
			return false;
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}

		protected override void AfterIssueCreation()
		{
			CounterOfferHero = base.IssueOwner.CurrentSettlement.Notables.FirstOrDefault((Hero x) => x != base.IssueOwner && x.IsMerchant) ?? base.IssueOwner.CurrentSettlement.Notables.FirstOrDefault();
		}

		protected override void OnGameLoad()
		{
			if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.0"))
			{
				Campaign.Current.IssueManager.DeactivateIssue(this);
			}
		}

		public override void IsSettlementBusy(Settlement settlement, object asker, ref int priority)
		{
			if (asker != this && settlement == _issueHideout)
			{
				priority = Math.Max(priority, 100);
			}
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new GangLeaderNeedsToOffloadStolenGoodsIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(20f), StolenTradeGood, _issueHideout, StolenTradeGoodPrice, RewardGold, StolenTradeGoodAmount, CounterOfferHero);
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
			if (issueGiver.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				flag |= PreconditionFlags.AtWar;
			}
			if (Clan.PlayerClan == issueGiver.CurrentSettlement.OwnerClan)
			{
				flag |= PreconditionFlags.PlayerIsOwnerOfSettlement;
			}
			return flag == PreconditionFlags.None;
		}

		protected override void OnIssueFinalized()
		{
		}
	}

	public class GangLeaderNeedsToOffloadStolenGoodsIssueQuest : QuestBase
	{
		[SaveableField(105)]
		private readonly ItemObject _stolenTradeGood;

		[SaveableField(106)]
		private readonly int _stolenTradeGoodAmount;

		[SaveableField(107)]
		private readonly int _stolenTradeGoodPrice;

		[SaveableField(109)]
		private bool _counterOfferGiven;

		[SaveableField(111)]
		private readonly int _counterOfferGold;

		[SaveableField(112)]
		private JournalLog _playerStartsQuestLog;

		[SaveableField(113)]
		private readonly Hero _counterOfferHero;

		[SaveableField(114)]
		private bool _talkedWithBanditLeader;

		[SaveableField(115)]
		private bool _isPayingForGoods;

		[SaveableField(116)]
		private bool _isFightingForGoods;

		[SaveableField(117)]
		private readonly Settlement _questHideout;

		[SaveableField(118)]
		private bool _playerHasTheGoods;

		public override bool IsRemainingTimeHidden => false;

		private CharacterObject BanditLeader => _questHideout.Culture.BanditChief;

		private TextObject PlayerStartsQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=uaYGZUDs}{QUEST_GIVER.LINK}, a gang leader from {QUEST_GIVER_SETTLEMENT}, has offered you to sell {STOLEN_GOODS_SIZE} {.%}{?(STOLEN_GOODS_SIZE > 1)}{PLURAL(STOLEN_GOOD)}{?}{STOLEN_GOOD}{\\?}{.%} for half the normal price, because they were stolen.. {?QUEST_GIVER.GENDER}She{?}He{\\?} says they are hidden in a remote location. You agreed to buy the goods.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_GIVER_SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("STOLEN_GOOD", _stolenTradeGood.Name);
				textObject.SetTextVariable("STOLEN_GOODS_SIZE", _stolenTradeGoodAmount);
				return textObject;
			}
		}

		private TextObject SuccessQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=mrNdB0Hh}You refused the {MERCHANT.LINK}'s request and kept the goods.");
				textObject.SetCharacterProperties("MERCHANT", _counterOfferHero.CharacterObject);
				return textObject;
			}
		}

		private TextObject SuccessByGivingBackTheGoodsQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=jO9cGnCK}You bought the goods from the bandits and handed over them to the {MERCHANT.LINK}.");
				textObject.SetCharacterProperties("MERCHANT", _counterOfferHero.CharacterObject);
				return textObject;
			}
		}

		private TextObject FailBetrayQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=v1bkAxmK}You accepted {MERCHANT.LINK}'s request. You've handed over the goods.");
				textObject.SetCharacterProperties("MERCHANT", _counterOfferHero.CharacterObject);
				return textObject;
			}
		}

		private TextObject FailBetrayTakeGoodsQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=7q0vbVdN}You betrayed the gang leader and refused the {MERCHANT.LINK}'s request. The goods stayed with you.");
				textObject.SetCharacterProperties("MERCHANT", _counterOfferHero.CharacterObject);
				return textObject;
			}
		}

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=izuhKnXy}Purchase Stolen Goods from {ISSUE_GIVER.NAME}");
				textObject.SetCharacterProperties("ISSUE_GIVER", base.QuestGiver.CharacterObject);
				return textObject;
			}
		}

		private TextObject FailLoseHideoutFightQuestLogText => new TextObject("{=g7atkzxO}You failed to clear the hideout.");

		private TextObject FailTimeOutQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=JwNTcFLA}You failed to make the deal with {QUEST_GIVER.LINK}'s associates in time.");
				textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
				return textObject;
			}
		}

		private TextObject CancelSettlementOwnerChangedQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=0YwZsiT4}Your clan is now owner of the town from which the goods were stolen. As the lord of the settlement you cannot get involved in deals of this kind. Your agreement with {QUEST_GIVER.LINK} is canceled.");
				textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
				return textObject;
			}
		}

		private TextObject BeforeAttackHideoutQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=sVL8Z0lg}You met {QUEST_GIVER.LINK}'s associates near their hideout and demanded they turn over the goods to you, refusing to pay. They refused, and your only option now is to attack the hideout under cover of darkness and take the goods by force.");
				textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
				return textObject;
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsGangLeaderNeedsToOffloadStolenGoodsIssueQuest(object o, List<object> collectedObjects)
		{
			((GangLeaderNeedsToOffloadStolenGoodsIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_stolenTradeGood);
			collectedObjects.Add(_playerStartsQuestLog);
			collectedObjects.Add(_counterOfferHero);
			collectedObjects.Add(_questHideout);
		}

		internal static object AutoGeneratedGetMemberValue_stolenTradeGood(object o)
		{
			return ((GangLeaderNeedsToOffloadStolenGoodsIssueQuest)o)._stolenTradeGood;
		}

		internal static object AutoGeneratedGetMemberValue_stolenTradeGoodAmount(object o)
		{
			return ((GangLeaderNeedsToOffloadStolenGoodsIssueQuest)o)._stolenTradeGoodAmount;
		}

		internal static object AutoGeneratedGetMemberValue_stolenTradeGoodPrice(object o)
		{
			return ((GangLeaderNeedsToOffloadStolenGoodsIssueQuest)o)._stolenTradeGoodPrice;
		}

		internal static object AutoGeneratedGetMemberValue_counterOfferGiven(object o)
		{
			return ((GangLeaderNeedsToOffloadStolenGoodsIssueQuest)o)._counterOfferGiven;
		}

		internal static object AutoGeneratedGetMemberValue_counterOfferGold(object o)
		{
			return ((GangLeaderNeedsToOffloadStolenGoodsIssueQuest)o)._counterOfferGold;
		}

		internal static object AutoGeneratedGetMemberValue_playerStartsQuestLog(object o)
		{
			return ((GangLeaderNeedsToOffloadStolenGoodsIssueQuest)o)._playerStartsQuestLog;
		}

		internal static object AutoGeneratedGetMemberValue_counterOfferHero(object o)
		{
			return ((GangLeaderNeedsToOffloadStolenGoodsIssueQuest)o)._counterOfferHero;
		}

		internal static object AutoGeneratedGetMemberValue_talkedWithBanditLeader(object o)
		{
			return ((GangLeaderNeedsToOffloadStolenGoodsIssueQuest)o)._talkedWithBanditLeader;
		}

		internal static object AutoGeneratedGetMemberValue_isPayingForGoods(object o)
		{
			return ((GangLeaderNeedsToOffloadStolenGoodsIssueQuest)o)._isPayingForGoods;
		}

		internal static object AutoGeneratedGetMemberValue_isFightingForGoods(object o)
		{
			return ((GangLeaderNeedsToOffloadStolenGoodsIssueQuest)o)._isFightingForGoods;
		}

		internal static object AutoGeneratedGetMemberValue_questHideout(object o)
		{
			return ((GangLeaderNeedsToOffloadStolenGoodsIssueQuest)o)._questHideout;
		}

		internal static object AutoGeneratedGetMemberValue_playerHasTheGoods(object o)
		{
			return ((GangLeaderNeedsToOffloadStolenGoodsIssueQuest)o)._playerHasTheGoods;
		}

		public GangLeaderNeedsToOffloadStolenGoodsIssueQuest(string questId, Hero questGiver, CampaignTime duration, ItemObject stolenTradeGood, Settlement questHideout, int stolenTradeGoodPrice, int rewardGold, int stolenGoodAmount, Hero counterOfferHero)
			: base(questId, questGiver, duration, rewardGold)
		{
			_stolenTradeGood = stolenTradeGood;
			_stolenTradeGoodPrice = stolenTradeGoodPrice;
			_stolenTradeGoodAmount = stolenGoodAmount;
			_counterOfferGold = TaleWorlds.Library.MathF.Round((float)(questGiver.CurrentSettlement.Town.GetItemPrice(_stolenTradeGood) * _stolenTradeGoodAmount) * 0.4f);
			_counterOfferHero = counterOfferHero;
			_questHideout = questHideout;
			SetDialogs();
			InitializeQuestOnCreation();
			AddGameMenuOptions();
		}

		protected override void SetDialogs()
		{
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(new TextObject("{=iKYd8PQc}Excellent. I'll have the whereabouts of the goods delivered to you right away...[if:convo_nonchalant][ib:confident]")).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.Consequence(QuestAcceptedConsequences)
				.CloseDialog();
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(new TextObject("{=RmjvSKVq}My colleagues are waiting for your arrival. They won't be happy waiting too long with so many valuables at hand.")).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=D4CsJMLE}Don't worry, I'll get there soon enough."))
				.NpcLine(new TextObject("{=2D4EdZVK}All right, but if this falls through because you took too long, I won't be too happy with that."))
				.CloseDialog()
				.PlayerOption(new TextObject("{=ErKDfLWJ}I have more urgent business to handle. I will get there when I can."))
				.NpcLine(new TextObject("{=qLdchkGU}Yeah... Well, a lot can go wrong if you don't move on these things quickly."))
				.EndPlayerOptions()
				.CloseDialog();
			Campaign.Current.ConversationManager.AddDialogFlow(GetCounterOfferDialogFlow(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetHideoutLeaderDialogFlow(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetAfterHideoutMerchantDialogFlow(), this);
		}

		private void QuestAcceptedConsequences()
		{
			StartQuest();
			_playerStartsQuestLog = AddLog(PlayerStartsQuestLogText, hideInformation: true);
			_questHideout.Hideout.IsSpotted = true;
			_questHideout.IsVisible = true;
			AddTrackedObject(_questHideout);
		}

		protected override void InitializeQuestOnGameLoad()
		{
			SetDialogs();
			AddGameMenuOptions();
		}

		protected override void HourlyTick()
		{
		}

		protected override void OnFinalize()
		{
		}

		private DialogFlow GetCounterOfferDialogFlow()
		{
			TextObject textObject = new TextObject("{=WJPAWHM9}A moment of your time, {?PLAYER.GENDER}ma'am{?}sir{\\?}. I hear you've been talking with a well-known scoundrel named {QUEST_GIVER.LINK}. A while ago, a caravan of mine carrying some {.%}{?(STOLEN_GOODS_SIZE > 1)}{PLURAL(STOLEN_GOOD)}{?}{STOLEN_GOOD}{\\?}{.%} was robbed. Now {?QUEST_GIVER.GENDER}she's{?}he's{\\?} going around offering to sell the exact same merchandise. The guards won't go beyond the walls to get it back, but you can. I will reward you, and you'll earn the gratitude of all law-abiding townsfolk.[if:convo_nonchalant][ib:normal2]");
			textObject.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
			textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
			textObject.SetTextVariable("STOLEN_GOODS_SIZE", _stolenTradeGoodAmount);
			textObject.SetTextVariable("STOLEN_GOOD", _stolenTradeGood.Name);
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject).Condition(() => _counterOfferHero == Hero.OneToOneConversationHero && !_talkedWithBanditLeader && !_playerHasTheGoods && !_counterOfferGiven)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=Fk9nVX8t}Yes... Well, I will see what I can do."))
				.NpcLine(new TextObject("{=WotPuijV}That's the right decision. Thank you.[if:convo_calm_friendly][ib:closed]"))
				.CloseDialog()
				.PlayerOption(new TextObject("{=1fXYQ7XG}My deals are none of your business, merchant."))
				.NpcLine(new TextObject("{=u0pofzPq}Please reconsider. Whether you help me or not, I will make sure other merchants will hear of your decision.[if:convo_very_stern][ib:closed2]"))
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
		}

		protected override void OnTimedOut()
		{
			AddLog(FailTimeOutQuestLogText);
		}

		private void AddGameMenuOptions()
		{
			TextObject optionText = new TextObject("{=wbSXI6iq}Approach to the meeting point");
			AddGameMenuOption("hideout_place", "talk_to_boss", optionText, game_menu_approach_meeting_on_condition, game_menu_approach_meeting_on_consequence, Isleave: false, 2);
		}

		private DialogFlow GetHideoutLeaderDialogFlow()
		{
			TextObject banditBrief = new TextObject("{=!}{BANDIT_BRIEF}");
			TextObject banditFirstInteraction = new TextObject("{=2dWvwc0T}You must be the nice {?PLAYER.GENDER}lady{?}lord{\\?} who {QUEST_GIVER.NAME} has been telling us about. We have been expecting you. The goods are nearby. I will tell my friends to bring them out as soon as you pay up.");
			banditFirstInteraction.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
			banditFirstInteraction.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
			TextObject banditSecondInteraction = new TextObject("{=n17USCaH}Well well... Do you have our money yet?");
			TextObject textObject = new TextObject("{=hXmmofDN}Here are the denars. You can tell your friends to bring out the {.%}{?(STOLEN_GOODS_SIZE > 1)}{PLURAL(STOLEN_GOOD)}{?}{STOLEN_GOOD}{\\?}{.%}.");
			textObject.SetTextVariable("STOLEN_GOODS_SIZE", _stolenTradeGoodAmount);
			textObject.SetTextVariable("STOLEN_GOOD", _stolenTradeGood.Name);
			TextObject text = new TextObject("{=Bvb0AmW7}Unfortunately, I do not have enough denars on me.");
			TextObject npcText = new TextObject("{=7kTipmjp}Then why are you here? We're not the sort who does business on credit. Come back when you have the money.");
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine(banditBrief).Condition(delegate
			{
				banditBrief.SetTextVariable("BANDIT_BRIEF", (!_talkedWithBanditLeader) ? banditFirstInteraction : banditSecondInteraction);
				return BanditLeader == CharacterObject.OneToOneConversationCharacter && Game.Current.GameStateManager.ActiveState is MapState && Settlement.CurrentSettlement == _questHideout;
			})
				.Consequence(delegate
				{
					_talkedWithBanditLeader = true;
				})
				.NpcLine(banditSecondInteraction)
				.Condition(() => BanditLeader == CharacterObject.OneToOneConversationCharacter && _talkedWithBanditLeader && Game.Current.GameStateManager.ActiveState is MapState)
				.BeginPlayerOptions()
				.PlayerOption(textObject)
				.ClickableCondition(delegate(out TextObject explanation)
				{
					bool result = false;
					if (Hero.MainHero.Gold >= _stolenTradeGoodPrice)
					{
						explanation = new TextObject("{=!}{GOLD_AMOUNT}{GOLD_ICON}");
						explanation.SetTextVariable("GOLD_AMOUNT", _stolenTradeGoodPrice);
						result = true;
					}
					else
					{
						explanation = new TextObject("{=YuLLsAUb}You don't have {GOLD_AMOUNT}{GOLD_ICON}.");
						explanation.SetTextVariable("GOLD_AMOUNT", _stolenTradeGoodPrice);
					}
					return result;
				})
				.NpcLine(new TextObject("{=21Y4tWE9}Good, good. All the best to you, friend."))
				.Consequence(delegate
				{
					GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, _stolenTradeGoodPrice);
					_isPayingForGoods = true;
					_playerHasTheGoods = true;
				})
				.CloseDialog()
				.PlayerOption(new TextObject("{=H7FpMR0O}I'm not buying them from you, thief. Bring them out now."))
				.NpcLine(new TextObject("{=M8j6J8aN}Really? Very well, I'll send word to my lads that you intended to take our loot by force. They'll get things ready for you, have no fear. Just maybe not in the way you're hoping..."))
				.Consequence(delegate
				{
					AddLog(BeforeAttackHideoutQuestLogText);
					_isFightingForGoods = true;
				})
				.CloseDialog()
				.PlayerOption(text)
				.Condition(() => Hero.MainHero.Gold < _stolenTradeGoodPrice)
				.NpcLine(npcText)
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
		}

		private DialogFlow GetAfterHideoutMerchantDialogFlow()
		{
			TextObject textObject = new TextObject("{=8nq0TbMZ}{?PLAYER.GENDER}Ma'am{?}Sir{\\?}! I saw your party from a distance. I hope you don't mind me catching up to you. I am glad to see you safe - and, from the looks of it, in possession of some fine new goods. Please tell me, would they be mine?");
			textObject.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
			TextObject textObject2 = new TextObject("{=Utq4svjd}Splendid. I will be sure that everyone in {MERCHANT_TOWN} hears of your honesty.");
			textObject2.SetTextVariable("MERCHANT_TOWN", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
			TextObject textObject3 = new TextObject("{=dtghhz46}Well... You have swords. You can do what you like. Just don't expect anyone in {MERCHANT_TOWN} to think well of you. Good day, {?PLAYER.GENDER}ma'am{?}sir{\\?}.");
			textObject3.SetTextVariable("MERCHANT_TOWN", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject).Condition(() => Settlement.CurrentSettlement == _questHideout && _counterOfferHero == Hero.OneToOneConversationHero)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=peUxzLsY}\"Your\" goods? These are my goods now."))
				.NpcLine(textObject3)
				.Consequence(delegate
				{
					if (_isPayingForGoods)
					{
						SucceedQuestByPayingAndKeepingTheGoods();
					}
					else
					{
						FailQuestByKeepingTheGoods();
					}
				})
				.CloseDialog()
				.PlayerOption(new TextObject("{=1lOSJ29M}Yes, they are here. Tell your men to come pick them up."))
				.NpcLine(textObject2)
				.Consequence(delegate
				{
					if (_isPayingForGoods)
					{
						SucceedQuestByPayingAndGivingTheGoodsBack();
					}
					else
					{
						FailQuestByGivingBackTheGoods();
					}
				})
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
		}

		private void SucceedQuestByPayingAndKeepingTheGoods()
		{
			AddLog(SuccessQuestLogText);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Calculating, 100)
			});
			MobileParty.MainParty.ItemRoster.AddToCounts(_stolenTradeGood, _stolenTradeGoodAmount);
			base.QuestGiver.AddPower(5f);
			_counterOfferHero.AddPower(-5f);
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, 10);
			foreach (Hero item in base.QuestGiver.CurrentSettlement.Notables.WhereQ((Hero notable) => notable.IsMerchant))
			{
				ChangeRelationAction.ApplyPlayerRelation(item, -3);
			}
			CompleteQuestWithSuccess();
		}

		private void SucceedQuestByPayingAndGivingTheGoodsBack()
		{
			AddLog(SuccessByGivingBackTheGoodsQuestLogText);
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, _counterOfferGold);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Calculating, 150)
			});
			base.QuestGiver.AddPower(5f);
			_counterOfferHero.AddPower(5f);
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, 10);
			foreach (Hero item in base.QuestGiver.CurrentSettlement.Notables.WhereQ((Hero notable) => notable != base.QuestGiver))
			{
				ChangeRelationAction.ApplyPlayerRelation(item, 3);
			}
			CompleteQuestWithSuccess();
		}

		private void FailQuestByKeepingTheGoods()
		{
			base.QuestGiver.AddPower(-5f);
			_counterOfferHero.AddPower(-5f);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Calculating, 100)
			});
			MobileParty.MainParty.ItemRoster.AddToCounts(_stolenTradeGood, _stolenTradeGoodAmount);
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -5);
			foreach (Hero item in base.QuestGiver.CurrentSettlement.Notables.WhereQ((Hero notable) => notable.IsMerchant))
			{
				ChangeRelationAction.ApplyPlayerRelation(item, -3);
			}
			CompleteQuestWithBetrayal(FailBetrayTakeGoodsQuestLogText);
		}

		private void FailQuestByGivingBackTheGoods()
		{
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, _counterOfferGold);
			base.QuestGiver.AddPower(-5f);
			_counterOfferHero.AddPower(5f);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 100)
			});
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -5);
			foreach (Hero item in base.QuestGiver.CurrentSettlement.Notables.WhereQ((Hero notable) => notable != base.QuestGiver))
			{
				ChangeRelationAction.ApplyPlayerRelation(item, 3);
			}
			CompleteQuestWithBetrayal(FailBetrayQuestLogText);
		}

		private void FailQuestByLosingHideoutBattle()
		{
			base.QuestGiver.AddPower(-5f);
			_counterOfferHero.AddPower(-5f);
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -5);
			CompleteQuestWithFail(FailLoseHideoutFightQuestLogText);
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionStarted);
			CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, GameMenuOpened);
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
			CampaignEvents.OnHideoutBattleCompletedEvent.AddNonSerializedListener(this, OnHideoutBattleCompleted);
			CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
			CampaignEvents.IsSettlementBusyEvent.AddNonSerializedListener(this, IsSettlementBusy);
		}

		private void IsSettlementBusy(Settlement settlement, object asker, ref int priority)
		{
			if (asker != this && settlement == _questHideout)
			{
				priority = Math.Max(priority, 200);
			}
		}

		private void OnSessionStarted(CampaignGameStarter campaignGameStarter)
		{
			if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.9") && _questHideout.IsSettlementBusy(this))
			{
				CompleteQuestWithCancel();
			}
		}

		private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			if (mapEvent.MapEventSettlement == _questHideout && attackerParty == PartyBase.MainParty)
			{
				_isFightingForGoods = true;
			}
		}

		private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			if (settlement == base.QuestGiver.CurrentSettlement && newOwner == Hero.MainHero)
			{
				CompleteQuestWithCancel(CancelSettlementOwnerChangedQuestLogText);
			}
		}

		private void OnHideoutBattleCompleted(BattleSideEnum winnerSide, HideoutEventComponent hideoutEventComponent, HideoutEventComponent.HideoutBattleEndState battleEndState)
		{
			if (hideoutEventComponent.MapEvent.PlayerSide != winnerSide)
			{
				FailQuestByLosingHideoutBattle();
			}
			else if (hideoutEventComponent.MapEvent.MapEventSettlement == _questHideout)
			{
				_playerHasTheGoods = true;
				TextObject textObject = new TextObject("{=9bFAsGmp}You have cleared the hideout. Your men found the {.%}{?(STOLEN_GOODS_SIZE > 1)}{PLURAL(STOLEN_GOOD)}{?}{STOLEN_GOOD}{\\?}{.%} stashed nearby.");
				textObject.SetTextVariable("STOLEN_GOODS_SIZE", _stolenTradeGoodAmount);
				textObject.SetTextVariable("STOLEN_GOOD", _stolenTradeGood.Name);
				MBInformationManager.AddQuickInformation(textObject);
			}
		}

		private void GameMenuOpened(MenuCallbackArgs args)
		{
			if (Settlement.CurrentSettlement == _questHideout && !_isFightingForGoods)
			{
				GameTexts.SetVariable("HIDEOUT_DESCRIPTION", "{=b14v853a}As you approach the gang's camp, you can see a single man out in the open waiting for you, pacing and kicking stones around.");
			}
		}

		private void OnSettlementLeft(MobileParty party, Settlement settlement)
		{
			if (party != MobileParty.MainParty)
			{
				return;
			}
			if (settlement == _questHideout)
			{
				if (_playerHasTheGoods)
				{
					CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter), new ConversationCharacterData(_counterOfferHero.CharacterObject));
				}
			}
			else if (settlement == _counterOfferHero.CurrentSettlement && !_counterOfferGiven)
			{
				CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter), new ConversationCharacterData(_counterOfferHero.CharacterObject));
				_counterOfferGiven = true;
			}
		}

		private void game_menu_approach_meeting_on_consequence(MenuCallbackArgs args)
		{
			CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter), new ConversationCharacterData(BanditLeader));
		}

		private bool game_menu_approach_meeting_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			args.OptionQuestData = GameMenuOption.IssueQuestFlags.ActiveIssue;
			if (Settlement.CurrentSettlement == _questHideout)
			{
				if (!_isFightingForGoods)
				{
					return !_isPayingForGoods;
				}
				return false;
			}
			return false;
		}

		public override void OnHeroCanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
		{
			if (hero == _counterOfferHero)
			{
				result = false;
			}
		}
	}

	public class GangLeaderNeedsToOffloadStolenGoodsIssueTypeDefiner : SaveableTypeDefiner
	{
		public GangLeaderNeedsToOffloadStolenGoodsIssueTypeDefiner()
			: base(460000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(GangLeaderNeedsToOffloadStolenGoodsIssue), 1);
			AddClassDefinition(typeof(GangLeaderNeedsToOffloadStolenGoodsIssueQuest), 2);
		}
	}

	private const int IssueDuration = 15;

	private const IssueBase.IssueFrequency GangLeaderNeedsToOffloadStolenGoodsIssueFrequency = IssueBase.IssueFrequency.Common;

	private static float MaxHideoutDistance => Campaign.Current.EstimatedAverageBanditPartySpeed * (float)CampaignTime.HoursInDay;

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private bool ConditionsHold(Hero issueGiver, out Settlement selectedHideout)
	{
		selectedHideout = null;
		bool num = issueGiver.IsGangLeader && issueGiver.CurrentSettlement.Town.Security < 70f && issueGiver.CurrentSettlement.Notables.Any((Hero x) => x.CharacterObject.IsHero && x.CharacterObject.HeroObject != issueGiver && x.CharacterObject.HeroObject.IsMerchant);
		selectedHideout = ((issueGiver.CurrentSettlement != null && issueGiver.CurrentSettlement.IsTown) ? FindSuitableHideout(issueGiver.CurrentSettlement) : null);
		if (num)
		{
			return selectedHideout != null;
		}
		return false;
	}

	public void OnCheckForIssue(Hero hero)
	{
		Campaign.Current.IssueManager.AddPotentialIssueData(hero, ConditionsHold(hero, out var selectedHideout) ? new PotentialIssueData(OnSelected, typeof(GangLeaderNeedsToOffloadStolenGoodsIssue), IssueBase.IssueFrequency.Common, selectedHideout) : new PotentialIssueData(typeof(GangLeaderNeedsToOffloadStolenGoodsIssue), IssueBase.IssueFrequency.Common));
	}

	private static Settlement FindSuitableHideout(Settlement settlement)
	{
		Settlement result = null;
		float num = float.MaxValue;
		foreach (Hideout item in Campaign.Current.AllHideouts.Where((Hideout t) => t.IsInfested))
		{
			if (!item.Settlement.IsSettlementBusy(null))
			{
				float num2 = DistanceHelper.FindClosestDistanceFromSettlementToSettlement(settlement, item.Settlement, MobileParty.NavigationType.Default);
				if (num2 <= MaxHideoutDistance && num2 < num)
				{
					num = num2;
					result = item.Settlement;
				}
			}
		}
		return result;
	}

	private IssueBase OnSelected(in PotentialIssueData pid, Hero issueOwner)
	{
		return new GangLeaderNeedsToOffloadStolenGoodsIssue(issueOwner, pid.RelatedObject as Settlement);
	}
}
