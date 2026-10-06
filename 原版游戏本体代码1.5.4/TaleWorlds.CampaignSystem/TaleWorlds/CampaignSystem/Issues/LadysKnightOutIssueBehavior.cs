using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues;

public class LadysKnightOutIssueBehavior : CampaignBehaviorBase
{
	public class LadysKnightOutIssue : IssueBase
	{
		private const int TakingQuestRelationLimit = -10;

		private const int QuestTimeLimit = 30;

		private const int IssueDuration = 30;

		private const int BaseRewardGold = 750;

		public override TextObject IssueBriefByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=M3af4N66}If you follow tournaments in this region, you'll know that I am a great devotee. I attend as many as I can - I love the spectacle, the tension... Despite this, I've never had a champion fight in my name, which is quite the fashion these days. Would you consider being my champion, {PLAYER.NAME}? I'm sure you could win glory for both of us.[if:convo_merry][ib:confident3]");
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
				return textObject;
			}
		}

		public override TextObject IssueAcceptByPlayer => new TextObject("{=4nbnFMNZ}I can't promise to win, my lady.");

		public override TextObject IssueLordSolutionExplanationByIssueGiver => new TextObject("{=SzIPvqLk}Of course not. If you don't wish to fight yourself - and I understand you might be quite busy - you could use your influence to convince one of the nobles of this realm to hold their next tournament in my honor.[ib:demure2][if:convo_merry]");

		public override TextObject IssuePlayerResponseAfterLordExplanation => new TextObject("{=MuwmsOSn}I will consider this. What does it mean to be your champion?");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=Pnc8ToV1}Just participate in a future tournament in this realm, and say you dedicate your victories to me. {TOURNAMENT_ROUND_GOAL}");
				if (TournamentRoundGoal == 5)
				{
					textObject.SetTextVariable("TOURNAMENT_ROUND_GOAL", new TextObject("{=2Rzw16OX}If you can advance to win the tournament, I'm sure that will do us both honor.[ib:hip2][if:convo_relaxed_happy]"));
				}
				else
				{
					TextObject textObject2 = new TextObject("{=al27CmYV}If you can advance to reach round {ROUND_COUNT}, I'm sure that will do us both honor.[if:convo_calm_friendly][ib:demure]");
					textObject2.SetTextVariable("ROUND_COUNT", TournamentRoundGoal);
					textObject.SetTextVariable("TOURNAMENT_ROUND_GOAL", textObject2);
				}
				return textObject;
			}
		}

		public override TextObject IssueQuestSolutionAcceptByPlayer
		{
			get
			{
				if (TournamentRoundGoal == 5)
				{
					return new TextObject("{=YArbm6TV}Then I will enter the tournament and win it, my lady.");
				}
				TextObject textObject = new TextObject("{=w3rcCibp}Then I will enter the tournament and reach round {ROUND_COUNT} my lady.");
				textObject.SetTextVariable("ROUND_COUNT", TournamentRoundGoal);
				return textObject;
			}
		}

		public override TextObject IssueLordSolutionAcceptByPlayer => new TextObject("{=0WOPfOIH}I'm afraid I don't have time to fight but I can use my influence.");

		public override TextObject IssueLordSolutionResponseByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=ZZ7Q3VOG}Thank you my {?PLAYER.GENDER}lady{?}lord{\\?}. I am indeed honored.");
				StringHelpers.SetCharacterProperties("PLAYER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		protected override TextObject LordSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=EnhdXpQf}{ISSUE_OWNER.LINK} from {ISSUE_OWNER.FACTION}, has told you about of the tournament at the {ISSUE_OWNER_SETTLEMENT}. She wants you to be her champion. {TOURNAMENT_ROUND_GOAL}");
				StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.IssueOwner.CharacterObject, textObject, includeDetails: true);
				textObject.SetTextVariable("ISSUE_OWNER_SETTLEMENT", base.IssueOwner.CurrentSettlement.EncyclopediaLinkWithName);
				if (TournamentRoundGoal == 5)
				{
					textObject.SetTextVariable("TOURNAMENT_ROUND_GOAL", new TextObject("{=YbzHpFiu}She expects you to win the tournament."));
				}
				else
				{
					TextObject textObject2 = new TextObject("{=5qkTxOJ5}She expects you to reach round {ROUND_COUNT}.");
					textObject2.SetTextVariable("ROUND_COUNT", TournamentRoundGoal);
					textObject.SetTextVariable("TOURNAMENT_ROUND_GOAL", textObject2);
				}
				return textObject;
			}
		}

		protected override TextObject LordSolutionCounterOfferRefuseLog
		{
			get
			{
				TextObject textObject = new TextObject("{=IPg30HIs}You told {ISSUE_OWNER.LINK} that instead of her request, the next tournament held will be held in her honor.");
				StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		public override bool IsThereAlternativeSolution => false;

		public override bool IsThereLordSolution => true;

		public override TextObject Title => new TextObject("{=a4XGmdd9}Lady's Knight Out");

		public override TextObject Description
		{
			get
			{
				TextObject textObject = new TextObject("{=oBl6SxeJ}{ISSUE_OWNER.LINK} wants you to participate in a tournament in her name.");
				StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		private int TournamentRoundGoal
		{
			get
			{
				if (base.IssueDifficultyMultiplier > 0.7f)
				{
					return 5;
				}
				if (0.7f >= base.IssueDifficultyMultiplier && base.IssueDifficultyMultiplier > 0.5f)
				{
					return 4;
				}
				if (0.5f >= base.IssueDifficultyMultiplier && base.IssueDifficultyMultiplier > 0.2f)
				{
					return 3;
				}
				return 2;
			}
		}

		public override int NeededInfluenceForLordSolution => 15 + TaleWorlds.Library.MathF.Ceiling(30f * base.IssueDifficultyMultiplier);

		internal static void AutoGeneratedStaticCollectObjectsLadysKnightOutIssue(object o, List<object> collectedObjects)
		{
			((LadysKnightOutIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		public LadysKnightOutIssue(Hero issueOwner)
			: base(issueOwner, CampaignTime.DaysFromNow(30f))
		{
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.SettlementLoyalty)
			{
				return -0.2f;
			}
			if (issueEffect == DefaultIssueEffects.ClanInfluence)
			{
				return -0.1f;
			}
			return 0f;
		}

		protected override void OnGameLoad()
		{
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new LadysKnightOutIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(30f), base.IssueDifficultyMultiplier, 750);
		}

		public override IssueFrequency GetFrequency()
		{
			return IssueFrequency.Common;
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

		public override bool IssueStayAliveConditions()
		{
			if (base.IssueOwner.IsNoncombatant && base.IssueOwner.CurrentSettlement != null && base.IssueOwner.CurrentSettlement.IsTown)
			{
				return base.IssueOwner.CurrentSettlement.Town.Security <= 80f;
			}
			return false;
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}

		protected override void LordSolutionConsequenceWithRefuseCounterOffer()
		{
			Clan.PlayerClan.AddRenown(7f);
			ChangeRelationAction.ApplyPlayerRelation(base.IssueOwner, 5);
		}
	}

	public class LadysKnightOutIssueQuest : QuestBase
	{
		[SaveableField(1)]
		private readonly float _difficultyMultiplier;

		[SaveableField(2)]
		private Town _tournamentTown;

		private int TournamentRoundGoal
		{
			get
			{
				if (_difficultyMultiplier > 0.7f)
				{
					return 5;
				}
				if (0.7f >= _difficultyMultiplier && _difficultyMultiplier > 0.5f)
				{
					return 4;
				}
				if (0.5f >= _difficultyMultiplier && _difficultyMultiplier > 0.2f)
				{
					return 3;
				}
				return 2;
			}
		}

		private TextObject QuestStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=kqgadhCF}{QUEST_GIVER.LINK} from {QUEST_GIVER.FACTION}, has asked you to be her champion in an upcoming tournament. {TOURNAMENT_ROUND_GOAL}{newline}You told her that you will honor her name in a tournament.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, includeDetails: true);
				textObject.SetTextVariable("QUEST_GIVER_TOWN", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
				if (TournamentRoundGoal == 5)
				{
					textObject.SetTextVariable("TOURNAMENT_ROUND_GOAL", new TextObject("{=r8VcYkUf}She expects you to fight in her name and win the tournament."));
				}
				else
				{
					TextObject textObject2 = new TextObject("{=edJtO2ua}She expects you to fight in her name and reach at least {ROUND_COUNT}.");
					textObject2.SetTextVariable("ROUND_COUNT", TournamentRoundGoal);
					textObject.SetTextVariable("TOURNAMENT_ROUND_GOAL", textObject2);
				}
				return textObject;
			}
		}

		private TextObject PlayerWinsTournamentSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=QAg8DQy6}You received a message from {QUEST_GIVER.LINK}.{newline}\"Thank you for fighting so valiantly in my name. Please take these {REWARD} denars with our gratitude.\"");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("REWARD", RewardGold);
				return textObject;
			}
		}

		private TextObject PlayerFailedToReachTournamentLevelLog
		{
			get
			{
				TextObject textObject = new TextObject("{=04ti5hcX}{TOURNAMENT_ROUND_GOAL}{QUEST_GIVER.LINK} will certainly be disappointed.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				if (TournamentRoundGoal == 5)
				{
					textObject.SetTextVariable("TOURNAMENT_ROUND_GOAL", new TextObject("{=tDNiLiRM}You have failed to win the tournament."));
				}
				else
				{
					TextObject textObject2 = new TextObject("{=CxKC5gqq}You have failed to reach round {ROUND_COUNT} in the tournament. ");
					textObject2.SetTextVariable("ROUND_COUNT", TournamentRoundGoal);
					textObject.SetTextVariable("TOURNAMENT_ROUND_GOAL", textObject2);
				}
				return textObject;
			}
		}

		private TextObject PlayerFailedToJoinTournamentLog
		{
			get
			{
				TextObject textObject = new TextObject("{=D40RPEbz}You have failed to enter a tournament in time. {QUEST_GIVER.LINK} will certainly be disappointed.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject WarDeclaredOnQuestGiversFactionCancelLog
		{
			get
			{
				TextObject textObject = new TextObject("{=zh3Wf6bu}You are now at war with {QUEST_GIVER.LINK}'s faction. Quest is canceled.");
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

		private TextObject QuestGiverIsDeadCancelLog
		{
			get
			{
				TextObject textObject = new TextObject("{=2Ju7Eduu}{QUEST_GIVER.LINK} has died. Your quest is canceled.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject QuestGiverLeftSettlementCancelLog
		{
			get
			{
				TextObject textObject = new TextObject("{=1RBxhh4W}{QUEST_GIVER.LINK} is not avaliable for this quest anymore.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject MainHeroAttackedLadysClansVillageLog
		{
			get
			{
				TextObject textObject = new TextObject("{=Qft5pfYr}You have been accused of a crime, and {QUEST_GIVER.LINK} has declared that you are no longer her champion.");
				textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
				return textObject;
			}
		}

		public override TextObject Title => new TextObject("{=a4XGmdd9}Lady's Knight Out");

		public override bool IsRemainingTimeHidden => false;

		internal static void AutoGeneratedStaticCollectObjectsLadysKnightOutIssueQuest(object o, List<object> collectedObjects)
		{
			((LadysKnightOutIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_tournamentTown);
		}

		internal static object AutoGeneratedGetMemberValue_difficultyMultiplier(object o)
		{
			return ((LadysKnightOutIssueQuest)o)._difficultyMultiplier;
		}

		internal static object AutoGeneratedGetMemberValue_tournamentTown(object o)
		{
			return ((LadysKnightOutIssueQuest)o)._tournamentTown;
		}

		public LadysKnightOutIssueQuest(string questId, Hero questGiver, CampaignTime duration, float issueDifficultyMultiplier, int rewardGold)
			: base(questId, questGiver, duration, rewardGold)
		{
			_difficultyMultiplier = issueDifficultyMultiplier;
			_tournamentTown = null;
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
			CampaignEvents.PlayerStartedTournamentMatch.AddNonSerializedListener(this, OnPlayerStartedTournamentMatch);
			CampaignEvents.PlayerEliminatedFromTournament.AddNonSerializedListener(this, OnPlayerEliminatedFromTournament);
			CampaignEvents.TournamentFinished.AddNonSerializedListener(this, OnTournamentFinished);
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
			CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
		}

		private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			if ((mapEvent.IsForcingSupplies || mapEvent.IsForcingVolunteers || mapEvent.IsRaid) && attackerParty == PartyBase.MainParty && mapEvent.MapEventSettlement.IsVillage && mapEvent.MapEventSettlement.OwnerClan == base.QuestGiver.Clan)
			{
				CriminalActionPerformedTowardsSettlement();
			}
			else if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
			{
				QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
			}
		}

		protected override void OnTimedOut()
		{
			base.OnTimedOut();
			PlayerFailedToJoinTournamentInTime();
		}

		protected override void SetDialogs()
		{
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(new TextObject("{=at0iWxo1}It is exciting to think a warrior of your caliber will be my champion. {TOURNAMENT_ROUND_GOAL}")).Condition(QuestAcceptedCondition)
				.Consequence(QuestAcceptedConsequences)
				.CloseDialog();
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(new TextObject("{=iPcbFoDx}Are you prepared for the tournament? Eating well? Staying healthy?")).Condition(() => CharacterObject.OneToOneConversationCharacter == base.QuestGiver.CharacterObject)
				.PlayerLine(new TextObject("{=YOmuyrIt}Do you know if there is a tournament starting soon?"))
				.BeginNpcOptions()
				.NpcOption(new TextObject("{=CeYwClaG}Yes, there is one starting at {NEARBY_TOURNAMENTS_LIST}. I am sure the arena master can explain the rules, if you need to know them."), NpcTournamentLocationCondition)
				.CloseDialog()
				.NpcDefaultOption("{=sUfSCLQx}Sadly, I've heard no news of an upcoming tournament. I am sure one will be held before too long.")
				.CloseDialog()
				.EndNpcOptions()
				.CloseDialog();
		}

		private bool QuestAcceptedCondition()
		{
			if (TournamentRoundGoal == 5)
			{
				MBTextManager.SetTextVariable("TOURNAMENT_ROUND_GOAL", new TextObject("{=nctTI5mv}I have every reason to believe you'll win the tournament."));
			}
			else
			{
				TextObject textObject = new TextObject("{=3FMvLhWV}I have every reason to believe you'll reach round {ROUND_COUNT}.");
				textObject.SetTextVariable("ROUND_COUNT", TournamentRoundGoal);
				MBTextManager.SetTextVariable("TOURNAMENT_ROUND_GOAL", textObject);
			}
			return CharacterObject.OneToOneConversationCharacter == base.QuestGiver.CharacterObject;
		}

		private void QuestAcceptedConsequences()
		{
			StartQuest();
			AddLog(QuestStartLog);
		}

		private bool NpcTournamentLocationCondition()
		{
			List<Town> source = Town.AllTowns.Where((Town x) => Campaign.Current.TournamentManager.GetTournamentGame(x) != null && x != Settlement.CurrentSettlement.Town).ToList();
			source = source.OrderBy((Town x) => DistanceHelper.FindClosestDistanceFromSettlementToSettlement(x.Settlement, Settlement.CurrentSettlement, MobileParty.NavigationType.Default)).ToList();
			if (source.Count > 0)
			{
				MBTextManager.SetTextVariable("NEARBY_TOURNAMENTS_LIST", source[0].Name);
				return true;
			}
			return false;
		}

		private void OnPlayerStartedTournamentMatch(Town town)
		{
			_tournamentTown = town;
		}

		private void OnPlayerEliminatedFromTournament(int round, Town settlement)
		{
			if (TournamentRoundGoal > round + 1)
			{
				PlayerCouldntReachedTournamentRoundGoal();
			}
			else
			{
				PlayerReachedTournamentRoundGoal();
			}
		}

		private void OnTournamentFinished(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town, ItemObject prize)
		{
			if (town == _tournamentTown && winner == CharacterObject.PlayerCharacter)
			{
				PlayerReachedTournamentRoundGoal();
			}
		}

		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (base.QuestGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				CompleteQuestWithCancel(WarDeclaredOnQuestGiversFactionCancelLog);
			}
		}

		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
		{
			QuestHelper.CheckWarDeclarationAndFailOrCancelTheQuest(this, faction1, faction2, detail, PlayerDeclaredWarQuestLogText, WarDeclaredOnQuestGiversFactionCancelLog);
		}

		private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
		{
			if (victim == base.QuestGiver)
			{
				QuestGiverIsDead();
			}
		}

		private void OnSettlementLeft(MobileParty party, Settlement settlement)
		{
			if (base.QuestGiver.CurrentSettlement == null)
			{
				QuestGiverLeftSettlement();
			}
		}

		private void PlayerReachedTournamentRoundGoal()
		{
			AddLog(PlayerWinsTournamentSuccessLog);
			Clan.PlayerClan.AddRenown(1f);
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, 5);
			CompleteQuestWithSuccess();
		}

		private void PlayerCouldntReachedTournamentRoundGoal()
		{
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -5);
			CompleteQuestWithFail(PlayerFailedToReachTournamentLevelLog);
		}

		private void PlayerFailedToJoinTournamentInTime()
		{
			AddLog(PlayerFailedToJoinTournamentLog);
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -5);
		}

		private void QuestGiverIsDead()
		{
			CompleteQuestWithCancel(QuestGiverIsDeadCancelLog);
		}

		private void QuestGiverLeftSettlement()
		{
			CompleteQuestWithCancel(QuestGiverLeftSettlementCancelLog);
		}

		private void CriminalActionPerformedTowardsSettlement()
		{
			RelationshipChangeWithQuestGiver = -5;
			Tuple<TraitObject, int>[] effectedTraits = new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -50)
			};
			TraitLevelingHelper.OnIssueSolvedThroughQuest(Hero.MainHero, effectedTraits);
			CompleteQuestWithFail(MainHeroAttackedLadysClansVillageLog);
		}
	}

	public class LadysKnightOutIssueTypeDefiner : SaveableTypeDefiner
	{
		public LadysKnightOutIssueTypeDefiner()
			: base(585700)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(LadysKnightOutIssue), 1);
			AddClassDefinition(typeof(LadysKnightOutIssueQuest), 2);
		}
	}

	private const IssueBase.IssueFrequency LadysKnightOutIssueFrequency = IssueBase.IssueFrequency.Common;

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
	}

	public void OnCheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero))
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnStartIssue, typeof(LadysKnightOutIssue), IssueBase.IssueFrequency.Common));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(LadysKnightOutIssue), IssueBase.IssueFrequency.Common));
		}
	}

	private bool ConditionsHold(Hero issueOwner)
	{
		if (issueOwner.IsLord && issueOwner.IsFemale && issueOwner.GetTraitLevel(DefaultTraits.Mercy) < 0 && issueOwner.GetTraitLevel(DefaultTraits.PersonaSoftspoken) <= 0 && issueOwner.GetTraitLevel(DefaultTraits.PersonaCurt) <= 0 && issueOwner.Clan.Leader != issueOwner && issueOwner.Clan != Clan.PlayerClan && issueOwner.IsNoncombatant && issueOwner.CurrentSettlement != null && issueOwner.CurrentSettlement.IsTown)
		{
			return issueOwner.CurrentSettlement.Town.Security <= 50f;
		}
		return false;
	}

	private IssueBase OnStartIssue(in PotentialIssueData pid, Hero issueOwner)
	{
		return new LadysKnightOutIssue(issueOwner);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
