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
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace SandBox.Issues;

public class FamilyFeudIssueBehavior : CampaignBehaviorBase
{
	public class FamilyFeudIssueTypeDefiner : SaveableTypeDefiner
	{
		public FamilyFeudIssueTypeDefiner()
			: base(1087000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(FamilyFeudIssue), 1);
			AddClassDefinition(typeof(FamilyFeudIssueQuest), 2);
		}
	}

	public class FamilyFeudIssueMissionBehavior : MissionLogic
	{
		private Action<Agent, Agent, int> OnAgentHitAction;

		public FamilyFeudIssueMissionBehavior(Action<Agent, Agent, int> agentHitAction)
		{
			OnAgentHitAction = agentHitAction;
		}

		public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
		{
			OnAgentHitAction?.Invoke(affectedAgent, affectorAgent, blow.InflictedDamage);
		}
	}

	public class FamilyFeudIssue : IssueBase
	{
		private const int CompanionRequiredSkillLevel = 120;

		private const int QuestTimeLimit = 20;

		private const int IssueDuration = 30;

		private const int TroopTierForAlternativeSolution = 2;

		[SaveableField(10)]
		private Settlement _targetVillage;

		[SaveableField(20)]
		private Hero _targetNotable;

		public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.FailureRisk;

		public override int AlternativeSolutionBaseNeededMenCount => 3 + TaleWorlds.Library.MathF.Ceiling(5f * base.IssueDifficultyMultiplier);

		protected override int AlternativeSolutionBaseDurationInDaysInternal => 3 + TaleWorlds.Library.MathF.Ceiling(7f * base.IssueDifficultyMultiplier);

		protected override int RewardGold => (int)(350f + 1500f * base.IssueDifficultyMultiplier);

		[SaveableProperty(30)]
		public override Hero CounterOfferHero { get; protected set; }

		public override int NeededInfluenceForLordSolution => 20;

		protected override int CompanionSkillRewardXP => (int)(500f + 700f * base.IssueDifficultyMultiplier);

		protected override TextObject AlternativeSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=zRJ1bQFO}{ISSUE_GIVER.LINK}, a landowner from {ISSUE_GIVER_SETTLEMENT}, told you about an incident that is about to turn into an ugly feud. One of the youngsters killed another in an accident and the victim's family refused blood money as compensation and wants blood. You decided to leave {COMPANION.LINK} with some men for {RETURN_DAYS} days to let things cool down. They should return with the reward of {REWARD_GOLD}{GOLD_ICON} denars as promised by {ISSUE_GIVER.LINK} after {RETURN_DAYS} days.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				textObject.SetTextVariable("ISSUE_GIVER_SETTLEMENT", base.IssueOwner.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				textObject.SetTextVariable("REWARD_GOLD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		public override bool IsThereAlternativeSolution => true;

		public override bool IsThereLordSolution => true;

		public override TextObject IssueBriefByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=7qPda0SA}Yes... We do have a problem. One of my relatives fell victim to his temper during a quarrel and killed a man from {TARGET_VILLAGE}.[ib:normal2][if:convo_dismayed] We offered to pay blood money but the family of the deceased have stubbornly refused it. As it turns out, the deceased is kin to {TARGET_NOTABLE}, an elder of this region and now the men of {TARGET_VILLAGE} have sworn to kill my relative.");
				textObject.SetTextVariable("TARGET_VILLAGE", _targetVillage.Name);
				textObject.SetTextVariable("TARGET_NOTABLE", _targetNotable.Name);
				return textObject;
			}
		}

		public override TextObject IssueAcceptByPlayer => new TextObject("{=XX3sWsVX}This sounds pretty serious. Go on.");

		public override TextObject IssueQuestSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=mgUoXwZt}My family is concerned for the boy's life. He has gone hiding around the village commons. We need someone who can protect him until [ib:normal][if:convo_normal]{TARGET_NOTABLE.LINK} sees reason, accepts the blood money and ends the feud. We would be eternally grateful, if you can help my relative and take him with you for a while maybe.");
				StringHelpers.SetCharacterProperties("TARGET_NOTABLE", _targetNotable.CharacterObject, textObject);
				textObject.SetTextVariable("TARGET_VILLAGE", _targetVillage.Name);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=cDYz49kZ}You can keep my relative under your protection for a time until the calls for vengeance die down.[ib:closed][if:convo_pondering] Maybe you can leave one of your warrior companions and {ALTERNATIVE_TROOP_COUNT} men with him to protect him.");
				textObject.SetTextVariable("ALTERNATIVE_TROOP_COUNT", GetTotalAlternativeSolutionNeededMenCount());
				return textObject;
			}
		}

		protected override TextObject LordSolutionStartLog
		{
			get
			{
				TextObject textObject = new TextObject("{=oJt4bemH}{QUEST_GIVER.LINK}, a landowner from {QUEST_SETTLEMENT}, told you about an incident that is about to turn into an ugly feud. One young man killed another in an quarrel and the victim's family refused blood money compensation, demanding vengeance instead.");
				StringHelpers.SetCharacterProperties("TARGET_NOTABLE", _targetNotable.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_SETTLEMENT", base.IssueOwner.CurrentSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		protected override TextObject LordSolutionCounterOfferRefuseLog
		{
			get
			{
				TextObject textObject = new TextObject("{=JqN5BSjN}As the dispenser of justice in the district, you decided to allow {TARGET_NOTABLE.LINK} to take vengeance for {?TARGET_NOTABLE.GENDER}her{?}his{\\?} kinsman. You failed to protect the culprit as you promised. {QUEST_GIVER.LINK} is furious.");
				StringHelpers.SetCharacterProperties("TARGET_NOTABLE", _targetNotable.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		protected override TextObject LordSolutionCounterOfferAcceptLog
		{
			get
			{
				TextObject textObject = new TextObject("{=UxrXNSW7}As the ruler, you have let {TARGET_NOTABLE.LINK} to take {?TARGET_NOTABLE.GENDER}her{?}him{\\?} kinsman's vengeance and failed to protect the boy as you have promised to {QUEST_GIVER.LINK}. {QUEST_GIVER.LINK} is furious.");
				StringHelpers.SetCharacterProperties("TARGET_NOTABLE", _targetNotable.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
				return textObject;
			}
		}

		public override TextObject IssueLordSolutionExplanationByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=tsjwrZCZ}I am sure that, as {?PLAYER.GENDER}lady{?}lord{\\?} of this district, you will not let these unlawful threats go unpunished. As the lord of the region, you can talk to {TARGET_NOTABLE.LINK} and force him to accept the blood money.");
				StringHelpers.SetCharacterProperties("TARGET_NOTABLE", _targetNotable.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject IssuePlayerResponseAfterLordExplanation => new TextObject("{=A3GfCPUb}I'm not sure about using my authority in this way. Is there any other way to solve this?");

		public override TextObject IssuePlayerResponseAfterAlternativeExplanation => new TextObject("{=8EaCJ2uw}What else can I do?");

		public override TextObject IssueLordSolutionAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=Du31GKSb}As the magistrate of this district, I hereby order that blood money shall be accepted. This is a crime of passion, not malice. Tell {TARGET_NOTABLE.LINK} to take the silver or face my wrath!");
				StringHelpers.SetCharacterProperties("TARGET_NOTABLE", _targetNotable.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject IssueLordSolutionResponseByIssueGiver => new TextObject("{=xNyLPMnx}Thank you, my {?PLAYER.GENDER}lady{?}lord{\\?}, thank you.");

		public override TextObject IssueLordSolutionCounterOfferExplanationByOtherNpc
		{
			get
			{
				TextObject textObject = new TextObject("{=vjk2q3OT}{?PLAYER.GENDER}Madam{?}Sir{\\?}, {TARGET_NOTABLE.LINK}'s nephew murdered one of my kinsman, [ib:aggressive][if:convo_bared_teeth]and it is our right to take vengeance on the murderer. Custom gives us the right of vengeance. Everyone must know that we are willing to avenge our sons, or others will think little of killing them. Does it do us good to be a clan of old men and women, drowning in silver, if all our sons are slain? Please sir, allow us to take vengeance. We promise we won't let this turn into a senseless blood feud.");
				StringHelpers.SetCharacterProperties("TARGET_NOTABLE", _targetNotable.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
				return textObject;
			}
		}

		public override TextObject IssueLordSolutionCounterOfferBriefByOtherNpc => new TextObject("{=JhbbB2dp}My {?PLAYER.GENDER}lady{?}lord{\\?}, may I have a word please?");

		public override TextObject IssueLordSolutionCounterOfferAcceptByPlayer => new TextObject("{=TIVHLAjy}You may have a point. I hereby revoke my previous decision.");

		public override TextObject IssueLordSolutionCounterOfferAcceptResponseByOtherNpc => new TextObject("{=A9uSikTY}Thank you my {?PLAYER.GENDER}lady{?}lord{\\?}.");

		public override TextObject IssueLordSolutionCounterOfferDeclineByPlayer => new TextObject("{=Vs9DfZmJ}No. My word is final. You will have to take the blood money.");

		public override TextObject IssueLordSolutionCounterOfferDeclineResponseByOtherNpc => new TextObject("{=3oaVUNdr}I hope you won't be [if:convo_disbelief]regret with your decision, my {?PLAYER.GENDER}lady{?}lord{\\?}.");

		public override TextObject IssueQuestSolutionAcceptByPlayer => new TextObject("{=VcfZdKcp}Don't worry, I will protect your relative.");

		public override TextObject Title => new TextObject("{=ZpDQxmzJ}Family Feud");

		public override TextObject Description
		{
			get
			{
				TextObject textObject = new TextObject("{=aSZvZRYC}A relative of {QUEST_GIVER.NAME} kills a relative of {TARGET_NOTABLE.NAME}. {QUEST_GIVER.NAME} offers to pay blood money for the crime but {TARGET_NOTABLE.NAME} wants revenge.");
				StringHelpers.SetCharacterProperties("TARGET_NOTABLE", _targetNotable.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionAcceptByPlayer
		{
			get
			{
				TextObject textObject = new TextObject("{=9ZngZ6W7}I will have one of my companions and {REQUIRED_TROOP_AMOUNT} of my men protect your kinsman for {RETURN_DAYS} days. ");
				textObject.SetTextVariable("REQUIRED_TROOP_AMOUNT", GetTotalAlternativeSolutionNeededMenCount());
				textObject.SetTextVariable("RETURN_DAYS", GetTotalAlternativeSolutionDurationInDays());
				return textObject;
			}
		}

		public override TextObject IssueDiscussAlternativeSolution
		{
			get
			{
				TextObject textObject = new TextObject("{=n9QRnxbC}I have no doubt that {TARGET_NOTABLE.LINK} will have to accept[ib:closed][if:convo_grateful] the offer after seeing the boy with that many armed men behind him. Thank you, {?PLAYER.GENDER}madam{?}sir{\\?}, for helping to ending this without more blood.");
				textObject.SetCharacterProperties("TARGET_NOTABLE", _targetNotable.CharacterObject);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionResponseByIssueGiver
		{
			get
			{
				TextObject textObject = new TextObject("{=MaGPKGHA}Thank you my {?PLAYER.GENDER}lady{?}lord{\\?}. [if:convo_pondering]I am sure your men will protect the boy and {TARGET_NOTABLE.LINK} will have nothing to do but to accept the blood money. I have to add, I'm ready to pay you {REWARD_GOLD}{GOLD_ICON} denars for your trouble.");
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
				StringHelpers.SetCharacterProperties("TARGET_NOTABLE", _targetNotable.CharacterObject, textObject);
				textObject.SetTextVariable("REWARD_GOLD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		public override TextObject IssueAsRumorInSettlement
		{
			get
			{
				TextObject textObject = new TextObject("{=lmVCRD4Q}I hope {QUEST_GIVER.LINK} [if:convo_disbelief]can work out that trouble with {?QUEST_GIVER.GENDER}her{?}his{\\?} kinsman.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject IssueAlternativeSolutionSuccessLog
		{
			get
			{
				TextObject textObject = new TextObject("{=vS6oZJPA}Your companion {COMPANION.LINK} and your men returns with the news of their success. Apparently {TARGET_NOTABLE.LINK} and {?TARGET_NOTABLE.GENDER}her{?}his{\\?} thugs finds the culprit and tries to murder him but your men manages to drive them away. {COMPANION.LINK} tells you that they bloodied their noses so badly that they wouldn’t dare to try again. {QUEST_GIVER.LINK} is grateful and sends {?QUEST_GIVER.GENDER}her{?}his{\\?} regards with a purse full of {REWARD}{GOLD_ICON} denars.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("TARGET_NOTABLE", _targetNotable.CharacterObject, textObject);
				textObject.SetTextVariable("REWARD", RewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		public FamilyFeudIssue(Hero issueOwner, Hero targetNotable, Settlement targetVillage)
			: base(issueOwner, CampaignTime.DaysFromNow(30f))
		{
			_targetNotable = targetNotable;
			_targetVillage = targetVillage;
		}

		public override void OnHeroCanBeSelectedInInventoryInfoIsRequested(Hero hero, ref bool result)
		{
			CommonResrictionInfoIsRequested(hero, ref result);
		}

		public override void OnHeroCanHavePartyRoleOrBeGovernorInfoIsRequested(Hero hero, ref bool result)
		{
			CommonResrictionInfoIsRequested(hero, ref result);
		}

		public override void OnHeroCanLeadPartyInfoIsRequested(Hero hero, ref bool result)
		{
			CommonResrictionInfoIsRequested(hero, ref result);
		}

		public override void OnHeroCanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
		{
			CommonResrictionInfoIsRequested(hero, ref result);
		}

		private void CommonResrictionInfoIsRequested(Hero hero, ref bool result)
		{
			if (_targetNotable == hero)
			{
				result = false;
			}
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
			return ((hero.GetSkillValue(DefaultSkills.Athletics) >= hero.GetSkillValue(DefaultSkills.Charm)) ? DefaultSkills.Athletics : DefaultSkills.Charm, 120);
		}

		protected override void LordSolutionConsequenceWithAcceptCounterOffer()
		{
			TraitLevelingHelper.OnIssueSolvedThroughBetrayal(base.IssueOwner, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -50)
			});
			RelationshipChangeWithIssueOwner = -10;
			ChangeRelationAction.ApplyPlayerRelation(_targetNotable, 5);
			base.IssueOwner.CurrentSettlement.Village.Bound.Town.Prosperity -= 5f;
			base.IssueOwner.CurrentSettlement.Village.Bound.Town.Security -= 5f;
		}

		protected override void LordSolutionConsequenceWithRefuseCounterOffer()
		{
			ApplySuccessRewards();
		}

		public override bool LordSolutionCondition(out TextObject explanation)
		{
			if (base.IssueOwner.CurrentSettlement.OwnerClan == Clan.PlayerClan)
			{
				explanation = null;
				return true;
			}
			explanation = new TextObject("{=9y0zpKUF}You need to be the owner of this settlement!");
			return false;
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

		protected override void AlternativeSolutionEndWithSuccessConsequence()
		{
			ApplySuccessRewards();
			float randomFloat = MBRandom.RandomFloat;
			SkillObject skillObject = null;
			skillObject = ((randomFloat <= 0.33f) ? DefaultSkills.OneHanded : ((!(randomFloat <= 0.66f)) ? DefaultSkills.Polearm : DefaultSkills.TwoHanded));
			base.AlternativeSolutionHero.AddSkillXp(skillObject, (int)(500f + 700f * base.IssueDifficultyMultiplier));
		}

		protected override void AlternativeSolutionEndWithFailureConsequence()
		{
			RelationshipChangeWithIssueOwner = -10;
			ChangeRelationAction.ApplyPlayerRelation(_targetNotable, 5);
			base.IssueOwner.CurrentSettlement.Village.Bound.Town.Security -= 5f;
			base.IssueOwner.CurrentSettlement.Village.Bound.Town.Prosperity -= 5f;
		}

		private void ApplySuccessRewards()
		{
			GainRenownAction.Apply(Hero.MainHero, 1f);
			RelationshipChangeWithIssueOwner = 10;
			ChangeRelationAction.ApplyPlayerRelation(_targetNotable, -5);
			base.IssueOwner.CurrentSettlement.Village.Bound.Town.Security += 10f;
		}

		protected override void AfterIssueCreation()
		{
			CounterOfferHero = base.IssueOwner.CurrentSettlement.Notables.FirstOrDefault((Hero x) => x.CharacterObject.IsHero && x.CharacterObject.HeroObject != base.IssueOwner);
		}

		protected override void OnGameLoad()
		{
		}

		protected override void HourlyTick()
		{
		}

		protected override QuestBase GenerateIssueQuest(string questId)
		{
			return new FamilyFeudIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(20f), _targetVillage, _targetNotable, RewardGold);
		}

		public override IssueFrequency GetFrequency()
		{
			return IssueFrequency.Rare;
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
			if (FactionManager.IsAtWarAgainstFaction(issueGiver.CurrentSettlement.MapFaction, Hero.MainHero.MapFaction))
			{
				flag |= PreconditionFlags.AtWar;
			}
			if (Clan.PlayerClan.Companions.Count >= Clan.PlayerClan.CompanionLimit)
			{
				flag |= PreconditionFlags.CompanionLimitReached;
			}
			return flag == PreconditionFlags.None;
		}

		public override bool IssueStayAliveConditions()
		{
			if (_targetNotable != null && _targetNotable.IsActive)
			{
				if (CounterOfferHero != null)
				{
					if (CounterOfferHero.IsActive)
					{
						return CounterOfferHero.CurrentSettlement == base.IssueSettlement;
					}
					return false;
				}
				return true;
			}
			return false;
		}

		protected override void CompleteIssueWithTimedOutConsequences()
		{
		}

		internal static void AutoGeneratedStaticCollectObjectsFamilyFeudIssue(object o, List<object> collectedObjects)
		{
			((FamilyFeudIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_targetVillage);
			collectedObjects.Add(_targetNotable);
			collectedObjects.Add(CounterOfferHero);
		}

		internal static object AutoGeneratedGetMemberValueCounterOfferHero(object o)
		{
			return ((FamilyFeudIssue)o).CounterOfferHero;
		}

		internal static object AutoGeneratedGetMemberValue_targetVillage(object o)
		{
			return ((FamilyFeudIssue)o)._targetVillage;
		}

		internal static object AutoGeneratedGetMemberValue_targetNotable(object o)
		{
			return ((FamilyFeudIssue)o)._targetNotable;
		}
	}

	public class FamilyFeudIssueQuest : QuestBase
	{
		private const int CustomCulpritAgentHealth = 350;

		private const int CustomTargetNotableAgentHealth = 350;

		public const string CommonAreaTag = "alley_2";

		[SaveableField(10)]
		private readonly Settlement _targetSettlement;

		[SaveableField(20)]
		private Hero _targetNotable;

		[SaveableField(30)]
		private Hero _culprit;

		[SaveableField(40)]
		private bool _culpritJoinedPlayerParty;

		[SaveableField(50)]
		private bool _checkForMissionEvents;

		[SaveableField(70)]
		private int _rewardGold;

		private bool _isCulpritDiedInMissionFight;

		private bool _isPlayerKnockedOutMissionFight;

		private bool _isNotableKnockedDownInMissionFight;

		private bool _conversationAfterFightIsDone;

		private bool _persuationInDoneAndSuccessfull;

		private bool _playerBetrayedCulprit;

		private Agent _notableAgent;

		private Agent _culpritAgent;

		private CharacterObject _notableGangsterCharacterObject;

		private List<LocationCharacter> _notableThugs;

		private PersuasionTask _task;

		private const PersuasionDifficulty Difficulty = PersuasionDifficulty.MediumHard;

		public override bool IsRemainingTimeHidden => false;

		private bool FightEnded
		{
			get
			{
				if (!_isCulpritDiedInMissionFight && !_isNotableKnockedDownInMissionFight)
				{
					return _persuationInDoneAndSuccessfull;
				}
				return true;
			}
		}

		public override TextObject Title => new TextObject("{=ZpDQxmzJ}Family Feud");

		private TextObject PlayerStartsQuestLogText1
		{
			get
			{
				TextObject textObject = new TextObject("{=rjHQpVDZ}{QUEST_GIVER.LINK} a landowner from {QUEST_GIVER_SETTLEMENT}, told you about an incident that is about to turn into an ugly feud. One of the youngsters killed another during a quarrel and the victim's family refuses the blood money as compensation and wants blood.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_GIVER_SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject PlayerStartsQuestLogText2
		{
			get
			{
				TextObject textObject = new TextObject("{=fgRq7kF2}You agreed to talk to {CULPRIT.LINK} in {QUEST_GIVER_SETTLEMENT} first and convince him to go to {TARGET_NOTABLE.LINK} with you in {TARGET_SETTLEMENT} and mediate the issue between them peacefully and end unnecessary bloodshed. {QUEST_GIVER.LINK} said {?QUEST_GIVER.GENDER}she{?}he{\\?} will pay you {REWARD_GOLD} once the boy is safe again.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("TARGET_NOTABLE", _targetNotable.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("CULPRIT", _culprit.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_GIVER_SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				textObject.SetTextVariable("REWARD_GOLD", _rewardGold);
				return textObject;
			}
		}

		private TextObject SuccessQuestSolutionLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=KJ61SXEU}You have successfully protected {CULPRIT.LINK} from harm as you have promised. {QUEST_GIVER.LINK} is grateful for your service and sends his regards with a purse full of {REWARD_GOLD}{GOLD_ICON} denars for your trouble.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("CULPRIT", _culprit.CharacterObject, textObject);
				textObject.SetTextVariable("REWARD_GOLD", _rewardGold);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return textObject;
			}
		}

		private TextObject CulpritJoinedPlayerPartyLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=s5fXZf2f}You have convinced {CULPRIT.LINK} to go to {TARGET_SETTLEMENT} to face {TARGET_NOTABLE.LINK} to try to solve this issue peacefully. He agreed on the condition that you protect him from his victim's angry relatives.");
				StringHelpers.SetCharacterProperties("CULPRIT", _culprit.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("TARGET_NOTABLE", _targetNotable.CharacterObject, textObject);
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject QuestGiverVillageRaidedBeforeTalkingToCulpritCancel
		{
			get
			{
				TextObject textObject = new TextObject("{=gJG0xmAq}{QUEST_GIVER.LINK}'s village {QUEST_SETTLEMENT} was raided. Your agreement with {QUEST_GIVER.LINK} is canceled.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				textObject.SetTextVariable("QUEST_SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject TargetVillageRaidedBeforeTalkingToCulpritCancel
		{
			get
			{
				TextObject textObject = new TextObject("{=WqY4nvHc}{TARGET_NOTABLE.LINK}'s village {TARGET_SETTLEMENT} was raided. Your agreement with {QUEST_GIVER.LINK} is canceled.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("TARGET_NOTABLE", _targetNotable.CharacterObject, textObject);
				textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject CulpritDiedQuestFail
		{
			get
			{
				TextObject textObject = new TextObject("{=6zcG8eng}You tried to defend {CULPRIT.LINK} but you were overcome. {NOTABLE.LINK} took {?NOTABLE.GENDER}her{?}his{\\?} revenge. You failed to protect {CULPRIT.LINK} as promised to {QUEST_GIVER.LINK}. {?QUEST_GIVER.GENDER}she{?}he{\\?} is furious.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("CULPRIT", _culprit.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("NOTABLE", _targetNotable.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject PlayerDiedInNotableBattle
		{
			get
			{
				TextObject textObject = new TextObject("{=kG92fjCY}You fell unconscious while defending {CULPRIT.LINK}. {TARGET_NOTABLE.LINK} has taken revenge. You failed to protect {CULPRIT.LINK} as you promised {QUEST_GIVER.LINK}. {?QUEST_GIVER.GENDER}She{?}He{\\?} is furious.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("CULPRIT", _culprit.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("TARGET_NOTABLE", _targetNotable.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject FailQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=LWjIbTBi}You failed to protect {CULPRIT.LINK} as you promised {QUEST_GIVER.LINK}. {QUEST_GIVER.LINK} is furious.");
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("CULPRIT", _culprit.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject CulpritNoLongerAClanMember
		{
			get
			{
				TextObject textObject = new TextObject("{=wWrEvkuj}{CULPRIT.LINK} is no longer a member of your clan. Your agreement with {QUEST_GIVER.LINK} was terminated.");
				StringHelpers.SetCharacterProperties("CULPRIT", _culprit.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject CompanionLimitReachedQuestLogText => new TextObject("{=rkQ7D36f}The quest was canceled because your party had more companions than you could manage.");

		public FamilyFeudIssueQuest(string questId, Hero questGiver, CampaignTime duration, Settlement targetSettlement, Hero targetHero, int rewardGold)
			: base(questId, questGiver, duration, rewardGold)
		{
			_targetSettlement = targetSettlement;
			_targetNotable = targetHero;
			_culpritJoinedPlayerParty = false;
			_checkForMissionEvents = false;
			_culprit = HeroCreator.CreateSpecialHero(MBObjectManager.Instance.GetObject<CharacterObject>("townsman_" + targetSettlement.Culture.StringId), targetSettlement);
			_culprit.SetNewOccupation(Occupation.Wanderer);
			ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>("pugio");
			_culprit.CivilianEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, new EquipmentElement(item));
			_culprit.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, new EquipmentElement(item));
			_notableGangsterCharacterObject = questGiver.CurrentSettlement.MapFaction.Culture.GangleaderBodyguard;
			_rewardGold = rewardGold;
			InitializeQuestDialogs();
			SetDialogs();
			InitializeQuestOnCreation();
		}

		private void InitializeQuestDialogs()
		{
			Campaign.Current.ConversationManager.AddDialogFlow(GetCulpritDialogFlow(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetNotableThugDialogFlow(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetNotableDialogFlowBeforeTalkingToCulprit(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetNotableDialogFlowAfterTalkingToCulprit(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetNotableDialogFlowAfterKillingCulprit(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetNotableDialogFlowAfterPlayerBetrayCulprit(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetCulpritDialogFlowAfterCulpritJoin(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetNotableDialogFlowAfterNotableKnowdown(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetNotableDialogFlowAfterQuestEnd(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetCulpritDialogFlowAfterQuestEnd(), this);
		}

		protected override void InitializeQuestOnGameLoad()
		{
			SetDialogs();
			InitializeQuestDialogs();
			_notableGangsterCharacterObject = MBObjectManager.Instance.GetObject<CharacterObject>("gangster_1");
		}

		protected override void HourlyTick()
		{
		}

		private DialogFlow GetNotableDialogFlowBeforeTalkingToCulprit()
		{
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=dpTHWqwv}Are you the {?PLAYER.GENDER}woman{?}man{\\?} who thinks our blood is cheap, that we will accept silver for the life of one of our own?")).Condition(notable_culprit_is_not_near_on_condition)
				.NpcLine(new TextObject("{=Vd22iVGE}Well {?PLAYER.GENDER}lady{?}sir{\\?}, sorry to disappoint you, but our people have some self-respect."))
				.PlayerLine(new TextObject("{=a3AFjfsU}We will see. "))
				.NpcLine(new TextObject("{=AeJqCMJc}Yes, you will see. Good day to you. "))
				.CloseDialog();
		}

		private DialogFlow GetNotableDialogFlowAfterKillingCulprit()
		{
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=108Dchvt}Stop! We don't need to fight any longer. We have no quarrel with you as justice has been served.")).Condition(() => _isCulpritDiedInMissionFight && Hero.OneToOneConversationHero == _targetNotable && !_playerBetrayedCulprit)
				.NpcLine(new TextObject("{=NMrzr7Me}Now, leave peacefully..."))
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += CulpritDiedInNotableFightFail;
				})
				.CloseDialog();
		}

		private DialogFlow GetNotableDialogFlowAfterPlayerBetrayCulprit()
		{
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=4aiabOd4}I knew you are a reasonable {?PLAYER.GENDER}woman{?}man{\\?}.")).Condition(() => _isCulpritDiedInMissionFight && _playerBetrayedCulprit && Hero.OneToOneConversationHero == _targetNotable)
				.NpcLine(new TextObject("{=NMrzr7Me}Now, leave peacefully..."))
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += CulpritDiedInNotableFightFail;
				})
				.CloseDialog();
		}

		private DialogFlow GetCulpritDialogFlowAfterCulpritJoin()
		{
			TextObject textObject = new TextObject("{=56ynu2bW}Yes, {?PLAYER.GENDER}milady{?}sir{\\?}.");
			TextObject textObject2 = new TextObject("{=c452Kevh}Well I'm anxious, but I am in your hands now. I trust you will protect me {?PLAYER.GENDER}milady{?}sir{\\?}.");
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject2);
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject).Condition(() => !FightEnded && _culpritJoinedPlayerParty && Hero.OneToOneConversationHero == _culprit)
				.PlayerLine(new TextObject("{=p1ETQbzg}Just checking on you."))
				.NpcLine(textObject2)
				.CloseDialog();
		}

		private DialogFlow GetNotableDialogFlowAfterQuestEnd()
		{
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=UBFS1JLj}I have no problem with the boy anymore,[ib:closed][if:convo_annoyed] okay? Just leave me alone.")).Condition(() => FightEnded && !_persuationInDoneAndSuccessfull && Hero.OneToOneConversationHero == _targetNotable && !_playerBetrayedCulprit)
				.CloseDialog()
				.NpcLine(new TextObject("{=adbQR9j0}I got my gold, you got your boy.[if:convo_bored2] Now leave me alone..."))
				.Condition(() => FightEnded && _persuationInDoneAndSuccessfull && Hero.OneToOneConversationHero == _targetNotable && !_playerBetrayedCulprit)
				.CloseDialog();
		}

		private DialogFlow GetCulpritDialogFlowAfterQuestEnd()
		{
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=OybG76Kf}Thank you for saving me, sir.[ib:normal][if:convo_astonished] I won't forget what you did here today.")).Condition(() => FightEnded && Hero.OneToOneConversationHero == _culprit)
				.CloseDialog();
		}

		private DialogFlow GetNotableDialogFlowAfterNotableKnowdown()
		{
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=c6GbRQlg}Stop. We don’t need to fight any longer. [ib:closed][if:convo_insulted]You have made your point. We will accept the blood money."), (IAgent agent) => IsTargetNotable(agent), (IAgent agent) => IsMainAgent(agent)).Condition(multi_character_conversation_condition_after_fight)
				.Consequence(multi_character_conversation_consequence_after_fight)
				.NpcLine(new TextObject("{=pS0bBRjt}You! Go to your family and tell [if:convo_angry]them to send us the blood money."), (IAgent agent) => IsTargetNotable(agent), (IAgent agent) => IsCulprit(agent))
				.NpcLine(new TextObject("{=nxs2U0Yk}Leave now and never come back! [if:convo_furious]If we ever see you here we will kill you."), (IAgent agent) => IsTargetNotable(agent), (IAgent agent) => IsCulprit(agent))
				.NpcLine("{=udD7Y7mO}Thank you, my {?PLAYER.GENDER}lady{?}sir{\\?}, for protecting me. I will go and tell {ISSUE_GIVER.LINK} of your success here.", (IAgent agent) => IsCulprit(agent), (IAgent agent) => IsMainAgent(agent))
				.Condition(AfterNotableKnowdownEndingCondition)
				.PlayerLine(new TextObject("{=g8qb3Ame}Thank you."), (IAgent agent) => IsCulprit(agent))
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += PlayerAndCulpritKnockedDownNotableQuestSuccess;
				})
				.CloseDialog();
		}

		private bool AfterNotableKnowdownEndingCondition()
		{
			StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.QuestGiver.CharacterObject);
			return true;
		}

		private void PlayerAndCulpritKnockedDownNotableQuestSuccess()
		{
			_conversationAfterFightIsDone = true;
			HandleAgentBehaviorAfterQuestConversations();
		}

		private void HandleAgentBehaviorAfterQuestConversations()
		{
			foreach (AccompanyingCharacter item in PlayerEncounter.LocationEncounter.CharactersAccompanyingPlayer)
			{
				if (item.LocationCharacter.Character == _culprit.CharacterObject && _culpritAgent.IsActive())
				{
					item.LocationCharacter.SpecialTargetTag = "npc_common";
					item.LocationCharacter.CharacterRelation = LocationCharacter.CharacterRelations.Neutral;
					_culpritAgent.SetMortalityState(Agent.MortalityState.Invulnerable);
					_culpritAgent.SetTeam(Team.Invalid, sync: false);
					DailyBehaviorGroup behaviorGroup = _culpritAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
					behaviorGroup.AddBehavior<WalkingBehavior>();
					behaviorGroup.RemoveBehavior<FollowAgentBehavior>();
					_culpritAgent.ResetEnemyCaches();
					_culpritAgent.InvalidateTargetAgent();
					_culpritAgent.InvalidateAIWeaponSelections();
					_culpritAgent.SetWatchState(Agent.WatchState.Patrolling);
					if (_notableAgent != null)
					{
						_notableAgent.ResetEnemyCaches();
						_notableAgent.InvalidateTargetAgent();
						_notableAgent.InvalidateAIWeaponSelections();
						_notableAgent.SetWatchState(Agent.WatchState.Patrolling);
					}
					_culpritAgent.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.WithAnimationUninterruptible);
					_culpritAgent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.WithAnimationUninterruptible);
				}
			}
			Mission.Current.SetMissionMode(MissionMode.StartUp, atStart: false);
		}

		private void ApplySuccessConsequences()
		{
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, _rewardGold);
			GainRenownAction.Apply(Hero.MainHero, 1f);
			RelationshipChangeWithQuestGiver = 10;
			ChangeRelationAction.ApplyPlayerRelation(_targetNotable, -5);
			base.QuestGiver.CurrentSettlement.Village.Bound.Town.Security += 10f;
			CompleteQuestWithSuccess();
		}

		private bool multi_character_conversation_condition_after_fight()
		{
			if (!_conversationAfterFightIsDone && Hero.OneToOneConversationHero == _targetNotable)
			{
				return _isNotableKnockedDownInMissionFight;
			}
			return false;
		}

		private void multi_character_conversation_consequence_after_fight()
		{
			if (Mission.Current.GetMissionBehavior<MissionConversationLogic>() != null)
			{
				Campaign.Current.ConversationManager.AddConversationAgents(new List<Agent> { _culpritAgent }, setActionsInstantly: true);
			}
			_conversationAfterFightIsDone = true;
		}

		private DialogFlow GetNotableDialogFlowAfterTalkingToCulprit()
		{
			DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=nh7a3Nog}Well well. Who did you bring to see us? [ib:confident][if:convo_irritable]Did he bring his funeral shroud with him? I hope so. He's not leaving here alive."), (IAgent agent) => IsTargetNotable(agent), (IAgent agent) => IsCulprit(agent)).Condition(multi_character_conversation_on_condition)
				.NpcLine(new TextObject("{=RsOmvdmU}We have come to talk! Just listen to us please![if:convo_shocked]"), (IAgent agent) => IsCulprit(agent), (IAgent agent) => IsTargetNotable(agent))
				.NpcLine("{=JUjvu4XL}I knew we'd find you eventually. Now you will face justice![if:convo_evil_smile]", (IAgent agent) => IsTargetNotable(agent), (IAgent agent) => IsCulprit(agent))
				.PlayerLine("{=UQyCoQCY}Wait! This lad is now under my protection. We have come to talk in peace..", (IAgent agent) => IsTargetNotable(agent))
				.NpcLine("{=7AiP4BwY}What there is to talk about? [if:convo_confused_annoyed]This bastard murdered one of my kinsman, and it is our right to take vengeance on him!", (IAgent agent) => IsTargetNotable(agent), (IAgent agent) => IsMainAgent(agent))
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=2iVytG2y}I am not convinced. I will protect the accused until you see reason."))
				.NpcLine(new TextObject("{=4HokUcma}You will regret pushing [if:convo_very_stern]your nose into issues that do not concern you!"))
				.NpcLine(new TextObject("{=vjOkDM6C}If you defend a murderer [ib:warrior][if:convo_furious]then you die like a murderer. Boys, kill them all!"))
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
					{
						StartFightWithNotableGang(playerBetrayedCulprit: false);
					};
				})
				.CloseDialog()
				.PlayerOption(new TextObject("{=boAcQxVV}You're breaking the law."))
				.Condition(() => _task == null || !_task.Options.All((PersuasionOptionArgs x) => x.IsBlocked))
				.GotoDialogState("start_notable_family_feud_persuasion")
				.PlayerOption(new TextObject("{=J5cQPqGQ}You are right. You are free to deliver justice as you see fit."))
				.NpcLine(new TextObject("{=aRPLW15x}Thank you. I knew you are a reasonable[ib:aggressive][if:convo_evil_smile] {?PLAYER.GENDER}woman{?}man{\\?}."))
				.NpcLine(new TextObject("{=k5R4qGtL}What? Are you just going [ib:nervous][if:convo_nervous2]to leave me here to be killed? My kin will never forget this!"), IsCulprit, IsMainAgent)
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
					{
						_playerBetrayedCulprit = true;
						StartFightWithNotableGang(_playerBetrayedCulprit);
					};
				})
				.CloseDialog();
			AddPersuasionDialogs(dialogFlow);
			return dialogFlow;
		}

		private bool IsMainAgent(IAgent agent)
		{
			return agent == Mission.Current.MainAgent;
		}

		private bool IsTargetNotable(IAgent agent)
		{
			return agent.Character == _targetNotable.CharacterObject;
		}

		private bool IsCulprit(IAgent agent)
		{
			return agent.Character == _culprit.CharacterObject;
		}

		private bool notable_culprit_is_not_near_on_condition()
		{
			if (Hero.OneToOneConversationHero != _targetNotable || Mission.Current == null || FightEnded)
			{
				return false;
			}
			return Mission.Current.GetNearbyAgents(Agent.Main.Position.AsVec2, 10f, new MBList<Agent>()).All((Agent a) => a.Character != _culprit.CharacterObject);
		}

		private bool multi_character_conversation_on_condition()
		{
			if (Hero.OneToOneConversationHero != _targetNotable || Mission.Current == null || FightEnded)
			{
				return false;
			}
			MBList<Agent> nearbyAgents = Mission.Current.GetNearbyAgents(Agent.Main.Position.AsVec2, 10f, new MBList<Agent>());
			if (nearbyAgents.IsEmpty() || nearbyAgents.All((Agent a) => a.Character != _culprit.CharacterObject))
			{
				return false;
			}
			foreach (Agent item in nearbyAgents)
			{
				if (item.Character == _culprit.CharacterObject)
				{
					_culpritAgent = item;
					if (Mission.Current.GetMissionBehavior<MissionConversationLogic>() != null)
					{
						Campaign.Current.ConversationManager.AddConversationAgents(new List<Agent> { _culpritAgent }, setActionsInstantly: true);
					}
					break;
				}
			}
			return true;
		}

		private void AddPersuasionDialogs(DialogFlow dialog)
		{
			dialog.AddDialogLine("family_feud_notable_persuasion_check_accepted", "start_notable_family_feud_persuasion", "family_feud_notable_persuasion_start_reservation", "{=6P1ruzsC}Maybe...", null, persuasion_start_with_notable_on_consequence, this);
			dialog.AddDialogLine("family_feud_notable_persuasion_failed", "family_feud_notable_persuasion_start_reservation", "persuation_failed", "{=!}{FAILED_PERSUASION_LINE}", persuasion_failed_with_family_feud_notable_on_condition, persuasion_failed_with_notable_on_consequence, this);
			dialog.AddDialogLine("family_feud_notable_persuasion_rejected", "persuation_failed", "close_window", "{=vjOkDM6C}If you defend a murderer [ib:warrior][if:convo_furious]then you die like a murderer. Boys, kill them all!", null, persuasion_failed_with_notable_start_fight_on_consequence, this);
			dialog.AddDialogLine("family_feud_notable_persuasion_attempt", "family_feud_notable_persuasion_start_reservation", "family_feud_notable_persuasion_select_option", "{CONTINUE_PERSUASION_LINE}", () => !persuasion_failed_with_family_feud_notable_on_condition(), null, this);
			dialog.AddDialogLine("family_feud_notable_persuasion_success", "family_feud_notable_persuasion_start_reservation", "close_window", "{=qIQbIjVS}All right! I spare the boy's life. Now get out of my sight[ib:closed][if:convo_nonchalant]", ConversationManager.GetPersuasionProgressSatisfied, persuasion_complete_with_notable_on_consequence, this, int.MaxValue);
			ConversationSentence.OnConditionDelegate conditionDelegate = persuasion_select_option_1_on_condition;
			ConversationSentence.OnConsequenceDelegate consequenceDelegate = persuasion_select_option_1_on_consequence;
			ConversationSentence.OnPersuasionOptionDelegate persuasionOptionDelegate = persuasion_setup_option_1;
			ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = persuasion_clickable_option_1_on_condition;
			dialog.AddPlayerLine("family_feud_notable_persuasion_select_option_1", "family_feud_notable_persuasion_select_option", "family_feud_notable_persuasion_selected_option_response", "{=!}{FAMILY_FEUD_PERSUADE_ATTEMPT_1}", conditionDelegate, consequenceDelegate, this, 100, clickableConditionDelegate, persuasionOptionDelegate);
			ConversationSentence.OnConditionDelegate conditionDelegate2 = persuasion_select_option_2_on_condition;
			ConversationSentence.OnConsequenceDelegate consequenceDelegate2 = persuasion_select_option_2_on_consequence;
			persuasionOptionDelegate = persuasion_setup_option_2;
			clickableConditionDelegate = persuasion_clickable_option_2_on_condition;
			dialog.AddPlayerLine("family_feud_notable_persuasion_select_option_2", "family_feud_notable_persuasion_select_option", "family_feud_notable_persuasion_selected_option_response", "{=!}{FAMILY_FEUD_PERSUADE_ATTEMPT_2}", conditionDelegate2, consequenceDelegate2, this, 100, clickableConditionDelegate, persuasionOptionDelegate);
			ConversationSentence.OnConditionDelegate conditionDelegate3 = persuasion_select_option_3_on_condition;
			ConversationSentence.OnConsequenceDelegate consequenceDelegate3 = persuasion_select_option_3_on_consequence;
			persuasionOptionDelegate = persuasion_setup_option_3;
			clickableConditionDelegate = persuasion_clickable_option_3_on_condition;
			dialog.AddPlayerLine("family_feud_notable_persuasion_select_option_3", "family_feud_notable_persuasion_select_option", "family_feud_notable_persuasion_selected_option_response", "{=!}{FAMILY_FEUD_PERSUADE_ATTEMPT_3}", conditionDelegate3, consequenceDelegate3, this, 100, clickableConditionDelegate, persuasionOptionDelegate);
			dialog.AddDialogLine("family_feud_notable_persuasion_select_option_reaction", "family_feud_notable_persuasion_selected_option_response", "family_feud_notable_persuasion_start_reservation", "{=D0xDRqvm}{PERSUASION_REACTION}", persuasion_selected_option_response_on_condition, persuasion_selected_option_response_on_consequence, this);
		}

		private void persuasion_complete_with_notable_on_consequence()
		{
			ConversationManager.EndPersuasion();
			_persuationInDoneAndSuccessfull = true;
			HandleAgentBehaviorAfterQuestConversations();
		}

		private void persuasion_failed_with_notable_on_consequence()
		{
			ConversationManager.EndPersuasion();
		}

		private void persuasion_failed_with_notable_start_fight_on_consequence()
		{
			Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
			{
				StartFightWithNotableGang(playerBetrayedCulprit: false);
			};
		}

		private bool persuasion_failed_with_family_feud_notable_on_condition()
		{
			MBTextManager.SetTextVariable("CONTINUE_PERSUASION_LINE", "{=7B7BhVhV}Let's see what you will come up with...[if:convo_confused_annoyed]");
			if (_task.Options.Any((PersuasionOptionArgs x) => x.IsBlocked))
			{
				MBTextManager.SetTextVariable("CONTINUE_PERSUASION_LINE", "{=wvbiyZfp}What else do you have to say?[if:convo_confused_annoyed]");
			}
			if (_task.Options.All((PersuasionOptionArgs x) => x.IsBlocked) && !ConversationManager.GetPersuasionProgressSatisfied())
			{
				MBTextManager.SetTextVariable("FAILED_PERSUASION_LINE", _task.FinalFailLine);
				return true;
			}
			return false;
		}

		private void persuasion_selected_option_response_on_consequence()
		{
			Tuple<PersuasionOptionArgs, PersuasionOptionResult> tuple = ConversationManager.GetPersuasionChosenOptions().Last();
			float difficulty = Campaign.Current.Models.PersuasionModel.GetDifficulty(PersuasionDifficulty.MediumHard);
			Campaign.Current.Models.PersuasionModel.GetEffectChances(tuple.Item1, out var moveToNextStageChance, out var blockRandomOptionChance, difficulty);
			_task.ApplyEffects(moveToNextStageChance, blockRandomOptionChance);
		}

		private bool persuasion_selected_option_response_on_condition()
		{
			PersuasionOptionResult item = ConversationManager.GetPersuasionChosenOptions().Last().Item2;
			MBTextManager.SetTextVariable("PERSUASION_REACTION", PersuasionHelper.GetDefaultPersuasionOptionReaction(item));
			return true;
		}

		private void persuasion_start_with_notable_on_consequence()
		{
			_task = GetPersuasionTask();
			ConversationManager.StartPersuasion(2f, 1f, 0f, 2f, 2f, 0f, PersuasionDifficulty.MediumHard);
		}

		private bool persuasion_select_option_1_on_condition()
		{
			if (_task.Options.Count > 0)
			{
				TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
				textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(_task.Options.ElementAt(0), showToPlayer: false));
				textObject.SetTextVariable("PERSUASION_OPTION_LINE", _task.Options.ElementAt(0).Line);
				MBTextManager.SetTextVariable("FAMILY_FEUD_PERSUADE_ATTEMPT_1", textObject);
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
				MBTextManager.SetTextVariable("FAMILY_FEUD_PERSUADE_ATTEMPT_2", textObject);
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
				MBTextManager.SetTextVariable("FAMILY_FEUD_PERSUADE_ATTEMPT_3", textObject);
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

		private bool persuasion_clickable_option_1_on_condition(out TextObject hintText)
		{
			hintText = new TextObject("{=9ACJsI6S}Blocked");
			if (_task.Options.Any())
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

		private PersuasionTask GetPersuasionTask()
		{
			PersuasionTask persuasionTask = new PersuasionTask(0);
			persuasionTask.FinalFailLine = new TextObject("{=rzGqa5oD}Revenge will be taken. Save your breath for the fight...");
			persuasionTask.TryLaterLine = new TextObject("{=!}IF YOU SEE THIS. CALL CAMPAIGN TEAM.");
			persuasionTask.SpokenLine = new TextObject("{=6P1ruzsC}Maybe...");
			PersuasionOptionArgs option = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.Easy, givesCriticalSuccess: false, new TextObject("{=K9i5SaDc}Blood money is appropriate for a crime of passion. But you kill this boy in cold blood, you will be a real murderer in the eyes of the law, and will no doubt die."));
			persuasionTask.AddOptionToTask(option);
			PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Valor, TraitEffect.Positive, PersuasionArgumentStrength.ExtremelyHard, givesCriticalSuccess: true, new TextObject("{=FUL8TcYa}I promised to protect the boy at the cost of my life. If you try to harm him, you will bleed for it."), null, canBlockOtherOption: true);
			persuasionTask.AddOptionToTask(option2);
			PersuasionOptionArgs option3 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Mercy, TraitEffect.Positive, PersuasionArgumentStrength.Normal, givesCriticalSuccess: false, new TextObject("{=Ytws5O9S}Some day you may wish to save the life of one of your sons through blood money. If you refuse mercy, mercy may be refused you."));
			persuasionTask.AddOptionToTask(option3);
			return persuasionTask;
		}

		private void StartFightWithNotableGang(bool playerBetrayedCulprit)
		{
			_notableAgent = (Agent)MissionConversationLogic.Current.ConversationManager.ConversationAgents[0];
			List<Agent> list = new List<Agent> { _culpritAgent };
			List<Agent> list2 = new List<Agent> { _notableAgent };
			MBList<Agent> agents = new MBList<Agent>();
			foreach (Agent nearbyAgent in Mission.Current.GetNearbyAgents(Agent.Main.Position.AsVec2, 30f, agents))
			{
				if ((CharacterObject)nearbyAgent.Character == _notableGangsterCharacterObject)
				{
					list2.Add(nearbyAgent);
				}
			}
			if (playerBetrayedCulprit)
			{
				Agent.Main.SetTeam(Mission.Current.SpectatorTeam, sync: false);
			}
			else
			{
				list.Add(Agent.Main);
				foreach (Agent item in list2)
				{
					item.Defensiveness = 2f;
				}
				_culpritAgent.Health = 350f;
				_culpritAgent.BaseHealthLimit = 350f;
				_culpritAgent.HealthLimit = 350f;
			}
			_notableAgent.Health = 350f;
			_notableAgent.BaseHealthLimit = 350f;
			_notableAgent.HealthLimit = 350f;
			Mission.Current.GetMissionBehavior<MissionFightHandler>().StartCustomFight(list, list2, dropWeapons: false, isItemUseDisabled: false, delegate
			{
				if (_isNotableKnockedDownInMissionFight)
				{
					if (Agent.Main != null && _notableAgent.Position.DistanceSquared(Agent.Main.Position) < 49f)
					{
						MissionConversationLogic.Current.StartConversation(_notableAgent, setActionsInstantly: false);
					}
					else
					{
						PlayerAndCulpritKnockedDownNotableQuestSuccess();
					}
				}
				else if (Agent.Main != null && _notableAgent.Position.DistanceSquared(Agent.Main.Position) < 49f)
				{
					MissionConversationLogic.Current.StartConversation(_notableAgent, setActionsInstantly: false);
				}
				else
				{
					CulpritDiedInNotableFightFail();
				}
			});
		}

		private void OnAgentHit(Agent affectedAgent, Agent affectorAgent, int damage)
		{
			if (base.IsOngoing && !_persuationInDoneAndSuccessfull && affectedAgent.Health <= (float)damage && Agent.Main != null)
			{
				if (affectedAgent == _notableAgent && !_isNotableKnockedDownInMissionFight)
				{
					affectedAgent.Health = 50f;
					_isNotableKnockedDownInMissionFight = true;
					Mission.Current.GetMissionBehavior<MissionFightHandler>().EndFight();
				}
				if (affectedAgent == _culpritAgent && !_isCulpritDiedInMissionFight)
				{
					Blow b = new Blow
					{
						DamageCalculated = true,
						BaseMagnitude = damage,
						InflictedDamage = damage,
						DamagedPercentage = 1f,
						OwnerId = (affectorAgent?.Index ?? (-1))
					};
					affectedAgent.Die(b);
					_isCulpritDiedInMissionFight = true;
				}
			}
		}

		protected override void SetDialogs()
		{
			OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start").NpcLine(new TextObject("{=JjXETjYb}Thank you.[ib:demure][if:convo_thinking] I have to add, I'm ready to pay you {REWARD_GOLD}{GOLD_ICON} denars for your trouble. He is hiding somewhere nearby. Go talk to him, and tell him that you're here to sort things out.")).Condition(delegate
			{
				MBTextManager.SetTextVariable("REWARD_GOLD", _rewardGold);
				MBTextManager.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return Hero.OneToOneConversationHero == base.QuestGiver;
			})
				.Consequence(QuestAcceptedConsequences)
				.CloseDialog();
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(new TextObject("{=ndDpjT8s}Have you been able to talk with my boy yet?[if:convo_innocent_smile]")).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=ETiAbgHa}I will talk with them right away"))
				.NpcLine(new TextObject("{=qmqTLZ9R}Thank you {?PLAYER.GENDER}madam{?}sir{\\?}. You are a savior."))
				.CloseDialog()
				.PlayerOption(new TextObject("{=18NtjryL}Not yet, but I will soon."))
				.NpcLine(new TextObject("{=HeIIW3EH}We are waiting for your good news {?PLAYER.GENDER}milady{?}sir{\\?}."))
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog();
		}

		private void QuestAcceptedConsequences()
		{
			StartQuest();
			AddLog(PlayerStartsQuestLogText1);
			AddLog(PlayerStartsQuestLogText2);
			AddTrackedObject(_targetNotable);
			AddTrackedObject(_culprit);
			Location locationWithId = Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("village_center");
			Settlement.CurrentSettlement.LocationComplex.ChangeLocation(CreateCulpritLocationCharacter(Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral), null, locationWithId);
		}

		private DialogFlow GetCulpritDialogFlow()
		{
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=w0HPC53e}Who are you? What do you want from me?[ib:nervous][if:convo_bared_teeth]")).Condition(() => !_culpritJoinedPlayerParty && Hero.OneToOneConversationHero == _culprit)
				.PlayerLine(new TextObject("{=UGTCe2qP}Relax. I've talked with your relative, {QUEST_GIVER.NAME}. I know all about your situation. I'm here to help."))
				.Condition(delegate
				{
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
					return Hero.OneToOneConversationHero == _culprit;
				})
				.Consequence(delegate
				{
					_culprit.SetHasMet();
				})
				.NpcLine(new TextObject("{=45llLiYG}How will you help? Will you protect me?[ib:normal][if:convo_astonished]"))
				.PlayerLine(new TextObject("{=4mwSvCgG}Yes I will. Come now, I will take you with me to {TARGET_NOTABLE.NAME} to resolve this issue peacefully."))
				.Condition(delegate
				{
					StringHelpers.SetCharacterProperties("TARGET_NOTABLE", _targetNotable.CharacterObject);
					return Hero.OneToOneConversationHero == _culprit;
				})
				.NpcLine(new TextObject("{=bHRZhYzd}No! I won't go anywhere near them! They'll kill me![ib:closed2][if:convo_stern]"))
				.PlayerLine(new TextObject("{=sakSp6H8}You can't hide in the shadows forever. I pledge on my honor to protect you if things turn ugly."))
				.NpcLine(new TextObject("{=4CFOH0kB}I'm still not sure about all this, but I suppose you're right that I don't have much choice. Let's go get this over.[ib:closed][if:convo_pondering]"))
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += CulpritJoinedPlayersArmy;
				})
				.CloseDialog();
		}

		private DialogFlow GetNotableThugDialogFlow()
		{
			TextObject textObject = new TextObject("{=QMaYa25R}If you dare to even breathe a word against {TARGET_NOTABLE.LINK},[ib:aggressive2][if:convo_furious] it will be your last. You got it scum?");
			StringHelpers.SetCharacterProperties("TARGET_NOTABLE", _targetNotable.CharacterObject, textObject);
			TextObject textObject2 = new TextObject("{=vGnY4KBO}I care very little for your threats. My business is with {TARGET_NOTABLE.LINK}.");
			StringHelpers.SetCharacterProperties("TARGET_NOTABLE", _targetNotable.CharacterObject, textObject2);
			return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject).Condition(() => _notableThugs != null && _notableThugs.Exists((LocationCharacter x) => x.AgentOrigin == Campaign.Current.ConversationManager.ConversationAgents[0].Origin))
				.PlayerLine(textObject2)
				.CloseDialog();
		}

		private void CulpritJoinedPlayersArmy()
		{
			_culprit.ChangeState(Hero.CharacterStates.Active);
			AddCompanionAction.Apply(Clan.PlayerClan, _culprit);
			AddHeroToPartyAction.Apply(_culprit, MobileParty.MainParty);
			AddLog(CulpritJoinedPlayerPartyLogText);
			if (Mission.Current != null)
			{
				DailyBehaviorGroup behaviorGroup = ((Agent)MissionConversationLogic.Current.ConversationManager.ConversationAgents[0]).GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
				FollowAgentBehavior followAgentBehavior = behaviorGroup.AddBehavior<FollowAgentBehavior>();
				behaviorGroup.SetScriptedBehavior<FollowAgentBehavior>();
				followAgentBehavior.SetTargetAgent(Agent.Main);
			}
			_culpritJoinedPlayerParty = true;
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
			CampaignEvents.VillageBeingRaided.AddNonSerializedListener(this, OnVillageRaid);
			CampaignEvents.BeforeMissionOpenedEvent.AddNonSerializedListener(this, OnBeforeMissionOpened);
			CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
			CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnd);
			CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
			CampaignEvents.CompanionRemoved.AddNonSerializedListener(this, OnCompanionRemoved);
			CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnPrisonerTaken);
			CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
			CampaignEvents.CanMoveToSettlementEvent.AddNonSerializedListener(this, CanMoveToSettlement);
			CampaignEvents.CanHeroDieEvent.AddNonSerializedListener(this, CanHeroDie);
			CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, LocationCharactersAreReadyToSpawn);
			CampaignEvents.PerkResetEvent.AddNonSerializedListener(this, OnPerksReset);
			CampaignEvents.NewCompanionAdded.AddNonSerializedListener(this, OnNewCompanionAdded);
			CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
		}

		private void OnGameLoadFinished()
		{
			CheckCompanionLimit();
		}

		private void OnNewCompanionAdded(Hero hero)
		{
			CheckCompanionLimit();
		}

		private void OnPerksReset(Hero hero, PerkObject perk)
		{
			if (hero == Hero.MainHero)
			{
				CheckCompanionLimit();
			}
		}

		private void CheckCompanionLimit()
		{
			if (Clan.PlayerClan.Companions.Count > Clan.PlayerClan.CompanionLimit)
			{
				AddLog(CompanionLimitReachedQuestLogText);
				CompleteQuestWithCancel();
			}
		}

		private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
		{
			if (!_culpritJoinedPlayerParty && Settlement.CurrentSettlement == base.QuestGiver.CurrentSettlement)
			{
				Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("village_center").AddLocationCharacters(CreateCulpritLocationCharacter, Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
			}
		}

		private void CanMoveToSettlement(Hero hero, ref bool result)
		{
			CommonRestrictionInfoIsRequested(hero, ref result);
		}

		public override void OnHeroCanBeSelectedInInventoryInfoIsRequested(Hero hero, ref bool result)
		{
			CommonRestrictionInfoIsRequested(hero, ref result);
		}

		public override void OnHeroCanHavePartyRoleOrBeGovernorInfoIsRequested(Hero hero, ref bool result)
		{
			CommonRestrictionInfoIsRequested(hero, ref result);
		}

		public override void OnHeroCanLeadPartyInfoIsRequested(Hero hero, ref bool result)
		{
			CommonRestrictionInfoIsRequested(hero, ref result);
		}

		public override void OnHeroCanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
		{
			CommonRestrictionInfoIsRequested(hero, ref result);
		}

		private void CommonRestrictionInfoIsRequested(Hero hero, ref bool result)
		{
			if (hero == _culprit || _targetNotable == hero)
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

		private void CanHeroDie(Hero hero, KillCharacterAction.KillCharacterActionDetail causeOfDeath, ref bool result)
		{
			if (hero == _targetNotable)
			{
				result = false;
			}
			else if (hero == Hero.MainHero && Settlement.CurrentSettlement == _targetSettlement && Mission.Current != null)
			{
				result = false;
			}
		}

		private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
		{
			if (victim == _targetNotable)
			{
				TextObject textObject = ((detail == KillCharacterAction.KillCharacterActionDetail.Lost) ? TargetHeroDisappearedLogText : TargetHeroDiedLogText);
				StringHelpers.SetCharacterProperties("QUEST_TARGET", _targetNotable.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject);
				AddLog(textObject);
				CompleteQuestWithCancel();
			}
		}

		private void OnPrisonerTaken(PartyBase capturer, Hero prisoner)
		{
			if (prisoner == _culprit)
			{
				AddLog(FailQuestLogText);
				TiemoutFailConsequences();
				CompleteQuestWithFail();
			}
		}

		private void OnVillageRaid(Village village)
		{
			if (village == _targetSettlement.Village)
			{
				AddLog(TargetVillageRaidedBeforeTalkingToCulpritCancel);
				CompleteQuestWithCancel();
			}
			else if (village == base.QuestGiver.CurrentSettlement.Village && !_culpritJoinedPlayerParty)
			{
				AddLog(QuestGiverVillageRaidedBeforeTalkingToCulpritCancel);
				CompleteQuestWithCancel();
			}
		}

		private void OnCompanionRemoved(Hero companion, RemoveCompanionAction.RemoveCompanionDetail detail)
		{
			if (base.IsOngoing && !_isCulpritDiedInMissionFight && !_isPlayerKnockedOutMissionFight && companion == _culprit)
			{
				AddLog(CulpritNoLongerAClanMember);
				TiemoutFailConsequences();
				CompleteQuestWithFail();
			}
		}

		public void OnMissionStarted(IMission iMission)
		{
			if (!_checkForMissionEvents)
			{
				return;
			}
			if (PlayerEncounter.LocationEncounter.CharactersAccompanyingPlayer.All((AccompanyingCharacter x) => x.LocationCharacter.Character != _culprit.CharacterObject))
			{
				LocationCharacter locationCharacterOfHero = LocationComplex.Current.GetLocationCharacterOfHero(_culprit);
				if (locationCharacterOfHero != null)
				{
					PlayerEncounter.LocationEncounter.AddAccompanyingCharacter(locationCharacterOfHero, isFollowing: true);
				}
			}
			FamilyFeudIssueMissionBehavior missionBehavior = new FamilyFeudIssueMissionBehavior(OnAgentHit);
			Mission.Current.AddMissionBehavior(missionBehavior);
			Mission.Current.GetMissionBehavior<MissionConversationLogic>().SetSpawnArea("alley_2");
		}

		private void OnMissionEnd(IMission mission)
		{
			if (!_checkForMissionEvents)
			{
				return;
			}
			_notableAgent = null;
			_culpritAgent = null;
			if (Agent.Main == null)
			{
				AddLog(PlayerDiedInNotableBattle);
				RelationshipChangeWithQuestGiver = -10;
				base.QuestGiver.CurrentSettlement.Village.Bound.Town.Prosperity -= 5f;
				base.QuestGiver.CurrentSettlement.Village.Bound.Town.Security -= 5f;
				_isPlayerKnockedOutMissionFight = true;
				CompleteQuestWithFail();
			}
			else if (_isCulpritDiedInMissionFight)
			{
				if (_playerBetrayedCulprit)
				{
					AddLog(FailQuestLogText);
					TraitLevelingHelper.OnIssueSolvedThroughBetrayal(Hero.MainHero, new Tuple<TraitObject, int>[1]
					{
						new Tuple<TraitObject, int>(DefaultTraits.Honor, -50)
					});
					ChangeRelationAction.ApplyPlayerRelation(_targetNotable, 5);
				}
				else
				{
					AddLog(CulpritDiedQuestFail);
				}
				RelationshipChangeWithQuestGiver = -10;
				base.QuestGiver.CurrentSettlement.Village.Bound.Town.Prosperity -= 5f;
				base.QuestGiver.CurrentSettlement.Village.Bound.Town.Security -= 5f;
				CompleteQuestWithFail();
			}
			else if (_persuationInDoneAndSuccessfull)
			{
				AddLog(SuccessQuestSolutionLogText);
				ApplySuccessConsequences();
			}
			else if (_isNotableKnockedDownInMissionFight)
			{
				AddLog(SuccessQuestSolutionLogText);
				ApplySuccessConsequences();
			}
		}

		private void OnGameMenuOpened(MenuCallbackArgs args)
		{
			if (_culpritJoinedPlayerParty && Hero.MainHero.CurrentSettlement == _targetSettlement)
			{
				_checkForMissionEvents = args.MenuContext.GameMenu.StringId == "village";
			}
		}

		public void OnSettlementLeft(MobileParty party, Settlement settlement)
		{
			if (party == MobileParty.MainParty)
			{
				if (settlement == _targetSettlement)
				{
					_checkForMissionEvents = false;
				}
				if (settlement == base.QuestGiver.CurrentSettlement && _culpritJoinedPlayerParty && !IsTracked(_targetSettlement))
				{
					AddTrackedObject(_targetSettlement);
				}
			}
		}

		public void OnBeforeMissionOpened()
		{
			if (!_checkForMissionEvents)
			{
				return;
			}
			Location locationWithId = Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("village_center");
			if (locationWithId != null)
			{
				locationWithId.GetLocationCharacter(_targetNotable).SpecialTargetTag = "alley_2";
				if (_notableThugs == null)
				{
					_notableThugs = new List<LocationCharacter>();
				}
				else
				{
					_notableThugs.Clear();
				}
				locationWithId.AddLocationCharacters(CreateNotablesThugs, Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, TaleWorlds.Library.MathF.Ceiling(Campaign.Current.Models.IssueModel.GetIssueDifficultyMultiplier() * 3f));
			}
		}

		private LocationCharacter CreateCulpritLocationCharacter(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(_culprit.CharacterObject.Race, "_settlement");
			Tuple<string, Monster> tuple = new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, _culprit.CharacterObject.IsFemale, "_villager"), monsterWithSuffix);
			return new LocationCharacter(new AgentData(new SimpleAgentOrigin(_culprit.CharacterObject)).Monster(tuple.Item2), SandBoxManager.Instance.AgentBehaviorManager.AddFirstCompanionBehavior, "alley_2", fixedLocation: true, relation, tuple.Item1, useCivilianEquipment: true);
		}

		private LocationCharacter CreateNotablesThugs(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(_notableGangsterCharacterObject.Race, "_settlement");
			Tuple<string, Monster> tuple = new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, _notableGangsterCharacterObject.IsFemale, "_villain"), monsterWithSuffix);
			LocationCharacter locationCharacter = new LocationCharacter(new AgentData(new SimpleAgentOrigin(_notableGangsterCharacterObject)).Monster(tuple.Item2), SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors, "alley_2", fixedLocation: true, relation, tuple.Item1, useCivilianEquipment: true);
			_notableThugs.Add(locationCharacter);
			return locationCharacter;
		}

		private void OnMapEventEnded(MapEvent mapEvent)
		{
			if (mapEvent.IsPlayerMapEvent && _culpritJoinedPlayerParty && !MobileParty.MainParty.MemberRoster.GetTroopRoster().Exists((TroopRosterElement x) => x.Character == _culprit.CharacterObject))
			{
				AddLog(FailQuestLogText);
				TiemoutFailConsequences();
				CompleteQuestWithFail();
			}
		}

		private void CulpritDiedInNotableFightFail()
		{
			_conversationAfterFightIsDone = true;
			HandleAgentBehaviorAfterQuestConversations();
		}

		protected override void OnFinalize()
		{
			if (_culprit.IsPlayerCompanion)
			{
				if (_culprit.IsPrisoner)
				{
					EndCaptivityAction.ApplyByEscape(_culprit, null, showNotification: false);
				}
				RemoveCompanionAction.ApplyAfterQuest(Clan.PlayerClan, _culprit);
			}
			if (_culprit.IsAlive)
			{
				_culprit.Clan = null;
				KillCharacterAction.ApplyByRemove(_culprit);
			}
		}

		protected override void OnTimedOut()
		{
			AddLog(FailQuestLogText);
			TiemoutFailConsequences();
		}

		private void TiemoutFailConsequences()
		{
			TraitLevelingHelper.OnIssueSolvedThroughBetrayal(base.QuestGiver, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -50)
			});
			RelationshipChangeWithQuestGiver = -10;
			base.QuestGiver.CurrentSettlement.Village.Bound.Town.Prosperity -= 5f;
			base.QuestGiver.CurrentSettlement.Village.Bound.Town.Security -= 5f;
		}

		internal static void AutoGeneratedStaticCollectObjectsFamilyFeudIssueQuest(object o, List<object> collectedObjects)
		{
			((FamilyFeudIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_targetSettlement);
			collectedObjects.Add(_targetNotable);
			collectedObjects.Add(_culprit);
		}

		internal static object AutoGeneratedGetMemberValue_targetSettlement(object o)
		{
			return ((FamilyFeudIssueQuest)o)._targetSettlement;
		}

		internal static object AutoGeneratedGetMemberValue_targetNotable(object o)
		{
			return ((FamilyFeudIssueQuest)o)._targetNotable;
		}

		internal static object AutoGeneratedGetMemberValue_culprit(object o)
		{
			return ((FamilyFeudIssueQuest)o)._culprit;
		}

		internal static object AutoGeneratedGetMemberValue_culpritJoinedPlayerParty(object o)
		{
			return ((FamilyFeudIssueQuest)o)._culpritJoinedPlayerParty;
		}

		internal static object AutoGeneratedGetMemberValue_checkForMissionEvents(object o)
		{
			return ((FamilyFeudIssueQuest)o)._checkForMissionEvents;
		}

		internal static object AutoGeneratedGetMemberValue_rewardGold(object o)
		{
			return ((FamilyFeudIssueQuest)o)._rewardGold;
		}
	}

	private const IssueBase.IssueFrequency FamilyFeudIssueFrequency = IssueBase.IssueFrequency.Rare;

	public override void RegisterEvents()
	{
		CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, OnCheckForIssue);
	}

	public void OnCheckForIssue(Hero hero)
	{
		if (ConditionsHold(hero, out var otherVillage, out var otherNotable))
		{
			KeyValuePair<Hero, Settlement> keyValuePair = new KeyValuePair<Hero, Settlement>(otherNotable, otherVillage);
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(OnStartIssue, typeof(FamilyFeudIssue), IssueBase.IssueFrequency.Rare, keyValuePair));
		}
		else
		{
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(FamilyFeudIssue), IssueBase.IssueFrequency.Rare));
		}
	}

	private bool ConditionsHold(Hero issueGiver, out Settlement otherVillage, out Hero otherNotable)
	{
		otherVillage = null;
		otherNotable = null;
		if (!issueGiver.IsNotable)
		{
			return false;
		}
		if (issueGiver.IsRuralNotable && issueGiver.CurrentSettlement.IsVillage)
		{
			Settlement bound = issueGiver.CurrentSettlement.Village.Bound;
			if (bound.IsTown)
			{
				foreach (Village item in bound.BoundVillages.WhereQ((Village x) => x != issueGiver.CurrentSettlement.Village))
				{
					Hero hero = item.Settlement.Notables.FirstOrDefaultQ((Hero y) => y.IsRuralNotable && y.CanHaveCampaignIssues() && y.GetTraitLevel(DefaultTraits.Mercy) <= 0);
					if (hero != null)
					{
						otherVillage = item.Settlement;
						otherNotable = hero;
					}
				}
				return otherVillage != null;
			}
		}
		return false;
	}

	private IssueBase OnStartIssue(in PotentialIssueData pid, Hero issueOwner)
	{
		KeyValuePair<Hero, Settlement> keyValuePair = (KeyValuePair<Hero, Settlement>)pid.RelatedObject;
		return new FamilyFeudIssue(issueOwner, keyValuePair.Key, keyValuePair.Value);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
