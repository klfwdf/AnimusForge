using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Load;

namespace TaleWorlds.CampaignSystem.Party;

public sealed class MobileParty : CampaignObjectBase, ILocatable<MobileParty>, IMapPoint, ITrackableCampaignObject, ITrackableBase, IRandomOwner
{
	public enum PartyObjective
	{
		Neutral,
		Defensive,
		Aggressive,
		NumberOfPartyObjectives
	}

	[Flags]
	public enum NavigationType
	{
		None = 0,
		Default = 1,
		Naval = 2,
		All = 3
	}

	internal struct CachedPartyVariables
	{
		internal bool IsAttachedArmyMember;

		internal bool IsArmyLeader;

		internal bool IsMoving;

		internal bool HasMapEvent;

		internal float NextMoveDistance;

		internal CampaignVec2 CurrentPosition;

		internal CampaignVec2 LastCurrentPosition;

		internal CampaignVec2 NextPosition;

		internal CampaignVec2 TargetPartyPositionAtFrameStart;

		internal bool IsTargetMovingAtFrameStart;

		internal bool IsTransitionInProgress;

		public override string ToString()
		{
			return string.Concat("IsAttachedArmyMember:", IsAttachedArmyMember.ToString(), "\nIsArmyLeader", IsArmyLeader.ToString(), "\nIsMoving", IsMoving.ToString(), "\nHasMapEvent", HasMapEvent.ToString(), "\nNextMoveDistance", NextMoveDistance, "\nCurrentPosition", CurrentPosition, "\nLastCurrentPosition", LastCurrentPosition, "\nNextPosition", NextPosition, "\nTargetPartyPositionAtFrameStart", TargetPartyPositionAtFrameStart, "\n");
		}
	}

	public const int DefaultPartyTradeInitialGold = 5000;

	public const int ClanRoleAssignmentMinimumSkillValue = 0;

	public const int MinimumSpareGoldForWageBudget = 5;

	private const int ArrayLength = 6;

	[SaveableField(1001)]
	private Settlement _currentSettlement;

	[CachedData]
	private MBList<MobileParty> _attachedParties;

	[SaveableField(1046)]
	private MobileParty _attachedTo;

	[SaveableField(1006)]
	public float HasUnpaidWages;

	[SaveableField(1060)]
	private Vec2 _eventPositionAdder;

	[SaveableField(1026)]
	private bool _isInRaftState;

	[SaveableField(1040)]
	public bool IsInfoHidden;

	[SaveableField(1024)]
	private bool _isVisible;

	[CachedData]
	internal float _lastCalculatedSpeed = 1f;

	[CachedData]
	public CampaignVec2 NextLongTermPathPoint = CampaignVec2.Invalid;

	[SaveableField(1025)]
	private bool _isInspected;

	[SaveableField(1955)]
	private CampaignTime _disorganizedUntilTime;

	[CachedData]
	private int _partyPureSpeedLastCheckVersion = -1;

	[CachedData]
	private bool _partyLastCheckIsPrisoner;

	[CachedData]
	private ExplainedNumber _lastCalculatedBaseSpeedExplained;

	[CachedData]
	private bool _partyLastCheckAtNight;

	[CachedData]
	private int _itemRosterVersionNo = -1;

	[CachedData]
	private int _partySizeRatioLastCheckVersion = -1;

	[CachedData]
	private int _partyWeightLastCheckVersionNo = -1;

	[CachedData]
	private float _cachedPartySizeRatio = 1f;

	[SaveableField(1059)]
	private BesiegerCamp _besiegerCamp;

	[SaveableField(1048)]
	private MobileParty _targetParty;

	[SaveableField(1049)]
	private Settlement _targetSettlement;

	[SaveableField(224)]
	private CampaignVec2 _targetPosition;

	private int _doNotAttackMainParty;

	[SaveableField(1034)]
	private Settlement _customHomeSettlement;

	[SaveableField(1035)]
	private Army _army;

	[CachedData]
	private bool _isDisorganized;

	[SaveableField(1959)]
	private bool _isCurrentlyUsedByAQuest;

	[SaveableField(1956)]
	private int _partyTradeGold;

	[SaveableField(1063)]
	private CampaignTime _ignoredUntilTime;

	[SaveableField(1071)]
	public Vec2 AverageFleeTargetDirection;

	private bool _besiegerCampResetStarted;

	[SaveableField(211)]
	private bool _pathMode;

	[SaveableField(220)]
	private CampaignVec2 _pathLastPosition;

	[SaveableField(221)]
	public CampaignVec2 NextTargetPosition;

	[SaveableField(222)]
	public CampaignVec2 MoveTargetPoint;

	[SaveableField(215)]
	public MoveModeType PartyMoveMode;

	[SaveableField(216)]
	private Vec2 _formationPosition;

	[SaveableField(217)]
	public MobileParty MoveTargetParty;

	[SaveableField(218)]
	private AiBehavior _defaultBehavior;

	[SaveableField(1110)]
	private bool _isCurrentlyAtSea;

	[SaveableField(1094)]
	private bool _isTargetingPort;

	public bool StartTransitionNextFrameToExitFromPort;

	[SaveableField(1096)]
	private CampaignTime _navigationTransitionStartTime;

	[SaveableField(1093)]
	private NavigationType _desiredAiNavigationType;

	[CachedData]
	private int _locatorNodeIndex;

	[SaveableField(1120)]
	private Clan _actualClan;

	[SaveableField(1200)]
	private float _moraleDueToEvents;

	[CachedData]
	private float _totalWeightCarriedCache;

	[CachedData]
	private PathFaceRecord _lastNavigationFace;

	[CachedData]
	private MapWeatherModel.WeatherEventEffectOnTerrain _lastWeatherTerrainEffect;

	[CachedData]
	private bool _aiPathNotFound;

	[CachedData]
	public NavigationPath Path;

	[CachedData]
	public PathFaceRecord PathLastFace;

	[CachedData]
	private NavigationType _lastComputedPathNavigationType;

	[SaveableField(225)]
	private CampaignVec2 _position;

	private Vec2 _lastWind;

	[SaveableField(210)]
	private PartyComponent _partyComponent;

	public static MobileParty MainParty => Campaign.Current.MainParty;

	public static MBReadOnlyList<MobileParty> All => Campaign.Current.MobileParties;

	public static MBReadOnlyList<MobileParty> AllCaravanParties => Campaign.Current.CaravanParties;

	public static MBReadOnlyList<MobileParty> AllPatrolParties => Campaign.Current.PatrolParties;

	public static MBReadOnlyList<MobileParty> AllBanditParties => Campaign.Current.BanditParties;

	public static MBReadOnlyList<MobileParty> AllLordParties => Campaign.Current.LordParties;

	public static MBReadOnlyList<MobileParty> AllGarrisonParties => Campaign.Current.GarrisonParties;

	public static MBReadOnlyList<MobileParty> AllMilitiaParties => Campaign.Current.MilitiaParties;

	public static MBReadOnlyList<MobileParty> AllVillagerParties => Campaign.Current.VillagerParties;

	public static MBReadOnlyList<MobileParty> AllCustomParties => Campaign.Current.CustomParties;

	public static MBReadOnlyList<MobileParty> AllPartiesWithoutPartyComponent => Campaign.Current.PartiesWithoutPartyComponent;

	public static int Count => Campaign.Current.MobileParties.Count;

	public static MobileParty ConversationParty => Campaign.Current.ConversationManager.ConversationParty;

