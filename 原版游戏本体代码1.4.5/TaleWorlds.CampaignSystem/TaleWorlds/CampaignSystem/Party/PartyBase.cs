using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Load;

namespace TaleWorlds.CampaignSystem.Party;

public sealed class PartyBase : IBattleCombatant, IRandomOwner, IInteractablePoint
{
	[SaveableField(15)]
	private int _remainingFoodPercentage;

	[SaveableField(182)]
	private CampaignTime _lastEatingTime = CampaignTime.Now;

	[SaveableField(8)]
	private Hero _customOwner;

	[SaveableField(9)]
	private int _index;

	[SaveableField(200)]
	private MapEventSide _mapEventSide;

	[CachedData]
	private int _partyMemberSizeLastCheckVersion;

	[CachedData]
	private int _cachedPartyMemberSizeLimit;

	[CachedData]
	private int _prisonerSizeLastCheckVersion;

	[CachedData]
	private int _cachedPrisonerSizeLimit;

	[CachedData]
	private int _lastNumberOfMenWithHorseVersionNo;

	[CachedData]
	private int _lastNumberOfMenPerTierVersionNo;

	[SaveableField(17)]
	private int _numberOfMenWithHorse;

	private int[] _numberOfHealthyMenPerTier;

	[CachedData]
	private int _lastEstimatedStrengthVersionNo = -1;

	[CachedData]
	private float _cachedEstimatedStrength;

	[SaveableField(20)]
	private MBList<Ship> _ships = new MBList<Ship>();

	public CampaignVec2 Position
	{
		get
		{
			if (!IsMobile)
			{
				return Settlement.Position;
			}
			return MobileParty.Position;
		}
	}

	public bool IsVisible
	{
		get
		{
			if (!IsMobile)
			{
				return Settlement.IsVisible;
			}
			return MobileParty.IsVisible;
		}
	}

	public bool IsActive
	{
		get
		{
			if (!IsMobile)
			{
				return Settlement.IsActive;
			}
			return MobileParty.IsActive;
		}
	}

	public SiegeEvent SiegeEvent
	{
		get
		{
			if (!IsMobile)
			{
				return Settlement.SiegeEvent;
			}
			return MobileParty.SiegeEvent;
		}
	}

	[SaveableProperty(1)]
	public Settlement Settlement { get; private set; }

	[SaveableProperty(2)]
	public MobileParty MobileParty { get; private set; }

	public bool IsSettlement => Settlement != null;

	public bool IsMobile => MobileParty != null;

	[SaveableProperty(3)]
	public TroopRoster MemberRoster { get; private set; }

	[SaveableProperty(4)]
	public TroopRoster PrisonRoster { get; private set; }

	[SaveableProperty(5)]
	public ItemRoster ItemRoster { get; private set; }

	public TextObject Name
	{
		get
		{
			if (!IsSettlement)
			{
				if (!IsMobile)
				{
					return TextObject.GetEmpty();
				}
				return MobileParty.Name;
			}
			return Settlement.Name;
		}
	}

	public float DaysStarving
	{
		get
		{
			if (!IsStarving)
			{
				return 0f;
			}
			return _lastEatingTime.ElapsedDaysUntilNow;
		}
	}

	public int RemainingFoodPercentage
	{
		get
		{
			return _remainingFoodPercentage;
		}
		set
		{
			_remainingFoodPercentage = value;
		}
	}

	public bool IsStarving => _remainingFoodPercentage < 0;

	public string Id => MobileParty?.StringId ?? Settlement.StringId;

	public float HealingRateForMemberRegulars => Campaign.Current.Models.PartyHealingModel.GetDailyHealingForRegulars(this, isPrisoner: false).ResultNumber;

	public ExplainedNumber HealingRateForMemberRegularsExplained => Campaign.Current.Models.PartyHealingModel.GetDailyHealingForRegulars(this, isPrisoner: false, includeDescriptions: true);

	public float HealingRateForMemberHeroes => Campaign.Current.Models.PartyHealingModel.GetDailyHealingHpForHeroes(this, isPrisoners: false).ResultNumber;

	public ExplainedNumber HealingRateForMemberHeroesExplained => Campaign.Current.Models.PartyHealingModel.GetDailyHealingHpForHeroes(this, isPrisoners: false, includeDescriptions: true);

	public Hero Owner
	{
		get
		{
			Hero hero = _customOwner;
			if (hero == null)
			{
				if (!IsMobile)
				{
					return Settlement.Owner;
				}
				hero = MobileParty.Owner;
			}
			return hero;
		}
	}

	public Hero LeaderHero => MobileParty?.LeaderHero;

