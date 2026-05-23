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

public class LandLordTheArtOfTheTradeIssueBehavior : CampaignBehaviorBase
{
	public class LandLordTheArtOfTheTradeIssue : IssueBase
	{
		private const int IssueAndQuestDuration = 30;

		private const int CompanionRequiredSkillLevel = 120;

		private ItemObject _selectedItemObject;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.Duration;

		private int SelectedItemObjectCount => MathF.Max(1, MathF.Round((float)(int)(5000f / (float)_selectedItemObject.Value) * base.IssueDifficultyMultiplier));

		public override int AlternativeSolutionBaseNeededMenCount => 2 + MathF.Ceiling(4f * base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 3 + MathF.Ceiling(5f * base.IssueDifficultyMultiplier);

		protected override int RewardGold => (int)((float)(_selectedItemObject.Value * SelectedItemObjectCount) * RewardGoldDeterministicRandomContribution);

		private float RewardGoldDeterministicRandomContribution => base.IssueOwner.RandomFloatWithSeed((uint)IssueCreationTime.ElapsedDaysUntilNow, 0.2f, 0.5f);

		public override TextObject IssueBriefByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=AKdDSZoM}Yes. It's a good problem to have, though. As you know, I deal in {.%}{SELECTED_ITEM}{.%}. Production this year has been very good, and we can no longer make a profit on the local market. I cannot however, put together a caravan to sell it elsewhere. So, I propose a very simple deal.[if:convo_innocent_smile][ib:confident]");
				if (_selectedItemObject.HasHorseComponent || _selectedItemObject.IsAnimal)
				{
					textObject = new TextObject("{=llVFTH6n}Yes. It's a good problem to have, though. As you may know, I deal in {PLURAL(SELECTED_ITEM)}. Our herds have increased this year, and we can no longer make a profit on the local market. I cannot however organize a drive to a new market. So, I propose a very simple deal.[if:convo_innocent_smile][ib:confident]");
				}
				textObject.SetTextVariable("SELECTED_ITEM", _selectedItemObject.Name);
				return textObject;
			}
		}

		public override TextObject IssueAcceptByPlayer => new TextObject("{=8jyUn6mb}You sell me the goods at a discount?");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=rWnvnufw}That could work, if you have the money. But if you don't, I'm willing to take a chance on you. I reckon for {SELECTED_ITEM_COUNT} {.%}{?SELECTED_ITEM_COUNT > 1}{PLURAL(SELECTED_ITEM)}{?}{SELECTED_ITEM}{\\?}{.%} you can probably find a market nearby where buyers will pay you a total of {TOTAL_GOLD}{GOLD_ICON}. Here's my offer. I loan you the product. You sell it at whatever price you like, and bring me back {TOTAL_GOLD}{GOLD_ICON} denars. I have little doubt you could find a market where you could get a better price than this, and make a profit.[if:convo_innocent_smile][ib:confident]");
				if (_selectedItemObject.HasHorseComponent || _selectedItemObject.IsAnimal)
				{
					textObject = new TextObject("{=b19Hlp7h}That could work, if you have the money. But if you don't, I'm willing to take a chance on you. I reckon for {SELECTED_ITEM_COUNT} {?SELECTED_ITEM_COUNT > 1}{PLURAL(SELECTED_ITEM)}{?}{SELECTED_ITEM}{\\?} you can probably find a market nearby where buyers will pay you a total of {TOTAL_GOLD}{GOLD_ICON}. Here's my offer. I loan you the livestock. You sell at whatever price you like, and bring me back {TOTAL_GOLD}{GOLD_ICON} denars. I have little doubt you could find a market where you could get a better price than this, and make a profit.[if:convo_innocent_smile][ib:confident]");
				}
				textObject.SetTextVariable("TOTAL_GOLD", (int)((float)(_selectedItemObject.Value * SelectedItemObjectCount) * 0.55f));
				textObject.SetTextVariable("SELECTED_ITEM_COUNT", SelectedItemObjectCount);
				textObject.SetTextVariable("SELECTED_ITEM", _selectedItemObject.Name);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		public override TextObject IssuePlayerResponseAfterAlternativeExplanation => new TextObject("{=w1rVTUIs}This seems like a simple errand. Would your offer still stand if I get someone else to do it for me?");

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=PE97ZWvX}Not just anybody. If you have a companion that has your complete trust, then I'll agree... Just make sure he is well guarded. Goods attract bandits, as I'm sure you know. I suppose {ALTERNATIVE_TROOP_AMOUNT} well-armed men would be enough.[if:convo_innocent_smile][ib:confident]");
				textObject.SetTextVariable("ALTERNATIVE_TROOP_AMOUNT", GetTotalAlternativeSolutionNeededMenCount());
				return textObject;
			}
		}

		public override TextObject IssueQuestSolutionAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=aB0TT6Ur}Fear not. I will find the right market for your {.%}{SELECTED_ITEM}{.%} myself.");
				if (_selectedItemObject.HasHorseComponent || _selectedItemObject.IsAnimal)
				{
					textObject = new TextObject("{=cBO49V5b}Fear not. I will find the right market for your {PLURAL(SELECTED_ITEM)} myself.");
				}
				textObject.SetTextVariable("SELECTED_ITEM", _selectedItemObject.Name);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=IFasMslv}I will assign a companion with {TROOP_COUNT} good men for {RETURN_DAYS} days.");
				textObject.SetTextVariable("TROOP_COUNT", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		public override TextObject IssueDiscussAlternativeSolution => new TextObject("{=dLywF1Uz}I have heard your companion has started to sell the goods. This was a good deal, {?PLAYER.GENDER}ma'am{?}sir{\\?}.[if:convo_relaxed_happy][ib:confident]");

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver => new TextObject("{=aUXAh8cE}Very well, {PLAYER.NAME}. If you're willing to vouch for your companion, I'm sure this will work.[if:convo_innocent_smile][ib:confident]");

		public override bool IsThereAlternativeSolution => true;

		public override bool IsThereLordSolution => false;

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=BK9ww4NU}{QUEST_GIVER.LINK} asked you to sell {?QUEST_GIVER.GENDER}her{?}his{\\?} goods for at least {UNIT_PRICE}{GOLD_ICON} per {UNIT_NAME} and return to {?QUEST_GIVER.GENDER}her{?}him{\\?}. {?QUEST_GIVER.GENDER}She{?}He{\\?} told you that any profit that the deal would make above this price is yours to keep. You asked {COMPANION.LINK} to take {TROOP_COUNT} of your best men to go and take care of it. They should report back to you in {RETURN_DAYS} days.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject, includeDetails: true);
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				textObject.SetTextVariable("UNIT_NAME", "{=g72xNv75}load");
				if (_selectedItemObject.HasHorseComponent || _selectedItemObject.IsAnimal)
				{
					textObject.SetTextVariable("UNIT_NAME", "{=T9Tgi9is}animal");
				}
				textObject.SetTextVariable("UNIT_PRICE", _selectedItemObject.Value);
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				textObject.SetTextVariable("TROOP_COUNT", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=BDAmZkJF}You received a message from {ISSUE_GIVER.LINK}. 'It was a pleasure doing business with you and your folks.'");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject Title => new TextObject("{=96m29Eb7}The Art of the Trade");

		public override TextObject Description
		{
			get
			{
				TextObject result = new TextObject("{=BZwKEIm5}{ISSUE_GIVER.LINK} wants you to sell {?ISSUE_GIVER.GENDER}her{?}his{\\?} goods for a price above the global average.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject);
				return result;
			}
		}

		protected override int CompanionSkillRewardXP => (int)(900f + 800f * base.IssueDifficultyMultiplier);

		internal static void AutoGeneratedStaticCollectObjectsLandLordTheArtOfTheTradeIssue(object o, List<object> collectedObjects)
		{
			((LandLordTheArtOfTheTradeIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		public LandLordTheArtOfTheTradeIssue(Hero issueOwner, ItemObject villageProduction)
			: base(issueOwner, CampaignTime.DaysFromNow(30f))
		{
			_selectedItemObject = villageProduction;
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.VillageHearth)
			{
				return -0.1f;
			}
			if (issueEffect == DefaultIssueEffects.IssueOwnerPower)
			{
				return -0.1f;
			}
			return 0f;
		}

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			return ((hero.GetSkillValue(DefaultSkills.Trade) >= hero.GetSkillValue(DefaultSkills.Riding)) ? DefaultSkills.Trade : DefaultSkills.Riding, 120);
		}

		public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
		{
			int totalAlternativeSolutionNeededMenCount = GetTotalAlternativeSolutionNeededMenCount();
			if (GetNumberOfTier2Troops(troopRoster) < totalAlternativeSolutionNeededMenCount)
			{
				explanation = new TextObject("{=rJIY7OiR}You have to send {NEEDED_TROOP_COUNT} troops with at least tier 2 to this quest.");
				explanation.SetTextVariable("NEEDED_TROOP_COUNT", totalAlternativeSolutionNeededMenCount);
				return false;
			}
			explanation = null;
			return true;
		}

		private int GetNumberOfTier2Troops(TroopRoster troopRoster)
		{
			int num = 0;
			foreach (TroopRosterElement item in troopRoster.GetTroopRoster())
			{
				if (item.Character.Tier >= 2)
				{
					num += troopRoster.GetTroopCount(item.Character) - troopRoster.GetElementWoundedNumber(troopRoster.FindIndexOfTroop(item.Character));
				}
			}
			return num;
		}

		public override IssueFrequency GetFrequency()
		{
			return IssueFrequency.VeryCommon;
		}

		public override bool IssueStayAliveConditions()
		{
			if (!base.IssueOwner.CurrentSettlement.IsRaided && !base.IssueOwner.CurrentSettlement.IsUnderRaid)
			{
				return (float)base.IssueOwner.CurrentSettlement.Village.Bound.Town.GetItemPrice(base.IssueOwner.CurrentSettlement.Village.VillageType.PrimaryProduction) < (float)base.IssueOwner.CurrentSettlement.Village.VillageType.PrimaryProduction.Value * 1.3f;
			}
			return false;
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
			_selectedItemObject = base.IssueOwner.CurrentSettlement.Village.VillageType.PrimaryProduction;
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new LandLordTheArtOfTheTradeIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(30f), _selectedItemObject, SelectedItemObjectCount);
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			GainRenownAction.Apply(Hero.MainHero, 1f);
			RelationshipChangeWithIssueOwner = 5;
		}
	}

	public class LandLordTheArtOfTheTradeIssueQuest : QuestBase
	{
		[SaveableField(10)]
		private ItemObject _selectedItemObject;

		[SaveableField(20)]
		private int _selectedItemObjectCount;

		[SaveableField(30)]
		private int _soldCount;

		[SaveableField(40)]
		private int _gatheredDenars;

		private bool _productsUndersold = true;

		[SaveableField(60)]
		private int _daysPassed;

		[SaveableField(70)]
		private bool _underSoldLogAdded;

		private int _targetDenarsToAchieve;

		[SaveableField(80)]
		private JournalLog _soldItemAmountLog;

		[SaveableField(90)]
		private JournalLog _gatheredDenarsLog;

		public override TextObject Title => new TextObject("{=96m29Eb7}The Art of the Trade");

		public override bool IsRemainingTimeHidden => false;

		private bool QuestCanBeFinalized
		{
			get
			{
				if (Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero == base.QuestGiver)
				{
					if (ItemCountLeftToSell > 0 && _targetDenarsToAchieve > _gatheredDenars)
					{
						if (_daysPassed >= 1)
						{
							return CheckIfPlayerLostItem();
						}
						return false;
					}
					return true;
				}
				return false;
			}
		}

		private int ItemCountLeftToSell => _selectedItemObjectCount - _soldCount;

		private int RemainingDenars => _targetDenarsToAchieve - _gatheredDenars;

		private TextObject QuestStartedLogText1
		{
			get
			{
				TextObject textObject = new TextObject("{=2bcNCnI3}{QUEST_GIVER.LINK} asked you to sell {?QUEST_GIVER.GENDER}her{?}his{\\?} goods for at least {UNIT_PRICE}{GOLD_ICON} per load and return to {?QUEST_GIVER.GENDER}her{?}him{\\?}. {?QUEST_GIVER.GENDER}She{?}He{\\?} told you that any profit would make above this price is yours to keep.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, includeDetails: true);
				textObject.SetTextVariable("UNIT_PRICE", _targetDenarsToAchieve / _selectedItemObjectCount);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		private TextObject QuestStartedLogText2
		{
			get
			{
				TextObject textObject = new TextObject("{=jI251oj9}{?QUEST_GIVER.GENDER}She{?}He{\\?} noted that {?QUEST_GIVER.GENDER}she{?}he{\\?} has {SELECTED_ITEM_COUNT} {.%}{?SELECTED_ITEM_COUNT > 1}{PLURAL(SELECTED_ITEM)}{?}{SELECTED_ITEM}{\\?}{.%}. {?QUEST_GIVER.GENDER}She{?}He{\\?} is expecting to earn {TOTAL_GOLD}{GOLD_ICON} denars from that.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("SELECTED_ITEM_COUNT", _selectedItemObjectCount);
				textObject.SetTextVariable("SELECTED_ITEM", _selectedItemObject.Name);
				textObject.SetTextVariable("TOTAL_GOLD", _targetDenarsToAchieve);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		private TextObject QuestStartedLogText3
		{
			get
			{
				TextObject textObject = new TextObject("{=tf04Piaj}You told {?QUEST_GIVER.GENDER}her{?}him{\\?} that you will sell the goods personally.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject QuestSuccessLogText => new TextObject("{=EsNDadiR}You have completed your end of your bargain and made some denars simply by using your smarts.");

		private TextObject QuestReadyToBeFinishedLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=2GMOnUIM}{QUEST_GIVER.LINK} expects your return to have {?QUEST_GIVER.GENDER}her{?}his{\\?} share of the deal.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
				return textObject;
			}
		}

		private TextObject QuestReadyToBeFinishedUnderSoldLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=IfPnl0B6}You have lost some of the wares you were supposed to be selling. You can speak with {QUEST_GIVER.LINK} to pay for a compensation.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject QuestSuccessWithPayingGatheredDenarsLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=4bDHBIr1}You have completed your end of the bargain. {QUEST_GIVER.LINK} now considers you as a trustworthy {?PLAYER.GENDER}tradeswoman{?}tradesman{\\?}.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
				return textObject;
			}
		}

		private TextObject QuestSuccessPlayerBoughtItemsLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=rYt43AWk}{QUEST_GIVER.LINK} accepted your offer to buy the products from an average price. You can now sell those for profits without any obligations to {?QUEST_GIVER.GENDER}her{?}him{\\?}.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject QuestFailedPlayerBrokeTheAgreementLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=1q2dxlol}You have failed to pay {TOTAL_DENAR}{GOLD_ICON} denars to {QUEST_GIVER.LINK}. {QUEST_GIVER_VILLAGE} and its locals are aware of your misdemeanor.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_GIVER_VILLAGE", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("TOTAL_DENAR", _targetDenarsToAchieve);
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
				TextObject textObject = new TextObject("{=w0FD9U98}{QUEST_GIVER.LINK}'s village is raided. Your agreement with {?QUEST_GIVER.GENDER}her{?}him{\\?} is moot.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject QuestFailedWithTimeOutLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=PQt8lfTX}You have failed to return to the {QUEST_GIVER.LINK} in time. {QUEST_GIVER_VILLAGE} and its locals are aware of your misdemeanor.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_GIVER_VILLAGE", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsLandLordTheArtOfTheTradeIssueQuest(object o, List<object> collectedObjects)
		{
			((LandLordTheArtOfTheTradeIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_selectedItemObject);
			collectedObjects.Add(_soldItemAmountLog);
			collectedObjects.Add(_gatheredDenarsLog);
		}

		internal static object AutoGeneratedGetMemberValue_selectedItemObject(object o)
		{
			return ((LandLordTheArtOfTheTradeIssueQuest)o)._selectedItemObject;
		}

		internal static object AutoGeneratedGetMemberValue_selectedItemObjectCount(object o)
		{
			return ((LandLordTheArtOfTheTradeIssueQuest)o)._selectedItemObjectCount;
		}

		internal static object AutoGeneratedGetMemberValue_soldCount(object o)
		{
			return ((LandLordTheArtOfTheTradeIssueQuest)o)._soldCount;
		}

		internal static object AutoGeneratedGetMemberValue_gatheredDenars(object o)
		{
			return ((LandLordTheArtOfTheTradeIssueQuest)o)._gatheredDenars;
		}

		internal static object AutoGeneratedGetMemberValue_daysPassed(object o)
		{
			return ((LandLordTheArtOfTheTradeIssueQuest)o)._daysPassed;
		}

		internal static object AutoGeneratedGetMemberValue_underSoldLogAdded(object o)
		{
			return ((LandLordTheArtOfTheTradeIssueQuest)o)._underSoldLogAdded;
		}

		internal static object AutoGeneratedGetMemberValue_soldItemAmountLog(object o)
		{
			return ((LandLordTheArtOfTheTradeIssueQuest)o)._soldItemAmountLog;
		}

		internal static object AutoGeneratedGetMemberValue_gatheredDenarsLog(object o)
		{
			return ((LandLordTheArtOfTheTradeIssueQuest)o)._gatheredDenarsLog;
		}

		public LandLordTheArtOfTheTradeIssueQuest(string questId, Hero giverHero, CampaignTime duration, ItemObject selectedItemObject, int selectedItemObjectCount)
			: base(questId, giverHero, duration, 0)
		{
			_selectedItemObject = selectedItemObject;
			_selectedItemObjectCount = selectedItemObjectCount;
			_targetDenarsToAchieve = (int)((float)(selectedItemObject.Value * selectedItemObjectCount) * 0.55f);
			_daysPassed = 0;
			SetDialogs();
			InitializeQuestOnCreation();
		}

		protected override void SetDialogs()
		{
			TextObject textObject = new TextObject("{=3Sk0BQ4n}I will buy the products from you for {TOTAL_GOLD}{GOLD_ICON}. This way we both will get what we desire.");
			if (_selectedItemObject.IsAnimal || _selectedItemObject.HasHorseComponent)
			{
				textObject = new TextObject("{=7nBOWsg2}I will buy the livestock from you for {TOTAL_GOLD}{GOLD_ICON}. This way we both will get what we desire.");
			}
			textObject.SetTextVariable("TOTAL_GOLD", _targetDenarsToAchieve);
			textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
			TextObject textObject2 = new TextObject("{=iYtzlRSN}I would rather be your middleman on this matter. I need to keep my money. You can have your men load up the {.%}{SELECTED_ITEM}{.%} already.");
			if (_selectedItemObject.IsAnimal || _selectedItemObject.HasHorseComponent)
			{
				textObject2 = new TextObject("{=exmSGWUb}I would rather be your middleman on this matter. I need to keep my money. You can have your men get the herd ready.");
			}
			textObject2.SetTextVariable("SELECTED_ITEM", _selectedItemObject.Name);
			TextObject textObject3 = new TextObject("{=9d6WxRrj}Do you want to buy the goods yourself? Or will I retain ownership, and you just keep the extra profits? I am expecting to earn {TOTAL_DENARS}{GOLD_ICON} for {SELECTED_COUNT} loads of produce. If would like to buy it right away you can simply sell it yourself or do whatever you wish with it.");
			if (_selectedItemObject.IsAnimal || _selectedItemObject.HasHorseComponent)
			{
				textObject3 = new TextObject("{=GihVcxIB}Do you want to buy the livestock yourself? Or will I retain ownership, and you just keep the extra profits? I am expecting to earn {TOTAL_DENARS}{GOLD_ICON} in total. If would like to buy the livestock right away you can simply sell it yourself or do whatever you wish with it.");
			}
			textObject3.SetTextVariable("TOTAL_DENARS", _targetDenarsToAchieve);
			textObject3.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
			textObject3.SetTextVariable("SELECTED_COUNT", _selectedItemObjectCount);
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(new TextObject("{=NaoYCmC6}Good. Needless to say, by not taking any money up front, I am trusting in your honesty in your ability to protect those goods. But I am sure that trust will not be misplaced.[if:convo_innocent_smile][ib:closed]")).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.NpcLine(textObject3)
				.BeginPlayerOptions()
				.PlayerOption(textObject)
				.ClickableCondition(PlayerBuyClickableOptionCondition)
				.NpcLine(new TextObject("{=LmTii9E2}It was a pleasure doing business with you. If only everyone was as honest as you.[if:convo_happy][ib:confident3]"))
				.Consequence(delegate
				{
					StartQuest();
					QuestFinishedPlayerBoughtTheGoods();
				})
				.CloseDialog()
				.PlayerOption(textObject2)
				.Consequence(QuestAcceptedConsequences)
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
			TextObject playerMainOptionOneWithGold = new TextObject("{=1zdkXAwL}The market isn't what we expected. I am afraid I only made {GATHERED_DENARS}{GOLD_ICON} of the {TOTAL_DENARS}{GOLD_ICON} that we agreed upon.");
			playerMainOptionOneWithGold.SetTextVariable("GATHERED_DENARS", _gatheredDenars);
			playerMainOptionOneWithGold.SetTextVariable("TOTAL_DENARS", _targetDenarsToAchieve);
			playerMainOptionOneWithGold.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
			TextObject playerMainOptionOneNoGold = new TextObject("{=52lNazA1}I'm afraid that things came up. I was not able to make the sale.");
			TextObject text = new TextObject("{=!}{PLAYER_OPTION}");
			TextObject textObject4 = new TextObject("{=THD3C7xc}I have. Here is the {TOTAL_DENARS}{GOLD_ICON} denars just as we agreed.");
			textObject4.SetTextVariable("TOTAL_DENARS", _targetDenarsToAchieve);
			textObject4.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
			TextObject textObject5 = new TextObject("{=z47GjqTZ}Yes, of course. This is the {TOTAL_DENARS}{GOLD_ICON} denars that I owe you.");
			textObject5.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
			textObject5.SetTextVariable("TOTAL_DENARS", _targetDenarsToAchieve);
			TextObject playerFailOptionWithGold = new TextObject("{=dtzKfkrh}We never agreed on this. I am not paying you any more than {GATHERED_DENARS}{GOLD_ICON}, and you cannot force me.");
			playerFailOptionWithGold.SetTextVariable("GATHERED_DENARS", _gatheredDenars);
			TextObject playerFailOptionNoGold = new TextObject("{=aFDiKxhr}Our deal involved you getting your cut from the sales I made. No sale means no cut. I'm sure you understand.[ib:warrior2]");
			TextObject text2 = new TextObject("{=!}{PLAYER_FAIL_OPTION}");
			TextObject textObject6 = new TextObject("{=41wb8QaV}I know I can not force you to pay you what you owe me. But I think you will find that a good name is worth more than a few loads of {SELECTED_ITEM}...");
			textObject6.SetTextVariable("SELECTED_ITEM", _selectedItemObject.Name);
			if (_selectedItemObject.IsAnimal || _selectedItemObject.HasHorseComponent)
			{
				textObject6 = new TextObject("{=pcrdOlE8}I know I can not force you to pay you what you owe me. But I think you will find that a good name is worth more than a bit of livestock...");
			}
			TextObject textObject7 = new TextObject("{=OIwtLKN3}I am a {?PLAYER.GENDER}woman{?}man{\\?} of my word. I will raise some money to pay you. Wait for my return {QUEST_GIVER.LINK}.");
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject7);
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject7);
			TextObject textObject8 = new TextObject("{=bPPXiybO}I just happened to be around. Have no fear {QUEST_GIVER.NAME}, your goods are fine.");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject8);
			TextObject npcText = new TextObject("{=ekSg8okD}I will be better once you return with the denars you owe me {PLAYER.NAME}.[ib:aggressive2]");
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject8);
			TextObject textObject9 = new TextObject("{=bbsN6hOo}Have you already sold that {SELECTED_ITEM}? If so, that seems awfully quick.");
			textObject9.SetTextVariable("SELECTED_ITEM", _selectedItemObject.Name);
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").BeginNpcOptions().NpcOption(textObject9, () => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero == base.QuestGiver && !QuestCanBeFinalized)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=gVad94Br}I was just checking in with you."))
				.Condition(() => !QuestCanBeFinalized)
				.NpcLine(npcText)
				.CloseDialog()
				.PlayerOption(textObject8)
				.Condition(() => !QuestCanBeFinalized)
				.NpcLine(npcText)
				.CloseDialog()
				.EndPlayerOptions()
				.NpcOption(new TextObject("{=1q3FKY1s}Have you made your trip yet? I presume you were able to make a sale?"), () => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero == base.QuestGiver && QuestCanBeFinalized)
				.BeginPlayerOptions()
				.PlayerOption(text)
				.Condition(delegate
				{
					if (_gatheredDenars > 0)
					{
						playerMainOptionOneWithGold.SetTextVariable("GATHERED_DENARS", _gatheredDenars);
						Campaign.Current.ConversationManager.GetCurrentDialogLine().SetTextVariable("PLAYER_OPTION", playerMainOptionOneWithGold);
					}
					else
					{
						Campaign.Current.ConversationManager.GetCurrentDialogLine().SetTextVariable("PLAYER_OPTION", playerMainOptionOneNoGold);
					}
					return QuestCanBeFinalized && _productsUndersold;
				})
				.NpcLine(new TextObject("{=QlYUE00L}Well. We did have an agreement. I do expect you to pay the full amount."))
				.BeginPlayerOptions()
				.PlayerOption(textObject5)
				.ClickableCondition(PlayerPayAgreedDenarsClickableCondition)
				.NpcLine(new TextObject("{=gNHh9bvb}I am sorry that you did not manage to make a profit. But you are keeping your end of the bargain, and that is admirable."))
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += PlayerPaidAgreedDenarsQuestSuccess;
				})
				.CloseDialog()
				.PlayerOption(text2)
				.ClickableCondition(PlayerFailWithGoldClickableCondition)
				.Condition(delegate
				{
					if (_gatheredDenars > 0)
					{
						playerFailOptionWithGold.SetTextVariable("GATHERED_DENARS", _gatheredDenars);
						Campaign.Current.ConversationManager.GetCurrentDialogLine().SetTextVariable("PLAYER_FAIL_OPTION", playerFailOptionWithGold);
					}
					else
					{
						Campaign.Current.ConversationManager.GetCurrentDialogLine().SetTextVariable("PLAYER_FAIL_OPTION", playerFailOptionNoGold);
					}
					return true;
				})
				.NpcLine(textObject6)
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += QuestFailedPlayerBrokeTheAgreement;
				})
				.CloseDialog()
				.PlayerOption(textObject7)
				.NpcLine(new TextObject("{=RxjuaDum}I am glad of that. Don't make me wait too long. Some men say they will pay a debt, and you never see them again.[if:convo_mocking_teasing][ib:confident2]"))
				.CloseDialog()
				.EndPlayerOptions()
				.PlayerOption(textObject4)
				.Condition(() => QuestCanBeFinalized && !_productsUndersold)
				.NpcLine(new TextObject("{=9jFqXvHy}Excellent! I knew you were an honest soul. Trust is a fine thing, isn't it? Perhaps we can do more business in the future.[if:convo_innocent_smile][ib:confident]"))
				.Consequence(QuestSuccessPlayerSoldTheProducts)
				.CloseDialog()
				.EndPlayerOptions()
				.EndNpcOptions();
		}

		private void QuestSuccessPlayerSoldTheProducts()
		{
			AddLog(QuestSuccessLogText);
			GainRenownAction.Apply(Hero.MainHero, 1f);
			RelationshipChangeWithQuestGiver = 5;
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, base.QuestGiver, _targetDenarsToAchieve);
			CompleteQuestWithSuccess();
		}

		private void QuestFailedPlayerBrokeTheAgreement()
		{
			AddLog(QuestFailedPlayerBrokeTheAgreementLogText);
			base.QuestGiver.AddPower(-5f);
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, base.QuestGiver, _gatheredDenars);
			RelationshipChangeWithQuestGiver = -10 - RemainingDenars / 10;
			CompleteQuestWithFail();
			ChangeCrimeRatingAction.Apply(base.QuestGiver.MapFaction, 5f);
		}

		private void PlayerPaidAgreedDenarsQuestSuccess()
		{
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, base.QuestGiver, _targetDenarsToAchieve);
			RelationshipChangeWithQuestGiver = 5;
			GainRenownAction.Apply(Hero.MainHero, 1f);
			AddLog(QuestSuccessWithPayingGatheredDenarsLogText);
			CompleteQuestWithSuccess();
		}

		private bool PlayerPayAgreedDenarsClickableCondition(out TextObject explanation)
		{
			if (Hero.MainHero.Gold < _targetDenarsToAchieve)
			{
				explanation = new TextObject("{=d0kbtGYn}You don't have enough gold.");
				return false;
			}
			MBTextManager.SetTextVariable("REMAINING_DENARS", RemainingDenars);
			explanation = null;
			return true;
		}

		private bool PlayerFailWithGoldClickableCondition(out TextObject explanation)
		{
			if (_gatheredDenars > 0 && Hero.MainHero.Gold < _gatheredDenars)
			{
				explanation = new TextObject("{=d0kbtGYn}You don't have enough gold.");
				return false;
			}
			explanation = null;
			return true;
		}

		private void QuestAcceptedConsequences()
		{
			StartQuest();
			AddTrackedObject(base.QuestGiver.CurrentSettlement);
			MobileParty.MainParty.ItemRoster.AddToCounts(_selectedItemObject, _selectedItemObjectCount);
			TextObject textObject = new TextObject("{=jKHkGzUn}As agreed, {SELECTED_ITEM_COUNT} {?IS_PLURAL}{.%}{PLURAL(SELECTED_ITEM)}{.%} have been{?}{.%}{SELECTED_ITEM}{.%} has been{\\?} added to your inventory.");
			textObject.SetTextVariable("SELECTED_ITEM", _selectedItemObject.Name);
			textObject.SetTextVariable("SELECTED_ITEM_COUNT", _selectedItemObjectCount);
			textObject.SetTextVariable("IS_PLURAL", (_selectedItemObjectCount > 1) ? 1 : 0);
			MBInformationManager.AddQuickInformation(textObject);
			AddLog(QuestStartedLogText1);
			AddLog(QuestStartedLogText2);
			AddLog(QuestStartedLogText3);
			TextObject textObject2 = new TextObject("{=GaXEmoFy}Sold {.%}{SELECTED_ITEM}{.%} amount:");
			textObject2.SetTextVariable("SELECTED_ITEM", _selectedItemObject.Name);
			TextObject textObject3 = new TextObject("{=pwnqk0Nj}Gathered denars from {.%}{SELECTED_ITEM}{.%}");
			textObject3.SetTextVariable("SELECTED_ITEM", _selectedItemObject.Name);
			_soldItemAmountLog = AddDiscreteLog(TextObject.GetEmpty(), textObject2, _soldCount, _selectedItemObjectCount);
			_gatheredDenarsLog = AddDiscreteLog(TextObject.GetEmpty(), textObject3, _gatheredDenars, _targetDenarsToAchieve);
		}

		private void QuestFinishedPlayerBoughtTheGoods()
		{
			AddLog(QuestSuccessPlayerBoughtItemsLogText);
			MobileParty.MainParty.ItemRoster.AddToCounts(_selectedItemObject, _selectedItemObjectCount);
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, base.QuestGiver, _targetDenarsToAchieve);
			GainRenownAction.Apply(Hero.MainHero, 1f);
			RelationshipChangeWithQuestGiver = 1;
			CompleteQuestWithSuccess();
		}

		private bool PlayerBuyClickableOptionCondition(out TextObject explanation)
		{
			if (Hero.MainHero.Gold < _targetDenarsToAchieve)
			{
				explanation = new TextObject("{=d0kbtGYn}You don't have enough gold.");
				return false;
			}
			explanation = null;
			return true;
		}

		protected override void OnTimedOut()
		{
			AddLog(QuestFailedWithTimeOutLogText);
			RelationshipChangeWithQuestGiver = -10;
			CampaignEvents.WarDeclared.ClearListeners(this);
			ChangeCrimeRatingAction.Apply(base.QuestGiver.MapFaction, 5f);
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

		protected override void HourlyTick()
		{
			if (base.IsOngoing && !_underSoldLogAdded && CheckIfPlayerLostItem())
			{
				AddLog(QuestReadyToBeFinishedUnderSoldLogText);
				_daysPassed++;
				_underSoldLogAdded = true;
			}
		}

		private bool CheckIfPlayerLostItem()
		{
			return _soldCount + MobileParty.MainParty.ItemRoster.GetItemNumber(_selectedItemObject) < _selectedItemObjectCount;
		}

		private void OnPlayerInventoryExchange(List<(ItemRosterElement, int)> purchasedItems, List<(ItemRosterElement, int)> soldItems, bool isTrading)
		{
			if (!isTrading || ItemCountLeftToSell <= 0)
			{
				return;
			}
			foreach (var soldItem in soldItems)
			{
				ItemRosterElement itemRosterElement;
				(itemRosterElement, _) = soldItem;
				if (itemRosterElement.EquipmentElement.Item == _selectedItemObject)
				{
					int soldCount = _soldCount;
					itemRosterElement = soldItem.Item1;
					_soldCount = soldCount + itemRosterElement.Amount;
					_gatheredDenars += soldItem.Item2;
				}
			}
			if (ItemCountLeftToSell <= 0)
			{
				AddLog(QuestReadyToBeFinishedLogText);
			}
			_productsUndersold = _gatheredDenars < _targetDenarsToAchieve;
			_soldItemAmountLog.UpdateCurrentProgress(_soldCount);
			_gatheredDenarsLog.UpdateCurrentProgress(_gatheredDenars);
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
			_targetDenarsToAchieve = (int)((float)(_selectedItemObject.Value * _selectedItemObjectCount) * 0.55f);
			_productsUndersold = _gatheredDenars < _targetDenarsToAchieve;
			SetDialogs();
		}
	}

	public class LandLordTheArtOfTheTradeIssueBehaviorTypeDefiner : SaveableTypeDefiner
	{
		public LandLordTheArtOfTheTradeIssueBehaviorTypeDefiner()
			: base(249000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(LandLordTheArtOfTheTradeIssue), 1);
			AddClassDefinition(typeof(LandLordTheArtOfTheTradeIssueQuest), 2);
		}
	}

	private const IssueBase.IssueFrequency LandLordTheArtOfTheTradeIssueFrequency = IssueBase.IssueFrequency.VeryCommon;

	private const float TargetDenarsConstant = 0.55f;

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private bool ConditionsHold(Hero issueGiver)
	{
		if (issueGiver.IsRuralNotable)
		{
			Village village = issueGiver.CurrentSettlement?.Village;
			if (village != null)
			{
				return village.Bound.Town.GetItemPrice(village.VillageType.PrimaryProduction) < village.VillageType.PrimaryProduction.Value;
			}
			return false;
		}
		return false;
	}

	public void OnCheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero))
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnSelected, typeof(LandLordTheArtOfTheTradeIssue), IssueBase.IssueFrequency.VeryCommon));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(LandLordTheArtOfTheTradeIssue), IssueBase.IssueFrequency.VeryCommon));
		}
	}

	private IssueBase OnSelected(in PotentialIssueData pid, Hero issueOwner)
	{
		return new LandLordTheArtOfTheTradeIssue(issueOwner, issueOwner.CurrentSettlement.Village.VillageType.PrimaryProduction);
	}
}
