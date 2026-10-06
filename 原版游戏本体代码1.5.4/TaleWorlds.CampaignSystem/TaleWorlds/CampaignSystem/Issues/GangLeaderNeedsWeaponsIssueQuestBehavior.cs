using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues;

public class GangLeaderNeedsWeaponsIssueQuestBehavior : CampaignBehaviorBase
{
	public class GangLeaderNeedsWeaponsIssue : IssueBase
	{
		private const int BaseNeededWeaponCount = 3;

		private const int BaseRewardGold = 500;

		private const int AlternativeSolutionTroopTierRequirement = 2;

		private const int RequiredSkillLevelForSendingComp = 120;

		private const int IssueDuration = 15;

		private const int QuestTimeLimit = 25;

		[SaveableField(20)]
		private int _requiredWeaponClassIndex;

		[SaveableField(30)]
		private int _averagePriceForItem;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.Duration;

		private int RequestedWeaponAmount
		{
			get
			{
				float num = 0f;
				num = ((base.IssueDifficultyMultiplier >= 0.1f && base.IssueDifficultyMultiplier < 0.3f) ? 0.1f : ((!(base.IssueDifficultyMultiplier >= 0.3f) || !(base.IssueDifficultyMultiplier < 0.6f)) ? 0.3f : 0.2f));
				if (3 + _averagePriceForItem == 0)
				{
					return 0;
				}
				return (int)((float)(20000 / _averagePriceForItem) * num);
			}
		}

		private int CompanionGoldNeedForAlternativeSolution => RewardGold / 2 + _averagePriceForItem * RequestedWeaponAmount / 4;

		public override int AlternativeSolutionBaseNeededMenCount => 2 + TaleWorlds.Library.MathF.Ceiling(4f * base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 7 + TaleWorlds.Library.MathF.Ceiling(8f * base.IssueDifficultyMultiplier);

		protected override int RewardGold => 500 + _averagePriceForItem * RequestedWeaponAmount;

		private WeaponClass RequestedWeaponClass => _canBeRequestedWeaponClassList[_requiredWeaponClassIndex];

		public override TextObject IssueBriefByIssueGiver => new TextObject("{=m24bXmOD}Yes, you can help me. Do you want to make some easy money? I need some 'tools' for my private business. Are you interested?[if:convo_bored][ib:confident2]");

		public override TextObject IssueAcceptByPlayer => new TextObject("{=PFNXodyo}What sort of tools?");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=xBaL3RM4}Well, as you know we're not farmers or artisans. I need {NEEDED_AMOUNT} {.%}{NEEDED_TYPE}{.%}. Don't mind the quality, just buy the weapons. Bring them to me and {REWARD_GOLD}{GOLD_ICON} is yours. Got it?[if:convo_bored]");
				textObject.SetTextVariable("NEEDED_AMOUNT", RequestedWeaponAmount);
				textObject.SetTextVariable("REWARD_GOLD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				textObject.SetTextVariable("NEEDED_TYPE", GetNeededClassTextObject(RequestedWeaponClass));
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=0i8RgslK}Or, maybe one of your trusted companions can buy the {ITEM_TYPE}. I reckon they'll cost {COMPANION_NEED_GOLD_AMOUNT}{GOLD_ICON} denars, and they'd best take at least {ALTERNATIVE_TROOP_AMOUNT} men for protection.[if:convo_bored]");
				textObject.SetTextVariable("COMPANION_NEED_GOLD_AMOUNT", CompanionGoldNeedForAlternativeSolution);
				textObject.SetTextVariable("ITEM_TYPE", GetNeededClassTextObject(RequestedWeaponClass));
				textObject.SetTextVariable("ALTERNATIVE_TROOP_AMOUNT", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		public override TextObject IssueQuestSolutionAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=ABYGws1M}I'll bring you {NEEDED_AMOUNT} of those by myself.");
				textObject.SetTextVariable("NEEDED_AMOUNT", RequestedWeaponAmount);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionAcceptByPlayer => new TextObject("{=Ggjb9BVQ}Actually I prefer not to get involved in this kind of business personally but my men will help you out.");

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver => new TextObject("{=HawJormK}That's fine, so long as your men know well enough to handle this quietly. Good luck.");

