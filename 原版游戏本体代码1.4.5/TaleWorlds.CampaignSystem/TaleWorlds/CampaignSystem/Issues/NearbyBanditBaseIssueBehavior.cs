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

public class NearbyBanditBaseIssueBehavior : CampaignBehaviorBase
{
	public class NearbyBanditBaseIssue : IssueBase
	{
		private const int QuestSolutionNeededMinimumHealthyMenCount = 25;

		private const int AlternativeSolutionFinalMenCount = 10;

		private const int AlternativeSolutionMinimumTroopTier = 2;

		private const int AlternativeSolutionCompanionSkillThreshold = 120;

		private const int AlternativeSolutionRelationRewardOnSuccess = 5;

		private const int AlternativeSolutionRelationPenaltyOnFail = -5;

		private const int IssueOwnerPowerBonusOnSuccess = 5;

		private const int IssueOwnerPowerPenaltyOnFail = -5;

		private const int SettlementProsperityBonusOnSuccess = 10;

		private const int SettlementProsperityPenaltyOnFail = -10;

		private const int IssueDuration = 15;

		private const int QuestTimeLimit = 30;

		[SaveableField(100)]
		private readonly Settlement _targetHideout;

		[SaveableField(101)]
		private Settlement _issueSettlement;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.Casualties | AlternativeSolutionScaleFlag.FailureRisk;

		public override int AlternativeSolutionBaseNeededMenCount => 10;

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 4 + TaleWorlds.Library.MathF.Ceiling(6f * base.IssueDifficultyMultiplier);

		protected override int RewardGold => 3000;

		internal Settlement TargetHideout => _targetHideout;

		public override TextObject IssueBriefByIssueGiver => new TextObject("{=vw2Q9jJH}Yes... There's this old ruin, a place that offers a good view of the roads, and is yet hard to reach. Needless to say, it attracts bandits. A new gang has moved in and they have been giving hell to the caravans and travellers passing by.[ib:closed][if:convo_undecided_open]");

		public override TextObject IssueAcceptByPlayer => new TextObject("{=IqH0jFdK}So you need someone to deal with these bastards?");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver => new TextObject("{=zstiYI49}Any bandits there can easily spot and evade a large army moving against them, but if you can enter the hideout with a small group of determined warriors you can catch them unaware.[ib:closed][if:convo_thinking]");

		public override TextObject IssueQuestSolutionAcceptByPlayer => new TextObject("{=uhYprSnG}I will go to the hideout myself and ambush the bandits.");

		protected override int CompanionSkillRewardXP => (int)(1000f + 1250f * base.IssueDifficultyMultiplier);

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

