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
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues;

public class MerchantArmyOfPoachersIssueBehavior : CampaignBehaviorBase
{
	public class MerchantArmyOfPoachersIssue : IssueBase
	{
		private const int AlternativeSolutionTroopTierRequirement = 2;

		private const int CompanionRequiredSkillLevel = 150;

		private const int MinimumRequiredMenCount = 15;

		private const int IssueDuration = 15;

		private const int QuestTimeLimit = 20;

		[SaveableField(10)]
		private Village _questVillage;

		public override int AlternativeSolutionBaseNeededMenCount => 12 + TaleWorlds.Library.MathF.Ceiling(28f * base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 3 + TaleWorlds.Library.MathF.Ceiling(5f * base.IssueDifficultyMultiplier);

		protected override int RewardGold => (int)(500f + 3000f * base.IssueDifficultyMultiplier);

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.Casualties | AlternativeSolutionScaleFlag.FailureRisk;

		public override TextObject IssueBriefByIssueGiver => new TextObject("{=Jk3mDlU6}Yeah... I've got some problems. A few years ago, I needed hides for my tannery and I hired some hunters. I didn't ask too many questions about where they came by the skins they sold me. Well, that was a bit of mistake. Now they've banded together as a gang and are trying to muscle me out of the leather business.[ib:closed2][if:convo_thinking]");

		public override TextObject IssueAcceptByPlayer => new TextObject("{=apuNQC2W}What can I do for you?");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=LbTETjZu}I want you to crush them. Go to {VILLAGE} and give them a lesson they won't forget.[ib:closed2][if:convo_grave]");
				textObject.SetTextVariable("VILLAGE", _questVillage.Settlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=2ELhox6C}If you don't want to get involved in this yourself, leave one of your capable companions and {NUMBER_OF_TROOPS} men for some days.[ib:closed][if:convo_grave]");
				textObject.SetTextVariable("NUMBER_OF_TROOPS", GetTotalAlternativeSolutionNeededMenCount());
				return textObject;
			}
		}

		public override TextObject IssueQuestSolutionAcceptByPlayer => new TextObject("{=b6naGx6H}I'll rid you of those poachers myself.");

		public override TextObject IssueAlternativeSolutionAcceptByPlayer => new TextObject("{=lA14Ubal}I can send a companion to hunt these poachers.");

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver => new TextObject("{=Xmtlrrmf}Thank you.[ib:normal][if:convo_normal]  Don't forget to warn your men. These poachers are not ordinary bandits. Good luck.");

		public override TextObject IssueDiscussAlternativeSolution => new TextObject("{=51ahPi69}I understand that your men are still chasing those poachers. I realize that this mess might take a little time to clean up.[ib:normal2][if:convo_grave]");

		public override bool IsThereAlternativeSolution => true;

		public override bool IsThereLordSolution => false;

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=428B377z}{ISSUE_GIVER.LINK}, a merchant of {QUEST_GIVER_SETTLEMENT}, told you that the poachers {?ISSUE_GIVER.GENDER}she{?}he{\\?} hired are now out of control. You asked {COMPANION.LINK} to take {NEEDED_MEN_COUNT} of your men to go to {QUEST_VILLAGE} and kill the poachers. They should rejoin your party in {RETURN_DAYS} days.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_GIVER_SETTLEMENT", base.IssueOwner.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("NEEDED_MEN_COUNT", AlternativeSolutionSentTroops.TotalManCount - 1);
				textObject.SetTextVariable("QUEST_VILLAGE", _questVillage.Settlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		public override TextObject Title => new TextObject("{=iHFo2kjz}Army of Poachers");

		public override TextObject Description
		{
			get
			{
				TextObject result = new TextObject("{=NCC4VUOc}{ISSUE_GIVER.LINK} wants you to get rid of the poachers who once worked for {?ISSUE_GIVER.GENDER}her{?}him{\\?} but are now out of control.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject);
				return result;
			}
		}

		protected override int CompanionSkillRewardXP => (int)(800f + 1000f * base.IssueDifficultyMultiplier);

		internal static void AutoGeneratedStaticCollectObjectsMerchantArmyOfPoachersIssue(object o, List<object> collectedObjects)
		{
			((MerchantArmyOfPoachersIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_questVillage);
		}

		internal static object AutoGeneratedGetMemberValue_questVillage(object o)
		{
			return ((MerchantArmyOfPoachersIssue)o)._questVillage;
		}

		public MerchantArmyOfPoachersIssue(Hero issueOwner, Village questVillage)
			: base(issueOwner, CampaignTime.DaysFromNow(15f))
		{
			_questVillage = questVillage;
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.SettlementProsperity)
			{
				return 0.2f;
			}
			if (issueEffect == DefaultIssueEffects.SettlementSecurity)
			{
				return -1f;
			}
			if (issueEffect == DefaultIssueEffects.SettlementLoyalty)
			{
				return -0.2f;
			}
			if (issueEffect == DefaultIssueEffects.IssueOwnerPower)
			{
				return -0.2f;
			}
			return 0f;
		}

		public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
		{
			return character.Tier >= 2;
		}

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			int skillValue = hero.GetSkillValue(DefaultSkills.Bow);
			int skillValue2 = hero.GetSkillValue(DefaultSkills.Crossbow);
			int skillValue3 = hero.GetSkillValue(DefaultSkills.Throwing);
			if (skillValue >= skillValue2 && skillValue >= skillValue3)
			{
				return (DefaultSkills.Bow, 150);
			}
			return ((skillValue2 >= skillValue3) ? DefaultSkills.Crossbow : DefaultSkills.Throwing, 150);
		}

		public override bool AlternativeSolutionCondition(out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		public override IssueFrequency GetFrequency()
		{
			return IssueFrequency.Common;
		}

		public override bool IssueStayAliveConditions()
		{
			if (!_questVillage.Settlement.IsUnderRaid && !_questVillage.Settlement.IsRaided)
			{
				return base.IssueOwner.CurrentSettlement.Town.Security <= 90f;
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
			if (MobileParty.MainParty.MemberRoster.TotalHealthyCount < 15)
			{
				flag |= PreconditionFlags.NotEnoughTroops;
			}
			if (issueGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				flag |= PreconditionFlags.AtWar;
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
			return new MerchantArmyOfPoachersIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(20f), _questVillage, base.IssueDifficultyMultiplier, RewardGold);
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			RelationshipChangeWithIssueOwner = 5;
			base.IssueOwner.AddPower(30f);
			base.IssueOwner.CurrentSettlement.Town.Prosperity += 50f;
		}

		protected override void AlternativeSolutionEndWithFailureConsequence()
		{
			RelationshipChangeWithIssueOwner = -5;
			base.IssueOwner.AddPower(-50f);
			base.IssueOwner.CurrentSettlement.Town.Prosperity -= 30f;
			base.IssueOwner.CurrentSettlement.Town.Security -= 5f;
			TraitLevelingHelper.OnIssueFailed(base.IssueOwner, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -30)
			});
		}
	}

	public class MerchantArmyOfPoachersIssueQuest : QuestBase
	{
		[SaveableField(10)]
		internal MobileParty _poachersParty;

		[SaveableField(20)]
		internal Village _questVillage;

		[SaveableField(30)]
		internal bool _talkedToPoachersBattleWillStart;

		[SaveableField(40)]
		internal bool _isReadyToBeFinalized;

		[SaveableField(50)]
		internal bool _persuasionTriedOnce;

		[SaveableField(60)]
		internal float _difficultyMultiplier;

		[SaveableField(70)]
		internal int _rewardGold;

		private PersuasionTask _task;

		private const PersuasionDifficulty Difficulty = PersuasionDifficulty.MediumHard;

		public override TextObject Title => new TextObject("{=iHFo2kjz}Army of Poachers");

		public override bool IsRemainingTimeHidden => false;

		private TextObject QuestStartedLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=fk4ewfQh}{QUEST_GIVER.LINK}, a merchant of {SETTLEMENT}, told you that the poachers {?QUEST_GIVER.GENDER}she{?}he{\\?} hired before are now out of control. {?QUEST_GIVER.GENDER}She{?}He{\\?} asked you to go to {VILLAGE} around midnight and kill the poachers.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("VILLAGE", _questVillage.Settlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject QuestCanceledTargetVillageRaidedQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=etYq1Tky}{VILLAGE} was raided and the poachers scattered.");
				textObject.SetTextVariable("VILLAGE", _questVillage.Settlement.EncyclopediaLinkWithName);
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

		private TextObject QuestFailedAfterTalkingWithProachers
		{
			get
			{
				TextObject textObject = new TextObject("{=PIukmFYA}You decided not to get involved and left the village. You have failed to help {QUEST_GIVER.LINK} as promised.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject QuestSuccessPlayerComesToAnAgreementWithPoachersQuestLogText => new TextObject("{=qPfJpwGa}You have persuaded the poachers to leave the district.");

		private TextObject QuestFailWithPlayerDefeatedAgainstPoachersQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=p8Kfl5u6}You lost the battle against the poachers and failed to help {QUEST_GIVER.LINK} as promised.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject QuestSuccessWithPlayerDefeatedPoachersQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=8gNqLqFl}You have defeated the poachers and helped {QUEST_GIVER.LINK} as promised.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject QuestFailedWithTimeOutLogText => new TextObject("{=HX7E09XJ}You failed to complete the quest in time.");

		internal static void AutoGeneratedStaticCollectObjectsMerchantArmyOfPoachersIssueQuest(object o, List<object> collectedObjects)
		{
			((MerchantArmyOfPoachersIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_poachersParty);
			collectedObjects.Add(_questVillage);
		}

		internal static object AutoGeneratedGetMemberValue_poachersParty(object o)
		{
			return ((MerchantArmyOfPoachersIssueQuest)o)._poachersParty;
		}

		internal static object AutoGeneratedGetMemberValue_questVillage(object o)
		{
			return ((MerchantArmyOfPoachersIssueQuest)o)._questVillage;
		}

		internal static object AutoGeneratedGetMemberValue_talkedToPoachersBattleWillStart(object o)
		{
			return ((MerchantArmyOfPoachersIssueQuest)o)._talkedToPoachersBattleWillStart;
		}

		internal static object AutoGeneratedGetMemberValue_isReadyToBeFinalized(object o)
		{
			return ((MerchantArmyOfPoachersIssueQuest)o)._isReadyToBeFinalized;
		}

		internal static object AutoGeneratedGetMemberValue_persuasionTriedOnce(object o)
		{
			return ((MerchantArmyOfPoachersIssueQuest)o)._persuasionTriedOnce;
		}

		internal static object AutoGeneratedGetMemberValue_difficultyMultiplier(object o)
		{
			return ((MerchantArmyOfPoachersIssueQuest)o)._difficultyMultiplier;
		}

		internal static object AutoGeneratedGetMemberValue_rewardGold(object o)
		{
			return ((MerchantArmyOfPoachersIssueQuest)o)._rewardGold;
		}

		public MerchantArmyOfPoachersIssueQuest(string questId, Hero giverHero, CampaignTime duration, Village questVillage, float difficultyMultiplier, int rewardGold)
			: base(questId, giverHero, duration, rewardGold)
		{
			_questVillage = questVillage;
			_talkedToPoachersBattleWillStart = false;
			_isReadyToBeFinalized = false;
			_difficultyMultiplier = difficultyMultiplier;
			_rewardGold = rewardGold;
			SetDialogs();
			InitializeQuestOnCreation();
		}

		private bool SetStartDialogOnCondition()
		{
			if (_poachersParty != null && CharacterObject.OneToOneConversationCharacter == ConversationHelper.GetConversationCharacterPartyLeader(_poachersParty.Party))
			{
				MBTextManager.SetTextVariable("POACHER_PARTY_START_LINE", "{=j9MBwnWI}Well...Are you working for that merchant in the town ? So it's all fine when the rich folk trade in poached skins, but if we do it, armed men come to hunt us down.");
				if (_persuasionTriedOnce)
				{
					MBTextManager.SetTextVariable("POACHER_PARTY_START_LINE", "{=Nn06TSq9}Anything else to say?");
				}
				return true;
			}
			return false;
		}

		private DialogFlow GetPoacherPartyDialogFlow()
		{
			DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 125).NpcLine("{=!}{POACHER_PARTY_START_LINE}").Condition(() => SetStartDialogOnCondition())
				.Consequence(delegate
				{
					_task = GetPersuasionTask();
				})
				.BeginPlayerOptions()
				.PlayerOption("{=afbLOXbb}Maybe we can come to an agreement.")
				.Condition(() => !_persuasionTriedOnce)
				.Consequence(delegate
				{
					_persuasionTriedOnce = true;
				})
				.GotoDialogState("start_poachers_persuasion")
				.PlayerOption("{=mvw1ayGt}I'm here to do the job I agreed to do, outlaw. Give up or die.")
				.NpcLine("{=hOVr77fd}You will never see the sunrise again![ib:warrior][if:convo_furious]")
				.Consequence(delegate
				{
					_talkedToPoachersBattleWillStart = true;
				})
				.CloseDialog()
				.PlayerOption("{=VJYEoOAc}Well... You have a point. Go on. We won't bother you any more.")
				.NpcLine("{=wglTyBbx}Thank you, friend. Go in peace.[ib:normal][if:convo_approving]")
				.Consequence(delegate
				{
					Campaign.Current.GameMenuManager.SetNextMenu("village");
					Campaign.Current.ConversationManager.ConversationEndOneShot += QuestFailedAfterTalkingWithPoachers;
				})
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
			AddPersuasionDialogs(dialogFlow);
			return dialogFlow;
		}

		private void AddPersuasionDialogs(DialogFlow dialog)
		{
			dialog.AddDialogLine("poachers_persuasion_check_accepted", "start_poachers_persuasion", "poachers_persuasion_start_reservation", "{=6P1ruzsC}Maybe...", persuasion_start_with_poachers_on_condition, persuasion_start_with_poachers_on_consequence, this);
			dialog.AddDialogLine("poachers_persuasion_rejected", "poachers_persuasion_start_reservation", "start", "{=!}{FAILED_PERSUASION_LINE}", persuasion_failed_with_poachers_on_condition, persuasion_rejected_with_poachers_on_consequence, this);
			dialog.AddDialogLine("poachers_persuasion_attempt", "poachers_persuasion_start_reservation", "poachers_persuasion_select_option", "{=wM77S68a}What's there to discuss?", () => !persuasion_failed_with_poachers_on_condition(), null, this);
			dialog.AddDialogLine("poachers_persuasion_success", "poachers_persuasion_start_reservation", "close_window", "{=JQKCPllJ}You've made your point.", ConversationManager.GetPersuasionProgressSatisfied, persuasion_complete_with_poachers_on_consequence, this, 200);
			ConversationSentence.OnConditionDelegate conditionDelegate = poachers_persuasion_select_option_1_on_condition;
			ConversationSentence.OnConsequenceDelegate consequenceDelegate = poachers_persuasion_select_option_1_on_consequence;
			ConversationSentence.OnPersuasionOptionDelegate persuasionOptionDelegate = poachers_persuasion_setup_option_1;
			ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = poachers_persuasion_clickable_option_1_on_condition;
			dialog.AddPlayerLine("poachers_persuasion_select_option_1", "poachers_persuasion_select_option", "poachers_persuasion_selected_option_response", "{=!}{POACHERS_PERSUADE_ATTEMPT_1}", conditionDelegate, consequenceDelegate, this, 100, clickableConditionDelegate, persuasionOptionDelegate);
			ConversationSentence.OnConditionDelegate conditionDelegate2 = poachers_persuasion_select_option_2_on_condition;
			ConversationSentence.OnConsequenceDelegate consequenceDelegate2 = poachers_persuasion_select_option_2_on_consequence;
			persuasionOptionDelegate = poachers_persuasion_setup_option_2;
			clickableConditionDelegate = poachers_persuasion_clickable_option_2_on_condition;
			dialog.AddPlayerLine("poachers_persuasion_select_option_2", "poachers_persuasion_select_option", "poachers_persuasion_selected_option_response", "{=!}{POACHERS_PERSUADE_ATTEMPT_2}", conditionDelegate2, consequenceDelegate2, this, 100, clickableConditionDelegate, persuasionOptionDelegate);
			ConversationSentence.OnConditionDelegate conditionDelegate3 = poachers_persuasion_select_option_3_on_condition;
			ConversationSentence.OnConsequenceDelegate consequenceDelegate3 = poachers_persuasion_select_option_3_on_consequence;
			persuasionOptionDelegate = poachers_persuasion_setup_option_3;
			clickableConditionDelegate = poachers_persuasion_clickable_option_3_on_condition;
			dialog.AddPlayerLine("poachers_persuasion_select_option_3", "poachers_persuasion_select_option", "poachers_persuasion_selected_option_response", "{=!}{POACHERS_PERSUADE_ATTEMPT_3}", conditionDelegate3, consequenceDelegate3, this, 100, clickableConditionDelegate, persuasionOptionDelegate);
			ConversationSentence.OnConditionDelegate conditionDelegate4 = poachers_persuasion_select_option_4_on_condition;
			ConversationSentence.OnConsequenceDelegate consequenceDelegate4 = poachers_persuasion_select_option_4_on_consequence;
			persuasionOptionDelegate = poachers_persuasion_setup_option_4;
			clickableConditionDelegate = poachers_persuasion_clickable_option_4_on_condition;
			dialog.AddPlayerLine("poachers_persuasion_select_option_4", "poachers_persuasion_select_option", "poachers_persuasion_selected_option_response", "{=!}{POACHERS_PERSUADE_ATTEMPT_4}", conditionDelegate4, consequenceDelegate4, this, 100, clickableConditionDelegate, persuasionOptionDelegate);
			ConversationSentence.OnConditionDelegate conditionDelegate5 = poachers_persuasion_select_option_5_on_condition;
			ConversationSentence.OnConsequenceDelegate consequenceDelegate5 = poachers_persuasion_select_option_5_on_consequence;
			persuasionOptionDelegate = poachers_persuasion_setup_option_5;
			clickableConditionDelegate = poachers_persuasion_clickable_option_5_on_condition;
			dialog.AddPlayerLine("poachers_persuasion_select_option_5", "poachers_persuasion_select_option", "poachers_persuasion_selected_option_response", "{=!}{POACHERS_PERSUADE_ATTEMPT_5}", conditionDelegate5, consequenceDelegate5, this, 100, clickableConditionDelegate, persuasionOptionDelegate);
			dialog.AddDialogLine("poachers_persuasion_select_option_reaction", "poachers_persuasion_selected_option_response", "poachers_persuasion_start_reservation", "{=!}{PERSUASION_REACTION}", poachers_persuasion_selected_option_response_on_condition, poachers_persuasion_selected_option_response_on_consequence, this);
		}

		private void persuasion_start_with_poachers_on_consequence()
		{
			ConversationManager.StartPersuasion(2f, 1f, 0f, 2f, 2f, 0f, PersuasionDifficulty.MediumHard);
		}

		private bool persuasion_start_with_poachers_on_condition()
		{
			if (_poachersParty != null)
			{
				return CharacterObject.OneToOneConversationCharacter == ConversationHelper.GetConversationCharacterPartyLeader(_poachersParty.Party);
			}
			return false;
		}

		private PersuasionTask GetPersuasionTask()
		{
			PersuasionTask persuasionTask = new PersuasionTask(0);
			persuasionTask.FinalFailLine = new TextObject("{=l7Jt5tvt}This is how I earn my living, and all your clever talk doesn't make it any different. Leave now!");
			persuasionTask.TryLaterLine = new TextObject("{=!}TODO");
			persuasionTask.SpokenLine = new TextObject("{=wM77S68a}What's there to discuss?");
			PersuasionOptionArgs option = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.Easy, givesCriticalSuccess: false, new TextObject("{=cQCs72U7}You're not bad people. You can easily ply your trade somewhere else, somewhere safe."));
			persuasionTask.AddOptionToTask(option);
			PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Valor, TraitEffect.Positive, PersuasionArgumentStrength.ExtremelyHard, givesCriticalSuccess: true, new TextObject("{=bioyMrUD}You are just a bunch of hunters. You don't stand a chance against us!"), null, canBlockOtherOption: true);
			persuasionTask.AddOptionToTask(option2);
			PersuasionOptionArgs option3 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Mercy, TraitEffect.Positive, PersuasionArgumentStrength.Normal, givesCriticalSuccess: false, new TextObject("{=FO1oruNy}You talk about poor folk, but you think the people here like their village turned into a nest of outlaws?"));
			persuasionTask.AddOptionToTask(option3);
			TextObject textObject = new TextObject("{=S0NeQdLp}You had an agreement with {QUEST_GIVER.NAME}. Your word is your bond, no matter which side of the law you're on.");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
			PersuasionOptionArgs option4 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, PersuasionArgumentStrength.Normal, givesCriticalSuccess: false, textObject);
			persuasionTask.AddOptionToTask(option4);
			PersuasionOptionArgs option5 = new PersuasionOptionArgs(line: new TextObject("{=brW4pjPQ}Flee while you can. An army is already on its way here to hang you all."), skill: DefaultSkills.Roguery, trait: DefaultTraits.Calculating, traitEffect: TraitEffect.Positive, argumentStrength: PersuasionArgumentStrength.Hard, givesCriticalSuccess: true);
			persuasionTask.AddOptionToTask(option5);
			return persuasionTask;
		}

		private bool poachers_persuasion_selected_option_response_on_condition()
		{
			PersuasionOptionResult item = ConversationManager.GetPersuasionChosenOptions().Last().Item2;
			MBTextManager.SetTextVariable("PERSUASION_REACTION", PersuasionHelper.GetDefaultPersuasionOptionReaction(item));
			if (item == PersuasionOptionResult.CriticalFailure)
			{
				_task.BlockAllOptions();
			}
			return true;
		}

		private void poachers_persuasion_selected_option_response_on_consequence()
		{
			Tuple<PersuasionOptionArgs, PersuasionOptionResult> tuple = ConversationManager.GetPersuasionChosenOptions().Last();
			float difficulty = Campaign.Current.Models.PersuasionModel.GetDifficulty(PersuasionDifficulty.MediumHard);
			Campaign.Current.Models.PersuasionModel.GetEffectChances(tuple.Item1, out var moveToNextStageChance, out var blockRandomOptionChance, difficulty);
			_task.ApplyEffects(moveToNextStageChance, blockRandomOptionChance);
		}

		private bool poachers_persuasion_select_option_1_on_condition()
		{
			if (_task.Options.Count > 0)
			{
				TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
				textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(_task.Options.ElementAt(0), showToPlayer: false));
				textObject.SetTextVariable("PERSUASION_OPTION_LINE", _task.Options.ElementAt(0).Line);
				MBTextManager.SetTextVariable("POACHERS_PERSUADE_ATTEMPT_1", textObject);
				return true;
			}
			return false;
		}

		private bool poachers_persuasion_select_option_2_on_condition()
		{
			if (_task.Options.Count > 1)
			{
				TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
				textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(_task.Options.ElementAt(1), showToPlayer: false));
				textObject.SetTextVariable("PERSUASION_OPTION_LINE", _task.Options.ElementAt(1).Line);
				MBTextManager.SetTextVariable("POACHERS_PERSUADE_ATTEMPT_2", textObject);
				return true;
			}
			return false;
		}

		private bool poachers_persuasion_select_option_3_on_condition()
		{
			if (_task.Options.Count > 2)
			{
				TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
				textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(_task.Options.ElementAt(2), showToPlayer: false));
				textObject.SetTextVariable("PERSUASION_OPTION_LINE", _task.Options.ElementAt(2).Line);
				MBTextManager.SetTextVariable("POACHERS_PERSUADE_ATTEMPT_3", textObject);
				return true;
			}
			return false;
		}

		private bool poachers_persuasion_select_option_4_on_condition()
		{
			if (_task.Options.Count > 3)
			{
				TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
				textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(_task.Options.ElementAt(3), showToPlayer: false));
				textObject.SetTextVariable("PERSUASION_OPTION_LINE", _task.Options.ElementAt(3).Line);
				MBTextManager.SetTextVariable("POACHERS_PERSUADE_ATTEMPT_4", textObject);
				return true;
			}
			return false;
		}

		private bool poachers_persuasion_select_option_5_on_condition()
		{
			if (_task.Options.Count > 4)
			{
				TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
				textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(_task.Options.ElementAt(4), showToPlayer: false));
				textObject.SetTextVariable("PERSUASION_OPTION_LINE", _task.Options.ElementAt(4).Line);
				MBTextManager.SetTextVariable("POACHERS_PERSUADE_ATTEMPT_5", textObject);
				return true;
			}
			return false;
		}

		private void poachers_persuasion_select_option_1_on_consequence()
		{
			if (_task.Options.Count > 0)
			{
				_task.Options[0].BlockTheOption(isBlocked: true);
			}
		}

		private void poachers_persuasion_select_option_2_on_consequence()
		{
			if (_task.Options.Count > 1)
			{
				_task.Options[1].BlockTheOption(isBlocked: true);
			}
		}

		private void poachers_persuasion_select_option_3_on_consequence()
		{
			if (_task.Options.Count > 2)
			{
				_task.Options[2].BlockTheOption(isBlocked: true);
			}
		}

		private void poachers_persuasion_select_option_4_on_consequence()
		{
			if (_task.Options.Count > 3)
			{
				_task.Options[3].BlockTheOption(isBlocked: true);
			}
		}

		private void poachers_persuasion_select_option_5_on_consequence()
		{
			if (_task.Options.Count > 4)
			{
				_task.Options[4].BlockTheOption(isBlocked: true);
			}
		}

		private bool persuasion_failed_with_poachers_on_condition()
		{
			if (_task.Options.All((PersuasionOptionArgs x) => x.IsBlocked) && !ConversationManager.GetPersuasionProgressSatisfied())
			{
				MBTextManager.SetTextVariable("FAILED_PERSUASION_LINE", _task.FinalFailLine);
				return true;
			}
			return false;
		}

		private PersuasionOptionArgs poachers_persuasion_setup_option_1()
		{
			return _task.Options.ElementAt(0);
		}

		private PersuasionOptionArgs poachers_persuasion_setup_option_2()
		{
			return _task.Options.ElementAt(1);
		}

		private PersuasionOptionArgs poachers_persuasion_setup_option_3()
		{
			return _task.Options.ElementAt(2);
		}

		private PersuasionOptionArgs poachers_persuasion_setup_option_4()
		{
			return _task.Options.ElementAt(3);
		}

		private PersuasionOptionArgs poachers_persuasion_setup_option_5()
		{
			return _task.Options.ElementAt(4);
		}

		private bool poachers_persuasion_clickable_option_1_on_condition(out TextObject hintText)
		{
			hintText = new TextObject("{=9ACJsI6S}Blocked");
			if (_task.Options.Count > 0)
			{
				hintText = (_task.Options.ElementAt(0).IsBlocked ? hintText : null);
				return !_task.Options.ElementAt(0).IsBlocked;
			}
			return false;
		}

		private bool poachers_persuasion_clickable_option_2_on_condition(out TextObject hintText)
		{
			hintText = new TextObject("{=9ACJsI6S}Blocked");
			if (_task.Options.Count > 1)
			{
				hintText = (_task.Options.ElementAt(1).IsBlocked ? hintText : null);
				return !_task.Options.ElementAt(1).IsBlocked;
			}
			return false;
		}

		private bool poachers_persuasion_clickable_option_3_on_condition(out TextObject hintText)
		{
			hintText = new TextObject("{=9ACJsI6S}Blocked");
			if (_task.Options.Count > 2)
			{
				hintText = (_task.Options.ElementAt(2).IsBlocked ? hintText : null);
				return !_task.Options.ElementAt(2).IsBlocked;
			}
			return false;
		}

		private bool poachers_persuasion_clickable_option_4_on_condition(out TextObject hintText)
		{
			hintText = new TextObject("{=9ACJsI6S}Blocked");
			if (_task.Options.Count > 3)
			{
				hintText = (_task.Options.ElementAt(3).IsBlocked ? hintText : null);
				return !_task.Options.ElementAt(3).IsBlocked;
			}
			return false;
		}

		private bool poachers_persuasion_clickable_option_5_on_condition(out TextObject hintText)
		{
			hintText = new TextObject("{=9ACJsI6S}Blocked");
			if (_task.Options.Count > 4)
			{
				hintText = (_task.Options.ElementAt(4).IsBlocked ? hintText : null);
				return !_task.Options.ElementAt(4).IsBlocked;
			}
			return false;
		}

		private void persuasion_rejected_with_poachers_on_consequence()
		{
			PlayerEncounter.LeaveEncounter = false;
			ConversationManager.EndPersuasion();
		}

		private void persuasion_complete_with_poachers_on_consequence()
		{
			PlayerEncounter.LeaveEncounter = true;
			ConversationManager.EndPersuasion();
			Campaign.Current.GameMenuManager.SetNextMenu("village");
			Campaign.Current.ConversationManager.ConversationEndOneShot += QuestSuccessPlayerComesToAnAgreementWithPoachers;
		}

		internal void StartQuestBattle()
		{
			PlayerEncounter.RestartPlayerEncounter(PartyBase.MainParty, _poachersParty.Party, forcePlayerOutFromSettlement: false);
			PlayerEncounter.StartBattle();
			PlayerEncounter.Update();
			_talkedToPoachersBattleWillStart = false;
			MapEvent.PlayerMapEvent.AttackerSide.RemoveNearbyPartiesFromPlayerMapEvent();
			MapEvent.PlayerMapEvent.DefenderSide.RemoveNearbyPartiesFromPlayerMapEvent();
			GameMenu.ActivateGameMenu("army_of_poachers_village");
			CampaignMission.OpenBattleMission(_questVillage.Settlement.LocationComplex.GetScene("village_center", 1), usesTownDecalAtlas: false);
			_isReadyToBeFinalized = true;
		}

		private bool DialogCondition()
		{
			return Hero.OneToOneConversationHero == base.QuestGiver;
		}

		protected override void SetDialogs()
		{
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(new TextObject("{=IefM6uAy}Thank you. You'll be paid well. Also you can keep their illegally obtained leather.[ib:normal2][if:convo_bemused]")).Condition(DialogCondition)
				.NpcLine(new TextObject("{=NC2VGafO}They skin their beasts in the woods, then go into the village after midnight to stash the hides. The villagers are terrified of them, I believe. If you go into the village late at night, you should be able to track them down.[ib:normal][if:convo_thinking]"))
				.NpcLine(new TextObject("{=3pkVKMnA}Most poachers would probably run if they were surprised by armed men. But these ones are bold and desperate. Be ready for a fight.[ib:normal2][if:convo_undecided_closed]"))
				.Consequence(QuestAcceptedConsequences)
				.CloseDialog();
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(new TextObject("{=QNV1b5s5}Are those poachers still in business?[ib:normal2][if:convo_undecided_open]")).Condition(DialogCondition)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=JhJBBWab}They will be gone soon."))
				.NpcLine(new TextObject("{=gjGb044I}I hope they will be...[ib:normal2][if:convo_dismayed]"))
				.CloseDialog()
				.PlayerOption(new TextObject("{=Gu3jF88V}Any night battle can easily go wrong. I need more time to prepare."))
				.NpcLine(new TextObject("{=2EiC1YyZ}Well, if they get wind of what you're up to, things could go very wrong for me. Do be quick.[ib:nervous2][if:convo_dismayed]"))
				.CloseDialog()
				.EndPlayerOptions();
			QuestCharacterDialogFlow = GetPoacherPartyDialogFlow();
		}

		internal void CreatePoachersParty()
		{
			Hideout closestHideout = SettlementHelper.FindNearestHideoutToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.Default, (Settlement x) => x.IsActive);
			Clan clan = Clan.BanditFactions.FirstOrDefaultQ((Clan t) => t.Culture == closestHideout.Settlement.Culture);
			_poachersParty = CustomPartyComponent.CreateCustomPartyWithTroopRoster(_questVillage.Settlement.GatePosition, 1f, null, new TextObject("{=WQa1R55u}Poachers Party"), clan, TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), null);
			ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>("leather");
			int num = TaleWorlds.Library.MathF.Ceiling(_difficultyMultiplier * 5f) + MBRandom.RandomInt(0, 2);
			_poachersParty.ItemRoster.AddToCounts(item, num * 2);
			CharacterObject character = CharacterObject.All.FirstOrDefault((CharacterObject t) => t.StringId == "poacher");
			int count = 10 + TaleWorlds.Library.MathF.Ceiling(40f * _difficultyMultiplier);
			_poachersParty.MemberRoster.AddToCounts(character, count);
			_poachersParty.SetPartyUsedByQuest(isActivelyUsed: true);
			_poachersParty.Ai.DisableAi();
			EnterSettlementAction.ApplyForParty(_poachersParty, Settlement.CurrentSettlement);
		}

		private void QuestAcceptedConsequences()
		{
			StartQuest();
			AddLog(QuestStartedLogText);
			AddTrackedObject(_questVillage.Settlement);
		}

		internal void QuestFailedAfterTalkingWithPoachers()
		{
			AddLog(QuestFailedAfterTalkingWithProachers);
			TraitLevelingHelper.OnIssueFailed(base.QuestGiver, new Tuple<TraitObject, int>[2]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -50),
				new Tuple<TraitObject, int>(DefaultTraits.Mercy, 20)
			});
			RelationshipChangeWithQuestGiver = -5;
			base.QuestGiver.AddPower(-50f);
			base.QuestGiver.CurrentSettlement.Town.Security -= 5f;
			base.QuestGiver.CurrentSettlement.Town.Prosperity -= 30f;
			CompleteQuestWithFail();
		}

		internal void QuestSuccessPlayerComesToAnAgreementWithPoachers()
		{
			AddLog(QuestSuccessPlayerComesToAnAgreementWithPoachersQuestLogText);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[2]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 10),
				new Tuple<TraitObject, int>(DefaultTraits.Mercy, 50)
			});
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, _rewardGold);
			RelationshipChangeWithQuestGiver = 5;
			GainRenownAction.Apply(Hero.MainHero, 1f);
			base.QuestGiver.AddPower(30f);
			base.QuestGiver.CurrentSettlement.Town.Security -= 5f;
			base.QuestGiver.CurrentSettlement.Town.Prosperity += 50f;
			CompleteQuestWithSuccess();
		}

		internal void QuestFailWithPlayerDefeatedAgainstPoachers()
		{
			AddLog(QuestFailWithPlayerDefeatedAgainstPoachersQuestLogText);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -30)
			});
			RelationshipChangeWithQuestGiver = -5;
			base.QuestGiver.AddPower(-50f);
			base.QuestGiver.CurrentSettlement.Town.Security -= 5f;
			base.QuestGiver.CurrentSettlement.Town.Prosperity -= 30f;
			CompleteQuestWithFail();
		}

		internal void QuestSuccessWithPlayerDefeatedPoachers()
		{
			AddLog(QuestSuccessWithPlayerDefeatedPoachersQuestLogText);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 50)
			});
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, _rewardGold);
			RelationshipChangeWithQuestGiver = 5;
			base.QuestGiver.AddPower(30f);
			base.QuestGiver.CurrentSettlement.Town.Prosperity += 50f;
			CompleteQuestWithSuccess();
		}

		protected override void OnTimedOut()
		{
			AddLog(QuestFailedWithTimeOutLogText);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -30)
			});
			RelationshipChangeWithQuestGiver = -5;
			base.QuestGiver.AddPower(-50f);
			base.QuestGiver.CurrentSettlement.Town.Prosperity -= 30f;
			base.QuestGiver.CurrentSettlement.Town.Security -= 5f;
		}

		private void QuestCanceledTargetVillageRaided()
		{
			AddLog(QuestCanceledTargetVillageRaidedQuestLogText);
			CompleteQuestWithFail();
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, MapEventCheck);
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, MapEventStarted);
			CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, GameMenuOpened);
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
			CampaignEvents.CanHeroBecomePrisonerEvent.AddNonSerializedListener(this, OnCanHeroBecomePrisonerInfoIsRequested);
		}

		private void OnCanHeroBecomePrisonerInfoIsRequested(Hero hero, ref bool result)
		{
			if (hero == Hero.MainHero && _isReadyToBeFinalized)
			{
				result = false;
			}
		}

		protected override void HourlyTick()
		{
			if (PlayerEncounter.Current != null && PlayerEncounter.Current.IsPlayerWaiting && PlayerEncounter.EncounterSettlement == _questVillage.Settlement && CampaignTime.Now.IsNightTime && !_isReadyToBeFinalized && base.IsOngoing)
			{
				EnterSettlementAction.ApplyForParty(MobileParty.MainParty, _questVillage.Settlement);
				GameMenu.SwitchToMenu("army_of_poachers_village");
			}
		}

		private void GameMenuOpened(MenuCallbackArgs obj)
		{
			if (obj.MenuContext.GameMenu.StringId == "village" && CampaignTime.Now.IsNightTime && Settlement.CurrentSettlement == _questVillage.Settlement && !_isReadyToBeFinalized)
			{
				GameMenu.SwitchToMenu("army_of_poachers_village");
			}
			if (obj.MenuContext.GameMenu.StringId == "army_of_poachers_village" && _isReadyToBeFinalized && MapEvent.PlayerMapEvent != null && MapEvent.PlayerMapEvent.HasWinner && _poachersParty != null)
			{
				_poachersParty.IsVisible = false;
			}
		}

		private void MapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			MapEventCheck(mapEvent);
			if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
			{
				QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
			}
		}

		private void MapEventCheck(MapEvent mapEvent)
		{
			if (mapEvent.IsRaid && mapEvent.MapEventSettlement == _questVillage.Settlement)
			{
				QuestCanceledTargetVillageRaided();
			}
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

		protected override void OnFinalize()
		{
			if (_poachersParty != null && _poachersParty.IsActive)
			{
				DestroyPartyAction.Apply(null, _poachersParty);
			}
			if (Hero.MainHero.IsPrisoner)
			{
				EndCaptivityAction.ApplyByPeace(Hero.MainHero);
			}
			if (Campaign.Current.CurrentMenuContext != null && Campaign.Current.CurrentMenuContext.GameMenu.StringId == "army_of_poachers_village")
			{
				PlayerEncounter.Finish();
			}
		}

		protected override void InitializeQuestOnGameLoad()
		{
			SetDialogs();
		}
	}

	public class MerchantArmyOfPoachersIssueBehaviorTypeDefiner : SaveableTypeDefiner
	{
		public MerchantArmyOfPoachersIssueBehaviorTypeDefiner()
			: base(800000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(MerchantArmyOfPoachersIssue), 1);
			AddClassDefinition(typeof(MerchantArmyOfPoachersIssueQuest), 2);
		}
	}

	private const IssueBase.IssueFrequency ArmyOfPoachersIssueFrequency = IssueBase.IssueFrequency.Common;

	private MerchantArmyOfPoachersIssueQuest _cachedQuest;

	private static MerchantArmyOfPoachersIssueQuest Instance
	{
		get
		{
			MerchantArmyOfPoachersIssueBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<MerchantArmyOfPoachersIssueBehavior>();
			if (campaignBehavior._cachedQuest != null && campaignBehavior._cachedQuest.IsOngoing)
			{
				return campaignBehavior._cachedQuest;
			}
			foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
			{
				if (quest is MerchantArmyOfPoachersIssueQuest cachedQuest)
				{
					campaignBehavior._cachedQuest = cachedQuest;
					return campaignBehavior._cachedQuest;
				}
			}
			return null;
		}
	}

	private void engage_poachers_consequence(MenuCallbackArgs args)
	{
		Instance.StartQuestBattle();
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
	}

	private bool poachers_menu_back_condition(MenuCallbackArgs args)
	{
		return Hero.MainHero.IsWounded;
	}

	private void OnSessionLaunched(CampaignGameStarter gameStarter)
	{
		gameStarter.AddGameMenu("army_of_poachers_village", "{=eaQxeRh6}A boy runs out of the village and asks you to talk to the leader of the poachers. The villagers want to avoid a fight outside their homes.", army_of_poachers_village_on_init);
		gameStarter.AddGameMenuOption("army_of_poachers_village", "engage_the_poachers", "{=xF7he8fZ}Fight the poachers", engage_poachers_condition, engage_poachers_consequence);
		gameStarter.AddGameMenuOption("army_of_poachers_village", "talk_to_the_poachers", "{=wwJGE28v}Negotiate with the poachers", talk_to_leader_of_poachers_condition, talk_to_leader_of_poachers_consequence);
		gameStarter.AddGameMenuOption("army_of_poachers_village", "back_poachers", "{=E1OwmQFb}Back", poachers_menu_back_condition, poachers_menu_back_consequence);
	}

	private void army_of_poachers_village_on_init(MenuCallbackArgs args)
	{
		if (Instance == null || !Instance.IsOngoing)
		{
			return;
		}
		args.MenuContext.SetBackgroundMeshName(Instance._questVillage.Settlement.SettlementComponent.WaitMeshName);
		if (Instance._poachersParty == null && !Hero.MainHero.IsWounded)
		{
			Instance.CreatePoachersParty();
		}
		if (Instance._isReadyToBeFinalized && PlayerEncounter.Current != null)
		{
			bool flag = PlayerEncounter.Battle.WinningSide == PlayerEncounter.Battle.PlayerSide;
			PlayerEncounter.Update();
			if (PlayerEncounter.Current == null)
			{
				Instance._isReadyToBeFinalized = false;
				if (flag)
				{
					Instance.QuestSuccessWithPlayerDefeatedPoachers();
				}
				else
				{
					Instance.QuestFailWithPlayerDefeatedAgainstPoachers();
				}
			}
			else if (PlayerEncounter.Battle.WinningSide == BattleSideEnum.None)
			{
				PlayerEncounter.LeaveEncounter = true;
				PlayerEncounter.Update();
				Instance.QuestFailWithPlayerDefeatedAgainstPoachers();
			}
			else if (flag && PlayerEncounter.Current != null && Game.Current.GameStateManager.ActiveState is MapState)
			{
				PlayerEncounter.Finish();
				Instance.QuestSuccessWithPlayerDefeatedPoachers();
			}
		}
		if (Instance != null && Instance._talkedToPoachersBattleWillStart)
		{
			Instance.StartQuestBattle();
		}
	}

	private bool engage_poachers_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		if (Hero.MainHero.IsWounded)
		{
			args.Tooltip = new TextObject("{=gEHEQazX}You're heavily wounded and not fit for the fight. Come back when you're ready.");
			args.IsEnabled = false;
		}
		return true;
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private bool talk_to_leader_of_poachers_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
		if (Hero.MainHero.IsWounded)
		{
			args.Tooltip = new TextObject("{=gEHEQazX}You're heavily wounded and not fit for the fight. Come back when you're ready.");
			args.IsEnabled = false;
		}
		return true;
	}

	private void poachers_menu_back_consequence(MenuCallbackArgs args)
	{
		PlayerEncounter.LeaveSettlement();
		PlayerEncounter.Finish();
	}

	private bool ConditionsHold(Hero issueGiver, out Village questVillage)
	{
		questVillage = null;
		if (issueGiver.CurrentSettlement != null)
		{
			questVillage = issueGiver.CurrentSettlement.BoundVillages.GetRandomElementWithPredicate((Village x) => !x.Settlement.IsUnderRaid && !x.Settlement.IsRaided);
			if (questVillage != null && issueGiver.IsMerchant && issueGiver.GetTraitLevel(DefaultTraits.Mercy) + issueGiver.GetTraitLevel(DefaultTraits.Honor) < 0)
			{
				Town town = issueGiver.CurrentSettlement.Town;
				if (town != null && town.Security <= 60f)
				{
					return SettlementHelper.FindNearestHideoutToSettlement(questVillage.Settlement, MobileParty.NavigationType.Default, (Settlement x) => x.IsActive) != null;
				}
			}
			return false;
		}
		return false;
	}

	private void talk_to_leader_of_poachers_consequence(MenuCallbackArgs args)
	{
		CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty), new ConversationCharacterData(ConversationHelper.GetConversationCharacterPartyLeader(Instance._poachersParty.Party), Instance._poachersParty.Party));
	}

	public void OnCheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero, out var questVillage))
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnSelected, typeof(MerchantArmyOfPoachersIssue), IssueBase.IssueFrequency.Common, questVillage));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(MerchantArmyOfPoachersIssue), IssueBase.IssueFrequency.Common));
		}
	}

	private IssueBase OnSelected(in PotentialIssueData pid, Hero issueOwner)
	{
		return new MerchantArmyOfPoachersIssue(issueOwner, pid.RelatedObject as Village);
	}
}
