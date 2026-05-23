using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
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

public class EscortMerchantCaravanIssueBehavior : CampaignBehaviorBase
{
	public class EscortMerchantCaravanIssue : IssueBase
	{
		private const int MinimumRequiredMenCount = 20;

		private const int AlternativeSolutionTroopTierRequirement = 2;

		private const int NeededCompanionSkillAmount = 120;

		private const int QuestTimeLimit = 30;

		private const int IssueDuration = 30;

		[SaveableField(10)]
		private int _companionRewardRandom;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.Casualties | AlternativeSolutionScaleFlag.FailureRisk;

		public override int AlternativeSolutionBaseNeededMenCount => 10 + TaleWorlds.Library.MathF.Ceiling(16f * base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 6 + TaleWorlds.Library.MathF.Ceiling(10f * base.IssueDifficultyMultiplier);

		protected int DailyQuestRewardGold => 250 + TaleWorlds.Library.MathF.Ceiling(1000f * base.IssueDifficultyMultiplier);

		protected override int RewardGold => Math.Min(DailyQuestRewardGold * _companionRewardRandom, 8000);

		public override TextObject IssueBriefByIssueGiver
		{
			get
			{
				TextObject result = new TextObject("{=CSqaF7tz}There's been a real surge of banditry around here recently. I don't know if it's because the lords are away fighting or something else, but it's a miracle if a traveler can make three leagues beyond the gates without being set upon by highwaymen.[if:convo_annoyed][ib:hip]");
				if (base.IssueOwner.CharacterObject.GetPersona() == DefaultTraits.PersonaCurt || base.IssueOwner.CharacterObject.GetPersona() == DefaultTraits.PersonaSoftspoken)
				{
					result = new TextObject("{=xwc9mJdC}Things have gotten a lot worse recently with the brigands on the roads around town. My caravans get looted as soon as they're out of sight of the gates.[if:convo_stern][ib:hip]");
				}
				return result;
			}
		}

		public override TextObject IssueAcceptByPlayer => new TextObject("{=TGYJUUn0}Go on.");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver => new TextObject("{=8ym6UvxE}I'm of a mind to send out a new caravan but I fear it will be plundered before it can turn a profit. So I am looking for some good fighters who can escort it until it finds its footing and visits a couple of settlements.");

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=ytdZutjw}I will be willing to pay generously {BASE_REWARD}{GOLD_ICON} for each day the caravan is on the road. It will be more than I usually pay for caravan guards, but you look like the type who send a message to these brigands, that my caravans aren't to be messed with.[if:convo_undecided_closed]");
				if (base.IssueOwner.CharacterObject.GetPersona() == DefaultTraits.PersonaCurt || base.IssueOwner.CharacterObject.GetPersona() == DefaultTraits.PersonaSoftspoken)
				{
					textObject = new TextObject("{=YbbfaHqd}I will be willing to pay generously {BASE_REWARD}{GOLD_ICON} for each day the caravan is on the road. It will be more than I usually pay for guards, but figure maybe you can scare these bandits off. I'm sick of choosing between sending my men to the their deaths or letting them go because I've lost my goods and can't pay their wages.[if:convo_undecided_closed]");
				}
				textObject.SetTextVariable("BASE_REWARD", DailyQuestRewardGold);
				return textObject;
			}
		}

		public override TextObject IssueQuestSolutionAcceptByPlayer => new TextObject("{=a7fEPW5Y}Don't worry, I'll escort the caravan myself.");

		public override TextObject IssueAlternativeSolutionAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=N4p2GCsG}I'll assign one of my companions and {NEEDED_MEN_COUNT} of my men to protect your caravan for {RETURN_DAYS} days.");
				textObject.SetTextVariable("NEEDED_MEN_COUNT", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		public override TextObject IssueDiscussAlternativeSolution => new TextObject("{=hU5j7b3e}I am sure your men are as capable as you are and will look after my caravan. Thanks again for your help, my friend.[if:convo_focused_happy]");

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver => new TextObject("{=iny76Ifh}Thank you, {?PLAYER.GENDER}madam{?}sir{\\?}, I think they will be enough.");

		public override bool IsThereAlternativeSolution => true;

		public override bool IsThereLordSolution => false;

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=6y59FBgL}{ISSUEGIVER.LINK}, a merchant from {SETTLEMENT}, has told you about {?ISSUEGIVER.GENDER}her{?}his{\\?} recent problems with bandits. {?ISSUEGIVER.GENDER}She{?}he{\\?} asked you to guard {?ISSUEGIVER.GENDER}her{?}his{\\?} caravan for a while and deal with any attackers. In return {?ISSUEGIVER.GENDER}she{?}he{\\?} offered you {GOLD}{GOLD_ICON} for each day your troops spend on escort duty.{newline}You agreed to lend {?ISSUEGIVER.GENDER}her{?}him{\\?} {NEEDED_MEN_COUNT} men. They should be enough to turn away most of the bandits. Your troops should return after {RETURN_DAYS} days.");
				StringHelpers.SetCharacterProperties("ISSUEGIVER", base.IssueOwner.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", base.IssueOwner.CurrentSettlement.Name);
				textObject.SetTextVariable("NEEDED_MEN_COUNT", AlternativeSolutionSentTroops.TotalManCount);
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				textObject.SetTextVariable("GOLD", DailyQuestRewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		public override TextObject Title => new TextObject("{=VpLzd69e}Escort Merchant Caravan");

		public override TextObject Description => new TextObject("{=8RNueEmy}A merchant caravan needs an escort for protection against bandits and brigands.");

		public override TextObject IssueAlternativeSolutionFailLog => new TextObject("{=KLauwaRJ}The caravan was destroyed despite your companion's efforts. Quest failed.");

		public override TextObject IssueAlternativeSolutionSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=3NX8H4TJ}Your companion has protected the caravan that belongs to {ISSUE_GIVER.LINK} from {SETTLEMENT} as promised. {?ISSUE_GIVER.GENDER}She{?}He{\\?} was happy with your work.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", base.IssueSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		protected override int CompanionSkillRewardXP => (int)(800f + 1000f * base.IssueDifficultyMultiplier);

		internal static void AutoGeneratedStaticCollectObjectsEscortMerchantCaravanIssue(object o, List<object> collectedObjects)
		{
			((EscortMerchantCaravanIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		internal static object AutoGeneratedGetMemberValue_companionRewardRandom(object o)
		{
			return ((EscortMerchantCaravanIssue)o)._companionRewardRandom;
		}

		public EscortMerchantCaravanIssue(Hero issueOwner)
			: base(issueOwner, CampaignTime.DaysFromNow(30f))
		{
			_companionRewardRandom = MBRandom.RandomInt(3, 10);
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.SettlementProsperity)
			{
				return -0.4f;
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

		public override IssueFrequency GetFrequency()
		{
			return IssueFrequency.VeryCommon;
		}

		protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out PreconditionFlags flags, out Hero relationHero, out SkillObject skill, out int requiredGold)
		{
			skill = null;
			relationHero = null;
			requiredGold = 0;
			flags = PreconditionFlags.None;
			if (issueGiver.GetRelationWithPlayer() < -10f)
			{
				flags |= PreconditionFlags.Relation;
				relationHero = issueGiver;
			}
			if (issueGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				flags |= PreconditionFlags.AtWar;
			}
			if (MobileParty.MainParty.MemberRoster.TotalHealthyCount < 20)
			{
				flags |= PreconditionFlags.NotEnoughTroops;
			}
			return flags == PreconditionFlags.None;
		}

		public override bool IssueStayAliveConditions()
		{
			if (base.IssueOwner.OwnedCaravans.Count < 2)
			{
				return base.IssueOwner.CurrentSettlement.Town.Security <= 80f;
			}
			return false;
		}

		protected override void OnGameLoad()
		{
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new EscortMerchantCaravanIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(30f), base.IssueDifficultyMultiplier, DailyQuestRewardGold);
		}

		protected override void AlternativeSolutionEndWithFailureConsequence()
		{
			base.IssueOwner.AddPower(-5f);
			RelationshipChangeWithIssueOwner = -5;
			TraitLevelingHelper.OnIssueFailed(base.IssueOwner, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -20)
			});
			base.IssueSettlement.Town.Prosperity -= 20f;
		}

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			base.IssueOwner.AddPower(10f);
			RelationshipChangeWithIssueOwner = 5;
			base.IssueSettlement.Town.Prosperity += 10f;
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}
	}

	public class EscortMerchantCaravanIssueQuest : QuestBase
	{
		private const int BattleFakeSimulationDuration = 3;

		private const string CustomPartyComponentTalkId = "escort_caravan_talk";

		[SaveableField(2)]
		private readonly int _requiredSettlementNumber;

		[SaveableField(3)]
		private List<Settlement> _visitedSettlements;

		[SaveableField(4)]
		private MobileParty _questCaravanMobileParty;

		[SaveableField(5)]
		private MobileParty _questBanditMobileParty;

		[SaveableField(7)]
		private readonly float _difficultyMultiplier;

		[SaveableField(12)]
		private bool _isPlayerNotifiedForDanger;

		[SaveableField(26)]
		private MobileParty _otherBanditParty;

		[SaveableField(30)]
		private int _questBanditPartyFollowDuration;

		[SaveableField(31)]
		private int _otherBanditPartyFollowDuration;

		[SaveableField(11)]
		private int _daysSpentForEscorting = 1;

		private int _caravanWaitedInSettlementForHours;

		[SaveableField(23)]
		private bool _questBanditPartyAlreadyAttacked;

		private CustomPartyComponent _customPartyComponent;

		[SaveableField(1)]
		private JournalLog _playerStartsQuestLog;

		private float BanditPartyAttackRadiusMin => Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius * 2.5f;

		private float QuestBanditPartySpawnDistance => Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Default) * 1.25f;

		public override TextObject Title => new TextObject("{=VpLzd69e}Escort Merchant Caravan");

		public override bool IsRemainingTimeHidden => false;

		private int BanditPartyTroopCount => (int)TaleWorlds.Library.MathF.Min(40f, (float)(MobileParty.MainParty.MemberRoster.TotalHealthyCount + _questCaravanMobileParty.MemberRoster.TotalHealthyCount) * 0.7f);

		private int CaravanPartyTroopCount => (int)(5f * _difficultyMultiplier) + 10;

		private bool CaravanIsInsideSettlement => _questCaravanMobileParty.CurrentSettlement != null;

		private int TotalRewardGold => TaleWorlds.Library.MathF.Min(8000, RewardGold * _daysSpentForEscorting);

		private CustomPartyComponent CaravanCustomPartyComponent
		{
			get
			{
				if (_customPartyComponent == null)
				{
					_customPartyComponent = _questCaravanMobileParty.PartyComponent as CustomPartyComponent;
				}
				return _customPartyComponent;
			}
		}

		private TextObject PlayerStartsQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=YXbKXUDu}{ISSUE_GIVER.LINK}, a merchant from {SETTLEMENT}, has told you about {?ISSUE_GIVER.GENDER}her{?}his{\\?} recent problems with bandits. {?ISSUE_GIVER.GENDER}She{?}He{\\?} asked you to guard {?ISSUE_GIVER.GENDER}her{?}his{\\?} caravan for a while and deal with any attackers. In return {?ISSUE_GIVER.GENDER}she{?}he{\\?} offered you {GOLD}{GOLD_ICON} denars for each day you spend on escort duty.{newline}You have agreed to guard it yourself until it visits {NUMBER_OF_SETTLEMENTS} settlements.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", Settlement.CurrentSettlement.Name);
				textObject.SetTextVariable("NUMBER_OF_SETTLEMENTS", _requiredSettlementNumber);
				textObject.SetTextVariable("GOLD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		private TextObject CaravanDestroyedQuestLogText => new TextObject("{=zk9QyKIz}The caravan was destroyed. Quest failed.");

		private TextObject CaravanLostTheTrackLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=y62dyzH6}You have lost the track of caravan. Your agreement with {ISSUE_GIVER.LINK} is failed.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject CaravanDestroyedByBanditsLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=MhvyTcrH}The caravan is destroyed by some bandits. Your agreement with {ISSUE_GIVER.LINK} is failed.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject CaravanDestroyedByPlayerQuestLogText => new TextObject("{=Rd3m5kyk}You have attacked the caravan.");

		private TextObject SuccessQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=dKEADOhG}You have protected the caravan belonging to {QUEST_GIVER.LINK} from {SETTLEMENT} as promised. {?QUEST_GIVER.GENDER}She{?}He{\\?} was happy with your work.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("SETTLEMENT", base.QuestGiver.CurrentSettlement.Name);
				return textObject;
			}
		}

		private TextObject CancelByWarQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=KhNkBd9O}Your clan is now at war with the {QUEST_GIVER.LINK}’s lord. Your agreement with {QUEST_GIVER.LINK} was canceled.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject CaravanNoTargetLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=1FOmvEdf}All profitable trade routes of the caravan are blocked by recent wars. {QUEST_GIVER.LINK} decided to recall the caravan until the situation gets better. {?QUEST_GIVER.GENDER}She{?}He{\\?} was happy with your service and sent you {REWARD}{GOLD_ICON} as promised.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("REWARD", TotalRewardGold);
				return textObject;
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsEscortMerchantCaravanIssueQuest(object o, List<object> collectedObjects)
		{
			((EscortMerchantCaravanIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_visitedSettlements);
			collectedObjects.Add(_questCaravanMobileParty);
			collectedObjects.Add(_questBanditMobileParty);
			collectedObjects.Add(_otherBanditParty);
			collectedObjects.Add(_playerStartsQuestLog);
		}

		internal static object AutoGeneratedGetMemberValue_requiredSettlementNumber(object o)
		{
			return ((EscortMerchantCaravanIssueQuest)o)._requiredSettlementNumber;
		}

		internal static object AutoGeneratedGetMemberValue_visitedSettlements(object o)
		{
			return ((EscortMerchantCaravanIssueQuest)o)._visitedSettlements;
		}

		internal static object AutoGeneratedGetMemberValue_questCaravanMobileParty(object o)
		{
			return ((EscortMerchantCaravanIssueQuest)o)._questCaravanMobileParty;
		}

		internal static object AutoGeneratedGetMemberValue_questBanditMobileParty(object o)
		{
			return ((EscortMerchantCaravanIssueQuest)o)._questBanditMobileParty;
		}

		internal static object AutoGeneratedGetMemberValue_difficultyMultiplier(object o)
		{
			return ((EscortMerchantCaravanIssueQuest)o)._difficultyMultiplier;
		}

		internal static object AutoGeneratedGetMemberValue_isPlayerNotifiedForDanger(object o)
		{
			return ((EscortMerchantCaravanIssueQuest)o)._isPlayerNotifiedForDanger;
		}

		internal static object AutoGeneratedGetMemberValue_otherBanditParty(object o)
		{
			return ((EscortMerchantCaravanIssueQuest)o)._otherBanditParty;
		}

		internal static object AutoGeneratedGetMemberValue_questBanditPartyFollowDuration(object o)
		{
			return ((EscortMerchantCaravanIssueQuest)o)._questBanditPartyFollowDuration;
		}

		internal static object AutoGeneratedGetMemberValue_otherBanditPartyFollowDuration(object o)
		{
			return ((EscortMerchantCaravanIssueQuest)o)._otherBanditPartyFollowDuration;
		}

		internal static object AutoGeneratedGetMemberValue_daysSpentForEscorting(object o)
		{
			return ((EscortMerchantCaravanIssueQuest)o)._daysSpentForEscorting;
		}

		internal static object AutoGeneratedGetMemberValue_questBanditPartyAlreadyAttacked(object o)
		{
			return ((EscortMerchantCaravanIssueQuest)o)._questBanditPartyAlreadyAttacked;
		}

		internal static object AutoGeneratedGetMemberValue_playerStartsQuestLog(object o)
		{
			return ((EscortMerchantCaravanIssueQuest)o)._playerStartsQuestLog;
		}

		public EscortMerchantCaravanIssueQuest(string questId, Hero giverHero, CampaignTime duration, float difficultyMultiplier, int rewardGold)
			: base(questId, giverHero, duration, rewardGold)
		{
			_difficultyMultiplier = difficultyMultiplier;
			_requiredSettlementNumber = TaleWorlds.Library.MathF.Round(2f + 4f * _difficultyMultiplier);
			_visitedSettlements = new List<Settlement>();
			SetDialogs();
			InitializeQuestOnCreation();
		}

		protected override void SetDialogs()
		{
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(new TextObject("{=TdwKwExD}Thank you. You can find the caravan just outside the settlement.[if:convo_grateful]")).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.Consequence(QuestAcceptedConsequences)
				.CloseDialog();
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(new TextObject("{=vtZYmAaR}I feel good knowing that you're looking after my caravan. Safe journeys, my friend![if:convo_grateful]")).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.CloseDialog();
			Campaign.Current.ConversationManager.AddDialogFlow(GetCaravanPartyDialogFlow(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetCaravanGreetingDialogFlow(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetCaravanTradeDialogFlow(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetCaravanLootDialogFlow(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetCaravanFarewellDialogFlow(), this);
		}

		private DialogFlow GetCaravanPartyDialogFlow()
		{
			TextObject textObject = new TextObject("{=ZAqEJI9T}About the task {QUEST_GIVER.LINK} gave me.");
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
			return DialogFlow.CreateDialogFlow("escort_caravan_talk", 125).BeginPlayerOptions().PlayerOption(textObject)
				.Condition(caravan_talk_on_condition)
				.NpcLine("{=heWYa9Oq}I feel safe knowing that you're looking after us. Please continue to follow us my friend!")
				.Consequence(delegate
				{
					PlayerEncounter.LeaveEncounter = true;
				})
				.CloseDialog()
				.EndPlayerOptions();
		}

		private bool caravan_talk_on_condition()
		{
			int num;
			if (_questCaravanMobileParty.MemberRoster.Contains(CharacterObject.OneToOneConversationCharacter) && _questCaravanMobileParty == MobileParty.ConversationParty && MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsCustomParty && !CharacterObject.OneToOneConversationCharacter.IsHero)
			{
				num = ((MobileParty.ConversationParty.Party.Owner != Hero.MainHero) ? 1 : 0);
				if (num != 0)
				{
					MBTextManager.SetTextVariable("HOMETOWN", MobileParty.ConversationParty.HomeSettlement.EncyclopediaLinkWithName);
					StringHelpers.SetCharacterProperties("MERCHANT", MobileParty.ConversationParty.Party.Owner.CharacterObject);
					StringHelpers.SetCharacterProperties("PROTECTOR", MobileParty.ConversationParty.HomeSettlement.OwnerClan.Leader.CharacterObject);
				}
			}
			else
			{
				num = 0;
			}
			return (byte)num != 0;
		}

		private DialogFlow GetCaravanFarewellDialogFlow()
		{
			TextObject text = new TextObject("{=1IJouNaM}Carry on, then. Farewell.");
			return DialogFlow.CreateDialogFlow("escort_caravan_talk", 125).BeginPlayerOptions().PlayerOption(text)
				.Condition(caravan_talk_on_condition)
				.NpcLine("{=heWYa9Oq}I feel safe knowing that you're looking after us. Please continue to follow us my friend!")
				.Consequence(delegate
				{
					PlayerEncounter.LeaveEncounter = true;
				})
				.CloseDialog()
				.EndPlayerOptions();
		}

		private DialogFlow GetCaravanLootDialogFlow()
		{
			TextObject text = new TextObject("{=WOBy5UfY}Hand over your goods, or die!");
			return DialogFlow.CreateDialogFlow("escort_caravan_talk", 125).BeginPlayerOptions().PlayerOption(text)
				.Condition(caravan_loot_on_condition)
				.NpcLine("{=QNaKmkt9}We're paid to guard this caravan. If you want to rob it, it's going to be over our dead bodies![if:convo_angry][ib:aggressive]")
				.BeginPlayerOptions()
				.PlayerOption("{=EhxS7NQ4}So be it. Attack!")
				.Consequence(conversation_caravan_fight_on_consequence)
				.CloseDialog()
				.PlayerOption("{=bfPsE9M1}You must have misunderstood me. Go in peace.")
				.Consequence(caravan_talk_leave_on_consequence)
				.CloseDialog()
				.EndPlayerOptions()
				.EndPlayerOptions();
		}

		private void conversation_caravan_fight_on_consequence()
		{
			BeHostileAction.ApplyEncounterHostileAction(PartyBase.MainParty, MobileParty.ConversationParty.Party);
		}

		private void caravan_talk_leave_on_consequence()
		{
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.LeaveEncounter = true;
			}
		}

		private DialogFlow GetCaravanTradeDialogFlow()
		{
			TextObject text = new TextObject("{=t0UGXPV4}I'm interested in trading. What kind of products do you have?");
			return DialogFlow.CreateDialogFlow("escort_caravan_talk", 125).BeginPlayerOptions().PlayerOption(text)
				.Condition(caravan_buy_products_on_condition)
				.NpcLine("{=tlLDHAIu}Very well. A pleasure doing business with you.[if:convo_relaxed_happy][ib:demure]")
				.Condition(conversation_caravan_player_trade_end_on_condition)
				.NpcLine("{=DQBaaC0e}Is there anything else?")
				.GotoDialogState("escort_caravan_talk")
				.EndPlayerOptions();
		}

		private bool caravan_buy_products_on_condition()
		{
			if (MobileParty.ConversationParty != null && MobileParty.ConversationParty == _questCaravanMobileParty && !MobileParty.ConversationParty.IsCaravan)
			{
				for (int i = 0; i < MobileParty.ConversationParty.ItemRoster.Count; i++)
				{
					if (MobileParty.ConversationParty.ItemRoster.GetElementNumber(i) > 0)
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool conversation_caravan_player_trade_end_on_condition()
		{
			if (MobileParty.ConversationParty != null && MobileParty.ConversationParty == _questCaravanMobileParty && !MobileParty.ConversationParty.IsCaravan)
			{
				InventoryScreenHelper.OpenTradeWithCaravanOrAlleyParty(MobileParty.ConversationParty);
			}
			return true;
		}

		private DialogFlow GetCaravanGreetingDialogFlow()
		{
			TextObject npcText = new TextObject("{=FpUybbSk}Greetings. This caravan is owned by {MERCHANT.LINK}. We trade under the protection of {PROTECTOR.LINK}, master of {HOMETOWN}. How may we help you?[if:convo_normal]");
			if (MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsCurrentlyAtSea)
			{
				npcText = new TextObject("{=yGttYe7g}Greetings. This ship is owned by {MERCHANT.LINK}. We sail under the protection of {PROTECTOR.LINK}, master of {HOMETOWN}. How may we help you?[if:convo_normal]");
			}
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine(npcText).Condition(caravan_talk_on_condition)
				.GotoDialogState("escort_caravan_talk");
		}

		private void QuestAcceptedConsequences()
		{
			StartQuest();
			SpawnCaravan();
			_playerStartsQuestLog = AddDiscreteLog(PlayerStartsQuestLogText, new TextObject("{=r2y3n7dR}Visited Settlements"), _visitedSettlements.Count, _requiredSettlementNumber);
		}

		private bool caravan_loot_on_condition()
		{
			int num;
			if (MobileParty.ConversationParty != null && MobileParty.ConversationParty.Party.MapFaction != Hero.MainHero.MapFaction && !MobileParty.ConversationParty.IsCaravan)
			{
				num = ((MobileParty.ConversationParty == _questCaravanMobileParty) ? 1 : 0);
				if (num != 0)
				{
					MBTextManager.SetTextVariable("HOMETOWN", MobileParty.ConversationParty.HomeSettlement.EncyclopediaLinkWithName);
					StringHelpers.SetCharacterProperties("MERCHANT", MobileParty.ConversationParty.Party.Owner.CharacterObject);
					StringHelpers.SetCharacterProperties("PROTECTOR", MobileParty.ConversationParty.HomeSettlement.OwnerClan.Leader.CharacterObject);
				}
			}
			else
			{
				num = 0;
			}
			return (byte)num != 0;
		}

		private void SpawnCaravan()
		{
			ItemRoster itemRoster = new ItemRoster();
			foreach (ItemObject defaultCaravanItem in Instance.DefaultCaravanItems)
			{
				itemRoster.AddToCounts(defaultCaravanItem, 7);
			}
			GetAdditionalVisualsForParty(base.QuestGiver.Culture, out var mountStringId, out var harnessStringId);
			TextObject textObject = GameTexts.FindText("str_caravan_party_name");
			textObject.SetCharacterProperties("OWNER", base.QuestGiver.CharacterObject);
			_questCaravanMobileParty = CustomPartyComponent.CreateCustomPartyWithTroopRoster(base.QuestGiver.CurrentSettlement.GatePosition, 0f, base.QuestGiver.CurrentSettlement, textObject, base.QuestGiver.Clan, TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), base.QuestGiver, mountStringId, harnessStringId, 4f);
			InitializeCaravanOnCreation(_questCaravanMobileParty, base.QuestGiver, base.QuestGiver.CurrentSettlement, itemRoster);
			AddTrackedObject(_questCaravanMobileParty);
			_questCaravanMobileParty.SetPartyUsedByQuest(isActivelyUsed: true);
			_questCaravanMobileParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
			_questCaravanMobileParty.IgnoreByOtherPartiesTill(base.QuestDueTime);
			_caravanWaitedInSettlementForHours = 4;
		}

		private bool ProperSettlementCondition(Settlement settlement)
		{
			if (settlement != Settlement.CurrentSettlement && settlement.IsTown && !settlement.IsUnderSiege)
			{
				return !_visitedSettlements.Contains(settlement);
			}
			return false;
		}

		private void InitializeCaravanOnCreation(MobileParty mobileParty, Hero owner, Settlement settlement, ItemRoster caravanItems)
		{
			mobileParty.Aggressiveness = 0f;
			PartyTemplateObject randomCaravanTemplate = CaravanHelper.GetRandomCaravanTemplate(owner.Culture, isElite: false, isLand: true);
			mobileParty.InitializeMobilePartyAtPosition(TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), settlement.GatePosition);
			MobilePartyHelper.FillPartyManuallyAfterCreation(mobileParty, randomCaravanTemplate, CaravanPartyTroopCount);
			CharacterObject character = CharacterObject.All.First((CharacterObject characterObject) => characterObject.Occupation == Occupation.CaravanGuard && characterObject.IsInfantry && characterObject.Level == 26 && characterObject.Culture == mobileParty.Party.Owner.Culture);
			mobileParty.MemberRoster.AddToCounts(character, 1, insertAtFront: true);
			mobileParty.Party.SetVisualAsDirty();
			mobileParty.InitializePartyTrade(Campaign.Current.Models.CaravanModel.GetInitialTradeGold(owner, isNavalCaravan: false, eliteCaravan: false));
			if (caravanItems != null)
			{
				mobileParty.ItemRoster.Add(caravanItems);
				return;
			}
			float num = 10000f;
			ItemObject itemObject = null;
			foreach (ItemObject item in Items.All)
			{
				if (item.ItemCategory == DefaultItemCategories.PackAnimal && !item.NotMerchandise && (float)item.Value < num)
				{
					itemObject = item;
					num = item.Value;
				}
			}
			if (itemObject != null)
			{
				mobileParty.ItemRoster.Add(new ItemRosterElement(itemObject, (int)((float)mobileParty.MemberRoster.TotalManCount * 0.5f)));
			}
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

		protected override void RegisterEvents()
		{
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
			CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, OnPartyHourlyTick);
			CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
		}

		private void OnPartyHourlyTick(MobileParty mobileParty)
		{
			CheckPartyAndMakeItAttackTheCaravan(mobileParty);
			CheckEncounterForBanditParty(_questBanditMobileParty);
			CheckEncounterForBanditParty(_otherBanditParty);
			CheckOtherBanditPartyDistance();
		}

		private void CheckOtherBanditPartyDistance()
		{
			if (!base.IsOngoing)
			{
				return;
			}
			if (_otherBanditParty != null && _otherBanditParty.IsActive && _otherBanditParty.TargetParty == _questCaravanMobileParty && _otherBanditPartyFollowDuration < 0)
			{
				if (IsTracked(_otherBanditParty))
				{
					RemoveTrackedObject(_otherBanditParty);
				}
				_otherBanditParty.SetMoveModeHold();
				_otherBanditParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: false);
				_otherBanditParty = null;
			}
			if (_questBanditMobileParty != null && _questBanditMobileParty.IsActive && _questBanditMobileParty.MapEvent == null && _questBanditMobileParty.TargetParty == _questCaravanMobileParty && _questBanditPartyFollowDuration < 0 && !_questBanditMobileParty.IsVisible)
			{
				if (IsTracked(_questBanditMobileParty))
				{
					RemoveTrackedObject(_questBanditMobileParty);
				}
				_questBanditMobileParty.SetMoveModeHold();
				_questBanditMobileParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: false);
			}
		}

		private void CheckEncounterForBanditParty(MobileParty mobileParty)
		{
			if (base.IsOngoing && mobileParty != null && mobileParty.IsActive && mobileParty.MapEvent == null && _questCaravanMobileParty.IsActive && _questCaravanMobileParty.MapEvent == null && _questCaravanMobileParty.CurrentSettlement == null && mobileParty.Position.DistanceSquared(_questCaravanMobileParty.Position) <= 1f)
			{
				EncounterManager.StartPartyEncounter(mobileParty.Party, _questCaravanMobileParty.Party);
				MBInformationManager.AddQuickInformation(new TextObject("{=o8uAzFaJ}The caravan you are protecting is ambushed by raiders!"));
				_questCaravanMobileParty.MapEvent.IsInvulnerable = true;
			}
		}

		private void CheckPartyAndMakeItAttackTheCaravan(MobileParty mobileParty)
		{
			if (_otherBanditParty != null || mobileParty == _questBanditMobileParty || !mobileParty.IsBandit || mobileParty.IsCurrentlyUsedByAQuest || mobileParty.MapEvent != null || mobileParty.NavigationCapability != MobileParty.NavigationType.Default || mobileParty.Party.NumberOfHealthyMembers <= _questCaravanMobileParty.Party.NumberOfHealthyMembers || (!(mobileParty.Speed > _questCaravanMobileParty.Speed) && !(mobileParty.Position.DistanceSquared(_questCaravanMobileParty.Position) < 9f)))
			{
				return;
			}
			Settlement toSettlement = _visitedSettlements.LastOrDefault() ?? _questCaravanMobileParty.HomeSettlement;
			Settlement targetSettlement = _questCaravanMobileParty.TargetSettlement;
			if (targetSettlement == null)
			{
				TryToFindAndSetTargetToNextSettlement();
				return;
			}
			float distance;
			float distance2;
			if (_questCaravanMobileParty.CurrentSettlement != null)
			{
				distance = Campaign.Current.Models.MapDistanceModel.GetDistance(_questCaravanMobileParty.CurrentSettlement, targetSettlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default);
				distance2 = Campaign.Current.Models.MapDistanceModel.GetDistance(_questCaravanMobileParty.CurrentSettlement, toSettlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default);
			}
			else
			{
				distance = Campaign.Current.Models.MapDistanceModel.GetDistance(_questCaravanMobileParty, targetSettlement, isTargetingPort: false, MobileParty.NavigationType.Default, out var estimatedLandRatio);
				distance2 = Campaign.Current.Models.MapDistanceModel.GetDistance(_questCaravanMobileParty, toSettlement, isTargetingPort: false, MobileParty.NavigationType.Default, out estimatedLandRatio);
			}
			float num = mobileParty.Position.DistanceSquared(_questCaravanMobileParty.Position);
			if (distance > 5f && distance2 > 5f && num < BanditPartyAttackRadiusMin * BanditPartyAttackRadiusMin)
			{
				SetPartyAiAction.GetActionForEngagingParty(mobileParty, _questCaravanMobileParty, MobileParty.NavigationType.Default, isFromPort: false);
				mobileParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
				if (!IsTracked(mobileParty))
				{
					AddTrackedObject(mobileParty);
				}
				float num2 = mobileParty.Speed + _questCaravanMobileParty.Speed;
				_otherBanditPartyFollowDuration = (int)(num / num2) + 5;
				_otherBanditParty = mobileParty;
			}
		}

		private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
		{
			if (party != _questCaravanMobileParty || settlement == _questCaravanMobileParty.HomeSettlement || !settlement.Position.NearlyEquals(MobileParty.MainParty.Position.ToVec2(), MobileParty.MainParty.SeeingRange + 2f) || settlement != _questCaravanMobileParty.TargetSettlement)
			{
				return;
			}
			_visitedSettlements.Add(settlement);
			UpdateQuestTaskStage(_playerStartsQuestLog, _visitedSettlements.Count);
			TextObject textObject = new TextObject("{=0wj3HIbh}Caravan entered {SETTLEMENT_LINK}.");
			textObject.SetTextVariable("SETTLEMENT_LINK", settlement.EncyclopediaLinkWithName);
			AddLog(textObject, hideInformation: true);
			if (_questBanditMobileParty != null && _questBanditMobileParty.IsActive)
			{
				if (IsTracked(_questBanditMobileParty))
				{
					RemoveTrackedObject(_questBanditMobileParty);
				}
				_questBanditMobileParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: false);
				_questBanditMobileParty.IgnoreByOtherPartiesTill(CampaignTime.Now);
				if (_questBanditMobileParty.MapEvent == null)
				{
					SetPartyAiAction.GetActionForPatrollingAroundSettlement(_questBanditMobileParty, settlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
				}
			}
			if (_otherBanditParty != null)
			{
				if (IsTracked(_otherBanditParty))
				{
					RemoveTrackedObject(_otherBanditParty);
				}
				_otherBanditParty.SetMoveModeHold();
				_otherBanditParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: false);
				_otherBanditParty = null;
			}
			if (_visitedSettlements.Count == _requiredSettlementNumber)
			{
				SuccessConsequences(isNoTargetLeftSuccess: false);
				return;
			}
			int num = CaravanPartyTroopCount - _questCaravanMobileParty.MemberRoster.TotalManCount;
			if (num > 0)
			{
				_questCaravanMobileParty.AddElementToMemberRoster(_questCaravanMobileParty.TargetSettlement.Culture.CaravanGuard, MBRandom.RandomInt(Math.Min(15, num)));
			}
		}

		protected override void DailyTick()
		{
			_daysSpentForEscorting++;
		}

		private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			CheckWarDeclaration();
		}

		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			CheckWarDeclaration();
		}

		private void CheckWarDeclaration()
		{
			if (base.QuestGiver.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				CompleteQuestWithCancel(CancelByWarQuestLogText);
			}
		}

		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
		{
			if (detail == DeclareWarAction.DeclareWarDetail.CausedByPlayerHostility && (faction1 == _questCaravanMobileParty.MapFaction || faction2 == _questCaravanMobileParty.MapFaction) && PlayerEncounter.Current != null && PlayerEncounter.PlayerIsAttacker)
			{
				FailByPlayerHostileConsequences();
			}
			else
			{
				CheckWarDeclaration();
			}
			if (_questCaravanMobileParty != null && (_questCaravanMobileParty.TargetSettlement == null || _questCaravanMobileParty.TargetSettlement.MapFaction.IsAtWarWith(_questCaravanMobileParty.MapFaction)) && base.IsOngoing)
			{
				TryToFindAndSetTargetToNextSettlement();
			}
		}

		protected override void HourlyTick()
		{
			if (base.IsOngoing && _questCaravanMobileParty.TargetSettlement == null)
			{
				TryToFindAndSetTargetToNextSettlement();
			}
			if (base.IsOngoing)
			{
				if (CaravanIsInsideSettlement)
				{
					SimulateSettlementWaitForCaravan();
				}
				else if (_questCaravanMobileParty.MapEvent == null)
				{
					AdjustCaravansSpeed();
				}
				NotifyPlayerOrCancelTheQuestIfCaravanIsFar();
				if (base.IsOngoing)
				{
					ThinkAboutSpawningBanditParty();
					CheckCaravanMapEvent();
					_otherBanditPartyFollowDuration--;
					_questBanditPartyFollowDuration--;
				}
			}
		}

		private void CheckCaravanMapEvent()
		{
			if (_questCaravanMobileParty.MapEvent != null && _questCaravanMobileParty.MapEvent.IsInvulnerable && _questCaravanMobileParty.MapEvent.BattleStartTime.ElapsedHoursUntilNow > 3f)
			{
				_questCaravanMobileParty.MapEvent.IsInvulnerable = false;
			}
		}

		private void AdjustCaravansSpeed()
		{
			if (!MobileParty.MainParty.IsActive)
			{
				return;
			}
			float speed = MobileParty.MainParty.Speed;
			float speed2 = _questCaravanMobileParty.Speed;
			while (speed < speed2 || speed - speed2 > 1f)
			{
				if (speed2 >= speed)
				{
					CaravanCustomPartyComponent.SetBaseSpeed(CaravanCustomPartyComponent.BaseSpeed - 0.05f);
				}
				else if (speed - speed2 > 1f)
				{
					CaravanCustomPartyComponent.SetBaseSpeed(CaravanCustomPartyComponent.BaseSpeed + 0.05f);
				}
				speed = MobileParty.MainParty.Speed;
				speed2 = _questCaravanMobileParty.Speed;
			}
		}

		private void ThinkAboutSpawningBanditParty()
		{
			if (_questBanditPartyAlreadyAttacked || _questBanditMobileParty != null)
			{
				return;
			}
			Settlement targetSettlement = _questCaravanMobileParty.TargetSettlement;
			if (targetSettlement != null)
			{
				float estimatedLandRatio;
				float num = ((_questCaravanMobileParty.CurrentSettlement != null) ? Campaign.Current.Models.MapDistanceModel.GetDistance(_questCaravanMobileParty.CurrentSettlement, targetSettlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default) : Campaign.Current.Models.MapDistanceModel.GetDistance(_questCaravanMobileParty, targetSettlement, isTargetingPort: false, MobileParty.NavigationType.Default, out estimatedLandRatio));
				if (num > 10f && num < QuestBanditPartySpawnDistance)
				{
					ActivateBanditParty();
					float num2 = _questBanditMobileParty.Speed + _questCaravanMobileParty.Speed;
					_questBanditPartyFollowDuration = (int)(QuestBanditPartySpawnDistance / num2) + 5;
					_questBanditPartyAlreadyAttacked = true;
				}
			}
		}

		private void SimulateSettlementWaitForCaravan()
		{
			_caravanWaitedInSettlementForHours++;
			if (_caravanWaitedInSettlementForHours >= 5)
			{
				LeaveSettlementAction.ApplyForParty(_questCaravanMobileParty);
				_caravanWaitedInSettlementForHours = 0;
			}
		}

		private void NotifyPlayerOrCancelTheQuestIfCaravanIsFar()
		{
			if (_questCaravanMobileParty.IsActive && !_questCaravanMobileParty.IsVisible)
			{
				float num = _questCaravanMobileParty.Position.Distance(MobileParty.MainParty.Position);
				if (!_isPlayerNotifiedForDanger && num >= MobileParty.MainParty.SeeingRange + 3f)
				{
					MBInformationManager.AddQuickInformation(new TextObject("{=2y9DhzCR}You are about to lose sight of the caravan. Find the caravan before they are in danger!"));
					_isPlayerNotifiedForDanger = true;
				}
				else if (num >= MobileParty.MainParty.SeeingRange + 20f)
				{
					AddLog(CaravanLostTheTrackLogText);
					FailConsequences();
				}
			}
		}

		private void OnSettlementLeft(MobileParty party, Settlement settlement)
		{
			if (party == _questCaravanMobileParty)
			{
				AdjustCaravansSpeed();
				if (party.TargetSettlement == null || party.TargetSettlement == settlement)
				{
					TryToFindAndSetTargetToNextSettlement();
				}
				_caravanWaitedInSettlementForHours = 0;
				_questBanditPartyAlreadyAttacked = false;
				_questCaravanMobileParty.Party.SetAsCameraFollowParty();
				if (IsTracked(settlement))
				{
					RemoveTrackedObject(settlement);
				}
			}
		}

		private void TryToFindAndSetTargetToNextSettlement()
		{
			int num = 0;
			int num2 = -1;
			do
			{
				num2 = SettlementHelper.FindNextSettlementAroundMobileParty(_questCaravanMobileParty, MobileParty.NavigationType.Default, 150f, num2);
				if (num2 >= 0)
				{
					Settlement settlement = Settlement.All[num2];
					if (ProperSettlementCondition(settlement) && settlement != _questCaravanMobileParty.HomeSettlement && (_visitedSettlements.Count == 0 || settlement != _visitedSettlements[_visitedSettlements.Count - 1]) && !settlement.MapFaction.IsAtWarWith(_questCaravanMobileParty.MapFaction))
					{
						num++;
					}
				}
			}
			while (num2 >= 0);
			if (num > 0)
			{
				int num3 = MBRandom.RandomInt(num);
				num2 = -1;
				do
				{
					num2 = SettlementHelper.FindNextSettlementAroundMobileParty(_questCaravanMobileParty, MobileParty.NavigationType.Default, 150f, num2);
					if (num2 < 0)
					{
						continue;
					}
					Settlement settlement2 = Settlement.All[num2];
					if (!ProperSettlementCondition(settlement2) || settlement2 == _questCaravanMobileParty.HomeSettlement || (_visitedSettlements.Count != 0 && settlement2 == _visitedSettlements[_visitedSettlements.Count - 1]) || settlement2.MapFaction.IsAtWarWith(_questCaravanMobileParty.MapFaction))
					{
						continue;
					}
					num3--;
					if (num3 >= 0)
					{
						continue;
					}
					Settlement settlement3 = settlement2;
					SetPartyAiAction.GetActionForVisitingSettlement(_questCaravanMobileParty, settlement3, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
					_questCaravanMobileParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
					TextObject textObject = new TextObject("{=OjI8uGFa}We are traveling to {SETTLEMENT_NAME}.");
					textObject.SetTextVariable("SETTLEMENT_NAME", settlement3.Name);
					MBInformationManager.AddQuickInformation(textObject, 100, PartyBaseHelper.GetVisualPartyLeader(_questCaravanMobileParty.Party));
					TextObject textObject2 = new TextObject("{=QDpfYm4c}The caravan is moving to {SETTLEMENT_NAME}.");
					textObject2.SetTextVariable("SETTLEMENT_NAME", settlement3.EncyclopediaLinkWithName);
					AddLog(textObject2, hideInformation: true);
					if (!IsTracked(settlement3))
					{
						AddTrackedObject(settlement3);
					}
					if (_questBanditMobileParty == null || !_questBanditMobileParty.IsActive)
					{
						break;
					}
					float num4 = DistanceHelper.FindClosestDistanceFromMobilePartyToMobileParty(_questCaravanMobileParty, _questBanditMobileParty, MobileParty.NavigationType.Default);
					if (_questBanditMobileParty.Speed < _questCaravanMobileParty.Speed || num4 > 10f)
					{
						_questBanditMobileParty.SetMoveModeHold();
						_questBanditMobileParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: false);
						_questBanditMobileParty.IgnoreByOtherPartiesTill(CampaignTime.Now);
						if (IsTracked(_questBanditMobileParty))
						{
							RemoveTrackedObject(_questBanditMobileParty);
						}
						_questBanditMobileParty = null;
					}
					break;
				}
				while (num2 >= 0);
			}
			else
			{
				CaravanNoTargetQuestSuccess();
			}
		}

		private void CaravanNoTargetQuestSuccess()
		{
			SuccessConsequences(isNoTargetLeftSuccess: true);
		}

		private void OnMapEventEnded(MapEvent mapEvent)
		{
			if (_questCaravanMobileParty == null || !mapEvent.InvolvedParties.Contains(_questCaravanMobileParty.Party))
			{
				return;
			}
			if (mapEvent.HasWinner)
			{
				bool flag = _questCaravanMobileParty.MapEventSide == MobileParty.MainParty.MapEventSide && mapEvent.IsPlayerMapEvent;
				bool num = mapEvent.Winner == _questCaravanMobileParty.MapEventSide;
				bool flag2 = mapEvent.InvolvedParties.Contains(PartyBase.MainParty);
				if (!num)
				{
					if (flag2)
					{
						if (flag)
						{
							AddLog(CaravanDestroyedQuestLogText);
							FailConsequences(banditsWon: true);
						}
						else
						{
							FailByPlayerHostileConsequences();
						}
					}
					else
					{
						AddLog(CaravanDestroyedByBanditsLogText);
						FailConsequences(banditsWon: true);
					}
					return;
				}
				if (_questBanditMobileParty != null && _questBanditMobileParty.IsActive && mapEvent.InvolvedParties.Contains(_questBanditMobileParty.Party))
				{
					DestroyPartyAction.Apply(MobileParty.MainParty.Party, _questBanditMobileParty);
				}
				if (_otherBanditParty != null && _otherBanditParty.IsActive && mapEvent.InvolvedParties.Contains(_otherBanditParty.Party))
				{
					DestroyPartyAction.Apply(MobileParty.MainParty.Party, _otherBanditParty);
				}
				if (_questCaravanMobileParty.MemberRoster.TotalManCount <= 0)
				{
					FailConsequences(banditsWon: true);
				}
				if (_questCaravanMobileParty.Speed < 2f)
				{
					_questCaravanMobileParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("sumpter_horse"), 5);
				}
			}
			else if (_questCaravanMobileParty.MemberRoster.TotalManCount <= 0)
			{
				FailConsequences(banditsWon: true);
			}
		}

		private void SuccessConsequences(bool isNoTargetLeftSuccess)
		{
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, TotalRewardGold);
			base.QuestGiver.AddPower(10f);
			RelationshipChangeWithQuestGiver = 5;
			TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 50)
			});
			base.QuestGiver.CurrentSettlement.Town.Prosperity += 10f;
			if (isNoTargetLeftSuccess)
			{
				AddLog(CaravanNoTargetLogText);
			}
			else
			{
				AddLog(SuccessQuestLogText, hideInformation: true);
			}
			_questBanditMobileParty?.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: false);
			CompleteQuestWithSuccess();
		}

		private void FailConsequences(bool banditsWon = false)
		{
			base.QuestGiver.AddPower(-10f);
			RelationshipChangeWithQuestGiver = -5;
			TraitLevelingHelper.OnIssueFailed(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -20)
			});
			base.QuestGiver.CurrentSettlement.Town.Prosperity -= 10f;
			if (_questBanditMobileParty != null)
			{
				_questBanditMobileParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: false);
				_questBanditMobileParty.IgnoreByOtherPartiesTill(CampaignTime.Now);
				if (IsTracked(_questBanditMobileParty))
				{
					RemoveTrackedObject(_questBanditMobileParty);
				}
			}
			if (_questCaravanMobileParty != null)
			{
				_questCaravanMobileParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: false);
				_questCaravanMobileParty.IgnoreByOtherPartiesTill(CampaignTime.Now);
			}
			if (_questBanditMobileParty != null && !banditsWon)
			{
				if (IsTracked(_questBanditMobileParty))
				{
					RemoveTrackedObject(_questBanditMobileParty);
				}
				_questBanditMobileParty.SetPartyUsedByQuest(isActivelyUsed: false);
				_questBanditMobileParty.IgnoreByOtherPartiesTill(CampaignTime.Now);
				if (_questBanditMobileParty.IsActive && _questBanditMobileParty.IsVisible)
				{
					DestroyPartyAction.Apply(null, _questBanditMobileParty);
				}
			}
			CompleteQuestWithFail();
		}

		private void FailByPlayerHostileConsequences()
		{
			base.QuestGiver.AddPower(-10f);
			RelationshipChangeWithQuestGiver = -10;
			TraitLevelingHelper.OnIssueFailed(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -80)
			});
			base.QuestGiver.CurrentSettlement.Town.Prosperity -= 20f;
			AddLog(CaravanDestroyedByPlayerQuestLogText, hideInformation: true);
			_questBanditMobileParty?.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: false);
			CompleteQuestWithFail();
		}

		protected override void InitializeQuestOnGameLoad()
		{
			MobileParty questCaravanMobileParty = _questCaravanMobileParty;
			if (questCaravanMobileParty != null && questCaravanMobileParty.IsCaravan)
			{
				CompleteQuestWithCancel();
			}
			SetDialogs();
		}

		private void ActivateBanditParty()
		{
			Hideout closestHideout = SettlementHelper.FindNearestHideoutToMobileParty(_questCaravanMobileParty, _questCaravanMobileParty.NavigationCapability, (Settlement x) => x.IsActive);
			Clan clan = Clan.BanditFactions.FirstOrDefault((Clan t) => t.Culture == closestHideout.Settlement.Culture);
			PartyTemplateObject partyTemplateObject = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("kingdom_hero_party_caravan_ambushers") ?? clan.DefaultPartyTemplate;
			_questBanditMobileParty = BanditPartyComponent.CreateBanditParty("escort_caravan_quest_" + base.StringId, clan, closestHideout.Settlement.Hideout, isBossParty: false, partyTemplateObject, _questCaravanMobileParty.TargetSettlement.GatePosition);
			_questBanditMobileParty.Party.SetCustomName(new TextObject("{=u1Pkt4HC}Raiders"));
			Campaign.Current.MobilePartyLocator.UpdateLocator(_questBanditMobileParty);
			_questBanditMobileParty.ActualClan = clan;
			_questBanditMobileParty.MemberRoster.Clear();
			for (int num = 0; num < BanditPartyTroopCount; num++)
			{
				List<(PartyTemplateStack, float)> list = new List<(PartyTemplateStack, float)>();
				foreach (PartyTemplateStack stack in partyTemplateObject.Stacks)
				{
					list.Add((stack, 64 - stack.Character.Level));
				}
				PartyTemplateStack partyTemplateStack = MBRandom.ChooseWeighted(list);
				_questBanditMobileParty.MemberRoster.AddToCounts(partyTemplateStack.Character, 1);
			}
			_questBanditMobileParty.ItemRoster.AddToCounts(DefaultItems.Grain, BanditPartyTroopCount);
			_questBanditMobileParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("sumpter_horse"), BanditPartyTroopCount);
			_questBanditMobileParty.IgnoreByOtherPartiesTill(base.QuestDueTime);
			SetPartyAiAction.GetActionForEngagingParty(_questBanditMobileParty, _questCaravanMobileParty, MobileParty.NavigationType.Default, isFromPort: false);
			_questBanditMobileParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
			AddTrackedObject(_questBanditMobileParty);
		}

		protected override void OnFinalize()
		{
			if (_questCaravanMobileParty != null && _questCaravanMobileParty.IsActive && _questCaravanMobileParty.IsCustomParty)
			{
				CaravanPartyComponent.ConvertPartyToCaravanParty(_questCaravanMobileParty, base.QuestGiver, base.QuestGiver.CurrentSettlement);
				_questCaravanMobileParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: false);
				_questCaravanMobileParty.IgnoreByOtherPartiesTill(CampaignTime.Now);
			}
			if (_questCaravanMobileParty != null)
			{
				RemoveTrackedObject(_questCaravanMobileParty);
			}
			if (_otherBanditParty != null && _otherBanditParty.IsActive)
			{
				_otherBanditParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: false);
				_otherBanditParty.IgnoreByOtherPartiesTill(CampaignTime.Now);
			}
		}

		protected override void OnTimedOut()
		{
			base.QuestGiver.AddPower(-5f);
			RelationshipChangeWithQuestGiver = -5;
			TraitLevelingHelper.OnIssueFailed(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -20)
			});
			base.QuestGiver.CurrentSettlement.Town.Prosperity -= 20f;
			AddLog(new TextObject("{=pUrSIed8}You have failed to escort the caravan to its destination."));
		}
	}

	public class EscortMerchantCaravanIssueTypeDefiner : SaveableTypeDefiner
	{
		public EscortMerchantCaravanIssueTypeDefiner()
			: base(450000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(EscortMerchantCaravanIssue), 1);
			AddClassDefinition(typeof(EscortMerchantCaravanIssueQuest), 2);
		}
	}

	private const IssueBase.IssueFrequency EscortMerchantCaravanIssueFrequency = IssueBase.IssueFrequency.VeryCommon;

	internal readonly List<ItemObject> DefaultCaravanItems = new List<ItemObject>();

	private static EscortMerchantCaravanIssueBehavior Instance => Campaign.Current.GetCampaignBehavior<EscortMerchantCaravanIssueBehavior>();

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
	}

	private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
	{
		InitializeOnStart();
		if (!MBSaveLoad.IsUpdatingGameVersion || !(MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("e1.9.1")))
		{
			return;
		}
		for (int num = MobileParty.All.Count - 1; num >= 0; num--)
		{
			MobileParty mobileParty = MobileParty.All[num];
			if (mobileParty.StringId.Contains("defend_caravan_quest"))
			{
				if (mobileParty.MapEvent != null)
				{
					mobileParty.MapEvent.FinalizeEvent();
				}
				DestroyPartyAction.Apply(null, MobileParty.All[num]);
			}
		}
	}

	private void InitializeOnStart()
	{
		if (MBObjectManager.Instance.GetObject<ItemObject>("hardwood") == null || MBObjectManager.Instance.GetObject<ItemObject>("sumpter_horse") == null)
		{
			CampaignEventDispatcher.Instance.RemoveListeners(this);
			{
				foreach (KeyValuePair<Hero, IssueBase> item in Campaign.Current.IssueManager.Issues.Where((KeyValuePair<Hero, IssueBase> x) => x.Value.GetType() == typeof(EscortMerchantCaravanIssue)).ToList())
				{
					item.Value.CompleteIssueWithStayAliveConditionsFailed();
				}
				return;
			}
		}
		DefaultCaravanItems.Add(DefaultItems.Grain);
		string[] array = new string[5] { "cotton", "velvet", "oil", "linen", "date_fruit" };
		foreach (string objectName in array)
		{
			ItemObject itemObject = MBObjectManager.Instance.GetObject<ItemObject>(objectName);
			if (itemObject != null)
			{
				DefaultCaravanItems.Add(itemObject);
			}
		}
	}

	private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
	{
		InitializeOnStart();
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private bool ConditionsHold(Hero issueGiver)
	{
		if (issueGiver.IsMerchant && issueGiver.CurrentSettlement != null && issueGiver.CurrentSettlement.IsTown && !issueGiver.CurrentSettlement.HasPort && issueGiver.CurrentSettlement.Town.Security <= 50f)
		{
			return issueGiver.OwnedCaravans.Count < 2;
		}
		return false;
	}

	public void OnCheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero))
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnSelected, typeof(EscortMerchantCaravanIssue), IssueBase.IssueFrequency.VeryCommon));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(EscortMerchantCaravanIssue), IssueBase.IssueFrequency.VeryCommon));
		}
	}

	private IssueBase OnSelected(in PotentialIssueData pid, Hero issueOwner)
	{
		return new EscortMerchantCaravanIssue(issueOwner);
	}
}