	public static PartyBase MainParty
	{
		get
		{
			if (Campaign.Current == null)
			{
				return null;
			}
			return Campaign.Current.MainParty.Party;
		}
	}

	public bool LevelMaskIsDirty { get; private set; }

	public int Index
	{
		get
		{
			return _index;
		}
		private set
		{
			_index = value;
		}
	}

	public bool IsValid => Index >= 0;

	public IFaction MapFaction
	{
		get
		{
			if (MobileParty != null)
			{
				return MobileParty.MapFaction;
			}
			if (Settlement != null)
			{
				return Settlement.MapFaction;
			}
			return null;
		}
	}

	[SaveableProperty(210)]
	public int RandomValue { get; private set; } = MBRandom.RandomInt(1, int.MaxValue);

	public CultureObject Culture => MapFaction.Culture;

	public Tuple<uint, uint> PrimaryColorPair
	{
		get
		{
			if (MapFaction == null)
			{
				return new Tuple<uint, uint>(4291609515u, 4291609515u);
			}
			return new Tuple<uint, uint>(MapFaction.Color, MapFaction.Color2);
		}
	}

	[SaveableProperty(216)]
	public TextObject CustomName { get; private set; }

	[SaveableProperty(215)]
	public Banner CustomBanner { get; private set; }

	public Banner Banner
	{
		get
		{
			if (!IsMobile)
			{
				return Settlement.Banner;
			}
			return MobileParty.Banner;
		}
	}

	public MapEvent MapEvent => _mapEventSide?.MapEvent;

