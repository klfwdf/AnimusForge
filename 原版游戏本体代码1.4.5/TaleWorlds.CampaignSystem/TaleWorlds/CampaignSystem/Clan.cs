using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem;

public sealed class Clan : MBObjectBase, IFaction
{
	[SaveableField(54)]
	private PartyTemplateObject _defaultPartyTemplate;

	[SaveableField(97)]
	private bool _isEliminated;

	[SaveableField(99)]
	private MBList<CharacterObject> _minorFactionCharacterTemplates;

	[CachedData]
	private MBList<Hero> _supporterNotablesCache;

	[SaveableField(57)]
	private Kingdom _kingdom;

	[CachedData]
	private MBList<Town> _fiefsCache;

	[CachedData]
	private MBList<Village> _villagesCache;

	[CachedData]
	private MBList<Settlement> _settlementsCache;

	[CachedData]
	private MBList<Hero> _aliveLordsCache;

	[CachedData]
	private MBList<Hero> _deadLordsCache;

	[CachedData]
	private MBList<Hero> _heroesCache;

	[CachedData]
	private MBList<Hero> _companionsCache;

	[CachedData]
	private MBList<WarPartyComponent> _warPartyComponentsCache;

	[SaveableField(62)]
	private float _influence;

	[CachedData]
	private Settlement _midSettlement;

	[SaveableField(82)]
	private CharacterObject _basicTroop;

	[SaveableField(83)]
	private Hero _leader;

	[SaveableField(84)]
	private Banner _banner;

	[SaveableField(91)]
	private int _tier;

	[SaveableField(120)]
	private float _aggressiveness;

	[SaveableField(130)]
	private int _tributeWallet;

	[SaveableField(95)]
	private Settlement _home;

	[SaveableField(110)]
	private int _clanDebtToKingdom;

	private MBList<IFaction> _factionsAtWarWith;

	[CachedData]
	private float _distanceToClosestNonAllyFortificationCache;

	[CachedData]
	internal bool _distanceToClosestNonAllyFortificationCacheDirty = true;

	[SaveableProperty(51)]
	public TextObject Name { get; private set; }

	[SaveableProperty(52)]
	public TextObject InformalName { get; private set; }

	[SaveableProperty(53)]
	public CultureObject Culture { get; set; }

	[SaveableProperty(55)]
	public CampaignTime LastFactionChangeTime { get; set; }

	public PartyTemplateObject DefaultPartyTemplate
	{
		get
		{
			if (_defaultPartyTemplate != null)
			{
				return _defaultPartyTemplate;
			}
			return Culture.DefaultPartyTemplate;
		}
	}

	public bool HasNavalNavigationCapability => DefaultPartyTemplate.ShipHulls.Count > 0;

	[SaveableProperty(58)]
	public int AutoRecruitmentExpenses { get; set; }

	[SaveableProperty(56)]
	public TextObject EncyclopediaText { get; private set; }

	[SaveableProperty(140)]
	public bool IsNoble { get; set; }

	public bool IsEliminated => _isEliminated;

	public IList<CharacterObject> MinorFactionCharacterTemplates => _minorFactionCharacterTemplates;

	public string EncyclopediaLink => Campaign.Current.EncyclopediaManager.GetIdentifier(typeof(Clan)) + "-" + base.StringId;

	public TextObject EncyclopediaLinkWithName => HyperlinkTexts.GetClanHyperlinkText(EncyclopediaLink, Name);

	public Kingdom Kingdom
	{
		get
		{
			return _kingdom;
		}
		set
		{
			if (_kingdom != value)
			{
				SetKingdomInternal(value);
			}
		}
	}

	public IEnumerable<CharacterObject> DungeonPrisonersOfClan
	{
		get
		{
			foreach (Town fief in Fiefs)
			{
				foreach (CharacterObject prisonerHero in fief.Settlement.Party.PrisonerHeroes)
				{
					yield return prisonerHero;
				}
			}
		}
	}

	public MBReadOnlyList<Town> Fiefs => _fiefsCache;

	public MBReadOnlyList<Village> Villages => _villagesCache;

	public MBReadOnlyList<Settlement> Settlements => _settlementsCache;

	public MBReadOnlyList<Hero> SupporterNotables => _supporterNotablesCache;

	public MBReadOnlyList<Hero> AliveLords => _aliveLordsCache;

	public MBReadOnlyList<Hero> DeadLords => _deadLordsCache;

	public MBReadOnlyList<Hero> Heroes => _heroesCache;

	public MBReadOnlyList<Hero> Companions => _companionsCache;

	public MBReadOnlyList<WarPartyComponent> WarPartyComponents => _warPartyComponentsCache;

	public float Influence
	{
		get
		{
			return _influence;
		}
		set
		{
			if (value < _influence && Leader != null)
			{
				SkillLevelingManager.OnInfluenceSpent(Leader, value - _influence);
			}
			_influence = value;
		}
	}

	public ExplainedNumber InfluenceChangeExplained => Campaign.Current.Models.ClanPoliticsModel.CalculateInfluenceChange(this, includeDescriptions: true);

	[CachedData]
	public float CurrentTotalStrength { get; private set; }

	[SaveableProperty(65)]
	public int MercenaryAwardMultiplier { get; set; }

	public bool IsMapFaction => _kingdom == null;

	[SaveableProperty(114)]
	public Settlement InitialHomeSettlement { get; private set; }

	[SaveableProperty(68)]
	public bool IsRebelClan { get; set; }

	[SaveableProperty(69)]
	public bool IsMinorFaction { get; private set; }

