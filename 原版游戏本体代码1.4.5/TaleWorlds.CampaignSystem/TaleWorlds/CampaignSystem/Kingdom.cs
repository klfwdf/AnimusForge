using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Load;

namespace TaleWorlds.CampaignSystem;

public sealed class Kingdom : MBObjectBase, IFaction
{
	[SaveableField(10)]
	private MBList<KingdomDecision> _unresolvedDecisions = new MBList<KingdomDecision>();

	private MBList<IFaction> _factionsAtWarWith;

	private MBList<Kingdom> _alliedKingdoms;

	[CachedData]
	private MBList<Town> _fiefsCache;

	[CachedData]
	private MBList<Town> _townsCache;

	[CachedData]
	private MBList<Village> _villagesCache;

	[CachedData]
	private MBList<Settlement> _settlementsCache;

	[CachedData]
	private MBList<Hero> _heroesCache;

	[CachedData]
	private MBList<Hero> _aliveLordsCache;

	[CachedData]
	private MBList<Hero> _deadLordsCache;

	[CachedData]
	private MBList<WarPartyComponent> _warPartyComponentsCache;

	[CachedData]
	private MBList<Clan> _clans;

	[SaveableField(18)]
	private Clan _rulingClan;

	[SaveableField(20)]
	private MBList<Army> _armies;

	[CachedData]
	private Settlement _midSettlement;

	[CachedData]
	private float _distanceToClosestNonAllyFortificationCache;

	[CachedData]
	private bool _distanceToClosestNonAllyFortificationCacheDirty = true;

	[SaveableField(23)]
	public int PoliticalStagnation;

	[SaveableField(26)]
	private MBList<PolicyObject> _activePolicies;

	[SaveableField(29)]
	private bool _isEliminated;

	[SaveableField(60)]
	private float _aggressiveness;

	[SaveableField(80)]
	private int _tributeWallet;

	[SaveableField(81)]
	private int _kingdomBudgetWallet;

	[SaveableField(82)]
	private int _callToWarWallet;

	[SaveableProperty(1)]
	public TextObject Name { get; private set; }

	[SaveableProperty(2)]
	public TextObject InformalName { get; private set; }

	[SaveableProperty(3)]
	public TextObject EncyclopediaText { get; private set; }

	[SaveableProperty(4)]
	public TextObject EncyclopediaTitle { get; private set; }

	[SaveableProperty(5)]
	public TextObject EncyclopediaRulerTitle { get; private set; }

	public string EncyclopediaLink => Campaign.Current.EncyclopediaManager.GetIdentifier(typeof(Kingdom)) + "-" + base.StringId;

	public TextObject EncyclopediaLinkWithName => HyperlinkTexts.GetKingdomHyperlinkText(EncyclopediaLink, InformalName);

	public MBReadOnlyList<KingdomDecision> UnresolvedDecisions => _unresolvedDecisions;

	[SaveableProperty(6)]
	public CultureObject Culture { get; private set; }

	[SaveableProperty(17)]
	public Settlement InitialHomeSettlement { get; private set; }

	public bool IsMapFaction => true;

	public bool HasNavalNavigationCapability => Culture.DefaultPartyTemplate.ShipHulls.Any();

	[SaveableProperty(9)]
	public uint Color { get; private set; }

	[SaveableProperty(10)]
	public uint Color2 { get; private set; }

	[SaveableProperty(13)]
	public uint PrimaryBannerColor { get; private set; }

	[SaveableProperty(14)]
	public uint SecondaryBannerColor { get; private set; }

	[SaveableProperty(15)]
	public float MainHeroCrimeRating { get; set; }

	public MBReadOnlyList<IFaction> FactionsAtWarWith => _factionsAtWarWith;

	public MBReadOnlyList<Kingdom> AlliedKingdoms => _alliedKingdoms;

	public MBReadOnlyList<Town> Fiefs => _fiefsCache;

	public MBReadOnlyList<Town> Towns => _townsCache;

	public MBReadOnlyList<Village> Villages => _villagesCache;

