using System;
using System.Collections.Generic;
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
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues;

public class HeadmanNeedsGrainIssueBehavior : CampaignBehaviorBase
{
	public class HeadmanNeedsGrainIssue : IssueBase
	{
		private const int IssueDuration = 30;

		private const int AlternativeSolutionSuccessRenownBonus = 1;

		private const int AlternativeSolutionSuccessGenerosityBonus = 30;

		private const int AlternativeSolutionFailPowerPenalty = -5;

		private const int QuestTimeLimit = 18;

		private const int AlternativeSolutionSuccessPowerBonus = 10;

		private const int AlternativeSolutionSuccessRelationBonusWithQuestGiver = 5;

		private const int AlternativeSolutionSuccessRelationBonusWithOtherNotables = 1;

		private const int AlternativeSolutionFailRelationPenaltyWithNotables = -3;

		private const int AlternativeSolutionSuccessProsperityBonus = 50;

		private const int AlternativeSolutionFailProsperityPenalty = -10;

		private const int CompanionTradeSkillLimit = 120;

		[CachedData]
		private Settlement _nearbySuitableSettlementCache;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.Duration;

		private int NeededGrainAmount => (int)(12f + 180f * base.IssueDifficultyMultiplier);

		private int AlternativeSolutionNeededGold => NeededGrainAmount * AverageGrainPriceInCalradia;

