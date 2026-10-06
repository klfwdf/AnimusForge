using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CharacterCreationContent;

public class NarrativeMenuCharacter
{
	public readonly string StringId;

	public readonly bool IsHuman;

	public string SpawnPointEntityId;

	public BodyProperties BodyProperties { get; private set; }

	public int Race { get; private set; }

	public bool IsFemale { get; set; }

	public MBEquipmentRoster Equipment { get; private set; }

	public string AnimationId { get; private set; }

	public MountCreationKey MountCreationKey { get; private set; }

	public string Item1Id { get; private set; }

	public string Item2Id { get; private set; }

	public EquipmentIndex RightHandEquipmentIndex { get; private set; }

	public EquipmentIndex LeftHandEquipmentIndex { get; private set; }

	public NarrativeMenuCharacter(string stringId, BodyProperties bodyProperties, int race, bool isFemale)
	{
		StringId = stringId;
		BodyProperties = bodyProperties;
		Race = race;
		IsFemale = isFemale;
		IsHuman = true;
		SpawnPointEntityId = "spawnpoint_player_1";
		AnimationId = "act_inventory_idle_start";
		Equipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("player_char_creation_default");
	}

	public NarrativeMenuCharacter(string stringId)
	{
		StringId = stringId;
		IsHuman = false;
		SpawnPointEntityId = "spawnpoint_mount_1";
		AnimationId = "act_inventory_idle_start";
	}

	public void UpdateBodyProperties(BodyProperties bodyProperties, int race, bool isFemale)
	{
		BodyProperties = bodyProperties;
		Race = race;
		IsFemale = isFemale;
	}

	public void SetEquipment(MBEquipmentRoster equipment)
	{
		Equipment = equipment;
	}

	public void SetAnimationId(string animationId)
	{
		AnimationId = animationId;
	}

	public void SetRightHandItem(string itemId)
	{
		Item1Id = itemId;
	}

	public void SetLeftHandItem(string itemId)
	{
		Item2Id = itemId;
	}

	public void EquipRightHandItemWithEquipmentIndex(EquipmentIndex item)
	{
		RightHandEquipmentIndex = item;
	}

	public void EquipLeftHandItemWithEquipmentIndex(EquipmentIndex item)
	{
		LeftHandEquipmentIndex = item;
	}

	public void SetSpawnPointEntityId(string spawnPointEntityId)
	{
		SpawnPointEntityId = spawnPointEntityId;
	}

	public void ChangeAge(float age)
	{
		BodyProperties originalBodyProperties = BodyProperties;
		BodyProperties = FaceGen.GetBodyPropertiesWithAge(ref originalBodyProperties, age);
	}

	public void SetMountCreationKey(MountCreationKey mountCreationKey)
	{
		MountCreationKey = mountCreationKey;
	}

	public void SetHorseItemId(string itemId)
	{
		Item1Id = itemId;
	}

	public void SetHarnessItemId(string itemId)
	{
		Item2Id = itemId;
	}
}
