using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Quests;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Nameplate;

public class PartyNameplateVM : NameplateVM
{
	public static string PositiveIndicator = Color.FromUint(4285650500u).ToString();

	public static string PositiveArmyIndicator = Color.FromUint(4288804731u).ToString();

	public static string NegativeIndicator = Color.FromUint(4292232774u).ToString();

	public static string NegativeArmyIndicator = Color.FromUint(4294931829u).ToString();

	public static string NeutralIndicator = Color.FromUint(4291877096u).ToString();

	public static string NeutralArmyIndicator = Color.FromUint(4294573055u).ToString();

	public static string MainPartyIndicator = Color.FromUint(4287421380u).ToString();

	public static string MainPartyArmyIndicator = Color.FromUint(4289593317u).ToString();

	public static string AllianceIndicator = Color.FromUint(4279460044u).ToString();

	public static string AllianceArmyIndicator = Color.FromUint(4279476684u).ToString();

	protected float _latestX;

	protected float _latestY;

	protected float _latestW;

	protected float _cachedSpeed;

	protected Camera _mapCamera;

	protected int _latestPrisonerAmount = -1;

	protected int _latestWoundedAmount = -1;

	protected int _latestTotalCount = -1;

	protected bool _isPartyBannerDirty;

	protected TextObject _latestNameTextObject;

	protected CampaignUIHelper.IssueQuestFlags _previousQuestsBind;

	protected CampaignUIHelper.IssueQuestFlags _questsBind;

	protected Vec2 _partyPositionBind;

	protected Vec2 _headPositionBind;

	protected bool _isHighBind;

	protected bool _isBehindBind;

	protected bool _isInArmyBind;

	protected bool _isInSettlementBind;

	protected bool _isVisibleOnMapBind;

	protected bool _isArmyBind;

	protected bool _isDisorganizedBind;

	protected bool _isCurrentlyAtSeaBind;

	protected string _factionColorBind;

	protected string _countBind;

	protected string _woundedBind;

	protected string _prisonerBind;

	protected string _extraInfoTextBind;

	protected string _fullNameBind;

	protected string _movementSpeedTextBind;

	private string _count;

	private string _wounded;

	private string _prisoner;

	private MBBindingList<QuestMarkerVM> _quests;

	private string _fullName;

	private string _extraInfoText;

	private string _movementSpeedText;

	private bool _isBehind;

	private bool _isHigh;

	private bool _shouldShowFullName;

	private bool _isInArmy;

	private bool _isArmy;

	private bool _isInSettlement;

	private bool _isDisorganized;

	private bool _isCurrentlyAtSea;

	private BannerImageIdentifierVM _partyBanner;

	private Vec2 _headPosition;

	public MobileParty Party { get; private set; }

	public Vec2 HeadPosition
	{
		get
		{
			return _headPosition;
		}
		set
		{
			if (value != _headPosition)
			{
				_headPosition = value;
				OnPropertyChangedWithValue(value, "HeadPosition");
			}
		}
	}

	public string Count
	{
		get
		{
			return _count;
		}
		set
		{
			if (value != _count)
			{
				_count = value;
				OnPropertyChangedWithValue(value, "Count");
			}
		}
	}

	public string Prisoner
	{
		get
		{
			return _prisoner;
		}
		set
		{
			if (value != _prisoner)
			{
				_prisoner = value;
				OnPropertyChangedWithValue(value, "Prisoner");
			}
		}
	}

	public MBBindingList<QuestMarkerVM> Quests
	{
		get
		{
			return _quests;
		}
		set
		{
			if (value != _quests)
			{
				_quests = value;
				OnPropertyChangedWithValue(value, "Quests");
			}
		}
	}

	public string Wounded
	{
		get
		{
			return _wounded;
		}
		set
		{
			if (value != _wounded)
			{
				_wounded = value;
				OnPropertyChangedWithValue(value, "Wounded");
			}
		}
	}

	public string ExtraInfoText
	{
		get
		{
			return _extraInfoText;
		}
		set
		{
			if (value != _extraInfoText)
			{
				_extraInfoText = value;
				OnPropertyChangedWithValue(value, "ExtraInfoText");
			}
		}
	}

	public string MovementSpeedText
	{
		get
		{
			return _movementSpeedText;
		}
		set
		{
			if (value != _movementSpeedText)
			{
				_movementSpeedText = value;
				OnPropertyChangedWithValue(value, "MovementSpeedText");
			}
		}
	}

	public string FullName
	{
		get
		{
			return _fullName;
		}
		set
		{
			if (value != _fullName)
			{
				_fullName = value;
				OnPropertyChangedWithValue(value, "FullName");
			}
		}
	}

