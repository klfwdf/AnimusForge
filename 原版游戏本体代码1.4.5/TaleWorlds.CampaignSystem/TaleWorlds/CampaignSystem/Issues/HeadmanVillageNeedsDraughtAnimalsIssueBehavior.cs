using System;
using System.Collections.Generic;
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

public class HeadmanVillageNeedsDraughtAnimalsIssueBehavior : CampaignBehaviorBase
{
	public class HeadmanVillageNeedsDraughtAnimalsIssue : IssueBase
	{
		private const int IssueActiveTime = 30;

		private const int QuestDuration = 30;

		private const int VillageHearthConstant = 300;

		private const int AlternativeSolutionTroopTierRequirement = 2;

		private const int CompanionRequiredSkillLevel = 120;

		[CachedData]
		private readonly MBList<ItemObject> _possibleAnimals = new MBList<ItemObject>
		{
			Game.Current.ObjectManager.GetObject<ItemObject>("cow"),
			Game.Current.ObjectManager.GetObject<ItemObject>("mule"),
			Game.Current.ObjectManager.GetObject<ItemObject>("sumpter_horse")
		};

		[SaveableField(1)]
		private ItemObject _selectedAnimal;

		[SaveableField(2)]
		private bool _isQuestWithMeatOffer;

		[SaveableField(3)]
		private int _requestedAnimalAmount;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.Duration;

		private ItemObject _meatItem => Game.Current.ObjectManager.GetObject<ItemObject>("meat");

		private int OfferedMeatAmount => (300 + _selectedAnimal.Value * _requestedAnimalAmount) / _meatItem.Value * 2;

		private int GoldRequiredForAlternativeSolution => (int)((float)(_selectedAnimal.Value * _requestedAnimalAmount) * 0.7f);