	public MBReadOnlyList<Settlement> Settlements => _settlementsCache;

	public MBReadOnlyList<Hero> Heroes => _heroesCache;

	public MBReadOnlyList<Hero> AliveLords => _aliveLordsCache;

	public MBReadOnlyList<Hero> DeadLords => _deadLordsCache;

	public MBReadOnlyList<WarPartyComponent> WarPartyComponents => _warPartyComponentsCache;

	public float DailyCrimeRatingChange => Campaign.Current.Models.CrimeModel.GetDailyCrimeRatingChange(this).ResultNumber;

	public ExplainedNumber DailyCrimeRatingChangeExplained => Campaign.Current.Models.CrimeModel.GetDailyCrimeRatingChange(this, includeDescriptions: true);

	public CharacterObject BasicTroop => Culture.BasicTroop;

	public Hero Leader => _rulingClan?.Leader;

	[SaveableProperty(16)]
	public Banner Banner { get; set; }

	public bool IsBanditFaction
	{
		get
		{
			if (_rulingClan != null)
			{
				return _rulingClan.IsBanditFaction;
			}
			return false;
		}
	}

	public bool IsMinorFaction => false;

	bool IFaction.IsKingdomFaction => true;

	public bool IsRebelClan => false;

	public bool IsClan => false;

	public bool IsOutlaw
	{
		get
		{
			if (_rulingClan != null)
			{
				return _rulingClan.IsOutlaw;
			}
			return false;
		}
	}

	public MBReadOnlyList<Clan> Clans => _clans;

	public Clan RulingClan
	{
		get
		{
			return _rulingClan;
		}
		set
		{
			_rulingClan = value;
		}
	}

	public MBReadOnlyList<Army> Armies => _armies;

	public float CurrentTotalStrength
	{
		get
		{
			float num = 0f;
			int count = _clans.Count;
			for (int i = 0; i < count; i++)
			{
				num += _clans[i].CurrentTotalStrength;
			}
			return num;
		}
	}

	public Settlement FactionMidSettlement => _midSettlement;

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

	public IList<PolicyObject> ActivePolicies => _activePolicies;

	public static MBReadOnlyList<Kingdom> All => Campaign.Current.Kingdoms;

	[SaveableProperty(28)]
	public CampaignTime LastKingdomDecisionConclusionDate { get; private set; }

	public bool IsEliminated => _isEliminated;

	[SaveableProperty(41)]
	public CampaignTime LastMercenaryOfferTime { get; set; }

	public IFaction MapFaction => this;

	[SaveableProperty(50)]
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

	public IEnumerable<MobileParty> AllParties
	{
		get
		{
			foreach (MobileParty mobileParty in Campaign.Current.MobileParties)
			{
				if (mobileParty.MapFaction == this)
				{
					yield return mobileParty;
				}
			}
		}
	}

	[SaveableProperty(70)]
	public int MercenaryWallet { get; internal set; }

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

	public int KingdomBudgetWallet
	{
		get
		{
			return _kingdomBudgetWallet;
		}
		set
		{
			_kingdomBudgetWallet = value;
		}
	}

	public int CallToWarWallet
	{
		get
		{
			return _callToWarWallet;
		}
		set
		{
			_callToWarWallet = value;
		}
	}

