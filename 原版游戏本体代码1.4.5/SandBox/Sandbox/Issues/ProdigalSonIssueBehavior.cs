using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox.Conversation.MissionLogics;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace SandBox.Issues;

public class ProdigalSonIssueBehavior : CampaignBehaviorBase
{
	public class ProdigalSonIssueTypeDefiner : SaveableTypeDefiner
	{
		public ProdigalSonIssueTypeDefiner()
			: base(345000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(ProdigalSonIssue), 1);
			AddClassDefinition(typeof(ProdigalSonIssueQuest), 2);
		}
	}

	public class ProdigalSonIssue : IssueBase
	{
		private const int IssueDurationInDays = 50;

		private const int QuestDurationInDays = 24;

		private const int TroopTierForAlternativeSolution = 2;

		private const int RequiredSkillValueForAlternativeSolution = 120;

		[SaveableField(10)]
		private readonly Hero _prodigalSon;

		[SaveableField(20)]
		private readonly Hero _targetHero;

		[SaveableField(30)]
		private readonly Location _targetHouse;

		private Settlement _targetSettlement;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.FailureRisk;

		private Clan Clan => base.IssueOwner.Clan;

		protected override int RewardGold => 1200 + (int)(3000f * base.IssueDifficultyMultiplier);

		public override TextObject IssueBriefByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=5a6KlSXt}I have a problem. [ib:normal2][if:convo_pondering]My young kinsman {PRODIGAL_SON.LINK} has gone to town to have fun, drinking, wenching and gambling. Many young men do that, but it seems he was a bit reckless. Now he sends news that he owes a large sum of money to {TARGET_HERO.LINK}, one of the local gang bosses in the city of {SETTLEMENT_LINK}. These ruffians are holding him as a “guest” in their house until someone pays his debt.");
				StringHelpers.SetCharacterProperties("PRODIGAL_SON", _prodigalSon.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("TARGET_HERO", _targetHero.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT_LINK", _targetSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		public override TextObject IssueAcceptByPlayer => new TextObject("{=YtS3cgto}What are you planning to do?");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=ZC1slXw1}I'm not inclined to pay the debt. [ib:closed][if:convo_worried]I'm not going to reward this kind of lawlessness, when even the best families aren't safe. I've sent word to the lord of {SETTLEMENT_NAME} but I can't say I expect to hear back, what with the wars and all. I want someone to go there and free the lad. You could pay, I suppose, but I'd prefer it if you taught those bastards a lesson. I'll pay you either way but obviously you get to keep more if you use force.");
				textObject.SetTextVariable("SETTLEMENT_NAME", _targetSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		public override TextObject IssuePlayerResponseAfterAlternativeExplanation => new TextObject("{=4zf1lg6L}I could go myself, or send a companion.");

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=CWbAoGRu}Yes, I don't care how you solve it. [if:convo_normal]Just solve it any way you like. I reckon {NEEDED_MEN_COUNT} led by someone who knows how to handle thugs could solve this in about {ALTERNATIVE_SOLUTION_DURATION} days. I'd send my own men but it could cause complications for us to go marching in wearing our clan colors in another lord's territory.");
				textObject.SetTextVariable("NEEDED_MEN_COUNT", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("ALTERNATIVE_SOLUTION_DURATION", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		public override TextObject IssueQuestSolutionAcceptByPlayer => new TextObject("{=aKbyJsho}I will free your kinsman myself.");

		public override TextObject IssueAlternativeSolutionAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=PuuVGOyM}I will send {NEEDED_MEN_COUNT} of my men with one of my lieutenants for {ALTERNATIVE_SOLUTION_DURATION} days to help you.");
				textObject.SetTextVariable("NEEDED_MEN_COUNT", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("ALTERNATIVE_SOLUTION_DURATION", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		public override TextObject IssueDiscussAlternativeSolution => new TextObject("{=qxhMagyZ}I'm glad someone's on it.[if:convo_relaxed_happy] Just see that they do it quickly.");

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver => new TextObject("{=mDXzDXKY}Very good. [if:convo_relaxed_happy]I'm sure you'll chose competent men to bring our boy back.");

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=Z9sp21rl}{QUEST_GIVER.LINK}, a lord from the {QUEST_GIVER_CLAN} clan, asked you to free {?QUEST_GIVER.GENDER}her{?}his{\\?} relative. The young man is currently held by {TARGET_HERO.LINK} a local gang leader because of his debts. {?QUEST_GIVER.GENDER}Lady{?}Lord{\\?} {QUEST_GIVER.LINK} has given you enough gold to settle {?QUEST_GIVER.GENDER}her{?}his{\\?} debts but {?QUEST_GIVER.GENDER}she{?}he{\\?} encourages you to keep the money to yourself and make an example of these criminals so no one would dare to hold a nobleman again. You have sent {COMPANION.LINK} and {NEEDED_MEN_COUNT} men to take care of the situation for you. They should be back in {ALTERNATIVE_SOLUTION_DURATION} days.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("TARGET_HERO", _targetHero.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_GIVER_CLAN", base.IssueOwner.Clan.EncyclopediaLinkWithName);
				textObject.SetTextVariable("SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("NEEDED_MEN_COUNT", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("ALTERNATIVE_SOLUTION_DURATION", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=IXnvQ8kG}{COMPANION.LINK} and the men you sent with {?COMPANION.GENDER}her{?}him{\\?} safely return with the news of success. {QUEST_GIVER.LINK} is happy and sends you {?QUEST_GIVER.GENDER}her{?}his{\\?} regards with {REWARD}{GOLD_ICON} the money he promised.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				textObject.SetTextVariable("REWARD", RewardGold);
				return textObject;
			}
		}

		public override bool IsThereAlternativeSolution => true;

		public override int AlternativeSolutionBaseNeededMenCount => 1 + TaleWorlds.Library.MathF.Ceiling(3f * base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 7 + TaleWorlds.Library.MathF.Ceiling(7f * base.IssueDifficultyMultiplier);

		protected override int CompanionSkillRewardXP => (int)(700f + 900f * base.IssueDifficultyMultiplier);

		public override bool IsThereLordSolution => false;

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=Mr2rt8g8}Prodigal Son of {CLAN_NAME}");
				textObject.SetTextVariable("CLAN_NAME", Clan.Name);
				return textObject;
			}
		}

		public override TextObject Description
		{
			get
			{
				TextObject textObject = new TextObject("{=5puy0Jle}{ISSUE_OWNER.NAME} asks the player to aid a young clan member. He is supposed to have huge gambling debts so the gang leaders holds him as a hostage. You are asked to retrieve him any way possible.");
				StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		public ProdigalSonIssue(Hero issueOwner, Hero prodigalSon, Hero targetGangHero)
			: base(issueOwner, CampaignTime.DaysFromNow(50f))
		{
			_prodigalSon = prodigalSon;
			_targetHero = targetGangHero;
			_targetSettlement = _targetHero.CurrentSettlement;
			_targetHouse = _targetSettlement.LocationComplex.GetListOfLocations().FirstOrDefault((Location x) => x.CanBeReserved && !x.IsReserved);
			TextObject textObject = new TextObject("{=EZ19JOGj}{MENTOR.NAME}'s House");
			StringHelpers.SetCharacterProperties("MENTOR", _targetHero.CharacterObject, textObject);
			_targetHouse.ReserveLocation(textObject, textObject);
			DisableHeroAction.Apply(_prodigalSon);
		}

		public override void OnHeroCanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
		{
			if (hero == _targetHero || hero == _prodigalSon)
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
			return 0f;
		}

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			return ((hero.GetSkillValue(DefaultSkills.Charm) >= hero.GetSkillValue(DefaultSkills.Roguery)) ? DefaultSkills.Charm : DefaultSkills.Roguery, 120);
		}

		protected override void OnGameLoad()
		{
			Town town = Town.AllTowns.FirstOrDefault((Town x) => x.Settlement.LocationComplex.GetListOfLocations().Contains(_targetHouse));
			if (town != null)
			{
				_targetSettlement = town.Settlement;
			}
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new ProdigalSonIssueQuest(questId, base.IssueOwner, _targetHero, _prodigalSon, _targetHouse, base.IssueDifficultyMultiplier, CampaignTime.DaysFromNow(24f), RewardGold);
		}

		public override IssueFrequency GetFrequency()
		{
			return IssueFrequency.Rare;
		}

		protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out PreconditionFlags flag, out Hero relationHero, out SkillObject skill, out int requiredGold)
		{
			bool flag2 = issueGiver.GetRelationWithPlayer() >= -10f && !issueGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction) && Clan.PlayerClan.Tier >= 1;
			flag = ((!flag2) ? (issueGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction) ? PreconditionFlags.AtWar : ((Clan.PlayerClan.Tier >= 1) ? PreconditionFlags.Relation : PreconditionFlags.ClanTier)) : PreconditionFlags.None);
			relationHero = issueGiver;
			skill = null;
			requiredGold = 0;
			return flag2;
		}

		public override bool IssueStayAliveConditions()
		{
			return _targetHero.IsActive;
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
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
			base.AlternativeSolutionHero.AddSkillXp(DefaultSkills.Charm, (int)(700f + 900f * base.IssueDifficultyMultiplier));
			RelationshipChangeWithIssueOwner = 5;
			GainRenownAction.Apply(Hero.MainHero, 3f);
		}

		protected override void AlternativeSolutionEndWithFailureConsequence()
		{
			RelationshipChangeWithIssueOwner = -5;
		}

		protected override void OnIssueFinalized()
		{
			if (_prodigalSon.HeroState == Hero.CharacterStates.Disabled)
			{
				_prodigalSon.ChangeState(Hero.CharacterStates.Released);
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsProdigalSonIssue(object o, List<object> collectedObjects)
		{
			((ProdigalSonIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_prodigalSon);
			collectedObjects.Add(_targetHero);
			collectedObjects.Add(_targetHouse);
		}

		internal static object AutoGeneratedGetMemberValue_prodigalSon(object o)
		{
			return ((ProdigalSonIssue)o)._prodigalSon;
		}

		internal static object AutoGeneratedGetMemberValue_targetHero(object o)
		{
			return ((ProdigalSonIssue)o)._targetHero;
		}

		internal static object AutoGeneratedGetMemberValue_targetHouse(object o)
		{
			return ((ProdigalSonIssue)o)._targetHouse;
		}
	}

	public class ProdigalSonIssueQuest : QuestBase
	{
		private const PersuasionDifficulty Difficulty = PersuasionDifficulty.Hard;

		private const int DistanceSquaredToStartConversation = 4;

		private const int CrimeRatingCancelRelationshipPenalty = -5;

		private const int CrimeRatingCancelHonorXpPenalty = -50;

		[SaveableField(10)]
		private readonly Hero _targetHero;

		[SaveableField(20)]
		private readonly Hero _prodigalSon;

		[SaveableField(30)]
		private bool _playerTalkedToTargetHero;

		[SaveableField(40)]
		private readonly Location _targetHouse;

		[SaveableField(50)]
		private readonly float _questDifficulty;

		[SaveableField(60)]
		private bool _isHouseFightFinished;

		[SaveableField(70)]
		private bool _playerTriedToPersuade;

		private PersuasionTask _task;

		private bool _isMissionFightInitialized;

		private bool _isFirstMissionTick;

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=Mr2rt8g8}Prodigal Son of {CLAN_NAME}");
				textObject.SetTextVariable("CLAN_NAME", base.QuestGiver.Clan.Name);
				return textObject;
			}
		}

		public override bool IsRemainingTimeHidden => false;

		private Settlement Settlement => _targetHero.CurrentSettlement;

		private int DebtWithInterest => (int)((float)RewardGold * 1.1f);

		private TextObject QuestStartedLog
		{
			get
			{
				TextObject textObject = new TextObject("{=CXw9a1i5}{QUEST_GIVER.LINK}, a {?QUEST_GIVER.GENDER}lady{?}lord{\\?} from the {QUEST_GIVER_CLAN} clan, asked you to go to {SETTLEMENT} to free {?QUEST_GIVER.GENDER}her{?}his{\\?} relative. The young man is currently held by {TARGET_HERO.LINK}, a local gang leader, because of his debts. {QUEST_GIVER.LINK} has suggested that you make an example of the gang so no one would dare to hold a nobleman again. {?QUEST_GIVER.GENDER}She{?}He{\\?} said you can easily find the house in which the young nobleman is held in the town square.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("TARGET_HERO", _targetHero.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_GIVER_CLAN", base.QuestGiver.Clan.EncyclopediaLinkWithName);
				textObject.SetTextVariable("SETTLEMENT", Settlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject PlayerDefeatsThugsQuestSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=axLR9bQo}You have defeated the thugs that held {PRODIGAL_SON.LINK} as {QUEST_GIVER.LINK} has asked you to. {?QUEST_GIVER.GENDER}Lady{?}Lord{\\?} {QUEST_GIVER.LINK} soon sends {?QUEST_GIVER.GENDER}her{?}his{\\?} best regards and a sum of {REWARD}{GOLD_ICON} as a reward.");
				StringHelpers.SetCharacterProperties("PRODIGAL_SON", _prodigalSon.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("REWARD", RewardGold);
				return textObject;
			}
		}

		private TextObject PlayerPaysTheDebtQuestSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=skMoB7c6}You have paid the debt that {PRODIGAL_SON.LINK} owes. True to {?TARGET_HERO.GENDER}her{?}his{\\?} word {TARGET_HERO.LINK} releases the boy immediately. Soon after, {?QUEST_GIVER.GENDER}Lady{?}Lord{\\?} {QUEST_GIVER.LINK} sends {?QUEST_GIVER.GENDER}her{?}his{\\?} best regards and a sum of {REWARD}{GOLD_ICON} as a reward.");
				StringHelpers.SetCharacterProperties("PRODIGAL_SON", _prodigalSon.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("TARGET_HERO", _targetHero.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("REWARD", RewardGold);
				return textObject;
			}
		}

		private TextObject QuestTimeOutFailLog
		{
			get
			{
				TextObject textObject = new TextObject("{=dmijPqWn}You have failed to extract {QUEST_GIVER.LINK}'s relative captive in time. They have moved the boy to a more secure place. Its impossible to find him now. {QUEST_GIVER.LINK} will have to deal with {TARGET_HERO.LINK} himself now. {?QUEST_GIVER.GENDER}She{?}He{\\?} won't be happy to hear this.");
				StringHelpers.SetCharacterProperties("TARGET_HERO", _targetHero.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject PlayerHasDefeatedQuestFailLog
		{
			get
			{
				TextObject textObject = new TextObject("{=d5a8xQos}You have failed to defeat the thugs that keep {QUEST_GIVER.LINK}'s relative captive. After your assault you learn that they move the boy to a more secure place. Now its impossible to find him. {QUEST_GIVER.LINK} will have to deal with {TARGET_HERO.LINK} himself now. {?QUEST_GIVER.GENDER}She{?}He{\\?} won't be happy to hear this.");
				StringHelpers.SetCharacterProperties("TARGET_HERO", _targetHero.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject PlayerConvincesGangLeaderQuestSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=Rb7g1U2s}You have convinced {TARGET_HERO.LINK} to release {PRODIGAL_SON.LINK}. Soon after, {?QUEST_GIVER.GENDER}Lady{?}Lord{\\?} {QUEST_GIVER.LINK} sends {?QUEST_GIVER.GENDER}her{?}his{\\?} best regards and a sum of {REWARD}{GOLD_ICON} as a reward.");
				StringHelpers.SetCharacterProperties("PRODIGAL_SON", _prodigalSon.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("TARGET_HERO", _targetHero.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("REWARD", RewardGold);
				return textObject;
			}
		}

		private TextObject WarDeclaredQuestCancelLog
		{
			get
			{
				TextObject textObject = new TextObject("{=VuqZuSe2}Your clan is now at war with the {QUEST_GIVER.LINK}'s faction. Your agreement has been canceled.");
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

		private TextObject CrimeRatingCancelLog
		{
			get
			{
				TextObject textObject = new TextObject("{=oulvvl52}You are accused in {SETTLEMENT} of a crime, and {QUEST_GIVER.LINK} no longer trusts you in this matter.");
				textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
				textObject.SetTextVariable("SETTLEMENT", Settlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		public ProdigalSonIssueQuest(string questId, Hero questGiver, Hero targetHero, Hero prodigalSon, Location targetHouse, float questDifficulty, CampaignTime duration, int rewardGold)
			: base(questId, questGiver, duration, rewardGold)
		{
			_targetHero = targetHero;
			_prodigalSon = prodigalSon;
			_targetHouse = targetHouse;
			_questDifficulty = questDifficulty;
			SetDialogs();
			InitializeQuestOnCreation();
		}

		protected override void SetDialogs()
		{
			TextObject textObject = new TextObject("{=bQnVtegC}Good, even better. [ib:confident][if:convo_astonished]You can find the house easily when you go to {SETTLEMENT} and walk around the town square. Or you could just speak to this gang leader, {TARGET_HERO.LINK}, and make {?TARGET_HERO.GENDER}her{?}him{\\?} understand and get my boy released. Good luck. I await good news.");
			StringHelpers.SetCharacterProperties("TARGET_HERO", _targetHero.CharacterObject, textObject);
			Settlement settlement = ((_targetHero.CurrentSettlement != null) ? _targetHero.CurrentSettlement : _targetHero.PartyBelongedTo.HomeSettlement);
			textObject.SetTextVariable("SETTLEMENT", settlement.EncyclopediaLinkWithName);
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(textObject).Condition(is_talking_to_quest_giver)
				.Consequence(QuestAcceptedConsequences)
				.CloseDialog();
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(new TextObject("{=TkYk5yxn}Yes? Go already. Get our boy back.[if:convo_excited]")).Condition(is_talking_to_quest_giver)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=kqXxvtwQ}Don't worry I'll free him."))
				.NpcLine(new TextObject("{=ddEu5IFQ}I hope so."))
				.Consequence(MapEventHelper.OnConversationEnd)
				.CloseDialog()
				.PlayerOption(new TextObject("{=Jss9UqZC}I'll go right away"))
				.NpcLine(new TextObject("{=IdKG3IaS}Good to hear that."))
				.Consequence(MapEventHelper.OnConversationEnd)
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
			Campaign.Current.ConversationManager.AddDialogFlow(GetTargetHeroDialogFlow(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetProdigalSonDialogFlow(), this);
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
			CampaignEvents.BeforeMissionOpenedEvent.AddNonSerializedListener(this, BeforeMissionOpened);
			CampaignEvents.MissionTickEvent.AddNonSerializedListener(this, OnMissionTick);
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
			CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
			CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
		}

		private void OnMissionStarted(IMission mission)
		{
			if (CampaignMission.Current?.Location == _targetHouse)
			{
				_isFirstMissionTick = true;
			}
		}

		public override void OnHeroCanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
		{
			if (hero == _prodigalSon || hero == _targetHero)
			{
				result = false;
			}
		}

		public override void OnHeroCanMoveToSettlementInfoIsRequested(Hero hero, ref bool result)
		{
			if (hero == _prodigalSon)
			{
				result = false;
			}
		}

		private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
			{
				QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
			}
		}

		private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
		{
			if (victim == _targetHero || victim == _prodigalSon)
			{
				TextObject textObject = ((detail == KillCharacterAction.KillCharacterActionDetail.Lost) ? TargetHeroDisappearedLogText : TargetHeroDiedLogText);
				StringHelpers.SetCharacterProperties("QUEST_TARGET", victim.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				AddLog(textObject);
				CompleteQuestWithCancel();
			}
		}

		protected override void OnTimedOut()
		{
			FinishQuestFail1();
		}

		protected override void OnFinalize()
		{
			_targetHouse.RemoveReservation();
		}

		private void BeforeMissionOpened()
		{
			if (Settlement.CurrentSettlement != Settlement || LocationComplex.Current == null)
			{
				return;
			}
			if (LocationComplex.Current.GetLocationOfCharacter(_prodigalSon) == null)
			{
				SpawnProdigalSonInHouse();
				if (!_isHouseFightFinished)
				{
					SpawnThugsInHouse();
					_isMissionFightInitialized = false;
				}
			}
			foreach (AccompanyingCharacter character in PlayerEncounter.LocationEncounter.CharactersAccompanyingPlayer)
			{
				if (!character.CanEnterLocation(_targetHouse))
				{
					character.AllowEntranceToLocations((Location x) => character.CanEnterLocation(x) || x == _targetHouse);
				}
			}
		}

		private void OnMissionTick(float dt)
		{
			if (CampaignMission.Current.Location != _targetHouse)
			{
				return;
			}
			Mission current = Mission.Current;
			if (_isFirstMissionTick)
			{
				Mission.Current.Agents.First((Agent x) => x.Character == _prodigalSon.CharacterObject).GetComponent<CampaignAgentComponent>().AgentNavigator.RemoveBehaviorGroup<AlarmedBehaviorGroup>();
				_isFirstMissionTick = false;
			}
			if (_isMissionFightInitialized || _isHouseFightFinished || current.Agents.Count <= 0)
			{
				return;
			}
			_isMissionFightInitialized = true;
			MissionFightHandler missionBehavior = current.GetMissionBehavior<MissionFightHandler>();
			List<Agent> list = new List<Agent>();
			List<Agent> list2 = new List<Agent>();
			foreach (Agent agent in current.Agents)
			{
				if (agent.IsEnemyOf(Agent.Main))
				{
					list.Add(agent);
				}
				else if (agent.Team == Agent.Main.Team)
				{
					list2.Add(agent);
				}
			}
			missionBehavior.StartCustomFight(list2, list, dropWeapons: false, isItemUseDisabled: false, HouseFightFinished);
			foreach (Agent item in list)
			{
				item.Defensiveness = 2f;
			}
		}

		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
		{
			if (base.QuestGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				if (detail == DeclareWarAction.DeclareWarDetail.CausedByCrimeRatingChange)
				{
					RelationshipChangeWithQuestGiver = -5;
					TraitLevelingHelper.OnIssueSolvedThroughQuest(effectedTraits: new Tuple<TraitObject, int>[1]
					{
						new Tuple<TraitObject, int>(DefaultTraits.Honor, -50)
					}, targetHero: Hero.MainHero);
				}
				if (DiplomacyHelper.IsWarCausedByPlayer(faction1, faction2, detail))
				{
					CompleteQuestWithFail(PlayerDeclaredWarQuestLogText);
				}
				else
				{
					CompleteQuestWithCancel((detail == DeclareWarAction.DeclareWarDetail.CausedByCrimeRatingChange) ? CrimeRatingCancelLog : WarDeclaredQuestCancelLog);
				}
			}
		}

		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (clan == Clan.PlayerClan && ((newKingdom != null && newKingdom.IsAtWarWith(base.QuestGiver.MapFaction)) || (newKingdom == null && clan.IsAtWarWith(base.QuestGiver.MapFaction))))
			{
				CompleteQuestWithCancel(WarDeclaredQuestCancelLog);
			}
		}

		private void HouseFightFinished(bool isPlayerSideWon)
		{
			if (isPlayerSideWon)
			{
				Agent agent = Mission.Current.Agents.FirstOrDefaultQ((Agent x) => x.Character == _prodigalSon.CharacterObject);
				if (agent == null)
				{
					Debug.Print("Prodigal son id: " + _prodigalSon.CharacterObject.StringId);
					Debug.Print("Mission agent count: " + Mission.Current.Agents.Count);
					foreach (Agent agent2 in Mission.Current.Agents)
					{
						Debug.Print(string.Concat("Agent: ", agent2.Character.Name, ", id: ", agent2.Character.StringId, ", team: ", agent2.Team));
					}
				}
				if (agent.Position.Distance(Agent.Main.Position) > agent.GetInteractionDistanceToUsable(Agent.Main))
				{
					ScriptBehavior.AddTargetWithDelegate(agent, SelectPlayerAsTarget, null, OnTargetReached);
				}
				else
				{
					Agent targetAgent = null;
					UsableMachine targetUsableMachine = null;
					WorldFrame targetFrame = WorldFrame.Invalid;
					OnTargetReached(agent, ref targetAgent, ref targetUsableMachine, ref targetFrame);
				}
			}
			else
			{
				FinishQuestFail2();
			}
			_isHouseFightFinished = true;
		}

		private bool OnTargetReached(Agent agent, ref Agent targetAgent, ref UsableMachine targetUsableMachine, ref WorldFrame targetFrame)
		{
			Mission.Current.GetMissionBehavior<MissionConversationLogic>().StartConversation(agent, setActionsInstantly: false);
			targetAgent = null;
			return false;
		}

		private bool SelectPlayerAsTarget(Agent agent, ref Agent targetAgent, ref UsableMachine targetUsableMachine, ref WorldFrame targetFrame, ref float customTargetReachedRangeThreshold, ref float customTargetReachedRotationThreshold)
		{
			targetAgent = null;
			if (agent.Position.Distance(Agent.Main.Position) > agent.GetInteractionDistanceToUsable(Agent.Main))
			{
				targetAgent = Agent.Main;
			}
			return targetAgent != null;
		}

		private void SpawnProdigalSonInHouse()
		{
			Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(_prodigalSon.CharacterObject.Race, "_settlement");
			LocationCharacter locationCharacter = new LocationCharacter(new AgentData(new SimpleAgentOrigin(_prodigalSon.CharacterObject)).Monster(monsterWithSuffix), SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "npc_common", fixedLocation: true, LocationCharacter.CharacterRelations.Neutral, null, useCivilianEquipment: true);
			_targetHouse.AddCharacter(locationCharacter);
		}

		private void SpawnThugsInHouse()
		{
			CharacterObject item = MBObjectManager.Instance.GetObject<CharacterObject>("gangster_1");
			CharacterObject item2 = MBObjectManager.Instance.GetObject<CharacterObject>("gangster_2");
			CharacterObject item3 = MBObjectManager.Instance.GetObject<CharacterObject>("gangster_3");
			List<CharacterObject> list = new List<CharacterObject>();
			if (_questDifficulty < 0.4f)
			{
				list.Add(item);
				list.Add(item);
				if (_questDifficulty >= 0.2f)
				{
					list.Add(item2);
				}
			}
			else if (_questDifficulty < 0.6f)
			{
				list.Add(item);
				list.Add(item2);
				list.Add(item2);
			}
			else
			{
				list.Add(item2);
				list.Add(item3);
				list.Add(item3);
			}
			foreach (CharacterObject item4 in list)
			{
				Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(item4.Race, "_settlement");
				LocationCharacter locationCharacter = new LocationCharacter(new AgentData(new SimpleAgentOrigin(item4)).Monster(monsterWithSuffix), SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "npc_common", fixedLocation: true, LocationCharacter.CharacterRelations.Enemy, null, useCivilianEquipment: true);
				_targetHouse.AddCharacter(locationCharacter);
			}
		}

		private void QuestAcceptedConsequences()
		{
			StartQuest();
			AddTrackedObject(Settlement);
			AddTrackedObject(_targetHero);
			AddLog(QuestStartedLog);
		}

		private DialogFlow GetProdigalSonDialogFlow()
		{
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine("{=DYq30shK}Thank you, {?PLAYER.GENDER}milady{?}sir{\\?}.").Condition(() => Hero.OneToOneConversationHero == _prodigalSon)
				.NpcLine("{=K8TSoRSD}Did {?QUEST_GIVER.GENDER}Lady{?}Lord{\\?} {QUEST_GIVER.LINK} send you to rescue me?")
				.Condition(delegate
				{
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
					return true;
				})
				.PlayerLine("{=ln3bGyIO}Yes, I'm here to take you back.")
				.NpcLine("{=evIohG6b}Thank you, but there's no need. Once we are out of here I can manage to return on my own.[if:convo_happy] I appreciate your efforts. I'll tell everyone in my clan of your heroism.")
				.NpcLine("{=qsJxhNGZ}Safe travels {?PLAYER.GENDER}milady{?}sir{\\?}.")
				.Consequence(delegate
				{
					Mission.Current.Agents.First((Agent x) => x.Character == _prodigalSon.CharacterObject).GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().DisableScriptedBehavior();
					Campaign.Current.ConversationManager.ConversationEndOneShot += OnEndHouseMissionDialog;
				})
				.CloseDialog();
		}

		private DialogFlow GetTargetHeroDialogFlow()
		{
			DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 125).BeginNpcOptions().NpcOption(new TextObject("{=M0vxXQGB}Yes? Do you have something to say?[ib:closed][if:convo_nonchalant]"), () => Hero.OneToOneConversationHero == _targetHero && !_playerTalkedToTargetHero)
				.Consequence(delegate
				{
					StringHelpers.SetCharacterProperties("PRODIGAL_SON", _prodigalSon.CharacterObject);
					_playerTalkedToTargetHero = true;
				})
				.PlayerLine("{=K5DgDU2a}I am here for the boy. {PRODIGAL_SON.LINK}. You know who I mean.")
				.GotoDialogState("start")
				.NpcOption(new TextObject("{=I979VDEn}Yes, did you bring {GOLD_AMOUNT}{GOLD_ICON}? [ib:hip][if:convo_stern]That's what he owes... With an interest of course."), delegate
				{
					int num;
					if (Hero.OneToOneConversationHero == _targetHero)
					{
						num = (_playerTalkedToTargetHero ? 1 : 0);
						if (num != 0)
						{
							MBTextManager.SetTextVariable("GOLD_AMOUNT", DebtWithInterest);
						}
					}
					else
					{
						num = 0;
					}
					return (byte)num != 0;
				})
				.BeginPlayerOptions()
				.PlayerOption("{=IboStvbL}Here is the money, now release him!")
				.ClickableCondition(delegate(out TextObject explanation)
				{
					bool result = false;
					if (Hero.MainHero.Gold >= DebtWithInterest)
					{
						explanation = null;
						result = true;
					}
					else
					{
						explanation = new TextObject("{=YuLLsAUb}You don't have {GOLD_AMOUNT}{GOLD_ICON}.");
						explanation.SetTextVariable("GOLD_AMOUNT", DebtWithInterest);
					}
					return result;
				})
				.NpcLine("{=7k03GxZ1}It's great doing business with you. I'll order my men to release him immediately.[if:convo_mocking_teasing]")
				.Consequence(FinishQuestSuccess4)
				.CloseDialog()
				.PlayerOption("{=9pTkQ5o2}It would be in your interest to let this young nobleman go...")
				.Condition(() => !_playerTriedToPersuade)
				.Consequence(delegate
				{
					_playerTriedToPersuade = true;
					_task = GetPersuasionTask();
					persuasion_start_on_consequence();
				})
				.GotoDialogState("persuade_gang_start_reservation")
				.PlayerOption("{=AwZhx2tT}I will be back.")
				.NpcLine("{=0fp67gxl}Have a good day.")
				.CloseDialog()
				.EndPlayerOptions()
				.EndNpcOptions();
			AddPersuasionDialogs(dialogFlow);
			return dialogFlow;
		}

		private void AddPersuasionDialogs(DialogFlow dialog)
		{
			dialog.AddDialogLine("persuade_gang_introduction", "persuade_gang_start_reservation", "persuade_gang_player_option", "{=EIsQnfLP}Tell me how it's in my interest...[ib:closed][if:convo_nonchalant]", persuasion_start_on_condition, null, this);
			dialog.AddDialogLine("persuade_gang_success", "persuade_gang_start_reservation", "close_window", "{=alruamIW}Hmm... You may be right. It's not worth it. I'll release the boy immediately.[ib:hip][if:convo_pondering]", ConversationManager.GetPersuasionProgressSatisfied, persuasion_success_on_consequence, this, int.MaxValue);
			dialog.AddDialogLine("persuade_gang_failed", "persuade_gang_start_reservation", "start", "{=1YGgXOB7}Meh... Do you think ruling the streets of a city is easy? You underestimate us. Now, about the money.[ib:closed2][if:convo_nonchalant]", null, ConversationManager.EndPersuasion, this);
			ConversationSentence.OnConditionDelegate conditionDelegate = persuasion_select_option_1_on_condition;
			ConversationSentence.OnConsequenceDelegate consequenceDelegate = persuasion_select_option_1_on_consequence;
			ConversationSentence.OnPersuasionOptionDelegate persuasionOptionDelegate = persuasion_setup_option_1;
			ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = persuasion_clickable_option_1_on_condition;
			dialog.AddPlayerLine("persuade_gang_player_option_1", "persuade_gang_player_option", "persuade_gang_player_option_response", "{=!}{PERSUADE_GANG_ATTEMPT_1}", conditionDelegate, consequenceDelegate, this, 100, clickableConditionDelegate, persuasionOptionDelegate);
			ConversationSentence.OnConditionDelegate conditionDelegate2 = persuasion_select_option_2_on_condition;
			ConversationSentence.OnConsequenceDelegate consequenceDelegate2 = persuasion_select_option_2_on_consequence;
			persuasionOptionDelegate = persuasion_setup_option_2;
			clickableConditionDelegate = persuasion_clickable_option_2_on_condition;
			dialog.AddPlayerLine("persuade_gang_player_option_2", "persuade_gang_player_option", "persuade_gang_player_option_response", "{=!}{PERSUADE_GANG_ATTEMPT_2}", conditionDelegate2, consequenceDelegate2, this, 100, clickableConditionDelegate, persuasionOptionDelegate);
			ConversationSentence.OnConditionDelegate conditionDelegate3 = persuasion_select_option_3_on_condition;
			ConversationSentence.OnConsequenceDelegate consequenceDelegate3 = persuasion_select_option_3_on_consequence;
			persuasionOptionDelegate = persuasion_setup_option_3;
			clickableConditionDelegate = persuasion_clickable_option_3_on_condition;
			dialog.AddPlayerLine("persuade_gang_player_option_3", "persuade_gang_player_option", "persuade_gang_player_option_response", "{=!}{PERSUADE_GANG_ATTEMPT_3}", conditionDelegate3, consequenceDelegate3, this, 100, clickableConditionDelegate, persuasionOptionDelegate);
			dialog.AddDialogLine("persuade_gang_option_reaction", "persuade_gang_player_option_response", "persuade_gang_start_reservation", "{=!}{PERSUASION_REACTION}", persuasion_selected_option_response_on_condition, persuasion_selected_option_response_on_consequence, this);
		}

		private bool is_talking_to_quest_giver()
		{
			return Hero.OneToOneConversationHero == base.QuestGiver;
		}

		private bool persuasion_start_on_condition()
		{
			if (Hero.OneToOneConversationHero == _targetHero && !ConversationManager.GetPersuasionIsFailure())
			{
				return _task.Options.Any((PersuasionOptionArgs x) => !x.IsBlocked);
			}
			return false;
		}

		private void persuasion_selected_option_response_on_consequence()
		{
			Tuple<PersuasionOptionArgs, PersuasionOptionResult> tuple = ConversationManager.GetPersuasionChosenOptions().Last();
			float difficulty = Campaign.Current.Models.PersuasionModel.GetDifficulty(PersuasionDifficulty.Hard);
			Campaign.Current.Models.PersuasionModel.GetEffectChances(tuple.Item1, out var moveToNextStageChance, out var blockRandomOptionChance, difficulty);
			_task.ApplyEffects(moveToNextStageChance, blockRandomOptionChance);
		}

		private bool persuasion_selected_option_response_on_condition()
		{
			PersuasionOptionResult item = ConversationManager.GetPersuasionChosenOptions().Last().Item2;
			MBTextManager.SetTextVariable("PERSUASION_REACTION", PersuasionHelper.GetDefaultPersuasionOptionReaction(item));
			if (item == PersuasionOptionResult.CriticalFailure)
			{
				_task.BlockAllOptions();
			}
			return true;
		}

		private bool persuasion_select_option_1_on_condition()
		{
			if (_task.Options.Count > 0)
			{
				TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
				textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(_task.Options.ElementAt(0), showToPlayer: false));
				textObject.SetTextVariable("PERSUASION_OPTION_LINE", _task.Options.ElementAt(0).Line);
				MBTextManager.SetTextVariable("PERSUADE_GANG_ATTEMPT_1", textObject);
				return true;
			}
			return false;
		}

		private bool persuasion_select_option_2_on_condition()
		{
			if (_task.Options.Count > 1)
			{
				TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
				textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(_task.Options.ElementAt(1), showToPlayer: false));
				textObject.SetTextVariable("PERSUASION_OPTION_LINE", _task.Options.ElementAt(1).Line);
				MBTextManager.SetTextVariable("PERSUADE_GANG_ATTEMPT_2", textObject);
				return true;
			}
			return false;
		}

		private bool persuasion_select_option_3_on_condition()
		{
			if (_task.Options.Count > 2)
			{
				TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
				textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(_task.Options.ElementAt(2), showToPlayer: false));
				textObject.SetTextVariable("PERSUASION_OPTION_LINE", _task.Options.ElementAt(2).Line);
				MBTextManager.SetTextVariable("PERSUADE_GANG_ATTEMPT_3", textObject);
				return true;
			}
			return false;
		}

		private void persuasion_select_option_1_on_consequence()
		{
			if (_task.Options.Count > 0)
			{
				_task.Options[0].BlockTheOption(isBlocked: true);
			}
		}

		private void persuasion_select_option_2_on_consequence()
		{
			if (_task.Options.Count > 1)
			{
				_task.Options[1].BlockTheOption(isBlocked: true);
			}
		}

		private void persuasion_select_option_3_on_consequence()
		{
			if (_task.Options.Count > 2)
			{
				_task.Options[2].BlockTheOption(isBlocked: true);
			}
		}

		private PersuasionOptionArgs persuasion_setup_option_1()
		{
			return _task.Options.ElementAt(0);
		}

		private PersuasionOptionArgs persuasion_setup_option_2()
		{
			return _task.Options.ElementAt(1);
		}

		private PersuasionOptionArgs persuasion_setup_option_3()
		{
			return _task.Options.ElementAt(2);
		}

		private bool persuasion_clickable_option_1_on_condition(out TextObject hintText)
		{
			hintText = new TextObject("{=9ACJsI6S}Blocked");
			if (_task.Options.Count > 0)
			{
				hintText = (_task.Options.ElementAt(0).IsBlocked ? hintText : null);
				return !_task.Options.ElementAt(0).IsBlocked;
			}
			return false;
		}

		private bool persuasion_clickable_option_2_on_condition(out TextObject hintText)
		{
			hintText = new TextObject("{=9ACJsI6S}Blocked");
			if (_task.Options.Count > 1)
			{
				hintText = (_task.Options.ElementAt(1).IsBlocked ? hintText : null);
				return !_task.Options.ElementAt(1).IsBlocked;
			}
			return false;
		}

		private bool persuasion_clickable_option_3_on_condition(out TextObject hintText)
		{
			hintText = new TextObject("{=9ACJsI6S}Blocked");
			if (_task.Options.Count > 2)
			{
				hintText = (_task.Options.ElementAt(2).IsBlocked ? hintText : null);
				return !_task.Options.ElementAt(2).IsBlocked;
			}
			return false;
		}

		private void persuasion_success_on_consequence()
		{
			ConversationManager.EndPersuasion();
			FinishQuestSuccess3();
		}

		private void OnEndHouseMissionDialog()
		{
			Campaign.Current.GameMenuManager.NextLocation = LocationComplex.Current.GetLocationWithId("center");
			Campaign.Current.GameMenuManager.PreviousLocation = CampaignMission.Current.Location;
			Mission.Current.EndMission();
			FinishQuestSuccess1();
		}

		private PersuasionTask GetPersuasionTask()
		{
			PersuasionTask persuasionTask = new PersuasionTask(0);
			persuasionTask.FinalFailLine = TextObject.GetEmpty();
			persuasionTask.TryLaterLine = TextObject.GetEmpty();
			persuasionTask.SpokenLine = new TextObject("{=6P1ruzsC}Maybe...");
			PersuasionOptionArgs option = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.ExtremelyHard, givesCriticalSuccess: true, new TextObject("{=Lol4clzR}Look, it was a good try, but they're not going to pay. Releasing the kid is the only move that makes sense."));
			persuasionTask.AddOptionToTask(option);
			PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Mercy, TraitEffect.Negative, PersuasionArgumentStrength.Hard, givesCriticalSuccess: false, new TextObject("{=wJCVlVF7}These nobles aren't like you and me. They've kept their wealth by crushing people like you for generations. Don't mess with them."));
			persuasionTask.AddOptionToTask(option2);
			PersuasionOptionArgs option3 = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Generosity, TraitEffect.Positive, PersuasionArgumentStrength.Normal, givesCriticalSuccess: false, new TextObject("{=o1KOn4WZ}If you let this boy go, his family will remember you did them a favor. That's a better deal for you than a fight you can't hope to win."));
			persuasionTask.AddOptionToTask(option3);
			return persuasionTask;
		}

		private void persuasion_start_on_consequence()
		{
			ConversationManager.StartPersuasion(2f, 1f, 1f, 2f, 2f, 0f, PersuasionDifficulty.Hard);
		}

		private void FinishQuestSuccess1()
		{
			CompleteQuestWithSuccess();
			AddLog(PlayerDefeatsThugsQuestSuccessLog);
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, 5);
			GainRenownAction.Apply(Hero.MainHero, 3f);
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
		}

		private void FinishQuestSuccess3()
		{
			CompleteQuestWithSuccess();
			AddLog(PlayerConvincesGangLeaderQuestSuccessLog);
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, 5);
			GainRenownAction.Apply(Hero.MainHero, 1f);
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
		}

		private void FinishQuestSuccess4()
		{
			GainRenownAction.Apply(Hero.MainHero, 1f);
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, _targetHero, DebtWithInterest);
			CompleteQuestWithSuccess();
			AddLog(PlayerPaysTheDebtQuestSuccessLog);
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, 5);
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
		}

		private void FinishQuestFail1()
		{
			AddLog(QuestTimeOutFailLog);
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -5);
		}

		private void FinishQuestFail2()
		{
			CompleteQuestWithFail();
			AddLog(PlayerHasDefeatedQuestFailLog);
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -5);
		}

		internal static void AutoGeneratedStaticCollectObjectsProdigalSonIssueQuest(object o, List<object> collectedObjects)
		{
			((ProdigalSonIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_targetHero);
			collectedObjects.Add(_prodigalSon);
			collectedObjects.Add(_targetHouse);
		}

		internal static object AutoGeneratedGetMemberValue_targetHero(object o)
		{
			return ((ProdigalSonIssueQuest)o)._targetHero;
		}

		internal static object AutoGeneratedGetMemberValue_prodigalSon(object o)
		{
			return ((ProdigalSonIssueQuest)o)._prodigalSon;
		}

		internal static object AutoGeneratedGetMemberValue_playerTalkedToTargetHero(object o)
		{
			return ((ProdigalSonIssueQuest)o)._playerTalkedToTargetHero;
		}

		internal static object AutoGeneratedGetMemberValue_targetHouse(object o)
		{
			return ((ProdigalSonIssueQuest)o)._targetHouse;
		}

		internal static object AutoGeneratedGetMemberValue_questDifficulty(object o)
		{
			return ((ProdigalSonIssueQuest)o)._questDifficulty;
		}

		internal static object AutoGeneratedGetMemberValue_isHouseFightFinished(object o)
		{
			return ((ProdigalSonIssueQuest)o)._isHouseFightFinished;
		}

		internal static object AutoGeneratedGetMemberValue_playerTriedToPersuade(object o)
		{
			return ((ProdigalSonIssueQuest)o)._playerTriedToPersuade;
		}
	}

	private const IssueBase.IssueFrequency ProdigalSonIssueFrequency = IssueBase.IssueFrequency.Rare;

	private const int AgeLimitForSon = 35;

	private const int AgeLimitForIssueOwner = 30;

	private const int MinimumAgeDifference = 10;

	private float MaxDistanceForSettlementSelection => Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Default) * 2.18f;

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, CheckForIssue);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	public void CheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero, out var selectedHero, out var targetHero))
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnStartIssue, typeof(ProdigalSonIssue), IssueBase.IssueFrequency.Rare, new Tuple<Hero, Hero>(selectedHero, targetHero)));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(ProdigalSonIssue), IssueBase.IssueFrequency.Rare));
		}
	}

	private bool ConditionsHoldForSettlement(Settlement settlement, Hero issueGiver)
	{
		if (settlement.IsTown && settlement.MapFaction == issueGiver.MapFaction && settlement != issueGiver.CurrentSettlement && settlement.OwnerClan != issueGiver.Clan && settlement.OwnerClan != Clan.PlayerClan && settlement.HeroesWithoutParty.FirstOrDefault((Hero x) => x.CanHaveCampaignIssues() && x.IsGangLeader) != null)
		{
			return settlement.LocationComplex.GetListOfLocations().AnyQ((Location x) => x.CanBeReserved && !x.IsReserved);
		}
		return false;
	}

	private bool ConditionsHold(Hero issueGiver, out Hero selectedHero, out Hero targetHero)
	{
		selectedHero = null;
		targetHero = null;
		if (issueGiver.IsLord && !issueGiver.IsPrisoner && issueGiver.Clan != Clan.PlayerClan && issueGiver.Age > 30f && issueGiver.GetTraitLevel(DefaultTraits.Mercy) <= 0 && (issueGiver.CurrentSettlement != null || issueGiver.PartyBelongedTo != null))
		{
			selectedHero = issueGiver.Clan.AliveLords.GetRandomElementWithPredicate((Hero x) => x.IsActive && !x.IsFemale && x.Age < 35f && (int)x.Age + 10 <= (int)issueGiver.Age && !x.IsPrisoner && x.CanHaveCampaignIssues() && x.PartyBelongedTo == null && x.CurrentSettlement != null && x.GovernorOf == null && x.GetTraitLevel(DefaultTraits.Honor) + x.GetTraitLevel(DefaultTraits.Calculating) < 0);
			if (selectedHero != null)
			{
				targetHero = SettlementHelper.FindRandomSettlement((Settlement x) => ConditionsHoldForSettlement(x, issueGiver) && x.HeroesWithoutParty.FirstOrDefault((Hero y) => y.CanHaveCampaignIssues() && y.IsGangLeader && Campaign.Current.Models.MapDistanceModel.GetDistance(issueGiver.CurrentSettlement, x, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default) < MaxDistanceForSettlementSelection) != null)?.HeroesWithoutParty.FirstOrDefault((Hero y) => y.CanHaveCampaignIssues() && y.IsGangLeader);
			}
		}
		if (selectedHero != null)
		{
			return targetHero != null;
		}
		return false;
	}

	private IssueBase OnStartIssue(in PotentialIssueData pid, Hero issueOwner)
	{
		Tuple<Hero, Hero> tuple = pid.RelatedObject as Tuple<Hero, Hero>;
		return new ProdigalSonIssue(issueOwner, tuple.Item1, tuple.Item2);
	}
}
