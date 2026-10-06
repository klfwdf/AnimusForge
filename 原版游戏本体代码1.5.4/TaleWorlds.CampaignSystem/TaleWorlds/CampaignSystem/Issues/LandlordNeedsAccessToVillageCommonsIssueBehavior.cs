using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues;

public class LandlordNeedsAccessToVillageCommonsIssueBehavior : CampaignBehaviorBase
{
	public class LandlordNeedsAccessToVillageCommonsIssue : IssueBase
	{
		private const int AlternativeSolutionTroopTierRequirement = 2;

		private const int MinimumRequiredMenCount = 10;

		private const int NeededCompanionSkill = 150;

		private const int IssueDuration = 15;

		private const int QuestTimeLimit = 6;

		[SaveableField(100)]
		private Settlement _targetSettlement;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.Casualties | AlternativeSolutionScaleFlag.FailureRisk;

		public override int AlternativeSolutionBaseNeededMenCount => 3 + TaleWorlds.Library.MathF.Ceiling(4f + base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 2 + TaleWorlds.Library.MathF.Ceiling(4f + base.IssueDifficultyMultiplier);

		protected override int RewardGold => (int)(250f + 1000f * base.IssueDifficultyMultiplier);

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=6OKW8Ba3}{ISSUE_GIVER.LINK}, a landowner from {ISSUE_GIVER_SETTLEMENT}, has told you about {?ISSUE_GIVER.GENDER}her{?}his{\\?} problems with the herders of {TARGET_SETTLEMENT}. Apparently {?ISSUE_GIVER.GENDER}she{?}he{\\?} purchased the right to use a pasture near the village. But some local herders refuse to clear out and are causing problems for {?ISSUE_GIVER.GENDER}her{?}his{\\?} herdsmen. You have agreed to send {COMPANION.LINK} along with {NEEDED_MEN_COUNT} of your men to take care of the situation. You expect them to return in {RETURN_DAYS} days.");
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject);
				textObject.SetTextVariable("ISSUE_GIVER_SETTLEMENT", base.IssueOwner.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("NEEDED_MEN_COUNT", AlternativeSolutionSentTroops.TotalManCount - 1);
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=jYHKGhnc}Landlord Needs Access to the {TARGET_SETTLEMENT} Commons");
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.Name);
				return textObject;
			}
		}

		public override TextObject Description
		{
			get
			{
				TextObject textObject = new TextObject("{=XXac5fMa}A landowner needs your help in a dispute with herders from nearby {TARGET_SETTLEMENT}. They won't let {?ISSUE_GIVER.GENDER}her{?}his{\\?} herdsmen use pastures {?ISSUE_GIVER.GENDER}she{?}he{\\?} bought.");
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.Name);
				return textObject;
			}
		}

		public override TextObject IssueAsRumorInSettlement
		{
			get
			{
				TextObject textObject = new TextObject("{=nxyXKy2z}Old {QUEST_GIVER.NAME} has got some problems with those herders over in {TARGET_SETTLEMENT}.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.Name);
				return textObject;
			}
		}

		public override TextObject IssueBriefByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=DUB6zfSe}I have a bit of a dispute with {TARGET_SETTLEMENT}. I recently purchased the right to graze cattle in the nearby pastures from a landowner there. But now some of the herders are making problems.[if:convo_thinking][ib:closed]");
				if (base.IssueOwner.CharacterObject.GetPersona() == DefaultTraits.PersonaCurt)
				{
					textObject = new TextObject("{=0TyPBryV}I recently bought the right to graze cattle near the village of {TARGET_SETTLEMENT}. Good pastureland is hard to find. But now the locals are giving my herdsmen trouble.[if:convo_thinking][ib:closed]");
				}
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.Name);
				return textObject;
			}
		}

		public override TextObject IssueAcceptByPlayer => new TextObject("{=TXDjKUNf}What's the problem?");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject result = new TextObject("{=Je7SWXK3}They claim that I don't have the right to graze there, that village land can't be bought and sold like that. But look, I spent my silver. I won't get it back. Meanwhile, I can't afford to wait. I need someone to ride along with my herdsmen and my cattle can graze, one way or the other, even if it means violence. I can't let my herd just starve.[if:convo_bored][ib:hip]");
				if (base.IssueOwner.CharacterObject.GetPersona() == DefaultTraits.PersonaCurt)
				{
					result = new TextObject("{=5ehlbXm6}They don't want to share the pastures. But I spent my silver, and I hold the title deed. I need someone to ride along with my herdsmen and clear off anyone who gets in their way.[if:convo_stern][ib:hip]");
				}
				return result;
			}
		}

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=wodLHjnh}You or one of your companions with some {ALTERNATIVE_TROOP_AMOUNT} men should do the job. Either way I am willing to pay you {REWARD}{GOLD_ICON}. I doubt they'd stand up long to real warriors.[if:convo_mocking_teasing][ib:closed2]");
				textObject.SetTextVariable("REWARD", RewardGold);
				textObject.SetTextVariable("ALTERNATIVE_TROOP_AMOUNT", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		public override bool IsThereAlternativeSolution => true;

		public override bool IsThereLordSolution => false;

		public override TextObject IssueQuestSolutionAcceptByPlayer => new TextObject("{=GOkjJwZr}I can get your herdsmen to the pastures.");

		public override TextObject IssueAlternativeSolutionAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=QO0EX2O3}Sure. I can order one of my companions and {TROOP_AMOUNT} men to escort your herds to pasture in {TARGET_SETTLEMENT}.");
				textObject.SetTextVariable("TROOP_AMOUNT", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.Name);
				return textObject;
			}
		}

		public override TextObject IssueDiscussAlternativeSolution
		{
			get
			{
				TextObject textObject = new TextObject("{=BH17ZNSe}I don't think we'll have any more problems at {TARGET_SETTLEMENT}, thanks to your men. Please give them our thanks, {?PLAYER.GENDER}madam{?}sir{\\?}.[if:convo_mocking_teasing][ib:hip]");
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.Name);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=8INOZiew}Thank you, [if:convo_normal][ib:confident]both for looking out for my interests and upholding the law.");
				if (base.IssueOwner.CharacterObject.GetPersona() == DefaultTraits.PersonaCurt)
				{
					textObject = new TextObject("{=UsuOXc25}Thanks. [if:convo_stern][ib:closed]Show those troublemakers that the law is the law.");
				}
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
				return textObject;
			}
		}

		protected override int CompanionSkillRewardXP => (int)(700f + 900f * base.IssueDifficultyMultiplier);

		internal static void AutoGeneratedStaticCollectObjectsLandlordNeedsAccessToVillageCommonsIssue(object o, List<object> collectedObjects)
		{
			((LandlordNeedsAccessToVillageCommonsIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_targetSettlement);
		}

		internal static object AutoGeneratedGetMemberValue_targetSettlement(object o)
		{
			return ((LandlordNeedsAccessToVillageCommonsIssue)o)._targetSettlement;
		}

		public LandlordNeedsAccessToVillageCommonsIssue(Hero issueOwner)
			: base(issueOwner, CampaignTime.DaysFromNow(15f))
		{
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.SettlementSecurity)
			{
				return -1f;
			}
			if (issueEffect == DefaultIssueEffects.IssueOwnerPower)
			{
				return -0.1f;
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
				return (DefaultSkills.OneHanded, 150);
			}
			return ((skillValue2 >= skillValue3) ? DefaultSkills.TwoHanded : DefaultSkills.Polearm, 150);
		}

		public override bool AlternativeSolutionCondition(out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			ApplySuccessRewards();
		}

		protected override void AlternativeSolutionEndWithFailureConsequence()
		{
			ApplyFailRewards();
		}

		private void ApplySuccessRewards()
		{
			base.IssueOwner.AddPower(10f);
			ChangeRelationAction.ApplyPlayerRelation(base.IssueOwner, 5);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.IssueOwner, new Tuple<TraitObject, int>[2]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 30),
				new Tuple<TraitObject, int>(DefaultTraits.Mercy, -20)
			});
			foreach (Hero notable in _targetSettlement.Notables)
			{
				if (notable.IsHeadman)
				{
					ChangeRelationAction.ApplyPlayerRelation(notable, -3);
					notable.AddPower(-10f);
				}
			}
		}

		private void ApplyFailRewards()
		{
			base.IssueOwner.AddPower(-10f);
			ChangeRelationAction.ApplyPlayerRelation(base.IssueOwner, -5);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.IssueOwner, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -20)
			});
			foreach (Hero notable in _targetSettlement.Notables)
			{
				if (notable.IsHeadman)
				{
					ChangeRelationAction.ApplyPlayerRelation(notable, 3);
					notable.AddPower(10f);
				}
			}
		}

		public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
		{
			return character.Tier >= 2;
		}

		protected override void AfterIssueCreation()
		{
			_targetSettlement = base.IssueOwner.CurrentSettlement.Village.Bound.BoundVillages.FirstOrDefault((Village x) => x.Settlement != base.IssueOwner.CurrentSettlement && !x.Settlement.IsUnderRaid && x.Settlement.Notables.Any((Hero notable) => notable.IsHeadman))?.Settlement;
		}

		protected override void OnGameLoad()
		{
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new LandlordNeedsAccessToVillageCommonsIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(6f), _targetSettlement, RewardGold, base.IssueDifficultyMultiplier);
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
			if (base.IssueOwner.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				flag |= PreconditionFlags.AtWar;
			}
			if (MobileParty.MainParty.MemberRoster.TotalHealthyCount < 10)
			{
				flag |= PreconditionFlags.NotEnoughTroops;
			}
			return flag == PreconditionFlags.None;
		}

		public override bool IssueStayAliveConditions()
		{
			if (!base.IssueOwner.CurrentSettlement.IsRaided && !base.IssueOwner.CurrentSettlement.IsUnderRaid && base.IssueOwner.CurrentSettlement.Village.Bound.Town.Security <= 90f && _targetSettlement.Notables.Any((Hero x) => x.IsHeadman))
			{
				return !Clan.BanditFactions.IsEmpty();
			}
			return false;
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}
	}

	public class LandlordNeedsAccessToVillageCommonsIssueQuest : QuestBase
	{
		private const int BattleFakeSimulationDuration = 5;

		[SaveableField(11)]
		private MobileParty _rivalMobileParty;

		[SaveableField(30)]
		private readonly Settlement _targetSettlement;

		[SaveableField(21)]
		private MobileParty _herdersMobileParty;

		[SaveableField(31)]
		private float _issueDifficultyMultiplier;

		[SaveableField(50)]
		private readonly Hero _headmanNotable;

		[SaveableField(80)]
		private string _questId;

		[SaveableField(90)]
		private int _rewardGold;

		[SaveableField(100)]
		private int _rivalPartySpawnDeltaTime;

		[SaveableField(110)]
		private bool _battleStarted;

		[SaveableField(120)]
		private int _spawnRivalPartyAfterHours;

		private float PastureRadius => Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius * 2.5f;

		public sealed override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=oT8DUcHf}Landowner Needs {TARGET_SETTLEMENT}'s Pasture");
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.Name);
				return textObject;
			}
		}

		public override bool IsRemainingTimeHidden => false;

		private TextObject PlayerStartsQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=GyZHauax}{QUEST_GIVER.LINK}, a rural landowner, has told you about {?QUEST_GIVER.GENDER}her{?}his{\\?} problems with the local herders of {TARGET_SETTLEMENT}. Apparently {?QUEST_GIVER.GENDER}she{?}he{\\?} purchased the right to use a nearby pasture. But the local herders of {TARGET_SETTLEMENT} refuse to clear out and are causing problems for {?QUEST_GIVER.GENDER}her{?}his{\\?} herdsmen. You agreed to do the job yourself, and escort herders to pasture.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_GIVER_SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject SuccessWitHVillagerSurrenderQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=avMQKUoJ}You were able to drive the herders from the disputed pasture. The landowner {QUEST_GIVER.LINK}. {?QUEST_GIVER.GENDER}She{?}he{\\?} is grateful and sends {REWARD}{GOLD_ICON} with {?QUEST_GIVER.GENDER}her{?}his{\\?} regards.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				textObject.SetTextVariable("REWARD", _rewardGold);
				return textObject;
			}
		}

		private TextObject SuccessWitHWinningTheFightQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=YbBObxLH}You were able to drive the herders from the disputed pasture. The landowner, {QUEST_GIVER.LINK}, is grateful and sends {REWARD}{GOLD_ICON} with {?QUEST_GIVER.GENDER}her{?}his{\\?} regards.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_GIVER_SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				textObject.SetTextVariable("REWARD", _rewardGold);
				return textObject;
			}
		}

		private TextObject FailWithLosingTheFightQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=117W31dZ}You were unable to drive the herders from the disputed pasture. The landowner, {QUEST_GIVER.LINK}, probably feels that you let {?QUEST_GIVER.GENDER}her{?}him{\\?} down.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_GIVER_SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject FailWithCounterOfferLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=zJujFbXU}You decided that the herders had more right to the disputed pasture than the landowner, {QUEST_GIVER.LINK}. {?QUEST_GIVER.GENDER}She{?}He{\\?} probably feels betrayed.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_GIVER_SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject FailByTimeoutLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=ja74aAEI}You ignored the herders and did not solve the dispute. {?QUEST_GIVER.GENDER}She{?}he{\\?} probably feels betrayed.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject TargetVillageRaided
		{
			get
			{
				TextObject textObject = new TextObject("{=aN85Kfnq}{SETTLEMENT} was raided. Your agreement with {QUEST_GIVER.LINK} is canceled.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
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

		private TextObject WarDeclaredCancelLog => new TextObject("{=wQH1N109}War broke out between your clan and the quest giver's realm. Quest canceled.");

		internal static void AutoGeneratedStaticCollectObjectsLandlordNeedsAccessToVillageCommonsIssueQuest(object o, List<object> collectedObjects)
		{
			((LandlordNeedsAccessToVillageCommonsIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_rivalMobileParty);
			collectedObjects.Add(_targetSettlement);
			collectedObjects.Add(_herdersMobileParty);
			collectedObjects.Add(_headmanNotable);
		}

		internal static object AutoGeneratedGetMemberValue_rivalMobileParty(object o)
		{
			return ((LandlordNeedsAccessToVillageCommonsIssueQuest)o)._rivalMobileParty;
		}

		internal static object AutoGeneratedGetMemberValue_targetSettlement(object o)
		{
			return ((LandlordNeedsAccessToVillageCommonsIssueQuest)o)._targetSettlement;
		}

		internal static object AutoGeneratedGetMemberValue_herdersMobileParty(object o)
		{
			return ((LandlordNeedsAccessToVillageCommonsIssueQuest)o)._herdersMobileParty;
		}

		internal static object AutoGeneratedGetMemberValue_issueDifficultyMultiplier(object o)
		{
			return ((LandlordNeedsAccessToVillageCommonsIssueQuest)o)._issueDifficultyMultiplier;
		}

		internal static object AutoGeneratedGetMemberValue_headmanNotable(object o)
		{
			return ((LandlordNeedsAccessToVillageCommonsIssueQuest)o)._headmanNotable;
		}

		internal static object AutoGeneratedGetMemberValue_questId(object o)
		{
			return ((LandlordNeedsAccessToVillageCommonsIssueQuest)o)._questId;
		}

		internal static object AutoGeneratedGetMemberValue_rewardGold(object o)
		{
			return ((LandlordNeedsAccessToVillageCommonsIssueQuest)o)._rewardGold;
		}

		internal static object AutoGeneratedGetMemberValue_rivalPartySpawnDeltaTime(object o)
		{
			return ((LandlordNeedsAccessToVillageCommonsIssueQuest)o)._rivalPartySpawnDeltaTime;
		}

		internal static object AutoGeneratedGetMemberValue_battleStarted(object o)
		{
			return ((LandlordNeedsAccessToVillageCommonsIssueQuest)o)._battleStarted;
		}

		internal static object AutoGeneratedGetMemberValue_spawnRivalPartyAfterHours(object o)
		{
			return ((LandlordNeedsAccessToVillageCommonsIssueQuest)o)._spawnRivalPartyAfterHours;
		}

		public LandlordNeedsAccessToVillageCommonsIssueQuest(string questId, Hero questGiver, CampaignTime duration, Settlement targetSettlement, int rewardGold, float issueDifficultyMultiplier)
			: base(questId, questGiver, duration, rewardGold)
		{
			_targetSettlement = targetSettlement;
			_headmanNotable = targetSettlement.Notables.First((Hero x) => x.IsHeadman);
			_questId = questId;
			_rewardGold = rewardGold;
			_issueDifficultyMultiplier = issueDifficultyMultiplier;
			_rivalPartySpawnDeltaTime = 0;
			SetDialogs();
			InitializeQuestOnCreation();
		}

		protected override void SetDialogs()
		{
			Campaign.Current.ConversationManager.AddDialogFlow(GetRivalPartyDialogues(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetHerderPartyNearVillageDialogues(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetHerderPartyDialogues(), this);
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(new TextObject("{=qkYCWjTA}I appreciate it. I wait for the good news.")).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.Consequence(QuestAcceptedConsequences)
				.CloseDialog();
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(new TextObject("{=FogJnYH9}Any news about the pastures?[if:convo_undecided_open][ib:closed]")).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=wErSpkjy}I'm still working on it."))
				.NpcLine(new TextObject("{=xolucdbr}I am glad to hear that.[if:convo_undecided_open]"))
				.CloseDialog()
				.PlayerOption(new TextObject("{=7o68QryW}Not yet. I have some other business to attend to."))
				.NpcLine(new TextObject("{=bEab8stb}Okay. I'm waiting for your good news.[if:convo_undecided_open]"))
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
		}

		private DialogFlow GetRivalPartyDialogues()
		{
			TextObject npcText = new TextObject("{=Rt2w61N8}Don't get involved in this. [if:convo_confused_normal][ib:hip]We've grazed our herds on these hillsides since our fathers' fathers' time. We don't care if one rich bastard gave a couple of bags of silver to another rich bastard. We don't care about title deeds or courts of law or any of that. Custom is custom, and we're not going anywhere!");
			TextObject text = new TextObject("{=YPTZ2et7}Calm down. You're right. No one has the right to sell your ancestral lands. These herdsmen can take their cattle elsewhere.");
			TextObject text2 = new TextObject("{=R1W5Il2d}You can take your grievances to your lord, or to whoever sold the land. The law says a buyer has rights, and you need to clear out.");
			TextObject textObject = new TextObject("{=l3ALRD7c}You're just a rich bastard's lackey.[if:convo_confused_annoyed][ib:closed]");
			TextObject textObject2 = new TextObject("{=YLjksPbk}You're a kind {?PLAYER.GENDER}woman, [if:convo_happy]madam{?}man, sir{\\?}. You understand what poor folk like us are up against.");
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject2);
			TextObject text3 = new TextObject("{=ybb0ToHE}We will protect our lands![if:convo_thinking]");
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine(npcText).Condition(RivalPartyTalkOnCondition)
				.BeginPlayerOptions()
				.PlayerOption(text)
				.NpcLine(textObject2)
				.Consequence(FailWithAcceptingCounterOffer)
				.CloseDialog()
				.PlayerOption(text2)
				.EndPlayerOptions()
				.BeginNpcOptions()
				.NpcOption(textObject, RivalPartySurrenderOnCondition)
				.Consequence(SuccessWithVillagersSurrender)
				.CloseDialog()
				.NpcOption(text3, RivalPartyFightOnCondition)
				.Consequence(delegate
				{
					PlayerEncounter.LeaveEncounter = false;
				})
				.CloseDialog()
				.EndNpcOptions()
				.CloseDialog();
		}

		private DialogFlow GetHerderPartyDialogues()
		{
			TextObject textObject = new TextObject("{=He6DW2Xb}Thank you for protecting us {?PLAYER.GENDER}madam{?}sir{\\?}. Keep following, we are almost there.");
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject).Condition(HerdersTalkOnCondition)
				.Consequence(delegate
				{
					PlayerEncounter.LeaveEncounter = true;
				})
				.CloseDialog();
		}

		private bool RivalPartySurrenderOnCondition()
		{
			return MobileParty.MainParty.MemberRoster.TotalManCount - MobileParty.MainParty.MemberRoster.TotalWounded > 14;
		}

		private DialogFlow GetHerderPartyNearVillageDialogues()
		{
			TextObject textObject = new TextObject("{=crg2DrbZ}We are worried that the herders of {TARGET_SETTLEMENT} will harm our animals. Fortunately we have you on our side {?PLAYER.GENDER}madam{?}sir{\\?}.");
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
			textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.Name);
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject).Condition(() => HerdersTalkOnCondition() && DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(MobileParty.ConversationParty, _targetSettlement, MobileParty.NavigationType.Default) < PastureRadius + 2f)
				.Consequence(delegate
				{
					PlayerEncounter.LeaveEncounter = true;
				})
				.CloseDialog();
		}

		private bool RivalPartyTalkOnCondition()
		{
			if (base.IsOngoing && _rivalMobileParty != null && CharacterObject.OneToOneConversationCharacter != null && _rivalMobileParty.MemberRoster.Contains(CharacterObject.OneToOneConversationCharacter) && MobileParty.ConversationParty != null && !CharacterObject.OneToOneConversationCharacter.IsHero)
			{
				return MobileParty.ConversationParty.HomeSettlement == _targetSettlement;
			}
			return false;
		}

		private bool RivalPartyFightOnCondition()
		{
			return MobileParty.MainParty.MemberRoster.TotalManCount - MobileParty.MainParty.MemberRoster.TotalWounded <= 14;
		}

		private bool HerdersTalkOnCondition()
		{
			if (base.IsOngoing && _herdersMobileParty != null && CharacterObject.OneToOneConversationCharacter != null && _herdersMobileParty.MemberRoster.Contains(CharacterObject.OneToOneConversationCharacter) && MobileParty.ConversationParty != null && !CharacterObject.OneToOneConversationCharacter.IsHero && MobileParty.ConversationParty.Party.Owner != Hero.MainHero)
			{
				return MobileParty.ConversationParty.HomeSettlement == base.QuestGiver.CurrentSettlement;
			}
			return false;
		}

		private void FailWithAcceptingCounterOffer()
		{
			ChangeRelationAction.ApplyPlayerRelation(_headmanNotable, 3);
			TraitLevelingHelper.OnIssueSolvedThroughBetrayal(_headmanNotable, new Tuple<TraitObject, int>[2]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -30),
				new Tuple<TraitObject, int>(DefaultTraits.Mercy, 20)
			});
			CompleteQuestWithFail();
			AddLog(FailWithCounterOfferLogText);
			PlayerEncounter.LeaveEncounter = true;
		}

		private void SpawnRivalParty()
		{
			Clan clan = Clan.BanditFactions.FirstOrDefault((Clan x) => !x.Culture.CanHaveSettlement);
			if (clan == null)
			{
				Hideout hideout = SettlementHelper.FindNearestHideoutToSettlement(_targetSettlement, MobileParty.NavigationType.Default);
				if (hideout != null)
				{
					CultureObject banditCulture = hideout.Settlement.Culture;
					clan = Clan.BanditFactions.FirstOrDefault((Clan x) => x.Culture == banditCulture);
				}
				if (clan == null)
				{
					clan = Clan.BanditFactions.GetRandomElementInefficiently();
				}
			}
			_rivalMobileParty = BanditPartyComponent.CreateLooterParty("villagers_of_landlord_needs_access_to_village_common_quest" + _questId, clan, _targetSettlement, isBossParty: false, null, _targetSettlement.GatePosition);
			_rivalMobileParty.MemberRoster.AddToCounts(base.QuestGiver.Culture.Villager, TaleWorlds.Library.MathF.Ceiling(10f + 20f * _issueDifficultyMultiplier));
			TextObject textObject = new TextObject("{=QLLeHRWw}Herders of {QUEST_SETTLEMENT}");
			textObject.SetTextVariable("QUEST_SETTLEMENT", _targetSettlement.Name);
			_rivalMobileParty.InitializePartyTrade(200);
			_rivalMobileParty.SetPartyUsedByQuest(isActivelyUsed: true);
			_rivalMobileParty.Party.SetCustomName(textObject);
			_rivalMobileParty.IgnoreForHours(CampaignTime.HoursInDay * 30);
			_rivalMobileParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
			SetPartyAiAction.GetActionForEngagingParty(_rivalMobileParty, _herdersMobileParty, MobileParty.NavigationType.Default, isFromPort: false);
			_rivalMobileParty.TargetPosition = _herdersMobileParty.Position;
			_rivalMobileParty.Party.SetVisualAsDirty();
			AddTrackedObject(_rivalMobileParty);
			_rivalMobileParty.Aggressiveness = 0f;
		}

		private void SuccessWithVillagersSurrender()
		{
			CompleteQuestWithSuccess();
			AddLog(SuccessWitHVillagerSurrenderQuestLogText);
			PlayerEncounter.LeaveEncounter = true;
		}

		private void SpawnHerdersParty()
		{
			TextObject textObject = new TextObject("{=tLakpr0a}Herdsmen of {QUEST_GIVER}");
			textObject.SetTextVariable("QUEST_GIVER", base.QuestGiver.Name);
			_herdersMobileParty = CustomPartyComponent.CreateCustomPartyWithTroopRoster(base.QuestGiver.CurrentSettlement.GatePosition, 1f, base.QuestGiver.CurrentSettlement, textObject, null, TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), null);
			_herdersMobileParty.MemberRoster.AddToCounts(base.QuestGiver.Culture.Villager, TaleWorlds.Library.MathF.Ceiling(2f + 5f * _issueDifficultyMultiplier));
			_herdersMobileParty.InitializePartyTrade(200);
			_herdersMobileParty.SetPartyUsedByQuest(isActivelyUsed: true);
			_herdersMobileParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("sumpter_horse"), TaleWorlds.Library.MathF.Ceiling(2f + 5f * _issueDifficultyMultiplier));
			_herdersMobileParty.IgnoreForHours(CampaignTime.HoursInDay * 30);
			_herdersMobileParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
			_herdersMobileParty.Party.SetVisualAsDirty();
			AddTrackedObject(_herdersMobileParty);
			_herdersMobileParty.Aggressiveness = 0f;
			CampaignVec2 point = _targetSettlement.GatePosition;
			int[] invalidTerrainTypesForNavigationType = Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(MobileParty.NavigationType.Default);
			for (int i = 0; i < 15; i++)
			{
				point = NavigationHelper.FindReachablePointAroundPosition(_targetSettlement.GatePosition, MobileParty.NavigationType.Default, PastureRadius - 2f, PastureRadius - 3f);
				if (Campaign.Current.MapSceneWrapper.GetPathDistanceBetweenAIFaces(_targetSettlement.GatePosition.Face, point.Face, _targetSettlement.GatePosition.ToVec2(), point.ToVec2(), 0.3f, PastureRadius, out var _, invalidTerrainTypesForNavigationType, Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromLandToSea, Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromSeaToLand))
				{
					break;
				}
			}
			_herdersMobileParty.SetMoveGoToPoint(point, MobileParty.NavigationType.Default);
			Campaign.Current.MapSceneWrapper.GetPathDistanceBetweenAIFaces(_herdersMobileParty.CurrentNavigationFace, point.Face, _herdersMobileParty.GetPosition2D, point.ToVec2(), 0.3f, float.MaxValue, out var distance2, invalidTerrainTypesForNavigationType, Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromLandToSea, Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromSeaToLand);
			_spawnRivalPartyAfterHours = (int)(distance2 / _herdersMobileParty.Speed) + 3;
		}

		private void QuestAcceptedConsequences()
		{
			StartQuest();
			AddLog(PlayerStartsQuestLogText);
			AddTrackedObject(_targetSettlement);
			SpawnHerdersParty();
		}

		protected override void OnCompleteWithSuccess()
		{
			base.QuestGiver.AddPower(10f);
			RelationshipChangeWithQuestGiver = 5;
			_headmanNotable.AddPower(-10f);
			ChangeRelationAction.ApplyPlayerRelation(_headmanNotable, -3);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[2]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 30),
				new Tuple<TraitObject, int>(DefaultTraits.Mercy, -20)
			});
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, _rewardGold);
		}

		public override void OnFailed()
		{
			base.QuestGiver.AddPower(-10f);
			_headmanNotable.AddPower(10f);
			RelationshipChangeWithQuestGiver = -5;
			TraitLevelingHelper.OnIssueFailed(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -20)
			});
		}

		protected override void OnTimedOut()
		{
			base.QuestGiver.AddPower(-10f);
			RelationshipChangeWithQuestGiver = -5;
			TraitLevelingHelper.OnIssueFailed(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -20)
			});
			AddLog(FailByTimeoutLogText);
		}

		protected override void OnFinalize()
		{
			if (_herdersMobileParty != null && _herdersMobileParty.IsActive)
			{
				DestroyPartyAction.Apply(null, _herdersMobileParty);
				_herdersMobileParty = null;
			}
			if (_rivalMobileParty != null && _rivalMobileParty.IsActive)
			{
				DestroyPartyAction.Apply(null, _rivalMobileParty);
				_rivalMobileParty = null;
			}
		}

		private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			if (_rivalMobileParty != null && mapEvent.InvolvedParties.Contains(PartyBase.MainParty) && mapEvent.InvolvedParties.Contains(_rivalMobileParty.Party) && !mapEvent.InvolvedParties.Contains(_herdersMobileParty.Party))
			{
				_herdersMobileParty.Party.MapEventSide = PartyBase.MainParty.MapEventSide;
			}
			if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
			{
				QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
			}
		}

		private void OnMapEventEnded(MapEvent mapEvent)
		{
			if (_rivalMobileParty != null && ((mapEvent.InvolvedParties.Contains(PartyBase.MainParty) && mapEvent.InvolvedParties.Contains(_rivalMobileParty.Party)) || (mapEvent.InvolvedParties.Contains(PartyBase.MainParty) && mapEvent.InvolvedParties.Contains(_rivalMobileParty.Party) && mapEvent.InvolvedParties.Contains(_herdersMobileParty.Party))))
			{
				if (mapEvent.WinningSide == mapEvent.PlayerSide)
				{
					CompleteQuestWithSuccess();
					AddLog(SuccessWitHWinningTheFightQuestLogText);
				}
				else
				{
					CompleteQuestWithFail();
					AddLog(FailWithLosingTheFightQuestLogText);
				}
			}
		}

		protected override void HourlyTick()
		{
			if (!base.IsOngoing)
			{
				return;
			}
			CheckAnSpawnRivalParty();
			if (_rivalMobileParty != null && _rivalMobileParty.IsActive)
			{
				if (_herdersMobileParty?.MapEvent == null && _rivalMobileParty?.MapEvent == null && !_battleStarted && DistanceHelper.FindClosestDistanceFromMobilePartyToMobileParty(_rivalMobileParty, _herdersMobileParty, MobileParty.NavigationType.Default) < Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius / 6f)
				{
					EncounterManager.StartPartyEncounter(_rivalMobileParty.Party, _herdersMobileParty.Party);
					_rivalMobileParty.MapEvent.IsInvulnerable = true;
					_battleStarted = true;
				}
				if (_battleStarted && _rivalMobileParty.MapEvent.BattleStartTime.ElapsedHoursUntilNow > 5f)
				{
					_rivalMobileParty.MapEvent.FinalizeEvent();
					DestroyPartyAction.Apply(_rivalMobileParty.Party, _herdersMobileParty);
				}
			}
		}

		private void CheckAnSpawnRivalParty()
		{
			if (_rivalMobileParty == null && _rivalPartySpawnDeltaTime <= _spawnRivalPartyAfterHours)
			{
				_rivalPartySpawnDeltaTime++;
				if (_rivalPartySpawnDeltaTime > _spawnRivalPartyAfterHours)
				{
					SpawnRivalParty();
				}
			}
		}

		private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
		{
			if (base.IsOngoing)
			{
				if (mobileParty == _rivalMobileParty && (destroyerParty == PartyBase.MainParty || destroyerParty == _herdersMobileParty.Party))
				{
					_rivalMobileParty = null;
					AddLog(SuccessWitHWinningTheFightQuestLogText);
					CompleteQuestWithSuccess();
				}
				if (mobileParty == _herdersMobileParty)
				{
					_herdersMobileParty = null;
					AddLog(FailWithLosingTheFightQuestLogText);
					CompleteQuestWithFail();
				}
			}
		}

		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (base.QuestGiver.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				CompleteQuestWithCancel(WarDeclaredCancelLog);
			}
		}

		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
		{
			QuestHelper.CheckWarDeclarationAndFailOrCancelTheQuest(this, faction1, faction2, detail, PlayerDeclaredWarQuestLogText, WarDeclaredCancelLog);
		}

		protected override void InitializeQuestOnGameLoad()
		{
			SetDialogs();
		}

		private void OnVillageRaided(Village village)
		{
			if (village == _targetSettlement.Village || village == base.QuestGiver.CurrentSettlement.Village)
			{
				AddLog(TargetVillageRaided);
				CompleteQuestWithCancel();
			}
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
			CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
			CampaignEvents.VillageBeingRaided.AddNonSerializedListener(this, OnVillageRaided);
			CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
			CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
		}

		public override void OnHeroCanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
		{
			if (hero == _headmanNotable)
			{
				result = false;
			}
		}

		private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
		{
			if (victim == _headmanNotable)
			{
				TextObject textObject = ((detail == KillCharacterAction.KillCharacterActionDetail.Lost) ? TargetHeroDisappearedLogText : TargetHeroDiedLogText);
				StringHelpers.SetCharacterProperties("QUEST_TARGET", _headmanNotable.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				AddLog(textObject);
				CompleteQuestWithCancel();
			}
		}

		private void OnGameMenuOpened(MenuCallbackArgs args)
		{
			if (args.MenuContext.GameMenu.StringId == "join_encounter" && _battleStarted && PlayerEncounter.EncounteredBattle != null && PlayerEncounter.EncounteredBattle.InvolvedParties.Contains(_rivalMobileParty.Party))
			{
				_rivalMobileParty.MapEvent.IsInvulnerable = false;
			}
		}
	}

	public class LandlordNeedsAccessToVillageCommonsIssueTypeDefiner : SaveableTypeDefiner
	{
		public LandlordNeedsAccessToVillageCommonsIssueTypeDefiner()
			: base(420000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(LandlordNeedsAccessToVillageCommonsIssue), 1);
			AddClassDefinition(typeof(LandlordNeedsAccessToVillageCommonsIssueQuest), 2);
		}
	}

	private const IssueBase.IssueFrequency LandlordNeedsAccessToVillageCommonsIssueFrequency = IssueBase.IssueFrequency.Common;

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	public void OnCheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero))
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnSelected, typeof(LandlordNeedsAccessToVillageCommonsIssue), IssueBase.IssueFrequency.Common));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(LandlordNeedsAccessToVillageCommonsIssue), IssueBase.IssueFrequency.Common));
		}
	}

	private bool ConditionsHold(Hero issueGiver)
	{
		if (issueGiver.CurrentSettlement != null && issueGiver.CurrentSettlement.IsVillage && !issueGiver.CurrentSettlement.IsUnderRaid && issueGiver.CurrentSettlement.Village.VillageType == DefaultVillageTypes.WheatFarm && issueGiver.IsRuralNotable && !Clan.BanditFactions.IsEmpty() && issueGiver.GetTraitLevel(DefaultTraits.Mercy) <= 0 && issueGiver.GetTraitLevel(DefaultTraits.Generosity) <= 0 && issueGiver.CurrentSettlement.Village.Bound.Town.Security <= 70f)
		{
			return issueGiver.CurrentSettlement.Village.Bound.BoundVillages.Any((Village x) => x.Settlement != issueGiver.CurrentSettlement && !x.Settlement.IsUnderRaid && x.Settlement.Notables.Any((Hero notable) => notable.IsHeadman && notable.CanHaveCampaignIssues()));
		}
		return false;
	}

	private IssueBase OnSelected(in PotentialIssueData pid, Hero issueOwner)
	{
		return new LandlordNeedsAccessToVillageCommonsIssue(issueOwner);
	}
}