	internal static void AutoGeneratedStaticCollectObjectsKingdom(object o, List<object> collectedObjects)
	{
		((Kingdom)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		collectedObjects.Add(_unresolvedDecisions);
		collectedObjects.Add(_rulingClan);
		collectedObjects.Add(_armies);
		collectedObjects.Add(_activePolicies);
		collectedObjects.Add(Name);
		collectedObjects.Add(InformalName);
		collectedObjects.Add(EncyclopediaText);
		collectedObjects.Add(EncyclopediaTitle);
		collectedObjects.Add(EncyclopediaRulerTitle);
		collectedObjects.Add(Culture);
		collectedObjects.Add(InitialHomeSettlement);
		collectedObjects.Add(Banner);
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(LastKingdomDecisionConclusionDate, collectedObjects);
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(LastMercenaryOfferTime, collectedObjects);
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(NotAttackableByPlayerUntilTime, collectedObjects);
	}

	internal static object AutoGeneratedGetMemberValueName(object o)
	{
		return ((Kingdom)o).Name;
	}

	internal static object AutoGeneratedGetMemberValueInformalName(object o)
	{
		return ((Kingdom)o).InformalName;
	}

	internal static object AutoGeneratedGetMemberValueEncyclopediaText(object o)
	{
		return ((Kingdom)o).EncyclopediaText;
	}

	internal static object AutoGeneratedGetMemberValueEncyclopediaTitle(object o)
	{
		return ((Kingdom)o).EncyclopediaTitle;
	}

	internal static object AutoGeneratedGetMemberValueEncyclopediaRulerTitle(object o)
	{
		return ((Kingdom)o).EncyclopediaRulerTitle;
	}

	internal static object AutoGeneratedGetMemberValueCulture(object o)
	{
		return ((Kingdom)o).Culture;
	}

	internal static object AutoGeneratedGetMemberValueInitialHomeSettlement(object o)
	{
		return ((Kingdom)o).InitialHomeSettlement;
	}

	internal static object AutoGeneratedGetMemberValueColor(object o)
	{
		return ((Kingdom)o).Color;
	}

	internal static object AutoGeneratedGetMemberValueColor2(object o)
	{
		return ((Kingdom)o).Color2;
	}

	internal static object AutoGeneratedGetMemberValuePrimaryBannerColor(object o)
	{
		return ((Kingdom)o).PrimaryBannerColor;
	}

	internal static object AutoGeneratedGetMemberValueSecondaryBannerColor(object o)
	{
		return ((Kingdom)o).SecondaryBannerColor;
	}

	internal static object AutoGeneratedGetMemberValueMainHeroCrimeRating(object o)
	{
		return ((Kingdom)o).MainHeroCrimeRating;
	}

	internal static object AutoGeneratedGetMemberValueBanner(object o)
	{
		return ((Kingdom)o).Banner;
	}

	internal static object AutoGeneratedGetMemberValueLastKingdomDecisionConclusionDate(object o)
	{
		return ((Kingdom)o).LastKingdomDecisionConclusionDate;
	}

	internal static object AutoGeneratedGetMemberValueLastMercenaryOfferTime(object o)
	{
		return ((Kingdom)o).LastMercenaryOfferTime;
	}

	internal static object AutoGeneratedGetMemberValueNotAttackableByPlayerUntilTime(object o)
	{
		return ((Kingdom)o).NotAttackableByPlayerUntilTime;
	}

	internal static object AutoGeneratedGetMemberValueMercenaryWallet(object o)
	{
		return ((Kingdom)o).MercenaryWallet;
	}

	internal static object AutoGeneratedGetMemberValuePoliticalStagnation(object o)
	{
		return ((Kingdom)o).PoliticalStagnation;
	}

	internal static object AutoGeneratedGetMemberValue_unresolvedDecisions(object o)
	{
		return ((Kingdom)o)._unresolvedDecisions;
	}

	internal static object AutoGeneratedGetMemberValue_rulingClan(object o)
	{
		return ((Kingdom)o)._rulingClan;
	}

	internal static object AutoGeneratedGetMemberValue_armies(object o)
	{
		return ((Kingdom)o)._armies;
	}

	internal static object AutoGeneratedGetMemberValue_activePolicies(object o)
	{
		return ((Kingdom)o)._activePolicies;
	}

	internal static object AutoGeneratedGetMemberValue_isEliminated(object o)
	{
		return ((Kingdom)o)._isEliminated;
	}

	internal static object AutoGeneratedGetMemberValue_aggressiveness(object o)
	{
		return ((Kingdom)o)._aggressiveness;
	}

	internal static object AutoGeneratedGetMemberValue_tributeWallet(object o)
	{
		return ((Kingdom)o)._tributeWallet;
	}

	internal static object AutoGeneratedGetMemberValue_kingdomBudgetWallet(object o)
	{
		return ((Kingdom)o)._kingdomBudgetWallet;
	}

	internal static object AutoGeneratedGetMemberValue_callToWarWallet(object o)
	{
		return ((Kingdom)o)._callToWarWallet;
	}

	public override TextObject GetName()
	{
		return Name;
	}

	public override string ToString()
	{
		return Name.ToString();
	}

	public void UpdateFactionsAtWarWith()
	{
		_factionsAtWarWith.Clear();
		foreach (Kingdom item in All)
		{
			if (!item.IsEliminated && IsAtWarWith(item))
			{
				_factionsAtWarWith.Add(item);
			}
		}
		foreach (Clan item2 in Clan.All)
		{
			if (!item2.IsEliminated && IsAtWarWith(item2))
			{
				_factionsAtWarWith.Add(item2);
			}
		}
	}

	public void UpdateAlliedKingdoms()
	{
		_alliedKingdoms.Clear();
		IAllianceCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>();
		foreach (Kingdom item in All)
		{
			if (!item.IsEliminated && campaignBehavior != null && campaignBehavior.IsAllyWithKingdom(this, item))
			{
				_alliedKingdoms.Add(item);
			}
		}
	}

	public Kingdom()
	{
		_activePolicies = new MBList<PolicyObject>();
		_armies = new MBList<Army>();
		InitializeCachedLists();
		EncyclopediaText = TextObject.GetEmpty();
		EncyclopediaTitle = TextObject.GetEmpty();
		EncyclopediaRulerTitle = TextObject.GetEmpty();
		float randomFloat = MBRandom.RandomFloat;
		float randomFloat2 = MBRandom.RandomFloat;
		PoliticalStagnation = 10 + (int)(randomFloat * randomFloat2 * 100f);
		_distanceToClosestNonAllyFortificationCacheDirty = true;
		_isEliminated = false;
		NotAttackableByPlayerUntilTime = CampaignTime.Zero;
	}

	public static Kingdom CreateKingdom(string stringID)
	{
		stringID = Campaign.Current.CampaignObjectManager.FindNextUniqueStringId<Kingdom>(stringID);
		Kingdom kingdom = new Kingdom();
		kingdom.StringId = stringID;
		Campaign.Current.CampaignObjectManager.AddKingdom(kingdom);
		return kingdom;
	}

	public void InitializeKingdom(TextObject name, TextObject informalName, CultureObject culture, Banner banner, uint kingdomColor1, uint kingdomColor2, Settlement initialHomeSettlement, TextObject encyclopediaText, TextObject encyclopediaTitle, TextObject encyclopediaRulerTitle)
	{
		ChangeKingdomName(name, informalName);
		Culture = culture;
		Banner = banner;
		Color = kingdomColor1;
		Color2 = kingdomColor2;
		PrimaryBannerColor = Color;
		SecondaryBannerColor = Color2;
		PoliticalStagnation = 100;
		EncyclopediaText = encyclopediaText;
		EncyclopediaTitle = encyclopediaTitle;
		EncyclopediaRulerTitle = encyclopediaRulerTitle;
		InitialHomeSettlement = initialHomeSettlement;
		foreach (PolicyObject defaultPolicy in Culture.DefaultPolicyList)
		{
			AddPolicy(defaultPolicy);
		}
	}

	private void InitializeCachedLists()
	{
		_clans = new MBList<Clan>();
		_factionsAtWarWith = new MBList<IFaction>();
		_fiefsCache = new MBList<Town>();
		_townsCache = new MBList<Town>();
		_villagesCache = new MBList<Village>();
		_settlementsCache = new MBList<Settlement>();
		_heroesCache = new MBList<Hero>();
		_aliveLordsCache = new MBList<Hero>();
		_deadLordsCache = new MBList<Hero>();
		_warPartyComponentsCache = new MBList<WarPartyComponent>();
		_alliedKingdoms = new MBList<Kingdom>();
	}

	public void ChangeKingdomName(TextObject name, TextObject informalName)
	{
		Name = name;
		InformalName = informalName;
	}

	public void OnHeroChangedState(Hero hero, Hero.CharacterStates oldState)
	{
		if (hero.IsDead && oldState != Hero.CharacterStates.Dead)
		{
			_aliveLordsCache.Remove(hero);
			_deadLordsCache.Add(hero);
		}
	}

	[LoadInitializationCallback]
	private void OnLoad(MetaData metaData, ObjectLoadData objectLoadData)
	{
		InitializeCachedLists();
	}

	protected override void AfterLoad()
	{
		for (int num = _activePolicies.Count - 1; num >= 0; num--)
		{
			if (_activePolicies[num] == null || !_activePolicies[num].IsReady)
			{
				_activePolicies.RemoveAt(num);
			}
		}
		for (int num2 = Clans.Count - 1; num2 >= 0; num2--)
		{
			Clan clan = Clans[num2];
			if (Campaign.Current.Models.DiplomacyModel.IsAtConstantWar(clan, this))
			{
				foreach (WarPartyComponent item in clan.WarPartyComponents.ToList())
				{
					if (item.MobileParty.MapEvent != null && (item.MobileParty.Army == null || item.MobileParty.Army.LeaderParty == item.MobileParty))
					{
						bool isPlayerMapEvent = item.MobileParty.MapEvent.IsPlayerMapEvent;
						item.MobileParty.MapEventSide = null;
						if (isPlayerMapEvent && MobileParty.MainParty.MapEvent == null)
						{
							PlayerEncounter.Finish();
						}
					}
				}
			}
		}
		CalculateMidSettlement();
	}

	public bool IsAllyWith(Kingdom other)
	{
		return AlliedKingdoms.ContainsQ(other);
	}

	public bool IsAtWarWith(IFaction other)
	{
		return FactionManager.IsAtWarAgainstFaction(this, other);
	}

	public bool IsAtConstantWarWith(IFaction other)
	{
		return FactionManager.IsAtConstantWarAgainstFaction(this, other);
	}

	public StanceLink GetStanceWith(IFaction other)
	{
		return FactionManager.Instance.GetStanceLinkInternal(this, other);
	}

	internal void AddArmyInternal(Army army)
	{
		_armies.Add(army);
	}

	internal void RemoveArmyInternal(Army army)
	{
		_armies.Remove(army);
	}

	public void CreateArmy(Hero armyLeader, Settlement targetSettlement, Army.ArmyTypes selectedArmyType, MBReadOnlyList<MobileParty> partiesToCallToArmy = null)
	{
		if (!armyLeader.IsActive)
		{
			Debug.Print($"Failed to create army, leader - {armyLeader?.StringId}: {armyLeader?.Name} is inactive");
			return;
		}
		if (armyLeader?.PartyBelongedTo.LeaderHero != null)
		{
			Army army = new Army(this, armyLeader.PartyBelongedTo, selectedArmyType);
			army.Gather(targetSettlement, partiesToCallToArmy);
			CampaignEventDispatcher.Instance.OnArmyCreated(army);
		}
		if (armyLeader == Hero.MainHero)
		{
			(Game.Current.GameStateManager.GameStates.Single((TaleWorlds.Core.GameState S) => S is MapState) as MapState)?.OnArmyCreated(MobileParty.MainParty);
		}
	}

	public void AddDecision(KingdomDecision kingdomDecision, bool ignoreInfluenceCost = false)
	{
		if (!ignoreInfluenceCost)
		{
			Clan proposerClan = kingdomDecision.ProposerClan;
			int influenceCost = kingdomDecision.GetInfluenceCost(proposerClan);
			ChangeClanInfluenceAction.Apply(proposerClan, -influenceCost);
		}
		bool isPlayerInvolved = kingdomDecision.DetermineChooser().Leader.IsHumanPlayerCharacter || kingdomDecision.DetermineSupporters().Any((Supporter x) => x.IsPlayer);
		CampaignEventDispatcher.Instance.OnKingdomDecisionAdded(kingdomDecision, isPlayerInvolved);
		if (kingdomDecision.Kingdom != Clan.PlayerClan.Kingdom)
		{
			new KingdomElection(kingdomDecision).StartElection();
		}
		else
		{
			_unresolvedDecisions.Add(kingdomDecision);
		}
	}

	public void RemoveDecision(KingdomDecision kingdomDecision)
	{
		_unresolvedDecisions.Remove(kingdomDecision);
	}

	public void OnKingdomDecisionConcluded()
	{
		LastKingdomDecisionConclusionDate = CampaignTime.Now;
	}

	public void AddPolicy(PolicyObject policy)
	{
		if (!_activePolicies.Contains(policy))
		{
			_activePolicies.Add(policy);
		}
	}

	public void RemovePolicy(PolicyObject policy)
	{
		if (_activePolicies.Contains(policy))
		{
			_activePolicies.Remove(policy);
		}
	}

	public bool HasPolicy(PolicyObject policy)
	{
		for (int i = 0; i < _activePolicies.Count; i++)
		{
			if (_activePolicies[i] == policy)
			{
				return true;
			}
		}
		return false;
	}

	public override void Deserialize(MBObjectManager objectManager, XmlNode node)
	{
		base.Deserialize(objectManager, node);
		EncyclopediaText = ((node.Attributes["text"] != null) ? new TextObject(node.Attributes["text"].Value) : TextObject.GetEmpty());
		EncyclopediaTitle = ((node.Attributes["title"] != null) ? new TextObject(node.Attributes["title"].Value) : TextObject.GetEmpty());
		EncyclopediaRulerTitle = ((node.Attributes["ruler_title"] != null) ? new TextObject(node.Attributes["ruler_title"].Value) : TextObject.GetEmpty());
		Settlement initialHomeSettlement = null;
		if (node.Attributes["initial_home_settlement"] != null)
		{
			initialHomeSettlement = (Settlement)objectManager.ReadObjectReferenceFromXml("initial_home_settlement", typeof(Settlement), node);
		}
		InitializeKingdom(new TextObject(node.Attributes["name"].Value), (node.Attributes["short_name"] != null) ? new TextObject(node.Attributes["short_name"].Value) : new TextObject(node.Attributes["name"].Value), (CultureObject)objectManager.ReadObjectReferenceFromXml("culture", typeof(CultureObject), node), null, (node.Attributes["color"] != null) ? Convert.ToUInt32(node.Attributes["color"].Value, 16) : 0u, (node.Attributes["color2"] != null) ? Convert.ToUInt32(node.Attributes["color2"].Value, 16) : 0u, initialHomeSettlement, EncyclopediaText, EncyclopediaTitle, EncyclopediaRulerTitle);
		RulingClan = (objectManager.ReadObjectReferenceFromXml("owner", typeof(Hero), node) as Hero)?.Clan;
		PrimaryBannerColor = ((node.Attributes["primary_banner_color"] != null) ? Convert.ToUInt32(node.Attributes["primary_banner_color"].Value, 16) : 0u);
		SecondaryBannerColor = ((node.Attributes["secondary_banner_color"] != null) ? Convert.ToUInt32(node.Attributes["secondary_banner_color"].Value, 16) : 0u);
		if (node.Attributes["banner_key"] != null)
		{
			Banner = new Banner();
			Banner.Deserialize(node.Attributes["banner_key"].Value);
		}
		else
		{
			Banner = Banner.CreateRandomClanBanner(base.StringId.GetDeterministicHashCode());
		}
		foreach (XmlNode childNode in node.ChildNodes)
		{
			if (childNode.Name == "relationships")
			{
				foreach (XmlNode childNode2 in childNode.ChildNodes)
				{
					IFaction faction = ((childNode2.Attributes["clan"] == null) ? ((IFaction)(Kingdom)objectManager.ReadObjectReferenceFromXml("kingdom", typeof(Kingdom), childNode2)) : ((IFaction)(Clan)objectManager.ReadObjectReferenceFromXml("clan", typeof(Clan), childNode2)));
					if (Convert.ToInt32(childNode2.Attributes["value"].InnerText) < 0)
					{
						FactionManager.DeclareWar(this, faction);
					}
					else
					{
						FactionManager.SetNeutral(this, faction);
					}
					if (childNode2.Attributes["isAtWar"] != null && Convert.ToBoolean(childNode2.Attributes["isAtWar"].Value))
					{
						FactionManager.DeclareWar(this, faction);
					}
				}
			}
			else
			{
				if (!(childNode.Name == "policies"))
				{
					continue;
				}
				foreach (XmlNode childNode3 in childNode.ChildNodes)
				{
					PolicyObject policyObject = Game.Current.ObjectManager.GetObject<PolicyObject>(childNode3.Attributes["id"].Value);
					if (policyObject != null)
					{
						AddPolicy(policyObject);
					}
				}
			}
		}
	}

	internal void AddClanInternal(Clan clan)
	{
		_clans.Add(clan);
		CalculateMidSettlement();
		_distanceToClosestNonAllyFortificationCacheDirty = true;
	}

	internal void RemoveClanInternal(Clan clan)
	{
		_clans.Remove(clan);
		CalculateMidSettlement();
		_distanceToClosestNonAllyFortificationCacheDirty = true;
	}

	public void OnFortificationAdded(Town fortification)
	{
		_distanceToClosestNonAllyFortificationCacheDirty = true;
		_fiefsCache.Add(fortification);
		_settlementsCache.Add(fortification.Settlement);
		if (fortification.IsTown)
		{
			_townsCache.Add(fortification);
		}
		foreach (Village boundVillage in fortification.Settlement.BoundVillages)
		{
			OnBoundVillageAdded(boundVillage);
		}
		CalculateMidSettlement();
	}

	public void OnFortificationRemoved(Town fortification)
	{
		_distanceToClosestNonAllyFortificationCacheDirty = true;
		_fiefsCache.Remove(fortification);
		_settlementsCache.Remove(fortification.Settlement);
		if (fortification.IsTown)
		{
			_townsCache.Remove(fortification);
		}
		foreach (Village boundVillage in fortification.Settlement.BoundVillages)
		{
			OnBoundVillageRemoved(boundVillage);
		}
		CalculateMidSettlement();
	}

	private void OnBoundVillageRemoved(Village village)
	{
		_villagesCache.Remove(village);
		_settlementsCache.Remove(village.Settlement);
	}

	internal void OnBoundVillageAdded(Village village)
	{
		_villagesCache.Add(village);
		_settlementsCache.Add(village.Settlement);
	}

	public void OnHeroAdded(Hero hero)
	{
		_heroesCache.Add(hero);
		if (hero.Occupation == Occupation.Lord)
		{
			OnLordAdded(hero);
		}
	}

	public void OnHeroRemoved(Hero hero)
	{
		_heroesCache.Remove(hero);
		if (hero.Occupation == Occupation.Lord)
		{
			OnLordRemoved(hero);
		}
	}

	private void OnLordAdded(Hero hero)
	{
		if (hero.IsDead)
		{
			_deadLordsCache.Add(hero);
		}
		else
		{
			_aliveLordsCache.Add(hero);
		}
	}

	private void OnLordRemoved(Hero hero)
	{
		if (hero.IsDead)
		{
			_deadLordsCache.Remove(hero);
		}
		else
		{
			_aliveLordsCache.Remove(hero);
		}
	}

	public void OnWarPartyAdded(WarPartyComponent warPartyComponent)
	{
		_warPartyComponentsCache.Add(warPartyComponent);
	}

	public void OnWarPartyRemoved(WarPartyComponent warPartyComponent)
	{
		_warPartyComponentsCache.Remove(warPartyComponent);
	}

	public void CalculateMidSettlement()
	{
		if (Campaign.Current.MapSceneWrapper != null)
		{
			_midSettlement = FactionHelper.GetMidSettlementOfFaction(this);
		}
	}

	public void ReactivateKingdom()
	{
		_isEliminated = false;
	}

	internal void DeactivateKingdom()
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