	public MapEventSide MapEventSide
	{
		get
		{
			return _mapEventSide;
		}
		set
		{
			if (_mapEventSide == value)
			{
				return;
			}
			if (value != null && IsMobile && MapEvent != null && MapEvent.DefenderSide.LeaderParty == this)
			{
				Debug.FailedAssert($"Double MapEvent For {Name}", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Party\\PartyBase.cs", "MapEventSide", 246);
			}
			if (_mapEventSide != null)
			{
				_mapEventSide.RemovePartyInternal(this);
			}
			_mapEventSide = value;
			if (_mapEventSide != null)
			{
				_mapEventSide.AddPartyInternal(this);
			}
			if (MobileParty == null)
			{
				return;
			}
			if (IsActive && MobileParty.IsTransitionInProgress)
			{
				MobileParty.CancelNavigationTransition();
			}
			foreach (MobileParty attachedParty in MobileParty.AttachedParties)
			{
				attachedParty.Party.MapEventSide = _mapEventSide;
			}
		}
	}

	public BattleSideEnum Side => MapEventSide?.MissionSide ?? BattleSideEnum.None;

	public BattleSideEnum OpponentSide
	{
		get
		{
			if (Side == BattleSideEnum.Attacker)
			{
				return BattleSideEnum.Defender;
			}
			return BattleSideEnum.Attacker;
		}
	}

	public int PartySizeLimit
	{
		get
		{
			int versionNo = MemberRoster.VersionNo;
			if (_partyMemberSizeLastCheckVersion != versionNo || _cachedPartyMemberSizeLimit == 0)
			{
				_partyMemberSizeLastCheckVersion = versionNo;
				_cachedPartyMemberSizeLimit = (int)Campaign.Current.Models.PartySizeLimitModel.GetPartyMemberSizeLimit(this).ResultNumber;
			}
			return _cachedPartyMemberSizeLimit;
		}
	}

	public int PrisonerSizeLimit
	{
		get
		{
			int versionNo = PrisonRoster.VersionNo;
			if (_prisonerSizeLastCheckVersion != versionNo || _cachedPrisonerSizeLimit == 0)
			{
				_prisonerSizeLastCheckVersion = versionNo;
				_cachedPrisonerSizeLimit = (int)Campaign.Current.Models.PartySizeLimitModel.GetPartyPrisonerSizeLimit(this).ResultNumber;
			}
			return _cachedPrisonerSizeLimit;
		}
	}

	public ExplainedNumber PartySizeLimitExplainer => Campaign.Current.Models.PartySizeLimitModel.GetPartyMemberSizeLimit(this, includeDescriptions: true);

	public ExplainedNumber PrisonerSizeLimitExplainer => Campaign.Current.Models.PartySizeLimitModel.GetPartyPrisonerSizeLimit(this, includeDescriptions: true);

	public int NumberOfHealthyMembers => MemberRoster.TotalManCount - MemberRoster.TotalWounded;

	public int NumberOfRegularMembers => MemberRoster.TotalRegulars;

	public int NumberOfWoundedTotalMembers => MemberRoster.TotalWounded;

	public int NumberOfAllMembers => MemberRoster.TotalManCount;

	public int NumberOfPrisoners => PrisonRoster.TotalManCount;

	public int NumberOfMounts => ItemRoster.NumberOfMounts;

	public int NumberOfPackAnimals => ItemRoster.NumberOfPackAnimals;

	public IEnumerable<CharacterObject> PrisonerHeroes
	{
		get
		{
			for (int j = 0; j < PrisonRoster.Count; j++)
			{
				if (PrisonRoster.GetElementNumber(j) > 0)
				{
					TroopRosterElement elementCopyAtIndex = PrisonRoster.GetElementCopyAtIndex(j);
					if (elementCopyAtIndex.Character.IsHero)
					{
						yield return elementCopyAtIndex.Character;
					}
				}
			}
		}
	}

	public int NumberOfMenWithHorse
	{
		get
		{
			if (_lastNumberOfMenWithHorseVersionNo != MemberRoster.VersionNo)
			{
				RecalculateNumberOfMenWithHorses();
				_lastNumberOfMenWithHorseVersionNo = MemberRoster.VersionNo;
			}
			return _numberOfMenWithHorse;
		}
	}

	public int NumberOfMenWithoutHorse => NumberOfAllMembers - NumberOfMenWithHorse;

	public float EstimatedStrength
	{
		get
		{
			if (_lastEstimatedStrengthVersionNo == GetStrengthVersionNo())
			{
				return _cachedEstimatedStrength;
			}
			UpdateEstimatedStrengthCaches();
			return _cachedEstimatedStrength;
		}
	}

	public MBReadOnlyList<Ship> Ships => _ships;

	public Ship FlagShip => Ships.MaxBy((Ship x) => x.FlagshipScore);

	public BasicCultureObject BasicCulture => Culture;

	public BasicCharacterObject General
	{
		get
		{
			if (MobileParty?.Army != null)
			{
				return MobileParty.Army.LeaderParty?.LeaderHero?.CharacterObject;
			}
			return LeaderHero?.CharacterObject;
		}
	}

	[CachedData]
	public bool IsVisualDirty { get; private set; }

	internal static void AutoGeneratedStaticCollectObjectsPartyBase(object o, List<object> collectedObjects)
	{
		((PartyBase)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	private void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(_lastEatingTime, collectedObjects);
		collectedObjects.Add(_customOwner);
		collectedObjects.Add(_mapEventSide);
		collectedObjects.Add(_ships);
		collectedObjects.Add(Settlement);
		collectedObjects.Add(MobileParty);
		collectedObjects.Add(MemberRoster);
		collectedObjects.Add(PrisonRoster);
		collectedObjects.Add(ItemRoster);
		collectedObjects.Add(CustomName);
		collectedObjects.Add(CustomBanner);
	}

	internal static object AutoGeneratedGetMemberValueSettlement(object o)
	{
		return ((PartyBase)o).Settlement;
	}

	internal static object AutoGeneratedGetMemberValueMobileParty(object o)
	{
		return ((PartyBase)o).MobileParty;
	}

	internal static object AutoGeneratedGetMemberValueMemberRoster(object o)
	{
		return ((PartyBase)o).MemberRoster;
	}

	internal static object AutoGeneratedGetMemberValuePrisonRoster(object o)
	{
		return ((PartyBase)o).PrisonRoster;
	}

	internal static object AutoGeneratedGetMemberValueItemRoster(object o)
	{
		return ((PartyBase)o).ItemRoster;
	}

	internal static object AutoGeneratedGetMemberValueRandomValue(object o)
	{
		return ((PartyBase)o).RandomValue;
	}

	internal static object AutoGeneratedGetMemberValueCustomName(object o)
	{
		return ((PartyBase)o).CustomName;
	}

	internal static object AutoGeneratedGetMemberValueCustomBanner(object o)
	{
		return ((PartyBase)o).CustomBanner;
	}

	internal static object AutoGeneratedGetMemberValue_remainingFoodPercentage(object o)
	{
		return ((PartyBase)o)._remainingFoodPercentage;
	}

	internal static object AutoGeneratedGetMemberValue_lastEatingTime(object o)
	{
		return ((PartyBase)o)._lastEatingTime;
	}

	internal static object AutoGeneratedGetMemberValue_customOwner(object o)
	{
		return ((PartyBase)o)._customOwner;
	}

	internal static object AutoGeneratedGetMemberValue_index(object o)
	{
		return ((PartyBase)o)._index;
	}

	internal static object AutoGeneratedGetMemberValue_mapEventSide(object o)
	{
		return ((PartyBase)o)._mapEventSide;
	}

	internal static object AutoGeneratedGetMemberValue_numberOfMenWithHorse(object o)
	{
		return ((PartyBase)o)._numberOfMenWithHorse;
	}

	internal static object AutoGeneratedGetMemberValue_ships(object o)
	{
		return ((PartyBase)o)._ships;
	}

	public void OnVisibilityChanged(bool value)
	{
		MapEvent?.PartyVisibilityChanged(this, value);
		CampaignEventDispatcher.Instance.OnPartyVisibilityChanged(this);
		SetVisualAsDirty();
	}

	public void OnConsumedFood()
	{
		_lastEatingTime = CampaignTime.Now;
	}

	public void SetCustomOwner(Hero customOwner)
	{
		_customOwner = customOwner;
	}

	public static bool IsPartyUnderPlayerCommand(PartyBase party)
	{
		return Campaign.Current.Models.EncounterModel.IsPartyUnderPlayerCommand(party);
	}

	public void SetLevelMaskIsDirty()
	{
		LevelMaskIsDirty = true;
	}

	public void OnLevelMaskUpdated()
	{
		LevelMaskIsDirty = false;
	}

	public void SetCustomName(TextObject name)
	{
		CustomName = name;
		if (IsSettlement && !TextObject.IsNullOrEmpty(CustomName))
		{
			CustomName.SetSettlementProperties(Settlement);
		}
		SetVisualAsDirty();
	}

	CampaignVec2 IInteractablePoint.GetInteractionPosition(MobileParty interactingParty)
	{
		if (IsMobile)
		{
			return MobileParty.Position;
		}
		if (IsSettlement)
		{
			if (!interactingParty.IsTargetingPort)
			{
				return Settlement.GatePosition;
			}
			return Settlement.PortPosition;
		}
		return Position;
	}

	bool IInteractablePoint.CanPartyInteract(MobileParty mobileParty, float dt)
	{
		bool flag = false;
		if (IsMobile && (mobileParty.IsMainParty || !MobileParty.ShouldBeIgnored))
		{
			flag = mobileParty.IsCurrentlyAtSea == MobileParty.IsCurrentlyAtSea;
		}
		else if (IsSettlement)
		{
			flag = mobileParty.IsTargetingPort == mobileParty.IsCurrentlyAtSea && !mobileParty.IsTransitionInProgress;
		}
		if (flag)
		{
			GetEncounterTargetPoint(dt, mobileParty, out var targetPoint, out var neededMaximumDistanceForEncountering);
			float length = (mobileParty.Position.ToVec2() - targetPoint.ToVec2()).Length;
			flag = (mobileParty.BesiegedSettlement != null && mobileParty.BesiegedSettlement == mobileParty.TargetSettlement) || length < neededMaximumDistanceForEncountering;
		}
		return flag;
	}

	void IInteractablePoint.OnPartyInteraction(MobileParty engagingParty)
	{
		if (IsMobile)
		{
			MobileParty.OnPartyInteraction(engagingParty);
		}
		else if (IsSettlement)
		{
			Settlement.OnPartyInteraction(engagingParty);
		}
	}

	private static void GetEncounterTargetPoint(float dt, MobileParty mobileParty, out CampaignVec2 targetPoint, out float neededMaximumDistanceForEncountering)
	{
		EncounterModel encounterModel = Campaign.Current.Models.EncounterModel;
		float num = (mobileParty.IsCurrentlyAtSea ? encounterModel.NeededMaximumNavalDistanceForEncounteringMobileParty : encounterModel.NeededMaximumLandDistanceForEncounteringMobileParty);
		if (mobileParty.Army != null)
		{
			float a = (mobileParty.IsCurrentlyAtSea ? encounterModel.MaximumAllowedNavalDistanceForEncounteringMobilePartyInArmy : encounterModel.MaximumAllowedLandDistanceForEncounteringMobilePartyInArmy);
			neededMaximumDistanceForEncountering = TaleWorlds.Library.MathF.Clamp(num * TaleWorlds.Library.MathF.Sqrt(mobileParty.Army.LeaderParty.AttachedParties.Count + 1), TaleWorlds.Library.MathF.Max(num, dt * Campaign.Current.EstimatedMaximumLordPartySpeedExceptPlayer), TaleWorlds.Library.MathF.Max(a, dt * (Campaign.Current.EstimatedMaximumLordPartySpeedExceptPlayer + 0.01f)));
		}
		else
		{
			neededMaximumDistanceForEncountering = TaleWorlds.Library.MathF.Max(num, dt * Campaign.Current.EstimatedMaximumLordPartySpeedExceptPlayer);
		}
		if (mobileParty.IsCurrentlyEngagingSettlement)
		{
			Settlement shortTermTargetSettlement = mobileParty.ShortTermTargetSettlement;
			targetPoint = (mobileParty.IsTargetingPort ? shortTermTargetSettlement.PortPosition : shortTermTargetSettlement.GatePosition);
			neededMaximumDistanceForEncountering = (shortTermTargetSettlement.IsTown ? encounterModel.NeededMaximumDistanceForEncounteringTown : encounterModel.NeededMaximumDistanceForEncounteringVillage);
			if (mobileParty.IsTargetingPort)
			{
				SiegeEvent siegeEvent = shortTermTargetSettlement.SiegeEvent;
				if (siegeEvent != null && siegeEvent.IsBlockadeActive)
				{
					neededMaximumDistanceForEncountering = encounterModel.NeededMaximumDistanceForEncounteringBlockade;
				}
			}
		}
		else if (mobileParty.Army != null && mobileParty.Army.LeaderParty != mobileParty && mobileParty.ShortTermTargetParty.MapEvent != null && mobileParty.ShortTermTargetParty.MapEvent == mobileParty.Army.LeaderParty.MapEvent && mobileParty.Army.LeaderParty.AttachedParties.Contains(mobileParty))
		{
			targetPoint = mobileParty.Position;
		}
		else if (mobileParty.CurrentSettlement != null && mobileParty.ShortTermTargetParty.BesiegedSettlement == mobileParty.CurrentSettlement)
		{
			targetPoint = mobileParty.CurrentSettlement.GatePosition;
		}
		else
		{
			targetPoint = mobileParty.Ai.AiBehaviorInteractable.GetInteractionPosition(mobileParty);
		}
	}

	public void SetCustomBanner(Banner banner)
	{
		CustomBanner = banner;
		SetVisualAsDirty();
	}

	public bool IsUnderPlayersCommand(BattleSideEnum playerSide)
	{
		if (playerSide != Side)
		{
			return false;
		}
		return IsPartyUnderPlayerCommand(this);
	}

	int IBattleCombatant.GetTacticsSkillAmount()
	{
		if (LeaderHero != null)
		{
			return LeaderHero.GetSkillValue(DefaultSkills.Tactics);
		}
		return 0;
	}

	internal void AfterLoad()
	{
		if (!MBSaveLoad.IsUpdatingGameVersion)
		{
			return;
		}
		if (RandomValue == 0)
		{
			RandomValue = MBRandom.RandomInt(1, int.MaxValue);
		}
		if (MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.11.104253")))
		{
			if (PrisonRoster.Contains(CharacterObject.PlayerCharacter))
			{
				if (Hero.MainHero.PartyBelongedToAsPrisoner != this)
				{
					if (Hero.MainHero.PartyBelongedToAsPrisoner != null)
					{
						MBList<TroopRosterElement> troopRoster = PrisonRoster.GetTroopRoster();
						PrisonRoster = new TroopRoster(this);
						foreach (TroopRosterElement item in troopRoster)
						{
							if (item.Character != Hero.MainHero.CharacterObject)
							{
								PrisonRoster.AddToCounts(item.Character, item.Number);
							}
						}
					}
					else if (Hero.MainHero.PartyBelongedTo != null)
					{
						PrisonRoster.RemoveTroop(CharacterObject.PlayerCharacter);
					}
					else
					{
						TakePrisonerAction.Apply(this, Hero.MainHero);
					}
				}
				else if (Hero.MainHero.PartyBelongedTo != null)
				{
					PrisonRoster.RemoveTroop(CharacterObject.PlayerCharacter);
				}
			}
			foreach (TroopRosterElement item2 in PrisonRoster.GetTroopRoster())
			{
				if (item2.Character.HeroObject == null || item2.Character == CharacterObject.PlayerCharacter)
				{
					continue;
				}
				Hero heroObject = item2.Character.HeroObject;
				if (heroObject.PartyBelongedTo != null || heroObject.PartyBelongedToAsPrisoner != this)
				{
					PrisonRoster.RemoveTroop(item2.Character);
					if (!heroObject.IsDead && !heroObject.IsDisabled)
					{
						MakeHeroFugitiveAction.Apply(heroObject);
					}
				}
			}
			if (IsMobile && MobileParty.IsCaravan && !MobileParty.IsCurrentlyUsedByAQuest && _customOwner != null && MobileParty.Owner != Owner)
			{
				SetCustomOwner(null);
			}
		}
		if (MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.0"))
		{
			MemberRoster.RemoveZeroCounts();
		}
	}

	internal void InitCache()
	{
		_partyMemberSizeLastCheckVersion = -1;
		_prisonerSizeLastCheckVersion = -1;
		_lastNumberOfMenWithHorseVersionNo = -1;
		_lastNumberOfMenPerTierVersionNo = -1;
		_lastEstimatedStrengthVersionNo = -1;
	}

	[LoadInitializationCallback]
	private void OnLoad(MetaData metaData, ObjectLoadData objectLoadData)
	{
		if (MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.0")))
		{
			_ships = new MBList<Ship>();
		}
		InitCache();
	}

	public int GetNumberOfHealthyMenOfTier(int tier)
	{
		if (tier < 0)
		{
			Debug.FailedAssert("Requested men count for negative tier.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Party\\PartyBase.cs", "GetNumberOfHealthyMenOfTier", 652);
			return 0;
		}
		bool flag = false;
		if (_numberOfHealthyMenPerTier == null || tier >= _numberOfHealthyMenPerTier.Length)
		{
			int num = TaleWorlds.Library.MathF.Max(tier, 6);
			_numberOfHealthyMenPerTier = new int[num + 1];
			flag = true;
		}
		else if (_lastNumberOfMenPerTierVersionNo != MemberRoster.VersionNo)
		{
			flag = true;
		}
		if (flag)
		{
			for (int i = 0; i < _numberOfHealthyMenPerTier.Length; i++)
			{
				_numberOfHealthyMenPerTier[i] = 0;
			}
			for (int j = 0; j < MemberRoster.Count; j++)
			{
				CharacterObject characterAtIndex = MemberRoster.GetCharacterAtIndex(j);
				if (characterAtIndex != null && !characterAtIndex.IsHero)
				{
					int tier2 = characterAtIndex.Tier;
					if (tier2 >= 0 && tier2 < _numberOfHealthyMenPerTier.Length)
					{
						int num2 = MemberRoster.GetElementNumber(j) - MemberRoster.GetElementWoundedNumber(j);
						_numberOfHealthyMenPerTier[tier2] += num2;
					}
				}
			}
			_lastNumberOfMenPerTierVersionNo = MemberRoster.VersionNo;
		}
		return _numberOfHealthyMenPerTier[tier];
	}

	private int GetStrengthVersionNo()
	{
		int num = MemberRoster.VersionNo;
		if (IsMobile && MobileParty.IsCurrentlyAtSea)
		{
			num = GetShipsVersion() + num * 13;
		}
		num <<= 1;
		if (MapEvent != null)
		{
			num += ((Side == BattleSideEnum.Attacker) ? 1 : 0);
		}
		return num;
	}

	private void UpdateEstimatedStrengthCaches()
	{
		_cachedEstimatedStrength = CalculateEstimatedCurrentStrength();
		_lastEstimatedStrengthVersionNo = GetStrengthVersionNo();
	}

	public float CalculateCurrentStrength()
	{
		float result = 0f;
		if (IsMobile)
		{
			BattleSideEnum side = BattleSideEnum.Defender;
			CampaignVec2 position = Position;
			if (SiegeEvent != null && SiegeEvent.IsBlockadeActive && MapEvent != null && MapEvent.IsNavalMapEvent)
			{
				position = SiegeEvent.BesiegedSettlement.PortPosition;
			}
			MapEvent.PowerCalculationContext contextForPosition = Campaign.Current.Models.MilitaryPowerModel.GetContextForPosition(position);
			if (MapEvent != null)
			{
				side = Side;
			}
			result = Campaign.Current.Models.MilitaryPowerModel.GetPowerOfParty(this, side, contextForPosition);
		}
		return result;
	}

	private float CalculateEstimatedCurrentStrength()
	{
		float result = 0f;
		if (IsMobile)
		{
			BattleSideEnum side = BattleSideEnum.Defender;
			if (MapEvent != null)
			{
				side = Side;
			}
			result = Campaign.Current.Models.MilitaryPowerModel.GetPowerOfParty(this, side, MapEvent.PowerCalculationContext.Estimated);
		}
		return result;
	}

	public float GetCustomStrength(BattleSideEnum side, MapEvent.PowerCalculationContext context)
	{
		return Campaign.Current.Models.MilitaryPowerModel.GetPowerOfParty(this, side, context);
	}

	public PartyBase(MobileParty mobileParty)
		: this(mobileParty, null)
	{
	}

	public PartyBase(Settlement settlement)
		: this(null, settlement)
	{
	}

	private PartyBase(MobileParty mobileParty, Settlement settlement)
	{
		Index = Campaign.Current.GeneratePartyId(this);
		MobileParty = mobileParty;
		Settlement = settlement;
		ItemRoster = new ItemRoster();
		MemberRoster = new TroopRoster(this);
		PrisonRoster = new TroopRoster(this);
	}

	internal void AddShipInternal(Ship ship)
	{
		_ships.Add(ship);
	}

	internal void RemoveShipInternal(Ship ship)
	{
		_ships.Remove(ship);
	}

	public int GetShipsVersion()
	{
		int num = Ships.Count;
		foreach (Ship ship in Ships)
		{
			uint versionNo = ship.VersionNo;
			num = (int)((num * 31 + ((versionNo << 5) | (versionNo >> 3))) % 1000000007);
		}
		return (num + Ships.Count * 31) % 1000000007;
	}

	private void RecalculateNumberOfMenWithHorses()
	{
		_numberOfMenWithHorse = 0;
		for (int i = 0; i < MemberRoster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = MemberRoster.GetElementCopyAtIndex(i);
			if (elementCopyAtIndex.Character != null && elementCopyAtIndex.Character.IsMounted)
			{
				_numberOfMenWithHorse += elementCopyAtIndex.Number;
			}
		}
	}

	public int GetNumberOfMenWith(TraitObject trait)
	{
		int num = 0;
		foreach (TroopRosterElement item in MemberRoster.GetTroopRoster())
		{
			if (item.Character.GetTraitLevel(trait) > 0)
			{
				num += item.Number;
			}
		}
		return num;
	}

	public int AddPrisoner(CharacterObject element, int numberToAdd)
	{
		return PrisonRoster.AddToCounts(element, numberToAdd);
	}

	public int AddMember(CharacterObject element, int numberToAdd, int numberToAddWounded = 0)
	{
		return MemberRoster.AddToCounts(element, numberToAdd, insertAtFront: false, numberToAddWounded);
	}

	public void AddPrisoners(TroopRoster roster)
	{
		foreach (TroopRosterElement item in roster.GetTroopRoster())
		{
			AddPrisoner(item.Character, item.Number);
		}
	}

	public void AddMembers(TroopRoster roster)
	{
		MemberRoster.Add(roster);
	}

	public override string ToString()
	{
		if (!IsSettlement)
		{
			return MobileParty.Name.ToString();
		}
		return Settlement.Name.ToString();
	}

	public int AddElementToMemberRoster(CharacterObject element, int numberToAdd, bool insertAtFront = false)
	{
		return MemberRoster.AddToCounts(element, numberToAdd, insertAtFront);
	}

	public void AddToMemberRosterElementAtIndex(int index, int numberToAdd, int woundedCount = 0)
	{
		MemberRoster.AddToCountsAtIndex(index, numberToAdd, woundedCount);
	}

	public void WoundMemberRosterElements(CharacterObject elementObj, int numberToWound)
	{
		MemberRoster.AddToCounts(elementObj, 0, insertAtFront: false, numberToWound);
	}

	public void WoundMemberRosterElementsWithIndex(int elementIndex, int numberToWound)
	{
		MemberRoster.AddToCountsAtIndex(elementIndex, 0, numberToWound);
	}

	public void UpdateVisibilityAndInspected(CampaignVec2 fromPosition, float mainPartySeeingRange = 0f)
	{
		bool isVisible = false;
		bool isInspected = false;
		if (IsSettlement)
		{
			isVisible = true;
			if (Settlement.SettlementComponent is ISpottable { IsSpotted: false })
			{
				isVisible = false;
			}
			if (isVisible)
			{
				isInspected = CalculateSettlementInspected(fromPosition.ToVec2(), Settlement, mainPartySeeingRange);
			}
		}
		else if (MobileParty.IsActive)
		{
			if (Campaign.Current.TrueSight)
			{
				isVisible = true;
				isInspected = true;
			}
			else if (MobileParty.CurrentSettlement == null || MobileParty.LeaderHero?.ClanBanner != null || (MobileParty.MapEvent != null && MobileParty.MapEvent.IsSiegeAssault && MobileParty.Party.Side == BattleSideEnum.Attacker))
			{
				CalculateVisibilityAndInspected(fromPosition.ToVec2(), MobileParty, out isVisible, out isInspected, mainPartySeeingRange);
			}
		}
		if (IsSettlement)
		{
			Settlement.IsVisible = isVisible;
			Settlement.IsInspected = isInspected;
		}
		else
		{
			MobileParty.IsVisible = isVisible;
			MobileParty.IsInspected = isInspected;
		}
	}

	private static void CalculateVisibilityAndInspected(Vec2 fromPosition, IMapPoint mapPoint, out bool isVisible, out bool isInspected, float mainPartySeeingRange = 0f)
	{
		isInspected = false;
		isVisible = false;
		MobileParty mobileParty = mapPoint as MobileParty;
		if (mobileParty?.Army != null && mobileParty.Army.LeaderParty.AttachedParties.IndexOf(mobileParty) >= 0)
		{
			isVisible = mobileParty.Army.LeaderParty.IsVisible;
			return;
		}
		if (mobileParty != null && mobileParty.SiegeEvent != null && mobileParty.SiegeEvent.BesiegedSettlement.IsInspected)
		{
			isVisible = true;
			return;
		}
		if (mobileParty != null && mobileParty.MapEvent != null && mobileParty.MapEvent.IsRaid && mobileParty.MapEvent.MapEventSettlement.IsInspected)
		{
			isVisible = true;
			return;
		}
		float num = CalculateVisibilityRangeOfMapPoint(fromPosition, mapPoint, mainPartySeeingRange);
		if (num < 1f)
		{
			float partySpottingRatioForMainPartySeeingRange = Campaign.Current.Models.MapVisibilityModel.GetPartySpottingRatioForMainPartySeeingRange(mobileParty);
			isVisible = mapPoint.IsActive && num <= partySpottingRatioForMainPartySeeingRange;
			if (isVisible)
			{
				isInspected = true;
			}
		}
	}

	private static bool CalculateSettlementInspected(Vec2 fromPosition, IMapPoint mapPoint, float mainPartySeeingRange = 0f)
	{
		float num = 1.5f;
		return CalculateVisibilityRangeOfMapPoint(fromPosition, mapPoint, mainPartySeeingRange) <= num;
	}

	private static float CalculateVisibilityRangeOfMapPoint(Vec2 fromPosition, IMapPoint mapPoint, float mainPartySeeingRange)
	{
		MobileParty mainParty = MobileParty.MainParty;
		Vec2 vec = fromPosition - mapPoint.Position.ToVec2();
		float num = mainPartySeeingRange;
		if (mainPartySeeingRange == 0f)
		{
			num = mainParty.SeeingRange;
		}
		return vec.Length / num;
	}

	public void SetAsCameraFollowParty()
	{
		Campaign.Current.CameraFollowParty = this;
	}

	internal void OnFinishLoadState()
	{
		SetVisualAsDirty();
		MobileParty?.OnFinishLoadState();
	}

	public void SetVisualAsDirty()
	{
		if (MobileParty != null)
		{
			if (!MobileParty.IsCurrentlyAtSea || MobileParty.IsTransitionInProgress)
			{
				SiegeEvent siegeEvent = MobileParty.SiegeEvent;
				if (siegeEvent == null || !siegeEvent.IsBlockadeActive)
				{
					goto IL_0046;
				}
			}
			MobileParty.SetNavalVisualAsDirty();
		}
		goto IL_0046;
		IL_0046:
		IsVisualDirty = true;
	}

	public void OnVisualsUpdated()
	{
		IsVisualDirty = false;
	}

	internal void OnHeroAdded(Hero heroObject, TroopRoster roster)
	{
		if (object.Equals(roster, PrisonRoster))
		{
			heroObject.OnAddedToPartyAsPrisoner(this);
		}
		else
		{
			MobileParty?.OnHeroAdded(heroObject);
		}
	}

	internal void OnHeroRemoved(Hero heroObject, TroopRoster roster)
	{
		if (object.Equals(roster, PrisonRoster))
		{
			heroObject.OnRemovedFromPartyAsPrisoner(this);
		}
		else
		{
			MobileParty?.OnHeroRemoved(heroObject);
		}
	}

	internal void OnXpChanged(TroopRoster roster, ref TroopRosterElement element)
	{
		CharacterObject character = element.Character;
		if (!character.IsHero)
		{
			if (object.Equals(roster, PrisonRoster))
			{
				int maxValue = element.Number * character.ConformityNeededToRecruitPrisoner;
				int xp = element.Xp;
				element.Xp = MBMath.ClampInt(xp, 0, maxValue);
				return;
			}
			int num = 0;
			for (int i = 0; i < character.UpgradeTargets.Length; i++)
			{
				int upgradeXpCost = character.GetUpgradeXpCost(this, i);
				if (num < upgradeXpCost)
				{
					num = upgradeXpCost;
				}
			}
			int xp2 = MBMath.ClampInt(element.Xp, 0, element.Number * num);
			element.Xp = xp2;
		}
		else
		{
			element.Xp = TaleWorlds.Library.MathF.Max(element.Xp, 0);
		}
	}

	internal void OnRosterSizeChanged(TroopRoster roster)
	{
		if (object.Equals(roster, MemberRoster))
		{
			CampaignEventDispatcher.Instance.OnPartySizeChanged(this);
		}
	}
}
