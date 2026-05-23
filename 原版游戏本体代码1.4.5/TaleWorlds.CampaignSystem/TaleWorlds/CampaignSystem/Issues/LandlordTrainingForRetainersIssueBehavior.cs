using System;
using System.Collections.Generic;
using System.Linq;
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
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues;

public class LandlordTrainingForRetainersIssueBehavior : CampaignBehaviorBase
{
	public class LandlordTrainingForRetainersIssue : IssueBase
	{
		private const int QuestTimeLimit = 60;

		private const int IssueDuration = 30;

		private const int AlternativeSolutionTroopTierRequirement = 2;

		private const int SkillLimit = 120;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.Casualties | AlternativeSolutionScaleFlag.FailureRisk;

		private int BorrowedTroopCount => 3 + TaleWorlds.Library.MathF.Ceiling(17f * base.IssueDifficultyMultiplier);

		protected override bool IssueQuestCanBeDuplicated => false;

		public override int AlternativeSolutionBaseNeededMenCount => 8 + TaleWorlds.Library.MathF.Ceiling(19f * base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 9 + TaleWorlds.Library.MathF.Ceiling(10f * base.IssueDifficultyMultiplier);

		protected override int RewardGold => (int)(2000f + 4000f * base.IssueDifficultyMultiplier);

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=q2aed7tv}Train Troops for {ISSUE_OWNER.NAME}");
				StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject Description
		{
			get
			{
				TextObject textObject = new TextObject("{=va7rEVZn}{ISSUE_OWNER.NAME}, a landowner in {ISSUE_SETTLEMENT}, needs some of his watchmen and retainers to gain some real war experience. {?ISSUE_OWNER.GENDER}She{?}He{\\?} wants you to take them with you on some fairly safe expeditions, such as hunting some bandits. ");
				StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.IssueOwner.CharacterObject, textObject);
				textObject.SetTextVariable("ISSUE_SETTLEMENT", base.IssueSettlement.Name);
				return textObject;
			}
		}

		protected override int CompanionSkillRewardXP => (int)(500f + 700f * base.IssueDifficultyMultiplier);

		public override TextObject IssueBriefByIssueGiver => new TextObject("{=dBphGKTI}Things are getting a bit rough around these parts. I have some lads who help me out with local troublemakers, but they wouldn't last long against real warriors. Maybe you could take them out, show them what actual war is about. I'm not expecting you to make them fit for a noble's retinue, but at least I want to give the bandits around here some pause for thought.[if:convo_bored][ib:closed]");

		public override TextObject IssueAcceptByPlayer => new TextObject("{=1REltXXz}I'll help if I can.");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver => new TextObject("{=uLiRasv1}Maybe you could take them in your party for a while, until they get a bit of experience?[if:convo_thinking]");

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver => new TextObject("{=rutgr1VF}Or if you can assign a companion for a while, they can stay here and train the men... I will also give you some provisions and money for their expenses and your trouble.[if:convo_thinking]");

		public override TextObject IssueQuestSolutionAcceptByPlayer => new TextObject("{=QxEPwLyp}I'll take your men into my party and show them a bit of the world.");

		public override TextObject IssueAlternativeSolutionAcceptByPlayer => new TextObject("{=oT4JNyFp}I will assign one of my companions to train your men.");

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver => new TextObject("{=dE3vxfTo}Excellent.[if:convo_focused_happy] I'm sure they can learn a lot from your veterans.");

		public override TextObject IssueDiscussAlternativeSolution => new TextObject("{=QRRgXOrN}As expected, your veterans have really sharpened up our boys. Please pass on my thanks to them, {?PLAYER.GENDER}madam{?}sir{\\?}.[if:convo_focused_happy][ib:hip]");

		public override bool IsThereAlternativeSolution => true;

