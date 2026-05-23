using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
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

public class CaravanAmbushIssueBehavior : CampaignBehaviorBase
{
	public class CaravanAmbushIssue : IssueBase
	{
		private const float CaravanAmbushIssueDurationInDays = 20f;

		private const int AlternativeSolutionMinimumTroopTier = 2;

		private const int AlternativeSolutionRenownReward = 3;

		private const int AlternativeSolutionRelationReward = 5;

		private const int AlternativeSolutionRelationPenalty = -5;

		private const int CaravanAmbushIssueNotableMinimumRelation = -10;

		private const int CompanionSkill = 120;

		private const int MinimumRequiredMenCount = 30;

		[SaveableField(1)]
		private readonly Settlement _targetSettlement;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.Casualties | AlternativeSolutionScaleFlag.FailureRisk;

		public override TextObject IssueBriefByIssueGiver => new TextObject("{=kOxu3Lw0}Yes... I run caravans, as you may know. I lose a few to bandits from time to time, but generally my caravans are sufficiently well guarded to scare off the small gangs and move quickly enough to outrun the big ones.The problem is that there's a new bandit chief out there who knows his business, who has outfitted his men with horses and uses proper cavalry tactics.I’ve lost three caravans in a row, and I can’t afford to keep this up for long.[if:convo_stern][ib:hip]");

		public override TextObject IssueAcceptByPlayer => new TextObject("{=aJcChHfj}What are you planning to do about them?");

		protected override int CompanionSkillRewardXP => (int)(600f + 800f * base.IssueDifficultyMultiplier);

		public override TextObject IssueQuestSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=iWWKTOik}I've got a trick up my sleeve. We'll bait them. I've paid some of my workers to spread rumors about a particularly fat caravan laden with silverware heading out towards {TARGET_SETTLEMENT}. It is a trap, of course. I've got a bunch of mercenaries going with it, disguised as packers. But they could use some backup. Go and follow my caravan. Stay at a proper distance, until they are attacked. Then move in to finish the bandits once and for all. My caravan master will pay you {REWARD}{GOLD_ICON} when the fight is over.[if:convo_mocking_revenge][ib:confident2]");
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("REWARD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		public override TextObject IssueQuestSolutionAcceptByPlayer => new TextObject("{=Ov3b2b8p}I'll help you myself.");

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=jiFCxZ4B}In that case you should send a good commander with some {TROOP_COUNT} men, just to be safe. And I'll send them back to you in {RETURN_DAYS} days. [if:convo_normal][ib:closed]");
				textObject.SetTextVariable("TROOP_COUNT", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=XLswis9W}I will lend you one of my best lieutenants and {TROOP_COUNT} men.");
				textObject.SetTextVariable("TROOP_COUNT", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		public override TextObject IssueDiscussAlternativeSolution => new TextObject("{=L3U98ygQ}We're still preparing the ambush. I hope to have your men back to you shortly.");

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver => new TextObject("{=Y9LNbRho}Thank you. I will put your men to good use.");

		public override bool IsThereLordSolution => false;

		public override TextObject Title => new TextObject("{=wF7uiYzy}Caravan Ambush");

		public override TextObject Description
		{
			get
			{
				TextObject textObject = new TextObject("{=H3B75sYi}A merchant asked you to follow a fake caravan that was sent out as a trap to destroy a particularly large and dangerous group of bandits.");
				StringHelpers.SetCharacterProperties("NOTABLE", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=YbZYXRqt}{QUEST_GIVER.LINK} asked you to follow a caravan that he sent out as bait to destroy a particularly large and dangerous group of bandits. You ordered {COMPANION.LINK} and {TROOP_COUNT} of your men to follow the caravan from a safe distance and join in the fight once it is attacked. You expect them to return in {RETURN_DAYS} days with the news of success and {REWARD_GOLD}{GOLD_ICON}.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				textObject.SetTextVariable("REWARD_GOLD", RewardGold);
				textObject.SetTextVariable("TROOP_COUNT", AlternativeSolutionSentTroops.TotalManCount - 1);
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=PHAm9BIp}{COMPANION.LINK} and the men you sent with {?COMPANION.GENDER}her{?}him{\\?} successfully protected the caravan. {QUEST_GIVER.LINK} is happy and sends you {?QUES_GIVER.GENDER}her{?}him{\\?} regards with {REWARD_GOLD}{GOLD_ICON} he promised.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				textObject.SetTextVariable("REWARD_GOLD", RewardGold);
				return textObject;
			}
		}