	public TextObject Name
	{
		get
		{
			if (!TextObject.IsNullOrEmpty(Party.CustomName))
			{
				return Party.CustomName;
			}
			if (_partyComponent != null)
			{
				return _partyComponent.Name;
			}
			Debug.FailedAssert("UnnamedMobileParty", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Party\\MobileParty.cs", "Name", 122);
			return new TextObject("{=!}unnamedMobileParty");
		}
	}

	[SaveableProperty(1002)]
	public Settlement LastVisitedSettlement { get; private set; }

	[SaveableProperty(1004)]
	public Vec2 Bearing { get; internal set; }

	public MBReadOnlyList<MobileParty> AttachedParties => _attachedParties;

	[SaveableProperty(1099)]
	public bool HasLandNavigationCapability { get; private set; } = true;

	public MBReadOnlyList<Ship> Ships => Party.Ships;

	public bool HasNavalNavigationCapability => Campaign.Current.Models.PartyNavigationModel.HasNavalNavigationCapability(this);

	[SaveableProperty(1009)]
	public float Aggressiveness { get; set; }

	public int PaymentLimit => _partyComponent?.WagePaymentLimit ?? Campaign.Current.Models.PartyWageModel.MaxWagePaymentLimit;

	public Banner Banner
	{
		get
		{
			if (Party.CustomBanner != null)
			{
				return Party.CustomBanner;
			}
			if (PartyComponent != null && PartyComponent.GetDefaultComponentBanner() != null)
			{
				return PartyComponent.GetDefaultComponentBanner();
			}
			if (MapFaction != null)
			{
				return MapFaction.Banner;
			}
			return null;
		}
	}

	[SaveableProperty(1005)]
	public Vec2 ArmyPositionAdder { get; private set; }

	public CampaignVec2 AiBehaviorTarget => Ai.BehaviorTarget;

	[SaveableProperty(1090)]
	public PartyObjective Objective { get; private set; }

	[CachedData]
	MobileParty ILocatable<MobileParty>.NextLocatable { get; set; }

	[SaveableProperty(1019)]
	public MobilePartyAi Ai { get; private set; }

	[SaveableProperty(1020)]
	public PartyBase Party { get; private set; }

	[SaveableProperty(1023)]
	public bool IsActive { get; set; }

	public bool IsInRaftState
	{
		get
		{
			return _isInRaftState;
		}
		set
		{
			if (_isInRaftState != value)
			{
				_isInRaftState = value;
				if (_isInRaftState)
				{
					Anchor.ResetPosition();
				}
				Party.SetVisualAsDirty();
			}
		}
	}

	public CampaignTime DisorganizedUntilTime => _disorganizedUntilTime;

	[CachedData]
	public float LastCalculatedBaseSpeed => _lastCalculatedBaseSpeedExplained.ResultNumber;

	[CachedData]
	public PartyThinkParams ThinkParamsCache { get; private set; }

	public float Speed
	{
		get
		{
			if (IsActive)
			{
				return CalculateSpeed();
			}
			Debug.FailedAssert("!IsActive", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Party\\MobileParty.cs", "Speed", 312);
			return 0f;
		}
	}

	public ExplainedNumber SpeedExplained
	{
		get
		{
			_lastCalculatedBaseSpeedExplained = Campaign.Current.Models.PartySpeedCalculatingModel.CalculateBaseSpeed(this, includeDescriptions: true);
			ExplainedNumber result = Campaign.Current.Models.PartySpeedCalculatingModel.CalculateFinalSpeed(this, _lastCalculatedBaseSpeedExplained);
			_lastCalculatedSpeed = result.ResultNumber;
			return result;
		}
	}

	public MobileParty ShortTermTargetParty => Ai.AiBehaviorPartyBase?.MobileParty;

	public Settlement ShortTermTargetSettlement => Ai.AiBehaviorPartyBase?.Settlement;

	public bool IsDisorganized => _isDisorganized;

	public bool IsCurrentlyUsedByAQuest => _isCurrentlyUsedByAQuest;

	[SaveableProperty(1050)]
	public AiBehavior ShortTermBehavior { get; internal set; }

	[SaveableProperty(1958)]
	public bool IsPartyTradeActive { get; private set; }

	public int PartyTradeGold
	{
		get
		{
			if (IsLordParty && LeaderHero != null)
			{
				return LeaderHero.Gold;
			}
			return _partyTradeGold;
		}
		set
		{
			if (IsLordParty && LeaderHero != null)
			{
				LeaderHero.Gold = TaleWorlds.Library.MathF.Max(value, 0);
			}
			else
			{
				_partyTradeGold = TaleWorlds.Library.MathF.Max(value, 0);
			}
		}
	}

	[SaveableProperty(1957)]
	public int PartyTradeTaxGold { get; private set; }

	[SaveableProperty(1960)]
	public CampaignTime StationaryStartTime { get; private set; }

	[CachedData]
	public int VersionNo { get; private set; }

	[SaveableProperty(1080)]
	public bool ShouldJoinPlayerBattles { get; set; }

	[SaveableProperty(1081)]
	public bool IsDisbanding { get; set; }

	public int RandomValue => Party.RandomValue;

	public NavigationType NavigationCapability
	{
		get
		{
			NavigationType navigationType = NavigationType.None;
			if (HasLandNavigationCapability)
			{
				navigationType |= NavigationType.Default;
			}
			if (HasNavalNavigationCapability)
			{
				navigationType |= NavigationType.Naval;
			}
			return navigationType;
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
			if (_isCurrentlyAtSea == value)
			{
				return;
			}
			_isCurrentlyAtSea = value;
			foreach (MobileParty attachedParty in _attachedParties)
			{
				attachedParty.IsCurrentlyAtSea = value;
			}
			UpdateVersionNo();
			CampaignEventDispatcher.Instance.OnMobilePartyNavigationStateChanged(this);
			Party.SetVisualAsDirty();
			if (!IsCurrentlyAtSea)
			{
				SetNavalVisualAsDirty();
			}
		}
	}

	[CachedData]
	public bool IsNavalVisualDirty { get; private set; }

	public bool IsTargetingPort
	{
		get
		{
			if (AttachedTo != null)
			{
				return AttachedTo.IsTargetingPort;
			}
			return _isTargetingPort;
		}
		private set
		{
			_isTargetingPort = value;
		}
	}

	[SaveableProperty(1092)]
	public AnchorPoint Anchor { get; private set; }

	public bool IsTransitionInProgress => NavigationTransitionStartTime != CampaignTime.Zero;

	[SaveableProperty(223)]
	public CampaignVec2 EndPositionForNavigationTransition { get; private set; }

	public CampaignTime NavigationTransitionStartTime
	{
		get
		{
			return _navigationTransitionStartTime;
		}
		private set
		{
			_navigationTransitionStartTime = value;
			if (_navigationTransitionStartTime != CampaignTime.Zero)
			{
				if (IsCurrentlyAtSea)
				{
					NavigationTransitionDuration = Campaign.Current.Models.PartyTransitionModel.GetTransitionTimeDisembarking(this);
				}
				else
				{
					NavigationTransitionDuration = Campaign.Current.Models.PartyTransitionModel.GetTransitionTimeForEmbarking(this);
				}
			}
			else
			{
				NavigationTransitionDuration = CampaignTime.Zero;
			}
		}
	}

	[SaveableProperty(1097)]
	public CampaignTime NavigationTransitionDuration { get; private set; } = CampaignTime.Zero;

	public NavigationType DesiredAiNavigationType
	{
		get
		{
			return _desiredAiNavigationType;
		}
		set
		{
			_desiredAiNavigationType = value;
		}
	}

	public Settlement CurrentSettlement
	{
		get
		{
			return _currentSettlement;
		}
		set
		{
			if (value == _currentSettlement)
			{
				return;
			}
			if (_currentSettlement != null)
			{
				_currentSettlement.RemoveMobileParty(this);
				if (!_currentSettlement.IsVillage)
				{
					ArmyPositionAdder = Vec2.Zero;
				}
			}
			_currentSettlement = value;
			if (_currentSettlement != null)
			{
				_currentSettlement.AddMobileParty(this);
				Position = (IsCurrentlyAtSea ? _currentSettlement.PortPosition : _currentSettlement.GatePosition);
				LastVisitedSettlement = value;
				EndPositionForNavigationTransition = Position;
			}
			else
			{
				EndPositionForNavigationTransition = CampaignVec2.Invalid;
			}
			foreach (MobileParty attachedParty in _attachedParties)
			{
				attachedParty.CurrentSettlement = value;
			}
			if (_currentSettlement != null && _currentSettlement.IsFortification)
			{
				ArmyPositionAdder = Vec2.Zero;
				Bearing = Vec2.Zero;
				foreach (MobileParty party in _currentSettlement.Parties)
				{
					party.Party.SetVisualAsDirty();
				}
			}
			Party.SetVisualAsDirty();
		}
	}

	public Settlement HomeSettlement
	{
		get
		{
			Settlement settlement = _customHomeSettlement;
			if (settlement == null)
			{
				PartyComponent partyComponent = _partyComponent;
				if (partyComponent == null)
				{
					return null;
				}
				settlement = partyComponent.HomeSettlement;
			}
			return settlement;
		}
	}

	public MobileParty AttachedTo
	{
		get
		{
			return _attachedTo;
		}
		set
		{
			if (_attachedTo != value)
			{
				SetAttachedToInternal(value);
			}
		}
	}

	public Army Army
	{
		get
		{
			return _army;
		}
		set
		{
			if (_army == value)
			{
				return;
			}
			UpdateVersionNo();
			Army army = _army;
			if (_army != null)
			{
				_army.OnRemovePartyInternal(this);
			}
			_army = value;
			if (value == null)
			{
				if (this == MainParty && Game.Current.GameStateManager.ActiveState is MapState)
				{
					((MapState)Game.Current.GameStateManager.ActiveState).OnLeaveArmy();
				}
				CampaignEventDispatcher.Instance.OnPartyLeftArmy(this, army);
			}
			else
			{
				_army.OnAddPartyInternal(this);
			}
		}
	}

	public BesiegerCamp BesiegerCamp
	{
		get
		{
			return _besiegerCamp;
		}
		set
		{
			if (_besiegerCamp == value || _besiegerCampResetStarted)
			{
				return;
			}
			_besiegerCampResetStarted = true;
			if (_besiegerCamp != null)
			{
				OnPartyLeftSiegeInternal();
			}
			_besiegerCamp = value;
			if (_besiegerCamp != null)
			{
				OnPartyJoinedSiegeInternal();
			}
			foreach (MobileParty attachedParty in _attachedParties)
			{
				attachedParty.BesiegerCamp = value;
			}
			Party.SetVisualAsDirty();
			_besiegerCampResetStarted = false;
		}
	}

	public AiBehavior DefaultBehavior
	{
		get
		{
			return _defaultBehavior;
		}
		private set
		{
			if (_defaultBehavior != value)
			{
				_defaultBehavior = value;
				Ai.DefaultBehaviorNeedsUpdate = true;
				RecalculateShortTermBehavior();
			}
		}
	}

	public Settlement TargetSettlement => _targetSettlement;

	public CampaignVec2 TargetPosition
	{
		get
		{
			return _targetPosition;
		}
		internal set
		{
			if (_targetPosition != value)
			{
				_targetPosition = value;
				Ai.DefaultBehaviorNeedsUpdate = true;
			}
		}
	}

	public MobileParty TargetParty
	{
		get
		{
			return _targetParty;
		}
		internal set
		{
			if (value != _targetParty)
			{
				_targetParty = value;
				Ai.DefaultBehaviorNeedsUpdate = true;
			}
		}
	}

	public Hero LeaderHero => PartyComponent?.Leader;

	[SaveableProperty(1070)]
	private Hero Scout { get; set; }

	[SaveableProperty(1072)]
	private Hero Engineer { get; set; }

	[SaveableProperty(1071)]
	private Hero Quartermaster { get; set; }

	[SaveableProperty(1073)]
	private Hero Surgeon { get; set; }

	[SaveableProperty(1076)]
	private Hero FirstMate { get; set; }

	[SaveableProperty(1077)]
	private Hero Navigator { get; set; }

	public Hero Owner => _partyComponent?.PartyOwner;

	public Hero EffectiveScout
	{
		get
		{
			if (Scout == null || Scout.PartyBelongedTo != this)
			{
				return LeaderHero;
			}
			return Scout;
		}
	}

	public Hero EffectiveQuartermaster
	{
		get
		{
			if (Quartermaster == null || Quartermaster.PartyBelongedTo != this)
			{
				return LeaderHero;
			}
			return Quartermaster;
		}
	}

	public Hero EffectiveEngineer
	{
		get
		{
			if (Engineer == null || Engineer.PartyBelongedTo != this)
			{
				return LeaderHero;
			}
			return Engineer;
		}
	}

	public Hero EffectiveSurgeon
	{
		get
		{
			if (Surgeon == null || Surgeon.PartyBelongedTo != this)
			{
				return LeaderHero;
			}
			return Surgeon;
		}
	}

	public Hero EffectiveFirstMate
	{
		get
		{
			if (FirstMate == null || FirstMate.PartyBelongedTo != this)
			{
				return LeaderHero;
			}
			return FirstMate;
		}
	}

	public Hero EffectiveNavigator
	{
		get
		{
			if (Navigator == null || Navigator.PartyBelongedTo != this)
			{
				return LeaderHero;
			}
			return Navigator;
		}
	}

	public float RecentEventsMorale
	{
		get
		{
			return _moraleDueToEvents;
		}
		set
		{
			_moraleDueToEvents = value;
			if (_moraleDueToEvents < -100f)
			{
				_moraleDueToEvents = -100f;
			}
			else if (_moraleDueToEvents > 100f)
			{
				_moraleDueToEvents = 100f;
			}
		}
	}

	public ExplainedNumber SeeingRangeExplanation => Campaign.Current.Models.MapVisibilityModel.GetPartySpottingRange(this, includeDescriptions: true);

	public int InventoryCapacity => (int)Campaign.Current.Models.InventoryCapacityModel.CalculateInventoryCapacity(this, IsCurrentlyAtSea).ResultNumber;

	public ExplainedNumber InventoryCapacityExplainedNumber => Campaign.Current.Models.InventoryCapacityModel.CalculateInventoryCapacity(this, IsCurrentlyAtSea, includeDescriptions: true);

	public float TotalWeightCarried
	{
		get
		{
			if (IsWeightCacheInvalid())
			{
				_partyWeightLastCheckVersionNo = GetVersionNoForWeightCalculation();
				_totalWeightCarriedCache = Campaign.Current.Models.InventoryCapacityModel.CalculateTotalWeightCarried(this, IsCurrentlyAtSea).ResultNumber;
			}
			return _totalWeightCarriedCache;
		}
	}

	public MapEventSide MapEventSide
	{
		get
		{
			return Party.MapEventSide;
		}
		set
		{
			Party.MapEventSide = value;
		}
	}

	public ExplainedNumber TotalWeightCarriedExplainedNumber => Campaign.Current.Models.InventoryCapacityModel.CalculateTotalWeightCarried(this, IsCurrentlyAtSea, includeDescriptions: true);

	public float Morale
	{
		get
		{
			float resultNumber = Campaign.Current.Models.PartyMoraleModel.GetEffectivePartyMorale(this).ResultNumber;
			return (resultNumber < 0f) ? 0f : ((resultNumber > 100f) ? 100f : resultNumber);
		}
	}

	public float FoodChange
	{
		get
		{
			ExplainedNumber baseConsumption = Campaign.Current.Models.MobilePartyFoodConsumptionModel.CalculateDailyBaseFoodConsumptionf(this);
			return Campaign.Current.Models.MobilePartyFoodConsumptionModel.CalculateDailyFoodConsumptionf(this, baseConsumption).ResultNumber;
		}
	}

	public float BaseFoodChange => Campaign.Current.Models.MobilePartyFoodConsumptionModel.CalculateDailyBaseFoodConsumptionf(this).ResultNumber;

	public Clan ActualClan
	{
		get
		{
			return _actualClan;
		}
		set
		{
			if (_actualClan != value)
			{
				if (_actualClan != null && value != null && PartyComponent is WarPartyComponent warPartyComponent)
				{
					warPartyComponent.OnClanChange(_actualClan, value);
				}
				_actualClan = value;
			}
		}
	}

	public ExplainedNumber FoodChangeExplained
	{
		get
		{
			ExplainedNumber baseConsumption = Campaign.Current.Models.MobilePartyFoodConsumptionModel.CalculateDailyBaseFoodConsumptionf(this, includeDescription: true);
			return Campaign.Current.Models.MobilePartyFoodConsumptionModel.CalculateDailyFoodConsumptionf(this, baseConsumption);
		}
	}

	public ExplainedNumber MoraleExplained => Campaign.Current.Models.PartyMoraleModel.GetEffectivePartyMorale(this, includeDescription: true);

	int ILocatable<MobileParty>.LocatorNodeIndex
	{
		get
		{
			return _locatorNodeIndex;
		}
		set
		{
			_locatorNodeIndex = value;
		}
	}

	[CachedData]
	public PathFaceRecord CurrentNavigationFace => Position.Face;

	[CachedData]
	public int PathBegin { get; private set; }

	[CachedData]
	public bool ForceAiNoPathMode { get; set; }

	public Vec2 EventPositionAdder
	{
		get
		{
			return _eventPositionAdder;
		}
		set
		{
			_eventPositionAdder = value;
		}
	}

	public bool IsVisible
	{
		get
		{
			return _isVisible;
		}
		set
		{
			if (_isVisible == value)
			{
				return;
			}
			_isVisible = value;
			Party.OnVisibilityChanged(value);
			if (!IsCurrentlyAtSea || IsTransitionInProgress)
			{
				SiegeEvent siegeEvent = SiegeEvent;
				if (siegeEvent == null || !siegeEvent.IsBlockadeActive)
				{
					return;
				}
			}
			SetNavalVisualAsDirty();
		}
	}

	public CampaignVec2 Position
	{
		get
		{
			return _position;
		}
		set
		{
			if (_position != value)
			{
				_position = value;
				Campaign.Current.MobilePartyLocator.UpdateLocator(this);
			}
		}
	}

	public bool IsInspected
	{
		get
		{
			if (Army == null || Army != MainParty.Army)
			{
				return _isInspected;
			}
			return true;
		}
		set
		{
			_isInspected = value;
		}
	}

	public Vec2 GetPosition2D => Position.ToVec2();

	public int TotalWage => (int)Campaign.Current.Models.PartyWageModel.GetTotalWage(this, MemberRoster).ResultNumber;

	public ExplainedNumber TotalWageExplained => Campaign.Current.Models.PartyWageModel.GetTotalWage(this, MemberRoster, includeDescriptions: true);

	public MapEvent MapEvent => Party.MapEvent;

	public TroopRoster MemberRoster => Party.MemberRoster;

	public TroopRoster PrisonRoster => Party.PrisonRoster;

	public ItemRoster ItemRoster => Party.ItemRoster;

	public bool IsMainParty => this == MainParty;

	public IFaction MapFaction
	{
		get
		{
			if (ActualClan != null)
			{
				return ActualClan.MapFaction;
			}
			if (Party.Owner != null)
			{
				if (Party.Owner == Hero.MainHero)
				{
					return Party.Owner.MapFaction;
				}
				if (Party.Owner.IsNotable)
				{
					return Party.Owner.HomeSettlement.MapFaction;
				}
				if ((IsMilitia || IsGarrison || IsVillager || IsPatrolParty) && HomeSettlement?.OwnerClan != null)
				{
					return HomeSettlement.OwnerClan.MapFaction;
				}
				if (IsCaravan || IsBanditBossParty)
				{
					return Party.Owner.MapFaction;
				}
				if (_isCurrentlyUsedByAQuest && Party.Owner != null)
				{
					return Party.Owner.MapFaction;
				}
				return (LeaderHero != null) ? LeaderHero.MapFaction : null;
			}
			if (HomeSettlement != null)
			{
				return HomeSettlement.OwnerClan.MapFaction;
			}
			return (LeaderHero != null) ? LeaderHero.MapFaction : null;
		}
	}

	public TextObject ArmyName
	{
		get
		{
			if (Army == null || Army.LeaderParty != this)
			{
				return Name;
			}
			return Army.Name;
		}
	}

	public SiegeEvent SiegeEvent => BesiegerCamp?.SiegeEvent;

	public float Food => (float)Party.RemainingFoodPercentage * 0.01f + (float)TotalFoodAtInventory;

	public int TotalFoodAtInventory => ItemRoster.TotalFood;

	public float SeeingRange => Campaign.Current.Models.MapVisibilityModel.GetPartySpottingRange(this).ResultNumber;

	public Settlement BesiegedSettlement => BesiegerCamp?.SiegeEvent.BesiegedSettlement;

	public bool IsEngaging
	{
		get
		{
			if (DefaultBehavior != AiBehavior.EngageParty)
			{
				return ShortTermBehavior == AiBehavior.EngageParty;
			}
			return true;
		}
	}

	internal bool IsCurrentlyEngagingSettlement
	{
		get
		{
			if (ShortTermBehavior != AiBehavior.GoToSettlement && ShortTermBehavior != AiBehavior.RaidSettlement && ShortTermBehavior != AiBehavior.AssaultSettlement)
			{
				return ShortTermBehavior == AiBehavior.FleeToGate;
			}
			return true;
		}
	}

	internal bool IsCurrentlyEngagingParty => ShortTermBehavior == AiBehavior.EngageParty;

	public float PartySizeRatio
	{
		get
		{
			int versionNo = Party.MemberRoster.VersionNo;
			float cachedPartySizeRatio = _cachedPartySizeRatio;
			if (_partySizeRatioLastCheckVersion != versionNo || this == MainParty)
			{
				_partySizeRatioLastCheckVersion = versionNo;
				_cachedPartySizeRatio = (float)Party.NumberOfAllMembers / (float)Party.PartySizeLimit;
				cachedPartySizeRatio = _cachedPartySizeRatio;
			}
			return cachedPartySizeRatio;
		}
	}

	public Vec2 VisualPosition2DWithoutError => Position.ToVec2() + EventPositionAdder + ArmyPositionAdder;

	public bool IsMoving
	{
		get
		{
			if (IsMainParty)
			{
				if (!IsTransitionInProgress)
				{
					return !Campaign.Current.IsMainPartyWaiting;
				}
				return false;
			}
			if (IsActive && !IsTransitionInProgress && CurrentSettlement == null && MapEvent == null && BesiegedSettlement == null)
			{
				return Position != MoveTargetPoint;
			}
			return false;
		}
	}

	public bool ShouldBeIgnored
	{
		get
		{
			if (!_ignoredUntilTime.IsFuture)
			{
				return IsInRaftState;
			}
			return true;
		}
	}

	public VillagerPartyComponent VillagerPartyComponent => _partyComponent as VillagerPartyComponent;

	public CaravanPartyComponent CaravanPartyComponent => _partyComponent as CaravanPartyComponent;

	public WarPartyComponent WarPartyComponent => _partyComponent as WarPartyComponent;

	public BanditPartyComponent BanditPartyComponent => _partyComponent as BanditPartyComponent;

	public PatrolPartyComponent PatrolPartyComponent => _partyComponent as PatrolPartyComponent;

	public LordPartyComponent LordPartyComponent => _partyComponent as LordPartyComponent;

	public GarrisonPartyComponent GarrisonPartyComponent => _partyComponent as GarrisonPartyComponent;

	public PartyComponent PartyComponent => _partyComponent;

	[CachedData]
	public bool IsMilitia { get; private set; }

	[CachedData]
	public bool IsLordParty { get; private set; }

	[CachedData]
	public bool IsVillager { get; private set; }

	[CachedData]
	public bool IsCaravan { get; private set; }

	[CachedData]
	public bool IsPatrolParty { get; private set; }

	[CachedData]
	public bool IsGarrison { get; private set; }

	[CachedData]
	public bool IsCustomParty { get; private set; }

	[CachedData]
	public bool IsBandit { get; private set; }

	public bool IsBanditBossParty
	{
		get
		{
			if (IsBandit)
			{
				return BanditPartyComponent.IsBossParty;
			}
			return false;
		}
	}

	public bool AvoidHostileActions
	{
		get
		{
			if (_partyComponent != null)
			{
				return _partyComponent.AvoidHostileActions;
			}
			return false;
		}
	}

	internal static void AutoGeneratedStaticCollectObjectsMobileParty(object o, List<object> collectedObjects)
	{
		((MobileParty)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		CampaignVec2.AutoGeneratedStaticCollectObjectsCampaignVec2(NextTargetPosition, collectedObjects);
		CampaignVec2.AutoGeneratedStaticCollectObjectsCampaignVec2(MoveTargetPoint, collectedObjects);
		collectedObjects.Add(MoveTargetParty);
		collectedObjects.Add(_currentSettlement);
		collectedObjects.Add(_attachedTo);
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(_disorganizedUntilTime, collectedObjects);
		collectedObjects.Add(_besiegerCamp);
		collectedObjects.Add(_targetParty);
		collectedObjects.Add(_targetSettlement);
		CampaignVec2.AutoGeneratedStaticCollectObjectsCampaignVec2(_targetPosition, collectedObjects);
		collectedObjects.Add(_customHomeSettlement);
		collectedObjects.Add(_army);
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(_ignoredUntilTime, collectedObjects);
		CampaignVec2.AutoGeneratedStaticCollectObjectsCampaignVec2(_pathLastPosition, collectedObjects);
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(_navigationTransitionStartTime, collectedObjects);
		collectedObjects.Add(_actualClan);
		CampaignVec2.AutoGeneratedStaticCollectObjectsCampaignVec2(_position, collectedObjects);
		collectedObjects.Add(_partyComponent);
		collectedObjects.Add(LastVisitedSettlement);
		collectedObjects.Add(Ai);
		collectedObjects.Add(Party);
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(StationaryStartTime, collectedObjects);
		collectedObjects.Add(Anchor);
		CampaignVec2.AutoGeneratedStaticCollectObjectsCampaignVec2(EndPositionForNavigationTransition, collectedObjects);
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(NavigationTransitionDuration, collectedObjects);
		collectedObjects.Add(Scout);
		collectedObjects.Add(Engineer);
		collectedObjects.Add(Quartermaster);
		collectedObjects.Add(Surgeon);
		collectedObjects.Add(FirstMate);
		collectedObjects.Add(Navigator);
	}

	internal static object AutoGeneratedGetMemberValueLastVisitedSettlement(object o)
	{
		return ((MobileParty)o).LastVisitedSettlement;
	}

	internal static object AutoGeneratedGetMemberValueBearing(object o)
	{
		return ((MobileParty)o).Bearing;
	}

	internal static object AutoGeneratedGetMemberValueHasLandNavigationCapability(object o)
	{
		return ((MobileParty)o).HasLandNavigationCapability;
	}

	internal static object AutoGeneratedGetMemberValueAggressiveness(object o)
	{
		return ((MobileParty)o).Aggressiveness;
	}

	internal static object AutoGeneratedGetMemberValueArmyPositionAdder(object o)
	{
		return ((MobileParty)o).ArmyPositionAdder;
	}

	internal static object AutoGeneratedGetMemberValueObjective(object o)
	{
		return ((MobileParty)o).Objective;
	}

	internal static object AutoGeneratedGetMemberValueAi(object o)
	{
		return ((MobileParty)o).Ai;
	}

	internal static object AutoGeneratedGetMemberValueParty(object o)
	{
		return ((MobileParty)o).Party;
	}

	internal static object AutoGeneratedGetMemberValueIsActive(object o)
	{
		return ((MobileParty)o).IsActive;
	}

	internal static object AutoGeneratedGetMemberValueShortTermBehavior(object o)
	{
		return ((MobileParty)o).ShortTermBehavior;
	}

	internal static object AutoGeneratedGetMemberValueIsPartyTradeActive(object o)
	{
		return ((MobileParty)o).IsPartyTradeActive;
	}

	internal static object AutoGeneratedGetMemberValuePartyTradeTaxGold(object o)
	{
		return ((MobileParty)o).PartyTradeTaxGold;
	}

	internal static object AutoGeneratedGetMemberValueStationaryStartTime(object o)
	{
		return ((MobileParty)o).StationaryStartTime;
	}

	internal static object AutoGeneratedGetMemberValueShouldJoinPlayerBattles(object o)
	{
		return ((MobileParty)o).ShouldJoinPlayerBattles;
	}

	internal static object AutoGeneratedGetMemberValueIsDisbanding(object o)
	{
		return ((MobileParty)o).IsDisbanding;
	}

	internal static object AutoGeneratedGetMemberValueAnchor(object o)
	{
		return ((MobileParty)o).Anchor;
	}

	internal static object AutoGeneratedGetMemberValueEndPositionForNavigationTransition(object o)
	{
		return ((MobileParty)o).EndPositionForNavigationTransition;
	}

	internal static object AutoGeneratedGetMemberValueNavigationTransitionDuration(object o)
	{
		return ((MobileParty)o).NavigationTransitionDuration;
	}

	internal static object AutoGeneratedGetMemberValueScout(object o)
	{
		return ((MobileParty)o).Scout;
	}

	internal static object AutoGeneratedGetMemberValueEngineer(object o)
	{
		return ((MobileParty)o).Engineer;
	}

	internal static object AutoGeneratedGetMemberValueQuartermaster(object o)
	{
		return ((MobileParty)o).Quartermaster;
	}

	internal static object AutoGeneratedGetMemberValueSurgeon(object o)
	{
		return ((MobileParty)o).Surgeon;
	}

	internal static object AutoGeneratedGetMemberValueFirstMate(object o)
	{
		return ((MobileParty)o).FirstMate;
	}

	internal static object AutoGeneratedGetMemberValueNavigator(object o)
	{
		return ((MobileParty)o).Navigator;
	}

	internal static object AutoGeneratedGetMemberValueHasUnpaidWages(object o)
	{
		return ((MobileParty)o).HasUnpaidWages;
	}

	internal static object AutoGeneratedGetMemberValueIsInfoHidden(object o)
	{
		return ((MobileParty)o).IsInfoHidden;
	}

	internal static object AutoGeneratedGetMemberValueAverageFleeTargetDirection(object o)
	{
		return ((MobileParty)o).AverageFleeTargetDirection;
	}

	internal static object AutoGeneratedGetMemberValueNextTargetPosition(object o)
	{
		return ((MobileParty)o).NextTargetPosition;
	}

	internal static object AutoGeneratedGetMemberValueMoveTargetPoint(object o)
	{
		return ((MobileParty)o).MoveTargetPoint;
	}

	internal static object AutoGeneratedGetMemberValuePartyMoveMode(object o)
	{
		return ((MobileParty)o).PartyMoveMode;
	}

	internal static object AutoGeneratedGetMemberValueMoveTargetParty(object o)
	{
		return ((MobileParty)o).MoveTargetParty;
	}

	internal static object AutoGeneratedGetMemberValue_currentSettlement(object o)
	{
		return ((MobileParty)o)._currentSettlement;
	}

	internal static object AutoGeneratedGetMemberValue_attachedTo(object o)
	{
		return ((MobileParty)o)._attachedTo;
	}

	internal static object AutoGeneratedGetMemberValue_eventPositionAdder(object o)
	{
		return ((MobileParty)o)._eventPositionAdder;
	}

	internal static object AutoGeneratedGetMemberValue_isInRaftState(object o)
	{
		return ((MobileParty)o)._isInRaftState;
	}

	internal static object AutoGeneratedGetMemberValue_isVisible(object o)
	{
		return ((MobileParty)o)._isVisible;
	}

	internal static object AutoGeneratedGetMemberValue_isInspected(object o)
	{
		return ((MobileParty)o)._isInspected;
	}

	internal static object AutoGeneratedGetMemberValue_disorganizedUntilTime(object o)
	{
		return ((MobileParty)o)._disorganizedUntilTime;
	}

	internal static object AutoGeneratedGetMemberValue_besiegerCamp(object o)
	{
		return ((MobileParty)o)._besiegerCamp;
	}

	internal static object AutoGeneratedGetMemberValue_targetParty(object o)
	{
		return ((MobileParty)o)._targetParty;
	}

	internal static object AutoGeneratedGetMemberValue_targetSettlement(object o)
	{
		return ((MobileParty)o)._targetSettlement;
	}

	internal static object AutoGeneratedGetMemberValue_targetPosition(object o)
	{
		return ((MobileParty)o)._targetPosition;
	}

	internal static object AutoGeneratedGetMemberValue_customHomeSettlement(object o)
	{
		return ((MobileParty)o)._customHomeSettlement;
	}

	internal static object AutoGeneratedGetMemberValue_army(object o)
	{
		return ((MobileParty)o)._army;
	}

	internal static object AutoGeneratedGetMemberValue_isCurrentlyUsedByAQuest(object o)
	{
		return ((MobileParty)o)._isCurrentlyUsedByAQuest;
	}

	internal static object AutoGeneratedGetMemberValue_partyTradeGold(object o)
	{
		return ((MobileParty)o)._partyTradeGold;
	}

	internal static object AutoGeneratedGetMemberValue_ignoredUntilTime(object o)
	{
		return ((MobileParty)o)._ignoredUntilTime;
	}

	internal static object AutoGeneratedGetMemberValue_pathMode(object o)
	{
		return ((MobileParty)o)._pathMode;
	}

	internal static object AutoGeneratedGetMemberValue_pathLastPosition(object o)
	{
		return ((MobileParty)o)._pathLastPosition;
	}

	internal static object AutoGeneratedGetMemberValue_formationPosition(object o)
	{
		return ((MobileParty)o)._formationPosition;
	}

	internal static object AutoGeneratedGetMemberValue_defaultBehavior(object o)
	{
		return ((MobileParty)o)._defaultBehavior;
	}

	internal static object AutoGeneratedGetMemberValue_isCurrentlyAtSea(object o)
	{
		return ((MobileParty)o)._isCurrentlyAtSea;
	}

	internal static object AutoGeneratedGetMemberValue_isTargetingPort(object o)
	{
		return ((MobileParty)o)._isTargetingPort;
	}

	internal static object AutoGeneratedGetMemberValue_navigationTransitionStartTime(object o)
	{
		return ((MobileParty)o)._navigationTransitionStartTime;
	}

	internal static object AutoGeneratedGetMemberValue_desiredAiNavigationType(object o)
	{
		return ((MobileParty)o)._desiredAiNavigationType;
	}

	internal static object AutoGeneratedGetMemberValue_actualClan(object o)
	{
		return ((MobileParty)o)._actualClan;
	}

	internal static object AutoGeneratedGetMemberValue_moraleDueToEvents(object o)
	{
		return ((MobileParty)o)._moraleDueToEvents;
	}

	internal static object AutoGeneratedGetMemberValue_position(object o)
	{
		return ((MobileParty)o)._position;
	}

	internal static object AutoGeneratedGetMemberValue_partyComponent(object o)
	{
		return ((MobileParty)o)._partyComponent;
	}

	public void SetLandNavigationAccess(bool access)
	{
		HasLandNavigationCapability = access;
	}

	public override TextObject GetName()
	{
		return Name;
	}

	public bool HasLimitedWage()
	{
		return PaymentLimit != Campaign.Current.Models.PartyWageModel.MaxWagePaymentLimit;
	}

	public int GetAvailableWageBudget()
	{
		return PaymentLimit - TotalWage - 5;
	}

	public bool IsWageLimitExceeded()
	{
		if (HasLimitedWage())
		{
			return PaymentLimit < TotalWage;
		}
		return false;
	}

	public void SetWagePaymentLimit(int newLimit)
	{
		PartyComponent?.SetWagePaymentLimit(newLimit);
	}

	public void SetNavalVisualAsDirty()
	{
		IsNavalVisualDirty = true;
	}

	public void OnNavalVisualsUpdated()
	{
		IsNavalVisualDirty = false;
	}

	internal void InitializeNavigationTransitionParallel(CampaignVec2 transitionStartPosition, CampaignVec2 transitionEndPosition, ref int gridChangeCount, ref MobileParty[] gridChangeMobilePartyList)
	{
		NavigationTransitionStartTime = CampaignTime.Now;
		Party.SetVisualAsDirty();
		if (CurrentSettlement == null)
		{
			EndPositionForNavigationTransition = transitionEndPosition;
			SetPositionParallel(in transitionStartPosition, ref gridChangeCount, ref gridChangeMobilePartyList);
		}
		foreach (MobileParty attachedParty in _attachedParties)
		{
			attachedParty.InitializeNavigationTransitionParallel(transitionStartPosition, transitionEndPosition, ref gridChangeCount, ref gridChangeMobilePartyList);
		}
	}

	public void SetSailAtPosition(CampaignVec2 position)
	{
		IsCurrentlyAtSea = true;
		Position = position;
		TargetPosition = position;
		Anchor.ResetPosition();
		for (int i = 0; i < AttachedParties.Count; i++)
		{
			AttachedParties[i].Position = Position;
			AttachedParties[i].Anchor.ResetPosition();
		}
	}

	public void DisembarkToPosition(CampaignVec2 position)
	{
		IsCurrentlyAtSea = false;
		Position = position;
		TargetPosition = position;
		for (int i = 0; i < AttachedParties.Count; i++)
		{
			AttachedParties[i].Position = Position;
		}
	}

	public void CancelNavigationTransition()
	{
		CancelNavigationTransitionParallel();
	}

	private void CancelNavigationTransitionParallel()
	{
		NavigationTransitionStartTime = CampaignTime.Zero;
		EndPositionForNavigationTransition = CampaignVec2.Invalid;
		Party.SetVisualAsDirty();
		foreach (MobileParty attachedParty in _attachedParties)
		{
			attachedParty.CancelNavigationTransitionParallel();
		}
	}

	internal void FinishNavigationTransitionInternal()
	{
		bool flag = (_isCurrentlyAtSea = !IsCurrentlyAtSea);
		if (Path.Size > PathBegin + 1)
		{
			PathBegin++;
		}
		if (flag || IsInRaftState)
		{
			Anchor.ResetPosition();
		}
		else if (Ships.Any())
		{
			Anchor.SetPosition(Position);
		}
		NavigationTransitionStartTime = CampaignTime.Zero;
		CampaignVec2 position = ((CurrentSettlement == null) ? ((Army != null && AttachedTo != null) ? Army.LeaderParty.EndPositionForNavigationTransition : EndPositionForNavigationTransition) : ((!flag) ? CurrentSettlement.GatePosition : CurrentSettlement.PortPosition));
		_position = position;
		if (IsInRaftState)
		{
			RaftStateChangeAction.DeactivateRaftStateForParty(this);
		}
		foreach (MobileParty attachedParty in _attachedParties)
		{
			attachedParty.FinishNavigationTransitionInternal();
			attachedParty.ArmyPositionAdder = Vec2.Zero;
		}
		ComputePath(MoveTargetPoint, NavigationCapability, !IsFleeing());
		UpdateVersionNo();
		CampaignEventDispatcher.Instance.OnMobilePartyNavigationStateChanged(this);
		Party.SetVisualAsDirty();
		if (!IsCurrentlyAtSea)
		{
			SetNavalVisualAsDirty();
		}
		Anchor.SetLastUsedDisembarkPosition(EndPositionForNavigationTransition);
		EndPositionForNavigationTransition = CampaignVec2.Invalid;
	}

	public void ChangeIsCurrentlyAtSeaCheat()
	{
		IsCurrentlyAtSea = !IsCurrentlyAtSea;
		Anchor.ResetPosition();
		for (int i = 0; i < AttachedParties.Count; i++)
		{
			AttachedParties[i].Anchor.ResetPosition();
		}
	}

	public void SetCustomHomeSettlement(Settlement customHomeSettlement)
	{
		_customHomeSettlement = customHomeSettlement;
	}

	private void SetAttachedToInternal(MobileParty value)
	{
		if (_attachedTo != null)
		{
			if (IsTransitionInProgress)
			{
				CancelNavigationTransitionParallel();
			}
			_attachedTo.RemoveAttachedPartyInternal(this);
			if (Party.MapEventSide != null && IsActive)
			{
				Party.MapEventSide.HandleMapEventEndForPartyInternal(Party);
				Party.MapEventSide = null;
			}
			if (BesiegerCamp != null)
			{
				BesiegerCamp = null;
			}
			OnAttachedToRemoved();
		}
		_attachedTo = value;
		if (_attachedTo != null)
		{
			_attachedTo.AddAttachedPartyInternal(this);
			Party.MapEventSide = _attachedTo.Party.MapEventSide;
			BesiegerCamp = _attachedTo.BesiegerCamp;
			CurrentSettlement = _attachedTo.CurrentSettlement;
			if (_attachedTo.IsTransitionInProgress)
			{
				NavigationTransitionStartTime = CampaignTime.Now;
			}
			else if (IsTransitionInProgress)
			{
				CancelNavigationTransitionParallel();
			}
			if (IsCurrentlyAtSea != _attachedTo.IsCurrentlyAtSea)
			{
				IsCurrentlyAtSea = _attachedTo.IsCurrentlyAtSea;
			}
		}
		Party.SetVisualAsDirty();
	}

	private void AddAttachedPartyInternal(MobileParty mobileParty)
	{
		if (_attachedParties == null)
		{
			_attachedParties = new MBList<MobileParty>();
		}
		_attachedParties.Add(mobileParty);
		if (CampaignEventDispatcher.Instance != null)
		{
			CampaignEventDispatcher.Instance.OnPartyAttachedAnotherParty(mobileParty);
		}
	}

	private void RemoveAttachedPartyInternal(MobileParty mobileParty)
	{
		_attachedParties.Remove(mobileParty);
	}

	private void OnAttachedToRemoved()
	{
		ArmyPositionAdder = Vec2.Zero;
		SetMoveModeHold();
	}

	public void SetTargetSettlement(Settlement settlement, bool isTargetingPort)
	{
		if (settlement != _targetSettlement || IsTargetingPort != isTargetingPort)
		{
			_targetSettlement = settlement;
			if (settlement != null)
			{
				MoveTargetPoint = (isTargetingPort ? settlement.PortPosition : settlement.GatePosition);
			}
			Ai.DefaultBehaviorNeedsUpdate = true;
			IsTargetingPort = isTargetingPort;
		}
	}

	public MobileParty()
	{
		_isVisible = false;
		IsActive = true;
		_isCurrentlyUsedByAQuest = false;
		Party = new PartyBase(this);
		Anchor = new AnchorPoint(this);
		InitMembers();
		InitCached();
		Initialize();
	}

	public void SetPartyScout(Hero hero)
	{
		if (!CanAssignMoreRoleToHero(hero))
		{
			RemoveOnePartyRoleOfHero(hero);
		}
		Scout = hero;
	}

	public void SetPartyQuartermaster(Hero hero)
	{
		if (!CanAssignMoreRoleToHero(hero))
		{
			RemoveOnePartyRoleOfHero(hero);
		}
		Quartermaster = hero;
	}

	public void SetPartyEngineer(Hero hero)
	{
		if (!CanAssignMoreRoleToHero(hero))
		{
			RemoveOnePartyRoleOfHero(hero);
		}
		Engineer = hero;
	}

	public void SetPartySurgeon(Hero hero)
	{
		if (!CanAssignMoreRoleToHero(hero))
		{
			RemoveOnePartyRoleOfHero(hero);
		}
		Surgeon = hero;
	}

	public void SetPartyFirstMate(Hero hero)
	{
		if (!CanAssignMoreRoleToHero(hero))
		{
			RemoveOnePartyRoleOfHero(hero);
		}
		FirstMate = hero;
	}

	public void SetPartyNavigator(Hero hero)
	{
		if (!CanAssignMoreRoleToHero(hero))
		{
			RemoveOnePartyRoleOfHero(hero);
		}
		Navigator = hero;
	}

	private bool CanAssignMoreRoleToHero(Hero hero)
	{
		int num = 0;
		if (Quartermaster == hero)
		{
			num++;
		}
		if (Scout == hero)
		{
			num++;
		}
		if (Surgeon == hero)
		{
			num++;
		}
		if (Engineer == hero)
		{
			num++;
		}
		if (FirstMate == hero)
		{
			num++;
		}
		if (Navigator == hero)
		{
			num++;
		}
		return num < Campaign.Current.Models.ClanMemberPartyRoleModel.MaximumPartyRoleAssignmentCount;
	}

	internal void StartUp()
	{
		NextTargetPosition = Position;
		PathLastFace = PathFaceRecord.NullFaceRecord;
		MoveTargetPoint = Position;
		ForceAiNoPathMode = false;
	}

	[LateLoadInitializationCallback]
	private void OnLateLoad(MetaData metaData, ObjectLoadData objectLoadData)
	{
		if (MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.0")))
		{
			TextObject textObject = (TextObject)objectLoadData.GetMemberValueBySaveId(1021);
			if (!TextObject.IsNullOrEmpty(textObject))
			{
				Party.SetCustomName(textObject);
			}
		}
		if (MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.0")))
		{
			Vec2 pos = (Vec2)objectLoadData.GetMemberValueBySaveId(1100);
			_position = new CampaignVec2(pos, isOnLand: true);
		}
		if (MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.1.0"))
		{
			PartyBase partyBase = (PartyBase)objectLoadData.GetMemberValueBySaveId(1052);
			IInteractablePoint interactablePoint = null;
			if (partyBase != null && (partyBase.IsSettlement || partyBase.IsMobile))
			{
				interactablePoint = partyBase;
			}
			object memberValueBySaveId = objectLoadData.GetMemberValueBySaveId(1036);
			object memberValueBySaveId2 = objectLoadData.GetMemberValueBySaveId(1037);
			object memberValueBySaveId3 = objectLoadData.GetMemberValueBySaveId(1064);
			object memberValueBySaveId4 = objectLoadData.GetMemberValueBySaveId(1065);
			object memberValueBySaveId5 = objectLoadData.GetMemberValueBySaveId(1047);
			object memberValueBySaveId6 = objectLoadData.GetMemberValueBySaveId(1051);
			object memberValueBySaveId7 = objectLoadData.GetMemberValueBySaveId(1038);
			object memberValueBySaveId8 = objectLoadData.GetMemberValueBySaveId(1039);
			object memberValueBySaveId9 = objectLoadData.GetMemberValueBySaveId(1055);
			object memberValueBySaveId10 = objectLoadData.GetMemberValueBySaveId(1054);
			object memberValueBySaveId11 = objectLoadData.GetMemberValueBySaveId(1062);
			object memberValueBySaveId12 = objectLoadData.GetMemberValueBySaveId(1061);
			object fieldValueBySaveId = objectLoadData.GetFieldValueBySaveId(1070);
			object memberValueBySaveId13 = objectLoadData.GetMemberValueBySaveId(1022);
			object obj = interactablePoint ?? objectLoadData.GetMemberValueBySaveId(1056);
			object memberValueBySaveId14 = objectLoadData.GetMemberValueBySaveId(1074);
			if (memberValueBySaveId != null)
			{
				IInteractablePoint oldAiBehaviorMapEntity = null;
				if (obj != null)
				{
					oldAiBehaviorMapEntity = ((!(obj is Settlement settlement)) ? ((!(obj is MobileParty mobileParty)) ? ((IInteractablePoint)obj) : mobileParty.Party) : settlement.Party);
				}
				Ai.InitializeForOldSaves((float)memberValueBySaveId, (float)memberValueBySaveId2, (CampaignTime)memberValueBySaveId3, (int)memberValueBySaveId4, (AiBehavior)memberValueBySaveId5, (Vec2)memberValueBySaveId6, (bool)memberValueBySaveId7, (bool)memberValueBySaveId8, (memberValueBySaveId9 != null) ? ((MoveModeType)memberValueBySaveId9) : MoveModeType.Hold, (MobileParty)memberValueBySaveId10, (Vec2)memberValueBySaveId11, (Vec2)memberValueBySaveId12, (Vec2)fieldValueBySaveId, (Vec2)memberValueBySaveId13, oldAiBehaviorMapEntity, ((CampaignTime?)memberValueBySaveId14) ?? CampaignTime.Never);
			}
			UpdatePartyComponentFlags();
			if (IsGarrison || IsLordParty)
			{
				object memberValueBySaveId15 = objectLoadData.GetMemberValueBySaveId(1010);
				if (memberValueBySaveId15 != null)
				{
					SetWagePaymentLimit((int)memberValueBySaveId15);
				}
			}
		}
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.0")))
		{
			object memberValueBySaveId16 = objectLoadData.GetMemberValueBySaveId(1100);
			_position = new CampaignVec2((Vec2)memberValueBySaveId16, isOnLand: true);
			object memberValueBySaveId17 = objectLoadData.GetMemberValueBySaveId(1053);
			if (memberValueBySaveId17 != null)
			{
				_targetPosition = new CampaignVec2((Vec2)memberValueBySaveId17, isOnLand: true);
			}
			memberValueBySaveId17 = objectLoadData.GetMemberValueBySaveId(212);
			if (memberValueBySaveId17 != null)
			{
				_pathLastPosition = new CampaignVec2((Vec2)memberValueBySaveId17, isOnLand: true);
			}
			memberValueBySaveId17 = objectLoadData.GetMemberValueBySaveId(213);
			if (memberValueBySaveId17 != null)
			{
				NextTargetPosition = new CampaignVec2((Vec2)memberValueBySaveId17, isOnLand: true);
			}
			memberValueBySaveId17 = objectLoadData.GetMemberValueBySaveId(214);
			if (memberValueBySaveId17 != null)
			{
				MoveTargetPoint = new CampaignVec2((Vec2)memberValueBySaveId17, isOnLand: true);
			}
		}
	}

	public override string ToString()
	{
		return base.StringId + ":" + Party.Index + " Name: " + Name.ToString();
	}

	internal void ValidateSpeed()
	{
		CalculateSpeed();
	}

	public void ChangePartyLeader(Hero newLeader)
	{
		PartyComponent.ChangePartyLeader(newLeader);
	}

	public void OnPartyInteraction(MobileParty engagingParty)
	{
		MobileParty mobileParty = this;
		if (mobileParty.AttachedTo != null && engagingParty != mobileParty.AttachedTo)
		{
			mobileParty = mobileParty.AttachedTo;
		}
		bool flag = false;
		if (mobileParty.CurrentSettlement != null)
		{
			if (mobileParty.MapEvent != null)
			{
				flag = mobileParty.MapEvent.MapEventSettlement == mobileParty.CurrentSettlement && (mobileParty.MapEvent.AttackerSide.LeaderParty.MapFaction == engagingParty.MapFaction || mobileParty.MapEvent.DefenderSide.LeaderParty.MapFaction == engagingParty.MapFaction);
			}
		}
		else
		{
			flag = engagingParty != MainParty || !mobileParty.IsEngaging || mobileParty.ShortTermTargetParty != MainParty;
		}
		if (flag)
		{
			if (engagingParty == MainParty)
			{
				(Game.Current.GameStateManager.ActiveState as MapState)?.OnMainPartyEncounter();
			}
			EncounterManager.StartPartyEncounter(engagingParty.Party, mobileParty.Party);
		}
	}

	public void SetPositionAfterMapChange(CampaignVec2 newPosition)
	{
		Position = newPosition;
	}

	public void RemovePartyLeader()
	{
		PartyComponent.ChangePartyLeader(null);
	}

	public void CheckPositionsForMapChangeAndUpdateIfNeeded()
	{
		if (!Position.IsValid() || IsCurrentlyAtSea == Position.IsOnLand)
		{
			CampaignVec2 closestNavMeshFaceCenterPositionForPosition = NavigationHelper.GetClosestNavMeshFaceCenterPositionForPosition(Position, Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType((!IsCurrentlyAtSea) ? NavigationType.Default : NavigationType.Naval));
			Position = NavigationHelper.FindPointAroundPosition(closestNavMeshFaceCenterPositionForPosition, NavigationCapability, 8f, 1f);
		}
	}

	public void CheckAiForMapChangeAndUpdateIfNeeded()
	{
		if (IsMainParty)
		{
			return;
		}
		CampaignVec2 moveTargetPoint = CampaignVec2.Invalid;
		if (TargetSettlement != null)
		{
			moveTargetPoint = (IsTargetingPort ? TargetSettlement.PortPosition : TargetSettlement.GatePosition);
		}
		Ai.CacheAiBehaviorPartyBase();
		MobileParty mobileParty = MoveTargetParty ?? TargetParty;
		mobileParty?.CheckPositionsForMapChangeAndUpdateIfNeeded();
		CampaignVec2 moveTargetPoint2;
		switch (DefaultBehavior)
		{
		case AiBehavior.Hold:
			MoveTargetPoint = Position;
			break;
		case AiBehavior.GoToSettlement:
		case AiBehavior.AssaultSettlement:
		case AiBehavior.RaidSettlement:
		case AiBehavior.BesiegeSettlement:
			MoveTargetPoint = moveTargetPoint;
			break;
		case AiBehavior.EngageParty:
		case AiBehavior.JoinParty:
		case AiBehavior.EscortParty:
			MoveTargetPoint = mobileParty.Position;
			break;
		case AiBehavior.GoAroundParty:
			MoveTargetPoint = Ai.AiBehaviorPartyBase.Position;
			break;
		case AiBehavior.GoToPoint:
			if (Army != null)
			{
				IMapPoint aiBehaviorObject = Army.AiBehaviorObject;
				if (aiBehaviorObject == null)
				{
					goto IL_015a;
				}
				if (!(aiBehaviorObject is Settlement settlement2))
				{
					if (!(aiBehaviorObject is MobileParty mobileParty2))
					{
						goto IL_015a;
					}
					moveTargetPoint2 = mobileParty2.Position;
				}
				else
				{
					Settlement settlement3 = settlement2;
					moveTargetPoint = (IsTargetingPort ? settlement3.PortPosition : settlement3.GatePosition);
					moveTargetPoint2 = moveTargetPoint;
				}
				goto IL_016c;
			}
			MoveTargetPoint = Position;
			SetMoveModeHold();
			break;
		case AiBehavior.FleeToPoint:
		{
			Ai.CalculateFleePosition(out var fleeTargetPoint, ShortTermTargetParty, (ShortTermTargetParty.Position - Position).ToVec2());
			MoveTargetPoint = fleeTargetPoint;
			break;
		}
		case AiBehavior.FleeToGate:
			MoveTargetPoint = moveTargetPoint;
			break;
		case AiBehavior.FleeToParty:
			MoveTargetPoint = MoveTargetParty.Position;
			break;
		case AiBehavior.PatrolAroundPoint:
		{
			if (MoveTargetParty != null)
			{
				MoveTargetPoint = MoveTargetParty.Position;
				break;
			}
			if (TargetSettlement != null)
			{
				MoveTargetPoint = moveTargetPoint;
				break;
			}
			Settlement settlement = SettlementHelper.FindNearestHideoutToMobileParty(this, NavigationCapability)?.Settlement;
			if (settlement != null)
			{
				MoveTargetPoint = ((NavigationCapability == NavigationType.Naval) ? settlement.PortPosition : settlement.GatePosition);
				break;
			}
			MoveTargetPoint = Position;
			SetMoveModeHold();
			break;
		}
		case AiBehavior.DefendSettlement:
			{
				if (TargetSettlement.SiegeEvent != null)
				{
					MoveTargetPoint = TargetSettlement.SiegeEvent.BesiegerCamp.LeaderParty.Position;
				}
				else
				{
					MoveTargetPoint = moveTargetPoint;
				}
				break;
			}
			IL_016c:
			MoveTargetPoint = moveTargetPoint2;
			break;
			IL_015a:
			moveTargetPoint2 = Army.AiBehaviorObject.Position;
			goto IL_016c;
		}
		if (DefaultBehavior != AiBehavior.EscortParty)
		{
			TargetPosition = MoveTargetPoint;
			Ai.BehaviorTarget = MoveTargetPoint;
			NextTargetPosition = MoveTargetPoint;
		}
	}

	[LoadInitializationCallback]
	private void OnLoad(MetaData metaData, ObjectLoadData objectLoadData)
	{
		object memberValueBySaveId = objectLoadData.GetMemberValueBySaveId(1032);
		if (memberValueBySaveId != null)
		{
			_doNotAttackMainParty = (int)memberValueBySaveId;
		}
		if (MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.0")))
		{
			HasLandNavigationCapability = true;
			Anchor = new AnchorPoint(this);
		}
	}

	protected override void PreAfterLoad()
	{
		UpdatePartyComponentFlags();
		PartyComponent?.Initialize(this);
		ComputePathAfterLoad();
		Anchor?.InitializeOnLoad(this);
		Ai.PreAfterLoad();
		if (_disorganizedUntilTime.IsFuture)
		{
			_isDisorganized = true;
		}
		if (!MBSaveLoad.IsUpdatingGameVersion)
		{
			return;
		}
		if (MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.2"))
		{
			if ((LeaderHero != null && this != MainParty && LeaderHero.PartyBelongedTo != this) || (MapEvent == null && base.StringId.Contains("troops_of_")))
			{
				DestroyPartyAction.Apply(null, this);
			}
			if (MapEvent == null && (base.StringId.Contains("troops_of_CharacterObject") || base.StringId.Contains("troops_of_TaleWorlds.CampaignSystem.CharacterObject")))
			{
				if (!IsActive)
				{
					IsActive = true;
				}
				DestroyPartyAction.Apply(null, this);
			}
		}
		if (MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.3.0") && IsActive && MapFaction == null)
		{
			if (MapEvent != null)
			{
				MapEventSide = null;
			}
			RemoveParty();
		}
		if (MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.15.113119")) && (IsGarrison || IsMilitia) && CurrentSettlement == null && (MapEvent == null || (!MapEvent.IsSallyOut && !MapEvent.IsBlockadeSallyOut)))
		{
			MapEventSide = null;
			DestroyPartyAction.Apply(null, this);
		}
		if (MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.14")))
		{
			string value = "weapon_smuggling_quest_guards_party_";
			if (base.StringId.StartsWith(value) && !IsActive)
			{
				IsActive = true;
				DestroyPartyAction.Apply(null, this);
			}
		}
	}

	private void ComputePathAfterLoad()
	{
		if (DesiredAiNavigationType != NavigationType.None)
		{
			if (!MoveTargetPoint.Face.IsValid())
			{
				MoveTargetPoint = new CampaignVec2(MoveTargetPoint.ToVec2(), !MoveTargetPoint.IsOnLand);
			}
			TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MoveTargetPoint.Face);
			NavigationType navigationType = DesiredAiNavigationType;
			if (DesiredAiNavigationType == NavigationType.Naval != IsCurrentlyAtSea || !Campaign.Current.Models.PartyNavigationModel.IsTerrainTypeValidForNavigationType(faceTerrainType, DesiredAiNavigationType))
			{
				navigationType = NavigationType.All;
			}
			if (navigationType != NavigationType.None)
			{
				ComputePath(MoveTargetPoint, navigationType, !IsFleeing());
			}
		}
	}

	protected override void OnBeforeLoad()
	{
		InitMembers();
		InitCached();
		_attachedTo?.AddAttachedPartyInternal(this);
	}

	private void InitCached()
	{
		Path = new NavigationPath();
		PathLastFace = PathFaceRecord.NullFaceRecord;
		ForceAiNoPathMode = false;
		((ILocatable<MobileParty>)this).LocatorNodeIndex = -1;
		ThinkParamsCache = new PartyThinkParams(this);
		ResetCached();
	}

	private void ResetCached()
	{
		_partySizeRatioLastCheckVersion = -1;
		_cachedPartySizeRatio = 1f;
		VersionNo = 0;
		_partyPureSpeedLastCheckVersion = -1;
		_itemRosterVersionNo = -1;
		Party.InitCache();
	}

	protected override void AfterLoad()
	{
		Party.AfterLoad();
		if (IsGarrison && MapEvent == null && SiegeEvent == null && TargetParty != null && CurrentSettlement != null)
		{
			SetMoveModeHold();
		}
		if (CurrentSettlement != null && !CurrentSettlement.Parties.Contains(this))
		{
			CurrentSettlement.AddMobileParty(this);
			foreach (MobileParty attachedParty in _attachedParties)
			{
				if (Army.LeaderParty != this)
				{
					CurrentSettlement.AddMobileParty(attachedParty);
				}
			}
		}
		if (_doNotAttackMainParty > 0)
		{
			Ai.DoNotAttackMainPartyUntil = CampaignTime.HoursFromNow(_doNotAttackMainParty);
		}
		if (IsCaravan && Army != null)
		{
			Army = null;
		}
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.1.0") && (PaymentLimit == 2000 || (this == MainParty && PaymentLimit == 0)))
		{
			SetWagePaymentLimit(Campaign.Current.Models.PartyWageModel.MaxWagePaymentLimit);
		}
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.0") && IsCaravan && Owner == Hero.MainHero && ActualClan == null)
		{
			ActualClan = Owner.Clan;
		}
		if (!MBSaveLoad.IsUpdatingGameVersion || !(MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.1.4")))
		{
			return;
		}
		if (TargetParty != null)
		{
			IFaction mapFaction = TargetParty.MapFaction;
			if (mapFaction == null || !mapFaction.IsAtWarWith(MapFaction))
			{
				goto IL_024d;
			}
		}
		if (TargetSettlement != null)
		{
			IFaction mapFaction2 = TargetSettlement.MapFaction;
			if (mapFaction2 == null || !mapFaction2.IsAtWarWith(MapFaction))
			{
				goto IL_024d;
			}
		}
		if (ShortTermTargetParty != null)
		{
			MobileParty shortTermTargetParty = ShortTermTargetParty;
			if (shortTermTargetParty != null && shortTermTargetParty.MapFaction?.IsAtWarWith(MapFaction) == true)
			{
				return;
			}
			goto IL_024d;
		}
		return;
		IL_024d:
		SetMoveModeHold();
	}

	internal void OnFinishLoadState()
	{
		Campaign.Current.MobilePartyLocator.UpdateLocator(this);
	}

	internal void HourlyTick()
	{
		if (IsActive)
		{
			if (LeaderHero != null && CurrentSettlement != null && CurrentSettlement == LeaderHero.HomeSettlement)
			{
				LeaderHero.PassedTimeAtHomeSettlement++;
			}
			Anchor.HourlyTick();
		}
	}

	public void MovePartyToTheClosestLand()
	{
		int[] invalidTerrainTypesForNavigationType = Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(NavigationType.All);
		CampaignVec2 nearestFaceCenterForPositionWithPath = Campaign.Current.MapSceneWrapper.GetNearestFaceCenterForPositionWithPath(CurrentNavigationFace, targetIsLand: true, Campaign.MapDiagonal / 2f, invalidTerrainTypesForNavigationType);
		SetNavigationModePoint(nearestFaceCenterForPositionWithPath);
		SetMoveGoToPoint(nearestFaceCenterForPositionWithPath, NavigationType.All);
	}

	internal void DailyTick()
	{
		RecentEventsMorale -= RecentEventsMorale * 0.1f;
		if (LeaderHero != null)
		{
			LeaderHero.PassedTimeAtHomeSettlement *= 0.9f;
		}
	}

	public TextObject GetBehaviorText()
	{
		TextObject textObject = TextObject.GetEmpty();
		if (Army != null && (AttachedTo != null || Army.LeaderParty == this) && !Army.LeaderParty.IsEngaging && !Army.LeaderParty.IsFleeing())
		{
			textObject = Army.GetLongTermBehaviorText();
		}
		if (textObject.IsEmpty())
		{
			float estimatedLandRatio;
			if (DefaultBehavior == AiBehavior.Hold || ShortTermBehavior == AiBehavior.Hold || (IsMainParty && Campaign.Current.IsMainPartyWaiting))
			{
				textObject = ((!IsVillager || !HasNavalNavigationCapability) ? new TextObject("{=RClxLG6N}Holding.") : new TextObject("{=WYxUqYpu}Fishing."));
			}
			else if (ShortTermBehavior == AiBehavior.EngageParty && ShortTermTargetParty != null)
			{
				textObject = new TextObject("{=5bzk75Ql}Engaging {TARGET_PARTY}.");
				textObject.SetTextVariable("TARGET_PARTY", ShortTermTargetParty.Name);
			}
			else if (DefaultBehavior == AiBehavior.GoAroundParty && ShortTermBehavior == AiBehavior.GoToPoint)
			{
				textObject = new TextObject("{=XYAVu2f0}Chasing {TARGET_PARTY}.");
				textObject.SetTextVariable("TARGET_PARTY", TargetParty.Name);
			}
			else if (ShortTermBehavior == AiBehavior.FleeToParty && ShortTermTargetParty != null)
			{
				textObject = new TextObject("{=R8vuwKaf}Running from {TARGET_PARTY} to ally party.");
				textObject.SetTextVariable("TARGET_PARTY", ShortTermTargetParty.Name);
			}
			else if (ShortTermBehavior == AiBehavior.FleeToPoint)
			{
				if (ShortTermTargetParty != null)
				{
					textObject = new TextObject("{=AcMayd1p}Running from {TARGET_PARTY}.");
					textObject.SetTextVariable("TARGET_PARTY", ShortTermTargetParty.Name);
				}
				else
				{
					textObject = new TextObject("{=5W2oZOwu}Sailing away from storm.");
				}
			}
			else if (ShortTermBehavior == AiBehavior.FleeToGate && ShortTermTargetSettlement != null)
			{
				textObject = new TextObject("{=p0C3WfHE}Running to settlement.");
			}
			else if (DefaultBehavior == AiBehavior.DefendSettlement)
			{
				textObject = ((!(Campaign.Current.Models.MapDistanceModel.GetDistance(this, TargetSettlement, IsTargetingPort, NavigationCapability, out estimatedLandRatio) > Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(NavigationType.All) * 0.2f)) ? new TextObject("{=rGy8vjOv}Defending {TARGET_SETTLEMENT}.") : new TextObject("{=*}Travelling to {TARGET_SETTLEMENT} to defend."));
				if (ShortTermBehavior == AiBehavior.GoToPoint)
				{
					if (!IsMoving)
					{
						textObject = new TextObject("{=LAt87KjS}Waiting for ally parties to defend {TARGET_SETTLEMENT}.");
					}
					else if (ShortTermTargetParty != null && ShortTermTargetParty.MapFaction == MapFaction)
					{
						textObject = new TextObject("{=yD7rL5Nc}Helping ally party to defend {TARGET_SETTLEMENT}.");
					}
				}
				textObject.SetTextVariable("TARGET_SETTLEMENT", TargetSettlement.Name);
			}
			else if (DefaultBehavior == AiBehavior.RaidSettlement)
			{
				Settlement targetSettlement = TargetSettlement;
				textObject = ((!(Campaign.Current.Models.MapDistanceModel.GetDistance(this, targetSettlement, IsTargetingPort, NavigationCapability, out estimatedLandRatio) > Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(NavigationType.All) * 0.2f)) ? new TextObject("{=VtWa9Pmh}Raiding {TARGET_SETTLEMENT}.") : new TextObject("{=BqIRb85N}Going to raid {TARGET_SETTLEMENT}"));
				textObject.SetTextVariable("TARGET_SETTLEMENT", targetSettlement.Name);
			}
			else if (DefaultBehavior == AiBehavior.BesiegeSettlement)
			{
				textObject = new TextObject("{=JTxI3sW2}Besieging {TARGET_SETTLEMENT}.");
				textObject.SetTextVariable("TARGET_SETTLEMENT", TargetSettlement.Name);
			}
			else if (ShortTermBehavior == AiBehavior.GoToPoint && DefaultBehavior != AiBehavior.EscortParty)
			{
				if (ShortTermTargetParty != null)
				{
					textObject = new TextObject("{=AcMayd1p}Running from {TARGET_PARTY}.");
					textObject.SetTextVariable("TARGET_PARTY", ShortTermTargetParty.Name);
				}
				else if (TargetSettlement == null)
				{
					textObject = ((DefaultBehavior == AiBehavior.MoveToNearestLandOrPort) ? new TextObject("{=8vuOdcpy}Moving to the nearest shore.") : ((DefaultBehavior == AiBehavior.PatrolAroundPoint) ? new TextObject("{=BifGz0h4}Patrolling.") : ((!IsInRaftState) ? new TextObject("{=XAL3t1bs}Going to a point.") : new TextObject("{=vxdIEThU}Drifting to shore."))));
				}
				else if (DefaultBehavior == AiBehavior.PatrolAroundPoint)
				{
					bool flag = IsLordParty && !AiBehaviorTarget.IsOnLand;
					textObject = ((!(Campaign.Current.Models.MapDistanceModel.GetDistance(this, TargetSettlement, IsTargetingPort, NavigationCapability, out estimatedLandRatio) > Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(NavigationType.All) * 0.5f)) ? ((!flag) ? new TextObject("{=yUVv3z5V}Patrolling around {TARGET_SETTLEMENT}.") : (TargetSettlement.MapFaction.IsAtWarWith(MapFaction) ? new TextObject("{=VQY0e6hF}Threatening the coastal waters off {TARGET_SETTLEMENT}.") : new TextObject("{=8qvUbTvW}Guarding the coastal waters off {TARGET_SETTLEMENT}."))) : ((!flag) ? new TextObject("{=MNoogAgk}Heading to patrol around {TARGET_SETTLEMENT}.") : (TargetSettlement.MapFaction.IsAtWarWith(MapFaction) ? new TextObject("{=YIKb1kob}Heading to the coastal waters off {TARGET_SETTLEMENT}.") : new TextObject("{=avhlH79s}Heading to patrol the coastal waters off {TARGET_SETTLEMENT}."))));
					textObject.SetTextVariable("TARGET_SETTLEMENT", (TargetSettlement != null) ? TargetSettlement.Name : HomeSettlement.Name);
				}
				else
				{
					textObject = new TextObject("{=TaK6ydAx}Travelling.");
				}
			}
			else if (ShortTermBehavior == AiBehavior.GoToSettlement || DefaultBehavior == AiBehavior.GoToSettlement)
			{
				if (ShortTermBehavior == AiBehavior.GoToSettlement && ShortTermTargetSettlement != null && ShortTermTargetSettlement != TargetSettlement)
				{
					if (DefaultBehavior == AiBehavior.MoveToNearestLandOrPort)
					{
						textObject = new TextObject("{=amHKbKfV}Running away from the sea.");
						textObject.SetTextVariable("TARGET_PARTY", ShortTermTargetSettlement.Name);
					}
					else
					{
						textObject = ((ShortTermTargetParty != null && ShortTermTargetParty.MapFaction.IsAtWarWith(MapFaction)) ? new TextObject("{=NRpbagbZ}Running to {TARGET_PARTY}.") : new TextObject("{=EQHq3bHM}Travelling to {TARGET_PARTY}"));
						textObject.SetTextVariable("TARGET_PARTY", ShortTermTargetSettlement.Name);
					}
				}
				else if (DefaultBehavior == AiBehavior.GoToSettlement && TargetSettlement != null)
				{
					textObject = ((CurrentSettlement != TargetSettlement) ? new TextObject("{=EQHq3bHM}Travelling to {TARGET_PARTY}") : new TextObject("{=Y65gdbrx}Waiting in {TARGET_PARTY}."));
					textObject.SetTextVariable("TARGET_PARTY", TargetSettlement.Name);
				}
				else if (ShortTermTargetParty != null)
				{
					textObject = new TextObject("{=AcMayd1p}Running from {TARGET_PARTY}.");
					textObject.SetTextVariable("TARGET_PARTY", ShortTermTargetParty.Name);
				}
				else
				{
					textObject = new TextObject("{=QGyoSLeY}Traveling to a settlement.");
				}
			}
			else if (ShortTermBehavior == AiBehavior.AssaultSettlement)
			{
				textObject = new TextObject("{=exnL6SS7}Attacking {TARGET_SETTLEMENT}.");
				textObject.SetTextVariable("TARGET_SETTLEMENT", ShortTermTargetSettlement.Name);
			}
			else if (DefaultBehavior != AiBehavior.EscortParty && ShortTermBehavior != AiBehavior.EscortParty)
			{
				textObject = ((DefaultBehavior != AiBehavior.MoveToNearestLandOrPort) ? new TextObject("{=QXBf26Rv}Unknown Behavior.") : new TextObject("{=amHKbKfV}Running away from the sea."));
			}
			else
			{
				textObject = new TextObject("{=OpzzCPiP}Following {TARGET_PARTY}.");
				textObject.SetTextVariable("TARGET_PARTY", (ShortTermTargetParty != null) ? ShortTermTargetParty.Name : TargetParty.Name);
			}
		}
		return textObject;
	}

	public override void Initialize()
	{
		base.Initialize();
		Aggressiveness = 1f;
		Ai = new MobilePartyAi(this);
		_formationPosition.x = 10000f;
		_formationPosition.y = 10000f;
		while (_formationPosition.LengthSquared > 0.36f || _formationPosition.LengthSquared < 0.22f)
		{
			_formationPosition = new Vec2(MBRandom.RandomFloat * 1.2f - 0.6f, MBRandom.RandomFloat * 1.2f - 0.6f);
		}
		CampaignEventDispatcher.Instance.OnPartyVisibilityChanged(Party);
	}

	private void InitMembers()
	{
		if (_attachedParties == null)
		{
			_attachedParties = new MBList<MobileParty>();
		}
	}

	public void InitializeMobilePartyAtPosition(CampaignVec2 position)
	{
		IsCurrentlyAtSea = !position.IsOnLand;
		CreateFigure(position);
		SetMoveModeHold();
	}

	public void InitializeMobilePartyAtPosition(TroopRoster memberRoster, TroopRoster prisonerRoster, CampaignVec2 position, bool isNaval = false)
	{
		InitializeMobilePartyWithRosterInternal(memberRoster, prisonerRoster, position);
	}

	public void InitializeMobilePartyAroundPosition(TroopRoster memberRoster, TroopRoster prisonerRoster, CampaignVec2 position, float spawnRadius, float minSpawnRadius = 0f, bool isNaval = false)
	{
		if (spawnRadius > 0f)
		{
			NavigationType navigationCapability = ((!isNaval) ? NavigationType.Default : NavigationType.Naval);
			position = NavigationHelper.FindReachablePointAroundPosition(position, navigationCapability, spawnRadius, minSpawnRadius);
		}
		InitializeMobilePartyWithRosterInternal(memberRoster, prisonerRoster, position);
	}

	public void InitializeMobilePartyAtPosition(PartyTemplateObject pt, CampaignVec2 position)
	{
		InitializeMobilePartyWithPartyTemplate(pt, position);
	}

	private void InitializeMobilePartyWithPartyTemplate(PartyTemplateObject pt, CampaignVec2 position)
	{
		TroopRoster memberRoster = Campaign.Current.Models.PartySizeLimitModel.FindAppropriateInitialRosterForMobileParty(this, pt);
		foreach (Ship item in Campaign.Current.Models.PartySizeLimitModel.FindAppropriateInitialShipsForMobileParty(this, pt))
		{
			ChangeShipOwnerAction.ApplyByMobilePartyCreation(Party, item);
		}
		InitializeMobilePartyWithRosterInternal(memberRoster, null, position);
	}

	private void InitializeMobilePartyWithRosterInternal(TroopRoster memberRoster, TroopRoster prisonerRoster, CampaignVec2 position)
	{
		MemberRoster.Add(memberRoster);
		if (prisonerRoster != null)
		{
			PrisonRoster.Add(prisonerRoster);
		}
		InitializeMobilePartyAtPosition(position);
	}

	public void InitializeMobilePartyAroundPosition(PartyTemplateObject pt, CampaignVec2 position, float spawnRadius, float minSpawnRadius = 0f)
	{
		if (spawnRadius > 0f)
		{
			position = NavigationHelper.FindReachablePointAroundPosition(position, position.IsOnLand ? NavigationType.Default : NavigationType.Naval, spawnRadius, minSpawnRadius);
		}
		InitializeMobilePartyWithPartyTemplate(pt, position);
	}

	internal void InitializePartyForOldSave(int numberOfRecentFleeingFromAParty, AiBehavior defaultBehavior, bool aiPathMode, MoveModeType partyMoveMode, Vec2 formationPosition, MobileParty moveTargetParty, Vec2 nextTargetPosition, Vec2 moveTargetPoint, Vec2 aiPathLastPosition)
	{
		_defaultBehavior = defaultBehavior;
		_pathMode = aiPathMode;
		PartyMoveMode = partyMoveMode;
		_formationPosition = formationPosition;
		MoveTargetParty = moveTargetParty;
		NextTargetPosition = new CampaignVec2(nextTargetPosition, isOnLand: true);
		MoveTargetPoint = new CampaignVec2(moveTargetPoint, isOnLand: true);
		_pathLastPosition = new CampaignVec2(aiPathLastPosition, isOnLand: true);
	}

	internal void TickForStationaryMobileParty(ref CachedPartyVariables variables, float dt, float realDt)
	{
		if (StationaryStartTime == CampaignTime.Never)
		{
			StationaryStartTime = CampaignTime.Now;
		}
		CheckIsDisorganized();
		DoUpdatePosition(ref variables, dt, realDt);
	}

	internal void FillCurrentTickMoveDataForMovingMobileParty(ref CachedPartyVariables variables, float dt, float realDt)
	{
		ComputeNextMoveDistance(ref variables, dt);
		CommonMovingPartyTick(ref variables, dt, realDt);
	}

	internal void FillCurrentTickMoveDataForMovingArmyLeader(ref CachedPartyVariables variables, float dt, float realDt)
	{
		ComputeNextMoveDistanceForArmyLeader(ref variables, dt);
		CommonMovingPartyTick(ref variables, dt, realDt);
	}

	private void CommonMovingPartyTick(ref CachedPartyVariables variables, float dt, float realDt)
	{
		StationaryStartTime = CampaignTime.Never;
		CheckIsDisorganized();
		DoAiPathMode(ref variables);
		DoUpdatePosition(ref variables, dt, realDt);
	}

	internal void CommonTransitioningPartyTick(ref CachedPartyVariables variables, ref int navigationTypeChangeCounter, ref MobileParty[] navigationTypeChangeList, float dt)
	{
		if (ShouldEndTransition())
		{
			if (_attachedTo == null)
			{
				int num = Interlocked.Increment(ref navigationTypeChangeCounter);
				navigationTypeChangeList[num] = this;
			}
		}
		else
		{
			if (!(dt > 0f))
			{
				return;
			}
			if (!HasNavalNavigationCapability)
			{
				CancelNavigationTransitionParallel();
			}
			else
			{
				if (CurrentSettlement != null)
				{
					return;
				}
				Vec2 direction = CampaignVec2.Normalized(variables.NextPosition - Position).ToVec2();
				if (direction.IsNonZero())
				{
					NavigationHelper.EmbarkDisembarkData obj = (IsMainParty ? NavigationHelper.GetEmbarkAndDisembarkDataForPlayer(Position, direction, MoveTargetPoint, isMoveTargetOnLand: true) : NavigationHelper.GetEmbarkDisembarkDataForTick(Position, direction));
					PathFaceRecord face = obj.TransitionEndPosition.Face;
					PathFaceRecord face2 = EndPositionForNavigationTransition.Face;
					if (obj.IsTargetingOwnSideOfTheDeadZone || face.FaceIndex == CurrentNavigationFace.FaceIndex || face2.FaceIndex != face.FaceIndex)
					{
						CancelNavigationTransitionParallel();
					}
				}
			}
		}
	}

	internal void InitializeCachedPartyVariables(ref CachedPartyVariables variables)
	{
		variables.HasMapEvent = MapEvent != null;
		variables.CurrentPosition = Position;
		variables.TargetPartyPositionAtFrameStart = CampaignVec2.Invalid;
		variables.LastCurrentPosition = Position;
		variables.IsAttachedArmyMember = false;
		variables.IsMoving = IsMoving;
		variables.IsArmyLeader = false;
		variables.NextPosition = Position;
		variables.IsTransitionInProgress = IsTransitionInProgress;
		if (Army != null)
		{
			if (Army.LeaderParty == this)
			{
				variables.IsArmyLeader = true;
			}
			else if (AttachedTo != null)
			{
				variables.IsAttachedArmyMember = true;
				variables.IsMoving = IsMoving || Army.LeaderParty.IsMoving;
				variables.IsTransitionInProgress = Army.LeaderParty.IsTransitionInProgress;
			}
		}
	}

	internal void ComputeNextMoveDistanceForArmyLeader(ref CachedPartyVariables variables, float dt)
	{
		if (dt > 0f)
		{
			CalculateSpeedForPartyUnified();
			variables.NextMoveDistance = Speed * dt;
		}
		else
		{
			variables.NextMoveDistance = 0f;
		}
	}

	internal void ComputeNextMoveDistance(ref CachedPartyVariables variables, float dt)
	{
		if (dt > 0f)
		{
			CalculateSpeed();
			variables.NextMoveDistance = Speed * dt;
		}
		else
		{
			variables.NextMoveDistance = 0f;
		}
	}

	internal void UpdateStationaryTimer()
	{
		if (!IsMoving)
		{
			if (StationaryStartTime == CampaignTime.Never)
			{
				StationaryStartTime = CampaignTime.Now;
			}
		}
		else
		{
			StationaryStartTime = CampaignTime.Never;
		}
	}

	private void CheckIsDisorganized()
	{
		if (_isDisorganized && _disorganizedUntilTime.IsPast)
		{
			SetDisorganized(isDisorganized: false);
		}
	}

	public void SetDisorganized(bool isDisorganized)
	{
		if (isDisorganized)
		{
			_disorganizedUntilTime = CampaignTime.HoursFromNow(Campaign.Current.Models.PartyImpairmentModel.GetDisorganizedStateDuration(this).ResultNumber);
		}
		_isDisorganized = isDisorganized;
		UpdateVersionNo();
	}

	internal void CacheTargetPartyVariablesAtFrameStart(ref CachedPartyVariables variables)
	{
		if (MoveTargetParty != null)
		{
			variables.TargetPartyPositionAtFrameStart = MoveTargetParty.Position;
			variables.IsTargetMovingAtFrameStart = MoveTargetParty.IsMoving || MoveTargetParty.IsTransitionInProgress;
		}
	}

	public void RecalculateShortTermBehavior()
	{
		if (DefaultBehavior == AiBehavior.RaidSettlement)
		{
			SetShortTermBehavior(AiBehavior.RaidSettlement, TargetSettlement.Party);
		}
		else if (DefaultBehavior == AiBehavior.BesiegeSettlement)
		{
			SetShortTermBehavior(AiBehavior.BesiegeSettlement, TargetSettlement.Party);
		}
		else if (DefaultBehavior == AiBehavior.GoToSettlement)
		{
			SetShortTermBehavior(AiBehavior.GoToSettlement, TargetSettlement.Party);
		}
		else if (DefaultBehavior == AiBehavior.EngageParty)
		{
			SetShortTermBehavior(AiBehavior.EngageParty, TargetParty.Party);
		}
		else if (DefaultBehavior == AiBehavior.DefendSettlement)
		{
			SetShortTermBehavior(AiBehavior.GoToPoint, TargetSettlement.Party);
		}
		else if (DefaultBehavior == AiBehavior.EscortParty)
		{
			SetShortTermBehavior(AiBehavior.EscortParty, TargetParty.Party);
		}
		else if (DefaultBehavior == AiBehavior.GoToPoint)
		{
			SetShortTermBehavior(AiBehavior.GoToPoint, Ai.AiBehaviorInteractable);
		}
		else if (DefaultBehavior == AiBehavior.MoveToNearestLandOrPort)
		{
			SetShortTermBehavior(AiBehavior.GoToPoint, null);
		}
		else if (DefaultBehavior == AiBehavior.None)
		{
			ShortTermBehavior = AiBehavior.None;
		}
	}

	internal void SetShortTermBehavior(AiBehavior newBehavior, IInteractablePoint mapEntity)
	{
		if (ShortTermBehavior != newBehavior)
		{
			ShortTermBehavior = newBehavior;
		}
		if (!IsMainParty && DefaultBehavior == AiBehavior.Hold && DesiredAiNavigationType == NavigationType.None && !IsMilitia && !IsGarrison)
		{
			DesiredAiNavigationType = ((!IsCurrentlyAtSea) ? NavigationType.Default : (HasNavalNavigationCapability ? NavigationType.Naval : NavigationType.None));
		}
		Ai.AiBehaviorInteractable = mapEntity;
	}

	public static bool IsFleeBehavior(AiBehavior aiBehavior)
	{
		if (aiBehavior != AiBehavior.FleeToPoint && aiBehavior != AiBehavior.FleeToGate)
		{
			return aiBehavior == AiBehavior.FleeToParty;
		}
		return true;
	}

	public bool IsFleeing()
	{
		if (!IsFleeBehavior(ShortTermBehavior))
		{
			return IsFleeBehavior(DefaultBehavior);
		}
		return true;
	}

	private void UpdatePathModeWithPosition(CampaignVec2 newTargetPosition)
	{
		MoveTargetPoint = newTargetPosition;
		NavigationHelper.IsPositionValidForNavigationType(newTargetPosition, NavigationCapability);
	}

	internal void TryToMoveThePartyWithCurrentTickMoveData(ref CachedPartyVariables variables, ref int gridChangeCount, ref MobileParty[] gridChangeMobilePartyList)
	{
		if (!(variables.NextMoveDistance > 0f) || !variables.IsMoving || BesiegedSettlement != null || variables.HasMapEvent)
		{
			return;
		}
		CheckTransitionParallel(ref variables, ref gridChangeCount, ref gridChangeMobilePartyList);
		if (!IsTransitionInProgress || IsInRaftState)
		{
			if (variables.IsAttachedArmyMember && (Army.LeaderParty.Position.ToVec2() - (Position.ToVec2() + ArmyPositionAdder + (variables.NextPosition - Position).ToVec2())).Length > 1E-05f)
			{
				SetPositionParallel(Army.LeaderParty.Position, ref gridChangeCount, ref gridChangeMobilePartyList);
				ArmyPositionAdder += variables.NextPosition.ToVec2() - Position.ToVec2();
			}
			else if (CurrentNavigationFace.IsValid() && CurrentNavigationFace.FaceIslandIndex == variables.NextPosition.Face.FaceIslandIndex)
			{
				SetPositionParallel(in variables.NextPosition, ref gridChangeCount, ref gridChangeMobilePartyList);
			}
		}
	}

	internal void CheckTransitionParallel(ref CachedPartyVariables variables, ref int gridChangeCount, ref MobileParty[] gridChangeMobilePartyList)
	{
		if (AttachedTo != null || (IsMainParty && !IsCurrentlyAtSea) || IsTransitionInProgress || !CurrentNavigationFace.IsValid() || !(IsCurrentlyAtSea ? HasLandNavigationCapability : HasNavalNavigationCapability))
		{
			return;
		}
		Vec2 direction = CampaignVec2.Normalized(variables.NextPosition - variables.CurrentPosition).ToVec2();
		NavigationHelper.EmbarkDisembarkData embarkDisembarkData = (IsMainParty ? NavigationHelper.GetEmbarkAndDisembarkDataForPlayer(Position, direction, MoveTargetPoint, isMoveTargetOnLand: true) : NavigationHelper.GetEmbarkDisembarkDataForTick(Position, direction));
		if (!embarkDisembarkData.IsValidTransition || embarkDisembarkData.IsTargetingOwnSideOfTheDeadZone)
		{
			return;
		}
		TerrainType faceGroupIndex = (TerrainType)embarkDisembarkData.TransitionEndPosition.Face.FaceGroupIndex;
		if (!Campaign.Current.Models.PartyNavigationModel.IsTerrainTypeValidForNavigationType(faceGroupIndex, (!IsCurrentlyAtSea) ? NavigationType.Default : NavigationType.Naval) && Campaign.Current.Models.PartyNavigationModel.IsTerrainTypeValidForNavigationType(faceGroupIndex, NavigationType.All))
		{
			float num = embarkDisembarkData.TransitionStartPosition.Distance(embarkDisembarkData.NavMeshEdgePosition);
			float num2 = embarkDisembarkData.NavMeshEdgePosition.Distance(variables.CurrentPosition);
			if (MoveTargetPoint.Distance(variables.CurrentPosition) > num2 && (num2 < num || num2 < variables.NextMoveDistance))
			{
				InitializeNavigationTransitionParallel(embarkDisembarkData.TransitionStartPosition, embarkDisembarkData.TransitionEndPosition, ref gridChangeCount, ref gridChangeMobilePartyList);
				variables.NextMoveDistance = 0f;
			}
		}
	}

	private bool ShouldEndTransition()
	{
		if (IsTransitionInProgress && (NavigationTransitionStartTime + NavigationTransitionDuration).IsPast)
		{
			return AttachedParties.All((MobileParty x) => x.ShouldEndTransition());
		}
		return false;
	}

	private void SetPositionParallel(in CampaignVec2 value, ref int gridChangeCounter, ref MobileParty[] gridChangeList)
	{
		if (Position != value)
		{
			_lastNavigationFace = Position.Face;
			_position = value;
			if (!Campaign.Current.MobilePartyLocator.CheckWhetherPositionsAreInSameNode(value.ToVec2(), this))
			{
				int num = Interlocked.Increment(ref gridChangeCounter);
				gridChangeList[num] = this;
			}
		}
	}

	public void SetPartyUsedByQuest(bool isActivelyUsed)
	{
		if (_isCurrentlyUsedByAQuest != isActivelyUsed)
		{
			_isCurrentlyUsedByAQuest = isActivelyUsed;
			CampaignEventDispatcher.Instance.OnMobilePartyQuestStatusChanged(this, isActivelyUsed);
		}
	}

	public void IgnoreForHours(float hours)
	{
		_ignoredUntilTime = CampaignTime.HoursFromNow(hours);
	}

	public void IgnoreByOtherPartiesTill(CampaignTime time)
	{
		_ignoredUntilTime = time;
	}

	private void OnRemoveParty()
	{
		Army = null;
		CurrentSettlement = null;
		AttachedTo = null;
		BesiegerCamp = null;
		List<Settlement> list = new List<Settlement>();
		if (CurrentSettlement != null)
		{
			list.Add(CurrentSettlement);
		}
		else if ((IsGarrison || IsMilitia || IsBandit || IsVillager || IsPatrolParty) && HomeSettlement != null)
		{
			list.Add(HomeSettlement);
		}
		PartyComponent?.Finish();
		ActualClan = null;
		Anchor = null;
		Campaign.Current.CampaignObjectManager.RemoveMobileParty(this);
		foreach (Settlement item in list)
		{
			item.SettlementComponent.OnRelatedPartyRemoved(this);
		}
	}

	public void SetAnchor(AnchorPoint anchor)
	{
		Anchor = anchor;
		Anchor.InitializeOnLoad(this);
	}

	public void SetPartyObjective(PartyObjective objective)
	{
		Objective = objective;
	}

	public void UpdateVersionNo()
	{
		VersionNo++;
	}

	private bool IsLastSpeedCacheInvalid()
	{
		Hero leaderHero = LeaderHero;
		bool flag = !IsActive || leaderHero == null || leaderHero.PartyBelongedToAsPrisoner != null;
		bool isNight = Campaign.Current.IsNight;
		Vec2 vec = _lastWind;
		if (IsCurrentlyAtSea)
		{
			vec = Campaign.Current.Models.MapWeatherModel.GetWindForPosition(Position);
		}
		if (_lastNavigationFace.FaceIndex == CurrentNavigationFace.FaceIndex && _partyLastCheckIsPrisoner == flag && _partyLastCheckAtNight == isNight && !(Math.Abs(_lastWind.RotationInRadians - vec.RotationInRadians) > 0.06f))
		{
			return Math.Abs(_lastWind.LengthSquared - vec.LengthSquared) > 0.0001f;
		}
		return true;
	}

	private bool IsBaseSpeedCacheInvalid()
	{
		UpdateCommonCacheVersions();
		MapWeatherModel.WeatherEventEffectOnTerrain weatherEffectOnTerrainForPosition = Campaign.Current.Models.MapWeatherModel.GetWeatherEffectOnTerrainForPosition(Position.ToVec2());
		if (_partyPureSpeedLastCheckVersion == GetVersionNoForBaseSpeedCalculation())
		{
			return _lastWeatherTerrainEffect != weatherEffectOnTerrainForPosition;
		}
		return true;
	}

	private int GetVersionNoForWeightCalculation()
	{
		if (IsCurrentlyAtSea)
		{
			return (17 * 31 + VersionNo) * 31 + Party.GetShipsVersion();
		}
		return VersionNo;
	}

	private int GetVersionNoForBaseSpeedCalculation()
	{
		return GetVersionNoForWeightCalculation();
	}

	private float CalculateSpeedForPartyUnified()
	{
		bool flag = false;
		if (IsBaseSpeedCacheInvalid())
		{
			if (Army != null && Army.LeaderParty.AttachedParties.Contains(this))
			{
				_lastCalculatedBaseSpeedExplained = Army.LeaderParty._lastCalculatedBaseSpeedExplained;
			}
			else
			{
				_lastCalculatedBaseSpeedExplained = Campaign.Current.Models.PartySpeedCalculatingModel.CalculateBaseSpeed(this);
			}
			MapWeatherModel.WeatherEventEffectOnTerrain weatherEffectOnTerrainForPosition = Campaign.Current.Models.MapWeatherModel.GetWeatherEffectOnTerrainForPosition(Position.ToVec2());
			_lastWeatherTerrainEffect = weatherEffectOnTerrainForPosition;
			_partyPureSpeedLastCheckVersion = GetVersionNoForBaseSpeedCalculation();
			flag = true;
		}
		if (flag)
		{
			_lastCalculatedSpeed = Campaign.Current.Models.PartySpeedCalculatingModel.CalculateFinalSpeed(this, _lastCalculatedBaseSpeedExplained).ResultNumber;
		}
		else if (IsLastSpeedCacheInvalid())
		{
			Hero leaderHero = LeaderHero;
			bool partyLastCheckIsPrisoner = !IsActive || leaderHero == null || leaderHero.PartyBelongedToAsPrisoner != null;
			bool isNight = Campaign.Current.IsNight;
			if (IsCurrentlyAtSea)
			{
				_lastWind = Campaign.Current.Models.MapWeatherModel.GetWindForPosition(Position);
			}
			_lastNavigationFace = CurrentNavigationFace;
			_partyLastCheckIsPrisoner = partyLastCheckIsPrisoner;
			_partyLastCheckAtNight = isNight;
			_lastCalculatedSpeed = Campaign.Current.Models.PartySpeedCalculatingModel.CalculateFinalSpeed(this, _lastCalculatedBaseSpeedExplained).ResultNumber;
		}
		return _lastCalculatedSpeed;
	}

	private bool IsWeightCacheInvalid()
	{
		UpdateCommonCacheVersions();
		return _partyWeightLastCheckVersionNo != GetVersionNoForWeightCalculation();
	}

	private void UpdateCommonCacheVersions()
	{
		if (_itemRosterVersionNo != Party.ItemRoster.VersionNo)
		{
			_itemRosterVersionNo = ItemRoster.VersionNo;
			UpdateVersionNo();
		}
	}

	private float CalculateSpeed()
	{
		if (Army != null)
		{
			if (Army.LeaderParty.AttachedParties.Contains(this))
			{
				Vec2 armyFacing = (((Army.LeaderParty.MapEvent != null) ? Army.LeaderParty.Position.ToVec2() : Army.LeaderParty.NextTargetPosition.ToVec2()) - Army.LeaderParty.Position.ToVec2()).Normalized();
				Vec2 vec = Army.LeaderParty.Position.ToVec2() + armyFacing.TransformToParentUnitF(Army.GetRelativePositionForParty(this, armyFacing));
				float num = Bearing.DotProduct(vec - VisualPosition2DWithoutError);
				return Army.LeaderParty._lastCalculatedSpeed * MBMath.ClampFloat(1f + num * 1f, 0.7f, 1.3f);
			}
		}
		else if (DefaultBehavior == AiBehavior.EscortParty && TargetParty != null && _lastCalculatedSpeed > TargetParty._lastCalculatedSpeed)
		{
			return TargetParty._lastCalculatedSpeed;
		}
		return CalculateSpeedForPartyUnified();
	}

	public bool IsSpotted()
	{
		return IsVisible;
	}

	public int AddElementToMemberRoster(CharacterObject element, int numberToAdd, bool insertAtFront = false)
	{
		return Party.AddElementToMemberRoster(element, numberToAdd, insertAtFront);
	}

	public int AddPrisoner(CharacterObject element, int numberToAdd)
	{
		return Party.AddPrisoner(element, numberToAdd);
	}

	public Vec3 GetPositionAsVec3()
	{
		return Position.AsVec3();
	}

	public float GetTotalLandStrengthWithFollowers(bool includeNonAttachedArmyMembers = true)
	{
		MobileParty mobileParty = ((DefaultBehavior == AiBehavior.EscortParty) ? TargetParty : this);
		float num = Campaign.Current.Models.MilitaryPowerModel.GetPowerOfParty(mobileParty.Party, BattleSideEnum.Attacker, MapEvent.PowerCalculationContext.PlainBattle);
		if (mobileParty.Army != null && mobileParty == mobileParty.Army.LeaderParty)
		{
			foreach (MobileParty party in mobileParty.Army.Parties)
			{
				if (party.Army.LeaderParty != party && (party.AttachedTo != null || includeNonAttachedArmyMembers))
				{
					num += Campaign.Current.Models.MilitaryPowerModel.GetPowerOfParty(party.Party, BattleSideEnum.Attacker, MapEvent.PowerCalculationContext.PlainBattle);
				}
			}
		}
		return num;
	}

	private void OnPartyJoinedSiegeInternal()
	{
		_besiegerCamp.AddSiegePartyInternal(this);
		Town town = SiegeEvent.BesiegedSettlement.Town;
		CampaignVec2 siegeCampPartyPosition = _besiegerCamp.GetSiegeCampPartyPosition(this, town.BesiegerCampPositions1, town.BesiegerCampPositions2);
		if (siegeCampPartyPosition.IsValid())
		{
			Position = siegeCampPartyPosition;
		}
		else
		{
			Position = town.Settlement.GatePosition;
		}
		if (IsMainParty && SiegeEvent.IsBlockadeActive)
		{
			Anchor.IsDisabled = true;
		}
		CampaignEventDispatcher.Instance.OnMobilePartyJoinedToSiegeEvent(this);
	}

	private void OnPartyLeftSiegeInternal()
	{
		if (IsMainParty && SiegeEvent.IsBlockadeActive)
		{
			Anchor.IsDisabled = false;
		}
		_besiegerCamp.RemoveSiegePartyInternal(this);
		EventPositionAdder = Vec2.Zero;
		CampaignEventDispatcher.Instance.OnMobilePartyLeftSiegeEvent(this);
	}

	public bool HasPerk(PerkObject perk, bool checkSecondaryRole = false)
	{
		switch (checkSecondaryRole ? perk.SecondaryRole : perk.PrimaryRole)
		{
		case PartyRole.Scout:
			return EffectiveScout?.GetPerkValue(perk) ?? false;
		case PartyRole.Engineer:
			return EffectiveEngineer?.GetPerkValue(perk) ?? false;
		case PartyRole.Quartermaster:
			return EffectiveQuartermaster?.GetPerkValue(perk) ?? false;
		case PartyRole.Surgeon:
			return EffectiveSurgeon?.GetPerkValue(perk) ?? false;
		case PartyRole.FirstMate:
			return EffectiveFirstMate?.GetPerkValue(perk) ?? false;
		case PartyRole.Navigator:
			return EffectiveNavigator?.GetPerkValue(perk) ?? false;
		case PartyRole.PartyLeader:
			return LeaderHero?.GetPerkValue(perk) ?? false;
		case PartyRole.ArmyCommander:
			return Army?.LeaderParty?.LeaderHero?.GetPerkValue(perk) ?? false;
		case PartyRole.PartyMember:
			foreach (TroopRosterElement item in MemberRoster.GetTroopRoster())
			{
				if (item.Character.IsHero && item.Character.HeroObject.GetPerkValue(perk))
				{
					return true;
				}
			}
			return false;
		case PartyRole.Personal:
			Debug.FailedAssert("personal perk is called in party", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Party\\MobileParty.cs", "HasPerk", 3195);
			return LeaderHero?.GetPerkValue(perk) ?? false;
		case PartyRole.ClanLeader:
			if (LeaderHero != null)
			{
				return LeaderHero.Clan?.Leader?.GetPerkValue(perk) ?? false;
			}
			return false;
		default:
			return false;
		}
	}

	public void SetHeroPartyRole(Hero hero, PartyRole partyRole)
	{
		switch (partyRole)
		{
		case PartyRole.Surgeon:
			SetPartySurgeon(hero);
			break;
		case PartyRole.Engineer:
			SetPartyEngineer(hero);
			break;
		case PartyRole.Scout:
			SetPartyScout(hero);
			break;
		case PartyRole.Quartermaster:
			SetPartyQuartermaster(hero);
			break;
		case PartyRole.FirstMate:
			SetPartyFirstMate(hero);
			break;
		case PartyRole.Navigator:
			SetPartyNavigator(hero);
			break;
		case PartyRole.None:
		case PartyRole.Ruler:
		case PartyRole.ClanLeader:
		case PartyRole.Governor:
		case PartyRole.ArmyCommander:
		case PartyRole.PartyLeader:
		case PartyRole.PartyOwner:
		case PartyRole.PartyMember:
		case PartyRole.Personal:
		case PartyRole.Captain:
		case PartyRole.NumberOfPartyRoles:
			break;
		}
	}

	public void RemoveAllPartyRolesOfHero(Hero hero)
	{
		if (Quartermaster == hero)
		{
			Quartermaster = null;
		}
		if (Scout == hero)
		{
			Scout = null;
		}
		if (Surgeon == hero)
		{
			Surgeon = null;
		}
		if (Engineer == hero)
		{
			Engineer = null;
		}
		if (FirstMate == hero)
		{
			FirstMate = null;
		}
		if (Navigator == hero)
		{
			Navigator = null;
		}
		ResetCached();
	}

	public List<PartyRole> GetHeroPartyRoles(Hero hero)
	{
		List<PartyRole> list = new List<PartyRole>();
		if (hero != null)
		{
			if (Engineer == hero)
			{
				list.Add(PartyRole.Engineer);
			}
			if (Quartermaster == hero)
			{
				list.Add(PartyRole.Quartermaster);
			}
			if (Surgeon == hero)
			{
				list.Add(PartyRole.Surgeon);
			}
			if (Scout == hero)
			{
				list.Add(PartyRole.Scout);
			}
			if (FirstMate == hero)
			{
				list.Add(PartyRole.FirstMate);
			}
			if (Navigator == hero)
			{
				list.Add(PartyRole.Navigator);
			}
		}
		return list;
	}

	public void RemovePartyRoleOfHero(Hero hero, PartyRole partyRole)
	{
		switch (partyRole)
		{
		case PartyRole.Quartermaster:
			Quartermaster = null;
			break;
		case PartyRole.Scout:
			Scout = null;
			break;
		case PartyRole.Surgeon:
			Surgeon = null;
			break;
		case PartyRole.Engineer:
			Engineer = null;
			break;
		case PartyRole.FirstMate:
			FirstMate = null;
			break;
		case PartyRole.Navigator:
			Navigator = null;
			break;
		}
	}

	public void RemoveOnePartyRoleOfHero(Hero hero)
	{
		bool flag = false;
		if (Quartermaster == hero && !flag)
		{
			Quartermaster = null;
			flag = true;
		}
		if (Scout == hero && !flag)
		{
			Scout = null;
			flag = true;
		}
		if (Surgeon == hero && !flag)
		{
			Surgeon = null;
			flag = true;
		}
		if (Engineer == hero && !flag)
		{
			Engineer = null;
			flag = true;
		}
		if (FirstMate == hero && !flag)
		{
			FirstMate = null;
			flag = true;
		}
		if (Navigator == hero && !flag)
		{
			Navigator = null;
			flag = true;
		}
		ResetCached();
	}

	public Hero GetRoleHolder(PartyRole partyRole)
	{
		return partyRole switch
		{
			PartyRole.PartyLeader => LeaderHero, 
			PartyRole.Surgeon => Surgeon, 
			PartyRole.Engineer => Engineer, 
			PartyRole.Quartermaster => Quartermaster, 
			PartyRole.Scout => Scout, 
			PartyRole.FirstMate => FirstMate, 
			PartyRole.Navigator => Navigator, 
			_ => null, 
		};
	}

	public Hero GetEffectiveRoleHolder(PartyRole partyRole)
	{
		return partyRole switch
		{
			PartyRole.PartyLeader => LeaderHero, 
			PartyRole.Surgeon => EffectiveSurgeon, 
			PartyRole.Engineer => EffectiveEngineer, 
			PartyRole.Quartermaster => EffectiveQuartermaster, 
			PartyRole.Scout => EffectiveScout, 
			PartyRole.FirstMate => EffectiveFirstMate, 
			PartyRole.Navigator => EffectiveNavigator, 
			_ => null, 
		};
	}

	public int GetNumDaysForFoodToLast()
	{
		int totalFood = ItemRoster.TotalFood;
		totalFood *= 100;
		if (this == MainParty)
		{
			totalFood += Party.RemainingFoodPercentage;
		}
		return (int)((float)totalFood / (100f * (0f - FoodChange)));
	}

	TextObject ITrackableBase.GetName()
	{
		return Name;
	}

	Vec3 ITrackableBase.GetPosition()
	{
		return GetPositionAsVec3();
	}

	Banner ITrackableCampaignObject.GetBanner()
	{
		return Banner;
	}

	private Settlement DetermineRelatedBesiegedSettlementWhileDestroyingParty()
	{
		Settlement settlement = BesiegedSettlement;
		if (settlement == null)
		{
			settlement = ((ShortTermBehavior == AiBehavior.AssaultSettlement) ? ShortTermTargetSettlement : null);
		}
		if (settlement == null && (IsGarrison || IsMilitia) && CurrentSettlement != null)
		{
			MapEvent mapEvent = CurrentSettlement.LastAttackerParty?.MapEvent;
			if (mapEvent != null && (mapEvent.IsSiegeAssault || mapEvent.IsSiegeOutside || mapEvent.IsSallyOut) && mapEvent.DefeatedSide != BattleSideEnum.None && mapEvent.State == MapEventState.WaitingRemoval)
			{
				settlement = CurrentSettlement;
			}
		}
		return settlement;
	}

	internal void RemoveParty()
	{
		IsActive = false;
		IsVisible = false;
		Settlement settlement = DetermineRelatedBesiegedSettlementWhileDestroyingParty();
		Campaign current = Campaign.Current;
		AttachedTo = null;
		BesiegerCamp = null;
		ReleaseHeroPrisoners();
		ItemRoster.Clear();
		MemberRoster.Clear();
		PrisonRoster.Clear();
		for (int num = Ships.Count - 1; num >= 0; num--)
		{
			DestroyShipAction.Apply(Ships[num]);
		}
		Campaign.Current.MobilePartyLocator.RemoveLocatable(this);
		Campaign.Current.VisualTrackerManager.RemoveTrackedObject(this, forceRemove: true);
		CampaignEventDispatcher.Instance.OnPartyRemoved(Party);
		GC.SuppressFinalize(Party);
		foreach (MobileParty mobileParty in current.MobileParties)
		{
			bool flag = (mobileParty.Ai.AiBehaviorPartyBase == Party || (mobileParty.TargetSettlement != null && mobileParty.TargetSettlement == settlement && mobileParty.CurrentSettlement != settlement) || (mobileParty.ShortTermTargetSettlement != null && mobileParty.ShortTermTargetSettlement == settlement && mobileParty.CurrentSettlement != settlement)) && !mobileParty.IsInRaftState && mobileParty.MapEvent == null;
			if (mobileParty.TargetParty != null && mobileParty.TargetParty == this)
			{
				flag = true;
			}
			if (flag && mobileParty.TargetSettlement != null && (mobileParty.MapEvent == null || mobileParty.MapEvent.IsFinalized) && mobileParty.DefaultBehavior == AiBehavior.GoToSettlement)
			{
				_ = mobileParty.TargetSettlement;
				mobileParty.SetMoveModeHold();
				mobileParty.SetNavigationModeHold();
				mobileParty.Ai.RethinkAtNextHourlyTick = true;
				flag = false;
			}
			if (flag)
			{
				mobileParty.SetMoveModeHold();
				mobileParty.SetNavigationModeHold();
			}
		}
		OnRemoveParty();
		_customHomeSettlement = null;
	}

	private void ReleaseHeroPrisoners()
	{
		for (int num = PrisonRoster.Count - 1; num >= 0; num--)
		{
			if (PrisonRoster.GetElementNumber(num) > 0)
			{
				TroopRosterElement elementCopyAtIndex = PrisonRoster.GetElementCopyAtIndex(num);
				if (elementCopyAtIndex.Character.IsHero && !elementCopyAtIndex.Character.IsPlayerCharacter)
				{
					EndCaptivityAction.ApplyByReleasedByChoice(elementCopyAtIndex.Character.HeroObject);
				}
			}
		}
	}

	private void CreateFigureAux(CampaignVec2 position)
	{
		_ = Campaign.Current.MapSceneWrapper;
		Position = position;
		Vec2 bearing = new Vec2(1f, 0f);
		float angleInRadians = MBRandom.RandomFloat * 2f * System.MathF.PI;
		bearing.RotateCCW(angleInRadians);
		Bearing = bearing;
		Party.UpdateVisibilityAndInspected(MainParty.Position);
		StartUp();
	}

	private void CreateFigure(CampaignVec2 position)
	{
		CreateFigureAux(position);
	}

	internal void TeleportPartyToOutSideOfEncounterRadius()
	{
		float num = 0f;
		num = (IsCurrentlyAtSea ? ((Army == null || AttachedTo == null) ? Campaign.Current.Models.EncounterModel.NeededMaximumNavalDistanceForEncounteringMobileParty : Campaign.Current.Models.EncounterModel.MaximumAllowedNavalDistanceForEncounteringMobilePartyInArmy) : ((Army == null || AttachedTo == null) ? Campaign.Current.Models.EncounterModel.NeededMaximumLandDistanceForEncounteringMobileParty : Campaign.Current.Models.EncounterModel.MaximumAllowedLandDistanceForEncounteringMobilePartyInArmy));
		float num2 = num;
		float maxDistance = num2 * 1.25f;
		NavigationType navigationCapability = ((!IsCurrentlyAtSea) ? NavigationType.Default : NavigationType.Naval);
		if (IsCurrentlyAtSea && !HasNavalNavigationCapability)
		{
			return;
		}
		for (int i = 0; i < 15; i++)
		{
			CampaignVec2 position = NavigationHelper.FindReachablePointAroundPosition(Position, navigationCapability, maxDistance, num2);
			bool flag = true;
			LocatableSearchData<MobileParty> data = StartFindingLocatablesAroundPosition(position.ToVec2(), num2);
			for (MobileParty mobileParty = FindNextLocatable(ref data); mobileParty != null; mobileParty = FindNextLocatable(ref data))
			{
				if (mobileParty.MapFaction.IsAtWarWith(MapFaction))
				{
					flag = false;
					break;
				}
			}
			if (!flag)
			{
				continue;
			}
			Position = position;
			{
				foreach (MobileParty attachedParty in AttachedParties)
				{
					attachedParty.Position = position;
					attachedParty.ArmyPositionAdder = Vec2.Zero;
					attachedParty.EventPositionAdder = Vec2.Zero;
				}
				break;
			}
		}
	}

	private void DoAiPathMode(ref CachedPartyVariables variables)
	{
		if (variables.IsAttachedArmyMember)
		{
			_pathMode = false;
			return;
		}
		DoAIMove(ref variables);
		if (IsTransitionInProgress || !_pathMode)
		{
			return;
		}
		bool flag;
		do
		{
			flag = false;
			NextTargetPosition = new CampaignVec2(Path[PathBegin], !IsCurrentlyAtSea);
			float lengthSquared = (NextTargetPosition.ToVec2() - variables.CurrentPosition.ToVec2()).LengthSquared;
			float num = variables.NextMoveDistance * variables.NextMoveDistance;
			if (lengthSquared < num)
			{
				flag = true;
				variables.NextMoveDistance -= TaleWorlds.Library.MathF.Sqrt(lengthSquared);
				variables.LastCurrentPosition = variables.CurrentPosition;
				variables.CurrentPosition = NextTargetPosition;
				PathBegin++;
			}
		}
		while (flag && PathBegin < Path.Size);
		if (PathBegin >= Path.Size)
		{
			_pathMode = false;
			if (Path.Size > 0)
			{
				variables.CurrentPosition = variables.LastCurrentPosition;
				NextTargetPosition = new CampaignVec2(Path[Path.Size - 1], !IsCurrentlyAtSea);
			}
		}
	}

	public bool RecalculateLongTermPath()
	{
		NavigationType navigationType = DesiredAiNavigationType;
		if (navigationType == NavigationType.None)
		{
			navigationType = ((!IsCurrentlyAtSea) ? NavigationType.Default : NavigationType.Naval);
		}
		if ((navigationType == NavigationType.Naval && !IsCurrentlyAtSea) || (navigationType == NavigationType.Default && IsCurrentlyAtSea))
		{
			navigationType = NavigationType.All;
		}
		CampaignVec2 newTargetPosition = ((TargetSettlement != null) ? (IsTargetingPort ? TargetSettlement.PortPosition : TargetSettlement.GatePosition) : ((TargetParty == null) ? TargetPosition : TargetParty.Position));
		if ((!newTargetPosition.IsOnLand || DesiredAiNavigationType != NavigationType.Naval) && (newTargetPosition.IsOnLand || DesiredAiNavigationType != NavigationType.Default))
		{
			return ComputePath(newTargetPosition, navigationType, cacheNextLongTermPathPoint: true);
		}
		return false;
	}

	private bool ComputePath(CampaignVec2 newTargetPosition, NavigationType navigationType, bool cacheNextLongTermPathPoint)
	{
		bool flag = false;
		if (Position.IsValid())
		{
			if (newTargetPosition.IsValid())
			{
				TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(newTargetPosition.Face);
				if (!Campaign.Current.Models.PartyNavigationModel.IsTerrainTypeValidForNavigationType(faceTerrainType, NavigationCapability) && !IsInRaftState)
				{
					return false;
				}
				CampaignVec2 position = Position;
				if (IsMainParty && MainParty.IsCurrentlyAtSea && NavigationHelper.IsPositionValidForNavigationType(newTargetPosition, NavigationType.Naval))
				{
					navigationType = NavigationType.Naval;
				}
				float agentRadius = (IsCurrentlyAtSea ? Campaign.Current.Models.PartyNavigationModel.GetEmbarkDisembarkThresholdDistance() : 0.3f);
				int[] invalidTerrainTypesForNavigationType = Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(navigationType);
				flag = Campaign.Current.MapSceneWrapper.GetPathBetweenAIFaces(CurrentNavigationFace, newTargetPosition.Face, position.ToVec2(), newTargetPosition.ToVec2(), agentRadius, Path, invalidTerrainTypesForNavigationType, 2f, GetRegionSwitchCostFromSeaToLand(), GetRegionSwitchCostFromLandToSea());
				if (cacheNextLongTermPathPoint)
				{
					CampaignVec2 nextLongTermPathPoint = new CampaignVec2(Path.PathPoints[0], !IsCurrentlyAtSea);
					if (!nextLongTermPathPoint.IsValid())
					{
						nextLongTermPathPoint = new CampaignVec2(Path.PathPoints[0], IsCurrentlyAtSea);
					}
					NextLongTermPathPoint = nextLongTermPathPoint;
				}
			}
			else
			{
				Debug.FailedAssert("Path finding target is not valid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Party\\MobileParty.cs", "ComputePath", 3899);
			}
		}
		PathBegin = 0;
		if (!flag)
		{
			_pathMode = false;
		}
		return flag;
	}

	public int GetRegionSwitchCostFromLandToSea()
	{
		if (IsMainParty)
		{
			return Campaign.PlayerRegionSwitchCostFromLandToSea;
		}
		return Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromLandToSea;
	}

	public int GetRegionSwitchCostFromSeaToLand()
	{
		return Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromSeaToLand;
	}

	private void DoAIMove(ref CachedPartyVariables variables)
	{
		GetTargetCampaignPosition(ref variables, out var finalTargetPosition, out var forceNoPathMode);
		PathFaceRecord face = finalTargetPosition.Face;
		if (_pathMode && (face.FaceIndex != PathLastFace.FaceIndex || finalTargetPosition.Distance(_pathLastPosition) > 1E-05f))
		{
			_pathMode = false;
			PathLastFace = PathFaceRecord.NullFaceRecord;
		}
		if (!_pathMode && (face.FaceIndex != PathLastFace.FaceIndex || finalTargetPosition.Distance(_pathLastPosition) > 1E-05f) && _aiPathNotFound)
		{
			_aiPathNotFound = false;
		}
		if (IsComputePathCacheDirty(finalTargetPosition, forceNoPathMode, out var navigationType))
		{
			if (CurrentNavigationFace.FaceIndex != MoveTargetPoint.Face.FaceIndex)
			{
				_aiPathNotFound = !ComputePath(finalTargetPosition, navigationType, !IsFleeing());
				if (!_aiPathNotFound)
				{
					PathLastFace = face;
					_pathLastPosition = finalTargetPosition;
					_lastComputedPathNavigationType = navigationType;
					_pathMode = true;
				}
			}
			else
			{
				_pathMode = false;
				PathLastFace = PathFaceRecord.NullFaceRecord;
			}
		}
		else if (face.FaceIndex == PathLastFace.FaceIndex && CurrentNavigationFace.FaceIndex != face.FaceIndex)
		{
			_pathMode = true;
		}
		if (!_pathMode)
		{
			NextTargetPosition = finalTargetPosition;
		}
	}

	private bool IsComputePathCacheDirty(CampaignVec2 finalTargetPosition, bool forceNoPathMode, out NavigationType navigationType)
	{
		navigationType = DesiredAiNavigationType;
		TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(finalTargetPosition.Face);
		if (!StartTransitionNextFrameToExitFromPort && (DesiredAiNavigationType == NavigationType.Naval != IsCurrentlyAtSea || !Campaign.Current.Models.PartyNavigationModel.IsTerrainTypeValidForNavigationType(faceTerrainType, DesiredAiNavigationType)))
		{
			navigationType = NavigationType.All;
		}
		if (_lastComputedPathNavigationType == navigationType && (_pathMode || forceNoPathMode || _aiPathNotFound))
		{
			if (finalTargetPosition.Face.FaceIndex != PathLastFace.FaceIndex)
			{
				return finalTargetPosition.IsValid();
			}
			return false;
		}
		return true;
	}

	private void GetTargetCampaignPosition(ref CachedPartyVariables variables, out CampaignVec2 finalTargetPosition, out bool forceNoPathMode)
	{
		finalTargetPosition = Position;
		forceNoPathMode = false;
		if (PartyMoveMode == MoveModeType.Point)
		{
			finalTargetPosition = MoveTargetPoint;
			forceNoPathMode = ForceAiNoPathMode;
		}
		else
		{
			if (PartyMoveMode != MoveModeType.Party || !MoveTargetParty.Party.IsValid)
			{
				return;
			}
			finalTargetPosition = variables.TargetPartyPositionAtFrameStart;
			bool flag = !NavigationHelper.IsPositionValidForNavigationType(variables.TargetPartyPositionAtFrameStart, NavigationCapability);
			if (flag && !variables.IsTargetMovingAtFrameStart)
			{
				variables.IsTargetMovingAtFrameStart = true;
			}
			if (variables.IsTargetMovingAtFrameStart && flag)
			{
				finalTargetPosition = Position;
				forceNoPathMode = false;
				if (!Ai.IsDisabled && !IsMainParty)
				{
					Ai.DefaultBehaviorNeedsUpdate = true;
				}
			}
		}
	}

	private void DoUpdatePosition(ref CachedPartyVariables variables, float dt, float realDt)
	{
		Vec2 vec;
		if (variables.IsAttachedArmyMember)
		{
			if (variables.HasMapEvent || CurrentSettlement != null)
			{
				vec = Vec2.Zero;
			}
			else
			{
				Vec2 vec2 = (variables.HasMapEvent ? Army.LeaderParty.Position.ToVec2() : Army.LeaderParty.NextTargetPosition.ToVec2());
				Army.LeaderParty.GetTargetCampaignPosition(ref variables, out var finalTargetPosition, out var _);
				Vec2 armyFacing = (((vec2 - Army.LeaderParty.Position.ToVec2()).LengthSquared < 0.0025000002f) ? Army.LeaderParty.Bearing.Normalized() : (vec2 - Army.LeaderParty.Position.ToVec2()).Normalized());
				Vec2 vec3 = armyFacing.TransformToParentUnitF(Army.GetRelativePositionForParty(this, armyFacing));
				vec = vec2 + vec3 - VisualPosition2DWithoutError;
				if ((finalTargetPosition.ToVec2() + vec3 - VisualPosition2DWithoutError).LengthSquared < 1.0000001E-06f || vec.LengthSquared < 1.0000001E-06f)
				{
					vec = Vec2.Zero;
				}
				float num = vec.LeftVec().Normalized().DotProduct(Army.LeaderParty.Position.ToVec2() + vec3 - VisualPosition2DWithoutError);
				vec.RotateCCW((num < 0f) ? TaleWorlds.Library.MathF.Max(num * 2f, -System.MathF.PI / 4f) : TaleWorlds.Library.MathF.Min(num * 2f, System.MathF.PI / 4f));
			}
		}
		else
		{
			vec = (variables.HasMapEvent ? Party.MapEvent.Position.ToVec2() : NextTargetPosition.ToVec2()) - VisualPosition2DWithoutError;
		}
		float num2 = vec.Normalize();
		if (num2 < variables.NextMoveDistance)
		{
			variables.NextMoveDistance = num2;
		}
		if (BesiegedSettlement != null || CurrentSettlement != null || (!(variables.NextMoveDistance > 0f) && !variables.HasMapEvent))
		{
			return;
		}
		Vec2 vec4 = Bearing;
		if (num2 > 0f)
		{
			vec4 = vec;
			if (!variables.IsAttachedArmyMember || !variables.HasMapEvent)
			{
				Bearing = vec4;
			}
		}
		else if (variables.IsAttachedArmyMember && variables.HasMapEvent)
		{
			vec4 = (Bearing = Army.LeaderParty.Bearing);
		}
		variables.NextPosition = variables.CurrentPosition + vec4 * variables.NextMoveDistance;
	}

	private void ResetAllMovementParameters()
	{
		TargetParty = null;
		SetTargetSettlement(null, isTargetingPort: false);
		DefaultBehavior = AiBehavior.None;
		SetShortTermBehavior(AiBehavior.None, null);
		TargetPosition = Position;
		MoveTargetPoint = Position;
	}

	public void SetMoveModeHold()
	{
		ResetAllMovementParameters();
		DefaultBehavior = AiBehavior.Hold;
		SetShortTermBehavior(AiBehavior.Hold, null);
		SetTargetSettlement(null, isTargetingPort: false);
		TargetParty = null;
		TargetPosition = Position;
		MoveTargetPoint = Position;
		DesiredAiNavigationType = NavigationType.None;
	}

	public void SetMoveEngageParty(MobileParty party, NavigationType navigationType)
	{
		ResetAllMovementParameters();
		TargetParty = party;
		MoveTargetPoint = party.Position;
		DesiredAiNavigationType = navigationType;
		DefaultBehavior = AiBehavior.EngageParty;
	}

	public void SetMoveGoAroundParty(MobileParty party, NavigationType navigationType)
	{
		ResetAllMovementParameters();
		TargetParty = party;
		MoveTargetPoint = party.Position;
		DesiredAiNavigationType = navigationType;
		DefaultBehavior = AiBehavior.GoAroundParty;
	}

	public void SetMoveGoToSettlement(Settlement settlement, NavigationType navigationType, bool isTargetingThePort)
	{
		ResetAllMovementParameters();
		SetTargetSettlement(settlement, isTargetingThePort);
		CampaignVec2 moveTargetPoint = (TargetPosition = (isTargetingThePort ? settlement.PortPosition : settlement.GatePosition));
		MoveTargetPoint = moveTargetPoint;
		DesiredAiNavigationType = navigationType;
		DefaultBehavior = AiBehavior.GoToSettlement;
	}

	public void SetMoveGoToPoint(CampaignVec2 point, NavigationType navigationType)
	{
		ResetAllMovementParameters();
		TargetPosition = point;
		MoveTargetPoint = point;
		DesiredAiNavigationType = navigationType;
		Ai.AiBehaviorInteractable = null;
		DefaultBehavior = AiBehavior.GoToPoint;
	}

	public void SetMoveToNearestLand(Settlement settlement)
	{
		ResetAllMovementParameters();
		if (settlement != null)
		{
			SetTargetSettlement(settlement, isTargetingPort: true);
		}
		DesiredAiNavigationType = (HasLandNavigationCapability ? NavigationType.All : NavigationType.Naval);
		DefaultBehavior = AiBehavior.MoveToNearestLandOrPort;
	}

	public void SetMoveGoToInteractablePoint(IInteractablePoint point, NavigationType navigationType)
	{
		ResetAllMovementParameters();
		TargetPosition = point.GetInteractionPosition(this);
		MoveTargetPoint = TargetPosition;
		Ai.AiBehaviorInteractable = point;
		DesiredAiNavigationType = navigationType;
		DefaultBehavior = AiBehavior.GoToPoint;
	}

	public void SetMoveEscortParty(MobileParty mobileParty, NavigationType navigationType, bool isTargetingPort)
	{
		ResetAllMovementParameters();
		TargetParty = mobileParty;
		MoveTargetPoint = mobileParty.Position;
		if (isTargetingPort)
		{
			SetTargetSettlement(mobileParty.CurrentSettlement, isTargetingPort: true);
		}
		DesiredAiNavigationType = navigationType;
		DefaultBehavior = AiBehavior.EscortParty;
	}

	public void SetMovePatrolAroundPoint(CampaignVec2 point, NavigationType navigationType)
	{
		ResetAllMovementParameters();
		TargetPosition = point;
		MoveTargetPoint = point;
		DesiredAiNavigationType = navigationType;
		DefaultBehavior = AiBehavior.PatrolAroundPoint;
	}

	public void SetMovePatrolAroundSettlement(Settlement settlement, NavigationType navigationType, bool isTargetingPort)
	{
		SetMovePatrolAroundPoint(isTargetingPort ? settlement.PortPosition : settlement.GatePosition, navigationType);
		SetTargetSettlement(settlement, isTargetingPort);
	}

	public void SetMoveRaidSettlement(Settlement settlement, NavigationType navigationType, bool isTargetingPort)
	{
		ResetAllMovementParameters();
		SetTargetSettlement(settlement, isTargetingPort);
		CampaignVec2 moveTargetPoint = (TargetPosition = (isTargetingPort ? settlement.PortPosition : settlement.GatePosition));
		MoveTargetPoint = moveTargetPoint;
		DesiredAiNavigationType = navigationType;
		DefaultBehavior = AiBehavior.RaidSettlement;
	}

	public void SetMoveBesiegeSettlement(Settlement settlement, NavigationType navigationType)
	{
		ResetAllMovementParameters();
		if (BesiegedSettlement != null && BesiegedSettlement != settlement)
		{
			BesiegerCamp = null;
		}
		SetTargetSettlement(settlement, isTargetingPort: false);
		DesiredAiNavigationType = navigationType;
		DefaultBehavior = AiBehavior.BesiegeSettlement;
	}

	public void SetMoveDefendSettlement(Settlement settlement, bool isTargetingPort, NavigationType navigationType)
	{
		ResetAllMovementParameters();
		SetTargetSettlement(settlement, isTargetingPort);
		TargetPosition = (isTargetingPort ? settlement.PortPosition : settlement.GatePosition);
		DesiredAiNavigationType = navigationType;
		DefaultBehavior = AiBehavior.DefendSettlement;
	}

	internal void SetNavigationModeHold()
	{
		PartyMoveMode = MoveModeType.Hold;
		_pathMode = false;
		NextTargetPosition = Position;
		MoveTargetParty = null;
	}

	internal void SetNavigationModePoint(CampaignVec2 newTargetPosition)
	{
		PartyMoveMode = MoveModeType.Point;
		UpdatePathModeWithPosition(newTargetPosition);
		_aiPathNotFound = false;
		MoveTargetParty = null;
	}

	internal void SetNavigationModeParty(MobileParty targetParty)
	{
		PartyMoveMode = MoveModeType.Party;
		MoveTargetParty = targetParty;
		MoveTargetPoint = targetParty.Position;
		_aiPathNotFound = false;
	}

	public static LocatableSearchData<MobileParty> StartFindingLocatablesAroundPosition(Vec2 position, float radius)
	{
		return Campaign.Current.MobilePartyLocator.StartFindingLocatablesAroundPosition(position, radius);
	}

	public static MobileParty FindNextLocatable(ref LocatableSearchData<MobileParty> data)
	{
		return Campaign.Current.MobilePartyLocator.FindNextLocatable(ref data);
	}

	public static void UpdateLocator(MobileParty party)
	{
		Campaign.Current.MobilePartyLocator.UpdateLocator(party);
	}

	internal void OnHeroAdded(Hero hero)
	{
		hero.OnAddedToParty(this);
	}

	internal void OnHeroRemoved(Hero hero)
	{
		hero.OnRemovedFromParty(this);
	}

	internal void CheckExitingSettlementParallel(ref int exitingPartyCount, ref MobileParty[] exitingPartyList, ref int gridChangeCount, ref MobileParty[] gridChangeMobilePartyList)
	{
		if (Ai.IsDisabled || ShortTermBehavior == AiBehavior.Hold || CurrentSettlement == null || ((ShortTermTargetSettlement != null || TargetSettlement == CurrentSettlement) && ShortTermTargetSettlement == CurrentSettlement) || IsMainParty || (Army != null && AttachedTo != null && Army.LeaderParty != this))
		{
			return;
		}
		if (StartTransitionNextFrameToExitFromPort)
		{
			StartTransitionNextFrameToExitFromPort = false;
			if (!IsCurrentlyAtSea)
			{
				InitializeNavigationTransitionParallel(CurrentSettlement.PortPosition, CurrentSettlement.PortPosition, ref gridChangeCount, ref gridChangeMobilePartyList);
			}
		}
		else if (!IsTransitionInProgress)
		{
			int num = Interlocked.Increment(ref exitingPartyCount);
			exitingPartyList[num] = this;
		}
	}

	public bool ComputeIsWaiting()
	{
		CampaignVec2 campaignVec = MoveTargetParty?.Position ?? MoveTargetPoint;
		if (!(((2f * Position).ToVec2() - campaignVec.ToVec2() - NextTargetPosition.ToVec2()).LengthSquared < 1E-05f) && DefaultBehavior != AiBehavior.Hold)
		{
			if ((DefaultBehavior == AiBehavior.EngageParty || DefaultBehavior == AiBehavior.EscortParty) && Ai.AiBehaviorPartyBase != null && Ai.AiBehaviorPartyBase.IsValid && Ai.AiBehaviorPartyBase.IsActive && Ai.AiBehaviorPartyBase.IsMobile)
			{
				return Ai.AiBehaviorPartyBase.MobileParty.CurrentSettlement != null;
			}
			return false;
		}
		return true;
	}

	public void InitializePartyTrade(int initialGold)
	{
		IsPartyTradeActive = true;
		PartyTradeGold = initialGold;
	}

	public void AddTaxGold(int amount)
	{
		PartyTradeTaxGold += amount;
	}

	public static MobileParty CreateParty(string stringId, PartyComponent component)
	{
		stringId = Campaign.Current.CampaignObjectManager.FindNextUniqueStringId<MobileParty>(stringId);
		MobileParty mobileParty = new MobileParty();
		mobileParty.StringId = stringId;
		mobileParty._partyComponent = component;
		mobileParty.UpdatePartyComponentFlags();
		mobileParty._partyComponent?.Create(mobileParty);
		mobileParty._partyComponent?.Initialize(mobileParty);
		Campaign.Current.CampaignObjectManager.AddMobileParty(mobileParty);
		CampaignEventDispatcher.Instance.OnMobilePartyCreated(mobileParty);
		CampaignEventDispatcher.Instance.OnMapInteractableCreated(mobileParty.Party);
		return mobileParty;
	}

	public void SetPartyComponent(PartyComponent partyComponent, bool firstTimePartyComponentCreation = true)
	{
		if (_partyComponent == partyComponent)
		{
			return;
		}
		if (_partyComponent != null)
		{
			_partyComponent.Finish();
		}
		Campaign.Current.CampaignObjectManager.BeforePartyComponentChanged(this);
		_partyComponent = partyComponent;
		UpdatePartyComponentFlags();
		Campaign.Current.CampaignObjectManager.AfterPartyComponentChanged(this);
		if (_partyComponent != null)
		{
			if (firstTimePartyComponentCreation)
			{
				_partyComponent.Create(this);
			}
			_partyComponent.Initialize(this);
		}
		Party.SetVisualAsDirty();
	}

	public void UpdatePartyComponentFlags()
	{
		IsLordParty = _partyComponent is LordPartyComponent;
		IsVillager = _partyComponent is VillagerPartyComponent;
		IsMilitia = _partyComponent is MilitiaPartyComponent;
		IsCaravan = _partyComponent is CaravanPartyComponent;
		IsPatrolParty = _partyComponent is PatrolPartyComponent;
		IsGarrison = _partyComponent is GarrisonPartyComponent;
		IsCustomParty = _partyComponent is CustomPartyComponent;
		IsBandit = _partyComponent is BanditPartyComponent;
	}

	[SpecialName]
	bool ITrackableCampaignObject.get_IsReady()
	{
		return base.IsReady;
	}
}
