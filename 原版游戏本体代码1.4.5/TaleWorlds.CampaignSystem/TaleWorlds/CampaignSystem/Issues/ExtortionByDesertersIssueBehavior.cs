using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
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

public class ExtortionByDesertersIssueBehavior : CampaignBehaviorBase
{
	public class ExtortionByDesertersIssue : IssueBase
	{
		internal const int IssuePreConditionMaxSecurityLevel = 50;

		internal const int IssueStayAliveConditionMaxSecurityLevel = 80;

		internal const float IssuePreConditionMinPlayerRelation = -10f;

		internal const int MinimumRequiredMenCount = 20;

		internal const int IssueDuration = 15;

		internal const int QuestTimeLimit = 12;

		internal const int AlternativeSolutionTroopTierRequirement = 2;

		private const int AlternativeSolutionRangedSkillThreshold = 150;

		private const int AlternativeSolutionEngineeringSkillThreshold = 120;

		private const int AlternativeSolutionHonorRewardOnSuccess = 60;

		private const int AlternativeSolutionHonorPenaltyOnFail = -20;

		private const int AlternativeSolutionRelationRewardOnSuccess = 8;

		private const int AlternativeSolutionRelationPenaltyOnFail = -10;

		private const int AlternativeSolutionIssueOwnerPowerBonusOnSuccess = 15;

		private const int AlternativeSolutionIssueOwnerPowerPenaltyOnFail = -10;

		private const int AlternativeSolutionTownSecurityBonusOnSuccess = 10;

		private const int AlternativeSolutionTownSecurityPenaltyOnFail = -10;

		private const int AlternativeSolutionTownProsperityBonusOnSuccess = 100;

		private const int AlternativeSolutionTownProsperityPenaltyOnFail = -50;

		private const int AlternativeSolutionPlayerRenownBonusOnSuccess = 2;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.Casualties | AlternativeSolutionScaleFlag.FailureRisk;

		protected override int CompanionSkillRewardXP => (int)(800f + 1000f * base.IssueDifficultyMultiplier);