		public override bool IsThereLordSolution => false;

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=Ci8NCwgW}{ISSUE_GIVER.LINK} a landowner in {SETTLEMENT}, asked you to train recruits for {?QUEST_GIVER.GENDER}her{?}him{\\?}. {?QUEST_GIVER.GENDER}She{?}He{\\?} gave you {NUMBER_OF_MEN} men, hoping to take them back when once they are veterans.{newline}You sent them with one of your companions {COMPANION.LINK} to hunt down some easy targets. You arranged to meet them in {RETURN_DAYS} days.");
				textObject.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject);
				textObject.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject);
				textObject.SetTextVariable("SETTLEMENT", base.IssueOwner.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				textObject.SetTextVariable("NUMBER_OF_MEN", GetTotalAlternativeSolutionNeededMenCount());
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=AndfZYIJ}Your companion managed to return all of the troops {ISSUE_GIVER.LINK} gave you to train. {?ISSUE_GIVER.GENDER}She{?}He{\\?} sends you the following letter.{newline}{newline}“{?PLAYER.GENDER}Madam{?}Sir{\\?}, Thank you for looking after my men. You honored our agreement, and you have my gratitude. Please accept this {GOLD}{GOLD_ICON}.");
				textObject.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject);
				textObject.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
				textObject.SetTextVariable("GOLD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsLandlordTrainingForRetainersIssue(object o, List<object> collectedObjects)
		{
			((LandlordTrainingForRetainersIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		public LandlordTrainingForRetainersIssue(Hero issueOwner)
			: base(issueOwner, CampaignTime.DaysFromNow(30f))
		{
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.SettlementSecurity)
			{
				return -1f;
			}
			return 0f;
		}

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			return ((hero.GetSkillValue(DefaultSkills.Steward) >= hero.GetSkillValue(DefaultSkills.Leadership)) ? DefaultSkills.Steward : DefaultSkills.Leadership, 120);
		}

		public override bool AlternativeSolutionCondition(out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			RelationshipChangeWithIssueOwner = 5;
			base.IssueOwner.AddPower(10f);
		}

		protected override void AlternativeSolutionEndWithFailureConsequence()
		{
			RelationshipChangeWithIssueOwner = -5;
			base.IssueOwner.AddPower(-10f);
		}

		public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
		{
			return character.Tier >= 2;
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
			return new LandlordTrainingForRetainersIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(60f), base.IssueDifficultyMultiplier, RewardGold);
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
			if (MobileParty.MainParty.MemberRoster.TotalManCount + BorrowedTroopCount > PartyBase.MainParty.PartySizeLimit)
			{
				flag |= PreconditionFlags.PartySizeLimit;
			}
			return flag == PreconditionFlags.None;
		}

		private int GetTier2TroopCount(TroopRoster troopRoster)
		{
			int num = 0;
			foreach (TroopRosterElement item in troopRoster.GetTroopRoster())
			{
				if (item.Character.Tier >= 2 && !item.Character.IsNotTransferableInPartyScreen)
				{
					num += troopRoster.GetTroopCount(item.Character);
				}
			}
			return num;
		}
	}

	public class LandlordTrainingForRetainersIssueQuest : QuestBase
	{
		private bool _popUpOpened;

		private CharacterObject _questGivenChar;

		private CharacterObject _questTargetChar;

		[SaveableField(1)]
		private readonly float _difficultyMultiplier;

		private CampaignTimeControlMode _campaignTimeControlModeCacheForDecisionPopUp;

		[SaveableField(2)]
		private JournalLog _playerStartsQuestLog;

		private int _borrowedTroopCount => 3 + TaleWorlds.Library.MathF.Ceiling(17f * _difficultyMultiplier);

		private TextObject QuestStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=fcRLq8LL}{QUEST_GIVER.LINK}, a landowner in {QUEST_SETTLEMENT}, asked you to train some recruits for {?QUEST_GIVER.GENDER}her{?}him{\\?}. {?QUEST_GIVER.GENDER}She{?}He{\\?} gave you {NUMBER_OF_MEN} men, hoping to take them back when once they have some experience.{newline}The easiest way to train them without putting them in too much danger is to attack weak parties.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_SETTLEMENT", base.QuestGiver.CurrentSettlement.Name);
				textObject.SetTextVariable("NUMBER_OF_MEN", _borrowedTroopCount);
				return textObject;
			}
		}

		private TextObject TotalSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=4RNREbPW}You managed to return all of the troops {QUEST_GIVER.LINK} gave you to train. {?QUEST_GIVER.GENDER}She{?}He{\\?} sends you the following letter.{newline}“{?PLAYER.GENDER}Madam{?}Sir{\\?}, Thank you for looking after my men. You honored our agreement, and you have my gratitude. Please accept this {GOLD}{GOLD_ICON}.”");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("GOLD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		private TextObject PartialSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=yjAHh66a}You managed to return more than half of the troops {QUEST_GIVER.LINK} gave you to train. {?QUEST_GIVER.GENDER}She{?}He{\\?} sends you the following letter. {newline}“{?PLAYER.GENDER}Madam{?}Sir{\\?}, Thank you for returning my men to me. The losses they suffered are somewhat higher than I thought. I can only hope you did what you could to honor our agreement and try to keep them alive.”");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject WeakSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=NXs7kr2B}You managed to return a fraction of the troops {QUEST_GIVER.LINK} gave you to train. {?QUEST_GIVER.GENDER}She{?}He{\\?} sends you the following letter. {newline}“{?PLAYER.GENDER}Madam{?}Sir{\\?}, Thank you for returning my men to me. The losses they suffered are somewhat higher than I thought. I can only hope you did what you could do to honor our agreement and try to keep them alive.”");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject FailLog
		{
			get
			{
				TextObject textObject = new TextObject("{=YBEB7GLa}All the borrowed troops in your party are gone. You are unable to return any of the troops {QUEST_GIVER.LINK} gave you to train. {?QUEST_GIVER.GENDER}She{?}He{\\?} sends you the following letter. {newline}“{?PLAYER.GENDER}Madam{?}Sir{\\?}, I understand that all my men are dead. I asked you to try and keep them alive. I do not know what to say to their kinfolk. This is a breach of my trust.”");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject EpicFailLog
		{
			get
			{
				TextObject textObject = new TextObject("{=eSpRuda1}You have decided to keep the borrowed troops {QUEST_GIVER.LINK} gave you to train. When {?QUEST_GIVER.GENDER}She{?}He{\\?} hears about this {?QUEST_GIVER.GENDER}she{?}he{\\?} sends you the following letter. {newline}“{?PLAYER.GENDER}Madam{?}Sir{\\?}, I made it clear that I expected my men to be returned to me. I consider this a betrayal of my trust.”");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject CancelLogOnWarDeclared
		{
			get
			{
				TextObject textObject = new TextObject("{=TrewB5c7}Now your {?IS_MAP_FACTION}clan{?}kingdom{\\?} is at war with the {QUEST_GIVER.LINK}'s lord. Your agreement with {QUEST_GIVER.LINK} becomes invalid.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("IS_MAP_FACTION", Clan.PlayerClan.IsMapFaction ? 1 : 0);
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

		private TextObject TimeoutLog
		{
			get
			{
				TextObject textObject = new TextObject("{=txtsL6QQ}You failed to train the troops by the time {QUEST_GIVER.LINK} needed them back. {?QUEST_GIVER.GENDER}She{?}He{\\?} sends you the following letter. “{?PLAYER.GENDER}Madam{?}Sir{\\?}, I expected my men to be returned to me. I consider this a breach of our agreement.”");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject CrimeLog
		{
			get
			{
				TextObject textObject = new TextObject("{=faZuFQUF}You are accused in {SETTLEMENT} of a crime, and {QUEST_GIVER.LINK} no longer wants your help.");
				textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
				textObject.SetTextVariable("SETTLEMENT", base.QuestGiver.CurrentSettlement.Name);
				return textObject;
			}
		}

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=q2aed7tv}Train Troops for {ISSUE_OWNER.NAME}");
				textObject.SetCharacterProperties("ISSUE_OWNER", base.QuestGiver.CharacterObject);
				return textObject;
			}
		}

		public override bool IsRemainingTimeHidden => false;

		internal static void AutoGeneratedStaticCollectObjectsLandlordTrainingForRetainersIssueQuest(object o, List<object> collectedObjects)
		{
			((LandlordTrainingForRetainersIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_playerStartsQuestLog);
		}

		internal static object AutoGeneratedGetMemberValue_difficultyMultiplier(object o)
		{
			return ((LandlordTrainingForRetainersIssueQuest)o)._difficultyMultiplier;
		}

		internal static object AutoGeneratedGetMemberValue_playerStartsQuestLog(object o)
		{
			return ((LandlordTrainingForRetainersIssueQuest)o)._playerStartsQuestLog;
		}

		public LandlordTrainingForRetainersIssueQuest(string questId, Hero giverHero, CampaignTime duration, float difficultyMultiplier, int rewardGold)
			: base(questId, giverHero, duration, rewardGold)
		{
			_difficultyMultiplier = difficultyMultiplier;
			SetDialogs();
			InitializeQuestOnCreation();
		}

		protected override void InitializeQuestOnGameLoad()
		{
			_questGivenChar = MBObjectManager.Instance.GetObject<CharacterObject>("borrowed_troop");
			_questGivenChar.SetTransferableInPartyScreen(isTransferable: false);
			_questTargetChar = MBObjectManager.Instance.GetObject<CharacterObject>("veteran_borrowed_troop");
			_questTargetChar.SetTransferableInPartyScreen(isTransferable: false);
			if (_playerStartsQuestLog == null)
			{
				_playerStartsQuestLog = base.JournalEntries.First();
				UpdateQuestTaskStage(_playerStartsQuestLog, PartyBase.MainParty.MemberRoster.GetTroopCount(_questTargetChar));
			}
			SetDialogs();
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.OnTroopsDesertedEvent.AddNonSerializedListener(this, OnTroopsDeserted);
			CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnPlayerBattleEnd);
			CampaignEvents.PlayerUpgradedTroopsEvent.AddNonSerializedListener(this, OnPlayerUpgradedTroops);
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
			CampaignEvents.OnTroopGivenToSettlementEvent.AddNonSerializedListener(this, OnTroopGivenToSettlement);
		}

		protected override void SetDialogs()
		{
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(new TextObject("{=J8qFgwal}Excellent. I'll tell the lads to join your party.[if:convo_relaxed_happy][ib:confident2]")).Condition(() => CharacterObject.OneToOneConversationCharacter == base.QuestGiver.CharacterObject)
				.Consequence(QuestAcceptedConsequences)
				.NpcLineWithVariation("{=7lee0h29}One thing - if one or two die, that's the fortunes of war, things could go even worse if we get raided and have no one who can fight back... But try not to get them all massacred. These men will take some risks for me, but not have their lives thrown away to no purpose.[if:convo_stern]")
				.Variation("{=EaPQ2mm7}One thing - if possible, try not to get them all killed, will you? Green troops aren't much use to me, but corpses are even less.[if:convo_stern]", "UngratefulTag", 1, "MercyTag", -1)
				.CloseDialog();
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(new TextObject("{=r9F1W4KZ}Yes? Have you been able to train my men?[if:convo_astonished]")).Condition(() => CharacterObject.OneToOneConversationCharacter == base.QuestGiver.CharacterObject)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=PVO3YFSq}Yes we are heading out now."))
				.NpcLine(new TextObject("{=weW40mKG}Good to hear that! Safe journeys.[if:convo_relaxed_happy]"))
				.CloseDialog()
				.PlayerOption(new TextObject("{=wErSpkjy}I'm still working on it."))
				.NpcLine(new TextObject("{=weW40mKG}Good to hear that! Safe journeys.[if:convo_relaxed_happy]"))
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
		}

		private void QuestAcceptedConsequences()
		{
			StartQuest();
			_questGivenChar = MBObjectManager.Instance.GetObject<CharacterObject>("borrowed_troop");
			_questGivenChar.SetTransferableInPartyScreen(isTransferable: false);
			_questTargetChar = MBObjectManager.Instance.GetObject<CharacterObject>("veteran_borrowed_troop");
			_questTargetChar.SetTransferableInPartyScreen(isTransferable: false);
			PartyBase.MainParty.AddElementToMemberRoster(_questGivenChar, _borrowedTroopCount);
			PartyBase.MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, 3);
			_playerStartsQuestLog = AddDiscreteLog(QuestStartLog, new TextObject("{=wUb5h4a3}Trained Troops"), PartyBase.MainParty.MemberRoster.GetTroopCount(_questTargetChar), _borrowedTroopCount);
		}

		private void OnPlayerBattleEnd(MapEvent mapEvent)
		{
			if (mapEvent.IsPlayerMapEvent)
			{
				CheckFail();
			}
		}

		private void OnTroopsDeserted(MobileParty mobileParty, TroopRoster desertedTroops)
		{
			if (mobileParty.IsMainParty)
			{
				CheckFail();
			}
		}

		private void OnPlayerUpgradedTroops(CharacterObject upgradeFromTroop, CharacterObject upgradeToTroop, int number)
		{
			if (upgradeFromTroop == _questGivenChar && upgradeToTroop == _questTargetChar && number > 0)
			{
				UpdateQuestTaskStage(_playerStartsQuestLog, PartyBase.MainParty.MemberRoster.GetTroopCount(_questTargetChar));
			}
			if (!CheckFail())
			{
				CheckSuccess();
			}
		}

		private void OnSettlementLeft(MobileParty party, Settlement settlement)
		{
			if (!CheckFail())
			{
				CheckSuccess();
			}
		}

		protected override void HourlyTick()
		{
			if (base.IsOngoing && !CheckFail())
			{
				CheckSuccess();
			}
		}

		private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			if ((mapEvent.IsForcingSupplies || mapEvent.IsForcingVolunteers || mapEvent.IsRaid) && attackerParty == PartyBase.MainParty && mapEvent.MapEventSettlement.IsVillage && mapEvent.MapEventSettlement == base.QuestGiver.CurrentSettlement)
			{
				CriminalActionPerformedTowardsSettlement();
			}
		}

		private void OnTroopGivenToSettlement(Hero giverHero, Settlement recipientSettlement, TroopRoster roster)
		{
			if (giverHero == Hero.MainHero && !CheckFail())
			{
				CheckSuccess();
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
			QuestHelper.CheckWarDeclarationAndFailOrCancelTheQuest(this, faction1, faction2, detail, PlayerDeclaredWarQuestLogText, CancelLogOnWarDeclared);
		}

		private void RemoveBorrowedTroopsFromParty(PartyBase party)
		{
			int troopCount = party.MemberRoster.GetTroopCount(_questTargetChar);
			if (troopCount > 0)
			{
				party.MemberRoster.AddToCounts(_questTargetChar, -troopCount);
			}
			int troopCount2 = party.MemberRoster.GetTroopCount(_questGivenChar);
			if (troopCount2 > 0)
			{
				party.MemberRoster.AddToCounts(_questGivenChar, -troopCount2);
			}
		}

		private void TurnRemainingQuestTroopsIntoNormalTroops(PartyBase party)
		{
			int elementNumber = party.MemberRoster.GetElementNumber(_questGivenChar);
			int elementNumber2 = party.MemberRoster.GetElementNumber(_questTargetChar);
			int elementNumber3 = party.PrisonRoster.GetElementNumber(_questGivenChar);
			int elementNumber4 = party.PrisonRoster.GetElementNumber(_questTargetChar);
			if (elementNumber > 0)
			{
				party.MemberRoster.AddToCounts(_questGivenChar, -elementNumber);
				party.MemberRoster.AddToCounts(base.QuestGiver.Culture.BasicTroop, elementNumber);
			}
			if (elementNumber2 > 0)
			{
				party.MemberRoster.AddToCounts(_questTargetChar, -elementNumber2);
				party.MemberRoster.AddToCounts(base.QuestGiver.Culture.EliteBasicTroop, elementNumber2);
			}
			if (elementNumber3 > 0)
			{
				party.PrisonRoster.AddToCounts(_questGivenChar, -elementNumber3);
				party.PrisonRoster.AddToCounts(base.QuestGiver.Culture.BasicTroop, elementNumber3);
			}
			if (elementNumber4 > 0)
			{
				party.PrisonRoster.AddToCounts(_questTargetChar, -elementNumber4);
				party.PrisonRoster.AddToCounts(base.QuestGiver.Culture.EliteBasicTroop, elementNumber4);
			}
		}

		private bool CheckFail()
		{
			if (PartyBase.MainParty.MemberRoster.GetTroopCount(_questTargetChar) == 0 && PartyBase.MainParty.MemberRoster.GetTroopCount(_questGivenChar) == 0)
			{
				Fail();
				return true;
			}
			return false;
		}

		private void CheckSuccess(bool isConversationEnded = false)
		{
			if (PartyBase.MainParty.MemberRoster.GetTroopCount(_questGivenChar) == 0 && PartyBase.MainParty.MemberRoster.GetTroopCount(_questTargetChar) > 0 && !_popUpOpened && (Campaign.Current.ConversationManager.OneToOneConversationHero == null || isConversationEnded))
			{
				OpenDecisionPopUp();
			}
		}

		private void OpenDecisionPopUp()
		{
			_popUpOpened = true;
			_campaignTimeControlModeCacheForDecisionPopUp = Campaign.Current.TimeControlMode;
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			TextObject textObject = new TextObject("{=LO7EjoY7}The borrowed troops remaining in your party are now all experienced. You can send them back to {QUEST_GIVER.LINK}.");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
			InformationManager.ShowInquiry(new InquiryData("", textObject.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, new TextObject("{=ikDX1Fd7}Send the troops back").ToString(), new TextObject("{=yFahppU2}Hold on to them").ToString(), CompleteQuestSuccessfully, EpicFail));
		}

		private void CompleteQuestSuccessfully()
		{
			Campaign.Current.TimeControlMode = _campaignTimeControlModeCacheForDecisionPopUp;
			int troopCount = PartyBase.MainParty.MemberRoster.GetTroopCount(_questTargetChar);
			if (troopCount > 0)
			{
				PartyBase.MainParty.MemberRoster.AddToCounts(_questTargetChar, -troopCount);
			}
			int troopCount2 = PartyBase.MainParty.MemberRoster.GetTroopCount(_questGivenChar);
			if (troopCount2 > 0)
			{
				PartyBase.MainParty.MemberRoster.AddToCounts(_questGivenChar, -troopCount2);
			}
			if (troopCount >= _borrowedTroopCount)
			{
				TotalSuccess();
			}
			else if ((float)_borrowedTroopCount * 0.5f < (float)troopCount && troopCount < _borrowedTroopCount)
			{
				PartialSuccess();
			}
			else if (0 < troopCount && (float)troopCount <= (float)_borrowedTroopCount * 0.5f)
			{
				WeakSuccess();
			}
		}

		private void TotalSuccess()
		{
			Clan.PlayerClan.AddRenown(2f);
			RelationshipChangeWithQuestGiver = 5;
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 50)
			});
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
			base.QuestGiver.AddPower(10f);
			AddLog(TotalSuccessLog);
			CompleteQuestWithSuccess();
		}

		private void PartialSuccess()
		{
			Clan.PlayerClan.AddRenown(1f);
			RelationshipChangeWithQuestGiver = 3;
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 30)
			});
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
			base.QuestGiver.AddPower(10f);
			AddLog(PartialSuccessLog);
			CompleteQuestWithSuccess();
		}

		private void WeakSuccess()
		{
			Clan.PlayerClan.AddRenown(1f);
			RelationshipChangeWithQuestGiver = 1;
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 10)
			});
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
			base.QuestGiver.AddPower(10f);
			AddLog(WeakSuccessLog);
			CompleteQuestWithSuccess();
		}

		private void Fail()
		{
			RelationshipChangeWithQuestGiver = -5;
			base.QuestGiver.AddPower(-10f);
			AddLog(FailLog);
			CompleteQuestWithFail();
		}

		private void EpicFail()
		{
			Campaign.Current.TimeControlMode = _campaignTimeControlModeCacheForDecisionPopUp;
			RelationshipChangeWithQuestGiver = -10;
			base.QuestGiver.AddPower(-10f);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -10)
			});
			AddLog(EpicFailLog);
			CompleteQuestWithFail();
		}

		private void CriminalActionPerformedTowardsSettlement()
		{
			RelationshipChangeWithQuestGiver = -5;
			base.QuestGiver.AddPower(-10f);
			Tuple<TraitObject, int>[] effectedTraits = new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -50)
			};
			TraitLevelingHelper.OnIssueSolvedThroughQuest(Hero.MainHero, effectedTraits);
			CompleteQuestWithFail(CrimeLog);
		}

		protected override void OnFinalize()
		{
			_questGivenChar = MBObjectManager.Instance.GetObject<CharacterObject>("borrowed_troop");
			_questTargetChar = MBObjectManager.Instance.GetObject<CharacterObject>("veteran_borrowed_troop");
			foreach (MobileParty item in MobileParty.All.ToList())
			{
				if (!item.IsMilitia)
				{
					TurnRemainingQuestTroopsIntoNormalTroops(item.Party);
				}
			}
		}

		protected override void OnBeforeTimedOut(ref bool completeWithSuccess, ref bool doNotResolveTheQuest)
		{
			if (PartyBase.MainParty.MemberRoster.GetTroopCount(_questTargetChar) > 0)
			{
				doNotResolveTheQuest = true;
				if (!_popUpOpened && MobileParty.MainParty.MapEvent == null)
				{
					OpenDecisionPopUp();
				}
			}
		}

		protected override void OnTimedOut()
		{
			RelationshipChangeWithQuestGiver = -5;
			base.QuestGiver.AddPower(-10f);
			RemoveBorrowedTroopsFromParty(PartyBase.MainParty);
			AddLog(TimeoutLog);
		}
	}

	public class LandlordTrainingForRetainersIssueTypeDefiner : SaveableTypeDefiner
	{
		public LandlordTrainingForRetainersIssueTypeDefiner()
			: base(410000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(LandlordTrainingForRetainersIssue), 1);
			AddClassDefinition(typeof(LandlordTrainingForRetainersIssueQuest), 2);
		}
	}

	private const IssueBase.IssueFrequency LandlordTrainingForRetainersIssueFrequency = IssueBase.IssueFrequency.VeryCommon;

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private bool ConditionsHold(Hero issueGiver)
	{
		ItemObject itemObject = issueGiver.CurrentSettlement?.Village?.VillageType?.PrimaryProduction;
		if (issueGiver.IsRuralNotable && issueGiver.CurrentSettlement.IsVillage && itemObject.HasHorseComponent)
		{
			if (itemObject.ItemCategory != DefaultItemCategories.Horse && itemObject.ItemCategory != DefaultItemCategories.NobleHorse)
			{
				return itemObject.ItemCategory == DefaultItemCategories.WarHorse;
			}
			return true;
		}
		return false;
	}

	public void OnCheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero))
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnSelected, typeof(LandlordTrainingForRetainersIssue), IssueBase.IssueFrequency.VeryCommon));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(LandlordTrainingForRetainersIssue), IssueBase.IssueFrequency.VeryCommon));
		}
	}

	private IssueBase OnSelected(in PotentialIssueData pid, Hero issueOwner)
	{
		return new LandlordTrainingForRetainersIssue(issueOwner);
	}
}