		public override TextObject IssueDiscussAlternativeSolution => new TextObject("{=DgVU7owN}I pray for your warriors. The people here will be very glad to hear of their success.[ib:hip][if:convo_excited]");

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=aXOgAKfj}Thank you, {?PLAYER.GENDER}madam{?}sir{\\?}. I hope your people will be successful.[ib:hip][if:convo_excited]");
				StringHelpers.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=VNXgZ8mt}Alternatively, if you can assign a companion with {TROOP_COUNT} or so men to this task, they can do the job.[ib:closed][if:convo_undecided_open]");
				textObject.SetTextVariable("TROOP_COUNT", GetTotalAlternativeSolutionNeededMenCount());
				return textObject;
			}
		}

		public override TextObject IssueAsRumorInSettlement
		{
			get
			{
				TextObject textObject = new TextObject("{=ctgihUte}I hope {QUEST_GIVER.NAME} has a plan to get rid of those bandits.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		public override bool IsThereAlternativeSolution => true;

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=G4kpabSf}{ISSUE_GIVER.LINK}, a headman from {ISSUE_SETTLEMENT}, has told you about recent bandit attacks on local villagers and asked you to clear out the outlaws' hideout. You asked {COMPANION.LINK} to take {TROOP_COUNT} of your best men to go and take care of it. They should report back to you in {RETURN_DAYS} days.");
				StringHelpers.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				textObject.SetTextVariable("ISSUE_SETTLEMENT", _issueSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("TROOP_COUNT", AlternativeSolutionSentTroops.TotalManCount - 1);
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		public override bool IsThereLordSolution => false;

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=ENYbLO8r}Bandit Base Near {SETTLEMENT}");
				textObject.SetTextVariable("SETTLEMENT", _issueSettlement.Name);
				return textObject;
			}
		}

		public override TextObject Description
		{
			get
			{
				TextObject textObject = new TextObject("{=vZ01a4cG}{QUEST_GIVER.LINK} wants you to clear the hideout that attracts more bandits to {?QUEST_GIVER.GENDER}her{?}his{\\?} region.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=SN3pjZiK}You received a message from {QUEST_GIVER.LINK}.{newline}\"Thank you for clearing out that bandits' nest. Please accept these {REWARD}{GOLD_ICON} denars with our gratitude.\"");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				textObject.SetTextVariable("REWARD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionFailLog
		{
			get
			{
				TextObject textObject = new TextObject("{=qsMnnfQ3}You failed to clear the hideout in time to prevent further attacks. {QUEST_GIVER.LINK} is disappointed.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		protected override bool IssueQuestCanBeDuplicated => false;

		internal static void AutoGeneratedStaticCollectObjectsNearbyBanditBaseIssue(object o, List<object> collectedObjects)
		{
			((NearbyBanditBaseIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_targetHideout);
			collectedObjects.Add(_issueSettlement);
		}

		internal static object AutoGeneratedGetMemberValue_targetHideout(object o)
		{
			return ((NearbyBanditBaseIssue)o)._targetHideout;
		}

		internal static object AutoGeneratedGetMemberValue_issueSettlement(object o)
		{
			return ((NearbyBanditBaseIssue)o)._issueSettlement;
		}

		public override bool CanBeCompletedByAI()
		{
			if (Hero.MainHero.PartyBelongedToAsPrisoner == _targetHideout.Party)
			{
				return false;
			}
			return true;
		}

		public NearbyBanditBaseIssue(Hero issueOwner, Settlement targetHideout)
			: base(issueOwner, CampaignTime.DaysFromNow(15f))
		{
			_targetHideout = targetHideout;
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.SettlementProsperity)
			{
				return -0.2f;
			}
			if (issueEffect == DefaultIssueEffects.SettlementSecurity)
			{
				return -1f;
			}
			return 0f;
		}

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			int skillValue = hero.GetSkillValue(DefaultSkills.OneHanded);
			int skillValue2 = hero.GetSkillValue(DefaultSkills.TwoHanded);
			int skillValue3 = hero.GetSkillValue(DefaultSkills.Polearm);
			if (skillValue >= skillValue2 && skillValue >= skillValue3)
			{
				return (DefaultSkills.OneHanded, 120);
			}
			return ((skillValue2 >= skillValue3) ? DefaultSkills.TwoHanded : DefaultSkills.Polearm, 120);
		}

		protected override void AfterIssueCreation()
		{
			_issueSettlement = base.IssueOwner.CurrentSettlement;
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

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			RelationshipChangeWithIssueOwner = 5;
			base.IssueOwner.AddPower(5f);
			_issueSettlement.Village.Bound.Town.Prosperity += 10f;
			TraitLevelingHelper.OnIssueSolvedThroughAlternativeSolution(base.IssueOwner, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 50)
			});
			GainRenownAction.Apply(Hero.MainHero, 1f);
		}

		protected override void AlternativeSolutionEndWithFailureConsequence()
		{
			RelationshipChangeWithIssueOwner = -5;
			base.IssueOwner.AddPower(-5f);
			_issueSettlement.Village.Bound.Town.Prosperity += -10f;
		}

		protected override void OnGameLoad()
		{
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new NearbyBanditBaseIssueQuest(questId, base.IssueOwner, _targetHideout, _issueSettlement, RewardGold, CampaignTime.DaysFromNow(30f));
		}

		public override IssueFrequency GetFrequency()
		{
			return IssueFrequency.VeryCommon;
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
			if (MobileParty.MainParty.MemberRoster.TotalHealthyCount - 1 < 25)
			{
				flags |= PreconditionFlags.NotEnoughTroops;
			}
			return flags == PreconditionFlags.None;
		}

		public override bool IssueStayAliveConditions()
		{
			if (_targetHideout.Hideout.IsInfested && base.IssueOwner.CurrentSettlement.IsVillage && !base.IssueOwner.CurrentSettlement.IsRaided && !base.IssueOwner.CurrentSettlement.IsUnderRaid)
			{
				return base.IssueOwner.CurrentSettlement.Village.Bound.Town.Security <= 80f;
			}
			return false;
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}
	}

	public class NearbyBanditBaseIssueQuest : QuestBase
	{
		private const int QuestGiverRelationBonus = 5;

		private const int QuestGiverRelationPenalty = -5;

		private const int WarCausedByPlayerQuestGiverRelationPenalty = -5;

		private const int QuestGiverPowerBonus = 5;

		private const int QuestGiverPowerPenalty = -5;

		private const int WarCausedByPlayerQuestGiverPowerPenalty = -10;

		private const int TownProsperityBonus = 10;

		private const int TownProsperityPenalty = -10;

		private const int WarCausedByPlayerTownProsperityPenalty = -10;

		private const int TownSecurityPenalty = -5;

		private const int WarCausedByPlayerTownSecurityPenalty = -10;

		private const int WarCausedByPlayerFailHonorPenalty = -50;

		private const int QuestGuid = 1056731;

		[SaveableField(100)]
		private readonly Settlement _targetHideout;

		[SaveableField(101)]
		private readonly Settlement _questSettlement;

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=ENYbLO8r}Bandit Base Near {SETTLEMENT}");
				textObject.SetTextVariable("SETTLEMENT", _questSettlement.Name);
				return textObject;
			}
		}

		public override bool IsRemainingTimeHidden => false;

		private TextObject OnQuestStartedLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=ogsh3V6G}{QUEST_GIVER.LINK}, a headman from {QUEST_SETTLEMENT}, has told you about the hideout of some bandits who have recently been attacking local villagers. You told {?QUEST_GIVER.GENDER}her{?}him{\\?} that you will take care of the situation yourself. {QUEST_GIVER.LINK} also marked the location of the hideout on your map.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_SETTLEMENT", _questSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject OnQuestSucceededLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=SN3pjZiK}You received a message from {QUEST_GIVER.LINK}.{newline}\"Thank you for clearing out that bandits' nest. Please accept these {REWARD}{GOLD_ICON} denars with our gratitude.\"");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("REWARD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		private TextObject OnQuestFailedLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=qsMnnfQ3}You failed to clear the hideout in time to prevent further attacks. {QUEST_GIVER.LINK} is disappointed.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject OnQuestFailedFromWarCausedByPlayerLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=B8KZ6tx2}You are accused in {SETTLEMENT} of a crime. This has angered {QUEST_GIVER.LINK} and {?QUEST_GIVER.GENDER}she{?}he{\\?} broke off your agreement.");
				textObject.SetTextVariable("SETTLEMENT", base.QuestGiver.CurrentSettlement.Name);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject OnQuestCanceledLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=4Bub0GY6}Hideout was cleared by someone else. Your agreement with {QUEST_GIVER.LINK} is canceled.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsNearbyBanditBaseIssueQuest(object o, List<object> collectedObjects)
		{
			((NearbyBanditBaseIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_targetHideout);
			collectedObjects.Add(_questSettlement);
		}

		internal static object AutoGeneratedGetMemberValue_targetHideout(object o)
		{
			return ((NearbyBanditBaseIssueQuest)o)._targetHideout;
		}

		internal static object AutoGeneratedGetMemberValue_questSettlement(object o)
		{
			return ((NearbyBanditBaseIssueQuest)o)._questSettlement;
		}

		public NearbyBanditBaseIssueQuest(string questId, Hero questGiver, Settlement targetHideout, Settlement questSettlement, int rewardGold, CampaignTime duration)
			: base(questId, questGiver, duration, rewardGold)
		{
			_targetHideout = targetHideout;
			_questSettlement = questSettlement;
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

		protected override void SetDialogs()
		{
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine("{=spj8bYVo}Good! I'll mark the hideout for you on a map.[if:convo_excited]").Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.Consequence(OnQuestAccepted)
				.CloseDialog();
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine("{=l9wYpIuV}Any news? Have you managed to clear out the hideout yet?[if:convo_astonished]").Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.BeginPlayerOptions()
				.PlayerOption("{=wErSpkjy}I'm still working on it.")
				.NpcLine("{=XTt6gZ7h}Do make haste, if you can. As long as those bandits are up there, no traveller is safe![if:convo_grave]")
				.CloseDialog()
				.PlayerOption("{=I8raOMRH}Sorry. No progress yet.")
				.NpcLine("{=kWruAXaF}Well... You know as long as those bandits remain there, no traveller is safe.[if:convo_grave]")
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
		}

		private void OnQuestAccepted()
		{
			StartQuest();
			_targetHideout.Hideout.IsSpotted = true;
			_targetHideout.IsVisible = true;
			AddTrackedObject(_targetHideout);
			QuestHelper.AddMapArrowFromPointToTarget(new TextObject("{=xpsQyPaV}Direction to Bandits"), _questSettlement.Position, _targetHideout.Position, 5f, 0.1f);
			TextObject textObject = new TextObject("{=XGa8MkbJ}{QUEST_GIVER.NAME} has marked the hideout on your map");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
			MBInformationManager.AddQuickInformation(textObject);
			AddLog(OnQuestStartedLogText);
		}

		private void OnQuestSucceeded()
		{
			AddLog(OnQuestSucceededLogText);
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
			GainRenownAction.Apply(Hero.MainHero, 1f);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 50)
			});
			base.QuestGiver.AddPower(5f);
			RelationshipChangeWithQuestGiver = 5;
			_questSettlement.Village.Bound.Town.Prosperity += 10f;
			CompleteQuestWithSuccess();
		}

		private void OnQuestFailed(bool isTimedOut)
		{
			AddLog(OnQuestFailedLogText);
			RelationshipChangeWithQuestGiver = -5;
			base.QuestGiver.AddPower(-5f);
			_questSettlement.Village.Bound.Town.Prosperity += -10f;
			_questSettlement.Village.Bound.Town.Security += -5f;
			if (!isTimedOut)
			{
				CompleteQuestWithFail();
			}
		}

		private void OnQuestCanceled()
		{
			AddLog(OnQuestCanceledLogText);
			CompleteQuestWithFail();
		}

		protected override void OnTimedOut()
		{
			OnQuestFailed(isTimedOut: true);
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
			CampaignEvents.OnHideoutDeactivatedEvent.AddNonSerializedListener(this, OnHideoutCleared);
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
		}

		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
		{
			if (DiplomacyHelper.IsWarCausedByPlayer(faction1, faction2, detail))
			{
				WarCausedByPlayerFail();
			}
		}

		private void WarCausedByPlayerFail()
		{
			base.QuestGiver.AddPower(-10f);
			RelationshipChangeWithQuestGiver = -5;
			TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -50)
			});
			_questSettlement.Village.Bound.Town.Prosperity += -10f;
			_questSettlement.Village.Bound.Town.Security += -10f;
			CompleteQuestWithFail(OnQuestFailedFromWarCausedByPlayerLogText);
		}

		private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
			{
				QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
			}
		}

		private void OnHideoutCleared(Settlement hideout)
		{
			if (_targetHideout == hideout)
			{
				CompleteQuestWithCancel();
			}
		}

		private void OnMapEventEnded(MapEvent mapEvent)
		{
			if (!mapEvent.IsHideoutBattle || mapEvent.MapEventSettlement != _targetHideout)
			{
				return;
			}
			if (mapEvent.InvolvedParties.Contains(PartyBase.MainParty))
			{
				if (mapEvent.BattleState == BattleState.DefenderVictory)
				{
					OnQuestFailed(isTimedOut: false);
				}
				else if (mapEvent.BattleState == BattleState.AttackerVictory)
				{
					OnQuestSucceeded();
				}
			}
			else if (mapEvent.BattleState == BattleState.AttackerVictory)
			{
				OnQuestCanceled();
			}
		}
	}

	public class NearbyBanditBaseIssueTypeDefiner : SaveableTypeDefiner
	{
		public NearbyBanditBaseIssueTypeDefiner()
			: base(400000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(NearbyBanditBaseIssue), 1);
			AddClassDefinition(typeof(NearbyBanditBaseIssueQuest), 2);
		}
	}

	private const IssueBase.IssueFrequency NearbyHideoutIssueFrequency = IssueBase.IssueFrequency.VeryCommon;

	private float NearbyHideoutMaxRange => Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Default) * 0.5f;

	private Settlement FindSuitableHideout(Hero issueOwner)
	{
		Settlement result = null;
		float num = float.MaxValue;
		for (int i = 0; i < Campaign.Current.AllHideouts.Count; i++)
		{
			Hideout hideout = Campaign.Current.AllHideouts[i];
			if (hideout.IsInfested)
			{
				float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(hideout.Settlement, issueOwner.CurrentSettlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default);
				if (distance <= NearbyHideoutMaxRange && distance < num)
				{
					num = distance;
					result = hideout.Settlement;
				}
			}
		}
		return result;
	}

	private void OnCheckForIssue(Hero hero)
	{
		if (!hero.IsNotable)
		{
			return;
		}
		if (ConditionsHold(hero))
		{
			Settlement settlement = FindSuitableHideout(hero);
			if (settlement != null)
			{
				Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnIssueSelected, typeof(NearbyBanditBaseIssue), IssueBase.IssueFrequency.VeryCommon, settlement));
			}
			else
			{
				Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(NearbyBanditBaseIssue), IssueBase.IssueFrequency.VeryCommon));
			}
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(NearbyBanditBaseIssue), IssueBase.IssueFrequency.VeryCommon));
		}
	}

	private IssueBase OnIssueSelected(in PotentialIssueData pid, Hero issueOwner)
	{
		return new NearbyBanditBaseIssue(issueOwner, pid.RelatedObject as Settlement);
	}

	private bool ConditionsHold(Hero issueGiver)
	{
		if (issueGiver.IsHeadman && issueGiver.CurrentSettlement != null)
		{
			return issueGiver.CurrentSettlement.Village.Bound.Town.Security <= 50f;
		}
		return false;
	}

	private void OnIssueUpdated(IssueBase issue, IssueBase.IssueUpdateDetails details, Hero issueSolver = null)
	{
		if (!(issue is NearbyBanditBaseIssue nearbyBanditBaseIssue) || details != IssueBase.IssueUpdateDetails.IssueFinishedByAILord)
		{
			return;
		}
		foreach (MobileParty party in nearbyBanditBaseIssue.TargetHideout.Parties)
		{
			party.SetMovePatrolAroundSettlement(nearbyBanditBaseIssue.TargetHideout, MobileParty.NavigationType.Default, isTargetingPort: false);
		}
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
		CampaignEvents.OnIssueUpdatedEvent.AddNonSerializedListener(this, OnIssueUpdated);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
