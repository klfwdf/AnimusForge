namespace TaleWorlds.Core;

public class AgentData
{
	public BasicCharacterObject AgentCharacter { get; private set; }

	public Monster AgentMonster { get; private set; }

	public IBattleCombatant AgentOwnerParty { get; private set; }

	public Equipment AgentOverridenEquipment { get; private set; }

	public int AgentEquipmentSeed { get; private set; }

	public bool AgentNoHorses { get; private set; }

	public string AgentMountKey { get; private set; }

	public bool AgentNoWeapons { get; private set; }

	public bool AgentNoArmor { get; private set; }

	public bool AgentFixedEquipment { get; private set; }

	public bool AgentCivilianEquipment { get; private set; }

	public uint AgentClothingColor1 { get; private set; }

	public uint AgentClothingColor2 { get; private set; }

	public bool PrepareImmediately { get; private set; }

	public bool BodyPropertiesOverriden { get; private set; }

	public BodyProperties AgentBodyProperties { get; private set; }

	public bool AgeOverriden { get; private set; }

	public int AgentAge { get; private set; }

	public bool GenderOverriden { get; private set; }

	public bool AgentIsFemale { get; private set; }

	public int AgentRace { get; private set; }

	public IAgentOriginBase AgentOrigin { get; private set; }

	public AgentData(IAgentOriginBase agentOrigin)
		: this(agentOrigin.Troop)
	{
		AgentOrigin = agentOrigin;
		AgentCharacter = agentOrigin.Troop;
		AgentEquipmentSeed = agentOrigin.Seed;
	}

	public AgentData(BasicCharacterObject characterObject)
	{
		AgentCharacter = characterObject;
		AgentRace = characterObject.Race;
		AgentMonster = FaceGen.GetBaseMonsterFromRace(AgentRace);
		AgentOwnerParty = null;
		AgentOverridenEquipment = null;
		AgentEquipmentSeed = 0;
		AgentNoHorses = false;
		AgentNoWeapons = false;
		AgentNoArmor = false;
		AgentFixedEquipment = false;
		AgentCivilianEquipment = false;
		AgentClothingColor1 = uint.MaxValue;
		AgentClothingColor2 = uint.MaxValue;
		BodyPropertiesOverriden = false;
		GenderOverriden = false;
	}

	public AgentData Character(BasicCharacterObject characterObject)
	{
		AgentCharacter = characterObject;
		return this;
	}

	public AgentData Monster(Monster monster)
	{
		AgentMonster = monster;
		return this;
	}

	public AgentData OwnerParty(IBattleCombatant owner)
	{
		AgentOwnerParty = owner;
		return this;
	}

	public AgentData Equipment(Equipment equipment)
	{
		AgentOverridenEquipment = equipment;
		return this;
	}

	public AgentData EquipmentSeed(int seed)
	{
		AgentEquipmentSeed = seed;
		return this;
	}

	public AgentData NoHorses(bool noHorses)
	{
		AgentNoHorses = noHorses;
		return this;
	}

	public AgentData NoWeapons(bool noWeapons)
	{
		AgentNoWeapons = noWeapons;
		return this;
	}

	public AgentData NoArmor(bool noArmor)
	{
		AgentNoArmor = noArmor;
		return this;
	}

	public AgentData FixedEquipment(bool fixedEquipment)
	{
		AgentFixedEquipment = fixedEquipment;
		return this;
	}

	public AgentData CivilianEquipment(bool civilianEquipment)
	{
		AgentCivilianEquipment = civilianEquipment;
		return this;
	}

	public AgentData SetPrepareImmediately()
	{
		PrepareImmediately = true;
		return this;
	}

	public AgentData ClothingColor1(uint color)
	{
		AgentClothingColor1 = color;
		return this;
	}

	public AgentData ClothingColor2(uint color)
	{
		AgentClothingColor2 = color;
		return this;
	}

	public AgentData BodyProperties(BodyProperties bodyProperties)
	{
		AgentBodyProperties = bodyProperties;
		BodyPropertiesOverriden = true;
		return this;
	}

	public AgentData Age(int age)
	{
		AgentAge = age;
		AgeOverriden = true;
		return this;
	}

	public AgentData TroopOrigin(IAgentOriginBase troopOrigin)
	{
		AgentOrigin = troopOrigin;
		if (troopOrigin?.Troop != null && !troopOrigin.Troop.IsHero)
		{
			EquipmentSeed(troopOrigin.Seed);
		}
		return this;
	}

	public AgentData IsFemale(bool isFemale)
	{
		AgentIsFemale = isFemale;
		GenderOverriden = true;
		return this;
	}

	public AgentData Race(int race)
	{
		AgentRace = race;
		GenderOverriden = true;
		return this;
	}

	public AgentData MountKey(string mountKey)
	{
		AgentMountKey = mountKey;
		return this;
	}
}
