using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues;

public abstract class IssueBase : MBObjectBase
{
	internal enum IssueState
	{
		Ongoing,
		SolvingWithQuestSolution,
		SolvingWithAlternativeSolution,
		SolvingWithLordSolution
	}

	[Flags]
	public enum AlternativeSolutionScaleFlag : uint
	{
		None = 0u,
		Duration = 1u,
		RequiredTroops = 2u,
		Casualties = 4u,
		FailureRisk = 8u
	}

	[Flags]
	protected enum PreconditionFlags : uint
	{
		None = 0u,
		Relation = 1u,
		Skill = 2u,
		Money = 4u,
		Renown = 8u,
		Influence = 0x10u,
		Wounded = 0x20u,
		AtWar = 0x40u,
		ClanTier = 0x80u,
		NotEnoughTroops = 0x100u,
		NotInSameFaction = 0x200u,
		PartySizeLimit = 0x400u,
		ClanIsMercenary = 0x800u,
		MainHeroIsKingdomLeader = 0x4000u,
		PlayerIsOwnerOfSettlement = 0x8000u,
		CompanionLimitReached = 0x10000u
	}

	public enum IssueUpdateDetails
	{
		None,
		PlayerStartedIssueQuestClassicSolution,
		PlayerSentTroopsToQuest,
		SentTroopsFinishedQuest,
		SentTroopsFailedQuest,
		IssueFinishedWithSuccess,
		IssueFinishedWithBetrayal,
		IssueFinishedByAILord,
		IssueFail,
		IssueCancel,
		IssueTimedOut
	}

	public enum IssueFrequency
	{
		VeryCommon,
		Common,
		Rare
	}

	public const int IssueRelatedConversationPriority = 125;

	[SaveableField(27)]
	private float _totalTroopXpAmount;

	[SaveableField(30)]
	public readonly TroopRoster AlternativeSolutionSentTroops;

	[SaveableField(35)]
	private SkillObject _companionRewardSkill;

	[SaveableField(14)]
	private readonly MBList<JournalLog> _journalEntries;

	[SaveableField(11)]
	private IssueState _issueState;

	[SaveableField(12)]
	public CampaignTime IssueDueTime;

	[SaveableField(16)]
	public CampaignTime IssueCreationTime;

	[SaveableField(13)]
	private Hero _issueOwner;

	[SaveableField(26)]
	private float _issueDifficultyMultiplier;

	[SaveableField(32)]
	private bool _areIssueEffectsResolved;

	[SaveableField(33)]
	private int _alternativeSolutionCasualtyCount;

	[SaveableField(34)]
	private float _failureChance;

	[SaveableField(31)]
	private readonly List<ITrackableCampaignObject> _trackedObjects = new List<ITrackableCampaignObject>();

	protected virtual bool IssueQuestCanBeDuplicated => false;

	public virtual int RelationshipChangeWithIssueOwner { get; protected set; }

	public abstract TextObject IssueBriefByIssueGiver { get; }

	public abstract TextObject IssueAcceptByPlayer { get; }

	public virtual TextObject IssuePlayerResponseAfterLordExplanation => new TextObject("{=sMCN7eCp}Is there any other way to solve this problem?");

	public virtual TextObject IssuePlayerResponseAfterAlternativeExplanation => new TextObject("{=yrPEqZEa}Any other way?");

	public abstract TextObject IssueQuestSolutionExplanationByIssueGiver { get; }

	public virtual TextObject IssueAlternativeSolutionExplanationByIssueGiver => TextObject.GetEmpty();

	public virtual TextObject IssueLordSolutionExplanationByIssueGiver => TextObject.GetEmpty();

	public abstract TextObject IssueQuestSolutionAcceptByPlayer { get; }

	public virtual TextObject IssueAlternativeSolutionAcceptByPlayer => TextObject.GetEmpty();

	public virtual TextObject IssueAlternativeSolutionResponseByIssueGiver => TextObject.GetEmpty();

	public virtual TextObject IssueLordSolutionAcceptByPlayer => TextObject.GetEmpty();

	public virtual TextObject IssueLordSolutionResponseByIssueGiver => TextObject.GetEmpty();

	public virtual TextObject IssueLordSolutionCounterOfferBriefByOtherNpc => TextObject.GetEmpty();

	public virtual TextObject IssueLordSolutionCounterOfferExplanationByOtherNpc => TextObject.GetEmpty();

	public virtual TextObject IssueLordSolutionCounterOfferAcceptByPlayer => TextObject.GetEmpty();

	public virtual TextObject IssueLordSolutionCounterOfferDeclineByPlayer => TextObject.GetEmpty();

	public virtual TextObject IssueLordSolutionCounterOfferAcceptResponseByOtherNpc => TextObject.GetEmpty();

	public virtual TextObject IssueLordSolutionCounterOfferDeclineResponseByOtherNpc => TextObject.GetEmpty();

	public virtual TextObject IssueAsRumorInSettlement => TextObject.GetEmpty();

	public virtual int AlternativeSolutionBaseNeededMenCount { get; }

	protected virtual int AlternativeSolutionBaseDurationInDaysInternal { get; }