		public override TextObject IssuePlayerResponseAfterAlternativeExplanation => new TextObject("{=DAYaprEi}Maybe I'll send someone to look.");

		public override bool IsThereAlternativeSolution => true;

		public override int AlternativeSolutionBaseNeededMenCount => 22 + MathF.Ceiling(30f * base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 3 + MathF.Ceiling(5f * base.IssueDifficultyMultiplier);

		protected override int RewardGold => (int)(1000f + 3000f * base.IssueDifficultyMultiplier);

		internal static void AutoGeneratedStaticCollectObjectsCaravanAmbushIssue(object o, List<object> collectedObjects)
		{
			((CaravanAmbushIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_targetSettlement);
		}

		internal static object AutoGeneratedGetMemberValue_targetSettlement(object o)
		{
			return ((CaravanAmbushIssue)o)._targetSettlement;
		}

		public CaravanAmbushIssue(Hero issueOwner, Settlement targetSettlement)
			: base(issueOwner, CampaignTime.DaysFromNow(20f))
		{
			_targetSettlement = targetSettlement;
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.SettlementProsperity)
			{
				return -0.3f;
			}
			if (issueEffect == DefaultIssueEffects.SettlementSecurity)
			{
				return -1f;
			}
			if (issueEffect == DefaultIssueEffects.IssueOwnerPower)
			{
				return -0.2f;
			}
			return 0f;
		}

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			return ((hero.GetSkillValue(DefaultSkills.Tactics) >= hero.GetSkillValue(DefaultSkills.Roguery)) ? DefaultSkills.Tactics : DefaultSkills.Roguery, 120);
		}

		protected override void OnGameLoad()
		{
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new CaravanAmbushIssueQuest("caravan_ambush_quest_" + CampaignTime.Now.ElapsedSecondsUntilNow, base.IssueOwner, _targetSettlement, CampaignTime.DaysFromNow(20f), RewardGold, base.IssueDifficultyMultiplier);
		}

		public override IssueFrequency GetFrequency()
		{
			return IssueFrequency.Common;
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
			if (issueGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				flag |= PreconditionFlags.AtWar;
			}
			if (MobileParty.MainParty.MemberRoster.TotalHealthyCount < 30)
			{
				flag |= PreconditionFlags.NotEnoughTroops;
			}
			return flag == PreconditionFlags.None;
		}

		public override bool IssueStayAliveConditions()
		{
			if (base.IssueOwner != null && base.IssueOwner.OwnedCaravans.Count > 0)
			{
				return !base.IssueOwner.MapFaction.IsAtWarWith(Clan.PlayerClan);
			}
			return false;
		}

		public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
		{
			return character.Tier >= 2;
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}

