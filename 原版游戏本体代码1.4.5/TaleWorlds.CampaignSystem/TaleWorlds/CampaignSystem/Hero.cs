using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Serialization;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Load;

namespace TaleWorlds.CampaignSystem;

public sealed class Hero : MBObjectBase, ITrackableCampaignObject, ITrackableBase, IRandomOwner
{
	public enum CharacterStates
	{
		NotSpawned,
		Active,
		Fugitive,
		Prisoner,
		Released,
		Dead,
		Disabled,
		Traveling
	}

	[SaveableField(120)]
	public int LastTimeStampForActivity;

	public const int MaximumNumberOfVolunteers = 6;

	[SaveableField(130)]
	public CharacterObject[] VolunteerTypes;

	[SaveableField(160)]
	private float _passedTimeAtHomeSettlement;

	[SaveableField(170)]
	private CharacterObject _characterObject;

	[SaveableField(180)]
	private TextObject _firstName;

	[SaveableField(181)]
	private TextObject _name;

	[SaveableField(260)]
	private CharacterStates _heroState;

	[SaveableField(270)]
	private PropertyOwner<TraitObject> _heroTraits;

	[SaveableField(280)]
	private PropertyOwner<PerkObject> _heroPerks;

	[SaveableField(290)]
	private PropertyOwner<SkillObject> _heroSkills;

	[SaveableField(301)]
	private PropertyOwner<CharacterAttribute> _characterAttributes;

	internal bool IsNobleForOldSaves;

	[SaveableField(370)]
	public int Level;

	[SaveableField(810)]
	public bool HiddenInEncyclopedia;

	[SaveableField(380)]
	private Clan _companionOf;

	[SaveableField(430)]
	private int _health;

	[SaveableField(441)]
	private float _defaultAge;

	[SaveableField(440)]
	private CampaignTime _birthDay;

	[SaveableField(450)]
	private CampaignTime _deathDay;

	[SaveableField(460)]
	private float _power;

	[SaveableField(500)]
	private Clan _clan;

	[SaveableField(501)]
	private Clan _originClan;

	[SaveableField(510)]
	private Clan _supporterOf;

	[SaveableField(520)]
	private Town _governorOf;

	[SaveableField(530)]
	private MBList<Workshop> _ownedWorkshops = new MBList<Workshop>();

	[SaveableField(551)]
	public CultureObject Culture;

	[XmlIgnore]
	[SaveableField(560)]
	private MobileParty _partyBelongedTo;

	[SaveableField(580)]
	private Settlement _stayingInSettlement;

	[SaveableField(590)]
	public MBList<ItemObject> SpecialItems;

	[SaveableField(412)]
	private bool _isKnownToPlayer;

	[SaveableField(610)]
	private bool _hasMet;

	[SaveableField(630)]
	private Settlement _bornSettlement;

	[CachedData]
	private Settlement _homeSettlement;

	[CachedData]
	private float _powerModifier = -1f;

	[SaveableField(650)]
	private int _gold;

	[SaveableField(700)]
	private Hero _father;

	[SaveableField(710)]
	private Hero _mother;

	[SaveableField(720)]
	private readonly MBList<Hero> _exSpouses = new MBList<Hero>();

	[SaveableField(730)]
	private Hero _spouse;

	[SaveableField(740)]
	private readonly MBList<Hero> _children = new MBList<Hero>();

	[SaveableField(760)]
	public bool IsPregnant;

	[SaveableField(770)]
	private HeroDeveloper _heroDeveloper;

	[SaveableProperty(100)]
	public StaticBodyProperties StaticBodyProperties { get; set; }

	[SaveableProperty(111)]
	public float Weight { get; set; }

	[SaveableProperty(112)]
	public float Build { get; set; }

	public BodyProperties BodyProperties => new BodyProperties(new DynamicBodyProperties(Age, Weight, Build), StaticBodyProperties);

	public float PassedTimeAtHomeSettlement
	{
		get
		{
			return _passedTimeAtHomeSettlement;
		}
		set
		{
			_passedTimeAtHomeSettlement = value;
		}
	}

	public bool CanHaveRecruits => Campaign.Current.Models.VolunteerModel.CanHaveRecruits(this);

	public CharacterObject CharacterObject => _characterObject;

	public TextObject FirstName => _firstName;

	public TextObject Name => _name;

	[SaveableProperty(190)]
	public TextObject EncyclopediaText { get; set; }

	public string EncyclopediaLink => (Campaign.Current.EncyclopediaManager.GetIdentifier(typeof(Hero)) + "-" + base.StringId) ?? "";

	public TextObject EncyclopediaLinkWithName => HyperlinkTexts.GetHeroHyperlinkText(EncyclopediaLink, Name);

	[SaveableProperty(200)]
	public bool IsFemale { get; set; }

	[SaveableProperty(210)]
	private Equipment _battleEquipment { get; set; }

	[SaveableProperty(220)]
	private Equipment _civilianEquipment { get; set; }

	[SaveableProperty(881)]
	private Equipment _stealthEquipment { get; set; }

	public Equipment BattleEquipment => _battleEquipment ?? Campaign.Current.DeadBattleEquipment;

	public Equipment CivilianEquipment => _civilianEquipment ?? Campaign.Current.DeadCivilianEquipment;

	public Equipment StealthEquipment => _stealthEquipment ?? Campaign.Current.DefaultStealthEquipment;

	[SaveableProperty(240)]
	public CampaignTime CaptivityStartTime { get; set; }

	[SaveableProperty(800)]
	public FormationClass PreferredUpgradeFormation { get; set; }

	public CharacterStates HeroState
	{
		get
		{
			return _heroState;
		}
		private set
		{
			ChangeState(value);
		}
	}

	public IReadOnlyPropertyOwner<CharacterAttribute> CharacterAttributes => _characterAttributes;

	[SaveableProperty(320)]
	public bool IsMinorFactionHero { get; set; }

	public IssueBase Issue { get; private set; }

	public int WoundedHealthLimit => Campaign.Current.Models.CharacterStatsModel.WoundedHitPointLimit(this);

	public bool IsNoncombatant => !Campaign.Current.Models.HeroCreationModel.IsHeroCombatant(this);

	public Clan CompanionOf
	{
		get
		{
			return _companionOf;
		}
		set
		{
			if (value != _companionOf)
			{
				_homeSettlement = null;
				if (_companionOf != null)
				{
					_companionOf.OnCompanionRemoved(this);
				}
				_companionOf = value;
				if (_companionOf != null)
				{
					_companionOf.OnCompanionAdded(this);
				}
			}
		}
	}

	public IEnumerable<Hero> CompanionsInParty
	{
		get
		{
			if (PartyBelongedTo == null || Clan == null)
			{
				yield break;
			}
			foreach (Hero companion in Clan.Companions)
			{
				if (companion.PartyBelongedTo == PartyBelongedTo)
				{
					yield return companion;
				}
			}
		}
	}

	[SaveableProperty(780)]
	public Occupation Occupation { get; private set; }

	public CharacterObject Template => CharacterObject.OriginalCharacter;

	public bool IsDead => HeroState == CharacterStates.Dead;

	public bool IsFugitive => HeroState == CharacterStates.Fugitive;

	public bool IsPrisoner => HeroState == CharacterStates.Prisoner;

	public bool IsReleased => HeroState == CharacterStates.Released;

	public bool IsActive => HeroState == CharacterStates.Active;

	public bool IsNotSpawned => HeroState == CharacterStates.NotSpawned;

	public bool IsDisabled => HeroState == CharacterStates.Disabled;

	public bool IsTraveling => HeroState == CharacterStates.Traveling;

	public bool IsAlive => !IsDead;

	[SaveableProperty(400)]
	public KillCharacterAction.KillCharacterActionDetail DeathMark { get; private set; }

	[SaveableProperty(401)]
	public Hero DeathMarkKillerHero { get; private set; }

	[SaveableProperty(411)]
	public Settlement LastKnownClosestSettlement { get; private set; }

	public bool IsWanderer => Occupation == Occupation.Wanderer;

	public bool IsTemplate => CharacterObject.IsTemplate;

	public bool IsWounded => HitPoints <= WoundedHealthLimit;

	public bool IsPlayerCompanion => CompanionOf == Clan.PlayerClan;

	public bool IsMerchant => Occupation == Occupation.Merchant;

	public bool IsPreacher => Occupation == Occupation.Preacher;

	public bool IsHeadman => Occupation == Occupation.Headman;

	public bool IsGangLeader => Occupation == Occupation.GangLeader;

	public bool IsArtisan => Occupation == Occupation.Artisan;

	public bool IsRuralNotable => Occupation == Occupation.RuralNotable;

	public bool IsUrbanNotable
	{
		get
		{
			if (Occupation != Occupation.Merchant && Occupation != Occupation.Artisan)
			{
				return Occupation == Occupation.GangLeader;
			}
			return true;
		}
	}

	public bool IsSpecial => Occupation == Occupation.Special;

	public bool IsRebel
	{
		get
		{
			if (Clan != null)
			{
				return Clan.IsRebelClan;
			}
			return false;
		}
	}

	public bool IsCommander => GetTraitLevel(DefaultTraits.Commander) > 0;