	[SaveableProperty(25)]
	public CampaignTime AlternativeSolutionReturnTimeForTroops { get; private set; }

	public abstract bool IsThereAlternativeSolution { get; }

	protected virtual TextObject AlternativeSolutionStartLog { get; }

	protected virtual TextObject AlternativeSolutionEndLogDefault => new TextObject("{=xbvQzR2B}Your men should be on their way.");

	public bool IsThereDiscussDialogFlow => IssueDiscussAlternativeSolution != null;

	protected virtual int CompanionSkillRewardXP { get; }

	[SaveableProperty(31)]
	public CampaignTime AlternativeSolutionIssueEffectClearTime { get; private set; }

	public Hero AlternativeSolutionHero
	{
		get
		{
			foreach (TroopRosterElement item in AlternativeSolutionSentTroops.GetTroopRoster())
			{
				if (item.Character.IsHero)
				{
					return item.Character.HeroObject;
				}
			}
			return null;
		}
	}

	public virtual TextObject IssueDiscussAlternativeSolution { get; }

	public virtual TextObject IssueAlternativeSolutionSuccessLog { get; }

	public virtual TextObject IssueAlternativeSolutionFailLog { get; }

	public abstract bool IsThereLordSolution { get; }

	protected virtual TextObject LordSolutionStartLog => TextObject.GetEmpty();

	protected virtual TextObject LordSolutionCounterOfferAcceptLog => TextObject.GetEmpty();

	protected virtual TextObject LordSolutionCounterOfferRefuseLog => TextObject.GetEmpty();

	public virtual int NeededInfluenceForLordSolution { get; }

	public virtual Hero CounterOfferHero { get; protected set; }

	public MBReadOnlyList<JournalLog> JournalEntries => _journalEntries;

	public Hero IssueOwner
	{
		get
		{
			return _issueOwner;
		}
		set
		{
			Hero issueOwner = _issueOwner;
			_issueOwner = value;
			if (IsSolvingWithAlternative)
			{
				TextObject textObject = new TextObject("{=gmaqJZyv}You have received a message from {NEW_OWNER.LINK}:{newline}\"Sadly, {OLD_OWNER.LINK} has died. You may continue on your task, however, and report back to me.");
				StringHelpers.SetCharacterProperties("OLD_OWNER", issueOwner.CharacterObject, textObject);
				StringHelpers.SetCharacterProperties("NEW_OWNER", _issueOwner.CharacterObject, textObject);
				AddLog(new JournalLog(CampaignTime.Now, textObject));
			}
		}
	}

	public abstract TextObject Title { get; }

	[SaveableProperty(15)]
	public QuestBase IssueQuest { get; private set; }

	public Settlement IssueSettlement
	{
		get
		{
			if (!_issueOwner.IsNotable)
			{
				return null;
			}
			return IssueOwner.CurrentSettlement;
		}
	}

	public abstract TextObject Description { get; }

	[SaveableProperty(22)]
	public bool IsTriedToSolveBefore { get; private set; }

	public bool IsOngoingWithoutQuest => _issueState == IssueState.Ongoing;

	public bool IsSolvingWithQuest => _issueState == IssueState.SolvingWithQuestSolution;

	public bool IsSolvingWithAlternative => _issueState == IssueState.SolvingWithAlternativeSolution;

	public bool IsSolvingWithLordSolution => _issueState == IssueState.SolvingWithLordSolution;

	protected float IssueDifficultyMultiplier
	{
		get
		{
			if (_issueDifficultyMultiplier != 0f)
			{
				return _issueDifficultyMultiplier;
			}
			return Campaign.Current.Models.IssueModel.GetIssueDifficultyMultiplier();
		}
	}

	protected virtual int RewardGold { get; }

	public virtual AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags => AlternativeSolutionScaleFlag.None;

	public bool AlternativeSolutionHasCasualties => AlternativeSolutionScaleFlags.HasAnyFlag(AlternativeSolutionScaleFlag.Casualties);

	public bool AlternativeSolutionHasScaledRequiredTroops => AlternativeSolutionScaleFlags.HasAnyFlag(AlternativeSolutionScaleFlag.RequiredTroops);

	public bool AlternativeSolutionHasScaledDuration => AlternativeSolutionScaleFlags.HasAnyFlag(AlternativeSolutionScaleFlag.Duration);

	public bool AlternativeSolutionHasFailureRisk => AlternativeSolutionScaleFlags.HasAnyFlag(AlternativeSolutionScaleFlag.FailureRisk);

	protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		collectedObjects.Add(AlternativeSolutionSentTroops);
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(IssueDueTime, collectedObjects);
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(IssueCreationTime, collectedObjects);
		collectedObjects.Add(_companionRewardSkill);
		collectedObjects.Add(_journalEntries);
		collectedObjects.Add(_issueOwner);
		collectedObjects.Add(_trackedObjects);
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(AlternativeSolutionReturnTimeForTroops, collectedObjects);
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(AlternativeSolutionIssueEffectClearTime, collectedObjects);
		collectedObjects.Add(IssueQuest);
	}

	internal static object AutoGeneratedGetMemberValueAlternativeSolutionReturnTimeForTroops(object o)
	{
		return ((IssueBase)o).AlternativeSolutionReturnTimeForTroops;
	}

	internal static object AutoGeneratedGetMemberValueAlternativeSolutionIssueEffectClearTime(object o)
	{
		return ((IssueBase)o).AlternativeSolutionIssueEffectClearTime;
	}

	internal static object AutoGeneratedGetMemberValueIssueQuest(object o)
	{
		return ((IssueBase)o).IssueQuest;
	}

	internal static object AutoGeneratedGetMemberValueIsTriedToSolveBefore(object o)
	{
		return ((IssueBase)o).IsTriedToSolveBefore;
	}

	internal static object AutoGeneratedGetMemberValueAlternativeSolutionSentTroops(object o)
	{
		return ((IssueBase)o).AlternativeSolutionSentTroops;
	}

	internal static object AutoGeneratedGetMemberValueIssueDueTime(object o)
	{
		return ((IssueBase)o).IssueDueTime;
	}

	internal static object AutoGeneratedGetMemberValueIssueCreationTime(object o)
	{
		return ((IssueBase)o).IssueCreationTime;
	}

	internal static object AutoGeneratedGetMemberValue_totalTroopXpAmount(object o)
	{
		return ((IssueBase)o)._totalTroopXpAmount;
	}

	internal static object AutoGeneratedGetMemberValue_companionRewardSkill(object o)
	{
		return ((IssueBase)o)._companionRewardSkill;
	}

	internal static object AutoGeneratedGetMemberValue_journalEntries(object o)
	{
		return ((IssueBase)o)._journalEntries;
	}

	internal static object AutoGeneratedGetMemberValue_issueState(object o)
	{
		return ((IssueBase)o)._issueState;
	}

	internal static object AutoGeneratedGetMemberValue_issueOwner(object o)
	{
		return ((IssueBase)o)._issueOwner;
	}

	internal static object AutoGeneratedGetMemberValue_issueDifficultyMultiplier(object o)
	{
		return ((IssueBase)o)._issueDifficultyMultiplier;
	}

	internal static object AutoGeneratedGetMemberValue_areIssueEffectsResolved(object o)
	{
		return ((IssueBase)o)._areIssueEffectsResolved;
	}

	internal static object AutoGeneratedGetMemberValue_alternativeSolutionCasualtyCount(object o)
	{
		return ((IssueBase)o)._alternativeSolutionCasualtyCount;
	}

	internal static object AutoGeneratedGetMemberValue_failureChance(object o)
	{
		return ((IssueBase)o)._failureChance;
	}

	internal static object AutoGeneratedGetMemberValue_trackedObjects(object o)
	{
		return ((IssueBase)o)._trackedObjects;
	}

	public int GetTotalAlternativeSolutionNeededMenCount()
	{
		if (AlternativeSolutionHasScaledRequiredTroops && AlternativeSolutionHero != null)
		{
			return Campaign.Current.Models.IssueModel.GetTroopsRequiredForHero(AlternativeSolutionHero, this);
		}
		return AlternativeSolutionBaseNeededMenCount;
	}

	public int GetTotalAlternativeSolutionDurationInDays()
	{
		if (AlternativeSolutionHasScaledDuration && AlternativeSolutionHero != null)
		{
			return (int)Campaign.Current.Models.IssueModel.GetDurationOfResolutionForHero(AlternativeSolutionHero, this).ToDays;
		}
		return AlternativeSolutionBaseDurationInDaysInternal;
	}

	public int GetBaseAlternativeSolutionDurationInDays()
	{
		return AlternativeSolutionBaseDurationInDaysInternal;
	}

	public virtual bool AlternativeSolutionCondition(out TextObject explanation)
	{
		explanation = null;
		return true;
	}

	public virtual void AlternativeSolutionStartConsequence()
	{
	}

	public virtual bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
	{
		explanation = null;
		if (IsThereAlternativeSolution)
		{
			return AlternativeSolutionBaseNeededMenCount == 1;
		}
		return false;
	}

	protected virtual void AlternativeSolutionEndWithFailureConsequence()
	{
	}

	protected virtual void AlternativeSolutionEndWithSuccessConsequence()
	{
	}

	public virtual bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
	{
		return true;
	}

	public virtual bool LordSolutionCondition(out TextObject explanation)
	{
		explanation = null;
		return true;
	}

	protected virtual void LordSolutionConsequence()
	{
	}

	protected virtual void LordSolutionConsequenceWithRefuseCounterOffer()
	{
	}

	protected virtual void LordSolutionConsequenceWithAcceptCounterOffer()
	{
	}

	public override TextObject GetName()
	{
		return Title;
	}

	public float GetActiveIssueEffectAmount(IssueEffect issueEffect)
	{
		if (!_areIssueEffectsResolved)
		{
			return GetIssueEffectAmountInternal(issueEffect);
		}
		return 0f;
	}

	public virtual (SkillObject, int) GetAlternativeSolutionSkill(Hero hero)
	{
		return (null, 0);
	}

	protected virtual float GetIssueEffectAmountInternal(IssueEffect issueEffect)
	{
		return 0f;
	}

	protected IssueBase(Hero issueOwner, CampaignTime issueDueTime)
	{
		_issueOwner = issueOwner;
		IssueDueTime = issueDueTime;
		IssueDiscussAlternativeSolution = null;
		IssueCreationTime = CampaignTime.Now;
		_issueState = IssueState.Ongoing;
		IsTriedToSolveBefore = false;
		AlternativeSolutionSentTroops = TroopRoster.CreateDummyTroopRoster();
		_journalEntries = new MBList<JournalLog>();
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
	}

	public override string ToString()
	{
		return base.StringId;
	}

	public void InitializeIssueBaseOnLoad()
	{
		OnGameLoad();
	}

	internal void HourlyTickWithIssueManager()
	{
		HourlyTick();
	}

	protected abstract void OnGameLoad();

	protected abstract void HourlyTick();

	protected abstract QuestBase GenerateIssueQuest(string questId);

	public abstract IssueFrequency GetFrequency();

	protected abstract bool CanPlayerTakeQuestConditions(Hero issueGiver, out PreconditionFlags flag, out Hero relationHero, out SkillObject skill, out int requiredGold);

	public abstract bool IssueStayAliveConditions();

	protected abstract void CompleteIssueWithTimedOutConsequences();

	protected virtual void AfterIssueCreation()
	{
	}

	public virtual bool CanBeCompletedByAI()
	{
		return true;
	}

	protected virtual void OnIssueFinalized()
	{
	}

	public virtual void OnHeroCanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
	{
	}

	public virtual void OnHeroCanLeadPartyInfoIsRequested(Hero hero, ref bool result)
	{
	}

	public virtual void OnHeroCanHavePartyRoleOrBeGovernorInfoIsRequested(Hero hero, ref bool result)
	{
	}

	public virtual void OnHeroCanDieInfoIsRequested(Hero hero, KillCharacterAction.KillCharacterActionDetail causeOfDeath, ref bool result)
	{
	}

	public virtual void OnHeroCanBecomePrisonerInfoIsRequested(Hero hero, ref bool result)
	{
	}

	public virtual void OnHeroCanBeSelectedInInventoryInfoIsRequested(Hero hero, ref bool result)
	{
	}

	public virtual void OnHeroCanMoveToSettlementInfoIsRequested(Hero hero, ref bool result)
	{
	}

	public virtual void OnHeroCanMarryInfoIsRequested(Hero hero, ref bool result)
	{
	}

	public virtual void IsSettlementBusy(Settlement settlement, object asker, ref int priority)
	{
	}

	public bool StartIssueWithQuest()
	{
		_issueDifficultyMultiplier = Campaign.Current.Models.IssueModel.GetIssueDifficultyMultiplier();
		_issueState = IssueState.SolvingWithQuestSolution;
		IssueQuest = GenerateIssueQuest(base.StringId + "_quest");
		IsTriedToSolveBefore = true;
		IssueDueTime = CampaignTime.Never;
		CampaignEventDispatcher.Instance.OnIssueUpdated(this, IssueUpdateDetails.PlayerStartedIssueQuestClassicSolution, Hero.MainHero);
		return true;
	}

	public void StartIssueWithAlternativeSolution()
	{
		_issueDifficultyMultiplier = Campaign.Current.Models.IssueModel.GetIssueDifficultyMultiplier();
		IssueModel issueModel = Campaign.Current.Models.IssueModel;
		_failureChance = (AlternativeSolutionHasFailureRisk ? issueModel.GetFailureRiskForHero(AlternativeSolutionHero, this) : 0f);
		if (AlternativeSolutionHasCasualties)
		{
			(int, int) causalityForHero = issueModel.GetCausalityForHero(AlternativeSolutionHero, this);
			_alternativeSolutionCasualtyCount = MBRandom.RandomInt(causalityForHero.Item1, causalityForHero.Item2 + 1);
		}
		else
		{
			_alternativeSolutionCasualtyCount = 0;
		}
		_companionRewardSkill = issueModel.GetIssueAlternativeSolutionSkill(AlternativeSolutionHero, this).Item1;
		_issueState = IssueState.SolvingWithAlternativeSolution;
		IsTriedToSolveBefore = true;
		_totalTroopXpAmount = 1000f + 500f * IssueDifficultyMultiplier;
		AlternativeSolutionReturnTimeForTroops = CampaignTime.DaysFromNow(GetTotalAlternativeSolutionDurationInDays());
		IssueDueTime = AlternativeSolutionReturnTimeForTroops;
		AddLog(new JournalLog(CampaignTime.Now, AlternativeSolutionStartLog, new TextObject("{=VFO7rMzK}Return Days"), 0, AlternativeSolutionBaseDurationInDaysInternal));
		AlternativeSolutionIssueEffectClearTime = AlternativeSolutionReturnTimeForTroops - CampaignTime.Days(1f);
		if (AlternativeSolutionIssueEffectClearTime.IsPast)
		{
			AlternativeSolutionIssueEffectClearTime = AlternativeSolutionReturnTimeForTroops;
		}
		DisableHeroAction.Apply(AlternativeSolutionHero);
		if (LocationComplex.Current != null)
		{
			Location locationOfCharacter = LocationComplex.Current.GetLocationOfCharacter(AlternativeSolutionHero);
			if (locationOfCharacter != null)
			{
				LocationCharacter locationCharacter = locationOfCharacter.GetLocationCharacter(AlternativeSolutionHero);
				LocationComplex.Current.ChangeLocation(locationCharacter, locationOfCharacter, null);
			}
		}
		CampaignEventDispatcher.Instance.OnIssueUpdated(this, IssueUpdateDetails.PlayerSentTroopsToQuest, Hero.MainHero);
	}

	public void OnAlternativeSolutionSolvedAndTroopsAreReturning()
	{
		_areIssueEffectsResolved = true;
		AddLog(new JournalLog(CampaignTime.Now, AlternativeSolutionEndLogDefault));
	}

	public void IssueFinalized()
	{
		IssueQuest = null;
		CampaignEventDispatcher.Instance.RemoveListeners(this);
		Campaign.Current.IssueManager.DeactivateIssue(this);
		_areIssueEffectsResolved = true;
		AlternativeSolutionSentTroops.Clear();
		RemoveAllTrackedObjects();
		OnIssueFinalized();
	}

	public void CompleteIssueWithQuest()
	{
		CampaignEventDispatcher.Instance.OnIssueUpdated(this, IssueUpdateDetails.IssueFinishedWithSuccess, IsTriedToSolveBefore ? Hero.MainHero : null);
		IssueFinalized();
	}

	public void CompleteIssueWithTimedOut()
	{
		CampaignEventDispatcher.Instance.OnIssueUpdated(this, IssueUpdateDetails.IssueTimedOut, IsTriedToSolveBefore ? Hero.MainHero : null);
		IssueFinalized();
	}

	public void CompleteIssueWithStayAliveConditionsFailed()
	{
		CampaignEventDispatcher.Instance.OnIssueUpdated(this, IssueUpdateDetails.IssueCancel, IsTriedToSolveBefore ? Hero.MainHero : null);
		IssueFinalized();
	}

	public void CompleteIssueWithBetrayal()
	{
		if (IssueQuest != null && IssueQuest.IsOngoing)
		{
			IssueQuest.CompleteQuestWithBetrayal();
		}
		CampaignEventDispatcher.Instance.OnIssueUpdated(this, IssueUpdateDetails.IssueFinishedWithBetrayal, IsTriedToSolveBefore ? Hero.MainHero : null);
		IssueFinalized();
	}

	public void CompleteIssueWithFail(TextObject log = null)
	{
		if (IssueQuest != null && IssueQuest.IsOngoing)
		{
			IssueQuest.CompleteQuestWithFail(log);
		}
		CampaignEventDispatcher.Instance.OnIssueUpdated(this, IssueUpdateDetails.IssueFail, IsTriedToSolveBefore ? Hero.MainHero : null);
		IssueFinalized();
	}

	public void CompleteIssueWithCancel(TextObject log = null)
	{
		if (IssueQuest != null)
		{
			if (IssueQuest.IsOngoing)
			{
				IssueQuest.CompleteQuestWithCancel(log);
			}
		}
		else if (IsSolvingWithAlternative)
		{
			AddLog(new JournalLog(CampaignTime.Now, new TextObject("{=V5Za6d4h}Your troops have returned from their mission.")));
			Campaign.Current.IssueManager.TryToMakeTroopsReturn(this);
		}
		else if (IsSolvingWithLordSolution && log != null)
		{
			AddLog(new JournalLog(CampaignTime.Now, log));
		}
		CampaignEventDispatcher.Instance.OnIssueUpdated(this, IssueUpdateDetails.IssueCancel, IsTriedToSolveBefore ? Hero.MainHero : null);
		IssueFinalized();
	}

	public void CompleteIssueWithAiLord(Hero issueSolver)
	{
		CampaignEventDispatcher.Instance.OnIssueUpdated(this, IssueUpdateDetails.IssueFinishedByAILord, issueSolver);
		IssueFinalized();
	}

	private void AlternativeSolutionEndWithSuccess()
	{
		if (AlternativeSolutionHero == null)
		{
			Debug.Print("AlternativeSolutionHero is null for " + base.StringId);
			Debug.Print("AlternativeSolutionSentTroops:");
			foreach (TroopRosterElement item in AlternativeSolutionSentTroops.GetTroopRoster())
			{
				Debug.Print("troop id: " + item.Character.StringId + " count:" + item.Number);
			}
		}
		int totalManCount = AlternativeSolutionSentTroops.TotalManCount;
		AlternativeSolutionSentTroops.RemoveNumberOfNonHeroTroopsRandomly(_alternativeSolutionCasualtyCount);
		float num = 0.5f;
		float num2 = 1.2f - (float)AlternativeSolutionBaseNeededMenCount / (float)AlternativeSolutionSentTroops.TotalManCount;
		foreach (FlattenedTroopRosterElement item2 in AlternativeSolutionSentTroops.ToFlattenedRoster())
		{
			if (AlternativeSolutionBaseNeededMenCount < AlternativeSolutionSentTroops.TotalManCount)
			{
				num /= num2 * 0.9f + MBRandom.RandomFloat * 0.1f;
			}
			if (MBRandom.RandomFloat < num)
			{
				AlternativeSolutionSentTroops.WoundTroop(item2.Troop);
			}
			if (item2.Troop == AlternativeSolutionHero.CharacterObject && AlternativeSolutionHero.IsAlive)
			{
				AlternativeSolutionHero.AddSkillXp(_companionRewardSkill, CompanionSkillRewardXP);
			}
			num = 0.5f;
		}
		List<TroopRosterElement> list = AlternativeSolutionSentTroops.GetTroopRoster().FindAll((TroopRosterElement x) => x.Character.UpgradeTargets.Length != 0 || x.Character.IsHero);
		int num3 = MBRandom.RandomInt(1, list.Count + 1);
		int num4 = (int)(_totalTroopXpAmount / (float)num3);
		for (int num5 = 0; num5 < num3; num5++)
		{
			if (list.Count <= 0)
			{
				break;
			}
			List<(TroopRosterElement, float)> list2 = new List<(TroopRosterElement, float)>();
			foreach (TroopRosterElement item3 in list)
			{
				list2.Add((item3, item3.Number));
			}
			int index = AlternativeSolutionSentTroops.FindIndexOfTroop(MBRandom.ChooseWeighted(list2).Character);
			AlternativeSolutionSentTroops.SetElementXp(index, num4 + AlternativeSolutionSentTroops.GetElementXp(index));
		}
		if (RewardGold > 0)
		{
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
		}
		if (!TextObject.IsNullOrEmpty(IssueAlternativeSolutionSuccessLog))
		{
			AddLog(new JournalLog(CampaignTime.Now, IssueAlternativeSolutionSuccessLog));
		}
		TextObject textObject;
		if (_alternativeSolutionCasualtyCount > 0)
		{
			int variable = totalManCount - _alternativeSolutionCasualtyCount;
			textObject = new TextObject("{=fCHVyxJ1}{COMPANION.LINK} reported that {?COMPANION.GENDER}she{?}he{\\?} had resolved the matter. Out of {NUMBER1} {?(NUMBER1 > 1)}troops{?}troop{\\?} you sent {NUMBER2} {?(NUMBER2 > 1)}troops{?}troop{\\?} will join back to your party.");
			textObject.SetTextVariable("NUMBER1", totalManCount);
			textObject.SetTextVariable("NUMBER2", variable);
		}
		else
		{
			textObject = new TextObject("{=WOwaHClt}{COMPANION.LINK} reported that {?COMPANION.GENDER}she{?}he{\\?} had resolved the matter. {NUMBER} {?(NUMBER > 1)}troops{?}troop{\\?} you sent will join back to your party.");
			textObject.SetTextVariable("NUMBER", totalManCount);
		}
		StringHelpers.SetCharacterProperties("COMPANION", AlternativeSolutionHero.CharacterObject, textObject);
		AddLog(new JournalLog(CampaignTime.Now, textObject));
		CampaignEventDispatcher.Instance.OnIssueUpdated(this, IssueUpdateDetails.SentTroopsFinishedQuest, Hero.MainHero);
	}

	public void StartIssueWithLordSolution()
	{
		_issueDifficultyMultiplier = Campaign.Current.Models.IssueModel.GetIssueDifficultyMultiplier();
		if (!TextObject.IsNullOrEmpty(LordSolutionStartLog))
		{
			AddLog(new JournalLog(CampaignTime.Now, LordSolutionStartLog));
		}
		_issueState = IssueState.SolvingWithLordSolution;
		IsTriedToSolveBefore = true;
		CampaignEvents.BeforeGameMenuOpenedEvent.AddNonSerializedListener(this, BeforeGameMenuOpened);
	}

	private void BeforeGameMenuOpened(MenuCallbackArgs args)
	{
		if (_issueState != IssueState.SolvingWithLordSolution || Campaign.Current.GameMenuManager.NextLocation != null || !(GameStateManager.Current.ActiveState is MapState))
		{
			return;
		}
		if (CounterOfferHero != null)
		{
			if (IssueOwner.CurrentSettlement != null)
			{
				CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter), new ConversationCharacterData(CounterOfferHero.CharacterObject));
			}
		}
		else
		{
			CompleteIssueWithLordSolutionWithRefuseCounterOffer();
		}
	}

	public void CompleteIssueWithAlternativeSolution()
	{
		if (MBRandom.RandomFloat > _failureChance)
		{
			AlternativeSolutionEndWithSuccessConsequence();
			AlternativeSolutionEndWithSuccess();
		}
		else
		{
			AlternativeSolutionEndWithFailureConsequence();
			AlternativeSolutionEndWithFail();
		}
		Campaign.Current.IssueManager.TryToMakeTroopsReturn(this);
		IssueFinalized();
	}

	private void AlternativeSolutionEndWithFail()
	{
		int totalManCount = AlternativeSolutionSentTroops.TotalManCount;
		if (AlternativeSolutionHasCasualties)
		{
			AlternativeSolutionSentTroops.RemoveNumberOfNonHeroTroopsRandomly(_alternativeSolutionCasualtyCount);
			AlternativeSolutionHero.MakeWounded();
		}
		TextObject textObject;
		if (AlternativeSolutionHasCasualties && _alternativeSolutionCasualtyCount > 0)
		{
			textObject = new TextObject("{=yxwuGcDo}{COMPANION.LINK} has failed to resolve the matter. Out of {NUMBER1} troops you sent {NUMBER2} troops came back safe and sound.");
			textObject.SetTextVariable("NUMBER1", totalManCount);
			textObject.SetTextVariable("NUMBER2", totalManCount - _alternativeSolutionCasualtyCount);
		}
		else
		{
			textObject = new TextObject("{=k6fpAw92}{COMPANION.LINK} has failed to resolve the matter. {NUMBER} troops came back safe and sound.");
			textObject.SetTextVariable("NUMBER", totalManCount);
		}
		if (!TextObject.IsNullOrEmpty(IssueAlternativeSolutionFailLog))
		{
			AddLog(new JournalLog(CampaignTime.Now, IssueAlternativeSolutionFailLog));
		}
		StringHelpers.SetCharacterProperties("COMPANION", AlternativeSolutionHero.CharacterObject);
		AddLog(new JournalLog(CampaignTime.Now, textObject));
		CampaignEventDispatcher.Instance.OnIssueUpdated(this, IssueUpdateDetails.SentTroopsFailedQuest, Hero.MainHero);
	}

	public void CompleteIssueWithLordSolutionWithRefuseCounterOffer()
	{
		if (!TextObject.IsNullOrEmpty(LordSolutionCounterOfferRefuseLog))
		{
			AddLog(new JournalLog(CampaignTime.Now, LordSolutionCounterOfferRefuseLog));
		}
		ChangeClanInfluenceAction.Apply(Clan.PlayerClan, -NeededInfluenceForLordSolution);
		if (RewardGold > 0)
		{
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
		}
		LordSolutionConsequenceWithRefuseCounterOffer();
		IssueFinalized();
		CampaignEventDispatcher.Instance.OnIssueUpdated(this, IssueUpdateDetails.IssueFinishedWithSuccess, Hero.MainHero);
	}

	public void CompleteIssueWithLordSolutionWithAcceptCounterOffer()
	{
		if (!TextObject.IsNullOrEmpty(LordSolutionCounterOfferAcceptLog))
		{
			AddLog(new JournalLog(CampaignTime.Now, LordSolutionCounterOfferAcceptLog));
		}
		LordSolutionConsequenceWithAcceptCounterOffer();
		CompleteIssueWithBetrayal();
	}

	internal bool CheckPreconditions(Hero issueGiver, out TextObject explanation)
	{
		explanation = new TextObject("{=!}{EXPLANATION}");
		if (!IssueStayAliveConditions() && IsOngoingWithoutQuest)
		{
			CompleteIssueWithCancel();
			return false;
		}
		PreconditionFlags flag;
		Hero relationHero;
		SkillObject skill;
		int requiredGold;
		bool result = CanPlayerTakeQuestConditions(issueGiver, out flag, out relationHero, out skill, out requiredGold);
		bool flag2 = false;
		if (!IssueQuestCanBeDuplicated)
		{
			foreach (KeyValuePair<Hero, IssueBase> issue in Campaign.Current.IssueManager.Issues)
			{
				IssueBase value = issue.Value;
				if ((value.IsSolvingWithQuest || value.IsSolvingWithAlternative) && value.GetType() == GetType())
				{
					flag2 = true;
					result = false;
				}
			}
		}
		if ((flag & PreconditionFlags.AtWar) == PreconditionFlags.AtWar)
		{
			explanation.SetTextVariable("EXPLANATION", new TextObject("{=21dlZJt6}I don't wish to speak about that. As you know, our factions are at war."));
		}
		else if (flag2)
		{
			explanation.SetTextVariable("EXPLANATION", new TextObject("{=HvY7wjHt}I don't think you can help me. I think you may have other, similar commitments that could interfere."));
		}
		else if ((flag & PreconditionFlags.NotInSameFaction) == PreconditionFlags.NotInSameFaction)
		{
			explanation.SetTextVariable("EXPLANATION", new TextObject("{=rBPI2dvX}I don't need the service of strangers. I work only with lords of the realm and loyal mercenaries.[ib:closed][if:convo_grave]"));
		}
		else if ((flag & PreconditionFlags.MainHeroIsKingdomLeader) == PreconditionFlags.MainHeroIsKingdomLeader || (flag & PreconditionFlags.PlayerIsOwnerOfSettlement) == PreconditionFlags.PlayerIsOwnerOfSettlement)
		{
			explanation.SetTextVariable("EXPLANATION", new TextObject("{=dYJKy2mO}Thank you for asking my {?PLAYER.GENDER}lady{?}lord{\\?}, but I can't bother you with such an unimportant issue."));
		}
		else if ((flag & PreconditionFlags.ClanTier) == PreconditionFlags.ClanTier)
		{
			explanation.SetTextVariable("EXPLANATION", new TextObject("{=QOiPDGbf}I have never heard of your clan. I am not sure if I can rely on you or not.[ib:closed][if:convo_grave]"));
		}
		else if ((flag & PreconditionFlags.Renown) == PreconditionFlags.Renown)
		{
			explanation.SetTextVariable("EXPLANATION", new TextObject("{=7uJcPQnc}I don't think you can help me. I'm looking for someone with a bit more, shall we say, renown..."));
		}
		else if ((flag & PreconditionFlags.Relation) == PreconditionFlags.Relation)
		{
			TextObject textObject;
			if (issueGiver == relationHero)
			{
				textObject = new TextObject("{=Cn4lnECZ}You and I do not have a good history... I don't trust you.[ib:closed][if:convo_grave]");
			}
			else
			{
				textObject = new TextObject("{=5ZJMa7Om}I don't think you can help me. I've heard you have a history with {HERO.LINK}, and, well, that could complicate things...[ib:closed][if:convo_grave]");
				StringHelpers.SetCharacterProperties("HERO", relationHero.CharacterObject, textObject);
			}
			explanation.SetTextVariable("EXPLANATION", textObject);
		}
		else if ((flag & PreconditionFlags.Skill) == PreconditionFlags.Skill)
		{
			TextObject textObject2 = new TextObject("{=S9yUBtKc}I don't think you can help me. You need to have some experience in {SKILL_NAME}...");
			textObject2.SetTextVariable("SKILL_NAME", skill.Name);
			explanation.SetTextVariable("EXPLANATION", textObject2);
		}
		else if ((flag & PreconditionFlags.Money) == PreconditionFlags.Money)
		{
			TextObject textObject3 = new TextObject("{=GdXQhtME}I don't think you can help me. I need someone who has at least {GOLD_AMOUNT} gold to spend...[ib:closed]");
			textObject3.SetTextVariable("GOLD_AMOUNT", requiredGold);
			explanation.SetTextVariable("EXPLANATION", textObject3);
		}
		else if ((flag & PreconditionFlags.Influence) == PreconditionFlags.Influence)
		{
			explanation.SetTextVariable("EXPLANATION", new TextObject("{=b6Zc1yre}I don't think you can help me. You'd need a bit of influence..."));
		}
		else if ((flag & PreconditionFlags.Wounded) == PreconditionFlags.Wounded)
		{
			explanation.SetTextVariable("EXPLANATION", new TextObject("{=BUf9WeyN}I don't think you can help me. You should rest for a while and let your wounds heal."));
		}
		else if ((flag & PreconditionFlags.NotEnoughTroops) == PreconditionFlags.NotEnoughTroops)
		{
			explanation.SetTextVariable("EXPLANATION", new TextObject("{=dCv4Qbr6}I don't think you can help me. You don't have enough troops for this task."));
		}
		else if ((flag & PreconditionFlags.PartySizeLimit) == PreconditionFlags.PartySizeLimit)
		{
			explanation.SetTextVariable("EXPLANATION", new TextObject("{=yaiQgyfB}I was planning to give you some troops to solve this task but it seems like you would have difficulties taking any more into your company."));
		}
		else if ((flag & PreconditionFlags.ClanIsMercenary) == PreconditionFlags.ClanIsMercenary)
		{
			explanation.SetTextVariable("EXPLANATION", new TextObject("{=vz4M8SRn}I do have one particular task, but it is not really suited to a mercenary.Please carry on with your other duties."));
		}
		else if ((flag & PreconditionFlags.CompanionLimitReached) == PreconditionFlags.CompanionLimitReached)
		{
			explanation.SetTextVariable("EXPLANATION", new TextObject("{=144bieK5}I was planning to entrust one of my people to you, but it seems you have enough companions in your party."));
		}
		else
		{
			explanation.SetTextVariable("EXPLANATION", TextObject.GetEmpty());
		}
		return result;
	}

	internal void AfterCreation()
	{
		AfterIssueCreation();
	}

	public void InitializeIssueOnSettlementOwnerChange()
	{
		if (IsThereLordSolution)
		{
			Campaign.Current.ConversationManager.RemoveRelatedLines(this);
		}
	}

	private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		if (IssueSettlement == settlement || (IssueSettlement == null && IssueOwner.CurrentSettlement == settlement && IssueOwner.IsNoncombatant))
		{
			Campaign.Current.ConversationManager.RemoveRelatedLines(this);
		}
	}

	public void AddLog(JournalLog log)
	{
		_journalEntries.Add(log);
		CampaignEventDispatcher.Instance.OnIssueLogAdded(this, hideInformation: false);
	}

	private void RemoveAllTrackedObjects()
	{
		foreach (ITrackableCampaignObject trackedObject in _trackedObjects)
		{
			Campaign.Current.VisualTrackerManager.RemoveTrackedObject(trackedObject);
		}
		_trackedObjects.Clear();
	}

	public void AddTrackedObject(ITrackableCampaignObject o)
	{
		_trackedObjects.Add(o);
		Campaign.Current.VisualTrackerManager.RegisterObject(o);
	}

	public void ToggleTrackedObjects(bool enableTrack)
	{
		if (enableTrack)
		{
			foreach (ITrackableCampaignObject trackedObject in _trackedObjects)
			{
				Campaign.Current.VisualTrackerManager.RegisterObject(trackedObject);
			}
			return;
		}
		foreach (ITrackableCampaignObject trackedObject2 in _trackedObjects)
		{
			Campaign.Current.VisualTrackerManager.RemoveTrackedObject(trackedObject2);
		}
	}
}
