using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace SandBox.Issues;

public class SnareTheWealthyIssueBehavior : CampaignBehaviorBase
{
	public class SnareTheWealthyIssueTypeDefiner : SaveableTypeDefiner
	{
		public SnareTheWealthyIssueTypeDefiner()
			: base(340000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(SnareTheWealthyIssue), 1);
			AddClassDefinition(typeof(SnareTheWealthyIssueQuest), 2);
		}

		protected override void DefineEnumTypes()
		{
			AddEnumDefinition(typeof(SnareTheWealthyIssueQuest.SnareTheWealthyQuestChoice), 3);
		}
	}

	public class SnareTheWealthyIssue : IssueBase
	{
		private const int IssueDuration = 30;

		private const int IssueQuestDuration = 10;

		private const int MinimumRequiredMenCount = 20;

		private const int MinimumRequiredRelationWithIssueGiver = -10;

		private const int AlternativeSolutionMinimumTroopTier = 2;

		private const int CompanionRoguerySkillValueThreshold = 120;

		[SaveableField(1)]
		private readonly CharacterObject _targetMerchantCharacter;

		private int AlternativeSolutionReward => TaleWorlds.Library.MathF.Floor(1000f + 3000f * base.IssueDifficultyMultiplier);

		public override TextObject IssueBriefByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=bLigh8Sd}Well, let's just say there's an idea I've been mulling over.[ib:confident2][if:convo_bemused] You may be able to help. Have you met {TARGET_MERCHANT.NAME}? {?TARGET_MERCHANT.GENDER}She{?}He{\\?} is a very rich merchant. Very rich indeed. But not very honest… It's not right that someone without morals should have so much wealth, is it? I have a plan to redistribute it a bit.");
				StringHelpers.SetCharacterProperties("TARGET_MERCHANT", _targetMerchantCharacter, textObject);
				return textObject;
			}
		}

		public override TextObject IssueAcceptByPlayer => new TextObject("{=keKEFagm}So what's the plan?");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=SliFGAX4}{TARGET_MERCHANT.NAME} is always looking for extra swords to protect[if:convo_evil_smile] {?TARGET_MERCHANT.GENDER}her{?}his{\\?} caravans. The wicked are the ones who fear wickedness the most, you might say. What if those guards turned out to be robbers? {TARGET_MERCHANT.NAME} wouldn't trust just anyone but I think {?TARGET_MERCHANT.GENDER}she{?}he{\\?} might hire a renowned warrior like yourself. And if that warrior were to lead the caravan into an ambush… Oh I suppose it's all a bit dishonorable, but I wouldn't worry too much about your reputation. {TARGET_MERCHANT.NAME} is known to defraud {?TARGET_MERCHANT.GENDER}her{?}his{\\?} partners. If something happened to one of {?TARGET_MERCHANT.GENDER}her{?}his{\\?} caravans - well, most people won't know who to believe, and won't really care either.");
				StringHelpers.SetCharacterProperties("TARGET_MERCHANT", _targetMerchantCharacter, textObject);
				return textObject;
			}
		}

		public override TextObject IssueQuestSolutionAcceptByPlayer => new TextObject("{=4upBpsnb}All right. I am in.");

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=ivNVRP69}I prefer if you do this yourself, but one of your trusted companions with a strong[if:convo_evil_smile] sword-arm and enough brains to set an ambush can do the job with {TROOP_COUNT} fighters. We'll split the loot, and I'll throw in a little bonus on top of that for you..");
				textObject.SetTextVariable("TROOP_COUNT", GetTotalAlternativeSolutionNeededMenCount());
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionAcceptByPlayer => new TextObject("{=biqYiCnr}My companion can handle it. Do not worry.");

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver => new TextObject("{=UURamhdC}Thank you. This should make both of us a pretty penny.[if:convo_delighted]");

		public override TextObject IssueDiscussAlternativeSolution => new TextObject("{=pmuEeFV8}We are still arranging with your men how we'll spring this ambush. Do not worry. Everything will go smoothly.");

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=28lLrXOe}{ISSUE_GIVER.LINK} shared their plan for robbing {TARGET_MERCHANT.LINK} with you. You agreed to send your companion along with {TROOP_COUNT} men to lead the ambush for them. They will return after {RETURN_DAYS} days.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("TARGET_MERCHANT", _targetMerchantCharacter, textObject);
				textObject.SetTextVariable("TROOP_COUNT", AlternativeSolutionSentTroops.TotalManCount - 1);
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		public override bool IsThereAlternativeSolution => true;

		public override bool IsThereLordSolution => false;

		public override TextObject Title => new TextObject("{=IeihUvCD}Snare the Wealthy");

		public override TextObject Description
		{
			get
			{
				TextObject textObject = new TextObject("{=8LghFfQO}Help {ISSUE_GIVER.NAME} to rob {TARGET_MERCHANT.NAME} by acting as their guard.");
				StringHelpers.SetCharacterProperties("TARGET_MERCHANT", _targetMerchantCharacter, textObject);
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		protected override bool IssueQuestCanBeDuplicated => false;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.Casualties | AlternativeSolutionScaleFlag.FailureRisk;

		public override int AlternativeSolutionBaseNeededMenCount => 10 + TaleWorlds.Library.MathF.Ceiling(16f * base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 2 + TaleWorlds.Library.MathF.Ceiling(4f * base.IssueDifficultyMultiplier);

		protected override int CompanionSkillRewardXP => (int)(800f + 1000f * base.IssueDifficultyMultiplier);

		public SnareTheWealthyIssue(Hero issueOwner, CharacterObject targetMerchant)
			: base(issueOwner, CampaignTime.DaysFromNow(30f))
		{
			_targetMerchantCharacter = targetMerchant;
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

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			return ((hero.GetSkillValue(DefaultSkills.Roguery) >= hero.GetSkillValue(DefaultSkills.Tactics)) ? DefaultSkills.Roguery : DefaultSkills.Tactics, 120);
		}

		public override bool AlternativeSolutionCondition(out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
		{
			return character.Tier >= 2;
		}

		public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
		{
			explanation = null;
			return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			TraitLevelingHelper.OnIssueSolvedThroughAlternativeSolution(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -50)
			});
			TraitLevelingHelper.OnIssueSolvedThroughAlternativeSolution(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Calculating, 50)
			});
			ChangeRelationAction.ApplyPlayerRelation(base.IssueOwner, 5);
			ChangeRelationAction.ApplyPlayerRelation(_targetMerchantCharacter.HeroObject, -10);
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, AlternativeSolutionReward);
		}

		protected override void AlternativeSolutionEndWithFailureConsequence()
		{
			TraitLevelingHelper.OnIssueSolvedThroughAlternativeSolution(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -100)
			});
			TraitLevelingHelper.OnIssueSolvedThroughAlternativeSolution(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Calculating, 100)
			});
			ChangeRelationAction.ApplyPlayerRelation(base.IssueOwner, -10);
			ChangeRelationAction.ApplyPlayerRelation(_targetMerchantCharacter.HeroObject, -10);
		}

		protected override void OnGameLoad()
		{
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new SnareTheWealthyIssueQuest(questId, base.IssueOwner, _targetMerchantCharacter, base.IssueDifficultyMultiplier, CampaignTime.DaysFromNow(10f));
		}

		protected override void OnIssueFinalized()
		{
			if (base.IsSolvingWithQuest)
			{
				Campaign.Current.IssueManager.AddIssueCoolDownData(GetType(), new HeroRelatedIssueCoolDownData(_targetMerchantCharacter.HeroObject, CampaignTime.DaysFromNow(Campaign.Current.Models.IssueModel.IssueOwnerCoolDownInDays)));
				Campaign.Current.IssueManager.AddIssueCoolDownData(typeof(EscortMerchantCaravanIssueBehavior.EscortMerchantCaravanIssueQuest), new HeroRelatedIssueCoolDownData(_targetMerchantCharacter.HeroObject, CampaignTime.DaysFromNow(Campaign.Current.Models.IssueModel.IssueOwnerCoolDownInDays)));
				Campaign.Current.IssueManager.AddIssueCoolDownData(typeof(CaravanAmbushIssueBehavior.CaravanAmbushIssueQuest), new HeroRelatedIssueCoolDownData(_targetMerchantCharacter.HeroObject, CampaignTime.DaysFromNow(Campaign.Current.Models.IssueModel.IssueOwnerCoolDownInDays)));
			}
		}

		public override IssueFrequency GetFrequency()
		{
			return IssueFrequency.Rare;
		}

		protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out PreconditionFlags flag, out Hero relationHero, out SkillObject skill, out int requiredGold)
		{
			flag = PreconditionFlags.None;
			relationHero = null;
			requiredGold = 0;
			skill = null;
			if (MobileParty.MainParty.MemberRoster.TotalHealthyCount < 20)
			{
				flag |= PreconditionFlags.NotEnoughTroops;
			}
			if (issueGiver.GetRelationWithPlayer() < -10f)
			{
				flag |= PreconditionFlags.Relation;
				relationHero = issueGiver;
			}
			if (issueGiver.CurrentSettlement.OwnerClan == Clan.PlayerClan)
			{
				flag |= PreconditionFlags.PlayerIsOwnerOfSettlement;
			}
			return flag == PreconditionFlags.None;
		}

		public override bool IssueStayAliveConditions()
		{
			if (base.IssueOwner.IsAlive && base.IssueOwner.CurrentSettlement.Town.Security <= 80f)
			{
				return _targetMerchantCharacter.HeroObject.IsAlive;
			}
			return false;
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}

		public override void OnHeroCanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
		{
			if (hero == _targetMerchantCharacter.HeroObject)
			{
				result = false;
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsSnareTheWealthyIssue(object o, List<object> collectedObjects)
		{
			((SnareTheWealthyIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_targetMerchantCharacter);
		}

		internal static object AutoGeneratedGetMemberValue_targetMerchantCharacter(object o)
		{
			return ((SnareTheWealthyIssue)o)._targetMerchantCharacter;
		}
	}

	public class SnareTheWealthyIssueQuest : QuestBase
	{
		internal enum SnareTheWealthyQuestChoice
		{
			None,
			SidedWithCaravan,
			SidedWithGang,
			BetrayedBoth
		}

		private delegate void QuestEndDelegate();

		private QuestEndDelegate _startConversationDelegate;

		[SaveableField(1)]
		private CharacterObject _targetMerchantCharacter;

		[SaveableField(2)]
		private Settlement _targetSettlement;

		[SaveableField(3)]
		private MobileParty _caravanParty;

		[SaveableField(4)]
		private MobileParty _gangParty;

		[SaveableField(5)]
		private readonly float _questDifficulty;

		[SaveableField(6)]
		private SnareTheWealthyQuestChoice _playerChoice;

		[SaveableField(7)]
		private bool _canEncounterConversationStart;

		[SaveableField(8)]
		private bool _isCaravanFollowing = true;

		private float CaravanEncounterStartDistance => Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius * 7f;

		private int CaravanPartyTroopCount => 20 + TaleWorlds.Library.MathF.Ceiling(40f * _questDifficulty);

		private int GangPartyTroopCount => 10 + TaleWorlds.Library.MathF.Ceiling(25f * _questDifficulty);

		private int Reward1 => TaleWorlds.Library.MathF.Floor(1000f + 3000f * _questDifficulty);

		private int Reward2 => TaleWorlds.Library.MathF.Floor((float)Reward1 * 0.4f);

		public override TextObject Title => new TextObject("{=IeihUvCD}Snare the Wealthy");

		public override bool IsRemainingTimeHidden => false;

		private TextObject QuestStartedLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=Ba9nsfHc}{QUEST_GIVER.LINK} shared their plan for robbing {TARGET_MERCHANT.LINK} with you. You agreed to talk with {TARGET_MERCHANT.LINK} to convince {?TARGET_MERCHANT.GENDER}her{?}him{\\?} to guard {?TARGET_MERCHANT.GENDER}her{?}his{\\?} caravan and lead the caravan to ambush around {TARGET_SETTLEMENT}.");
				StringHelpers.SetCharacterProperties("TARGET_MERCHANT", _targetMerchantCharacter, textObject);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject Success1LogText
		{
			get
			{
				TextObject textObject = new TextObject("{=bblwaDi1}You have successfully robbed {TARGET_MERCHANT.LINK}'s caravan with {QUEST_GIVER.LINK}.");
				StringHelpers.SetCharacterProperties("TARGET_MERCHANT", _targetMerchantCharacter, textObject);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject SidedWithGangLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=lZjj3MZg}When {QUEST_GIVER.LINK} arrived, you kept your side of the bargain and attacked the caravan");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject TimedOutWithoutTalkingToMerchantText
		{
			get
			{
				TextObject textObject = new TextObject("{=OMKgidoP}You have failed to convince the merchant to guard {?TARGET_MERCHANT.GENDER}her{?}his{\\?} caravan in time. {QUEST_GIVER.LINK} must be furious.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("TARGET_MERCHANT", _targetMerchantCharacter, textObject);
				return textObject;
			}
		}

		private TextObject Fail1LogText => new TextObject("{=DRpcqEMI}The caravan leader said your decisions were wasting their time and decided to go on his way. You have failed to uphold your part in the plan.");

		private TextObject Fail2LogText => new TextObject("{=EFjas6hI}At the last moment, you decided to side with the caravan guard and defend them.");

		private TextObject Fail2OutcomeLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=JgrG0uoO}Having the {TARGET_MERCHANT.LINK} by your side, you were successful in protecting the caravan.");
				StringHelpers.SetCharacterProperties("TARGET_MERCHANT", _targetMerchantCharacter, textObject);
				return textObject;
			}
		}

		private TextObject Fail3LogText => new TextObject("{=0NxiTi8b}You didn't feel like splitting the loot, so you betrayed both the merchant and the gang leader.");

		private TextObject Fail3OutcomeLogText => new TextObject("{=KbMew14D}Although the gang leader and the caravaneer joined their forces, you have successfully defeated them and kept the loot for yourself.");

		private TextObject Fail4LogText
		{
			get
			{
				TextObject textObject = new TextObject("{=22nahm29}You have lost the battle against the merchant's caravan and failed to help {QUEST_GIVER.LINK}.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject Fail5LogText
		{
			get
			{
				TextObject textObject = new TextObject("{=QEgzLRnC}You have lost the battle against {QUEST_GIVER.LINK} and failed to help the merchant as you promised.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject Fail6LogText
		{
			get
			{
				TextObject textObject = new TextObject("{=pGu2mcar}You have lost the battle against the combined forces of the {QUEST_GIVER.LINK} and the caravan.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject PlayerCapturedQuestSettlementLogText => new TextObject("{=gPFfHluf}Your clan is now owner of the settlement. As the lord of the settlement you cannot be part of the criminal activities anymore. Your agreement with the questgiver has canceled.");

		private TextObject QuestSettlementWasCapturedLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=uVigJ3LP}{QUEST_GIVER.LINK} has lost the control of {SETTLEMENT} and the deal is now invalid.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject WarDeclaredBetweenPlayerAndQuestGiverLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=ojpW4WRD}Your clan is now at war with the {QUEST_GIVER.LINK}'s lord. Your agreement with {QUEST_GIVER.LINK} was canceled.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject TargetSettlementRaidedLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=QkbkesNJ}{QUEST_GIVER.LINK} called off the ambush after {TARGET_SETTLEMENT} was raided.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject TalkedToMerchantLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=N1ZiaLRL}You talked to {TARGET_MERCHANT.LINK} as {QUEST_GIVER.LINK} asked. The caravan is waiting for you outside the gates to be escorted to {TARGET_SETTLEMENT}.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("TARGET_MERCHANT", _targetMerchantCharacter, textObject);
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		public SnareTheWealthyIssueQuest(string questId, Hero questGiver, CharacterObject targetMerchantCharacter, float questDifficulty, CampaignTime duration)
			: base(questId, questGiver, duration, 0)
		{
			_targetMerchantCharacter = targetMerchantCharacter;
			_targetSettlement = GetTargetSettlement();
			_questDifficulty = questDifficulty;
			SetDialogs();
			InitializeQuestOnCreation();
		}

		protected override void InitializeQuestOnGameLoad()
		{
			SetDialogs();
			Campaign.Current.ConversationManager.AddDialogFlow(GetEncounterDialogue(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetDialogueWithMerchant(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetDialogueWithCaravan(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetDialogueWithGangWithoutCaravan(), this);
		}

		private Settlement GetTargetSettlement()
		{
			MapDistanceModel model = Campaign.Current.Models.MapDistanceModel;
			return TaleWorlds.Core.Extensions.MinBy(Settlement.All.Where((Settlement t) => t != base.QuestGiver.CurrentSettlement && t.IsTown), (Settlement t) => model.GetDistance(t, base.QuestGiver.CurrentSettlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default)).BoundVillages.GetRandomElement().Settlement;
		}

		protected override void SetDialogs()
		{
			TextObject discussIntroDialogue = new TextObject("{=lOFR5sq6}Have you talked with {TARGET_MERCHANT.NAME}? It would be a damned waste if we waited too long and word of our plans leaked out.");
			TextObject textObject = new TextObject("{=cc4EEDMg}Splendid. Go have a word with {TARGET_MERCHANT.LINK}. [if:convo_focused_happy]If you can convince {?TARGET_MERCHANT.GENDER}her{?}him{\\?} to guide the caravan, we will wait in ambush along their route.");
			StringHelpers.SetCharacterProperties("TARGET_MERCHANT", _targetMerchantCharacter, textObject);
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(textObject).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.Consequence(OnQuestAccepted)
				.CloseDialog();
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(discussIntroDialogue).Condition(delegate
			{
				StringHelpers.SetCharacterProperties("TARGET_MERCHANT", _targetMerchantCharacter, discussIntroDialogue);
				return Hero.OneToOneConversationHero == base.QuestGiver;
			})
				.BeginPlayerOptions()
				.PlayerOption("{=YuabHAbV}I'll take care of it shortly..")
				.NpcLine("{=CDXUehf0}Good, good.")
				.CloseDialog()
				.PlayerOption("{=2haJj9mp}I have but I need to deal with some other problems before leading the caravan.")
				.NpcLine("{=bSDIHQzO}Please do so. Hate to have word leak out.[if:convo_nervous]")
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
		}

		private DialogFlow GetDialogueWithMerchant()
		{
			TextObject npcText = new TextObject("{=OJtUNAbN}Very well. You'll find the caravan [if:convo_calm_friendly]getting ready outside the gates. You will get your payment after the job. Good luck, friend.");
			return DialogFlow.CreateDialogFlow("hero_main_options", 125).BeginPlayerOptions().PlayerOption(new TextObject("{=K1ICRis9}I have heard you are looking for extra swords to protect your caravan. I am here to offer my services."))
				.Condition(() => Hero.OneToOneConversationHero == _targetMerchantCharacter.HeroObject && _caravanParty == null)
				.NpcLine("{=ltbu3S63}Yes, you have heard correctly. I am looking for a capable [if:convo_astonished]leader with a good number of followers. You only need to escort the caravan until they reach {TARGET_SETTLEMENT}. A simple job, but the cargo is very important. I'm willing to pay {MERCHANT_REWARD} denars. And of course, if you betrayed me...")
				.Condition(delegate
				{
					MBTextManager.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
					MBTextManager.SetTextVariable("MERCHANT_REWARD", Reward2);
					return true;
				})
				.Consequence(SpawnQuestParties)
				.BeginPlayerOptions()
				.PlayerOption("{=AGnd7nDb}Worry not. The outlaws in these parts know my name well, and fear it.")
				.NpcLine(npcText)
				.CloseDialog()
				.PlayerOption("{=RCsbpizl}If you have the denars we'll do the job.")
				.NpcLine(npcText)
				.CloseDialog()
				.PlayerOption("{=TfDomerj}I think my men and I are more than enough to protect the caravan, good {?TARGET_MERCHANT.GENDER}madam{?}sir{\\?}.")
				.Condition(delegate
				{
					StringHelpers.SetCharacterProperties("TARGET_MERCHANT", _targetMerchantCharacter);
					return true;
				})
				.NpcLine(npcText)
				.CloseDialog()
				.EndPlayerOptions()
				.EndPlayerOptions()
				.CloseDialog();
		}

		private DialogFlow GetDialogueWithCaravan()
		{
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine("{=Xs7Qweuw}Lead the way, {PLAYER.NAME}.").Condition(() => MobileParty.ConversationParty == _caravanParty && _caravanParty != null && !_canEncounterConversationStart)
				.Consequence(delegate
				{
					PlayerEncounter.LeaveEncounter = true;
				})
				.CloseDialog();
		}

		private DialogFlow GetDialogueWithGangWithoutCaravan()
		{
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine("{=F44s8kPB}Where is the caravan? My men can't wait here for too long.[if:convo_undecided_open]").Condition(() => MobileParty.ConversationParty == _gangParty && _gangParty != null && !_canEncounterConversationStart)
				.BeginPlayerOptions()
				.PlayerOption("{=Yqv1jk7D}Don't worry, they are coming towards our trap.")
				.NpcLine("{=fHc6fwrb}Good, let's finish this.")
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
		}

		private DialogFlow GetEncounterDialogue()
		{
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine("{=vVH7wT07}Who are these men? Be on your guard {PLAYER.NAME}, I smell trouble![if:convo_confused_annoyed]").Condition(() => MobileParty.ConversationParty == _caravanParty && _caravanParty != null && _canEncounterConversationStart)
				.Consequence(delegate
				{
					StringHelpers.SetCharacterProperties("TARGET_MERCHANT", _targetMerchantCharacter);
					AgentBuildData agentBuildData = new AgentBuildData(ConversationHelper.GetConversationCharacterPartyLeader(_gangParty.Party));
					agentBuildData.TroopOrigin(new SimpleAgentOrigin(agentBuildData.AgentCharacter));
					Vec3 vec = Agent.Main.LookDirection * 10f;
					vec.RotateAboutZ(1.3962634f);
					agentBuildData.InitialPosition(Agent.Main.Position + vec);
					agentBuildData.InitialDirection(-Agent.Main.LookDirection.AsVec2.Normalized());
					Agent item = Mission.Current.SpawnAgent(agentBuildData);
					Campaign.Current.ConversationManager.AddConversationAgents(new List<IAgent> { item }, setActionsInstantly: true);
				})
				.NpcLine("{=LJ2AoQyS}Well, well. What do we have here? Must be one of our lucky days, [if:convo_huge_smile]huh? Release all the valuables you carry and nobody gets hurt.", IsGangPartyLeader, IsCaravanMaster)
				.NpcLine("{=SdgDF4OZ}Hah! You're making a big mistake. See that group of men over there, [if:convo_excited]led by the warrior {PLAYER.NAME}? They're with us, and they'll cut you open.", IsCaravanMaster, IsGangPartyLeader)
				.NpcLine("{=LaHWB3r0}Oh… I'm afraid there's been a misunderstanding. {PLAYER.NAME} is with us, you see.[if:convo_evil_smile] Did {TARGET_MERCHANT.LINK} stuff you with lies and then send you out to your doom? Oh, shameful, shameful. {?TARGET_MERCHANT.GENDER}She{?}He{\\?} does that fairly often, unfortunately.", IsGangPartyLeader, IsCaravanMaster)
				.NpcLine("{=EGC4BA4h}{PLAYER.NAME}! Is this true? Look, you're a smart {?PLAYER.GENDER}woman{?}man{\\?}. [if:convo_shocked]You know that {TARGET_MERCHANT.LINK} can pay more than these scum. Take the money and keep your reputation.", IsCaravanMaster, IsMainHero)
				.NpcLine("{=zUKqWeUa}Come on, {PLAYER.NAME}. All this back-and-forth  is making me anxious. Let's finish this.[if:convo_nervous]", IsGangPartyLeader, IsMainHero)
				.BeginPlayerOptions()
				.PlayerOption("{=UEY5aQ2l}I'm here to rob {TARGET_MERCHANT.NAME}, not be {?TARGET_MERCHANT.GENDER}her{?}his{\\?} lackey. Now, cough up the goods or fight.", IsGangPartyLeader)
				.NpcLine("{=tHUHfe6C}You're with them? This is the basest treachery I have ever witnessed![if:convo_furious]", IsCaravanMaster, IsMainHero)
				.Consequence(delegate
				{
					AddLog(SidedWithGangLogText);
				})
				.NpcLine("{=IKeZLbIK}No offense, captain, but if that's the case you need to get out more. [if:convo_mocking_teasing]Anyway, shall we go to it?", IsGangPartyLeader, IsMainHero)
				.Consequence(delegate
				{
					StartBattle(SnareTheWealthyQuestChoice.SidedWithGang);
				})
				.CloseDialog()
				.PlayerOption("{=W7TD4yTc}You know, {TARGET_MERCHANT.NAME}'s man makes a good point. I'm guarding this caravan.", IsGangPartyLeader)
				.NpcLine("{=VXp0R7da}Heaven protect you! I knew you'd never be tempted by such a perfidious offer.[if:convo_huge_smile]", IsCaravanMaster, IsMainHero)
				.Consequence(delegate
				{
					AddLog(Fail2LogText);
				})
				.NpcLine("{=XJOqws2b}Hmf. A funny sense of honor you have… Anyway, I'm not going home empty handed, so let's do this.[if:convo_furious]", IsGangPartyLeader, IsMainHero)
				.Consequence(delegate
				{
					StartBattle(SnareTheWealthyQuestChoice.SidedWithCaravan);
				})
				.CloseDialog()
				.PlayerOption("{=ILrYPvTV}You know, I think I'd prefer to take all the loot for myself.", IsGangPartyLeader)
				.NpcLine("{=cpTMttNb}Is that so? Hey, caravan captain, whatever your name is… [if:convo_contemptuous]As long as we're all switching sides here, how about I join with you to defeat this miscreant who just betrayed both of us? Whichever of us comes out of this with the most men standing keeps your goods.", IsGangPartyLeader, IsMainHero)
				.Consequence(delegate
				{
					AddLog(Fail3LogText);
				})
				.NpcLine("{=15UCTrNA}I have no choice, do I? Well, better an honest robber than a traitor![if:convo_aggressive] Let's take {?PLAYER.GENDER}her{?}him{\\?} down.", IsCaravanMaster, IsMainHero)
				.Consequence(delegate
				{
					StartBattle(SnareTheWealthyQuestChoice.BetrayedBoth);
				})
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
		}

		private void OnQuestAccepted()
		{
			StartQuest();
			AddLog(QuestStartedLogText);
			AddTrackedObject(_targetMerchantCharacter.HeroObject);
			Campaign.Current.ConversationManager.AddDialogFlow(GetEncounterDialogue(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetDialogueWithMerchant(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetDialogueWithCaravan(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetDialogueWithGangWithoutCaravan(), this);
		}

		public void GetMountAndHarnessVisualIdsForQuestCaravan(CultureObject culture, out string mountStringId, out string harnessStringId)
		{
			if (culture.StringId == "khuzait" || culture.StringId == "aserai")
			{
				mountStringId = "camel";
				harnessStringId = "camel_saddle_b";
			}
			else
			{
				mountStringId = "mule";
				harnessStringId = "mule_load_c";
			}
		}

		private void SpawnQuestParties()
		{
			TextObject textObject = GameTexts.FindText("str_caravan_party_name");
			textObject.SetCharacterProperties("OWNER", _targetMerchantCharacter);
			GetMountAndHarnessVisualIdsForQuestCaravan(_targetMerchantCharacter.Culture, out var mountStringId, out var harnessStringId);
			PartyTemplateObject randomCaravanTemplate = CaravanHelper.GetRandomCaravanTemplate(_targetMerchantCharacter.Culture, isElite: false, isLand: true);
			_caravanParty = CustomPartyComponent.CreateCustomPartyWithTroopRoster(_targetMerchantCharacter.HeroObject.CurrentSettlement.GatePosition, 0.1f, _targetMerchantCharacter.HeroObject.CurrentSettlement, textObject, _targetMerchantCharacter.HeroObject.Clan, TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), _targetMerchantCharacter.HeroObject, mountStringId, harnessStringId, MobileParty.MainParty.Speed);
			MobilePartyHelper.FillPartyManuallyAfterCreation(_caravanParty, randomCaravanTemplate, CaravanPartyTroopCount);
			_caravanParty.MemberRoster.AddToCounts(_targetMerchantCharacter.Culture.CaravanMaster, 1);
			_caravanParty.ItemRoster.AddToCounts(Game.Current.ObjectManager.GetObject<ItemObject>("grain"), 40);
			_caravanParty.IgnoreByOtherPartiesTill(base.QuestDueTime);
			SetPartyAiAction.GetActionForEscortingParty(_caravanParty, MobileParty.MainParty, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
			_caravanParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
			_caravanParty.SetPartyUsedByQuest(isActivelyUsed: true);
			AddTrackedObject(_caravanParty);
			MobilePartyHelper.TryMatchPartySpeedWithItemWeight(_caravanParty, MobileParty.MainParty.Speed * 1.5f);
			Hideout closestHideout = SettlementHelper.FindNearestHideoutToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.Default, (Settlement x) => x.IsActive);
			Clan clan = Clan.BanditFactions.FirstOrDefault((Clan t) => t.Culture == closestHideout.Settlement.Culture);
			CampaignVec2 gatePosition = _targetSettlement.GatePosition;
			PartyTemplateObject partyTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("kingdom_hero_party_caravan_ambushers") ?? base.QuestGiver.Culture.BanditBossPartyTemplate;
			_gangParty = CustomPartyComponent.CreateCustomPartyWithTroopRoster(gatePosition, 0.1f, _targetSettlement, new TextObject("{=gJNdkwHV}Gang Party"), null, TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), base.QuestGiver);
			MobilePartyHelper.FillPartyManuallyAfterCreation(_gangParty, partyTemplate, GangPartyTroopCount);
			_gangParty.MemberRoster.AddToCounts(clan.Culture.BanditBoss, 1, insertAtFront: true);
			_gangParty.ItemRoster.AddToCounts(Game.Current.ObjectManager.GetObject<ItemObject>("grain"), 40);
			_gangParty.SetPartyUsedByQuest(isActivelyUsed: true);
			_gangParty.IgnoreByOtherPartiesTill(base.QuestDueTime);
			_gangParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
			_gangParty.Ai.DisableAi();
			MobilePartyHelper.TryMatchPartySpeedWithItemWeight(_gangParty, 0.2f);
			_gangParty.SetMoveGoToSettlement(_targetSettlement, MobileParty.NavigationType.Default, isTargetingThePort: false);
			EnterSettlementAction.ApplyForParty(_gangParty, _targetSettlement);
			AddTrackedObject(_targetSettlement);
			AddLog(TalkedToMerchantLogText);
		}

		private void StartBattle(SnareTheWealthyQuestChoice playerChoice)
		{
			_playerChoice = playerChoice;
			if (_caravanParty.MapEvent != null)
			{
				_caravanParty.MapEvent.FinalizeEvent();
			}
			Hideout closestHideout = SettlementHelper.FindNearestHideoutToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.Default, (Settlement x) => x.IsActive);
			Clan clan = Clan.BanditFactions.FirstOrDefault((Clan t) => t.Culture == closestHideout.Settlement.Culture);
			Clan actualClan = ((playerChoice != SnareTheWealthyQuestChoice.SidedWithCaravan) ? clan : _caravanParty.Owner.SupporterOf);
			_caravanParty.ActualClan = actualClan;
			Clan actualClan2 = ((playerChoice == SnareTheWealthyQuestChoice.SidedWithGang) ? base.QuestGiver.SupporterOf : clan);
			_gangParty.ActualClan = actualClan2;
			PartyBase attackerParty = ((playerChoice != SnareTheWealthyQuestChoice.SidedWithGang) ? _gangParty.Party : _caravanParty.Party);
			PlayerEncounter.Start();
			PlayerEncounter.Current.SetupFields(attackerParty, PartyBase.MainParty);
			PlayerEncounter.StartBattle();
			switch (playerChoice)
			{
			case SnareTheWealthyQuestChoice.BetrayedBoth:
				_caravanParty.MapEventSide = _gangParty.MapEventSide;
				break;
			case SnareTheWealthyQuestChoice.SidedWithCaravan:
				_caravanParty.MapEventSide = PartyBase.MainParty.MapEventSide;
				break;
			default:
				_gangParty.MapEventSide = PartyBase.MainParty.MapEventSide;
				break;
			}
		}

		private void StartEncounterDialogue()
		{
			if (_gangParty.CurrentSettlement != null)
			{
				LeaveSettlementAction.ApplyForParty(_gangParty);
			}
			PlayerEncounter.Finish();
			_canEncounterConversationStart = true;
			ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, noHorse: true);
			ConversationCharacterData conversationPartnerData = new ConversationCharacterData(ConversationHelper.GetConversationCharacterPartyLeader(_caravanParty.Party), _caravanParty.Party, noHorse: true, noWeapon: false, spawnAfterFight: false, isCivilianEquipmentRequiredForLeader: false, isCivilianEquipmentRequiredForBodyGuardCharacters: false, noBodyguards: true);
			CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData);
		}

		private void StartDialogueWithoutCaravan()
		{
			PlayerEncounter.Finish();
			ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, noHorse: true);
			ConversationCharacterData conversationPartnerData = new ConversationCharacterData(ConversationHelper.GetConversationCharacterPartyLeader(_gangParty.Party), _gangParty.Party, noHorse: true);
			CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData);
		}

		protected override void HourlyTick()
		{
			if (_caravanParty != null)
			{
				if (_caravanParty.DefaultBehavior != AiBehavior.EscortParty || _caravanParty.ShortTermBehavior != AiBehavior.EscortParty)
				{
					SetPartyAiAction.GetActionForEscortingParty(_caravanParty, MobileParty.MainParty, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
				}
				(_caravanParty.PartyComponent as CustomPartyComponent).CustomPartyBaseSpeed = MobileParty.MainParty.Speed;
				if (MobileParty.MainParty.TargetParty == _caravanParty)
				{
					_caravanParty.SetMoveModeHold();
					_isCaravanFollowing = false;
				}
				else if (!_isCaravanFollowing)
				{
					SetPartyAiAction.GetActionForEscortingParty(_caravanParty, MobileParty.MainParty, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
					_isCaravanFollowing = true;
				}
			}
		}

		private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			if (settlement == base.QuestGiver.CurrentSettlement)
			{
				if (newOwner.Clan == Clan.PlayerClan)
				{
					OnCancel4();
				}
				else
				{
					OnCancel2();
				}
			}
		}

		public void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail reason)
		{
			if ((faction1 == base.QuestGiver.MapFaction && faction2 == Hero.MainHero.MapFaction) || (faction2 == base.QuestGiver.MapFaction && faction1 == Hero.MainHero.MapFaction))
			{
				OnCancel1();
			}
		}

		public void OnVillageStateChanged(Village village, Village.VillageStates oldState, Village.VillageStates newState, MobileParty raiderParty)
		{
			if (village == _targetSettlement.Village && newState != Village.VillageStates.Normal)
			{
				OnCancel3();
			}
		}

		public void OnMapEventEnded(MapEvent mapEvent)
		{
			if (!mapEvent.IsPlayerMapEvent || _caravanParty == null)
			{
				return;
			}
			if (mapEvent.InvolvedParties.Contains(_caravanParty.Party))
			{
				if (!mapEvent.InvolvedParties.Contains(_gangParty.Party))
				{
					OnFail1();
				}
				else if (mapEvent.WinningSide == mapEvent.PlayerSide)
				{
					if (_playerChoice == SnareTheWealthyQuestChoice.SidedWithGang)
					{
						OnSuccess1();
					}
					else if (_playerChoice == SnareTheWealthyQuestChoice.SidedWithCaravan)
					{
						OnFail2();
					}
					else
					{
						OnFail3();
					}
				}
				else if (_playerChoice == SnareTheWealthyQuestChoice.SidedWithGang)
				{
					OnFail4();
				}
				else if (_playerChoice == SnareTheWealthyQuestChoice.SidedWithCaravan)
				{
					OnFail5();
				}
				else
				{
					OnFail6();
				}
			}
			else
			{
				OnFail1();
			}
		}

		private void OnPartyJoinedArmy(MobileParty mobileParty)
		{
			if (mobileParty == MobileParty.MainParty && _caravanParty != null)
			{
				OnFail1();
			}
		}

		private void OnGameMenuOpened(MenuCallbackArgs args)
		{
			if (_startConversationDelegate != null && MobileParty.MainParty.CurrentSettlement == _targetSettlement && _caravanParty != null)
			{
				_startConversationDelegate();
				_startConversationDelegate = null;
			}
		}

		public void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
		{
			if (party == MobileParty.MainParty && settlement == _targetSettlement && _caravanParty != null)
			{
				if (_caravanParty.Position.DistanceSquared(_targetSettlement.Position) <= CaravanEncounterStartDistance)
				{
					_startConversationDelegate = StartEncounterDialogue;
				}
				else
				{
					_startConversationDelegate = StartDialogueWithoutCaravan;
				}
			}
		}

		public void OnSettlementLeft(MobileParty party, Settlement settlement)
		{
			if (party == MobileParty.MainParty && _caravanParty != null)
			{
				SetPartyAiAction.GetActionForEscortingParty(_caravanParty, MobileParty.MainParty, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
			}
		}

		private void CanHeroBecomePrisoner(Hero hero, ref bool result)
		{
			if (hero == Hero.MainHero && _playerChoice != SnareTheWealthyQuestChoice.None)
			{
				result = false;
			}
		}

		protected override void OnFinalize()
		{
			if (_caravanParty != null && _caravanParty.IsActive)
			{
				DestroyPartyAction.Apply(null, _caravanParty);
			}
			if (_gangParty != null && _gangParty.IsActive)
			{
				DestroyPartyAction.Apply(null, _gangParty);
			}
		}

		private void OnSuccess1()
		{
			AddLog(Success1LogText);
			TraitLevelingHelper.OnIssueSolvedThroughQuest(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -100)
			});
			TraitLevelingHelper.OnIssueSolvedThroughQuest(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Calculating, 50)
			});
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, 5);
			ChangeRelationAction.ApplyPlayerRelation(_targetMerchantCharacter.HeroObject, -10);
			base.QuestGiver.AddPower(30f);
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, Reward1);
			CompleteQuestWithSuccess();
		}

		private void OnTimedOutWithoutTalkingToMerchant()
		{
			AddLog(TimedOutWithoutTalkingToMerchantText);
			TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -50)
			});
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -5);
		}

		private void OnFail1()
		{
			ApplyFail1Consequences();
			CompleteQuestWithFail();
		}

		private void ApplyFail1Consequences()
		{
			AddLog(Fail1LogText);
			TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -50)
			});
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -5);
			ChangeRelationAction.ApplyPlayerRelation(_targetMerchantCharacter.HeroObject, -5);
		}

		private void OnFail2()
		{
			AddLog(Fail2OutcomeLogText);
			TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 100)
			});
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -10);
			ChangeRelationAction.ApplyPlayerRelation(_targetMerchantCharacter.HeroObject, 5);
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, Reward2);
			CompleteQuestWithBetrayal();
		}

		private void OnFail3()
		{
			AddLog(Fail3OutcomeLogText);
			TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -200)
			});
			TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Calculating, 100)
			});
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -15);
			ChangeRelationAction.ApplyPlayerRelation(_targetMerchantCharacter.HeroObject, -20);
			CompleteQuestWithBetrayal();
		}

		private void OnFail4()
		{
			AddLog(Fail4LogText);
			TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -100)
			});
			TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Calculating, 100)
			});
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -10);
			ChangeRelationAction.ApplyPlayerRelation(_targetMerchantCharacter.HeroObject, -10);
			CompleteQuestWithFail();
		}

		private void OnFail5()
		{
			AddLog(Fail5LogText);
			TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -100)
			});
			TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Calculating, 100)
			});
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -10);
			ChangeRelationAction.ApplyPlayerRelation(_targetMerchantCharacter.HeroObject, -10);
			CompleteQuestWithBetrayal();
		}

		private void OnFail6()
		{
			AddLog(Fail6LogText);
			TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -200)
			});
			TraitLevelingHelper.OnIssueFailed(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Calculating, 100)
			});
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -15);
			ChangeRelationAction.ApplyPlayerRelation(_targetMerchantCharacter.HeroObject, -20);
			CompleteQuestWithBetrayal();
		}

		protected override void OnTimedOut()
		{
			if (_caravanParty == null)
			{
				OnTimedOutWithoutTalkingToMerchant();
			}
			else
			{
				ApplyFail1Consequences();
			}
		}

		private void OnCancel1()
		{
			AddLog(WarDeclaredBetweenPlayerAndQuestGiverLogText);
			CompleteQuestWithCancel();
		}

		private void OnCancel2()
		{
			AddLog(QuestSettlementWasCapturedLogText);
			CompleteQuestWithCancel();
		}

		private void OnCancel3()
		{
			AddLog(TargetSettlementRaidedLogText);
			CompleteQuestWithCancel();
		}

		private void OnCancel4()
		{
			AddLog(PlayerCapturedQuestSettlementLogText);
			CompleteQuestWithCancel();
		}

		private bool IsGangPartyLeader(IAgent agent)
		{
			return agent.Character == ConversationHelper.GetConversationCharacterPartyLeader(_gangParty.Party);
		}

		private bool IsCaravanMaster(IAgent agent)
		{
			return agent.Character == ConversationHelper.GetConversationCharacterPartyLeader(_caravanParty.Party);
		}

		private bool IsMainHero(IAgent agent)
		{
			return agent.Character == CharacterObject.PlayerCharacter;
		}

		public override void OnHeroCanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
		{
			if (hero == _targetMerchantCharacter.HeroObject)
			{
				result = false;
			}
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
			CampaignEvents.VillageStateChanged.AddNonSerializedListener(this, OnVillageStateChanged);
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
			CampaignEvents.OnPartyJoinedArmyEvent.AddNonSerializedListener(this, OnPartyJoinedArmy);
			CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
			CampaignEvents.CanHeroBecomePrisonerEvent.AddNonSerializedListener(this, CanHeroBecomePrisoner);
			CampaignEvents.CanHaveCampaignIssuesEvent.AddNonSerializedListener(this, OnHeroCanHaveCampaignIssuesInfoIsRequested);
		}

		internal static void AutoGeneratedStaticCollectObjectsSnareTheWealthyIssueQuest(object o, List<object> collectedObjects)
		{
			((SnareTheWealthyIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_targetMerchantCharacter);
			collectedObjects.Add(_targetSettlement);
			collectedObjects.Add(_caravanParty);
			collectedObjects.Add(_gangParty);
		}

		internal static object AutoGeneratedGetMemberValue_targetMerchantCharacter(object o)
		{
			return ((SnareTheWealthyIssueQuest)o)._targetMerchantCharacter;
		}

		internal static object AutoGeneratedGetMemberValue_targetSettlement(object o)
		{
			return ((SnareTheWealthyIssueQuest)o)._targetSettlement;
		}

		internal static object AutoGeneratedGetMemberValue_caravanParty(object o)
		{
			return ((SnareTheWealthyIssueQuest)o)._caravanParty;
		}

		internal static object AutoGeneratedGetMemberValue_gangParty(object o)
		{
			return ((SnareTheWealthyIssueQuest)o)._gangParty;
		}

		internal static object AutoGeneratedGetMemberValue_questDifficulty(object o)
		{
			return ((SnareTheWealthyIssueQuest)o)._questDifficulty;
		}

		internal static object AutoGeneratedGetMemberValue_playerChoice(object o)
		{
			return ((SnareTheWealthyIssueQuest)o)._playerChoice;
		}

		internal static object AutoGeneratedGetMemberValue_canEncounterConversationStart(object o)
		{
			return ((SnareTheWealthyIssueQuest)o)._canEncounterConversationStart;
		}

		internal static object AutoGeneratedGetMemberValue_isCaravanFollowing(object o)
		{
			return ((SnareTheWealthyIssueQuest)o)._isCaravanFollowing;
		}
	}

	private const IssueBase.IssueFrequency SnareTheWealthyIssueFrequency = IssueBase.IssueFrequency.Rare;

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
	}

	private void OnCheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero))
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnStartIssue, typeof(SnareTheWealthyIssue), IssueBase.IssueFrequency.Rare));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(SnareTheWealthyIssue), IssueBase.IssueFrequency.Rare));
		}
	}

	private bool ConditionsHold(Hero issueGiver)
	{
		if (issueGiver.IsGangLeader && issueGiver.CurrentSettlement != null && issueGiver.CurrentSettlement.IsTown && !issueGiver.CurrentSettlement.HasPort && issueGiver.CurrentSettlement.Town.Security <= 50f)
		{
			return GetTargetMerchant(issueGiver) != null;
		}
		return false;
	}

	private Hero GetTargetMerchant(Hero issueOwner)
	{
		Hero result = null;
		foreach (Hero notable in issueOwner.CurrentSettlement.Notables)
		{
			if (notable != issueOwner && notable.IsMerchant && notable.Power >= 150f && notable.GetTraitLevel(DefaultTraits.Mercy) + notable.GetTraitLevel(DefaultTraits.Honor) < 0 && notable.CanHaveCampaignIssues() && !Campaign.Current.IssueManager.HasIssueCoolDown(typeof(SnareTheWealthyIssue), notable) && !Campaign.Current.IssueManager.HasIssueCoolDown(typeof(EscortMerchantCaravanIssueBehavior), notable) && !Campaign.Current.IssueManager.HasIssueCoolDown(typeof(CaravanAmbushIssueBehavior), notable))
			{
				result = notable;
				break;
			}
		}
		return result;
	}

	private IssueBase OnStartIssue(in PotentialIssueData pid, Hero issueOwner)
	{
		Hero targetMerchant = GetTargetMerchant(issueOwner);
		return new SnareTheWealthyIssue(issueOwner, targetMerchant.CharacterObject);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