		public override bool AlternativeSolutionCondition(out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2);
		}

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			base.AlternativeSolutionHero.AddSkillXp(DefaultSkills.Scouting, (int)(600f + 800f * base.IssueDifficultyMultiplier));
			float randomFloat = MBRandom.RandomFloat;
			SkillObject skill = ((randomFloat > 0.66f) ? DefaultSkills.OneHanded : ((!(randomFloat <= 0.66f) || !(randomFloat > 0.33f)) ? DefaultSkills.Polearm : DefaultSkills.TwoHanded));
			base.AlternativeSolutionHero.AddSkillXp(skill, (int)(600f + 800f * base.IssueDifficultyMultiplier));
			Clan.PlayerClan.AddRenown(3f);
			RelationshipChangeWithIssueOwner = 5;
		}

		protected override void AlternativeSolutionEndWithFailureConsequence()
		{
			RelationshipChangeWithIssueOwner = -5;
			base.IssueOwner.AddPower(-5f);
		}
	}

	public class CaravanAmbushIssueQuest : QuestBase
	{
		private const int VicinityCheckFailedRelationPenalty = -5;

		private const int VicinityCheckFailedPowerPenalty = -5;

		private const int CaravanDestroyedRelationPenalty = -5;

		private const int CaravanDestroyedPowerPenalty = -5;

		private const int TimeoutRelationPenalty = -5;

		private const int TimeoutPowerPenalty = -5;

		private const int QuestSucceededRelationReward = 5;

		private const int QuestSucceededRenownReward = 3;

		private const int VicinityCheckFailThreshold = 4;

		private const int NumberOfRandomRewardItems = 3;

		private const float MapEventInvulnerabilityDurationInHours = 6f;

		private const float CaravanMainPartySpeedRatio = 0.7f;

		[SaveableField(1)]
		private readonly Settlement _targetSettlement;

		[SaveableField(2)]
		private readonly float _issueDifficulty;

		[SaveableField(3)]
		private MobileParty _caravanParty;

		[SaveableField(4)]
		private MobileParty _banditParty;

		[SaveableField(5)]
		private int _vicinityCheckFailCounter;

		[SaveableField(6)]
		private List<ItemObject> _rewardItems = new List<ItemObject>();

		[SaveableField(7)]
		private bool _isCaravanSaved;

		[SaveableField(8)]
		private CampaignTime _vicinityCheckDisabledUntil;

		[SaveableField(10)]
		private bool _isCaravanWaitingForEscort;

		private float PartyEscortOuterRadius => Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius * 2.36f;

		private float PartyEscortInnerRadius => Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius * 2.1f;

		private float VicinityCheckDistance => Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius;

		private int CaravanPartyTroopCount => 22 + MathF.Ceiling(30f * _issueDifficulty);

		private int BanditPartyTroopCount => 25 + MathF.Ceiling(50f * _issueDifficulty);

		public override TextObject Title => new TextObject("{=wF7uiYzy}Caravan Ambush");

		public override bool IsRemainingTimeHidden => false;

		private TextObject CaravanAmbushIssueQuestActivatedLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=S4kpdrgw}{QUEST_GIVER.LINK}, {?IS_ARTISAN}an artisan{?}a merchant{\\?} from {SETTLEMENT}, asked you to follow a fake caravan that was bait for a particularly large and dangerous group of bandits. {?QUEST_GIVER.GENDER}She{?}He{\\?} suspects this fake caravan will be attacked on its way to {TARGET_SETTLEMENT}, so {?QUEST_GIVER.GENDER}she{?}he{\\?} wants you to follow the caravan from a safe distance and join in the fight once it is attacked. If you succeed, {?QUEST_GIVER.GENDER}she{?}he{\\?} promised to pay you {REWARD_GOLD}{GOLD_ICON}. ");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("IS_ARTISAN", base.QuestGiver.IsArtisan ? 1 : 0);
				textObject.SetTextVariable("SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("REWARD_GOLD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		private TextObject CaravanAmbushIssueQuestSucceededLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=g5bRX0dd}You have defeated the large group of bandits that {QUEST_GIVER.LINK} mentioned and {?QUEST_GIVER.GENDER}she{?}he{\\?} sends {?QUEST_GIVER.GENDER}her{?}his{\\?} regards with the {REWARD_GOLD}{GOLD_ICON} {?QUEST_GIVER.GENDER}she{?}he{\\?} promised and some trade goods as reward.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("REWARD_GOLD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		private TextObject CaravanAmbushIssueQuestVicinityCheckFailedLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=DbXMSRmA}You got too close to the caravan. The bandits saw you and withdrew. {QUEST_GIVER.LINK}'s plan failed and {?QUEST_GIVER.GENDER}she{?}he{\\?} will be very upset.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject CaravanAmbushIssueQuestCaravanDestroyedLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=WtCSdcs9}You have failed to defeat the bandits, as {QUEST_GIVER.LINK} asked you to do.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject CaravanAmbushIssueQuestTimeOutLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=qi0wKvPX}You failed to catch up to the caravan before it was overwhelmed. {QUEST_GIVER.LINK} will be very upset about this.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject CaravanSurvivedWithoutHelpLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=UFce0iyy}The caravan survived the battle without your help. You failed to keep your promise to {QUEST_GIVER.LINK}.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject CaravanAmbushIssueQuestHiredBanditsLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=bdab7SmZ}You recruited the bandits who were giving {QUEST_GIVER.LINK} trouble. {?QUEST_GIVER.GENDER}she{?}he{\\?} is satisfied with this outcome, and sends you {REWARD_GOLD}{GOLD_ICON} that {?QUEST_GIVER.GENDER}she{?}he{\\?} promised.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("REWARD_GOLD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		private TextObject CaravanAmbushWarDeclaredCancelLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=dQl0Bnwm}Your clan is now at war with the {QUEST_GIVER.LINK}'s faction. Your agreement with {QUEST_GIVER.LINK} has been canceled.");
				textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
				return textObject;
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsCaravanAmbushIssueQuest(object o, List<object> collectedObjects)
		{
			((CaravanAmbushIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_targetSettlement);
			collectedObjects.Add(_caravanParty);
			collectedObjects.Add(_banditParty);
			collectedObjects.Add(_rewardItems);
			CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(_vicinityCheckDisabledUntil, collectedObjects);
		}

		internal static object AutoGeneratedGetMemberValue_targetSettlement(object o)
		{
			return ((CaravanAmbushIssueQuest)o)._targetSettlement;
		}

		internal static object AutoGeneratedGetMemberValue_issueDifficulty(object o)
		{
			return ((CaravanAmbushIssueQuest)o)._issueDifficulty;
		}

		internal static object AutoGeneratedGetMemberValue_caravanParty(object o)
		{
			return ((CaravanAmbushIssueQuest)o)._caravanParty;
		}

		internal static object AutoGeneratedGetMemberValue_banditParty(object o)
		{
			return ((CaravanAmbushIssueQuest)o)._banditParty;
		}

		internal static object AutoGeneratedGetMemberValue_vicinityCheckFailCounter(object o)
		{
			return ((CaravanAmbushIssueQuest)o)._vicinityCheckFailCounter;
		}

		internal static object AutoGeneratedGetMemberValue_rewardItems(object o)
		{
			return ((CaravanAmbushIssueQuest)o)._rewardItems;
		}

		internal static object AutoGeneratedGetMemberValue_isCaravanSaved(object o)
		{
			return ((CaravanAmbushIssueQuest)o)._isCaravanSaved;
		}

		internal static object AutoGeneratedGetMemberValue_vicinityCheckDisabledUntil(object o)
		{
			return ((CaravanAmbushIssueQuest)o)._vicinityCheckDisabledUntil;
		}

		internal static object AutoGeneratedGetMemberValue_isCaravanWaitingForEscort(object o)
		{
			return ((CaravanAmbushIssueQuest)o)._isCaravanWaitingForEscort;
		}

		public CaravanAmbushIssueQuest(string questId, Hero questGiver, Settlement targetSettlement, CampaignTime duration, int rewardGold, float issueDifficulty)
			: base(questId, questGiver, duration, rewardGold)
		{
			_targetSettlement = targetSettlement;
			_issueDifficulty = issueDifficulty;
			SetDialogs();
			InitializeQuestOnCreation();
		}

		protected override void SetDialogs()
		{
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine("{=1sbbbOyr}Excellent... I'm counting on you! The caravan will be leaving soon.[if:convo_normal][ib:hip]").Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.Consequence(OnQuestAccepted)
				.CloseDialog();
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine("{=5o9udV96}Yes? You should go already. The caravan is on its way.[if:convo_annoyed][ib:normal2]").Condition(() => Hero.OneToOneConversationHero == base.QuestGiver && !_isCaravanSaved)
				.BeginPlayerOptions()
				.PlayerOption("{=DKiLA9f2}Don't worry, I'll find them.")
				.NpcLine("{=ddEu5IFQ}I hope so.")
				.CloseDialog()
				.PlayerOption("{=zpqP5LsC}I'll go right away.")
				.NpcLine("{=3ssQAe1t}Good to hear that")
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
			Campaign.Current.ConversationManager.AddDialogFlow(GetCaravaneerDialogFlow(), this);
		}

		private DialogFlow GetCaravaneerDialogFlow()
		{
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine("{=LqEdr7sQ}Thank you, {?PLAYER.GENDER}milady{?}sir{\\?}. {QUEST_GIVER.LINK} had informed me that help would be on the way. We needed it, I think. Those were a pretty tough lot.").Condition(delegate
			{
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
				return CharacterObject.OneToOneConversationCharacter == ConversationHelper.GetConversationCharacterPartyLeader(_caravanParty.Party) && _isCaravanSaved;
			})
				.PlayerLine("{=MKbLhn9d}I'm glad we caught up to you in time.")
				.NpcLine("{=yxg91L0a}We'll tell everyone what you did.[if:convo_happy][ib:normal2] Please take some of these goods in compensation. We have no intention to sell them anyway. Safe travels, {?PLAYER.GENDER}milady{?}sir{\\?}.")
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += OnQuestSucceeded;
				})
				.CloseDialog();
		}

		private void OnQuestAccepted()
		{
			StartQuest();
			AddLog(CaravanAmbushIssueQuestActivatedLogText);
			ItemRoster itemRoster = new ItemRoster();
			itemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("fish"), 20);
			itemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("grain"), 40);
			itemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("butter"), 20);
			itemRoster.AddToCounts(DefaultItems.HardWood, 60);
			PartyTemplateObject randomCaravanTemplate = CaravanHelper.GetRandomCaravanTemplate(base.QuestGiver.Culture, isElite: false, isLand: true);
			_caravanParty = CaravanPartyComponent.CreateCaravanParty(base.QuestGiver, base.QuestGiver.CurrentSettlement, randomCaravanTemplate, isInitialSpawn: false, null, itemRoster);
			_caravanParty.MemberRoster.Clear();
			_caravanParty.MemberRoster.AddToCounts(base.QuestGiver.Culture.CaravanMaster, 1);
			_caravanParty.MemberRoster.AddToCounts(base.QuestGiver.Culture.BasicTroop, CaravanPartyTroopCount);
			_caravanParty.IgnoreByOtherPartiesTill(base.QuestDueTime);
			Campaign.Current.MobilePartyLocator.UpdateLocator(_caravanParty);
			SetPartyAiAction.GetActionForVisitingSettlement(_caravanParty, _targetSettlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
			_caravanParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
			_caravanParty.SetPartyUsedByQuest(isActivelyUsed: true);
			AddTrackedObject(_caravanParty);
			MobilePartyHelper.TryMatchPartySpeedWithItemWeight(_caravanParty, MobileParty.MainParty.Speed * 0.7f);
			Hideout hideout = SettlementHelper.FindNearestHideoutToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.Default, (Settlement x) => x.IsActive);
			Clan clan = Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "looters");
			PartyTemplateObject partyTemplateObject = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("kingdom_hero_party_caravan_ambushers") ?? clan.DefaultPartyTemplate;
			_banditParty = BanditPartyComponent.CreateBanditParty("caravan_ambush_quest_" + clan.Name, clan, hideout.Settlement.Hideout, isBossParty: false, partyTemplateObject, _targetSettlement.GatePosition);
			_banditParty.Party.SetCustomName(new TextObject("{=u1Pkt4HC}Raiders"));
			Campaign.Current.MobilePartyLocator.UpdateLocator(_banditParty);
			_banditParty.MemberRoster.Clear();
			_banditParty.SetPartyUsedByQuest(isActivelyUsed: true);
			AddTrackedObject(_banditParty);
			for (int num = 0; num < BanditPartyTroopCount; num++)
			{
				List<(PartyTemplateStack, float)> list = new List<(PartyTemplateStack, float)>();
				foreach (PartyTemplateStack stack in partyTemplateObject.Stacks)
				{
					list.Add((stack, 64 - stack.Character.Level));
				}
				PartyTemplateStack partyTemplateStack = MBRandom.ChooseWeighted(list);
				_banditParty.MemberRoster.AddToCounts(partyTemplateStack.Character, 1);
			}
			_banditParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("sumpter_horse"), BanditPartyTroopCount / 4);
			_banditParty.IgnoreByOtherPartiesTill(base.QuestDueTime);
			SetPartyAiAction.GetActionForEngagingParty(_banditParty, _caravanParty, MobileParty.NavigationType.Default, isFromPort: false);
			_banditParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
			for (int num2 = 0; num2 < 3; num2++)
			{
				_rewardItems.Add(Items.All.GetRandomElementWithPredicate((ItemObject t) => t.IsTradeGood && !t.NotMerchandise));
			}
			_vicinityCheckDisabledUntil = CampaignTime.HoursFromNow(1f);
		}

		private void OnQuestSucceeded()
		{
			AddLog(CaravanAmbushIssueQuestSucceededLogText);
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
			RelationshipChangeWithQuestGiver = 5;
			Clan.PlayerClan.AddRenown(3f);
			foreach (ItemObject rewardItem in _rewardItems)
			{
				MobileParty.MainParty.ItemRoster.AddToCounts(rewardItem, 1);
			}
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.LeaveEncounter = true;
			}
			CompleteQuestWithSuccess();
		}

		private void OnPlayerHiredBandits()
		{
			AddLog(CaravanAmbushIssueQuestHiredBanditsLogText);
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
			RelationshipChangeWithQuestGiver = 5;
			Clan.PlayerClan.AddRenown(3f);
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.LeaveEncounter = true;
			}
			CompleteQuestWithSuccess();
		}

		protected override void HourlyTick()
		{
			if (_caravanParty == null || _banditParty == null || !base.IsOngoing)
			{
				return;
			}
			if (_caravanParty.MapEvent == null && !_isCaravanSaved)
			{
				if (_caravanParty.Position.DistanceSquared(_banditParty.Position) <= Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius * 2.2f)
				{
					EncounterManager.StartPartyEncounter(_banditParty.Party, _caravanParty.Party);
					return;
				}
				if (_caravanParty.Position.DistanceSquared(base.QuestGiver.CurrentSettlement.Position) >= Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Default) * 0.5f && _caravanParty.Position.DistanceSquared(MobileParty.MainParty.Position) <= VicinityCheckDistance * VicinityCheckDistance * 2f && _vicinityCheckDisabledUntil.IsPast)
				{
					_vicinityCheckFailCounter++;
					if (_vicinityCheckFailCounter == 3)
					{
						_vicinityCheckDisabledUntil = CampaignTime.HoursFromNow(2.5f);
						MBInformationManager.AddQuickInformation(new TextObject("{=uD2pfRAh}Get back immediately! If you keep this close to the caravan the ambushers will certainly notice you."));
					}
					else if (_vicinityCheckFailCounter < 4)
					{
						_vicinityCheckDisabledUntil = CampaignTime.HoursFromNow(1.5f);
						MBInformationManager.AddQuickInformation(new TextObject("{=ki1CWgcP}Warning! You are too close to the caravan. Stay a bit farther away."));
					}
					if (_vicinityCheckFailCounter >= 4)
					{
						OnFailedVicinityChecks();
					}
				}
				UtilizePartyEscortBehavior(_caravanParty, MobileParty.MainParty, ref _isCaravanWaitingForEscort, PartyEscortInnerRadius, PartyEscortOuterRadius, ResumeCaravanMovement);
			}
			if (_caravanParty.MapEvent != null && _caravanParty.MapEvent.IsInvulnerable && _caravanParty.MapEvent.BattleStartTime.ElapsedHoursUntilNow > 6f)
			{
				_caravanParty.MapEvent.IsInvulnerable = false;
			}
		}

		private void ResumeCaravanMovement()
		{
			SetPartyAiAction.GetActionForVisitingSettlement(_caravanParty, _targetSettlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
		}

		private void OnFailedVicinityChecks()
		{
			AddLog(CaravanAmbushIssueQuestVicinityCheckFailedLogText);
			RelationshipChangeWithQuestGiver = -5;
			base.QuestGiver.AddPower(-5f);
			HandlePartyAiAfterCompletion();
			CompleteQuestWithFail();
		}

		protected override void OnTimedOut()
		{
			AddLog(CaravanAmbushIssueQuestTimeOutLogText);
			RelationshipChangeWithQuestGiver = -5;
			base.QuestGiver.AddPower(-5f);
		}

		private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
		{
			if (party == _caravanParty)
			{
				Debug.FailedAssert("Caravan has arrived at settlement without encountering the bandits", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Issues\\CaravanAmbushIssueBehavior.cs", "OnSettlementEntered", 718);
				DestroyPartyAction.Apply(_caravanParty.Party, _caravanParty);
				_caravanParty = null;
				_banditParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: false);
				CompleteQuestWithCancel();
			}
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, MapEventEnded);
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, MapEventStarted);
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
			CampaignEvents.BanditPartyRecruited.AddNonSerializedListener(this, OnBanditPartyRecruited);
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		}

		private void OnBanditPartyRecruited(MobileParty party)
		{
			if (party == _banditParty)
			{
				OnPlayerHiredBandits();
			}
		}

		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail declareWarDetail)
		{
			if (base.QuestGiver.CurrentSettlement.MapFaction.IsAtWarWith(Clan.PlayerClan.MapFaction))
			{
				CompleteQuestWithCancel(CaravanAmbushWarDeclaredCancelLogText);
			}
		}

		private void MapEventEnded(MapEvent mapEvent)
		{
			if (mapEvent.WinningSide == BattleSideEnum.None || mapEvent.DefeatedSide == BattleSideEnum.None)
			{
				return;
			}
			MapEventSide mapEventSide = mapEvent.GetMapEventSide(mapEvent.WinningSide);
			MapEventSide mapEventSide2 = mapEvent.GetMapEventSide(mapEvent.DefeatedSide);
			if (mapEventSide2.Parties.Any((MapEventParty t) => t.Party == _caravanParty.Party))
			{
				HandlePartyAiAfterCompletion();
				OnCaravanDestroyed(mapEventSide.LeaderParty);
			}
			else
			{
				if (!mapEventSide2.Parties.Any((MapEventParty t) => t.Party == _banditParty.Party))
				{
					return;
				}
				_isCaravanSaved = true;
				HandlePartyAiAfterCompletion();
				if (mapEventSide.IsMainPartyAmongParties())
				{
					if (_caravanParty.IsActive && mapEventSide.Parties.Any((MapEventParty t) => t.Party == _caravanParty.Party))
					{
						CampaignMapConversation.OpenConversation(new ConversationCharacterData(Hero.MainHero.CharacterObject), new ConversationCharacterData(ConversationHelper.GetConversationCharacterPartyLeader(_caravanParty.Party)));
					}
					else
					{
						OnQuestSucceeded();
					}
				}
				else
				{
					OnCaravanSurvivedWithoutHelp();
				}
			}
		}

		private void OnWarDeclared(IFaction faction1, IFaction faction2)
		{
			CheckFailureDueToDiplomaticState();
		}

		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (clan == Clan.PlayerClan && newKingdom != null)
			{
				CheckFailureDueToDiplomaticState();
			}
		}

		private void CheckFailureDueToDiplomaticState()
		{
			if (base.QuestGiver.CurrentSettlement.MapFaction.IsAtWarWith(Clan.PlayerClan.MapFaction))
			{
				CompleteQuestWithCancel(CaravanAmbushWarDeclaredCancelLogText);
			}
		}

		private void MapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			if (defenderParty.MobileParty == _caravanParty && attackerParty.MobileParty == _banditParty)
			{
				mapEvent.IsInvulnerable = true;
			}
		}

		private void OnCaravanSurvivedWithoutHelp()
		{
			AddLog(CaravanSurvivedWithoutHelpLogText);
			ChangeRelationAction.ApplyPlayerRelation(base.QuestGiver, -5);
			base.QuestGiver.AddPower(-5f);
			HandlePartyAiAfterCompletion();
			CompleteQuestWithFail();
		}

		private void OnCaravanDestroyed(PartyBase destroyerParty)
		{
			AddLog(CaravanAmbushIssueQuestCaravanDestroyedLogText);
			RelationshipChangeWithQuestGiver = -5;
			base.QuestGiver.AddPower(-5f);
			if (_caravanParty.MapEvent != null)
			{
				_caravanParty.MapEvent.IsInvulnerable = false;
			}
			_caravanParty = null;
			CompleteQuestWithFail();
		}

		private void HandlePartyAiAfterCompletion()
		{
			if (_caravanParty.IsActive)
			{
				_caravanParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: false);
				SetPartyAiAction.GetActionForVisitingSettlement(_caravanParty, _targetSettlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
			}
			if (_banditParty.MapEvent != null)
			{
				_banditParty.MapEvent.IsInvulnerable = false;
			}
			if (_banditParty.IsActive)
			{
				_banditParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: false);
				SetPartyAiAction.GetActionForVisitingSettlement(_banditParty, _banditParty.HomeSettlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
			}
			else
			{
				_banditParty = null;
			}
		}

		protected override void InitializeQuestOnGameLoad()
		{
			SetDialogs();
			if (_banditParty.MapEvent != null && _banditParty.MapEvent.DefenderSide.LeaderParty.MobileParty != _caravanParty && !_banditParty.MapEvent.IsPlayerMapEvent)
			{
				_banditParty.MapEvent.FinalizeEvent();
			}
			if (!_banditParty.Ai.DoNotMakeNewDecisions || _banditParty.TargetParty != _caravanParty)
			{
				SetPartyAiAction.GetActionForEngagingParty(_banditParty, _caravanParty, MobileParty.NavigationType.Default, isFromPort: false);
				_banditParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
				_banditParty.IgnoreByOtherPartiesTill(base.QuestDueTime);
			}
		}
	}

	public class CaravanAmbushIssueTypeDefiner : SaveableTypeDefiner
	{
		public CaravanAmbushIssueTypeDefiner()
			: base(380000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(CaravanAmbushIssue), 1);
			AddClassDefinition(typeof(CaravanAmbushIssueQuest), 2);
		}
	}

	private const IssueBase.IssueFrequency CaravanAmbushIssueFrequency = IssueBase.IssueFrequency.Common;

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
	}

	private void OnCheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero))
		{
			Settlement targetSettlement = GetTargetSettlement(hero.CurrentSettlement);
			if (targetSettlement != null)
			{
				Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnIssueSelected, typeof(CaravanAmbushIssue), IssueBase.IssueFrequency.Common, targetSettlement));
			}
		}
	}

	private IssueBase OnIssueSelected(in PotentialIssueData pid, Hero issueOwner)
	{
		return new CaravanAmbushIssue(issueOwner, pid.RelatedObject as Settlement);
	}

	private bool ConditionsHold(Hero issueGiver)
	{
		if (issueGiver != null && issueGiver.IsNotable && !issueGiver.OwnedCaravans.IsEmpty() && (issueGiver.IsArtisan || issueGiver.IsMerchant) && issueGiver.CurrentSettlement != null)
		{
			return !issueGiver.CurrentSettlement.HasPort;
		}
		return false;
	}

	private Settlement GetTargetSettlement(Settlement currentSettlement)
	{
		IEnumerable<Settlement> source = Settlement.All.Where((Settlement t) => t.IsTown && t != currentSettlement && t.MapFaction != null && !t.MapFaction.IsAtWarWith(currentSettlement.MapFaction) && !t.IsUnderRaid && !t.IsUnderSiege);
		if (!source.Any())
		{
			return null;
		}
		return TaleWorlds.Core.Extensions.MinBy(source, (Settlement t) => Campaign.Current.Models.MapDistanceModel.GetDistance(t, currentSettlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default));
	}

	private void OnGameLoadFinished()
	{
		if (!MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.0")))
		{
			return;
		}
		foreach (MapEvent mapEvent in Campaign.Current.MapEventManager.MapEvents)
		{
			if (mapEvent.IsInvulnerable && mapEvent.IsFieldBattle && mapEvent.BattleStartTime.ElapsedWeeksUntilNow > 1f)
			{
				mapEvent.FinalizeEvent();
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	public static void UtilizePartyEscortBehavior(MobileParty escortedParty, MobileParty escortParty, ref bool isWaitingForEscortParty, float innerRadius, float outerRadius, MobilePartyHelper.ResumePartyEscortBehaviorDelegate onPartyEscortBehaviorResumed, bool showDebugSpheres = false)
	{
		if (!isWaitingForEscortParty)
		{
			if (escortParty.Position.DistanceSquared(escortedParty.Position) >= outerRadius * outerRadius)
			{
				escortedParty.SetMoveGoToPoint(escortedParty.Position, MobileParty.NavigationType.Default);
				escortedParty.Ai.CheckPartyNeedsUpdate();
				isWaitingForEscortParty = true;
			}
		}
		else if (escortParty.Position.DistanceSquared(escortedParty.Position) <= innerRadius * innerRadius)
		{
			onPartyEscortBehaviorResumed();
			escortedParty.Ai.CheckPartyNeedsUpdate();
			isWaitingForEscortParty = false;
		}
	}
}
