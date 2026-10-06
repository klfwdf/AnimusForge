using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace SandBox.Issues;

public class RivalGangMovingInIssueBehavior : CampaignBehaviorBase
{
	public class RivalGangMovingInIssueTypeDefiner : SaveableTypeDefiner
	{
		public RivalGangMovingInIssueTypeDefiner()
			: base(310000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(RivalGangMovingInIssue), 1);
			AddClassDefinition(typeof(RivalGangMovingInIssueQuest), 2);
		}
	}

	public class RivalGangMovingInIssue : IssueBase
	{
		private const int AlternativeSolutionRelationChange = 5;

		private const int AlternativeSolutionFailRelationChange = -5;

		private const int AlternativeSolutionQuestGiverPowerChange = 10;

		private const int AlternativeSolutionRivalGangLeaderPowerChange = -10;

		private const int AlternativeSolutionFailQuestGiverPowerChange = -10;

		private const int AlternativeSolutionFailSecurityChange = -10;

		private const int AlternativeSolutionRivalGangLeaderRelationChange = -5;

		private const int AlternativeSolutionMinimumTroopTier = 2;

		private const int IssueDuration = 15;

		private const int MinimumRequiredMenCount = 5;

		private const int IssueQuestDuration = 8;

		private const int MeleeSkillValueThreshold = 150;

		private const int RoguerySkillValueThreshold = 120;

		private const int PreparationDurationInDays = 2;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.Casualties | AlternativeSolutionScaleFlag.FailureRisk;

		public override TextObject IssueAlternativeSolutionSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=pzvQ1DkE}Your companion has defeated the rival gang and protected the interests of {QUEST_GIVER.LINK} in {SETTLEMENT}.");
				textObject.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject);
				textObject.SetTextVariable("SETTLEMENT", base.IssueOwner.CurrentSettlement.Name);
				return textObject;
			}
		}

		[SaveableProperty(207)]
		public Hero RivalGangLeader { get; private set; }

		public override int AlternativeSolutionBaseNeededMenCount => 4 + TaleWorlds.Library.MathF.Ceiling(6f * base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 3 + TaleWorlds.Library.MathF.Ceiling(5f * base.IssueDifficultyMultiplier);

		protected override int RewardGold => (int)(600f + 1700f * base.IssueDifficultyMultiplier);

		protected override int CompanionSkillRewardXP => (int)(750f + 1000f * base.IssueDifficultyMultiplier);

		public override TextObject IssueBriefByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=GXk6f9ah}I've got a problem... [ib:confident][if:convo_undecided_closed]And {?TARGET_NOTABLE.GENDER}her{?}his{\\?} name is {TARGET_NOTABLE.LINK}. {?TARGET_NOTABLE.GENDER}Her{?}His{\\?} people have been coming around outside the walls, robbing the dice-players and the drinkers enjoying themselves under our protection. Me and my boys are eager to teach them a lesson but I figure some extra muscle wouldn't hurt.");
				if (base.IssueOwner.RandomInt(2) == 0)
				{
					textObject = new TextObject("{=rgTGzfzI}Yeah. I have a problem all right. [ib:confident][if:convo_undecided_closed]{?TARGET_NOTABLE.GENDER}Her{?}His{\\?} name is {TARGET_NOTABLE.LINK}. {?TARGET_NOTABLE.GENDER}Her{?}His{\\?} people have been bothering shop owners under our protection, demanding money and making threats. Let me tell you something - those shop owners are my cows, and no one else gets to milk them. We're ready to teach these interlopers a lesson, but I could use some help.");
				}
				if (RivalGangLeader != null)
				{
					StringHelpers.SetCharacterProperties("TARGET_NOTABLE", RivalGangLeader.CharacterObject, textObject);
				}
				return textObject;
			}
		}

		public override TextObject IssueAcceptByPlayer => new TextObject("{=kc6vCycY}What exactly do you want me to do?");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=tyyAfWRR}We already had a small scuffle with them recently. [if:convo_mocking_revenge]They'll be waiting for us to come down hard. Instead, we'll hold off for {NUMBER} days. Let them think that we're backing off… Then, after {NUMBER} days, your men and mine will hit them in the middle of the night when they least expect it. I'll send you a messenger when the time comes and we'll strike them down together.");
				textObject.SetTextVariable("NUMBER", 2);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=sSIjPCPO}If you'd rather not go into the fray yourself, [if:convo_mocking_aristocratic]you can leave me one of your companions together with {TROOP_COUNT} or so good men. If they stuck around for {RETURN_DAYS} days or so, I'd count it a very big favor.");
				textObject.SetTextVariable("TROOP_COUNT", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=ymbVPod1}{ISSUE_GIVER.LINK}, a gang leader from {SETTLEMENT}, has told you about a new gang that is trying to get a hold on the town. You asked {COMPANION.LINK} to take {TROOP_COUNT} of your best men to stay with {ISSUE_GIVER.LINK} and help {?ISSUE_GIVER.GENDER}her{?}him{\\?} in the coming gang war. They should return to you in {RETURN_DAYS} days.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", base.IssueOwner.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("TROOP_COUNT", AlternativeSolutionSentTroops.TotalManCount - 1);
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		public override TextObject IssueQuestSolutionAcceptByPlayer => new TextObject("{=LdCte9H0}I'll fight the other gang with you myself.");

		public override TextObject IssueAlternativeSolutionAcceptByPlayer => new TextObject("{=AdbiUqtT}I'm busy, but I will leave a companion and some men.");

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver => new TextObject("{=0enbhess}Thank you. [ib:normal][if:convo_approving]I'm sure your guys are worth their salt..");

		public override TextObject IssueDiscussAlternativeSolution => new TextObject("{=QR0V8Ae5}Our lads are well hidden nearby,[ib:normal][if:convo_excited] waiting for the signal to go get those bastards. I won't forget this little favor you're doing me.");

		public override bool IsThereAlternativeSolution => true;

		public override bool IsThereLordSolution => false;

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=vAjgn7yx}Rival Gang Moving in at {SETTLEMENT}");
				textObject.SetTextVariable("SETTLEMENT", base.IssueSettlement?.Name ?? base.IssueOwner.HomeSettlement.Name);
				return textObject;
			}
		}

		public override TextObject Description => new TextObject("{=H4EVfKAh}Gang leader needs help to beat the rival gang.");

		public override TextObject IssueAsRumorInSettlement
		{
			get
			{
				TextObject textObject = new TextObject("{=C9feTaca}I hear {QUEST_GIVER.LINK} is going to sort it out with {RIVAL_GANG_LEADER.LINK} once and for all.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("RIVAL_GANG_LEADER", RivalGangLeader.CharacterObject, textObject);
				return textObject;
			}
		}

		protected override bool IssueQuestCanBeDuplicated => false;

		public RivalGangMovingInIssue(Hero issueOwner, Hero rivalGangLeader)
			: base(issueOwner, CampaignTime.DaysFromNow(15f))
		{
			RivalGangLeader = rivalGangLeader;
		}

		public override void OnHeroCanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
		{
			if (hero == RivalGangLeader)
			{
				result = false;
			}
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.IssueOwnerPower)
			{
				return -0.2f;
			}
			if (issueEffect == DefaultIssueEffects.SettlementSecurity)
			{
				return -0.5f;
			}
			return 0f;
		}

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			RelationshipChangeWithIssueOwner = 5;
			ChangeRelationAction.ApplyPlayerRelation(RivalGangLeader, -5);
			base.IssueOwner.AddPower(10f);
			RivalGangLeader.AddPower(-10f);
		}

		protected override void AlternativeSolutionEndWithFailureConsequence()
		{
			RelationshipChangeWithIssueOwner = -5;
			base.IssueSettlement.Town.Security += -10f;
			base.IssueOwner.AddPower(-10f);
		}

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			int skillValue = hero.GetSkillValue(DefaultSkills.OneHanded);
			int skillValue2 = hero.GetSkillValue(DefaultSkills.TwoHanded);
			int skillValue3 = hero.GetSkillValue(DefaultSkills.Polearm);
			int skillValue4 = hero.GetSkillValue(DefaultSkills.Roguery);
			if (skillValue >= skillValue2 && skillValue >= skillValue3 && skillValue >= skillValue4)
			{
				return (DefaultSkills.OneHanded, 150);
			}
			if (skillValue2 >= skillValue3 && skillValue2 >= skillValue4)
			{
				return (DefaultSkills.TwoHanded, 150);
			}
			if (skillValue3 < skillValue4)
			{
				return (DefaultSkills.Roguery, 120);
			}
			return (DefaultSkills.Polearm, 150);
		}

		public override bool AlternativeSolutionCondition(out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
		{
			return character.Tier >= 2;
		}

		protected override void OnGameLoad()
		{
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new RivalGangMovingInIssueQuest(questId, base.IssueOwner, RivalGangLeader, 8, RewardGold, base.IssueDifficultyMultiplier);
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
			if (Hero.MainHero.IsWounded)
			{
				flag |= PreconditionFlags.Wounded;
			}
			if (issueGiver.GetRelationWithPlayer() < -10f)
			{
				flag |= PreconditionFlags.Relation;
				relationHero = issueGiver;
			}
			if (MobileParty.MainParty.MemberRoster.TotalHealthyCount < 5)
			{
				flag |= PreconditionFlags.NotEnoughTroops;
			}
			if (base.IssueOwner.CurrentSettlement.OwnerClan == Clan.PlayerClan)
			{
				flag |= PreconditionFlags.PlayerIsOwnerOfSettlement;
			}
			return flag == PreconditionFlags.None;
		}

		public override bool IssueStayAliveConditions()
		{
			if (RivalGangLeader.IsAlive && base.IssueOwner.CurrentSettlement.OwnerClan != Clan.PlayerClan)
			{
				return base.IssueOwner.CurrentSettlement.Town.Security <= 80f;
			}
			return false;
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}

		internal static void AutoGeneratedStaticCollectObjectsRivalGangMovingInIssue(object o, List<object> collectedObjects)
		{
			((RivalGangMovingInIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(RivalGangLeader);
		}

		internal static object AutoGeneratedGetMemberValueRivalGangLeader(object o)
		{
			return ((RivalGangMovingInIssue)o).RivalGangLeader;
		}
	}

	public class RivalGangMovingInIssueQuest : QuestBase
	{
		private const int QuestGiverRelationChangeOnSuccess = 5;

		private const int RivalGangLeaderRelationChangeOnSuccess = -5;

		private const int QuestGiverNotablePowerChangeOnSuccess = 10;

		private const int RivalGangLeaderPowerChangeOnSuccess = -10;

		private const int RenownChangeOnSuccess = 1;

		private const int QuestGiverRelationChangeOnFail = -5;

		private const int QuestGiverRelationChangeOnTimedOut = -5;

		private const int NotablePowerChangeOnFail = -10;

		private const int TownSecurityChangeOnFail = -10;

		private const int RivalGangLeaderRelationChangeOnSuccessfulBetrayal = 5;

		private const int QuestGiverRelationChangeOnSuccessfulBetrayal = -15;

		private const int RivalGangLeaderPowerChangeOnSuccessfulBetrayal = 10;

		private const int QuestGiverRelationChangeOnFailedBetrayal = -10;

		private const int PlayerAttackedQuestGiverHonorChange = -150;

		private const int PlayerAttackedQuestGiverPowerChange = -10;

		private const int NumberOfRegularEnemyTroops = 15;

		private const int PlayerAttackedQuestGiverRelationChange = -8;

		private const int PlayerAttackedQuestGiverSecurityChange = -10;

		private const int NumberOfRegularAllyTroops = 20;

		private const int MaxNumberOfPlayerOwnedTroops = 5;

		private const string AllyGangLeaderHenchmanStringId = "gangster_2";

		private const string RivalGangLeaderHenchmanStringId = "gangster_3";

		private const int PreparationDurationInDays = 2;

		[SaveableField(10)]
		internal readonly Hero _rivalGangLeader;

		[SaveableField(20)]
		private MobileParty _rivalGangLeaderParty;

		private Hero _rivalGangLeaderHenchmanHero;

		[SaveableField(30)]
		private readonly CampaignTime _preparationCompletionTime;

		private Hero _allyGangLeaderHenchmanHero;

		private MobileParty _allyGangLeaderParty;

		[SaveableField(40)]
		private readonly CampaignTime _questTimeoutTime;

		[SaveableField(60)]
		internal readonly float _timeoutDurationInDays;

		[SaveableField(70)]
		internal bool _isFinalStage;

		[SaveableField(80)]
		internal bool _isReadyToBeFinalized;

		[SaveableField(90)]
		internal bool _hasBetrayedQuestGiver;

		private List<TroopRosterElement> _allPlayerTroops;

		private List<CharacterObject> _sentTroops;

		private Hero _partyEngineer;

		private Hero _partyScout;

		private Hero _partyQuartermaster;

		private Hero _partySurgeon;

		[SaveableField(110)]
		private bool _preparationsComplete;

		[SaveableField(120)]
		private int _rewardGold;

		[SaveableField(130)]
		private float _issueDifficulty;

		private Settlement _questSettlement;

		private JournalLog _onQuestStartedLog;

		private JournalLog _onQuestSucceededLog;

		private TextObject OnQuestStartedLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=dav5rmDd}{QUEST_GIVER.LINK}, a gang leader from {SETTLEMENT} has told you about a rival that is trying to get a foothold in {?QUEST_GIVER.GENDER}her{?}his{\\?} town. {?QUEST_GIVER.GENDER}She{?}He{\\?} asked you to wait {DAY_COUNT} days so that the other gang lets its guard down.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", _questSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("DAY_COUNT", 2);
				return textObject;
			}
		}

		private TextObject OnQuestFailedWithRejectionLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=aXMg9M7t}You didn't respond to the messenger {QUEST_GIVER.LINK} sent you. {?QUEST_GIVER.GENDER}She{?}He{\\?} will certainly lose to the rival gang without your help.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject OnQuestFailedWithBetrayalLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=Rf0QqRIX}You have chosen to side with the rival gang leader, {RIVAL_GANG_LEADER.LINK}. {QUEST_GIVER.LINK} must be furious.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("RIVAL_GANG_LEADER", _rivalGangLeader.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject OnQuestFailedWithDefeatLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=du3dpMaV}You were unable to defeat {RIVAL_GANG_LEADER.LINK}'s gang, and thus failed to fulfill your commitment to {QUEST_GIVER.LINK}.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("RIVAL_GANG_LEADER", _rivalGangLeader.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject OnQuestSucceededLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=vpUl7xcy}You have defeated the rival gang and protected the interests of {QUEST_GIVER.LINK} in {SETTLEMENT}.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", _questSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject OnQuestPreperationsCompletedLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=OIBiRTRP}{QUEST_GIVER.LINK} is waiting for you at {SETTLEMENT}.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", _questSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject OnQuestCancelledDueToWarLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=vaUlAZba}Your clan is now at war with {QUEST_GIVER.LINK}. Your agreement with {QUEST_GIVER.LINK} was canceled.");
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

		private TextObject OnQuestCancelledDueToSiegeLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=s1GWSE9Y}{QUEST_GIVER.LINK} cancels your plans due to the siege of {SETTLEMENT}. {?QUEST_GIVER.GENDER}She{?}He{\\?} has worse troubles than {?QUEST_GIVER.GENDER}her{?}his{\\?} quarrel with the rival gang.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", _questSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject PlayerStartedAlleyFightWithRivalGangLeader
		{
			get
			{
				TextObject textObject = new TextObject("{=OeKgpuAv}After your attack on the rival gang's alley, {QUEST_GIVER.LINK} decided to change {?QUEST_GIVER.GENDER}her{?}his{\\?} plans, and doesn't need your assistance anymore. Quest is canceled.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject PlayerStartedAlleyFightWithQuestgiver
		{
			get
			{
				TextObject textObject = new TextObject("{=VPGkIqlh}Your attack on {QUEST_GIVER.LINK}'s gang has angered {?QUEST_GIVER.GENDER}her{?}him{\\?} and {?QUEST_GIVER.GENDER}she{?}he{\\?} broke off the agreement that you had.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject OwnerOfQuestSettlementIsPlayerClanLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=KxEnNEoD}Your clan is now owner of the settlement. As the {?PLAYER.GENDER}lady{?}lord{\\?} of the settlement you cannot get involved in gang wars anymore. Your agreement with the {QUEST_GIVER.LINK} has canceled.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
				return textObject;
			}
		}

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=vAjgn7yx}Rival Gang Moving in at {SETTLEMENT}");
				textObject.SetTextVariable("SETTLEMENT", _questSettlement.Name);
				return textObject;
			}
		}

		public override bool IsRemainingTimeHidden => false;

		public RivalGangMovingInIssueQuest(string questId, Hero questGiver, Hero rivalGangLeader, int duration, int rewardGold, float issueDifficulty)
			: base(questId, questGiver, CampaignTime.DaysFromNow(duration), rewardGold)
		{
			_rivalGangLeader = rivalGangLeader;
			_rewardGold = rewardGold;
			_issueDifficulty = issueDifficulty;
			_timeoutDurationInDays = duration;
			_preparationCompletionTime = CampaignTime.DaysFromNow(2f);
			_questTimeoutTime = CampaignTime.DaysFromNow(_timeoutDurationInDays);
			_sentTroops = new List<CharacterObject>();
			_allPlayerTroops = new List<TroopRosterElement>();
			InitializeQuestSettlement();
			SetDialogs();
			InitializeQuestOnCreation();
		}

		protected override void InitializeQuestOnGameLoad()
		{
			InitializeQuestSettlement();
			SetDialogs();
			Campaign.Current.ConversationManager.AddDialogFlow(GetRivalGangLeaderDialogFlow(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetQuestGiverPreparationCompletedDialogFlow(), this);
			_rivalGangLeaderParty?.SetPartyUsedByQuest(isActivelyUsed: true);
			_sentTroops = new List<CharacterObject>();
			_allPlayerTroops = new List<TroopRosterElement>();
		}

		private void InitializeQuestSettlement()
		{
			_questSettlement = base.QuestGiver.CurrentSettlement;
		}

		protected override void SetDialogs()
		{
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine("{=Fwm0PwVb}Great. As I said we need minimum of {NUMBER} days,[ib:normal][if:convo_mocking_revenge] so they'll let their guard down. I will let you know when it's time. Remember, we wait for the dark of the night to strike.").Condition(delegate
			{
				MBTextManager.SetTextVariable("SETTLEMENT", _questSettlement.EncyclopediaLinkWithName);
				MBTextManager.SetTextVariable("NUMBER", 2);
				return Hero.OneToOneConversationHero == base.QuestGiver;
			})
				.Consequence(OnQuestAccepted)
				.CloseDialog();
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine("{=z43j3Tzq}I'm still gathering my men for the fight. I'll send a runner for you when the time comes.").Condition(delegate
			{
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
				return Hero.OneToOneConversationHero == base.QuestGiver && !_isFinalStage && !_preparationsComplete;
			})
				.BeginPlayerOptions()
				.PlayerOption("{=4IHRAmnA}All right. I am waiting for your runner.")
				.NpcLine("{=xEs830bT}You'll know right away once the preparations are complete.[ib:closed][if:convo_mocking_teasing] Just don't leave town.")
				.CloseDialog()
				.PlayerOption("{=6g8qvD2M}I can't just hang on here forever. Be quick about it.")
				.NpcLine("{=lM7AscLo}I'm getting this together as quickly as I can.[ib:closed][if:convo_nervous]")
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
		}

		private DialogFlow GetRivalGangLeaderDialogFlow()
		{
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine("{=IfeN8lYd}Coming to fight us, eh? Did {QUEST_GIVER.LINK} put you up to this?[ib:aggressive2][if:convo_confused_annoyed] Look, there's no need for bloodshed. This town is big enough for all of us. But... if bloodshed is what you want, we will be happy to provide.").Condition(delegate
			{
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
				return Hero.OneToOneConversationHero == _rivalGangLeaderHenchmanHero && _isReadyToBeFinalized;
			})
				.NpcLine("{=WSJxl2Hu}What I want to say is... [if:convo_mocking_teasing]You don't need to be a part of this. My boss will double whatever {?QUEST_GIVER.GENDER}she{?}he{\\?} is paying you if you join us.")
				.BeginPlayerOptions()
				.PlayerOption("{=GPBja02V}I gave my word to {QUEST_GIVER.LINK}, and I won't be bought.")
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
					{
						Mission.Current.GetMissionBehavior<CombatMissionWithDialogueController>()?.StartFight(hasPlayerChangedSide: false);
					};
				})
				.NpcLine("{=OSgBicif}You will regret this![ib:warrior][if:convo_furious]")
				.CloseDialog()
				.PlayerOption("{=RB4uQpPV}You're going to pay me a lot then, {REWARD}{GOLD_ICON} to be exact. But at that price, I agree.")
				.Condition(delegate
				{
					MBTextManager.SetTextVariable("REWARD", _rewardGold * 2);
					return true;
				})
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
					{
						_hasBetrayedQuestGiver = true;
						Mission.Current.GetMissionBehavior<CombatMissionWithDialogueController>()?.StartFight(hasPlayerChangedSide: true);
					};
				})
				.NpcLine("{=5jW4FVDc}Welcome to our ranks then. [ib:warrior][if:convo_evil_smile]Let's kill those bastards!")
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
		}

		private DialogFlow GetQuestGiverPreparationCompletedDialogFlow()
		{
			return DialogFlow.CreateDialogFlow("start", 125).BeginNpcOptions().NpcOption(new TextObject("{=hM7LSuB1}Good to see you. But we still need to wait until after dusk. {HERO.LINK}'s men may be watching, so let's keep our distance from each other until night falls."), delegate
			{
				StringHelpers.SetCharacterProperties("HERO", _rivalGangLeader.CharacterObject);
				return Hero.OneToOneConversationHero == base.QuestGiver && !_isFinalStage && _preparationCompletionTime.IsPast && (!_preparationsComplete || !CampaignTime.Now.IsNightTime);
			})
				.CloseDialog()
				.NpcOption("{=JxNlB547}Are you ready for the fight?[ib:normal][if:convo_undecided_open]", () => Hero.OneToOneConversationHero == base.QuestGiver && _preparationsComplete && !_isFinalStage && CampaignTime.Now.IsNightTime)
				.EndNpcOptions()
				.BeginPlayerOptions()
				.PlayerOption("{=NzMX0s21}I am ready.")
				.Condition(() => !Hero.MainHero.IsWounded)
				.NpcLine("{=dNjepcKu}Let's finish this![ib:hip][if:convo_mocking_revenge]")
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += rival_gang_start_fight_on_consequence;
				})
				.CloseDialog()
				.PlayerOption("{=B2Donbwz}I need more time.")
				.Condition(() => !Hero.MainHero.IsWounded)
				.NpcLine("{=advPT3WY}You'd better hurry up![ib:closed][if:convo_astonished]")
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += rival_gang_need_more_time_on_consequence;
				})
				.CloseDialog()
				.PlayerOption("{=QaN26CZ5}My wounds are still fresh. I need some time to recover.")
				.Condition(() => Hero.MainHero.IsWounded)
				.NpcLine("{=s0jKaYo0}We must attack before the rival gang hears about our plan. You'd better hurry up![if:convo_astonished]")
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
		}

		public override void OnHeroCanDieInfoIsRequested(Hero hero, KillCharacterAction.KillCharacterActionDetail causeOfDeath, ref bool result)
		{
			if (hero == base.QuestGiver || hero == _rivalGangLeader)
			{
				result = false;
			}
		}

		private void rival_gang_start_fight_on_consequence()
		{
			_isFinalStage = true;
			if (Mission.Current != null)
			{
				Mission.Current.EndMission();
			}
			Campaign.Current.GameMenuManager.SetNextMenu("rival_gang_quest_before_fight");
		}

		private void rival_gang_need_more_time_on_consequence()
		{
			if (Campaign.Current.CurrentMenuContext.GameMenu.StringId == "rival_gang_quest_wait_duration_is_over")
			{
				Campaign.Current.GameMenuManager.SetNextMenu("town_wait_menus");
			}
		}

		private void AddQuestGiverGangLeaderOnSuccessDialogFlow()
		{
			Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 125).NpcLine("{=zNPzh5jO}Ah! Now that was as good a fight as any I've had. Here, take this purse, It is all yours as {QUEST_GIVER.LINK} has promised.[ib:hip2][if:convo_huge_smile]").Condition(delegate
			{
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
				return base.IsOngoing && Hero.OneToOneConversationHero == _allyGangLeaderHenchmanHero;
			})
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += OnQuestSucceeded;
				})
				.CloseDialog());
		}

		private CharacterObject GetTroopTypeTemplateForDifficulty()
		{
			int difficultyRange = MBMath.ClampInt(TaleWorlds.Library.MathF.Ceiling(_issueDifficulty / 0.1f), 1, 10);
			CharacterObject characterObject = ((difficultyRange == 1) ? CharacterObject.All.FirstOrDefault((CharacterObject t) => t.StringId == "looter") : ((difficultyRange != 10) ? CharacterObject.All.FirstOrDefault((CharacterObject t) => t.StringId == "mercenary_" + (difficultyRange - 1)) : CharacterObject.All.FirstOrDefault((CharacterObject t) => t.StringId == "mercenary_8")));
			if (characterObject == null)
			{
				Debug.FailedAssert("Can't find troop in rival gang leader quest", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Issues\\RivalGangMovingInIssueBehavior.cs", "GetTroopTypeTemplateForDifficulty", 806);
				characterObject = CharacterObject.All.First((CharacterObject t) => t.IsBasicTroop && t.IsSoldier);
			}
			return characterObject;
		}

		internal void StartAlleyBattle()
		{
			CreateRivalGangLeaderParty();
			CreateAllyGangLeaderParty();
			PreparePlayerParty();
			PlayerEncounter.RestartPlayerEncounter(_rivalGangLeaderParty.Party, PartyBase.MainParty, forcePlayerOutFromSettlement: false);
			PlayerEncounter.StartBattle();
			_allyGangLeaderParty.MapEventSide = PlayerEncounter.Battle.GetMapEventSide(PlayerEncounter.Battle.PlayerSide);
			GameMenu.ActivateGameMenu("rival_gang_quest_after_fight");
			_isReadyToBeFinalized = true;
			PlayerEncounter.StartCombatMissionWithDialogueInTownCenter(_rivalGangLeaderHenchmanHero.CharacterObject);
		}

		private void CreateRivalGangLeaderParty()
		{
			TextObject textObject = new TextObject("{=u4jhIFwG}{GANG_LEADER}'s Party");
			textObject.SetTextVariable("RIVAL_GANG_LEADER", _rivalGangLeader.Name);
			textObject.SetTextVariable("GANG_LEADER", _rivalGangLeader.Name);
			Hideout closestHideout = SettlementHelper.FindNearestHideoutToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All, (Settlement x) => x.IsActive);
			Clan clan = Clan.BanditFactions.FirstOrDefaultQ((Clan t) => t.Culture == closestHideout.Settlement.Culture);
			_rivalGangLeaderParty = CustomPartyComponent.CreateCustomPartyWithTroopRoster(_questSettlement.GatePosition, 1f, _questSettlement, textObject, clan, TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), null);
			_rivalGangLeaderParty.SetPartyUsedByQuest(isActivelyUsed: true);
			CharacterObject troopTypeTemplateForDifficulty = GetTroopTypeTemplateForDifficulty();
			_rivalGangLeaderParty.MemberRoster.AddToCounts(troopTypeTemplateForDifficulty, 15);
			CharacterObject template = MBObjectManager.Instance.GetObject<CharacterObject>("gangster_3");
			_rivalGangLeaderHenchmanHero = HeroCreator.CreateSpecialHero(template);
			TextObject textObject2 = new TextObject("{=zJqEdDiq}Henchman of {GANG_LEADER}");
			textObject2.SetTextVariable("GANG_LEADER", _rivalGangLeader.Name);
			_rivalGangLeaderHenchmanHero.SetName(textObject2, textObject2);
			_rivalGangLeaderHenchmanHero.HiddenInEncyclopedia = true;
			_rivalGangLeaderHenchmanHero.Culture = _rivalGangLeader.Culture;
			_rivalGangLeaderHenchmanHero.SetNewOccupation(Occupation.Special);
			_rivalGangLeaderHenchmanHero.ChangeState(Hero.CharacterStates.Active);
			_rivalGangLeaderParty.MemberRoster.AddToCounts(_rivalGangLeaderHenchmanHero.CharacterObject, 1);
			_rivalGangLeaderParty.IgnoreByOtherPartiesTill(CampaignTime.Never);
			EnterSettlementAction.ApplyForParty(_rivalGangLeaderParty, _questSettlement);
		}

		private void CreateAllyGangLeaderParty()
		{
			TextObject textObject = new TextObject("{=u4jhIFwG}{GANG_LEADER}'s Party");
			textObject.SetTextVariable("GANG_LEADER", base.QuestGiver.Name);
			Hideout closestHideout = SettlementHelper.FindNearestHideoutToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All, (Settlement x) => x.IsActive);
			Clan clan = Clan.BanditFactions.FirstOrDefaultQ((Clan t) => t.Culture == closestHideout.Settlement.Culture);
			_allyGangLeaderParty = CustomPartyComponent.CreateCustomPartyWithTroopRoster(_questSettlement.GatePosition, 1f, _questSettlement, textObject, clan, TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), null);
			_allyGangLeaderParty.SetPartyUsedByQuest(isActivelyUsed: true);
			CharacterObject troopTypeTemplateForDifficulty = GetTroopTypeTemplateForDifficulty();
			_allyGangLeaderParty.MemberRoster.AddToCounts(troopTypeTemplateForDifficulty, 20);
			CharacterObject template = MBObjectManager.Instance.GetObject<CharacterObject>("gangster_2");
			_allyGangLeaderHenchmanHero = HeroCreator.CreateSpecialHero(template);
			TextObject textObject2 = new TextObject("{=zJqEdDiq}Henchman of {GANG_LEADER}");
			textObject2.SetTextVariable("GANG_LEADER", base.QuestGiver.Name);
			_allyGangLeaderHenchmanHero.SetName(textObject2, textObject2);
			_allyGangLeaderHenchmanHero.HiddenInEncyclopedia = true;
			_allyGangLeaderHenchmanHero.Culture = base.QuestGiver.Culture;
			_allyGangLeaderHenchmanHero.ChangeState(Hero.CharacterStates.Active);
			_allyGangLeaderParty.MemberRoster.AddToCounts(_allyGangLeaderHenchmanHero.CharacterObject, 1);
			_allyGangLeaderParty.IgnoreByOtherPartiesTill(CampaignTime.Never);
			EnterSettlementAction.ApplyForParty(_allyGangLeaderParty, _questSettlement);
		}

		private void PreparePlayerParty()
		{
			_allPlayerTroops.Clear();
			foreach (TroopRosterElement item in PartyBase.MainParty.MemberRoster.GetTroopRoster())
			{
				if (!item.Character.IsPlayerCharacter)
				{
					_allPlayerTroops.Add(item);
				}
			}
			_partyEngineer = MobileParty.MainParty.GetRoleHolder(PartyRole.Engineer);
			_partyScout = MobileParty.MainParty.GetRoleHolder(PartyRole.Scout);
			_partyQuartermaster = MobileParty.MainParty.GetRoleHolder(PartyRole.Quartermaster);
			_partySurgeon = MobileParty.MainParty.GetRoleHolder(PartyRole.Surgeon);
			PartyBase.MainParty.MemberRoster.RemoveIf((TroopRosterElement t) => !t.Character.IsPlayerCharacter);
			if (_allPlayerTroops.IsEmpty())
			{
				return;
			}
			_sentTroops.Clear();
			int num = 5;
			foreach (TroopRosterElement item2 in _allPlayerTroops.OrderByDescending((TroopRosterElement t) => t.Character.Level))
			{
				if (num <= 0)
				{
					break;
				}
				for (int num2 = 0; num2 < item2.Number - item2.WoundedNumber; num2++)
				{
					if (num <= 0)
					{
						break;
					}
					_sentTroops.Add(item2.Character);
					num--;
				}
			}
			foreach (CharacterObject sentTroop in _sentTroops)
			{
				PartyBase.MainParty.MemberRoster.AddToCounts(sentTroop, 1);
			}
		}

		internal void HandlePlayerEncounterResult(bool hasPlayerWon)
		{
			PlayerEncounter.Finish(forcePlayerOutFromSettlement: false);
			EncounterManager.StartSettlementEncounter(MobileParty.MainParty, _questSettlement);
			TroopRoster troopRoster = PartyBase.MainParty.MemberRoster.CloneRosterData();
			PartyBase.MainParty.MemberRoster.RemoveIf((TroopRosterElement t) => !t.Character.IsPlayerCharacter);
			foreach (TroopRosterElement playerTroop in _allPlayerTroops)
			{
				int num = troopRoster.FindIndexOfTroop(playerTroop.Character);
				int num2 = playerTroop.Number;
				int num3 = playerTroop.WoundedNumber;
				int num4 = playerTroop.Xp;
				if (num >= 0)
				{
					TroopRosterElement elementCopyAtIndex = troopRoster.GetElementCopyAtIndex(num);
					num2 -= _sentTroops.Count((CharacterObject t) => t == playerTroop.Character) - elementCopyAtIndex.Number;
					num3 += elementCopyAtIndex.WoundedNumber;
					num4 += elementCopyAtIndex.Xp;
				}
				else if (_sentTroops.Contains(playerTroop.Character))
				{
					num2 -= _sentTroops.Count((CharacterObject t) => t == playerTroop.Character);
				}
				PartyBase.MainParty.MemberRoster.AddToCounts(playerTroop.Character, num2, insertAtFront: false, num3, num4);
			}
			MobileParty.MainParty.SetPartyEngineer(_partyEngineer);
			MobileParty.MainParty.SetPartyScout(_partyScout);
			MobileParty.MainParty.SetPartyQuartermaster(_partyQuartermaster);
			MobileParty.MainParty.SetPartySurgeon(_partySurgeon);
			if (_rivalGangLeader.PartyBelongedTo == _rivalGangLeaderParty)
			{
				_rivalGangLeaderParty.MemberRoster.AddToCounts(_rivalGangLeader.CharacterObject, -1);
			}
			if (hasPlayerWon)
			{
				if (!_hasBetrayedQuestGiver)
				{
					AddQuestGiverGangLeaderOnSuccessDialogFlow();
					SpawnAllyHenchmanAfterMissionSuccess();
					PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(LocationComplex.Current.GetLocationOfCharacter(_allyGangLeaderHenchmanHero), null, _allyGangLeaderHenchmanHero.CharacterObject);
				}
				else
				{
					OnBattleWonWithBetrayal();
				}
			}
			else if (!_hasBetrayedQuestGiver)
			{
				OnQuestFailedWithDefeat();
			}
			else
			{
				OnBattleLostWithBetrayal();
			}
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
			CampaignEvents.AlleyClearedByPlayer.AddNonSerializedListener(this, OnAlleyClearedByPlayer);
			CampaignEvents.AlleyOccupiedByPlayer.AddNonSerializedListener(this, OnAlleyOccupiedByPlayer);
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
			CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, OnSiegeEventStarted);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
			CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
		}

		private void SpawnAllyHenchmanAfterMissionSuccess()
		{
			Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(_allyGangLeaderHenchmanHero.CharacterObject.Race, "_settlement");
			LocationCharacter locationCharacter = new LocationCharacter(new AgentData(new SimpleAgentOrigin(_allyGangLeaderHenchmanHero.CharacterObject)).Monster(monsterWithSuffix), SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "npc_common", fixedLocation: true, LocationCharacter.CharacterRelations.Neutral, null, useCivilianEquipment: true);
			LocationComplex.Current.GetLocationWithId("center").AddCharacter(locationCharacter);
		}

		private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			if (settlement == base.QuestGiver.CurrentSettlement && newOwner == Hero.MainHero)
			{
				AddLog(OwnerOfQuestSettlementIsPlayerClanLogText);
				base.QuestGiver.AddPower(-10f);
				ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -5);
				CompleteQuestWithCancel();
			}
		}

		public override void OnHeroCanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
		{
			if (hero == _rivalGangLeader)
			{
				result = false;
			}
		}

		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (base.QuestGiver.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				CompleteQuestWithCancel(OnQuestCancelledDueToWarLogText);
			}
		}

		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
		{
			QuestHelper.CheckWarDeclarationAndFailOrCancelTheQuest(this, faction1, faction2, detail, PlayerDeclaredWarQuestLogText, OnQuestCancelledDueToWarLogText);
		}

		private void OnSiegeEventStarted(SiegeEvent siegeEvent)
		{
			if (siegeEvent.BesiegedSettlement == _questSettlement)
			{
				AddLog(OnQuestCancelledDueToSiegeLogText);
				CompleteQuestWithCancel();
			}
		}

		protected override void HourlyTick()
		{
			if (Instance != null && Instance.IsOngoing && (2f - Instance._preparationCompletionTime.RemainingDaysFromNow) / 2f >= 1f && !_preparationsComplete && CampaignTime.Now.IsNightTime)
			{
				OnGuestGiverPreparationsCompleted();
			}
		}

		private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
		{
			if (victim == _rivalGangLeader)
			{
				TextObject textObject = ((detail == KillCharacterAction.KillCharacterActionDetail.Lost) ? TargetHeroDisappearedLogText : TargetHeroDiedLogText);
				StringHelpers.SetCharacterProperties("QUEST_TARGET", _rivalGangLeader.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				AddLog(textObject);
				CompleteQuestWithCancel();
			}
		}

		private void OnPlayerAlleyFightEnd(Alley alley)
		{
			if (!_isReadyToBeFinalized)
			{
				if (alley.Owner == _rivalGangLeader)
				{
					OnPlayerAttackedRivalGangAlley();
				}
				else if (alley.Owner == base.QuestGiver)
				{
					OnPlayerAttackedQuestGiverAlley();
				}
			}
		}

		private void OnAlleyClearedByPlayer(Alley alley)
		{
			OnPlayerAlleyFightEnd(alley);
		}

		private void OnAlleyOccupiedByPlayer(Alley alley, TroopRoster troops)
		{
			OnPlayerAlleyFightEnd(alley);
		}

		private void OnPlayerAttackedRivalGangAlley()
		{
			AddLog(PlayerStartedAlleyFightWithRivalGangLeader);
			CompleteQuestWithCancel();
		}

		private void OnPlayerAttackedQuestGiverAlley()
		{
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -150)
			});
			base.QuestGiver.AddPower(-10f);
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -8);
			_questSettlement.Town.Security += -10f;
			AddLog(PlayerStartedAlleyFightWithQuestgiver);
			CompleteQuestWithFail();
		}

		protected override void OnTimedOut()
		{
			OnQuestFailedWithRejectionOrTimeout();
		}

		private void OnGuestGiverPreparationsCompleted()
		{
			_preparationsComplete = true;
			if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement == _questSettlement && Campaign.Current.CurrentMenuContext != null && Campaign.Current.CurrentMenuContext.GameMenu.StringId == "town_wait_menus")
			{
				Campaign.Current.CurrentMenuContext.SwitchToMenu("rival_gang_quest_wait_duration_is_over");
			}
			TextObject textObject = new TextObject("{=DUKbtlNb}{QUEST_GIVER.LINK} has finally sent a messenger telling you it's time to meet {?QUEST_GIVER.GENDER}her{?}him{\\?} and join the fight.");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
			AddLog(OnQuestPreperationsCompletedLogText);
			MBInformationManager.AddQuickInformation(textObject);
		}

		private void OnQuestAccepted()
		{
			StartQuest();
			_onQuestStartedLog = AddLog(OnQuestStartedLogText);
			Campaign.Current.ConversationManager.AddDialogFlow(GetRivalGangLeaderDialogFlow(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetQuestGiverPreparationCompletedDialogFlow(), this);
		}

		private void OnQuestSucceeded()
		{
			_onQuestSucceededLog = AddLog(OnQuestSucceededLogText);
			GainRenownAction.Apply(Hero.MainHero, 1f);
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, _rewardGold);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 50)
			});
			base.QuestGiver.AddPower(10f);
			_rivalGangLeader.AddPower(-10f);
			RelationshipChangeWithQuestGiver = 5;
			ChangeRelationAction.ApplyPlayerRelation(_rivalGangLeader, -5);
			GameMenu.ExitToLast();
			GameMenu.ActivateGameMenu("town");
			CompleteQuestWithSuccess();
		}

		private void OnQuestFailedWithRejectionOrTimeout()
		{
			AddLog(OnQuestFailedWithRejectionLogText);
			TraitLevelingHelper.OnIssueFailed(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -20)
			});
			RelationshipChangeWithQuestGiver = -5;
			ApplyQuestFailConsequences();
		}

		private void OnBattleWonWithBetrayal()
		{
			AddLog(OnQuestFailedWithBetrayalLogText);
			RelationshipChangeWithQuestGiver = -15;
			if (!_rivalGangLeader.IsDead)
			{
				ChangeRelationAction.ApplyPlayerRelation(_rivalGangLeader, 5);
			}
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, _rewardGold * 2);
			TraitLevelingHelper.OnIssueSolvedThroughBetrayal(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -100)
			});
			_rivalGangLeader.AddPower(10f);
			GameMenu.SwitchToMenu("town");
			ApplyQuestFailConsequences();
			CompleteQuestWithBetrayal();
		}

		private void OnBattleLostWithBetrayal()
		{
			AddLog(OnQuestFailedWithBetrayalLogText);
			RelationshipChangeWithQuestGiver = -10;
			if (!_rivalGangLeader.IsDead)
			{
				ChangeRelationAction.ApplyPlayerRelation(_rivalGangLeader, -5);
			}
			_rivalGangLeader.AddPower(-10f);
			TraitLevelingHelper.OnIssueSolvedThroughBetrayal(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -100)
			});
			GameMenu.SwitchToMenu("town");
			ApplyQuestFailConsequences();
			CompleteQuestWithBetrayal();
		}

		private void OnQuestFailedWithDefeat()
		{
			RelationshipChangeWithQuestGiver = -5;
			GameMenu.SwitchToMenu("town");
			AddLog(OnQuestFailedWithDefeatLogText);
			ApplyQuestFailConsequences();
			CompleteQuestWithFail();
		}

		private void ApplyQuestFailConsequences()
		{
			base.QuestGiver.AddPower(-10f);
			_questSettlement.Town.Security += -10f;
			if (_rivalGangLeaderParty != null && _rivalGangLeaderParty.IsActive)
			{
				DestroyPartyAction.Apply(null, _rivalGangLeaderParty);
			}
		}

		protected override void OnFinalize()
		{
			if (_rivalGangLeaderParty != null && _rivalGangLeaderParty.IsActive)
			{
				DestroyPartyAction.Apply(null, _rivalGangLeaderParty);
			}
			if (_allyGangLeaderParty != null && _allyGangLeaderParty.IsActive)
			{
				DestroyPartyAction.Apply(null, _allyGangLeaderParty);
			}
			if (_allyGangLeaderHenchmanHero != null && _allyGangLeaderHenchmanHero.IsAlive)
			{
				_allyGangLeaderHenchmanHero.SetNewOccupation(Occupation.Special);
				KillCharacterAction.ApplyByRemove(_allyGangLeaderHenchmanHero);
			}
			if (_rivalGangLeaderHenchmanHero != null && _rivalGangLeaderHenchmanHero.IsAlive)
			{
				_rivalGangLeaderHenchmanHero.SetNewOccupation(Occupation.NotAssigned);
				KillCharacterAction.ApplyByRemove(_rivalGangLeaderHenchmanHero);
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsRivalGangMovingInIssueQuest(object o, List<object> collectedObjects)
		{
			((RivalGangMovingInIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_rivalGangLeader);
			collectedObjects.Add(_rivalGangLeaderParty);
			CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(_preparationCompletionTime, collectedObjects);
			CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(_questTimeoutTime, collectedObjects);
		}

		internal static object AutoGeneratedGetMemberValue_rivalGangLeader(object o)
		{
			return ((RivalGangMovingInIssueQuest)o)._rivalGangLeader;
		}

		internal static object AutoGeneratedGetMemberValue_timeoutDurationInDays(object o)
		{
			return ((RivalGangMovingInIssueQuest)o)._timeoutDurationInDays;
		}

		internal static object AutoGeneratedGetMemberValue_isFinalStage(object o)
		{
			return ((RivalGangMovingInIssueQuest)o)._isFinalStage;
		}

		internal static object AutoGeneratedGetMemberValue_isReadyToBeFinalized(object o)
		{
			return ((RivalGangMovingInIssueQuest)o)._isReadyToBeFinalized;
		}

		internal static object AutoGeneratedGetMemberValue_hasBetrayedQuestGiver(object o)
		{
			return ((RivalGangMovingInIssueQuest)o)._hasBetrayedQuestGiver;
		}

		internal static object AutoGeneratedGetMemberValue_rivalGangLeaderParty(object o)
		{
			return ((RivalGangMovingInIssueQuest)o)._rivalGangLeaderParty;
		}

		internal static object AutoGeneratedGetMemberValue_preparationCompletionTime(object o)
		{
			return ((RivalGangMovingInIssueQuest)o)._preparationCompletionTime;
		}

		internal static object AutoGeneratedGetMemberValue_questTimeoutTime(object o)
		{
			return ((RivalGangMovingInIssueQuest)o)._questTimeoutTime;
		}

		internal static object AutoGeneratedGetMemberValue_preparationsComplete(object o)
		{
			return ((RivalGangMovingInIssueQuest)o)._preparationsComplete;
		}

		internal static object AutoGeneratedGetMemberValue_rewardGold(object o)
		{
			return ((RivalGangMovingInIssueQuest)o)._rewardGold;
		}

		internal static object AutoGeneratedGetMemberValue_issueDifficulty(object o)
		{
			return ((RivalGangMovingInIssueQuest)o)._issueDifficulty;
		}
	}

	private const IssueBase.IssueFrequency RivalGangLeaderIssueFrequency = IssueBase.IssueFrequency.Common;

	private RivalGangMovingInIssueQuest _cachedQuest;

	private static RivalGangMovingInIssueQuest Instance
	{
		get
		{
			RivalGangMovingInIssueBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<RivalGangMovingInIssueBehavior>();
			if (campaignBehavior._cachedQuest != null && campaignBehavior._cachedQuest.IsOngoing)
			{
				return campaignBehavior._cachedQuest;
			}
			foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
			{
				if (quest is RivalGangMovingInIssueQuest cachedQuest)
				{
					campaignBehavior._cachedQuest = cachedQuest;
					return campaignBehavior._cachedQuest;
				}
			}
			return null;
		}
	}

	private void OnCheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero))
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnStartIssue, typeof(RivalGangMovingInIssue), IssueBase.IssueFrequency.Common));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(RivalGangMovingInIssue), IssueBase.IssueFrequency.Common));
		}
	}

	private IssueBase OnStartIssue(in PotentialIssueData pid, Hero issueOwner)
	{
		Hero rivalGangLeader = GetRivalGangLeader(issueOwner);
		return new RivalGangMovingInIssue(issueOwner, rivalGangLeader);
	}

	private static void rival_gang_wait_duration_is_over_menu_on_init(MenuCallbackArgs args)
	{
		Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
		TextObject text = new TextObject("{=9Kr9pjGs}{QUEST_GIVER.LINK} has prepared {?QUEST_GIVER.GENDER}her{?}his{\\?} men and is waiting for you.");
		StringHelpers.SetCharacterProperties("QUEST_GIVER", Instance.QuestGiver.CharacterObject);
		MBTextManager.SetTextVariable("MENU_TEXT", text);
	}

	private bool ConditionsHold(Hero issueGiver)
	{
		if (issueGiver.IsGangLeader && issueGiver.CurrentSettlement != null && issueGiver.CurrentSettlement.IsTown && issueGiver.CurrentSettlement.Town.Security <= 60f)
		{
			return GetRivalGangLeader(issueGiver) != null;
		}
		return false;
	}

	private void rival_gang_quest_wait_duration_is_over_yes_consequence(MenuCallbackArgs args)
	{
		CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, null, noHorse: true, noWeapon: true), new ConversationCharacterData(Instance.QuestGiver.CharacterObject, null, noHorse: true, noWeapon: true));
	}

	private Hero GetRivalGangLeader(Hero issueOwner)
	{
		Hero result = null;
		foreach (Hero notable in issueOwner.CurrentSettlement.Notables)
		{
			if (notable != issueOwner && notable.IsGangLeader && notable.CanHaveCampaignIssues())
			{
				result = notable;
				break;
			}
		}
		return result;
	}

	private bool rival_gang_quest_wait_duration_is_over_yes_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Continue;
		return true;
	}

	private bool rival_gang_quest_wait_duration_is_over_no_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return true;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
	}

	private void OnSessionLaunched(CampaignGameStarter gameStarter)
	{
		gameStarter.AddGameMenu("rival_gang_quest_before_fight", "", rival_gang_quest_before_fight_init, GameMenu.MenuOverlayType.SettlementWithBoth);
		gameStarter.AddGameMenu("rival_gang_quest_after_fight", "", rival_gang_quest_after_fight_init, GameMenu.MenuOverlayType.SettlementWithBoth);
		gameStarter.AddGameMenu("rival_gang_quest_wait_duration_is_over", "{MENU_TEXT}", rival_gang_wait_duration_is_over_menu_on_init);
		gameStarter.AddGameMenuOption("rival_gang_quest_wait_duration_is_over", "rival_gang_quest_wait_duration_is_over_yes", "{=aka03VdU}Meet {?QUEST_GIVER.GENDER}her{?}him{\\?} now", rival_gang_quest_wait_duration_is_over_yes_condition, rival_gang_quest_wait_duration_is_over_yes_consequence);
		gameStarter.AddGameMenuOption("rival_gang_quest_wait_duration_is_over", "rival_gang_quest_wait_duration_is_over_no", "{=NIzQb6nT}Leave and meet {?QUEST_GIVER.GENDER}her{?}him{\\?} later", rival_gang_quest_wait_duration_is_over_no_condition, rival_gang_quest_wait_duration_is_over_no_consequence, isLeave: true);
	}

	private void rival_gang_quest_wait_duration_is_over_no_consequence(MenuCallbackArgs args)
	{
		Campaign.Current.CurrentMenuContext.SwitchToMenu("town_wait_menus");
	}

	private static void rival_gang_quest_before_fight_init(MenuCallbackArgs args)
	{
		if (Instance != null && Instance._isFinalStage)
		{
			Instance.StartAlleyBattle();
		}
	}

	private static void rival_gang_quest_after_fight_init(MenuCallbackArgs args)
	{
		if (Instance != null && Instance._isReadyToBeFinalized)
		{
			bool hasPlayerWon = PlayerEncounter.Battle.WinningSide == PlayerEncounter.Battle.PlayerSide;
			PlayerEncounter.Current.FinalizeBattle();
			Instance.HandlePlayerEncounterResult(hasPlayerWon);
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	[GameMenuInitializationHandler("rival_gang_quest_after_fight")]
	[GameMenuInitializationHandler("rival_gang_quest_wait_duration_is_over")]
	private static void game_menu_rival_gang_quest_end_on_init(MenuCallbackArgs args)
	{
		Settlement currentSettlement = Settlement.CurrentSettlement;
		if (currentSettlement != null)
		{
			args.MenuContext.SetBackgroundMeshName(currentSettlement.SettlementComponent.WaitMeshName);
		}
	}
}
