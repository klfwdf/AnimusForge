using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues;

public class LordNeedsGarrisonTroopsIssueQuestBehavior : CampaignBehaviorBase
{
	public class LordNeedsGarrisonTroopsIssue : IssueBase
	{
		private const int QuestDurationInDays = 30;

		private const int CompanionRequiredSkillLevel = 120;

		[SaveableField(60)]
		private Settlement _settlement;

		[SaveableField(30)]
		private CharacterObject _neededTroopType;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.FailureRisk;

		private int NumberOfTroopToBeRecruited => 3 + (int)(base.IssueDifficultyMultiplier * 18f);

		public override int AlternativeSolutionBaseNeededMenCount => 5 + MathF.Ceiling(8f * base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 3 + MathF.Ceiling(4f * base.IssueDifficultyMultiplier);

		protected override int RewardGold
		{
			get
			{
				int num = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(_neededTroopType, Hero.MainHero).RoundedResultNumber * NumberOfTroopToBeRecruited;
				return (int)(1500f + (float)num * 1.5f);
			}
		}

		public override TextObject IssueBriefByIssueGiver => new TextObject("{=ZuTvTGsh}These wars have taken a toll on my men. The bravest often fall first, they say, and fewer and fewer families are willing to let their sons join my banner. But the wars don't stop because I have problems.[if:convo_undecided_closed][ib:closed]");

		public override TextObject IssueAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=tTM6nPul}What can I do for you, {?ISSUE_OWNER.GENDER}madam{?}sir{\\?}?");
				StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject IssueQuestSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=driH06vI}I need more recruits in {SETTLEMENT}'s garrison. Since I'll be elsewhere... maybe you can recruit {NUMBER_OF_TROOP_TO_BE_RECRUITED} {TROOP_TYPE} and bring them to the garrison for me?[if:convo_undecided_open][ib:normal]");
				textObject.SetTextVariable("SETTLEMENT", _settlement.Name);
				textObject.SetTextVariable("TROOP_TYPE", _neededTroopType.EncyclopediaLinkWithName);
				textObject.SetTextVariable("NUMBER_OF_TROOP_TO_BE_RECRUITED", NumberOfTroopToBeRecruited);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=igXcCqdo}One of your trusted companions who knows how to lead men can go around with {ALTERNATIVE_SOLUTION_MAN_COUNT} horsemen and pick some up. One way or the other I will pay {REWARD_GOLD}{GOLD_ICON} denars in return for your services. What do you say?[if:convo_thinking]");
				textObject.SetTextVariable("ALTERNATIVE_SOLUTION_MAN_COUNT", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("REWARD_GOLD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		public override TextObject IssueQuestSolutionAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=YHSm72Ln}I'll find your recruits and bring them to {SETTLEMENT} garrison.");
				textObject.SetTextVariable("SETTLEMENT", _settlement.Name);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=JPclWyyr}My companion can handle it... So, {NUMBER_OF_TROOP_TO_BE_RECRUITED} {TROOP_TYPE} to {SETTLEMENT}.");
				textObject.SetTextVariable("SETTLEMENT", _settlement.Name);
				textObject.SetTextVariable("TROOP_TYPE", _neededTroopType.EncyclopediaLinkWithName);
				textObject.SetTextVariable("NUMBER_OF_TROOP_TO_BE_RECRUITED", NumberOfTroopToBeRecruited);
				return textObject;
			}
		}

		public override TextObject IssueDiscussAlternativeSolution
		{
			get
			{
				TextObject textObject = new TextObject("{=lWrmxsYR}I haven't heard any news from {SETTLEMENT}, but I realize it might take some time for your men to deliver the recruits.");
				textObject.SetTextVariable("SETTLEMENT", _settlement.Name);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver => new TextObject("{=WUWzyzWI}Thank you. Your help will be remembered.");

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=M560TDza}{ISSUE_OWNER.LINK}, the {?ISSUE_OWNER.GENDER}lady{?}lord{\\?} of {QUEST_SETTLEMENT}, told you that {?ISSUE_OWNER.GENDER}she{?}he{\\?} needs more troops in {?ISSUE_OWNER.GENDER}her{?}his{\\?} garrison. {?ISSUE_OWNER.GENDER}She{?}He{\\?} is willing to pay {REWARD}{GOLD_ICON} for your services. You asked your companion to deploy {NUMBER_OF_TROOP_TO_BE_RECRUITED} {TROOP_TYPE} troops to {QUEST_SETTLEMENT}'s garrison.");
				StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.IssueOwner.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_SETTLEMENT", _settlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("TROOP_TYPE", _neededTroopType.EncyclopediaLinkWithName);
				textObject.SetTextVariable("REWARD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				textObject.SetTextVariable("NUMBER_OF_TROOP_TO_BE_RECRUITED", NumberOfTroopToBeRecruited);
				return textObject;
			}
		}

		public override bool IsThereAlternativeSolution => true;

		public override bool IsThereLordSolution => false;

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=g6Ra6LUY}{ISSUE_OWNER.NAME} Needs Garrison Troops in {SETTLEMENT}");
				StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.IssueOwner.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", _settlement.Name);
				return textObject;
			}
		}

		public override TextObject Description
		{
			get
			{
				TextObject textObject = new TextObject("{=BOAaF6x5}{ISSUE_OWNER.NAME} asks for help to increase troop levels in {SETTLEMENT}");
				StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.IssueOwner.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", _settlement.Name);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=sfFkYm0a}Your companion has successfully brought the troops {ISSUE_OWNER.LINK} requested. You received {REWARD}{GOLD_ICON}.");
				StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.IssueOwner.CharacterObject, textObject);
				textObject.SetTextVariable("REWARD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		protected override int CompanionSkillRewardXP => (int)(800f + 900f * base.IssueDifficultyMultiplier);

		internal static void AutoGeneratedStaticCollectObjectsLordNeedsGarrisonTroopsIssue(object o, List<object> collectedObjects)
		{
			((LordNeedsGarrisonTroopsIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_settlement);
			collectedObjects.Add(_neededTroopType);
		}

		internal static object AutoGeneratedGetMemberValue_settlement(object o)
		{
			return ((LordNeedsGarrisonTroopsIssue)o)._settlement;
		}

		internal static object AutoGeneratedGetMemberValue_neededTroopType(object o)
		{
			return ((LordNeedsGarrisonTroopsIssue)o)._neededTroopType;
		}

		public LordNeedsGarrisonTroopsIssue(Hero issueOwner, Settlement selectedSettlement)
			: base(issueOwner, CampaignTime.DaysFromNow(30f))
		{
			_settlement = selectedSettlement;
			_neededTroopType = CharacterHelper.GetTroopTree(base.IssueOwner.Culture.BasicTroop, 3f, 3f).GetRandomElementInefficiently();
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.SettlementSecurity)
			{
				return -0.5f;
			}
			return 0f;
		}

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			return ((hero.GetSkillValue(DefaultSkills.Leadership) >= hero.GetSkillValue(DefaultSkills.Steward)) ? DefaultSkills.Leadership : DefaultSkills.Steward, 120);
		}

		public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 0, mountedRequired: true);
		}

		public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
		{
			return character.IsMounted;
		}

		public override bool AlternativeSolutionCondition(out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 0, mountedRequired: true);
		}

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			GainRenownAction.Apply(Hero.MainHero, 1f);
			RelationshipChangeWithIssueOwner = 2;
		}

		public override IssueFrequency GetFrequency()
		{
			return IssueFrequency.Common;
		}

		public override bool IssueStayAliveConditions()
		{
			bool flag = false;
			if (_settlement.IsTown)
			{
				MobileParty garrisonParty = _settlement.Town.GarrisonParty;
				flag = garrisonParty != null && garrisonParty.MemberRoster.TotalRegulars < 200;
			}
			else if (_settlement.IsCastle)
			{
				MobileParty garrisonParty2 = _settlement.Town.GarrisonParty;
				flag = garrisonParty2 != null && garrisonParty2.MemberRoster.TotalRegulars < 160;
			}
			if (_settlement.OwnerClan == base.IssueOwner.Clan && flag && !base.IssueOwner.IsDead)
			{
				return base.IssueOwner.Clan != Clan.PlayerClan;
			}
			return false;
		}

		protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out PreconditionFlags flags, out Hero relationHero, out SkillObject skill, out int requiredGold)
		{
			skill = null;
			relationHero = null;
			requiredGold = 0;
			flags = PreconditionFlags.None;
			if (issueGiver.GetRelationWithPlayer() < -10f)
			{
				flags |= PreconditionFlags.Relation;
				relationHero = issueGiver;
			}
			if (Hero.MainHero.IsKingdomLeader)
			{
				flags |= PreconditionFlags.MainHeroIsKingdomLeader;
			}
			if (issueGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
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
			return new LordNeedsGarrisonTroopsIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(30f), RewardGold, _settlement, NumberOfTroopToBeRecruited, _neededTroopType);
		}

		protected override void AlternativeSolutionEndWithFailureConsequence()
		{
			RelationshipChangeWithIssueOwner = -5;
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}
	}

	public class LordNeedsGarrisonTroopsIssueQuest : QuestBase
	{
		internal Settlement _settlement;

		[SaveableField(10)]
		private string _settlementStringID;

		private int _collectedTroopAmount;

		[SaveableField(20)]
		private int _requestedTroopAmount;

		[SaveableField(30)]
		private int _rewardGold;

		[SaveableField(40)]
		private CharacterObject _requestedTroopType;

		internal CharacterObject _selectedCharacterToTalk;

		[SaveableField(50)]
		private JournalLog _playerStartsQuestLog;

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=g6Ra6LUY}{ISSUE_OWNER.NAME} Needs Garrison Troops in {SETTLEMENT}");
				StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", _settlement.Name);
				return textObject;
			}
		}

		public override bool IsRemainingTimeHidden => false;

		private TextObject PlayerStartsQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=FViaQrbV}{QUEST_GIVER.LINK}, the {?QUEST_GIVER.GENDER}lady{?}lord{\\?} of {QUEST_SETTLEMENT}, told you that {?QUEST_GIVER.GENDER}she{?}he{\\?} needs more troops in {?QUEST_GIVER.GENDER}her{?}his{\\?} garrison. {?QUEST_GIVER.GENDER}She{?}He{\\?} is willing to pay {REWARD}{GOLD_ICON} for your services. {?QUEST_GIVER.GENDER}She{?}He{\\?} asked you to deliver {NUMBER_OF_TROOP_TO_BE_RECRUITED} {TROOP_TYPE} troops to garrison commander in {QUEST_SETTLEMENT}.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("TROOP_TYPE", _requestedTroopType.Name);
				textObject.SetTextVariable("REWARD", _rewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				textObject.SetTextVariable("NUMBER_OF_TROOP_TO_BE_RECRUITED", _requestedTroopAmount);
				textObject.SetTextVariable("QUEST_SETTLEMENT", _settlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject SuccessQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=UEn466Y6}You have successfully brought the troops {QUEST_GIVER.LINK} requested. You received {REWARD} gold in return for your service.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("REWARD", _rewardGold);
				return textObject;
			}
		}

		private TextObject QuestGiverLostTheSettlementLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=zS68eOsl}{QUEST_GIVER.LINK} has lost {SETTLEMENT} and your agreement with {?QUEST_GIVER.GENDER}her{?}his{\\?} canceled.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", _settlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject QuestFailedWarDeclaredLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=JIWVeTMD}Your clan is now at war with {QUEST_GIVER.LINK}'s realm. Your agreement with {QUEST_GIVER.LINK} was canceled.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", _settlement.EncyclopediaLinkWithName);
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

		private TextObject TimeOutLogText => new TextObject("{=cnaxgN5b}You have failed to bring the troops in time.");

		internal static void AutoGeneratedStaticCollectObjectsLordNeedsGarrisonTroopsIssueQuest(object o, List<object> collectedObjects)
		{
			((LordNeedsGarrisonTroopsIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_requestedTroopType);
			collectedObjects.Add(_playerStartsQuestLog);
		}

		internal static object AutoGeneratedGetMemberValue_settlementStringID(object o)
		{
			return ((LordNeedsGarrisonTroopsIssueQuest)o)._settlementStringID;
		}

		internal static object AutoGeneratedGetMemberValue_requestedTroopAmount(object o)
		{
			return ((LordNeedsGarrisonTroopsIssueQuest)o)._requestedTroopAmount;
		}

		internal static object AutoGeneratedGetMemberValue_rewardGold(object o)
		{
			return ((LordNeedsGarrisonTroopsIssueQuest)o)._rewardGold;
		}

		internal static object AutoGeneratedGetMemberValue_requestedTroopType(object o)
		{
			return ((LordNeedsGarrisonTroopsIssueQuest)o)._requestedTroopType;
		}

		internal static object AutoGeneratedGetMemberValue_playerStartsQuestLog(object o)
		{
			return ((LordNeedsGarrisonTroopsIssueQuest)o)._playerStartsQuestLog;
		}

		public LordNeedsGarrisonTroopsIssueQuest(string questId, Hero giverHero, CampaignTime duration, int rewardGold, Settlement selectedSettlement, int requestedTroopAmount, CharacterObject requestedTroopType)
			: base(questId, giverHero, duration, rewardGold)
		{
			_settlement = selectedSettlement;
			_settlementStringID = selectedSettlement.StringId;
			_requestedTroopAmount = requestedTroopAmount;
			_collectedTroopAmount = 0;
			_requestedTroopType = requestedTroopType;
			_rewardGold = rewardGold;
			SetDialogs();
			AddTrackedObject(_settlement);
			InitializeQuestOnCreation();
		}

		private bool DialogCondition()
		{
			return Hero.OneToOneConversationHero == base.QuestGiver;
		}

		protected override void SetDialogs()
		{
			Campaign.Current.ConversationManager.AddDialogFlow(GetGarrisonCommanderDialogFlow(), this);
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(new TextObject("{=9iZg4vpz}Thank you. You will be rewarded when you are done.[if:convo_mocking_aristocratic]")).Condition(DialogCondition)
				.Consequence(QuestAcceptedConsequences)
				.CloseDialog();
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(new TextObject("{=o6BunhbE}Have you brought my troops?[if:convo_undecided_open]")).Condition(DialogCondition)
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += MapEventHelper.OnConversationEnd;
				})
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=eC4laxrj}I'm still out recruiting."))
				.NpcLine(new TextObject("{=TxxbCbUc}Good. I have faith in you...[if:convo_mocking_aristocratic]"))
				.CloseDialog()
				.PlayerOption(new TextObject("{=DbraLcwM}I need more time to find proper men."))
				.NpcLine(new TextObject("{=Mw5bJ5Fb}Every day without a proper garrison is a day that we're vulnerable. Do hurry, if you can.[if:convo_normal]"))
				.CloseDialog()
				.EndPlayerOptions();
		}

		private void QuestAcceptedConsequences()
		{
			StartQuest();
			_playerStartsQuestLog = AddDiscreteLog(PlayerStartsQuestLogText, new TextObject("{=WIb9VvEM}Collected Troops"), _collectedTroopAmount, _requestedTroopAmount);
		}

		private DialogFlow GetGarrisonCommanderDialogFlow()
		{
			TextObject textObject = new TextObject("{=abda9slW}We were waiting for you, {?PLAYER.GENDER}madam{?}sir{\\?}. Have you brought the troops that our {?ISSUE_OWNER.GENDER}lady{?}lord{\\?} requested?");
			StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.QuestGiver.CharacterObject, textObject);
			return DialogFlow.CreateDialogFlow("start", 300).NpcLine(textObject).Condition(() => CharacterObject.OneToOneConversationCharacter == _selectedCharacterToTalk)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=ooHbl6JU}Here are your men."))
				.ClickableCondition(PlayerGiveTroopsToGarrisonCommanderCondition)
				.NpcLine(new TextObject("{=Ouy4sN5b}Thank you.[if:convo_mocking_aristocratic]"))
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += PlayerTransferredTroopsToGarrisonCommander;
				})
				.CloseDialog()
				.PlayerOption(new TextObject("{=G5tyQj6N}Not yet."))
				.NpcLine(new TextObject("{=yPOZd1wb}Very well. We'll keep waiting.[if:convo_normal]"))
				.CloseDialog()
				.EndPlayerOptions();
		}

		private void PlayerTransferredTroopsToGarrisonCommander()
		{
			foreach (TroopRosterElement item in MobileParty.MainParty.MemberRoster.GetTroopRoster())
			{
				if (item.Character == _requestedTroopType)
				{
					MobileParty.MainParty.MemberRoster.AddToCounts(_requestedTroopType, -_requestedTroopAmount);
					break;
				}
			}
			AddLog(SuccessQuestLogText);
			RelationshipChangeWithQuestGiver = 2;
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, _rewardGold);
			GainRenownAction.Apply(Hero.MainHero, 1f);
			CompleteQuestWithSuccess();
		}

		private bool PlayerGiveTroopsToGarrisonCommanderCondition(out TextObject explanation)
		{
			int num = 0;
			foreach (TroopRosterElement item in MobileParty.MainParty.MemberRoster.GetTroopRoster())
			{
				if (item.Character == _requestedTroopType)
				{
					num = item.Number;
					break;
				}
			}
			if (num < _requestedTroopAmount)
			{
				explanation = new TextObject("{=VFO2aQ4l}You don't have enough men.");
				return false;
			}
			explanation = null;
			return true;
		}

		protected override void InitializeQuestOnGameLoad()
		{
			_settlement = Settlement.Find(_settlementStringID);
			CalculateTroopAmount();
			SetDialogs();
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
			CampaignEvents.OnPartySizeChangedEvent.AddNonSerializedListener(this, OnPartySizeChanged);
		}

		private void OnPartySizeChanged(PartyBase party)
		{
			if (party.IsMobile && party.MobileParty.IsMainParty)
			{
				CalculateTroopAmount();
				_collectedTroopAmount = MBMath.ClampInt(_collectedTroopAmount, 0, _requestedTroopAmount);
				_playerStartsQuestLog.UpdateCurrentProgress(_collectedTroopAmount);
			}
		}

		private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
			{
				QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
			}
		}

		private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			if (settlement == _settlement && _settlement.OwnerClan != base.QuestGiver.Clan)
			{
				AddLog(QuestGiverLostTheSettlementLogText);
				CompleteQuestWithCancel();
			}
		}

		protected override void HourlyTick()
		{
			if (base.IsOngoing)
			{
				CalculateTroopAmount();
				_collectedTroopAmount = MBMath.ClampInt(_collectedTroopAmount, 0, _requestedTroopAmount);
				_playerStartsQuestLog.UpdateCurrentProgress(_collectedTroopAmount);
			}
		}

		private void CalculateTroopAmount()
		{
			foreach (TroopRosterElement item in MobileParty.MainParty.MemberRoster.GetTroopRoster())
			{
				if (item.Character == _requestedTroopType)
				{
					_collectedTroopAmount = MobileParty.MainParty.MemberRoster.GetTroopCount(item.Character);
					break;
				}
			}
		}

		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (base.QuestGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				CompleteQuestWithCancel(QuestFailedWarDeclaredLogText);
			}
		}

		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
		{
			QuestHelper.CheckWarDeclarationAndFailOrCancelTheQuest(this, faction1, faction2, detail, PlayerDeclaredWarQuestLogText, QuestFailedWarDeclaredLogText);
		}

		protected override void OnTimedOut()
		{
			AddLog(TimeOutLogText);
			RelationshipChangeWithQuestGiver = -5;
		}
	}

	public class LordNeedsGarrisonTroopsIssueQuestTypeDefiner : SaveableTypeDefiner
	{
		public LordNeedsGarrisonTroopsIssueQuestTypeDefiner()
			: base(5080000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(LordNeedsGarrisonTroopsIssue), 1);
			AddClassDefinition(typeof(LordNeedsGarrisonTroopsIssueQuest), 2);
		}
	}

	private const IssueBase.IssueFrequency LordNeedsGarrisonTroopsIssueFrequency = IssueBase.IssueFrequency.Common;

	private LordNeedsGarrisonTroopsIssueQuest _cachedQuest;

	private static LordNeedsGarrisonTroopsIssueQuest Instance
	{
		get
		{
			LordNeedsGarrisonTroopsIssueQuestBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<LordNeedsGarrisonTroopsIssueQuestBehavior>();
			if (campaignBehavior._cachedQuest != null && campaignBehavior._cachedQuest.IsOngoing)
			{
				return campaignBehavior._cachedQuest;
			}
			foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
			{
				if (quest is LordNeedsGarrisonTroopsIssueQuest cachedQuest)
				{
					campaignBehavior._cachedQuest = cachedQuest;
					return campaignBehavior._cachedQuest;
				}
			}
			return null;
		}
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
	}

	private void OnSessionLaunched(CampaignGameStarter gameStarter)
	{
		string optionText = "{=FirEOQaI}Talk to the garrison commander";
		gameStarter.AddGameMenuOption("town", "talk_to_garrison_commander_town", optionText, talk_to_garrison_commander_on_condition, talk_to_garrison_commander_on_consequence, isLeave: false, 0);
		gameStarter.AddGameMenuOption("town_guard", "talk_to_garrison_commander_town", optionText, talk_to_garrison_commander_on_condition, talk_to_garrison_commander_on_consequence, isLeave: false, 2);
		gameStarter.AddGameMenuOption("castle_guard", "talk_to_garrison_commander_castle", optionText, talk_to_garrison_commander_on_condition, talk_to_garrison_commander_on_consequence, isLeave: false, 2);
	}

	private bool talk_to_garrison_commander_on_condition(MenuCallbackArgs args)
	{
		if (Instance != null)
		{
			if (Settlement.CurrentSettlement == Instance._settlement && Instance._settlement.Town?.GarrisonParty == null)
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("{=JmoOJX4e}There is no one in the garrison to receive the troops requested. You should wait until someone arrives.");
			}
			args.optionLeaveType = GameMenuOption.LeaveType.LeaveTroopsAndFlee;
			args.OptionQuestData = GameMenuOption.IssueQuestFlags.ActiveIssue;
			return Settlement.CurrentSettlement == Instance._settlement;
		}
		return false;
	}

	private void talk_to_garrison_commander_on_consequence(MenuCallbackArgs args)
	{
		CharacterObject characterObject = Instance._settlement.OwnerClan.Culture.EliteBasicTroop;
		foreach (TroopRosterElement item in Instance._settlement.Town.GarrisonParty.MemberRoster.GetTroopRoster())
		{
			if (item.Character.IsInfantry && characterObject.Level < item.Character.Level)
			{
				characterObject = item.Character;
			}
		}
		Instance._selectedCharacterToTalk = characterObject;
		CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty), new ConversationCharacterData(Instance._selectedCharacterToTalk, Instance._settlement.Town?.GarrisonParty.Party));
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private bool ConditionsHold(Hero issueGiver, out Settlement selectedSettlement)
	{
		selectedSettlement = null;
		if (issueGiver.IsLord && issueGiver.Clan.Leader == issueGiver && !issueGiver.IsMinorFactionHero && issueGiver.Clan != Clan.PlayerClan)
		{
			foreach (Settlement settlement in issueGiver.Clan.Settlements)
			{
				if (settlement.IsCastle)
				{
					MobileParty garrisonParty = settlement.Town.GarrisonParty;
					if (garrisonParty != null && garrisonParty.MemberRoster.TotalHealthyCount < 120)
					{
						selectedSettlement = settlement;
						break;
					}
				}
				if (settlement.IsTown)
				{
					MobileParty garrisonParty2 = settlement.Town.GarrisonParty;
					if (garrisonParty2 != null && garrisonParty2.MemberRoster.TotalHealthyCount < 150)
					{
						selectedSettlement = settlement;
						break;
					}
				}
			}
			return selectedSettlement != null;
		}
		return false;
	}

	public void OnCheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero, out var selectedSettlement))
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnSelected, typeof(LordNeedsGarrisonTroopsIssue), IssueBase.IssueFrequency.Common, selectedSettlement));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(LordNeedsGarrisonTroopsIssue), IssueBase.IssueFrequency.Common));
		}
	}

	private IssueBase OnSelected(in PotentialIssueData pid, Hero issueOwner)
	{
		return new LordNeedsGarrisonTroopsIssue(issueOwner, pid.RelatedObject as Settlement);
	}
}