		public override TextObject IssueBriefByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=NhDEwaab}Well, if you know how to fight... Yes, we could use help. A group of deserters is camped out near here. They come every few weeks, demanding food and money. They've killed two villagers who resisted them. We asked our {?LORD.GENDER}mistress{?}lord{\\?}, {LORD.NAME}, for help but {?LORD.GENDER}her{?}his{\\?} men never get here in time.[if:convo_stern][ib:closed]");
				StringHelpers.SetCharacterProperties("LORD", base.IssueSettlement.OwnerClan.Leader.CharacterObject, textObject);
				if (base.IssueSettlement.OwnerClan.Leader == Hero.MainHero)
				{
					textObject = new TextObject("{=dYO8piOM}Yes, my {?PLAYER.GENDER}lady{?}lord{\\?}. It is good of you to ask. A group of deserters is camped out near here. They come every few weeks, demanding food and money. They've killed two villagers who resisted them.[if:convo_stern][ib:closed]");
				}
				return textObject;
			}
		}

		public override TextObject IssueAcceptByPlayer => new TextObject("{=A6iOIurY}How can I help you?");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver => new TextObject("{=tb0gqxDZ}Here's the plan. We lay an ambush in the village. When they show up, we spring. If you join us, I think we've got a good chance of ridding ourselves of this scourge before they murder us one by one.[if:convo_stern][ib:normal]");

		public override TextObject IssueQuestSolutionAcceptByPlayer => new TextObject("{=vKxiFQRA}All right. I'll wait here and join you in your fight.");

		public override TextObject IssueAlternativeSolutionSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=CLvxAzyV}Following your orders, {COMPANION.LINK} and your men has defeated the deserters and saved the people of {SETTLEMENT}.");
				textObject.SetTextVariable("SETTLEMENT", base.IssueSettlement.EncyclopediaLinkWithName);
				textObject.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject);
				return textObject;
			}
		}

		public override bool IsThereAlternativeSolution => true;

		public override int AlternativeSolutionBaseNeededMenCount => 21 + TaleWorlds.Library.MathF.Ceiling(25f * base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 3 + TaleWorlds.Library.MathF.Ceiling(5f * base.IssueDifficultyMultiplier);

		protected override int RewardGold => 800 + TaleWorlds.Library.MathF.Ceiling(4200f * base.IssueDifficultyMultiplier);

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=J0Clb1RD}If you don't have the time, at least send one of your best companions with {TROOP_COUNT} experienced men for {RETURN_DAYS} days.[if:convo_stern][ib:closed]");
				textObject.SetTextVariable("TROOP_COUNT", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionAcceptByPlayer => new TextObject("{=A4oWozvO}I don't have time for this, but my companion and my men will wait here with you.");

		public override TextObject IssueDiscussAlternativeSolution => new TextObject("{=YwMMZFSm}We're working with your men to plan the ambush, {?PLAYER.GENDER}madam{?}sir{\\?}. These thieves will regret the day they ever thought we were easy pickings.");

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver => new TextObject("{=OLcbuZSa}Thank you. Your help is much appreciated.");

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=MTVhHmbb}{ISSUE_GIVER.LINK} told you that a group of deserters have been raiding their village regularly. You asked your companion {COMPANION.LINK} to take {TROOP_COUNT} men to stay with {ISSUE_GIVER.LINK} and defend the people of {ISSUE_SETTLEMENT}.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				textObject.SetTextVariable("ISSUE_SETTLEMENT", base.IssueSettlement.Name);
				textObject.SetTextVariable("TROOP_COUNT", AlternativeSolutionSentTroops.TotalManCount - 1);
				return textObject;
			}
		}

		public override bool IsThereLordSolution => false;

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=P2VNGJDa}Extortion by Deserters at {SETTLEMENT}");
				textObject.SetTextVariable("SETTLEMENT", base.IssueSettlement.Name);
				return textObject;
			}
		}

		public override TextObject Description
		{
			get
			{
				TextObject textObject = new TextObject("{=atk3PTae}Deserters are extorting from the villagers of {SETTLEMENT} and have killed several who resisted.");
				textObject.SetTextVariable("SETTLEMENT", base.IssueSettlement.Name);
				return textObject;
			}
		}

		public override TextObject IssueAsRumorInSettlement
		{
			get
			{
				TextObject textObject = new TextObject("{=Fv8CKNU2}I hope {QUEST_GIVER.NAME} knows what {?QUEST_GIVER.GENDER}she's{?}he's{\\?} doing with that plan to get rid of the deserters.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsExtortionByDesertersIssue(object o, List<object> collectedObjects)
		{
			((ExtortionByDesertersIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		public override bool AlternativeSolutionCondition(out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
		{
			return character.Tier >= 2;
		}

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			RelationshipChangeWithIssueOwner = 8;
			base.IssueOwner.AddPower(15f);
			base.IssueSettlement.Village.Bound.Town.Security += 10f;
			base.IssueSettlement.Village.Bound.Town.Prosperity += 100f;
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.IssueOwner, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 60)
			});
		}

		protected override void AlternativeSolutionEndWithFailureConsequence()
		{
			base.IssueSettlement.Village.Bound.Town.Security += -10f;
			base.IssueSettlement.Village.Bound.Town.Prosperity += -50f;
			RelationshipChangeWithIssueOwner = -10;
			base.IssueOwner.AddPower(-10f);
			TraitLevelingHelper.OnIssueFailed(base.IssueOwner, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -20)
			});
		}

		protected override void OnGameLoad()
		{
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new ExtortionByDesertersIssueQuest(questId, base.IssueOwner, base.IssueDifficultyMultiplier, RewardGold, CampaignTime.DaysFromNow(12f));
		}

		public override IssueFrequency GetFrequency()
		{
			return IssueFrequency.Common;
		}

		protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out PreconditionFlags flag, out Hero relationHero, out SkillObject skill, out int requiredGold)
		{
			flag = PreconditionFlags.None;
			skill = null;
			requiredGold = 0;
			relationHero = null;
			if (issueGiver.GetRelationWithPlayer() < -10f)
			{
				relationHero = issueGiver;
				flag |= PreconditionFlags.Relation;
			}
			Settlement currentSettlement = issueGiver.CurrentSettlement;
			if (currentSettlement != null && FactionManager.IsAtWarAgainstFaction(currentSettlement.MapFaction, Hero.MainHero.MapFaction))
			{
				flag |= PreconditionFlags.AtWar;
			}
			if (MobileParty.MainParty.MemberRoster.TotalHealthyCount < 20)
			{
				flag |= PreconditionFlags.NotEnoughTroops;
			}
			return flag == PreconditionFlags.None;
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}

		public override bool IssueStayAliveConditions()
		{
			if (base.IssueSettlement.Village.Bound.Town.Security <= 80f && !base.IssueOwner.CurrentSettlement.IsRaided)
			{
				return !base.IssueOwner.CurrentSettlement.IsUnderRaid;
			}
			return false;
		}

		public ExtortionByDesertersIssue(Hero issueOwner)
			: base(issueOwner, CampaignTime.DaysFromNow(15f))
		{
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.SettlementProsperity)
			{
				return -1f;
			}
			if (issueEffect == DefaultIssueEffects.SettlementSecurity)
			{
				return -1f;
			}
			if (issueEffect == DefaultIssueEffects.SettlementLoyalty)
			{
				return -0.5f;
			}
			return 0f;
		}

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			int skillValue = hero.GetSkillValue(DefaultSkills.Bow);
			int skillValue2 = hero.GetSkillValue(DefaultSkills.Crossbow);
			int skillValue3 = hero.GetSkillValue(DefaultSkills.Throwing);
			int skillValue4 = hero.GetSkillValue(DefaultSkills.Engineering);
			if (skillValue >= skillValue2 && skillValue >= skillValue3 && skillValue >= skillValue4)
			{
				return (DefaultSkills.Bow, 150);
			}
			if (skillValue2 >= skillValue3 && skillValue2 >= skillValue4)
			{
				return (DefaultSkills.Crossbow, 150);
			}
			if (skillValue3 < skillValue4)
			{
				return (DefaultSkills.Engineering, 120);
			}
			return (DefaultSkills.Throwing, 150);
		}
	}

	public class ExtortionByDesertersIssueQuest : QuestBase
	{
		private readonly struct ExtortionByDesertersQuestResult
		{
			public readonly int RenownChange;

			public readonly int HonorChange;

			public readonly float GoldMultiplier;

			public readonly int QuestGiverRelationChange;

			public readonly int QuestGiverPowerChange;

			public readonly int TownSecurityChange;

			public readonly int TownProsperityChange;

			public readonly bool IsSuccess;

			public ExtortionByDesertersQuestResult(int renownChange, int honorChange, float goldMultiplier, int questGiverRelationChange, int questGiverPowerChange, int townSecurityChange, int townProsperityChange, bool isSuccess)
			{
				RenownChange = renownChange;
				HonorChange = honorChange;
				GoldMultiplier = goldMultiplier;
				QuestGiverRelationChange = questGiverRelationChange;
				QuestGiverPowerChange = questGiverPowerChange;
				TownSecurityChange = townSecurityChange;
				TownProsperityChange = townProsperityChange;
				IsSuccess = isSuccess;
			}
		}

		internal enum ExtortionByDesertersQuestState
		{
			DesertersMovingToSettlement,
			DesertersRunningAwayFromPlayer,
			DesertersDefeatedPlayer,
			DesertersAreDefeated
		}

		private const float SightOffsetToLoseDeserterParty = 3f;

		private const float SightRatioToStartEvadingPlayerParty = 0.8f;

		[SaveableField(1)]
		private MobileParty _deserterMobileParty;

		[SaveableField(2)]
		private MobileParty _defenderMobileParty;

		[SaveableField(3)]
		private ExtortionByDesertersQuestState _currentState;

		[SaveableField(4)]
		private readonly float _questDifficultyMultiplier;

		[SaveableField(5)]
		private bool _deserterBattleFinalizedForTheFirstTime;

		private bool _playerAwayFromSettlementNotificationSent;

		[SaveableField(6)]
		private CampaignTime _desertersRunAwayTimeoutTime;

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=P2VNGJDa}Extortion by Deserters at {SETTLEMENT}");
				textObject.SetTextVariable("SETTLEMENT", QuestSettlement.Name);
				return textObject;
			}
		}

		private ExtortionByDesertersQuestResult _questResultSuccess1 => new ExtortionByDesertersQuestResult(0, 20, 1f, 4, 5, 10, 0, isSuccess: true);

		private ExtortionByDesertersQuestResult _questResultSuccess2 => new ExtortionByDesertersQuestResult(1, 40, 0.5f, 6, 10, 10, 25, isSuccess: true);

		private ExtortionByDesertersQuestResult _questResultSuccess3 => new ExtortionByDesertersQuestResult(2, 60, 0f, 8, 15, 10, 100, isSuccess: true);

		private ExtortionByDesertersQuestResult _questResultFail1 => new ExtortionByDesertersQuestResult(0, -20, 0f, -10, -10, -10, -50, isSuccess: false);

		private ExtortionByDesertersQuestResult _questResultFail2 => new ExtortionByDesertersQuestResult(0, -20, 0f, -10, -10, -10, -50, isSuccess: false);

		private ExtortionByDesertersQuestResult _questResultFail3 => new ExtortionByDesertersQuestResult(0, -50, 0f, -5, -10, -10, -50, isSuccess: false);

		private ExtortionByDesertersQuestResult _questResultTimeOut => new ExtortionByDesertersQuestResult(0, -20, 0f, -10, 0, 0, 0, isSuccess: false);

		private ExtortionByDesertersQuestResult _questResultCancel1 => new ExtortionByDesertersQuestResult(0, 0, 0f, 0, 0, 0, 0, isSuccess: false);

		private ExtortionByDesertersQuestResult _questResultCancel2 => new ExtortionByDesertersQuestResult(0, 0, 0f, 0, 0, 0, 0, isSuccess: false);

		private TextObject OnQuestStartedLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=pWxXvtXP}{QUEST_GIVER.LINK}, a headman of {QUEST_SETTLEMENT} told you that a group of deserters have been raiding their village regularly. {?QUEST_GIVER.GENDER}She{?}He{\\?} asked you to wait in {QUEST_SETTLEMENT} until the deserters arrive...");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_SETTLEMENT", QuestSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject OnQuestSucceededLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=IXkgPlKR}You have defeated the deserters and saved the people of {QUEST_SETTLEMENT}.");
				textObject.SetTextVariable("QUEST_SETTLEMENT", QuestSettlement.Name);
				return textObject;
			}
		}

		private TextObject OnQuestFailed1LogText => new TextObject("{=bdWc1VEl}You've lost track of the deserter party.");

		private TextObject OnQuestFailed2LogText => new TextObject("{=oYJCP3mt}You've failed to stop the deserters. The deserters ravaged the village and left.");

		private TextObject OnQuestFailed3LogText
		{
			get
			{
				TextObject textObject = new TextObject("{=GLuN0dZg}Your criminal action towards {QUEST_SETTLEMENT} has angered {QUEST_GIVER.LINK} and {?QUEST_GIVER.GENDER}she{?}he{\\?} broke off the agreement that you had.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_SETTLEMENT", QuestSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject OnQuestTimedOutLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=YjxCkglX}You've failed to complete this quest in time. Your agreement with {QUEST_GIVER.LINK} was canceled.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject OnQuestCancel1LogText
		{
			get
			{
				TextObject textObject = new TextObject("{=x346Rqle}Your clan is now at war with the {QUEST_GIVER.LINK}'s lord. Your agreement was canceled.");
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

		private TextObject OnQuestCancel2LogText
		{
			get
			{
				TextObject textObject = new TextObject("{=wTx2MNIJ}{QUEST_SETTLEMENT} was raided. {QUEST_GIVER.LINK} can no longer fulfill your contract. Agreement was canceled.");
				textObject.SetTextVariable("QUEST_SETTLEMENT", QuestSettlement.Name);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject OnDeserterPartyDefeatedLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=sRBvUj6U}The deserter party is defeated. Return back to {QUEST_GIVER.LINK} to claim your rewards.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject OnPlayerLeftQuestSettlementNotificationText
		{
			get
			{
				TextObject textObject = new TextObject("{=qjuiWN4K}{PLAYER.NAME}, you should wait with us in the village to ambush the deserters!");
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
				return textObject;
			}
		}

		private TextObject OnPlayerDefeatedDesertersNotificationText
		{
			get
			{
				TextObject textObject = new TextObject("{=EfZaCzb0}{PLAYER.NAME}, please return back to {QUEST_SETTLEMENT} to claim your rewards.");
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
				textObject.SetTextVariable("QUEST_SETTLEMENT", QuestSettlement.Name);
				return textObject;
			}
		}

		private TextObject OnDesertersNoticedPlayerNotificationText => new TextObject("{=9vzm2j5T}Deserters have noticed our presence, they are running away!");

		private DialogFlow QuestCompletionDialogFlow => DialogFlow.CreateDialogFlow("start", 125).NpcLine("{=SCaWkKF1}Here is what we've promised, {GOLD_REWARD}{GOLD_ICON} denars. I hope this makes it worth the blood spilled.[if:convo_normal][ib:hip]").Condition(delegate
		{
			MBTextManager.SetTextVariable("GOLD_REWARD", RewardGold);
			return Hero.OneToOneConversationHero == base.QuestGiver && _currentState == ExtortionByDesertersQuestState.DesertersAreDefeated;
		})
			.BeginPlayerOptions()
			.PlayerOption("{=Bb3oHQNa}Thanks, this will come in handy.")
			.NpcLine("{=khIuyBAi}Thank you for your help. Farewell.")
			.Consequence(delegate
			{
				ApplyQuestResult(_questResultSuccess1);
				AddLog(OnQuestSucceededLogText);
				CompleteQuestWithSuccess();
			})
			.CloseDialog()
			.PlayerOption("{=xcyr5Oq2}Half of the coin is enough for our needs.")
			.NpcLine("{=SVrCpvpZ}Thank you {PLAYER.NAME}. Our people are grateful to you for what you have done today. Farewell.[if:convo_relaxed_happy]")
			.Condition(() => true)
			.Consequence(delegate
			{
				ApplyQuestResult(_questResultSuccess2);
				AddLog(OnQuestSucceededLogText);
				CompleteQuestWithSuccess();
			})
			.CloseDialog()
			.PlayerOption("{=52GFRUTE}Keep your coin, headman. Your people's blessings are enough.")
			.NpcLine("{=1l5dKk1c}Oh, thank you {PLAYER.NAME}. You will always be remembered by our people. Farewell.[if:convo_merry]")
			.Condition(() => true)
			.Consequence(delegate
			{
				ApplyQuestResult(_questResultSuccess3);
				AddLog(OnQuestSucceededLogText);
				CompleteQuestWithSuccess();
			})
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog();

		private DialogFlow DeserterPartyAmbushedDialogFlow => DialogFlow.CreateDialogFlow("start", 125).NpcLine("{=s2btPjJf}Who the hell are you? If you live in this village, you'd better rustle up some silver and wine. Look lively, eh?[if:convo_confused_annoyed][ib:warrior]").Condition(() => _deserterMobileParty != null && _deserterMobileParty.IsActive && CharacterObject.OneToOneConversationCharacter == ConversationHelper.GetConversationCharacterPartyLeader(_deserterMobileParty.Party) && _deserterMobileParty.Position.Distance(QuestSettlement.Position) <= Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius * 1.75f)
			.PlayerLine("{=Pp3koSqA}This time you'll have to fight for it!")
			.CloseDialog();

		private int DeserterPartyMenCount => 24 + TaleWorlds.Library.MathF.Ceiling(24f * _questDifficultyMultiplier);

		private int DefenderPartyMenCount => 16 + TaleWorlds.Library.MathF.Ceiling(16f * _questDifficultyMultiplier);

		public Settlement QuestSettlement => base.QuestGiver.CurrentSettlement;

		public override bool IsRemainingTimeHidden => false;

		internal static void AutoGeneratedStaticCollectObjectsExtortionByDesertersIssueQuest(object o, List<object> collectedObjects)
		{
			((ExtortionByDesertersIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_deserterMobileParty);
			collectedObjects.Add(_defenderMobileParty);
			CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(_desertersRunAwayTimeoutTime, collectedObjects);
		}

		internal static object AutoGeneratedGetMemberValue_deserterMobileParty(object o)
		{
			return ((ExtortionByDesertersIssueQuest)o)._deserterMobileParty;
		}

		internal static object AutoGeneratedGetMemberValue_defenderMobileParty(object o)
		{
			return ((ExtortionByDesertersIssueQuest)o)._defenderMobileParty;
		}

		internal static object AutoGeneratedGetMemberValue_currentState(object o)
		{
			return ((ExtortionByDesertersIssueQuest)o)._currentState;
		}

		internal static object AutoGeneratedGetMemberValue_questDifficultyMultiplier(object o)
		{
			return ((ExtortionByDesertersIssueQuest)o)._questDifficultyMultiplier;
		}

		internal static object AutoGeneratedGetMemberValue_deserterBattleFinalizedForTheFirstTime(object o)
		{
			return ((ExtortionByDesertersIssueQuest)o)._deserterBattleFinalizedForTheFirstTime;
		}

		internal static object AutoGeneratedGetMemberValue_desertersRunAwayTimeoutTime(object o)
		{
			return ((ExtortionByDesertersIssueQuest)o)._desertersRunAwayTimeoutTime;
		}

		public ExtortionByDesertersIssueQuest(string questId, Hero questGiver, float difficultyMultiplier, int rewardGold, CampaignTime duration)
			: base(questId, questGiver, duration, rewardGold)
		{
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
			_questDifficultyMultiplier = difficultyMultiplier;
			_defenderMobileParty = null;
			_deserterBattleFinalizedForTheFirstTime = false;
			_playerAwayFromSettlementNotificationSent = false;
			_desertersRunAwayTimeoutTime = CampaignTime.Never;
			CreateDeserterParty();
			_currentState = ExtortionByDesertersQuestState.DesertersMovingToSettlement;
			AddTrackedObject(_deserterMobileParty);
			SetDialogs();
			InitializeQuestOnCreation();
		}

		protected override void InitializeQuestOnGameLoad()
		{
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
			_playerAwayFromSettlementNotificationSent = false;
			if (_currentState == ExtortionByDesertersQuestState.DesertersMovingToSettlement)
			{
				float num = _deserterMobileParty.Position.Distance(MobileParty.MainParty.Position);
				bool flag = PlayerEncounter.Current != null && PlayerEncounter.EncounterSettlement == QuestSettlement;
				bool flag2 = num <= _deserterMobileParty.SeeingRange * 0.8f;
				_playerAwayFromSettlementNotificationSent = !flag && !flag2;
			}
			SetDialogs();
			Campaign.Current.ConversationManager.AddDialogFlow(QuestCompletionDialogFlow, this);
			Campaign.Current.ConversationManager.AddDialogFlow(DeserterPartyAmbushedDialogFlow, this);
		}

		protected override void SetDialogs()
		{
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine("{=PnRVabwv}Thank you. Just wait in the village. We'll stand lookout and lure them into your ambush. Just wait for the signal.").Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.Consequence(OnQuestAccepted)
				.CloseDialog();
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine("{=iN1kBsac}I don't think they'll be long now. Our hunters have spotted them making ready. Keep waiting.").Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.BeginPlayerOptions()
				.PlayerOption("{=IJihRdfF}Don't worry, we'll be ready for the fight.")
				.NpcLine("{=U0UoayfA}Good, good. Thank you again.")
				.CloseDialog()
				.PlayerOption("{=bcGzpFSg}Are you sure about what your hunters saw? My men are starting to run out of patience.")
				.NpcLine("{=YsASaPKq}I'm sure they'll be here soon. Please don't leave the village, or we'll stand no chance...[if:convo_nervous][ib:nervous]")
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
			QuestCharacterDialogFlow = DialogFlow.CreateDialogFlow("start", 125).NpcLine("{=rAqyKcKZ}Who the hell are you? What do you want from us?[if:convo_confused_annoyed][ib:aggressive]").Condition(() => _deserterMobileParty != null && _deserterMobileParty.IsActive && CharacterObject.OneToOneConversationCharacter == ConversationHelper.GetConversationCharacterPartyLeader(_deserterMobileParty.Party) && _deserterMobileParty.Position.Distance(QuestSettlement.Position) >= 5f)
				.BeginPlayerOptions()
				.PlayerOption("{=Ljs9ahMk}I know your intentions. I will not let you steal from those poor villagers!")
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
		}

		private void OnQuestAccepted()
		{
			StartQuest();
			Campaign.Current.ConversationManager.AddDialogFlow(QuestCompletionDialogFlow, this);
			Campaign.Current.ConversationManager.AddDialogFlow(DeserterPartyAmbushedDialogFlow, this);
			AddLog(OnQuestStartedLogText);
		}

		private void ApplyQuestResult(in ExtortionByDesertersQuestResult result)
		{
			int num = (int)(result.GoldMultiplier * (float)RewardGold);
			if (num > 0)
			{
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, num);
			}
			if (result.QuestGiverPowerChange != 0)
			{
				base.QuestGiver.AddPower(result.QuestGiverPowerChange);
			}
			if (result.TownSecurityChange != 0)
			{
				QuestSettlement.Village.Bound.Town.Security += result.TownSecurityChange;
			}
			if (result.TownProsperityChange != 0)
			{
				QuestSettlement.Village.Bound.Town.Prosperity += result.TownProsperityChange;
			}
			if (result.HonorChange != 0)
			{
				if (result.IsSuccess)
				{
					TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
					{
						new Tuple<TraitObject, int>(DefaultTraits.Honor, result.HonorChange)
					});
				}
				else
				{
					TraitLevelingHelper.OnIssueFailed(base.QuestGiver, new Tuple<TraitObject, int>[1]
					{
						new Tuple<TraitObject, int>(DefaultTraits.Honor, result.HonorChange)
					});
				}
			}
			if (result.QuestGiverRelationChange != 0)
			{
				RelationshipChangeWithQuestGiver = result.QuestGiverRelationChange;
			}
			if (result.RenownChange > 0)
			{
				GainRenownAction.Apply(Hero.MainHero, result.RenownChange);
			}
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, MapEventEnded);
			CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, GameMenuOpened);
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
			CampaignEvents.GameMenuOptionSelectedEvent.AddNonSerializedListener(this, GameMenuOptionSelected);
			CampaignEvents.VillageBeingRaided.AddNonSerializedListener(this, OnVillageBeingRaided);
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
			CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
			CampaignEvents.BattleStarted.AddNonSerializedListener(this, OnBattleStarted);
		}

		private void OnBattleStarted(PartyBase attackerParty, PartyBase defenderParty, object subject, bool showNotification)
		{
			if (attackerParty == _deserterMobileParty?.Party && attackerParty.MapEvent != null && attackerParty.MapEvent.IsRaid && attackerParty.MapEvent.DefenderSide.LeaderParty == QuestSettlement.Party)
			{
				StartAmbushEncounter();
			}
		}

		private void OnSettlementLeft(MobileParty party, Settlement settlement)
		{
			if (party.IsMainParty && settlement == QuestSettlement && settlement.Party.MapEvent != null && settlement.Party.MapEvent.IsRaid && settlement.Party.MapEvent.AttackerSide.LeaderParty == _deserterMobileParty.Party)
			{
				ApplyQuestResult(_questResultFail2);
				CompleteQuestWithFail(OnQuestFailed2LogText);
			}
		}

		private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			if (attackerParty == PartyBase.MainParty)
			{
				if (mapEvent.IsFieldBattle && defenderParty.IsMobile && defenderParty.MobileParty.HomeSettlement == QuestSettlement && defenderParty != _deserterMobileParty.Party)
				{
					CompleteQuestWithFail(OnQuestFailed3LogText);
					ApplyQuestResult(_questResultFail3);
				}
				else if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
				{
					QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
				}
			}
		}

		protected override void OnFinalize()
		{
			DestroyDefenderParty();
			ReleaseDeserterParty();
		}

		private void TickDesertersPartyLogic()
		{
			if ((_deserterMobileParty != null && _deserterMobileParty.MapEvent != null) || !base.IsOngoing)
			{
				return;
			}
			switch (_currentState)
			{
			case ExtortionByDesertersQuestState.DesertersMovingToSettlement:
			{
				float num = _deserterMobileParty.Position.Distance(MobileParty.MainParty.Position);
				bool num2 = PlayerEncounter.Current != null && PlayerEncounter.EncounterSettlement == QuestSettlement;
				bool flag = num <= _deserterMobileParty.SeeingRange * 0.8f;
				if (!num2)
				{
					if (flag)
					{
						MBInformationManager.AddQuickInformation(OnDesertersNoticedPlayerNotificationText, 0, Hero.MainHero.CharacterObject);
						HandleDesertersRunningAway();
						_currentState = ExtortionByDesertersQuestState.DesertersRunningAwayFromPlayer;
						_desertersRunAwayTimeoutTime = CampaignTime.HoursFromNow(10f);
					}
					else if (!_playerAwayFromSettlementNotificationSent)
					{
						MBInformationManager.AddQuickInformation(OnPlayerLeftQuestSettlementNotificationText, 0, base.QuestGiver.CharacterObject);
						_playerAwayFromSettlementNotificationSent = true;
					}
				}
				else if (_deserterMobileParty.DefaultBehavior != AiBehavior.RaidSettlement)
				{
					SetPartyAiAction.GetActionForRaidingSettlement(_deserterMobileParty, QuestSettlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
				}
				break;
			}
			case ExtortionByDesertersQuestState.DesertersRunningAwayFromPlayer:
				if (_deserterMobileParty.Position.Distance(MobileParty.MainParty.Position) > MobileParty.MainParty.SeeingRange + 3f)
				{
					ApplyQuestResult(_questResultFail1);
					CompleteQuestWithFail(OnQuestFailed1LogText);
				}
				else if (_desertersRunAwayTimeoutTime.IsPast)
				{
					DestroyDeserterParty();
					ApplyQuestResult(_questResultFail1);
					CompleteQuestWithFail(OnQuestFailed1LogText);
				}
				else
				{
					HandleDesertersRunningAway();
				}
				break;
			case ExtortionByDesertersQuestState.DesertersDefeatedPlayer:
				if (_deserterMobileParty.DefaultBehavior != AiBehavior.RaidSettlement)
				{
					SetPartyAiAction.GetActionForRaidingSettlement(_deserterMobileParty, QuestSettlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
				}
				break;
			}
		}

		protected override void HourlyTick()
		{
			TickDesertersPartyLogic();
		}

		private void MapEventEnded(MapEvent mapEvent)
		{
			if (!mapEvent.IsPlayerMapEvent || _deserterMobileParty == null || !mapEvent.InvolvedParties.Contains(_deserterMobileParty.Party))
			{
				return;
			}
			_deserterBattleFinalizedForTheFirstTime = true;
			if (mapEvent.WinningSide == mapEvent.PlayerSide)
			{
				AddLog(OnDeserterPartyDefeatedLogText);
				if (!IsTracked(base.QuestGiver))
				{
					AddTrackedObject(base.QuestGiver);
				}
				if (!IsTracked(QuestSettlement))
				{
					AddTrackedObject(QuestSettlement);
				}
				MBInformationManager.AddQuickInformation(OnPlayerDefeatedDesertersNotificationText, 0, base.QuestGiver.CharacterObject);
				_currentState = ExtortionByDesertersQuestState.DesertersAreDefeated;
			}
			else
			{
				if (_deserterMobileParty.DefaultBehavior != AiBehavior.RaidSettlement)
				{
					SetPartyAiAction.GetActionForRaidingSettlement(_deserterMobileParty, QuestSettlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
				}
				_currentState = ExtortionByDesertersQuestState.DesertersDefeatedPlayer;
			}
		}

		private void GameMenuOpened(MenuCallbackArgs mArgs)
		{
			if (mArgs.MenuContext.GameMenu.StringId == "encounter" && _deserterBattleFinalizedForTheFirstTime)
			{
				_deserterBattleFinalizedForTheFirstTime = false;
				if (_currentState == ExtortionByDesertersQuestState.DesertersAreDefeated)
				{
					DestroyDeserterParty();
				}
				DestroyDefenderParty();
			}
		}

		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (base.QuestGiver.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				CompleteQuestWithCancel(OnQuestCancel1LogText);
				ApplyQuestResult(_questResultCancel1);
			}
		}

		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
		{
			QuestHelper.CheckWarDeclarationAndFailOrCancelTheQuest(this, faction1, faction2, detail, PlayerDeclaredWarQuestLogText, OnQuestCancel1LogText);
		}

		private void OnVillageBeingRaided(Village village)
		{
			if (village.Settlement != QuestSettlement)
			{
				return;
			}
			if (village.Settlement.Party.MapEvent.AttackerSide.LeaderParty == _deserterMobileParty.Party)
			{
				if (PlayerEncounter.Current == null || PlayerEncounter.EncounterSettlement != QuestSettlement)
				{
					ApplyQuestResult(_questResultFail2);
					CompleteQuestWithFail(OnQuestFailed2LogText);
				}
			}
			else if (village.Settlement.Party.MapEvent.AttackerSide.LeaderParty == PartyBase.MainParty)
			{
				ApplyQuestResult(_questResultFail3);
				CompleteQuestWithFail(OnQuestFailed3LogText);
			}
			else
			{
				ApplyQuestResult(_questResultCancel2);
				CompleteQuestWithCancel(OnQuestCancel2LogText);
			}
		}

		private void GameMenuOptionSelected(GameMenu gameMenu, GameMenuOption gameMenuOption)
		{
			if (gameMenuOption.IsLeave)
			{
				TickDesertersPartyLogic();
			}
		}

		private void OnGameLoadFinished()
		{
			if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.1.0")))
			{
				_desertersRunAwayTimeoutTime = CampaignTime.Never;
				if (_currentState == ExtortionByDesertersQuestState.DesertersRunningAwayFromPlayer)
				{
					_desertersRunAwayTimeoutTime = CampaignTime.HoursFromNow(10f);
				}
			}
		}

		protected override void OnTimedOut()
		{
			ApplyQuestResult(_questResultTimeOut);
			AddLog(OnQuestTimedOutLogText);
		}

		private void CreateDeserterParty()
		{
			float seeingRange = MobileParty.MainParty.SeeingRange;
			float maxDistance = seeingRange + 3f;
			CampaignVec2 initialPosition = NavigationHelper.FindReachablePointAroundPosition(MobileParty.MainParty.Position, MobileParty.NavigationType.Default, maxDistance, seeingRange);
			Clan clan = Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "deserters");
			if (clan != null)
			{
				_deserterMobileParty = BanditPartyComponent.CreateLooterParty("ebdi_deserters_party_1_new", clan, QuestSettlement, isBossParty: false, null, initialPosition);
			}
			else
			{
				Hideout hideout = SettlementHelper.FindNearestHideoutToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.Default);
				Clan clan2 = null;
				if (hideout != null)
				{
					CultureObject banditCulture = hideout.Settlement.Culture;
					clan2 = Clan.BanditFactions.FirstOrDefault((Clan x) => x.Culture == banditCulture);
				}
				if (clan2 == null)
				{
					clan2 = Clan.All.GetRandomElementWithPredicate((Clan x) => x.IsBanditFaction);
				}
				_deserterMobileParty = BanditPartyComponent.CreateBanditParty("ebdi_deserters_party_1", clan2, hideout, isBossParty: false, null, initialPosition);
				TextObject customName = new TextObject("{=zT2b0v8y}Deserters Party");
				_deserterMobileParty.Party.SetCustomName(customName);
			}
			PartyTemplateObject defaultPartyTemplate = QuestSettlement.Culture.DefaultPartyTemplate;
			MobilePartyHelper.FillPartyManuallyAfterCreation(_deserterMobileParty, defaultPartyTemplate, DeserterPartyMenCount);
			int num = 0;
			foreach (TroopRosterElement item in _deserterMobileParty.MemberRoster.GetTroopRoster())
			{
				if (!item.Character.HasMount())
				{
					num += item.Number;
				}
			}
			ItemObject itemObject = Items.All.GetRandomElementWithPredicate((ItemObject x) => x.IsMountable && x.Culture == QuestSettlement.Culture && !x.NotMerchandise && x.Tier == ItemObject.ItemTiers.Tier2);
			if (itemObject == null)
			{
				itemObject = MBObjectManager.Instance.GetObject<ItemObject>("vlandia_horse") ?? MBObjectManager.Instance.GetObject<ItemObject>("sumpter_horse");
			}
			if (itemObject != null)
			{
				_deserterMobileParty.ItemRoster.AddToCounts(itemObject, num);
			}
			float num2 = TaleWorlds.Library.MathF.Abs(_deserterMobileParty.FoodChange);
			int num3 = TaleWorlds.Library.MathF.Ceiling(base.QuestDueTime.RemainingDaysFromNow * num2);
			int num4 = num3 / 2;
			_deserterMobileParty.ItemRoster.AddToCounts(DefaultItems.Grain, num4);
			int number = num3 - num4;
			_deserterMobileParty.ItemRoster.AddToCounts(DefaultItems.Meat, number);
			_deserterMobileParty.SetPartyUsedByQuest(isActivelyUsed: true);
			_deserterMobileParty.IgnoreByOtherPartiesTill(CampaignTime.Never);
			_deserterMobileParty.Aggressiveness = 0f;
			_deserterMobileParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
			_deserterMobileParty.Party.SetVisualAsDirty();
			SetPartyAiAction.GetActionForRaidingSettlement(_deserterMobileParty, QuestSettlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
		}

		private void DestroyDeserterParty()
		{
			if (_deserterMobileParty != null && _deserterMobileParty.IsActive)
			{
				DestroyPartyAction.Apply(null, _deserterMobileParty);
				_deserterMobileParty = null;
			}
		}

		private void ReleaseDeserterParty()
		{
			if (_deserterMobileParty != null && _deserterMobileParty.IsActive)
			{
				_deserterMobileParty.SetPartyUsedByQuest(isActivelyUsed: false);
				_deserterMobileParty.IgnoreByOtherPartiesTill(CampaignTime.HoursFromNow(0f));
				_deserterMobileParty.Aggressiveness = 1f;
				if (_deserterMobileParty.MapEvent != null)
				{
					_deserterMobileParty.MapEvent.FinalizeEvent();
				}
				if (_deserterMobileParty.CurrentSettlement != null)
				{
					LeaveSettlementAction.ApplyForParty(_deserterMobileParty);
				}
				_deserterMobileParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: false);
				_deserterMobileParty.Party.SetCustomName(new TextObject(_deserterMobileParty.ActualClan.StringId + "_1"));
				_deserterMobileParty.Party.SetVisualAsDirty();
			}
		}

		private void CreateDefenderParty()
		{
			TextObject textObject = new TextObject("{=dPU8UbKA}{QUEST_GIVER}'s Party");
			textObject.SetTextVariable("QUEST_GIVER", base.QuestGiver.Name);
			_defenderMobileParty = CustomPartyComponent.CreateCustomPartyWithTroopRoster(QuestSettlement.GatePosition, 1f, base.QuestGiver.CurrentSettlement, textObject, base.QuestGiver.CurrentSettlement.OwnerClan, TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), base.QuestGiver);
			MobilePartyHelper.FillPartyManuallyAfterCreation(_defenderMobileParty, QuestSettlement.Culture.MilitiaPartyTemplate, DefenderPartyMenCount);
			_defenderMobileParty.SetPartyUsedByQuest(isActivelyUsed: true);
			_defenderMobileParty.Aggressiveness = 1f;
			_defenderMobileParty.ShouldJoinPlayerBattles = true;
		}

		private void DestroyDefenderParty()
		{
			if (_defenderMobileParty != null && _defenderMobileParty.IsActive)
			{
				DestroyPartyAction.Apply(null, _defenderMobileParty);
				_defenderMobileParty = null;
			}
		}

		private void HandleDesertersRunningAway()
		{
			Vec2 vec = (_deserterMobileParty.Position - MobileParty.MainParty.Position).ToVec2();
			vec.Normalize();
			float num = _deserterMobileParty.Speed * 1.5f;
			CampaignVec2 point = _deserterMobileParty.Position + vec * num;
			point = FindFreePositionBetweenPointAndParty(_deserterMobileParty, in point, out var distance);
			PathFaceRecord face = point.Face;
			float angleInRadians = 0.34906584f;
			float distance2;
			for (int i = 0; i < 10; i++)
			{
				if (face.FaceIndex != -1 && face.FaceIslandIndex != -1)
				{
					break;
				}
				vec.RotateCCW(angleInRadians);
				vec.Normalize();
				point = FindFreePositionBetweenPointAndParty(_deserterMobileParty, _deserterMobileParty.Position + vec * num, out distance2);
				face = point.Face;
			}
			if (distance <= 1E-05f)
			{
				vec.RotateCCW(-System.MathF.PI / 2f + (float)MBRandom.RandomInt(0, 2) * System.MathF.PI);
				point = _deserterMobileParty.Position + vec * num;
				point = FindFreePositionBetweenPointAndParty(_deserterMobileParty, in point, out distance2);
			}
			_deserterMobileParty.SetMoveGoToPoint(point, MobileParty.NavigationType.Default);
		}

		private void StartAmbushEncounter()
		{
			PlayerEncounter.Finish();
			PlayerEncounter.Start();
			EnterSettlementAction.ApplyForParty(MobileParty.MainParty, QuestSettlement);
			PlayerEncounter.Current.Init(_deserterMobileParty.Party, QuestSettlement.Party, QuestSettlement);
			CreateDefenderParty();
			_defenderMobileParty.MapEventSide = MobileParty.MainParty.MapEventSide;
			_deserterMobileParty.IgnoreByOtherPartiesTill(CampaignTime.Now - CampaignTime.Hours(1f));
			if (QuestSettlement.MilitiaPartyComponent != null)
			{
				QuestSettlement.MilitiaPartyComponent.MobileParty.MapEventSide = null;
			}
			CharacterObject conversationCharacterPartyLeader = ConversationHelper.GetConversationCharacterPartyLeader(_deserterMobileParty.Party);
			ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, noHorse: true);
			ConversationCharacterData conversationPartnerData = new ConversationCharacterData(conversationCharacterPartyLeader, _deserterMobileParty.Party, noHorse: true);
			GameMenu.ActivateGameMenu("encounter_meeting");
			CampaignMapConversation.OpenConversation(playerCharacterData, conversationPartnerData);
		}

		private CampaignVec2 FindFreePositionBetweenPointAndParty(MobileParty party, in CampaignVec2 point, out float distance, float maxIterations = 10f, float acceptThres = 1E-05f, float maxPathDistance = 1000f, float euclideanThressholdFactor = 1.5f)
		{
			_ = Campaign.Current.MapSceneWrapper;
			CampaignVec2 position = party.Position;
			PathFaceRecord currentNavigationFace = party.CurrentNavigationFace;
			CampaignVec2 campaignVec = position;
			distance = 0f;
			if (!NavigationHelper.IsPositionValidForNavigationType(position, MobileParty.NavigationType.Default))
			{
				Debug.FailedAssert("Origin point not valid!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Issues\\ExtortionByDesertersIssueBehavior.cs", "FindFreePositionBetweenPointAndParty", 1397);
			}
			else
			{
				CampaignVec2 campaignVec2 = point;
				PathFaceRecord face = campaignVec2.Face;
				CampaignVec2 campaignVec3 = point;
				float num = acceptThres * acceptThres;
				for (int i = 0; (float)i < maxIterations; i++)
				{
					if (!(campaignVec.DistanceSquared(point) > num))
					{
						break;
					}
					if (!face.IsValid())
					{
						break;
					}
					float num2 = position.Distance(campaignVec2);
					int[] invalidTerrainTypesForNavigationType = Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(MobileParty.NavigationType.Default);
					float distance2;
					bool pathDistanceBetweenAIFaces = Campaign.Current.MapSceneWrapper.GetPathDistanceBetweenAIFaces(currentNavigationFace, face, position.ToVec2(), campaignVec2.ToVec2(), 0.3f, maxPathDistance, out distance2, invalidTerrainTypesForNavigationType, Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromLandToSea, Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromSeaToLand);
					bool flag = distance2 < num2 * euclideanThressholdFactor;
					if (pathDistanceBetweenAIFaces && flag)
					{
						campaignVec = campaignVec2;
						distance = num2;
						campaignVec2 = (campaignVec2 + campaignVec3) * 0.5f;
					}
					else
					{
						campaignVec3 = campaignVec2;
						campaignVec2 = (campaignVec + campaignVec3) * 0.5f;
					}
					face = campaignVec2.Face;
				}
			}
			return campaignVec;
		}
	}

	public class ExtortionByDesertersIssueBehaviorTypeDefiner : SaveableTypeDefiner
	{
		public ExtortionByDesertersIssueBehaviorTypeDefiner()
			: base(490000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(ExtortionByDesertersIssue), 1);
			AddClassDefinition(typeof(ExtortionByDesertersIssueQuest), 2);
		}

		protected override void DefineEnumTypes()
		{
			AddEnumDefinition(typeof(ExtortionByDesertersIssueQuest.ExtortionByDesertersQuestState), 3);
		}
	}

	private const IssueBase.IssueFrequency ExtortionByDesertersIssueFrequency = IssueBase.IssueFrequency.Common;

	private void OnCheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero))
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnIssueSelected, typeof(ExtortionByDesertersIssue), IssueBase.IssueFrequency.Common));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(ExtortionByDesertersIssue), IssueBase.IssueFrequency.Common));
		}
	}

	private IssueBase OnIssueSelected(in PotentialIssueData pid, Hero issueOwner)
	{
		return new ExtortionByDesertersIssue(issueOwner);
	}

	private bool ConditionsHold(Hero issueGiver)
	{
		if (issueGiver.IsHeadman)
		{
			Settlement currentSettlement = issueGiver.CurrentSettlement;
			if (currentSettlement.IsVillage && currentSettlement.Culture != null)
			{
				Town town = currentSettlement.Village.Bound?.Town;
				if (town != null)
				{
					return town.Security <= 50f;
				}
			}
		}
		return false;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
