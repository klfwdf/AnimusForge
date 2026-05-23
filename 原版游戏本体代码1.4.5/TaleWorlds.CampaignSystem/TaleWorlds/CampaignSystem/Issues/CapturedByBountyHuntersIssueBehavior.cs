using System;
using System.Collections.Generic;
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
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues;

public class CapturedByBountyHuntersIssueBehavior : CampaignBehaviorBase
{
	public class CapturedByBountyHuntersIssue : IssueBase
	{
		private const int QuestSolutionNeededMinimumHealthyMenCount = 25;

		private const int CompanionRequiredSkillLevel = 120;

		private const int IssueDuration = 15;

		private const int QuestTimeLimit = 30;

		private const int AlternativeSolutionMinimumTroopTier = 2;

		[SaveableField(100)]
		private Settlement _hideout;

		public override int AlternativeSolutionBaseNeededMenCount => 10;

		protected override int AlternativeSolutionBaseDurationInDaysInternal => Math.Max(5, 4 + TaleWorlds.Library.MathF.Ceiling(6f * base.IssueDifficultyMultiplier));

		protected override int RewardGold => 3000;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.Casualties | AlternativeSolutionScaleFlag.FailureRisk;

		public override TextObject IssueBriefByIssueGiver => new TextObject("{=QtmPWQ5a}Some of my lads have gone missing. I've got a witness who says they'd gotten themselves dead drunk drinking with another band in these parts who turned out to be filthy bounty hunters. Now my boys are all trussed up, and these treacherous animals aim to turn them in for the bounty.[if:convo_annoyed][ib:closed]");