		public override TextObject IssueDiscussAlternativeSolution => new TextObject("{=aG9iX1a8}We're looking forward to trying out our new \"tools\" on some bastard's head. Let your boys know that drinks are on us when they get here.");

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=otwfa0K3}{QUEST_GIVER.LINK}, a gang leader from {SETTLEMENT} wanted you to buy some weapons for {?QUEST_GIVER.GENDER}her{?}his{\\?} private business. {?QUEST_GIVER.GENDER}She{?}He{\\?} offers you {REWARD_GOLD}{GOLD_ICON} on their delivery. You asked {COMPANION.LINK} to take some of your men and buy {NEEDED_AMOUNT} units of {NEEDED_TYPE} to deliver to {QUEST_GIVER.LINK} in {SETTLEMENT}. They should rejoin your party in {RETURN_DAYS} days.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", base.IssueOwner.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				textObject.SetTextVariable("NEEDED_AMOUNT", RequestedWeaponAmount);
				textObject.SetTextVariable("NEEDED_TYPE", GetNeededClassTextObject(RequestedWeaponClass));
				textObject.SetTextVariable("REWARD_GOLD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		public override bool IsThereAlternativeSolution => true;

		public override bool IsThereLordSolution => false;

		public override TextObject Title => new TextObject("{=zKHkS5Gf}Gang Leader Needs Weapons");

		public override TextObject Description
		{
			get
			{
				TextObject textObject = new TextObject("{=VYq93e36}A gang leader needs you to buy weapons for {?QUEST_GIVER.GENDER}her{?}his{\\?} men.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		protected override int CompanionSkillRewardXP => (int)(800f + 900f * base.IssueDifficultyMultiplier);

		internal static void AutoGeneratedStaticCollectObjectsGangLeaderNeedsWeaponsIssue(object o, List<object> collectedObjects)
		{
			((GangLeaderNeedsWeaponsIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		internal static object AutoGeneratedGetMemberValue_requiredWeaponClassIndex(object o)
		{
			return ((GangLeaderNeedsWeaponsIssue)o)._requiredWeaponClassIndex;
		}

		internal static object AutoGeneratedGetMemberValue_averagePriceForItem(object o)
		{
			return ((GangLeaderNeedsWeaponsIssue)o)._averagePriceForItem;
		}

		public GangLeaderNeedsWeaponsIssue(Hero issueOwner)
			: base(issueOwner, CampaignTime.DaysFromNow(15f))
		{
			MBList<(WeaponClass, int)> mBList = new MBList<(WeaponClass, int)>();
			WeaponClass[] canBeRequestedWeaponClassList = _canBeRequestedWeaponClassList;
			foreach (WeaponClass weaponClass in canBeRequestedWeaponClassList)
			{
				int num = CalculateAveragePriceForWeaponClass(weaponClass);
				if (num > 0)
				{
					mBList.Add((weaponClass, num));
				}
			}
			(WeaponClass, int) selectedItemAndPrize = mBList.GetRandomElement();
			_averagePriceForItem = selectedItemAndPrize.Item2;
			_requiredWeaponClassIndex = Array.FindIndex(_canBeRequestedWeaponClassList, (WeaponClass x) => x == selectedItemAndPrize.Item1);
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.SettlementSecurity)
			{
				return 1f;
			}
			if (issueEffect == DefaultIssueEffects.IssueOwnerPower)
			{
				return -0.2f;
			}
			return 0f;
		}

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			return ((hero.GetSkillValue(DefaultSkills.Trade) >= hero.GetSkillValue(DefaultSkills.Crafting)) ? DefaultSkills.Trade : DefaultSkills.Crafting, 120);
		}

		private int CalculateAveragePriceForWeaponClass(WeaponClass weaponClass)
		{
			int num = 0;
			int num2 = 0;
			foreach (Settlement item in Settlement.All)
			{
				if (!item.IsTown)
				{
					continue;
				}
				for (int i = 0; i < item.ItemRoster.Count; i++)
				{
					ItemRosterElement itemRosterElement = item.ItemRoster[i];
					WeaponComponent weaponComponent = itemRosterElement.EquipmentElement.Item.WeaponComponent;
					if (weaponComponent != null && weaponComponent.PrimaryWeapon.WeaponClass == weaponClass)
					{
						num2 += itemRosterElement.Amount;
						num += itemRosterElement.EquipmentElement.ItemValue;
					}
				}
			}
			return num / ((num2 == 0) ? 1 : num2);
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}

		public override bool AlternativeSolutionCondition(out TextObject explanation)
		{
			if (QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2))
			{
				return QuestHelper.CheckGoldForAlternativeSolution(CompanionGoldNeedForAlternativeSolution, out explanation);
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
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, CompanionGoldNeedForAlternativeSolution);
		}

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			ChangeCrimeRatingAction.Apply(base.IssueOwner.CurrentSettlement.MapFaction, 10f);
			base.IssueOwner.AddPower(10f);
			RelationshipChangeWithIssueOwner = 5;
			base.IssueOwner.CurrentSettlement.Town.Security -= 30f;
		}

		public override IssueFrequency GetFrequency()
		{
			return IssueFrequency.Common;
		}

		protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out PreconditionFlags flag, out Hero relationHero, out SkillObject skill, out int requiredGold)
		{
			flag = PreconditionFlags.None;
			skill = null;
			requiredGold = 0;
			relationHero = null;
			if (issueGiver.GetRelationWithPlayer() < -10f)
			{
				flag |= PreconditionFlags.Relation;
				relationHero = issueGiver;
			}
			if (base.IssueOwner.CurrentSettlement.OwnerClan == Clan.PlayerClan)
			{
				flag |= PreconditionFlags.PlayerIsOwnerOfSettlement;
			}
			return flag == PreconditionFlags.None;
		}

		public override bool IssueStayAliveConditions()
		{
			if (base.IssueOwner.CurrentSettlement?.Town != null && base.IssueOwner.CurrentSettlement.OwnerClan != Clan.PlayerClan)
			{
				return base.IssueOwner.CurrentSettlement.Town.Loyalty < 75f;
			}
			return false;
		}

		protected override void OnGameLoad()
		{
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new GangLeaderNeedsWeaponsIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(25f), RewardGold, _requiredWeaponClassIndex, RequestedWeaponAmount, base.IssueDifficultyMultiplier, _averagePriceForItem);
		}
	}

	public class GangLeaderNeedsWeaponsIssueQuest : QuestBase
	{
		private const int LowCrimeRatingValue = 30;

		private const int HighCrimeRatingValue = 60;

		private WeaponClass _requestedWeaponClass;

		[SaveableField(10)]
		private int _randomForRequiredWeaponClass;

		[SaveableField(20)]
		private int _requestedWeaponAmount;

		[SaveableField(30)]
		private bool _playerDodgedGuards;

		[SaveableField(40)]
		private int _collectedItemAmount;

		[SaveableField(50)]
		private bool _lowCrimeRatingWillBeApplied;

		[SaveableField(60)]
		private bool _highCrimeRatingWillBeApplied;

		[SaveableField(71)]
		private Dictionary<EquipmentElement, int> _weaponsThatGuardTook;

		[SaveableField(80)]
		private float _issueDifficulty;

		[SaveableField(90)]
		private float _rewardGold;

		[SaveableField(100)]
		private int _bribeGold;

		[SaveableField(101)]
		private bool _persuasionTriedOnce;

		private bool _checkForBattleResult;

		private MobileParty _guardsParty;

		private bool _startBattleMission;

		private bool _playerGoBack;

		private PersuasionTask _task;

		private const PersuasionDifficulty Difficulty = PersuasionDifficulty.VeryHard;

		[SaveableField(110)]
		private JournalLog _playerStartsQuestLog;

		public override TextObject Title => new TextObject("{=zKHkS5Gf}Gang Leader Needs Weapons");

		public override bool IsRemainingTimeHidden => false;