	public bool IsPartyLeader
	{
		get
		{
			if (PartyBelongedTo != null)
			{
				return PartyBelongedTo.LeaderHero == this;
			}
			return false;
		}
	}

	public bool IsNotable
	{
		get
		{
			if (!IsArtisan && !IsGangLeader && !IsPreacher && !IsMerchant && !IsRuralNotable)
			{
				return IsHeadman;
			}
			return true;
		}
	}

	public bool IsLord => Occupation == Occupation.Lord;

	public int MaxHitPoints => CharacterObject.MaxHitPoints();

	public int HitPoints
	{
		get
		{
			return _health;
		}
		set
		{
			if (_health == value)
			{
				return;
			}
			int health = _health;
			_health = value;
			if (_health < 0)
			{
				_health = 1;
			}
			else if (_health > CharacterObject.MaxHitPoints())
			{
				_health = CharacterObject.MaxHitPoints();
			}
			if (health <= WoundedHealthLimit != _health <= WoundedHealthLimit)
			{
				if (PartyBelongedTo != null)
				{
					PartyBelongedTo.MemberRoster.OnHeroHealthStatusChanged(this);
				}
				if (PartyBelongedToAsPrisoner != null)
				{
					PartyBelongedToAsPrisoner.PrisonRoster.OnHeroHealthStatusChanged(this);
				}
			}
			if (health > WoundedHealthLimit && IsWounded)
			{
				CampaignEventDispatcher.Instance.OnHeroWounded(this);
			}
		}
	}

	public CampaignTime BirthDay
	{
		get
		{
			if (CampaignOptions.IsLifeDeathCycleDisabled)
			{
				return CampaignTime.YearsFromNow(0f - _defaultAge);
			}
			return _birthDay;
		}
	}

	public CampaignTime DeathDay
	{
		get
		{
			if (CampaignOptions.IsLifeDeathCycleDisabled)
			{
				return CampaignTime.YearsFromNow(0f - _defaultAge) + CampaignTime.Years(_defaultAge);
			}
			return _deathDay;
		}
		private set
		{
			_deathDay = value;
		}
	}

	public float Age
	{
		get
		{
			if (CampaignOptions.IsLifeDeathCycleDisabled)
			{
				return _defaultAge;
			}
			if (IsAlive)
			{
				return _birthDay.ElapsedYearsUntilNow;
			}
			return (float)(DeathDay - _birthDay).ToYears;
		}
	}

	public bool IsChild => Age < (float)Campaign.Current.Models.AgeModel.HeroComesOfAge;

	public float Power => _power;

	public Banner ClanBanner => Clan?.Banner;

	[SaveableProperty(481)]
	public long LastExaminedLogEntryID { get; set; }

	public Clan OriginClan => _originClan;

	public Clan Clan
	{
		get
		{
			return CompanionOf ?? _clan;
		}
		set
		{
			if (_clan != value)
			{
				if (_clan == null)
				{
					_originClan = value;
				}
				_homeSettlement = null;
				if (_clan != null)
				{
					_clan.OnLordRemoved(this);
				}
				Clan clan = _clan;
				_clan = value;
				if (_clan != null)
				{
					_clan.OnLordAdded(this);
				}
				CampaignEventDispatcher.Instance.OnHeroChangedClan(this, clan);
			}
		}
	}

	public Clan SupporterOf
	{
		get
		{
			return _supporterOf;
		}
		set
		{
			if (_supporterOf != value)
			{
				if (_supporterOf != null)
				{
					_supporterOf.OnSupporterNotableRemoved(this);
				}
				_supporterOf = value;
				if (_supporterOf != null)
				{
					_supporterOf.OnSupporterNotableAdded(this);
				}
			}
		}
	}

	public Town GovernorOf
	{
		get
		{
			return _governorOf;
		}
		set
		{
			if (value != _governorOf)
			{
				_governorOf = value;
			}
		}
	}

	public IFaction MapFaction
	{
		get
		{
			if (Clan != null)
			{
				IFaction kingdom = Clan.Kingdom;
				return kingdom ?? Clan;
			}
			if (IsSpecial)
			{
				return null;
			}
			if (HomeSettlement != null)
			{
				return HomeSettlement.MapFaction;
			}
			if (PartyBelongedTo != null)
			{
				return PartyBelongedTo.MapFaction;
			}
			return null;
		}
	}

	public List<Alley> OwnedAlleys { get; private set; }

	public bool IsFactionLeader
	{
		get
		{
			if (MapFaction != null)
			{
				return MapFaction.Leader == this;
			}
			return false;
		}
	}

	public bool IsKingdomLeader
	{
		get
		{
			if (MapFaction != null && MapFaction.IsKingdomFaction)
			{
				return MapFaction.Leader == this;
			}
			return false;
		}
	}

	public bool IsClanLeader
	{
		get
		{
			if (Clan != null)
			{
				return Clan.Leader == this;
			}
			return false;
		}
	}

	public List<CaravanPartyComponent> OwnedCaravans { get; private set; }

	public MobileParty PartyBelongedTo
	{
		get
		{
			return _partyBelongedTo;
		}
		private set
		{
			SetPartyBelongedTo(value);
		}
	}

	[SaveableProperty(570)]
	public PartyBase PartyBelongedToAsPrisoner { get; private set; }

	public Settlement StayingInSettlement
	{
		get
		{
			return _stayingInSettlement;
		}
		set
		{
			if (_stayingInSettlement != value)
			{
				if (_stayingInSettlement != null)
				{
					_stayingInSettlement.RemoveHeroWithoutParty(this);
					_stayingInSettlement = null;
				}
				value?.AddHeroWithoutParty(this);
				_stayingInSettlement = value;
			}
		}
	}

	public bool IsHumanPlayerCharacter => this == MainHero;

	public bool IsKnownToPlayer
	{
		get
		{
			return _isKnownToPlayer;
		}
		set
		{
			if (_isKnownToPlayer != value)
			{
				_isKnownToPlayer = value;
				CampaignEventDispatcher.Instance.OnPlayerLearnsAboutHero(this);
			}
		}
	}

	public bool HasMet
	{
		get
		{
			return _hasMet;
		}
		private set
		{
			if (_hasMet != value)
			{
				_hasMet = value;
				CampaignEventDispatcher.Instance.OnPlayerMetHero(this);
			}
		}
	}

	[SaveableProperty(620)]
	public CampaignTime LastMeetingTimeWithPlayer { get; set; }

	public Settlement BornSettlement
	{
		get
		{
			return _bornSettlement;
		}
		set
		{
			_bornSettlement = value;
			_homeSettlement = null;
		}
	}

	public Settlement HomeSettlement
	{
		get
		{
			if (_homeSettlement == null)
			{
				UpdateHomeSettlement();
			}
			return _homeSettlement;
		}
	}

	public float PowerModifier
	{
		get
		{
			if (_powerModifier == -1f)
			{
				_powerModifier = Campaign.Current.Models.MilitaryPowerModel.GetPowerModifierOfHero(this);
			}
			return _powerModifier;
		}
	}

	public Settlement CurrentSettlement
	{
		get
		{
			Settlement result = null;
			if (PartyBelongedTo != null)
			{
				result = PartyBelongedTo.CurrentSettlement;
			}
			else if (PartyBelongedToAsPrisoner != null)
			{
				result = (PartyBelongedToAsPrisoner.IsSettlement ? PartyBelongedToAsPrisoner.Settlement : (PartyBelongedToAsPrisoner.IsMobile ? PartyBelongedToAsPrisoner.MobileParty.CurrentSettlement : null));
			}
			else if (StayingInSettlement != null)
			{
				result = StayingInSettlement;
			}
			return result;
		}
	}

	public int Gold
	{
		get
		{
			return _gold;
		}
		set
		{
			_gold = TaleWorlds.Library.MathF.Max(0, value);
		}
	}

	[SaveableProperty(660)]
	public int RandomValue { get; private set; } = MBRandom.RandomInt(1, int.MaxValue);

	public EquipmentElement BannerItem
	{
		get
		{
			return BattleEquipment[EquipmentIndex.ExtraWeaponSlot];
		}
		set
		{
			BattleEquipment[EquipmentIndex.ExtraWeaponSlot] = value;
		}
	}

	public float ProbabilityOfDeath => Campaign.Current.Models.HeroDeathProbabilityCalculationModel.CalculateHeroDeathProbability(this);

	public Hero Father
	{
		get
		{
			return _father;
		}
		set
		{
			_father = value;
			if (_father != null)
			{
				_father._children.Add(this);
			}
		}
	}

	public Hero Mother
	{
		get
		{
			return _mother;
		}
		set
		{
			_mother = value;
			if (_mother != null)
			{
				_mother._children.Add(this);
			}
		}
	}

	public MBReadOnlyList<Hero> ExSpouses => _exSpouses;

	public Hero Spouse
	{
		get
		{
			return _spouse;
		}
		set
		{
			if (_spouse != value)
			{
				Hero spouse = _spouse;
				_spouse = value;
				if (spouse != null)
				{
					_exSpouses.Add(spouse);
					spouse.Spouse = null;
				}
				if (_spouse != null)
				{
					_spouse.Spouse = this;
				}
			}
		}
	}

	public MBList<Hero> Children => _children;