	[SaveableProperty(70)]
	public bool IsOutlaw { get; private set; }

	[SaveableProperty(71)]
	public bool IsNomad { get; private set; }

	[SaveableProperty(72)]
	public bool IsMafia { get; private set; }

	[SaveableProperty(73)]
	public bool IsClanTypeMercenary { get; private set; }

	[SaveableProperty(74)]
	public bool IsSect { get; private set; }

	[SaveableProperty(75)]
	public bool IsUnderMercenaryService { get; private set; }

	[SaveableProperty(188)]
	public CampaignTime ShouldStayInKingdomUntil { get; set; }

	[SaveableProperty(76)]
	public uint Color { get; set; }

	[SaveableProperty(77)]
	public uint Color2 { get; set; }

	[SaveableProperty(111)]
	private uint BannerBackgroundColorPrimary { get; set; }

	[SaveableProperty(112)]
	private uint BannerBackgroundColorSecondary { get; set; }

	[SaveableProperty(113)]
	private uint BannerIconColor { get; set; }

	public Settlement FactionMidSettlement => _midSettlement;

	public CharacterObject BasicTroop
	{
		get
		{
			return _basicTroop ?? Culture.BasicTroop;
		}
		set
		{
			_basicTroop = value;
		}
	}

	public static Clan PlayerClan => Campaign.Current.PlayerDefaultFaction;

	public Hero Leader => _leader;

	public int Gold => Leader?.Gold ?? 0;

	public Banner Banner
	{
		get
		{
			if (Kingdom == null || Kingdom.RulingClan != this)
			{
				return _banner;
			}
			return Kingdom.Banner;
		}
		set
		{
			_banner = value;
		}
	}

	public Banner ClanOriginalBanner => _banner;

	[SaveableProperty(85)]
	public bool IsBanditFaction { get; private set; }

	bool IFaction.IsKingdomFaction => false;

	public bool IsClan => true;

	[SaveableProperty(88)]
	public float Renown { get; set; }

	[SaveableProperty(89)]
	public float MainHeroCrimeRating { get; set; }

	public float DailyCrimeRatingChange => Campaign.Current.Models.CrimeModel.GetDailyCrimeRatingChange(this).ResultNumber;

	public ExplainedNumber DailyCrimeRatingChangeExplained => Campaign.Current.Models.CrimeModel.GetDailyCrimeRatingChange(this, includeDescriptions: true);

	public int Tier
	{
		get
		{
			return _tier;
		}
		private set
		{
			int minClanTier = Campaign.Current.Models.ClanTierModel.MinClanTier;
			int maxClanTier = Campaign.Current.Models.ClanTierModel.MaxClanTier;
			if (value > maxClanTier)
			{
				value = maxClanTier;
			}
			else if (value < minClanTier)
			{
				value = minClanTier;
			}
			_tier = value;
		}
	}

	public IFaction MapFaction
	{
		get
		{
			if (Kingdom != null)
			{
				return Kingdom;
			}
			return this;
		}
	}

	[SaveableProperty(100)]
	public CampaignTime NotAttackableByPlayerUntilTime { get; set; }

	public float Aggressiveness
	{
		get
		{
			return _aggressiveness;
		}
		internal set
		{
			_aggressiveness = TaleWorlds.Library.MathF.Clamp(value, 0f, 100f);
		}
	}

	public int TributeWallet
	{
		get
		{
			return _tributeWallet;
		}
		set
		{
			_tributeWallet = value;
		}
	}

	public Settlement HomeSettlement
	{
		get
		{
			return _home;
		}
		private set
		{
			_home = value;
		}
	}

	public int DebtToKingdom
	{
		get
		{
			return _clanDebtToKingdom;
		}
		set
		{
			_clanDebtToKingdom = value;
		}
	}

	public MBReadOnlyList<IFaction> FactionsAtWarWith => _factionsAtWarWith;

	public int RenownRequirementForNextTier => Campaign.Current.Models.ClanTierModel.GetRequiredRenownForTier(Tier + 1);

	public int CompanionLimit => Campaign.Current.Models.ClanTierModel.GetCompanionLimit(this);

	public float DistanceToClosestNonAllyFortification
	{
		get
		{
			if (_distanceToClosestNonAllyFortificationCacheDirty)
			{
				_distanceToClosestNonAllyFortificationCache = FactionHelper.GetDistanceToClosestNonAllyFortificationOfFaction(this);
				_distanceToClosestNonAllyFortificationCacheDirty = false;
			}
			return _distanceToClosestNonAllyFortificationCache;
		}
	}

	public int WarPartyLimit => Campaign.Current.Models.ClanTierModel.GetPartyLimitForTier(this, Tier);

	public static MBReadOnlyList<Clan> All => Campaign.Current.Clans;

	public static IEnumerable<Clan> NonBanditFactions
	{
		get
		{
			foreach (Clan clan in Campaign.Current.Clans)
			{
				if (!clan.IsBanditFaction)
				{
					yield return clan;
				}
			}
		}
	}

	public static IEnumerable<Clan> BanditFactions
	{
		get
		{
			foreach (Clan clan in Campaign.Current.Clans)
			{
				if (clan.IsBanditFaction)
				{
					yield return clan;
				}
			}
		}
	}