		private TextObject PlayerStartsQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=B3WyJjBx}{QUEST_GIVER.LINK}, a gang leader from {SETTLEMENT}, asked you to buy some weapons for {?QUEST_GIVER.GENDER}her{?}his{\\?} private business. {?QUEST_GIVER.GENDER}She{?}He{\\?} offered you {REWARD_GOLD}{GOLD_ICON} for their delivery. {?QUEST_GIVER.GENDER}She{?}He{\\?} asked you to buy {NEEDED_AMOUNT} {NEEDED_TYPE} and deliver them to {SETTLEMENT} where {QUEST_GIVER.LINK}'s men will be waiting for you. {?QUEST_GIVER.GENDER}She{?}He{\\?} promised to give you {REWARD_GOLD}{GOLD_ICON} for your troubles.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("NEEDED_AMOUNT", _requestedWeaponAmount);
				textObject.SetTextVariable("NEEDED_TYPE", GetNeededClassTextObject(_requestedWeaponClass));
				textObject.SetTextVariable("REWARD_GOLD", _rewardGold);
				GameTexts.SetVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		private TextObject SuccessQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=R6V2Jv3a}You have delivered the weapons to the {QUEST_GIVER.LINK} as promised.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject FailPlayerDefeatedAgainstGuardsLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=XrxCaoT2}You have failed to deliver the weapons to the {QUEST_GIVER.LINK}.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject FailQuestTimedOutLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=4n71Xoyq}You have failed to bring the weapons to the {QUEST_GIVER.LINK} in time.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject OwnerOfQuestSettlementIsPlayerClanLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=Txtwv90o}Your clan has conquered the town into which you are trying to smuggle weapons. As the {?PLAYER.GENDER}lady{?}lord{\\?} of the town you cannot get involved in this kind of activity. Your agreement with the {QUEST_GIVER.LINK} has canceled.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
				return textObject;
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsGangLeaderNeedsWeaponsIssueQuest(object o, List<object> collectedObjects)
		{
			((GangLeaderNeedsWeaponsIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_weaponsThatGuardTook);
			collectedObjects.Add(_playerStartsQuestLog);
		}

		internal static object AutoGeneratedGetMemberValue_randomForRequiredWeaponClass(object o)
		{
			return ((GangLeaderNeedsWeaponsIssueQuest)o)._randomForRequiredWeaponClass;
		}

		internal static object AutoGeneratedGetMemberValue_requestedWeaponAmount(object o)
		{
			return ((GangLeaderNeedsWeaponsIssueQuest)o)._requestedWeaponAmount;
		}

		internal static object AutoGeneratedGetMemberValue_playerDodgedGuards(object o)
		{
			return ((GangLeaderNeedsWeaponsIssueQuest)o)._playerDodgedGuards;
		}

		internal static object AutoGeneratedGetMemberValue_collectedItemAmount(object o)
		{
			return ((GangLeaderNeedsWeaponsIssueQuest)o)._collectedItemAmount;
		}

		internal static object AutoGeneratedGetMemberValue_lowCrimeRatingWillBeApplied(object o)
		{
			return ((GangLeaderNeedsWeaponsIssueQuest)o)._lowCrimeRatingWillBeApplied;
		}

		internal static object AutoGeneratedGetMemberValue_highCrimeRatingWillBeApplied(object o)
		{
			return ((GangLeaderNeedsWeaponsIssueQuest)o)._highCrimeRatingWillBeApplied;
		}

		internal static object AutoGeneratedGetMemberValue_weaponsThatGuardTook(object o)
		{
			return ((GangLeaderNeedsWeaponsIssueQuest)o)._weaponsThatGuardTook;
		}

		internal static object AutoGeneratedGetMemberValue_issueDifficulty(object o)
		{
			return ((GangLeaderNeedsWeaponsIssueQuest)o)._issueDifficulty;
		}

		internal static object AutoGeneratedGetMemberValue_rewardGold(object o)
		{
			return ((GangLeaderNeedsWeaponsIssueQuest)o)._rewardGold;
		}

		internal static object AutoGeneratedGetMemberValue_bribeGold(object o)
		{
			return ((GangLeaderNeedsWeaponsIssueQuest)o)._bribeGold;
		}

		internal static object AutoGeneratedGetMemberValue_persuasionTriedOnce(object o)
		{
			return ((GangLeaderNeedsWeaponsIssueQuest)o)._persuasionTriedOnce;
		}

		internal static object AutoGeneratedGetMemberValue_playerStartsQuestLog(object o)
		{
			return ((GangLeaderNeedsWeaponsIssueQuest)o)._playerStartsQuestLog;
		}

		public GangLeaderNeedsWeaponsIssueQuest(string questId, Hero questGiver, CampaignTime dueTime, int rewardGold, int randomForRequiredWeaponClass, int requestedWeaponAmount, float issueDifficulty, int averagePrice)
			: base(questId, questGiver, dueTime, rewardGold)
		{
			_randomForRequiredWeaponClass = randomForRequiredWeaponClass;
			_requestedWeaponClass = _canBeRequestedWeaponClassList[_randomForRequiredWeaponClass];
			_requestedWeaponAmount = requestedWeaponAmount;
			_weaponsThatGuardTook = new Dictionary<EquipmentElement, int>();
			_issueDifficulty = issueDifficulty;
			_rewardGold = rewardGold;
			_bribeGold = averagePrice * requestedWeaponAmount / 4;
			SetDialogs();
			InitializeQuestOnCreation();
		}

		protected override void InitializeQuestOnGameLoad()
		{
			SetDialogs();
			_requestedWeaponClass = _canBeRequestedWeaponClassList[_randomForRequiredWeaponClass];
		}

		protected override void HourlyTick()
		{
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEnter);
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
			CampaignEvents.PlayerInventoryExchangeEvent.AddNonSerializedListener(this, OnPlayerInventoryChanged);
			CampaignEvents.OnNewItemCraftedEvent.AddNonSerializedListener(this, OnItemCrafted);
			CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
			CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
			CampaignEvents.OnEquipmentSmeltedByHeroEvent.AddNonSerializedListener(this, OnEquipmentSmeltedByHero);
		}

		protected override void OnBeforeTimedOut(ref bool completeWithSuccess, ref bool doNotResolveTheQuest)
		{
			GiveBackPlayersWeaponsOnCancelOrTimeOut();
		}

		public override void OnCanceled()
		{
			GiveBackPlayersWeaponsOnCancelOrTimeOut();
		}

		private void GiveBackPlayersWeaponsOnCancelOrTimeOut()
		{
			if (_weaponsThatGuardTook.Count <= 0)
			{
				return;
			}
			foreach (KeyValuePair<EquipmentElement, int> item in _weaponsThatGuardTook)
			{
				PartyBase.MainParty.ItemRoster.AddToCounts(item.Key, item.Value);
			}
		}

		private void OnEquipmentSmeltedByHero(Hero hero, EquipmentElement equipmentElement)
		{
			if (hero.PartyBelongedTo == MobileParty.MainParty)
			{
				ItemObject item = equipmentElement.Item;
				if (item.WeaponComponent != null && item.WeaponComponent.PrimaryWeapon.WeaponClass == _requestedWeaponClass)
				{
					SetCurrentItemAmount(_collectedItemAmount - 1);
				}
			}
		}

		private void OnItemCrafted(ItemObject itemObject, ItemModifier overriddenItemModifier, bool isCraftingOrderItem)
		{
			if (!isCraftingOrderItem && itemObject.WeaponComponent != null && itemObject.WeaponComponent.PrimaryWeapon.WeaponClass == _requestedWeaponClass)
			{
				SetCurrentItemAmount(_collectedItemAmount + 1);
			}
		}

		private void SetCurrentItemAmount(int value)
		{
			_collectedItemAmount = value;
			_playerStartsQuestLog?.UpdateCurrentProgress(_collectedItemAmount);
		}

		private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			if (settlement == base.QuestGiver.CurrentSettlement && newOwner == Hero.MainHero)
			{
				AddLog(OwnerOfQuestSettlementIsPlayerClanLogText);
				CompleteQuestWithCancel();
			}
		}

		private void OnGameMenuOpened(MenuCallbackArgs args)
		{
			if (!(args.MenuContext.GameMenu.StringId == "town") || Campaign.Current.GameMenuManager.NextLocation != null || !(GameStateManager.Current.ActiveState is MapState))
			{
				return;
			}
			if (_checkForBattleResult && PlayerEncounter.Battle != null && PlayerEncounter.Battle.InvolvedParties.Any((PartyBase x) => x == _guardsParty.Party))
			{
				bool num = PlayerEncounter.Battle.WinningSide == PlayerEncounter.Battle.PlayerSide;
				_checkForBattleResult = false;
				PlayerEncounter.Finish();
				if (num)
				{
					PlayerDodgedGuards();
				}
				else
				{
					PlayerDefeatedAgainstGuards();
				}
			}
			if (_startBattleMission)
			{
				_startBattleMission = false;
				StartFight();
			}
			if (_playerGoBack)
			{
				PlayerEncounter.LeaveEncounter = true;
				PlayerEncounter.Finish();
				_playerGoBack = false;
			}
		}

		private void OnPlayerInventoryChanged(List<(ItemRosterElement, int)> purchasedItems, List<(ItemRosterElement, int)> soldItems, bool isTrading)
		{
			CalculateAndSetRequestedItemCountOnPlayer();
		}

		private void OnSettlementLeft(MobileParty party, Settlement settlement)
		{
			if (settlement != base.QuestGiver.CurrentSettlement || party != MobileParty.MainParty)
			{
				return;
			}
			if (_weaponsThatGuardTook.Count > 0)
			{
				int num = 1500;
				bool flag = false;
				foreach (KeyValuePair<EquipmentElement, int> item in _weaponsThatGuardTook.OrderBy((KeyValuePair<EquipmentElement, int> x) => x.Key.Item.Value).ToList())
				{
					int num2 = 0;
					for (int num3 = 0; num3 < item.Value; num3++)
					{
						if (num <= 0)
						{
							break;
						}
						if ((double)MBRandom.RandomFloat >= 0.6)
						{
							num2++;
							num -= item.Key.Item.Value;
							flag = true;
						}
					}
					if (item.Value > num2)
					{
						PartyBase.MainParty.ItemRoster.AddToCounts(item.Key, item.Value - num2);
					}
				}
				if (flag)
				{
					MBInformationManager.AddQuickInformation(new TextObject("{=Ibm1A68t}The guards gave your weapons back to you but you noticed something missing."));
				}
				else
				{
					MBInformationManager.AddQuickInformation(new TextObject("{=isGIZ18i}The guards gave all your weapons back to you."));
				}
				_weaponsThatGuardTook.Clear();
				CalculateAndSetRequestedItemCountOnPlayer();
			}
			if (_guardsParty != null)
			{
				EnterSettlementAction.ApplyForParty(_guardsParty, base.QuestGiver.CurrentSettlement);
				_guardsParty.IsVisible = false;
			}
		}

		private void OnSettlementEnter(MobileParty party, Settlement settlement, Hero hero)
		{
			if (_playerDodgedGuards || party != MobileParty.MainParty || settlement != base.QuestGiver.CurrentSettlement || MobileParty.MainParty.Army != null || Campaign.Current.GameMenuManager.NextLocation != null || !(GameStateManager.Current.ActiveState is MapState) || PlayerEncounter.EncounterSettlement == null)
			{
				return;
			}
			CalculateAndSetRequestedItemCountOnPlayer();
			if (_collectedItemAmount >= _requestedWeaponAmount / 3 && !_playerGoBack)
			{
				if (_guardsParty == null)
				{
					CreateGuardsParty();
				}
				CampaignMapConversation.OpenConversation(conversationPartnerData: new ConversationCharacterData(ConversationHelper.GetConversationCharacterPartyLeader(_guardsParty.Party), null, noHorse: false, noWeapon: false, spawnAfterFight: false, isCivilianEquipmentRequiredForLeader: true), playerCharacterData: new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty));
			}
		}

		private void CreateGuardsParty()
		{
			TextObject name = new TextObject("{=7aaAWc01}Guard's Party");
			_guardsParty = CustomPartyComponent.CreateCustomPartyWithTroopRoster(base.QuestGiver.CurrentSettlement.GatePosition, 1f, base.QuestGiver.CurrentSettlement, name, base.QuestGiver.CurrentSettlement.OwnerClan, TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), base.QuestGiver.CurrentSettlement.OwnerClan.Leader);
			_guardsParty.IsVisible = false;
			_guardsParty.SetPartyUsedByQuest(isActivelyUsed: true);
			_guardsParty.Ai.DisableAi();
			CharacterObject character = CharacterObject.All.First((CharacterObject x) => x.StringId == "guard_" + _guardsParty.HomeSettlement.Culture.StringId);
			_guardsParty.MemberRoster.AddToCounts(character, 1, insertAtFront: true);
			float num = 5f + 15f * _issueDifficulty;
			_guardsParty.MemberRoster.AddToCounts(_guardsParty.HomeSettlement.Culture.MeleeMilitiaTroop, (int)num);
			EnterSettlementAction.ApplyForParty(_guardsParty, base.QuestGiver.CurrentSettlement);
		}

		protected override void SetDialogs()
		{
			Campaign.Current.ConversationManager.AddDialogFlow(GetGuardDialogFlow(), this);
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(new TextObject("{=nQoAsBZY}When you enter the town my men will take the weapons from you. Good luck.")).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.Consequence(QuestAcceptedConsequences)
				.CloseDialog();
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(new TextObject("{=CQH7E6Gr}What about my weapons?[if:convo_confused_normal][ib:hip]")).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=RbToDs0n}Here is your cargo. Now it's time for payment."))
				.Condition(CheckIfPlayerHasEnoughRequestedWeapons)
				.NpcLine(new TextObject("{=nixINYwE}Yes of course. It was a pleasure doing business with you.[if:convo_mocking_aristocratic]"))
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += PlayerSuccessfullyDeliveredWeapons;
				})
				.CloseDialog()
				.PlayerOption(new TextObject("{=bj49Bq15}It's not that easy to find what you wanted. Be patient, please."))
				.NpcLine(new TextObject("{=avFXbBLV}I know how it is. Just bring me the weapons.[if:convo_bored2]"))
				.EndPlayerOptions()
				.CloseDialog();
		}

		private bool CheckIfPlayerHasEnoughRequestedWeapons()
		{
			CalculateAndSetRequestedItemCountOnPlayer();
			return _collectedItemAmount >= _requestedWeaponAmount;
		}

		private void CalculateAndSetRequestedItemCountOnPlayer()
		{
			int num = 0;
			foreach (ItemRosterElement item in PartyBase.MainParty.ItemRoster)
			{
				if (item.EquipmentElement.Item != null && item.EquipmentElement.Item.WeaponComponent != null && item.EquipmentElement.Item.WeaponComponent.PrimaryWeapon.WeaponClass == _requestedWeaponClass)
				{
					num += item.Amount;
				}
			}
			SetCurrentItemAmount(num);
		}

		private void QuestAcceptedConsequences()
		{
			StartQuest();
			_playerStartsQuestLog = AddDiscreteLog(PlayerStartsQuestLogText, new TextObject("{=9j7LJk60}Collected Items"), _collectedItemAmount, _requestedWeaponAmount);
			CalculateAndSetRequestedItemCountOnPlayer();
		}

		private DialogFlow GetGuardDialogFlow()
		{
			TextObject npcText = new TextObject("{=wBBidWVw}What have we here? You can't enter the town with so many weapons. Hand them over! You can retrieve them when you leave.[if:convo_thinking][ib:closed2]");
			TextObject text = new TextObject("{=oVAtPtsu}Clear the way! We don't want to use force!");
			TextObject text2 = new TextObject("{=JL204Kc0}Sure... sure. We'll hand them over..");
			TextObject text3 = new TextObject("{=nlCa3tW8}You seem like a reasonable man. What is your price?");
			TextObject text4 = new TextObject("{=VUjaLmIH}Relax. We have permission. Let us pass. ");
			TextObject text5 = new TextObject("{=tGFgar0U}Fine. We won't enter at all, then. Good bye.");
			TextObject textObject = new TextObject("{=Qb7N6txQ}Mmm. For {BRIBE_COST}{GOLD_ICON} denars, I could be persuaded that this is just harmless scrap metal...[if:convo_mocking_aristocratic][ib:confident]");
			textObject.SetTextVariable("BRIBE_COST", _bribeGold);
			textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
			DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 125).NpcLine(npcText).Condition(DialogStartCondition)
				.Consequence(delegate
				{
					_task = GetPersuasionTask();
				})
				.BeginPlayerOptions()
				.PlayerOption(text)
				.ClickableCondition(CheckPlayerHealth)
				.BeginNpcOptions()
				.NpcOption(new TextObject("{=NcQZz4N2}This is my last warning! It would be better for both sides if you don't resist.[if:convo_grave][ib:closed]"), CheckPlayersPartySize)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=NTlbbrwB}If there will be a fight, then we will fight"))
				.NpcLine(new TextObject("{=h5np5kcC}All right, all right... We don't want any trouble here, okay? Go on.[if:convo_contemptuous]"))
				.Consequence(PlayerDodgeGuardsLowCrimeRating)
				.CloseDialog()
				.PlayerOption(new TextObject("{=pfxG5Ubu}Fine, fine. Take our weapons."))
				.NpcLine(new TextObject("{=A8lvIDVw}Wise decision! Now move along![if:convo_angry] "))
				.Consequence(DeleteAllWeaponsFromPlayer)
				.CloseDialog()
				.EndPlayerOptions()
				.NpcOption(new TextObject("{=G632MneX}This is my last warning! Hand over your weapons![if:convo_thinking][ib:closed]"), null)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=NTlbbrwB}If there will be a fight, then we will fight"))
				.NpcLine(new TextObject("{=8WaoQpYn}Oh there will be one, all right.[if:convo_predatory][ib:warrior]"))
				.Consequence(delegate
				{
					_startBattleMission = true;
				})
				.CloseDialog()
				.PlayerOption(new TextObject("{=pfxG5Ubu}Fine, fine. Take our weapons."))
				.NpcLine(new TextObject("{=3xyd2Dxu}Good. That saves us all trouble.[if:convo_grave][ib:warrior2]"))
				.Consequence(DeleteAllWeaponsFromPlayer)
				.CloseDialog()
				.EndPlayerOptions()
				.EndNpcOptions()
				.PlayerOption(text2)
				.NpcLine(new TextObject("{=GyI86plp}Don't worry. I guarantee your property will be returned, if everything's in order like you say...[if:convo_calm_friendly][ib:closed]"))
				.Consequence(DeleteAllWeaponsFromPlayer)
				.CloseDialog()
				.PlayerOption(text3)
				.NpcLine(textObject)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=Obk7j3ai}Here it is. Now let us pass"))
				.ClickableCondition(HasPlayerEnoughMoneyToBribe)
				.Consequence(PlayerBribeGuard)
				.NpcLine(new TextObject("{=musbt5Hm}Sorry for the interruption. Go on please...[if:convo_bored2]"))
				.CloseDialog()
				.PlayerOption(new TextObject("{=d5ztuP3P}That's too much."))
				.NpcLine(new TextObject("{=49IwOe5E}As you wish...[if:convo_normal]"))
				.GotoDialogState("start")
				.EndPlayerOptions()
				.PlayerOption(text4)
				.Condition(() => !_persuasionTriedOnce)
				.Consequence(delegate
				{
					_persuasionTriedOnce = true;
				})
				.GotoDialogState("start_guard_persuasion")
				.PlayerOption(text5)
				.NpcLine(new TextObject("{=xvYDbEUa}All right. Go on.[if:convo_bored]"))
				.Consequence(delegate
				{
					_playerGoBack = true;
				})
				.CloseDialog()
				.EndPlayerOptions();
			AddPersuasionDialogs(dialogFlow);
			return dialogFlow;
		}

		private bool CheckPlayerHealth(out TextObject explanation)
		{
			if (Hero.MainHero.IsWounded)
			{
				explanation = new TextObject("{=yNMrF2QF}You are wounded");
				return false;
			}
			explanation = null;
			return true;
		}

		private void AddPersuasionDialogs(DialogFlow dialog)
		{
			dialog.AddDialogLine("guard_persuation_weapon_smuggling_check_accepted", "start_guard_persuasion", "guard_persuation_weapon_smuggling_start_reservation", "{=v5tPWFFu}Then, what is the purpose of this?", persuasion_start_with_guards_on_condition, persuasion_start_with_guards_on_consequence, this);
			dialog.AddDialogLine("guard_persuation_weapon_smuggling_rejected", "guard_persuation_weapon_smuggling_start_reservation", "start", "{=!}{FAILED_PERSUASION_LINE}", persuasion_failed_with_guards_on_condition, persuasion_rejected_with_guards_on_consequence, this);
			dialog.AddDialogLine("guard_persuation_weapon_smuggling_attempt", "guard_persuation_weapon_smuggling_start_reservation", "guard_persuation_weapon_smuggling_select_option", "{=D9SS2Oh0}I'm going to need you to tell me a little more.", () => !persuasion_failed_with_guards_on_condition(), null, this);
			dialog.AddDialogLine("guard_persuation_weapon_smuggling_success", "guard_persuation_weapon_smuggling_start_reservation", "close_window", "{=kD8yLgRv}Go on. But I have my eye on you.", ConversationManager.GetPersuasionProgressSatisfied, persuasion_complete_with_guards_on_consequence, this, 200);
			ConversationSentence.OnConditionDelegate conditionDelegate = guard_persuation_weapon_smuggling_select_option_1_on_condition;
			ConversationSentence.OnConsequenceDelegate consequenceDelegate = guard_persuation_weapon_smuggling_select_option_1_on_consequence;
			ConversationSentence.OnPersuasionOptionDelegate persuasionOptionDelegate = guard_persuation_weapon_smuggling_setup_option_1;
			ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = guard_persuation_weapon_smuggling_clickable_option_1_on_condition;
			dialog.AddPlayerLine("guard_persuation_weapon_smuggling_select_option_1", "guard_persuation_weapon_smuggling_select_option", "guard_persuation_weapon_smuggling_selected_option_response", "{=!}{GUARDS_PERSUADE_ATTEMPT_1}", conditionDelegate, consequenceDelegate, this, 100, clickableConditionDelegate, persuasionOptionDelegate);
			ConversationSentence.OnConditionDelegate conditionDelegate2 = guard_persuation_weapon_smuggling_select_option_2_on_condition;
			ConversationSentence.OnConsequenceDelegate consequenceDelegate2 = guard_persuation_weapon_smuggling_select_option_2_on_consequence;
			persuasionOptionDelegate = guard_persuation_weapon_smuggling_setup_option_2;
			clickableConditionDelegate = guard_persuation_weapon_smuggling_clickable_option_2_on_condition;
			dialog.AddPlayerLine("guard_persuation_weapon_smuggling_select_option_2", "guard_persuation_weapon_smuggling_select_option", "guard_persuation_weapon_smuggling_selected_option_response", "{=!}{GUARDS_PERSUADE_ATTEMPT_2}", conditionDelegate2, consequenceDelegate2, this, 100, clickableConditionDelegate, persuasionOptionDelegate);
			ConversationSentence.OnConditionDelegate conditionDelegate3 = guard_persuation_weapon_smuggling_select_option_3_on_condition;
			ConversationSentence.OnConsequenceDelegate consequenceDelegate3 = guard_persuation_weapon_smuggling_select_option_3_on_consequence;
			persuasionOptionDelegate = guard_persuation_weapon_smuggling_setup_option_3;
			clickableConditionDelegate = guard_persuation_weapon_smuggling_clickable_option_3_on_condition;
			dialog.AddPlayerLine("guard_persuation_weapon_smuggling_select_option_3", "guard_persuation_weapon_smuggling_select_option", "guard_persuation_weapon_smuggling_selected_option_response", "{=!}{GUARDS_PERSUADE_ATTEMPT_3}", conditionDelegate3, consequenceDelegate3, this, 100, clickableConditionDelegate, persuasionOptionDelegate);
			dialog.AddDialogLine("guard_persuation_weapon_smuggling_select_option_reaction", "guard_persuation_weapon_smuggling_selected_option_response", "guard_persuation_weapon_smuggling_start_reservation", "{=!}{PERSUASION_REACTION}", guard_persuation_weapon_smuggling_selected_option_response_on_condition, guard_persuation_weapon_smuggling_selected_option_response_on_consequence, this);
		}

		private void persuasion_start_with_guards_on_consequence()
		{
			ConversationManager.StartPersuasion(2f, 1f, 0f, 2f, 2f, 0f, PersuasionDifficulty.VeryHard);
		}

		private bool persuasion_start_with_guards_on_condition()
		{
			if (_guardsParty != null)
			{
				return CharacterObject.OneToOneConversationCharacter == ConversationHelper.GetConversationCharacterPartyLeader(_guardsParty.Party);
			}
			return false;
		}

		private bool persuasion_failed_with_guards_on_condition()
		{
			if (_task.Options.All((PersuasionOptionArgs x) => x.IsBlocked) && !ConversationManager.GetPersuasionProgressSatisfied())
			{
				MBTextManager.SetTextVariable("FAILED_PERSUASION_LINE", _task.FinalFailLine);
				return true;
			}
			return false;
		}

		private void persuasion_rejected_with_guards_on_consequence()
		{
			PlayerEncounter.LeaveEncounter = false;
			ConversationManager.EndPersuasion();
		}

		private void persuasion_complete_with_guards_on_consequence()
		{
			PlayerDodgedGuards();
			PlayerEncounter.LeaveEncounter = true;
			ConversationManager.EndPersuasion();
		}

		private PersuasionTask GetPersuasionTask()
		{
			PersuasionTask persuasionTask = new PersuasionTask(0);
			persuasionTask.FinalFailLine = new TextObject("{=XCJGl82o}Do you think you can pull one over on me? Now hand over the weapons![if:convo_furious][ib:aggressive]");
			persuasionTask.TryLaterLine = new TextObject("{=!}TODO");
			persuasionTask.SpokenLine = new TextObject("{=6P1ruzsC}Maybe...");
			PersuasionOptionArgs option = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.RogueSkills, TraitEffect.Positive, PersuasionArgumentStrength.Hard, givesCriticalSuccess: false, new TextObject("{=1hbos200}Here are some documents from the chancellery."));
			persuasionTask.AddOptionToTask(option);
			PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.Normal, givesCriticalSuccess: false, new TextObject("{=UHCKXapl}The metalworkers' guild asked for them. Don't worry, they'll be melted into scrap."));
			persuasionTask.AddOptionToTask(option2);
			PersuasionOptionArgs option3 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.RogueSkills, TraitEffect.Positive, PersuasionArgumentStrength.VeryHard, givesCriticalSuccess: false, new TextObject("{=8Wa6OxG8}It's secret. You must be new in your post if you don't know who I am and what I do."));
			persuasionTask.AddOptionToTask(option3);
			return persuasionTask;
		}

		private bool guard_persuation_weapon_smuggling_selected_option_response_on_condition()
		{
			PersuasionOptionResult item = ConversationManager.GetPersuasionChosenOptions().Last().Item2;
			MBTextManager.SetTextVariable("PERSUASION_REACTION", PersuasionHelper.GetDefaultPersuasionOptionReaction(item));
			if (item == PersuasionOptionResult.CriticalFailure)
			{
				_task.BlockAllOptions();
			}
			return true;
		}

		private void guard_persuation_weapon_smuggling_selected_option_response_on_consequence()
		{
			Tuple<PersuasionOptionArgs, PersuasionOptionResult> tuple = ConversationManager.GetPersuasionChosenOptions().Last();
			float difficulty = Campaign.Current.Models.PersuasionModel.GetDifficulty(PersuasionDifficulty.VeryHard);
			Campaign.Current.Models.PersuasionModel.GetEffectChances(tuple.Item1, out var moveToNextStageChance, out var blockRandomOptionChance, difficulty);
			_task.ApplyEffects(moveToNextStageChance, blockRandomOptionChance);
		}

		private bool guard_persuation_weapon_smuggling_select_option_1_on_condition()
		{
			if (_task.Options.Count > 0)
			{
				TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
				textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(_task.Options.ElementAt(0), showToPlayer: false));
				textObject.SetTextVariable("PERSUASION_OPTION_LINE", _task.Options.ElementAt(0).Line);
				MBTextManager.SetTextVariable("GUARDS_PERSUADE_ATTEMPT_1", textObject);
				return true;
			}
			return false;
		}

		private bool guard_persuation_weapon_smuggling_select_option_2_on_condition()
		{
			if (_task.Options.Count > 1)
			{
				TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
				textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(_task.Options.ElementAt(1), showToPlayer: false));
				textObject.SetTextVariable("PERSUASION_OPTION_LINE", _task.Options.ElementAt(1).Line);
				MBTextManager.SetTextVariable("GUARDS_PERSUADE_ATTEMPT_2", textObject);
				return true;
			}
			return false;
		}

		private bool guard_persuation_weapon_smuggling_select_option_3_on_condition()
		{
			if (_task.Options.Count > 2)
			{
				TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
				textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(_task.Options.ElementAt(2), showToPlayer: false));
				textObject.SetTextVariable("PERSUASION_OPTION_LINE", _task.Options.ElementAt(2).Line);
				MBTextManager.SetTextVariable("GUARDS_PERSUADE_ATTEMPT_3", textObject);
				return true;
			}
			return false;
		}

		private void guard_persuation_weapon_smuggling_select_option_1_on_consequence()
		{
			if (_task.Options.Count > 0)
			{
				_task.Options[0].BlockTheOption(isBlocked: true);
			}
		}

		private void guard_persuation_weapon_smuggling_select_option_2_on_consequence()
		{
			if (_task.Options.Count > 1)
			{
				_task.Options[1].BlockTheOption(isBlocked: true);
			}
		}

		private void guard_persuation_weapon_smuggling_select_option_3_on_consequence()
		{
			if (_task.Options.Count > 2)
			{
				_task.Options[2].BlockTheOption(isBlocked: true);
			}
		}

		private PersuasionOptionArgs guard_persuation_weapon_smuggling_setup_option_1()
		{
			return _task.Options.ElementAt(0);
		}

		private PersuasionOptionArgs guard_persuation_weapon_smuggling_setup_option_2()
		{
			return _task.Options.ElementAt(1);
		}

		private PersuasionOptionArgs guard_persuation_weapon_smuggling_setup_option_3()
		{
			return _task.Options.ElementAt(2);
		}

		private bool guard_persuation_weapon_smuggling_clickable_option_1_on_condition(out TextObject hintText)
		{
			hintText = new TextObject("{=9ACJsI6S}Blocked");
			if (_task.Options.Count > 0)
			{
				hintText = (_task.Options.ElementAt(0).IsBlocked ? hintText : TextObject.GetEmpty());
				return !_task.Options.ElementAt(0).IsBlocked;
			}
			return false;
		}

		private bool guard_persuation_weapon_smuggling_clickable_option_2_on_condition(out TextObject hintText)
		{
			hintText = new TextObject("{=9ACJsI6S}Blocked");
			if (_task.Options.Count > 1)
			{
				hintText = (_task.Options.ElementAt(1).IsBlocked ? hintText : TextObject.GetEmpty());
				return !_task.Options.ElementAt(1).IsBlocked;
			}
			return false;
		}

		private bool guard_persuation_weapon_smuggling_clickable_option_3_on_condition(out TextObject hintText)
		{
			hintText = new TextObject("{=9ACJsI6S}Blocked");
			if (_task.Options.Count > 2)
			{
				hintText = (_task.Options.ElementAt(2).IsBlocked ? hintText : TextObject.GetEmpty());
				return !_task.Options.ElementAt(2).IsBlocked;
			}
			return false;
		}

		private bool DialogStartCondition()
		{
			if (!_playerDodgedGuards && _guardsParty != null)
			{
				return CharacterObject.OneToOneConversationCharacter == ConversationHelper.GetConversationCharacterPartyLeader(_guardsParty.Party);
			}
			return false;
		}

		private void StartFight()
		{
			int upgradeLevel = ((!base.QuestGiver.CurrentSettlement.IsTown) ? 1 : base.QuestGiver.CurrentSettlement.Town.GetWallLevel());
			_highCrimeRatingWillBeApplied = true;
			if (_guardsParty.CurrentSettlement == null)
			{
				_guardsParty.CurrentSettlement = base.QuestGiver.CurrentSettlement;
			}
			PlayerEncounter.RestartPlayerEncounter(_guardsParty.Party, PartyBase.MainParty, forcePlayerOutFromSettlement: false);
			PlayerEncounter.StartBattle();
			GameMenu.ActivateGameMenu("town");
			int num = (int)(5f + 15f * _issueDifficulty);
			CampaignMission.OpenBattleMissionWhileEnteringSettlement(base.QuestGiver.CurrentSettlement.LocationComplex.GetLocationWithId("center").GetSceneName(upgradeLevel), upgradeLevel, num, num);
			_checkForBattleResult = true;
		}

		private void PlayerDodgeGuardsLowCrimeRating()
		{
			PlayerDodgedGuards();
			_lowCrimeRatingWillBeApplied = true;
		}

		private bool CheckPlayersPartySize()
		{
			return MobileParty.MainParty.MemberRoster.TotalRegulars - MobileParty.MainParty.MemberRoster.TotalWoundedRegulars > 30;
		}

		private void PlayerDodgedGuards()
		{
			MBInformationManager.AddQuickInformation(new TextObject("{=vYRbyXkz}The guards won't come after you anymore."));
			if (_guardsParty.IsActive && _guardsParty.IsVisible)
			{
				DestroyPartyAction.Apply(PartyBase.MainParty, _guardsParty);
			}
			_playerDodgedGuards = true;
		}

		private void PlayerBribeGuard()
		{
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, _bribeGold);
			PlayerDodgedGuards();
		}

		private void DeleteAllWeaponsFromPlayer()
		{
			Dictionary<EquipmentElement, int> dictionary = new Dictionary<EquipmentElement, int>();
			foreach (ItemRosterElement item in PartyBase.MainParty.ItemRoster)
			{
				if (item.EquipmentElement.Item != null && !item.EquipmentElement.IsQuestItem && item.EquipmentElement.Item.WeaponComponent != null && item.Amount > 0 && !dictionary.ContainsKey(item.EquipmentElement))
				{
					dictionary.Add(item.EquipmentElement, item.Amount);
					PartyBase.MainParty.ItemRoster.AddToCounts(item.EquipmentElement, -item.Amount);
				}
			}
			_weaponsThatGuardTook = dictionary;
			CalculateAndSetRequestedItemCountOnPlayer();
		}

		private void QuestSuccessDeleteWeaponsFromPlayer()
		{
			int num = _requestedWeaponAmount;
			int num2 = PartyBase.MainParty.ItemRoster.Count - 1;
			while (num2 >= 0)
			{
				ItemRosterElement itemRosterElement = PartyBase.MainParty.ItemRoster[num2];
				if (itemRosterElement.EquipmentElement.Item != null && itemRosterElement.EquipmentElement.Item.WeaponComponent != null && itemRosterElement.EquipmentElement.Item.WeaponComponent.PrimaryWeapon.WeaponClass == _requestedWeaponClass && itemRosterElement.Amount > 0)
				{
					if (num < itemRosterElement.Amount)
					{
						PartyBase.MainParty.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, -num);
						break;
					}
					PartyBase.MainParty.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, -itemRosterElement.Amount);
					num -= itemRosterElement.Amount;
				}
				if (num != 0)
				{
					num2--;
					continue;
				}
				break;
			}
		}

		private bool HasPlayerEnoughMoneyToBribe(out TextObject hintText)
		{
			if (Hero.MainHero.Gold < _bribeGold)
			{
				hintText = new TextObject("{=1V6DRayw}You don't have {BRIBE_COST} denars.");
				hintText.SetTextVariable("BRIBE_COST", _bribeGold);
				return false;
			}
			hintText = TextObject.GetEmpty();
			return true;
		}

		private void PlayerDefeatedAgainstGuards()
		{
			AddLog(FailPlayerDefeatedAgainstGuardsLogText);
			base.QuestGiver.AddPower(-10f);
			RelationshipChangeWithQuestGiver = -5;
			CompleteQuestWithFail();
		}

		private void PlayerSuccessfullyDeliveredWeapons()
		{
			AddLog(SuccessQuestLogText);
			GainRenownAction.Apply(Hero.MainHero, 2f);
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, (int)_rewardGold);
			base.QuestGiver.AddPower(10f);
			RelationshipChangeWithQuestGiver = 5;
			QuestSuccessDeleteWeaponsFromPlayer();
			_weaponsThatGuardTook.Clear();
			base.QuestGiver.CurrentSettlement.Town.Security -= 30f;
			if (_lowCrimeRatingWillBeApplied)
			{
				ChangeCrimeRatingAction.Apply(base.QuestGiver.CurrentSettlement.MapFaction, 30f);
			}
			if (_highCrimeRatingWillBeApplied)
			{
				ChangeCrimeRatingAction.Apply(base.QuestGiver.CurrentSettlement.MapFaction, 60f);
			}
			CompleteQuestWithSuccess();
		}

		public override void OnFailed()
		{
			if (_lowCrimeRatingWillBeApplied)
			{
				ChangeCrimeRatingAction.Apply(base.QuestGiver.CurrentSettlement.MapFaction, 30f);
			}
			if (_highCrimeRatingWillBeApplied)
			{
				ChangeCrimeRatingAction.Apply(base.QuestGiver.CurrentSettlement.MapFaction, 60f);
			}
		}

		protected override void OnFinalize()
		{
			if (_guardsParty != null && _guardsParty.IsActive)
			{
				DestroyPartyAction.Apply(PartyBase.MainParty, _guardsParty);
			}
		}

		protected override void OnTimedOut()
		{
			AddLog(FailQuestTimedOutLogText);
			base.QuestGiver.AddPower(-10f);
			RelationshipChangeWithQuestGiver = -5;
		}
	}

	public class GangLeaderNeedsWeaponsIssueTypeDefiner : SaveableTypeDefiner
	{
		public GangLeaderNeedsWeaponsIssueTypeDefiner()
			: base(3940000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(GangLeaderNeedsWeaponsIssue), 1);
			AddClassDefinition(typeof(GangLeaderNeedsWeaponsIssueQuest), 2);
		}
	}

	private const IssueBase.IssueFrequency GangLeaderNeedsWeaponsIssueFrequency = IssueBase.IssueFrequency.Common;

	private int _createdPartyCount;

	private static WeaponClass[] _canBeRequestedWeaponClassList = new WeaponClass[1] { WeaponClass.OneHandedAxe };

	private static int CreatedPartyCount => Campaign.Current.GetCampaignBehavior<GangLeaderNeedsWeaponsIssueQuestBehavior>()._createdPartyCount;

	public GangLeaderNeedsWeaponsIssueQuestBehavior()
	{
		_createdPartyCount = 0;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_createdPartyCount", ref _createdPartyCount);
	}

	public void OnCheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero))
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnStartIssue, typeof(GangLeaderNeedsWeaponsIssue), IssueBase.IssueFrequency.Common));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(GangLeaderNeedsWeaponsIssue), IssueBase.IssueFrequency.Common));
		}
	}

	private bool ConditionsHold(Hero IssueOwner)
	{
		if (IssueOwner.IsGangLeader && IssueOwner.CurrentSettlement != null && IssueOwner.CurrentSettlement.IsTown)
		{
			return IssueOwner.CurrentSettlement.Town.Loyalty < 60f;
		}
		return false;
	}

	private IssueBase OnStartIssue(in PotentialIssueData pid, Hero issueOwner)
	{
		_createdPartyCount++;
		return new GangLeaderNeedsWeaponsIssue(issueOwner);
	}

	private static TextObject GetNeededClassTextObject(WeaponClass requestedWeaponClass)
	{
		if (requestedWeaponClass == WeaponClass.OneHandedAxe)
		{
			return new TextObject("{=tza4micZ}one-handed axes");
		}
		return new TextObject("{=!}Undefined!");
	}
}
