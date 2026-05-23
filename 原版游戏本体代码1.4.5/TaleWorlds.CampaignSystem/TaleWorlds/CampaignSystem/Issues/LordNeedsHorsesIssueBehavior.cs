using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Extensions;
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

public class LordNeedsHorsesIssueBehavior : CampaignBehaviorBase
{
	public class LordNeedsHorsesIssue : IssueBase
	{
		private const int IssueDueTimeInDays = 20;

		internal const int IssueGiverMinimumPartySize = 50;

		internal const int IssueGiverMinimumInfantryCount = 10;

		internal const float IssuePreConditionMinPlayerRelation = -10f;

		internal const float IssuePreConditionMountsOverInfantryRatioThreshold = 0.6f;

		internal const float IssueStayingAliveMountsOverInfantryRatioThreshold = 0.8f;

		private const int AlternativeSolutionCompanionSkillThreshold = 120;

		private const int AlternativeSolutionRenownRewardOnSuccess = 1;

		private const int AlternativeSolutionRelationRewardOnSuccess = 2;

		private const int AlternativeSolutionRelationPenaltyOnFail = -5;

		private const int AlternativeSolutionTroopTierRequirement = 2;

		[SaveableField(4)]
		private int _numMountsToBeDelivered;

		[SaveableField(2)]
		private readonly ItemObject _mountObjectToBeDelivered;

		[SaveableField(3)]
		private readonly int _mountValuePerUnit;

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 2 + MathF.Ceiling(4f * base.IssueDifficultyMultiplier);

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.Duration;

		protected override int RewardGold => 500 + MathF.Ceiling(1.5f * (float)IssueNumMountsToBeDelivered * (float)_mountValuePerUnit);

		private int IssueNumMountsToBeDelivered
		{
			get
			{
				if (_numMountsToBeDelivered == 0)
				{
					_numMountsToBeDelivered = 1 + MathF.Ceiling(12f * base.IssueDifficultyMultiplier);
				}
				return _numMountsToBeDelivered;
			}
		}

		public override int AlternativeSolutionBaseNeededMenCount => 3 + MathF.Ceiling(6f * base.IssueDifficultyMultiplier);

		private int AlternativeSolutionGoldRequirement => IssueNumMountsToBeDelivered * _mountValuePerUnit;

		protected override int CompanionSkillRewardXP => (int)(500f + 700f * base.IssueDifficultyMultiplier);

