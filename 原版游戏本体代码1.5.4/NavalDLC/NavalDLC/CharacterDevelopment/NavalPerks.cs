using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;

namespace NavalDLC.CharacterDevelopment;

public class NavalPerks
{
	public class Mariner
	{
		public static PerkObject RollingThunder => Instance._rollingThunder;

		public static PerkObject PiratesProwess => Instance._piratesProwess;

		public static PerkObject Forceful => Instance._forceful;

		public static PerkObject BruteForce => Instance._bruteForce;

		public static PerkObject AxeOfTheNorthwind => Instance._axeOfTheNorthwind;

		public static PerkObject SunnyDisposition => Instance._sunnyDisposition;

		public static PerkObject EnemyOfTheWood => Instance._enemyOfTheWood;

		public static PerkObject NavalFightingTraining => Instance._navalFightingTraining;

		public static PerkObject TerrorOfTheSeas => Instance._terrorOfTheSeas;

		public static PerkObject RallyingCry => Instance._rallyingCry;

		public static PerkObject ShatteringBlow => Instance._shatteringBlow;

		public static PerkObject ShatteringVolley => Instance._shatteringVolley;

		public static PerkObject Arr => Instance._arr;

		public static PerkObject PirateHunter => Instance._pirateHunter;

		public static PerkObject BoardingMaster => Instance._boardingMaster;

		public static PerkObject HomeTurfAdvantage => Instance._homeTurfAdvantage;

		public static PerkObject MightyBlows => Instance._mightyBlows;

		public static PerkObject CrewOfSpears => Instance._crewOfSpears;

		public static PerkObject TheSkysFury => Instance._theSkysFury;

		public static PerkObject WarriorsMight => Instance._warriorsMight;
	}

	public class Boatswain
	{
		public static PerkObject MerchantPrince => Instance._merchantPrince;

		public static PerkObject MasterShipwright => Instance._masterShipwright;

		public static PerkObject StreamlinedOperations => Instance._streamlinedOperations;

		public static PerkObject WellStocked => Instance._wellStocked;

		public static PerkObject NavalHorde => Instance._navalHorde;

		public static PerkObject Optimization => Instance._optimization;

		public static PerkObject GildedPurse => Instance._gildedPurse;

		public static PerkObject VeteransWisdom => Instance._veteransWisdom;

		public static PerkObject ShipwrightsInsight => Instance._shipwrightsInsight;

		public static PerkObject SpecialArrows => Instance._specialArrows;

		public static PerkObject SmoothOperator => Instance._smoothOperator;

		public static PerkObject AccuracyTraining => Instance._accuracyTraining;

		public static PerkObject EfficientCaptain => Instance._efficientCaptain;

		public static PerkObject PopularCaptain => Instance._popularCaptain;

		public static PerkObject PortAuthority => Instance._portAuthority;

		public static PerkObject BlessingsOfTheSea => Instance._blessingsOfTheSea;

		public static PerkObject ShipwrightsHand => Instance._shipwrightsHand;

		public static PerkObject Salvage => Instance._salvage;

		public static PerkObject MerchantFleet => Instance._merchantFleet;

		public static PerkObject Resilience => Instance._resilience;

		public static PerkObject NavalBombardment => Instance._navalBombardment;
	}

	public class Shipmaster
	{
		public static PerkObject MasterAngler => Instance._masterAngler;

		public static PerkObject OldSaltsTouch => Instance._oldSaltsTouch;

		public static PerkObject GhostShip => Instance._ghostShip;

		public static PerkObject WindRider => Instance._windRider;

		public static PerkObject RiverRaider => Instance._riverRaider;

		public static PerkObject NightRaider => Instance._nightRaider;

		public static PerkObject Windborne => Instance._windborne;

		public static PerkObject ShockAndAwe => Instance._shockAndAwe;

		public static PerkObject TheHelmsmansShield => Instance._theHelmsmansShield;

		public static PerkObject RavenEye => Instance._ravenEye;

		public static PerkObject FairWinds => Instance._fairWinds;

		public static PerkObject FavorableTide => Instance._favorableTide;

		public static PerkObject Unflinching => Instance._unflinching;

		public static PerkObject ShoreMaster => Instance._shoreMaster;

		public static PerkObject FleetCommander => Instance._fleetCommander;

		public static PerkObject ChainToOars => Instance._chainToOars;

		public static PerkObject Stormrider => Instance._stormrider;

		public static PerkObject MasterAndCommander => Instance._masterAndCommander;

		public static PerkObject TheCorsairsEdge => Instance._theCorsairsEdge;

		public static PerkObject SeaborneFortress => Instance._seaborneFortress;

		public static PerkObject Commodore => Instance._commodore;
	}

	private static readonly int[] TierSkillRequirements = new int[12]
	{
		25, 50, 75, 100, 125, 150, 175, 200, 225, 250,
		275, 300
	};

	private PerkObject _rollingThunder;

	private PerkObject _piratesProwess;

	private PerkObject _forceful;

	private PerkObject _bruteForce;

	private PerkObject _axeOfTheNorthwind;

	private PerkObject _sunnyDisposition;

	private PerkObject _enemyOfTheWood;

	private PerkObject _navalFightingTraining;

	private PerkObject _terrorOfTheSeas;

	private PerkObject _rallyingCry;

	private PerkObject _shatteringBlow;

	private PerkObject _shatteringVolley;

	private PerkObject _arr;

	private PerkObject _pirateHunter;

	private PerkObject _boardingMaster;

	private PerkObject _homeTurfAdvantage;

