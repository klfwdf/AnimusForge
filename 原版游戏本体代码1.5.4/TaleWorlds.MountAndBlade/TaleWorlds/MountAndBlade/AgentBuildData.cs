using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class AgentBuildData
{
	public AgentData AgentData { get; private set; }

	public BasicCharacterObject AgentCharacter => AgentData.AgentCharacter;

	public Monster AgentMonster => AgentData.AgentMonster;

	public Equipment AgentOverridenSpawnEquipment => AgentData.AgentOverridenEquipment;

	public MissionEquipment AgentOverridenSpawnMissionEquipment { get; private set; }

	public int AgentEquipmentSeed => AgentData.AgentEquipmentSeed;

	public bool AgentNoHorses => AgentData.AgentNoHorses;

	public string AgentMountKey => AgentData.AgentMountKey;

	public bool AgentNoWeapons => AgentData.AgentNoWeapons;

	public bool AgentNoArmor => AgentData.AgentNoArmor;

	public bool AgentFixedEquipment => AgentData.AgentFixedEquipment;

	public bool AgentCivilianEquipment => AgentData.AgentCivilianEquipment;

	public uint AgentClothingColor1 => AgentData.AgentClothingColor1;

	public uint AgentClothingColor2 => AgentData.AgentClothingColor2;

	public bool BodyPropertiesOverriden => AgentData.BodyPropertiesOverriden;

	public BodyProperties AgentBodyProperties => AgentData.AgentBodyProperties;

	public bool AgeOverriden => AgentData.AgeOverriden;

	public int AgentAge => AgentData.AgentAge;

	public bool PrepareImmediately => AgentData.PrepareImmediately;

	public bool GenderOverriden => AgentData.GenderOverriden;

	public bool AgentIsFemale => AgentData.AgentIsFemale;

	public int AgentRace => AgentData.AgentRace;

	public IAgentOriginBase AgentOrigin => AgentData.AgentOrigin;

	public AgentControllerType AgentController { get; private set; }

	public Team AgentTeam { get; private set; }

	public bool AgentIsReinforcement { get; private set; }

	public bool AgentSpawnsIntoOwnFormation { get; private set; }

	public bool AgentSpawnsUsingOwnTroopClass { get; private set; }

	public float MakeUnitStandOutDistance { get; private set; }

	public Vec3? AgentInitialPosition { get; private set; }

	public Vec2? AgentInitialDirection { get; private set; }

	public Formation AgentFormation { get; private set; }

	public int AgentFormationTroopSpawnCount { get; private set; }

	public int AgentFormationTroopSpawnIndex { get; private set; }

	public MissionPeer AgentMissionPeer { get; private set; }

	public MissionPeer OwningAgentMissionPeer { get; private set; }

	public bool AgentIndexOverriden { get; private set; }

	public int AgentIndex { get; private set; }

	public bool AgentMountIndexOverriden { get; private set; }

	public int AgentMountIndex { get; private set; }

	public int AgentVisualsIndex { get; private set; }

	public Banner AgentBanner { get; private set; }

	public ItemObject AgentBannerItem { get; private set; }

	public ItemObject AgentBannerReplacementWeaponItem { get; private set; }

	public bool AgentCanSpawnOutsideOfMissionBoundary { get; private set; }

	public bool RandomizeColors
	{
		get
		{
			if (AgentCharacter != null && !AgentCharacter.IsHero)
			{
				return AgentMissionPeer == null;
			}
			return false;
		}
	}

	public bool UseFaceCache { get; set; }

	public int FaceCacheId { get; set; }

	private AgentBuildData()
	{
		AgentController = AgentControllerType.AI;
		AgentTeam = TaleWorlds.MountAndBlade.Team.Invalid;
		AgentFormation = null;
		AgentMissionPeer = null;
		AgentFormationTroopSpawnIndex = -1;
		UseFaceCache = false;
		FaceCacheId = 0;
	}

	public AgentBuildData(AgentData agentData)
		: this()
	{
		AgentData = agentData;
	}

	public AgentBuildData(IAgentOriginBase agentOrigin)
		: this()
	{
		AgentData = new AgentData(agentOrigin);
	}

	public AgentBuildData(BasicCharacterObject characterObject)
		: this()
	{
		AgentData = new AgentData(characterObject);
	}

	public AgentBuildData Character(BasicCharacterObject characterObject)
	{
		AgentData.Character(characterObject);
		return this;
	}

	public AgentBuildData Controller(AgentControllerType controller)
	{
		AgentController = controller;
		return this;
	}

	public AgentBuildData Team(Team team)
	{
		AgentTeam = team;
		return this;
	}

	public AgentBuildData IsReinforcement(bool isReinforcement)
	{
		AgentIsReinforcement = isReinforcement;
		return this;
	}

	public AgentBuildData SpawnsIntoOwnFormation(bool spawnIntoOwnFormation)
	{
		AgentSpawnsIntoOwnFormation = spawnIntoOwnFormation;
		return this;
	}

	public AgentBuildData SpawnsUsingOwnTroopClass(bool spawnUsingOwnTroopClass)
	{
		AgentSpawnsUsingOwnTroopClass = spawnUsingOwnTroopClass;
		return this;
	}

	public AgentBuildData MakeUnitStandOutOfFormationDistance(float makeUnitStandOutDistance)
	{
		MakeUnitStandOutDistance = makeUnitStandOutDistance;
		return this;
	}

	public AgentBuildData InitialPosition(in Vec3 position)
	{
		AgentInitialPosition = position;
		return this;
	}

	public AgentBuildData InitialDirection(in Vec2 direction)
	{
		AgentInitialDirection = direction;
		return this;
	}

	public AgentBuildData InitialFrameFromSpawnPointEntity(GameEntity entity)
	{
		MatrixFrame globalFrame = entity.GetGlobalFrame();
		AgentInitialPosition = globalFrame.origin;
		AgentInitialDirection = globalFrame.rotation.f.AsVec2.Normalized();
		return this;
	}

	public AgentBuildData InitialFrameFromSpawnPointEntity(WeakGameEntity entity)
	{
		MatrixFrame globalFrame = entity.GetGlobalFrame();
		AgentInitialPosition = globalFrame.origin;
		AgentInitialDirection = globalFrame.rotation.f.AsVec2.Normalized();
		return this;
	}

	public AgentBuildData Formation(Formation formation)
	{
		AgentFormation = formation;
		return this;
	}

	public AgentBuildData Monster(Monster monster)
	{
		AgentData.Monster(monster);
		return this;
	}

	public AgentBuildData VisualsIndex(int index)
	{
		AgentVisualsIndex = index;
		return this;
	}

	public AgentBuildData Equipment(Equipment equipment)
	{
		AgentData.Equipment(equipment);
		return this;
	}

	public AgentBuildData MissionEquipment(MissionEquipment missionEquipment)
	{
		AgentOverridenSpawnMissionEquipment = missionEquipment;
		return this;
	}

	public AgentBuildData EquipmentSeed(int seed)
	{
		AgentData.EquipmentSeed(seed);
		return this;
	}

	public AgentBuildData NoHorses(bool noHorses)
	{
		AgentData.NoHorses(noHorses);
		return this;
	}

	public AgentBuildData NoWeapons(bool noWeapons)
	{
		AgentData.NoWeapons(noWeapons);
		return this;
	}

	public AgentBuildData NoArmor(bool noArmor)
	{
		AgentData.NoArmor(noArmor);
		return this;
	}

	public AgentBuildData FixedEquipment(bool fixedEquipment)
	{
		AgentData.FixedEquipment(fixedEquipment);
		return this;
	}

	public AgentBuildData CivilianEquipment(bool civilianEquipment)
	{
		AgentData.CivilianEquipment(civilianEquipment);
		return this;
	}

	public AgentBuildData SetPrepareImmediately()
	{
		AgentData.SetPrepareImmediately();
		return this;
	}

	public AgentBuildData ClothingColor1(uint color)
	{
		AgentData.ClothingColor1(color);
		return this;
	}

	public AgentBuildData ClothingColor2(uint color)
	{
		AgentData.ClothingColor2(color);
		return this;
	}

	public AgentBuildData MissionPeer(MissionPeer missionPeer)
	{
		AgentMissionPeer = missionPeer;
		return this;
	}

	public AgentBuildData OwningMissionPeer(MissionPeer missionPeer)
	{
		OwningAgentMissionPeer = missionPeer;
		return this;
	}

	public AgentBuildData BodyProperties(BodyProperties bodyProperties)
	{
		AgentData.BodyProperties(bodyProperties);
		return this;
	}

	public AgentBuildData Age(int age)
	{
		AgentData.Age(age);
		return this;
	}

	public AgentBuildData TroopOrigin(IAgentOriginBase troopOrigin)
	{
		AgentData.TroopOrigin(troopOrigin);
		return this;
	}

	public AgentBuildData IsFemale(bool isFemale)
	{
		AgentData.IsFemale(isFemale);
		return this;
	}

	public AgentBuildData Race(int race)
	{
		AgentData.Race(race);
		return this;
	}

	public AgentBuildData MountKey(string mountKey)
	{
		AgentData.MountKey(mountKey);
		return this;
	}

	public AgentBuildData Index(int index)
	{
		AgentIndex = index;
		AgentIndexOverriden = true;
		return this;
	}

	public AgentBuildData MountIndex(int mountIndex)
	{
		AgentMountIndex = mountIndex;
		AgentMountIndexOverriden = true;
		return this;
	}

	public AgentBuildData Banner(Banner banner)
	{
		AgentBanner = banner;
		return this;
	}

	public AgentBuildData BannerItem(ItemObject bannerItem)
	{
		AgentBannerItem = bannerItem;
		return this;
	}

	public AgentBuildData BannerReplacementWeaponItem(ItemObject weaponItem)
	{
		AgentBannerReplacementWeaponItem = weaponItem;
		return this;
	}

	public AgentBuildData FormationTroopSpawnCount(int formationTroopCount)
	{
		AgentFormationTroopSpawnCount = formationTroopCount;
		return this;
	}

	public AgentBuildData FormationTroopSpawnIndex(int formationTroopIndex)
	{
		AgentFormationTroopSpawnIndex = formationTroopIndex;
		return this;
	}

	public AgentBuildData CanSpawnOutsideOfMissionBoundary(bool canSpawn)
	{
		AgentCanSpawnOutsideOfMissionBoundary = canSpawn;
		return this;
	}
}
