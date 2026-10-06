using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues;

public class VillageNeedsCraftingMaterialsIssueBehavior : CampaignBehaviorBase
{
	public class VillageNeedsCraftingMaterialsIssue : IssueBase
	{
		private const int TimeLimit = 30;

		private const int PowerChangeForQuestGiver = 10;

		private const int RelationWithIssueOwnerRewardOnSuccess = 5;

		private const int VillageHeartChangeOnAlternativeSuccess = 60;

		private const int RequiredSkillValueForAlternativeSolution = 120;

		[SaveableField(1)]
		private readonly ItemObject _requestedItem;

		[SaveableField(4)]
		private int _promisedPayment;

		private int _numberOfRequestedItem => TaleWorlds.Library.MathF.Round((float)(int)(750f / (float)_requestedItem.Value) * base.IssueDifficultyMultiplier);

		protected override int CompanionSkillRewardXP => 500 + (int)(700f * base.IssueDifficultyMultiplier);

		protected override bool IssueQuestCanBeDuplicated => false;

		public override int AlternativeSolutionBaseNeededMenCount => 4;

		protected override int AlternativeSolutionBaseDurationInDaysInternal => (int)(2f + 4f * base.IssueDifficultyMultiplier);

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=eR7P1cVA}{VILLAGE} Needs Crafting Materials");
				textObject.SetTextVariable("VILLAGE", base.IssueOwner.CurrentSettlement.Name);
				return textObject;
			}
		}

		public override TextObject Description
		{
			get
			{
				TextObject textObject = new TextObject("{=5CJrR0X3}{ISSUE_GIVER.LINK} in the village requested crafting materials for their ongoing project.");
				textObject.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject);
				return textObject;
			}
		}

		public override TextObject IssueBriefByIssueGiver => new TextObject("{=095beaQ5}Yes, there's a lot of work we need to do around the village, and we're short on the materials that our smith needs to make us tools and fittings. Do you think you could get us some? We'll pay well.[ib:demure][if:convo_dismayed]");

		public override TextObject IssueAcceptByPlayer => new TextObject("{=xmu89biL}Maybe I can help. What do you need exactly?");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=PftlaE0x}We need {REQUESTED_ITEM_COUNT} {?(REQUESTED_ITEM_COUNT > 1)}{PLURAL(REQUESTED_ITEM)}{?}{REQUESTED_ITEM}{\\?} in {NUMBER_OF_DAYS} days. We need to repair some roofs before the next big storms. I can offer {PAYMENT}{GOLD_ICON}. What do you say?");
				textObject.SetTextVariable("PAYMENT", GetPayment());
				textObject.SetTextVariable("REQUESTED_ITEM", _requestedItem.Name);
				textObject.SetTextVariable("REQUESTED_ITEM_COUNT", _numberOfRequestedItem);
				textObject.SetTextVariable("NUMBER_OF_DAYS", 30);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		public override TextObject IssuePlayerResponseAfterAlternativeExplanation => new TextObject("{=i96OaGH3}Is there anything else I could do to help?");

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver => new TextObject("{=WzdhPF7M}Well, if we had some extra skilled labor, we could probably melt down old tools and reforge them. That's too much work for just our smith by himself, but maybe he could do it with someone proficient in crafting to help him.[ib:demure2][if:convo_thinking]");

		public override TextObject IssueQuestSolutionAcceptByPlayer => new TextObject("{=WsmH9Cfd}I will provide what you need.");

		public override TextObject IssueAlternativeSolutionAcceptByPlayer => new TextObject("{=8DWTTnpP}My comrade will help your smith to produce what you need.");

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver => new TextObject("{=xlagNKZ2}Thank you. With their help, we should be able to make what we need.[if:convo_astonished]");

		public override TextObject IssueDiscussAlternativeSolution => new TextObject("{=P3Uu0Ham}Your companion is still working with our smith. I hope they will finish the order in time.[if:convo_approving]");

		public override bool IsThereAlternativeSolution => true;

		public override bool IsThereLordSolution => false;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.Duration;

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=1XuYGQcT}{ISSUE_GIVER.LINK} told you that {?QUEST_GIVER.GENDER}her{?}his{\\?} local smith needs {REQUESTED_ITEM} to forge more tools. You asked your companion {COMPANION.LINK} to help the local smith and craft {REQUESTED_ITEM_COUNT} {?(REQUESTED_ITEM_COUNT > 1)}{PLURAL(REQUESTED_ITEM)}{?}{REQUESTED_ITEM}{\\?} for the village. Your companion will rejoin your party in {RETURN_DAYS} days.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", base.IssueOwner.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("REQUESTED_ITEM", _requestedItem.Name);
				textObject.SetTextVariable("REQUESTED_ITEM_COUNT", _numberOfRequestedItem);
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=n86jgG3m}Your companion {COMPANION.LINK} has helped the local smith and produced {REQUESTED_AMOUNT} {?(REQUESTED_AMOUNT > 1)}{PLURAL(REQUESTED_GOOD)}{?}{REQUESTED_GOOD}{\\?} as you promised.");
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				textObject.SetTextVariable("REQUESTED_AMOUNT", _numberOfRequestedItem);
				textObject.SetTextVariable("REQUESTED_GOOD", _requestedItem.Name);
				return textObject;
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsVillageNeedsCraftingMaterialsIssue(object o, List<object> collectedObjects)
		{
			((VillageNeedsCraftingMaterialsIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_requestedItem);
		}

		internal static object AutoGeneratedGetMemberValue_requestedItem(object o)
		{
			return ((VillageNeedsCraftingMaterialsIssue)o)._requestedItem;
		}

		internal static object AutoGeneratedGetMemberValue_promisedPayment(object o)
		{
			return ((VillageNeedsCraftingMaterialsIssue)o)._promisedPayment;
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
			return (DefaultSkills.Crafting, 120);
		}

		public override bool AlternativeSolutionCondition(out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation);
		}

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, GetPayment());
			RelationshipChangeWithIssueOwner = 5;
			base.IssueSettlement.Village.Hearth += 60f;
			base.IssueOwner.AddPower(10f);
		}

		public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation);
		}

		protected override void OnGameLoad()
		{
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new VillageNeedsCraftingMaterialsIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(30f), GetPayment(), _requestedItem, _numberOfRequestedItem);
		}

		public override IssueFrequency GetFrequency()
		{
			return IssueFrequency.Rare;
		}

		public override void AlternativeSolutionStartConsequence()
		{
			_promisedPayment = GetPayment();
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

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}

		public override bool IssueStayAliveConditions()
		{
			if (!base.IssueOwner.CurrentSettlement.IsRaided)
			{
				return !base.IssueOwner.CurrentSettlement.IsUnderRaid;
			}
			return false;
		}

		public VillageNeedsCraftingMaterialsIssue(Hero issueOwner)
			: base(issueOwner, CampaignTime.DaysFromNow(30f))
		{
			_requestedItem = SelectCraftingMaterial();
		}

		private int GetPayment()
		{
			if (_promisedPayment != 0)
			{
				return _promisedPayment;
			}
			return 750 + (base.IssueSettlement.Village.Bound.Town.MarketData.GetPrice(_requestedItem) + QuestHelper.GetAveragePriceOfItemInTheWorld(_requestedItem) / 2) * _numberOfRequestedItem;
		}
	}

	public class VillageNeedsCraftingMaterialsIssueQuest : QuestBase
	{
		[SaveableField(10)]
		private readonly int _requestedItemAmount;

		[SaveableField(20)]
		private readonly ItemObject _requestedItem;

		[SaveableField(30)]
		private JournalLog _playerAcceptedQuestLog;

		[SaveableField(40)]
		private JournalLog _playerHasNeededItemsLog;

		private const int SuccessRelationBonus = 5;

		private const int FailRelationPenalty = -5;

		private const int SuccessPowerBonus = 10;

		private const int FailPowerPenalty = -10;

		private const int SuccessHonorBonus = 30;

		private const int FailWithCrimeHonorPenalty = -50;

		private const int SuccessHearthBonus = 30;

		private const int FailToDeliverInTimeHearthPenalty = -40;

		private TextObject QuestStartedLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=YZeKScP5}{QUEST_GIVER.LINK} told you that {?QUEST_GIVER.GENDER}her{?}his{\\?} local smith needs {REQUESTED_ITEM} to forge more tools. {?QUEST_GIVER.GENDER}She{?}He{\\?} asked you to bring {REQUESTED_ITEM_AMOUNT} {?(REQUESTED_ITEM_AMOUNT > 1)}{PLURAL(REQUESTED_ITEM)}{?}{REQUESTED_ITEM}{\\?} to {?QUEST_GIVER.GENDER}her{?}him{\\?}.");
				textObject.SetTextVariable("REQUESTED_ITEM_AMOUNT", _requestedItemAmount);
				textObject.SetTextVariable("REQUESTED_ITEM", _requestedItem.Name);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject QuestSuccessLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=LiDSTrvV}You brought {REQUESTED_ITEM_AMOUNT} {?(REQUESTED_ITEM_AMOUNT > 1)}{PLURAL(REQUESTED_ITEM)}{?}{REQUESTED_ITEM}{\\?} to {?QUEST_GIVER.GENDER}her{?}him{\\?} as promised.");
				textObject.SetTextVariable("REQUESTED_ITEM_AMOUNT", _requestedItemAmount);
				textObject.SetTextVariable("REQUESTED_ITEM", _requestedItem.Name);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject QuestCanceledWarDeclaredLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=vW6kBki9}Your clan is now at war with {QUEST_GIVER.LINK}'s realm. Your agreement with {QUEST_GIVER.LINK} is canceled.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject QuestGiverVillageRaidedLogText
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
				TextObject textObject = new TextObject("{=nmz1ky2D}You failed to deliver {REQUESTED_ITEM_AMOUNT} {?(REQUESTED_ITEM_AMOUNT > 1)}{PLURAL(REQUESTED_ITEM)}{?}{REQUESTED_ITEM}{\\?} to {QUEST_GIVER.LINK} in time.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("REQUESTED_ITEM_AMOUNT", _requestedItemAmount);
				textObject.SetTextVariable("REQUESTED_ITEM", _requestedItem.Name);
				return textObject;
			}
		}

		private TextObject PlayerHasNeededItemsLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=MxpPkytG}You now have enough {ITEM} to complete the quest. Return to {QUEST_SETTLEMENT} to hand them over.");
				textObject.SetTextVariable("ITEM", _requestedItem.Name);
				textObject.SetTextVariable("QUEST_SETTLEMENT", base.QuestGiver.CurrentSettlement.Name);
				return textObject;
			}
		}

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=LgiRMbgE}{ISSUE_SETTLEMENT} Needs Crafting Materials");
				textObject.SetTextVariable("ISSUE_SETTLEMENT", base.QuestGiver.CurrentSettlement.Name);
				return textObject;
			}
		}

		public override bool IsRemainingTimeHidden => false;

		internal static void AutoGeneratedStaticCollectObjectsVillageNeedsCraftingMaterialsIssueQuest(object o, List<object> collectedObjects)
		{
			((VillageNeedsCraftingMaterialsIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_requestedItem);
			collectedObjects.Add(_playerAcceptedQuestLog);
			collectedObjects.Add(_playerHasNeededItemsLog);
		}

		internal static object AutoGeneratedGetMemberValue_requestedItemAmount(object o)
		{
			return ((VillageNeedsCraftingMaterialsIssueQuest)o)._requestedItemAmount;
		}

		internal static object AutoGeneratedGetMemberValue_requestedItem(object o)
		{
			return ((VillageNeedsCraftingMaterialsIssueQuest)o)._requestedItem;
		}

		internal static object AutoGeneratedGetMemberValue_playerAcceptedQuestLog(object o)
		{
			return ((VillageNeedsCraftingMaterialsIssueQuest)o)._playerAcceptedQuestLog;
		}

		internal static object AutoGeneratedGetMemberValue_playerHasNeededItemsLog(object o)
		{
			return ((VillageNeedsCraftingMaterialsIssueQuest)o)._playerHasNeededItemsLog;
		}

		public VillageNeedsCraftingMaterialsIssueQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold, ItemObject requestedItem, int requestedItemAmount)
			: base(questId, questGiver, duration, rewardGold)
		{
			_requestedItem = requestedItem;
			_requestedItemAmount = requestedItemAmount;
			SetDialogs();
			InitializeQuestOnCreation();
		}

		protected override void SetDialogs()
		{
			TextObject npcText = new TextObject("{=UbUokDyI}Thank you. We'd appreciate it if you got the goods to us as quickly as possible. Good luck![ib:nervous2][if:convo_excited]");
			TextObject textObject = new TextObject("{=4c9ySfVj}Did you find what we needed, {?PLAYER.GENDER}madam{?}sir{\\?}?");
			TextObject textObject2 = new TextObject("{=nEGe8rUd}Thank you for your help, {?PLAYER.GENDER}madam{?}sir{\\?}. Here is what we promised.");
			TextObject npcText2 = new TextObject("{=sTfr1C8H}Thank you. But if the storms come before you find them, well, that would be bad for us.[ib:nervous2][if:convo_nervous]");
			textObject.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject);
			textObject2.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject);
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(npcText).Condition(() => CharacterObject.OneToOneConversationCharacter == base.QuestGiver.CharacterObject)
				.Consequence(QuestAcceptedConsequences)
				.CloseDialog();
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(textObject).Condition(() => CharacterObject.OneToOneConversationCharacter == base.QuestGiver.CharacterObject)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=bLRGix1b}Yes, I have them with me."))
				.ClickableCondition(CompleteQuestClickableConditions)
				.NpcLine(textObject2)
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += Success;
				})
				.CloseDialog()
				.PlayerOption(new TextObject("{=D8KFcE2i}Not yet, I am still working on it."))
				.NpcLine(npcText2)
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
		}

		private bool CompleteQuestClickableConditions(out TextObject explanation)
		{
			if (_playerAcceptedQuestLog.CurrentProgress >= _requestedItemAmount)
			{
				explanation = null;
				return true;
			}
			explanation = new TextObject("{=EmBla2xa}You don't have enough {ITEM}");
			explanation.SetTextVariable("ITEM", _requestedItem.Name);
			return false;
		}

		protected override void InitializeQuestOnGameLoad()
		{
			SetDialogs();
		}

		protected override void HourlyTick()
		{
		}

		private void QuestAcceptedConsequences()
		{
			StartQuest();
			int requiredItemCountOnPlayer = GetRequiredItemCountOnPlayer();
			TextObject textObject = new TextObject("{=nAEhfGJk}Collect {ITEM}");
			textObject.SetTextVariable("ITEM", _requestedItem.Name);
			_playerAcceptedQuestLog = AddDiscreteLog(QuestStartedLogText, textObject, requiredItemCountOnPlayer, _requestedItemAmount);
		}

		protected override void OnTimedOut()
		{
			Fail();
		}

		private void Success()
		{
			AddLog(QuestSuccessLogText);
			ItemRosterElement itemRosterElement = new ItemRosterElement(_requestedItem, _requestedItemAmount);
			GiveItemAction.ApplyForParties(PartyBase.MainParty, Settlement.CurrentSettlement.Party, in itemRosterElement);
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 30)
			});
			base.QuestGiver.AddPower(10f);
			RelationshipChangeWithQuestGiver = 5;
			base.QuestGiver.CurrentSettlement.Village.Hearth += 30f;
			CompleteQuestWithSuccess();
		}

		private void Fail()
		{
			AddLog(QuestFailedWithTimeOutLogText);
			base.QuestGiver.AddPower(-10f);
			RelationshipChangeWithQuestGiver = -5;
			base.QuestGiver.CurrentSettlement.Village.Hearth += -40f;
			CompleteQuestWithFail();
		}

		private int GetRequiredItemCountOnPlayer()
		{
			int itemNumber = PartyBase.MainParty.ItemRoster.GetItemNumber(_requestedItem);
			if (itemNumber >= _requestedItemAmount)
			{
				TextObject textObject = new TextObject("{=MTCrXEvj}You have enough {ITEM} to complete the quest. Return to {QUEST_SETTLEMENT} to hand it over.");
				textObject.SetTextVariable("QUEST_SETTLEMENT", base.QuestGiver.CurrentSettlement.Name);
				textObject.SetTextVariable("ITEM", _requestedItem.Name);
				MBInformationManager.AddQuickInformation(textObject);
			}
			if (itemNumber <= _requestedItemAmount)
			{
				return itemNumber;
			}
			return _requestedItemAmount;
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
			CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, OnRaidCompleted);
			CampaignEvents.PlayerInventoryExchangeEvent.AddNonSerializedListener(this, OnPlayerInventoryExchange);
			CampaignEvents.OnNewItemCraftedEvent.AddNonSerializedListener(this, OnItemCrafted);
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
			CampaignEvents.OnEquipmentSmeltedByHeroEvent.AddNonSerializedListener(this, OnEquipmentSmeltedByHero);
			CampaignEvents.OnItemsRefinedEvent.AddNonSerializedListener(this, OnItemsRefined);
		}

		private void OnItemsRefined(Hero hero, Crafting.RefiningFormula refiningFormula)
		{
			UpdateQuestLog();
		}

		private void OnEquipmentSmeltedByHero(Hero hero, EquipmentElement equipmentElement)
		{
			UpdateQuestLog();
		}

		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
		{
			QuestHelper.CheckWarDeclarationAndFailOrCancelTheQuest(this, faction1, faction2, detail, QuestCanceledWarDeclaredLogText, QuestCanceledWarDeclaredLogText);
		}

		private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
			{
				QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
			}
		}

		private void OnItemCrafted(ItemObject item, ItemModifier overriddenItemModifier, bool isCraftingOrderItem)
		{
			UpdateQuestLog();
		}

		private void OnPlayerInventoryExchange(List<(ItemRosterElement, int)> purchasedItems, List<(ItemRosterElement, int)> soldItems, bool isTrading)
		{
			UpdateQuestLog();
		}

		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (base.QuestGiver.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				CompleteQuestWithCancel(QuestCanceledWarDeclaredLogText);
			}
		}

		private void OnRaidCompleted(BattleSideEnum battleSide, RaidEventComponent mapEvent)
		{
			if (mapEvent.MapEventSettlement == base.QuestGiver.CurrentSettlement)
			{
				CompleteQuestWithCancel(QuestGiverVillageRaidedLogText);
			}
		}

		private void CheckIfPlayerReadyToReturnItems()
		{
			if (_playerHasNeededItemsLog == null && _playerAcceptedQuestLog.CurrentProgress >= _requestedItemAmount)
			{
				_playerHasNeededItemsLog = AddLog(PlayerHasNeededItemsLogText);
			}
			else if (_playerHasNeededItemsLog != null && _playerAcceptedQuestLog.CurrentProgress < _requestedItemAmount)
			{
				RemoveLog(_playerHasNeededItemsLog);
				_playerHasNeededItemsLog = null;
			}
		}

		private void UpdateQuestLog()
		{
			_playerAcceptedQuestLog.UpdateCurrentProgress(GetRequiredItemCountOnPlayer());
			CheckIfPlayerReadyToReturnItems();
		}
	}

	public class VillageNeedsCraftingMaterialsIssueTypeDefiner : SaveableTypeDefiner
	{
		public VillageNeedsCraftingMaterialsIssueTypeDefiner()
			: base(601000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(VillageNeedsCraftingMaterialsIssue), 1);
			AddClassDefinition(typeof(VillageNeedsCraftingMaterialsIssueQuest), 2);
		}
	}

	private const IssueBase.IssueFrequency VillageNeedsCraftingMaterialsIssueFrequency = IssueBase.IssueFrequency.Rare;

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
	}

	private void OnCheckForIssue(Hero hero)
	{
		Campaign.Current.IssueManager.AddPotentialIssueData(hero, ConditionsHold(hero) ? new PotentialIssueData(OnStartIssue, typeof(VillageNeedsCraftingMaterialsIssue), IssueBase.IssueFrequency.Rare) : new PotentialIssueData(typeof(VillageNeedsCraftingMaterialsIssue), IssueBase.IssueFrequency.Rare));
	}

	private bool ConditionsHold(Hero issueGiver)
	{
		if (issueGiver.IsRuralNotable)
		{
			return !issueGiver.MapFaction.IsAtWarWith(Clan.PlayerClan);
		}
		return false;
	}

	private IssueBase OnStartIssue(in PotentialIssueData pid, Hero issueOwner)
	{
		return new VillageNeedsCraftingMaterialsIssue(issueOwner);
	}

	private static ItemObject SelectCraftingMaterial()
	{
		return MBRandom.RandomInt(0, 2) switch
		{
			0 => DefaultItems.IronIngot1, 
			1 => DefaultItems.IronIngot2, 
			_ => DefaultItems.IronIngot1, 
		};
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