		public override TextObject IssueBriefByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=TBpMffcv}Campaigning this season has taken even a higher toll on {?MOUNT_TYPE_IS_CAMEL}camels{?}horses{\\?} than it has on my men. The animals will drop dead of exhaustion while my troops soldier on. Yet if we don't keep our stocks up, the enemy will run rings around us.[if:convo_undecided_closed][ib:closed]");
				textObject.SetTextVariable("MOUNT_TYPE_IS_CAMEL", IsMountCamel(_mountObjectToBeDelivered) ? 1 : 0);
				return textObject;
			}
		}

		public override TextObject IssueAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=jjNSVzx3}What do you need, my {?ISSUE_GIVER.GENDER}lady{?}lord{\\?}?");
				textObject.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject);
				return textObject;
			}
		}

		public override TextObject IssueQuestSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=ugHO6Sa6}I need more {?MOUNT_TYPE_IS_CAMEL}camels{?}horses{\\?}, specifically, we need {MOUNT_COUNT} and they need to be {PLURAL(MOUNT_NAME)}, because we know how to use them and how they fit our needs. Bring them to me and a bag of {REWARD}{GOLD_ICON} will be right in your pocket.[if:convo_undecided_closed]");
				textObject.SetTextVariable("MOUNT_TYPE_IS_CAMEL", IsMountCamel(_mountObjectToBeDelivered) ? 1 : 0);
				textObject.SetTextVariable("MOUNT_COUNT", IssueNumMountsToBeDelivered);
				textObject.SetTextVariable("REWARD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				textObject.SetTextVariable("MOUNT_NAME", _mountObjectToBeDelivered.Name);
				return textObject;
			}
		}

		public override TextObject IssueQuestSolutionAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=0YRl5Yie}I'll bring your {?MOUNT_TYPE_IS_CAMEL}camels{?}horses{\\?} by myself.");
				textObject.SetTextVariable("MOUNT_TYPE_IS_CAMEL", IsMountCamel(_mountObjectToBeDelivered) ? 1 : 0);
				return textObject;
			}
		}

		public override bool IsThereAlternativeSolution => true;

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=YHG17dqZ}One of your companions who is good at haggling and riding would be appropriate for this task. {?MOUNT_TYPE_IS_CAMEL}Camels{?}Horses{\\?} should cost no more than {REQUIRED_GOLD_AMOUNT}{GOLD_ICON} denars and this should be covered by yourself. You'll also need some cavalry to bring the {?MOUNT_TYPE_IS_CAMEL}camels{?}horses{\\?}. A purse of {REWARD_GOLD_AMOUNT}{GOLD_ICON} denars will be waiting for you when you get the job done.[if:convo_undecided_closed]");
				textObject.SetTextVariable("MOUNT_TYPE_IS_CAMEL", IsMountCamel(_mountObjectToBeDelivered) ? 1 : 0);
				textObject.SetTextVariable("REQUIRED_GOLD_AMOUNT", AlternativeSolutionGoldRequirement);
				textObject.SetTextVariable("REWARD_GOLD_AMOUNT", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=loJja04L}My men will bring your {?MOUNT_TYPE_IS_CAMEL}camels{?}horses{\\?} as soon as possible.");
				textObject.SetTextVariable("MOUNT_TYPE_IS_CAMEL", IsMountCamel(_mountObjectToBeDelivered) ? 1 : 0);
				return textObject;
			}
		}

		public override TextObject IssueDiscussAlternativeSolution => new TextObject("{=6EJbUGpi}You will be rewarded when your companion returns with the animals we discussed.[if:convo_approving]");

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=YF6PPWlT}Very good. I'm sure your men will bring my {?MOUNT_TYPE_IS_CAMEL}camels{?}horses{\\?} as soon as possible.[if:convo_approving]");
				textObject.SetTextVariable("MOUNT_TYPE_IS_CAMEL", IsMountCamel(_mountObjectToBeDelivered) ? 1 : 0);
				return textObject;
			}
		}

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=PCigQKQ8}{QUEST_GIVER.LINK} told you that {?QUEST_GIVER.GENDER}she{?}he{\\?} needs {?MOUNT_TYPE_IS_CAMEL}camels{?}horses{\\?} for {?QUEST_GIVER.GENDER}her{?}his{\\?} party. You asked your companion {COMPANION.LINK} and {TROOP_COUNT} of your horsemen to deliver {MOUNT_COUNT} {PLURAL(MOUNT_NAME)} to {QUEST_GIVER.LINK}. {?QUEST_GIVER.GENDER}She{?}He{\\?} will pay you {REWARD}{GOLD_ICON} denars when the task is done. They will rejoin your party in {RETURN_DAYS} days.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				textObject.SetTextVariable("MOUNT_TYPE_IS_CAMEL", IsMountCamel(_mountObjectToBeDelivered) ? 1 : 0);
				textObject.SetTextVariable("REWARD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				textObject.SetTextVariable("TROOP_COUNT", AlternativeSolutionSentTroops.TotalManCount - 1);
				textObject.SetTextVariable("MOUNT_COUNT", IssueNumMountsToBeDelivered);
				textObject.SetTextVariable("MOUNT_NAME", _mountObjectToBeDelivered.Name);
				textObject.SetTextVariable("RETURN_DAYS", MathF.Ceiling(base.AlternativeSolutionReturnTimeForTroops.RemainingDaysFromNow));
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=tJc5mZua}Your companion has successfully delivered the {?MOUNT_TYPE_IS_CAMEL}camels{?}horses{\\?} {QUEST_GIVER.LINK} requested. You received {QUEST_REWARD}{GOLD_ICON} gold in return for your service.");
				textObject.SetTextVariable("MOUNT_TYPE_IS_CAMEL", IsMountCamel(_mountObjectToBeDelivered) ? 1 : 0);
				textObject.SetTextVariable("QUEST_REWARD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		public override bool IsThereLordSolution => false;

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=5zF6vI5s}Lord Needs {?MOUNT_TYPE_IS_CAMEL}Camels{?}Horses{\\?}");
				textObject.SetTextVariable("MOUNT_TYPE_IS_CAMEL", IsMountCamel(_mountObjectToBeDelivered) ? 1 : 0);
				return textObject;
			}
		}

		public override TextObject Description
		{
			get
			{
				TextObject textObject = new TextObject("{=GHbM1i6R}{QUEST_GIVER.LINK} needs {?MOUNT_TYPE_IS_CAMEL}camels{?}horses{\\?} to speed up {?QUEST_GIVER.GENDER}her{?}his{\\?} party.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				textObject.SetTextVariable("MOUNT_TYPE_IS_CAMEL", IsMountCamel(_mountObjectToBeDelivered) ? 1 : 0);
				return textObject;
			}
		}

		internal static void AutoGeneratedStaticCollectObjectsLordNeedsHorsesIssue(object o, List<object> collectedObjects)
		{
			((LordNeedsHorsesIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_mountObjectToBeDelivered);
		}

		internal static object AutoGeneratedGetMemberValue_numMountsToBeDelivered(object o)
		{
			return ((LordNeedsHorsesIssue)o)._numMountsToBeDelivered;
		}

		internal static object AutoGeneratedGetMemberValue_mountObjectToBeDelivered(object o)
		{
			return ((LordNeedsHorsesIssue)o)._mountObjectToBeDelivered;
		}

		internal static object AutoGeneratedGetMemberValue_mountValuePerUnit(object o)
		{
			return ((LordNeedsHorsesIssue)o)._mountValuePerUnit;
		}

		public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
		{
			return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2, mountedRequired: true);
		}

		public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
		{
			if (character.Tier >= 2)
			{
				return character.IsMounted;
			}
			return false;
		}

		public override bool AlternativeSolutionCondition(out TextObject explanation)
		{
			if (QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2, mountedRequired: true))
			{
				return QuestHelper.CheckGoldForAlternativeSolution(AlternativeSolutionGoldRequirement, out explanation);
			}
			return false;
		}

		public override void AlternativeSolutionStartConsequence()
		{
			Hero.MainHero.ChangeHeroGold(-AlternativeSolutionGoldRequirement);
		}

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			RelationshipChangeWithIssueOwner = 2;
			GainRenownAction.Apply(Hero.MainHero, 1f);
		}

		protected override void AlternativeSolutionEndWithFailureConsequence()
		{
			RelationshipChangeWithIssueOwner = -5;
		}

		protected override void OnGameLoad()
		{
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new LordNeedsHorsesIssueQuest(questId, base.IssueOwner, IssueNumMountsToBeDelivered, _mountObjectToBeDelivered, RewardGold, CampaignTime.DaysFromNow(20f));
		}

		public override IssueFrequency GetFrequency()
		{
			return IssueFrequency.VeryCommon;
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
			if (Hero.MainHero.IsKingdomLeader)
			{
				flag |= PreconditionFlags.MainHeroIsKingdomLeader;
			}
			if (issueGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				flag |= PreconditionFlags.AtWar;
			}
			return flag == PreconditionFlags.None;
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}

		public override bool IssueStayAliveConditions()
		{
			if (!base.IssueOwner.IsDead && base.IssueOwner.PartyBelongedTo != null && base.IssueOwner.Clan != Clan.PlayerClan && (base.IssueOwner.IsKingdomLeader || base.IssueOwner.Clan.Leader == base.IssueOwner) && ComputeMountsOverInfantryCountRatio(base.IssueOwner.PartyBelongedTo, out var _) < 0.8f)
			{
				return true;
			}
			return false;
		}

		public LordNeedsHorsesIssue(Hero issueOwner)
			: base(issueOwner, CampaignTime.DaysFromNow(20f))
		{
			MBList<ItemObject> mBList = new MBList<ItemObject>();
			foreach (ItemObject item in Items.All)
			{
				if (item.IsMountable && item.Culture == issueOwner.Culture && !item.NotMerchandise && item.Tierf > 2f && item.Tierf < 3f)
				{
					mBList.Add(item);
				}
			}
			_mountObjectToBeDelivered = mBList.GetRandomElement();
			_numMountsToBeDelivered = 1 + MathF.Ceiling(12f * base.IssueDifficultyMultiplier);
			if (_mountObjectToBeDelivered == null)
			{
				_mountObjectToBeDelivered = MBObjectManager.Instance.GetObject<ItemObject>("sumpter_horse");
			}
			_mountValuePerUnit = _mountObjectToBeDelivered?.Value ?? 0;
		}

		protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
		{
			if (issueEffect == DefaultIssueEffects.ClanInfluence)
			{
				return -0.1f;
			}
			return 0f;
		}

		public override (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
		{
			return ((hero.GetSkillValue(DefaultSkills.Trade) >= hero.GetSkillValue(DefaultSkills.Riding)) ? DefaultSkills.Trade : DefaultSkills.Riding, 120);
		}
	}

	public class LordNeedsHorsesIssueQuest : QuestBase
	{
		private const int RenownChangeOnSuccess = 1;

		private const int RelationChangeOnSuccess = 2;

		private const int RelationChangeOnFailure = -3;

		private const int RelationChangeOnTimeOut = -5;

		[SaveableField(1)]
		private readonly int _numMountsToBeDelivered;

		[SaveableField(2)]
		private readonly ItemObject _mountObjectToBeDelivered;

		[SaveableField(3)]
		private JournalLog _questJournalEntry;

		private int _numMountsInInventory;

		private int _playerInventoryVersionNo;

		private CharacterObject _questGiversAgentCharacterObject;

		public override TextObject Title
		{
			get
			{
				TextObject textObject = new TextObject("{=5zF6vI5s}Lord Needs {?MOUNT_TYPE_IS_CAMEL}Camels{?}Horses{\\?}");
				textObject.SetTextVariable("MOUNT_TYPE_IS_CAMEL", IsMountCamel(_mountObjectToBeDelivered) ? 1 : 0);
				return textObject;
			}
		}

		private TextObject JournalTaskName
		{
			get
			{
				TextObject textObject = new TextObject("{=plHZYtxF}{PLURAL(REQUESTED_MOUNT_NAME)} in Inventory: ");
				textObject.SetTextVariable("REQUESTED_MOUNT_NAME", _mountObjectToBeDelivered.Name);
				return textObject;
			}
		}

		private TextObject OnQuestStartedLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=GQ96SX4M}{QUEST_GIVER.LINK} told you that {?QUEST_GIVER.GENDER}she{?}he{\\?} needs {?MOUNT_TYPE_IS_CAMEL}camels{?}horses{\\?} for {?QUEST_GIVER.GENDER}her{?}his{\\?} party. {?QUEST_GIVER.GENDER}She{?}He{\\?} asked you to bring {MOUNT_COUNT} {PLURAL(MOUNT_NAME)} to {?QUEST_GIVER.GENDER}her{?}him{\\?} or one of {?QUEST_GIVER.GENDER}her{?}his{\\?} garrison commanders. {?QUEST_GIVER.GENDER}She{?}He{\\?} will pay you {REWARD_GOLD}{GOLD_ICON} denars when the task is done.");
				textObject.SetTextVariable("MOUNT_TYPE_IS_CAMEL", IsMountCamel(_mountObjectToBeDelivered) ? 1 : 0);
				textObject.SetTextVariable("MOUNT_COUNT", _numMountsToBeDelivered);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				textObject.SetTextVariable("MOUNT_NAME", _mountObjectToBeDelivered.Name);
				textObject.SetTextVariable("REWARD_GOLD", RewardGold);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject OnQuestSucceededLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=aGvLxsub}You have successfully delivered {MOUNT_COUNT} {PLURAL(MOUNT_NAME)} to {QUEST_GIVER.LINK} as requested. You received {GOLD_REWARD}{GOLD_ICON} denars in return for your service.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("MOUNT_COUNT", _numMountsToBeDelivered);
				textObject.SetTextVariable("MOUNT_NAME", _mountObjectToBeDelivered.Name);
				textObject.SetTextVariable("GOLD_REWARD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		private TextObject OnQuestFailedLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=yLuzvTtd}You had promised to deliver {MOUNT_COUNT} {PLURAL(MOUNT_NAME)} to {QUEST_GIVER.LINK}. But you've failed to complete this task. {QUEST_GIVER.LINK} was displeased.");
				textObject.SetTextVariable("MOUNT_COUNT", _numMountsToBeDelivered);
				textObject.SetTextVariable("MOUNT_NAME", _mountObjectToBeDelivered.Name);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject OnQuestTimedOutLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=BFEYIMdi}You had promised to deliver {MOUNT_COUNT} {PLURAL(MOUNT_NAME)} to {QUEST_GIVER.LINK}. But you've failed to complete this task in time. {QUEST_GIVER.LINK} was displeased.");
				textObject.SetTextVariable("MOUNT_COUNT", _numMountsToBeDelivered);
				textObject.SetTextVariable("MOUNT_NAME", _mountObjectToBeDelivered.Name);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject OnQuestCancelled2LogText
		{
			get
			{
				TextObject textObject = new TextObject("{=pYgl86Cr}Your clan had entered a war with the {QUEST_GIVER.LINK}'s faction. Your agreement was canceled. You can no longer deliver the {?MOUNT_TYPE_IS_CAMEL}camels{?}horses{\\?} to {?QUEST_GIVER.GENDER}her{?}him{\\?}.");
				textObject.SetTextVariable("MOUNT_TYPE_IS_CAMEL", IsMountCamel(_mountObjectToBeDelivered) ? 1 : 0);
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

		private TextObject OnQuestCancelled3LogText
		{
			get
			{
				TextObject textObject = new TextObject("{=qEh4gdjU}{QUEST_GIVER.LINK} was imprisoned and your agreement with {?QUEST_GIVER.GENDER}her{?}him{\\?} was canceled. You can no longer deliver the {?MOUNT_TYPE_IS_CAMEL}camels{?}horses{\\?} to {?QUEST_GIVER.GENDER}her{?}him{\\?}.");
				textObject.SetTextVariable("MOUNT_TYPE_IS_CAMEL", IsMountCamel(_mountObjectToBeDelivered) ? 1 : 0);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject OnQuestMountRequirementSatisfiedQuickText
		{
			get
			{
				TextObject textObject = new TextObject("{=ZCPrYXaO}You have enough {?MOUNT_TYPE_IS_CAMEL}camels{?}horses{\\?}. Return back to {QUEST_GIVER.LINK}.");
				textObject.SetTextVariable("MOUNT_TYPE_IS_CAMEL", IsMountCamel(_mountObjectToBeDelivered) ? 1 : 0);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject OnQuestMountRequirementNotSatisfiedQuickText
		{
			get
			{
				TextObject textObject = new TextObject("{=aFNfxhwz}You no longer have enough {?MOUNT_TYPE_IS_CAMEL}camels{?}horses{\\?} for {QUEST_GIVER.LINK}.");
				textObject.SetTextVariable("MOUNT_TYPE_IS_CAMEL", IsMountCamel(_mountObjectToBeDelivered) ? 1 : 0);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		public override bool IsRemainingTimeHidden => false;

		internal static void AutoGeneratedStaticCollectObjectsLordNeedsHorsesIssueQuest(object o, List<object> collectedObjects)
		{
			((LordNeedsHorsesIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_mountObjectToBeDelivered);
			collectedObjects.Add(_questJournalEntry);
		}

		internal static object AutoGeneratedGetMemberValue_numMountsToBeDelivered(object o)
		{
			return ((LordNeedsHorsesIssueQuest)o)._numMountsToBeDelivered;
		}

		internal static object AutoGeneratedGetMemberValue_mountObjectToBeDelivered(object o)
		{
			return ((LordNeedsHorsesIssueQuest)o)._mountObjectToBeDelivered;
		}

		internal static object AutoGeneratedGetMemberValue_questJournalEntry(object o)
		{
			return ((LordNeedsHorsesIssueQuest)o)._questJournalEntry;
		}

		public LordNeedsHorsesIssueQuest(string questId, Hero questGiver, int numMountsToBeDelivered, ItemObject mountObjectToBeDelivered, int rewardGold, CampaignTime duration)
			: base(questId, questGiver, duration, rewardGold)
		{
			_numMountsToBeDelivered = numMountsToBeDelivered;
			_mountObjectToBeDelivered = mountObjectToBeDelivered;
			_playerInventoryVersionNo = MobileParty.MainParty.ItemRoster.VersionNo;
			_questGiversAgentCharacterObject = null;
			AddTrackedObject(base.QuestGiver);
			SetDialogs();
			InitializeQuestOnCreation();
		}

		protected override void InitializeQuestOnGameLoad()
		{
			SetDialogs();
			_numMountsInInventory = GetNumQuestMountsInInventory();
			_playerInventoryVersionNo = MobileParty.MainParty.ItemRoster.VersionNo;
			if (_questJournalEntry == null)
			{
				_questJournalEntry = base.JournalEntries.FirstOrDefault((JournalLog x) => x.Range == _numMountsToBeDelivered && x.Type == LogType.Discreate);
				if (_questJournalEntry == null)
				{
					if (base.JournalEntries.Count > 0)
					{
						for (int num = 0; num < base.JournalEntries.Count; num++)
						{
							if (base.JournalEntries[num].Type == LogType.Discreate)
							{
								RemoveLog(base.JournalEntries[num]);
							}
						}
						_questJournalEntry = AddDiscreteLog(OnQuestStartedLogText, JournalTaskName, MBMath.ClampInt(_numMountsInInventory, 0, _numMountsToBeDelivered), _numMountsToBeDelivered);
					}
					else
					{
						_questJournalEntry = AddDiscreteLog(OnQuestStartedLogText, JournalTaskName, MBMath.ClampInt(_numMountsInInventory, 0, _numMountsToBeDelivered), _numMountsToBeDelivered);
					}
				}
			}
			UpdateQuestJournalEntry();
		}

		protected override void SetDialogs()
		{
			Campaign.Current.ConversationManager.AddDialogFlow(GetGarrisonCommanderDialogFlow(), this);
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine("{=qR3gQrLi}Splendid. We'll need to keep moving around, though, so it might be tricky to find us. My fellow nobles will usually know where to find me though, if you ask them.[if:convo_approving][ib:hip2]").Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.Consequence(OnQuestAccepted)
				.CloseDialog();
			new TextObject(IsMountCamel(_mountObjectToBeDelivered) ? "{=nysBXpEO}camels" : "{=6YbGQDme}horses");
			TextObject textObject = new TextObject("{=bmW77NvO}What about my {?MOUNT_TYPE_IS_CAMEL}camels{?}horses{\\?}?");
			textObject.SetTextVariable("MOUNT_TYPE_IS_CAMEL", IsMountCamel(_mountObjectToBeDelivered) ? 1 : 0);
			TextObject textObject2 = new TextObject("{=TLcnbALt}Here are your {?MOUNT_TYPE_IS_CAMEL}camels{?}horses{\\?} my {?QUEST_GIVER.GENDER}lady{?}lord{\\?}.");
			textObject2.SetTextVariable("MOUNT_TYPE_IS_CAMEL", IsMountCamel(_mountObjectToBeDelivered) ? 1 : 0);
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject2);
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject2);
			TextObject textObject3 = new TextObject("{=6bt9jYai}I am still looking for the {?MOUNT_TYPE_IS_CAMEL}camels{?}horses{\\?} you have requested.");
			textObject3.SetTextVariable("MOUNT_TYPE_IS_CAMEL", IsMountCamel(_mountObjectToBeDelivered) ? 1 : 0);
			TextObject textObject4 = new TextObject("{=pV7DLTAu}I am sorry, my {?QUEST_GIVER.GENDER}lady{?}lord{\\?}. I cannot deliver the {?MOUNT_TYPE_IS_CAMEL}camels{?}horses{\\?} you requested.");
			textObject4.SetTextVariable("MOUNT_TYPE_IS_CAMEL", IsMountCamel(_mountObjectToBeDelivered) ? 1 : 0);
			StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject4);
			TextObject textObject5 = new TextObject("{=inkr6Dzy}Too bad. Perhaps someone else will be able to get me the {?MOUNT_TYPE_IS_CAMEL}camels{?}horses{\\?} that I need.");
			textObject5.SetTextVariable("MOUNT_TYPE_IS_CAMEL", IsMountCamel(_mountObjectToBeDelivered) ? 1 : 0);
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(textObject).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += MapEventHelper.OnConversationEnd;
				})
				.BeginPlayerOptions()
				.PlayerOption(textObject2)
				.Condition(() => GetNumQuestMountsInInventory() >= _numMountsToBeDelivered)
				.NpcLine("{=9HJbLneH}Thank you for your help, {PLAYER.NAME}. Here is the purse I promised you. Farewell.[if:convo_happy][ib:hip]")
				.Consequence(base.CompleteQuestWithSuccess)
				.CloseDialog()
				.PlayerOption(textObject3)
				.NpcLine("{=cH0iAEfq}Please take care of this as quickly as you can. I need those animals.[if:convo_undecided_closed]")
				.CloseDialog()
				.PlayerOption(textObject4)
				.NpcLine(textObject5)
				.Consequence(delegate
				{
					OnQuestDeclined();
				})
				.CloseDialog()
				.EndPlayerOptions();
		}

		private DialogFlow GetGarrisonCommanderDialogFlow()
		{
			TextObject textObject = new TextObject("{=JxhEOqyT}We were waiting for you, {?PLAYER.GENDER}madam{?}sir{\\?}. Have you brought the horses that our {?ISSUE_OWNER.GENDER}lady{?}lord{\\?} requested?");
			StringHelpers.SetCharacterProperties("ISSUE_OWNER", base.QuestGiver.CharacterObject, textObject);
			return DialogFlow.CreateDialogFlow("start", 300).NpcLine(textObject).Condition(() => CharacterObject.OneToOneConversationCharacter == _questGiversAgentCharacterObject)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=ZEy3gE7w}Here are your horses."))
				.Condition(() => GetNumQuestMountsInInventory() >= _numMountsToBeDelivered)
				.NpcLine(new TextObject("{=g8qb3Ame}Thank you."))
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += base.CompleteQuestWithSuccess;
				})
				.CloseDialog()
				.PlayerOption(new TextObject("{=G5tyQj6N}Not yet."))
				.NpcLine(new TextObject("{=sjTpEzju}Very well. We'll keep waiting."))
				.CloseDialog()
				.EndPlayerOptions();
		}

		private void OnQuestAccepted()
		{
			StartQuest();
			_numMountsInInventory = GetNumQuestMountsInInventory();
			_questJournalEntry = AddDiscreteLog(OnQuestStartedLogText, JournalTaskName, MBMath.ClampInt(_numMountsInInventory, 0, _numMountsToBeDelivered), _numMountsToBeDelivered);
		}

		private void OnQuestDeclined()
		{
			AddLog(OnQuestFailedLogText);
			CompleteQuestWithFail();
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.PlayerInventoryExchangeEvent.AddNonSerializedListener(this, OnPlayerInventoryExchange);
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
			CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		}

		private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
		{
			if (!Campaign.Current.ConversationManager.IsConversationFlowActive && hero == Hero.MainHero && mobileParty == MobileParty.MainParty && settlement.IsFortification && settlement.OwnerClan == base.QuestGiver.Clan)
			{
				CharacterObject characterObject = null;
				MBList<TroopRosterElement> mBList = Settlement.CurrentSettlement.Town.GarrisonParty?.MemberRoster?.GetTroopRoster();
				characterObject = ((mBList != null && mBList.Count != 0) ? TaleWorlds.Core.Extensions.MaxBy(mBList, (TroopRosterElement troop) => troop.Character.Tier).Character : base.QuestGiver.Culture.Guard);
				_questGiversAgentCharacterObject = characterObject;
				CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty), new ConversationCharacterData(_questGiversAgentCharacterObject));
			}
		}

		private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
			{
				QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
			}
		}

		private void UpdateQuestJournalEntry()
		{
			int num = MBMath.ClampInt(_numMountsInInventory, 0, _numMountsToBeDelivered);
			if (num != _questJournalEntry.CurrentProgress)
			{
				UpdateQuestTaskStage(_questJournalEntry, num);
			}
		}

		private void CheckAndHandleQuestSuccessConditions()
		{
			int numQuestMountsInInventory = GetNumQuestMountsInInventory();
			if (numQuestMountsInInventory != _numMountsInInventory)
			{
				if (_numMountsInInventory < _numMountsToBeDelivered && numQuestMountsInInventory >= _numMountsToBeDelivered)
				{
					MBInformationManager.AddQuickInformation(OnQuestMountRequirementSatisfiedQuickText);
				}
				else if (_numMountsInInventory >= _numMountsToBeDelivered && numQuestMountsInInventory < _numMountsToBeDelivered)
				{
					MBInformationManager.AddQuickInformation(OnQuestMountRequirementNotSatisfiedQuickText);
				}
				_numMountsInInventory = numQuestMountsInInventory;
				UpdateQuestJournalEntry();
			}
		}

		protected override void HourlyTick()
		{
			int versionNo = MobileParty.MainParty.ItemRoster.VersionNo;
			if (_playerInventoryVersionNo != versionNo)
			{
				CheckAndHandleQuestSuccessConditions();
				_playerInventoryVersionNo = versionNo;
			}
		}

		private void OnPlayerInventoryExchange(List<(ItemRosterElement, int)> purchasedItems, List<(ItemRosterElement, int)> soldItems, bool isTrading)
		{
			bool flag = false;
			foreach (var (itemRosterElement, _) in purchasedItems)
			{
				if (itemRosterElement.EquipmentElement.Item == _mountObjectToBeDelivered)
				{
					flag = true;
					break;
				}
			}
			bool flag2 = false;
			foreach (var (itemRosterElement, _) in soldItems)
			{
				if (itemRosterElement.EquipmentElement.Item == _mountObjectToBeDelivered)
				{
					flag2 = true;
					break;
				}
			}
			if (flag || flag2)
			{
				CheckAndHandleQuestSuccessConditions();
			}
			_playerInventoryVersionNo = MobileParty.MainParty.ItemRoster.VersionNo;
		}

		private void OnHeroPrisonerTaken(PartyBase capturer, Hero prisoner)
		{
			if (prisoner == base.QuestGiver)
			{
				CompleteQuestWithCancel(OnQuestCancelled3LogText);
			}
		}

		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (base.QuestGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				CompleteQuestWithCancel(OnQuestCancelled2LogText);
			}
		}

		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
		{
			QuestHelper.CheckWarDeclarationAndFailOrCancelTheQuest(this, faction1, faction2, detail, PlayerDeclaredWarQuestLogText, OnQuestCancelled2LogText);
		}

		protected override void OnCompleteWithSuccess()
		{
			AddLog(OnQuestSucceededLogText);
			int num = _numMountsToBeDelivered;
			foreach (ItemRosterElement item in MobileParty.MainParty.ItemRoster)
			{
				if (item.EquipmentElement.Item == _mountObjectToBeDelivered)
				{
					int amount = item.Amount;
					if (amount >= num)
					{
						GiveMounts(Hero.MainHero, base.QuestGiver, item, num);
						break;
					}
					num -= amount;
					GiveMounts(Hero.MainHero, base.QuestGiver, item, amount);
				}
			}
			GainRenownAction.Apply(Hero.MainHero, 1f);
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
			RelationshipChangeWithQuestGiver = 2;
		}

		private void GiveMounts(Hero giver, Hero receiver, ItemRosterElement item, int count)
		{
			if (giver.PartyBelongedTo != null)
			{
				giver.PartyBelongedTo.Party.ItemRoster.AddToCounts(item.EquipmentElement, -count);
				if (receiver.PartyBelongedTo != null)
				{
					receiver.PartyBelongedTo.Party.ItemRoster.AddToCounts(item.EquipmentElement, count);
				}
			}
		}

		protected override void OnFinalize()
		{
			RemoveTrackedObject(base.QuestGiver);
		}

		public override void OnFailed()
		{
			RelationshipChangeWithQuestGiver = -3;
		}

		protected override void OnTimedOut()
		{
			AddLog(OnQuestTimedOutLogText);
			RelationshipChangeWithQuestGiver = -5;
		}

		private int GetNumQuestMountsInInventory()
		{
			int num = 0;
			foreach (ItemRosterElement item in MobileParty.MainParty.ItemRoster)
			{
				if (item.EquipmentElement.Item == _mountObjectToBeDelivered)
				{
					num += item.Amount;
				}
			}
			return num;
		}
	}

	public class LordNeedsHorsesIssueBehaviorTypeDefiner : SaveableTypeDefiner
	{
		public LordNeedsHorsesIssueBehaviorTypeDefiner()
			: base(510000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(LordNeedsHorsesIssue), 1);
			AddClassDefinition(typeof(LordNeedsHorsesIssueQuest), 2);
		}

		protected override void DefineEnumTypes()
		{
		}
	}

	private const IssueBase.IssueFrequency LordNeedsHorsesIssueFrequency = IssueBase.IssueFrequency.VeryCommon;

	private void OnCheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero))
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnIssueSelected, typeof(LordNeedsHorsesIssue), IssueBase.IssueFrequency.VeryCommon));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(LordNeedsHorsesIssue), IssueBase.IssueFrequency.VeryCommon));
		}
	}

	private IssueBase OnIssueSelected(in PotentialIssueData pid, Hero issueOwner)
	{
		return new LordNeedsHorsesIssue(issueOwner);
	}

	private bool ConditionsHold(Hero issueGiver)
	{
		if ((issueGiver.IsKingdomLeader || issueGiver.Clan?.Leader == issueGiver) && issueGiver.Culture.StringId != "nord" && !issueGiver.IsMinorFactionHero && issueGiver.Clan != Clan.PlayerClan)
		{
			MobileParty partyBelongedTo = issueGiver.PartyBelongedTo;
			if (partyBelongedTo != null && partyBelongedTo.Party.MemberRoster.TotalManCount > 50)
			{
				int numInfantry;
				float num = ComputeMountsOverInfantryCountRatio(partyBelongedTo, out numInfantry);
				if (numInfantry >= 10)
				{
					return num < 0.6f;
				}
				return false;
			}
		}
		return false;
	}

	public static float ComputeMountsOverInfantryCountRatio(MobileParty issueParty, out int numInfantry)
	{
		if (issueParty == null)
		{
			Debug.FailedAssert("Cannot compute mounts over infantry ratio as related party is null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Issues\\LordNeedsHorsesIssueBehavior.cs", "ComputeMountsOverInfantryCountRatio", 917);
			numInfantry = 0;
			return float.MaxValue;
		}
		int numberOfMounts = issueParty.ItemRoster.NumberOfMounts;
		numInfantry = 0;
		foreach (TroopRosterElement item in issueParty.Party.MemberRoster.GetTroopRoster())
		{
			if (item.Character.IsInfantry)
			{
				numInfantry += item.Number;
			}
		}
		if (numInfantry != 0)
		{
			return (float)numberOfMounts / (float)numInfantry;
		}
		return float.MaxValue;
	}

	public static bool IsMountCamel(ItemObject mountObject)
	{
		if (mountObject?.ItemComponent is HorseComponent horseComponent)
		{
			return horseComponent.Monster.MonsterUsage == "camel";
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