		public override int AlternativeSolutionBaseNeededMenCount => 3 + TaleWorlds.Library.MathF.Ceiling(6f * base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 2 + TaleWorlds.Library.MathF.Ceiling(6f * base.IssueDifficultyMultiplier);

		protected override int RewardGold => 0;

		[CachedData]
		private Settlement NearbySuitableSettlement
		{
			get
			{
				if (_nearbySuitableSettlementCache == null)
				{
					Settlement nearbySuitableSettlementCache = SettlementHelper.FindNearestSettlementToSettlement(base.IssueOwner.CurrentSettlement, MobileParty.NavigationType.Default, delegate(Settlement x)
					{
						if (x.Town != null && !x.Town.IsCastle && !x.MapFaction.IsAtWarWith(base.IssueOwner.MapFaction))
						{
							int price = x.Town.MarketData.GetPrice(DefaultItems.Grain, MobileParty.MainParty);
							int inStore = x.Town.MarketData.GetCategoryData(DefaultItemCategories.Grain).InStore;
							bool num = price > 0 && price < AverageGrainPriceInCalradia * 2;
							bool flag = inStore < 250;
							return num && flag;
						}
						return false;
					});
					_nearbySuitableSettlementCache = nearbySuitableSettlementCache;
				}
				return _nearbySuitableSettlementCache;
			}
		}

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=LPMXVHHT}{ISSUE_SETTLEMENT} Needs Grain Seeds");
				textObject.SetTextVariable("ISSUE_SETTLEMENT", base.IssueSettlement.Name);
				return textObject;
			}
		}

		public override TextObject Description
		{
			get
			{
				TextObject textObject = new TextObject("{=OJObD61e}The headman of {ISSUE_SETTLEMENT} needs grain seeds for the coming sowing season.");
				textObject.SetTextVariable("ISSUE_SETTLEMENT", base.IssueSettlement.Name);
				return textObject;
			}
		}

		public override TextObject IssueBriefByIssueGiver => new TextObject("{=p1buAbOQ}The harvest has been poor, and rats have eaten much of our stores. We can eat less and tighten our belts, but if we don't have seed grain left over to plant, we'll starve next year.[if:convo_dismayed][ib:demure2]");

		public override TextObject IssueAcceptByPlayer => new TextObject("{=vKwndBbe}Is there a way to prevent this?");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=nG750jQB}Grain will solve our problems. If we had {GRAIN_AMOUNT} bushels, we could use it to sow our fields. But I doubt that {NEARBY_TOWN} has so much to sell at this time of the year. {GRAIN_AMOUNT} bushels of grain costs around {DENAR_AMOUNT}{GOLD_ICON} in the markets, and we don't have that![if:convo_thinking]");
				int price = NearbySuitableSettlement.Town.MarketData.GetPrice(DefaultItems.Grain, MobileParty.MainParty);
				textObject.SetTextVariable("NEARBY_TOWN", NearbySuitableSettlement.Name);
				textObject.SetTextVariable("GRAIN_AMOUNT", NeededGrainAmount);
				textObject.SetTextVariable("DENAR_AMOUNT", price * NeededGrainAmount);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=5NYPqKBj}I know you're busy, but maybe you can ask some of your men to find us that grain? {MEN_COUNT} men should do the job along with {GOLD}{GOLD_ICON}, and I'd reckon the whole affair should take two weeks.{newline}I'm desperate here, {?PLAYER.GENDER}madam{?}sir{\\?}... Don't let our children starve![if:convo_dismayed][ib:demure]");
				textObject.SetTextVariable("MEN_COUNT", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("GOLD", AlternativeSolutionNeededGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				textObject.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject);
				return textObject;
			}
		}

		public override TextObject IssueQuestSolutionAcceptByPlayer => new TextObject("{=ihfuqu2S}I will find that seed grain for you.");

		public override TextObject IssueAlternativeSolutionAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=HCMsvAFv}I can order one of my companions and {MEN_COUNT} men to find grain for you.");
				textObject.SetTextVariable("MEN_COUNT", GetTotalAlternativeSolutionNeededMenCount());
				return textObject;
			}
		}

		public override TextObject IssueDiscussAlternativeSolution => new TextObject("{=W6X5DffB}Thank you for sparing the men to bring us that seed grain, {?PLAYER.GENDER}madam{?}sir{\\?}. That should get us through the hard times ahead.[if:convo_grateful][ib:normal]");

		public override TextObject IssueAsRumorInSettlement
		{
			get
			{
				TextObject textObject = new TextObject("{=WVobv24n}Heaven save us if {QUEST_GIVER.NAME} can't get {?QUEST_GIVER.GENDER}her{?}his{\\?} hands on more grain.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver => new TextObject("{=k63ZKmXX}Thank you, {?PLAYER.GENDER}milady{?}sir{\\?}! You are a saviour.[if:convo_grateful][ib:normal]");

		public override bool IsThereAlternativeSolution => true;

		public override bool IsThereLordSolution => false;

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=a0UTO8tW}{ISSUE_OWNER.LINK}, the headman of {ISSUE_SETTLEMENT}, asked you to deliver {GRAIN_AMOUNT} bushels of grain to {?QUEST_GIVER.GENDER}her{?}him{\\?} to use as seeds. Otherwise the peasants cannot sow their fields and starve in the coming season. You have agreed to send your companion {COMPANION.NAME} along with {MEN_COUNT} men to find some grain and return to the village. Your men should return in {RETURN_DAYS} days.");
				StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				textObject.SetTextVariable("ISSUE_SETTLEMENT", base.IssueSettlement.Name);
				textObject.SetTextVariable("GRAIN_AMOUNT", NeededGrainAmount);
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				textObject.SetTextVariable("MEN_COUNT", AlternativeSolutionSentTroops.TotalManCount - 1);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=IYSfevY0}Your companion has delivered {GRAIN_AMOUNT} units of grain to the villagers. They will be able to sow them before the coming winter. You have saved a lot of lives today. The Headman and the villagers are grateful.");
				textObject.SetTextVariable("GRAIN_AMOUNT", NeededGrainAmount);
				return textObject;
			}
		}

		protected override int CompanionSkillRewardXP => (int)(500f + 700f * base.IssueDifficultyMultiplier);

		internal static void AutoGeneratedStaticCollectObjectsHeadmanNeedsGrainIssue(object o, List<object> collectedObjects)
		{
			((HeadmanNeedsGrainIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		public HeadmanNeedsGrainIssue(Hero issueOwner)
			: base(issueOwner, CampaignTime.DaysFromNow(30f))
		{
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.SettlementProsperity)
			{
				return -0.2f;
			}
			if (issueEffect == DefaultIssueEffects.SettlementLoyalty)
			{
				return -0.5f;
			}
			return 0f;
		}

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			return ((hero.GetSkillValue(DefaultSkills.Trade) >= hero.GetSkillValue(DefaultSkills.Medicine)) ? DefaultSkills.Trade : DefaultSkills.Medicine, 120);
		}

		public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		public override bool AlternativeSolutionCondition(out TextObject explanation)
		{
			if (QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation))
			{
				return QuestHelper.CheckGoldForAlternativeSolution(AlternativeSolutionNeededGold, out explanation);
			}
			return false;
		}

		public override void AlternativeSolutionStartConsequence()
		{
			GiveGoldAction.ApplyForCharacterToParty(Hero.MainHero, base.IssueSettlement.Party, AlternativeSolutionNeededGold);
			TextObject textObject = new TextObject("{=ex6ZhAAv}You gave {DENAR}{GOLD_ICON} to companion to buy {GRAIN_AMOUNT} units of grain for the {ISSUE_OWNER.NAME}.");
			textObject.SetTextVariable("GRAIN_AMOUNT", NeededGrainAmount);
			textObject.SetTextVariable("DENAR", AlternativeSolutionNeededGold);
			textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
			StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.IssueOwner.CharacterObject, textObject);
			MBInformationManager.AddQuickInformation(textObject);
		}

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			TraitLevelingHelper.OnIssueSolvedThroughAlternativeSolution(base.IssueOwner, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Generosity, 30)
			});
			base.IssueOwner.AddPower(10f);
			base.IssueSettlement.Village.Bound.Town.Prosperity += 50f;
			GainRenownAction.Apply(Hero.MainHero, 1f);
			RelationshipChangeWithIssueOwner = 5;
			foreach (Hero notable in base.IssueOwner.CurrentSettlement.Notables)
			{
				if (notable != base.IssueOwner)
				{
					ChangeRelationAction.ApplyPlayerRelation(notable, 1);
				}
			}
		}

		protected override void AlternativeSolutionEndWithFailureConsequence()
		{
			base.IssueOwner.AddPower(-5f);
			foreach (Hero notable in base.IssueOwner.CurrentSettlement.Notables)
			{
				ChangeRelationAction.ApplyPlayerRelation(notable, -3);
			}
			base.IssueSettlement.Village.Bound.Town.Prosperity += -10f;
		}

		public override IssueFrequency GetFrequency()
		{
			return IssueFrequency.Common;
		}

		public override bool IssueStayAliveConditions()
		{
			if (NearbySuitableSettlement != null)
			{
				return NearbySuitableSettlement.Town.MarketData.GetItemCountOfCategory(DefaultItems.Grain.ItemCategory) < 350;
			}
			return false;
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}

		protected override void OnGameLoad()
		{
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new HeadmanNeedsGrainIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(18f), base.IssueDifficultyMultiplier, RewardGold, NeededGrainAmount);
		}

		protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out PreconditionFlags flag, out Hero relationHero, out SkillObject skill, out int requiredGold)
		{
			skill = null;
			relationHero = null;
			requiredGold = 0;
			flag = PreconditionFlags.None;
			if (issueGiver.GetRelationWithPlayer() < -10f)
			{
				flag |= PreconditionFlags.Relation;
				relationHero = issueGiver;
			}
			if (FactionManager.IsAtWarAgainstFaction(issueGiver.CurrentSettlement.MapFaction, Hero.MainHero.MapFaction))
			{
				flag |= PreconditionFlags.AtWar;
			}
			return flag == PreconditionFlags.None;
		}
	}

	public class HeadmanNeedsGrainIssueQuest : QuestBase
	{
		private const int SuccessRenownBonus = 1;

		private const int SuccessMercyBonus = 70;

		private const int SuccessGenerosityBonus = 50;

		private const int SuccessRelationBonusWithQuestGiver = 5;

		private const int SuccessRelationBonusWithOtherNotables = 1;

		private const int SuccessPowerBonus = 10;

		private const int SuccessProsperityBonus = 50;

		private const int FailRelationPenalty = -5;

		private const int FailRelationPenaltyWithOtherNotables = -3;

		private const int CrimeRatingFailHonorPenalty = -50;

		private const int CrimeRatingFailRelationshipWithQuestGiverPenalty = -5;

		private const int CrimeRatingFailQuestGiverPowerPenalty = -10;

		private const int TimeOutProsperityPenalty = -10;

		private const int TimeOutPowerPenalty = -5;

		[SaveableField(10)]
		private readonly int _neededGrainAmount;

		[SaveableField(20)]
		private int _rewardGold;

		[SaveableField(30)]
		private JournalLog _playerAcceptedQuestLog;

		[SaveableField(40)]
		private JournalLog _playerHasNeededGrainsLog;

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=LPMXVHHT}{ISSUE_SETTLEMENT} Needs Grain Seeds");
				textObject.SetTextVariable("ISSUE_SETTLEMENT", base.QuestGiver.CurrentSettlement.Name);
				return textObject;
			}
		}

		public override bool IsRemainingTimeHidden => false;

		private TextObject PlayerAcceptedQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=5CokRxmL}{QUEST_GIVER.LINK}, the headman of the {QUEST_SETTLEMENT} asked you to deliver {GRAIN_AMOUNT} units of grain to {?QUEST_GIVER.GENDER}her{?}him{\\?} to use as seeds. Otherwise peasants cannot sow their fields and starve in the coming season.{newline}{newline}You have agreed to bring them {GRAIN_AMOUNT} units of grain as soon as possible.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_SETTLEMENT", base.QuestGiver.CurrentSettlement.Name);
				textObject.SetTextVariable("GRAIN_AMOUNT", _neededGrainAmount);
				return textObject;
			}
		}

		private TextObject PlayerHasNeededGrainsLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=vOHc5dxC}You now have enough grain seeds to complete the quest. Return to {QUEST_SETTLEMENT} to hand them over.");
				textObject.SetTextVariable("QUEST_SETTLEMENT", base.QuestGiver.CurrentSettlement.Name);
				return textObject;
			}
		}

		private TextObject QuestTimeoutLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=brDw7ewN}You have failed to deliver {GRAIN_AMOUNT} units of grain to the villagers. They won't be able to sow them before the coming winter. The Headman and the villagers are doomed.");
				textObject.SetTextVariable("GRAIN_AMOUNT", _neededGrainAmount);
				return textObject;
			}
		}

		private TextObject SuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=GGTxzAtn}You have delivered {GRAIN_AMOUNT} units of grain to the villagers. They will be able to sow them before the coming winter. You have saved a lot of lives today. The Headman and the villagers are grateful.");
				textObject.SetTextVariable("GRAIN_AMOUNT", _neededGrainAmount);
				return textObject;
			}
		}

		private TextObject CancelLogOnWarDeclared
		{
			get
			{
				TextObject textObject = new TextObject("{=8Z4vlcib}Your clan is now at war with the {ISSUE_GIVER.LINK}'s lord. Your agreement with {ISSUE_GIVER.LINK} was canceled.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.QuestGiver.CharacterObject, textObject);
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

		private TextObject CancelLogOnVillageRaided
		{
			get
			{
				TextObject textObject = new TextObject("{=PgFJLK85}{SETTLEMENT_NAME} is raided. It isn’t safe for the villagers to plant their fields, and agreement with {ISSUE_GIVER.LINK} was canceled.");
				textObject.SetTextVariable("SETTLEMENT_NAME", base.QuestGiver.CurrentSettlement.Name);
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsHeadmanNeedsGrainIssueQuest(object o, List<object> collectedObjects)
		{
			((HeadmanNeedsGrainIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_playerAcceptedQuestLog);
			collectedObjects.Add(_playerHasNeededGrainsLog);
		}

		internal static object AutoGeneratedGetMemberValue_neededGrainAmount(object o)
		{
			return ((HeadmanNeedsGrainIssueQuest)o)._neededGrainAmount;
		}

		internal static object AutoGeneratedGetMemberValue_rewardGold(object o)
		{
			return ((HeadmanNeedsGrainIssueQuest)o)._rewardGold;
		}

		internal static object AutoGeneratedGetMemberValue_playerAcceptedQuestLog(object o)
		{
			return ((HeadmanNeedsGrainIssueQuest)o)._playerAcceptedQuestLog;
		}

		internal static object AutoGeneratedGetMemberValue_playerHasNeededGrainsLog(object o)
		{
			return ((HeadmanNeedsGrainIssueQuest)o)._playerHasNeededGrainsLog;
		}

		public HeadmanNeedsGrainIssueQuest(string questId, Hero giverHero, CampaignTime duration, float difficultyMultiplier, int rewardGold, int neededGrainAmount)
			: base(questId, giverHero, duration, rewardGold)
		{
			_neededGrainAmount = neededGrainAmount;
			_rewardGold = rewardGold;
			SetDialogs();
			InitializeQuestOnCreation();
		}

		protected override void InitializeQuestOnGameLoad()
		{
			SetDialogs();
		}

		protected override void HourlyTick()
		{
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.PlayerInventoryExchangeEvent.AddNonSerializedListener(this, OnPlayerInventoryExchange);
			CampaignEvents.OnPartyConsumedFoodEvent.AddNonSerializedListener(this, OnPartyConsumedFood);
			CampaignEvents.OnHeroSharedFoodWithAnotherHeroEvent.AddNonSerializedListener(this, OnHeroSharedFoodWithAnotherHero);
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
			CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
			CampaignEvents.VillageBeingRaided.AddNonSerializedListener(this, OnVillageBeingRaided);
		}

		protected override void HourlyTickParty(MobileParty mobileParty)
		{
			if (mobileParty == MobileParty.MainParty)
			{
				_playerAcceptedQuestLog.UpdateCurrentProgress(GetRequiredGrainCountOnPlayer());
				CheckIfPlayerReadyToReturnGrains();
			}
		}

		private void OnHeroPrisonerTaken(PartyBase capturer, Hero prisoner)
		{
			if (prisoner == Hero.MainHero)
			{
				_playerAcceptedQuestLog.UpdateCurrentProgress(GetRequiredGrainCountOnPlayer());
				CheckIfPlayerReadyToReturnGrains();
			}
		}

		private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
		{
			if (party == MobileParty.MainParty)
			{
				_playerAcceptedQuestLog.UpdateCurrentProgress(GetRequiredGrainCountOnPlayer());
				CheckIfPlayerReadyToReturnGrains();
			}
		}

		private void OnVillageBeingRaided(Village village)
		{
			bool flag = MobileParty.MainParty.MapEvent != null && MobileParty.MainParty.MapEvent.IsRaid && MobileParty.MainParty.CurrentSettlement == village.Settlement;
			if (village == base.QuestGiver.CurrentSettlement.Village && !flag)
			{
				CompleteQuestWithCancel(CancelLogOnVillageRaided);
			}
		}

		protected override void OnTimedOut()
		{
			AddLog(QuestTimeoutLogText);
			TimeoutFail();
		}

		protected override void SetDialogs()
		{
			TextObject textObject = new TextObject("{=nwIYsJRO}Have you brought our grain {?PLAYER.GENDER}milady{?}sir{\\?}?[if:convo_shocked][ib:demure2]");
			TextObject textObject2 = new TextObject("{=zsE7ldPY}Thank you, {?PLAYER.GENDER}milady{?}sir{\\?}! You are a saviour.[if:convo_merry][ib:normal2]");
			TextObject textObject3 = new TextObject("{=0tB3VGE4}We await your success, {?PLAYER.GENDER}milady{?}sir{\\?}.[if:convo_nervous]");
			textObject.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject);
			textObject2.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject);
			textObject3.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject);
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(textObject2).Condition(() => CharacterObject.OneToOneConversationCharacter == base.QuestGiver.CharacterObject)
				.Consequence(QuestAcceptedConsequences)
				.CloseDialog();
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(textObject).Condition(delegate
			{
				MBTextManager.SetTextVariable("GRAIN_AMOUNT", _neededGrainAmount);
				return CharacterObject.OneToOneConversationCharacter == base.QuestGiver.CharacterObject;
			})
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=9UABeRWO}Yes. Here is your grain."))
				.ClickableCondition(CompleteQuestClickableConditions)
				.NpcLine(textObject2)
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += Success;
				})
				.CloseDialog()
				.PlayerOption(new TextObject("{=PI6ikMsc}I'm working on it."))
				.NpcLine(textObject3)
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
		}

		private bool CompleteQuestClickableConditions(out TextObject explanation)
		{
			if (_playerAcceptedQuestLog.CurrentProgress >= _neededGrainAmount)
			{
				explanation = null;
				return true;
			}
			explanation = new TextObject("{=mzabdwoh}You don't have enough grain.");
			return false;
		}

		private void QuestAcceptedConsequences()
		{
			StartQuest();
			int requiredGrainCountOnPlayer = GetRequiredGrainCountOnPlayer();
			_playerAcceptedQuestLog = AddDiscreteLog(PlayerAcceptedQuestLogText, new TextObject("{=eEwI880g}Collect Grain"), requiredGrainCountOnPlayer, _neededGrainAmount);
		}

		private int GetRequiredGrainCountOnPlayer()
		{
			int itemNumber = PartyBase.MainParty.ItemRoster.GetItemNumber(DefaultItems.Grain);
			if (itemNumber <= _neededGrainAmount)
			{
				return itemNumber;
			}
			return _neededGrainAmount;
		}

		private void CheckIfPlayerReadyToReturnGrains()
		{
			if (_playerHasNeededGrainsLog == null && _playerAcceptedQuestLog.CurrentProgress >= _neededGrainAmount)
			{
				_playerHasNeededGrainsLog = AddLog(PlayerHasNeededGrainsLogText);
				TextObject textObject = new TextObject("{=Gtbfm10o}You have enough grain to complete the quest. Return to {QUEST_SETTLEMENT} to hand it over.");
				textObject.SetTextVariable("QUEST_SETTLEMENT", base.QuestGiver.CurrentSettlement.Name);
				MBInformationManager.AddQuickInformation(textObject);
			}
			else if (_playerHasNeededGrainsLog != null && _playerAcceptedQuestLog.CurrentProgress < _neededGrainAmount)
			{
				RemoveLog(_playerHasNeededGrainsLog);
				_playerHasNeededGrainsLog = null;
			}
		}

		private void OnPlayerInventoryExchange(List<(ItemRosterElement, int)> purchasedItems, List<(ItemRosterElement, int)> soldItems, bool isTrading)
		{
			bool flag = false;
			foreach (var (itemRosterElement, _) in purchasedItems)
			{
				if (itemRosterElement.EquipmentElement.Item == DefaultItems.Grain)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				foreach (var (itemRosterElement, _) in soldItems)
				{
					if (itemRosterElement.EquipmentElement.Item == DefaultItems.Grain)
					{
						flag = true;
						break;
					}
				}
			}
			if (flag)
			{
				_playerAcceptedQuestLog.UpdateCurrentProgress(GetRequiredGrainCountOnPlayer());
				CheckIfPlayerReadyToReturnGrains();
			}
		}

		private void OnPartyConsumedFood(MobileParty party)
		{
			if (party.IsMainParty)
			{
				_playerAcceptedQuestLog.UpdateCurrentProgress(GetRequiredGrainCountOnPlayer());
				CheckIfPlayerReadyToReturnGrains();
			}
		}

		private void OnHeroSharedFoodWithAnotherHero(Hero supporterHero, Hero supportedHero, float influence)
		{
			if (supporterHero == Hero.MainHero || supportedHero == Hero.MainHero)
			{
				_playerAcceptedQuestLog.UpdateCurrentProgress(GetRequiredGrainCountOnPlayer());
				CheckIfPlayerReadyToReturnGrains();
			}
		}

		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (base.QuestGiver.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				CompleteQuestWithCancel(CancelLogOnWarDeclared);
			}
		}

		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
		{
			if (detail == DeclareWarAction.DeclareWarDetail.CausedByCrimeRatingChange || (detail == DeclareWarAction.DeclareWarDetail.CausedByPlayerHostility && MobileParty.MainParty.MapEvent != null && MobileParty.MainParty.MapEvent.IsRaid && MobileParty.MainParty.MapEvent.MapEventSettlement.MapFaction == base.QuestGiver.MapFaction && base.QuestGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction)))
			{
				CriminalRatingFail();
			}
			else
			{
				QuestHelper.CheckWarDeclarationAndFailOrCancelTheQuest(this, faction1, faction2, detail, PlayerDeclaredWarQuestLogText, CancelLogOnWarDeclared, forceCancel: true);
			}
		}

		private void Success()
		{
			AddLog(SuccessLog);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[2]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Mercy, 70),
				new Tuple<TraitObject, int>(DefaultTraits.Generosity, 50)
			});
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, _rewardGold);
			ItemRosterElement itemRosterElement = new ItemRosterElement(DefaultItems.Grain, _neededGrainAmount);
			GiveItemAction.ApplyForParties(PartyBase.MainParty, Settlement.CurrentSettlement.Party, in itemRosterElement);
			GainRenownAction.Apply(Hero.MainHero, 1f);
			base.QuestGiver.AddPower(10f);
			base.QuestGiver.CurrentSettlement.Village.Bound.Town.Prosperity += 50f;
			RelationshipChangeWithQuestGiver = 5;
			foreach (Hero notable in base.QuestGiver.CurrentSettlement.Notables)
			{
				if (notable != base.QuestGiver)
				{
					ChangeRelationAction.ApplyPlayerRelation(notable, 1);
				}
			}
			CompleteQuestWithSuccess();
		}

		private void TimeoutFail()
		{
			base.QuestGiver.AddPower(-5f);
			base.QuestGiver.CurrentSettlement.Village.Bound.Town.Prosperity += -10f;
			RelationshipChangeWithQuestGiver = -5;
			foreach (Hero notable in base.QuestGiver.CurrentSettlement.Notables)
			{
				if (notable != base.QuestGiver)
				{
					ChangeRelationAction.ApplyPlayerRelation(notable, -3);
				}
			}
		}

		private void CriminalRatingFail()
		{
			base.QuestGiver.AddPower(-10f);
			RelationshipChangeWithQuestGiver = -5;
			TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -50)
			});
			TextObject textObject = new TextObject("{=9w1RbVEk}You are accused of a crime in {SETTLEMENT}, and {QUEST_GIVER.LINK} no longer wants your help.");
			textObject.SetTextVariable("SETTLEMENT", MobileParty.MainParty.MapEvent.MapEventSettlement.Name);
			textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
			CompleteQuestWithFail(textObject);
		}
	}

	public class HeadmanNeedsGrainIssueTypeDefiner : SaveableTypeDefiner
	{
		public HeadmanNeedsGrainIssueTypeDefiner()
			: base(440000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(HeadmanNeedsGrainIssue), 1);
			AddClassDefinition(typeof(HeadmanNeedsGrainIssueQuest), 2);
		}
	}

	private const IssueBase.IssueFrequency HeadmanNeedsGrainIssueFrequency = IssueBase.IssueFrequency.Common;

	private const int NearbyTownMarketGrainLimit = 350;

	private int _averageGrainPriceInCalradia;

	private static int AverageGrainPriceInCalradia => Campaign.Current.GetCampaignBehavior<HeadmanNeedsGrainIssueBehavior>()._averageGrainPriceInCalradia;

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
		CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, WeeklyTick);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
		CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, OnNewGameCreatedPartialFollowUp);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private bool ConditionsHold(Hero issueGiver)
	{
		if (issueGiver.CurrentSettlement == null || !issueGiver.IsNotable || !issueGiver.CurrentSettlement.IsVillage || !issueGiver.CurrentSettlement.Village.Bound.IsTown)
		{
			return false;
		}
		if (issueGiver.IsHeadman && issueGiver.CurrentSettlement.Village.VillageType != DefaultVillageTypes.WheatFarm && issueGiver.CurrentSettlement.Village.Bound.Town.MarketData.GetCategoryData(DefaultItemCategories.Grain).InStore < 30)
		{
			return (float)issueGiver.CurrentSettlement.Village.GetItemPrice(DefaultItems.Grain) > (float)_averageGrainPriceInCalradia * 0.9f;
		}
		return false;
	}

	public void OnCheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero))
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnSelected, typeof(HeadmanNeedsGrainIssue), IssueBase.IssueFrequency.Common));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(HeadmanNeedsGrainIssue), IssueBase.IssueFrequency.Common));
		}
	}

	private IssueBase OnSelected(in PotentialIssueData pid, Hero issueOwner)
	{
		return new HeadmanNeedsGrainIssue(issueOwner);
	}

	private void WeeklyTick()
	{
		CacheGrainPrice();
	}

	private void OnGameLoadFinished()
	{
		CacheGrainPrice();
	}

	private void OnNewGameCreatedPartialFollowUp(CampaignGameStarter starter, int i)
	{
		if (i == 99)
		{
			CacheGrainPrice();
		}
	}

	private void CacheGrainPrice()
	{
		_averageGrainPriceInCalradia = QuestHelper.GetAveragePriceOfItemInTheWorld(DefaultItems.Grain);
	}
}