		public override int AlternativeSolutionBaseNeededMenCount => 3 + TaleWorlds.Library.MathF.Ceiling(3f * base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 3 + TaleWorlds.Library.MathF.Ceiling(5f * base.IssueDifficultyMultiplier);

		protected override int RewardGold
		{
			get
			{
				if (_isQuestWithMeatOffer)
				{
					return 0;
				}
				return 500 + _selectedAnimal.Value * _requestedAnimalAmount;
			}
		}

		public override TextObject IssueBriefByIssueGiver => new TextObject("{=9nxTUZkO}We do have a problem. Last winter was hard on our animals. A number died from disease, and others were taken by wolves. We'd go to town to buy more, but, well, herds make a tempting target for bandits and we're not really suited to fight them. We can't afford to slaughter even the oldest and weakest of our animals because we need them to pull the plough. Maybe you can help us?[if:convo_grave][ib:demure2]");

		public override TextObject IssueAcceptByPlayer => new TextObject("{=nvaLVB5f}Tell me your needs.");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject;
				if (_isQuestWithMeatOffer)
				{
					textObject = new TextObject("{=aExKdXmx}We need {REQUESTED_ANIMAL_AMOUNT} {.%}{?(REQUESTED_ANIMAL_AMOUNT > 1)}{PLURAL(SELECTED_ANIMAL)}{?}{SELECTED_ANIMAL}{\\?}{.%}.[if:convo_normal][ib:demure] To be honest our village is poor and our coffers are empty. We can make payment only as meat - the meat of the old animals that we'll slaughter as soon as you bring us the new ones. We can offer {MEAT_AMOUNT} loads of meat, will you accept that, {?PLAYER.GENDER}madam{?}sir{\\?}?");
					textObject.SetTextVariable("MEAT_AMOUNT", OfferedMeatAmount);
				}
				else
				{
					textObject = new TextObject("{=TEhwK74M}We are willing to pay {REWARD}{GOLD_ICON} denars for {REQUESTED_ANIMAL_AMOUNT} healthy and strong [if:convo_normal][ib:demure]{.%}{?(REQUESTED_ANIMAL_AMOUNT > 1)}{PLURAL(SELECTED_ANIMAL)}{?}{SELECTED_ANIMAL}{\\?}{.%}. Unlike us, I'm sure you can travel distant villages easily and find the finest and cheapest ones there.");
					textObject.SetTextVariable("REWARD", RewardGold);
				}
				textObject.SetTextVariable("REQUESTED_ANIMAL_AMOUNT", _requestedAnimalAmount);
				textObject.SetTextVariable("SELECTED_ANIMAL", _selectedAnimal.Name);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		public override TextObject IssuePlayerResponseAfterAlternativeExplanation => new TextObject("{=qYSrlr4q}Maybe I should send one of my men to find the animals you need.");

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=bvEOmHWd}I think a man who knows how to trade alongside {ALTERNATIVE_TROOP_AMOUNT} [if:convo_undecided_open]fighters can get the job done without trouble. they will need {GOLD_REQUIRED_FOR_ALTERNATIVE_SOLUTION}{GOLD_ICON} denars to buy the animals. You or one of your companions, {?PLAYER.GENDER}madam{?}sir{\\?} - it doesn't matter for us as long as you find the animals we need...");
				textObject.SetTextVariable("ALTERNATIVE_TROOP_AMOUNT", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("GOLD_REQUIRED_FOR_ALTERNATIVE_SOLUTION", GoldRequiredForAlternativeSolution);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		public override TextObject IssueQuestSolutionAcceptByPlayer => new TextObject("{=iq3yEuO9}All right. I will bring your animals by myself.");

		public override TextObject IssueAlternativeSolutionAcceptByPlayer => new TextObject("{=BMQQSeTp}I'm sure my men will find what you need and bring them to you in time.");

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver => new TextObject("{=D9LxQxNa}Thank you for your help {?PLAYER.GENDER}madam{?}sir{\\?}.[if:convo_bemused][ib:demure2] We will be waiting for your men. Good luck to you all.");

		public override TextObject IssueDiscussAlternativeSolution => new TextObject("{=jol73R8f}We are still waiting for your men to arrive, {?PLAYER.GENDER}ma'am{?}sir{\\?}. The village needs the animals they are bringing.");

		public override bool IsThereAlternativeSolution => true;

		public override bool IsThereLordSolution => false;

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=QJn8pbzh}{QUEST_GIVER.LINK} told you that {?QUEST_GIVER.GENDER}she{?}he{\\?} needs {.%}{?(REQUESTED_ANIMAL_AMOUNT > 1)}{PLURAL(SELECTED_ANIMAL)}{?}{SELECTED_ANIMAL}{\\?}{.%} for {?QUEST_GIVER.GENDER}her{?}his{\\?} village. {?QUEST_GIVER.GENDER}She{?}He{\\?} will pay you {REWARD} when the animals are delivered. You asked your {COMPANION.LINK} and {ALTERNATIVE_TROOP_AMOUNT} of your men to deliver {REQUESTED_ANIMAL_AMOUNT} {.%}{?(REQUESTED_ANIMAL_AMOUNT > 1)}{PLURAL(SELECTED_ANIMAL)}{?}{SELECTED_ANIMAL}{\\?}{.%} to {QUEST_GIVER.LINK}. They will rejoin your party in {RETURN_DAYS} days.");
				textObject.SetTextVariable("ALTERNATIVE_TROOP_AMOUNT", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("REQUESTED_ANIMAL_AMOUNT", _requestedAnimalAmount);
				textObject.SetTextVariable("SELECTED_ANIMAL", _selectedAnimal.Name);
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				TextObject empty = TextObject.GetEmpty();
				if (!_isQuestWithMeatOffer)
				{
					empty = new TextObject("{=OyOn7N84}{REWARD_GOLD}{GOLD_ICON} denars");
					empty.SetTextVariable("REWARD_GOLD", RewardGold);
					empty.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				}
				else
				{
					empty = new TextObject("{=3bh1q4ZV}{MEAT_AMOUNT} meat");
					empty.SetTextVariable("MEAT_AMOUNT", OfferedMeatAmount);
				}
				textObject.SetTextVariable("REWARD", empty);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject Title => new TextObject("{=MXLcZPaO}Village Needs Draught Animals");

		public override TextObject Description => new TextObject("{=Ntv5KPFe}Headman in the village requested draught animals to replace them with old ones.");

		protected override int CompanionSkillRewardXP => (int)(500f + 700f * base.IssueDifficultyMultiplier);

		internal static void AutoGeneratedStaticCollectObjectsHeadmanVillageNeedsDraughtAnimalsIssue(object o, List<object> collectedObjects)
		{
			((HeadmanVillageNeedsDraughtAnimalsIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_selectedAnimal);
		}

		internal static object AutoGeneratedGetMemberValue_selectedAnimal(object o)
		{
			return ((HeadmanVillageNeedsDraughtAnimalsIssue)o)._selectedAnimal;
		}

		internal static object AutoGeneratedGetMemberValue_isQuestWithMeatOffer(object o)
		{
			return ((HeadmanVillageNeedsDraughtAnimalsIssue)o)._isQuestWithMeatOffer;
		}

		internal static object AutoGeneratedGetMemberValue_requestedAnimalAmount(object o)
		{
			return ((HeadmanVillageNeedsDraughtAnimalsIssue)o)._requestedAnimalAmount;
		}

		public HeadmanVillageNeedsDraughtAnimalsIssue(Hero issueOwner)
			: base(issueOwner, CampaignTime.DaysFromNow(30f))
		{
			_selectedAnimal = _possibleAnimals.GetRandomElement();
			_isQuestWithMeatOffer = issueOwner.CurrentSettlement.Village.Hearth <= 300f;
			_requestedAnimalAmount = TaleWorlds.Library.MathF.Round((float)(int)(5000f / (float)_selectedAnimal.Value) * base.IssueDifficultyMultiplier);
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

		public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
		{
			return character.Tier >= 2;
		}

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			return ((hero.GetSkillValue(DefaultSkills.Riding) >= hero.GetSkillValue(DefaultSkills.Trade)) ? DefaultSkills.Riding : DefaultSkills.Trade, 120);
		}

		public override bool AlternativeSolutionCondition(out TextObject explanation)
		{
			if (QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2))
			{
				return QuestHelper.CheckGoldForAlternativeSolution(GoldRequiredForAlternativeSolution, out explanation);
			}
			return false;
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

		public override void AlternativeSolutionStartConsequence()
		{
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, GoldRequiredForAlternativeSolution);
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
			return new HeadmanVillageNeedsDraughtAnimalsIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(30f), _selectedAnimal, _requestedAnimalAmount, _isQuestWithMeatOffer, OfferedMeatAmount, RewardGold);
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			base.IssueOwner.AddPower(10f);
			ChangeRelationAction.ApplyPlayerRelation(base.IssueOwner, 5);
			base.IssueOwner.CurrentSettlement.Village.Hearth += 30f;
			if (_isQuestWithMeatOffer)
			{
				ItemObject item = Game.Current.ObjectManager.GetObject<ItemObject>("meat");
				MobileParty.MainParty.ItemRoster.AddToCounts(item, OfferedMeatAmount);
			}
		}
	}