	public IEnumerable<Hero> Siblings
	{
		get
		{
			if (Father != null)
			{
				foreach (Hero child in Father._children)
				{
					if (child != this)
					{
						yield return child;
					}
				}
			}
			else
			{
				if (Mother == null)
				{
					yield break;
				}
				foreach (Hero child2 in Mother._children)
				{
					if (child2 != this)
					{
						yield return child2;
					}
				}
			}
		}
	}

	public HeroDeveloper HeroDeveloper => _heroDeveloper;

	public MBReadOnlyList<Workshop> OwnedWorkshops => _ownedWorkshops;

	public static MBReadOnlyList<Hero> AllAliveHeroes => Campaign.Current.AliveHeroes;

	public static MBReadOnlyList<Hero> DeadOrDisabledHeroes => Campaign.Current.DeadOrDisabledHeroes;

	public static Hero MainHero => CharacterObject.PlayerCharacter.HeroObject;

	public static Hero OneToOneConversationHero => Campaign.Current.ConversationManager.OneToOneConversationHero;

	public static bool IsMainHeroIll => Campaign.Current.MainHeroIllDays != -1;

	internal static void AutoGeneratedStaticCollectObjectsHero(object o, List<object> collectedObjects)
	{
		((Hero)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		collectedObjects.Add(VolunteerTypes);
		collectedObjects.Add(Culture);
		collectedObjects.Add(SpecialItems);
		collectedObjects.Add(_characterObject);
		collectedObjects.Add(_firstName);
		collectedObjects.Add(_name);
		collectedObjects.Add(_heroTraits);
		collectedObjects.Add(_heroPerks);
		collectedObjects.Add(_heroSkills);
		collectedObjects.Add(_characterAttributes);
		collectedObjects.Add(_companionOf);
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(_birthDay, collectedObjects);
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(_deathDay, collectedObjects);
		collectedObjects.Add(_clan);
		collectedObjects.Add(_originClan);
		collectedObjects.Add(_supporterOf);
		collectedObjects.Add(_governorOf);
		collectedObjects.Add(_ownedWorkshops);
		collectedObjects.Add(_partyBelongedTo);
		collectedObjects.Add(_stayingInSettlement);
		collectedObjects.Add(_bornSettlement);
		collectedObjects.Add(_father);
		collectedObjects.Add(_mother);
		collectedObjects.Add(_exSpouses);
		collectedObjects.Add(_spouse);
		collectedObjects.Add(_children);
		collectedObjects.Add(_heroDeveloper);
		StaticBodyProperties.AutoGeneratedStaticCollectObjectsStaticBodyProperties(StaticBodyProperties, collectedObjects);
		collectedObjects.Add(EncyclopediaText);
		collectedObjects.Add(_battleEquipment);
		collectedObjects.Add(_civilianEquipment);
		collectedObjects.Add(_stealthEquipment);
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(CaptivityStartTime, collectedObjects);
		collectedObjects.Add(DeathMarkKillerHero);
		collectedObjects.Add(LastKnownClosestSettlement);
		collectedObjects.Add(PartyBelongedToAsPrisoner);
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(LastMeetingTimeWithPlayer, collectedObjects);
	}

	internal static object AutoGeneratedGetMemberValueStaticBodyProperties(object o)
	{
		return ((Hero)o).StaticBodyProperties;
	}

	internal static object AutoGeneratedGetMemberValueWeight(object o)
	{
		return ((Hero)o).Weight;
	}

	internal static object AutoGeneratedGetMemberValueBuild(object o)
	{
		return ((Hero)o).Build;
	}

	internal static object AutoGeneratedGetMemberValueEncyclopediaText(object o)
	{
		return ((Hero)o).EncyclopediaText;
	}

	internal static object AutoGeneratedGetMemberValueIsFemale(object o)
	{
		return ((Hero)o).IsFemale;
	}

	internal static object AutoGeneratedGetMemberValue_battleEquipment(object o)
	{
		return ((Hero)o)._battleEquipment;
	}

	internal static object AutoGeneratedGetMemberValue_civilianEquipment(object o)
	{
		return ((Hero)o)._civilianEquipment;
	}

	internal static object AutoGeneratedGetMemberValue_stealthEquipment(object o)
	{
		return ((Hero)o)._stealthEquipment;
	}

	internal static object AutoGeneratedGetMemberValueCaptivityStartTime(object o)
	{
		return ((Hero)o).CaptivityStartTime;
	}

	internal static object AutoGeneratedGetMemberValuePreferredUpgradeFormation(object o)
	{
		return ((Hero)o).PreferredUpgradeFormation;
	}

	internal static object AutoGeneratedGetMemberValueIsMinorFactionHero(object o)
	{
		return ((Hero)o).IsMinorFactionHero;
	}

	internal static object AutoGeneratedGetMemberValueOccupation(object o)
	{
		return ((Hero)o).Occupation;
	}

	internal static object AutoGeneratedGetMemberValueDeathMark(object o)
	{
		return ((Hero)o).DeathMark;
	}

	internal static object AutoGeneratedGetMemberValueDeathMarkKillerHero(object o)
	{
		return ((Hero)o).DeathMarkKillerHero;
	}

	internal static object AutoGeneratedGetMemberValueLastKnownClosestSettlement(object o)
	{
		return ((Hero)o).LastKnownClosestSettlement;
	}

	internal static object AutoGeneratedGetMemberValueLastExaminedLogEntryID(object o)
	{
		return ((Hero)o).LastExaminedLogEntryID;
	}

	internal static object AutoGeneratedGetMemberValuePartyBelongedToAsPrisoner(object o)
	{
		return ((Hero)o).PartyBelongedToAsPrisoner;
	}

	internal static object AutoGeneratedGetMemberValueLastMeetingTimeWithPlayer(object o)
	{
		return ((Hero)o).LastMeetingTimeWithPlayer;
	}

	internal static object AutoGeneratedGetMemberValueRandomValue(object o)
	{
		return ((Hero)o).RandomValue;
	}

	internal static object AutoGeneratedGetMemberValueLastTimeStampForActivity(object o)
	{
		return ((Hero)o).LastTimeStampForActivity;
	}

	internal static object AutoGeneratedGetMemberValueVolunteerTypes(object o)
	{
		return ((Hero)o).VolunteerTypes;
	}

	internal static object AutoGeneratedGetMemberValueLevel(object o)
	{
		return ((Hero)o).Level;
	}

	internal static object AutoGeneratedGetMemberValueHiddenInEncyclopedia(object o)
	{
		return ((Hero)o).HiddenInEncyclopedia;
	}

	internal static object AutoGeneratedGetMemberValueCulture(object o)
	{
		return ((Hero)o).Culture;
	}

	internal static object AutoGeneratedGetMemberValueSpecialItems(object o)
	{
		return ((Hero)o).SpecialItems;
	}

	internal static object AutoGeneratedGetMemberValueIsPregnant(object o)
	{
		return ((Hero)o).IsPregnant;
	}

	internal static object AutoGeneratedGetMemberValue_passedTimeAtHomeSettlement(object o)
	{
		return ((Hero)o)._passedTimeAtHomeSettlement;
	}

	internal static object AutoGeneratedGetMemberValue_characterObject(object o)
	{
		return ((Hero)o)._characterObject;
	}

	internal static object AutoGeneratedGetMemberValue_firstName(object o)
	{
		return ((Hero)o)._firstName;
	}

	internal static object AutoGeneratedGetMemberValue_name(object o)
	{
		return ((Hero)o)._name;
	}

	internal static object AutoGeneratedGetMemberValue_heroState(object o)
	{
		return ((Hero)o)._heroState;
	}

	internal static object AutoGeneratedGetMemberValue_heroTraits(object o)
	{
		return ((Hero)o)._heroTraits;
	}

	internal static object AutoGeneratedGetMemberValue_heroPerks(object o)
	{
		return ((Hero)o)._heroPerks;
	}

	internal static object AutoGeneratedGetMemberValue_heroSkills(object o)
	{
		return ((Hero)o)._heroSkills;
	}

	internal static object AutoGeneratedGetMemberValue_characterAttributes(object o)
	{
		return ((Hero)o)._characterAttributes;
	}

	internal static object AutoGeneratedGetMemberValue_companionOf(object o)
	{
		return ((Hero)o)._companionOf;
	}

	internal static object AutoGeneratedGetMemberValue_health(object o)
	{
		return ((Hero)o)._health;
	}

	internal static object AutoGeneratedGetMemberValue_defaultAge(object o)
	{
		return ((Hero)o)._defaultAge;
	}

	internal static object AutoGeneratedGetMemberValue_birthDay(object o)
	{
		return ((Hero)o)._birthDay;
	}

	internal static object AutoGeneratedGetMemberValue_deathDay(object o)
	{
		return ((Hero)o)._deathDay;
	}

	internal static object AutoGeneratedGetMemberValue_power(object o)
	{
		return ((Hero)o)._power;
	}

	internal static object AutoGeneratedGetMemberValue_clan(object o)
	{
		return ((Hero)o)._clan;
	}

	internal static object AutoGeneratedGetMemberValue_originClan(object o)
	{
		return ((Hero)o)._originClan;
	}

	internal static object AutoGeneratedGetMemberValue_supporterOf(object o)
	{
		return ((Hero)o)._supporterOf;
	}

	internal static object AutoGeneratedGetMemberValue_governorOf(object o)
	{
		return ((Hero)o)._governorOf;
	}

	internal static object AutoGeneratedGetMemberValue_ownedWorkshops(object o)
	{
		return ((Hero)o)._ownedWorkshops;
	}

	internal static object AutoGeneratedGetMemberValue_partyBelongedTo(object o)
	{
		return ((Hero)o)._partyBelongedTo;
	}

	internal static object AutoGeneratedGetMemberValue_stayingInSettlement(object o)
	{
		return ((Hero)o)._stayingInSettlement;
	}

	internal static object AutoGeneratedGetMemberValue_isKnownToPlayer(object o)
	{
		return ((Hero)o)._isKnownToPlayer;
	}

	internal static object AutoGeneratedGetMemberValue_hasMet(object o)
	{
		return ((Hero)o)._hasMet;
	}

	internal static object AutoGeneratedGetMemberValue_bornSettlement(object o)
	{
		return ((Hero)o)._bornSettlement;
	}

	internal static object AutoGeneratedGetMemberValue_gold(object o)
	{
		return ((Hero)o)._gold;
	}

	internal static object AutoGeneratedGetMemberValue_father(object o)
	{
		return ((Hero)o)._father;
	}

	internal static object AutoGeneratedGetMemberValue_mother(object o)
	{
		return ((Hero)o)._mother;
	}

	internal static object AutoGeneratedGetMemberValue_exSpouses(object o)
	{
		return ((Hero)o)._exSpouses;
	}

	internal static object AutoGeneratedGetMemberValue_spouse(object o)
	{
		return ((Hero)o)._spouse;
	}

	internal static object AutoGeneratedGetMemberValue_children(object o)
	{
		return ((Hero)o)._children;
	}

	internal static object AutoGeneratedGetMemberValue_heroDeveloper(object o)
	{
		return ((Hero)o)._heroDeveloper;
	}

	private void SetCharacterObject(CharacterObject characterObject)
	{
		_characterObject = characterObject;
		SetInitialValuesFromCharacter(_characterObject);
		_characterObject.HeroObject = this;
	}

	public override TextObject GetName()
	{
		return Name;
	}

	public void SetName(TextObject fullName, TextObject firstName)
	{
		_name = fullName;
		_firstName = firstName;
		if (PartyBelongedTo != null && PartyBelongedTo.LeaderHero == this)
		{
			PartyBelongedTo.PartyComponent.ClearCachedName();
		}
	}

	public void OnIssueCreatedForHero(IssueBase issue)
	{
		Issue = issue;
	}

	public void OnIssueDeactivatedForHero()
	{
		Issue = null;
	}

	public override string ToString()
	{
		return Name.ToString();
	}

	public void UpdateLastKnownClosestSettlement(Settlement settlement)
	{
		LastKnownClosestSettlement = settlement;
	}

	public void SetNewOccupation(Occupation occupation)
	{
		Occupation occupation2 = Occupation;
		Occupation = occupation;
		CampaignEventDispatcher.Instance.OnHeroOccupationChanged(this, occupation2);
	}

	public void SetBirthDay(CampaignTime birthday)
	{
		_birthDay = birthday;
		_defaultAge = (birthday.IsNow ? 0.001f : _birthDay.ElapsedYearsUntilNow);
	}

	public void SetDeathDay(CampaignTime deathDay)
	{
		_deathDay = deathDay;
	}

	public void AddPower(float value)
	{
		_power += value;
	}

	public void SetHasMet()
	{
		HasMet = true;
		LastMeetingTimeWithPlayer = CampaignTime.Now;
	}

	public void UpdatePowerModifier()
	{
		_powerModifier = Campaign.Current.Models.MilitaryPowerModel.GetPowerModifierOfHero(this);
	}

	public void UpdateHomeSettlement()
	{
		if (GovernorOf != null)
		{
			_homeSettlement = GovernorOf.Owner.Settlement;
			return;
		}
		if (Spouse != null && Spouse.GovernorOf != null)
		{
			_homeSettlement = Spouse.GovernorOf.Owner.Settlement;
			return;
		}
		foreach (Hero child in Children)
		{
			if (child.GovernorOf != null && child.Clan == Clan)
			{
				_homeSettlement = child.GovernorOf.Owner.Settlement;
				return;
			}
		}
		if (Father != null && Father.GovernorOf != null && Father.Clan == Clan)
		{
			_homeSettlement = Father.GovernorOf.Owner.Settlement;
			return;
		}
		if (Mother != null && Mother.GovernorOf != null && Mother.Clan == Clan)
		{
			_homeSettlement = Mother.GovernorOf.Owner.Settlement;
			return;
		}
		foreach (Hero sibling in Siblings)
		{
			if (sibling.GovernorOf != null && sibling.Clan == Clan)
			{
				_homeSettlement = sibling.GovernorOf.Owner.Settlement;
				return;
			}
		}
		if (Clan != null)
		{
			_homeSettlement = Clan.HomeSettlement;
		}
		else if (CompanionOf != null)
		{
			_homeSettlement = CompanionOf.HomeSettlement;
		}
		else if (IsNotable && CurrentSettlement != null)
		{
			_homeSettlement = CurrentSettlement;
		}
		else
		{
			_homeSettlement = _bornSettlement;
		}
	}

	public int GetSkillValue(SkillObject skill)
	{
		if (_heroSkills == null)
		{
			return 0;
		}
		return _heroSkills.GetPropertyValue(skill);
	}

	public void SetSkillValue(SkillObject skill, int value)
	{
		_heroSkills.SetPropertyValue(skill, value);
	}

	public void ClearSkills()
	{
		_heroSkills.ClearAllProperty();
	}

	public void AddSkillXp(SkillObject skill, float xpAmount)
	{
		_heroDeveloper?.AddSkillXp(skill, xpAmount);
	}

	public int GetAttributeValue(CharacterAttribute charAttribute)
	{
		if (_characterAttributes == null)
		{
			return 0;
		}
		return _characterAttributes.GetPropertyValue(charAttribute);
	}

	internal void SetAttributeValueInternal(CharacterAttribute charAttribute, int value)
	{
		int value2 = MBMath.ClampInt(value, 0, Campaign.Current.Models.CharacterDevelopmentModel.MaxAttribute);
		_characterAttributes.SetPropertyValue(charAttribute, value2);
	}

	public void ClearAttributes()
	{
		_characterAttributes?.ClearAllProperty();
	}

	public void SetTraitLevel(TraitObject trait, int value)
	{
		value = MBMath.ClampInt(value, trait.MinValue, trait.MaxValue);
		_heroTraits.SetPropertyValue(trait, value);
	}

	public int GetTraitLevel(TraitObject trait)
	{
		if (_heroTraits == null)
		{
			return 0;
		}
		return _heroTraits.GetPropertyValue(trait);
	}

	public void ClearTraits()
	{
		_heroTraits.ClearAllProperty();
	}

	internal void SetPerkValueInternal(PerkObject perk, bool value)
	{
		if (_heroPerks.GetPropertyValue(perk) == 1 != value)
		{
			_heroPerks.SetPropertyValue(perk, value ? 1 : 0);
			if (value)
			{
				CampaignEventDispatcher.Instance.OnPerkOpened(this, perk);
			}
			else
			{
				CampaignEventDispatcher.Instance.OnPerkReset(this, perk);
			}
		}
	}

	public bool GetPerkValue(PerkObject perk)
	{
		if (_heroPerks == null)
		{
			return false;
		}
		return _heroPerks.GetPropertyValue(perk) != 0;
	}

	public void ClearPerks()
	{
		foreach (SkillObject property in _heroSkills.GetProperties())
		{
			PerkHelper.ClearPerksForSkill(this, property);
		}
	}

	public Hero(string stringId, CharacterObject characterObject, CampaignTime birthDay)
		: this()
	{
		_birthDay = birthDay;
		base.StringId = Campaign.Current.CampaignObjectManager.FindNextUniqueStringId<Hero>(stringId);
		SetCharacterObject(characterObject);
		Campaign.Current.CampaignObjectManager.AddHero(this);
	}

	public Hero(string stringId, CharacterObject characterObject, CampaignTime birthDay, CampaignTime deathDay)
		: this()
	{
		_birthDay = birthDay;
		_deathDay = deathDay;
		_heroState = ((_deathDay != CampaignTime.Never) ? CharacterStates.Dead : CharacterStates.NotSpawned);
		base.StringId = Campaign.Current.CampaignObjectManager.FindNextUniqueStringId<Hero>(stringId);
		SetCharacterObject(characterObject);
		Campaign.Current.CampaignObjectManager.AddHero(this);
	}

	public Hero()
	{
		_battleEquipment = null;
		_civilianEquipment = null;
		_stealthEquipment = null;
		_gold = 0;
		OwnedCaravans = new List<CaravanPartyComponent>();
		OwnedAlleys = new List<Alley>();
		SpecialItems = new MBList<ItemObject>();
		_health = 1;
		_birthDay = default(CampaignTime);
		_deathDay = CampaignTime.Never;
		_heroState = CharacterStates.NotSpawned;
		_heroDeveloper = new HeroDeveloper(this);
		_heroSkills = new PropertyOwner<SkillObject>();
		_heroTraits = new PropertyOwner<TraitObject>();
		_heroPerks = new PropertyOwner<PerkObject>();
		_characterAttributes = new PropertyOwner<CharacterAttribute>();
		VolunteerTypes = new CharacterObject[6];
	}

	[LoadInitializationCallback]
	private void OnLoad(MetaData metaData, ObjectLoadData objectLoadData)
	{
		OwnedCaravans = new List<CaravanPartyComponent>();
		OwnedAlleys = new List<Alley>();
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("e1.7.3.0"))
		{
			PreferredUpgradeFormation = FormationClass.NumberOfAllFormations;
		}
		if (_firstName == null)
		{
			_firstName = Name;
		}
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("e1.8.0.0"))
		{
			object memberValueBySaveId;
			bool flag = default(bool);
			int num;
			if ((memberValueBySaveId = objectLoadData.GetMemberValueBySaveId(310)) is bool)
			{
				flag = (bool)memberValueBySaveId;
				num = 1;
			}
			else
			{
				num = 0;
			}
			IsNobleForOldSaves = (byte)((uint)num & (flag ? 1u : 0u)) != 0;
		}
	}

	protected override void PreAfterLoad()
	{
		if (!_characterObject.IsObsolete)
		{
			_supporterOf?.OnSupporterNotableAdded(this);
			if (_companionOf != null)
			{
				_companionOf.OnCompanionAdded(this);
			}
			else if (_clan != null && _clan.StringId != "neutral" && Occupation == Occupation.Lord)
			{
				_clan.OnLordAdded(this);
			}
			if (CurrentSettlement != null && PartyBelongedTo == null && PartyBelongedToAsPrisoner == null)
			{
				CurrentSettlement.AddHeroWithoutParty(this);
			}
		}
		if (MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.1.0") && FirstName != null && Name != null && this != MainHero)
		{
			if (Name.Attributes == null || !Name.Attributes.ContainsKey("FIRSTNAME"))
			{
				Name.SetTextVariable("FIRSTNAME", FirstName.CopyTextObject());
			}
			if (Name.Attributes == null || !Name.Attributes.ContainsKey("FEMALE"))
			{
				Name.SetTextVariable("FEMALE", IsFemale ? 1 : 0);
			}
		}
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.4.0")) && Name.Attributes != null && Name.Attributes.ContainsKey("FIRSTNAME") && Name.Attributes["FIRSTNAME"] is TextObject textObject && textObject != null && (object)Name == textObject)
		{
			Name.Attributes["FIRSTNAME"] = new TextObject(Name.Value);
		}
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.2.8.31599")) && !CharacterObject.IsTemplate && !CharacterObject.HiddenInEncyclopedia && PartyBelongedTo != null && PartyBelongedTo.LeaderHero != this && (CharacterObject.Occupation == Occupation.Soldier || CharacterObject.Occupation == Occupation.Mercenary || CharacterObject.Occupation == Occupation.Bandit || CharacterObject.Occupation == Occupation.Gangster || CharacterObject.Occupation == Occupation.CaravanGuard || (CharacterObject.Occupation == Occupation.Villager && CharacterObject.UpgradeTargets.Length != 0)))
		{
			PartyBelongedTo.MemberRoster.AddToCounts(CharacterObject, -PartyBelongedTo.MemberRoster.GetTroopCount(CharacterObject));
		}
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.14")) && PartyBelongedToAsPrisoner != null && StayingInSettlement != null)
		{
			StayingInSettlement = null;
			if (PartyBelongedToAsPrisoner != null && PartyBelongedToAsPrisoner.PrisonRoster.Contains(CharacterObject))
			{
				PartyBelongedToAsPrisoner.PrisonRoster.RemoveTroop(CharacterObject);
			}
			PartyBelongedToAsPrisoner = null;
			if (!IsDead && !IsDisabled)
			{
				ChangeState(CharacterStates.Fugitive);
			}
		}
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.4.0") && OriginClan == null)
		{
			if (Father != null)
			{
				_originClan = Father.Clan;
			}
			else
			{
				_originClan = Clan;
			}
		}
		UpdatePowerModifier();
	}

	private void ClearChangedPerks()
	{
		foreach (PerkObject item in _heroPerks.GetProperties().ToMBList())
		{
			if (item == null || item.IsTrash || (float)GetSkillValue(item.Skill) < item.RequiredSkillValue)
			{
				_heroPerks.SetPropertyValue(item, 0);
			}
		}
	}

	protected override void AfterLoad()
	{
		if (IsAlive)
		{
			ClearChangedPerks();
		}
		HeroDeveloper?.AfterLoad();
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.2.9.35637")) && GovernorOf != null && (PartyBelongedTo != null || PartyBelongedToAsPrisoner != null))
		{
			ChangeGovernorAction.RemoveGovernorOf(this);
		}
		if (MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.2.8.31599")))
		{
			if (this != MainHero && IsPrisoner && this != MainHero && IsPrisoner && PartyBelongedToAsPrisoner == null && CurrentSettlement != null)
			{
				PartyBelongedToAsPrisoner = CurrentSettlement.Party;
			}
			MobileParty mainParty = MobileParty.MainParty;
			if (this != MainHero && PartyBelongedTo == mainParty && !mainParty.MemberRoster.Contains(CharacterObject))
			{
				MakeHeroFugitiveAction.Apply(this);
			}
			if (mainParty.MemberRoster.Contains(CharacterObject) && PartyBelongedTo != mainParty)
			{
				mainParty.MemberRoster.RemoveTroop(CharacterObject, mainParty.MemberRoster.GetElementNumber(CharacterObject));
				if (!Campaign.Current.IssueManager.IssueSolvingCompanionList.Contains(this))
				{
					MobileParty partyBelongedTo = PartyBelongedTo;
					if (partyBelongedTo != null && !partyBelongedTo.IsCaravan)
					{
						MakeHeroFugitiveAction.Apply(this);
					}
				}
			}
			if (Spouse != null && (Spouse.Clan != Clan || Clan == null || Age < (float)Campaign.Current.Models.AgeModel.HeroComesOfAge))
			{
				Hero spouse = Spouse;
				Spouse = null;
				_exSpouses.Remove(spouse);
				spouse._exSpouses.Remove(this);
				MBReadOnlyList<LogEntry> gameActionLogs = Campaign.Current.LogEntryHistory.GameActionLogs;
				for (int num = gameActionLogs.Count - 1; num >= 0; num--)
				{
					if (gameActionLogs[num] is CharacterMarriedLogEntry characterMarriedLogEntry && (characterMarriedLogEntry.IsVisibleInEncyclopediaPageOf(this) || characterMarriedLogEntry.IsVisibleInEncyclopediaPageOf(this)))
					{
						Campaign.Current.LogEntryHistory.DeleteLogAtIndex(num);
					}
				}
				Hero hero = Mother ?? Father;
				if (hero != null)
				{
					Clan = hero.Clan;
				}
				else if (Age < (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && !IsDead)
				{
					KillCharacterAction.ApplyByRemove(this);
				}
				else
				{
					Clan clan = null;
					int num2 = int.MaxValue;
					for (int i = 0; i < Clan.All.Count; i++)
					{
						Clan clan2 = Clan.All[i];
						if (clan2 != Clan.PlayerClan && !clan2.IsBanditFaction && !clan2.IsRebelClan && !clan2.IsEliminated && Culture == clan2.Culture && clan2.Heroes.Count < num2)
						{
							num2 = clan2.Heroes.Count;
							clan = clan2;
						}
					}
					if (clan == null)
					{
						clan = Clan.All.GetRandomElementWithPredicate((Clan currentClan) => currentClan != Clan.PlayerClan && !currentClan.IsBanditFaction && !currentClan.IsRebelClan && !currentClan.IsEliminated);
					}
					Clan = clan;
				}
			}
		}
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.1.1") && !IsDead && CurrentSettlement == null && IsNotable && BornSettlement != null)
		{
			TeleportHeroAction.ApplyImmediateTeleportToSettlement(this, BornSettlement);
			if (!IsActive)
			{
				ChangeState(CharacterStates.Active);
			}
			UpdateHomeSettlement();
		}
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.2.8.31599")) && !CharacterObject.IsTemplate && !CharacterObject.HiddenInEncyclopedia && (CharacterObject.Occupation == Occupation.Soldier || CharacterObject.Occupation == Occupation.Mercenary || CharacterObject.Occupation == Occupation.Bandit || CharacterObject.Occupation == Occupation.Gangster || CharacterObject.Occupation == Occupation.CaravanGuard || (CharacterObject.Occupation == Occupation.Villager && CharacterObject.UpgradeTargets.Length != 0)))
		{
			if (PartyBelongedTo != null)
			{
				if (PartyBelongedTo.LeaderHero == this)
				{
					DestroyPartyAction.Apply(null, PartyBelongedTo);
				}
				else
				{
					PartyBelongedTo.MemberRoster.AddToCounts(CharacterObject, -PartyBelongedTo.MemberRoster.GetTroopCount(CharacterObject));
				}
			}
			if (PartyBelongedToAsPrisoner != null)
			{
				EndCaptivityAction.ApplyByDeath(this);
			}
			if (IsAlive)
			{
				KillCharacterAction.ApplyByRemove(this);
			}
			Campaign.Current.CampaignObjectManager.UnregisterDeadHero(this);
			Campaign.Current.ObjectManager.UnregisterObject(CharacterObject);
		}
		if (!MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.4.1.113285")) || this == MainHero || !IsAlive || IsSpecial || !(Age > (float)Campaign.Current.Models.AgeModel.HeroComesOfAge))
		{
			return;
		}
		IEnumerable<Equipment> battleEquipments = MBEquipmentRosterExtensions.All.Find((MBEquipmentRoster x) => x.StringId == "generic_bat_dummy").GetBattleEquipments();
		IEnumerable<Equipment> civilianEquipments = MBEquipmentRosterExtensions.All.Find((MBEquipmentRoster x) => x.StringId == "generic_civ_dummy").GetCivilianEquipments();
		Equipment other = battleEquipments.First();
		Equipment equipment = civilianEquipments.First();
		bool flag = false;
		bool flag2 = false;
		ItemFlags p = (IsFemale ? ItemFlags.NotUsableByFemale : ItemFlags.NotUsableByMale);
		for (int num3 = 5; num3 < 12; num3++)
		{
			EquipmentElement equipmentFromSlot = BattleEquipment.GetEquipmentFromSlot((EquipmentIndex)num3);
			EquipmentElement equipmentFromSlot2 = CivilianEquipment.GetEquipmentFromSlot((EquipmentIndex)num3);
			if (equipmentFromSlot.Item != null && equipmentFromSlot.Item.ItemFlags.HasAnyFlag(p))
			{
				flag = true;
			}
			if (equipmentFromSlot2.Item != null && equipmentFromSlot2.Item.ItemFlags.HasAnyFlag(p))
			{
				flag2 = true;
			}
			if (flag && flag2)
			{
				break;
			}
		}
		bool flag3 = !IsNoncombatant && !BattleEquipment.HasWeapon();
		if (BattleEquipment.IsEmpty() || BattleEquipment.IsEquipmentEqualTo(other) || flag || flag3)
		{
			Equipment equipmentForHeroComeOfAge = Campaign.Current.Models.EquipmentSelectionModel.GetEquipmentForHeroComeOfAge(this, Equipment.EquipmentType.Battle);
			EquipmentHelper.AssignHeroEquipmentFromEquipment(this, equipmentForHeroComeOfAge);
		}
		if (CivilianEquipment.IsEmpty() || equipment.IsEquipmentEqualTo(CivilianEquipment) || flag2)
		{
			Equipment equipmentForHeroComeOfAge2 = Campaign.Current.Models.EquipmentSelectionModel.GetEquipmentForHeroComeOfAge(this, Equipment.EquipmentType.Civilian);
			EquipmentHelper.AssignHeroEquipmentFromEquipment(this, equipmentForHeroComeOfAge2);
		}
	}

	public void ChangeState(CharacterStates newState)
	{
		CharacterStates heroState = _heroState;
		_heroState = newState;
		_clan?.OnHeroChangedState(this, heroState);
		Campaign.Current.CampaignObjectManager.HeroStateChanged(this, heroState);
		switch (newState)
		{
		case CharacterStates.Traveling:
			CampaignEventDispatcher.Instance.OnHeroGetsBusy(this, HeroGetsBusyReasons.Traveling);
			break;
		case CharacterStates.Active:
			CampaignEventDispatcher.Instance.OnHeroActivated(this, heroState);
			break;
		}
	}

	public bool IsHealthFull()
	{
		return HitPoints >= CharacterObject.MaxHitPoints();
	}

	private void HealByAmountInternal(int healingAmount, bool addXp = false)
	{
		if (!IsHealthFull())
		{
			int num = TaleWorlds.Library.MathF.Min(healingAmount, CharacterObject.MaxHitPoints() - HitPoints);
			HitPoints += num;
			if (addXp)
			{
				SkillLevelingManager.OnHeroHealedWhileWaiting(this, num);
			}
		}
	}

	public void Heal(int healAmount, bool addXp = false)
	{
		int heroesEffectedHealingAmount = Campaign.Current.Models.PartyHealingModel.GetHeroesEffectedHealingAmount(this, healAmount);
		HealByAmountInternal(heroesEffectedHealingAmount, addXp);
	}

	public override void Deserialize(MBObjectManager objectManager, XmlNode node)
	{
		base.Deserialize(objectManager, node);
		CharacterObject characterObject = MBObjectManager.Instance.GetObject<CharacterObject>(base.StringId);
		float age = characterObject.Age;
		SetCharacterObject(characterObject);
		foreach (var (skill, value) in Campaign.Current.Models.HeroCreationModel.GetDefaultSkillsForHero(this))
		{
			SetSkillValue(skill, value);
		}
		StaticBodyProperties = CharacterObject.GetBodyPropertiesMin(returnBaseValue: true).StaticProperties;
		DynamicBodyProperties dynamicBodyProperties = CharacterObject.GetBodyPropertiesMin(returnBaseValue: true).DynamicProperties;
		if (dynamicBodyProperties == DynamicBodyProperties.Invalid)
		{
			dynamicBodyProperties = DynamicBodyProperties.Default;
		}
		Weight = dynamicBodyProperties.Weight;
		Build = dynamicBodyProperties.Build;
		XmlAttribute xmlAttribute = node.Attributes["alive"];
		bool flag = xmlAttribute != null && xmlAttribute.Value == "false";
		_heroState = (flag ? CharacterStates.Dead : CharacterStates.NotSpawned);
		if (IsDead)
		{
			HeroHelper.GetRandomDeathDayAndBirthDay((int)age, out _birthDay, out _deathDay);
		}
		Father = objectManager.ReadObjectReferenceFromXml("father", typeof(Hero), node) as Hero;
		Mother = objectManager.ReadObjectReferenceFromXml("mother", typeof(Hero), node) as Hero;
		if (Spouse == null)
		{
			Spouse = objectManager.ReadObjectReferenceFromXml("spouse", typeof(Hero), node) as Hero;
		}
		Clan clan = objectManager.ReadObjectReferenceFromXml("faction", typeof(Clan), node) as Clan;
		if (clan.StringId != "neutral")
		{
			Clan = clan;
		}
		EncyclopediaText = ((node.Attributes["text"] != null) ? new TextObject(node.Attributes["text"].Value) : TextObject.GetEmpty());
		XmlNode xmlNode = node.Attributes["preferred_upgrade_formation"];
		PreferredUpgradeFormation = FormationClass.NumberOfAllFormations;
		if (xmlNode != null && Enum.TryParse<FormationClass>(xmlNode.InnerText, ignoreCase: true, out var result))
		{
			PreferredUpgradeFormation = result;
		}
		ItemObject itemObject = ((node.Attributes["banner_item"] != null) ? MBObjectManager.Instance.GetObject<ItemObject>(node.Attributes["banner_item"].Value) : null);
		if (itemObject != null)
		{
			BannerItem = new EquipmentElement(itemObject);
		}
		HeroDeveloper.InitializeHeroDeveloper();
	}

	public bool CanLeadParty()
	{
		bool result = true;
		CampaignEventDispatcher.Instance.CanHeroLeadParty(this, ref result);
		return result;
	}

	public static TextObject SetHeroEncyclopediaTextAndLinks(Hero o)
	{
		StringHelpers.SetCharacterProperties("LORD", o.CharacterObject);
		MBTextManager.SetTextVariable("TITLE", HeroHelper.GetTitleInIndefiniteCase(o));
		MBTextManager.SetTextVariable("REPUTATION", CharacterHelper.GetReputationDescription(o.CharacterObject));
		MBTextManager.SetTextVariable("FACTION_NAME", GameTexts.FindText("str_neutral_term_for_culture", o.MapFaction.IsMinorFaction ? o.Culture.StringId : o.MapFaction.Culture.StringId));
		if (o.MapFaction.Culture.StringId == "empire")
		{
			MBTextManager.SetTextVariable("FACTION_NAME", "{=empirefaction}Empire");
		}
		MBTextManager.SetTextVariable("CLAN_NAME", o.Clan.Name);
		if (o.Clan.IsMinorFaction || o.Clan.IsRebelClan)
		{
			if (o.Clan == MainHero.Clan)
			{
				MBTextManager.SetTextVariable("CLAN_DESCRIPTION", "{=REWGj2ge}a rising new clan");
			}
			else if (o.Clan.IsSect)
			{
				MBTextManager.SetTextVariable("CLAN_DESCRIPTION", "{=IlRC9Drl}a religious sect");
			}
			else if (o.Clan.IsClanTypeMercenary)
			{
				MBTextManager.SetTextVariable("CLAN_DESCRIPTION", "{=5cH6ssDI}a mercenary company");
			}
			else if (o.Clan.IsNomad)
			{
				MBTextManager.SetTextVariable("CLAN_DESCRIPTION", "{=nt1ra97u}a nomadic clan");
			}
			else if (o.Clan.IsMafia)
			{
				MBTextManager.SetTextVariable("CLAN_DESCRIPTION", "{=EmBEupR5}a secret society");
			}
			else
			{
				MBTextManager.SetTextVariable("CLAN_DESCRIPTION", "{=KZxKVby0}an organization");
			}
			if (o == MainHero)
			{
				if (o.GetTraitLevel(DefaultTraits.Mercy) == 0 && o.GetTraitLevel(DefaultTraits.Honor) == 0 && o.GetTraitLevel(DefaultTraits.Generosity) == 0 && o.GetTraitLevel(DefaultTraits.Valor) == 0 && o.GetTraitLevel(DefaultTraits.Calculating) == 0)
				{
					return new TextObject("{=V097rA1v}{LORD.FIRSTNAME} is a head of the {CLAN_NAME}, a rising new clan. {?LORD.GENDER}She{?}He{\\?} is still making {?LORD.GENDER}her{?}his{\\?} reputation.");
				}
				return new TextObject("{=hRfXSdlP}{LORD.FIRSTNAME} is a head of the {CLAN_NAME}, {CLAN_DESCRIPTION} from the lands of the {FACTION_NAME}. {?LORD.GENDER}She{?}He{\\?} has the reputation of being {REPUTATION}.");
			}
			return new TextObject("{=9Obe3S6L}{LORD.FIRSTNAME} is a member of the {CLAN_NAME}, {CLAN_DESCRIPTION} from the lands of the {FACTION_NAME}. {?LORD.GENDER}She{?}He{\\?} has the reputation of being {REPUTATION}.");
		}
		List<Kingdom> list = Campaign.Current.Kingdoms.Where((Kingdom x) => x.Culture == o.MapFaction.Culture).ToList();
		if (list.Count > 1)
		{
			MBTextManager.SetTextVariable("RULER", o.MapFaction.Leader.Name);
		}
		MBTextManager.SetTextVariable("CLAN_DESCRIPTION", "{=KzSeg8ks}a noble family");
		if (list.Count == 1)
		{
			if (o.Clan.Leader == o)
			{
				return new TextObject("{=6d4ZTvGv}{LORD.NAME} is {TITLE} of the {FACTION_NAME} and head of the {CLAN_NAME}, {CLAN_DESCRIPTION} of the realm. {?LORD.GENDER}She{?}He{\\?} has the reputation of being {REPUTATION}.");
			}
			return new TextObject("{=o5AUljbW}{LORD.NAME} is a member of the {CLAN_NAME}, {CLAN_DESCRIPTION} of the {FACTION_NAME}. {?LORD.GENDER}She{?}He{\\?} has the reputation of being {REPUTATION}.");
		}
		if (list.Count > 1)
		{
			if (o.Clan.Leader == o)
			{
				return new TextObject("{=JuPUG5wX}{LORD.NAME} is {TITLE} of the {FACTION_NAME} and head of the {CLAN_NAME}, {CLAN_DESCRIPTION} that is backing {RULER} in the civil war. {?LORD.GENDER}She{?}He{\\?} has the reputation of being {REPUTATION}.");
			}
			return new TextObject("{=0bPb5btR}{LORD.NAME} is a member of the {CLAN_NAME}, {CLAN_DESCRIPTION} of the {FACTION_NAME} that is backing {RULER} in the civil war. {?LORD.GENDER}She{?}He{\\?} has the reputation of being {REPUTATION}.");
		}
		return new TextObject("{=!}Placeholder text");
	}

	public bool CanHeroEquipmentBeChanged()
	{
		bool result = true;
		CampaignEventDispatcher.Instance.CanHeroEquipmentBeChanged(this, ref result);
		return result;
	}

	public bool CanMarry()
	{
		if (!Campaign.Current.Models.MarriageModel.IsSuitableForMarriage(this))
		{
			return false;
		}
		bool result = true;
		CampaignEventDispatcher.Instance.CanHeroMarry(this, ref result);
		return result;
	}

	internal void OnDeath()
	{
		ClearAttributes();
		_heroSkills = null;
		_heroPerks = null;
		_heroTraits = null;
		_characterAttributes = null;
		_heroDeveloper = null;
		VolunteerTypes = null;
		_battleEquipment = null;
		_civilianEquipment = null;
		_stealthEquipment = null;
		SupporterOf = null;
		DeathMarkKillerHero = null;
		LastKnownClosestSettlement = null;
	}

	private void SetPartyBelongedTo(MobileParty party)
	{
		if (_partyBelongedTo != party && _partyBelongedTo != null)
		{
			if (_partyBelongedTo.LeaderHero == this)
			{
				_partyBelongedTo.PartyComponent.ChangePartyLeader(null);
			}
			_partyBelongedTo.RemoveAllPartyRolesOfHero(this);
		}
		_partyBelongedTo = party;
	}

	public bool CanBeGovernorOrHavePartyRole()
	{
		if (IsPrisoner)
		{
			return false;
		}
		bool result = true;
		CampaignEventDispatcher.Instance.CanBeGovernorOrHavePartyRole(this, ref result);
		return result;
	}

	public bool CanDie(KillCharacterAction.KillCharacterActionDetail causeOfDeath)
	{
		if (CampaignOptions.IsLifeDeathCycleDisabled && causeOfDeath == KillCharacterAction.KillCharacterActionDetail.DiedOfOldAge)
		{
			return false;
		}
		bool result = true;
		CampaignEventDispatcher.Instance.CanHeroDie(this, causeOfDeath, ref result);
		return result;
	}

	public bool CanBecomePrisoner()
	{
		if (!IsLord && !IsPlayerCompanion && !IsSpecial)
		{
			Debug.FailedAssert("Only lords, companions and special quest heroes can become prisoners! Check CanBecomePrisoner usage.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Hero.cs", "CanBecomePrisoner", 1922);
			return false;
		}
		if (this != MainHero)
		{
			return true;
		}
		bool result = true;
		CampaignEventDispatcher.Instance.CanHeroBecomePrisoner(this, ref result);
		return result;
	}

	public bool CanMoveToSettlement()
	{
		bool result = true;
		CampaignEventDispatcher.Instance.CanMoveToSettlement(this, ref result);
		return result;
	}

	public bool CanHaveCampaignIssues()
	{
		if (Issue != null)
		{
			return false;
		}
		bool result = true;
		CampaignEventDispatcher.Instance.CanHaveCampaignIssues(this, ref result);
		return result;
	}

	public void AddInfluenceWithKingdom(float additionalInfluence)
	{
		float randomFloat = MBRandom.RandomFloat;
		ChangeClanInfluenceAction.Apply(Clan, (int)additionalInfluence + ((randomFloat < additionalInfluence - (float)TaleWorlds.Library.MathF.Floor(additionalInfluence)) ? 1 : 0));
	}

	public float GetRelationWithPlayer()
	{
		return MainHero.GetRelation(this);
	}

	public float GetUnmodifiedClanLeaderRelationshipWithPlayer()
	{
		return MainHero.GetBaseHeroRelation(this);
	}

	public void SetTextVariables()
	{
		MBTextManager.SetTextVariable("SALUTATION_BY_PLAYER", (!CharacterObject.OneToOneConversationCharacter.IsFemale) ? GameTexts.FindText("str_my_lord") : GameTexts.FindText("str_my_lady"));
		if (!TextObject.IsNullOrEmpty(FirstName))
		{
			MBTextManager.SetTextVariable("FIRST_NAME", FirstName);
		}
		else
		{
			MBTextManager.SetTextVariable("FIRST_NAME", Name);
		}
		MBTextManager.SetTextVariable("GENDER", IsFemale ? 1 : 0);
	}

	public void SetPersonalRelation(Hero otherHero, int value)
	{
		value = MBMath.ClampInt(value, Campaign.Current.Models.DiplomacyModel.MinRelationLimit, Campaign.Current.Models.DiplomacyModel.MaxRelationLimit);
		CharacterRelationManager.SetHeroRelation(this, otherHero, value);
	}

	public int GetRelation(Hero otherHero)
	{
		if (otherHero == this)
		{
			return 0;
		}
		return Campaign.Current.Models.DiplomacyModel.GetEffectiveRelation(this, otherHero);
	}

	public int GetBaseHeroRelation(Hero otherHero)
	{
		return Campaign.Current.Models.DiplomacyModel.GetBaseRelation(this, otherHero);
	}

	public bool IsEnemy(Hero otherHero)
	{
		return CharacterRelationManager.GetHeroRelation(this, otherHero) < Campaign.Current.Models.DiplomacyModel.MinNeutralRelationLimit;
	}

	public bool IsFriend(Hero otherHero)
	{
		return CharacterRelationManager.GetHeroRelation(this, otherHero) > Campaign.Current.Models.DiplomacyModel.MaxNeutralRelationLimit;
	}

	public bool IsNeutral(Hero otherHero)
	{
		if (!IsFriend(otherHero))
		{
			return !IsEnemy(otherHero);
		}
		return false;
	}

	public void ModifyHair(int hair, int beard, int tattoo)
	{
		BodyProperties bodyProperties = BodyProperties;
		FaceGen.SetHair(ref bodyProperties, hair, beard, tattoo);
		StaticBodyProperties = bodyProperties.StaticProperties;
	}

	public void AddOwnedWorkshop(Workshop workshop)
	{
		if (!_ownedWorkshops.Contains(workshop))
		{
			_ownedWorkshops.Add(workshop);
		}
	}

	public void RemoveOwnedWorkshop(Workshop workshop)
	{
		if (_ownedWorkshops.Contains(workshop))
		{
			_ownedWorkshops.Remove(workshop);
		}
	}

	public static Hero FindFirst(Func<Hero, bool> predicate)
	{
		return Campaign.Current.Characters.FirstOrDefault((CharacterObject x) => x.IsHero && predicate(x.HeroObject))?.HeroObject;
	}

	public static Hero Find(string stringId)
	{
		return Campaign.Current.CampaignObjectManager.Find<Hero>(stringId);
	}

	public static IEnumerable<Hero> FindAll(Func<Hero, bool> predicate)
	{
		return from x in Campaign.Current.Characters
			where x.IsHero && predicate(x.HeroObject)
			select x.HeroObject;
	}

	public void MakeWounded(Hero killerHero = null, KillCharacterAction.KillCharacterActionDetail deathMarkDetail = KillCharacterAction.KillCharacterActionDetail.None)
	{
		DeathMark = deathMarkDetail;
		DeathMarkKillerHero = killerHero;
		HitPoints = 1;
	}

	public void AddDeathMark(Hero killerHero = null, KillCharacterAction.KillCharacterActionDetail deathMarkDetail = KillCharacterAction.KillCharacterActionDetail.None)
	{
		DeathMark = deathMarkDetail;
		DeathMarkKillerHero = killerHero;
	}

	internal void OnAddedToParty(MobileParty mobileParty)
	{
		PartyBelongedTo = mobileParty;
		StayingInSettlement = null;
	}

	internal void OnRemovedFromParty(MobileParty mobileParty)
	{
		PartyBelongedTo = null;
	}

	internal void OnAddedToPartyAsPrisoner(PartyBase party)
	{
		PartyBelongedToAsPrisoner = party;
		PartyBelongedTo = null;
	}

	internal void OnRemovedFromPartyAsPrisoner(PartyBase party)
	{
		PartyBelongedToAsPrisoner = null;
	}

	public Vec3 GetPositionAsVec3()
	{
		return GetCampaignPosition().AsVec3();
	}

	public CampaignVec2 GetCampaignPosition()
	{
		CampaignVec2 result = CampaignVec2.Invalid;
		if (CurrentSettlement != null)
		{
			result = CurrentSettlement.GatePosition;
		}
		else if (IsPrisoner && PartyBelongedToAsPrisoner != null)
		{
			result = (PartyBelongedToAsPrisoner.IsSettlement ? PartyBelongedToAsPrisoner.Settlement.GatePosition : PartyBelongedToAsPrisoner.MobileParty.Position);
		}
		else if (PartyBelongedTo != null)
		{
			result = PartyBelongedTo.Position;
		}
		return result;
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
		return ClanBanner;
	}

	public IMapPoint GetMapPoint()
	{
		if (CurrentSettlement != null)
		{
			return CurrentSettlement;
		}
		if (IsPrisoner && PartyBelongedToAsPrisoner != null)
		{
			if (!PartyBelongedToAsPrisoner.IsSettlement)
			{
				return PartyBelongedToAsPrisoner.MobileParty;
			}
			return PartyBelongedToAsPrisoner.Settlement;
		}
		return PartyBelongedTo;
	}

	private void SetInitialValuesFromCharacter(CharacterObject characterObject)
	{
		foreach (TraitObject item in TraitObject.All)
		{
			SetTraitLevel(item, characterObject.GetTraitLevel(item));
		}
		Level = characterObject.Level;
		SetName(characterObject.Name, characterObject.Name);
		Culture = characterObject.Culture;
		_defaultAge = characterObject.Age;
		if (_birthDay == CampaignTime.Zero)
		{
			_birthDay = HeroHelper.GetRandomBirthDayForAge(_defaultAge);
		}
		HitPoints = characterObject.MaxHitPoints();
		IsFemale = characterObject.IsFemale;
		Occupation = CharacterObject.GetDefaultOccupation();
		List<Equipment> list = characterObject.BattleEquipments.Where((Equipment x) => !x.IsEmpty()).ToList();
		List<Equipment> list2 = characterObject.CivilianEquipments.Where((Equipment x) => !x.IsEmpty()).ToList();
		List<Equipment> list3 = characterObject.StealthEquipments.Where((Equipment x) => !x.IsEmpty()).ToList();
		if (list.IsEmpty())
		{
			CultureObject cultureObject = Game.Current.ObjectManager.GetObject<CultureObject>("neutral_culture");
			list.AddRange(cultureObject.DefaultBattleEquipmentRoster.AllEquipments);
		}
		if (list2.IsEmpty())
		{
			CultureObject cultureObject2 = Game.Current.ObjectManager.GetObject<CultureObject>("neutral_culture");
			list2.AddRange(cultureObject2.DefaultCivilianEquipmentRoster.AllEquipments);
		}
		if (list3.IsEmpty())
		{
			CultureObject cultureObject3 = Game.Current.ObjectManager.GetObject<CultureObject>("neutral_culture");
			list3.AddRange(cultureObject3.DefaultStealthEquipmentRoster.AllEquipments);
		}
		if (!list.IsEmpty())
		{
			_battleEquipment = list.GetRandomElement().Clone();
		}
		if (!list2.IsEmpty())
		{
			_civilianEquipment = list2.GetRandomElement().Clone();
		}
		if (!list3.IsEmpty())
		{
			_stealthEquipment = list3.GetRandomElement().Clone();
		}
	}

	public void ResetEquipments()
	{
		_battleEquipment = Template.FirstBattleEquipment.Clone();
		_civilianEquipment = Template.FirstCivilianEquipment.Clone();
		_stealthEquipment = Template.FirstStealthEquipment.Clone();
	}

	public void ChangeHeroGold(int changeAmount)
	{
		int num = 0;
		num = ((changeAmount <= int.MaxValue - _gold) ? (_gold + changeAmount) : int.MaxValue);
		Gold = num;
	}

	public void CheckInvalidEquipmentsAndReplaceIfNeeded()
	{
		for (int i = 0; i < 12; i++)
		{
			if (BattleEquipment?[i].Item == DefaultItems.Trash)
			{
				HandleInvalidItem(isBattleEquipment: true, i);
			}
			else if (BattleEquipment?[i].Item != null)
			{
				if (!BattleEquipment[i].Item.IsReady)
				{
					if (MBObjectManager.Instance.GetObject(BattleEquipment[i].Item.Id) == BattleEquipment[i].Item)
					{
						MBObjectManager.Instance.UnregisterObject(BattleEquipment[i].Item);
					}
					HandleInvalidItem(isBattleEquipment: true, i);
					PartyBelongedTo?.ItemRoster.AddToCounts(DefaultItems.Trash, 1);
				}
				ItemModifier itemModifier = BattleEquipment[i].ItemModifier;
				if (itemModifier != null && !itemModifier.IsReady)
				{
					HandleInvalidModifier(isBattleEquipment: true, i);
				}
			}
			if (CivilianEquipment?[i].Item == DefaultItems.Trash)
			{
				HandleInvalidItem(isBattleEquipment: false, i);
			}
			else
			{
				if (CivilianEquipment?[i].Item == null)
				{
					continue;
				}
				if (!CivilianEquipment[i].Item.IsReady)
				{
					if (MBObjectManager.Instance.GetObject(CivilianEquipment[i].Item.Id) == CivilianEquipment[i].Item)
					{
						MBObjectManager.Instance.UnregisterObject(CivilianEquipment[i].Item);
					}
					HandleInvalidItem(isBattleEquipment: false, i);
					PartyBelongedTo?.ItemRoster.AddToCounts(DefaultItems.Trash, 1);
				}
				ItemModifier itemModifier2 = CivilianEquipment[i].ItemModifier;
				if (itemModifier2 != null && !itemModifier2.IsReady)
				{
					HandleInvalidModifier(isBattleEquipment: false, i);
				}
			}
		}
	}

	private void HandleInvalidItem(bool isBattleEquipment, int i)
	{
		if (IsHumanPlayerCharacter)
		{
			if (isBattleEquipment)
			{
				BattleEquipment[i] = EquipmentElement.Invalid;
			}
			else
			{
				CivilianEquipment[i] = EquipmentElement.Invalid;
			}
			return;
		}
		List<Equipment> list = (isBattleEquipment ? CharacterObject.BattleEquipments.Where((Equipment t) => !t.IsEmpty()).ToList() : CharacterObject.CivilianEquipments.Where((Equipment t) => !t.IsEmpty()).ToList());
		EquipmentElement value = list[this.RandomInt(list.Count)][i];
		if (value.IsEmpty || !value.Item.IsReady)
		{
			value = EquipmentElement.Invalid;
		}
		if (!isBattleEquipment)
		{
			_ = CivilianEquipment[i];
		}
		else
		{
			_ = BattleEquipment[i];
		}
		if (isBattleEquipment)
		{
			BattleEquipment[i] = value;
		}
		else
		{
			CivilianEquipment[i] = value;
		}
	}

	private void HandleInvalidModifier(bool isBattleEquipment, int i)
	{
		if (isBattleEquipment)
		{
			BattleEquipment[i] = new EquipmentElement(BattleEquipment[i].Item);
		}
		else
		{
			CivilianEquipment[i] = new EquipmentElement(CivilianEquipment[i].Item);
		}
	}

	internal void ResetClanForOldSave()
	{
		_clan = null;
	}

	[SpecialName]
	bool ITrackableCampaignObject.get_IsReady()
	{
		return base.IsReady;
	}
}