	public bool IsInArmy
	{
		get
		{
			return _isInArmy;
		}
		set
		{
			if (value != _isInArmy)
			{
				_isInArmy = value;
				OnPropertyChangedWithValue(value, "IsInArmy");
			}
		}
	}

	public bool IsInSettlement
	{
		get
		{
			return _isInSettlement;
		}
		set
		{
			if (value != _isInSettlement)
			{
				_isInSettlement = value;
				OnPropertyChangedWithValue(value, "IsInSettlement");
			}
		}
	}

	public bool IsDisorganized
	{
		get
		{
			return _isDisorganized;
		}
		set
		{
			if (value != _isDisorganized)
			{
				_isDisorganized = value;
				OnPropertyChangedWithValue(value, "IsDisorganized");
			}
		}
	}

	public bool IsCurrentlyAtSea
	{
		get
		{
			return _isCurrentlyAtSea;
		}
		set
		{
			if (value != _isCurrentlyAtSea)
			{
				_isCurrentlyAtSea = value;
				OnPropertyChangedWithValue(value, "IsCurrentlyAtSea");
			}
		}
	}

	public bool IsArmy
	{
		get
		{
			return _isArmy;
		}
		set
		{
			if (value != _isArmy)
			{
				_isArmy = value;
				OnPropertyChangedWithValue(value, "IsArmy");
			}
		}
	}

	public bool IsBehind
	{
		get
		{
			return _isBehind;
		}
		set
		{
			if (value != _isBehind)
			{
				_isBehind = value;
				OnPropertyChangedWithValue(value, "IsBehind");
			}
		}
	}

	public bool IsHigh
	{
		get
		{
			return _isHigh;
		}
		set
		{
			if (value != _isHigh)
			{
				_isHigh = value;
				OnPropertyChangedWithValue(value, "IsHigh");
			}
		}
	}

	public bool ShouldShowFullName
	{
		get
		{
			if (!_shouldShowFullName)
			{
				return base.IsTargetedByTutorial;
			}
			return true;
		}
		set
		{
			if (value != _shouldShowFullName)
			{
				_shouldShowFullName = value;
				OnPropertyChangedWithValue(value, "ShouldShowFullName");
			}
		}
	}

	public BannerImageIdentifierVM PartyBanner
	{
		get
		{
			return _partyBanner;
		}
		set
		{
			if (value != _partyBanner)
			{
				_partyBanner = value;
				OnPropertyChangedWithValue(value, "PartyBanner");
			}
		}
	}

	public PartyNameplateVM()
	{
		Quests = new MBBindingList<QuestMarkerVM>();
	}

	public void InitializeWith(MobileParty party, Camera mapCamera)
	{
		_mapCamera = mapCamera;
		Party = party;
		_isPartyBannerDirty = true;
		Quests.Clear();
		RegisterEvents();
	}

	public virtual void Clear()
	{
		_mapCamera = null;
		Party = null;
		_isPartyBannerDirty = false;
		_latestNameTextObject = null;
		_previousQuestsBind = CampaignUIHelper.IssueQuestFlags.None;
		Quests.Clear();
		OnFinalize();
		UnregisterEvents();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		RefreshDynamicProperties(forceUpdate: true);
	}