	public class HeadmanVillageNeedsDraughtAnimalsIssueQuest : QuestBase
	{
		[SaveableField(1)]
		private ItemObject _requestedAnimal;

		[SaveableField(2)]
		private int _requestedAnimalAmount;

		private int _currentAnimalAmount;

		[SaveableField(3)]
		private bool _isQuestWithMeatOffer;

		[SaveableField(4)]
		private int _discountValue;

		[SaveableField(5)]
		private int _offeredMeatAmount;

		[SaveableField(6)]
		private JournalLog _questProgressLogTest;

		public override TextObject Title => new TextObject("{=MXLcZPaO}Village Needs Draught Animals");

		public override bool IsRemainingTimeHidden => false;

		private TextObject QuestStartedLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=V7YG3nKb}{QUEST_GIVER.LINK} told you that {?QUEST_GIVER.GENDER}she{?}he{\\?} needs {.%}{?(REQUESTED_ANIMAL_AMOUNT > 1)}{PLURAL(SELECTED_ANIMAL)}{?}{SELECTED_ANIMAL}{\\?}{.%} for {?QUEST_GIVER.GENDER}her{?}his{\\?} village. {?QUEST_GIVER.GENDER}She{?}He{\\?} asked you to bring {REQUESTED_ANIMAL_AMOUNT} {.%}{?(REQUESTED_ANIMAL_AMOUNT > 1)}{PLURAL(SELECTED_ANIMAL)}{?}{SELECTED_ANIMAL}{\\?}{.%} to {?QUEST_GIVER.GENDER}her{?}him{\\?}.");
				textObject.SetTextVariable("REQUESTED_ANIMAL_AMOUNT", _requestedAnimalAmount);
				textObject.SetTextVariable("SELECTED_ANIMAL", _requestedAnimal.Name);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject QuestStartedLogTextWithMeat
		{
			get
			{
				TextObject textObject = new TextObject("{=7VUNi3zy}{QUEST_GIVER.LINK} will make payment as {MEAT_AMOUNT} meat when the task is done.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("MEAT_AMOUNT", _offeredMeatAmount);
				return textObject;
			}
		}

		private TextObject QuestSuccessLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=uubL3Uck}You brought {REQUESTED_ANIMAL_AMOUNT} {.%}{?(REQUESTED_ANIMAL_AMOUNT > 1)}{PLURAL(SELECTED_ANIMAL)}{?}{SELECTED_ANIMAL}{\\?}{.%} to {?QUEST_GIVER.GENDER}her{?}him{\\?} as promised.");
				textObject.SetTextVariable("REQUESTED_ANIMAL_AMOUNT", _requestedAnimalAmount);
				textObject.SetTextVariable("SELECTED_ANIMAL", _requestedAnimal.Name);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject QuestStartedLogTextWithDenars
		{
			get
			{
				TextObject textObject = new TextObject("{=7LjTNs1k}{QUEST_GIVER.LINK} will pay you {REWARD_GOLD}{GOLD_ICON} denars when the task is done.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("REWARD_GOLD", RewardGold);
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

		private TextObject QuestFailedWithTimeOutLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=xvCzjcjU}You failed to deliver {REQUESTED_ANIMAL_AMOUNT} {.%}{?(REQUESTED_ANIMAL_AMOUNT > 1)}{REQUESTED_ANIMAL}{.s}{?}{REQUESTED_ANIMAL}{\\?}{.%} to {QUEST_GIVER.LINK} in time.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("REQUESTED_ANIMAL_AMOUNT", _requestedAnimalAmount);
				textObject.SetTextVariable("REQUESTED_ANIMAL", _requestedAnimal.Name);
				return textObject;
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsHeadmanVillageNeedsDraughtAnimalsIssueQuest(object o, List<object> collectedObjects)
		{
			((HeadmanVillageNeedsDraughtAnimalsIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_requestedAnimal);
			collectedObjects.Add(_questProgressLogTest);
		}

		internal static object AutoGeneratedGetMemberValue_requestedAnimal(object o)
		{
			return ((HeadmanVillageNeedsDraughtAnimalsIssueQuest)o)._requestedAnimal;
		}

		internal static object AutoGeneratedGetMemberValue_requestedAnimalAmount(object o)
		{
			return ((HeadmanVillageNeedsDraughtAnimalsIssueQuest)o)._requestedAnimalAmount;
		}

		internal static object AutoGeneratedGetMemberValue_isQuestWithMeatOffer(object o)
		{
			return ((HeadmanVillageNeedsDraughtAnimalsIssueQuest)o)._isQuestWithMeatOffer;
		}

		internal static object AutoGeneratedGetMemberValue_discountValue(object o)
		{
			return ((HeadmanVillageNeedsDraughtAnimalsIssueQuest)o)._discountValue;
		}

		internal static object AutoGeneratedGetMemberValue_offeredMeatAmount(object o)
		{
			return ((HeadmanVillageNeedsDraughtAnimalsIssueQuest)o)._offeredMeatAmount;
		}

		internal static object AutoGeneratedGetMemberValue_questProgressLogTest(object o)
		{
			return ((HeadmanVillageNeedsDraughtAnimalsIssueQuest)o)._questProgressLogTest;
		}

		public HeadmanVillageNeedsDraughtAnimalsIssueQuest(string questId, Hero giverHero, CampaignTime duration, ItemObject requestedAnimal, int requestedAnimalAmount, bool isQuestWithMeatOffer, int offeredMeatAmount, int rewardGold)
			: base(questId, giverHero, duration, rewardGold)
		{
			_isQuestWithMeatOffer = isQuestWithMeatOffer;
			_requestedAnimal = requestedAnimal;
			_requestedAnimalAmount = requestedAnimalAmount;
			_offeredMeatAmount = offeredMeatAmount;
			CalculateRequestedAnimalCountOnPlayer();
			SetDialogs();
			InitializeQuestOnCreation();
			if (!isQuestWithMeatOffer && MBRandom.RandomFloat <= 0.7f)
			{
				_discountValue = (int)((float)RewardGold * 0.3f);
			}
		}

		private void QuestAcceptedConsequences()
		{
			StartQuest();
			AddTrackedObject(base.QuestGiver.CurrentSettlement);
			TextObject textObject = new TextObject("{=yc9rGlzb}Ready to deliver {.%}{?(REQUESTED_ANIMAL_AMOUNT > 1)}{PLURAL(SELECTED_ANIMAL)}{?}{SELECTED_ANIMAL}{\\?}:{.%}");
			textObject.SetTextVariable("SELECTED_ANIMAL", _requestedAnimal.Name);
			textObject.SetTextVariable("REQUESTED_ANIMAL_AMOUNT", _requestedAnimalAmount);
			_questProgressLogTest = AddDiscreteLog(QuestStartedLogText, textObject, _currentAnimalAmount, _requestedAnimalAmount);
			AddLog(_isQuestWithMeatOffer ? QuestStartedLogTextWithMeat : QuestStartedLogTextWithDenars);
		}

		protected override void SetDialogs()
		{
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(new TextObject("{=msDCQIY7}Thank you, {?PLAYER.GENDER}madam{?}sir{\\?}. [if:convo_grateful][ib:confident]In these hard times, people like you are a gift from Heaven… The village will never forget that you were willing to help. Good luck.")).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.Consequence(QuestAcceptedConsequences)
				.CloseDialog();
			TextObject textObject = new TextObject("{=rRGbn0Sm}Thank you {?PLAYER.GENDER}madam{?}sir{\\?}. [if:convo_focused_happy][ib:normal]You did a great favor to people of {ISSUE_VILLAGE}.");
			textObject.SetTextVariable("ISSUE_VILLAGE", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
			TextObject npcDiscountLine = new TextObject("{=p9rzYV4T}Things have gotten worse for the village, [if:convo_dismayed][ib:nervous2]since we last met {?PLAYER.GENDER}madam{?}sir{\\?}. Is it possible that we could pay you a bit less, what about {DISCOUNTED_REWARD}{GOLD_ICON} denars?");
			TextObject npcText = new TextObject("{=4kealpZK}Have you brought the animals {?PLAYER.GENDER}madam{?}sir{\\?}?");
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(npcText).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=CjuVoxaC}Yes, Here are your animals."))
				.Condition(CheckIfPlayerHasEnoughAnimals)
				.BeginNpcOptions()
				.NpcOption(npcDiscountLine, delegate
				{
					if (_discountValue > 0)
					{
						npcDiscountLine.SetTextVariable("DISCOUNTED_REWARD", RewardGold - _discountValue);
						npcDiscountLine.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
						return true;
					}
					return false;
				})
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=KBmAqg54}No problem, you can pay what you can afford."))
				.NpcLine(textObject)
				.Consequence(QuestSuccessPlayerDeliveredAnimalsWithAcceptingDiscount)
				.CloseDialog()
				.PlayerOption(new TextObject("{=kYc90hEl}Sorry, but the price is what we agreed on. I can't lower it."))
				.NpcLine(new TextObject("{=r4pLtP5V}You're right. We agreed on this price. Thank you for your efforts.[if:convo_nervous][ib:normal]"))
				.Consequence(QuestSuccessPlayerDeliveredAnimalsWithoutAcceptingDiscount)
				.CloseDialog()
				.EndPlayerOptions()
				.NpcOption(new TextObject("{=l8ezl95j}Thank you {?PLAYER.GENDER}madam{?}sir{\\?}.[if:convo_calm_friendly] Here is what we promised."), null)
				.Consequence(QuestSuccessPlayerDeliveredAnimalsNormal)
				.CloseDialog()
				.EndNpcOptions()
				.PlayerOption(new TextObject("{=PI6ikMsc}I'm working on it."))
				.NpcLine(new TextObject("{=4MQQf3wp}We are waiting for your arrival. "))
				.CloseDialog()
				.EndPlayerOptions();
		}

		private void CalculateRequestedAnimalCountOnPlayer()
		{
			int num = 0;
			foreach (ItemRosterElement item in MobileParty.MainParty.ItemRoster)
			{
				if (item.EquipmentElement.Item == _requestedAnimal)
				{
					num += item.Amount;
				}
			}
			_currentAnimalAmount = num;
		}

		private bool CheckIfPlayerHasEnoughAnimals()
		{
			CalculateRequestedAnimalCountOnPlayer();
			return _currentAnimalAmount >= _requestedAnimalAmount;
		}

		private void QuestSuccessPlayerDeliveredAnimalsNormal()
		{
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, 5);
			base.QuestGiver.CurrentSettlement.Village.Hearth += 30f;
			ApplyRewards(applyDiscount: false);
			CompleteQuestWithSuccess();
		}

		private void QuestSuccessPlayerDeliveredAnimalsWithAcceptingDiscount()
		{
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, 8);
			TraitLevelingHelper.OnIssueSolvedThroughBetrayal(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Mercy, 30)
			});
			base.QuestGiver.CurrentSettlement.Village.Hearth += 80f;
			ApplyRewards(applyDiscount: true);
			foreach (Hero notable in base.QuestGiver.CurrentSettlement.Notables)
			{
				if (notable != base.QuestGiver)
				{
					ChangeRelationAction.ApplyPlayerRelation(notable, 3);
				}
			}
			CompleteQuestWithSuccess();
		}

		private void QuestSuccessPlayerDeliveredAnimalsWithoutAcceptingDiscount()
		{
			TraitLevelingHelper.OnIssueSolvedThroughBetrayal(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Mercy, -20)
			});
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, 5);
			base.QuestGiver.CurrentSettlement.Village.Hearth += 50f;
			ApplyRewards(applyDiscount: false);
			CompleteQuestWithSuccess();
		}

		private void ApplyRewards(bool applyDiscount)
		{
			if (_isQuestWithMeatOffer)
			{
				ItemObject item = Game.Current.ObjectManager.GetObject<ItemObject>("meat");
				MobileParty.MainParty.ItemRoster.AddToCounts(item, _offeredMeatAmount);
				{
					foreach (Hero notable in base.QuestGiver.CurrentSettlement.Notables)
					{
						if (notable != base.QuestGiver)
						{
							ChangeRelationAction.ApplyPlayerRelation(notable, 3);
						}
					}
					return;
				}
			}
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, applyDiscount ? (RewardGold - _discountValue) : RewardGold);
		}

		protected override void OnCompleteWithSuccess()
		{
			AddLog(QuestSuccessLogText);
			MobileParty.MainParty.ItemRoster.FindIndexOfItem(_requestedAnimal);
			int num = 0;
			for (int num2 = MobileParty.MainParty.ItemRoster.Count - 1; num2 >= 0; num2--)
			{
				ItemRosterElement itemRosterElement = MobileParty.MainParty.ItemRoster[num2];
				if (itemRosterElement.EquipmentElement.Item == _requestedAnimal)
				{
					int num3 = ((itemRosterElement.Amount >= _requestedAnimalAmount) ? (_requestedAnimalAmount - num) : itemRosterElement.Amount);
					EquipmentElement rosterElement = new EquipmentElement(itemRosterElement.EquipmentElement.Item, itemRosterElement.EquipmentElement.ItemModifier);
					MobileParty.MainParty.ItemRoster.AddToCounts(rosterElement, -num3);
					num += itemRosterElement.Amount;
					if (num >= _requestedAnimalAmount)
					{
						break;
					}
				}
			}
			TraitLevelingHelper.OnIssueSolvedThroughBetrayal(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 30)
			});
			base.QuestGiver.AddPower(10f);
		}

		protected override void OnTimedOut()
		{
			AddLog(QuestFailedWithTimeOutLogText);
			base.QuestGiver.AddPower(-10f);
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -5);
			base.QuestGiver.CurrentSettlement.Village.Hearth -= 30f;
		}

		protected override void HourlyTick()
		{
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
			CampaignEvents.VillageBeingRaided.AddNonSerializedListener(this, OnVillageRaided);
			CampaignEvents.PlayerInventoryExchangeEvent.AddNonSerializedListener(this, OnPlayerInventoryExchange);
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
		}

		private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
			{
				QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
			}
		}

		private void OnPlayerInventoryExchange(List<(ItemRosterElement, int)> purchasedItems, List<(ItemRosterElement, int)> soldItems, bool isTrading)
		{
			int num = 0;
			foreach (var purchasedItem in purchasedItems)
			{
				ItemRosterElement itemRosterElement;
				(itemRosterElement, _) = purchasedItem;
				if (itemRosterElement.EquipmentElement.Item == _requestedAnimal)
				{
					int num2 = num;
					itemRosterElement = purchasedItem.Item1;
					num = num2 + itemRosterElement.Amount;
				}
			}
			foreach (var soldItem in soldItems)
			{
				ItemRosterElement itemRosterElement;
				(itemRosterElement, _) = soldItem;
				if (itemRosterElement.EquipmentElement.Item == _requestedAnimal)
				{
					int num3 = num;
					itemRosterElement = soldItem.Item1;
					num = num3 - itemRosterElement.Amount;
				}
			}
			_currentAnimalAmount += num;
			_currentAnimalAmount = (int)TaleWorlds.Library.MathF.Clamp(_currentAnimalAmount, 0f, _requestedAnimalAmount);
			_questProgressLogTest.UpdateCurrentProgress(_currentAnimalAmount);
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
			CalculateRequestedAnimalCountOnPlayer();
			SetDialogs();
		}
	}

	public class HeadmanVillageNeedsDraughtAnimalsIssueBehaviorTypeDefiner : SaveableTypeDefiner
	{
		public HeadmanVillageNeedsDraughtAnimalsIssueBehaviorTypeDefiner()
			: base(812000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(HeadmanVillageNeedsDraughtAnimalsIssue), 1);
			AddClassDefinition(typeof(HeadmanVillageNeedsDraughtAnimalsIssueQuest), 2);
		}
	}

	private const IssueBase.IssueFrequency HeadmanVillageNeedsDraughtAnimalsIssueFrequency = IssueBase.IssueFrequency.VeryCommon;

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private bool ConditionsHold(Hero issueGiver)
	{
		if (issueGiver.CurrentSettlement != null && issueGiver.IsHeadman)
		{
			Village village = issueGiver.CurrentSettlement.Village;
			if (village.GetProsperityLevel() == SettlementComponent.ProsperityLevel.Low || village.GetProsperityLevel() == SettlementComponent.ProsperityLevel.Mid)
			{
				if (village.VillageType != DefaultVillageTypes.IronMine && village.VillageType != DefaultVillageTypes.ClayMine && village.VillageType != DefaultVillageTypes.SaltMine && village.VillageType != DefaultVillageTypes.SilverMine)
				{
					return village.VillageType == DefaultVillageTypes.Lumberjack;
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
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnSelected, typeof(HeadmanVillageNeedsDraughtAnimalsIssue), IssueBase.IssueFrequency.VeryCommon));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(HeadmanVillageNeedsDraughtAnimalsIssue), IssueBase.IssueFrequency.VeryCommon));
		}
	}

	private IssueBase OnSelected(in PotentialIssueData pid, Hero issueOwner)
	{
		return new HeadmanVillageNeedsDraughtAnimalsIssue(issueOwner);
	}
}
