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
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace SandBox.Issues;

public class NotableWantsDaughterFoundIssueBehavior : CampaignBehaviorBase
{
	public class NotableWantsDaughterFoundIssueTypeDefiner : SaveableTypeDefiner
	{
		public NotableWantsDaughterFoundIssueTypeDefiner()
			: base(1088000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(NotableWantsDaughterFoundIssue), 1);
			AddClassDefinition(typeof(NotableWantsDaughterFoundIssueQuest), 2);
		}
	}

	public class NotableWantsDaughterFoundIssue : IssueBase
	{
		private const int TroopTierForAlternativeSolution = 2;

		private const int RequiredSkillLevelForAlternativeSolution = 120;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.FailureRisk;

		public override bool IsThereAlternativeSolution => true;

		public override bool IsThereLordSolution => false;

		protected override int RewardGold => 500 + TaleWorlds.Library.MathF.Round(1200f * base.IssueDifficultyMultiplier);

		public override int AlternativeSolutionBaseNeededMenCount => 2 + TaleWorlds.Library.MathF.Ceiling(4f * base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 4 + TaleWorlds.Library.MathF.Ceiling(5f * base.IssueDifficultyMultiplier);

		protected override int CompanionSkillRewardXP => (int)(500f + 1000f * base.IssueDifficultyMultiplier);

		public override TextObject IssueBriefByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=x9VgLEzi}Yes... I've suffered a great misfortune. [ib:demure][if:convo_shocked]My daughter, a headstrong girl, has been bewitched by this never-do-well. I told her to stop seeing him but she wouldn't listen! Now she's missing - I'm sure she's been abducted by him! I'm offering a bounty of {BASE_REWARD_GOLD}{GOLD_ICON} to anyone who brings her back. Please {?PLAYER.GENDER}ma'am{?}sir{\\?}! Don't let a father's heart be broken.");
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				textObject.SetTextVariable("BASE_REWARD_GOLD", RewardGold);
				return textObject;
			}
		}

		public override TextObject IssueAcceptByPlayer => new TextObject("{=35w6g8gM}Tell me more. What's wrong with the man? ");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver => new TextObject("{=IY5b9vZV}Everything is wrong. [if:convo_annoyed]He is from a low family, the kind who is always involved in some land fraud scheme, or seen dealing with known bandits. Every village has a black sheep like that but I never imagined he would get his hooks into my daughter!");

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=v0XsM7Zz}If you send your best tracker with a few men, I am sure they will find my girl [if:convo_pondering]and be back to you in no more than {ALTERNATIVE_SOLUTION_WAIT_DAYS} days.");
				textObject.SetTextVariable("ALTERNATIVE_SOLUTION_WAIT_DAYS", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		public override TextObject IssuePlayerResponseAfterAlternativeExplanation => new TextObject("{=Ldp6ckgj}Don't worry, either I or one of my companions should be able to find her and see what's going on.");

		public override TextObject IssueQuestSolutionAcceptByPlayer => new TextObject("{=uYrxCtDa}I should be able to find her and see what's going on.");

		public override TextObject IssueAlternativeSolutionAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=WSrGHkal}I will have one of my trackers and {REQUIRED_TROOP_AMOUNT} of my men to find your daughter.");
				textObject.SetTextVariable("REQUIRED_TROOP_AMOUNT", GetTotalAlternativeSolutionNeededMenCount());
				return textObject;
			}
		}

		public override TextObject IssueDiscussAlternativeSolution => new TextObject("{=mBPcZddA}{?PLAYER.GENDER}Madam{?}Sir{\\?}, we are still waiting [ib:demure][if:convo_undecided_open]for your men to bring my daughter back. I pray for their success.");

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=Hhd3KaKu}Thank you, my {?PLAYER.GENDER}lady{?}lord{\\?}. If your men can find my girl and bring her back to me, I will be so grateful.[if:convo_happy] I will pay you {BASE_REWARD_GOLD}{GOLD_ICON} for your trouble.");
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				textObject.SetTextVariable("BASE_REWARD_GOLD", RewardGold);
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
				return textObject;
			}
		}

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=6OmbzoBs}{ISSUE_GIVER.LINK}, a merchant from {ISSUE_GIVER_SETTLEMENT}, has told you that {?ISSUE_GIVER.GENDER}her{?}his{\\?} daughter has gone missing. You choose {COMPANION.LINK} and {REQUIRED_TROOP_AMOUNT} men to search for her and bring her back. You expect them to complete this task and return in {ALTERNATIVE_SOLUTION_DAYS} days.");
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				textObject.SetTextVariable("BASE_REWARD_GOLD", RewardGold);
				textObject.SetTextVariable("ISSUE_GIVER_SETTLEMENT", base.IssueOwner.CurrentSettlement.Name);
				textObject.SetTextVariable("REQUIRED_TROOP_AMOUNT", AlternativeSolutionSentTroops.TotalManCount - 1);
				textObject.SetTextVariable("ALTERNATIVE_SOLUTION_DAYS", GetTotalAlternativeSolutionDurationInDays());
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=MaXA5HJi}Your companions report that the {ISSUE_GIVER.LINK}'s daughter returns to {?ISSUE_GIVER.GENDER}her{?}him{\\?} safe and sound. {?ISSUE_GIVER.GENDER}She{?}He{\\?} is happy and sends {?ISSUE_GIVER.GENDER}her{?}his{\\?} regards with a large pouch of {BASE_REWARD_GOLD}{GOLD_ICON}.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				textObject.SetTextVariable("BASE_REWARD_GOLD", RewardGold);
				return textObject;
			}
		}

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=kr68V5pm}{ISSUE_GIVER.NAME} Wants {?ISSUE_GIVER.GENDER}Her{?}His{\\?} Daughter Found");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject Description
		{
			get
			{
				TextObject textObject = new TextObject("{=SkzM5eSv}{ISSUE_GIVER.LINK}'s daughter is missing. {?ISSUE_GIVER.GENDER}She{?}He{\\?} is offering a substantial reward to find the young woman and bring her back safely.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject IssueAsRumorInSettlement
		{
			get
			{
				TextObject textObject = new TextObject("{=7RyXSkEE}Wouldn't want to be the poor lovesick sap who ran off with {QUEST_GIVER.NAME}'s daughter.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		public NotableWantsDaughterFoundIssue(Hero issueOwner)
			: base(issueOwner, CampaignTime.DaysFromNow(30f))
		{
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.IssueOwnerPower)
			{
				return -0.1f;
			}
			return 0f;
		}

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			ApplySuccessRewards();
			float randomFloat = MBRandom.RandomFloat;
			SkillObject skillObject = null;
			skillObject = ((randomFloat <= 0.33f) ? DefaultSkills.OneHanded : ((!(randomFloat <= 0.66f)) ? DefaultSkills.Polearm : DefaultSkills.TwoHanded));
			base.AlternativeSolutionHero.AddSkillXp(skillObject, (int)(500f + 1000f * base.IssueDifficultyMultiplier));
		}

		protected override void AlternativeSolutionEndWithFailureConsequence()
		{
			RelationshipChangeWithIssueOwner = -10;
			if (base.IssueOwner.CurrentSettlement.Village.Bound != null)
			{
				base.IssueOwner.CurrentSettlement.Village.Bound.Town.Prosperity -= 5f;
				base.IssueOwner.CurrentSettlement.Village.Bound.Town.Security -= 5f;
			}
		}

		private void ApplySuccessRewards()
		{
			GainRenownAction.Apply(Hero.MainHero, 2f);
			base.IssueOwner.AddPower(10f);
			RelationshipChangeWithIssueOwner = 10;
			if (base.IssueOwner.CurrentSettlement.Village.Bound != null)
			{
				base.IssueOwner.CurrentSettlement.Village.Bound.Town.Security += 10f;
			}
		}

		protected override void OnGameLoad()
		{
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new NotableWantsDaughterFoundIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(19f), RewardGold, base.IssueDifficultyMultiplier);
		}

		public override IssueFrequency GetFrequency()
		{
			return IssueFrequency.Rare;
		}

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			return ((hero.GetSkillValue(DefaultSkills.Charm) >= hero.GetSkillValue(DefaultSkills.Scouting)) ? DefaultSkills.Charm : DefaultSkills.Scouting, 120);
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

		protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out PreconditionFlags flag, out Hero relationHero, out SkillObject skill, out int requiredGold)
		{
			bool flag2 = issueGiver.GetRelationWithPlayer() >= -10f && !issueGiver.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction);
			flag = ((!flag2) ? ((!issueGiver.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction)) ? PreconditionFlags.Relation : PreconditionFlags.AtWar) : PreconditionFlags.None);
			relationHero = issueGiver;
			skill = null;
			requiredGold = 0;
			return flag2;
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

		internal static void AutoGeneratedStaticCollectObjectsNotableWantsDaughterFoundIssue(object o, List<object> collectedObjects)
		{
			((NotableWantsDaughterFoundIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		}
	}

	public class NotableWantsDaughterFoundIssueQuest : QuestBase
	{
		[SaveableField(10)]
		private readonly Hero _daughterHero;

		[SaveableField(20)]
		private readonly Hero _rogueHero;

		private Agent _daughterAgent;

		private Agent _rogueAgent;

		[SaveableField(50)]
		private bool _isQuestTargetMission;

		[SaveableField(60)]
		private bool _didPlayerBeatRouge;

		[SaveableField(70)]
		private bool _exitedQuestSettlementForTheFirstTime = true;

		[SaveableField(80)]
		private bool _isTrackerLogAdded;

		[SaveableField(90)]
		private bool _isDaughterPersuaded;

		[SaveableField(91)]
		private bool _isDaughterCaptured;

		[SaveableField(100)]
		private bool _acceptedDaughtersEscape;

		[SaveableField(110)]
		private readonly Village _targetVillage;

		[SaveableField(120)]
		private bool _villageIsRaidedTalkWithDaughter;

		[SaveableField(140)]
		private Dictionary<Village, bool> _villagesAndAlreadyVisitedBooleans = new Dictionary<Village, bool>();

		private Dictionary<string, CharacterObject> _rogueCharacterBasedOnCulture = new Dictionary<string, CharacterObject>();

		private bool _playerDefeatedByRogue;

		private PersuasionTask _task;

		private const PersuasionDifficulty Difficulty = PersuasionDifficulty.Hard;

		private const int MaxAgeForDaughterAndRogue = 25;

		[SaveableField(130)]
		private readonly float _questDifficultyMultiplier;

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=kr68V5pm}{ISSUE_GIVER.NAME} Wants {?ISSUE_GIVER.GENDER}Her{?}His{\\?} Daughter Found");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		public override bool IsRemainingTimeHidden => false;

		private bool DoesMainPartyHasEnoughScoutingSkill => (float)MobilePartyHelper.GetMainPartySkillCounsellor(DefaultSkills.Scouting).GetSkillValue(DefaultSkills.Scouting) >= 150f * _questDifficultyMultiplier;

		private TextObject PlayerStartsQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=1jExD58d}{QUEST_GIVER.LINK}, a merchant from {SETTLEMENT_NAME}, told you that {?QUEST_GIVER.GENDER}her{?}his{\\?} daughter {TARGET_HERO.NAME} has either been abducted or run off with a local rogue. You have agreed to search for her and bring her back to {SETTLEMENT_NAME}. If you cannot find their tracks when you exit settlement, you should visit the nearby villages of {SETTLEMENT_NAME} to look for clues and tracks of the kidnapper.");
				textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
				textObject.SetCharacterProperties("TARGET_HERO", _daughterHero.CharacterObject);
				textObject.SetTextVariable("SETTLEMENT_NAME", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("BASE_REWARD_GOLD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		private TextObject SuccessQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=asVE53ac}Daughter returns to {QUEST_GIVER.LINK}. {?QUEST_GIVER.GENDER}She{?}He{\\?} is happy. Sends {?QUEST_GIVER.GENDER}her{?}his{\\?} regards with a large pouch of {BASE_REWARD}{GOLD_ICON}.");
				textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				textObject.SetTextVariable("BASE_REWARD", RewardGold);
				return textObject;
			}
		}

		private TextObject PlayerDefeatedByRogueLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=i1sth9Ls}You were defeated by the rogue. He and {TARGET_HERO.NAME} ran off while you were unconscious. You failed to bring the daughter back to her {?QUEST_GIVER.GENDER}mother{?}father{\\?} as promised to {QUEST_GIVER.LINK}. {?QUEST_GIVER.GENDER}She{?}He{\\?} is furious.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("TARGET_HERO", _daughterHero.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject FailQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=ak2EMWWR}You failed to bring the daughter back to her {?QUEST_GIVER.GENDER}mother{?}father{\\?} as promised to {QUEST_GIVER.LINK}. {QUEST_GIVER.LINK} is furious");
				textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
				return textObject;
			}
		}

		private TextObject QuestCanceledWarDeclaredLog
		{
			get
			{
				TextObject textObject = new TextObject("{=vW6kBki9}Your clan is now at war with {QUEST_GIVER.LINK}'s realm. Your agreement with {QUEST_GIVER.LINK} is canceled.");
				textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
				return textObject;
			}
		}

		private TextObject PlayerDeclaredWarQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=bqeWVVEE}Your actions have started a war with {QUEST_GIVER.LINK}'s faction. {?QUEST_GIVER.GENDER}She{?}He{\\?} cancels your agreement and the quest is a failure.");
				textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
				return textObject;
			}
		}

		private TextObject VillageRaidedCancelQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=aN85Kfnq}{SETTLEMENT} was raided. Your agreement with {QUEST_GIVER.LINK} is canceled.");
				textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
				textObject.SetTextVariable("SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		public NotableWantsDaughterFoundIssueQuest(string questId, Hero questGiver, CampaignTime duration, int baseReward, float issueDifficultyMultiplier)
			: base(questId, questGiver, duration, baseReward)
		{
			_questDifficultyMultiplier = issueDifficultyMultiplier;
			_targetVillage = questGiver.CurrentSettlement.Village.Bound.BoundVillages.GetRandomElementWithPredicate((Village x) => x != questGiver.CurrentSettlement.Village);
			_rogueCharacterBasedOnCulture.Add("khuzait", Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "steppe_bandits")?.Culture.BanditBoss);
			_rogueCharacterBasedOnCulture.Add("vlandia", Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "mountain_bandits")?.Culture.BanditBoss);
			_rogueCharacterBasedOnCulture.Add("aserai", Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "desert_bandits")?.Culture.BanditBoss);
			_rogueCharacterBasedOnCulture.Add("battania", Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "forest_bandits")?.Culture.BanditBoss);
			_rogueCharacterBasedOnCulture.Add("sturgia", Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "sea_raiders")?.Culture.BanditBoss);
			_rogueCharacterBasedOnCulture.Add("empire_w", Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "mountain_bandits")?.Culture.BanditBoss);
			_rogueCharacterBasedOnCulture.Add("empire_s", Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "mountain_bandits")?.Culture.BanditBoss);
			_rogueCharacterBasedOnCulture.Add("empire", Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "mountain_bandits")?.Culture.BanditBoss);
			_rogueCharacterBasedOnCulture.Add("nord", Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "sea_raiders")?.Culture.BanditBoss);
			int heroComesOfAge = Campaign.Current.Models.AgeModel.HeroComesOfAge;
			int age = MBRandom.RandomInt(heroComesOfAge, 25);
			int age2 = MBRandom.RandomInt(heroComesOfAge, 25);
			CharacterObject randomCompanionTemplateWithPredicate = CharacterHelper.GetRandomCompanionTemplateWithPredicate((CharacterObject x) => x.IsFemale && x.Culture == questGiver.CurrentSettlement.Culture);
			_daughterHero = HeroCreator.CreateSpecialHero(randomCompanionTemplateWithPredicate, questGiver.HomeSettlement, questGiver.Clan, null, age);
			_daughterHero.HiddenInEncyclopedia = true;
			_daughterHero.Father = questGiver;
			_rogueHero = HeroCreator.CreateSpecialHero(GetRogueCharacterBasedOnCulture(questGiver.Culture.StringId), questGiver.HomeSettlement, questGiver.Clan, null, age2);
			_rogueHero.Culture = questGiver.Culture;
			_rogueHero.HiddenInEncyclopedia = true;
			SetDialogs();
			InitializeQuestOnCreation();
		}

		private CharacterObject GetRogueCharacterBasedOnCulture(string cultureStrId)
		{
			if (_rogueCharacterBasedOnCulture.ContainsKey(cultureStrId))
			{
				return _rogueCharacterBasedOnCulture[cultureStrId];
			}
			return base.QuestGiver.CurrentSettlement.Culture.NotableTemplates.GetRandomElementWithPredicate((CharacterObject x) => x.Occupation == Occupation.GangLeader && !x.IsFemale);
		}

		protected override void SetDialogs()
		{
			TextObject textObject = new TextObject("{=PZq1EMcx}Thank you for your help. I am still very worried about my girl {TARGET_HERO.FIRSTNAME}. Please find her and bring her back to me as soon as you can.[if:convo_worried]");
			StringHelpers.SetCharacterProperties("TARGET_HERO", _daughterHero.CharacterObject, textObject);
			TextObject npcText = new TextObject("{=sglD6abb}Please! Bring my daughter back.");
			TextObject npcText2 = new TextObject("{=ddEu5IFQ}I hope so.");
			TextObject npcText3 = new TextObject("{=IdKG3IaS}Good to hear that.");
			TextObject text = new TextObject("{=0hXofVLx}Don't worry I'll bring her.");
			TextObject text2 = new TextObject("{=zpqP5LsC}I'll go right away.");
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(textObject).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver && !_didPlayerBeatRouge)
				.Consequence(QuestAcceptedConsequences)
				.CloseDialog();
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(npcText).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver && !_didPlayerBeatRouge)
				.BeginPlayerOptions()
				.PlayerOption(text)
				.NpcLine(npcText2)
				.CloseDialog()
				.PlayerOption(text2)
				.NpcLine(npcText3)
				.CloseDialog();
			Campaign.Current.ConversationManager.AddDialogFlow(GetRougeDialogFlow(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetDaughterAfterFightDialog(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetDaughterAfterAcceptDialog(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetDaughterAfterPersuadedDialog(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetDaughterDialogWhenVillageRaid(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetRougeAfterAcceptDialog(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetRogueAfterPersuadedDialog(), this);
		}

		protected override void InitializeQuestOnGameLoad()
		{
			SetDialogs();
			if (_daughterHero != null)
			{
				_daughterHero.HiddenInEncyclopedia = true;
			}
			if (_rogueHero != null)
			{
				_rogueHero.HiddenInEncyclopedia = true;
			}
		}

		protected override void HourlyTick()
		{
		}

		private bool IsRougeHero(IAgent agent)
		{
			return agent.Character == _rogueHero.CharacterObject;
		}

		private bool IsDaughterHero(IAgent agent)
		{
			return agent.Character == _daughterHero.CharacterObject;
		}

		private bool IsMainHero(IAgent agent)
		{
			return agent.Character == CharacterObject.PlayerCharacter;
		}

		private bool multi_character_conversation_on_condition()
		{
			if (!_villageIsRaidedTalkWithDaughter && !_isDaughterPersuaded && !_didPlayerBeatRouge && !_acceptedDaughtersEscape && _isQuestTargetMission && (CharacterObject.OneToOneConversationCharacter == _daughterHero.CharacterObject || CharacterObject.OneToOneConversationCharacter == _rogueHero.CharacterObject))
			{
				MBList<Agent> agents = new MBList<Agent>();
				foreach (Agent nearbyAgent in Mission.Current.GetNearbyAgents(Agent.Main.Position.AsVec2, 100f, agents))
				{
					if (nearbyAgent.Character == _daughterHero.CharacterObject)
					{
						_daughterAgent = nearbyAgent;
						if (Mission.Current.GetMissionBehavior<MissionConversationLogic>() != null && Hero.OneToOneConversationHero != _daughterHero)
						{
							Campaign.Current.ConversationManager.AddConversationAgents(new List<Agent> { _daughterAgent }, setActionsInstantly: true);
						}
					}
					else if (nearbyAgent.Character == _rogueHero.CharacterObject)
					{
						_rogueAgent = nearbyAgent;
						if (Mission.Current.GetMissionBehavior<MissionConversationLogic>() != null && Hero.OneToOneConversationHero != _rogueHero)
						{
							Campaign.Current.ConversationManager.AddConversationAgents(new List<Agent> { _rogueAgent }, setActionsInstantly: true);
						}
					}
				}
				if (_daughterAgent != null && _rogueAgent != null && _daughterAgent.Health > 10f)
				{
					return _rogueAgent.Health > 10f;
				}
				return false;
			}
			return false;
		}

		private bool daughter_conversation_after_fight_on_condition()
		{
			if (CharacterObject.OneToOneConversationCharacter == _daughterHero.CharacterObject)
			{
				return _didPlayerBeatRouge;
			}
			return false;
		}

		private void multi_agent_conversation_on_consequence()
		{
			_task = GetPersuasionTask();
		}

		private DialogFlow GetRougeDialogFlow()
		{
			TextObject textObject = new TextObject("{=ovFbMMTJ}Who are you? Are you one of the bounty hunters sent by {QUEST_GIVER.LINK} to track us? Like we're animals or something? Look friend, we have done nothing wrong. As you may have figured out already, this woman and I, we love each other. I didn't force her to do anything.[ib:closed][if:convo_innocent_smile]");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
			TextObject textObject2 = new TextObject("{=D25oY3j1}Thank you {?PLAYER.GENDER}lady{?}sir{\\?}. For your kindness and understanding. We won't forget this.[ib:demure][if:convo_happy]");
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject2);
			TextObject textObject3 = new TextObject("{=oL3amiu1}Come {DAUGHTER_NAME.NAME}, let's go before other hounds sniff our trail... I mean... No offense {?PLAYER.GENDER}madam{?}sir{\\?}.");
			StringHelpers.SetCharacterProperties("DAUGHTER_NAME", _daughterHero.CharacterObject, textObject3);
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject3);
			TextObject textObject4 = new TextObject("{=92sbq1YY}I'm no child, {?PLAYER.GENDER}lady{?}sir{\\?}! Draw your weapon! I challenge you to a duel![ib:warrior2][if:convo_excited]");
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject4);
			TextObject textObject5 = new TextObject("{=jfzErupx}He is right! I ran away with him willingly. I love my {?QUEST_GIVER.GENDER}mother{?}father{\\?},[ib:closed][if:convo_grave] but {?QUEST_GIVER.GENDER}she{?}he{\\?} can be such a tyrant. Please {?PLAYER.GENDER}lady{?}sir{\\?}, if you believe in freedom and love, please leave us be.");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject5);
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject5);
			TextObject textObject6 = new TextObject("{=5NljlbLA}Thank you kind {?PLAYER.GENDER}lady{?}sir{\\?}, thank you.");
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject6);
			TextObject textObject7 = new TextObject("{=i5fNZrhh}Please, {?PLAYER.GENDER}lady{?}sir{\\?}. I love him truly and I wish to spend the rest of my life with him.[ib:demure][if:convo_worried] I beg of you, please don't stand in our way.");
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject7);
			TextObject textObject8 = new TextObject("{=0RCdPKj2}Yes {?QUEST_GIVER.GENDER}she{?}he{\\?} would probably be sad. But not because of what you think. See, {QUEST_GIVER.LINK} promised me to one of {?QUEST_GIVER.GENDER}her{?}his{\\?} allies' sons and this will devastate {?QUEST_GIVER.GENDER}her{?}his{\\?} plans. That is true.");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject8);
			TextObject text = new TextObject("{=5W7Kxfq9}I understand. If that is the case, I will let you go.");
			TextObject text2 = new TextObject("{=3XimdHOn}How do I know he's not forcing you to say that?");
			TextObject textObject9 = new TextObject("{=zNqDEuAw}But I've promised to find you and return you to your {?QUEST_GIVER.GENDER}mother{?}father{\\?}. {?QUEST_GIVER.GENDER}She{?}He{\\?} would be devastated.");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject9);
			TextObject textObject10 = new TextObject("{=tuaQ5uU3}I guess the only way to free you from this pretty boy's spell is to kill him.");
			TextObject textObject11 = new TextObject("{=HDCmeGhG}I'm sorry but I gave a promise. I don't break my promises.");
			TextObject text3 = new TextObject("{=VGrHWxzf}This will be a massacre, not a duel, but I'm fine with that.");
			TextObject text4 = new TextObject("{=sytYViXb}I accept your duel.");
			DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject, IsRougeHero, IsMainHero).Condition(multi_character_conversation_on_condition)
				.Consequence(multi_agent_conversation_on_consequence)
				.NpcLine(textObject5, IsDaughterHero, IsMainHero)
				.BeginPlayerOptions()
				.PlayerOption(text, IsDaughterHero)
				.NpcLine(textObject2, IsRougeHero, IsMainHero)
				.NpcLine(textObject3, IsRougeHero, IsDaughterHero)
				.NpcLine(textObject6, IsDaughterHero, IsMainHero)
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += PlayerAcceptedDaughtersEscape;
				})
				.CloseDialog()
				.PlayerOption(text2, IsDaughterHero)
				.NpcLine(textObject7, IsDaughterHero, IsMainHero)
				.PlayerLine(textObject9, IsDaughterHero)
				.NpcLine(textObject8, IsDaughterHero, IsMainHero)
				.GotoDialogState("start_daughter_persuade_to_come_persuasion")
				.GoBackToDialogState("daughter_persuade_to_come_persuasion_finished")
				.PlayerLine((Hero.MainHero.GetTraitLevel(DefaultTraits.Mercy) < 0) ? textObject10 : textObject11, IsDaughterHero)
				.NpcLine(textObject4, IsRougeHero, IsMainHero)
				.BeginPlayerOptions()
				.PlayerOption(text3, IsRougeHero)
				.NpcLine(new TextObject("{=XWVW0oTB}You bastard![ib:aggressive][if:convo_furious]"), IsRougeHero)
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += PlayerRejectsDuelFight;
				})
				.CloseDialog()
				.PlayerOption(text4, IsRougeHero)
				.NpcLine(new TextObject("{=jqahxjWD}Heaven protect me![ib:aggressive][if:convo_astonished]"), IsRougeHero)
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += PlayerAcceptsDuelFight;
				})
				.CloseDialog()
				.EndPlayerOptions()
				.EndPlayerOptions()
				.CloseDialog();
			AddPersuasionDialogs(dialogFlow);
			return dialogFlow;
		}

		private DialogFlow GetDaughterAfterFightDialog()
		{
			TextObject npcText = new TextObject("{=MN2v1AZQ}I hate you! You killed him! I can't believe it! I will hate you with all my heart till my dying days.[if:convo_angry]");
			TextObject npcText2 = new TextObject("{=TTkVcObg}What choice do I have, you heartless bastard?![if:convo_furious]");
			TextObject textObject = new TextObject("{=XqsrsjiL}I did what I had to do. Pack up, you need to go.");
			TextObject textObject2 = new TextObject("{=KQ3aYvp3}Some day you'll see I did you a favor. Pack up, you need to go.");
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine(npcText).Condition(daughter_conversation_after_fight_on_condition)
				.PlayerLine((Hero.MainHero.GetTraitLevel(DefaultTraits.Mercy) < 0) ? textObject : textObject2)
				.NpcLine(npcText2)
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += PlayerWonTheFight;
				})
				.CloseDialog();
		}

		private DialogFlow GetDaughterAfterAcceptDialog()
		{
			TextObject textObject = new TextObject("{=0Wg00sfN}Thank you, {?PLAYER.GENDER}madam{?}sir{\\?}. We will be moving immediately.");
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
			TextObject playerText = new TextObject("{=kUReBc04}Good.");
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject).Condition(daughter_conversation_after_accept_on_condition)
				.PlayerLine(playerText)
				.CloseDialog();
		}

		private bool daughter_conversation_after_accept_on_condition()
		{
			if (CharacterObject.OneToOneConversationCharacter == _daughterHero.CharacterObject)
			{
				return _acceptedDaughtersEscape;
			}
			return false;
		}

		private DialogFlow GetDaughterAfterPersuadedDialog()
		{
			TextObject textObject = new TextObject("{=B8bHpJRP}You are right, {?PLAYER.GENDER}my lady{?}sir{\\?}. I should be moving immediately.");
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
			TextObject playerText = new TextObject("{=kUReBc04}Good.");
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject).Condition(daughter_conversation_after_persuaded_on_condition)
				.PlayerLine(playerText)
				.CloseDialog();
		}

		private DialogFlow GetDaughterDialogWhenVillageRaid()
		{
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=w0HPC53e}Who are you? What do you want from me?[ib:nervous][if:convo_bared_teeth]")).Condition(() => _villageIsRaidedTalkWithDaughter)
				.PlayerLine(new TextObject("{=iRupMGI0}Calm down! Your father has sent me to find you."))
				.NpcLine(new TextObject("{=dwNquUNr}My father? Oh, thank god! I saw terrible things. [ib:nervous2][if:convo_shocked]They took my beloved one and slew many innocents without hesitation."))
				.PlayerLine("{=HtAr22re}Try to forget all about these and return to your father's house.")
				.NpcLine("{=FgSIsasF}Yes, you are right. I shall be on my way...")
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
					{
						ApplyDeliverySuccessConsequences();
						CompleteQuestWithSuccess();
						AddLog(SuccessQuestLogText);
						_villageIsRaidedTalkWithDaughter = false;
					};
				})
				.CloseDialog();
		}

		private bool daughter_conversation_after_persuaded_on_condition()
		{
			if (CharacterObject.OneToOneConversationCharacter == _daughterHero.CharacterObject)
			{
				return _isDaughterPersuaded;
			}
			return false;
		}

		private DialogFlow GetRougeAfterAcceptDialog()
		{
			TextObject textObject = new TextObject("{=wlKtDR2z}Thank you, {?PLAYER.GENDER}my lady{?}sir{\\?}.");
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject).Condition(rogue_conversation_after_accept_on_condition)
				.PlayerLine(new TextObject("{=0YJGvJ7o}You should leave now."))
				.NpcLine(new TextObject("{=6Q4cPOSG}Yes, we will."))
				.CloseDialog();
		}

		private bool rogue_conversation_after_accept_on_condition()
		{
			if (CharacterObject.OneToOneConversationCharacter == _rogueHero.CharacterObject)
			{
				return _acceptedDaughtersEscape;
			}
			return false;
		}

		private DialogFlow GetRogueAfterPersuadedDialog()
		{
			TextObject textObject = new TextObject("{=GFt9KiHP}You are right. Maybe we need to persuade {QUEST_GIVER.NAME}.");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
			TextObject playerText = new TextObject("{=btJkBTSF}I am sure you can solve it.");
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject).Condition(rogue_conversation_after_persuaded_on_condition)
				.PlayerLine(playerText)
				.CloseDialog();
		}

		private bool rogue_conversation_after_persuaded_on_condition()
		{
			if (CharacterObject.OneToOneConversationCharacter == _rogueHero.CharacterObject)
			{
				return _isDaughterPersuaded;
			}
			return false;
		}

		protected override void OnTimedOut()
		{
			ApplyDeliveryRejectedFailConsequences();
			TextObject textObject = new TextObject("{=KAvwytDK}You didn't bring {DAUGHTER.NAME} to {QUEST_GIVER.LINK}. {?QUEST_GIVER.GENDER}she{?}he{\\?} must be furious.");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
			StringHelpers.SetCharacterProperties("DAUGHTER", _daughterHero.CharacterObject, textObject);
			AddLog(textObject);
		}

		private void PlayerAcceptedDaughtersEscape()
		{
			_acceptedDaughtersEscape = true;
		}

		private void PlayerWonTheFight()
		{
			_isDaughterCaptured = true;
			Mission.Current.SetMissionMode(MissionMode.StartUp, atStart: false);
		}

		private void ApplyDaughtersEscapeAcceptedFailConsequences()
		{
			RelationshipChangeWithQuestGiver = -10;
			if (base.QuestGiver.CurrentSettlement.Village.Bound != null)
			{
				base.QuestGiver.CurrentSettlement.Village.Bound.Town.Security -= 5f;
				base.QuestGiver.CurrentSettlement.Village.Bound.Town.Prosperity -= 5f;
			}
		}

		private void ApplyDeliveryRejectedFailConsequences()
		{
			RelationshipChangeWithQuestGiver = -10;
			if (base.QuestGiver.CurrentSettlement.Village.Bound != null)
			{
				base.QuestGiver.CurrentSettlement.Village.Bound.Town.Security -= 5f;
				base.QuestGiver.CurrentSettlement.Village.Bound.Town.Prosperity -= 5f;
			}
		}

		private void ApplyDeliverySuccessConsequences()
		{
			GainRenownAction.Apply(Hero.MainHero, 2f);
			base.QuestGiver.AddPower(10f);
			RelationshipChangeWithQuestGiver = 10;
			if (base.QuestGiver.CurrentSettlement.Village.Bound != null)
			{
				base.QuestGiver.CurrentSettlement.Village.Bound.Town.Security += 10f;
			}
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
		}

		private void PlayerRejectsDuelFight()
		{
			_rogueAgent = (Agent)MissionConversationLogic.Current.ConversationManager.ConversationAgents.First((IAgent x) => !x.Character.IsFemale);
			List<Agent> list = new List<Agent> { Agent.Main };
			List<Agent> opponentSideAgents = new List<Agent> { _rogueAgent };
			MBList<Agent> agents = new MBList<Agent>();
			foreach (Agent nearbyAgent in Mission.Current.GetNearbyAgents(Agent.Main.Position.AsVec2, 30f, agents))
			{
				foreach (Hero item in Hero.MainHero.CompanionsInParty)
				{
					if (nearbyAgent.Character == item.CharacterObject)
					{
						list.Add(nearbyAgent);
						break;
					}
				}
			}
			_rogueAgent.Health = 150 + list.Count * 20;
			_rogueAgent.Defensiveness = 1f;
			Mission.Current.GetMissionBehavior<MissionFightHandler>().StartCustomFight(list, opponentSideAgents, dropWeapons: false, isItemUseDisabled: false, StartConversationAfterFight);
		}

		private void PlayerAcceptsDuelFight()
		{
			_rogueAgent = (Agent)MissionConversationLogic.Current.ConversationManager.ConversationAgents.First((IAgent x) => !x.Character.IsFemale);
			List<Agent> playerSideAgents = new List<Agent> { Agent.Main };
			List<Agent> opponentSideAgents = new List<Agent> { _rogueAgent };
			MBList<Agent> agents = new MBList<Agent>();
			foreach (Agent nearbyAgent in Mission.Current.GetNearbyAgents(Agent.Main.Position.AsVec2, 30f, agents))
			{
				foreach (Hero item in Hero.MainHero.CompanionsInParty)
				{
					if (nearbyAgent.Character == item.CharacterObject)
					{
						nearbyAgent.SetTeam(Mission.Current.SpectatorTeam, sync: false);
						DailyBehaviorGroup behaviorGroup = nearbyAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
						if (behaviorGroup.GetActiveBehavior() is FollowAgentBehavior)
						{
							behaviorGroup.GetBehavior<FollowAgentBehavior>().SetTargetAgent(null);
						}
						break;
					}
				}
			}
			_rogueAgent.Health = 200f;
			Mission.Current.GetMissionBehavior<MissionFightHandler>().StartCustomFight(playerSideAgents, opponentSideAgents, dropWeapons: false, isItemUseDisabled: false, StartConversationAfterFight);
		}

		private void StartConversationAfterFight(bool isPlayerSideWon)
		{
			if (isPlayerSideWon)
			{
				_didPlayerBeatRouge = true;
				Campaign.Current.ConversationManager.SetupAndStartMissionConversation(_daughterAgent, Mission.Current.MainAgent, setActionsInstantly: false);
				TraitLevelingHelper.OnHostileAction(-50);
			}
			else
			{
				_playerDefeatedByRogue = true;
			}
		}

		private void AddPersuasionDialogs(DialogFlow dialog)
		{
			TextObject textObject = new TextObject("{=ob5SejgJ}I will not abandon my love, {?PLAYER.GENDER}lady{?}sir{\\?}!");
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
			TextObject textObject2 = new TextObject("{=cqe8FU8M}{?QUEST_GIVER.GENDER}She{?}He{\\?} cares nothing about me! Only about {?QUEST_GIVER.GENDER}her{?}his{\\?} reputation in our district.");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject2);
			dialog.AddDialogLine("daughter_persuade_to_come_introduction", "start_daughter_persuade_to_come_persuasion", "daughter_persuade_to_come_start_reservation", textObject2.ToString(), null, persuasion_start_with_daughter_on_consequence, this, 100, null, IsDaughterHero, IsMainHero);
			dialog.AddDialogLine("daughter_persuade_to_come_rejected", "daughter_persuade_to_come_start_reservation", "daughter_persuade_to_come_persuasion_failed", "{=!}{FAILED_PERSUASION_LINE}", daughter_persuade_to_come_persuasion_failed_on_condition, daughter_persuade_to_come_persuasion_failed_on_consequence, this, 100, null, IsDaughterHero, IsMainHero);
			dialog.AddDialogLine("daughter_persuade_to_come_failed", "daughter_persuade_to_come_persuasion_failed", "daughter_persuade_to_come_persuasion_finished", textObject.ToString(), null, null, this);
			dialog.AddDialogLine("daughter_persuade_to_come_start", "daughter_persuade_to_come_start_reservation", "daughter_persuade_to_come_persuasion_select_option", "{=9b2BETct}I have already decided. Don't expect me to change my mind.", () => !daughter_persuade_to_come_persuasion_failed_on_condition(), null, this, 100, null, IsDaughterHero, IsMainHero);
			dialog.AddDialogLine("daughter_persuade_to_come_success", "daughter_persuade_to_come_start_reservation", "close_window", "{=3tmXBpRH}You're right. I cannot do this. I will return to my family. ", ConversationManager.GetPersuasionProgressSatisfied, daughter_persuade_to_come_persuasion_success_on_consequence, this, int.MaxValue, null, IsDaughterHero, IsMainHero);
			ConversationSentence.OnConditionDelegate conditionDelegate = persuasion_select_option_1_on_condition;
			ConversationSentence.OnConsequenceDelegate consequenceDelegate = persuasion_select_option_1_on_consequence;
			ConversationSentence.OnPersuasionOptionDelegate persuasionOptionDelegate = persuasion_setup_option_1;
			ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = persuasion_clickable_option_1_on_condition;
			dialog.AddPlayerLine("daughter_persuade_to_come_select_option_1", "daughter_persuade_to_come_persuasion_select_option", "daughter_persuade_to_come_persuasion_selected_option_response", "{=!}{DAUGHTER_PERSUADE_TO_COME_PERSUADE_ATTEMPT_1}", conditionDelegate, consequenceDelegate, this, 100, clickableConditionDelegate, persuasionOptionDelegate, IsMainHero, IsDaughterHero);
			ConversationSentence.OnConditionDelegate conditionDelegate2 = persuasion_select_option_2_on_condition;
			ConversationSentence.OnConsequenceDelegate consequenceDelegate2 = persuasion_select_option_2_on_consequence;
			persuasionOptionDelegate = persuasion_setup_option_2;
			clickableConditionDelegate = persuasion_clickable_option_2_on_condition;
			dialog.AddPlayerLine("daughter_persuade_to_come_select_option_2", "daughter_persuade_to_come_persuasion_select_option", "daughter_persuade_to_come_persuasion_selected_option_response", "{=!}{DAUGHTER_PERSUADE_TO_COME_PERSUADE_ATTEMPT_2}", conditionDelegate2, consequenceDelegate2, this, 100, clickableConditionDelegate, persuasionOptionDelegate, IsMainHero, IsDaughterHero);
			ConversationSentence.OnConditionDelegate conditionDelegate3 = persuasion_select_option_3_on_condition;
			ConversationSentence.OnConsequenceDelegate consequenceDelegate3 = persuasion_select_option_3_on_consequence;
			persuasionOptionDelegate = persuasion_setup_option_3;
			clickableConditionDelegate = persuasion_clickable_option_3_on_condition;
			dialog.AddPlayerLine("daughter_persuade_to_come_select_option_3", "daughter_persuade_to_come_persuasion_select_option", "daughter_persuade_to_come_persuasion_selected_option_response", "{=!}{DAUGHTER_PERSUADE_TO_COME_PERSUADE_ATTEMPT_3}", conditionDelegate3, consequenceDelegate3, this, 100, clickableConditionDelegate, persuasionOptionDelegate, IsMainHero, IsDaughterHero);
			ConversationSentence.OnConditionDelegate conditionDelegate4 = persuasion_select_option_4_on_condition;
			ConversationSentence.OnConsequenceDelegate consequenceDelegate4 = persuasion_select_option_4_on_consequence;
			persuasionOptionDelegate = persuasion_setup_option_4;
			clickableConditionDelegate = persuasion_clickable_option_4_on_condition;
			dialog.AddPlayerLine("daughter_persuade_to_come_select_option_4", "daughter_persuade_to_come_persuasion_select_option", "daughter_persuade_to_come_persuasion_selected_option_response", "{=!}{DAUGHTER_PERSUADE_TO_COME_PERSUADE_ATTEMPT_4}", conditionDelegate4, consequenceDelegate4, this, 100, clickableConditionDelegate, persuasionOptionDelegate, IsMainHero, IsDaughterHero);
			dialog.AddDialogLine("daughter_persuade_to_come_select_option_reaction", "daughter_persuade_to_come_persuasion_selected_option_response", "daughter_persuade_to_come_start_reservation", "{=D0xDRqvm}{PERSUASION_REACTION}", persuasion_selected_option_response_on_condition, persuasion_selected_option_response_on_consequence, this);
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
			return true;
		}

		private bool persuasion_select_option_1_on_condition()
		{
			if (_task.Options.Count > 0)
			{
				TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
				textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(_task.Options.ElementAt(0), showToPlayer: false));
				textObject.SetTextVariable("PERSUASION_OPTION_LINE", _task.Options.ElementAt(0).Line);
				MBTextManager.SetTextVariable("DAUGHTER_PERSUADE_TO_COME_PERSUADE_ATTEMPT_1", textObject);
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
				MBTextManager.SetTextVariable("DAUGHTER_PERSUADE_TO_COME_PERSUADE_ATTEMPT_2", textObject);
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
				MBTextManager.SetTextVariable("DAUGHTER_PERSUADE_TO_COME_PERSUADE_ATTEMPT_3", textObject);
				return true;
			}
			return false;
		}

		private bool persuasion_select_option_4_on_condition()
		{
			if (_task.Options.Count > 3)
			{
				TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
				textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(_task.Options.ElementAt(3), showToPlayer: false));
				textObject.SetTextVariable("PERSUASION_OPTION_LINE", _task.Options.ElementAt(3).Line);
				MBTextManager.SetTextVariable("DAUGHTER_PERSUADE_TO_COME_PERSUADE_ATTEMPT_4", textObject);
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

		private void persuasion_select_option_4_on_consequence()
		{
			if (_task.Options.Count > 3)
			{
				_task.Options[3].BlockTheOption(isBlocked: true);
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

		private PersuasionOptionArgs persuasion_setup_option_4()
		{
			return _task.Options.ElementAt(3);
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

		private bool persuasion_clickable_option_4_on_condition(out TextObject hintText)
		{
			hintText = new TextObject("{=9ACJsI6S}Blocked");
			if (_task.Options.Count > 3)
			{
				hintText = (_task.Options.ElementAt(3).IsBlocked ? hintText : null);
				return !_task.Options.ElementAt(3).IsBlocked;
			}
			return false;
		}

		private PersuasionTask GetPersuasionTask()
		{
			PersuasionTask persuasionTask = new PersuasionTask(0);
			persuasionTask.FinalFailLine = new TextObject("{=5aDlmdmb}No... No. It does not make sense.");
			persuasionTask.TryLaterLine = TextObject.GetEmpty();
			persuasionTask.SpokenLine = new TextObject("{=6P1ruzsC}Maybe...");
			PersuasionOptionArgs option = new PersuasionOptionArgs(DefaultSkills.Leadership, DefaultTraits.Honor, TraitEffect.Positive, PersuasionArgumentStrength.Hard, givesCriticalSuccess: true, new TextObject("{=Nhfl6tcM}Maybe, but that is your duty to your family."));
			persuasionTask.AddOptionToTask(option);
			TextObject textObject = new TextObject("{=lustkZ7s}Perhaps {?QUEST_GIVER.GENDER}she{?}he{\\?} made those plans because {?QUEST_GIVER.GENDER}she{?}he{\\?} loves you.");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
			PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Mercy, TraitEffect.Positive, PersuasionArgumentStrength.VeryEasy, givesCriticalSuccess: false, textObject);
			persuasionTask.AddOptionToTask(option2);
			PersuasionOptionArgs option3 = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.VeryHard, givesCriticalSuccess: false, new TextObject("{=Ns6Svjsn}Do you think this one will be faithful to you over many years? I know a rogue when I see one."));
			persuasionTask.AddOptionToTask(option3);
			PersuasionOptionArgs option4 = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Mercy, TraitEffect.Negative, PersuasionArgumentStrength.ExtremelyHard, givesCriticalSuccess: true, new TextObject("{=2dL6j8Hp}You want to marry a corpse? Because I'm going to kill your lover if you don't listen."), null, canBlockOtherOption: true);
			persuasionTask.AddOptionToTask(option4);
			return persuasionTask;
		}

		private void persuasion_start_with_daughter_on_consequence()
		{
			ConversationManager.StartPersuasion(2f, 1f, 0f, 2f, 2f, 0f, PersuasionDifficulty.Hard);
		}

		private void daughter_persuade_to_come_persuasion_success_on_consequence()
		{
			ConversationManager.EndPersuasion();
			_isDaughterPersuaded = true;
		}

		private bool daughter_persuade_to_come_persuasion_failed_on_condition()
		{
			if (_task.Options.All((PersuasionOptionArgs x) => x.IsBlocked) && !ConversationManager.GetPersuasionProgressSatisfied())
			{
				MBTextManager.SetTextVariable("FAILED_PERSUASION_LINE", _task.FinalFailLine);
				return true;
			}
			return false;
		}

		private void daughter_persuade_to_come_persuasion_failed_on_consequence()
		{
			ConversationManager.EndPersuasion();
		}

		private void OnSettlementLeft(MobileParty party, Settlement settlement)
		{
			if (party.IsMainParty && settlement == base.QuestGiver.CurrentSettlement && _exitedQuestSettlementForTheFirstTime)
			{
				if (DoesMainPartyHasEnoughScoutingSkill)
				{
					QuestHelper.AddMapArrowFromPointToTarget(new TextObject("{=YdwLnWa1}Direction of daughter and rogue"), settlement.Position, _targetVillage.Settlement.Position, 5f, 0.1f);
					MBInformationManager.AddQuickInformation(new TextObject("{=O15PyNUK}With the help of your scouting skill, you were able to trace their tracks."));
					MBInformationManager.AddQuickInformation(new TextObject("{=gOWebWiK}Their direction is marked with an arrow in the campaign map."));
					AddTrackedObject(_targetVillage.Settlement);
				}
				else
				{
					foreach (Village boundVillage in base.QuestGiver.CurrentSettlement.Village.Bound.BoundVillages)
					{
						if (boundVillage != base.QuestGiver.CurrentSettlement.Village)
						{
							_villagesAndAlreadyVisitedBooleans.Add(boundVillage, value: false);
							AddTrackedObject(boundVillage.Settlement);
						}
					}
				}
				TextObject textObject = new TextObject("{=FvtAJE2Q}In order to find {QUEST_GIVER.LINK}'s daughter, you have decided to visit nearby villages.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				AddLog(textObject, DoesMainPartyHasEnoughScoutingSkill);
				_exitedQuestSettlementForTheFirstTime = false;
			}
			if (party.IsMainParty && settlement == _targetVillage.Settlement)
			{
				_isQuestTargetMission = false;
			}
		}

		public void OnBeforeMissionOpened()
		{
			if (_isQuestTargetMission)
			{
				Location locationWithId = Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("village_center");
				if (locationWithId != null)
				{
					HandleRogueEquipment();
					locationWithId.AddCharacter(CreateQuestLocationCharacter(_daughterHero.CharacterObject, LocationCharacter.CharacterRelations.Neutral));
					locationWithId.AddCharacter(CreateQuestLocationCharacter(_rogueHero.CharacterObject, LocationCharacter.CharacterRelations.Neutral));
				}
			}
		}

		private void HandleRogueEquipment()
		{
			ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>("short_sword_t3");
			_rogueHero.CivilianEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, new EquipmentElement(item));
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
			{
				ItemObject item2 = _rogueHero.BattleEquipment[equipmentIndex].Item;
				if (item2 != null && item2.WeaponComponent.PrimaryWeapon.IsShield)
				{
					_rogueHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(equipmentIndex, default(EquipmentElement));
				}
			}
		}

		private void OnMissionEnded(IMission mission)
		{
			if (_isQuestTargetMission)
			{
				_daughterAgent = null;
				_rogueAgent = null;
				if (_isDaughterPersuaded)
				{
					ApplyDeliverySuccessConsequences();
					CompleteQuestWithSuccess();
					AddLog(SuccessQuestLogText);
					RemoveQuestCharacters();
				}
				else if (_acceptedDaughtersEscape)
				{
					ApplyDaughtersEscapeAcceptedFailConsequences();
					CompleteQuestWithFail(FailQuestLogText);
					RemoveQuestCharacters();
				}
				else if (_isDaughterCaptured)
				{
					ApplyDeliverySuccessConsequences();
					CompleteQuestWithSuccess();
					AddLog(SuccessQuestLogText);
					RemoveQuestCharacters();
				}
				else if (_playerDefeatedByRogue)
				{
					ApplyDeliveryFailedDueToDuelLostConsequences();
					CompleteQuestWithFail();
					AddLog(PlayerDefeatedByRogueLogText);
					RemoveQuestCharacters();
				}
			}
		}

		private void ApplyDeliveryFailedDueToDuelLostConsequences()
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, _daughterHero, -5);
			RelationshipChangeWithQuestGiver = -10;
			if (base.QuestGiver.CurrentSettlement.Village.Bound != null)
			{
				base.QuestGiver.CurrentSettlement.Village.Bound.Town.Security -= 5f;
				base.QuestGiver.CurrentSettlement.Village.Bound.Town.Prosperity -= 5f;
			}
		}

		private LocationCharacter CreateQuestLocationCharacter(CharacterObject character, LocationCharacter.CharacterRelations relation)
		{
			Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(character.Race, "_settlement");
			Tuple<string, Monster> tuple = new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, character.IsFemale, "_villager"), monsterWithSuffix);
			return new LocationCharacter(new AgentData(new SimpleAgentOrigin(character)).Monster(tuple.Item2), SandBoxManager.Instance.AgentBehaviorManager.AddCompanionBehaviors, "alley_2", fixedLocation: true, relation, tuple.Item1, useCivilianEquipment: false, isFixedCharacter: false, null, isHidden: false, isVisualTracked: true);
		}

		private void RemoveQuestCharacters()
		{
			Settlement.CurrentSettlement.LocationComplex.RemoveCharacterIfExists(_daughterHero);
			Settlement.CurrentSettlement.LocationComplex.RemoveCharacterIfExists(_rogueHero);
		}

		private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
		{
			if (party == null || !party.IsMainParty || !settlement.IsVillage)
			{
				return;
			}
			if (_villagesAndAlreadyVisitedBooleans.ContainsKey(settlement.Village) && !_villagesAndAlreadyVisitedBooleans[settlement.Village])
			{
				if (settlement.Village != _targetVillage)
				{
					if (settlement.IsSettlementBusy(this))
					{
						TextObject textObject = (settlement.IsRaided ? new TextObject("{=YTaM6G1E}It seems the village has been raided a short while ago. You found nothing but smoke, fire and crying people.") : new TextObject("{=2P3UJ8be}You ask around the village if anyone saw {TARGET_HERO.NAME} or some suspicious characters with a young woman.{newline}{newline}Villagers say that they saw a young man and woman ride in early in the morning. They bought some supplies and trotted off towards {TARGET_VILLAGE}."));
						textObject.SetTextVariable("TARGET_VILLAGE", _targetVillage.Name);
						StringHelpers.SetCharacterProperties("TARGET_HERO", _daughterHero.CharacterObject, textObject);
						InformationManager.ShowInquiry(new InquiryData(Title.ToString(), textObject.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, new TextObject("{=yS7PvrTD}OK").ToString(), "", null, null));
					}
					if (!_isTrackerLogAdded)
					{
						TextObject textObject2 = new TextObject("{=WGi3Zuv7}You asked the villagers around {CURRENT_SETTLEMENT} if they saw a young woman matching the description of {QUEST_GIVER.LINK}'s daughter, {TARGET_HERO.NAME}.{newline}{newline}They said a young woman and a young man dropped by early in the morning to buy some supplies and then rode off towards {TARGET_VILLAGE}.");
						textObject2.SetTextVariable("CURRENT_SETTLEMENT", Hero.MainHero.CurrentSettlement.Name);
						textObject2.SetTextVariable("TARGET_VILLAGE", _targetVillage.Settlement.EncyclopediaLinkWithName);
						StringHelpers.SetCharacterProperties("TARGET_HERO", _daughterHero.CharacterObject, textObject2);
						StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject2);
						AddLog(textObject2);
						_isTrackerLogAdded = true;
					}
				}
				else
				{
					InquiryData inquiryData = null;
					if (settlement.IsRaided)
					{
						TextObject textObject3 = new TextObject("{=edoXFdmg}You have found {QUEST_GIVER.NAME}'s daughter.");
						StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject3);
						TextObject textObject4 = new TextObject("{=aYMW8bWi}Talk to her");
						inquiryData = new InquiryData(Title.ToString(), textObject3.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, textObject4.ToString(), null, TalkWithDaughterAfterRaid, null);
					}
					else if (settlement.IsSettlementBusy(this))
					{
						TextObject textObject5 = new TextObject("{=aj4DZYyX}You ask around the village if anyone saw {TARGET_HERO.NAME} or some suspicious characters with a young woman. Villagers say that there was a young man and woman who arrived here exhausted. The villagers allowed them to stay for a while. You should search the village to find her.");
						StringHelpers.SetCharacterProperties("TARGET_HERO", _daughterHero.CharacterObject, textObject5);
						AddLog(textObject5);
					}
					else
					{
						TextObject textObject6 = new TextObject("{=bbwNIIKI}You ask around the village if anyone saw {TARGET_HERO.NAME} or some suspicious characters with a young woman.{newline}{newline}Villagers say that there was a young man and woman who arrived here exhausted. The villagers allowed them to stay for a while.{newline}You can check the area, and see if they are still hiding here.");
						StringHelpers.SetCharacterProperties("TARGET_HERO", _daughterHero.CharacterObject, textObject6);
						inquiryData = new InquiryData(Title.ToString(), textObject6.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, new TextObject("{=bb6e8DoM}Search the village").ToString(), new TextObject("{=3CpNUnVl}Cancel").ToString(), SearchTheVillage, null);
					}
					if (inquiryData != null)
					{
						InformationManager.ShowInquiry(inquiryData);
					}
				}
				_villagesAndAlreadyVisitedBooleans[settlement.Village] = true;
			}
			if (settlement == _targetVillage.Settlement)
			{
				if (!IsTracked(_daughterHero))
				{
					AddTrackedObject(_daughterHero);
				}
				if (!IsTracked(_rogueHero))
				{
					AddTrackedObject(_rogueHero);
				}
				_isQuestTargetMission = true;
			}
		}

		private void SearchTheVillage()
		{
			(PlayerEncounter.LocationEncounter as VillageEncounter)?.CreateAndOpenMissionController(LocationComplex.Current.GetLocationWithId("village_center"));
		}

		private void TalkWithDaughterAfterRaid()
		{
			_villageIsRaidedTalkWithDaughter = true;
			CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter), new ConversationCharacterData(_daughterHero.CharacterObject));
		}

		private void QuestAcceptedConsequences()
		{
			StartQuest();
			AddLog(PlayerStartsQuestLogText);
		}

		private void CanHeroDie(Hero victim, KillCharacterAction.KillCharacterActionDetail detail, ref bool result)
		{
			if (victim == Hero.MainHero && Settlement.CurrentSettlement == _targetVillage.Settlement && Mission.Current != null)
			{
				result = false;
			}
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
			CampaignEvents.BeforeMissionOpenedEvent.AddNonSerializedListener(this, OnBeforeMissionOpened);
			CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
			CampaignEvents.CanHeroDieEvent.AddNonSerializedListener(this, CanHeroDie);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
			CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, OnRaidCompleted);
		}

		private void OnRaidCompleted(BattleSideEnum side, RaidEventComponent raidEventComponent)
		{
			if (raidEventComponent.MapEventSettlement == base.QuestGiver.CurrentSettlement)
			{
				CompleteQuestWithCancel(VillageRaidedCancelQuestLogText);
			}
		}

		public override void OnHeroCanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
		{
			if (hero == _rogueHero || hero == _daughterHero)
			{
				result = false;
			}
		}

		public override void OnHeroCanMoveToSettlementInfoIsRequested(Hero hero, ref bool result)
		{
			if (hero == _rogueHero || hero == _daughterHero)
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
			if (IsTracked(_targetVillage.Settlement))
			{
				RemoveTrackedObject(_targetVillage.Settlement);
			}
			if (!Hero.MainHero.IsPrisoner && !DoesMainPartyHasEnoughScoutingSkill)
			{
				foreach (Village boundVillage in base.QuestGiver.CurrentSettlement.BoundVillages)
				{
					if (IsTracked(boundVillage.Settlement))
					{
						RemoveTrackedObject(boundVillage.Settlement);
					}
				}
			}
			if (_rogueHero != null && _rogueHero.IsAlive)
			{
				KillCharacterAction.ApplyByRemove(_rogueHero);
			}
			if (_daughterHero != null && _daughterHero.IsAlive)
			{
				KillCharacterAction.ApplyByRemove(_daughterHero);
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsNotableWantsDaughterFoundIssueQuest(object o, List<object> collectedObjects)
		{
			((NotableWantsDaughterFoundIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_daughterHero);
			collectedObjects.Add(_rogueHero);
			collectedObjects.Add(_targetVillage);
			collectedObjects.Add(_villagesAndAlreadyVisitedBooleans);
		}

		internal static object AutoGeneratedGetMemberValue_daughterHero(object o)
		{
			return ((NotableWantsDaughterFoundIssueQuest)o)._daughterHero;
		}

		internal static object AutoGeneratedGetMemberValue_rogueHero(object o)
		{
			return ((NotableWantsDaughterFoundIssueQuest)o)._rogueHero;
		}

		internal static object AutoGeneratedGetMemberValue_isQuestTargetMission(object o)
		{
			return ((NotableWantsDaughterFoundIssueQuest)o)._isQuestTargetMission;
		}

		internal static object AutoGeneratedGetMemberValue_didPlayerBeatRouge(object o)
		{
			return ((NotableWantsDaughterFoundIssueQuest)o)._didPlayerBeatRouge;
		}

		internal static object AutoGeneratedGetMemberValue_exitedQuestSettlementForTheFirstTime(object o)
		{
			return ((NotableWantsDaughterFoundIssueQuest)o)._exitedQuestSettlementForTheFirstTime;
		}

		internal static object AutoGeneratedGetMemberValue_isTrackerLogAdded(object o)
		{
			return ((NotableWantsDaughterFoundIssueQuest)o)._isTrackerLogAdded;
		}

		internal static object AutoGeneratedGetMemberValue_isDaughterPersuaded(object o)
		{
			return ((NotableWantsDaughterFoundIssueQuest)o)._isDaughterPersuaded;
		}

		internal static object AutoGeneratedGetMemberValue_isDaughterCaptured(object o)
		{
			return ((NotableWantsDaughterFoundIssueQuest)o)._isDaughterCaptured;
		}

		internal static object AutoGeneratedGetMemberValue_acceptedDaughtersEscape(object o)
		{
			return ((NotableWantsDaughterFoundIssueQuest)o)._acceptedDaughtersEscape;
		}

		internal static object AutoGeneratedGetMemberValue_targetVillage(object o)
		{
			return ((NotableWantsDaughterFoundIssueQuest)o)._targetVillage;
		}

		internal static object AutoGeneratedGetMemberValue_villageIsRaidedTalkWithDaughter(object o)
		{
			return ((NotableWantsDaughterFoundIssueQuest)o)._villageIsRaidedTalkWithDaughter;
		}

		internal static object AutoGeneratedGetMemberValue_villagesAndAlreadyVisitedBooleans(object o)
		{
			return ((NotableWantsDaughterFoundIssueQuest)o)._villagesAndAlreadyVisitedBooleans;
		}

		internal static object AutoGeneratedGetMemberValue_questDifficultyMultiplier(object o)
		{
			return ((NotableWantsDaughterFoundIssueQuest)o)._questDifficultyMultiplier;
		}
	}

	private const IssueBase.IssueFrequency NotableWantsDaughterFoundIssueFrequency = IssueBase.IssueFrequency.Rare;

	private const int IssueDuration = 30;

	private const int QuestTimeLimit = 19;

	private const int BaseRewardGold = 500;

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
	}

	private void OnGameLoadFinished()
	{
		if (!MBSaveLoad.IsUpdatingGameVersion || !MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.2.8.31599")))
		{
			return;
		}
		foreach (Hero deadOrDisabledHero in Hero.DeadOrDisabledHeroes)
		{
			if (deadOrDisabledHero.IsDead && deadOrDisabledHero.CompanionOf == Clan.PlayerClan && deadOrDisabledHero.Father != null && deadOrDisabledHero.Father.IsNotable && deadOrDisabledHero.Father.CurrentSettlement.IsVillage)
			{
				RemoveCompanionAction.ApplyByDeath(Clan.PlayerClan, deadOrDisabledHero);
			}
		}
	}

	public void OnCheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero))
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnStartIssue, typeof(NotableWantsDaughterFoundIssue), IssueBase.IssueFrequency.Rare));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(NotableWantsDaughterFoundIssue), IssueBase.IssueFrequency.Rare));
		}
	}

	private bool ConditionsHold(Hero issueGiver)
	{
		if (issueGiver.IsRuralNotable && issueGiver.CurrentSettlement.IsVillage && issueGiver.CurrentSettlement.Village.Bound != null && issueGiver.CurrentSettlement.Village.Bound.BoundVillages.Count > 2 && issueGiver.CanHaveCampaignIssues() && issueGiver.Age > (float)(Campaign.Current.Models.AgeModel.HeroComesOfAge * 2) && CharacterHelper.GetRandomCompanionTemplateWithPredicate((CharacterObject x) => x.IsFemale && x.Culture == issueGiver.CurrentSettlement.Culture) != null && issueGiver.CurrentSettlement.Culture.NotableTemplates.Any((CharacterObject x) => x.Occupation == Occupation.GangLeader && !x.IsFemale) && issueGiver.GetTraitLevel(DefaultTraits.Mercy) <= 0)
		{
			return issueGiver.GetTraitLevel(DefaultTraits.Generosity) <= 0;
		}
		return false;
	}

	private IssueBase OnStartIssue(in PotentialIssueData pid, Hero issueOwner)
	{
		return new NotableWantsDaughterFoundIssue(issueOwner);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