	public void RegisterEvents()
	{
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangeKingdom);
		CampaignEvents.OnClanLeaderChangedEvent.AddNonSerializedListener(this, OnClanLeaderChanged);
		CampaignEvents.OnHeroTeleportationRequestedEvent.AddNonSerializedListener(this, OnHeroTeleportationRequested);
		if (Game.Current != null)
		{
			Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(base.OnTutorialNotificationElementChanged);
		}
	}

	public void UnregisterEvents()
	{
		CampaignEvents.OnSettlementOwnerChangedEvent.ClearListeners(this);
		CampaignEvents.OnClanChangedKingdomEvent.ClearListeners(this);
		CampaignEvents.OnClanLeaderChangedEvent.ClearListeners(this);
		CampaignEvents.OnHeroTeleportationRequestedEvent.ClearListeners(this);
		Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(base.OnTutorialNotificationElementChanged);
	}

	private void AddQuestBindFlagsForParty(MobileParty party)
	{
		if (party == MobileParty.MainParty || party == Party)
		{
			return;
		}
		if (party.LeaderHero?.Issue != null && (_questsBind & CampaignUIHelper.IssueQuestFlags.TrackedIssue) == 0 && ((_questsBind & CampaignUIHelper.IssueQuestFlags.AvailableIssue) == 0 || (_questsBind & CampaignUIHelper.IssueQuestFlags.ActiveIssue) == 0))
		{
			_questsBind |= CampaignUIHelper.GetIssueType(party.LeaderHero.Issue);
		}
		if (((_questsBind & CampaignUIHelper.IssueQuestFlags.TrackedStoryQuest) != CampaignUIHelper.IssueQuestFlags.None || (_questsBind & CampaignUIHelper.IssueQuestFlags.ActiveIssue) != CampaignUIHelper.IssueQuestFlags.None) && (_questsBind & CampaignUIHelper.IssueQuestFlags.ActiveStoryQuest) != CampaignUIHelper.IssueQuestFlags.None)
		{
			return;
		}
		List<QuestBase> questsRelatedToParty = CampaignUIHelper.GetQuestsRelatedToParty(party);
		for (int i = 0; i < questsRelatedToParty.Count; i++)
		{
			QuestBase questBase = questsRelatedToParty[i];
			if (party.LeaderHero != null && questBase.QuestGiver == party.LeaderHero)
			{
				if (questBase.IsSpecialQuest && (_questsBind & CampaignUIHelper.IssueQuestFlags.ActiveStoryQuest) == 0)
				{
					_questsBind |= CampaignUIHelper.IssueQuestFlags.ActiveStoryQuest;
				}
				else if (!questBase.IsSpecialQuest && (_questsBind & CampaignUIHelper.IssueQuestFlags.ActiveIssue) == 0)
				{
					_questsBind |= CampaignUIHelper.IssueQuestFlags.ActiveIssue;
				}
			}
			else if (questBase.IsSpecialQuest && (_questsBind & CampaignUIHelper.IssueQuestFlags.TrackedStoryQuest) == 0)
			{
				_questsBind |= CampaignUIHelper.IssueQuestFlags.TrackedStoryQuest;
			}
			else if (!questBase.IsSpecialQuest && (_questsBind & CampaignUIHelper.IssueQuestFlags.TrackedIssue) == 0)
			{
				_questsBind |= CampaignUIHelper.IssueQuestFlags.TrackedIssue;
			}
		}
	}

	public override void RefreshDynamicProperties(bool forceUpdate)
	{
		base.RefreshDynamicProperties(forceUpdate);
		if (_isVisibleOnMapBind || forceUpdate)
		{
			IssueBase issueBase = Party?.LeaderHero?.Issue;
			_questsBind = CampaignUIHelper.IssueQuestFlags.None;
			if (Party != MobileParty.MainParty)
			{
				if (issueBase != null)
				{
					_questsBind |= CampaignUIHelper.GetIssueType(issueBase);
				}
				List<QuestBase> questsRelatedToParty = CampaignUIHelper.GetQuestsRelatedToParty(Party);
				for (int i = 0; i < questsRelatedToParty.Count; i++)
				{
					QuestBase questBase = questsRelatedToParty[i];
					if (questBase.QuestGiver != null && questBase.QuestGiver == Party.LeaderHero)
					{
						_questsBind |= (CampaignUIHelper.IssueQuestFlags)(questBase.IsSpecialQuest ? 4 : 2);
					}
					else
					{
						_questsBind |= (CampaignUIHelper.IssueQuestFlags)(questBase.IsSpecialQuest ? 16 : 8);
					}
				}
			}
		}
		_isInArmyBind = Party.Army != null && Party.AttachedTo != null;
		_isArmyBind = Party.Army != null && Party.Army.LeaderParty == Party;
		_isInSettlementBind = Party?.CurrentSettlement != null;
		if (_isArmyBind && (_isVisibleOnMapBind || forceUpdate))
		{
			AddQuestBindFlagsForParty(Party.Army.LeaderParty);
			for (int j = 0; j < Party.Army.LeaderParty.AttachedParties.Count; j++)
			{
				MobileParty party = Party.Army.LeaderParty.AttachedParties[j];
				AddQuestBindFlagsForParty(party);
			}
		}
		if (_isArmyBind || !_isInArmy || forceUpdate)
		{
			int partyHealthyCount = SandBoxUIHelper.GetPartyHealthyCount(Party);
			if (partyHealthyCount != _latestTotalCount)
			{
				_latestTotalCount = partyHealthyCount;
				_countBind = (Party.IsInfoHidden ? "?" : partyHealthyCount.ToString());
			}
			int allWoundedMembersAmount = SandBoxUIHelper.GetAllWoundedMembersAmount(Party);
			int allPrisonerMembersAmount = SandBoxUIHelper.GetAllPrisonerMembersAmount(Party);
			if (_latestWoundedAmount != allWoundedMembersAmount || _latestPrisonerAmount != allPrisonerMembersAmount)
			{
				if (_latestWoundedAmount != allWoundedMembersAmount)
				{
					_woundedBind = ((allWoundedMembersAmount == 0) ? "" : (Party.IsInfoHidden ? "?" : SandBoxUIHelper.GetPartyWoundedText(allWoundedMembersAmount)));
					_latestWoundedAmount = allWoundedMembersAmount;
				}
				if (_latestPrisonerAmount != allPrisonerMembersAmount)
				{
					_prisonerBind = ((allPrisonerMembersAmount == 0) ? "" : (Party.IsInfoHidden ? "?" : SandBoxUIHelper.GetPartyPrisonerText(allPrisonerMembersAmount)));
					_latestPrisonerAmount = allPrisonerMembersAmount;
				}
				_extraInfoTextBind = _woundedBind + _prisonerBind;
			}
		}
		if (!Party.IsMainParty)
		{
			Army army = Party.Army;
			if (army == null || !army.LeaderParty.AttachedParties.Contains(MobileParty.MainParty) || !Party.Army.LeaderParty.AttachedParties.Contains(Party))
			{
				if (Hero.MainHero?.MapFaction != null && FactionManager.IsAtWarAgainstFaction(Party.MapFaction, Hero.MainHero?.MapFaction))
				{
					_factionColorBind = ((Party.Army != null && Party.Army.LeaderParty == Party) ? NegativeArmyIndicator : NegativeIndicator);
				}
				else if (DiplomacyHelper.IsSameFactionAndNotEliminated(Party.MapFaction, Hero.MainHero.MapFaction))
				{
					_factionColorBind = ((Party.Army != null && Party.Army.LeaderParty == Party) ? PositiveArmyIndicator : PositiveIndicator);
				}
				else if (DiplomacyHelper.HasAllianceWithFaction(Party.MapFaction, Hero.MainHero?.MapFaction))
				{
					_factionColorBind = ((Party.Army != null && Party.Army.LeaderParty == Party) ? AllianceArmyIndicator : AllianceIndicator);
				}
				else
				{
					_factionColorBind = ((Party.Army != null && Party.Army.LeaderParty == Party) ? NeutralArmyIndicator : NeutralIndicator);
				}
				goto IL_04cd;
			}
		}
		_factionColorBind = ((Party.Army != null && Party.Army.LeaderParty == Party) ? MainPartyArmyIndicator : MainPartyIndicator);
		goto IL_04cd;
		IL_04cd:
		if (_isPartyBannerDirty || forceUpdate)
		{
			PartyBanner = new BannerImageIdentifierVM(Party.Banner, nineGrid: true);
			_isPartyBannerDirty = false;
		}
		if (_isVisibleOnMapBind && (_isInArmyBind || _isInSettlementBind || (!Party.IsMainParty && Party.IsInRaftState)))
		{
			_isVisibleOnMapBind = false;
		}
		Army army2 = Party.Army;
		TextObject textObject = ((army2 != null && army2.DoesLeaderPartyAndAttachedPartiesContain(Party)) ? Party.ArmyName : ((Party.LeaderHero == null) ? Party.Name : Party.LeaderHero.Name));
		_isDisorganizedBind = Party.IsDisorganized;
		if (_latestNameTextObject == null || forceUpdate || !_latestNameTextObject.Equals(textObject))
		{
			_latestNameTextObject = textObject;
			_fullNameBind = _latestNameTextObject.ToString();
		}
		if (Party.IsActive && !_cachedSpeed.ApproximatelyEqualsTo(Party.Speed, 0.01f))
		{
			_cachedSpeed = Party.Speed;
			_movementSpeedTextBind = _cachedSpeed.ToString("F1");
		}
		_isCurrentlyAtSeaBind = Party.IsCurrentlyAtSea;
	}

	public override void RefreshPosition()
	{
		base.RefreshPosition();
		Vec3 vec = (Party.Position + Party.EventPositionAdder).AsVec3();
		MapEvent mapEvent = Party.MapEvent;
		if (mapEvent != null && mapEvent.MapEventSettlement?.IsVillage == true && Party.IsCurrentlyAtSea)
		{
			vec = Party.MapEvent.MapEventSettlement.GatePosition.AsVec3();
			vec += new Vec3(Party.RandomFloatWithSeed((uint)Party.RandomValue, -0.3f, 0.3f), Party.RandomFloatWithSeed((uint)Party.RandomValue, -0.3f, 0.3f));
		}
		Vec3 worldSpacePosition = vec + new Vec3(0f, 0f, 0.8f);
		_latestX = 0f;
		_latestY = 0f;
		_latestW = 0f;
		MBWindowManager.WorldToScreenInsideUsableArea(_mapCamera, vec, ref _latestX, ref _latestY, ref _latestW);
		_partyPositionBind = new Vec2(_latestX, _latestY);
		MBWindowManager.WorldToScreenInsideUsableArea(_mapCamera, worldSpacePosition, ref _latestX, ref _latestY, ref _latestW);
		_headPositionBind = new Vec2(_latestX, _latestY);
		base.DistanceToCamera = vec.Distance(_mapCamera.Position);
	}

	public override void RefreshTutorialStatus(string newTutorialHighlightElementID)
	{
		base.RefreshTutorialStatus(newTutorialHighlightElementID);
		if (Party?.Party?.Id == null)
		{
			Debug.FailedAssert("Mobile party id is null when refreshing tutorial status", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\Nameplate\\PartyNameplateVM.cs", "RefreshTutorialStatus", 357);
		}
		else
		{
			_bindIsTargetedByTutorial = ((Party.Party.Id == newTutorialHighlightElementID) ? true : false);
		}
	}

	public void DetermineIsVisibleOnMap()
	{
		_isVisibleOnMapBind = _latestW < 100f && _latestW > 0f && _mapCamera.Position.z < 200f;
	}

	private bool IsInsideWindow()
	{
		if (!(_latestX > Screen.RealScreenResolutionWidth) && !(_latestY > Screen.RealScreenResolutionHeight) && !(_latestX + 100f < 0f))
		{
			return !(_latestY + 30f < 0f);
		}
		return false;
	}

	public virtual void RefreshBinding()
	{
		base.Position = _partyPositionBind;
		HeadPosition = _headPositionBind;
		base.IsVisibleOnMap = _isVisibleOnMapBind;
		IsInSettlement = _isInSettlementBind;
		base.FactionColor = _factionColorBind;
		IsHigh = _isHighBind;
		Count = _countBind;
		Prisoner = _prisonerBind;
		Wounded = _woundedBind;
		IsBehind = _isBehindBind;
		FullName = _fullNameBind;
		base.IsTargetedByTutorial = _bindIsTargetedByTutorial;
		IsInArmy = _isInArmyBind;
		IsArmy = _isArmyBind;
		ExtraInfoText = _extraInfoTextBind;
		IsDisorganized = _isDisorganizedBind;
		MovementSpeedText = _movementSpeedTextBind;
		IsCurrentlyAtSea = _isCurrentlyAtSeaBind;
		if (_previousQuestsBind == _questsBind)
		{
			return;
		}
		Quests.Clear();
		for (int i = 0; i < CampaignUIHelper.IssueQuestFlagsValues.Length; i++)
		{
			CampaignUIHelper.IssueQuestFlags issueQuestFlags = CampaignUIHelper.IssueQuestFlagsValues[i];
			if (issueQuestFlags != CampaignUIHelper.IssueQuestFlags.None && (_questsBind & issueQuestFlags) != CampaignUIHelper.IssueQuestFlags.None)
			{
				Quests.Add(new QuestMarkerVM(issueQuestFlags));
			}
		}
		_previousQuestsBind = _questsBind;
	}

	private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		bool flag = Party.HomeSettlement != null && (Party.HomeSettlement.IsVillage ? settlement.BoundVillages.Contains(Party.HomeSettlement.Village) : (Party.HomeSettlement == settlement));
		if ((Party.IsCaravan || Party.IsVillager) && flag)
		{
			_isPartyBannerDirty = true;
		}
	}

	private void OnClanChangeKingdom(Clan arg1, Kingdom arg2, Kingdom arg3, ChangeKingdomAction.ChangeKingdomActionDetail arg4, bool showNotification)
	{
		if (Party.LeaderHero?.Clan == arg1)
		{
			_isPartyBannerDirty = true;
		}
	}

	private void OnClanLeaderChanged(Hero arg1, Hero arg2)
	{
		if (arg2.MapFaction == Party.MapFaction)
		{
			_isPartyBannerDirty = true;
		}
	}

	private void OnHeroTeleportationRequested(Hero arg1, Settlement arg2, MobileParty arg3, TeleportHeroAction.TeleportationDetail arg4)
	{
		if (arg1.MapFaction == Party.MapFaction)
		{
			_isPartyBannerDirty = true;
		}
	}
}
