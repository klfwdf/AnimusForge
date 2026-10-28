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
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues;

public class LesserNobleRevoltIssueBehavior : CampaignBehaviorBase
{
	public class LesserNobleRevoltIssue : IssueBase
	{
		private const int MinimumRequiredMenCount = 50;

		private const int IssueDuration = 20;

		private const int QuestTimeLimit = 60;

		private const int AlternativeSolutionTroopTierRequirement = 2;

		private const int CompanionRequiredSkillLevel = 120;

		[SaveableField(20)]
		private TextObject _lesserNobleName;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.Casualties | AlternativeSolutionScaleFlag.FailureRisk;

		public override int AlternativeSolutionBaseNeededMenCount => (int)(26f + 34f * base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => (int)(7f + 13f * base.IssueDifficultyMultiplier);

		protected override int RewardGold => 150 * base.IssueOwner.RandomIntWithSeed((uint)IssueCreationTime.ElapsedDaysUntilNow, 30, 60);

		private TextObject LesserNobleTitle => GetLesserNobleTitle(base.IssueOwner);

		public override TextObject IssueBriefByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=BbqzSmHG}Yes... As you know, the wars this realm has fought have been very costly. The great lords in particular are hard-put to raise the armies that are key to this land's defense. Some of us have proposed increasing the hearth taxes paid by the peasantry, which is a key source of revenue. I don't know if this law will be approved, but rumors of it have gotten out in my district, and a lot of the farmers are restless. Now, most of us are doing their part to keep a lid on things. But there's one, {MALE_LESSER_NOBLE_NAME}, who I am ashamed to say was once a {TOP_TIER_CAV_TITLE} under my banner, who's been going around the countryside, stirring up trouble. He's gathered a following and they have a whole list of demands, including a ban on new taxes, that would frankly cripple our realm's ability to raise the money needed to defend itself. I'd take care of him myself, but that wouldn't look good. Maybe you can help?[if:convo_thinking][ib:closed2]");
				textObject.SetTextVariable("MALE_LESSER_NOBLE_NAME", _lesserNobleName);
				textObject.SetTextVariable("TOP_TIER_CAV_TITLE", LesserNobleTitle);
				return textObject;
			}
		}

		public override TextObject IssueAcceptByPlayer => new TextObject("{=8m8wIhZW}Maybe. What do you need exactly?");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver => new TextObject("{=muFvSMrb}His band has been going about from village to village. Find him and stop him. You can tell him he's guilty of sedition, and if he doesn't disperse, then you can use force.[if:convo_thinking][ib:hip2]");

		public override TextObject IssuePlayerResponseAfterAlternativeExplanation => new TextObject("{=sMCN7eCp}Is there any other way to solve this problem?");

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=bbXFwya1}Not that I can think of. But if you don't want to do it, you can nominate someone from your party. It's not going to be easy, though. Whoever you name should be a good leader and have a good knowledge of tactics, and should take along at least {ALTERNATIVE_SOLUTION_NEEDED_MAN_COUNT} men. I'm ready to pay {REWARD}{GOLD_ICON} for your service. And if it comes to a fight, you can take whatever loot you want as spoils of war.[if:convo_thinking]");
				textObject.SetTextVariable("ALTERNATIVE_SOLUTION_NEEDED_MAN_COUNT", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("REWARD", RewardGold);
				return textObject;
			}
		}

		public override TextObject IssueQuestSolutionAcceptByPlayer => new TextObject("{=a9pK8pyq}I'll find them and put a stop to what they're doing.");

		public override TextObject IssueAlternativeSolutionAcceptByPlayer => new TextObject("{=hmaZ6Snq}Don't worry. I will send my best men to deal with this.");

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver => new TextObject("{=yLqDHZv9}Thank you.[if:convo_relaxed_happy] I hope your men deal with them as soon as possible.");

		public override bool IsThereAlternativeSolution => true;

		public override bool IsThereLordSolution => false;

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=fhAsNPoE}{ISSUE_GIVER.LINK}, lord of {SETTLEMENT}, says that a {MALE_LESSER_NOBLE_TITLE} named {MALE_LESSER_NOBLE_NAME} is stirring up unrest in the countryside. You asked your companion to deal with {MALE_LESSER_NOBLE_NAME}'s band. They should rejoin your party in {RETURN_DAYS} days");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject);
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				textObject.SetTextVariable("MALE_LESSER_NOBLE_TITLE", LesserNobleTitle);
				textObject.SetTextVariable("MALE_LESSER_NOBLE_NAME", _lesserNobleName);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=aKrzoAsh}You have defeated {MALE_LESSER_NOBLE_NAME} and helped {ISSUE_OWNER.LINK} as promised. You received {REWARD}{GOLD_ICON} in return for your service.");
				StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.IssueOwner.CharacterObject, textObject);
				textObject.SetTextVariable("MALE_LESSER_NOBLE_NAME", _lesserNobleName);
				textObject.SetTextVariable("REWARD", RewardGold);
				return textObject;
			}
		}

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=xxZeGhqf}{MALE_LESSER_NOBLE_NAME}{.o} Revolt");
				textObject.SetTextVariable("MALE_LESSER_NOBLE_NAME", _lesserNobleName);
				return textObject;
			}
		}

		public override TextObject Description
		{
			get
			{
				TextObject textObject = new TextObject("{=bYTSR4AG}{ISSUE_GIVER.LINK} wants you to stop the {MALE_LESSER_NOBLE_NAME}'s movement before it turns into a strong resistance.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject);
				textObject.SetTextVariable("MALE_LESSER_NOBLE_NAME", _lesserNobleName);
				return textObject;
			}
		}

		protected override int CompanionSkillRewardXP => (int)(800f + 1000f * base.IssueDifficultyMultiplier);

		internal static void AutoGeneratedStaticCollectObjectsLesserNobleRevoltIssue(object o, List<object> collectedObjects)
		{
			((LesserNobleRevoltIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_lesserNobleName);
		}

		internal static object AutoGeneratedGetMemberValue_lesserNobleName(object o)
		{
			return ((LesserNobleRevoltIssue)o)._lesserNobleName;
		}

		public LesserNobleRevoltIssue(Hero issueOwner)
			: base(issueOwner, CampaignTime.DaysFromNow(20f))
		{
			MBReadOnlyList<TextObject> nameListForCulture = NameGenerator.Current.GetNameListForCulture(base.IssueOwner.Culture, isFemale: false);
			_lesserNobleName = nameListForCulture.GetRandomElement();
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.ClanInfluence)
			{
				return -1f;
			}
			return 0f;
		}

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			return ((hero.GetSkillValue(DefaultSkills.Leadership) >= hero.GetSkillValue(DefaultSkills.Scouting)) ? DefaultSkills.Leadership : DefaultSkills.Scouting, 120);
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
			return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		public override IssueFrequency GetFrequency()
		{
			return IssueFrequency.Rare;
		}

		public override bool IssueStayAliveConditions()
		{
			if (base.IssueOwner.Clan.Fiefs.Count >= 2)
			{
				return base.IssueOwner.MapFaction.IsKingdomFaction;
			}
			return false;
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
			if (MobileParty.MainParty.MemberRoster.TotalHealthyCount < 50)
			{
				flag |= PreconditionFlags.NotEnoughTroops;
			}
			if (issueGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				flag |= PreconditionFlags.AtWar;
			}
			if (Clan.PlayerClan.Kingdom != issueGiver.MapFaction)
			{
				flag |= PreconditionFlags.NotInSameFaction;
			}
			if (Hero.MainHero.IsKingdomLeader)
			{
				flag |= PreconditionFlags.MainHeroIsKingdomLeader;
			}
			return flag == PreconditionFlags.None;
		}

		protected override void OnGameLoad()
		{
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new LesserNobleRevoltIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(60f), RewardGold, _lesserNobleName);
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			GainRenownAction.Apply(Hero.MainHero, 1f);
			TraitLevelingHelper.OnIssueSolvedThroughAlternativeSolution(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 15)
			});
			RelationshipChangeWithIssueOwner = 5;
		}

		protected override void AlternativeSolutionEndWithFailureConsequence()
		{
			RelationshipChangeWithIssueOwner = -5;
			TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -15)
			});
		}
	}

	public class LesserNobleRevoltIssueQuest : QuestBase
	{
		private const int NeededVillageVisitCount = 20;

		private const int GoOutFromSettlementAfterHours = 5;

		private const int MinTroopToRecruit = 3;

		private const int MaxTroopToRecruit = 4;

		private const int NoblePartyMemberMaxLimitAtStart = 40;

		private const int LesserNoblePartySizeLimit = 80;

		[SaveableField(10)]
		private bool _checkForEventEnd;

		[SaveableField(20)]
		private TextObject _lesserNobleName;

		[SaveableField(30)]
		private MobileParty _lesserNobleParty;

		[SaveableField(50)]
		private List<Settlement> _suitableVillagesToVisitList;

		[SaveableField(60)]
		private List<Settlement> _visitedVillagesList;

		[SaveableField(70)]
		private bool _persuasionTriedOnce;

		[SaveableField(80)]
		private bool _firstTalkIsDone;

		[SaveableField(90)]
		private int _goOutFromSettlementAfterHoursCounter;

		[SaveableField(120)]
		private int _giveNotificationAfterHoursCounter;

		[SaveableField(100)]
		private JournalLog _discreteLog;

		[SaveableField(130)]
		private int _rewardGold;

		private CharacterObject _tier5Troop;

		private CharacterObject _tier6Troop;

		private PersuasionTask _task;

		private const PersuasionDifficulty Difficulty = PersuasionDifficulty.MediumHard;

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=xxZeGhqf}{MALE_LESSER_NOBLE_NAME}{.o} Revolt");
				textObject.SetTextVariable("MALE_LESSER_NOBLE_NAME", _lesserNobleName);
				return textObject;
			}
		}

		private TextObject LesserNobleTitle => GetLesserNobleTitle(base.QuestGiver);

		public override bool IsRemainingTimeHidden => false;

		private TextObject QuestStartedLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=ARXvmC1v}{QUEST_GIVER.LINK} says that a {MALE_LESSER_NOBLE_TITLE} named {MALE_LESSER_NOBLE_NAME} is stirring up unrest in the countryside. {?QUEST_GIVER.GENDER}She{?}He{\\?} asked you to disperse {MALE_LESSER_NOBLE_NAME}'s band using whatever means necessary before the movement gets enough support.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("MALE_LESSER_NOBLE_TITLE", LesserNobleTitle);
				textObject.SetTextVariable("MALE_LESSER_NOBLE_NAME", _lesserNobleName);
				return textObject;
			}
		}

		private TextObject QuestCanceledWarDeclared
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

		private TextObject QuestLesserNoblePartyLocationLog
		{
			get
			{
				TextObject textObject = new TextObject("{=dEXpZOlC}{QUEST_GIVER}{.o} informants reported that {MALE_LESSER_NOBLE_NAME}{.o} party has been seen near {VILLAGE}.");
				textObject.SetTextVariable("QUEST_GIVER", base.QuestGiver.EncyclopediaLinkWithName);
				textObject.SetTextVariable("MALE_LESSER_NOBLE_NAME", _lesserNobleName);
				return textObject;
			}
		}

		private TextObject QuestFailedAfterTalkingWithLesserNoblePartyLog
		{
			get
			{
				TextObject textObject = new TextObject("{=RybVf6Nt}You were convinced by {MALE_LESSER_NOBLE_NAME} to let him gather support. {QUEST_GIVER.LINK} will be very upset about this.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("MALE_LESSER_NOBLE_NAME", _lesserNobleName);
				return textObject;
			}
		}

		private TextObject QuestSuccessPlayerComesToAnAgreementWithLesserNoblePartyLog
		{
			get
			{
				TextObject textObject = new TextObject("{=RY8Fglsk}You have persuaded {MALE_LESSER_NOBLE_NAME} to disband his party and return home. You received {REWARD}{GOLD_ICON} in return for your service.");
				textObject.SetTextVariable("MALE_LESSER_NOBLE_NAME", _lesserNobleName);
				textObject.SetTextVariable("REWARD", RewardGold);
				return textObject;
			}
		}

		private TextObject QuestFailWithPlayerDefeatedAgainstNobleParty
		{
			get
			{
				TextObject textObject = new TextObject("{=R3aVlXGu}You lost the battle against {MALE_LESSER_NOBLE_NAME} and failed to help {QUEST_GIVER.LINK} as promised.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("MALE_LESSER_NOBLE_NAME", _lesserNobleName);
				return textObject;
			}
		}

		private TextObject QuestSuccessWithPlayerDefeatedNobleParty
		{
			get
			{
				TextObject textObject = new TextObject("{=aKrzoAsh}You have defeated {MALE_LESSER_NOBLE_NAME} and helped {QUEST_GIVER.LINK} as promised. You received {REWARD}{GOLD_ICON} in return for your service.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("MALE_LESSER_NOBLE_NAME", _lesserNobleName);
				textObject.SetTextVariable("REWARD", RewardGold);
				return textObject;
			}
		}

		private TextObject QuestFailedWithTimeOutLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=zhzh4yd1}You failed to stop {MALE_LESSER_NOBLE_NAME} before he stirred up unrest in the countryside. {QUEST_GIVER.LINK} will be very upset about this.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("MALE_LESSER_NOBLE_NAME", _lesserNobleName);
				return textObject;
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsLesserNobleRevoltIssueQuest(object o, List<object> collectedObjects)
		{
			((LesserNobleRevoltIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_lesserNobleName);
			collectedObjects.Add(_lesserNobleParty);
			collectedObjects.Add(_suitableVillagesToVisitList);
			collectedObjects.Add(_visitedVillagesList);
			collectedObjects.Add(_discreteLog);
		}

		internal static object AutoGeneratedGetMemberValue_checkForEventEnd(object o)
		{
			return ((LesserNobleRevoltIssueQuest)o)._checkForEventEnd;
		}

		internal static object AutoGeneratedGetMemberValue_lesserNobleName(object o)
		{
			return ((LesserNobleRevoltIssueQuest)o)._lesserNobleName;
		}

		internal static object AutoGeneratedGetMemberValue_lesserNobleParty(object o)
		{
			return ((LesserNobleRevoltIssueQuest)o)._lesserNobleParty;
		}

		internal static object AutoGeneratedGetMemberValue_suitableVillagesToVisitList(object o)
		{
			return ((LesserNobleRevoltIssueQuest)o)._suitableVillagesToVisitList;
		}

		internal static object AutoGeneratedGetMemberValue_visitedVillagesList(object o)
		{
			return ((LesserNobleRevoltIssueQuest)o)._visitedVillagesList;
		}

		internal static object AutoGeneratedGetMemberValue_persuasionTriedOnce(object o)
		{
			return ((LesserNobleRevoltIssueQuest)o)._persuasionTriedOnce;
		}

		internal static object AutoGeneratedGetMemberValue_firstTalkIsDone(object o)
		{
			return ((LesserNobleRevoltIssueQuest)o)._firstTalkIsDone;
		}

		internal static object AutoGeneratedGetMemberValue_goOutFromSettlementAfterHoursCounter(object o)
		{
			return ((LesserNobleRevoltIssueQuest)o)._goOutFromSettlementAfterHoursCounter;
		}

		internal static object AutoGeneratedGetMemberValue_giveNotificationAfterHoursCounter(object o)
		{
			return ((LesserNobleRevoltIssueQuest)o)._giveNotificationAfterHoursCounter;
		}

		internal static object AutoGeneratedGetMemberValue_discreteLog(object o)
		{
			return ((LesserNobleRevoltIssueQuest)o)._discreteLog;
		}

		internal static object AutoGeneratedGetMemberValue_rewardGold(object o)
		{
			return ((LesserNobleRevoltIssueQuest)o)._rewardGold;
		}

		private TextObject GetQuestCanceledOnClanChangedKingdomLogText(Kingdom oldKingdom)
		{
			TextObject textObject = new TextObject("{=lHNgL7Xn}{QUEST_GIVER.LINK} has left {KINGDOM}. Your agreement with {?QUEST_GIVER.GENDER}her{?}him{\\?} has been canceled.");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
			textObject.SetTextVariable("KINGDOM", oldKingdom.Name);
			return textObject;
		}

		public LesserNobleRevoltIssueQuest(string questId, Hero giverHero, CampaignTime duration, int rewardGold, TextObject lesserNobleName)
			: base(questId, giverHero, duration, rewardGold)
		{
			_lesserNobleName = lesserNobleName;
			_tier5Troop = CharacterHelper.GetTroopTree(giverHero.Culture.EliteBasicTroop, 5f, 5f).First();
			_tier6Troop = CharacterHelper.GetTroopTree(giverHero.Culture.EliteBasicTroop, 6f, 6f).First();
			SetDialogs();
			InitializeQuestOnCreation();
			CollectSuitableVillages();
			SpawnLesserNobleParty();
			_rewardGold = 150 * _lesserNobleParty.MemberRoster.TotalManCount;
		}

		private void CollectSuitableVillages()
		{
			_suitableVillagesToVisitList = new List<Settlement>();
			_visitedVillagesList = new List<Settlement>();
			foreach (Settlement settlement in base.QuestGiver.Clan.Settlements)
			{
				if (settlement.IsVillage && !settlement.IsUnderRaid && !settlement.IsRaided)
				{
					_suitableVillagesToVisitList.Add(settlement);
				}
			}
			IMapPoint questGiverMapPoint = base.QuestGiver.GetMapPoint();
			if (questGiverMapPoint == null)
			{
				questGiverMapPoint = base.QuestGiver.HomeSettlement;
			}
			_suitableVillagesToVisitList = _suitableVillagesToVisitList.OrderBy((Settlement x) => Campaign.Current.Models.MapDistanceModel.GetDistance(x, questGiverMapPoint as Settlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default)).ToList();
		}

		private void SpawnLesserNobleParty()
		{
			TextObject textObject = new TextObject("{=WhHDg7ag}{MALE_LESSER_NOBLE_NAME}{.o} Party");
			textObject.SetTextVariable("MALE_LESSER_NOBLE_NAME", _lesserNobleName);
			Settlement settlement = _suitableVillagesToVisitList[1];
			_lesserNobleParty = CustomPartyComponent.CreateCustomPartyWithTroopRoster(settlement.GatePosition, 1f, base.QuestGiver.HomeSettlement, textObject, null, TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), null);
			_lesserNobleParty.SetPartyUsedByQuest(isActivelyUsed: true);
			AddTrackedObject(_lesserNobleParty);
			AddTrackedObject(settlement);
			_lesserNobleParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
			_lesserNobleParty.IgnoreByOtherPartiesTill(CampaignTime.Never);
			_lesserNobleParty.MemberRoster.AddToCounts(_tier5Troop, TaleWorlds.Library.MathF.Min(40, MobileParty.MainParty.Party.PartySizeLimit / 2));
			SetPartyAiAction.GetActionForVisitingSettlement(_lesserNobleParty, settlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
			TextObject textObject2 = new TextObject("{=Y2Z6F5Fj}Support level of {MALE_LESSER_NOBLE_NAME}");
			textObject2.SetTextVariable("MALE_LESSER_NOBLE_NAME", _lesserNobleName);
			_discreteLog = AddDiscreteLog(QuestStartedLogText, textObject2, 0, 20);
		}

		private DialogFlow GetLesserNoblePartyDialogFlow()
		{
			TextObject textObject = new TextObject("{=0L79bLR5}Sedition? You have been misinformed. My men and I are loyal servants of {RULER_NAME_AND_TITLE}. We are simply going about informing the people that our {RULER_TITLE} is surrounded by bad advisers who wish to raise their taxes. We petition {RULER.NAME} to listen to {?RULER.GENDER}her{?}his{\\?} people and their needs.[if:convo_normal][ib:hip]");
			textObject.SetTextVariable("RULER_NAME_AND_TITLE", GameTexts.FindText("str_faction_ruler_name_with_title", base.QuestGiver.Culture.StringId));
			textObject.SetTextVariable("RULER_TITLE", GameTexts.FindText("str_faction_ruler", base.QuestGiver.Culture.StringId));
			TextObject npcText = new TextObject("{=p14S00jq}Consequences? You would shed the blood of {RULER.NAME}'s loyal servants just for raising their voices against oppression?[if:convo_confused_normal][ib:closed]");
			StringHelpers.SetCharacterProperties("RULER", base.QuestGiver.MapFaction.Leader.CharacterObject);
			DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=!}{MALE_LESSER_NOBLE_PARTY_START_LINE}")).Condition(SetStartDialogOnCondition)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=XkjRNMam}You have no right to go around, under arms, spreading sedition. Disperse!"))
				.Condition(() => !_firstTalkIsDone)
				.NpcLine(textObject)
				.Consequence(delegate
				{
					_task = GetPersuasionTask();
				})
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=dxTSVT4C}Yes, that's sedition all right. Disperse now or face the consequences."))
				.NpcLine(npcText)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=GdC4yKiO}I will talk no more. Get ready to fight."))
				.NpcLine(new TextObject("{=7VLmHrgC}The Heavens will grant victory to the just![ib:warrior][if:convo_contemptuous]"))
				.Consequence(delegate
				{
					EncounterManager.StartPartyEncounter(PartyBase.MainParty, _lesserNobleParty.Party);
					Campaign.Current.GameMenuManager.SetNextMenu("encounter");
				})
				.CloseDialog()
				.PlayerOption(new TextObject("{=RTNK5FSs}No, I don't want to shed your blood."))
				.NpcLine(new TextObject("{=izzbaMUf}Wise decision.[if:convo_undecided_closed][ib:normal]"))
				.GotoDialogState("start")
				.EndPlayerOptions()
				.PlayerOption("{=UTaXn4pb}Well, I have indeed been misinformed. You may go on your way...")
				.NpcLine("{=8nGRa1pO}You're doing a good thing, {?PLAYER.GENDER}madame{?}sir{\\?}. The poor people around here will remember you")
				.Consequence(QuestFailedAfterTalkingWithLesserNobleParty)
				.CloseDialog()
				.PlayerOption("{=etYcn79b}Let me try to talk some sense into you...")
				.Condition(() => !_persuasionTriedOnce)
				.Consequence(delegate
				{
					_persuasionTriedOnce = true;
				})
				.GotoDialogState("start_lesser_noble_party_persuasion")
				.EndPlayerOptions()
				.PlayerOption(new TextObject("{=tm0GQosH}It is time your revolt ends!"))
				.Condition(() => _firstTalkIsDone)
				.NpcLine(new TextObject("{=Fs8KhPuh}We shall see!"))
				.Consequence(delegate
				{
					EncounterManager.StartPartyEncounter(PartyBase.MainParty, _lesserNobleParty.Party);
					Campaign.Current.GameMenuManager.SetNextMenu("encounter");
				})
				.CloseDialog()
				.PlayerOption(new TextObject("{=7PQOzmgb}I don’t want no confrontation with you."))
				.Condition(() => _firstTalkIsDone)
				.NpcLine(new TextObject("{=6m14pQbt}Good, begone then![if:convo_undecided_closed][ib:normal]"))
				.Consequence(delegate
				{
					PlayerEncounter.Finish();
				})
				.CloseDialog()
				.EndPlayerOptions();
			AddPersuasionDialogs(dialogFlow);
			return dialogFlow;
		}

		private bool SetStartDialogOnCondition()
		{
			if (_lesserNobleParty != null && CharacterObject.OneToOneConversationCharacter == ConversationHelper.GetConversationCharacterPartyLeader(_lesserNobleParty.Party))
			{
				if (_firstTalkIsDone)
				{
					MBTextManager.SetTextVariable("MALE_LESSER_NOBLE_PARTY_START_LINE", "{=iKhmpTgq}What is it that you want from me you snake! Get out of my way or you’ll be meeting my wroth.[ib:warrior][if:convo_contemptuous]");
				}
				else if (_persuasionTriedOnce)
				{
					MBTextManager.SetTextVariable("MALE_LESSER_NOBLE_PARTY_START_LINE", "{=Nn06TSq9}Anything else to say?");
				}
				else
				{
					MBTextManager.SetTextVariable("MALE_LESSER_NOBLE_PARTY_START_LINE", "{=ZqjiBjYJ}Greetings, {?PLAYER.GENDER}madame{?}sir{\\?}. May I help you?");
				}
				return true;
			}
			return false;
		}

		private void AddPersuasionDialogs(DialogFlow dialog)
		{
			dialog.AddDialogLine("lesser_party_persuasion_check_accepted", "start_lesser_noble_party_persuasion", "lesser_party_persuasion_start_reservation", "{=FwtFtpwp}How?", null, persuasion_start_with_lesser_party_on_consequence, this);
			dialog.AddDialogLine("lesser_party_persuasion_rejected", "lesser_party_persuasion_start_reservation", "start", "{=!}{FAILED_PERSUASION_LINE}", persuasion_failed_with_lesser_party_on_condition, persuasion_rejected_with_lesser_party_on_consequence, this);
			dialog.AddDialogLine("lesser_party_persuasion_attempt", "lesser_party_persuasion_start_reservation", "lesser_party_persuasion_select_option", "{=wM77S68a}What's there to discuss?", () => !persuasion_failed_with_lesser_party_on_condition(), null, this);
			dialog.AddDialogLine("lesser_party_persuasion_success", "lesser_party_persuasion_start_reservation", "close_window", "{=zWILNszA}You may have a point, but don't think that we will give up demanding our rights. We will return home, but we will pursue our grievances some other way.", ConversationManager.GetPersuasionProgressSatisfied, persuasion_complete_with_lesser_party_on_consequence, this, 200);
			ConversationSentence.OnConditionDelegate conditionDelegate = lesser_party_persuasion_select_option_1_on_condition;
			ConversationSentence.OnConsequenceDelegate consequenceDelegate = lesser_party_persuasion_select_option_1_on_consequence;
			ConversationSentence.OnPersuasionOptionDelegate persuasionOptionDelegate = lesser_party_persuasion_setup_option_1;
			ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = lesser_party_persuasion_clickable_option_1_on_condition;
			dialog.AddPlayerLine("lesser_party_persuasion_select_option_1", "lesser_party_persuasion_select_option", "lesser_party_persuasion_selected_option_response", "{=!}{LESSER_PARTY_PERSUADE_ATTEMPT_1}", conditionDelegate, consequenceDelegate, this, 100, clickableConditionDelegate, persuasionOptionDelegate);
			ConversationSentence.OnConditionDelegate conditionDelegate2 = lesser_party_persuasion_select_option_2_on_condition;
			ConversationSentence.OnConsequenceDelegate consequenceDelegate2 = lesser_party_persuasion_select_option_2_on_consequence;
			persuasionOptionDelegate = lesser_party_persuasion_setup_option_2;
			clickableConditionDelegate = lesser_party_persuasion_clickable_option_2_on_condition;
			dialog.AddPlayerLine("lesser_party_persuasion_select_option_2", "lesser_party_persuasion_select_option", "lesser_party_persuasion_selected_option_response", "{=!}{LESSER_PARTY_PERSUADE_ATTEMPT_2}", conditionDelegate2, consequenceDelegate2, this, 100, clickableConditionDelegate, persuasionOptionDelegate);
			ConversationSentence.OnConditionDelegate conditionDelegate3 = lesser_party_persuasion_select_option_3_on_condition;
			ConversationSentence.OnConsequenceDelegate consequenceDelegate3 = lesser_party_persuasion_select_option_3_on_consequence;
			persuasionOptionDelegate = lesser_party_persuasion_setup_option_3;
			clickableConditionDelegate = lesser_party_persuasion_clickable_option_3_on_condition;
			dialog.AddPlayerLine("lesser_party_persuasion_select_option_3", "lesser_party_persuasion_select_option", "lesser_party_persuasion_selected_option_response", "{=!}{LESSER_PARTY_PERSUADE_ATTEMPT_3}", conditionDelegate3, consequenceDelegate3, this, 100, clickableConditionDelegate, persuasionOptionDelegate);
			ConversationSentence.OnConditionDelegate conditionDelegate4 = lesser_party_persuasion_select_option_4_on_condition;
			ConversationSentence.OnConsequenceDelegate consequenceDelegate4 = lesser_party_persuasion_select_option_4_on_consequence;
			persuasionOptionDelegate = lesser_party_persuasion_setup_option_4;
			clickableConditionDelegate = lesser_party_persuasion_clickable_option_4_on_condition;
			dialog.AddPlayerLine("lesser_party_persuasion_select_option_4", "lesser_party_persuasion_select_option", "lesser_party_persuasion_selected_option_response", "{=!}{LESSER_PARTY_PERSUADE_ATTEMPT_4}", conditionDelegate4, consequenceDelegate4, this, 100, clickableConditionDelegate, persuasionOptionDelegate);
			dialog.AddDialogLine("lesser_party_persuasion_select_option_reaction", "lesser_party_persuasion_selected_option_response", "lesser_party_persuasion_start_reservation", "{=!}{PERSUASION_REACTION}", lesser_party_persuasion_selected_option_response_on_condition, lesser_party_persuasion_selected_option_response_on_consequence, this);
		}

		private void persuasion_start_with_lesser_party_on_consequence()
		{
			ConversationManager.StartPersuasion(2f, 1f, 0f, 2f, 2f, 0f, PersuasionDifficulty.MediumHard);
		}

		private PersuasionTask GetPersuasionTask()
		{
			PersuasionTask persuasionTask = new PersuasionTask(0)
			{
				FinalFailLine = new TextObject("{=W7c3BfIX}Thus always spoke the tyrant to the oppressed! We can only pray that the Heavens help the just. Now stay out of my way.[if:convo_undecided_closed][ib:closed]"),
				TryLaterLine = TextObject.GetEmpty(),
				SpokenLine = new TextObject("{=wM77S68a}What's there to discuss?")
			};
			PersuasionOptionArgs option = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Mercy, TraitEffect.Positive, PersuasionArgumentStrength.Normal, givesCriticalSuccess: false, new TextObject("{=JDGz56HX}If you start a rebellion, you're just going to get a lot of peasants around here killed..."));
			persuasionTask.AddOptionToTask(option);
			PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.Normal, givesCriticalSuccess: true, new TextObject("{=s99TKI9x}The nobles aren't trying to starve anyone, but if they don't collect taxes, they won't be able to fight off raiders."));
			persuasionTask.AddOptionToTask(option2);
			PersuasionOptionArgs option3 = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.Normal, givesCriticalSuccess: false, new TextObject("{=TCSggNOS}You know the dark hearts of men... Those who start as rebels end up turning bandit. Rebellion brings anarchy, not justice. "));
			persuasionTask.AddOptionToTask(option3);
			if (MobileParty.MainParty.MemberRoster.TotalHealthyCount >= _lesserNobleParty.MemberRoster.TotalHealthyCount * 3)
			{
				TextObject line = new TextObject("{=IP3e4x8S}Count my troops. Then count yours. You have no chance.");
				PersuasionOptionArgs option4 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, PersuasionArgumentStrength.Easy, givesCriticalSuccess: false, line);
				persuasionTask.AddOptionToTask(option4);
			}
			return persuasionTask;
		}

		private bool lesser_party_persuasion_selected_option_response_on_condition()
		{
			PersuasionOptionResult item = ConversationManager.GetPersuasionChosenOptions().Last().Item2;
			MBTextManager.SetTextVariable("PERSUASION_REACTION", PersuasionHelper.GetDefaultPersuasionOptionReaction(item));
			if (item == PersuasionOptionResult.CriticalFailure)
			{
				_task.BlockAllOptions();
			}
			return true;
		}

		private void lesser_party_persuasion_selected_option_response_on_consequence()
		{
			Tuple<PersuasionOptionArgs, PersuasionOptionResult> tuple = ConversationManager.GetPersuasionChosenOptions().Last();
			float difficulty = Campaign.Current.Models.PersuasionModel.GetDifficulty(PersuasionDifficulty.MediumHard);
			Campaign.Current.Models.PersuasionModel.GetEffectChances(tuple.Item1, out var moveToNextStageChance, out var blockRandomOptionChance, difficulty);
			_task.ApplyEffects(moveToNextStageChance, blockRandomOptionChance);
		}

		private bool lesser_party_persuasion_select_option_1_on_condition()
		{
			if (_task.Options.Count > 0)
			{
				TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
				textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(_task.Options.ElementAt(0), showToPlayer: false));
				textObject.SetTextVariable("PERSUASION_OPTION_LINE", _task.Options.ElementAt(0).Line);
				MBTextManager.SetTextVariable("LESSER_PARTY_PERSUADE_ATTEMPT_1", textObject);
				return true;
			}
			return false;
		}

		private bool lesser_party_persuasion_select_option_2_on_condition()
		{
			if (_task.Options.Count > 1)
			{
				TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
				textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(_task.Options.ElementAt(1), showToPlayer: false));
				textObject.SetTextVariable("PERSUASION_OPTION_LINE", _task.Options.ElementAt(1).Line);
				MBTextManager.SetTextVariable("LESSER_PARTY_PERSUADE_ATTEMPT_2", textObject);
				return true;
			}
			return false;
		}

		private bool lesser_party_persuasion_select_option_3_on_condition()
		{
			if (_task.Options.Count > 2)
			{
				TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
				textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(_task.Options.ElementAt(2), showToPlayer: false));
				textObject.SetTextVariable("PERSUASION_OPTION_LINE", _task.Options.ElementAt(2).Line);
				MBTextManager.SetTextVariable("LESSER_PARTY_PERSUADE_ATTEMPT_3", textObject);
				return true;
			}
			return false;
		}

		private bool lesser_party_persuasion_select_option_4_on_condition()
		{
			if (_task.Options.Count > 3)
			{
				TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
				textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(_task.Options.ElementAt(3), showToPlayer: false));
				textObject.SetTextVariable("PERSUASION_OPTION_LINE", _task.Options.ElementAt(3).Line);
				MBTextManager.SetTextVariable("LESSER_PARTY_PERSUADE_ATTEMPT_4", textObject);
				return true;
			}
			return false;
		}

		private void lesser_party_persuasion_select_option_1_on_consequence()
		{
			if (_task.Options.Count > 0)
			{
				_task.Options[0].BlockTheOption(isBlocked: true);
			}
		}

		private void lesser_party_persuasion_select_option_2_on_consequence()
		{
			if (_task.Options.Count > 1)
			{
				_task.Options[1].BlockTheOption(isBlocked: true);
			}
		}

		private void lesser_party_persuasion_select_option_3_on_consequence()
		{
			if (_task.Options.Count > 2)
			{
				_task.Options[2].BlockTheOption(isBlocked: true);
			}
		}

		private void lesser_party_persuasion_select_option_4_on_consequence()
		{
			if (_task.Options.Count > 3)
			{
				_task.Options[3].BlockTheOption(isBlocked: true);
			}
		}

		private bool persuasion_failed_with_lesser_party_on_condition()
		{
			if (_task.Options.All((PersuasionOptionArgs x) => x.IsBlocked) && !ConversationManager.GetPersuasionProgressSatisfied())
			{
				MBTextManager.SetTextVariable("FAILED_PERSUASION_LINE", _task.FinalFailLine);
				return true;
			}
			return false;
		}

		private PersuasionOptionArgs lesser_party_persuasion_setup_option_1()
		{
			return _task.Options.ElementAt(0);
		}

		private PersuasionOptionArgs lesser_party_persuasion_setup_option_2()
		{
			return _task.Options.ElementAt(1);
		}

		private PersuasionOptionArgs lesser_party_persuasion_setup_option_3()
		{
			return _task.Options.ElementAt(2);
		}

		private PersuasionOptionArgs lesser_party_persuasion_setup_option_4()
		{
			return _task.Options.ElementAt(3);
		}

		private bool lesser_party_persuasion_clickable_option_1_on_condition(out TextObject hintText)
		{
			hintText = new TextObject("{=9ACJsI6S}Blocked");
			if (_task.Options.Count > 0)
			{
				hintText = (_task.Options.ElementAt(0).IsBlocked ? hintText : null);
				return !_task.Options.ElementAt(0).IsBlocked;
			}
			return false;
		}

		private bool lesser_party_persuasion_clickable_option_2_on_condition(out TextObject hintText)
		{
			hintText = new TextObject("{=9ACJsI6S}Blocked");
			if (_task.Options.Count > 1)
			{
				hintText = (_task.Options.ElementAt(1).IsBlocked ? hintText : null);
				return !_task.Options.ElementAt(1).IsBlocked;
			}
			return false;
		}

		private bool lesser_party_persuasion_clickable_option_3_on_condition(out TextObject hintText)
		{
			hintText = new TextObject("{=9ACJsI6S}Blocked");
			if (_task.Options.Count > 2)
			{
				hintText = (_task.Options.ElementAt(2).IsBlocked ? hintText : null);
				return !_task.Options.ElementAt(2).IsBlocked;
			}
			return false;
		}

		private bool lesser_party_persuasion_clickable_option_4_on_condition(out TextObject hintText)
		{
			hintText = new TextObject("{=9ACJsI6S}Blocked");
			if (_task.Options.Count > 3)
			{
				hintText = (_task.Options.ElementAt(3).IsBlocked ? hintText : null);
				return !_task.Options.ElementAt(3).IsBlocked;
			}
			return false;
		}

		private void persuasion_rejected_with_lesser_party_on_consequence()
		{
			PlayerEncounter.LeaveEncounter = false;
			ConversationManager.EndPersuasion();
		}

		private void persuasion_complete_with_lesser_party_on_consequence()
		{
			PlayerEncounter.LeaveEncounter = true;
			ConversationManager.EndPersuasion();
			Campaign.Current.ConversationManager.ConversationEndOneShot += QuestSuccessPlayerComesToAnAgreementWithLesserNobleParty;
		}

		private bool DialogCondition()
		{
			return Hero.OneToOneConversationHero == base.QuestGiver;
		}

		protected override void SetDialogs()
		{
			TextObject textObject = new TextObject("{=YMGQYhvg}Thank you. {MALE_LESSER_NOBLE_NAME} is always on the move, so finding him might be tricky. But don't worry, I get regular reports from the landowners around here and I'll keep you updated on his movements...[if:convo_undecided_closed]");
			textObject.SetTextVariable("MALE_LESSER_NOBLE_NAME", _lesserNobleName);
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(textObject).Condition(DialogCondition)
				.Consequence(QuestAcceptedConsequences)
				.CloseDialog();
			TextObject textObject2 = new TextObject("{=ZpabPAtS}Have you put a stop to {MALE_LESSER_NOBLE_TITLE}{.s}?[if:convo_undecided_open]");
			textObject2.SetTextVariable("MALE_LESSER_NOBLE_TITLE", LesserNobleTitle);
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(textObject2).Condition(DialogCondition)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=p8ye30mG}Yes, we know where he is. It's just a matter of time to catch them."))
				.NpcLine(new TextObject("{=2L1bZdwh}Good. I count on you.[if:convo_relaxed_happy]"))
				.Consequence(MapEventHelper.OnConversationEnd)
				.CloseDialog()
				.PlayerOption(new TextObject("{=zjMqgbXz}We are working on it."))
				.NpcLine(new TextObject("{=HRwb3qJZ}Very well. But every day he's out there spreading sedition make things more dangerous for all of us.[if:convo_undecided_closed]"))
				.Consequence(MapEventHelper.OnConversationEnd)
				.CloseDialog()
				.EndPlayerOptions();
			QuestCharacterDialogFlow = GetLesserNoblePartyDialogFlow();
		}

		private void QuestAcceptedConsequences()
		{
			StartQuest();
		}

		private void QuestFailedAfterTalkingWithLesserNobleParty()
		{
			PlayerEncounter.Finish();
			AddLog(QuestFailedAfterTalkingWithLesserNoblePartyLog);
			ChangeRelationWithRuralNotables(1);
			CompleteQuestWithFail();
		}

		private void QuestSuccessPlayerComesToAnAgreementWithLesserNobleParty()
		{
			AddLog(QuestSuccessPlayerComesToAnAgreementWithLesserNoblePartyLog);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -10)
			});
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
			RelationshipChangeWithQuestGiver = 8;
			GainRenownAction.Apply(Hero.MainHero, 1f);
			CompleteQuestWithSuccess();
		}

		private void QuestFailWithPlayerDefeatedAgainstLesserNobleParty()
		{
			AddLog(QuestFailWithPlayerDefeatedAgainstNobleParty);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -15)
			});
			CompleteQuestWithFail();
		}

		private void QuestSuccessWithPlayerDefeatedLesserNobleParty()
		{
			AddLog(QuestSuccessWithPlayerDefeatedNobleParty);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 30)
			});
			GainRenownAction.Apply(Hero.MainHero, 3f);
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
			RelationshipChangeWithQuestGiver = 8;
			ChangeRelationWithRuralNotables(-2);
			CompleteQuestWithSuccess();
		}

		private void ChangeRelationWithRuralNotables(int value)
		{
			foreach (Settlement visitedVillages in _visitedVillagesList)
			{
				foreach (Hero notable in visitedVillages.Notables)
				{
					if (notable.IsRuralNotable)
					{
						ChangeRelationAction.ApplyPlayerRelation(notable, value);
					}
				}
			}
		}

		protected override void OnTimedOut()
		{
			AddLog(QuestFailedWithTimeOutLogText);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -10)
			});
			CompleteQuestWithFail();
		}

		public override void OnFailed()
		{
			RelationshipChangeWithQuestGiver = -5;
		}

		protected override void HourlyTick()
		{
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
			CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnd);
			CampaignEvents.CanHeroBecomePrisonerEvent.AddNonSerializedListener(this, OnCanHeroBecomePrisonerInfoIsRequested);
		}

		private void OnCanHeroBecomePrisonerInfoIsRequested(Hero hero, ref bool result)
		{
			if (hero == Hero.MainHero && _checkForEventEnd)
			{
				result = false;
			}
		}

		private void OnMapEventEnd(MapEvent obj)
		{
			if (!_checkForEventEnd)
			{
				return;
			}
			if (PlayerEncounter.Battle.WinningSide != BattleSideEnum.None)
			{
				if (PlayerEncounter.Battle.WinningSide == PlayerEncounter.Battle.PlayerSide)
				{
					QuestSuccessWithPlayerDefeatedLesserNobleParty();
				}
				else
				{
					QuestFailWithPlayerDefeatedAgainstLesserNobleParty();
				}
			}
			_firstTalkIsDone = true;
			_checkForEventEnd = false;
		}

		private void OnGameMenuOpened(MenuCallbackArgs args)
		{
			if (!(args.MenuContext.GameMenu.StringId == "encounter") || PlayerEncounter.Battle == null || !PlayerEncounter.Battle.InvolvedParties.Contains(_lesserNobleParty.Party) || PlayerEncounter.Battle.State != MapEventState.Wait || MapEvent.PlayerMapEvent == null || !MapEvent.PlayerMapEvent.InvolvedParties.Contains(_lesserNobleParty.Party))
			{
				return;
			}
			foreach (PartyBase item in MapEvent.PlayerMapEvent.InvolvedParties.ToList())
			{
				if (item.IsMobile && item.MobileParty.IsMilitia)
				{
					item.MapEventSide = null;
				}
			}
		}

		private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
			{
				QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
			}
			if (base.IsOngoing && attackerParty == PartyBase.MainParty && defenderParty == _lesserNobleParty.Party)
			{
				_checkForEventEnd = true;
			}
		}

		protected override void HourlyTickParty(MobileParty mobileParty)
		{
			if (mobileParty != _lesserNobleParty)
			{
				return;
			}
			if (_lesserNobleParty.TargetSettlement == null)
			{
				if (_goOutFromSettlementAfterHoursCounter > 0)
				{
					_goOutFromSettlementAfterHoursCounter--;
				}
				if (_goOutFromSettlementAfterHoursCounter == 0)
				{
					if (_suitableVillagesToVisitList.Count == 0)
					{
						CollectSuitableVillages();
					}
					if (_suitableVillagesToVisitList.Count > 0)
					{
						IOrderedEnumerable<Settlement> source = _suitableVillagesToVisitList.OrderBy((Settlement x) => DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(_lesserNobleParty, x.Village.Settlement, MobileParty.NavigationType.Default));
						SetPartyAiAction.GetActionForVisitingSettlement(_lesserNobleParty, source.First(), MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
					}
					else
					{
						_goOutFromSettlementAfterHoursCounter = 5;
					}
				}
			}
			if (_giveNotificationAfterHoursCounter > 0)
			{
				_giveNotificationAfterHoursCounter--;
			}
			if (_giveNotificationAfterHoursCounter != 0 || _visitedVillagesList.Count <= 0)
			{
				return;
			}
			TextObject questLesserNoblePartyLocationLog = QuestLesserNoblePartyLocationLog;
			questLesserNoblePartyLocationLog.SetTextVariable("VILLAGE", _visitedVillagesList[_visitedVillagesList.Count - 1].EncyclopediaLinkWithName);
			MBInformationManager.AddQuickInformation(questLesserNoblePartyLocationLog);
			AddLog(questLesserNoblePartyLocationLog);
			foreach (Settlement visitedVillages in _visitedVillagesList)
			{
				if (IsTracked(visitedVillages))
				{
					RemoveTrackedObject(visitedVillages);
				}
			}
			AddTrackedObject(_visitedVillagesList[_visitedVillagesList.Count - 1]);
			_giveNotificationAfterHoursCounter = -1;
		}

		private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
		{
			if (party == _lesserNobleParty)
			{
				_visitedVillagesList.Add(settlement);
				_suitableVillagesToVisitList.Remove(settlement);
				_goOutFromSettlementAfterHoursCounter = 5;
				_giveNotificationAfterHoursCounter = 9;
				if (80 > _lesserNobleParty.MemberRoster.TotalManCount)
				{
					_lesserNobleParty.MemberRoster.AddToCounts(((double)MBRandom.RandomFloat < 0.7) ? _tier5Troop : _tier6Troop, (MBRandom.RandomFloat < 0.5f) ? 3 : 4);
				}
				_lesserNobleParty.ItemRoster.AddToCounts(DefaultItems.Grain, 5);
				_discreteLog.UpdateCurrentProgress(_discreteLog.CurrentProgress + 1);
				if (_discreteLog.CurrentProgress == 20)
				{
					OnTimedOut();
				}
			}
		}

		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (base.QuestGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				CompleteQuestWithCancel(QuestCanceledWarDeclared);
			}
			else if (clan == base.QuestGiver.Clan)
			{
				CompleteQuestWithCancel(GetQuestCanceledOnClanChangedKingdomLogText(oldKingdom));
			}
		}

		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
		{
			QuestHelper.CheckWarDeclarationAndFailOrCancelTheQuest(this, faction1, faction2, detail, PlayerDeclaredWarQuestLogText, QuestCanceledWarDeclared);
		}

		protected override void OnFinalize()
		{
			if (_lesserNobleParty.IsActive)
			{
				DestroyPartyAction.Apply(null, _lesserNobleParty);
			}
		}

		protected override void InitializeQuestOnGameLoad()
		{
			_tier5Troop = CharacterHelper.GetTroopTree(base.QuestGiver.Culture.EliteBasicTroop, 5f, 5f).First();
			_tier6Troop = CharacterHelper.GetTroopTree(base.QuestGiver.Culture.EliteBasicTroop, 6f, 6f).First();
			SetDialogs();
		}
	}

	public class LesserNobleRevoltIssueBehaviorTypeDefiner : SaveableTypeDefiner
	{
		public LesserNobleRevoltIssueBehaviorTypeDefiner()
			: base(870000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(LesserNobleRevoltIssue), 1);
			AddClassDefinition(typeof(LesserNobleRevoltIssueQuest), 2);
		}
	}

	private const IssueBase.IssueFrequency LesserNobleRevoltIssueIssueFrequency = IssueBase.IssueFrequency.Rare;

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private bool ConditionsHold(Hero issueGiver)
	{
		if (issueGiver.IsLord && !issueGiver.IsFactionLeader && issueGiver.MapFaction.IsKingdomFaction && issueGiver.Clan != null)
		{
			return issueGiver.Clan.Fiefs.Count >= 3;
		}
		return false;
	}

	public void OnCheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero))
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnSelected, typeof(LesserNobleRevoltIssue), IssueBase.IssueFrequency.Rare));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(LesserNobleRevoltIssue), IssueBase.IssueFrequency.Rare));
		}
	}

	private IssueBase OnSelected(in PotentialIssueData pid, Hero issueOwner)
	{
		return new LesserNobleRevoltIssue(issueOwner);
	}

	private static TextObject GetLesserNobleTitle(Hero questGiver)
	{
		return questGiver.Culture.StringId switch
		{
			"sturgia" => new TextObject("{=k1Xr4rKn}Druzhinnik"), 
			"vlandia" => new TextObject("{=WtEoXblx}Knight"), 
			"battania" => new TextObject("{=8Kx17LDS}Fian"), 
			"empire" => new TextObject("{=5qRuGS2P}Equite"), 
			"khuzait" => new TextObject("{=ZaIBcQxa}Kheshig"), 
			"aserai" => new TextObject("{=1UBNuatk}Faris"), 
			"nord" => new TextObject("{=DHbF9JvO}Huscarl"), 
			_ => null, 
		};
	}
}
