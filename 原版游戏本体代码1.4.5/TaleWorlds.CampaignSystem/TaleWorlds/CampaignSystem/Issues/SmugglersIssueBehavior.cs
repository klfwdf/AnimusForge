using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
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

public class SmugglersIssueBehavior : CampaignBehaviorBase
{
	public class SmugglersIssue : IssueBase
	{
		[SaveableField(1)]
		private readonly Settlement _targetSettlement;

		[SaveableField(2)]
		private readonly Settlement _originSettlement;

		private const int IssueDuration = 30;

		private const int QuestTimeLimit = 20;

		private const int CompanionRequiredSkillLevel = 150;

		private const int AlternativeSolutionMinimumTroopLevel = 2;

		private const int SuccessRelationBonus = 10;

		private const int FailureRelationPenalty = -10;

		private const int SuccessSecurityBonus = 10;

		private const int TroopsRequiredForQuest = 10;

		public override bool IsThereAlternativeSolution => true;

		public override bool IsThereLordSolution => false;

		protected override bool IssueQuestCanBeDuplicated => false;

		protected override int RewardGold => (int)(750f + 3000f * base.IssueDifficultyMultiplier);

		public override TextObject IssueBriefByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=bZQQPtJr}{TARGET_SETTLEMENT} has a problem with smugglers. We depend on tariffs for income. People complain, but if we don't collect, then there's no money to keep up the walls and pay the garrison, and believe me if an enemy army came around and pillaged the place because it wasn't defended properly, they'd complain about that too.");
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.Name);
				return textObject;
			}
		}

		public override TextObject IssueAcceptByPlayer => new TextObject("{=088shjNR}Go on...");

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=84x0gfUf}I would prefer it if you attended to this yourself. But if you have any companions who can handle a force of {REQUIRED_TROOP_AMOUNT} men, one of them can take care of it.");
				textObject.SetTextVariable("REQUIRED_TROOP_AMOUNT", GetTotalAlternativeSolutionNeededMenCount());
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionAcceptByPlayer => new TextObject("{=tbR8HmFc}No need to worry, I will have my men attend to the matter immediately.");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=9jJ0QQYm}I think the reason we have it so bad here is because one gang in particular has good contacts both in {TARGET_SETTLEMENT} and in {ORIGIN_SETTLEMENT}. They've so far managed to elude me, but if you patrol the area between the two cities, you might have better luck. And look - I'd prefer it if you brought them to justice, but if you can persuade them to move elsewhere, I'd settle for that too.");
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.Name);
				textObject.SetTextVariable("ORIGIN_SETTLEMENT", _originSettlement.Name);
				return textObject;
			}
		}

		public override TextObject IssueQuestSolutionAcceptByPlayer => new TextObject("{=acqSONrT}I will handle these smugglers myself.");

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver => new TextObject("{=g8qb3Ame}Thank you.");

		public override TextObject IssueDiscussAlternativeSolution => new TextObject("{=RZdL3Mbt}I'm sure your men will take care of those smugglers in no time. I await your good news.");

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=Gop9Khfk}Smugglers of {TARGET_CITY}");
				textObject.SetTextVariable("TARGET_CITY", _targetSettlement.Name);
				return textObject;
			}
		}

		public override TextObject Description
		{
			get
			{
				TextObject textObject = new TextObject("{=pS8Iqeja}{QUEST_GIVER.LINK} is having problems with smugglers in {ORIGIN_CITY}. {?QUEST_GIVER.GENDER}She{?}He{\\?} said that they get their goods from {TARGET_CITY}.");
				textObject.SetTextVariable("TARGET_CITY", _targetSettlement.Name);
				textObject.SetTextVariable("ORIGIN_CITY", _originSettlement.Name);
				textObject.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject);
				return textObject;
			}
		}

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=bGERUWEr}{ISSUE_GIVER.LINK} {?ISSUE_GIVER.GENDER}lady{?}lord{\\?} of {TARGET_SETTLEMENT} told you that {?ISSUE_GIVER.GENDER}she{?}he{\\?} has been having issues with smugglers running between {TARGET_SETTLEMENT} and {ORIGIN_SETTLEMENT}. You decided to send {COMPANION.LINK} with {TROOP_COUNT} men to find the smugglers. They should return after {RETURN_DAYS} days.");
				textObject.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject);
				textObject.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject);
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("ORIGIN_SETTLEMENT", _originSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("TROOP_COUNT", AlternativeSolutionSentTroops.TotalManCount - 1);
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=QhyB7MX0}Your companion {COMPANION.LINK} and your men return with the news of their success. They dispersed the smugglers as promised to {QUEST_GIVER.LINK}.");
				textObject.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject);
				textObject.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionFailLog
		{
			get
			{
				TextObject textObject = new TextObject("{=ExRIMXDV}Your men failed to get rid of the smugglers as you told {QUEST_GIVER.LINK} you would do.");
				textObject.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject);
				return textObject;
			}
		}

		protected override int CompanionSkillRewardXP => 500 + (int)(1000f * base.IssueDifficultyMultiplier);

		public override int AlternativeSolutionBaseNeededMenCount => (int)(10f + 20f * base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => (int)(7f + 10f * base.IssueDifficultyMultiplier);

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.Casualties | AlternativeSolutionScaleFlag.FailureRisk;

		internal static void AutoGeneratedStaticCollectObjectsSmugglersIssue(object o, List<object> collectedObjects)
		{
			((SmugglersIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_targetSettlement);
			collectedObjects.Add(_originSettlement);
		}

		internal static object AutoGeneratedGetMemberValue_targetSettlement(object o)
		{
			return ((SmugglersIssue)o)._targetSettlement;
		}

		internal static object AutoGeneratedGetMemberValue_originSettlement(object o)
		{
			return ((SmugglersIssue)o)._originSettlement;
		}

		public SmugglersIssue(Hero issueOwner, KeyValuePair<Settlement, Settlement> questSettlementPair)
			: base(issueOwner, CampaignTime.DaysFromNow(30f))
		{
			_targetSettlement = questSettlementPair.Key;
			_originSettlement = questSettlementPair.Value;
		}

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			List<SkillObject> alternativeSolutionMeleeSkills = QuestHelper.GetAlternativeSolutionMeleeSkills();
			alternativeSolutionMeleeSkills.Add(DefaultSkills.Scouting);
			return (TaleWorlds.Core.Extensions.MaxBy(alternativeSolutionMeleeSkills, (SkillObject skill) => hero.GetSkillValue(skill)), 150);
		}

		public override bool AlternativeSolutionCondition(out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
			RelationshipChangeWithIssueOwner = 10;
			_targetSettlement.Town.Security += 10f;
		}

		protected override void AlternativeSolutionEndWithFailureConsequence()
		{
			RelationshipChangeWithIssueOwner = -10;
		}

		public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
		{
			return character.Tier >= 2;
		}

		public override IssueFrequency GetFrequency()
		{
			return IssueFrequency.Rare;
		}

		public override bool IssueStayAliveConditions()
		{
			if (base.IssueOwner.IsAlive && _targetSettlement.Owner == base.IssueOwner)
			{
				return _targetSettlement.Town.Security < 80f;
			}
			return false;
		}

		protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out PreconditionFlags flag, out Hero relationHero, out SkillObject skill, out int requiredGold)
		{
			flag = PreconditionFlags.None;
			relationHero = issueGiver;
			requiredGold = 0;
			skill = null;
			if (issueGiver.GetRelationWithPlayer() < -10f)
			{
				flag |= PreconditionFlags.Relation;
			}
			if (FactionManager.IsAtWarAgainstFaction(issueGiver.MapFaction, Hero.MainHero.MapFaction))
			{
				flag |= PreconditionFlags.AtWar;
			}
			if (MobileParty.MainParty.MemberRoster.TotalManCount <= 10)
			{
				flag |= PreconditionFlags.NotEnoughTroops;
			}
			return flag == PreconditionFlags.None;
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.SettlementLoyalty)
			{
				return -0.1f;
			}
			if (issueEffect == DefaultIssueEffects.SettlementSecurity)
			{
				return -0.5f;
			}
			return 0f;
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}

		protected override void OnGameLoad()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new SmugglersIssueQuest(questId, base.IssueOwner, _targetSettlement, _originSettlement, base.IssueDifficultyMultiplier, CampaignTime.DaysFromNow(20f), RewardGold);
		}

		protected override void HourlyTick()
		{
		}
	}

	public class SmugglersIssueQuest : QuestBase
	{
		[SaveableField(1)]
		private readonly Settlement _targetSettlement;

		[SaveableField(2)]
		private readonly Settlement _originSettlement;

		[SaveableField(3)]
		private MobileParty _smugglerParty;

		[SaveableField(4)]
		private readonly float _issueDifficulty;

		[SaveableField(5)]
		private int _smugglerSettlementWaitCounter;

		private const int FailRelationPenalty = -10;

		private const int SuccessRelationBonus = 10;

		private const int SuccessSecurityBonus = 10;

		private const int MaxSmugglerPartySize = 35;

		private const int MinSmugglerPartySize = 15;

		private const int SmugglerPartyWaitingHours = 4;

		private const string SmugglerPersuasionDialogToken = "start_smugglers_persuasion";

		private readonly string[] _possibleSmuggledItems = new string[4] { "jewelry", "spice", "velvet", "fur" };

		private PersuasionTask _task;

		private const PersuasionDifficulty Difficulty = PersuasionDifficulty.MediumHard;

		private int BribeAmount => (int)((float)RewardGold * 0.75f);

		public override bool IsRemainingTimeHidden => false;

		private TextObject QuestStartedLog
		{
			get
			{
				TextObject textObject = new TextObject("{=R98gwuK1}{QUEST_GIVER.LINK} {?QUEST_GIVER.GENDER}lady{?}lord{\\?} of {TARGET_SETTLEMENT} told you that {?QUEST_GIVER.GENDER}she{?}he{\\?} has been having issues with smugglers running between {TARGET_SETTLEMENT} and {ORIGIN_SETTLEMENT}. You promised to track the smugglers down and chase them away from {TARGET_SETTLEMENT}.");
				textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("ORIGIN_SETTLEMENT", _originSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject QuestCanceledWarDeclaredLog
		{
			get
			{
				TextObject textObject = new TextObject("{=vW6kBki9}Your clan is now at war with {QUEST_GIVER.LINK}'s realm. Your agreement with {QUEST_GIVER.LINK} is canceled.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject QuestCanceledSettlementOwnerChangedLog
		{
			get
			{
				TextObject textObject = new TextObject("{=xSaVRIN7}{QUEST_GIVER.LINK} has lost the ownership of {TARGET_CITY}. Your agreement with {QUEST_GIVER.LINK} is canceled.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("TARGET_CITY", _targetSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject QuestFailedLog
		{
			get
			{
				TextObject textObject = new TextObject("{=QB4bKAVR}You failed to get rid of the smugglers as you told {QUEST_GIVER.LINK} you would do.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject QuestSuccessWithFightLog
		{
			get
			{
				TextObject textObject = new TextObject("{=7N8jdPdV}You got rid of the smugglers as you promised {QUEST_GIVER.LINK} you would do.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject QuestSuccessWithPersuasionLog
		{
			get
			{
				TextObject textObject = new TextObject("{=84ZwbJaq}You persuaded the smugglers to move away from {TARGET_SETTLEMENT}. They are now far away from {TARGET_SETTLEMENT} and {QUEST_GIVER.LINK} is satisfied.");
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.Name);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject QuestSuccessWithBribeLog
		{
			get
			{
				TextObject textObject = new TextObject("{=Ici6us1S}You paid the smugglers to move away from {TARGET_SETTLEMENT}. They are now far away and {QUEST_GIVER.LINK} is satisfied.");
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.Name);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=Gop9Khfk}Smugglers of {TARGET_CITY}");
				textObject.SetTextVariable("TARGET_CITY", _targetSettlement.Name);
				return textObject;
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsSmugglersIssueQuest(object o, List<object> collectedObjects)
		{
			((SmugglersIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_targetSettlement);
			collectedObjects.Add(_originSettlement);
			collectedObjects.Add(_smugglerParty);
		}

		internal static object AutoGeneratedGetMemberValue_targetSettlement(object o)
		{
			return ((SmugglersIssueQuest)o)._targetSettlement;
		}

		internal static object AutoGeneratedGetMemberValue_originSettlement(object o)
		{
			return ((SmugglersIssueQuest)o)._originSettlement;
		}

		internal static object AutoGeneratedGetMemberValue_smugglerParty(object o)
		{
			return ((SmugglersIssueQuest)o)._smugglerParty;
		}

		internal static object AutoGeneratedGetMemberValue_issueDifficulty(object o)
		{
			return ((SmugglersIssueQuest)o)._issueDifficulty;
		}

		internal static object AutoGeneratedGetMemberValue_smugglerSettlementWaitCounter(object o)
		{
			return ((SmugglersIssueQuest)o)._smugglerSettlementWaitCounter;
		}

		public SmugglersIssueQuest(string questId, Hero questGiver, Settlement targetSettlement, Settlement originSettlement, float issueDifficulty, CampaignTime duration, int rewardGold)
			: base(questId, questGiver, duration, rewardGold)
		{
			_targetSettlement = targetSettlement;
			_originSettlement = originSettlement;
			_issueDifficulty = issueDifficulty;
			SetDialogs();
			InitializeQuestOnCreation();
		}

		protected override void SetDialogs()
		{
			TextObject npcText = new TextObject("{=g8qb3Ame}Thank you.");
			TextObject npcText2 = new TextObject("{=KLkaBjy7}I'm glad you're taking care of those smugglers.");
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(npcText).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.Consequence(QuestAcceptedConsequences)
				.CloseDialog();
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(npcText2).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += MapEventHelper.OnConversationEnd;
				})
				.CloseDialog();
			DialogFlow smugglerPartyDialog = GetSmugglerPartyDialog();
			AddPersuasionDialogs(smugglerPartyDialog);
			Campaign.Current.ConversationManager.AddDialogFlow(smugglerPartyDialog, this);
		}

		private DialogFlow GetSmugglerPartyDialog()
		{
			TextObject npcText = new TextObject("{=EUJaTe2v}Who are you? What do you want from us?");
			TextObject textObject = new TextObject("{=4iEQr3il}Halt. You're wanted by the authorities in {TARGET_SETTLEMENT} for smuggling.");
			textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.Name);
			TextObject npcText2 = new TextObject("{=vo8fmqwC}Hey... We're just honest traders here, trying to get by in these hard times.");
			TextObject playerText = new TextObject("{=qa3xzdbO}I think not. I'm going to give you one chance to leave this district, and never come back under pain of death.");
			TextObject npcText3 = new TextObject("{=BRaGcALd}Hah! Now why would we do that?");
			TextObject text = new TextObject("{=Ga5E2saO}Because it would be easier to work elsewhere.");
			TextObject text2 = new TextObject("{=722uabab}Because if you don't, I'll cut you down right here and now.");
			TextObject npcText4 = new TextObject("{=8dys8maS}Oh is that so? This will be fun.");
			TextObject text3 = new TextObject("{=WwzniBzk}Because I'll pay you. Silver is better than death, isn't it?");
			TextObject textObject2 = new TextObject("{=aABDmDJk}Heh, I like the way you think. Fine. It's become too risky around these parts anyway. You won't see us near {TARGET_SETTLEMENT} again.");
			textObject2.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.Name);
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine(npcText).Condition(() => _smugglerParty != null && CharacterObject.OneToOneConversationCharacter == ConversationHelper.GetConversationCharacterPartyLeader(_smugglerParty.Party))
				.Consequence(delegate
				{
					_task = GetPersuasionTask();
				})
				.PlayerLine(textObject)
				.NpcLine(npcText2)
				.PlayerLine(playerText)
				.NpcLine(npcText3)
				.BeginPlayerOptions()
				.PlayerOption(text)
				.GotoDialogState("start_smugglers_persuasion")
				.PlayerOption(text2)
				.NpcLine(npcText4)
				.Consequence(delegate
				{
					EncounterManager.StartPartyEncounter(PartyBase.MainParty, _smugglerParty.Party);
					Campaign.Current.GameMenuManager.SetNextMenu("encounter");
				})
				.CloseDialog()
				.PlayerOption(text3)
				.ClickableCondition(BribeCondition)
				.NpcLine(textObject2)
				.Consequence(SucceedQuestWithBribe)
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
		}

		private PersuasionTask GetPersuasionTask()
		{
			PersuasionTask obj = new PersuasionTask(0)
			{
				FinalFailLine = new TextObject("{=iRarm6f7}We are not going anywhere friend. You're going to have to fight for your silver today."),
				TryLaterLine = TextObject.GetEmpty(),
				SpokenLine = new TextObject("{=xnT03Yv0}I'm listening.")
			};
			TextObject textObject = new TextObject("{=gtY7QuX0}{QUEST_GIVER.LINK} is on to you. I can guarantee you the time of easy pickings and low risk is over.");
			textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
			TextObject textObject2 = new TextObject("{=T7bMlcj3}Prices in {TARGET_SETTLEMENT} are down these days. Enterprising lads like you can do better elsewhere.");
			textObject2.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.Name);
			TextObject line = new TextObject("{=6cbdm02F}Because if you make me angry, you'll wish you were dead long before I actually kill you.");
			PersuasionOptionArgs option = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Valor, TraitEffect.Positive, PersuasionArgumentStrength.Hard, givesCriticalSuccess: true, textObject);
			PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Trade, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.VeryEasy, givesCriticalSuccess: false, textObject2);
			PersuasionOptionArgs option3 = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Mercy, TraitEffect.Negative, PersuasionArgumentStrength.VeryHard, givesCriticalSuccess: false, line);
			obj.AddOptionToTask(option);
			obj.AddOptionToTask(option2);
			obj.AddOptionToTask(option3);
			return obj;
		}

		private void AddPersuasionDialogs(DialogFlow dialog)
		{
			dialog.AddDialogLine("smugglers_persuasion_check_accepted", "start_smugglers_persuasion", "smugglers_persuasion_start_reservation", "{=6P1ruzsC}Maybe...", persuasion_start_with_smugglers_on_condition, persuasion_start_with_smugglers_on_consequence, this);
			dialog.AddDialogLine("smugglers_persuasion_rejected", "smugglers_persuasion_start_reservation", "close_window", "{=!}{FAILED_PERSUASION_LINE}", persuasion_failed_with_smugglers_on_condition, persuasion_rejected_with_smugglers_on_consequence, this);
			dialog.AddDialogLine("smugglers_persuasion_attempt", "smugglers_persuasion_start_reservation", "smugglers_persuasion_select_option", "{=wM77S68a}What's there to discuss?", () => !persuasion_failed_with_smugglers_on_condition(), null, this);
			dialog.AddDialogLine("smugglers_persuasion_success", "smugglers_persuasion_start_reservation", "close_window", "{=m61UnySr}Yeah, you make a good point. If you find us, others can too. We will move elsewhere.", ConversationManager.GetPersuasionProgressSatisfied, persuasion_complete_with_smugglers_on_consequence, this, 200);
			ConversationSentence.OnConditionDelegate conditionDelegate = smugglers_persuasion_select_option_1_on_condition;
			ConversationSentence.OnConsequenceDelegate consequenceDelegate = smugglers_persuasion_select_option_1_on_consequence;
			ConversationSentence.OnPersuasionOptionDelegate persuasionOptionDelegate = smugglers_persuasion_setup_option_1;
			ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = smugglers_persuasion_clickable_option_1_on_condition;
			dialog.AddPlayerLine("smugglers_persuasion_select_option_1", "smugglers_persuasion_select_option", "smugglers_persuasion_selected_option_response", "{=!}{smugglers_PERSUADE_ATTEMPT_1}", conditionDelegate, consequenceDelegate, this, 100, clickableConditionDelegate, persuasionOptionDelegate);
			ConversationSentence.OnConditionDelegate conditionDelegate2 = smugglers_persuasion_select_option_2_on_condition;
			ConversationSentence.OnConsequenceDelegate consequenceDelegate2 = smugglers_persuasion_select_option_2_on_consequence;
			persuasionOptionDelegate = smugglers_persuasion_setup_option_2;
			clickableConditionDelegate = smugglers_persuasion_clickable_option_2_on_condition;
			dialog.AddPlayerLine("smugglers_persuasion_select_option_2", "smugglers_persuasion_select_option", "smugglers_persuasion_selected_option_response", "{=!}{smugglers_PERSUADE_ATTEMPT_2}", conditionDelegate2, consequenceDelegate2, this, 100, clickableConditionDelegate, persuasionOptionDelegate);
			ConversationSentence.OnConditionDelegate conditionDelegate3 = smugglers_persuasion_select_option_3_on_condition;
			ConversationSentence.OnConsequenceDelegate consequenceDelegate3 = smugglers_persuasion_select_option_3_on_consequence;
			persuasionOptionDelegate = smugglers_persuasion_setup_option_3;
			clickableConditionDelegate = smugglers_persuasion_clickable_option_3_on_condition;
			dialog.AddPlayerLine("smugglers_persuasion_select_option_3", "smugglers_persuasion_select_option", "smugglers_persuasion_selected_option_response", "{=!}{smugglers_PERSUADE_ATTEMPT_3}", conditionDelegate3, consequenceDelegate3, this, 100, clickableConditionDelegate, persuasionOptionDelegate);
			dialog.AddDialogLine("smugglers_persuasion_select_option_reaction", "smugglers_persuasion_selected_option_response", "smugglers_persuasion_start_reservation", "{=!}{PERSUASION_REACTION}", smugglers_persuasion_selected_option_response_on_condition, smugglers_persuasion_selected_option_response_on_consequence, this);
		}

		private void persuasion_start_with_smugglers_on_consequence()
		{
			ConversationManager.StartPersuasion(2f, 1f, 0f, 2f, 2f, 0f, PersuasionDifficulty.MediumHard);
		}

		private bool persuasion_start_with_smugglers_on_condition()
		{
			if (_smugglerParty != null)
			{
				return CharacterObject.OneToOneConversationCharacter == ConversationHelper.GetConversationCharacterPartyLeader(_smugglerParty.Party);
			}
			return false;
		}

		private bool smugglers_persuasion_selected_option_response_on_condition()
		{
			PersuasionOptionResult item = ConversationManager.GetPersuasionChosenOptions().Last().Item2;
			MBTextManager.SetTextVariable("PERSUASION_REACTION", PersuasionHelper.GetDefaultPersuasionOptionReaction(item));
			if (item == PersuasionOptionResult.CriticalFailure)
			{
				_task.BlockAllOptions();
			}
			return true;
		}

		private void smugglers_persuasion_selected_option_response_on_consequence()
		{
			Tuple<PersuasionOptionArgs, PersuasionOptionResult> tuple = ConversationManager.GetPersuasionChosenOptions().Last();
			float difficulty = Campaign.Current.Models.PersuasionModel.GetDifficulty(PersuasionDifficulty.MediumHard);
			Campaign.Current.Models.PersuasionModel.GetEffectChances(tuple.Item1, out var moveToNextStageChance, out var blockRandomOptionChance, difficulty);
			_task.ApplyEffects(moveToNextStageChance, blockRandomOptionChance);
		}

		private bool smugglers_persuasion_select_option_1_on_condition()
		{
			if (_task.Options.Count > 0)
			{
				TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
				textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(_task.Options.ElementAt(0), showToPlayer: false));
				textObject.SetTextVariable("PERSUASION_OPTION_LINE", _task.Options.ElementAt(0).Line);
				MBTextManager.SetTextVariable("smugglers_PERSUADE_ATTEMPT_1", textObject);
				return true;
			}
			return false;
		}

		private bool smugglers_persuasion_select_option_2_on_condition()
		{
			if (_task.Options.Count > 1)
			{
				TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
				textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(_task.Options.ElementAt(1), showToPlayer: false));
				textObject.SetTextVariable("PERSUASION_OPTION_LINE", _task.Options.ElementAt(1).Line);
				MBTextManager.SetTextVariable("smugglers_PERSUADE_ATTEMPT_2", textObject);
				return true;
			}
			return false;
		}

		private bool smugglers_persuasion_select_option_3_on_condition()
		{
			if (_task.Options.Count > 2)
			{
				TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
				textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(_task.Options.ElementAt(2), showToPlayer: false));
				textObject.SetTextVariable("PERSUASION_OPTION_LINE", _task.Options.ElementAt(2).Line);
				MBTextManager.SetTextVariable("smugglers_PERSUADE_ATTEMPT_3", textObject);
				return true;
			}
			return false;
		}

		private void smugglers_persuasion_select_option_1_on_consequence()
		{
			if (_task.Options.Count > 0)
			{
				_task.Options[0].BlockTheOption(isBlocked: true);
			}
		}

		private void smugglers_persuasion_select_option_2_on_consequence()
		{
			if (_task.Options.Count > 1)
			{
				_task.Options[1].BlockTheOption(isBlocked: true);
			}
		}

		private void smugglers_persuasion_select_option_3_on_consequence()
		{
			if (_task.Options.Count > 2)
			{
				_task.Options[2].BlockTheOption(isBlocked: true);
			}
		}

		private bool persuasion_failed_with_smugglers_on_condition()
		{
			if (_task.Options.All((PersuasionOptionArgs x) => x.IsBlocked) && !ConversationManager.GetPersuasionProgressSatisfied())
			{
				MBTextManager.SetTextVariable("FAILED_PERSUASION_LINE", _task.FinalFailLine);
				return true;
			}
			return false;
		}

		private PersuasionOptionArgs smugglers_persuasion_setup_option_1()
		{
			return _task.Options.ElementAt(0);
		}

		private PersuasionOptionArgs smugglers_persuasion_setup_option_2()
		{
			return _task.Options.ElementAt(1);
		}

		private PersuasionOptionArgs smugglers_persuasion_setup_option_3()
		{
			return _task.Options.ElementAt(2);
		}

		private bool smugglers_persuasion_clickable_option_1_on_condition(out TextObject hintText)
		{
			hintText = new TextObject("{=9ACJsI6S}Blocked");
			if (_task.Options.Count > 0)
			{
				hintText = (_task.Options.ElementAt(0).IsBlocked ? hintText : null);
				return !_task.Options.ElementAt(0).IsBlocked;
			}
			return false;
		}

		private bool smugglers_persuasion_clickable_option_2_on_condition(out TextObject hintText)
		{
			hintText = new TextObject("{=9ACJsI6S}Blocked");
			if (_task.Options.Count > 1)
			{
				hintText = (_task.Options.ElementAt(1).IsBlocked ? hintText : null);
				return !_task.Options.ElementAt(1).IsBlocked;
			}
			return false;
		}

		private bool smugglers_persuasion_clickable_option_3_on_condition(out TextObject hintText)
		{
			hintText = new TextObject("{=9ACJsI6S}Blocked");
			if (_task.Options.Count > 2)
			{
				hintText = (_task.Options.ElementAt(2).IsBlocked ? hintText : null);
				return !_task.Options.ElementAt(2).IsBlocked;
			}
			return false;
		}

		private void persuasion_rejected_with_smugglers_on_consequence()
		{
			PlayerEncounter.LeaveEncounter = false;
			ConversationManager.EndPersuasion();
		}

		private void persuasion_complete_with_smugglers_on_consequence()
		{
			PlayerEncounter.LeaveEncounter = true;
			ConversationManager.EndPersuasion();
			Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
			{
				SucceedQuest(QuestSuccessWithPersuasionLog);
			};
		}

		private bool BribeCondition(out TextObject explanation)
		{
			if (Hero.MainHero.Gold >= BribeAmount)
			{
				explanation = new TextObject("{=FebKjriO}You will give {BRIBE_AMOUNT} denars.");
				explanation.SetTextVariable("BRIBE_AMOUNT", BribeAmount);
				return true;
			}
			explanation = new TextObject("{=Xy4brTbf}You don't have {BRIBE_AMOUNT} denars.");
			explanation.SetTextVariable("BRIBE_AMOUNT", BribeAmount);
			return false;
		}

		private void QuestAcceptedConsequences()
		{
			StartQuest();
			AddLog(QuestStartedLog);
			_smugglerParty = CreateSmugglerParty();
			AddTrackedObject(_targetSettlement);
			AddTrackedObject(_originSettlement);
		}

		private MobileParty CreateSmugglerParty()
		{
			TextObject textObject = new TextObject("{=3dhAfC4k}Smugglers of {ORIGIN_SETTLEMENT}");
			textObject.SetTextVariable("ORIGIN_SETTLEMENT", _originSettlement.Name);
			GetAdditionalVisualsForParty(_originSettlement.Culture, out var mountStringId, out var harnessStringId);
			Hideout nearestHideoutSettlement = SettlementHelper.FindNearestHideoutToSettlement(_originSettlement, MobileParty.NavigationType.Default);
			int desiredMenCount = (int)TaleWorlds.Library.MathF.Clamp(TaleWorlds.Library.MathF.Ceiling((float)MobileParty.MainParty.MemberRoster.TotalManCount * 0.8f), 15f, 35f);
			float customPartyBaseSpeed = MobileParty.MainParty.Speed * 1.1f;
			PartyTemplateObject randomCaravanTemplate = CaravanHelper.GetRandomCaravanTemplate(_originSettlement.Culture, isElite: false, isLand: true);
			MobileParty mobileParty = CustomPartyComponent.CreateCustomPartyWithTroopRoster(_originSettlement.GatePosition, 0.1f, _originSettlement, textObject, Clan.BanditFactions.FirstOrDefault((Clan faction) => faction.Culture == nearestHideoutSettlement.Settlement.Culture), TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), null, mountStringId, harnessStringId, customPartyBaseSpeed, avoidHostileActions: true);
			MobilePartyHelper.FillPartyManuallyAfterCreation(mobileParty, randomCaravanTemplate, desiredMenCount);
			CharacterObject character = MBObjectManager.Instance.GetObject<CharacterObject>("nervous_caravanmaster_" + MBRandom.RandomInt(1, 4));
			mobileParty.MemberRoster.AddToCounts(character, 1, insertAtFront: true);
			GiveGoodsToParty(mobileParty);
			InitializePartyState(mobileParty);
			mobileParty.SetPartyUsedByQuest(isActivelyUsed: true);
			return mobileParty;
		}

		private void GetAdditionalVisualsForParty(CultureObject culture, out string mountStringId, out string harnessStringId)
		{
			if (culture.StringId == "aserai" || culture.StringId == "khuzait")
			{
				mountStringId = "camel";
				harnessStringId = ((MBRandom.RandomFloat > 0.5f) ? "camel_saddle_a" : "camel_saddle_b");
			}
			else
			{
				mountStringId = "mule";
				harnessStringId = ((MBRandom.RandomFloat > 0.5f) ? "mule_load_a" : ((MBRandom.RandomFloat > 0.5f) ? "mule_load_b" : "mule_load_c"));
			}
		}

		private void GiveGoodsToParty(MobileParty mobileParty)
		{
			int num = TaleWorlds.Library.MathF.Ceiling(400f + 3000f * _issueDifficulty);
			ItemObject itemObject = MBObjectManager.Instance.GetObject<ItemObject>(_possibleSmuggledItems.GetRandomElement());
			int number = num / itemObject.Value;
			mobileParty.ItemRoster.AddToCounts(itemObject, number);
			mobileParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("grain"), TaleWorlds.Library.MathF.Ceiling(30f + 20f * _issueDifficulty));
		}

		private void InitializePartyState(MobileParty mobileParty)
		{
			TextObject customName = new TextObject("{=GTnVcUz9}Smugglers' Party");
			mobileParty.InitializeMobilePartyAtPosition(new TroopRoster(mobileParty.Party), new TroopRoster(mobileParty.Party), _originSettlement.GatePosition);
			mobileParty.Party.SetCustomName(customName);
			mobileParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
			mobileParty.IgnoreByOtherPartiesTill(CampaignTime.Never);
			mobileParty.SetCustomHomeSettlement(_originSettlement);
			SetPartyAiAction.GetActionForVisitingSettlement(mobileParty, _targetSettlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
		}

		protected override void OnTimedOut()
		{
			FailQuest();
		}

		private void SucceedQuestWithBribe()
		{
			PlayerEncounter.LeaveEncounter = true;
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, BribeAmount);
			SucceedQuest(QuestSuccessWithBribeLog);
		}

		private void SucceedQuest(TextObject log)
		{
			AddLog(log);
			RemoveTrackedObject(_targetSettlement);
			RemoveTrackedObject(_originSettlement);
			RelationshipChangeWithQuestGiver = 10;
			_targetSettlement.Town.Security += 10f;
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
			if (_smugglerParty != null && _smugglerParty.IsActive)
			{
				DestroyPartyAction.Apply(null, _smugglerParty);
			}
			CompleteQuestWithSuccess();
		}

		private void FailQuest()
		{
			TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[2]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -50),
				new Tuple<TraitObject, int>(DefaultTraits.Valor, -50)
			});
			RelationshipChangeWithQuestGiver = -10;
			RemoveTrackedObject(_targetSettlement);
			RemoveTrackedObject(_originSettlement);
			CompleteQuestWithFail(QuestFailedLog);
			if (_smugglerParty != null && _smugglerParty.IsActive)
			{
				DestroyPartyAction.Apply(null, _smugglerParty);
			}
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
			CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, OnHourlyTickParty);
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
		}

		private void OnMapEventEnded(MapEvent mapEvent)
		{
			if (mapEvent.IsPlayerMapEvent && mapEvent.InvolvedParties.Contains(_smugglerParty.Party))
			{
				if (mapEvent.WinningSide == mapEvent.PlayerSide)
				{
					SucceedQuest(QuestSuccessWithFightLog);
				}
				else
				{
					FailQuest();
				}
			}
		}

		private void OnHourlyTickParty(MobileParty mobileParty)
		{
			if (mobileParty == _smugglerParty && mobileParty.CurrentSettlement != null)
			{
				if (_smugglerSettlementWaitCounter >= 4)
				{
					_smugglerSettlementWaitCounter = 0;
					Settlement settlement = ((mobileParty.CurrentSettlement == _targetSettlement) ? _originSettlement : _targetSettlement);
					SetPartyAiAction.GetActionForVisitingSettlement(mobileParty, settlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
				}
				else
				{
					_smugglerSettlementWaitCounter++;
				}
			}
		}

		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (newKingdom != null && ((clan == base.QuestGiver.Clan && newKingdom.IsAtWarWith(Clan.PlayerClan.MapFaction)) || (clan == Clan.PlayerClan && newKingdom.IsAtWarWith(base.QuestGiver.Clan.MapFaction))))
			{
				CompleteQuestWithCancel(QuestCanceledWarDeclaredLog);
			}
		}

		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail declareWarDetail)
		{
			QuestHelper.CheckWarDeclarationAndFailOrCancelTheQuest(this, faction1, faction2, declareWarDetail, QuestCanceledWarDeclaredLog, QuestCanceledWarDeclaredLog);
		}

		private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			if (settlement == _targetSettlement)
			{
				CompleteQuestWithCancel(QuestCanceledSettlementOwnerChangedLog);
			}
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
			if (_smugglerParty != null && _smugglerParty.IsActive)
			{
				DestroyPartyAction.Apply(null, _smugglerParty);
			}
		}
	}

	public class SmugglersIssueTypeDefiner : SaveableTypeDefiner
	{
		public SmugglersIssueTypeDefiner()
			: base(585960)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(SmugglersIssue), 1);
			AddClassDefinition(typeof(SmugglersIssueQuest), 2);
		}
	}

	private const IssueBase.IssueFrequency SmugglersIssueFrequency = IssueBase.IssueFrequency.Rare;

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
	}

	private void OnCheckForIssue(Hero hero)
	{
		Campaign.Current.IssueManager.AddPotentialIssueData(hero, ConditionsHold(hero, out var questSettlementPair) ? new PotentialIssueData(OnStartIssue, typeof(SmugglersIssue), IssueBase.IssueFrequency.Rare, questSettlementPair) : new PotentialIssueData(typeof(SmugglersIssue), IssueBase.IssueFrequency.Rare));
	}

	private bool ConditionsHold(Hero issueGiver, out KeyValuePair<Settlement, Settlement> questSettlementPair)
	{
		questSettlementPair = default(KeyValuePair<Settlement, Settlement>);
		if (issueGiver.IsLord && issueGiver.Clan != Clan.PlayerClan && issueGiver.GetRelationWithPlayer() >= -10f)
		{
			IEnumerable<Settlement> enumerable = issueGiver.Clan.Settlements.WhereQ((Settlement settlement2) => settlement2.Owner == issueGiver && settlement2.IsTown && SettlementHelper.FindNearestTownToSettlement(settlement2, MobileParty.NavigationType.Default, (Settlement town) => town != settlement2) != null);
			if (enumerable.Any())
			{
				Settlement targetSettlement = enumerable.GetRandomElementInefficiently();
				Settlement settlement = SettlementHelper.FindNearestTownToSettlement(targetSettlement, MobileParty.NavigationType.Default, (Settlement town) => town != targetSettlement).Settlement;
				questSettlementPair = new KeyValuePair<Settlement, Settlement>(targetSettlement, settlement);
				return true;
			}
		}
		return false;
	}

	private IssueBase OnStartIssue(in PotentialIssueData pid, Hero issueOwner)
	{
		return new SmugglersIssue(issueOwner, (KeyValuePair<Settlement, Settlement>)pid.RelatedObject);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