		public override TextObject IssueAcceptByPlayer => new TextObject("{=A6iOIurY}How can I help you?");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=MiVYmiBc}Raid the bounty hunters' hideout and rescue my associates from them. I will make it worth your while, say {GOLD_AMOUNT} denars.[if:convo_mocking_revenge][ib:closed2]");
				textObject.SetTextVariable("GOLD_AMOUNT", RewardGold);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=GIkvhuCC}Maybe one of your men who knows a thing or two about scouting, with {TROOP_AMOUNT} good men can deal with these scum. So what do you say?[if:convo_undecided_open]");
				textObject.SetTextVariable("TROOP_AMOUNT", GetTotalAlternativeSolutionNeededMenCount());
				return textObject;
			}
		}

		public override TextObject IssueQuestSolutionAcceptByPlayer => new TextObject("{=cvWxXGo5}I can do the job.");

		public override TextObject IssueAlternativeSolutionAcceptByPlayer => new TextObject("{=AvBNKK5y}Alright, I will have one of my companions go and rescue your associates.");

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver => new TextObject("{=9u9OEZ9Y}Splendid. My men will guide your companion to the hideout.");

		public override TextObject IssueDiscussAlternativeSolution => new TextObject("{=zwNjgdbi}My boys are getting ready for the battle. I'm pretty sure your men will tip the balance of that fight in our favor. Thank you.");

		public override bool IsThereAlternativeSolution => true;

		public override bool IsThereLordSolution => false;

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=TQyB9rAs}{ISSUE_OWNER.NAME}'s Associates Captured by Bounty Hunters");
				StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject Description
		{
			get
			{
				TextObject textObject = new TextObject("{=HhTTzgLj}{ISSUE_OWNER.LINK}, a gang leader in {ISSUE_SETTLEMENT}, wants us to raid some bounty hunters' hideout and rescue {?ISSUE_OWNER.GENDER}her{?}his{\\?} associates.");
				StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.IssueOwner.CharacterObject, textObject);
				textObject.SetTextVariable("ISSUE_SETTLEMENT", base.IssueOwner.CurrentSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		protected override int CompanionSkillRewardXP => (int)(750f + 1000f * base.IssueDifficultyMultiplier);

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=U7sTASN4}{ISSUE_OWNER.LINK}, a gang leader from {QUEST_SETTLEMENT}, has told you that some bounty hunters captured some of {?ISSUE_OWNER.GENDER}her{?}his{\\?} gang members and are holding them in their hideout. {?ISSUE_OWNER.GENDER}She{?}He{\\?} wants them found and rescued. You agreed to send {TROOP_COUNT} of your men along with a {COMPANION.LINK} to find these bounty hunters and rescue {?ISSUE_OWNER.GENDER}her{?}his{\\?} associates. They should be back in {RETURN_DAYS} days.");
				StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_SETTLEMENT", base.IssueOwner.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				textObject.SetTextVariable("TROOP_COUNT", AlternativeSolutionSentTroops.TotalManCount - 1);
				return textObject;
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsCapturedByBountyHuntersIssue(object o, List<object> collectedObjects)
		{
			((CapturedByBountyHuntersIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_hideout);
		}

		internal static object AutoGeneratedGetMemberValue_hideout(object o)
		{
			return ((CapturedByBountyHuntersIssue)o)._hideout;
		}

		public CapturedByBountyHuntersIssue(Hero issueOwner, Settlement hideout)
			: base(issueOwner, CampaignTime.DaysFromNow(15f))
		{
			_hideout = hideout;
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.SettlementSecurity)
			{
				return 1f;
			}
			if (issueEffect == DefaultIssueEffects.IssueOwnerPower)
			{
				return -0.2f;
			}
			return 0f;
		}

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			return ((hero.GetSkillValue(DefaultSkills.Scouting) >= hero.GetSkillValue(DefaultSkills.Riding)) ? DefaultSkills.Scouting : DefaultSkills.Riding, 120);
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
			RelationshipChangeWithIssueOwner = 5;
			base.IssueOwner.AddPower(10f);
			base.IssueOwner.CurrentSettlement.Town.Security -= 5f;
		}

		protected override void AlternativeSolutionEndWithFailureConsequence()
		{
			TraitLevelingHelper.OnIssueFailed(base.IssueOwner, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -10)
			});
			RelationshipChangeWithIssueOwner = -5;
			base.IssueOwner.AddPower(-10f);
			base.IssueOwner.CurrentSettlement.Town.Security += 5f;
		}

		public override IssueFrequency GetFrequency()
		{
			return IssueFrequency.Common;
		}

		public override bool IssueStayAliveConditions()
		{
			return _hideout.Hideout.IsInfested;
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
			if (MobileParty.MainParty.MemberRoster.TotalHealthyCount - 1 < 25)
			{
				flag |= PreconditionFlags.NotEnoughTroops;
			}
			return flag == PreconditionFlags.None;
		}

		protected override void OnGameLoad()
		{
		}

		protected override void HourlyTick()
		{
		}

		public override void IsSettlementBusy(Settlement settlement, object asker, ref int priority)
		{
			if (asker != this && settlement == _hideout)
			{
				priority = Math.Max(priority, 100);
			}
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new CapturedByBountyHuntersIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(30f), RewardGold, _hideout);
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}

		protected override void OnIssueFinalized()
		{
		}
	}

	public class CapturedByBountyHuntersIssueQuest : QuestBase
	{
		[SaveableField(102)]
		private Settlement _questHideout;

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=TQyB9rAs}{ISSUE_OWNER.NAME}'s Associates Captured by Bounty Hunters");
				StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		public override bool IsRemainingTimeHidden => false;

		private TextObject PlayerStartsQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=P7MZ0hZb}{QUEST_GIVER.LINK}, a gang leader from {QUEST_SETTLEMENT}, has told you that some bounty hunters captured some of {?QUEST_GIVER.GENDER}her{?}his{\\?} gang members and are holding them in their hideout. You told {?QUEST_GIVER.GENDER}her{?}him{\\?} you would find them yourself.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_SETTLEMENT", Settlement.CurrentSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject SuccessQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=rNDRyFP4}You cleared the hideout and rescued the {QUEST_GIVER.LINK}'s associates. {?QUEST_GIVER.GENDER}She{?}He{\\?} sends you the following letter. \"Thank you for rescuing my men. I'll remember this.\"");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject PlayerLostTheFightLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=nq0qLQ1x}You lost the fight against bounty hunters and failed to rescue the {QUEST_GIVER.LINK}'s men. {?QUEST_GIVER.GENDER}She{?}He{\\?} sends you the following letter. \"I appreciate your effort but it wasn't good enough...\"");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject HideoutClearedBySomeoneElseLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=4Bub0GY6}Hideout was cleared by someone else. Your agreement with {QUEST_GIVER.LINK} is canceled.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject TimeOutLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=JPAGzEhe}You failed to rescue the {QUEST_GIVER.LINK}'s men in time. {?QUEST_GIVER.GENDER}She{?}He{\\?} sends you the following letter. \"You sat on your heels doing nothing and my men will pay the price. I won't forget this...\"");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsCapturedByBountyHuntersIssueQuest(object o, List<object> collectedObjects)
		{
			((CapturedByBountyHuntersIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_questHideout);
		}

		internal static object AutoGeneratedGetMemberValue_questHideout(object o)
		{
			return ((CapturedByBountyHuntersIssueQuest)o)._questHideout;
		}

		public CapturedByBountyHuntersIssueQuest(string questId, Hero giverHero, CampaignTime duration, int rewardGold, Settlement hideout)
			: base(questId, giverHero, duration, rewardGold)
		{
			_questHideout = hideout;
			SetDialogs();
			InitializeQuestOnCreation();
		}

		private bool DialogCondition()
		{
			return Hero.OneToOneConversationHero == base.QuestGiver;
		}

		protected override void SetDialogs()
		{
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(new TextObject("{=BUM63VJq}That's the spirit. My men will tell you how to find the hideout. Rescue those poor captives, and a large sack of silver will be on your way![if:convo_approving][ib:hip]")).Condition(DialogCondition)
				.Consequence(QuestAcceptedConsequences)
				.CloseDialog();
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(new TextObject("{=vYCY931w}Any news about my men?")).Condition(DialogCondition)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=DJcMau0U}Not yet. We are still looking for them."))
				.NpcLine(new TextObject("{=VZhs6rpG}Well, try to speed it up. Once the bounty hunters turn them in, it'll be too late."))
				.CloseDialog()
				.PlayerOption(new TextObject("{=LvNTjCtQ}We need more time."))
				.NpcLine(new TextObject("{=15wCjIBY}Take too much time, and my men will swing from the gallows. Speed it along, will you?"))
				.CloseDialog()
				.EndPlayerOptions();
		}

		private void QuestAcceptedConsequences()
		{
			StartQuest();
			AddLog(PlayerStartsQuestLogText);
		}

		protected override void InitializeQuestOnGameLoad()
		{
			SetDialogs();
		}

		protected override void HourlyTick()
		{
		}

		protected override void OnFinalize()
		{
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
			CampaignEvents.IsSettlementBusyEvent.AddNonSerializedListener(this, IsSettlementBusy);
		}

		private void IsSettlementBusy(Settlement settlement, object asker, ref int priority)
		{
			if (asker != this && settlement == _questHideout)
			{
				priority = Math.Max(priority, 200);
			}
		}

		private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.9") && _questHideout.IsSettlementBusy(this))
			{
				CompleteQuestWithCancel();
			}
		}

		private void OnSettlementLeft(MobileParty party, Settlement settlement)
		{
			if (party == MobileParty.MainParty && settlement == base.QuestGiver.CurrentSettlement)
			{
				_questHideout.Hideout.IsSpotted = true;
				_questHideout.IsVisible = true;
				AddTrackedObject(_questHideout);
				TextObject textObject = new TextObject("{=R9R6imnU}Scouts working for {QUEST_GIVER.NAME} marked the hideout on your map");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				MBInformationManager.AddQuickInformation(textObject);
			}
		}

		private void OnMapEventEnded(MapEvent mapEvent)
		{
			if (mapEvent.IsPlayerMapEvent)
			{
				if (mapEvent.MapEventSettlement == _questHideout)
				{
					if (mapEvent.DefeatedSide == mapEvent.PlayerSide || mapEvent.DefeatedSide == BattleSideEnum.None)
					{
						AddLog(PlayerLostTheFightLogText);
						FailConsequences(isTimedOut: false);
					}
					else
					{
						AddLog(SuccessQuestLogText);
						SuccessConsequences();
					}
				}
			}
			else if (_questHideout.Parties.Count == 0)
			{
				AddLog(HideoutClearedBySomeoneElseLogText);
				CompleteQuestWithFail();
			}
		}

		private void SuccessConsequences()
		{
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
			base.QuestGiver.AddPower(10f);
			RelationshipChangeWithQuestGiver = 5;
			if (base.QuestGiver.CurrentSettlement != null && base.QuestGiver.CurrentSettlement.Town != null)
			{
				base.QuestGiver.CurrentSettlement.Town.Security -= 5f;
			}
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 100)
			});
			CompleteQuestWithSuccess();
		}

		private void FailConsequences(bool isTimedOut)
		{
			TraitLevelingHelper.OnIssueFailed(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -10)
			});
			RelationshipChangeWithQuestGiver = -5;
			base.QuestGiver.AddPower(-10f);
			if (base.QuestGiver.CurrentSettlement != null && base.QuestGiver.CurrentSettlement.Town != null)
			{
				base.QuestGiver.CurrentSettlement.Town.Security += 5f;
			}
			if (!isTimedOut)
			{
				CompleteQuestWithFail();
			}
		}

		protected override void OnTimedOut()
		{
			AddLog(TimeOutLogText);
			FailConsequences(isTimedOut: true);
		}
	}

	public class CapturedByBountyHuntersIssueTypeDefiner : SaveableTypeDefiner
	{
		public CapturedByBountyHuntersIssueTypeDefiner()
			: base(580000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(CapturedByBountyHuntersIssue), 1);
			AddClassDefinition(typeof(CapturedByBountyHuntersIssueQuest), 2);
		}
	}

	private const IssueBase.IssueFrequency CapturedByBountyHuntersIssueFrequency = IssueBase.IssueFrequency.Common;

	private float ValidHideoutDistance => Campaign.Current.EstimatedAverageBanditPartySpeed * (float)CampaignTime.HoursInDay;

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private bool ConditionsHold(Hero issueGiver, out Settlement selectedHideout)
	{
		selectedHideout = null;
		if (issueGiver.IsLord || (issueGiver.IsNotable && issueGiver.CurrentSettlement == null))
		{
			return false;
		}
		if (issueGiver.IsGangLeader)
		{
			selectedHideout = FindSuitableHideout(issueGiver);
			CharacterObject characterObject = MBObjectManager.Instance.GetObject<CharacterObject>("looter");
			if (selectedHideout != null)
			{
				return characterObject != null;
			}
			return false;
		}
		return false;
	}

	private Settlement FindSuitableHideout(Hero issueGiver)
	{
		return SettlementHelper.FindNearestHideoutToSettlement(issueGiver.CurrentSettlement, MobileParty.NavigationType.Default, (Settlement s) => s.Hideout.IsInfested && !s.IsSettlementBusy(this) && Campaign.Current.Models.MapDistanceModel.GetDistance(issueGiver.CurrentSettlement, s, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default) < ValidHideoutDistance)?.Settlement;
	}

	public void OnCheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero, out var selectedHideout))
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnSelected, typeof(CapturedByBountyHuntersIssue), IssueBase.IssueFrequency.Common, selectedHideout));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(CapturedByBountyHuntersIssue), IssueBase.IssueFrequency.Common));
		}
	}

	private IssueBase OnSelected(in PotentialIssueData pid, Hero issueOwner)
	{
		return new CapturedByBountyHuntersIssue(issueOwner, pid.RelatedObject as Settlement);
	}
}