	private PerkObject _mightyBlows;

	private PerkObject _crewOfSpears;

	private PerkObject _theSkysFury;

	private PerkObject _warriorsMight;

	private PerkObject _merchantPrince;

	private PerkObject _masterShipwright;

	private PerkObject _streamlinedOperations;

	private PerkObject _wellStocked;

	private PerkObject _navalHorde;

	private PerkObject _optimization;

	private PerkObject _gildedPurse;

	private PerkObject _veteransWisdom;

	private PerkObject _shipwrightsInsight;

	private PerkObject _specialArrows;

	private PerkObject _smoothOperator;

	private PerkObject _accuracyTraining;

	private PerkObject _efficientCaptain;

	private PerkObject _popularCaptain;

	private PerkObject _portAuthority;

	private PerkObject _blessingsOfTheSea;

	private PerkObject _shipwrightsHand;

	private PerkObject _salvage;

	private PerkObject _merchantFleet;

	private PerkObject _resilience;

	private PerkObject _navalBombardment;

	private PerkObject _masterAngler;

	private PerkObject _oldSaltsTouch;

	private PerkObject _ghostShip;

	private PerkObject _windRider;

	private PerkObject _riverRaider;

	private PerkObject _nightRaider;

	private PerkObject _windborne;

	private PerkObject _shockAndAwe;

	private PerkObject _theHelmsmansShield;

	private PerkObject _ravenEye;

	private PerkObject _fairWinds;

	private PerkObject _favorableTide;

	private PerkObject _unflinching;

	private PerkObject _shoreMaster;

	private PerkObject _fleetCommander;

	private PerkObject _chainToOars;

	private PerkObject _stormrider;

	private PerkObject _masterAndCommander;

	private PerkObject _theCorsairsEdge;

	private PerkObject _seaborneFortress;

	private PerkObject _commodore;

	private static NavalPerks Instance => NavalDLCManager.Instance.NavalPerks;

	public NavalPerks()
	{
		RegisterAll();
		InitializeAll();
	}

	private void RegisterAll()
	{
		_rollingThunder = Create("RollingThunder");
		_piratesProwess = Create("PiratesProwess");
		_forceful = Create("Forceful");
		_bruteForce = Create("BruteForce");
		_axeOfTheNorthwind = Create("AxeOfTheNorthwind");
		_sunnyDisposition = Create("SunnyDisposition");
		_enemyOfTheWood = Create("EnemyOfTheWood");
		_navalFightingTraining = Create("NavalFightingTraining");
		_terrorOfTheSeas = Create("TerrorOfTheSeas");
		_rallyingCry = Create("RallyingCry");
		_shatteringBlow = Create("ShatteringBlow");
		_shatteringVolley = Create("ShatteringVolley");
		_arr = Create("Arr");
		_pirateHunter = Create("PirateHunter");
		_boardingMaster = Create("BoardingMaster");
		_homeTurfAdvantage = Create("HomeTurfAdvantage");
		_mightyBlows = Create("MightyBlows");
		_crewOfSpears = Create("CrewOfSpears");
		_theSkysFury = Create("TheSkysFury");
		_warriorsMight = Create("WarriorsMight");
		_merchantPrince = Create("MerchantPrince");
		_masterShipwright = Create("MasterShipwright");
		_streamlinedOperations = Create("StreamlinedOperations");
		_wellStocked = Create("WellStocked");
		_navalHorde = Create("NavalHorde");
		_optimization = Create("Optimization");
		_gildedPurse = Create("GildedPurse");
		_veteransWisdom = Create("VeteransWisdom");
		_shipwrightsInsight = Create("ShipwrightsInsight");
		_specialArrows = Create("SpecialArrows");
		_smoothOperator = Create("SmoothOperator");
		_accuracyTraining = Create("Accuracytraining");
		_efficientCaptain = Create("EfficientCaptain");
		_popularCaptain = Create("PopularCaptain");
		_portAuthority = Create("PortAuthority");
		_blessingsOfTheSea = Create("BlessingsOfTheSea");
		_shipwrightsHand = Create("ShipwrightsHand");
		_salvage = Create("Salvage");
		_merchantFleet = Create("MerchantFleet");
		_resilience = Create("Resilience");
		_navalBombardment = Create("NavalBombardment");
		_masterAngler = Create("MasterAngler");
		_oldSaltsTouch = Create("OldSaltsTouch");
		_ghostShip = Create("GhostShip");
		_windRider = Create("WindRider");
		_riverRaider = Create("RiverRaider");
		_nightRaider = Create("NightRaider");
		_windborne = Create("Windborne");
		_shockAndAwe = Create("ShockAndAwe");
		_theHelmsmansShield = Create("TheHelmsmansShield");
		_ravenEye = Create("RavenEye");
		_fairWinds = Create("FairWinds");
		_favorableTide = Create("FavorableTide");
		_unflinching = Create("Unflinching");
		_shoreMaster = Create("ShoreMaster");
		_fleetCommander = Create("FleetCommander");
		_chainToOars = Create("ChainToOars");
		_stormrider = Create("Stormrider");
		_masterAndCommander = Create("MasterAndCommander");
		_theCorsairsEdge = Create("TheCorsairsEdge");
		_seaborneFortress = Create("SeaborneFortress");
		_commodore = Create("Commodore");
	}

