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

public class VillageNeedsToolsIssueBehavior : CampaignBehaviorBase
{
	public class VillageNeedsToolsIssue : IssueBase
	{
		private const int TimeLimit = 30;

		private const int TroopTierForAlternativeSolution = 2;

		public const int PowerRewardForQuestGiverOnSuccess = 10;

		private const int RelationWithIssueOwnerRewardOnSuccess = 5;

		private const int VillageHeartChangeOnSuccess = 50;

		private const int RequiredSkillValueForAlternativeSolution = 120;

		[SaveableField(10)]
		private readonly ItemObject _requestedItem;

		[SaveableField(20)]
		private readonly ItemObject _exchangeItem;

		[SaveableField(30)]
		private readonly int _numberOfExchangeItem;

		[SaveableField(40)]
		private readonly int _numberOfRequestedItem;

		[SaveableField(50)]
		private readonly int _payment;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.Duration;

		protected override int RewardGold => 500 + _numberOfRequestedItem * (int)((float)(base.IssueSettlement.SettlementComponent.GetItemPrice(_requestedItem) + _requestedItem.Value) / 2f);

		private int CostOfToolsForAlternativeSolution => (int)((float)(_requestedItem.Value * _numberOfRequestedItem) * 0.7f);

		protected override int CompanionSkillRewardXP => 500 + (int)(700f * base.IssueDifficultyMultiplier);

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=gnuojd9u}{VILLAGE} Needs Tools");
				textObject.SetTextVariable("VILLAGE", base.IssueOwner.CurrentSettlement.Name);
				return textObject;
			}
		}

		public override TextObject Description => new TextObject("{=Td2RGRBn}Headman in the village requested tools to increase production.");

		public override TextObject IssueBriefByIssueGiver => new TextObject("{=BGJpwxvm}We do have some problems. [ib:demure][if:convo_dismayed] A sickness passed through here last month. Praise the Heavens, only a few people died, but many were weakened and we couldn't get much work done. Now we need to hire some laborers from nearby settlements to make up the shortfall, but we don't have the tools for them. We're in a bit of a rush - do you think you could find tools for us?");

		public override TextObject IssueAcceptByPlayer => new TextObject("{=3EL0wY1h}Tell me about the details.");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject;
				if (_exchangeItem == null)
				{
					textObject = new TextObject("{=daXZlOBi}We need {REQUESTED_ITEM_COUNT} {.%}{?(REQUESTED_ITEM_COUNT > 1)}{PLURAL(REQUESTED_ITEM)}{?}{REQUESTED_ITEM}{\\?}{.%} in {NUMBER_OF_DAYS} days. We can offer {PAYMENT}{GOLD_ICON} for the tools and your services. What do you say?");
					textObject.SetTextVariable("PAYMENT", _payment);
				}
				else
				{
					textObject = new TextObject("{=uwmfgcM3}We need {REQUESTED_ITEM_COUNT} {.%}{?(REQUESTED_ITEM_COUNT > 1)}{PLURAL(REQUESTED_ITEM)}{?}{REQUESTED_ITEM}{\\?}{.%} in {NUMBER_OF_DAYS} days. The village is short on denars so we can make the payment in kind - with {?NUMBER_OF_EXCHANGE_ITEM > 1}{NUMBER_OF_EXCHANGE_ITEM} {._}{PLURAL(EXCHANGE_ITEM)}{?}one {._}{EXCHANGE_ITEM}{\\?}. What do you say?");
					textObject.SetTextVariable("EXCHANGE_ITEM", _exchangeItem.Name);
					textObject.SetTextVariable("NUMBER_OF_EXCHANGE_ITEM", _numberOfExchangeItem);
				}
				textObject.SetTextVariable("REQUESTED_ITEM", _requestedItem.Name);
				textObject.SetTextVariable("REQUESTED_ITEM_COUNT", _numberOfRequestedItem);
				textObject.SetTextVariable("NUMBER_OF_DAYS", 30);
				return textObject;
			}
		}

		public override TextObject IssuePlayerResponseAfterAlternativeExplanation => new TextObject("{=Tp4X51vX}Maybe my men can handle this for you.");

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=8llksa4h}If so, you'll need a man with good understanding of trade. Also you will need at least {NUMBER_OF_TROOPS} fighting men to protect the goods while taking them to market and back. Your companion will also probably need around {GOLD_COST}{GOLD_ICON} in order to buy the tools.[if:convo_confused_normal]");
				textObject.SetTextVariable("NUMBER_OF_TROOPS", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("GOLD_COST", CostOfToolsForAlternativeSolution);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		public override TextObject IssueQuestSolutionAcceptByPlayer => new TextObject("{=ggGmjgIS}I think I can handle this myself.");

		public override TextObject IssueAlternativeSolutionAcceptByPlayer => new TextObject("{=5aDpzB1F}My men will deliver your goods on time don't worry.");

		public override TextObject IssueDiscussAlternativeSolution => new TextObject("{=bkZOcbGu}Your men are still getting us the tools. I ask for your patience. We very much appreciate this.");

		public override bool IsThereAlternativeSolution => true;

		public override bool IsThereLordSolution => false;

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 4 + TaleWorlds.Library.MathF.Ceiling(5f * base.IssueDifficultyMultiplier);

		public override int AlternativeSolutionBaseNeededMenCount => 2 + TaleWorlds.Library.MathF.Ceiling(4f * base.IssueDifficultyMultiplier);

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver => new TextObject("{=JxUrkzd1}Thank you. I hope your men can get us the tools on time. Good luck.[ib:demure][if:convo_astonished]");

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=qycE7IO0}{QUEST_GIVER.LINK} told you that {?QUEST_GIVER.GENDER}she{?}he{\\?} needs {._}{ITEM} for {?QUEST_GIVER.GENDER}her{?}his{\\?} village. {?QUEST_GIVER.GENDER}She{?}He{\\?} offers you {REWARD_GOLD}{GOLD_ICON} for the delivery of the tools. You asked your companion {COMPANION.LINK} and {NEEDED_MEN_COUNT} of your men to deliver {NUMBER_OF_ITEM} {?NUMBER_OF_ITEM>1}units{?}unit{\\?} of {._}{ITEM} to {QUEST_GIVER.LINK}. They will rejoin your party in {ALTERNATIVE_SOLUTION_DURATION} days.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				textObject.SetTextVariable("ITEM", _requestedItem.Name);
				textObject.SetTextVariable("NUMBER_OF_ITEM", _numberOfRequestedItem);
				textObject.SetTextVariable("REWARD_GOLD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				textObject.SetTextVariable("NEEDED_MEN_COUNT", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("ALTERNATIVE_SOLUTION_DURATION", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=W0w0Eunx}Your companion {COMPANION.LINK} has delivered {ISSUE_GIVER.LINK}'s goods as you promised.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				return textObject;
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsVillageNeedsToolsIssue(object o, List<object> collectedObjects)
		{
			((VillageNeedsToolsIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_requestedItem);
			collectedObjects.Add(_exchangeItem);
		}

		internal static object AutoGeneratedGetMemberValue_requestedItem(object o)
		{
			return ((VillageNeedsToolsIssue)o)._requestedItem;
		}

		internal static object AutoGeneratedGetMemberValue_exchangeItem(object o)
		{
			return ((VillageNeedsToolsIssue)o)._exchangeItem;
		}

		internal static object AutoGeneratedGetMemberValue_numberOfExchangeItem(object o)
		{
			return ((VillageNeedsToolsIssue)o)._numberOfExchangeItem;
		}

		internal static object AutoGeneratedGetMemberValue_numberOfRequestedItem(object o)
		{
			return ((VillageNeedsToolsIssue)o)._numberOfRequestedItem;
		}

		internal static object AutoGeneratedGetMemberValue_payment(object o)
		{
			return ((VillageNeedsToolsIssue)o)._payment;
		}

		public VillageNeedsToolsIssue(Hero issueOwner, ItemObject requestedItem)
			: base(issueOwner, CampaignTime.DaysFromNow(30f))
		{
			_requestedItem = requestedItem;
			int itemPrice = issueOwner.CurrentSettlement.SettlementComponent.GetItemPrice(_requestedItem);
			_numberOfRequestedItem = TaleWorlds.Library.MathF.Round((float)(int)(2500f / (float)_requestedItem.Value) * base.IssueDifficultyMultiplier);
			int num = 500 + _numberOfRequestedItem * (int)((float)(itemPrice + _requestedItem.Value) / 2f);
			if (issueOwner.CurrentSettlement.Village.Hearth < 300f)
			{
				_exchangeItem = issueOwner.CurrentSettlement.Village.VillageType.PrimaryProduction;
				_numberOfExchangeItem = TaleWorlds.Library.MathF.Ceiling((float)num * 0.7f / (float)_exchangeItem.Value);
			}
			else
			{
				_payment = num;
				_numberOfExchangeItem = 0;
				_exchangeItem = null;
			}
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.VillageHearth)
			{
				return -0.2f;
			}
			if (issueEffect == DefaultIssueEffects.IssueOwnerPower)
			{
				return -0.1f;
			}
			return 0f;
		}

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			return ((hero.GetSkillValue(DefaultSkills.Engineering) >= hero.GetSkillValue(DefaultSkills.Crafting)) ? DefaultSkills.Engineering : DefaultSkills.Crafting, 120);
		}

		protected override void OnGameLoad()
		{
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new VillageNeedsToolsIssueQuest(questId, base.IssueOwner, _requestedItem, _numberOfRequestedItem, _exchangeItem, _numberOfExchangeItem, _payment, CampaignTime.DaysFromNow(30f));
		}

		public override IssueFrequency GetFrequency()
		{
			return IssueFrequency.VeryCommon;
		}

		protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out PreconditionFlags flag, out Hero relationHero, out SkillObject skill, out int requiredGold)
		{
			bool flag2 = issueGiver.GetRelationWithPlayer() >= -10f && !issueGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction);
			flag = ((!flag2) ? ((!issueGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction)) ? PreconditionFlags.Relation : PreconditionFlags.AtWar) : PreconditionFlags.None);
			relationHero = issueGiver;
			skill = null;
			requiredGold = 0;
			return flag2;
		}

		public override bool IssueStayAliveConditions()
		{
			if (base.IssueOwner.CurrentSettlement.ItemRoster.GetItemNumber(_requestedItem) == 0 && !base.IssueOwner.CurrentSettlement.IsRaided)
			{
				return !base.IssueOwner.CurrentSettlement.IsUnderRaid;
			}
			return false;
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}

		public override void AlternativeSolutionStartConsequence()
		{
			GiveGoldAction.ApplyForCharacterToParty(Hero.MainHero, null, CostOfToolsForAlternativeSolution);
		}

		public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
		{
			return character.Tier >= 2;
		}

		public override bool AlternativeSolutionCondition(out TextObject explanation)
		{
			if (QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2))
			{
				return QuestHelper.CheckGoldForAlternativeSolution(CostOfToolsForAlternativeSolution, out explanation);
			}
			return false;
		}

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			VillageNeedsToolsIssueQuest.GiveTradeOrExchangeRewardToMainParty(base.IssueOwner, _payment, _exchangeItem, _numberOfExchangeItem);
			ChangeRelationAction.ApplyPlayerRelation(base.IssueOwner, 5);
			base.IssueOwner.AddPower(10f);
			base.IssueOwner.CurrentSettlement.Village.Hearth += 50f;
		}
	}

	public class VillageNeedsToolsIssueQuest : QuestBase
	{
		private const int VillageHeartChangeOnExchangeSuccess = 40;

		private const int VillageHeartChangeOnTradeSuccess = 20;

		private const int TraitChangeOnSuccess = 30;

		private const int RelationChangeWithQuestGiverOnExchangeSuccess = 7;

		private const int RelationChangeWithNotablesOnExchangeSuccess = 2;

		private const int RelationChangeWithQuestGiverOnTradeSuccess = 5;

		private const int RelationChangeWithQuestGiverOnFail = -5;

		private const int QuestGiverPowerChangeOnFail = -10;

		private const int VillageHeartChangeOnFail = -30;

		[SaveableField(10)]
		private readonly ItemObject _requestedTradeGood;

		[SaveableField(20)]
		private readonly int _numberOfRequestedGood;

		[SaveableField(30)]
		private readonly ItemObject _exchangeItem;

		[SaveableField(40)]
		private readonly int _numberOfExchangeItem;

		[SaveableField(50)]
		private JournalLog _numberOfToolsLog;

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=gnuojd9u}{VILLAGE} Needs Tools");
				textObject.SetTextVariable("VILLAGE", base.QuestGiver.CurrentSettlement.Name);
				return textObject;
			}
		}

		public override bool IsRemainingTimeHidden => false;

		private TextObject QuestStartedLog
		{
			get
			{
				TextObject textObject = new TextObject("{=BOp61V4A}{QUEST_GIVER.LINK} told you that {?QUEST_GIVER.GENDER}she{?}he{\\?} needs {._}{REQUIRED_ITEM} for {?QUEST_GIVER.GENDER}her{?}his{\\?} village. {?QUEST_GIVER.GENDER}She{?}He{\\?} asked you to bring {ITEM_COUNT} {.%}{?(ITEM_COUNT > 1)}{PLURAL(REQUIRED_ITEM)}{?}{REQUIRED_ITEM}{\\?}{.%}  to {?QUEST_GIVER.GENDER}her{?}him{\\?}. {PAYMENT_DESCRIPTION}");
				textObject.SetTextVariable("REQUIRED_ITEM", _requestedTradeGood.Name);
				textObject.SetTextVariable("ITEM_COUNT", _numberOfRequestedGood);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				TextObject textObject2;
				if (_exchangeItem == null)
				{
					textObject2 = new TextObject("{=ZOTBiLiS}{?QUEST_GIVER.GENDER}She{?}He{\\?} will pay you {PAYMENT}{GOLD_ICON} when the task is done.");
					textObject2.SetTextVariable("PAYMENT", RewardGold);
					textObject2.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				}
				else
				{
					textObject2 = new TextObject("{=eQzskygV}{?QUEST_GIVER.GENDER}She{?}He{\\?} will make payment as {EXCHANGE_ITEM_COUNT} {?EXCHANGE_ITEM_COUNT>1}units{?}unit{\\?} of {._}{EXCHANGE_ITEM} when the task is done.");
					textObject2.SetTextVariable("EXCHANGE_ITEM", _exchangeItem.Name);
					textObject2.SetTextVariable("EXCHANGE_ITEM_COUNT", _numberOfExchangeItem);
				}
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject2);
				textObject.SetTextVariable("PAYMENT_DESCRIPTION", textObject2);
				return textObject;
			}
		}

		private TextObject WarDeclaredQuestCancelLog
		{
			get
			{
				TextObject textObject = new TextObject("{=PakhagOy}Your clan is now at war with {QUEST_GIVER.LINK}'s lord. Your agreement with {QUEST_GIVER.LINK} was canceled.");
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

		private TextObject VillageRaidedQuestCancelLog
		{
			get
			{
				TextObject textObject = new TextObject("{=9zJNjWes}{SETTLEMENT} was raided. Your agreement with {QUEST_GIVER.LINK} was canceled.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject QuestTimeOutFailLog
		{
			get
			{
				TextObject textObject = new TextObject("{=jXTshvhV}You couldn't fully bring {ITEM} to {?QUEST_GIVER.GENDER}her{?}him{\\?} on time.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("ITEM", _requestedTradeGood.Name);
				return textObject;
			}
		}

		private TextObject QuestSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=ytqqEyFw}You brought {NUMBER_OF_ITEM} {?NUMBER_OF_ITEM>1}units{?}unit{\\?} of {ITEM} to {?QUEST_GIVER.GENDER}her{?}him{\\?} as promised.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("ITEM", _requestedTradeGood.Name);
				textObject.SetTextVariable("NUMBER_OF_ITEM", _numberOfRequestedGood);
				return textObject;
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsVillageNeedsToolsIssueQuest(object o, List<object> collectedObjects)
		{
			((VillageNeedsToolsIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_requestedTradeGood);
			collectedObjects.Add(_exchangeItem);
			collectedObjects.Add(_numberOfToolsLog);
		}

		internal static object AutoGeneratedGetMemberValue_requestedTradeGood(object o)
		{
			return ((VillageNeedsToolsIssueQuest)o)._requestedTradeGood;
		}

		internal static object AutoGeneratedGetMemberValue_numberOfRequestedGood(object o)
		{
			return ((VillageNeedsToolsIssueQuest)o)._numberOfRequestedGood;
		}

		internal static object AutoGeneratedGetMemberValue_exchangeItem(object o)
		{
			return ((VillageNeedsToolsIssueQuest)o)._exchangeItem;
		}

		internal static object AutoGeneratedGetMemberValue_numberOfExchangeItem(object o)
		{
			return ((VillageNeedsToolsIssueQuest)o)._numberOfExchangeItem;
		}

		internal static object AutoGeneratedGetMemberValue_numberOfToolsLog(object o)
		{
			return ((VillageNeedsToolsIssueQuest)o)._numberOfToolsLog;
		}

		public VillageNeedsToolsIssueQuest(string questId, Hero questGiver, ItemObject requestedItem, int numberOfRequestedGood, ItemObject exchangeItem, int numberOfExchangeItem, int payment, CampaignTime duration)
			: base(questId, questGiver, duration, payment)
		{
			_requestedTradeGood = requestedItem;
			_numberOfRequestedGood = numberOfRequestedGood;
			_exchangeItem = exchangeItem;
			_numberOfExchangeItem = numberOfExchangeItem;
			SetDialogs();
			InitializeQuestOnCreation();
		}

		protected override void SetDialogs()
		{
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(new TextObject("{=ELxhTMuy}Excellent. But please hurry - we need to put the men we hired to work right away. Good luck.")).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.Consequence(delegate
				{
					StartQuest();
					TextObject textObject = new TextObject("{=M8PXWpyV}Collected {ITEM}");
					textObject.SetTextVariable("ITEM", _requestedTradeGood.Name);
					_numberOfToolsLog = AddDiscreteLog(QuestStartedLog, textObject, 0, _numberOfRequestedGood);
					UpdateToolsAmount();
				})
				.CloseDialog();
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(new TextObject("{=dJVjbgyu}Any news about our tools {?PLAYER.GENDER}madame{?}sir{\\?}?")).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=yvXNvh2B}Yes, I brought your tools."))
				.Condition(PlayerHasTools)
				.NpcLine(new TextObject("{=yF3cBat5}Thank you {?PLAYER.GENDER}madame{?}sir{\\?}. Here is what we promised."))
				.Consequence(FinishQuestSuccess1)
				.CloseDialog()
				.PlayerOption(new TextObject("{=ULWYVuVw}I'm still looking for your goods."))
				.NpcLine(new TextObject("{=tkaEZNpB}Of course. But… please hurry, {?PLAYER.GENDER}madame{?}sir{\\?}. We can't afford to pay the hired men to sit around. We don't have much money to spare, {?PLAYER.GENDER}madame{?}sir{\\?}."))
				.CloseDialog()
				.EndPlayerOptions();
		}

		private bool PlayerHasTools()
		{
			return GetCurrentToolsAmountInPlayerRoster() >= _numberOfRequestedGood;
		}

		protected override void HourlyTick()
		{
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
			CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, RaidCompleted);
			CampaignEvents.PlayerInventoryExchangeEvent.AddNonSerializedListener(this, OnInventoryExchange);
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
		}

		private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
			{
				QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
			}
		}

		private void RaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEvent)
		{
			if (raidEvent.MapEventSettlement == base.QuestGiver.CurrentSettlement)
			{
				CompleteQuestWithCancel(VillageRaidedQuestCancelLog);
			}
		}

		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
		{
			QuestHelper.CheckWarDeclarationAndFailOrCancelTheQuest(this, faction1, faction2, detail, PlayerDeclaredWarQuestLogText, WarDeclaredQuestCancelLog);
		}

		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (base.QuestGiver.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				CompleteQuestWithCancel(WarDeclaredQuestCancelLog);
			}
		}

		protected override void InitializeQuestOnGameLoad()
		{
			SetDialogs();
		}

		private void OnInventoryExchange(List<(ItemRosterElement, int)> purchasedItems, List<(ItemRosterElement, int)> soldItems, bool isTrading)
		{
			UpdateToolsAmount();
		}

		private int GetCurrentToolsAmountInPlayerRoster()
		{
			return MobileParty.MainParty.ItemRoster.GetItemNumber(_requestedTradeGood);
		}

		private void UpdateToolsAmount()
		{
			_numberOfToolsLog.UpdateCurrentProgress((int)TaleWorlds.Library.MathF.Clamp(GetCurrentToolsAmountInPlayerRoster(), 0f, _numberOfRequestedGood));
		}

		protected override void OnTimedOut()
		{
			AddLog(QuestTimeOutFailLog);
			base.QuestGiver.AddPower(-10f);
			base.QuestGiver.CurrentSettlement.Village.Hearth += -30f;
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -5);
		}

		public override void OnFailed()
		{
			base.QuestGiver.AddPower(-10f);
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -5);
		}

		private void FinishQuestSuccess1()
		{
			AddLog(QuestSuccessLog);
			base.QuestGiver.AddPower(10f);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 30)
			});
			PartyBase.MainParty.ItemRoster.AddToCounts(_requestedTradeGood, -_numberOfRequestedGood);
			GiveTradeOrExchangeRewardToMainParty(base.QuestGiver, RewardGold, _exchangeItem, _numberOfExchangeItem);
			int num;
			if (_exchangeItem != null)
			{
				ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, 7);
				foreach (Hero notable in base.QuestGiver.CurrentSettlement.Notables)
				{
					if (notable != base.QuestGiver)
					{
						ChangeRelationAction.ApplyPlayerRelation(notable, 2);
					}
				}
				num = 40;
			}
			else
			{
				ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, 5);
				num = 20;
			}
			base.QuestGiver.CurrentSettlement.Village.Hearth += num;
			CompleteQuestWithSuccess();
		}

		public static void GiveTradeOrExchangeRewardToMainParty(Hero questGiver, int gold, ItemObject exchangeItem, int exchangeItemCount)
		{
			if (exchangeItem != null)
			{
				questGiver.CurrentSettlement.ItemRoster.AddToCounts(exchangeItem, exchangeItemCount);
				ItemRosterElement itemRosterElement = new ItemRosterElement(exchangeItem, exchangeItemCount);
				GiveItemAction.ApplyForParties(questGiver.CurrentSettlement.Party, PartyBase.MainParty, in itemRosterElement);
			}
			else
			{
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, gold);
			}
		}
	}

	public class VillageNeedsToolsIssueTypeDefiner : SaveableTypeDefiner
	{
		public VillageNeedsToolsIssueTypeDefiner()
			: base(600000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(VillageNeedsToolsIssue), 1);
			AddClassDefinition(typeof(VillageNeedsToolsIssueQuest), 2);
		}
	}

	private const IssueBase.IssueFrequency VillageNeedsToolsIssueFrequency = IssueBase.IssueFrequency.VeryCommon;

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
	}

	private void OnCheckForIssue(Hero hero)
	{
		ItemObject tools = DefaultItems.Tools;
		if (ConditionsHold(hero, tools))
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnStartIssue, typeof(VillageNeedsToolsIssue), IssueBase.IssueFrequency.VeryCommon, tools));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(VillageNeedsToolsIssue), IssueBase.IssueFrequency.VeryCommon));
		}
	}

	private bool ConditionsHold(Hero issueGiver, ItemObject item)
	{
		Settlement currentSettlement = issueGiver.CurrentSettlement;
		if (issueGiver.IsHeadman && currentSettlement != null && currentSettlement.IsVillage && currentSettlement.Village.GetProsperityLevel() < SettlementComponent.ProsperityLevel.Mid && currentSettlement.Village.VillageType.Productions.Count > 0 && currentSettlement.Village.VillageType.Productions.All(((ItemObject, float) x) => !x.Item1.IsAnimal))
		{
			return currentSettlement.ItemRoster.GetItemNumber(item) == 0;
		}
		return false;
	}

	private IssueBase OnStartIssue(in PotentialIssueData pid, Hero issueOwner)
	{
		return new VillageNeedsToolsIssue(issueOwner, (ItemObject)pid.RelatedObject);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