	internal static void AutoGeneratedStaticCollectObjectsClan(object o, List<object> collectedObjects)
	{
		((Clan)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		collectedObjects.Add(_defaultPartyTemplate);
		collectedObjects.Add(_minorFactionCharacterTemplates);
		collectedObjects.Add(_kingdom);
		collectedObjects.Add(_basicTroop);
		collectedObjects.Add(_leader);
		collectedObjects.Add(_banner);
		collectedObjects.Add(_home);
		collectedObjects.Add(Name);
		collectedObjects.Add(InformalName);
		collectedObjects.Add(Culture);
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(LastFactionChangeTime, collectedObjects);
		collectedObjects.Add(EncyclopediaText);
		collectedObjects.Add(InitialHomeSettlement);
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(ShouldStayInKingdomUntil, collectedObjects);
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(NotAttackableByPlayerUntilTime, collectedObjects);
	}

	internal static object AutoGeneratedGetMemberValueName(object o)
	{
		return ((Clan)o).Name;
	}

	internal static object AutoGeneratedGetMemberValueInformalName(object o)
	{
		return ((Clan)o).InformalName;
	}

	internal static object AutoGeneratedGetMemberValueCulture(object o)
	{
		return ((Clan)o).Culture;
	}

	internal static object AutoGeneratedGetMemberValueLastFactionChangeTime(object o)
	{
		return ((Clan)o).LastFactionChangeTime;
	}

	internal static object AutoGeneratedGetMemberValueAutoRecruitmentExpenses(object o)
	{
		return ((Clan)o).AutoRecruitmentExpenses;
	}

	internal static object AutoGeneratedGetMemberValueEncyclopediaText(object o)
	{
		return ((Clan)o).EncyclopediaText;
	}

	internal static object AutoGeneratedGetMemberValueIsNoble(object o)
	{
		return ((Clan)o).IsNoble;
	}

	internal static object AutoGeneratedGetMemberValueMercenaryAwardMultiplier(object o)
	{
		return ((Clan)o).MercenaryAwardMultiplier;
	}

	internal static object AutoGeneratedGetMemberValueInitialHomeSettlement(object o)
	{
		return ((Clan)o).InitialHomeSettlement;
	}

	internal static object AutoGeneratedGetMemberValueIsRebelClan(object o)
	{
		return ((Clan)o).IsRebelClan;
	}

	internal static object AutoGeneratedGetMemberValueIsMinorFaction(object o)
	{
		return ((Clan)o).IsMinorFaction;
	}

	internal static object AutoGeneratedGetMemberValueIsOutlaw(object o)
	{
		return ((Clan)o).IsOutlaw;
	}

	internal static object AutoGeneratedGetMemberValueIsNomad(object o)
	{
		return ((Clan)o).IsNomad;
	}

	internal static object AutoGeneratedGetMemberValueIsMafia(object o)
	{
		return ((Clan)o).IsMafia;
	}

	internal static object AutoGeneratedGetMemberValueIsClanTypeMercenary(object o)
	{
		return ((Clan)o).IsClanTypeMercenary;
	}

	internal static object AutoGeneratedGetMemberValueIsSect(object o)
	{
		return ((Clan)o).IsSect;
	}

	internal static object AutoGeneratedGetMemberValueIsUnderMercenaryService(object o)
	{
		return ((Clan)o).IsUnderMercenaryService;
	}

	internal static object AutoGeneratedGetMemberValueShouldStayInKingdomUntil(object o)
	{
		return ((Clan)o).ShouldStayInKingdomUntil;
	}

	internal static object AutoGeneratedGetMemberValueColor(object o)
	{
		return ((Clan)o).Color;
	}

	internal static object AutoGeneratedGetMemberValueColor2(object o)
	{
		return ((Clan)o).Color2;
	}

	internal static object AutoGeneratedGetMemberValueBannerBackgroundColorPrimary(object o)
	{
		return ((Clan)o).BannerBackgroundColorPrimary;
	}

	internal static object AutoGeneratedGetMemberValueBannerBackgroundColorSecondary(object o)
	{
		return ((Clan)o).BannerBackgroundColorSecondary;
	}

	internal static object AutoGeneratedGetMemberValueBannerIconColor(object o)
	{
		return ((Clan)o).BannerIconColor;
	}

	internal static object AutoGeneratedGetMemberValueIsBanditFaction(object o)
	{
		return ((Clan)o).IsBanditFaction;
	}

	internal static object AutoGeneratedGetMemberValueRenown(object o)
	{
		return ((Clan)o).Renown;
	}

	internal static object AutoGeneratedGetMemberValueMainHeroCrimeRating(object o)
	{
		return ((Clan)o).MainHeroCrimeRating;
	}

	internal static object AutoGeneratedGetMemberValueNotAttackableByPlayerUntilTime(object o)
	{
		return ((Clan)o).NotAttackableByPlayerUntilTime;
	}

	internal static object AutoGeneratedGetMemberValue_defaultPartyTemplate(object o)
	{
		return ((Clan)o)._defaultPartyTemplate;
	}

	internal static object AutoGeneratedGetMemberValue_isEliminated(object o)
	{
		return ((Clan)o)._isEliminated;
	}

	internal static object AutoGeneratedGetMemberValue_minorFactionCharacterTemplates(object o)
	{
		return ((Clan)o)._minorFactionCharacterTemplates;
	}

	internal static object AutoGeneratedGetMemberValue_kingdom(object o)
	{
		return ((Clan)o)._kingdom;
	}

	internal static object AutoGeneratedGetMemberValue_influence(object o)
	{
		return ((Clan)o)._influence;
	}

	internal static object AutoGeneratedGetMemberValue_basicTroop(object o)
	{
		return ((Clan)o)._basicTroop;
	}

	internal static object AutoGeneratedGetMemberValue_leader(object o)
	{
		return ((Clan)o)._leader;
	}

	internal static object AutoGeneratedGetMemberValue_banner(object o)
	{
		return ((Clan)o)._banner;
	}

	internal static object AutoGeneratedGetMemberValue_tier(object o)
	{
		return ((Clan)o)._tier;
	}

	internal static object AutoGeneratedGetMemberValue_aggressiveness(object o)
	{
		return ((Clan)o)._aggressiveness;
	}

	internal static object AutoGeneratedGetMemberValue_tributeWallet(object o)
	{
		return ((Clan)o)._tributeWallet;
	}

	internal static object AutoGeneratedGetMemberValue_home(object o)
	{
		return ((Clan)o)._home;
	}

	internal static object AutoGeneratedGetMemberValue_clanDebtToKingdom(object o)
	{
		return ((Clan)o)._clanDebtToKingdom;
	}

	public void UpdateFactionsAtWarWith()
	{
		_factionsAtWarWith.Clear();
		foreach (Kingdom item in Kingdom.All)
		{
			if (!item.IsEliminated && IsAtWarWith(item))
			{
				_factionsAtWarWith.Add(item);
			}
		}
		foreach (Clan item2 in All)
		{
			if (!item2.IsEliminated && IsAtWarWith(item2))
			{
				_factionsAtWarWith.Add(item2);
			}
		}
	}

	public void UpdateCurrentStrength()
	{
		CurrentTotalStrength = 0f;
		foreach (WarPartyComponent item in _warPartyComponentsCache)
		{
			CurrentTotalStrength += item.MobileParty.Party.EstimatedStrength;
		}
		foreach (Town fief in Fiefs)
		{
			if (fief.GarrisonParty != null)
			{
				CurrentTotalStrength += fief.GarrisonParty.Party.EstimatedStrength;
			}
		}
	}

	public bool IsAtWarWith(IFaction other)
	{
		return FactionManager.IsAtWarAgainstFaction(this, other);
	}

	private void InitMembers()
	{
		_companionsCache = new MBList<Hero>();
		_factionsAtWarWith = new MBList<IFaction>();
		_warPartyComponentsCache = new MBList<WarPartyComponent>();
		_supporterNotablesCache = new MBList<Hero>();
		_aliveLordsCache = new MBList<Hero>();
		_deadLordsCache = new MBList<Hero>();
		_heroesCache = new MBList<Hero>();
		_villagesCache = new MBList<Village>();
		_fiefsCache = new MBList<Town>();
		_settlementsCache = new MBList<Settlement>();
	}

	public Clan()
	{
		InitMembers();
		_isEliminated = false;
		NotAttackableByPlayerUntilTime = CampaignTime.Zero;
	}

	public static Clan CreateClan(string stringID)
	{
		stringID = Campaign.Current.CampaignObjectManager.FindNextUniqueStringId<Clan>(stringID);
		Clan clan = new Clan();
		clan.StringId = stringID;
		Campaign.Current.CampaignObjectManager.AddClan(clan);
		return clan;
	}

	protected override void PreAfterLoad()
	{
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.3.0") && IsBanditFaction && IsEliminated)
		{
			_isEliminated = false;
		}
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.2") && base.StringId == "neutral")
		{
			foreach (Hero aliveHero in Campaign.Current.AliveHeroes)
			{
				if (aliveHero.Clan == this)
				{
					aliveHero.ResetClanForOldSave();
					if (_aliveLordsCache.Contains(aliveHero))
					{
						_aliveLordsCache.Remove(aliveHero);
					}
					if (_heroesCache.Contains(aliveHero))
					{
						_heroesCache.Remove(aliveHero);
					}
				}
			}
			foreach (Hero deadOrDisabledHero in Campaign.Current.DeadOrDisabledHeroes)
			{
				if (deadOrDisabledHero.Clan == this)
				{
					deadOrDisabledHero.ResetClanForOldSave();
					if (_aliveLordsCache.Contains(deadOrDisabledHero))
					{
						_aliveLordsCache.Remove(deadOrDisabledHero);
					}
					if (_heroesCache.Contains(deadOrDisabledHero))
					{
						_heroesCache.Remove(deadOrDisabledHero);
					}
				}
			}
			for (int num = Heroes.Count - 1; num >= 0; num--)
			{
				Hero hero = Heroes[num];
				hero.ResetClanForOldSave();
				if (_aliveLordsCache.Contains(hero))
				{
					_aliveLordsCache.Remove(hero);
				}
				if (_heroesCache.Contains(hero))
				{
					_heroesCache.Remove(hero);
				}
			}
			DestroyClanAction.Apply(this);
			Campaign.Current.CampaignObjectManager.RemoveClan(this);
		}
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("e1.8.0.0"))
		{
			IsNoble = Leader?.IsNobleForOldSaves ?? false;
		}
		_kingdom?.AddClanInternal(this);
		UpdateBannerColorsAccordingToKingdom();
	}

