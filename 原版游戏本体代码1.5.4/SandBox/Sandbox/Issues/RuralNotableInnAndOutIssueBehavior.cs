using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox.BoardGames.MissionLogics;
using SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace SandBox.Issues;

public class RuralNotableInnAndOutIssueBehavior : CampaignBehaviorBase
{
	public class RuralNotableInnAndOutIssueTypeDefiner : SaveableTypeDefiner
	{
		public RuralNotableInnAndOutIssueTypeDefiner()
			: base(585900)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(RuralNotableInnAndOutIssue), 1);
			AddClassDefinition(typeof(RuralNotableInnAndOutIssueQuest), 2);
		}
	}

	public class RuralNotableInnAndOutIssue : IssueBase
	{
		private const int CompanionSkillLimit = 120;

		private const int QuestMoneyLimit = 2000;

		private const int AlternativeSolutionGoldCost = 1000;

		private CultureObject.BoardGameType _boardGameType;

		private Settlement _targetSettlement;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.FailureRisk;

		protected override bool IssueQuestCanBeDuplicated => false;

		public override int AlternativeSolutionBaseNeededMenCount => 1 + MathF.Ceiling(3f * base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 1 + MathF.Ceiling(3f * base.IssueDifficultyMultiplier);

		protected override int RewardGold => 1000;

		public override TextObject Title => new TextObject("{=uUhtKnfA}Inn and Out");

		public override TextObject Description
		{
			get
			{
				TextObject textObject = new TextObject("{=swamqBRq}{ISSUE_OWNER.NAME} wants you to beat the game host");
				StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject IssueBriefByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=T0zupcGB}Ah yes... It is a bit embarrassing to mention, [ib:nervous][if:convo_nervous]but... Well, when I am in town, I often have a drink at the inn and perhaps play a round of {GAME_TYPE} or two. Normally I play for low stakes but let's just say that last time the wine went to my head, and I lost something I couldn't afford to lose.");
				textObject.SetTextVariable("GAME_TYPE", GameTexts.FindText("str_boardgame_name", _boardGameType.ToString()));
				return textObject;
			}
		}

		public override TextObject IssueAcceptByPlayer => new TextObject("{=h2tMadtI}I've heard that story before. What did you lose?");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=LD4tGYCA}It's a deed to a plot of farmland. Not a big or valuable plot,[ib:normal][if:convo_disbelief] mind you, but I'd rather not have to explain to my men why they won't be sowing it this year. You can find the man who took it from me at the tavern in {TARGET_SETTLEMENT}. They call him the \"Game Host\". Just be straight about what you're doing. He's in no position to work the land. I don't imagine that he'll turn down a chance to make more money off of it. Bring it back and {REWARD}{GOLD_ICON} is yours.");
				textObject.SetTextVariable("REWARD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.Name);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=urCXu9Fc}Well, I could try and buy it from him, but I would not really prefer that.[if:convo_innocent_smile] I would be the joke of the tavern for months to come... If you choose to do that, I can only offer {REWARD}{GOLD_ICON} to compensate for your payment. If you have a man with a knack for such games he might do the trick.");
				textObject.SetTextVariable("REWARD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		public override TextObject IssueQuestSolutionAcceptByPlayer => new TextObject("{=KMThnMbt}I'll go to the tavern and win it back the same way you lost it.");

		public override TextObject IssueAlternativeSolutionAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=QdKWaabR}Worry not {ISSUE_OWNER.NAME}, my men will be back with your deed in no time.");
				StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject IssueDiscussAlternativeSolution => new TextObject("{=1yEyUHJe}I really hope your men can get my deed back. [if:convo_excited]On my father's name, I will never gamble again.");

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=kiaN39yb}Thank you, {PLAYER.NAME}. I'm sure your companion will be persuasive.[if:convo_relaxed_happy]");
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
				return textObject;
			}
		}

		public override bool IsThereAlternativeSolution => true;

		public override bool IsThereLordSolution => false;

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=MIxzaqzi}{QUEST_GIVER.LINK} told you that he lost a land deed in a wager in {TARGET_CITY}. He needs to buy it back, and he wants your companions to intimidate the seller into offering a reasonable price. You asked {COMPANION.LINK} to take {TROOP_COUNT} of your men to go and take care of it. They should report back to you in {RETURN_DAYS} days.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				textObject.SetTextVariable("TARGET_CITY", _targetSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				textObject.SetTextVariable("TROOP_COUNT", AlternativeSolutionSentTroops.TotalManCount - 1);
				return textObject;
			}
		}

		protected override int CompanionSkillRewardXP => (int)(500f + 1000f * base.IssueDifficultyMultiplier);

		public RuralNotableInnAndOutIssue(Hero issueOwner)
			: base(issueOwner, CampaignTime.DaysFromNow(30f))
		{
			InitializeQuestVariables();
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.SettlementProsperity)
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
			return ((hero.GetSkillValue(DefaultSkills.Charm) >= hero.GetSkillValue(DefaultSkills.Tactics)) ? DefaultSkills.Charm : DefaultSkills.Tactics, 120);
		}

		public override bool AlternativeSolutionCondition(out TextObject explanation)
		{
			if (QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation))
			{
				return QuestHelper.CheckGoldForAlternativeSolution(1000, out explanation);
			}
			return false;
		}

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			RelationshipChangeWithIssueOwner = 5;
			GainRenownAction.Apply(Hero.MainHero, 5f);
			base.IssueOwner.CurrentSettlement.Village.Bound.Town.Loyalty += 5f;
		}

		protected override void AlternativeSolutionEndWithFailureConsequence()
		{
			RelationshipChangeWithIssueOwner -= 5;
			base.IssueOwner.CurrentSettlement.Village.Bound.Town.Loyalty -= 5f;
		}

		public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation);
		}

		public override IssueFrequency GetFrequency()
		{
			return IssueFrequency.Common;
		}

		public override bool IssueStayAliveConditions()
		{
			BoardGameCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<BoardGameCampaignBehavior>();
			if (campaignBehavior != null && !campaignBehavior.WonBoardGamesInOneWeekInSettlement.Contains(_targetSettlement) && !base.IssueOwner.CurrentSettlement.IsRaided)
			{
				return !base.IssueOwner.CurrentSettlement.IsUnderRaid;
			}
			return false;
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}

		private void InitializeQuestVariables()
		{
			_targetSettlement = base.IssueOwner.CurrentSettlement.Village.Bound;
			_boardGameType = _targetSettlement.Culture.BoardGame;
		}

		protected override void OnGameLoad()
		{
			InitializeQuestVariables();
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new RuralNotableInnAndOutIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(14f), RewardGold);
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
			if (Hero.MainHero.Gold < 2000)
			{
				requiredGold = 2000;
				flag |= PreconditionFlags.Money;
			}
			return flag == PreconditionFlags.None;
		}

		internal static void AutoGeneratedStaticCollectObjectsRuralNotableInnAndOutIssue(object o, List<object> collectedObjects)
		{
			((RuralNotableInnAndOutIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		}
	}

	public class RuralNotableInnAndOutIssueQuest : QuestBase
	{
		public const int LesserReward = 800;

		private CultureObject.BoardGameType _boardGameType;

		private Settlement _targetSettlement;

		private bool _checkForBoardGameEnd;

		private bool _playerWonTheGame;

		private bool _applyLesserReward;

		[SaveableField(1)]
		private int _tryCount;

		private TextObject QuestStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=tirG1BB2}{QUEST_GIVER.LINK} told you that he lost a land deed while playing games in a tavern in {TARGET_SETTLEMENT}. He wants you to go find the game host and win it back for him. You told him that you will take care of the situation yourself.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject SuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=bvhWLb4C}You defeated the Game Host and got the deed back. {QUEST_GIVER.LINK}.{newline}\"Thank you for resolving this issue so neatly. Please accept these {GOLD}{GOLD_ICON} denars with our gratitude.\"");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("GOLD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		private TextObject SuccessWithPayingLog
		{
			get
			{
				TextObject textObject = new TextObject("{=TIPxWsYW}You have bought the deed from the game host. {QUEST_GIVER.LINK}.{newline}\"I am happy that I got my land back. I'm not so happy that everyone knows I had to pay for it, but... Anyway, please accept these {GOLD}{GOLD_ICON} denars with my gratitude.\"");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("GOLD", 800);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		private TextObject LostLog
		{
			get
			{
				TextObject textObject = new TextObject("{=ye4oqBFB}You lost the board game and failed to help {QUEST_GIVER.LINK}. \"Thank you for trying, {PLAYER.NAME}, but I guess I chose the wrong person for the job.\"");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
				return textObject;
			}
		}

		private TextObject QuestCanceledTargetVillageRaided
		{
			get
			{
				TextObject textObject = new TextObject("{=DLesz9jI}{QUEST_GIVER.LINK}’s village is raided. {?QUEST_GIVER.GENDER}She{?}He{\\?} flees to the countryside, and your agreement with {?QUEST_GIVER.GENDER}her{?}him{\\?} is canceled.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject QuestCanceledWarDeclared
		{
			get
			{
				TextObject textObject = new TextObject("{=cKz1cyuM}Your clan is now at war with {QUEST_GIVER_SETTLEMENT_FACTION}. Quest is canceled.");
				textObject.SetTextVariable("QUEST_GIVER_SETTLEMENT_FACTION", base.QuestGiver.CurrentSettlement.MapFaction.Name);
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

		private TextObject QuestCanceledSettlementIsUnderSiege
		{
			get
			{
				TextObject textObject = new TextObject("{=b5LdBYpF}{SETTLEMENT} is under siege. Your agreement with {QUEST_GIVER.LINK} is canceled.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject TimeoutLog
		{
			get
			{
				TextObject textObject = new TextObject("{=XLy8anVr}You received a message from {QUEST_GIVER.LINK}. \"This may not have seemed like an important task, but I placed my trust in you. I guess I was wrong to do so.\"");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject Title => new TextObject("{=uUhtKnfA}Inn and Out");

		public override bool IsRemainingTimeHidden => false;

		public RuralNotableInnAndOutIssueQuest(string questId, Hero giverHero, CampaignTime duration, int rewardGold)
			: base(questId, giverHero, duration, rewardGold)
		{
			InitializeQuestVariables();
			SetDialogs();
			InitializeQuestOnCreation();
		}

		private void InitializeQuestVariables()
		{
			_targetSettlement = base.QuestGiver.CurrentSettlement.Village.Bound;
			_boardGameType = _targetSettlement.Culture.BoardGame;
		}

		private void QuestAcceptedConsequences()
		{
			StartQuest();
			AddLog(QuestStartLog);
			AddTrackedObject(_targetSettlement);
		}

		protected override void InitializeQuestOnGameLoad()
		{
			InitializeQuestVariables();
			SetDialogs();
			if (Campaign.Current.GetCampaignBehavior<BoardGameCampaignBehavior>() == null)
			{
				CompleteQuestWithCancel();
			}
		}

		protected override void HourlyTick()
		{
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.OnPlayerBoardGameOverEvent.AddNonSerializedListener(this, OnBoardGameEnd);
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
			CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, OnSiegeStarted);
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
			CampaignEvents.VillageBeingRaided.AddNonSerializedListener(this, OnVillageBeingRaided);
			CampaignEvents.LocationCharactersSimulatedEvent.AddNonSerializedListener(this, OnLocationCharactersSimulated);
		}

		private void OnLocationCharactersSimulated()
		{
			if (Settlement.CurrentSettlement == null || Settlement.CurrentSettlement != _targetSettlement || Campaign.Current.GameMenuManager.MenuLocations.Count <= 0 || !(Campaign.Current.GameMenuManager.MenuLocations[0].StringId == "tavern"))
			{
				return;
			}
			foreach (Agent agent in Mission.Current.Agents)
			{
				LocationCharacter locationCharacter = LocationComplex.Current.GetLocationWithId("tavern").GetLocationCharacter(agent.Origin);
				if (locationCharacter != null && locationCharacter.Character.Occupation == Occupation.TavernGameHost)
				{
					locationCharacter.IsVisualTracked = true;
				}
			}
		}

		private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
			{
				QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
			}
		}

		private void OnVillageBeingRaided(Village village)
		{
			if (village == base.QuestGiver.CurrentSettlement.Village)
			{
				CompleteQuestWithCancel(QuestCanceledTargetVillageRaided);
			}
		}

		private void OnBoardGameEnd(Hero opposingHero, BoardGameHelper.BoardGameState state)
		{
			if (_checkForBoardGameEnd)
			{
				_playerWonTheGame = state == BoardGameHelper.BoardGameState.Win;
			}
		}

		private void OnSiegeStarted(SiegeEvent siegeEvent)
		{
			if (siegeEvent.BesiegedSettlement == _targetSettlement)
			{
				CompleteQuestWithCancel(QuestCanceledSettlementIsUnderSiege);
			}
		}

		protected override void SetDialogs()
		{
			TextObject textObject = new TextObject("{=I6amLvVE}Good, good. That's the best way to do these things. [if:convo_normal]Go to {TARGET_SETTLEMENT}, find this game host and wipe the smirk off of his face.");
			textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.Name);
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(textObject).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.Consequence(QuestAcceptedConsequences)
				.CloseDialog();
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(new TextObject("{=HGRWs0zE}Have you met the man who took my deed? Did you get it back?[if:convo_astonished]")).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=uJPAYUU7}I will be on my way soon enough."))
				.NpcLine(new TextObject("{=MOmePlJQ}Could you hurry this along? I don't want him to find another buyer.[if:convo_pondering] Thank you."))
				.CloseDialog()
				.PlayerOption(new TextObject("{=azVhRGik}I am waiting for the right moment."))
				.NpcLine(new TextObject("{=bRMLn0jj}Well, if he wanders off to another town, or gets his throat slit,[if:convo_pondering] or loses the deed, that would be the wrong moment, now wouldn't it?"))
				.CloseDialog()
				.EndPlayerOptions();
			Campaign.Current.ConversationManager.AddDialogFlow(GetGameHostDialogFlow(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetGameHostDialogueAfterFirstGame(), this);
		}

		private DialogFlow GetGameHostDialogFlow()
		{
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine("{=dzWioKRa}Hello there, are you looking for a friendly match? A wager perhaps?[if:convo_mocking_aristocratic]").Condition(() => TavernHostDialogCondition(isInitialDialogue: true))
				.PlayerLine(new TextObject("{=eOle8pYT}You won a deed of land from my associate. I'm here to win it back."))
				.NpcLine("{=bEipgE5E}Ah, yes, these are the most interesting kinds of games, aren't they? [if:convo_excited]I won't deny myself the pleasure but clearly that deed is worth more to him than just the value of the land. I'll wager the deed, but you need to put up 1000 denars.")
				.BeginPlayerOptions()
				.PlayerOption("{=XvkSbY6N}I see your wager. Let's play.")
				.Condition(() => Hero.MainHero.Gold >= 1000)
				.Consequence(StartBoardGame)
				.CloseDialog()
				.PlayerOption("{=89b5ao7P}As of now, I do not have 1000 denars to afford on gambling. I may get back to you once I get the required amount.")
				.Condition(() => Hero.MainHero.Gold < 1000)
				.NpcLine(new TextObject("{=ppi6eVos}As you wish."))
				.CloseDialog()
				.PlayerOption("{=WrnvRayQ}Let's just save ourselves some trouble, and I'll just pay you that amount.")
				.ClickableCondition(CheckPlayerHasEnoughDenarsClickableCondition)
				.NpcLine("{=pa3RY39w}Sure. I'm happy to turn paper into silver... 1000 denars it is.[if:convo_evil_smile]")
				.Consequence(PlayerPaid1000QuestSuccess)
				.CloseDialog()
				.PlayerOption("{=BSeplVwe}That's too much. I will be back later.")
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
		}

		private DialogFlow GetGameHostDialogueAfterFirstGame()
		{
			return DialogFlow.CreateDialogFlow("start", 125).BeginNpcOptions().NpcOption(new TextObject("{=dyhZUHao}Well, I thought you were here to be sheared, [if:convo_shocked]but it looks like the sheep bites back. Very well, nicely played, here's your man's land back."), () => _playerWonTheGame && TavernHostDialogCondition())
				.Consequence(PlayerWonTheBoardGame)
				.CloseDialog()
				.NpcOption("{=TdnD29Ax}Ah! You almost had me! Maybe you just weren't paying attention. [if:convo_mocking_teasing]Care to put another 1000 denars on the table and have another go?", () => !_playerWonTheGame && _tryCount < 2 && TavernHostDialogCondition())
				.BeginPlayerOptions()
				.PlayerOption("{=fiMZ696A}Yes, I'll play again.")
				.ClickableCondition(CheckPlayerHasEnoughDenarsClickableCondition)
				.Consequence(StartBoardGame)
				.CloseDialog()
				.PlayerOption("{=zlFSIvD5}No, no. I know a trap when I see one. You win. Good-bye.")
				.NpcLine(new TextObject("{=ppi6eVos}As you wish."))
				.Consequence(PlayerFailAfterBoardGame)
				.CloseDialog()
				.EndPlayerOptions()
				.NpcOption("{=hkNrC5d3}That was fun, but I've learned not to inflict too great a humiliation on those who carry a sword.[if:convo_merry] I'll take my winnings and enjoy them now. Good-bye to you!", () => _tryCount >= 2 && TavernHostDialogCondition())
				.Consequence(PlayerFailAfterBoardGame)
				.CloseDialog()
				.EndNpcOptions();
		}

		private bool CheckPlayerHasEnoughDenarsClickableCondition(out TextObject explanation)
		{
			if (Hero.MainHero.Gold >= 1000)
			{
				explanation = null;
				return true;
			}
			explanation = new TextObject("{=AMlaYbJv}You don't have 1000 denars.");
			return false;
		}

		private bool TavernHostDialogCondition(bool isInitialDialogue = false)
		{
			if ((!_checkForBoardGameEnd || !isInitialDialogue) && Settlement.CurrentSettlement == _targetSettlement && CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.TavernGameHost && LocationComplex.Current?.GetLocationWithId("tavern") != null)
			{
				Mission.Current.GetMissionBehavior<MissionBoardGameLogic>().DetectOpposingAgent();
				return Mission.Current.GetMissionBehavior<MissionBoardGameLogic>().CheckIfBothSidesAreSitting();
			}
			return false;
		}

		private void PlayerPaid1000QuestSuccess()
		{
			AddLog(SuccessWithPayingLog);
			_applyLesserReward = true;
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, 1000);
			CompleteQuestWithSuccess();
		}

		protected override void OnFinalize()
		{
			if (Mission.Current == null)
			{
				return;
			}
			foreach (Agent agent in Mission.Current.Agents)
			{
				Location locationWithId = LocationComplex.Current.GetLocationWithId("tavern");
				if (locationWithId != null)
				{
					LocationCharacter locationCharacter = locationWithId.GetLocationCharacter(agent.Origin);
					if (locationCharacter != null && locationCharacter.Character.Occupation == Occupation.TavernGameHost)
					{
						locationCharacter.IsVisualTracked = false;
					}
				}
			}
		}

		private void ApplySuccessRewards()
		{
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, _applyLesserReward ? 800 : RewardGold);
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, 5);
			GainRenownAction.Apply(Hero.MainHero, 1f);
			base.QuestGiver.CurrentSettlement.Village.Bound.Town.Loyalty += 5f;
		}

		protected override void OnCompleteWithSuccess()
		{
			ApplySuccessRewards();
		}

		private void StartBoardGame()
		{
			MissionBoardGameLogic missionBehavior = Mission.Current.GetMissionBehavior<MissionBoardGameLogic>();
			Campaign.Current.GetCampaignBehavior<BoardGameCampaignBehavior>().SetBetAmount(1000);
			missionBehavior.DetectOpposingAgent();
			missionBehavior.SetCurrentDifficulty(BoardGameHelper.AIDifficulty.Normal);
			missionBehavior.SetBoardGame(_boardGameType);
			missionBehavior.StartBoardGame();
			_checkForBoardGameEnd = true;
			_tryCount++;
		}

		private void PlayerWonTheBoardGame()
		{
			AddLog(SuccessLog);
			CompleteQuestWithSuccess();
		}

		private void PlayerFailAfterBoardGame()
		{
			AddLog(LostLog);
			RelationshipChangeWithQuestGiver = -5;
			base.QuestGiver.CurrentSettlement.Village.Bound.Town.Loyalty -= 5f;
			CompleteQuestWithFail();
		}

		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (base.QuestGiver.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				CompleteQuestWithCancel(QuestCanceledWarDeclared);
			}
		}

		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
		{
			QuestHelper.CheckWarDeclarationAndFailOrCancelTheQuest(this, faction1, faction2, detail, PlayerDeclaredWarQuestLogText, QuestCanceledWarDeclared);
		}

		public override GameMenuOption.IssueQuestFlags IsLocationTrackedByQuest(Location location)
		{
			if (PlayerEncounter.LocationEncounter.Settlement == _targetSettlement && location.StringId == "tavern")
			{
				return GameMenuOption.IssueQuestFlags.ActiveIssue;
			}
			return GameMenuOption.IssueQuestFlags.None;
		}

		protected override void OnTimedOut()
		{
			RelationshipChangeWithQuestGiver = -5;
			base.QuestGiver.CurrentSettlement.Village.Bound.Town.Loyalty -= 5f;
			AddLog(TimeoutLog);
		}

		internal static void AutoGeneratedStaticCollectObjectsRuralNotableInnAndOutIssueQuest(object o, List<object> collectedObjects)
		{
			((RuralNotableInnAndOutIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		internal static object AutoGeneratedGetMemberValue_tryCount(object o)
		{
			return ((RuralNotableInnAndOutIssueQuest)o)._tryCount;
		}
	}

	private const IssueBase.IssueFrequency RuralNotableInnAndOutIssueFrequency = IssueBase.IssueFrequency.Common;

	private const float IssueDuration = 30f;

	private const float QuestDuration = 14f;

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private bool ConditionsHold(Hero issueGiver)
	{
		if ((issueGiver.IsRuralNotable || issueGiver.IsHeadman) && issueGiver.CurrentSettlement.Village != null && issueGiver.CurrentSettlement.Village.Bound.IsTown && issueGiver.GetTraitLevel(DefaultTraits.Mercy) + issueGiver.GetTraitLevel(DefaultTraits.Honor) < 0 && Campaign.Current.GetCampaignBehavior<BoardGameCampaignBehavior>() != null)
		{
			return issueGiver.CurrentSettlement.Village.Bound.Culture.BoardGame != CultureObject.BoardGameType.None;
		}
		return false;
	}

	public void OnCheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero))
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnSelected, typeof(RuralNotableInnAndOutIssue), IssueBase.IssueFrequency.Common));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(RuralNotableInnAndOutIssue), IssueBase.IssueFrequency.Common));
		}
	}

	private IssueBase OnSelected(in PotentialIssueData pid, Hero issueOwner)
	{
		return new RuralNotableInnAndOutIssue(issueOwner);
	}
}