	private void InitializeAll()
	{
		_rollingThunder.Initialize("{=AtNKfDDP}Rolling Thunder", NavalSkills.Mariner, GetTierCost(1), _piratesProwess, "{=aYaJGhh9}{VALUE}% accuracy penalty from ship roll.", PartyRole.Personal, -0.3f, EffectIncrementType.AddFactor, "{=mbcN7l2f}{VALUE}% conformity gain for pirate prisoners in your party while at sea.", PartyRole.PartyLeader, 0.5f, EffectIncrementType.AddFactor, TroopUsageFlags.Ranged, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_piratesProwess.Initialize("{=csEbtEj5}Pirate's Prowess", NavalSkills.Mariner, GetTierCost(1), _rollingThunder, "{=7MtEpwKM}{VALUE}% melee weapon handling.", PartyRole.Personal, 0.25f, EffectIncrementType.AddFactor, "{=bNZPW3qe}{VALUE}% loot from defeated merchant convoys.", PartyRole.PartyLeader, 0.3f, EffectIncrementType.AddFactor, TroopUsageFlags.Melee, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_forceful.Initialize("{=DeWp2GjP}Shield Breaker", NavalSkills.Mariner, GetTierCost(2), _bruteForce, "{=yfpyCOuu}{VALUE}% damage to shields dealt by crew.", PartyRole.Captain, 0.3f, EffectIncrementType.AddFactor, "{=S4GbRzTr}{VALUE}% naval raid speed.", PartyRole.PartyLeader, 0.25f, EffectIncrementType.Add, TroopUsageFlags.Any, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_bruteForce.Initialize("{=DLcRb2jH}Brute Force", NavalSkills.Mariner, GetTierCost(2), _forceful, "{=Jbc2m29I}{VALUE}% kicking and bashing damage.", PartyRole.Personal, 0.5f, EffectIncrementType.AddFactor, "{=YzZr0gtE}{VALUE}% more loot from naval raids.", PartyRole.PartyLeader, 0.2f, EffectIncrementType.AddFactor, TroopUsageFlags.Melee, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_axeOfTheNorthwind.Initialize("{=mhzGQYl9}Axe of the North Wind", NavalSkills.Mariner, GetTierCost(3), _sunnyDisposition, "{=SlnKlVOl}{VALUE}% damage dealt by axes.", PartyRole.Personal, 0.2f, EffectIncrementType.AddFactor, "{=8STt46ci}{VALUE} morale for mariner troops at start of battle.", PartyRole.PartyLeader, 20f, EffectIncrementType.Add, TroopUsageFlags.Melee, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_sunnyDisposition.Initialize("{=MKOmGiqt}Sunny Disposition", NavalSkills.Mariner, GetTierCost(3), _axeOfTheNorthwind, "{=VB9rTE73}{VALUE}% damage dealt by swords.", PartyRole.Personal, 0.2f, EffectIncrementType.AddFactor, "{=BYY8MiFe}{VALUE} morale for regular troops at start of battle.", PartyRole.PartyLeader, 20f, EffectIncrementType.Add, TroopUsageFlags.Melee, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_enemyOfTheWood.Initialize("{=Tf7zOvfL}Enemy of the Wood", NavalSkills.Mariner, GetTierCost(4), _navalFightingTraining, "{=McqgoySh}{VALUE} morale to enemy for each ship destroyed in battle.", PartyRole.ArmyCommander, -10f, EffectIncrementType.Add, "{=6YsSNwTW}{VALUE}% fire damage dealt by your ship and crew to enemy sails.", PartyRole.Captain, 0.25f, EffectIncrementType.AddFactor, TroopUsageFlags.Any, TroopUsageFlags.Any, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_navalFightingTraining.Initialize("{=cvOhFtKn}Naval Fighting Training", NavalSkills.Mariner, GetTierCost(4), _enemyOfTheWood, "{=pRTSU12h}{VALUE}% to xp gained by party companions after each naval battle.", PartyRole.PartyLeader, 0.1f, EffectIncrementType.AddFactor, "{=F8cJJlZn}{VALUE}% Increase to militia veterancy in coastal settlements.", PartyRole.Governor, 0.1f, EffectIncrementType.AddFactor, TroopUsageFlags.Undefined, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly);
		_terrorOfTheSeas.Initialize("{=nUUAag3J}Terror of the Seas", NavalSkills.Mariner, GetTierCost(5), _rallyingCry, "{=VAFEpyau}{VALUE}% to morale loss suffered by enemy ships in battle.", PartyRole.PartyLeader, 0.2f, EffectIncrementType.AddFactor, "{=hMFAmWkE}{VALUE}% melee damage taken by crew while on enemy ships.", PartyRole.Captain, -0.1f, EffectIncrementType.AddFactor, TroopUsageFlags.Any, TroopUsageFlags.Any, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_rallyingCry.Initialize("{=5S1QiUvh}Rallying Cry", NavalSkills.Mariner, GetTierCost(5), _terrorOfTheSeas, "{=GTY1d7RX}{VALUE}% morale boost for crew while on own ship.", PartyRole.Captain, 0.2f, EffectIncrementType.AddFactor, "{=GX5M0gbo}{VALUE}% melee damage taken by crew while on own ships.", PartyRole.Captain, -0.1f, EffectIncrementType.AddFactor, TroopUsageFlags.Any, TroopUsageFlags.Any, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_shatteringBlow.Initialize("{=mbaYZ0QB}Shattering Blow", NavalSkills.Mariner, GetTierCost(6), _shatteringVolley, "{=MUsv10MO}{VALUE}% armor penetration for melee weapons.", PartyRole.Personal, 0.5f, EffectIncrementType.AddFactor, "{=vmBVfzVL}{VALUE}% armor penetration for melee weapons wielded by crew.", PartyRole.Captain, 0.5f, EffectIncrementType.AddFactor, TroopUsageFlags.Melee, TroopUsageFlags.Melee, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_shatteringVolley.Initialize("{=InUgc3PT}Shattering Volley", NavalSkills.Mariner, GetTierCost(6), _shatteringBlow, "{=pKk0fKba}{VALUE}% armor penetration for ranged weapons.", PartyRole.Personal, 0.5f, EffectIncrementType.AddFactor, "{=MzQBOs13}{VALUE}% armor penetration for ranged weapons wielded by crew.", PartyRole.Captain, 0.5f, EffectIncrementType.AddFactor, TroopUsageFlags.Ranged, TroopUsageFlags.Ranged, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_arr.Initialize("{=OlvwVG3b}Arr!", NavalSkills.Mariner, GetTierCost(7), _pirateHunter, "{=Sa7FPVnT}Surrendering pirate parties can be recruited.", PartyRole.Personal, 0f, EffectIncrementType.Add, "{=eHg3h3j7}{VALUE}% xp gain after each battle for mariner troops under character's command.", PartyRole.Captain, 0.15f, EffectIncrementType.AddFactor, TroopUsageFlags.Undefined, TroopUsageFlags.Any, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_pirateHunter.Initialize("{=qlMgDT7y}Pirate Hunter", NavalSkills.Mariner, GetTierCost(7), _arr, "{=HnyLGFbu}{VALUE}% bonus when crew is sent to confront pirates.", PartyRole.PartyLeader, 0.2f, EffectIncrementType.AddFactor, "{=sIOdlPOA}{VALUE}% xp gain after each battle for regular troops under character's command.", PartyRole.Captain, 0.1f, EffectIncrementType.AddFactor, TroopUsageFlags.Undefined, TroopUsageFlags.Any, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_boardingMaster.Initialize("{=gkJ1fRSM}Boarding Master", NavalSkills.Mariner, GetTierCost(8), _homeTurfAdvantage, "{=HAbSFYFz}{VALUE}% melee damage dealt by character when fighting on other ships.", PartyRole.Personal, 0.15f, EffectIncrementType.AddFactor, "{=pR8afW3c}{VALUE}% melee damage dealt by crew when fighting on other ships.", PartyRole.Captain, 0.15f, EffectIncrementType.AddFactor, TroopUsageFlags.Melee, TroopUsageFlags.Melee, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_homeTurfAdvantage.Initialize("{=n5g7EvDQ}Home Turf Advantage", NavalSkills.Mariner, GetTierCost(8), _boardingMaster, "{=RLrenzWj}{VALUE}% melee damage dealt by character when fighting on own ship.", PartyRole.Personal, 0.2f, EffectIncrementType.AddFactor, "{=8Qp9IKuG}{VALUE}% melee damage dealt by crew when fighting on own ship.", PartyRole.Captain, 0.2f, EffectIncrementType.AddFactor, TroopUsageFlags.Melee, TroopUsageFlags.Melee, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_mightyBlows.Initialize("{=RSMwW4mr}Mighty Blows", NavalSkills.Mariner, GetTierCost(9), _crewOfSpears, "{=YbAddajR}Better cleave with two handed weapons swings. (Two handed weapons lose {VALUE}% less damage when they cut through the first opponent.)", PartyRole.Personal, -0.5f, EffectIncrementType.AddFactor, "{=fjettvEU}{VALUE}% melee damage dealt by crew armed with two-handed weapons.", PartyRole.Captain, 0.15f, EffectIncrementType.AddFactor, TroopUsageFlags.TwoHandedUser, TroopUsageFlags.TwoHandedUser, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_crewOfSpears.Initialize("{=BDp8MzPJ}Crew of Spears", NavalSkills.Mariner, GetTierCost(9), _mightyBlows, "{=IQhRdMoc}Impale shields with thrown javelins, and throwing axes deals damage after if they break a shield.", PartyRole.Personal, 0f, EffectIncrementType.Add, "{=wMwRA172}{VALUE}% ranged damage dealt by crew armed with throwing weapons.", PartyRole.Captain, 0.15f, EffectIncrementType.AddFactor, TroopUsageFlags.ThrownUser, TroopUsageFlags.ThrownUser, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_theSkysFury.Initialize("{=fS6ZrhCH}The Sky's Fury", NavalSkills.Mariner, GetTierCost(10), _warriorsMight, "{=aP1gxbRF}{VALUE}% ranged damage dealt by character.", PartyRole.Personal, 0.15f, EffectIncrementType.AddFactor, "{=MHC4pkJE}{VALUE}% to bow and crossbow damage dealt by crew.", PartyRole.Captain, 0.15f, EffectIncrementType.AddFactor, TroopUsageFlags.Ranged, TroopUsageFlags.BowUser | TroopUsageFlags.CrossbowUser, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_warriorsMight.Initialize("{=CuNzwLc3}Warrior's Might", NavalSkills.Mariner, GetTierCost(10), _theSkysFury, "{=H7Hs2E3D}{VALUE}% melee damage dealt by character.", PartyRole.Personal, 0.2f, EffectIncrementType.AddFactor, "{=KvGLih9i}{VALUE}% to throwing damage dealt by crew.", PartyRole.Captain, 0.2f, EffectIncrementType.AddFactor, TroopUsageFlags.Melee, TroopUsageFlags.ThrownUser, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_merchantPrince.Initialize("{=P79raYEW}Merchant Prince", NavalSkills.Boatswain, GetTierCost(1), _masterShipwright, "{=UL4LyWhF}{VALUE}% to ship repair cost", PartyRole.FirstMate, -0.3f, EffectIncrementType.AddFactor, "{=bQ5iyRiM}{VALUE} denars for each ship bought or sold in governed settlement.", PartyRole.Governor, 500f, EffectIncrementType.Add);
		_masterShipwright.Initialize("{=DQ7KWQJq}Master Shipwright", NavalSkills.Boatswain, GetTierCost(1), _merchantPrince, "{=R0akt6jI}{VALUE}% to cost of ship upgrades", PartyRole.FirstMate, -0.3f, EffectIncrementType.AddFactor, "{=WvBaUVAr}{VALUE} denars from each ship repaired at governed settlement", PartyRole.Governor, 30f, EffectIncrementType.Add);
		_streamlinedOperations.Initialize("{=oUtZ27Id}Streamlined Operations", NavalSkills.Boatswain, GetTierCost(2), _wellStocked, "{=Trsw67ag}{VALUE}% ballista reload time.", PartyRole.Captain, 0.1f, EffectIncrementType.AddFactor, "{=ZWOY78k2}{VALUE}% to shipyard production rate in governed settlement.", PartyRole.Governor, 0.2f, EffectIncrementType.AddFactor, TroopUsageFlags.Any, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly);
		_wellStocked.Initialize("{=ReTUWtie}Well Stocked", NavalSkills.Boatswain, GetTierCost(2), _streamlinedOperations, "{=Myea2YPh}{VALUE}% ammunition per stack for crew under command.", PartyRole.FirstMate, 0.3f, EffectIncrementType.AddFactor, "{=4XCAUAee}{VALUE} extra ammunition per stack for thrown weapons of the crew.", PartyRole.FirstMate, 2f, EffectIncrementType.Add, TroopUsageFlags.Any, TroopUsageFlags.ThrownUser, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_navalHorde.Initialize("{=1uWha4cw}Naval Horde", NavalSkills.Boatswain, GetTierCost(3), _optimization, "{=1aCQf9Xf}{VALUE}% to wages for cavalry troops while at sea.", PartyRole.FirstMate, -0.3f, EffectIncrementType.AddFactor, "{=9Hsd1fuX}{VALUE}% to weight of mounts when in ship's cargo.", PartyRole.FirstMate, -0.3f, EffectIncrementType.AddFactor, TroopUsageFlags.Undefined, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_optimization.Initialize("{=ON5j1Gwp}Optimization", NavalSkills.Boatswain, GetTierCost(3), _navalHorde, "{=KVrphJkB}{VALUE}% to wages of non-mariner troops while at sea.", PartyRole.FirstMate, -0.1f, EffectIncrementType.AddFactor, "{=wdnSjdLE}{VALUE}% to weight of pack animals and livestock when in ship's cargo", PartyRole.FirstMate, -0.3f, EffectIncrementType.AddFactor, TroopUsageFlags.Undefined, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_gildedPurse.Initialize("{=tXOmhbFz}Gilded Purse", NavalSkills.Boatswain, GetTierCost(4), _veteransWisdom, "{=xI8UK8Wp}{VALUE}% to chance of capturing ships after battle.", PartyRole.PartyLeader, 0.25f, EffectIncrementType.AddFactor, "{=bUXtraYH}{VALUE}% to weight of trade goods in ship's cargo.", PartyRole.FirstMate, -0.15f, EffectIncrementType.AddFactor, TroopUsageFlags.Undefined, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_veteransWisdom.Initialize("{=Nlz7g0GX}Veteran's Wisdom", NavalSkills.Boatswain, GetTierCost(4), _gildedPurse, "{=ziKgHMqy}Daily bonus of {VALUE}x character level xp to companions.", PartyRole.PartyLeader, 10f, EffectIncrementType.Add, "{=jzwCbxni}{VALUE}% overburden penalty for ships with too much cargo.", PartyRole.FirstMate, -0.2f, EffectIncrementType.AddFactor, TroopUsageFlags.Undefined, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_shipwrightsInsight.Initialize("{=6gQTNK1Q}Shipwright's Insight", NavalSkills.Boatswain, GetTierCost(5), _specialArrows, "{=fEiCOPxK}{VALUE}% damage to hulls of enemy ships dealt by ballista.", PartyRole.Personal, 0.3f, EffectIncrementType.AddFactor, "{=9WinSM8I}{VALUE}% extra ammo for crew.", PartyRole.Captain, 0.25f, EffectIncrementType.AddFactor, TroopUsageFlags.Any, TroopUsageFlags.Ranged, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_specialArrows.Initialize("{=sSwzYLVp}Special Arrows", NavalSkills.Boatswain, GetTierCost(5), _shipwrightsInsight, "{=KqEZ5bht}{VALUE} armor for low-tier troops.", PartyRole.Captain, 5f, EffectIncrementType.Add, "{=8kHlbgJZ}{VALUE}% damage dealt by crew to enemy sails.", PartyRole.FirstMate, 0.4f, EffectIncrementType.AddFactor, TroopUsageFlags.Any, TroopUsageFlags.Any, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_smoothOperator.Initialize("{=k5xfZsXE}Smooth Operator", NavalSkills.Boatswain, GetTierCost(6), _accuracyTraining, "{=elLNH0Ys}{VALUE} ammo for ballista.", PartyRole.Captain, 5f, EffectIncrementType.Add, "{=JLP7pNIv}{VALUE}% food consumption at sea.", PartyRole.FirstMate, -0.3f, EffectIncrementType.AddFactor, TroopUsageFlags.Any, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_accuracyTraining.Initialize("{=09T5s6fh}Accuracy Training", NavalSkills.Boatswain, GetTierCost(6), _smoothOperator, "{=TAcbw7Ac}{VALUE}% damage dealt to shields with ranged weapons wielded by crew.", PartyRole.Captain, 0.3f, EffectIncrementType.AddFactor, "{=MDDAbNbX}{VALUE} militia in coastal towns and villages.", PartyRole.Governor, 2f, EffectIncrementType.Add, TroopUsageFlags.Ranged, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly);
		_efficientCaptain.Initialize("{=bb4nRJwq}Efficient Captain", NavalSkills.Boatswain, GetTierCost(7), _popularCaptain, "{=OFa86K25}{VALUE}% upgrade cost for mariner troops.", PartyRole.PartyLeader, -0.3f, EffectIncrementType.AddFactor, "{=VbWCb1JQ}{VALUE} morale for the party while waiting in a port.", PartyRole.PartyLeader, 5f, EffectIncrementType.Add, TroopUsageFlags.Undefined, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.All, PerkObject.EffectEnvironment.NavalOnly);
		_popularCaptain.Initialize("{=di0OsDy8}Popular Captain", NavalSkills.Boatswain, GetTierCost(7), _efficientCaptain, "{=V4ornHeA}{VALUE}% recruitment cost for mariner troops.", PartyRole.PartyLeader, -0.3f, EffectIncrementType.AddFactor, "{=oIWHDTkm}{VALUE}% combat deck size for the ship under the character's command.", PartyRole.PartyLeader, 0.05f, EffectIncrementType.AddFactor, TroopUsageFlags.Undefined, TroopUsageFlags.Any, PerkObject.EffectEnvironment.All, PerkObject.EffectEnvironment.NavalOnly);
		_portAuthority.Initialize("{=MATYHBox}Port Authority", NavalSkills.Boatswain, GetTierCost(8), _blessingsOfTheSea, "{=2mzGeKT1}{VALUE} ship to command limit.", PartyRole.PartyLeader, 1f, EffectIncrementType.Add, "{=dW9eIKTx}{VALUE}% to production of walrus ivory and whale oil at villages of the governed settlement.", PartyRole.Governor, 0.15f, EffectIncrementType.AddFactor, TroopUsageFlags.Undefined, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly);
		_blessingsOfTheSea.Initialize("{=eeY1vDcp}Blessings Of The Sea", NavalSkills.Boatswain, GetTierCost(8), _portAuthority, "{=2mzGeKT1}{VALUE} ship to command limit.", PartyRole.PartyLeader, 1f, EffectIncrementType.Add, "{=astRi69P}{VALUE}% to production of fish at villages governed of the governed settlement.", PartyRole.Governor, 0.25f, EffectIncrementType.AddFactor, TroopUsageFlags.Undefined, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly);
		_shipwrightsHand.Initialize("{=2DY8xbnU}Shipwright's Hand", NavalSkills.Boatswain, GetTierCost(9), _salvage, "{=MsFlwAwg}Discarded ships repair party ships.", PartyRole.PartyLeader, 0f, EffectIncrementType.Add, "{=XSAXjPX3}{VALUE} recruit to garrison for each merchant convoy entering port.", PartyRole.Governor, 1f, EffectIncrementType.Add);
		_salvage.Initialize("{=LkaTaAyq}Salvage", NavalSkills.Boatswain, GetTierCost(9), _shipwrightsHand, "{=0zuAboaA}Discarded ships provide influence.", PartyRole.PartyLeader, 0f, EffectIncrementType.Add, "{=L7uvT9VN}{VALUE} denars gained for each merchant convoy entering port", PartyRole.Governor, 40f, EffectIncrementType.Add);
		_merchantFleet.Initialize("{=0xz4b4wl}Merchant Fleet", NavalSkills.Boatswain, GetTierCost(10), _resilience, "{=Iu7QpMVa}{VALUE} to command limit of ships in battle.", PartyRole.PartyLeader, 1f, EffectIncrementType.Add, "{=JfZESBwx}{VALUE} influence gained for each ship built in the governed settlement.", PartyRole.Governor, 5f, EffectIncrementType.Add, TroopUsageFlags.Undefined, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly);
		_resilience.Initialize("{=9mNUfKMo}Resilience", NavalSkills.Boatswain, GetTierCost(10), _merchantFleet, "{=qVyf6an2}{VALUE}% of the health points lost in a battle are recovered after a victory.", PartyRole.Personal, 0.3f, EffectIncrementType.AddFactor, "{=KkCqaWLc}{VALUE}% to troops' healing rate while at sea.", PartyRole.FirstMate, 0.3f, EffectIncrementType.AddFactor, TroopUsageFlags.Undefined, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_navalBombardment.Initialize("{=21meODRf}Naval Bombardment", NavalSkills.Boatswain, GetTierCost(11), null, "{=61FfgHSc}Shipboard ballistas fire during sieges", PartyRole.PartyLeader, 0f, EffectIncrementType.Add, "", PartyRole.None, 0f, EffectIncrementType.Invalid, TroopUsageFlags.Undefined, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly);
		_masterAngler.Initialize("{=DWBiOEdQ}Master Angler", NavalSkills.Shipmaster, GetTierCost(1), _oldSaltsTouch, "{=DrNqwl3D}{VALUE}% chance per hour of campaign time to catch fish while sailing on campaign map.", PartyRole.Navigator, 0.25f, EffectIncrementType.AddFactor, "{=CHCbc79h}{VALUE}% to catch brought in by fishing boats from settlements.", PartyRole.Governor, 0.15f, EffectIncrementType.AddFactor, TroopUsageFlags.Undefined, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly);
		_oldSaltsTouch.Initialize("{=vcyysBJb}Old Salt's Touch", NavalSkills.Shipmaster, GetTierCost(1), _masterAngler, "{=l6P2ivTu}{VALUE}% to travel speed at sea.", PartyRole.Navigator, 0.02f, EffectIncrementType.AddFactor, "{=1UIq6BRz}{VALUE}% to swimming endurance of crew", PartyRole.Captain, 0.3f, EffectIncrementType.AddFactor, TroopUsageFlags.Undefined, TroopUsageFlags.Any, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_ghostShip.Initialize("{=Sya9pgBv}Ghost Ship", NavalSkills.Shipmaster, GetTierCost(2), _windRider, "{=nTPPc5CT}{VALUE}% fewer troops lost when running a blockade and {VALUE}% chance to avoid leaving ships behind when escaping engagements.", PartyRole.Navigator, 0.2f, EffectIncrementType.AddFactor, "{=B9AfUcsl}{VALUE}% to campaign map speed of town's fishing boats.", PartyRole.Governor, 0.2f, EffectIncrementType.AddFactor, TroopUsageFlags.Undefined, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly);
		_windRider.Initialize("{=bIFiogWa}Wind Rider", NavalSkills.Shipmaster, GetTierCost(2), _ghostShip, "{=kIb7qvy8}{VALUE}% to deck movement penalty.", PartyRole.Personal, -0.5f, EffectIncrementType.AddFactor, "{=m2ZMZ27i}{VALUE}% to deck movement penalty for crew.", PartyRole.Captain, -0.5f, EffectIncrementType.AddFactor, TroopUsageFlags.Any, TroopUsageFlags.Any, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_riverRaider.Initialize("{=46X8OsCn}River Raider", NavalSkills.Shipmaster, GetTierCost(3), _nightRaider, "{=fMe3Yas2}{VALUE}% to coastal movement speed penalty.", PartyRole.Navigator, -0.03f, EffectIncrementType.AddFactor, "{=XbbSGawE}{VALUE}% to chance of capturing enemy troops as prisoners.", PartyRole.PartyLeader, 0.1f, EffectIncrementType.AddFactor, TroopUsageFlags.Undefined, TroopUsageFlags.Any, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_nightRaider.Initialize("{=MQlcI2bf}Night Raider", NavalSkills.Shipmaster, GetTierCost(3), _riverRaider, "{=CRSaWc9S}{VALUE}% to night spotting range penalty on campaign map.", PartyRole.Navigator, -0.5f, EffectIncrementType.AddFactor, "{=tYqawD5G}{VALUE} fish per day produced by coastal villages.", PartyRole.Governor, 5f, EffectIncrementType.Add, TroopUsageFlags.Undefined, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly);
		_windborne.Initialize("{=49NFcwEM}Windborne", NavalSkills.Shipmaster, GetTierCost(4), _shockAndAwe, "{=6JZ8ZAqD}{VALUE}% to sail forces in missions.", PartyRole.Captain, 0.2f, EffectIncrementType.AddFactor, "{=dnCVmLuI}{VALUE}% to duration of disorganized state while at sea.", PartyRole.Navigator, -0.5f, EffectIncrementType.AddFactor, TroopUsageFlags.Any, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_shockAndAwe.Initialize("{=M1RMMBzF}Shock and Awe", NavalSkills.Shipmaster, GetTierCost(4), _windborne, "{=nxYOlJuf}{VALUE}% morale boost when ship rams enemy ship.", PartyRole.Captain, 0.3f, EffectIncrementType.AddFactor, "{=YEpGBEOb}{VALUE}% to speed on campaign map when against the wind.", PartyRole.Navigator, 0.1f, EffectIncrementType.AddFactor, TroopUsageFlags.Any, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_theHelmsmansShield.Initialize("{=nO4nrVeF}The Helmsman's Shield", NavalSkills.Shipmaster, GetTierCost(5), _ravenEye, "{=IEQgidZB}{VALUE}% to ranged damage suffered by character while steering the ship.", PartyRole.Personal, -0.5f, EffectIncrementType.AddFactor, "{=ZV0bkXpX}{VALUE} prosperity for each fishing boat returning to port.", PartyRole.Governor, 1f, EffectIncrementType.Add, TroopUsageFlags.Any, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly);
		_ravenEye.Initialize("{=NMgbVLbx}Raven Eye", NavalSkills.Shipmaster, GetTierCost(5), _theHelmsmansShield, "{=zEJLYSYa}{VALUE}% to spotting range on campaign map.", PartyRole.Navigator, 0.2f, EffectIncrementType.AddFactor, "{=5bFACRPa}{VALUE} loyalty for each fishing boat returning to port.", PartyRole.Governor, 1f, EffectIncrementType.Add, TroopUsageFlags.Undefined, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly);
		_fairWinds.Initialize("{=LOZkPSV1}Fair Winds", NavalSkills.Shipmaster, GetTierCost(6), _favorableTide, "{=qC0VbVsB}{VALUE}% to campaign map travel speed when running before the wind.", PartyRole.Navigator, 0.1f, EffectIncrementType.AddFactor, "{=alTcdq2M}{VALUE}% to hearth growth rate in coastal villages", PartyRole.Governor, 0.1f, EffectIncrementType.AddFactor, TroopUsageFlags.Undefined, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly);
		_favorableTide.Initialize("{=uZo1RgiX}Favorable Tide", NavalSkills.Shipmaster, GetTierCost(6), _fairWinds, "{=MgNvqdV5}{VALUE}% to campaign map travel speed ", PartyRole.Navigator, 0.05f, EffectIncrementType.AddFactor, "{=5fwthk1U}{VALUE} building material in settlement for each merchant convoy visiting port", PartyRole.Governor, 1f, EffectIncrementType.Add, TroopUsageFlags.Undefined, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly);
		_unflinching.Initialize("{=WdFTSzc1}Unflinching", NavalSkills.Shipmaster, GetTierCost(7), _shoreMaster, "{=dpCdggb6}Halves disembarkation time", PartyRole.PartyLeader, -0.5f, EffectIncrementType.AddFactor);
		_shoreMaster.Initialize("{=YluQBFDG}Shore Master", NavalSkills.Shipmaster, GetTierCost(7), _unflinching, "{=qhsfjbv6}{VALUE}% to ship recall time.", PartyRole.PartyLeader, -0.5f, EffectIncrementType.AddFactor, "{=mbT5BIx3}{VALUE}% to fleet size penalty for campaign map movement", PartyRole.Navigator, -0.3f, EffectIncrementType.AddFactor, TroopUsageFlags.Undefined, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.All, PerkObject.EffectEnvironment.NavalOnly);
		_fleetCommander.Initialize("{=ZJyEHWfa}Fleet Commander", NavalSkills.Shipmaster, GetTierCost(8), _chainToOars, "{=mbT5BIx3}{VALUE}% to fleet size penalty for campaign map movement", PartyRole.PartyLeader, -0.3f, EffectIncrementType.AddFactor, "{=F4zoUlg1}{VALUE}% to skeleton crew requirement for any ship in party", PartyRole.PartyLeader, -0.2f, EffectIncrementType.AddFactor, TroopUsageFlags.Undefined, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly);
		_chainToOars.Initialize("{=R1izgjgK}Chain to Oars", NavalSkills.Shipmaster, GetTierCost(8), _fleetCommander, "{=vSJEt7l8}{VALUE}% to oar force in mission", PartyRole.Captain, 0.2f, EffectIncrementType.AddFactor, "{=vRjKbjOa}Prisoners help meet skeleton crew requirement", PartyRole.PartyLeader, 0f, EffectIncrementType.Add, TroopUsageFlags.Any, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly);
		_stormrider.Initialize("{=uLPXg0qS}Stormrider", NavalSkills.Shipmaster, GetTierCost(9), _masterAndCommander, "{=EAlm5gSh}{VALUE} xp gained by each troop once per day when entering storms", PartyRole.PartyLeader, 30f, EffectIncrementType.Add, "{=2mzGeKT1}{VALUE} ship to command limit.", PartyRole.PartyLeader, 1f, EffectIncrementType.Add, TroopUsageFlags.Undefined, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_masterAndCommander.Initialize("{=tivTy0RA}Master and commander", NavalSkills.Shipmaster, GetTierCost(9), _stormrider, "{=dGZAPFdQ}{VALUE} xp gained by each troop per hour at sea", PartyRole.PartyLeader, 1f, EffectIncrementType.Add, "{=2mzGeKT1}{VALUE} ship to command limit.", PartyRole.PartyLeader, 1f, EffectIncrementType.Add, TroopUsageFlags.Undefined, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_theCorsairsEdge.Initialize("{=JvwosrT8}The Corsair's Edge", NavalSkills.Shipmaster, GetTierCost(10), _seaborneFortress, "{=QUI46tx7}{VALUE}% damage when wielding one-handed weapons at sea", PartyRole.Personal, 0.1f, EffectIncrementType.AddFactor, "{=DRb8Us5b}{VALUE} fishing boat produced by each coastal settlement.", PartyRole.Governor, 1f, EffectIncrementType.Add, TroopUsageFlags.OneHandedUser, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly);
		_seaborneFortress.Initialize("{=Dyy3HcUI}Seaborne Fortress", NavalSkills.Shipmaster, GetTierCost(10), _theCorsairsEdge, "{=kAajH6yd}{VALUE}% to damage sustained by ships when crew is sent to confront the enemies.", PartyRole.PartyLeader, -0.1f, EffectIncrementType.AddFactor, "{=OszgWi0t}{VALUE}% to ranged damage taken by crew if not boarded.", PartyRole.Captain, -0.2f, EffectIncrementType.AddFactor, TroopUsageFlags.Undefined, TroopUsageFlags.Any, PerkObject.EffectEnvironment.NavalOnly, PerkObject.EffectEnvironment.NavalOnly);
		_commodore.Initialize("{=NbZeB1RT}Commodore", NavalSkills.Shipmaster, GetTierCost(11), null, "{=1g9TufnU}Flagship figurehead provides bonus to all allied ships.", PartyRole.ArmyCommander, 0f, EffectIncrementType.Add, "", PartyRole.None, 0f, EffectIncrementType.Invalid, TroopUsageFlags.Undefined, TroopUsageFlags.Undefined, PerkObject.EffectEnvironment.NavalOnly);
	}

	private PerkObject Create(string stringId)
	{
		return Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject(stringId));
	}

	private static int GetTierCost(int tierIndex)
	{
		return TierSkillRequirements[tierIndex - 1];
	}
}