	protected override void AfterLoad()
	{
		UpdateCurrentStrength();
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("e1.8.0.0") && Kingdom != null)
		{
			FactionHelper.AdjustFactionStancesForClanJoiningKingdom(this, Kingdom);
		}
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.1.3") && Kingdom == null && IsUnderMercenaryService)
		{
			EndMercenaryServiceAction.EndByLeavingKingdom(this);
		}
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.0") && IsEliminated && Leader != null && Leader.IsAlive)
		{
			DestroyClanAction.Apply(this);
		}
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.3.0"))
		{
			Settlement settlement = HomeSettlement;
			if (settlement == null)
			{
				settlement = Campaign.Current.Settlements.Where((Settlement x) => x.Culture == Culture)?.GetRandomElementInefficiently();
			}
			if (settlement == null)
			{
				settlement = Campaign.Current.Settlements.First();
			}
			SetInitialHomeSettlement(settlement);
		}
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.3.11") && Kingdom == null && this == PlayerClan)
		{
			Kingdom kingdom = Kingdom.All.FirstOrDefault((Kingdom t) => t.RulingClan == PlayerClan);
			if (kingdom != null)
			{
				Clan clan = (from t in kingdom.Clans
					where t != PlayerClan
					orderby t.CurrentTotalStrength descending
					select t).FirstOrDefault();
				if (clan != null)
				{
					ChangeRulingClanAction.Apply(kingdom, clan);
				}
				else
				{
					DestroyKingdomAction.Apply(kingdom);
				}
			}
		}
		CalculateMidSettlement();
	}

	public override void Deserialize(MBObjectManager objectManager, XmlNode node)
	{
		base.Deserialize(objectManager, node);
		SetLeader(objectManager.ReadObjectReferenceFromXml("owner", typeof(Hero), node) as Hero);
		Kingdom = (Kingdom)objectManager.ReadObjectReferenceFromXml("super_faction", typeof(Kingdom), node);
		Tier = ((node.Attributes["tier"] == null) ? 1 : Convert.ToInt32(node.Attributes["tier"].Value));
		Renown = Campaign.Current.Models.ClanTierModel.CalculateInitialRenown(this);
		if (node.Attributes["initial_home_settlement"] != null)
		{
			Settlement initialHomeSettlement = (Settlement)objectManager.ReadObjectReferenceFromXml("initial_home_settlement", typeof(Settlement), node);
			SetInitialHomeSettlement(initialHomeSettlement);
		}
		ChangeClanName(new TextObject(node.Attributes["name"].Value), (node.Attributes["short_name"] != null) ? new TextObject(node.Attributes["short_name"].Value) : new TextObject(node.Attributes["name"].Value));
		Culture = (CultureObject)objectManager.ReadObjectReferenceFromXml("culture", typeof(CultureObject), node);
		Banner = null;
		XmlNode xmlNode = node.Attributes["is_noble"];
		if (xmlNode != null)
		{
			IsNoble = Convert.ToBoolean(xmlNode.InnerText);
		}
		Color = ((node.Attributes["color"] == null) ? 4291609515u : Convert.ToUInt32(node.Attributes["color"].Value, 16));
		Color2 = ((node.Attributes["color2"] == null) ? 4291609515u : Convert.ToUInt32(node.Attributes["color2"].Value, 16));
		IsBanditFaction = node.Attributes["is_bandit"] != null && Convert.ToBoolean(node.Attributes["is_bandit"].Value);
		IsMinorFaction = node.Attributes["is_minor_faction"] != null && Convert.ToBoolean(node.Attributes["is_minor_faction"].Value);
		IsOutlaw = node.Attributes["is_outlaw"] != null && Convert.ToBoolean(node.Attributes["is_outlaw"].Value);
		IsSect = node.Attributes["is_sect"] != null && Convert.ToBoolean(node.Attributes["is_sect"].Value);
		IsMafia = node.Attributes["is_mafia"] != null && Convert.ToBoolean(node.Attributes["is_mafia"].Value);
		IsClanTypeMercenary = node.Attributes["is_clan_type_mercenary"] != null && Convert.ToBoolean(node.Attributes["is_clan_type_mercenary"].Value);
		IsNomad = node.Attributes["is_nomad"] != null && Convert.ToBoolean(node.Attributes["is_nomad"].Value);
		_defaultPartyTemplate = (PartyTemplateObject)objectManager.ReadObjectReferenceFromXml("default_party_template", typeof(PartyTemplateObject), node);
		EncyclopediaText = ((node.Attributes["text"] != null) ? new TextObject(node.Attributes["text"].Value) : TextObject.GetEmpty());
		if (node.Attributes["banner_key"] != null)
		{
			_banner = new Banner();
			_banner.Deserialize(node.Attributes["banner_key"].Value);
		}
		else
		{
			_banner = Banner.CreateRandomClanBanner(base.StringId.GetDeterministicHashCode());
		}
		BannerBackgroundColorPrimary = _banner.GetPrimaryColor();
		BannerBackgroundColorSecondary = _banner.GetSecondaryColor();
		BannerIconColor = _banner.GetFirstIconColor();
		UpdateBannerColorsAccordingToKingdom();
		_minorFactionCharacterTemplates = new MBList<CharacterObject>();
		foreach (XmlNode childNode in node.ChildNodes)
		{
			if (childNode.Name == "minor_faction_character_templates")
			{
				foreach (XmlNode childNode2 in childNode.ChildNodes)
				{
					CharacterObject item = objectManager.ReadObjectReferenceFromXml("id", typeof(CharacterObject), childNode2) as CharacterObject;
					_minorFactionCharacterTemplates.Add(item);
				}
			}
			else if (childNode.Name == "relationship")
			{
				IFaction faction = ((childNode.Attributes["clan"] == null) ? ((IFaction)(Kingdom)objectManager.ReadObjectReferenceFromXml("kingdom", typeof(Kingdom), childNode)) : ((IFaction)(Clan)objectManager.ReadObjectReferenceFromXml("clan", typeof(Clan), childNode)));
				if (Convert.ToInt32(childNode.Attributes["value"].InnerText) < 0)
				{
					FactionManager.DeclareWar(this, faction);
				}
				else
				{
					FactionManager.SetNeutral(this, faction);
				}
			}
		}
	}

	protected override void OnBeforeLoad()
	{
	}

	[LoadInitializationCallback]
	private void OnLoad(MetaData metaData)
	{
		InitMembers();
	}

	public int GetRelationWithClan(Clan other)
	{
		if (Leader != null && other.Leader != null)
		{
			return Leader.GetRelation(other.Leader);
		}
		return 0;
	}

	public void SetLeader(Hero leader)
	{
		_leader = leader;
		if (leader != null)
		{
			leader.Clan = this;
		}
	}

	public void SetInitialHomeSettlement(Settlement initialHomeSettlement)
	{
		InitialHomeSettlement = initialHomeSettlement;
		ConsiderAndUpdateHomeSettlement();
	}

	public void ConsiderAndUpdateHomeSettlement()
	{
		Settlement settlement = Campaign.Current.Models.SettlementValueModel.FindMostSuitableHomeSettlement(this);
		if (settlement == HomeSettlement)
		{
			return;
		}
		HomeSettlement = settlement;
		foreach (Hero hero in Heroes)
		{
			hero.UpdateHomeSettlement();
		}
	}

	public override TextObject GetName()
	{
		return Name;
	}

	public void ChangeClanName(TextObject name, TextObject informalName)
	{
		Name = name;
		InformalName = informalName;
	}

	public override string ToString()
	{
		return string.Concat("(", base.Id, ") ", Name);
	}

	public StanceLink GetStanceWith(IFaction other)
	{
		return FactionManager.Instance.GetStanceLinkInternal(this, other);
	}

	private void SetKingdomInternal(Kingdom value)
	{
		if (Kingdom != null)
		{
			LeaveKingdomInternal();
		}
		_kingdom = value;
		if (Kingdom != null)
		{
			EnterKingdomInternal();
		}
		UpdateBannerColorsAccordingToKingdom();
		LastFactionChangeTime = CampaignTime.Now;
	}

	private void EnterKingdomInternal()
	{
		_kingdom.AddClanInternal(this);
		foreach (Hero hero in Heroes)
		{
			_kingdom.OnHeroAdded(hero);
		}
		foreach (Town fief in Fiefs)
		{
			_kingdom.OnFortificationAdded(fief);
		}
		foreach (WarPartyComponent warPartyComponent in WarPartyComponents)
		{
			_kingdom.OnWarPartyAdded(warPartyComponent);
		}
	}

	private void LeaveKingdomInternal()
	{
		ChangeClanInfluenceAction.Apply(this, 0f - Influence);
		_kingdom.RemoveClanInternal(this);
		foreach (Hero hero in Heroes)
		{
			_kingdom.OnHeroRemoved(hero);
		}
		foreach (Town fief in Fiefs)
		{
			_kingdom.OnFortificationRemoved(fief);
		}
		List<WarPartyComponent> list = WarPartyComponents.ToListQ();
		for (int num = list.Count() - 1; num >= 0; num--)
		{
			if (list[num].MobileParty.Army != null)
			{
				list[num].MobileParty.Army = null;
			}
			_kingdom.OnWarPartyRemoved(list[num]);
		}
	}

	public void ClanLeaveKingdom(bool giveBackFiefs = false)
	{
		ChangeClanInfluenceAction.Apply(this, 0f - Influence);
		if (Kingdom != null)
		{
			foreach (Settlement settlement in Campaign.Current.Settlements)
			{
				if (settlement.IsTown && settlement.OwnerClan == this)
				{
					SettlementHelper.TakeEnemyVillagersOutsideSettlements(settlement);
				}
			}
		}
		LastFactionChangeTime = CampaignTime.Now;
		Kingdom = null;
	}

	public float CalculateTotalSettlementBaseValue()
	{
		float num = 0f;
		foreach (Town fief in Fiefs)
		{
			num += Campaign.Current.Models.SettlementValueModel.CalculateSettlementBaseValue(fief.Owner.Settlement);
		}
		return num;
	}

	public void StartMercenaryService()
	{
		IsUnderMercenaryService = true;
	}

	public void ResetPlayerHomeAndFactionMidSettlement()
	{
		_home = null;
		_midSettlement = null;
		InitialHomeSettlement = null;
		Settlement initialHomeSettlement = Campaign.Current.Models.SettlementValueModel.FindMostSuitableHomeSettlement(this);
		SetInitialHomeSettlement(initialHomeSettlement);
		CalculateMidSettlement();
	}

	private int DistanceOfTwoValues(int x, int y)
	{
		int num = ((x < 50) ? x : (100 - x));
		int num2 = ((y < 50) ? y : (100 - y));
		return TaleWorlds.Library.MathF.Min(num + num2, x - y);
	}

	public static Clan FindFirst(Predicate<Clan> predicate)
	{
		return All.FirstOrDefault((Clan x) => predicate(x));
	}

	public void EndMercenaryService(bool isByLeavingKingdom)
	{
		IsUnderMercenaryService = false;
	}

	public static IEnumerable<Clan> FindAll(Predicate<Clan> predicate)
	{
		return All.Where((Clan x) => predicate(x));
	}

	public float CalculateTotalSettlementValueForFaction(Kingdom kingdom)
	{
		float num = 0f;
		foreach (Town fief in Fiefs)
		{
			num += Campaign.Current.Models.SettlementValueModel.CalculateSettlementValueForFaction(fief.Owner.Settlement, kingdom);
		}
		return num;
	}

	internal void OnFortificationAdded(Town settlement)
	{
		_fiefsCache.Add(settlement);
		_settlementsCache.Add(settlement.Settlement);
		foreach (Village boundVillage in settlement.Settlement.BoundVillages)
		{
			OnBoundVillageAddedInternal(boundVillage);
		}
		_distanceToClosestNonAllyFortificationCacheDirty = true;
		if (_kingdom != null)
		{
			_kingdom.OnFortificationAdded(settlement);
		}
		CalculateMidSettlement();
	}

	internal void OnFortificationRemoved(Town settlement)
	{
		_fiefsCache.Remove(settlement);
		_settlementsCache.Remove(settlement.Settlement);
		foreach (Village boundVillage in settlement.Settlement.BoundVillages)
		{
			_villagesCache.Remove(boundVillage);
			_settlementsCache.Remove(boundVillage.Settlement);
		}
		_distanceToClosestNonAllyFortificationCacheDirty = true;
		if (_kingdom != null)
		{
			_kingdom.OnFortificationRemoved(settlement);
		}
		CalculateMidSettlement();
	}

	internal void OnBoundVillageAdded(Village village)
	{
		OnBoundVillageAddedInternal(village);
		if (_kingdom != null)
		{
			_kingdom.OnBoundVillageAdded(village);
		}
	}

	private void OnBoundVillageAddedInternal(Village village)
	{
		_villagesCache.Add(village);
		_settlementsCache.Add(village.Settlement);
	}

	internal void OnLordAdded(Hero lord)
	{
		if (lord.IsDead)
		{
			_deadLordsCache.Add(lord);
		}
		else
		{
			_aliveLordsCache.Add(lord);
		}
		OnHeroAdded(lord);
	}

	public void OnHeroChangedState(Hero hero, Hero.CharacterStates oldState)
	{
		if (hero.IsDead && oldState != Hero.CharacterStates.Dead)
		{
			_aliveLordsCache.Remove(hero);
			_deadLordsCache.Add(hero);
			if (_kingdom != null)
			{
				_kingdom.OnHeroChangedState(hero, oldState);
			}
		}
	}

	internal void OnLordRemoved(Hero lord)
	{
		if (lord.IsDead)
		{
			_deadLordsCache.Remove(lord);
		}
		else
		{
			_aliveLordsCache.Remove(lord);
		}
		OnHeroRemoved(lord);
	}

	internal void OnCompanionAdded(Hero companion)
	{
		_companionsCache.Add(companion);
		OnHeroAdded(companion);
	}

	internal void OnCompanionRemoved(Hero companion)
	{
		_companionsCache.Remove(companion);
		OnHeroRemoved(companion);
	}

	private void OnHeroAdded(Hero hero)
	{
		_heroesCache.Add(hero);
		if (_kingdom != null)
		{
			_kingdom.OnHeroAdded(hero);
		}
	}

	private void OnHeroRemoved(Hero hero)
	{
		_heroesCache.Remove(hero);
		if (_kingdom != null)
		{
			_kingdom.OnHeroRemoved(hero);
		}
	}

	internal void OnWarPartyAdded(WarPartyComponent warPartyComponent)
	{
		_warPartyComponentsCache.Add(warPartyComponent);
		if (_kingdom != null)
		{
			_kingdom.OnWarPartyAdded(warPartyComponent);
		}
	}

	internal void OnWarPartyRemoved(WarPartyComponent warPartyComponent)
	{
		_warPartyComponentsCache.Remove(warPartyComponent);
		if (_kingdom != null)
		{
			_kingdom.OnWarPartyRemoved(warPartyComponent);
		}
	}

	internal void OnSupporterNotableAdded(Hero hero)
	{
		_supporterNotablesCache.Add(hero);
	}

	internal void OnSupporterNotableRemoved(Hero hero)
	{
		_supporterNotablesCache.Remove(hero);
	}

	public void AddRenown(float value, bool shouldNotify = true)
	{
		if (value > 0f)
		{
			Renown += value;
			int num = Campaign.Current.Models.ClanTierModel.CalculateTier(this);
			if (num > Tier)
			{
				Tier = num;
				CampaignEventDispatcher.Instance.OnClanTierChanged(this, shouldNotify);
			}
		}
	}

	public void ResetClanRenown()
	{
		Renown = 0f;
		Tier = Campaign.Current.Models.ClanTierModel.CalculateTier(this);
		CampaignEventDispatcher.Instance.OnClanTierChanged(this, shouldNotify: false);
	}

	public void OnSupportedByClan(Clan supporterClan)
	{
		DiplomacyModel diplomacyModel = Campaign.Current.Models.DiplomacyModel;
		int influenceCostOfSupportingClan = diplomacyModel.GetInfluenceCostOfSupportingClan();
		if (supporterClan.Influence >= (float)influenceCostOfSupportingClan)
		{
			int influenceValueOfSupportingClan = diplomacyModel.GetInfluenceValueOfSupportingClan();
			int relationValueOfSupportingClan = diplomacyModel.GetRelationValueOfSupportingClan();
			ChangeClanInfluenceAction.Apply(supporterClan, -influenceCostOfSupportingClan);
			ChangeClanInfluenceAction.Apply(this, influenceValueOfSupportingClan);
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(supporterClan.Leader, Leader, relationValueOfSupportingClan);
		}
	}

	public static Clan CreateSettlementRebelClan(Settlement settlement, Hero owner, int iconMeshId = -1)
	{
		TextObject textObject = new TextObject("{=2LIV2cy7}{SETTLEMENT}{.o} rebels");
		textObject.SetTextVariable("SETTLEMENT", settlement.Name);
		Clan clan = CreateClan(settlement.StringId + "_rebel_clan");
		clan.ChangeClanName(textObject, textObject);
		clan.Culture = settlement.Culture;
		clan.Banner = Banner.CreateOneColoredBannerWithOneIcon(settlement.MapFaction.Banner.GetFirstIconColor(), settlement.MapFaction.Banner.GetPrimaryColor(), iconMeshId);
		clan.SetInitialHomeSettlement(settlement);
		clan.SetLeader(owner);
		clan.Color = settlement.MapFaction.Color2;
		clan.Color2 = settlement.MapFaction.Color;
		clan.Tier = Campaign.Current.Models.ClanTierModel.RebelClanStartingTier;
		clan.BannerBackgroundColorPrimary = settlement.MapFaction.Banner.GetFirstIconColor();
		clan.BannerBackgroundColorSecondary = settlement.MapFaction.Banner.GetFirstIconColor();
		clan.BannerIconColor = settlement.MapFaction.Banner.GetPrimaryColor();
		clan._distanceToClosestNonAllyFortificationCacheDirty = true;
		clan.HomeSettlement = settlement;
		clan.IsRebelClan = true;
		clan.CalculateMidSettlement();
		CampaignEventDispatcher.Instance.OnClanCreated(clan, isCompanion: false);
		return clan;
	}

	public void CalculateMidSettlement()
	{
		if (Campaign.Current.MapSceneWrapper != null)
		{
			_midSettlement = FactionHelper.GetMidSettlementOfFaction(this);
		}
	}

	public static Clan CreateCompanionToLordClan(Hero hero, Settlement settlement, TextObject clanName, int newClanIconId)
	{
		Clan clan = CreateClan(Hero.MainHero.MapFaction.StringId + "_companion_clan");
		clan.ChangeClanName(clanName, clanName);
		clan.Culture = settlement.Culture;
		clan.Banner = Banner.CreateOneColoredBannerWithOneIcon(settlement.MapFaction.Banner.GetFirstIconColor(), settlement.MapFaction.Banner.GetPrimaryColor(), newClanIconId);
		clan.Kingdom = Hero.MainHero.Clan.Kingdom;
		clan.Tier = Campaign.Current.Models.ClanTierModel.CompanionToLordClanStartingTier;
		clan.SetInitialHomeSettlement(settlement);
		hero.Clan = clan;
		clan.SetLeader(hero);
		clan.IsNoble = true;
		ChangeOwnerOfSettlementAction.ApplyByGift(settlement, hero);
		CampaignEventDispatcher.Instance.OnClanCreated(clan, isCompanion: true);
		return clan;
	}

	public Dictionary<Hero, int> GetHeirApparents()
	{
		Dictionary<Hero, int> dictionary = new Dictionary<Hero, int>();
		int heroComesOfAge = Campaign.Current.Models.AgeModel.HeroComesOfAge;
		Hero maxSkillHero = Leader;
		foreach (Hero hero in Heroes)
		{
			if (hero != Leader && hero.IsAlive && hero.DeathMark == KillCharacterAction.KillCharacterActionDetail.None && !hero.IsNotSpawned && !hero.IsDisabled && !hero.IsWanderer && !hero.IsNotable && hero.Age >= (float)heroComesOfAge)
			{
				int value = Campaign.Current.Models.HeirSelectionCalculationModel.CalculateHeirSelectionPoint(hero, Leader, ref maxSkillHero);
				dictionary.Add(hero, value);
			}
		}
		if (maxSkillHero != Leader)
		{
			dictionary[maxSkillHero] += Campaign.Current.Models.HeirSelectionCalculationModel.HighestSkillPoint;
		}
		return dictionary;
	}

	private void UpdateBannerColorsAccordingToKingdom()
	{
		if (Kingdom != null)
		{
			Banner?.ChangePrimaryColor(Kingdom.PrimaryBannerColor);
			Banner?.ChangeIconColors(Kingdom.SecondaryBannerColor);
			if (Kingdom.RulingClan == this)
			{
				_banner?.ChangePrimaryColor(Kingdom.PrimaryBannerColor);
				_banner?.ChangeIconColors(Kingdom.SecondaryBannerColor);
			}
		}
		else if (BannerBackgroundColorPrimary != 0 || BannerBackgroundColorSecondary != 0 || BannerIconColor != 0)
		{
			Banner?.ChangeBackgroundColor(BannerBackgroundColorPrimary, BannerBackgroundColorSecondary);
			Banner?.ChangeIconColors(BannerIconColor);
		}
		else if (IsMinorFaction)
		{
			Banner?.ChangePrimaryColor(Color);
			Banner?.ChangeIconColors((Color != Color2) ? Color2 : uint.MaxValue);
		}
		foreach (WarPartyComponent warPartyComponent in WarPartyComponents)
		{
			warPartyComponent.Party.SetVisualAsDirty();
		}
	}

	public void UpdateBannerColor(uint backgroundColor, uint iconColor)
	{
		BannerBackgroundColorPrimary = backgroundColor;
		BannerBackgroundColorSecondary = backgroundColor;
		BannerIconColor = iconColor;
	}

	internal void DeactivateClan()
	{
		_isEliminated = true;
	}

	[SpecialName]
	string IFaction.get_StringId()
	{
		return base.StringId;
	}

	[SpecialName]
	MBGUID IFaction.get_Id()
	{
		return base.Id;
	}
}
