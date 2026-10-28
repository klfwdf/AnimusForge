using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class AgentVisualsData
{
	public MBAgentVisuals AgentVisuals;

	public MBActionSet ActionSetData { get; private set; }

	public MatrixFrame FrameData { get; private set; }

	public BodyProperties BodyPropertiesData { get; private set; }

	public Equipment EquipmentData { get; private set; }

	public int RightWieldedItemIndexData { get; private set; }

	public int LeftWieldedItemIndexData { get; private set; }

	public SkeletonType SkeletonTypeData { get; private set; }

	public Banner BannerData { get; private set; }

	public GameEntity CachedWeaponSlot0Entity { get; private set; }

	public GameEntity CachedWeaponSlot1Entity { get; private set; }

	public GameEntity CachedWeaponSlot2Entity { get; private set; }

	public GameEntity CachedWeaponSlot3Entity { get; private set; }

	public GameEntity CachedWeaponSlot4Entity { get; private set; }

	public Scene SceneData { get; private set; }

	public Monster MonsterData { get; private set; }

	public bool PrepareImmediatelyData { get; private set; }

	public bool UseScaledWeaponsData { get; private set; }

	public bool UseTranslucencyData { get; private set; }

	public bool UseTesselationData { get; private set; }

	public bool UseMorphAnimsData { get; private set; }

	public uint ClothColor1Data { get; private set; }

	public uint ClothColor2Data { get; private set; }

	public float ScaleData { get; private set; }

	public string CharacterObjectStringIdData { get; private set; }

	public ActionIndexCache ActionCodeData { get; private set; } = ActionIndexCache.act_none;

	public GameEntity EntityData { get; private set; }

	public bool HasClippingPlaneData { get; private set; }

	public string MountCreationKeyData { get; private set; }

	public bool AddColorRandomnessData { get; private set; }

	public int RaceData { get; private set; }

	public AgentVisualsData(AgentVisualsData agentVisualsData)
	{
		AgentVisuals = agentVisualsData.AgentVisuals;
		ActionSetData = agentVisualsData.ActionSetData;
		FrameData = agentVisualsData.FrameData;
		BodyPropertiesData = agentVisualsData.BodyPropertiesData;
		EquipmentData = agentVisualsData.EquipmentData;
		RightWieldedItemIndexData = agentVisualsData.RightWieldedItemIndexData;
		LeftWieldedItemIndexData = agentVisualsData.LeftWieldedItemIndexData;
		SkeletonTypeData = agentVisualsData.SkeletonTypeData;
		BannerData = agentVisualsData.BannerData;
		CachedWeaponSlot0Entity = agentVisualsData.CachedWeaponSlot0Entity;
		CachedWeaponSlot1Entity = agentVisualsData.CachedWeaponSlot1Entity;
		CachedWeaponSlot2Entity = agentVisualsData.CachedWeaponSlot2Entity;
		CachedWeaponSlot3Entity = agentVisualsData.CachedWeaponSlot3Entity;
		CachedWeaponSlot4Entity = agentVisualsData.CachedWeaponSlot4Entity;
		SceneData = agentVisualsData.SceneData;
		MonsterData = agentVisualsData.MonsterData;
		PrepareImmediatelyData = agentVisualsData.PrepareImmediatelyData;
		UseScaledWeaponsData = agentVisualsData.UseScaledWeaponsData;
		UseTranslucencyData = agentVisualsData.UseTranslucencyData;
		UseTesselationData = agentVisualsData.UseTesselationData;
		UseMorphAnimsData = agentVisualsData.UseMorphAnimsData;
		ClothColor1Data = agentVisualsData.ClothColor1Data;
		ClothColor2Data = agentVisualsData.ClothColor2Data;
		ScaleData = agentVisualsData.ScaleData;
		ActionCodeData = agentVisualsData.ActionCodeData;
		EntityData = agentVisualsData.EntityData;
		CharacterObjectStringIdData = agentVisualsData.CharacterObjectStringIdData;
		HasClippingPlaneData = agentVisualsData.HasClippingPlaneData;
		MountCreationKeyData = agentVisualsData.MountCreationKeyData;
		AddColorRandomnessData = agentVisualsData.AddColorRandomnessData;
		RaceData = agentVisualsData.RaceData;
	}

	public AgentVisualsData()
	{
		ClothColor1Data = uint.MaxValue;
		ClothColor2Data = uint.MaxValue;
		RightWieldedItemIndexData = -1;
		LeftWieldedItemIndexData = -1;
		ScaleData = 0f;
	}

	public AgentVisualsData Equipment(Equipment equipment)
	{
		EquipmentData = equipment;
		return this;
	}

	public AgentVisualsData BodyProperties(BodyProperties bodyProperties)
	{
		BodyPropertiesData = bodyProperties;
		return this;
	}

	public AgentVisualsData Frame(MatrixFrame frame)
	{
		FrameData = frame;
		return this;
	}

	public AgentVisualsData ActionSet(MBActionSet actionSet)
	{
		ActionSetData = actionSet;
		return this;
	}

	public AgentVisualsData Scene(Scene scene)
	{
		SceneData = scene;
		return this;
	}

	public AgentVisualsData Monster(Monster monster)
	{
		MonsterData = monster;
		return this;
	}

	public AgentVisualsData PrepareImmediately(bool prepareImmediately)
	{
		PrepareImmediatelyData = prepareImmediately;
		return this;
	}

	public AgentVisualsData UseScaledWeapons(bool useScaledWeapons)
	{
		UseScaledWeaponsData = useScaledWeapons;
		return this;
	}

	public AgentVisualsData SkeletonType(SkeletonType skeletonType)
	{
		SkeletonTypeData = skeletonType;
		return this;
	}

	public AgentVisualsData UseMorphAnims(bool useMorphAnims)
	{
		UseMorphAnimsData = useMorphAnims;
		return this;
	}

	public AgentVisualsData ClothColor1(uint clothColor1)
	{
		ClothColor1Data = clothColor1;
		return this;
	}

	public AgentVisualsData ClothColor2(uint clothColor2)
	{
		ClothColor2Data = clothColor2;
		return this;
	}

	public AgentVisualsData Banner(Banner banner)
	{
		BannerData = banner;
		return this;
	}

	public AgentVisualsData Race(int race)
	{
		RaceData = race;
		return this;
	}

	public GameEntity GetCachedWeaponEntity(EquipmentIndex slotIndex)
	{
		return slotIndex switch
		{
			EquipmentIndex.WeaponItemBeginSlot => CachedWeaponSlot0Entity, 
			EquipmentIndex.Weapon1 => CachedWeaponSlot1Entity, 
			EquipmentIndex.Weapon2 => CachedWeaponSlot2Entity, 
			EquipmentIndex.Weapon3 => CachedWeaponSlot3Entity, 
			EquipmentIndex.ExtraWeaponSlot => CachedWeaponSlot4Entity, 
			_ => null, 
		};
	}

	public AgentVisualsData CachedWeaponEntity(EquipmentIndex slotIndex, GameEntity cachedWeaponEntity)
	{
		switch (slotIndex)
		{
		case EquipmentIndex.WeaponItemBeginSlot:
			CachedWeaponSlot0Entity = cachedWeaponEntity;
			break;
		case EquipmentIndex.Weapon1:
			CachedWeaponSlot1Entity = cachedWeaponEntity;
			break;
		case EquipmentIndex.Weapon2:
			CachedWeaponSlot2Entity = cachedWeaponEntity;
			break;
		case EquipmentIndex.Weapon3:
			CachedWeaponSlot3Entity = cachedWeaponEntity;
			break;
		case EquipmentIndex.ExtraWeaponSlot:
			CachedWeaponSlot4Entity = cachedWeaponEntity;
			break;
		}
		return this;
	}

	public AgentVisualsData Entity(GameEntity entity)
	{
		EntityData = entity;
		return this;
	}

	public AgentVisualsData UseTranslucency(bool useTranslucency)
	{
		UseTranslucencyData = useTranslucency;
		return this;
	}

	public AgentVisualsData UseTesselation(bool useTesselation)
	{
		UseTesselationData = useTesselation;
		return this;
	}

	public AgentVisualsData ActionCode(in ActionIndexCache actionCode)
	{
		ActionCodeData = actionCode;
		return this;
	}

	public AgentVisualsData RightWieldedItemIndex(int rightWieldedItemIndex)
	{
		RightWieldedItemIndexData = rightWieldedItemIndex;
		return this;
	}

	public AgentVisualsData LeftWieldedItemIndex(int leftWieldedItemIndex)
	{
		LeftWieldedItemIndexData = leftWieldedItemIndex;
		return this;
	}

	public AgentVisualsData Scale(float scale)
	{
		ScaleData = scale;
		return this;
	}

	public AgentVisualsData CharacterObjectStringId(string characterObjectStringId)
	{
		CharacterObjectStringIdData = characterObjectStringId;
		return this;
	}

	public AgentVisualsData HasClippingPlane(bool hasClippingPlane)
	{
		HasClippingPlaneData = hasClippingPlane;
		return this;
	}

	public AgentVisualsData MountCreationKey(string mountCreationKey)
	{
		MountCreationKeyData = mountCreationKey;
		return this;
	}

	public AgentVisualsData AddColorRandomness(bool addColorRandomness)
	{
		AddColorRandomnessData = addColorRandomness;
		return this;
	}
}
